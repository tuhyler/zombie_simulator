using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.Rendering.DebugUI;

public class TradeRouteManager : MonoBehaviour
{
    [HideInInspector]
    public int startingStop, ambushProb = 100;
    [HideInInspector]
    public List<Vector3Int> cityStops = new();
    public Dictionary<int, List<Vector3Int>> routePathsDict = new();
    //public Dictionary<int, List<Vector3Int>> RoutePathsDict { get { return routePathsDict; } set { routePathsDict = value; } }
    [HideInInspector]
    public List<int> ambushSpots = new();
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
    //private UIPersonalResourceInfoPanel uiPersonalResourceInfoPanel;
    private UITradeRouteManager uiTradeRouteManager;

    //private List<bool> resourceStepComplete = new();

    //for seeing if route orders are completed at a stop
    public UnityEvent FinishedLoading; //listener is in Trader
    private Dictionary<ResourceType, int> resourcesAtArrival = new();
    private int secondIntervals = 1;
    [HideInInspector]
    public int timeWaited = 0, waitTime, amountMoved/*for storage waitlist*/; 
    //private Coroutine holdingCo;
    [HideInInspector]
    public bool resourceCheck = false, waitForever = false;
    [HideInInspector]
    public int loadUnloadRate;

    [HideInInspector]
    public Trader trader;
    private ITradeStop stop;
    //private City city;
    //private Wonder wonder;
    //private TradeCenter tradeCenter;
    private WaitForSeconds totalWait;
    [HideInInspector]
    public float percDone;
    //private ResourceType resourceWaiter;
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

			List<Vector3Int> currentPath = GridSearch.TraderMove(world, cityStops[i], cityStops[j], trader.bySea);
            routePathsDict[i] = currentPath;
            length += currentPath.Count;

            if (currentPath.Count > 22)
                ambushSpots.Add(i);
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
        
        //roll to see if ambushed (currently only affects land units)
        if (!trader.bySea && ambushSpots.Contains(nextPath) && UnityEngine.Random.Range(0, 100) < ambushProb)
        {
            Vector3Int randomLoc = routePathsDict[nextPath][UnityEngine.Random.Range(6, routePathsDict[nextPath].Count - 5)];

            Debug.Log(randomLoc);
            randomLoc = new Vector3Int(8, 0, 26);
            //check if city close by
            bool farCity = true;

            foreach (City city in trader.world.cityDict.Values)
            {
                if (city.currentPop < 5)
                    continue;
                
                if (Mathf.Abs(city.cityLoc.x - randomLoc.x) < 13 && Mathf.Abs(city.cityLoc.z - randomLoc.z) < 13)
                {
                    farCity = false;
                    break;
                }
            }

            if (farCity)
            {
                trader.ambushLoc = trader.world.GetClosestTerrainLoc(randomLoc);
            }
            else
            {
                trader.ambushLoc = new Vector3Int(0, -10, 0); //can't reach this loc
            }
        }

        return routePathsDict[nextPath];
    }

    //public void SetUIPersonalResourceManager(UIPersonalResourceInfoPanel uiPersonalResourceInfoPanel)
    //{
    //    //this.personalResourceManager = personalResourceManager;
    //    this.uiPersonalResourceInfoPanel = uiPersonalResourceInfoPanel;
    //}

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

    public void SetStop(ITradeStop stop)
    {
        this.stop = stop;
    }

    //public void SetCity(City city)
    //{
    //    this.city = city;
    //    PrepareResourceDictionary();
    //}

    //public void SetWonder(Wonder wonder)
    //{
    //    this.wonder = wonder;
    //    PrepareResourceDictionary();
    //}

    //public void SetTradeCenter(TradeCenter tradeCenter)
    //{
    //    this.tradeCenter = tradeCenter;
    //    PrepareResourceDictionary();
    //}

    public bool IsTC()
    {
        return stop.center != null;
    }

    private void PrepareResourceDictionary()
    {
        resourcesAtArrival = new(trader.personalResourceManager.ResourceDict);
    }

    public IEnumerator LoadUnloadCoroutine(int loadUnloadRate, bool loading)
    {
        this.loadUnloadRate = loadUnloadRate;
        bool complete = false;

        int i = 0;
        trader.personalResourceManager.DictCheck(resourceAssignments[currentStop]);

        foreach (ResourceValue value in resourceAssignments[currentStop])
        {
            currentResource = i;
            i++;
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
                    if (!stop.center.ResourceBuyDict.ContainsKey(value.resourceType))
                    {
                        InfoPopUpHandler.WarningMessage(trader.world.objectPoolItemHolder).Create(stop.center.mainLoc, "Can't buy " + value.resourceType);
                        SuddenFinish(true);
                        complete = true;
                        continue;
                    }
     //               else
     //               {
     //                   cost = Mathf.CeilToInt(tradeCenter.ResourceBuyDict[value.resourceType] * tradeCenter.multiple);
					//}
                }
                else if (resourceAmount < 0)
                {
                    if (!stop.center.ResourceSellDict.ContainsKey(value.resourceType))
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
            else if (resourceAmount > 0) //moving from city to trader
            {
                if (loading)
                {
					int space = trader.personalResourceManager.resourceStorageLimit - trader.personalResourceManager.ResourceStorageLevel;
					if (space + amountMoved < resourceAmount)
                    {
                        resourceAmount = space;
						resourceAmount += amountMoved;
					}
					resourceCurrentAmount = amountMoved;

                    if (resourceCheck)
                    {
                        if (stop.center != null)
                        {
							stop.center.SetWaiter(this, Mathf.Min(resourceAmount - amountMoved, loadUnloadRate) * Mathf.CeilToInt(stop.center.ResourceBuyDict[value.resourceType] * stop.center.multiple), true);
							trader.SetWarning(false, false, true);
						}
                        else
                        {
						    trader.SetWarning(false, false, false);
                        }

						trader.SetLoadingAnimation(false);

						yield return HoldingPatternCoroutine();
					}
                }
                else
                {
                    amountMoved = 0;
                    resourceCurrentAmount = 0;
                
                    //if trader wants more than it can store
                    int space = trader.personalResourceManager.resourceStorageLimit - trader.personalResourceManager.ResourceStorageLevel;
                    if (space < resourceAmount)
                    {
                        resourceAmount = space;

                        //for when inventory is full
                        if (resourceAmount == 0)
                        {
                            SuddenFinish(true);
                            complete = true;
                            continue;
                        }
                    }
                }

                //for when trader already has requisite amount
                if (resourceAmount == 0)
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
                    
                    if (stop.city != null)
                    {
                        resourceAmountAdjusted = Mathf.Abs(stop.city.ResourceManager.SubtractTraderResource(value.resourceType, loadUnloadRateMod));
                    }
                    else
                    {
						cost = Mathf.CeilToInt(stop.center.ResourceBuyDict[value.resourceType] * stop.center.multiple); //cost is calculated each time in case it changes while trader is there
						if (stop.center.world.CheckWorldGold(loadUnloadRateMod * cost))
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
						if (stop.city != null)
                        {
                            stop.city.ResourceManager.AddToResourceWaitList(value.resourceType, trader);
                            trader.SetWarning(false, false, false);
                        }
                        else
                        {
							stop.center.SetWaiter(this, loadUnloadRateMod * cost, false);
							trader.SetWarning(false, false, true);
						}

                        trader.SetLoadingAnimation(false);
                        isPlaying = false;

						yield return HoldingPatternCoroutine();

                        //in case deselecting while waiting
						if (!trader.resourceGridDict.ContainsKey(value.resourceType))
							trader.AddToGrid(value.resourceType);

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

                    //do it before coroutine so that numbers are accurate and can't cancel whilst occurring
                    trader.personalResourceManager.AddResource(value.resourceType, resourceAmountAdjusted);

                    if (stop.center)
                    {
                        int buyAmount = -resourceAmountAdjusted * cost;
						stop.center.tcRep.IncreasePurchasedAmount(buyAmount);
						stop.center.world.UpdateWorldGold(buyAmount);
                        InfoResourcePopUpHandler.CreateResourceStat(transform.position, buyAmount, ResourceHolder.Instance.GetIcon(ResourceType.Gold), stop.center.world);
                    }

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
                if (loading) //when loading
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
					    trader.SetWarning(true, false, false);
					    yield return HoldingPatternCoroutine();
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

				if (stop.city != null && !stop.city.resourceGridDict.ContainsKey(value.resourceType))
					stop.city.AddToGrid(value.resourceType);

				while (loadUnloadCheck)
                {
                    int loadUnloadRateMod = Mathf.Abs(Mathf.Max(resourceAmount - amountMoved, -loadUnloadRate));
                    int resourceAmountAdjusted;

                    if (stop.city != null)
                        resourceAmountAdjusted = stop.city.ResourceManager.AddTraderResource(value.resourceType, loadUnloadRateMod);
                    else if (stop.wonder != null)
                        resourceAmountAdjusted = stop.wonder.AddResource(value.resourceType, loadUnloadRateMod);
                    else
                        resourceAmountAdjusted = loadUnloadRateMod;

                    amountMoved -= resourceAmountAdjusted;
                    resourceCurrentAmount += resourceAmountAdjusted;
                    percDone = (float)resourceCurrentAmount / resourceTotalAmount;

                    if (uiTradeRouteManager.activeStatus && trader.isSelected)
                        uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[currentResource].SetAmount(percDone);

                    if (resourceAmountAdjusted == 0)
                    {
                        resourceCheck = true;
                        if (stop.city != null)
                            stop.city.ResourceManager.AddToUnloadWaitList(trader);

                        trader.SetUnloadingAnimation(false);
                        isPlaying = false;
						trader.SetWarning(true, false, false);
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

                    trader.personalResourceManager.SubtractResource(value.resourceType, resourceAmountAdjusted);

                    if (stop.center)
                    {
                        int sellAmount = resourceAmountAdjusted * stop.center.ResourceSellDict[value.resourceType];
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
        yield return new WaitWhile(() => resourceCheck);
        
        //while (resourceCheck)
        //    yield return null;
    }

    public bool ResourceWaitCheck(ResourceType type)
    {
        if (type == ResourceType.None)
        {
            int loadUnloadRateMod = Mathf.Abs(Mathf.Max(resourceAssignments[currentStop][currentResource].resourceAmount - amountMoved, -loadUnloadRate));
            if (stop.city != null && loadUnloadRateMod <= stop.city.warehouseStorageLimit - stop.city.ResourceManager.ResourceStorageLevel)
            {
                ContinueLoadingUnloading();
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
            if (stop.city != null && resourceAssignments[currentStop][currentResource].resourceType == type && resourceAssignments[currentStop][currentResource].resourceAmount >= stop.city.ResourceManager.ResourceDict[type])
            {
                ContinueLoadingUnloading();
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
		//StopCoroutine(holdingCo);
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
                    stop.city.ResourceManager.RemoveFromCityResourceWaitList(trader, tempValueList);
                }
                else
                {
                    stop.city.ResourceManager.RemoveFromUnloadWaitList(trader);
                }
            }
        }

        //resourceCheck = false;
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
        resourceCheck = false; //just in case
        trader.RemoveWarning();
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
        if (stop != null)
        {
            stop.RemoveFromWaitList(trader, stop);
            stop = null;
        }
   //     if (city != null)
   //     {
   //         if (trader.bySea)
   //             city.CheckSeaQueue();
   //         else if (trader.byAir)
   //             city.CheckAirQueue();
   //         else
   //             city.CheckQueue();

			////city = null;
   //     }
   //     else if (wonder != null)
   //     {
   //         if (trader.bySea)
			//	wonder.CheckSeaQueue();
   //         else
			//    wonder.CheckQueue();
            
   //         //wonder = null;
   //     }
   //     else if (tradeCenter != null)
   //     {
   //         if (trader.bySea)
   //             tradeCenter.CheckSeaQueue();
   //         else
			//	tradeCenter.CheckQueue();

			////tradeCenter = null;
   //     }
    }

    //public void UnloadCheck()
    //{
    //    resourceCheck = false;
    //    trader.RemoveWarning();
    //}

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
