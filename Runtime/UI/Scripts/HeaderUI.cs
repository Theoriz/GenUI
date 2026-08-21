using UnityEngine;
using UnityEngine.UI;

public class HeaderUI : ControllableUI
{
    Text _label;

    protected override float WidgetHeight { get { return GenUIStyle.HeaderHeight; } }

    protected override void BuildHierarchy()
    {
        //On the row itself: a header is nothing but its text.
        _label = UIFactory.AddText(gameObject, string.Empty, GenUIStyle.LabelFontSize,
            TextAnchor.MiddleCenter, GenUIStyle.LabelColor, bold: true);
    }

    public void CreateUI(Controllable target, string text)
    {
        LinkedControllable = target;
        _label.text = text;
    }

    public override void RemoveUI()
    {
    }
}
