using UnityEngine;
//using UnityEngine.UI;

[CreateAssetMenu(fileName = "Resource Data", menuName = "Resource/ResourceData")]
public class ResourceIndividualSO : ScriptableObject
{
    //public GameObject prefab;
    public ResourceType resourceType;
    public float resourceStorageMultiplier;
    public string resourceName;
    public Sprite resourceIcon;
    public int ResourceGatheringTime = 5;
    public int resourcePrice;
    public int resourceQuantityPerPop = 1;
    public RawResourceType rawResource;
    //public ResourceValue resourceValue;
}
