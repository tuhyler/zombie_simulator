using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class Wonder : MonoBehaviour
{
    private MapWorld world;
    //private UIWonderSelection uiWonderSelection;
    [HideInInspector]
    public UIPersonalResourceInfoPanel uiCityResourceInfoPanel;
    public GameObject mesh0Percent;
    //public GameObject mesh25Percent;
    public GameObject mesh33Percent;
    public GameObject mesh67Percent;
    public GameObject meshComplete;
    public GameObject mapIcon;
    public List<Light> wonderLights = new();

    private SelectionHighlight highlight;

    private int percentDone;
    public int PercentDone { get { return percentDone; } set { percentDone = value; } }

    //private Quaternion rotation;
    //public Quaternion Rotation { set { rotation = value; } }

    [HideInInspector]
    public bool isConstructing, canBuildHarbor, hasHarbor, isActive, roadPreExisted;
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

    //for building the wonder
    private Dictionary<ResourceType, int> resourceThreshold = new();
    //private TimeProgressBar timeProgressBar;
    private UITimeProgressBar uiTimeProgressBar;
    private Coroutine buildingCo;
    private int totalTime;
    private int totalGoldCost;
    private int timePassed;
    private bool isBuilding;
    private WaitForSeconds oneSecondWait = new WaitForSeconds(1);

    //for queuing unloading
    private Queue<Unit> waitList = new(), seaWaitList = new();
    private TradeRouteManager tradeRouteWaiter;
    private ResourceType resourceWaiter = ResourceType.None;

    //particle systems
    [SerializeField]
    private ParticleSystem heavenHighlight, smokeEmitter, smokeSplash, removeSplash, fireworks1, fireworks2;

    private CameraController focusCam;

    private void Awake()
    {
        uiTimeProgressBar = Instantiate(GameAssets.Instance.uiTimeProgressPrefab, transform.position, Quaternion.Euler(90, 0, 0)).GetComponent<UITimeProgressBar>();
        isConstructing = true;
        highlight = GetComponent<SelectionHighlight>();
        fireworks1.gameObject.SetActive(false);
        fireworks2.gameObject.SetActive(false);

        removeSplash = Instantiate(removeSplash, transform.position, Quaternion.Euler(-90, 0, 0));
        removeSplash.transform.localScale = new Vector3(2, 2, 2);
        removeSplash.Stop();

        heavenHighlight = Instantiate(heavenHighlight, transform.position, Quaternion.identity);
        heavenHighlight.transform.parent = transform;
        heavenHighlight.Pause();
    }

    public void SetReferences(MapWorld world, CameraController focusCam)
    {
        this.world = world;
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
        resourceThreshold[resourceType] = (percentDone + 1) * Mathf.RoundToInt(resourceCostDict[resourceType] * 0.01f);
    }

    private void LightCheck()
    {
        if (world.dayNightCycle.timeODay > 18 || world.dayNightCycle.timeODay < 6)
            foreach (Light light in wonderLights)
                light.gameObject.SetActive(true);
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

    internal void SetWaiter(TradeRouteManager tradeRouteManager, ResourceType resourceType = ResourceType.None)
    {
        tradeRouteWaiter = tradeRouteManager;
        resourceWaiter = resourceType;
    }

    public void CheckResourceWaiter(ResourceType resourceType)
    {
        if (tradeRouteWaiter != null && resourceWaiter == resourceType)
        {
            tradeRouteWaiter.resourceCheck = false;
            tradeRouteWaiter = null;
            resourceWaiter = ResourceType.None;
        }
    }

    public void AddToWaitList(Unit unit)
    {
        if (unit.bySea)
        {
			if (!seaWaitList.Contains(unit))
				seaWaitList.Enqueue(unit);
		}
		else
        {
            if (!waitList.Contains(unit))
                waitList.Enqueue(unit);
        }
    }

    public void RemoveFromWaitList(Unit unit)
    {
        List<Unit> waitListList = unit.bySea ? seaWaitList.ToList() : waitList.ToList();
        
        if (!waitListList.Contains(unit))
        {
            if (unit.bySea)
                CheckSeaQueue();
            else
                CheckQueue();
            
            return;
        }

        int index = waitListList.IndexOf(unit);
        waitListList.Remove(unit);

        int j = 0;
        for (int i = index; i < waitListList.Count; i++)
        {
            j++;
            waitListList[i].waitingCo = StartCoroutine(waitListList[i].MoveUpInLine(j));
        }

        if (unit.bySea)
            seaWaitList = new Queue<Unit>(waitListList);
        else
			waitList = new Queue<Unit>(waitListList);
		//waitList = new Queue<Unit>(waitList.Where(x => x != unit));
	}

    internal void CheckQueue()
    {
        if (waitList.Count > 0)
        {
            waitList.Dequeue().ExitLine();
        }

        if (waitList.Count > 0)
        {
            int i = 0;
            foreach (Unit unit in waitList)
            {
                i++;
                unit.waitingCo = StartCoroutine(unit.MoveUpInLine(i));
            }
        }
    }

    internal void CheckSeaQueue()
    {
		if (seaWaitList.Count > 0)
		{
			seaWaitList.Dequeue().ExitLine();
		}

		if (seaWaitList.Count > 0)
		{
			int i = 0;
			foreach (Unit unit in seaWaitList)
			{
				i++;
				unit.waitingCo = StartCoroutine(unit.MoveUpInLine(i));
			}
		}
	}

    internal int CheckResource(ResourceType type, int newResourceAmount)
    {
        if (resourceDict.ContainsKey(type) && CheckStorageSpaceForResource(type, newResourceAmount))
        {
            if (!resourceGridDict.ContainsKey(type))
                AddToGrid(type);

            return AddResourceToStorage(type, newResourceAmount);
        }
        else
        {
            InfoPopUpHandler.WarningMessage().Create(centerPos, "No storage space for " + type);
            return 0;
        }
    }

    private void AddToGrid(ResourceType type)
    {
        resourceGridDict[type] = resourceGridDict.Count;
    }

    private bool CheckStorageSpaceForResource(ResourceType resourceType, int resourceAmount)
    {
        return resourceDict[resourceType] < resourceCostDict[resourceType];
    }

    private int AddResourceToStorage(ResourceType resourceType, int resourceAmount)
    {
        //check to ensure you don't take out more resources than are available in dictionary
        //if (resourceAmount < 0 && -resourceAmount > resourceDict[resourceType])
        //{
        //    resourceAmount = -resourceDict[resourceType];
        //}

        int resourceLimit = resourceCostDict[resourceType];

        //adjusting resource amount to move based on how much space is available
        int newResourceAmount = resourceAmount;
        int newResourceBalance = (resourceDict[resourceType] + newResourceAmount) - resourceLimit;

        if (newResourceBalance >= 0)
        {
            newResourceAmount -= newResourceBalance;
        }

        resourceDict[resourceType] += newResourceAmount; //updating the dictionary

        if (isActive)
            world.cityBuilderManager.uiWonderSelection.UpdateUI(resourceType, resourceDict[resourceType], resourceLimit);

        if (uiCityResourceInfoPanel)
            uiCityResourceInfoPanel.UpdateResourceInteractable(resourceType, resourceDict[resourceType], false);

        if (newResourceAmount > 0)
            CheckResourceWaiter(resourceType);
        //else if (newResourceAmount < 0)
        //    CheckLimitWaiter();

        if (!isBuilding)
            ThresholdCheck();
        return resourceAmount;
    }

    public void AddWorker(Unit unit)
    {
        Vector3 pos = unit.transform.position;
		pos.y = 3f;
		world.laborerList.Remove(unit.GetComponent<Laborer>());

		workersReceived++;
        heavenHighlight.transform.position = pos;
        heavenHighlight.Play();
        if (world.cityBuilderManager.uiWonderSelection.activeStatus)
			world.cityBuilderManager.uiWonderSelection.UpdateUIWorkers(workersReceived, this);

		if (!StillNeedsWorkers())
            ThresholdCheck();
    }

    public bool StillNeedsWorkers()
    {
        return workersReceived < wonderData.workersNeeded;
    }

    public void ThresholdCheck()
    {
        if (StillNeedsWorkers())
        {
            if (smokeEmitter.isPlaying)
                StopSmokeEmitter();
            return;
        }

        if (!world.CheckWorldGold(totalGoldCost))
        {
            world.AddToGoldWonderWaitList(this);
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
            PlayFireworks();
            focusCam.CenterCameraNoFollow(centerPos);
            SetNewGO(mesh67Percent,meshComplete);
            LightCheck();
            PlaySmokeSplash();
            isConstructing = false;
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
            RemoveUnits();
			if (smokeEmitter.isPlaying)
				StopSmokeEmitter();

			world.cityBuilderManager.CreateAllWorkers(this);
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

		if (grasslandCount == Mathf.Ceil(wonderLocs.Count * 0.5f)) //turn to grassland if all are grassland
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
        hasHarbor = false;
        GameObject harbor = world.GetStructure(harborLoc);
        harbor.GetComponent<CityImprovement>().PlayRemoveEffect(false);
        harborImprovement = null;
        Destroy(harbor);
        world.RemoveSingleBuildFromCityLabor(harborLoc);
        world.RemoveStructure(harborLoc);
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
        if (world.IsUnitLocationTaken(unloadLoc))
        {
            Unit unit = world.GetUnit(unloadLoc);
            if (unit.isTrader)
            {
                world.RemoveUnitPosition(unloadLoc);
                if (unit.followingRoute)
                    unit.InterruptRoute();
                unit.TeleportToNearestRoad(unloadLoc);
            }
            else
                unit.FindNewSpot(unloadLoc, null);
        }

        foreach (Vector3Int neighbor in world.GetNeighborsFor(unloadLoc, MapWorld.State.EIGHTWAY))
        {
            if (world.IsUnitLocationTaken(neighbor))
            {
                Unit unit = world.GetUnit(neighbor);
                if (unit.isTrader)
                {
                    world.RemoveUnitPosition(neighbor);
                    if (unit.followingRoute)
                        unit.InterruptRoute();
                    unit.TeleportToNearestRoad(neighbor);
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
        world.UpdateWorldResources(ResourceType.Gold, amount);
        Vector3 loc = centerPos;
        loc.y += 0.4f;
        if (isActive)
            InfoResourcePopUpHandler.CreateResourceStat(loc, amount, ResourceHolder.Instance.GetIcon(ResourceType.Gold));
    }

    public void StopConstructing()
    {
        if (isBuilding)
        {
            StopSmokeEmitter();
            uiTimeProgressBar.gameObject.SetActive(false);
            StopCoroutine(buildingCo);
            int amount = totalGoldCost;
            world.UpdateWorldResources(ResourceType.Gold, amount);
            InfoResourcePopUpHandler.CreateResourceStat(centerPos, amount, ResourceHolder.Instance.GetIcon(ResourceType.Gold));
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

		List<Unit> tempWaitList = waitList.ToList();

		for (int i = 0; i < tempWaitList.Count; i++)
			data.waitList.Add(tempWaitList[i].id);

		List<Unit> tempSeaWaitList = seaWaitList.ToList();

		for (int i = 0; i < tempSeaWaitList.Count; i++)
			data.seaWaitList.Add(tempSeaWaitList[i].id);

		//public Dictionary<ResourceType, int> resourceDict, resourceCostDict, resourceGridDict;

		return data;
    }

    public void LoadData(WonderData data)
    {
        //centerPos = data.centerPos; //done elsewhere
        wonderName = data.name;
		unloadLoc = data.unloadLoc;
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
					this.waitList.Enqueue(world.traderList[j]);
					break;
				}
			}
		}
	}

	public void SetSeaWaitList(List<int> seaWaitList)
	{
		for (int i = 0; i < seaWaitList.Count; i++)
		{
			for (int j = 0; j < world.traderList.Count; j++)
			{
				if (world.traderList[j].id == seaWaitList[i])
				{
					this.seaWaitList.Enqueue(world.traderList[j]);
					break;
				}
			}
		}
	}
}
