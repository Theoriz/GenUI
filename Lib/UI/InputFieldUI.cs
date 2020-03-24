using System.Collections;
using System;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class InputFieldUI : ControllableUI
{


    // Use this for initialization
    public void CreateUI(Controllable target, FieldInfo property, bool isInteractible) {

        LinkedControllable = target;
        IsInteractible = isInteractible;
        Property = property;
        target.controllableValueChanged += HandleTargetChange;

        var inputFieldComponent = this.transform.GetComponentInChildren<InputField>();
        var textComponent = this.transform.GetChild(1).gameObject.GetComponent<Text>();

        textComponent.text = ParseNameString(property.Name);
        this.transform.GetComponentInChildren<InputField>().interactable = isInteractible;

        if (property.FieldType.ToString() == "System.Int32")
            inputFieldComponent.contentType = InputField.ContentType.IntegerNumber;
        if (property.FieldType.ToString() == "System.Single")
            inputFieldComponent.contentType = InputField.ContentType.DecimalNumber;
        if (property.FieldType.ToString() == "System.String")
            inputFieldComponent.contentType = InputField.ContentType.Standard;

        var str = "" + property.GetValue(target).ToString();
        str = str.Replace(",", ".");
        inputFieldComponent.text = "" + str; 

        inputFieldComponent.onEndEdit.AddListener((value) =>
        {
            var list = new List<object>();
            var propertyType = property.FieldType;
            if (ShowDebug)
            {
                Debug.Log("Property type : " + propertyType.ToString());
                Debug.Log("Value : " + value + " size : " + value.Length);
            }
            if (propertyType.ToString() == "System.Int32")
            {
                var result = 0;
                try { result = int.Parse(value, CultureInfo.InvariantCulture);}
                catch (Exception e) { Debug.Log(e.Message); result = 0; }
                list.Add(result);
            }
            else if (propertyType.ToString() == "System.Single")
            {
                var result = 0.0f;
                try { result = float.Parse(value.ToString(), CultureInfo.InvariantCulture); }
                catch (Exception e) { Debug.Log(e.Message); result = 0.0f; }
                list.Add(result);
            }
            else if (propertyType.ToString() == "System.String")
                list.Add(value);

            target.setFieldProp(property, list);
        });

        this.transform.GetChild(0).Find("Text").gameObject.GetComponent<Text>().color = Color.white;
        this.transform.GetChild(0).Find("Placeholder").gameObject.GetComponent<Text>().text = target.getPropInfoForAddress(property.Name).GetValue(target).ToString();
        this.GetComponent<RectTransform>().localScale = new Vector3(1.0f, 1.0f, 1.0f);
    }

    public override void HandleTargetChange(string name)
    {
        if (name != Property.Name && !String.IsNullOrEmpty(name))
            return;
        
        var str = "" + Property.GetValue(LinkedControllable);
        str = str.Replace(",", ".");
        this.transform.GetComponentInChildren<InputField>().text = "" + str;
        
    }
}
