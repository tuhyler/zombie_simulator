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
        roadManager = GetComponent<RoadManager>();
    }

    //Methods to run when pressing certain keys
    public void HandleR()
    {
        if (world.mainPlayer.isSelected && world.scottFollow && !world.mainPlayer.sayingSomething && !uiBuildingSomething.activeStatus)
            BuildRoadButton();
    }

    public void HandleB()
    {
        if (world.mainPlayer.isSelected && !world.mainPlayer.sayingSomething && !uiBuildingSomething.activeStatus)
            BuildCityButton();
    }

    public void HandleF()
    {
        if (world.mainPlayer.isSelected && !world.mainPlayer.sayingSomething && !uiBuildingSomething.activeStatus)
            GatherResourceButton();
    }

    public void HandleX()
    {
        if (world.mainPlayer.isSelected && world.scottFollow && !world.mainPlayer.sayingSomething && !uiBuildingSomething.activeStatus)
            RemoveAllPrep();
    }

    public void HandleZ()
    {
        if (world.mainPlayer.isSelected && world.scottFollow && !world.mainPlayer.sayingSomething && !uiBuildingSomething.activeStatus)
            ClearForestButton();
	}

    public void HandleT()
    {
        if (world.mainPlayer.isSelected && uiWorkerHandler.uiLoadUnload.IsInteractable() && world.scottFollow && !world.mainPlayer.sayingSomething && !uiBuildingSomething.activeStatus)
            world.unitMovement.LoadUnloadPrep();
	}

    private void MoveToCenterOfTile(Vector3Int workerTile)
    {
        world.mainPlayer.StopMovementCheck(false);
        unitMovement.GoStraightToSelectedLocation(workerTile, workerTile, world.mainPlayer);
    }

	public void BuildCityButton()
    {
        if (!world.mainPlayer.isBusy && !world.mainPlayer.inEnemyLines && !world.mainPlayer.runningAway)
        {
            BuildCityPrep();	
        }
	}

    public void BuildCityPrep()
    {
        if (world.tutorialGoing)
			unitMovement.uiWorkerTask.GetButton("Build").FlashCheck();

		unitMovement.CancelMove();
		unitMovement.LoadUnloadFinish(true);
        unitMovement.GivingFinish(true);

		world.cityBuilderManager.PlaySelectAudio();
		Vector3 pos = world.mainPlayer.transform.position;
		Vector3Int workerTile = world.GetClosestTerrainLoc(pos);

        if (world.RoundToInt(pos) == workerTile)
		{
			world.mainPlayer.BuildCity();
            
            if (world.scottFollow && world.scott.isMoving)
                world.scott.GoToPosition(workerTile, true);

            //if (world.azaiFollow && world.azai.isMoving)
            //    world.azai.GoToPosition(workerTile, false);
		}
		else
		{
			MoveToCenterOfTile(workerTile);
			world.mainPlayer.FinishedMoving.AddListener(world.mainPlayer.BuildCity);
		}
	}

    public void GatherResourceButton()
    {
        if (!world.mainPlayer.isBusy && !world.mainPlayer.inEnemyLines && !world.mainPlayer.runningAway)
        {
		    world.cityBuilderManager.PlaySelectAudio();
			world.mainPlayer.GatherResource();
        }
	}

    public void BuildUtilityButton()
    {
		if (!world.mainPlayer.isBusy  && !world.mainPlayer.inEnemyLines && !world.mainPlayer.runningAway)
        {
            world.cityBuilderManager.PlaySelectAudio();
		    world.buildingRoad = true;
		    uiBuildingSomething.SetText("Building Road");
		    OrdersPrep();
			world.scott.WorkerOrdersPreparations();
        }
	}

    public void BuildRoadButton()
    {
		if (!world.mainPlayer.isBusy && !world.mainPlayer.inEnemyLines && !world.mainPlayer.runningAway)
		{
			world.cityBuilderManager.PlaySelectAudio();
			world.buildingRoad = true;
			uiBuildingSomething.SetText("Building Road");
			OrdersPrep();
			world.scott.WorkerOrdersPreparations();
		}
	}

    public void BuildWaterButton()
    {
		if (!world.mainPlayer.isBusy && !world.mainPlayer.inEnemyLines && !world.mainPlayer.runningAway)
		{
			world.cityBuilderManager.PlaySelectAudio();
			//unitMovement.buildingRoad = true;
			uiBuildingSomething.SetText("Building Aqueduct");
			//OrdersPrep();
			//world.scott.WorkerOrdersPreparations();
		}
	}

    public void BuildPowerButton()
    {
		if (!world.mainPlayer.isBusy && !world.mainPlayer.inEnemyLines && !world.mainPlayer.runningAway)
		{
			world.cityBuilderManager.PlaySelectAudio();
			//unitMovement.buildingRoad = true;
			uiBuildingSomething.SetText("Building Power Lines");
			//OrdersPrep();
			//world.scott.WorkerOrdersPreparations();
		}
	}


    public void ClearForestButton()
    {
		if (!world.mainPlayer.isBusy && !world.mainPlayer.inEnemyLines && !world.mainPlayer.runningAway)
        {
            world.cityBuilderManager.PlaySelectAudio();
			world.mainPlayer.ClearForest();
        }
    }

    public void RemoveAllPrep()
    {
        if (!world.mainPlayer.isBusy && !world.mainPlayer.inEnemyLines && !world.mainPlayer.runningAway)
        {
			world.cityBuilderManager.PlaySelectAudio();
            world.removing = true;
            world.removingAll = true;
            uiBuildingSomething.SetText("Removing All");
            OrdersPrep();
			world.scott.WorkerOrdersPreparations();
        }
    }

    public void RemoveRoadPrep()
    {
        if (!world.mainPlayer.isBusy && !world.mainPlayer.inEnemyLines && !world.mainPlayer.runningAway)
        {
            world.cityBuilderManager.PlaySelectAudio();
			world.removing = true;
			world.removingRoad = true;
            uiBuildingSomething.SetText("Removing Road");
            OrdersPrep();
			world.scott.WorkerOrdersPreparations();
        }
    }

    public void RemoveLiquidPrep()
    {
        if (!world.mainPlayer.isBusy && !world.mainPlayer.inEnemyLines && !world.mainPlayer.runningAway)
        {
			world.cityBuilderManager.PlaySelectAudio();
			world.removing = true;
			world.removingLiquid = true;
            uiBuildingSomething.SetText("Removing Liquid");
            OrdersPrep();
			world.scott.WorkerOrdersPreparations();
        }
    }

    public void RemovePowerPrep()
    {
        if (!world.mainPlayer.isBusy && !world.mainPlayer.inEnemyLines && !world.mainPlayer.runningAway)
        {
			world.cityBuilderManager.PlaySelectAudio();
			world.removingPower = true;
            uiBuildingSomething.SetText("Removing Power");
            OrdersPrep();
			world.scott.WorkerOrdersPreparations();
        }
    }

    private void OrdersPrep()
    {
        //if (world.moveUnit)
        unitMovement.CancelMove();
        unitMovement.LoadUnloadFinish(true);
        unitMovement.GivingFinish(true);

		uiWorkerHandler.ToggleVisibility(false, world, true);
        uiBuildingSomething.ToggleVisibility(true);

        world.mainPlayer.StopMovementCheck(true);

        if (world.azaiFollow && world.azai.isMoving)
            world.azai.GetBehindScott(world.RoundToInt(transform.position));
            //world.azai.GoToPosition(world.GetClosestTerrainLoc(world.mainPlayer.transform.position), false);

        world.unitOrders = true;
		world.mainPlayer.isBusy = true;
        world.unitMovement.uiCancelTask.ToggleVisibility(true);
        unitMovement.uiMoveUnit.ToggleVisibility(false);
    }

    //for scott to build and remove roads
    public void MoveToCompleteOrders(Vector3Int workerTile, Vector3Int nextWorkerTile, Worker workerUnit)
    {
        Vector3 mainPos = world.mainPlayer.transform.position;
        
        //moving main player to position
        if (Mathf.Abs(mainPos.x - workerTile.x) > 1.2f || Mathf.Abs(mainPos.z - workerTile.z) > 1.2f)
        {
            Vector3Int diff = nextWorkerTile - workerTile;
			Vector3Int finalLoc = workerTile;
            if (diff == Vector3Int.zero)
                diff = workerTile - world.mainPlayer.currentLocation;
			
            if (diff.x > 0)
				diff.x = 1;
			else if (diff.x < 0)
				diff.x = -1;

			if (diff.z > 0)
				diff.z = 1;
			else if (diff.z < 0)
				diff.z = -1;

            finalLoc += diff;
			unitMovement.GoStraightToSelectedLocation(finalLoc, workerTile, world.mainPlayer);
        }
        
		unitMovement.GoStraightToSelectedLocation(workerTile, workerTile, workerUnit);
    }

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
        world.AddCity(workerTile, city);
        city.CheckForAvailableSingleBuilds();

		//clear the forest if building on forest tile
		if (clearForest)
		{
            city.ResourceManager.resourceCount = 0;
			city.ResourceManager.AddResource(ResourceType.Lumber, world.scott.clearedForestlumberAmount);
            world.scott.clearedForest = false;
		}

		//build road where city is placed
		if (!world.IsRoadOnTerrain(workerTile))
        {
            int level = 0;
            foreach (Vector3Int loc in world.GetNeighborsFor(workerTile, MapWorld.State.EIGHTWAYINCREMENT))
            {
                if (world.IsRoadOnTerrain(loc))
                    level = Mathf.Max(level, world.GetRoadLevel(loc));
            }

            if (level > 0)
            {
                //moving worker up a smidge to be on top of road
                Vector3 moveUp = worker.transform.position;
                moveUp.y += .2f;
                worker.transform.position = moveUp;
                roadManager.BuildRoadAtPosition(workerTile, UtilityType.Road, level);
            }
        }
        
        city.LightFire(td.isHill);

        SetCityBools(city, workerTile);

        city.reachedWaterLimit = !city.hasFreshWater;
        city.waterCount = city.hasFreshWater ? 9999 : 0;
        world.AddCityBuildingDict(workerTile);
        world.TutorialCheck("Build City");
        unitMovement.ShowIndividualCityButtonsUI();
    }

    public void SetCityBools(City city, Vector3Int cityLoc)
    {
		foreach (Vector3Int tile in world.GetNeighborsFor(cityLoc, MapWorld.State.CITYRADIUS))
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
	}

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
            unitMovement.uiPersonalResourceInfoPanel.ToggleVisibility(true, world.mainPlayer);
        }

        if (world.mainPlayer.isBusy)
            unitMovement.ToggleOrderHighlights(false);

        CancelingTask();
        TurnOffCancelTask();
    }

    public void CancelingTask()
    {
        //if (world.azaiFollow && world.azai.isMoving)
        //    world.azai.GetBehindScott();
            //world.azai.GoToPosition(world.GetClosestTerrainLoc(world.mainPlayer.transform.position), false);
        
        if (world.scottFollow) 
		{
			if (world.scott.isMoving)
                world.scott.WorkerOrdersPreparations();

			if (world.scott.clearingForest)
			{
				Vector3Int pos = world.GetClosestTerrainLoc(world.mainPlayer.transform.position);
				world.GetTerrainDataAt(pos).beingCleared = false;
				GameLoader.Instance.gameData.allTerrain[pos].beingCleared = false;
			}
		
            world.scott.SetWorkAnimation(false);
            ResetWorker(world.scott);
		    world.scott.clearingForest = false;
		    world.scott.ResetOrderQueue();
		    world.scott.removing = false;
		    world.scott.building = false;
		}

		world.mainPlayer.StopMovementCheck(true);

        ResetWorker(world.mainPlayer);
		world.mainPlayer.isBusy = false;
		world.mainPlayer.buildingCity = false;
	}

    public void ResetWorker(Worker worker)
    {
		worker.SetGatherAnimation(false);
		worker.TaskCoCheck();
		worker.HideProgressTimeBar();
		world.RemoveWorkerWorkLocation();
	    worker.gathering = false;  
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
