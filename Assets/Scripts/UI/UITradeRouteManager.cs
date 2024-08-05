using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITradeRouteManager : MonoBehaviour
{
    [SerializeField]
    public MapWorld world;
    
    [SerializeField]
    public GameObject uiTradeStopHolder, uiTradeStopPanel, startingStopGO, newStopButton, confirmButton, stopRouteButton, waitingForText, costHolder;

    [SerializeField]
    public UITradeResourceNum uiTradeResourceNum;

    [SerializeField]
    private UnitMovement unitMovement;

    [SerializeField]
    public TMP_Dropdown chosenStop;

    private Trader selectedTrader;
    //[HideInInspector]
    //public Canvas rootCanvas;

    [SerializeField]
    public UIResourceSelectionGrid resourceSelectionGrid; 

    List<string> cityNames;

    [SerializeField]
    private Transform stopHolder, costRect;
	[HideInInspector]
    public int stopCount, startingStop = 0, traderCargoLimit;
	private List<UIResourceInfoPanel> costsInfo = new();

	[HideInInspector]
    public List<UITradeStopHandler> tradeStopHandlerList = new();
    //private Dictionary<int, UITradeRouteStopHolder> tradeStopHolderDict = new();

    //for generating resource lists
    //private List<TMP_Dropdown.OptionData> resources = new();

    [SerializeField] //changing color of button when selected
    private Image buttonImage;
    private Color originalButtonColor;

    [SerializeField] //for tweening
    public RectTransform allContents, stopScroller;
    [HideInInspector]
    public bool activeStatus;
    [HideInInspector]
    public Vector3 originalLoc;
    private Vector2 originalSize, originalPos;

    //for object pooling
    //private Queue<UITradeStopHandler> tradeStopHandlerQueue = new();

    private void Awake()
    {
        //rootCanvas = GetComponentInParent<Canvas>();
        originalLoc = allContents.anchoredPosition3D;
        originalButtonColor = buttonImage.color;
        gameObject.SetActive(false);

		foreach (Transform selection in costRect)
		{
			if (selection.TryGetComponent(out UIResourceInfoPanel panel))
			{
				costsInfo.Add(panel);
			}
		}

        originalSize = stopScroller.sizeDelta;
        originalPos = stopScroller.transform.localPosition;
	}

    public void HandleSpace()
    {
        if (activeStatus && !uiTradeResourceNum.activeStatus)
            CreateRoute();
    }



	public void StopRoute()
    {
        world.cityBuilderManager.PlaySelectAudio();
        unitMovement.CancelTradeRoute();
    }

    public void ResetButtons()
    {
        chosenStop.enabled = true;
        newStopButton.SetActive(true);
        confirmButton.SetActive(true);
        stopRouteButton.SetActive(false);

		startingStopGO.SetActive(true);
		costHolder.SetActive(false);
		waitingForText.SetActive(false);
		stopScroller.sizeDelta = originalSize;
		stopScroller.transform.localPosition = originalPos;
	}

    public void ShowRouteCostFlag(bool show, Trader trader)
    {
        if (activeStatus && selectedTrader == trader)
        {
            waitingForText.SetActive(show);
            //SetCostsInfoColor(show);
        }
    }

    public void ResetTradeRouteInfo(TradeRouteManager tradeRouteManager)
    {
        for (int i = 0; i < tradeStopHandlerList.Count; i++)
        {
            tradeStopHandlerList[i].addResourceButton.SetActive(true);
            tradeStopHandlerList[i].arrowUpButton.SetActive(true);
            tradeStopHandlerList[i].closeButton.SetActive(true);
            tradeStopHandlerList[i].arrowDownButton.SetActive(true);
            tradeStopHandlerList[i].waitForeverToggle.gameObject.SetActive(true);
            tradeStopHandlerList[i].waitForeverToggle.interactable = true;
            
            //tradeStopHandlerList[i].inputWaitTime.enabled = true;

            if (!tradeStopHandlerList[i].waitForever)
            {
                tradeStopHandlerList[i].waitSlider.gameObject.SetActive(true);
                tradeStopHandlerList[i].SetWaitTimeValue();
                tradeStopHandlerList[i].waitTimeText.transform.localPosition = new Vector3(550, 15, 0);
            }
    
            tradeStopHandlerList[i].cityNameList.enabled = true;
            tradeStopHandlerList[i].ResetResources();
            tradeStopHandlerList[i].progressBarHolder.SetActive(false);

            if (i <= tradeRouteManager.currentStop)
                tradeStopHandlerList[i].SetAsNext(false);
        }
    }

    public void SetChosenStop(int value)
    {
        if (!activeStatus)
            return;
        //world.cityBuilderManager.PlaySelectAudio();
        startingStop = value;
    }

    public void LoadTraderRouteInfo(Trader selectedTrader, TradeRouteManager tradeRouteManager, MapWorld world)
    {
        startingStop = tradeRouteManager.startingStop;
        List<Vector3Int> cityStops = new(tradeRouteManager.cityStops);
        int currentStop = tradeRouteManager.currentStop;
        if (cityStops.Count == 0)
            startingStopGO.SetActive(false);
        
        if (selectedTrader.followingRoute)
        {
            startingStopGO.SetActive(false);
            //stopScroller.sizeDelta += new Vector2(0, -100);
			Vector2 currentPos = originalPos;
			currentPos.y -= 100;
			stopScroller.transform.localPosition = currentPos;

			ShowRouteCosts(selectedTrader.totalRouteCosts);	
			costHolder.SetActive(true);
            if (selectedTrader.waitingOnRouteCosts)
                waitingForText.SetActive(true);
            else
				waitingForText.SetActive(false);
		}
        else
        {
            costHolder.SetActive(false);
			waitingForText.SetActive(false);
            stopScroller.sizeDelta = originalSize;
            stopScroller.transform.localPosition = originalPos;
		}

		for (int i = 0; i < cityStops.Count; i++)
        {
            string cityName = world.GetStopName(cityStops[i]);

            //if (cityName == "")
            //{
            //    tradeRouteManager.RemoveStop(cityStops[i]);
            //    continue;
            //}

            UITradeStopHandler newStopHandler = AddStopPanel(selectedTrader.followingRoute);
            if (newStopHandler != null)
            {
                if (cityName != "")
                    newStopHandler.SetCaptionCity(cityName);
                newStopHandler.SetResourceAssignments(tradeRouteManager.resourceAssignments[i], selectedTrader.followingRoute);
                newStopHandler.SetWaitTimes(tradeRouteManager.waitTimes[i], selectedTrader.followingRoute);

                if (selectedTrader.followingRoute)
                {
                    if (i < currentStop)
                    {
                        newStopHandler.SetAsComplete(true, tradeRouteManager.resourceCompletion[i]);
                    }
                    else if (i == currentStop)
                    {
                        if (selectedTrader.atStop)
                        {
                            newStopHandler.progressBarHolder.SetActive(true);
                            newStopHandler.SetProgressBarValue(tradeRouteManager.waitTime);
                            newStopHandler.SetProgressBarMask(tradeRouteManager.waitTime - tradeRouteManager.timeWaited, newStopHandler.waitForever);

                            if (newStopHandler.waitForever)
                                newStopHandler.SetTime(tradeRouteManager.timeWaited, newStopHandler.waitForever);
                            else
                                newStopHandler.SetTime(tradeRouteManager.waitTime - tradeRouteManager.timeWaited, newStopHandler.waitForever);
                        }

                        newStopHandler.SetAsCurrent(tradeRouteManager.resourceCompletion[i], tradeRouteManager.currentResource, tradeRouteManager.resourceCurrentAmount, tradeRouteManager.resourceTotalAmount);
                    }
                    else
                    {
                        newStopHandler.SetAsNext(true, tradeRouteManager.resourceCompletion[i]);
                    }
                }
                else
                {
                    newStopHandler.background.sprite = newStopHandler.nextStopSprite;
                    //newStopHandler.resourceButton.sprite = newStopHandler.nextResource;
                }
            }
        }

        chosenStop.value = startingStop;
        if (selectedTrader.followingRoute)
        {
            chosenStop.enabled = false;
            newStopButton.SetActive(false);
            confirmButton.SetActive(false);
            stopRouteButton.SetActive(true);
        }
        else
        {
            chosenStop.enabled = true;
            newStopButton.SetActive(true);
            confirmButton.SetActive(true);
            stopRouteButton.SetActive(false);
        }
    }

    private void ShowRouteCosts(List<ResourceValue> resourceList)
    {
		for (int i = 0; i < costsInfo.Count; i++)
		{
			if (i >= resourceList.Count)
			{
				costsInfo[i].gameObject.SetActive(false);
			}
			else
			{
				costsInfo[i].gameObject.SetActive(true);
                costsInfo[i].SetResourceAmount(resourceList[i].resourceAmount);
				costsInfo[i].SetResourceType(resourceList[i].resourceType);
				costsInfo[i].resourceImage.sprite = ResourceHolder.Instance.GetIcon(resourceList[i].resourceType);
			}
		}
	}

    public void SetCostsInfoColor(bool v, List<ResourceValue> resourceList = null)
    {
        if (v)
        {
            for (int i = 0; i < costsInfo.Count; i++)
            {
                if (costsInfo[i].gameObject.activeSelf)
                {
                    for (int j = 0; j < resourceList.Count; j++)
                    {
                        //if (resourceList[j].resourceType == costsInfo[i].resourceType && resourceList[j].resourceAmount >= costsInfo[i].resourceAmountText.)
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < costsInfo.Count; i++)
            {
                if (costsInfo[i].gameObject.activeSelf)
                    costsInfo[i].resourceAmountText.color = Color.white;
            }
        }
    }

	public void SetChosenStopLive(int value)
    {
        startingStop = value;
        chosenStop.value = startingStop;
    }
    //private void AddResources()
    //{
    //    foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
    //    {
    //        resources.Add(new TMP_Dropdown.OptionData(resource.resourceName, resource.resourceIcon));
    //    }
    //}

    public void ToggleVisibility(bool v) 
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(v);
            world.tradeRouteManagerCanvas.gameObject.SetActive(true);
            chosenStop.options.Clear();
            //chosenStop.RefreshShownValue();

            activeStatus = true;

            allContents.anchoredPosition3D = originalLoc + new Vector3(-600f, 0, 0);

            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + 600f, 0.3f).setEaseOutSine().setOnComplete(TutorialCheck);
            //LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear();
        }
        else
        {
            if (resourceSelectionGrid.activeStatus)
                resourceSelectionGrid.ToggleVisibility(false);

            if (uiTradeResourceNum.activeStatus)
                uiTradeResourceNum.ToggleVisibility(false);

            selectedTrader = null;
            ToggleButtonColor(false);

            activeStatus = false;
            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + -600f, 0.3f).setOnComplete(SetActiveStatusFalse);
            List<UITradeStopHandler> stopList = new(tradeStopHandlerList);

            foreach (UITradeStopHandler stopHandler in stopList)
            {
                stopHandler.CloseWindow(false);
            }

            tradeStopHandlerList.Clear();
            unitMovement.TurnOnInfoScreen();

            if (world.tutorial)
            {
                if (world.cityBuilderManager.uiHelperWindow.activeStatus)
                    world.cityBuilderManager.uiHelperWindow.ToggleVisibility(false);
            }
        }
    }

    private void TutorialCheck()
    {
		if (world.tutorial && GameLoader.Instance.gameData.tutorialData.builtTrader && !GameLoader.Instance.gameData.tutorialData.openedTRM)
		{
			world.ButtonFlashCheck();
			world.cityBuilderManager.uiHelperWindow.ToggleVisibility(true, 0);
			world.cityBuilderManager.uiHelperWindow.SetMessage("Select here to create a new stop for the trader. The first stop must be a city to pay for the route.");
			world.cityBuilderManager.uiHelperWindow.SetPlacement(originalLoc + new Vector3(50, 700, 0), allContents.pivot);
			GameLoader.Instance.gameData.tutorialData.openedTRM = true;
		}
	}

	private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
        world.tradeRouteManagerCanvas.gameObject.SetActive(false);
    }

    public void CloseMenu()
    {
        if (!activeStatus)
            return;
        
        world.cityBuilderManager.PlayCloseAudio();
        ToggleVisibility(false);
    }

    public void PrepareTradeRouteMenu(List<string> cityNames, Trader selectedTrader)
    {
        this.selectedTrader = selectedTrader;
        traderCargoLimit = selectedTrader.personalResourceManager.resourceStorageLimit;
        //this.selectedTrader.tradeRouteManager.SetTradeRouteManager(this);
        this.cityNames = cityNames;
    }

    public void ToggleButtonColor(bool v)
    {
        if (v)
            buttonImage.color = Color.green;
        else
            buttonImage.color = originalButtonColor;
    }

    public void AddStopPanelButton() //added this as a method attached to button as it can't return anything
    {
        if (!activeStatus)
            return;
        
        world.cityBuilderManager.PlaySelectAudio();
        AddStopPanel(false);
    }

    private UITradeStopHandler AddStopPanel(bool onRoute) //showing a new resource task panel
    {
        if (stopCount >= 20)
        {
            //Vector3 mousePos = Input.mousePosition;
            //mousePos.z = 935;
            //Vector3 mouseLoc = Camera.main.ScreenToWorldPoint(mousePos);
            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Hit stop limit");

            return null;
        }

        //for tweening (doesn't currently work as stop sizes aren't consistent)
        //GameObject newHolder = Instantiate(uiTradeStopHolder);
        //newHolder.transform.SetParent(stopHolder);
        //UITradeRouteStopHolder newStopHolder = newHolder.GetComponent<UITradeRouteStopHolder>();
        //tradeStopHolderDict[stopCount] = newStopHolder;
        //newStopHolder.loc = stopCount;
        //for tweening

        GameObject newStop = Instantiate(uiTradeStopPanel);
        UITradeStopHandler newStopHandler = newStop.GetComponent<UITradeStopHandler>();
        newStopHandler.gameObject.transform.SetParent(stopHolder, false);

        //object pooling
        //UITradeStopHandler newStopHandler = GetFromTradeStopPool();
        //newStopHandler.gameObject.transform.SetParent(stopHolder, false);
        //object pooling

        //for tweening
        //newStopHolder.stopHandler = newStopHandler;
        //newStopHandler.SetStopHolder(newStopHolder);
        //newStopHandler.loc = stopCount;
        //for tweening

        newStopHandler.ChangeCounter(stopCount + 1); //counter.text = (stopCount + 1).ToString();
        newStopHandler.SetTradeRouteManager(this);
        newStopHandler.AddCityNames(cityNames);
        //newStopHandler.AddResources(resources);
        tradeStopHandlerList.Add(newStopHandler);
        //newStopHandler.SetCargoStorageLimit(selectedTrader.CargoStorageLimit);
        stopCount++;
        chosenStop.options.Add(new TMP_Dropdown.OptionData(stopCount.ToString()));
        if (stopCount == 1 && !selectedTrader.followingRoute)
        {
            startingStopGO.SetActive(true);
            //chosenStop.options.Remove(0); //removing top choice;
        }

        //newStop.transform.localPosition = Vector3.zero;
        //newHolder.transform.localPosition = Vector3.zero;
        //newHolder.transform.localScale = Vector3.one;
        //newHolder.transform.localEulerAngles = Vector3.zero;
        if (onRoute)
            PrepStop(newStopHandler);

		if (world.tutorial && GameLoader.Instance.gameData.tutorialData.openedTRM && !GameLoader.Instance.gameData.tutorialData.addedStop)
		{
			world.cityBuilderManager.uiHelperWindow.ToggleVisibility(true, 0);
			world.cityBuilderManager.uiHelperWindow.SetMessage("Select here to specify the stop location. \"+ Resource\" allows resource assignments to be given to traders.");
			world.cityBuilderManager.uiHelperWindow.SetPlacement(originalLoc + new Vector3(100, 600, 0), allContents.pivot);

			GameLoader.Instance.gameData.tutorialData.addedStop = true;
		}

		return newStopHandler;
    }

    private void PrepStop(UITradeStopHandler stop)
    {
        stop.addResourceButton.SetActive(false);
        stop.arrowUpButton.SetActive(false);
        stop.arrowDownButton.SetActive(false);
        stop.closeButton.SetActive(false);
        stop.waitForeverToggle.interactable = false;
        stop.cityNameList.enabled = false;
    }

    public void MoveStop(int loc, bool up)
    {
        UITradeStopHandler currentStop = up ? tradeStopHandlerList[loc] : tradeStopHandlerList[loc - 2];
        tradeStopHandlerList.Remove(currentStop);
        tradeStopHandlerList.Insert(loc - 1, currentStop);

        if (up)
        {
            tradeStopHandlerList[loc].ChangeCounter(loc + 1);
            //tradeStopHolderDict[loc - 1].MoveStop(tradeStopHolderDict[loc]);
        }
        else
        {
            tradeStopHandlerList[loc - 2].ChangeCounter(loc - 1);
            //tradeStopHolderDict[loc - 1].MoveStop(tradeStopHolderDict[loc - 2]);
        }

        //currentStop.transform.SetParent(tradeStopHolderDict[loc - 1].transform);
        //Vector3 newLoc = tradeStopHolderDict[loc - 1].transform.position;
        //tradeStopHolderDict[loc - 1].stopHandler = currentStop;
        //currentStop.loc = loc - 1;

        //LeanTween.move(currentStop.gameObject, newLoc, 0.2f).setEaseOutSine().setOnComplete(() => { SetZeroLoc(currentStop); });
        //currentStop.transform.localPosition = Vector3.zero;
    }

    //public void SetZeroLoc(UITradeStopHandler currentStop)
    //{
    //    currentStop.transform.localPosition = Vector3.zero;
    //}

    //public void DestroyHolder(int loc)
    //{
    //    Destroy(tradeStopHolderDict[loc].gameObject);
    //}

    public void UpdateStopNumbers(int removedStop)
    {
        for (int i = removedStop; i < tradeStopHandlerList.Count; i++)
        {
            tradeStopHandlerList[i].ChangeCounter(i);
        }
    }

    public void CreateRoute()
    {
        if (!activeStatus)
            return;
        world.cityBuilderManager.PlaySelectAudio();

        List<string> destinations = new();
        List<List<ResourceValue>> resourceAssignments = new();
        List<int> waitTimes = new();

        //checking for consecutive stops
        int i = 0;
        int childCount = tradeStopHandlerList.Count;
        bool consecFound = false;

        foreach(UITradeStopHandler stopHandler in tradeStopHandlerList)
        {            
            (string destination, List<ResourceValue> resourceAssignment, int waitTime) = stopHandler.GetStopInfo();

            if (destination == null || destination == "")
            {
                UIInfoPopUpHandler.WarningMessage().Create(confirmButton.transform.position, "No assigned destination to stop", false);
                return;
            }

            ITradeStop stop = world.GetTradeStopByName(destination);
            
            if (i == 0 && !stop.city)
            {
                UIInfoPopUpHandler.WarningMessage().Create(confirmButton.transform.position, "First stop must be city", false);
                return;
            }

            if (stop.wonder)
            {
                for (int j = 0; j < resourceAssignment.Count; j++)
                {
                    if (resourceAssignment[j].resourceAmount > 0)
                    {
						UIInfoPopUpHandler.WarningMessage().Create(confirmButton.transform.position, "Can't load from wonder", false);
						return;
                    }
                    else if (!stop.wonder.resourceCostDict.ContainsKey(resourceAssignment[j].resourceType))
                    {
						UIInfoPopUpHandler.WarningMessage().Create(confirmButton.transform.position, destination + " doesn't need " + resourceAssignment[j].resourceType, false);
						return;
					}
                }
            }

            if (stop.center)
            {
                for (int j = 0; j < resourceAssignment.Count; j++)
                {
                    if (resourceAssignment[j].resourceAmount < 0 && !stop.center.resourceSellDict.ContainsKey(resourceAssignment[j].resourceType))
                    {
						UIInfoPopUpHandler.WarningMessage().Create(confirmButton.transform.position, destination + " doesn't sell " + resourceAssignment[j].resourceType, false);
						return;
					}
                    else if (resourceAssignment[j].resourceAmount > 0 && !stop.center.resourceBuyDict.ContainsKey(resourceAssignment[j].resourceType))
                    {
						UIInfoPopUpHandler.WarningMessage().Create(confirmButton.transform.position, destination + " won't buy " + resourceAssignment[j].resourceType, false);
						return;
					}
                }
            }

            destinations.Add(destination);
            resourceAssignments.Add(resourceAssignment);
            waitTimes.Add(waitTime);

            if (i == childCount - 1)
                consecFound = destinations[i] == destinations[0];
            else if (i > 0)
                consecFound = destinations[i] == destinations[i - 1];

            i++;

            if (consecFound)
            {
                UIInfoPopUpHandler.WarningMessage().Create(confirmButton.transform.position, "Consecutive stops for same stop", false);
                return;
            }

            if (resourceAssignment.Count == 0 && waitTime < 0)
            {
                UIInfoPopUpHandler.WarningMessage().Create(confirmButton.transform.position, "No resource assignment for stop", false);
                return;
            }
        }

        if (destinations.Count > 0)
        {
            selectedTrader.SetTradeRoute(startingStop, destinations, resourceAssignments, waitTimes);
            unitMovement.uiTraderPanel.uiBeginTradeRoute.ToggleInteractable(true);
        }
        else
        {
            selectedTrader.ClearTradeRoute();
            unitMovement.uiTraderPanel.uiBeginTradeRoute.ToggleInteractable(false);
        }

        ToggleVisibility(false);
    }


    //Object pooling stops
    //private void GrowTradeStopPool()
    //{
    //    for (int i = 0; i < 5; i++) //grow pool 5 at a time
    //    {
    //        GameObject newStop = Instantiate(uiTradeStopPanel);
    //        //newStop.transform.SetParent(transform, false);
    //        UITradeStopHandler newStopHandler = newStop.GetComponent<UITradeStopHandler>();
    //        newStopHandler.SetTradeRouteManager(this);
    //        AddToTradeStopPool(newStopHandler);
    //    }
    //}

    //public void AddToTradeStopPool(UITradeStopHandler newStopHandler)
    //{
    //    newStopHandler.gameObject.transform.SetParent(transform, false);
    //    newStopHandler.gameObject.SetActive(false);
    //    tradeStopHandlerQueue.Enqueue(newStopHandler);
    //}

    //private UITradeStopHandler GetFromTradeStopPool()
    //{
    //    if (tradeStopHandlerQueue.Count == 0)
    //        GrowTradeStopPool();

    //    UITradeStopHandler newStopHandler = tradeStopHandlerQueue.Dequeue();
    //    newStopHandler.gameObject.SetActive(true);
    //    return newStopHandler;
    //}

    //public void ReturnTradeStop(UITradeStopHandler stop)
    //{
    //    //foreach (UITradeStopHandler stop in tradeStopHandlerList)
    //    //{
        
    //    AddToTradeStopPool(stop);
    //    //}

    //    //tradeStopHandlerList.Clear();
    //}
}
