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
    private Vector3Int currentDestination;
    private float attackSpeed;

    //private Unit targetUnit;

    private WaitForSeconds attackPause;


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

    public void SetAttackSpeed(float speed)
    {
        attackSpeed = speed;
        attackPause = new(attackSpeed);
	}

	public void AggroCheck()
	{
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
		List<Vector3Int> attackingZones = new();

		Vector3Int forward = unit.enemyCamp.forward;
		attackingZones.Add(forward + unit.CurrentLocation);
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
					unit.attacking = true;
					if (!unit.enemyCamp.attackingArmy.attackingSpots.Contains(unit.CurrentLocation))
						unit.enemyCamp.attackingArmy.attackingSpots.Add(unit.CurrentLocation);
					StartAttack(enemy);
					return;
				}
			}
		}

		unit.enemyCamp.attackingArmy.attackingSpots.Remove(unit.CurrentLocation);

		Unit newEnemy = unit.enemyCamp.FindClosestTarget(unit);
		List<Vector3Int> path = unit.enemyCamp.PathToEnemy(unit.CurrentLocation, unit.world.RoundToInt(newEnemy.transform.position));

		if (path.Count > 0)
		{
			//moving unit behind if stuck
			Vector3Int positionBehind = unit.enemyCamp.forward * -1 + unit.CurrentLocation;

			if (unit.world.IsUnitLocationTaken(positionBehind))
			{
				Unit unitBehind = unit.world.GetUnit(positionBehind);
				if (unitBehind.enemyAI && unitBehind.targetSearching)
					unitBehind.AggroCheck();
			}

			unit.attacking = true;

			if (path.Count >= 2)
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
			unit.attacking = false;
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
		//needsDestination = false;
		unit.targetSearching = false;
		unit.Rotate(target.transform.position);

		if (unit.attackCo == null)
			unit.attackCo = StartCoroutine(Attack(target));
    }

    public IEnumerator Attack(Unit target)
    {
		float dist = 0;
		if (target.targetSearching)
			target.StartAttack(unit);
		
		while (!unit.isDead && target.currentHealth > 0 && dist < 2.1f)
        {
    		unit.StartAttackingAnimation();
            //yield return new WaitForSeconds(.1f);
			yield return attackPause;
	        target.ReduceHealth(unit.attackStrength, unit.transform.eulerAngles);
			dist = (target.transform.position - unit.transform.position).sqrMagnitude;
		}

		if (dist >= 2.1f)
		{
			unit.enemyCamp.ResetStatus();
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
		float dist = 0;
		unit.Rotate(target.transform.position);

		while (!unit.isDead && target.currentHealth > 0 && dist < 30)
		{
			//StartAttackingAnimation();
			unit.projectile.SetPoints(transform.position, target.transform.position);
			StartCoroutine(unit.projectile.Shoot(unit, target));
			dist = (target.transform.position - unit.transform.position).sqrMagnitude;
			yield return attackPause;
		}

		if (dist >= 30)
		{
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
		if (unit.attackCo != null)
			StopCoroutine(unit.attackCo);
		//targetUnit = null;
		//needsDestination = false;
		//EnemyMove(campSpot, false);
		unit.attacking = false;
		unit.targetSearching = false;
		unit.finalDestinationLoc = campSpot;
		List<Vector3Int> path = GridSearch.AStarSearch(unit.world, unit.world.RoundToInt(unit.transform.position), campSpot, unit.isTrader, unit.bySea, true);

		if (path.Count > 0)
		{
			unit.MoveThroughPath(path);
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
