using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Army : MonoBehaviour
{
    private MapWorld world;
    
    private Vector3Int loc;
    [HideInInspector]
    public Vector3Int forward;

    [HideInInspector]
    public EnemyCamp targetCamp;
    [HideInInspector]
    public int armyCount;
    private Vector3Int enemyTarget, attackZone;
    public Vector3Int EnemyTarget { get { return enemyTarget; } }
    private List<Vector3Int> totalSpots = new(), openSpots = new(), pathToTarget = new(), pathTraveled = new();
    [HideInInspector]
    public List<Vector3Int> attackingSpots = new(), movementRange = new(), cavalryRange = new();
    
    private List<Unit> unitsInArmy = new(), deadList = new();
    public List<Unit> UnitsInArmy { get { return unitsInArmy; } }
    public List<Unit> DeadList { get { return deadList; } set { deadList = value; } }
    private int unitsReady, stepCount;

    //private WaitForSeconds waitOneSec = new(0.1f);

    [HideInInspector]
    public bool isEmpty = true, isFull, isTraining, isTransferring, isRepositioning, traveling, inBattle, returning, atHome, selected, enemyReady;

	private void Awake()
	{
        atHome = true;
	}

    public void SetLoc(Vector3Int loc)
    {
        this.loc = loc;
    }

    public void SetWorld(MapWorld world)
    {
        this.world = world;
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
        armyCount++;
    }

    public void RemoveFromArmy(Unit unit, Vector3Int loc)
    {
        unitsInArmy.Remove(unit);
        armyCount--;

        if (armyCount == 0)
            isEmpty = true;
        if (isFull)
            isFull = false;

        int index = totalSpots.IndexOf(loc);

        if (index >= openSpots.Count)
            openSpots.Add(loc);
        else
            openSpots.Insert(index,loc);
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
                UnitReady();
                unit.marchPosition = unit.barracksBunk - loc;
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
						unitDiff += unitDiff.x * new Vector3Int(1, 0, -1);
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

    public bool MoveArmy(Vector3Int current, Vector3Int target, bool deploying)
    {
        Vector3Int destination = deploying ? target : loc;

        if (deploying)
        {
            List<Vector3Int> exemptList = new() { target };
            
            foreach (Vector3Int tile in world.GetNeighborsFor(target, MapWorld.State.EIGHTWAYINCREMENT))
                exemptList.Add(tile);
            
            pathToTarget = GridSearch.TerrainSearch(world, current, destination, exemptList);
        }
        else
        {
            pathTraveled.Reverse();
			pathToTarget = pathTraveled;
        }

        if (pathToTarget.Count == 0)
        {
            if (deploying)
                return false;
            else
                pathToTarget.Add(target);
        }

        unitsReady = 0;

        if (deploying)
        {
            enemyTarget = destination;
            pathToTarget.Remove(pathToTarget[pathToTarget.Count - 1]);
            atHome = false;
            traveling = true;
            Vector3Int penultimate = pathToTarget[pathToTarget.Count - 1];
            attackZone = penultimate;
            forward = (enemyTarget - attackZone) / 3;

            if (returning)
                DeployArmy(true);
            else
                RealignUnits(world, destination, penultimate);
        }
        else
        {
            traveling = false;
            returning = true;
            DestroyDeadList();
            targetCamp = null;
            DeployArmy(false);
        }

        return true;
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

                atHome = true;
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
        //if (!deploying)
        //{
        //    armyCount -= deathCount;
        //    deathCount = 0;
        //}
        
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
		traveling = false;
		movementRange.Clear();
        attackingSpots.Clear();
        movementRange.Add(attackZone);
        cavalryRange.Add(attackZone);
        movementRange.Add(enemyTarget);
        cavalryRange.Add(enemyTarget);
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

    public void ClearTraveledPath()
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
            inBattle = false;
            MoveArmy(enemyTarget, loc, false);

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
        inBattle = false;
        returning = true;
        attackingSpots.Clear();
        DestroyDeadList();
        MoveArmy(attackZone, loc, false);

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
            Color color = unit == selectedUnit ? Color.green : Color.white;
            unit.Select(color);
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
        List<Unit> tempList = new();
        
        for (int i = 0; i < tempList.Count; i++)
        {
            if (!tempList[i].isDead)
                return tempList[i];
        }

        return null;
    }
}
