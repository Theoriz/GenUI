using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Globalization;

public class TooltipUI : ControllableUI
{
    public void CreateUI(Controllable target, string text)
    {
        LinkedControllable = target;
        this.GetComponent<Text>().text = text;
    }

    public override void RemoveUI()
    {
    }
}
