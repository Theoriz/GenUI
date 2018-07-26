using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TweenCurves : MonoBehaviour {

    public static TweenCurves Instance;

    public AnimationCurve LinearCurve;
    public AnimationCurve EaseOutCurve;
    public AnimationCurve EaseInCurve;
    public AnimationCurve EaseInOutCurve;

    void Awake()
    {
        Instance = this;
    }
}
