using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Wonder Data", menuName = "EconomyData/WonderData")]
public class WonderDataSO : ScriptableObject
{
    public GameObject wonderPrefab;
    //public MeshRenderer[] prefabRenderers;
    public MeshRenderer mesh0Percent;
    public MeshRenderer mesh25Percent;
    public MeshRenderer mesh50Percent;
    public MeshRenderer mesh75Percent;
    public MeshRenderer meshComplete;
    public string wonderName;
    public string wonderDecription = "Fill in description";
    public int workersNeeded = 1;
    public int workerCost;
    public int sizeHeight;
    public int sizeWidth;
    public Vector3Int unloadLoc;
    public Sprite image;
    public List<ResourceValue> wonderCost;
    public TerrainType terrainType;
    public float workEthicChange;
    public bool replaceTerrain = false; //prefab replaces terrain
    public bool replaceProp = true; //replace terrain prop when building upon it
    public int buildTimePerPercent = 10;
    public bool locked = true;
    public UnitBuildDataSO workerData;
}
