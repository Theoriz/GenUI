using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class ButtonUI : ControllableUI
{
    public void CreateUI(Controllable target, ClassMethodInfo method)
    {
        LinkedControllable = target;
        Method = method.methodInfo;

        this.GetComponentInChildren<Text>().text = ParseNameString(method.methodInfo.Name);
        this.GetComponent<Button>().onClick.AddListener(() =>
        {
            target.setMethodProp(method, null);
        });
    }

    public override void RemoveUI()
    {
    }
}
