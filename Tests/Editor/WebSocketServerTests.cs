using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace Theoriz.GenUI.Tests.Editor
{
    /// <summary>
    /// The transport over a real loopback socket: the handshake, both roles on one port, and the
    /// teardown - the parts the pure codec tests cannot reach.
    /// </summary>
    /// <remarks>
    /// Every wait is bounded, and the server binds port 0 so a machine already running the demo does
    /// not fail the suite.
    /// </remarks>
    public class WebSocketServerTests
    {
        const int TimeoutMs = 3000;

        WebSocketServer _server;
        readonly List<TcpClient> _clients = new List<TcpClient>();

        [SetUp]
        public void SetUp()
        {
            _server = new WebSocketServer(0);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var client in _clients)
            {
                try
                {
                    client.Close();
                }
                catch (Exception)
                {
                    //Already closed by the test.
                }
            }

            _clients.Clear();
            _server.Stop();
        }

        #region Helpers

        static bool WaitUntil(Func<bool> condition)
        {
            var watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < TimeoutMs)
            {
                if (condition())
                    return true;

                Thread.Sleep(5);
            }

            return condition();
        }

        TcpClient Connect()
        {
            var client = new TcpClient("127.0.0.1", _server.Port);
            client.NoDelay = true;
            client.GetStream().ReadTimeout = TimeoutMs;
            _clients.Add(client);

            return client;
        }

        static void Write(TcpClient client, byte[] bytes)
        {
            client.GetStream().Write(bytes, 0, bytes.Length);
            client.GetStream().Flush();
        }

        static void WriteAscii(TcpClient client, string text)
        {
            Write(client, Encoding.ASCII.GetBytes(text));
        }

        /// <summary>Opens a connection and completes the handshake on it.</summary>
        TcpClient ConnectWebSocket()
        {
            var client = Connect();
            WriteAscii(client,
                "GET / HTTP/1.1\r\nHost: localhost\r\nUpgrade: websocket\r\nConnection: Upgrade\r\n"
                + "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\nSec-WebSocket-Version: 13\r\n\r\n");

            var response = ReadHead(client);
            Assert.IsTrue(response.StartsWith("HTTP/1.1 101 "), response);
            Assert.IsTrue(response.Contains("Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo="), response);

            return client;
        }

        /// <summary>Reads up to the blank line ending an HTTP response head.</summary>
        static string ReadHead(TcpClient client)
        {
            var stream = client.GetStream();
            var head = new StringBuilder();
            var one = new byte[1];

            while (!head.ToString().EndsWith("\r\n\r\n"))
            {
                if (stream.Read(one, 0, 1) <= 0)
                    break;

                head.Append((char)one[0]);
            }

            return head.ToString();
        }

        static string ReadBody(TcpClient client, int length)
        {
            var stream = client.GetStream();
            var body = new byte[length];
            var read = 0;

            while (read < length)
            {
                var got = stream.Read(body, read, length - read);
                if (got <= 0)
                    break;

                read += got;
            }

            return Encoding.UTF8.GetString(body, 0, read);
        }

        /// <summary>Reads one whole frame off a socket, however it was split into packets.</summary>
        static WebSocketFrameInfo ReadFrame(TcpClient client)
        {
            var stream = client.GetStream();
            var buffer = new byte[8192];
            var pending = 0;

            while (true)
            {
                WebSocketFrameInfo frame;
                var status = WebSocketFrame.TryRead(buffer, 0, pending, buffer.Length, out frame);

                if (status == WebSocketFrameStatus.Complete)
                    return frame;

                Assert.AreNotEqual(WebSocketFrameStatus.Invalid, status, "The server sent an invalid frame.");

                var read = stream.Read(buffer, pending, buffer.Length - pending);
                Assert.Greater(read, 0, "The connection closed before a whole frame arrived.");
                pending += read;
            }
        }

        static void SendText(TcpClient client, string text)
        {
            Write(client, WebSocketFrame.Encode(WebSocketOpcode.Text, Encoding.UTF8.GetBytes(text),
                new byte[] { 0x37, 0xfa, 0x21, 0x3d }));
        }

        WebMessage NextMessage()
        {
            WebMessage message = default(WebMessage);
            Assert.IsTrue(WaitUntil(() => _server.Inbound.TryDequeue(out message)), "No message arrived.");

            return message;
        }

        #endregion

        [Test]
        public void Starting_BindsAPortAndStoppingReleasesIt()
        {
            Assert.IsTrue(_server.Start());
            Assert.IsTrue(_server.IsRunning);
            Assert.Greater(_server.Port, 0);

            _server.Stop();
            Assert.IsFalse(_server.IsRunning);
        }

        [Test]
        public void APlainGet_IsAnsweredByTheHandler()
        {
            _server.HttpHandler = path => path == "/client.js"
                ? WebHttpResponse.Ok("text/javascript", "console.log(1)")
                : (WebHttpResponse?)null;
            _server.Start();

            var client = Connect();
            WriteAscii(client, "GET /client.js HTTP/1.1\r\nHost: localhost\r\n\r\n");

            var head = ReadHead(client);
            Assert.IsTrue(head.StartsWith("HTTP/1.1 200 OK"), head);
            Assert.AreEqual("console.log(1)", ReadBody(client, 14));
        }

        [Test]
        public void AnUnhandledPath_Is404()
        {
            _server.HttpHandler = path => null;
            _server.Start();

            var client = Connect();
            WriteAscii(client, "GET /nothing HTTP/1.1\r\nHost: localhost\r\n\r\n");

            Assert.IsTrue(ReadHead(client).StartsWith("HTTP/1.1 404 "));
        }

        [Test]
        public void AnUpgradeRequest_ConnectsAndReportsIt()
        {
            _server.Start();
            ConnectWebSocket();

            Assert.AreEqual(WebMessageKind.Connected, NextMessage().Kind);
            Assert.IsTrue(WaitUntil(() => _server.ClientCount == 1));
        }

        [Test]
        public void AClientFrame_ArrivesAsAQueuedMessage()
        {
            _server.Start();
            var client = ConnectWebSocket();
            var connected = NextMessage();

            SendText(client, "{\"t\":\"set\",\"value\":1}");

            var message = NextMessage();
            Assert.AreEqual(WebMessageKind.Text, message.Kind);
            Assert.AreEqual("{\"t\":\"set\",\"value\":1}", message.Text);
            Assert.AreEqual(connected.ClientId, message.ClientId);
        }

        //Two frames in one packet is the ordinary case for a browser sending a drag.
        [Test]
        public void SeveralFramesInOnePacket_AllArrive()
        {
            _server.Start();
            var client = ConnectWebSocket();
            NextMessage();

            SendText(client, "one");
            SendText(client, "two");

            Assert.AreEqual("one", NextMessage().Text);
            Assert.AreEqual("two", NextMessage().Text);
        }

        [Test]
        public void ABroadcast_ReachesEveryConnectedBrowser()
        {
            _server.Start();
            var first = ConnectWebSocket();
            var second = ConnectWebSocket();
            Assert.IsTrue(WaitUntil(() => _server.ClientCount == 2));

            _server.Broadcast("{\"t\":\"values\"}");

            Assert.AreEqual("{\"t\":\"values\"}", ReadFrame(first).Text);
            Assert.AreEqual("{\"t\":\"values\"}", ReadFrame(second).Text);
        }

        [Test]
        public void ASend_ReachesOneBrowser()
        {
            _server.Start();
            var client = ConnectWebSocket();
            var connected = NextMessage();

            _server.Send(connected.ClientId, "schema");

            Assert.AreEqual("schema", ReadFrame(client).Text);
        }

        [Test]
        public void APing_IsAnsweredWithAPong()
        {
            _server.Start();
            var client = ConnectWebSocket();

            Write(client, WebSocketFrame.Encode(WebSocketOpcode.Ping, Encoding.UTF8.GetBytes("Hello"),
                new byte[] { 1, 2, 3, 4 }));

            var pong = ReadFrame(client);
            Assert.AreEqual(WebSocketOpcode.Pong, pong.Opcode);
            Assert.AreEqual("Hello", pong.Text);
        }

        [Test]
        public void AClosedTab_IsReportedAndForgotten()
        {
            _server.Start();
            var client = ConnectWebSocket();
            Assert.AreEqual(WebMessageKind.Connected, NextMessage().Kind);

            client.Close();

            Assert.AreEqual(WebMessageKind.Disconnected, NextMessage().Kind);
            Assert.IsTrue(WaitUntil(() => _server.ClientCount == 0));
        }

        //An unparseable frame cannot be recovered from: the server closes rather than resynchronising.
        [Test]
        public void AnInvalidFrame_ClosesTheConnection()
        {
            _server.Start();
            var client = ConnectWebSocket();
            NextMessage();

            //RSV1 set, which only a negotiated extension could justify.
            Write(client, new byte[] { 0xC1, 0x80, 0, 0, 0, 0 });

            var close = ReadFrame(client);
            Assert.AreEqual(WebSocketOpcode.Close, close.Opcode);
            Assert.AreEqual(1002, WebSocketFrame.CloseCode(close.Payload));
        }

        [Test]
        public void Stopping_DisconnectsEveryClient()
        {
            _server.Start();
            ConnectWebSocket();
            Assert.IsTrue(WaitUntil(() => _server.ClientCount == 1));

            _server.Stop();

            Assert.AreEqual(0, _server.ClientCount);
            Assert.IsFalse(_server.IsRunning);
        }
    }
}
