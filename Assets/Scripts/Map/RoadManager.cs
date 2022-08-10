using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoadManager : MonoBehaviour
{
    [SerializeField]
    private GameObject solo, deadEnd, straightRoad, curve, threeWay, fourWay, diagDeadEnd, diagonal, diagCurve, diagThreeWay, diagFourWay;

    [SerializeField]
    private MapWorld world;

    private int basicRoadMovementCost = 5;



    private void CreateRoad(GameObject model, Vector3Int roadPosition, Quaternion rotation, bool straight) //creating road prefabs
    {
        Vector3 pos = roadPosition;
        pos.y += .5f;
        GameObject structure = Instantiate(model, pos, rotation);
        world.SetRoads(roadPosition, structure, straight);
    }

    //finds if road changes are happening diagonally or on straight, then destroys objects accordingly
    public void BuildRoadAtPosition(Vector3Int roadPosition) 
    {
        TerrainData td = world.GetTerrainDataAt(roadPosition);

        if (td.GetTerrainData().type == TerrainType.Forest)
        {
            GameObject newPrefab = td.GetTerrainData().roadPrefab;
            GameObject newTile = Instantiate(newPrefab, roadPosition, Quaternion.Euler(0, 0, 0));
            td.DestroyTile(world);
            td = newTile.GetComponent<TerrainData>();
            td.AddTerrainToWorld(world);
        }
        
        world.InitializeRoads(roadPosition);
        td.MovementCost = basicRoadMovementCost;
        td.hasRoad = true;
        //world.AddRoadStructure(roadPosition, emptyRoad);
        (List<(Vector3Int, bool, int[])> roadNeighbors, int[] straightRoads, int[] diagRoads) = world.GetRoadNeighborsFor(roadPosition);

        int straightRoadsCount = straightRoads.Sum(); //number of roads in straight neighbors
        int diagRoadsCount = diagRoads.Sum(); //number of roads in diagonal neighbors

        //making road shape based on how many of its neighbors have roads, and where the roads are
        if (straightRoadsCount + diagRoadsCount == 0)
            CreateRoadSolo(roadPosition);

        if (straightRoadsCount > 0)
            PrepareRoadCreation(roadPosition, straightRoads, straightRoadsCount, true);
        if (diagRoadsCount > 0)
            PrepareRoadCreation(roadPosition, diagRoads, diagRoadsCount, false);

        //changing neighbor roads to meet up with new road
        FixNeighborRoads(roadNeighbors);
    }

    private void FixNeighborRoads(List<(Vector3Int, bool, int[])> roadNeighbors)
    {
        foreach ((Vector3Int roadLoc, bool straight, int[] roads) in roadNeighbors)
        {
            int roadCount = roads.Sum();
            Destroy(world.GetRoads(roadLoc, straight)); //destroying road, consider object pooling?
            if (roadCount == 0) //for placing solo roads on neighboring roads (when removing roads)
            {
                if(world.SoloRoadCheck(roadLoc, straight))
                {
                    if (world.GetRoads(roadLoc, false) != solo) //if there's not already a solo road there
                        CreateRoadSolo(roadLoc);
                }
            }

            PrepareRoadCreation(roadLoc, roads, roadCount, straight);
        }
    }

    private void PrepareRoadCreation(Vector3Int roadPosition, int[] roads, int roadCount, bool straight)
    {
        if (roadCount == 1) //dead end if just one 
        {
            CreateDeadEnd(roadPosition, roads, straight);
        }
        else if (roadCount == 2)
        {
            CreateTwoWay(roadPosition, roads, straight);
        }
        else if (roadCount == 3)
        {
            CreateThreeWay(roadPosition, roads, straight);
        }
        else if (roadCount == 4)
        {
            CreateFourWay(roadPosition, straight);
        }
    }

    private void CreateRoadSolo(Vector3Int roadPosition)
    {
        CreateRoad(solo, roadPosition, Quaternion.Euler(0, 0, 0), false); //solo roads still exists when connecting with straight road
    }

    private void CreateDeadEnd(Vector3Int roadPosition, int[] roads, bool straight)
    {
        int index = Array.FindIndex(roads, x => x == 1);
        GameObject road = straight ? deadEnd : diagDeadEnd;
        CreateRoad(road, roadPosition, Quaternion.Euler(0, index * 90, 0), straight);
    }

    private void CreateTwoWay(Vector3Int roadPosition, int[] roads, bool straight)
    {
        int index = 0;
        int totalIndex = 0;
        for (int i = 0; i < 2; i++)
        {
            index = Array.FindIndex(roads, x => x == 1);
            roads[index] = 0; //setting to zero so it doesn't find the first one again
            totalIndex += index;
        }

        int rotationFactor;    

        if (totalIndex % 2 == 0) //for straight roads
        {
            rotationFactor = index % 2;
            GameObject road = straight ? straightRoad : diagonal;
            CreateRoad(road, roadPosition, Quaternion.Euler(0, rotationFactor * 90, 0), straight);
        }
        else //for curves
        {
            rotationFactor = totalIndex / 2;
            if (totalIndex == 3 && index == 3) 
                rotationFactor = 3;
            GameObject road = straight ? curve : diagCurve;
            CreateRoad(road, roadPosition, Quaternion.Euler(0, rotationFactor * 90, 0), straight);
        }
    }

    private void CreateThreeWay(Vector3Int roadPosition, int[] roads, bool straight)
    {
        int index = Array.FindIndex(roads, x => x == 0);
        GameObject road = straight ? threeWay: diagThreeWay;
        CreateRoad(road, roadPosition, Quaternion.Euler(0, index * 90, 0), straight);
    }

    private void CreateFourWay(Vector3Int roadPosition, bool straight)
    {
        GameObject road = straight ? fourWay : diagFourWay;
        CreateRoad(road, roadPosition, Quaternion.Euler(0, 0, 0), straight);
    }

    public void RemoveRoadAtPosition(Vector3Int tile)
    {
        world.RemoveStructure(tile);
        TerrainData td = world.GetTerrainDataAt(tile);
        td.ResetMovementCost();
        td.hasRoad = false;

        foreach (GameObject road in world.GetAllRoadsOnTile(tile))
        {
            Destroy(road);
        }
        world.RemoveRoad(tile);

        (List<(Vector3Int, bool, int[])> removedRoadNeighbors, int[] straightRoads, int[] diagRoads) = world.GetRoadNeighborsFor(tile);
        FixNeighborRoads(removedRoadNeighbors);
    }
}
