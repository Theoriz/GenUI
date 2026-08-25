using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The colour picker: a saturation/value square, a hue bar, an alpha bar over a checkerboard and a
/// hex field, built from <see cref="UIFactory"/> and <see cref="GenUIStyle"/> like every widget.
/// </summary>
/// <remarks>
/// State is kept as HSVA, not as a Color: round-tripping through RGB loses the hue whenever
/// saturation or value reaches zero, which is what makes a picker's hue bar jump while the pointer is
/// dragged into the black corner. GetColor converts out; SetColor converts in and keeps the hue it
/// already has when the incoming colour has none of its own.
/// </remarks>
[AddComponentMenu("")]
public class GenUIColorPicker : MonoBehaviour
{
    //Ample for a square this size, and cheap to rewrite on every hue change.
    const int SvTextureSize = 128;
    const int BarTextureSize = 256;

    //In the order the boxes are laid out, which is also the order GetColor reports them.
    static readonly string[] ChannelNames = { "R", "G", "B", "A" };

    float _h, _s, _v = 1f, _a = 1f;

    //The hue the SV square is currently drawn for; NaN so the first refresh always writes it.
    float _svHue = float.NaN;

    Texture2D _svTexture;
    Texture2D _alphaTexture;

    //One hue ramp and one checkerboard for every picker: neither depends on the colour being picked,
    //so they are built once and never destroyed with an instance.
    static Texture2D _hueTexture;
    static Texture2D _checkerTexture;

    RectTransform _svMarker;
    Image _svMarkerFill;
    RectTransform _hueMarker;
    RectTransform _alphaMarker;
    InputField[] _channels;
    InputField _hexField;

    Color32[] _svPixels;
    Color32[] _alphaPixels;

    #region MonoBehaviour

    private void OnDestroy()
    {
        //A Texture2D made in code is not collected with the GameObject that referenced it, and the SV
        //one is rewritten on every hue change.
        DestroyTexture(_svTexture);
        DestroyTexture(_alphaTexture);
    }

    #endregion

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _hueTexture = null;
        _checkerTexture = null;
    }

    #region Colour

    /// <summary>Where the hue bar sits. Kept across colours that have no hue of their own, which a
    /// Color could not hold.</summary>
    public float Hue { get { return _h; } }

    public float Saturation { get { return _s; } }
    public float Value { get { return _v; } }
    public float Alpha { get { return _a; } }

    public Color GetColor()
    {
        return HsvToRgb(_h, _s, _v, _a);
    }

    public void SetColor(Color color)
    {
        float h, s, v;
        RgbToHsv(color, out h, out s, out v);

        //A grey has no hue of its own, so taking the one RGBToHSV reports for it would throw away the
        //hue the user is holding on the bar.
        if (s > 0f && v > 0f)
            _h = h;

        _s = s;
        _v = v;
        _a = color.a;

        Refresh();
    }

    void SetHsv(float h, float s, float v, float a)
    {
        _h = h;
        _s = s;
        _v = v;
        _a = a;

        Refresh();
    }

    #endregion

    #region Conversions

    /// <summary>
    /// Where <paramref name="localPoint"/> falls inside <paramref name="rect"/>, as a fraction of it
    /// on each axis, clamped to the rect so a drag that leaves it holds at the edge.
    /// </summary>
    public static Vector2 NormalizedPoint(Rect rect, Vector2 localPoint)
    {
        var x = rect.width > 0f ? (localPoint.x - rect.xMin) / rect.width : 0f;
        var y = rect.height > 0f ? (localPoint.y - rect.yMin) / rect.height : 0f;

        return new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y));
    }

    /// <summary>Unity's conversion, which carries no alpha, with the alpha put back.</summary>
    public static Color HsvToRgb(float h, float s, float v, float a)
    {
        var color = Color.HSVToRGB(Mathf.Repeat(h, 1f), Mathf.Clamp01(s), Mathf.Clamp01(v));
        color.a = Mathf.Clamp01(a);
        return color;
    }

    public static void RgbToHsv(Color c, out float h, out float s, out float v)
    {
        Color.RGBToHSV(c, out h, out s, out v);
    }

    /// <summary>
    /// Parses #RGB, #RRGGBB and #RRGGBBAA, with or without the leading '#'.
    /// </summary>
    /// <remarks>
    /// The digits are checked here rather than left to ColorUtility, which also accepts colour names
    /// - so "red" would be a valid entry in a field that only ever shows hex.
    /// </remarks>
    public static bool TryParseHex(string text, out Color color)
    {
        color = Color.white;

        if (string.IsNullOrEmpty(text))
            return false;

        var hex = text.Trim();
        if (hex.Length > 0 && hex[0] == '#')
            hex = hex.Substring(1);

        if (hex.Length != 3 && hex.Length != 6 && hex.Length != 8)
            return false;

        foreach (var c in hex)
        {
            var isHexDigit = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
            if (!isHexDigit)
                return false;
        }

        return ColorUtility.TryParseHtmlString("#" + hex, out color);
    }

    /// <summary>A channel as the 0..255 the RGBA boxes and the hex field both show it in.</summary>
    public static int ToByte(float channel)
    {
        return Mathf.RoundToInt(Mathf.Clamp01(channel) * 255f);
    }

    public static float FromByte(int value)
    {
        return Mathf.Clamp(value, 0, 255) / 255f;
    }

    /// <summary>
    /// The hex a colour reads as: six digits, or eight when it is not fully opaque. Unity's own
    /// formatting, so it is upper case and independent of the culture the game runs under.
    /// </summary>
    public static string ToHex(Color color)
    {
        return color.a >= 1f
            ? "#" + ColorUtility.ToHtmlStringRGB(color)
            : "#" + ColorUtility.ToHtmlStringRGBA(color);
    }

    #endregion

    #region Building

    /// <summary>
    /// Creates the picker and its own hierarchy. Its size comes from GenUIStyle; the caller places it.
    /// </summary>
    public static GenUIColorPicker Build(Transform parent)
    {
        var root = UIFactory.CreateChild("GenUIColorPicker", parent);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.zero;
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(GenUIStyle.PickerWidth, GenUIStyle.PickerHeight);

        //Its own opaque backing: this is a popup over the panel, and the SV square has to be read as a
        //colour rather than as whatever shows through it.
        UIFactory.AddImage(root.gameObject, GenUIAssets.Instance.Box, GenUIStyle.PickerBackground);

        var picker = root.gameObject.AddComponent<GenUIColorPicker>();
        picker.BuildHierarchy(root);
        picker.Refresh();

        return picker;
    }

    void BuildHierarchy(RectTransform root)
    {
        //Every part is full width and stacked with one spacing, so the layout group is the whole
        //arrangement: adding or resizing a part needs no offsets recomputed.
        UIFactory.AddVerticalLayout(root.gameObject, GenUIStyle.PickerBarSpacing, GenUIStyle.PickerPadding,
            expandWidth: true, expandHeight: false);

        BuildSvSquare(root);
        BuildHueBar(root);
        BuildAlphaBar(root);
        BuildChannelRow(root);
        BuildHexField(root);
    }

    void BuildSvSquare(Transform parent)
    {
        var area = Part("SV", parent, GenUIStyle.PickerSvHeight);

        _svTexture = NewTexture(SvTextureSize, SvTextureSize);
        _svPixels = new Color32[SvTextureSize * SvTextureSize];
        AddRawImage(area, _svTexture);

        //A ring rather than a dot, showing the colour it sits on through its middle, so the marker
        //never hides the pixel it points at.
        _svMarker = UIFactory.CreateCentered("Marker", area, GenUIStyle.PickerMarkerSize, GenUIStyle.PickerMarkerSize);
        var ring = UIFactory.AddImage(_svMarker.gameObject, GenUIAssets.Instance.Knob, GenUIStyle.PickerMarkerColor, Image.Type.Simple);
        ring.raycastTarget = false;

        var inner = GenUIStyle.PickerMarkerSize - 2f * GenUIStyle.PickerMarkerThickness;
        var fill = UIFactory.CreateCentered("Fill", _svMarker, inner, inner);
        _svMarkerFill = UIFactory.AddImage(fill.gameObject, GenUIAssets.Instance.Knob, Color.white, Image.Type.Simple);
        _svMarkerFill.raycastTarget = false;

        PickerAreaDrag.Attach(area, OnSvPoint);
    }

    void BuildHueBar(Transform parent)
    {
        var area = Part("Hue", parent, GenUIStyle.PickerBarThickness);

        AddRawImage(area, HueTexture());
        _hueMarker = AddBarMarker(area);

        PickerAreaDrag.Attach(area, OnHuePoint);
    }

    void BuildAlphaBar(Transform parent)
    {
        var area = Part("Alpha", parent, GenUIStyle.PickerBarThickness);

        //The checkerboard is on the area itself and the gradient is a child, because a child draws in
        //front of its parent: the ramp has to be over the squares it is judged against.
        var checker = AddRawImage(area, CheckerTexture());
        checker.uvRect = new Rect(0f, 0f,
            GenUIStyle.PickerContentWidth / (2f * GenUIStyle.PickerCheckerCellSize),
            GenUIStyle.PickerBarThickness / (2f * GenUIStyle.PickerCheckerCellSize));

        _alphaTexture = NewTexture(BarTextureSize, 1);
        _alphaPixels = new Color32[BarTextureSize];

        var gradient = UIFactory.CreateChild("Gradient", area);
        AddRawImage(gradient, _alphaTexture).raycastTarget = false;

        _alphaMarker = AddBarMarker(area);

        PickerAreaDrag.Attach(area, OnAlphaPoint);
    }

    /// <summary>
    /// The four channel boxes, in bytes rather than in 0..1 so they agree digit for digit with the
    /// hex field under them.
    /// </summary>
    void BuildChannelRow(Transform parent)
    {
        var area = Part("Channels", parent, GenUIStyle.PickerFieldRowHeight);
        //The cells share the row evenly, the same way a vector row's axes do.
        UIFactory.AddHorizontalLayout(area.gameObject, GenUIStyle.AxisSpacing, expandHeight: true);

        _channels = new InputField[ChannelNames.Length];

        for (var i = 0; i < ChannelNames.Length; i++)
        {
            var cell = UIFactory.CreateChild(ChannelNames[i] + "Input", area);
            //The letter takes exactly its glyph's width and the box takes the rest.
            UIFactory.AddHorizontalLayout(cell.gameObject, GenUIStyle.AxisLabelGap,
                expandWidth: false, expandHeight: true, alignment: TextAnchor.MiddleLeft);

            var letter = UIFactory.CreateChild("Text", cell);
            UIFactory.AddText(letter.gameObject, ChannelNames[i], GenUIStyle.LabelFontSize,
                TextAnchor.MiddleLeft, GenUIStyle.LabelColor);

            var fieldRect = UIFactory.CreateChild("InputField", cell);
            //Asks for no width of its own, above the InputField's own priority: InputField reports
            //the width of its text as its preferred width, which would widen one cell and squeeze
            //the others. Same reasoning as VectorUIBase.
            var element = UIFactory.AddLayoutElement(fieldRect.gameObject, preferredWidth: 0f, flexibleWidth: 1f);
            element.layoutPriority = 2;

            _channels[i] = UIFactory.AddInputField(fieldRect, InputField.ContentType.IntegerNumber);
            _channels[i].characterLimit = 3;

            //Captured per iteration: the listener has to know which channel it belongs to when it runs.
            var channel = i;
            _channels[i].onEndEdit.AddListener((edited) => OnChannelCommitted(channel, edited));
        }
    }

    void BuildHexField(Transform parent)
    {
        var area = Part("Hex", parent, GenUIStyle.PickerFieldRowHeight);

        _hexField = UIFactory.AddInputField(area);
        //'#' plus eight digits, the longest form ToHex writes.
        _hexField.characterLimit = 9;
        _hexField.onEndEdit.AddListener(OnHexCommitted);
    }

    /// <summary>One row of the picker: full width, its own height, laid out by the group.</summary>
    static RectTransform Part(string name, Transform parent, float height)
    {
        var rect = UIFactory.CreateChild(name, parent);
        UIFactory.AddLayoutElement(rect.gameObject, minHeight: height, preferredHeight: height);
        return rect;
    }

    static RectTransform AddBarMarker(Transform parent)
    {
        var marker = UIFactory.CreateChild("Marker", parent);
        //Spanning the bar's full height, so only its horizontal position has to be moved.
        marker.anchorMin = new Vector2(0f, 0f);
        marker.anchorMax = new Vector2(0f, 1f);
        marker.sizeDelta = new Vector2(GenUIStyle.PickerMarkerThickness, 0f);

        //No sprite: the UI sprites are rounded and 9-sliced, and one this narrow draws as a blob.
        var image = UIFactory.AddImage(marker.gameObject, null, GenUIStyle.PickerMarkerColor);
        image.raycastTarget = false;

        return marker;
    }

    static RawImage AddRawImage(Transform target, Texture2D texture)
    {
        var image = target.gameObject.AddComponent<RawImage>();
        image.texture = texture;
        return image;
    }

    static Texture2D NewTexture(int width, int height)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        return texture;
    }

    #endregion

    #region Textures

    static Texture2D HueTexture()
    {
        if (_hueTexture != null)
            return _hueTexture;

        _hueTexture = NewTexture(BarTextureSize, 1);

        var pixels = new Color32[BarTextureSize];
        for (var x = 0; x < BarTextureSize; x++)
            pixels[x] = Color.HSVToRGB(x / (float)(BarTextureSize - 1), 1f, 1f);

        _hueTexture.SetPixels32(pixels);
        _hueTexture.Apply(false);

        return _hueTexture;
    }

    static Texture2D CheckerTexture()
    {
        if (_checkerTexture != null)
            return _checkerTexture;

        //Two squares by two, tiled by the RawImage's uvRect: the cell size is a style token, not a
        //texture size.
        _checkerTexture = NewTexture(2, 2);
        _checkerTexture.filterMode = FilterMode.Point;
        _checkerTexture.wrapMode = TextureWrapMode.Repeat;

        Color32 light = GenUIStyle.PickerCheckerLight;
        Color32 dark = GenUIStyle.PickerCheckerDark;
        _checkerTexture.SetPixels32(new[] { light, dark, dark, light });
        _checkerTexture.Apply(false);

        return _checkerTexture;
    }

    void WriteSvTexture()
    {
        for (var y = 0; y < SvTextureSize; y++)
        {
            var v = y / (float)(SvTextureSize - 1);
            for (var x = 0; x < SvTextureSize; x++)
                _svPixels[y * SvTextureSize + x] = Color.HSVToRGB(_h, x / (float)(SvTextureSize - 1), v);
        }

        _svTexture.SetPixels32(_svPixels);
        _svTexture.Apply(false);
    }

    void WriteAlphaTexture()
    {
        var opaque = HsvToRgb(_h, _s, _v, 1f);

        for (var x = 0; x < BarTextureSize; x++)
        {
            var c = opaque;
            c.a = x / (float)(BarTextureSize - 1);
            _alphaPixels[x] = c;
        }

        _alphaTexture.SetPixels32(_alphaPixels);
        _alphaTexture.Apply(false);
    }

    //Destroy throws outside play mode, which is where the EditMode tests build a picker.
    static void DestroyTexture(Texture2D texture)
    {
        if (texture == null)
            return;

        if (Application.isPlaying)
            Destroy(texture);
        else
            DestroyImmediate(texture);
    }

    #endregion

    #region Refresh

    void Refresh()
    {
        //Rewritten only when the hue actually moved: the square is 16k pixels and both bars and the
        //hex field change on every drag of it.
        if (!Mathf.Approximately(_svHue, _h))
        {
            _svHue = _h;
            WriteSvTexture();
        }

        WriteAlphaTexture();

        _svMarker.anchorMin = _svMarker.anchorMax = new Vector2(_s, _v);
        _svMarker.anchoredPosition = Vector2.zero;
        _svMarkerFill.color = HsvToRgb(_h, _s, _v, 1f);

        MoveBarMarker(_hueMarker, _h);
        MoveBarMarker(_alphaMarker, _a);

        var color = GetColor();
        for (var i = 0; i < _channels.Length; i++)
            WriteField(_channels[i], ToByte(color[i]).ToString(CultureInfo.InvariantCulture));

        WriteField(_hexField, ToHex(color));
    }

    //Never while the field is being typed into, or the caret jumps to the end on every keystroke.
    static void WriteField(InputField field, string text)
    {
        if (!field.isFocused)
            field.text = text;
    }

    static void MoveBarMarker(RectTransform marker, float fraction)
    {
        marker.anchorMin = new Vector2(fraction, 0f);
        marker.anchorMax = new Vector2(fraction, 1f);
        marker.anchoredPosition = Vector2.zero;
    }

    #endregion

    #region Input

    void OnSvPoint(Vector2 point)
    {
        SetHsv(_h, point.x, point.y, _a);
    }

    void OnHuePoint(Vector2 point)
    {
        SetHsv(point.x, _s, _v, _a);
    }

    void OnAlphaPoint(Vector2 point)
    {
        SetHsv(_h, _s, _v, point.x);
    }

    void OnChannelCommitted(int channel, string edited)
    {
        int value;
        if (!int.TryParse(edited, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            //Rejected rather than kept, the same way the hex field rejects: the boxes always show the
            //colour the picker holds.
            Refresh();
            return;
        }

        //The other three come from the colour rather than from their own text: that is what the
        //picker holds, and it is what those boxes are already showing.
        var color = GetColor();
        color[channel] = FromByte(value);

        SetColor(color);
    }

    void OnHexCommitted(string text)
    {
        Color parsed;
        if (TryParseHex(text, out parsed))
            SetColor(parsed);
        else
            Refresh();
    }

    #endregion
}
