using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasUpdate : MonoBehaviour
{
    public int defaultHeight = 1080;
    
    private void Awake()
    {
        RectTransform rt = GetComponent<RectTransform>();
        Vector3 canvasScale = rt.localScale;
        float ratioToDefault = (float)defaultHeight / Screen.height;
        Vector3 newCanvasScale = canvasScale * ratioToDefault;
        rt.localScale = newCanvasScale;

        rt.sizeDelta = new Vector2(Screen.width, Screen.height);

        float canvasHeight = rt.rect.height;
        float newCanvasWidth = canvasHeight * Camera.main.aspect;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newCanvasWidth);
    }
}
