using System.Collections.Generic;
using UnityEngine;

[SerializeField]
public class LaborerData : IUnitData
{
	public string unitNameAndLevel, unitName;
	public int totalWait;
	public Vector3 position, destinationLoc, finalDestinationLoc;
	public Quaternion rotation;
	public List<Vector3Int> moveOrders;
	public bool secondary, isMoving, somethingToSay, celebrating, atSea;
	public Vector3Int currentLocation, homeCityLoc;

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
	public TransportData GetTransportData()
	{
		return null;
	}
	string IUnitData.unitNameAndLevel => unitNameAndLevel;
	bool IUnitData.secondaryPrefab => secondary;
	Vector3 IUnitData.position => position;
	Quaternion IUnitData.rotation => rotation;
	Vector3Int IUnitData.barracksBunk => Vector3Int.zero;
}
