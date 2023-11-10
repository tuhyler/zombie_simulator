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
	public bool secondaryPrefab, isBusy, moreToMove, isMoving, somethingToSay;
	public Vector3 destinationLoc;
	public Vector3 finalDestinationLoc;
	public Vector3Int currentLocation;
	public Vector3Int prevTerrainTile;

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
