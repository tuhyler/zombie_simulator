using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkerTaskManager : MonoBehaviour
{
    [SerializeField]
    public UIWorkerHandler workerTaskUI;

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
    private RoadManager roadManager;

    [SerializeField]
    private ImprovementDataSO cityData;

    private ResourceIndividualHandler resourceIndividualHandler;

    private void Awake()
    {
        //audioSource = GetComponent<AudioSource>();
        resourceIndividualHandler = GetComponent<ResourceIndividualHandler>();
    }

    //Methods to run when pressing the letter R
    public void HandleR()
    {
        if (workerUnit != null)
        {
            BuildRoad(Vector3Int.FloorToInt(workerUnit.transform.position));
        }
    }

    public void HandleB()
    {
        if (workerUnit != null)
        {
            BuildCity(world.GetClosestTerrainLoc(workerUnit.transform.position), cityData);
        }
    }

    public void HandleG()
    {
        if (workerUnit != null)
        {
            GatherResource(workerUnit.transform.position, Vector3Int.FloorToInt(workerUnit.transform.position));
        }
    }

    public void HandleX()
    {
        if (workerUnit != null)
        {
            RemoveRoad(Vector3Int.FloorToInt(workerUnit.transform.position));
        }
    }

    public void HandleSelection(GameObject selectedObject)
    {
        if (selectedObject == null)
            return;

        //for deselecting unit
        Unit unitReference = selectedObject.GetComponent<Unit>(); //is this slow?
        if (CheckIfSameUnitSelected(unitReference) && unitReference != null)
        {
            workerTaskUI.ToggleVisibility(false);
            workerUnit = null;
            return;
        }

        workerUnit = null;
        ResetWorkerTaskSystem(); //reset everything before selecting

        workerUnit = selectedObject.GetComponent<Worker>(); //checks if unit is worker

        if (workerUnit != null)
        {
            HandleUnitSelection();
        }
    }

    public void HandleUnitSelection(Unit unit) //for unit turn handler
    {
        if (workerUnit != null)
        {
            workerUnit = null;
            ResetWorkerTaskSystem();
        }
        workerUnit = unit.GetComponent<Worker>();
        if (workerUnit != null)
        {
            HandleUnitSelection();
        }
    }

    private void HandleUnitSelection()
    {
        //workerUnit = worker.GetComponent<Unit>();
        if (workerUnit.harvested) //if unit just finished harvesting something, can't move until resource is sent to city
            workerUnit.SendResourceToCity();
        if (workerUnit != null) //Because warrior doesn't have worker component, it won't see the build UI
        {
            workerTaskUI.ToggleVisibility(true);
            //workerUnit.FinishedMoving.AddListener(ResetWorkerTaskSystem); //hides the UI here without needing to connect to unit class
        }
    }

    private bool CheckIfSameUnitSelected(Unit unitReference) //for deselection
    {
        return workerUnit == unitReference;
    }

    private void ResetWorkerTaskSystem() //hides worker ui, nulls unit
    {
        if (workerUnit == null)
        {
            workerTaskUI.ToggleVisibility(false);
        }
        else
        {
            //workerUnit.FinishedMoving.RemoveListener(ResetWorkerTaskSystem); //need to remove the listener else it will stay, adding to memory
            workerTaskUI.ToggleVisibility(true);
        }
    }

    public void PerformTask(ImprovementDataSO improvementData)
    {
        Vector3 workerPos = workerUnit.transform.position;
        Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);

        Debug.Log("Performing task at " + workerTile);

        if (improvementData == null) //removing the road
        {
            RemoveRoad(workerTile);
            return;
        }

        if (improvementData.prefab.name == "Resource") //if action is to gather resources
        {
            GatherResource(workerPos, workerTile);
            return;
        }

        if (improvementData.prefab.name == "Road") //adding road
        {
            BuildRoad(workerTile);
            return;
        }

        if (improvementData.prefab.name == "City") //creating city
        {
            BuildCity(workerTile, improvementData);
            return;
        }
    }

    private void RemoveRoad(Vector3Int workerTile)
    {
        if (!world.IsRoadOnTile(workerTile))
        {
            Debug.Log("No road on tile.");
            return;
        }

        if (world.IsCityOnTile(workerTile))
        {
            Debug.Log("Can't remove city road");
            return;
        }
        roadManager.RemoveRoadAtPosition(workerTile);
        movementSystem.HidePath();
        Debug.Log("Removing road at " + workerTile);
    }

    private void GatherResource(Vector3 workerPos, Vector3Int workerTile)
    {
        if (world.IsBuildLocationTaken(workerTile) || world.IsRoadOnTile(workerTile))
        {
            Debug.Log("Harvest on open tile");
            return;
        }

        if (!resourceIndividualHandler.CheckForCity(workerTile)) //only works if city is nearby. 
            return;

        ResourceIndividualSO resourceIndividual = resourceIndividualHandler.GetResourcePrefab(workerTile);
        if (resourceIndividual == null)
        {
            Debug.Log("No resource to harvest here.");
            return;
        }
        Debug.Log("Harvesting resource at " + workerPos);

        resourceIndividualHandler.GenerateHarvestedResource(resourceIndividual, workerPos, workerUnit);

        movementSystem.HidePath();
        return;
    }

    private void BuildRoad(Vector3Int workerTile)
    {
        if (world.IsRoadOnTile(workerTile))
        {
            Debug.Log("Already road on tile");
            return;
        }
        if (world.IsBuildLocationTaken(workerTile))
        {
            Debug.Log("Already structure on tile");
            return;
        }

        roadManager.BuildRoadAtPosition(workerTile);
        movementSystem.HidePath();
    }

    private void BuildCity(Vector3Int workerTile, ImprovementDataSO improvementData)
    {
        if (world.IsBuildLocationTaken(workerTile))
        {
            Debug.Log("Already occupied");
            return;
        }

        if (CheckForCity(workerTile))
            return;

        //clear the forest if building on forest tile
        TerrainData td = world.GetTerrainDataAt(workerTile);
        if (td.GetTerrainData().type == TerrainType.Forest)
        {
            GameObject newPrefab = td.GetTerrainData().clearedForestPrefab;
            GameObject newTile = Instantiate(newPrefab, workerTile, Quaternion.Euler(0, 0, 0));
            td.DestroyTile(world);
            newTile.GetComponent<TerrainData>().AddTerrainToWorld(world);
        }

        if (!world.IsRoadOnTile(workerTile)) //build road where city is placed
            roadManager.BuildRoadAtPosition(workerTile);

        //Vector3Int workerTile = Vector3Int.FloorToInt(workerPos);
        GameObject newCity = Instantiate(improvementData.prefab, workerTile, Quaternion.identity); //creates building unit position.
        world.AddStructure(workerTile, newCity); //adds building location to buildingDict

        ResourceProducer resourceProducer = newCity.GetComponent<ResourceProducer>();
        world.AddResourceProducer(workerTile, resourceProducer);
        resourceProducer.InitializeImprovementData(improvementData); //allows the new structure to also start generating resources
        ResourceManager resourceManager = newCity.GetComponent<ResourceManager>();
        resourceProducer.SetResourceManager(resourceManager);
        resourceProducer.BeginResourceGeneration(); //begin generating resources
        if (world.TileHasBuildings(workerTile)) //if tile already has buildings, need to switch resourceManager for each resourceProducer 
        {
            foreach (string buildingName in world.GetBuildingListForCity(workerTile))
            {
                if (world.CheckBuildingIsProducer(workerTile, buildingName))
                {
                    world.GetBuildingProducer(workerTile, buildingName).SetResourceManager(resourceManager);
                }
            }
        }
        else //if no currently existing buildings, set up dictionaries
        {
            world.AddCityBuildingDict(workerTile);
        }

        newCity.GetComponent<City>().SetNewCityName();
        //uiCityNamer.HandleCityName();
        //RunCityNamerUI();

        //world.RemoveUnitPosition(workerPos/*, workerUnit.gameObject*/);
        workerTaskUI.ToggleVisibility(false);
        workerUnit.DestroyUnit(); //This unit handles its own destruction, done in unit class
        infoManager.HideInfoPanel();
        movementSystem.HidePath();
    }

    //checking if building city too close to another one
    private bool CheckForCity(Vector3 workerPos)
    {
        Vector3Int workerTile = Vector3Int.FloorToInt(workerPos);
        foreach (Vector3Int tile in world.GetNeighborsFor(workerTile, MapWorld.State.EIGHTWAYTWODEEP))
        {
            if (!world.IsCityOnTile(tile))
            {
                continue;
            }
            else
            {
                Debug.Log("Can't build here, too close to another city.");
                return true;
            }
        }

        return false;
    }
}
