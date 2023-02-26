using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITradeRouteManager : MonoBehaviour
{
    [SerializeField]
    private GameObject uiTradeStopPanel;

    [SerializeField]
    private UnitMovement unitMovement;

    private Trader selectedTrader;

    List<string> cityNames;

    [SerializeField]
    private Transform stopHolder;

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
        List<Vector3Int> cityStops = selectedTrader.GetCityStops();

        for (int i = 0; i < cityStops.Count; i++)
        {
            string cityName = world.GetStopName(cityStops[i]);
            UITradeStopHandler newStopHandler = AddStopPanel();
            newStopHandler.SetCaptionCity(cityName);
            newStopHandler.SetResourceAssignments(selectedTrader.TradeRouteManager.ResourceAssignments[i]);
            newStopHandler.SetWaitTimes(selectedTrader.TradeRouteManager.WaitTimes[i]);
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

        foreach (UITradeStopHandler stopHandler in transform.GetComponentsInChildren<UITradeStopHandler>())
        {
            stopHandler.CloseWindow();
        }

        unitMovement.TurnOnInfoScreen();
    }

    public void AddStopPanelButton() //added this as a method attached to button as it can't return anything
    {
        AddStopPanel();
    }

    private UITradeStopHandler AddStopPanel() //showing a new resource task panel
    {
        GameObject newStop = Instantiate(uiTradeStopPanel);
        newStop.SetActive(true);
        newStop.transform.SetParent(stopHolder, false);

        UITradeStopHandler newStopHandler = newStop.GetComponent<UITradeStopHandler>();
        newStopHandler.AddCityNames(cityNames);
        newStopHandler.AddResources(resources);
        //newStopHandler.SetCargoStorageLimit(selectedTrader.CargoStorageLimit);

        return newStopHandler;
    }

    public void CreateRoute()
    {
        List<string> destinations = new();
        List<List<ResourceValue>> resourceAssignments = new();
        List<int> waitTimes = new();
        UITradeStopHandler[] stopInfoArray = transform.GetComponentsInChildren<UITradeStopHandler>();

        //checking for consecutive stops
        int i = 0;
        int childCount = stopInfoArray.Length;
        bool consecFound = false;

        foreach(UITradeStopHandler stopHandler in stopInfoArray)
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
                InfoPopUpHandler.WarningMessage().Create(selectedTrader.transform.position, "No orders and no wait time max for stop");
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
