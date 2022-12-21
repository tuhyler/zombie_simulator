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
    private TimeProgressBar timeProgressBar;
    private List<Vector3Int> roadBuildList = new();
    private Queue<Vector3Int> roadBuildQueue = new();

    private void Awake()
    {
        AwakeMethods();
        isWorker = true;
        workerTaskManager = FindObjectOfType<WorkerTaskManager>();
        resourceIndividualHandler = FindObjectOfType<ResourceIndividualHandler>();
        SetProgressTimeBar();
    }

    protected override void AwakeMethods()
    {
        base.AwakeMethods();
    }

    private void SetProgressTimeBar()
    {
        //producerLoc.z -= 1.5f; //bottom center of tile
        GameObject gameObject = Instantiate(GameAssets.Instance.timeProgressPrefab, transform.position, Quaternion.Euler(90, 0, 0));
        timeProgressBar = gameObject.GetComponent<TimeProgressBar>();
        //timeProgressBar.SetTimeProgressBarValue(improvementData.producedResourceTime);
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
    public bool AddToRoadQueue(Vector3Int roadLoc)
    {
        if (roadBuildList.Contains(roadLoc))
        {
            roadBuildList.Remove(roadLoc);
            return false;
        }
        else
        {
            roadBuildList.Add(roadLoc);
            return true;
        }
    }

    public bool MoreRoadToBuild()
    {
        return roadBuildQueue.Count > 0;
    }

    public void ResetRoadQueue()
    {
        roadBuildList.Clear();
        roadBuildQueue.Clear();
    }

    public void BuildRoadPreparations()
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

    public void SetRoadQueue()
    {
        if (roadBuildList.Count > 0)
        {
            roadBuildQueue = new Queue<Vector3Int>(roadBuildList);
            roadBuildList.Clear();
            BeginBuildingRoad();
        }
        else
        {
            isBusy = false;
            workerTaskManager.TurnOffCancelTask();
        }
    }

    public void BeginBuildingRoad()
    {
        if (world.RoundToInt(transform.position) == roadBuildQueue.Peek())
        {
            roadBuildQueue.Dequeue();
            BuildRoad();
        }
        else
        {
            FinishedMoving.AddListener(BuildRoad);
            workerTaskManager.MoveToBuildRoad(roadBuildQueue.Dequeue(), this);
        }
    }

    public override void SkipRoadBuild()
    {
        if (MoreRoadToBuild())
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
        FinishedMoving.RemoveListener(BuildRoad);
        Vector3 currentPos = transform.position;
        currentPos.y = 0;
        workerTaskManager.BuildRoad(world.RoundToInt(currentPos), this);
    }

    public void RemoveRoad()
    {
        Vector3 workerPos = transform.position;
        Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);
        FinishedMoving.RemoveListener(RemoveRoad);

        if (!world.IsRoadOnTerrain(workerTile))
        {
            InfoPopUpHandler.Create(workerPos, "No road here");
            return;
        }

        if (world.IsCityOnTile(workerTile))
        {
            InfoPopUpHandler.Create(workerPos, "Can't remove city road");
            return;
        }

        StopMovement();
        isBusy = true;
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

        if (!world.IsTileOpenCheck(workerTile))
        {
            InfoPopUpHandler.Create(workerPos, "Already something here");
            return;
        }

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
        timeProgressBar.gameObject.transform.position = pos;
        //timeProgressBar.SetConstructionTime(time);
        timeProgressBar.SetTimeProgressBarValue(time);
        timeProgressBar.SetActive(true);
    }

    public void HideProgressTimeBar()
    {
        timeProgressBar.SetActive(false);
    }

    public void SetTime(int time)
    {
        timeProgressBar.SetTime(time);
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
