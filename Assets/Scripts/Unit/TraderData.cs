using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TraderData : IUnitData
{
	public int id, currentHealth;
	public string unitNameAndLevel, unitName;
	public Vector3 position;
	public Quaternion rotation;
	public List<Vector3Int> moveOrders;
	public bool secondaryPrefab, isMoving, interruptedRoute, atStop, followingRoute, isWaiting, isUpgrading, paid, hasRoute, waitingOnRouteCosts, ambush, guarded, waitingOnGuard, atHome, 
		returning, movingUpInLine, posSet, atStall;
	public Vector3 destinationLoc;
	public Vector3 finalDestinationLoc;
	public Vector3Int currentLocation, ambushLoc, homeCity;
	public Vector3Int prevTile;
	public UnitData guardUnit;

	//personal resource info
	public Dictionary<ResourceType, int> resourceGridDict;
	public Dictionary<ResourceType, int> resourceDict;
	public int resourceStorageLevel;

	//route info
	public int startingStop, currentStop, currentResource, resourceCurrentAmount, resourceTotalAmount, timeWaited, goldNeeded, amountMoved, linePause;
	public List<Vector3Int> cityStops;
	public List<List<ResourceValue>> resourceAssignments;
	public List<List<int>> resourceCompletion;
	public List<int> waitTimes;
	public Vector3Int currentDestination;
	public bool resourceCheck, waitForever;

	//public float percDone;

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
		return this;
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
