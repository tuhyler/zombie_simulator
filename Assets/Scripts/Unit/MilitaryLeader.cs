using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MilitaryLeader : Military
{
	[HideInInspector]
	public EnemyEmpire empire;
	[HideInInspector]
	public string leaderName;
	public List<GameObject> leaderMilitaryUnits;
	private int timeWaited = 0;
	[HideInInspector]
	public bool hasSomethingToSay, defending;


	private void Awake()
	{
		base.AwakeMethods();
		base.MilitaryAwakeMethods();
		leader = this;
	}

	public void SetUpNPC(MapWorld world)
	{
		leaderName = buildDataSO.unitDisplayName;
		name = buildDataSO.unitDisplayName;
		SetReferences(world, true);
		world.uiSpeechWindow.AddToSpeakingDict(leaderName, this);
		Vector3 actualPosition = transform.position;
		world.SetNPCLoc(actualPosition, this);
		currentLocation = world.RoundToInt(actualPosition);
		//world.allTCReps[npcName] = this;
	}

	public void SetEmpire(EnemyEmpire empire)
	{
		this.empire = empire;
	}

	public void SetSomethingToSay(string conversationTopic)
	{
		conversationHaver.SetSomethingToSay(conversationTopic);
	}

	public void CancelApproachingConversation()
	{
		if (world.mainPlayer.inEnemyLines)
		{
			if (world.mainPlayer.isMoving)
			{
				world.mainPlayer.StopAnimation();
				world.mainPlayer.ShiftMovement();
				world.mainPlayer.StopMovement();
			}

			world.mainPlayer.ReturnToFriendlyTile();
		}
		else if (world.RoundToInt(world.mainPlayer.finalDestinationLoc) == currentLocation)
		{
			world.mainPlayer.StopAnimation();
			world.mainPlayer.ShiftMovement();
			world.mainPlayer.StopMovement();
			world.mainPlayer.RealignFollowers(world.mainPlayer.currentLocation, world.mainPlayer.prevTile, false);
		}
	}

	public void PrepForBattle()
	{
		defending = true;

		if (somethingToSay)
		{
			somethingToSay = false;
			hasSomethingToSay = true;
			questionMark.SetActive(false);
		}
	}

	public void FinishBattle()
	{
		defending = false;
		if (!isDead)
		{
			enemyAI.StartReturn();

			if (!isMoving)
				FinishMovementEnemyLeader();
		}
	}

	public void SetBarracksBunk(Vector3Int loc)
	{
		UnitType leaderType = leader.buildDataSO.unitType;
		leader.barracksBunk = loc;

		if (leaderType == UnitType.Infantry)
		{
			leader.barracksBunk += new Vector3Int(0, 0, -1);
		}
		else if (leaderType == UnitType.Ranged)
		{
			leader.barracksBunk += new Vector3Int(0, 0, 1);
		}
		else if (leaderType == UnitType.Cavalry)
		{
			leader.barracksBunk += new Vector3Int(0, 0, 0);
		}
	}

	public void FinishMovementEnemyLeader()
	{
		if (hasSomethingToSay)
		{
			hasSomethingToSay = false;
			somethingToSay = true;
			questionMark.SetActive(true);
		}

		if (currentHealth < buildDataSO.health)
			healthbar.RegenerateHealth();
	}

	public MilitaryLeaderData SaveMilitaryLeaderData()
	{
		MilitaryLeaderData data = new MilitaryLeaderData();

		data.somethingToSay = somethingToSay;
		data.conversationTopics = conversationHaver.conversationTopics;
		data.timeWaited = timeWaited;
		data.hasSomethingToSay = hasSomethingToSay;
		data.defending = defending;

		//for empire
		data.attackingCity = empire.attackingCity;
		data.capitalCity = empire.capitalCity;
		data.empireCities = new();

		for (int i = 0; i < empire.empireCities.Count; i++)
			data.empireCities.Add(empire.empireCities[i]);

		return data;
	}

	public void LoadMilitaryLeaderData(UnitData data)
	{
		timeWaited = data.leaderData.timeWaited;
		hasSomethingToSay = data.leaderData.hasSomethingToSay;
		defending = data.leaderData.defending;
		timeWaited = data.leaderData.timeWaited;

		if (data.leaderData.somethingToSay)
		{
			conversationHaver.conversationTopics = new(data.leaderData.conversationTopics);
			data.leaderData.conversationTopics.Clear();
			SetSomethingToSay(conversationHaver.conversationTopics[0]);
		}

		if (timeWaited > 0)
		{

		}
	}
}
