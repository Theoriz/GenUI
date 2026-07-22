using System.Collections.Generic;

/// <summary>
/// The values edited in the UI, oldest first, so Ctrl+Z can put the last one back.
/// </summary>
/// <remarks>
/// Only edits made in the UI are recorded. Values arriving over OSC or from a preset go through
/// <c>Controllable.setFieldProp</c> like UI edits do, but recording those would let a source writing
/// every frame fill the stack and fight the user - which is why widgets record, and setFieldProp
/// does not.
/// </remarks>
public class UndoStack
{
    /// <summary>A member's value in the shape <c>setFieldProp</c> takes it.</summary>
    public struct Value
    {
        public List<object> Values;

        public Value(List<object> values)
        {
            Values = values;
        }
    }

    /// <summary>One undoable edit: what the widget held before it.</summary>
    public struct Entry
    {
        public ControllableUI Widget;
        public Value Value;
        public float Time;
    }

    //Roughly "everything done in this sitting". An entry is a reference, a one-element list and two
    //primitives, and nothing reads the stack per frame, so the depth is a matter of taste rather
    //than of cost.
    public const int MaxDepth = 50;

    //A slider drag raises onValueChanged every frame and a scrub writes on every pixel step that
    //changes the text, so consecutive edits to one widget this close together are one gesture and
    //must undo as one. Two deliberate edits to the same field are always further apart: the second
    //needs the field refocused and retyped.
    public const float CoalesceWindow = 0.5f;

    readonly List<Entry> _entries = new List<Entry>();

    public int Count { get { return _entries.Count; } }

    /// <summary>
    /// Records what <paramref name="widget"/> held before the edit being made now.
    /// </summary>
    public void Record(ControllableUI widget, Value value, float now)
    {
        if (widget == null)
            return;

        //Same widget, still the same gesture: keep the value from before the gesture started and
        //just extend it, so the whole drag undoes in one press.
        if (_entries.Count > 0)
        {
            var top = _entries[_entries.Count - 1];
            if (top.Widget == widget && now - top.Time <= CoalesceWindow)
            {
                top.Time = now;
                _entries[_entries.Count - 1] = top;
                return;
            }
        }

        _entries.Add(new Entry { Widget = widget, Value = value, Time = now });

        if (_entries.Count > MaxDepth)
            _entries.RemoveAt(0);
    }

    /// <summary>
    /// The most recent edit whose widget still exists, or false when there is nothing left to undo.
    /// </summary>
    /// <remarks>
    /// Two kinds of entry are dropped on the way out rather than returned as a press that appears to
    /// do nothing: those whose widget no longer exists, since panels are created and destroyed at
    /// runtime, and those the member already holds - see <see cref="ControllableUI.HoldsValue"/>.
    /// </remarks>
    public bool TryPop(out Entry entry)
    {
        while (_entries.Count > 0)
        {
            var candidate = _entries[_entries.Count - 1];
            _entries.RemoveAt(_entries.Count - 1);

            if (candidate.Widget != null && !candidate.Widget.HoldsValue(candidate.Value))
            {
                entry = candidate;
                return true;
            }
        }

        entry = default(Entry);
        return false;
    }
}
