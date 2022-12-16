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

    private int goldAmount, researchAmount;

    private bool researching; //flag if researching something


    private void Awake()
    {
        //goldResourceAmount.text = "0".ToString();
        SetResearchValue(researchAmount);
        SetResearchTitle();
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

    private void SetGoldValue(int goldVal)
    {
        //goldAmount = goldVal;
        //goldResourceAmount.text = goldAmount.ToString();
    }

    private void SetResearchValue(int researchVal)
    {
        researchAmount += researchVal;
        float researchPerc = researchAmount / researchLimit;
        progressBarMask.fillAmount = researchPerc;
        researchResourceAmount.text = Mathf.RoundToInt(researchPerc * 100).ToString() + "%";
    }

    public void SetResearchTitle(string title = "")
    {
        if (researching)
        {
            researchTitle.text = title;
        }
        else
        {
            researchTitle.text = "No Current Research";
        }
    }

    public void SetResourceGenerationAmount(ResourceType resourceType, float val)
    {
        //if (val > 0)
        //{
        //    //resourceGenerationAmount.text = $"+{val}";
        //}
        //if (val == 0)
        //{
        //    //resourceGenerationAmount.text = "-";
        //}
        //if (val < 0)
        //{
        //    //resourceGenerationAmount.text = val.ToString();
        //}
    }
}
