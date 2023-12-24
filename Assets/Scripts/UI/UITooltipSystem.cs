using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UITooltipSystem : MonoBehaviour
{
    private static UITooltipSystem current;
    //private Canvas canvas;
    public UITooltip tooltip;
    public UITooltipWorkEthic tooltipWorkEthic;

    private void Awake()
    {
        //canvas = GetComponent<Canvas>();
        current = this;
    }

    public static void ShowWorkEthic(City city)
    {
		current.tooltipWorkEthic.gameObject.SetActive(true);

		Color fade = current.tooltipWorkEthic.titleText.color;
		LeanTween.alpha(current.tooltipWorkEthic.allContents, 1f, .2f).setFrom(0f).setEaseLinear();
		LeanTween.alpha(current.tooltipWorkEthic.line, 1f, .2f).setFrom(0f).setEaseLinear();
		LeanTween.value(current.tooltipWorkEthic.titleText.gameObject, fade.a, 1, 0.2f).setOnUpdate((value) => { fade.a = value; current.tooltipWorkEthic.titleText.color = fade; });
		string improvementMessage, wonderMessage;
        int totalCount = 0;

        if (city.improvementWorkEthic > 0)
        {
            totalCount++;
            improvementMessage = "Improvements: <color=green>+" + (city.improvementWorkEthic * 100).ToString() + "%</color>";
            current.tooltipWorkEthic.improvementText.gameObject.SetActive(true);
            LeanTween.value(current.tooltipWorkEthic.improvementText.gameObject, fade.a, 1, 0.2f).setOnUpdate((value) => { fade.a = value; current.tooltipWorkEthic.improvementText.color = fade; });
		}
        else
        {
			current.tooltipWorkEthic.improvementText.gameObject.SetActive(false);
			improvementMessage = "";
        }

		if (city.wonderWorkEthic > 0)
		{
            totalCount++;
            wonderMessage = "Wonders: <color=green>+" + (city.wonderWorkEthic * 100).ToString() + "%</color>";
			current.tooltipWorkEthic.wonderText.gameObject.SetActive(true);
			LeanTween.value(current.tooltipWorkEthic.wonderText.gameObject, fade.a, 1, 0.2f).setOnUpdate((value) => { fade.a = value; current.tooltipWorkEthic.wonderText.color = fade; });
		}
		else
		{
			current.tooltipWorkEthic.wonderText.gameObject.SetActive(false);
			wonderMessage = "";
		}

        if (totalCount == 0)
        {
            totalCount++;
			improvementMessage = "Affects Production Yield";
			current.tooltipWorkEthic.improvementText.gameObject.SetActive(true);
			LeanTween.value(current.tooltipWorkEthic.improvementText.gameObject, fade.a, 1, 0.2f).setOnUpdate((value) => { fade.a = value; current.tooltipWorkEthic.improvementText.color = fade; });
		}

        int shift = 25 * totalCount;

        current.tooltipWorkEthic.allContents.sizeDelta = new Vector2(260, 51 + shift);
		current.tooltipWorkEthic.SetInfo(improvementMessage, wonderMessage);
	}

    public static void Show(string message)
    {
        current.tooltip.SetInfo(message);
        current.tooltip.gameObject.SetActive(true);

        Color fade = current.tooltip.messageText.color;
        LeanTween.alpha(current.tooltip.allContents, 1f, .2f).setFrom(0f).setEaseLinear();
        LeanTween.value(current.tooltip.messageText.gameObject, fade.a, 1, 0.2f).setOnUpdate((value) => { fade.a = value; current.tooltip.messageText.color = fade; });
        //current.canvas.gameObject.SetActive(true);
    }

    public static void HideWorkEthic()
    {
		Color fade = current.tooltipWorkEthic.titleText.color;
		LeanTween.alpha(current.tooltipWorkEthic.allContents, 0f, .2f).setEaseLinear().setOnComplete(current.SetWorkEthicActiveStatusFalse);
		LeanTween.alpha(current.tooltipWorkEthic.line, 0f, .2f).setEaseLinear();
		LeanTween.value(current.tooltipWorkEthic.titleText.gameObject, fade.a, 0, 0.2f).setOnUpdate((value) => { fade.a = value; current.tooltipWorkEthic.titleText.color = fade; });
		LeanTween.value(current.tooltipWorkEthic.improvementText.gameObject, fade.a, 0, 0.2f).setOnUpdate((value) => { fade.a = value; current.tooltipWorkEthic.improvementText.color = fade; });
		LeanTween.value(current.tooltipWorkEthic.wonderText.gameObject, fade.a, 0, 0.2f).setOnUpdate((value) => { fade.a = value; current.tooltipWorkEthic.wonderText.color = fade; });
	}

    public static void Hide()
    {
		Color fade = current.tooltip.messageText.color;
        LeanTween.alpha(current.tooltip.allContents, 0f, .2f).setEaseLinear().setOnComplete(current.SetActiveStatusFalse); 
        LeanTween.value(current.tooltip.messageText.gameObject, fade.a, 0, 0.2f).setOnUpdate((value) => { fade.a = value; current.tooltip.messageText.color = fade; });
    }

    private void SetWorkEthicActiveStatusFalse()
    {
		current.tooltipWorkEthic.gameObject.SetActive(false);
		//current.canvas.gameObject.SetActive(false);
	}

	private void SetActiveStatusFalse()
    {
        current.tooltip.gameObject.SetActive(false);
        //current.canvas.gameObject.SetActive(false);
    }
}
