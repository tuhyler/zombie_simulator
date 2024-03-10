using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC : Unit
{
	public string npcName;
	public Sprite npcImage;
	[HideInInspector]
	public int rapportScore;
	public int angryIncrease;
	public int happyDiscount;
	public int ecstaticDiscount;

	private ResourceValue desiredGift;

	public bool GiftCheck(ResourceValue value)
	{
		bool likes;

		if (value.resourceType == desiredGift.resourceType && value.resourceAmount >= desiredGift.resourceAmount)
			likes = true;
		else
			likes = false;

		if (likes)
		{
			if (rapportScore <= 12)
				rapportScore++;
			return true;
		}
		else
		{
			if (rapportScore >= -12)
				rapportScore--;
			return false;
		}
	}

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
