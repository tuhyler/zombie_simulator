using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trader : Unit
{
    private PersonalResourceManager personalResourceManager;
    public PersonalResourceManager PersonalResourceManager { get { return personalResourceManager; } }

    private TradeRouteManager tradeRouteManager;
    public TradeRouteManager TradeRouteManager { get { return tradeRouteManager; } }
    
    private int cargoStorageLimit = 10;
    public int CargoStorageLimit { get { return cargoStorageLimit; } }

    [HideInInspector]
    public bool hasRoute; //for showing begin route, for cancelling/following route, and for picking/dropping load

    [SerializeField]
    public int loadUnloadRate = 1;

    private Coroutine LoadUnloadCo;
    private Coroutine WaitTimeCo;

    //private UnitMovement unitMovement;

    private void Awake()
    {
        AwakeMethods();
        isTrader = true;
    }

    protected override void AwakeMethods()
    {
        base.AwakeMethods();
        personalResourceManager = GetComponent<PersonalResourceManager>();
        personalResourceManager.ResourceStorageLimit = cargoStorageLimit;
    }

    //passing details of the trade route
    public void SetTradeRoute(List<string> cityNames, List<List<ResourceValue>> resourceAssignments, List<int> waitTimes, UIPersonalResourceInfoPanel uiPersonalResourceInfoPanel)
    {
        tradeRouteManager = GetComponent<TradeRouteManager>();
        tradeRouteManager.SetTrader(this);
        tradeRouteManager.SetPersonalResourceManager(personalResourceManager, uiPersonalResourceInfoPanel);

        List<Vector3Int> cityStops = new();

        foreach (string name in cityNames)
        {
            cityStops.Add(world.GetCityLocation(name));
        }

        if (cityStops.Count > 0)
            hasRoute = true;
        else
            hasRoute = false;

        tradeRouteManager.SetTradeRoute(cityStops, resourceAssignments, waitTimes);
    }

    public List<Vector3Int> GetCityStops()
    {
        if (tradeRouteManager == null)
        {
            List<Vector3Int> cityStops = new();
            return cityStops;
        }

        return tradeRouteManager.CityStops;
    }

    protected override void TradeRouteCheck(Vector3 endPosition)
    {
        if (followingRoute)
        {
            Vector3Int endLoc = Vector3Int.RoundToInt(endPosition);

            if (endLoc == tradeRouteManager.CurrentDestination)
            {
                tradeRouteManager.SetCity(world.GetCity(endLoc));
                atStop = true;
                isWaiting = true;
                tradeRouteManager.FinishedLoading.AddListener(BeginNextStepInRoute);
                LoadUnloadCo = StartCoroutine(tradeRouteManager.LoadUnloadCoroutine(loadUnloadRate));
                WaitTimeCo = StartCoroutine(tradeRouteManager.WaitTimeCoroutine());

                //if (tradeRouteManager.GoToNextStopCheck(loadUnloadRate))
                //{
                //    BeginNextStepInRoute();
                //}
                //Deselect(); //lots of repetition here. 
                //routeManager.CompleteTradeRouteOrders();
            }
        }
    }

    public override void BeginNextStepInRoute() //this does not have the finish movement listeners
    {
        tradeRouteManager.FinishedLoading.RemoveListener(BeginNextStepInRoute);
        if (LoadUnloadCo != null)
            StopCoroutine(LoadUnloadCo);
        LoadUnloadCo = null;
        if (WaitTimeCo != null)
            StopCoroutine(WaitTimeCo);
        WaitTimeCo = null;
        followingRoute = true;
        atStop = false;
        Vector3Int nextStop = tradeRouteManager.GoToNext();


        List<Vector3Int> currentPath = GridSearch.AStarSearch(world, transform.position, nextStop, isTrader, bySea);

        //List<TerrainData> paths = new();

        if (currentPath.Count == 0)
        {
            TradeRouteCheck(transform.position);
            return;
        }

        //foreach (Vector3Int path in currentPath)
        //{
        //    paths.Add(world.GetTerrainDataAt(path));
        //}

        if (currentPath.Count > 0)
        {
            FinalDestinationLoc = nextStop;
            MoveThroughPath(currentPath);
        }
    }

    public void CancelRoute()
    {
        followingRoute = false;
        if (LoadUnloadCo != null)
        {
            //tradeRouteManager.StopHoldingPatternCoroutine();
            StopCoroutine(LoadUnloadCo);
            tradeRouteManager.StopHoldingPatternCoroutine();
        }
        if (WaitTimeCo != null)
        {
            StopCoroutine(WaitTimeCo);
        }
    }

    //protected override void WaitTurnMethods()
    //{
    //    base.WaitTurnMethods();
    //    if (followingRoute && atStop)
    //    {
    //        if (tradeRouteManager.GoToNextStopCheck())
    //            BeginNextStepInRoute();
    //    }
    //}
}
