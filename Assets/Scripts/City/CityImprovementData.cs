using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CityImprovementData
{
    public string name, trainingUnitName;
    public Vector3Int location, cityLoc;
	public int rotation; //int because only matters for harbor
	public bool queued, isConstruction, isUpgrading, isTraining, isWaitingForStorageRoom, isWaitingforResources, isWaitingToUnload, isWaitingForResearch, isProducing;
	public int housingIndex, laborCost, timePassed, producedResourceIndex, currentLabor, productionTimer, upgradeLevel;
    public float tempLabor, unloadLabor;
    public ResourceType producedResource;
    public List<float> tempLaborPercsList;
}
