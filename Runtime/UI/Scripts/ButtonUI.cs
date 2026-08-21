using UnityEngine;
using UnityEngine.UI;

public class ButtonUI : ControllableUI
{
    Button _button;
    Text _label;

    protected override void BuildHierarchy()
    {
        _button = UIFactory.AddButton(gameObject, GenUIAssets.Instance.Box, Color.white);
        _button.colors = GenUIStyle.ControlColors();

        var labelRect = UIFactory.CreateChild("Label", transform);
        _label = UIFactory.AddText(labelRect.gameObject, string.Empty, GenUIStyle.LabelFontSize,
            TextAnchor.MiddleCenter, GenUIStyle.LabelColor);
    }

    public void CreateUI(Controllable target, ClassMethodInfo method)
    {
        LinkedControllable = target;
        Method = method.methodInfo;
        //A method has no read-only form: the button invokes it and its address is callable over OSC.
        IsInteractible = true;

        _label.text = ParseNameString(method.methodInfo.Name);
        _button.onClick.AddListener(() =>
        {
            target.SetMethodProp(method, null);
        });
    }

    public override void RemoveUI()
    {
    }
}
