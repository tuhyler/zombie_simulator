using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyCampData
{
	public int enemyReady, campCount, infantryCount, rangedCount, cavalryCount, seigeCount, health, strength, pillageTime, countDownTimer, timeTilReturn;
	public bool revealed, prepping, attacked, attackReady, armyReady, inBattle, returning, movingOut, pillage, growing, removingOut, atSea, battleAtSea, seaTravel, retreat;
	public Vector3Int threatLoc, forward, moveToLoc, fieldBattleLoc, lastSpot, actualAttackLoc;
	public List<Vector3Int> pathToTarget;

	public List<UnitData> allUnits = new();
}
