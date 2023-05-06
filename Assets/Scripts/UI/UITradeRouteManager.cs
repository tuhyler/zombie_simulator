using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITradeRouteManager : MonoBehaviour
{
    [SerializeField]
    private GameObject uiTradeStopHolder, uiTradeStopPanel;

    [SerializeField]
    private UnitMovement unitMovement;

    private Trader selectedTrader;

    List<string> cityNames;

    [SerializeField]
    private Transform stopHolder;
    [HideInInspector]
    public int stopCount;

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


    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;
        originalButtonColor = buttonImage.color;
        AddResources();
        gameObject.SetActive(false);
    }

    public void LoadTraderRouteInfo(Trader selectedTrader, MapWorld world)
    {
        List<Vector3Int> cityStops = selectedTrader.tradeRouteManager.cityStops;
        int currentStop = selectedTrader.tradeRouteManager.currentStop;

        for (int i = 0; i < cityStops.Count; i++)
        {
            string cityName = world.GetStopName(cityStops[i]);
            UITradeStopHandler newStopHandler = AddStopPanel();
            if (newStopHandler != null)
            {
                newStopHandler.SetCaptionCity(cityName);
                newStopHandler.SetResourceAssignments(selectedTrader.tradeRouteManager.ResourceAssignments[i]);
                newStopHandler.SetWaitTimes(selectedTrader.tradeRouteManager.WaitTimes[i]);

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
                            newStopHandler.SetProgressBarMask(selectedTrader.tradeRouteManager.timeWaited, selectedTrader.tradeRouteManager.waitTime);
                            newStopHandler.SetTime(selectedTrader.tradeRouteManager.timeWaited, selectedTrader.tradeRouteManager.waitTime, newStopHandler.waitForever);
                        }

                        newStopHandler.SetAsCurrent(selectedTrader.tradeRouteManager.currentResource, selectedTrader.tradeRouteManager.resourceCurrentAmount, selectedTrader.tradeRouteManager.resourceTotalAmount);
                    }
                    else
                    {
                        newStopHandler.SetAsNext();
                    }
                }
                else
                {
                    newStopHandler.background.sprite = newStopHandler.nextStopSprite;
                    newStopHandler.resourceButton.sprite = newStopHandler.nextResource;
                }
            }
        }
    }

    public void AddResources()
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

            activeStatus = true;

            allContents.anchoredPosition3D = originalLoc + new Vector3(-600f, 0, 0);

            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + 600f, 0.3f).setEaseOutSine();
            LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear();
        }
        else
        {
            activeStatus = false;
            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + -600f, 0.3f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void PrepareTradeRouteMenu(List<string> cityNames, Trader selectedTrader)
    {
        this.selectedTrader = selectedTrader;
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

    public void CloseWindow()
    {
        selectedTrader = null;
        ToggleButtonColor(false);
        //gameObject.SetActive(false);

        activeStatus = false;
        LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + -600f, 0.3f).setOnComplete(SetActiveStatusFalse);
        List<UITradeStopHandler> stopList = new(tradeStopHandlerList);

        foreach (UITradeStopHandler stopHandler in stopList)
        {
            stopHandler.CloseWindow();
        }

        //tradeStopHolderDict.Clear();
        unitMovement.TurnOnInfoScreen();
    }

    public void AddStopPanelButton() //added this as a method attached to button as it can't return anything
    {
        AddStopPanel();
    }

    private UITradeStopHandler AddStopPanel() //showing a new resource task panel
    {
        if (stopCount >= 20)
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 10f;
            Vector3 mouseLoc = Camera.main.ScreenToWorldPoint(mousePos);
            InfoPopUpHandler.WarningMessage().Create(mouseLoc, "Hit stop limit");

            return null;
        }

        //GameObject newHolder = Instantiate(uiTradeStopHolder);
        //newHolder.transform.SetParent(stopHolder);
        //UITradeRouteStopHolder newStopHolder = newHolder.GetComponent<UITradeRouteStopHolder>();
        //tradeStopHolderDict[stopCount] = newStopHolder;
        //newStopHolder.loc = stopCount;

        GameObject newStop = Instantiate(uiTradeStopPanel);
        newStop.transform.SetParent(stopHolder, false);
        UITradeStopHandler newStopHandler = newStop.GetComponent<UITradeStopHandler>();
        //newStopHolder.stopHandler = newStopHandler;
        //newStopHandler.SetStopHolder(newStopHolder);
        //newStopHandler.loc = stopCount;
        newStopHandler.counter.text = (stopCount + 1).ToString();
        newStopHandler.SetTradeRouteManager(this);
        newStopHandler.AddCityNames(cityNames);
        newStopHandler.AddResources(resources);
        tradeStopHandlerList.Add(newStopHandler);
        //newStopHandler.SetCargoStorageLimit(selectedTrader.CargoStorageLimit);
        stopCount++;

        //newStop.transform.localPosition = Vector3.zero;
        //newHolder.transform.localPosition = Vector3.zero;
        //newHolder.transform.localScale = Vector3.one;
        //newHolder.transform.localEulerAngles = Vector3.zero;
        return newStopHandler;
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
                InfoPopUpHandler.WarningMessage().Create(selectedTrader.transform.position, "No assigned city to stop");
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
                InfoPopUpHandler.WarningMessage().Create(selectedTrader.transform.position, "Consecutive stops for same city");
                return;
            }

            if (resourceAssignment.Count == 0 && waitTime < 0)
            {
                InfoPopUpHandler.WarningMessage().Create(selectedTrader.transform.position, "No resource assignment with forever wait time for stop");
                return;
            }
        }

        selectedTrader.SetTradeRoute(destinations, resourceAssignments, waitTimes, unitMovement.GetUIPersonalResourceInfoPanel);

        if (selectedTrader.followingRoute)
        {
            unitMovement.CancelTradeRoute();
        }

        if (destinations.Count > 0)
        {
            unitMovement.UninterruptedRoute();
            unitMovement.uiTraderPanel.uiBeginTradeRoute.ToggleInteractable(true);
        }
        else
        {
            unitMovement.uiTraderPanel.uiBeginTradeRoute.ToggleInteractable(false);
        }

        CloseWindow();
    }

}
