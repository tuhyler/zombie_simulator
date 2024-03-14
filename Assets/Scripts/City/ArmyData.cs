using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ArmyData
{
    public Vector3Int forward, attackZone, enemyTarget, enemyCityLoc;
    public int cyclesGone, unitsReady, stepCount, noMoneyCycles;
    public List<Vector3Int> pathToTarget, pathTraveled, attackingSpots, movementRange, cavalryRange;
    public bool isTransferring, isRepositioning, traveling, inBattle, returning, atHome, enemyReady, issueRefund, defending, atSea, seaTravel;
}
