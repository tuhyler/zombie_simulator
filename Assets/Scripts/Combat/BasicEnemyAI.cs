using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BasicEnemyAI : MonoBehaviour
{
    private Vector3Int campLoc;
    private Vector3Int startingPosition;
    private Unit unit;
    private List<Vector3Int> moveRange = new();
    private State state;
    [HideInInspector]
    public bool needsDestination = true, isLeader;
    private Vector3Int currentDestination;
    private Coroutine roamCo, attackCo;
    private float attackSpeed;

    private Unit targetUnit;

    private WaitForSeconds roamingPause = new(1), attackPause;


	private void Awake()
	{
        unit = GetComponent<Unit>();
    }

	private void Update()
	{
		switch (state)
		{
            default:
            case State.Roaming:
				if (needsDestination)
                {
                    needsDestination = false;
                    //AggroCheck();
					//StartRoaming();
                }
				break;
			case State.Chasing:
                //ResetTarget();
                if (needsDestination)
                    StartChase();

				if (Mathf.Abs(transform.position.x - targetUnit.transform.position.x) + Mathf.Abs(transform.position.z - targetUnit.transform.position.z) < 1)
                {
                    StopMovement();
                    state = State.Attacking;
					unit.world.AddUnitPosition(transform.position, unit);
					if (isLeader)
                        unit.world.ConvergeCamp(unit, campLoc);
                }
                
                if (Mathf.Abs(transform.position.x - campLoc.x) + Mathf.Abs(transform.position.z - campLoc.z) > 10)
                {
                    StopMovement();
                    state = State.Returning;
                }
				break;
			case State.Attacking:
                if (needsDestination)
                {
                    unit.transform.LookAt(targetUnit.transform);
                    StartAttack();
                }

				if (Mathf.Abs(transform.position.x - targetUnit.transform.position.x) + Mathf.Abs(transform.position.z - targetUnit.transform.position.z) > 1.5f)
                {
                    if (attackCo != null)
                    {
                        StopCoroutine(attackCo);
                    }
                    StopMovement();
                    state = State.Returning;
                }

                if (targetUnit.isDead)
                {
					StopMovement();
					if (attackCo != null)
						StopCoroutine(attackCo);

					state = State.Returning;
					targetUnit = null;
					AggroCheck();
				}
				break;
			case State.Hunting:
				break;
			case State.Returning:
                if (needsDestination)
                {
                    StopMovement();
                    if (attackCo != null)
						StopCoroutine(attackCo);
					StartReturn();
                    state = State.Roaming;
                }
				break;
		}
	}


	public void SetStartingPosition()
    {
		startingPosition = unit.world.RoundToInt(transform.position);
		moveRange.Add(startingPosition);
	}

    public void SetCampLoc(Vector3Int campLoc)
    {
        this.campLoc = campLoc;
    }

    //public void SetPosition()
    //{
    //    unit.EnemyAttackSetup();
    //}

	//public void AddToRoamRange(Vector3Int tile)
 //   {
 //       moveRange.Add(tile);
 //   }

    public void SetAttackSpeed(float speed)
    {
        attackSpeed = speed;
        attackPause = new(attackSpeed);
	}

    //public void StartRoaming()
    //{
    //    needsDestination = false;
    //    roamCo = StartCoroutine(RoamAbout());
    //} 

    //private IEnumerator RoamAbout()
    //{
    //    int roamWait = Random.Range(4, 9);
    //    int timeWaited = 0;

    //    while (timeWaited < roamWait)
    //    {
    //        yield return roamingPause;
    //        timeWaited++;
    //    }

    //    roamCo = null;
    //    EnemyMove(NewRoamingPosition());
    //}

    //public Vector3Int NewRoamingPosition()
    //{
    //    Vector3Int newLoc = unit.CurrentLocation;
        
    //    while (newLoc == unit.CurrentLocation)
    //        newLoc = moveRange[Random.Range(0, moveRange.Count)];

    //    return newLoc;
    //}

    private void AggroCheck()
    {
        if (unit.world.GetTerrainDataAt(unit.CurrentLocation).isDiscovered)
        {
            foreach (Vector3Int tile in unit.world.GetNeighborsFor(unit.CurrentLocation, MapWorld.State.EIGHTWAYTWODEEP))
            {
                if (unit.world.IsUnitLocationTaken(tile))
                {
                    Unit unitInQuestion = unit.world.GetUnit(tile);
                    
                    if (unitInQuestion.CompareTag("Player"))
                    {
                        targetUnit = unitInQuestion;
                        StopMovement();
                        state = State.Chasing;
                        return;
                    }
                }
            }
        }
    }

    public void WakeUp(Unit targetUnit, bool isLeader)
    {
        if (state == State.Roaming)
        {
            //Debug.Log("wake up");
            this.isLeader = isLeader;
            if (roamCo != null)
                StopCoroutine(roamCo);
            this.targetUnit = targetUnit;
            StopMovement();
            state = State.Chasing;
        }
    }

    public void StopMovement()
    {
		if (unit.isMoving)
		{
			unit.StopAnimation();
			unit.ShiftMovement();
		}
        
        needsDestination = true;
	}

    public bool ResetTarget()
    {
        if (isLeader && state == State.Chasing)
        {
            if (unit.world.RoundToInt(targetUnit.transform.position) != currentDestination)
            {
                StopMovement();
                StartChase();
                return true;
            }
        }

        return false;
    }

    public void StartChase()
    {
        //Debug.Log("chasing");
        if (isLeader)
        {
    		needsDestination = false;
            List<Vector3Int> path = GetPath(targetUnit.transform.position);

            if (path.Count == 0)
            {
                needsDestination = true;
				return;
            }
            
            unit.world.MoveCamp(path, campLoc, unit, targetUnit);
			unit.MoveThroughPath(path);
		}


        //EnemyMove(targetUnit.transform.position);
	}

    public void FollowLeader(List<Vector3Int> path, Vector3Int leaderDiff, Unit targetUnit)
    {
        StopMovement();
        if (path.Count == 0)
        {
            needsDestination = true;
            return;
        }
        
        List<Vector3Int> newPath = new();
        
        foreach (Vector3Int tile in path)
        {
            newPath.Add(tile + leaderDiff);
        }

        needsDestination = false;
        this.targetUnit = targetUnit;
        state = State.Chasing;

		unit.finalDestinationLoc = newPath[newPath.Count - 1];
		currentDestination = newPath[newPath.Count - 1];

		unit.MoveThroughPath(newPath);
	}

    public void StartAttack()
    {
        needsDestination = false;
        attackCo = StartCoroutine(Attack());
    }

    public IEnumerator Attack()
    {
        if (targetUnit == null)
            yield break;
        
        while (targetUnit.currentHealth > 0)
        {
    		unit.StartAttackingAnimation();
            yield return new WaitForSeconds(.1f);
            targetUnit.ReduceHealth(unit.attackStrength, unit.transform.eulerAngles);
			yield return attackPause;
		}

        state = State.Returning;
        StopMovement();
        unit.StopAnimation();

        targetUnit = null;
        AggroCheck();
    }

    public void StartReturn()
    {
        targetUnit = null;
        needsDestination = false;
        EnemyMove(startingPosition, false);
    }

    public void Converge()
    {
		unit.StopAnimation();
		unit.ShiftMovement();
		EnemyMove(targetUnit.transform.position, true);
    }

    //public void ReadjustAttack()
    //{
    //    EnemyMove(targetUnit.transform.position, true);
    //}

	private void EnemyMove(Vector3 destination, bool attack)
    {
        unit.finalDestinationLoc = destination;
        currentDestination = unit.world.RoundToInt(destination);
        //Debug.Log(currentDestination);
        List<Vector3Int> path = GridSearch.AStarSearch(unit.world, unit.world.RoundToInt(unit.transform.position), currentDestination, unit.isTrader, unit.bySea, attack);
		
        if (path.Count == 0)
        {
            needsDestination = true;
            return;
        }

        unit.MoveThroughPath(path);
	}

    private List<Vector3Int> GetPath(Vector3 destination)
    {
		unit.finalDestinationLoc = destination;
		currentDestination = unit.world.RoundToInt(destination);

		return GridSearch.AStarSearch(unit.world, unit.world.RoundToInt(unit.transform.position), currentDestination, unit.isTrader, unit.bySea);
	}



    private enum State
    {
        Roaming,
        Chasing,
        Attacking,
        Returning,
        Hunting
    }
}
