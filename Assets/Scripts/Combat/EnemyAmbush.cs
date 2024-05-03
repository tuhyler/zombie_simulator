using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class EnemyAmbush
{
    public Vector3Int loc;
    public string attackedTrader;
    public List<Military> attackingUnits = new();
    public List<Unit> attackedUnits = new();
    public bool targetTrader;

    public EnemyAmbushData GetAmbushData(MapWorld world)
    {
        EnemyAmbushData data = new();
		List<UnitData> attackingList = new();

		for (int i = 0; i < attackingUnits.Count; i++)
        {
            world.enemyCount++;
            attackingUnits[i].id = -world.enemyCount;
			attackingList.Add(attackingUnits[i].SaveMilitaryUnitData());
        }

		data.loc = loc;
		data.attackingUnits = attackingList;
        data.attackedTrader = attackedTrader;
        data.targetTrader = targetTrader;

        return data;
    }

    public void ContinueTradeRoute()
    {
        for (int i = 0; i < attackedUnits.Count; i++)
        {
            if (attackedUnits[i].trader)
            {
                attackedUnits[i].trader.ContinueTradeRoute();
                break;
            }
        }
    }
}
