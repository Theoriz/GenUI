using System;
using System.Text;

/// <summary>
/// A plain HTTP response - what the same port answers a browser asking for the client files rather
/// than for a WebSocket.
/// </summary>
/// <remarks>
/// One request, one response, connection closed: there is no keep-alive and no routing beyond the
/// handler the server is given.
/// </remarks>
public struct WebHttpResponse
{
    public int StatusCode;
    public string ContentType;
    public byte[] Body;

    public static WebHttpResponse Ok(string contentType, byte[] body)
    {
        return new WebHttpResponse { StatusCode = 200, ContentType = contentType, Body = body };
    }

    public static WebHttpResponse Ok(string contentType, string body)
    {
        return Ok(contentType + "; charset=utf-8", Encoding.UTF8.GetBytes(body));
    }

    public static WebHttpResponse NotFound()
    {
        return new WebHttpResponse
        {
            StatusCode = 404,
            ContentType = "text/plain; charset=utf-8",
            Body = Encoding.UTF8.GetBytes("Not found")
        };
    }

    /// <summary>The response as it goes on the wire, head and body in one buffer.</summary>
    public byte[] ToBytes()
    {
        var body = Body ?? new byte[0];
        var head = new StringBuilder();

        head.Append("HTTP/1.1 ").Append(StatusCode).Append(' ').Append(ReasonPhrase(StatusCode)).Append("\r\n");
        head.Append("Content-Type: ").Append(string.IsNullOrEmpty(ContentType) ? "application/octet-stream" : ContentType).Append("\r\n");
        head.Append("Content-Length: ").Append(body.Length).Append("\r\n");
        //The client files are edited while the page is open, and a cached copy would show yesterday's UI.
        head.Append("Cache-Control: no-store\r\n");
        head.Append("Connection: close\r\n\r\n");

        var headBytes = Encoding.ASCII.GetBytes(head.ToString());
        var response = new byte[headBytes.Length + body.Length];
        Array.Copy(headBytes, response, headBytes.Length);
        Array.Copy(body, 0, response, headBytes.Length, body.Length);

        return response;
    }

    static string ReasonPhrase(int statusCode)
    {
        switch (statusCode)
        {
            case 200: return "OK";
            case 404: return "Not Found";
            case 500: return "Internal Server Error";
            default: return "OK";
        }
    }
}
