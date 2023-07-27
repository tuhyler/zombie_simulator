using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TradeRouteManager : MonoBehaviour
{
    [HideInInspector]
    public int startingStop;
    [HideInInspector]
    public List<Vector3Int> cityStops = new();
    [HideInInspector]
    public List<List<ResourceValue>> resourceAssignments;
    [HideInInspector]
    public List<int> waitTimes;

    [HideInInspector]
    public int currentStop = 0, currentResource = 0, resourceCurrentAmount, resourceTotalAmount;
    private Vector3Int currentDestination;
    public Vector3Int CurrentDestination { get { return currentDestination; } }

    private PersonalResourceManager personalResourceManager;
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
    private WaitForSeconds resourceWait = new WaitForSeconds(1);
    private WaitForSeconds loadWait;// = new WaitForSeconds(1);

    private void Awake()
    {
        trader = GetComponent<Trader>();
        loadWait = new WaitForSeconds(secondIntervals);
    }

    public void SetTradeRoute(int startingStop, List<Vector3Int> cityStops, List<List<ResourceValue>> resourceAssignments, List<int> waitTimes)
    {
        this.startingStop = startingStop;
        this.cityStops = cityStops;
        this.resourceAssignments = resourceAssignments;
        this.waitTimes = waitTimes;
        currentStop = startingStop;
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

    public void SetUIPersonalResourceManager(UIPersonalResourceInfoPanel uiPersonalResourceInfoPanel)
    {
        //this.personalResourceManager = personalResourceManager;
        this.uiPersonalResourceInfoPanel = uiPersonalResourceInfoPanel;
    }

    public void SetPersonalResourceManager(PersonalResourceManager personalResourceManager)
    {
        this.personalResourceManager = personalResourceManager;
    }

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
        resourcesAtArrival = new(personalResourceManager.ResourceDict);
    }

    public IEnumerator LoadUnloadCoroutine(int loadUnloadRate)
    {
        this.loadUnloadRate = loadUnloadRate;
        bool complete = false;

        int i = 0;
        foreach (ResourceValue value in resourceAssignments[currentStop])
        {
            currentResource = i;
            i++;
            resourceTotalAmount = Mathf.Abs(value.resourceAmount);
            int resourceAmount = value.resourceAmount;
            bool loadUnloadCheck = true;
            if (uiTradeRouteManager.activeStatus && trader.isSelected)
            {
                uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[currentResource].completeImage.gameObject.SetActive(true);
                uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[currentResource].SetCompletePerc(0, resourceTotalAmount);
            }

            if (wonder != null)
            {
                if (!wonder.CheckResourceType(value.resourceType))
                {
                    InfoPopUpHandler.WarningMessage().Create(wonder.centerPos, "Wrong resource type: " + value.resourceType);
                    if (uiTradeRouteManager.activeStatus && trader.isSelected)
                        uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[currentResource].SetCompleteFull();
                    complete = true;
                    continue;
                }
                else if (resourceAmount > 0)
                {
                    InfoPopUpHandler.WarningMessage().Create(wonder.centerPos, "Can't move from wonder");
                    if (uiTradeRouteManager.activeStatus && trader.isSelected)
                        uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[currentResource].SetCompleteFull();
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
                        if (uiTradeRouteManager.activeStatus && trader.isSelected)
                            uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[currentResource].SetCompleteFull();
                        complete = true;
                        continue;
                    }
                }
                else if (resourceAmount < 0)
                {
                    if (!tradeCenter.ResourceSellDict.ContainsKey(value.resourceType))
                    {
                        InfoPopUpHandler.WarningMessage().Create(tradeCenter.mainLoc, "Can't sell " + value.resourceType);
                        if (uiTradeRouteManager.activeStatus && trader.isSelected)
                            uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[currentResource].SetCompleteFull();
                        complete = true;
                        continue;
                    }
                }
            }

            if (resourceAmount == 0)
            {
                if (uiTradeRouteManager.activeStatus && trader.isSelected)
                    uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[currentResource].SetCompleteFull();
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
                int level = Mathf.CeilToInt(trader.personalResourceManager.GetResourceStorageLevel);
                int limit = trader.cargoStorageLimit;
                int currentAmount = trader.personalResourceManager.ResourceDict[value.resourceType];
                resourceAmount -= currentAmount;
                if (limit - level < resourceAmount)
                {
                    resourceAmount = limit - level;
                    resourceTotalAmount = resourceAmount;
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
                    if (uiTradeRouteManager.activeStatus && trader.isSelected)
                        uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[currentResource].SetAmount(resourceCurrentAmount, resourceTotalAmount);

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

                    yield return loadWait;
                    personalResourceManager.CheckResource(value.resourceType, resourceAmountAdjusted);

                    if (trader.isSelected)
                    {
                        uiPersonalResourceInfoPanel.UpdateResource(value.resourceType, personalResourceManager.GetResourceDictValue(value.resourceType));
                        uiPersonalResourceInfoPanel.UpdateStorageLevel(personalResourceManager.GetResourceStorageLevel);
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

                complete = true;
            }
            else if (resourceAmount < 0) //moving from trader to city
            {
                //if trader holds less than what is asked to be dropped off
                int remainingWithTrader = personalResourceManager.GetResourceDictValue(value.resourceType);
                if (remainingWithTrader < Mathf.Abs(resourceAmount))
                {
                    resourceAmount = -remainingWithTrader;
                    resourceTotalAmount = remainingWithTrader;
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
                    if (uiTradeRouteManager.activeStatus && trader.isSelected)
                        uiTradeRouteManager.tradeStopHandlerList[currentStop].uiResourceTasks[currentResource].SetAmount(resourceCurrentAmount, resourceTotalAmount);

                    //if (resourceAmountAdjusted != 0)
                    //    trader.SetUnloadingAnimation(true);
                    //else
                    //    trader.SetUnloadingAnimation(false);

                    if (resourceAmountAdjusted == 0)
                    {
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

                    yield return loadWait;

                    personalResourceManager.CheckResource(value.resourceType, -resourceAmountAdjusted);

                    if (trader.isSelected)
                    {
                        uiPersonalResourceInfoPanel.UpdateResource(value.resourceType, personalResourceManager.GetResourceDictValue(value.resourceType));
                        uiPersonalResourceInfoPanel.UpdateStorageLevel(personalResourceManager.GetResourceStorageLevel);
                    }
                    if (resourceAmountAdjusted == 0)
                        resourceCheck = true;
                    else if (tradeCenter)
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

                complete = true;
            }
        }

        if (complete)
        {
            trader.SetLoadingAnimation(false);
            trader.SetUnloadingAnimation(false);
            FinishLoading();
        }
    }

    public IEnumerator WaitTimeCoroutine()
    {
        if (uiTradeRouteManager.activeStatus && trader.isSelected)
        {
            uiTradeRouteManager.tradeStopHandlerList[currentStop].progressBarHolder.SetActive(true);
            uiTradeRouteManager.tradeStopHandlerList[currentStop].SetProgressBarMask(0, waitTime);
            uiTradeRouteManager.tradeStopHandlerList[currentStop].SetTime(0, waitTime, true);
        }
        
        if (waitTime < 0)
        {
            waitForever = true;
            
            while (waitForever)
            {
                yield return new WaitForSeconds(secondIntervals);
                timeWaited += secondIntervals;
                if (uiTradeRouteManager.activeStatus && trader.isSelected)
                    uiTradeRouteManager.tradeStopHandlerList[currentStop].SetTime(timeWaited, waitTime, true);
                //Debug.Log("waited " + timeWaited + " seconds");
            }

            yield break; //this is enough, just using while loop to count seconds
        }

        while (timeWaited < waitTime)
        {
            yield return new WaitForSeconds(secondIntervals);
            timeWaited += secondIntervals;
            if (uiTradeRouteManager.activeStatus && trader.isSelected)
                uiTradeRouteManager.tradeStopHandlerList[currentStop].SetTime(timeWaited, waitTime, false);
            //Debug.Log("waited " + timeWaited + " seconds");
        }

        FinishLoading();
    }

    //for waiting for resources to arrive to load
    private IEnumerator HoldingPatternCoroutine()
    {
        while (resourceCheck)
        {
            yield return resourceWait;        
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
            uiTradeRouteManager.tradeStopHandlerList[currentStop].SetAsComplete();

            if (currentStop + 1 == cityStops.Count) //if last stop
            {
                uiTradeRouteManager.tradeStopHandlerList[0].SetAsCurrent();
                for (int i = 1; i < cityStops.Count; i++)
                {
                    uiTradeRouteManager.tradeStopHandlerList[i].SetAsNext();
                }
            }
            else
            {
                uiTradeRouteManager.tradeStopHandlerList[currentStop + 1].SetAsCurrent();
            }
        }

        //Debug.Log("Finished loading");
        resourceCheck = false;
        waitForever=false;
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
