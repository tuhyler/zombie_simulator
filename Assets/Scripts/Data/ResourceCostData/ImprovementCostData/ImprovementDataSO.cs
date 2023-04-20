using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Improvement Data", menuName = "EconomyData/ImprovementData")]
public class ImprovementDataSO : ScriptableObject
{
    public GameObject prefab;
    public ImprovementDataSO secondaryData;
    public bool isBuilding;
    public string improvementName;
    public string improvementDisplayName;
    public int improvementLevel;
    public string improvementNameAndLevel;
    public string improvementDescription;
    public Vector3 buildingLocation;
    public Sprite image;
    public Sprite littleImage;
    public List<ResourceValue> improvementCost;
    //for each produced resource, set consumed resources in the same order as the produced resources
    public List<ResourceValue> consumedResources;
    public List<ResourceValue> consumedResources1;
    public List<ResourceValue> consumedResources2;
    public List<ResourceValue> consumedResources3;
    public List<ResourceValue> consumedResources4;
    public List<ResourceValue> producedResources;
    public List<int> producedResourceTime;
    public bool rawMaterials;
    public ResourceType rawResourceType; //used for highlight tiles in city
    public TerrainType terrainType;
    public float workEthicChange; 
    public int maxLabor; //max amount of labor
    //public int laborCost; //how much gold to charge labor
    public int housingIncrease;
    public bool cityHousing;
    public bool replaceTerrain = false; //prefab replaces terrain
    public bool replaceRocks = false; //setting rocks as the same color as the prop
    public bool returnProp = false; //return terrain prop when removing it
    public bool singleBuild = false; //only one per city
    public int buildTime;
    public bool availableInitially = false;
    private bool locked = true;
    public bool Locked { get { return locked; } set { locked = value; } }
    public bool workAnimLoop = false;
    public bool hideAnimMesh = false;
}
