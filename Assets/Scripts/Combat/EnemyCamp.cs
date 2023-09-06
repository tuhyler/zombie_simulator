using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class EnemyCamp 
{
	public MapWorld world;
	
	public Vector3Int loc;
	private Vector3Int armyDiff;

	private int enemyReady;
	public bool attacked, attackReady, armyReady;
	public Army attackingArmy;

	public Queue<Vector3Int> threatQueue = new();
	public Vector3Int threatLoc;

	private List<Unit> unitsInCamp = new();
    public List<Unit> UnitsInCamp { get { return unitsInCamp; } set { unitsInCamp = value; } }
    Vector3Int[] frontLines = { new Vector3Int(0, 0, -1), new Vector3Int(-1, 0, -1), new Vector3Int(1, 0, -1) };
	Vector3Int[] midLines = { new Vector3Int(0, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(1, 0, 0) };
	Vector3Int[] backLines = { new Vector3Int(0, 0, 1), new Vector3Int(-1, 0, 1), new Vector3Int(1, 0, 1) };

	private SpriteRenderer minimapIcon;

	private Coroutine co;


	public void FormBattlePositions()
    {
        int infantry = 0;
        int cavalry;
        int ranged;
        
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

        cavalry = infantry;

        if (cavalry == 0)
            return;

        foreach (Unit unit in unitsInCamp)
        {
            
            if (unit.buildDataSO.unitType == UnitType.Cavalry)
            {
				if (cavalry < 3)
					unit.barracksBunk = loc + frontLines[cavalry];
				else if (cavalry < 6)
					unit.barracksBunk = loc + midLines[cavalry - 3];
				else
					unit.barracksBunk = loc + backLines[cavalry - 6];

				cavalry++;
            }
        }

        ranged = 9 - (cavalry + infantry);

        if (ranged == 0)
            return;

		foreach (Unit unit in unitsInCamp)
        {
			if (unit.buildDataSO.unitType == UnitType.Ranged)
			{
				if (ranged < 3)
                    unit.barracksBunk = loc + backLines[3 - ranged];
                if (ranged < 6)
                    unit.barracksBunk = loc + backLines[ranged];
	
                ranged--;
			}
        }
    }

    public void BattleStations()
    {
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

			List<Vector3Int> path = GridSearch.AStarSearch(world, unit.CurrentLocation, loc + unitDiff, false, false);

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

	public void EnemyReady(Unit unit)
	{
		unit.Rotate(unit.CurrentLocation + armyDiff);
		enemyReady++;

		if (enemyReady == unitsInCamp.Count)
		{
			attackReady = true;

			if (armyReady)
				attackingArmy.Charge();
		}
	}

	public void Charge()
	{
		
	}

	public void ResetStatus()
	{
		attacked = false;
		attackReady = false;
		armyReady = false;
		attackingArmy = null;
	}

	public void ReturnToCamp()
	{
		foreach (Unit unit in unitsInCamp)
		{
			unit.enemyAI.StartReturn();
		}
	}

    public void SetMinimapIcon(Transform parent)
    {
        //minimapIcon.sprite = buildDataSO.mapIcon;
        ConstraintSource constraintSource = new();
        constraintSource.sourceTransform = parent;
        constraintSource.weight = 1;
        RotationConstraint rotation = minimapIcon.GetComponent<RotationConstraint>();
		rotation.rotationAtRest = new Vector3(90, 0, 0);
		rotation.rotationOffset = new Vector3(90, 0, 0);
		rotation.AddSource(constraintSource);
    }
}
