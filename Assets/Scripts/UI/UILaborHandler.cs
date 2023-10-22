using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    //set numbers when opening menu
    private void PrepUI()
    {
        foreach (ResourceType resourceType in city.GetResourcesWorked())
        {
            laborOptionsDict[resourceType].ToggleVisibility(true);
            laborOptionsDict[resourceType].SetUICount(city.GetResourcesWorkedResourceCount(resourceType), city.ResourceManager.GetResourceGenerationValues(resourceType));
            laborOptions.Add(laborOptionsDict[resourceType]);
        }
    }

    //setting city upon city selection
    public void SetCity(City city)
    {
        this.city = city;
    }

    public void ToggleVisibility(bool v)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(true);
            activeStatus = true;
            PrepUI();

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

    public void ToggleCityLaborCostButton()
    {
        city.world.cityBuilderManager.PlaySelectAudio(true);
        ToggleCityLaborCost();
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
    }
}
