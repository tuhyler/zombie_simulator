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
    private int width = 50, height = 50, yCoord = 3, desertPerc = 30, forestAndJunglePerc = 70, mountainPerc = 10, mountainousPerc = 80, 
        mountainRangeLength = 20, equatorDist = 10, riverPerc = 5, riverCountMin = 10, oceanRingDepth = 2;

    [Header("Perlin Noise Generation Parameters")]
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

    [Header("Tiles")]
    [SerializeField]
    private GameObject grassland; //separate here so that header isn't on every line
    [SerializeField]
    private GameObject forest, jungle, grasslandHill, forestHill, jungleHill, grasslandFloodPlain,
        desertFloodPlainVar1, desertFloodPlainVar2, desertFloodPlainVar3, desertFloodPlainVar4, desertFloodPlainVar5,
        desertVar0, desertVar1, desertVar2, desertVar3, desertVar4, desertVar5,
        desertHillVar0, desertHillVar1, desertHillVar2, desertHillVar3, desertHillVar4, desertHillVar5,
        grasslandMountain1, grasslandMountain2, grasslandMountain3, grasslandMountain4, desertMountain1, desertMountain2, desertMountain3, desertMountain4,
        swampVar0, swampVar1, swampVar2, swampVar3, swampVar4, swampVar5,
        riverSolo,  riverEnd, riverStraightVar1, riverStraightVar2, riverStraightVar3, riverCurve, riverThreeWay, riverFourWay,
        ocean, oceanStraightCoastVar1, oceanCurve, oceanCurveRiverLeft, oceanCurveRiverRight, oceanRiver, oceanThreeWay, oceanCorner, oceanKittyCorner, 
        oceanStraightCornerLeft, oceanStraightCornerRight, oceanRiverCornerLeft, oceanRiverCornerRight;

    [Header("Props")]
    [SerializeField]
    private GameObject grasslandProp01;
    [SerializeField]
    private GameObject grasslandProp02, grasslandProp03, desertProp01, desertFloodPlainProp01;

    [Header("Water")]
    [SerializeField]
    private GameObject water;

    [Header("Parents for Tiles")]
    [SerializeField]
    private Transform groundTiles;

    private GameObject[] grasslandMountains;
    private GameObject[] desertMountains;
    private GameObject[] grasslandProps;
    private GameObject[] desertProps;
    private GameObject[] desertFloodPlainProps;
    private GameObject[] riverStraights;

    int[] ugh = { 1, 2, 3 }; 

    private List<GameObject> allTiles = new();

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

        mainMap[new Vector3Int(46, 3, 3)] = ProceduralGeneration.grassland;
        //mainMap[new Vector3Int(44, 3, 3)] = ProceduralGeneration.grassland;
        //mainMap[new Vector3Int(16, 3, 25)] = ProceduralGeneration.grassland;
        //mainMap[new Vector3Int(16, 3, 23)] = ProceduralGeneration.grassland;

        mainMap = ProceduralGeneration.ConvertOceanToRivers(mainMap);


        foreach (Vector3Int position in mainMap.Keys)
        {            
            if (mainMap[position] == ProceduralGeneration.sea)
            {
                GameObject sea = ocean;
                Quaternion rotation = Quaternion.identity;
                int[] neighborDirectTerrainLoc = new int[4] { 0, 0, 0, 0 };
                int[] neighborTerrainLoc = new int[4] { 0, 0, 0, 0 };
                int[] neighborRiver = new int[4] { 0, 0, 0, 0 };
                int directNeighborCount = 0;
                int neighborCount = 0;
                int riverCount = 0;
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
                            neighborDirectTerrainLoc[i / 2] = 1;
                            if (mainMap[neighborPos] == ProceduralGeneration.river)
                            {
                                neighborRiver[i / 2] = 1;
                                riverCount++;
                            }
                        }
                        else
                        {
                            neighborCount++;
                            neighborTerrainLoc[i / 2] = 1;
                        }
                    }

                    i++;
                }

                if (directNeighborCount == 1)
                {
                    sea = riverCount == 0 ? oceanStraightCoastVar1 : oceanRiver;
                    int index = Array.FindIndex(neighborDirectTerrainLoc, x => x == 1);
                    rotation = Quaternion.Euler(0, index * 90, 0);

                    if (neighborCount >= 1)
                    {
                        int cornerOne = index + 1;
                        int cornerTwo = index + 2;
                        cornerOne = cornerOne == 4 ? 0 : cornerOne;
                        cornerTwo = cornerTwo == 4 ? 0 : cornerTwo;
                        cornerTwo = cornerTwo == 5 ? 1 : cornerTwo;

                        if (neighborTerrainLoc[cornerOne] == 1)
                            sea = riverCount == 0 ? oceanStraightCornerLeft : oceanRiverCornerLeft;
                        else if (neighborTerrainLoc[cornerTwo] == 1)
                            sea = riverCount == 0 ? oceanStraightCornerRight : oceanRiverCornerRight;
                    }
                }
                else if (directNeighborCount == 2)
                {
                    if (position == new Vector3Int(19,3,16))
                        Debug.Log("");
                    
                    sea = oceanCurve;
                    int[] holder = new int[2];

                    for (int j = 0; j < 2; j++)
                    {
                        holder[j] = Array.FindIndex(neighborDirectTerrainLoc, x => x == 1);
                        neighborDirectTerrainLoc[holder[j]] = 0; //setting to zero so it doesn't find the first one again
                    }

                    int max = Mathf.Max(holder);
                    int min = Mathf.Min(holder);

                    rotation = max - min == 1 ? Quaternion.Euler(0, min * 90, 0) : Quaternion.Euler(0, max * 90, 0);

                    if (riverCount == 1)
                    {
                        int riverIndex = Array.FindIndex(neighborRiver, x => x == 1);

                        if (riverIndex == max)
                            sea = max - min == 1 ? oceanCurveRiverLeft : oceanCurveRiverRight;
                        else if (riverIndex == min)
                            sea = max - min == 1 ? oceanCurveRiverRight : oceanCurveRiverLeft;
                    }
                    else if (riverCount == 2)
                    {
                        sea = oceanThreeWay;
                    }
                }
                else if (directNeighborCount == 3)
                {
                    sea = ocean;
                    rotation = Quaternion.identity;
                }
                else if (directNeighborCount == 4)
                {
                    sea = ocean;
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
                    else if (neighborCount == 2)
                    {
                        sea = oceanKittyCorner;
                        int index = Array.FindIndex(neighborTerrainLoc, x => x == 1);
                        rotation = Quaternion.Euler(0, index * 90, 0);
                    }
                    else if (neighborCount == 3)
                    {
                        sea = ocean;
                        int index = Array.FindIndex(neighborTerrainLoc, x => x == 0);
                        rotation = Quaternion.Euler(0, index * 90, 0);
                    }
                }

                GenerateTile(sea, position, rotation);
                //GenerateTile(ocean, position, Quaternion.identity);
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
                GameObject grasslandMountain = grasslandMountains[random.Next(0,4)];
                
                GenerateTile(grasslandMountain, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0));
            }
            else if (mainMap[position] == ProceduralGeneration.desertMountain)
            {
                GameObject desertMountain = desertMountains[random.Next(0, 4)];

                GenerateTile(desertMountain, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0));
            }
            else if (mainMap[position] == ProceduralGeneration.grassland)
            {                
                GameObject newTile = GenerateTile(grassland, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0));

                if (random.Next(0, 10) < 7)
                    AddProp(random, newTile, grasslandProps);
            }
            else if (mainMap[position] == ProceduralGeneration.desert)
            {
                FadeAndRotateTerrain(random, rotate, mainMap, position, true, false, desertVar0, desertVar1, desertVar2, desertVar3,
                    desertVar4, desertVar5, out Quaternion rotation, out GameObject desert);

                GameObject newTile = GenerateTile(desert, position, rotation);

                if (random.Next(0, 10) < 3)
                    AddProp(random, newTile, desertProps);
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

                if (river == riverStraightVar1)
                    river = riverStraights[random.Next(0, riverStraights.Length)];

                GenerateTile(river, position, rotation);
            }
            else if (mainMap[position] == ProceduralGeneration.grasslandFloodPlain)
            {
                GameObject newTile = GenerateTile(grasslandFloodPlain, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0));

                if (random.Next(0, 10) < 7)
                    AddProp(random, newTile, grasslandProps);
            }
            else if (mainMap[position] == ProceduralGeneration.desertFloodPlain)
            {
                FadeAndRotateTerrain(random, rotate, mainMap, position, true, false, desertVar0, desertFloodPlainVar1, desertFloodPlainVar2,
                    desertFloodPlainVar3, desertFloodPlainVar4, desertFloodPlainVar5, out Quaternion rotation, out GameObject desertFloodPlain);

                GameObject newTile = GenerateTile(desertFloodPlain, position, rotation);

                if (random.Next(0, 10) < 5)
                    AddProp(random, newTile, desertFloodPlainProps);
            }
            else
            {
                GenerateTile(grassland, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0));
            }
        }

        //Finish it all of by placing water
        Vector3 waterLoc = new Vector3((width) / 2, yCoord - .02f, (height) / 2);
        GameObject water = Instantiate(this.water, waterLoc, Quaternion.identity);
        water.transform.SetParent(groundTiles.transform, false);
        allTiles.Add(water);
        water.transform.localScale = new Vector3((width + oceanRingDepth * 2)/10f, 1, (height + oceanRingDepth * 2)/10f);
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

            if (desert) //for desert
            {
                if (mainMap[neighborPos] == ProceduralGeneration.grassland || mainMap[neighborPos] == ProceduralGeneration.forest ||
                    mainMap[neighborPos] == ProceduralGeneration.jungle || mainMap[neighborPos] == ProceduralGeneration.jungleHill ||
                    mainMap[neighborPos] == ProceduralGeneration.forestHill || mainMap[neighborPos] == ProceduralGeneration.swamp ||
                    mainMap[neighborPos] == ProceduralGeneration.grasslandHill || mainMap[neighborPos] == ProceduralGeneration.grasslandMountain ||
                    /*mainMap[neighborPos] == ProceduralGeneration.river || */mainMap[neighborPos] == ProceduralGeneration.grasslandFloodPlain /*||
                    mainMap[neighborPos] == ProceduralGeneration.sea*/)
                {
                    neighborCount++;
                    neighborTerrainLoc[i] = 1;
                }
            }
            else if (!desert && !river) //for swamp
            {
                if (!mainMap.ContainsKey(neighborPos) || mainMap[neighborPos] == ProceduralGeneration.grassland || 
                    mainMap[neighborPos] == ProceduralGeneration.forest ||mainMap[neighborPos] == ProceduralGeneration.jungle || 
                    mainMap[neighborPos] == ProceduralGeneration.jungleHill || mainMap[neighborPos] == ProceduralGeneration.forestHill || 
                    mainMap[neighborPos] == ProceduralGeneration.desertHill || mainMap[neighborPos] == ProceduralGeneration.desert ||
                    mainMap[neighborPos] == ProceduralGeneration.grasslandHill || mainMap[neighborPos] == ProceduralGeneration.grasslandMountain ||
                    mainMap[neighborPos] == ProceduralGeneration.grasslandFloodPlain || mainMap[neighborPos] == ProceduralGeneration.desertFloodPlain /*||
                    mainMap[neighborPos] == ProceduralGeneration.river || mainMap[neighborPos] == ProceduralGeneration.sea*/)
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

    private void AddProp(System.Random random, GameObject terrain, GameObject[] propArray)
    {
        TerrainData td = terrain.GetComponent<TerrainData>();
        Quaternion rotation = Quaternion.Euler(0, random.Next(0, 4) * 90, 0);
        rotation = Quaternion.identity;

        GameObject newProp = Instantiate(propArray[random.Next(0, propArray.Length)], new Vector3Int(0, 0, 0), rotation);
        newProp.transform.SetParent(td.prop, false);
    }

    private void PopulateArrays()
    {
        grasslandMountains = new GameObject[4] { grasslandMountain1, grasslandMountain2, grasslandMountain3, grasslandMountain4 };
        desertMountains = new GameObject[4] { desertMountain1, desertMountain2, desertMountain3, desertMountain4 };
        grasslandProps = new GameObject[3] { grasslandProp01, grasslandProp02, grasslandProp03 };
        desertProps = new GameObject[1] { desertProp01 };
        desertFloodPlainProps = new GameObject[1] { desertFloodPlainProp01 };
        riverStraights = new GameObject[3] { riverStraightVar1, riverStraightVar2, riverStraightVar3 };
    }

    private GameObject GenerateTile(/*float perlinCoord, */GameObject tile, Vector3Int position, Quaternion rotation)
    {
        GameObject newTile = Instantiate(tile, position, rotation);
        newTile.transform.SetParent(groundTiles.transform, false);
        allTiles.Add(newTile);

        return newTile;
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
