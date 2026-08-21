using UnityEngine;

public class Vector3IntUI : VectorUIBase
{
    protected override string[] AxisNames { get { return XYZ; } }
    protected override bool IsInteger { get { return true; } }

    protected override string[] ReadAxisValues()
    {
        var value = (Vector3Int)Property.GetValue(LinkedControllable);
        return new[] { FormatAxis(value.x), FormatAxis(value.y), FormatAxis(value.z) };
    }
}
