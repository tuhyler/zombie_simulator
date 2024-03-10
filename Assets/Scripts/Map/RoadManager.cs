using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoadManager : MonoBehaviour
{
    [SerializeField]
    List<UtilityCostSO> roadCosts = new();
    
    [SerializeField]
    private GameObject solo, deadEnd, straightRoad, curve, threeWay, fourWay, diagDeadEnd, diagonal, diagCurve, diagThreeWay, diagFourWay, bridge;

    [SerializeField]
    private GameObject soloHill, deadEndHill, straightRoadHill, curveHill, threeWayHill, fourWayHill, diagDeadEndHill, diagonalHill, diagCurveHill, diagThreeWayHill, diagFourWayHill;

    [SerializeField]
    private MapWorld world;

    [SerializeField]
    private WorkerTaskManager workerTaskManager;

    private bool hideRoads;

    public int roadMovementCost = 5, roadBuildingTime = 5, roadRemovingTime = 1;
    //[HideInInspector]
    //public int timePassed;
    //private WaitForSeconds oneSecondWait = new WaitForSeconds(1);

    [SerializeField]
    private Transform roadHolder, roadHolderMinimap;
    [HideInInspector]
    public List<MeshFilter> roadMeshList = new();
    public Dictionary<int, UtilityCostSO> roadCostDict = new();
    
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
        SetDicts();
    }

    private void SetDicts()
    {
        for (int i = 0; i < roadCosts.Count; i++)
            roadCostDict[roadCosts[i].utilityLevel] = roadCosts[i];
    }

    private void CreateRoad(GameObject model, Vector3Int roadPosition, Quaternion rotation, bool straight, bool highlight, int level) //placing road prefabs
    {
        Vector3 pos = roadPosition;
        //pos.y = -.04f;
        GameObject roadGO = Instantiate(model, pos, rotation);
        //for tweening (can't tween with combined meshes, looks weird)
        //roadGO.transform.localScale = Vector3.zero;
        //LeanTween.scale(roadGO, new Vector3(1.5f, 1.5f, 1.5f), 0.25f).setEase(LeanTweenType.easeOutBack);
        //if (city) //hiding solo roads for new cities
        //    structure.SetActive(false);
        roadGO.transform.parent = roadHolder.transform;
        Road road = roadGO.GetComponent<Road>();
        road.roadLevel = level;
        if (highlight)
            road.SelectionHighlight.EnableHighlight(Color.white);

        if (hideRoads)
        {
            roadGO.SetActive(false);
        }
        else
        {
            roadMeshList.Add(road.MeshFilter);
        }

        world.SetRoads(roadPosition, road, straight);
        //replacing prop
        //TerrainData td = world.GetTerrainDataAt(roadPosition);
        //if (td.prop != null)
        //    td.prop.gameObject.SetActive(true);
    }

   // public IEnumerator BuildRoad(Vector3Int roadPosition, Worker worker)
   // {
   //     worker.ShowProgressTimeBar(roadBuildingTime);
   //     worker.SetWorkAnimation(true);
   //     worker.SetTime(timePassed);

   //     while (timePassed > 0)
   //     {
   //         yield return oneSecondWait;
   //         timePassed--;
   //         worker.SetTime(timePassed);
   //     }

   //     worker.taskCo = null;
   //     worker.HideProgressTimeBar();
   //     worker.SetWorkAnimation(false);
   //     BuildRoadAtPosition(roadPosition);
   //     world.RemoveWorkerWorkLocation(roadPosition);

   //     //moving worker up a smidge to be on top of road
   //     Vector3 moveUp = worker.transform.position;
   //     moveUp.y += .2f;
   //     worker.transform.position = moveUp;

   //     if (worker.MoreOrdersToFollow())
   //     {
   //         worker.BeginBuildingRoad();
   //     }
   //     else
   //     {
   //         world.mainPlayer.isBusy = false;
			//worker.building = false;
			//if (world.mainPlayer.isSelected)
   //             workerTaskManager.TurnOffCancelTask();
   //         //StartCoroutine(CombineMeshWaiter());
   //     }
   // }

    //public IEnumerator CombineMeshWaiter() //waiting for tweening to finish to combine
    //{
    //    yield return new WaitForSeconds(0.26f);

    //    CombineMeshes();
    //}


    //finds if road changes are happening diagonally or on straight, then destroys objects accordingly
    public void BuildRoadAtPosition(Vector3Int roadPosition, int level) 
    {
        TerrainData td = world.GetTerrainDataAt(roadPosition);
        bool river = td.straightRiver;
        bool hill = td.isHill;

        hideRoads = !td.isDiscovered;

        if (td.terrainData.type == TerrainType.Forest || td.terrainData.type == TerrainType.ForestHill)
            td.SwitchToRoad();
        
        world.InitializeRoads(roadPosition);
        td.hasRoad = true;
        (List<(Vector3Int, bool, int[])> roadNeighbors, int[] straightRoads, int[] diagRoads) = world.GetRoadNeighborsFor(roadPosition, river);

        int straightRoadsCount = straightRoads.Sum(); //number of roads in straight neighbors
        int diagRoadsCount = diagRoads.Sum(); //number of roads in diagonal neighbors

        //making road shape based on how many of its neighbors have roads, and where the roads are
        if (straightRoadsCount + diagRoadsCount == 0)
        {
            if (river)
                CreateBridge(roadPosition, false, level);
            else
                CreateRoadSolo(roadPosition, hill, false, level);
        }

        world.SetRoadLocations(roadPosition);

        if (straightRoadsCount > 0)
            SetRoadLocations(roadPosition, straightRoads, true);
        if (diagRoadsCount > 0)
            SetRoadLocations(roadPosition, diagRoads, false);

        if (straightRoadsCount > 0)
            PrepareRoadCreation(roadPosition, straightRoads, straightRoadsCount, true, hill, false, river, level);
        if (diagRoadsCount > 0)
            PrepareRoadCreation(roadPosition, diagRoads, diagRoadsCount, false, hill, false, river, level);

        //changing neighbor roads to meet up with new road
        FixNeighborRoads(roadNeighbors);

        CombineMeshes();

        AddCityRoads(roadPosition, level);
    }

    public void AddCityRoads(Vector3Int roadPosition, int level)
    {
        foreach (Vector3Int loc in world.GetNeighborsFor(roadPosition, MapWorld.State.EIGHTWAYINCREMENT))
        {
            if (world.IsRoadOnTerrain(loc))
                continue;

            if (world.IsTradeCenterOnTile(loc))
            {
                BuildRoadAtPosition(loc, level);

                TradeCenter center = world.GetTradeCenter(loc);
                Vector3 pos = center.tcRep.transform.position;
                pos.y += .2f;
                center.tcRep.transform.position = pos;
            }

            if (world.IsWonderOnTile(loc) && world.GetWonder(loc).unloadLoc == loc)
            {
                //if (world.GetRoadLevel(loc) > level)
                //    UpgradeRoad();
            }

            if (world.IsCityOnTile(loc))
            {
                BuildRoadAtPosition(loc, level);

                if (loc - roadPosition == new Vector3Int(0, 0, 3))
                    world.GetCity(loc).RepositionFire();
            }
        }
    }

    private void FixNeighborRoads(List<(Vector3Int, bool, int[])> roadNeighbors)
    {
        foreach ((Vector3Int roadLoc, bool straight, int[] roads) in roadNeighbors)
        {
            TerrainData td = world.GetTerrainDataAt(roadLoc);
            if (td.straightRiver)
                continue;
            int roadCount = roads.Sum();
            bool highlight = td.isGlowing;
            bool hill = td.isHill;
            int level = world.GetRoadLevel(roadLoc);

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
                        CreateRoadSolo(roadLoc, hill, highlight, level);
                }
            }

            SetRoadLocations(roadLoc, roads, straight);
            PrepareRoadCreation(roadLoc, roads, roadCount, straight, hill, highlight, false, level);
        }
    }

    private void PrepareRoadCreation(Vector3Int roadPosition, int[] roads, int roadCount, bool straight, bool hill, bool highlight, bool river, int level)
    {
        if (river)
        {
            CreateBridge(roadPosition, highlight, level);
        }
        else if (roadCount == 1) //dead end if just one 
        {
            CreateDeadEnd(roadPosition, roads, straight, hill, highlight, level);
        }
        else if (roadCount == 2)
        {
            CreateTwoWay(roadPosition, roads, straight, hill, highlight, level);
        }
        else if (roadCount == 3)
        {
            CreateThreeWay(roadPosition, roads, straight, hill, highlight, level);
        }
        else if (roadCount == 4)
        {
            CreateFourWay(roadPosition, straight, hill, highlight, level);
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

    private void CreateBridge(Vector3Int roadPosition, bool highlight, int level)
    {
        List<Vector3Int> land = world.GetNeighborsCoordinates(MapWorld.State.FOURWAYINCREMENT);
        int rotation = 0;
        if (world.GetTerrainDataAt(land[1]).isLand && world.GetTerrainDataAt(land[3]).isLand)
            rotation = 90;

        CreateRoad(bridge, roadPosition, Quaternion.Euler(0, rotation, 0), false, highlight, level);
	}

    private void CreateRoadSolo(Vector3Int roadPosition, bool hill, bool highlight, int level)
    {
        GameObject road = hill ? soloHill : solo;
        CreateRoad(road, roadPosition, Quaternion.Euler(0, 0, 0), false, highlight, level); //solo roads still exists when connecting with straight road
        world.SetSoloRoadLocations(roadPosition);
    }

    private void CreateDeadEnd(Vector3Int roadPosition, int[] roads, bool straight, bool hill, bool highlight, int level)
    {
        int index = Array.FindIndex(roads, x => x == 1);
        GameObject road;

        if (hill)
            road = straight ? deadEndHill : diagDeadEndHill;
        else
            road = straight ? deadEnd : diagDeadEnd;
        CreateRoad(road, roadPosition, Quaternion.Euler(0, index * 90, 0), straight, highlight, level);
    }

    private void CreateTwoWay(Vector3Int roadPosition, int[] roads, bool straight, bool hill, bool highlight, int level)
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
            CreateRoad(road, roadPosition, Quaternion.Euler(0, rotationFactor * 90, 0), straight, highlight, level);
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
            CreateRoad(road, roadPosition, Quaternion.Euler(0, rotationFactor * 90, 0), straight, highlight, level);
        }
    }

    private void CreateThreeWay(Vector3Int roadPosition, int[] roads, bool straight, bool hill, bool highlight, int level)
    {
        int index = Array.FindIndex(roads, x => x == 0);
        GameObject road;

        if (hill)
            road = straight ? threeWayHill : diagThreeWayHill;
        else
            road = straight ? threeWay: diagThreeWay;
        CreateRoad(road, roadPosition, Quaternion.Euler(0, index * 90, 0), straight, highlight, level);
    }

    private void CreateFourWay(Vector3Int roadPosition, bool straight, bool hill, bool highlight, int level)
    {
        GameObject road;

        if (hill)
            road = straight ? fourWayHill : diagFourWayHill;
        else
            road = straight ? fourWay : diagFourWay;

        CreateRoad(road, roadPosition, Quaternion.Euler(0, 0, 0), straight, highlight, level);
    }

    //public IEnumerator RemoveRoad(Vector3Int tile, Worker worker)
    //{
    //    worker.ShowProgressTimeBar(roadRemovingTime);
    //    worker.SetWorkAnimation(true);
    //    worker.SetTime(timePassed);

    //    while (timePassed > 0)
    //    {
    //        yield return oneSecondWait;
    //        timePassed--;
    //        worker.SetTime(timePassed);
    //    }

    //    worker.taskCo = null;
    //    //worker.PlaySplash(tile, isHill);
    //    worker.HideProgressTimeBar();
    //    worker.SetWorkAnimation(false);
    //    //worker.isBusy = false;
    //    //workerTaskManager.TurnOffCancelTask();
    //    RemoveRoadAtPosition(tile);
    //    world.RemoveWorkerWorkLocation(tile);

    //    foreach (Vector3Int loc in world.GetNeighborsFor(tile, MapWorld.State.EIGHTWAYINCREMENT))
    //    {
    //        if (world.IsTradeCenterOnTile(loc)/* || world.IsCityOnTile(loc)*/)
    //        {
    //            int i = 0;

    //            foreach (Vector3Int pos in world.GetNeighborsFor(loc, MapWorld.State.EIGHTWAYINCREMENT))
    //            {
    //                if (world.IsRoadOnTerrain(pos))
    //                {
    //                    i++;
    //                    break;
    //                }
    //            }

    //            if (i == 0)
    //                RemoveRoadAtPosition(loc);
    //        }
    //    }

    //    if (worker.MoreOrdersToFollow())
    //    {
    //        worker.RoadHighlightCheck();
    //        worker.BeginRoadRemoval();
    //    }
    //    else
    //    {
    //        world.mainPlayer.isBusy = false;
    //        worker.removing = false;
    //        if (world.mainPlayer.isSelected)
    //            workerTaskManager.TurnOffCancelTask();
    //    }
    //}

    public void RemoveRoadAtPosition(Vector3Int tile)
    {
        TerrainData td = world.GetTerrainDataAt(tile);
        td.ResetMovementCost();
        td.hasRoad = false;

        if (td.terrainData.type == TerrainType.Forest || td.terrainData.type == TerrainType.ForestHill)
            td.SwitchFromRoad();

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
        //world.RemoveAllRoadIcons(tile);

        foreach (Vector3Int neighbor in neighborsFourDirections)
            world.RemoveRoadLocation(tile + neighbor);
        foreach (Vector3Int neighbor in neighborsDiagFourDirections)
            world.RemoveRoadLocation(tile + neighbor);

        (List<(Vector3Int, bool, int[])> removedRoadNeighbors, int[] straightRoads, int[] diagRoads) = world.GetRoadNeighborsFor(tile, td.straightRiver);
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

        //for minimap roads
        MeshFilter meshFilterMinimap = roadHolderMinimap.GetComponent<MeshFilter>();
        meshFilterMinimap.mesh = new Mesh();
        meshFilterMinimap.mesh.CombineMeshes(combine);

        roadHolder.transform.gameObject.SetActive(true);
    }
}

public enum UtilityType
{
    Road,
    Water,
    Power
}
