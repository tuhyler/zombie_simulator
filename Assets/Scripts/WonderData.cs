using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WonderData
{
	public string name;
	public Vector3 centerPos;
	public Quaternion rotation;
	public Vector3Int unloadLoc, harborLoc;
	public int percentDone, workersReceived, timePassed;
	public bool isConstructing, canBuildHarbor, hasHarbor, roadPreExisted, isBuilding;
	public List<Vector3Int> wonderLocs, possibleHarborLocs, coastTiles;
	public Dictionary<ResourceType, int> resourceGridDict;
	public List<int> waitList = new(), seaWaitList = new();
}
