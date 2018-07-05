using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class DropdownUI : ControllableUI
{
    public FieldInfo ListProperty;
    public FieldInfo ActiveElement;

    public int OrderDropDown(List<string> listInObject, string activeElementInObject)
    {
        var activeElementIndex = -1;
        for (var i = 0; i < listInObject.Count; i++)
        {
            if (activeElementInObject == listInObject[i].ToString())
                activeElementIndex = i;
        }
        //Switch active element in list to be the first one, so the displayed one in dropdown
        if (activeElementIndex != -1 && listInObject.Count > 1)
        {
            var tmp = listInObject[0];
            listInObject[0] = listInObject[activeElementIndex];
            listInObject[activeElementIndex] = tmp;
        }

        return activeElementIndex;
    }

    // Use this for initialization
    public void CreateUI(Controllable target, FieldInfo listProperty, FieldInfo activeElement) {

        ListProperty = listProperty;
        ActiveElement = activeElement;
        LinkedControllable = target;
        target.controllableValueChanged += HandleTargetChange;

        var listInObject = (List<string>)ListProperty.GetValue(target);
        var activeElementIndex = OrderDropDown(listInObject, ActiveElement.GetValue(target).ToString());

        this.GetComponent<Dropdown>().value = 0;

        this.GetComponent<Dropdown>().AddOptions(listInObject);
        this.GetComponent<Dropdown>().onValueChanged.AddListener((value) =>
        {
            var associatedList = (List<string>)ListProperty.GetValue(target);
            string activeItem = associatedList[value];

            List<object> objParams = new List<object> { activeItem };
            target.setFieldProp(ActiveElement, objParams);
        });
    }

    public override void HandleTargetChange(string name)
    {
        OrderDropDown((List<string>)ListProperty.GetValue(LinkedControllable), ActiveElement.GetValue(LinkedControllable).ToString());

        this.GetComponent<Dropdown>().ClearOptions();
        this.GetComponent<Dropdown>().AddOptions((List<string>)ListProperty.GetValue(LinkedControllable));
    }
}
