using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// The single tooltip shown beside the pointer, after a hover delay, for whichever row asks for it.
/// </summary>
/// <remarks>
/// Unlike the other two popups it has no backdrop and nothing in it raycasts: a backdrop is a
/// screen-covering button, and a tooltip that answered the mouse would take the pointer off the row that
/// asked for it and flicker. The root stays active, since the delay is counted here; only the content is
/// switched on and off.
/// </remarks>
public class TooltipPopup : MonoBehaviour
{
    /// <summary>The part that follows the pointer. The root stays over the whole screen.</summary>
    public RectTransform Content { get; private set; }

    Text _label;
    LayoutElement _labelSize;
    TooltipTrigger _owner;
    float _hoverStart;

    #region MonoBehaviour

    void Update()
    {
        //Nothing hovers without a pointer, and the tooltip is placed where that pointer is.
        if (Mouse.current == null)
            return;

        //A button held means the user is dragging a slider, scrubbing a label or picking a colour: no
        //tooltip over any of that. The delay restarts on release, so hovering on still shows it.
        if (MouseButtonHeld())
        {
            Content.gameObject.SetActive(false);
            _hoverStart = Time.unscaledTime;
            return;
        }

        if (_owner == null || Content.gameObject.activeSelf)
            return;

        if (Time.unscaledTime - _hoverStart < GenUIStyle.TooltipDelay)
            return;

        Reveal();
    }

    void OnDisable()
    {
        _owner = null;
        Content.gameObject.SetActive(false);
    }

    #endregion

    /// <summary>
    /// Creates the tooltip, hidden. Rows ask for it through UIMaster.
    /// </summary>
    public static TooltipPopup Build(Transform canvas)
    {
        var root = UIFactory.CreateChild("TooltipPopup", canvas);
        var popup = root.gameObject.AddComponent<TooltipPopup>();

        //Anchored by its top-left corner, so the tooltip opens down and to the right of the pointer.
        var content = UIFactory.CreateChild("Tooltip", root);
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.zero;
        content.pivot = new Vector2(0f, 1f);
        content.sizeDelta = Vector2.zero;
        popup.Content = content;

        var background = UIFactory.AddImage(content.gameObject, GenUIAssets.Instance.Background,
            GenUIStyle.TooltipBackground);
        background.raycastTarget = false;

        UIFactory.AddVerticalLayout(content.gameObject, padding: GenUIStyle.TooltipPadding);
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var label = UIFactory.CreateChild("Text", content);
        popup._label = UIFactory.AddText(label.gameObject, string.Empty, GenUIStyle.TooltipFontSize,
            TextAnchor.UpperLeft, GenUIStyle.TooltipColor);
        popup._label.raycastTarget = false;
        popup._label.horizontalOverflow = HorizontalWrapMode.Wrap;
        popup._labelSize = UIFactory.AddLayoutElement(label.gameObject);

        content.gameObject.SetActive(false);

        return popup;
    }

    /// <summary>Starts the hover delay for <paramref name="source"/>, which the tooltip then belongs to.</summary>
    public void Request(string text, TooltipTrigger source)
    {
        if (_owner == source)
            return;

        _owner = source;
        _label.text = text;
        _hoverStart = Time.unscaledTime;

        Content.gameObject.SetActive(false);
    }

    /// <summary>Hides the tooltip, unless another row has since asked for it.</summary>
    public void Release(TooltipTrigger source)
    {
        if (_owner != source)
            return;

        _owner = null;
        Content.gameObject.SetActive(false);
    }

    void Reveal()
    {
        //One line of the text, so a short tooltip is its own width and only a long one wraps. Set
        //before the layout pass PlacePopup forces, which is what the height is then measured against.
        _labelSize.preferredWidth = Mathf.Min(_label.preferredWidth, GenUIStyle.TooltipMaxWidth);

        Content.gameObject.SetActive(true);

        //Placed after activation: a disabled rect reports no size, so it could not be kept on screen.
        UIFactory.PlacePopup(Content, Mouse.current.position.value + GenUIStyle.TooltipCursorOffset);
    }

    static bool MouseButtonHeld()
    {
        var mouse = Mouse.current;
        return mouse.leftButton.isPressed || mouse.rightButton.isPressed || mouse.middleButton.isPressed;
    }
}
