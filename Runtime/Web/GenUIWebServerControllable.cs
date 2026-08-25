using UnityEngine;

public class GenUIWebServerControllable : Controllable
{
    [OCFProperty]
    public bool enableWebServer;

    [OCFProperty]
    public int port;

    //Replaces Controllable's reflection-based poll, which boxes every exposed value every frame.
    protected override void PollTargetScript()
    {
        var target = controllableTargetScript as GenUIWebServer;
        if (target == null) return;

        if (enableWebServer != target.enableWebServer) { enableWebServer = target.enableWebServer; RaiseScriptValueChanged("enableWebServer"); }
        if (port != target.port) { port = target.port; RaiseScriptValueChanged("port"); }
    }

}
