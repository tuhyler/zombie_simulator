using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Improvement Data", menuName = "EconomyData/ImprovementData")]
public class ImprovementDataSO : ScriptableObject
{
    public GameObject prefab;
    public List<ImprovementDataSO> secondaryData;
    public AudioClip audio;
    public bool isSecondary;
    public bool isBuilding;
    public bool isBuildingImprovement;
    public string improvementName;
    public string improvementDisplayName;
    public int improvementLevel;
    public string improvementNameAndLevel;
    public string improvementDescription;
    public Vector3 buildingLocation;
    public Sprite image;
    public Sprite mapIcon;
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
    public RawResourceType rawResourceType; //used for highlight tiles in city
    public bool isResearch; //to identify which improvements produce research
    public TerrainType terrainType;
    public SpecificTerrain specificTerrain;
    public float workEthicChange; 
    public int maxLabor; //max amount of labor
    //public int laborCost; //how much gold to charge labor
    public int housingIncrease;
    public bool cityHousing;
    public int waterIncrease;
    public bool replaceTerrain = false; //prefab replaces terrain
    public bool replaceRocks = false; //setting rocks as the same color as the prop
    public bool singleBuild = false; //only one per city
    public bool oneTerrain = true; //can only be built on one type of terrain
    public bool adjustForHill = true; //shift the improvement up when built on hill
    public bool hideProp = true; //hiding the prop already on terrain
    public float hillAdjustment = 0.6f;
    public int buildTime;
    public bool availableInitially = false;
    private bool locked = true;
    public bool Locked { get { return locked; } set { locked = value; } }
    public bool hideIdleMesh = false;
    public bool getTerrainResource = false; //for mines and quarries to provide resource where they're placed
    public List<Vector3Int> noWalkAreas;
}
