using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Units/UnitData")]
public class UnitDataSO : ScriptableObject
{
    public int movementPoints = 10;
    public int health = 10;
    public int attackStrength = 10;
    //private Dictionary<Unit, List<TerrainData>> previousMovementOrdersDict = new Dictionary<Unit, List<TerrainData>>();

    public void MovementPointsCheck()
    {
        if (movementPoints <= 0)
        {
            throw new Exception("Movement Points cannot be zero or less");
        }
    }
}
