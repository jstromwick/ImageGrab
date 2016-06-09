using ImageGrab;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ImageGrabTests
{
    [TestClass]
    public class ImageGrabberTests
    {
        [TestMethod]
        public void SimpleTest()
        {
            var imageGrabber = new ImageGrabber();
            imageGrabber.GetImages("www.google.com", "");
        }

        [TestMethod]
        public void GetHtmlTest()
        {
            var imageGrabber = new ImageGrabber();
            var html = imageGrabber.GetHtml("www.google.com");

            Assert.IsNotNull(html);
        }
        
    }
}