using UnityEngine;
using System.Collections;

public class TestProp : OSCControllable
{
    [OSCProperty("rotation")]
    public float rotationSpeed;

    [OSCProperty("big")]
    public bool big;

    [OSCProperty("color")]
    public int color;

    [OSCProperty("position")]
    public Vector3 pos;

    // Use this for initialization
    public override void Start () {
	}

    // Update is called once per frame
    public override void Update () {
        transform.localScale = Vector3.one * (big ? 3 : 1);
        transform.Rotate(Vector3.up, rotationSpeed);
        transform.localPosition = pos;
	}
}
