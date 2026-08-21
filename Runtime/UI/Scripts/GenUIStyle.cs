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
}
