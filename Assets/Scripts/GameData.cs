using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    public Vector3 camPosition;
    public Quaternion camRotation;
    public float timeODay;
    
    public UnitData playerUnit;
    public Dictionary<Vector3Int, TerrainSaveData> allTerrain = new();
    public List<TradeCenterData> allTradeCenters = new();
    public List<CityData> allCities = new();
    public List<RoadData> allRoads = new();
    public List<UnitData> allUnits;

}
