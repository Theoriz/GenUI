using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderValue : MonoBehaviour
{

    private Slider slider;

    private Text text;

    public string propertyName;
    // Use this for initialization
    void Start()
    {
        slider = GetComponent<Slider>();
        text = transform.Find("Text").GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        text.text = propertyName + " : " + slider.value.ToString("F2");

    }
}
