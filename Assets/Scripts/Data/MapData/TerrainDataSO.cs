using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/TerrainData")]
public class TerrainDataSO : ScriptableObject
{
    public GameObject prefab;
    public GameObject roadPrefab; //only for changing forest terrain when a road is built
    public GameObject clearedForestPrefab; //for clearing a forest
    public bool walkable = false;
    public bool sailable = false;
    public int movementCost = 10;
    public TerrainType type;
    public ResourceType resourceType;

    public void MovementCostCheck()
    {
        if (movementCost <= 0)
        {
            throw new Exception("MovementCost cannot be zero or less");
        }
    }
}

public enum TerrainType
{
    Difficult,
    Moveable,
    Obstacle,
    Sea,
    River,
    Forest
}
