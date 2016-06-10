using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageGrab;
using log4net;
using NSubstitute;
using NUnit.Framework;

namespace ImageGrabTests
{
    [TestFixture]
    public class ImageGrabberTests
    {
        [TestCase("http://www.hanselman.com/", "/images/blog-hanselminutes.png", ExpectedResult = "http://www.hanselman.com/images/blog-hanselminutes.png")]
        [TestCase("http://www.hanselman.com/blog/MSBuildStructuredLogRecordAndVisualizeYourBuilds.aspx",
             "http://www.hanselman.com/blog/content/binary/Windows-Live-Writer/aff20f822904_BAA2/image_d.png",
             ExpectedResult = "http://www.hanselman.com/blog/content/binary/Windows-Live-Writer/aff20f822904_BAA2/image_d.png")]
        [TestCase("https://www.pivotaltracker.com/why-tracker/", "/marketing_assets/why-binoculars.png",
             ExpectedResult = "https://www.pivotaltracker.com/marketing_assets/why-binoculars.png")]
        [TestCase("http://www.chami.com/html-kit/minit/pages/imgtag1.html", "../../i/g/cached/tmp10.jpg", ExpectedResult = "http://www.chami.com/html-kit/i/g/cached/tmp10.jpg"
         )]
        [TestCase("http://www.chami.com/html-kit/minit/pages/imgtag1.html", "tmp10.jpg", ExpectedResult = "http://www.chami.com/html-kit/minit/pages/tmp10.jpg")]
        public string CreateAbsoluteUrlTests(string baseUrl, string imgSrcUrl)
        {
            var imageGrabber = new ImageGrabber(Substitute.For<ILog>());
            return imageGrabber.CreateAbsoluteUrl(baseUrl, imgSrcUrl);
        }

        [Test, Explicit]
        public void CleanOutTestDirectory()
        {
            Directory.Delete(@"C:\temp\ImageGrabber", true);
        }

        [Test]
        public async Task DownloadTest()
        {
            CleanOutTestDirectory();

            var imageGrabber = new ImageGrabber(Substitute.For<ILog>());

            const string url = "http://www.hanselman.com/images/blog-hanselminutes.png";
            var results = await imageGrabber.DownloadImages(new[] {url}, @"C:\temp\ImageGrabber");

            Assert.That(results.Count(), Is.EqualTo(1));
            var downloadResult = results.First();

            Assert.That(downloadResult.Url, Is.EqualTo(url));
            Assert.That(downloadResult.FileLocation, Is.EqualTo(@"C:\temp\ImageGrabber\blog-hanselminutes.png"));
            Assert.That(downloadResult.FileSize, Is.EqualTo(1538));

            Assert.That(File.Exists(downloadResult.FileLocation), Is.True);
            var fileInfo = new FileInfo(downloadResult.FileLocation);
            Assert.That(fileInfo.Length, Is.EqualTo(downloadResult.FileSize));
        }


        [Test]
        public async Task GetHtmlTest()
        {
            var imageGrabber = new ImageGrabber(Substitute.For<ILog>());
            var html = await imageGrabber.GetHtml("http://www.google.com");

            Assert.IsNotNull(html);
            Assert.That(html, Does.Contain("google"));
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
        public async Task SimpleTest()
        {
            var imageGrabber = new ImageGrabber(Substitute.For<ILog>());
            await imageGrabber.GetImages("www.google.com", "");
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