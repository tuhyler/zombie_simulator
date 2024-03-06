using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC : Unit
{
	public string npcName;
	public Sprite npcImage;
	[HideInInspector]
	public int rapportScore;
	
	public NPCData SaveNPCData()
	{
		NPCData data = new NPCData();

		data.somethingToSay = somethingToSay;
		data.conversationTopics = conversationHaver.conversationTopics;

		return data;
	}

	public void LoadNPCData(NPCData data)
	{
		somethingToSay = data.somethingToSay;

		if (somethingToSay)
		{
			conversationHaver.conversationTopics = new(data.conversationTopics);
			data.conversationTopics.Clear();
			conversationHaver.SetSomethingToSay(conversationHaver.conversationTopics[0]);
		}
	}
}
