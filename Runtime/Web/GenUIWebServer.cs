using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

/// <summary>
/// Serves the panel to a browser on the local network, when it is switched on.
/// </summary>
/// <remarks>
/// Everything a browser causes happens here, on the main thread: the socket threads only fill
/// <c>WebSocketServer.Inbound</c>, which <see cref="Update"/> drains. Inbound values go through
/// <c>ControllableMaster.UpdateValue</c>, the entry point OSC uses, so clamping, read-only refusal,
/// write-through and the change fan-out are the ones that already exist.
///
/// Outbound changes are coalesced: a member driven by its target script raises
/// <c>controllableValueChanged</c> every frame, so names are accumulated and sent once per frame
/// rather than one message per event.
///
/// No authentication and no TLS: anyone who can reach the port drives every exposed member. The
/// mitigation is this component being off unless someone switches it on.
/// </remarks>
public class GenUIWebServer : MonoBehaviour
{
    //Exposed to the panel and to OSC by GenUIMasterControllable, which mirrors them beside OCF's own
    //settings so they appear in the GenUI panel rather than in one of their own. They carry no
    //[OCFExposed]: generating a mirror for this script would produce a second, competing one.
    public bool enableWebServer = false;

    public int port = 6080;

    public bool showDebug = false;

    WebSocketServer _server;

    //The last value of enableWebServer that was acted on. Comparing against it - rather than against
    //_server being null - is what stops a start that failed from being retried every frame.
    bool _serverRequested;

    //The port the running listener was opened on, so a change to the field is noticed the same way.
    int _portRequested;

    //The delegate each controllable was subscribed with, kept so it can be unsubscribed again.
    readonly Dictionary<Controllable, Controllable.ControllableValueChangedEvent> _subscriptions
        = new Dictionary<Controllable, Controllable.ControllableValueChangedEvent>();

    //Members changed since the last send, per controllable. A HashSet is what makes a value moving
    //every frame cost one entry rather than one message.
    readonly Dictionary<Controllable, HashSet<string>> _dirty = new Dictionary<Controllable, HashSet<string>>();

    #region MonoBehaviour

    void OnEnable()
    {
        _serverRequested = enableWebServer;
        _portRequested = port;

        //The zero-cost guarantee: while the option is off, no listener, thread or subscription exists.
        if (enableWebServer)
            StartServer();
    }

    void Update()
    {
        //enableWebServer and port are plain fields, so nothing announces that either moved: this is
        //where ticking the option or editing the port at runtime - from the inspector, a script, the
        //panel or OSC - is noticed. A port change restarts the listener, which drops the connected
        //browsers: they have to be reloaded on the new port.
        if (enableWebServer != _serverRequested || (enableWebServer && port != _portRequested))
        {
            _serverRequested = enableWebServer;
            _portRequested = port;

            StopServer();

            if (enableWebServer)
                StartServer();
        }

        if (_server == null)
            return;

        DrainInbound();
        SendDirtyValues();
    }

    void OnDisable()
    {
        StopServer();
    }

    #endregion

    #region Server lifetime

    void StartServer()
    {
        _server = new WebSocketServer(port);

        //The other half of the port: a plain GET is answered with the client files, which then open
        //the WebSocket back to it. Loading the assets here and not on first request is what keeps
        //Resources.Load - main thread only - off the socket thread that serves the page.
        GenUIWebAssets.Preload();
        _server.HttpHandler = GenUIWebAssets.ResponseFor;

        if (!_server.Start())
        {
            _server = null;
            return;
        }

        ControllableMaster.controllableAdded += OnControllableAdded;
        ControllableMaster.controllableRemoved += OnControllableRemoved;

        //Controllables register from their own OnEnable, which may already have run.
        foreach (var registered in ControllableMaster.RegisteredControllables)
            Subscribe(registered.Value);

        //ControllableMaster caches the address in its Start, which may not have run yet - resolving it
        //ourselves is what keeps a real address in the URL whatever the script order.
        var address = ControllableMaster.instance != null && !string.IsNullOrEmpty(ControllableMaster.instance.IPAddress)
            ? ControllableMaster.instance.IPAddress
            : ControllableMaster.GetLocalIPAddress();
        Debug.Log("[GenUI] Web mirror listening on http://" + address + ":" + _server.Port + "/");
    }

    void StopServer()
    {
        if (_server == null)
            return;

        ControllableMaster.controllableAdded -= OnControllableAdded;
        ControllableMaster.controllableRemoved -= OnControllableRemoved;

        foreach (var controllable in new List<Controllable>(_subscriptions.Keys))
            Unsubscribe(controllable);

        _subscriptions.Clear();
        _dirty.Clear();

        _server.Stop();
        _server = null;

        Debug.Log("[GenUI] Web mirror stopped.");
    }

    #endregion

    #region Controllable registry

    void OnControllableAdded(Controllable controllable)
    {
        Subscribe(controllable);

        //The browser rebuilds its panels from a fresh schema rather than being sent a partial one.
        _server.Broadcast("{\"t\":\"added\",\"id\":" + WebJson.Quote(controllable.controllableId) + "}");
    }

    void OnControllableRemoved(Controllable controllable)
    {
        Unsubscribe(controllable);
        _dirty.Remove(controllable);

        _server.Broadcast("{\"t\":\"removed\",\"id\":" + WebJson.Quote(controllable.controllableId) + "}");
    }

    void Subscribe(Controllable controllable)
    {
        if (controllable == null || _subscriptions.ContainsKey(controllable))
            return;

        //The event carries only a member name, so the controllable is captured here rather than
        //looked up again when it fires.
        Controllable.ControllableValueChangedEvent handler = member => MarkDirty(controllable, member);

        controllable.controllableValueChanged += handler;
        _subscriptions.Add(controllable, handler);
    }

    void Unsubscribe(Controllable controllable)
    {
        Controllable.ControllableValueChangedEvent handler;
        if (!_subscriptions.TryGetValue(controllable, out handler))
            return;

        //A destroyed controllable's event is gone with it; only a live one needs detaching.
        if (controllable != null)
            controllable.controllableValueChanged -= handler;

        _subscriptions.Remove(controllable);
    }

    #endregion

    #region Sending values

    void MarkDirty(Controllable controllable, string member)
    {
        if (string.IsNullOrEmpty(member))
            return;

        HashSet<string> members;
        if (!_dirty.TryGetValue(controllable, out members))
        {
            members = new HashSet<string>();
            _dirty.Add(controllable, members);
        }

        members.Add(member);
    }

    //One message per frame holding every member that moved, keyed "id/member" as the OSC address is.
    void SendDirtyValues()
    {
        if (_dirty.Count == 0)
            return;

        if (_server.ClientCount == 0)
        {
            //Nothing to tell, and keeping the names would send a burst of stale values on connect -
            //the schema carries current values anyway.
            _dirty.Clear();
            return;
        }

        var json = new StringBuilder("{\"t\":\"values\",\"v\":{");
        var first = true;

        foreach (var changed in _dirty)
        {
            var controllable = changed.Key;
            if (controllable == null || controllable.controllableFields == null)
                continue;

            foreach (var member in changed.Value)
            {
                FieldInfo field;
                if (!controllable.controllableFields.TryGetValue(member, out field))
                    continue;

                var value = WebValueCodec.ToJson(field.FieldType, field.GetValue(controllable));
                if (value == null)
                    continue;

                if (!first) json.Append(',');
                first = false;

                json.Append(WebJson.Quote(controllable.controllableId + "/" + member)).Append(':').Append(value);
            }
        }

        _dirty.Clear();

        //Every changed member turned out to have no web representation, so there is nothing to send.
        if (first)
            return;

        _server.Broadcast(json.Append("}}").ToString());
    }

    #endregion

    #region Receiving

    void DrainInbound()
    {
        WebMessage message;
        while (_server.Inbound.TryDequeue(out message))
        {
            switch (message.Kind)
            {
                case WebMessageKind.Connected:
                    if (showDebug)
                        Debug.Log("[GenUI] Web client " + message.ClientId + " connected.");

                    _server.Send(message.ClientId, WebSchema.SchemaMessage());
                    break;

                case WebMessageKind.Text:
                    if (showDebug)
                        Debug.Log("[GenUI] Web client " + message.ClientId + " : " + message.Text);

                    Handle(message.ClientId, message.Text);
                    break;

                case WebMessageKind.Disconnected:
                    if (showDebug)
                        Debug.Log("[GenUI] Web client " + message.ClientId + " disconnected.");
                    break;
            }
        }
    }

    void Handle(int clientId, string text)
    {
        var message = WebJson.Parse(text);
        var kind = WebJson.AsString(WebJson.Member(message, "t"));

        switch (kind)
        {
            case "schema":
                _server.Send(clientId, WebSchema.SchemaMessage());
                break;

            case "set":
                Set(WebJson.AsString(WebJson.Member(message, "id")),
                    WebJson.AsString(WebJson.Member(message, "member")),
                    WebJson.Member(message, "value"));
                break;

            case "invoke":
                Invoke(WebJson.AsString(WebJson.Member(message, "id")),
                    WebJson.AsString(WebJson.Member(message, "method")));
                break;

            default:
                //A message this version does not know is ignored rather than reported: the browser is
                //served from this same build, so it can only be a stale tab or something else entirely.
                break;
        }
    }

    void Set(string id, string member, object value)
    {
        var controllable = Find(id);
        if (controllable == null || string.IsNullOrEmpty(member))
            return;

        FieldInfo field;
        if (controllable.controllableFields == null
            || !controllable.controllableFields.TryGetValue(member, out field))
            return;

        List<object> values;
        if (!WebValueCodec.TryReadValues(field.FieldType, value, out values))
            return;

        //The OSC entry point, so [Range] clamping, read-only refusal, write-through to the target
        //script and the change fan-out to every other view all happen exactly as they do for OSC.
        ControllableMaster.UpdateValue(id, member, values);
    }

    void Invoke(string id, string method)
    {
        var controllable = Find(id);
        if (controllable == null || string.IsNullOrEmpty(method) || controllable.controllableMethods == null)
            return;

        ClassMethodInfo info;
        if (!controllable.controllableMethods.TryGetValue(method, out info))
            return;

        //Only what the panel would draw a button for: a method hidden from the UI, or one taking
        //arguments, has no press to mirror.
        if (!WebSchema.IsButton(info))
            return;

        controllable.SetMethodProp(info, new List<object>());
    }

    static Controllable Find(string id)
    {
        Controllable controllable;
        return !string.IsNullOrEmpty(id)
            && ControllableMaster.RegisteredControllables.TryGetValue(id, out controllable)
            ? controllable
            : null;
    }

    #endregion
}
