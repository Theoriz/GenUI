using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Every measurement and colour the generated UI is drawn with.
/// </summary>
/// <remarks>
/// The widgets build their own hierarchies, so this is the one place the look is defined: changing a
/// row height or a tint here changes it everywhere, and no two widgets can drift apart. Grouped by
/// concern so the class can become a ScriptableObject later without touching any call site.
/// </remarks>
public static class GenUIStyle
{
    #region Metrics

    /// <summary>Height of an ordinary member row. The panel's layout group does not control child
    /// heights, so a row's own height is what it gets.</summary>
    public const float RowHeight = 25f;

    public const float HeaderHeight = 40f;
    public const float TooltipHeight = 20f;
    public const float PanelTitleHeight = 35f;
    public const float PresetRowHeight = 25f;

    public const float CheckboxSize = 20f;
    public const float PanelArrowSize = 18f;

    /// <summary>How far the fold arrow sits in from the panel's left edge.</summary>
    public const float PanelArrowInset = 12f;
    public const float ColorBarWidth = 4f;

    public const int PanelPadding = 3;

    /// <summary>Inset of the preset rows inside their section's backing.</summary>
    public const int PresetSectionPadding = 3;

    /// <summary>Empty space kept above a preset section, holding the rule that separates it from the
    /// member rows.</summary>
    public const int PresetSectionGap = 18;

    /// <summary>How far into that gap the rule sits. More space above it than below, so the rule reads
    /// as belonging to the block it heads rather than to the member row it follows.</summary>
    public const float SeparatorSpaceAbove = 10f;

    public const float SeparatorThickness = 1f;

    /// <summary>Gap between a panel's colour bar and its body.</summary>
    public const float PanelBarGap = 4f;

    /// <summary>Space between the axis cells of a vector row: each cell starts with its letter, so
    /// without a gap the letter reads as belonging to the box on its left.</summary>
    public const float AxisSpacing = 6f;

    /// <summary>Gap between an axis letter and its own box.</summary>
    /// <remarks>
    /// The letter is laid out at its own glyph width rather than in a fixed column, so this gap is
    /// the same for every axis: w is several pixels wider than x, y and z, and a fixed column would
    /// take that difference out of the space beside it.
    /// </remarks>
    public const float AxisLabelGap = 4f;

    //A [Range] row: the track takes most of the control half and the value box sits at its end.
    public const float SliderTrackStart = 0.5f;
    public const float SliderTrackEnd = 0.8f;
    public const float SliderValueStart = 0.82f;

    /// <summary>How much shorter than its row the track is, so it does not touch the rows above and below.</summary>
    public const float SliderTrackInset = 5f;

    public const float SliderHandleWidth = 20f;

    //The track is a band across the middle of its rect; the handle spans the full height.
    public const float SliderBandMin = 0.25f;
    public const float SliderBandMax = 0.75f;

    /// <summary>Fraction of the row the label takes; the control fills the rest.</summary>
    public const float LabelWidthRatio = 0.5f;

    /// <summary>Inset of an input field's text and placeholder from its box.</summary>
    public static readonly Vector2 InputTextInset = new Vector2(10f, 7f);

    /// <summary>Empty space kept under a tooltip, so it reads as belonging to the row above it.</summary>
    public const float TooltipBottomSpacing = 8f;

    /// <summary>Empty space kept above the first method button, so the buttons read as a block of
    /// their own rather than as one more member row.</summary>
    public const float MethodGapHeight = 10f;

    /// <summary>Empty space kept under a panel's title, so the first row does not sit tight against
    /// the heading. A row of its own rather than extra title height, which would grow the tinted
    /// backing behind the title text.</summary>
    public const float PanelTitleBottomSpacing = 10f;

    #endregion

    #region Text

    public const int LabelFontSize = 14;
    public const int PanelTitleFontSize = 16;
    public const int TooltipFontSize = 12;

    /// <summary>Labels shrink rather than clip when a member name is long.</summary>
    public const int LabelMinFontSize = 10;

    #endregion

    #region Colours

    public static readonly Color LabelColor = Color.white;
    public static readonly Color InputTextColor = Color.white;

    /// <summary>White at half alpha, not the dark grey the prefabs used: a placeholder has to read
    /// against the input box behind it, which is dark.</summary>
    public static readonly Color PlaceholderColor = new Color(1f, 1f, 1f, 0.5f);

    public static readonly Color TooltipColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    public static readonly Color PanelBackground = new Color(0.078431375f, 0.078431375f, 0.078431375f, 0.4509804f);

    /// <summary>
    /// The backing behind a panel's title, tinted from that panel's own bar colour so the heading is
    /// told apart from the rows under it and still reads as belonging to the panel.
    /// </summary>
    /// <remarks>
    /// Kept faint: the panel is translucent over whatever the scene shows, so a strong tint would
    /// fight the bar rather than back the title.
    /// </remarks>
    public static Color PanelTitleBackground(Color barColor)
    {
        return new Color(barColor.r, barColor.g, barColor.b, PanelTitleBackgroundAlpha);
    }

    public const float PanelTitleBackgroundAlpha = 0.04f;

    /// <summary>The rule above a preset section. Light rather than dark: the panel is drawn over
    /// whatever the scene shows, so only a line brighter than the rows reads on every background.</summary>
    public static readonly Color SeparatorColor = new Color(1f, 1f, 1f, 0.04f);

    public static readonly Color ToggleOn = new Color(0.43f, 0.9f, 0.47f, 0.75f);
    public static readonly Color ToggleOff = new Color(0.9f, 0.4f, 0.4f, 0.8f);

    //The open dropdown is a light list over the dark panel, which is what tells it apart from the
    //rows behind it.
    public static readonly Color DropdownTemplateBackground = new Color(0.35294116f, 0.35294116f, 0.35294116f, 1f);
    public static readonly Color DropdownItemBackground = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f);
    public static readonly Color DropdownItemLabel = new Color(0.19607843f, 0.19607843f, 0.19607843f, 1f);

    /// <summary>Dimmed backing behind the right-click menu, so the menu reads as being in front of
    /// the panel. The colour picker's own backing is the same shape but invisible: it is only there
    /// to be clicked.</summary>
    public static readonly Color PopupBackdrop = new Color(0f, 0f, 0f, 0.69803923f);

    public static readonly Color CaretColor = Color.white;
    public static readonly Color SelectionColor = new Color(0.8602941f, 0.9213151f, 1f, 0.7529412f);

    #endregion

    #region Selectable states

    /// <summary>
    /// The tints of an editable control: dark, lifting on hover and sinking on press.
    /// </summary>
    /// <remarks>
    /// disabledColor is left at Color.clear so a read-only member shows its value with no frame
    /// around it; see ControllableUI.MakeDisplayOnly, which sets the same thing on the widgets that
    /// only turn read-only at runtime.
    /// </remarks>
    public static ColorBlock ControlColors()
    {
        var colors = ColorBlock.defaultColorBlock;
        colors.normalColor = new Color(0.227451f, 0.227451f, 0.227451f, 1f);
        colors.highlightedColor = new Color(0.28235295f, 0.28235295f, 0.28235295f, 1f);
        colors.pressedColor = new Color(0.09019608f, 0.09019608f, 0.09019608f, 1f);
        colors.selectedColor = new Color(0.28235295f, 0.28235295f, 0.28235295f, 1f);
        colors.disabledColor = Color.clear;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        return colors;
    }

    #endregion

    #region Colour picker

    /// <summary>Inset of the picker's parts from its own edge. An int because it is handed straight
    /// to the layout group's RectOffset.</summary>
    public const int PickerPadding = 8;

    /// <summary>Width of every part of the picker. Set by the RGBA row, which is the widest thing it
    /// has to hold: four boxes each fitting "255" beside its letter.</summary>
    public const float PickerContentWidth = 216f;

    /// <summary>Height of the saturation/value square. A little less than its width, which costs
    /// nothing: neither axis is a measurement, both are just 0..1.</summary>
    public const float PickerSvHeight = 184f;

    public const float PickerBarThickness = 14f;
    public const float PickerBarSpacing = 6f;

    /// <summary>Height of the RGBA row and of the hex field: a member row's, since they hold the same
    /// label-sized text inset by the same amount. A shorter box leaves less room than one line needs,
    /// and Text truncates a line that does not fit rather than clipping it - the field draws empty.</summary>
    public const float PickerFieldRowHeight = RowHeight;

    public const float PickerWidth = PickerContentWidth + 2f * PickerPadding;

    //Derived from the parts rather than fixed, so adding or resizing one cannot leave a gap or clip
    //the bottom row.
    public const float PickerHeight = 2f * PickerPadding + PickerSvHeight
        + 2f * (PickerBarSpacing + PickerBarThickness)
        + 2f * (PickerBarSpacing + PickerFieldRowHeight);

    /// <summary>Diameter of the ring marking the picked point in the SV square.</summary>
    public const float PickerMarkerSize = 12f;

    /// <summary>Width of the line marking the picked point on the hue and alpha bars.</summary>
    public const float PickerMarkerThickness = 3f;

    /// <summary>Side of one square of the checkerboard the alpha bar is drawn over.</summary>
    public const float PickerCheckerCellSize = 5f;

    /// <summary>Opaque, unlike the panel: the picker is a popup over the panel and its own SV square
    /// has to be read as a colour, which a translucent backing would tint.</summary>
    public static readonly Color PickerBackground = new Color(0.13f, 0.13f, 0.13f, 1f);

    /// <summary>White, so a marker reads over the whole of the SV square and both bars. Nothing
    /// else in the picker is guaranteed to contrast with every colour it can be moved over.</summary>
    public static readonly Color PickerMarkerColor = Color.white;

    public static readonly Color PickerCheckerLight = new Color(0.8f, 0.8f, 0.8f, 1f);
    public static readonly Color PickerCheckerDark = new Color(0.55f, 0.55f, 0.55f, 1f);

    #endregion

    #region CSS export

    /// <summary>Name of the CSS custom property carrying <paramref name="token"/>, so a caller naming a
    /// single token spells it the same way this class emits it.</summary>
    public static string CssVariable(string token)
    {
        return "--genui-" + token;
    }

    /// <summary>
    /// Every value above as a `:root` block of `--genui-*` custom properties, for the web mirror to
    /// draw its rows with.
    /// </summary>
    /// <remarks>
    /// Generated rather than hand-written so the browser cannot drift from the panel: changing a
    /// metric here moves both. Ratios become percentages and lengths pixels, which is what the client
    /// uses them as; a colour that a panel's own bar tints (the title backing) is emitted as its alpha
    /// alone, since the bar colour is per-controllable and arrives with the schema.
    /// </remarks>
    public static string ToCss()
    {
        var css = new StringBuilder();
        css.Append(":root {\n");

        Length(css, "row-height", RowHeight);
        Length(css, "header-height", HeaderHeight);
        Length(css, "tooltip-height", TooltipHeight);
        Length(css, "panel-title-height", PanelTitleHeight);
        Length(css, "preset-row-height", PresetRowHeight);
        Length(css, "checkbox-size", CheckboxSize);
        Length(css, "panel-arrow-size", PanelArrowSize);
        Length(css, "panel-arrow-inset", PanelArrowInset);
        Length(css, "color-bar-width", ColorBarWidth);
        Length(css, "panel-padding", PanelPadding);
        Length(css, "preset-section-padding", PresetSectionPadding);
        Length(css, "preset-section-gap", PresetSectionGap);
        Length(css, "separator-space-above", SeparatorSpaceAbove);
        Length(css, "separator-thickness", SeparatorThickness);
        Length(css, "panel-bar-gap", PanelBarGap);
        Length(css, "axis-spacing", AxisSpacing);
        Length(css, "axis-label-gap", AxisLabelGap);
        Percent(css, "slider-track-start", SliderTrackStart);
        Percent(css, "slider-track-end", SliderTrackEnd);
        Percent(css, "slider-value-start", SliderValueStart);
        Length(css, "slider-track-inset", SliderTrackInset);
        Length(css, "slider-handle-width", SliderHandleWidth);
        Percent(css, "slider-band-min", SliderBandMin);
        Percent(css, "slider-band-max", SliderBandMax);
        Percent(css, "label-width-ratio", LabelWidthRatio);
        Length(css, "input-text-inset-x", InputTextInset.x);
        Length(css, "input-text-inset-y", InputTextInset.y);
        Length(css, "tooltip-bottom-spacing", TooltipBottomSpacing);
        Length(css, "method-gap-height", MethodGapHeight);
        Length(css, "panel-title-bottom-spacing", PanelTitleBottomSpacing);

        Length(css, "label-font-size", LabelFontSize);
        Length(css, "panel-title-font-size", PanelTitleFontSize);
        Length(css, "tooltip-font-size", TooltipFontSize);
        Length(css, "label-min-font-size", LabelMinFontSize);

        Rgba(css, "label-color", LabelColor);
        Rgba(css, "input-text-color", InputTextColor);
        Rgba(css, "placeholder-color", PlaceholderColor);
        Rgba(css, "tooltip-color", TooltipColor);
        Rgba(css, "panel-background", PanelBackground);
        Number(css, "panel-title-background-alpha", PanelTitleBackgroundAlpha);
        Rgba(css, "separator-color", SeparatorColor);
        Rgba(css, "toggle-on", ToggleOn);
        Rgba(css, "toggle-off", ToggleOff);
        Rgba(css, "dropdown-template-background", DropdownTemplateBackground);
        Rgba(css, "dropdown-item-background", DropdownItemBackground);
        Rgba(css, "dropdown-item-label", DropdownItemLabel);
        Rgba(css, "popup-backdrop", PopupBackdrop);
        Rgba(css, "caret-color", CaretColor);
        Rgba(css, "selection-color", SelectionColor);

        var controls = ControlColors();
        Rgba(css, "control-normal", controls.normalColor);
        Rgba(css, "control-highlighted", controls.highlightedColor);
        Rgba(css, "control-pressed", controls.pressedColor);
        Rgba(css, "control-selected", controls.selectedColor);
        Rgba(css, "control-disabled", controls.disabledColor);
        Seconds(css, "control-fade-duration", controls.fadeDuration);

        Length(css, "picker-padding", PickerPadding);
        Length(css, "picker-content-width", PickerContentWidth);
        Length(css, "picker-sv-height", PickerSvHeight);
        Length(css, "picker-bar-thickness", PickerBarThickness);
        Length(css, "picker-bar-spacing", PickerBarSpacing);
        Length(css, "picker-field-row-height", PickerFieldRowHeight);
        Length(css, "picker-width", PickerWidth);
        Length(css, "picker-height", PickerHeight);
        Length(css, "picker-marker-size", PickerMarkerSize);
        Length(css, "picker-marker-thickness", PickerMarkerThickness);
        Length(css, "picker-checker-cell-size", PickerCheckerCellSize);
        Rgba(css, "picker-background", PickerBackground);
        Rgba(css, "picker-marker-color", PickerMarkerColor);
        Rgba(css, "picker-checker-light", PickerCheckerLight);
        Rgba(css, "picker-checker-dark", PickerCheckerDark);

        css.Append("}\n");
        return css.ToString();
    }

    static void Declaration(StringBuilder css, string token, string value)
    {
        css.Append("  ").Append(CssVariable(token)).Append(": ").Append(value).Append(";\n");
    }

    static void Length(StringBuilder css, string token, float value)
    {
        Declaration(css, token, Format(value) + "px");
    }

    static void Percent(StringBuilder css, string token, float ratio)
    {
        Declaration(css, token, Format(ratio * 100f) + "%");
    }

    static void Seconds(StringBuilder css, string token, float value)
    {
        Declaration(css, token, Format(value) + "s");
    }

    static void Number(StringBuilder css, string token, float value)
    {
        Declaration(css, token, Format(value));
    }

    static void Rgba(StringBuilder css, string token, Color color)
    {
        Declaration(css, token, "rgba(" + Channel(color.r) + ", " + Channel(color.g) + ", " + Channel(color.b)
            + ", " + Format(color.a) + ")");
    }

    static int Channel(float value)
    {
        return Mathf.Clamp(Mathf.RoundToInt(value * 255f), 0, 255);
    }

    //Invariant culture throughout: a French editor would otherwise emit "0,5", which is not a CSS number.
    static string Format(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    #endregion
}
