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
    private Dictionary<ResourceType, UILaborHandlerOptions> laborOptionsDict = new();

    [SerializeField]
    private UICityLaborCostPanel uiCityLaborCostPanel;

    private City city;

    [SerializeField] //for tweening
    private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus;
    private Vector3 originalLoc;

    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;
        gameObject.SetActive(false);

        laborOptions = new List<UILaborHandlerOptions>();

        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources.Concat(ResourceHolder.Instance.allWorldResources))
        {
            UILaborHandlerOptions newLaborOption = Instantiate(laborResourceOption).GetComponent<UILaborHandlerOptions>();
            //laborResourceGO.SetActive(true);
            newLaborOption.transform.SetParent(laborOptionScrollRect, false);
            //= laborResourceGO.;
            newLaborOption.resourceImage.sprite = resource.resourceIcon;
            newLaborOption.resourceType = resource.resourceType;
            newLaborOption.ToggleVisibility(false);
            //laborOptions.Add(newLaborOption);
            laborOptionsDict[newLaborOption.resourceType] = newLaborOption;
        }
    }

    //preparing labor menu upon city selection
    public void PrepUI(City city)
    {
        this.city = city;
        
        foreach (ResourceType resourceType in city.GetResourcesWorked())
        {
            laborOptionsDict[resourceType].ToggleVisibility(true);
            laborOptionsDict[resourceType].SetUICount(city.GetResourcesWorkedResourceCount(resourceType), city.ResourceManager.GetResourceGenerationValues(resourceType));
            laborOptions.Add(laborOptionsDict[resourceType]);
        }

        //foreach (UILaborHandlerOptions option in laborOptions)
        //{
        //    if (city.CheckResourcesWorkedExists(option.resourceType))
        //    {
        //        option.ToggleVisibility(true);
        //        option.SetUICount(city.GetResourcesWorkedResourceCount(option.resourceType), city.ResourceManager.GetResourceGenerationValues(option.resourceType));
        //    }
        //}
    }

    //pass data to know if can show in the UI
    //public void ShowUI(City city) 
    //{
    //    ToggleVisibility(true);
        
    //    foreach (UILaborHandlerOptions option in laborOptions)
    //    {
    //        if (city.CheckResourcesWorkedExists(option.resourceType))
    //        {
    //            option.ToggleVisibility(true);
    //            option.SetUICount(city.GetResourcesWorkedResourceCount(option.resourceType), city.ResourceManager.GetResourceGenerationValues(option.resourceType));
    //        }
    //    }
    //}

    public void ToggleVisibility(bool v)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(true);
            activeStatus = true;

            if (uiCityLaborCostPanel.isOpen)
                ToggleCityLaborCost();

            allContents.anchoredPosition3D = originalLoc + new Vector3(500f, 0, 0);

            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x - 500f, 0.3f).setEaseOutSine();
            LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear();
        }
        else
        {
            activeStatus = false;
            uiCityLaborCostPanel.ToggleVisibility(false, true);
            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + 600f, 0.2f).setOnComplete(SetActiveStatusFalse);
        }
    }

    public void HideUI()
    {
        ToggleVisibility(false);        
    }

    public void ResetUI()
    {
        city = null;
        
        //if (activeStatus)
        //{
        foreach (UILaborHandlerOptions option in laborOptions)
        {
            option.HideLaborIcons();
            option.ToggleVisibility(false);
        }

        laborOptions.Clear();
        uiCityLaborCostPanel.ResetUI();
        //}
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void ToggleCityLaborCost()
    {
        if (uiCityLaborCostPanel.activeStatus)
        {
            uiCityLaborCostPanel.ToggleVisibility(false, false);
        }
        else
        {
            uiCityLaborCostPanel.SetConsumedResourcesInfo(city.ResourceManager.ResourceConsumedPerMinuteDict);
            uiCityLaborCostPanel.ToggleVisibility(true, uiCityLaborCostPanel.isOpen);
        }
    }

    public void UpdateResourcesConsumed(List<ResourceType> consumedResourceTypes, Dictionary<ResourceType, float> consumedResourcesDict)
    {
        uiCityLaborCostPanel.UpdateConsumedResources(consumedResourceTypes, consumedResourcesDict);
    }

    public void PlusMinusOneLabor(ResourceType resourceType, int laborCount, int laborChange, float resourceGeneration)
    {
        if (laborCount == 1)
        {
            laborOptionsDict[resourceType].ToggleVisibility(true);
            laborOptions.Add(laborOptionsDict[resourceType]);
        }
        else if (laborCount == 0)
        {
            laborOptionsDict[resourceType].ToggleVisibility(false);
            laborOptions.Remove(laborOptionsDict[resourceType]);
        }

        laborOptionsDict[resourceType].AddSubtractUICount(laborCount, laborChange, resourceGeneration);

        //foreach (UILaborHandlerOptions uiLaborHandlerOption in laborOptions)
        //{
        //    if (resourceType == uiLaborHandlerOption.resourceType)
        //    {
        //        if (laborCount == 1)
        //            uiLaborHandlerOption.ToggleVisibility(true);
        //        else if (laborCount == 0)
        //            uiLaborHandlerOption.ToggleVisibility(false);

        //        uiLaborHandlerOption.AddSubtractUICount(laborCount, laborChange, resourceGeneration);
        //    }
        //}
    }
}
