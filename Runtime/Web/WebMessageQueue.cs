using System.Collections.Generic;

/// <summary>What a queued message says happened on a connection.</summary>
public enum WebMessageKind
{
    Connected,
    Text,
    Disconnected
}

/// <summary>One thing a browser did, waiting to be handled on the main thread.</summary>
public struct WebMessage
{
    public WebMessageKind Kind;
    public int ClientId;

    /// <summary>The frame's text; null for a connection notice.</summary>
    public string Text;
}

/// <summary>
/// The queue the socket threads fill and the main thread drains.
/// </summary>
/// <remarks>
/// Everything a message leads to - reflection over a <c>Controllable</c>, a value written to a target
/// script - is main-thread only, so nothing is handled where it arrives.
/// </remarks>
public class WebMessageQueue
{
    //A browser dragging a slider sends a message per input event, and a paused or stalled editor drains
    //nothing meanwhile. Past this depth the backlog is stale by definition, so further text is dropped
    //rather than replayed minutes later.
    public const int MaxPending = 1024;

    readonly Queue<WebMessage> _queue = new Queue<WebMessage>();

    public int Count
    {
        get
        {
            lock (_queue)
            {
                return _queue.Count;
            }
        }
    }

    /// <summary>
    /// Queues a message, or drops it and answers false when the backlog is already too deep.
    /// </summary>
    /// <remarks>
    /// Connection notices are never dropped: a lost <c>Connected</c> would leave that browser without
    /// its schema, and a lost <c>Disconnected</c> would leak the client.
    /// </remarks>
    public bool Enqueue(WebMessage message)
    {
        lock (_queue)
        {
            if (message.Kind == WebMessageKind.Text && _queue.Count >= MaxPending)
                return false;

            _queue.Enqueue(message);
            return true;
        }
    }

    public bool TryDequeue(out WebMessage message)
    {
        lock (_queue)
        {
            if (_queue.Count == 0)
            {
                message = default(WebMessage);
                return false;
            }

            message = _queue.Dequeue();
            return true;
        }
    }

    public void Clear()
    {
        lock (_queue)
        {
            _queue.Clear();
        }
    }
}
