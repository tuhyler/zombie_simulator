using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC : Unit
{
	private TradeCenter center;
	public string npcName;
	public Sprite npcImage;
	[HideInInspector]
	public int rapportScore;
	public int angryIncrease;
	public int happyDiscount;
	public int ecstaticDiscount;
	public string introductionConversation;
	public List<ResourceValue> questGoals = new();
	public int waitForNextQuest = 60;
	public int purchasedAmountBaseThreshold = 500;
	[HideInInspector]
	public int currentQuest, purchasedAmount;
	private int timeWaited = 0;

	private ResourceValue desiredGift;
	[HideInInspector]
	public bool onQuest, hasMetKoa;

	private void Start()
	{
		if (!hasMetKoa)
			SetSomethingToSay(introductionConversation);
	}

	public void SetTradeCenter(TradeCenter center)
	{
		this.center = center;
	}

	public bool GiftCheck(ResourceValue value)
	{
		bool likes;

		if (value.resourceType == desiredGift.resourceType && value.resourceAmount >= desiredGift.resourceAmount)
			likes = true;
		else
			likes = false;

		if (likes)
		{
			if (rapportScore < 5)
				rapportScore++;

			PlayAudioClip(world.cityBuilderManager.receiveGift);
			CompleteQuest();
			return true;
		}
		else
		{
			if (rapportScore > -5)
				rapportScore--;

			PlayAudioClip(world.cityBuilderManager.denyGift);
			return false;
		}
	}

	public void IncreasePurchasedAmount(int amount)
	{
		purchasedAmount += amount;

		if (purchasedAmount >= purchasedAmountBaseThreshold * Mathf.Max(1, rapportScore))
		{
			IncreaseRapport();
			PlayAudioClip(world.cityBuilderManager.receiveGift);
			world.PlayGiftResponse(transform.position, true);
		}
	}

	public void IncreaseRapport()
	{
		if (rapportScore < 5)
			rapportScore++;

		center.CheckRapport();
		if (world.cityBuilderManager.uiTradeCenter.activeStatus && world.cityBuilderManager.uiTradeCenter.center == center)
			world.cityBuilderManager.uiTradeCenter.SetHappinessMeter(this);
	}

	public IEnumerator SetNextQuestWait()
	{
		while (timeWaited > 0)
		{
			yield return attackPauses[0];
			timeWaited--;
		}

		SetNextQuest();
	}

	public void SetNextQuest()
	{
		onQuest = true;
		desiredGift = questGoals[currentQuest];
		SetSomethingToSay(npcName + "_quest" + currentQuest.ToString());
	}

	public void CompleteQuest()
	{
		SetSomethingToSay(npcName + "_quest" + currentQuest.ToString() + "_complete");
		currentQuest++;
		onQuest = false;

		timeWaited = waitForNextQuest;
		StartCoroutine(SetNextQuestWait());
	}

	public void SetSomethingToSay(string conversationTopic)
	{
		conversationHaver.SetSomethingToSay(conversationTopic);
	}

	public NPCData SaveNPCData()
	{
		NPCData data = new NPCData();

		data.somethingToSay = somethingToSay;
		data.onQuest = onQuest;
		data.hasMetKoa = hasMetKoa;
		data.conversationTopics = conversationHaver.conversationTopics;
		data.currentQuest = currentQuest;
		data.timeWaited = timeWaited;
		data.purchasedAmount = purchasedAmount;

		return data;
	}

	public void LoadNPCData(NPCData data)
	{
		somethingToSay = data.somethingToSay;
		onQuest = data.onQuest;
		hasMetKoa = data.hasMetKoa;
		currentQuest = data.currentQuest;
		timeWaited = data.timeWaited;
		purchasedAmount = data.purchasedAmount;
		desiredGift = questGoals[currentQuest];

		if (somethingToSay)
		{
			conversationHaver.conversationTopics = new(data.conversationTopics);
			data.conversationTopics.Clear();
			SetSomethingToSay(conversationHaver.conversationTopics[0]);
		}

		if (timeWaited > 0)
		{
			StartCoroutine(SetNextQuestWait());
		}
	}
}
