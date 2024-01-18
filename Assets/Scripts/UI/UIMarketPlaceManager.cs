using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;

public class UIMarketPlaceManager : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;
    
    [SerializeField]
    private GameObject uiMarketResourcePanel;

    [SerializeField]
    private Transform resourceHolder;

    List<UIMarketResourcePanel> marketResourceList = new();
    List<string> resourceNames = new(); //for sorting alphabetically

    //for sorting
    [SerializeField]
    private Image sortResources, sortPrices, sortAmounts, /*sortTotals,*/ sortSell;
    [SerializeField]
    private Sprite buttonUp;
    private Sprite buttonDown;
    private bool sortResourcesUp, sortPricesUp, sortAmountUp, /*sortTotalsUp,*/ sortSellUp;
    private Color sortButtonOriginalColor;

    [HideInInspector]
    public City city;

    [SerializeField] //for tweening
    private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus;
    private Vector3 originalLoc;


    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;
        buttonDown = sortResources.sprite;
        //sortButtonOriginalColor = sortResources.color;
        sortButtonOriginalColor = sortResources.color;
        gameObject.SetActive(false);

        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
        {
            if (!world.ResourceCheck(resource.resourceType))
                continue;

            CreateMarketResourcePanel(resource);

			//if (resource.resourceType == ResourceType.Food)
   //             marketResourceList[marketResourceList.Count - 1].sellToggle.interactable = false;
		}

		resourceNames.Sort();
    }

    public void ToggleVisibility(bool v, City city = null)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(v);
            this.city = city;
            SetResourceData();

            activeStatus = true;

            SortAmounts(false);
			SortSell(false);
			ChangeSortButtonsColors("sell");
			sortSellUp = !sortSellUp;

			allContents.anchoredPosition3D = originalLoc + new Vector3(0, -1200f, 0);

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 1200f, 0.4f).setEaseOutBack();
            //LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear();
        }
        else
        {
            activeStatus = false;
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -1200f, 0.2f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        ResetSortButtonColors();
        gameObject.SetActive(false);
    }

    private void CreateMarketResourcePanel(ResourceIndividualSO resource)
    {
        //currently only allowing sellable resources to be sold
        if (!resource.sellResource)
            return;
        //delete above to allow all to be sold (except world resources)

        GameObject marketResourceGO = Instantiate(uiMarketResourcePanel, resourceHolder);
		UIMarketResourcePanel marketResource = marketResourceGO.GetComponent<UIMarketResourcePanel>();
		marketResource.SetMarketPlaceManager(this);
		marketResource.SetPrice(resource.resourcePrice);
        marketResource.sell = resource.sellResource;
        marketResource.minimumAmount.gameObject.SetActive(marketResource.sell);
		marketResource.resourceImage.sprite = resource.resourceIcon;
        marketResource.SetResourceType(resource.resourceType, resource.resourceName);
		resourceNames.Add(resource.resourceName);

        if (resource.resourceType == ResourceType.Food)
			marketResource.sellToggle.interactable = false;

		marketResourceList.Add(marketResource);
	}

    public void UpdateMarketPlaceManager(ResourceType type)
    {
        CreateMarketResourcePanel(ResourceHolder.Instance.GetData(type));
    }

    private void SetResourceData()
    {
        foreach (UIMarketResourcePanel resourcePanel in marketResourceList)
        {
            ResourceType type = resourcePanel.resourceType;
            
            resourcePanel.SetPrice(city.ResourceManager.resourcePriceDict[type]);
            //resourcePanel.cityPrice.text = city.ResourceManager.resourcePriceDict[type].ToString();
            resourcePanel.SetAmount(city.ResourceManager.ResourceDict[type]);
            //resourcePanel.cityAmount.text = city.ResourceManager.ResourceDict[type].ToString();
            //resourcePanel.total = city.ResourceManager.resourceSellHistoryDict[type];
            //resourcePanel.cityTotals.text = city.ResourceManager.resourceSellHistoryDict[type].ToString();

            bool isOn = city.ResourceManager.resourceSellList.Contains(type);
            resourcePanel.sellToggle.isOn = isOn;
            resourcePanel.minimumAmount.interactable = isOn;

            if (isOn)
                resourcePanel.minimumAmount.text = city.ResourceManager.resourceMinHoldDict[type].ToString();
        }
    }

    public void SetResourceSell(ResourceType type, bool isOn)
    {
        if (isOn)
        {
            if (!city.ResourceManager.resourceSellList.Contains(type))
                city.ResourceManager.resourceSellList.Add(type);
        }
        else
        {
            city.ResourceManager.resourceSellList.Remove(type);
        }
    }

    public void SetResourceMinHold(ResourceType resourceType, int minHold)
    {
        city.ResourceManager.resourceMinHoldDict[resourceType] = minHold;
    }

    public void UpdateMarketResourceNumbers(ResourceType resourceType, int amount/*, int total*/)
    {
        foreach (UIMarketResourcePanel resourcePanel in marketResourceList)
        {
            if (resourcePanel.resourceType == resourceType)
            {
                //resourcePanel.SetPrice(price);
                //resourcePanel.cityPrice.text = price.ToString();
                
                resourcePanel.SetAmount(amount);
                //resourcePanel.cityAmount.text = amount.ToString();

                //resourcePanel.total = total;
                //resourcePanel.cityTotals.text = total.ToString();
                return;
            }
        }
    }



    //sort button coloring
    private void ChangeSortButtonsColors(string column)
    {
        if (column == "resources")
        {
            sortResources.color = Color.green;
            sortPrices.color = sortButtonOriginalColor;
            sortAmounts.color = sortButtonOriginalColor;
            //sortTotals.color = sortButtonOriginalColor;
            sortSell.color = sortButtonOriginalColor;
        }
        else if (column == "prices")
        {
            sortResources.color = sortButtonOriginalColor;
            sortPrices.color = Color.green;
            sortAmounts.color = sortButtonOriginalColor;
            //sortTotals.color = sortButtonOriginalColor;
            sortSell.color = sortButtonOriginalColor;
        }
        else if (column == "amount")
        {
            sortResources.color = sortButtonOriginalColor;
            sortPrices.color = sortButtonOriginalColor;
            sortAmounts.color = Color.green;
            //sortTotals.color = sortButtonOriginalColor;
            sortSell.color = sortButtonOriginalColor;
        }
        else if (column == "total")
        {
            sortResources.color = sortButtonOriginalColor;
            sortPrices.color = sortButtonOriginalColor;
            sortAmounts.color = sortButtonOriginalColor;
            //sortTotals.color = Color.green;
            sortSell.color = sortButtonOriginalColor;
        }
        else if (column == "sell")
        {
            sortResources.color = sortButtonOriginalColor;
            sortPrices.color = sortButtonOriginalColor;
            sortAmounts.color = sortButtonOriginalColor;
            //sortTotals.color = sortButtonOriginalColor;
            sortSell.color = Color.green;
        }
    }

    private void ResetSortButtonColors()
    {
        sortResources.color = sortButtonOriginalColor;
        sortPrices.color = sortButtonOriginalColor;
        sortAmounts.color = sortButtonOriginalColor;
        //sortTotals.color = sortButtonOriginalColor;
        sortSell.color = sortButtonOriginalColor;
    }



    //sorting algos
    public void SortResources()
    {
        city.world.cityBuilderManager.PlaySelectAudio();
        ChangeSortButtonsColors("resources");

        if (sortResourcesUp)
            sortResources.sprite = buttonUp;
        else
            sortResources.sprite = buttonDown;

        SortResources(sortResourcesUp);
        sortResourcesUp = !sortResourcesUp;
    }

    private void SortResources(bool up)
    {
        //reorder on UI
        if (up) //ascending
            marketResourceList = marketResourceList.OrderBy(m => m.resourceName).ToList();
        else //descending
            marketResourceList = marketResourceList.OrderByDescending(m => m.resourceName).ToList();

		for (int i = 0; i < marketResourceList.Count; i++)
			marketResourceList[i].transform.SetSiblingIndex(i);
	}

    public void SortPrices()
    {
		city.world.cityBuilderManager.PlaySelectAudio();
		ChangeSortButtonsColors("prices");
            
        if (sortPricesUp)
            sortPrices.sprite = buttonUp;
        else
            sortPrices.sprite = buttonDown;
        
        SortPrices(sortPricesUp);
        sortPricesUp = !sortPricesUp;        
    }

    private void SortPrices(bool up)
    {
		if (up) //ascending
			marketResourceList = marketResourceList.OrderBy(m => m.price).ToList();
		else //descending
			marketResourceList = marketResourceList.OrderByDescending(m => m.price).ToList();

		//reorder on UI
		for (int i = 0; i < marketResourceList.Count; i++)
            marketResourceList[i].transform.SetSiblingIndex(i);
    }

    public void SortAmounts()
    {
		city.world.cityBuilderManager.PlaySelectAudio();
		ChangeSortButtonsColors("amount");

        if (sortAmountUp)
            sortAmounts.sprite = buttonUp;
        else
            sortAmounts.sprite = buttonDown;

        SortAmounts(sortAmountUp);
        sortAmountUp = !sortAmountUp;
    }

    private void SortAmounts(bool up)
    {
		if (up) //ascending
			marketResourceList = marketResourceList.OrderBy(m => m.amount).ToList();
		else //descending
			marketResourceList = marketResourceList.OrderByDescending(m => m.amount).ToList();

		//reorder on UI
		for (int i = 0; i < marketResourceList.Count; i++)
            marketResourceList[i].transform.SetSiblingIndex(i);
    }

  //  public void SortTotals()
  //  {
		//city.world.cityBuilderManager.PlaySelectAudio();
		//ChangeSortButtonsColors("total");

  //      if (sortTotalsUp)
  //          sortTotals.sprite = buttonUp;
  //      else
  //          sortTotals.sprite = buttonDown;

  //      SortTotals(sortTotalsUp);
  //      sortTotalsUp = !sortTotalsUp;
  //  }

  //  private void SortTotals(bool up)
  //  {
		//if (up) //ascending
		//	marketResourceList = marketResourceList.OrderBy(m => m.total).ToList();
		//else //descending
		//	marketResourceList = marketResourceList.OrderByDescending(m => m.total).ToList();

		////reorder on UI
		//for (int i = 0; i < marketResourceList.Count; i++)
  //          marketResourceList[i].transform.SetSiblingIndex(i);
  //  }

    public void SortSell()
    {
		city.world.cityBuilderManager.PlaySelectAudio();
		ChangeSortButtonsColors("sell");

        if (sortSellUp)
            sortSell.sprite = buttonUp;
        else
            sortSell.sprite = buttonDown;

        SortSell(sortSellUp);
        sortSellUp = !sortSellUp;
    }

    private void SortSell(bool up)
    {
		if (up) //ascending
			marketResourceList = marketResourceList.OrderBy(m => m.sell).ToList();
		else //descending
			marketResourceList = marketResourceList.OrderByDescending(m => m.sell).ToList();

		//reorder on UI
		for (int i = 0; i < marketResourceList.Count; i++)
            marketResourceList[i].transform.SetSiblingIndex(i);
    }
}
