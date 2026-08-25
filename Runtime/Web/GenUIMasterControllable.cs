using UnityEngine;

/// <summary>
/// The mirror behind the GenUI panel: OCF's own settings, plus the web server's two options.
/// </summary>
/// <remarks>
/// A <see cref="Controllable"/> mirrors one target script, and these two members live on a second one
/// (<see cref="GenUIWebServer"/>). They are declared <c>selfBound</c> so OCF does not look for them on
/// <c>controllableTargetScript</c>, and both directions are wired here: the write-through in
/// <see cref="OnUiValueChanged"/>, the read-back in <see cref="PollTargetScript"/>.
///
/// This exists so the web server's options sit in the GenUI panel rather than in a panel of their own.
/// It is deliberately a one-off for this panel: GenUI has no general mechanism for hosting one
/// controllable's rows inside another's.
/// </remarks>
[RequireComponent(typeof(GenUIWebServer))]
public class GenUIMasterControllable : ControllableMasterControllable
{
    [Header("Web Server")]

    [OCFProperty(selfBound = true)]
    public bool enableWebServer;

    [OCFProperty(selfBound = true)]
    public int webServerPort;

    private GenUIWebServer _webServer;

    private GenUIWebServer WebServer
    {
        get
        {
            if (_webServer == null)
                _webServer = GetComponent<GenUIWebServer>();

            return _webServer;
        }
    }

    #region MonoBehaviour

    public override void Awake()
    {
        base.Awake();

        //Seeded here rather than left to the first poll: the panel is built when this registers in
        //OnEnable, before any Update, so the widgets would otherwise draw the serialized value once.
        if (WebServer != null)
        {
            enableWebServer = WebServer.enableWebServer;
            webServerPort = WebServer.port;
        }
    }

    #endregion

    #region Web server members

    public override void OnUiValueChanged(string name)
    {
        //Every write route - UI, OSC, preset, the web mirror itself - ends here, so this is the one
        //place the value has to reach the second script.
        if (WebServer != null)
        {
            if (name == nameof(enableWebServer))
            {
                WebServer.enableWebServer = enableWebServer;
                return;
            }

            if (name == nameof(webServerPort))
            {
                WebServer.port = webServerPort;
                return;
            }
        }

        base.OnUiValueChanged(name);
    }

    protected override void PollTargetScript()
    {
        base.PollTargetScript();

        if (WebServer == null) return;

        //RaiseEventValueChanged rather than RaiseScriptValueChanged: the latter goes through
        //OnScriptValueChanged, which re-reads the member from controllableTargetScript and bails on
        //one that is not there - so the widgets would never refresh.
        if (enableWebServer != WebServer.enableWebServer)
        {
            enableWebServer = WebServer.enableWebServer;
            RaiseEventValueChanged(nameof(enableWebServer));
        }

        if (webServerPort != WebServer.port)
        {
            webServerPort = WebServer.port;
            RaiseEventValueChanged(nameof(webServerPort));
        }
    }

    #endregion
}
