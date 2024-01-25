using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyCampData
{
	public int enemyReady, campCount, infantryCount, rangedCount, cavalryCount, seigeCount, health, strength, pillageTime;
	public bool revealed, prepping, attacked, attackReady, armyReady, inBattle, returning, movingOut, chasing, pillage;
	public Vector3Int threatLoc, forward, chaseLoc;
	public List<Vector3Int> pathToTarget;

	public List<UnitData> allUnits = new();
}
