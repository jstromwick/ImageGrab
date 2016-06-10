using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;
using log4net;

namespace ImageGrab
{
    public class ImageGrabber
    {
        private readonly ILog _log;

        public ImageGrabber(ILog log)
        {
            _log = log;
        }

        public async Task<IEnumerable<DownloadResult>> DownloadAllImages(string url, string filePath)
        {
            if (!CheckUrl(url))
            {
                _log.ErrorFormat("url '{0}' failed formatting check", url);
                throw new ArgumentException($"'{url}' is not a well formed or aboslute url", nameof(url));
            }

            _log.DebugFormat("Formatting for url '{0}' is valid", url);
            var html = await GetHtml(url);

            if (html == null)
            {
                _log.DebugFormat("HTML was not returned");
                return null;
            }

            var imageUrls = GetImageUrlsFromHtml(html);
            _log.DebugFormat("Found {0} distinct img urls", imageUrls.Length);

            var absUrls = CreateAbsoluteUrls(url, imageUrls);
            var downloadResults = await DownloadImages(absUrls, filePath);

            return downloadResults;
        }

        public static bool CheckUrl(string url)
        {
            return Uri.IsWellFormedUriString(url, UriKind.Absolute);
        }

        internal async Task<string> GetHtml(string url)
        {
            using (var httpClient = new HttpClient())
            {
                var responseMessage = await httpClient.GetAsync(url);
                if (!responseMessage.Content.Headers.ContentType.MediaType.StartsWith("text/html"))
                {
                    _log.DebugFormat("Response from url '{0}' did not have the correct content type. Expected 'text/html' but was '{1}'", url,
                        responseMessage.Content.Headers.ContentType.MediaType);
                    return null;
                }

                var html = await responseMessage.Content.ReadAsStringAsync();
                return html;
            }
        }

        internal static string[] GetImageUrlsFromHtml(string html)
        {
            var parser = new HtmlParser();
            var htmlDocument = parser.Parse(html);
            var imageUrls = htmlDocument.All
                .Where(e => e.TagName == "IMG" && e.HasAttribute("SRC"))
                .Select(e => e.GetAttribute("SRC"))
                .Where(e => e != null)
                .Distinct()
                .ToArray();

            return imageUrls;
        }

        private string[] CreateAbsoluteUrls(string baseUrl, IEnumerable<string> imageUrls)
        {
            return imageUrls.Select(u => CreateAbsoluteUrl(baseUrl, u)).ToArray();
        }

        internal string CreateAbsoluteUrl(string baseUrl, string sourceUrl)
        {
            //Already fully qualified
            if (sourceUrl.StartsWith("http"))
            {
                return sourceUrl;
            }

            var uri = new Uri(baseUrl, UriKind.Absolute);
            var host = uri.Host;
            var scheme = uri.Scheme;

            var hostUrl = (!string.IsNullOrWhiteSpace(scheme) ? scheme + "://" : "") + host.Trim('/');

            //Double wacks indicate call to another domain using current protocol
            if (sourceUrl.StartsWith("//"))
            {
                sourceUrl = sourceUrl.TrimStart('/');

                return scheme + "://" + (UrlHasSubDomain(sourceUrl) ? "" : "www.") + sourceUrl.TrimStart('/');
            }

            //Path relative to host, qualify and return
            if (sourceUrl.StartsWith("/"))
            {
                return hostUrl + sourceUrl;
            }

            //Get the local path in segments removing the last segment (e.g. index.html) to get current directory
            var chompTo = uri.LocalPath.LastIndexOf("/", StringComparison.Ordinal);

            var localPath = uri.LocalPath.Substring(0, chompTo)
                .Trim('/')
                .Split(new[] {'/'}, StringSplitOptions.None);

            //Path relative to document location
            if (sourceUrl.StartsWith("../"))
            {
                //count how many parent directory's we need to pop up  
                var sourceUrlParts = sourceUrl.Split(new[] {"../"}, StringSplitOptions.None);
                var numStepsBack = sourceUrlParts.Length - 1;

                //subtract number of pops from the total path length to get target path level
                var targetPathLevel = localPath.Length - numStepsBack;

                //set path to the appropriate segments from the original path plus the relevant sourceUrl
                var path = string.Join("/", localPath.Take(targetPathLevel)) + "/" + sourceUrlParts.Last();

                return string.Join("/", hostUrl, path);
            }

            //if neither absolute, or parent directory, src is coming from the current directory
            if (sourceUrl.StartsWith("./"))
            {
                sourceUrl = sourceUrl.Substring(2, sourceUrl.Length - 2);
            }

            return hostUrl + "/" + string.Join("/", localPath) + "/" + sourceUrl;
        }

        private static bool UrlHasSubDomain(string url)
        {
            if (url == null)
            {
                return false;
            }

            var hostName = url.Split(new[] {"/"}, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(s => s.Contains("."));

            return hostName != null && hostName.Count(c => c == '.') > 1;
        }

        internal async Task<IEnumerable<DownloadResult>> DownloadImages(IEnumerable<string> urls, string filePath)
        {
            CreatePath(filePath);

            var downloadUrls = urls.ToArray();
            _log.DebugFormat("Attempting to download images from {0} urls", downloadUrls.Length);

            var downloadResults = new List<DownloadResult>();
            foreach (var url in downloadUrls)
            {
                var downloadResult = new DownloadResult
                {
                    Url = url
                };
                downloadResults.Add(downloadResult);

                using (var httpClient = new HttpClient())
                {
                    _log.DebugFormat("Downloading image from '{0}'", url);
                    try
                    {
                        var response = await httpClient.GetAsync(url);
                        if (!response.IsSuccessStatusCode)
                        {
                            downloadResult.ErrorReason = $"{response.StatusCode} - {response.ReasonPhrase}";
                            _log.DebugFormat("Failed to download file. Server returned Status Code {0}", downloadResult.ErrorReason);
                            continue;
                        }


                        var fileName = url.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries).Last();

                        //Prevent Illegal characters in filename
                        var invalidFileNameChars = Path.GetInvalidFileNameChars();
                        if (fileName.Any(n => invalidFileNameChars.Contains(n)))
                        {
                            fileName = "safe-" + Guid.NewGuid();
                        }

                        var fullPath = Path.Combine(filePath, fileName);

                        //Avoid duplicate file names
                        if (File.Exists(fullPath))
                        {
                            var extensionStart = fileName.LastIndexOf('.');

                            var newName = extensionStart == -1
                                              ? fileName + "-" + Guid.NewGuid()
                                              : fileName.Substring(0, extensionStart) + "_" + Guid.NewGuid() +
                                                fileName.Substring(extensionStart, fileName.Length - extensionStart);

                            fullPath = Path.Combine(filePath, newName);
                        }

                        downloadResult.FileLocation = fullPath;
                        downloadResult.FileSize = response.Content.Headers.ContentLength;

                        _log.DebugFormat("Downloaded Content is {0} bytes", downloadResult.FileSize);
                        var responseContent = await response.Content.ReadAsStreamAsync();

                        using (var fileStream = File.Create(fullPath))
                        {
                            responseContent.CopyTo(fileStream);
                        }

                        _log.DebugFormat("Image output to '{0}'", fullPath);
                    }

                    catch (Exception e)
                    {
                        downloadResult.ErrorReason = $"Failed to download and save resource. Exception:\n {e.Message + "\n" + e.StackTrace}";
                    }
                }
            }
            return downloadResults;
        }

        internal static void CreatePath(string path)
        {
            Directory.CreateDirectory(path);
        }
    }
}