using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkerTaskManager : MonoBehaviour
{
    //[SerializeField]
    //public UIWorkerHandler workerTaskUI;

    private Worker workerUnit; //The only unit that can build

    //[SerializeField]
    //private AudioSource audioSource; //do audio later

    [SerializeField]
    private MapWorld world;
    [SerializeField]
    private InfoManager infoManager;
    [SerializeField]
    private MovementSystem movementSystem;
    [SerializeField]
    private UnitMovement unitMovement;
    [SerializeField]
    private ImprovementDataSO cityData;
    [SerializeField]
    private UISingleConditionalButtonHandler uiCancelTask;

    private ResourceIndividualHandler resourceIndividualHandler;
    private RoadManager roadManager;

    private Coroutine taskCoroutine;

    private int cityBuildingTime = 1;

    private void Awake()
    {
        //audioSource = GetComponent<AudioSource>();
        resourceIndividualHandler = GetComponent<ResourceIndividualHandler>();
        roadManager = GetComponent<RoadManager>();
    }

    //Methods to run when pressing certain keys
    public void HandleR()
    {
        if (workerUnit != null && !workerUnit.isBusy)
        {
            Vector3 pos = workerUnit.transform.position;
            Vector3Int workerTile = world.GetClosestTerrainLoc(pos);

            if (Vector3Int.RoundToInt(pos) == workerTile)
            {
                //add to finish animation listener
                workerUnit.BuildRoad(); 
            }
            else
            {
                workerUnit.FinishedMoving.AddListener(workerUnit.BuildRoad);
                MoveToCenterOfTile(workerTile);
            }
        }
    }

    public void HandleB()
    {
        if (workerUnit != null && !workerUnit.isBusy)
        {
            Vector3 pos = workerUnit.transform.position;
            Vector3Int workerTile = world.GetClosestTerrainLoc(pos);

            if (Vector3Int.RoundToInt(pos) == workerTile)
            {
                //add to finish animation listener
                workerUnit.BuildCity();
            }
            else
            {
                MoveToCenterOfTile(workerTile);
                workerUnit.FinishedMoving.AddListener(workerUnit.BuildCity);
            }
        }
    }

    public void HandleG()
    {
        if (workerUnit != null && !workerUnit.isBusy)
        {
            //Vector3 pos = workerUnit.transform.position;
            //Vector3Int workerTile = world.GetClosestTerrainLoc(pos);

            workerUnit.StopMovement();
            workerUnit.GatherResource();

            //if (Vector3Int.RoundToInt(pos) == workerTile)
            //{
            //    //add to finish animation listener
            //    GatherResource();
            //}
            //else
            //{
            //    workerUnit.StopMovement();
            //    workerUnit.isBusy = true;
            //    unitMovement.HandleSelectedLocation(pos, workerTile);
            //    workerUnit.FinishedMoving.AddListener(GatherResource);
            //}
        }
    }

    public void HandleX()
    {
        if (workerUnit != null && !workerUnit.isBusy)
        {
            Vector3 pos = workerUnit.transform.position;
            Vector3Int workerTile = world.GetClosestTerrainLoc(pos);

            if (Vector3Int.RoundToInt(pos) == workerTile)
            {
                //add to finish animation listener
                workerUnit.RemoveRoad();
            }
            else
            {
                MoveToCenterOfTile(workerTile);
                workerUnit.FinishedMoving.AddListener(workerUnit.RemoveRoad);
            }
        }
    }

    private void MoveToCenterOfTile(Vector3Int workerTile)
    {
        workerUnit.StopMovement();
        unitMovement.HandleSelectedLocation(workerTile, workerTile);
    }

    public void SetWorkerUnit(Worker workerUnit)
    {
        this.workerUnit = workerUnit;
        if (workerUnit.isBusy)
            uiCancelTask.ToggleTweenVisibility(true);
    }

    //public void HandleCitySelection(GameObject selectedObject)
    //{
    //    if (selectedObject == null)
    //        return;

    //    //for deselecting unit
    //    //Unit unitReference = selectedObject.GetComponent<Unit>();
    //    //if (CheckIfSameUnitSelected(unitReference) && unitReference != null)
    //    //{
    //    //    //workerTaskUI.ToggleVisibility(false);
    //    //    workerUnit = null;
    //    //    return;
    //    //}

    //    workerUnit = null;

    //    workerUnit = selectedObject.GetComponent<Worker>(); //checks if unit is worker

    //    if (workerUnit != null)
    //    {
    //        //HandleUnitSelection();
    //    }
    //}

    //public void HandleUnitSelection(Unit unit) //for unit turn handler
    //{
    //    if (workerUnit != null)
    //    {
    //        workerUnit = null;
    //    }
    //    workerUnit = unit.GetComponent<Worker>();
    //    if (workerUnit != null)
    //    {
    //        HandleUnitSelection();
    //    }
    //}

    //private void HandleUnitSelection()
    //{
    //    //workerUnit = worker.GetComponent<Unit>();
    //    if (workerUnit.harvested) //if unit just finished harvesting something, can't move until resource is sent to city
    //        workerUnit.SendResourceToCity();
    //    //if (workerUnit != null) //Because warrior doesn't have worker component, it won't see the build UI
    //    //{
    //    //    workerTaskUI.ToggleVisibility(true);
    //    //    //workerUnit.FinishedMoving.AddListener(ResetWorkerTaskSystem); //hides the UI here without needing to connect to unit class
    //    //}
    //}

    //private bool CheckIfSameUnitSelected(Unit unitReference) //for deselection
    //{
    //    return workerUnit == unitReference;
    //}

    public void PerformTask(ImprovementDataSO improvementData) //don't actually use "improvementData".
    {
        if (workerUnit.isBusy)
            return;
        
        Vector3 pos = workerUnit.transform.position;
        Vector3Int workerTile = world.GetClosestTerrainLoc(pos);

        if (improvementData == null) //removing the road
        {
            if (Vector3Int.RoundToInt(pos) == workerTile)
            {
                //add to finish animation listener
                workerUnit.RemoveRoad();
            }
            else
            {
                workerUnit.FinishedMoving.AddListener(workerUnit.RemoveRoad);
                MoveToCenterOfTile(workerTile);
            }
        }

        else if (improvementData.improvementName == "Resource") //if action is to gather resources
        {
            workerUnit.GatherResource();
        }

        else if (improvementData.improvementName == "Road") //adding road
        {
            if (Vector3Int.RoundToInt(pos) == workerTile)
            {
                //add to finish animation listener
                workerUnit.BuildRoad();
            }
            else
            {
                MoveToCenterOfTile(workerTile);
                workerUnit.FinishedMoving.AddListener(workerUnit.BuildRoad);
            }
        }

        else if (improvementData.improvementName == "City") //creating city
        {
            if (Vector3Int.RoundToInt(pos) == workerTile)
            {
                //add to finish animation listener
                workerUnit.BuildCity();
            }
            else
            {
                MoveToCenterOfTile(workerTile);
                workerUnit.FinishedMoving.AddListener(workerUnit.BuildCity);
            }
        }
    }

    public void RemoveRoad(Vector3Int workerTile, Worker worker)
    {
        //Vector3 workerPos = workerUnit.transform.position;
        //Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);
        //workerUnit.FinishedMoving.RemoveListener(RemoveRoad);
        
        //if (!world.IsRoadOnTerrain(workerTile))
        //{
        //    InfoPopUpHandler.Create(workerUnit.transform.position, "No road here");
        //    return;
        //}

        //if (world.IsCityOnTile(workerTile))
        //{
        //    InfoPopUpHandler.Create(workerUnit.transform.position, "Can't remove city road");
        //    return;
        //}

        //workerUnit.StopMovement();
        //workerUnit.isBusy = true;
        uiCancelTask.ToggleTweenVisibility(true);
        taskCoroutine = StartCoroutine(roadManager.RemoveRoad(workerTile, worker)); //specific worker (instead of workerUnit) to allow concurrent build
        //workerUnit.HidePath();
        //Debug.Log("Removing road at " + workerTile);
    }

    public void GatherResource(Vector3 workerPos, Worker worker, City city, ResourceIndividualSO resourceIndividual)
    {
        //Vector3 workerPos = workerUnit.transform.position;
        //workerPos.y = 0;
        //Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);
        //workerUnit.FinishedMoving.RemoveListener(GatherResource);

        //if (world.IsBuildLocationTaken(workerTile) || world.IsRoadOnTerrain(workerTile))
        //{
        //    InfoPopUpHandler.Create(workerPos, "Harvest on open tile");
        //    return;
        //}

        //if (!resourceIndividualHandler.CheckForCity(workerTile))
        //{
        //    InfoPopUpHandler.Create(workerPos, "No nearby city");
        //    return;
        //} 

        //ResourceIndividualSO resourceIndividual = resourceIndividualHandler.GetResourcePrefab(workerTile);
        //if (resourceIndividual == null)
        //{
        //    InfoPopUpHandler.Create(workerPos, "No resource to harvest here");
        //    return;
        //}
        //Debug.Log("Harvesting resource at " + workerPos);

        ////resourceIndividualHandler.GenerateHarvestedResource(workerPos, workerUnit);
        //workerUnit.StopMovement();
        //workerUnit.isBusy = true;
        //resourceIndividualHandler.SetWorker(workerUnit);
        uiCancelTask.ToggleTweenVisibility(true);
        taskCoroutine = StartCoroutine(resourceIndividualHandler.GenerateHarvestedResource(workerPos, worker, city, resourceIndividual));


        //workerUnit.HidePath();
    }

    public void BuildRoad(Vector3Int workerTile, Worker worker)
    {
        //Vector3 workerPos = workerUnit.transform.position;
        //Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);
        //workerUnit.FinishedMoving.RemoveListener(BuildRoad);

        //if (world.IsRoadOnTerrain(workerTile))
        //{
        //    InfoPopUpHandler.Create(workerPos, "Already road on tile");
        //    return;
        //}
        //if (world.IsBuildLocationTaken(workerTile))
        //{
        //    InfoPopUpHandler.Create(workerPos, "Already something here");
        //    return;
        //}

        //workerUnit.StopMovement();
        //workerUnit.isBusy = true;
        uiCancelTask.ToggleTweenVisibility(true);
        //taskCoroutine = StartCoroutine(worker.BuildRoad(workerTile, roadManager));
        taskCoroutine = StartCoroutine(roadManager.BuildRoad(workerTile, worker)); //specific worker (instead of workerUnit) to allow concurrent build
        //workerUnit.HidePath();
    }

    public void BuildCityPreparations(Vector3Int workerTile, Worker worker)
    {
        if (CheckForNearbyCity(workerTile))
            return;

        TerrainData td = world.GetTerrainDataAt(workerTile);
        bool clearForest = td.GetTerrainData().type == TerrainType.Forest;

        taskCoroutine = StartCoroutine(BuildCityCoroutine(workerTile, worker, clearForest, td));   
    }

    private IEnumerator BuildCityCoroutine(Vector3Int workerTile, Worker worker, bool clearForest, TerrainData td)
    {
        int timePassed = cityBuildingTime;
        //int timeLimit = cityBuildingTime;

        if (clearForest)
            timePassed *= 2;

        worker.ShowProgressTimeBar(timePassed);
        worker.SetTime(timePassed);

        while (timePassed > 0)
        {
            yield return new WaitForSeconds(1);
            timePassed--;
            worker.SetTime(timePassed);
        }

        worker.HideProgressTimeBar();
        TurnOffCancelTask();
        BuildCity(workerTile, worker, clearForest, td);
    }

    private void BuildCity(Vector3Int workerTile, Worker worker, bool clearForest, TerrainData td)
    {
        worker.isBusy = false;

        //clear the forest if building on forest tile
        if (clearForest)
        {
            Destroy(td.prop.GetChild(0).gameObject);
            td.terrainData = td.GetTerrainData().clearedForestData;
            td.gameObject.tag = "Flatland";
        }

        if (!world.IsRoadOnTerrain(workerTile)) //build road where city is placed
            roadManager.BuildRoadAtPosition(workerTile, true);

        //Vector3Int workerTile = Vector3Int.FloorToInt(workerPos);
        GameObject newCity = Instantiate(cityData.prefab, workerTile, Quaternion.identity); //creates building unit position.
        world.AddStructure(workerTile, newCity); //adds building location to buildingDict
        City city = newCity.GetComponent<City>();
        world.AddCity(workerTile, city);
        city.SetNewCityName();

        //ResourceProducer resourceProducer = newCity.GetComponent<ResourceProducer>();
        //world.AddResourceProducer(workerTile, resourceProducer);
        //resourceProducer.InitializeImprovementData(improvementData); //allows the new structure to also start generating resources
        //ResourceManager resourceManager = newCity.GetComponent<ResourceManager>();
        //resourceProducer.SetResourceManager(resourceManager);
        //resourceProducer.BeginResourceGeneration(); //begin generating resources
        //resourceProducer.StartProducing();
        //if (world.TileHasBuildings(workerTile)) //if tile already has buildings, need to switch resourceManager for each resourceProducer 
        //{
        //    foreach (string buildingName in world.GetBuildingListForCity(workerTile))
        //    {
        //        if (world.CheckBuildingIsProducer(workerTile, buildingName))
        //        {
        //            ResourceProducer resourceProducer = world.GetBuildingProducer(workerTile, buildingName);
        //            resourceProducer.SetResourceManager(resourceManager);
        //            resourceProducer.SetLocation(workerTile);
        //        }
        //    }
        //}
        //else //if no currently existing buildings, set up dictionaries
        //{
        world.AddCityBuildingDict(workerTile);
        city.SetHouse(workerTile);
        //}

        //showing join city button
        unitMovement.ShowIndividualCityButtonsUI();
    }

    //checking if building city too close to another one
    private bool CheckForNearbyCity(Vector3 workerPos)
    {
        Vector3Int workerTile = Vector3Int.RoundToInt(workerPos);
        foreach (Vector3Int tile in world.GetNeighborsFor(workerTile, MapWorld.State.CITYRADIUS))
        {
            if (!world.IsCityOnTile(tile))
            {
                continue;
            }
            else
            {
                InfoPopUpHandler.Create(workerUnit.transform.position, "Too close to another city");
                return true;
            }
        }

        return false;
    }

    public void CancelTask()
    {
        StopCoroutine(taskCoroutine);
        workerUnit.HideProgressTimeBar();
        workerUnit.isBusy = false;
        //resourceIndividualHandler.NullHarvestValues();
        TurnOffCancelTask();
    }

    public void TurnOffCancelTask()
    {
        uiCancelTask.ToggleTweenVisibility(false);
    }
}
