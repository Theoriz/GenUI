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

    /// <summary>A numeric field and the label whose drag scrubs it.</summary>
    public struct ScrubTarget
    {
        public InputField Field;
        public Text Label;

        public ScrubTarget(InputField field, Text label)
        {
            Field = field;
            Label = label;
        }
    }

    static readonly InputField[] _noInputFields = new InputField[0];
    static readonly ScrubTarget[] _noScrubTargets = new ScrubTarget[0];

    /// <summary>
    /// The widget's numeric fields, each paired with the label that scrubs it. Empty when the widget
    /// holds nothing numeric.
    /// </summary>
    /// <remarks>
    /// Paired rather than returned as a second array alongside <see cref="GetInputFields"/>: two
    /// arrays that have to stay index-aligned are the kind of thing that silently mis-pairs later.
    /// The label is the drag target because InputField activates editing on pointer-down and handles
    /// its own drag for text selection, so a handler on the field would do both at once.
    /// </remarks>
    public virtual ScrubTarget[] GetScrubTargets()
    {
        return _noScrubTargets;
    }

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
