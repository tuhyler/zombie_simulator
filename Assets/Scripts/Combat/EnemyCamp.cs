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
	
	public Vector3Int loc, forward, moveToLoc, actualAttackLoc;
	public Vector3Int armyDiff, fieldBattleLoc, lastSpot;

	public GameObject campfire;

	public int enemyReady;
	public int campCount, deathCount, infantryCount, rangedCount, cavalryCount, seigeCount, health, strength, pillageTime;
	public bool revealed, prepping, attacked, attackReady = false, armyReady, inBattle, returning, movingOut, chasing, isCity, pillage, growing, removingOut, atSea, battleAtSea, seaTravel, duel;
	public Army attackingArmy;
	public Military benchedUnit;

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

		if (benchedUnit)
			campList.Add(benchedUnit.SaveMilitaryUnitData());

		campData.enemyReady = enemyReady;
		campData.moveToLoc = moveToLoc;
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
		//campData.chasing = chasing;
		campData.allUnits = campList;
		campData.pillage = pillage;
		campData.pillageTime = pillageTime;
		campData.growing = growing;
		campData.fieldBattleLoc = fieldBattleLoc;
		campData.lastSpot = lastSpot;
		campData.removingOut = removingOut;
		campData.countDownTimer = world.GetEnemyCity(cityLoc).countDownTimer;
		campData.atSea = atSea;
		campData.seaTravel = seaTravel;
		campData.actualAttackLoc = actualAttackLoc;

		return campData;
	}

    public void BattleStations(Vector3Int travelLoc, Vector3Int forward, MilitaryLeader leader = null)
    {
		if (attackReady || prepping)
			return;

		if (unitsInCamp.Count == 0 && !leader)
		{
			attackReady = true;
			return;
		}

		if (leader)
		{
			leader.PrepForBattle();

			Military removedUnit = null;
			foreach (Military unit in unitsInCamp)
			{
				if (unit.barracksBunk == leader.barracksBunk)
				{
					removedUnit = unit;
					break;
				}
			}

			if (removedUnit != null)
			{
				removedUnit.benched = true;
				removedUnit.gameObject.SetActive(false);
				benchedUnit = removedUnit;
				unitsInCamp.Remove(removedUnit);
				world.RemoveUnitPosition(removedUnit.currentLocation);
			}
			else
			{
				campCount++;
			}

			unitsInCamp.Add(leader);
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
				unit.StopMovementCheck(false);
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
				//if (chasing)
				//{
				for (int i = 0; i < unitsInCamp.Count; i++)
					unitsInCamp[i].minimapIcon.gameObject.SetActive(true);

				//	world.mainPlayer.StartRunningAway();
				//	pathToTarget = GridSearch.TerrainSearchEnemy(world, loc, moveToLoc);
				//}

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
			if (attackingArmy != null)
				attackingArmy.inBattle = false;
			inBattle = false;
			enemyReady = 0;
			ResetStatus();
		
			ResurrectCamp();

			if (isCity)
				RestartCityCheck();
		}
	}

	public void Charge()
	{
		inBattle = true;

		foreach (Military unit in unitsInCamp)
		{
			unit.strengthBonus = Mathf.RoundToInt(world.GetTerrainDataAt(unit.currentLocation).terrainData.terrainAttackBonus * 0.01f * unit.attackStrength);

			if (world.CheckIfTileIsImproved(world.GetClosestTerrainLoc(unit.currentLocation)))
				unit.strengthBonus += Mathf.RoundToInt(world.GetCityDevelopment(world.GetClosestTerrainLoc(unit.currentLocation)).GetImprovementData.attackBonus * 0.01f * unit.attackStrength);

			if (unit.isSelected)
				world.unitMovement.infoManager.UpdateStrengthBonus(unit.strengthBonus);

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

	public Military FindClosestTarget(Military unit)
	{
		Military closestEnemy = null;
		float dist = 0;

		if (attackingArmy == null)
			return null;

		List<Military> tempList = new(attackingArmy.UnitsInArmy);
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
	public Military FindEdgeRanged(Vector3Int currentLoc)
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
					return potential.military;

				if (i % 2 == 0)
					skipMiddle = true;
			}

			i++;
		}

		return null;
	}

	public List<Vector3Int> PathToEnemy(Vector3Int pos, Vector3Int target)
	{
		return GridSearch.BattleMove(world, pos, target, attackingArmy.movementRange, attackingArmy.attackingSpots, battleAtSea);
	}

	public List<Vector3Int> CavalryPathToEnemy(Vector3Int pos, Vector3Int target)
	{
		return GridSearch.BattleMove(world, pos, target, attackingArmy.cavalryRange, attackingArmy.attackingSpots, battleAtSea);
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

			inBattle = false;
			world.ToggleCityMaterialClear(isCity ? cityLoc : loc, attackingArmy.city.cityLoc, attackingArmy.enemyTarget, attackingArmy.attackZone, false);

			foreach (Military unit in unitsInCamp)
			{
				unit.enemyAI.AttackCheck();
				unit.StopAttacking();
			}

			if (attackingArmy != null)
			{
				//if (world.cityBuilderManager.uiUnitBuilder.activeStatus)
				//	world.cityBuilderManager.uiUnitBuilder.UpdateBarracksStatus(attackingArmy.isFull);
				attackingArmy.ResetArmy();
				attackingArmy.targetCamp = null;
				attackingArmy.defending = false;
				attackingArmy.battleAtSea = false;
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
			battleAtSea = false;

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
			if (!movingOut && !duel)
			{
				if (isCity && !world.IsEnemyCityOnTile(attackingArmy.enemyTarget)) //in case barracks is attacked instead
					world.RemoveEnemyCamp(cityLoc, isCity);
				else
					world.RemoveEnemyCamp(attackingArmy.enemyTarget, isCity);
			}
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
		movingOut = false;
		returning = false;
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

	public void ToggleClapping(bool v)
	{
		foreach (Military unit in unitsInCamp)
		{
			if ((unit.buildDataSO.unitType == UnitType.Infantry || unit.buildDataSO.unitType == UnitType.Ranged) && !unit.isMoving)
				unit.ToggleClapping(v);
		}
	}

	public void ReturnToCamp()
	{
		enemyReady = 0;
		CheckForSubInBenched();

		foreach (Military unit in unitsInCamp)
		{
			unit.inBattle = false; //leaving it here just in case
			unit.preparingToMoveOut = false;
			unit.isMarching = false;
			unit.duelWatch = false;
			if (!unit.repositioning)
				unit.enemyAI.StartReturn();
		}
	}

	public void FinishDuelCheck()
	{
		if (unitsInCamp.Count == 0)
		{
			RestartCityCheck();
		}
		else if (unitsInCamp[0].duelWatch)
		{
			ToggleClapping(false);
			StopMovement();
			ReturnToCamp();
		}
	}

	public void RestartCityCheck()
	{
		City city = world.GetEnemyCity(cityLoc);
		if (growing)
			city.StartSpawnCycle(false);
		else
			city.LoadSendAttackWait(true);
	}

	public void StopMovement()
	{
		foreach (Unit unit in unitsInCamp)
		{
			//unit.StopAnimation();
			unit.StopMovementCheck(true);
		}
	}

	public void CheckForSubInBenched()
	{
		if (benchedUnit)
		{
			MilitaryLeader leader = world.GetEnemyCity(cityLoc).empire.enemyLeader;
			unitsInCamp.Remove(leader);
			leader.FinishBattle();

			benchedUnit.benched = false;
			benchedUnit.gameObject.SetActive(true);
			world.AddUnitPosition(benchedUnit.currentLocation, benchedUnit);
			unitsInCamp.Add(benchedUnit);
			benchedUnit = null;
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
		
		CheckForSubInBenched();
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
			unit.unitMesh.SetActive(true);
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

	//public void StartChase(Vector3Int loc)
	//{
	//	movingOut = true;
	//	chasing = true;
	//	moveToLoc = loc;
	//	GameLoader.Instance.gameData.movingEnemyBases[this.loc] = new();

	//	//getting closest tile to determine threat loc
	//	bool firstOne = true;
	//	int dist = 0;
	//	Vector3Int closestTile = this.loc;
	//	foreach (Vector3Int tile in world.GetNeighborsFor(this.loc, MapWorld.State.FOURWAYINCREMENT))
	//	{
	//		if (firstOne)
	//		{
	//			firstOne = false;
	//			dist = Mathf.Abs(tile.x - loc.x) + Mathf.Abs(tile.z - loc.z);
	//			closestTile = tile;
	//			continue;
	//		}

	//		int newDist = Mathf.Abs(tile.x - loc.x) + Mathf.Abs(tile.z - loc.z);
	//		if (newDist < dist)
	//		{
	//			dist = newDist;
	//			closestTile = tile;
	//		}
	//	}

	//	threatLoc = closestTile;
	//	forward = (threatLoc - loc) / 3;
	//	BattleStations(this.loc, forward);
	//}

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
			world.CityBattleStations(moveToLoc, actualAttackLoc, threatLoc, this);

		foreach (Military unit in unitsInCamp)
		{
			if (unit.isDead)
				continue;
			
			if (/*!chasing && */!returning)
				unit.isMarching = true;
			
			unit.preparingToMoveOut = false;
			unit.attacking = false;
			unit.enemyAI.AttackCheck();
			List<Vector3Int> path = new();

			foreach (Vector3Int tile in pathToTarget)
				path.Add(tile + unit.marchPosition);

			//if (returning)
			//	path.Add(unit.barracksBunk);

			unit.StopMovementCheck(false);

			//if (!chasing)
			//	unit.isMarching = true;

			if (path.Count > 0)
			{
				unit.finalDestinationLoc = path[path.Count - 1];
				unit.MoveThroughPath(path);
			}
			else
			{
				unit.FinishMoving(unit.transform.position);
			}
		}
	}

	private void RemoveOutCamp()
	{
		foreach (Military unit in unitsInCamp)
		{
			unit.isMarching = true;
			unit.preparingToMoveOut = false;
			unit.attacking = false;
			unit.enemyAI.AttackCheck();
			List<Vector3Int> path = new();

			int start = pathToTarget.IndexOf(fieldBattleLoc);
			for (int i = start + 1; i < pathToTarget.Count; i++)
				path.Add(pathToTarget[i] + unit.marchPosition);

			unit.StopMovementCheck(false);

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
			
			/*if (chasing)
			{
				chasing = false;

				for (int i = 0; i < unitsInCamp.Count; i++)
				{
					unitsInCamp[i].looking = true;
					unitsInCamp[i].StartLookingAround();
				}
			}
			else */if (returning)
			{
				if (isCity)
				{
					if (world.GetEnemyCity(cityLoc).empire.enemyLeader.dueling)
					{
						PrepForDuel();
						return;
					}
				}
				else
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
					world.uiAttackWarning.AttackNotification(((Vector3)attackingArmy.attackZone + attackingArmy.enemyTarget) * 0.5f);
					GoToPillage();
					pillage = true;
				}
				else
				{
					attackReady = true;
					
					if (armyReady)
					{
						world.uiAttackWarning.AttackNotification(((Vector3)attackingArmy.attackZone + attackingArmy.enemyTarget) * 0.5f);
						attackingArmy.Charge();
					}
				}
			}
		}
	}

	private void GoToPillage()
	{
		returning = false;
		
		if (actualAttackLoc != moveToLoc)
		{
			Vector3Int originalAttackLoc;
			if (pathToTarget.Count > 0)
				originalAttackLoc = pathToTarget[pathToTarget.Count - 1];
			else
				originalAttackLoc = loc;

			List<Vector3Int> restOfPath = GridSearch.MoveWherever(world, originalAttackLoc, moveToLoc);
			pathToTarget.AddRange(restOfPath);

			for (int i = 0; i < unitsInCamp.Count; i++)
			{
				unitsInCamp[i].isMarching = false;
				List<Vector3Int> path = new();

				foreach (Vector3Int tile in restOfPath)
					path.Add(tile + unitsInCamp[i].marchPosition);

				if (path.Count > 0)
				{
					unitsInCamp[i].finalDestinationLoc = path[path.Count - 1];
					unitsInCamp[i].MoveThroughPath(path);
				}
			}
		}
		else
		{
			for (int i = 0; i < unitsInCamp.Count; i++)
			{
				unitsInCamp[i].isMarching = false;
				Vector3Int cityLoc = unitsInCamp[i].marchPosition + moveToLoc;
				unitsInCamp[i].finalDestinationLoc = cityLoc;
				List<Vector3Int> path = new() { cityLoc };
				unitsInCamp[i].MoveThroughPath(path);
			}
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
				world.CityBattleStations(moveToLoc, actualAttackLoc, threatLoc, this);

			world.CheckMainPlayerLoc(lastSpot, pathToTarget);

			if (seaTravel)
			{
				if (atSea)
				{
					Vector3Int nextSpot;
					if (unitsInCamp[0].pathPositions.Count == 0)
						nextSpot = moveToLoc;
					else
						nextSpot = unitsInCamp[0].pathPositions.Peek();

					if (world.GetTerrainDataAt(nextSpot).isLand)
					{
						atSea = false;
						for (int i = 0; i < unitsInCamp.Count; i++)
							unitsInCamp[i].ToggleBoat(false);
					}
				}
				else
				{
					if (unitsInCamp[0].pathPositions.Count > 0)
					{
						if (!world.GetTerrainDataAt(endPositionInt).isLand && !world.GetTerrainDataAt(unitsInCamp[0].pathPositions.Peek()).isLand)
						{
							atSea = true;
							for (int i = 0; i < unitsInCamp.Count; i++)				
								unitsInCamp[i].ToggleBoat(true);
						}
					}
				}
			}

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
					unitsInCamp[i].StopMovementCheck(true);
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

	public bool MoveOut(City targetCity)
	{
		moveToLoc = targetCity.cityLoc;
		seaTravel = false;

		pathToTarget = GridSearch.TerrainSearchEnemy(world, loc, moveToLoc);

		if (pathToTarget.Count == 0)
		{
			List<Vector3Int> directSeaList = new(), outerRingList = new();
			//Checking if target is by sea
			List<Vector3Int> surroundingArea = world.GetNeighborsFor(moveToLoc, MapWorld.State.CITYRADIUS);
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
			//first check those in inner circle
			if (!hasRoute && directSeaList.Count > 0)
			{
				chosenPath = world.GetSeaLandRoute(directSeaList, world.GetEnemyCity(cityLoc).harborLocation, moveToLoc, true);

				if (chosenPath.Count > 0)
					hasRoute = true;
			}

			//outer ring next
			if (!hasRoute && outerRingList.Count > 0)
			{
				chosenPath = world.GetSeaLandRoute(outerRingList, world.GetEnemyCity(cityLoc).harborLocation, moveToLoc, true);

				if (chosenPath.Count > 0)
					hasRoute = true;
			}

			if (hasRoute)
			{
				seaTravel = true;
				pathToTarget = GridSearch.TerrainSearchEnemy(world, loc, world.GetEnemyCity(cityLoc).harborLocation, true);
				pathToTarget.AddRange(chosenPath);
			}
		}

		if (pathToTarget.Count > 0)
		{
			//finding best spot to attack from
			pathToTarget = FindOptimalAttackZone(pathToTarget, moveToLoc, seaTravel, false);

			if (pathToTarget.Count == 0)
				return false;

			fieldBattleLoc = cityLoc;
			attackingArmy = targetCity.army;
			enemyReady = 0;
			pathToTarget.Remove(pathToTarget[pathToTarget.Count - 1]);
			movingOut = true;
			Vector3Int penultimate;

			if (pathToTarget.Count > 0)
				penultimate = pathToTarget[pathToTarget.Count - 1];
			else
				penultimate = loc;

			threatLoc = penultimate;

			forward = (actualAttackLoc - threatLoc) / 3;
			if (world.uiCampTooltip.activeStatus && world.uiCampTooltip.army == attackingArmy)
				world.unitMovement.CancelArmyDeployment();
		
			if (world.unitMovement.deployingArmy)
			{
				for (int i = 0; i < unitsInCamp.Count; i++)
					unitsInCamp[i].SoftSelect(Color.red);
			}
			
			BattleStations(loc, forward);
			return true;
		}
		else
		{
			return false;
		}
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

	public List<Vector3Int> FindOptimalAttackZone(List<Vector3Int> currentPathBase, Vector3Int targetBase, bool bySea, bool justCity)
	{
		List<Vector3Int> currentPath = new(currentPathBase);
		Vector3Int target = targetBase;
		
		Vector3Int newStart;
		if (currentPath.Count < 4)
		{
			newStart = currentPath[0];
			currentPath.Clear();
		}
		else
		{
			newStart = currentPath[currentPath.Count - 4];

			//removing last 3
			for (int i = 0; i < 3; i++)
				currentPath.RemoveAt(currentPath.Count - 1);
		}

		//seeing if it would be closer to attack barracks than the city
		if (world.GetCity(target).hasBarracks && !justCity)
		{
			Vector3Int barracksLoc = world.GetCity(target).barracksLocation;

			int cityDiff = Math.Abs(newStart.x - target.x) + Math.Abs(newStart.z - target.z);
			int barracksDiff = Math.Abs(newStart.x - barracksLoc.x) + Math.Abs(newStart.z - barracksLoc.z);

			if (barracksDiff < cityDiff)
			{
				target = barracksLoc;

				if (barracksDiff <= 6)
				{
					if (currentPath.Count == 0)
					{
						newStart = loc;
						currentPath.Clear();
					}
					else
					{
						newStart = currentPath[currentPath.Count - 2];

						//removing last 1
						for (int i = 0; i < 1; i++)
							currentPath.RemoveAt(currentPath.Count - 1);
					}
				}
			}
		}

		Vector3Int diff = newStart - target;

		int[] tilesToCheckArray = new int[4] { 1, 1, 1, 1 };
		int absX = Math.Abs(diff.x);
		int absZ = Math.Abs(diff.z);

		if (absX > absZ)
		{
			if (diff.x > 0)
				tilesToCheckArray[3] = 0;
			else
				tilesToCheckArray[1] = 0;
		}
		else if (absX < absZ)
		{
			if (diff.z > 0)
				tilesToCheckArray[2] = 0;
			else
				tilesToCheckArray[0] = 0;
		}
		else
		{
			if (diff.z > 0 && diff.x > 0)
			{
				tilesToCheckArray[2] = 0;
				tilesToCheckArray[3] = 0;
			}
			else if (diff.z < 0 && diff.x > 0)
			{
				tilesToCheckArray[0] = 0;
				tilesToCheckArray[3] = 0;
			}
			else if (diff.z < 0 && diff.x < 0)
			{
				tilesToCheckArray[0] = 0;
				tilesToCheckArray[1] = 0;
			}
			else
			{
				tilesToCheckArray[1] = 0;
				tilesToCheckArray[2] = 0;
			}
		}

		List<Vector3Int> fourWayTiles = world.GetNeighborsFor(target, MapWorld.State.FOURWAYINCREMENT);
		List<Vector3Int> tilesToCheckLoc = new();
		List<(int, int)> tilesData = new();
		//List<int> tilesDist = new();
		for (int i = 0; i < tilesToCheckArray.Length; i++)
		{
			//getting info first, then sorting
			if (tilesToCheckArray[i] == 1)
			{
				TerrainData td = world.GetTerrainDataAt(fourWayTiles[i]);
				if (td.isDiscovered)
				{
					tilesToCheckLoc.Add(fourWayTiles[i]);
					tilesData.Add((td.terrainData.terrainAttackBonus, Math.Abs(fourWayTiles[i].x - newStart.x) + Math.Abs(fourWayTiles[i].z - newStart.z)));
					//tilesDist.Add(Math.Abs(fourWayTiles[i].x - newStart.x) + Math.Abs(fourWayTiles[i].z - newStart.z)); 
				}
			}
		}

		//sorting by priority
		int loopCount = tilesToCheckLoc.Count;
		for (int i = 0; i < loopCount; i++)
		{
			for (int j = i + 1; j < loopCount; j++)
			{
				if (tilesData[j].Item1 > tilesData[i].Item1)
				{
					Vector3Int tile = tilesToCheckLoc[j];
					(int, int) datum = tilesData[j];
					tilesToCheckLoc.RemoveAt(j);
					tilesData.RemoveAt(j);
					tilesToCheckLoc.Insert(i, tile);
					tilesData.Insert(i, datum);
				}
				else if (tilesData[j].Item1 == tilesData[i].Item1 && tilesData[j].Item2 < tilesData[i].Item2)
				{
					Vector3Int tile = tilesToCheckLoc[j];
					(int, int) datum = tilesData[j];
					tilesToCheckLoc.RemoveAt(j);
					tilesData.RemoveAt(j);
					tilesToCheckLoc.Insert(i, tile);
					tilesData.Insert(i, datum);
				}
			}
		}

		bool seaStart = false;
		if (bySea && !world.GetTerrainDataAt(newStart).isLand)
			seaStart = true;

		bool foundPath = false;
		List<Vector3Int> avoidList = new(tilesToCheckLoc) { target };
		for (int i = 0; i < tilesToCheckLoc.Count; i++)
		{
			if (tilesToCheckLoc[i] == newStart)
			{
				currentPath.Add(target);
				foundPath = true;
				break;
			}
			
			List<Vector3Int> pathCoda = GridSearch.TerrainSearchEnemyCoda(world, newStart, tilesToCheckLoc[i], avoidList, seaStart);

			if (pathCoda.Count > 0)
			{
				currentPath.AddRange(pathCoda);
				currentPath.Add(target);
				foundPath = true;
				break;
			}
		}

		if (!foundPath)
		{
			List<Vector3Int> lastPath = FindOptimalAttackZone(currentPathBase, targetBase, bySea, true);
			currentPath.AddRange(lastPath);
			currentPath.Add(target);
		}

		actualAttackLoc = target;
		return currentPath;
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

	public void PrepForDuel()
	{
		foreach (Military unit in unitsInCamp)
		{
			Vector3 diff = unit.barracksBunk - loc;
			Vector3 newLoc;

			if (diff.sqrMagnitude == 0)
			{
				newLoc = unit.barracksBunk + new Vector3(-0.75f, 0, 1.5f);
			}
			else
			{
				if (diff.x != 0)
					diff.x /= 2f;
				if (diff.z != 0)
					diff.z /= 2f;
				
				newLoc = unit.barracksBunk + diff;
			}

			Vector3Int newLocInt = world.RoundToInt(newLoc);

			List<Vector3Int> path = GridSearch.MoveWherever(world, unit.transform.position, newLocInt);

			unit.duelWatch = true;
			if (path.Count == 0)
				path.Add(unit.barracksBunk);
			unit.finalDestinationLoc = newLoc;
			unit.MoveThroughPath(path);
		}
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
