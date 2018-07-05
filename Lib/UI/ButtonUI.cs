using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class ButtonUI : ControllableUI
{
    public MethodInfo Method;

    public void CreateUI(Controllable target, MethodInfo method)
    {
        LinkedControllable = target;
        Method = method;

        this.GetComponentInChildren<Text>().text = method.Name;
        this.GetComponent<Button>().onClick.AddListener(() =>
        {
            target.setMethodProp(method, method.Name, null);
        });
    }

    public override void RemoveUI()
    {
    }
}
