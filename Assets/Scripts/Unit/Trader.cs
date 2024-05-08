using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Trader : Unit, ICityGoldWait, ICityResourceWait
{
    [HideInInspector]
    public PersonalResourceManager personalResourceManager;

    [HideInInspector]
    public TradeRouteManager tradeRouteManager;

    [HideInInspector]
    public List<ResourceValue> totalRouteCosts = new();
    //[HideInInspector]
    //public List<ResourceType> routeCostTypes = new();
    private int totalRouteLength, linePause;

    [HideInInspector]
    public bool paid, hasRoute, atStop, followingRoute, waitingOnRouteCosts, interruptedRoute, guarded, waitingOnGuard, guardLeft, atHome, returning, movingUpInLine, atStall;
    [HideInInspector]
    public Unit guardUnit;
	[HideInInspector]
	public Vector3Int homeCity;

    public int loadUnloadRate = 1;
	private float inLineSpeed = 0.5f;

	[HideInInspector]
	public int goldNeeded;
	
    private Coroutine LoadUnloadCo, WaitTimeCo, waitingCo;
	private WaitForSeconds moveInLinePause = new WaitForSeconds(1f);

	[HideInInspector]
    public Dictionary<ResourceType, int> resourceGridDict = new(); //order of resources shown

	Vector3Int ICityGoldWait.WaitLoc => new Vector3Int(0, -10, 0);
	Vector3Int ICityResourceWait.WaitLoc => new Vector3Int(0, -10, 0);
	int ICityGoldWait.waitId => id;
	int ICityResourceWait.waitId => id;

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
		ambushLoc = new Vector3Int(0, -10, 0);
        personalResourceManager = GetComponent<PersonalResourceManager>();
        //tradeRouteManager.SetPersonalResourceManager(personalResourceManager);
        personalResourceManager.resourceStorageLimit = buildDataSO.cargoCapacity;
        if (bySea)
            ripples.SetActive(false);
    }

	private void Start()
	{
		tradeRouteManager.SetTradeRouteManager(world.unitMovement.uiTradeRouteManager);
        //tradeRouteManager.SetUIPersonalResourceManager(world.unitMovement.uiPersonalResourceInfoPanel);
        personalResourceManager.SetUnit(this);
	}

    public void SetRouteManagers(UITradeRouteManager uiTradeRouteManager, UIPersonalResourceInfoPanel uiPersonalResourceInfoPanel)
    {
		tradeRouteManager.SetTradeRouteManager(uiTradeRouteManager);
		//tradeRouteManager.SetUIPersonalResourceManager(uiPersonalResourceInfoPanel);
	}

    //animations
    public void SetInterruptedAnimation(bool v)
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
			tradeStops.Add(world.GetStopLocation(name, buildDataSO.singleBuildType));

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
		//routeCostTypes.Clear();

        CalculateRouteCosts();

        return totalRouteCosts;
    }

    private void CalculateRouteCosts()
    {
		float multiple = totalRouteLength / (buildDataSO.movementSpeed * 24); //24 is tiles per minute on road with speed of 1
        totalRouteCosts = RouteCostCalculator(multiple);
	}

	public bool StartingCityCheck()
	{
		return world.IsSingleBuildStopOnTile(tradeRouteManager.cityStops[0], buildDataSO.singleBuildType);
	}

    public City GetStartingCity()
    {
		return world.GetSingleBuildStopCity(tradeRouteManager.cityStops[0]);
    }

	public bool RestartGold(int gold)
	{
		if (goldNeeded <= gold)
		{
			TradeRouteCheck(tradeRouteManager.cityStops[0]);
			return true;
		}
		else
		{
			return false;
		}
	}

	public bool Restart(ResourceType type)
	{
		if (waitingOnRouteCosts)
		{
			City city = GetStartingCity();

			if (city.ResourceManager.RouteCostCheck(totalRouteCosts, type))
			{
				TradeRouteCheck(city.cityLoc);
				return true;
			}
			else
			{
				return false;
			}
		}
		else
		{
			return tradeRouteManager.ResourceWaitCheck(type);
		}
	}

    //public void RestartRoute(Vector3Int cityLoc)
    //{
    //    TradeRouteCheck(cityLoc);
    //}

    public void TradeRouteCheck(Vector3 endPosition)
    {
        if (followingRoute)
        {
			movingUpInLine = false;
			Vector3Int endLoc = world.RoundToInt(endPosition);
			if (!TradeStopExistsCheck(endLoc))
				return;

			if (endLoc == tradeRouteManager.currentDestination)
			{
				tradeRouteManager.SetWaitTime();
				
				if (bySea)
					RotateTrader(endLoc);

				ITradeStop stop = world.GetStop(endLoc);

				if (isWaiting)
					stop.InLineCheck(this, stop);

				currentLocation = world.AddTraderPosition(endLoc, this);

				if (tradeRouteManager.currentStop == 0)
				{
					City city = world.GetCity(stop.mainLoc);
					
					if (city.ResourceManager.ConsumeResourcesForRouteCheck(totalRouteCosts, this))
					{
						waitingOnRouteCosts = false;
						RemoveWarning();
						world.unitMovement.uiTradeRouteManager.ShowRouteCostFlag(false, this);
						paid = true;
						SpendRouteCosts(0, city);
					}
					else
					{
						waitingOnRouteCosts = true;
						isWaiting = true;
						originalMoveSpeed = inLineSpeed;
						SetWarning(false, true, false);
						world.unitMovement.uiTradeRouteManager.ShowRouteCostFlag(true, this);
						return;
					}
				}

				tradeRouteManager.SetStop(stop);

				isWaiting = true;
				originalMoveSpeed = inLineSpeed;
				atStop = true;
                //tradeRouteManager.FinishedLoading.AddListener(BeginNextStepInRoute);
                WaitTimeCo = StartCoroutine(tradeRouteManager.WaitTimeCoroutine());
                LoadUnloadCo = StartCoroutine(tradeRouteManager.LoadUnloadCoroutine(loadUnloadRate, false));
			}
        }
    }

	private void RotateTrader(Vector3Int endLoc)
	{
		int rot = Mathf.RoundToInt(world.GetStructure(endLoc).transform.localEulerAngles.y / 90);

		if (rot == 3)
			rot = 0;
		else
			rot += 1;
		Vector3Int harborRot = world.GetNeighborsCoordinates(MapWorld.State.FOURWAYINCREMENT)[rot] + endLoc;
		Rotate(harborRot);
	}

    //checking to see if stop still exists
	public bool TradeStopExistsCheck(Vector3Int endLoc)
	{
		if (!world.StopExistsCheck(endLoc))
		{
			TraderStallCheck();
			tradeRouteManager.CheckQueues();
			InterruptRoute(true);
			tradeRouteManager.RemoveStop(endLoc);
			interruptedRoute = true;
			return false;
		}

		return true;
	}

	public void InterruptRoute(bool message)
	{
		CancelRoute();

		if (message)
		{
			interruptedRoute = true;
			if (isSelected)
				InterruptedRouteMessage();
		}
		//else
		//	SetInterruptedAnimation(true);
	}

    public void StartMoveUpInLine(int num)
    {
        waitingCo = StartCoroutine(MoveUpInLine(num));
    }

	public void GetInLine()
	{
		movingUpInLine = false;
		Vector3Int currentLoc = world.RoundToInt(transform.position);
		ITradeStop stop = world.GetStop(tradeRouteManager.currentDestination);

		if (!stop.TraderAlreadyThere(currentLoc, this, world))
		{
			ambushLoc = new Vector3Int(0, -10, 0); //no ambushing while in line
			if (world.IsTraderWaitingForSameStop(currentLoc, tradeRouteManager.currentDestination, this))
			{
				GoToBackOfLine(tradeRouteManager.currentDestination, currentLoc);
				GetInLine();
				return;
			}

			//currentLocation = world.AddUnitPosition(currentLoc, this);
		}

		isWaiting = true;
		originalMoveSpeed = inLineSpeed; //in line speed;
		StopAnimation();
		currentLocation = world.AddTraderPosition(currentLoc, this);
		stop.AddToWaitList(this, stop);

		if (guarded && /*!atHome &&*/ guardUnit.isMoving)
			guardUnit.military.GuardGetInLine(prevTile, currentLocation);
	}

	private void GoToBackOfLine(Vector3Int finalLoc, Vector3Int currentLoc)
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

				if (!world.IsTraderWaitingForSameStop(prevTile, finalLoc, this))
				{
					Teleport(prevTile);
					if (guarded)
					{
						Vector3Int prevPrevTile = prevTile - (currentLoc - prevTile);
						guardUnit.Teleport(world.RoundToInt(guardUnit.military.GuardRouteFinish(prevTile, prevPrevTile)));
						guardUnit.StopMovementCheck(true);
					}
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
				List<Vector3Int> checkList = world.GetNeighborsFor(current, MapWorld.State.EIGHTWAY);

				//prioritizing road locs for land traders
				if (!bySea && !byAir)
				{
					int count = checkList.Count;
					for (int i = 1; i < count; i++) //starting on one
					{
						if (world.IsRoadOnTileLocation(checkList[i]))
						{
							Vector3Int tile = checkList[i];
							checkList.RemoveAt(i);
							checkList.Insert(0, tile);
						}
					}
				}

				foreach (Vector3Int neighbor in world.GetNeighborsFor(current, MapWorld.State.EIGHTWAY))
				{
					if (bySea)
					{
						if (!world.CheckIfSeaPositionIsValid(neighbor))
							continue;
					}

					if (world.IsTraderWaitingForSameStop(neighbor, finalLoc, this)) //going away from final loc
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
							guardUnit.Teleport(neighbor + ((Vector3)neighbor*0.5f)); //teleport guard to be right behind it
						positionsToCheck.Clear();
						success = true;
						break;
					}
				}
			}
		}

		if (!success)
		{
			InterruptRoute(true);
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
		world.RemoveTraderPosition(currentLocation, this);
		
		if (!movingUpInLine)
		{	
			if (pathPositions.Count == 0)
				pathPositions.Enqueue(tradeRouteManager.currentDestination);

			Vector3Int nextSpot = pathPositions.Peek();
			StartAnimation();
			currentLocation = world.AddTraderPosition(nextSpot, this);
			isMoving = true;
			RestartPath(pathPositions.Dequeue());

			if (guarded /*&& !atHome*/)
				guardUnit.military.GuardGetInLine(prevTile, nextSpot);
		}
	}

	public IEnumerator MoveUpInLine(int placeInLine)
	{
		if (pathPositions.Count <= 1)
			yield break;

		movingUpInLine = true;
		linePause = 1 * placeInLine;

		while (linePause > 0)
		{
			yield return moveInLinePause;// new WaitForSeconds(0.5f * placeInLine);
			linePause -= 1;
		}

		if (!world.StopExistsCheck(tradeRouteManager.currentDestination))
		{
			InterruptRoute(true);
			yield break;
		}

		if (world.IsTraderWaitingForSameStop(pathPositions.Peek(), tradeRouteManager.currentDestination, this))
		{
			movingUpInLine = false;
			yield break;
		}

		Vector3Int nextSpot = pathPositions.Dequeue();
		world.RemoveTraderPosition(currentLocation, this);
		currentLocation = world.AddTraderPosition(nextSpot, this);
        StartAnimation();
		isMoving = true;
        RestartPath(nextSpot);

		if (guarded /*&& !atHome*/)
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

		world.RemoveUnitPosition(currentLocation);
		ambush = false;
		SetInterruptedAnimation(false);
        StartAnimation();
		isMoving = true;
		RestartPath(pathPositions.Dequeue());
	}

	public void BeginNextStepInRoute() //this does not have the finish movement listeners
    {
        //tradeRouteManager.FinishedLoading.RemoveListener(BeginNextStepInRoute);
        if (LoadUnloadCo != null)
            StopCoroutine(LoadUnloadCo);
        LoadUnloadCo = null;
        if (WaitTimeCo != null)
            StopCoroutine(WaitTimeCo);
        WaitTimeCo = null;
        atStop = false;
        paid = false;
		tradeRouteManager.currentDestination = tradeRouteManager.cityStops[tradeRouteManager.currentStop];
        Vector3Int nextStop = tradeRouteManager.currentDestination;

		if (!TradeStopExistsCheck(nextStop))
			return;

        List<Vector3Int> currentPath;
        
		if (atHome)
		{
			atHome = false;
			//tradeRouteManager.SetStop(world.GetStop(nextStop));
			currentPath = GridSearch.MilitaryMove(world, transform.position, nextStop, bySea);
		}
        else if (followingRoute)
		{
            currentPath = tradeRouteManager.GetNextPath();
		}
		else //in case starting off path
		{
            currentPath = GridSearch.TraderMove(world, transform.position, nextStop, bySea); 
		}

        followingRoute = true;
        if (currentPath.Count == 0)
        {
            if (tradeRouteManager.currentDestination == world.RoundToInt(transform.position))
            {
                finalDestinationLoc = tradeRouteManager.currentDestination;
                TradeRouteCheck(transform.position);
            }
            else
            {
                CancelRoute();

				TraderStallCheck();
				tradeRouteManager.CheckQueues();

                interruptedRoute = true;
                if (isSelected)
                    InterruptedRouteMessage();
                return;
            }
        }
        else if (currentPath.Count > 0)
        {
			finalDestinationLoc = nextStop;
			world.RemoveTraderPosition(currentLocation, this);
			TraderStallCheck();
            tradeRouteManager.CheckQueues();
			MoveThroughPath(currentPath);
			GuardFollow(nextStop, new(currentPath));
        }
    }

	private void TraderStallCheck()
	{
		if (atStall)
		{
			trader.world.GetCityDevelopment(world.GetClosestTerrainLoc(trader.currentLocation)).RemoveTraderFromStall(trader.currentLocation);
			atStall = false;
		}
	}

	private void GuardFollow(Vector3Int destination, List<Vector3Int> currentPath)
	{
		if (guarded)
		{
			guardUnit.StopMovementCheck(false);
			currentPath.Insert(0, world.RoundToInt(transform.position));
			currentPath.RemoveAt(currentPath.Count - 1);

			guardUnit.finalDestinationLoc = guardUnit.military.GuardRouteFinish(destination, currentPath[currentPath.Count - 1]);
			guardUnit.MoveThroughPath(currentPath);
		}
	}

	public void PrepForRoute(Vector3Int firstStep)
	{
		//Vector3Int firstStep = bySea ? world.GetCity(homeCity).singleBuildDict[SingleBuildType.Harbor] : homeCity;
		List<Vector3Int> path = GridSearch.MilitaryMove(world, transform.position, firstStep, bySea);

		if (path.Count == 0)
		{
			path = GridSearch.MoveWherever(world, transform.position, firstStep);

			if (path.Count == 0)
				path.Add(firstStep);
		}

		followingRoute = true;
		finalDestinationLoc = firstStep;
		MoveThroughPath(path);
	}

	private void ReturnToStall()
	{
		GoToStall();

		if (guarded)
		{
			guarded = false;
			guardUnit.military.GuardToBunkCheck(homeCity);
			guardUnit = null;
	
			if (world.uiTradeRouteBeginTooltip.activeStatus && world.uiTradeRouteBeginTooltip.trader == this)
				world.uiTradeRouteBeginTooltip.UpdateGuardCosts();
		}
	}

	public void GoToStall()
	{
		//Walking on land to get to stall
		atHome = true;
		Vector3Int stallLoc = world.GetTraderBuildLoc(world.GetCity(homeCity).singleBuildDict[buildDataSO.singleBuildType]);
		List<Vector3Int> path = GridSearch.MilitaryMove(world, transform.position, stallLoc, bySea);

		if (path.Count == 0)
		{
			path = GridSearch.MoveWherever(world, transform.position, stallLoc);

			if (path.Count == 0)
				path.Add(stallLoc);
		}

		finalDestinationLoc = stallLoc;
		MoveThroughPath(path);
	}

	public void ReturnHome(int tries)
	{
		tries++;
		Vector3Int currentLoc = world.GetClosestTerrainLoc(transform.position);
		returning = true;
				
		if (world.IsCityOnTile(homeCity) && world.GetCity(homeCity).singleBuildDict.ContainsKey(buildDataSO.singleBuildType))
		{
			Vector3Int homeLoc = world.GetCity(homeCity).singleBuildDict[buildDataSO.singleBuildType];
			if (currentLoc == homeLoc)
			{
				ReturnToStall();
				return;
			}

			List<Vector3Int> pathHome = GridSearch.MilitaryMove(world, transform.position, homeLoc, bySea); //allowing movement off road to get back home

			if (pathHome.Count > 0)
			{
				finalDestinationLoc = homeLoc;
				MoveThroughPath(pathHome);
				GuardFollow(homeLoc, new(pathHome));
				return;
			}
		}

		//if can't get back home, look for next closest city connected by road with trade depot
		List<string> cityNames = world.GetConnectedCityNames(currentLoc, bySea, true, buildDataSO.singleBuildType);

		City chosenCity = null;
		int dist = 0;
		bool firstOne = true;

		for (int i = 0; i < cityNames.Count; i++)
		{
			City city = world.GetCity(world.GetStopMainLocation(cityNames[i]));
				
			if (firstOne)
			{
				firstOne = false;
				chosenCity = city;
				dist = Mathf.Abs(currentLoc.x - city.cityLoc.x) + Mathf.Abs(currentLoc.z - city.cityLoc.z);
				continue;
			}

			int newDist = Mathf.Abs(currentLoc.x - city.cityLoc.x) + Mathf.Abs(currentLoc.z - city.cityLoc.z);
			if (newDist < dist)
			{
				chosenCity = city;
				dist = newDist;
			}
		}

		if (chosenCity == null || tries >= 3)
		{
			if (!FindCityToJoin(cityNames, currentLoc))
			{
				if (guarded)
					guardUnit.KillUnit(Vector3.zero);

				KillUnit(Vector3.zero);
			}
			
			return;
		}

		homeCity = chosenCity.cityLoc;
		atHome = false;
		ReturnHome(tries);
	}

	private bool FindCityToJoin(List<string> cityNames, Vector3Int currentLoc)
	{
		if (bySea || byAir)
			return false;
		
		if (world.IsCityOnTile(currentLoc))
		{
			if (guarded)
			{
				guarded = false;
				guardUnit.military.GuardToBunkCheck(currentLoc);
				guardUnit = null;

				if (world.uiTradeRouteBeginTooltip.activeStatus && world.uiTradeRouteBeginTooltip.trader == this)
					world.uiTradeRouteBeginTooltip.UpdateGuardCosts();
			}
			
			world.unitMovement.AddToCity(world.GetCity(currentLoc), this);
			//DestroyUnit();
			return true;
		}

		City chosenCity = null;
		int dist = 0;
		bool firstOne = true;

		for (int i = 0; i < cityNames.Count; i++)
		{
			City city = world.GetCity(world.GetStopMainLocation(cityNames[i]));

			if (firstOne)
			{
				firstOne = false;
				chosenCity = city;
				dist = Mathf.Abs(currentLoc.x - city.cityLoc.x) + Mathf.Abs(currentLoc.z - city.cityLoc.z);
				continue;
			}

			int newDist = Mathf.Abs(currentLoc.x - city.cityLoc.x) + Mathf.Abs(currentLoc.z - city.cityLoc.z);
			if (newDist < dist)
			{
				chosenCity = city;
				dist = newDist;
			}
		}

		if (chosenCity != null)
		{
			List<Vector3Int> pathHome = GridSearch.TraderMove(world, transform.position, chosenCity.cityLoc, bySea);
			
			if (pathHome.Count > 0)
			{
				if (guarded)
					GuardFollow(chosenCity.cityLoc, new(pathHome));
				finalDestinationLoc = chosenCity.cityLoc;
				MoveThroughPath(pathHome);
				
				return true;
			}
		}
		
		return false;
	}

	public bool LineCutterCheck()
    {
        if (world.IsTraderWaitingForSameStop(world.RoundToInt(transform.position), tradeRouteManager.currentDestination, this))
        {
            InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(transform.position, "No cutting in line");
            return true;
        }

        return false;
    }

	public void InterruptedRouteMessage()
	{
		interruptedRoute = false;
		//SetInterruptedAnimation(false);
		InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(transform.position, "Route not possible to complete");
	}

	public void CancelRoute()
    {
        if (isSelected)
			world.unitMovement.uiTraderPanel.SwitchRouteIcons(false);

		StopMovementCheck(false);

		followingRoute = false;
		RemoveWarning();
		if (waitingOnRouteCosts)
		{
			if (StartingCityCheck())
			{
				City city = GetStartingCity();
				int place = city.ResourceManager.RemoveFromGoldWaitList(this);
				if (place >= 0)
					trader.world.RemoveCityFromGoldWaitList(city, place);
				city.ResourceManager.RemoveFromCityResourceWaitList(this, totalRouteCosts);
				world.GetCityDevelopment(tradeRouteManager.currentDestination).RemoveTraderFromStall(currentLocation);
			}

			waitingOnRouteCosts = false;
		}
		
        if (waitingCo != null)
            StopCoroutine(waitingCo);

        if (isWaiting)
        {
			if (world.StopExistsCheck(tradeRouteManager.currentDestination))
			{
				ITradeStop stop = world.GetStop(tradeRouteManager.currentDestination);
				stop.RemoveFromWaitList(this, stop);
			}
        }

		movingUpInLine = true;
        isWaiting = false;
		originalMoveSpeed = buildDataSO.movementSpeed;
		if (LoadUnloadCo != null)
        {
			world.GetCityDevelopment(tradeRouteManager.currentDestination).RemoveTraderFromStall(currentLocation);
            tradeRouteManager.StopHoldingPatternCoroutine();
            StopCoroutine(LoadUnloadCo);
            tradeRouteManager.CancelLoad();
            //tradeRouteManager.FinishedLoading.RemoveListener(BeginNextStepInRoute);
            SetLoadingAnimation(false);
            SetUnloadingAnimation(false);
        }
        if (WaitTimeCo != null)
        {
            StopCoroutine(WaitTimeCo);
            tradeRouteManager.CancelLoad();
            //tradeRouteManager.FinishedLoading.RemoveListener(BeginNextStepInRoute);
		}

		atStop = false;
		world.RemoveTraderPosition(currentLocation, this);
		if (!isDead)
			ReturnHome(0);
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
    public void SpendRouteCosts(int stop, City city)
    {
		Vector3Int loc = city.singleBuildDict[buildDataSO.singleBuildType];

        float multiple;
        if (stop == 0)
            multiple = paid ? 1 : 0;
        else
            multiple = (float)stop / tradeRouteManager.cityStops.Count;

        List<ResourceValue> newCosts = AdjustTotalCosts(multiple);
		city.ResourceManager.ConsumeMaintenanceResources(newCosts, loc, false, true);
    }

    public void RefundRouteCosts()
    {
        float multiple; 
        if (tradeRouteManager.currentStop == 0)
            multiple = paid ? 1 : 0;
        else
            multiple = (float)tradeRouteManager.currentStop / tradeRouteManager.cityStops.Count;

        paid = false;

		if (!StartingCityCheck())
			return;
		
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
				InfoResourcePopUpHandler.CreateResourceStat(cityLoc, amount, ResourceHolder.Instance.GetIcon(totalRouteCosts[i].resourceType), world);
			}
		}
	}

    public void UnloadAll(City city)
    {
        personalResourceManager.UnloadAll(city);
    }

    private List<ResourceValue> RouteCostCalculator(float multiple/*, bool types*/)
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
        //        if (types)
    				//routeCostTypes.Add(type);
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
        //        if (types)
    				//routeCostTypes.Add(value.resourceType);
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
		InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(transform.position, "Guard returned to nearest city due to inactivity");
	}

    public void LookSad()
    {
		SetInterruptedAnimation(true);
    }

	//public void TeleportToNearestRoad(Vector3Int loc)
	//{
	//	foreach (Vector3Int neighbor in world.GetNeighborsFor(loc, MapWorld.State.EIGHTWAY))
	//	{
	//		if (world.IsRoadOnTileLocation(neighbor))
	//			return;
	//	}

	//	Vector3Int newSpot = loc;
	//	Vector3Int terrainLoc = world.GetClosestTerrainLoc(loc);

	//	foreach (Vector3Int neighbor in world.GetNeighborsFor(terrainLoc, MapWorld.State.EIGHTWAYINCREMENT))
	//	{
	//		if (world.CheckIfPositionIsValid(neighbor))
	//		{
	//			newSpot = neighbor;
	//			if (world.IsRoadOnTerrain(neighbor))
	//			{
	//				Teleport(neighbor);
	//				return;
	//			}
	//		}
	//	}

	//	Teleport(newSpot);
	//}

	public void CheckWarning()
	{
		if (waitingOnRouteCosts)
		{
			world.unitMovement.infoManager.infoPanel.ShowWarning(false, true, false);
		}
		else if (atStop)
		{
			if (tradeRouteManager.resourceAssignments[tradeRouteManager.currentStop][tradeRouteManager.currentResource].resourceAmount < 0)
				world.unitMovement.infoManager.infoPanel.ShowWarning(true, false, false);
			else if (tradeRouteManager.IsTC() && tradeRouteManager.resourceAssignments[tradeRouteManager.currentStop][tradeRouteManager.currentResource].resourceAmount > 0)
				world.unitMovement.infoManager.infoPanel.ShowWarning(false, false, true);
			else
				world.unitMovement.infoManager.infoPanel.ShowWarning(false, false, false);
		}
	}

	public void SetWarning(bool inventory, bool costs, bool gold)
	{
		exclamationPoint.SetActive(true);

		if (isSelected)
			world.unitMovement.infoManager.infoPanel.ShowWarning(inventory, costs, false);
	}

	public void RemoveWarning()
	{
		exclamationPoint.SetActive(false);

		if (isSelected)
			world.unitMovement.infoManager.infoPanel.HideWarning();
	}

	private bool GetInLineCheck()
	{
		if (world.IsTraderWaitingForSameStop(currentLocation, tradeRouteManager.currentDestination, this))
		{
			GoToBackOfLine(tradeRouteManager.currentDestination, currentLocation);
			GetInLine();
			prevTile = currentLocation;
			return true;
		}
		else
		{
			return false;
		}
	}

	public void TradersHereCheck()
	{
		if (atHome && !isMoving)
		{
			if (world.IsCityOnTile(homeCity) && world.GetCity(homeCity).singleBuildDict.ContainsKey(buildDataSO.singleBuildType))
				world.GetCityDevelopment(world.GetCity(homeCity).singleBuildDict[buildDataSO.singleBuildType]).RemoveTraderFromImprovement(this);
		}
	}

	//public Vector3Int GetCurrentDestination()
	//{
	//	return currentDest;
	//	//return tradeRouteManager.cityStops[tradeRouteManager.currentStop];
	//}

	//public bool BeginNextStepCheck(Vector3Int currentLoc)
	//{
	//	if (currentLoc != tradeRouteManager.cityStops[tradeRouteManager.currentStop])
	//	{
	//		if (!GetInLineCheck())
	//			BeginNextStepInRoute();

	//		return true;
	//	}

	//	return false;
	//}

	public void FinishMovementTrader(Vector3 endPosition)
    {
		if (followingRoute)
		{
			//if (atHome)
			//{
			//	if (!world.IsCityOnTile(homeCity))
			//	{
			//		CancelRoute();
			//		return;
			//	}

			//	Vector3Int homeLoc = world.GetCity(homeCity).singleBuildDict[buildDataSO.singleBuildType];
			//	bool homeCityArrival = world.IsSingleBuildStopOnTile(currentLocation, buildDataSO.singleBuildType);
			//	//if (bySea)
			//	//{
			//	//	homeLoc = world.GetCity(homeCity).singleBuildDict[SingleBuildType.Harbor];
			//	//	homeCityArrival = world.IsCityHarborOnTile(currentLocation);
			//	//}
			//	//else
			//	//{
			//	//	homeLoc = homeCity;
			//	//	homeCityArrival = currentLocation == homeCity;
			//	//}

			//	if (homeCityArrival)
			//	{
			//		if (guarded && guardUnit.isMoving)
			//		{
			//			world.AddTraderPosition(currentLocation, this);
			//			waitingOnGuard = true;
			//			return;
			//		}
			//		else if (BeginNextStepCheck(homeLoc))
			//		{
			//			return;
			//		}
			//		else
			//		{
			//			atHome = false; //if not waiting for guard and if not going to another stop
			//		}
			//	}
			//}
			
			if (!world.StopExistsCheck(world.RoundToInt(endPosition)))
			{
				InterruptRoute(true);
				return;
			}

			if (GetInLineCheck())
				return;

			TradeRouteCheck(endPosition);
		}
		else
		{
			if (!world.GetCity(homeCity).singleBuildDict.ContainsKey(buildDataSO.singleBuildType))
			{
				atHome = false;
				ReturnHome(0);
				return;
			}

			bool homeCityArrival = world.IsSingleBuildStopOnTile(currentLocation, buildDataSO.singleBuildType);

			if (homeCityArrival)
			{
				if (returning)
					ReturnToStall();
			}
			else if (returning)
			{
				City city = world.GetCity(homeCity);
				CityImprovement improvement = world.GetCityDevelopment(city.singleBuildDict[buildDataSO.singleBuildType]);

				if (improvement.loc != world.GetClosestTerrainLoc(endPosition))
				{
					atHome = false;
					ReturnHome(0);
					return;
				}

				if (world.traderPosDict.ContainsKey(currentLocation) && world.IsSpotAvailable(improvement.loc))
				{
					ReturnToStall();
					return;
				}

				returning = false;

				world.GetCityDevelopment(city.singleBuildDict[buildDataSO.singleBuildType]).AddTraderToImprovement(this);
				//city.tradersHere.Add(this);
				if (city.activeCity && world.unitMovement.upgradingUnit)
					world.unitMovement.CheckIndividualUnitHighlight(this, city);

				//world.AddUnitPosition(currentLocation, this);
				world.AddTraderPosition(currentLocation, this);

				Rotate(world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAY)[Random.Range(0,8)] + currentLocation);

				if (isSelected)
					world.unitMovement.ShowIndividualCityButtonsUI();
			}
		}

		prevTile = currentLocation;
	}

    public void KillTrader()
    {
		if (isSelected)
		{
			world.somethingSelected = false;
			world.unitMovement.ClearSelection();
		}

		world.traderList.Remove(this);
		world.RemoveTraderPosition(currentLocation, this);
		StartCoroutine(WaitKillUnit());

		if (guarded)
		{
			guardUnit.military.guard = false;
			guardUnit.military.guardedTrader = null;
			guardUnit = null;
			guarded = false;
		}

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
				world.mainPlayer.StopRunningAway();
		}
		else
		{
			StopMovementCheck(false);
			TradersHereCheck();

			if (followingRoute)
			{
				if (isWaiting)
				{
					ITradeStop stop = world.GetStop(tradeRouteManager.currentDestination);
					stop.ClearRestInLine(this, stop);
				}
					
				CancelRoute();
			}
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
		data.goldNeeded = goldNeeded;
		data.homeCity = homeCity;
		//data.stallLoc = stallLoc;
		data.atHome = atHome;
		data.returning = returning;
		data.movingUpInLine = movingUpInLine;
		data.linePause = linePause;
		data.posSet = posSet;
		data.atStall = atStall;

		if (isMoving /*&& !isWaiting*/)
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
        data.currentDestination = tradeRouteManager.currentDestination;
        data.resourceCheck = tradeRouteManager.resourceCheck;
        data.waitForever = tradeRouteManager.waitForever;
		data.amountMoved = tradeRouteManager.amountMoved;
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
		isMoving = data.isMoving;
		interruptedRoute = data.interruptedRoute;
		atStop = data.atStop;
		followingRoute = data.followingRoute;
		isWaiting = data.isWaiting;
		if (isWaiting)
			originalMoveSpeed = inLineSpeed;
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
		homeCity = data.homeCity;
		//stallLoc = data.stallLoc;
		atHome = data.atHome;
		returning = data.returning;
		goldNeeded = data.goldNeeded;
		movingUpInLine = data.movingUpInLine;
		linePause = data.linePause;
		posSet = data.posSet;
		atStall = data.atStall;

		if (posSet)
			world.AddUnitPosition(currentLocation, this);

		if (atHome && !isMoving)
			world.GetCityDevelopment(world.GetCity(homeCity).singleBuildDict[buildDataSO.singleBuildType]).AddTraderToImprovement(this);

		if (guarded)
            world.CreateGuard(data.guardUnit, this);

		if (isUpgrading)
			GameLoader.Instance.unitUpgradeList.Add(this);

		if (currentHealth < healthMax)
		{
			healthbar.LoadHealthLevel(currentHealth);
			healthbar.gameObject.SetActive(true);
		}

		if (isMoving)
		{
			if (bySea)
				ripples.SetActive(true);
		}
		//else if (!followingRoute)
		//{
		//	world.AddUnitPosition(currentLocation, this);
		//}

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
		tradeRouteManager.currentDestination = data.currentDestination;
		tradeRouteManager.resourceCheck = data.resourceCheck;
		tradeRouteManager.waitForever = data.waitForever;
		tradeRouteManager.amountMoved = data.amountMoved;
		//tradeRouteManager.percDone = data.percDone;

		totalRouteLength = tradeRouteManager.CalculateRoutePaths(world);

		if (movingUpInLine)
		{
			if (isMoving)
			{
				if (data.moveOrders.Count == 0)
				{
					Vector3Int endPosition = world.RoundToInt(finalDestinationLoc);
					data.moveOrders.Add(endPosition);
				}

				GameLoader.Instance.unitMoveOrders[this] = data.moveOrders;
			}
			else
			{
				pathPositions = new Queue<Vector3Int>(data.moveOrders);
				StartMoveUpInLine(linePause);
			}
		}
		else if (isMoving)
		{
			if (data.moveOrders.Count == 0)
			{
				Vector3Int endPosition = world.RoundToInt(finalDestinationLoc);
				data.moveOrders.Add(endPosition);
			}

            //if (!isWaiting)
			GameLoader.Instance.unitMoveOrders[this] = data.moveOrders;
			//MoveThroughPath(data.moveOrders);
            //else
            //    pathPositions = new Queue<Vector3Int>(data.moveOrders);
		}
		else if (isWaiting || ambush)
		{
			pathPositions = new Queue<Vector3Int>(data.moveOrders);
		}

        if (followingRoute)
        {
            CalculateRouteCosts();
            tradeRouteManager.waitTime = tradeRouteManager.waitTimes[tradeRouteManager.currentStop];

			if (waitingOnRouteCosts)
			{
				exclamationPoint.SetActive(true);
			}
            else if (atStop)
            {
				tradeRouteManager.SetStop(world.GetStop(currentLocation));
				WaitTimeCo = StartCoroutine(tradeRouteManager.WaitTimeCoroutine());
				LoadUnloadCo = StartCoroutine(tradeRouteManager.LoadUnloadCoroutine(loadUnloadRate, true));
			}
		}
	}
}
