using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralGeneration
{
    public readonly static List<Vector3Int> neighborsFourDirections = new()
    {
        new Vector3Int(0,0,1), //up
        new Vector3Int(1,0,0), //right
        new Vector3Int(0,0,-1), //down
        new Vector3Int(-1,0,0), //left
    };

    public static HashSet<Vector3Int> SimpleRandomWalk(Vector3Int startPosition, int walkLength)
    {
        HashSet<Vector3Int> path = new HashSet<Vector3Int>();

        path.Add(startPosition);
        var previousPosition = startPosition;

        for (int i = 0; i < walkLength; i++)
        {
            while (path.Count == i+1)
            {
                var newPosition = previousPosition + neighborsFourDirections[Random.Range(0, 4)]; //random direction

                path.Add(newPosition);
                previousPosition = newPosition;
            }
        }

        return path;
    }

    private void RemoveHoles(HashSet<Vector3Int> generatedTiles)
    {

    }

    public static void CreateCoasts()
    {

    }
}
