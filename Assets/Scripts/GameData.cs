using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    public string saveDate;
    public string saveName;
    public float savePlayTime = 0;
    public string saveVersion;
    public string saveScreenshot;

    //world misc lists
    public List<string> completedResearch, currentResearch;
    public int goldAmount, researchAmount, cityCount, infantryCount, rangedCount, cavalryCount, traderCount, boatTraderCount, laborerCount, food, lumber;
    public List<ResourceType> resourceDiscoveredList = new();
    public Dictionary<Vector3Int, int> currentWorkedTileDict;
    public Dictionary<Vector3Int, Vector3Int?> cityWorkedTileDict;
    public Dictionary<Vector3Int, Vector3Int> cityImprovementQueueList;
    public List<Vector3Int> unclaimedSingleBuildList, researchWaitList = new(), goldCityWaitList = new(), goldCityRouteWaitList = new(), goldWonderWaitList = new(), goldTradeCenterWaitList = new();
    public string tutorialStep;
    public Dictionary<string, (bool, bool)> conversationTaskDict = new();
    public bool scottFollow;

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

    public WorkerData playerUnit, scott;
    public Dictionary<Vector3Int, TerrainSaveData> allTerrain = new();
    public Dictionary<Vector3Int, TradeCenterData> allTradeCenters = new();
    public List<WonderData> allWonders = new();
    public List<CityData> allCities = new();
    public Dictionary<Vector3Int, ArmyData> allArmies = new();
    public List<RoadData> allRoads = new();
    public List<CityImprovementData> allCityImprovements = new();
    public List<UnitData> allUnits = new();

}
