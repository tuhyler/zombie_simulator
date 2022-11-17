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
    private int secondIntervals = 1;
    private int timeWaited = 0;
    private int waitTime;
    //private Coroutine holdingPatternCo;
    [HideInInspector]
    public bool resourceCheck = false, waitForever = false;
    [HideInInspector]
    public int loadUnloadRate;

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

    //        uiPersonalResourceInfoPanel.UpdateResourceInteractable(resourceValue.resourceType, personalResourceManager.GetResourceDictValue(resourceValue.resourceType));
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
        this.loadUnloadRate = loadUnloadRate;
        bool complete = false;

        foreach (ResourceValue resourceValue in resourceAssignments[currentStop])
        {
            int resourceAmount = resourceValue.resourceAmount;
            bool loadUnloadCheck = true;

            if (resourceAmount == 0)
            {

                continue;
            }
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
                    int loadUnloadRateMod = Mathf.Min(resourceAmount - amountMoved, loadUnloadRate);
                    yield return new WaitForSeconds(secondIntervals);

                    int resourceAmountAdjusted = Mathf.Abs(city.ResourceManager.CheckResource(resourceValue.resourceType, -loadUnloadRateMod));
                    personalResourceManager.CheckResource(resourceValue.resourceType, resourceAmountAdjusted);


                    if (trader.isSelected)
                    {
                        uiPersonalResourceInfoPanel.UpdateResource(resourceValue.resourceType, personalResourceManager.GetResourceDictValue(resourceValue.resourceType));
                        uiPersonalResourceInfoPanel.UpdateStorageLevel(personalResourceManager.GetResourceStorageLevel);
                    }

                    amountMoved += resourceAmountAdjusted;
                    if (resourceAmountAdjusted == 0)
                        resourceCheck = true;

                    if (amountMoved >= resourceAmount)
                    {
                        loadUnloadCheck = false;
                    }
                    else if (resourceAmountAdjusted == 0)
                    {
                        city.SetWaiter(this, resourceValue.resourceType);
                        yield return HoldingPatternCoroutine();
                    }
                }

                complete = true;
            }
            else if (resourceAmount < 0) //moving from trader to city
            {
                //if trader holds less than what is asked to be dropped off
                int remainingWithTrader = personalResourceManager.GetResourceDictValue(resourceValue.resourceType);
                if (remainingWithTrader < Mathf.Abs(resourceAmount))
                    resourceAmount = -remainingWithTrader;

                int amountMoved = 0;

                while (loadUnloadCheck)
                {
                    int loadUnloadRateMod = Mathf.Abs(Mathf.Max(resourceAmount - amountMoved, -loadUnloadRate));
                    yield return new WaitForSeconds(secondIntervals);


                    int resourceAmountAdjusted = city.ResourceManager.CheckResource(resourceValue.resourceType, loadUnloadRateMod);
                    personalResourceManager.CheckResource(resourceValue.resourceType, -resourceAmountAdjusted);

                    if (trader.isSelected)
                    {
                        uiPersonalResourceInfoPanel.UpdateResource(resourceValue.resourceType, personalResourceManager.GetResourceDictValue(resourceValue.resourceType));
                        uiPersonalResourceInfoPanel.UpdateStorageLevel(personalResourceManager.GetResourceStorageLevel);
                    }
                    amountMoved -= resourceAmountAdjusted;
                    if (resourceAmountAdjusted == 0)
                        resourceCheck = true;

                    if (amountMoved <= resourceAmount)
                    {
                        loadUnloadCheck = false;
                    }
                    else if (resourceAmountAdjusted == 0)
                    {
                        city.SetWaiter(this);
                        yield return HoldingPatternCoroutine();
                    }
                }

                complete = true;
            }
        }

        if (complete)
            FinishLoading();
    }

    public IEnumerator WaitTimeCoroutine()
    {
        if (waitTime < 0)
        {
            waitForever = true;
            
            while (waitForever)
            {
                yield return new WaitForSeconds(secondIntervals);
                timeWaited += secondIntervals;
                //Debug.Log("waited " + timeWaited + " seconds");
            }

            yield break; //this is enough, just using while loop to count seconds
        }

        while (timeWaited < waitTime)
        {
            yield return new WaitForSeconds(secondIntervals);
            timeWaited += secondIntervals;
            //Debug.Log("waited " + timeWaited + " seconds");
        }

        FinishLoading();
    }

    private IEnumerator HoldingPatternCoroutine()
    {
        while (resourceCheck)
        {
            yield return new WaitForSeconds(1);        }
    }

    public void StopHoldingPatternCoroutine()
    {
        resourceCheck = false;
        StopCoroutine(HoldingPatternCoroutine());
    }

    private void FinishLoading()
    {
        Debug.Log("Finished loading");
        resourceCheck = false;
        waitForever=false;
        //StopAllCoroutines(); //this does nothing
        timeWaited = 0;
        city.CheckQueue();
        trader.isWaiting = false;
        IncreaseCurrentStop();
        FinishedLoading?.Invoke();
    }

    public void UnloadCheck()
    {
        resourceCheck = false;
    }

    private void IncreaseCurrentStop()
    {
        currentStop++;

        if (currentStop == cityStops.Count)
            currentStop = 0;
    }

    public void RemoveCityStop(Vector3Int cityStop)
    {
        cityStops.Remove(cityStop);
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
