using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TradeRouteManager : MonoBehaviour
{
    [HideInInspector]
    public int startingStop;
    [HideInInspector]
    public List<Vector3Int> cityStops = new();
    public Dictionary<int, List<Vector3Int>> routePathsDict = new();
    [HideInInspector]
    public List<List<ResourceValue>> resourceAssignments;
    [HideInInspector]
    public List<List<int>> resourceCompletion = new();
    [HideInInspector]
    public List<int> waitTimes;

    [HideInInspector]
    public int currentStop = 0, currentResource = 0, resourceCurrentAmount, resourceTotalAmount;
    [HideInInspector]
    public Vector3Int currentDestination;

    private UITradeRouteManager uiTradeRouteManager;

    //for seeing if route orders are completed at a stop
    private int secondIntervals = 1;
    [HideInInspector]
    public int timeWaited = 0, waitTime, amountMoved, goldAmount; 
    [HideInInspector]
    public bool resourceCheck = false, waitForever = false;

    [HideInInspector]
    public Trader trader;
    private ITradeStop stop;
    private WaitForSeconds totalWait;
    [HideInInspector]
    public float percDone;

    private void Awake()
    {
        trader = GetComponent<Trader>();
        //loadWait = new WaitForSeconds(secondIntervals);
        totalWait = new WaitForSeconds(secondIntervals);
    }

    public void ClearTradeRoute()
    {
        cityStops.Clear();
        resourceAssignments.Clear();
        resourceCompletion.Clear();
        waitTimes.Clear();
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
        //currentDestination = cityStops[startingStop];
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

			List<Vector3Int> currentPath = GridSearch.TraderMove(world, cityStops[i], cityStops[j], trader.bySea);
            routePathsDict[i] = currentPath;
            length += currentPath.Count;
		}

        return length;
    }

    public void SetWaitTime()
    {
        waitTime = waitTimes[currentStop];
    }

    public List<Vector3Int> GetNextPath()
    {
        int nextPath;
        if (currentStop == 0)
            nextPath = cityStops.Count - 1;
        else
            nextPath = currentStop - 1;
        
        //roll to see if ambushed (currently only affects land units)
        if (!trader.bySea && routePathsDict[nextPath].Count > 15 && UnityEngine.Random.Range(0, 100) < trader.world.ambushProb)
        {
            Vector3Int randomLoc = routePathsDict[nextPath][UnityEngine.Random.Range(7, routePathsDict[nextPath].Count - 8)];

            //randomLoc = new Vector3Int(8, 0, 26);
            //check if city close by
            bool farCity = true;
			List<City> cityList = trader.world.cityDict.Values.ToList();
            cityList.AddRange(trader.world.enemyCityDict.Values.ToList());

            for (int i = 0; i < cityList.Count; i++)
            {
                if (cityList[i].currentPop < 5)
                    continue;
                
                if (Mathf.Abs(cityList[i].cityLoc.x - randomLoc.x) < 13 && Mathf.Abs(cityList[i].cityLoc.z - randomLoc.z) < 13)
                {
                    farCity = false;
                    break;
                }
            }

            //randomLoc = new Vector3Int(42, 0, 45);
            if (farCity)
            {
                trader.ambushLoc = trader.world.GetClosestTerrainLoc(randomLoc);
                Debug.Log(trader.ambushLoc);
            }
            else
            {
                trader.ambushLoc = new Vector3Int(0, -10, 0); //can't reach this loc
            }
        }

        return routePathsDict[nextPath];
    }

    public void SetTradeRouteManager(UITradeRouteManager uiTradeRouteManager)
    {
        this.uiTradeRouteManager = uiTradeRouteManager;
    }

    public void SetTrader(Trader trader)
    {
        this.trader = trader;
        currentDestination = new Vector3Int(0, -10, 0);
    }

    public void SetStop(ITradeStop stop)
    {
        this.stop = stop;
    }

    public bool IsTC()
    {
        return stop.center != null;
    }

    //separated to be slightly faster
    public IEnumerator CityLoadUnloadCoroutine(int loadUnloadRate, bool loading)
    {
		bool complete = false;

		trader.personalResourceManager.DictCheck(resourceAssignments[currentStop]);

		for (int i = currentResource; i < resourceAssignments[currentStop].Count; i++)
		{
			currentResource = i;
			ResourceValue value = resourceAssignments[currentStop][currentResource];
			int resourceAmount = value.resourceAmount;
			resourceTotalAmount = Mathf.Abs(resourceAmount);
			bool loadUnloadCheck = true;
			percDone = 0;

			if (uiTradeRouteManager.activeStatus && trader.isSelected)
				uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[currentResource].SetCompletePerc(0, resourceTotalAmount);

			if (resourceAmount == 0)
			{
				SuddenFinish(false);
				complete = true;
				continue;
			}
			else if (resourceAmount > 0) //moving from city to trader
			{
                resourceAmount = Mathf.Max(0, resourceAmount - trader.personalResourceManager.resourceDict[value.resourceType]); //subtracting amount already present in trader
                
                if (loading)
				{
					resourceAmount += amountMoved; //resetting to original amount
					int space = Mathf.Max(0, trader.personalResourceManager.resourceStorageLimit - trader.personalResourceManager.resourceStorageLevel);
					if (space + amountMoved < resourceAmount)
						resourceAmount = space + amountMoved;

					resourceCurrentAmount = amountMoved;

					if (resourceCheck)
					{
						trader.SetWarning(false, false, false, false);
						trader.SetLoadingAnimation(false);

						yield break;
					}
				}
				else
				{
					amountMoved = 0;
					resourceCurrentAmount = 0;

					//if trader wants more than it can store
					int space = Mathf.Max(0, trader.personalResourceManager.resourceStorageLimit - trader.personalResourceManager.resourceStorageLevel);
					if (space < resourceAmount)
					{
						resourceAmount = space;

						//for when inventory is full
						if (resourceAmount <= 0)
						{
							SuddenFinish(true);
							complete = true;
							continue;
						}
					}
				}

				//for when trader already has requisite amount
				if (resourceAmount <= 0)
				{
					SuddenFinish(false);
					complete = true;
					continue;
				}

				bool isPlaying = false;

				if (!trader.resourceGridDict.ContainsKey(value.resourceType))
					trader.AddToGrid(value.resourceType);

				while (loadUnloadCheck)
				{
					int loadUnloadRateMod = Mathf.Min(resourceAmount - amountMoved, loadUnloadRate);
					int resourceAmountAdjusted = Mathf.Abs(stop.city.resourceManager.SubtractTraderResource(value.resourceType, loadUnloadRateMod));

					amountMoved += resourceAmountAdjusted;
					resourceCurrentAmount += resourceAmountAdjusted;
					percDone = (float)resourceCurrentAmount / resourceTotalAmount;

					if (uiTradeRouteManager.activeStatus && trader.isSelected)
						uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[currentResource].SetAmount(percDone);

					if (resourceAmountAdjusted == 0)
					{
						resourceCheck = true;
						stop.city.resourceManager.AddToResourceWaitList(value.resourceType, trader);
						trader.SetWarning(false, false, false, false);

						trader.SetLoadingAnimation(false);
						isPlaying = false;

						yield break;
					}
					else if (!isPlaying)
					{
						trader.SetLoadingAnimation(true);
						isPlaying = true;
					}

					//do it before coroutine so that numbers are accurate and can't cancel whilst occurring
					trader.personalResourceManager.AddResource(value.resourceType, resourceAmountAdjusted);

					yield return totalWait;

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
				if (loading)
				{
					int remainingWithTrader = trader.personalResourceManager.GetResourceDictValue(value.resourceType);
					if (remainingWithTrader - amountMoved < Mathf.Abs(resourceAmount))
					{
						resourceAmount = -remainingWithTrader;
						resourceAmount += amountMoved;
						resourceTotalAmount = Mathf.Abs(resourceAmount);
					}

					resourceCurrentAmount = Mathf.Abs(amountMoved);

					if (resourceCheck)
					{
						trader.SetUnloadingAnimation(false);
						trader.SetWarning(true, false, false, false);
						yield break;
					}
				}
				else
				{
					amountMoved = 0;
					resourceCurrentAmount = 0;

					//if trader holds less than what is asked to be dropped off
					int remainingWithTrader = trader.personalResourceManager.GetResourceDictValue(value.resourceType);
					if (remainingWithTrader < Mathf.Abs(resourceAmount))
					{
						resourceAmount = -remainingWithTrader;
						resourceTotalAmount = remainingWithTrader;

						//for when trader isn't carrying any
						if (resourceAmount == 0)
						{
							SuddenFinish(true);
							complete = true;
							continue;
						}
					}
				}

				bool isPlaying = false;
				bool maxCheck = stop.city.resourceManager.resourceMaxHoldDict[value.resourceType] >= 0;

				if (!stop.city.resourceGridDict.ContainsKey(value.resourceType))
					stop.city.AddToGrid(value.resourceType);

				while (loadUnloadCheck)
				{
					int loadUnloadRateMod = Mathf.Abs(Mathf.Max(resourceAmount - amountMoved, -loadUnloadRate));
                    int resourceAmountAdjusted;

					if (maxCheck)
					{
						int maxDiff = Mathf.Clamp(stop.city.resourceManager.resourceMaxHoldDict[value.resourceType] - stop.city.resourceManager.resourceDict[value.resourceType], 0, loadUnloadRateMod);
                        resourceAmountAdjusted = stop.city.resourceManager.AddTraderResource(value.resourceType, maxDiff);

						if (maxDiff < resourceAmountAdjusted)
							resourceAmountAdjusted = maxDiff;
					}
                    else
                    {
                        resourceAmountAdjusted = stop.city.resourceManager.AddTraderResource(value.resourceType, loadUnloadRateMod);
					}

					amountMoved -= resourceAmountAdjusted;
					resourceCurrentAmount += resourceAmountAdjusted;
					percDone = (float)resourceCurrentAmount / resourceTotalAmount;

					if (uiTradeRouteManager.activeStatus && trader.isSelected)
						uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[currentResource].SetAmount(percDone);

					if (resourceAmountAdjusted == 0)
					{
						trader.SetUnloadingAnimation(false);
						if (maxCheck && stop.city.warehouseStorageLimit > stop.city.resourceManager.resourceStorageLevel)
						{
							loadUnloadCheck = false;
							break;
						}
						stop.city.resourceManager.AddToUnloadWaitList(trader);

						resourceCheck = true;
						isPlaying = false;
						trader.SetWarning(true, false, false, false);
						yield break;
					}
					else if (!isPlaying)
					{
						trader.SetUnloadingAnimation(true);
						isPlaying = true;
					}

					trader.personalResourceManager.SubtractResource(value.resourceType, resourceAmountAdjusted);

					yield return totalWait;

					if (amountMoved <= resourceAmount)
						loadUnloadCheck = false;
				}

				trader.SetUnloadingAnimation(false);
				resourceCurrentAmount = 0;
				amountMoved = 0;
				complete = true;
			}

			if (resourceCompletion[currentStop].Count < currentResource + 1)
				resourceCompletion[currentStop].Add(Mathf.RoundToInt(percDone * 100));
			else
				resourceCompletion[currentStop][currentResource] = Mathf.RoundToInt(percDone * 100);
		}

		if (complete)
		{
			trader.personalResourceManager.ResetDict(resourceAssignments[currentStop]);
			FinishLoading();
		}
	}

    public IEnumerator LoadUnloadCoroutine(int loadUnloadRate, bool loading)
    {
		bool complete = false;

        trader.personalResourceManager.DictCheck(resourceAssignments[currentStop]);

        for (int i = currentResource; i < resourceAssignments[currentStop].Count; i++)
        {
            currentResource = i;
            ResourceValue value = resourceAssignments[currentStop][currentResource];
            int resourceAmount = value.resourceAmount;
            resourceTotalAmount = Mathf.Abs(resourceAmount);
            bool loadUnloadCheck = true;
            int cost = 0; //for buying from trade center
            percDone = 0;
            
            if (uiTradeRouteManager.activeStatus && trader.isSelected)
                uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[currentResource].SetCompletePerc(0, resourceTotalAmount);

            if (stop.wonder != null)
            {
                if (!stop.wonder.CheckResourceType(value.resourceType))
                {
                    InfoPopUpHandler.WarningMessage(trader.world.objectPoolItemHolder).Create(stop.wonder.centerPos, "Wrong resource type: " + value.resourceType);
                    SuddenFinish(true);
                    complete = true;
                    continue;
                }
                else if (resourceAmount > 0)
                {
                    InfoPopUpHandler.WarningMessage(trader.world.objectPoolItemHolder).Create(stop.wonder.centerPos, "Can't move from wonder");
                    SuddenFinish(true);
                    complete = true;
                    continue;
                }
            }
            else if (stop.center != null)
            {
                if (resourceAmount > 0)
                {
                    if (!stop.center.resourceBuyDict.ContainsKey(value.resourceType))
                    {
                        InfoPopUpHandler.WarningMessage(trader.world.objectPoolItemHolder).Create(stop.center.mainLoc, "Can't buy " + value.resourceType);
                        SuddenFinish(true);
                        complete = true;
                        continue;
                    }
                }
                else if (resourceAmount < 0)
                {
                    if (!stop.center.resourceSellDict.ContainsKey(value.resourceType))
                    {
                        InfoPopUpHandler.WarningMessage(trader.world.objectPoolItemHolder).Create(stop.center.mainLoc, "Can't sell " + value.resourceType);
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
            else if (resourceAmount > 0) //moving from trade center to trader
            {
				resourceAmount = Mathf.Max(0, resourceAmount - trader.personalResourceManager.resourceDict[value.resourceType]); //subtracting amount already present in trader

				if (loading)
                {
					resourceAmount += amountMoved;
					int space = Mathf.Max(0, trader.personalResourceManager.resourceStorageLimit - trader.personalResourceManager.resourceStorageLevel);
					if (space + amountMoved < resourceAmount)
                        resourceAmount = space + amountMoved;

					resourceCurrentAmount = amountMoved;

                    if (resourceCheck)
                    {
                        goldAmount = Mathf.Min(resourceAmount - amountMoved, loadUnloadRate) * Mathf.CeilToInt(stop.center.resourceBuyDict[value.resourceType] * stop.center.multiple);
                        stop.center.SetWaiter(trader, goldAmount, true);
						trader.SetWarning(false, false, true, false);
						trader.SetLoadingAnimation(false);

                        yield break;
					}
                }
                else
                {
                    amountMoved = 0;
                    resourceCurrentAmount = 0;
                
                    //if trader wants more than it can store
                    int space = Mathf.Max(0, trader.personalResourceManager.resourceStorageLimit - trader.personalResourceManager.resourceStorageLevel);
                    if (space < resourceAmount)
                    {
                        resourceAmount = space;

                        //for when inventory is full
                        if (resourceAmount <= 0)
                        {
                            SuddenFinish(true);
                            complete = true;
                            continue;
                        }
                    }
                }

                //for when trader already has requisite amount
                if (resourceAmount <= 0)
                {
                    SuddenFinish(false);
                    complete = true;
                    continue;
                }

                bool isPlaying = false;

				if (!trader.resourceGridDict.ContainsKey(value.resourceType))
					trader.AddToGrid(value.resourceType);

				while (loadUnloadCheck)
                {
                    int loadUnloadRateMod = Mathf.Min(resourceAmount - amountMoved, loadUnloadRate);
                    int resourceAmountAdjusted;
                    
					cost = Mathf.CeilToInt(stop.center.resourceBuyDict[value.resourceType] * stop.center.multiple); //cost is calculated each time in case it changes while trader is there
					if (stop.center.world.CheckWorldGold(loadUnloadRateMod * cost))
                        resourceAmountAdjusted = loadUnloadRateMod;
                    else
                        resourceAmountAdjusted = 0;

                    amountMoved += resourceAmountAdjusted;
                    resourceCurrentAmount += resourceAmountAdjusted;
                    percDone = (float)resourceCurrentAmount / resourceTotalAmount;

                    if (uiTradeRouteManager.activeStatus && trader.isSelected)
                        uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[currentResource].SetAmount(percDone);

                    if (resourceAmountAdjusted == 0)
                    {
                        resourceCheck = true;
                        goldAmount = loadUnloadRateMod * cost;
						stop.center.SetWaiter(trader, goldAmount, false);
						trader.SetWarning(false, false, true, false);

                        trader.SetLoadingAnimation(false);
                        isPlaying = false;

                        yield break;
                    }
                    else if (!isPlaying)
					{
                        trader.SetLoadingAnimation(true);
                        isPlaying = true;
                    }

                    //do it before coroutine so that numbers are accurate and can't cancel whilst occurring
                    trader.personalResourceManager.AddResource(value.resourceType, resourceAmountAdjusted);

                    int buyAmount = -resourceAmountAdjusted * cost;
					stop.center.tcRep.IncreasePurchasedAmount(buyAmount);
					stop.center.world.UpdateWorldGold(buyAmount);
                    InfoResourcePopUpHandler.CreateResourceStat(transform.position, buyAmount, ResourceHolder.Instance.GetIcon(ResourceType.Gold), stop.center.world);

                    yield return totalWait;

                    if (amountMoved >= resourceAmount)
                    {
                        loadUnloadCheck = false;
                    }
                }

                trader.SetLoadingAnimation(false);
                complete = true;
            }
            else if (resourceAmount < 0) //moving from trader to wonder/trade center
            {
                if (loading)
                {
					int remainingWithTrader = trader.personalResourceManager.GetResourceDictValue(value.resourceType);
                    if (remainingWithTrader - amountMoved < Mathf.Abs(resourceAmount))
                    {
                        resourceAmount = -remainingWithTrader;
                        resourceAmount += amountMoved;
                        resourceTotalAmount = Mathf.Abs(resourceAmount);
                    }
					
                    resourceCurrentAmount = Mathf.Abs(amountMoved);

                    if (resourceCheck)
                    {
                        trader.SetUnloadingAnimation(false);
					    trader.SetWarning(true, false, false, false);
                        yield break;
                    }
				}
                else
                {
                    amountMoved = 0;
                    resourceCurrentAmount = 0;
				
    				//if trader holds less than what is asked to be dropped off
                    int remainingWithTrader = trader.personalResourceManager.GetResourceDictValue(value.resourceType);
                    if (remainingWithTrader < Mathf.Abs(resourceAmount))
                    {
                        resourceAmount = -remainingWithTrader;
                        resourceTotalAmount = remainingWithTrader;
                    
                        //for when trader isn't carrying any
                        if (resourceAmount == 0)
                        {
                            SuddenFinish(true); 
                            complete = true;
                            continue;
                        }
                    }

                    //for when wonder doesn't need as much as trader carries
                    if (stop.wonder)
                    {
                        int amountNeeded = stop.wonder.resourceCostDict[value.resourceType] - stop.wonder.resourceDict[value.resourceType];

                        if (amountNeeded < remainingWithTrader)
                        {
                            resourceAmount = -amountNeeded;
                            resourceTotalAmount = amountNeeded;
                        }
                    }
                }

                bool isPlaying = false;

				while (loadUnloadCheck)
                {
                    int loadUnloadRateMod = Mathf.Abs(Mathf.Max(resourceAmount - amountMoved, -loadUnloadRate));
                    int resourceAmountAdjusted;

                    if (stop.wonder != null)
                    {
						if (stop.wonder.completed)
                        {
							stop.wonder.ClearWonderStop();
							yield break;
						}

                        resourceAmountAdjusted = stop.wonder.AddResource(value.resourceType, loadUnloadRateMod);

                        if (stop.wonder.CompletedResourceCheck(value.resourceType))
                        {
                            loadUnloadCheck = false;

                            if (resourceAmountAdjusted == 0)
                                break;
                        }
					}
                    else
                    {
                        resourceAmountAdjusted = loadUnloadRateMod;
                    }

                    amountMoved -= resourceAmountAdjusted;
                    resourceCurrentAmount += resourceAmountAdjusted;
                    percDone = (float)resourceCurrentAmount / resourceTotalAmount;

                    if (uiTradeRouteManager.activeStatus && trader.isSelected)
                        uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[currentResource].SetAmount(percDone);

                    if (resourceAmountAdjusted == 0)
                    {
                        trader.SetUnloadingAnimation(false);
                        resourceCheck = true;
                        isPlaying = false;
						trader.SetWarning(true, false, false, false);
                        yield break;
                    }
                    else if (!isPlaying)
					{
                        trader.SetUnloadingAnimation(true);
                        isPlaying = true;
                    }

                    trader.personalResourceManager.SubtractResource(value.resourceType, resourceAmountAdjusted);

                    if (stop.center)
                    {
                        int sellAmount = resourceAmountAdjusted * stop.center.resourceSellDict[value.resourceType];
						stop.center.world.UpdateWorldGold(sellAmount);
                        InfoResourcePopUpHandler.CreateResourceStat(transform.position, sellAmount, ResourceHolder.Instance.GetIcon(ResourceType.Gold), stop.center.world);
                    }

                    yield return totalWait;

                    if (amountMoved <= resourceAmount)
                    {
                        loadUnloadCheck = false;
                    }
                }

                trader.SetUnloadingAnimation(false);
                resourceCurrentAmount = 0;
                amountMoved = 0;
                complete = true;
            }

            if (resourceCompletion[currentStop].Count < currentResource + 1)
                resourceCompletion[currentStop].Add(Mathf.RoundToInt(percDone * 100));
            else
                resourceCompletion[currentStop][currentResource] = Mathf.RoundToInt(percDone * 100);
        }

        if (complete)
        {
			trader.personalResourceManager.ResetDict(resourceAssignments[currentStop]);
            
            if (stop.wonder)
            {
                if (stop.wonder.completed)
                {
                    FinishWonder();
				    stop.wonder.ClearWonderStop();
                }
                else if (stop.wonder.ResourcesNeededCheck(resourceAssignments[currentStop]))
                {
                    trader.CancelRoute();
                }
                else
                {
					FinishLoading();
				}
            }
            else
            {
                FinishLoading();
            }
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

        if (!trader.personalResourceManager.resourceDict.ContainsKey(resourceAssignments[currentStop][currentResource].resourceType))
            percDone = 0;
        else
            percDone = (float)trader.personalResourceManager.resourceDict[resourceAssignments[currentStop][currentResource].resourceType] / resourceTotalAmount;
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

        StopHoldingPatternCoroutine();
        trader.SetLoadingAnimation(false);
        trader.SetUnloadingAnimation(false);
        FinishLoading();
    }

    //for waiting for resources to arrive to load
    //private IEnumerator HoldingPatternCoroutine()
    //{
    //    yield return new WaitWhile(() => resourceCheck);
        
    //    //while (resourceCheck)
    //    //    yield return null;
    //}

    public bool ResourceWaitCheck(ResourceType type)
    {
        if (type == ResourceType.None)
        {
            int loadUnloadRateMod = Mathf.Abs(Mathf.Max(resourceAssignments[currentStop][currentResource].resourceAmount - amountMoved, -trader.loadUnloadRate));
            if (stop.city != null && loadUnloadRateMod <= stop.city.warehouseStorageLimit - stop.city.resourceManager.resourceStorageLevel)
            {
                //ContinueLoadingUnloading();
                //trader.RemoveWarning();
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            if (stop.city != null && resourceAssignments[currentStop][currentResource].resourceType == type && 
                stop.city.resourceManager.resourceDict[type] - stop.city.resourceManager.resourceMinHoldDict[type] > 0)
            {
                //ContinueLoadingUnloading();
                //         resourceCheck = false;
			    //trader.RemoveWarning();
                return true;
		    }
            else
            {
                return false;
            }
        }
    }

    public void ContinueLoadingUnloading()
    {
		trader.RemoveWarning();
		resourceCheck = false;
        trader.RestartLoadUnload(stop.city != null);
    }

    public void StopHoldingPatternCoroutine()
    {
        //ContinueLoadingUnloading();
        trader.RemoveWarning();
        //StopCoroutine(holdingCo);
        if (resourceCheck)
        {
            if (stop.center != null)
                trader.world.RemoveFromGoldWaitList(stop.center);
            if (stop.city != null)
            {
                if (resourceAssignments[currentStop][currentResource].resourceAmount > 0)
                {
                    ResourceValue tempValue;
                    tempValue.resourceType = resourceAssignments[currentStop][currentResource].resourceType;
                    tempValue.resourceAmount = resourceAssignments[currentStop][currentResource].resourceAmount;
                    List<ResourceValue> tempValueList = new() { tempValue };
                    stop.city.resourceManager.RemoveFromCityResourceWaitList(trader, tempValueList);
                }
                else
                {
                    stop.city.resourceManager.RemoveFromUnloadWaitList(trader);
                }
            }

            resourceCheck = false;
        }
    }

	public void CancelLoad()
    {
        if (uiTradeRouteManager.activeStatus && trader.isSelected)
            uiTradeRouteManager.tradeStopHandlerList[currentStop].progressBarHolder.SetActive(false);

        timeWaited = 0;
        resourceCurrentAmount = 0;
        amountMoved = 0;
        currentResource = 0;
    }

    public void FinishWonder()
    {
		FinishLoadSteps();
	}

    public void FinishLoading()
    {
        FinishLoadSteps();
		IncreaseCurrentStop();
        trader.BeginNextStepInRoute();
    }

    private void FinishLoadSteps()
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

		resourceCheck = false; //just in case
		trader.RemoveWarning();
		waitForever = false;
		timeWaited = 0;
		trader.isWaiting = false;
		trader.originalMoveSpeed = trader.buildDataSO.movementSpeed;

		resourceCurrentAmount = 0;
        currentResource = 0;
	}

    public void CheckQueues()
    {
        if (stop != null)
        {
            stop.RemoveFromWaitList(trader, stop);
            stop = null;
        }
    }

    private void IncreaseCurrentStop()
    {
        currentStop++;

        if (currentStop == cityStops.Count)
        {
            trader.paid = false;
            currentStop = 0;
        }

        startingStop = currentStop;
        if (uiTradeRouteManager.activeStatus && trader.isSelected)
            uiTradeRouteManager.SetChosenStopLive(currentStop);
    }

    //public void RemoveStop(Vector3Int stop)
    //{
    //    cityStops.Remove(stop);
    //}

    public bool TradeRouteCheck()
    {
        List<Vector3Int> destinations = new();
        
        //checking for consecutive stops
        int childCount = cityStops.Count;
        bool consecFound = false;

        for (int i = 0; i < cityStops.Count; i++)
        {
            if (trader.world.GetStopName(cityStops[i]) == "")
            {
				UIInfoPopUpHandler.WarningMessage().Create(trader.world.unitMovement.uiTraderPanel.uiBeginTradeRoute.transform.position, "No assigned destination to stop");
				return false;
            }

            destinations.Add(cityStops[i]);

            if (i == childCount - 1)
                consecFound = destinations[i] == destinations[0];
            else if (i > 0)
                consecFound = destinations[i] == destinations[i - 1];

            if (consecFound)
            {
                UIInfoPopUpHandler.WarningMessage().Create(trader.world.unitMovement.uiTraderPanel.uiBeginTradeRoute.transform.position, "Consecutive stops for same stop");
                return false;
            }
        }

        return true;
    }
}
