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

    #region Widget lifetime

    public virtual void RemoveUI()
    {
        LinkedControllable.controllableValueChanged -= HandleTargetChange;

        Destroy(this.gameObject);
    }

    public virtual void HandleTargetChange(string name)
    {
    }

    #endregion

    #region Undo

    /// <summary>
    /// The value the widget's member holds right now, for the undo stack.
    /// </summary>
    /// <remarks>
    /// One boxed value is enough for every type GenUI draws: setFieldProp accepts a single Vector or
    /// Color as well as the loose components the widgets send it. The enum dropdown is the exception
    /// and overrides this.
    /// </remarks>
    public virtual UndoStack.Value CaptureValue()
    {
        return new UndoStack.Value(new List<object> { Property.GetValue(LinkedControllable) }, false);
    }

    /// <summary>
    /// Records what this widget's member holds, so Ctrl+Z can put it back. Call it at the top of a
    /// commit callback, while the member still holds the old value.
    /// </summary>
    protected void RecordUndo()
    {
        if (!CanRecordUndo())
            return;

        RecordUndo(CaptureValue());
    }

    /// <summary>
    /// Records a value captured earlier, for a widget whose edit spans a whole interaction rather
    /// than one callback - the colour picker, which is open for as long as the user is choosing.
    /// </summary>
    protected void RecordUndo(UndoStack.Value value)
    {
        if (!CanRecordUndo())
            return;

        UIMaster.Instance.Undo.Record(this, value, Time.unscaledTime);
    }

    bool CanRecordUndo()
    {
        if (Property == null || LinkedControllable == null || UIMaster.Instance == null)
            return false;

        //Setting currentPreset loads that preset, which rewrites every member of the controllable.
        //Undoing a preset selection would mean silently reloading the previous one, so a preset
        //choice is not treated as a value edit.
        return Property.Name != "currentPreset";
    }

    /// <summary>
    /// Whether the member already holds <paramref name="value"/>, making an undo to it a no-op.
    /// </summary>
    /// <remarks>
    /// InputField raises onEndEdit whenever it loses focus, whether or not the text changed, so
    /// leaving a field - by Tab, by clicking elsewhere - commits it and records an edit that changed
    /// nothing. Rather than have every widget work out whether its callback is a real change, those
    /// entries are recognised here and skipped when the stack is popped.
    /// </remarks>
    public virtual bool HoldsValue(UndoStack.Value value)
    {
        if (Property == null || LinkedControllable == null || value.Values == null || value.Values.Count != 1)
            return false;

        var current = CaptureValue();
        if (current.Values == null || current.Values.Count != 1)
            return false;

        return Equals(current.Values[0], value.Values[0]);
    }

    /// <summary>
    /// Restores a value taken from the undo stack.
    /// </summary>
    /// <remarks>
    /// It goes back through setFieldProp, the same path an edit takes, so the restore is clamped to
    /// [Range], written through to the target script, sent over OSC and redrawn in the widget without
    /// any of that being duplicated here.
    /// </remarks>
    public virtual void ApplyUndo(UndoStack.Value value)
    {
        if (Property == null || LinkedControllable == null)
            return;

        LinkedControllable.setFieldProp(Property, value.Values, value.IsEnum);
    }

    #endregion

    #region Fields the panel drives

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

    #endregion

    #region Naming and address

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

    #endregion
}
