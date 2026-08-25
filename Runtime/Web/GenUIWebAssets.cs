using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The browser client's files, and the plain-HTTP answers they are served as.
/// </summary>
/// <remarks>
/// Serialized TextAsset references rather than a name lookup per request: a serialized reference is
/// what guarantees the files reach a player build, the same reason <see cref="GenUIAssets"/> holds the
/// sprites. The .txt extensions are Unity's doing - it does not import .html or .js as a TextAsset.
/// </remarks>
[CreateAssetMenu(fileName = "GenUIWebAssets", menuName = "Theoriz/GenUI/Web Assets")]
public class GenUIWebAssets : ScriptableObject
{
    public const string ResourcePath = "GenUIWebAssets";

    public TextAsset Html;
    public TextAsset Css;
    public TextAsset Script;

    private static GenUIWebAssets _instance;

    //The finished responses, built once on the main thread: bytes, holding no Unity object.
    private static Dictionary<string, WebHttpResponse> _responses;

    /// <summary>The asset, or null until <see cref="Preload"/> has run.</summary>
    public static GenUIWebAssets Instance
    {
        get { return _instance; }
    }

    /// <summary>
    /// Loads the client files and turns them into the responses they are served as. Must be called
    /// from the main thread, before the server starts serving.
    /// </summary>
    /// <remarks>
    /// Every Unity asset access happens here, and none in <see cref="ResponseFor"/>: that one runs on
    /// a socket thread, where loading an asset - Resources.Load, or a TextAsset resolved through a
    /// serialized reference - throws "Load can only be called from the main thread" and serves nothing.
    /// </remarks>
    public static void Preload()
    {
        if (_responses != null)
            return;

        _responses = new Dictionary<string, WebHttpResponse>();
        _instance = Resources.Load<GenUIWebAssets>(ResourcePath);

        if (_instance == null)
        {
            //Every path then answers 404, which is what a browser can report.
            Debug.LogError("[GenUI] Could not load the GenUIWebAssets asset from Resources. The web mirror will serve nothing.");
            return;
        }

        Add("/", _instance.Html, "text/html");
        Add("/client.css", _instance.Css, "text/css");
        Add("/client.js", _instance.Script, "application/javascript");
    }

    static void Add(string path, TextAsset asset, string contentType)
    {
        if (asset == null)
        {
            Debug.LogError("[GenUI] The GenUIWebAssets asset is missing the file served at '" + path + "'.");
            return;
        }

        _responses[path] = WebHttpResponse.Ok(contentType, asset.text);
    }

    /// <summary>
    /// The client file a plain GET for <paramref name="path"/> is answered with, or null for a 404.
    /// </summary>
    /// <remarks>
    /// Given to <c>WebSocketServer.HttpHandler</c>, so it runs on a socket thread: it hands back the
    /// bytes <see cref="Preload"/> built and touches nothing of Unity's.
    /// </remarks>
    public static WebHttpResponse? ResponseFor(string path)
    {
        var responses = _responses;
        if (responses == null)
            return null;

        WebHttpResponse response;
        return responses.TryGetValue(Normalize(path), out response) ? response : (WebHttpResponse?)null;
    }

    /// <summary>The path a request is routed by: no query string, and the page under its own name too.</summary>
    static string Normalize(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "/";

        var query = path.IndexOf('?');
        if (query >= 0)
            path = path.Substring(0, query);

        return path == "/index.html" ? "/" : path;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        _responses = null;
    }
}
