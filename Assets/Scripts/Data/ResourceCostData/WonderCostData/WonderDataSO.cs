using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Wonder Data", menuName = "EconomyData/WonderData")]
public class WonderDataSO : ScriptableObject
{
    public GameObject prefab0Percent;
    public GameObject prefab25Percent;
    public GameObject prefab50Percent;
    public GameObject prefab75Percent;
    public GameObject prefabComplete;
    public string wonderName;
    public string wonderDecription = "Fill in description";
    public int workerCount = 1;
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
}
