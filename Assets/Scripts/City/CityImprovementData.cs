using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CityImprovementData
{
    public string name;
    public Vector3Int location, cityLoc;
	public bool queued, isConstruction, isUpgrading, isTraining;
	public int housingIndex, laborCost, producedResourceIndex;
    public ResourceType producedResource;

}
