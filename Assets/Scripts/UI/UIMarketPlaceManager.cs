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
    private Image sortResources, sortPrices, sortAmounts, sortSell, sortPurchaseAmounts;
    [SerializeField]
    private Sprite buttonUp;
    private Sprite buttonDown;
    private bool sortResourcesUp, sortPricesUp, sortAmountUp, sortSellUp, sortPurchaseAmountsUp, resourcesSorted, pricesSorted, amountSorted, sellSorted, purchaseAmountsSorted;
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

            SortAmounts(false);
			SortSell(false);
			ChangeSortButtonsColors("sell");
			sortSellUp = !sortSellUp;

			allContents.anchoredPosition3D = originalLoc + new Vector3(0, -1200f, 0);

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 1200f, 0.8f).setEaseOutBack();
            //LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear();

            if (resourcesSorted)
                SortResources(sortResourcesUp);
            else if (pricesSorted)
                SortPrices(sortPricesUp);
			else if (amountSorted)
				SortAmounts(sortAmountUp);
			else if (purchaseAmountsSorted)
				SortPurchaseAmounts(sortPurchaseAmountsUp);
			else if (sellSorted)
				SortSell(sortSellUp);
		}
        else
        {
            isTyping = false;
            activeStatus = false;
            this.city = null;
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -1200f, 0.5f).setOnComplete(SetActiveStatusFalse);
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
        marketResource.SetPurchaseAmount(resource.resourceQuantityPerPop);
        marketResource.sell = resource.sellResource;
        //marketResource.minimumAmount.gameObject.SetActive(marketResource.sell);
		marketResource.resourceImage.sprite = resource.resourceIcon;
        marketResource.SetResourceType(resource.resourceType, resource.resourceName);
        resourceNames.Add(resource.resourceName);

        if (resource.resourceType == ResourceType.Food)
			marketResource.sellToggle.interactable = false;

        if (!resource.sellResource)
            marketResource.sellToggle.gameObject.SetActive(false);

		marketResourceList.Add(marketResource);
        marketResourceDict[resource.resourceType] = marketResource;
	}

    public void UpdateMarketPlaceManager(ResourceType type)
    {
        CreateMarketResourcePanel(ResourceHolder.Instance.GetData(type));
    }

    public void UpdatePurchaseAmounts()
    {
        foreach (UIMarketResourcePanel resourcePanel in marketResourceList)
            resourcePanel.SetPurchaseAmount(city.purchaseAmountMultiple * ResourceHolder.Instance.GetPurchaseAmount(resourcePanel.resourceType));
	}

    private void SetResourceData()
    {
        foreach (UIMarketResourcePanel resourcePanel in marketResourceList)
        {
            ResourceType type = resourcePanel.resourceType;
            
            resourcePanel.SetPrice(city.resourceManager.resourcePriceDict[type]);
            resourcePanel.SetAmount(city.resourceManager.resourceDict[type]);
            resourcePanel.SetPurchaseAmount(city.purchaseAmountMultiple * ResourceHolder.Instance.GetPurchaseAmount(type));

            bool isOn = city.resourceManager.resourceSellList.Contains(type);
            resourcePanel.sellToggle.isOn = isOn;
            //resourcePanel.minimumAmount.interactable = isOn;

            //if (isOn)
            //    resourcePanel.minimumAmount.text = city.resourceManager.resourceMinHoldDict[type].ToString();

            int maxHold = city.resourceManager.resourceMaxHoldDict[type];
            resourcePanel.maximumAmount.text = maxHold < 0 ? city.warehouseStorageLimit.ToString(): maxHold.ToString();
        }
    }

    public void SetResourceSell(ResourceType type, bool isOn)
    {
        if (isOn)
        {
            if (!city.resourceManager.resourceSellList.Contains(type))
                city.resourceManager.resourceSellList.Add(type);
        }
        else
        {
            city.resourceManager.resourceSellList.Remove(type);
        }
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
                resourcesSorted = true;
                pricesSorted = false;
                amountSorted = false;
                purchaseAmountsSorted = false;
                sellSorted = false;
                break;
            case "prices":
                sortResources.color = sortButtonOriginalColor;
                sortPrices.color = Color.green;
                sortAmounts.color = sortButtonOriginalColor;
                sortSell.color = sortButtonOriginalColor;
				sortPurchaseAmounts.color = sortButtonOriginalColor;
				resourcesSorted = false;
				pricesSorted = true;
				amountSorted = false;
				purchaseAmountsSorted = false;
				sellSorted = false;
				break;
            case "amount":
                sortResources.color = sortButtonOriginalColor;
                sortPrices.color = sortButtonOriginalColor;
                sortAmounts.color = Color.green;
                sortSell.color = sortButtonOriginalColor;
				sortPurchaseAmounts.color = sortButtonOriginalColor;
				resourcesSorted = false;
				pricesSorted = false;
				amountSorted = true;
				purchaseAmountsSorted = false;
				sellSorted = false;
				break;
            case "purchaseAmount":
                sortResources.color = sortButtonOriginalColor;
                sortPrices.color = sortButtonOriginalColor;
                sortAmounts.color = sortButtonOriginalColor;
                sortSell.color = sortButtonOriginalColor;
				sortPurchaseAmounts.color = Color.green;
				resourcesSorted = false;
				pricesSorted = false;
				amountSorted = false;
				purchaseAmountsSorted = true;
				sellSorted = false;
				break;
            case "sell":
                sortResources.color = sortButtonOriginalColor;
                sortPrices.color = sortButtonOriginalColor;
                sortAmounts.color = sortButtonOriginalColor;
                sortSell.color = Color.green;
			    sortPurchaseAmounts.color = sortButtonOriginalColor;
				resourcesSorted = false;
				pricesSorted = false;
				amountSorted = false;
				purchaseAmountsSorted = false;
				sellSorted = true;
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
		marketResourceList = up ? marketResourceList.OrderBy(m => m.price).ToList() : marketResourceList.OrderByDescending(m => m.price).ToList();

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

    public void SortPurchaseAmounts(bool up)
    {
        marketResourceList = up ? marketResourceList.OrderBy(m => m.purchaseAmount).ToList() : marketResourceList.OrderByDescending(m => m.purchaseAmount).ToList();

        for (int i = 0; i < marketResourceList.Count; i++)
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
		marketResourceList = up ? marketResourceList.OrderBy(m => m.sell).ToList() : marketResourceList.OrderByDescending(m => m.sell).ToList();

		//reorder on UI
		for (int i = 0; i < marketResourceList.Count; i++)
            marketResourceList[i].transform.SetSiblingIndex(i);
    }
}
