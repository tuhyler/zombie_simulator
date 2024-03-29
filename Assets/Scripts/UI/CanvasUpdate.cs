using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasUpdate : MonoBehaviour
{
    //scale size at 1920x1080 0.001069167 //defaults
    public int defaultHeight = 1080;
    public int maxNumerator = 21; 
    public int denominator = 18; 
    public int stepSize = 640;

    [HideInInspector]
    public float newCanvasWidth; //to pass in case some UI elements define their own size

    //0. 0 7/6 21/18 (maxNumerator / denominator)
    // + 640 (step size)
    //1. 640 10/9 20/18
    // + 640 
    //2. 1280 19/18
    // + 640
    //3. 1920 1 18/18
    // + 640
    //4. 2560 17/18
    // + 640
    //5. 3200 8/9 16/18
    // + 640
    //6. 3840 5/6 15/18
    // + 640
    //7. 4480 7/9 14/18
    // + 640
    //8. 5120 13/18

    private void Awake()
    {
        RectTransform rt = GetComponent<RectTransform>();

        //setting the canvas borders
        //rt.sizeDelta = new Vector2(Screen.width, Screen.height);
        float multiple = maxNumerator - (Screen.width / (float)stepSize);
        float adjustWidth = Screen.width * multiple / denominator;
        float adjustHeight = Screen.height * multiple / denominator;
        rt.sizeDelta = new Vector2(adjustWidth, adjustHeight);

        //scaling the buttons
        Vector3 canvasScale = rt.localScale;
        float ratioToDefault = (float)defaultHeight / adjustHeight;
        Vector3 newCanvasScale = canvasScale * ratioToDefault;
        rt.localScale = newCanvasScale;

        //setting the aspect ratio
        float canvasHeight = rt.rect.height;
        newCanvasWidth = canvasHeight * Camera.main.aspect;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newCanvasWidth);
    }
}
