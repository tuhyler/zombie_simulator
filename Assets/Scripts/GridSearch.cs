using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class GridSearch
{
    public static List<Vector3Int> AStarSearch(MapWorld world, Vector3 startLocation, Vector3Int endPosition, bool isTrader, bool bySea)
    {
        if (bySea)
            return AStarSearchSea(world, startLocation, endPosition);
        

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
                
                if (!world.CheckIfPositionIsValid(neighbor)) //If it's an obstacle, ignore
                    continue;

                if (world.CheckIfEnemyTerritory(neighbor))
                    continue;
      //          if (attack && world.IsUnitLocationTaken(neighbor))
      //          {
      //              if (neighbor == endPosition)
      //              {
						//path = GeneratePath(parentsDictionary, current);
      //                  path.Add(neighbor);
      //                  return path;
      //              }
                    
      //              continue;
      //          }

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

                if (isTrader && !hasRoad) //If it's a trader and not on road, ignore
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

                    tempCost = Mathf.RoundToInt(tempCost * 1.414f); //multiply by square root 2 for the diagonal squares
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

        InfoPopUpHandler.WarningMessage().Create(endPosition, "Cannot reach selected area");
        return path;
    }

    public static List<Vector3Int> AStarSearchSea(MapWorld world, Vector3 startLocation, Vector3Int endPosition)
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

        InfoPopUpHandler.WarningMessage().Create(startLocation, "Cannot reach selected area");
        return path;
    }

	//for enemies
	public static List<Vector3Int> AStarSearchEnemy(MapWorld world, Vector3 startLocation, Vector3Int endPosition, bool bySea, List<Vector3Int> avoidList = null)
	{
		if (bySea)
			return AStarSearchSeaEnemy(world, startLocation, endPosition);

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

					tempCost = Mathf.RoundToInt(tempCost * 1.414f); //multiply by square root 2 for the diagonal squares
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

		InfoPopUpHandler.WarningMessage().Create(endPosition, "Cannot reach selected area");
		return path;
	}

	public static List<Vector3Int> AStarSearchSeaEnemy(MapWorld world, Vector3 startLocation, Vector3Int endPosition)
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

		InfoPopUpHandler.WarningMessage().Create(startLocation, "Cannot reach selected area");
		return path;
	}

	//for moving entire army
	public static List<Vector3Int> TerrainSearch(MapWorld world, Vector3Int startTerrain, Vector3Int endTerrain, List<Vector3Int> exemptList)
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

				if (!world.CheckIfPositionIsMarchable(neighbor)) //If it's an obstacle, ignore
					continue;

                if (world.CheckIfEnemyTerritory(neighbor) && !exemptList.Contains(neighbor))
                    continue;

				int tempCost = world.GetMovementCost(neighbor);

				if (tile.sqrMagnitude == 18)
				{
					Vector3Int temp = neighbor - current;

					if (!world.CheckIfPositionIsArmyValid(current + new Vector3Int(temp.x, 0, 0)) || !world.CheckIfPositionIsArmyValid(current + new Vector3Int(0, 0, temp.z)))
						continue;

					tempCost = Mathf.RoundToInt(tempCost * 1.414f); //multiply by square root 2 for the diagonal squares
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

		InfoPopUpHandler.WarningMessage().Create(endTerrain, "Cannot reach selected area");
		return path;
	}

	public static List<Vector3Int> TerrainSearchEnemy(MapWorld world, Vector3Int startTerrain, Vector3Int endTerrain, List<Vector3Int> avoidList)
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

				if (!world.CheckIfPositionIsMarchableForEnemy(neighbor)) //If it's an obstacle, ignore
					continue;

				int tempCost = world.GetMovementCost(neighbor);

				if (tile.sqrMagnitude == 18)
				{
                    //Vector3Int temp = neighbor - current;

					if (!world.CheckIfPositionIsEnemyArmyValid(current + new Vector3Int(tile.x, 0, 0), avoidList) || !world.CheckIfPositionIsEnemyArmyValid(current + new Vector3Int(0, 0, tile.z), avoidList))
						continue;

					tempCost = Mathf.RoundToInt(tempCost * 1.414f); //multiply by square root 2 for the diagonal squares
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
	public static List<Vector3Int> BattleMove(MapWorld world, Vector3 startLocation, Vector3Int endPosition, List<Vector3Int> movementList, List<Vector3Int> excludeList)
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
				    if (!world.CheckIfPositionIsValid(neighbor))
					    continue;

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
					tempCost = Mathf.RoundToInt(tempCost * 1.414f);
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

    public static List<Vector3Int> TerrainSearchMovingTarget(MapWorld world, Vector3Int startTerrain, Queue<Vector3Int> enemyPath, List<Vector3Int> exemptList, bool update, Vector3Int? lastSpot = null)
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
						break;
					}

					return path;
				}

				foreach (Vector3Int tile in world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAYINCREMENT))
				{
					Vector3Int neighbor = tile + current;

					if (!world.CheckIfPositionIsMarchable(neighbor)) //If it's an obstacle, ignore
						continue;

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

						tempCost = Mathf.RoundToInt(tempCost * 1.414f); //multiply by square root 2 for the diagonal squares
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

		return path;
	}

	public static bool TraderMovementCheck(MapWorld world, Vector3Int startPosition, Vector3Int endPosition, bool isSeaTrader = true)
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

            foreach (Vector3Int neighbor in world.GetNeighborsFor(current, MapWorld.State.EIGHTWAY))
            {
                if (isSeaTrader)
                {
                    if (!world.CheckIfSeaPositionIsValid(neighbor))
                        continue;
                }
                else
                {
                    if (!world.CheckIfPositionIsValid(neighbor))
                        continue;

                    if (!world.IsRoadOnTileLocation(neighbor))
                        continue;
                }

                //if (!isSeaTrader && !world.IsRoadOnTileLocation(neighbor))
                //    continue;

                if (!priorityDictionary.ContainsKey(neighbor))
                {
                    positionsToCheck.Add(neighbor);
                    priorityDictionary[neighbor] = ManhattanDistance(endPosition, neighbor);
                }
            }
        }
        return false;
    }


    private static Vector3Int GetClosestVertex(List<Vector3Int> list, Dictionary<Vector3Int, int> distanceMap)
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

    private static int ManhattanDistance(Vector3Int endPos, Vector3Int point) //assigns priority for each tile, lower is higher priority
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
