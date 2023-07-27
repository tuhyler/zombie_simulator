using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UITooltipSystem : MonoBehaviour
{
    private static UITooltipSystem current;
    private Canvas canvas;
    public UITooltip tooltip;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        current = this;
    }

    public static void Show(string message)
    {
        current.tooltip.SetInfo(message);
        current.tooltip.gameObject.SetActive(true);
        current.canvas.gameObject.SetActive(true);

        Color fade = current.tooltip.messageText.color;
        LeanTween.alpha(current.tooltip.allContents, 1f, .2f).setFrom(0f).setEaseLinear();
        LeanTween.value(current.tooltip.messageText.gameObject, fade.a, 1, 0.2f).setOnUpdate((value) => { fade.a = value; current.tooltip.messageText.color = fade; });
    }

    public static void Hide()
    {
        Color fade = current.tooltip.messageText.color;
        LeanTween.alpha(current.tooltip.allContents, 0f, .2f).setEaseLinear().setOnComplete(current.SetActiveStatusFalse); 
        LeanTween.value(current.tooltip.messageText.gameObject, fade.a, 0, 0.2f).setOnUpdate((value) => { fade.a = value; current.tooltip.messageText.color = fade; });
    }

    private void SetActiveStatusFalse()
    {
        current.tooltip.gameObject.SetActive(false);
        current.canvas.gameObject.SetActive(false);
    }
}
