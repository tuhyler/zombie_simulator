using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class Worker : Unit
{
    //[HideInInspector]
    //public bool harvested;// harvesting, resourceIsNotNull;
    private ResourceIndividualHandler resourceIndividualHandler;
    private WorkerTaskManager workerTaskManager;
    private Vector3Int resourceCityLoc;
    private Resource resource;
    //private TimeProgressBar timeProgressBar;
    private UITimeProgressBar uiTimeProgressBar;
    private List<Vector3Int> orderList = new();
    public List<Vector3Int> OrderList { get { return orderList; } }
    private Queue<Vector3Int> orderQueue = new();
    [HideInInspector]
    public bool removing, gathering, clearingForest, buildingCity, working;
    public int clearingForestTime = 1;
    public int clearedForestlumberAmount = 100;

    //animations
    private int isWorkingHash;

    //dialogue stats

    private Coroutine workingCo;
    private WaitForSeconds workingWait = new(0.6111f); //for sound effects

    //[SerializeField]
    //private ParticleSystem removeSplash;

    private void Awake()
    {
        AwakeMethods();
        isWorkingHash = Animator.StringToHash("isWorking");
        isWorker = true;
        workerTaskManager = FindObjectOfType<WorkerTaskManager>();
        resourceIndividualHandler = FindObjectOfType<ResourceIndividualHandler>();
        //timeProgressBar = Instantiate(GameAssets.Instance.timeProgressPrefab, transform.position, Quaternion.Euler(90, 0, 0)).GetComponent<TimeProgressBar>();
        uiTimeProgressBar = Instantiate(GameAssets.Instance.uiTimeProgressPrefab, transform.position, Quaternion.Euler(90, 0, 0)).GetComponent<UITimeProgressBar>();
    }

    protected override void AwakeMethods()
    {
        base.AwakeMethods();
        //removeSplash = Instantiate(removeSplash, new Vector3(0, 0, 0), Quaternion.Euler(-90,0,0));
        //removeSplash.Stop();
    }

    //public void PlaySplash(Vector3 loc)
    //{
    //    removeSplash.transform.position = loc;
    //    removeSplash.Play();
    //}

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

    private IEnumerator PlayWorkSound()
    {
        while (working)
        {
            yield return workingWait;
        
            audioSource.clip = attacks[Random.Range(0, attacks.Length)];
            audioSource.Play();

            yield return workingWait;
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

        StopMovement();
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
            isBusy = false;
            if (isSelected)
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

            if (isSelected)
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
            isBusy = false;
            if (isSelected)
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

    public override void SkipRoadBuild()
    {
        RemoveFromOrderQueue();
        FinishedMoving.RemoveListener(BuildRoad);

        if (MoreOrdersToFollow())
        {
            BeginBuildingRoad();
        }
        else
        {
            isBusy = false;
            StopMovement();

            if (isSelected)
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
        if (isSelected)
            world.GetTerrainDataAt(workerTile).DisableHighlight();
        workerTaskManager.BuildRoad(workerTile, this);
    }

    public void RemoveRoad()
    {
        Vector3Int workerTile = orderQueue.Dequeue();
        orderList.Remove(workerTile);

        //Vector3 workerPos = transform.position;
        //Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);
        FinishedMoving.RemoveListener(RemoveRoad);

        TerrainData td = world.GetTerrainDataAt(workerTile);
        td.DisableHighlight();
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
        workerTaskManager.RemoveRoad(workerTile, this);
    }

    public void GatherResource()
    {
        Vector3 workerPos = transform.position;
        workerPos.y = 0;
        Vector3Int workerTile = world.GetClosestTerrainLoc(workerPos);

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
        ResourceIndividualSO resourceIndividual = resourceIndividualHandler.GetResourcePrefab(workerTile);
        if (resourceIndividual == null)
        {
            InfoPopUpHandler.WarningMessage().Create(workerPos, "No resource to harvest here");
            return;
        }
        //Debug.Log("Harvesting resource at " + workerPos);

        //resourceIndividualHandler.GenerateHarvestedResource(workerPos, workerUnit);
        StopMovement();
        //unitAnimator.SetBool(isWorkingHash, true);
        isBusy = true;
        //resourceIndividualHandler.SetWorker(this);
        gathering = true;
        workerTaskManager.GatherResource(workerPos, this, city, resourceIndividual, false);
    }

    public void ClearForest()
    {
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

        clearingForest = true;
		City city = world.GetCity(resourceCityLoc);
		ResourceIndividualSO resourceIndividual = resourceIndividualHandler.GetResourcePrefab(workerTile);

		StopMovement();
        SetWorkAnimation(true);
		isBusy = true;
		workerTaskManager.GatherResource(workerPos, this, city, resourceIndividual, true);
	}

    public void RemoveWorkLocation()
    {
        world.RemoveWorkerWorkLocation(world.RoundToInt(transform.position));
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
        isBusy = true;
        workerTaskManager.BuildCityPreparations(workerTile, this);
    }

    public bool CheckForCity(Vector3Int workerPos) //finds if city is nearby, returns it (interface? in WorkerTaskManager)
    {
        foreach (Vector3Int tile in world.GetNeighborsFor(workerPos, MapWorld.State.CITYRADIUS))
        {
            if (!world.IsCityOnTile(tile))
            {
                continue;
            }
            else
            {
                resourceCityLoc = tile;
                return true;
            }
        }

        return false;
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

    public WorkerData SaveWorkerData()
    {
        WorkerData data = new();

		data.unitNameAndLevel = buildDataSO.unitNameAndLevel;
		data.secondaryPrefab = secondaryPrefab;
		data.position = transform.position;
		data.rotation = transform.rotation;
		data.destinationLoc = destinationLoc;
		data.finalDestinationLoc = finalDestinationLoc;
		data.currentLocation = CurrentLocation;
		data.prevTerrainTile = prevTerrainTile;
		data.moveOrders = pathPositions.ToList();
		data.isMoving = isMoving;
		data.moreToMove = moreToMove;
        data.somethingToSay = somethingToSay;
        data.isBusy = isBusy;
        data.removing = removing;
        data.gathering = gathering;
        data.clearingForest = clearingForest;
        data.buildingCity = buildingCity;
        data.harvested = harvested;
        data.harvestedForest = harvestedForest;
        data.orderList = orderList;

        if (removing)
        {
            data.timePassed = workerTaskManager.roadManager.timePassed;
        }
        else if (gathering || clearingForest)
        {
            data.timePassed = workerTaskManager.resourceIndividualHandler.timePassed;
        }
        else if (buildingCity)
        {
            data.timePassed = workerTaskManager.timePassed;
        }
        else
        {
			data.timePassed = workerTaskManager.roadManager.timePassed;
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
		CurrentLocation = data.currentLocation;
		prevTerrainTile = data.prevTerrainTile;
		isMoving = data.isMoving;
        isBusy = data.isBusy;
		moreToMove = data.moreToMove;
		somethingToSay = data.somethingToSay;
        removing = data.removing;
        gathering = data.gathering;
        clearingForest = data.clearingForest;
        buildingCity = data.buildingCity;
        harvested = data.harvested;
        harvestedForest = data.harvestedForest;
        orderList = data.orderList;
		orderQueue = new Queue<Vector3Int>(orderList);

		if (isMoving)
		{
			Vector3Int endPosition = world.RoundToInt(finalDestinationLoc);

			if (data.moveOrders.Count == 0)
				data.moveOrders.Add(endPosition);

			MoveThroughPath(data.moveOrders);

            if (isBusy)
            {
                if (removing)
                {
                    FinishedMoving.AddListener(RemoveRoad);
                }
                else
                {
                    FinishedMoving.AddListener(BuildRoad);
                }
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

			workerTaskManager.resourceIndividualHandler.LoadHarvestedResource(workerPos, resourceIndividualHandler.GetResourcePrefab(workerTile), city, this, harvestedForest);
        }
        else if (isBusy)
        {
			if (removing)
			{
				workerTaskManager.LoadRemoveRoadCoroutine(data.timePassed, CurrentLocation, this);
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

    			workerTaskManager.LoadGatherResourceCoroutine(data.timePassed, workerPos, this, city, resourceIndividualHandler.GetResourcePrefab(workerTile), clearingForest);
			}
			else if (buildingCity)
			{
                workerTaskManager.LoadBuildCityCoroutine(data.timePassed, CurrentLocation, this);	
			}
			else
			{
                workerTaskManager.LoadRoadBuildCoroutine(data.timePassed, CurrentLocation, this);
			}
		}
	}
}
