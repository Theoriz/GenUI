using System.Text;
using NUnit.Framework;

namespace Theoriz.GenUI.Tests.Editor
{
    /// <summary>
    /// The plain HTTP half of the port - the response a browser gets when it asks for the client files
    /// rather than for a WebSocket.
    /// </summary>
    public class WebHttpResponseTests
    {
        static string HeadOf(WebHttpResponse response)
        {
            var text = Encoding.UTF8.GetString(response.ToBytes());
            return text.Substring(0, text.IndexOf("\r\n\r\n") + 4);
        }

        [Test]
        public void AServedFile_IsA200WithItsLengthAndType()
        {
            var head = HeadOf(WebHttpResponse.Ok("text/html", "<p>hi</p>"));

            Assert.IsTrue(head.StartsWith("HTTP/1.1 200 OK\r\n"), head);
            Assert.IsTrue(head.Contains("Content-Type: text/html; charset=utf-8\r\n"), head);
            Assert.IsTrue(head.Contains("Content-Length: 9\r\n"), head);
        }

        //Content-Length counts bytes, not characters, or a browser truncates the body.
        [Test]
        public void TheLength_CountsBytesNotCharacters()
        {
            var head = HeadOf(WebHttpResponse.Ok("text/plain", "éé"));

            Assert.IsTrue(head.Contains("Content-Length: 4\r\n"), head);
        }

        [Test]
        public void TheBody_FollowsTheHead()
        {
            var bytes = WebHttpResponse.Ok("text/plain", "body").ToBytes();
            var text = Encoding.UTF8.GetString(bytes);

            Assert.IsTrue(text.EndsWith("\r\n\r\nbody"), text);
        }

        [Test]
        public void AMissingFile_IsA404()
        {
            Assert.IsTrue(HeadOf(WebHttpResponse.NotFound()).StartsWith("HTTP/1.1 404 Not Found\r\n"));
        }

        [Test]
        public void AnEmptyBody_IsStillLengthZero()
        {
            var head = HeadOf(new WebHttpResponse { StatusCode = 200, ContentType = "text/plain" });

            Assert.IsTrue(head.Contains("Content-Length: 0\r\n"), head);
        }

        //The client files are edited while a tab is open; a cached copy would show yesterday's UI.
        [Test]
        public void NothingIsCached_AndTheConnectionCloses()
        {
            var head = HeadOf(WebHttpResponse.Ok("text/css", "body{}"));

            Assert.IsTrue(head.Contains("Cache-Control: no-store\r\n"), head);
            Assert.IsTrue(head.Contains("Connection: close\r\n"), head);
        }
    }
}
