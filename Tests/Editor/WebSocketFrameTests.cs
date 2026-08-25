using System.Text;
using NUnit.Framework;

namespace Theoriz.GenUI.Tests.Editor
{
    /// <summary>
    /// The RFC 6455 handshake and frame codec the web mirror's transport is built on, checked against
    /// the examples published in the RFC itself (1.3 for the accept key, 5.7 for the frames).
    /// </summary>
    public class WebSocketFrameTests
    {
        const int Max = 1 << 20;

        static byte[] Bytes(params int[] values)
        {
            var bytes = new byte[values.Length];
            for (var i = 0; i < values.Length; i++)
                bytes[i] = (byte)values[i];
            return bytes;
        }

        static WebSocketFrameInfo Read(byte[] buffer)
        {
            WebSocketFrameInfo frame;
            Assert.AreEqual(WebSocketFrameStatus.Complete,
                WebSocketFrame.TryRead(buffer, 0, buffer.Length, Max, out frame));
            return frame;
        }

        static WebSocketFrameStatus StatusOf(byte[] buffer)
        {
            WebSocketFrameInfo frame;
            return WebSocketFrame.TryRead(buffer, 0, buffer.Length, Max, out frame);
        }

        #region Handshake

        //RFC 6455 1.3.
        [Test]
        public void TheAcceptKey_IsTheRfcsExample()
        {
            Assert.AreEqual("s3pPLMBiTxaQ9kYGzzhZRbK+xOo=", WebSocketFrame.AcceptKey("dGhlIHNhbXBsZSBub25jZQ=="));
        }

        const string UpgradeRequest =
            "GET /ws HTTP/1.1\r\n"
            + "Host: 192.168.0.10:8080\r\n"
            + "Upgrade: websocket\r\n"
            + "Connection: keep-alive, Upgrade\r\n"
            + "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\n"
            + "Sec-WebSocket-Version: 13\r\n\r\n";

        [Test]
        public void AnUpgradeRequest_IsRecognised()
        {
            Assert.IsTrue(WebSocketFrame.IsUpgradeRequest(UpgradeRequest));
        }

        [Test]
        public void APlainGet_IsNotAnUpgradeRequest()
        {
            Assert.IsFalse(WebSocketFrame.IsUpgradeRequest("GET /client.js HTTP/1.1\r\nHost: x\r\n\r\n"));
        }

        //A key with no Upgrade header is not an upgrade, and vice versa: all three must agree.
        [Test]
        public void AnUpgradeRequest_WithoutAKey_IsRefused()
        {
            var request = "GET / HTTP/1.1\r\nUpgrade: websocket\r\nConnection: Upgrade\r\n\r\n";
            Assert.IsFalse(WebSocketFrame.IsUpgradeRequest(request));
        }

        [Test]
        public void AHeader_IsFoundWhateverItsCasing()
        {
            Assert.AreEqual("websocket", WebSocketFrame.Header(UpgradeRequest, "upgrade"));
            Assert.AreEqual("dGhlIHNhbXBsZSBub25jZQ==", WebSocketFrame.Header(UpgradeRequest, "SEC-WEBSOCKET-KEY"));
            Assert.IsNull(WebSocketFrame.Header(UpgradeRequest, "Origin"));
        }

        [Test]
        public void TheRequestPath_DropsTheQueryString()
        {
            Assert.AreEqual("/ws", WebSocketFrame.RequestPath(UpgradeRequest));
            Assert.AreEqual("/client.js", WebSocketFrame.RequestPath("GET /client.js?v=2 HTTP/1.1\r\n\r\n"));
        }

        [Test]
        public void ANonGetRequest_HasNoPath()
        {
            Assert.IsNull(WebSocketFrame.RequestPath("POST /set HTTP/1.1\r\n\r\n"));
        }

        [Test]
        public void TheHandshakeResponse_Is101WithTheAcceptKey()
        {
            var response = WebSocketFrame.HandshakeResponse("dGhlIHNhbXBsZSBub25jZQ==");

            Assert.IsTrue(response.StartsWith("HTTP/1.1 101 "), response);
            Assert.IsTrue(response.Contains("Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=\r\n"), response);
            Assert.IsTrue(response.EndsWith("\r\n\r\n"), response);
        }

        #endregion

        #region Encoding

        //RFC 6455 5.7: a single-frame unmasked text message.
        [Test]
        public void AServerTextFrame_IsTheRfcsUnmaskedExample()
        {
            CollectionAssert.AreEqual(Bytes(0x81, 0x05, 0x48, 0x65, 0x6c, 0x6c, 0x6f),
                WebSocketFrame.EncodeText("Hello"));
        }

        //RFC 6455 5.7: the same message masked, as a client sends it.
        [Test]
        public void AMaskedTextFrame_IsTheRfcsMaskedExample()
        {
            var frame = WebSocketFrame.Encode(WebSocketOpcode.Text, Encoding.UTF8.GetBytes("Hello"),
                Bytes(0x37, 0xfa, 0x21, 0x3d));

            CollectionAssert.AreEqual(
                Bytes(0x81, 0x85, 0x37, 0xfa, 0x21, 0x3d, 0x7f, 0x9f, 0x4d, 0x51, 0x58), frame);
        }

        //RFC 6455 5.7: 256 bytes uses the 16-bit length form, 65536 the 64-bit one.
        [Test]
        public void TheSixteenBitLengthForm_StartsAt126Bytes()
        {
            var frame = WebSocketFrame.Encode(WebSocketOpcode.Binary, new byte[256]);

            Assert.AreEqual(0x82, frame[0]);
            Assert.AreEqual(0x7E, frame[1]);
            CollectionAssert.AreEqual(Bytes(0x01, 0x00), new[] { frame[2], frame[3] });
            Assert.AreEqual(4 + 256, frame.Length);
        }

        [Test]
        public void TheSixtyFourBitLengthForm_StartsAbove65535Bytes()
        {
            var frame = WebSocketFrame.Encode(WebSocketOpcode.Binary, new byte[65536]);

            Assert.AreEqual(0x82, frame[0]);
            Assert.AreEqual(0x7F, frame[1]);
            CollectionAssert.AreEqual(Bytes(0, 0, 0, 0, 0, 0x01, 0x00, 0x00),
                new[] { frame[2], frame[3], frame[4], frame[5], frame[6], frame[7], frame[8], frame[9] });
            Assert.AreEqual(10 + 65536, frame.Length);
        }

        [Test]
        public void A125BytePayload_StillUsesTheShortForm()
        {
            var frame = WebSocketFrame.Encode(WebSocketOpcode.Binary, new byte[125]);

            Assert.AreEqual(125, frame[1]);
            Assert.AreEqual(2 + 125, frame.Length);
        }

        [Test]
        public void AMaskKey_MustBeFourBytes()
        {
            Assert.Throws<System.ArgumentException>(() =>
                WebSocketFrame.Encode(WebSocketOpcode.Text, new byte[1], Bytes(1, 2)));
        }

        #endregion

        #region Decoding

        [Test]
        public void AMaskedFrame_DecodesToItsText()
        {
            var frame = Read(WebSocketFrame.Encode(WebSocketOpcode.Text,
                Encoding.UTF8.GetBytes("{\"t\":\"set\"}"), Bytes(0x37, 0xfa, 0x21, 0x3d)));

            Assert.AreEqual(WebSocketOpcode.Text, frame.Opcode);
            Assert.IsTrue(frame.Fin);
            Assert.AreEqual("{\"t\":\"set\"}", frame.Text);
        }

        [Test]
        public void EveryLengthForm_RoundTrips()
        {
            foreach (var length in new[] { 0, 1, 125, 126, 65535, 65536, 70000 })
            {
                var payload = new byte[length];
                for (var i = 0; i < length; i++)
                    payload[i] = (byte)(i % 251);

                var frame = Read(WebSocketFrame.Encode(WebSocketOpcode.Binary, payload, Bytes(9, 8, 7, 6)));

                Assert.AreEqual(length, frame.Payload.Length, "Length " + length);
                CollectionAssert.AreEqual(payload, frame.Payload, "Length " + length);
            }
        }

        [Test]
        public void TheFrameLength_IsWhatTheFrameOccupied()
        {
            var first = WebSocketFrame.EncodeText("one");
            var second = WebSocketFrame.EncodeText("two");
            var buffer = new byte[first.Length + second.Length];
            System.Array.Copy(first, buffer, first.Length);
            System.Array.Copy(second, 0, buffer, first.Length, second.Length);

            WebSocketFrameInfo frame;
            WebSocketFrame.TryRead(buffer, 0, buffer.Length, Max, out frame);

            Assert.AreEqual(first.Length, frame.FrameLength);
            Assert.AreEqual("one", frame.Text);
        }

        [Test]
        public void APartialFrame_IsIncomplete()
        {
            var frame = WebSocketFrame.EncodeText("Hello");

            for (var count = 0; count < frame.Length; count++)
            {
                WebSocketFrameInfo read;
                Assert.AreEqual(WebSocketFrameStatus.Incomplete,
                    WebSocketFrame.TryRead(frame, 0, count, Max, out read), "After " + count + " bytes");
            }
        }

        //The extended length headers are themselves read in pieces off the socket.
        [Test]
        public void APartialLengthHeader_IsIncomplete()
        {
            Assert.AreEqual(WebSocketFrameStatus.Incomplete, StatusOf(Bytes(0x82, 0x7E, 0x01)));
            Assert.AreEqual(WebSocketFrameStatus.Incomplete, StatusOf(Bytes(0x82, 0x7F, 0, 0, 0)));
        }

        [Test]
        public void AReservedBit_IsInvalid()
        {
            //RSV1 set is what a negotiated permessage-deflate would look like, and none was negotiated.
            Assert.AreEqual(WebSocketFrameStatus.Invalid, StatusOf(Bytes(0xC1, 0x00)));
        }

        [Test]
        public void AnUnknownOpcode_IsInvalid()
        {
            Assert.AreEqual(WebSocketFrameStatus.Invalid, StatusOf(Bytes(0x83, 0x00)));
        }

        [Test]
        public void AnOversizedControlFrame_IsInvalid()
        {
            Assert.AreEqual(WebSocketFrameStatus.Invalid,
                StatusOf(WebSocketFrame.Encode(WebSocketOpcode.Ping, new byte[126])));
        }

        [Test]
        public void AFragmentedControlFrame_IsInvalid()
        {
            var ping = WebSocketFrame.Encode(WebSocketOpcode.Ping, new byte[0]);
            ping[0] &= 0x7F;

            Assert.AreEqual(WebSocketFrameStatus.Invalid, StatusOf(ping));
        }

        [Test]
        public void APayloadOverTheCap_IsInvalidBeforeItIsAllocated()
        {
            //Only the ten header bytes are present: a 4GB length must be refused on the header alone.
            Assert.AreEqual(WebSocketFrameStatus.Invalid,
                StatusOf(Bytes(0x82, 0x7F, 0, 0, 0, 0x01, 0, 0, 0, 0)));
        }

        [Test]
        public void ALengthWithItsTopBitSet_IsInvalid()
        {
            Assert.AreEqual(WebSocketFrameStatus.Invalid,
                StatusOf(Bytes(0x82, 0x7F, 0x80, 0, 0, 0, 0, 0, 0, 0)));
        }

        [Test]
        public void AFragment_KeepsItsOpcodeAndClearsFin()
        {
            var frame = WebSocketFrame.EncodeText("half");
            frame[0] &= 0x7F;

            var read = Read(frame);
            Assert.IsFalse(read.Fin);
            Assert.AreEqual(WebSocketOpcode.Text, read.Opcode);
        }

        #endregion

        #region Control frames

        [Test]
        public void APing_RoundTripsItsPayload()
        {
            var frame = Read(WebSocketFrame.Encode(WebSocketOpcode.Ping, Encoding.UTF8.GetBytes("Hello")));

            Assert.AreEqual(WebSocketOpcode.Ping, frame.Opcode);
            Assert.AreEqual("Hello", frame.Text);
            Assert.IsTrue(WebSocketFrame.IsControl(frame.Opcode));
        }

        [Test]
        public void ACloseFrame_CarriesItsCodeAndReason()
        {
            var frame = Read(WebSocketFrame.EncodeClose(1002, "protocol"));

            Assert.AreEqual(WebSocketOpcode.Close, frame.Opcode);
            Assert.AreEqual(1002, WebSocketFrame.CloseCode(frame.Payload));
            Assert.AreEqual("protocol", Encoding.UTF8.GetString(frame.Payload, 2, frame.Payload.Length - 2));
        }

        [Test]
        public void ACloseFrame_WithNoPayload_ReadsAsNoStatus()
        {
            Assert.AreEqual(1005, WebSocketFrame.CloseCode(new byte[0]));
            Assert.AreEqual(1005, WebSocketFrame.CloseCode(null));
        }

        [Test]
        public void TextAndBinary_AreNotControlFrames()
        {
            Assert.IsFalse(WebSocketFrame.IsControl(WebSocketOpcode.Text));
            Assert.IsFalse(WebSocketFrame.IsControl(WebSocketOpcode.Continuation));
        }

        #endregion
    }
}
