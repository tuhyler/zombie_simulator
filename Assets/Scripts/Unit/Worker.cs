using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.FilePathAttribute;
using static UnityEditor.PlayerSettings;
using static UnityEngine.UI.CanvasScaler;

public class Worker : Unit
{
    //[HideInInspector]
    //public bool harvested;// harvesting, resourceIsNotNull;
    //private ResourceIndividualHandler resourceIndividualHandler;
    private WorkerTaskManager workerTaskManager;
	[HideInInspector]
    public Vector3Int resourceCityLoc, prevFriendlyTile;
    private Resource resource;
    //private TimeProgressBar timeProgressBar;
    private UITimeProgressBar uiTimeProgressBar;
    private List<Vector3Int> orderList = new();
    public List<Vector3Int> OrderList { get { return orderList; } }
    private Queue<Vector3Int> orderQueue = new();
    [HideInInspector]
    public bool building, removing, gathering, clearingForest, buildingCity, working, clearedForest, toTransport, inTransport, inEnemyLines, harvested, harvestedForest, runningAway, firstStep, isBusy;
    public int clearingForestTime = 1, cityBuildingTime = 2, roadBuildingTime = 1, roadRemovingTime = 2;
    public int clearedForestlumberAmount = 100;
    public AudioClip[] gatheringClips;
    [HideInInspector]
	public int timePassed;
	[HideInInspector]
	public Transport transportTarget;

	//for building utilities (costs)
	[HideInInspector]
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
		uiTimeProgressBar.transform.SetParent(transform, false);
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
                world.cityBuilderManager.PlaySelectAudio(world.cityBuilderManager.thudClip);
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
        loc.y += 1.25f;
        Quaternion rotation = Quaternion.Euler(90, 0, 90);
        GameObject dizzy = Instantiate(world.dizzyMarker, loc, rotation);
        dizzy.transform.SetParent(transform, false);

        ToggleDizzy(true);
        
        yield return new WaitForSeconds(3);

        Destroy(dizzy);
		ToggleDizzy(false);
        world.playerInput.paused = false;
		Vector3Int startingLoc = world.GetClosestTerrainLoc(transform.position);
		prevTerrainTile = startingLoc;
		lastClearTile = startingLoc;
		currentLocation = startingLoc;
        conversationHaver.SetSomethingToSay("just_landed");
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

	public void PlayRingAudio()
	{
		audioSource.clip = world.cityBuilderManager.ringClip;
		audioSource.Play();
	}

    public void SendResourceToCity()
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
        FinishedMoving.RemoveListener(BuildRoad);
        FinishedMoving.RemoveListener(RemoveRoad);
        FinishedMoving.RemoveListener(GatherResourceListener);

        GoToPosition(world.GetClosestTerrainLoc(world.mainPlayer.transform.position), true);
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
			List<Vector3Int> workList = orderQueue.ToList();
			Vector3Int nextSpot;
			if (workList.Count == 1)
				nextSpot = workList[0];
			else
				nextSpot = workList[1];
			workerTaskManager.MoveToCompleteOrders(orderQueue.Peek(), nextSpot, this);
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
			List<Vector3Int> workList = orderQueue.ToList();
			Vector3Int nextSpot = workList.Count == 1 ? nextSpot = workList[0] : nextSpot = workList[1];
            workerTaskManager.MoveToCompleteOrders(orderQueue.Peek(), nextSpot, this);
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
            world.mainPlayer.StopMovementCheck(true);

			world.scott.GoToPosition(world.GetClosestTerrainLoc(world.mainPlayer.transform.position), true);
			//if (world.azaiFollow)
			//	world.azai.GoToPosition(world.GetClosestTerrainLoc(world.mainPlayer.transform.position), false);

			if (world.mainPlayer.isSelected)
                workerTaskManager.TurnOffCancelTask();
        }
    }

	private void BuildRoad()
    {
		//unitAnimator.SetBool(isWorkingHash, true);
		Vector3Int workerTile = orderQueue.Peek();
		world.CheckTileForTreasure(workerTile);
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

		world.roadManager.BuildRoadAtPosition(roadPosition, currentUtilityCost.utilityType, currentUtilityCost.utilityLevel);
		world.RemoveWorkerWorkLocation();

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
				InfoResourcePopUpHandler.CreateResourceStat(loc2, costs[i].resourceAmount * -1, ResourceHolder.Instance.GetIcon(costs[i].resourceType), world);
			}
		}
		else
		{
			for (int i = 0; i < costs.Count; i++)
			{
				if (!resourceGridDict.ContainsKey(costs[i].resourceType))
					world.mainPlayer.AddToGrid(costs[i].resourceType);

				int amount = costs[i].resourceAmount;
				int storageSpace = personalResourceManager.resourceStorageLimit - personalResourceManager.resourceStorageLevel;
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
					InfoResourcePopUpHandler.CreateResourceStat(loc2, wasted, ResourceHolder.Instance.GetIcon(costs[i].resourceType), world, true);
				}
				else
				{
					Vector3 loc2 = loc;
					loc2.y += -.4f * i;
					InfoResourcePopUpHandler.CreateResourceStat(loc2, costs[i].resourceAmount, ResourceHolder.Instance.GetIcon(costs[i].resourceType), world);
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

        //if (!world.IsRoadOnTerrain(workerTile))
        //{
        //    InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(workerTile, "No road here");
        //    return;
        //}

        //if (world.IsCityOnTile(workerTile) || world.IsWonderOnTile(workerTile) || world.IsTradeCenterOnTile(workerTile))
        //{
        //    InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(workerTile, "Can't remove this");
        //    return;
        //}

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
			world.mainPlayer.SetResources(UpgradeableObjectHolder.Instance.utilityDict[UtilityType.Road][level].bridgeCost, false, tile);
		else
			world.mainPlayer.SetResources(UpgradeableObjectHolder.Instance.utilityDict[UtilityType.Road][level].utilityCost, false, tile);
		HideProgressTimeBar();
		SetWorkAnimation(false);
		world.roadManager.RemoveRoadAtPosition(tile);
		world.RemoveWorkerWorkLocation();

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

	private void AddFollowerLocToWorldCheck()
	{
		if (world.scottFollow)
			world.scott.FinishMoving(world.scott.transform.position);

		if (world.azaiFollow)
			world.azai.FinishMoving(world.azai.transform.position);
	}

	public void GatherResource()
    {
		//if (world.mainPlayer.inEnemyLines || world.mainPlayer.runningAway)
		//	return;
		
		//if (world.moveUnit)
		world.unitMovement.CancelMove();

		world.unitMovement.LoadUnloadFinish(true);
		world.unitMovement.GivingFinish(true);

		Vector3 workerPos = transform.position;
        workerPos.y = 0;
        Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);

		StopMovementCheck(true);
		if (world.scottFollow)
		{
			world.scott.StopMovementCheck(false);

			if (world.azaiFollow)
				world.azai.StopMovementCheck(false);
		}

		if (!world.IsTileOpenCheck(workerTile))
        {
            InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(workerPos, "Harvest on open tile");
			AddFollowerLocToWorldCheck();
            return;
        }

        if (!CheckForCity(workerTile))
        {
            InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(workerPos, "No nearby city");
			AddFollowerLocToWorldCheck();
			return;
        }

        City city = world.GetCity(resourceCityLoc);
        ResourceType type = world.GetTerrainDataAt(workerTile).resourceType;
        if (type == ResourceType.None)
        {
            InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(workerPos, "No resource to harvest here");
			AddFollowerLocToWorldCheck();
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
		Vector3Int pos = world.GetClosestTerrainLoc(unitPos);

		if (clearForest)
			worker.ShowProgressTimeBar(worker.clearingForestTime);
		else
			worker.ShowProgressTimeBar(resourceIndividual.ResourceGatheringTime);

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

		//hiding forest
		if (clearForest)
		{
			worker.clearingForest = false;
			TerrainData td = world.GetTerrainDataAt(pos);
			td.beingCleared = false;
			td.ShowProp(false);
			worker.marker.ToggleVisibility(false);
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

		//showing harvested resource
		worker.gathering = false;

		if (world.mainPlayer.buildingCity)
		{
			worker.clearedForest = true;
			world.RemoveWorkerWorkLocation();
			workerTaskManager.BuildCityPrep();
		}
		else
		{
			worker.harvested = true;
			worker.harvestedForest = clearForest;
			unitPos.y += 1.5f;
			GameObject resourceGO = Instantiate(GameAssets.Instance.resourceBubble, unitPos, Quaternion.Euler(90, 0, 0));
			resourceGO.transform.SetParent(world.objectPoolItemHolder, false);
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
		world.unitMovement.CancelMove();

		Vector3 workerPos = transform.position;
		workerPos.y = 0;
		Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);

		if (!world.IsTileOpenButRoadCheck(workerTile))
		{
			InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(workerPos, "Must be open tile");
			return;
		}

		if (!CheckForCity(workerTile))
		{
			InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(workerPos, "No nearby city");
			return;
		}

        if (world.GetTerrainDataAt(workerTile).terrainData.type != TerrainType.Forest && world.GetTerrainDataAt(workerTile).terrainData.type != TerrainType.ForestHill)
        {
			InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(workerPos, "Nothing to clear here");
			return;
		}

		StopMovementCheck(false);
        PrepareScottForestClear();
		world.GetTerrainDataAt(workerTile).beingCleared = true;
		GameLoader.Instance.gameData.allTerrain[workerTile].beingCleared = true;
		isBusy = true;
	}

    public void RemoveWorkLocation()
    {
        world.RemoveWorkerWorkLocation();
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

		GatherResourceTask(workerTile, city, resourceData, clearingForest);
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

        StopMovementCheck(true);

		if (CheckForCityOrCenter(workerTile))
		{
			return;
		}
		if (!world.IsTileOpenButRoadCheck(workerTile))
		{
			InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(workerTile, "Already something here");
			return;
		}

		TerrainData td = world.GetTerrainDataAt(workerTile);
		TerrainType type = td.terrainData.type;

		if (type == TerrainType.River)
		{
			InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(workerTile, "Not in water...");
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
				InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(workerTile, "Can't clear forest...");
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
		world.RemoveWorkerWorkLocation();
	}

	private bool CheckForCityOrCenter(Vector3 workerPos)
	{
		Vector3Int workerTile = world.RoundToInt(workerPos);
		int i = 0;
		foreach (Vector3Int tile in world.GetNeighborsFor(workerTile, MapWorld.State.CITYRADIUS))
		{
			if (i < 8 && world.IsTradeCenterOnTile(tile))
			{
				InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(world.mainPlayer.transform.position, "Too close to another city");
				return true;
			}
			i++;

			if (!world.IsCityOnTile(tile) && !world.IsEnemyCityOnTile(tile))
			{
				continue;
			}
			else
			{
				InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(world.mainPlayer.transform.position, "Too close to another city");
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
			world.scott.FinishMoving(world.scott.transform.position);
			world.scott.GatherResourceListener();

			if (world.azai.isMoving)
				world.azai.GetBehindScott(scottSpot);
		}
		else
		{
            List<Vector3Int> neighborList = world.GetNeighborsFor(currentTerrain, MapWorld.State.EIGHTWAY);

            int dist = Mathf.Abs(scottSpot.x - workSpot.x) + Mathf.Abs(scottSpot.z - workSpot.z);
            for (int i = 0; i < neighborList.Count; i++)
            {
                if (neighborList[i] == currentSpot)
                    continue;

				if (world.GetTerrainDataAt(neighborList[i]).hasBattle)
					continue;

                int newDist = Mathf.Abs(scottSpot.x - neighborList[i].x) + Mathf.Abs(scottSpot.z - neighborList[i].z);
                if (newDist < dist)
                {
                    workSpot = neighborList[i];
                    dist = newDist;
                }
            }

			world.scott.FinishedMoving.AddListener(world.scott.GatherResourceListener);
			world.unitMovement.GoStraightToSelectedLocation(workSpot, currentTerrain, world.scott);
		}
	}

    public void PrepareScottForestClear()
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
			FinishMoving(transform.position);
        }
        else
        {
            if (currentSpot == workSpot)
                FindNewSpot(currentSpot, null);

            world.scott.FinishedMoving.AddListener(world.scott.GatherResourceListener);
            world.unitMovement.GoStraightToSelectedLocation(workSpot, currentTerrain, world.scott);
        }
    }

	//public void SetSomethingToSay(string conversationTopic, Worker alternateSpeaker = null)
	//{
	//	conversationHaver.SetSomethingToSay(conversationTopic, alternateSpeaker);
	//}

	public void GoToPosition(Vector3Int position, bool diag)
	{
		StopMovementCheck(false);
		Vector3Int currentLoc = world.RoundToInt(transform.position);

		if (Mathf.Abs(position.x - currentLoc.x) < 2 && Mathf.Abs(position.z - currentLoc.z) < 2)
			return;

		int i = 0;
		int factor = diag ? 1 : 0;
		Vector3Int finalLoc = currentLoc;
		int dist = 0;

		bool firstOne = true;
		foreach (Vector3Int tile in world.GetNeighborsFor(position, MapWorld.State.EIGHTWAY))
		{
			i++;

			if (i % 2 == factor)
				continue;

			if (world.IsPlayerLocationTaken(tile))
				continue;

			if (world.GetTerrainDataAt(tile).hasBattle)
				continue;

			if (firstOne)
			{
				firstOne = false;
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

		List<Vector3Int> path = GridSearch.MilitaryMove(world, transform.position, finalLoc, false);

		if (path.Count > 0)
		{
			finalDestinationLoc = finalLoc;
			MoveThroughPath(path);

			if (world.azaiFollow)
			{
				world.azai.StopMovementCheck(false);
				//world.azai.ShiftMovement();

				List<Vector3Int> azaiPath = GridSearch.MilitaryMove(world, world.azai.transform.position, world.RoundToInt(transform.position), false);
				azaiPath.AddRange(path);
				Vector3Int azaiFinalLoc = path[path.Count - 1];
				azaiPath.Remove(azaiFinalLoc);
				if (azaiPath.Count > 0)
				{
					world.azai.finalDestinationLoc = azaiPath[azaiPath.Count - 1];
					world.azai.MoveThroughPath(azaiPath);
				}
				else
				{
					world.azai.FinishMoving(world.azai.transform.position);
				}
			}
		}
		else
		{
			FinishMoving(transform.position);
		}
	}

	//public void FollowScott(List<Vector3Int> scottPath, Vector3 currentLoc)
	//{
	//	List<Vector3Int> azaiPath = GridSearch.MilitaryMove(world, transform.position, world.RoundToInt(currentLoc), false);
	//	scottPath.RemoveAt(scottPath.Count - 1);
	//	azaiPath.AddRange(scottPath);

	//	if (azaiPath.Count > 0)
	//	{
	//		finalDestinationLoc = azaiPath[azaiPath.Count - 1];
	//		MoveThroughPath(azaiPath);
	//	}
	//}

	//public void GetBehindScott(Vector3Int scottSpot)
	//{
	//	Vector3Int currentLoc = world.RoundToInt(transform.position);
	//	int dist = 0;
	//	Vector3Int finalLoc = scottSpot;
	//	bool firstOne = true;
	//	foreach (Vector3Int tile in world.GetNeighborsFor(scottSpot, MapWorld.State.EIGHTWAY))
	//	{
	//		if (firstOne)
	//		{
	//			firstOne = false;
	//			finalLoc = tile;
	//			dist = Math.Abs(tile.x - currentLoc.x) + Math.Abs(tile.z - currentLoc.z);
	//			continue;
	//		}

	//		int newDist = Math.Abs(tile.x - currentLoc.x) + Math.Abs(tile.z - currentLoc.z);
	//		if (newDist < dist)
	//		{
	//			dist = newDist;
	//			finalLoc = tile;
	//		}
	//	}
		
	//	List<Vector3Int> azaiPath = GridSearch.AStarSearch(world, currentLoc, finalLoc, false, false);

	//	if (azaiPath.Count > 0)
	//	{
	//		finalDestinationLoc = finalLoc;
	//		MoveThroughPath(azaiPath);
	//	}
	//}

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
			if (personalResourceManager.resourceDict.ContainsKey(type))
				amount = personalResourceManager.resourceDict[type];

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

	public void HandleSelectedFollowerLoc(Queue<Vector3Int> path, Vector3Int currentSpot, Vector3Int finalSpot)
	{
		//world.moveUnit = false;
		
		world.scott.StopMovementCheck(false);
		world.azai.StopMovementCheck(false);

		Vector3Int currentScottSpot = world.RoundToInt(world.scott.transform.position);
		List<Vector3Int> scottPath;
		if (world.mainPlayer.inEnemyLines)
			scottPath = GridSearch.PlayerMoveExempt(world, currentScottSpot, currentSpot, world.GetExemptList(world.mainPlayer.finalDestinationLoc));
		else
			scottPath = GridSearch.MilitaryMove(world, currentScottSpot, currentSpot, false);
		scottPath.AddRange(path);
		scottPath.Remove(finalSpot);

		Vector3Int finalScottSpot;
		bool scottStays = false;
		if (scottPath.Count > 0)
		{
			finalScottSpot = scottPath[scottPath.Count - 1];
		}
		else
		{
			scottStays = true;
			finalScottSpot = currentScottSpot;
			scottPath.Add(currentScottSpot);
		}
		
		world.scott.finalDestinationLoc = finalScottSpot;
		world.scott.MoveThroughPath(scottPath);

		if (world.azaiFollow && !scottStays)
		{
			Vector3Int currentAzaiSpot = world.RoundToInt(world.azai.transform.position);
			List<Vector3Int> azaiPath;

			if (world.mainPlayer.inEnemyLines)
				azaiPath = GridSearch.PlayerMoveExempt(world, currentAzaiSpot, currentScottSpot, world.GetExemptList(world.mainPlayer.finalDestinationLoc));
			else
				azaiPath = GridSearch.MilitaryMove(world, currentAzaiSpot, currentScottSpot, false);

			azaiPath.AddRange(scottPath);
			azaiPath.Remove(finalScottSpot);

			//world.azai.StopMovementCheck(false);
			if (azaiPath.Count > 0)
			{
				world.azai.finalDestinationLoc = azaiPath[azaiPath.Count - 1];
			}
			else
			{
				world.azai.finalDestinationLoc = currentAzaiSpot;
				azaiPath.Add(currentAzaiSpot);
			}
			world.azai.MoveThroughPath(azaiPath);
		}
	}

	//where scott and azai stand when speaking
	public void SetUpSpeakingPositions(Vector3 pos, bool leader)
	{
		Vector3Int currentPos = world.RoundToInt(transform.position);
		Vector3Int talkingPos = world.RoundToInt(pos);
		int zDiff = currentPos.z - talkingPos.z;
		int xDiff = currentPos.x - talkingPos.x;

		if (world.scottFollow)
		{
			Vector3Int newPos;
			if (Math.Abs(zDiff) + Math.Abs(xDiff) == 2)
			{
				newPos = talkingPos;
				newPos.z += zDiff;
			}
			else
			{
				newPos = currentPos;
				if (zDiff == 0)
					newPos.z += -1;
				else
					newPos.x += -1;
			}

			world.scott.RepositionWorker(newPos, false, leader, world.GetExemptList(talkingPos));
		}

		if (world.azaiFollow)
		{
			Vector3Int newPos;
			if (Math.Abs(zDiff) + Math.Abs(xDiff) == 2)
			{
				newPos = talkingPos;
				newPos.x += xDiff;
			}
			else
			{
				newPos = currentPos;
				if (zDiff == 0)
					newPos.z += 1;
				else
					newPos.x += 1;
			}

			world.azai.RepositionBodyGuard(newPos, false, leader, world.GetExemptList(talkingPos));
		}
	}

	public void CheckToFollow(Vector3Int endPositionInt)
	{
		if (world.scottFollow)
		{
			if (!isBusy && !runningAway)
			{
				worker.HandleSelectedFollowerLoc(pathPositions, endPositionInt, world.RoundToInt(finalDestinationLoc));
			}

			if (runningAway)
			{
				worker.HandleSelectedFollowerLoc(pathPositions, endPositionInt, world.RoundToInt(finalDestinationLoc));
			}
		}
	}

	public void RealignFollowers(Vector3Int newLoc, Vector3Int prevLoc, bool enemy)
	{
		if (world.scottFollow)
		{
			Vector3Int currentLoc = world.RoundToInt(world.scott.transform.position);
			Vector3Int firstLoc = newLoc;
			Vector3Int secondLoc = newLoc;

			bool firstOne = true;
			int dist = 0;
			foreach (Vector3Int tile in world.GetNeighborsFor(newLoc, MapWorld.State.EIGHTWAY))
			{
				if (world.GetTerrainDataAt(tile).hasBattle)
					continue;
				
				if (firstOne)
				{
					firstOne = false;
					firstLoc = tile;
					secondLoc = tile;
					dist = Mathf.Abs(tile.x - currentLoc.x) + Mathf.Abs(tile.z - currentLoc.z);
					continue;
				}

				int newDist = Mathf.Abs(tile.x - currentLoc.x) + Mathf.Abs(tile.z - currentLoc.z);
				if (newDist < dist)
				{
					dist = newDist;
					secondLoc = firstLoc;
					firstLoc = tile;
				}
			}

			HashSet<Vector3Int> exemptList = new();
			if (enemy)
				exemptList = world.GetExemptList(prevLoc);

			world.scott.RepositionWorker(firstLoc, false, enemy, exemptList);
		
			if (world.azaiFollow)
			{
				world.azai.RepositionBodyGuard(secondLoc, false, enemy, exemptList);
			}
		}
	}

	public void RepositionWorker(Vector3Int newPos, bool loading, bool enemy, HashSet<Vector3Int> exemptList = null)
	{
		StopMovementCheck(false);

		List<Vector3Int> path;
		
		if (enemy)
			path = GridSearch.PlayerMoveExempt(world, transform.position, newPos, exemptList);
		else
			path = GridSearch.MilitaryMove(world, transform.position, newPos, false);

		if (path.Count > 0)
		{
			finalDestinationLoc = newPos;
			MoveThroughPath(path);
		}
		else if (loading)
		{
			LoadWorkerInTransport(transportTarget);
		}
		else
		{
			FinishMoving(transform.position);
		}
	}

	public void LoadWorkerInTransport(Transport transport)
	{
		toTransport = false;
		Vector3Int tile = world.RoundToInt(transform.position);
		//bool foundTransport = transport;

		if (transport.isUpgrading)
		{
			InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(tile, "Can't load while upgrading");
			return;
		}

		transport.Load(this);
		gameObject.SetActive(false);
		inTransport = true;
	}

	public void UnloadWorkerFromTransport(Vector3Int tile)
	{
		transform.position = tile;
		gameObject.SetActive(true);
		inTransport = false;
		currentLocation = world.AddPlayerPosition(transform.position, this);
	}

	public void StopPlayer()
	{
		StopMovementCheck(false);
		world.scott.StopMovementCheck(false);
		world.azai.StopMovementCheck(false);
	}

	public void StopRunningAway()
	{
		isBusy = false;
		runningAway = false;
		if (inTransport)
			world.GetKoasTransport().exclamationPoint.SetActive(false);
		else
			exclamationPoint.SetActive(false);
	}

	public void StepAside(Vector3Int playerLoc, HashSet<Vector3Int> route = null)
	{
		Vector3Int safeTarget = playerLoc;

		foreach (Vector3Int tile in world.GetNeighborsFor(playerLoc, MapWorld.State.EIGHTWAYINCREMENT))
		{
			if (route != null && route.Contains(tile))
				continue;

			if (world.PlayerCheckIfPositionIsValid(tile) && !world.CheckIfEnemyTerritory(tile))
			{
				safeTarget = tile;
				break;
			}
		}

		finalDestinationLoc = safeTarget;

		List<Vector3Int> runAwayPath = GridSearch.MilitaryMove(world, currentLocation, safeTarget, false);

		//in case already there
		if (runAwayPath.Count > 0)
			MoveThroughPath(runAwayPath);
		else
			FinishMoving(transform.position);

		RealignFollowers(safeTarget, currentLocation, false);
	}

	public void ReturnToFriendlyTile()
	{
		List<Vector3Int> path;
		Vector3Int currentTerrain = world.GetClosestTerrainLoc(transform.position);
		Vector3Int goToSpot = prevFriendlyTile;

		if (world.IsPlayerLocationTaken(goToSpot) && world.GetPlayer(goToSpot).transport)
		{
			Transport transport = world.GetPlayer(goToSpot).transport;
			transportTarget = transport;
			toTransport = true;
			world.scott.transportTarget = transport;
			world.scott.toTransport = true;
			world.azai.transportTarget = transport;
			world.azai.toTransport = true;

			if (transportTarget.bySea && !world.GetTerrainDataAt(goToSpot).canWalk)
				goToSpot = world.GetAdjacentMoveToTile(world.RoundToInt(transform.position), goToSpot, true);

			HashSet<Vector3Int> exemptList = world.GetExemptList(currentTerrain);
			path = GridSearch.PlayerMoveExempt(world, transform.position, goToSpot, exemptList);

			if (path.Count > 0)
			{
				finalDestinationLoc = goToSpot;
				MoveThroughPath(path);
			}
			else
			{
				inEnemyLines = false;
				LoadWorkerInTransport(transportTarget);
			}

			world.scott.RepositionWorker(goToSpot, true, true, exemptList);
			world.azai.RepositionBodyGuard(goToSpot, true, true, exemptList);
		}
		else
		{
			path = GridSearch.PlayerMoveExempt(world, transform.position, goToSpot, world.GetExemptList(currentTerrain));

			if (path.Count > 0)
			{
				finalDestinationLoc = goToSpot;
				MoveThroughPath(path);
				RealignFollowers(goToSpot, currentTerrain, true);
			}
		}
	}

	public bool NPCCheck(Vector3Int endPositionInt)
	{
		if (pathPositions.Count == 0)
		{
			if (world.IsNPCThere(endPositionInt))
			{
				Unit unitInTheWay = world.GetNPC(endPositionInt);

				if (unitInTheWay.somethingToSay)
				{
					if (isSelected)
					{
						world.unitMovement.QuickSelect(this);
						unitInTheWay.SpeakingCheck();
						if (unitInTheWay.buildDataSO.npc)
							world.uiSpeechWindow.SetSpeakingNPC(unitInTheWay);
					}

					bool leader = unitInTheWay.military && unitInTheWay.military.leader;
					worker.SetUpSpeakingPositions(unitInTheWay.transform.position, leader);
					FinishMoving(transform.position);
					return true;
				}
				else
				{
					if (unitInTheWay.tradeRep && unitInTheWay.tradeRep.onQuest)
					{
						if (isSelected)
						{
							world.ToggleGiftGiving(unitInTheWay.tradeRep);
						}
						worker.SetUpSpeakingPositions(unitInTheWay.transform.position, false);
					}

					FinishMoving(transform.position);
					return true;
				}
			}
		}

		return false;
	}

	public bool TransportCheck(Vector3 endPosition, Vector3Int endPositionInt)
	{
		if (pathPositions.Count == 0)
		{
			if (world.IsPlayerLocationTaken(endPositionInt))
			{
				Unit unitInTheWay = world.GetPlayer(endPositionInt);

				if (unitInTheWay.transport)
				{
					if (worker)
					{
						FinishMoving(endPosition);
						return true;
					}
				}
				//else if (unitInTheWay.worker && !unitInTheWay.worker.isBusy && !unitInTheWay.worker.gathering)
				//         {
				//             Vector3Int next;
				//             if (pathPositions.Count > 0)
				//                 next = pathPositions.Peek();
				//             else
				//                 next = new Vector3Int(0, -10, 0);
				//             unitInTheWay.FindNewSpot(endPositionInt, next);
				//         }
			}
			else if (world.IsNPCThere(endPositionInt) && worker)
			{
				FinishMoving(endPosition);
				return true;
			}
		}

		return false;
	}

	public void PlayerNextStepCheck(Vector3 endPosition, Vector3Int endPositionInt)
	{
		Vector3Int pos = world.GetClosestTerrainLoc(transform.position);
		if (pos != prevTerrainTile)
			RevealCheck(pos, false);

		if (!worker.inEnemyLines && world.GetTerrainDataAt(pos).enemyZone)
		{
			worker.inEnemyLines = true;
			worker.prevFriendlyTile = world.GetClosestTerrainLoc(prevTile);

			if (isSelected)
				world.unitMovement.uiMoveUnit.ToggleVisibility(false);
		}

		if (worker.firstStep && (Mathf.Abs(transform.position.x - world.scott.transform.position.x) > 1.2f || Mathf.Abs(transform.position.z - world.scott.transform.position.z) > 1.2f))
		{
			worker.firstStep = false;
			worker.CheckToFollow(endPositionInt);
		}

		if (pathPositions.Count > 0)
		{
			if (world.IsInNoWalkZone(pathPositions.Peek()))
			{
				worker.RealignFollowers(endPositionInt, prevTile, false);
				StopMovementCheck(true);
				return;
			}

			prevTile = endPositionInt;
			GoToNextStepInPath();
		}
		else
		{
			FinishMoving(endPosition);
		}
	}

	public void WorkerNextStepCheck(Vector3 endPosition, Vector3Int endPositionInt)
	{
		if (pathPositions.Count > 0)
		{
			prevTile = endPositionInt;
			GoToNextStepInPath();
		}
		else
		{
			FinishMoving(endPosition);
		}
	}

	public void FinishMovementPlayer(Vector3 endPosition)
	{
		if (world.tutorialGoing)
			world.TutorialCheck("Finished Movement");

		Vector3Int endPositionInt = world.RoundToInt(endPosition);

		world.IsTreasureHere(endPositionInt, true);

		if (world.tempBattleZone.Contains(endPositionInt) || world.IsEnemyAmbushHere(endPositionInt))
		{
			if (isBusy)
				world.unitMovement.workerTaskManager.ForceCancelWorkerTask();
			
			StepAside(currentLocation, null);
			return;
		}

		if (toTransport && !transportTarget.isUpgrading)
		{
			Transport tempTransport = transportTarget;
			LoadWorkerInTransport(tempTransport);
			
			if (inEnemyLines)
			{
				inEnemyLines = false;
			}
			else
			{
				world.scott.transportTarget = tempTransport;
				world.scott.toTransport = true;
				world.scott.RepositionWorker(endPositionInt, true, false);

				world.azai.transportTarget = tempTransport;
				world.azai.toTransport = true;
				world.azai.RepositionBodyGuard(endPositionInt, true, false);
			}
			
			return;
		}

		if (inEnemyLines)
		{
			if (endPositionInt == prevFriendlyTile)
				inEnemyLines = false;
		}

		if (isSelected)
		{
			world.unitMovement.ShowIndividualCityButtonsUI();
			Vector3Int terrainLoc = world.GetClosestTerrainLoc(currentLocation);

			if (world.IsCityOnTile(terrainLoc) || world.IsTradeCenterOnTile(terrainLoc))
				world.unitMovement.uiWorkerTask.uiLoadUnload.ToggleInteractable(true);
		}

		if (world.IsPlayerLocationTaken(currentLocation))
		{
			UnitInWayCheck();
		}
		else
		{
			world.AddPlayerPosition(currentLocation, this);
			world.tempNoWalkList.Clear();
		}
	}

	public void FinishMovementWorker(Vector3 endPosition)
	{
		if (gathering || clearingForest)
			world.AddPlayerPosition(currentLocation, this);
		else if (toTransport)
			LoadWorkerInTransport(worker.transportTarget);
		else if (world.IsPlayerLocationTaken(currentLocation))
			UnitInWayCheck();
		else
			world.AddPlayerPosition(currentLocation, this);
	}

	public WorkerData SaveWorkerData()
    {
        WorkerData data = new();

		data.unitNameAndLevel = buildDataSO.unitNameAndLevel;
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
        //data.runningAway = runningAway; //not saving running away just in case
        //data.stepAside = stepAside;
        data.orderList = orderList;
        data.timePassed = timePassed;
		data.toTransport = toTransport;
		data.inTransport = inTransport;
		data.inEnemyLines = inEnemyLines;
		data.prevFriendlyTile = prevFriendlyTile;
		if (transportTarget != null)
			data.transportTarget = transportTarget.name;

		//personal resource info
		if (isPlayer)
        {
			data.resourceDict = personalResourceManager.resourceDict;
			data.resourceStorageLevel = personalResourceManager.resourceStorageLevel;
			data.resourceGridDict = resourceGridDict;
		}

		return data;
    }

    public void LoadWorkerData(WorkerData data)
    {
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
        //runningAway = data.runningAway;
        //stepAside = data.stepAside;
		//somethingToSay = data.somethingToSay;
		toTransport = data.toTransport;
		inTransport = data.inTransport;
		inEnemyLines = data.inEnemyLines;
		prevFriendlyTile = data.prevFriendlyTile;
		transportTarget = world.LoadTransport(data.transportTarget);

        //if (runningAway)
        //    exclamationPoint.SetActive(true);

		orderQueue = new Queue<Vector3Int>(orderList);

		//personal resource info
		if (isPlayer)
		{
			personalResourceManager.resourceDict = data.resourceDict;
			personalResourceManager.resourceStorageLevel = data.resourceStorageLevel;
			resourceGridDict = data.resourceGridDict;
		}

		if (!isMoving)
            world.AddPlayerPosition(currentLocation, this);

        if (data.somethingToSay)
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

			GameLoader.Instance.unitMoveOrders[this] = data.moveOrders;
			//MoveThroughPath(data.moveOrders);

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
  //      else if (runningAway)
  //      {
		//	if (!stepAside)
  //          {
  //  			runningAway = false; //gets reset in next method
  //              StartRunningAway();
  //          }
		//}

		if (inTransport)
			gameObject.SetActive(false);
	}
}
