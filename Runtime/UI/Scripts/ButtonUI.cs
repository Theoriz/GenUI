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
        //A method has no read-only form: the button invokes it and its address is callable over OSC.
        IsInteractible = true;

        this.GetComponentInChildren<Text>().text = ParseNameString(method.methodInfo.Name);
        this.GetComponent<Button>().onClick.AddListener(() =>
        {
            target.SetMethodProp(method, null);
        });
    }

    public override void RemoveUI()
    {
    }
}
