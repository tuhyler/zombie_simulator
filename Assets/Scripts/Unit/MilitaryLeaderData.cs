using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MilitaryLeaderData
{
	public bool somethingToSay, hasSomethingToSay, defending, dueling;
	public List<string> conversationTopics;
	public int timeWaited;
	public Vector3Int attackingCity, capitalCity;
	public List<Vector3Int> empireCities;
}
