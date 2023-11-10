using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.UI.CanvasScaler;

public class Army : MonoBehaviour
{
    private MapWorld world;
    
    private Vector3Int loc;
    [HideInInspector]
    public Vector3Int forward, attackZone;

    [HideInInspector]
    public EnemyCamp targetCamp;
    [HideInInspector]
    public int armyCount, cyclesGone, infantryCount, rangedCount, cavalryCount, seigeCount, strength, health;
    private Vector3Int enemyTarget;
    public Vector3Int EnemyTarget { get { return enemyTarget; } set { enemyTarget = value; } }
    [HideInInspector]
    public List<Vector3Int> totalSpots = new(), openSpots = new(), pathToTarget = new(), pathTraveled = new();
    [HideInInspector]
    public List<Vector3Int> attackingSpots = new(), movementRange = new(), cavalryRange = new();
    
    private List<Unit> unitsInArmy = new(), deadList = new();
    public List<Unit> UnitsInArmy { get { return unitsInArmy; } }
    public List<Unit> DeadList { get { return deadList; } set { deadList = value; } }
    [HideInInspector]
    public int unitsReady, stepCount, noMoneyCycles;

    //army maintenance and battle costs
    private Dictionary<ResourceType, int> armyCycleCostDict = new();
    private Dictionary<ResourceType, int> armyBattleCostDict = new();
    private List<ResourceValue> totalBattleCosts = new();

    [HideInInspector]
    public City city;
	private Queue<GameObject> pathQueue = new();
	//private Dictionary<ResourceType, int> armyStagingCostDict = new();
	//private List<Unit> stagingUnit = new();

	//private WaitForSeconds waitOneSec = new(0.1f);

	[HideInInspector]
    public bool isEmpty = true, isFull, isTraining, isTransferring, isRepositioning, traveling, inBattle, returning, atHome, selected, enemyReady, issueRefund = true;

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

	public Vector3Int GetAvailablePosition()
    {
        Vector3Int openSpot = openSpots[0];
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

        if (index >= openSpots.Count) 
            openSpots.Add(oldLoc);
        else
		    openSpots.Insert(index, oldLoc);
		openSpots.Remove(newLoc);
    }

    public void AddToArmy(Unit unit)
    {
        unitsInArmy.Add(unit);

        if (unitsInArmy.IndexOf(unit) == 0)
            unit.isLeader = true;

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

		//if (unit.newlyJoined)
		//{
		//    AddToStagingCost(unit.buildDataSO.cycleCost);
		//    stagingUnit.Add(unit);
		//}
		//else
		AddToCycleCost(unit.buildDataSO.cycleCost);
        AddToBattleCost(unit.buildDataSO.battleCost);

		if (city.cityPop.CurrentPop == 0 && unitsInArmy.Count == 1)
			city.StartFoodCycle();
	}

	//   private void AddToStagingCost(List<ResourceValue> costs)
	//   {
	//       for (int i = 0; i < costs.Count; i++)
	//           armyStagingCostDict[costs[i].resourceType] += costs[i].resourceAmount;
	//}

	private void AddToCycleCost(List<ResourceValue> costs)
    {
		for (int i = 0; i < costs.Count; i++)
        {
            if (!armyCycleCostDict.ContainsKey(costs[i].resourceType))
                armyCycleCostDict[costs[i].resourceType] = costs[i].resourceAmount;
            else
                armyCycleCostDict[costs[i].resourceType] += costs[i].resourceAmount;
        }
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

	//public void AddStagingCostToCycle()
 //   {
 //       foreach (ResourceType type in armyStagingCostDict.Keys)
 //           armyCycleCostDict[type] += armyStagingCostDict[type];

 //       armyStagingCostDict.Clear();

 //       foreach (Unit unit in stagingUnit)
 //           unit.newlyJoined = false;

 //       stagingUnit.Clear();
 //   }

	private void RemoveFromCycleCost(List<ResourceValue> costs)
	{
		for (int i = 0; i < costs.Count; i++)
        {
			armyCycleCostDict[costs[i].resourceType] -= costs[i].resourceAmount;

            if (armyCycleCostDict[costs[i].resourceType] == 0)
                armyCycleCostDict.Remove(costs[i].resourceType);
        }
	}

	//private void RemoveFromStagingCost(List<ResourceValue> costs)
 //   {
 //       for (int i = 0; i < costs.Count; i++)
 //           armyStagingCostDict[costs[i].resourceType] -= costs[i].resourceAmount;
 //   }

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

    public void RemoveFromArmy(Unit unit, Vector3Int loc)
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
                HidePath();
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
			if (unit.isLeader)
				unitsInArmy[0].isLeader = true;

			if (world.uiCampTooltip.activeStatus)
				world.uiCampTooltip.RefreshData();
		}

		if (isFull)
            isFull = false;

        int index = totalSpots.IndexOf(loc);

        if (index >= openSpots.Count)
            openSpots.Add(loc);
        else
            openSpots.Insert(index,loc);

		if (city.cityPop.CurrentPop == 0 && armyCount == 0)
			city.StopFoodCycle();
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
    public void RealignUnits(MapWorld world, Vector3Int targetZone, Vector3Int attackZone)
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

        foreach (Unit unit in unitsInArmy)
        {
            unit.healthbar.CancelRegeneration();
            unit.atHome = false;
            Vector3Int unitDiff = unit.CurrentLocation - loc;

            if (rotation == 0)
            {
                unit.marchPosition = unit.barracksBunk - loc;
                UnitReady();
                continue;
            }
            else if (rotation == 1)
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
                unitDiff *= -1;
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

            List<Vector3Int> path = GridSearch.AStarSearch(world, unit.CurrentLocation, loc + unitDiff, false, false);
            unit.marchPosition = unitDiff;

            if (path.Count > 0)
            {
                unit.preparingToMoveOut = true;
                unit.finalDestinationLoc = loc + unitDiff;
    			unit.MoveThroughPath(path);
            }
            else
            {
                UnitReady();
            }
        }
    }

    public bool DeployArmyCheck(Vector3Int current, Vector3Int target)
    {
		List<Vector3Int> exemptList = new() { target };
        enemyTarget = target;

		foreach (Vector3Int tile in world.GetNeighborsFor(target, MapWorld.State.EIGHTWAYINCREMENT))
			exemptList.Add(tile);

		pathToTarget = GridSearch.TerrainSearch(world, current, target, exemptList);

        if (pathToTarget.Count == 0)
            return false;
        else
            return true;
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
			RealignUnits(world, enemyTarget, penultimate);
	}

    public void ShowBattlePath()
    {
        ShowBattlePath(pathToTarget, loc);
    }

    public void MoveArmyHome(Vector3Int target)
    {
        pathTraveled.Reverse();
		pathToTarget = pathTraveled;

        if (pathToTarget.Count == 0)
            pathToTarget.Add(target);

        unitsReady = 0;
        traveling = false;
        returning = true;
        DestroyDeadList();
        targetCamp = null;
        DeployArmy(false);
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
        city.ResourceManager.ConsumeResources(totalBattleCosts, 1, city.barracksLocation, false);
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
			foreach (ResourceValue value in totalBattleCosts)
			{
                int amount;

                if (armyCycleCostDict.ContainsKey(value.resourceType))
                    amount = value.resourceAmount - cyclesGone * armyCycleCostDict[value.resourceType];
                else
                    amount = armyBattleCostDict[value.resourceType];

                if (amount > 0)
                {
                    city.ResourceManager.CheckResource(value.resourceType, amount);
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

    public void UnitReady()
    {
        unitsReady++;

        if (unitsReady == armyCount)
        {
            unitsReady = 0;
            DeployArmy(true);
        }
    }

    public void UnitArrived(Vector3Int loc)
    {
        unitsReady++;

        if (unitsReady == armyCount)
        {
            unitsReady = 0;
            
            if (this.loc == loc)
            {
                if (openSpots.Count == 0)
                    isFull = true;

                IssueBattleRefund();
                atHome = true;
                cyclesGone = 0;
                returning = false;
                
                pathTraveled.Clear();
                stepCount = 0;

                if (world.cityBuilderManager.uiUnitBuilder.activeStatus)
                    world.cityBuilderManager.uiUnitBuilder.UpdateBarracksStatus(isFull);
            }
            else
            {
                targetCamp.armyReady = true;

                if (targetCamp.attackReady)
                {
                    targetCamp.attackReady = false;
                    targetCamp.armyReady = false;
                    Charge();
                }
            }
        }
    }

    public void UnitNextStep()
    {
        unitsReady++;

        if (unitsReady == armyCount)
        {
            unitsReady = 0;
            if (traveling)
            {
                pathTraveled.Add(pathToTarget[stepCount]);
                stepCount++;
            }

            BeginNextStep();
        }
    }

    public void BeginNextStep()
    {
        foreach (Unit unit in unitsInArmy)
            unit.readyToMarch = true;
    }

    private void DeployArmy(bool deploying)
    {
        foreach (Unit unit in unitsInArmy)
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

    //private IEnumerator WaitOneSec()
    //{
    //    yield return waitOneSec;

    //    if (!inBattle)
    //        ArmyCharge();

    //    if (!targetCamp.inBattle)
    //        targetCamp.Charge();
    //}

    private void ArmyCharge()
    {
        inBattle = true;
        
        foreach (Unit unit in unitsInArmy)
        {
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

    //public void TargetCheck()
    //{
    //    for (int i = 0; i < unitsInArmy.Count; i++)
    //    {
    //        if (unitsInArmy[i].targetSearching)
    //            unitsInArmy[i].AggroCheck();
    //    }
    //}

    public Unit FindClosestTarget(Unit unit)
    {        
        Unit closestEnemy = null;
        float dist = 0;
        List<Unit> tempList = new(targetCamp.UnitsInCamp);
        bool firstOne = true;

        //find closest target
        for (int i = 0; i < tempList.Count; i++)
        {
            Unit enemy = tempList[i];

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

            ///check both sides if in middle
            //if (currentOffset == 0)
            //{
                //tilesToCheck.Add(new Vector3Int(-1 * currentOffset, 0, 0) + enemyTarget);
			    //tilesToCheck.Add(new Vector3Int(-1 * currentOffset, 0, 1 * battleDiff.z) + enemyTarget);
            //}
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

			//if (currentOffset == 0)
			//{
			    //tilesToCheck.Add(new Vector3Int(0, 0, -1 * currentOffset) + enemyTarget);
			    //tilesToCheck.Add(new Vector3Int(1 * battleDiff.x, 0, -1 * currentOffset) + enemyTarget);
			//}
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
        return GridSearch.BattleMove(world, pos, target, movementRange, attackingSpots);
	}

    public List<Vector3Int> CavalryPathToEnemy(Vector3Int pos, Vector3Int target)
    {
		return GridSearch.BattleMove(world, pos, target, cavalryRange, attackingSpots);
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
            returning = true;
            DestroyDeadList();

            foreach (Unit unit in unitsInArmy)
            {
                unit.StopAttacking();
            }

            attackingSpots.Clear();
            world.unitMovement.uiCancelTask.ToggleTweenVisibility(false);
            //world.unitMovement.uiDeployArmy.ToggleTweenVisibility(true);
            city.battleIcon.SetActive(false);
            inBattle = false;
            MoveArmyHome(loc);

            return true;
        }
        else
        {
            return false;
        }
    }

	public void Retreat()
    {
        foreach (Unit unit in unitsInArmy)
        {
            unit.StopAttacking();
        }

        StartCoroutine(targetCamp.RetreatTimer());
		world.unitMovement.uiCancelTask.ToggleTweenVisibility(false);
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

    public void SelectArmy(Unit selectedUnit)
    {
        selected = true;
        
        foreach (Unit unit in unitsInArmy)
        {
            if (unit == selectedUnit)
                unit.Select(Color.green);
            else
                unit.SoftSelect(Color.white);
        }
    }

    public bool AllAreHomeCheck()
    {
        foreach (Unit unit in unitsInArmy)
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

		foreach (Unit unit in deadList)
			Destroy(unit.gameObject);

		deadList.Clear();
	}

    public void UnselectArmy(Unit selectedUnit)
    {
		selected = false;
        
        foreach (Unit unit in unitsInArmy)
        {
            if (unit == selectedUnit)
                continue;

            unit.Deselect();
        }
	}

    public void AWOLCheck()
    {
        if (noMoneyCycles < 1) //get only one chance
        {
            noMoneyCycles++;
            return;
        }

        noMoneyCycles = 0;
        //int random = UnityEngine.Random.Range(0, unitsInArmy.Count);
        Unit unit = GetMostExpensiveUnit();
        world.unitMovement.AddToCity(unit.homeBase, unit);
		RemoveFromArmy(unit, unit.barracksBunk);
        if (unit.isSelected)
        {
			Unit nextUnitUp = GetNextLivingUnit();
			if (nextUnitUp != null)
				world.unitMovement.PrepareMovement(nextUnitUp);
            else
                world.unitMovement.ClearSelection();
		}
		
        unit.DestroyUnit();
	}

	public Vector3Int RemoveRandomArmyUnit()
    {
        int random = UnityEngine.Random.Range(0, unitsInArmy.Count);
        Unit unit = unitsInArmy[random];
        Vector3Int loc = unit.barracksBunk;
        RemoveFromArmy(unit, loc);
        if (unit.isSelected)
        {
			Unit nextUnitUp = GetNextLivingUnit();
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

    public Unit GetNextLivingUnit()
    {
        List<Unit> tempList = new(unitsInArmy);
        
        for (int i = 0; i < tempList.Count; i++)
        {
            if (!tempList[i].isDead)
                return tempList[i];
        }

        return null;
    }

    private Unit GetMostExpensiveUnit()
    {
        Unit unit = null;
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
