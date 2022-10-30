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
    private RoadManager roadManager;
    [SerializeField]
    private ImprovementDataSO cityData;
    [SerializeField]
    private UISingleConditionalButtonHandler uiCancelTask;

    private ResourceIndividualHandler resourceIndividualHandler;

    private Coroutine taskCoroutine;

    private void Awake()
    {
        //audioSource = GetComponent<AudioSource>();
        resourceIndividualHandler = GetComponent<ResourceIndividualHandler>();
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
                BuildRoad(); 
            }
            else
            {
                MoveToCenterOfTile(workerTile);
                workerUnit.FinishedMoving.AddListener(BuildRoad);
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
                BuildCity();
            }
            else
            {
                MoveToCenterOfTile(workerTile);
                workerUnit.FinishedMoving.AddListener(BuildCity);
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
            GatherResource();

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
                RemoveRoad();
            }
            else
            {
                MoveToCenterOfTile(workerTile);
                workerUnit.FinishedMoving.AddListener(RemoveRoad);
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
                RemoveRoad();
            }
            else
            {
                MoveToCenterOfTile(workerTile);
                workerUnit.FinishedMoving.AddListener(RemoveRoad);
            }
        }

        else if (improvementData.improvementName == "Resource") //if action is to gather resources
        {
            GatherResource();

            //if (Vector3Int.RoundToInt(pos) == workerTile)
            //{
            //    //add to finish animation listener
            //    GatherResource();
            //}
            //else
            //{
            //    workerUnit.StopMovement();
            //    workerUnit.isBusy = true;
            //    unitMovement.HandleSelectedLocation(pos, workerTile); //only one that doesn't need to centered on larger tile
            //    workerUnit.FinishedMoving.AddListener(GatherResource);
            //}
        }

        else if (improvementData.improvementName == "Road") //adding road
        {
            if (Vector3Int.RoundToInt(pos) == workerTile)
            {
                //add to finish animation listener
                BuildRoad();
            }
            else
            {
                MoveToCenterOfTile(workerTile);
                workerUnit.FinishedMoving.AddListener(BuildRoad);
            }
        }

        else if (improvementData.improvementName == "City") //creating city
        {
            if (Vector3Int.RoundToInt(pos) == workerTile)
            {
                //add to finish animation listener
                BuildCity();
            }
            else
            {
                MoveToCenterOfTile(workerTile);
                workerUnit.FinishedMoving.AddListener(BuildCity);
            }
        }
    }

    private void RemoveRoad()
    {
        Vector3 workerPos = workerUnit.transform.position;
        Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);
        workerUnit.FinishedMoving.RemoveListener(RemoveRoad);
        
        if (!world.IsRoadOnTerrain(workerTile))
        {
            InfoPopUpHandler.Create(workerUnit.transform.position, "No road here");
            return;
        }

        if (world.IsCityOnTile(workerTile))
        {
            InfoPopUpHandler.Create(workerUnit.transform.position, "Can't remove city road");
            return;
        }

        workerUnit.StopMovement();
        workerUnit.isBusy = true;
        uiCancelTask.ToggleTweenVisibility(true);
        taskCoroutine = StartCoroutine(roadManager.RemoveRoad(workerTile, workerUnit));
        //workerUnit.HidePath();
        Debug.Log("Removing road at " + workerTile);
    }

    private void GatherResource()
    {
        Vector3 workerPos = workerUnit.transform.position;
        workerPos.y = 0;
        Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);
        //workerUnit.FinishedMoving.RemoveListener(GatherResource);

        if (world.IsBuildLocationTaken(workerTile) || world.IsRoadOnTerrain(workerTile))
        {
            InfoPopUpHandler.Create(workerUnit.transform.position, "Harvest on open tile");
            return;
        }

        if (!resourceIndividualHandler.CheckForCity(workerTile))
        {
            InfoPopUpHandler.Create(workerUnit.transform.position, "No nearby city");
            return;
        } 

        ResourceIndividualSO resourceIndividual = resourceIndividualHandler.GetResourcePrefab(workerTile);
        if (resourceIndividual == null)
        {
            InfoPopUpHandler.Create(workerUnit.transform.position, "No resource to harvest here");
            return;
        }
        Debug.Log("Harvesting resource at " + workerPos);

        //resourceIndividualHandler.GenerateHarvestedResource(workerPos, workerUnit);
        workerUnit.StopMovement();
        workerUnit.isBusy = true;
        resourceIndividualHandler.SetWorker(workerUnit);
        uiCancelTask.ToggleTweenVisibility(true);
        taskCoroutine = StartCoroutine(resourceIndividualHandler.GenerateHarvestedResource(workerPos));


        //workerUnit.HidePath();
    }

    private void BuildRoad()
    {
        Vector3 workerPos = workerUnit.transform.position;
        Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);
        workerUnit.FinishedMoving.RemoveListener(BuildRoad);
        workerUnit.isBusy = false;

        if (world.IsRoadOnTerrain(workerTile))
        {
            InfoPopUpHandler.Create(workerUnit.transform.position, "Already road on tile");
            return;
        }
        if (world.IsBuildLocationTaken(workerTile))
        {
            InfoPopUpHandler.Create(workerUnit.transform.position, "Already something here");
            return;
        }

        workerUnit.StopMovement();
        workerUnit.isBusy = true;
        uiCancelTask.ToggleTweenVisibility(true);
        taskCoroutine = StartCoroutine(roadManager.BuildRoad(workerTile, workerUnit));
        //workerUnit.HidePath();
    }

    private void BuildCity()
    {
        Vector3 workerPos = workerUnit.transform.position;
        Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);
        workerUnit.FinishedMoving.RemoveListener(BuildCity);
        workerUnit.isBusy = false;

        if (world.IsBuildLocationTaken(workerTile))
        {
            InfoPopUpHandler.Create(workerUnit.transform.position, "Already something here");
            return;
        }

        if (CheckForNearbyCity(workerTile))
            return;

        //clear the forest if building on forest tile
        TerrainData td = world.GetTerrainDataAt(workerTile);
        if (td.GetTerrainData().type == TerrainType.Forest)
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
        world.AddCity(workerTile);
        City city = newCity.GetComponent<City>();
        city.SetHouse();
        city.SetNewCityName();

        //ResourceProducer resourceProducer = newCity.GetComponent<ResourceProducer>();
        //world.AddResourceProducer(workerTile, resourceProducer);
        //resourceProducer.InitializeImprovementData(improvementData); //allows the new structure to also start generating resources
        ResourceManager resourceManager = newCity.GetComponent<ResourceManager>();
        //resourceProducer.SetResourceManager(resourceManager);
        //resourceProducer.BeginResourceGeneration(); //begin generating resources
        //resourceProducer.StartProducing();
        if (world.TileHasBuildings(workerTile)) //if tile already has buildings, need to switch resourceManager for each resourceProducer 
        {
            foreach (string buildingName in world.GetBuildingListForCity(workerTile))
            {
                if (world.CheckBuildingIsProducer(workerTile, buildingName))
                {
                    ResourceProducer resourceProducer = world.GetBuildingProducer(workerTile, buildingName);
                    resourceProducer.SetResourceManager(resourceManager);
                    resourceProducer.SetLocation(workerTile);
                }
            }
        }
        else //if no currently existing buildings, set up dictionaries
        {
            world.AddCityBuildingDict(workerTile);
        }

        //uiCityNamer.HandleCityName();
        //RunCityNamerUI();

        //world.RemoveUnitPosition(workerPos/*, workerUnit.gameObject*/);
        //unitMovement.uiWorkerTask.ToggleVisibility(false);
        //workerUnit.DestroyUnit(); //This unit handles its own destruction, done in unit class
        //infoManager.HideInfoPanel();
        //workerUnit.HidePath();
    }

    //checking if building city too close to another one
    private bool CheckForNearbyCity(Vector3 workerPos)
    {
        Vector3Int workerTile = Vector3Int.RoundToInt(workerPos);
        foreach (Vector3Int tile in world.GetNeighborsFor(workerTile, MapWorld.State.EIGHTWAYTWODEEP))
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
        workerUnit.isBusy = false;
        resourceIndividualHandler.NullHarvestValues();
        TurnOffCancelTask();
    }

    public void TurnOffCancelTask()
    {
        uiCancelTask.ToggleTweenVisibility(false);
    }
}
