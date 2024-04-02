using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BodyGuardData
{
	public string transportationTarget;
	public bool somethingToSay, toTransport, inTransport, dueling;
	public List<string> conversationTopics;
}
