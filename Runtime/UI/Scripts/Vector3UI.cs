using UnityEngine;

public class Vector3UI : VectorUIBase
{
    protected override string[] AxisNames { get { return XYZ; } }
    protected override bool IsInteger { get { return false; } }

    protected override string[] ReadAxisValues()
    {
        var value = (Vector3)Property.GetValue(LinkedControllable);
        return new[] { FormatAxis(value.x), FormatAxis(value.y), FormatAxis(value.z) };
    }
}
