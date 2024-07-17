using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class UIMarketPlaceManager : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;
    
    [SerializeField]
    private GameObject uiMarketResourcePanel;

    [SerializeField]
    private Transform resourceHolder;

    List<UIMarketResourcePanel> marketResourceList = new();
    Dictionary<ResourceType, UIMarketResourcePanel> marketResourceDict = new(); //for updating data
    List<string> resourceNames = new(); //for sorting alphabetically

    //for sorting
    [SerializeField]
    private Image sortResources, sortPrices, sortAmounts, sortSell, sortPurchaseAmounts, sortSalesForecast;
    [SerializeField]
    public Sprite buttonUp, arrowUp, arrowDown;
    private Sprite buttonDown;
    private bool sortResourcesUp, sortPricesUp, sortAmountUp, sortSellUp, sortPurchaseAmountsUp, sortSalesForecastUp, resourcesSorted, pricesSorted, amountSorted, sellSorted, purchaseAmountsSorted, 
        salesForecastSorted;
    private Color sortButtonOriginalColor;

    [HideInInspector]
    public City city;

    [SerializeField] //for tweening
    private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus, isTyping;
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

            if (resourcesSorted)
                SortResources(sortResourcesUp);
            else if (pricesSorted)
                SortPrices(sortPricesUp);
            else if (amountSorted)
                SortAmounts(sortAmountUp);
            else if (purchaseAmountsSorted)
                SortPurchaseAmounts(sortPurchaseAmountsUp);
            else if (salesForecastSorted)
                SortSalesForecast(sortSalesForecastUp);
            else if (sellSorted)
                SortSell(sortSellUp);
            else
            {
                SortAmounts(false);
                SortSell(false);
                ChangeSortButtonsColors("sell");
            }

			allContents.anchoredPosition3D = originalLoc + new Vector3(0, -1200f, 0);

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 1200f, 0.4f).setEaseOutSine();
            //LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear();

		}
        else
        {
            isTyping = false;
            activeStatus = false;
            this.city = null;
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
        //if (!resource.sellResource)
        //    return;
        //delete above to allow all to be sold (except world resources)

        GameObject marketResourceGO = Instantiate(uiMarketResourcePanel, resourceHolder);
		UIMarketResourcePanel marketResource = marketResourceGO.GetComponent<UIMarketResourcePanel>();
		marketResource.SetMarketPlaceManager(this);
		marketResource.SetPrice(resource.resourcePrice);
        marketResource.SetPriceChangeImage(0);
        int currentPop = 1;
        float purchaseAmountMultiple = 1;
        
        if (activeStatus)
        {
            currentPop = world.cityBuilderManager.SelectedCity.currentPop;
            purchaseAmountMultiple = world.cityBuilderManager.SelectedCity.purchaseAmountMultiple;
        }

        if (ResourceHolder.Instance.GetSell(resource.resourceType))
        {
            if (resource.resourceType == ResourceType.Food)
                marketResource.SetPurchaseAmount(world.resourcePurchaseAmountDict[resource.resourceType]);
            else
				marketResource.SetPurchaseAmount(world.resourcePurchaseAmountDict[resource.resourceType] * purchaseAmountMultiple);
		}
        else
        {
            marketResource.cityPurchaseAmount.gameObject.SetActive(false);
            marketResource.cityPrice.gameObject.SetActive(false);
            marketResource.priceChangeImage.gameObject.SetActive(false);
            marketResource.citySalesForecast.gameObject.SetActive(false);
        }
        
        marketResource.canSell = resource.sellResource;
        marketResource.sell = resource.sellResource;
        //marketResource.minimumAmount.gameObject.SetActive(marketResource.sell);
		marketResource.resourceImage.sprite = resource.resourceIcon;
        marketResource.SetResourceType(resource.resourceType, resource.resourceName);
        resourceNames.Add(resource.resourceName);

        if (resource.resourceType == ResourceType.Food)
			marketResource.sellToggle.interactable = false;

        if (!resource.sellResource)
            marketResource.sellToggle.gameObject.SetActive(false);

        marketResource.priceChangeImage.gameObject.SetActive(false);
		marketResourceList.Add(marketResource);
        marketResourceDict[resource.resourceType] = marketResource;
	}

    public void UpdateMarketPlaceManager(ResourceType type)
    {
        CreateMarketResourcePanel(ResourceHolder.Instance.GetData(type));
    }

    public void UpdatePrices()
    {
		foreach (UIMarketResourcePanel resourcePanel in marketResourceList)
        {
            resourcePanel.SetPrice(city.resourceManager.resourcePriceDict[resourcePanel.resourceType]);
			resourcePanel.SetPriceChangeImage(city.resourceManager.resourcePriceChangeDict[resourcePanel.resourceType]);
            resourcePanel.SetForecastedSales(city.currentPop);
		}
	}

    public void UpdatePurchaseAmounts()
    {
        foreach (UIMarketResourcePanel resourcePanel in marketResourceList)
        {
            if (resourcePanel.purchaseAmount > 0)
            {
                if (resourcePanel.resourceType == ResourceType.Food)
					resourcePanel.SetPurchaseAmount(world.resourcePurchaseAmountDict[resourcePanel.resourceType]);
                else
                    resourcePanel.SetPurchaseAmount(city.purchaseAmountMultiple * world.resourcePurchaseAmountDict[resourcePanel.resourceType]);
			}
        }
	}

    private void SetResourceData()
    {
        foreach (UIMarketResourcePanel resourcePanel in marketResourceList)
        {
            ResourceType type = resourcePanel.resourceType;
            
            resourcePanel.SetPrice(city.resourceManager.resourcePriceDict[type]);
            resourcePanel.SetPriceChangeImage(city.resourceManager.resourcePriceChangeDict[type]);
            resourcePanel.SetAmount(city.resourceManager.resourceDict[type]);
            if (resourcePanel.purchaseAmount > 0)
            {
                if (type == ResourceType.Food)
                    resourcePanel.SetPurchaseAmount(world.resourcePurchaseAmountDict[type]);
                else
					resourcePanel.SetPurchaseAmount(city.purchaseAmountMultiple * world.resourcePurchaseAmountDict[type]);
			}

            bool isOn = city.resourceManager.resourceSellList.Contains(type);
            resourcePanel.sellToggle.isOn = isOn;
            //resourcePanel.minimumAmount.interactable = isOn;

            if (isOn)
            {
                //    resourcePanel.minimumAmount.text = city.resourceManager.resourceMinHoldDict[type].ToString();
				resourcePanel.citySalesForecast.gameObject.SetActive(true);
                resourcePanel.SetForecastedSales(city.currentPop);
            }
            else
            {
                resourcePanel.citySalesForecast.gameObject.SetActive(false);
			}

            int maxHold = city.resourceManager.resourceMaxHoldDict[type];
            resourcePanel.maximumAmount.text = maxHold < 0 ? city.warehouseStorageLimit.ToString(): maxHold.ToString();
        }
    }

    public void SetResourceSell(ResourceType type, bool isOn)
    {
        if (isOn)
            city.resourceManager.resourceSellList.Add(type);
        else
            city.resourceManager.resourceSellList.Remove(type);
    }

    //public void SetResourceMinHold(ResourceType type, int minHold, UIMarketResourcePanel panel)
    //{
    //    if (minHold >= city.warehouseStorageLimit)
    //    {
    //        city.resourceManager.resourceMinHoldDict[type] = city.warehouseStorageLimit;
    //        panel.minimumAmount.text = city.warehouseStorageLimit.ToString();
    //    }
    //    else
    //    {
    //        city.resourceManager.resourceMinHoldDict[type] = minHold;
    //    }
    //}

    public void SetResourceMaxHold(ResourceType type, int maxHold, UIMarketResourcePanel panel)
    {
        if (maxHold >= city.warehouseStorageLimit)
        {
            city.resourceManager.resourceMaxHoldDict[type] = -1;
            panel.maximumAmount.text = city.warehouseStorageLimit.ToString();
        }
        else
        {
			city.resourceManager.resourceMaxHoldDict[type] = maxHold;
		}
    }

    public void UpdateMarketResourceNumbers(ResourceType type, int amount/*, int total*/)
    {
        marketResourceDict[type].SetAmount(amount);
        marketResourceDict[type].SetForecastedSales(city.currentPop);
    }

    //sort button coloring
    private void ChangeSortButtonsColors(string column)
    {
        switch (column)
        {
            case "resources":
                sortResources.color = Color.green;
                sortPrices.color = sortButtonOriginalColor;
                sortAmounts.color = sortButtonOriginalColor;
                sortSell.color = sortButtonOriginalColor;
                sortPurchaseAmounts.color = sortButtonOriginalColor;
                sortSalesForecast.color = sortButtonOriginalColor;
				resourcesSorted = true;
                pricesSorted = false;
                amountSorted = false;
                purchaseAmountsSorted = false;
                sellSorted = false;
                salesForecastSorted = false;
                break;
            case "prices":
                sortResources.color = sortButtonOriginalColor;
                sortPrices.color = Color.green;
                sortAmounts.color = sortButtonOriginalColor;
                sortSell.color = sortButtonOriginalColor;
				sortPurchaseAmounts.color = sortButtonOriginalColor;
				sortSalesForecast.color = sortButtonOriginalColor;
				resourcesSorted = false;
				pricesSorted = true;
				amountSorted = false;
				purchaseAmountsSorted = false;
				sellSorted = false;
				salesForecastSorted = false;
				break;
            case "amount":
                sortResources.color = sortButtonOriginalColor;
                sortPrices.color = sortButtonOriginalColor;
                sortAmounts.color = Color.green;
                sortSell.color = sortButtonOriginalColor;
				sortPurchaseAmounts.color = sortButtonOriginalColor;
				sortSalesForecast.color = sortButtonOriginalColor;
				resourcesSorted = false;
				pricesSorted = false;
				amountSorted = true;
				purchaseAmountsSorted = false;
				sellSorted = false;
				salesForecastSorted = false;
				break;
            case "purchaseAmount":
                sortResources.color = sortButtonOriginalColor;
                sortPrices.color = sortButtonOriginalColor;
                sortAmounts.color = sortButtonOriginalColor;
                sortSell.color = sortButtonOriginalColor;
				sortPurchaseAmounts.color = Color.green;
				sortSalesForecast.color = sortButtonOriginalColor;
				resourcesSorted = false;
				pricesSorted = false;
				amountSorted = false;
				purchaseAmountsSorted = true;
				sellSorted = false;
				salesForecastSorted = false;
				break;
            case "sell":
                sortResources.color = sortButtonOriginalColor;
                sortPrices.color = sortButtonOriginalColor;
                sortAmounts.color = sortButtonOriginalColor;
                sortSell.color = Color.green;
			    sortPurchaseAmounts.color = sortButtonOriginalColor;
				sortSalesForecast.color = sortButtonOriginalColor;
				resourcesSorted = false;
				pricesSorted = false;
				amountSorted = false;
				purchaseAmountsSorted = false;
				sellSorted = true;
				salesForecastSorted = false;
				break;
            case "forecast":
				sortResources.color = sortButtonOriginalColor;
				sortPrices.color = sortButtonOriginalColor;
				sortAmounts.color = sortButtonOriginalColor;
				sortSell.color = sortButtonOriginalColor;
				sortPurchaseAmounts.color = sortButtonOriginalColor;
				sortSalesForecast.color = Color.green;
				resourcesSorted = false;
				pricesSorted = false;
				amountSorted = false;
				purchaseAmountsSorted = false;
				sellSorted = false;
				salesForecastSorted = true;
				break;
        }
    }

    private void ResetSortButtonColors()
    {
        sortResources.color = sortButtonOriginalColor;
        sortPrices.color = sortButtonOriginalColor;
        sortAmounts.color = sortButtonOriginalColor;
        //sortTotals.color = sortButtonOriginalColor;
        sortSell.color = sortButtonOriginalColor;
        sortPurchaseAmounts.color = sortButtonOriginalColor;
    }



    //sorting
    public void SortResources()
    {
        city.world.cityBuilderManager.PlaySelectAudio();
        ChangeSortButtonsColors("resources");

        sortResources.sprite = sortResourcesUp ? buttonUp : buttonDown;

        SortResources(sortResourcesUp);
        sortResourcesUp = !sortResourcesUp;
    }

    private void SortResources(bool up)
    {
		marketResourceList = up ? marketResourceList.OrderBy(m => m.resourceName).ToList() : marketResourceList.OrderByDescending(m => m.resourceName).ToList();

        //reorder on UI
		for (int i = 0; i < marketResourceList.Count; i++)
			marketResourceList[i].transform.SetSiblingIndex(i);
	}

    public void SortPrices()
    {
		city.world.cityBuilderManager.PlaySelectAudio();
		ChangeSortButtonsColors("prices");

        sortPrices.sprite = sortPricesUp ? buttonUp : buttonDown;
        
        SortPrices(sortPricesUp);
        sortPricesUp = !sortPricesUp;        
    }

    private void SortPrices(bool up)
    {
		int listCount = marketResourceList.Count;

		if (up)
		{
			for (int i = 0; i < listCount; i++)
			{
				for (int j = i + 1; j < listCount; j++)
				{
					if (marketResourceList[j].canSell && marketResourceList[j].price < marketResourceList[i].price || (marketResourceList[j].canSell && !marketResourceList[i].canSell))
					{
						UIMarketResourcePanel oldPanel = marketResourceList[j];
						marketResourceList.RemoveAt(j);
						marketResourceList.Insert(i, oldPanel);
					}
				}
			}
		}
		else
		{
			for (int i = 0; i < listCount; i++)
			{
				for (int j = i + 1; j < listCount; j++)
				{
					if (marketResourceList[j].canSell && marketResourceList[j].price > marketResourceList[i].price || (marketResourceList[j].canSell && !marketResourceList[i].canSell))
					{
						UIMarketResourcePanel oldPanel = marketResourceList[j];
						marketResourceList.RemoveAt(j);
						marketResourceList.Insert(i, oldPanel);
					}
				}
			}
		}

		//marketResourceList = up ? marketResourceList.OrderBy(m => m.price).ToList() : marketResourceList.OrderByDescending(m => m.price).ToList();

		//reorder on UI
		for (int i = 0; i < marketResourceList.Count; i++)
            marketResourceList[i].transform.SetSiblingIndex(i);
    }

    public void SortAmounts()
    {
		city.world.cityBuilderManager.PlaySelectAudio();
		ChangeSortButtonsColors("amount");

        sortAmounts.sprite = sortAmountUp ? buttonUp : buttonDown;

        SortAmounts(sortAmountUp);
        sortAmountUp = !sortAmountUp;
    }

    private void SortAmounts(bool up)
    {
		marketResourceList = up ? marketResourceList.OrderBy(m => m.amount).ToList() : marketResourceList.OrderByDescending(m => m.amount).ToList();

		//reorder on UI
		for (int i = 0; i < marketResourceList.Count; i++)
            marketResourceList[i].transform.SetSiblingIndex(i);
    }

    public void SortPurchaseAmounts()
    {
		city.world.cityBuilderManager.PlaySelectAudio();
		ChangeSortButtonsColors("purchaseAmount");

        sortPurchaseAmounts.sprite = sortPurchaseAmountsUp ? buttonUp : buttonDown;

        SortPurchaseAmounts(sortPurchaseAmountsUp);
        sortPurchaseAmountsUp = !sortPurchaseAmountsUp;
	}

    private void SortPurchaseAmounts(bool up)
    {
		int listCount = marketResourceList.Count;

		if (up)
		{
			for (int i = 0; i < listCount; i++)
			{
				for (int j = i + 1; j < listCount; j++)
				{
					if (marketResourceList[j].canSell && marketResourceList[j].purchaseAmount < marketResourceList[i].purchaseAmount || (marketResourceList[j].canSell && !marketResourceList[i].canSell))
					{
						UIMarketResourcePanel oldPanel = marketResourceList[j];
						marketResourceList.RemoveAt(j);
						marketResourceList.Insert(i, oldPanel);
					}
				}
			}
		}
		else
		{
			for (int i = 0; i < listCount; i++)
			{
				for (int j = i + 1; j < listCount; j++)
				{
					if (marketResourceList[j].canSell && marketResourceList[j].purchaseAmount > marketResourceList[i].purchaseAmount || (marketResourceList[j].canSell && !marketResourceList[i].canSell))
					{
						UIMarketResourcePanel oldPanel = marketResourceList[j];
						marketResourceList.RemoveAt(j);
						marketResourceList.Insert(i, oldPanel);
					}
				}
			}
		}

		//marketResourceList = up ? marketResourceList.OrderBy(m => m.purchaseAmount).ToList() : marketResourceList.OrderByDescending(m => m.purchaseAmount).ToList();

		for (int i = 0; i < marketResourceList.Count; i++)
            marketResourceList[i].transform.SetSiblingIndex(i);
    }

	public void SortSalesForecast()
	{
		city.world.cityBuilderManager.PlaySelectAudio();
		ChangeSortButtonsColors("forecast");

		sortSalesForecast.sprite = sortSalesForecastUp ? buttonUp : buttonDown;

		SortSalesForecast(sortSalesForecastUp);
		sortSalesForecastUp = !sortSalesForecastUp;
	}

	private void SortSalesForecast(bool up)
	{
        int listCount = marketResourceList.Count;
        
        if (up)
        {
			for (int i = 0; i < listCount; i++)
			{
				for (int j = i + 1; j < listCount; j++)
				{
					if (marketResourceList[j].sell && marketResourceList[j].salesForecast < marketResourceList[i].salesForecast || (marketResourceList[j].sell && !marketResourceList[i].sell))
					{
						UIMarketResourcePanel oldPanel = marketResourceList[j];
						marketResourceList.RemoveAt(j);
						marketResourceList.Insert(i, oldPanel);
					}
				}
			}
        }
        else
        {
            for (int i = 0; i < listCount; i++)
            {
                for (int j = i + 1; j < listCount; j++)
                {
                    if (marketResourceList[j].sell && marketResourceList[j].salesForecast > marketResourceList[i].salesForecast || (marketResourceList[j].sell && !marketResourceList[i].sell))
                    {
                        UIMarketResourcePanel oldPanel = marketResourceList[j];
						marketResourceList.RemoveAt(j);
						marketResourceList.Insert(i, oldPanel);
                    }
                }
            }
		}
        
        //marketResourceList = up ? marketResourceList.OrderBy(m => m.salesForecast).ToList() : marketResourceList.OrderByDescending(m => m.salesForecast).ToList();

		for (int i = 0; i < listCount; i++)
			marketResourceList[i].transform.SetSiblingIndex(i);
	}

	public void SortSell()
    {
		city.world.cityBuilderManager.PlaySelectAudio();
		ChangeSortButtonsColors("sell");

        sortSell.sprite = sortSellUp ? buttonUp : buttonDown;

        SortSell(sortSellUp);
        sortSellUp = !sortSellUp;
    }

    private void SortSell(bool up)
    {
		int listCount = marketResourceList.Count;

		if (up)
		{
			for (int i = 0; i < listCount; i++)
			{
				for (int j = i + 1; j < listCount; j++)
				{
					if (marketResourceList[j].canSell && !marketResourceList[j].sell && marketResourceList[i].sell || (marketResourceList[j].canSell && !marketResourceList[i].canSell))
					{
						UIMarketResourcePanel oldPanel = marketResourceList[j];
						marketResourceList.RemoveAt(j);
						marketResourceList.Insert(i, oldPanel);
					}
				}
			}
		}
		else
		{
			for (int i = 0; i < listCount; i++)
			{
				for (int j = i + 1; j < listCount; j++)
				{
					if (marketResourceList[j].canSell && marketResourceList[j].sell && !marketResourceList[i].sell || (marketResourceList[j].canSell && !marketResourceList[i].canSell))
					{
						UIMarketResourcePanel oldPanel = marketResourceList[j];
						marketResourceList.RemoveAt(j);
						marketResourceList.Insert(i, oldPanel);
					}
				}
			}
		}

		//marketResourceList = up ? marketResourceList.OrderBy(m => m.sell).ToList() : marketResourceList.OrderByDescending(m => m.sell).ToList();

		//reorder on UI
		for (int i = 0; i < marketResourceList.Count; i++)
            marketResourceList[i].transform.SetSiblingIndex(i);
    }
}
