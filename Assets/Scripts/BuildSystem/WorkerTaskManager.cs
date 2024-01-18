using System.Collections;
using UnityEngine;
using UnityEngine.Timeline;
using static UnityEditor.PlayerSettings;
using static UnityEngine.UI.GridLayoutGroup;

public class WorkerTaskManager : MonoBehaviour
{
    //[SerializeField]
    //public UIWorkerHandler workerTaskUI;

    //private Worker workerUnit; //The only unit that can build

    //[SerializeField]
    //private AudioSource audioSource; //do audio later

    [SerializeField]
    private MapWorld world;
    [SerializeField]
    private MovementSystem movementSystem;
    [SerializeField]
    private UnitMovement unitMovement;
    [SerializeField]
    private ImprovementDataSO cityData;
    [SerializeField]
    private UIWorkerHandler uiWorkerHandler;
    [SerializeField]
    private UIBuildingSomething uiBuildingSomething;

    //[HideInInspector]
    //public ResourceIndividualHandler resourceIndividualHandler;
    [HideInInspector]
    public RoadManager roadManager;

    //[HideInInspector]
    //public Coroutine taskCoroutine;

    //private int cityBuildingTime = 1;
    //[HideInInspector]
    //public int timePassed;
    //private WaitForSeconds oneSecondWait = new WaitForSeconds(1);

    private void Awake()
    {
        //resourceIndividualHandler = GetComponent<ResourceIndividualHandler>();
        roadManager = GetComponent<RoadManager>();
    }

    //public void HandleEsc()
    //{
    //    if (world.mainPlayer.isSelected && world.mainPlayer.isBusy)
    //        CancelTask();
    //}

    //Methods to run when pressing certain keys
    public void HandleR()
    {
        if (world.mainPlayer.isSelected && !world.mainPlayer.isBusy && !world.mainPlayer.sayingSomething && world.scottFollow)
        {
			unitMovement.buildingRoad = true;
            uiBuildingSomething.SetText("Building Road");
            OrdersPrep();
			world.scott.WorkerOrdersPreparations();
        }
    }

    public void HandleB()
    {
        if (world.mainPlayer.isSelected && !world.mainPlayer.isBusy && !unitMovement.uiJoinCity.activeStatus && !world.mainPlayer.sayingSomething)
        {
            BuildCityPrep();
        }
    }

    public void HandleF()
    {
        if (world.mainPlayer.isSelected && !world.mainPlayer.isBusy && !world.mainPlayer.sayingSomething)
        {
			if (world.tutorialGoing)
				unitMovement.uiWorkerTask.GetButton("Gather").FlashCheck();

			world.mainPlayer.StopMovement();
			world.mainPlayer.GatherResource();
        }
    }

    public void HandleX()
    {
        if (world.mainPlayer.isSelected && !world.mainPlayer.isBusy && !world.mainPlayer.sayingSomething && world.scottFollow)
        {
			unitMovement.removingRoad = true;
            uiBuildingSomething.SetText("Removing Road");
            OrdersPrep();
			world.scott.WorkerOrdersPreparations();
        }
    }

    private void MoveToCenterOfTile(Vector3Int workerTile)
    {
        world.mainPlayer.ShiftMovement();
        unitMovement.GoStraightToSelectedLocation(workerTile, workerTile, world.mainPlayer);
    }

    public void SetWorkerUnit()
    {
        //this.workerUnit = workerUnit;
        if (world.mainPlayer.isBusy && !world.mainPlayer.runningAway)
            world.unitMovement.uiCancelTask.ToggleVisibility(true);
    }

	public void BuildCityButton()
    {
        if (!world.mainPlayer.isBusy)
        {
            BuildCityPrep();	
        }
	}

    public void BuildCityPrep()
    {
		if (world.tutorialGoing)
			unitMovement.uiWorkerTask.GetButton("Build").FlashCheck();

		if (unitMovement.moveUnit)
			unitMovement.CancelMove();

		world.cityBuilderManager.PlaySelectAudio();
		Vector3 pos = world.mainPlayer.transform.position;
		pos.y = 0;
		Vector3Int workerTile = world.GetClosestTerrainLoc(pos);

        if (world.scottFollow)
            world.scott.StopMovement();

        if (world.azaiFollow)
            world.azai.StopMovement();

		if (Vector3Int.RoundToInt(pos) == workerTile)
		{
			//add to finish animation listener
			world.mainPlayer.BuildCity();
		}
		else
		{
			MoveToCenterOfTile(workerTile);
			world.mainPlayer.FinishedMoving.AddListener(world.mainPlayer.BuildCity);
		}
	}

    public void GatherResourceButton()
    {
        if (!world.mainPlayer.isBusy)
        {
		    world.cityBuilderManager.PlaySelectAudio();
			world.mainPlayer.GatherResource();
        }
	}

    public void BuildUtilityButton()
    {
		if (!world.mainPlayer.isBusy)
        {
            world.cityBuilderManager.PlaySelectAudio();
		    unitMovement.buildingRoad = true;
		    uiBuildingSomething.SetText("Building Road");
		    OrdersPrep();
			world.scott.WorkerOrdersPreparations();
        }
	}

    public void ClearForestButton()
    {
		if (!world.mainPlayer.isBusy)
        {
            world.cityBuilderManager.PlaySelectAudio();
			world.mainPlayer.ClearForest();
        }
    }

    //public void PerformTask(ImprovementDataSO improvementData) //don't actually use "improvementData".
    //{
    //    if (workerUnit.isBusy || improvementData == null)
    //        return;
        
    //    Vector3 pos = workerUnit.transform.position;
    //    pos.y = 0;
    //    Vector3Int workerTile = world.GetClosestTerrainLoc(pos);

    //    if (improvementData.improvementName == "Resource") //if action is to gather resources
    //    {
    //        workerUnit.GatherResource();
    //    }

    //    else if (improvementData.improvementName == "Road") //adding road
    //    {
    //        unitMovement.buildingRoad = true;
    //        uiBuildingSomething.SetText("Building Road");
    //        OrdersPrep();
    //        workerUnit.WorkerOrdersPreparations();
    //    }

    //    else if (improvementData.improvementName == "City") //creating city
    //    {
    //        if (Vector3Int.RoundToInt(pos) == workerTile)
    //        {
    //            //add to finish animation listener
    //            workerUnit.BuildCity();
    //        }
    //        else
    //        {
    //            MoveToCenterOfTile(workerTile);
    //            workerUnit.FinishedMoving.AddListener(workerUnit.BuildCity);
    //        }
    //    }
    //}

    public void RemoveAllPrep()
    {
        if (!world.mainPlayer.isBusy)
        {
			world.cityBuilderManager.PlaySelectAudio();
			unitMovement.removingAll = true;
            uiBuildingSomething.SetText("Removing All");
            OrdersPrep();
			world.scott.WorkerOrdersPreparations();
        }
    }

    public void RemoveRoadPrep()
    {
        if (!world.mainPlayer.isBusy)
        {
            world.cityBuilderManager.PlaySelectAudio();
            unitMovement.removingRoad = true;
            uiBuildingSomething.SetText("Removing Road");
            OrdersPrep();
			world.scott.WorkerOrdersPreparations();
        }
    }

    public void RemoveLiquidPrep()
    {
        if (!world.mainPlayer.isBusy)
        {
			world.cityBuilderManager.PlaySelectAudio();
			unitMovement.removingLiquid = true;
            uiBuildingSomething.SetText("Removing Liquid");
            OrdersPrep();
			world.scott.WorkerOrdersPreparations();
        }
    }

    public void RemovePowerPrep()
    {
        if (!world.mainPlayer.isBusy)
        {
			world.cityBuilderManager.PlaySelectAudio();
			unitMovement.removingPower = true;
            uiBuildingSomething.SetText("Removing Power");
            OrdersPrep();
			world.scott.WorkerOrdersPreparations();
        }
    }

    //public void RemoveRoad(Vector3Int tile, Worker worker)
    //{
    //    if (!world.IsRoadOnTerrain(tile))
    //    {
    //        UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "No road here");
    //        worker.SkipRoadRemoval();
    //        return;
    //    }
    //    else if (world.IsCityOnTile(tile) || world.IsWonderOnTile(tile) || world.IsTradeCenterOnTile(tile))
    //    {
    //        UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't remove this");
    //        worker.SkipRoadRemoval();
    //        return;
    //    }

    //    world.SetWorkerWorkLocation(tile);
    //    //world.RemoveQueueItemCheck(tile);
    //    roadManager.timePassed = roadManager.roadRemovingTime;
    //    taskCoroutine = StartCoroutine(roadManager.RemoveRoad(tile, worker));
    //}

 //   public void LoadRemoveRoadCoroutine(int timePassed, Vector3Int tile, Worker worker)
 //   {
	//	world.SetWorkerWorkLocation(tile);
	//	roadManager.timePassed = timePassed;
 //       taskCoroutine = StartCoroutine(roadManager.RemoveRoad(tile, worker));
	//}

  //  public void GatherResource(Vector3 workerPos, Worker worker, City city, ResourceIndividualSO resourceIndividual, bool clearForest)
  //  {
		//world.SetWorkerWorkLocation(world.GetClosestTerrainLoc(workerPos));
  //      if (world.mainPlayer.isSelected)
  //          uiCancelTask.ToggleVisibility(true);
  //      if (clearForest)
  //          resourceIndividualHandler.timePassed = world.scott.clearingForestTime;
  //      else
  //          resourceIndividualHandler.timePassed = resourceIndividual.ResourceGatheringTime;
  //      taskCoroutine = StartCoroutine(resourceIndividualHandler.GenerateHarvestedResource(workerPos, worker, city, resourceIndividual, clearForest));
  //  }

 //   public void LoadGatherResourceCoroutine(int timePassed, Vector3 workerPos, Worker worker, City city, ResourceIndividualSO resourceIndividual, bool clearForest)
 //   {
	//	world.SetWorkerWorkLocation(world.GetClosestTerrainLoc(workerPos));
 //       resourceIndividualHandler.timePassed = timePassed;
	//	taskCoroutine = StartCoroutine(resourceIndividualHandler.GenerateHarvestedResource(workerPos, worker, city, resourceIndividual, clearForest));
	//}

    private void OrdersPrep()
    {
        if (unitMovement.moveUnit)
            unitMovement.CancelMove();
        
        uiWorkerHandler.ToggleVisibility(false, world);
        uiBuildingSomething.ToggleVisibility(true);

        world.mainPlayer.StopMovement();

        if (world.azaiFollow)
            world.azai.StopMovement();

        world.unitOrders = true;
		world.mainPlayer.isBusy = true;
        world.unitMovement.uiCancelTask.ToggleVisibility(true);
        unitMovement.uiMoveUnit.ToggleVisibility(false);
    }

    public void MoveToCompleteOrders(Vector3Int workerTile, Worker workerUnit)
    {
        Vector3 mainPos = world.mainPlayer.transform.position;
        Vector3Int mainPosInt = world.RoundToInt(mainPos);
        
        if (Mathf.Abs(mainPos.x - workerTile.x) > 1.2f || Mathf.Abs(mainPos.z - workerTile.z) > 1.2f)
        {
			Vector3Int diff = mainPosInt - workerTile;
            Vector3Int closestLoc = workerTile;

            if (diff.x > 0)
                diff.x = 1;
            else if (diff.x < 0)
                diff.x = -1;

            if (diff.z > 0)
                diff.z = 1;
            else if (diff.z < 0)
                diff.z = -1;

            closestLoc += diff;

            if (mainPosInt == workerTile)
            {
                world.mainPlayer.FindNewSpot(mainPosInt, null);
            }
            else
            {
                unitMovement.GoStraightToSelectedLocation(closestLoc, closestLoc, world.mainPlayer);
            }
        }
        
        if (mainPosInt == workerTile)
			world.mainPlayer.FindNewSpot(mainPosInt, null);

		unitMovement.GoStraightToSelectedLocation(workerTile, workerTile, workerUnit);
    }

  //  public void BuildRoad(Vector3Int tile, Worker worker)
  //  {
  //      if (!world.IsTileOpenCheck(tile))
  //      {
  //          worker.SkipRoadBuild();
  //          return;
  //      }

		//worker.RemoveFromOrderQueue();
  //      world.SetWorkerWorkLocation(tile);
  //      world.RemoveQueueItemCheck(tile);
		//roadManager.timePassed = roadManager.roadBuildingTime;
		//taskCoroutine = StartCoroutine(roadManager.BuildRoad(tile, worker)); //specific worker (instead of workerUnit) to allow concurrent build
  //  }

 //   public void LoadRoadBuildCoroutine(int timePassed, Vector3Int tile, Worker worker)
 //   {
	//	world.SetWorkerWorkLocation(tile);
	//	roadManager.timePassed = timePassed;
	//	taskCoroutine = StartCoroutine(roadManager.BuildRoad(tile, worker));
	//}

  //  public void BuildCityPreparations(Vector3Int tile, Worker worker)
  //  {
  //      if (CheckForNearbyCity(tile))
  //      {
  //          worker.isBusy = false;
  //          return;
  //      }
  //      if (!world.IsTileOpenButRoadCheck(tile))
  //      {
  //          InfoPopUpHandler.WarningMessage().Create(tile, "Already something here");
  //          worker.isBusy = false;
  //          return;
  //      }

  //      TerrainData td = world.GetTerrainDataAt(tile);
  //      TerrainType type = td.terrainData.type;

  //      if (type == TerrainType.River)
  //      {
  //          InfoPopUpHandler.WarningMessage().Create(tile, "Not in water...");
  //          worker.isBusy = false;
  //          return;
  //      }

		//bool clearForest = type == TerrainType.Forest || type == TerrainType.ForestHill;
  //      world.RemoveQueueItemCheck(tile);
  //      int totalTime = cityBuildingTime;
		//worker.buildingCity = true;

		//if (clearForest)
  //      {
  //          if (world.scottFollow)
  //              worker.PrepareScottForestClear();
  //          else
		//		InfoPopUpHandler.WarningMessage().Create(tile, "Can't clear forest...");
		//}
  //      else
  //      {
  //          world.SetWorkerWorkLocation(tile);
		//    timePassed = totalTime;
		//    taskCoroutine = StartCoroutine(BuildCityCoroutine(tile, worker, td, totalTime));   
  //      }   
  //  }

 //   public void LoadBuildCityCoroutine(int timePassed, Vector3Int workerTile, Worker worker)
 //   {
	//	TerrainData td = world.GetTerrainDataAt(workerTile);
	//	TerrainType type = td.terrainData.type;
	//	bool clearForest = type == TerrainType.Forest || type == TerrainType.ForestHill;
 //       int totalTime = cityBuildingTime;
	//	worker.buildingCity = true;

	//	if (clearForest)
 //       {
	//		worker.PrepareScottForestClear();
 //       }
	//	else
 //       {
 //   		world.SetWorkerWorkLocation(workerTile);
 //           this.timePassed = timePassed;
 //           taskCoroutine = StartCoroutine(BuildCityCoroutine(workerTile, worker, td, totalTime));
 //       }
	//}

    //private IEnumerator BuildCityCoroutine(Vector3Int workerTile, Worker worker, TerrainData td, int totalTime)
    //{
    //    worker.ShowProgressTimeBar(totalTime);
    //    worker.SetGatherAnimation(true);
    //    worker.SetTime(timePassed);

    //    while (timePassed > 0)
    //    {
    //        yield return oneSecondWait;
    //        timePassed--;
    //        worker.SetTime(timePassed);
    //    }

    //    taskCoroutine = null;
    //    worker.HideProgressTimeBar();
    //    worker.SetGatherAnimation(false);
    //    if (worker.isSelected)
    //        TurnOffCancelTask();
    //    BuildCity(workerTile, worker, world.scott.clearedForest, td);
    //    world.RemoveWorkerWorkLocation(workerTile);
    //}

    public void BuildCity(Vector3Int workerTile, Worker worker, bool clearForest, TerrainData td)
    {
        worker.isBusy = false;
        worker.buildingCity = false;

        td.ShowProp(false);
        td.FloodPlainCheck(true);

        GameObject newCity = Instantiate(cityData.prefab, workerTile, Quaternion.identity); 
        newCity.transform.SetParent(world.cityHolder, false);
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
            //td.ShowProp(false);
			//worker.marker.ToggleVisibility(false);
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

			//city.SetNewTerrainData(td);
            //td.SetNewData(tempData);
            //GameLoader.Instance.gameData.allTerrain[workerTile] = td.SaveData();
			city.ResourceManager.AddResource(ResourceType.Lumber, world.scott.clearedForestlumberAmount);
            world.scott.clearedForest = false;
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
    //private bool CheckForNearbyCity(Vector3 workerPos)
    //{
    //    Vector3Int workerTile = Vector3Int.RoundToInt(workerPos);
    //    int i = 0;
    //    foreach (Vector3Int tile in world.GetNeighborsFor(workerTile, MapWorld.State.CITYRADIUS))
    //    {
    //        if (i < 8 && world.IsTradeCenterOnTile(tile))
    //        {
    //            InfoPopUpHandler.WarningMessage().Create(world.mainPlayer.transform.position, "Too close to another city");
    //            return true;
    //        }
    //        i++;
    //        if (!world.IsCityOnTile(tile))
    //        {
    //            continue;
    //        }
    //        else
    //        {
    //            InfoPopUpHandler.WarningMessage().Create(world.mainPlayer.transform.position, "Too close to another city");
    //            return true;
    //        }
    //    }

    //    return false;
    //}

    public void ForceCancelWorkerTask()
    {
        if (world.mainPlayer.isSelected)
        {
            CancelTask();
        }
        else
        {
            CancelingTask();
		}
    }

    public void CancelTask()
    {
        if (!world.mainPlayer.isSelected)
            return;

        if (world.mainPlayer.runningAway)
            return;
        
        if (world.unitOrders)
        {
            unitMovement.ToggleOrderHighlights(false);
            unitMovement.ClearBuildRoad();
			uiBuildingSomething.ToggleVisibility(false);
			unitMovement.ResetOrderFlags();
        }

        if (world.mainPlayer.isBusy)
            unitMovement.ToggleOrderHighlights(false);

        CancelingTask();
        TurnOffCancelTask();
    }

    public void CancelingTask()
    {
		if (world.scott.isMoving)
		{
			world.scott.WorkerOrdersPreparations();
		}

		if (world.mainPlayer.isMoving)
		{
			world.mainPlayer.StopMovement();
		}

		if (world.scott.clearingForest)
		{
			Vector3Int pos = world.GetClosestTerrainLoc(world.mainPlayer.transform.position);
			world.GetTerrainDataAt(pos).beingCleared = false;
			GameLoader.Instance.gameData.allTerrain[pos].beingCleared = false;
		}

		world.scott.SetWorkAnimation(false);
		world.mainPlayer.SetGatherAnimation(false);
		world.scott.SetGatherAnimation(false);

		world.mainPlayer.TaskCoCheck();
		world.scott.TaskCoCheck();

		world.mainPlayer.HideProgressTimeBar();
		world.scott.HideProgressTimeBar();
		world.RemoveWorkerWorkLocation(world.GetClosestTerrainLoc(world.mainPlayer.transform.position));
		world.RemoveWorkerWorkLocation(world.GetClosestTerrainLoc(world.scott.transform.position));

		world.scott.clearingForest = false;
		world.scott.ResetOrderQueue();
		world.mainPlayer.isBusy = false;
		world.scott.removing = false;
		world.scott.building = false;
		world.mainPlayer.gathering = false;
		world.scott.gathering = false;
		world.mainPlayer.buildingCity = false;
	}

    public void TurnOffCancelTask()
    {
        world.unitMovement.uiCancelTask.ToggleVisibility(false);
    }

    //public void NullWorkerUnit()
    //{
    //    workerUnit = null;
    //}
}
