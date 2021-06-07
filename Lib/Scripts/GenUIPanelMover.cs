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

    private RectTransform _rectTransform;
    private Vector2 _defaultAnchoredPosition;
    private Vector3 _defaultScale;

    private void Start() {

        _rectTransform = GetComponent<RectTransform>();
        _defaultAnchoredPosition = _rectTransform.anchoredPosition;
        _defaultScale = _rectTransform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(leftKey))
            _rectTransform.anchoredPosition += Vector2.left * speed;

        if (Input.GetKey(rightKey))
            _rectTransform.anchoredPosition += Vector2.right * speed;

        if (Input.GetKey(upKey)) {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                _rectTransform.localScale += Vector3.one * 0.1f;
            } else {
                _rectTransform.anchoredPosition += Vector2.up * speed;
            }
        }

        if (Input.GetKey(downKey)) {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                _rectTransform.localScale -= Vector3.one * 0.1f;
            } else {
                _rectTransform.anchoredPosition += Vector2.down * speed;
            }
        }

        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) &&
            Input.GetKey(leftKey) &&
            Input.GetKey(rightKey) &&
			Input.GetKey(downKey)) {
            //Reset
            _rectTransform.anchoredPosition = _defaultAnchoredPosition;
            _rectTransform.localScale = _defaultScale;
		}
    }
}
