using System;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;
    [SerializeField]
    private GameObject enemyBorder, improvementResource;

    [Header("Era & Region")]
    [SerializeField]
    private Era newEra;
    [SerializeField]
    public Region newRegion;

    [Header("General Map Parameters")]
    [SerializeField]
    public int seed = 4;
    [SerializeField]
    public int width = 50, height = 50, yCoord = 3, landMassLimit = 1, totalLandLimit = 400, desertPerc = 30, forestAndJunglePerc = 70, mountainPerc = 10, mountainousPerc = 80, 
        mountainRangeLength = 20, equatorDist = 10, equatorPos = 25,/*riverPerc = 5, */riverCountMin = 10, oceanRingDepth = 2, startingSpotGrasslandCountMin = 5, startingSpotGrasslandCountMax = 20,
        tradeCenterCount = 1, tradeCenterDistance = 30, resourceFrequency = 4, enemyCountDifficulty = 1;
    [HideInInspector]
    public int continentsFlag, resourceFlag;

    [Header("Natural Resource Percs (sum to 100)")]
    [SerializeField]
    private int stone = 40;
    [SerializeField]
    private int iron = 10, copper = 10, gold = 5, jewel = 5, clay = 15, cloth = 15;

    [Header("Count of adjacent tiles with resource")]
    [SerializeField]
    private int stoneAdjacent = 2;
    [SerializeField]
    private int goldAdjacent = 1, jewelAdjacent = 0;

    [Header("Range of Resource Amounts")]
    [SerializeField]
    private (int, int) stoneAmounts = (1000, 10000);
    [SerializeField]
    private (int, int) ironAmounts = (500, 1000), copperAmounts = (500, 1000), goldAmounts = (200, 500), jewelAmounts = (100, 300);
    private int goldAmount, jewelAmount, ironAmount, copperAmount, stoneAmount, clayAmount, clothAmount, goldPlaced, jewelPlaced, ironPlaced, copperPlaced, stonePlaced;

    [SerializeField]
	private bool startingGame = true, nonBuildTime = false;

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
    public int iterations = 15;
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
    private Transform tradeCenterHolder, enemyHolder, enemyCityHolder;

    [Header("Terrain Data SO")]
    [SerializeField]
    private TerrainDataSO coastSO;
    [SerializeField]
    private TerrainDataSO desertFloodPlainsSO, desertHillResourceSO, desertHillSO, desertMountainSO, desertResourceSO, desertSO, forestHillSO, forestSO, 
        grasslandFloodPlainsSO, grasslandHillResourceSO, grasslandHillSO, grasslandMountainSO, grasslandResourceSO, grasslandSO, 
        jungleHillSO, jungleSO, riverSO, riverResourceSO, seaIntersectionSO, seaSO, seaResourceSO, swampSO;

    [Header("Misc")]
    [SerializeField]
    private GameObject mountainMiddle;

    [Header("Trade Center Prefabs")]
    [SerializeField]
    private List<GameObject> tradeCenters;
    [SerializeField]
    private List<string> tradeCenterNames;

    [Header("Enemy Leader Prefabs")]
    [SerializeField]
    private List<UnitBuildDataSO> enemyLeaders;

    [Header("Enemy Unit Prefabs (use same order)")]
    [SerializeField]
    public List<UnitBuildDataSO> enemyUnits;

    [HideInInspector]
    public List<Vector3Int> enemyCityLocs = new(), enemyRoadLocs = new(), enemyCampLocs = new();
    [HideInInspector]
    public List<EnemyEmpire> enemyEmpires = new();

    [Header("Enemy Camp Parameters")]
    [SerializeField] 
    private int infantryMax = 9;
    [SerializeField]
    private int rangedMax = 6, cavalryMax = 9;

    private List<GameObject> allTiles = new();

    public bool autoUpdate, explored;

    public ResourceHolder resourceHolder;

    public Dictionary<Vector3Int, TerrainData> terrainDict = new();
    private Vector3Int startingPlace;
    private List<TerrainData> propTiles = new();
    private List<Vector3Int> tradeCenterPositions = new();
    private List<Vector3Int> fourWayRiverLocs = new();

    //unit dicts
    public Dictionary<Era, Dictionary<Region, List<GameObject>>> enemyLeaderDict = new(); 
    public Dictionary<Era, Dictionary<Region, Dictionary<UnitType, GameObject>>> enemyUnitDict = new();

	private void OnDrawGizmos() //for highlighting difficulty of terrain
	{
		if (Application.isPlaying)
			return;
		
        CreateCityIndicator(startingPlace, Color.green, nonBuildTime);

        for (int i = 0; i < tradeCenterPositions.Count; i++)
            CreateCityIndicator(tradeCenterPositions[i], new Color(0.5f, 0, 1), nonBuildTime);

		for (int i = 0; i < enemyCityLocs.Count; i++)
			CreateCityIndicator(enemyCityLocs[i], Color.red, nonBuildTime);

        for (int i = 0; i < enemyRoadLocs.Count; i++)
            CreateCityIndicator(enemyRoadLocs[i], new Color(0.4f, 0.3f, 0.3f), nonBuildTime);

		for (int i = 0; i < enemyCampLocs.Count; i++)
			CreateCityIndicator(enemyCampLocs[i], new Color(1, 0, 1), nonBuildTime);
	}

    private void PopulateUnitDicts()
    {
        foreach (UnitBuildDataSO data in enemyLeaders)
        {
            if (!enemyLeaderDict.ContainsKey(data.unitEra))
                enemyLeaderDict[data.unitEra] = new();

            if (!enemyLeaderDict[data.unitEra].ContainsKey(data.unitRegion))
                enemyLeaderDict[data.unitEra][data.unitRegion] = new();

            enemyLeaderDict[data.unitEra][data.unitRegion].Add(Resources.Load<GameObject>("Prefabs/" + data.prefabLoc));
        }

        foreach (UnitBuildDataSO data in enemyUnits)
        {
            if (!enemyUnitDict.ContainsKey(data.unitEra))
                enemyUnitDict[data.unitEra] = new();
            
            if (!enemyUnitDict[data.unitEra].ContainsKey(data.unitRegion))
                enemyUnitDict[data.unitEra][data.unitRegion] = new();
            
            enemyUnitDict[data.unitEra][data.unitRegion][data.unitType] = Resources.Load<GameObject>("Prefabs/" + data.prefabLoc);
		}
    }

	public void GenerateMap()
    {
        terrainDict.Clear();
        RunProceduralGeneration(true);
    }

    public void RemoveMap()
    {
		RemoveAllTiles();
    }

    public void RunProceduralGeneration(bool newGame)
    {
        PopulateUnitDicts();
        
        RemoveAllTiles();
        retry = false;

        if (nonBuildTime)
        {
            resourceHolder.ClearDict();
            resourceHolder.PopulateDict();
        }

        System.Random random = new System.Random(seed);
        int[] rotate = new int[4] { 0, 90, 180, 270 };
        List<Vector3Int> landLocs = new();
        List<Vector3Int> riverTiles = new();
        List<Vector3Int> coastTiles = new();
        List<Vector3Int> grasslandTiles = new();
        List<Vector3Int> desertTiles = new();
        List<Vector3Int> mountainTiles = new();
        //List<TerrainData> propTiles = new();

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
            return;
        }

        Dictionary<Vector3Int, float> noise = ProceduralGeneration.PerlinNoiseGenerator(mainMap,
            scale, octaves, persistance, lacunarity, seed, offset);

        mainMap = ProceduralGeneration.GenerateTerrain(mainMap, noise, 0.45f, 0.65f, desertPerc, forestAndJunglePerc,
            width, height, yCoord, equatorDist, equatorPos, seed);

        Dictionary<Vector3Int, int> mountainMap = ProceduralGeneration.GenerateMountainRanges(mainMap, landMasses, mountainPerc, mountainousPerc, mountainRangeLength, 
            width, height, yCoord, seed);

        mountainMap = ProceduralGeneration.GenerateRivers(mountainMap, /*riverPerc, */riverCountMin, seed);

        mainMap = ProceduralGeneration.MergeMountainTerrain(random, mainMap, mountainMap, resourceFrequency);

        mainMap = ProceduralGeneration.AddOceanRing(mainMap, width, height, yCoord, oceanRingDepth);

        mainMap = ProceduralGeneration.ConvertOceanToRivers(mainMap);


        foreach (Vector3Int position in mainMap.Keys)
        {
            if (mainMap[position] == ProceduralGeneration.sea)
            {
                int prefabIndex = 0;
                GameObject sea = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + seaSO.prefabLocs[0]);
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
                        sea = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + coastSO.prefabLocs[3]);
						coastTiles.Add(position);
                        prefabIndex = 3;
					}
                    else
                    {
                        sea = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + seaIntersectionSO.prefabLocs[2]);
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
                                sea = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + coastSO.prefabLocs[4]);
								coastTiles.Add(position);
								prefabIndex = 4;
							}
                            else
                            {
                                sea = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + seaIntersectionSO.prefabLocs[3]);
								prefabIndex = 3;
							}
                        }
                        else if (neighborTerrainLoc[cornerTwo] == 1)
                        {
                            if (riverCount == 0)
                            {
                                sea = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + coastSO.prefabLocs[5]);
								coastTiles.Add(position);
								prefabIndex = 5;
							}
                            else
                            {
                                sea = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + seaIntersectionSO.prefabLocs[4]);

								prefabIndex = 4;
							}
                        }
                    }
                }
                else if (directNeighborCount == 2)
                {
                    sea = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + coastSO.prefabLocs[0]);
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
                                sea = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + coastSO.prefabLocs[1]);
								coastTiles.Add(position);
								prefabIndex = 1;
							}
                            else
                            {
                                sea = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + coastSO.prefabLocs[2]);
								coastTiles.Add(position);
								prefabIndex = 2;
							}
                        }
                        else if (riverIndex == min)
                        {
                            if (max - min == 1)
                            {
                                sea = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + coastSO.prefabLocs[2]);
								coastTiles.Add(position);
								prefabIndex = 2;
							}
                            else
                            {
                                sea = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + coastSO.prefabLocs[1]);
								coastTiles.Add(position);
								prefabIndex = 1;
							}
                        }
                    }
                    else if (riverCount == 2)
                    {
                        sea = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + seaIntersectionSO.prefabLocs[5]);
						prefabIndex = 5;
					}
                }
                else if (directNeighborCount == 3)
                {
                    sea = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + seaSO.prefabLocs[0]); 
					prefabIndex = 0;
					rotation = Quaternion.identity;
                }
                else if (directNeighborCount == 4)
                {
                    sea = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + seaSO.prefabLocs[0]);
					prefabIndex = 0;
					rotation = Quaternion.identity;
                }
                else
                {
                    if (neighborCount == 1)
                    {
                        sea = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + seaIntersectionSO.prefabLocs[0]);
						prefabIndex = 0;
						int index = Array.FindIndex(neighborTerrainLoc, x => x == 1);
                        rotation = Quaternion.Euler(0, index * 90, 0);
                    }
                    else if (neighborCount == 2)
                    {
                        sea = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + seaIntersectionSO.prefabLocs[1]);
						prefabIndex = 1;
						int index = Array.FindIndex(neighborTerrainLoc, x => x == 1);
                        rotation = Quaternion.Euler(0, index * 90, 0);
                    }
                    else if (neighborCount == 3)
                    {
                        sea = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + seaSO.prefabLocs[0]);
						prefabIndex = 0;
                        int index = Array.FindIndex(neighborTerrainLoc, x => x == 0);
                        rotation = Quaternion.Euler(0, index * 90, 0);
                    }
                }

                TerrainData td = GenerateTile(sea, position, rotation, prefabIndex, true);

                if (position.x < -3 || position.x > width * 3 + 3 || position.z < -3 || position.z > height * 3 + 3) //buffer of one tile since land can be build to edge
                    td.border = true;
            }
            else if (mainMap[position] == ProceduralGeneration.grasslandHill)
            {
                landLocs.Add(position);
                GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandHillSO.prefabLocs[0]), position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
            }
            else if (mainMap[position] == ProceduralGeneration.desertHill)
            {
				landLocs.Add(position);
				GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + desertHillSO.prefabLocs[0]), position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
            }
            else if (mainMap[position] == ProceduralGeneration.forestHill)
            {
				landLocs.Add(position);
				GameObject forestHill = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandHillSO.prefabLocs[0]);
				TerrainData td = GenerateTile(forestHill, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
                td.gameObject.tag = "Forest Hill";
                td.terrainData = forestHillSO;
                td.resourceAmount = -1;
                propTiles.Add(td);
            }
            else if (mainMap[position] == ProceduralGeneration.jungleHill)
            {
				landLocs.Add(position);
				GameObject jungleHill = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandHillSO.prefabLocs[0]);
				TerrainData td = GenerateTile(jungleHill, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
                td.gameObject.tag = "Forest Hill";
                td.terrainData = jungleHillSO;
                td.resourceAmount = -1;
				propTiles.Add(td);
			}
            else if (mainMap[position] == ProceduralGeneration.grasslandMountain)
            {
                mountainTiles.Add(position);
				int prefabIndex = random.Next(0, grasslandMountainSO.prefabLocs.Count); 
				GameObject grasslandMountain = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandMountainSO.prefabLocs[prefabIndex]);
				GenerateTile(grasslandMountain, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), prefabIndex);
            }
            else if (mainMap[position] == ProceduralGeneration.desertMountain)
            {
                mountainTiles.Add(position);
                int prefabIndex = random.Next(0, desertMountainSO.prefabLocs.Count);
				GameObject desertMountain = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + desertMountainSO.prefabLocs[prefabIndex]);
				GenerateTile(desertMountain, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), prefabIndex);
            }
            else if (mainMap[position] == ProceduralGeneration.grassland)
            {
				landLocs.Add(position);
				TerrainData td = GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandSO.prefabLocs[0]), position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
				//propTiles.Add(td);
				grasslandTiles.Add(position);
			}
            else if (mainMap[position] == ProceduralGeneration.desert)
            {
				landLocs.Add(position);
				TerrainData td = GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + desertSO.prefabLocs[0]), position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
				propTiles.Add(td);
                desertTiles.Add(position);
			}
            else if (mainMap[position] == ProceduralGeneration.forest)
            {
				landLocs.Add(position);
				GameObject forest = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandSO.prefabLocs[0]);
				TerrainData td = GenerateTile(forest, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
                td.gameObject.tag = "Forest";
                td.terrainData = forestSO;
                td.resourceAmount = -1;
				propTiles.Add(td);
			}
            else if (mainMap[position] == ProceduralGeneration.jungle)
            {
				landLocs.Add(position);
				GameObject jungle = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandSO.prefabLocs[0]);
				TerrainData td = GenerateTile(jungle, position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
                td.gameObject.tag = "Forest";
                td.terrainData = jungleSO;
                td.resourceAmount = -1;
				propTiles.Add(td);
			}
            else if (mainMap[position] == ProceduralGeneration.swamp)
            {
				landLocs.Add(position);
				TerrainData td = GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + swampSO.prefabLocs[0]), position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
				td.gameObject.tag = "Forest";
				td.terrainData = swampSO;
                td.resourceAmount = -1;
                propTiles.Add(td);
			}
            else if (mainMap[position] == ProceduralGeneration.river)
            {
                RotateRiverTerrain(random, rotate, mainMap, position, /*false, true, false, */out Quaternion rotation, out int riverInt);

                GameObject river;
                if (riverInt == 3)
                    riverInt += random.Next(0, 3);

                bool skipRiver = false;
                if (riverInt == 9999)
                {
                    skipRiver = true;
                    river = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandSO.prefabLocs[0]);
					riverInt = 0;
                    landLocs.Add(position);
                    grasslandTiles.Add(position);
                }
                else
                {
                    river = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + riverSO.prefabLocs[riverInt]);
				}

                TerrainData td = GenerateTile(river, position, rotation, riverInt, true);
                if (riverInt == 2)
                    fourWayRiverLocs.Add(position);
				else if (!skipRiver)
                    riverTiles.Add(position);
            }
            else if (mainMap[position] == ProceduralGeneration.grasslandFloodPlain)
            {
				landLocs.Add(position);
				TerrainData td = GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandFloodPlainsSO.prefabLocs[0]), position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
                td.resourceAmount = -1;
				propTiles.Add(td);
			}
            else if (mainMap[position] == ProceduralGeneration.desertFloodPlain)
            {
				landLocs.Add(position);
				TerrainData td = GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + desertFloodPlainsSO.prefabLocs[0]), position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
                td.resourceAmount = -1;
                propTiles.Add(td);
			}
            else
            {
				landLocs.Add(position);
				GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandSO.prefabLocs[0]), position, Quaternion.Euler(0, rotate[random.Next(0, 4)], 0), 0);
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
            return;
        }

		this.startingPlace = startingPlace;
		SwampCheck(startingPlace);
        FloodPlainCheck(startingPlace);
        ForestCheck(startingPlace);
        //generating resource counts
		int xCount = width / resourceFrequency - 1;
		int zCount = height / resourceFrequency - 1;

		int totalResourcePlacements = xCount * zCount;
		//all must have minimum of one
		goldAmount = Mathf.CeilToInt(gold * totalResourcePlacements / 100f);
        goldPlaced = 0;
		jewelAmount = Mathf.CeilToInt(jewel * totalResourcePlacements / 100f);
        jewelPlaced = 0;
		clayAmount = Mathf.CeilToInt(clay * totalResourcePlacements / 100f);
		clothAmount = Mathf.CeilToInt(cloth * totalResourcePlacements / 100f);
		ironAmount = Mathf.CeilToInt(iron * totalResourcePlacements / 100f);
        ironPlaced = 0;
		copperAmount = Mathf.CeilToInt(copper * totalResourcePlacements / 100f);
        copperPlaced = 0;
		stoneAmount = Mathf.CeilToInt(stone * totalResourcePlacements / 100f);
        stonePlaced = 0;

        List<Vector3Int> enemyFoodLocs = new(), enemyWaterLocs = new(), enemyResourceLocs = new();

        if (newGame)
        {
            GameObject chosenLeader = enemyLeaderDict[newEra][newRegion][0];
			(enemyFoodLocs, enemyWaterLocs, enemyResourceLocs) = GenerateEnemyCities(random, startingPlace, coastTiles, chosenLeader, true);
        }
        else
        {
            List<Vector3Int> tempEnemyFoodLocs = new(), tempEnemyWaterLocs = new(), tempEnemyResourceLocs = new();
			
            for (int i = 0; i < enemyLeaderDict[newEra][newRegion].Count; i++)
            {
                GameObject chosenLeader = enemyLeaderDict[newEra][newRegion][i];
                (tempEnemyFoodLocs, tempEnemyWaterLocs, tempEnemyResourceLocs) = GenerateEnemyCities(random, startingPlace, coastTiles, chosenLeader, false, enemyCityLocs);
                enemyFoodLocs.AddRange(tempEnemyFoodLocs);
                enemyWaterLocs.AddRange(tempEnemyWaterLocs);
                enemyResourceLocs.AddRange(tempEnemyResourceLocs);
            }
		}

		//setting up range of enemy cities so nothing is placed too close
		List<Vector3Int> enemyCityRange = new();
		for (int i = 0; i < enemyCityLocs.Count; i++)
		{
			enemyCityRange.Add(enemyCityLocs[i]);

			for (int j = 0; j < ProceduralGeneration.neighborsCityRadius.Count; j++)
				enemyCityRange.Add(ProceduralGeneration.neighborsCityRadius[j] + enemyCityLocs[i]);
		}

        for (int i = 0; i < enemyRoadLocs.Count; i++)
        {
            if (!enemyCityRange.Contains(enemyRoadLocs[i]))
                enemyCityRange.Add(enemyRoadLocs[i]);
        }

		Vector3Int firstTradeCenter = new Vector3Int(0, -10, 0);
        if (startingGame)
            firstTradeCenter = PlaceNeighborTradeCenter(random, startingPlace, coastTiles, riverTiles, enemyCityRange);
        propTiles.Remove(terrainDict[firstTradeCenter]); //make sure there's no prop there already

        List<Vector3Int> tradeCenterLocs = new();

        if (firstTradeCenter.y != -10) //setting as Vector3Int? overly complicates
            tradeCenterLocs.Add(firstTradeCenter);
            
        tradeCenterLocs = PlaceTradeCenters(random, tradeCenterLocs, startingPlace, coastTiles, riverTiles, enemyCityRange);

        //seting up range of trade centers so nothing is placed too close
        List<Vector3Int> tradeCenterRange = new();
        for (int i = 0; i < tradeCenterLocs.Count; i++)
        {
            tradeCenterRange.Add(tradeCenterLocs[i]);

            for (int j = 0; j < ProceduralGeneration.neighborsCityRadius.Count; j++)
                tradeCenterRange.Add(ProceduralGeneration.neighborsCityRadius[j] + tradeCenterLocs[i]);
        }

		(List<Vector3Int> luxuryLocs, List<Vector3Int> foodLocs, List<Vector3Int> waterLocs, List<Vector3Int> resourceLocs) = 
            GenerateResources(random, startingPlace, grasslandTiles, riverTiles, coastTiles, enemyCityRange, tradeCenterRange, desertTiles);
		(List<TerrainData> newPropTiles, List<Vector3Int> newFoodLocs, List<Vector3Int> newWaterLocs, List<Vector3Int> newResourceLocs) = SetStartingResources(random, startingPlace);
        (List<Vector3Int> tcFoodLocs, List<Vector3Int> tcWaterLocs) = GenerateTradeCenter(random, tradeCenterLocs);
        propTiles.AddRange(newPropTiles);
        foodLocs.AddRange(enemyFoodLocs);
        foodLocs.AddRange(newFoodLocs);
        foodLocs.AddRange(tcFoodLocs);
        waterLocs.AddRange(enemyWaterLocs);
        waterLocs.AddRange(newWaterLocs);
        waterLocs.AddRange(tcWaterLocs);
        resourceLocs.AddRange(enemyResourceLocs);
        resourceLocs.AddRange(newResourceLocs);

		for (int i = 0; i < propTiles.Count; i++)
        {
            if (resourceLocs.Contains(propTiles[i].TileCoordinates) || foodLocs.Contains(propTiles[i].TileCoordinates))
                continue;

            bool swamp = propTiles[i].terrainData.terrainDesc == TerrainDesc.Swamp;
            bool forest = propTiles[i].CompareTag("Forest") || propTiles[i].CompareTag("Forest Hill");
            AddProp(random, propTiles[i], propTiles[i].terrainData.decorLocs, swamp, forest);
        }

        for (int i = 0; i < foodLocs.Count; i++)
			AddResource(random, terrainDict[foodLocs[i]]);

		for (int i = 0; i < waterLocs.Count; i++)
			AddResource(random, terrainDict[waterLocs[i]]);

		for (int i = 0; i < resourceLocs.Count; i++)
            AddResource(random, terrainDict[resourceLocs[i]]);

        enemyCampLocs = GenerateEnemyCamps(random, startingPlace, landLocs, luxuryLocs, resourceLocs, enemyCityRange, tradeCenterRange, newEra, newRegion);

        //lastly adding mountain middles
        for (int i = 0; i < mountainTiles.Count; i++)
        {
            TerrainData td = terrainDict[mountainTiles[i]];
            if (td.terrainData.terrainDesc == TerrainDesc.Mountain)
            {
                bool grassland = td.terrainData.grassland;
                for (int j = 0; j < ProceduralGeneration.neighborsFourDirections.Count; j++)
                {
                    Vector3Int tile = ProceduralGeneration.neighborsFourDirections[j] + mountainTiles[i];
				    
                    if (terrainDict.ContainsKey(tile) && terrainDict[tile].terrainData.terrainDesc == TerrainDesc.Mountain && terrainDict[tile].terrainData.grassland == grassland)
                        SetMountainMiddle(td, j, grassland, td.main.rotation);
				}
			}
        }
        //Finish it all off by placing water
        //Vector3 waterLoc = new Vector3(width*3 / 2 - .5f, yCoord - .02f, height*3 / 2 - .5f);
        //      GameObject water = Instantiate(this.water, waterLoc, Quaternion.identity);
        //      water.transform.SetParent(groundTiles.transform, false);
        //      allTiles.Add(water);
        //      water.transform.localScale = new Vector3((width*3 + oceanRingDepth * 2)/10f, 1, (height*3 + oceanRingDepth * 2)/10f);
		//return terrainDict;
    }

    private void CreateCityIndicator(Vector3 loc, Color color, bool isShowing) 
    {
		if (isShowing)
        {
            loc.y += 1;
            Gizmos.color = color;
            Gizmos.DrawSphere(loc, 1f);
        }
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

    private void AddProp(System.Random random, TerrainData td, List<string> propArray, bool swamp, bool forest)
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

			if (propInt == 1 && !nonBuildTime)
			{
				td.changeLeafColor = changeLeafColor;

                if (changeLeafColor)
                {
				    for (int i = 0; i < 10; i++)
					    td.uvMapIndex.Add(random.Next(0, 4));
                }
			}

			GameObject newProp = Instantiate(Resources.Load<GameObject>("Prefabs/TerrainPropPrefabs/" + propArray[propInt]), Vector3Int.zero, Quaternion.identity);
			newProp.transform.SetParent(td.prop, false);
			newProp.GetComponent<TreeHandler>().propMesh.rotation = rotation;
            td.SkinnedMeshCheck(); //probably not necessary

			GameObject nonStaticProp = Instantiate(Resources.Load<GameObject>("Prefabs/TerrainPropPrefabs/" + td.terrainData.decorLocs[td.decorIndex]), Vector3.zero, Quaternion.identity);
			nonStaticProp.transform.SetParent(td.nonstatic, false);
			td.nonstatic.rotation = rotation;
			td.SetNonStatic();
		}
        else
        {
			int propInt = random.Next(0, propArray.Count);
			td.decorIndex = propInt;

            if (propArray[propInt] != "")
            {
			    GameObject newProp = Instantiate(Resources.Load<GameObject>("Prefabs/TerrainPropPrefabs/" + propArray[propInt]), Vector3Int.zero, rotation);
			    newProp.transform.SetParent(td.prop, false);
				td.SkinnedMeshCheck();
			}
		}
    }

    private void AddResource(System.Random random, TerrainData td)
    {
		Quaternion rotation = Quaternion.Euler(0, random.Next(0, 4) * 90, 0);
        int index = td.decorIndex;

		if (td.terrainData.decorLocs[index] != "")
		{
			GameObject newProp = Instantiate(Resources.Load<GameObject>("Prefabs/TerrainPropPrefabs/" + td.terrainData.decorLocs[index]), Vector3Int.zero, rotation);
			newProp.transform.SetParent(td.prop, false);
            td.SkinnedMeshCheck();

			if (nonBuildTime && td.rawResourceType == RawResourceType.Rocks)
				CreateResourceIcon(td);

			if (td.isHill && td.rawResourceType == RawResourceType.Rocks)
			{
                GameObject nonStaticProp = Instantiate(Resources.Load<GameObject>("Prefabs/TerrainPropPrefabs/" + td.terrainData.decorLocs[index]), Vector3.zero, Quaternion.identity);
				nonStaticProp.transform.SetParent(td.nonstatic, false);
                nonStaticProp.transform.rotation = rotation;
				//td.nonstatic.rotation = rotation;
                if (!nonBuildTime)
    				td.SetNonStatic();
			}
		}
	}

    private void CreateResourceIcon(TerrainData td)
    {
		Vector3 loc = td.transform.position;

        if (td.isHill)
            loc.y += 0.7f;
        else
            loc.y += 0.1f;
		GameObject improvementResource = Instantiate(this.improvementResource, loc, Quaternion.Euler(90, 0, 0));
		improvementResource.gameObject.transform.SetParent(groundTiles.transform, false);
		ImprovementResource resource = improvementResource.GetComponent<ImprovementResource>();
		resource.SetImage(resourceHolder.GetIcon(td.resourceType));
        allTiles.Add(improvementResource);
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
		propTiles.Clear();
		tradeCenterPositions.Clear();
		enemyCityLocs.Clear();
		enemyRoadLocs.Clear();
        enemyCampLocs.Clear();
        fourWayRiverLocs.Clear();

		for (int i = 0; i < allTiles.Count; i++)
        {
            DestroyImmediate(allTiles[i]);
        }

        allTiles.Clear();
    }

    private Vector3Int FindStartingPlace(List<Vector3Int> riverTiles)
    {
        Vector3Int middleTile = new Vector3Int(width / 2 * 3, 0, height / 2 * 3);
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
            else if (terrainDict[tile].isHill)
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
            else if ((terrainDict[tile].terrainData.type == TerrainType.River && !fourWayRiverLocs.Contains(tile)) || terrainDict[tile].terrainData.type == TerrainType.Coast)
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

					this.propTiles.Remove(terrainDict[tile]);
					DestroyImmediate(terrainDict[tile].gameObject);
					GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandFloodPlainsSO.prefabLocs[0]), tile, Quaternion.identity, 0);
					mountainTiles.Remove(tile);
                    potentialMountainTiles.Remove(tile);
					floodPlainTiles.Add(tile);
				}
				else if (potentialHillTiles.Count > 0)
                {
                    Vector3Int tile = potentialHillTiles[random.Next(0, potentialHillTiles.Count)];

					this.propTiles.Remove(terrainDict[tile]);
					DestroyImmediate(terrainDict[tile].gameObject);
                    GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandFloodPlainsSO.prefabLocs[0]), tile, Quaternion.identity, 0);
                    hillTiles.Remove(tile);
                    potentialHillTiles.Remove(tile);
                    floodPlainTiles.Add(tile);
                }
                else if (potentialForestTiles.Count > 0)
                {
				    Vector3Int tile = potentialForestTiles[random.Next(0, potentialForestTiles.Count)];

					this.propTiles.Remove(terrainDict[tile]);
					DestroyImmediate(terrainDict[tile].gameObject);
				    GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandFloodPlainsSO.prefabLocs[0]), tile, Quaternion.identity, 0);
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
			this.propTiles.Remove(terrainDict[mountain]);
			DestroyImmediate(terrainDict[mountain].gameObject);
			GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandHillSO.prefabLocs[0]), mountain, Quaternion.identity, 0);
            hillTiles.Add(mountain);
            mountainTiles.Remove(mountain);
		}

        if (mountainTiles.Count > 0 && grasslandTiles.Count == 0)
        {
			Vector3Int mountain = mountainTiles[random.Next(0, mountainTiles.Count)];
			this.propTiles.Remove(terrainDict[mountain]);
			DestroyImmediate(terrainDict[mountain].gameObject);
			TerrainData td = GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandSO.prefabLocs[0]), mountain, Quaternion.identity, 0);
            td.gameObject.tag = "Flatland";
            grasslandTiles.Add(mountain);
            mountainTiles.Remove(mountain);
        }

        //making sure starting loc has lumber
        if (forestTiles.Count == 0)
        {
            Vector3Int newForest = startingPlace;
            TerrainDataSO data = forestSO;
            string tag = "Forest";
            
            if (hillTiles.Count > 0)
            {
				newForest = hillTiles[random.Next(0, hillTiles.Count)];

                if (!terrainDict[newForest].terrainData.grassland)
                {
					this.propTiles.Remove(terrainDict[newForest]);
					DestroyImmediate(terrainDict[newForest].gameObject);
					GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandHillSO.prefabLocs[0]), newForest, Quaternion.identity, 0);
				}

                data = forestHillSO;
                tag = "Forest Hill";
                hillTiles.Remove(newForest);
			}
			else if (mountainTiles.Count > 0)
			{
				newForest = mountainTiles[random.Next(0, mountainTiles.Count)];
				this.propTiles.Remove(terrainDict[newForest]);
				DestroyImmediate(terrainDict[newForest].gameObject);
				GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandHillSO.prefabLocs[0]), newForest, Quaternion.identity, 0);
				data = forestHillSO;
				tag = "Forest Hill";
				mountainTiles.Remove(newForest);
			}
			else if (grasslandTiles.Count > 0)
            {
                newForest = grasslandTiles[random.Next(0, grasslandTiles.Count)];
                FloodPlainCheck(newForest);
                flatlandTiles.Remove(newForest);
                grasslandTiles.Remove(newForest);
			}
            else if (flatlandTiles.Count > 0)
            {
                newForest = flatlandTiles[random.Next(0, flatlandTiles.Count)];
				this.propTiles.Remove(terrainDict[newForest]);
				DestroyImmediate(terrainDict[newForest].gameObject);
				GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandSO.prefabLocs[0]), newForest, Quaternion.identity, 0);
				flatlandTiles.Remove(newForest);
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
			this.propTiles.Remove(terrainDict[newCloth]);
			DestroyImmediate(terrainDict[newCloth].gameObject); 
			GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandSO.prefabLocs[0]), newCloth, Quaternion.identity, 0);
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
				this.propTiles.Remove(terrainDict[newClay]);
				TerrainDataSO data = grassland ? grasslandSO : desertSO;
                DestroyImmediate(terrainDict[newClay].gameObject);
				GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + data.prefabLocs[0]), newClay, Quaternion.identity, 0);
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
        Vector3Int newStone = startingPlace;
        bool stoneGrassland = terrainDict[startingPlace].terrainData.grassland;
        bool isHill = false;
        if (hillTiles.Count > 0)
        {
            newStone = hillTiles[random.Next(0, hillTiles.Count)];
            stoneGrassland = terrainDict[newStone].terrainData.grassland;
            isHill = true;
            hillTiles.Remove(newStone);
        }
        else if (flatlandTiles.Count > 0)
        {
			newStone = flatlandTiles[random.Next(0, flatlandTiles.Count)];
            stoneGrassland = terrainDict[newStone].terrainData.grassland;
            FloodPlainCheck(newStone);
            flatlandTiles.Remove(newStone);
            grasslandTiles.Remove(newStone);
        }
        else if (forestTiles.Count > 1)
        {
            newStone = forestTiles[random.Next(0, forestTiles.Count)];
            isHill = terrainDict[newStone].isHill;
			stoneGrassland = terrainDict[newStone].terrainData.grassland;
            this.propTiles.Remove(terrainDict[newStone]);

			DestroyImmediate(terrainDict[newStone].gameObject);

            string newTerrainName;

            if (isHill)
                newTerrainName = stoneGrassland ? grasslandHillSO.prefabLocs[0] : desertHillSO.prefabLocs[0];
            else
                newTerrainName = stoneGrassland ? grasslandSO.prefabLocs[0] : desertSO.prefabLocs[0];

            GameObject newTerrain = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + newTerrainName);
			TerrainData td = GenerateTile(newTerrain, newStone, Quaternion.identity, 0);
			td.gameObject.tag = isHill ? "Hill" : "Flatland";

			forestTiles.Remove(newStone);
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
        if (grasslandTiles.Count <= 1)
        {
            int currentGrasslandCount = grasslandTiles.Count;
            for (int i = 0; i < 2-currentGrasslandCount; i++)
            {
                if (mountainTiles.Count > 0)
                {
				    Vector3Int mountain = mountainTiles[random.Next(0, mountainTiles.Count)];
					this.propTiles.Remove(terrainDict[mountain]);
					DestroyImmediate(terrainDict[mountain].gameObject);
				    TerrainData td = GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandSO.prefabLocs[0]), mountain, Quaternion.identity, 0);
                    td.gameObject.tag = "Flatland";
                    grasslandTiles.Add(mountain);
                    mountainTiles.Remove(mountain);
			    }
                else if (hillTiles.Count > 0)
                {
				    Vector3Int hill = hillTiles[random.Next(0, hillTiles.Count)];
					this.propTiles.Remove(terrainDict[hill]);
					DestroyImmediate(terrainDict[hill].gameObject);
				    TerrainData td = GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandSO.prefabLocs[0]), hill, Quaternion.identity, 0);
				    td.gameObject.tag = "Flatland";
				    grasslandTiles.Add(hill);
                    hillTiles.Remove(hill);
			    }
                else if (flatlandTiles.Count > 0)
                {
                    List<Vector3Int> newFlatlandTiles = new();
                    for (int j = 0; j < flatlandTiles.Count; j++)
                    {
                        if (!grasslandTiles.Contains(flatlandTiles[j]))
                            newFlatlandTiles.Add(flatlandTiles[j]);
                    }

                    if (newFlatlandTiles.Count > 0)
                    {
                        Vector3Int flatland = newFlatlandTiles[random.Next(0, newFlatlandTiles.Count)];

                        if (terrainDict[flatland].terrainData.specificTerrain != SpecificTerrain.FloodPlain)
                        {
							this.propTiles.Remove(terrainDict[flatland]);
							DestroyImmediate(terrainDict[flatland].gameObject);
				            GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandSO.prefabLocs[0]), flatland, Quaternion.identity, 0);
					        grasslandTiles.Add(flatland);
                            flatlandTiles.Remove(flatland);
				        }
                    }
			    }
                else if (forestTiles.Count > 1) //gotta leave one
                {
					Vector3Int forest = forestTiles[random.Next(0, forestTiles.Count)];
                    this.propTiles.Remove(terrainDict[forest]);
                    DestroyImmediate(terrainDict[forest].gameObject);
					TerrainData td = GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandSO.prefabLocs[0]), forest, Quaternion.identity, 0);
					td.gameObject.tag = "Flatland";
					grasslandTiles.Add(forest);
                    forestTiles.Remove(forest);
				}
            }
        }

        int swampCount = 0;
        //making sure all of the forest isn't swamp
        for (int i = 0; i < forestTiles.Count; i++)
        {
            if (terrainDict[forestTiles[i]].terrainData.terrainDesc == TerrainDesc.Swamp)
                swampCount++;
		}
        if (swampCount > 0 && swampCount == forestTiles.Count)
        {
			Vector3Int swamp = forestTiles[random.Next(0, forestTiles.Count)];
            SwampCheck(swamp);
			terrainDict[swamp].gameObject.tag = "Forest";
			terrainDict[swamp].terrainData = forestSO;
			terrainDict[swamp].resourceAmount = -1;
			propTiles.Add(terrainDict[swamp]);
		}

        //adding food, max of two per start (if increasing, force more grassland above)
        int foodToAdd;
        int floodPlainExcess = floodPlainTiles.Count - 2;
        if (floodPlainExcess > 0)
            foodToAdd = 0;
        else if (floodPlainExcess == 0)
            foodToAdd = 1;
        else
            foodToAdd = 2;
        int loopCount = Math.Min(foodToAdd, grasslandTiles.Count);
        for (int i = 0; i < loopCount; i++)
        {
			Vector3Int newFood = grasslandTiles[random.Next(0, grasslandTiles.Count)];

            FloodPlainCheck(newFood);
            AddFood(random, newFood);

			grasslandTiles.Remove(newFood);

			//don't add to resourcetiles
			foodLocs.Add(newFood);
		}

        //adding fish, max of two per start
        for (int i = 0; i < 2; i++)
        {
            Vector3Int newWater = riverCoastTiles[random.Next(0, riverCoastTiles.Count)];
            AddSeaFood(newWater);
			riverCoastTiles.Remove(newWater);

            //don't add to resourcetiles
            waterLocs.Add(newWater);
		}

        return (propTiles, foodLocs, waterLocs, resourceTiles);
	}

    private Vector3Int PlaceNeighborTradeCenter(System.Random random, Vector3Int startingPlace, List<Vector3Int> coastTiles, List<Vector3Int> riverTiles, List<Vector3Int> enemyCityRange)
    {
		//placing first trade center
		int[] widthArray = new int[2] { 5, 6 };
		int width = widthArray[random.Next(0, widthArray.Length)];
        int direction = random.Next(0, 4);
        int trys = 0;

        while (trys < 8)
        {
            switch (direction)
            {
                case 0:
				    Vector3Int corner = new Vector3Int(startingPlace.x - width * 3, yCoord, startingPlace.z - width * 3);

				    for (int x = 0; x < width; x++)
				    {
					    corner.x += 3;

                        if (enemyCityRange.Contains(corner))
                            continue;

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

						if (enemyCityRange.Contains(corner1))
							continue;

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

						if (enemyCityRange.Contains(corner2))
							continue;

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

						if (enemyCityRange.Contains(corner3))
							continue;

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

    private List<Vector3Int> PlaceTradeCenters(System.Random random, List<Vector3Int> tradeCenterLocs, Vector3Int startingPlace, List<Vector3Int> coastTiles, List<Vector3Int> riverTiles, List<Vector3Int> enemyCityRange)
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

                        if (enemyCityRange.Contains(tile))
                            continue;

                        if (terrainDict[tile].CompareTag("Flatland") || terrainDict[tile].CompareTag("Forest"))
                        {
                            spotLooking = false;
                            tradeCenterLocs.Add(tile);
                            propTiles.Remove(terrainDict[tile]);
                            break;
                        }
					}
                }
            }
        }
    
        return tradeCenterLocs;
    }

    private (List<Vector3Int>, List<Vector3Int>, List<Vector3Int>, List<Vector3Int>) GenerateResources(System.Random random, 
        Vector3Int startingPlace, List<Vector3Int> grasslandLocs, List<Vector3Int> riverTiles, List<Vector3Int> coastTiles, List<Vector3Int> enemyCityRange, List<Vector3Int> tradeCenterRange, List<Vector3Int> desertTiles)
    {
        List<Vector3Int> startingPlaceRange = new() { startingPlace };
        List<Vector3Int> finalResourceLocs = new();
        List<Vector3Int> finalWaterLocs = new();
        List<Vector3Int> finalFoodLocs = new();
        List<Vector3Int> luxuryResourceLocs = new();
        List<Vector3Int> potentialWaterResourceLocs = new(riverTiles);
        List<Vector3Int> newCoastTiles = new(coastTiles); 
        potentialWaterResourceLocs.AddRange(newCoastTiles);
        grasslandLocs.Remove(startingPlace);

        for (int i = 0; i < ProceduralGeneration.neighborsCityRadius.Count; i++)
        {
            Vector3Int tile = ProceduralGeneration.neighborsCityRadius[i] + startingPlace;

			startingPlaceRange.Add(tile);
            potentialWaterResourceLocs.Remove(tile);
            grasslandLocs.Remove(tile);
        }

        for (int i = 0; i < tradeCenterRange.Count; i++)
        {
            grasslandLocs.Remove(tradeCenterRange[i]);
			potentialWaterResourceLocs.Remove(tradeCenterRange[i]);
        }

        for (int i = 0; i < enemyCityRange.Count; i++)
        {
            grasslandLocs.Remove(enemyCityRange[i]);
            potentialWaterResourceLocs.Remove(enemyCityRange[i]);
		}

        int xCount = width / resourceFrequency - 1;
        int zCount = height / resourceFrequency - 1;

        //since rounding up, total placements could be low
        int rocksAmount = stoneAmount + goldAmount + jewelAmount + ironAmount + copperAmount - goldPlaced - jewelPlaced - stonePlaced - ironPlaced - copperPlaced;
        int totalResourcePlacements = rocksAmount + clayAmount + clothAmount;

        List<Vector3Int> potentialResourceLocs = new();

        //finding tiles to place resources
		for (int i = 1; i <= xCount; i++)
        {
            for (int j = 1; j <= zCount; j++)
            {
                Vector3Int potentialLoc = new Vector3Int(i * random.Next(resourceFrequency-1, resourceFrequency + 1) * 3, 
                    yCoord, j * random.Next(resourceFrequency - 1, resourceFrequency + 1) * 3);

                if (startingPlaceRange.Contains(potentialLoc) || tradeCenterRange.Contains(potentialLoc) || enemyCityRange.Contains(potentialLoc))
                    continue;

                if (terrainDict[potentialLoc].isLand && terrainDict[potentialLoc].walkable)
                {
					if (!potentialResourceLocs.Contains(potentialLoc))
						potentialResourceLocs.Add(potentialLoc);
                }
                else
                {
                    for (int k = 0; k < ProceduralGeneration.neighborsEightDirections.Count; k++)
                    {
                        Vector3Int tile = potentialLoc + ProceduralGeneration.neighborsEightDirections[k];

                        if (terrainDict[tile].isLand && terrainDict[tile].walkable && !startingPlaceRange.Contains(tile) && !tradeCenterRange.Contains(tile) && !enemyCityRange.Contains(tile) && !potentialResourceLocs.Contains(tile))
                        {
                            if (!potentialResourceLocs.Contains(tile))
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

					if (terrainDict[tile].isLand && terrainDict[tile].walkable && !startingPlaceRange.Contains(tile) && !tradeCenterRange.Contains(tile) && !enemyCityRange.Contains(tile) && !potentialResourceLocs.Contains(tile))
					{
						if (!potentialResourceLocs.Contains(tile))
							potentialResourceLocs.Add(tile);
						break;
					}
				}
			}
        }
        else if (gap < 0)
        {
            for (int i = 0; i < Math.Abs(gap); i++)
                potentialResourceLocs.RemoveAt(random.Next(0, potentialResourceLocs.Count));
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

						if (!terrainDict[tile].isHill && terrainDict[tile].terrainData.grassland && !startingPlaceRange.Contains(tile) && !tradeCenterRange.Contains(tile) && !enemyCityRange.Contains(tile)
							&& !finalResourceLocs.Contains(tile) && terrainDict[tile].walkable)
						{
							selectedTile = tile;
							foundLocation = true;
							grasslandLocs.Remove(randomTile);
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
				desertTiles.Remove(selectedTile);
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
                    
                        if (terrainDict[tile].CompareTag("Flatland") && !startingPlaceRange.Contains(tile) && !tradeCenterRange.Contains(tile) && !enemyCityRange.Contains(tile) && !finalResourceLocs.Contains(tile))
                        {
                            foundLocation = true;
                            selectedTile = tile;
							break;
                        }
                    }
			    }
                else
                {
                    foundLocation = true;
                    selectedTile = randomTile;
				}

                if (!foundLocation)
                    attempts++;
            }

            if (foundLocation)
            {
				potentialResourceLocs.Remove(selectedTile);
				grasslandLocs.Remove(selectedTile);
				GenerateClayTile(selectedTile);
				desertTiles.Remove(selectedTile);
				if (!finalResourceLocs.Contains(selectedTile))
					finalResourceLocs.Add(selectedTile);
            }
		}

        //placing rocks
        List<ResourceType> resourcesToPlace = new() { ResourceType.GoldOre, ResourceType.Ruby, ResourceType.IronOre, ResourceType.CopperOre, ResourceType.Stone };
		if (goldPlaced >= goldAmount)
			resourcesToPlace.Remove(ResourceType.GoldOre);
		if (jewelPlaced >= jewelAmount)
			resourcesToPlace.Remove(ResourceType.Ruby);
		if (ironPlaced >= ironAmount)
			resourcesToPlace.Remove(ResourceType.IronOre);
		if (copperPlaced >= copperAmount)
			resourcesToPlace.Remove(ResourceType.CopperOre);
		if (stonePlaced >= stoneAmount)
			resourcesToPlace.Remove(ResourceType.Stone);
		int loopAmount = Math.Min(rocksAmount, potentialResourceLocs.Count);
        int cycle = 0;
        for (int i = 0; i < loopAmount; i++)
        {
            //check to remove resources already fully placed
            if (cycle == resourcesToPlace.Count)
            {
                cycle = 0;
				if (goldPlaced >= goldAmount)
					resourcesToPlace.Remove(ResourceType.GoldOre);
				if (jewelPlaced >= jewelAmount)
					resourcesToPlace.Remove(ResourceType.Ruby);
				if (ironPlaced >= ironAmount)
					resourcesToPlace.Remove(ResourceType.IronOre);
				if (copperPlaced >= copperAmount)
					resourcesToPlace.Remove(ResourceType.CopperOre);
				if (stonePlaced >= stoneAmount)
					resourcesToPlace.Remove(ResourceType.Stone);
			}

			int index = 0;
            int adjacentCount = stoneAdjacent;
            int amountMin = stoneAmounts.Item1;
            int amountMax = stoneAmounts.Item2;
            bool luxury = false;

			ResourceType type = resourcesToPlace[cycle];
            cycle++;
            //specifying which rocks to place
            if (type == ResourceType.GoldOre)
            {
                luxury = true;
                type = ResourceType.GoldOre;
				index = 2;
				adjacentCount = goldAdjacent;
				amountMin = goldAmounts.Item1;
				amountMax = goldAmounts.Item2;
                goldPlaced++;
			}
            else if (type == ResourceType.Ruby)
            {
                luxury = true;
				index = 1;
				adjacentCount = jewelAdjacent;
				amountMin = jewelAmounts.Item1;
				amountMax = jewelAmounts.Item2;
                jewelPlaced++;
			}
			else if (type == ResourceType.IronOre)
            {
				index = 0;
				adjacentCount = stoneAdjacent;
				amountMin = ironAmounts.Item1;
				amountMax = ironAmounts.Item2;
                ironPlaced++;
			}
			else if (type == ResourceType.CopperOre)
            {
                index = 0;
                adjacentCount = stoneAdjacent;
                amountMin = copperAmounts.Item1;
                amountMax = copperAmounts.Item2;
                copperPlaced++;
			}
            else if (type == ResourceType.Stone)
            {
				index = 0;
				adjacentCount = stoneAdjacent;
				amountMin = stoneAmounts.Item1;
				amountMax = stoneAmounts.Item2;
                stonePlaced++;
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

				if (terrainDict[newTile].isLand && terrainDict[newTile].walkable && !startingPlaceRange.Contains(newTile) && !tradeCenterRange.Contains(newTile) && !enemyCityRange.Contains(newTile) && !finalResourceLocs.Contains(newTile))
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

					desertTiles.Remove(newTile);
					grasslandLocs.Remove(newTile);
				}                
			}

			desertTiles.Remove(randomTile);
			potentialResourceLocs.Remove(randomTile);
			grasslandLocs.Remove(randomTile);
		}

        //put food every 2nd, 3rd or 4th remaining grasslandtile
        int grasslandCount = grasslandLocs.Count / (resourceFrequency - 1);
        for (int i = 0; i < grasslandCount; i++)
        {
			Vector3Int newFood = grasslandLocs[random.Next(0, grasslandLocs.Count)];
            AddFood(random, newFood);

			grasslandLocs.Remove(newFood);
            finalFoodLocs.Add(newFood);
			desertTiles.Remove(newFood);
		}

        //put fish every fourth tile on average
        int fishCount = potentialWaterResourceLocs.Count / 4;
        for (int i = 0; i < fishCount; i++)
        {
            Vector3Int newWater = potentialWaterResourceLocs[random.Next(0, potentialWaterResourceLocs.Count)];
            AddSeaFood(newWater);

			potentialWaterResourceLocs.Remove(newWater);
            finalWaterLocs.Add(newWater);
        }

        //placing lots of clay in the desert (or grassland in east)?
        int clayCount = desertTiles.Count / 6;
        for (int i = 0; i < clayCount; i++)
        {
            if (desertTiles.Count == 0)
                break;

            Vector3Int newClay = desertTiles[random.Next(0, desertTiles.Count)];

            if (enemyCityRange.Contains(newClay) || startingPlaceRange.Contains(newClay) || tradeCenterRange.Contains(newClay)) // do it again if found here
            {
                i--;
                desertTiles.Remove(newClay);
                continue;
            }

            desertTiles.Remove(newClay);
            GenerateClayTile(newClay);
            finalResourceLocs.Add(newClay);
		}

		return (luxuryResourceLocs, finalFoodLocs, finalWaterLocs, finalResourceLocs);
    }

    private void GenerateClayTile(Vector3Int selectedTile)
    {
		SwampCheck(selectedTile);
		FloodPlainCheck(selectedTile);
		terrainDict[selectedTile].terrainData = terrainDict[selectedTile].terrainData.grassland ? grasslandResourceSO : desertResourceSO;
		terrainDict[selectedTile].resourceType = ResourceType.Clay;
		terrainDict[selectedTile].rawResourceType = RawResourceType.Clay;
		terrainDict[selectedTile].decorIndex = 3;
		terrainDict[selectedTile].gameObject.tag = "Flatland";
		terrainDict[selectedTile].resourceAmount = -1;
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
            propTiles.Remove(terrainDict[tile]);
			DestroyImmediate(terrainDict[tile].gameObject);
			TerrainData td = GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandSO.prefabLocs[0]), tile, Quaternion.identity, 0);
            td.gameObject.tag = "Flatland";
		}
	}

    private void FloodPlainCheck(Vector3Int tile)
    {
		if (terrainDict[tile].terrainData.specificTerrain == SpecificTerrain.FloodPlain)
		{
            propTiles.Remove(terrainDict[tile]);
            TerrainDataSO data = terrainDict[tile].terrainData.grassland ? grasslandSO : desertSO;
			DestroyImmediate(terrainDict[tile].gameObject);
			GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + data.prefabLocs[0]), tile, Quaternion.identity, 0);
		}
	}

    private void ForestCheck(Vector3Int tile)
    {
        if (terrainDict[tile].CompareTag("Forest") || terrainDict[tile].CompareTag("Forest Hill"))
        {
			propTiles.Remove(terrainDict[tile]);
			TerrainDataSO data = terrainDict[tile].isHill ? grasslandHillSO : grasslandSO;
			DestroyImmediate(terrainDict[tile].gameObject);
			GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + data.prefabLocs[0]), tile, Quaternion.identity, 0);
		}
    }

    private void AddFood(System.Random random, Vector3Int newFood)
    {
		terrainDict[newFood].terrainData = grasslandResourceSO;
		terrainDict[newFood].resourceType = ResourceType.Food;
		terrainDict[newFood].rawResourceType = RawResourceType.FoodLand;
		terrainDict[newFood].decorIndex = 4;
		terrainDict[newFood].resourceAmount = -1;
		terrainDict[newFood].uvMapIndex.Add(random.Next(0, 0)); //for determining which flowers to show
		terrainDict[newFood].uvMapIndex.Add(random.Next(0, 2));
	}

    private void AddSeaFood(Vector3Int newWater)
    {
		bool river = terrainDict[newWater].terrainData.type == TerrainType.River;
		terrainDict[newWater].terrainData = river ? riverResourceSO : seaResourceSO;
		terrainDict[newWater].resourceType = ResourceType.Fish;
		terrainDict[newWater].rawResourceType = RawResourceType.FoodSea;
		terrainDict[newWater].decorIndex = 0;
		terrainDict[newWater].resourceAmount = -1;
	}

	public void SetMountainMiddle(TerrainData td, int i, bool grassland, Quaternion mainRotation)
	{
        int diff = Mathf.RoundToInt(mainRotation.eulerAngles.y / 90);

        i -= diff;
        if (i < 0)
            i += 4;

        Vector3 pos;
		if (i == 0)
			pos = new Vector3(0, 0, 1);
		else if (i == 1)
			pos = new Vector3(1, 0, 0);
		else if (i == 2)
			pos = new Vector3(0, 0, -1);
		else
			pos = new Vector3(-1, 0, 0);
		Quaternion rotation = Quaternion.Euler(0, i * 90, 0);
		GameObject mmGO = Instantiate(mountainMiddle, pos, rotation);
		GameObject mmNonStatic = Instantiate(mountainMiddle, pos, rotation);
		mmGO.transform.SetParent(td.main, false);
		mmNonStatic.transform.SetParent(td.nonstatic, false);

        td.AddMountainMiddleToWhiteMesh(mmGO.GetComponentInChildren<MeshRenderer>());		    

		if (!nonBuildTime && grassland)
		{
			MeshFilter mesh = mmGO.GetComponentInChildren<MeshFilter>();
			Vector2[] allUVs = mesh.mesh.uv;

			for (int j = 0; j < allUVs.Length; j++)
				allUVs[j].x += -0.062f;

			mesh.mesh.uv = allUVs;
			mmNonStatic.GetComponentInChildren<MeshFilter>().mesh.uv = allUVs;
		}
	}

	private (List<Vector3Int>, List<Vector3Int>, List<Vector3Int>) GenerateEnemyCities(System.Random random, Vector3Int startingPlace, List<Vector3Int> coastTiles, GameObject chosenLeader, bool starting, List<Vector3Int> otherEnemyCities = null)
    {
		//make sure enough flatland surround it (3 to 4)
        List<Vector3Int> foodLocs = new();
        List<Vector3Int> waterLocs = new();
        List<Vector3Int> resourceLocs = new();
        List<(Vector3Int, int)> coastDist = new();
        List<Vector3Int> voidedTiles = new();
        List<Vector3Int> empireCities = new();

        int xMinStart;
        int zMinStart;

        if (starting)
        {
            voidedTiles.Add(startingPlace);

            for (int i = 0; i < coastTiles.Count; i++)
                coastDist.Add((coastTiles[i], Math.Abs(coastTiles[i].x - startingPlace.x) + Math.Abs(coastTiles[i].z - startingPlace.z)));

            //can't be within 4 tiles of starting place
			xMinStart = startingPlace.x - 12;
			zMinStart = startingPlace.z - 12;
		}
        else
        {
            Vector3Int avgEnemyCity = Vector3Int.zero;
            for (int i = 0; i < otherEnemyCities.Count; i++)
            {
                voidedTiles.Add(otherEnemyCities[i]);
                avgEnemyCity += otherEnemyCities[i];
            }

			if (otherEnemyCities.Count > 0)
				avgEnemyCity /= otherEnemyCities.Count;

			for (int i = 0; i < coastTiles.Count; i++)
				coastDist.Add((coastTiles[i], Math.Abs(coastTiles[i].x - avgEnemyCity.x) + Math.Abs(coastTiles[i].z - avgEnemyCity.z)));

            xMinStart = avgEnemyCity.x - 12;
            zMinStart = avgEnemyCity.z - 12;
		}

		coastDist.Sort((a, b) => b.Item2.CompareTo(a.Item2));

        if (starting || otherEnemyCities.Count > 0)
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    voidedTiles.Add(new Vector3Int(xMinStart + i * 3, yCoord, zMinStart + j * 3));
                }
            }
        }

        //put enemy cities as far as possible from starting place
        Vector3Int newEnemyCity = FindCoastTileForCity(coastDist, voidedTiles);

        if (newEnemyCity == new Vector3Int(0, -10, 0))
        {
            return (foodLocs, waterLocs, resourceLocs);
        }
        else
        {
			empireCities.Add(newEnemyCity);
            propTiles.Remove(terrainDict[newEnemyCity]);
        }

        //finding neighboring cities
        for (int i = 0; i < enemyCountDifficulty - 1; i++)
        {
            Vector3Int mostRecentLoc = empireCities[empireCities.Count - 1];
            voidedTiles.Add(mostRecentLoc);

            for (int j = 0; j < ProceduralGeneration.neighborsCityRadius.Count; j++)
                voidedTiles.Add(ProceduralGeneration.neighborsCityRadius[j] + mostRecentLoc);   
            
            int xMin = mostRecentLoc.x - 12;
			int xMax = mostRecentLoc.x + 12;
			int zMin = mostRecentLoc.z - 12;
			int zMax = mostRecentLoc.z + 12;
			List<Vector3Int> circleList = new();
            bool foundLoc = false;

			//making circle with 4 radius to find next ocean spot
            for (int j = 0; j < 9; j++)
            {
                Vector3Int nextTile = new Vector3Int(xMin + j * 3, yCoord, zMin);
                circleList.Add(nextTile);
            }

			for (int j = 0; j < 9; j++)
			{
				Vector3Int nextTile = new Vector3Int(xMin + j * 3, yCoord, zMax);
				circleList.Add(nextTile);
			}

			for (int j = 1; j < 8; j++)
			{
				Vector3Int nextTile = new Vector3Int(xMin, yCoord, zMin + j * 3);
				circleList.Add(nextTile);
			}

			for (int j = 1; j < 8; j++)
			{
				Vector3Int nextTile = new Vector3Int(xMax, yCoord, zMin + j * 3);
				circleList.Add(nextTile);
			}

            //seeing if potential loc is in circle
			for (int j = 0; j < circleList.Count; j++)
            {
				if (voidedTiles.Contains(circleList[j]))
					continue;

				if (coastTiles.Contains(circleList[j]))
				{
					for (int m = 0; m < ProceduralGeneration.neighborsFourDirections.Count; m++)
					{
						Vector3Int tile = ProceduralGeneration.neighborsFourDirections[m] + circleList[j];

						if (voidedTiles.Contains(tile))
							continue;

						if (terrainDict.ContainsKey(tile) && terrainDict[tile].isLand)
						{
    						int groundCount = 0;
							for (int l = 0; l < ProceduralGeneration.neighborsEightDirections.Count; l++)
							{
								Vector3Int newTile = ProceduralGeneration.neighborsEightDirections[l] + tile;

								if (terrainDict.ContainsKey(newTile) && terrainDict[newTile].isLand)
									groundCount++;
							}

						    if (groundCount >= 3)
						    {
								empireCities.Add(tile);
								propTiles.Remove(terrainDict[tile]);
								foundLoc = true;
							    break;
						    }
						}
					}
				}

				if (foundLoc)
					break;
			}

            //if still can't find anything, go with closest sea tile
            if (!foundLoc)
            {
				List<(Vector3Int, int)> enemyCoastDist = new();

				for (int j = 0; j < coastTiles.Count; j++)
					enemyCoastDist.Add((coastTiles[j], Math.Abs(coastTiles[j].x - mostRecentLoc.x) + Math.Abs(coastTiles[j].z - mostRecentLoc.z)));

				enemyCoastDist.Sort((a, b) => a.Item2.CompareTo(b.Item2));

				Vector3Int nextEnemyCity = FindCoastTileForCity(enemyCoastDist, voidedTiles);
				if (nextEnemyCity != new Vector3Int(0, -10, 0))
                {
					empireCities.Add(nextEnemyCity);
					propTiles.Remove(terrainDict[nextEnemyCity]);
					foundLoc = true;
                }
			}
        }

        for (int i = 0; i < empireCities.Count; i++)
        {
            ForestCheck(empireCities[i]);
            
            if (!terrainDict[empireCities[i]].walkable)
            {
                propTiles.Remove(terrainDict[empireCities[i]]);
                DestroyImmediate(terrainDict[empireCities[i]].gameObject);
				GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandHillSO.prefabLocs[0]), empireCities[i], Quaternion.identity, 0);
			}
            else if (!terrainDict[empireCities[i]].terrainData.grassland)
            {
				propTiles.Remove(terrainDict[empireCities[i]]);
				TerrainDataSO data;
                if (terrainDict[empireCities[i]].isHill)
                    data = grasslandHillSO;
                else
                    data = grasslandSO;
                DestroyImmediate(terrainDict[empireCities[i]].gameObject);
                GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + data.prefabLocs[0]), empireCities[i], Quaternion.identity, 0);
			}

            SwampCheck(empireCities[i]);
            FloodPlainCheck(empireCities[i]);

            for (int j = 0; j < ProceduralGeneration.neighborsEightDirections.Count; j++)
            {
                Vector3Int tile = ProceduralGeneration.neighborsEightDirections[j] + empireCities[i];

                if (!terrainDict[tile].walkable && terrainDict[tile].isLand)
                {
					propTiles.Remove(terrainDict[tile]);
					DestroyImmediate(terrainDict[tile].gameObject);
					GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + grasslandSO.prefabLocs[0]), tile, Quaternion.identity, 0);
				}

                /*if (terrainDict[tile].CompareTag("Flatland"))
                {
                    if (!terrainDict[tile].terrainData.grassland)
                    {
						propTiles.Remove(terrainDict[tile]);
						TerrainDataSO data = grasslandSO;
						DestroyImmediate(terrainDict[tile].gameObject);
						GenerateTile(data.prefabs[0], tile, Quaternion.identity, 0);
					}
                }
                else */if (terrainDict[tile].isHill)
                {
					propTiles.Remove(terrainDict[tile]);
                    TerrainDataSO data = grasslandSO;
				    DestroyImmediate(terrainDict[tile].gameObject);
				    GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + data.prefabLocs[0]), tile, Quaternion.identity, 0);
                }
			}
		}

        bool[] hasRoad = new bool[empireCities.Count];
        for (int i = 0; i < empireCities.Count; i++)
            hasRoad[i] = false;

        if (empireCities.Count == 1)
            hasRoad[0] = true;
        //building road between cities
        for (int i = 0; i < empireCities.Count; i++)
		{
            if (hasRoad[i])
                continue;
            
            Vector3Int startingEnemyCity = empireCities[0];
			Vector3Int finalEnemyCity = empireCities[i];
			int dist = 0;
            //find closest city to build road to (if close enough)
            bool firstOne = true;
			for (int j = 0; j < empireCities.Count; j++)
			{
                if (j == i) //can't find itself
                    continue;
                    
                if (firstOne)
				{
                    firstOne = false;
                    dist = Math.Abs(finalEnemyCity.x - empireCities[j].x) + Math.Abs(finalEnemyCity.z - empireCities[j].z);
					startingEnemyCity = empireCities[j];
                    continue;
				}

				int newDist = Math.Abs(finalEnemyCity.x - empireCities[j].x) + Math.Abs(finalEnemyCity.z - empireCities[j].z);
				if (newDist < dist)
				{
					dist = newDist;
					startingEnemyCity = empireCities[j];
				}
			}

			if (dist < 25)
			{
				List<Vector3Int> path = new();

				List<Vector3Int> positionsToCheck = new();
				Dictionary<Vector3Int, int> costDictionary = new();
				Dictionary<Vector3Int, int> priorityDictionary = new();
				Dictionary<Vector3Int, Vector3Int?> parentsDictionary = new();
                bool prevRiver = false;

				positionsToCheck.Add(startingEnemyCity);
				priorityDictionary.Add(startingEnemyCity, 0);
				costDictionary.Add(startingEnemyCity, 0);
				parentsDictionary.Add(startingEnemyCity, null);

				while (positionsToCheck.Count > 0)
				{
					Vector3Int current = GridSearch.GetClosestVertex(positionsToCheck, priorityDictionary);

					positionsToCheck.Remove(current);
					if (current == finalEnemyCity)
					{
						path = GridSearch.GeneratePath(parentsDictionary, current);
						path.RemoveAt(path.Count - 1); //remove last one
						break;
					}

					foreach (Vector3Int tile in ProceduralGeneration.neighborsEightDirections)
					{
						Vector3Int neighbor = tile + current;

						if (terrainDict[neighbor].walkable)
						{
							if (terrainDict[neighbor].sailable)
                            {
                                if (prevRiver)
                                    continue;
                                if (!terrainDict[neighbor].straightRiver)
								    continue;
                            }
						}
						else
						{
							continue;//If it's an obstacle, ignore
						}

                        int tempCost = terrainDict[neighbor].terrainData.movementCost;
                        if (tile.sqrMagnitude == 18)
                            tempCost = Mathf.RoundToInt(1.414f * tempCost);

						int newCost = costDictionary[current] + tempCost;
						if (!costDictionary.ContainsKey(neighbor) || newCost < costDictionary[neighbor])
						{
							costDictionary[neighbor] = newCost;

							int priority = newCost + GridSearch.ManhattanDistance(finalEnemyCity, neighbor); //only check the neighbors closest to destination
							positionsToCheck.Add(neighbor);
							priorityDictionary[neighbor] = priority;
                            prevRiver = false;
                            if (terrainDict[neighbor].straightRiver)
                                prevRiver = true;
							parentsDictionary[neighbor] = current;
						}
					}
				}

				if (path.Count < 7)
                {
                    hasRoad[i] = true;
                    hasRoad[empireCities.IndexOf(startingEnemyCity)] = true;
                    for (int j = 0; j < path.Count; j++)
                    {
                        if (!enemyRoadLocs.Contains(path[j]))
                            enemyRoadLocs.Add(path[j]);
                    }
                }
			}
		}

        //adding enemy city resources
        for (int i = 0; i < empireCities.Count; i++)
        {
			List<Vector3Int> flatlandTiles = new();
            List<Vector3Int> desertTiles = new();
            List<Vector3Int> forestTiles = new();
            List<Vector3Int> cityCoastTiles = new();
            List<Vector3Int> hillTiles = new();
			
            for (int j = 0; j < ProceduralGeneration.neighborsEightDirections.Count; j++)
            {
				Vector3Int tile = ProceduralGeneration.neighborsEightDirections[j] + empireCities[i];

                if (enemyRoadLocs.Contains(tile))
                    continue;

                if (terrainDict[tile].isHill)
                    hillTiles.Add(tile);

                if (terrainDict[tile].CompareTag("Flatland"))
                {
                    if (terrainDict[tile].terrainData.grassland)
                        flatlandTiles.Add(tile);
                    else
                        desertTiles.Add(tile);
                }
                else if (terrainDict[tile].CompareTag("Forest"))
                {
                    forestTiles.Add(tile);
                }

				if ((terrainDict[tile].terrainData.type == TerrainType.River && !fourWayRiverLocs.Contains(tile)) || terrainDict[tile].terrainData.type == TerrainType.Coast)
					cityCoastTiles.Add(tile);
			}

			//adding land food (1)
			int flatlandCount = flatlandTiles.Count;
            if (flatlandCount == 0 && desertTiles.Count > 0)
            {
                Vector3Int tile = desertTiles[random.Next(0, desertTiles.Count)];

				propTiles.Remove(terrainDict[tile]);
				TerrainDataSO newData = grasslandSO;
				DestroyImmediate(terrainDict[tile].gameObject);
				GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + newData.prefabLocs[0]), tile, Quaternion.identity, 0);
                desertTiles.Remove(tile);
                flatlandTiles.Add(tile);
                flatlandCount++;
			}

            if (flatlandCount == 0 && hillTiles.Count > 0)
            {
                Vector3Int tile = hillTiles[random.Next(0, hillTiles.Count)];
                
                propTiles.Remove(terrainDict[tile]);
				TerrainDataSO newData = grasslandSO;
				DestroyImmediate(terrainDict[tile].gameObject);
				GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + newData.prefabLocs[0]), tile, Quaternion.identity, 0);
                hillTiles.Remove(tile);
                flatlandTiles.Add(tile);
                flatlandCount++;
			}

            if (flatlandCount == 0 && forestTiles.Count > 0)
            {
                Vector3Int tile = forestTiles[random.Next(0, forestTiles.Count)];

                ForestCheck(tile);
                forestTiles.Remove(tile);
				flatlandTiles.Add(tile);
				flatlandCount++;
			}

		    for (int j = 0; j < flatlandCount; j++)
		    {
			    if (j == 1)
				    break;

			    Vector3Int newFood = flatlandTiles[random.Next(0, flatlandTiles.Count)];
                AddFood(random, newFood);

			    foodLocs.Add(newFood);
			    flatlandTiles.Remove(newFood);
		    }

            flatlandTiles.AddRange(desertTiles); //adding back to flatland to build rocks on
            //adding coastal food (2)
		    int coastCount = cityCoastTiles.Count;
		    for (int j = 0; j < coastCount; j++)
		    {
			    if (j == 2)
				    break;

			    Vector3Int newWater = cityCoastTiles[random.Next(0, cityCoastTiles.Count)];
                AddSeaFood(newWater);

			    waterLocs.Add(newWater);
			    cityCoastTiles.Remove(newWater);
		    }

            bool addResource = false;
            ResourceType type = ResourceType.None;
            int index = 0, amountMin = 0, amountMax = 0;
            TerrainDataSO data = grasslandSO;
            Vector3Int resourceLoc = Vector3Int.zero;
            if (hillTiles.Count + flatlandTiles.Count > 0)
            {
                bool hill = hillTiles.Count > 0;
                
                if (goldAmount - goldPlaced > 0)
                {
                    addResource = true;
                    type = ResourceType.GoldOre;
                    resourceLoc = hill ? hillTiles[random.Next(0, hillTiles.Count)] : flatlandTiles[random.Next(0, flatlandTiles.Count)];
					goldPlaced++;
					index = 2;
					amountMin = goldAmounts.Item1;
					amountMax = goldAmounts.Item2;
				}
				else if (jewelAmount - jewelPlaced > 0)
                {
					addResource = true;
					type = ResourceType.Ruby;
					resourceLoc = hill ? hillTiles[random.Next(0, hillTiles.Count)] : flatlandTiles[random.Next(0, flatlandTiles.Count)];
					jewelPlaced++;
					index = 1;
					amountMin = jewelAmounts.Item1;
					amountMax = jewelAmounts.Item2;
				}

                if (hill)
                    data = terrainDict[resourceLoc].terrainData.grassland ? grasslandHillResourceSO : desertHillResourceSO;
				else
                    data = terrainDict[resourceLoc].terrainData.grassland ? grasslandResourceSO : desertResourceSO;
			}

            if (addResource)
            {
                resourceLocs.Add(resourceLoc);
                terrainDict[resourceLoc].terrainData = data;
				terrainDict[resourceLoc].resourceType = type;
				terrainDict[resourceLoc].rawResourceType = RawResourceType.Rocks;
				terrainDict[resourceLoc].resourceAmount = random.Next(amountMin, amountMax);
				terrainDict[resourceLoc].decorIndex = index;
                hillTiles.Remove(resourceLoc);
                flatlandTiles.Remove(resourceLoc);
			}

            //making sure one flatland per city exists to place barracks
            if (flatlandTiles.Count == 0)
            {
                if (forestTiles.Count > 0)
                {
                    Vector3Int tile = forestTiles[random.Next(0, forestTiles.Count)];
                    ForestCheck(tile);
                }
                else if (hillTiles.Count > 0)
                {
					Vector3Int tile = hillTiles[random.Next(0, hillTiles.Count)];
					propTiles.Remove(terrainDict[tile]);
					TerrainDataSO newData = terrainDict[tile].terrainData.grassland ? grasslandSO : desertSO;
					DestroyImmediate(terrainDict[tile].gameObject);
					GenerateTile(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + newData.prefabLocs[0]), tile, Quaternion.identity, 0);
				}
			}
		}

        //generating terrain info for enemy cities
  //      for (int i = 0; i < empireCities.Count; i++)
  //      {
		//	terrainDict[empireCities[i]].enemyCamp = true;

  //          for (int j = 0; j < ProceduralGeneration.neighborsEightDirections.Count; j++)
  //              terrainDict[ProceduralGeneration.neighborsEightDirections[j] + empireCities[i]].enemyZone = true;
		//}

        if (nonBuildTime)
        {
            for (int i = 0; i < empireCities.Count; i++)
                enemyCityLocs.Add(empireCities[i]);
		}
        else
        {
			EnemyEmpire empire = new();
            empire.capitalCity = empireCities[0];

			Vector3 direction = Vector3.zero - empire.capitalCity;
			Quaternion rotation;
			if (direction == Vector3.zero)
				rotation = Quaternion.identity;
			else
				rotation = Quaternion.LookRotation(direction, Vector3.up);

            GameObject leaderGO = Instantiate(chosenLeader, empire.capitalCity, rotation);
			leaderGO.transform.SetParent(enemyCityHolder, false);
			MilitaryLeader leader = leaderGO.GetComponent<MilitaryLeader>();
            empire.enemyLeader = leader;
            empire.enemyEra = leader.buildDataSO.unitEra;
            empire.enemyRegion = leader.buildDataSO.unitRegion;
            leaderGO.SetActive(false);

			for (int i = 0; i < empireCities.Count; i++)
			{
				enemyCityLocs.Add(empireCities[i]);
                empire.empireCities.Add(empireCities[i]);
			}

            enemyEmpires.Add(empire);
        }

		return (foodLocs, waterLocs, resourceLocs);
	}

    private Vector3Int FindCoastTileForCity(List<(Vector3Int, int)> coastDist, List<Vector3Int> voidedTiles)
    {
        Vector3Int finalLoc = new Vector3Int(0, -10, 0);
        
        for (int i = 0; i < coastDist.Count; i++)
		{
			for (int j = 0; j < ProceduralGeneration.neighborsFourDirections.Count; j++)
			{
				Vector3Int tile = ProceduralGeneration.neighborsFourDirections[j] + coastDist[i].Item1;

				if (voidedTiles.Contains(tile))
					continue;

				if (terrainDict.ContainsKey(tile) && terrainDict[tile].isLand)
				{
					int groundCount = 0;

					for (int k = 0; k < ProceduralGeneration.neighborsEightDirections.Count; k++)
					{
						Vector3Int newTile = ProceduralGeneration.neighborsEightDirections[k] + tile;

						if (terrainDict.ContainsKey(newTile) && terrainDict[newTile].isLand)
							groundCount++;
					}

					if (groundCount >= 3)
						return tile;
				}
			}
		}

        return finalLoc;
	}

    private (List<Vector3Int>, List<Vector3Int>) GenerateTradeCenter(System.Random random, List<Vector3Int> tradeCenterLocs)
    {
        List<Vector3Int> foodLocs = new();
        List<Vector3Int> waterLocs = new();
        //generating trade center
		for (int i = 0; i < tradeCenterLocs.Count; i++)
		{
			SwampCheck(tradeCenterLocs[i]);
			FloodPlainCheck(tradeCenterLocs[i]);
			ForestCheck(tradeCenterLocs[i]);

			int index = -2;
			Vector3Int harborTile = tradeCenterLocs[i];

			//finding water terrain for harbor
			for (int j = 0; j < ProceduralGeneration.neighborsFourDirections.Count; j++)
			{
				Vector3Int tile = ProceduralGeneration.neighborsFourDirections[j] + tradeCenterLocs[i];
				harborTile = tile;

				if (terrainDict[tile].isLand)
					index++;
				else
					break;
			}

			//adding resources to trade center areas (1) land food (2) water food
		    List<Vector3Int> tcGrasslandTiles = new();
		    List<Vector3Int> tcWaterTiles = new();
			for (int j = 0; j < ProceduralGeneration.neighborsEightDirections.Count; j++)
			{
				Vector3Int tile = ProceduralGeneration.neighborsEightDirections[j] + tradeCenterLocs[i];

				if (tile == harborTile)
					continue;

				if ((terrainDict[tile].terrainData.type == TerrainType.River && !fourWayRiverLocs.Contains(tile)) || terrainDict[tile].terrainData.type == TerrainType.Coast)
					tcWaterTiles.Add(tile);
				else if (terrainDict[tile].CompareTag("Flatland") && terrainDict[tile].terrainData.grassland)
					tcGrasslandTiles.Add(tile);
			}

			int tcWaterCount = tcWaterTiles.Count;
			for (int j = 0; j < tcWaterCount; j++)
			{
				if (j == 2) //max of 2
					break;

				Vector3Int newWater = tcWaterTiles[random.Next(0, tcWaterTiles.Count)];
				AddSeaFood(newWater);
				waterLocs.Add(newWater);
				tcWaterTiles.Remove(newWater);
			}
			int tcFoodCount = tcGrasslandTiles.Count;
			for (int j = 0; j < tcFoodCount; j++)
			{
				if (j == 1) //max of 1
					break;

				Vector3Int newFood = tcGrasslandTiles[random.Next(0, tcGrasslandTiles.Count)];
				FloodPlainCheck(newFood);
				AddFood(random, newFood);
				foodLocs.Add(newFood);
				tcGrasslandTiles.Remove(newFood);
			}

			if (nonBuildTime)
			{
				tradeCenterPositions.Add(tradeCenterLocs[i]);
			}
			else
			{
				terrainDict[tradeCenterLocs[i]].prop.gameObject.SetActive(false);
				terrainDict[tradeCenterLocs[i]].showProp = false;
				Quaternion rotation = Quaternion.Euler(0, index * 90, 0);
				GameObject tradeCenterGO = Instantiate(Resources.Load<GameObject>("Prefabs/TradeCenterPrefabs/" + tradeCenterNames[i]), tradeCenterLocs[i], Quaternion.identity);
				tradeCenterGO.transform.SetParent(tradeCenterHolder, false);
				Quaternion miniRotation = Quaternion.Euler(90, 0, 0);
				TradeCenter center = tradeCenterGO.GetComponent<TradeCenter>();
				center.main.rotation = rotation;
                center.lightHolder.rotation = rotation;
				center.minimapIcon.rotation = miniRotation;
				allTiles.Add(tradeCenterGO);
			}
		}

        return (foodLocs, waterLocs); 
	}

	private List<Vector3Int> GenerateEnemyCamps(System.Random random, Vector3Int startingPlace, List<Vector3Int> landLocs, List<Vector3Int> luxuryLocs, List<Vector3Int> resourceLocs, List<Vector3Int> enemyCityRange, List<Vector3Int> tradeCenterRange, Era selectedEra, Region selectedRegion)
	{
        List<Vector3Int> enemyLocs = new();
        List<Vector3Int> locsLeft = new(landLocs);
        locsLeft.Remove(startingPlace);

		//no enemies within 4 tiles of starting place 
		for (int i = 0; i < ProceduralGeneration.neighborsCityRadius.Count; i++)
        {
            Vector3Int tile = ProceduralGeneration.neighborsCityRadius[i] + startingPlace;

			locsLeft.Remove(tile);
			resourceLocs.Remove(tile);
			luxuryLocs.Remove(tile);

            if (i > 7)
            {
                for (int j = 0; j < ProceduralGeneration.neighborsCityRadius.Count; j++)
                {
                    Vector3Int newTile = ProceduralGeneration.neighborsCityRadius[j] + tile;

					locsLeft.Remove(newTile);
                    resourceLocs.Remove(newTile);
                    luxuryLocs.Remove(newTile);
                }
                
            }
        }

		//no enemies within 2 tiles of enemy cities
		for (int i = 0; i < enemyCityRange.Count; i++)
        {
			locsLeft.Remove(enemyCityRange[i]);
            resourceLocs.Remove(enemyCityRange[i]);
            luxuryLocs.Remove(enemyCityRange[i]);
        }

		//no enemies with in 2 tiles of trade centers
		for (int i = 0; i < tradeCenterRange.Count; i++)
        {
			locsLeft.Remove(tradeCenterRange[i]);
            resourceLocs.Remove(tradeCenterRange[i]);
            luxuryLocs.Remove(tradeCenterRange[i]);
        }

   //     //no enemies between start loc and trade center (if close enough)
   //     if (Mathf.Abs(tradeCenterLocs[0].x - startingPlace.x) + Mathf.Abs(tradeCenterLocs[0].z - startingPlace.z) < 37)
   //     {
   //         Vector3Int vectorDiff = tradeCenterLocs[0] - startingPlace;

   //         bool pos = vectorDiff.x * vectorDiff.z >= 0;
            
   //         int xMax = Math.Max(tradeCenterLocs[0].x, startingPlace.x);
		 //   int xMin = Math.Min(tradeCenterLocs[0].x, startingPlace.x);
		 //   int zMax = Math.Max(tradeCenterLocs[0].z, startingPlace.z);
		 //   int zMin = Math.Min(tradeCenterLocs[0].z, startingPlace.z);

   //         int xDiff = (xMax - xMin) / 3;
   //         int zDiff = (zMax - zMin) / 3;

   //         if (xDiff > zDiff)
   //         {
   //             int stepIntervals = xDiff / (zDiff + 1);

   //             int nextZ = pos ? zMin : zMax;
   //             int increment = pos ? 3 : -3;

			//	for (int i = 0; i < xDiff; i++)
   //             {
   //                 if (i % stepIntervals == 0)
   //                     nextZ += increment;
                    
   //                 Vector3Int tile = new Vector3Int(xMin + i * 3, yCoord, nextZ);

			//		if (locsLeft.Contains(tile))
   //                 {
			//			locsLeft.Remove(tile);
			//			resourceLocs.Remove(tile);
			//			luxuryLocs.Remove(tile);
			//		}

   //                 for (int j = 0; j < ProceduralGeneration.neighborsCityRadius.Count; j++)
   //                 {
   //                     Vector3Int nextTile = ProceduralGeneration.neighborsCityRadius[j] + tile;

			//			if (locsLeft.Contains(nextTile))
   //                     {
			//				locsLeft.Remove(nextTile);
			//				resourceLocs.Remove(nextTile);
   //                         luxuryLocs.Remove(nextTile);
			//			}
			//		}
			//	}
   //         }
   //         else if (zDiff > xDiff)
   //         {
			//	int stepIntervals = zDiff / (xDiff + 1);

			//	int nextX = pos ? xMin : xMax;
			//	int increment = pos ? 3 : -3;

			//	for (int i = 0; i < zDiff; i++)
			//	{
			//		if (i % stepIntervals == 0)
			//			nextX += increment;

			//		Vector3Int tile = new Vector3Int(nextX, yCoord, zMin + i * 3);

			//		if (locsLeft.Contains(tile))
   //                 {
			//			locsLeft.Remove(tile);
			//			resourceLocs.Remove(tile);
			//			luxuryLocs.Remove(tile);
			//		}

			//		for (int j = 0; j < ProceduralGeneration.neighborsCityRadius.Count; j++)
			//		{
			//			Vector3Int nextTile = ProceduralGeneration.neighborsCityRadius[j] + tile;

			//			if (locsLeft.Contains(nextTile))
   //                     {
			//				locsLeft.Remove(nextTile);
			//				resourceLocs.Remove(nextTile);
			//				luxuryLocs.Remove(tile);
			//			}
			//		}
			//	}
			//}
   //         else
   //         {
			//	for (int i = 0; i < xDiff; i++)
			//	{
			//		Vector3Int tile = new Vector3Int(xMin + i * 3, yCoord, zMin + i * 3);

   //                 if (locsLeft.Contains(tile))
   //                 {
			//			locsLeft.Remove(tile);
   //                     resourceLocs.Remove(tile);
			//			luxuryLocs.Remove(tile);
			//		}

			//		for (int j = 0; j < ProceduralGeneration.neighborsCityRadius.Count; j++)
			//		{
			//			Vector3Int nextTile = ProceduralGeneration.neighborsCityRadius[j] + tile;

			//			if (locsLeft.Contains(nextTile))
   //                     {
			//				locsLeft.Remove(nextTile);
			//				resourceLocs.Remove(nextTile);
			//				luxuryLocs.Remove(nextTile);
			//			}
			//		}
			//	}
			//}
   //     }

        int divisor; 

		if (enemyCountDifficulty == 1)
            divisor = 16;
        else if (enemyCountDifficulty == 2)
            divisor = 12;
        else
            divisor = 8;

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

			//making sure there's a way to reach camp by land
			List<Vector3Int> mountainTiles = new();
			for (int i = 0; i < ProceduralGeneration.neighborsFourDirections.Count; i++)
			{
				Vector3Int tile = ProceduralGeneration.neighborsFourDirections[i] + chosenTile;
				if (terrainDict[tile].terrainData.terrainDesc == TerrainDesc.Mountain)
                    mountainTiles.Add(tile);
			}

			if (mountainTiles.Count == 4)
			{
                for (int i = 0; i < 2; i++)
                {
                    Vector3Int mountainTile = mountainTiles[random.Next(0, mountainTiles.Count)];
                    mountainTiles.Remove(mountainTile);
                    string prefabName = terrainDict[mountainTile].terrainData.grassland ? grasslandHillSO.prefabLocs[0] : desertHillSO.prefabLocs[0];
                    GameObject prefab = Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + prefabName);
					DestroyImmediate(terrainDict[mountainTile].gameObject);
				    GenerateTile(prefab, mountainTile, Quaternion.identity, 0);
                }
			}

			if (!nonBuildTime)
                CreateCamp(random, Mathf.Abs(chosenTile.x - startingPlace.x) + Mathf.Abs(chosenTile.z - startingPlace.z), chosenTile, luxury, selectedEra, selectedRegion);
            enemyLocs.Add(chosenTile);
            //terrainDict[chosenTile].enemyCamp = true;
            //terrainDict[chosenTile].enemyZone = true;

			for (int i = 0; i < ProceduralGeneration.neighborsCityRadius.Count; i++)
            {
                Vector3Int tile = ProceduralGeneration.neighborsCityRadius[i] + chosenTile;
                
                //if (i < 8)
                //    terrainDict[tile].enemyZone = true;

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

	private void CreateCamp(System.Random random, int startProximity, Vector3Int campLoc, bool luxury, Era selectedEra, Region selectedRegion)
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

        List<Vector3> campLocs;
        if (campCount < 9)
    		campLocs = new();
        else
			campLocs = new() { centerLoc };

		if (terrainDict[campLoc].isHill)
            centerLoc.y -= 0.5f;

        for (int i = 0; i < ProceduralGeneration.neighborsEightDirectionsOneStep.Count; i++)
            campLocs.Add(ProceduralGeneration.neighborsEightDirectionsOneStep[i] + centerLoc);

        //0 is for infantry, 1 ranged, 2 cavalry
        List<int> unitList = new() { 0, 1, 2 };

        for (int i = 0; i < campCount; i++)
        {
            Vector3 spawnLoc = campLocs[random.Next(0, campLocs.Count)];
            campLocs.Remove(spawnLoc);

            int chosenUnitType = unitList[random.Next(0, 3)];
            UnitType type = RandomlySelectUnitType(chosenUnitType);
            GameObject enemy = enemyUnitDict[selectedEra][selectedRegion][type];

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

            Quaternion rotation;
            if (campCount == 9)
            {
                rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
            }
            else
            {
			    Vector3 direction = centerLoc - spawnLoc;

                if (direction == Vector3.zero)
			        rotation = Quaternion.identity;
			    else
                    rotation = Quaternion.LookRotation(direction, Vector3.up);
            }

			GameObject enemyGO = Instantiate(enemy, spawnLoc, rotation);
            allTiles.Add(enemyGO);
            enemyGO.transform.SetParent(enemyHolder, false);
        }
    }

    public void SetMainPlayerLoc() 
    {
        world.mainPlayer.transform.position = startingPlace;
        Vector3Int cameraLoc = startingPlace;
        cameraLoc.y += 5;
        //world.startingSpotlight.transform.position = cameraLoc;
        //world.spotlight.transform.position = startingPlace;
        world.cameraController.ResetCamLimits(world.RoundToInt(world.mainPlayer.transform.position));
        world.cameraController.CenterCameraNoFollow(startingPlace);
        //world.water.transform.position = new Vector3(width / 2f * 3, yCoord - 0.06f, height / 2f * 3);
        //world.water.minimapIcon.localScale = new Vector3(0.14f * width, 1.8f, 0.14f * height);
	}

    public void SetAuroraBorealis(Vector3 loc)
    {
        Quaternion rotation = Quaternion.Euler(0, 90, 0);
        GameObject aurora1 = Instantiate(Resources.Load<GameObject>("Prefabs/MiscPrefabs/AuroraGreen"), loc, rotation);
        aurora1.GetComponent<AuroraBorealis>().SetCam(world.mainCam.transform);
        aurora1.transform.SetParent(world.transform, false);
        loc.y += 0.6f;
		GameObject aurora2 = Instantiate(Resources.Load<GameObject>("Prefabs/MiscPrefabs/AuroraPurple"), loc, rotation);
        aurora2.GetComponent<AuroraBorealis>().SetCam(world.mainCam.transform);
        aurora2.transform.SetParent(world.transform, false);
	}

    public UnitType RandomlySelectUnitType(int num)
    {
		switch (num)
		{
			case 0:
				return UnitType.Infantry;
			case 1:
				return UnitType.Ranged;
			case 2:
				return UnitType.Cavalry;
		}

        return UnitType.Infantry;
	}

    public void SetYCoord(int yCoord)
    {
        this.yCoord = yCoord;
    }

    public void Clear()
    {
        propTiles.Clear();
        allTiles.Clear();
        terrainDict.Clear();
        enemyCityLocs.Clear();
        enemyEmpires.Clear();
        enemyRoadLocs.Clear();
        fourWayRiverLocs.Clear();
    }
}


