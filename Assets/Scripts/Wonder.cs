using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wonder : MonoBehaviour
{
    private MapWorld world;
    private UIWonderSelection uiWonderSelection;
    [HideInInspector]
    public UIPersonalResourceInfoPanel uiCityResourceInfoPanel;
    public MeshRenderer mesh0Percent;
    public MeshRenderer mesh25Percent;
    public MeshRenderer mesh50Percent;
    public MeshRenderer mesh75Percent;
    public MeshRenderer meshComplete;
    //public MeshRenderer currentMesh;
    
    private int percentDone;
    public int PercentDone { get { return percentDone; } set { percentDone = value; } }

    //private Quaternion rotation;
    //public Quaternion Rotation { set { rotation = value; } }

    [HideInInspector]
    public bool isConstructing, canBuildHarbor, hasHarbor, isActive, roadPreExisted;
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

    private Dictionary<ResourceType, int> resourceGridDict = new(); //order of resources shown
    public Dictionary<ResourceType, int> ResourceGridDict { get { return resourceGridDict; } set { resourceGridDict = value; } }

    private WonderDataSO wonderData;
    public WonderDataSO WonderData { get { return wonderData; } set { wonderData = value; } }

    private int workersReceived;
    public int WorkersReceived { get { return workersReceived; } set { workersReceived = value; } }

    //for building the wonder
    private Dictionary<ResourceType, int> resourceThreshold = new();
    //private TimeProgressBar timeProgressBar;
    private UITimeProgressBar uiTimeProgressBar;
    private Coroutine buildingCo;
    private int totalTime;
    private int timePassed;
    private bool isBuilding;

    //for queuing unloading
    private Queue<Unit> waitList = new();
    private TradeRouteManager tradeRouteWaiter;
    private ResourceType resourceWaiter = ResourceType.None;

    private void Awake()
    {
        uiTimeProgressBar = Instantiate(GameAssets.Instance.uiTimeProgressPrefab, transform.position, Quaternion.Euler(90, 0, 0)).GetComponent<UITimeProgressBar>();
        isConstructing = true;


    }

    public void SetPrefabs()
    {
        //mesh0Percent = wonderData.mesh0Percent;
        //currentMesh = mesh0Percent;
        //mesh25Percent = wonderData.mesh25Percent;
        mesh25Percent.enabled = false;
        //mesh25Percent.
        //mesh50Percent = wonderData.mesh50Percent;
        mesh50Percent.enabled = false;
        //mesh75Percent = wonderData.mesh75Percent;
        mesh75Percent.enabled = false;
        //meshComplete = wonderData.meshComplete;
        meshComplete.enabled = false;
    }

    public void SetCenterPos(Vector3 centerPos)
    {
        this.centerPos = centerPos;
        uiTimeProgressBar.gameObject.transform.position = centerPos;
        totalTime = wonderData.buildTimePerPercent;
        uiTimeProgressBar.SetTimeProgressBarValue(totalTime);

    }

    public void SetResourceDict(List<ResourceValue> resources)
    {
        foreach (ResourceValue resource in resources)
        {
            resourceDict[resource.resourceType] = 0;
            resourceCostDict[resource.resourceType] = resource.resourceAmount;
            resourceGridDict[resource.resourceType] = resourceDict.Count;
            SetNextResourceThreshold(resource.resourceType);
        }
    }

    private void SetNextResourceThreshold(ResourceType resourceType)
    {
        resourceThreshold[resourceType] = (percentDone + 1) * Mathf.RoundToInt(resourceCostDict[resourceType] * 0.01f);
    }

    private void IncreasePercentDone()
    {
        percentDone++;
        if (percentDone == 100)
            return;

        foreach (ResourceType resourceType in resourceDict.Keys)
            SetNextResourceThreshold(resourceType);
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

    //public void CheckLimitWaiter()
    //{
    //    if (tradeRouteWaiter != null && resourceWaiter == ResourceType.None)
    //    {
    //        tradeRouteWaiter.resourceCheck = false;
    //        tradeRouteWaiter = null;
    //    }
    //}

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

    internal int CheckResource(ResourceType type, int newResourceAmount)
    {
        if (resourceDict.ContainsKey(type) && CheckStorageSpaceForResource(type, newResourceAmount))
        {
            if (!resourceGridDict.ContainsKey(type))
                AddToGrid(type);

            return AddResourceToStorage(type, newResourceAmount);
        }
        else
        {
            InfoPopUpHandler.WarningMessage().Create(centerPos, "No storage space for " + type);
            return 0;
        }
    }

    private void AddToGrid(ResourceType type)
    {
        resourceGridDict[type] = resourceGridDict.Count;
    }

    private bool CheckStorageSpaceForResource(ResourceType resourceType, int resourceAmount)
    {
        return resourceDict[resourceType] < resourceCostDict[resourceType];
    }

    private int AddResourceToStorage(ResourceType resourceType, int resourceAmount)
    {
        //check to ensure you don't take out more resources than are available in dictionary
        //if (resourceAmount < 0 && -resourceAmount > resourceDict[resourceType])
        //{
        //    resourceAmount = -resourceDict[resourceType];
        //}

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

        if (uiCityResourceInfoPanel)
            uiCityResourceInfoPanel.UpdateResourceInteractable(resourceType, resourceDict[resourceType], false);

        if (newResourceAmount > 0)
            CheckResourceWaiter(resourceType);
        //else if (newResourceAmount < 0)
        //    CheckLimitWaiter();

        if (!isBuilding)
            ThresholdCheck();
        return resourceAmount;
    }

    public void AddWorker()
    {
        workersReceived++;

        if (!StillNeedsWorkers())
            ThresholdCheck();
    }

    public bool StillNeedsWorkers()
    {
        return workersReceived < wonderData.workersNeeded;
    }

    //private void ThresholdUpdate(ResourceType resourceType)
    //{
    //    int nextLevel = (percentDone + 1) * resourceThreshold[resourceType];
    //    resourceThresholdMet[resourceType] = resourceDict[resourceType] / nextLevel >= 1;

    //    ThresholdCheck();
    //}

    public void ThresholdCheck()
    {
        foreach (ResourceType type in resourceThreshold.Keys)
        {
            if (resourceDict[type] < resourceThreshold[type])
                return;
        }

        if (StillNeedsWorkers())
            return;

        if (!world.CheckWorldGold(wonderData.workerCost * workersReceived))
        {
            world.AddToGoldWonderWaitList(this);
            return;
        }

        buildingCo = StartCoroutine(BuildNextPortionOfWonder());
    }

    public IEnumerator BuildNextPortionOfWonder()
    {
        timePassed = totalTime;
        
        if (isActive)
        {
            uiTimeProgressBar.gameObject.SetActive(true);
            uiTimeProgressBar.SetToZero();
            uiTimeProgressBar.SetTime(timePassed);
        }

        isBuilding = true;

        ConsumeWorkerCost();

        while (timePassed > 0)
        {
            yield return new WaitForSeconds(1);
            timePassed--;
            if (isActive)
                uiTimeProgressBar.SetTime(timePassed);
        }

        isBuilding = false;
        if (isActive)
            uiTimeProgressBar.gameObject.SetActive(false);
        IncreasePercentDone();
        if (isActive)
            uiWonderSelection.UpdateUIPercent(percentDone);
        NextPhaseCheck();
    }

    private void NextPhaseCheck()
    {
        if (percentDone == 25)
        {
            SetNewGO(mesh0Percent, mesh25Percent);
        }
        else if (percentDone == 50)
        {
            SetNewGO(mesh25Percent, mesh50Percent);
        }
        else if (percentDone == 75)
        {
            SetNewGO(mesh50Percent, mesh75Percent);
        }
        else if (percentDone == 100)
        {
            SetNewGO(mesh75Percent,meshComplete);
            isConstructing = false;
            world.RemoveWonderName(wonderName);
            if (isActive)
                uiWonderSelection.HideCancelConstructionButton();
            world.RemoveTradeLoc(unloadLoc);

            if (hasHarbor)
            {
                GameObject harbor = world.GetStructure(harborLoc);
                Destroy(harbor);
                world.RemoveSingleBuildFromCityLabor(harborLoc);
                world.RemoveStructure(harborLoc);
                world.RemoveTradeLoc(harborLoc);
            }
        }

        ThresholdCheck();
    }

    private void SetNewGO(MeshRenderer prevMesh, MeshRenderer newMesh)
    {
        prevMesh.enabled = false;
        newMesh.enabled = true;
        //currentMesh = newMesh;

        //GameObject wonderGO = Instantiate(newGO, centerPos, rotation);
        //GameObject priorGO = world.GetStructure(wonderLocs[0]);
        //Destroy(priorGO);

        //foreach (Vector3Int tile in wonderLocs)
        //{
        //    world.RemoveStructure(tile);
        //    world.AddStructure(tile, wonderGO);
        //}
    }

    public void TimeProgressBarSetActive(bool v)
    {
        if (isBuilding)
        {
            uiTimeProgressBar.gameObject.SetActive(v);
            if (v)
            {
                uiTimeProgressBar.SetProgressBarMask(timePassed);
                uiTimeProgressBar.SetTime(timePassed);
                //timeProgressBar.SetProgressBarMask();
            }
        }
    }

    private void ConsumeWorkerCost()
    {
        int amount = -wonderData.workerCost * workersReceived;
        world.UpdateWorldResources(ResourceType.Gold, amount);
        Vector3 loc = centerPos;
        loc.y += 0.5f;
        if (isActive)
            InfoResourcePopUpHandler.CreateResourceStat(loc, amount, ResourceHolder.Instance.GetIcon(ResourceType.Gold));
    }

    public void StopConstructing()
    {
        if (isBuilding)
        {
            uiTimeProgressBar.gameObject.SetActive(false);
            StopCoroutine(buildingCo);
            int amount = wonderData.workerCost * workersReceived;
            world.UpdateWorldResources(ResourceType.Gold, amount);
            InfoResourcePopUpHandler.CreateResourceStat(centerPos, amount, ResourceHolder.Instance.GetIcon(ResourceType.Gold));
            isBuilding = false;
        }
    }
}
