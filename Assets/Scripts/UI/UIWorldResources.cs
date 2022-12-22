using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIWorldResources : MonoBehaviour
{
    [SerializeField]
    private TMP_Text goldResourceAmount, researchResourceAmount, researchTitle;//, resourceGenerationAmount;
    [SerializeField]
    private Image progressBarMask;

    private int researchLimit = 10;
    public int ResearchLimit { set { researchLimit = value; } } 

    private int researchAmount;


    private void Awake()
    {
        SetResearchValue(researchAmount);
    }

    public void SetActiveStatus(bool v)
    {
        gameObject.SetActive(v);
    }

    public void SetResource(ResourceType resourceType, int resourceAmount)
    {
        if (resourceType == ResourceType.Gold)
            goldResourceAmount.text = resourceAmount.ToString();
            //SetGoldValue(resourceAmount);
        else if (resourceType == ResourceType.Research)
            SetResearchValue(resourceAmount);
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
}
