using UnityEngine;
using UnityEngine.EventSystems;

// GenUI does not own the scene's EventSystem — providing one is the host project's job, and a
// second EventSystem in a scene that already drives UI leaves both in an undefined state. The
// editor menu sets one up when the scene has none; at runtime GenUI only reports a missing one and
// never creates, replaces or reconfigures anything.
public static class EventSystemCheck
{
    public const string MissingMessage =
        "[GenUI] No EventSystem in the scene — the UI will not respond to input. " +
        "Add one via GameObject > UI > Event System, or use Theoriz > GenUI > Add GenUI to Scene.";

    // Returns true when the loaded scenes contain an EventSystem, and warns naming both fixes
    // when they do not.
    public static bool WarnIfMissing()
    {
        // EventSystem.current is only set once an EventSystem's OnEnable has run, so it says
        // nothing about one that exists but has not enabled yet. Searching the loaded scenes is
        // what makes the answer independent of script execution order.
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
            return true;

        Debug.LogWarning(MissingMessage);
        return false;
    }
}
