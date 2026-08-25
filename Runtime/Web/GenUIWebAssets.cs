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

    /// <summary>The asset, loaded from Resources on first use.</summary>
    public static GenUIWebAssets Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GenUIWebAssets>(ResourcePath);

                if (_instance == null)
                {
                    Debug.LogError("[GenUI] Could not load the GenUIWebAssets asset from Resources. The web mirror will serve nothing.");

                    //An empty stand-in rather than null: every path then answers 404, which is what a
                    //browser can report, instead of a null reference on a socket thread.
                    _instance = CreateInstance<GenUIWebAssets>();
                }
            }

            return _instance;
        }
    }

    /// <summary>
    /// The client file a plain GET for <paramref name="path"/> is answered with, or null for a 404.
    /// </summary>
    /// <remarks>
    /// Given to <c>WebSocketServer.HttpHandler</c>, so it runs on a socket thread: it reads the
    /// already-loaded TextAssets and touches nothing else of Unity's.
    /// </remarks>
    public static WebHttpResponse? ResponseFor(string path)
    {
        var assets = Instance;

        switch (Normalize(path))
        {
            case "/": return Serve(assets.Html, "text/html");
            case "/client.css": return Serve(assets.Css, "text/css");
            case "/client.js": return Serve(assets.Script, "application/javascript");
        }

        return null;
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

    static WebHttpResponse? Serve(TextAsset asset, string contentType)
    {
        return asset == null ? (WebHttpResponse?)null : WebHttpResponse.Ok(contentType, asset.text);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
    }
}
