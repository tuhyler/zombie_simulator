using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TradeRep : Unit
{
	[HideInInspector]
	public TradeCenter center;
	[HideInInspector]
	public string tradeRepName;
	public Sprite npcImage;
	[HideInInspector]
	public int rapportScore;
	public int angryIncrease;
	public int happyDiscount;
	public int ecstaticDiscount;
	public List<ResourceValue> questGoals = new();
	public List<int> questRewardsInGold = new();
	public List<string> questHints = new();
	public int waitForNextQuest = 10;
	public int purchasedAmountBaseThreshold = 500;
	[HideInInspector]
	public int currentQuest, purchasedAmount;
	private int timeWaited = 0;

	private ResourceValue desiredGift;
	[HideInInspector]
	public bool onQuest;

	private void Awake()
	{
		AwakeMethods();
		tradeRep = this;
	}

	public void SetUpTradeRep(MapWorld world)
	{
		tradeRepName = buildDataSO.unitDisplayName;
		name = buildDataSO.unitDisplayName;
		SetReferences(world, true);
		world.uiSpeechWindow.AddToSpeakingDict(tradeRepName, this);
		Vector3 actualPosition = transform.position;
		world.SetNPCLoc(actualPosition, this);
		currentLocation = world.RoundToInt(actualPosition);
		world.allTCReps[tradeRepName] = this;
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
			ChangeRapport(true);
			PlayAudioClip(world.cityBuilderManager.receiveGift);
			CompleteQuest();
			return true;
		}
		else
		{
			ChangeRapport(false);
			PlayAudioClip(world.cityBuilderManager.denyGift);
			return false;
		}
	}

	public void IncreasePurchasedAmount(int amount)
	{
		purchasedAmount += amount;

		if (rapportScore < 5 && purchasedAmount >= purchasedAmountBaseThreshold * Mathf.Max(1, rapportScore))
		{
			ChangeRapport(true);
			PlayAudioClip(world.cityBuilderManager.receiveGift);
			world.PlayGiftResponse(transform.position, true);
		}
	}

	public void ChangeRapport(bool increase)
	{
		if (increase)
		{
			if (rapportScore < 5)
				rapportScore++;
			else
				return;
		}
		else
		{
			if (rapportScore > -5)
				rapportScore--;
			else
				return;
		}

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
		desiredGift = questGoals[currentQuest];
		SetSomethingToSay(tradeRepName + "_quest" + currentQuest.ToString());
	}

	public void CompleteQuest()
	{
		SetSomethingToSay(tradeRepName + "_quest" + currentQuest.ToString() + "_complete");
		currentQuest++;
		onQuest = false;
		//world.uiConversationTaskManager.CompleteTask(npcName, true);
	}

	public void BeginNextQuestWait(bool giveReward)
	{
		if (giveReward)
			world.ReceiveQuestReward(world.mainPlayer.transform.position, questRewardsInGold[currentQuest - 1]);

		if (currentQuest < questGoals.Count)
		{
			timeWaited = waitForNextQuest;
			StartCoroutine(SetNextQuestWait());
		}
	}

	public void CreateConversationTaskItem()
	{
		world.uiConversationTaskManager.CreateConversationTask(tradeRepName, false);
	}

	public void SetSomethingToSay(string conversationTopic)
	{
		conversationHaver.SetSomethingToSay(conversationTopic);
	}

	public TradeRepData SaveTradeRepData()
	{
		TradeRepData data = new TradeRepData();

		data.name = tradeRepName;
		data.position = transform.position;
		data.rotation = transform.rotation;
		data.currentLocation = currentLocation;
		data.somethingToSay = somethingToSay;
		data.onQuest = onQuest;
		data.conversationTopics = conversationHaver.conversationTopics;
		data.currentQuest = currentQuest;
		data.timeWaited = timeWaited;
		data.purchasedAmount = purchasedAmount;

		return data;
	}

	public void LoadTradeRepData(TradeRepData data)
	{
		transform.position = data.position;
		transform.rotation = data.rotation;
		currentLocation = data.currentLocation;
		onQuest = data.onQuest;
		currentQuest = data.currentQuest;
		timeWaited = data.timeWaited;
		purchasedAmount = data.purchasedAmount;

		if (currentQuest < questGoals.Count)
			desiredGift = questGoals[currentQuest];

		if (data.somethingToSay)
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
