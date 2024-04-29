using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static UnityEngine.Rendering.DebugUI;

public class Wonder : MonoBehaviour, ITradeStop, IGoldWaiter
{
    private MapWorld world;
    //private UIWonderSelection uiWonderSelection;
    [HideInInspector]
    public UIPersonalResourceInfoPanel uiCityResourceInfoPanel;
    public GameObject mesh0Percent, mesh33Percent, mesh67Percent, meshComplete, mapIcon, exclamationPoint;
    public List<Light> wonderLights = new();

    private SelectionHighlight highlight;

    private int percentDone;
    public int PercentDone { get { return percentDone; } set { percentDone = value; } }

    //private Quaternion rotation;
    //public Quaternion Rotation { set { rotation = value; } }

    [HideInInspector]
    public bool isConstructing, canBuildHarbor, hasHarbor, isActive, roadPreExisted, goldWait;
    [HideInInspector]
    public Vector3Int unloadLoc, harborLoc;
    [HideInInspector]
    public Vector3 centerPos;
    public string wonderName;
    [HideInInspector]
    public CityImprovement harborImprovement;

    private List<Vector3Int> wonderLocs = new();
    public List<Vector3Int> WonderLocs { get { return wonderLocs; } set { wonderLocs = value; } }

    private List<Vector3Int> possibleHarborLocs = new();
    public List<Vector3Int> PossibleHarborLocs { get { return possibleHarborLocs; } set { possibleHarborLocs = value; } }

    private List<Vector3Int> coastTiles = new();
    public List<Vector3Int> CoastTiles { get { return coastTiles; } set { coastTiles = value; } }

    private Dictionary<ResourceType, int> resourceDict = new(); //how much that has been added
    public Dictionary<ResourceType, int> ResourceDict { get { return resourceDict; } set { resourceDict = value; } }

    private Dictionary<ResourceType, int> resourceCostDict = new(); //total cost to build wonder
    public Dictionary<ResourceType, int> ResourceCostDict { get { return resourceCostDict; } }

    private Dictionary<ResourceType, int> resourceGridDict = new(); //order of resources shown for when trader manually unldoads
    public Dictionary<ResourceType, int> ResourceGridDict { get { return resourceGridDict; } set { resourceGridDict = value; } }

    private WonderDataSO wonderData;
    public WonderDataSO WonderData { get { return wonderData; } set { wonderData = value; } }

    private int workersReceived;
    public int WorkersReceived { get { return workersReceived; } set { workersReceived = value; } }

    [HideInInspector]
    public List<(bool, Vector3Int)> workerSexAndHome = new();

    //for building the wonder
    private Dictionary<ResourceType, int> resourceThreshold = new();
    //private TimeProgressBar timeProgressBar;
    private UITimeProgressBar uiTimeProgressBar;
    private Coroutine buildingCo;
    private int totalTime;
    [HideInInspector]
    public int totalGoldCost;
    private int timePassed;
    private bool isBuilding;
    private WaitForSeconds oneSecondWait = new WaitForSeconds(1);

	//stop info
	ITradeStop stop;
	string ITradeStop.stopName => wonderName;
    [HideInInspector]
    public List<Trader> waitList = new(), seaWaitList = new();
	List<Trader> ITradeStop.waitList => waitList;
	List<Trader> ITradeStop.seaWaitList => seaWaitList;
	List<Trader> ITradeStop.airWaitList => new();
	Vector3Int ITradeStop.mainLoc => unloadLoc;
	City ITradeStop.city => null;
	Wonder ITradeStop.wonder => this;
	TradeCenter ITradeStop.center => null;

	//private Dictionary<Vector3Int, Trader> traderPosDict = new();
	//private Queue<Unit> waitList = new(), seaWaitList = new(), airWaitList = new();
	//private TradeRouteManager tradeRouteWaiter;
	//private ResourceType resourceWaiter = ResourceType.None;

	//particle systems
	[SerializeField]
    private ParticleSystem heavenHighlight, smokeEmitter, smokeSplash, removeSplash, fireworks1, fireworks2;

    int IGoldWaiter.goldNeeded => totalGoldCost;
	Vector3Int IGoldWaiter.waiterLoc => unloadLoc;
	//audio
	private AudioSource audioSource;

    private CameraController focusCam;

    private void Awake()
    {
        stop = this;
        uiTimeProgressBar = Instantiate(GameAssets.Instance.uiTimeProgressPrefab, transform.position, Quaternion.Euler(90, 0, 0)).GetComponent<UITimeProgressBar>();
        uiTimeProgressBar.transform.SetParent(transform, false);
        isConstructing = true;
        highlight = GetComponent<SelectionHighlight>();
        audioSource = GetComponent<AudioSource>();
        fireworks1.gameObject.SetActive(false);
        fireworks2.gameObject.SetActive(false);

        removeSplash = Instantiate(removeSplash, transform.position, Quaternion.Euler(-90, 0, 0));
        removeSplash.transform.localScale = new Vector3(2, 2, 2);
        removeSplash.Stop();

        heavenHighlight = Instantiate(heavenHighlight, transform.position, Quaternion.identity);
        heavenHighlight.transform.SetParent(transform, false);
        heavenHighlight.Pause();
    }

    public void SetReferences(MapWorld world, CameraController focusCam)
    {
        this.world = world;
        removeSplash.transform.SetParent(world.psHolder, false);
        this.focusCam = focusCam;
    }

    public void SetPrefabs(bool load)
    {
        //mesh0Percent.SetActive(false);
        if (isConstructing)
        {
            meshComplete.SetActive(false);
            
            if (percentDone < 33)
            {
                mesh33Percent.SetActive(false);
                mesh67Percent.SetActive(false);
            }
            else if (percentDone < 67)
            {
				mesh0Percent.SetActive(false);
				mesh67Percent.SetActive(false);
			}
            else
            {
				mesh0Percent.SetActive(false);
				mesh33Percent.SetActive(false);
			}
		}
        else
        {
            mesh0Percent.SetActive(false);
			mesh33Percent.SetActive(false);
			mesh67Percent.SetActive(false);
		}

        if (!load)
            PlaySmokeSplash();
        mapIcon.SetActive(true);
		//PlayFireworks();

		//mesh25Percent.SetActive(false);
	}

    public void SetLastPrefab()
    {
        mesh0Percent.SetActive(false);
        //mesh25Percent.SetActive(false);
        mesh33Percent.SetActive(false);
        mesh67Percent.SetActive(false);
    }

    public void SetCenterPos(Vector3 centerPos)
    {
        this.centerPos = centerPos;
        uiTimeProgressBar.gameObject.transform.position = centerPos;
        totalTime = wonderData.buildTimePerPercent;
        uiTimeProgressBar.SetTimeProgressBarValue(totalTime);
    }

    public void SetExclamationPoint()
    {
        Vector3 pos = unloadLoc;
        pos.y += 1;
        exclamationPoint.transform.position = pos;
    }

    public void SetResourceDict(List<ResourceValue> resources, bool load)
    {
        foreach (ResourceValue resource in resources)
        {
            if (!load)
            {
                resourceDict[resource.resourceType] = 0;
                resourceGridDict[resource.resourceType] = resourceDict.Count;
            }

            resourceCostDict[resource.resourceType] = resource.resourceAmount;
            SetNextResourceThreshold(resource.resourceType);
        }

        totalGoldCost = wonderData.workerCost * wonderData.workersNeeded;
    }

    private void SetNextResourceThreshold(ResourceType resourceType)
    {
        resourceThreshold[resourceType] = Mathf.RoundToInt((percentDone + 1) * resourceCostDict[resourceType] * 0.01f);
    }

    private void LightCheck()
    {
        if (world.dayNightCycle.timeODay > 18 || world.dayNightCycle.timeODay < 6)
            foreach (Light light in wonderLights)
                light.gameObject.SetActive(true);
    }

	public void PlayPopGainAudio()
	{
		audioSource.clip = world.cityBuilderManager.popGainClip;
		audioSource.Play();
	}

	private void IncreasePercentDone()
    {
        percentDone++;
        if (percentDone == 100)
            return;

        foreach (ResourceType resourceType in resourceDict.Keys)
            SetNextResourceThreshold(resourceType);
    }

    //public void SetUI(UIWonderSelection uiWonderSelection)
    //{
    //    this.uiWonderSelection = uiWonderSelection;
    //}

    public bool CheckResourceType(ResourceType resourceType)
    {
        return resourceDict.ContainsKey(resourceType);
    }

	//internal void SetWaiter(TradeRouteManager tradeRouteManager, ResourceType resourceType = ResourceType.None)
	//{
	//    tradeRouteWaiter = tradeRouteManager;
	//    resourceWaiter = resourceType;
	//}

	//public void CheckResourceWaiter(ResourceType resourceType)
	//{
	//    if (tradeRouteWaiter != null && resourceWaiter == resourceType)
	//    {
	//        tradeRouteWaiter.resourceCheck = false;
	//        tradeRouteWaiter = null;
	//        resourceWaiter = ResourceType.None;
	//    }
	//}

	//public void AddToWaitList(Unit unit)
 //   {
 //       if (unit.bySea)
 //       {
	//		if (!seaWaitList.Contains(unit))
	//			seaWaitList.Enqueue(unit);
	//	}
	//	else
 //       {
 //           if (!waitList.Contains(unit))
 //               waitList.Enqueue(unit);
 //       }
 //   }

 //   public void RemoveFromWaitList(Unit unit)
 //   {
 //       List<Unit> waitListList = unit.bySea ? seaWaitList.ToList() : waitList.ToList();
        
 //       if (!waitListList.Contains(unit))
 //       {
 //           if (unit.bySea)
 //               CheckSeaQueue();
 //           else
 //               CheckQueue();
            
 //           return;
 //       }

 //       int index = waitListList.IndexOf(unit);
 //       waitListList.Remove(unit);

 //       int j = 0;
 //       for (int i = index; i < waitListList.Count; i++)
 //       {
 //           j++;
 //           waitListList[i].trader.StartMoveUpInLine(j);
 //       }

 //       if (unit.bySea)
 //           seaWaitList = new Queue<Unit>(waitListList);
 //       else
	//		waitList = new Queue<Unit>(waitListList);
	//	//waitList = new Queue<Unit>(waitList.Where(x => x != unit));
	//}

 //   internal void CheckQueue()
 //   {
 //       if (waitList.Count > 0)
 //           waitList.Dequeue().trader.ExitLine();

 //       if (waitList.Count > 0)
 //       {
 //           int i = 0;
 //           foreach (Unit unit in waitList)
 //           {
 //               i++;
 //               unit.trader.StartMoveUpInLine(i);
 //           }
 //       }
 //   }

 //   internal void CheckSeaQueue()
 //   {
	//	if (seaWaitList.Count > 0)
	//		seaWaitList.Dequeue().trader.ExitLine();

	//	if (seaWaitList.Count > 0)
	//	{
	//		int i = 0;
	//		foreach (Unit unit in seaWaitList)
	//		{
	//			i++;
 //               unit.trader.StartMoveUpInLine(i);
	//		}
	//	}
	//}

    //internal int CheckResource(ResourceType type, int newResourceAmount)
    //{
    //    if (resourceDict.ContainsKey(type) && CheckStorageSpaceForResource(type))
    //    {
    //        if (!resourceGridDict.ContainsKey(type))
    //            AddToGrid(type);

    //        return AddResourceToStorage(type, newResourceAmount);
    //    }
    //    else
    //    {
    //        InfoPopUpHandler.WarningMessage().Create(centerPos, "No storage space for " + type);
    //        return 0;
    //    }
    //}

    public int AddResource(ResourceType type, int amount)
    {
		amount = AddResourceCheck(type, amount);
		if (amount > 0)
			AddResourceToStorage(type, amount);

		if (!resourceGridDict.ContainsKey(type))
			AddToGrid(type);

        return amount;
	}

    private int AddResourceCheck(ResourceType type, int amount)
	{
        int diff = resourceCostDict[type] - resourceDict[type];

        if (diff < amount)
            amount = diff;

        return amount;
	}

    private void UICheck(ResourceType type)
    {
		if (isActive)
			world.cityBuilderManager.uiWonderSelection.UpdateUI(type, resourceDict[type], resourceCostDict[type]);

		if (uiCityResourceInfoPanel)
			uiCityResourceInfoPanel.UpdateResourceInteractable(type, resourceDict[type], false);

		if (!isBuilding)
			ThresholdCheck();
	}

    private void AddToGrid(ResourceType type)
    {
        resourceGridDict[type] = resourceGridDict.Count;
    }


    private void AddResourceToStorage(ResourceType resourceType, int resourceAmount)
    {
        resourceDict[resourceType] += resourceAmount; //updating the dictionary
        UICheck(resourceType);
    }

    public void AddWorker(Unit unit)
    {
        Vector3 pos = unit.transform.position;
		pos.y = 3f;
        Laborer laborer = unit.GetComponent<Laborer>();
		world.laborerList.Remove(laborer);

		workersReceived++;
        workerSexAndHome.Add((unit.secondaryPrefab, laborer.homeCityLoc));
        heavenHighlight.transform.position = pos;
        PlayPopGainAudio();
        heavenHighlight.Play();
        if (world.cityBuilderManager.uiWonderSelection.activeStatus && world.cityBuilderManager.uiWonderSelection.wonder == this)
			world.cityBuilderManager.uiWonderSelection.UpdateUIWorkers(workersReceived, this);

		if (!StillNeedsWorkers())
            ThresholdCheck();
    }

    public bool StillNeedsWorkers()
    {
        return workersReceived < wonderData.workersNeeded;
    }

    public bool RestartGold(int gold)
    {
        if (totalGoldCost <= gold)
        {
            goldWait = false;
            if (world.cityBuilderManager.uiWonderSelection.activeStatus)
                world.cityBuilderManager.uiWonderSelection.UpdateWaitingForMessage(false);
			exclamationPoint.SetActive(false);
            ThresholdCheck();
            return true;
        }
        else
        {
            return false;
        }
    }

    public void ThresholdCheck()
    {
        if (goldWait)
            return;
        
        if (StillNeedsWorkers())
        {
            if (smokeEmitter.isPlaying)
                StopSmokeEmitter();
            return;
        }

        if (!world.CheckWorldGold(totalGoldCost))
        {
            goldWait = true;
            if (world.cityBuilderManager.uiWonderSelection.activeStatus)
                world.cityBuilderManager.uiWonderSelection.UpdateWaitingForMessage(true);
            exclamationPoint.SetActive(true);
            world.AddToGoldWaitList(this);
            if (smokeEmitter.isPlaying)
                StopSmokeEmitter();
            return;
        }

		foreach (ResourceType type in resourceThreshold.Keys)
		{
			if (resourceDict[type] < resourceThreshold[type])
			{
				if (smokeEmitter.isPlaying)
					StopSmokeEmitter();
				return;
			}
		}

		timePassed = totalTime;
		ConsumeWorkerCost();
		buildingCo = StartCoroutine(BuildNextPortionOfWonder(false));
    }

	public void LoadWonderBuild()
	{
		buildingCo = StartCoroutine(BuildNextPortionOfWonder(false));
	}

	public IEnumerator BuildNextPortionOfWonder(bool load)
    {
        if (!smokeEmitter.isPlaying)
            PlaySmokeEmitter(load);
        
        if (isActive)
        {
            uiTimeProgressBar.gameObject.SetActive(true);
            uiTimeProgressBar.SetToZero();
            uiTimeProgressBar.SetTime(timePassed);
        }

        isBuilding = true;

        while (timePassed > 0)
        {
            yield return oneSecondWait;
            timePassed--;
            if (isActive)
                uiTimeProgressBar.SetTime(timePassed);
        }

        isBuilding = false;
        if (isActive)
            uiTimeProgressBar.gameObject.SetActive(false);
        IncreasePercentDone();
        if (isActive)
			world.cityBuilderManager.uiWonderSelection.UpdateUIPercent(percentDone);

        NextPhaseCheck();
    }

    private void NextPhaseCheck()
    {   
        if (percentDone == 33)
        {
            SetNewGO(mesh0Percent, mesh33Percent);
            PlaySmokeSplash();
        }
        else if (percentDone == 67)
        {
            SetNewGO(mesh33Percent, mesh67Percent);
            PlaySmokeSplash();
        }
        //else if (percentDone == 75)
        //{
        //    SetNewGO(mesh50Percent, mesh75Percent);
        //    PlaySmokeSplash();
        //}
        else if (percentDone == 100)
        {
            world.UnselectAll();
            PlayFireworks();
            focusCam.CenterCameraNoFollow(centerPos);
            SetNewGO(mesh67Percent,meshComplete);
            LightCheck();
            PlaySmokeSplash();
            isConstructing = false;
            world.RemoveStop(unloadLoc);
            world.RemoveWonderName(wonderName);
            if (isActive)
            {
				world.cityBuilderManager.uiWonderSelection.HideCancelConstructionButton();
				world.cityBuilderManager.uiWonderSelection.HideHarborButton();
				world.cityBuilderManager.uiWonderSelection.HideWorkerCounts();
            }
            world.RemoveTradeLoc(unloadLoc);

            MeshCheck();

			if (hasHarbor)
                DestroyHarbor();

            world.roadManager.RemoveRoadAtPosition(unloadLoc);
			world.AddToNoWalkList(unloadLoc);
			if (world.mainPlayer.isMoving)
				world.tempNoWalkList.Add(unloadLoc);
            RemoveUnits();
			if (smokeEmitter.isPlaying)
				StopSmokeEmitter();

			world.cityBuilderManager.CreateAllWorkers(this);
            ApplyWonderCompletionReward();
            Destroy(uiTimeProgressBar.gameObject);
            return;
        }

        ThresholdCheck();
    }

    public void MeshCheck()
    {
		int grasslandCount = 0;
		foreach (Vector3Int tile in wonderLocs)
		{
			if (world.GetTerrainDataAt(tile).terrainData.grassland)
				grasslandCount++;
		}

		if (grasslandCount >= Mathf.Ceil(wonderLocs.Count * 0.5f)) //tie breaker goes to grassland
		{
			foreach (MeshFilter mesh in meshComplete.GetComponentsInChildren<MeshFilter>())
			{
				if (mesh.name == "Ground")
				{
					Vector2[] newUVs = mesh.mesh.uv;
					for (int i = 0; i < newUVs.Length; i++)
						newUVs[i].x -= 0.625f; //shift over one tile in atlas

					mesh.mesh.uv = newUVs;
					break;
				}
			}
		}
	}

    public void DestroyHarbor()
    {
        stop.ClearStopCheck(stop.seaWaitList, harborLoc, world);
        //ClearHarborCheck();
        
        hasHarbor = false;
        GameObject harbor = world.GetStructure(harborLoc);
        harbor.GetComponent<CityImprovement>().PlayRemoveEffect(false);
        harborImprovement = null;
        Destroy(harbor);
        world.RemoveSingleBuildFromCityLabor(harborLoc);
        world.RemoveStructure(harborLoc);
		world.RemoveStop(harborLoc);
		world.RemoveTradeLoc(harborLoc);
    }

    public List<Vector3Int> OuterRim()
    {
        List<Vector3Int> locs = new();

        int yAngle = Mathf.RoundToInt(transform.localEulerAngles.y);

        int k = 0;
        int[] xArray = new int[wonderLocs.Count];
        int[] zArray = new int[wonderLocs.Count];

        for (int i = 0; i < wonderLocs.Count; i++)
        {
			xArray[k] = wonderLocs[i].x;
			zArray[k] = wonderLocs[i].z;
			k++;
		}

        int xMin = Mathf.Min(xArray) - 1;
        int xMax = Mathf.Max(xArray) + 1;
        int zMin = Mathf.Min(zArray) - 1;
        int zMax = Mathf.Max(zArray) + 1;

		//looping around the wonder counter clockwise, starting at front left corner
        if (yAngle == 270)
        {
			for (int i = 0; i < zMax - zMin; i++)
				locs.Add(new Vector3Int(xMax, 0, zMin + i));

			for (int i = 0; i < xMax - xMin; i++)
				locs.Add(new Vector3Int(xMax - i, 0, zMax));

			for (int i = 0; i < zMax - zMin; i++)
				locs.Add(new Vector3Int(xMin, 0, zMax - i));

			for (int i = 0; i < xMax - xMin; i++)
				locs.Add(new Vector3Int(xMin + i, 0, zMin));
		}
		else if (yAngle == 180)
        {
			for (int i = 0; i < xMax - xMin; i++)
				locs.Add(new Vector3Int(xMax - i, 0, zMax));

			for (int i = 0; i < zMax - zMin; i++)
				locs.Add(new Vector3Int(xMin, 0, zMax - i));
	
            for (int i = 0; i < xMax - xMin; i++)
				locs.Add(new Vector3Int(xMin + i, 0, zMin));

			for (int i = 0; i < zMax - zMin; i++)
				locs.Add(new Vector3Int(xMax, 0, zMin + i));
		}
		else if (yAngle == 90)
        {
			for (int i = 0; i < zMax - zMin; i++)
				locs.Add(new Vector3Int(xMin, 0, zMax - i));

			for (int i = 0; i < xMax - xMin; i++)
				locs.Add(new Vector3Int(xMin + i, 0, zMin));

			for (int i = 0; i < zMax - zMin; i++)
				locs.Add(new Vector3Int(xMax, 0, zMin + i));

			for (int i = 0; i < xMax - xMin; i++)
				locs.Add(new Vector3Int(xMax - i, 0, zMax));
		}
		else
        {
			for (int i = 0; i < xMax - xMin; i++)
				locs.Add(new Vector3Int(xMin + i, 0, zMin));

			for (int i = 0; i < zMax - zMin; i++)
				locs.Add(new Vector3Int(xMax, 0, zMin + i));

			for (int i = 0; i < xMax - xMin; i++)
				locs.Add(new Vector3Int(xMax - i, 0, zMax));

			for (int i = 0; i < zMax - zMin; i++)
				locs.Add(new Vector3Int(xMin, 0, zMax - i));
		}

        return locs;
    }

    private void PlayFireworks()
    {
        fireworks1.gameObject.SetActive(true);
        fireworks1.Play();
        fireworks2.gameObject.SetActive(true);
        fireworks2.Play();
    }

    private void RemoveUnits()
    {
        List<Vector3Int> locs = world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAY);
        locs.Insert(0, unloadLoc);

        for (int i = 0; i < locs.Count; i++)
        {
            if (world.IsTraderLocationTaken(locs[i]))
            {
                List<Trader> traders = world.GetTrader(locs[i]);
                for (int j = 0; j < traders.Count; j++)
                {
                    world.RemoveTraderPosition(unloadLoc, traders[j]);
                    if (traders[j].followingRoute)
                        traders[j].InterruptRoute(false);
                }
            }
        }
    }

    private void SetNewGO(GameObject prevMesh, GameObject newMesh)
    {
        prevMesh.SetActive(false);
        newMesh.SetActive(true);
    }

    public void TimeProgressBarSetActive(bool v)
    {
        if (isBuilding)
        {
            uiTimeProgressBar.gameObject.SetActive(v);
            if (v)
            {
                uiTimeProgressBar.SetProgressBarMask(timePassed);
                uiTimeProgressBar.SetTime(timePassed);
            }
        }
    }

    private void ConsumeWorkerCost()
    {
        int amount = -totalGoldCost;
        world.UpdateWorldGold(amount);
        Vector3 loc = centerPos;
        loc.y += 0.4f;
        if (isActive)
            InfoResourcePopUpHandler.CreateResourceStat(loc, amount, ResourceHolder.Instance.GetIcon(ResourceType.Gold), world);
    }

    public void StopConstructing()
    {
        if (isBuilding)
        {
            StopSmokeEmitter();
            uiTimeProgressBar.gameObject.SetActive(false);
            StopCoroutine(buildingCo);
            int amount = totalGoldCost;
            world.UpdateWorldGold(amount);
            InfoResourcePopUpHandler.CreateResourceStat(centerPos, amount, ResourceHolder.Instance.GetIcon(ResourceType.Gold), world);
            isBuilding = false;
        }
    }

    public void PlayRemoveEffect()
    {
        removeSplash.Play();
    }

    private void PlaySmokeEmitter(bool load)
    {
        int time = wonderData.buildTimePerPercent;
        var emission = smokeEmitter.emission;
        emission.rateOverTime = 10f / (time * 6); //a bit of a delay so the smoke isn't too overwhelming so fast

        if (load)
            smokeEmitter.time = totalTime - timePassed;

        smokeEmitter.gameObject.SetActive(true);
        smokeEmitter.Play();
    }

    public void PlaySmokeSplash()
    {
        smokeSplash.Play();
    }

    private void StopSmokeEmitter()
    {
        smokeEmitter.Stop();
        smokeEmitter.gameObject.SetActive(false);
    }

    public void EnableHighlight(Color highlightColor, bool newGlow)
    {
        if (highlight.isGlowing)
            return;

        highlight.EnableHighlight(highlightColor, newGlow);
    }

    public void DisableHighlight()
    {
        if (!highlight.isGlowing)
            return;

        highlight.DisableHighlight();
    }

    //manually applying benefits for now
    public void ApplyWonderCompletionReward()
    {
        string wonderName = wonderData.wonderDisplayName;

        switch (wonderName)
        {
            case "Pyramids":
                world.AddToCityPermanentChanges("Warehouse Storage", wonderData.wonderBenefitChange);

                foreach (City city in world.cityDict.Values)
                {
                    city.warehouseStorageLimit += (int)wonderData.wonderBenefitChange;
                    city.ResourceManager.StorageSpaceCheck();
                    //city.ResourceManager.RestartStorageRoomWaitProduction();
                }
                break;
            case "Great Lighthouse":
				world.AddToUnitPermanentChanges(UnitType.BoatTrader.ToString(), "Movement Speed", wonderData.wonderBenefitChange);

				foreach (Trader trader in world.traderList)
                {
                    if (trader.buildDataSO.unitType == UnitType.BoatTrader)
                        trader.originalMoveSpeed *= 1f + wonderData.wonderBenefitChange;
                }
                break;
            case "Great Ziggurat":
                world.AddToCityImprovementChanges("Production Yield", ResourceType.Research, wonderData.wonderBenefitChange);
                break;
            case "Hanging Gardens":
                world.AddToCityPermanentChanges("Work Ethic", wonderData.wonderBenefitChange);

                foreach (City city in world.cityDict.Values)
                {
                    city.workEthic += wonderData.wonderBenefitChange;
                    city.wonderWorkEthic += wonderData.wonderBenefitChange;
                }
                break;
        }
    }

    public void GoldWaitCheck()
    {
        if (goldWait)
        {
			goldWait = false;
			world.cityBuilderManager.uiWonderSelection.UpdateWaitingForMessage(false);
			exclamationPoint.SetActive(false);
			world.RemoveFromGoldWaitList(this);
		}
    }

   // public void ClearWonderCheck()
   // {
   //     for (int i = 0; i < waitList.Count; i++)
			//waitList.Dequeue().trader.CancelRoute();

   //     if (world.IsUnitLocationTaken(unloadLoc))
   //         world.CancelTraderRoute(unloadLoc);
   // }

	//public void ClearHarborCheck()
 //   {
	//	for (int i = 0; i < seaWaitList.Count; i++)
	//		seaWaitList.Dequeue().trader.CancelRoute();

	//	if (world.IsUnitLocationTaken(harborLoc))
	//		world.CancelTraderRoute(harborLoc);
	//}

    public void RemoveQueuedTraders()
    {
        stop.ClearStopCheck(stop.waitList, unloadLoc, world);
        if (hasHarbor)
            stop.ClearStopCheck(stop.seaWaitList, harborLoc, world);
                
        //ClearWonderCheck();
        //ClearHarborCheck();
	}

	public WonderData SaveData()
    {
        WonderData data = new();

        data.name = wonderName;
        data.centerPos = centerPos;
        data.rotation = transform.rotation;
        data.unloadLoc = unloadLoc;
        data.harborLoc = harborLoc;
        data.percentDone = percentDone;
        data.workersReceived = workersReceived;
        data.timePassed = timePassed;
        data.isConstructing = isConstructing;
        data.canBuildHarbor = canBuildHarbor;
        data.hasHarbor = hasHarbor;
        data.roadPreExisted = roadPreExisted;
        data.isBuilding = isBuilding;
        data.wonderLocs = wonderLocs;
        data.possibleHarborLocs = possibleHarborLocs;
        data.coastTiles = coastTiles;
        data.resourceDict = resourceDict;
        data.resourceGridDict = resourceGridDict;
        data.workerSexAndHome = workerSexAndHome;

		//List<Unit> tempWaitList = waitList.ToList();

		for (int i = 0; i < stop.waitList.Count; i++)
			data.waitList.Add(stop.waitList[i].id);

		//List<Unit> tempSeaWaitList = seaWaitList.ToList();

		for (int i = 0; i < stop.seaWaitList.Count; i++)
			data.seaWaitList.Add(stop.seaWaitList[i].id);

		//public Dictionary<ResourceType, int> resourceDict, resourceCostDict, resourceGridDict;

		return data;
    }

    public void LoadData(WonderData data)
    {
        //centerPos = data.centerPos; //done elsewhere
        wonderName = data.name;
		unloadLoc = data.unloadLoc;
        SetExclamationPoint();
		harborLoc = data.harborLoc;
		percentDone = data.percentDone;
		workersReceived = data.workersReceived;
        timePassed = data.timePassed;
		isConstructing = data.isConstructing;
		canBuildHarbor = data.canBuildHarbor;
		hasHarbor = data.hasHarbor;
		roadPreExisted = data.roadPreExisted;
		wonderLocs = data.wonderLocs;
		possibleHarborLocs = data.possibleHarborLocs;
		coastTiles = data.coastTiles;
        resourceDict = data.resourceDict;
        resourceGridDict = data.resourceGridDict;
        workerSexAndHome = data.workerSexAndHome;
	}

    public void DestroyParticleSystems()
    {
        Destroy(heavenHighlight.gameObject);
        Destroy(smokeEmitter.gameObject);
        Destroy(smokeSplash.gameObject);
        Destroy(removeSplash.gameObject);
        Destroy(fireworks1.gameObject);
        Destroy(fireworks2.gameObject);
	}

	public void SetWaitList(List<int> waitList)
	{
		for (int i = 0; i < waitList.Count; i++)
		{
			for (int j = 0; j < world.traderList.Count; j++)
			{
				if (world.traderList[j].id == waitList[i])
				{
					stop.AddToWaitList(world.traderList[j], stop);
					break;
				}
			}
		}
	}

	//public void SetSeaWaitList(List<int> seaWaitList)
	//{
	//	for (int i = 0; i < seaWaitList.Count; i++)
	//	{
	//		for (int j = 0; j < world.traderList.Count; j++)
	//		{
	//			if (world.traderList[j].id == seaWaitList[i])
	//			{
	//				this.seaWaitList.Enqueue(world.traderList[j]);
	//				break;
	//			}
	//		}
	//	}
	//}
}
