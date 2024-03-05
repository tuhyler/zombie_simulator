using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using static Unity.Burst.Intrinsics.X86.Avx;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.UI.CanvasScaler;
//using static UnityEngine.RuleTile.TilingRuleOutput;

public class EnemyCamp 
{
	public MapWorld world;
	
	public Vector3Int loc, forward, moveToLoc;
	public Vector3Int armyDiff, fieldBattleLoc, lastSpot;

	public GameObject campfire;

	public int enemyReady;
	public int campCount, deathCount, infantryCount, rangedCount, cavalryCount, seigeCount, health, strength, pillageTime;
	public bool revealed, prepping, attacked, attackReady = false, armyReady, inBattle, returning, movingOut, chasing, isCity, pillage, growing, removingOut;
	public Army attackingArmy;

	//public Queue<Vector3Int> threatQueue = new();
	public Vector3Int threatLoc, cityLoc;
	public List<Vector3Int> pathToTarget = new();

	private List<Military> unitsInCamp = new(), deadList = new();
    public List<Military> UnitsInCamp { get { return unitsInCamp; } set { unitsInCamp = value; } }
	public List<Military> DeadList { get { return deadList; } set { deadList = value; } }
    Vector3Int[] frontLines = { new Vector3Int(0, 0, -1), new Vector3Int(-1, 0, -1), new Vector3Int(1, 0, -1) };
	Vector3Int[] midLines = { new Vector3Int(0, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(1, 0, 0) };
	Vector3Int[] backLines = { new Vector3Int(0, 0, 1), new Vector3Int(-1, 0, 1), new Vector3Int(1, 0, 1) };

	public GameObject minimapIcon;

	public List<Vector3Int> openSpots, totalSpots;

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
        foreach (Military unit in unitsInCamp)
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

        foreach (Military unit in unitsInCamp)
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

		foreach (Military unit in unitsInCamp)
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
				{
					unit.barracksBunk = loc + frontLines[8 - ranged];
				}

				ranged++;
			}
        }

		campCount = unitsInCamp.Count;
    }

	public Dictionary<Vector3Int, string> SendCampData()
	{
		Dictionary<Vector3Int, string> campDict = new();

		for (int i = 0; i < unitsInCamp.Count; i++)
			campDict[unitsInCamp[i].enemyAI.CampSpot] = unitsInCamp[i].buildDataSO.unitNameAndLevel;
		
		return campDict;
	}

	public EnemyCampData SendCampUnitData()
	{
		EnemyCampData campData = new();
		
		List<UnitData> campList = new();

		for (int i = 0; i < unitsInCamp.Count; i++)
			campList.Add(unitsInCamp[i].SaveMilitaryUnitData());

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

	public EnemyCampData SendMovingCampUnitData()
	{
		EnemyCampData campData = new();

		List<UnitData> campList = new();

		for (int i = 0; i < unitsInCamp.Count; i++)
			campList.Add(unitsInCamp[i].SaveMilitaryUnitData());

		campData.enemyReady = enemyReady;
		campData.chaseLoc = moveToLoc;
		campData.threatLoc = threatLoc;
		campData.forward = forward;
		campData.revealed = revealed;
		campData.prepping = prepping;
		campData.attacked = attacked;
		campData.attackReady = attackReady;
		campData.armyReady = armyReady;
		campData.inBattle = inBattle;
		campData.campCount = campCount;
		campData.infantryCount = infantryCount;
		campData.rangedCount = rangedCount;
		campData.cavalryCount = cavalryCount;
		campData.seigeCount = seigeCount;
		campData.health = health;
		campData.strength = strength;
		campData.pathToTarget = pathToTarget;
		campData.movingOut = movingOut;
		campData.returning = returning;
		campData.chasing = chasing;
		campData.allUnits = campList;
		campData.pillage = pillage;
		campData.pillageTime = pillageTime;
		campData.growing = growing;
		campData.fieldBattleLoc = fieldBattleLoc;
		campData.lastSpot = lastSpot;
		campData.removingOut = removingOut;
		campData.countDownTimer = world.GetEnemyCity(cityLoc).countDownTimer;

		return campData;
	}

    public void BattleStations(Vector3Int travelLoc, Vector3Int forward)
    {
		if (attackReady || prepping)
			return;

		if (unitsInCamp.Count == 0)
		{
			attackReady = true;
			return;
		}

		if (campfire != null)
			campfire.SetActive(false);
		returning = false;
		prepping = true;

		int rotation;

		if (forward.x == -1)
			rotation = 1;
		else if (forward.z == 1)
			rotation = 2;
		else if (forward.x == 1)
			rotation = 3;
		else
			rotation = 0;

		foreach (Military unit in unitsInCamp)
		{
			if (unit.isDead)
				continue;
			
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

			List<Vector3Int> path = GridSearch.AStarSearchEnemy(world, unit.currentLocation, travelLoc + unitDiff, unit.bySea);
			unit.marchPosition = unitDiff;

			if (path.Count > 0)
			{
				if (unit.isMoving)
				{
					unit.StopAnimation();
					unit.ShiftMovement();
				}

				unit.preparingToMoveOut = true;
				unit.finalDestinationLoc = travelLoc + unitDiff;
				unit.MoveThroughPath(path);
			}
			else
			{
				EnemyReady(unit);
			}
		}
	}

	//getting ready to attack
	public void EnemyReady(Military unit)
	{
		unit.repositioning = false;
		unit.Rotate(unit.currentLocation + forward);
		enemyReady++;

		if (enemyReady == campCount - deathCount)
		{
			enemyReady = 0;
			prepping = false;

			if (movingOut && !attacked)
			{
				if (chasing)
				{
					for (int i = 0; i < unitsInCamp.Count; i++)
						unitsInCamp[i].minimapIcon.gameObject.SetActive(true);

					world.mainPlayer.StartRunningAway();
					List<Vector3Int> avoidList = new();
					pathToTarget = GridSearch.TerrainSearchEnemy(world, loc, moveToLoc, avoidList);
				}

				if (isCity && fieldBattleLoc != cityLoc)
					RemoveOutCamp();
				else
					MoveOutCamp();

				return;
			}

			attacked = false;
			attackReady = true;
			if (armyReady)
			{
				world.uiAttackWarning.AttackNotification(((Vector3)attackingArmy.attackZone + attackingArmy.enemyTarget)*0.5f);
				armyReady = false;
				attackReady = false;
				attackingArmy.Charge();
			}
		}
	}

	//getting ready to camp
	public void EnemyReturn(Military unit)
	{
		enemyReady++;

		if (campCount < 9)
			unit.Rotate(loc);
		else
			unit.Rotate(world.GetNeighborsFor(loc, MapWorld.State.EIGHTWAY)[UnityEngine.Random.Range(0, 8)]);

		if (unit.buildDataSO.unitType != UnitType.Cavalry && !isCity)
			unit.ToggleSitting(true);

		if (unit.currentHealth < unit.buildDataSO.health)
			unit.healthbar.RegenerateHealth();

		if (enemyReady == campCount - deathCount)
		{
			if (isCity)
			{
				if (growing)
				{
					world.GetEnemyCity(cityLoc).StartSpawnCycle(false);
				}
				else
				{
					world.GetEnemyCity(cityLoc).countDownTimer += 10; //give a bit of a head start
					world.GetEnemyCity(cityLoc).LoadSendAttackWait();
				}
			}

			inBattle = false;
			enemyReady = 0;
			ResetStatus();

			ResurrectCamp();
		}
	}

	public void Charge()
	{
		inBattle = true;

		foreach (Military unit in unitsInCamp)
		{
			unit.isMarching = false;
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

		if (attackingArmy == null)
			return null;

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
		Vector3Int battleDiff = (attackingArmy.attackZone - attackingArmy.enemyTarget) / 3;
		List<Vector3Int> tilesToCheck = new();

		if (battleDiff.z != 0)
		{
			int currentOffset = Math.Sign(currentLoc.x - attackingArmy.enemyTarget.x);

			//ignore if cavalry in middle
			if (currentOffset == 0)
				return null;

			tilesToCheck.Add(new Vector3Int(1 * currentOffset, 0, 0) + attackingArmy.attackZone);
			tilesToCheck.Add(attackingArmy.attackZone);
			tilesToCheck.Add(new Vector3Int(1 * currentOffset, 0, 1 * battleDiff.z) + attackingArmy.attackZone);
			tilesToCheck.Add(new Vector3Int(0, 0, 1 * battleDiff.z) + attackingArmy.attackZone);
		}
		else
		{
			int currentOffset = Math.Sign(currentLoc.z - attackingArmy.enemyTarget.z);

			if (currentOffset == 0)
				return null;

			tilesToCheck.Add(new Vector3Int(0, 0, 1 * currentOffset) + attackingArmy.attackZone);
			tilesToCheck.Add(attackingArmy.attackZone);
			tilesToCheck.Add(new Vector3Int(1 * battleDiff.x, 0, 1 * currentOffset) + attackingArmy.attackZone);
			tilesToCheck.Add(new Vector3Int(1 * battleDiff.x, 0, 0) + attackingArmy.attackZone);
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
		if (returning || removingOut)
			return true;
		
		if (attackingArmy != null && attackingArmy.armyCount == 0)
		{
			if (fieldBattleLoc == cityLoc)
				returning = true;
			else
				removingOut = true;

			world.ToggleCityMaterialClear(isCity ? cityLoc : loc, attackingArmy.city.cityLoc, attackingArmy.enemyTarget, attackingArmy.attackZone, false);

			foreach (Military unit in unitsInCamp)
				unit.StopAttacking();

			if (attackingArmy != null)
			{
				if (world.cityBuilderManager.uiUnitBuilder.activeStatus)
					world.cityBuilderManager.uiUnitBuilder.UpdateBarracksStatus(attackingArmy.isFull);
				attackingArmy.ResetArmy();
				attackingArmy.targetCamp = null;
				attackingArmy.defending = false;
				attackingArmy = null; //so that safety nets are thrown

				if (movingOut)
				{
					if (fieldBattleLoc != cityLoc)
					{
						RemoveOut();
						return true;
					}
					
					GoToPillage();
					pillage = true;

					return true;
				}
			}

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
		{
			if (!movingOut)
				world.RemoveEnemyCamp(attackingArmy.enemyTarget, isCity);
		}
	}

	public void TargetSearchCheck()
	{
		foreach (Military unit in unitsInCamp)
		{
			if (unit.targetSearching)
				unit.enemyAI.StartReturn();
		}
	}

	private void ResetStatus()
	{
		attacked = false;
		attackReady = false;
		armyReady = false;
		attackingArmy = null;
		prepping = false;
	}

	public void ResetCamp()
	{
		ResetStatus();

		campCount = 0;
		infantryCount = 0;
		rangedCount = 0;
		cavalryCount = 0;
		seigeCount = 0;
		health = 0;
		strength = 0;
	}

	public void ReturnToCamp()
	{
		enemyReady = 0;
		
		foreach (Military unit in unitsInCamp)
		{
			unit.inBattle = false; //leaving it here just in case
			unit.preparingToMoveOut = false;
			unit.isMarching = false;
			if (!unit.repositioning)
				unit.enemyAI.StartReturn();
		}
	}

	public IEnumerator RetreatTimer()
	{
		yield return retreatTime;

		foreach (Military unit in unitsInCamp)
		{
			if (unit.attacking || unit.targetSearching)
			{
				if (!unit.repositioning)
					unit.enemyAI.StartReturn();
			}
		}
	}

	private void ResurrectCamp()
	{
		if (campfire != null)
			campfire.SetActive(true);
		
		foreach (Military unit in deadList)
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
			unit.currentLocation = unit.enemyAI.CampSpot;
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
			if (unit.buildDataSO.unitType != UnitType.Cavalry && !isCity)
				unit.ToggleSitting(true);
		}

		deathCount = 0;
		deadList.Clear();
		GameLoader.Instance.gameData.attackedEnemyBases.Remove(loc);
		attackingArmy = null;
	}

	public void StartChase(Vector3Int loc)
	{
		movingOut = true;
		chasing = true;
		moveToLoc = loc;
		GameLoader.Instance.gameData.movingEnemyBases[this.loc] = new();

		//getting closest tile to determine threat loc
		bool firstOne = true;
		int dist = 0;
		Vector3Int closestTile = this.loc;
		foreach (Vector3Int tile in world.GetNeighborsFor(this.loc, MapWorld.State.FOURWAYINCREMENT))
		{
			if (firstOne)
			{
				firstOne = false;
				dist = Mathf.Abs(tile.x - loc.x) + Mathf.Abs(tile.z - loc.z);
				closestTile = tile;
				continue;
			}

			int newDist = Mathf.Abs(tile.x - loc.x) + Mathf.Abs(tile.z - loc.z);
			if (newDist < dist)
			{
				dist = newDist;
				closestTile = tile;
			}
		}

		threatLoc = closestTile;
		forward = (threatLoc - loc) / 3;
		BattleStations(this.loc, forward);
	}

	public void FinishMoveOut()
	{
		if (!returning)
		{
			returning = true;
			pathToTarget.Reverse();
			pathToTarget.RemoveAt(0);
			//pathToTarget.Add(loc);
			MoveOutCamp();
		}
	}

	private void MoveOutCamp()
	{
		if (world.IsCityOnTile(moveToLoc) && pathToTarget.Count < 3)
			world.CityBattleStations(moveToLoc, threatLoc, this);

		foreach (Military unit in unitsInCamp)
		{
			if (unit.isDead)
				continue;
			
			if (!chasing && !returning)
				unit.isMarching = true;
			
			unit.preparingToMoveOut = false;
			unit.attacking = false;
			unit.attackCo = null;
			List<Vector3Int> path = new();

			foreach (Vector3Int tile in pathToTarget)
				path.Add(tile + unit.marchPosition);

			//if (returning)
			//	path.Add(unit.barracksBunk);

			if (unit.isMoving)
			{
				unit.StopAnimation();
				unit.ShiftMovement();
			}

			//if (!chasing)
			//	unit.isMarching = true;

			unit.finalDestinationLoc = path[path.Count - 1];
			unit.MoveThroughPath(path);
		}
	}

	private void RemoveOutCamp()
	{
		foreach (Military unit in unitsInCamp)
		{
			unit.isMarching = true;
			unit.preparingToMoveOut = false;
			unit.attacking = false;
			unit.attackCo = null;
			List<Vector3Int> path = new();

			int start = pathToTarget.IndexOf(fieldBattleLoc);
			for (int i = start + 1; i < pathToTarget.Count; i++)
				path.Add(pathToTarget[i] + unit.marchPosition);

			if (unit.isMoving)
			{
				unit.StopAnimation();
				unit.ShiftMovement();
			}

			unit.finalDestinationLoc = path[path.Count - 1];
			unit.MoveThroughPath(path);
		}

		removingOut = false;
		fieldBattleLoc = cityLoc;
	}

	public void UnitArrived()
	{
		enemyReady++;

		if (enemyReady == campCount - deathCount)
		{
			enemyReady = 0;
			
			if (chasing)
			{
				chasing = false;

				for (int i = 0; i < unitsInCamp.Count; i++)
				{
					unitsInCamp[i].looking = true;
					unitsInCamp[i].StartLookingAround();
				}
			}
			else if (returning)
			{
				if (!isCity)
				{
					for (int i = 0; i < unitsInCamp.Count; i++)
						unitsInCamp[i].minimapIcon.gameObject.SetActive(false);
				}

				ReturnToCamp();
			}
			else
			{
				if (attackingArmy == null || attackingArmy.UnitsInArmy.Count == 0)
				{
					world.uiAttackWarning.AttackNotification(((Vector3)moveToLoc + threatLoc) * 0.5f);
					GoToPillage();
					pillage = true;
				}
				else
				{
					attackReady = true;
					
					if (armyReady)
					{
						world.uiAttackWarning.AttackNotification(((Vector3)moveToLoc + threatLoc) * 0.5f);
						attackingArmy.Charge();
					}
				}
			}
		}
	}

	private void GoToPillage()
	{
		returning = false;
		
		for (int i = 0; i < unitsInCamp.Count; i++)
		{
			unitsInCamp[i].isMarching = false;
			Vector3Int cityLoc = unitsInCamp[i].marchPosition + moveToLoc;
			unitsInCamp[i].finalDestinationLoc = cityLoc;
			List<Vector3Int> path = new() { cityLoc };
			unitsInCamp[i].MoveThroughPath(path);
		}
	}

	public IEnumerator Pillage()
	{
		List<Laborer> tempLaborerList = new(world.laborerList);
		for (int i = 0; i < tempLaborerList.Count; i++)
		{
			Vector3Int loc = world.GetClosestTerrainLoc(tempLaborerList[i].transform.position);
			if (loc == moveToLoc)
				tempLaborerList[i].KillUnit(moveToLoc - tempLaborerList[i].transform.position);
			else if (loc == threatLoc)
				tempLaborerList[i].KillUnit(threatLoc - tempLaborerList[i].transform.position);
		}

		List<Trader> tempTraderList = new(world.traderList);
		for (int i = 0; i < tempTraderList.Count; i++)
		{
			Vector3Int loc = world.GetClosestTerrainLoc(tempTraderList[i].transform.position);
			if (loc == moveToLoc)
				tempTraderList[i].KillUnit(moveToLoc - tempTraderList[i].transform.position);
			else if (loc == threatLoc)
				tempTraderList[i].KillUnit(threatLoc - tempTraderList[i].transform.position);
		}

		enemyReady = 0;
		Vector3[] splashLocs = new Vector3[4] { new Vector3(-1, 0, -1), new Vector3(1, 0, -1), new Vector3(1, 0, 1), new Vector3(-1, 0, 1)};

		//start animations
		for (int i = 0; i < unitsInCamp.Count; i++)
		{
			if (unitsInCamp[i].isDead)
				continue;

			if (unitsInCamp[i].buildDataSO.unitType == UnitType.Cavalry)
				unitsInCamp[i].StartAttackingAnimation();
			else
				unitsInCamp[i].StartPillageAnimation();
		}

		while (pillageTime > 0)
		{
			yield return new WaitForSeconds(1);
			pillageTime--;

			//particle system
			if (world.GetTerrainDataAt(moveToLoc).isHill)
			{
				world.PlayRemoveEruption(moveToLoc);
			}
			else
			{
				for (int i = 0; i < 4; i++)
					world.PlayRemoveEruption(moveToLoc + splashLocs[i]);
			}

			//play sounds and restart animations
			for (int i = 0; i < unitsInCamp.Count; i++)
			{
				if (unitsInCamp[i].isDead)
					continue;

				unitsInCamp[i].PillageSound();

				if (unitsInCamp[i].buildDataSO.unitType == UnitType.Cavalry)
					unitsInCamp[i].StartAttackingAnimation();
			}
		}

		for (int i = 0; i < unitsInCamp.Count; i++)
		{
			if (unitsInCamp[i].isDead)
				continue;

			if (unitsInCamp[i].buildDataSO.unitType == UnitType.Cavalry)
				unitsInCamp[i].StopAttackAnimation();
			else
				unitsInCamp[i].StopPillageAnimation();
		}

		FinishPillage();
	}

	public void FinishPillage()
	{
		if (world.mainPlayer.runningAway)
		{
			world.mainPlayer.StopRunningAway();
			world.mainPlayer.stepAside = false;
		}

		world.GetCity(moveToLoc).attacked = false;
		world.cityDict[moveToLoc].BePillaged(unitsInCamp.Count - deathCount);
		pillage = false;

		returning = true;
		pathToTarget.Reverse();
		MoveOutCamp();
	}

	public void UnitNextStep(bool close, Vector3Int endPositionInt)
	{
		enemyReady++;

		if (enemyReady == campCount - deathCount)
		{
			enemyReady = 0;

			lastSpot = world.GetClosestTerrainLoc(endPositionInt);

			if (close && !attackingArmy.defending)
				world.CityBattleStations(moveToLoc, threatLoc, this);

			world.CheckMainPlayerLoc(lastSpot, pathToTarget);

			if (world.uiCampTooltip.activeStatus && world.uiCampTooltip.enemyCamp == this)
			{
				if (world.uiCampTooltip.army.UpdateArmyCostsMovingTarget(world.uiCampTooltip.army.loc, cityLoc, pathToTarget, lastSpot))
					world.uiCampTooltip.RefreshData();
				else
					world.unitMovement.CancelArmyDeployment();
			}

			if (lastSpot == fieldBattleLoc)
			{
				for (int i = 0; i < unitsInCamp.Count; i++)
				{
					unitsInCamp[i].pathPositions.Clear();
					unitsInCamp[i].StopMovement();
					unitsInCamp[i].isMarching = false;
				}

				forward = (attackingArmy.attackZone - fieldBattleLoc) / 3;
				BattleStations(fieldBattleLoc, forward);
			}
			else
			{
				foreach (Military unit in unitsInCamp)
					unit.readyToMarch = true;
			}
		}
	}

	public void MoveOut(City targetCity)
	{
		fieldBattleLoc = cityLoc;
		attackingArmy = targetCity.army;
		moveToLoc = targetCity.cityLoc;

		List<Vector3Int> avoidList = world.GetNeighborsFor(moveToLoc, MapWorld.State.FOURWAYINCREMENT);
		pathToTarget = GridSearch.TerrainSearchEnemy(world, loc, moveToLoc, avoidList);

		enemyReady = 0;
		pathToTarget.Remove(pathToTarget[pathToTarget.Count - 1]);
		movingOut = true;
		Vector3Int penultimate = pathToTarget[pathToTarget.Count - 1];
		threatLoc = penultimate;

		forward = (moveToLoc - threatLoc) / 3;
		if (world.uiCampTooltip.activeStatus && world.uiCampTooltip.enemyCamp == this)
			world.unitMovement.CancelArmyDeployment();
		
		if (world.unitMovement.deployingArmy)
		{
			for (int i = 0; i < unitsInCamp.Count; i++)
				unitsInCamp[i].SoftSelect(Color.red);
		}

		BattleStations(loc, forward);
	}

	private void RemoveOut()
	{
		inBattle = false;
		attackingArmy = world.GetCity(moveToLoc).army;
		enemyReady = 0;
		forward = forward = (moveToLoc - threatLoc) / 3;

		if (world.unitMovement.deployingArmy)
		{
			for (int i = 0; i < unitsInCamp.Count; i++)
				unitsInCamp[i].SoftSelect(Color.red);
		}

		BattleStations(fieldBattleLoc, forward);
	}

	public void SetCityEnemyCamp()
	{
		totalSpots = new();
		openSpots = new();
		
		foreach (Vector3Int pos in world.GetNeighborsFor(loc, MapWorld.State.EIGHTWAYARMY))
		{
			totalSpots.Add(pos);
			openSpots.Add(pos);
		}
	}

	public void LoadCityEnemyCamp()
	{
		totalSpots = new();
		openSpots = new();

		List<Vector3Int> tempLocs = new();

		for (int i = 0; i < unitsInCamp.Count; i++)
			tempLocs.Add(unitsInCamp[i].barracksBunk);

		foreach (Vector3Int pos in world.GetNeighborsFor(loc, MapWorld.State.EIGHTWAYARMY))
		{
			totalSpots.Add(pos);
			
			if (!tempLocs.Contains(pos))
				openSpots.Add(pos);
		}
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
		campCount++;

		return openSpot;
	}

	//public void RemoveFromCamp(Unit unit)
	//{
	//	if (!growing)
	//		return;
		
	//	unitsInCamp.Remove(unit);
	//	campCount--;
	//	UnitType type = unit.buildDataSO.unitType;

	//	if (type == UnitType.Infantry)
	//		infantryCount--;
	//	else if (type == UnitType.Ranged)
	//		rangedCount--;
	//	else if (type == UnitType.Cavalry)
	//		cavalryCount--;

	//	strength -= unit.buildDataSO.baseAttackStrength;
	//	health -= unit.buildDataSO.health;

	//	int index = totalSpots.IndexOf(unit.military.barracksBunk);
	//	int newIndex = 0;

	//	for (int i = 0; i < index; i++)
	//	{
	//		if (openSpots.Contains(totalSpots[i]))
	//			newIndex++;
	//	}

	//	openSpots.Insert(newIndex, unit.military.barracksBunk);
	//}
}
