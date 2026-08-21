using UnityEngine;

public class Vector2UI : VectorUIBase
{
    protected override string[] AxisNames { get { return XY; } }
    protected override bool IsInteger { get { return false; } }

    protected override string[] ReadAxisValues()
    {
        var value = (Vector2)Property.GetValue(LinkedControllable);
        return new[] { FormatAxis(value.x), FormatAxis(value.y) };
    }
}
