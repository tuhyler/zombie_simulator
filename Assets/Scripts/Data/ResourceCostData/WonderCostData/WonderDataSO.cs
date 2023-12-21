using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Wonder Data", menuName = "EconomyData/WonderData")]
public class WonderDataSO : ScriptableObject
{
    public GameObject wonderPrefab;
    public string wonderName;
    public string wonderDisplayName;
    public string wonderDecription = "Fill in description";
    public Era wonderEra;
    public int workersNeeded = 1;
    public int workerCost;
    public int sizeHeight = 2;
    public int sizeWidth = 2;
    public Vector3Int unloadLoc;
    public Sprite image;
    public List<ResourceValue> wonderCost;
    public TerrainType terrainType;
    public float wonderBenefitChange;
    public bool replaceTerrain = false; //prefab replaces terrain
    public bool replaceProp = true; //replace terrain prop when building upon it
    public int buildTimePerPercent = 10;
    public bool locked = true;
    public bool isSea = false;
    public UnitBuildDataSO workerData;
}
