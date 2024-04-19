using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WorkerData : IUnitData
{
	public string unitNameAndLevel, transportTarget;
	public Vector3 position;
	public Quaternion rotation;
	public List<Vector3Int> moveOrders;
	public bool secondaryPrefab, isBusy, isMoving, somethingToSay, building, removing, gathering, clearingForest, clearedForest, buildingCity, harvested, harvestedForest, 
		firstStep, /*runningAway, stepAside,*/ toTransport, inTransport, inEnemyLines;
	public Vector3 destinationLoc;
	public Vector3 finalDestinationLoc;
	public Vector3Int currentLocation, prevTile, resourceCityLoc, prevFriendlyTile;
	public Vector3Int prevTerrainTile;
	public List<Vector3Int> orderList;
	public int timePassed;
	public List<string> conversationTopics;

	//personal resource info (just for koa)
	public Dictionary<ResourceType, int> resourceDict;
	public int resourceStorageLevel;
	public Dictionary<ResourceType, int> resourceGridDict;

	public WorkerData GetWorkerData()
	{
		return this;
	}
	public UnitData GetUnitData()
	{
		return null;
	}
	public TraderData GetTraderData()
	{
		return null;
	}
	public LaborerData GetLaborerData()
	{
		return null;
	}
	public TransportData GetTransportData()
	{
		return null;
	}
	string IUnitData.unitNameAndLevel => unitNameAndLevel;
	bool IUnitData.secondaryPrefab => secondaryPrefab;
	Vector3 IUnitData.position => position;
	Quaternion IUnitData.rotation => rotation;
	Vector3Int IUnitData.barracksBunk => Vector3Int.zero;
}
