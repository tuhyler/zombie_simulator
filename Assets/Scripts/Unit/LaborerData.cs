using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SerializeField]
public class LaborerData : IUnitData
{
	public string unitNameAndLevel, unitName;
	public Vector3 position;
	public Quaternion rotation;
	public List<Vector3Int> moveOrders;
	public bool secondaryPrefab, moreToMove, isMoving, somethingToSay;
	public Vector3 destinationLoc;
	public Vector3 finalDestinationLoc;
	public Vector3Int currentLocation;
	public Vector3Int prevTerrainTile;
	public string conversationTopic;

	public LaborerData GetLaborerData()
	{
		return this;
	}
	public WorkerData GetWorkerData()
	{
		return null;
	}
	public UnitData GetUnitData()
	{
		return null;
	}
	public TraderData GetTraderData()
	{
		return null;
	}
	string IUnitData.unitNameAndLevel => unitNameAndLevel;
	bool IUnitData.secondaryPrefab => secondaryPrefab;
	Vector3 IUnitData.position => position;
	Quaternion IUnitData.rotation => rotation;
	Vector3Int IUnitData.barracksBunk => Vector3Int.zero;
}
