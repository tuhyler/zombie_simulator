using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Improvement Data", menuName = "EconomyData/ImprovementData")]
public class ImprovementDataSO : ScriptableObject
{
    public GameObject prefab;
    public ImprovementDataSO secondaryData;
    public bool isBuilding;
    public string improvementName;
    public int improvementLevel;
    public string improvementNameAndLevel;
    public string improvementDescription;
    public bool rawMaterials;
    public Vector3 buildingLocation;
    public Sprite image;
    public List<ResourceValue> improvementCost;
    //for each produced resource, set consumed resources in the same order as the produced resources
    public List<ResourceValue> consumedResources = new();
    public List<ResourceValue> consumedResources1 = new();
    public List<ResourceValue> consumedResources2 = new();
    public List<ResourceValue> consumedResources3 = new();
    public List<ResourceValue> consumedResources4 = new();
    public List<List<ResourceValue>> allConsumedResources = new();
    public List<ResourceValue> producedResources;
    public int producedResourceTime;
    public ResourceType resourceType; //used for highlight tiles in city
    public TerrainType terrainType;
    public float workEthicChange; 
    public int maxLabor; //max amount of labor
    public int laborCost; //how much gold to charge labor
    public int housingIncrease;
    public bool cityHousing;
    public bool replaceTerrain = false; //prefab replaces terrain
    public bool replaceRocks = false; //setting rocks as the same color as the prop
    public bool returnProp = false; //return terrain prop when removing it
    public bool singleBuild = false; //only one per city
    public int buildTime;
    public bool availableInitially = false;
    public bool locked = true;
    public bool workAnimLoop = false;
    public bool hideAnimMesh = false;

    private void Awake()
    {
        allConsumedResources.Add(consumedResources);
        allConsumedResources.Add(consumedResources1);
        allConsumedResources.Add(consumedResources2);
        allConsumedResources.Add(consumedResources3);
        allConsumedResources.Add(consumedResources4);
    }
}
