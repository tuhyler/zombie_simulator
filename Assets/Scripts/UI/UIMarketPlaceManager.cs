using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMarketPlaceManager : MonoBehaviour
{
    [SerializeField]
    private GameObject uiMarketResourcePanel;

    [SerializeField]
    private Transform resourceHolder;

    List<UIMarketResourcePanel> marketResourceList = new();
    List<string> resourceNames = new(); //for sorting alphabetically

    //for sorting
    [SerializeField]
    private Image sortResources, sortPrices, sortAmounts, sortSell;
    [SerializeField]
    private Sprite buttonUp;
    private Sprite buttonDown;
    private bool sortResourcesUp, sortPricesUp, sortAmountUp, sortSellUp;
    private Color sortButtonOriginalColor;

    private City city;

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
        sortButtonOriginalColor = Color.white;
        gameObject.SetActive(false);

        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
        {
            //can't sell food
            //if (resource.resourceName == "Food")
            //    continue;

            GameObject marketResourceGO = Instantiate(uiMarketResourcePanel, resourceHolder);
            UIMarketResourcePanel marketResource = marketResourceGO.GetComponent<UIMarketResourcePanel>();
            marketResource.SetMarketPlaceManager(this);
            marketResource.cityPrice.text = resource.resourcePrice.ToString();
            marketResource.price = resource.resourcePrice;
            marketResource.resourceImage.sprite = resource.resourceIcon;
            marketResource.resourceName = resource.resourceName;
            marketResource.resourceType = resource.resourceType;
            resourceNames.Add(resource.resourceName);

            marketResourceList.Add(marketResource);
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

            allContents.anchoredPosition3D = originalLoc + new Vector3(0, -1200f, 0);

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 1200f, 0.4f).setEaseOutBack();
            LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear();
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

    private void SetResourceData()
    {
        foreach (UIMarketResourcePanel resourcePanel in marketResourceList)
        {
            resourcePanel.price = city.ResourceManager.ResourcePriceDict[resourcePanel.resourceType];
            resourcePanel.cityPrice.text = city.ResourceManager.ResourcePriceDict[resourcePanel.resourceType].ToString();
            resourcePanel.amount = city.ResourceManager.ResourceDict[resourcePanel.resourceType];
            resourcePanel.cityAmount.text = city.ResourceManager.ResourceDict[resourcePanel.resourceType].ToString();

            bool isOn = city.ResourceManager.ResourceSellDict[resourcePanel.resourceType];
            resourcePanel.sellToggle.isOn = isOn;
            resourcePanel.minimumAmount.interactable = isOn;

            if (isOn)
                resourcePanel.minimumAmount.text = city.ResourceManager.ResourceMinHoldDict[resourcePanel.resourceType].ToString();
        }
    }

    public void SetResourceSell(ResourceType resourceType, bool isOn)
    {
        city.ResourceManager.ResourceSellDict[resourceType] = isOn;
    }

    public void SetResourceMinHold(ResourceType resourceType, int minHold)
    {
        city.ResourceManager.ResourceMinHoldDict[resourceType] = minHold;
    }

    public void UpdateResourcePrices(ResourceType resourceType, int price, ResourceManager resourceManager)
    {

    }

    public void UpdateResourceAmount(ResourceType resourceType, int amount, ResourceManager resourceManager)
    {
        foreach (UIMarketResourcePanel resourcePanel in marketResourceList)
        {
            if (resourcePanel.resourceType == resourceType)
            {
                resourcePanel.amount = resourceManager.ResourceDict[resourcePanel.resourceType];
                resourcePanel.cityAmount.text = resourceManager.ResourceDict[resourcePanel.resourceType].ToString();
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
            sortSell.color = sortButtonOriginalColor;
        }
        else if (column == "prices")
        {
            sortResources.color = sortButtonOriginalColor;
            sortPrices.color = Color.green;
            sortAmounts.color = sortButtonOriginalColor;
            sortSell.color = sortButtonOriginalColor;
        }
        else if (column == "amount")
        {
            sortResources.color = sortButtonOriginalColor;
            sortPrices.color = sortButtonOriginalColor;
            sortAmounts.color = Color.green;
            sortSell.color = sortButtonOriginalColor;
        }
        else if (column == "sell")
        {
            sortResources.color = sortButtonOriginalColor;
            sortPrices.color = sortButtonOriginalColor;
            sortAmounts.color = sortButtonOriginalColor;
            sortSell.color = Color.green;
        }
    }

    private void ResetSortButtonColors()
    {
        sortResources.color = sortButtonOriginalColor;
        sortPrices.color = sortButtonOriginalColor;
        sortAmounts.color = sortButtonOriginalColor;
        sortSell.color = sortButtonOriginalColor;
    }



    //sorting algos
    public void SortResources()
    {
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
        {
            for (int i = 0; i < marketResourceList.Count; i++)
            {
                int index = resourceNames.IndexOf(marketResourceList[i].resourceName);
                marketResourceList[i].transform.SetSiblingIndex(index);
            }
        }
        else //descending
        {
            int listCount = marketResourceList.Count - 1;
            for (int i = 0; i < marketResourceList.Count; i++)
            {
                int index = listCount - resourceNames.IndexOf(marketResourceList[i].resourceName);
                marketResourceList[i].transform.SetSiblingIndex(index);
            }
        }
    }

    public void SortPrices()
    {
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
        if (up) //sort prices ascending
        {
            for (int i = 0; i < marketResourceList.Count; i++)
            {
                for (int j = i + 1; j < marketResourceList.Count; j++)
                {
                    if (marketResourceList[j].price < marketResourceList[i].price)
                    {
                        UIMarketResourcePanel marketResource = marketResourceList[i];
                        marketResourceList[i] = marketResourceList[j];
                        marketResourceList[j] = marketResource;
                    }
                }
            }
        }
        else //sort descending
        {
            for (int i = 0; i < marketResourceList.Count; i++)
            {
                for (int j = i + 1; j < marketResourceList.Count; j++)
                {
                    if (marketResourceList[j].price > marketResourceList[i].price)
                    {
                        UIMarketResourcePanel marketResource = marketResourceList[i];
                        marketResourceList[i] = marketResourceList[j];
                        marketResourceList[j] = marketResource;
                    }
                }
            }
        }

        //reorder on UI
        for (int i = 0; i < marketResourceList.Count; i++)
        {
            marketResourceList[i].transform.SetSiblingIndex(i);
        }
    }

    public void SortAmounts()
    {
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
        if (up) //sort prices ascending
        {
            for (int i = 0; i < marketResourceList.Count; i++)
            {
                for (int j = i + 1; j < marketResourceList.Count; j++)
                {
                    if (marketResourceList[j].amount < marketResourceList[i].amount)
                    {
                        UIMarketResourcePanel marketResource = marketResourceList[i];
                        marketResourceList[i] = marketResourceList[j];
                        marketResourceList[j] = marketResource;
                    }
                }
            }
        }
        else //sort descending
        {
            for (int i = 0; i < marketResourceList.Count; i++)
            {
                for (int j = i + 1; j < marketResourceList.Count; j++)
                {
                    if (marketResourceList[j].amount > marketResourceList[i].amount)
                    {
                        UIMarketResourcePanel marketResource = marketResourceList[i];
                        marketResourceList[i] = marketResourceList[j];
                        marketResourceList[j] = marketResource;
                    }
                }
            }
        }

        //reorder on UI
        for (int i = 0; i < marketResourceList.Count; i++)
        {
            marketResourceList[i].transform.SetSiblingIndex(i);
        }
    }

    public void SortSell()
    {
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
        if (up) //non sells first
        {
            for (int i = 0; i < marketResourceList.Count; i++)
            {
                for (int j = i + 1; j < marketResourceList.Count; j++)
                {
                    if (marketResourceList[j].sell && !marketResourceList[i].sell)
                    {
                        UIMarketResourcePanel marketResource = marketResourceList[i];
                        marketResourceList[i] = marketResourceList[j];
                        marketResourceList[j] = marketResource;
                    }
                }
            }
        }
        else //sells first
        {
            for (int i = 0; i < marketResourceList.Count; i++)
            {
                for (int j = i + 1; j < marketResourceList.Count; j++)
                {
                    if (!marketResourceList[j].sell && marketResourceList[i].sell)
                    {
                        UIMarketResourcePanel marketResource = marketResourceList[i];
                        marketResourceList[i] = marketResourceList[j];
                        marketResourceList[j] = marketResource;
                    }
                }
            }
        }

        //reorder on UI
        for (int i = 0; i < marketResourceList.Count; i++)
        {
            marketResourceList[i].transform.SetSiblingIndex(i);
        }
    }
}
