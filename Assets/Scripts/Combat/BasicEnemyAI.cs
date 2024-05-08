using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class BasicEnemyAI : MonoBehaviour
{
    //private Vector3Int campLoc;
    //public Vector3Int CampLoc { get { return campLoc; } set { campLoc = value; } }
    private Vector3Int campSpot;
    public Vector3Int CampSpot { get { return campSpot; } set { campSpot = value; } } 
    private Military unit;


	private void Awake()
	{
        unit = GetComponent<Military>();
    }

	public void AggroCheck()
	{
		if (unit.attacking)
			return;
		
		UnitType type = unit.buildDataSO.unitType;

		if (!unit.enemyCamp.FinishAttack())
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
		unit.targetSearching = false;

		List<Vector3Int> attackingZones = new();

		Vector3Int forward = unit.enemyCamp.forward;
		Vector3Int forwardTile = forward + unit.currentLocation;

		attackingZones.Add(forwardTile);
		if (forward.z != 0)
		{
			attackingZones.Add(new Vector3Int(1, 0, forward.z) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, forward.z) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(1, 0, 0) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, 0) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(1, 0, -forward.z) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, -forward.z) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(0, 0, -forward.z) + unit.currentLocation);
		}
		else
		{
			attackingZones.Add(new Vector3Int(forward.x, 0, 1) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(forward.x, 0, -1) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(0, 0, 1) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(0, 0, -1) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(-forward.x, 0, 1) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(-forward.x, 0, -1) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(-forward.x, 0, 0) + unit.currentLocation);
		}

		//just in case
		if (unit.enemyCamp.attackingArmy == null)
			return;

		foreach (Vector3Int zone in attackingZones)
		{
			if (!unit.enemyCamp.attackingArmy.movementRange.Contains(zone))
				continue;

			if (unit.world.IsUnitLocationTaken(zone))
			{
				Unit enemy = unit.world.GetUnit(zone);
				if (enemy.inArmy)
				{
					//unit.attacking = true;
					if (!unit.enemyCamp.attackingArmy.attackingSpots.Contains(unit.currentLocation))
						unit.enemyCamp.attackingArmy.attackingSpots.Add(unit.currentLocation);

					if (!unit.attacking)
					{
						StartAttack(enemy);
					}
					else
					{
						if (enemy.military && enemy.military.targetSearching)
							enemy.military.StartAttack(unit);
					}

				}
			}
		}

		if (unit.attacking)
			return;

		Military newEnemy = unit.enemyCamp.FindClosestTarget(unit);
		unit.targetLocation = newEnemy.currentLocation;
		List<Vector3Int> path = unit.enemyCamp.PathToEnemy(unit.currentLocation, unit.world.RoundToInt(newEnemy.transform.position));

		if (path.Count > 0)
		{
			//moving unit behind if stuck
			Vector3Int positionBehind = unit.enemyCamp.forward * -1 + unit.currentLocation;
			unit.enemyCamp.attackingArmy.attackingSpots.Remove(unit.currentLocation);

			if (path.Count >= 2)
			{
				List<Vector3Int> shortPath = new() { path[0] };
				unit.finalDestinationLoc = shortPath[0];
				unit.MoveThroughPath(shortPath);
				unit.enemyCamp.attackingArmy.attackingSpots.Add(path[0]);

				if (unit.world.IsUnitLocationTaken(positionBehind))
				{
					Unit unitBehind = unit.world.GetUnit(positionBehind);
					if (unitBehind.enemyAI && unitBehind.military.targetSearching)
						unitBehind.enemyAI.AggroCheck();
				}
			}
			else if (path.Count == 1)
				StartAttack(newEnemy);
		}
		else //a little redundant, in case path ends up being zero again
		{
			Vector3Int scoochCloser = new Vector3Int(Math.Sign(unit.targetLocation.x - unit.currentLocation.x), 0, Math.Sign(unit.targetLocation.z - unit.currentLocation.z)) + unit.currentLocation;

			if (unit.enemyCamp.attackingArmy.movementRange.Contains(scoochCloser) && !unit.world.IsUnitLocationTaken(scoochCloser) && !unit.enemyCamp.attackingArmy.attackingSpots.Contains(scoochCloser))
			{
				unit.finalDestinationLoc = scoochCloser;
				//List<Vector3Int> newPath = new() { scoochCloser };
				unit.MoveThroughPath(new List<Vector3Int> { scoochCloser });
			}

			unit.targetSearching = true;
		}
	}

	public void RangedAggroCheck()
	{
		Military enemy = unit.enemyCamp.FindClosestTarget(unit);

		if (enemy != null)
			unit.attackCo = StartCoroutine(RangedAttack(enemy));
	}

	public void CavalryAggroCheck()
	{
		unit.targetSearching = false;

		List<Vector3Int> attackingZones = new();

		Vector3Int forward = unit.enemyCamp.forward;
		Vector3Int forwardTile = forward + unit.currentLocation;

		attackingZones.Add(forwardTile);
		//check the sides too
		if (forward.z != 0)
		{
			attackingZones.Add(new Vector3Int(1, 0, forward.z) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, forward.z) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(1, 0, 0) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, 0) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(1, 0, -forward.z) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, -forward.z) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(0, 0, -forward.z) + unit.currentLocation);
		}
		else
		{
			attackingZones.Add(new Vector3Int(forward.x, 0, 1) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(forward.x, 0, -1) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(0, 0, 1) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(0, 0, -1) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(-forward.x, 0, 1) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(-forward.x, 0, -1) + unit.currentLocation);
			attackingZones.Add(new Vector3Int(-forward.x, 0, 0) + unit.currentLocation);
		}

		if (unit.enemyCamp.attackingArmy == null)
			return;

		foreach (Vector3Int zone in attackingZones)
		{
			if (!unit.enemyCamp.attackingArmy.movementRange.Contains(zone))
				continue;

			if (unit.world.IsUnitLocationTaken(zone))
			{
				Unit enemy = unit.world.GetUnit(zone);
				if (enemy.inArmy)
				{
					if (!unit.enemyCamp.attackingArmy.attackingSpots.Contains(unit.currentLocation))
						unit.enemyCamp.attackingArmy.attackingSpots.Add(unit.currentLocation);

					if (!unit.attacking)
					{
						if (unit.flanking)
						{
							foreach (Military unit in unit.enemyCamp.attackingArmy.UnitsInArmy)
							{
								if (unit.targetSearching)
									unit.AggroCheck();
							}
						}
						
						unit.flankedOnce = true; //can't flank if attacking front lines
						unit.flanking = false;
						StartAttack(enemy);
					}
					else
					{
						if (enemy.military && enemy.military.targetSearching)
							enemy.military.StartAttack(unit);
					}
				}
			}
		}

		if (unit.attacking)
			return;

		Military newEnemy = null;

		if (!unit.flankedOnce) //only one flank per battle
		{
			unit.flankedOnce = true;

			if (unit.world.IsUnitLocationTaken(forwardTile) && unit.world.GetUnit(forwardTile).enemyAI)
				newEnemy = unit.enemyCamp.FindEdgeRanged(unit.currentLocation);
		}
		else
		{
			unit.flanking = false;
		}

		if (newEnemy == null)
			newEnemy = unit.enemyCamp.FindClosestTarget(unit);
		else
			unit.flanking = true;

		unit.targetLocation = newEnemy.currentLocation;
		List<Vector3Int> path = unit.enemyCamp.CavalryPathToEnemy(unit.currentLocation, unit.world.RoundToInt(newEnemy.transform.position));

		if (path.Count > 0)
		{
			unit.enemyCamp.attackingArmy.attackingSpots.Remove(unit.currentLocation);

			//moving unit behind if stuck
			Vector3Int positionBehind = unit.enemyCamp.forward * -1 + unit.currentLocation;

			if (unit.world.IsUnitLocationTaken(positionBehind))
			{
				Unit unitBehind = unit.world.GetUnit(positionBehind);
				if (unitBehind.enemyAI && unitBehind.military.targetSearching)
					unitBehind.enemyAI.AggroCheck();
			}

			if (unit.flanking)
			{
				if (path.Count > 1)
					path.RemoveAt(path.Count - 1); //remove last one
				unit.finalDestinationLoc = path[path.Count - 1];
				unit.MoveThroughPath(path);
			}
			else if (path.Count >= 2)
			{
				List<Vector3Int> shortPath = new() { path[0] };
				unit.finalDestinationLoc = shortPath[0];
				unit.MoveThroughPath(shortPath);
				unit.enemyCamp.attackingArmy.attackingSpots.Add(path[0]);
			}
			else if (path.Count == 1)
				StartAttack(newEnemy);
		}
		else
		{
			Vector3Int scoochCloser = new Vector3Int(Math.Sign(unit.targetLocation.x - unit.currentLocation.x), 0, Math.Sign(unit.targetLocation.z - unit.currentLocation.z)) + unit.currentLocation;

			if (unit.enemyCamp.attackingArmy.cavalryRange.Contains(scoochCloser) && !unit.world.IsUnitLocationTaken(scoochCloser) && !unit.enemyCamp.attackingArmy.attackingSpots.Contains(scoochCloser))
			{
				unit.finalDestinationLoc = scoochCloser;
				//List<Vector3Int> newPath = new() { scoochCloser };
				unit.MoveThroughPath(new List<Vector3Int> { scoochCloser });
			}

			unit.targetSearching = true;
		}
	}

	public void AmbushAggro(Vector3Int endPosition, Vector3Int ambushLoc)
	{
		List<Vector3Int> avoidList = new() { ambushLoc };
		List<Vector3Int> path = GridSearch.EnemyMove(unit.world, transform.position, endPosition, unit.bySea, avoidList);

		if (path.Count > 1)
		{
			unit.finalDestinationLoc = path[path.Count - 1];
			unit.MoveThroughPath(path);
		}
		else if (path.Count == 1)
		{
			StartAttack(unit.world.GetUnit(path[0]));
		}
	}

	public void StartAttack(Unit target)
	{
		unit.targetSearching = false;
		unit.Rotate(target.transform.position);

		if (unit.attackCo == null)
			unit.attackCo = StartCoroutine(Attack(target));
    }

    public IEnumerator Attack(Unit target)
    {
		if (target.trader)
			unit.military.enemyAmbush.targetTrader = true;
		else
			unit.targetBunk = target.military.barracksBunk;

		unit.attacking = true;
		float dist = 0;
		float distThreshold;

		if (target.trader)
			target.GetComponent<Trader>().LookSad();

		if (Mathf.Abs(target.transform.position.x - unit.transform.position.x) + Mathf.Abs(target.transform.position.z - unit.transform.position.z) < 1.2f)
			distThreshold = 1.2f;
		else
			distThreshold = 2.2f;

		if (target.military && target.military.targetSearching)
			target.military.StartAttack(unit);

		int wait = UnityEngine.Random.Range(0, 3);
		if (wait != 0)
			yield return unit.attackPauses[wait];

		while (!unit.isDead && target.currentHealth > 0 && dist < distThreshold)
        {
			unit.transform.rotation = Quaternion.LookRotation(target.transform.position - unit.transform.position);
			unit.StartAttackingAnimation();
			yield return unit.attackPauses[2];
			if (!target.isDead)
				target.ReduceHealth(unit, unit.attacks[UnityEngine.Random.Range(0,unit.attacks.Length)]);
			yield return unit.attackPauses[0];
			if (!target.isDead)
				dist = Mathf.Abs(target.transform.position.x - unit.transform.position.x) + Mathf.Abs(target.transform.position.z - unit.transform.position.z);
			else
				dist = distThreshold;
        }

		unit.attacking = false;
		if (unit.ambush)
		{
			if (!unit.isDead) //just in case
			{
				unit.enemyAmbush.attackedUnits.Remove(target);
				unit.attackCo = null;
				unit.StopMovementCheck(true);
				//unit.StopAnimation();

				if (unit.enemyAmbush.attackedUnits.Count > 0)
				{
					Vector3 nextLoc = unit.enemyAmbush.attackedUnits[0].transform.position;
					Vector3 loc = unit.transform.position;

					if (Mathf.Abs(loc.x - nextLoc.x) < 1.3f && Mathf.Abs(loc.z - nextLoc.z) < 1.3f)
						StartAttack(unit.enemyAmbush.attackedUnits[0]);
					else
						AmbushAggro(unit.world.RoundToInt(nextLoc), unit.world.RoundToInt(loc));
				}
				else
				{
					if (unit.world.tutorial && unit.world.ambushes == 1)
					{
						unit.world.tutorialGoing = true;
						unit.world.TutorialCheck("Ambush");
					}

					StartCoroutine(unit.DramaticallyDisappear());
				}
			}
		}
		else if (unit.enemyCamp.attackingArmy == null)
		{
			if (!unit.enemyCamp.movingOut && !unit.repositioning) //pillage done else where
				StartReturn();
		}
		else if (dist >= distThreshold && unit.enemyCamp.attackingArmy.returning)
		{
            unit.enemyCamp.TargetSearchCheck();
			if (!unit.repositioning)
	            StartReturn();
		}
		else if (!unit.isDead && unit.inBattle) //just in case
		{
			unit.attackCo = null;
			unit.StopMovementCheck(true);
			AggroCheck();
		}
    }

	public void LoadAttack(bool ranged, Unit target)
	{
		if (ranged)
			unit.attackCo = StartCoroutine(RangedAttack(target));
		else
			unit.attackCo = StartCoroutine(Attack(target));	
	}

	public IEnumerator RangedAttack(Unit target)
	{
		unit.targetBunk = target.military.barracksBunk;
		unit.attacking = true;
		float dist = 0;
		unit.Rotate(target.transform.position);
		float distThreshold = 7.5f;

		int wait = UnityEngine.Random.Range(0, 3);
		if (wait != 0)
			yield return unit.attackPauses[wait];

		while (!unit.isDead && target.currentHealth > 0 && dist < distThreshold)
		{
			unit.StartAttackingAnimation();
			yield return unit.attackPauses[2];
			if (!target.isDead)
			{
				unit.projectile.SetPoints(transform.position, target.transform.position);
				StartCoroutine(unit.projectile.Shoot(unit, target));
			}
			yield return unit.attackPauses[0];
			if (!target.isDead)
				dist = Mathf.Abs(target.transform.position.x - unit.transform.position.x) + Mathf.Abs(target.transform.position.z - unit.transform.position.z);
			else
				dist = distThreshold;
        }

		unit.attacking = false;
		if (dist >= 7.5 && unit.enemyCamp.attackingArmy.returning)
		{
            unit.enemyCamp.TargetSearchCheck();
			if (!unit.repositioning)
	            StartReturn();
		}
		else if (!unit.isDead)
		{
			unit.attackCo = null;
			unit.StopAttackAnimation();
			AggroCheck();
		}
	}

	public void StartReturn()
    {
		if (unit.isDead)
			return;

		unit.StopAttacking(false);
		AttackCheck();

		unit.inBattle = false;
		unit.attacking = false;
		unit.targetSearching = false;
		unit.flanking = false;
		unit.flankedOnce = false;
		unit.repositioning = true;

		//unit.StopMovementCheck(false);

		unit.finalDestinationLoc = campSpot;
		List<Vector3Int> path;
		if (unit.enemyCamp.battleAtSea)
			path = GridSearch.MoveWherever(unit.world, unit.transform.position, campSpot);
		else
			path = GridSearch.EnemyMove(unit.world, unit.transform.position, campSpot, unit.bySea, null);

		if (path.Count == 0 && (campSpot - unit.transform.position).sqrMagnitude > 0.05f)
			path.Add(campSpot);

		if (path.Count > 0)
		{
			unit.MoveThroughPath(path);
		}
		else
		{
			
			if (unit.leader)
			{
				unit.repositioning = false;
			}
			else
			{
				unit.FinishMoving(unit.transform.position);
				//unit.enemyCamp.EnemyReturn(unit);

				//if (unit.currentHealth < unit.buildDataSO.health)
				//	unit.healthbar.RegenerateHealth();
			}
		}
	}

	public void AttackCheck()
	{
		if (unit.attackCo != null)
		{
			StopCoroutine(unit.attackCo);
			unit.attackCo = null;
		}
	}
}
