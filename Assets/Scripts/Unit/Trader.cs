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
    public bool paid, hasRoute, atStop, followingRoute, waitingOnRouteCosts, interruptedRoute, guarded, waitingOnGuard, guardLeft;//, interruptedRoute;
    [HideInInspector]
    public Unit guardUnit;

    public int loadUnloadRate = 1;

    private Coroutine LoadUnloadCo, WaitTimeCo, waitingCo;
	private WaitForSeconds moveInLinePause = new WaitForSeconds(0.5f);

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
        tradeRouteManager = GetComponent<TradeRouteManager>();
        tradeRouteManager.SetTrader(this);
        trader = this;
        isTrader = true;
        personalResourceManager = GetComponent<PersonalResourceManager>();
        //tradeRouteManager.SetPersonalResourceManager(personalResourceManager);
        personalResourceManager.resourceStorageLimit = buildDataSO.cargoCapacity;
        if (bySea)
            ripples.SetActive(false);
    }

	private void Start()
	{
		tradeRouteManager.SetTradeRouteManager(world.unitMovement.uiTradeRouteManager);
        tradeRouteManager.SetUIPersonalResourceManager(world.unitMovement.uiPersonalResourceInfoPanel);
        personalResourceManager.SetUnit(this);
	}

    public void SetRouteManagers(UITradeRouteManager uiTradeRouteManager, UIPersonalResourceInfoPanel uiPersonalResourceInfoPanel)
    {
		tradeRouteManager.SetTradeRouteManager(uiTradeRouteManager);
		tradeRouteManager.SetUIPersonalResourceManager(uiPersonalResourceInfoPanel);
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
        totalRouteCosts = RouteCostCalculator(multiple, true);
	}

    public City GetStartingCity()
    {
        if (bySea)
            return world.GetHarborCity(tradeRouteManager.cityStops[0]);
        else
            return world.GetCity(tradeRouteManager.cityStops[0]);
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
                City city = GetStartingCity();
				if (city.ResourceManager.ConsumeResourcesForRouteCheck(totalRouteCosts))
                {
                    waitingOnRouteCosts = false;
					world.unitMovement.uiTradeRouteManager.ShowRouteCostFlag(false, this);
					city.ResourceManager.RemoveFromTraderWaitList(this);
					city.ResourceManager.RemoveFromResourcesNeededForTrader(routeCostTypes);
                    paid = true;
					SpendRouteCosts(0);
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
                    Rotate(harborRot);
                }

                atStop = true;
                isWaiting = true;
                tradeRouteManager.FinishedLoading.AddListener(BeginNextStepInRoute);
                WaitTimeCo = StartCoroutine(tradeRouteManager.WaitTimeCoroutine());
                LoadUnloadCo = StartCoroutine(tradeRouteManager.LoadUnloadCoroutine(loadUnloadRate));
            }
        }
    }

	public void InterruptRoute()
	{
		CancelRoute();
		interruptedRoute = true;
		if (isSelected)
			InterruptedRouteMessage();
		else
			SetInterruptedAnimation(true);
	}

    public void StartMoveUpInLine(int num)
    {
        waitingCo = StartCoroutine(MoveUpInLine(num));
    }

	public void GetInLine()
	{
		Vector3Int currentLoc = world.RoundToInt(transform.position);

		if (world.UnitAlreadyThere(this, currentLoc))
			currentLocation = currentLoc;
		else
		{
			ambushLoc = new Vector3Int(0, -10, 0); //no ambushing while in line
			if (world.IsUnitWaitingForSameStop(currentLoc, finalDestinationLoc))
			{
				GoToBackOfLine(world.RoundToInt(finalDestinationLoc), currentLoc);
				GetInLine();
				return;
			}

			currentLocation = world.AddUnitPosition(currentLoc, this);
		}
		isWaiting = true;
		StopAnimation();

		Vector3Int tradePos = world.GetStopLocation(world.GetTradeLoc(world.RoundToInt(finalDestinationLoc)));

		if (world.IsCityOnTile(tradePos))
			world.GetCity(tradePos).AddToWaitList(this);
		else if (world.IsWonderOnTile(tradePos))
			world.GetWonder(tradePos).AddToWaitList(this);
		else if (world.IsTradeCenterOnTile(tradePos))
			world.GetTradeCenter(tradePos).AddToWaitList(this);

		if (guarded)
			guardUnit.military.GuardGetInLine(prevTile, currentLocation);
	}

	public void GoToBackOfLine(Vector3Int finalLoc, Vector3Int currentLoc)
	{
		List<Vector3Int> positionsToCheck = new() { currentLoc };
		bool success = false;
		bool prevPath = true;
		List<Vector3Int> newPath = new();

		while (positionsToCheck.Count > 0)
		{
			Vector3Int current = positionsToCheck[0];
			positionsToCheck.Remove(current);
			newPath.Add(current);

			if (prevPath) //first check the tile the trader came from
			{
				prevPath = false;

				if (!world.IsUnitWaitingForSameStop(prevTile, finalLoc))
				{
					Teleport(prevTile);
					if (guarded)
						guardUnit.Teleport(world.RoundToInt(guardUnit.military.GuardRouteFinish(prevTile, prevTile)));
					positionsToCheck.Clear();
					success = true;
				}
				else
				{
					positionsToCheck.Add(prevTile);
				}
			}
			else
			{
				foreach (Vector3Int neighbor in world.GetNeighborsFor(current, MapWorld.State.EIGHTWAY))
				{
					if (!bySea)
					{
						if (!world.IsRoadOnTileLocation(neighbor))
							continue;
					}
					else
					{
						if (!world.CheckIfSeaPositionIsValid(neighbor))
							continue;
					}

					if (world.IsUnitWaitingForSameStop(neighbor, finalLoc)) //going away from final loc
					{
						if (GridSearch.ManhattanDistance(finalLoc, current) < GridSearch.ManhattanDistance(finalLoc, neighbor))
						{
							positionsToCheck.Add(neighbor);
							break;
						}
					}
					else
					{
						//teleport to back of line
						Teleport(neighbor);
						if (guarded)
							guardUnit.Teleport(world.RoundToInt(guardUnit.military.GuardRouteFinish(neighbor, neighbor)));
						positionsToCheck.Clear();
						success = true;
						break;
					}
				}
			}
		}

		if (!success)
		{
			InterruptRoute();
		}
		else
		{
			List<Vector3Int> oldPath = new(pathPositions);
			newPath.Reverse();
			newPath.AddRange(oldPath);
			pathPositions = new Queue<Vector3Int>(newPath);
		}
	}

	public void ExitLine()
	{
		world.RemoveUnitPosition(currentLocation);
		StartAnimation();
		Vector3Int nextSpot = pathPositions.Peek();
		RestartPath(pathPositions.Dequeue());

		if (guarded)
			guardUnit.military.GuardGetInLine(currentLocation, nextSpot);
	}

	public IEnumerator MoveUpInLine(int placeInLine)
	{
		if (pathPositions.Count == 1)
			yield break;

		float pause = 0.5f * placeInLine;

		while (pause > 0)
		{
			yield return moveInLinePause;// new WaitForSeconds(0.5f * placeInLine);
			pause -= 0.5f;
		}

		if (world.IsUnitWaitingForSameStop(pathPositions.Peek(), finalDestinationLoc))
			yield break;

		Vector3Int nextSpot = pathPositions.Dequeue();
		world.RemoveUnitPosition(currentLocation);
		if (world.IsUnitLocationTaken(nextSpot))
		{
			Unit unitInTheWay = world.GetUnit(nextSpot);
			unitInTheWay.FindNewSpot(nextSpot, pathPositions.Peek());
		}
		world.AddUnitPosition(nextSpot, this);
        StartAnimation();
        RestartPath(nextSpot);

		if (guarded)
			guardUnit.military.GuardGetInLine(currentLocation, nextSpot);
	}

	//restarting route after ambush
	public void ContinueTradeRoute()
	{
		if (guarded)
		{
			guardUnit.originalMoveSpeed = originalMoveSpeed;
			guardUnit.military.ContinueGuarding(pathPositions, currentLocation);
		}

		ambush = false;
        StartAnimation();
		RestartPath(pathPositions.Dequeue());
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
        paid = false;
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
            currentPath = GridSearch.AStarSearch(world, transform.position, nextStop, isTrader, bySea); //in case starting off path

        followingRoute = true;
        if (currentPath.Count == 0)
        {
            if (tradeRouteManager.CurrentDestination == world.RoundToInt(transform.position))
            {
                finalDestinationLoc = tradeRouteManager.CurrentDestination;
                TradeRouteCheck(transform.position);
            }
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

            if (guarded)
            {
                currentPath.Insert(0, world.RoundToInt(transform.position));
                currentPath.RemoveAt(currentPath.Count - 1);

                guardUnit.finalDestinationLoc = guardUnit.military.GuardRouteFinish(nextStop, currentPath[currentPath.Count - 1]);
                guardUnit.MoveThroughPath(currentPath);
            }
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

	public void InterruptedRouteMessage()
	{
		interruptedRoute = false;
		SetInterruptedAnimation(false);
		InfoPopUpHandler.WarningMessage().Create(transform.position, "Route not possible to complete");
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

    //different from original calculation because trader could start on different stop
    public void SpendRouteCosts(int stop)
    {
        City city = GetStartingCity();

        float multiple;
        if (stop == 0)
            multiple = paid ? 1 : 0;
        else
            multiple = (float)stop / tradeRouteManager.cityStops.Count;

		//float multiple = totalRouteLength /*- CalculateTilesTraveled(stop))*/ / (buildDataSO.movementSpeed * 24) * percTraveled;
        List<ResourceValue> newCosts = AdjustTotalCosts(multiple);
		city.ResourceManager.ConsumeResources(newCosts, 1, city.cityLoc, false, true);
    }

    public void RefundRouteCosts()
    {
        float multiple; 
        if (tradeRouteManager.currentStop == 0)
            multiple = paid ? 1 : 0;
        else
            multiple = (float)tradeRouteManager.currentStop / tradeRouteManager.cityStops.Count;

        paid = false;
		City city = GetStartingCity();

		city.ResourceManager.resourceCount = 0;
        for (int i = 0; i < totalRouteCosts.Count; i++)
        {
			int amount = Mathf.FloorToInt(totalRouteCosts[i].resourceAmount * multiple);

			if (amount > 0)
			{
				city.ResourceManager.AddResource(totalRouteCosts[i].resourceType, amount);
				Vector3 cityLoc = city.cityLoc;
				cityLoc.y += totalRouteCosts.Count * 0.4f;
				cityLoc.y += -0.4f * i;
				InfoResourcePopUpHandler.CreateResourceStat(cityLoc, amount, ResourceHolder.Instance.GetIcon(totalRouteCosts[i].resourceType));
			}
		}

        //Dictionary<ResourceType, ResourceValue> tempCostDict = new();
        //List<ResourceValue> combinedCycleCosts = new();

  //      if (guarded)
  //      {
  //          for (int i = 0; i < buildDataSO.cycleCost.Count; i++)
  //              tempCostDict[buildDataSO.cycleCost[i].resourceType] = buildDataSO.cycleCost[i];
            
  //          for (int i = 0; i < guardUnit.buildDataSO.cycleCost.Count; i++)
  //          {
  //              if (tempCostDict.ContainsKey(guardUnit.buildDataSO.cycleCost[i].resourceType))
  //              {
  //                  ResourceValue value = tempCostDict[guardUnit.buildDataSO.cycleCost[i].resourceType];
  //                  value.resourceAmount += guardUnit.buildDataSO.cycleCost[i].resourceAmount;
  //                  combinedCycleCosts.Add(value);
		//		}
  //              else
  //              {
  //                  combinedCycleCosts.Add(guardUnit.buildDataSO.cycleCost[i]);
  //              }
  //          }
  //      }
  //      else
  //      {
  //          combinedCycleCosts = new(buildDataSO.cycleCost);
  //      }

		//for (int i = 0; i < combinedCycleCosts.Count; i++)
		//{
  //          int amount = Mathf.FloorToInt(combinedCycleCosts[i].resourceAmount * multiple);

		//	if (amount > 0)
		//	{
  //              city.ResourceManager.CheckResource(combinedCycleCosts[i].resourceType, amount);
		//		Vector3 cityLoc = city.cityLoc;
		//		cityLoc.y += combinedCycleCosts.Count * 0.4f;
		//		cityLoc.y += -0.4f * i;
		//		InfoResourcePopUpHandler.CreateResourceStat(cityLoc, amount, ResourceHolder.Instance.GetIcon(combinedCycleCosts[i].resourceType));
		//	}
		//}
	}

    public void UnloadAll(City city)
    {
        personalResourceManager.UnloadAll(city);
    }

    private List<ResourceValue> RouteCostCalculator(float multiple, bool types)
    {
        List<ResourceValue> totalCosts = new();
        
        if (guarded)
		{
			Dictionary<ResourceType, ResourceValue> tempCostDict = new();
			for (int i = 0; i < buildDataSO.cycleCost.Count; i++)
			{
				ResourceValue value;
				value.resourceType = buildDataSO.cycleCost[i].resourceType;
				value.resourceAmount = Mathf.CeilToInt(buildDataSO.cycleCost[i].resourceAmount * multiple);
				tempCostDict[value.resourceType] = value;
			}

			for (int i = 0; i < guardUnit.buildDataSO.cycleCost.Count; i++)
			{
				ResourceValue tempValue = guardUnit.buildDataSO.cycleCost[i];
				if (tempCostDict.ContainsKey(tempValue.resourceType))
				{
					ResourceValue value = tempCostDict[tempValue.resourceType];
					value.resourceAmount += Mathf.CeilToInt(tempValue.resourceAmount * multiple);
					tempCostDict[tempValue.resourceType] = value;
				}
				else
				{
					ResourceValue value;
					value.resourceType = tempValue.resourceType;
					value.resourceAmount = Mathf.CeilToInt(tempValue.resourceAmount * multiple);
					tempCostDict[value.resourceType] = value;
				}
			}

			foreach (ResourceType type in tempCostDict.Keys)
			{
				totalCosts.Add(tempCostDict[type]);
                if (types)
    				routeCostTypes.Add(type);
			}
		}
		else
		{
			for (int i = 0; i < buildDataSO.cycleCost.Count; i++)
			{
				ResourceValue value;
				value.resourceType = buildDataSO.cycleCost[i].resourceType;
				value.resourceAmount = Mathf.CeilToInt(buildDataSO.cycleCost[i].resourceAmount * multiple);
				totalCosts.Add(value);
                if (types)
    				routeCostTypes.Add(value.resourceType);
			}
		}

        return totalCosts;
	}

    //for when starting on a different stop other than stop 0;
    private List<ResourceValue> AdjustTotalCosts(float multiple)
    {
        List<ResourceValue> adjCosts = new();

		for (int i = 0; i < totalRouteCosts.Count; i++)
		{
			int amount = Mathf.CeilToInt(totalRouteCosts[i].resourceAmount * multiple);

			if (amount > 0)
			{
                ResourceValue value = totalRouteCosts[i];
                value.resourceAmount = amount;
                adjCosts.Add(value);
			}
		}

		return adjCosts;
    }
    //private int CalculateTilesTraveled(int stop)
    //{
    //    int tilesTraveled = 0;
    //    int currentStop;

    //    if (stop == 0)
    //        currentStop = tradeRouteManager.cityStops.Count;
    //    else
    //        currentStop = stop;

    //    if (currentStop == 1)
    //        return tilesTraveled;

    //    for (int i = currentStop - 1; i > -1; i--)
    //        tilesTraveled += tradeRouteManager.RoutePathsDict[i].Count;

    //    return tilesTraveled;
    //}

    public void SetGuardLeftMessage()
    {
        if (isSelected)
            ShowGuardLeftMessage();
        else
            guardLeft = true;
    }

    public void ShowGuardLeftMessage()
    {
        guardLeft = false;
		InfoPopUpHandler.WarningMessage().Create(transform.position, "Guard returned to nearest city due to inactivity");
	}

    public void LookSad()
    {
		SetInterruptedAnimation(true);
    }

	public void TeleportToNearestRoad(Vector3Int loc)
	{
		foreach (Vector3Int neighbor in world.GetNeighborsFor(loc, MapWorld.State.EIGHTWAY))
		{
			if (world.IsRoadOnTileLocation(neighbor))
				return;
		}

		Vector3Int newSpot = loc;
		Vector3Int terrainLoc = world.GetClosestTerrainLoc(loc);

		foreach (Vector3Int neighbor in world.GetNeighborsFor(terrainLoc, MapWorld.State.EIGHTWAYINCREMENT))
		{
			if (world.CheckIfPositionIsValid(neighbor))
			{
				newSpot = neighbor;
				if (world.IsRoadOnTerrain(neighbor))
				{
					Teleport(neighbor);
					return;
				}
			}
		}

		Teleport(newSpot);
	}

	public void FinishMovementTrader(Vector3 endPosition)
    {
		if (isSelected)
			world.unitMovement.ShowIndividualCityButtonsUI();

		if (bySea)
		{
			if (!followingRoute && world.IsCityHarborOnTile(currentLocation))
			{
				City city = world.GetHarborCity(world.GetClosestTerrainLoc(currentLocation));
				city.tradersHere.Add(this);
				if (world.unitMovement.upgradingUnit)
					world.unitMovement.ToggleUnitHighlights(true, city);
			}
		}
		else
		{
			if (!followingRoute && world.IsCityOnTile(currentLocation))
			{
				City city = world.GetCity(world.GetClosestTerrainLoc(currentLocation));
				city.tradersHere.Add(this);
				if (world.unitMovement.upgradingUnit)
					world.unitMovement.ToggleUnitHighlights(true, city);
			}
			//if location has been taken away (such as when wonder finishes)
			if (!world.CheckIfPositionIsValid(world.GetClosestTerrainLoc(endPosition)))
			{
				if (followingRoute)
					InterruptRoute();
                TeleportToNearestRoad(world.RoundToInt(currentLocation));
				prevTile = currentLocation;
				return;
			}
		}

		if (followingRoute)
		{
			if (world.IsUnitWaitingForSameStop(currentLocation, finalDestinationLoc))
			{
				GoToBackOfLine(world.RoundToInt(finalDestinationLoc), currentLocation);
				GetInLine();
				prevTile = currentLocation;
				return;
			}
		}
		else if (guarded)
		{
			guardUnit.military.IdleCheck();
		}

		TradeRouteCheck(endPosition);
		prevTile = currentLocation;

		if (!followingRoute && world.IsUnitLocationTaken(currentLocation))
			UnitInWayCheck(endPosition);
        else
			world.AddUnitPosition(currentLocation, this);
	}

    public void KillTrader()
    {
		if (isSelected)
			world.unitMovement.ClearSelection();

		world.traderList.Remove(this);
		StartCoroutine(WaitKillUnit());

		if (ambush)
		{
			//assuming trader is last to be killed in ambush
			Vector3Int ambushLoc = world.GetClosestTerrainLoc(transform.position);
			if (world.GetTerrainDataAt(ambushLoc).treeHandler != null)
				world.GetTerrainDataAt(ambushLoc).ToggleTransparentForest(false);

			world.ClearAmbush(ambushLoc);
			world.uiAttackWarning.AttackWarningCheck(ambushLoc);

			if (world.tutorial && world.ambushes == 1)
			{
				world.mainPlayer.conversationHaver.SetSomethingToSay("first_ambush", world.azai);
			}

			if (world.mainPlayer.runningAway)
			{
				world.mainPlayer.StopRunningAway();
				world.mainPlayer.stepAside = false;
			}
		}
		else
		{
			StopMovementCheck(true);
			TradersHereCheck();

			if (followingRoute)
				CancelRoute();
		}
	}

	public TraderData SaveTraderData()
	{
		TraderData data = new();

        data.id = id;
        data.unitName = gameObject.name;
		data.unitNameAndLevel = buildDataSO.unitNameAndLevel;
		data.position = transform.position;
		data.rotation = transform.rotation;
		data.destinationLoc = destinationLoc;
		data.finalDestinationLoc = finalDestinationLoc;
		data.currentLocation = currentLocation;
		data.prevTile = prevTile;
		data.prevTerrainTile = prevTerrainTile;
		data.moveOrders = pathPositions.ToList();
		data.isMoving = isMoving;
        data.ambushLoc = ambushLoc;
        data.ambush = ambush;
        data.guarded = guarded;
        if (guarded)
            data.guardUnit = guardUnit.military.SaveMilitaryUnitData();
        else
            data.guardUnit = null;
        data.waitingOnGuard = waitingOnGuard;
        data.guardLeft = guardLeft;
        data.currentHealth = currentHealth;
        data.paid = paid;

		if (isMoving && !isWaiting)
			data.moveOrders.Insert(0, world.RoundToInt(destinationLoc));

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
        currentHealth = data.currentHealth;
        gameObject.name = data.unitName;
		destinationLoc = data.destinationLoc;
		finalDestinationLoc = data.finalDestinationLoc;
		currentLocation = data.currentLocation;
		prevTile = data.prevTile;
		prevTerrainTile = data.prevTerrainTile;
		isMoving = data.isMoving;
		interruptedRoute = data.interruptedRoute;
		atStop = data.atStop;
		followingRoute = data.followingRoute;
		isWaiting = data.isWaiting;
		isUpgrading = data.isUpgrading;
		hasRoute = data.hasRoute;
		waitingOnRouteCosts = data.waitingOnRouteCosts;
		resourceGridDict = data.resourceGridDict;
        ambushLoc = data.ambushLoc;
        ambush = data.ambush;
        guarded = data.guarded;
        waitingOnGuard = data.waitingOnGuard;
        guardLeft = data.guardLeft;
        paid = data.paid;

        if (guarded)
            world.CreateGuard(data.guardUnit, this);

		if (currentHealth < healthMax)
		{
			healthbar.LoadHealthLevel(currentHealth);
			healthbar.gameObject.SetActive(true);
		}

		if (!isMoving)
			world.AddUnitPosition(currentLocation, this);

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

            if (!isWaiting)
				GameLoader.Instance.unitMoveOrders[this] = data.moveOrders;
			//MoveThroughPath(data.moveOrders);
            else
                pathPositions = new Queue<Vector3Int>(data.moveOrders);
		}

        if (followingRoute)
        {
            CalculateRouteCosts();
            tradeRouteManager.waitTime = tradeRouteManager.waitTimes[tradeRouteManager.currentStop];

            if (atStop && !waitingOnRouteCosts)
            {
				Vector3Int stopLoc = world.GetStopLocation(world.GetTradeLoc(currentLocation));

				if (world.IsCityOnTile(stopLoc))
					tradeRouteManager.SetCity(world.GetCity(stopLoc));
				else if (world.IsWonderOnTile(stopLoc))
					tradeRouteManager.SetWonder(world.GetWonder(stopLoc));
				else if (world.IsTradeCenterOnTile(stopLoc))
					tradeRouteManager.SetTradeCenter(world.GetTradeCenter(stopLoc));

				tradeRouteManager.FinishedLoading.AddListener(BeginNextStepInRoute);
				WaitTimeCo = StartCoroutine(tradeRouteManager.WaitTimeCoroutine());
				LoadUnloadCo = StartCoroutine(tradeRouteManager.LoadUnloadCoroutine(loadUnloadRate));
			}
            else if (isWaiting)
            {

            }
		}
	}
}
