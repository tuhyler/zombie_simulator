using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using UnityEditor.VersionControl;
using UnityEngine;

public class GridSearch
{
	public static List<Vector3Int> PlayerMove(MapWorld world, Vector3 startLocation, Vector3Int endPosition, bool bySea)
	{
		if (bySea)
			return PlayerMoveSea(world, startLocation, endPosition);

		List<Vector3Int> path = new();

		Vector3Int startPosition = world.RoundToInt(startLocation);

		//below is for units staying on road, don't skip across
		List<Vector3Int> xRoads = new() { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0) };
		List<Vector3Int> zRoads = new() { new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };
		List<Vector3Int> xzRoads = new() { new Vector3Int(1, 0, 1), new Vector3Int(-1, 0, 1), new Vector3Int(1, 0, -1), new Vector3Int(-1, 0, -1) };
		//above is for units staying on road

		List<Vector3Int> positionsToCheck = new();
		Dictionary<Vector3Int, int> costDictionary = new();
		Dictionary<Vector3Int, int> priorityDictionary = new();
		Dictionary<Vector3Int, Vector3Int?> parentsDictionary = new();

		positionsToCheck.Add(startPosition);
		priorityDictionary.Add(startPosition, 0);
		costDictionary.Add(startPosition, 0);
		parentsDictionary.Add(startPosition, null);

		while (positionsToCheck.Count > 0)
		{
			Vector3Int current = GetClosestVertex(positionsToCheck, priorityDictionary);

			positionsToCheck.Remove(current);
			if (current == endPosition)
			{
				path = GeneratePath(parentsDictionary, current);
				return path;
			}

			//below is for units to stay on road, don't skip across
			bool centerRoad = true;
			bool xRoad = false;
			bool zRoad = false;
			bool xzRoad = false;

			if (world.IsRoadOnTileLocation(current))
			{
				int x = current.x % 3;
				int z = current.z % 3;

				xRoad = x != 0;
				zRoad = z != 0;
				xzRoad = (xRoad && zRoad);
				centerRoad = (!xRoad && !zRoad);
			}
			//above is for units to stay on road

			foreach (Vector3Int tile in world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAY))
			{
				Vector3Int neighbor = tile + current;

				if (!world.PlayerCheckIfPositionIsValid(neighbor))
					continue;

				if (world.CheckIfEnemyTerritory(neighbor))
					continue;

				bool hasRoad;// = world.IsRoadOnTileLocation(neighbor);
				int tempCost;

				//below is for units staying on roads
				if (!centerRoad)
				{
					if (xzRoad && xzRoads.Contains(tile))
						hasRoad = world.IsRoadOnTileLocation(neighbor);
					else if (xRoad && xRoads.Contains(tile))
						hasRoad = world.IsRoadOnTileLocation(neighbor);
					else if (zRoad && zRoads.Contains(tile))
						hasRoad = world.IsRoadOnTileLocation(neighbor);
					else
						hasRoad = false;
				}
				else
				{
					hasRoad = world.IsRoadOnTileLocation(neighbor);
				}
				//above is for units staying on roads

				if (hasRoad)
					tempCost = world.GetRoadCost();
				else
					tempCost = world.GetMovementCost(neighbor);

				if (tile.sqrMagnitude == 2)
				{
					Vector3Int temp = neighbor - current;

					if (!hasRoad && (!world.PlayerCheckIfPositionIsValid(current + new Vector3Int(temp.x, 0, 0)) || !world.PlayerCheckIfPositionIsValid(current + new Vector3Int(0, 0, temp.z))))
						continue;

					tempCost = Mathf.RoundToInt(tempCost * 1.4f); //multiply by square root 2 for the diagonal squares
				}

				int newCost = costDictionary[current] + tempCost;
				if (!costDictionary.ContainsKey(neighbor) || newCost < costDictionary[neighbor])
				{
					costDictionary[neighbor] = newCost;

					int priority = newCost + ManhattanDistance(endPosition, neighbor); //only check the neighbors closest to destination
					positionsToCheck.Add(neighbor);
					priorityDictionary[neighbor] = priority;

					parentsDictionary[neighbor] = current;
				}
			}
		}

		InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(endPosition, "Cannot reach selected area");
		return path;
	}

	public static List<Vector3Int> PlayerMoveSea(MapWorld world, Vector3 startLocation, Vector3Int endPosition)
	{
		Vector3Int startPosition = world.RoundToInt(startLocation);

		List<Vector3Int> path = new();

		List<Vector3Int> positionsToCheck = new();
		Dictionary<Vector3Int, int> costDictionary = new();
		Dictionary<Vector3Int, int> priorityDictionary = new();
		Dictionary<Vector3Int, Vector3Int?> parentsDictionary = new();

		positionsToCheck.Add(startPosition);
		priorityDictionary.Add(startPosition, 0);
		costDictionary.Add(startPosition, 0);
		parentsDictionary.Add(startPosition, null);

		while (positionsToCheck.Count > 0)
		{
			Vector3Int current = GetClosestVertex(positionsToCheck, priorityDictionary);

			positionsToCheck.Remove(current);
			if (current.Equals(endPosition))
			{
				path = GeneratePath(parentsDictionary, current);
				return path;
			}

			foreach (Vector3Int tile in world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAY))
			{
				Vector3Int neighbor = tile + current;
				int sqrMagnitude = tile.sqrMagnitude;

				if (!world.PlayerCheckIfSeaPositionIsValid(neighbor))
					continue;

				if (world.CheckIfCoastCoast(neighbor) && neighbor != endPosition)
					continue;

				if (world.CheckIfEnemyNotNeutral(neighbor))
					continue;

				int tempCost = world.GetMovementCost(neighbor);

				if (sqrMagnitude == 2)
					tempCost = Mathf.RoundToInt(tempCost * 1.4f); //multiply by square root 2 for the diagonal squares

				int newCost = costDictionary[current] + tempCost;
				if (!costDictionary.ContainsKey(neighbor) || newCost < costDictionary[neighbor])
				{
					costDictionary[neighbor] = newCost;

					int priority = newCost + ManhattanDistance(endPosition, neighbor); //only check the neighbors closest to destination
					positionsToCheck.Add(neighbor);
					priorityDictionary[neighbor] = priority;

					parentsDictionary[neighbor] = current;
				}
			}
		}

		InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(endPosition, "Cannot reach selected area");
		return path;
	}

	public static List<Vector3Int> MilitaryMove(MapWorld world, Vector3 startLocation, Vector3Int endPosition, bool bySea)
    {
        if (bySea)
            return MilitaryMoveSea(world, startLocation, endPosition);

        List <Vector3Int> path = new();
        
        Vector3Int startPosition = world.RoundToInt(startLocation);

        //below is for units staying on road, don't skip across
        List<Vector3Int> xRoads = new() { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0) };
        List<Vector3Int> zRoads = new() { new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };
        List<Vector3Int> xzRoads = new() { new Vector3Int(1, 0, 1), new Vector3Int(-1, 0, 1), new Vector3Int(1, 0, -1), new Vector3Int(-1, 0, -1) };
        //above is for units staying on road

        List<Vector3Int> positionsToCheck = new();
        Dictionary<Vector3Int, int> costDictionary = new();
        Dictionary<Vector3Int, int> priorityDictionary = new();
        Dictionary<Vector3Int, Vector3Int?> parentsDictionary = new();

        positionsToCheck.Add(startPosition);
        priorityDictionary.Add(startPosition, 0);
        costDictionary.Add(startPosition, 0);
        parentsDictionary.Add(startPosition, null);

        while (positionsToCheck.Count > 0)
        {
            Vector3Int current = GetClosestVertex(positionsToCheck, priorityDictionary);

            positionsToCheck.Remove(current);
            if (current == endPosition)
            {
                path = GeneratePath(parentsDictionary, current);
                return path;
            }

            //below is for units to stay on road, don't skip across
            bool centerRoad = true;
            bool xRoad = false;
            bool zRoad = false;
            bool xzRoad = false;

            if (world.IsRoadOnTileLocation(current))
            {
                int x = current.x % 3;
                int z = current.z % 3;

                xRoad = x != 0;
                zRoad = z != 0;
                xzRoad = (xRoad && zRoad);
                centerRoad = (!xRoad && !zRoad);
            }
            //above is for units to stay on road

            foreach (Vector3Int tile in world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAY))
            {
                Vector3Int neighbor = tile + current;
                
                if (!world.CheckIfPositionIsValid(neighbor))
                    continue;

                if (world.CheckIfEnemyTerritory(neighbor))
                    continue;

                bool hasRoad;// = world.IsRoadOnTileLocation(neighbor);
                int tempCost;

                //below is for units staying on roads
                if (!centerRoad)
                {
                    if (xzRoad && xzRoads.Contains(tile))
                        hasRoad = world.IsRoadOnTileLocation(neighbor);
                    else if (xRoad && xRoads.Contains(tile))
                        hasRoad = world.IsRoadOnTileLocation(neighbor);
                    else if (zRoad && zRoads.Contains(tile))
                        hasRoad = world.IsRoadOnTileLocation(neighbor);
                    else
                        hasRoad = false;
                }
                else
                {
                    hasRoad = world.IsRoadOnTileLocation(neighbor);
                }
                //above is for units staying on roads

                if (hasRoad)
                    tempCost = world.GetRoadCost();
                else
                    tempCost = world.GetMovementCost(neighbor);

                if (tile.sqrMagnitude == 2)
                {
                    Vector3Int temp = neighbor - current;

                    if (!hasRoad && (!world.CheckIfPositionIsValid(current + new Vector3Int(temp.x, 0, 0)) || !world.CheckIfPositionIsValid(current + new Vector3Int(0, 0, temp.z))))
                        continue;

                    tempCost = Mathf.RoundToInt(tempCost * 1.4f); //multiply by square root 2 for the diagonal squares
                }

                int newCost = costDictionary[current] + tempCost;
                if (!costDictionary.ContainsKey(neighbor) || newCost < costDictionary[neighbor])
                {
                    costDictionary[neighbor] = newCost;

                    int priority = newCost + ManhattanDistance(endPosition, neighbor); //only check the neighbors closest to destination
                    positionsToCheck.Add(neighbor);
                    priorityDictionary[neighbor] = priority;

                    parentsDictionary[neighbor] = current;
                }
            }
        }

        return path;
    }

	public static List<Vector3Int> PlayerMoveExempt(MapWorld world, Vector3 startLocation, Vector3Int endPosition, HashSet<Vector3Int> exemptList, bool isPlayer = false)
	{
		List<Vector3Int> path = new();

		Vector3Int startPosition = world.RoundToInt(startLocation);

		//below is for units staying on road, don't skip across
		List<Vector3Int> xRoads = new() { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0) };
		List<Vector3Int> zRoads = new() { new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };
		List<Vector3Int> xzRoads = new() { new Vector3Int(1, 0, 1), new Vector3Int(-1, 0, 1), new Vector3Int(1, 0, -1), new Vector3Int(-1, 0, -1) };
		//above is for units staying on road

		List<Vector3Int> positionsToCheck = new();
		Dictionary<Vector3Int, int> costDictionary = new();
		Dictionary<Vector3Int, int> priorityDictionary = new();
		Dictionary<Vector3Int, Vector3Int?> parentsDictionary = new();

		positionsToCheck.Add(startPosition);
		priorityDictionary.Add(startPosition, 0);
		costDictionary.Add(startPosition, 0);
		parentsDictionary.Add(startPosition, null);

		while (positionsToCheck.Count > 0)
		{
			Vector3Int current = GetClosestVertex(positionsToCheck, priorityDictionary);

			positionsToCheck.Remove(current);
			if (current == endPosition)
			{
				path = GeneratePath(parentsDictionary, current);
				return path;
			}

			//below is for units to stay on road, don't skip across
			bool centerRoad = true;
			bool xRoad = false;
			bool zRoad = false;
			bool xzRoad = false;

			if (world.IsRoadOnTileLocation(current))
			{
				int x = current.x % 3;
				int z = current.z % 3;

				xRoad = x != 0;
				zRoad = z != 0;
				xzRoad = (xRoad && zRoad);
				centerRoad = (!xRoad && !zRoad);
			}
			//above is for units to stay on road

			foreach (Vector3Int tile in world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAY))
			{
				Vector3Int neighbor = tile + current;

				if (!world.PlayerCheckIfPositionIsValid(neighbor))
					continue;

				if (world.CheckIfEnemyTerritory(neighbor) && !exemptList.Contains(world.GetClosestTerrainLoc(neighbor)))
					continue;

				bool hasRoad;
				int tempCost;

				//below is for units staying on roads
				if (!centerRoad)
				{
					if (xzRoad && xzRoads.Contains(tile))
						hasRoad = world.IsRoadOnTileLocation(neighbor);
					else if (xRoad && xRoads.Contains(tile))
						hasRoad = world.IsRoadOnTileLocation(neighbor);
					else if (zRoad && zRoads.Contains(tile))
						hasRoad = world.IsRoadOnTileLocation(neighbor);
					else
						hasRoad = false;
				}
				else
				{
					hasRoad = world.IsRoadOnTileLocation(neighbor);
				}
				//above is for units staying on roads

				if (hasRoad)
					tempCost = world.GetRoadCost();
				else
					tempCost = world.GetMovementCost(neighbor);

				if (tile.sqrMagnitude == 2)
				{
					Vector3Int temp = neighbor - current;

					if (!hasRoad && (!world.PlayerCheckIfPositionIsValid(current + new Vector3Int(temp.x, 0, 0)) || !world.PlayerCheckIfPositionIsValid(current + new Vector3Int(0, 0, temp.z))))
						continue;

					tempCost = Mathf.RoundToInt(tempCost * 1.4f); //multiply by square root 2 for the diagonal squares
				}

				int newCost = costDictionary[current] + tempCost;
				if (!costDictionary.ContainsKey(neighbor) || newCost < costDictionary[neighbor])
				{
					costDictionary[neighbor] = newCost;

					int priority = newCost + ManhattanDistance(endPosition, neighbor); //only check the neighbors closest to destination
					positionsToCheck.Add(neighbor);
					priorityDictionary[neighbor] = priority;

					parentsDictionary[neighbor] = current;
				}
			}
		}

		if (isPlayer)
			InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(endPosition, "Cannot reach selected area");
		return path;
	}

	public static List<Vector3Int> TraderMove(MapWorld world, Vector3 startLocation, Vector3Int endPosition, bool bySea)
	{
		if (bySea)
			return TraderMoveSea(world, startLocation, endPosition);

		List<Vector3Int> path = new();

		Vector3Int startPosition = world.RoundToInt(startLocation);

		//below is for units staying on road, don't skip across
		List<Vector3Int> xRoads = new() { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0) };
		List<Vector3Int> zRoads = new() { new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };
		List<Vector3Int> xzRoads = new() { new Vector3Int(1, 0, 1), new Vector3Int(-1, 0, 1), new Vector3Int(1, 0, -1), new Vector3Int(-1, 0, -1) };
		//above is for units staying on road

		List<Vector3Int> positionsToCheck = new();
		Dictionary<Vector3Int, int> costDictionary = new();
		Dictionary<Vector3Int, int> priorityDictionary = new();
		Dictionary<Vector3Int, Vector3Int?> parentsDictionary = new();

		positionsToCheck.Add(startPosition);
		priorityDictionary.Add(startPosition, 0);
		costDictionary.Add(startPosition, 0);
		parentsDictionary.Add(startPosition, null);

		while (positionsToCheck.Count > 0)
		{
			Vector3Int current = GetClosestVertex(positionsToCheck, priorityDictionary);

			positionsToCheck.Remove(current);
			if (current == endPosition)
			{
				path = GeneratePath(parentsDictionary, current);
				return path;
			}

			//below is for units to stay on road, don't skip across
			bool centerRoad = true;
			bool xRoad = false;
			bool zRoad = false;
			bool xzRoad = false;

			if (world.IsRoadOnTileLocation(current))
			{
				int x = current.x % 3;
				int z = current.z % 3;

				xRoad = x != 0;
				zRoad = z != 0;
				xzRoad = (xRoad && zRoad);
				centerRoad = (!xRoad && !zRoad);
			}
			//above is for units to stay on road

			foreach (Vector3Int tile in world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAY))
			{
				Vector3Int neighbor = tile + current;

				if (!world.CheckIfPositionIsValid(neighbor))
					continue;

				if (world.CheckIfEnemyTerritory(neighbor))
					continue;

				bool hasRoad;
				int tempCost;

				//below is for units staying on roads
				if (!centerRoad)
				{
					if (xzRoad && xzRoads.Contains(tile))
						hasRoad = world.IsRoadOnTileLocation(neighbor);
					else if (xRoad && xRoads.Contains(tile))
						hasRoad = world.IsRoadOnTileLocation(neighbor);
					else if (zRoad && zRoads.Contains(tile))
						hasRoad = world.IsRoadOnTileLocation(neighbor);
					else
						hasRoad = false;
				}
				else
				{
					hasRoad = world.IsRoadOnTileLocation(neighbor);
				}
				//above is for units staying on roads

				if (!hasRoad) //If it's a trader and not on road, ignore
					continue;

				if (hasRoad)
					tempCost = world.GetRoadCost();
				else
					tempCost = world.GetMovementCost(neighbor);

				if (tile.sqrMagnitude == 2)
				{
					Vector3Int temp = neighbor - current;

					if (!hasRoad && (!world.CheckIfPositionIsValid(current + new Vector3Int(temp.x, 0, 0)) || !world.CheckIfPositionIsValid(current + new Vector3Int(0, 0, temp.z))))
						continue;

					tempCost = Mathf.RoundToInt(tempCost * 1.4f); //multiply by square root 2 for the diagonal squares
				}

				int newCost = costDictionary[current] + tempCost;
				if (!costDictionary.ContainsKey(neighbor) || newCost < costDictionary[neighbor])
				{
					costDictionary[neighbor] = newCost;

					int priority = newCost + ManhattanDistance(endPosition, neighbor); //only check the neighbors closest to destination
					positionsToCheck.Add(neighbor);
					priorityDictionary[neighbor] = priority;

					parentsDictionary[neighbor] = current;
				}
			}
		}

		return path;
	}

	public static List<Vector3Int> MilitaryMoveSea(MapWorld world, Vector3 startLocation, Vector3Int endPosition)
    {
		Vector3Int startPosition = world.RoundToInt(startLocation);

        List<Vector3Int> path = new();

        List<Vector3Int> positionsToCheck = new();
        Dictionary<Vector3Int, int> costDictionary = new();
        Dictionary<Vector3Int, int> priorityDictionary = new();
        Dictionary<Vector3Int, Vector3Int?> parentsDictionary = new();

        positionsToCheck.Add(startPosition);
        priorityDictionary.Add(startPosition, 0);
        costDictionary.Add(startPosition, 0);
        parentsDictionary.Add(startPosition, null);

        while (positionsToCheck.Count > 0)
        {
            Vector3Int current = GetClosestVertex(positionsToCheck, priorityDictionary);

            positionsToCheck.Remove(current);
            if (current.Equals(endPosition))
            {
                path = GeneratePath(parentsDictionary, current);
                return path;
            }

            //below is for units not cutting corners
            //bool currentIsRiverOrCoast = false;

            //if (current.x % 3 != 0 && current.z % 3 != 0)
            //    currentIsRiverOrCoast = world.GetTerrainDataAt(current).IsCoast;
            //above is for units not cutting corners

            foreach (Vector3Int tile in world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAY))
            {
                Vector3Int neighbor = tile + current;
                int sqrMagnitude = tile.sqrMagnitude;

                //below is for units not cutting corners
                //bool neighborIsRiverOrCoast = false;

                //if (sqrMagnitude == 2)
                //    neighborIsRiverOrCoast = world.GetTerrainDataAt(current).IsCoast; 

                //if (currentIsRiverOrCoast && neighborIsRiverOrCoast)
                //    continue;

                if (!world.CheckIfSeaPositionIsValid(neighbor))
                    continue;
                //above is for units not cutting corners

                if (world.CheckIfCoastCoast(neighbor) && neighbor != endPosition)
                    continue;

				if (world.CheckIfEnemyNotNeutral(neighbor))
					continue;

				int tempCost = world.GetMovementCost(neighbor);

                if (sqrMagnitude == 2)
                    tempCost = Mathf.RoundToInt(tempCost * 1.4f); //multiply by square root 2 for the diagonal squares

                //if (world.CheckIfCoastCoast(neighbor))
                //    tempCost *= 2;

                int newCost = costDictionary[current] + tempCost;
                if (!costDictionary.ContainsKey(neighbor) || newCost < costDictionary[neighbor])
                {
                    costDictionary[neighbor] = newCost;

                    int priority = newCost + ManhattanDistance(endPosition, neighbor); //only check the neighbors closest to destination
                    positionsToCheck.Add(neighbor);
                    priorityDictionary[neighbor] = priority;

                    parentsDictionary[neighbor] = current;
                }
            }
        }

        return path;
    }

	public static List<Vector3Int> TraderMoveSea(MapWorld world, Vector3 startLocation, Vector3Int endPosition)
	{
		Vector3Int startPosition = world.RoundToInt(startLocation);

		List<Vector3Int> path = new();

		List<Vector3Int> positionsToCheck = new();
		Dictionary<Vector3Int, int> costDictionary = new();
		Dictionary<Vector3Int, int> priorityDictionary = new();
		Dictionary<Vector3Int, Vector3Int?> parentsDictionary = new();

		positionsToCheck.Add(startPosition);
		priorityDictionary.Add(startPosition, 0);
		costDictionary.Add(startPosition, 0);
		parentsDictionary.Add(startPosition, null);

		while (positionsToCheck.Count > 0)
		{
			Vector3Int current = GetClosestVertex(positionsToCheck, priorityDictionary);

			positionsToCheck.Remove(current);
			if (current.Equals(endPosition))
			{
				path = GeneratePath(parentsDictionary, current);
				return path;
			}

			foreach (Vector3Int tile in world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAY))
			{
				Vector3Int neighbor = tile + current;
				int sqrMagnitude = tile.sqrMagnitude;

				if (!world.CheckIfSeaPositionIsValid(neighbor))
					continue;

				if (world.CheckIfCoastCoast(neighbor) && neighbor != endPosition)
					continue;

				if (world.CheckIfEnemyTerritory(neighbor))
					continue;

				int tempCost = world.GetMovementCost(neighbor);

				if (sqrMagnitude == 2)
					tempCost = Mathf.RoundToInt(tempCost * 1.4f); //multiply by square root 2 for the diagonal squares

				int newCost = costDictionary[current] + tempCost;
				if (!costDictionary.ContainsKey(neighbor) || newCost < costDictionary[neighbor])
				{
					costDictionary[neighbor] = newCost;

					int priority = newCost + ManhattanDistance(endPosition, neighbor); //only check the neighbors closest to destination
					positionsToCheck.Add(neighbor);
					priorityDictionary[neighbor] = priority;

					parentsDictionary[neighbor] = current;
				}
			}
		}

		return path;
	}

	//for enemies
	public static List<Vector3Int> EnemyMove(MapWorld world, Vector3 startLocation, Vector3Int endPosition, bool bySea, List<Vector3Int> avoidList = null)
	{
		if (bySea)
			return EnemyMoveSea(world, startLocation, endPosition);

        List<Vector3Int> pathAvoidList = new();

        if (avoidList != null)
            pathAvoidList = avoidList;

        List<Vector3Int> path = new();

		Vector3Int startPosition = world.RoundToInt(startLocation);

		List<Vector3Int> positionsToCheck = new();
		Dictionary<Vector3Int, int> costDictionary = new();
		Dictionary<Vector3Int, int> priorityDictionary = new();
		Dictionary<Vector3Int, Vector3Int?> parentsDictionary = new();

		positionsToCheck.Add(startPosition);
		priorityDictionary.Add(startPosition, 0);
		costDictionary.Add(startPosition, 0);
		parentsDictionary.Add(startPosition, null);

		while (positionsToCheck.Count > 0)
		{
			Vector3Int current = GetClosestVertex(positionsToCheck, priorityDictionary);

			positionsToCheck.Remove(current);
			if (current == endPosition)
			{
				path = GeneratePath(parentsDictionary, current);
				return path;
			}

			foreach (Vector3Int tile in world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAY))
			{
				Vector3Int neighbor = tile + current;

				if (!world.CheckIfPositionIsValidForEnemy(neighbor)) //If it's an obstacle, ignore
					continue;

                if (pathAvoidList.Contains(neighbor))
                    continue;

				int tempCost = world.GetMovementCost(neighbor);

				if (tile.sqrMagnitude == 2)
				{
					Vector3Int temp = neighbor - current;

					if ((!world.CheckIfPositionIsValidForEnemy(current + new Vector3Int(temp.x, 0, 0)) || !world.CheckIfPositionIsValidForEnemy(current + new Vector3Int(0, 0, temp.z))))
						continue;

					tempCost = Mathf.RoundToInt(tempCost * 1.4f); //multiply by square root 2 for the diagonal squares
				}

				int newCost = costDictionary[current] + tempCost;
				if (!costDictionary.ContainsKey(neighbor) || newCost < costDictionary[neighbor])
				{
					costDictionary[neighbor] = newCost;

					int priority = newCost + ManhattanDistance(endPosition, neighbor); //only check the neighbors closest to destination
					positionsToCheck.Add(neighbor);
					priorityDictionary[neighbor] = priority;

					parentsDictionary[neighbor] = current;
				}
			}
		}

		//InfoPopUpHandler.WarningMessage().Create(endPosition, "Cannot reach selected area");
		return path;
	}

	public static List<Vector3Int> EnemyMoveSea(MapWorld world, Vector3 startLocation, Vector3Int endPosition)
	{
		Vector3Int startPosition = world.RoundToInt(startLocation);

		List<Vector3Int> path = new();

		List<Vector3Int> positionsToCheck = new();
		Dictionary<Vector3Int, int> costDictionary = new();
		Dictionary<Vector3Int, int> priorityDictionary = new();
		Dictionary<Vector3Int, Vector3Int?> parentsDictionary = new();

		positionsToCheck.Add(startPosition);
		priorityDictionary.Add(startPosition, 0);
		costDictionary.Add(startPosition, 0);
		parentsDictionary.Add(startPosition, null);

		while (positionsToCheck.Count > 0)
		{
			Vector3Int current = GetClosestVertex(positionsToCheck, priorityDictionary);

			positionsToCheck.Remove(current);
			if (current.Equals(endPosition))
			{
				path = GeneratePath(parentsDictionary, current);
				return path;
			}

			foreach (Vector3Int tile in world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAY))
			{
				Vector3Int neighbor = tile + current;
				int sqrMagnitude = tile.sqrMagnitude;

				//below is for units not cutting corners
				if (!world.CheckIfSeaPositionIsValidForEnemy(neighbor))
					continue;
				//above is for units not cutting corners

				if (world.CheckIfCoastCoast(neighbor) && neighbor != endPosition)
					continue;

				int tempCost = world.GetMovementCost(neighbor);

				if (sqrMagnitude == 2)
					tempCost = Mathf.RoundToInt(tempCost * 1.4f); //multiply by square root 2 for the diagonal squares

				int newCost = costDictionary[current] + tempCost;
				if (!costDictionary.ContainsKey(neighbor) || newCost < costDictionary[neighbor])
				{
					costDictionary[neighbor] = newCost;

					int priority = newCost + ManhattanDistance(endPosition, neighbor); //only check the neighbors closest to destination
					positionsToCheck.Add(neighbor);
					priorityDictionary[neighbor] = priority;

					parentsDictionary[neighbor] = current;
				}
			}
		}

		//InfoPopUpHandler.WarningMessage().Create(startLocation, "Cannot reach selected area");
		return path;
	}

	//for moving entire army
	public static List<Vector3Int> TerrainSearch(MapWorld world, Vector3Int startTerrain, Vector3Int endTerrain, bool finalSea = false)
    {
		List<Vector3Int> path = new();

		List<Vector3Int> positionsToCheck = new();
		Dictionary<Vector3Int, int> costDictionary = new();
		Dictionary<Vector3Int, int> priorityDictionary = new();
		Dictionary<Vector3Int, Vector3Int?> parentsDictionary = new();

		positionsToCheck.Add(startTerrain);
		priorityDictionary.Add(startTerrain, 0);
		costDictionary.Add(startTerrain, 0);
		parentsDictionary.Add(startTerrain, null);

		while (positionsToCheck.Count > 0)
		{
			Vector3Int current = GetClosestVertex(positionsToCheck, priorityDictionary);

			positionsToCheck.Remove(current);
			if (current == endTerrain)
			{
				path = GeneratePath(parentsDictionary, current);
				return path;
			}

			foreach (Vector3Int tile in world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAYINCREMENT))
			{
				Vector3Int neighbor = tile + current;

				if (finalSea)
				{
					if (neighbor != endTerrain)
					{
						if (!world.PlayerCheckIfPositionIsValid(neighbor))
							continue;
					}
				}
				else
				{
					if (!world.PlayerCheckIfPositionIsValid(neighbor))
						continue;
				}

				if (world.CheckIfEnemyTerritory(neighbor) && neighbor != endTerrain)
                    continue;

				if (world.IsTradeCenterOnTile(neighbor))
					continue;

				int tempCost = world.GetMovementCost(neighbor);

				if (tile.sqrMagnitude == 18)
				{
					Vector3Int temp = neighbor - current;

					if (!world.CheckIfPositionIsArmyValid(current + new Vector3Int(temp.x, 0, 0)) || !world.CheckIfPositionIsArmyValid(current + new Vector3Int(0, 0, temp.z)))
						continue;

					tempCost = Mathf.RoundToInt(tempCost * 1.4f); //multiply by square root 2 for the diagonal squares
				}

				int newCost = costDictionary[current] + tempCost;
				if (!costDictionary.ContainsKey(neighbor) || newCost < costDictionary[neighbor])
				{
					costDictionary[neighbor] = newCost;

					int priority = newCost + ManhattanDistance(endTerrain, neighbor); //only check the neighbors closest to destination
					positionsToCheck.Add(neighbor);
					priorityDictionary[neighbor] = priority;

					parentsDictionary[neighbor] = current;
				}
			}
		}

		return path;
	}

	//for finding best terrain to attack from for army
	public static List<Vector3Int> TerrainSearchEnemyCoda(MapWorld world, Vector3Int startTerrain, Vector3Int endTerrain, List<Vector3Int> avoidList, bool bySea)
	{
		List<Vector3Int> path = new();

		List<Vector3Int> positionsToCheck = new();
		Dictionary<Vector3Int, int> costDictionary = new();
		Dictionary<Vector3Int, int> priorityDictionary = new();
		Dictionary<Vector3Int, Vector3Int?> parentsDictionary = new();

		positionsToCheck.Add(startTerrain);
		priorityDictionary.Add(startTerrain, 0);
		costDictionary.Add(startTerrain, 0);
		parentsDictionary.Add(startTerrain, null);

		while (positionsToCheck.Count > 0)
		{
			Vector3Int current = GetClosestVertex(positionsToCheck, priorityDictionary);

			positionsToCheck.Remove(current);
			if (current == endTerrain)
			{
				path = GeneratePath(parentsDictionary, current);
				return path;
			}

			int i = 0;
			foreach (Vector3Int tile in world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAYINCREMENT))
			{
				i++;
				if (i % 2 == 0)
					continue;

				Vector3Int neighbor = tile + current;

				if (neighbor != endTerrain)
				{
					if (avoidList.Contains(neighbor) || world.militaryStationLocs.Contains(neighbor))
						continue;
				} 

				if (world.CheckForFinalMarch(neighbor))
					continue;

				if (world.IsTradeCenterOnTile(neighbor))
					continue;

				int tempCost;
				if (bySea)
					tempCost = world.GetMovementCostAmphibious(neighbor);
				else
					tempCost = world.GetMovementCost(neighbor);

				int newCost = costDictionary[current] + tempCost;
				if (!costDictionary.ContainsKey(neighbor) || newCost < costDictionary[neighbor])
				{
					costDictionary[neighbor] = newCost;

					int priority = newCost + ManhattanDistance(endTerrain, neighbor); //only check the neighbors closest to destination
					positionsToCheck.Add(neighbor);
					priorityDictionary[neighbor] = priority;

					parentsDictionary[neighbor] = current;
				}
			}
		}

		return path;
	}

	//for moving entire across water
	public static List<Vector3Int> TerrainSearchSea(MapWorld world, Vector3Int startTerrain, Vector3Int endTerrain)
	{
		List<Vector3Int> path = new();

		List<Vector3Int> positionsToCheck = new();
		Dictionary<Vector3Int, int> costDictionary = new();
		Dictionary<Vector3Int, int> priorityDictionary = new();
		Dictionary<Vector3Int, Vector3Int?> parentsDictionary = new();

		positionsToCheck.Add(startTerrain);
		priorityDictionary.Add(startTerrain, 0);
		costDictionary.Add(startTerrain, 0);
		parentsDictionary.Add(startTerrain, null);

		while (positionsToCheck.Count > 0)
		{
			Vector3Int current = GetClosestVertex(positionsToCheck, priorityDictionary);

			positionsToCheck.Remove(current);
			if (current == endTerrain)
			{
				path = GeneratePath(parentsDictionary, current);
				return path;
			}

			foreach (Vector3Int tile in world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAYINCREMENT))
			{
				Vector3Int neighbor = tile + current;

				if (!world.PlayerCheckIfSeaPositionIsValid(neighbor))
					continue;
				
				if (world.CheckIfEnemyTerritory(neighbor) && neighbor != endTerrain)
					continue;

				int tempCost = 1;

				if (tile.sqrMagnitude == 18)
				{
					Vector3Int temp = neighbor - current;

					if (world.GetTerrainDataAt(current + new Vector3Int(temp.x, 0, 0)).isLand || world.GetTerrainDataAt(current + new Vector3Int(0, 0, temp.z)).isLand)
						continue;

					tempCost = Mathf.RoundToInt(tempCost * 1.4f); //multiply by square root 2 for the diagonal squares
				}

				int newCost = costDictionary[current] + tempCost;
				if (!costDictionary.ContainsKey(neighbor) || newCost < costDictionary[neighbor])
				{
					costDictionary[neighbor] = newCost;

					int priority = newCost + ManhattanDistance(endTerrain, neighbor); //only check the neighbors closest to destination
					positionsToCheck.Add(neighbor);
					priorityDictionary[neighbor] = priority;

					parentsDictionary[neighbor] = current;
				}
			}
		}

		return path;
	}

	public static List<Vector3Int> TerrainSearchEnemy(MapWorld world, Vector3Int startTerrain, Vector3Int endTerrain, bool finalSea = false)
	{
		List<Vector3Int> path = new();

		List<Vector3Int> positionsToCheck = new();
		Dictionary<Vector3Int, int> costDictionary = new();
		Dictionary<Vector3Int, int> priorityDictionary = new();
		Dictionary<Vector3Int, Vector3Int?> parentsDictionary = new();

		positionsToCheck.Add(startTerrain);
		priorityDictionary.Add(startTerrain, 0);
		costDictionary.Add(startTerrain, 0);
		parentsDictionary.Add(startTerrain, null);

		while (positionsToCheck.Count > 0)
		{
			Vector3Int current = GetClosestVertex(positionsToCheck, priorityDictionary);

			positionsToCheck.Remove(current);
			if (current == endTerrain)
			{
				path = GeneratePath(parentsDictionary, current);
				return path;
			}

			foreach (Vector3Int tile in world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAYINCREMENT))
			{
				Vector3Int neighbor = tile + current;

				if (finalSea)
				{
					if (neighbor != endTerrain)
					{
						if (!world.CheckIfPositionIsMarchableForEnemy(neighbor))
							continue;
					}
				}
				else
				{
					if (!world.CheckIfPositionIsMarchableForEnemy(neighbor)) 
						continue;
				}

				if (world.IsCityOnTile(neighbor) && endTerrain != neighbor) //won't walk through cities
					continue;

				//if (world.IsEnemyCampHere(neighbor))
				//	continue;

				int tempCost = world.GetMovementCost(neighbor);

				if (tile.sqrMagnitude == 18)
				{
                    //Vector3Int temp = neighbor - current;

					if (!world.CheckIfPositionIsEnemyArmyValid(current + new Vector3Int(tile.x, 0, 0)) || !world.CheckIfPositionIsEnemyArmyValid(current + new Vector3Int(0, 0, tile.z)))
						continue;

					tempCost = Mathf.RoundToInt(tempCost * 1.4f); //multiply by square root 2 for the diagonal squares
				}

				int newCost = costDictionary[current] + tempCost;
				if (!costDictionary.ContainsKey(neighbor) || newCost < costDictionary[neighbor])
				{
					costDictionary[neighbor] = newCost;

					int priority = newCost + ManhattanDistance(endTerrain, neighbor); //only check the neighbors closest to destination
					positionsToCheck.Add(neighbor);
					priorityDictionary[neighbor] = priority;

					parentsDictionary[neighbor] = current;
				}
			}
		}

		return path;
	}

	public static List<Vector3Int> TerrainSearchSeaEnemy(MapWorld world, Vector3Int startTerrain, Vector3Int endTerrain)
	{
		List<Vector3Int> path = new();

		List<Vector3Int> positionsToCheck = new();
		Dictionary<Vector3Int, int> costDictionary = new();
		Dictionary<Vector3Int, int> priorityDictionary = new();
		Dictionary<Vector3Int, Vector3Int?> parentsDictionary = new();

		positionsToCheck.Add(startTerrain);
		priorityDictionary.Add(startTerrain, 0);
		costDictionary.Add(startTerrain, 0);
		parentsDictionary.Add(startTerrain, null);

		while (positionsToCheck.Count > 0)
		{
			Vector3Int current = GetClosestVertex(positionsToCheck, priorityDictionary);

			positionsToCheck.Remove(current);
			if (current == endTerrain)
			{
				path = GeneratePath(parentsDictionary, current);
				return path;
			}

			foreach (Vector3Int tile in world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAYINCREMENT))
			{
				Vector3Int neighbor = tile + current;

				if (!world.CheckIfPositionIsSailableForEnemy(neighbor)) //If it's an obstacle, ignore
					continue;

				int tempCost = 1;

				if (tile.sqrMagnitude == 18)
				{
					//Vector3Int temp = neighbor - current;

					if (world.GetTerrainDataAt(current + new Vector3Int(tile.x, 0, 0)).isLand || world.GetTerrainDataAt(current + new Vector3Int(0, 0, tile.z)).isLand)
						continue;

					tempCost = Mathf.RoundToInt(tempCost * 1.4f); //multiply by square root 2 for the diagonal squares
				}

				int newCost = costDictionary[current] + tempCost;
				if (!costDictionary.ContainsKey(neighbor) || newCost < costDictionary[neighbor])
				{
					costDictionary[neighbor] = newCost;

					int priority = newCost + ManhattanDistance(endTerrain, neighbor); //only check the neighbors closest to destination
					positionsToCheck.Add(neighbor);
					priorityDictionary[neighbor] = priority;

					parentsDictionary[neighbor] = current;
				}
			}
		}

		return path;
	}

	//for battle movements
	public static List<Vector3Int> BattleMove(MapWorld world, Vector3 startLocation, Vector3Int endPosition, List<Vector3Int> movementList, List<Vector3Int> excludeList, bool atSea)
	{
		List<Vector3Int> path = new();

		Vector3Int startPosition = world.RoundToInt(startLocation);

		List<Vector3Int> positionsToCheck = new();
		Dictionary<Vector3Int, int> costDictionary = new();
		Dictionary<Vector3Int, int> priorityDictionary = new();
		Dictionary<Vector3Int, Vector3Int?> parentsDictionary = new();

		positionsToCheck.Add(startPosition);
		priorityDictionary.Add(startPosition, 0);
		costDictionary.Add(startPosition, 0);
		parentsDictionary.Add(startPosition, null);

		while (positionsToCheck.Count > 0)
		{
			Vector3Int current = GetClosestVertex(positionsToCheck, priorityDictionary);

			positionsToCheck.Remove(current);
			if (current == endPosition)
			{
				path = GeneratePath(parentsDictionary, current);
                return path;
			}

			//foreach (Vector3Int tile in world.GenerateForwardsTiles(forward))
			foreach (Vector3Int tile in world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAY))
			{
				Vector3Int neighbor = tile + current;

                if (neighbor != endPosition)
                {
				    if (atSea)
					{
						if (!world.CheckIfAmphibuousPositionIsValid(neighbor))
							continue;
					}
					else
					{
						if (!world.GetTerrainDataAt(neighbor).canWalk)
							continue;
					}

                    if (!movementList.Contains(neighbor))
                        continue;

                    if (world.IsUnitLocationTaken(neighbor))
                        continue;

                    if (excludeList.Contains(neighbor))
                        continue;
                }

		        int tempCost = world.GetMovementCost(neighbor);

				if (tile.sqrMagnitude == 2)
				{
					tempCost = Mathf.RoundToInt(tempCost * 1.4f);
				}

				int newCost = costDictionary[current] + tempCost;
				if (!costDictionary.ContainsKey(neighbor) || newCost < costDictionary[neighbor])
				{
					costDictionary[neighbor] = newCost;

					int priority = newCost + ManhattanDistance(endPosition, neighbor);
					positionsToCheck.Add(neighbor);
					priorityDictionary[neighbor] = priority;

					parentsDictionary[neighbor] = current;
				}
			}
		}

		return path;
	}

	//move through mountains, oceans, wherever (used quite sparingly)
	public static List<Vector3Int> MoveWherever(MapWorld world, Vector3 startLocation, Vector3Int endPosition)
	{
		List<Vector3Int> path = new();

		Vector3Int startPosition = world.RoundToInt(startLocation);

		List<Vector3Int> positionsToCheck = new();
		Dictionary<Vector3Int, int> costDictionary = new();
		Dictionary<Vector3Int, int> priorityDictionary = new();
		Dictionary<Vector3Int, Vector3Int?> parentsDictionary = new();

		positionsToCheck.Add(startPosition);
		priorityDictionary.Add(startPosition, 0);
		costDictionary.Add(startPosition, 0);
		parentsDictionary.Add(startPosition, null);

		while (positionsToCheck.Count > 0)
		{
			Vector3Int current = GetClosestVertex(positionsToCheck, priorityDictionary);

			positionsToCheck.Remove(current);
			if (current == endPosition)
			{
				path = GeneratePath(parentsDictionary, current);
				return path;
			}

			//foreach (Vector3Int tile in world.GenerateForwardsTiles(forward))
			foreach (Vector3Int tile in world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAY))
			{
				Vector3Int neighbor = tile + current;

				if (!world.TileExists(neighbor))
						continue;

				int tempCost = world.GetMovementCost(neighbor);

				if (tile.sqrMagnitude == 2)
				{
					tempCost = Mathf.RoundToInt(tempCost * 1.4f);
				}

				int newCost = costDictionary[current] + tempCost;
				if (!costDictionary.ContainsKey(neighbor) || newCost < costDictionary[neighbor])
				{
					costDictionary[neighbor] = newCost;

					int priority = newCost + ManhattanDistance(endPosition, neighbor);
					positionsToCheck.Add(neighbor);
					priorityDictionary[neighbor] = priority;

					parentsDictionary[neighbor] = current;
				}
			}
		}

		return path;
	}

    public static List<Vector3Int> TerrainSearchMovingTarget(MapWorld world, Vector3Int startTerrain, Queue<Vector3Int> enemyPath, HashSet<Vector3Int> firstSteps, HashSet<Vector3Int> exemptList, bool update, Vector3Int? lastSpot = null)
    {
		List<Vector3Int> path = new();

		List<Vector3Int> positionsToCheck = new();
		Dictionary<Vector3Int, int> costDictionary = new();
		Dictionary<Vector3Int, int> priorityDictionary = new();
		Dictionary<Vector3Int, Vector3Int?> parentsDictionary = new();

		positionsToCheck.Add(startTerrain);
		costDictionary.Add(startTerrain, 0);
		priorityDictionary.Add(startTerrain, 0);
		parentsDictionary.Add(startTerrain, null);
		int stepCount = 0;
		bool cantReachInTime = false;

		while (enemyPath.Count > 3) //limit until city starts to defend
		{
			stepCount++;
			Vector3Int endTerrain = enemyPath.Dequeue();

			if (update)
			{
				if (endTerrain != lastSpot)
					continue;
				else
					update = false;
			}

			if (Mathf.Abs(endTerrain.x - startTerrain.x) > stepCount * 3 || Mathf.Abs(endTerrain.z - startTerrain.z) > stepCount * 3)
				continue;

			while (positionsToCheck.Count > 0)
			{
				Vector3Int current = GetClosestVertex(positionsToCheck, priorityDictionary);

				positionsToCheck.Remove(current);
				if (current == endTerrain)
				{
					path = GeneratePath(parentsDictionary, current);

					if (path.Count > stepCount)
					{
						path.Clear();
						positionsToCheck.Clear();
						positionsToCheck.Add(startTerrain);
						costDictionary.Clear();
						costDictionary.Add(startTerrain, 0);
						priorityDictionary.Clear();
						priorityDictionary.Add(startTerrain, 0);
						parentsDictionary.Clear();
						parentsDictionary.Add(startTerrain, null);
						cantReachInTime = true;
						break;
					}

					return path;
				}

				foreach (Vector3Int tile in world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAYINCREMENT))
				{
					Vector3Int neighbor = tile + current;

					if (firstSteps.Contains(neighbor))
					{
						if (!world.CheckIfPositionIsValid(neighbor))
							continue;
					}
					else
					{
						if (!world.PlayerCheckIfPositionIsValid(neighbor)) 
							continue;
					}

					if (world.CheckIfEnemyTerritory(neighbor) && !exemptList.Contains(neighbor))
						continue;

					int tempCost = world.GetMovementCost(neighbor);

					if (tile.sqrMagnitude == 18)
					{
						if (endTerrain == neighbor) //can't finish path on diagonal
							continue;
						
						Vector3Int temp = neighbor - current;

						if (!world.CheckIfPositionIsArmyValid(current + new Vector3Int(temp.x, 0, 0)) || !world.CheckIfPositionIsArmyValid(current + new Vector3Int(0, 0, temp.z)))
							continue;

						tempCost = Mathf.RoundToInt(tempCost * 1.4f); //multiply by square root 2 for the diagonal squares
					}

					int newCost = costDictionary[current] + tempCost;
					if (!costDictionary.ContainsKey(neighbor) || newCost < costDictionary[neighbor])
					{
						costDictionary[neighbor] = newCost;

						int priority = newCost + ManhattanDistance(endTerrain, neighbor); //only check the neighbors closest to destination
						positionsToCheck.Add(neighbor);
						priorityDictionary[neighbor] = priority;

						parentsDictionary[neighbor] = current;
					}
				}
			}
		}

		if (cantReachInTime)
			UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't reach target in time");
		else
			UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't reach target");

		return path;
	}

	public static bool TraderMovementCheck(MapWorld world, Vector3Int startPosition, Vector3Int endPosition, bool bySea)
    {
		if (bySea)
			return TraderSeaMovementCheck(world, startPosition, endPosition);
		
		List<Vector3Int> positionsToCheck = new();
        Dictionary<Vector3Int, int> priorityDictionary = new();

        positionsToCheck.Add(startPosition);
        priorityDictionary.Add(startPosition, 0);

        while (positionsToCheck.Count > 0)
        {
            Vector3Int current = GetClosestVertex(positionsToCheck, priorityDictionary);

            positionsToCheck.Remove(current);
            if (current.Equals(endPosition))
            {
                return true;
            }

            foreach (Vector3Int neighbor in world.GetNeighborsFor(current, MapWorld.State.EIGHTWAYINCREMENT))
            {
                if (!world.IsRoadOnTileLocation(neighbor) || world.CheckIfEnemyTerritory(neighbor))
                    continue;

                if (!priorityDictionary.ContainsKey(neighbor))
                {
                    positionsToCheck.Add(neighbor);
                    priorityDictionary[neighbor] = ManhattanDistance(endPosition, neighbor);
                }
            }
        }
        return false;
    }

	public static bool TraderSeaMovementCheck(MapWorld world, Vector3Int startPosition, Vector3Int endPosition)
	{
		List<Vector3Int> positionsToCheck = new();
		Dictionary<Vector3Int, int> priorityDictionary = new();

		positionsToCheck.Add(startPosition);
		priorityDictionary.Add(startPosition, 0);

		while (positionsToCheck.Count > 0)
		{
			Vector3Int current = GetClosestVertex(positionsToCheck, priorityDictionary);

			positionsToCheck.Remove(current);
			if (current.Equals(endPosition))
			{
				return true;
			}

			foreach (Vector3Int neighbor in world.GetNeighborsFor(current, MapWorld.State.EIGHTWAYINCREMENT))
			{
				if (!world.CheckIfSeaPositionIsValid(neighbor) || world.CheckIfEnemyTerritory(neighbor))
					continue;

				if (!priorityDictionary.ContainsKey(neighbor))
				{
					positionsToCheck.Add(neighbor);
					priorityDictionary[neighbor] = ManhattanDistance(endPosition, neighbor);
				}
			}
		}

		return false;
	}

	public static int TraderMovementCheckLength(MapWorld world, Vector3Int startPosition, Vector3Int endPosition, bool bySea)
	{
		if (bySea)
			return TraderSeaMovementCheckLength(world, startPosition, endPosition);

		List<Vector3Int> positionsToCheck = new();
		Dictionary<Vector3Int, int> priorityDictionary = new();
		Dictionary<Vector3Int, Vector3Int?> parentsDictionary = new();

		positionsToCheck.Add(startPosition);
		priorityDictionary.Add(startPosition, 0);

		while (positionsToCheck.Count > 0)
		{
			Vector3Int current = GetClosestVertex(positionsToCheck, priorityDictionary);

			positionsToCheck.Remove(current);
			if (current.Equals(endPosition))
			{
				return parentsDictionary.Count();
			}

			foreach (Vector3Int neighbor in world.GetNeighborsFor(current, MapWorld.State.EIGHTWAYINCREMENT))
			{
				if (!world.IsRoadOnTileLocation(neighbor) || world.CheckIfEnemyTerritory(neighbor))
					continue;

				if (!priorityDictionary.ContainsKey(neighbor))
				{
					positionsToCheck.Add(neighbor);
					priorityDictionary[neighbor] = ManhattanDistance(endPosition, neighbor);
					parentsDictionary[neighbor] = current;
				}
			}
		}
		return 0;
	}

	public static int TraderSeaMovementCheckLength(MapWorld world, Vector3Int startPosition, Vector3Int endPosition)
	{
		List<Vector3Int> positionsToCheck = new();
		Dictionary<Vector3Int, int> priorityDictionary = new();
		Dictionary<Vector3Int, Vector3Int?> parentsDictionary = new();

		positionsToCheck.Add(startPosition);
		priorityDictionary.Add(startPosition, 0);

		while (positionsToCheck.Count > 0)
		{
			Vector3Int current = GetClosestVertex(positionsToCheck, priorityDictionary);

			positionsToCheck.Remove(current);
			if (current.Equals(endPosition))
			{
				return parentsDictionary.Count();
			}

			foreach (Vector3Int neighbor in world.GetNeighborsFor(current, MapWorld.State.EIGHTWAYINCREMENT))
			{
				if (!world.CheckIfSeaPositionIsValid(neighbor) || world.CheckIfEnemyTerritory(neighbor))
					continue;

				if (!priorityDictionary.ContainsKey(neighbor))
				{
					positionsToCheck.Add(neighbor);
					priorityDictionary[neighbor] = ManhattanDistance(endPosition, neighbor);
					parentsDictionary[neighbor] = current;
				}
			}
		}

		return 0;
	}


	public static Vector3Int GetClosestVertex(List<Vector3Int> list, Dictionary<Vector3Int, int> distanceMap)
    {
        Vector3Int candidate = list[0];
        foreach (Vector3Int vertex in list)
        {
            if (distanceMap[vertex] < distanceMap[candidate]) //finds the tile with closest priority, so not every tile is checked
            {
                candidate = vertex;
            }
        }
        return candidate;
    }

    public static int ManhattanDistance(Vector3Int endPos, Vector3Int point) //assigns priority for each tile, lower is higher priority
    {
        return Math.Abs(endPos.x - point.x) + Math.Abs(endPos.z - point.z);
    }

    public static List<Vector3Int> GeneratePath(Dictionary<Vector3Int, Vector3Int?> parentMap, Vector3Int endState)
    {
		List<Vector3Int> path = new() {	endState };
		while (parentMap[endState] != null)
        {
            Vector3Int nextPos = parentMap[endState].Value;
            path.Add(nextPos);
            endState = nextPos;
        }
        path.Reverse();
        return (path.Skip(1).ToList());
    }
}
