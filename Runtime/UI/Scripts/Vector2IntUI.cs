using UnityEngine;

public class Vector2IntUI : VectorUIBase
{
    protected override string[] AxisNames { get { return XY; } }
    protected override bool IsInteger { get { return true; } }

    protected override string[] ReadAxisValues()
    {
        var value = (Vector2Int)Property.GetValue(LinkedControllable);
        return new[] { FormatAxis(value.x), FormatAxis(value.y) };
    }
}
