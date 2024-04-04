using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyGuard : Military
{
	[HideInInspector]
	public bool toTransport, inTransport, dueling, waiting, dizzy;
	[HideInInspector]
	public Transport transportTarget;
	[SerializeField]
	public GameObject battleIcon;
	private int isDizzyHash;

	private void Awake()
	{
		base.AwakeMethods();
		base.MilitaryAwakeMethods();
		bodyGuard = this;
		isDizzyHash = Animator.StringToHash("isDizzy");
	}

	public void SetArmy()
	{
		army = GetComponent<Army>();
		army.SetWorld(world);
		army.UnitsInArmy.Add(this);
		army.armyCount = 1;
	}

	public void PrepForDuel(Vector3Int loc, Vector3Int barracksLoc, Vector3Int enemyCityLoc, EnemyCamp targetCamp)
	{
		if (isMoving)
		{
			StopAnimation();
			ShiftMovement();
		}

		if (world.scott.isMoving)
		{
			world.scott.StopAnimation();
			world.scott.ShiftMovement();
		}
		//world.mainPlayer.runningAway = true;
		//world.azaiFollow = false;
		dueling = true;
		inArmy = true;
		army.forward = barracksLoc - loc;
		army.enemyTarget = barracksLoc;
		army.enemyCityLoc = enemyCityLoc;
		army.targetCamp = targetCamp;
		world.mainPlayer.Rotate(barracksLoc);
		world.scott.Rotate(barracksLoc);

		List<Vector3Int> battleZone = new() { barracksLoc };
		foreach (Vector3Int tile in world.GetNeighborsFor(barracksLoc, MapWorld.State.EIGHTWAY))
			battleZone.Add(tile);

		army.movementRange = battleZone;

		List<Vector3Int> path = GridSearch.MoveWherever(world, transform.position, loc);

		if (path.Count > 0)
		{
			finalDestinationLoc = path[path.Count - 1];
			MoveThroughPath(path);
		}
	}

	public void Charge()
	{
		MilitaryLeader leader = world.GetEnemyCity(army.enemyCityLoc).empire.enemyLeader;

		//setting battle icon at battle loc
		battleIcon.SetActive(true);
		battleIcon.transform.position = army.enemyTarget;
		Vector3 goScale = battleIcon.transform.localScale;
		battleIcon.transform.localScale = Vector3.zero;
		LeanTween.scale(battleIcon, goScale, 0.5f);

		world.ToggleDuelMaterialClear(true, army.enemyTarget, this, leader);

		if (!leader.isMoving)
			StartCoroutine(WaitASec(leader));
	}

	public void StartWait(MilitaryLeader leader)
	{
		StartCoroutine(WaitASec(leader));
	}

	public IEnumerator WaitASec(MilitaryLeader leader)
	{
		waiting = true;
		yield return new WaitForSeconds(1);

		waiting = false;
		leader.LeaderCharge();
		BodyGuardCharge();
	}

	public void BodyGuardCharge()
	{
		inBattle = true;
		InfantryAggroCheck();
	}

	public void FinishDuel()
	{
		battleIcon.SetActive(false);
		army.attackingSpots.Clear();
		army.movementRange.Clear();
		dueling = false;
		inArmy = false;
		inBattle = false;
		if (currentHealth < buildDataSO.health)
			healthbar.RegenerateHealth();
	}

	public void GoDizzy()
	{
		StartCoroutine(DizzyCoroutine());
	}

	public void ToggleDizzy(bool v)
	{
		dizzy = v;
		unitAnimator.SetBool(isDizzyHash, v);
	}

	private IEnumerator DizzyCoroutine()
	{
		Vector3 loc = Vector3.zero;
		loc.y += 1.25f;
		Quaternion rotation = Quaternion.Euler(90, 0, 90);
		GameObject dizzy = Instantiate(world.dizzyMarker, loc, rotation);
		dizzy.transform.SetParent(transform, false);

		ToggleDizzy(true);

		yield return new WaitForSeconds(4);

		FinishDuel();
		Destroy(dizzy);
		ToggleDizzy(false);
		world.mainPlayer.ReturnToFriendlyTile();
		world.ToggleBadGuyTalk(false, army.targetCamp.cityLoc);
		army.targetCamp = null;
	}

	public void GetBehindScott(Vector3Int scottSpot)
	{
		Vector3Int currentLoc = world.RoundToInt(transform.position);
		int dist = 0;
		Vector3Int finalLoc = scottSpot;
		bool firstOne = true;
		foreach (Vector3Int tile in world.GetNeighborsFor(scottSpot, MapWorld.State.EIGHTWAY))
		{
			if (firstOne)
			{
				firstOne = false;
				finalLoc = tile;
				dist = Mathf.Abs(tile.x - currentLoc.x) + Mathf.Abs(tile.z - currentLoc.z);
				continue;
			}

			int newDist = Mathf.Abs(tile.x - currentLoc.x) + Mathf.Abs(tile.z - currentLoc.z);
			if (newDist < dist)
			{
				dist = newDist;
				finalLoc = tile;
			}
		}

		List<Vector3Int> azaiPath = GridSearch.AStarSearch(world, currentLoc, finalLoc, false, false);

		if (azaiPath.Count > 0)
		{
			finalDestinationLoc = finalLoc;
			MoveThroughPath(azaiPath);
		}
	}

	public void FollowScott(List<Vector3Int> scottPath, Vector3 currentLoc)
	{
		List<Vector3Int> azaiPath = GridSearch.AStarSearch(world, transform.position, world.RoundToInt(currentLoc), false, false);
		scottPath.RemoveAt(scottPath.Count - 1);
		azaiPath.AddRange(scottPath);

		if (azaiPath.Count > 0)
		{
			finalDestinationLoc = azaiPath[azaiPath.Count - 1];
			MoveThroughPath(azaiPath);
		}
	}

	public void RepositionBodyGuard(Vector3Int newPos, bool loading, bool enemy, List<Vector3Int> exemptList = null)
	{
		StopAnimation();
		ShiftMovement();

		List<Vector3Int> path;

		if (enemy)
		{
			path = GridSearch.AStarSearchExempt(world, transform.position, newPos, exemptList);
		}
		else
		{
			path = GridSearch.AStarSearch(world, transform.position, newPos, false, false);
		}

		if (path.Count > 0)
		{
			finalDestinationLoc = newPos;
			MoveThroughPath(path);
		}
		else if (loading)
		{
			LoadBodyGuardInTransport(transportTarget);
		}
	}

	public void LoadBodyGuardInTransport(Transport transport)
	{
		toTransport = false;
		Vector3Int tile = world.RoundToInt(transform.position);

		if (transport.isUpgrading)
		{
			InfoPopUpHandler.WarningMessage().Create(tile, "Can't load while upgrading");
			return;
		}

		transport.Load(this);
		gameObject.SetActive(false);
		inTransport = true;
	}

	public void UnloadBodyGuardFromTransport(Vector3Int tile)
	{
		transform.position = tile;
		gameObject.SetActive(true);
		inTransport = false;
	}

	public BodyGuardData SaveBodyGuardData()
	{
		BodyGuardData data = new();

		data.somethingToSay = somethingToSay;
		data.conversationTopics = new(conversationHaver.conversationTopics);
		data.toTransport = toTransport;
		data.inTransport = inTransport;
		data.dueling = dueling;
		data.waiting = waiting;
		data.dizzy = dizzy;

		if (dueling)
			data.forward = army.forward;

		return data;
	}

	public void LoadBodyGuardData(UnitData data)
	{
		toTransport = data.bodyGuardData.toTransport;
		inTransport = data.bodyGuardData.inTransport;
		dueling = data.bodyGuardData.dueling;
		waiting = data.bodyGuardData.waiting;
		dizzy = data.bodyGuardData.dizzy;

		if (data.bodyGuardData.somethingToSay)
		{
			conversationHaver.conversationTopics = new(data.bodyGuardData.conversationTopics);
			data.bodyGuardData.conversationTopics.Clear();
			conversationHaver.SetSomethingToSay(conversationHaver.conversationTopics[0]);
		}

		if (dueling)
		{
			MilitaryLeader leader = GameLoader.Instance.duelingLeader;
			inArmy = true;
			army.forward = data.bodyGuardData.forward;
			army.enemyTarget = leader.enemyCamp.loc;
			army.enemyCityLoc = leader.enemyCamp.cityLoc;
			army.targetCamp = leader.enemyCamp;
			leader.enemyCamp.attackingArmy = world.azai.army;
			
			if (waiting)
			{
				StartCoroutine(WaitASec(leader));
			}
			else if (dizzy)
			{
				leader.StartGloating();
				StartCoroutine(DizzyCoroutine());
			}
		}


		LoadUnitData(data);

		if (inTransport)
			gameObject.SetActive(false);
	}
}
