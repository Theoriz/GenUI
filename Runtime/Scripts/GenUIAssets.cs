using UnityEngine;

/// <summary>
/// The sprites and fonts the generated UI is built from.
/// </summary>
/// <remarks>
/// The widgets create their own hierarchies in code, so nothing else in GenUI holds a serialized
/// asset reference. These have to be serialized somewhere all the same: the UI sprites and Unity's
/// builtin font only reach a player build because an asset references them, and
/// Resources.GetBuiltinResource is not dependable at runtime across platforms.
/// </remarks>
[CreateAssetMenu(fileName = "GenUIAssets", menuName = "Theoriz/GenUI/Assets")]
public class GenUIAssets : ScriptableObject
{
    public const string ResourcePath = "GenUIAssets";

    public Font RegularFont;
    public Font BoldFont;

    public Sprite Box;
    public Sprite InputBackground;
    public Sprite Knob;
    public Sprite Background;
    public Sprite Checkmark;
    public Sprite UIMask;
    public Sprite DropdownArrow;
    public Sprite PanelArrow;

    private static GenUIAssets _instance;

    /// <summary>
    /// The asset, loaded from Resources on first use.
    /// </summary>
    /// <remarks>
    /// Lazy rather than resolved by UIMaster: the widgets build themselves, so a widget created
    /// outside a panel - a test, a host project - needs the assets without a UIMaster in the scene.
    /// </remarks>
    public static GenUIAssets Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GenUIAssets>(ResourcePath);

                if (_instance == null)
                {
                    Debug.LogError("[GenUI] Could not load the GenUIAssets asset from Resources. The UI will draw without its sprites and fonts.");

                    //An empty stand-in rather than null: the widgets read these fields all through
                    //their build, and one clear error beats a null reference from whichever of them
                    //happened to be built first.
                    _instance = CreateInstance<GenUIAssets>();
                }
            }

            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
    }
}
