using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Map/TerrainData")]
public class TerrainDataSO : ScriptableObject
{
    public string title;
    public GameObject roadPrefab; //only for changing forest terrain when a road is built
    public TerrainDataSO clearedForestData; //for clearing a forest
    public bool grassland = false;
    public bool desert = false;
    public bool walkable = false;
    public bool sailable = false;
    public bool isLand = false;
    public bool isSeaCorner = false;
    public int movementCost = 10;
    public TerrainType type = TerrainType.Flatland;
    public SpecificTerrain specificTerrain;
    public TerrainDesc terrainDesc;
    public RawResourceType rawResourceType;
    public ResourceType resourceType;
    public bool keepProp = false;
    public bool hasRocks = false;

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

public enum SpecificTerrain
{
    None,
    FloodPlain,
}

//for making map panel
public enum TerrainDesc { Grassland, Desert, GrasslandHill, DesertHill, GrasslandFloodPlain, DesertFloodPlain, Forest, ForestHill, Jungle, JungleHill, Swamp, Mountain, Sea, River, City, Wonder, TradeCenter };

