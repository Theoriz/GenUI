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

    /// <summary>
    /// Creates the widget's row and everything in it, then hands it to the panel it belongs to.
    /// </summary>
    /// <remarks>
    /// The caller follows this with the widget's own CreateUI, which binds it to a member. Splitting
    /// the two keeps every widget's structure in the file that reads it back: BuildHierarchy creates
    /// the children and caches them, so nothing has to find them again by index or by name.
    /// </remarks>
    public static T Create<T>(Transform parent) where T : ControllableUI
    {
        var rect = UIFactory.CreateRect(typeof(T).Name, parent);

        var widget = rect.gameObject.AddComponent<T>();
        rect.sizeDelta = new Vector2(0f, widget.WidgetHeight);
        widget.BuildHierarchy();
        widget.AddRowMouseEvent();

        var panel = parent.GetComponent<PanelUI>();
        if (panel != null)
            panel.AddUIElement(widget);

        return widget;
    }

    /// <summary>
    /// How tall the widget's row is. The panel's layout group does not control child heights, so
    /// this is what the row gets.
    /// </summary>
    protected virtual float WidgetHeight { get { return GenUIStyle.RowHeight; } }

    /// <summary>
    /// Creates the widget's children and caches them. Called once, before CreateUI.
    /// </summary>
    /// <remarks>
    /// A part that answers the mouse is given its MouseButtonEvent here, through
    /// UIFactory.AddMouseEvent, so linkedUI is never left to be repaired afterwards. A part needs one
    /// only when it takes the press away from the row - every Selectable does - or when it answers a
    /// click the row must not, since <see cref="AddRowMouseEvent"/> covers the row itself.
    /// </remarks>
    protected virtual void BuildHierarchy()
    {
    }

    /// <summary>
    /// Makes the whole row answer the mouse, so a right click copies the OSC address wherever it
    /// lands: over the label, over the control, or over the gap between them.
    /// </summary>
    /// <remarks>
    /// Added here rather than by each widget, so no member type can answer the mouse differently
    /// from another. A Selectable takes the press over its own area, so the widgets that build one
    /// add their own event as well and this one catches everything they do not.
    /// The backing graphic is what makes the bare parts of the row raycastable at all. It goes on the
    /// row itself, behind every child, so it cannot swallow a click meant for a control - which a
    /// child overlay, drawn in front, would. A widget that already draws a row-wide graphic keeps it:
    /// a GameObject holds only one Graphic.
    /// </remarks>
    void AddRowMouseEvent()
    {
        if (GetComponent<Graphic>() == null)
            UIFactory.AddImage(gameObject, null, Color.clear);

        UIFactory.AddMouseEvent(gameObject, this);
    }

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
    /// One boxed value is enough for every type GenUI draws: SetFieldProp accepts a single Vector,
    /// Color or enum member as well as the loose components the widgets send it.
    /// </remarks>
    public virtual UndoStack.Value CaptureValue()
    {
        return new UndoStack.Value(new List<object> { Property.GetValue(LinkedControllable) });
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

        //Setting controllableCurrentPreset loads that preset, which rewrites every member of the controllable.
        //Undoing a preset selection would mean silently reloading the previous one, so a preset
        //choice is not treated as a value edit.
        return Property.Name != "controllableCurrentPreset";
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
    /// It goes back through SetFieldProp, the same path an edit takes, so the restore is clamped to
    /// [Range], written through to the target script, sent over OSC and redrawn in the widget without
    /// any of that being duplicated here.
    /// </remarks>
    public virtual void ApplyUndo(UndoStack.Value value)
    {
        if (Property == null || LinkedControllable == null)
            return;

        LinkedControllable.SetFieldProp(Property, value.Values);
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

    /// <summary>
    /// Turns the widget's fields into a display when its member is read-only: they keep showing the
    /// value and stop accepting input. Call it at the end of CreateUI.
    /// </summary>
    protected void ApplyReadOnlyLook()
    {
        if (IsInteractible)
            return;

        foreach (var field in GetInputFields())
            MakeDisplayOnly(field);
    }

    /// <summary>
    /// Makes a control show its value without offering to change it: not interactable, and with no
    /// frame left around it.
    /// </summary>
    /// <remarks>
    /// The frame goes by tinting the disabled state to nothing rather than by hiding the graphic, so
    /// one call covers every widget whatever disabled colour its prefab carries - which is what makes
    /// every read-only row read the same. Public and static because a Dropdown needs it too, and it
    /// is not one of the fields GetInputFields returns.
    /// </remarks>
    public static void MakeDisplayOnly(Selectable selectable)
    {
        if (selectable == null)
            return;

        var colors = selectable.colors;
        colors.disabledColor = Color.clear;
        selectable.colors = colors;

        selectable.interactable = false;
    }

    #endregion

    #region Naming and address

    /// <summary>
    /// Whether the widget stands for a member at all. A header and a tooltip are rows like any
    /// other, but there is nothing about them to control or to copy an address for.
    /// </summary>
    public bool HasAddress
    {
        get { return LinkedControllable != null && (Property != null || Method != null); }
    }

    public void CopyAddressToClipboard()
    {
        GUIUtility.systemCopyBuffer = "/" + ControllableMaster.instance.RootOSCAddress + "/" + LinkedControllable.controllableId + "/" + (Property == null ? Method.Name : Property.Name) ;
    }

    static readonly Regex _nameRegex = new Regex(@"
                (?<=[A-Z])(?=[A-Z][a-z]) |
                 (?<=[^A-Z])(?=[A-Z]) |
                 (?<=[A-Za-z])(?=[^A-Za-z])", RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

    //Every member Controllable itself declares is prefixed, so without stripping it the preset row
    //would read "Controllable Save" and "Controllable Current Preset". Matched case-insensitively
    //because the prefix is spelled both ways: fields are serialized and stay "controllableCurrentPreset",
    //while methods are named "ControllableSave". The next character must be upper case, which is what
    //keeps a user member such as "controllablething" intact - only the label is affected either way,
    //never the OSC address.
    const string ControllablePrefix = "controllable";

    static string StripControllablePrefix(string name)
    {
        if (name.Length <= ControllablePrefix.Length) return name;
        if (!name.StartsWith(ControllablePrefix, System.StringComparison.OrdinalIgnoreCase)) return name;
        if (!char.IsUpper(name[ControllablePrefix.Length])) return name;

        return name.Substring(ControllablePrefix.Length);
    }

    public string ParseNameString(string name) {

        if (string.IsNullOrEmpty(name))
            return name;

        string output = StripControllablePrefix(name);

        output = char.ToUpper(output[0]) + output.Substring(1);

        return _nameRegex.Replace(output, " ");

    }

    #endregion
}
