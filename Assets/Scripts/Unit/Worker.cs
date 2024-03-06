using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

public class Worker : Unit
{
    //[HideInInspector]
    //public bool harvested;// harvesting, resourceIsNotNull;
    //private ResourceIndividualHandler resourceIndividualHandler;
    private WorkerTaskManager workerTaskManager;
    private Vector3Int resourceCityLoc;
    private Resource resource;
    //private TimeProgressBar timeProgressBar;
    private UITimeProgressBar uiTimeProgressBar;
    private List<Vector3Int> orderList = new();
    public List<Vector3Int> OrderList { get { return orderList; } }
    private Queue<Vector3Int> orderQueue = new();
    [HideInInspector]
    public bool building, removing, gathering, clearingForest, buildingCity, working, clearedForest, stepAside;
    public int clearingForestTime = 1, cityBuildingTime = 2, roadBuildingTime = 1, roadRemovingTime = 2;
    public int clearedForestlumberAmount = 100;
    public AudioClip[] gatheringClips;
    [HideInInspector]
	public int timePassed;

	//for building roads (costs)
	public UtilityCostSO currentUtilityCost;

    //for koa's inventory
    [HideInInspector]
    public PersonalResourceManager personalResourceManager;
	public UIPersonalResourceInfoPanel uiPersonalResourceInfoPanel;
	[HideInInspector]
	public Dictionary<ResourceType, int> resourceGridDict = new(); //order of resources shown

	//animations
	private int isWorkingHash, isGatheringHash, isFallingHash, isDizzyHash;

    private WaitForSeconds oneSecondWait = new(1);
    public Coroutine workingCo, taskCo;
    private WaitForSeconds workingWait = new(0.6111f); //for sound effects

    //[SerializeField]
    //private ParticleSystem removeSplash;

    private void Awake()
    {
        AwakeMethods();
        worker = this;
        isWorkingHash = Animator.StringToHash("isWorking");
        isGatheringHash = Animator.StringToHash("isGathering");
        isFallingHash = Animator.StringToHash("isFalling");
        isDizzyHash = Animator.StringToHash("isDizzy");
        //isWorker = true;
        workerTaskManager = FindObjectOfType<WorkerTaskManager>();
        //resourceIndividualHandler = FindObjectOfType<ResourceIndividualHandler>();
        //timeProgressBar = Instantiate(GameAssets.Instance.timeProgressPrefab, transform.position, Quaternion.Euler(90, 0, 0)).GetComponent<TimeProgressBar>();
        uiTimeProgressBar = Instantiate(GameAssets.Instance.uiTimeProgressPrefab, transform.position, Quaternion.Euler(90, 0, 0)).GetComponent<UITimeProgressBar>();
        personalResourceManager = GetComponent<PersonalResourceManager>();
		if (personalResourceManager != null)
			personalResourceManager.resourceStorageLimit = buildDataSO.cargoCapacity;
	}

    protected override void AwakeMethods()
    {
        base.AwakeMethods();
        //removeSplash = Instantiate(removeSplash, new Vector3(0, 0, 0), Quaternion.Euler(-90,0,0));
        //removeSplash.Stop();
    }

	private void Start()
	{
        if (personalResourceManager != null)
			personalResourceManager.SetUnit(this);
	}
	//public void PlaySplash(Vector3 loc)
	//{
	//    removeSplash.transform.position = loc;
	//    removeSplash.Play();
	//}

	#region gameBegin
	public void ToggleDizzy(bool v)
    {
        unitAnimator.SetBool(isDizzyHash, v);
    }

    public void ToggleFalling(bool v)
    {
        unitAnimator.SetBool(isFallingHash, v);
    }

    public IEnumerator FallingCoroutine(Vector3 pos)
    {
        bool playedOnce = false;
        while (transform.position.y > 0)
        {
            if (!playedOnce && transform.position.y < 5)
            {
                playedOnce = true;
                world.cityBuilderManager.PlayThudAudio();
            }
            
            yield return null;
        }

		Physics.gravity = new Vector3(0, -100, 0);
		transform.position = pos;
        Vector3 camPos = Camera.main.transform.position;
        camPos.y = 0;
        ToggleFalling(false);
        Rotate(camPos);

        yield return new WaitForSeconds(0.25f);

        StartCoroutine(DizzyCoroutine());
    }

    private IEnumerator DizzyCoroutine()
    {
        Vector3 loc = Vector3.zero;
        loc.y += 2.5f;
        Quaternion rotation = Quaternion.Euler(90, 0, 90);
        GameObject dizzy = Instantiate(world.dizzyMarker, loc, rotation);
        dizzy.transform.SetParent(transform, false);

        ToggleDizzy(true);
        
        yield return new WaitForSeconds(3);

        Destroy(dizzy);
		ToggleDizzy(false);
        world.playerInput.paused = false;
        SetSomethingToSay("just_landed");
    }
	#endregion

	public void SetWorkAnimation(bool v)
    {
        unitAnimator.SetBool(isWorkingHash, v);

        if (v)
        {
            working = true;
            workingCo = StartCoroutine(PlayWorkSound());
        }
        else
        {
            if (workingCo != null)
                StopCoroutine(workingCo);

            working = false;
            workingCo = null;
        }
    }

    public void SetGatherAnimation(bool v)
    {
        unitAnimator.SetBool(isGatheringHash, v);

        if (v)
        {
            working = true;
            workingCo = StartCoroutine(PlayGatherSound());
        }
        else
        {
            if (workingCo != null)
                StopCoroutine(workingCo);

            working = false;
            workingCo = null;
        }
    }

    private IEnumerator PlayWorkSound()
    {
        while (working)
        {
            yield return workingWait;
        
            audioSource.clip = attacks[UnityEngine.Random.Range(0, attacks.Length)];
            audioSource.Play();

            yield return workingWait;
        }
    }

    private IEnumerator PlayGatherSound()
    {
        while (working)
        {
            yield return new WaitForSeconds(0.45f);

            audioSource.clip = gatheringClips[UnityEngine.Random.Range(0, attacks.Length)];
            audioSource.Play();

            yield return new WaitForSeconds(0.45f);
        }
    }

    public override void SendResourceToCity()
    {
        //isBusy = false;
        //(City city, ResourceIndividualSO resourceIndividual) = resourceIndividualHandler.GetResourceGatheringDetails();
        world.CreateLightBeam(transform.position);
        
        int gatheringAmount;
        if (clearingForest)
            gatheringAmount = 100;
        else
            gatheringAmount = resource.resourceIndividual.ResourceGatheringAmount;

        //world.cityBuilderManager.PlayRingAudio();
		StartCoroutine(resource.SendResourceToCity(gatheringAmount));
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
        foreach (Vector3Int tile in orderList)
        {
            world.GetTerrainDataAt(tile).DisableHighlight();
        }
        orderList.Clear();
        orderQueue.Clear();
    }

    public void RemoveFromOrderQueue()
    {
        Vector3Int workerTile = orderQueue.Dequeue();
        orderList.Remove(workerTile);

        //return workerTile;
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
        FinishedMoving.RemoveListener(RemoveRoad);
        FinishedMoving.RemoveListener(GatherResourceListener);

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

        GoToPosition(world.GetClosestTerrainLoc(world.mainPlayer.transform.position), true);
        //StopMovement();
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
            //isBusy = false;
            world.mainPlayer.isBusy = false;
            if (world.mainPlayer.isSelected)
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

            if (world.mainPlayer.isSelected)
                workerTaskManager.TurnOffCancelTask();
        }
    }

    public void RoadHighlightCheck()
    {
        foreach (Vector3Int loc in orderList)
        {
            foreach (Road road in world.GetAllRoadsOnTile(loc))
            {
                if (road != null)
                    road.MeshFilter.gameObject.SetActive(true);
            }
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
			world.mainPlayer.isBusy = false;
            if (world.mainPlayer.isSelected)
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

    public void SkipRoadBuild()
    {
        RemoveFromOrderQueue();
        FinishedMoving.RemoveListener(BuildRoad);

        if (MoreOrdersToFollow())
        {
            BeginBuildingRoad();
        }
        else
        {
			world.mainPlayer.isBusy = false;
            world.mainPlayer.StopMovement();

			world.scott.GoToPosition(world.GetClosestTerrainLoc(world.mainPlayer.transform.position), true);
			if (world.azaiFollow)
				world.azai.GoToPosition(world.GetClosestTerrainLoc(world.mainPlayer.transform.position), false);

			if (world.mainPlayer.isSelected)
                workerTaskManager.TurnOffCancelTask();
        }
    }

	private void BuildRoad()
    {
        //unitAnimator.SetBool(isWorkingHash, true);

        Vector3Int workerTile = orderQueue.Peek();
        //orderList.Remove(workerTile);

        FinishedMoving.RemoveListener(BuildRoad);
        //Vector3 workerPos = transform.position;
        //Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);
        if (world.mainPlayer.isSelected)
            world.GetTerrainDataAt(workerTile).DisableHighlight();

		if (!world.IsTileOpenCheck(workerTile))
		{
			if (world.mainPlayer.isSelected)
				world.GetTerrainDataAt(workerTile).DisableHighlight();
			SkipRoadBuild();
			return;
		}

		RemoveFromOrderQueue();
		world.SetWorkerWorkLocation(workerTile);
		world.RemoveQueueItemCheck(workerTile);
		timePassed = world.roadManager.roadBuildingTime;

		taskCo = StartCoroutine(BuildRoad(workerTile)); //specific worker (instead of workerUnit) to allow concurrent build
		//workerTaskManager.BuildRoad(workerTile, this);
    }

	public IEnumerator BuildRoad(Vector3Int roadPosition)
	{
		ShowProgressTimeBar(roadBuildingTime);
		SetWorkAnimation(true);
		SetTime(timePassed);

		while (timePassed > 0)
		{
			yield return oneSecondWait;
			timePassed--;
			SetTime(timePassed);
		}

		taskCo = null;
		HideProgressTimeBar();
		SetWorkAnimation(false);

		if (world.GetTerrainDataAt(roadPosition).straightRiver)
			world.mainPlayer.SetResources(currentUtilityCost.bridgeCost, true, roadPosition);
		else
			world.mainPlayer.SetResources(currentUtilityCost.utilityCost, true, roadPosition);

		world.roadManager.BuildRoadAtPosition(roadPosition, currentUtilityCost.utilityLevel);
		world.RemoveWorkerWorkLocation(roadPosition);

		//moving worker up a smidge to be on top of road
		Vector3 moveUp = transform.position;
		moveUp.y += .2f;
		transform.position = moveUp;

		if (MoreOrdersToFollow())
		{
			BeginBuildingRoad();
		}
		else
		{
			world.mainPlayer.isBusy = false;
			building = false;
			if (world.mainPlayer.isSelected)
				workerTaskManager.TurnOffCancelTask();
		}
	}

	private void SetResources(List<ResourceValue> costs, bool spend, Vector3Int loc)
	{
		if (spend)
		{
			for (int i = 0; i < costs.Count; i++)
			{
				personalResourceManager.SubtractResource(costs[i].resourceType, costs[i].resourceAmount);
				Vector3 loc2 = loc;
				loc2.y += -.4f * i;
				InfoResourcePopUpHandler.CreateResourceStat(loc2, costs[i].resourceAmount * -1, ResourceHolder.Instance.GetIcon(costs[i].resourceType));
			}
		}
		else
		{
			for (int i = 0; i < costs.Count; i++)
			{
				if (!resourceGridDict.ContainsKey(costs[i].resourceType))
					world.mainPlayer.AddToGrid(costs[i].resourceType);

				int amount = costs[i].resourceAmount;
				int storageSpace = personalResourceManager.resourceStorageLimit - personalResourceManager.ResourceStorageLevel;
				int wasted = 0;

				if (storageSpace < amount)
				{
					wasted = amount - storageSpace;
					amount = storageSpace;
				}

				personalResourceManager.AddResource(costs[i].resourceType, amount);
				if (wasted > 0)
				{
					Vector3 loc2 = loc;
					loc2.y += -.4f * i;
					InfoResourcePopUpHandler.CreateResourceStat(loc2, wasted, ResourceHolder.Instance.GetIcon(costs[i].resourceType), true);
				}
				else
				{
					Vector3 loc2 = loc;
					loc2.y += -.4f * i;
					InfoResourcePopUpHandler.CreateResourceStat(loc2, costs[i].resourceAmount, ResourceHolder.Instance.GetIcon(costs[i].resourceType));
				}
			}
		}
	}

	public void RemoveRoad()
    {
        Vector3Int workerTile = orderQueue.Dequeue();
        orderList.Remove(workerTile);

        //Vector3 workerPos = transform.position;
        //Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);
        FinishedMoving.RemoveListener(RemoveRoad);

        if (world.mainPlayer.isSelected)
            world.GetTerrainDataAt(workerTile).DisableHighlight();
        //bool isHill = td.terrainData.isHill;
        foreach (Road road in world.GetAllRoadsOnTile(workerTile))
        {
            if (road == null)
                continue;
            road.MeshFilter.gameObject.SetActive(false);
            road.SelectionHighlight.DisableHighlight();
        }

        if (!world.IsRoadOnTerrain(workerTile))
        {
            InfoPopUpHandler.WarningMessage().Create(workerTile, "No road here");
            return;
        }

        if (world.IsCityOnTile(workerTile) || world.IsWonderOnTile(workerTile) || world.IsTradeCenterOnTile(workerTile))
        {
            InfoPopUpHandler.WarningMessage().Create(workerTile, "Can't remove this");
            return;
        }

		//StopMovement();
		//isBusy = true;

		if (!world.IsRoadOnTerrain(workerTile))
		{
			UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "No road here");
			SkipRoadRemoval();
			return;
		}
		else if (world.IsCityOnTile(workerTile) || world.IsWonderOnTile(workerTile) || world.IsTradeCenterOnTile(workerTile))
		{
			UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't remove this");
			SkipRoadRemoval();
			return;
		}

		world.SetWorkerWorkLocation(workerTile);
		timePassed = world.roadManager.roadRemovingTime;
		taskCo = StartCoroutine(RemoveRoad(workerTile));

		//workerTaskManager.RemoveRoad(workerTile, this);
    }

	public IEnumerator RemoveRoad(Vector3Int tile)
	{
		ShowProgressTimeBar(roadRemovingTime);
		SetWorkAnimation(true);
		SetTime(timePassed);

		while (timePassed > 0)
		{
			yield return oneSecondWait;
			timePassed--;
			SetTime(timePassed);
		}

		taskCo = null;

		int level = world.GetRoadLevel(tile);
		if (world.GetTerrainDataAt(tile).straightRiver)
			world.mainPlayer.SetResources(world.roadManager.roadCostDict[level].bridgeCost, false, tile);
		else
			world.mainPlayer.SetResources(world.roadManager.roadCostDict[level].utilityCost, false, tile);
		HideProgressTimeBar();
		SetWorkAnimation(false);
		world.roadManager.RemoveRoadAtPosition(tile);
		world.RemoveWorkerWorkLocation(tile);

		foreach (Vector3Int loc in world.GetNeighborsFor(tile, MapWorld.State.EIGHTWAYINCREMENT))
		{
			if (world.IsTradeCenterOnTile(loc)/* || world.IsCityOnTile(loc)*/)
			{
				int i = 0;

				foreach (Vector3Int pos in world.GetNeighborsFor(loc, MapWorld.State.EIGHTWAYINCREMENT))
				{
					if (world.IsRoadOnTerrain(pos))
					{
						i++;
						break;
					}
				}

				if (i == 0)
					world.roadManager.RemoveRoadAtPosition(loc);
			}
		}

		if (MoreOrdersToFollow())
		{
			RoadHighlightCheck();
			BeginRoadRemoval();
		}
		else
		{
			world.mainPlayer.isBusy = false;
			removing = false;
			if (world.mainPlayer.isSelected)
				workerTaskManager.TurnOffCancelTask();
		}
	}

	public void GatherResource()
    {
		if (world.unitMovement.moveUnit)
			world.unitMovement.CancelMove();

		world.unitMovement.LoadUnloadFinish(true);

		Vector3 workerPos = transform.position;
        workerPos.y = 0;
        Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);

		StopMovement();
		if (world.scottFollow)
		{
			world.scott.StopMovement();

			if (world.azaiFollow)
				world.azai.GoToPosition(workerTile, false);
    			//world.azai.StopMovement();
		}

		if (!world.IsTileOpenCheck(workerTile))
        {
            InfoPopUpHandler.WarningMessage().Create(workerPos, "Harvest on open tile");
            return;
        }

        if (!CheckForCity(workerTile))
        {
            InfoPopUpHandler.WarningMessage().Create(workerPos, "No nearby city");
            return;
        }

        City city = world.GetCity(resourceCityLoc);
        ResourceType type = world.GetTerrainDataAt(workerTile).resourceType;
        if (type == ResourceType.None)
        {
            InfoPopUpHandler.WarningMessage().Create(workerPos, "No resource to harvest here");
            return;
        }
        ResourceIndividualSO resourceIndividual = ResourceHolder.Instance.GetData(type);

        if (world.scottFollow)
			PrepareScottGather();

        isBusy = true;
        gathering = true;
        GatherResourceTask(workerPos, city, resourceIndividual, false);
    }

    private void GatherResourceTask(Vector3 workerPos, City city, ResourceIndividualSO resourceIndividual, bool clearForest)
    {
		if (isPlayer)
            world.SetWorkerWorkLocation(world.GetClosestTerrainLoc(workerPos));
		if (world.mainPlayer.isSelected)
			world.unitMovement.uiCancelTask.ToggleVisibility(true);
		if (clearForest)
			timePassed = world.scott.clearingForestTime;
		else
			timePassed = resourceIndividual.ResourceGatheringTime;
		taskCo = StartCoroutine(GenerateHarvestedResource(workerPos, this, city, resourceIndividual, clearForest));
	}

	public IEnumerator GenerateHarvestedResource(Vector3 unitPos, Worker worker, City city, ResourceIndividualSO resourceIndividual, bool clearForest)
	{
		//int timePassed;
		Vector3Int pos = world.GetClosestTerrainLoc(unitPos);

		if (clearForest)
		{
			//timePassed = worker.clearingForestTime;
			worker.ShowProgressTimeBar(worker.clearingForestTime);
		}
		else
		{
			//timePassed = resourceIndividual.ResourceGatheringTime;
			worker.ShowProgressTimeBar(resourceIndividual.ResourceGatheringTime);
		}

		if (clearForest)
			worker.SetWorkAnimation(true);
		else
			worker.SetGatherAnimation(true);

		worker.SetTime(timePassed);

		while (timePassed > 0)
		{
			yield return oneSecondWait;
			timePassed--;
			worker.SetTime(timePassed);
		}

		worker.taskCo = null;
		worker.HideProgressTimeBar();
		if (clearForest)
			worker.SetWorkAnimation(false);
		else
			worker.SetGatherAnimation(false);

		if (world.mainPlayer.isSelected)
			workerTaskManager.TurnOffCancelTask();

		if (clearForest)
		{
			worker.clearingForest = false;
			//otherWorker.clearingForest = false;
			TerrainData td = world.GetTerrainDataAt(pos);
			td.beingCleared = false;
			td.ShowProp(false);
			//Destroy(td.treeHandler.gameObject);
			worker.marker.ToggleVisibility(false);
			//otherWorker.marker.ToggleVisibility(false);
			TerrainDataSO tempData;

			if (td.isHill)
			{
				tempData = td.terrainData.grassland ? world.grasslandHillTerrain : world.desertHillTerrain;
			}
			else
			{
				tempData = td.terrainData.grassland ? world.grasslandTerrain : world.desertTerrain;
			}

			td.SetNewData(tempData);
			GameLoader.Instance.gameData.allTerrain[pos].beingCleared = false;
			GameLoader.Instance.gameData.allTerrain[pos] = td.SaveData();

			if (!world.mainPlayer.buildingCity)
				city.UpdateCityBools(ResourceType.Lumber);
		}
		//else
		//{
		//    if (otherWorker.isMoving)
		//    {
		//        otherWorker.WorkerOrdersPreparations();
		//    }
		//    else
		//    {
		//        otherWorker.SetGatherAnimation(false);
		//    }
		//}

		//showing harvested resource
		worker.gathering = false;
		//otherWorker.gathering = false;

		if (world.mainPlayer.buildingCity)
		{
			worker.clearedForest = true;
			world.RemoveWorkerWorkLocation(pos);
			workerTaskManager.BuildCityPrep();
		}
		else
		{
			worker.harvested = true;
			worker.harvestedForest = clearForest;
			unitPos.y += 1.5f;
			GameObject resourceGO = Instantiate(GameAssets.Instance.resourceBubble, unitPos, Quaternion.Euler(90, 0, 0));
			GameLoader.Instance.textList.Add(resourceGO);
			Resource resource = resourceGO.GetComponent<Resource>();
			resource.SetSprites(resourceIndividual.resourceIcon);
			resource.SetInfo(worker, city, resourceIndividual, clearForest);
			Vector3 localScale = resourceGO.transform.localScale;
			resourceGO.transform.localScale = Vector3.zero;
			LeanTween.scale(resourceGO, localScale, 0.25f).setEase(LeanTweenType.easeOutBack);
		}
	}

	public void ClearForest()
    {
		if (world.unitMovement.moveUnit)
			world.unitMovement.CancelMove();

		Vector3 workerPos = transform.position;
		workerPos.y = 0;
		Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);

		if (!world.IsTileOpenButRoadCheck(workerTile))
		{
			InfoPopUpHandler.WarningMessage().Create(workerPos, "Must be open tile");
			return;
		}

		if (!CheckForCity(workerTile))
		{
			InfoPopUpHandler.WarningMessage().Create(workerPos, "No nearby city");
			return;
		}

        if (world.GetTerrainDataAt(workerTile).terrainData.type != TerrainType.Forest && world.GetTerrainDataAt(workerTile).terrainData.type != TerrainType.ForestHill)
        {
			InfoPopUpHandler.WarningMessage().Create(workerPos, "Nothing to clear here");
			return;
		}

        //clearingForest = true;
		//City city = world.GetCity(resourceCityLoc);
		ResourceIndividualSO resourceIndividual = ResourceHolder.Instance.GetData(world.GetTerrainDataAt(workerTile).resourceType);

		StopMovement();
        PrepareScottForestClear(resourceIndividual);
		world.GetTerrainDataAt(workerTile).beingCleared = true;
		GameLoader.Instance.gameData.allTerrain[workerTile].beingCleared = true;

		//SetWorkAnimation(true);
		isBusy = true;
		//workerTaskManager.GatherResource(workerPos, this, city, resourceIndividual, true);
	}

    public void RemoveWorkLocation()
    {
        world.RemoveWorkerWorkLocation(world.GetClosestTerrainLoc(transform.position));
    }

    public void GatherResourceListener()
    {
		FinishedMoving.RemoveListener(GatherResourceListener);

		Vector3Int workerTile = world.RoundToInt(transform.position);
        ResourceType resource = world.GetTerrainDataAt(workerTile).resourceType;

        if (resource == ResourceType.None)
            return;

		ResourceIndividualSO resourceData = ResourceHolder.Instance.GetData(resource);

		City city = world.mainPlayer.buildingCity ? null : world.GetCity(resourceCityLoc);

		//workerTaskManager.GatherResource(workerTile, this, city, resourceData, clearingForest);
		GatherResourceTask(workerTile, city, resourceData, clearingForest);
		//if (clearingForest)
		//      {
		//          Vector3Int workerTile = world.RoundToInt(transform.position);
		//    ResourceIndividualSO resourceData = ResourceHolder.Instance.GetData(world.GetTerrainDataAt(workerTile).resourceType);

		//          City city = world.mainPlayer.buildingCity ? null : world.GetCity(resourceCityLoc);

		//    workerTaskManager.GatherResource(workerTile, this, city, resourceData, true);
		//      }
		//      else
		//      {
		//	Vector3Int workerTile = world.RoundToInt(transform.position);
		//	ResourceIndividualSO resourceData = ResourceHolder.Instance.GetData(world.GetTerrainDataAt(workerTile).resourceType);

		//	City city = world.mainPlayer.buildingCity ? null : world.GetCity(resourceCityLoc);

		//	workerTaskManager.GatherResource(workerTile, this, city, resourceData, false);
		//}
	}

    public void BuildCity()
    {
        Vector3 workerPos = transform.position;

		Vector3 lookAtTarget = workerPos;
		lookAtTarget.z -= 1;

        //rotating towards fire
		StartCoroutine(RotateTowardsPosition(lookAtTarget));

		Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);
        FinishedMoving.RemoveListener(BuildCity);

        StopMovement();

		if (CheckForCityOrCenter(workerTile))
		{
			return;
		}
		if (!world.IsTileOpenButRoadCheck(workerTile))
		{
			InfoPopUpHandler.WarningMessage().Create(workerTile, "Already something here");
			return;
		}

		TerrainData td = world.GetTerrainDataAt(workerTile);
		TerrainType type = td.terrainData.type;

		if (type == TerrainType.River)
		{
			InfoPopUpHandler.WarningMessage().Create(workerTile, "Not in water...");
			return;
		}

        isBusy = true;
		bool clearForest = type == TerrainType.Forest || type == TerrainType.ForestHill;
		world.RemoveQueueItemCheck(workerTile);
		int totalTime = cityBuildingTime;
		buildingCity = true;

		if (clearForest)
		{
			if (world.scottFollow)
				PrepareScottForestClear();
			else
				InfoPopUpHandler.WarningMessage().Create(workerTile, "Can't clear forest...");
		}
		else
		{
			world.SetWorkerWorkLocation(workerTile);
			timePassed = totalTime;
			taskCo = StartCoroutine(BuildCityCoroutine(workerTile, td, totalTime));
		}

		//workerTaskManager.BuildCityPreparations(workerTile, this);
	}

	private IEnumerator BuildCityCoroutine(Vector3Int workerTile, TerrainData td, int totalTime)
	{
		ShowProgressTimeBar(totalTime);
		SetGatherAnimation(true);
		SetTime(timePassed);

		while (timePassed > 0)
		{
			yield return oneSecondWait;
			timePassed--;
			SetTime(timePassed);
		}

		taskCo = null;
		HideProgressTimeBar();
		SetGatherAnimation(false);
		if (isSelected)
		{
			world.unitMovement.uiCancelTask.ToggleVisibility(false);
			world.unitMovement.uiWorkerTask.uiLoadUnload.ToggleInteractable(true);
		}

		world.unitMovement.workerTaskManager.BuildCity(workerTile, this, world.scott.clearedForest, td);
		world.RemoveWorkerWorkLocation(workerTile);
	}

	private bool CheckForCityOrCenter(Vector3 workerPos)
	{
		Vector3Int workerTile = Vector3Int.RoundToInt(workerPos);
		int i = 0;
		foreach (Vector3Int tile in world.GetNeighborsFor(workerTile, MapWorld.State.CITYRADIUS))
		{
			if (i < 8 && world.IsTradeCenterOnTile(tile))
			{
				InfoPopUpHandler.WarningMessage().Create(world.mainPlayer.transform.position, "Too close to another city");
				return true;
			}
			i++;

			if (!world.IsCityOnTile(tile) && !world.IsEnemyCityOnTile(tile))
			{
				continue;
			}
			else
			{
				InfoPopUpHandler.WarningMessage().Create(world.mainPlayer.transform.position, "Too close to another city");
				return true;
			}
		}

		return false;
	}

	public bool CheckForCity(Vector3Int workerPos) //finds if city is nearby, returns it (interface? in WorkerTaskManager)
    {
        foreach (Vector3Int tile in world.GetNeighborsFor(workerPos, MapWorld.State.CITYRADIUS))
        {
            if (world.IsCityOnTile(tile))
            {
                resourceCityLoc = tile;
                return true;
            }
        }

        return false;
    }

    public void TaskCoCheck()
    {
        if (taskCo != null)
        {
            StopCoroutine(taskCo);
            taskCo = null;
        }
    }

    public void SetResource(Resource resource)
    {
        this.resource = resource;
    }

    public void ShowProgressTimeBar(int time)
    {
        Vector3 pos = transform.position;
        //pos.z += -1f;
        pos.y += -0.4f;
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

    public void PrepareScottGather()
    {
		Vector3Int currentTerrain = world.GetClosestTerrainLoc(transform.position);
		Vector3Int currentSpot = currentLocation;
		Vector3Int scottSpot = world.RoundToInt(world.scott.transform.position);
		Vector3Int workSpot = currentTerrain;
		bool alreadyThere = false;
		world.scott.resourceCityLoc = resourceCityLoc;
        world.scott.gathering = true;

		if (world.GetClosestTerrainLoc(scottSpot) == currentTerrain)
		{
			workSpot = scottSpot;
			alreadyThere = true;
		}

		if (alreadyThere)
		{
            world.scott.GatherResourceListener();
		}
		else
		{
            List<Vector3Int> neighborList = world.GetNeighborsFor(currentTerrain, MapWorld.State.EIGHTWAY);

            int dist = Mathf.Abs(scottSpot.x - workSpot.x) + Mathf.Abs(scottSpot.z - workSpot.z);
            for (int i = 0; i < neighborList.Count; i++)
            {
                if (neighborList[i] == currentSpot)
                    continue;

                int newDist = Mathf.Abs(scottSpot.x - neighborList[i].x) + Mathf.Abs(scottSpot.z - neighborList[i].z);
                if (newDist < dist)
                {
                    workSpot = neighborList[i];
                    dist = newDist;
                }
            }

			world.scott.FinishedMoving.AddListener(world.scott.GatherResourceListener);
			world.unitMovement.GoStraightToSelectedLocation(workSpot, workSpot, world.scott);
		}
	}

    public void PrepareScottForestClear(ResourceIndividualSO resourceData = null)
    {
        Vector3Int currentTerrain = world.GetClosestTerrainLoc(transform.position);
        Vector3Int currentSpot = currentLocation;
        Vector3Int scottSpot = world.RoundToInt(world.scott.transform.position);
        Vector3Int workSpot = currentTerrain;
        bool alreadyThere = false;
        world.scott.clearingForest = true;
        world.scott.resourceCityLoc = resourceCityLoc;

        if (scottSpot == currentTerrain)
        {
            workSpot = scottSpot;
            alreadyThere = true;
        }

        if (alreadyThere)
        {
            world.scott.GatherResourceListener();
        }
        else
        {
            if (currentSpot == workSpot)
                FindNewSpot(currentSpot, null);

            world.scott.FinishedMoving.AddListener(world.scott.GatherResourceListener);
            world.unitMovement.GoStraightToSelectedLocation(workSpot, workSpot, world.scott);
        }
    }

	public void SetSomethingToSay(string conversationTopic, Worker alternateSpeaker = null)
	{
		conversationHaver.SetSomethingToSay(conversationTopic, alternateSpeaker);
	}

	public void SetSpeechBubble()
	{
		conversationHaver.SetSpeechBubble();
	}

	public void SaidSomething()
	{
		conversationHaver.SaidSomething();
	}

	public void GoToPosition(Vector3Int position, bool scott)
	{
		StopMovement();
		ShiftMovement();
		Vector3Int currentLoc = world.RoundToInt(transform.position);

		if (Mathf.Abs(position.x - currentLoc.x) < 2 && Mathf.Abs(position.z - currentLoc.z) < 2)
			return;

		int i = 0;
		int factor = scott ? 0 : 1;
		Vector3Int finalLoc = currentLoc;
		int dist = 0;

		foreach (Vector3Int tile in world.GetNeighborsFor(position, MapWorld.State.EIGHTWAY))
		{
			i++;

			if (i % 2 == factor)
				continue;

			if (world.IsUnitLocationTaken(tile))
				continue;

			if (i < 3)
			{
				finalLoc = tile;
				dist = Mathf.Abs(currentLoc.x - tile.x) + Mathf.Abs(currentLoc.z - tile.z);
				continue;
			}

			int newDist = Mathf.Abs(currentLoc.x - tile.x) + Mathf.Abs(currentLoc.z - tile.z);
			if (newDist < dist)
			{
				finalLoc = tile;
				newDist = dist;
			}
		}

		List<Vector3Int> path = GridSearch.AStarSearch(world, transform.position, finalLoc, false, false);

		if (path.Count > 0)
		{
			finalDestinationLoc = finalLoc;
			MoveThroughPath(path);
		}
	}

	public void AddToGrid(ResourceType type)
	{
		resourceGridDict[type] = resourceGridDict.Count;
	}

	public void ReshuffleGrid()
	{
		int i = 0;

		//re-sorting
		Dictionary<ResourceType, int> myDict = resourceGridDict.OrderBy(d => d.Value).ToDictionary(x => x.Key, x => x.Value);

		List<ResourceType> types = new List<ResourceType>(myDict.Keys);

		foreach (ResourceType type in types)
		{
			int amount = 0;
			if (personalResourceManager.ResourceDict.ContainsKey(type))
				amount = personalResourceManager.ResourceDict[type];

			if (amount > 0)
			{
				resourceGridDict[type] = i;
				i++;
			}
			else
			{
				resourceGridDict.Remove(type);
			}
		}
	}

	public void StopPlayer()
	{
		if (isMoving)
		{
			StopAnimation();
			ShiftMovement();
			ResetMovementOrders();
		}

		if (world.scott.isMoving)
		{
			world.scott.StopAnimation();
			world.scott.ShiftMovement();
			world.scott.ResetMovementOrders();
		}

		if (world.azai.isMoving)
		{
			world.azai.StopAnimation();
			world.azai.ShiftMovement();
			world.azai.ResetMovementOrders();
		}
	}

	public void StartRunningAway()
	{
		if (!runningAway)
		{
			exclamationPoint.SetActive(true);
			runningAway = true;
			StartCoroutine(RunAway());
		}
	}

	private IEnumerator RunAway()
	{
		//have to do the two following just in case
		pathPositions.Clear();
		isMoving = false;
		yield return new WaitForSeconds(1);

		//finding closest city
		Vector3Int safeTarget = world.startingLoc;

		bool firstOne = true;
		int dist = 0;
		foreach (City city in world.cityDict.Values)
		{
			if (firstOne)
			{
				firstOne = false;
				dist = Mathf.Abs(city.cityLoc.x - currentLocation.x) + Mathf.Abs(city.cityLoc.z - currentLocation.z);
				safeTarget = city.cityLoc;
				continue;
			}

			int newDist = Mathf.Abs(city.cityLoc.x - currentLocation.x) + Mathf.Abs(city.cityLoc.z - currentLocation.z);
			if (newDist < dist)
			{
				safeTarget = city.cityLoc;
				dist = newDist;
			}
		}

		finalDestinationLoc = safeTarget;
		if (world.scottFollow)
			firstStep = true;
		List<Vector3Int> runAwayPath = GridSearch.AStarSearch(world, currentLocation, safeTarget, isTrader, bySea);

		//in case already home
		if (runAwayPath.Count > 0)
			MoveThroughPath(runAwayPath);
	}

	public void StopRunningAway()
	{
		isBusy = false;
		runningAway = false;
		exclamationPoint.SetActive(false);
	}

	public void StepAside(Vector3Int playerLoc, List<Vector3Int> route)
	{
		Vector3Int safeTarget = playerLoc;

		foreach (Vector3Int tile in world.GetNeighborsFor(playerLoc, MapWorld.State.EIGHTWAYINCREMENT))
		{
			if (route != null && route.Contains(tile))
				continue;

			if (world.CheckIfPositionIsValid(tile))
			{
				safeTarget = tile;
				break;
			}
		}

		finalDestinationLoc = safeTarget;
		firstStep = true;
		List<Vector3Int> runAwayPath = GridSearch.AStarSearch(world, currentLocation, safeTarget, isTrader, bySea);

		//in case already there
		if (runAwayPath.Count > 0)
			MoveThroughPath(runAwayPath);
	}

	public void NextToCheck()
	{
		Vector3Int targetArea = pathPositions.Dequeue();
		Vector3Int diff = world.RoundToInt(world.mainPlayer.transform.position) - targetArea;
		List<Vector3Int> potentialAreas = new();

		if (diff.x != 0 && diff.z != 0)
		{
			potentialAreas.Add(targetArea + new Vector3Int(diff.x, 0, 0));
			potentialAreas.Add(targetArea + new Vector3Int(0, 0, diff.z));
		}
		else if (diff.x != 0)
		{
			potentialAreas.Add(targetArea + new Vector3Int(0, 0, diff.x));
			potentialAreas.Add(targetArea + new Vector3Int(0, 0, -diff.x));
		}
		else if (diff.z != 0)
		{
			potentialAreas.Add(targetArea + new Vector3Int(diff.z, 0, 0));
			potentialAreas.Add(targetArea + new Vector3Int(-diff.z, 0, 0));
		}

		potentialAreas.Add(prevTile);
		Vector3Int closestLoc = prevTile;

		bool firstOne = true;
		int dist = 0;
		for (int i = 0; i < potentialAreas.Count; i++)
		{
			if (!world.CheckIfPositionIsValid(potentialAreas[i]))
				continue;

			if (firstOne)
			{
				firstOne = false;
				dist = Math.Abs(prevTile.x - potentialAreas[i].x) + Math.Abs(prevTile.z - potentialAreas[i].z);
				closestLoc = potentialAreas[i];
				continue;
			}

			int newDist = Math.Abs(prevTile.x - potentialAreas[i].x) + Math.Abs(prevTile.z - potentialAreas[i].z);
			if (newDist < dist)
				closestLoc = potentialAreas[i];

			break;
		}

		finalDestinationLoc = closestLoc;
		pathPositions.Enqueue(closestLoc);
	}

	public void FinishMovementPlayer(Vector3 endPosition)
	{
		if (world.tutorialGoing)
			world.TutorialCheck("Finished Movement");

		if (isSelected)
		{
			world.unitMovement.ShowIndividualCityButtonsUI();
			Vector3Int terrainLoc = world.GetClosestTerrainLoc(currentLocation);

			if (world.IsTradeLocOnTile(terrainLoc) && !world.IsWonderOnTile(terrainLoc))
				world.unitMovement.uiWorkerTask.uiLoadUnload.ToggleInteractable(true);
		}

		if (world.IsUnitLocationTaken(currentLocation))
			UnitInWayCheck(endPosition);
		else
			world.AddUnitPosition(currentLocation, this);
	}

	public WorkerData SaveWorkerData()
    {
        WorkerData data = new();

		data.unitNameAndLevel = buildDataSO.unitNameAndLevel;
		data.secondaryPrefab = secondaryPrefab;
		data.position = transform.position;
		data.rotation = transform.rotation;
		data.destinationLoc = destinationLoc;
		data.finalDestinationLoc = finalDestinationLoc;
		data.currentLocation = currentLocation;
        data.prevTile = prevTile;
        data.resourceCityLoc = resourceCityLoc;
		data.prevTerrainTile = prevTerrainTile;
		data.moveOrders = pathPositions.ToList();
		data.isMoving = isMoving;
		data.moreToMove = moreToMove;
        data.somethingToSay = somethingToSay;
        data.conversationTopics = new(conversationHaver.conversationTopics);
        data.isBusy = isBusy;
        data.removing = removing;
        data.building = building;
        data.gathering = gathering;
        data.clearingForest = clearingForest;
        data.clearedForest = clearedForest;
        data.firstStep = firstStep;
        data.buildingCity = buildingCity;
        data.harvested = harvested;
        data.harvestedForest = harvestedForest;
        data.runningAway = runningAway;
        data.stepAside = stepAside;
        data.orderList = orderList;
        data.timePassed = timePassed;

		//personal resource info
        if (isPlayer)
        {
			data.resourceDict = personalResourceManager.ResourceDict;
			data.resourceStorageLevel = personalResourceManager.ResourceStorageLevel;
			data.resourceGridDict = resourceGridDict;
		}

		return data;
    }

    public void LoadWorkerData(WorkerData data)
    {
		secondaryPrefab = data.secondaryPrefab;
		transform.position = data.position;
		transform.rotation = data.rotation;
		destinationLoc = data.destinationLoc;
		finalDestinationLoc = data.finalDestinationLoc;
		currentLocation = data.currentLocation;
        prevTile = data.prevTile;
        resourceCityLoc = data.resourceCityLoc;
		prevTerrainTile = data.prevTerrainTile;
		isMoving = data.isMoving;
        isBusy = data.isBusy;
		moreToMove = data.moreToMove;
        building = data.building;
		removing = data.removing;
        gathering = data.gathering;
        clearingForest = data.clearingForest;
        clearedForest = data.clearedForest;
        firstStep = data.firstStep;
        buildingCity = data.buildingCity;
        harvested = data.harvested;
        harvestedForest = data.harvestedForest;
        orderList = data.orderList;
        runningAway = data.runningAway;
        stepAside = data.stepAside;
		somethingToSay = data.somethingToSay;
        if (runningAway)
            exclamationPoint.SetActive(true);

		orderQueue = new Queue<Vector3Int>(orderList);

		//personal resource info
		if (isPlayer)
		{
			personalResourceManager.ResourceDict = data.resourceDict;
			personalResourceManager.ResourceStorageLevel = data.resourceStorageLevel;
			resourceGridDict = data.resourceGridDict;
		}

		if (!isMoving)
            world.AddUnitPosition(currentLocation, this);

        if (somethingToSay)
        {
			conversationHaver.conversationTopics = new(data.conversationTopics);
            data.conversationTopics.Clear();
            conversationHaver.SetSomethingToSay(conversationHaver.conversationTopics[0]);
        }

		if (isMoving)
		{
			Vector3Int endPosition = world.RoundToInt(finalDestinationLoc);

			if (data.moveOrders.Count == 0)
				data.moveOrders.Add(endPosition);

			MoveThroughPath(data.moveOrders);

            if (removing)
            {
                FinishedMoving.AddListener(RemoveRoad);
            }
            else if (building)
            {
                FinishedMoving.AddListener(BuildRoad);
            }
            else if (clearingForest)
            {
                FinishedMoving.AddListener(GatherResourceListener);
            }
            else if (buildingCity)
            {
				if (!world.scott.clearingForest)
					FinishedMoving.AddListener(BuildCity);
            }
            else if (gathering)
            {
                FinishedMoving.AddListener(GatherResourceListener);
            }
		}
        else if (harvested)
        {
			//geting resource info
			Vector3 workerPos = transform.position;
			workerPos.y = 0;
			Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);

			//getting city
			City city = null;
			foreach (Vector3Int tile in world.GetNeighborsFor(workerTile, MapWorld.State.CITYRADIUS))
			{
				if (!world.IsCityOnTile(tile))
				{
					continue;
				}
				else
				{
					city = world.GetCity(tile);
					break;
				}
			}

            Vector3 unitPos = workerPos;
			unitPos.y += 1.5f;
            ResourceIndividualSO resourceIndividual = ResourceHolder.Instance.GetData(world.GetTerrainDataAt(workerTile).resourceType);
			GameObject resourceGO = Instantiate(GameAssets.Instance.resourceBubble, unitPos, Quaternion.Euler(90, 0, 0));
			GameLoader.Instance.textList.Add(resourceGO);
			Resource resource = resourceGO.GetComponent<Resource>();
			resource.SetSprites(resourceIndividual.resourceIcon);
			resource.SetInfo(this, city, resourceIndividual, harvestedForest);
			Vector3 localScale = resourceGO.transform.localScale;
			resourceGO.transform.localScale = Vector3.zero;
			LeanTween.scale(resourceGO, localScale, 0.25f).setEase(LeanTweenType.easeOutBack);

			//workerTaskManager.resourceIndividualHandler.LoadHarvestedResource(workerPos, ResourceHolder.Instance.GetData(world.GetTerrainDataAt(workerTile).resourceType), city, this, harvestedForest);
        }
        else if (building)
        {
			world.SetWorkerWorkLocation(currentLocation);
			timePassed = data.timePassed;
			taskCo = StartCoroutine(BuildRoad(currentLocation));
			//workerTaskManager.LoadRoadBuildCoroutine(data.timePassed, CurrentLocation, this);
        }
        else if (removing)
        {
			world.SetWorkerWorkLocation(currentLocation);
			timePassed = data.timePassed;
			taskCo = StartCoroutine(RemoveRoad(currentLocation));
			//workerTaskManager.LoadRemoveRoadCoroutine(data.timePassed, CurrentLocation, this);
		}
        else if (gathering || clearingForest)
        {
            //geting resource info
			Vector3 workerPos = transform.position;
			workerPos.y = 0;
			Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);

            //getting city
            City city = null;
			foreach (Vector3Int tile in world.GetNeighborsFor(workerTile, MapWorld.State.CITYRADIUS))
			{
				if (!world.IsCityOnTile(tile))
				{
					continue;
				}
				else
				{
					city = world.GetCity(tile);
                    break;
				}
			}

    		//workerTaskManager.LoadGatherResourceCoroutine(data.timePassed, workerPos, this, city, ResourceHolder.Instance.GetData(world.GetTerrainDataAt(workerTile).resourceType), clearingForest);

			world.SetWorkerWorkLocation(world.GetClosestTerrainLoc(workerPos));
			timePassed = data.timePassed;
			taskCo = StartCoroutine(GenerateHarvestedResource(workerPos, this, city, ResourceHolder.Instance.GetData(world.GetTerrainDataAt(workerTile).resourceType), clearingForest));
		}
        else if (buildingCity)
        {
            if (!world.scott.clearingForest)
            {
				TerrainData td = world.GetTerrainDataAt(currentLocation);
				TerrainType type = td.terrainData.type;
				bool clearForest = type == TerrainType.Forest || type == TerrainType.ForestHill;
				int totalTime = cityBuildingTime;
				buildingCity = true;

				if (clearForest)
				{
					PrepareScottForestClear();
				}
				else
				{
					world.SetWorkerWorkLocation(currentLocation);
					timePassed = data.timePassed;
					taskCo = StartCoroutine(BuildCityCoroutine(currentLocation, td, totalTime));
				}

				//workerTaskManager.LoadBuildCityCoroutine(data.timePassed, CurrentLocation, this);	
			}
        }
        else if (runningAway)
        {
			if (!stepAside)
            {
    			runningAway = false; //gets reset in next method
                StartRunningAway();
            }
		}
	}
}
