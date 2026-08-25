using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// The little JSON the web mirror speaks, written and read by hand.
/// </summary>
/// <remarks>
/// <c>JsonUtility</c> cannot help here: it needs a serializable type per message shape, and a value
/// message carries whatever the members happen to be. The reader answers with plain objects -
/// <c>Dictionary&lt;string, object&gt;</c>, <c>List&lt;object&gt;</c>, <c>string</c>, <c>double</c>,
/// <c>bool</c> or null - and refuses anything malformed rather than throwing at the caller.
/// </remarks>
public static class WebJson
{
    #region Writing

    /// <summary><paramref name="text"/> as a JSON string, quotes included.</summary>
    public static string Quote(string text)
    {
        var json = new StringBuilder(text != null ? text.Length + 2 : 2);
        json.Append('"');

        if (text != null)
        {
            foreach (var c in text)
            {
                switch (c)
                {
                    case '"': json.Append("\\\""); break;
                    case '\\': json.Append("\\\\"); break;
                    case '\n': json.Append("\\n"); break;
                    case '\r': json.Append("\\r"); break;
                    case '\t': json.Append("\\t"); break;
                    case '\b': json.Append("\\b"); break;
                    case '\f': json.Append("\\f"); break;
                    default:
                        //Everything below space has no literal form; the rest goes through as UTF-8.
                        if (c < ' ')
                            json.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            json.Append(c);
                        break;
                }
            }
        }

        json.Append('"');
        return json.ToString();
    }

    /// <summary>
    /// A float as a JSON number, round-tripping and in invariant culture.
    /// </summary>
    /// <remarks>
    /// NaN and the infinities have no JSON form, so they are written as 0 rather than as a token no
    /// browser would parse.
    /// </remarks>
    public static string Number(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return "0";

        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    public static string Number(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    public static string Bool(bool value)
    {
        return value ? "true" : "false";
    }

    /// <summary>The floats as a JSON array - how every vector and colour goes over the wire.</summary>
    public static string Array(params float[] values)
    {
        var json = new StringBuilder("[");
        for (var i = 0; i < values.Length; i++)
        {
            if (i != 0) json.Append(',');
            json.Append(Number(values[i]));
        }

        return json.Append(']').ToString();
    }

    #endregion

    #region Reading

    /// <summary>Parses <paramref name="text"/>, or answers null when it is not valid JSON.</summary>
    public static object Parse(string text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        var index = 0;
        var value = ParseValue(text, ref index);
        if (value == Invalid)
            return null;

        SkipWhitespace(text, ref index);
        return index == text.Length ? value : null;
    }

    /// <summary>The named member of a parsed object, or null when it is absent.</summary>
    public static object Member(object node, string name)
    {
        var map = node as Dictionary<string, object>;
        object value;
        return map != null && map.TryGetValue(name, out value) ? value : null;
    }

    /// <summary>
    /// A parsed node as text: a string as itself, a number or a bool as it would be written.
    /// </summary>
    public static string AsString(object node)
    {
        if (node is string text) return text;
        if (node is bool flag) return flag ? "true" : "false";
        if (node is double number) return number.ToString("R", CultureInfo.InvariantCulture);

        return null;
    }

    /// <summary>
    /// A parsed node as a float. A browser sends what an input field holds, so a number that arrived
    /// as text is read as the number it spells.
    /// </summary>
    public static bool TryGetFloat(object node, out float value)
    {
        if (node is double number)
        {
            value = (float)number;
            return true;
        }

        if (node is bool flag)
        {
            value = flag ? 1f : 0f;
            return true;
        }

        if (node is string text)
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        value = 0f;
        return false;
    }

    //A sentinel distinct from a legitimately parsed null.
    static readonly object Invalid = new object();

    static object ParseValue(string text, ref int index)
    {
        SkipWhitespace(text, ref index);
        if (index >= text.Length)
            return Invalid;

        switch (text[index])
        {
            case '{': return ParseObject(text, ref index);
            case '[': return ParseArray(text, ref index);
            case '"': return ParseString(text, ref index);
            case 't': return ParseLiteral(text, ref index, "true", true);
            case 'f': return ParseLiteral(text, ref index, "false", false);
            case 'n': return ParseLiteral(text, ref index, "null", null);
            default: return ParseNumber(text, ref index);
        }
    }

    static object ParseObject(string text, ref int index)
    {
        var map = new Dictionary<string, object>();
        index++; //'{'

        SkipWhitespace(text, ref index);
        if (index < text.Length && text[index] == '}')
        {
            index++;
            return map;
        }

        while (true)
        {
            SkipWhitespace(text, ref index);
            if (index >= text.Length || text[index] != '"')
                return Invalid;

            var name = ParseString(text, ref index);
            if (name == Invalid)
                return Invalid;

            SkipWhitespace(text, ref index);
            if (index >= text.Length || text[index] != ':')
                return Invalid;

            index++;
            var value = ParseValue(text, ref index);
            if (value == Invalid)
                return Invalid;

            map[(string)name] = value;

            SkipWhitespace(text, ref index);
            if (index >= text.Length)
                return Invalid;

            if (text[index] == ',')
            {
                index++;
                continue;
            }

            if (text[index] == '}')
            {
                index++;
                return map;
            }

            return Invalid;
        }
    }

    static object ParseArray(string text, ref int index)
    {
        var items = new List<object>();
        index++; //'['

        SkipWhitespace(text, ref index);
        if (index < text.Length && text[index] == ']')
        {
            index++;
            return items;
        }

        while (true)
        {
            var value = ParseValue(text, ref index);
            if (value == Invalid)
                return Invalid;

            items.Add(value);

            SkipWhitespace(text, ref index);
            if (index >= text.Length)
                return Invalid;

            if (text[index] == ',')
            {
                index++;
                continue;
            }

            if (text[index] == ']')
            {
                index++;
                return items;
            }

            return Invalid;
        }
    }

    static object ParseString(string text, ref int index)
    {
        var value = new StringBuilder();
        index++; //opening quote

        while (index < text.Length)
        {
            var c = text[index++];

            if (c == '"')
                return value.ToString();

            if (c != '\\')
            {
                value.Append(c);
                continue;
            }

            if (index >= text.Length)
                return Invalid;

            var escaped = text[index++];
            switch (escaped)
            {
                case '"': value.Append('"'); break;
                case '\\': value.Append('\\'); break;
                case '/': value.Append('/'); break;
                case 'n': value.Append('\n'); break;
                case 'r': value.Append('\r'); break;
                case 't': value.Append('\t'); break;
                case 'b': value.Append('\b'); break;
                case 'f': value.Append('\f'); break;
                case 'u':
                    if (index + 4 > text.Length)
                        return Invalid;

                    int code;
                    if (!int.TryParse(text.Substring(index, 4), NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture, out code))
                        return Invalid;

                    value.Append((char)code);
                    index += 4;
                    break;
                default: return Invalid;
            }
        }

        return Invalid;
    }

    static object ParseNumber(string text, ref int index)
    {
        var start = index;

        while (index < text.Length && "+-.eE0123456789".IndexOf(text[index]) >= 0)
            index++;

        double value;
        if (index == start || !double.TryParse(text.Substring(start, index - start), NumberStyles.Float,
            CultureInfo.InvariantCulture, out value))
            return Invalid;

        return value;
    }

    static object ParseLiteral(string text, ref int index, string literal, object value)
    {
        if (index + literal.Length > text.Length || string.CompareOrdinal(text, index, literal, 0, literal.Length) != 0)
            return Invalid;

        index += literal.Length;
        return value;
    }

    static void SkipWhitespace(string text, ref int index)
    {
        while (index < text.Length && (text[index] == ' ' || text[index] == '\t'
            || text[index] == '\n' || text[index] == '\r'))
            index++;
    }

    #endregion
}
