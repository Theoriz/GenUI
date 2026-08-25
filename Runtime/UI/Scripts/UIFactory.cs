using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The low-level vocabulary the widgets build themselves with: rects, images, text, input fields,
/// buttons and layout groups, styled from <see cref="GenUIStyle"/> and <see cref="GenUIAssets"/>.
/// </summary>
/// <remarks>
/// It knows nothing about controllables or members - a widget composes these calls in its own
/// BuildHierarchy, which is the file that then reads the result back.
/// </remarks>
public static class UIFactory
{
    //Unity's built-in UI layer. Named lookup would return -1 in a project that renamed it, which
    //silently puts the whole panel on a layer the canvas may not draw.
    const int UILayer = 5;

    static GenUIAssets Assets { get { return GenUIAssets.Instance; } }

    #region Rects

    /// <summary>
    /// A row of the panel: full width, its own height, since the panel's layout group controls
    /// child widths but not child heights.
    /// </summary>
    public static RectTransform CreateRect(string name, Transform parent, float height = 0f)
    {
        var rect = NewRect(name, parent);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, height);
        return rect;
    }

    /// <summary>A child filling its parent.</summary>
    public static RectTransform CreateChild(string name, Transform parent)
    {
        var rect = NewRect(name, parent);
        Stretch(rect);
        return rect;
    }

    /// <summary>A child occupying a horizontal slice of its parent, given as anchor fractions.</summary>
    public static RectTransform CreateSlice(string name, Transform parent, float xMin, float xMax)
    {
        var rect = NewRect(name, parent);
        rect.anchorMin = new Vector2(xMin, 0f);
        rect.anchorMax = new Vector2(xMax, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        return rect;
    }

    /// <summary>A child of a fixed size, centred in its parent.</summary>
    public static RectTransform CreateCentered(string name, Transform parent, float width, float height)
    {
        var rect = NewRect(name, parent);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = UILayer;

        var rect = (RectTransform)go.transform;
        //Without worldPositionStays: false the new child would keep its world scale, which a scaled
        //canvas then divides out of its size.
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        return rect;
    }

    #endregion

    #region Graphics

    /// <summary>
    /// Sliced by default, which is what the frames and boxes need; pass Simple for a sprite that has
    /// no border to preserve, such as an arrow or a checkmark.
    /// </summary>
    public static Image AddImage(GameObject go, Sprite sprite, Color color, Image.Type type = Image.Type.Sliced)
    {
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = type;
        return image;
    }

    public static Text AddText(GameObject go, string content, int fontSize, TextAnchor anchor, Color color, bool bold = false)
    {
        var text = go.AddComponent<Text>();
        text.font = bold ? Assets.BoldFont : Assets.RegularFont;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = color;
        text.text = content;
        return text;
    }

    /// <summary>
    /// The label of a member row: the left half, shrinking rather than clipping when the name is long.
    /// </summary>
    public static Text CreateLabel(Transform parent, string name = "Label")
    {
        var rect = CreateSlice(name, parent, 0f, GenUIStyle.LabelWidthRatio);
        var text = AddText(rect.gameObject, string.Empty, GenUIStyle.LabelFontSize, TextAnchor.MiddleLeft, GenUIStyle.LabelColor);
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = GenUIStyle.LabelMinFontSize;
        text.resizeTextMaxSize = GenUIStyle.LabelFontSize;
        text.alignByGeometry = true;
        return text;
    }

    #endregion

    #region Controls

    /// <summary>
    /// An input field filling <paramref name="rect"/>: its box, the text it edits and the
    /// placeholder behind it.
    /// </summary>
    /// <remarks>
    /// The one place the trio is defined. Each prefab used to carry its own copy, which is how the
    /// widgets ended up with different insets and a read-only look that only some of them had.
    /// </remarks>
    public static InputField AddInputField(RectTransform rect, InputField.ContentType contentType = InputField.ContentType.Standard)
    {
        var background = AddImage(rect.gameObject, Assets.InputBackground, Color.white);

        var placeholder = CreateInputText(rect, "Placeholder", GenUIStyle.PlaceholderColor);
        placeholder.fontStyle = FontStyle.Italic;
        placeholder.horizontalOverflow = HorizontalWrapMode.Wrap;

        var text = CreateInputText(rect, "Text", GenUIStyle.InputTextColor);
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.supportRichText = false;

        var field = rect.gameObject.AddComponent<InputField>();
        field.targetGraphic = background;
        field.textComponent = text;
        field.placeholder = placeholder;
        field.contentType = contentType;
        field.colors = GenUIStyle.ControlColors();
        field.customCaretColor = true;
        field.caretColor = GenUIStyle.CaretColor;
        field.selectionColor = GenUIStyle.SelectionColor;
        return field;
    }

    static Text CreateInputText(Transform parent, string name, Color color)
    {
        var rect = CreateChild(name, parent);
        rect.anchoredPosition = new Vector2(0f, -0.5f);
        rect.sizeDelta = -GenUIStyle.InputTextInset;

        return AddText(rect.gameObject, string.Empty, GenUIStyle.LabelFontSize, TextAnchor.UpperLeft, color);
    }

    public static Button AddButton(GameObject go, Sprite sprite, Color color)
    {
        var image = AddImage(go, sprite, color);

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }

    //How far a backdrop is grown past its parent on every side. Well beyond any screen, since the
    //canvas clips it and the cost is one flat quad either way.
    const float BackdropOverscan = 2000f;

    /// <summary>
    /// The screen-covering click target behind a popup, which dismisses it.
    /// </summary>
    /// <remarks>
    /// Grown past its parent rather than stretched to it, so it covers the screen whatever rect it
    /// hangs from - stretching to the parent leaves a strip along any edge the parent falls short of.
    /// It carries no sprite because the UI sprites are rounded and 9-sliced, and one stretched this
    /// far draws its border as a frame around the screen.
    /// </remarks>
    public static Button AddBackdrop(Transform parent, Color color)
    {
        var rect = CreateChild("CloseButton", parent);
        rect.offsetMin = new Vector2(-BackdropOverscan, -BackdropOverscan);
        rect.offsetMax = new Vector2(BackdropOverscan, BackdropOverscan);

        return AddButton(rect.gameObject, null, color);
    }

    /// <summary>
    /// Makes a widget's row, or one part of it, answer the mouse: right-click for the OSC address
    /// menu, and on the colour row, left-click for the picker.
    /// </summary>
    /// <remarks>
    /// linkedUI is set here, at construction, by the only code that knows which widget the part
    /// belongs to. The prefabs left it to be repaired afterwards by a pass over the built panel.
    /// </remarks>
    public static MouseButtonEvent AddMouseEvent(GameObject go, ControllableUI widget, bool enableColorPicker = false)
    {
        var mouseEvent = go.AddComponent<MouseButtonEvent>();
        mouseEvent.linkedUI = widget;
        mouseEvent.enableColorPicker = enableColorPicker;
        return mouseEvent;
    }

    #endregion

    #region Popups

    /// <summary>
    /// Puts a popup at a screen point, moved back inside the screen when it would hang over an edge.
    /// </summary>
    /// <remarks>
    /// The canvas is a screen-space overlay, so a rect's world coordinates are screen pixels and the
    /// point can be used as a position directly. The size is read after a forced layout pass: a popup
    /// sized by a ContentSizeFitter has no size yet on the frame it is re-enabled, and would be
    /// clamped as if it were empty.
    /// </remarks>
    public static void PlacePopup(RectTransform content, Vector2 screenPoint)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        content.position = screenPoint;

        var corners = new Vector3[4];
        content.GetWorldCorners(corners);

        content.position += (Vector3)ScreenNudge(corners[0], corners[2], new Vector2(Screen.width, Screen.height));
    }

    /// <summary>
    /// How far a rect spanning min..max has to move to sit inside a screen of the given size.
    /// </summary>
    /// <remarks>
    /// Each axis resolves the opposite edge last, so a popup bigger than the screen keeps its
    /// top-left corner visible - where its title and first control are - rather than hanging off the
    /// other side.
    /// </remarks>
    public static Vector2 ScreenNudge(Vector2 min, Vector2 max, Vector2 screen)
    {
        var nudge = Vector2.zero;

        if (max.x > screen.x)
            nudge.x = screen.x - max.x;
        if (min.x + nudge.x < 0f)
            nudge.x = -min.x;

        if (min.y < 0f)
            nudge.y = -min.y;
        if (max.y + nudge.y > screen.y)
            nudge.y = screen.y - max.y;

        return nudge;
    }

    #endregion

    #region Layout

    public static HorizontalLayoutGroup AddHorizontalLayout(GameObject go, float spacing = 0f, int padding = 0,
        bool controlWidth = true, bool controlHeight = true, bool expandWidth = true, bool expandHeight = false,
        TextAnchor alignment = TextAnchor.UpperLeft)
    {
        var group = go.AddComponent<HorizontalLayoutGroup>();
        Configure(group, spacing, padding, controlWidth, controlHeight, expandWidth, expandHeight, alignment);
        return group;
    }

    public static VerticalLayoutGroup AddVerticalLayout(GameObject go, float spacing = 0f, int padding = 0,
        bool controlWidth = true, bool controlHeight = true, bool expandWidth = true, bool expandHeight = false,
        TextAnchor alignment = TextAnchor.UpperLeft)
    {
        var group = go.AddComponent<VerticalLayoutGroup>();
        Configure(group, spacing, padding, controlWidth, controlHeight, expandWidth, expandHeight, alignment);
        return group;
    }

    static void Configure(HorizontalOrVerticalLayoutGroup group, float spacing, int padding,
        bool controlWidth, bool controlHeight, bool expandWidth, bool expandHeight, TextAnchor alignment)
    {
        group.spacing = spacing;
        group.padding = new RectOffset(padding, padding, padding, padding);
        group.childControlWidth = controlWidth;
        group.childControlHeight = controlHeight;
        group.childForceExpandWidth = expandWidth;
        group.childForceExpandHeight = expandHeight;
        group.childAlignment = alignment;
    }

    public static LayoutElement AddLayoutElement(GameObject go, float minWidth = -1f, float minHeight = -1f,
        float preferredWidth = -1f, float preferredHeight = -1f, float flexibleWidth = -1f, float flexibleHeight = -1f,
        bool ignoreLayout = false)
    {
        var element = go.AddComponent<LayoutElement>();
        element.ignoreLayout = ignoreLayout;
        element.minWidth = minWidth;
        element.minHeight = minHeight;
        element.preferredWidth = preferredWidth;
        element.preferredHeight = preferredHeight;
        element.flexibleWidth = flexibleWidth;
        element.flexibleHeight = flexibleHeight;
        return element;
    }

    #endregion
}
