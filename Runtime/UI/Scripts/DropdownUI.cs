using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System;

public class DropdownUI : ControllableUI
{
    //Set by whichever CreateUI ran: a list-backed dropdown carries the name of the List<string> its
    //entries come from, an enum-backed one carries the enum's Type.
    [System.NonSerialized] public string TargetListName;
    [System.NonSerialized] public Type enumType = null;

    //An enum's members never change, so its options and values are read once. The list route has to
    //re-read its entries instead: they can be added to at runtime, as controllablePresetList is.
    string[] _enumNames;
    Array _enumValues;

    #region Creation

    /// <summary>A dropdown over the entries of a <c>List&lt;string&gt;</c> named by `targetList`.</summary>
    public void CreateUI(Controllable target, string targetListName, FieldInfo activeElement) {

        TargetListName = targetListName;
        Property = activeElement;
        LinkedControllable = target;
        LinkedControllable.controllableValueChanged += HandleTargetChange;

        var text = this.transform.GetChild(0).GetComponent<Text>();
        text.text = ParseNameString(activeElement.Name);

        var dropdown = this.GetComponentInChildren<Dropdown>();
        dropdown.AddOptions(GetListEntries());
        //SetValueWithoutNotify: only a genuine user selection should fire onValueChanged (which loads
        //the selected preset). Programmatic updates here and in HandleTargetChange must not.
        dropdown.SetValueWithoutNotify(Mathf.Max(0, GetSelectedListIndex()));
        dropdown.onValueChanged.AddListener((value) =>
        {
            RecordUndo();

            var entries = GetListEntries();
            if (value < 0 || value >= entries.Count)
                return;

            List<object> objParams = new List<object> { entries[value] };
            LinkedControllable.SetFieldProp(Property, objParams);
        });
    }

    /// <summary>A dropdown over the members of an enum, taken from the member's own type.</summary>
    public void CreateUI(Controllable target, FieldInfo activeElement, Type _enumType)
    {
        Property = activeElement;
        LinkedControllable = target;
        LinkedControllable.controllableValueChanged += HandleTargetChange;

        enumType = _enumType;
        _enumNames = Enum.GetNames(enumType);
        _enumValues = Enum.GetValues(enumType);

        var text = this.transform.GetChild(0).GetComponent<Text>();
        text.text = ParseNameString(activeElement.Name);

        var dropdown = this.GetComponentInChildren<Dropdown>();
        dropdown.AddOptions(_enumNames.ToList());
        dropdown.SetValueWithoutNotify(Mathf.Max(0, GetSelectedEnumIndex()));
        dropdown.onValueChanged.AddListener((value) =>
        {
            RecordUndo();

            if (value < 0 || value >= _enumValues.Length)
                return;

            //The member itself, not its position: an enum numbered explicitly (Spot = 5) would
            //otherwise store whichever member happens to sit at that index.
            List<object> objParams = new List<object> { _enumValues.GetValue(value) };
            LinkedControllable.SetFieldProp(Property, objParams);
        });
    }

    #endregion

    #region Selection

    List<string> GetListEntries()
    {
        return LinkedControllable.GetTargetList(TargetListName) ?? new List<string>();
    }

    int GetSelectedListIndex()
    {
        var current = Property.GetValue(LinkedControllable);
        return TypeConverter.GetIndexInEnum(GetListEntries(), current == null ? "" : current.ToString());
    }

    int GetSelectedEnumIndex()
    {
        return Array.IndexOf(_enumValues, Property.GetValue(LinkedControllable));
    }

    public override void HandleTargetChange(string name)
    {
        if (name != Property.Name && !String.IsNullOrEmpty(name))
            return;

        var dropdown = this.GetComponentInChildren<Dropdown>();

        if (enumType != null)
        {
            dropdown.SetValueWithoutNotify(Mathf.Max(0, GetSelectedEnumIndex()));
            return;
        }

        //The entries themselves can have changed - controllablePresetList grows every time a preset is saved.
        dropdown.ClearOptions();
        dropdown.AddOptions(GetListEntries());
        dropdown.SetValueWithoutNotify(Mathf.Max(0, GetSelectedListIndex()));
    }

    #endregion
}
