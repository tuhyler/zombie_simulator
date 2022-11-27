using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Improvement Data", menuName = "EconomyData/ImprovementData")]
public class ImprovementDataSO : ScriptableObject
{
    public GameObject prefab;
    public string improvementName;
    public int improvementLevel;
    public bool rawMaterials;
    public Vector3 buildingLocation;
    public Sprite image;
    public List<ResourceValue> improvementCost;
    public List<ResourceValue> consumedResources;
    public List<ResourceValue> producedResources;
    public int producedResourceTime;
    public ResourceType resourceType; //used for highlight tiles in city
    public TerrainType terrainType;
    public float workEthicChange; 
    public int maxLabor; //max amount of labor
    public int laborCost; //how much gold to charge labor
    public bool replaceTerrain = false; //prefab replaces terrain
    public bool replaceProp = true; //replace terrain prop when building upon it
    public bool singleBuild = false; //only one per city
    public int buildTime;
}
