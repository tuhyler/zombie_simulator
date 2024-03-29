using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NPCData
{
    public string name;
    public bool somethingToSay, onQuest, moreToMove, isMoving, hasSomethingToSay;
    public List<string> conversationTopics;
    public int currentQuest, timeWaited, purchasedAmount;
    public Vector3 position, destinationLoc, finalDestinationLoc;
    public Quaternion rotation;
    public Vector3Int currentLocation, prevTile, prevTerrainTile;
    // enemy empire data
    public List<Vector3Int> moveOrders;
    public Vector3Int attackingCity, capitalCity;
    public List<Vector3Int> empireCities;
}
