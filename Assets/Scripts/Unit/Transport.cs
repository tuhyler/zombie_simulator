using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Transport : Unit
{
	[SerializeField]
	public GameObject koaMesh, scottMesh, azaiMesh;

	[HideInInspector]
	public int passengerCount;
	
	[HideInInspector]
    public bool canMove, hasKoa, hasScott, hasAzai;

	private void Awake()
	{
		base.AwakeMethods();
		transport = GetComponent<Transport>();
		passengerCount = 0;
	}

	private void Start()
	{
		if (!hasKoa)
			koaMesh.SetActive(false);
		if (!hasScott)
			scottMesh.SetActive(false);
		if (!hasAzai)
			azaiMesh.SetActive(false);
	}

	public void Load(Worker worker)
	{
		passengerCount++;

		if (worker.isPlayer)
		{
			koaMesh.SetActive(true);
			hasKoa = true;
			world.mainPlayer.transportTarget = null;
			minimapIcon.sprite = world.mainPlayer.buildDataSO.mapIcon;
		}
		else if (worker.buildDataSO.unitDisplayName == "Scott")
		{
			scottMesh.SetActive(true);
			hasScott = true;
			world.scott.transportTarget = null;
		}
		else if (worker.buildDataSO.unitDisplayName == "Azai")
		{
			azaiMesh.SetActive(true);
			hasAzai = true;
			world.azai.transportTarget = null;
		}

		if (passengerCount == 3)
			canMove = true;

		if (isSelected)
		{
			world.unitMovement.uiJoinCity.ToggleVisibility(false);

			if (passengerCount == 3)
			{
				world.unitMovement.uiUnload.ToggleVisibility(true);
				world.unitMovement.uiMoveUnit.ToggleVisibility(true);
			}
		}
	}

	public void Unload()
    {
        bool nearbyLand = false;
		Vector3Int landTile = Vector3Int.zero;
		Vector3Int currentLoc = world.RoundToInt(transform.position);

		foreach (Vector3Int tile in world.GetNeighborsFor(currentLoc, MapWorld.State.FOURWAY))
		{
			TerrainData td = world.GetTerrainDataAt(tile);
			if (td.isLand && td.walkable && !td.enemyZone)
			{
				landTile = tile;
				nearbyLand = true;
				break;
			}
		}

		if (nearbyLand)
		{
			Vector3Int terrainTile = world.GetClosestTerrainLoc(landTile);
			Vector3Int currentTerrain = world.GetClosestTerrainLoc(currentLoc);

			//finding closest side tile to transport
			bool firstOne = true;
			Vector3Int closeTile = terrainTile;
			int dist = 0;
			foreach (Vector3Int tile in world.GetNeighborsFor(terrainTile, MapWorld.State.FOURWAY))
			{
				if (firstOne)
				{
					firstOne = false;
					closeTile = tile;
					dist = Mathf.Abs(tile.x - currentTerrain.x) + Mathf.Abs(tile.z - currentTerrain.z); 
					continue;
				}

				int newDist = Mathf.Abs(tile.x - currentTerrain.x) + Mathf.Abs(tile.z - currentTerrain.z);
				if (newDist < dist)
				{
					closeTile = tile;
					dist = newDist;
				}
			}


			world.mainPlayer.UnloadWorkerFromTransport(closeTile);
			Vector3Int currentTerrainLoc = world.GetClosestTerrainLoc(currentLoc);
			int zDiff = Mathf.Abs(currentTerrainLoc.z - terrainTile.z);

			if (zDiff == 0)
			{
				world.scott.UnloadWorkerFromTransport(closeTile + new Vector3Int(0, 0, 1));
				world.azai.UnloadWorkerFromTransport(closeTile + new Vector3Int(0, 0, -1));
			}
			else
			{
				world.scott.UnloadWorkerFromTransport(closeTile + new Vector3Int(1, 0, 0));
				world.azai.UnloadWorkerFromTransport(closeTile + new Vector3Int(-1, 0, 0));
			}

			passengerCount = 0;
			canMove = false;
			hasKoa = false;
			hasScott = false;
			hasAzai = false;
			koaMesh.SetActive(false);
			scottMesh.SetActive(false);
			azaiMesh.SetActive(false);
			minimapIcon.sprite = buildDataSO.mapIcon;

			if (isSelected)
				world.unitMovement.ShowIndividualCityButtonsUI();
		}
		else
		{
			UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "No nearby walkable land");
		}
    }

	public void FinishMovementTransport(Vector3 endPosition)
	{
		Vector3Int currentLoc = world.RoundToInt(endPosition);
		world.AddUnitPosition(currentLoc, this);

		bool nearbyLand = false;
		foreach (Vector3Int tile in world.GetNeighborsFor(currentLoc, MapWorld.State.FOURWAY))
		{
			TerrainData td = world.GetTerrainDataAt(tile);
			if (td.isLand && td.walkable && !td.enemyZone)
			{
				nearbyLand = true;
				break;
			}
		}

		if (nearbyLand && isSelected)
			world.unitMovement.uiUnload.ToggleVisibility(true);
	}

	public TransportData SaveTransportData()
	{
		TransportData data = new();

		data.unitNameAndLevel = buildDataSO.unitNameAndLevel;
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
		data.passengerCount = passengerCount;
		data.canMove = canMove;
		data.hasKoa = hasKoa;
		data.hasScott = hasScott;
		data.hasAzai = hasAzai;

		return data;
	}

	public void LoadTransportData(TransportData data)
	{
		gameObject.name = buildDataSO.unitDisplayName;
		transform.position = data.position;
		transform.rotation = data.rotation;
		destinationLoc = data.destinationLoc;
		finalDestinationLoc = data.finalDestinationLoc;
		currentLocation = data.currentLocation;
		prevTile = data.prevTile;
		prevTerrainTile = data.prevTerrainTile;
		isMoving = data.isMoving;
		moreToMove = data.moreToMove;
		passengerCount = data.passengerCount;
		canMove = data.canMove;
		hasKoa = data.hasKoa;
		if (hasKoa)
			koaMesh.SetActive(true);
		hasScott = data.hasScott;
		if (hasScott)
			scottMesh.SetActive(true);
		hasAzai = data.hasAzai;
		if (hasAzai)
			azaiMesh.SetActive(true);

		if (!isMoving)
			world.AddUnitPosition(currentLocation, this);

		if (isMoving)
		{
			Vector3Int endPosition = world.RoundToInt(finalDestinationLoc);

			if (data.moveOrders.Count == 0)
				data.moveOrders.Add(endPosition);

			GameLoader.Instance.unitMoveOrders[this] = data.moveOrders;
			//MoveThroughPath(data.moveOrders);
		}
	}
}
