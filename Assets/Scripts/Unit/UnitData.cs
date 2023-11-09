using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class UnitData
{
    public string unitNameAndLevel;
    public Vector3 position; 
	public Vector3Int campSpot; //campSpot is for enemy camps
	public Quaternion rotation;
    public List<Vector3Int> moveOrders;
	public bool secondaryPrefab, moreToMove, isBusy, isMoving, isBeached, interruptedRoute, atStop, followingRoute, isWaiting, harvested, somethingToSay;
	public Vector3 destinationLoc;
    public Vector3 finalDestinationLoc;
    public Vector3Int currentLocation;
	public Vector3Int prevRoadTile, prevTerrainTile;

	//combat
	public Vector3Int cityHomeBase, barracksBunk, marchPosition, targetBunk;
	public int currentHealth;
	public float baseSpeed;
	public bool isLeader, readyToMarch, atHome, preparingToMoveOut, isMarching, transferring, repositioning, inBattle, attacking, targetSearching, flanking, flankedOnce, cavalryLine, isDead, isUpgrading;	
}
