using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class EnemyAmbush
{
    public Vector3Int loc;
    public string attackedTrader;
    public List<Unit> attackingUnits = new();
    public List<Unit> attackedUnits = new();

    public EnemyAmbushData GetAmbushData()
    {
        EnemyAmbushData data = new();
		List<UnitData> attackingList = new();

		for (int i = 0; i < attackingUnits.Count; i++)
		{
			attackingList.Add(attackingUnits[i].SaveMilitaryUnitData());
		}

		data.loc = loc;
		data.attackingUnits = attackingList;
        data.attackedTrader = attackedTrader;

        return data;
    }

    public void ContinueTradeRoute()
    {
        for (int i = 0; i < attackedUnits.Count; i++)
        {
            if (attackedUnits[i].isTrader)
            {
                attackedUnits[i].ContinueTradeRoute();
                break;
            }
        }
    }
}
