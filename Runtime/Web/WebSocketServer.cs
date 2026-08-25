using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// The transport behind the web mirror: one <see cref="TcpListener"/> serving both the client files
/// over plain HTTP and the WebSocket the browser then drives the panel with.
/// </summary>
/// <remarks>
/// Hand-rolled because Unity never implemented <c>HttpListener</c>'s WebSocket support and the .NET
/// server libraries are NuGet binaries that are untested under IL2CPP.
/// Threading follows UnityOSC's <c>OSCServer</c>: background threads, a volatile running flag, and a
/// shutdown that closes the socket to unblock the blocking read before joining. Nothing here touches
/// Unity's API beyond <c>Debug</c> - everything a message causes happens on the main thread, off
/// <see cref="Inbound"/>.
/// </remarks>
public class WebSocketServer
{
    /// <summary>
    /// The largest frame a browser may send. Values and method presses are tiny; anything approaching
    /// this is a mistake or an attack, and is answered by closing the connection.
    /// </summary>
    public const int MaxPayloadBytes = 1 << 20;

    //A handshake that never completes must not hold a thread and a socket forever.
    const int HandshakeTimeoutMs = 5000;
    const int MaxRequestHeadBytes = 8192;
    const int ReadBufferSize = 8192;
    const int ThreadJoinMs = 200;

    class Client
    {
        public int Id;
        public TcpClient Tcp;
        public Stream Stream;
        public Thread Thread;
        public volatile bool IsWebSocket;

        //One frame at a time on the wire: the main thread sends while the read thread answers a ping.
        public readonly object SendLock = new object();

        public void Close()
        {
            try
            {
                Tcp.Close();
            }
            catch (Exception)
            {
                //Already torn down by the other end or by Stop().
            }
        }
    }

    readonly int _requestedPort;
    readonly WebMessageQueue _inbound = new WebMessageQueue();
    readonly Dictionary<int, Client> _clients = new Dictionary<int, Client>();

    TcpListener _listener;
    Thread _acceptThread;
    volatile bool _running;
    int _nextClientId;

    /// <summary>
    /// Answers a plain <c>GET</c> for the given path. Null, or a null return, is a 404.
    /// </summary>
    public Func<string, WebHttpResponse?> HttpHandler;

    /// <summary>What the browsers said, drained on the main thread.</summary>
    public WebMessageQueue Inbound { get { return _inbound; } }

    public bool IsRunning { get { return _running; } }

    /// <summary>The port actually bound, which is what a caller passing 0 asked to be told.</summary>
    public int Port
    {
        get
        {
            var listener = _listener;
            return listener != null ? ((IPEndPoint)listener.LocalEndpoint).Port : _requestedPort;
        }
    }

    public int ClientCount
    {
        get
        {
            lock (_clients)
            {
                return _clients.Count;
            }
        }
    }

    public WebSocketServer(int port)
    {
        _requestedPort = port;
    }

    #region Lifetime

    /// <summary>
    /// Binds the port and starts accepting, or logs why it could not and answers false.
    /// </summary>
    public bool Start()
    {
        if (_running)
            return true;

        try
        {
            //All interfaces, so a phone on the LAN can reach it - see the security note in the README.
            _listener = new TcpListener(IPAddress.Any, _requestedPort);
            _listener.Start();
        }
        catch (Exception e)
        {
            Debug.LogError("[GenUI] Web server could not listen on port " + _requestedPort + " | " + e.Message);
            _listener = null;
            return false;
        }

        _running = true;
        _acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "GenUI web accept" };
        _acceptThread.Start();

        return true;
    }

    /// <summary>
    /// Stops accepting, closes every connection and joins the threads.
    /// </summary>
    public void Stop()
    {
        if (!_running && _listener == null)
            return;

        _running = false;

        //Closing the listener is what unblocks AcceptTcpClient; closing a socket unblocks its reader.
        if (_listener != null)
        {
            try
            {
                _listener.Stop();
            }
            catch (Exception)
            {
                //Nothing left to stop.
            }

            _listener = null;
        }

        Client[] clients;
        lock (_clients)
        {
            clients = new List<Client>(_clients.Values).ToArray();
            _clients.Clear();
        }

        foreach (var client in clients)
        {
            client.Close();
            if (client.Thread != null)
                client.Thread.Join(ThreadJoinMs);
        }

        if (_acceptThread != null)
        {
            _acceptThread.Join(ThreadJoinMs);
            _acceptThread = null;
        }

        _inbound.Clear();
    }

    #endregion

    #region Sending

    /// <summary>Sends one text frame to a connected browser, if it is still there.</summary>
    public void Send(int clientId, string text)
    {
        Client client;
        lock (_clients)
        {
            if (!_clients.TryGetValue(clientId, out client))
                return;
        }

        SendFrame(client, WebSocketFrame.EncodeText(text));
    }

    /// <summary>
    /// Sends one text frame to every connected browser, encoding it once.
    /// </summary>
    public void Broadcast(string text)
    {
        Client[] clients;
        lock (_clients)
        {
            if (_clients.Count == 0)
                return;

            clients = new List<Client>(_clients.Values).ToArray();
        }

        var frame = WebSocketFrame.EncodeText(text);
        foreach (var client in clients)
            SendFrame(client, frame);
    }

    void SendFrame(Client client, byte[] frame)
    {
        if (!client.IsWebSocket)
            return;

        try
        {
            lock (client.SendLock)
            {
                client.Stream.Write(frame, 0, frame.Length);
                client.Stream.Flush();
            }
        }
        catch (Exception)
        {
            //The browser went away mid-write; its read thread reports the disconnection.
            client.Close();
        }
    }

    #endregion

    #region Accepting

    void AcceptLoop()
    {
        while (_running)
        {
            try
            {
                var tcp = _listener.AcceptTcpClient();
                //Values are small and frequent, so latency beats coalescing them into fewer packets.
                tcp.NoDelay = true;

                var client = new Client
                {
                    Id = Interlocked.Increment(ref _nextClientId),
                    Tcp = tcp,
                    Stream = tcp.GetStream()
                };

                client.Thread = new Thread(() => ServeClient(client))
                {
                    IsBackground = true,
                    Name = "GenUI web client"
                };

                lock (_clients)
                {
                    _clients.Add(client.Id, client);
                }

                client.Thread.Start();
            }
            catch (Exception e)
            {
                //Listener disposed by Stop(), or a transient accept failure.
                if (!_running)
                    break;

                Debug.LogWarning("[GenUI] Web server accept error: " + e.Message);
            }
        }
    }

    void ServeClient(Client client)
    {
        try
        {
            client.Stream.ReadTimeout = HandshakeTimeoutMs;

            string request;
            if (!TryReadRequestHead(client.Stream, out request))
                return;

            if (WebSocketFrame.IsUpgradeRequest(request))
            {
                var response = Encoding.ASCII.GetBytes(
                    WebSocketFrame.HandshakeResponse(WebSocketFrame.Header(request, "Sec-WebSocket-Key")));
                client.Stream.Write(response, 0, response.Length);
                client.Stream.Flush();

                //A browser can sit idle for as long as the user leaves the tab open.
                client.Stream.ReadTimeout = Timeout.Infinite;
                client.IsWebSocket = true;
                _inbound.Enqueue(new WebMessage { Kind = WebMessageKind.Connected, ClientId = client.Id });

                ReadLoop(client);
                return;
            }

            var path = WebSocketFrame.RequestPath(request);
            var served = path != null && HttpHandler != null ? HttpHandler(path) : null;
            var bytes = (served ?? WebHttpResponse.NotFound()).ToBytes();
            client.Stream.Write(bytes, 0, bytes.Length);
            client.Stream.Flush();
        }
        catch (Exception e)
        {
            //A closed tab and a stopped server both surface as a failed read on a dead socket, which is
            //the ordinary end of a connection rather than something to report.
            if (_running && !IsDisconnection(e))
                Debug.LogWarning("[GenUI] Web client error: " + e.Message);
        }
        finally
        {
            var wasWebSocket = client.IsWebSocket;
            client.IsWebSocket = false;
            client.Close();

            lock (_clients)
            {
                _clients.Remove(client.Id);
            }

            if (wasWebSocket)
                _inbound.Enqueue(new WebMessage { Kind = WebMessageKind.Disconnected, ClientId = client.Id });
        }
    }

    static bool IsDisconnection(Exception e)
    {
        return e is IOException || e is SocketException || e is ObjectDisposedException;
    }

    /// <summary>
    /// Reads an HTTP request head, byte by byte until the blank line that ends it.
    /// </summary>
    /// <remarks>
    /// A request head is a few hundred bytes, and reading no further is what leaves the stream
    /// positioned exactly at the first WebSocket frame when the head turns out to be an upgrade.
    /// </remarks>
    static bool TryReadRequestHead(Stream stream, out string request)
    {
        var bytes = new List<byte>(512);
        var one = new byte[1];

        while (bytes.Count < MaxRequestHeadBytes)
        {
            if (stream.Read(one, 0, 1) <= 0)
                break;

            bytes.Add(one[0]);

            var end = bytes.Count;
            if (end >= 4 && bytes[end - 4] == '\r' && bytes[end - 3] == '\n'
                && bytes[end - 2] == '\r' && bytes[end - 1] == '\n')
            {
                request = Encoding.ASCII.GetString(bytes.ToArray());
                return true;
            }
        }

        request = null;
        return false;
    }

    #endregion

    #region Reading frames

    void ReadLoop(Client client)
    {
        var buffer = new byte[ReadBufferSize];
        var pending = 0;

        //A browser may split a message across frames; the parts are joined before anything is queued.
        var fragments = new List<byte>();
        var fragmentOpcode = WebSocketOpcode.Continuation;

        while (_running && client.IsWebSocket)
        {
            if (pending == buffer.Length)
                Array.Resize(ref buffer, buffer.Length * 2);

            var read = client.Stream.Read(buffer, pending, buffer.Length - pending);
            if (read <= 0)
                return;

            pending += read;

            while (true)
            {
                WebSocketFrameInfo frame;
                var status = WebSocketFrame.TryRead(buffer, 0, pending, MaxPayloadBytes, out frame);

                if (status == WebSocketFrameStatus.Incomplete)
                    break;

                if (status == WebSocketFrameStatus.Invalid)
                {
                    //1002: protocol error. Nothing sensible can follow a frame that made no sense.
                    SendFrame(client, WebSocketFrame.EncodeClose(1002));
                    return;
                }

                Array.Copy(buffer, frame.FrameLength, buffer, 0, pending - frame.FrameLength);
                pending -= frame.FrameLength;

                if (!HandleFrame(client, frame, fragments, ref fragmentOpcode))
                    return;
            }
        }
    }

    /// <summary>
    /// Acts on one decoded frame; false when the connection is finished with.
    /// </summary>
    bool HandleFrame(Client client, WebSocketFrameInfo frame, List<byte> fragments, ref WebSocketOpcode fragmentOpcode)
    {
        switch (frame.Opcode)
        {
            case WebSocketOpcode.Close:
                //Echoing the code back is the close handshake; the socket closes in the finally block.
                SendFrame(client, WebSocketFrame.EncodeClose(WebSocketFrame.CloseCode(frame.Payload)));
                return false;

            case WebSocketOpcode.Ping:
                SendFrame(client, WebSocketFrame.Encode(WebSocketOpcode.Pong, frame.Payload));
                return true;

            case WebSocketOpcode.Pong:
                return true;
        }

        if (frame.Opcode != WebSocketOpcode.Continuation)
        {
            fragments.Clear();
            fragmentOpcode = frame.Opcode;
        }

        fragments.AddRange(frame.Payload);

        if (fragments.Count > MaxPayloadBytes)
        {
            //1009: message too big. The cap covers a message assembled from many legal frames too.
            SendFrame(client, WebSocketFrame.EncodeClose(1009));
            return false;
        }

        if (!frame.Fin)
            return true;

        //Binary frames are decoded but carry nothing this protocol defines, so they are dropped.
        if (fragmentOpcode == WebSocketOpcode.Text)
        {
            _inbound.Enqueue(new WebMessage
            {
                Kind = WebMessageKind.Text,
                ClientId = client.Id,
                Text = Encoding.UTF8.GetString(fragments.ToArray())
            });
        }

        fragments.Clear();
        return true;
    }

    #endregion
}
