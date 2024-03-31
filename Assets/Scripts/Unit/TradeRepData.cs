using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TradeRepData
{
	public string name;
	public bool somethingToSay, onQuest, hasSomethingToSay;
	public List<string> conversationTopics;
	public int currentQuest, timeWaited, purchasedAmount;
	public Vector3 position;
	public Quaternion rotation;
	public Vector3Int currentLocation;
}
