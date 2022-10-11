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

    //private UnitMovement unitMovement;

    private void Awake()
    {
        AwakeMethods();
        isTrader = true;
        //unitMovement = FindObjectOfType<UnitMovement>();
    }

    protected override void AwakeMethods()
    {
        base.AwakeMethods();
        personalResourceManager = GetComponent<PersonalResourceManager>();
        personalResourceManager.ResourceStorageLimit = cargoStorageLimit;
    }

    //passing details of the trade route
    public void SetTradeRoute(List<string> cityNames, List<List<ResourceValue>> resourceAssignments, List<int> waitTimes)
    {
        tradeRouteManager = GetComponent<TradeRouteManager>();
        tradeRouteManager.SetPersonalResourceManager(personalResourceManager);

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

    public void BeginNextStepInRoute() //this does not have the finish movement listeners
    {
        followingRoute = true;
        atStop = false;
        
        List<Vector3Int> currentPath = GridSearch.AStarSearch(world, transform.position, tradeRouteManager.GoToNext(), isTrader);

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
            MoveThroughPath(currentPath);
    }

    public void CancelRoute()
    {
        followingRoute = false;
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
