using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// Source https://github.com/lordjesus/Packt-Introduction-to-graph-algorithms-for-game-developers
public class GridSearch
{
    public static (List<Vector3Int>, List<bool>, int) AStarSearch(MapWorld world, Vector3Int startPosition, Vector3Int endPosition, 
        int currentMovePoints, int regMovePoints, bool isTrader)
    {
        List<Vector3Int> path = new();
        List<bool> newTurnList = new(); //added to check for units sharing tile

        List<Vector3Int> positionsToCheck = new();
        Dictionary<Vector3Int, int> costDictionary = new();
        Dictionary<Vector3Int, int> priorityDictionary = new();
        Dictionary<Vector3Int, Vector3Int?> parentsDictionary = new();
        Dictionary<Vector3Int, int> movePointsDictionary = new(); //added to check for units sharing tile
        Dictionary<Vector3Int, bool> newTurnDictionary = new(); //added to check for units sharing tile

        //added to make movement orders after movement points are gone
        if (currentMovePoints <= 0)
            currentMovePoints = regMovePoints;

        positionsToCheck.Add(startPosition);
        priorityDictionary.Add(startPosition, 0);
        costDictionary.Add(startPosition, 0);
        parentsDictionary.Add(startPosition, null);
        movePointsDictionary.Add(startPosition, currentMovePoints); //added to check for units sharing tile
        newTurnDictionary.Add(startPosition, true); //added to check for units sharing tile

        while (positionsToCheck.Count > 0)
        {
            Vector3Int current = GetClosestVertex(positionsToCheck, priorityDictionary);
            bool hasRoad = world.GetTerrainDataAt(current).hasRoad; //for going onto road from non-road

            positionsToCheck.Remove(current);
            if (current.Equals(endPosition))
            {
                //newTurnDictionary[current] = true;
                (path, newTurnList) = GeneratePath(parentsDictionary, newTurnDictionary, current);
                return (path, newTurnList, costDictionary[current]);
            }

            foreach (Vector3Int neighbor in world.GetNeighborsFor(current, MapWorld.State.EIGHTWAY))
            {
                if (!world.CheckIfPositionIsValid(neighbor)) //If it's an obstacle, ignore
                    continue;

                if (isTrader && !world.GetTerrainDataAt(neighbor).hasRoad)
                    continue;

                int tempCost = world.GetMovementCost(neighbor, hasRoad);


                //everything below added to check for units sharing the same tile
                bool newTurn = false;
                int movePoints = movePointsDictionary[current] - tempCost;

                if (movePoints <= 0)
                {
                    if (!isTrader && world.IsUnitLocationTaken(neighbor)) //If unit is there, don't finish turn on tile
                        continue;
                    tempCost += movePoints; //since it's the last path, moving onto difficult terrain costs less
                    movePoints = regMovePoints;
                    newTurn = true;
                }
                //everything above to check for units sharing the same tile


                int newCost = costDictionary[current] + tempCost;
                if (!costDictionary.ContainsKey(neighbor) || newCost < costDictionary[neighbor])
                {
                    costDictionary[neighbor] = newCost;

                    movePointsDictionary[neighbor] = movePoints;//added to check for units sharing tile
                    newTurnDictionary[neighbor] = newTurn;//added to check for units sharing tile

                    int priority = newCost + ManhattanDistance(endPosition, neighbor); //only check the neighbors closest to destination
                    positionsToCheck.Add(neighbor);
                    priorityDictionary[neighbor] = priority;

                    parentsDictionary[neighbor] = current;
                }
            }
        }
        Debug.Log("Cannot reach selected area");
        return (path, newTurnList, 0);
    }

    public static bool TraderMovementCheck(MapWorld world, Vector3Int startPosition, Vector3Int endPosition, bool isTrader = true)
    {
        List<Vector3Int> positionsToCheck = new();
        //Dictionary<Vector3Int, int> costDictionary = new();
        Dictionary<Vector3Int, int> priorityDictionary = new();

        positionsToCheck.Add(startPosition);
        //costDictionary.Add(startPosition, 0);
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

                if (isTrader && !world.GetTerrainDataAt(neighbor).hasRoad)
                    continue;

                positionsToCheck.Add(neighbor);
                priorityDictionary[neighbor] = ManhattanDistance(endPosition, neighbor);


                //int tempCost = 1;


                //int newCost = costDictionary[current] + tempCost;
                //if (!costDictionary.ContainsKey(neighbor) || newCost < costDictionary[neighbor])
                //{
                //    costDictionary[neighbor] = newCost;

                //    int priority = newCost + ManhattanDistance(endPosition, neighbor);
                //    positionsToCheck.Add(neighbor);
                //    priorityDictionary[neighbor] = priority;
                //}
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

    //added "newTurnDict" to see when turns started for unit
    public static (List<Vector3Int>, List<bool>) GeneratePath(Dictionary<Vector3Int, Vector3Int?> parentMap, Dictionary<Vector3Int, bool> newTurnDict, Vector3Int endState)
    {
        List<Vector3Int> path = new();
        List<bool> newTurnList = new(); //added to check for units sharing tile
        path.Add(endState);
        newTurnList.Add(true); //added to check for units sharing tile, set to true always so that the final destination is recorded as having a unit (upon arrival).
        while (parentMap[endState] != null)
        {
            Vector3Int nextPos = parentMap[endState].Value;
            path.Add(nextPos);
            newTurnList.Add(newTurnDict[nextPos]); //added to check for units sharing tile
            endState = nextPos;
        }
        path.Reverse();
        newTurnList.Reverse(); //added to check for units sharing tile
        return (path.Skip(1).ToList(), newTurnList.Skip(1).ToList());
    }
}
