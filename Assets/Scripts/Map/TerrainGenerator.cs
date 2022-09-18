using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    [Header("General Map Parameters")]
    [SerializeField]
    private int seed = 4;
    [SerializeField]
    private int yCoord = 3, desertPerc = 30, forestAndJunglePerc = 70, mountainPerc = 10, mountainousPerc = 80, 
        mountainRangeLength = 20, equatorDist = 10, riverPerc = 5, riverCountMin = 10, oceanRingDepth = 2;

    [Header("Perlin Noise Generation Parameters")]
    //[SerializeField]
    //protected Vector3Int startPosition = Vector3Int.zero;
    [SerializeField]
    private int width = 50;
    [SerializeField]
    private int height = 50;
    [SerializeField]
    private float scale = 8;
    [SerializeField]
    private int octaves = 3;
    [Range(0f, 1f)]
    [SerializeField]
    private float persistance = 0.5f;
    [SerializeField]
    private float lacunarity = 1.01f;
    [SerializeField]
    private float offset;

    [Header("Cellular Automata Generation Parameters")]
    [SerializeField]
    private int threshold = 5;
    [SerializeField]
    private int iterations = 15;
    [SerializeField]
    private int randomFillPercent = 60;

    //[Header("Random Walk")]
    //[SerializeField]
    //private int iterationsRW = 10;
    //[SerializeField]
    //private int walkLength = 10;
    //[SerializeField]
    //private bool iterationStartRandom = true;

    [Header("Tiles")]
    [SerializeField]
    private GameObject grassland; //separate here so that header isn't on every line
    [SerializeField]
    private GameObject forest, jungle, grasslandHill, forestHill, jungleHill, grasslandFloodPlain,
        desertFloodPlainVar0, desertFloodPlainVar1, desertFloodPlainVar2, desertFloodPlainVar3, desertFloodPlainVar4, desertFloodPlainVar5,
        desertVar0, desertVar1, desertVar2, desertVar3, desertVar4, desertVar5,
        desertHillVar0, desertHillVar1, desertHillVar2, desertHillVar3, desertHillVar4, desertHillVar5,
        grasslandMountain1, grasslandMountain2, grasslandMountain3, grasslandMountain4, desertMountain1, desertMountain2, desertMountain3, desertMountain4,
        swampVar0, swampVar1, swampVar2, swampVar3, swampVar4, swampVar5,
        riverSolo,  riverEnd, riverStraightVar1, riverStraightVar2, riverStraightVar3, riverCurve, riverThreeWay, riverFourWay,
        ocean, oceanStraightCoastVar1, oceanCurve, oceanRiver, oceanThreeWay, oceanCorner;

    [Header("Parents for Tiles")]
    [SerializeField]
    private Transform groundTiles;

    private GameObject[] grasslandMountainArray;
    private GameObject[] desertMountainArray;

    int[] ugh = { 1, 2, 3 }; 

    private List<GameObject> allTiles = new();

    //[SerializeField]
    //protected Vector3Int startPosition = Vector3Int.zero;

    public bool autoUpdate;

    public void GenerateMap()
    {
        RunProceduralGeneration();
    }

    public void RemoveMap()
    {
        RemoveAllTiles();
    }

    protected void RunProceduralGeneration()
    {
        RemoveAllTiles();

        System.Random random = new System.Random(seed);
        int[] rotate = new int[4] { 0, 90, 180, 270 };

        PopulateArrays();

        //GameObject tile = null;

        //HashSet<float> noise = ProceduralGeneration.PerlinNoiseGenerator(width, height, scale, octaves, persistance, lacunarity);
        Dictionary<Vector3Int, int> mainMap = 
            ProceduralGeneration.GenerateCellularAutomata(threshold, width, height, iterations, randomFillPercent, seed, yCoord);

        Dictionary<Vector3Int, float> noise = ProceduralGeneration.PerlinNoiseGenerator(mainMap,
            scale, octaves, persistance, lacunarity, seed, offset);

        mainMap = ProceduralGeneration.GenerateTerrain(mainMap, noise, 0.45f, 0.65f, desertPerc, forestAndJunglePerc,
            width, height, yCoord, equatorDist, seed);

        Dictionary<Vector3Int, int> mountainMap = ProceduralGeneration.GenerateMountainRanges(mainMap, mountainPerc, mountainousPerc, mountainRangeLength, 
            width, height, yCoord, seed);

        mountainMap = ProceduralGeneration.GenerateRivers(mountainMap, riverPerc, riverCountMin, seed);

        mainMap = ProceduralGeneration.MergeMountainTerrain(mainMap, mountainMap);

        mainMap = ProceduralGeneration.AddOceanRing(mainMap, width, height, yCoord, oceanRingDepth);

        //List<Vector3Int> landPositions = new();

        foreach (Vector3Int position in mainMap.Keys)
        {            
            if (mainMap[position] == ProceduralGeneration.sea)
            {
                GameObject sea;
                Quaternion rotation;
                int[] neighborDirectTerrainLoc = new int[4] { 0, 0, 0, 0 }; 
                int[] neighborTerrainLoc = new int[4] { 0, 0, 0, 0 };
                int[] neighborDirectTerrainType = new int[4];
                int[] neighborTerrainType = new int[4];
                int directNeighborCount = 0;
                int neighborCount = 0;
                int i = 0;

                foreach (Vector3Int neighbor in ProceduralGeneration.neighborsEightDirections)
                {
                    Vector3Int neighborPos = neighbor + position;

                    if (!mainMap.ContainsKey(neighborPos))
                        continue;

                    if (mainMap[neighborPos] != ProceduralGeneration.sea)
                    {
                        if (i % 2 == 0)
                        {
                            directNeighborCount++;
                            neighborDirectTerrainLoc[i] = 1;
                            neighborDirectTerrainType[i] = mainMap[neighborPos];
                        }

                        neighborCount++;
                        neighborTerrainLoc[i] = 1;
                        neighborTerrainType[i] = mainMap[neighborPos];
                    }

                    i++;
                }

                if (directNeighborCount == 1)
                {
                    int index = Array.FindIndex(neighborTerrainLoc, x => x == 1);
                    if (neighborTerrainType[index] == ProceduralGeneration.river)
                    {
                        sea = oceanRiver;
                    }
                    else
                    {
                        sea = oceanStraightCoastVar1;
                    }

                    rotation = Quaternion.Euler(0, index * 90, 0);
                }
                else if (directNeighborCount == 2)
                {
                    int index = 0;
                    int totalIndex = 0;
                    //int rotationFactor;

                    for (int j = 0; j < 2; j++)
                    {
                        index = Array.FindIndex(neighborTerrainLoc, x => x == 1);
                        neighborTerrainLoc[index] = 0; //setting to zero so it doesn't find the first one again
                        totalIndex += index;
                    }

                    if (totalIndex % 2 == 0) //for straight across
                    {
                        sea = riverStraightVar1;
                        rotation = Quaternion.Euler(0, (index % 2) * 90, 0);
                    }
                    else //for corners
                    {
                        int rotationFactor = totalIndex / 2;
                        if (totalIndex == 3 && index == 3)
                            rotationFactor = 3;
                        sea = oceanCurve;
                        rotation = Quaternion.Euler(0, rotationFactor * 90, 0);
                    }
                }
                else if (directNeighborCount == 3)
                {
                    sea = riverEnd;
                    int index = Array.FindIndex(neighborTerrainLoc, x => x == 0);
                    rotation = Quaternion.Euler(0, index * 90, 0);
                }
                else if (directNeighborCount == 4)
                {
                    sea = riverSolo;
                    rotation = Quaternion.identity;
                }
                else
                {
                    if (neighborCount == 1)
                    {
                        sea = oceanCorner;
                        int index = Array.FindIndex(neighborTerrainLoc, x => x == 1);
                        rotation = Quaternion.Euler(0, index * 90, 0);
                    }
                    else
                    {
                        sea = ocean;
                        rotation = Quaternion.identity;
                    }
                }

                GenerateTile(sea, position, rotation);
            }
            else if (mainMap[position] == ProceduralGeneration.grasslandHill)
            {
                GenerateTile(grasslandHill, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0));
            }
            else if (mainMap[position] == ProceduralGeneration.desertHill)
            {
                FadeAndRotateTerrain(random, rotate, mainMap, position, true, false, desertHillVar0, desertHillVar1, desertHillVar2, desertHillVar3,
                    desertHillVar4, desertHillVar5, out Quaternion rotation, out GameObject desertHill);

                GenerateTile(desertHill, position, rotation);
            }
            else if (mainMap[position] == ProceduralGeneration.forestHill)
            {
                GenerateTile(forestHill, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0));
            }
            else if (mainMap[position] == ProceduralGeneration.jungleHill)
            {
                GenerateTile(jungleHill, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0));
            }
            else if (mainMap[position] == ProceduralGeneration.grasslandMountain)
            {
                GameObject grasslandMountain = grasslandMountainArray[random.Next(0,4)];
                
                GenerateTile(grasslandMountain, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0));
            }
            else if (mainMap[position] == ProceduralGeneration.desertMountain)
            {
                GameObject desertMountain = desertMountainArray[random.Next(0, 4)];

                GenerateTile(desertMountain, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0));
            }
            else if (mainMap[position] == ProceduralGeneration.grassland)
            {
                GenerateTile(grassland, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0));
            }
            else if (mainMap[position] == ProceduralGeneration.desert)
            {
                FadeAndRotateTerrain(random, rotate, mainMap, position, true, false, desertVar0, desertVar1, desertVar2, desertVar3,
                    desertVar4, desertVar5, out Quaternion rotation, out GameObject desert);

                GenerateTile(desert, position, rotation);
            }
            else if (mainMap[position] == ProceduralGeneration.forest)
            {
                GenerateTile(forest, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0));
            }
            else if (mainMap[position] == ProceduralGeneration.jungle)
            {
                GenerateTile(jungle, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0));
            }
            else if (mainMap[position] == ProceduralGeneration.swamp)
            {
                FadeAndRotateTerrain(random, rotate, mainMap, position, false, false, swampVar0, swampVar1, swampVar2, swampVar3,
                    swampVar4, swampVar5, out Quaternion rotation, out GameObject swamp);

                GenerateTile(swamp, position, rotation);
            }
            else if (mainMap[position] == ProceduralGeneration.river) //river is all one terrain now
            {
                FadeAndRotateTerrain(random, rotate, mainMap, position, false, true, riverSolo, riverEnd, riverCurve, riverThreeWay,
                    riverFourWay, riverStraightVar1, out Quaternion rotation, out GameObject river);

                GenerateTile(river, position, rotation);
            }
            //else if (mainMap[position] == ProceduralGeneration.desertRiver)
            //{
            //    GenerateTile(riverDesertStraight, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0));
            //}
            else
            {
                GenerateTile(grassland, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0));
            }
        }
    }

    //ugly method to make above method cleaner
    private void FadeAndRotateTerrain(System.Random random, int[] rotate, Dictionary<Vector3Int, int> mainMap, Vector3Int position, bool desert, bool river,
        GameObject go0, GameObject go1, GameObject go2, GameObject go3, GameObject go4, GameObject go5, out Quaternion rotation, out GameObject terrain)
    {
        int[] neighborTerrainLoc = new int[4] { 0, 0, 0, 0 };
        int neighborCount = 0;
        int i = 0;

        foreach (Vector3Int neighbor in ProceduralGeneration.neighborsFourDirections)
        {
            Vector3Int neighborPos = neighbor + position;

            if (desert && !mainMap.ContainsKey(neighborPos))
                continue;

            if (desert)
            {
                if (mainMap[neighborPos] == ProceduralGeneration.grassland || mainMap[neighborPos] == ProceduralGeneration.forest ||
                    mainMap[neighborPos] == ProceduralGeneration.jungle || mainMap[neighborPos] == ProceduralGeneration.jungleHill ||
                    mainMap[neighborPos] == ProceduralGeneration.forestHill || mainMap[neighborPos] == ProceduralGeneration.swamp ||
                    mainMap[neighborPos] == ProceduralGeneration.grasslandHill || mainMap[neighborPos] == ProceduralGeneration.grasslandMountain ||
                    mainMap[neighborPos] == ProceduralGeneration.river)
                {
                    neighborCount++;
                    neighborTerrainLoc[i] = 1;
                }
            }
            else if (!desert && !river)
            {
                if (!mainMap.ContainsKey(neighborPos) || mainMap[neighborPos] == ProceduralGeneration.grassland || 
                    mainMap[neighborPos] == ProceduralGeneration.forest ||mainMap[neighborPos] == ProceduralGeneration.jungle || 
                    mainMap[neighborPos] == ProceduralGeneration.jungleHill || mainMap[neighborPos] == ProceduralGeneration.forestHill || 
                    mainMap[neighborPos] == ProceduralGeneration.desertHill || mainMap[neighborPos] == ProceduralGeneration.desert ||
                    mainMap[neighborPos] == ProceduralGeneration.grasslandHill || mainMap[neighborPos] == ProceduralGeneration.grasslandMountain ||
                    mainMap[neighborPos] == ProceduralGeneration.river || mainMap[neighborPos] == ProceduralGeneration.sea)
                {
                    neighborCount++;
                    neighborTerrainLoc[i] = 1;
                }
            }
            else if (!desert && river)
            {
                if (!mainMap.ContainsKey(neighborPos) || 
                    mainMap[neighborPos] == ProceduralGeneration.river || mainMap[neighborPos] == ProceduralGeneration.sea)
                {
                    neighborCount++;
                    neighborTerrainLoc[i] = 1;
                }
            }

            i++;
        }

        if (neighborCount == 1)
        {
            terrain = go1;
            int index = Array.FindIndex(neighborTerrainLoc, x => x == 1);
            rotation = Quaternion.Euler(0, index * 90, 0);
        }
        else if (neighborCount == 2)
        {
            int index = 0;
            int totalIndex = 0;
            //int rotationFactor;

            for (int j = 0; j < 2; j++)
            {
                index = Array.FindIndex(neighborTerrainLoc, x => x == 1);
                neighborTerrainLoc[index] = 0; //setting to zero so it doesn't find the first one again
                totalIndex += index;
            }

            if (totalIndex % 2 == 0) //for straight across
            {
                terrain = go5;
                rotation = Quaternion.Euler(0, (index % 2) * 90, 0);
            }
            else //for corners
            {
                int rotationFactor = totalIndex / 2;
                if (totalIndex == 3 && index == 3)
                    rotationFactor = 3;
                terrain = go2;
                rotation = Quaternion.Euler(0, rotationFactor * 90, 0);
            }
        }
        else if (neighborCount == 3)
        {
            terrain = go3;
            int index = Array.FindIndex(neighborTerrainLoc, x => x == 0);
            rotation = Quaternion.Euler(0, index * 90, 0);
        }
        else if (neighborCount == 4)
        {
            terrain = go4;
            rotation = Quaternion.Euler(0, rotate[random.Next(0, 4)], 0);
        }
        else
        {
            terrain = go0;
            rotation = Quaternion.Euler(0, rotate[random.Next(0, 4)], 0);
        }
    }

    private void PopulateArrays()
    {
        grasslandMountainArray = new GameObject[4] { grasslandMountain1, grasslandMountain2, grasslandMountain3, grasslandMountain4 };
        desertMountainArray = new GameObject[4] { desertMountain1, desertMountain2, desertMountain3, desertMountain4 };
    }

    private void GenerateTile(/*float perlinCoord, */GameObject tile, Vector3Int position, Quaternion rotation)
    {
        GameObject newTile = Instantiate(tile, position, rotation);
        newTile.transform.SetParent(groundTiles.transform, false);
        allTiles.Add(newTile);

        //temporary, just to see the values
        //newTile.GetComponent<TerrainData>().numbers.text = Math.Round(perlinCoord, 3).ToString();
    }

    protected void RemoveAllTiles()
    {
        foreach (GameObject tile in allTiles)
        {
            DestroyImmediate(tile);
        }
    }
}


//public struct TerrainID
//{
//    public int landPlaceholder;
//    public int sea;
//    public int grasslandHill;
//    public int terrain;
//    public int forestHill;
//    public int jungleHill;
//    public int grasslandMountain;
//    public int desertMountain;
//    public int grassland;
//    public int desert;
//    public int forest;
//    public int jungle;
//    public int swamp;
//}
