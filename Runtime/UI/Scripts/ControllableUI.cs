using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Text.RegularExpressions;

public class ControllableUI : MonoBehaviour {

    public Controllable LinkedControllable;
    [System.NonSerialized] public FieldInfo Property;
    [System.NonSerialized] public MethodInfo Method;

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

    static readonly InputField[] _noInputFields = new InputField[0];

    /// <summary>
    /// The widget's editable fields, in the order Tab should visit them.
    /// </summary>
    /// <remarks>
    /// Widgets return them explicitly rather than letting callers search the hierarchy: the vector
    /// widgets find their inputs by name, so nothing else guarantees x, y, z, w order.
    /// </remarks>
    public virtual InputField[] GetInputFields()
    {
        return _noInputFields;
    }

    public void CopyAddressToClipboard()
    {
        GUIUtility.systemCopyBuffer = "/" + ControllableMaster.instance.RootOSCAddress + "/" + LinkedControllable.id + "/" + (Property == null ? Method.Name : Property.Name) ;
    }

    static readonly Regex _nameRegex = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

    public string ParseNameString(string name) {

        if (string.IsNullOrEmpty(name))
            return name;

        string output = char.ToUpper(name[0]) + name.Substring(1);

        return _nameRegex.Replace(output, " ");

    }
}
