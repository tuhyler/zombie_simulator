using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCamp : MonoBehaviour
{
    private List<Unit> unitsInCamp = new();
    public List<Unit> UnitsInCamp { get { return unitsInCamp; } set { unitsInCamp = value; } }
    Vector3Int[] frontLines = { new Vector3Int(0, 0, -1), new Vector3Int(-1, 0, -1), new Vector3Int(1, 0, -1) };
	Vector3Int[] midLines = { new Vector3Int(0, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(1, 0, 0) };
	Vector3Int[] backLines = { new Vector3Int(0, 0, 1), new Vector3Int(-1, 0, 1), new Vector3Int(1, 0, 1) };


	public void FormBattlePositions(Vector3Int loc)
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
}
