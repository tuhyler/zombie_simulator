using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class UILaborHandler : MonoBehaviour
{
    [SerializeField]
    private GameObject laborResourceOption;
    
    [SerializeField]
    private Transform laborOptionScrollRect;
    private List<UILaborHandlerOptions> laborOptions;

    //[SerializeField]
    //private TMP_Text noneText;

    [SerializeField] //for tweening
    private RectTransform allContents;
    private bool activeStatus;
    private Vector3 originalLoc;



    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;
        gameObject.SetActive(false);

        laborOptions = new List<UILaborHandlerOptions>();

        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources.Concat(ResourceHolder.Instance.allWorldResources).ToList())
        {
            UILaborHandlerOptions newLaborOption = Instantiate(laborResourceOption).GetComponent<UILaborHandlerOptions>();
            //laborResourceGO.SetActive(true);
            newLaborOption.transform.SetParent(laborOptionScrollRect, false);
            //= laborResourceGO.;
            newLaborOption.resourceImage.sprite = resource.resourceIcon;
            newLaborOption.resourceType = resource.resourceType;
            newLaborOption.ToggleVisibility(false);
            laborOptions.Add(newLaborOption);
        }



    }

    //pass data to know if can show in the UI
    public void ShowUI(City city) 
    {
        ToggleVisibility(true);
        
        foreach (UILaborHandlerOptions option in laborOptions)
        {
            if (city.CheckResourcesWorkedExists(option.resourceType))
            {
                option.ToggleVisibility(true);
                option.SetUICount(city.GetResourcesWorkedResourceCount(option.resourceType), city.ResourceManager.GetResourceGenerationValues(option.resourceType));
            }
        }
    }

    private void ToggleVisibility(bool v)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(true);

            activeStatus = true;

            allContents.anchoredPosition3D = originalLoc + new Vector3(500f, 0, 0);

            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x - 500f, 0.3f).setEaseOutSine();
            LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear();
        }
        else
        {
            activeStatus = false;
            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + 600f, 0.2f).setOnComplete(SetActiveStatusFalse);
        }
    }

    public void HideUI()
    {
        ToggleVisibility(false);        
    }

    public void ResetUI()
    {
        foreach (UILaborHandlerOptions option in laborOptions)
        {
            option.HideLaborIcons();
            option.ToggleVisibility(false);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void PlusMinusOneLabor(ResourceType resourceType, int laborCount, int laborChange, float resourceGeneration)
    {
        foreach (UILaborHandlerOptions uiLaborHandlerOption in laborOptions)
        {
            if (resourceType == uiLaborHandlerOption.resourceType)
            {
                if (laborCount == 1)
                    uiLaborHandlerOption.ToggleVisibility(true);
                else if (laborCount == 0)
                    uiLaborHandlerOption.ToggleVisibility(false);

                uiLaborHandlerOption.AddSubtractUICount(laborCount, laborChange, resourceGeneration);
            }
        }
    }
}
