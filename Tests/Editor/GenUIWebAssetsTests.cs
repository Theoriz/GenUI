using System.Text;
using NUnit.Framework;

namespace Theoriz.GenUI.Tests.Editor
{
    /// <summary>
    /// The plain-HTTP half of the web mirror's port: which path is answered with which client file.
    /// </summary>
    /// <remarks>
    /// Loading the asset here is the point as much as the routing is: the client files only reach a
    /// player build because GenUIWebAssets references them, so a rename that breaks the reference has
    /// to fail somewhere.
    /// </remarks>
    public class GenUIWebAssetsTests
    {
        //ResponseFor serves what Preload loaded: the server does this on the main thread at startup.
        [SetUp]
        public void SetUp()
        {
            GenUIWebAssets.Preload();
        }

        [TestCase("/", "text/html")]
        [TestCase("/index.html", "text/html")]
        [TestCase("/client.css", "text/css")]
        [TestCase("/client.js", "application/javascript")]
        public void AClientFile_IsServedWithItsContentType(string path, string contentType)
        {
            var response = GenUIWebAssets.ResponseFor(path);

            Assert.IsTrue(response.HasValue, "Nothing served for '" + path + "'.");
            Assert.AreEqual(200, response.Value.StatusCode);
            StringAssert.StartsWith(contentType, response.Value.ContentType);
            Assert.Greater(response.Value.Body.Length, 0);
        }

        //A cache-busting query is the browser's, not a path of its own.
        [Test]
        public void AQueryString_IsNotPartOfThePath()
        {
            Assert.IsTrue(GenUIWebAssets.ResponseFor("/client.js?v=2").HasValue);
        }

        [TestCase("/nothing")]
        [TestCase("/client.js.txt")]
        public void AnythingElse_IsA404(string path)
        {
            Assert.IsFalse(GenUIWebAssets.ResponseFor(path).HasValue);
        }

        //The page has to reach the client files it is served alongside, on the same port.
        [Test]
        public void ThePage_AsksForTheStyleAndTheScript()
        {
            var page = Encoding.UTF8.GetString(GenUIWebAssets.ResponseFor("/").Value.Body);

            StringAssert.Contains("/client.css", page);
            StringAssert.Contains("/client.js", page);
        }
    }
}
