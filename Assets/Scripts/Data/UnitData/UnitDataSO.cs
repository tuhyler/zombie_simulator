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
    public float movementSpeed = 1f;
}
