using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
//using static UnityEngine.RuleTile.TilingRuleOutput;

public class EnemyCamp 
{
	public MapWorld world;
	
	public Vector3Int loc, forward;
	public Vector3Int armyDiff;

	public GameObject campfire;

	public int enemyReady;
	public int campCount, deathCount, infantryCount, rangedCount, cavalryCount, seigeCount, health, strength;
	public bool revealed, prepping, attacked, attackReady = false, armyReady, inBattle, returning;
	public Army attackingArmy;

	//public Queue<Vector3Int> threatQueue = new();
	public Vector3Int threatLoc;

	private List<Unit> unitsInCamp = new(), deadList = new();
    public List<Unit> UnitsInCamp { get { return unitsInCamp; } set { unitsInCamp = value; } }
	public List<Unit> DeadList { get { return deadList; } set { deadList = value; } }
    Vector3Int[] frontLines = { new Vector3Int(0, 0, -1), new Vector3Int(-1, 0, -1), new Vector3Int(1, 0, -1) };
	Vector3Int[] midLines = { new Vector3Int(0, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(1, 0, 0) };
	Vector3Int[] backLines = { new Vector3Int(0, 0, 1), new Vector3Int(-1, 0, 1), new Vector3Int(1, 0, 1) };

	public GameObject minimapIcon;

	private WaitForSeconds retreatTime = new(8);

	public void SetCampfire(GameObject campfire, bool isHill, bool discovered)
	{
		Vector3 campfireLoc = loc;
		if (isHill)
			campfireLoc.y += 0.65f;
		campfire.transform.position = campfireLoc;
		this.campfire = campfire;

		if (!discovered)
			this.campfire.SetActive(false);
	}

	public void FormBattlePositions()
    {
        int infantry = 0;
        int cavalry = 0;
        int ranged = 0;
        
        //first only position the infantry
        foreach (Unit unit in unitsInCamp)
        {
            if (unit.buildDataSO.unitType == UnitType.Infantry)
            {
				infantryCount++;
				health += unit.buildDataSO.health;
				strength += unit.buildDataSO.baseAttackStrength;
				
				if (infantry < 3)
                    unit.barracksBunk = loc + frontLines[infantry];
                else if (infantry < 6)
					unit.barracksBunk = loc + midLines[infantry - 3];
                else
					unit.barracksBunk = loc + backLines[infantry - 6];

				infantry++;
            }
		}

        foreach (Unit unit in unitsInCamp)
        {
            if (unit.buildDataSO.unitType == UnitType.Cavalry)
            {
				cavalryCount++;
				health += unit.buildDataSO.health;
				strength += unit.buildDataSO.baseAttackStrength;

				if (cavalry + infantry < 3)
					unit.barracksBunk = loc + frontLines[cavalry + infantry];
				else if (cavalry + infantry < 6)
					unit.barracksBunk = loc + midLines[cavalry + infantry - 3];
				else
					unit.barracksBunk = loc + backLines[cavalry + infantry - 6];

				cavalry++;
            }
        }

		foreach (Unit unit in unitsInCamp)
        {
			if (unit.buildDataSO.unitType == UnitType.Ranged)
			{
				rangedCount++;
				health += unit.buildDataSO.health;
				strength += unit.buildDataSO.baseAttackStrength;

				if (ranged < 3)
				{
                    if (infantry+cavalry > 6)
						unit.barracksBunk = loc + backLines[2 - ranged];
					else
						unit.barracksBunk = loc + backLines[ranged];
				}
                else if (ranged < 6)
				{
					if (infantry+cavalry+ranged > 3)
	                    unit.barracksBunk = loc + midLines[5 - ranged];
					else
						unit.barracksBunk = loc + midLines[ranged];
				}
				else if (ranged < 9)
					unit.barracksBunk = loc + frontLines[8 - ranged];

				ranged++;
			}
        }

		campCount = unitsInCamp.Count;
    }

	public Dictionary<Vector3Int, string> SendCampData()
	{
		Dictionary<Vector3Int, string> campDict = new();

		for (int i = 0; i < unitsInCamp.Count; i++)
		{
			campDict[unitsInCamp[i].enemyAI.CampSpot] = unitsInCamp[i].buildDataSO.unitNameAndLevel;
		}
		
		return campDict;
	}

	public EnemyCampData SendCampUnitData()
	{
		EnemyCampData campData = new();
		
		List<UnitData> campList = new();

		for (int i = 0; i < unitsInCamp.Count; i++)
		{
			campList.Add(unitsInCamp[i].SaveMilitaryUnitData());
		}

		campData.enemyReady = enemyReady;
		campData.threatLoc = threatLoc;
		campData.forward = forward;
		campData.revealed = revealed;
		campData.prepping = prepping;
		campData.attacked = attacked;
		campData.attackReady = attackReady;
		campData.armyReady = armyReady;
		campData.inBattle = inBattle;
		campData.returning = returning;
		campData.allUnits = campList;
		campData.campCount = campCount;
		campData.infantryCount = infantryCount;
		campData.rangedCount = rangedCount;
		campData.cavalryCount = cavalryCount;
		campData.seigeCount = seigeCount;
		campData.health = health;
		campData.strength = strength;	

		return campData;
	}

	public List<ResourceValue> GetAttackCost()
	{
		List<ResourceValue> totalCost = new();

		return totalCost;
	}

    public void BattleStations()
    {
		if (attackReady || prepping)
			return;

		if (campfire != null)
			campfire.SetActive(false);
		returning = false;
		prepping = true;
		
		Vector3Int diff = (threatLoc - loc) / 3;

		int rotation;

		if (diff.x == -1)
			rotation = 1;
		else if (diff.z == 1)
			rotation = 2;
		else if (diff.x == 1)
			rotation = 3;
		else
			rotation = 0;

		foreach (Unit unit in unitsInCamp)
		{
			if (unit.buildDataSO.unitType != UnitType.Cavalry)
				unit.ToggleSitting(false);
			
			Vector3Int unitDiff = unit.barracksBunk - loc;

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

			List<Vector3Int> path = GridSearch.AStarSearch(world, unit.CurrentLocation, loc + unitDiff, false, false, true);

			if (path.Count > 0)
			{
				if (unit.isMoving)
				{
					unit.StopAnimation();
					unit.ShiftMovement();
				}

				unit.preparingToMoveOut = true;
				unit.finalDestinationLoc = loc + unitDiff;
				unit.MoveThroughPath(path);
			}
			else
			{
				EnemyReady(unit);
			}
		}
	}

	//getting ready to attack
	public void EnemyReady(Unit unit)
	{
		unit.Rotate(unit.CurrentLocation + forward);
		enemyReady++;

		if (enemyReady == campCount)
		{
			enemyReady = 0;
			prepping = false;
			attackReady = true;

			if (armyReady)
			{
				armyReady = false;
				attackReady = false;
				attackingArmy.Charge();
			}
		}
	}

	//getting ready to camp
	public void EnemyReturn(Unit unit)
	{
		enemyReady++;
		unit.Rotate(loc);
		if (unit.buildDataSO.unitType != UnitType.Cavalry)
			unit.ToggleSitting(true);

		if (unit.currentHealth < unit.buildDataSO.health)
			unit.healthbar.RegenerateHealth();

		if (enemyReady == campCount - deathCount)
		{
			inBattle = false;
			enemyReady = 0;
			ResetStatus();
			ResurrectCamp();
		}
	}

	public void Charge()
	{
		inBattle = true;

		foreach (Unit unit in unitsInCamp)
		{
			unit.inBattle = true;
			UnitType type = unit.buildDataSO.unitType;

			if (type == UnitType.Infantry)
				unit.enemyAI.InfantryAggroCheck();
			else if (type == UnitType.Ranged)
				unit.enemyAI.RangedAggroCheck();
			else if (type == UnitType.Cavalry)
				unit.enemyAI.CavalryAggroCheck();
		}
	}

	public Unit FindClosestTarget(Unit unit)
	{
		Unit closestEnemy = null;
		float dist = 0;
		List<Unit> tempList = new(attackingArmy.UnitsInArmy);
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
				dist = Mathf.Abs(enemy.transform.position.x - unit.transform.position.x) + Mathf.Abs(enemy.transform.position.z - unit.transform.position.z); //not using sqrMagnitude in case of hill
				continue;
			}

			float nextDist = Mathf.Abs(enemy.transform.position.x - unit.transform.position.x) + Mathf.Abs(enemy.transform.position.z - unit.transform.position.z);

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
		Vector3Int battleDiff = (threatLoc - loc) / 3;
		List<Vector3Int> tilesToCheck = new();

		if (battleDiff.z != 0)
		{
			int currentOffset = Math.Sign(currentLoc.x - loc.x);

			//ignore if in middle
			if (currentOffset == 0)
				return null;

			tilesToCheck.Add(new Vector3Int(1 * currentOffset, 0, 0) + threatLoc);
			tilesToCheck.Add(threatLoc);
			tilesToCheck.Add(new Vector3Int(1 * currentOffset, 0, 1 * battleDiff.z) + threatLoc);
			tilesToCheck.Add(new Vector3Int(0, 0, 1 * battleDiff.z) + threatLoc);
		}
		else
		{
			int currentOffset = Math.Sign(currentLoc.z - loc.z);

			if (currentOffset == 0)
				return null;

			tilesToCheck.Add(new Vector3Int(0, 0, 1 * currentOffset) + threatLoc);
			tilesToCheck.Add(threatLoc);
			tilesToCheck.Add(new Vector3Int(1 * battleDiff.x, 0, 1 * currentOffset) + threatLoc);
			tilesToCheck.Add(new Vector3Int(1 * battleDiff.x, 0, 0) + threatLoc);
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
		return GridSearch.BattleMove(world, pos, target, attackingArmy.movementRange, attackingArmy.attackingSpots);
	}

	public List<Vector3Int> CavalryPathToEnemy(Vector3Int pos, Vector3Int target)
	{
		return GridSearch.BattleMove(world, pos, target, attackingArmy.cavalryRange, attackingArmy.attackingSpots);
	}

	public bool FinishAttack()
	{
		if (returning)
			return true;
		
		if (attackingArmy != null && attackingArmy.armyCount == 0)
		{
			returning = true;
			
			if (attackingArmy != null)
			{
				if (world.cityBuilderManager.uiUnitBuilder.activeStatus)
					world.cityBuilderManager.uiUnitBuilder.UpdateBarracksStatus(attackingArmy.isFull);
				attackingArmy.ResetArmy();
				attackingArmy = null;
			}

			foreach (Unit unit in unitsInCamp)
				unit.StopAttacking();

			ReturnToCamp();

			return true;
		}
		else
		{
			return false;
		}
	}

	public void ClearCampCheck()
	{
		if (deathCount == campCount)
			world.RemoveEnemyCamp(loc);
	}

	public void TargetSearchCheck()
	{
		foreach (Unit unit in UnitsInCamp)
		{
			if (unit.targetSearching)
				unit.enemyAI.StartReturn();
		}
	}

	public void ResetStatus()
	{
		attacked = false;
		attackReady = false;
		armyReady = false;
		attackingArmy = null;
		prepping = false;
	}

	public void ReturnToCamp()
	{
		foreach (Unit unit in unitsInCamp)
		{
			unit.inBattle = false; //leaving it here just in case
			unit.enemyAI.StartReturn();
		}
	}

	public IEnumerator RetreatTimer()
	{
		yield return retreatTime;

		foreach (Unit unit in unitsInCamp)
		{
			if (unit.attacking || unit.targetSearching)
				unit.enemyAI.StartReturn();
		}
	}

	private void ResurrectCamp()
	{
		if (campfire != null)
			campfire.SetActive(true);
		
		foreach (Unit unit in deadList)
		{
			Vector3 rebornSpot;
			if (world.GetTerrainDataAt(unit.enemyAI.CampSpot).isHill)
				rebornSpot = unit.enemyAI.CampSpot + new Vector3(0, 0.6f, 0);
			else
				rebornSpot = unit.enemyAI.CampSpot;

			unit.unitRigidbody.useGravity = true;
			unit.transform.position = rebornSpot;
			unit.moreToMove = false;
			unit.isMoving = false;
			unit.CurrentLocation = unit.enemyAI.CampSpot;
			unit.isDead = false;
			unit.currentHealth = unit.buildDataSO.health;
			unit.healthbar.gameObject.SetActive(false);

			Vector3 goScale = unit.transform.localScale;
			float scaleX = goScale.x;
			float scaleZ = goScale.z;
			unit.transform.localScale = new Vector3(scaleX, 0.2f, scaleZ); //don't start at 0, otherwise lightbeam meshes with ground
			//unit.minimapIcon.gameObject.SetActive(true);
			//unit.unitMesh.gameObject.SetActive(true);
			unit.unitMesh.gameObject.SetActive(true);
			rebornSpot.y += 0.07f;
			unit.lightBeam.transform.position = rebornSpot;
			unit.lightBeam.Play();
			//unit.healthbar.gameObject.SetActive(true);
			LeanTween.scale(unit.gameObject, goScale, 0.5f).setEase(LeanTweenType.easeOutBack);

			unit.Rotate(loc);
			if (unit.buildDataSO.unitType != UnitType.Cavalry)
				unit.ToggleSitting(true);
		}

		deathCount = 0;
		deadList.Clear();
		GameLoader.Instance.gameData.attackedEnemyBases.Remove(loc);
	}
}
