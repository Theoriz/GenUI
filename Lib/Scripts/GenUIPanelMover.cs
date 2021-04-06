using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenUIPanelMover : MonoBehaviour
{
    [Header("Keyboard Shortcuts")]
    public KeyCode leftKey = KeyCode.LeftArrow;
    public KeyCode rightKey = KeyCode.RightArrow;
    public KeyCode upKey = KeyCode.UpArrow;
    public KeyCode downKey = KeyCode.DownArrow;

    public float speed = 10;

    private RectTransform rectTransform;

    private void Start() {

        rectTransform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(leftKey))
            rectTransform.anchoredPosition += Vector2.left * speed;

        if (Input.GetKey(rightKey))
            rectTransform.anchoredPosition += Vector2.right * speed;

        if (Input.GetKey(upKey)) {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                rectTransform.localScale += Vector3.one * 0.1f;
            } else {
                rectTransform.anchoredPosition += Vector2.up * speed;
            }
        }

        if (Input.GetKey(downKey)) {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                rectTransform.localScale -= Vector3.one * 0.1f;
            } else {
                rectTransform.anchoredPosition += Vector2.down * speed;
            }
        }
    }
}
