using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Utility Cost Data", menuName = "EconomyData/UtilityCostData")]
public class UtilityCostSO : ScriptableObject
{
	public string utilityName;
	public Material utilityMaterial;
	public int utilityLevel;
	public int movementSpeed;
	public List<ResourceValue> utilityCost; //cost to build
	public List<ResourceValue> bridgeCost; //cost to build bridge (For roads:
	public GameObject bridgePrefab;
}