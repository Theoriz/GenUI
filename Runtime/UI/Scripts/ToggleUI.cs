using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System;

public class ToggleUI : ControllableUI
{
    public Toggle toggle;

    Text _label;

    protected override void BuildHierarchy()
    {
        _label = UIFactory.CreateLabel(transform);

        //The box sits at the left of the control half, the width of the row it stands in.
        var box = UIFactory.CreateSlice("Background", transform, GenUIStyle.LabelWidthRatio, GenUIStyle.LabelWidthRatio + 0.13f);
        var background = UIFactory.AddImage(box.gameObject, GenUIAssets.Instance.Box, Color.white);

        var checkRect = UIFactory.CreateCentered("Checkmark", box, GenUIStyle.CheckboxSize, GenUIStyle.CheckboxSize);
        var checkmark = UIFactory.AddImage(checkRect.gameObject, GenUIAssets.Instance.Checkmark, Color.white, Image.Type.Simple);

        //On the row, not on the box: the label is a raycast target too, so clicking the name toggles
        //the value as well.
        toggle = gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = checkmark;
    }

    public void CreateUI(Controllable target, FieldInfo property, bool isInteractible)
    {
        Property = property;
        IsInteractible = isInteractible;
        LinkedControllable = target;
        target.controllableValueChanged += HandleTargetChange;
        HandleTargetChange(property.Name); //To set color

        _label.text = ParseNameString(property.Name);

        toggle.isOn = (bool)property.GetValue(target);
        toggle.interactable = isInteractible;
        toggle.onValueChanged.AddListener((value) =>
        {
            RecordUndo();

            var list = new List<object>();
            list.Add(value);
            target.SetFieldProp(property, list);
        });
    }

    public override void HandleTargetChange(string name)
    {
        if (name != Property.Name && !String.IsNullOrEmpty(name))
            return;

        var newValue = (bool)Property.GetValue(LinkedControllable);

        //Without notify: this is the widget catching up with a value that has already been written,
        //so raising onValueChanged would write it straight back and record an edit the user never made.
        toggle.SetIsOnWithoutNotify(newValue);

        //The disabled tint is the read-only look of a bool: a member that cannot be edited still
        //reads at a glance as green for true, red for false.
        var blockColors = toggle.colors;
        blockColors.disabledColor = newValue ? GenUIStyle.ToggleOn : GenUIStyle.ToggleOff;
        toggle.colors = blockColors;
    }
}
