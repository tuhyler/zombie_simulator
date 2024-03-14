using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class UnitData : IUnitData
{
    /*used for military units, both friendly and enemy*/
	//================================================//

	public string unitNameAndLevel;
    public Vector3 position; 
	public Vector3Int campSpot; //campSpot is for enemy camps
	public Quaternion rotation;
    public List<Vector3Int> moveOrders;
	public bool secondaryPrefab, moreToMove, isMoving, somethingToSay;
	public Vector3 destinationLoc;
    public Vector3 finalDestinationLoc;
    public Vector3Int currentLocation;
	public Vector3Int prevTerrainTile;
	public int idleTime;
	public List<string> conversationTopics;

	//combat
	public Vector3Int cityHomeBase, barracksBunk, marchPosition, targetBunk;
	public int currentHealth;
	public float baseSpeed;
	public bool readyToMarch, atHome, preparingToMoveOut, isMarching, transferring, repositioning, inBattle, attacking, targetSearching, flanking, flankedOnce, cavalryLine, isDead, isUpgrading, looking, ambush, aoe, guard, isGuarding, returning, atSea;

	public WorkerData GetWorkerData()
	{
		return null;
	}
	public UnitData GetUnitData()
	{
		return this;
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
	Vector3Int IUnitData.barracksBunk => barracksBunk;
}

public interface IUnitData
{
	string unitNameAndLevel { get; }
	bool secondaryPrefab { get; }
	Vector3 position { get; }
	Quaternion rotation { get; }
	Vector3Int barracksBunk { get; }

	WorkerData GetWorkerData();
	UnitData GetUnitData();
	TraderData GetTraderData();
	LaborerData GetLaborerData();
	TransportData GetTransportData();
}
