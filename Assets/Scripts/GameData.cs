using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    public string saveName;
    public float savePlayTime = 0;
    public string saveVersion;
    public string saveScreenshot;
    
    public Vector3 camPosition;
    public Quaternion camRotation;
    public float timeODay;
    public List<float> camLimits = new();

    public Dictionary<Vector3Int,Dictionary<Vector3Int,string>> enemyCampLocs = new();
    public List<Vector3Int> discoveredEnemyCampLocs = new();

    public Dictionary<Vector3Int, EnemyCampData> attackedEnemyBases = new();
    public Dictionary<Vector3Int, List<UnitData>> militaryUnits = new();

    public List<TraderData> allTraders = new();
    public List<LaborerData> allLaborers = new();

    public UnitData playerUnit;
    public Dictionary<Vector3Int, TerrainSaveData> allTerrain = new();
    public List<TradeCenterData> allTradeCenters = new();
    public List<WonderData> allWonders = new();
    public List<CityData> allCities = new();
    public Dictionary<Vector3Int, ArmyData> allArmies = new();
    public List<RoadData> allRoads = new();
    public List<CityImprovementData> allCityImprovements = new();
    public List<UnitData> allUnits = new();

}