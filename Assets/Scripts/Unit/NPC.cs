using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class NPC : Unit
{
	[HideInInspector]
	public TradeCenter center;
	[HideInInspector]
	public string npcName;
	public Sprite npcImage;
	[HideInInspector]
	public int rapportScore;
	public int angryIncrease;
	public int happyDiscount;
	public int ecstaticDiscount;
	public List<ResourceValue> questGoals = new();
	public List<string> questHints = new();
	public int waitForNextQuest = 10;
	public int purchasedAmountBaseThreshold = 500;
	[HideInInspector]
	public int currentQuest, purchasedAmount;
	private int timeWaited = 0;

	private ResourceValue desiredGift;
	[HideInInspector]
	public bool onQuest, hasSomethingToSay;

	[HideInInspector]
	public EnemyEmpire empire;

	private void Awake()
	{
		AwakeMethods();
		npc = GetComponent<NPC>();
	}

	public void SetUpNPC(MapWorld world)
	{
		npcName = buildDataSO.unitName;
		name = buildDataSO.unitName;
		SetReferences(world, true);
		world.uiSpeechWindow.AddToSpeakingDict(npcName, this);
		Vector3 actualPosition = transform.position;
		world.SetNPCLoc(actualPosition, this);
		currentLocation = world.RoundToInt(actualPosition);
		world.allTCReps[npcName] = this;
	}

	public void SetTradeCenter(TradeCenter center)
	{
		this.center = center;
	}

	public void SetEmpire(EnemyEmpire empire)
	{
		this.empire = empire;
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

		if (rapportScore < 5 && purchasedAmount >= purchasedAmountBaseThreshold * Mathf.Max(1, rapportScore))
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
		else
			return;

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
		//world.uiConversationTaskManager.CompleteTask(npcName, true);
	}

	public void BeginNextQuestWait()
	{
		if (currentQuest < questGoals.Count)
		{
			timeWaited = waitForNextQuest;
			StartCoroutine(SetNextQuestWait());
		}
	}

	public void CreateConversationTaskItem()
	{
		world.uiConversationTaskManager.CreateConversationTask(npcName, false);
	}

	public void SetSomethingToSay(string conversationTopic)
	{
		conversationHaver.SetSomethingToSay(conversationTopic);
	}

	public void CancelApproachingConversation()
	{
		if (somethingToSay)
		{
			somethingToSay = false;
			hasSomethingToSay = true;
			questionMark.SetActive(false);
		}
		
		if (world.mainPlayer.inEnemyLines)
		{
			world.mainPlayer.ReturnToFriendlyTile();
		}
		else if (world.RoundToInt(world.mainPlayer.finalDestinationLoc) == currentLocation)
		{
			world.mainPlayer.StopAnimation();
			world.mainPlayer.ShiftMovement();
			world.mainPlayer.StopMovement();
		}
	}

	public void FinishMovementNPC(Vector3 endPosition)
	{
		if (false)
		{

		}
		else if (hasSomethingToSay)
		{
			hasSomethingToSay = false;
			somethingToSay = true;
			questionMark.SetActive(true);
		}
	}

	public NPCData SaveNPCData()
	{
		NPCData data = new NPCData();

		data.name = npcName;
		data.position = transform.position;
		data.rotation = transform.rotation;
		data.destinationLoc = destinationLoc;
		data.finalDestinationLoc = finalDestinationLoc;
		data.currentLocation = currentLocation;
		data.prevTile = prevTile;
		data.prevTerrainTile = prevTerrainTile;
		data.moveOrders = pathPositions.ToList();
		data.isMoving = isMoving;
		data.moreToMove = moreToMove;
		data.somethingToSay = somethingToSay;
		data.onQuest = onQuest;
		data.conversationTopics = conversationHaver.conversationTopics;
		data.currentQuest = currentQuest;
		data.timeWaited = timeWaited;
		data.purchasedAmount = purchasedAmount;
		data.hasSomethingToSay = hasSomethingToSay;

		if (empire != null)
		{
			data.attackingCity = empire.attackingCity;
			data.capitalCity = empire.capitalCity;
			data.empireCities = new();

			for (int i = 0; i < empire.empireCities.Count; i++)
				data.empireCities.Add(empire.empireCities[i]);
		}

		return data;
	}

	public void LoadNPCData(NPCData data)
	{
		transform.position = data.position;
		transform.rotation = data.rotation;
		destinationLoc = data.destinationLoc;
		finalDestinationLoc = data.finalDestinationLoc;
		currentLocation = data.currentLocation;
		prevTile = data.prevTile;
		prevTerrainTile = data.prevTerrainTile;
		isMoving = data.isMoving;
		moreToMove = data.moreToMove;
		//somethingToSay = data.somethingToSay;
		onQuest = data.onQuest;
		currentQuest = data.currentQuest;
		timeWaited = data.timeWaited;
		purchasedAmount = data.purchasedAmount;
		hasSomethingToSay = data.hasSomethingToSay;

		if (currentQuest < questGoals.Count)
			desiredGift = questGoals[currentQuest];

		if (isMoving)
			GameLoader.Instance.unitMoveOrders[this] = data.moveOrders;
		//else
		//	world.AddUnitPosition(currentLocation, this);

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
