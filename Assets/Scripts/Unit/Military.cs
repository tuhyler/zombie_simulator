using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static UnityEngine.GraphicsBuffer;

public class Military : Unit
{
	[SerializeField]
	public GameObject boatMesh;

	[SerializeField]
	private MeshFilter boatSail;
	
	private int isPillagingHash, isDiscoveredHash, isSittingHash;
	[HideInInspector]
	public Vector3Int targetLocation, targetBunk, barracksBunk, marchPosition; //targetLocation is in case units overlap on same tile

	[HideInInspector]
	public Coroutine attackCo, waitingCo;
	[HideInInspector]
	public int idleTime, attackStrength, strengthBonus;

	[HideInInspector]
	public bool atHome, preparingToMoveOut, isMarching, transferring, repositioning, inBattle, attacking, targetSearching, flanking, 
		flankedOnce, cavalryLine, looking, aoe, guard, isGuarding, returning, atSea;

	[HideInInspector]
	public City homeBase;
	[HideInInspector]
	public EnemyCamp enemyCamp;
	[HideInInspector]
	public EnemyAmbush enemyAmbush;

	[HideInInspector]
	public Projectile projectile;

	[HideInInspector]
	public Trader guardedTrader;


	private void Awake()
	{
		AwakeMethods();
		attackStrength = buildDataSO.baseAttackStrength;

		if (GetComponent<NPC>() == null)
		{
			military = GetComponent<Military>();

			int factor = 0;
			if (enemyAI)
				factor = 4;
			else if (buildDataSO.unitType == UnitType.Ranged)
				factor = 1;
			else if (buildDataSO.unitType == UnitType.Cavalry)
				factor = 2;
			else if (buildDataSO.unitType == UnitType.Seige)
				factor = 3;

			if (factor > 0)
			{
				float shift = 0.03125f * factor;
				Vector2[] sailUV = boatSail.mesh.uv;
				for (int i = 0; i < sailUV.Length; i++)
					sailUV[i].x += shift;

				boatSail.mesh.uv = sailUV;
			}

			if (buildDataSO.unitType == UnitType.Ranged)
			{
				projectile = GetComponentInChildren<Projectile>();
				projectile.SetProjectilePos();
				projectile.gameObject.SetActive(false);
			}

			isPillagingHash = Animator.StringToHash("isPillaging");
			isDiscoveredHash = Animator.StringToHash("isDiscovered");
			isSittingHash = Animator.StringToHash("isSitting");
		}
	}

	protected override void AwakeMethods()
	{
		base.AwakeMethods();
	}

	private void Start()
	{
		if (!atSea && boatMesh != null)
			boatMesh.SetActive(false);
	}

	public void StartPillageAnimation()
	{
		unitAnimator.SetBool(isPillagingHash, true);
	}

	public void StopPillageAnimation()
	{
		unitAnimator.SetBool(isPillagingHash, false);
	}

	public void DiscoverSitting()
	{
		unitAnimator.SetBool(isDiscoveredHash, true);
	}

	public void ToggleSitting(bool v)
	{
		if (!v)
			unitAnimator.SetBool(isDiscoveredHash, false);

		unitAnimator.SetBool(isSittingHash, v);
	}

	public void StartAttack(Unit target)
	{
		targetSearching = false;
		Rotate(target.transform.position);
		attackCo = StartCoroutine(Attack(target));
	}

	public IEnumerator Attack(Unit target)
	{
		targetBunk = target.military.barracksBunk;
		attacking = true;

		if (target.military && target.military.targetSearching)
			target.enemyAI.StartAttack(this);

		int wait = UnityEngine.Random.Range(0, 3);
		if (wait != 0)
			yield return attackPauses[wait];

		while (target.currentHealth > 0)
		{
			transform.rotation = Quaternion.LookRotation(target.transform.position - transform.position);
			StartAttackingAnimation();
			yield return attackPauses[2];
			target.ReduceHealth(this, attacks[UnityEngine.Random.Range(0, attacks.Length)]);
			yield return attackPauses[0];
		}

		attacking = false;
		attackCo = null;

		if (ambush)
		{
			ambush = false;
		}
		else
		{
			StopMovement();
			StopAnimation();
			AggroCheck();
		}
	}

	public void StopAttack()
	{
		if (attackCo != null)
			StopCoroutine(attackCo);

		attackCo = null;
	}

	private IEnumerator RangedAttack(Unit target)
	{
		targetBunk = target.military.barracksBunk;
		attacking = true;
		Rotate(target.transform.position);

		int wait = UnityEngine.Random.Range(0, 3);
		if (wait != 0)
			yield return attackPauses[wait];

		while (target.currentHealth > 0)
		{
			transform.rotation = Quaternion.LookRotation(target.transform.position - transform.position);
			StartAttackingAnimation();
			yield return attackPauses[2];
			projectile.SetPoints(transform.position, target.transform.position);
			StartCoroutine(projectile.Shoot(this, target));
			yield return attackPauses[0];
		}

		attackCo = null;
		attacking = false;

		if (ambush)
		{
			ambush = false;
		}
		else
		{
			StopAnimation();
			AggroCheck();
		}
	}

	public void AggroCheck()
	{
		UnitType type = buildDataSO.unitType;
		if (attacking)
			return;

		if (!homeBase.army.FinishAttack())
		{
			if (type == UnitType.Infantry)
				InfantryAggroCheck();
			else if (type == UnitType.Ranged)
				RangedAggroCheck();
			else if (type == UnitType.Cavalry)
				CavalryAggroCheck();
		}
	}

	public void InfantryAggroCheck()
	{
		targetSearching = false;

		List<Vector3Int> attackingZones = new();

		Vector3Int forward = homeBase.army.forward;
		Vector3Int forwardTile = forward + currentLocation;

		attackingZones.Add(forwardTile);
		if (forward.z != 0)
		{
			attackingZones.Add(new Vector3Int(1, 0, forward.z) + currentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, forward.z) + currentLocation);
			attackingZones.Add(new Vector3Int(1, 0, 0) + currentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, 0) + currentLocation);
		}
		else
		{
			attackingZones.Add(new Vector3Int(forward.x, 0, 1) + currentLocation);
			attackingZones.Add(new Vector3Int(forward.x, 0, -1) + currentLocation);
			attackingZones.Add(new Vector3Int(0, 0, 1) + currentLocation);
			attackingZones.Add(new Vector3Int(0, 0, -1) + currentLocation);
		}

		foreach (Vector3Int zone in attackingZones)
		{
			if (!homeBase.army.movementRange.Contains(zone))
				continue;

			if (world.IsUnitLocationTaken(zone))
			{
				Unit enemy = world.GetUnit(zone);
				if (enemy.enemyAI)
				{
					//attacking = true;
					if (!homeBase.army.attackingSpots.Contains(currentLocation))
						homeBase.army.attackingSpots.Add(currentLocation);

					if (!attacking)
					{
						StartAttack(enemy);
					}
					else
					{
						if (enemy.military.targetSearching)
							enemy.enemyAI.StartAttack(this);
					}
				}
			}
		}

		if (attacking)
			return;

		Unit newEnemy = homeBase.army.FindClosestTarget(this);
		targetLocation = newEnemy.currentLocation;
		List<Vector3Int> path = homeBase.army.PathToEnemy(currentLocation, world.RoundToInt(newEnemy.transform.position));

		if (path.Count > 0)
		{
			homeBase.army.attackingSpots.Remove(currentLocation);

			//moving unit behind if stuck
			Vector3Int positionBehind = homeBase.army.forward * -1 + currentLocation;

			if (world.IsUnitLocationTaken(positionBehind))
			{
				Unit unitBehind = world.GetUnit(positionBehind);
				if (unitBehind.inArmy && unitBehind.military.targetSearching)
					unitBehind.military.AggroCheck();
			}

			if (path.Count >= 2)
			{
				List<Vector3Int> shortPath = new() { path[0] };
				finalDestinationLoc = shortPath[0];
				MoveThroughPath(shortPath);
				homeBase.army.attackingSpots.Add(path[0]);
			}
			else if (path.Count == 1)
				StartAttack(newEnemy);
		}
		else
		{
			Vector3Int scoochCloser = new Vector3Int(Math.Sign(targetLocation.x - currentLocation.x), 0, Math.Sign(targetLocation.z - currentLocation.z)) + currentLocation;

			if (homeBase.army.movementRange.Contains(scoochCloser) && !world.IsUnitLocationTaken(scoochCloser) && !homeBase.army.attackingSpots.Contains(scoochCloser))
			{
				finalDestinationLoc = scoochCloser;
				//List<Vector3Int> newPath = new() { scoochCloser };
				MoveThroughPath(new List<Vector3Int> { scoochCloser });
			}

			targetSearching = true;
		}
	}

	public void RangedAggroCheck()
	{
		Unit enemy = homeBase.army.FindClosestTarget(this);

		if (enemy != null)
			attackCo = StartCoroutine(RangedAttack(enemy));
	}

	public void RangedAmbushCheck(Unit enemy)
	{
		attackCo = StartCoroutine(RangedAttack(enemy));
	}

	public void CavalryAggroCheck()
	{
		targetSearching = false;

		List<Vector3Int> attackingZones = new();

		Vector3Int forward = homeBase.army.forward;
		Vector3Int forwardTile = forward + currentLocation;

		attackingZones.Add(forwardTile);
		if (forward.z != 0)
		{
			attackingZones.Add(new Vector3Int(1, 0, forward.z) + currentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, forward.z) + currentLocation);
			attackingZones.Add(new Vector3Int(1, 0, 0) + currentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, 0) + currentLocation);
		}
		else
		{
			attackingZones.Add(new Vector3Int(forward.x, 0, 1) + currentLocation);
			attackingZones.Add(new Vector3Int(forward.x, 0, -1) + currentLocation);
			attackingZones.Add(new Vector3Int(0, 0, 1) + currentLocation);
			attackingZones.Add(new Vector3Int(0, 0, -1) + currentLocation);
		}

		foreach (Vector3Int zone in attackingZones)
		{
			if (!homeBase.army.cavalryRange.Contains(zone))
				continue;

			if (world.IsUnitLocationTaken(zone))
			{
				Unit enemy = world.GetUnit(zone);
				if (enemy.enemyAI)
				{
					//attacking = true;
					if (!homeBase.army.attackingSpots.Contains(currentLocation))
						homeBase.army.attackingSpots.Add(currentLocation);

					if (!attacking)
					{
						if (flanking) //check for those sleeping if they can attack
						{
							foreach (Military unit in homeBase.army.targetCamp.UnitsInCamp)
							{
								if (unit.targetSearching)
									unit.enemyAI.AggroCheck();
							}
						}

						flankedOnce = true; //can't flank if attacking front lines
						flanking = false;
						StartAttack(enemy);
					}
					else
					{
						if (enemy.military.targetSearching)
							enemy.enemyAI.StartAttack(this);
					}
				}
			}
		}

		if (attacking)
			return;

		Unit newEnemy = null;

		if (!flankedOnce) //only one flank per battle
		{
			flankedOnce = true;

			if ((world.IsUnitLocationTaken(forwardTile) && world.GetUnit(forwardTile).inArmy) || cavalryLine)
			{
				cavalryLine = false; //for subsequent battles
				newEnemy = homeBase.army.FindEdgeRanged(currentLocation);
			}
		}
		else
		{
			flanking = false;
		}

		if (newEnemy == null)
			newEnemy = homeBase.army.FindClosestTarget(this);
		else
			flanking = true;

		targetLocation = newEnemy.currentLocation;
		List<Vector3Int> path = homeBase.army.CavalryPathToEnemy(currentLocation, world.RoundToInt(newEnemy.transform.position));

		if (path.Count > 0)
		{
			homeBase.army.attackingSpots.Remove(currentLocation);

			//moving unit behind if stuck
			Vector3Int positionBehind = homeBase.army.forward * -1 + currentLocation;

			if (world.IsUnitLocationTaken(positionBehind))
			{
				Unit unitBehind = world.GetUnit(positionBehind);
				if (unitBehind.inArmy && unitBehind.military.targetSearching)
					unitBehind.military.AggroCheck();
				else if (unitBehind.inArmy && unitBehind.buildDataSO.unitType == UnitType.Cavalry && !unitBehind.military.flankedOnce)
					unitBehind.military.cavalryLine = true;
			}

			if (flanking)
			{
				if (path.Count > 1)
					path.RemoveAt(path.Count - 1); //remove last one
				finalDestinationLoc = path[path.Count - 1];
				MoveThroughPath(path);
			}
			else if (path.Count >= 2)
			{
				List<Vector3Int> shortPath = new() { path[0] };
				finalDestinationLoc = shortPath[0];
				MoveThroughPath(shortPath);
				homeBase.army.attackingSpots.Add(path[0]);
			}
			else if (path.Count == 1)
				StartAttack(newEnemy);
		}
		else
		{
			Vector3Int scoochCloser = new Vector3Int(Math.Sign(targetLocation.x - currentLocation.x), 0, Math.Sign(targetLocation.z - currentLocation.z)) + currentLocation;

			if (homeBase.army.movementRange.Contains(scoochCloser) && !world.IsUnitLocationTaken(scoochCloser) && !homeBase.army.attackingSpots.Contains(scoochCloser))
			{
				finalDestinationLoc = scoochCloser;
				//List<Vector3Int> newPath = new() { scoochCloser };
				MoveThroughPath(new List<Vector3Int> { scoochCloser });
			}

			targetSearching = true;
		}
	}

	public void StopAttacking()
	{
		if (inArmy && attackCo != null)
			StopCoroutine(attackCo);

		strengthBonus = 0;
		if (isSelected)
			world.unitMovement.infoManager.UpdateStrengthBonus(strengthBonus);

		attackCo = null;
		attacking = false;
		inBattle = false;
		flankedOnce = false;
		flanking = false;
		targetSearching = false;
		isMarching = false;
		StopMovement();
		StopAnimation();
	}

	public void StartReturn()
	{
		if (isDead)
			return;

		//unit.StopAttacking();
		AttackCheck();

		attackCo = null;
		inBattle = false;
		attacking = false;
		targetSearching = false;
		flanking = false;
		flankedOnce = false;
		returning = true;

		if (isMoving)
		{
			StopAnimation();
			ShiftMovement();
		}

		finalDestinationLoc = barracksBunk;
		List<Vector3Int> path;

		if (homeBase.army.battleAtSea)
			path = GridSearch.MoveWherever(world, world.RoundToInt(transform.position), barracksBunk);
		else
			path = GridSearch.AStarSearch(world, world.RoundToInt(transform.position), barracksBunk, false, bySea);

		if (path.Count == 0)
			path = GridSearch.MoveWherever(world, world.RoundToInt(transform.position), barracksBunk);

		if (path.Count > 0)
		{
			MoveThroughPath(path);
		}
		else
		{
			FinishMovementMilitary(currentLocation);
		}
	}

	public void AttackCheck()
	{
		if (attackCo != null)
		{
			StopCoroutine(attackCo);
			attackCo = null;
		}
	}

	public void AmbushAggro(Vector3Int endPosition, Vector3Int ambushLoc)
	{
		List<Vector3Int> avoidList = new() { ambushLoc };
		List<Vector3Int> path = GridSearch.AStarSearchEnemy(world, transform.position, endPosition, bySea, avoidList); //enemy works in this case

		if (path.Count > 1)
		{
			finalDestinationLoc = path[path.Count - 1];
			MoveThroughPath(path);
		}
		else if (path.Count == 1)
		{
			StartAttack(world.GetUnit(path[0]));
		}
	}

	public void ContinueGuarding(Queue<Vector3Int> traderPath, Vector3Int currentLoc)
	{
		isGuarding = true;
		StopAnimation();
		if (currentHealth < buildDataSO.health)
			healthbar.RegenerateHealth();
		List<Vector3Int> path = traderPath.ToList();
		path.Insert(0, currentLoc);
		path.RemoveAt(path.Count - 1);
		finalDestinationLoc = path[path.Count - 1];
		MoveThroughPath(path);
	}

	//trader guard stepping to side when arriving
	public Vector3 GuardRouteFinish(Vector3Int finalTraderSpot, Vector3Int finalSpot)
	{
		Vector3Int diff = finalSpot - finalTraderSpot;
		Vector3 target = finalSpot;

		if (diff.x != 0)
		{
			if (diff.z != 0)
			{
				if (diff.x > 0)
					target.x -= 0.75f;
				else
					target.x += 0.75f;
			}
			else
			{
				target.z += 0.75f;
			}
		}
		else
		{
			target.x += 0.75f;
		}

		return target;
	}

	public void GoToBunk()
	{
		finalDestinationLoc = barracksBunk;
		MoveThroughPath(GridSearch.AStarSearch(world, currentLocation, barracksBunk, isTrader, bySea));
	}

	public void FindNewBattleSpot(Vector3Int current, Vector3Int target)
	{
		Army army;

		if (inArmy)
			army = homeBase.army;
		else
			army = enemyCamp.attackingArmy;

		Vector3Int closestTile = current;
		float dist = 0;
		int i = 0;
		UnitType type = buildDataSO.unitType;

		foreach (Vector3Int tile in world.GetNeighborsFor(target, MapWorld.State.EIGHTWAY))
		{
			if (type == UnitType.Cavalry)
			{
				if (!army.cavalryRange.Contains(tile))
					continue;
			}
			else
			{
				if (!army.movementRange.Contains(tile))
					continue;
			}

			if (!world.CheckIfPositionIsValid(tile) || world.IsUnitLocationTaken(tile) || army.attackingSpots.Contains(tile))
				continue;

			if (i == 0)
			{
				i++;
				closestTile = tile;
				dist = Math.Abs(current.x - tile.x) + Math.Abs(current.z - tile.z);

				if (dist == 1)
					break;
				continue;
			}

			float newDist = Math.Abs(current.x - tile.x) + Math.Abs(current.z - tile.z);
			if (newDist < dist)
			{
				closestTile = tile;
				dist = newDist;
				if (dist == 1)
					break;
			}
		}

		if (dist <= 3f)
		{
			finalDestinationLoc = closestTile;
			MoveThroughPath(new List<Vector3Int> { closestTile });
		}
	}

	public void StartLookingAround()
	{
		StartCoroutine(WaitAndScan());
	}

	private IEnumerator WaitAndScan()
	{
		int lookAroundCount = 0;

		while (lookAroundCount < 5)
		{
			yield return new WaitForSeconds(1);
			lookAroundCount++;

			//randomly looking around
			Rotate(world.GetNeighborsFor(currentLocation, MapWorld.State.EIGHTWAY)[UnityEngine.Random.Range(0, 8)]);
		}

		looking = false;
		enemyCamp.FinishMoveOut();
	}

	public void GuardGetInLine(Vector3Int traderPrevSpot, Vector3Int traderSpot)
	{
		StopAnimation();
		ShiftMovement();

		Vector3 sideSpot = GuardRouteFinish(traderSpot, traderPrevSpot);
		finalDestinationLoc = sideSpot;
		StartAnimation();
		RestartPath(traderPrevSpot);
	}

	public IEnumerator DramaticallyDisappear()
	{
		int lookAroundCount = 0;

		while (lookAroundCount < 2)
		{
			yield return new WaitForSeconds(1);
			lookAroundCount++;

			//randomly looking around
			Rotate(world.GetNeighborsFor(currentLocation, MapWorld.State.EIGHTWAY)[UnityEngine.Random.Range(0, 8)]);
		}

		Vector3 newScale = transform.localScale;
		newScale.y = 0.01f;
		Vector3 lightPosition = transform.position;
		lightPosition.y += 0.01f;
		if (world.IsRoadOnTileLocation(world.RoundToInt(transform.position)))
			lightPosition.y += .1f;
		lightBeam.transform.position = lightPosition;
		lightBeam.Play();
		LeanTween.scale(gameObject, Vector3.zero, 0.5f).setEase(LeanTweenType.easeOutBack).setOnComplete(DestroyUnit);
	}

	public void IdleCheck()
	{
		if (!isMoving && waitingCo == null)
			waitingCo = StartCoroutine(IdleTimer(false));
	}

	private IEnumerator IdleTimer(bool load)
	{
		if (!load)
			idleTime = 10;

		while (idleTime > 0)
		{
			yield return attackPauses[0];
			idleTime--;
		}

		waitingCo = null;
		ReturnToClosestCityBarracks();
	}

	public void ReturnToClosestCityBarracks()
	{
		if (world.uiTradeRouteBeginTooltip.activeStatus && world.uiTradeRouteBeginTooltip.trader == guardedTrader)
		{
			waitingCo = StartCoroutine(IdleTimer(false));
			return;
		}

		City closestCity = null;
		int dist = 0;
		bool firstOne = true;

		foreach (City city in world.cityDict.Values)
		{
			if (!city.hasBarracks || city.army.isFull)
				continue;

			if (firstOne)
			{
				closestCity = city;
				dist = Math.Abs(currentLocation.x - city.cityLoc.x) + Math.Abs(currentLocation.z - city.cityLoc.z);
				firstOne = false;
				continue;
			}

			int newDist = Math.Abs(currentLocation.x - city.cityLoc.x) + Math.Abs(currentLocation.z - city.cityLoc.z);
			if (newDist < dist)
			{
				closestCity = city;
				dist = newDist;
			}
		}

		if (!guardedTrader.followingRoute && !guardedTrader.isMoving) //just in case
		{
			if (closestCity != null && !closestCity.attacked && closestCity.army.atHome)
			{
				guardedTrader.SetGuardLeftMessage();
				Vector3Int newLoc = closestCity.army.GetAvailablePosition(buildDataSO.unitType);
				List<Vector3Int> path = GridSearch.AStarSearch(world, transform.position, newLoc, false, bySea);
				world.unitMovement.TransferMilitaryUnit(this, closestCity, newLoc, path);
			}
			else
			{
				waitingCo = StartCoroutine(IdleTimer(false));
			}
		}
	}

	public void ToggleIdleTimer(bool v)
	{
		if (v)
		{
			waitingCo = StartCoroutine(IdleTimer(false));
		}
		else
		{
			if (waitingCo != null)
				StopCoroutine(waitingCo);

			waitingCo = null;
		}
	}

	public void PillageSound()
	{
		audioSource.clip = attacks[UnityEngine.Random.Range(0, attacks.Length)];
		audioSource.Play();
	}

	public void ToggleBoat(bool v)
	{
		Vector3 lightBeamLoc = transform.position;
		lightBeamLoc.y += .01f;
		lightBeam.transform.position = lightBeamLoc;
		lightBeam.Play();

		GameObject go = v ? boatMesh : unitMesh;
		Vector3 unitScale = go.transform.localScale;
		float scaleX = unitScale.x;
		float scaleZ = unitScale.z;
		go.transform.localScale = new Vector3(scaleX, 0.1f, scaleZ);
		LeanTween.scale(go, unitScale, 0.5f).setEase(LeanTweenType.easeOutBack);

		unitMesh.SetActive(!v);
		boatMesh.SetActive(v);
		ripples.SetActive(v);
		atSea = v;
	}

	public void FinishMovementMilitary(Vector3 endPosition)
	{
		if (inBattle)
		{
			if (world.IsUnitLocationTaken(currentLocation))
				FindNewBattleSpot(currentLocation, targetLocation);
			else
				world.AddUnitPosition(currentLocation, this);

			AggroCheck();
		}
		else if (preparingToMoveOut)
		{
			world.AddUnitPosition(currentLocation, this);
			preparingToMoveOut = false;
			homeBase.army.UnitReady(this);
		}
		else if (isMarching)
		{
			world.AddUnitPosition(currentLocation, this);
			isMarching = false;

			Vector3Int endTerrain = world.GetClosestTerrainLoc(endPosition);
			homeBase.army.UnitArrived(endTerrain);

			if (currentLocation == barracksBunk)
			{
				if (currentHealth < buildDataSO.health)
					healthbar.RegenerateHealth();

				atHome = true;
				marker.ToggleVisibility(false);
				if (isSelected && !world.unitOrders)
					world.unitMovement.ShowIndividualCityButtonsUI();

				Rotate(homeBase.army.GetRandomSpot(barracksBunk));
				return;
			}

			//turning to face enemy
			Vector3Int diff = endTerrain - homeBase.army.enemyTarget;
			if (Math.Abs(diff.x) == 3)
				diff.z = 0;
			else if (Math.Abs(diff.z) == 3)
				diff.x = 0;

			Rotate(endPosition - diff);
		}
		else if (transferring)
		{
			world.AddUnitPosition(currentLocation, this);
			if (guard)
			{
				barracksBunk = new Vector3Int(0, 0, 1); //default bunk loc for guard (necessary for loading during battle)
				transferring = false;
				guardedTrader.waitingOnGuard = false;
				isGuarding = true;
				originalMoveSpeed = guardedTrader.originalMoveSpeed; //move as fast as trader

				if (!guardedTrader.isMoving && !guardedTrader.followingRoute)
					IdleCheck();
			}
			else if (endPosition != barracksBunk)
			{
				GoToBunk();
			}
			else
			{
				transferring = false;
				atHome = true;

				Rotate(homeBase.army.GetRandomSpot(barracksBunk));
				if (isSelected && !world.unitOrders)
					world.unitMovement.ShowIndividualCityButtonsUI();

				if (homeBase.army.AllAreHomeCheck())
					homeBase.army.isTransferring = false;
			}
		}
		else if (guard)
		{
			world.AddUnitPosition(currentLocation, this);

			if (!guardedTrader.isMoving && !guardedTrader.followingRoute)
				IdleCheck();
		}
		else if (repositioning)
		{
			world.AddUnitPosition(currentLocation, this);
			atHome = true;
			repositioning = false;
			Rotate(homeBase.army.GetRandomSpot(barracksBunk));
			if (homeBase.army.AllAreHomeCheck())
				homeBase.army.isRepositioning = false;

			if (isSelected)
				world.unitMovement.ShowIndividualCityButtonsUI();
		}
		else if (returning)
		{
			world.AddUnitPosition(currentLocation, this);
			returning = false;

			Vector3Int endTerrain = world.GetClosestTerrainLoc(endPosition);
			homeBase.army.UnitArrived(endTerrain);

			if (currentLocation == barracksBunk)
			{
				if (currentHealth < buildDataSO.health)
					healthbar.RegenerateHealth();

				atHome = true;
				marker.ToggleVisibility(false);
				if (isSelected && !world.unitOrders)
					world.unitMovement.ShowIndividualCityButtonsUI();

				Rotate(homeBase.army.GetRandomSpot(barracksBunk));
				return;
			}

			//turning to face enemy
			Vector3Int diff = endTerrain - homeBase.army.enemyTarget;
			if (Math.Abs(diff.x) == 3)
				diff.z = 0;
			else if (Math.Abs(diff.z) == 3)
				diff.x = 0;

			Rotate(endPosition - diff);
		}
	}

	public void FinishMovementEnemyMilitary()
	{
		if (inBattle)
		{
			if (world.IsUnitLocationTaken(currentLocation))
				FindNewBattleSpot(currentLocation, targetLocation);
			else
				world.AddUnitPosition(currentLocation, this);

			enemyAI.AggroCheck();
		}
		else if (preparingToMoveOut)
		{
			world.AddUnitPosition(currentLocation, this);
			preparingToMoveOut = false;
			enemyCamp.EnemyReady(this);
		}
		else if (repositioning)
		{
			world.AddUnitPosition(currentLocation, this);
			repositioning = false;
			enemyCamp.EnemyReturn(this);

			if (enemyCamp.movingOut)
			{
				enemyCamp.movingOut = false;
				enemyCamp.moveToLoc = enemyCamp.loc;
				GameLoader.Instance.gameData.movingEnemyBases.Remove(enemyCamp.loc);
				world.mainPlayer.StopRunningAway();
			}
		}
		else if (enemyCamp.movingOut)
		{
			world.AddUnitPosition(currentLocation, this);
			if (enemyCamp.pillage)
			{
				enemyCamp.enemyReady++;

				if (enemyCamp.enemyReady == enemyCamp.campCount - enemyCamp.deathCount)
				{
					enemyCamp.pillageTime = 5;
					StartCoroutine(enemyCamp.Pillage());
				}
			}
			else if (enemyCamp.fieldBattleLoc == enemyCamp.cityLoc)
			{
				enemyCamp.UnitArrived();
			}
		}
	}

	public void KillMilitaryUnit()
	{
		StopAttack();
		minimapIcon.gameObject.SetActive(false);
		if (guard)
		{
			if (isSelected)
				world.unitMovement.ClearSelection();

			guardedTrader.guarded = false;
			guardedTrader.guardUnit = null;
			guardedTrader = null;
			StartCoroutine(WaitKillUnit());
		}
		else
		{
			homeBase.army.UnitsInArmy.Remove(this);
			homeBase.army.attackingSpots.Remove(currentLocation);
			RemoveUnitFromData();

			foreach (Military unit in homeBase.army.UnitsInArmy)
			{
				if (unit.targetSearching)
					unit.AggroCheck();
			}

			if (isSelected)
			{
				if (homeBase.army.UnitsInArmy.Count > 0)//armyCount isn't changed until after battle
				{
					Military nextUnitUp = homeBase.army.GetNextLivingUnit();
					if (nextUnitUp != null)
					{
						world.unitMovement.PrepareMovement(nextUnitUp);
					}
					else
					{
						world.somethingSelected = false;
						world.unitMovement.ClearSelection();
					}
				}
				else
				{
					world.somethingSelected = false;
					world.unitMovement.ClearSelection();
				}
			}

			homeBase.army.RemoveFromArmy(this, barracksBunk);
			homeBase.army.DeadList.Add(this);
		}
	}

	public void KillMilitaryEnemyUnit()
	{
		StopAttack();
		world.RemoveUnitPosition(currentLocation);
		if (isSelected)
			world.unitMovement.ClearSelection();

		if (ambush)
		{
			Vector3Int ambushLoc = world.GetClosestTerrainLoc(transform.position);
			if (world.GetTerrainDataAt(ambushLoc).treeHandler != null)
				world.GetTerrainDataAt(ambushLoc).ToggleTransparentForest(false);

			minimapIcon.gameObject.SetActive(false);
			enemyAmbush.ContinueTradeRoute();
			world.ClearAmbush(enemyAmbush.loc);
			world.uiAttackWarning.AttackWarningCheck(enemyAmbush.loc);
			StartCoroutine(WaitKillUnit());

			if (world.mainPlayer.runningAway)
			{
				world.mainPlayer.StopRunningAway();
				world.mainPlayer.stepAside = false;
			}
		}
		else
		{
			enemyCamp.deathCount++;
			enemyCamp.attackingArmy.attackingSpots.Remove(currentLocation);
			enemyCamp.ClearCampCheck();

			foreach (Military unit in enemyCamp.UnitsInCamp)
			{
				if (unit.targetSearching)
					unit.enemyAI.AggroCheck();
			}

			enemyCamp.DeadList.Add(this);
		}
	}

	public UnitData SaveMilitaryUnitData()
	{
		UnitData data = new();

		data.unitNameAndLevel = buildDataSO.unitNameAndLevel;
		data.position = transform.position;
		data.rotation = transform.rotation;
		data.destinationLoc = destinationLoc;
		data.finalDestinationLoc = finalDestinationLoc;
		data.currentLocation = currentLocation;
		data.prevTerrainTile = prevTerrainTile;
		data.moveOrders = pathPositions.ToList();
		data.isMoving = isMoving;
		data.ambush = ambush;
		data.guard = guard;
		data.idleTime = idleTime;
		data.isGuarding = isGuarding;
		data.atSea = atSea;

		if (isMoving && readyToMarch)
			data.moveOrders.Insert(0, world.RoundToInt(destinationLoc));

		data.moreToMove = moreToMove;
		data.isUpgrading = isUpgrading;
		data.looking = looking;

		//combat
		if (inArmy || enemyAI)
		{
			if (inArmy)
			{
				if (!guard)
					data.cityHomeBase = homeBase.cityLoc;
				data.transferring = transferring;
			}
			else
			{
				data.campSpot = enemyAI.CampSpot;
			}

			data.strengthBonus = strengthBonus;
			data.repositioning = repositioning;
			data.barracksBunk = barracksBunk;
			data.marchPosition = marchPosition;
			data.targetBunk = targetBunk;
			data.currentHealth = currentHealth;
			data.baseSpeed = baseSpeed;
			data.readyToMarch = readyToMarch;
			data.atHome = atHome;
			data.preparingToMoveOut = preparingToMoveOut;
			data.isMarching = isMarching;
			data.returning = returning;
			data.inBattle = inBattle;
			data.attacking = attacking;
			data.aoe = aoe;
			data.targetSearching = targetSearching;
			data.flanking = flanking;
			data.flankedOnce = flankedOnce;
			data.cavalryLine = cavalryLine;
			data.isDead = isDead;
		}

		return data;
	}

	public void LoadUnitData(UnitData data)
	{
		transform.position = data.position;
		transform.rotation = data.rotation;
		destinationLoc = data.destinationLoc;
		finalDestinationLoc = data.finalDestinationLoc;
		currentLocation = data.currentLocation;
		prevTerrainTile = data.prevTerrainTile;
		isMoving = data.isMoving;
		moreToMove = data.moreToMove;
		isUpgrading = data.isUpgrading;
		looking = data.looking;
		ambush = data.ambush;
		guard = data.guard;
		idleTime = data.idleTime;
		somethingToSay = data.somethingToSay;
		isGuarding = data.isGuarding;
		atSea = data.atSea;

		if (!isMoving)
			world.AddUnitPosition(currentLocation, this);

		if (inArmy || enemyAI)
		{
			if (inArmy)
				transferring = data.transferring;
			else
				enemyAI.CampSpot = data.campSpot;

			strengthBonus = data.strengthBonus;
			repositioning = data.repositioning;
			barracksBunk = data.barracksBunk;
			marchPosition = data.marchPosition;
			targetBunk = data.targetBunk;
			currentHealth = data.currentHealth;

			if (currentHealth < healthMax)
			{
				healthbar.LoadHealthLevel(currentHealth);
				healthbar.gameObject.SetActive(true);

				if (enemyAI && !data.inBattle)
				{
					healthbar.RegenerateHealth();
				}
				else if (inArmy)
				{
					if (data.atHome || isGuarding)
						healthbar.RegenerateHealth();
				}
			}

			baseSpeed = data.baseSpeed; //coroutine
			readyToMarch = data.readyToMarch;
			atHome = data.atHome;
			preparingToMoveOut = data.preparingToMoveOut;
			isMarching = data.isMarching;
			returning = data.returning;
			inBattle = data.inBattle;
			attacking = data.attacking;
			aoe = data.aoe;
			targetSearching = data.targetSearching;
			flanking = data.flanking;
			flankedOnce = data.flankedOnce;
			cavalryLine = data.cavalryLine;
			isDead = data.isDead;

			if (enemyAI && enemyCamp.campCount == 0) //used for else statement
			{
			}
			else
			{
				GameLoader.Instance.attackingUnitList.Add(this);
			}
		}

		if (isMoving)
		{
			Vector3Int endPosition = world.RoundToInt(finalDestinationLoc);

			if (!readyToMarch)
			{
				if (data.moveOrders.Count == 0)
					data.moveOrders.Add(endPosition);

				pathPositions = new Queue<Vector3Int>(data.moveOrders);

				StartCoroutine(WaitForOthers(endPosition));
				return;
			}

			if (data.moveOrders.Count == 0)
				data.moveOrders.Add(endPosition);

			if (atSea)
				ToggleBoat(true);
			GameLoader.Instance.unitMoveOrders[this] = data.moveOrders;
			//MoveThroughPath(data.moveOrders);
		}
		else if (looking)
		{
			StartLookingAround();
		}

		if (guard && !guardedTrader.followingRoute)
			IdleCheck();
	}

	public void LoadAttack()
	{
		if (attacking)
		{
			Unit target = null;
			if (ambush)
			{
				if (guard)
				{
					EnemyAmbush ambush = world.GetEnemyAmbush(guardedTrader.ambushLoc);
					for (int i = 0; i < ambush.attackingUnits.Count; i++)
					{
						if (ambush.attackingUnits[i].barracksBunk == targetBunk)
						{
							target = ambush.attackingUnits[i];
							break;
						}
					}
				}
				else
				{
					for (int i = 0; i < enemyAmbush.attackedUnits.Count; i++)
					{
						if (enemyAmbush.attackedUnits[i].military.barracksBunk == targetBunk)
						{
							target = enemyAmbush.attackedUnits[i];
							break;
						}
					}
				}
			}
			else if (enemyAI)
			{
				List<Military> units = enemyCamp.attackingArmy.UnitsInArmy;

				for (int i = 0; i < units.Count; i++)
				{
					if (units[i].barracksBunk == targetBunk)
					{
						target = units[i];
						break;
					}
				}
			}
			else
			{
				List<Military> units = homeBase.army.targetCamp.UnitsInCamp;

				for (int i = 0; i < units.Count; i++)
				{
					if (units[i].barracksBunk == targetBunk)
					{
						target = units[i];
						break;
					}
				}
			}

			if (target == null)
			{
				attacking = false;
				if (inArmy)
					AggroCheck();
				else
					enemyAI.AggroCheck();

				return;
			}

			if (inArmy)
			{
				if (buildDataSO.unitType == UnitType.Ranged)
					attackCo = StartCoroutine(RangedAttack(target));
				else
					attackCo = StartCoroutine(Attack(target));
			}
			else
			{
				if (buildDataSO.unitType == UnitType.Ranged)
					attackCo = StartCoroutine(enemyAI.RangedAttack(target));
				else
					attackCo = StartCoroutine(enemyAI.Attack(target));
			}
		}
		else if (isDead)
		{
			unitMesh.SetActive(false);
			healthbar.gameObject.SetActive(false);
			marker.gameObject.SetActive(false);
			Vector3 sixFeetUnder = transform.position;
			sixFeetUnder.y -= 6;
			transform.position = sixFeetUnder;
			unitRigidbody.useGravity = false;

			if (enemyAI)
			{
				enemyCamp.deathCount++;
				if (enemyCamp.attackingArmy != null)
					enemyCamp.attackingArmy.attackingSpots.Remove(currentLocation);
				enemyCamp.ClearCampCheck();
				world.RemoveUnitPosition(currentLocation);
				enemyCamp.DeadList.Add(this);
			}
			else
			{
				minimapIcon.gameObject.SetActive(false);
				homeBase.army.UnitsInArmy.Remove(this);
				homeBase.army.attackingSpots.Remove(currentLocation);
				RemoveUnitFromData();
				homeBase.army.RemoveFromArmy(this, barracksBunk);
				homeBase.army.DeadList.Add(this);
				world.RemoveUnitPosition(currentLocation);
			}
		}
		else if (inBattle)
		{
			if (enemyAI)
				enemyAI.AggroCheck();
			else
				AggroCheck();
		}
	}

	public IEnumerator WaitForOthers(Vector3Int endPosition)
	{
		while (!readyToMarch)
		{
			yield return null;
		}
		StartAnimation();

		if (pathPositions.Count > 0)
		{
			RestartPath(pathPositions.Dequeue());
			if (pathQueue.Count > 0)
			{
				DequeuePath();
			}
		}
		else
		{
			FinishMoving(endPosition);
		}
	}
}
