using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Which widget an exposed member gets, decided once. UIMaster renders a descriptor; anything else
/// that has to draw the same members (the web mirror) renders the same descriptors, so a new
/// supported type is added in one place and both interfaces agree by construction.
/// </summary>
public enum WidgetKind
{
    None,
    Slider,
    Input,
    Toggle,
    Dropdown,
    Color,
    Vector2,
    Vector2Int,
    Vector3,
    Vector3Int,
    Vector4
}

public struct MemberDescriptor
{
    public WidgetKind Kind;

    public string Name;
    public string Label;
    public string Header;
    public string Tooltip;

    //Set when the dropdown draws a named List<string>; null when it draws an enum.
    public string TargetList;

    public bool ReadOnly;

    //Valid when Kind is Slider or Input: an int member formats and clamps as a whole number.
    public bool IsFloat;

    //Valid when Kind == Slider.
    public float Min;
    public float Max;

    //Valid when Kind == Dropdown and TargetList is null.
    public Type EnumType;

    //Non-null exactly when Kind == None: the warning the caller logs, already naming the member.
    public string SkipReason;

    /// <summary>
    /// The type dispatch. Order matters and matches the chain it replaces: targetList wins over the
    /// member's own type, so a string chosen from a list is a dropdown rather than an input field.
    /// </summary>
    public static MemberDescriptor Describe(Controllable controllable, FieldInfo field, OCFProperty attribute)
    {
        var descriptor = new MemberDescriptor
        {
            Kind = WidgetKind.None,
            Name = field.Name,
            Label = ControllableUI.ParseNameString(field.Name),
            ReadOnly = attribute != null && attribute.readOnly,
            IsFloat = true
        };

        var header = (HeaderAttribute[])field.GetCustomAttributes(typeof(HeaderAttribute), false);
        if (header.Length != 0)
            descriptor.Header = header[0].header;

        var tooltip = (TooltipAttribute[])field.GetCustomAttributes(typeof(TooltipAttribute), false);
        if (tooltip.Length != 0)
            descriptor.Tooltip = tooltip[0].tooltip;

        var type = field.FieldType;
        var targetList = attribute != null ? attribute.targetList : null;

        if (!string.IsNullOrEmpty(targetList))
        {
            //The name is passed on rather than a resolved FieldInfo: the list may live on the
            //mirror or on the target script, and its entries are read live on every refresh.
            if (controllable == null || controllable.GetTargetList(targetList) == null)
                descriptor.SkipReason = SkipMessage(controllable, field, "targetList '" + targetList
                    + "' names no List<string> on the controllable or its target script.");
            else
            {
                descriptor.Kind = WidgetKind.Dropdown;
                descriptor.TargetList = targetList;
            }

            return descriptor;
        }

        if (type.IsEnum)
        {
            //A [Flags] enum holds a combination of its members, which one dropdown cannot show.
            //Drawing a single-select control over it would silently discard every flag it leaves
            //out, so the member is left to OSC and presets instead.
            if (type.IsDefined(typeof(FlagsAttribute), false))
                descriptor.SkipReason = SkipMessage(controllable, field, type.Name
                    + " is a [Flags] enum. It stays controllable over OSC.");
            else
            {
                descriptor.Kind = WidgetKind.Dropdown;
                descriptor.EnumType = type;
            }

            return descriptor;
        }

        if (type == typeof(float) || type == typeof(int))
        {
            descriptor.IsFloat = type != typeof(int);

            var range = (RangeAttribute[])field.GetCustomAttributes(typeof(RangeAttribute), false);
            if (range.Length == 0)
                descriptor.Kind = WidgetKind.Input;
            else
            {
                descriptor.Kind = WidgetKind.Slider;
                descriptor.Min = range[0].min;
                descriptor.Max = range[0].max;
            }

            return descriptor;
        }

        if (type == typeof(bool)) descriptor.Kind = WidgetKind.Toggle;
        else if (type == typeof(string)) descriptor.Kind = WidgetKind.Input;
        else if (type == typeof(Color)) descriptor.Kind = WidgetKind.Color;
        else if (type == typeof(Vector3)) descriptor.Kind = WidgetKind.Vector3;
        else if (type == typeof(Vector4)) descriptor.Kind = WidgetKind.Vector4;
        else if (type == typeof(Vector3Int)) descriptor.Kind = WidgetKind.Vector3Int;
        else if (type == typeof(Vector2)) descriptor.Kind = WidgetKind.Vector2;
        else if (type == typeof(Vector2Int)) descriptor.Kind = WidgetKind.Vector2Int;
        else descriptor.SkipReason = SkipMessage(controllable, field, "unsupported type " + type + ".");

        return descriptor;
    }

    static string SkipMessage(Controllable controllable, FieldInfo field, string reason)
    {
        var id = controllable != null ? controllable.controllableId : "(no controllable)";
        return "[GenUI] No widget created for '" + field.Name + "' on " + id + " : " + reason;
    }
}
