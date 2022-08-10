using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonHighlight : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private Image borderImage; 

    private void Awake()
    {
        canvasGroup.alpha = 0;
    }

    //public Renderer outlineRenderer;
    //private Material material;

    ////private Color colorToChange;
    ////private Color originalColor;

    //private Color testColor;

    //private void Awake()
    //{
    //    //material = outlineRenderer.material;
    //    //testColor = material.GetColor("_OutlineColor");
    //    //originalColor = material.GetColor("_Color");
    //}

    //private void ApplyHighlights(bool val, Color colorToChange)
    //{
    //    material.SetColor("_OutlineColor", colorToChange);
    //    //material.SetColor("_Color", val ? colorToChange : originalColor);
    //}

    public void EnableHighlight(Color colorToChange)
    {
        //this.colorToChange = colorToChange;
        //ApplyHighlights(true, colorToChange);
        canvasGroup.alpha = 1;
        borderImage.color = colorToChange;
    }

    public void DisableHighlight()
    {
        //ApplyHighlights(false);
        canvasGroup.alpha = 0;
    }
}
