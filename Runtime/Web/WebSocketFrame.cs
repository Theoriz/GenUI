using System;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// The opcodes this server understands. Binary is decoded but never sent: the protocol is JSON text.
/// </summary>
public enum WebSocketOpcode
{
    Continuation = 0x0,
    Text = 0x1,
    Binary = 0x2,
    Close = 0x8,
    Ping = 0x9,
    Pong = 0xA
}

/// <summary>What a buffer held when a frame was read out of it.</summary>
public enum WebSocketFrameStatus
{
    /// <summary>A whole frame was read; <c>FrameLength</c> says how many bytes it took.</summary>
    Complete,
    /// <summary>The frame is not all there yet - read more from the socket and ask again.</summary>
    Incomplete,
    /// <summary>The bytes are not a frame this server accepts; the connection must be closed.</summary>
    Invalid
}

/// <summary>One decoded frame, its payload already unmasked.</summary>
public struct WebSocketFrameInfo
{
    public bool Fin;
    public WebSocketOpcode Opcode;
    public byte[] Payload;

    /// <summary>Bytes the frame occupied in the buffer it was read from, header included.</summary>
    public int FrameLength;

    public string Text { get { return Encoding.UTF8.GetString(Payload); } }
}

/// <summary>
/// RFC 6455 handshake and frame codec, as pure functions over byte arrays.
/// </summary>
/// <remarks>
/// Deliberately a subset: no extensions are negotiated (so the reserved bits must be clear and no
/// payload is ever compressed) and this side never fragments what it sends. Incoming fragments are
/// decoded frame by frame and reassembled by the caller.
/// Nothing here touches a socket, which is where the protocol bugs would otherwise be untestable.
/// </remarks>
public static class WebSocketFrame
{
    #region Handshake

    /// <summary>The constant every client key is concatenated with before hashing (RFC 6455 1.3).</summary>
    public const string HandshakeGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

    /// <summary>
    /// The <c>Sec-WebSocket-Accept</c> value proving to the client that this is a WebSocket server.
    /// </summary>
    public static string AcceptKey(string clientKey)
    {
        using (var sha1 = SHA1.Create())
            return Convert.ToBase64String(sha1.ComputeHash(Encoding.ASCII.GetBytes(clientKey + HandshakeGuid)));
    }

    /// <summary>
    /// The path of an HTTP request head, or null when the head is not a <c>GET</c> this server serves.
    /// </summary>
    /// <remarks>A query string is kept out of the path, so "/x?v=1" answers as "/x".</remarks>
    public static string RequestPath(string request)
    {
        if (string.IsNullOrEmpty(request))
            return null;

        var line = request.Split('\n')[0].Trim();
        var parts = line.Split(' ');
        if (parts.Length < 2 || parts[0] != "GET")
            return null;

        var path = parts[1];
        var query = path.IndexOf('?');
        return query >= 0 ? path.Substring(0, query) : path;
    }

    /// <summary>
    /// The value of a header in an HTTP request head, or null when it is absent.
    /// </summary>
    public static string Header(string request, string name)
    {
        if (string.IsNullOrEmpty(request))
            return null;

        foreach (var line in request.Split('\n'))
        {
            var separator = line.IndexOf(':');
            if (separator < 0)
                continue;

            //Header names are case-insensitive, and browsers do not agree on the casing they send.
            if (string.Equals(line.Substring(0, separator).Trim(), name, StringComparison.OrdinalIgnoreCase))
                return line.Substring(separator + 1).Trim();
        }

        return null;
    }

    /// <summary>
    /// True when the request asks to be upgraded to a WebSocket rather than served a file.
    /// </summary>
    /// <remarks>
    /// The Connection header can list several tokens ("keep-alive, Upgrade"), so it is searched, not
    /// compared.
    /// </remarks>
    public static bool IsUpgradeRequest(string request)
    {
        var upgrade = Header(request, "Upgrade");
        var connection = Header(request, "Connection");

        return upgrade != null && upgrade.IndexOf("websocket", StringComparison.OrdinalIgnoreCase) >= 0
            && connection != null && connection.IndexOf("upgrade", StringComparison.OrdinalIgnoreCase) >= 0
            && Header(request, "Sec-WebSocket-Key") != null;
    }

    /// <summary>
    /// The 101 response completing the handshake for <paramref name="clientKey"/>.
    /// </summary>
    public static string HandshakeResponse(string clientKey)
    {
        return "HTTP/1.1 101 Switching Protocols\r\n"
            + "Upgrade: websocket\r\n"
            + "Connection: Upgrade\r\n"
            + "Sec-WebSocket-Accept: " + AcceptKey(clientKey) + "\r\n\r\n";
    }

    #endregion

    #region Encoding

    /// <summary>A text frame, unmasked, unfragmented - what this server sends.</summary>
    public static byte[] EncodeText(string text)
    {
        return Encode(WebSocketOpcode.Text, Encoding.UTF8.GetBytes(text));
    }

    /// <summary>
    /// One whole frame carrying <paramref name="payload"/>.
    /// </summary>
    /// <param name="mask">
    /// Null for a server frame, which RFC 6455 requires to be unmasked. A four-byte key produces the
    /// masked form a client sends, which is what lets the codec be round-tripped without a socket.
    /// </param>
    public static byte[] Encode(WebSocketOpcode opcode, byte[] payload, byte[] mask = null)
    {
        if (payload == null)
            payload = new byte[0];
        if (mask != null && mask.Length != 4)
            throw new ArgumentException("A WebSocket mask key is four bytes.", "mask");

        var length = payload.Length;
        var lengthBytes = length <= 125 ? 0 : length <= ushort.MaxValue ? 2 : 8;
        var frame = new byte[2 + lengthBytes + (mask != null ? 4 : 0) + length];

        frame[0] = (byte)(0x80 | (byte)opcode);
        frame[1] = (byte)((mask != null ? 0x80 : 0x00) | (lengthBytes == 0 ? length : lengthBytes == 2 ? 126 : 127));

        var index = 2;
        for (var i = lengthBytes - 1; i >= 0; i--)
            frame[index++] = (byte)((long)length >> (8 * i));

        if (mask != null)
        {
            Array.Copy(mask, 0, frame, index, 4);
            index += 4;
            for (var i = 0; i < length; i++)
                frame[index + i] = (byte)(payload[i] ^ mask[i % 4]);
        }
        else
        {
            Array.Copy(payload, 0, frame, index, length);
        }

        return frame;
    }

    /// <summary>A close frame carrying a status code, and a reason when one is given.</summary>
    public static byte[] EncodeClose(ushort code, string reason = null)
    {
        var reasonBytes = string.IsNullOrEmpty(reason) ? new byte[0] : Encoding.UTF8.GetBytes(reason);
        var payload = new byte[2 + reasonBytes.Length];
        payload[0] = (byte)(code >> 8);
        payload[1] = (byte)code;
        Array.Copy(reasonBytes, 0, payload, 2, reasonBytes.Length);

        return Encode(WebSocketOpcode.Close, payload);
    }

    /// <summary>
    /// The status code in a close frame's payload, or 1005 ("no status") when it carries none.
    /// </summary>
    public static ushort CloseCode(byte[] payload)
    {
        if (payload == null || payload.Length < 2)
            return 1005;

        return (ushort)((payload[0] << 8) | payload[1]);
    }

    #endregion

    #region Decoding

    /// <summary>
    /// Reads the first frame out of <paramref name="buffer"/>.
    /// </summary>
    /// <remarks>
    /// The mask bit is honoured whichever side set it, so a server frame decodes too - the transport
    /// only ever hands this client frames, but the RFC's examples are of both kinds.
    /// <paramref name="maxPayloadBytes"/> is what stops a hostile length header from asking for a
    /// gigabyte allocation before a single payload byte has arrived.
    /// </remarks>
    public static WebSocketFrameStatus TryRead(byte[] buffer, int offset, int count, int maxPayloadBytes,
        out WebSocketFrameInfo frame)
    {
        frame = default(WebSocketFrameInfo);

        if (count < 2)
            return WebSocketFrameStatus.Incomplete;

        var first = buffer[offset];
        var second = buffer[offset + 1];

        //No extension was negotiated, so RSV1-3 must be clear.
        if ((first & 0x70) != 0)
            return WebSocketFrameStatus.Invalid;

        var opcode = (WebSocketOpcode)(first & 0x0F);
        if (!IsKnownOpcode(opcode))
            return WebSocketFrameStatus.Invalid;

        var fin = (first & 0x80) != 0;
        var masked = (second & 0x80) != 0;
        long length = second & 0x7F;
        var index = offset + 2;

        if (length == 126)
        {
            if (count < 4)
                return WebSocketFrameStatus.Incomplete;

            length = (buffer[index] << 8) | buffer[index + 1];
            index += 2;
        }
        else if (length == 127)
        {
            if (count < 10)
                return WebSocketFrameStatus.Incomplete;

            length = 0;
            for (var i = 0; i < 8; i++)
                length = (length << 8) | buffer[index + i];

            //The RFC reserves the top bit of the 64-bit form; set, it has overflowed into the sign.
            if (length < 0)
                return WebSocketFrameStatus.Invalid;

            index += 8;
        }

        //A control frame carries at most 125 bytes and is never fragmented (RFC 6455 5.5).
        if (IsControl(opcode) && (length > 125 || !fin))
            return WebSocketFrameStatus.Invalid;

        if (length > maxPayloadBytes)
            return WebSocketFrameStatus.Invalid;

        var headerLength = index - offset + (masked ? 4 : 0);
        if (count < headerLength + length)
            return WebSocketFrameStatus.Incomplete;

        var payload = new byte[length];
        if (masked)
        {
            var mask = index;
            index += 4;
            for (var i = 0; i < length; i++)
                payload[i] = (byte)(buffer[index + i] ^ buffer[mask + (i % 4)]);
        }
        else
        {
            Array.Copy(buffer, index, payload, 0, (int)length);
        }

        frame = new WebSocketFrameInfo
        {
            Fin = fin,
            Opcode = opcode,
            Payload = payload,
            FrameLength = headerLength + (int)length
        };

        return WebSocketFrameStatus.Complete;
    }

    public static bool IsControl(WebSocketOpcode opcode)
    {
        return ((byte)opcode & 0x08) != 0;
    }

    static bool IsKnownOpcode(WebSocketOpcode opcode)
    {
        return opcode == WebSocketOpcode.Continuation || opcode == WebSocketOpcode.Text
            || opcode == WebSocketOpcode.Binary || opcode == WebSocketOpcode.Close
            || opcode == WebSocketOpcode.Ping || opcode == WebSocketOpcode.Pong;
    }

    #endregion
}
