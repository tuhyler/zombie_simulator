using System.Collections.Generic;
using UnityEngine;

public class PathFinder
{
    public static List<Vector3Int> FindPath(MapWorld world, Vector3Int a_StartPos, Vector3Int a_TargetPos, bool onlyRoad, bool bySea)
    {
        PathNode startNode = world.GetPathNode(a_StartPos);
		PathNode targetNode = world.GetPathNode(a_TargetPos);

		List<PathNode> openList = new();
        HashSet<PathNode> closedList = new();

        openList.Add(startNode);
        startNode.gCost = 0;
        startNode.hCost = CalculateDistanceCost(startNode, targetNode, 1);

        while (openList.Count > 0)
        {
            PathNode currentNode = openList[0];

            for (int i = 0; i < openList.Count; i++)
            {
                if (openList[i].FCost < currentNode.FCost || openList[i].FCost == currentNode.FCost && openList[i].hCost < currentNode.hCost)
                {
                    currentNode = openList[i];
                }
            }

            if (currentNode == targetNode)
                return GetFinalPath(startNode, targetNode);

            openList.Remove(currentNode);
            closedList.Add(currentNode);


            foreach (Vector3Int neighbor in world.GetNeighborsFor(currentNode.Position,MapWorld.State.EIGHTWAY))
            {
                PathNode neighborNode = world.GetPathNode(neighbor);

                if (neighborNode.isWall || closedList.Contains(neighborNode))
                    continue;

                if (bySea && !neighborNode.isSea)
                    continue;

                if (onlyRoad && !neighborNode.hasRoad)
                    continue;

				int moveCost = currentNode.gCost + CalculateDistanceCost(currentNode, neighborNode, neighborNode.movementCost);

                if (moveCost < neighborNode.gCost || !openList.Contains(neighborNode))
                {
                    neighborNode.Parent = currentNode;
                    neighborNode.gCost = moveCost;
                    neighborNode.hCost = CalculateDistanceCost(neighborNode, targetNode, 1);

                    if (!openList.Contains(neighborNode))
                    {
                        openList.Add(neighborNode);
                    }
                }
			}
        }

		InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(a_TargetPos, "Cannot reach selected area");
		return null;
	}

    private static List<Vector3Int> GetFinalPath(PathNode a_StartingNode, PathNode a_EndNode)
    {
        List<Vector3Int> finalPath = new();
        PathNode currentNode = a_EndNode;

        while(currentNode != a_StartingNode)
        {
            finalPath.Add(currentNode.Position);
            currentNode = currentNode.Parent;
        }

        finalPath.Reverse();
        return finalPath;
    }

    private static int CalculateDistanceCost(PathNode a_nodeA, PathNode a_nodeB, int terrainCost)
    {
        int ix = Mathf.Abs(a_nodeA.gridX - a_nodeB.gridX);
        int iy = Mathf.Abs(a_nodeA.gridY - a_nodeB.gridY);
        int remaining = Mathf.Abs(ix - iy);

        return 14 * terrainCost * Mathf.Min(ix, iy) + 10 * terrainCost * remaining;
    }
}
