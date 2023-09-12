using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
//using static UnityEngine.RuleTile.TilingRuleOutput;

public class EnemyCamp 
{
	public MapWorld world;
	
	public Vector3Int loc, forward;
	private Vector3Int armyDiff;

	private int enemyReady;
	public int campCount, deathCount;
	public bool prepping, attacked, attackReady = false, armyReady, inBattle, returning;
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
				if (ranged < 3)
				{
                    if (infantry+cavalry > 6)
						unit.barracksBunk = loc + backLines[2 - ranged];
					else
						unit.barracksBunk = loc + backLines[ranged];
				}
                else if (ranged < 6)
				{
					if (infantry+cavalry > 3)
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

    public void BattleStations()
    {
		if (prepping)
			return;

		returning = false;
		prepping = true;
		
		Vector3Int diff = (threatLoc - loc) / 3;

		armyDiff = diff;
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
		unit.Rotate(unit.CurrentLocation + armyDiff);
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
				dist = (closestEnemy.transform.position - unit.transform.position).sqrMagnitude;
				continue;
			}

			float nextDist = (enemy.transform.position - unit.transform.position).sqrMagnitude;

			if (nextDist < dist)
			{
				closestEnemy = enemy;
				dist = nextDist;
			}
		}

		return closestEnemy;
	}

	public List<Vector3Int> PathToEnemy(Vector3Int pos, Vector3Int target)
	{
		return GridSearch.BattleMove(world, pos, target, attackingArmy.movementRange, attackingArmy.attackingSpots);
	}

	public bool FinishAttack()
	{
		if (returning)
			return true;
		
		if (attackingArmy != null && attackingArmy.armyCount == 0)
		{
			returning = true;
			
			foreach (Unit unit in unitsInCamp)
				unit.StopAttacking();

			//ResetStatus();
			ReturnToCamp();
			//attackingArmy.deathCount = 0;
			//attackingArmy.armyCount = 0;

			if (attackingArmy != null)
			{
				attackingArmy.inBattle = false;
				attackingArmy.atHome = true;
				attackingArmy = null;
			}

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

	private void ResurrectCamp()
	{
		foreach (Unit unit in deadList)
		{
			unit.transform.position = unit.enemyAI.CampSpot;
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
			unit.gameObject.SetActive(true);
			unit.lightBeam.Play();
			//unit.healthbar.gameObject.SetActive(true);
			LeanTween.scale(unit.gameObject, goScale, 0.5f).setEase(LeanTweenType.easeOutBack);
		}

		deathCount = 0;
		deadList.Clear();
	}
}
