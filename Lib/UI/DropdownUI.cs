using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class DropdownUI : ControllableUI
{
    public FieldInfo ListProperty;

    public int GetActiveElementIndex(List<string> listInObject, string activeElementInObject)
    {
        var activeElementIndex = -1;
        for (var i = 0; i < listInObject.Count; i++)
        {
            if (activeElementInObject == listInObject[i].ToString())
                activeElementIndex = i;
        }

        return activeElementIndex;
    }

    // Use this for initialization
    public void CreateUI(Controllable target, FieldInfo listProperty, FieldInfo activeElement) {

        ListProperty = listProperty;
        Property = activeElement;
        LinkedControllable = target;
        LinkedControllable.controllableValueChanged += HandleTargetChange;

        var listInObject = (List<string>)ListProperty.GetValue(LinkedControllable);
        var activeElementIndex = GetActiveElementIndex(listInObject, Property.GetValue(LinkedControllable).ToString());

        var dropdown = this.GetComponent<Dropdown>();
        dropdown.value = 0;
        dropdown.AddOptions(listInObject);
        dropdown.onValueChanged.AddListener((value) =>
        {
            var associatedList = (List<string>)ListProperty.GetValue(LinkedControllable);
            string activeItem = associatedList[value];

            List<object> objParams = new List<object> { activeItem };
            LinkedControllable.setFieldProp(Property, objParams);
        });
    }

    public override void HandleTargetChange(string name)
    {
        var dropdown = this.GetComponent<Dropdown>();
        dropdown.ClearOptions();
        dropdown.AddOptions((List<string>)ListProperty.GetValue(LinkedControllable));
        dropdown.value = GetActiveElementIndex((List<string>)ListProperty.GetValue(LinkedControllable), Property.GetValue(LinkedControllable).ToString());
    }
}
