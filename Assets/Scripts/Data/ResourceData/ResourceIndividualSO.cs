using UnityEngine;
//using UnityEngine.UI;

[CreateAssetMenu(fileName = "Resource Data", menuName = "Resource/ResourceData")]
public class ResourceIndividualSO : ScriptableObject
{
    //public GameObject prefab;
    public ResourceType resourceType;
    public float resourceStorageMultiplier = 1;
    public string resourceName;
    public Sprite resourceIcon;
    public int ResourceGatheringTime = 5;
    public int ResourceGatheringAmount = 1;
    public int resourcePrice;
    public int resourceQuantityPerPop = 1;
    public RawResourceType rawResource;
    public RocksType rocksType = RocksType.None;
    public ResourceCategory resourceCategory; //for the resource selection grid
    public string requirement; //for terrain data, which improvement is required to harvest
    public Vector2 uvCoordinatesForRocks;
    public bool forVisual = false; //for resources that are just a different picture for another resource (such as fish)
    public bool sellResource = false;
    //public ResourceValue resourceValue;
}
