using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UILaborHandler : MonoBehaviour
{
    [SerializeField]
    private GameObject laborResourceOption, resourcePanel;
    
    [SerializeField]
    private Transform laborOptionScrollRect, laborCostScrollRect;
    private List<UILaborHandlerOptions> laborOptions = new();
	private List<UIResourceInfoPanel> resourceOptions = new();
	private Dictionary<ResourceType, UILaborHandlerOptions> laborOptionsDict = new();
	private Dictionary<ResourceType, UIResourceInfoPanel> resourceOptionsDict = new();

	//[SerializeField]
 //   private UICityLaborCostPanel uiCityLaborCostPanel;

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
		CreateLaborHandlerOption(ResourceType.Food);
		CreateLaborCostResource(ResourceType.Food);
		CreateLaborHandlerOption(ResourceType.Research);
		CreateLaborCostResource(ResourceType.Gold);

		//foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources.Concat(ResourceHolder.Instance.allWorldResources))
		//{
		//    UILaborHandlerOptions newLaborOption = Instantiate(laborResourceOption).GetComponent<UILaborHandlerOptions>();
		//    //laborResourceGO.SetActive(true);
		//    newLaborOption.transform.SetParent(laborOptionScrollRect, false);
		//    //= laborResourceGO.;
		//    newLaborOption.resourceImage.sprite = resource.resourceIcon;
		//    newLaborOption.resourceType = resource.resourceType;
		//    newLaborOption.ToggleVisibility(false);
		//    //laborOptions.Add(newLaborOption);
		//    laborOptionsDict[newLaborOption.resourceType] = newLaborOption;
		//}
	}

    public void CreateLaborHandlerOption(ResourceType type)
    {
        if (!laborOptionsDict.ContainsKey(type)) 
        { 
            UILaborHandlerOptions newLaborOption = Instantiate(laborResourceOption).GetComponent<UILaborHandlerOptions>();
		    newLaborOption.transform.SetParent(laborOptionScrollRect, false);
		    newLaborOption.resourcePanel.resourceImage.sprite = ResourceHolder.Instance.GetIcon(type);
		    newLaborOption.resourceType = type;
		    newLaborOption.ToggleVisibility(false);
		    laborOptionsDict[newLaborOption.resourceType] = newLaborOption;
		}
	}

    public void CreateLaborCostResource(ResourceType type)
    {
        if (!resourceOptionsDict.ContainsKey(type))
        {
			GameObject resourceOptionGO = Instantiate(resourcePanel, laborCostScrollRect);
			UIResourceInfoPanel resourceOption = resourceOptionGO.GetComponent<UIResourceInfoPanel>();
			resourceOption.resourceImage.sprite = ResourceHolder.Instance.GetIcon(type);
			resourceOption.SetResourceType(type);
			resourceOption.resourceAmountText.color = Color.red;
			resourceOption.gameObject.SetActive(false);
			resourceOptionsDict[resourceOption.resourceType] = resourceOption;
        }
    }

    //public void CreateLaborCostResource(ResourceType type)
    //{
    //    uiCityLaborCostPanel.CreateLaborCostResource(type);
    //}

    //set numbers when opening menu
    private void PrepUI()
    {
        foreach (ResourceType resourceType in city.GetResourcesWorked())
        {
            if (resourceType != ResourceType.None)
            {
                //if (resourceType == ResourceType.Fish)
                //    continue;
                
                laborOptionsDict[resourceType].ToggleVisibility(true);
                laborOptionsDict[resourceType].SetUICount(city.GetResourcesWorkedResourceCount(resourceType), city.resourceManager.GetResourceGenerationValues(resourceType));
                laborOptions.Add(laborOptionsDict[resourceType]);
            }
        }

        SortLaborOptions();
    }

    private void SortLaborOptions()
    {
        int listCount = laborOptions.Count;
        
        for (int i = 0; i < listCount; i++)
		{
			for (int j = i + 1; j < listCount; j++)
			{
				if ((laborOptions[j].isShowing && laborOptions[j].generation > laborOptions[i].generation) || (laborOptions[j].isShowing && !laborOptions[i].isShowing))
				{
					UILaborHandlerOptions oldPanel = laborOptions[j];
					laborOptions.RemoveAt(j);
					laborOptions.Insert(i, oldPanel);
				}
			}
		}

        for (int i = 0; i < laborOptions.Count; i++)
            laborOptions[i].transform.SetSiblingIndex(i);
		
        if (laborOptionsDict[ResourceType.Food].isShowing)
            laborOptionsDict[ResourceType.Food].transform.SetSiblingIndex(0);
	}

    //setting city upon city selection
    //public void SetCity(City city)
    //{
    //    this.city = city;
    //}

    public void ToggleVisibility(bool v, City city = null)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            this.city = city;
            gameObject.SetActive(true);
            activeStatus = true;
            PrepUI();

            //if (uiCityLaborCostPanel.isOpen)
            ToggleCityLaborCost();

            allContents.anchoredPosition3D = originalLoc + new Vector3(600f, 0, 0);

            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x - 600f, 0.3f).setEaseOutSine();
        }
        else
        {
            activeStatus = false;
            //uiCityLaborCostPanel.ToggleVisibility(false, true);
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
        
        foreach (UILaborHandlerOptions option in laborOptions)
        {
            //option.HideLaborIcons();
            option.gameObject.SetActive(false);
            option.ToggleVisibility(false);
        }

        laborOptions.Clear();

        foreach (UIResourceInfoPanel option in resourceOptions)
        {
            option.gameObject.SetActive(false);
        }

        resourceOptions.Clear();
        //uiCityLaborCostPanel.ResetUI();
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
        ResetUI();
    }

    //public void ToggleCityLaborCostButton()
    //{
    //    city.world.cityBuilderManager.PlaySelectAudio();
    //    ToggleCityLaborCost();
    //}

    public void ToggleCityLaborCost()
    {
        SetConsumedResourcesInfo(city.resourceManager.resourceConsumedPerMinuteDict);
        //if (uiCityLaborCostPanel.activeStatus)
        //{
        //    uiCityLaborCostPanel.ToggleVisibility(false, false);
        //}
        //else
        //{
        //    //uiCityLaborCostPanel.ToggleVisibility(true, uiCityLaborCostPanel.isOpen);
        //}
    }

	public void SetConsumedResourcesInfo(Dictionary<ResourceType, float> consumedResourcesDict)
	{
		Dictionary<ResourceType, float> costsDict = new();

		foreach (ResourceType resourceType in consumedResourcesDict.Keys)
		{
			if (consumedResourcesDict[resourceType] == 0)
			{
				resourceOptionsDict[resourceType].gameObject.SetActive(false);
				continue;
			}

			resourceOptionsDict[resourceType].gameObject.SetActive(true);
			resourceOptions.Add(resourceOptionsDict[resourceType]);
			resourceOptionsDict[resourceType].SetNegativeAmount(consumedResourcesDict[resourceType]);
			costsDict[resourceType] = consumedResourcesDict[resourceType];
		}

		SortConsumedResourcesInfo(costsDict);
	}

	public void SortConsumedResourcesInfo(Dictionary<ResourceType, float> costsDict)
	{
		int listCount = resourceOptions.Count;

		for (int i = 0; i < listCount; i++)
		{
			for (int j = i + 1; j < listCount; j++)
			{
				if ((resourceOptions[j].gameObject.activeSelf && costsDict[resourceOptions[j].resourceType] > costsDict[resourceOptions[i].resourceType]) ||
					(resourceOptions[j].gameObject.activeSelf && !resourceOptions[i].gameObject.activeSelf))
				{
					UIResourceInfoPanel oldPanel = resourceOptions[j];
					resourceOptions.RemoveAt(j);
					resourceOptions.Insert(i, oldPanel);
				}
			}
		}

		for (int i = 0; i < resourceOptions.Count; i++)
			resourceOptions[i].transform.SetSiblingIndex(i);

		if (resourceOptionsDict[ResourceType.Gold].gameObject.activeSelf)
			resourceOptionsDict[ResourceType.Gold].transform.SetSiblingIndex(0);

		if (resourceOptionsDict[ResourceType.Food].gameObject.activeSelf)
			resourceOptionsDict[ResourceType.Food].transform.SetSiblingIndex(0);
	}

	public void UpdateConsumedResources(List<ResourceType> consumedResourceTypes, Dictionary<ResourceType, float> consumedResourcesDict)
	{
		foreach (ResourceType resourceType in consumedResourceTypes)
		{
			if (consumedResourcesDict[resourceType] > 0)
			{
				if (!resourceOptions.Contains(resourceOptionsDict[resourceType]))
				{
					resourceOptionsDict[resourceType].gameObject.SetActive(true);
					resourceOptions.Add(resourceOptionsDict[resourceType]);
				}

				resourceOptionsDict[resourceType].SetNegativeAmount(consumedResourcesDict[resourceType]);
			}
			else if (consumedResourcesDict[resourceType] == 0 && resourceOptions.Contains(resourceOptionsDict[resourceType]))
			{
				resourceOptionsDict[resourceType].gameObject.SetActive(false);
				resourceOptions.Remove(resourceOptionsDict[resourceType]);
			}
		}
	}

	public void UpdateResourcesConsumed(List<ResourceType> consumedResourceTypes, Dictionary<ResourceType, float> consumedResourcesDict)
    {
        UpdateConsumedResources(consumedResourceTypes, consumedResourcesDict);
    }

    public void PlusMinusOneLabor(ResourceType resourceType, int laborCount, int laborChange, float resourceGeneration)
    {
        if (laborCount > 0)
        {
            if (!laborOptionsDict[resourceType].isShowing)
            {
                laborOptionsDict[resourceType].ToggleVisibility(true);
                laborOptions.Add(laborOptionsDict[resourceType]);
            }
        }
        else
        {
            if (laborOptionsDict[resourceType].isShowing)
            {
                laborOptionsDict[resourceType].ToggleVisibility(false);
                laborOptions.Remove(laborOptionsDict[resourceType]);
            }
        }

        laborOptionsDict[resourceType].AddSubtractUICount(laborCount, laborChange, resourceGeneration);
    }

    public void UpdateUICount(ResourceType type, float resourceGeneration)
    {
        laborOptionsDict[type].UpdateResourceGenerationNumbers(resourceGeneration);
    }
}
