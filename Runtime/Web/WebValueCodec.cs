using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// A member's value as the browser sees it, and back again.
/// </summary>
/// <remarks>
/// Deliberately not <c>Controllable.GetData()</c>'s <c>"F8"</c> strings: those exist for the preset
/// file format, where everything is text. Here a number is a JSON number, a vector or a colour an
/// array of them, and an enum its member name - which is what a browser can render and edit without
/// parsing Unity's <c>ToString</c> forms.
///
/// The types handled here are the ones <see cref="MemberDescriptor.Describe"/> gives a widget to and
/// <c>Controllable.SetFieldProp</c> can write, and the three lists have to keep agreeing.
/// Inbound values are handed back as the <c>List&lt;object&gt;</c> that <c>SetFieldProp</c> takes, so
/// clamping, read-only refusal and write-through all stay where they already are.
/// </remarks>
public static class WebValueCodec
{
    #region Unity to JSON

    /// <summary>
    /// <paramref name="value"/> as JSON, or null when its type has no web representation - the same
    /// types the panel draws no widget for.
    /// </summary>
    public static string ToJson(Type type, object value)
    {
        if (type == null)
            return null;

        if (type.IsEnum)
            return WebJson.Quote(value != null ? value.ToString() : "");

        if (type == typeof(float)) return WebJson.Number(Convert.ToSingle(value, CultureInfo.InvariantCulture));
        if (type == typeof(int)) return WebJson.Number(Convert.ToInt32(value, CultureInfo.InvariantCulture));
        if (type == typeof(bool)) return WebJson.Bool((bool)value);
        if (type == typeof(string)) return WebJson.Quote(value as string);

        if (type == typeof(Color))
        {
            var color = (Color)value;
            return WebJson.Array(color.r, color.g, color.b, color.a);
        }

        if (type == typeof(Vector2))
        {
            var v = (Vector2)value;
            return WebJson.Array(v.x, v.y);
        }

        if (type == typeof(Vector2Int))
        {
            var v = (Vector2Int)value;
            return WebJson.Array(v.x, v.y);
        }

        if (type == typeof(Vector3))
        {
            var v = (Vector3)value;
            return WebJson.Array(v.x, v.y, v.z);
        }

        if (type == typeof(Vector3Int))
        {
            var v = (Vector3Int)value;
            return WebJson.Array(v.x, v.y, v.z);
        }

        if (type == typeof(Vector4))
        {
            var v = (Vector4)value;
            return WebJson.Array(v.x, v.y, v.z, v.w);
        }

        return null;
    }

    #endregion

    #region JSON to Unity

    /// <summary>
    /// Turns a parsed JSON value into the argument list <c>Controllable.SetFieldProp</c> expects for
    /// a member of <paramref name="type"/>, or answers false when it cannot.
    /// </summary>
    /// <remarks>
    /// A vector or colour must arrive complete: a partial array would be written through as a value
    /// the user never asked for, since SetFieldProp fills the missing components with nothing.
    /// </remarks>
    public static bool TryReadValues(Type type, object json, out List<object> values)
    {
        values = null;

        if (type == null || json == null)
            return false;

        //An enum arrives as its member name, and TryGetEnumValue resolves that from the FieldInfo.
        if (type.IsEnum)
        {
            var name = WebJson.AsString(json);
            if (name == null)
                return false;

            values = new List<object> { name };
            return true;
        }

        if (type == typeof(float) || type == typeof(int))
        {
            float number;
            if (!WebJson.TryGetFloat(json, out number))
                return false;

            values = new List<object> { number };
            return true;
        }

        if (type == typeof(bool))
        {
            if (json is bool flag)
            {
                values = new List<object> { flag };
                return true;
            }

            float number;
            if (!WebJson.TryGetFloat(json, out number))
                return false;

            values = new List<object> { number >= 1f };
            return true;
        }

        if (type == typeof(string))
        {
            var text = WebJson.AsString(json);
            if (text == null)
                return false;

            values = new List<object> { text };
            return true;
        }

        var components = ComponentCount(type);
        if (components == 0)
            return false;

        //A three-component colour is the one short form accepted: SetFieldProp fills alpha with 1.
        var minimum = type == typeof(Color) ? 3 : components;
        return TryReadNumbers(json, minimum, components, out values);
    }

    static bool TryReadNumbers(object json, int minimum, int maximum, out List<object> values)
    {
        values = null;

        var items = json as List<object>;
        if (items == null || items.Count < minimum)
            return false;

        var read = new List<object>(maximum);
        for (var i = 0; i < maximum && i < items.Count; i++)
        {
            float number;
            if (!WebJson.TryGetFloat(items[i], out number))
                return false;

            read.Add(number);
        }

        values = read;
        return true;
    }

    /// <summary>How many numbers a vector or colour carries; 0 for anything else.</summary>
    public static int ComponentCount(Type type)
    {
        if (type == typeof(Vector2) || type == typeof(Vector2Int)) return 2;
        if (type == typeof(Vector3) || type == typeof(Vector3Int)) return 3;
        if (type == typeof(Vector4) || type == typeof(Color)) return 4;

        return 0;
    }

    #endregion
}
