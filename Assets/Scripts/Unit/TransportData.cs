using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TransportData: IUnitData
{
	public string unitNameAndLevel;
	public Vector3 position;
	public Quaternion rotation;
	public List<Vector3Int> moveOrders;
	public int passengerCount;
	public bool isMoving, moreToMove, canMove, hasKoa, hasScott, hasAzai;
	public Vector3 destinationLoc;
	public Vector3 finalDestinationLoc;
	public Vector3Int currentLocation, prevTile;
	public Vector3Int prevTerrainTile;

	public TransportData GetTransportData()
	{
		return this;
	}
	public LaborerData GetLaborerData()
	{
		return null;
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
	bool IUnitData.secondaryPrefab => false;
	Vector3 IUnitData.position => position;
	Quaternion IUnitData.rotation => rotation;
	Vector3Int IUnitData.barracksBunk => Vector3Int.zero;
}
