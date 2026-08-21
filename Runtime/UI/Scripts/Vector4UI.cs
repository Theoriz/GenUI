using UnityEngine;

public class Vector4UI : VectorUIBase
{
    protected override string[] AxisNames { get { return XYZW; } }
    protected override bool IsInteger { get { return false; } }

    protected override string[] ReadAxisValues()
    {
        var value = (Vector4)Property.GetValue(LinkedControllable);
        return new[] { FormatAxis(value.x), FormatAxis(value.y), FormatAxis(value.z), FormatAxis(value.w) };
    }
}
