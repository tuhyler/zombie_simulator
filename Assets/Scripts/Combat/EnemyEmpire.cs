using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyEmpire
{
    public MilitaryLeader enemyLeader;
    public Era enemyEra;
    public Region enemyRegion;
    public List<Vector3Int> empireCities = new();
    public Vector3Int attackingCity = new Vector3Int(0, -10, 0), capitalCity;

    public void SetNextAttackingCity(MapWorld world, Vector3Int lastOne)
    {
        if (empireCities.Count <= 1)
            return;
        
        bool firstOne = true;
        int dist = 0;
        Vector3Int chosenCity = empireCities[0];
        for (int i = 0; i < empireCities.Count; i++)
        {
            if (empireCities[i] == lastOne)
                continue;

            if (firstOne)
            {
                chosenCity = empireCities[i];
                dist = Mathf.Abs(empireCities[i].x - lastOne.x) + Mathf.Abs(empireCities[i].z - lastOne.z);
                continue;
            }

            int newDist = Mathf.Abs(empireCities[i].x - lastOne.x) + Mathf.Abs(empireCities[i].z - lastOne.z);
            if (newDist < dist)
            {
                dist = newDist;
                chosenCity = empireCities[i];
            }
        }

        empireCities.Remove(lastOne);
        world.GetEnemyCity(chosenCity).StartSendAttackWait();
    }

    public bool CanAttackCheck(Vector3Int loc)
    {
        if (!empireCities.Contains(attackingCity))
        {
            attackingCity = loc;
            return true;
        }
        else
        {
            return false;
        }
    }

    public void LoadData(MilitaryLeaderData data)
    {
		attackingCity = data.attackingCity;
		capitalCity = data.capitalCity;
		empireCities = new();

		for (int i = 0; i < data.empireCities.Count; i++)
			empireCities.Add(data.empireCities[i]);
	}
}
