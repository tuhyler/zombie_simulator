using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.RuleTile.TilingRuleOutput;
using static UnityEngine.UI.CanvasScaler;

public class Military : Unit
{
	[SerializeField]
	public GameObject boatMesh;

	[SerializeField]
	private MeshFilter logoSail, sail;
	
	private int isPillagingHash, isDiscoveredHash, isSittingHash, isClappingHash;
	[HideInInspector]
	public Vector3Int targetLocation, targetBunk, barracksBunk, marchPosition; //targetLocation is in case units overlap on same tile

	[HideInInspector]
	public Coroutine attackCo/*, waitingCo*/;
	[HideInInspector]
	public int attackStrength, strengthBonus;

	[HideInInspector]
	public bool atHome, preparingToMoveOut, isMarching, transferring, repositioning, inBattle, attacking, targetSearching, flanking, 
		flankedOnce, cavalryLine, guard, isGuarding, returning, atSea, benched, duelWatch, battleCam;

	[HideInInspector]
	public List<Vector3Int> switchLocs = new();
	[HideInInspector]
	public Army army;
	[HideInInspector]
	public Navy navy;
	[HideInInspector]
	public AirForce airForce;
	[HideInInspector]
	public EnemyCamp enemyCamp;
	[HideInInspector]
	public EnemyAmbush enemyAmbush;

	[HideInInspector]
	public Projectile projectile;

	[HideInInspector]
	public Trader guardedTrader;

	[HideInInspector]
	public MilitaryLeader leader;

	[HideInInspector]
	public BodyGuard bodyGuard;


	private void Awake()
	{
		AwakeMethods();
		MilitaryAwakeMethods();

		if (logoSail)
		{
			int factor = 0;
			if (buildDataSO.unitType == UnitType.Ranged)
				factor = 1;
			else if (buildDataSO.unitType == UnitType.Cavalry)
				factor = 2;
			else if (buildDataSO.unitType == UnitType.Seige)
				factor = 3;

			if (factor > 0)
			{
				float shift = 0.03125f * factor;
				Vector2[] sailUV = logoSail.mesh.uv;
				for (int i = 0; i < sailUV.Length; i++)
					sailUV[i].x += shift;

				logoSail.mesh.uv = sailUV;
			}
		}
	}

	protected virtual void MilitaryAwakeMethods()
	{
		military = this;
		attackStrength = buildDataSO.baseAttackStrength;

		if (buildDataSO.unitType == UnitType.Ranged || buildDataSO.unitType == UnitType.Seige)
		{
			projectile = GetComponentInChildren<Projectile>();
			//projectile.SetProjectilePos();
			projectile.gameObject.SetActive(false);
		}

		isPillagingHash = Animator.StringToHash("isPillaging");
		isDiscoveredHash = Animator.StringToHash("isDiscovered");
		isSittingHash = Animator.StringToHash("isSitting");
		isClappingHash = Animator.StringToHash("isClapping");
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

	public void SetSailColor(Vector2 color)
	{
		Vector2[] sailUV = sail.mesh.uv;
		for (int i = 0; i < sailUV.Length; i++)
			sailUV[i] = color;

		sail.mesh.uv = sailUV;
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

	public void ToggleClapping(bool v)
	{
		unitAnimator.SetBool(isClappingHash, v);
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
			target.ReduceHealth(this, transform.eulerAngles, attackStrength + strengthBonus, attacks[UnityEngine.Random.Range(0, attacks.Length)]);
			yield return attackPauses[0];
		}

		attacking = false;
		attackCo = null;

		if (!ambush)
		{
			StopMovementCheck(true);
			//StopAnimation();
			AggroCheck();
		}
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
			projectile.SetPoints(transform.position, target.transform.position, false);
			StartCoroutine(projectile.Shoot(this, target));
			yield return attackPauses[0];
		}

		attackCo = null;
		attacking = false;

		if (!ambush)
		{
			StopAnimation();
			AggroCheck();
		}
	}

	private IEnumerator SeigeAttack(Vector3Int loc)
	{
		attacking = true;

		int wait = UnityEngine.Random.Range(0, 3);
		if (wait != 0)
			yield return attackPauses[wait];
		Vector3 endPoint = loc;
		endPoint.y += world.GetTerrainDataAt(loc).isHill ? 0.65f : 0;

		while (army.targetCamp != null)
		{
			transform.rotation = Quaternion.LookRotation(loc - transform.position);
			StartAttackingAnimation();
			yield return attackPauses[2];
			projectile.SetPoints(transform.position, endPoint, true);
			StartCoroutine(projectile.Shoot(this, null, true));
			yield return attackPauses[0];
			yield return attackPauses[0];
		}

		attackCo = null;
		attacking = false;
	}

	public void AOEExplosion(Vector3 position)
	{
		Vector3Int loc = world.RoundToInt(position);
		
		//if (world.IsUnitLocationTaken(loc))
		//{
		//	Military enemy = world.GetUnit(loc).military;

		//	if (enemyAI)
		//	{
		//		if (enemyCamp.attackingArmy.UnitsInArmy.Contains(enemy))
		//			enemy.ReduceHealth(this, transform.eulerAngles, military.attackStrength + military.strengthBonus, attacks[UnityEngine.Random.Range(0, attacks.Length)]);
		//	}
		//	else
		//	{
		//		if (army.targetCamp.UnitsInCamp.Contains(enemy))
		//			enemy.ReduceHealth(this, transform.eulerAngles, military.attackStrength + military.strengthBonus, attacks[UnityEngine.Random.Range(0, attacks.Length)]);
		//	}
		//}

		//foreach (Vector3Int pos in world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAY))
		//{
		//	Vector3Int neighbor = pos + loc;

		//	if (world.IsUnitLocationTaken(neighbor))
		//	{
		//		Military enemy = world.GetUnit(neighbor).military;
		//		int damage = Mathf.RoundToInt((military.attackStrength + military.strengthBonus) * 0.2f);
		//		Vector3 eulerAngles = Quaternion.LookRotation(enemy.transform.position - position, Vector3.up).eulerAngles;

		//		if (enemyAI)
		//		{
		//			if (enemyCamp.attackingArmy.UnitsInArmy.Contains(enemy))
		//				enemy.ReduceHealth(this, eulerAngles, damage, attacks[UnityEngine.Random.Range(0, attacks.Length)]);
		//		}
		//		else
		//		{
		//			if (army.targetCamp.UnitsInCamp.Contains(enemy))
		//				enemy.ReduceHealth(this, eulerAngles, damage, attacks[UnityEngine.Random.Range(0, attacks.Length)]);
		//		}
		//	}
		//}


		bool isHill = world.GetTerrainDataAt(loc).isHill;
		float increase = isHill ? 1.15f : 0.15f;

		Vector3 rayCastLoc = loc;
		rayCastLoc.y += increase;

		List<Vector3Int> directions = world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAY);
		directions.Add(Vector3Int.down);
		for (int i = 0; i < directions.Count; i++)
		{
			Vector3 pos = directions[i];
			if (isHill)
				pos.y -= 0.5f;

			float distance = i % 2 == 0 ? 1.5f : 2.1f;
			if (i == 8)
				rayCastLoc.y += 1.2f;

			//can't only hit one at a time
			Debug.DrawRay(rayCastLoc, pos * distance, Color.yellow, 10);

			RaycastHit[] hits = Physics.RaycastAll(rayCastLoc, pos, distance, world.unitMask);
			for (int j = 0; j < hits.Length; j++)
			{
				GameObject hitGO = hits[j].collider.gameObject;

				if (hitGO && hitGO.TryGetComponent(out Military enemy))
				{
					float diffDist = Mathf.Sqrt(Mathf.Pow(rayCastLoc.x - enemy.transform.position.x, 2) + Mathf.Pow(rayCastLoc.z - enemy.transform.position.z, 2));
					float perc = Mathf.Max(0, 1 - diffDist / distance);
					int damage = Mathf.RoundToInt((military.attackStrength + military.strengthBonus) * perc);
					Vector3 diff = enemy.transform.position - position;
					Vector3 eulerAngles;
					if (diff == Vector3.zero)
						eulerAngles = transform.eulerAngles;
					else
						eulerAngles = Quaternion.LookRotation(enemy.transform.position - position, Vector3.up).eulerAngles;

					bool hit = false;
					if (enemyAI)
					{
						if (enemy.buildDataSO.inMilitary && enemyCamp.attackingArmy.UnitsInArmy.Contains(enemy))
							hit = true;
					}
					else
					{
						if (enemy.enemyAI && army.targetCamp.UnitsInCamp.Contains(enemy))
							hit = true;
					}
					
					if (hit)
						enemy.ReduceHealth(this, eulerAngles, damage, attacks[UnityEngine.Random.Range(0, attacks.Length)]);
				}
			}
		}
	}

	public void PlayExplosion(Vector3 loc)
	{
		ParticleSystem explosion = Instantiate(Resources.Load<ParticleSystem>("Prefabs/ParticlePrefabs/" + buildDataSO.aoeExplosionPrefab), loc, Quaternion.Euler(-90, 0, 0));
		explosion.transform.SetParent(world.psHolder, false);
		explosion.Play();
	}

	public void AggroCheck()
	{
		if (attacking)
			return;
		UnitType type = buildDataSO.unitType;

		if (!army.FinishAttack())
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
		if (isDead)
			return;

		targetSearching = false;

		List<Vector3Int> attackingZones = new();

		Vector3Int forward = army.forward;
		Vector3Int forwardTile = forward + currentLocation;

		attackingZones.Add(forwardTile);
		if (forward.z != 0)
		{
			attackingZones.Add(new Vector3Int(1, 0, forward.z) + currentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, forward.z) + currentLocation);
			attackingZones.Add(new Vector3Int(1, 0, 0) + currentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, 0) + currentLocation);
			attackingZones.Add(new Vector3Int(1, 0, -forward.z) + currentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, -forward.z) + currentLocation);
			attackingZones.Add(new Vector3Int(0, 0, -forward.z) + currentLocation);
		}
		else
		{
			attackingZones.Add(new Vector3Int(forward.x, 0, 1) + currentLocation);
			attackingZones.Add(new Vector3Int(forward.x, 0, -1) + currentLocation);
			attackingZones.Add(new Vector3Int(0, 0, 1) + currentLocation);
			attackingZones.Add(new Vector3Int(0, 0, -1) + currentLocation);
			attackingZones.Add(new Vector3Int(-forward.x, 0, 1) + currentLocation);
			attackingZones.Add(new Vector3Int(-forward.x, 0, -1) + currentLocation);
			attackingZones.Add(new Vector3Int(-forward.x, 0, 0) + currentLocation);
		}

		foreach (Vector3Int zone in attackingZones)
		{
			if (!army.movementRange.Contains(zone))
				continue;

			if (world.IsUnitLocationTaken(zone))
			{
				Military enemy = world.GetUnit(zone).military;
				if (army.targetCamp.UnitsInCamp.Contains(enemy))
				{
					//attacking = true;
					if (!army.attackingSpots.Contains(currentLocation))
						army.attackingSpots.Add(currentLocation);

					if (!attacking)
					{
						StartAttack(enemy);
					}
					else
					{
						if (enemy.targetSearching)
							enemy.enemyAI.StartAttack(this);
					}
				}
			}
		}

		if (attacking)
			return;

		Unit newEnemy = army.FindClosestTarget(this);
		targetLocation = newEnemy.currentLocation;
		List<Vector3Int> path = army.PathToEnemy(currentLocation, world.RoundToInt(newEnemy.transform.position));

		if (path.Count > 0)
		{
			army.attackingSpots.Remove(currentLocation);

			//moving unit behind if stuck
			Vector3Int positionBehind = army.forward * -1 + currentLocation;

			if (world.IsUnitLocationTaken(positionBehind))
			{
				Unit unitBehind = world.GetUnit(positionBehind);
				if (army.UnitsInArmy.Contains(unitBehind) && unitBehind.military.targetSearching)
					unitBehind.military.AggroCheck();
			}

			if (path.Count >= 2)
			{
				List<Vector3Int> shortPath = new() { path[0] };
				finalDestinationLoc = shortPath[0];
				MoveThroughPath(shortPath);
				army.attackingSpots.Add(path[0]);
			}
			else if (path.Count == 1)
				StartAttack(newEnemy);
		}
		else
		{
			Vector3Int scoochCloser = new Vector3Int(Math.Sign(targetLocation.x - currentLocation.x), 0, Math.Sign(targetLocation.z - currentLocation.z)) + currentLocation;

			if (army.movementRange.Contains(scoochCloser) && !world.IsUnitLocationTaken(scoochCloser) && !army.attackingSpots.Contains(scoochCloser))
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
		if (isDead)
			return;

		Unit enemy = army.FindClosestTarget(this);

		if (enemy != null)
			attackCo = StartCoroutine(RangedAttack(enemy));
	}

	public void SeigeAggroCheck()
	{
		if (isDead)
			return;

		attackCo = StartCoroutine(SeigeAttack(army.enemyTarget));
	}

	public void RangedAmbushCheck(Unit enemy)
	{
		attackCo = StartCoroutine(RangedAttack(enemy));
	}

	public void CavalryAggroCheck()
	{
		if (isDead)
			return;

		targetSearching = false;

		List<Vector3Int> attackingZones = new();

		Vector3Int forward = army.forward;
		Vector3Int forwardTile = forward + currentLocation;

		attackingZones.Add(forwardTile);
		if (forward.z != 0)
		{
			attackingZones.Add(new Vector3Int(1, 0, forward.z) + currentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, forward.z) + currentLocation);
			attackingZones.Add(new Vector3Int(1, 0, 0) + currentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, 0) + currentLocation);
			attackingZones.Add(new Vector3Int(1, 0, -forward.z) + currentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, -forward.z) + currentLocation);
			attackingZones.Add(new Vector3Int(0, 0, -forward.z) + currentLocation);
		}
		else
		{
			attackingZones.Add(new Vector3Int(forward.x, 0, 1) + currentLocation);
			attackingZones.Add(new Vector3Int(forward.x, 0, -1) + currentLocation);
			attackingZones.Add(new Vector3Int(0, 0, 1) + currentLocation);
			attackingZones.Add(new Vector3Int(0, 0, -1) + currentLocation);
			attackingZones.Add(new Vector3Int(-forward.x, 0, 1) + currentLocation);
			attackingZones.Add(new Vector3Int(-forward.x, 0, -1) + currentLocation);
			attackingZones.Add(new Vector3Int(-forward.x, 0, 0) + currentLocation);
		}

		foreach (Vector3Int zone in attackingZones)
		{
			if (!army.cavalryRange.Contains(zone))
				continue;

			if (world.IsUnitLocationTaken(zone))
			{
				Military enemy = world.GetUnit(zone).military;
				if (army.targetCamp.UnitsInCamp.Contains(enemy))
				{
					if (!army.attackingSpots.Contains(currentLocation))
						army.attackingSpots.Add(currentLocation);

					if (!attacking)
					{
						if (flanking) //check for those sleeping if they can attack
						{
							foreach (Military unit in army.targetCamp.UnitsInCamp)
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
						if (enemy.targetSearching)
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

			if ((world.IsUnitLocationTaken(forwardTile) && army.UnitsInArmy.Contains(world.GetUnit(forwardTile))) || cavalryLine)
			{
				cavalryLine = false; //for subsequent battles
				newEnemy = army.FindEdgeRanged(currentLocation);
			}
		}
		else
		{
			flanking = false;
		}

		if (newEnemy == null)
			newEnemy = army.FindClosestTarget(this);
		else
			flanking = true;

		targetLocation = newEnemy.currentLocation;
		List<Vector3Int> path = army.CavalryPathToEnemy(currentLocation, world.RoundToInt(newEnemy.transform.position));

		if (path.Count > 0)
		{
			army.attackingSpots.Remove(currentLocation);

			//moving unit behind if stuck
			Vector3Int positionBehind = army.forward * -1 + currentLocation;

			if (world.IsUnitLocationTaken(positionBehind))
			{
				Unit unitBehind = world.GetUnit(positionBehind);
				if (army.UnitsInArmy.Contains(unitBehind))
				{
					if (unitBehind.military.targetSearching)
						unitBehind.military.AggroCheck();
					else if (unitBehind.buildDataSO.unitType == UnitType.Cavalry && !unitBehind.military.flankedOnce)
						unitBehind.military.cavalryLine = true;
				}	
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
				army.attackingSpots.Add(path[0]);
			}
			else if (path.Count == 1)
			{
				StartAttack(newEnemy);
			}
		}
		else
		{
			Vector3Int scoochCloser = new Vector3Int(Math.Sign(targetLocation.x - currentLocation.x), 0, Math.Sign(targetLocation.z - currentLocation.z)) + currentLocation;

			if (army.movementRange.Contains(scoochCloser) && !world.IsUnitLocationTaken(scoochCloser) && !army.attackingSpots.Contains(scoochCloser))
			{
				finalDestinationLoc = scoochCloser;
				//List<Vector3Int> newPath = new() { scoochCloser };
				MoveThroughPath(new List<Vector3Int> { scoochCloser });
			}

			targetSearching = true;
		}
	}

	public void StopAttacking(bool finishMovement)
	{
		strengthBonus = 0;
		if (isSelected)
			world.unitMovement.infoManager.UpdateStrengthBonus(strengthBonus);

		StopAttackAnimation();
		attacking = false;
		inBattle = false;
		flankedOnce = false;
		flanking = false;
		targetSearching = false;
		StopMovementCheck(finishMovement);
		isMarching = false;
		//StopAnimation();
	}

	public void StartReturn()
	{
		if (isDead)
			return;

		//unit.StopAttacking();
		AttackCheck();
		inBattle = false;
		attacking = false;
		targetSearching = false;
		flanking = false;
		flankedOnce = false;
		returning = true;

		StopMovementCheck(false);

		finalDestinationLoc = barracksBunk;
		List<Vector3Int> path;

		if (army.battleAtSea)
			path = GridSearch.MoveWherever(world, world.RoundToInt(transform.position), barracksBunk);
		else
			path = GridSearch.MilitaryMove(world, world.RoundToInt(transform.position), barracksBunk, bySea);

		if (path.Count == 0)
			path = GridSearch.MoveWherever(world, world.RoundToInt(transform.position), barracksBunk);

		if (path.Count > 0)
			MoveThroughPath(path);
		else
			FinishMoving(transform.position);
	}

	public void SoloMove(Vector3 loc)
	{
		Vector3Int locInt = world.RoundToInt(loc);
		StopMovementCheck(false);		
		List<Vector3Int> path;

		path = GridSearch.MilitaryMove(world, world.RoundToInt(transform.position), locInt, bySea);

		if (path.Count == 0)
			path = GridSearch.MoveWherever(world, world.RoundToInt(transform.position), locInt);

		if (path.Count == 0)
			path.Add(locInt);

		transferring = true;
		finalDestinationLoc = loc;
		MoveThroughPath(path);
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
		List<Vector3Int> path = GridSearch.EnemyMove(world, transform.position, endPosition, bySea, avoidList); //enemy works in this case

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
		StopAnimation();
		isGuarding = true;
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
		List<Vector3Int> path = GridSearch.MilitaryMove(world, currentLocation, barracksBunk, bySea);

		if (path.Count == 0)
			path = GridSearch.MoveWherever(world, currentLocation, barracksBunk);

		if (path.Count == 0)
			path.Add(barracksBunk);

		MoveThroughPath(path);
	}

	public void FindNewBattleSpot(Vector3Int current, Vector3Int target)
	{
		Army army;

		if (buildDataSO.inMilitary || bodyGuard)
			army = this.army;
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

			if (bySea)
			{
			}
			else
			{
				if (!world.CheckIfPositionIsValid(tile) || world.IsUnitLocationTaken(tile) || army.attackingSpots.Contains(tile))
					continue;
			}

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

	//public void StartLookingAround()
	//{
	//	StartCoroutine(WaitAndScan());
	//}

	//private IEnumerator WaitAndScan()
	//{
	//	int lookAroundCount = 0;

	//	while (lookAroundCount < 5)
	//	{
	//		yield return new WaitForSeconds(1);
	//		lookAroundCount++;

	//		//randomly looking around
	//		Rotate(world.GetNeighborsFor(currentLocation, MapWorld.State.EIGHTWAY)[UnityEngine.Random.Range(0, 8)]);
	//	}

	//	looking = false;
	//	enemyCamp.FinishMoveOut();
	//}

	public void GuardGetInLine(Vector3Int traderPrevSpot, Vector3Int traderSpot)
	{
		StopMovementCheck(false);

		Vector3 sideSpot = GuardRouteFinish(traderSpot, traderPrevSpot);
		finalDestinationLoc = sideSpot;
		StartAnimation();
		isMoving = true;
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

		PlayLightBeam();
		//Vector3 newScale = transform.localScale;
		//newScale.y = 0.01f;
		//Vector3 lightPosition = transform.position;
		//lightPosition.y += 0.01f;
		//if (world.IsRoadOnTileLocation(world.RoundToInt(transform.position)))
		//	lightPosition.y += .1f;
		//lightBeam.transform.position = lightPosition;
		//lightBeam.Play();
		LeanTween.scale(gameObject, Vector3.zero, 0.5f).setEase(LeanTweenType.easeOutBack).setOnComplete(DestroyUnit);
	}

	public void GuardToBunkCheck(Vector3Int homeCity, bool arrived)
	{
		//seeing if city has a spot for military unit, if not, it joins the city. 
		if (world.GetCity(homeCity).singleBuildDict.ContainsKey(buildDataSO.singleBuildType))
		{
			CityImprovement improvement = world.GetCityDevelopment(world.GetCity(homeCity).singleBuildDict[buildDataSO.singleBuildType]);

			if (improvement.army.isFull || improvement.army.defending || !improvement.army.atHome)
			{
				JoinCity(world.GetCity(homeCity));
			}
			else
			{
				StopMovementCheck(false);
				
				if (arrived)
				{
					if (!isSelected)
						Unhighlight();
					improvement.army.AddToArmy(this);
					army = improvement.army;
					barracksBunk = improvement.army.GetAvailablePosition(buildDataSO.unitType);
					SoloMove(barracksBunk);
				}
				else
				{
					guardedTrader = null;
					originalMoveSpeed = buildDataSO.movementSpeed;
					isGuarding = false;
					guard = false;
					SoloMove(improvement.loc);
				}
			}
		}
		else
		{
			JoinCity(world.GetCity(homeCity));
		}
	}

	public void JoinCity(City city)
	{
		world.unitMovement.AddToCity(city, this);
	}

	public void PillageSound()
	{
		audioSource.clip = attacks[UnityEngine.Random.Range(0, attacks.Length)];
		audioSource.Play();
	}

	public void ToggleBoat(bool v)
	{
		PlayLightBeam();
		//Vector3 lightBeamLoc = transform.position;
		//lightBeamLoc.y += .01f;
		//lightBeam.transform.position = lightBeamLoc;
		//lightBeam.Play();
		
		if (isSelected)
			selectionCircle.SetActive(v);

		GameObject go = v ? boatMesh : unitMesh;
		Vector3 unitScale = go.transform.localScale;
		float scaleX = unitScale.x;
		float scaleZ = unitScale.z;
		go.transform.localScale = new Vector3(scaleX, 0.1f, scaleZ);
		LeanTween.scale(go, unitScale, 0.5f).setEase(LeanTweenType.easeOutBack);

		if (transferring)
			bySea = v;

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
			army.UnitReady(this);
		}
		else if (isMarching)
		{
			world.AddUnitPosition(currentLocation, this);
			isMarching = false;

			Vector3Int endTerrain = world.GetClosestTerrainLoc(endPosition);
			army.UnitArrived(endTerrain);

			if (currentLocation == barracksBunk)
			{
				if (currentHealth < buildDataSO.health)
					healthbar.RegenerateHealth();

				atHome = true;
				army.AddToCycleCost(buildDataSO.cycleCost, false);
				outline.ToggleOutline(false);
				//marker.ToggleVisibility(false);
				if (isSelected && !world.unitOrders)
					world.unitMovement.ShowIndividualCityButtonsUI();

				Rotate(army.GetRandomSpot(barracksBunk));
				return;
			}

			//turning to face enemy
			Vector3Int diff = endTerrain - army.enemyTarget;
			if (Math.Abs(diff.x) == 3)
				diff.z = 0;
			else if (Math.Abs(diff.z) == 3)
				diff.x = 0;

			Rotate(endPosition - diff);
		}
		else if (transferring)
		{
			if (guard)
			{
				barracksBunk = new Vector3Int(0, 0, 1); //default bunk loc for guard (necessary for loading during battle)
				transferring = false;
				isGuarding = true;
				originalMoveSpeed = guardedTrader.originalMoveSpeed; //move as fast as trader
				
				if (guardedTrader.waitingOnGuard)
				{
					if (!bySea && !byAir)
					{
						guardedTrader.guardMeshList[buildDataSO.unitLevel - 1].SetActive(true);
						gameObject.SetActive(false);
					}

					guardedTrader.waitingOnGuard = false;
					if (guardedTrader.isSelected)
						guardedTrader.RemoveWarning();
					guardedTrader.BeginNextStepInRoute();
				}
			}
			else if (endPosition == barracksBunk)
			{
				world.AddUnitPosition(currentLocation, this);
				transferring = false;
				switchLocs.Clear();
				atHome = true;
				outline.ToggleOutline(false);

				Rotate(army.GetRandomSpot(barracksBunk));
				if (isSelected && !world.unitOrders)
				{
					world.unitMovement.ShowIndividualCityButtonsUI();
					military.army.SelectArmy(military);
				}
				else if (army.selected)
				{
					SoftSelect(Color.white);
				}
			}
			else 
			{
				if (world.TileHasCityImprovement(currentLocation) && world.GetCityDevelopment(currentLocation).city && 
					world.GetCityDevelopment(currentLocation).GetImprovementData.singleBuildType == buildDataSO.singleBuildType)
				{
					GuardToBunkCheck(world.GetCityDevelopment(currentLocation).city.cityLoc, true);
				}
				else
				{
					List<Vector3Int> cityRadius = world.GetNeighborsCoordinates(MapWorld.State.CITYRADIUS);

					for (int i = 0; i < cityRadius.Count; i++)
					{
						if (world.IsCityOnTile(cityRadius[i] + currentLocation))
						{
							GuardToBunkCheck(cityRadius[i] + currentLocation, false);
							return;
						}
					}

					KillUnit(Vector3.zero);
				}
			}
		}
		else if (guard)
		{
			if (ambush)
			{
				ambush = false;
				guardedTrader.ContinueTradeRoute();
			}
			else if (guardedTrader == null && world.IsCityOnTile(currentLocation))
			{
				GuardToBunkCheck(currentLocation, false);
			}
		}
		else if (repositioning)
		{
			world.AddUnitPosition(currentLocation, this);
			atHome = true;
			repositioning = false;
			Rotate(army.GetRandomSpot(barracksBunk));
			if (army.AllAreHomeCheck())
				army.isRepositioning = false;

			if (isSelected)
				world.unitMovement.ShowIndividualCityButtonsUI();
		}
		else if (returning)
		{
			world.AddUnitPosition(currentLocation, this);
			returning = false;

			Vector3Int endTerrain = world.GetClosestTerrainLoc(endPosition);
			army.UnitArrived(endTerrain);
			army.AddToCycleCost(buildDataSO.cycleCost, false);

			if (currentLocation == barracksBunk)
			{
				if (currentHealth < buildDataSO.health)
					healthbar.RegenerateHealth();

				atHome = true;
				outline.ToggleOutline(false);
				//marker.ToggleVisibility(false);
				if (isSelected && !world.unitOrders)
					world.unitMovement.ShowIndividualCityButtonsUI();

				Rotate(army.GetRandomSpot(barracksBunk));
				return;
			}

			//turning to face enemy
			Vector3Int diff = endTerrain - army.enemyTarget;
			if (Math.Abs(diff.x) == 3)
				diff.z = 0;
			else if (Math.Abs(diff.z) == 3)
				diff.x = 0;

			Rotate(endPosition - diff);
		}
		else if (bodyGuard && bodyGuard.dueling)
		{
			Rotate(currentLocation + bodyGuard.army.forward);
			bodyGuard.Charge();
		}
	}

	public void FinishMovementEnemyMilitary()
	{
		if (leader && (!leader.defending && !leader.dueling))
		{
			leader.FinishMovementEnemyLeader();
		}
		else if (inBattle)
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
			returning = false;
			outline.ToggleOutline(false);
			//marker.ToggleVisibility(false);

			if (leader)
			{
				leader.FinishMovementEnemyLeader();
				if (enemyCamp.campCount - enemyCamp.deathCount == 0)
					enemyCamp.ResetCampToBase();
			}
			else
			{
				enemyCamp.EnemyReturn(this);
			}

			if (!isSelected)
				Unhighlight();
			//if (enemyCamp.movingOut)
			//{
			//	//enemyCamp.movingOut = false;
			//	//enemyCamp.returning = false;
			//	//enemyCamp.moveToLoc = enemyCamp.loc;
			//	//GameLoader.Instance.gameData.movingEnemyBases.Remove(enemyCamp.loc);
			//	//world.mainPlayer.StopRunningAway();
			//}
		}
		else if (enemyCamp.movingOut)
		{
			if (duelWatch)
			{
				Rotate(enemyCamp.loc);
				return;
			}

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
		else if (duelWatch)
		{
			Rotate(enemyCamp.loc);
		}
		else if (leader && leader.dueling)
		{
			world.AddUnitPosition(currentLocation, this);
			Rotate(currentLocation + enemyCamp.forward);
			leader.Charge();
		}
	}

	public void KillMilitaryUnit(Vector3 rotation)
	{
		AttackCheck();
		world.RemoveUnitPosition(currentLocation);
		minimapIcon.gameObject.SetActive(false);
		pathPositions.Clear();
		
		if (guard)
		{
			if (isSelected)
			{
				world.somethingSelected = false;
				world.unitMovement.ClearSelection();
			}

			guardedTrader.guarded = false;
			guardedTrader.guardUnit = null;
			guardedTrader = null;
			guard = false;
			StartCoroutine(WaitKillUnit());
		}
		else if (army != null)
		{
			army.UnitsInArmy.Remove(this);
			army.attackingSpots.Remove(currentLocation);

			foreach (Military unit in army.UnitsInArmy)
			{
				if (unit.targetSearching)
					unit.AggroCheck();
			}

			if (isSelected)
			{
				if (army.UnitsInArmy.Count > 0)//armyCount isn't changed until after battle
				{
					Military nextUnitUp = army.GetNextLivingUnit();
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

			army.RemoveFromArmy(this, barracksBunk, false);
			army.DeadList.Add(this);

			//Vector3Int currentLoc = world.RoundToInt(transform.position);
			//if (world.IsTraderLocationTaken(currentLoc))
			//{
			//	List<Trader> tempTraderList = new(world.GetTrader(currentLoc));
			//	for (int i = 0; i < tempTraderList.Count; i++)
			//		tempTraderList[i].KillUnit(rotation);
			//}
		}
		else
		{
			if (isSelected)
			{
				world.somethingSelected = false;
				world.unitMovement.ClearSelection();
			}

			StartCoroutine(WaitKillUnit());
		}
	}

	public void KillMilitaryEnemyUnit()
	{
		enemyAI.AttackCheck();
		world.RemoveUnitPosition(currentLocation);
		if (isSelected)
			world.unitMovement.ClearSelection();

		if (ambush)
		{
			//Vector3Int ambushLoc = world.GetClosestTerrainLoc(transform.position);
			//if (world.GetTerrainDataAt(ambushLoc).treeHandler != null)
			//	world.GetTerrainDataAt(ambushLoc).ToggleTransparentForest(false);

			minimapIcon.gameObject.SetActive(false);
			enemyAmbush.ContinueTradeRoute();
			world.ClearAmbush(enemyAmbush.loc);
			world.uiAttackWarning.AttackWarningCheck(enemyAmbush.loc);
			StartCoroutine(WaitKillUnit());

			if (world.mainPlayer.runningAway)
				world.mainPlayer.StopRunningAway();
		}
		else
		{
			enemyCamp.deathCount++;
			enemyCamp.attackingArmy.attackingSpots.Remove(currentLocation);
			enemyCamp.FinishBattleCheck();
			//enemyCamp.ClearCampCheck();

			foreach (Military unit in enemyCamp.UnitsInCamp)
			{
				if (unit.targetSearching)
					unit.enemyAI.AggroCheck();
			}

			if (!leader)
			{
				enemyCamp.DeadList.Add(this);
			}
			else
			{
				StartCoroutine(SetInactiveWait());
				enemyCamp.UnitsInCamp.Remove(this);
				
				if (!leader.dueling && enemyCamp.benchedUnit != null)
				{
					Military unit = enemyCamp.benchedUnit;
					//world.RemoveUnitPosition(unit.currentLocation);
					unit.gameObject.SetActive(true);
					unit.HideUnit();
					Vector3 sixFeetUnder = transform.position;
					sixFeetUnder.y -= 6f;
					unit.transform.position = sixFeetUnder;
					unit.unitRigidbody.useGravity = false;
					unit.isDead = true;
					enemyCamp.DeadList.Add(enemyCamp.benchedUnit);
				}
			}
		}
	}

	//allowing sounds and effects to play before turhing off
	public IEnumerator SetInactiveWait()
	{
		yield return attackPauses[0];

		gameObject.SetActive(false);
	}

	public UnitData SaveMilitaryUnitData()
	{
		UnitData data = new();

		data.id = id;
		data.unitNameAndLevel = buildDataSO.unitNameAndLevel;
		data.position = transform.position;
		data.rotation = transform.rotation;
		data.destinationLoc = destinationLoc;
		data.finalDestinationLoc = finalDestinationLoc;
		data.currentLocation = currentLocation;
		data.moveOrders = pathPositions.ToList();
		data.isMoving = isMoving;
		data.ambush = ambush;
		data.guard = guard;
		data.isGuarding = isGuarding;
		data.atSea = atSea;
		data.posSet = posSet;
		data.switchLocs = switchLocs;

		if (isMoving && readyToMarch)
			data.moveOrders.Insert(0, world.RoundToInt(destinationLoc));

		data.isUpgrading = isUpgrading;
		data.upgradeLevel = upgradeLevel;
		//data.looking = looking;

		//combat
		if (buildDataSO.inMilitary || enemyAI || bodyGuard)
		{
			if (buildDataSO.inMilitary || bodyGuard)
			{
				if (!guard && !bodyGuard)
					data.cityHomeBase = army.city.cityLoc;
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
			data.targetSearching = targetSearching;
			data.flanking = flanking;
			data.flankedOnce = flankedOnce;
			data.cavalryLine = cavalryLine;
			data.isDead = isDead;
			data.benched = benched;
			data.duelWatch = duelWatch;
		}

		if (leader)
			data.leaderData = leader.SaveMilitaryLeaderData();

		if (bodyGuard)
			data.bodyGuardData = bodyGuard.SaveBodyGuardData();

		return data;
	}

	public void LoadUnitData(UnitData data)
	{
		id = data.id;
		transform.position = data.position;
		transform.rotation = data.rotation;
		destinationLoc = data.destinationLoc;
		finalDestinationLoc = data.finalDestinationLoc;
		currentLocation = data.currentLocation;
		isMoving = data.isMoving;
		isUpgrading = data.isUpgrading;
		upgradeLevel = data.upgradeLevel;
		//looking = data.looking;
		ambush = data.ambush;
		guard = data.guard;
		isGuarding = data.isGuarding;
		atSea = data.atSea;
		posSet = data.posSet;
		switchLocs = data.switchLocs;

		if (posSet)
			world.AddUnitPosition(currentLocation, this);

		if (isUpgrading)
			GameLoader.Instance.unitUpgradeList.Add(this);

		//if (!isMoving && !data.benched && !data.duelWatch && !data.isDead)
		//	world.AddUnitPosition(currentLocation, this);

		if (buildDataSO.inMilitary || enemyAI || bodyGuard)
		{
			if (buildDataSO.inMilitary)
			{
				if (!data.atHome && !bySea && !byAir)
					outline.ToggleOutline(true);

				transferring = data.transferring;
			}
			else if (bodyGuard)
			{
				transferring = data.transferring;
			}
			else
			{
				enemyAI.CampSpot = data.campSpot;
			}

			strengthBonus = data.strengthBonus;
			repositioning = data.repositioning;
			barracksBunk = data.barracksBunk;
			marchPosition = data.marchPosition;
			targetBunk = data.targetBunk;
			currentHealth = data.currentHealth;
			benched = data.benched;
			duelWatch = data.duelWatch;

			if (currentHealth < healthMax)
			{
				healthbar.LoadHealthLevel(currentHealth);
				healthbar.gameObject.SetActive(true);

				if (bodyGuard)
				{
					if (!bodyGuard.dueling && !bodyGuard.inTransport)
						healthbar.RegenerateHealth();
				}
				else if (enemyAI && !ambush && !data.attacking && !data.inBattle && !repositioning)
				{
					healthbar.RegenerateHealth();
				}
				else if (buildDataSO.inMilitary)
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
			targetSearching = data.targetSearching;
			flanking = data.flanking;
			flankedOnce = data.flankedOnce;
			cavalryLine = data.cavalryLine;
			isDead = data.isDead;

			if (leader)
			{
				if (leader.defending || leader.dueling)
					GameLoader.Instance.attackingUnitList.Add(this);
				else if (leader.isDead)
					gameObject.SetActive(false);
			}
			else if (!enemyAI || ambush || enemyCamp.campCount != 0)
			{
				GameLoader.Instance.attackingUnitList.Add(this);
			}
		}

		if (isMoving)
		{
			Vector3Int endPosition = world.RoundToInt(finalDestinationLoc);

			if (isMarching && !readyToMarch)
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
		//else if (looking)
		//{
		//	StartLookingAround();
		//}
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
						if (enemyAmbush.attackedUnits[i].trader && enemyAmbush.targetTrader)
						{
							target = enemyAmbush.attackedUnits[i];
							break;
						}
						else if (enemyAmbush.attackedUnits[i].military && enemyAmbush.attackedUnits[i].military.barracksBunk == targetBunk)
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
				List<Military> units = army.targetCamp.UnitsInCamp;

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
				if (buildDataSO.inMilitary || bodyGuard)
				{
					AggroCheck();
				}
				else if (ambush)
				{
					if (enemyAmbush.attackedUnits.Count > 0)
						enemyAI.AmbushAggro(world.RoundToInt(enemyAmbush.attackedUnits[enemyAmbush.attackedUnits.Count - 1].transform.position), world.RoundToInt(transform.position));
					else
						StartCoroutine(DramaticallyDisappear());
				}
				else
				{
					enemyAI.AggroCheck();
				}

				return;
			}

			if (buildDataSO.inMilitary || bodyGuard)
			{
				if (buildDataSO.unitType == UnitType.Ranged)
					attackCo = StartCoroutine(RangedAttack(target));
				else
					attackCo = StartCoroutine(Attack(target));
			}
			else
			{
				enemyAI.LoadAttack(buildDataSO.unitType == UnitType.Ranged, target);
			}
		}
		else if (isDead)
		{
			if (leader)
			{
				gameObject.SetActive(false);
				return;
			}
			
			unitMesh.SetActive(false);
			healthbar.gameObject.SetActive(false);
			//marker.gameObject.SetActive(false);
			Vector3 sixFeetUnder = transform.position;
			sixFeetUnder.y -= 6;
			transform.position = sixFeetUnder;
			unitRigidbody.useGravity = false;

			if (enemyAI)
			{
				enemyCamp.deathCount++;
				if (enemyCamp.attackingArmy != null)
					enemyCamp.attackingArmy.attackingSpots.Remove(currentLocation);
				//enemyCamp.ClearCampCheck();
				//world.RemoveUnitPosition(currentLocation);
				enemyCamp.DeadList.Add(this);
			}
			else
			{
				minimapIcon.gameObject.SetActive(false);
				army.UnitsInArmy.Remove(this);
				army.attackingSpots.Remove(currentLocation);
				RemoveUnitFromData();
				army.RemoveFromArmy(this, barracksBunk, false);
				army.DeadList.Add(this);
				//world.RemoveUnitPosition(currentLocation);
			}
		}
		else if (benched)
		{
			gameObject.SetActive(false);
		}
		else if (inBattle && !isMoving)
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
