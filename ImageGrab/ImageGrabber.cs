using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AngleSharp.Parser.Html;
using RestSharp;

namespace ImageGrab
{
    public class ImageGrabber
    {
        public bool GetImages(string url, string filePath)
        {
            if (!CheckUrl(url))
            {
                return false;
            }

            var html = GetHtml(url);

            if (html == null)
            {
                return false;
            }

            var imageUrls = GetImageUrlsFromHtml(html);
            DownloadImages(url, imageUrls, filePath);

            return true;
        }

        internal void DownloadImages(string baseUrl, IEnumerable<string> urls, string filePath)
        {
            CreatePath(filePath);

            var uri = new Uri(baseUrl, UriKind.RelativeOrAbsolute);
            var host = uri.Host;

            foreach (var url in urls)
            {
                var downloadUrl = url;
                if (!url.StartsWith("http"))
                {
                    if (url.StartsWith("/"))
                    {
                    }
                }
            }
        }

        internal static bool CheckUrl(string url)
        {
            return Uri.IsWellFormedUriString(url, UriKind.Absolute);
        }

        internal string GetHtml(string url)
        {
            var client = new RestClient(url);
            var request = new RestRequest(Method.GET);
            var response = client.Execute(request);

            if (!response.ContentType.StartsWith("text/html"))
            {
                return null;
            }

            return response.Content;
        }

        internal static IEnumerable<string> GetImageUrlsFromHtml(string html)
        {
            var parser = new HtmlParser();
            var htmlDocument = parser.Parse(html);
            var imageUrls = htmlDocument.All
                .Where(e => e.TagName == "IMG" && e.HasAttribute("SRC"))
                .Select(e => e.GetAttribute("SRC"))
                .Where(e => e != null)
                .Distinct();

            return imageUrls;
        }

        internal static void CreatePath(string path)
        {
            Directory.CreateDirectory(path);
        }
    }
}