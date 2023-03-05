using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using UnityEngine;

public class RoadManager : MonoBehaviour
{
    [SerializeField]
    private GameObject solo, deadEnd, straightRoad, curve, threeWay, fourWay, diagDeadEnd, diagonal, diagCurve, diagThreeWay, diagFourWay;

    [SerializeField]
    private GameObject soloHill, deadEndHill, straightRoadHill, curveHill, threeWayHill, fourWayHill, diagDeadEndHill, diagonalHill, diagCurveHill, diagThreeWayHill, diagFourWayHill;

    [SerializeField]
    private MapWorld world;

    [SerializeField]
    private WorkerTaskManager workerTaskManager;

    public int roadMovementCost = 5, roadBuildingTime = 5, roadRemovingTime = 1;

    [SerializeField]
    private Transform roadHolder;
    private List<MeshFilter> roadMeshList = new();
    
    public readonly static List<Vector3Int> neighborsFourDirections = new()
    {
        new Vector3Int(0,0,1), //up
        new Vector3Int(1,0,0), //right
        new Vector3Int(0,0,-1), //down
        new Vector3Int(-1,0,0), //left
    };

    private readonly static List<Vector3Int> neighborsDiagFourDirections = new()
    {
        new Vector3Int(1, 0, 1), //upper right
        new Vector3Int(1, 0, -1), //lower right
        new Vector3Int(-1, 0, -1), //lower left
        new Vector3Int(-1, 0, 1), //upper left
    };

    private void Awake()
    {
        world.SetRoadCost(roadMovementCost);
    }

    private void CreateRoad(GameObject model, Vector3Int roadPosition, Quaternion rotation, bool straight) //placing road prefabs
    {
        Vector3 pos = roadPosition;
        pos.y = -.04f;
        GameObject roadGO = Instantiate(model, pos, rotation);
        //for tweening (can't tween with combined meshes, looks weird)
        //roadGO.transform.localScale = Vector3.zero;
        //LeanTween.scale(roadGO, new Vector3(1.5f, 1.5f, 1.5f), 0.25f).setEase(LeanTweenType.easeOutBack);
        //if (city) //hiding solo roads for new cities
        //    structure.SetActive(false);
        roadGO.transform.parent = roadHolder.transform;
        Road road = roadGO.GetComponent<Road>();
        roadMeshList.Add(road.MeshFilter);

        world.SetRoads(roadPosition, road, straight);
        //replacing prop
        //TerrainData td = world.GetTerrainDataAt(roadPosition);
        //if (td.prop != null)
        //    td.prop.gameObject.SetActive(true);
    }

    public IEnumerator BuildRoad(Vector3Int roadPosition, Worker worker)
    {
        int timePassed = roadBuildingTime;
        worker.ShowProgressTimeBar(timePassed);
        worker.SetWorkAnimation(true);
        worker.SetTime(timePassed);

        while (timePassed > 0)
        {
            yield return new WaitForSeconds(1);
            timePassed--;
            worker.SetTime(timePassed);
        }

        worker.HideProgressTimeBar();
        worker.SetWorkAnimation(false);
        BuildRoadAtPosition(roadPosition);
        world.RemoveWorkerWorkLocation(roadPosition);

        if (worker.MoreOrdersToFollow())
        {
            worker.BeginBuildingRoad();
        }
        else
        {
            worker.isBusy = false;
            if (worker.isSelected)
                workerTaskManager.TurnOffCancelTask();
            //StartCoroutine(CombineMeshWaiter());
        }
    }

    //public IEnumerator CombineMeshWaiter() //waiting for tweening to finish to combine
    //{
    //    yield return new WaitForSeconds(0.26f);

    //    CombineMeshes();
    //}


    //finds if road changes are happening diagonally or on straight, then destroys objects accordingly
    public void BuildRoadAtPosition(Vector3Int roadPosition) 
    {
        TerrainData td = world.GetTerrainDataAt(roadPosition);
        bool hill = td.GetTerrainData().type == TerrainType.Hill || td.GetTerrainData().type == TerrainType.ForestHill;

        if (td.GetTerrainData().type == TerrainType.Forest || td.GetTerrainData().type == TerrainType.ForestHill)
        {
            GameObject newPropPF = td.GetTerrainData().roadPrefab;
            if (newPropPF != null)
            {
                GameObject newProp = Instantiate(newPropPF, Vector3Int.zero, td.prop.GetChild(0).transform.rotation);
                newProp.transform.SetParent(td.prop, false);
                MeshRenderer[] oldRenderer = td.prop.GetChild(0).GetComponentsInChildren<MeshRenderer>();
                MeshRenderer[] newRenderer = newProp.GetComponentsInChildren<MeshRenderer>();
                td.SetNewRenderer(oldRenderer, newRenderer);

                Destroy(td.prop.GetChild(0).gameObject);
            }
        }
        else if (td.prop != null) //for replacing decor (could destroy)
        {
            td.prop.gameObject.SetActive(false);
        }

        
        world.InitializeRoads(roadPosition);
        //td.MovementCost = basicRoadMovementCost;
        td.hasRoad = true;
        (List<(Vector3Int, bool, int[])> roadNeighbors, int[] straightRoads, int[] diagRoads) = world.GetRoadNeighborsFor(roadPosition);

        int straightRoadsCount = straightRoads.Sum(); //number of roads in straight neighbors
        int diagRoadsCount = diagRoads.Sum(); //number of roads in diagonal neighbors

        //making road shape based on how many of its neighbors have roads, and where the roads are
        if (straightRoadsCount + diagRoadsCount == 0)
            CreateRoadSolo(roadPosition, hill);

        world.SetRoadLocations(roadPosition);

        if (straightRoadsCount > 0)
            SetRoadLocations(roadPosition, straightRoads, true);
        if (diagRoadsCount > 0)
            SetRoadLocations(roadPosition, diagRoads, false);

        if (straightRoadsCount > 0)
            PrepareRoadCreation(roadPosition, straightRoads, straightRoadsCount, true, hill);
        if (diagRoadsCount > 0)
            PrepareRoadCreation(roadPosition, diagRoads, diagRoadsCount, false, hill);

        //changing neighbor roads to meet up with new road
        FixNeighborRoads(roadNeighbors);
        CombineMeshes();
    }

    private void FixNeighborRoads(List<(Vector3Int, bool, int[])> roadNeighbors)
    {
        foreach ((Vector3Int roadLoc, bool straight, int[] roads) in roadNeighbors)
        {
            int roadCount = roads.Sum();
            bool hill = world.GetTerrainDataAt(roadLoc).GetTerrainData().type == TerrainType.Hill || world.GetTerrainDataAt(roadLoc).GetTerrainData().type == TerrainType.ForestHill;

            Road road = world.GetRoads(roadLoc, straight);
            if (road != null)
            {
                roadMeshList.Remove(road.MeshFilter);
                Destroy(road.gameObject);
            }
            if (world.IsSoloRoadOnTileLocation(roadLoc))
            {
                Road road2 = world.GetRoads(roadLoc, false);
                if (road2 != null)
                {
                    roadMeshList.Remove(road2.MeshFilter);
                    Destroy(road2.gameObject);
                }
                world.RemoveSoloRoadLocation(roadLoc);
            }

            if (roadCount == 0) //for placing solo roads on neighboring roads (when removing roads)
            {
                if(world.SoloRoadCheck(roadLoc, straight))
                {
                    if (!world.IsSoloRoadOnTileLocation(roadLoc)) //if there's not already a solo road there
                        CreateRoadSolo(roadLoc, hill);
                }
            }

            SetRoadLocations(roadLoc, roads, straight);
            PrepareRoadCreation(roadLoc, roads, roadCount, straight, hill);
        }
    }

    private void PrepareRoadCreation(Vector3Int roadPosition, int[] roads, int roadCount, bool straight, bool hill)
    {
        if (roadCount == 1) //dead end if just one 
        {
            CreateDeadEnd(roadPosition, roads, straight, hill);
        }
        else if (roadCount == 2)
        {
            CreateTwoWay(roadPosition, roads, straight, hill);
        }
        else if (roadCount == 3)
        {
            CreateThreeWay(roadPosition, roads, straight, hill);
        }
        else if (roadCount == 4)
        {
            CreateFourWay(roadPosition, straight, hill);
        }
    }

    private void SetRoadLocations(Vector3Int location, int[] roads, bool straight)
    {
        for (int i = 0; i < 4; i++)
        {
            if (roads[i] == 1)
            {
                if (straight)
                    world.SetRoadLocations(location + neighborsFourDirections[i]);
                else
                    world.SetRoadLocations(location + neighborsDiagFourDirections[i]);
            }
            else
            {
                if (straight)
                    world.RemoveRoadLocation(location + neighborsFourDirections[i]);
                else
                    world.RemoveRoadLocation(location + neighborsDiagFourDirections[i]);
            }
        }
    }

    private void CreateRoadSolo(Vector3Int roadPosition, bool hill)
    {
        GameObject road = hill ? soloHill : solo;
        CreateRoad(road, roadPosition, Quaternion.Euler(0, 0, 0), false); //solo roads still exists when connecting with straight road
        world.SetSoloRoadLocations(roadPosition);
    }

    private void CreateDeadEnd(Vector3Int roadPosition, int[] roads, bool straight, bool hill)
    {
        int index = Array.FindIndex(roads, x => x == 1);
        GameObject road;

        if (hill)
            road = straight ? deadEndHill : diagDeadEndHill;
        else
            road = straight ? deadEnd : diagDeadEnd;
        CreateRoad(road, roadPosition, Quaternion.Euler(0, index * 90, 0), straight);
    }

    private void CreateTwoWay(Vector3Int roadPosition, int[] roads, bool straight, bool hill)
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
            GameObject road;

            if (hill)
                road = straight ? straightRoadHill : diagonalHill;
            else
                road = straight ? straightRoad : diagonal;
            CreateRoad(road, roadPosition, Quaternion.Euler(0, rotationFactor * 90, 0), straight);
        }
        else //for curves
        {
            rotationFactor = totalIndex / 2;
            if (totalIndex == 3 && index == 3) 
                rotationFactor = 3;

            GameObject road;
            
            if (hill)
                road = straight ? curveHill : diagCurveHill;
            else
                road = straight ? curve : diagCurve;
            CreateRoad(road, roadPosition, Quaternion.Euler(0, rotationFactor * 90, 0), straight);
        }
    }

    private void CreateThreeWay(Vector3Int roadPosition, int[] roads, bool straight, bool hill)
    {
        int index = Array.FindIndex(roads, x => x == 0);
        GameObject road;

        if (hill)
            road = straight ? threeWayHill : diagThreeWayHill;
        else
            road = straight ? threeWay: diagThreeWay;
        CreateRoad(road, roadPosition, Quaternion.Euler(0, index * 90, 0), straight);
    }

    private void CreateFourWay(Vector3Int roadPosition, bool straight, bool hill)
    {
        GameObject road;

        if (hill)
            road = straight ? fourWayHill : diagFourWayHill;
        else
            road = straight ? fourWay : diagFourWay;

        CreateRoad(road, roadPosition, Quaternion.Euler(0, 0, 0), straight);
    }

    public IEnumerator RemoveRoad(Vector3Int tile, Worker worker)
    {
        int timePassed = roadRemovingTime;
        worker.ShowProgressTimeBar(timePassed);
        worker.SetWorkAnimation(true);
        worker.SetTime(timePassed);

        while (timePassed > 0)
        {
            yield return new WaitForSeconds(1);
            timePassed--;
            worker.SetTime(timePassed);
        }

        //worker.PlaySplash(tile, isHill);
        worker.HideProgressTimeBar();
        worker.SetWorkAnimation(false);
        //worker.isBusy = false;
        //workerTaskManager.TurnOffCancelTask();
        RemoveRoadAtPosition(tile);
        world.RemoveWorkerWorkLocation(tile);

        if (worker.MoreOrdersToFollow())
        {
            worker.BeginRoadRemoval();
        }
        else
        {
            worker.isBusy = false;
            worker.removing = false;
            if (worker.isSelected)
                workerTaskManager.TurnOffCancelTask();
        }
    }

    public void RemoveRoadAtPosition(Vector3Int tile)
    {
        TerrainData td = world.GetTerrainDataAt(tile);
        td.ResetMovementCost();
        td.hasRoad = false;

        foreach (Road road in world.GetAllRoadsOnTile(tile))
        {
            if (road == null)
                continue;
            roadMeshList.Remove(road.MeshFilter);
            Destroy(road.gameObject);
            //for tweening (can't tween with combined meshes, looks weird)
            //LeanTween.scale(road.gameObject, Vector3.zero, 0.25f).setEase(LeanTweenType.easeOutBack).setOnComplete( ()=> { Destroy(road.gameObject); } );
        }
        world.RemoveRoad(tile);
        world.RemoveRoadLocation(tile);
        world.RemoveSoloRoadLocation(tile);

        foreach (Vector3Int neighbor in neighborsFourDirections)
            world.RemoveRoadLocation(tile + neighbor);
        foreach (Vector3Int neighbor in neighborsDiagFourDirections)
            world.RemoveRoadLocation(tile + neighbor);

        (List<(Vector3Int, bool, int[])> removedRoadNeighbors, int[] straightRoads, int[] diagRoads) = world.GetRoadNeighborsFor(tile);
        FixNeighborRoads(removedRoadNeighbors);
        CombineMeshes();
    }

    public void CombineMeshes()
    {
        //MeshFilter[] meshFilters;

        //if (adding)
        //    meshFilters = roadHolder.GetComponentsInChildren<MeshFilter>();
        //else
        MeshFilter[] meshFilters = roadMeshList.ToArray();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        int i = 0;

        while (i < meshFilters.Length)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);

            i++;
        }

        MeshFilter meshFilter = roadHolder.GetComponent<MeshFilter>();
        meshFilter.mesh = new Mesh();
        meshFilter.mesh.CombineMeshes(combine);
        roadHolder.GetComponent<MeshCollider>().sharedMesh = meshFilter.sharedMesh;

        roadHolder.transform.gameObject.SetActive(true);
    }
}
