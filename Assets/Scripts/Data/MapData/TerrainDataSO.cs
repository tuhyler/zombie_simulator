using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/TerrainData")]
public class TerrainDataSO : ScriptableObject
{
    public GameObject roadPrefab; //only for changing forest terrain when a road is built
    public TerrainDataSO clearedForestData; //for clearing a forest
    public bool isFloodPlain = false;
    public bool walkable = false;
    public bool sailable = false;
    public bool isHill = false;
    public bool isLand = false;
    public bool isSeaCorner = false;
    public int movementCost = 10;
    public TerrainType type = TerrainType.Flatland;
    public ResourceType resourceType;

    public void MovementCostCheck()
    {
        if (movementCost <= 0)
        {
            throw new Exception("MovementCost cannot be zero or less");
        }
    }
}

//always add new ones at the bottom
public enum TerrainType
{
    Obstacle,
    Coast,
    Sea,
    SeaIntersection,
    River,
    Flatland,
    Hill,
    Forest,
    ForestHill

}
