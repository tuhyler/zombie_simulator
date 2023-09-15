using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class BasicEnemyAI : MonoBehaviour
{
    private Vector3Int campLoc;
    public Vector3Int CampLoc { get { return campLoc; } set { campLoc = value; } }
    private Vector3Int campSpot;
    public Vector3Int CampSpot { get { return campSpot; } set { campSpot = value; } } 
    private Unit unit;
    //private State state;
    //[HideInInspector]
    //public bool needsDestination = true;
    //private Vector3Int currentDestination;
    //private float attackSpeed;

    //private Unit targetUnit;

    //private WaitForSeconds attackPause;


	private void Awake()
	{
        unit = GetComponent<Unit>();
    }

	//private void Update()
	//{
	//	switch (state)
	//	{
 //           default:
 //           case State.Camping:
	//			if (needsDestination)
 //               {
 //                   needsDestination = false;
 //                   //AggroCheck();
	//				//StartRoaming();
 //               }
	//			break;
	//		case State.Chasing:
 //               //ResetTarget();
 //               if (needsDestination)
 //                   StartChase();

	//			if (Mathf.Abs(transform.position.x - targetUnit.transform.position.x) + Mathf.Abs(transform.position.z - targetUnit.transform.position.z) < 1)
 //               {
 //                   StopMovement();
 //                   state = State.Attacking;
	//				unit.world.AddUnitPosition(transform.position, unit);
 //               }
                
 //               if (Mathf.Abs(transform.position.x - campLoc.x) + Mathf.Abs(transform.position.z - campLoc.z) > 10)
 //               {
 //                   StopMovement();
 //                   state = State.Returning;
 //               }
	//			break;
	//		case State.Attacking:
 //               if (needsDestination)
 //               {
 //                   unit.transform.LookAt(targetUnit.transform);
 //                   StartAttack();
 //               }

	//			if (Mathf.Abs(transform.position.x - targetUnit.transform.position.x) + Mathf.Abs(transform.position.z - targetUnit.transform.position.z) > 1.5f)
 //               {
 //                   if (attackCo != null)
 //                   {
 //                       StopCoroutine(attackCo);
 //                   }
 //                   StopMovement();
 //                   state = State.Returning;
 //               }

 //               if (targetUnit.isDead)
 //               {
	//				StopMovement();
	//				if (attackCo != null)
	//					StopCoroutine(attackCo);

	//				state = State.Returning;
	//				targetUnit = null;
	//				AggroCheck();
	//			}
	//			break;
	//		case State.Hunting:
	//			break;
	//		case State.Returning:
 //               if (needsDestination)
 //               {
 //                   StopMovement();
 //                   if (attackCo != null)
	//					StopCoroutine(attackCo);
	//				StartReturn();
 //               }
	//			break;
	//	}
	//}

    //public void StateCheck(MapWorld world)
    //{
    //    if (state == State.Returning)
    //    {
    //        state = State.Camping;
    //    }
    //}

 //   public void SetAttackSpeed(float speed)
 //   {
 //       attackSpeed = speed;
 //       attackPause = new(attackSpeed);
	//}

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
		Vector3Int forwardTile = forward + unit.CurrentLocation;

		attackingZones.Add(forwardTile);
		if (forward.z != 0)
		{
			attackingZones.Add(new Vector3Int(1, 0, forward.z) + unit.CurrentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, forward.z) + unit.CurrentLocation);
		}
		else
		{
			attackingZones.Add(new Vector3Int(forward.x, 0, 1) + unit.CurrentLocation);
			attackingZones.Add(new Vector3Int(forward.x, 0, -1) + unit.CurrentLocation);
		}

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
					if (!unit.enemyCamp.attackingArmy.attackingSpots.Contains(unit.CurrentLocation))
						unit.enemyCamp.attackingArmy.attackingSpots.Add(unit.CurrentLocation);

					if (!unit.attacking)
					{

						StartAttack(enemy);
					}
					else
					{
						if (enemy.targetSearching)
							enemy.StartAttack(unit);
					}

				}
			}
		}

		if (unit.attacking)
			return;

		Unit newEnemy = unit.enemyCamp.FindClosestTarget(unit);
		unit.targetLocation = newEnemy.CurrentLocation;
		List<Vector3Int> path = unit.enemyCamp.PathToEnemy(unit.CurrentLocation, unit.world.RoundToInt(newEnemy.transform.position));

		if (path.Count > 0)
		{
			//moving unit behind if stuck
			Vector3Int positionBehind = unit.enemyCamp.forward * -1 + unit.CurrentLocation;
			unit.enemyCamp.attackingArmy.attackingSpots.Remove(unit.CurrentLocation);

			if (path.Count >= 2)
			{
				List<Vector3Int> shortPath = new() { path[0] };
				unit.finalDestinationLoc = shortPath[0];
				unit.MoveThroughPath(shortPath);
				unit.enemyCamp.attackingArmy.attackingSpots.Add(path[0]);

				if (unit.world.IsUnitLocationTaken(positionBehind))
				{
					Unit unitBehind = unit.world.GetUnit(positionBehind);
					if (unitBehind.enemyAI && unitBehind.targetSearching)
						unitBehind.enemyAI.AggroCheck();
				}
			}
			else if (path.Count == 1)
				StartAttack(newEnemy);
		}
		else //a little redundant, in case path ends up being zero again
		{
			Vector3Int scoochCloser = new Vector3Int(Math.Sign(unit.targetLocation.x - unit.CurrentLocation.x), 0, Math.Sign(unit.targetLocation.z - unit.CurrentLocation.z)) + unit.CurrentLocation;

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
		Unit enemy = unit.enemyCamp.FindClosestTarget(unit);

		if (enemy != null)
			unit.attackCo = StartCoroutine(RangedAttack(enemy));
	}

	public void CavalryAggroCheck()
	{
		unit.targetSearching = false;

		List<Vector3Int> attackingZones = new();

		Vector3Int forward = unit.enemyCamp.forward;
		Vector3Int forwardTile = forward + unit.CurrentLocation;

		attackingZones.Add(forwardTile);
		//check the sides too
		if (forward.z != 0)
		{
			attackingZones.Add(new Vector3Int(1, 0, forward.z) + unit.CurrentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, forward.z) + unit.CurrentLocation);
			attackingZones.Add(new Vector3Int(1, 0, 0) + unit.CurrentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, 0) + unit.CurrentLocation);
		}
		else
		{
			attackingZones.Add(new Vector3Int(forward.x, 0, 1) + unit.CurrentLocation);
			attackingZones.Add(new Vector3Int(forward.x, 0, -1) + unit.CurrentLocation);
			attackingZones.Add(new Vector3Int(0, 0, 1) + unit.CurrentLocation);
			attackingZones.Add(new Vector3Int(0, 0, -1) + unit.CurrentLocation);
		}

		foreach (Vector3Int zone in attackingZones)
		{
			if (!unit.enemyCamp.attackingArmy.movementRange.Contains(zone))
				continue;

			if (unit.world.IsUnitLocationTaken(zone))
			{
				Unit enemy = unit.world.GetUnit(zone);
				if (enemy.inArmy)
				{
					if (!unit.enemyCamp.attackingArmy.attackingSpots.Contains(unit.CurrentLocation))
						unit.enemyCamp.attackingArmy.attackingSpots.Add(unit.CurrentLocation);

					if (!unit.attacking)
					{
						if (unit.flanking)
						{
							foreach (Unit unit in unit.enemyCamp.attackingArmy.UnitsInArmy)
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
						if (enemy.targetSearching)
							enemy.StartAttack(unit);
					}
				}
			}
		}

		if (unit.attacking)
			return;

		Unit newEnemy = null;

		if (!unit.flankedOnce) //only one flank per battle
		{
			unit.flankedOnce = true;

			if (unit.world.IsUnitLocationTaken(forwardTile) && unit.world.GetUnit(forwardTile).enemyAI)
				newEnemy = unit.enemyCamp.FindEdgeRanged(unit.CurrentLocation);
		}

		if (newEnemy == null)
			newEnemy = unit.enemyCamp.FindClosestTarget(unit);
		else
			unit.flanking = true;

		unit.targetLocation = newEnemy.CurrentLocation;
		List<Vector3Int> path = unit.enemyCamp.CavalryPathToEnemy(unit.CurrentLocation, unit.world.RoundToInt(newEnemy.transform.position));

		if (path.Count > 0)
		{
			unit.enemyCamp.attackingArmy.attackingSpots.Remove(unit.CurrentLocation);

			//moving unit behind if stuck
			Vector3Int positionBehind = unit.enemyCamp.forward * -1 + unit.CurrentLocation;

			if (unit.world.IsUnitLocationTaken(positionBehind))
			{
				Unit unitBehind = unit.world.GetUnit(positionBehind);
				if (unitBehind.enemyAI && unitBehind.targetSearching)
					unitBehind.enemyAI.AggroCheck();
			}

			if (unit.flanking)
			{
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
			Vector3Int scoochCloser = new Vector3Int(Math.Sign(unit.targetLocation.x - unit.CurrentLocation.x), 0, Math.Sign(unit.targetLocation.z - unit.CurrentLocation.z)) + unit.CurrentLocation;

			if (unit.enemyCamp.attackingArmy.cavalryRange.Contains(scoochCloser) && !unit.world.IsUnitLocationTaken(scoochCloser) && !unit.enemyCamp.attackingArmy.attackingSpots.Contains(scoochCloser))
			{
				unit.finalDestinationLoc = scoochCloser;
				//List<Vector3Int> newPath = new() { scoochCloser };
				unit.MoveThroughPath(new List<Vector3Int> { scoochCloser });
			}

			unit.targetSearching = true;
		}
	}


	//public void WakeUp(Unit targetUnit)
	//{
	//    if (state == State.Camping)
	//    {
	//        //Debug.Log("wake up");
	//        if (roamCo != null)
	//            StopCoroutine(roamCo);
	//        this.targetUnit = targetUnit;
	//        StopMovement();
	//        state = State.Chasing;
	//    }
	//}

	//public void StopMovement()
	//   {
	//	if (unit.isMoving)
	//	{
	//		unit.StopAnimation();
	//		unit.ShiftMovement();
	//	}
	//}

	//public bool ResetTarget()
	//{
	//    if (unit.world.RoundToInt(targetUnit.transform.position) != currentDestination)
	//    {
	//        StopMovement();
	//        StartChase();
	//        return true;
	//    }

	//    return false;
	//}

	//   public void StartChase()
	//   {
	//   	needsDestination = false;
	//       List<Vector3Int> path = GetPath(targetUnit.transform.position);

	//       if (path.Count == 0)
	//       {
	//           needsDestination = true;
	//		return;
	//       }

	//   	unit.MoveThroughPath(path);
	//}

	//   public void FollowLeader(List<Vector3Int> path, Vector3Int leaderDiff, Unit targetUnit)
	//   {
	//       StopMovement();
	//       if (path.Count == 0)
	//       {
	//           needsDestination = true;
	//           return;
	//       }

	//       List<Vector3Int> newPath = new();

	//       foreach (Vector3Int tile in path)
	//       {
	//           newPath.Add(tile + leaderDiff);
	//       }

	//       needsDestination = false;
	//       this.targetUnit = targetUnit;
	//       state = State.Chasing;

	//	unit.finalDestinationLoc = newPath[newPath.Count - 1];
	//	currentDestination = newPath[newPath.Count - 1];

	//	unit.MoveThroughPath(newPath);
	//}

	public void StartAttack(Unit target)
	{
		unit.targetSearching = false;
		unit.Rotate(target.transform.position);

		if (unit.attackCo == null)
			unit.attackCo = StartCoroutine(Attack(target));
    }

    public IEnumerator Attack(Unit target)
    {
		unit.attacking = true;
		float dist = 0;
		float distThreshold;

		if (Mathf.Abs(target.transform.position.x - unit.transform.position.x) + Mathf.Abs(target.transform.position.z - unit.transform.position.z) < 1.2f)
			distThreshold = 1.2f;
		else
			distThreshold = 2.2f;

		if (target.targetSearching)
			target.StartAttack(unit);
		
		while (!unit.isDead && target.currentHealth > 0 && dist < distThreshold)
        {
			unit.transform.rotation = Quaternion.LookRotation(target.transform.position - unit.transform.position);
			unit.StartAttackingAnimation();
			yield return unit.attackPauses[3];
	        target.ReduceHealth(unit.attackStrength, unit.transform.eulerAngles);
			yield return unit.attackPauses[UnityEngine.Random.Range(0,3)];
			dist = Mathf.Abs(target.transform.position.x - unit.transform.position.x) + Mathf.Abs(target.transform.position.z - unit.transform.position.z);
        }

		unit.attacking = false;
		if (unit.enemyCamp.attackingArmy == null)
		{
			StartReturn();
		}
		else if (dist >= distThreshold && unit.enemyCamp.attackingArmy.returning)
		{
            unit.enemyCamp.TargetSearchCheck();
            StartReturn();
		}
		else if (!unit.isDead) //won't let me stop coroutine for enemies
		{
			unit.attackCo = null;
			unit.StopMovement();
			unit.StopAnimation();
			AggroCheck();
		}
    }

	private IEnumerator RangedAttack(Unit target)
	{
		unit.attacking = true;
		float dist = 0;
		unit.Rotate(target.transform.position);

		while (!unit.isDead && target.currentHealth > 0 && dist < 7.5)
		{
			unit.StartAttackingAnimation();
			yield return unit.attackPauses[3];
			unit.projectile.SetPoints(transform.position, target.transform.position);
			StartCoroutine(unit.projectile.Shoot(unit, target));
			yield return unit.attackPauses[UnityEngine.Random.Range(0,3)];
			dist = Mathf.Abs(target.transform.position.x - unit.transform.position.x) + Mathf.Abs(target.transform.position.z - unit.transform.position.z);
        }

		unit.attacking = false;
		if (dist >= 7.5 && unit.enemyCamp.attackingArmy.returning)
		{
            unit.enemyCamp.TargetSearchCheck();
            StartReturn();
		}
		else if (!unit.isDead)
		{
			unit.attackCo = null;
			unit.StopAnimation();
			AggroCheck();
		}
	}

	public void StartReturn()
    {
		//unit.StopAttacking();
		if (unit.attackCo != null)
			StopCoroutine(unit.attackCo);

		unit.attackCo = null;
		unit.inBattle = false;
		unit.attacking = false;
		unit.targetSearching = false;
		unit.flanking = false;
		unit.flankedOnce = false;
		unit.repositioning = true;

		if (unit.isMoving)
		{
			unit.StopAnimation();
			unit.ShiftMovement();
		}

		unit.finalDestinationLoc = campSpot;
		List<Vector3Int> path = GridSearch.AStarSearch(unit.world, unit.world.RoundToInt(unit.transform.position), campSpot, unit.isTrader, unit.bySea, true);

		if (path.Count > 0)
		{
			unit.MoveThroughPath(path);
		}
		else
		{
			unit.enemyCamp.EnemyReturn(unit);

			if (unit.currentHealth < unit.buildDataSO.health)
				unit.healthbar.RegenerateHealth();
		}
	}

  //  public void Converge()
  //  {
		//unit.StopAnimation();
		//unit.ShiftMovement();
		//EnemyMove(targetUnit.transform.position, true);
  //  }

	//private void EnemyMove(Vector3 destination, bool attack)
 //   {
 //       unit.finalDestinationLoc = destination;
 //       currentDestination = unit.world.RoundToInt(destination);
 //       //Debug.Log(currentDestination);
 //       List<Vector3Int> path = GridSearch.AStarSearch(unit.world, unit.world.RoundToInt(unit.transform.position), currentDestination, unit.isTrader, unit.bySea);
		
 //       if (path.Count == 0)
 //       {
 //           needsDestination = true;
 //           return;
 //       }

 //       unit.MoveThroughPath(path);
	//}

 //   private List<Vector3Int> GetPath(Vector3 destination)
 //   {
	//	unit.finalDestinationLoc = destination;
	//	currentDestination = unit.world.RoundToInt(destination);

	//	return GridSearch.AStarSearch(unit.world, unit.world.RoundToInt(unit.transform.position), currentDestination, unit.isTrader, unit.bySea);
	//}



    //private enum State
    //{
    //    Camping,
    //    Chasing,
    //    Attacking,
    //    Returning,
    //    Hunting
    //}
}
