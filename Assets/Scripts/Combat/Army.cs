using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Army : MonoBehaviour
{
    private Vector3Int loc;
    private List<Vector3Int> totalSpots = new(), openSpots = new(), pathToTarget = new();
    
    private List<Unit> unitsInArmy = new();
    public List<Unit> UnitsInArmy { get { return unitsInArmy; } }
    private int unitsReady;

    [HideInInspector]
    public bool isEmpty = true, isFull, isTraining, traveling, inBattle, returning, atHome, selected;

	private void Awake()
	{
        atHome = true;
	}

    public void SetLoc(Vector3Int loc)
    {
        this.loc = loc;
    }

    public bool CheckIfInBase(Vector3Int loc)
    {
        return totalSpots.Contains(loc);
    }

	public Vector3Int GetAvailablePosition()
    {
        Vector3Int openSpot = openSpots[0];
        openSpots.Remove(openSpot);

        if (openSpots.Count == 0)
            isFull = true;
        if (isEmpty)
            isEmpty = false;

        return openSpot;
    }

    public void UpdateLocation(Vector3Int oldLoc, Vector3Int newLoc)
    {
		int index = totalSpots.IndexOf(oldLoc);

        if (index > totalSpots.Count - openSpots.Count) 
            openSpots.Add(oldLoc);
        else
		    openSpots.Insert(index, oldLoc);
		openSpots.Remove(newLoc);
    }

    public void AddToArmy(Unit unit)
    {
        unitsInArmy.Add(unit);
    }

    public void RemoveFromArmy(Unit unit, Vector3Int loc)
    {
        unitsInArmy.Remove(unit);

        if (unitsInArmy.Count == 0)
            isEmpty = true;
        if (isFull)
            isFull = false;

        int index = totalSpots.IndexOf(loc);
        openSpots.Insert(index,loc);
    }

    //preparing positions lists
    public void SetArmySpots(Vector3Int tile)
    {
        totalSpots.Add(tile);
        openSpots.Add(tile);
    }

    //realigning units to battle positions before moving out
    public void RealignUnits(MapWorld world, Vector3Int targetZone, Vector3Int attackZone)
    {
        Vector3Int diff = (targetZone - attackZone) / 3;
        int rotation;

        if (diff.x == -1)
            rotation = 1;
        else if (diff.z == 1)
            rotation = 2;
        else if (diff.x == 1)
            rotation = 3;
        else
            rotation = 0;

        foreach (Unit unit in unitsInArmy)
        {
            unit.atHome = false;
            Vector3Int unitDiff = unit.CurrentLocation - loc;

            if (rotation == 0)
            {
                UnitReady();
                continue;
            }
            else if (rotation == 1)
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
                unit.preparingToMoveOut = true;
                unit.finalDestinationLoc = loc + unitDiff;
    			unit.MoveThroughPath(path);
            }
            else
            {
                UnitReady();
            }
        }
    }

    public bool MoveArmy(MapWorld world, Vector3Int current, Vector3Int target, bool deploying)
    {
        Vector3Int destination = deploying ? target : loc;

        List<Vector3Int> exemptList = new();
        if (deploying)
        {
            exemptList.Add(target);
            
            foreach (Vector3Int tile in world.GetNeighborsFor(target, MapWorld.State.EIGHTWAYINCREMENT))
                exemptList.Add(tile);
        }

        pathToTarget = GridSearch.TerrainSearch(world, current, destination, exemptList);

        if (pathToTarget.Count == 0)
        {
            if (deploying)
                return false;
            else
                pathToTarget.Add(target);
        }

        unitsReady = 0;

        if (deploying)
        {
            pathToTarget.Remove(pathToTarget[pathToTarget.Count - 1]);
            atHome = false;
            traveling = true;
            Vector3Int penultimate = pathToTarget[pathToTarget.Count - 1];

            if (returning)
                DeployArmy(true);
            else
                RealignUnits(world, destination, penultimate);
        }
        else
        {
            traveling = false;
            returning = true;
            DeployArmy(false);
        }

        return true;
    }

    public void UnitReady()
    {
        unitsReady++;

        if (unitsReady == unitsInArmy.Count)
        {
            unitsReady = 0;
            DeployArmy(true);
        }
    }

    public void UnitArrived(Vector3Int loc)
    {
        unitsReady++;

        if (unitsReady == unitsInArmy.Count)
        {
            unitsReady = 0;
            
            if (this.loc == loc)
            {
                if (openSpots.Count == 0)
                    isFull = true;

                atHome = true;
                returning = false;
            }
            else
                Charge();
        }
    }

    public void UnitNextStep()
    {
        unitsReady++;

        if (unitsReady == unitsInArmy.Count)
        {
            unitsReady = 0;
            BeginNextStep();
        }
    }

    public void BeginNextStep()
    {
        foreach (Unit unit in unitsInArmy)
            unit.readyToMarch = true;
    }

    private void DeployArmy(bool deploying)
    {
		foreach (Unit unit in unitsInArmy)
		{
            unit.isMarching = true;
            Vector3Int diff = unit.CurrentLocation - loc;
			List<Vector3Int> path = new();

			foreach (Vector3Int tile in pathToTarget)
				path.Add(tile + diff);

            if (!deploying)
                path[path.Count - 1] = unit.barracksBunk;

			if (unit.isMoving)
			{
				unit.StopAnimation();
				unit.ShiftMovement();
			}

			unit.finalDestinationLoc = path[path.Count - 1];
			unit.MoveThroughPath(path);
		}
	}

    public void Charge()
    {
        foreach (Unit unit in unitsInArmy)
        {

        }
    }

    public bool IsGone()
    {
        if (traveling || returning)
            return true;

        return false;
    }

    public void SelectArmy(Unit selectedUnit)
    {
        selected = true;
        
        foreach (Unit unit in unitsInArmy)
        {
            Color color = unit == selectedUnit ? Color.green : Color.white;
            unit.Select(color);
        }
    }

    public bool AllAreHomeCheck()
    {
        foreach (Unit unit in unitsInArmy)
        {
            if (!unit.atHome)
                return false;
        }

        return true;
    }

    public void UnselectArmy(Unit selectedUnit)
    {
        selected = false;
        
        foreach (Unit unit in unitsInArmy)
        {
            if (unit == selectedUnit)
                continue;

            unit.Deselect();
        }
	}

    public Vector3 GetRandomSpot(Vector3Int current)
    {
        int random = Random.Range(0, totalSpots.Count);
        Vector3Int spot = totalSpots[random];

        if (spot == current)
            spot = totalSpots[3];

        return spot;
    }
}
