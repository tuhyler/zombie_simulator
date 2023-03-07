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
    public bool returnProp = false; //return terrain prop when removing it
    public bool singleBuild = false; //only one per city
    public int buildTime;
    public bool availableInitially = false;
    public bool locked = true;
    public Vector3 workFireLoc;
    public Vector3 workSmokeLoc;
    public bool workAnimLoop = false;
    public bool hideAnimMesh = false;
}
