using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BodyGuardData
{
	public string transportationTarget;
	public bool somethingToSay, toTransport, inTransport, dueling, waiting, dizzy;
	public List<string> conversationTopics;
	public Vector3Int forward;
}
