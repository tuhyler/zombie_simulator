using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.UI.CanvasScaler;

public class Army : MonoBehaviour
{
    private MapWorld world;

    [HideInInspector]
    public Vector3Int loc, forward, attackZone;

    [HideInInspector]
    public EnemyCamp targetCamp;
    [HideInInspector]
    public int armyCount, cyclesGone, infantryCount, rangedCount, cavalryCount, seigeCount, strength, health;
    [HideInInspector]
    public Vector3Int enemyTarget, enemyCityLoc;
    [HideInInspector]
    public List<Vector3Int> totalSpots = new(), openSpots = new(), pathToTarget = new(), pathTraveled = new();
    [HideInInspector]
    public List<Vector3Int> attackingSpots = new(), movementRange = new(), cavalryRange = new();
    
    private List<Military> unitsInArmy = new(), deadList = new();
    public List<Military> UnitsInArmy { get { return unitsInArmy; } }
    public List<Military> DeadList { get { return deadList; } set { deadList = value; } }
    [HideInInspector]
    public int unitsReady, stepCount, noMoneyCycles;

    //army maintenance and battle costs
    private Dictionary<ResourceType, int> armyCycleCostDict = new();
    private Dictionary<ResourceType, int> armyBattleCostDict = new();
    private List<ResourceValue> totalBattleCosts = new();

    [HideInInspector]
    public City city;
	private Queue<GameObject> pathQueue = new();


	[HideInInspector]
    public bool isEmpty = true, isFull, isTraining, isTransferring, isRepositioning, traveling, inBattle, returning, atHome, selected, enemyReady, issueRefund = true, defending, atSea, battleAtSea, seaTravel;

	private void Awake()
	{
        atHome = true;
	}

    public void SetLoc(Vector3Int loc, City city)
    {
        this.loc = loc;
        this.city = city;
    }

    public void SetWorld(MapWorld world)
    {
        this.world = world;
    }

    public List<UnitData> SendData()
    {
        List<UnitData> armyData = new();

        for (int i = 0; i < unitsInArmy.Count; i++)
        {
            if (!unitsInArmy[i].isDead) //only save living units
                armyData.Add(unitsInArmy[i].SaveMilitaryUnitData());
        }

        return armyData;
    }

    public bool CheckIfInBase(Vector3Int loc)
    {
        return totalSpots.Contains(loc);
    }

	public Vector3Int GetAvailablePosition(UnitType type)
    {
        Vector3Int openSpot = openSpots[0];
        
        switch (type)
        {
            case UnitType.Ranged:
				Vector3Int[] backSpots = new Vector3Int[6] { new Vector3Int(0, 0, 1), new Vector3Int(-1, 0, 1), new Vector3Int(1, 0, 1), new Vector3Int(0, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 0, 1) };

				for (int i = 0; i < backSpots.Length; i++)
				{
					Vector3Int trySpot = backSpots[i] + loc;

					if (openSpots.Contains(trySpot))
					{
						openSpot = trySpot;
						break;
					}
				}

				break;
            case UnitType.Cavalry:
                Vector3Int[] sideSpots = new Vector3Int[4] { new Vector3Int(-1, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 1), new Vector3Int(1, 0, 1) };

                for (int i = 0; i < sideSpots.Length; i++)
                {
                    Vector3Int trySpot = sideSpots[i] + loc;
                    
                    if (openSpots.Contains(trySpot))
                    {
                        openSpot = trySpot;
                        break;
                    }
                }

                break;
        }
        
        openSpots.Remove(openSpot);

        if (openSpots.Count == 0)
            isFull = true;
        if (isEmpty)
            isEmpty = false;

        return openSpot;
    }

    public void AddToOpenSpots(Vector3Int spot)
    {
        openSpots.Remove(spot);

		if (openSpots.Count == 0)
			isFull = true;
		if (isEmpty)
			isEmpty = false;
	}

    public void UpdateLocation(Vector3Int oldLoc, Vector3Int newLoc)
    {
		int index = totalSpots.IndexOf(oldLoc);
		int newIndex = 0;

		for (int i = 0; i < index; i++)
		{
			if (openSpots.Contains(totalSpots[i]))
				newIndex++;
		}

		openSpots.Insert(newIndex, oldLoc);
		openSpots.Remove(newLoc);
    }

    public void AddToArmy(Military unit)
    {
        unitsInArmy.Add(unit);

        armyCount++;
        UnitType type = unit.buildDataSO.unitType;

        if (type == UnitType.Infantry)
            infantryCount++;
        else if (type == UnitType.Ranged)
            rangedCount++;
        else if (type == UnitType.Cavalry)
            cavalryCount++;
        else if (type == UnitType.Seige)
            seigeCount++;

		strength += unit.buildDataSO.baseAttackStrength;
		health += unit.buildDataSO.health;

		AddToCycleCost(unit.buildDataSO.cycleCost);
        AddToBattleCost(unit.buildDataSO.battleCost);
	}

	private void AddToCycleCost(List<ResourceValue> costs)
    {
        List<ResourceType> resourceTypes = new();
        
        for (int i = 0; i < costs.Count; i++)
        {
            if (!armyCycleCostDict.ContainsKey(costs[i].resourceType))
                armyCycleCostDict[costs[i].resourceType] = costs[i].resourceAmount;
            else
                armyCycleCostDict[costs[i].resourceType] += costs[i].resourceAmount;

            resourceTypes.Add(costs[i].resourceType);
            city.ResourceManager.ModifyResourceConsumptionPerMinute(costs[i].resourceType, costs[i].resourceAmount);
        }

        if (city.activeCity && city.world.cityBuilderManager.uiLaborHandler.activeStatus)
            city.world.cityBuilderManager.uiLaborHandler.UpdateResourcesConsumed(resourceTypes, city.ResourceManager.resourceConsumedPerMinuteDict);
	}

    private void AddToBattleCost(List<ResourceValue> costs)
    {
		for (int i = 0; i < costs.Count; i++)
		{
			if (!armyBattleCostDict.ContainsKey(costs[i].resourceType))
				armyBattleCostDict[costs[i].resourceType] = costs[i].resourceAmount;
			else
				armyBattleCostDict[costs[i].resourceType] += costs[i].resourceAmount;
		}
	}

	private void RemoveFromCycleCost(List<ResourceValue> costs)
	{
        List<ResourceType> resourceTypes = new();
        
        for (int i = 0; i < costs.Count; i++)
        {
			armyCycleCostDict[costs[i].resourceType] -= costs[i].resourceAmount;

            //remove from dict if empty
            if (armyCycleCostDict[costs[i].resourceType] == 0)
                armyCycleCostDict.Remove(costs[i].resourceType);

            resourceTypes.Add(costs[i].resourceType);
            city.ResourceManager.ModifyResourceConsumptionPerMinute(costs[i].resourceType, -costs[i].resourceAmount);
        }

        if (city.activeCity && city.world.cityBuilderManager.uiLaborHandler.activeStatus)
            city.world.cityBuilderManager.uiLaborHandler.UpdateResourcesConsumed(resourceTypes, city.ResourceManager.resourceConsumedPerMinuteDict);
	}

    private void RemoveFromBattleCost(List<ResourceValue> costs)
    {
		for (int i = 0; i < costs.Count; i++)
        {
			armyBattleCostDict[costs[i].resourceType] -= costs[i].resourceAmount;

			if (armyBattleCostDict[costs[i].resourceType] == 0)
				armyBattleCostDict.Remove(costs[i].resourceType);
		}
	}


	public List<ResourceValue> GetArmyCycleCost()
    {
        List<ResourceValue> costs = new();

        foreach (ResourceType type in armyCycleCostDict.Keys)
        {
            ResourceValue value;
            value.resourceType = type;
            value.resourceAmount = armyCycleCostDict[type];
            costs.Add(value);
        }

        return costs;
    }

    public void RemoveFromArmy(Military unit, Vector3Int loc)
    {
        unitsInArmy.Remove(unit);
        //if (unit.newlyJoined)
        //{
        //    RemoveFromStagingCost(unit.buildDataSO.cycleCost);
        //    stagingUnit.Remove(unit);
        //}
        //else
        //{
		RemoveFromCycleCost(unit.buildDataSO.cycleCost);
		RemoveFromBattleCost(unit.buildDataSO.battleCost);
		//}
		armyCount--;
		UnitType type = unit.buildDataSO.unitType;

		if (type == UnitType.Infantry)
			infantryCount--;
		else if (type == UnitType.Ranged)
			rangedCount--;
		else if (type == UnitType.Cavalry)
			cavalryCount--;
		else if (type == UnitType.Seige)
			seigeCount--;

        strength -= unit.buildDataSO.baseAttackStrength;
        health -= unit.buildDataSO.health;

		if (armyCount == 0)
        {
            if (world.uiCampTooltip.EnemyScreenActive())
            {
                world.unitMovement.CancelArmyDeployment();
            }
            else
            {
				if (world.uiCampTooltip.activeStatus)
					world.uiCampTooltip.RefreshData();
			}

			isEmpty = true;
        }
        else
        {
			if (world.uiCampTooltip.activeStatus)
				world.uiCampTooltip.RefreshData();
		}

		if (isFull)
            isFull = false;

        int index = totalSpots.IndexOf(loc);
        int newIndex = 0;

        for (int i = 0; i < index; i++)
        {
            if (openSpots.Contains(totalSpots[i]))
                newIndex++;
        }

        openSpots.Insert(newIndex,loc);

		if (city.currentPop == 0 && armyCount == 0)
			city.StopGrowthCycle();
	}

    //preparing positions lists
    public void SetArmySpots(Vector3Int tile)
    {
        totalSpots.Add(tile);
        openSpots.Add(tile);
    }

    public void ClearArmySpots()
    {
        totalSpots.Clear();
        openSpots.Clear();
    }

    //realigning units to battle positions before moving out
    public void RealignUnits(MapWorld world, Vector3Int targetZone, Vector3Int attackZone, Vector3Int travelLoc)
    {
        Vector3Int diff = (targetZone - attackZone) / 3;
        int rotation;

        if (diff.x == -1)
            rotation = 1;
        else if (diff.z == 1)
            rotation = 2;
        else if (diff.x == 1)
            rotation = 3;
        else
            rotation = 0;

        foreach (Military unit in unitsInArmy)
        {            
            unit.healthbar.CancelRegeneration();
            unit.atHome = false;
         
            if (unit.isSelected)
			{
                world.unitMovement.ShowIndividualCityButtonsUI();
                
                if (world.unitMovement.deployingArmy || world.unitMovement.changingCity || world.unitMovement.assigningGuard)
                    world.unitMovement.CancelArmyDeployment();
                else if (world.unitMovement.swappingArmy)
                    world.unitMovement.CancelReposition();
            }
            
            Vector3Int unitDiff = unit.currentLocation - this.loc;

            if (rotation == 1)
            {
                if (unitDiff.sqrMagnitude == 2)
                {
                    int comb = Mathf.Abs(unitDiff.x + unitDiff.z);
                    if (comb == 0)
                        unitDiff.x *= -1;
                    if (comb == 2)
                        unitDiff.z *= -1;
                }
                else
                {
                    if (unitDiff.z != 0)
                        unitDiff += unitDiff.z * new Vector3Int(1, 0, -1);
                    else
                        unitDiff += unitDiff.x * new Vector3Int(-1, 0, -1);
				}
            }
            else if (rotation == 2)
            {
                unitDiff *= -1;
            }
            else if (rotation == 3)
            {
				if (unitDiff.sqrMagnitude == 2)
				{
					int comb = Mathf.Abs(unitDiff.x + unitDiff.z);
					if (comb == 0)
						unitDiff.z *= -1;
					if (comb == 2)
						unitDiff.x *= -1;
				}
				else
				{
					if (unitDiff.x != 0)
						unitDiff += unitDiff.x * new Vector3Int(-1, 0, 1);
					else
						unitDiff += unitDiff.z * new Vector3Int(-1, 0, -1);
				}
			}

            List<Vector3Int> path = GridSearch.AStarSearch(world, unit.currentLocation, travelLoc + unitDiff, false, false);
            unit.marchPosition = unitDiff;

            if (defending && path.Count == 0)
                path = GridSearch.MoveWherever(world, unit.currentLocation, travelLoc + unitDiff);

			if (path.Count > 0)
            {
                unit.preparingToMoveOut = true;
                unit.finalDestinationLoc = travelLoc + unitDiff;
    			unit.MoveThroughPath(path);
            }
            else
            {
                UnitReady(unit);
            }
        }
    }

    public bool DeployArmyCheck(Vector3Int current, Vector3Int target)
    {
		List<Vector3Int> exemptList = new() { target };
        enemyTarget = target;
        seaTravel = false;

		foreach (Vector3Int tile in world.GetNeighborsFor(target, MapWorld.State.EIGHTWAYINCREMENT))
			exemptList.Add(tile);

        bool getToHarbor = true;
        List<Vector3Int> waterPath = new();
		List<Vector3Int> landPath = GridSearch.TerrainSearch(world, current, target, exemptList);

        //seeing if cheaper to go by sea
        if (city.hasHarbor)
        {
			List<Vector3Int> pathToHarbor = GridSearch.TerrainSearch(world, loc, city.harborLocation, exemptList);

            if (pathToHarbor.Count == 0)
            {
                getToHarbor = false;
            }
            else
            {
				List<Vector3Int> directSeaList = new(), outerRingList = new();
				//Checking if target is by sea
				List<Vector3Int> surroundingArea = world.GetNeighborsFor(target, MapWorld.State.CITYRADIUS);
				for (int i = 0; i < surroundingArea.Count; i++)
				{
					if (world.GetTerrainDataAt(surroundingArea[i]).isLand)
						continue;

					if (i < 8)
						directSeaList.Add(surroundingArea[i]);
					else
						outerRingList.Add(surroundingArea[i]);
				}

				//finding shortest route to target
				bool hasRoute = false;
				List<Vector3Int> chosenPath = new();

				//first inner ring
				if (directSeaList.Count > 0)
				{
					chosenPath = world.GetSeaLandRoute(directSeaList, city.harborLocation, target, exemptList, false);

					if (chosenPath.Count > 0)
						hasRoute = true;
				}

				//outer ring next
				if (!hasRoute && outerRingList.Count > 0)
				{
					chosenPath = world.GetSeaLandRoute(outerRingList, city.harborLocation, target, exemptList, false);

					if (chosenPath.Count > 0)
						hasRoute = true;
				}


				if (hasRoute)
				{
                    seaTravel = true;
                    waterPath = pathToHarbor;
                    waterPath.AddRange(chosenPath);
				}
			}
        }

        if (landPath.Count == 0 || (waterPath.Count > 0 && waterPath.Count <= landPath.Count))
            pathToTarget = waterPath;
        else
            pathToTarget = landPath;

        if (pathToTarget.Count == 0)
        {
			if (getToHarbor)
                InfoPopUpHandler.WarningMessage().Create(target, "Cannot reach selected area");
            else
				InfoPopUpHandler.WarningMessage().Create(target, "Cannot reach own harbor");

			return false;
        }
        else
        {
            //finding best spot to attack from
            pathToTarget = world.FindOptimalAttackZone(pathToTarget, target, exemptList);
            return true;
        }
	}

    public bool DeployArmyMovingTargetCheck(Vector3Int current, Vector3Int cityLoc, List<Vector3Int> pathList, Vector3Int currentSpot)
    {
		List<Vector3Int> exemptList = new() { cityLoc };
        Queue<Vector3Int> path = new(pathList);

        int currentIndex = pathList.IndexOf(currentSpot);
        for (int i = 0; i < currentIndex; i++)
            path.Dequeue();

		foreach (Vector3Int tile in world.GetNeighborsFor(cityLoc, MapWorld.State.EIGHTWAYINCREMENT))
			exemptList.Add(tile);

		pathToTarget = GridSearch.TerrainSearchMovingTarget(world, current, path, exemptList, false);

		if (pathToTarget.Count == 0)
        {
			return false;
        }
        else
        {
            enemyTarget = pathToTarget[pathToTarget.Count - 1];
			return true;
        }
    }

    public bool UpdateArmyCostsMovingTarget(Vector3Int current, Vector3Int cityLoc, List<Vector3Int> pathList, Vector3Int currentSpot)
    {
		List<Vector3Int> exemptList = new() { cityLoc };
		Queue<Vector3Int> path = new(pathList);

        int currentIndex = pathList.IndexOf(currentSpot);
		for (int i = 0; i < currentIndex; i++)
			path.Dequeue();

		foreach (Vector3Int tile in world.GetNeighborsFor(cityLoc, MapWorld.State.EIGHTWAYINCREMENT))
			exemptList.Add(tile);

        HidePath();
		pathToTarget = GridSearch.TerrainSearchMovingTarget(world, current, path, exemptList, true, enemyTarget);

		if (pathToTarget.Count == 0)
        {
            return false;
        }
        else
        {
            ShowBattlePath();
            enemyTarget = pathToTarget[pathToTarget.Count - 1];
            return true;
		}
	}

    public void DeployArmy()
    {
        ConsumeBattleCosts();
        unitsReady = 0;

    	pathToTarget.Remove(pathToTarget[pathToTarget.Count - 1]);
	
        atHome = false;
		traveling = true;
		Vector3Int penultimate = pathToTarget[pathToTarget.Count - 1];
		attackZone = penultimate;
		forward = (enemyTarget - attackZone) / 3;

		if (returning)
			DeployArmy(true);
		else
			RealignUnits(world, enemyTarget, penultimate, loc);
	}

    public void ShowBattlePath()
    {
        ShowBattlePath(pathToTarget, loc);
    }

    public void MoveArmyHome(Vector3Int target)
    {
        pathTraveled.Reverse();
        stepCount = 0;
		pathToTarget = pathTraveled;

        if (pathToTarget.Count == 0)
            pathToTarget.Add(target);

        unitsReady = 0;
        traveling = false;
        returning = true;
        DestroyDeadList();
        if (world.IsEnemyCampHere(enemyTarget))
            targetCamp = null;
        DeployArmy(false);
    }

	public void ReturnToBarracks()
	{
		unitsReady = 0;

		foreach (Military unit in unitsInArmy)
		{
			unit.inBattle = false; //leaving it here just in case
			unit.preparingToMoveOut = false;
			unit.isMarching = false;
			unit.StartReturn();
		}
	}

	public bool DeployBattleScreenCheck()
    {
        return world.uiCampTooltip.EnemyScreenActive() && this == world.uiCampTooltip.army;
    }

    public List<ResourceValue> CalculateBattleCost(int enemyStrength)
    {
        totalBattleCosts.Clear();
        int cycles = Mathf.CeilToInt(pathToTarget.Count * 2 * 4f / city.secondsTillGrowthCheck); //*2 for there and back, 2.5 as seconds per tile rate

        foreach (ResourceType type in armyCycleCostDict.Keys)
        {
            ResourceValue value;
            value.resourceType = type;
            value.resourceAmount = armyCycleCostDict[type] * cycles;
            totalBattleCosts.Add(value);
        }

        foreach (ResourceType type in armyBattleCostDict.Keys)
        {
			ResourceValue value;
			value.resourceType = type;

            float perc = Mathf.Min(enemyStrength / strength,1);

			value.resourceAmount = Mathf.CeilToInt(armyBattleCostDict[type] * perc);
			totalBattleCosts.Add(value);
		}

		return totalBattleCosts;
    }

    public List<ResourceValue> GetBattleCost()
    {
        return totalBattleCosts;
    }

    private void ConsumeBattleCosts()
    {
        city.ResourceManager.ConsumeResources(totalBattleCosts, 1, city.barracksLocation, true);
        //totalBattleCosts.Clear();
    }

    public void ClearBattleCosts()
    {
        totalBattleCosts.Clear();
    }

    private void IssueBattleRefund()
    {
        if (issueRefund)
        {
            int i = 0;
            city.ResourceManager.resourceCount = 0;
			foreach (ResourceValue value in totalBattleCosts)
			{
                int amount;

                if (armyCycleCostDict.ContainsKey(value.resourceType))
                    amount = value.resourceAmount - cyclesGone * armyCycleCostDict[value.resourceType];
                else
                    amount = armyBattleCostDict[value.resourceType];

                if (amount > 0)
                {
                    city.ResourceManager.AddResource(value.resourceType, amount);
					Vector3 cityLoc = city.barracksLocation;
					cityLoc.y += totalBattleCosts.Count * 0.4f;
					cityLoc.y += -0.4f * i;
					InfoResourcePopUpHandler.CreateResourceStat(cityLoc, amount, ResourceHolder.Instance.GetIcon(value.resourceType));
					i++;
				}
            }
        }
        else
        {
            issueRefund = true;
        }

        totalBattleCosts.Clear();
    }

    public void UnitReady(Unit unit)
    {
		unit.Rotate(unit.currentLocation + forward);
		unitsReady++;

        if (unitsReady == armyCount)
        {
            unitsReady = 0;

			if (defending)
            {
                targetCamp.armyReady = true;

				if (targetCamp.attackReady)
				{
					targetCamp.attackReady = false;
					targetCamp.armyReady = false;

					world.uiAttackWarning.AttackNotification(((Vector3)attackZone + enemyTarget) * 0.5f);
					Charge();
				}
            }
            else
            {
                DeployArmy(true);
            }
        }
    }

    public void UnitArrived(Vector3Int loc)
    {
        unitsReady++;

        if (unitsReady == armyCount)
        {
            unitsReady = 0;
            stepCount = 0;
            
            if (this.loc == loc)
            {
                if (openSpots.Count == 0)
                    isFull = true;

                IssueBattleRefund();
                atHome = true;
                cyclesGone = 0;
                returning = false;
                
                pathTraveled.Clear();

                if (selected)
                    world.unitMovement.ShowIndividualCityButtonsUI();
                
                if (world.cityBuilderManager.uiUnitBuilder.activeStatus)
                    world.cityBuilderManager.uiUnitBuilder.UpdateBarracksStatus(isFull);

                if (world.IsEnemyCityOnTile(city.waitingAttackLoc))
                {
                    world.GetEnemyCity(city.waitingAttackLoc).SendAttack();
                    city.waitingAttackLoc = city.cityLoc;
                }
            }
            else
            {
                targetCamp.armyReady = true;
                unitsInArmy[0].RevealCheck(enemyTarget, true);

                if (selected)
                    HidePath();

                if (targetCamp.attackReady)
                {
					targetCamp.attackReady = false;
                    targetCamp.armyReady = false;

					if (targetCamp.UnitsInCamp.Count == 0) //for invading empty city (not needed right now)
                    {
						issueRefund = false;

						for (int i = 0; i < unitsInArmy.Count; i++)
                        {
                            Vector3Int cityLoc = unitsInArmy[i].marchPosition + enemyTarget;
                            unitsInArmy[i].inBattle = true;
                            unitsInArmy[i].finalDestinationLoc = cityLoc;
                            List<Vector3Int> path = new() { cityLoc };
                            unitsInArmy[i].MoveThroughPath(path);
                        }
                    }
                    else
                    {
                        world.uiAttackWarning.AttackNotification(((Vector3)attackZone + enemyTarget)*0.5f);
                        Charge();
                    }
                }
            }
        }
    }

    public void UnitNextStep(bool close)
    {
        unitsReady++;

        if (unitsReady == armyCount)
        {
            unitsReady = 0;

            if (stepCount == pathToTarget.Count) //when returned home
                stepCount--;
            Vector3Int stepFinished = pathToTarget[stepCount];
            
            Vector3Int nextStep;
            if (stepCount + 1 == pathToTarget.Count)
                nextStep = enemyTarget;
            else
                nextStep = pathToTarget[stepCount + 1];

			if (seaTravel)
            {
                if (atSea)
                {
                    if (world.GetTerrainDataAt(nextStep).isLand)
                    {
                        atSea = false;
                        for (int i = 0; i < unitsInArmy.Count; i++)
						    unitsInArmy[i].ToggleBoat(false);
				    }
			    }
                else
                {
                    if (!world.GetTerrainDataAt(stepFinished).isLand && !world.GetTerrainDataAt(nextStep).isLand)
                    {
                        atSea = true;
                        for (int i = 0; i < unitsInArmy.Count; i++)
                            unitsInArmy[i].ToggleBoat(true);
                    }
                }
            }

            if (traveling)
            {
                if (close)
                {
                    if (world.IsEnemyCityOnTile(targetCamp.cityLoc))
                        world.EnemyBattleStations(targetCamp.cityLoc, attackZone);
                    else
                        world.EnemyBattleStations(enemyTarget, attackZone);
                }

				pathTraveled.Add(stepFinished);
            }

            stepCount++;
            BeginNextStep();
        }
    }

    public void BeginNextStep()
    {
        foreach (Military unit in unitsInArmy)
            unit.readyToMarch = true;
    }

    private void DeployArmy(bool deploying)
    {
        foreach (Military unit in unitsInArmy)
		{
            if (unit.attackCo != null)
                StopCoroutine(unit.attackCo);

            unit.preparingToMoveOut = false;
            unit.attacking = false;
            unit.attackCo = null;
			List<Vector3Int> path = new();

			foreach (Vector3Int tile in pathToTarget)
				path.Add(tile + unit.marchPosition);

            if (!deploying)
                path.Add(unit.barracksBunk);

			if (unit.isMoving)
			{
				unit.StopAnimation();
				unit.ShiftMovement();
			}

            unit.isMarching = true;
			unit.finalDestinationLoc = path[path.Count - 1];
			unit.MoveThroughPath(path);
		}
	}

    public void Charge()
    {
        issueRefund = false;
        traveling = false;
		movementRange.Clear();
        attackingSpots.Clear();
        movementRange.Add(attackZone);
        cavalryRange.Add(attackZone);
        movementRange.Add(enemyTarget);
        cavalryRange.Add(enemyTarget);

        //setting battle icon at battle loc
        city.battleIcon.SetActive(true);
        city.battleIcon.transform.position = new Vector3((attackZone.x + enemyTarget.x) * 0.5f, 4, (attackZone.z + enemyTarget.z) * 0.5f) ;
        Vector3 goScale = city.battleIcon.transform.localScale;
        city.battleIcon.transform.localScale = Vector3.zero;
        LeanTween.scale(city.battleIcon, goScale, 0.5f);

		world.ToggleCityMaterialClear(targetCamp.isCity ? targetCamp.cityLoc : targetCamp.loc, city.cityLoc, enemyTarget, attackZone, true);
        if (!world.GetTerrainDataAt(attackZone).isLand)
        {
            battleAtSea = true;
            targetCamp.battleAtSea = true;
        }

		int i = 0;
        foreach (Vector3Int tile in world.GetNeighborsFor(attackZone, MapWorld.State.EIGHTWAYTWODEEP))
        {
            if (i < 8)
                movementRange.Add(tile);

            cavalryRange.Add(tile);
            i++;
        }

        i = 0;
        foreach (Vector3Int tile in world.GetNeighborsFor(enemyTarget, MapWorld.State.EIGHTWAYTWODEEP))
        {
            if (i < 8)
                movementRange.Add(tile);

            if (!cavalryRange.Contains(tile))
                cavalryRange.Add(tile);
            i++;
        }

		if (!inBattle)
			ArmyCharge();

		if (!targetCamp.inBattle)
			targetCamp.Charge();
	}

    private void ArmyCharge()
    {
        inBattle = true;
        
        foreach (Military unit in unitsInArmy)
        {
            unit.strengthBonus = Mathf.RoundToInt(world.GetTerrainDataAt(unit.currentLocation).terrainData.terrainAttackBonus * 0.01f * unit.attackStrength);
			if (unit.isSelected)
				world.unitMovement.infoManager.UpdateStrengthBonus(unit.strengthBonus);

			unit.inBattle = true;
            UnitType type = unit.buildDataSO.unitType;

            if (type == UnitType.Infantry)
                unit.InfantryAggroCheck();
            else if (type == UnitType.Ranged)
                unit.RangedAggroCheck();
            else if (type == UnitType.Cavalry)
                unit.CavalryAggroCheck();
        }
    }

    public Unit FindClosestTarget(Unit unit)
    {        
        Unit closestEnemy = null;
        float dist = 0;
        List<Military> tempList = new(targetCamp.UnitsInCamp);
        bool firstOne = true;

        //find closest target
        for (int i = 0; i < tempList.Count; i++)
        {
            Military enemy = tempList[i];

            if (enemy.isDead)
                continue;

            if (firstOne)
            {
                firstOne = false;
                closestEnemy = enemy;
                dist = Math.Abs(enemy.transform.position.x - unit.transform.position.x) + Math.Abs(enemy.transform.position.z - unit.transform.position.z); //not using sqrmagnitude in case of hill
                continue;
            }

            float nextDist = Math.Abs(enemy.transform.position.x - unit.transform.position.x) + Math.Abs(enemy.transform.position.z - unit.transform.position.z);

			if (nextDist < dist)
            {
                closestEnemy = enemy;
                dist = nextDist;
            }
        }

        return closestEnemy;
    }

    //return closest target if no ranged
	public Unit FindEdgeRanged(Vector3Int currentLoc)
	{
        Vector3Int battleDiff = (enemyTarget - attackZone) / 3;
        List<Vector3Int> tilesToCheck = new();

        if (battleDiff.z != 0)
        {
            int currentOffset = Math.Sign(currentLoc.x - attackZone.x);

            //ignore if in middle
            if (currentOffset == 0)
                return null;

			tilesToCheck.Add(new Vector3Int(1 * currentOffset, 0, 0) + enemyTarget);
			tilesToCheck.Add(enemyTarget);
			tilesToCheck.Add(new Vector3Int(1 * currentOffset, 0, 1 * battleDiff.z) + enemyTarget);
            tilesToCheck.Add(new Vector3Int(0, 0, 1 * battleDiff.z) + enemyTarget);
		}
        else
        {
            int currentOffset = Math.Sign(currentLoc.z - attackZone.z);

            if (currentOffset == 0)
                return null;

			tilesToCheck.Add(new Vector3Int(0, 0, 1 * currentOffset) + enemyTarget);
			tilesToCheck.Add(enemyTarget);
			tilesToCheck.Add(new Vector3Int(1 * battleDiff.x, 0, 1 * currentOffset) + enemyTarget);
			tilesToCheck.Add(new Vector3Int(1 * battleDiff.x, 0, 0) + enemyTarget);
		}

        int i = 0;
        bool skipMiddle = false;
        foreach (Vector3Int tile in tilesToCheck)
        {
            if (skipMiddle)
            {
				skipMiddle = false;
                continue;
            }
            
            if (world.IsUnitLocationTaken(tile))
            {
                Unit potential = world.GetUnit(tile);

                if (potential.buildDataSO.unitType == UnitType.Ranged)
                    return potential;

                if (i % 2 == 0)
                    skipMiddle = true;
            }
    
            i++;
		}

		return null;
	}

	public List<Vector3Int> PathToEnemy(Vector3Int pos, Vector3Int target)
	{
        return GridSearch.BattleMove(world, pos, target, movementRange, attackingSpots, battleAtSea);
	}

    public List<Vector3Int> CavalryPathToEnemy(Vector3Int pos, Vector3Int target)
    {
		return GridSearch.BattleMove(world, pos, target, cavalryRange, attackingSpots, battleAtSea);
	}

    private void ClearTraveledPath()
    {
        pathTraveled.Clear();
    }

	public bool FinishAttack()
	{
        if (returning)
            return true;
        
        if (targetCamp.deathCount == targetCamp.campCount)
		{
            city.attacked = false;
			world.ToggleCityMaterialClear(targetCamp.isCity ? targetCamp.cityLoc : targetCamp.loc, city.cityLoc, enemyTarget, attackZone, false);
            targetCamp.battleAtSea = false;

            if (world.mainPlayer.runningAway)
			{
				world.mainPlayer.StopRunningAway();
				world.mainPlayer.stepAside = false;
			}

			if (targetCamp.movingOut)
            {
				DestroyEnemyDeadList();
                targetCamp.ResetCamp();
                targetCamp.moveToLoc = targetCamp.loc;
                targetCamp.movingOut = false;
                targetCamp.inBattle = false;
                targetCamp.enemyReady = 0;
				targetCamp.deathCount = 0;
				targetCamp.attackingArmy = null;
                targetCamp.SetCityEnemyCamp();
                world.GetEnemyCity(targetCamp.cityLoc).StartSpawnCycle(true);
            }

            returning = true;
            DestroyDeadList();

            foreach (Military unit in unitsInArmy)
                unit.StopAttacking();

            attackingSpots.Clear();
            city.battleIcon.SetActive(false);
            inBattle = false;
            
            if (defending)
            {
                defending = false;
                ReturnToBarracks();
            }
            else
            {
                world.unitMovement.uiCancelTask.ToggleVisibility(false);
                //world.unitMovement.uiDeployArmy.ToggleTweenVisibility(true);
                MoveArmyHome(loc);
            }

            battleAtSea = false;
            return true;
        }
        else
        {
            return false;
        }
    }

	public void Retreat()
    {
        foreach (Military unit in unitsInArmy)
            unit.StopAttacking();

        StartCoroutine(targetCamp.RetreatTimer());
		world.unitMovement.uiCancelTask.ToggleVisibility(false);
		city.battleIcon.SetActive(false);
		inBattle = false;
        returning = true;
        attackingSpots.Clear();
        DestroyDeadList();
        MoveArmyHome(loc);

		//targetCamp.ResetStatus();
		//targetCamp.ReturnToCamp();
	}

    public bool IsGone()
    {
        if (traveling || returning || inBattle)
            return true;

        return false;
    }

    public void SelectArmy(Military selectedUnit)
    {
        selected = true;
        
        foreach (Military unit in unitsInArmy)
        {
            if (unit == selectedUnit)
                unit.Highlight(Color.green);
            else
                unit.SoftSelect(Color.white);
        }

        if (traveling)
        {
            List<Vector3Int> tempPath = new(pathToTarget);
			tempPath.Add(enemyTarget);

			ShowBattlePath(tempPath, loc);
        }
	}

    public bool AllAreHomeCheck()
    {
        foreach (Military unit in unitsInArmy)
        {
            if (!unit.atHome)
                return false;
        }

        return true;
    }

    private void DestroyDeadList()
    {
        StartCoroutine(DestroyWait());
    }

	private IEnumerator DestroyWait()
	{
		yield return new WaitForSeconds(5);

		foreach (Military unit in deadList)
			Destroy(unit.gameObject);

		deadList.Clear();
	}

	private void DestroyEnemyDeadList()
	{
		StartCoroutine(DestroyEnemyWait());
	}

	private IEnumerator DestroyEnemyWait()
	{
		yield return new WaitForSeconds(5);

		foreach (Military unit in targetCamp.DeadList)
			Destroy(unit.gameObject);

        targetCamp.UnitsInCamp.Clear();
		targetCamp.DeadList.Clear();
        targetCamp.campCount = 0;
        targetCamp = null;
	}

	public void UnselectArmy(Military selectedUnit)
    {
		selected = false;
        
        foreach (Military unit in unitsInArmy)
        {
            if (unit == selectedUnit)
                continue;

            unit.Unhighlight();
        }
	}

    public void AWOLCheck()
    {
        if (noMoneyCycles < 1) //get only one chance
        {
            noMoneyCycles++;
            city.world.uiCampTooltip.WarningCheck();
            world.GetCityDevelopment(city.barracksLocation).exclamationPoint.SetActive(true);
            return;
        }

        AWOLClear();
		//int random = UnityEngine.Random.Range(0, unitsInArmy.Count);
		Military unit = GetMostExpensiveUnit();
        world.unitMovement.AddToCity(unit.homeBase, unit);
		RemoveFromArmy(unit, unit.barracksBunk);
        if (unit.isSelected)
        {
			Military nextUnitUp = GetNextLivingUnit();
			if (nextUnitUp != null)
				world.unitMovement.PrepareMovement(nextUnitUp);
            else
                world.unitMovement.ClearSelection();
		}
		
        unit.DestroyUnit();
	}

    public void AWOLClear()
    {
        if (noMoneyCycles > 0)
        {
            noMoneyCycles = 0;
			world.GetCityDevelopment(city.barracksLocation).exclamationPoint.SetActive(false);
			city.world.uiCampTooltip.WarningCheck();
        }
	}

	public Vector3Int RemoveRandomArmyUnit()
    {
        int random = UnityEngine.Random.Range(0, unitsInArmy.Count);
        Military unit = unitsInArmy[random];
        Vector3Int loc = unit.barracksBunk;
        RemoveFromArmy(unit, loc);
        if (unit.isSelected)
        {
			Military nextUnitUp = GetNextLivingUnit();
			if (nextUnitUp != null)
				world.unitMovement.PrepareMovement(nextUnitUp);
			else
				world.unitMovement.ClearSelection();
		}

        unit.DestroyUnit();
        return loc;
    }

    public Vector3 GetRandomSpot(Vector3Int current)
    {
        int random = UnityEngine.Random.Range(0, totalSpots.Count);
        Vector3Int spot = totalSpots[random];

        if (spot == current)
            spot = totalSpots[3];
        if (current == spot)
            spot = totalSpots[0];

        return spot;
    }

    public Military GetNextLivingUnit()
    {
        List<Military> tempList = new(unitsInArmy);
        
        for (int i = 0; i < tempList.Count; i++)
        {
            if (!tempList[i].isDead)
                return tempList[i];
        }

        return null;
    }

    private Military GetMostExpensiveUnit()
    {
        Military unit = null;
        int gold = 0;

        for (int i = 0; i < unitsInArmy.Count; i++)
        {
            if (i == 0)
            {
                unit = unitsInArmy[i];

                foreach (ResourceValue value in unit.buildDataSO.cycleCost)
                {
                    if (value.resourceType == ResourceType.Gold)
                        gold = value.resourceAmount;
                }
            }

            foreach (ResourceValue value in unitsInArmy[i].buildDataSO.cycleCost)
            {
				if (value.resourceType == ResourceType.Gold && value.resourceAmount > gold)
                {
                    unit = unitsInArmy[i];
					gold = value.resourceAmount;
                }
			}
		}

        return unit;
    }

    public void ResetArmy()
    {
		ClearTraveledPath();
        city.battleIcon.SetActive(false);
		inBattle = false;
		atHome = true;
		cyclesGone = 0;
		stepCount = 0;
        DestroyDeadList();
	}

    //if anyone is transferring during battle stations, instantly move them to barracks
    public void EveryoneHomeCheck()
    {
        for (int i = 0; i < unitsInArmy.Count; i++)
        {
            if (!unitsInArmy[i].atHome)
                unitsInArmy[i].Teleport(unitsInArmy[i].barracksBunk);
        }
    }

	public void ShowBattlePath(List<Vector3Int> currentPath, Vector3Int startingPoint)
	{
		//interpolating path gaps
		List<Vector3Int> pathToShow = new();

		Vector3Int diff = (currentPath[0] - startingPoint) / 3;
		pathToShow.Add(startingPoint + new Vector3Int(diff.x, 0, diff.z));
		pathToShow.Add(startingPoint + new Vector3Int(diff.x, 0, diff.z) * 2);

		for (int i = 0; i < currentPath.Count - 1; i++)
		{
			pathToShow.Add(currentPath[i]);
			Vector3Int nextDiff = (currentPath[i + 1] - currentPath[i]) / 3;
			pathToShow.Add(currentPath[i] + new Vector3Int(nextDiff.x, 0, nextDiff.z));
			pathToShow.Add(currentPath[i] + new Vector3Int(nextDiff.x, 0, nextDiff.z) * 2);
		}

		for (int i = 0; i < pathToShow.Count; i++)
		{
			//position to place chevron
			Vector3 turnCountPosition = pathToShow[i];
			//turnCountPosition.y += 0.01f;

			Vector3 prevPosition;

			if (i == 0)
			{
				prevPosition = startingPoint;
			}
			else
			{
				prevPosition = pathToShow[i - 1];
			}

			//prevPosition.y = 0.01f;
			GameObject path;

			path = world.unitMovement.movementSystem.GetFromChevronPool();

			path.transform.position = (turnCountPosition + prevPosition) * 0.5f;
			float xDiff = turnCountPosition.x - Mathf.Round(prevPosition.x);
			float zDiff = turnCountPosition.z - Mathf.Round(prevPosition.z);

			int z = 0;

			//checking tile placements to see how to rotate chevrons
			if (xDiff < 0)
			{
				if (zDiff > 0)
					z = 135;
				else if (zDiff == 0)
					z = 180;
				else if (zDiff < 0)
					z = 225;
			}
			else if (xDiff == 0)
			{
				if (zDiff > 0)
					z = 90;
				else if (zDiff < 0)
					z = 270;
			}
			else //xDiff > 0
			{
				if (zDiff < 0)
					z = 315;
				else if (zDiff == 0)
					z = 0;
				else
					z = 45;
			}

			path.transform.rotation = Quaternion.Euler(90, 0, z); //x is rotating to lie flat on tile

			pathQueue.Enqueue(path);
		}
	}

	public void HidePath()
	{
		if (pathQueue.Count > 0)
		{
			int count = pathQueue.Count; //can't decrease count while using it

			for (int i = 0; i < count; i++)
			{
				DequeuePath();
			}

			pathQueue.Clear();
		}
	}

	private void DequeuePath()
	{
		GameObject path = pathQueue.Dequeue();
        world.unitMovement.movementSystem.AddToChevronPool(path);
	}
}
