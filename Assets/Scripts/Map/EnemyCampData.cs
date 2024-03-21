using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyCampData
{
	public int enemyReady, campCount, infantryCount, rangedCount, cavalryCount, seigeCount, health, strength, pillageTime, countDownTimer;
	public bool revealed, prepping, attacked, attackReady, armyReady, inBattle, returning, movingOut, chasing, pillage, growing, removingOut, atSea, battleAtSea, seaTravel;
	public Vector3Int threatLoc, forward, chaseLoc, fieldBattleLoc, lastSpot, actualAttackLoc;
	public List<Vector3Int> pathToTarget;

	public List<UnitData> allUnits = new();
}
