using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TradeRouteManager : MonoBehaviour
{
    private List<Vector3Int> cityStops;
    public List<Vector3Int> CityStops { get { return cityStops; } }
    private List<List<ResourceValue>> resourceAssignments;
    public List<List<ResourceValue>> ResourceAssignments { get { return resourceAssignments; } }
    private List<int> waitTimes;
    public List<int> WaitTimes { get { return waitTimes; } }

    private int currentStop = 0;
    private Vector3Int currentDestination;
    public Vector3Int CurrentDestination { get { return currentDestination; } }

    private PersonalResourceManager personalResourceManager;

    //private List<bool> resourceStepComplete = new();

    //for seeing if route orders are completed at a stop
    private Dictionary<ResourceType, int> resourcesAtArrival = new();
    private int turnsWaited = 0;
    private int waitTime;
    private int incomplete;

    private City city;


    public void SetTradeRoute(List<Vector3Int> cityStops, List<List<ResourceValue>> resourceAssignments, List<int> waitTimes)
    {
        this.cityStops = cityStops;
        this.resourceAssignments = resourceAssignments;
        this.waitTimes = waitTimes;
    }

    public Vector3Int GoToNext()
    {
        city = null;
        resourcesAtArrival.Clear();
        currentDestination = cityStops[currentStop];
        waitTime = waitTimes[currentStop];
        return cityStops[currentStop];
    }

    public void SetPersonalResourceManager(PersonalResourceManager personalResourceManager)
    {
        this.personalResourceManager = personalResourceManager;
    }

    public void SetCity(City city)
    {
        this.city = city;
        PrepareResourceDictionary();
    }

    private void PrepareResourceDictionary()
    {
        resourcesAtArrival = new(personalResourceManager.ResourceDict);
    }

    public void CompleteTradeRouteOrders()
    {
        incomplete = 0;

        foreach (ResourceValue resourceValue in resourceAssignments[currentStop])
        {
            bool complete = false;
            int resourceAmount = resourceValue.resourceAmount;
            //for setting amount to transport equal to something
            //resourceAmountText -= personalResourceManager.GetResourceDictValue(resourceValue.resourceType);

            if (resourceAmount == 0)
                continue;
            else if (resourceAmount > 0) //moving from city to trader
            {
                int remainingInCity = city.ResourceManager.GetResourceDictValue(resourceValue.resourceType);

                if (remainingInCity < resourceAmount)
                    resourceAmount = remainingInCity;

                int resourceAmountAdjusted = personalResourceManager.CheckResource(resourceValue.resourceType, resourceAmount);
                city.ResourceManager.CheckResource(resourceValue.resourceType, -resourceAmountAdjusted);
                complete = personalResourceManager.GetResourceDictValue(resourceValue.resourceType) ==
                    Mathf.Min(resourcesAtArrival[resourceValue.resourceType] + resourceValue.resourceAmount, personalResourceManager.ResourceStorageLimit);
            }
            else if (resourceAmount < 0) //moving from trader to city
            {
                //for dropoff
                int remainingWithTrader = personalResourceManager.GetResourceDictValue(resourceValue.resourceType);

                //for dropoff
                if (remainingWithTrader < Mathf.Abs(resourceAmount))
                    resourceAmount = -remainingWithTrader;

                int resourceAmountAdjusted = city.ResourceManager.CheckResource(resourceValue.resourceType, -resourceAmount);
                personalResourceManager.CheckResource(resourceValue.resourceType, -resourceAmountAdjusted);
                complete = personalResourceManager.GetResourceDictValue(resourceValue.resourceType) ==
                    Mathf.Max(resourcesAtArrival[resourceValue.resourceType] + resourceValue.resourceAmount, 0);
            }

            //for setting equal
            //bool complete = resourceValue.resourceAmount == personalResourceManager.GetResourceDictValue(resourceValue.resourceType);
            //for dropoff

            if (!complete)
                incomplete++;
            //resourceStepComplete.Add(complete); //for only checking the ones that aren't complete
            //GoToNextStopCheck();
        }
    }

    private void IncreaseCurrentStop()
    {
        currentStop++;

        if (currentStop == cityStops.Count)
            currentStop = 0;
    }

    public bool GoToNextStopCheck()
    {
        CompleteTradeRouteOrders();

        if (incomplete == 0 || turnsWaited == waitTime)
        {
            IncreaseCurrentStop();
            return true;
        }
        else //incomplete > 0
        {
            turnsWaited++;
            return false;
        }
    }

    //public void DumpResource(ResourceValue resourceValue)
    //{

    //}
}
