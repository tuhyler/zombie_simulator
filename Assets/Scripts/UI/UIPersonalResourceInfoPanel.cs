using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;

public class UIPersonalResourceInfoPanel : MonoBehaviour, IGoldUpdateCheck
{
    [SerializeField]
    public MapWorld world;
    
    [SerializeField]
    private TMP_Text unitNameTitle, /*unitStoragePercent, */storageLevel, storageLimit, slash;

    [SerializeField]
    private GameObject progressBar;

    [SerializeField]
    private Image progressBarMask, buttonDown;
    [SerializeField]
    private Sprite buttonUp;
    private Sprite originalDown;

    [SerializeField] //for tweening
    private RectTransform allContents, overflowGridAll, titleBar;
    [HideInInspector]
    public bool activeStatus, overflowActiveStatus;
    private Vector3 originalLoc, overflowOriginalLoc, overflowAllOriginalLoc;
    private Vector3 loadUnloadPosition;

    public bool isCity; //indicate which window is for the city or trader
    private bool atTradeCenter;
    [HideInInspector]
    public Unit unit;
    private City city;
    private Wonder wonder;
    private TradeCenter tradeCenter;

    private int unitStorageLevel;
    private int unitStorageLimit;

    //private Dictionary<ResourceType, UIPersonalResources> personalResourceUIDictionary = new();

    //[SerializeField]
    //private Transform uiElementsParent;
    [SerializeField]
    private GameObject resourcePersonalPanel;

    //private List<UIPersonalResources> personalResources = new();

    //for clicking resource buttons
    [SerializeField]
    private UnityEvent<ResourceType> OnIconButtonClick;
    private ResourceType resourceType;
    
    [HideInInspector]
    public bool inUse, dragging; //for clicking off screen to exit trade screen

    //resource grid
    public RectTransform gridHolder, overflowGridHolder;
    private Dictionary<int, UIPersonalDropLocation> gridCellDict = new();
    private int activeCells;
    private Dictionary<ResourceType, int> resourceUIDict = new();
    public int gridWidth = 10;

    //for pricing
    private int maxBuyCost;
    public GridLayoutGroup gridGrid, overflowGrid;

    private void Awake()
    {
        if (!isCity)
        { //whole sequence is to determine where trader resource window goes during unload/load (changes based on resolution)
            originalLoc = allContents.anchoredPosition3D;
            overflowOriginalLoc = overflowGridHolder.anchoredPosition3D;
            overflowAllOriginalLoc = overflowGridAll.anchoredPosition3D;
            allContents.anchorMin = new Vector2(0.5f, 0.5f);
            allContents.anchorMax = new Vector2(0.5f, 0.5f);
            allContents.anchoredPosition3D = new Vector3(0f, 235f, 0f);
            loadUnloadPosition = allContents.transform.localPosition;
            allContents.anchorMin = new Vector2(0.5f, 1.0f);
            allContents.anchorMax = new Vector2(0.5f, 1.0f);
            allContents.anchoredPosition3D = new Vector3(0f, 0f, 0f);
        }

		storageLevel.outlineColor = Color.black;
        storageLevel.outlineWidth = 0.4f;
        slash.outlineColor = Color.black;
        slash.outlineWidth = 0.4f;
        storageLimit.outlineColor = Color.black;
        storageLimit.outlineWidth = 0.4f;

        overflowGridHolder.sizeDelta = new Vector2(gridWidth * 90, 90);
        buttonDown.transform.localPosition = new Vector3(gridWidth * 45 + 20 , -80, 0);
        gameObject.SetActive(false);
        overflowGridHolder.gameObject.SetActive(false);
        buttonDown.gameObject.SetActive(false);
        originalDown = buttonDown.sprite;

        int total = 0;
        foreach (Transform selection in gridHolder)
        {
            GridCellPrep(selection, total);
            total++;
        }

        foreach (Transform selection in overflowGridHolder)
        {
            GridCellPrep(selection, total);
            total++;
        }
    }

    private void GridCellPrep(Transform selection, int total)
    {
        UIPersonalDropLocation loc = selection.GetComponent<UIPersonalDropLocation>();
        loc.gameObject.SetActive(false);
        loc.SetUIResourceManager(this);
        loc.gridLocation = total;
        loc.resource.loc = total;
        gridCellDict.Add(loc.gridLocation, loc);
    }

    public void ToggleVisibility(bool v, Unit unit = null, City city = null, Wonder wonder = null, TradeCenter tradeCenter = null)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            this.unit = unit;
            //this.trader = trader;
            //this.worker = worker;
            this.city = city;
            this.wonder = wonder;
            this.tradeCenter = tradeCenter;
            activeCells = 0;
            buttonDown.gameObject.SetActive(false);

            if (isCity)
            {
                world.overflowGridCanvas.gameObject.SetActive(true);

                if (city)
                {
					city.ReshuffleGrid();
					city.uiCityResourceInfoPanel = this;
                    
                    foreach (ResourceType type in city.resourceGridDict.Keys)
                        ActivateCell(type, true);
                }
                else if (wonder)
                {
                    wonder.uiCityResourceInfoPanel = this;
                    
                    foreach (ResourceType type in wonder.ResourceGridDict.Keys)
                        ActivateCell(type, true);
                }
                else if (tradeCenter)
                {
					foreach (ResourceType type in tradeCenter.ResourceBuyGridDict.Keys)
                        ActivateCell(type, true, true);

                    gridGrid.cellSize = new Vector2(90, 110);
                    gridGrid.padding.top = -10;
                    gridGrid.padding.bottom = 10;
                    overflowGrid.cellSize = new Vector2(90, 110);
                    overflowGrid.padding.top = -10;
                    overflowGrid.padding.bottom = 10;
					world.goldUpdateCheck = this;
					//overflowGridAll.anchoredPosition3D = new Vector3(0, -160, 0);
				}
			}
            else
            {
				if (unit.trader)
                {
                    unit.trader.ReshuffleGrid();

				    foreach (ResourceType type in unit.trader.resourceGridDict.Keys)
                    {
                        ActivateCell(type, true);
                        gridCellDict[resourceUIDict[type]].resource.clickable = false;
                    }
                }
                else
                {
					unit.worker.ReshuffleGrid();

					foreach (ResourceType type in unit.worker.resourceGridDict.Keys)
					{
						ActivateCell(type, true);
						gridCellDict[resourceUIDict[type]].resource.clickable = false;
					}
				}
            }

            if (activeCells > gridWidth)
            {
                gridHolder.sizeDelta = new Vector2(90 * gridWidth, 90);
                if (!isCity)
                    buttonDown.gameObject.SetActive(true);
            }
            else
            {
                gridHolder.sizeDelta = new Vector2(activeCells * 90, 90);
            }

            progressBarMask.fillAmount = 0;
            gameObject.SetActive(v);
            activeStatus = true;

            if (!isCity)
            {
                allContents.anchoredPosition3D = originalLoc + new Vector3(0, 200f, 0);

                LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -200f, 0.3f).setEaseOutSine();
            }

            progressBarMask.fillAmount = (float)unitStorageLevel / unitStorageLimit;
        }
        else
        {
            for (int i = 0; i < activeCells; i++)
            {
                gridCellDict[i].gameObject.SetActive(false);
            }

            activeStatus = false;
            world.goldUpdateCheck = null;
            ToggleOverflowVisibility(false);
            resourceUIDict.Clear();

            if (!isCity)
            {
                if (this.unit.trader)
                    this.unit.trader.ReshuffleGrid(); //gets rid of zeroed out resources
                else
                    this.unit.worker.ReshuffleGrid();

                this.unit = null;
                LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 400f, 0.2f).setOnComplete(SetActiveStatusFalse);
            }
            else if (this.city)
            {
                this.city.uiCityResourceInfoPanel = null;
                this.city.ReshuffleGrid();
                this.city = null;
                gameObject.SetActive(false);
            }
            else if (this.wonder)
            {
                this.wonder.uiCityResourceInfoPanel = null;
                this.wonder = null;
                gameObject.SetActive(false);
            }
            else
            {
                foreach (ResourceType type in this.tradeCenter.ResourceBuyGridDict.Keys)
                {
                    int loc = this.tradeCenter.ResourceBuyGridDict[type];
                    gridCellDict[loc].resource.HidePricing();
                }

                gridGrid.cellSize = new Vector2(90, 90);
                gridGrid.padding.top = 0;
                gridGrid.padding.bottom = 0;
                overflowGrid.cellSize = new Vector2(90, 90);
                overflowGrid.padding.top = 0;
                overflowGrid.padding.bottom = 0;

                overflowGridAll.anchoredPosition3D = overflowAllOriginalLoc;
                maxBuyCost = 0;
                this.tradeCenter = null;
                gameObject.SetActive(false);
            }
        }
    }

    public void ToggleOverflow()
    {
        world.cityBuilderManager.PlaySelectAudio();
        
        if (overflowActiveStatus)
            ToggleOverflowVisibility(false);
        else
        {
            overflowGridAll.anchoredPosition3D = overflowAllOriginalLoc;
            ToggleOverflowVisibility(true);
        }
    }

    public void ToggleOverflowVisibility(bool v, bool instant = false)
    {
        if (overflowActiveStatus == v)
            return;

        LeanTween.cancel(overflowGridHolder.gameObject);

        if (v)
        {
            buttonDown.sprite = buttonUp;
            world.overflowGridCanvas.gameObject.SetActive(true);
            overflowGridHolder.gameObject.SetActive(v);
            overflowActiveStatus = true;
            float size = Mathf.CeilToInt((activeCells - gridWidth) / (float)gridWidth) * 90;

            overflowGridHolder.anchoredPosition3D = overflowOriginalLoc + new Vector3(0, size, 0);

            float speed = instant ? 0 : 0.3f;
            LeanTween.moveY(overflowGridHolder, overflowGridHolder.anchoredPosition3D.y + -size, speed).setEaseOutSine();
        }
        else
        {
            buttonDown.sprite = originalDown;
            overflowActiveStatus = false;
            LeanTween.moveY(overflowGridHolder, overflowGridHolder.anchoredPosition3D.y + Mathf.CeilToInt((activeCells - gridWidth) / (float)gridWidth) * 90, 0.2f).setOnComplete(SetOverflowStatusFalse);
        }
    }

    private void SetOverflowStatusFalse()
    {
        if (!isCity)
            world.overflowGridCanvas.gameObject.SetActive(false);
        overflowGridHolder.gameObject.SetActive(false);
    }

    private void ActivateCell(ResourceType type, bool instant = false, bool pricing = false)
    {
        int loc;

        if (isCity)
        {
            if (city)
                loc = city.resourceGridDict[type];
            else if (wonder)
                loc = wonder.ResourceGridDict[type];
            else
                loc = tradeCenter.ResourceBuyGridDict[type];
        }
        else
        {
            if (unit.trader)
                loc = unit.trader.resourceGridDict[type];
            else
                loc = unit.worker.resourceGridDict[type];
        }

        gridCellDict[loc].gameObject.SetActive(true);
        activeCells++;

        gridCellDict[loc].resource.resourceType = type;

        if (isCity)
        {
            if (city)
                gridCellDict[loc].resource.SetValue(city.ResourceManager.ResourceDict[type]);
            else if (wonder)
                gridCellDict[loc].resource.SetValue(wonder.ResourceDict[type]);
            else
                gridCellDict[loc].resource.SetText(" ", tradeCenter.ResourceBuyDict[type]);
        }
        else
        {
            if (unit.trader)
                gridCellDict[loc].resource.SetValue(unit.trader.personalResourceManager.ResourceDict[type]);
            else
				gridCellDict[loc].resource.SetValue(unit.worker.personalResourceManager.ResourceDict[type]);
		}

        gridCellDict[loc].resource.resourceImage.sprite = ResourceHolder.Instance.GetIcon(type);
        resourceUIDict[type] = loc;
        gridCellDict[loc].resource.Activate(instant);

        if (inUse && activeCells % gridWidth == 1 && activeCells > 1)
        {
            if (activeCells == gridWidth + 1)
                ToggleOverflowVisibility(true, true);

            if (!isCity)
            {
                int increment = pricing ? 110 : 90;
                allContents.anchoredPosition3D += new Vector3(0, increment, 0);
                overflowGridAll.anchoredPosition3D += new Vector3(0, increment, 0);
            }
        }

        if (pricing)
        {
            if (isCity)
            {
                int price = Mathf.CeilToInt(tradeCenter.ResourceBuyDict[type] * tradeCenter.multiple);
                if (isCity && price > maxBuyCost)
                    maxBuyCost = price;

                Color color = world.CheckWorldGold(price) ? Color.white : Color.red;
                gridCellDict[loc].resource.SetPriceText(price);
                gridCellDict[loc].resource.SetPriceColor(color);
            }
            else
            {
                if (tradeCenter.ResourceSellDict.ContainsKey(type))
                    gridCellDict[loc].resource.SetPriceText(tradeCenter.ResourceSellDict[type]);
            }
        }

    }

    public void HandleButtonClick()
    {
        if (inUse)
            OnIconButtonClick?.Invoke(resourceType);
    }

    public void SetTitleInfo(string name, int level, int limit)
    {
        unitStorageLevel = level;
        unitStorageLimit = limit;
        progressBarMask.fillAmount = (float)unitStorageLevel / unitStorageLimit;

        SetTitle(name);

        if (limit == 0)
        {
            storageLevel.text = level.ToString();
            storageLimit.text = limit.ToString();
            //unitStoragePercent.text = "0%";
        }
        else
        {
            storageLevel.text = SetStringValue(level);
            storageLimit.text = SetStringValue(limit);
            //unitStoragePercent.text = $"{Mathf.RoundToInt((level / limit) * 100)}%";
        }
    }

    public void SetTitle(string name)
    {
		unitNameTitle.text = $"{name} Storage";

		int diff = name.Length - 3;
		if (diff > 0)
			titleBar.sizeDelta = new Vector2(diff * 20 + 220, 60);
		else
			titleBar.sizeDelta = new Vector2(220, 60);
	}

	private string SetStringValue(float amount)
	{
		string amountStr = "-";

		if (amount < 10000)
		{
            amountStr = $"{amount:n0}";
		}
		else if (amount < 1000000)
		{
			amountStr = Math.Round(amount * 0.001f, 1) + " k";
		}
		else if (amount < 1000000000)
		{
			amountStr = Math.Round(amount * 0.000001f, 1) + " M";
		}

		return amountStr;
	}

	public void UpdateStorageLevel(int level)
    {
        if (unitStorageLevel == 0) //progress bar gives value of null w/o this
            progressBarMask.fillAmount = 0;

        unitStorageLevel = level;

        if (unitStorageLimit == 0)
        {
            storageLevel.text = level.ToString();
            storageLimit.text = unitStorageLimit.ToString();
        }
        else
        {
            storageLevel.text = SetStringValue(level);
            //unitStoragePercent.text = $"{Mathf.RoundToInt((level / unitStorageLimit) * 100)}%";
        }

        LeanTween.value(progressBarMask.gameObject, progressBarMask.fillAmount, (float)unitStorageLevel / unitStorageLimit, 1)
            .setEase(LeanTweenType.easeOutSine)
            .setOnUpdate((value) =>
            {
                progressBarMask.fillAmount = value;
            });
    }

    public void PrepareResource(ResourceType resourceType)
    {
        this.resourceType = resourceType;
    }

    private void ToggleButtonInteractable(bool v)
    {
        foreach (int loc in gridCellDict.Keys)
        {
            gridCellDict[loc].resource.SetButtonInteractable(v);
        }
    }

    public void SetPosition(bool atTradeCenter = false, TradeCenter tradeCenter = null)
    {
        if (isCity)
        {
            ToggleVisibility(true);
            allContents.anchoredPosition3D = originalLoc + new Vector3(0, -200f, 0);
            if (atTradeCenter)
                overflowGridAll.anchoredPosition3D = originalLoc + new Vector3(0, -360f, 0);
            else
                overflowGridAll.anchoredPosition3D = originalLoc + new Vector3(0, -340f, 0);

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 260f, 0.4f).setEase(LeanTweenType.easeOutSine);
            LeanTween.moveY(overflowGridAll, overflowGridAll.anchoredPosition3D.y + 260f, 0.4f).setEase(LeanTweenType.easeOutSine);
        }
        else
        {
            allContents.anchoredPosition3D = originalLoc;
            overflowGridAll.anchoredPosition3D = overflowAllOriginalLoc;
            float unloadLoadShift = allContents.transform.localPosition.y - loadUnloadPosition.y; //how much to decrease 
            int increment = atTradeCenter ? 110 : 90;
            unloadLoadShift -= activeCells == 0 ? 0 : (Mathf.CeilToInt(activeCells / (float)gridWidth) - 1) * increment;

            if (atTradeCenter)
            {
                if (unit.trader)
                {
                    foreach (ResourceType type in tradeCenter.ResourceSellDict.Keys)
                    {
                        if (unit.trader.resourceGridDict.ContainsKey(type))
                            gridCellDict[unit.trader.resourceGridDict[type]].resource.SetPriceText(tradeCenter.ResourceSellDict[type]);
                    }
                }
                else
                {
					foreach (ResourceType type in tradeCenter.ResourceSellDict.Keys)
					{
						if (unit.worker.resourceGridDict.ContainsKey(type))
							gridCellDict[unit.worker.resourceGridDict[type]].resource.SetPriceText(tradeCenter.ResourceSellDict[type]);
					}
				}

                this.tradeCenter = tradeCenter;
                this.atTradeCenter = atTradeCenter;
                gridGrid.cellSize = new Vector2(90, 110);
                gridGrid.padding.top = -10;
                gridGrid.padding.bottom = 10;
                overflowGrid.cellSize = new Vector2(90, 110);
                overflowGrid.padding.top = -10;
                overflowGrid.padding.bottom = 10;
                overflowGridAll.anchoredPosition3D = new Vector3(0, -160, 0);
            }

            buttonDown.gameObject.SetActive(false);
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - unloadLoadShift, 0.4f).setEase(LeanTweenType.easeOutSine);
            LeanTween.moveY(overflowGridAll, overflowGridAll.anchoredPosition3D.y - unloadLoadShift, 0.4f).setEase(LeanTweenType.easeOutSine);
            ToggleButtonInteractable(true);
        }
        if (activeCells > gridWidth)
            ToggleOverflowVisibility(true, true);
        inUse = true;
    }

    public void RestorePosition(bool keepSelection)
    {

        if (isCity)
        {
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - 800f, 0.4f)
                .setEase(LeanTweenType.easeOutSine)
                .setOnComplete(SetVisibilityFalse);
            LeanTween.moveY(overflowGridAll, overflowGridAll.anchoredPosition3D.y - 800f, 0.4f)
                .setEase(LeanTweenType.easeOutSine)
                .setOnComplete(SetVisibilityFalse);
            //LeanTween.alpha(allContents, 0f, 0.4f).setEaseLinear();
        }
        else
        {
            ToggleButtonInteractable(false);
            float loc, loc2;
            if (keepSelection)
            {
                loc = originalLoc.y;
                loc2 = overflowAllOriginalLoc.y;
            }
            else
            {
                int yShift = 200;
                loc = originalLoc.y + yShift;
                loc2 = overflowAllOriginalLoc.y + yShift;
            }

            if (atTradeCenter)
            {
                foreach (ResourceType type in tradeCenter.ResourceBuyGridDict.Keys)
                {
                    int typeLoc = tradeCenter.ResourceBuyGridDict[type];
                    gridCellDict[typeLoc].resource.HidePricing();
                }

                atTradeCenter = false;
                tradeCenter = null;
                gridGrid.cellSize = new Vector2(90, 90);
                gridGrid.padding.top = 0;
                gridGrid.padding.bottom = 0;
                overflowGrid.cellSize = new Vector2(90, 90);
                overflowGrid.padding.top = 0;
                overflowGrid.padding.bottom = 0;
            }

            LeanTween.moveY(allContents, loc, 0.4f).setEase(LeanTweenType.easeOutSine).setOnComplete(ShowButtonDown);
            LeanTween.moveY(overflowGridAll, loc2, 0.4f).setEase(LeanTweenType.easeOutSine).setOnComplete(ShowButtonDown);
        }

        inUse = false;
    }

    private void ShowButtonDown()
    {
        if (activeCells > gridWidth)
            buttonDown.gameObject.SetActive(true);
    }

    private void SetActiveStatusFalse()
    {
        storageLevel.gameObject.SetActive(true);
        storageLimit.gameObject.SetActive(true);
        slash.gameObject.SetActive(true);
        //unitStoragePercent.gameObject.SetActive(true);
        progressBar.SetActive(true);
        gameObject.SetActive(false);
    }

    private void SetVisibilityFalse()
    {
        ToggleVisibility(false);
		world.overflowGridCanvas.gameObject.SetActive(false);
	}

    public void UpdateResourceInteractable(ResourceType type, int val, bool positive) //Set the resources to a value
    {
        if (resourceUIDict.ContainsKey(type))//checking if resource is in dictionary
        {
            gridCellDict[resourceUIDict[type]].resource.UpdateValue(val,positive);
        }
        else
        {
            ActivateCell(type, false, atTradeCenter);
            if (activeCells > gridWidth)
            {
                if (!inUse) 
                    buttonDown.gameObject.SetActive(true);
            }
            else
            {
                gridHolder.sizeDelta = new Vector2(activeCells * 90, 90);
            }
        }
    }

    public void FlashResource(ResourceType type)
    {
        gridCellDict[resourceUIDict[type]].resource.FlashResource();
    }

    public void UpdateResource(ResourceType type, int val) //Set the resources to a value
    {
        if (resourceUIDict.ContainsKey(type))//checking if resource is in dictionary
        {
            gridCellDict[resourceUIDict[type]].resource.SetValue(val);
        }
        else
        {
            ActivateCell(type);
            if (activeCells > gridWidth)
                buttonDown.gameObject.SetActive(true);
            else
            {
                gridHolder.sizeDelta = new Vector2(activeCells * 90, 90);
            }
        }
    }

    public void HideInventoryLevel()
    {
        storageLevel.gameObject.SetActive(false);
        storageLimit.gameObject.SetActive(false);
        slash.gameObject.SetActive(false);
        //unitStoragePercent.gameObject.SetActive(false);
        progressBar.SetActive(false);
    }

    public void MoveResources(int oldNum, int newNum, ResourceType type)
    {
        //shifting all the other resources
        if (unit.trader)
        {
            if (oldNum > newNum)
            {
                for (int i = oldNum; i > newNum; i--)
                {
                    int next = i - 1;
                    gridCellDict[next].MoveResource(gridCellDict[i], true);
                    unit.trader.resourceGridDict[gridCellDict[i].resource.resourceType] = i;
                    resourceUIDict[gridCellDict[i].resource.resourceType] = i;
                }
            }
            else
            {
                for (int i = oldNum; i < newNum; i++)
                {
                    int next = i + 1;
                    gridCellDict[next].MoveResource(gridCellDict[i], false);
                    unit.trader.resourceGridDict[gridCellDict[i].resource.resourceType] = i;
                    resourceUIDict[gridCellDict[i].resource.resourceType] = i;
                }
            }

            unit.trader.resourceGridDict[type] = newNum;
        }
        else
        {
			if (oldNum > newNum)
			{
				for (int i = oldNum; i > newNum; i--)
				{
					int next = i - 1;
					gridCellDict[next].MoveResource(gridCellDict[i], true);
					unit.worker.resourceGridDict[gridCellDict[i].resource.resourceType] = i;
					resourceUIDict[gridCellDict[i].resource.resourceType] = i;
				}
			}
			else
			{
				for (int i = oldNum; i < newNum; i++)
				{
					int next = i + 1;
					gridCellDict[next].MoveResource(gridCellDict[i], false);
					unit.worker.resourceGridDict[gridCellDict[i].resource.resourceType] = i;
					resourceUIDict[gridCellDict[i].resource.resourceType] = i;
				}
			}

			unit.worker.resourceGridDict[type] = newNum;
		}

        resourceUIDict[type] = newNum;
    }

    public void UpdateGold(int prevAmount, int goldAmount, bool pos)
    {
        if (pos)
        {
            if (prevAmount > maxBuyCost)
                return;
        }
        else
        {
            if (goldAmount > maxBuyCost)
                return;
        }

        foreach (ResourceType type in tradeCenter.ResourceBuyGridDict.Keys)
            gridCellDict[tradeCenter.ResourceBuyGridDict[type]].resource.SetPriceColor(goldAmount >= Mathf.RoundToInt(tradeCenter.ResourceBuyDict[type] * tradeCenter.multiple) ? Color.white : Color.red);
    }

    public void ReturnResource(ResourceType type, int amount)
    {
        unit.worker.personalResourceManager.AddResource(type, amount);
		gridCellDict[resourceUIDict[type]].resource.SetValue(unit.worker.personalResourceManager.ResourceDict[type]);
	}
}
