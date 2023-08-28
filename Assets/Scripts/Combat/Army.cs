using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Army : MonoBehaviour
{
    private Vector3Int loc;
    private List<Vector3Int> totalSpots = new(), openSpots = new(), pathToTarget = new();
    
    private List<Unit> unitsInArmy = new();
    private int unitsReady;

    [HideInInspector]
    public bool isFull, traveling, inBattle, atHome;

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
        return openSpot;
    }

    public void UpdateLocation(Vector3Int oldLoc, Vector3Int newLoc)
    {
		int index = totalSpots.IndexOf(oldLoc);
		openSpots.Insert(index, oldLoc);
		openSpots.Remove(newLoc);
    }

    public void AddToArmy(Unit unit)
    {
        unit.atHome = true;
        unitsInArmy.Add(unit);
    }

    public void RemoveFromArmy(Unit unit, Vector3Int loc)
    {
        unitsInArmy.Remove(unit);

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

        if (diff.x == 1)
            rotation = 1;
        else if (diff.z == 1)
            rotation = 2;
        else if (diff.x == -1)
            rotation = 3;
        else
            rotation = 0;

        foreach (Unit unit in unitsInArmy)
        {
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
                unit.repositioning = true;
                unit.finalDestinationLoc = loc + unitDiff;
    			unit.MoveThroughPath(path);
            }
        }
    }

    public void MoveArmy(MapWorld world, Vector3Int target, bool deploying)
    {
        atHome = false;
        traveling = true;

        Vector3Int destination = deploying ? target : loc;
        Vector3Int current = deploying ? loc : target;

        pathToTarget = GridSearch.TerrainSearch(world, current, destination);

        if (pathToTarget.Count == 0)
            return;

        Vector3Int penultimate = pathToTarget[pathToTarget.Count-2];

        RealignUnits(world, destination, penultimate);
    }

    public void UnitReady()
    {
        unitsReady++;

        if (unitsReady == unitsInArmy.Count)
            DeployArmy();
    }

    private void DeployArmy()
    {
		foreach (Unit unit in unitsInArmy)
		{
			Vector3Int diff = unit.CurrentLocation - loc;
			List<Vector3Int> path = new();

			foreach (Vector3Int tile in pathToTarget)
				path.Add(tile + diff);

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

    public void SelectArmy(Unit selectedUnit)
    {
        foreach (Unit unit in unitsInArmy)
        {
            Color color = unit == selectedUnit ? Color.green : Color.white;
            unit.Select(color);
        }
    }

    public void UnselectArmy(Unit selectedUnit)
    {
		foreach (Unit unit in unitsInArmy)
        {
            if (unit == selectedUnit)
                continue;

            unit.Deselect();
        }
	}
}
