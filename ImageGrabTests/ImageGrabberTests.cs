using System;
using System.IO;
using ImageGrab;
using NUnit.Framework;

namespace ImageGrabTests
{
    [TestFixture]
    public class ImageGrabberTests
    {
        [Test]
        [TestCase("http://www.google.com/foo/bar", "http://www.google.com", new[] { "foo", "bar" })]
        [TestCase("http://www.google.com/foo/bar/index.html", "http://www.google.com", new[] { "foo", "bar" })]
        [TestCase("http://www.google.com/foo/bar?toodle=1325", "http://www.google.com", new[] { "foo", "bar" })]
        [TestCase("http://www.google.com/foo/bar#app/stuff/biz?toodle=1325", "http://www.google.com", new[] { "foo", "bar" })]
        public void BaseUrlTests(string baseUrl, string expectedHost, string[] expectedPath)
        {
            var uri = new Uri(baseUrl, UriKind.Absolute);
            var host = uri.Host;
            var scheme = uri.Scheme;
            
            var hostUrl = (!string.IsNullOrWhiteSpace(scheme) ? scheme + "://" : "") + host;
            var localPath = uri.LocalPath.Split(new[] {'/'}, StringSplitOptions.RemoveEmptyEntries);

            Assert.AreEqual(expectedHost, hostUrl);
            CollectionAssert.AreEqual(expectedPath, localPath);
        }

        [Test]
        public void GetHtmlTest()
        {
            var imageGrabber = new ImageGrabber();
            var html = imageGrabber.GetHtml("http://www.google.com");

            Assert.IsNotNull(html);
        }


        [Test]
        public void GetImageUrlsFromHtmlTest()
        {
            var testHtml = @"

<!DOCTYPE html>
<html lang=""en"">
<head>
 	<meta http-equiv=""Content-type"" content=""text/html; charset=utf-8"" />
	<link rel=""stylesheet"" type=""text/css"" href=""/css/style.css"">
	<link href='http://fonts.googleapis.com/css?family=Montserrat:400,700' rel='stylesheet' type='text/css'>    
   <title>Scott Hanselman - Coder, Blogger, Teacher, Speaker, Author</title>
</head>
<body class=""line-darkbrown"">
<section class=""containerOuter line-tan"" id=""topbar"">
    <section class=""containerInner"">
        <h1><a href=""/"">Scott Hanselman</a></h1>
        <nav>
            <ul>
                <li><a href=""http://hanselman.com/about"">about</a></li>
                <li><a href=""http://hanselman.com/blog"" >blog</a></li>
                <li><a href=""http://hanselman.com/speaking"">speaking</a></li>
                <li><a href=""http://hanselman.com/podcasts"">podcasts</a></li>
                <li><a href=""http://hanselman.com/books"">books</a></li>  
          </ul>
            <ul class=""itemFeed"">
                  <li><a href=""http://www.hanselminutes.com""><img src=""/images/blog-hanselminutes.png"" alt=""The Hanselminutes Podcast"" /></a></li>
                  <li><a href=""http://thisdeveloperslife.com""><img src=""/images/blog-tdl.png"" alt=""This Developer's Life"" /></a></li>
                  <li><a href=""http://www.ratchetandthegeek.com""><img src=""/images/blog-rachetgeek.png"" alt=""Rachet and the Geek"" /></a></li>
                  <li><a href=""http://friday.azure.com""><img src=""/images/blog-AzureFriday.png"" alt=""Azure Friday"" /></a></li>
        </ul>              
        </nav>        
    </section>
</section>
</body>
</html>
";
            var urls = ImageGrabber.GetImageUrlsFromHtml(testHtml);

            var expectedUrls = new[]
            {
                "/images/blog-hanselminutes.png",
                "/images/blog-tdl.png",
                "/images/blog-rachetgeek.png",
                "/images/blog-AzureFriday.png"
            };

            CollectionAssert.AreEquivalent(expectedUrls, urls);
        }

        [Test]
        public void SimpleTest()
        {
            var imageGrabber = new ImageGrabber();
            imageGrabber.GetImages("www.google.com", "");
        }


        [Test]
        [TestCase("www.google.com", ExpectedResult = false)]
        [TestCase("http://www.google.com", ExpectedResult = true)]
        [TestCase("https://www.google.com", ExpectedResult = true)]
        [TestCase("https://httpbin.org/get", ExpectedResult = true)]
        public bool TestUrl(string url)
        {
            return ImageGrabber.CheckUrl(url);
        }

        [Test]
        [TestCase(@"C:\temp\foo")]
        [TestCase(@"C:\temp\foo\bz\doodle")]
        [TestCase(@"C:\temp\foo.bz")]
        [TestCase(@"C:\temp\foo/bz")]
        public void VerifyFolderPathTest(string path)
        {
            try
            {
                ImageGrabber.CreatePath(path);
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }
    }
}