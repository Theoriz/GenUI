using UnityEngine;
using System.Collections;

public class TestProp : Controllable
{
    [OSCProperty]
    public float rotationSpeed;

    [OSCProperty]
    public bool big;

    [OSCProperty]
    public int color;

    [OSCProperty]
    public Vector3 pos;

    // Use this for initialization
    void Start () {
	}

    // Update is called once per frame
    void Update () {
        transform.localScale = Vector3.one * (big ? 3 : 1);
        transform.Rotate(Vector3.up, rotationSpeed);
        transform.localPosition = pos;
	}
}
