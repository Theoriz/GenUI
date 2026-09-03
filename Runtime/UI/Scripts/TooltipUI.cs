using UnityEngine;
using UnityEngine.UI;

public class TooltipUI : ControllableUI, ILayoutElement
{
    //Empty space kept under the text, so a tooltip reads as belonging to the widget above it rather
    //than to the one below. Reported as part of the preferred height (see below).
    public float bottomSpacing = GenUIStyle.TooltipBottomSpacing;

    private Text _label;

    protected override float WidgetHeight { get { return GenUIStyle.TooltipHeight; } }

    protected override void BuildHierarchy()
    {
        //Anchored to the top, so the extra height the fitter adds all lands under the text instead
        //of being split above and below it.
        _label = UIFactory.AddText(gameObject, string.Empty, GenUIStyle.TooltipFontSize,
            TextAnchor.UpperLeft, GenUIStyle.TooltipColor);

        //The panel's layout group leaves child heights alone, so the tooltip would keep its row
        //height and cut off anything that wraps or follows a line break. The fitter grows it to the
        //height reported below instead.
        _label.verticalOverflow = VerticalWrapMode.Overflow;

        var fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    public void CreateUI(Controllable target, string text)
    {
        LinkedControllable = target;
        _label.text = text;
    }

    public override void RemoveUI()
    {
    }

    //ILayoutElement: the ContentSizeFitter above sizes the tooltip to the highest-priority preferred
    //height on this object. Text reports the bare wrapped text at priority 0; this reports the same
    //plus the spacing at priority 1, so it wins. Everything else returns -1, which the layout system
    //reads as "no opinion" and falls back to Text.
    public void CalculateLayoutInputHorizontal() { }
    public void CalculateLayoutInputVertical() { }

    public float minWidth { get { return -1; } }
    public float preferredWidth { get { return -1; } }
    public float flexibleWidth { get { return -1; } }
    public float minHeight { get { return -1; } }
    public float preferredHeight { get { return _label == null ? -1 : _label.preferredHeight + bottomSpacing; } }
    public float flexibleHeight { get { return -1; } }
    public float maxWidth { get { return -1; } }
    public float maxHeight { get { return -1; } }
    public int layoutPriority { get { return 1; } }
}
