using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MilitaryLeader : Military
{
	[HideInInspector]
	public EnemyEmpire empire;
	[HideInInspector]
	public string leaderName;
	public List<UnitBuildDataSO> leaderUnitList;
	public Dictionary<UnitType, UnitBuildDataSO> leaderUnitDict = new();
	private int timeWaited = 0;
	[HideInInspector]
	public bool hasSomethingToSay, defending, dueling;
	public Color borderColor;
	public Vector2 colorOne, colorTwo;

	private void Awake()
	{
		base.AwakeMethods();
		base.MilitaryAwakeMethods();
		leader = this;

		for (int i = 0; i < leaderUnitList.Count; i++)
			leaderUnitDict[leaderUnitList[i].unitType] = leaderUnitList[i];
	}

	public void SetUpNPC(MapWorld world, UnitData data = null)
	{
		leaderName = buildDataSO.unitDisplayName;
		name = buildDataSO.unitDisplayName;
		SetReferences(world, true);
		world.uiSpeechWindow.AddToSpeakingDict(leaderName, this);
		Vector3 actualPosition = transform.position;

		if (data != null && data.leaderData.dueling)
			world.SetNPCLoc(data.leaderData.capitalCity, this);
		else
			world.SetNPCLoc(actualPosition, this);

		currentLocation = world.RoundToInt(actualPosition);

		if (!defending && !dueling)
			outline.ToggleOutline(true);
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

	public void BeginChallengeWait()
	{
		timeWaited = 10;
		StartCoroutine(SetNextChallengeWait());
	}

	public IEnumerator SetNextChallengeWait()
	{
		while (timeWaited > 0)
		{
			yield return attackPauses[0];
			timeWaited--;
		}

		SetNextChallenge();
	}

	public void SetNextChallenge()
	{;
		SetSomethingToSay(leaderName + "_challenge" + 0.ToString());
	}

	public void CancelApproachingConversation()
	{
		if (world.mainPlayer.inEnemyLines)
		{
			if (world.mainPlayer.isMoving)
			{
				//world.mainPlayer.StopAnimation();
				//world.mainPlayer.ShiftMovement();
				world.mainPlayer.StopMovementCheck(true);
			}

			world.mainPlayer.ReturnToFriendlyTile();
		}
		else if (world.RoundToInt(world.mainPlayer.finalDestinationLoc) == currentLocation)
		{
			//world.mainPlayer.StopAnimation();
			//world.mainPlayer.ShiftMovement();
			world.mainPlayer.StopMovementCheck(true);
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

	public void DuelSetup()
	{
		City capitalCity = world.GetEnemyCity(empire.capitalCity);
		capitalCity.StopSpawnAndSendAttackCycle(true);

		Vector3Int barracksLoc = capitalCity.singleBuildDict[SingleBuildType.Barracks];

		Vector3Int targetLoc = barracksLoc;
		int dist = 0;
		int chosenNum = 0;
		List<Vector3Int> fourWayLoc = world.GetNeighborsFor(barracksLoc, MapWorld.State.FOURWAY);
		for (int i = 0; i < fourWayLoc.Count; i++)
		{
			if (i == 0)
			{
				targetLoc = fourWayLoc[i];
				dist = Mathf.Abs(currentLocation.x - barracksLoc.x) + Mathf.Abs(currentLocation.z - barracksLoc.z);
				continue;
			}

			int newDist = Mathf.Abs(currentLocation.x - barracksLoc.x) + Mathf.Abs(currentLocation.z - barracksLoc.z);
			if (newDist < dist)
			{
				targetLoc = fourWayLoc[i];
				dist = newDist;
				chosenNum = i;
			}
		}

		Vector3Int oppositeLoc;
		if (chosenNum < 2)
			oppositeLoc = fourWayLoc[chosenNum + 2];
		else
			oppositeLoc = fourWayLoc[chosenNum - 2];

		dueling = true;
		List<Vector3Int> path = GridSearch.MoveWherever(world, transform.position, targetLoc);

		if (path.Count > 0)
		{
			finalDestinationLoc = path[path.Count - 1];
			MoveThroughPath(path);
		}
		else
		{
			FinishMovementEnemyLeader();
		}

		//temporarily setting up an enemy camp for the duel		
		enemyCamp = new();
		enemyCamp.UnitsInCamp.Add(this);
		enemyCamp.campCount = 1;
		enemyCamp.loc = barracksLoc;
		enemyCamp.world = world;
		enemyCamp.forward = barracksLoc - targetLoc;
		enemyCamp.attackingArmy = world.azai.army;
		enemyCamp.cityLoc = capitalCity.cityLoc;
		enemyCamp.duel = true;

		world.azai.PrepForDuel(oppositeLoc, barracksLoc, capitalCity.cityLoc, enemyCamp);
		if (!capitalCity.enemyCamp.movingOut && !capitalCity.enemyCamp.inBattle && !capitalCity.enemyCamp.prepping && !capitalCity.enemyCamp.attackReady)
			capitalCity.enemyCamp.PrepForDuel();

	}

	public void Charge()
	{
		if (!world.azai.isMoving)
			world.azai.StartWait(this);
	}

	public void LeaderCharge()
	{
		inBattle = true;
		UnitType type = buildDataSO.unitType;

		if (type == UnitType.Infantry)
			enemyAI.InfantryAggroCheck();
		else if (type == UnitType.Ranged)
			enemyAI.RangedAggroCheck();
		else if (type == UnitType.Cavalry)
			enemyAI.CavalryAggroCheck();
	}

	public void FinishDuel()
	{
		inBattle = false;
		dueling = false;
	}

	public void StartGloating()
	{
		StartCoroutine(StandOverAndGloat());
	}

	public IEnumerator StandOverAndGloat()
	{
		City capitalCity = world.GetEnemyCity(empire.capitalCity);
		capitalCity.enemyCamp.ToggleClapping(true);

		yield return new WaitForSeconds(4);

		FinishDuel();
		enemyCamp = capitalCity.enemyCamp;
		world.ToggleDuelBattleCam(false, capitalCity.singleBuildDict[SingleBuildType.Barracks], world.azai, this);
		enemyAI.StartReturn();
		capitalCity.enemyCamp.FinishDuelCheck();
	}

	public void FinishBattle()
	{
		if (defending && enemyCamp.benchedUnit == null)
			enemyCamp.campCount--;

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
			leader.barracksBunk += new Vector3Int(0, 0, -1);
		else if (leaderType == UnitType.Ranged)
			leader.barracksBunk += new Vector3Int(0, 0, 1);
		else if (leaderType == UnitType.Cavalry)
			leader.barracksBunk += new Vector3Int(0, 0, 0);
	}

	public void FinishMovementEnemyLeader()
	{
		defending = false;
		
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
		data.dueling = dueling;
		if (dueling)
		{
			data.forward = enemyCamp.forward;
			data.duelLoc = enemyCamp.loc;
		}

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
		dueling = data.leaderData.dueling;
		timeWaited = data.leaderData.timeWaited;

		if (data.leaderData.somethingToSay)
		{
			conversationHaver.conversationTopics = new(data.leaderData.conversationTopics);
			data.leaderData.conversationTopics.Clear();
			SetSomethingToSay(conversationHaver.conversationTopics[0]);
		}

		if (dueling)
		{
			enemyCamp = new();
			enemyCamp.UnitsInCamp.Add(this);
			enemyCamp.campCount = 1;
			enemyCamp.loc = data.leaderData.duelLoc;
			enemyCamp.world = world;
			enemyCamp.forward = data.leaderData.forward;
			enemyCamp.cityLoc = data.leaderData.capitalCity;
			enemyCamp.duel = true;
			GameLoader.Instance.duelingLeader = this;
		}

		if (timeWaited > 0)
		{

		}

		LoadUnitData(data);
	}
}
