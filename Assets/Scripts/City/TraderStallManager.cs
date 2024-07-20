using System.Collections.Generic;
using UnityEngine;

public class TraderStallManager
{
    private List<Vector3Int> stallLocs = new();
    private HashSet<Vector3Int> usedStalls = new();
    
    [HideInInspector]
    public bool isFull;

    public void SetUpStallLocs(Vector3Int loc)
    {
        List<Vector3Int> stallCoordinates = new() { Vector3Int.zero, Vector3Int.left, Vector3Int.forward, Vector3Int.back, Vector3Int.right};

        for (int i = 0; i < stallCoordinates.Count; i++)
            stallLocs.Add(loc + stallCoordinates[i]);
    }

    public Vector3Int GetAvailableStall(Vector3Int loc)
    {
        Vector3Int chosenStall = stallLocs[0];
        int dist = 0;
        bool firstOne = true;
        for (int i = 0; i < stallLocs.Count; i++)
        {
            if (usedStalls.Contains(stallLocs[i]))
                continue;
            
            if (firstOne)
            {
                if (i == 0)
                {
                    TakeStall(stallLocs[i]);
                    return stallLocs[i];
                }
                
                firstOne = false;
                chosenStall = stallLocs[i];
                dist = Mathf.Abs(loc.x - stallLocs[i].x) + Mathf.Abs(loc.z - stallLocs[i].z);
                continue;
            }

            if (dist == 1)
                break;

            int newDist = Mathf.Abs(loc.x - stallLocs[i].x) + Mathf.Abs(loc.z - stallLocs[i].z);
            if (newDist < dist)
            {
                chosenStall = stallLocs[i];
                dist = newDist;
            }
		}

        TakeStall(chosenStall);
        return chosenStall;
    }

    public void TakeStall(Vector3Int stall)
    {
        //stallLocs.Remove(stall);
        usedStalls.Add(stall);

        if (usedStalls.Count == stallLocs.Count)
            isFull = true;
    }

    public void OpenStall(Vector3Int stall)
    {
        if (usedStalls.Count == stallLocs.Count)
            isFull = false;

        usedStalls.Remove(stall);
        //stallLocs.Add(stall);
    }

    public HashSet<Vector3Int> GetUsedStalls()
    {
        return usedStalls;
    }
}
