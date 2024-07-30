using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MilitaryLeaderData
{
	public bool somethingToSay, hasSomethingToSay, defending, dueling, paused;
	public List<string> conversationTopics;
	public int timeWaited, challenges, empireUnitCount, pauseTimer;
	public Vector3Int attackingCity, capitalCity, forward, duelLoc;
	public List<Vector3Int> empireCities;
}
