using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyCityData
{
	public Vector3Int loc, barracksLoc, harborLoc;
	public int popSize;
	public bool hasWater;
	public string cityName;
	public Dictionary<Vector3Int, string> enemyUnitData;
	public Era era;
}
