using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyCampData
{
	public int enemyReady, campCount, infantryCount, rangedCount, cavalryCount, seigeCount, health, strength;
	public bool revealed, prepping, attacked, attackReady, armyReady, inBattle, returning;
	public Vector3Int threatLoc, forward;

	public List<UnitData> allUnits = new();
}
