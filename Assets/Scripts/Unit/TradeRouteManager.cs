using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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
    private UIPersonalResourceInfoPanel uiPersonalResourceInfoPanel;

    //private List<bool> resourceStepComplete = new();

    //for seeing if route orders are completed at a stop
    public UnityEvent FinishedLoading; //listener is in Trader
    private Dictionary<ResourceType, int> resourcesAtArrival = new();
    private int secondIntervals = 2;
    private int timeWaited = 0;
    private int waitTime;
    private Coroutine holdingPatternCo;
    [HideInInspector]
    public bool resourceCheck = false, loadUnloadCheck = true;

    private Trader trader;
    private City city;


    public void SetTradeRoute(List<Vector3Int> cityStops, List<List<ResourceValue>> resourceAssignments, List<int> waitTimes)
    {
        this.cityStops = cityStops;
        this.resourceAssignments = resourceAssignments;
        this.waitTimes = waitTimes;
        currentStop = 0;
    }

    public Vector3Int GoToNext()
    {
        city = null;
        resourcesAtArrival.Clear();
        currentDestination = cityStops[currentStop];
        waitTime = waitTimes[currentStop];
        return cityStops[currentStop];
    }

    public void SetPersonalResourceManager(PersonalResourceManager personalResourceManager, UIPersonalResourceInfoPanel uiPersonalResourceInfoPanel)
    {
        this.personalResourceManager = personalResourceManager;
        this.uiPersonalResourceInfoPanel = uiPersonalResourceInfoPanel;
    }

    public void SetTrader(Trader trader)
    {
        this.trader = trader;
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

    //public void CompleteTradeRouteOrders()
    //{
    //    incomplete = 0;

    //    foreach (ResourceValue resourceValue in resourceAssignments[currentStop])
    //    {
    //        bool complete = false;
    //        int resourceAmount = resourceValue.resourceAmount;
    //        //for setting amount to transport equal to something
    //        //resourceAmountText -= personalResourceManager.GetResourceDictValue(resourceValue.resourceType);

    //        if (resourceAmount == 0)
    //            continue;
    //        else if (resourceAmount > 0) //moving from city to trader
    //        {
    //            int remainingInCity = city.ResourceManager.GetResourceDictValue(resourceValue.resourceType);

    //            if (remainingInCity < resourceAmount)
    //                resourceAmount = remainingInCity;

    //            int resourceAmountAdjusted = personalResourceManager.CheckResource(resourceValue.resourceType, resourceAmount);
    //            city.ResourceManager.CheckResource(resourceValue.resourceType, -resourceAmountAdjusted);
    //            complete = personalResourceManager.GetResourceDictValue(resourceValue.resourceType) ==
    //                Mathf.Min(resourcesAtArrival[resourceValue.resourceType] + resourceValue.resourceAmount, personalResourceManager.ResourceStorageLimit);
    //        }
    //        else if (resourceAmount < 0) //moving from trader to city
    //        {
    //            //for dropoff
    //            int remainingWithTrader = personalResourceManager.GetResourceDictValue(resourceValue.resourceType);

    //            //for dropoff
    //            if (remainingWithTrader < Mathf.Abs(resourceAmount))
    //                resourceAmount = -remainingWithTrader;

    //            int resourceAmountAdjusted = city.ResourceManager.CheckResource(resourceValue.resourceType, -resourceAmount);
    //            personalResourceManager.CheckResource(resourceValue.resourceType, -resourceAmountAdjusted);
    //            complete = personalResourceManager.GetResourceDictValue(resourceValue.resourceType) ==
    //                Mathf.Max(resourcesAtArrival[resourceValue.resourceType] + resourceValue.resourceAmount, 0);
    //        }

    //        uiPersonalResourceInfoPanel.UpdateResource(resourceValue.resourceType, personalResourceManager.GetResourceDictValue(resourceValue.resourceType));
    //        uiPersonalResourceInfoPanel.UpdateStorageLevel(personalResourceManager.GetResourceStorageLevel);

    //        //for setting equal
    //        //bool complete = resourceValue.resourceAmount == personalResourceManager.GetResourceDictValue(resourceValue.resourceType);
    //        //for dropoff

    //        if (!complete)
    //            incomplete++;
    //        //resourceStepComplete.Add(complete); //for only checking the ones that aren't complete
    //        //GoToNextStopCheck();
    //    }
    //}

    public IEnumerator LoadUnloadCoroutine(int loadUnloadRate)
    {
        //incomplete = 0;
        //bool complete = true;

        foreach (ResourceValue resourceValue in resourceAssignments[currentStop])
        {
            int resourceAmount = resourceValue.resourceAmount;

            if (resourceAmount == 0)
                continue;
            else if (resourceAmount > 0) //moving from city to trader
            {
                //int remainingInCity = city.ResourceManager.GetResourceDictValue(resourceValue.resourceType);

                //if city has less than what's needed
                //if (remainingInCity < resourceAmount)
                //    resourceAmount = remainingInCity;

                //if trader wants more than it can store
                int level = Mathf.CeilToInt(trader.PersonalResourceManager.GetResourceStorageLevel);
                int limit = trader.CargoStorageLimit;
                if (limit - level < resourceAmount)
                    resourceAmount = limit - level;

                int amountMoved = 0;

                while (loadUnloadCheck)
                {
                    yield return new WaitForSeconds(secondIntervals);

                    int loadUnloadRateMod = Mathf.Min(resourceAmount - amountMoved, loadUnloadRate);
                    //Debug.Log("amount Moved is " + amountMoved + " and resource Amount is " + resourceAmount);
                    timeWaited += secondIntervals;

                    int resourceAmountAdjusted = Mathf.Abs(city.ResourceManager.CheckResource(resourceValue.resourceType, -loadUnloadRateMod));
                    personalResourceManager.CheckResource(resourceValue.resourceType, resourceAmountAdjusted);


                    //if (trader.isSelected)
                    //{
                        uiPersonalResourceInfoPanel.UpdateResource(resourceValue.resourceType, personalResourceManager.GetResourceDictValue(resourceValue.resourceType));
                        uiPersonalResourceInfoPanel.UpdateStorageLevel(personalResourceManager.GetResourceStorageLevel);
                    //}

                    Debug.Log("resource moved amount " + resourceAmountAdjusted);
                    amountMoved += resourceAmountAdjusted;
                    if (resourceAmountAdjusted == 0)
                        resourceCheck = true;

                    if (amountMoved >= resourceAmount)
                    {
                        loadUnloadCheck = false;
                    }
                    else if (resourceCheck)
                    {
                        while (resourceCheck)
                        {
                            yield return StartCoroutine(HoldingPatternCoroutine());
                        }

                        //holdingPatternCo = null;
                    }
                }
                //bool test = personalResourceManager.GetResourceDictValue(resourceValue.resourceType) ==
                //    Mathf.Min(resourcesAtArrival[resourceValue.resourceType] + resourceValue.resourceAmount, personalResourceManager.ResourceStorageLimit);
            }
            else if (resourceAmount < 0) //moving from trader to city
            {
                //for dropoff
                int remainingWithTrader = personalResourceManager.GetResourceDictValue(resourceValue.resourceType);

                //for dropoff
                if (remainingWithTrader < Mathf.Abs(resourceAmount))
                    resourceAmount = -remainingWithTrader;

                int amountMoved = 0;

                while (amountMoved > resourceAmount)
                {
                    yield return new WaitForSeconds(secondIntervals);
                    int loadUnloadRateMod = Mathf.Abs(Mathf.Max(resourceAmount - amountMoved, -loadUnloadRate));
                    timeWaited += secondIntervals;
                    int resourceAmountAdjusted = city.ResourceManager.CheckResource(resourceValue.resourceType, loadUnloadRateMod);
                    personalResourceManager.CheckResource(resourceValue.resourceType, -resourceAmountAdjusted);

                    //if (trader.isSelected)
                    //{
                        uiPersonalResourceInfoPanel.UpdateResource(resourceValue.resourceType, personalResourceManager.GetResourceDictValue(resourceValue.resourceType));
                        uiPersonalResourceInfoPanel.UpdateStorageLevel(personalResourceManager.GetResourceStorageLevel);
                    //}

                    amountMoved -= loadUnloadRateMod;
                }
                //complete = personalResourceManager.GetResourceDictValue(resourceValue.resourceType) ==
                //    Mathf.Max(resourcesAtArrival[resourceValue.resourceType] + resourceValue.resourceAmount, 0);
            }
        }

        FinishLoading();
    }

    private IEnumerator HoldingPatternCoroutine()
    {
        //while (resourceCheck)
        //{
            yield return new WaitForSeconds(secondIntervals);
            timeWaited += secondIntervals;
            Debug.Log("waited " + timeWaited + " seconds");

            if (timeWaited >= waitTime)
            {
                loadUnloadCheck = false;
                resourceCheck = false;
                yield break;
            }
        //}
    }

    public void StopHoldingPatternCoroutine()
    {
        if (holdingPatternCo != null)
            StopCoroutine(holdingPatternCo);
    }

    private void FinishLoading()
    {
        IncreaseCurrentStop();
        FinishedLoading?.Invoke();
    }


    private void IncreaseCurrentStop()
    {
        currentStop++;

        if (currentStop == cityStops.Count)
            currentStop = 0;
    }

    //public bool GoToNextStopCheck()
    //{
    //    CompleteTradeRouteOrders();

    //    if (incomplete == 0 || timeWaited == waitTime)
    //    {
    //        IncreaseCurrentStop();
    //        return true;
    //    }
    //    else //incomplete > 0
    //    {
    //        timeWaited++;
    //        return false;
    //    }
    //}

    //public void DumpResource(ResourceValue resourceValue)
    //{

    //}
}
