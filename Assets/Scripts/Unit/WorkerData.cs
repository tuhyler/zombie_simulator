using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WorkerData : IUnitData
{
	public string unitNameAndLevel;
	public Vector3 position;
	public Quaternion rotation;
	public List<Vector3Int> moveOrders;
	public bool secondaryPrefab, isBusy, moreToMove, isMoving, somethingToSay, building, removing, gathering, clearingForest, clearedForest, buildingCity, harvested, harvestedForest, firstStep;
	public Vector3 destinationLoc;
	public Vector3 finalDestinationLoc;
	public Vector3Int currentLocation, prevTile, resourceCityLoc;
	public Vector3Int prevTerrainTile;
	public List<Vector3Int> orderList;
	public int timePassed;
	public string conversationTopic;

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
	string IUnitData.unitNameAndLevel => unitNameAndLevel;
	bool IUnitData.secondaryPrefab => secondaryPrefab;
	Vector3Int IUnitData.currentLocation => currentLocation;
	Quaternion IUnitData.rotation => rotation;
	Vector3Int IUnitData.barracksBunk => Vector3Int.zero;
}
