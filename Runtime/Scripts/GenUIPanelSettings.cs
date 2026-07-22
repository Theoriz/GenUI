using UnityEngine;

// How GenUI draws the panel for the Controllable on the same GameObject. It lives here rather than
// on Controllable because OCF draws nothing, and the dependency runs GenUI -> OCF, so GenUI cannot
// add fields to Controllable.
//
// The component is optional: a Controllable without one gets the defaults below, with a bar colour
// derived from its id so panels stay distinguishable with no setup at all. Resolve the values
// through the three static helpers rather than reading the fields, so the absent case is handled in
// one place.
[DisallowMultipleComponent]
[AddComponentMenu("Theoriz/GenUI Panel Settings")]
public class GenUIPanelSettings : MonoBehaviour
{
    [Tooltip("Color of this controllable's panel bar. Defaults to a color derived from the controllable's ID.")]
    public Color barColor = Color.white;

    [Tooltip("Uncheck to give this controllable no panel at all. It stays controllable over OSC.")]
    public bool usePanel = true;

    [Tooltip("Check to have the panel start collapsed.")]
    public bool closePanelAtStart = true;

    #region MonoBehaviour

    //Called when the component is added in the Editor. Seeding the colour with the one the panel
    //already had means adding the component changes nothing on screen; it just makes the value
    //editable.
    void Reset()
    {
        var controllable = GetComponent<Controllable>();
        if (controllable != null)
            barColor = DefaultBarColor(controllable.controllableId);
    }

    #endregion

    #region Resolving a controllable's settings

    public static bool UsePanel(Controllable controllable)
    {
        var settings = controllable.GetComponent<GenUIPanelSettings>();
        return settings == null || settings.usePanel;
    }

    public static Color BarColorFor(Controllable controllable)
    {
        var settings = controllable.GetComponent<GenUIPanelSettings>();
        return settings != null ? settings.barColor : DefaultBarColor(controllable.controllableId);
    }

    public static bool ClosePanelAtStart(Controllable controllable)
    {
        var settings = controllable.GetComponent<GenUIPanelSettings>();
        return settings == null || settings.closePanelAtStart;
    }

    /// <summary>
    /// The bar colour a controllable gets when it has no <see cref="GenUIPanelSettings"/>: a hue
    /// picked from its id, fully saturated enough to read against the panel. Deriving it rather
    /// than defaulting to white is what keeps panels telling apart without any setup, and deriving
    /// it from the id rather than at random is what makes a panel keep its colour between runs.
    /// </summary>
    public static Color DefaultBarColor(string controllableId)
    {
        if (string.IsNullOrEmpty(controllableId))
            return Color.white;

        //FNV-1a. string.GetHashCode is deliberately not used: it is not guaranteed stable between
        //runs or platforms, which would make a panel change colour for no reason.
        uint hash = 2166136261;
        foreach (char c in controllableId)
        {
            hash ^= c;
            hash *= 16777619;
        }

        return Color.HSVToRGB((hash % 360) / 360f, 0.8f, 1f);
    }

    #endregion
}
