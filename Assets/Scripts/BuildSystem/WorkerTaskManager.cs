using System.Collections;
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
    [SerializeField]
    private UIWorkerHandler uiWorkerHandler;
    [SerializeField]
    private UIBuildingSomething uiBuildingSomething;

    private ResourceIndividualHandler resourceIndividualHandler;
    private RoadManager roadManager;

    private Coroutine taskCoroutine;

    private int cityBuildingTime = 1;
    private WaitForSeconds cityBuildTime = new WaitForSeconds(1);

    private void Awake()
    {
        //audioSource = GetComponent<AudioSource>();
        resourceIndividualHandler = GetComponent<ResourceIndividualHandler>();
        roadManager = GetComponent<RoadManager>();
    }

    public void HandleEsc()
    {
        if (workerUnit != null && workerUnit.isBusy)
            CancelTask();
    }

    //Methods to run when pressing certain keys
    public void HandleR()
    {
        if (workerUnit != null && !workerUnit.isBusy)
        {
            unitMovement.buildingRoad = true;
            uiBuildingSomething.SetText("Building Road");
            OrdersPrep();
            workerUnit.WorkerOrdersPreparations();
            //ToggleRoadBuild(true);
            //Vector3 pos = workerUnit.transform.position;
            //pos.y = 0;
            //Vector3Int workerTile = world.GetClosestTerrainLoc(pos);

            //if (Vector3Int.RoundToInt(pos) == workerTile)
            //{
            //    //add to finish animation listener
            //}
            //else
            //{
            //    workerUnit.FinishedMoving.AddListener(workerUnit.BuildRoadPreparations);
            //    MoveToCenterOfTile(workerTile);
            //}
        }
    }

    public void HandleB()
    {
        if (workerUnit != null && !workerUnit.isBusy && !unitMovement.uiJoinCity.activeStatus)
        {
            Vector3 pos = workerUnit.transform.position;
            pos.y = 0;
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
            unitMovement.removingRoad = true;
            uiBuildingSomething.SetText("Removing Road");
            OrdersPrep();
            workerUnit.WorkerOrdersPreparations();
            //Vector3 pos = workerUnit.transform.position;
            //pos.y = 0;
            //Vector3Int workerTile = world.GetClosestTerrainLoc(pos);

            //if (Vector3Int.RoundToInt(pos) == workerTile)
            //{
            //    //add to finish animation listener
            //    workerUnit.RemoveRoad();
            //}
            //else
            //{
            //    MoveToCenterOfTile(workerTile);
            //    workerUnit.FinishedMoving.AddListener(workerUnit.RemoveRoad);
            //}
        }
    }

    private void MoveToCenterOfTile(Vector3Int workerTile)
    {
        workerUnit.ShiftMovement();
        unitMovement.HandleSelectedLocation(workerTile, workerTile, workerUnit);
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
        if (workerUnit.isBusy || improvementData == null)
            return;
        
        Vector3 pos = workerUnit.transform.position;
        pos.y = 0;
        Vector3Int workerTile = world.GetClosestTerrainLoc(pos);

        //if (improvementData == null) //removing things
        //{
        //    return;
        //    //if (Vector3Int.RoundToInt(pos) == workerTile)
        //    //{
        //    //    //add to finish animation listener
        //    //    workerUnit.RemoveRoad();
        //    //}
        //    //else
        //    //{
        //    //    workerUnit.FinishedMoving.AddListener(workerUnit.RemoveRoad);
        //    //    MoveToCenterOfTile(workerTile);
        //    //}
        //}

        if (improvementData.improvementName == "Resource") //if action is to gather resources
        {
            workerUnit.GatherResource();
        }

        else if (improvementData.improvementName == "Road") //adding road
        {
            unitMovement.buildingRoad = true;
            uiBuildingSomething.SetText("Building Road");
            OrdersPrep();
            workerUnit.WorkerOrdersPreparations();

            //if (Vector3Int.RoundToInt(pos) == workerTile)
            //{
            //    workerUnit.BuildRoadPreparations();
            //}
            //else
            //{
            //    MoveToCenterOfTile(workerTile);
            //    workerUnit.FinishedMoving.AddListener(workerUnit.BuildRoadPreparations);
            //}
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

    public void RemoveAllPrep()
    {
        if (!workerUnit.isBusy)
        {
            unitMovement.removingAll = true;
            uiBuildingSomething.SetText("Removing All");
            OrdersPrep();
            workerUnit.WorkerOrdersPreparations();
        }
    }

    public void RemoveRoadPrep()
    {
        if (!workerUnit.isBusy)
        {
            unitMovement.removingRoad = true;
            uiBuildingSomething.SetText("Removing Road");
            OrdersPrep();
            workerUnit.WorkerOrdersPreparations();
        }
    }

    public void RemoveLiquidPrep()
    {
        if (!workerUnit.isBusy)
        {
            unitMovement.removingLiquid = true;
            uiBuildingSomething.SetText("Removing Liquid");
            OrdersPrep();
            workerUnit.WorkerOrdersPreparations();
        }
    }

    public void RemovePowerPrep()
    {
        if (!workerUnit.isBusy)
        {
            unitMovement.removingPower = true;
            uiBuildingSomething.SetText("Removing Power");
            OrdersPrep(); 
            workerUnit.WorkerOrdersPreparations();
        }
    }

    public void RemoveRoad(Vector3Int tile, Worker worker)
    {
        if (!world.IsRoadOnTerrain(tile))
        {
            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "No road here");
            worker.SkipRoadRemoval();
            return;
        }
        else if (world.IsCityOnTile(tile) || world.IsWonderOnTile(tile) || world.IsTradeCenterOnTile(tile))
        {
            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't remove this");
            worker.SkipRoadRemoval();
            return;
        }
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
        //uiCancelTask.ToggleTweenVisibility(true);
        world.SetWorkerWorkLocation(tile);
        world.RemoveQueueItemCheck(tile);
        taskCoroutine = StartCoroutine(roadManager.RemoveRoad(tile, worker)); //specific worker (instead of workerUnit) to allow concurrent build
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
        world.SetWorkerWorkLocation(world.RoundToInt(workerPos));
        uiCancelTask.ToggleTweenVisibility(true);
        taskCoroutine = StartCoroutine(resourceIndividualHandler.GenerateHarvestedResource(workerPos, worker, city, resourceIndividual));


        //workerUnit.HidePath();
    }

    private void OrdersPrep()
    {
        uiWorkerHandler.ToggleVisibility(false, null, world);
        uiBuildingSomething.ToggleVisibility(true);

        world.unitOrders = true;
        workerUnit.isBusy = true;
        //unitMovement.uiConfirmBuildRoad.ToggleTweenVisibility(true);
        uiCancelTask.ToggleTweenVisibility(true);
        unitMovement.uiMoveUnit.ToggleTweenVisibility(false);
        unitMovement.SelectedWorker = workerUnit;
    }

    //public void CloseBuildingRoadPanel()
    //{
    //    uiBuildingSomething.ToggleVisibility(false);
    //}

    public void MoveToCompleteOrders(Vector3Int workerTile, Worker workerUnit)
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
        //taskCoroutine = StartCoroutine(worker.BuildRoad(workerTile, roadManager));
        //workerUnit.HidePath();

        unitMovement.HandleSelectedLocation(workerTile, workerTile, workerUnit);

        //taskCoroutine = StartCoroutine(roadManager.BuildRoad(workerTile, worker)); //specific worker (instead of workerUnit) to allow concurrent build
    }

    public void BuildRoad(Vector3Int tile, Worker worker)
    {
        if (!world.IsTileOpenCheck(tile))
        {
            worker.SkipRoadBuild();
            return;
        }

        worker.RemoveFromOrderQueue();
        world.SetWorkerWorkLocation(tile);
        world.RemoveQueueItemCheck(tile);
        taskCoroutine = StartCoroutine(roadManager.BuildRoad(tile, worker)); //specific worker (instead of workerUnit) to allow concurrent build
    }

    public void BuildCityPreparations(Vector3Int tile, Worker worker)
    {
        if (CheckForNearbyCity(tile))
        {
            worker.isBusy = false;
            return;
        }
        if (!world.IsTileOpenButRoadCheck(tile))
        {
            InfoPopUpHandler.WarningMessage().Create(tile, "Already something here");
            worker.isBusy = false;
            return;
        }

        TerrainData td = world.GetTerrainDataAt(tile);
        TerrainType type = td.terrainData.type;

        if (type == TerrainType.Forest || type == TerrainType.ForestHill)
        {
            InfoPopUpHandler.WarningMessage().Create(tile, "Trees in the way");
            worker.isBusy = false;
            return;
        }
        else if (type == TerrainType.River)
        {
            InfoPopUpHandler.WarningMessage().Create(tile, "Not in water...");
            worker.isBusy = false;
            return;
        }

        bool clearForest = type == TerrainType.Forest || type == TerrainType.ForestHill;
        world.SetWorkerWorkLocation(tile);
        world.RemoveQueueItemCheck(tile);
        worker.citiesBuilt++;
        worker.SpeechCheck();
        taskCoroutine = StartCoroutine(BuildCityCoroutine(tile, worker, clearForest, td));   
    }

    private IEnumerator BuildCityCoroutine(Vector3Int workerTile, Worker worker, bool clearForest, TerrainData td)
    {
        int timePassed = cityBuildingTime;
        //int timeLimit = cityBuildingTime;

        if (clearForest)
            timePassed *= 2;

        worker.ShowProgressTimeBar(timePassed);
        worker.SetWorkAnimation(true);
        worker.SetTime(timePassed);

        while (timePassed > 0)
        {
            yield return cityBuildTime;
            timePassed--;
            worker.SetTime(timePassed);
        }

        worker.HideProgressTimeBar();
        worker.SetWorkAnimation(false);
        if (worker.isSelected)
            TurnOffCancelTask();
        BuildCity(workerTile, worker, clearForest, td);
        world.RemoveWorkerWorkLocation(workerTile);

        //moving worker up a smidge to be on top of road
        //Vector3 moveUp = worker.transform.position;
        //moveUp.y += .2f;
        //worker.transform.position = moveUp;
    }

    private void BuildCity(Vector3Int workerTile, Worker worker, bool clearForest, TerrainData td)
    {
        worker.isBusy = false;

        //clear the forest if building on forest tile
        if (clearForest)
        {
            Destroy(td.prop.GetChild(0).gameObject);
            td.terrainData = td.terrainData.clearedForestData;
            td.gameObject.tag = "Flatland";
        }

        td.prop.gameObject.SetActive(false);

        GameObject newCity = Instantiate(cityData.prefab, workerTile, Quaternion.identity); 
        newCity.gameObject.transform.SetParent(world.cityHolder, false);
        world.AddStructure(workerTile, newCity); //adds building location to buildingDict
        City city = newCity.GetComponent<City>();
        city.SetWorld(world);
        city.SetNewCityName();
        //world.AddStructureMap(workerTile, city.mapIcon);
        //city.cityMapName = world.SetCityTileMap(workerTile, city.name);
        world.AddCity(workerTile, city);
        city.SetCityBuilderManager(GetComponent<CityBuilderManager>());
        city.CheckForAvailableSingleBuilds();

        //build road where city is placed
        if (!world.IsRoadOnTerrain(workerTile))
        {
            foreach (Vector3Int loc in world.GetNeighborsFor(workerTile, MapWorld.State.EIGHTWAYINCREMENT))
            {
                if (world.IsRoadOnTerrain(loc))
                {
                    //moving worker up a smidge to be on top of road
                    Vector3 moveUp = worker.transform.position;
                    moveUp.y += .2f;
                    worker.transform.position = moveUp;
                    roadManager.BuildRoadAtPosition(workerTile);
                    break;
                }
            }
        }
        
        city.LightFire(td.isHill);
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
        //city.SetHouse(city.housingData, workerTile, td.isHill, false);
        //td.gameObject.tag = "City";
        //}

        //city.CombineFire();
        //showing join city button
        unitMovement.ShowIndividualCityButtonsUI();
    }

    //checking if building city too close to another one
    private bool CheckForNearbyCity(Vector3 workerPos)
    {
        Vector3Int workerTile = Vector3Int.RoundToInt(workerPos);
        int i = 0;
        foreach (Vector3Int tile in world.GetNeighborsFor(workerTile, MapWorld.State.CITYRADIUS))
        {
            if (i < 8 && world.IsTradeCenterOnTile(tile))
            {
                InfoPopUpHandler.WarningMessage().Create(workerUnit.transform.position, "Too close to another city");
                return true;
            }
            i++;
            if (!world.IsCityOnTile(tile))
            {
                continue;
            }
            else
            {
                InfoPopUpHandler.WarningMessage().Create(workerUnit.transform.position, "Too close to another city");
                return true;
            }
        }

        return false;
    }

    public void CancelTask()
    {
        if (!workerUnit)
            return;
        
        if (world.unitOrders)
        {
            unitMovement.ToggleOrderHighlights(false);
            unitMovement.ClearBuildRoad();
			uiBuildingSomething.ToggleVisibility(false);
			unitMovement.ResetOrderFlags();
            //workerUnit.ResetRoadQueue();
        }
        else if (workerUnit.isMoving)
        {
            workerUnit.WorkerOrdersPreparations();
            //workerUnit.ResetRoadQueue();
        }
        else
        {
            workerUnit.SetWorkAnimation(false);
            StopCoroutine(taskCoroutine);
            workerUnit.HideProgressTimeBar();
            world.RemoveWorkerWorkLocation(world.RoundToInt(workerUnit.transform.position));
        }

        workerUnit.ResetOrderQueue();
        workerUnit.isBusy = false;
        workerUnit.removing = false;
        TurnOffCancelTask();
    }

    //public void ToggleRoadBuild(bool v)
    //{
    //    uiWorkerHandler.roadBuildOption.ToggleColor(v);
    //}

    public void TurnOffCancelTask()
    {
        uiCancelTask.ToggleTweenVisibility(false);
    }

    public void NullWorkerUnit()
    {
        workerUnit = null;
    }
}
