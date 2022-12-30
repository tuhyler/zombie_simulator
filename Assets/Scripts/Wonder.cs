using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class Wonder : MonoBehaviour
{
    private MapWorld world;
    private UIWonderSelection uiWonderSelection;

    private int percentDone;
    public int PercentDone { get { return percentDone; } set { percentDone = value; } }

    private Quaternion rotation;
    public Quaternion Rotation { set { rotation = value; } }

    [HideInInspector]
    public bool isConstructing, canBuildHarbor, hasHarbor, isActive, roadPreExisted, completed;
    [HideInInspector]
    public Vector3Int unloadLoc, harborLoc;
    [HideInInspector]
    public Vector3 centerPos;
    [HideInInspector]
    public string wonderName;

    private List<Vector3Int> wonderLocs = new();
    public List<Vector3Int> WonderLocs { get { return wonderLocs; } set { wonderLocs = value; } }

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

    //for building the wonder
    private Dictionary<ResourceType, int> resourceThreshold = new();
    private Dictionary<ResourceType, bool> resourceThresholdMet = new();
    private TimeProgressBar timeProgressBar;
    private Coroutine co;

    //for queuing unloading
    private Queue<Unit> waitList = new();
    private TradeRouteManager tradeRouteWaiter;
    private ResourceType resourceWaiter = ResourceType.None;

    private void Awake()
    {
        timeProgressBar = Instantiate(GameAssets.Instance.timeProgressPrefab, transform.position, Quaternion.Euler(90, 0, 0)).GetComponent<TimeProgressBar>();
        timeProgressBar.gameObject.transform.position = centerPos;
    }

    public void SetResourceDict(List<ResourceValue> resources)
    {
        foreach (ResourceValue resource in resources)
        {
            resourceDict[resource.resourceType] = 0;
            resourceCostDict[resource.resourceType] = resource.resourceAmount;
            resourceThreshold[resource.resourceType] = Mathf.RoundToInt(resource.resourceAmount * 0.01f);
            resourceThresholdMet[resource.resourceType] = false;
        }
    }

    public void SetWorld(MapWorld world)
    {
        this.world = world;
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

        ThresholdUpdate(resourceType);
        return resourceAmount;
    }

    public bool StillNeedsWorkers()
    {
        return workersReceived < wonderData.workerCount;
    }

    private void ThresholdUpdate(ResourceType resourceType)
    {
        int nextLevel = (percentDone + 1) * resourceThreshold[resourceType];
        resourceThresholdMet[resourceType] = resourceDict[resourceType] / nextLevel >= 1;

        ThresholdCheck();
    }

    private void ThresholdCheck()
    {
        foreach (ResourceType type in resourceThresholdMet.Keys)
        {
            if (!resourceThresholdMet[type])
                return;
        }

        if (!StillNeedsWorkers())
            co = StartCoroutine(BuildNextPortionOfWonder());
    }

    public IEnumerator BuildNextPortionOfWonder()
    {
        int timePassed = wonderData.buildTimePerPercent;
        timeProgressBar.SetActive(true);
        timeProgressBar.SetTimeProgressBarValue(timePassed);
        timeProgressBar.SetTime(timePassed);

        while (timePassed > 0)
        {
            yield return new WaitForSeconds(1);
            timePassed--;
            timeProgressBar.SetTime(timePassed);
        }

        timeProgressBar.ResetProgressBar();
        timeProgressBar.SetActive(false);
        percentDone++;
        NextPhaseCheck();
    }

    private void NextPhaseCheck()
    {
        if (percentDone == 25)
        {
            SetNewGO(wonderData.prefab25Percent);
        }
        else if (percentDone == 50)
        {
            SetNewGO(wonderData.prefab50Percent);
        }
        else if (percentDone == 75)
        {
            SetNewGO(wonderData.prefab75Percent);
        }
        else if (percentDone == 100)
        {
            SetNewGO(wonderData.prefabComplete);
            completed = true;
        }

        ThresholdCheck();
    }

    private void SetNewGO(GameObject newGO)
    {
        GameObject wonderGO = Instantiate(newGO, centerPos, rotation);
        GameObject priorGO = world.GetStructure(wonderLocs[0]);
        Destroy(priorGO);

        foreach (Vector3Int tile in wonderLocs)
            world.AddStructure(tile, wonderGO);
    }
}
