using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyAmbushData
{
	public Vector3Int loc;
	public List<UnitData> attackingUnits = new();
	public string attackedTrader;
}
