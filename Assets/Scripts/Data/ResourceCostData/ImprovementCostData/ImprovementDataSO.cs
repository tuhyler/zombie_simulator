using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Improvement Data", menuName = "EconomyData/ImprovementData")]
public class ImprovementDataSO : ScriptableObject
{
    public GameObject prefab;
    public string improvementName;
    public Sprite image;
    public List<ResourceValue> improvementCost;
    public List<ResourceValue> consumedResources;
    public List<ResourceValue> producedResources;
    public int producedResourceTime;
    public ResourceType resourceType; //used for highlight tiles in city
    public float workEthicChange;
    public int maxLabor;
    public int laborCost;
}
