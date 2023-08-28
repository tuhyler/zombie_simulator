using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathNode
{
    public int gridX;
    public int gridY;

    public bool isWall, isSea, hasRoad;
    public Vector3Int Position;

    public PathNode Parent;

    public int movementCost;
    public int gCost;
    public int hCost;

    public int FCost { get { return gCost + hCost; } }

    public PathNode(bool a_IsWall, bool a_isSea, Vector3Int a_Pos, int a_gridX, int a_gridY, int cost)
    {
        isWall = a_IsWall;
        Position = a_Pos;
        gridX = a_gridX;
        gridY = a_gridY;
        isSea = a_isSea;
        movementCost = cost;
    }
}
