using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.Text.RegularExpressions;

public class ControllableUI : MonoBehaviour {

    public Controllable LinkedControllable;
    public FieldInfo Property;
    public MethodInfo Method;

    public bool IsInteractible;

    public bool ShowDebug;

    public virtual void RemoveUI()
    {
        LinkedControllable.controllableValueChanged -= HandleTargetChange;

        Destroy(this.gameObject);
    }

    public virtual void HandleTargetChange(string name)
    {
    }

    public void CopyAddressToClipboard()
    {

        GUIUtility.systemCopyBuffer = "/" + ControllableMaster.instance.RootOSCAddress + "/" + LinkedControllable.id + "/" + (Property == null ? Method.Name : Property.Name) ;
    }

    public string ParseNameString(string name) {

        var r = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace);

        string output = char.ToUpper(name[0]) + name.Substring(1);

        return r.Replace(output, " ");

    }
}
