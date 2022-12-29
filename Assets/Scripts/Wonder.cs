using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wonder : MonoBehaviour
{
    private UIWonderSelection uiWonderSelection;

    private int percentDone;
    public int PercentDone { get { return percentDone; } set { percentDone = value; } }

    [HideInInspector]
    public bool isConstructing, canBuildHarbor, hasHarbor, isActive;
    [HideInInspector]
    public Vector3Int unloadLoc, harborLoc;
    [HideInInspector]
    public Vector3 centerPos;
    [HideInInspector]
    public string wonderName;

    private List<Vector3Int> possibleHarborLocs = new();
    public List<Vector3Int> PossibleHarborLocs { get { return possibleHarborLocs; } set { possibleHarborLocs = value; } }

    private Dictionary<ResourceType, int> resourceDict = new();
    public Dictionary<ResourceType, int> ResourceDict { get { return resourceDict; } set { resourceDict = value; } }

    private Dictionary<ResourceType, int> resourceCostDict = new();
    public Dictionary<ResourceType, int> ResourceCostDict { get { return resourceCostDict; } }

    private WonderDataSO wonderData;
    public WonderDataSO WonderData { get { return wonderData; } set { wonderData = value; } }

    private int workersReceived;
    public int WorkersReceived { get { return workersReceived; } set { workersReceived = value; } }

    //for queuing unlaoding
    private Queue<Unit> waitList = new();
    private TradeRouteManager tradeRouteWaiter;
    private ResourceType resourceWaiter = ResourceType.None;

    public void SetResourceDict(List<ResourceValue> resources)
    {
        foreach (ResourceValue resource in resources)
        {
            resourceDict[resource.resourceType] = 0;
            resourceCostDict[resource.resourceType] = resource.resourceAmount;
        }
    }

    public void SetUI(UIWonderSelection uiWonderSelection)
    {
        this.uiWonderSelection = uiWonderSelection;
    }

    public bool CheckResourceType(ResourceType resourceType)
    {
        return resourceDict.ContainsKey(resourceType);
    }

    internal void SetWaiter(TradeRouteManager tradeRouteManager, ResourceType resourceType = ResourceType.None)
    {
        tradeRouteWaiter = tradeRouteManager;
        resourceWaiter = resourceType;
    }

    public void CheckResourceWaiter(ResourceType resourceType)
    {
        if (tradeRouteWaiter != null && resourceWaiter == resourceType)
        {
            tradeRouteWaiter.resourceCheck = false;
            tradeRouteWaiter = null;
            resourceWaiter = ResourceType.None;
        }
    }

    public void CheckLimitWaiter()
    {
        if (tradeRouteWaiter != null && resourceWaiter == ResourceType.None)
        {
            tradeRouteWaiter.resourceCheck = false;
            tradeRouteWaiter = null;
        }
    }

    public void AddToWaitList(Unit unit)
    {
        if (!waitList.Contains(unit))
            waitList.Enqueue(unit);
    }

    internal void CheckQueue()
    {
        if (waitList.Count > 0)
        {
            waitList.Dequeue().MoveUpInLine();
        }

        if (waitList.Count > 0)
        {
            foreach (Unit unit in waitList)
            {
                unit.MoveUpInLine();
            }
        }
    }

    internal int CheckResource(ResourceType resourceType, int newResourceAmount)
    {
        if (resourceDict.ContainsKey(resourceType) && CheckStorageSpaceForResource(resourceType, newResourceAmount))
        {
            return AddResourceToStorage(resourceType, newResourceAmount);
        }
        else
        {
            InfoPopUpHandler.Create(centerPos, "No storage space for " + resourceType);
            return 0;
        }
    }

    private bool CheckStorageSpaceForResource(ResourceType resourceType, int resourceAmount)
    {
        return resourceDict[resourceType] < resourceCostDict[resourceType];
    }

    private int AddResourceToStorage(ResourceType resourceType, int resourceAmount)
    {
        //check to ensure you don't take out more resources than are available in dictionary
        if (resourceAmount < 0 && -resourceAmount > resourceDict[resourceType])
        {
            resourceAmount = -resourceDict[resourceType];
        }

        int resourceLimit = resourceCostDict[resourceType];

        //adjusting resource amount to move based on how much space is available
        int newResourceAmount = resourceAmount;
        int newResourceBalance = (resourceDict[resourceType] + newResourceAmount) - resourceLimit;

        if (newResourceBalance >= 0)
        {
            newResourceAmount -= newResourceBalance;
        }

        resourceDict[resourceType] += newResourceAmount; //updating the dictionary

        if (isActive)
            uiWonderSelection.UpdateUI(resourceType, resourceDict[resourceType], resourceLimit);
        
        if (newResourceAmount > 0)
            CheckResourceWaiter(resourceType);
        else if (newResourceAmount < 0)
            CheckLimitWaiter();

        return resourceAmount;
    }
}
