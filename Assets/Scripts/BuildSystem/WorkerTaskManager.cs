using System.Collections;
using UnityEngine;
using UnityEngine.Timeline;
using static UnityEditor.PlayerSettings;
using static UnityEngine.UI.GridLayoutGroup;

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

    [HideInInspector]
    public ResourceIndividualHandler resourceIndividualHandler;
    [HideInInspector]
    public RoadManager roadManager;

    [HideInInspector]
    public Coroutine taskCoroutine;

    private int cityBuildingTime = 1;
    [HideInInspector]
    public int timePassed;
    private WaitForSeconds oneSecondWait = new WaitForSeconds(1);

    private void Awake()
    {
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
        if (workerUnit != null && !workerUnit.isBusy && !workerUnit.sayingSomething)
        {
			unitMovement.buildingRoad = true;
            uiBuildingSomething.SetText("Building Road");
            OrdersPrep();
            workerUnit.WorkerOrdersPreparations();
        }
    }

    public void HandleB()
    {
        if (workerUnit != null && !workerUnit.isBusy && !unitMovement.uiJoinCity.activeStatus && !workerUnit.sayingSomething)
        {
			if (world.tutorialGoing)
				unitMovement.uiWorkerTask.GetButton("Build").FlashCheck();

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
        if (workerUnit != null && !workerUnit.isBusy && !workerUnit.sayingSomething)
        {
			if (world.tutorialGoing)
				unitMovement.uiWorkerTask.GetButton("Gather").FlashCheck();

			workerUnit.StopMovement();
            workerUnit.GatherResource();
        }
    }

    public void HandleX()
    {
        if (workerUnit != null && !workerUnit.isBusy && !workerUnit.sayingSomething)
        {
			unitMovement.removingRoad = true;
            uiBuildingSomething.SetText("Removing Road");
            OrdersPrep();
            workerUnit.WorkerOrdersPreparations();
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
            uiCancelTask.ToggleVisibility(true);
    }

	public void BuildCityButton()
    {
        if (!workerUnit.isBusy)
        {
			world.cityBuilderManager.PlaySelectAudio();
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

    public void GatherResourceButton()
    {
        if (!workerUnit.isBusy)
        {
		    world.cityBuilderManager.PlaySelectAudio();
		    workerUnit.GatherResource();
        }
	}

    public void BuildUtilityButton()
    {
		if (!workerUnit.isBusy)
        {
            world.cityBuilderManager.PlaySelectAudio();
		    unitMovement.buildingRoad = true;
		    uiBuildingSomething.SetText("Building Road");
		    OrdersPrep();
		    workerUnit.WorkerOrdersPreparations();
        }
	}

    public void ClearForestButton()
    {
		if (!workerUnit.isBusy)
        {
            world.cityBuilderManager.PlaySelectAudio();
		    workerUnit.ClearForest();
        }
    }

    public void PerformTask(ImprovementDataSO improvementData) //don't actually use "improvementData".
    {
        if (workerUnit.isBusy || improvementData == null)
            return;
        
        Vector3 pos = workerUnit.transform.position;
        pos.y = 0;
        Vector3Int workerTile = world.GetClosestTerrainLoc(pos);

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
			world.cityBuilderManager.PlaySelectAudio();
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
            world.cityBuilderManager.PlaySelectAudio();
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
			world.cityBuilderManager.PlaySelectAudio();
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
			world.cityBuilderManager.PlaySelectAudio();
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

        world.SetWorkerWorkLocation(tile);
        //world.RemoveQueueItemCheck(tile);
        roadManager.timePassed = roadManager.roadRemovingTime;
        taskCoroutine = StartCoroutine(roadManager.RemoveRoad(tile, worker));
    }

    public void LoadRemoveRoadCoroutine(int timePassed, Vector3Int tile, Worker worker)
    {
		world.SetWorkerWorkLocation(tile);
		roadManager.timePassed = timePassed;
        taskCoroutine = StartCoroutine(roadManager.RemoveRoad(tile, worker));
	}

    public void GatherResource(Vector3 workerPos, Worker worker, City city, ResourceIndividualSO resourceIndividual, bool clearForest)
    {
		world.SetWorkerWorkLocation(world.RoundToInt(workerPos));
        uiCancelTask.ToggleVisibility(true);
        if (clearForest)
            resourceIndividualHandler.timePassed = worker.clearingForestTime;
        else
            resourceIndividualHandler.timePassed = resourceIndividual.ResourceGatheringTime;
        taskCoroutine = StartCoroutine(resourceIndividualHandler.GenerateHarvestedResource(workerPos, worker, city, resourceIndividual, clearForest));
    }

    public void LoadGatherResourceCoroutine(int timePassed, Vector3 workerPos, Worker worker, City city, ResourceIndividualSO resourceIndividual, bool clearForest)
    {
		world.SetWorkerWorkLocation(world.RoundToInt(workerPos));
        resourceIndividualHandler.timePassed = timePassed;
		taskCoroutine = StartCoroutine(resourceIndividualHandler.GenerateHarvestedResource(workerPos, worker, city, resourceIndividual, clearForest));
	}

    private void OrdersPrep()
    {
        uiWorkerHandler.ToggleVisibility(false, world);
        uiBuildingSomething.ToggleVisibility(true);

        world.unitOrders = true;
        workerUnit.isBusy = true;
        uiCancelTask.ToggleVisibility(true);
        unitMovement.uiMoveUnit.ToggleVisibility(false);
        unitMovement.SelectedWorker = workerUnit;
    }

    public void MoveToCompleteOrders(Vector3Int workerTile, Worker workerUnit)
    {
        unitMovement.HandleSelectedLocation(workerTile, workerTile, workerUnit);
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
		roadManager.timePassed = roadManager.roadBuildingTime;
		taskCoroutine = StartCoroutine(roadManager.BuildRoad(tile, worker)); //specific worker (instead of workerUnit) to allow concurrent build
    }

    public void LoadRoadBuildCoroutine(int timePassed, Vector3Int tile, Worker worker)
    {
		world.SetWorkerWorkLocation(tile);
		roadManager.timePassed = timePassed;
		taskCoroutine = StartCoroutine(roadManager.BuildRoad(tile, worker));
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

        if (type == TerrainType.River)
        {
            InfoPopUpHandler.WarningMessage().Create(tile, "Not in water...");
            worker.isBusy = false;
            return;
        }

		bool clearForest = type == TerrainType.Forest || type == TerrainType.ForestHill;
        world.SetWorkerWorkLocation(tile);
        world.RemoveQueueItemCheck(tile);
        int totalTime = cityBuildingTime;
        if (clearForest)
            totalTime += worker.clearingForestTime;
            
		timePassed = totalTime;
		taskCoroutine = StartCoroutine(BuildCityCoroutine(tile, worker, clearForest, td, totalTime));   
    }

    public void LoadBuildCityCoroutine(int timePassed, Vector3Int workerTile, Worker worker)
    {
		TerrainData td = world.GetTerrainDataAt(workerTile);
		TerrainType type = td.terrainData.type;
		bool clearForest = type == TerrainType.Forest || type == TerrainType.ForestHill;
		world.SetWorkerWorkLocation(workerTile);
        int totalTime = cityBuildingTime;
        if (clearForest)
            totalTime += worker.clearingForestTime;

        this.timePassed = timePassed;
        taskCoroutine = StartCoroutine(BuildCityCoroutine(workerTile, worker, clearForest, td, totalTime));
	}

    private IEnumerator BuildCityCoroutine(Vector3Int workerTile, Worker worker, bool clearForest, TerrainData td, int totalTime)
    {
        worker.buildingCity = true;
        worker.ShowProgressTimeBar(totalTime);
        worker.SetWorkAnimation(true);
        worker.SetTime(timePassed);

        while (timePassed > 0)
        {
            yield return oneSecondWait;
            timePassed--;
            worker.SetTime(timePassed);
        }

        taskCoroutine = null;
        worker.HideProgressTimeBar();
        worker.SetWorkAnimation(false);
        if (worker.isSelected)
            TurnOffCancelTask();
        BuildCity(workerTile, worker, clearForest, td);
        world.RemoveWorkerWorkLocation(workerTile);
    }

    private void BuildCity(Vector3Int workerTile, Worker worker, bool clearForest, TerrainData td)
    {
        worker.isBusy = false;
        worker.buildingCity = false;

        td.ShowProp(false);
        td.FloodPlainCheck(true);

        GameObject newCity = Instantiate(cityData.prefab, workerTile, Quaternion.identity); 
        newCity.gameObject.transform.SetParent(world.cityHolder, false);
        world.AddStructure(workerTile, newCity); //adds building location to buildingDict
        City city = newCity.GetComponent<City>();
        city.SetWorld(world);
		world.cityCount++;
		city.SetNewCityName();
        //world.AddStructureMap(workerTile, city.mapIcon);
        //city.cityMapName = world.SetCityTileMap(workerTile, city.name);
        world.AddCity(workerTile, city);
        //city.SetCityBuilderManager(GetComponent<CityBuilderManager>());
        city.CheckForAvailableSingleBuilds();

		//clear the forest if building on forest tile
		if (clearForest)
		{
            td.ShowProp(false);
			worker.marker.ToggleVisibility(false);
			//Destroy(td.treeHandler.gameObject);
			//         TerrainDataSO tempData;

			//if (td.isHill)
			//{
			//	tempData = td.terrainData.grassland ? world.grasslandHillTerrain : world.desertHillTerrain;
			//}
			//else
			//{
			//	tempData = td.terrainData.grassland ? world.grasslandTerrain : world.desertTerrain;
			//}

			city.SetNewTerrainData(td);
            //td.SetNewData(tempData);
            //GameLoader.Instance.gameData.allTerrain[workerTile] = td.SaveData();
			city.ResourceManager.CheckResource(ResourceType.Lumber, worker.clearedForestlumberAmount); 
		}

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

        foreach (Vector3Int tile in world.GetNeighborsFor(workerTile, MapWorld.State.CITYRADIUS))
        {
            TerrainData tData = world.GetTerrainDataAt(tile);

			if (tData.terrainData.type == TerrainType.River)
            {
                city.hasWater = true;
                city.hasFreshWater = true;
            }
            
            if (tData.terrainData.type == TerrainType.Coast)
                city.hasWater = true;

            if (tData.rawResourceType == RawResourceType.Rocks)
            {
                if (tData.isHill)
                    city.hasRocksHill = true;
                else
                    city.hasRocksFlat = true;
            }

            if (tData.resourceType == ResourceType.Clay)
                city.hasClay = true;

			if (tData.resourceType == ResourceType.Wool)
				city.hasWool = true; 
            
            if (tData.resourceType == ResourceType.Silk)
                city.hasSilk = true;

            if (tData.resourceType == ResourceType.Lumber)
                city.hasTrees = true;

            if ((tData.terrainData.grassland && tData.terrainData.type == TerrainType.Flatland) || tData.terrainData.specificTerrain == SpecificTerrain.FloodPlain)
                city.hasFood = true;
        }

        city.reachedWaterLimit = !city.hasFreshWater;
        city.waterCount = city.hasFreshWater ? 9999 : 0;
        world.AddCityBuildingDict(workerTile);
        world.TutorialCheck("Build City");
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
        }
        else if (workerUnit.isMoving)
        {
            workerUnit.WorkerOrdersPreparations();
        }
        else
        {
            if (workerUnit.clearingForest)
            {
                workerUnit.clearingForest = false;
                Vector3Int pos = world.GetClosestTerrainLoc(workerUnit.transform.position);
				world.GetTerrainDataAt(pos).beingCleared = false;
				GameLoader.Instance.gameData.allTerrain[pos].beingCleared = false;
			}
            workerUnit.SetWorkAnimation(false);

            if (taskCoroutine != null)
                StopCoroutine(taskCoroutine);
            workerUnit.HideProgressTimeBar();
            world.RemoveWorkerWorkLocation(world.RoundToInt(workerUnit.transform.position));
        }

        workerUnit.ResetOrderQueue();
        workerUnit.isBusy = false;
        workerUnit.removing = false;
        workerUnit.gathering = false;
        workerUnit.buildingCity = false;
        TurnOffCancelTask();
    }

    public void TurnOffCancelTask()
    {
        uiCancelTask.ToggleVisibility(false);
    }

    public void NullWorkerUnit()
    {
        workerUnit = null;
    }
}
