using System;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;
    
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

    //[Header("Tiles")]
    //[SerializeField]
    //private GameObject grassland; //separate here so that header isn't on every line
    //[SerializeField]
    //private GameObject grasslandHill, grasslandFloodPlain, desertFloodPlain, desert, desertHill, swamp,
    //    grasslandMountain1, grasslandMountain2, grasslandMountain3, grasslandMountain4, desertMountain1, desertMountain2, desertMountain3, desertMountain4,
    //    riverEnd, riverStraightVar1, riverStraightVar2, riverStraightVar3, riverCurve, riverThreeWay, riverFourWay,
    //    ocean, oceanStraightCoastVar1, oceanCurve, oceanCurveDiagonal, oceanCurveRiverLeft, oceanCurveRiverRight, oceanRiver, oceanThreeWay, 
    //    oceanCorner, oceanKittyCorner, oceanStraightCornerLeft, oceanStraightCornerRight, oceanRiverCornerLeft, oceanRiverCornerRight;

    //[Header("Props")]
    //[SerializeField]
    //private GameObject grasslandProp01;
    //[SerializeField]
    //private GameObject grasslandProp02, grasslandProp03, desertProp01, desertProp02, desertFloodPlainProp01, desertFloodPlainProp02, forestPropVar01,
    //    junglePropVar01, swampPropVar01;

    [Header("Water")]
    [SerializeField]
    private GameObject water;

    [Header("Parents for Tiles")]
    [SerializeField]
    private Transform groundTiles;

    [Header("Terrain Data SO")]
    [SerializeField]
    private TerrainDataSO coastSO;
    [SerializeField]
    private TerrainDataSO desertFloodPlainsSO, desertHillResourceSO, desertHillSO, desertMountainSO, desertResourceSO, desertSO, forestHillSO, forestSO, 
        grasslandFloodPlainsSO, grasslandHillResourceSO, grasslandHillSO, grasslandMountainSO, grasslandResourceSO, grasslandSO, 
        jungleHillSO, jungleSO, riverSO, seaIntersectionSO, seaSO, swampSO;

    //private GameObject[] grasslandMountains, desertMountains, grasslandProps, forestProps, jungleProps, swampProps, desertProps,
    //    desertFloodPlainProps, riverStraights, oceanCurves;

    private List<GameObject> allTiles = new();

    public bool autoUpdate, explored;

    private Dictionary<Vector3Int, TerrainData> terrainDict = new();

    public void GenerateMap()
    {
        terrainDict.Clear();
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

        //PopulateArrays();

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

        //mainMap[new Vector3Int(22, 3, 5)] = ProceduralGeneration.grassland;
        //mainMap[new Vector3Int(22, 3, 3)] = ProceduralGeneration.grassland;
        //mainMap[new Vector3Int(23, 3, 4)] = ProceduralGeneration.grassland;
        //mainMap[new Vector3Int(16, 3, 25)] = ProceduralGeneration.grasslandVar00;
        //mainMap[new Vector3Int(16, 3, 23)] = ProceduralGeneration.grasslandVar00;

        mainMap = ProceduralGeneration.ConvertOceanToRivers(mainMap);


        foreach (Vector3Int position in mainMap.Keys)
        {
            if (mainMap[position] == ProceduralGeneration.sea)
            {
                int prefabIndex = 0;
                GameObject sea = seaSO.prefabs[0];
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
                    if (riverCount == 0)
                    {
                        sea = coastSO.prefabs[3];
                        prefabIndex = 3;
					}
                    else
                    {
                        sea = seaIntersectionSO.prefabs[2];
                        prefabIndex = 2;
					}
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
                        {
                            if (riverCount == 0)
                            {
                                sea = coastSO.prefabs[4];
                                prefabIndex = 4;
							}
                            else
                            {
                                sea = seaIntersectionSO.prefabs[3];
                                prefabIndex = 3;
							}
                        }
                        else if (neighborTerrainLoc[cornerTwo] == 1)
                        {
                            if (riverCount == 0)
                            {
                                sea = coastSO.prefabs[5];
                                prefabIndex = 5;
							}
                            else
                            {
                                sea = seaIntersectionSO.prefabs[4];
                                prefabIndex = 4;
							}
                        }
                    }
                }
                else if (directNeighborCount == 2)
                {
                    sea = coastSO.prefabs[0];
					prefabIndex = 0;
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
                        {
                            if (max - min == 1)
                            {
                                sea = coastSO.prefabs[1];
                                prefabIndex = 1;
							}
                            else
                            {
                                sea = coastSO.prefabs[2];
                                prefabIndex = 2;
							}
                        }
                        else if (riverIndex == min)
                        {
                            if (max - min == 1)
                            {
                                sea = coastSO.prefabs[2];
                                prefabIndex = 2;
							}
                            else
                            {
                                sea = coastSO.prefabs[1];
                                prefabIndex = 1;
							}
                        }
                    }
                    else if (riverCount == 2)
                    {
                        sea = seaIntersectionSO.prefabs[5];
						prefabIndex = 5;
					}
                }
                else if (directNeighborCount == 3)
                {
                    sea = seaSO.prefabs[0];
					prefabIndex = 0;
					rotation = Quaternion.identity;
                }
                else if (directNeighborCount == 4)
                {
                    sea = seaSO.prefabs[0];
					prefabIndex = 0;
					rotation = Quaternion.identity;
                }
                else
                {
                    if (neighborCount == 1)
                    {
                        sea = seaIntersectionSO.prefabs[0];
						prefabIndex = 0;
						int index = Array.FindIndex(neighborTerrainLoc, x => x == 1);
                        rotation = Quaternion.Euler(0, index * 90, 0);
                    }
                    else if (neighborCount == 2)
                    {
                        sea = seaIntersectionSO.prefabs[1];
						prefabIndex = 1;
						int index = Array.FindIndex(neighborTerrainLoc, x => x == 1);
                        rotation = Quaternion.Euler(0, index * 90, 0);
                    }
                    else if (neighborCount == 3)
                    {
                        sea = seaSO.prefabs[0];
                        prefabIndex = 0;
                        int index = Array.FindIndex(neighborTerrainLoc, x => x == 0);
                        rotation = Quaternion.Euler(0, index * 90, 0);
                    }
                }

                GenerateTile(sea, position, rotation, prefabIndex);
                //GenerateTile(ocean, position, Quaternion.identity);
            }
            else if (mainMap[position] == ProceduralGeneration.grasslandHill)
            {
                //FadeAndRotateTerrain(random, rotate, mainMap, position, false, false, true, grasslandHillVar00, grasslandHillVar01, grasslandHillVar02,
                //    grasslandHillVar03, grasslandHillVar04, grasslandHillVar05, out Quaternion rotation, out GameObject grasslandHill);

                //grasslandHill.tag = "Hill";
                GenerateTile(grasslandHillSO.prefabs[0], position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
            }
            else if (mainMap[position] == ProceduralGeneration.desertHill)
            {
                //FadeAndRotateTerrain(random, rotate, mainMap, position, true, false, false, desertHillVar0, desertHillVar1, desertHillVar2, desertHillVar3,
                //    desertHillVar4, desertHillVar5, out Quaternion rotation, out GameObject desertHill);

                //desertHill.tag = "Hill";
                GenerateTile(desertHillSO.prefabs[0], position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
            }
            else if (mainMap[position] == ProceduralGeneration.forestHill)
            {
                //FadeAndRotateTerrain(random, rotate, mainMap, position, false, false, true, grasslandHillVar00, grasslandHillVar01, grasslandHillVar02,
                //    grasslandHillVar03, grasslandHillVar04, grasslandHillVar05, out Quaternion rotation, out GameObject forestHill);

                GameObject forestHill = grasslandHillSO.prefabs[0];
                forestHill.tag = "Forest Hill";
                TerrainData td = GenerateTile(forestHill, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
                td.terrainData = forestHillSO;

                AddProp(random, td, forestHillSO.decors);
            }
            else if (mainMap[position] == ProceduralGeneration.jungleHill)
            {
                //FadeAndRotateTerrain(random, rotate, mainMap, position, false, false, true, grasslandHillVar00, grasslandHillVar01, grasslandHillVar02,
                //    grasslandHillVar03, grasslandHillVar04, grasslandHillVar05, out Quaternion rotation, out GameObject jungleHill);

                GameObject jungleHill = grasslandHillSO.prefabs[0];
                jungleHill.tag = "Forest Hill";
                TerrainData td = GenerateTile(jungleHill, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
                td.terrainData = jungleHillSO;

                AddProp(random, td, jungleHillSO.decors);
            }
            else if (mainMap[position] == ProceduralGeneration.grasslandMountain)
            {
                int prefabIndex = random.Next(0, grasslandMountainSO.prefabs.Count);
				GameObject grasslandMountain = grasslandMountainSO.prefabs[prefabIndex];

                GenerateTile(grasslandMountain, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), prefabIndex);
            }
            else if (mainMap[position] == ProceduralGeneration.desertMountain)
            {
                int prefabIndex = random.Next(0, desertMountainSO.prefabs.Count);
				GameObject desertMountain = desertMountainSO.prefabs[prefabIndex];

                GenerateTile(desertMountain, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), prefabIndex);
            }
            else if (mainMap[position] == ProceduralGeneration.grassland)
            {
                //FadeAndRotateTerrain(random, rotate, mainMap, position, false, false, true, grasslandVar00, grasslandVar01, grasslandVar02,
                //   grasslandVar03, grasslandVar04, grasslandVar05, out Quaternion rotation, out GameObject grassland);

                TerrainData td = GenerateTile(grasslandSO.prefabs[0], position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);

                //if (random.Next(0, 10) < 3)
                AddProp(random, td, grasslandSO.decors);
            }
            else if (mainMap[position] == ProceduralGeneration.desert)
            {
                //FadeAndRotateTerrain(random, rotate, mainMap, position, true, false, false, desertVar0, desertVar1, desertVar2, desertVar3,
                //    desertVar4, desertVar5, out Quaternion rotation, out GameObject desert);

                TerrainData td = GenerateTile(desertSO.prefabs[0], position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);

                //if (random.Next(0, 10) < 3)
                AddProp(random, td, desertSO.decors);
            }
            else if (mainMap[position] == ProceduralGeneration.forest)
            {
                //FadeAndRotateTerrain(random, rotate, mainMap, position, false, false, true, grasslandVar00, grasslandVar01, grasslandVar02,
                //    grasslandVar03, grasslandVar04, grasslandVar05, out Quaternion rotation, out GameObject forest);

                GameObject forest = grasslandSO.prefabs[0];
                forest.tag = "Forest";
                TerrainData td = GenerateTile(forest, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
                td.terrainData = forestSO;

                AddProp(random, td, forestSO.decors);
            }
            else if (mainMap[position] == ProceduralGeneration.jungle)
            {
                //FadeAndRotateTerrain(random, rotate, mainMap, position, false, false, true, grasslandVar00, grasslandVar01, grasslandVar02,
                //    grasslandVar03, grasslandVar04, grasslandVar05, out Quaternion rotation, out GameObject jungle);

                GameObject jungle = grasslandSO.prefabs[0];
                jungle.tag = "Forest";
                TerrainData td = GenerateTile(jungle, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
                td.terrainData = jungleSO;

                AddProp(random, td, jungleSO.decors);
            }
            else if (mainMap[position] == ProceduralGeneration.swamp)
            {
                //FadeAndRotateTerrain(random, rotate, mainMap, position, false, false, false, swampVar0, swampVar1, swampVar2,
                //    swampVar3, swampVar4, swampVar5, out Quaternion rotation, out GameObject swamp);

                //swamp.tag = "Forest";
                GenerateTile(swampSO.prefabs[0], position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);

                //AddProp(random, newTile, swampProps, swampSO);
            }
            else if (mainMap[position] == ProceduralGeneration.river)
            {
                RotateRiverTerrain(random, rotate, mainMap, position, /*false, true, false, */out Quaternion rotation, out int riverInt);

                GameObject river;
                if (riverInt == 3)
                    riverInt += random.Next(0, 3);

                if (riverInt == 9999)
                {
                    river = grasslandSO.prefabs[0];
                    riverInt = 0;
                }
                else
                {
                    river = riverSO.prefabs[riverInt];
                }

                GenerateTile(river, position, rotation, riverInt);
            }
            else if (mainMap[position] == ProceduralGeneration.grasslandFloodPlain)
            {
                //FadeAndRotateTerrain(random, rotate, mainMap, position, false, false, true, grasslandFloodPlainVar00, grasslandFloodPlainVar01,
                //    grasslandFloodPlainVar02, grasslandFloodPlainVar03, grasslandFloodPlainVar04, grasslandFloodPlainVar05, 
                //    out Quaternion rotation, out GameObject grasslandFloodPlain);

                TerrainData td = GenerateTile(grasslandFloodPlainsSO.prefabs[0], position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);

                //if (random.Next(0, 10) < 3)
                AddProp(random, td, grasslandFloodPlainsSO.decors);
            }
            else if (mainMap[position] == ProceduralGeneration.desertFloodPlain)
            {
                //FadeAndRotateTerrain(random, rotate, mainMap, position, true, false, false, desertFloodPlainVar0, desertFloodPlainVar1, desertFloodPlainVar2,
                //    desertFloodPlainVar3, desertFloodPlainVar4, desertFloodPlainVar5, out Quaternion rotation, out GameObject desertFloodPlain);

                TerrainData td = GenerateTile(desertFloodPlainsSO.prefabs[0], position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);

                //if (random.Next(0, 10) < 3)
                AddProp(random, td, desertFloodPlainsSO.decors);
            }
            else
            {
                GenerateTile(grasslandSO.prefabs[0], position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
            }
        }

        //foreach (Vector3Int tile in terrainDict.Keys)
        //{
        //    world.ConfigureUVs(terrainDict[tile], this);
        //}

        //Finish it all of by placing water
        Vector3 waterLoc = new Vector3(width*3 / 2 - .5f, yCoord - .02f, height*3 / 2 - .5f);
        GameObject water = Instantiate(this.water, waterLoc, Quaternion.identity);
        water.transform.SetParent(groundTiles.transform, false);
        allTiles.Add(water);
        water.transform.localScale = new Vector3((width*3 + oceanRingDepth * 2)/10f, 1, (height*3 + oceanRingDepth * 2)/10f);
    }

    //only used for rivers now
    private void RotateRiverTerrain(System.Random random, int[] rotate, Dictionary<Vector3Int, int> mainMap, Vector3Int position, /*bool desert, bool river, bool grassland, */out Quaternion rotation, out int terrain)
    {
        int[] neighborTerrainLoc = new int[4] { 0, 0, 0, 0 };
        int neighborCount = 0;
        int i = 0;

        foreach (Vector3Int neighbor in ProceduralGeneration.neighborsFourDirections)
        {
            Vector3Int neighborPos = neighbor + position;

            //if (desert && !mainMap.ContainsKey(neighborPos))
            //    continue;

            //if (desert) //for desert
            //{
            //    if (mainMap[neighborPos] == ProceduralGeneration.grassland || mainMap[neighborPos] == ProceduralGeneration.forest ||
            //        mainMap[neighborPos] == ProceduralGeneration.jungle || mainMap[neighborPos] == ProceduralGeneration.jungleHill ||
            //        mainMap[neighborPos] == ProceduralGeneration.forestHill || mainMap[neighborPos] == ProceduralGeneration.swamp ||
            //        mainMap[neighborPos] == ProceduralGeneration.grasslandHill || mainMap[neighborPos] == ProceduralGeneration.grasslandMountain ||
            //        /*mainMap[neighborPos] == ProceduralGeneration.river || */mainMap[neighborPos] == ProceduralGeneration.grasslandFloodPlain /*||
            //        mainMap[neighborPos] == ProceduralGeneration.sea*/)
            //    {
            //        neighborCount++;
            //        neighborTerrainLoc[i] = 1;
            //    }
            //}
            //else if (!desert && !river && !grassland) //for swamp
            //{
            //    if (!mainMap.ContainsKey(neighborPos) ||
            //        mainMap[neighborPos] == ProceduralGeneration.river || mainMap[neighborPos] == ProceduralGeneration.sea ||
            //        mainMap[neighborPos] == ProceduralGeneration.desertMountain)
            //    {
            //        neighborCount++;
            //        neighborTerrainLoc[i] = 1;
            //    }
            //    //if (!mainMap.ContainsKey(neighborPos) || mainMap[neighborPos] == ProceduralGeneration.grassland || 
            //    //    mainMap[neighborPos] == ProceduralGeneration.forest ||mainMap[neighborPos] == ProceduralGeneration.jungle || 
            //    //    mainMap[neighborPos] == ProceduralGeneration.jungleHill || mainMap[neighborPos] == ProceduralGeneration.forestHill || 
            //    //    mainMap[neighborPos] == ProceduralGeneration.desertHill || mainMap[neighborPos] == ProceduralGeneration.desert ||
            //    //    mainMap[neighborPos] == ProceduralGeneration.grasslandHill || mainMap[neighborPos] == ProceduralGeneration.grasslandMountain ||
            //    //    mainMap[neighborPos] == ProceduralGeneration.grasslandFloodPlain || mainMap[neighborPos] == ProceduralGeneration.desertFloodPlain /*||
            //    //    mainMap[neighborPos] == ProceduralGeneration.river || mainMap[neighborPos] == ProceduralGeneration.sea*/)
            //    //{
            //    //    neighborCount++;
            //    //    neighborTerrainLoc[i] = 1;
            //    //}
            //}
            //else if (river) //for river 
            {
                if (!mainMap.ContainsKey(neighborPos) || 
                    mainMap[neighborPos] == ProceduralGeneration.river || mainMap[neighborPos] == ProceduralGeneration.sea)
                {
                    neighborCount++;
                    neighborTerrainLoc[i] = 1;
                }
            }
            //else if (grassland) //for grassland
            //{
            //    if (!mainMap.ContainsKey(neighborPos) ||
            //        mainMap[neighborPos] == ProceduralGeneration.river || mainMap[neighborPos] == ProceduralGeneration.sea ||
            //        mainMap[neighborPos] == ProceduralGeneration.desertMountain)
            //    {
            //        neighborCount++;
            //        neighborTerrainLoc[i] = 1;
            //    }
            //}

            i++;
        }

        if (neighborCount == 1)
        {
            terrain = 1;
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
                terrain = 3;
                rotation = Quaternion.Euler(0, (index % 2) * 90, 0);
            }
            else //for corners
            {
                int rotationFactor = totalIndex / 2;
                if (totalIndex == 3 && index == 3)
                    rotationFactor = 3;
                terrain = 0;
                rotation = Quaternion.Euler(0, rotationFactor * 90, 0);
            }
        }
        else if (neighborCount == 3)
        {
            terrain = 6;
            int index = Array.FindIndex(neighborTerrainLoc, x => x == 0);
            rotation = Quaternion.Euler(0, index * 90, 0);
        }
        else if (neighborCount == 4)
        {
            terrain = 2;
            rotation = Quaternion.Euler(0, rotate[random.Next(0, 4)], 0);
        }
        else
        {
            terrain = 9999;
            rotation = Quaternion.Euler(0, rotate[random.Next(0, 4)], 0);
        }
    }

    private void AddProp(System.Random random, TerrainData td, List<GameObject> propArray)
    {
        //TerrainData td = terrain.GetComponent<TerrainData>();
        //if (tdSO != null)
        //    td.terrainData = tdSO;

        Quaternion rotation = Quaternion.Euler(0, random.Next(0, 4) * 90, 0);
        //rotation = Quaternion.identity;

        int propInt = random.Next(0, propArray.Count);
        td.decorIndex = propInt;

        if (propArray[propInt] != null)
        {
    		GameObject newProp = Instantiate(propArray[propInt], Vector3Int.zero, rotation);
            newProp.transform.SetParent(td.prop, false);
        }
        //td.propPrefab = newProp;
    }

    //private void PopulateArrays()
    //{
    //    //grasslandMountains = new GameObject[4] { grasslandMountain1, grasslandMountain2, grasslandMountain3, grasslandMountain4 };
    //    //desertMountains = new GameObject[4] { desertMountain1, desertMountain2, desertMountain3, desertMountain4 };
    //    //grasslandProps = new GameObject[3] { grasslandProp01, grasslandProp02, grasslandProp03 };
    //    //desertProps = new GameObject[2] { desertProp01, desertProp02 };
    //    //desertFloodPlainProps = new GameObject[2] { desertFloodPlainProp01, desertFloodPlainProp02 };
    //    //riverStraights = new GameObject[3] { riverStraightVar1, riverStraightVar2, riverStraightVar3 };
    //    //forestProps = new GameObject[1] { forestPropVar01 };
    //    //jungleProps = new GameObject[1] { junglePropVar01 };
    //    //swampProps = new GameObject[1] { swampPropVar01 };
    //    //oceanCurves = new GameObject[2] { oceanCurve, oceanCurveDiagonal };
    //}

    private TerrainData GenerateTile(/*float perlinCoord, */GameObject tile, Vector3Int position, Quaternion rotation, int prefabIndex)
    {
        GameObject newTile = Instantiate(tile, position, rotation);
        newTile.transform.SetParent(groundTiles.transform, false);
        allTiles.Add(newTile);
		TerrainData td = newTile.GetComponent<TerrainData>();
        td.prefabIndex = prefabIndex;
        td.TileCoordinates = position;
        td.TerrainDataPrep();
        terrainDict[position] = td;

        if (explored)
            td.fog.gameObject.SetActive(false);

		return td;
    }

    protected void RemoveAllTiles()
    {
        foreach (GameObject tile in allTiles)
        {
            DestroyImmediate(tile);
        }
    }

    public TerrainData GetTerrainDataAt(Vector3Int loc)
    {
        return terrainDict[loc];
    }

    public bool TerrainExistsCheck(Vector3Int loc)
    {
        return terrainDict.ContainsKey(loc);
    }
}


