using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Trader : Unit
{
    [HideInInspector]
    public PersonalResourceManager personalResourceManager;

    [HideInInspector]
    public TradeRouteManager tradeRouteManager;

    [HideInInspector]
    public int cargoStorageLimit;

    [HideInInspector]
    public bool hasRoute, interruptedRoute; //for showing begin route, for cancelling/following route, and for picking/dropping load

    public int loadUnloadRate = 1;

    [SerializeField]
    private GameObject ripples;

    private Coroutine LoadUnloadCo;
    private Coroutine WaitTimeCo;

    [HideInInspector]
    public Dictionary<ResourceType, int> resourceGridDict = new(); //order of resources shown


    //private UnitMovement unitMovement;

    private void Awake()
    {
        AwakeMethods();
    }

    protected override void AwakeMethods()
    {
        base.AwakeMethods();
        cargoStorageLimit = buildDataSO.cargoCapacity;
        tradeRouteManager = GetComponent<TradeRouteManager>();
        tradeRouteManager.SetTrader(this);
        isTrader = true;
        personalResourceManager = GetComponent<PersonalResourceManager>();
        personalResourceManager.SetTrader(this);
        tradeRouteManager.SetPersonalResourceManager(personalResourceManager);
        personalResourceManager.ResourceStorageLimit = cargoStorageLimit;
        if (bySea)
            ripples.SetActive(false);
    }

    public void TurnOnRipples()
    {
        if (!isMoving)
        {
            ripples.SetActive(true);
            //for tweening
            LeanTween.alpha(ripples, 1f, 0.2f).setFrom(0f).setEase(LeanTweenType.linear);
        }
    }

    public override void TurnOffRipples()
    {
        LeanTween.alpha(ripples, 0f, 0.5f).setFrom(1f).setEase(LeanTweenType.linear).setOnComplete(SetActiveStatusFalse);
    }

    private void SetActiveStatusFalse()
    {
        ripples.SetActive(false);
    }

    //passing details of the trade route
    public void SetTradeRoute(int startingStop, List<string> cityNames, List<List<ResourceValue>> resourceAssignments, List<int> waitTimes, UIPersonalResourceInfoPanel uiPersonalResourceInfoPanel)
    {
        //tradeRouteManager.SetTrader(this);
        tradeRouteManager.SetUIPersonalResourceManager(uiPersonalResourceInfoPanel);

        List<Vector3Int> tradeStops = new();

        foreach (string name in cityNames)
        {
            if (bySea)
                tradeStops.Add(world.GetHarborStopLocation(name));
            else
                tradeStops.Add(world.GetStopLocation(name));
        }

        if (tradeStops.Count > 0)
            hasRoute = true;
        else
            hasRoute = false;

        tradeRouteManager.SetTradeRoute(startingStop, tradeStops, resourceAssignments, waitTimes);
    }

    //public List<Vector3Int> GetCityStops()
    //{
    //    if (tradeRouteManager == null)
    //    {
    //        List<Vector3Int> cityStops = new();
    //        return cityStops;
    //    }

    //    return tradeRouteManager.CityStops;
    //}

    protected override void TradeRouteCheck(Vector3 endPosition)
    {
        if (followingRoute)
        {
            Vector3Int endLoc = Vector3Int.RoundToInt(endPosition);

            if (endLoc == tradeRouteManager.CurrentDestination)
            {
                //checking to see if stop still exists
                if (!world.CheckIfStopStillExists(endLoc))
                {
                    CancelRoute();
                    tradeRouteManager.RemoveStop(endLoc);
                    interruptedRoute = true;
                    return;
                }

                Vector3Int stopLoc = world.GetStopLocation(world.GetTradeLoc(endLoc));
                if (world.IsCityOnTile(stopLoc))
                    tradeRouteManager.SetCity(world.GetCity(stopLoc));
                else if (world.IsWonderOnTile(stopLoc))
                    tradeRouteManager.SetWonder(world.GetWonder(stopLoc));
                else
                    tradeRouteManager.SetTradeCenter(world.GetTradeCenter(stopLoc));
                //if (bySea)
                //    tradeRouteManager.SetCity(world.GetHarborCity(endLoc));
                //else
                //    tradeRouteManager.SetCity(world.GetCity(endLoc));
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

        //checking to see if stop still exists
        if (!world.CheckIfStopStillExists(nextStop))
        {
            CancelRoute();
            tradeRouteManager.RemoveStop(nextStop);
            interruptedRoute = true;
            return;
        }

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
            tradeRouteManager.CancelLoad();
            tradeRouteManager.StopHoldingPatternCoroutine();
            tradeRouteManager.FinishedLoading.RemoveListener(BeginNextStepInRoute);
        }
        if (WaitTimeCo != null)
        {
            StopCoroutine(WaitTimeCo);
            tradeRouteManager.CancelLoad();
            tradeRouteManager.FinishedLoading.RemoveListener(BeginNextStepInRoute);
        }
    }

    public void AddToGrid(ResourceType type)
    {
        resourceGridDict[type] = resourceGridDict.Count;
    }

    public void ReshuffleGrid()
    {
        int i = 0;

        //re-sorting
        Dictionary<ResourceType, int> myDict = resourceGridDict.OrderBy(d => d.Value).ToDictionary(x => x.Key, x => x.Value);

        List<ResourceType> types = new List<ResourceType>(myDict.Keys);

        foreach (ResourceType type in types)
        {
            int amount = personalResourceManager.ResourceDict[type];
            if (amount > 0)
            {
                resourceGridDict[type] = i;
                i++;
            }
            else
            {
                resourceGridDict.Remove(type);
            }
        }
    }
}
