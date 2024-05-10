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
                if (attackedUnits[i].trader.guarded)
                {
					attackedUnits[i].trader.guardUnit.StopAnimation();
					List<Vector3Int> path = new();
                    if (attackedUnits[i].bySea)
                    {
						path.Add(attackedUnits[i].currentLocation + Vector3Int.left);
					}
                    else if (attackedUnits[i].byAir)
                    {
                        Vector3Int tempSpot = attackedUnits[i].currentLocation;
                        tempSpot.y += 1;
                        path.Add(tempSpot);
					}
                    else
                    {
                        path.Add(attackedUnits[i].currentLocation);
                    }

                    attackedUnits[i].trader.guardUnit.finalDestinationLoc = path[path.Count - 1];
					attackedUnits[i].trader.guardUnit.MoveThroughPath(path);
                }
                else
                {
                    attackedUnits[i].trader.ContinueTradeRoute();
                }

                break;
            }
        }
    }
}
