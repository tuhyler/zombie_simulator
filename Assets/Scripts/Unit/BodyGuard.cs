using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyGuard : Military
{
	[HideInInspector]
	public bool toTransport, inTransport, dueling;
	[HideInInspector]
	public Transport transportTarget;
	
	private void Awake()
	{
		base.AwakeMethods();
		base.MilitaryAwakeMethods();
		army = GetComponent<Army>();
		army.SetWorld(world);
		bodyGuard = this;
	}

	public void PrepForDuel(Vector3Int loc, Vector3Int barracksLoc)
	{
		world.mainPlayer.runningAway = true;
		world.azaiFollow = false;
		dueling = true;
		army.forward = barracksLoc - loc;

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

	public void FinishDuel()
	{
		army.attackingSpots.Clear();
		army.movementRange.Clear();
		world.azaiFollow = true;
		world.mainPlayer.runningAway = false;

		world.mainPlayer.ReturnToFriendlyTile();
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

		return data;
	}

	public void LoadBodyGuardData(UnitData data)
	{
		LoadUnitData(data);

		toTransport = data.bodyGuardData.toTransport;
		inTransport = data.bodyGuardData.inTransport;
		dueling = data.bodyGuardData.dueling;

		if (data.bodyGuardData.somethingToSay)
		{
			conversationHaver.conversationTopics = new(data.bodyGuardData.conversationTopics);
			data.bodyGuardData.conversationTopics.Clear();
			conversationHaver.SetSomethingToSay(conversationHaver.conversationTopics[0]);
		}

		if (inTransport)
			gameObject.SetActive(false);
	}
}
