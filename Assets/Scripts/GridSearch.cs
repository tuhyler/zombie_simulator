using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridSearch
{
    public static List<Vector3Int> AStarSearch(MapWorld world, Vector3 startLocation, Vector3Int endPosition, bool isTrader, bool bySea)
    {
        if (bySea)
            return AStarSearchSea(world, startLocation, endPosition);
        
        Vector3Int startPosition = world.GetClosestTile(startLocation);

        //below is for units staying on road, don't skip across
        List<Vector3Int> xRoads = new() { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0) };
        List<Vector3Int> zRoads = new() { new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };
        List<Vector3Int> xzRoads = new() { new Vector3Int(1, 0, 1), new Vector3Int(-1, 0, 1), new Vector3Int(1, 0, -1), new Vector3Int(-1, 0, -1) };
        //above is for units staying on road

        List <Vector3Int> path = new();

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

                bool isRoadOnTileLocation;// = world.IsRoadOnTileLocation(neighbor);
                int tempCost;

                //below is for units staying on roads
                if (!centerRoad)
                {
                    if (xzRoad && xzRoads.Contains(tile))
                        isRoadOnTileLocation = world.IsRoadOnTileLocation(neighbor);
                    else if (xRoad && xRoads.Contains(tile))
                        isRoadOnTileLocation = world.IsRoadOnTileLocation(neighbor);
                    else if (zRoad && zRoads.Contains(tile))
                        isRoadOnTileLocation = world.IsRoadOnTileLocation(neighbor);
                    else
                        isRoadOnTileLocation = false;
                }
                else
                {
                    isRoadOnTileLocation = world.IsRoadOnTileLocation(neighbor);
                }
                //above is for units staying on roads

                if (isTrader && !isRoadOnTileLocation) //If it's a trader and not on road, ignore
                    continue;

                if (isRoadOnTileLocation)
                    tempCost = world.GetRoadCost();
                else
                    tempCost = world.GetMovementCost(neighbor);

                if (tile.sqrMagnitude == 2)
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

        InfoPopUpHandler.Create(endPosition, "Cannot reach selected area");
        return path;
    }

    public static List<Vector3Int> AStarSearchSea(MapWorld world, Vector3 startLocation, Vector3Int endPosition)
    {
        Vector3Int startPosition = world.GetClosestTile(startLocation);

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
            bool currentIsRiverOrCoast = false;

            if (current.x % 3 != 0 && current.z % 3 != 0)
                currentIsRiverOrCoast = world.CheckIfSeaPositionIsRiverOrCoast(current);

            foreach (Vector3Int tile in world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAY))
            {
                Vector3Int neighbor = tile + current;
                int sqrMagnitude = tile.sqrMagnitude;

                //below is for units not cutting corners
                bool neighborIsRiverOrCoast = false;
                
                if (sqrMagnitude == 2)
                    neighborIsRiverOrCoast = world.CheckIfSeaPositionIsRiverOrCoast(current);
                
                if (currentIsRiverOrCoast && neighborIsRiverOrCoast)
                    continue;

                if (!world.CheckIfSeaPositionIsValid(neighbor))
                    continue;
                //above is for units not cutting corners

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

        InfoPopUpHandler.Create(endPosition, "Cannot reach selected area");
        return path;
    }


    public static bool TraderMovementCheck(MapWorld world, Vector3Int startPosition, Vector3Int endPosition, bool isTrader = true)
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
                if (!world.CheckIfPositionIsValid(neighbor))
                    continue;

                if (isTrader && !world.IsRoadOnTileLocation(neighbor))
                    continue;

                positionsToCheck.Add(neighbor);
                priorityDictionary[neighbor] = ManhattanDistance(endPosition, neighbor);
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
        List<Vector3Int> path = new();
        path.Add(endState);
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
