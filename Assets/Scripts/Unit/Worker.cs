using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Worker : Unit
{
    //[HideInInspector]
    //public bool harvested;// harvesting, resourceIsNotNull;
    private ResourceIndividualHandler resourceIndividualHandler;
    private WorkerTaskManager workerTaskManager;
    private Vector3Int resourceCityLoc;
    private Resource resource;
    //private TimeProgressBar timeProgressBar;
    private UITimeProgressBar uiTimeProgressBar;
    private List<Vector3Int> orderList = new();
    public List<Vector3Int> OrderList { get { return orderList; } }
    private Queue<Vector3Int> orderQueue = new();
    [HideInInspector]
    public bool removing;

    private void Awake()
    {
        AwakeMethods();
        isWorker = true;
        workerTaskManager = FindObjectOfType<WorkerTaskManager>();
        resourceIndividualHandler = FindObjectOfType<ResourceIndividualHandler>();
        //timeProgressBar = Instantiate(GameAssets.Instance.timeProgressPrefab, transform.position, Quaternion.Euler(90, 0, 0)).GetComponent<TimeProgressBar>();
        uiTimeProgressBar = Instantiate(GameAssets.Instance.uiTimeProgressPrefab, transform.position, Quaternion.Euler(90, 0, 0)).GetComponent<UITimeProgressBar>();
    }

    protected override void AwakeMethods()
    {
        base.AwakeMethods();
    }

    public override void SendResourceToCity()
    {
        //isBusy = false;
        //(City city, ResourceIndividualSO resourceIndividual) = resourceIndividualHandler.GetResourceGatheringDetails();
        
        resource.SendResourceToCity();
        //resourceIndividualHandler.ResetHarvestValues();
        //resourceIndividualHandler = null;
    }

    //public void PrepResourceGathering(ResourceIndividualHandler resourceIndividualHandler)
    //{
    //    this.resourceIndividualHandler = resourceIndividualHandler;
    //}
    public bool AddToOrderQueue(Vector3Int roadLoc)
    {
        if (orderList.Contains(roadLoc))
        {
            orderList.Remove(roadLoc);
            return false;
        }
        else
        {
            orderList.Add(roadLoc);
            return true;
        }
    }

    public bool MoreOrdersToFollow()
    {
        return orderQueue.Count > 0;
    }

    public void ResetOrderQueue()
    {
        orderList.Clear();
        orderQueue.Clear();
    }

    public bool IsOrderListMoreThanZero()
    {
        return orderList.Count > 0;
    }

    public void WorkerOrdersPreparations()
    {
        //Vector3 workerPos = transform.position;
        //Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);
        FinishedMoving.RemoveListener(BuildRoad);

        //if (world.IsRoadOnTerrain(workerTile))
        //{
        //    InfoPopUpHandler.Create(workerPos, "Already road on tile");
        //    return;
        //}
        //else if (world.IsBuildLocationTaken(workerTile))
        //{
        //    InfoPopUpHandler.Create(workerPos, "Already something here");
        //    return;
        //}

        StopMovement();
        //isBusy = true;
        //workerTaskManager.BuildRoad(workerTile, this);
    }

    public void SetRoadRemovalQueue()
    {
        if (orderList.Count > 0)
        {
            orderQueue = new Queue<Vector3Int>(orderList);
            //orderList.Clear();
            BeginRoadRemoval();
        }
        else
        {
            isBusy = false;
            if (isSelected)
                workerTaskManager.TurnOffCancelTask();
        }
    }

    public void BeginRoadRemoval()
    {        
        if (world.RoundToInt(transform.position) == orderQueue.Peek())
        {
            RemoveRoad();
        }
        else
        {
            FinishedMoving.AddListener(RemoveRoad);
            workerTaskManager.MoveToCompleteOrders(orderQueue.Peek(), this);
        }
    }

    public void SkipRoadRemoval()
    {
        if (MoreOrdersToFollow())
        {
            BeginRoadRemoval();
        }
        else
        {
            isBusy = false;

            if (isSelected)
                workerTaskManager.TurnOffCancelTask();
        }
    }

    public void SetRoadQueue()
    {
        if (orderList.Count > 0)
        {
            orderQueue = new Queue<Vector3Int>(orderList);
            //orderList.Clear();
            BeginBuildingRoad();
        }
        else
        {
            isBusy = false;
            if (isSelected)
                workerTaskManager.TurnOffCancelTask();
        }
    }

    public void BeginBuildingRoad()
    {
        if (world.RoundToInt(transform.position) == orderQueue.Peek())
        {
            BuildRoad();
        }
        else
        {
            FinishedMoving.AddListener(BuildRoad);
            workerTaskManager.MoveToCompleteOrders(orderQueue.Peek(), this);
        }
    }

    public override void SkipRoadBuild()
    {
        if (MoreOrdersToFollow())
        {
            BeginBuildingRoad();
        }
        else
        {
            isBusy = false;

            if (isSelected)
                workerTaskManager.TurnOffCancelTask();
        }
    }

    private void BuildRoad()
    {
        Vector3Int workerTile = orderQueue.Dequeue();
        orderList.Remove(workerTile);

        FinishedMoving.RemoveListener(BuildRoad);
        //Vector3 workerPos = transform.position;
        //Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);
        workerTaskManager.BuildRoad(workerTile, this);
        world.GetTerrainDataAt(workerTile).DisableHighlight();
    }

    public void RemoveRoad()
    {
        Vector3Int workerTile = orderQueue.Dequeue();
        orderList.Remove(workerTile);

        //Vector3 workerPos = transform.position;
        //Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);
        FinishedMoving.RemoveListener(RemoveRoad);

        world.GetTerrainDataAt(workerTile).DisableHighlight();
        foreach (GameObject go in world.GetAllRoadsOnTile(workerTile))
        {
            if (go == null)
                continue;
            go.GetComponent<SelectionHighlight>().DisableHighlight();
        }

        if (!world.IsRoadOnTerrain(workerTile))
        {
            InfoPopUpHandler.Create(workerTile, "No road here");
            return;
        }

        if (world.IsCityOnTile(workerTile) || world.IsWonderOnTile(workerTile))
        {
            InfoPopUpHandler.Create(workerTile, "Can't remove this");
            return;
        }

        //StopMovement();
        //isBusy = true;
        workerTaskManager.RemoveRoad(workerTile, this);
    }

    public void GatherResource()
    {
        Vector3 workerPos = transform.position;
        workerPos.y = 0;
        Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);

        if (!world.IsTileOpenCheck(workerTile))
        {
            InfoPopUpHandler.Create(workerPos, "Harvest on open tile");
            return;
        }

        if (!CheckForCity(workerTile))
        {
            InfoPopUpHandler.Create(workerPos, "No nearby city");
            return;
        }

        City city = world.GetCity(resourceCityLoc);
        ResourceIndividualSO resourceIndividual = resourceIndividualHandler.GetResourcePrefab(workerTile);
        if (resourceIndividual == null)
        {
            InfoPopUpHandler.Create(workerPos, "No resource to harvest here");
            return;
        }
        //Debug.Log("Harvesting resource at " + workerPos);

        //resourceIndividualHandler.GenerateHarvestedResource(workerPos, workerUnit);
        StopMovement();
        isBusy = true;
        //resourceIndividualHandler.SetWorker(this);
        workerTaskManager.GatherResource(workerPos, this, city, resourceIndividual);
    }

    public void BuildCity()
    {
        Vector3 workerPos = transform.position;
        Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);
        FinishedMoving.RemoveListener(BuildCity);

        StopMovement();
        isBusy = true;
        workerTaskManager.BuildCityPreparations(workerTile, this);
    }

    public bool CheckForCity(Vector3Int workerPos) //finds if city is nearby, returns it (interface? in WorkerTaskManager)
    {
        foreach (Vector3Int tile in world.GetNeighborsFor(workerPos, MapWorld.State.CITYRADIUS))
        {
            if (!world.IsCityOnTile(tile))
            {
                continue;
            }
            else
            {
                resourceCityLoc = tile;
                return true;
            }
        }

        return false;
    }

    public void SetResource(Resource resource)
    {
        this.resource = resource;
    }

    public void ShowProgressTimeBar(int time)
    {
        Vector3 pos = transform.position;
        pos.z += -1f;
        uiTimeProgressBar.gameObject.transform.position = pos;
        //timeProgressBar.SetConstructionTime(time);
        //timeProgressBar.SetProgressBarBeginningPosition();
        uiTimeProgressBar.SetTimeProgressBarValue(time);
        uiTimeProgressBar.SetToZero();
        uiTimeProgressBar.gameObject.SetActive(true);
    }

    public void HideProgressTimeBar()
    {
        uiTimeProgressBar.gameObject.SetActive(false);
    }

    public void SetTime(int time)
    {
        uiTimeProgressBar.SetTime(time);
    }

    //public IEnumerator BuildRoad(Vector3Int roadPosition, RoadManager roadManager)
    //{
    //    int timePassed = 0;
    //    Debug.Log("building road at " + roadPosition);

    //    while (timePassed < roadManager.roadBuildingTime)
    //    {
    //        yield return new WaitForSeconds(1);
    //        timePassed++;
    //    }

    //    isBusy = false;
    //    workerTaskManager.TurnOffCancelTask();
    //    roadManager.BuildRoadAtPosition(roadPosition);
    //}

    //private void HarvestResource()
    //{
    //    if (harvesting)
    //    {
    //        harvesting = false;
    //        harvested = true;
    //        resourceIsNotNull = false;
    //        resourceIndividualHandler.SetResourceActive();
    //    }
    //}

    //protected override void WaitTurnMethods()
    //{
    //    base.WaitTurnMethods();
    //    if (harvesting && resourceIsNotNull)
    //        HarvestResource();
    //}

    //public new void WaitTurn()
    //{
    //    WaitTurnMethods();
    //}
}
