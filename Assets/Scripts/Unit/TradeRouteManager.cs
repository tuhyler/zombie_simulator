using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.Events;

public class TradeRouteManager : MonoBehaviour
{
    [HideInInspector]
    public int startingStop;
    [HideInInspector]
    public List<Vector3Int> cityStops = new();
    private Dictionary<int, List<Vector3Int>> routePathsDict = new();
    public Dictionary<int, List<Vector3Int>> RoutePathsDict { get { return routePathsDict; } set { routePathsDict = value; } }
    [HideInInspector]
    public List<List<ResourceValue>> resourceAssignments;
    [HideInInspector]
    public List<List<int>> resourceCompletion = new();
    [HideInInspector]
    public List<int> waitTimes;

    [HideInInspector]
    public int currentStop = 0, currentResource = 0, resourceCurrentAmount, resourceTotalAmount;
    private Vector3Int currentDestination;
    public Vector3Int CurrentDestination { get { return currentDestination; } set { currentDestination = value; } }

    //private PersonalResourceManager personalResourceManager;
    private UIPersonalResourceInfoPanel uiPersonalResourceInfoPanel;
    private UITradeRouteManager uiTradeRouteManager;

    //private List<bool> resourceStepComplete = new();

    //for seeing if route orders are completed at a stop
    public UnityEvent FinishedLoading; //listener is in Trader
    private Dictionary<ResourceType, int> resourcesAtArrival = new();
    private int secondIntervals = 1;
    [HideInInspector]
    public int timeWaited = 0, waitTime;
    //private Coroutine holdingPatternCo;
    [HideInInspector]
    public bool resourceCheck = false, waitForever = false;
    [HideInInspector]
    public int loadUnloadRate;

    private Trader trader;
    private City city;
    private Wonder wonder;
    private TradeCenter tradeCenter;
    private WaitForSeconds totalWait;
    [HideInInspector]
    public float percDone;
    //private WaitForSeconds resourceWait = new WaitForSeconds(1);
    //private WaitForSeconds loadWait;// = new WaitForSeconds(1);

    //public TradeRouteManager(TradeRouteManager tradeRouteManager)
    //{
    //    this = tradeRouteManager;
    //}


    private void Awake()
    {
        trader = GetComponent<Trader>();
        //loadWait = new WaitForSeconds(secondIntervals);
        totalWait = new WaitForSeconds(secondIntervals);
    }

    public void SetTradeRoute(int startingStop, List<Vector3Int> cityStops, List<List<ResourceValue>> resourceAssignments, List<int> waitTimes)
    {
        this.startingStop = startingStop;
        this.cityStops = cityStops;
        this.resourceAssignments = resourceAssignments;

        for (int i = 0; i < cityStops.Count; i++)
            resourceCompletion.Add(new List<int>());

        this.waitTimes = waitTimes;
        currentStop = startingStop;
    }

    public int CalculateRoutePaths(MapWorld world)
    {
        routePathsDict.Clear();
        int length = 0;
        
        for (int i = 0; i < cityStops.Count; i++)
        {
            int j;
            if (i == cityStops.Count - 1)
                j = 0;
            else
                j = i + 1;

			List<Vector3Int> currentPath = GridSearch.AStarSearch(world, cityStops[i], cityStops[j], true, trader.bySea);
            routePathsDict[i] = currentPath;
            length += currentPath.Count;
		}

        return length;
    }

    public Vector3Int GoToNext()
    {
        //city = null;
        //wonder = null;
        //tradeCenter = null;
        resourcesAtArrival.Clear();
        currentDestination = cityStops[currentStop];
        waitTime = waitTimes[currentStop];
        return cityStops[currentStop];
    }

    public List<Vector3Int> GetNextPath()
    {
        int nextPath;
        if (currentStop == 0)
            nextPath = cityStops.Count - 1;
        else
            nextPath = currentStop - 1;
        
        return routePathsDict[nextPath];
    }

    public void SetUIPersonalResourceManager(UIPersonalResourceInfoPanel uiPersonalResourceInfoPanel)
    {
        //this.personalResourceManager = personalResourceManager;
        this.uiPersonalResourceInfoPanel = uiPersonalResourceInfoPanel;
    }

    //public void SetPersonalResourceManager(PersonalResourceManager personalResourceManager)
    //{
    //    //this.personalResourceManager = personalResourceManager;
    //}

    public void SetTradeRouteManager(UITradeRouteManager uiTradeRouteManager)
    {
        this.uiTradeRouteManager = uiTradeRouteManager;
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

    public void SetWonder(Wonder wonder)
    {
        this.wonder = wonder;
        PrepareResourceDictionary();
    }

    public void SetTradeCenter(TradeCenter tradeCenter)
    {
        this.tradeCenter = tradeCenter;
        PrepareResourceDictionary();
    }

    private void PrepareResourceDictionary()
    {
        resourcesAtArrival = new(trader.personalResourceManager.ResourceDict);
    }

    public IEnumerator LoadUnloadCoroutine(int loadUnloadRate)
    {
        this.loadUnloadRate = loadUnloadRate;
        bool complete = false;

        int i = 0;
        trader.personalResourceManager.DictCheck(resourceAssignments[currentStop]);

        foreach (ResourceValue value in resourceAssignments[currentStop])
        {
            currentResource = i;
            i++;
            resourceTotalAmount = Mathf.Abs(value.resourceAmount);
            int resourceAmount = value.resourceAmount;
            bool loadUnloadCheck = true;
            percDone = 0;
            
            if (uiTradeRouteManager.activeStatus && trader.isSelected)
                uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[currentResource].SetCompletePerc(0, resourceTotalAmount);

            if (wonder != null)
            {
                if (!wonder.CheckResourceType(value.resourceType))
                {
                    InfoPopUpHandler.WarningMessage().Create(wonder.centerPos, "Wrong resource type: " + value.resourceType);
                    SuddenFinish(true);
                    complete = true;
                    continue;
                }
                else if (resourceAmount > 0)
                {
                    InfoPopUpHandler.WarningMessage().Create(wonder.centerPos, "Can't move from wonder");
                    SuddenFinish(true);
                    complete = true;
                    continue;
                }
            }
            else if (tradeCenter != null)
            {
                if (resourceAmount > 0)
                {
                    if (!tradeCenter.ResourceBuyDict.ContainsKey(value.resourceType))
                    {
                        InfoPopUpHandler.WarningMessage().Create(tradeCenter.mainLoc, "Can't buy " + value.resourceType);
                        SuddenFinish(true);
                        complete = true;
                        continue;
                    }
                }
                else if (resourceAmount < 0)
                {
                    if (!tradeCenter.ResourceSellDict.ContainsKey(value.resourceType))
                    {
                        InfoPopUpHandler.WarningMessage().Create(tradeCenter.mainLoc, "Can't sell " + value.resourceType);
                        SuddenFinish(true);
                        complete = true;
                        continue;
                    }
                }
            }

            if (resourceAmount == 0)
            {
                SuddenFinish(false);
                complete = true;
                continue;
            }
            else if (resourceAmount > 0) //moving from city to trader
            {
                //int remainingInCity = city.ResourceManager.GetResourceDictValue(resourceValue.resourceType);

                //if city has less than what's needed
                //if (remainingInCity < resourceAmount)
                //    resourceAmount = remainingInCity;

                //if trader wants more than it can store
                int level = Mathf.CeilToInt(trader.personalResourceManager.ResourceStorageLevel);
                int limit = trader.cargoStorageLimit;
                int currentAmount = trader.personalResourceManager.ResourceDict[value.resourceType];
                resourceAmount -= currentAmount;

                //for when trader already has requisite amount
                if (resourceAmount == 0)
                {
                    SuddenFinish(false);
                    complete = true;
                    continue;
                }

                if (limit - level < resourceAmount)
                {
                    resourceAmount = limit - level;
                    //resourceTotalAmount = resourceAmount;

                    //for when inventory is full
                    if (resourceAmount == 0)
                    {
                        SuddenFinish(true);
                        complete = true;
                        continue;
                    }
                }

                int amountMoved = 0;
                resourceCurrentAmount = currentAmount;
                bool isPlaying = false;

                while (loadUnloadCheck)
                {
                    int loadUnloadRateMod = Mathf.Min(resourceAmount - amountMoved, loadUnloadRate);
                    int resourceAmountAdjusted;
                    
                    if (city != null)
                        resourceAmountAdjusted = Mathf.Abs(city.ResourceManager.CheckResource(value.resourceType, -loadUnloadRateMod));
                    else if (wonder != null)
                        resourceAmountAdjusted = Mathf.Abs(wonder.CheckResource(value.resourceType, -loadUnloadRateMod));
                    else
                    {
                        if (tradeCenter.world.CheckWorldGold(loadUnloadRateMod * tradeCenter.ResourceBuyDict[value.resourceType]))
                            resourceAmountAdjusted = loadUnloadRateMod;
                        else
                            resourceAmountAdjusted = 0;
                    }

                    amountMoved += resourceAmountAdjusted;
                    resourceCurrentAmount += resourceAmountAdjusted;
                    percDone = (float)resourceCurrentAmount / resourceTotalAmount;

                    if (uiTradeRouteManager.activeStatus && trader.isSelected)
                        uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[currentResource].SetAmount(percDone);

                    if (resourceAmountAdjusted == 0)
                    {
                        resourceCheck = true;
                        if (city != null)
                            city.SetWaiter(this, value.resourceType);
                        else if (wonder != null)
                            wonder.SetWaiter(this, value.resourceType);
                        else
                            tradeCenter.SetWaiter(this, loadUnloadRateMod * tradeCenter.ResourceBuyDict[value.resourceType]);

                        trader.SetLoadingAnimation(false);
                        isPlaying = false;
                        yield return HoldingPatternCoroutine();
                        continue; //start loop over once resources have been found again
                    }
                    else
                    {
                        if (!isPlaying)
                        {
                            trader.SetLoadingAnimation(true);
                            isPlaying = true;
                        }
                    }

                    yield return totalWait;
                    trader.personalResourceManager.CheckResource(value.resourceType, resourceAmountAdjusted);

                    if (trader.isSelected)
                    {
                        uiPersonalResourceInfoPanel.UpdateResource(value.resourceType, trader.personalResourceManager.GetResourceDictValue(value.resourceType));
                        uiPersonalResourceInfoPanel.UpdateStorageLevel(trader.personalResourceManager.ResourceStorageLevel);
                    }

                    if (tradeCenter)
                    {
                        int buyAmount = -resourceAmountAdjusted * tradeCenter.ResourceBuyDict[value.resourceType];
                        tradeCenter.world.UpdateWorldResources(ResourceType.Gold, buyAmount);
                        InfoResourcePopUpHandler.CreateResourceStat(transform.position, buyAmount, ResourceHolder.Instance.GetIcon(ResourceType.Gold));
                    }

                    if (amountMoved >= resourceAmount)
                    {
                        loadUnloadCheck = false;
                    }
                }

                trader.SetLoadingAnimation(false);
                complete = true;
            }
            else if (resourceAmount < 0) //moving from trader to city
            {
                //if trader holds less than what is asked to be dropped off
                int remainingWithTrader = trader.personalResourceManager.GetResourceDictValue(value.resourceType);
                if (remainingWithTrader < Mathf.Abs(resourceAmount))
                {
                    resourceAmount = -remainingWithTrader;
                    //resourceTotalAmount = remainingWithTrader;
                    
                    //for when trader isn't carrying any
                    if (resourceAmount == 0)
                    {
                        SuddenFinish(true); 
                        complete = true;
                        continue;
                    }
                }

                int amountMoved = 0;
                resourceCurrentAmount = 0;
                bool isPlaying = false;

                while (loadUnloadCheck)
                {
                    int loadUnloadRateMod = Mathf.Abs(Mathf.Max(resourceAmount - amountMoved, -loadUnloadRate));
                    int resourceAmountAdjusted;

                    if (city != null)
                        resourceAmountAdjusted = city.ResourceManager.CheckResource(value.resourceType, loadUnloadRateMod);
                    else if (wonder != null)
                        resourceAmountAdjusted = wonder.CheckResource(value.resourceType, loadUnloadRateMod);
                    else
                        resourceAmountAdjusted = loadUnloadRateMod;

                    amountMoved -= resourceAmountAdjusted;
                    resourceCurrentAmount += resourceAmountAdjusted;
                    percDone = (float)resourceCurrentAmount / resourceTotalAmount;

                    if (uiTradeRouteManager.activeStatus && trader.isSelected)
                        uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[currentResource].SetAmount(percDone);

                    //if (resourceAmountAdjusted != 0)
                    //    trader.SetUnloadingAnimation(true);
                    //else
                    //    trader.SetUnloadingAnimation(false);

                    if (resourceAmountAdjusted == 0)
                    {
                        resourceCheck = true;
                        if (city != null)
                            city.SetWaiter(this);
                        else if (wonder != null)
                            wonder.SetWaiter(this);
                        //trade centers don't need to wait here

                        trader.SetUnloadingAnimation(false);
                        isPlaying = false;
                        yield return HoldingPatternCoroutine();
                        continue; //restart loop once there's storage room
                    }
                    else
                    {
                        if (!isPlaying)
                        {
                            trader.SetUnloadingAnimation(true);
                            isPlaying = true;
                        }
                    }

                    yield return totalWait;

                    trader.personalResourceManager.CheckResource(value.resourceType, -resourceAmountAdjusted);

                    if (trader.isSelected)
                    {
                        uiPersonalResourceInfoPanel.UpdateResource(value.resourceType, trader.personalResourceManager.GetResourceDictValue(value.resourceType));
                        uiPersonalResourceInfoPanel.UpdateStorageLevel(trader.personalResourceManager.ResourceStorageLevel);
                    }
                    //if (resourceAmountAdjusted == 0)
                    //    resourceCheck = true;
                    if (tradeCenter)
                    {
                        int sellAmount = resourceAmountAdjusted * tradeCenter.ResourceSellDict[value.resourceType];
                        tradeCenter.world.UpdateWorldResources(ResourceType.Gold, sellAmount);
                        InfoResourcePopUpHandler.CreateResourceStat(transform.position, sellAmount, ResourceHolder.Instance.GetIcon(ResourceType.Gold));
                    }

                    if (amountMoved <= resourceAmount)
                    {
                        loadUnloadCheck = false;
                    }
                    else if (resourceAmountAdjusted == 0)
                    {
                        if (city != null)
                            city.SetWaiter(this);
                        else if (wonder != null)
                            wonder.SetWaiter(this);
                        //trade centers don't need to wait here

                        trader.SetUnloadingAnimation(false);
                        yield return HoldingPatternCoroutine();
                    }
                }

                trader.SetUnloadingAnimation(false);
                complete = true;
            }

            if (resourceCompletion[currentStop].Count < currentResource + 1)
                resourceCompletion[currentStop].Add(Mathf.RoundToInt(percDone * 100));
            else
                resourceCompletion[currentStop][currentResource] = Mathf.RoundToInt(percDone * 100);
        }

        if (complete)
        {
            //trader.SetLoadingAnimation(false);
            //trader.SetUnloadingAnimation(false);
            trader.personalResourceManager.ResetDict(resourceAssignments[currentStop]);
            FinishLoading();
        }
    }

    private void SuddenFinish(bool fail)
    {
        int num = fail ? 0 : 100;

        if (resourceCompletion[currentStop].Count < currentResource + 1)
            resourceCompletion[currentStop].Add(num);
        else
            resourceCompletion[currentStop][currentResource] = num;

        if (uiTradeRouteManager.activeStatus && trader.isSelected)
            uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[currentResource].SetCompleteFull(fail, false);
    }

    public IEnumerator WaitTimeCoroutine()
    {
        bool forever = waitTime < 0;
        
        if (uiTradeRouteManager.activeStatus && trader.isSelected)
        {
            UITradeStopHandler stop = uiTradeRouteManager.tradeStopHandlerList[currentStop];
            stop.progressBarHolder.SetActive(true);
            stop.SetProgressBarValue(waitTime);
            stop.SetProgressBarMask(waitTime, forever);
            
            if (forever)
                stop.SetTime(0, forever);
            else
                stop.SetTime(waitTime, forever);
        }
        
        if (forever)
        {
            waitForever = true;
            
            while (waitForever)
            {
                yield return totalWait;
                timeWaited += secondIntervals;
                if (uiTradeRouteManager.activeStatus && trader.isSelected)
                    uiTradeRouteManager.tradeStopHandlerList[currentStop].SetTime(timeWaited, true);
                //Debug.Log("waited " + timeWaited + " seconds");
            }

            yield break; //this is enough, just using while loop to count seconds
        }

        while (timeWaited < waitTime)
        {
            yield return totalWait;
            timeWaited += secondIntervals;
            if (uiTradeRouteManager.activeStatus && trader.isSelected)
                uiTradeRouteManager.tradeStopHandlerList[currentStop].SetTime(waitTime - timeWaited, false);
            //Debug.Log("waited " + timeWaited + " seconds");
        }

        percDone = (float)trader.personalResourceManager.ResourceDict[resourceAssignments[currentStop][currentResource].resourceType] / resourceTotalAmount;
        if (uiTradeRouteManager.activeStatus && trader.isSelected)
        {
            if (percDone == 0)
                uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[currentResource].SetCompleteFull(true, true);
            else
                uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[currentResource].SetAmount(percDone);
        }

        if (resourceCompletion[currentStop].Count < currentResource + 1)
            resourceCompletion[currentStop].Add(Mathf.RoundToInt(percDone * 100));
        else
            resourceCompletion[currentStop][currentResource] = Mathf.RoundToInt(percDone * 100);

        for (int i = currentResource + 1; i < resourceAssignments[currentStop].Count; i++)
        {
            if (resourceCompletion[currentStop].Count < i + 1)
                resourceCompletion[currentStop].Add(0);
            else
                resourceCompletion[currentStop][i] = 0;

            if (uiTradeRouteManager.activeStatus && trader.isSelected)
                uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[i].SetCompleteFull(true, true);
        }

        trader.SetLoadingAnimation(false);
        trader.SetUnloadingAnimation(false);
        FinishLoading();
    }

    //for waiting for resources to arrive to load
    private IEnumerator HoldingPatternCoroutine()
    {
        while (resourceCheck)
        {
            yield return totalWait;        
        }
    }

    public void StopHoldingPatternCoroutine()
    {
        resourceCheck = false;
        StopCoroutine(HoldingPatternCoroutine());
        if (tradeCenter != null)
            tradeCenter.RemoveFromWaitList();
    }

    public void CancelLoad()
    {
        if (uiTradeRouteManager.activeStatus && trader.isSelected)
            uiTradeRouteManager.tradeStopHandlerList[currentStop].progressBarHolder.SetActive(false);

        timeWaited = 0;
        resourceCurrentAmount = 0;
        currentResource = 0;
    }

    public void FinishLoading()
    {
        if (uiTradeRouteManager.activeStatus && trader.isSelected)
        {
            uiTradeRouteManager.tradeStopHandlerList[currentStop].progressBarHolder.SetActive(false);
            uiTradeRouteManager.tradeStopHandlerList[currentStop].SetAsComplete(false);

            if (currentStop + 1 == cityStops.Count) //if last stop
            {
                uiTradeRouteManager.tradeStopHandlerList[0].SetAsCurrent(resourceCompletion[currentStop]);
                for (int i = 1; i < cityStops.Count; i++)
                {
                    uiTradeRouteManager.tradeStopHandlerList[i].SetAsNext(true, resourceCompletion[i]);
                }
            }
            else
            {
                uiTradeRouteManager.tradeStopHandlerList[currentStop + 1].SetAsCurrent(resourceCompletion[currentStop + 1]);
            }
        }

        //Debug.Log("Finished loading");
        resourceCheck = false;
        waitForever = false;
        timeWaited = 0;

        //if (city != null)
        //    city.CheckQueue();
        //else if (wonder != null)
        //    wonder.CheckQueue();
        //else
        //    tradeCenter.CheckQueue();

        trader.isWaiting = false;
        IncreaseCurrentStop();
        resourceCurrentAmount = 0;
        currentResource = 0;
        FinishedLoading?.Invoke();
    }

    public void CheckQueues()
    {
        if (city != null)
        {
            city.CheckQueue();
            city = null;
        }
        else if (wonder != null)
        {
            wonder.CheckQueue();
            wonder = null;
        }
        else if (tradeCenter != null)
        {
            tradeCenter.CheckQueue();
            tradeCenter = null;
        }
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

        startingStop = currentStop;
        if (uiTradeRouteManager.activeStatus && trader.isSelected)
            uiTradeRouteManager.SetChosenStopLive(currentStop);
    }

    public void RemoveStop(Vector3Int stop)
    {
        cityStops.Remove(stop);
    }

    public bool TradeRouteCheck()
    {
        List<Vector3Int> destinations = new();
        
        //checking for consecutive stops
        int i = 0;
        int childCount = cityStops.Count;
        bool consecFound = false;

        foreach (Vector3Int stop in cityStops)
        {
            destinations.Add(stop);

            if (i == childCount - 1)
                consecFound = destinations[i] == destinations[0];
            else if (i > 0)
                consecFound = destinations[i] == destinations[i - 1];

            i++;

            if (consecFound)
            {
                UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Consecutive stops for same stop");
                return false;
            }
            else
            {
                return true;
            }
        }

        return true;
    }
}
