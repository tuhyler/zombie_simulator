using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Resources;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class Trader : Unit
{
    [HideInInspector]
    public PersonalResourceManager personalResourceManager;

    [HideInInspector]
    public TradeRouteManager tradeRouteManager;

    [HideInInspector]
    public List<ResourceValue> totalRouteCosts = new();
    [HideInInspector]
    public List<ResourceType> routeCostTypes = new();
    private int totalRouteLength;

    [HideInInspector]
    public int cargoStorageLimit;

    [HideInInspector]
    public bool hasRoute, waitingOnRouteCosts;//, interruptedRoute;

    public int loadUnloadRate = 1;

    [SerializeField]
    private GameObject ripples;

    private Coroutine LoadUnloadCo;
    private Coroutine WaitTimeCo;

    [HideInInspector]
    public Dictionary<ResourceType, int> resourceGridDict = new(); //order of resources shown

    //animations
    private int isInterruptedHash;
    private int isLoadingHash;
    private int isUnloadingHash;

    //private UnitMovement unitMovement;

    private void Awake()
    {
        AwakeMethods();
    }

    protected override void AwakeMethods()
    {
        base.AwakeMethods();
        isInterruptedHash = Animator.StringToHash("isInterrupted");
        isLoadingHash = Animator.StringToHash("isLoading");
        isUnloadingHash = Animator.StringToHash("isUnloading");
        cargoStorageLimit = buildDataSO.cargoCapacity;
        tradeRouteManager = GetComponent<TradeRouteManager>();
        tradeRouteManager.SetTrader(this);
        isTrader = true;
        personalResourceManager = GetComponent<PersonalResourceManager>();
        personalResourceManager.SetTrader(this);
        //tradeRouteManager.SetPersonalResourceManager(personalResourceManager);
        personalResourceManager.ResourceStorageLimit = cargoStorageLimit;
        if (bySea)
            ripples.SetActive(false);
    }

	private void Start()
	{
		tradeRouteManager.SetTradeRouteManager(world.unitMovement.uiTradeRouteManager);
        tradeRouteManager.SetUIPersonalResourceManager(world.unitMovement.uiPersonalResourceInfoPanel);
	}

    public void SetRouteManagers(UITradeRouteManager uiTradeRouteManager, UIPersonalResourceInfoPanel uiPersonalResourceInfoPanel)
    {
		tradeRouteManager.SetTradeRouteManager(uiTradeRouteManager);
		tradeRouteManager.SetUIPersonalResourceManager(uiPersonalResourceInfoPanel);
	}

	public void TurnOnRipples()
    {
        if (!isMoving)
        {
            ripples.SetActive(true);
            //for tweening
            LeanTween.alpha(ripples, 1f, 0.2f).setFrom(0f).setEase(LeanTweenType.linear);
        }
    }

    public override void TurnOffRipples()
    {
        LeanTween.alpha(ripples, 0f, 0.5f).setFrom(1f).setEase(LeanTweenType.linear).setOnComplete(SetActiveStatusFalse);
    }

    private void SetActiveStatusFalse()
    {
        ripples.SetActive(false);
    }

    //animations
    public override void SetInterruptedAnimation(bool v)
    {
        unitAnimator.SetBool(isInterruptedHash, v);
    }

    public void SetLoadingAnimation(bool v)
    {
        unitAnimator.SetBool(isLoadingHash, v);
    }

    public void SetUnloadingAnimation(bool v)
    {
        unitAnimator.SetBool(isUnloadingHash, v);
    }

    //passing details of the trade route
    public void SetTradeRoute(int startingStop, List<string> cityNames, List<List<ResourceValue>> resourceAssignments, List<int> waitTimes)
    {
        //tradeRouteManager.SetTrader(this);
        //tradeRouteManager.SetUIPersonalResourceManager(uiPersonalResourceInfoPanel);

        List<Vector3Int> tradeStops = new();

        foreach (string name in cityNames)
        {
            if (bySea)
                tradeStops.Add(world.GetHarborStopLocation(name));
            else
                tradeStops.Add(world.GetStopLocation(name));
        }

        if (tradeStops.Count > 0)
            hasRoute = true;
        else
            hasRoute = false;

        tradeRouteManager.SetTradeRoute(startingStop, tradeStops, resourceAssignments, waitTimes);
    }

    public List<ResourceValue> ShowRouteCost()
    {
        totalRouteLength = tradeRouteManager.CalculateRoutePaths(world);
        totalRouteCosts.Clear();
        routeCostTypes.Clear();

        CalculateRouteCosts();

        return totalRouteCosts;
    }

    private void CalculateRouteCosts()
    {
		float multiple = totalRouteLength / (buildDataSO.movementSpeed * 24); //24 is tiles per minute on road with speed of 1

		for (int i = 0; i < buildDataSO.cycleCost.Count; i++)
		{
			ResourceValue value;
			value.resourceType = buildDataSO.cycleCost[i].resourceType;
			value.resourceAmount = Mathf.CeilToInt(buildDataSO.cycleCost[i].resourceAmount * multiple);
			totalRouteCosts.Add(value);
			routeCostTypes.Add(value.resourceType);
		}
	}

    public Vector3Int GetStartingCity()
    {
        return tradeRouteManager.cityStops[0];
    }

    //public List<Vector3Int> GetCityStops()
    //{
    //    if (tradeRouteManager == null)
    //    {
    //        List<Vector3Int> cityStops = new();
    //        return cityStops;
    //    }

    //    return tradeRouteManager.CityStops;
    //}

    public void RestartRoute(Vector3Int cityLoc)
    {
        TradeRouteCheck(cityLoc);
    }

    protected override void TradeRouteCheck(Vector3 endPosition)
    {
        if (followingRoute)
        {
			if (tradeRouteManager.currentStop == 0)
            {
                City city = world.GetCity(GetStartingCity());
				if (city.ResourceManager.ConsumeResourcesForRouteCheck(totalRouteCosts))
                {
                    waitingOnRouteCosts = false;
					world.unitMovement.uiTradeRouteManager.ShowRouteCostFlag(false, this);
					city.ResourceManager.RemoveFromTraderWaitList(this);
					city.ResourceManager.RemoveFromResourcesNeededForTrader(routeCostTypes);
					SpendRouteCosts(1);
                }
                else
                {
                    waitingOnRouteCosts = true;
                    world.unitMovement.uiTradeRouteManager.ShowRouteCostFlag(true, this);
                    city.ResourceManager.AddToTraderWaitList(this);
					city.ResourceManager.AddToResourcesNeededForTrader(routeCostTypes);
					//SetInterruptedAnimation(true);
                    return;
				}
            }

			Vector3Int endLoc = Vector3Int.RoundToInt(endPosition);

            if (endLoc == tradeRouteManager.CurrentDestination)
            {
                //checking to see if stop still exists
                if (!world.CheckIfStopStillExists(endLoc))
                {
                    CancelRoute();
                    tradeRouteManager.RemoveStop(endLoc);
                    interruptedRoute = true;
                    if (isSelected)
                        InterruptedRouteMessage();
                    else
                        SetInterruptedAnimation(true);
                    return;
                }

                Vector3Int stopLoc = world.GetStopLocation(world.GetTradeLoc(endLoc));
                Vector3 harborRot = Vector3.zero;

                if (world.IsCityOnTile(stopLoc))
                {
                    tradeRouteManager.SetCity(world.GetCity(stopLoc));
                    if (bySea)
                        harborRot = world.GetStructure(endLoc).transform.localEulerAngles;
                }
                else if (world.IsWonderOnTile(stopLoc))
                {
                    tradeRouteManager.SetWonder(world.GetWonder(stopLoc));
                    if (bySea)
                        harborRot = world.GetStructure(endLoc).transform.localEulerAngles;
                }
                else if (world.IsTradeCenterOnTile(stopLoc))
                {
                    tradeRouteManager.SetTradeCenter(world.GetTradeCenter(stopLoc));
                    if (bySea)
                        harborRot = world.GetTradeCenter(stopLoc).transform.localEulerAngles;
                }

                if (bySea)
                {
                    harborRot.y += 90;
                    StartCoroutine(RotateCoroutine(harborRot));
                    //transform.localEulerAngles = harborRot; 
                }
                //if (bySea)
                //    tradeRouteManager.SetCity(world.GetHarborCity(endLoc));
                //else
                //    tradeRouteManager.SetCity(world.GetCity(endLoc));
                atStop = true;
                isWaiting = true;
                tradeRouteManager.FinishedLoading.AddListener(BeginNextStepInRoute);
                WaitTimeCo = StartCoroutine(tradeRouteManager.WaitTimeCoroutine());
                LoadUnloadCo = StartCoroutine(tradeRouteManager.LoadUnloadCoroutine(loadUnloadRate));

                //if (tradeRouteManager.GoToNextStopCheck(loadUnloadRate))
                //{
                //    BeginNextStepInRoute();
                //}
                //Deselect(); //lots of repetition here. 
                //routeManager.CompleteTradeRouteOrders();
            }
        }
    }

    private IEnumerator RotateCoroutine(Vector3 rot)
    {
        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = new Quaternion();
        endRotation.eulerAngles = rot;
        float timeElapsed = 0;

        while (timeElapsed < 0.2f)
        {
            timeElapsed += Time.deltaTime;
            float lerpStep = timeElapsed / 0.2f;
            transform.rotation = Quaternion.Lerp(startRotation, endRotation, lerpStep);

            yield return null;
        }
    }

    public override void BeginNextStepInRoute() //this does not have the finish movement listeners
    {
        tradeRouteManager.FinishedLoading.RemoveListener(BeginNextStepInRoute);
        if (LoadUnloadCo != null)
            StopCoroutine(LoadUnloadCo);
        LoadUnloadCo = null;
        if (WaitTimeCo != null)
            StopCoroutine(WaitTimeCo);
        WaitTimeCo = null;
        atStop = false;
        Vector3Int nextStop = tradeRouteManager.GoToNext();

        //checking to see if stop still exists
        if (!world.CheckIfStopStillExists(nextStop))
        {
            CancelRoute();
            tradeRouteManager.RemoveStop(nextStop);
            interruptedRoute = true;
            if (isSelected)
                InterruptedRouteMessage();
            else
                SetInterruptedAnimation(true);
            return;
        }

        List<Vector3Int> currentPath;
        
        if (followingRoute)
            currentPath = tradeRouteManager.GetNextPath();
        else
            currentPath = GridSearch.AStarSearch(world, transform.position, nextStop, isTrader, bySea);

        followingRoute = true;
        if (currentPath.Count == 0)
        {
            if (tradeRouteManager.CurrentDestination == world.RoundToInt(transform.position))
                TradeRouteCheck(transform.position);
            else
            {
                CancelRoute();
                tradeRouteManager.CheckQueues();

                interruptedRoute = true;
                if (isSelected)
                    InterruptedRouteMessage();
                else
                    SetInterruptedAnimation(true);
                return;
            }
        }
        else if (currentPath.Count > 0)
        {
            finalDestinationLoc = nextStop;
            MoveThroughPath(currentPath);
            tradeRouteManager.CheckQueues();
        }
    }

    public bool LineCutterCheck()
    {
        if (world.IsUnitWaitingForSameStop(world.RoundToInt(transform.position), tradeRouteManager.GoToNext()))
        {
            CancelRoute();
            InfoPopUpHandler.WarningMessage().Create(transform.position, "No cutting in line");
            return true;
        }

        return false;
    }

    public override void CancelRoute()
    {
        followingRoute = false;
        waitingOnRouteCosts = false;
        if (waitingCo != null)
            StopCoroutine(waitingCo);

        if (isWaiting)
        {
            Vector3Int tradePos = world.GetStopLocation(world.GetTradeLoc(world.RoundToInt(finalDestinationLoc)));

            if (world.IsCityOnTile(tradePos))
            {
                City city = world.GetCity(tradePos);
                city.RemoveFromWaitList(this);
                //city.CheckQueue();
            }
            else if (world.IsWonderOnTile(tradePos))
            {
                Wonder wonder = world.GetWonder(tradePos);
                wonder.RemoveFromWaitList(this);
                //wonder.CheckQueue();
            }
            else if (world.IsTradeCenterOnTile(tradePos))
            {
                TradeCenter center = world.GetTradeCenter(tradePos);
                center.RemoveFromWaitList(this);
                //center.CheckQueue();
            }
        }

        isWaiting = false;
        if (LoadUnloadCo != null)
        {
            //tradeRouteManager.StopHoldingPatternCoroutine();
            StopCoroutine(LoadUnloadCo);
            tradeRouteManager.CancelLoad();
            tradeRouteManager.StopHoldingPatternCoroutine();
            tradeRouteManager.FinishedLoading.RemoveListener(BeginNextStepInRoute);
            SetLoadingAnimation(false);
            SetUnloadingAnimation(false);
        }
        if (WaitTimeCo != null)
        {
            StopCoroutine(WaitTimeCo);
            tradeRouteManager.CancelLoad();
            tradeRouteManager.FinishedLoading.RemoveListener(BeginNextStepInRoute);
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

    public void SpendRouteCosts(int stop)
    {
		City city = world.GetCity(tradeRouteManager.cityStops[0]);

        List<ResourceValue> newCosts = new();
		float multiple = (totalRouteLength - CalculateTilesTraveled(stop)) / (buildDataSO.movementSpeed * 24);

		for (int i = 0; i < buildDataSO.cycleCost.Count; i++)
		{
			ResourceValue value;
			value.resourceType = buildDataSO.cycleCost[i].resourceType;
			value.resourceAmount = Mathf.CeilToInt(buildDataSO.cycleCost[i].resourceAmount * multiple);
			newCosts.Add(value);
		}

		world.GetCity(tradeRouteManager.cityStops[0]).ResourceManager.ConsumeResources(newCosts, 1, city.cityLoc, false, true);

    }

    public void RefundRouteCosts()
    {
		float multiple = (totalRouteLength - CalculateTilesTraveled(tradeRouteManager.currentStop)) / (buildDataSO.movementSpeed * 24);
        City city = world.GetCity(tradeRouteManager.cityStops[0]);

		for (int i = 0; i < buildDataSO.cycleCost.Count; i++)
		{
            int amount = Mathf.FloorToInt(buildDataSO.cycleCost[i].resourceAmount * multiple);

			if (amount > 0)
			{
                city.ResourceManager.CheckResource(buildDataSO.cycleCost[i].resourceType, amount);
				Vector3 cityLoc = city.cityLoc;
				cityLoc.y += buildDataSO.cycleCost.Count * 0.4f;
				cityLoc.y += -0.4f * i;
				InfoResourcePopUpHandler.CreateResourceStat(cityLoc, amount, ResourceHolder.Instance.GetIcon(buildDataSO.cycleCost[i].resourceType));
			}
		}
	}

    private int CalculateTilesTraveled(int stop)
    {
		int tilesTraveled = 0;
        int currentStop;

        if (stop == 0)
            currentStop = tradeRouteManager.cityStops.Count;
        else
            currentStop = stop;

        if (currentStop == 1)
            return tilesTraveled;

		for (int i = currentStop - 1; i > -1; i--)
			tilesTraveled += tradeRouteManager.RoutePathsDict[i].Count;

		return tilesTraveled;
    }

	public TraderData SaveTraderData()
	{
		TraderData data = new();

        data.id = id;
		data.unitNameAndLevel = buildDataSO.unitNameAndLevel;
		data.position = transform.position;
		data.rotation = transform.rotation;
		data.destinationLoc = destinationLoc;
		data.finalDestinationLoc = finalDestinationLoc;
		data.currentLocation = CurrentLocation;
		data.prevRoadTile = prevRoadTile;
		data.prevTerrainTile = prevTerrainTile;
		data.moveOrders = pathPositions.ToList();
		data.isMoving = isMoving;

		if (isMoving)
			data.moveOrders.Insert(0, world.RoundToInt(destinationLoc));

		data.moreToMove = moreToMove;
		data.interruptedRoute = interruptedRoute;
		data.atStop = atStop;
		data.followingRoute = followingRoute;
		data.isWaiting = isWaiting;
		data.isUpgrading = isUpgrading;
        data.hasRoute = hasRoute;
        data.waitingOnRouteCosts = waitingOnRouteCosts;
        data.resourceGridDict = resourceGridDict;

        //personal resource info
        data.resourceDict = personalResourceManager.ResourceDict;
        data.resourceStorageLevel = personalResourceManager.ResourceStorageLevel;

        //route info
        data.startingStop = tradeRouteManager.startingStop;
        data.cityStops = tradeRouteManager.cityStops;
        data.resourceAssignments = tradeRouteManager.resourceAssignments;
        data.resourceCompletion = tradeRouteManager.resourceCompletion;
        data.waitTimes = tradeRouteManager.waitTimes;
        data.currentStop = tradeRouteManager.currentStop;
        data.currentResource = tradeRouteManager.currentResource;
        data.resourceCurrentAmount = tradeRouteManager.resourceCurrentAmount;
        data.resourceTotalAmount = tradeRouteManager.resourceTotalAmount;
        data.timeWaited = tradeRouteManager.timeWaited;
        data.currentDestination = tradeRouteManager.CurrentDestination;
        data.resourceCheck = tradeRouteManager.resourceCheck;
        data.waitForever = tradeRouteManager.waitForever;
        //data.percDone = tradeRouteManager.percDone;

		return data;
	}

    public void LoadTraderData(TraderData data)
    {
		id = data.id;
		destinationLoc = data.destinationLoc;
		finalDestinationLoc = data.finalDestinationLoc;
		CurrentLocation = data.currentLocation;
		prevRoadTile = data.prevRoadTile;
		prevTerrainTile = data.prevTerrainTile;
		isMoving = data.isMoving;
		moreToMove = data.moreToMove;
		interruptedRoute = data.interruptedRoute;
		atStop = data.atStop;
		followingRoute = data.followingRoute;
		isWaiting = data.isWaiting;
		isUpgrading = data.isUpgrading;
		hasRoute = data.hasRoute;
		waitingOnRouteCosts = data.waitingOnRouteCosts;
		resourceGridDict = data.resourceGridDict;

		//personal resource info
		personalResourceManager.ResourceDict = data.resourceDict;
		personalResourceManager.ResourceStorageLevel = data.resourceStorageLevel;

		//route info
		tradeRouteManager.startingStop = data.startingStop;
		tradeRouteManager.cityStops = data.cityStops;
		tradeRouteManager.resourceAssignments = data.resourceAssignments;
		tradeRouteManager.resourceCompletion = data.resourceCompletion;
		tradeRouteManager.waitTimes = data.waitTimes;
		tradeRouteManager.currentStop = data.currentStop;
		tradeRouteManager.currentResource = data.currentResource;
		tradeRouteManager.resourceCurrentAmount = data.resourceCurrentAmount;
		tradeRouteManager.resourceTotalAmount = data.resourceTotalAmount;
        tradeRouteManager.timeWaited = data.timeWaited;
		tradeRouteManager.CurrentDestination = data.currentDestination;
		tradeRouteManager.resourceCheck = data.resourceCheck;
		tradeRouteManager.waitForever = data.waitForever;
		//tradeRouteManager.percDone = data.percDone;

		totalRouteLength = tradeRouteManager.CalculateRoutePaths(world);

		if (isMoving)
		{
			Vector3Int endPosition = world.RoundToInt(finalDestinationLoc);

			if (data.moveOrders.Count == 0)
				data.moveOrders.Add(endPosition);

			MoveThroughPath(data.moveOrders);
		}

        if (followingRoute)
        {
            CalculateRouteCosts();
            tradeRouteManager.waitTime = tradeRouteManager.waitTimes[tradeRouteManager.currentStop];

            if (atStop && !waitingOnRouteCosts)
            {
                if (world.IsCityOnTile(CurrentLocation))
				{
					tradeRouteManager.SetCity(world.GetCity(CurrentLocation));
				}
				else if (world.IsWonderOnTile(CurrentLocation))
				{
					tradeRouteManager.SetWonder(world.GetWonder(CurrentLocation));
				}
				else if (world.IsTradeCenterOnTile(CurrentLocation))
				{
					tradeRouteManager.SetTradeCenter(world.GetTradeCenter(CurrentLocation));
				}

				tradeRouteManager.FinishedLoading.AddListener(BeginNextStepInRoute);
				WaitTimeCo = StartCoroutine(tradeRouteManager.WaitTimeCoroutine());
				LoadUnloadCo = StartCoroutine(tradeRouteManager.LoadUnloadCoroutine(loadUnloadRate));
			}
		}
	}
}
