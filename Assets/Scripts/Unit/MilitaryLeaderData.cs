using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MilitaryLeaderData
{
	public bool somethingToSay, hasSomethingToSay, defending, dueling;
	public List<string> conversationTopics;
	public int timeWaited, challenges, empireUnitCount;
	public Vector3Int attackingCity, capitalCity, forward, duelLoc;
	public List<Vector3Int> empireCities;
}
