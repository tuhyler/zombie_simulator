using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class TerrainGenerator : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;
    [SerializeField]
    private GameObject startingArrow, enemyBorder;
    
    [Header("General Map Parameters")]
    [SerializeField]
    private int seed = 4;
    [SerializeField]
    private int width = 50, height = 50, yCoord = 3, landMassLimit = 1, totalLandLimit = 400, desertPerc = 30, forestAndJunglePerc = 70, mountainPerc = 10, mountainousPerc = 80, 
        mountainRangeLength = 20, equatorDist = 10, riverPerc = 5, riverCountMin = 10, oceanRingDepth = 2, startingSpotGrasslandCountMin = 5, startingSpotGrasslandCountMax = 20,
        tradeCenterCount = 1, tradeCenterDistance = 30, resourceFrequency = 4, enemyCountDifficulty = 1;

    [Header("Natural Resource Percs (sum to 100)")]
    [SerializeField]
    private int stone = 60;
    [SerializeField]
    private int gold = 5, jewel = 5, clay = 15, cloth = 15;

    [Header("Count of adjacent tiles with resource")]
    [SerializeField]
    private int stoneAdjacent = 3;
    [SerializeField]
    private int goldAdjacent = 2, jewelAdjacent = 0;

    [Header("Range of Resource Amounts")]
    [SerializeField]
    private (int, int) stoneAmounts = (1000, 10000);
    [SerializeField]
    private (int, int) goldAmounts = (200, 500), jewelAmounts = (100, 300);

    [SerializeField]
	private bool startingGame = true;

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

    private bool retry;

    [Header("Water")]
    [SerializeField]
    private GameObject water;

    [Header("Parent Transforms")]
    [SerializeField]
    private Transform groundTiles;
    [SerializeField]
    private Transform tradeCenterHolder, enemyHolder;

    [Header("Terrain Data SO")]
    [SerializeField]
    private TerrainDataSO coastSO;
    [SerializeField]
    private TerrainDataSO desertFloodPlainsSO, desertHillResourceSO, desertHillSO, desertMountainSO, desertResourceSO, desertSO, forestHillSO, forestSO, 
        grasslandFloodPlainsSO, grasslandHillResourceSO, grasslandHillSO, grasslandMountainSO, grasslandResourceSO, grasslandSO, 
        jungleHillSO, jungleSO, riverSO, riverResourceSO, seaIntersectionSO, seaSO, seaResourceSO, swampSO;

    [Header("Trade Center Prefabs")]
    [SerializeField]
    private List<GameObject> tradeCenters;

    [Header("Enemy Unit Prefabs (use same order)")]
    [SerializeField]
    private List<GameObject> enemyUnits;

    [Header("Enemy Camp Parameters")]
    [SerializeField] 
    private int infantryMax = 9;
    [SerializeField]
    private int rangedMax = 6, cavalryMax = 9;

    private List<GameObject> allTiles = new();

    public bool autoUpdate, explored;

    private Dictionary<Vector3Int, TerrainData> terrainDict = new();
    private Vector3Int startingPlace;

    public void GenerateMap()
    {
        terrainDict.Clear();
        RunProceduralGeneration(false);
    }

    public void RemoveMap()
    {
        RemoveAllTiles();
    }

    public Dictionary<Vector3Int, TerrainData> RunProceduralGeneration(bool newGame)
    {
        RemoveAllTiles();
        retry = false;

        System.Random random = new System.Random(seed);
        int[] rotate = new int[4] { 0, 90, 180, 270 };
        List<Vector3Int> landLocs = new();
        List<Vector3Int> riverTiles = new();
        List<Vector3Int> coastTiles = new();
        List<Vector3Int> grasslandTiles = new();
        List<TerrainData> propTiles = new();

        Dictionary<Vector3Int, int> mainMap = 
            ProceduralGeneration.GenerateCellularAutomata(threshold, width, height, iterations, randomFillPercent, seed, yCoord);

        Dictionary<int, List<Vector3Int>> landMasses = ProceduralGeneration.GetLandMasses(mainMap);

        int totalLand = 0;
        foreach (int num in landMasses.Keys)
            totalLand += landMasses[num].Count;

        if (landMasses.Count > landMassLimit || totalLand < totalLandLimit)
            retry = true;

        if (retry)
        {
            seed++;
            terrainDict.Clear();
            RunProceduralGeneration(newGame);
            return null;
        }

        Dictionary<Vector3Int, float> noise = ProceduralGeneration.PerlinNoiseGenerator(mainMap,
            scale, octaves, persistance, lacunarity, seed, offset);

        mainMap = ProceduralGeneration.GenerateTerrain(mainMap, noise, 0.45f, 0.65f, desertPerc, forestAndJunglePerc,
            width, height, yCoord, equatorDist, seed);

        Dictionary<Vector3Int, int> mountainMap = ProceduralGeneration.GenerateMountainRanges(mainMap, landMasses, mountainPerc, mountainousPerc, mountainRangeLength, 
            width, height, yCoord, seed);

        mountainMap = ProceduralGeneration.GenerateRivers(mountainMap, riverPerc, riverCountMin, seed);

        mainMap = ProceduralGeneration.MergeMountainTerrain(mainMap, mountainMap);

        mainMap = ProceduralGeneration.AddOceanRing(mainMap, width, height, yCoord, oceanRingDepth);

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
                        coastTiles.Add(position);
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
								coastTiles.Add(position);
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
								coastTiles.Add(position);
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
					coastTiles.Add(position);
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
								coastTiles.Add(position);
								prefabIndex = 1;
							}
                            else
                            {
                                sea = coastSO.prefabs[2];
								coastTiles.Add(position);
								prefabIndex = 2;
							}
                        }
                        else if (riverIndex == min)
                        {
                            if (max - min == 1)
                            {
                                sea = coastSO.prefabs[2];
								coastTiles.Add(position);
								prefabIndex = 2;
							}
                            else
                            {
                                sea = coastSO.prefabs[1];
								coastTiles.Add(position);
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

                GenerateTile(sea, position, rotation, prefabIndex, true);
            }
            else if (mainMap[position] == ProceduralGeneration.grasslandHill)
            {
                landLocs.Add(position);
                GenerateTile(grasslandHillSO.prefabs[0], position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
            }
            else if (mainMap[position] == ProceduralGeneration.desertHill)
            {
				landLocs.Add(position);
				GenerateTile(desertHillSO.prefabs[0], position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
            }
            else if (mainMap[position] == ProceduralGeneration.forestHill)
            {
				landLocs.Add(position);
				GameObject forestHill = grasslandHillSO.prefabs[0];
                TerrainData td = GenerateTile(forestHill, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
                td.gameObject.tag = "Forest Hill";
                td.terrainData = forestHillSO;
                td.resourceAmount = -1;
                propTiles.Add(td);
            }
            else if (mainMap[position] == ProceduralGeneration.jungleHill)
            {
				landLocs.Add(position);
				GameObject jungleHill = grasslandHillSO.prefabs[0];
                TerrainData td = GenerateTile(jungleHill, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
                td.gameObject.tag = "Forest Hill";
                td.terrainData = jungleHillSO;
                td.resourceAmount = -1;
				propTiles.Add(td);
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
				landLocs.Add(position);
				TerrainData td = GenerateTile(grasslandSO.prefabs[0], position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
				//propTiles.Add(td);
				grasslandTiles.Add(position);
			}
            else if (mainMap[position] == ProceduralGeneration.desert)
            {
				landLocs.Add(position);
				TerrainData td = GenerateTile(desertSO.prefabs[0], position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
				propTiles.Add(td);
            }
            else if (mainMap[position] == ProceduralGeneration.forest)
            {
				landLocs.Add(position);
				GameObject forest = grasslandSO.prefabs[0];
                TerrainData td = GenerateTile(forest, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
                td.gameObject.tag = "Forest";
                td.terrainData = forestSO;
                td.resourceAmount = -1;
				propTiles.Add(td);
            }
            else if (mainMap[position] == ProceduralGeneration.jungle)
            {
				landLocs.Add(position);
				GameObject jungle = grasslandSO.prefabs[0];
                TerrainData td = GenerateTile(jungle, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
                td.gameObject.tag = "Forest";
                td.terrainData = jungleSO;
                td.resourceAmount = -1;
				propTiles.Add(td);
            }
            else if (mainMap[position] == ProceduralGeneration.swamp)
            {
				landLocs.Add(position);
				TerrainData td = GenerateTile(swampSO.prefabs[0], position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
				td.gameObject.tag = "Forest";
				td.terrainData = swampSO;
                td.resourceAmount = -1;
                propTiles.Add(td);
            }
            else if (mainMap[position] == ProceduralGeneration.river)
            {
				riverTiles.Add(position);
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

                GenerateTile(river, position, rotation, riverInt, true);
            }
            else if (mainMap[position] == ProceduralGeneration.grasslandFloodPlain)
            {
				landLocs.Add(position);
				TerrainData td = GenerateTile(grasslandFloodPlainsSO.prefabs[0], position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
                td.resourceAmount = -1;
				propTiles.Add(td);
            }
            else if (mainMap[position] == ProceduralGeneration.desertFloodPlain)
            {
				landLocs.Add(position);
				TerrainData td = GenerateTile(desertFloodPlainsSO.prefabs[0], position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
                td.resourceAmount = -1;
                propTiles.Add(td);
            }
            else
            {
				landLocs.Add(position);
				GenerateTile(grasslandSO.prefabs[0], position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
                grasslandTiles.Add(position);
            }
        }

        Vector3Int startingPlace = Vector3Int.zero;

        if (riverTiles.Count == 0)
        {
            retry = true;
        }
        else if (startingGame)
        {
    		startingPlace = FindStartingPlace(riverTiles);
        }

		//if failure to find starting place, then rebuild the map
		if (retry)
        {
            seed++;
            terrainDict.Clear();
            RunProceduralGeneration(newGame);
            return null;
        }

        Vector3Int firstTradeCenter = new Vector3Int(0, -10, 0);
        if (startingGame)
            firstTradeCenter = PlaceNeighborTradeCenter(random, startingPlace, coastTiles, riverTiles);

        List<Vector3Int> tradeCenterLocs = new();

        if (firstTradeCenter.y != -10) //setting as Vector3Int? overly complicates
            tradeCenterLocs.Add(firstTradeCenter);
            
        tradeCenterLocs = PlaceTradeCenters(random, tradeCenterLocs, startingPlace, coastTiles, riverTiles);

        (List<Vector3Int> luxuryLocs, List<Vector3Int> foodLocs, List<Vector3Int> waterLocs, List<Vector3Int> resourceLocs) = 
            GenerateResources(random, startingPlace, tradeCenterLocs, grasslandTiles, riverTiles, coastTiles);
		(List<TerrainData> newPropTiles, List<Vector3Int> newFoodLocs, List<Vector3Int> newWaterLocs, List<Vector3Int> newResourceLocs) = SetStartingResources(random, startingPlace);
        propTiles.AddRange(newPropTiles);
        foodLocs.AddRange(newFoodLocs);
        waterLocs.AddRange(newWaterLocs);
        resourceLocs.AddRange(newResourceLocs);

		for (int i = 0; i < propTiles.Count; i++)
        {
            if (resourceLocs.Contains(propTiles[i].TileCoordinates) || foodLocs.Contains(propTiles[i].TileCoordinates))
                continue;

            bool swamp = propTiles[i].terrainData.terrainDesc == TerrainDesc.Swamp;
            bool forest = propTiles[i].CompareTag("Forest") || propTiles[i].CompareTag("Forest Hill");
            AddProp(random, propTiles[i], propTiles[i].terrainData.decors, swamp, forest, newGame);
        }

        for (int i = 0; i < foodLocs.Count; i++)
			AddResource(random, terrainDict[foodLocs[i]]);

		for (int i = 0; i < waterLocs.Count; i++)
			AddResource(random, terrainDict[waterLocs[i]]);

		for (int i = 0; i < resourceLocs.Count; i++)
            AddResource(random, terrainDict[resourceLocs[i]]);

        List<Vector3Int> enemyLocs = GenerateEnemyCamps(random, startingPlace, tradeCenterLocs, landLocs, luxuryLocs, resourceLocs);
        //SetEnemyBorders(enemyLocs);

        for (int i = 0; i < tradeCenterLocs.Count; i++)
        {
            SwampCheck(tradeCenterLocs[i]);
            FloodPlainCheck(tradeCenterLocs[i]);

            if (terrainDict[tradeCenterLocs[i]].CompareTag("Forest"))
            {
                TerrainDataSO data = terrainDict[tradeCenterLocs[i]].terrainData.grassland ? grasslandSO : desertSO;
                DestroyImmediate(terrainDict[tradeCenterLocs[i]].gameObject);
                TerrainData td = GenerateTile(data.prefabs[0], tradeCenterLocs[i], Quaternion.identity, 0);
                td.gameObject.tag = "Flatland";
            }
            
            int index = -2;

            //finding water terrain for harbor
            for (int j = 0; j < ProceduralGeneration.neighborsFourDirections.Count; j++)
            {
                Vector3Int tile = ProceduralGeneration.neighborsFourDirections[j] + tradeCenterLocs[i];

                if (terrainDict[tile].isLand)
                    index++;
                else
                    break;
			}

            terrainDict[tradeCenterLocs[i]].prop.gameObject.SetActive(false);
            terrainDict[tradeCenterLocs[i]].showProp = false;
			Quaternion rotation = Quaternion.Euler(0, index * 90, 0);
            GameObject tradeCenterGO = Instantiate(tradeCenters[i], tradeCenterLocs[i], rotation);
            tradeCenterGO.transform.SetParent(tradeCenterHolder, false);
            Quaternion miniRotation = Quaternion.Euler(90, 0, 0);
            tradeCenterGO.GetComponent<TradeCenter>().minimapIcon.rotation = miniRotation;
            allTiles.Add(tradeCenterGO);
        }

        //placing main unit
        if (!newGame)
        {
            GameObject arrow = Instantiate(startingArrow, startingPlace, Quaternion.identity);
            arrow.transform.SetParent(groundTiles, false);
            allTiles.Add(arrow);
        }

        this.startingPlace = startingPlace;
		FloodPlainCheck(startingPlace);

		//Finish it all off by placing water
		//Vector3 waterLoc = new Vector3(width*3 / 2 - .5f, yCoord - .02f, height*3 / 2 - .5f);
		//      GameObject water = Instantiate(this.water, waterLoc, Quaternion.identity);
		//      water.transform.SetParent(groundTiles.transform, false);
		//      allTiles.Add(water);
		//      water.transform.localScale = new Vector3((width*3 + oceanRingDepth * 2)/10f, 1, (height*3 + oceanRingDepth * 2)/10f);
		return terrainDict;
    }

	//only used for rivers now
	private void RotateRiverTerrain(System.Random random, int[] rotate, Dictionary<Vector3Int, int> mainMap, Vector3Int position, out Quaternion rotation, out int terrain)
    {
        int[] neighborTerrainLoc = new int[4] { 0, 0, 0, 0 };
        int neighborCount = 0;
        int i = 0;

        foreach (Vector3Int neighbor in ProceduralGeneration.neighborsFourDirections)
        {
            Vector3Int neighborPos = neighbor + position;

            if (!mainMap.ContainsKey(neighborPos) || 
                mainMap[neighborPos] == ProceduralGeneration.river || mainMap[neighborPos] == ProceduralGeneration.sea)
            {
                neighborCount++;
                neighborTerrainLoc[i] = 1;
            }

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

    private void AddProp(System.Random random, TerrainData td, List<GameObject> propArray, bool swamp, bool forest, bool newGame)
    {
        Quaternion rotation;
        if (swamp)
            rotation = terrainDict[td.TileCoordinates].main.rotation;
        else
            rotation = Quaternion.Euler(0, random.Next(0, 4) * 90, 0);

        if (forest && !swamp)
        {
            int propInt = 0;
            bool changeLeafColor = false;
            
            if (propArray.Count > 1)
			{
				propInt = 1;
				//check for nearby mountains
				for (int i = 0; i < ProceduralGeneration.neighborsEightDirections.Count; i++)
				{
					if (terrainDict[ProceduralGeneration.neighborsEightDirections[i] + td.TileCoordinates].terrainData.terrainDesc == TerrainDesc.Mountain)
					{
						propInt = random.Next(0, 2);
                        changeLeafColor = true;
						break;
					}
				}
			}

			td.decorIndex = propInt;

			if (propInt == 1 && newGame)
			{
				td.changeLeafColor = changeLeafColor;

                if (changeLeafColor)
                {
				    for (int i = 0; i < 10; i++)
					    td.uvMapIndex.Add(random.Next(1, 4));
                }
			}

			GameObject newProp = Instantiate(propArray[propInt], Vector3Int.zero, Quaternion.identity);
			newProp.transform.SetParent(td.prop, false);
			newProp.GetComponent<TreeHandler>().propMesh.rotation = rotation;

			GameObject nonStaticProp = Instantiate(td.terrainData.decors[td.decorIndex], Vector3.zero, Quaternion.identity);
			nonStaticProp.transform.SetParent(td.nonstatic, false);
			td.nonstatic.rotation = rotation;
			td.SetNonStatic();
		}
        else
        {
			int propInt = random.Next(0, propArray.Count);
			td.decorIndex = propInt;

            if (propArray[propInt] != null)
            {
			    GameObject newProp = Instantiate(propArray[propInt], Vector3Int.zero, rotation);
			    newProp.transform.SetParent(td.prop, false);
            }
		}
    }

    private void AddResource(System.Random random, TerrainData td)
    {
		Quaternion rotation = Quaternion.Euler(0, random.Next(0, 4) * 90, 0);
        int index = td.decorIndex;

		if (td.terrainData.decors[index] != null)
		{
			GameObject newProp = Instantiate(td.terrainData.decors[index], Vector3Int.zero, rotation);
			newProp.transform.SetParent(td.prop, false);
		}
	}

    private TerrainData GenerateTile(GameObject tile, Vector3Int position, Quaternion rotation, int prefabIndex, bool water = false)
    {
        GameObject newTile = Instantiate(tile, position, Quaternion.identity);
        newTile.transform.SetParent(groundTiles.transform, false);
        allTiles.Add(newTile);
		TerrainData td = newTile.GetComponent<TerrainData>();
        td.prefabIndex = prefabIndex;
        td.TileCoordinates = position;
        td.TerrainDataPrep();
        terrainDict[position] = td;
        //must keep collision in parent for some reason
        if (water)
            newTile.transform.rotation = rotation;
        else
            td.main.rotation = rotation;

        td.nonstatic.rotation = rotation;

        if (explored)
            td.fog.gameObject.SetActive(false);

        //if (td.minimapIcon != null)
        //    td.minimapIcon.transform.rotation = rotation;

		return td;
    }

    protected void RemoveAllTiles()
    {
        for (int i = 0; i < allTiles.Count; i++)
        {
            DestroyImmediate(allTiles[i]);
        }

        allTiles.Clear();
    }

    private Vector3Int FindStartingPlace(List<Vector3Int> riverTiles)
    {
        Vector3Int middleTile = new Vector3Int(width / 2, 0, height / 2);
        Vector3Int startingLoc = Vector3Int.zero;
        Queue<Vector3Int> potentialStarts = new();

        List<(Vector3Int, int)> middleDist = new();
        List<(Vector3Int, int, int)> grasslandCountList = new();
        List<Vector3Int> alreadyChecked = new();

        //calculating distance from middle to sort by proximity
        for (int i = 0; i < riverTiles.Count; i++)
        {
            int dist = Mathf.Abs(riverTiles[i].x - middleTile.x) + Mathf.Abs(riverTiles[i].z - middleTile.z);
            middleDist.Add((riverTiles[i], dist));
        }

        middleDist.Sort((a, b) => a.Item2.CompareTo(b.Item2));

        //taking all tiles with grassland and counting neighboring grassland
        for (int i = 0; i < middleDist.Count; i++)
        {
            //Vector3Int startingLoc = Vector3Int.zero;
            bool findNeighbors = false; 

            for (int j = 0; j < ProceduralGeneration.neighborsFourDirections.Count; j++)
            {
                Vector3Int tile = ProceduralGeneration.neighborsFourDirections[j] + middleDist[i].Item1;

                if (terrainDict[tile].CompareTag("Flatland"))
                {
                    startingLoc = tile;
                    findNeighbors = true;
                    break;
                }
            }

            if (!findNeighbors)
                continue;
            
            for (int j = 0; j < ProceduralGeneration.neighborsEightDirections.Count; j++)
            {
                Vector3Int tile = ProceduralGeneration.neighborsEightDirections[j] + startingLoc;

                if (alreadyChecked.Contains(tile))
                    continue;

                alreadyChecked.Add(tile);

				if (terrainDict[tile].CompareTag("Flatland"))
                {
                    int flatlandCount = 0;
                    int waterCount = 0;
                        
                    for (int k = 0; k < ProceduralGeneration.neighborsCityRadius.Count; k++)
                    {
                        Vector3Int neighborTile = tile + ProceduralGeneration.neighborsCityRadius[k];

						if (terrainDict[neighborTile].CompareTag("Flatland"))
                            flatlandCount++;

                        //water tiles have to be less than half
                        if (!terrainDict[neighborTile].isLand)
                            waterCount++;
                    }

                    grasslandCountList.Add((startingLoc, flatlandCount, waterCount));
                }
			}
        }

        for (int i = 0; i < grasslandCountList.Count; i++)
        {
            if (grasslandCountList[i].Item2 >= startingSpotGrasslandCountMin && grasslandCountList[i].Item2 <= startingSpotGrasslandCountMax && grasslandCountList[i].Item3 < 10)
                return grasslandCountList[i].Item1;
        }

        retry = true;
        return startingLoc;
    }

    private (List<TerrainData>, List<Vector3Int>, List<Vector3Int>, List<Vector3Int>) SetStartingResources(System.Random random, Vector3Int startingPlace)
    {
        List<Vector3Int> forestTiles = new();
        List<Vector3Int> hillTiles = new();
        List<Vector3Int> flatlandTiles = new();
        List<Vector3Int> grasslandTiles = new();
        List<Vector3Int> resourceTiles = new();
        List<Vector3Int> mountainTiles = new();
        List<Vector3Int> floodPlainTiles = new();
        List<Vector3Int> riverCoastTiles = new();
        List<TerrainData> propTiles = new();
        List<Vector3Int> foodLocs = new();
        List<Vector3Int> waterLocs = new();
        
        for (int i = 0; i < ProceduralGeneration.neighborsCityRadius.Count; i++)
        {
            Vector3Int tile = ProceduralGeneration.neighborsCityRadius[i] + startingPlace;

            if (terrainDict[tile].CompareTag("Forest") || terrainDict[tile].CompareTag("Forest Hill"))
            {
                forestTiles.Add(tile);
            }
            else if (terrainDict[tile].CompareTag("Hill"))
            {
                hillTiles.Add(tile);
            }
            else if (terrainDict[tile].CompareTag("Flatland"))
            {
                flatlandTiles.Add(tile);
                if (terrainDict[tile].terrainData.grassland)
                    grasslandTiles.Add(tile);
                
                if (terrainDict[tile].terrainData.specificTerrain == SpecificTerrain.FloodPlain)
                    floodPlainTiles.Add(tile);
            }
            else if (terrainDict[tile].terrainData.type == TerrainType.River || terrainDict[tile].terrainData.type == TerrainType.Coast)
            {
                riverCoastTiles.Add(tile);
            }
            else if (terrainDict[tile].terrainData.terrainDesc == TerrainDesc.Mountain)
            {
                mountainTiles.Add(tile);
            }
        }

        //making sure every starting spot has at least x floodplains
        int minimumFloodPlainTiles = 1;
        int remainingFloodPlainTiles = minimumFloodPlainTiles - floodPlainTiles.Count;
        if (remainingFloodPlainTiles > 0)
        {
            for (int i = 0; i < floodPlainTiles.Count; i++)
            {
				Vector3Int floodPlainsTile = floodPlainTiles[random.Next(0, floodPlainTiles.Count)];

				flatlandTiles.Remove(floodPlainsTile);
				grasslandTiles.Remove(floodPlainsTile);
				floodPlainTiles.Remove(floodPlainsTile);
			}
            
            List<Vector3Int> potentialMountainTiles = new();
            List<Vector3Int> potentialHillTiles = new();
            List<Vector3Int> potentialForestTiles = new();

            for (int i = 0; i < riverCoastTiles.Count; i++)
            {
                if (terrainDict[riverCoastTiles[i]].terrainData.type == TerrainType.Coast)
                    continue;

                for (int j = 0; j < ProceduralGeneration.neighborsFourDirections.Count; j++)
                {
                    Vector3Int tile = ProceduralGeneration.neighborsFourDirections[j] + riverCoastTiles[i];

                    if (mountainTiles.Contains(tile))
                    {
                        potentialMountainTiles.Add(tile);
                    }
                    else if (hillTiles.Contains(tile))
                    {
                        potentialHillTiles.Add(tile);
                    }
                    else if (forestTiles.Contains(tile))
                    {
                        potentialForestTiles.Add(tile);
                    }
				}
            }

            for (int i = 0; i < remainingFloodPlainTiles; i++)
            {
                if (potentialMountainTiles.Count > 0)
                {
                    Vector3Int tile = potentialMountainTiles[random.Next(0, potentialMountainTiles.Count)];
                    
                    DestroyImmediate(terrainDict[tile].gameObject);
					GenerateTile(grasslandFloodPlainsSO.prefabs[0], tile, Quaternion.identity, 0);
					mountainTiles.Remove(tile);
                    potentialMountainTiles.Remove(tile);
					floodPlainTiles.Add(tile);
				}
				else if (potentialHillTiles.Count > 0)
                {
                    Vector3Int tile = potentialHillTiles[random.Next(0, potentialHillTiles.Count)];

                    DestroyImmediate(terrainDict[tile].gameObject);
                    GenerateTile(grasslandFloodPlainsSO.prefabs[0], tile, Quaternion.identity, 0);
                    hillTiles.Remove(tile);
                    potentialHillTiles.Remove(tile);
                    floodPlainTiles.Add(tile);
                }
                else
                {
				    Vector3Int tile = potentialForestTiles[random.Next(0, potentialForestTiles.Count)];

				    DestroyImmediate(terrainDict[tile].gameObject);
				    GenerateTile(grasslandFloodPlainsSO.prefabs[0], tile, Quaternion.identity, 0);
				    forestTiles.Remove(tile);
                    potentialForestTiles.Remove(tile);
				    floodPlainTiles.Add(tile);
			    }
            }
        }
        else
        {
            for (int i = 0; i < minimumFloodPlainTiles; i++)
            {
                Vector3Int floodPlainsTile = floodPlainTiles[random.Next(0, floodPlainTiles.Count)];

                flatlandTiles.Remove(floodPlainsTile);
                grasslandTiles.Remove(floodPlainsTile);
                floodPlainTiles.Remove(floodPlainsTile);
            }
        }

        //making sure mountains don't dominate
        if (mountainTiles.Count > 0 && hillTiles.Count == 0)
        {
			Vector3Int mountain = mountainTiles[random.Next(0, mountainTiles.Count)];
			DestroyImmediate(terrainDict[mountain].gameObject);
			GenerateTile(grasslandHillSO.prefabs[0], mountain, Quaternion.identity, 0);
            hillTiles.Add(mountain);
            mountainTiles.Remove(mountain);
		}

        if (mountainTiles.Count > 0 && grasslandTiles.Count == 0)
        {
			Vector3Int mountain = mountainTiles[random.Next(0, mountainTiles.Count)];
			DestroyImmediate(terrainDict[mountain].gameObject);
			TerrainData td = GenerateTile(grasslandSO.prefabs[0], mountain, Quaternion.identity, 0);
            td.gameObject.tag = "Flatland";
            grasslandTiles.Add(mountain);
        }

        //making sure starting loc has lumber
        if (forestTiles.Count == 0)
        {
            Vector3Int newForest;
            TerrainDataSO data;
            string tag;
            
            if (hillTiles.Count > 0)
            {
				newForest = hillTiles[random.Next(0, hillTiles.Count)];
                data = forestHillSO;
                hillTiles.Remove(newForest);
                tag = "Forest Hill";
			}
            else if (grasslandTiles.Count > 0)
            {
                newForest = grasslandTiles[random.Next(0, grasslandTiles.Count)];
                data = forestSO;
                FloodPlainCheck(newForest);
                flatlandTiles.Remove(newForest);
                grasslandTiles.Remove(newForest);
                tag = "Forest";
			}
            else
            {
                newForest = flatlandTiles[random.Next(0, flatlandTiles.Count)];
                DestroyImmediate(terrainDict[newForest].gameObject);
				GenerateTile(grasslandSO.prefabs[0], newForest, Quaternion.identity, 0);
                data = forestSO;
				flatlandTiles.Remove(newForest);
                tag = "Forest";
			}

            terrainDict[newForest].gameObject.tag = tag;
            terrainDict[newForest].terrainData = data;
			terrainDict[newForest].resourceAmount = -1;
			propTiles.Add(terrainDict[newForest]);
        }

        //making sure starting loc has cloth
        Vector3Int newCloth;
        if (grasslandTiles.Count == 0)
        {
			newCloth = flatlandTiles[random.Next(0, flatlandTiles.Count)];
            FloodPlainCheck(newCloth);
			DestroyImmediate(terrainDict[newCloth].gameObject);
			GenerateTile(grasslandSO.prefabs[0], newCloth, Quaternion.identity, 0);
		}
        else
        {
			newCloth = grasslandTiles[random.Next(0, grasslandTiles.Count)];
            FloodPlainCheck(newCloth);
		}

        GenerateClothTile(random, newCloth);
		resourceTiles.Add(newCloth);
        flatlandTiles.Remove(newCloth);
        grasslandTiles.Remove(newCloth);

		//making sure starting loc has clay
		Vector3Int newClay;
        bool grassland;
        if (flatlandTiles.Count == 0)
        {
            if (forestTiles.Count > 0)
            {
                newClay = forestTiles[random.Next(0, forestTiles.Count)];
                SwampCheck(newClay);
                grassland = true;
			}
            else
            {
				newClay = hillTiles[random.Next(0, hillTiles.Count)];
                grassland = terrainDict[newClay].terrainData.grassland;
				TerrainDataSO data = grassland ? grasslandSO : desertSO;
                DestroyImmediate(terrainDict[newClay].gameObject);
				GenerateTile(data.prefabs[0], newClay, Quaternion.identity, 0);
			}
		}
        else
        {
            newClay = flatlandTiles[random.Next(0, flatlandTiles.Count)];
            FloodPlainCheck(newClay);
            grassland = terrainDict[newClay].terrainData.grassland;
        }

        terrainDict[newClay].terrainData = grassland ? grasslandResourceSO : desertResourceSO;
		terrainDict[newClay].resourceType = ResourceType.Clay;
		terrainDict[newClay].rawResourceType = RawResourceType.Clay;
		terrainDict[newClay].decorIndex = 3;
        terrainDict[newClay].gameObject.tag = "Flatland";
		terrainDict[newClay].resourceAmount = -1;
		resourceTiles.Add(newClay);
        flatlandTiles.Remove(newClay);
        grasslandTiles.Remove(newClay);

        //making sure starting loc has stone
        Vector3Int newStone;
        bool stoneGrassland;
        bool isHill;
        if (hillTiles.Count > 0)
        {
            newStone = hillTiles[random.Next(0, hillTiles.Count)];
            stoneGrassland = terrainDict[newStone].terrainData.grassland;
            isHill = true;
            hillTiles.Remove(newStone);
        }
        else
        {
			newStone = flatlandTiles[random.Next(0, flatlandTiles.Count)];
            stoneGrassland = terrainDict[newStone].terrainData.grassland;
            FloodPlainCheck(newStone);
			isHill = false;
            flatlandTiles.Remove(newStone);
            grasslandTiles.Remove(newStone);
        }

        if (isHill)
		    terrainDict[newStone].terrainData = stoneGrassland ? grasslandHillResourceSO : desertHillResourceSO;
        else
			terrainDict[newStone].terrainData = stoneGrassland ? grasslandResourceSO : desertResourceSO;

		terrainDict[newStone].resourceType = ResourceType.Stone;
		terrainDict[newStone].rawResourceType = RawResourceType.Rocks;
        terrainDict[newStone].resourceAmount = random.Next(stoneAmounts.Item1, stoneAmounts.Item2);
		terrainDict[newStone].decorIndex = 0;
		resourceTiles.Add(newStone);

        //making sure one farmland is left (not counting desert flood plains)
        if (grasslandTiles.Count == 0)
        {
            if (mountainTiles.Count > 0)
            {
				Vector3Int mountain = mountainTiles[random.Next(0, mountainTiles.Count)];
				DestroyImmediate(terrainDict[mountain].gameObject);
				TerrainData td = GenerateTile(grasslandSO.prefabs[0], mountain, Quaternion.identity, 0);
                td.gameObject.tag = "Flatland";
                grasslandTiles.Add(mountain);
			}
            else if (hillTiles.Count > 0)
            {
				Vector3Int hill = hillTiles[random.Next(0, hillTiles.Count)];
				DestroyImmediate(terrainDict[hill].gameObject);
				TerrainData td = GenerateTile(grasslandSO.prefabs[0], hill, Quaternion.identity, 0);
				td.gameObject.tag = "Flatland";
				grasslandTiles.Add(hill);
			}
            else if (flatlandTiles.Count > 0)
            {
				Vector3Int flatland = flatlandTiles[random.Next(0, flatlandTiles.Count)];

                if (terrainDict[flatland].terrainData.specificTerrain != SpecificTerrain.FloodPlain)
                {
				    DestroyImmediate(terrainDict[flatland].gameObject);
				    GenerateTile(grasslandSO.prefabs[0], flatland, Quaternion.identity, 0);
					grasslandTiles.Add(flatland);
				}
			}
        }

        //adding food, max of two per start
        for (int i = 0; i < 2; i++)
        {
			Vector3Int newFood = grasslandTiles[random.Next(0, grasslandTiles.Count)];

            FloodPlainCheck(newFood);
			terrainDict[newFood].terrainData = grasslandResourceSO;
			terrainDict[newFood].resourceType = ResourceType.Food;
			terrainDict[newFood].rawResourceType = RawResourceType.FoodLand;
			terrainDict[newFood].decorIndex = 4;
			terrainDict[newFood].resourceAmount = -1;
            terrainDict[newFood].uvMapIndex.Add(random.Next(0, 0));
			terrainDict[newFood].uvMapIndex.Add(random.Next(0, 2));

			grasslandTiles.Remove(newFood);

			//don't add to resourcetiles
			foodLocs.Add(newFood);
		}

        //adding fish, max of two per start
        for (int i = 0; i < 2; i++)
        {
            Vector3Int newWater = riverCoastTiles[random.Next(0, riverCoastTiles.Count)];
            bool river = terrainDict[newWater].terrainData.type == TerrainType.River;

            terrainDict[newWater].terrainData = river ? riverResourceSO : seaResourceSO;
			terrainDict[newWater].resourceType = ResourceType.Fish;
			terrainDict[newWater].rawResourceType = RawResourceType.FoodSea;
			terrainDict[newWater].decorIndex = 0;
			terrainDict[newWater].resourceAmount = -1;
			riverCoastTiles.Remove(newWater);

            //don't add to resourcetiles
            waterLocs.Add(newWater);
		}

        return (propTiles, foodLocs, waterLocs, resourceTiles);
	}

    private Vector3Int PlaceNeighborTradeCenter(System.Random random, Vector3Int startingPlace, List<Vector3Int> coastTiles, List<Vector3Int> riverTiles)
    {
		//placing first trade center
		int[] widthArray = new int[2] { 5, 6 };
		int width = widthArray[random.Next(0, widthArray.Length)];
        int direction = random.Next(0, 4);
        int trys = 0;

        while (trys < 4)
        {
            switch (direction)
            {
                case 0:
				    Vector3Int corner = new Vector3Int(startingPlace.x - width * 3, yCoord, startingPlace.z - width * 3);

				    for (int x = 0; x < width; x++)
				    {
					    corner.x += 3;

					    if (coastTiles.Contains(corner) || riverTiles.Contains(corner))
                        {
							if (TryClosestFlatland(corner, out Vector3Int tile))
								return tile;
						}
				    }

                    trys++;
                    direction++;
				    break;
                case 1:
					Vector3Int corner1 = new Vector3Int(startingPlace.x + width * 3, yCoord, startingPlace.z - width * 3);

					for (int z = 0; z < width; z++)
					{
						corner1.z += 3;

						if (coastTiles.Contains(corner1) || riverTiles.Contains(corner1))
                        {
							if (TryClosestFlatland(corner1, out Vector3Int tile))
								return tile;
						}
					}

					trys++;
					direction++;
					break;
                case 2:
					Vector3Int corner2 = new Vector3Int(startingPlace.x - width * 3, yCoord, startingPlace.z + width * 3);

					for (int x = 0; x < width; x++)
					{
						corner2.x += 3;

						if (coastTiles.Contains(corner2) || riverTiles.Contains(corner2))
                        {
							if (TryClosestFlatland(corner2, out Vector3Int tile))
								return tile;
						}
					}

					trys++;
					direction++;
					break;
                case 3:
					Vector3Int corner3 = new Vector3Int(startingPlace.x - width * 3, yCoord, startingPlace.z - width * 3);

					for (int z = 0; z < width; z++)
					{
						corner3.z += 3;

						if (coastTiles.Contains(corner3) || riverTiles.Contains(corner3))
                        {
                            if (TryClosestFlatland(corner3, out Vector3Int tile))
                                return tile;
                        }
					}

					trys++;
					direction = 0;
					break;
            }
        }

        return new Vector3Int(0, -10, 0);
	}

    private bool TryClosestFlatland(Vector3Int waterTile, out Vector3Int foundTile)
    {
        List<(Vector3Int, int)> terrainLocs = new(); 
        
        for (int i = 0; i < ProceduralGeneration.neighborsFourDirections.Count; i++)
        {
            Vector3Int tile = waterTile + ProceduralGeneration.neighborsFourDirections[i];
            
            if (terrainDict[tile].CompareTag("Flatland") || terrainDict[tile].CompareTag("Forest"))
            {
                terrainLocs.Add((tile, Mathf.Abs(waterTile.x - tile.x) + Mathf.Abs(waterTile.z - tile.z)));
            }
        }

		terrainLocs.Sort((a, b) => a.Item2.CompareTo(b.Item2));

        if (terrainLocs.Count == 0)
        {
            foundTile = Vector3Int.zero;
            return false;
        }
        else
        {
            foundTile = terrainLocs[0].Item1;
			return true;
        }
	}

    private List<Vector3Int> PlaceTradeCenters(System.Random random, List<Vector3Int> tradeCenterLocs, Vector3Int startingPlace, List<Vector3Int> coastTiles, List<Vector3Int> riverTiles)
    {
        List<Vector3Int> placeChecks = new(tradeCenterLocs) { startingPlace };
        List<Vector3Int> waterTiles = new(coastTiles);
        waterTiles.AddRange(riverTiles);
        int count = tradeCenterLocs.Count;
        
        for (int i = count; i < tradeCenterCount; i++)
        {
            bool spotLooking = true;
            
            while (spotLooking)
            {
                Vector3Int potentialSpot = waterTiles[random.Next(0, waterTiles.Count)];
                waterTiles.Remove(potentialSpot);

                for (int j = 0; j < placeChecks.Count; j++)
                {
                    int distance = Mathf.Abs(placeChecks[j].x - potentialSpot.x) + Mathf.Abs(placeChecks[j].z - potentialSpot.z);
                    
                    if (distance < tradeCenterDistance)
                    {
                        spotLooking = true;
                        break;
                    }
                    else
                    {
                        spotLooking = false;
                    }
                }

                if (!spotLooking)
                {
                    spotLooking = true;
                    for (int j = 0; j < ProceduralGeneration.neighborsFourDirections.Count; j++)
                    {
                        Vector3Int tile = ProceduralGeneration.neighborsFourDirections[j] + potentialSpot;

                        if (terrainDict[tile].CompareTag("Flatland") || terrainDict[tile].CompareTag("Forest"))
                        {
                            spotLooking = false;
                            tradeCenterLocs.Add(tile);
                            break;
                        }
					}
                }
            }
        }
    
        return tradeCenterLocs;
    }

    private (List<Vector3Int>, List<Vector3Int>, List<Vector3Int>, List<Vector3Int>) GenerateResources(System.Random random, 
        Vector3Int startingPlace, List<Vector3Int> tradeCenterLocs, List<Vector3Int> grasslandLocs, List<Vector3Int> riverTiles, List<Vector3Int> coastTiles)
    {
        List<Vector3Int> startingPlaceRange = new() { startingPlace };
        List<Vector3Int> tradeCenterRange = new();
        List<Vector3Int> finalResourceLocs = new();
        List<Vector3Int> finalWaterLocs = new();
        List<Vector3Int> finalFoodLocs = new();
        List<Vector3Int> luxuryResourceLocs = new();
        List<Vector3Int> potentialWaterResourceLocs = new(riverTiles);
        List<Vector3Int> newCoastTiles = new(coastTiles); 
        potentialWaterResourceLocs.AddRange(newCoastTiles);

        for (int i = 0; i < ProceduralGeneration.neighborsCityRadius.Count; i++)
        {
            Vector3Int tile = ProceduralGeneration.neighborsCityRadius[i] + startingPlace;

			startingPlaceRange.Add(tile);
            potentialWaterResourceLocs.Remove(tile);
            grasslandLocs.Remove(tile);
        }

        for (int i = 0; i < tradeCenterLocs.Count; i++)
        {
            tradeCenterRange.Add(tradeCenterLocs[i]);

			for (int j = 0; j < ProceduralGeneration.neighborsEightDirections.Count; j++)
            {
                Vector3Int tile = ProceduralGeneration.neighborsEightDirections[j] + tradeCenterLocs[i];
                tradeCenterRange.Add(tile);
				potentialWaterResourceLocs.Remove(tile);
			}
        }

        int xCount = width / resourceFrequency - 1;
        int zCount = height / resourceFrequency - 1;

        int totalResourcePlacements = xCount * zCount;
        int goldAmount = Mathf.CeilToInt(gold * totalResourcePlacements / 100);
        int jewelAmount = Mathf.CeilToInt(jewel * totalResourcePlacements / 100);
        int clayAmount = Mathf.CeilToInt(clay * totalResourcePlacements / 100);
        int clothAmount = Mathf.CeilToInt(cloth * totalResourcePlacements / 100);
        int stoneAmount = Mathf.CeilToInt(stone * totalResourcePlacements / 100);
        int rocksAmount = stoneAmount + goldAmount + jewelAmount;

        List<Vector3Int> potentialResourceLocs = new();

        //finding tiles to place resources
		for (int i = 1; i <= xCount; i++)
        {
            for (int j = 1; j <= zCount; j++)
            {
                Vector3Int potentialLoc = new Vector3Int(i * random.Next(resourceFrequency-1, resourceFrequency + 1) * 3, 
                    yCoord, j * random.Next(resourceFrequency - 1, resourceFrequency + 1) * 3);

                if (startingPlaceRange.Contains(potentialLoc) || tradeCenterRange.Contains(potentialLoc))
                    continue;

                if (terrainDict[potentialLoc].isLand && terrainDict[potentialLoc].walkable)
                {
                    potentialResourceLocs.Add(potentialLoc);
                }
                else
                {
                    for (int k = 0; k < ProceduralGeneration.neighborsEightDirections.Count; k++)
                    {
                        Vector3Int tile = potentialLoc + ProceduralGeneration.neighborsEightDirections[k];

                        if (terrainDict[tile].isLand && terrainDict[tile].walkable && !startingPlaceRange.Contains(tile) && !tradeCenterRange.Contains(tile))
                        {
                            potentialResourceLocs.Add(tile);
                            break;
                        }
                    }
                }
            }
        }

        //filling in gaps in case some missed
        int gap = totalResourcePlacements - potentialResourceLocs.Count;
		if (gap > 0)
        {
            for (int i = 0; i < gap; i++)
            {
                Vector3Int potentialNewSpot = potentialResourceLocs[random.Next(0, potentialResourceLocs.Count)];

				for (int k = 0; k < ProceduralGeneration.neighborsCityRadius.Count; k++)
				{
					Vector3Int tile = potentialNewSpot + ProceduralGeneration.neighborsCityRadius[k];

					if (terrainDict[tile].isLand && terrainDict[tile].walkable && !startingPlaceRange.Contains(tile) && !tradeCenterRange.Contains(tile))
					{
						potentialResourceLocs.Add(tile);
						break;
					}
				}
			}
        }

		//placing cloth material
		for (int i = 0; i < clothAmount; i++)
		{
			bool foundLocation = false;
			Vector3Int selectedTile = Vector3Int.zero;
			int attempts = 0;

			while (!foundLocation && attempts < 5)
			{
				Vector3Int randomTile = potentialResourceLocs[random.Next(0, potentialResourceLocs.Count)];

				if (!terrainDict[randomTile].isHill && terrainDict[randomTile].terrainData.grassland)
				{
					foundLocation = true;
					selectedTile = randomTile;
					potentialResourceLocs.Remove(randomTile);
                    grasslandLocs.Remove(randomTile);
				}
				else
				{
					for (int j = 0; j < ProceduralGeneration.neighborsEightDirections.Count; j++)
					{
						Vector3Int tile = ProceduralGeneration.neighborsEightDirections[j] + randomTile;

						if (!terrainDict[tile].isHill && terrainDict[tile].terrainData.grassland && !startingPlaceRange.Contains(tile) && !tradeCenterRange.Contains(tile) 
                            && !finalResourceLocs.Contains(tile) && terrainDict[tile].walkable)
						{
							selectedTile = tile;
							foundLocation = true;
							potentialResourceLocs.Remove(randomTile);
							break;
						}
					}
				}

				if (!foundLocation)
					attempts++;
			}

			if (foundLocation)
			{
                SwampCheck(selectedTile);
                FloodPlainCheck(selectedTile);
                GenerateClothTile(random, selectedTile);
				if (!finalResourceLocs.Contains(selectedTile))
					finalResourceLocs.Add(selectedTile);
			}
		}

		//placing clay
		for (int i = 0; i < clayAmount; i++)
		{
            bool foundLocation = false;
            Vector3Int selectedTile = Vector3Int.zero;
            int attempts = 0;

            while (!foundLocation && attempts < 5)
            {
                Vector3Int randomTile = potentialResourceLocs[random.Next(0, potentialResourceLocs.Count)];

			    if (terrainDict[randomTile].isHill)
			    {
                    for (int j = 0; j < ProceduralGeneration.neighborsEightDirections.Count; j++)
                    {
                        Vector3Int tile = ProceduralGeneration.neighborsEightDirections[j] + randomTile;
                    
                        if (terrainDict[tile].CompareTag("Flatland") && !startingPlaceRange.Contains(tile) && !tradeCenterRange.Contains(tile) && !finalResourceLocs.Contains(tile))
                        {
                            selectedTile = tile;
                            foundLocation = true;
							potentialResourceLocs.Remove(randomTile);
							break;
                        }
                    }
			    }
                else
                {
                    foundLocation = true;
                    selectedTile = randomTile;
					potentialResourceLocs.Remove(randomTile);
					grasslandLocs.Remove(randomTile);
				}

                if (!foundLocation)
                    attempts++;
            }

            if (foundLocation)
            {
                SwampCheck(selectedTile);
				FloodPlainCheck(selectedTile);
				terrainDict[selectedTile].terrainData = terrainDict[selectedTile].terrainData.grassland ? grasslandResourceSO : desertResourceSO;
			    terrainDict[selectedTile].resourceType = ResourceType.Clay;
			    terrainDict[selectedTile].rawResourceType = RawResourceType.Clay;
			    terrainDict[selectedTile].decorIndex = 3;
                terrainDict[selectedTile].gameObject.tag = "Flatland";
                terrainDict[selectedTile].resourceAmount = -1;
				if (!finalResourceLocs.Contains(selectedTile))
					finalResourceLocs.Add(selectedTile);
            }
		}

		//placing rocks
		for (int i = 0; i < rocksAmount; i++)
        {
            ResourceType type;
            int index;
            int adjacentCount;
            int amountMin;
            int amountMax;
            bool luxury = false;

            //specifying which rocks to place
            if (i < goldAmount)
            {
                luxury = true;
                type = ResourceType.GoldOre;
				index = 2;
				adjacentCount = goldAdjacent;
				amountMin = goldAmounts.Item1;
				amountMax = goldAmounts.Item2;
			}
            else if (i < goldAmount + jewelAmount)
            {
                luxury = true;
                type = ResourceType.Ruby;
				index = 1;
				adjacentCount = jewelAdjacent;
				amountMin = jewelAmounts.Item1;
				amountMax = jewelAmounts.Item2;
			}
            else
            {
                type = ResourceType.Stone;
                index = 0;
                adjacentCount = stoneAdjacent;
                amountMin = stoneAmounts.Item1;
                amountMax = stoneAmounts.Item2;
            }
            
            Vector3Int randomTile = potentialResourceLocs[random.Next(0, potentialResourceLocs.Count)];
            SwampCheck(randomTile);
			FloodPlainCheck(randomTile);

            if (terrainDict[randomTile].isHill)
            {
                terrainDict[randomTile].terrainData = terrainDict[randomTile].terrainData.grassland ? grasslandHillResourceSO : desertHillResourceSO;
            }
            else
            {
				terrainDict[randomTile].terrainData = terrainDict[randomTile].terrainData.grassland ? grasslandResourceSO : desertResourceSO;
			}

			terrainDict[randomTile].resourceType = type;
            terrainDict[randomTile].rawResourceType = RawResourceType.Rocks;
            terrainDict[randomTile].resourceAmount = random.Next(amountMin, amountMax);
            terrainDict[randomTile].decorIndex = index;

            if (luxury && !luxuryResourceLocs.Contains(randomTile))
                luxuryResourceLocs.Add(randomTile);
            if (!finalResourceLocs.Contains(randomTile))
                finalResourceLocs.Add(randomTile);

            //placing adjacent rocks
            List<Vector3Int> neighbors = new(ProceduralGeneration.neighborsEightDirections);
            for (int j = 0; j < adjacentCount; j++)
            {
                Vector3Int potentialNeighbor = neighbors[random.Next(0, neighbors.Count)];
                neighbors.Remove(potentialNeighbor);
                Vector3Int newTile = potentialNeighbor + randomTile;

				if (terrainDict[newTile].isLand && terrainDict[newTile].walkable && !startingPlaceRange.Contains(newTile) && !tradeCenterRange.Contains(newTile) && !finalResourceLocs.Contains(newTile))
                {
                    SwampCheck(newTile);
					FloodPlainCheck(newTile);
                    
                    if (terrainDict[newTile].isHill)
				    {
					    terrainDict[newTile].terrainData = terrainDict[newTile].terrainData.grassland ? grasslandHillResourceSO : desertHillResourceSO;
				    }
				    else
				    {
					    terrainDict[newTile].terrainData = terrainDict[newTile].terrainData.grassland ? grasslandResourceSO : desertResourceSO;
				    }

					terrainDict[newTile].resourceType = type;
					terrainDict[newTile].rawResourceType = RawResourceType.Rocks;
					terrainDict[newTile].resourceAmount = random.Next(amountMin, amountMax);
                    terrainDict[newTile].decorIndex = index;
                    if (luxury && !luxuryResourceLocs.Contains(newTile))
                        luxuryResourceLocs.Add(newTile);
					if (!finalResourceLocs.Contains(newTile))
						finalResourceLocs.Add(newTile);

					grasslandLocs.Remove(newTile);
				}                
			}

			potentialResourceLocs.Remove(randomTile);
			grasslandLocs.Remove(randomTile);
		}

        //put food every 3rd remaining grasslandtile
        int grasslandCount = grasslandLocs.Count / 3;
        for (int i = 0; i < grasslandCount; i++)
        {
			Vector3Int newFood = grasslandLocs[random.Next(0, grasslandLocs.Count)];

			terrainDict[newFood].terrainData = grasslandResourceSO;
			terrainDict[newFood].resourceType = ResourceType.Food;
			terrainDict[newFood].rawResourceType = RawResourceType.FoodLand;
			terrainDict[newFood].decorIndex = 4;
			terrainDict[newFood].resourceAmount = -1;
			terrainDict[newFood].uvMapIndex.Add(random.Next(0, 0)); //for determining what flowers to show
			terrainDict[newFood].uvMapIndex.Add(random.Next(0, 2));

			grasslandLocs.Remove(newFood);
            finalFoodLocs.Add(newFood);
		}

        //put fish every fourth tile on average
        int fishCount = potentialWaterResourceLocs.Count / 4;
        for (int i = 0; i < fishCount; i++)
        {
            Vector3Int newWater = potentialWaterResourceLocs[random.Next(0, potentialWaterResourceLocs.Count)];
			bool river = terrainDict[newWater].terrainData.type == TerrainType.River;

			terrainDict[newWater].terrainData = river ? riverResourceSO : seaResourceSO;
			terrainDict[newWater].resourceType = ResourceType.Fish;
			terrainDict[newWater].rawResourceType = RawResourceType.FoodSea;
			terrainDict[newWater].decorIndex = 0;
			terrainDict[newWater].resourceAmount = -1;

			potentialWaterResourceLocs.Remove(newWater);
            finalWaterLocs.Add(newWater);
        }

		return (luxuryResourceLocs, finalFoodLocs, finalWaterLocs, finalResourceLocs);
    }

    private void GenerateClothTile(System.Random random, Vector3Int loc)
    {
		int clothType = random.Next(0, 3);
		ResourceType type;
		RawResourceType rawType;
		int index;

		if (clothType == 0)
		{
			type = ResourceType.Wool;
			rawType = RawResourceType.Wool;
			index = 7;
		}
		else if (clothType == 1)
		{
			type = ResourceType.Cotton;
			rawType = RawResourceType.Cotton;
			index = 5;
		}
		else
		{
			type = ResourceType.Silk;
			rawType = RawResourceType.Silk;
			index = 6;
		}

		terrainDict[loc].terrainData = grasslandResourceSO;
		terrainDict[loc].resourceType = type;
		terrainDict[loc].rawResourceType = rawType;
		terrainDict[loc].decorIndex = index;
        terrainDict[loc].gameObject.tag = "Flatland";
		terrainDict[loc].resourceAmount = -1;
	}

    private void SwampCheck(Vector3Int tile)
    {
		if (terrainDict[tile].terrainData.terrainDesc == TerrainDesc.Swamp)
		{
			DestroyImmediate(terrainDict[tile].gameObject);
			TerrainData td = GenerateTile(grasslandSO.prefabs[0], tile, Quaternion.identity, 0);
            td.gameObject.tag = "Flatland";
		}
	}

    private void FloodPlainCheck(Vector3Int tile)
    {
		if (terrainDict[tile].terrainData.specificTerrain == SpecificTerrain.FloodPlain)
		{
			TerrainDataSO data = terrainDict[tile].terrainData.grassland ? grasslandSO : desertSO;
			DestroyImmediate(terrainDict[tile].gameObject);
			GenerateTile(data.prefabs[0], tile, Quaternion.identity, 0);
		}
	}

	private List<Vector3Int> GenerateEnemyCamps(System.Random random, Vector3Int startingPlace, List<Vector3Int> tradeCenterLocs, List<Vector3Int> landLocs, List<Vector3Int> luxuryLocs, List<Vector3Int> resourceLocs)
	{
        List<Vector3Int> enemyLocs = new();
        List<Vector3Int> locsLeft = new(landLocs);
        locsLeft.Remove(startingPlace);

		//no enemies within 4 tiles of starting place 
		for (int i = 0; i < ProceduralGeneration.neighborsCityRadius.Count; i++)
        {
            Vector3Int tile = ProceduralGeneration.neighborsCityRadius[i] + startingPlace;

            if (locsLeft.Contains(tile))
            {
				locsLeft.Remove(tile);
				resourceLocs.Remove(tile);
				luxuryLocs.Remove(tile);
			}

            if (i > 7)
            {
                for (int j = 0; j < ProceduralGeneration.neighborsCityRadius.Count; j++)
                {
                    Vector3Int newTile = ProceduralGeneration.neighborsCityRadius[j] + tile;

                    if (locsLeft.Contains(newTile))
                    {
						locsLeft.Remove(newTile);
                        resourceLocs.Remove(newTile);
                        luxuryLocs.Remove(newTile);
                    }
                }
                
            }
        }

        //no enemies with in 2 tiles of trade centers
        for (int i = 0; i < tradeCenterLocs.Count; i++)
        {
			locsLeft.Remove(tradeCenterLocs[i]);

            for (int j = 0; j < ProceduralGeneration.neighborsCityRadius.Count; j++)
            {
                Vector3Int tile = ProceduralGeneration.neighborsCityRadius[j] + tradeCenterLocs[i];

                if (locsLeft.Contains(tile))
                {
					locsLeft.Remove(tile);
					resourceLocs.Remove(tile);
					luxuryLocs.Remove(tile);
				}
            }
        }

        //no enemies between start loc and trade center (if close enough)
        if (Mathf.Abs(tradeCenterLocs[0].x - startingPlace.x) + Mathf.Abs(tradeCenterLocs[0].z - startingPlace.z) < 37)
        {
            Vector3Int vectorDiff = tradeCenterLocs[0] - startingPlace;

            bool pos = vectorDiff.x * vectorDiff.z >= 0;
            
            int xMax = Math.Max(tradeCenterLocs[0].x, startingPlace.x);
		    int xMin = Math.Min(tradeCenterLocs[0].x, startingPlace.x);
		    int zMax = Math.Max(tradeCenterLocs[0].z, startingPlace.z);
		    int zMin = Math.Min(tradeCenterLocs[0].z, startingPlace.z);

            int xDiff = (xMax - xMin) / 3;
            int zDiff = (zMax - zMin) / 3;

            if (xDiff > zDiff)
            {
                int stepIntervals = xDiff / (zDiff + 1);

                int nextZ = pos ? zMin : zMax;
                int increment = pos ? 3 : -3;

				for (int i = 0; i < xDiff; i++)
                {
                    if (i % stepIntervals == 0)
                        nextZ += increment;
                    
                    Vector3Int tile = new Vector3Int(xMin + i * 3, yCoord, nextZ);

					if (locsLeft.Contains(tile))
                    {
						locsLeft.Remove(tile);
						resourceLocs.Remove(tile);
						luxuryLocs.Remove(tile);
					}

                    for (int j = 0; j < ProceduralGeneration.neighborsCityRadius.Count; j++)
                    {
                        Vector3Int nextTile = ProceduralGeneration.neighborsCityRadius[j] + tile;

						if (locsLeft.Contains(nextTile))
                        {
							locsLeft.Remove(nextTile);
							resourceLocs.Remove(nextTile);
                            luxuryLocs.Remove(nextTile);
						}
					}
				}
            }
            else if (zDiff > xDiff)
            {
				int stepIntervals = zDiff / (xDiff + 1);

				int nextX = pos ? xMin : xMax;
				int increment = pos ? 3 : -3;

				for (int i = 0; i < zDiff; i++)
				{
					if (i % stepIntervals == 0)
						nextX += increment;

					Vector3Int tile = new Vector3Int(nextX, yCoord, zMin + i * 3);

					if (locsLeft.Contains(tile))
                    {
						locsLeft.Remove(tile);
						resourceLocs.Remove(tile);
						luxuryLocs.Remove(tile);
					}

					for (int j = 0; j < ProceduralGeneration.neighborsCityRadius.Count; j++)
					{
						Vector3Int nextTile = ProceduralGeneration.neighborsCityRadius[j] + tile;

						if (locsLeft.Contains(nextTile))
                        {
							locsLeft.Remove(nextTile);
							resourceLocs.Remove(nextTile);
							luxuryLocs.Remove(tile);
						}
					}
				}
			}
            else
            {
				for (int i = 0; i < xDiff; i++)
				{
					Vector3Int tile = new Vector3Int(xMin + i * 3, yCoord, zMin + i * 3);

                    if (locsLeft.Contains(tile))
                    {
						locsLeft.Remove(tile);
                        resourceLocs.Remove(tile);
						luxuryLocs.Remove(tile);
					}

					for (int j = 0; j < ProceduralGeneration.neighborsCityRadius.Count; j++)
					{
						Vector3Int nextTile = ProceduralGeneration.neighborsCityRadius[j] + tile;

						if (locsLeft.Contains(nextTile))
                        {
							locsLeft.Remove(nextTile);
							resourceLocs.Remove(nextTile);
							luxuryLocs.Remove(nextTile);
						}
					}
				}
			}
        }

        int divisor; 

		if (enemyCountDifficulty == 1)
            divisor = 20;
        else if (enemyCountDifficulty == 2)
            divisor = 16;
        else
            divisor = 12;

        int targetCampsCount = locsLeft.Count / divisor;

        //adding enemy camps (minimum gap of 2 tiles)
        while (targetCampsCount > 0 && locsLeft.Count > 0)
        {
            Vector3Int chosenTile;
            bool luxury = false;
            if (luxuryLocs.Count > 0)
            {
				chosenTile = luxuryLocs[random.Next(0, luxuryLocs.Count)];
                luxuryLocs.Remove(chosenTile);
				resourceLocs.Remove(chosenTile);
                luxury = true;
			}
            else if (resourceLocs.Count > 0)
            {
                chosenTile = resourceLocs[random.Next(0, resourceLocs.Count)];
                resourceLocs.Remove(chosenTile);
			}
            else
            {
                chosenTile = locsLeft[random.Next(0,locsLeft.Count)];
			}

            locsLeft.Remove(chosenTile);
            CreateCamp(random, Mathf.Abs(chosenTile.x - startingPlace.x) + Mathf.Abs(chosenTile.z - startingPlace.z), chosenTile, luxury);
            enemyLocs.Add(chosenTile);
            terrainDict[chosenTile].enemyCamp = true;
            terrainDict[chosenTile].enemyZone = true;
            
            for (int i = 0; i < ProceduralGeneration.neighborsCityRadius.Count; i++)
            {
                Vector3Int tile = ProceduralGeneration.neighborsCityRadius[i] + chosenTile;
                
                if (i < 8)
                    terrainDict[tile].enemyZone = true;

                if (locsLeft.Contains(tile))
                {
				    locsLeft.Remove(tile);
					resourceLocs.Remove(tile);
					luxuryLocs.Remove(tile);
				}
            }
            
            targetCampsCount--;
        }

		return enemyLocs;
	}

	private void CreateCamp(System.Random random, int startProximity, Vector3Int campLoc, bool luxury)
    {
        int campCount;
        int infantryCount = 0;
        int rangedCount = 0;
        int cavalryCount = 0;
        
        if (startProximity < 37)
        {
            if (enemyCountDifficulty == 1)
				campCount = 1;
            else if (enemyCountDifficulty == 2)
				campCount = 2;
			else
				campCount = 3;
		}
        else if (startProximity < 52)
        {
			if (enemyCountDifficulty == 1)
				campCount = 4;
			else if (enemyCountDifficulty == 2)
				campCount = 5;
			else
				campCount = 6;
		}
        else
        {
			if (enemyCountDifficulty == 1)
				campCount = 7;
			else if (enemyCountDifficulty == 2)
				campCount = 8;
			else
				campCount = 9;
		}

        if (luxury)
        {
            if (campCount < 8)
                campCount += 2;
            else
                campCount = 9;
        }

        Vector3 centerLoc = campLoc;
        if (terrainDict[campLoc].isHill)
            centerLoc.y += .65f;

        List<Vector3> campLocs = new() { centerLoc };

        if (terrainDict[campLoc].isHill)
            centerLoc.y -= 0.5f;

        for (int i = 0; i < ProceduralGeneration.neighborsEightDirectionsOneStep.Count; i++)
            campLocs.Add(ProceduralGeneration.neighborsEightDirectionsOneStep[i] + centerLoc);

        //0 infantry, 1 ranged, 2 cavalry
        List<int> unitList = new() { 0, 1, 2 };

        for (int i = 0; i < campCount; i++)
        {
            Vector3 spawnLoc = campLocs[random.Next(0, campLocs.Count)];
            campLocs.Remove(spawnLoc);

            int chosenUnitType = unitList[random.Next(0, unitList.Count)];
            GameObject enemy = enemyUnits[chosenUnitType];
            UnitType type = enemy.GetComponent<Unit>().buildDataSO.unitType;

            if (type == UnitType.Infantry)
            {
                infantryCount++;
                if (infantryCount >= infantryMax)
                    unitList.Remove(0);
            }
            else if (type == UnitType.Ranged)
			{
				rangedCount++;
                if (rangedCount >= rangedMax)
                    unitList.Remove(1);
			}
            else if (type == UnitType.Cavalry)
			{
				cavalryCount++;
				if (cavalryCount >= cavalryMax)
					unitList.Remove(2);
			}

			Quaternion rotation = Quaternion.Euler(0, random.Next(0,8) * 45, 0);

            GameObject enemyGO = Instantiate(enemy, spawnLoc, rotation);
            allTiles.Add(enemyGO);
            enemyGO.transform.SetParent(enemyHolder, false);
        }
    }

	//private void SetEnemyBorders(List<Vector3Int> enemyLocs)
	//{
 //       for (int i = 0; i < enemyLocs.Count; i++)
 //       {
 //           for (int j = 0; j < ProceduralGeneration.neighborsEightDirections.Count; j++)
 //           {
 //               Vector3Int tile = enemyLocs[i] + ProceduralGeneration.neighborsEightDirections[j];

 //               for (int k = 0; k < ProceduralGeneration.neighborsFourDirections.Count; k++)
 //               {
 //                   Vector3Int newTile = tile + ProceduralGeneration.neighborsFourDirections[k];

	//				if (!terrainDict[newTile].enemyZone)
 //                   {
	//					Vector3 borderPosition = newTile - tile;
	//					borderPosition.y = -0.1f;
 //                       Quaternion rotation = Quaternion.identity;

	//					if (borderPosition.x != 0)
	//					{
	//						borderPosition.x = (borderPosition.x / 3 * 0.99f);
	//						rotation = Quaternion.Euler(0, 90, 0); //only need to rotate on this one
	//					}
	//					else if (borderPosition.z != 0)
	//					{
	//						borderPosition.z = (borderPosition.z / 3 * 0.99f);
	//					}

 //                       GameObject border = Instantiate(enemyBorder, borderPosition, rotation);
 //                       allTiles.Add(border);
 //                       border.transform.SetParent(terrainDict[tile].transform, false);
 //                       border.SetActive(false);
 //                       terrainDict[tile].enemyBorders.Add(border);
 //                   }
 //               }
 //           }
 //       }
	//}

    public void SetMainPlayerLoc() 
    {
        world.mainPlayer.transform.position = startingPlace;
		world.mainPlayer.CenterCamera();
        world.cameraController.ResetCamLimits(world.RoundToInt(world.mainPlayer.transform.position));
        world.water.transform.position = new Vector3(width / 2f * 3, yCoord - 0.06f, height / 2f * 3);
        world.water.minimapIcon.localScale = new Vector3(0.14f * width, 1.8f, 0.14f * height);
	}

    public void SetYCoord(int yCoord)
    {
        this.yCoord = yCoord;
    }

    public void Clear()
    {
        allTiles.Clear();
        terrainDict.Clear();
    }
}


