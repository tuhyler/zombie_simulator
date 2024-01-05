using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIWorldResources : MonoBehaviour
{
    [SerializeField]
    private TMP_Text goldResourceAmount, researchResourceAmount, researchTitle;//, resourceGenerationAmount;
    [SerializeField]
    private Image progressBarMask, researchBackground;
    [SerializeField]
    public List<Button> buttons;
    [SerializeField]
    private Sprite completedBackground;
    [SerializeField]
    public RectTransform allContents;

    private Sprite originalBackground;

    private int researchLimit = 10;
    public int ResearchLimit { set { researchLimit = value; } } 

    private int researchAmount;


    private void Awake()
    {
        SetResearchValue(researchAmount);
        originalBackground = researchBackground.sprite;
    }

    public void SetActiveStatus(bool v)
    {
        gameObject.SetActive(v);
    }

    public void SetResource(ResourceType resourceType, int resourceAmount)
    {
        if (resourceType == ResourceType.Gold)
        {
			if (resourceAmount < 1000)
			{
				goldResourceAmount.text = resourceAmount.ToString();
			}
			else if (resourceAmount < 1000000)
			{
				goldResourceAmount.text = Math.Round(resourceAmount * 0.001f, 1) + " k";
			}
			else if (resourceAmount < 1000000000)
			{
				goldResourceAmount.text = Math.Round(resourceAmount * 0.000001f, 1) + " M";
			}
        }
        else if (resourceType == ResourceType.Research)
        {
            SetResearchValue(resourceAmount);
        }
    }

    public void SetResearchName(string name)
    {
        researchTitle.text = name;
    }

    public void SetResearchValue(int researchVal)
    {
        researchAmount = researchVal;
        float researchPerc = (float)researchAmount / researchLimit;
        //progressBarMask.fillAmount = researchPerc;
        researchResourceAmount.text = Mathf.RoundToInt(researchPerc * 100).ToString() + "%";

        LeanTween.value(progressBarMask.gameObject, progressBarMask.fillAmount, researchPerc, 0.2f)
            .setEase(LeanTweenType.easeOutSine)
            .setOnUpdate((value) =>
            {
                progressBarMask.fillAmount = value;
            });
    }

    public void SetResearchBackground(bool complete)
    {
        if (complete)
        {
            researchTitle.color = Color.black;
            researchResourceAmount.color = Color.black;
            researchBackground.sprite = completedBackground;
        }
        else
        {
            researchTitle.color = Color.white;
			researchResourceAmount.color = Color.white;
			researchBackground.sprite = originalBackground;
        }
    }

    public void SetInteractable(bool v)
    {
        foreach (Button button in buttons)
        {
            //button.interactable = v;
            button.enabled = v;
        }
    }
}
