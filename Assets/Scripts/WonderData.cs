using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WonderData
{
	public string name;
	public Vector3 centerPos;
	public Quaternion rotation;
	public Vector3Int unloadLoc;
	public Dictionary<SingleBuildType, Vector3Int> singleBuildDict;
	public int percentDone, workersReceived, timePassed;
	public bool isConstructing, canBuildHarbor, hadRoad, isBuilding, completed;
	public List<Vector3Int> wonderLocs, possibleHarborLocs, coastTiles;
	public Dictionary<ResourceType, int> resourceDict, resourceGridDict;
	public List<int> waitList = new(), seaWaitList = new();
	public List<(bool, Vector3Int)> workerSexAndHome = new();
}
