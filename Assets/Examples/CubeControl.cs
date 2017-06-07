using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeControl : Controllable
{
    [OSCProperty("nombreInt")]
    public int nombreInt;

    [OSCProperty("nombreFloat")]
    public float nombreFloat;

    [OSCProperty("myString")]
    public string myString;

    [OSCProperty("rotate")]
    public bool rotate;

    [OSCProperty("speed")]
    [Range(0,100)]
    public float speed;

    [OSCProperty("pos")]
    public Vector3 pos;

    [OSCMethod("setColor")]
    public void setColor(Color col)
    {
        GetComponent<Renderer>().material.color = col;
    }

    [OSCMethod("setColorRed")]
    public void setColorRed()//Color col)
    {
        GetComponent<Renderer>().material.color = Color.red;//col;
    }

    [OSCMethod("setColorWhite")]
    public void setColorWhite()//Color col)
    {
        GetComponent<Renderer>().material.color = Color.white;//col;
    }

    // Use this for initialization
    void Start ()
	{
        base.init();
        controllableMaster.Register(GetComponent<CubeControl>());
    }

    // Update is called once per frame
    void Update ()
    {
         pos = transform.position;
         if(rotate)
            transform.Rotate(Vector3.up, Time.deltaTime * speed);

         Debug.Log("Nombre (int) : " + nombreInt);
         Debug.Log("Nombre (float) : " + nombreFloat);
    }
}
