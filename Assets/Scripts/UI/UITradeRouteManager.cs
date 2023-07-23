using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITradeRouteManager : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;
    
    [SerializeField]
    public GameObject uiTradeStopHolder, uiTradeStopPanel, startingStopGO, newStopButton, confirmButton, stopRouteButton;

    [SerializeField]
    private UnitMovement unitMovement;

    [SerializeField]
    public TMP_Dropdown chosenStop;

    private Trader selectedTrader;

    List<string> cityNames;

    [SerializeField]
    private Transform stopHolder;
    [HideInInspector]
    public int stopCount, startingStop = 0, traderCargoLimit;

    [HideInInspector]
    public List<UITradeStopHandler> tradeStopHandlerList = new();
    //private Dictionary<int, UITradeRouteStopHolder> tradeStopHolderDict = new();

    //for generating resource lists
    private List<TMP_Dropdown.OptionData> resources = new();

    [SerializeField] //changing color of button when selected
    private Image buttonImage;
    private Color originalButtonColor;

    [SerializeField] //for tweening
    private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus;
    private Vector3 originalLoc;

    //for object pooling
    private Queue<UITradeStopHandler> tradeStopHandlerQueue = new();

    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;
        originalButtonColor = buttonImage.color;
        AddResources();
        GrowTradeStopPool();
        gameObject.SetActive(false);
    }

    public void StopRoute()
    {
        unitMovement.CancelTradeRoute();
        ResetTradeRouteInfo(selectedTrader.tradeRouteManager);

        chosenStop.enabled = true;
        newStopButton.SetActive(true);
        confirmButton.SetActive(true);
        stopRouteButton.SetActive(false);
    }

    public void PrepTradeRoute()
    {
        for (int i = 0; i < tradeStopHandlerList.Count; i++)
        {
            PrepStop(tradeStopHandlerList[i]);
            tradeStopHandlerList[i].PrepResources();

            if (i < startingStop)
                tradeStopHandlerList[i].SetAsComplete();
            else if (i == startingStop)
                tradeStopHandlerList[i].SetAsCurrent();
        }

        chosenStop.enabled = false;
        newStopButton.SetActive(false);
        confirmButton.SetActive(false);
        stopRouteButton.SetActive(true);
    }

    private void ResetTradeRouteInfo(TradeRouteManager tradeRouteManager)
    {
        for (int i = 0; i < tradeStopHandlerList.Count; i++)
        {
            tradeStopHandlerList[i].addResourceButton.SetActive(true);
            tradeStopHandlerList[i].arrowUpButton.SetActive(true);
            tradeStopHandlerList[i].arrowDownButton.SetActive(true);
            tradeStopHandlerList[i].waitForeverToggle.interactable = true;
            tradeStopHandlerList[i].inputWaitTime.enabled = true;
            tradeStopHandlerList[i].cityNameList.enabled = true;
            tradeStopHandlerList[i].ResetResources();
            tradeStopHandlerList[i].progressBarHolder.SetActive(false);

            if (i <= tradeRouteManager.currentStop)
                tradeStopHandlerList[i].SetAsNext();
        }
    }

    public void SetChosenStop(int value)
    {
        startingStop = value;
    }

    public void LoadTraderRouteInfo(Trader selectedTrader, TradeRouteManager tradeRouteManager, MapWorld world)
    {
        startingStop = tradeRouteManager.startingStop;
        List<Vector3Int> cityStops = new(tradeRouteManager.cityStops);
        int currentStop = tradeRouteManager.currentStop;
        if (cityStops.Count == 0)
            startingStopGO.SetActive(false);

        for (int i = 0; i < cityStops.Count; i++)
        {
            string cityName = world.GetStopName(cityStops[i]);

            if (cityName == "")
            {
                tradeRouteManager.RemoveStop(cityStops[i]);
                continue;
            }

            UITradeStopHandler newStopHandler = AddStopPanel(selectedTrader.followingRoute);
            if (newStopHandler != null)
            {
                newStopHandler.SetCaptionCity(cityName);
                newStopHandler.SetResourceAssignments(tradeRouteManager.resourceAssignments[i], selectedTrader.followingRoute);
                newStopHandler.SetWaitTimes(tradeRouteManager.waitTimes[i]);

                if (selectedTrader.followingRoute)
                {
                    if (i < currentStop)
                    {
                        newStopHandler.SetAsComplete();
                    }
                    else if (i == currentStop)
                    {
                        if (selectedTrader.atStop)
                        {
                            newStopHandler.progressBarHolder.SetActive(true);
                            newStopHandler.SetProgressBarMask(tradeRouteManager.timeWaited, tradeRouteManager.waitTime);
                            newStopHandler.SetTime(tradeRouteManager.timeWaited, tradeRouteManager.waitTime, newStopHandler.waitForever);
                        }

                        newStopHandler.SetAsCurrent(tradeRouteManager.currentResource, tradeRouteManager.resourceCurrentAmount, tradeRouteManager.resourceTotalAmount);
                    }
                    else
                    {
                        newStopHandler.SetAsNext();
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

    private void AddResources()
    {
        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
        {
            resources.Add(new TMP_Dropdown.OptionData(resource.resourceName, resource.resourceIcon));
        }
    }

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

            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + 600f, 0.3f).setEaseOutSine();
            //LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear();
        }
        else
        {
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
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
        world.tradeRouteManagerCanvas.gameObject.SetActive(false);
    }

    public void CloseMenu()
    {
        ToggleVisibility(false);
    }

    public void PrepareTradeRouteMenu(List<string> cityNames, Trader selectedTrader)
    {
        this.selectedTrader = selectedTrader;
        traderCargoLimit = selectedTrader.cargoStorageLimit;
        this.selectedTrader.tradeRouteManager.SetTradeRouteManager(this);
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

        //GameObject newHolder = Instantiate(uiTradeStopHolder);
        //newHolder.transform.SetParent(stopHolder);
        //UITradeRouteStopHolder newStopHolder = newHolder.GetComponent<UITradeRouteStopHolder>();
        //tradeStopHolderDict[stopCount] = newStopHolder;
        //newStopHolder.loc = stopCount;

        //GameObject newStop = Instantiate(uiTradeStopPanel);
        //UITradeStopHandler newStopHandler = newStop.GetComponent<UITradeStopHandler>();
        UITradeStopHandler newStopHandler = GetFromTradeStopPool();
        newStopHandler.gameObject.transform.SetParent(stopHolder, false);
        //newStopHolder.stopHandler = newStopHandler;
        //newStopHandler.SetStopHolder(newStopHolder);
        //newStopHandler.loc = stopCount;
        newStopHandler.ChangeCounter(stopCount + 1); //counter.text = (stopCount + 1).ToString();
        //newStopHandler.SetTradeRouteManager(this);
        newStopHandler.AddCityNames(cityNames);
        newStopHandler.AddResources(resources);
        tradeStopHandlerList.Add(newStopHandler);
        //newStopHandler.SetCargoStorageLimit(selectedTrader.CargoStorageLimit);
        stopCount++;
        chosenStop.options.Add(new TMP_Dropdown.OptionData(stopCount.ToString()));
        if (stopCount == 1)
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
        
        return newStopHandler;
    }

    private void PrepStop(UITradeStopHandler stop)
    {
        stop.addResourceButton.SetActive(false);
        stop.arrowUpButton.SetActive(false);
        stop.arrowDownButton.SetActive(false);
        stop.waitForeverToggle.interactable = false;
        stop.inputWaitTime.enabled = false;
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
            if (destination == null)
            {
                UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "No assigned city to stop");
                return;
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
                UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Consecutive stops for same stop");
                return;
            }

            if (resourceAssignment.Count == 0 && waitTime < 0)
            {
                UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "No resource assignment for stop");
                return;
            }
        }

        selectedTrader.SetTradeRoute(startingStop, destinations, resourceAssignments, waitTimes, unitMovement.uiPersonalResourceInfoPanel);

        //if (selectedTrader.followingRoute)
        //{
        //    unitMovement.CancelTradeRoute();
        //}

        if (destinations.Count > 0)
        {
            unitMovement.UninterruptedRoute();
            unitMovement.uiTraderPanel.uiBeginTradeRoute.ToggleInteractable(true);
        }
        else
        {
            unitMovement.uiTraderPanel.uiBeginTradeRoute.ToggleInteractable(false);
        }

        ToggleVisibility(false);
    }


    //Object pooling stops
    private void GrowTradeStopPool()
    {
        for (int i = 0; i < 5; i++) //grow pool 5 at a time
        {
            GameObject newStop = Instantiate(uiTradeStopPanel);
            //newStop.transform.SetParent(transform, false);
            UITradeStopHandler newStopHandler = newStop.GetComponent<UITradeStopHandler>();
            newStopHandler.SetTradeRouteManager(this);
            AddToTradeStopPool(newStopHandler);
        }
    }

    public void AddToTradeStopPool(UITradeStopHandler newStopHandler)
    {
        newStopHandler.gameObject.transform.SetParent(transform, false);
        newStopHandler.gameObject.SetActive(false);
        tradeStopHandlerQueue.Enqueue(newStopHandler);
    }

    private UITradeStopHandler GetFromTradeStopPool()
    {
        if (tradeStopHandlerQueue.Count == 0)
            GrowTradeStopPool();

        UITradeStopHandler newStopHandler = tradeStopHandlerQueue.Dequeue();
        newStopHandler.gameObject.SetActive(true);
        return newStopHandler;
    }

    public void ReturnTradeStop(UITradeStopHandler stop)
    {
        //foreach (UITradeStopHandler stop in tradeStopHandlerList)
        //{
        
        AddToTradeStopPool(stop);
        //}

        //tradeStopHandlerList.Clear();
    }
}
