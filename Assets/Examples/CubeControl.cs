using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeControl : OSCControllable
{

    [OSCProperty("speed")]
    [Range(0,100)]
    public float speed = 0;
    
    [OSCProperty("position")]
    public Vector3 pos;

    [OSCMethod("setColor")]
    public void setColor(Color col)
    {
        GetComponent<Renderer>().material.color = col;
    }

	// Use this for initialization
	public override void Start () {
		
	}

    // Update is called once per frame
    public override void Update () {
        transform.position = pos;
        transform.Rotate(Vector3.up, Time.deltaTime * speed);
	}

  
}
