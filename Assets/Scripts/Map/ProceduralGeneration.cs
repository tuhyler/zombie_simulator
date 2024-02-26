using System;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralGeneration
{
    //should probably turn into enum
    
    public static int landPlaceholder = 0;
    public static int sea = 1;
    public static int hill = 2;
    public static int grasslandHill = 3;
    public static int desertHill = 4;
    public static int forestHill = 5;
    public static int jungleHill = 6;
    public static int mountain = 7;
    public static int grasslandMountain = 8;
    public static int desertMountain = 9;
    public static int grassland = 10;
    public static int desert = 11;
    public static int forest = 12;
    public static int jungle = 13;
    public static int swamp = 14;
    public static int river = 15;
    public static int grasslandFloodPlain = 16; 
    public static int desertFloodPlain = 17;
    private static int increment = 3;

    public readonly static List<Vector3Int> neighborsFourDirections = new()
    {
        new Vector3Int(0,0,increment), //up
        new Vector3Int(increment,0,0), //right
        new Vector3Int(0,0,-increment), //down
        new Vector3Int(-increment,0,0), //left
    };

    public readonly static List<Vector3Int> neighborsEightDirections = new()
    {
        new Vector3Int(0,0,increment), //up
        new Vector3Int(increment,0,increment), //upper right
        new Vector3Int(increment,0,0), //right
        new Vector3Int(increment,0,-increment), //lower right
        new Vector3Int(0,0,-increment), //down
        new Vector3Int(-increment,0,-increment), //lower left
        new Vector3Int(-increment,0,0), //left
        new Vector3Int(-increment,0,increment), //upper left
    };

	public readonly static List<Vector3Int> neighborsEightDirectionsOneStep = new()
	{
		new Vector3Int(0,0,1), //up
        new Vector3Int(1,0,1), //upper right
        new Vector3Int(1,0,0), //right
        new Vector3Int(1,0,-1), //lower right
        new Vector3Int(0,0,-1), //down
        new Vector3Int(-1,0,-1), //lower left
        new Vector3Int(-1,0,0), //left
        new Vector3Int(-1,0,1), //upper left
    };

	public readonly static List<Vector3Int> neighborsCityRadius = new()
	{
		new Vector3Int(0,0,increment), //up
        new Vector3Int(increment,0,increment), //upper right
        new Vector3Int(increment,0,0), //right
        new Vector3Int(increment,0,-increment), //lower right
        new Vector3Int(0,0,-increment), //down
        new Vector3Int(-increment,0,-increment), //lower left
        new Vector3Int(-increment,0,0), //left
        new Vector3Int(-increment,0,increment), //upper left
        new Vector3Int(0,0,2*increment), //up up
        new Vector3Int(increment,0,2*increment), //up up right
        new Vector3Int(2*increment,0,2*increment), //upper right corner
        new Vector3Int(2*increment,0,increment), //up right right
        new Vector3Int(2*increment,0,0), //right right
        new Vector3Int(2*increment,0,-increment), //right right down
        new Vector3Int(2*increment,0,-2*increment), //lower right corner
        new Vector3Int(increment,0,-2*increment), //down down right
        new Vector3Int(0,0,-2*increment), //down down
        new Vector3Int(-increment,0,-2*increment), //down down left
        new Vector3Int(-2*increment,0,-2*increment), //lower left corner
        new Vector3Int(-2*increment,0,-increment), //left left down
        new Vector3Int(-2*increment,0,0), //left left
        new Vector3Int(-2*increment,0,increment), //left left up
        new Vector3Int(-2*increment,0,2*increment), //upper left corner
        new Vector3Int(-increment,0,2*increment), //up up left
    };

	public static Dictionary<Vector3Int, float> PerlinNoiseGenerator(Dictionary<Vector3Int, int> positions, 
        float scale, int octaves, float persistance, float lacunarity, int seed, float offset)
    {
        List<float> noiseList = new();
        List<Vector3Int> landPosList = new();
        Dictionary<Vector3Int, float> noiseMap = new();

        foreach (Vector3Int pos in positions.Keys)
        {
            if (positions[pos] == 0)
            {
                landPosList.Add(pos);
            }
        }

        float max = 0;
        float min = 0;

        System.Random random = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = (random.Next(-100000, 100000) + offset);
            float offsetY = (random.Next(-100000, 100000) + offset);
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        //clamping
        if (scale <= 0)
            scale = 0.0001f;
        if (lacunarity < 1)
            lacunarity = 1;

        foreach (Vector3Int pos in landPosList)
        {
            float amplitude = 1;
            float frequency = 1;
            float noiseHeight = 0;

            for (int k = 0; k < octaves; k++)
            {
                float xCoord = (float)pos.x / (scale * frequency) + octaveOffsets[k].x;
                float zCoord = (float)pos.z / (scale * frequency) + octaveOffsets[k].y;

                //patches are messy
                //float simplexValue = Unity.Mathematics.noise.snoise(new Unity.Mathematics.float4(xCoord, 3.0f, zCoord, 1.0f));
                //noiseHeight += simplexValue * amplitude;

                //cellular (patches too big, scale to decrease in size but get messy)
                //Unity.Mathematics.float2 cellularValue = Unity.Mathematics.noise.cellular2x2(new Unity.Mathematics.float2(xCoord, zCoord));
                //noiseHeight += cellularValue.x * amplitude;

                float perlinValue = Mathf.PerlinNoise(xCoord, zCoord) * 2 - 1;
                noiseHeight += perlinValue * amplitude;

                //float avgNoise = (simplexValue + simplexValue + perlinValue) / 3;
                //noiseHeight += avgNoise * amplitude;

                amplitude *= persistance;
                frequency *= lacunarity;
            }

            //getting max and min of list
            if (noiseHeight > max)
                max = noiseHeight;
            if (noiseHeight < min)
                min = noiseHeight;

            noiseList.Add(noiseHeight);
        }

        //Normalizing values
        for (int i = 0; i < noiseList.Count; i++)
        {
            noiseList[i] = Mathf.InverseLerp(min, max, noiseList[i]);
        }

        //populating noise dict
        for (int i = 0; i < landPosList.Count; i++)
        {
            noiseMap[landPosList[i]] = noiseList[i];
        }

        return noiseMap;
    }

    public static Dictionary<Vector3Int, int> AddOceanRing(Dictionary<Vector3Int, int> mainMap, int width, int height, int yCoord, int depth)
    {
        for (int i = -depth; i < width + depth; i++)
        {
            for (int j = -depth; j < height + depth; j++)
            {
                Vector3Int oceanTile = new Vector3Int(i*increment, yCoord, j*increment);
                if (!mainMap.ContainsKey(oceanTile))
                    mainMap[oceanTile] = sea;
            }
        }

        return mainMap;
    }

    public static Dictionary<Vector3Int, int> ConvertOceanToRivers(Dictionary<Vector3Int, int> mainMap)
    {
        Dictionary<Vector3Int, int> mapTiles = new(mainMap);

        for (int k = 0; k < 2; k++) //2 passes
        {
            foreach (Vector3Int pos in mainMap.Keys)
            {
                int[] neighborDirectTerrainLoc = new int[4] { 0, 0, 0, 0 };
                int neighborDirectCount = 0;
                int[] neighborTerrainLoc = new int[4] { 0, 0, 0, 0 };
                int neighborCount = 0;
                int i = 0;

                if (mainMap[pos] == sea)
                {
                    foreach (Vector3Int neighbor in neighborsEightDirections)
                    {
                        Vector3Int neighborPos = neighbor + pos;

                        if (!mainMap.ContainsKey(neighborPos))
                            continue;

                        if (mainMap[neighborPos] != sea) //|| mainMap[neighborPos] == river)
                        {
                            if (i % 2 == 0)
                            {
                                neighborDirectTerrainLoc[i / 2] = 1;
                                neighborDirectCount++;
                            }
                            else
                            {
                                neighborTerrainLoc[i / 2] = 1;
                                neighborCount++;
                            }
                        }

                        i++;
                    }
                }

                if (neighborDirectCount == 0)
                {
                    if (neighborCount == 4)
                    {
                        mapTiles[pos] = river;
                    }
                }
                else if (neighborDirectCount == 1)
                {
                    if (neighborCount >= 2)
                    {
                        int index = Array.FindIndex(neighborDirectTerrainLoc, x => x == 1);
                        int cornerOne = index + 1;
                        int cornerTwo = index + 2;
                        cornerOne = cornerOne == 4 ? 0 : cornerOne;
                        cornerTwo = cornerTwo == 4 ? 0 : cornerTwo;
                        cornerTwo = cornerTwo == 5 ? 1 : cornerTwo;

                        if (cornerOne == 1 && cornerTwo == 1)
                            mapTiles[pos] = river;
                    }
                }
                else if (neighborDirectCount == 2)
                {
                    int[] holder = new int[2];

                    for (int j = 0; j < 2; j++)
                    {
                        holder[j] = Array.FindIndex(neighborDirectTerrainLoc, x => x == 1);
                        neighborDirectTerrainLoc[holder[j]] = 0; //setting to zero so it doesn't find the first one again
                    }

                    int max = Mathf.Max(holder);
                    int min = Mathf.Min(holder);

                    if ((max - min) % 2 == 0) //for straight across
                        mapTiles[pos] = river;
                    else
                    {
                        if (neighborCount >= 1)
                        {
                            if (max < 3 && neighborTerrainLoc[max + 1] == 1)
                                mapTiles[pos] = river;
                            else if (min == 0 && max == 3 && neighborTerrainLoc[min + 1] == 1)
                                mapTiles[pos] = river;
                            else if (min == 2 && neighborTerrainLoc[0] == 1)
                                mapTiles[pos] = river;
                        }
                    }

                }
                else if (neighborDirectCount == 3)
                {
                    mapTiles[pos] = river;
                }
                else
                {
                    mapTiles[pos] = river;
                }

            }

            mainMap = new(mapTiles);
        }

        return mapTiles;
    }

    public static Dictionary<Vector3Int, int> MergeMountainTerrain(System.Random random, Dictionary<Vector3Int, int> mainMap, Dictionary<Vector3Int, int> mountainMap, int resourceFrequency)
    {
        int limit;

        if (resourceFrequency == 3)
            limit = 1; //100%
        else if (resourceFrequency == 4)
            limit = 4; //75% 
        else
            limit = 6; //50%
        
        foreach (Vector3Int tile in mountainMap.Keys)
        {
            if (mountainMap[tile] == mountain)
            {
                if (mainMap[tile] == grassland || mainMap[tile] == forest || mainMap[tile] == jungle || mainMap[tile] == swamp)
                    mainMap[tile] = grasslandMountain;
                if (mainMap[tile] == desert)
                    mainMap[tile] = desertMountain;
            }
            else if (mountainMap[tile] == hill)
            {
                //mainMap[tile] = GetHill(mainMap[tile]);

                if (mainMap[tile] == desert)
                    mainMap[tile] = desertHill;
                else if (mainMap[tile] == forest)
                    mainMap[tile] = forestHill;
                else if (mainMap[tile] == jungle || mainMap[tile] == swamp)
                    mainMap[tile] = jungleHill;
                else if (mainMap[tile] == grassland)
                    mainMap[tile] = grasslandHill;
            }
            else if (mountainMap[tile] == river)
            {
                mainMap[tile] = river;

                foreach (Vector3Int neighbor in neighborsFourDirections)
                {
                    Vector3Int neighborPos = neighbor + tile;

                    if (!mainMap.ContainsKey(neighborPos))
                        continue;

                    int floodPlain = random.Next(0, limit);

                    if (mainMap[neighborPos] == grassland)
                    {
                        int terrain = floodPlain < 3 ? grasslandFloodPlain : grassland;
                        mainMap[neighborPos] = terrain;
                    }
                    else if (mainMap[neighborPos] == desert)
                    {
						int terrain = floodPlain < 3 ? desertFloodPlain : desert;
						mainMap[neighborPos] = terrain;
                    }
                }
            }
            else if (mountainMap[tile] == sea && mainMap[tile] != sea)
            {
                mainMap[tile] = sea;
            }
        }

        return mainMap;
    }

    public static Dictionary<Vector3Int, int> GenerateTerrain(Dictionary<Vector3Int, int> mainTiles, Dictionary<Vector3Int, float> mainNoise,
        float lowerThreshold, float upperThreshold, int desertPerc, int forestPerc,
        int width, int height, int yCoord, int equatorDist, int equatorPos, int seed)
    {
        System.Random random = new System.Random(seed);

        //getting sections of maps to assign terrain to
        Dictionary<int, List<Vector3Int>> terrainRegions = GetTerrainRegions(mainNoise, lowerThreshold, upperThreshold);
        int allTilesCount = 0;

        //getting count of all land tiles
        foreach (int key in terrainRegions.Keys)
        {
            if (key != 0) //0 is grasslandVar00
                allTilesCount += terrainRegions[key].Count;
        }

        //setting clamps
        if (desertPerc <= 0)
            desertPerc = 0;
        if (forestPerc <= 0)
            forestPerc = 0;

        int allPercs = desertPerc + forestPerc;

        if (allPercs <= 0)
        {
            desertPerc = 33;
            forestPerc = 67;
            allPercs = 100;
        }

        if (allPercs != 100)
        {
            desertPerc = Mathf.RoundToInt((desertPerc / (float)allPercs) * 100);
            forestPerc = Mathf.RoundToInt((forestPerc / (float)allPercs) * 100);
        }

        int totalDesert = (int)((desertPerc / 100f) * allTilesCount);
        int totalForest = (int)((forestPerc / 100f) * allTilesCount);

        int desertCount = 0;
        int forestCount = 0;

        //if (lowerThreshold > upperThreshold)
        //    lowerThreshold = upperThreshold;

        foreach (int region in terrainRegions.Keys)
        {
            //float desertFillPerc = desertCount / (float)totalDesert; //percentage desert is filled by iteration
            //float forestFillPerc = forestCount / (float)totalForest;

            desertPerc = Mathf.RoundToInt(((float)totalDesert - desertCount) / (totalDesert + totalForest - desertCount - forestCount) * 100);
            forestPerc = Mathf.RoundToInt(((float)totalForest - forestCount) / (totalDesert + totalForest - desertCount - forestCount) * 100);

            //desertPerc -= Mathf.RoundToInt(desertPerc * desertFillPerc);
            //forestPerc -= Mathf.RoundToInt(forestPerc * forestFillPerc);
            //desertPerc = Mathf.RoundToInt(((float)desertPerc / (forestPerc + desertPerc)) * 100);
            //forestPerc = Mathf.RoundToInt(((float)forestPerc / (forestPerc + desertPerc)) * 100);

            //terrain randomly chosen based on changing forestPerc to desertPerc ratio
            int chosenTerrain = random.Next(0, 100) > forestPerc ? desert : forest;
            int prevTerrain = chosenTerrain;

            if (region == 0)
                chosenTerrain = grassland; //first region (borders of noise regions) is grasslandVar00
            
            foreach (Vector3Int tile in terrainRegions[region])
            {
                if (region != 0 && tile.z < equatorPos + equatorDist && tile.z > equatorPos - equatorDist) //all terrain on equator is jungle
                    chosenTerrain = jungle;

                if (region !=0 && (tile.z == equatorPos + equatorDist || tile.z == equatorPos - equatorDist)) //terrain next to equator is sometimes jungle
                {
                    chosenTerrain = random.Next(0,3) < 2 ? jungle : chosenTerrain;
                }

                mainTiles[tile] = chosenTerrain;

                if (chosenTerrain == desert)
                    desertCount++;
                else if (chosenTerrain == forest)
                    forestCount++;
                else if (chosenTerrain == jungle)
                {
                    //jungleCount++;
                    forestCount++;
                    chosenTerrain = prevTerrain;
                }

            }
        }

        mainTiles = RemoveSingleIslands(mainTiles);
        mainTiles = RemoveSingles2(mainTiles, width, height, yCoord);
		mainTiles = RemoveSingles2(mainTiles, width, height, yCoord);
		mainTiles = GenerateSwamps(mainTiles, width, height, yCoord, seed);

        return mainTiles;
    }

    private static Dictionary<Vector3Int, int> GenerateSwamps(Dictionary<Vector3Int, int> mainTiles, int width, int height, int yCoord, int seed)
    {
        System.Random random = new System.Random(seed);
        
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector3Int currentTile = new Vector3Int(i*increment, yCoord, j*increment);

                if (mainTiles[currentTile] == jungle)
                {
                    int seaCount = 0;
                    
                    foreach (Vector3Int neighbor in neighborsEightDirections)
                    {
                        if (!mainTiles.ContainsKey(currentTile + neighbor) || mainTiles[currentTile + neighbor] == sea)
                            seaCount++;
                    }

                    if (seaCount > 0 && random.Next(0,5) < 4)
                        mainTiles[currentTile] = swamp;
                }
            }
        }

        return mainTiles;
    }

    public static Dictionary<Vector3Int, int> GenerateCellularAutomata(int threshold, int width, int height, int iterations, 
        int randomFillPercent, int seed, int yCoord)
    {
        Dictionary<Vector3Int, int> newTiles = new();
        Dictionary<Vector3Int, int> tileHolder = new();
        Dictionary<Vector3Int, int> randomTiles = RandomFillMap(width, height, randomFillPercent, seed, yCoord);

        for (int i = 0; i < iterations; i++)
        {
            if (i > 0)
                tileHolder = new(newTiles);
            else
                tileHolder = new(randomTiles);

            foreach (Vector3Int tile in randomTiles.Keys)
            {
                int one = 0;

                foreach (Vector3Int pos in neighborsEightDirections)
                {
                    if (randomTiles.ContainsKey(pos + tile))
                    {
                        if (tileHolder[pos + tile] == sea)
                        {
                            one++;
                        }
                    }
                    else
                    {
                        one++;
                    }
                }

                if (one >= threshold)
                {
                    newTiles[tile] = sea;
                }
                else
                {
                    newTiles[tile] = landPlaceholder;
                }
            }
        }

        newTiles = RemoveSingles(newTiles, width, height, yCoord, landPlaceholder, sea);
        return newTiles;
    }

    private static Dictionary<Vector3Int, int> RemoveSingles2(Dictionary<Vector3Int, int> mapDict, int width, int height, int yCoord)
    {
        Dictionary<Vector3Int, int> newMapDict = new(mapDict);
        List<int> desertList = new() { desert, desertFloodPlain, desertHill, desertMountain };
        List<int> grasslandList = new() { grassland, grasslandFloodPlain, grasslandHill, grasslandMountain, forest, forestHill, jungle, jungleHill, swamp };
        List<int> waterList = new() { sea, river };
        List<int> exemptionList = new() { sea, river, forest, forestHill, jungle, jungleHill, swamp };

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Vector3Int currentTile = new Vector3Int(i * increment, yCoord, j * increment);

                if (exemptionList.Contains(mapDict[currentTile]))
                    continue;

                bool isDesert = false;

                if (desertList.Contains(mapDict[currentTile]))
                    isDesert = true;

                int desertCount = 0;
                int grasslandCount = 0;
                int waterCount = 0;
                int desertPriority = 0;
                int grasslandPriority = 0;

                for (int k = 0; k < neighborsEightDirections.Count; k++)
                {
                    Vector3Int tile = currentTile + neighborsEightDirections[k];
                    if (!mapDict.ContainsKey(tile))
                        continue;

                    if (grasslandList.Contains(mapDict[tile]))
                    {
                        grasslandCount++;
                        if (k % 2 == 0)
                            grasslandPriority++;
                    }
                    else if (desertList.Contains(mapDict[tile]))
                    {
                        desertCount++;
                        if (k % 2 == 0)
                            desertPriority++;
                    }
                    else if (waterList.Contains(mapDict[tile]))
                        waterCount++;
                }

                //switching to more common terrain
                if (isDesert)
                {
                    int diff = grasslandCount - desertCount;

                    if (diff > 1)
                    {
						int currentTerrain = mapDict[currentTile];
						int reverseTerrain = grassland;

						if (currentTerrain == desert)
							reverseTerrain = grassland;
						else if (currentTerrain == desertFloodPlain)
							reverseTerrain = grasslandFloodPlain;
						else if (currentTerrain == desertHill)
							reverseTerrain = grasslandHill;
						else if (currentTerrain == desertMountain)
							reverseTerrain = grasslandMountain;

						if (diff > 2)
							newMapDict[currentTile] = reverseTerrain;
                        else if (diff == 2)
                        {
                            if (desertPriority < 2)
								newMapDict[currentTile] = reverseTerrain;
                        }
                    }
                }
				else
				{
					int diff = desertCount - grasslandCount;

                    if (diff > 1)
                    {
						int currentTerrain = mapDict[currentTile];
						int reverseTerrain = desert;

						if (currentTerrain == grassland)
							reverseTerrain = desert;
						else if (currentTerrain == grasslandFloodPlain)
							reverseTerrain = desertFloodPlain;
						else if (currentTerrain == grasslandHill)
							reverseTerrain = desertHill;
						else if (currentTerrain == grasslandMountain)
							reverseTerrain = desertMountain;

						if (diff > 2)
							newMapDict[currentTile] = reverseTerrain;
						else if (diff == 2)
						{
							if (grasslandPriority < 2)
								newMapDict[currentTile] = reverseTerrain;
						}
					}
				}

			}
        }

        return newMapDict;
    }

    private static Dictionary<Vector3Int, int> RandomFillMap(int width, int height, int randomFillPercent, int seed, int yCoord)
    {
        Dictionary<Vector3Int, int> randomTiles = new();

        System.Random random = new System.Random(seed.GetHashCode());

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                randomTiles[new Vector3Int(i*increment, yCoord, j*increment)] = random.Next(0, 100) < randomFillPercent ? sea : landPlaceholder;
            }
        }

        return randomTiles;
    }

    private static Dictionary<Vector3Int, int> RemoveSingleIslands(Dictionary<Vector3Int, int> mapDict)
    {
        Dictionary<Vector3Int, int> tempDict = new(mapDict);
        foreach (Vector3Int tile in mapDict.Keys)
        {
            if (mapDict[tile] != 1)
            {
                int seaCount = 0;
                for (int i = 0; i < neighborsFourDirections.Count; i++)
                {
                    Vector3Int newTile = tile + neighborsFourDirections[i];

                    if (!mapDict.ContainsKey(newTile) || mapDict[newTile] == 1)
                        seaCount++;
                    else
                        break;
                }

                if (seaCount == 4)
                    tempDict[tile] = 1;
            }
        }
        
        return tempDict;
    }

    private static Dictionary<Vector3Int, int> RemoveSingles(Dictionary<Vector3Int, int> mapDict, int width, int height, int yCoord,
        int endsTerrain, int middleTerrain, bool keep = true)
    {
        int boundary1;
        int boundary2;

        for (int k = 0; k < 2; k++) //to change directions 
        {
            if (k == 0)
            {
                boundary1 = width;
                boundary2 = height;
            }
            else
            {
                boundary1 = height;
                boundary2 = width;
            }

            Vector3Int currentTile;
            Vector3Int prevTile = Vector3Int.zero;
            int firstPrevType = 0;
            int secondPrevType = 0;

            for (int i = 0; i < boundary1; i++)
            {
                for (int j = 0; j < boundary2; j++)
                {
                    if (k == 0)
                        currentTile = new Vector3Int(i*increment, yCoord, j*increment);
                    else
                        currentTile = new Vector3Int(j*increment, yCoord, i*increment);

                    int currentType = mapDict[currentTile];

                    if (currentType == endsTerrain && firstPrevType == middleTerrain && secondPrevType == endsTerrain)
                    {
                        if (keep)
                            mapDict[currentTile] = middleTerrain;
                        else
                            mapDict[prevTile] = endsTerrain;
                    }

                    prevTile = currentTile;
                    secondPrevType = firstPrevType;
                    firstPrevType = currentType;
                }
            }
        }

        return mapDict;
    }

	public static Dictionary<int, List<Vector3Int>> GetLandMasses(Dictionary<Vector3Int, int> mapDict)
    {
        Dictionary<int, List<Vector3Int>> landMassDict = new();
        List<Vector3Int> checkedPositions = new();
        Queue<Vector3Int> queue = new();
        int landMassCount = 0;

        foreach (Vector3Int pos in mapDict.Keys)
        {
            if (checkedPositions.Contains(pos) || mapDict[pos] == sea)
                continue;

            queue.Enqueue(pos);
            checkedPositions.Add(pos);
            landMassDict[landMassCount] = new List<Vector3Int>();

            while (queue.Count > 0)
            {
                Vector3Int pos2 = queue.Dequeue();

                foreach (Vector3Int neighbor in neighborsFourDirections)
                {
                    Vector3Int neighborCheck = pos2 + neighbor;

                    if (mapDict.ContainsKey(neighborCheck) && !checkedPositions.Contains(neighborCheck))
                    {
                        checkedPositions.Add(neighborCheck);

                        if (mapDict[neighborCheck] != sea)
                        {
                            landMassDict[landMassCount].Add(neighborCheck);
                            queue.Enqueue(neighborCheck);
                        }
                    }
                }
            }

            landMassCount++;
        }

        return landMassDict;
    }

    //enough differences in this code to warrant a separate method
    private static Dictionary<int, List<Vector3Int>> GetTerrainRegions(Dictionary<Vector3Int, float> mapDict, 
        float lowerThreshold, float upperThreshold)
    {
        Dictionary<int, List<Vector3Int>> landMassDict = new();
        landMassDict[0] = new List<Vector3Int>();

        List<Vector3Int> checkedPositions = new();
        Queue<Vector3Int> queue = new();
        int landMassCount = 1;

        foreach (Vector3Int pos in mapDict.Keys)
        {
            if (mapDict[pos] > lowerThreshold && mapDict[pos] < upperThreshold)
            {
                landMassDict[0].Add(pos); //first region is borders of noise groups
                checkedPositions.Add(pos);
                continue;
            }
            
            if (checkedPositions.Contains(pos))
                continue;

            queue.Enqueue(pos);
            checkedPositions.Add(pos);
            landMassDict[landMassCount] = new List<Vector3Int>();

            while (queue.Count > 0)
            {
                Vector3Int pos2 = queue.Dequeue();
                landMassDict[landMassCount].Add(pos2);
                bool upper = mapDict[pos] >= upperThreshold;

                foreach (Vector3Int neighbor in neighborsFourDirections)
                {
                    Vector3Int neighborCheck = pos2 + neighbor;

                    if (mapDict.ContainsKey(neighborCheck) && !checkedPositions.Contains(neighborCheck))
                    {
                        if (upper)
                        {
                            if (mapDict[neighborCheck] >= upperThreshold)
                            {
                                landMassDict[landMassCount].Add(neighborCheck);
                                queue.Enqueue(neighborCheck);
                                checkedPositions.Add(neighborCheck);
                            }
                        }
                        else
                        {
                            if (mapDict[neighborCheck] <= lowerThreshold)
                            {
                                landMassDict[landMassCount].Add(neighborCheck);
                                queue.Enqueue(neighborCheck);
                                checkedPositions.Add(neighborCheck);
                            }
                        }
                    }
                }
            }

            landMassCount++;
        }

        return landMassDict;
    }

    public static Dictionary<Vector3Int, int> GenerateMountainRanges(Dictionary<Vector3Int, int> mapDict, Dictionary<int, List<Vector3Int>> landMassDict,
        int mountainPerc, int mountainousPerc, int mountainRangeLength, int width, int height, int yCoord, int seed) 
    {
        System.Random random = new System.Random(seed);

        Dictionary<Vector3Int, int> mainTiles = new(mapDict);
        //Dictionary<int, List<Vector3Int>> landMassDict = GetLandMasses(mainTiles);
        Queue<Vector3Int> mountainStarts = new();
        List<Vector3Int> allTiles = new();
        bool justHills = false;

        foreach (int key in landMassDict.Keys)
        {
            if (landMassDict[key].Count > 0)
            {
                mountainStarts.Enqueue(landMassDict[key][random.Next(0, landMassDict[key].Count)]);
                allTiles.AddRange(landMassDict[key]);
            }
        }

        int mountainCount = 0;
        int mountainTotal = Mathf.RoundToInt((mountainPerc / 100f) * allTiles.Count);

        while (mountainStarts.Count > 0)
        {
            Vector3Int startPosition = mountainStarts.Dequeue();
            List<Vector3Int> sideTileList = new();
            
            mainTiles[startPosition] = hill; //first position is always hill
            mountainCount++;
            allTiles.Remove(startPosition);

            Queue<Vector3Int> queue = new();
            queue.Enqueue(startPosition);
            int newDirection = 0;

            int[] alternateDirections = DirectionSetUp(random.Next(0, 4));

            while (queue.Count > 0)
            {
                Vector3Int prevTile = queue.Dequeue();
                Vector3Int nextTile;

                if (newDirection == 0)
                    newDirection = random.Next(0, 3); 
                else if (newDirection == 1)
                    newDirection = random.Next(0, 2); //can't go backwards
                else
                    newDirection = new int[2] { 0, 2 }[random.Next(0, 2)]; //can't go backwards

                Vector3Int tileShift = neighborsFourDirections[alternateDirections[newDirection]];
                nextTile = prevTile + tileShift;

                if (!mainTiles.ContainsKey(nextTile) || mountainCount >= mountainTotal)
                    continue;

                if (mainTiles[nextTile] != sea)
                {
                    //int hill = dhill;
                    //int mountain = dmountain; 
                    
                    //if (mainTiles[nextTile] == desert)
                    //    mountain = desertMountain;

                    if (!justHills)
                        mainTiles[nextTile] = random.Next(0, 100) > mountainousPerc ? hill : mountain; 
                    else
                        mainTiles[nextTile] = hill;

                    mountainCount++;
                    allTiles.Remove(nextTile);
                    sideTileList.Remove(nextTile);

                    //adding tiles to the side tile list
                    if (tileShift.x == 0)
                    {
                        if (random.Next(0,5) >= 1)
                            sideTileList.Add(nextTile + new Vector3Int(-1, 0, 0));
                        if (justHills && random.Next(0,5) < 1)
                            sideTileList.Add(nextTile + new Vector3Int(1, 0, 0));
                        else if (!justHills && random.Next(0, 5) >= 1)
                            sideTileList.Add(nextTile + new Vector3Int(1, 0, 0));
                    }
                    else
                    {
                        if (random.Next(0, 5) >= 1)
                            sideTileList.Add(nextTile + new Vector3Int(0, 0, -1));
                        if (justHills && random.Next(0, 5) < 1)
                            sideTileList.Add(nextTile + new Vector3Int(0, 0, 1));
                        else if (!justHills && random.Next(0, 5) >= 1)
                            sideTileList.Add(nextTile + new Vector3Int(0, 0, 1));
                    }

                    if (random.Next(0, mountainRangeLength) > 0)
                        queue.Enqueue(nextTile);
                }
                else if (mainTiles[nextTile] == mountain)
                {
                    mainTiles[prevTile] = hill; //turn back into hill if encountered mountain
                }
            }

            //adding hills to the side
            foreach (Vector3Int tile in sideTileList)
            {
                if (mainTiles.ContainsKey(tile) && mainTiles[tile] != sea)
                {
                    mainTiles[tile] = hill;
                    mountainCount++;
                    allTiles.Remove(tile);
                }
            }

            //if more mountain tiles need to be placed, get random position
            if (mountainStarts.Count == 0 && mountainCount < mountainTotal)
            {
                if (allTiles.Count > 0)
                {
                    mountainStarts.Enqueue(allTiles[random.Next(0, allTiles.Count)]);
                    justHills = random.Next(0, 6) < 1;
                }
            }
        }

        //cleaning up diagonals
        mainTiles = DiagonalCheck(mainTiles, width, height, yCoord);

        return mainTiles;
    }

    //private static int GetHill(int terrainType)
    //{
    //    int hillType = grasslandHillVar00;

    //    if (terrainType == desert)
    //        hillType = desertHill;
    //    else if (terrainType == forest)
    //        hillType = forestHill;
    //    else if (terrainType == jungle || terrainType == swamp)
    //        hillType = jungleHill;
    //    else if (terrainType == grasslandVar00)
    //        hillType = grasslandHillVar00;

    //    return hillType;
    //}

    //getting vector3s that are diagonal and perpendicular to main direction
    private static int[] DirectionSetUp(int mainDirection)
    {
        int[] directions = new int[3];

        directions[0] = mainDirection;

        if (mainDirection == 0)
            directions[1] = 3;
        else
            directions[1] = mainDirection - 1;

        if (mainDirection == 3)
            directions[2] = 0;
        else
            directions[2] = mainDirection + 1;

        return directions;
    }

    public static Dictionary<Vector3Int, int> DiagonalCheck(Dictionary<Vector3Int, int> mapDict, int width, int height, int yCoord)
    {
        for (int k = 0; k < 2; k++) //2 passes
        {
            for (int i = 0; i < width - 1; i++)
            {
                for (int j = 0; j < height - 1; j++)
                {
                    Vector3Int tileA = new(i*increment, yCoord, j *increment);
                    Vector3Int tileB = new(i*increment, yCoord, j *increment + increment);
                    Vector3Int tileC = new(i*increment + increment, yCoord, j*increment + increment);
                    Vector3Int tileD = new(i*increment + increment, yCoord, j*increment);

                    if (mapDict[tileA] == sea && mapDict[tileC] == sea && mapDict[tileB] != sea && mapDict[tileD] != sea)
                        mapDict[tileB] = sea;

                    if (mapDict[tileA] != sea && mapDict[tileC] != sea && mapDict[tileB] == sea && mapDict[tileD] == sea)
                        mapDict[tileA] = sea;

                    if (mapDict[tileA] == mountain && mapDict[tileC] == mountain && mapDict[tileB] != mountain && mapDict[tileD] != mountain)
                        mapDict[tileA] = hill;

                    if (mapDict[tileA] != mountain && mapDict[tileC] != mountain && mapDict[tileB] == mountain && mapDict[tileD] == mountain)
                        mapDict[tileB] = hill;
                }
            }
        }

        return mapDict;
    }

    public static Dictionary<Vector3Int, int> GenerateRivers(Dictionary<Vector3Int, int> mapDict, /*int riverPerc,*/ int riverCountMin, int seed)
    {
        System.Random random = new System.Random(seed);

        Queue<Vector3Int> riverStarts = new();
        List<Vector3Int> riverStartOptions = new();
        //int totalTileCount = 0;
        //List<Vector3Int> allTiles = new();
        List<Vector3Int> potentialStartTiles = new();
        int riverTileCount = 0;
        int riverCount = 0;

        //getting tiles distant from edge and sea (5 tiles)
        foreach (Vector3Int tile in mapDict.Keys)
        {
            //allTiles.Add(tile);
            //totalTileCount++;
            int seaCheck = 0;

            foreach (Vector3Int neighbor in neighborsFourDirections)
            {   
                if (mapDict.ContainsKey(tile + neighbor * 5) && mapDict[tile + neighbor] != sea && mapDict[tile + neighbor * 2] != sea
                    /*&& mapDict[tile + neighbor * 3] != sea && mapDict[tile + neighbor * 4] != sea*/ /*&& mapDict[tile + neighbor * 5] != sea*/)
                {
                    seaCheck++;
                }
            }

            if (seaCheck == 4)
                riverStartOptions.Add(tile);
        }

        //int riverTileTotal = Mathf.RoundToInt((riverPerc/100f) * totalTileCount);

        //getting tiles at depth next to mountains
        foreach (Vector3Int tile in riverStartOptions)
        {
            if (mapDict[tile] == mountain || mapDict[tile] == hill)
            {
                foreach(Vector3Int neighbor in neighborsFourDirections)
                {
                    Vector3Int neighborTile = tile + neighbor;

                    if (mapDict.ContainsKey(neighborTile) && mapDict[neighborTile] != sea && mapDict[neighborTile] != mountain)
                    {
                        potentialStartTiles.Add(neighborTile);
                    }
                }   
            }
        }

        //getting river starting points
        for (int i = 0; i < riverCountMin; i++)
        {
            if (potentialStartTiles.Count > 0)
            {
                Vector3Int newStart = potentialStartTiles[random.Next(0, potentialStartTiles.Count)];
                riverStarts.Enqueue(newStart);

                foreach (Vector3Int neighbor in neighborsEightDirections) //don't want rivers starting up right next to other, 2 deep
                {
                    for (int j = 0; j < 2; j++)
                    {
                        potentialStartTiles.Remove(newStart + neighbor * (j + 1));
                    }
                }
            }
        }

        //generating rivers
        while (riverStarts.Count > 0 && potentialStartTiles.Count > 0)
        {
            Queue<Vector3Int> newRiver = new();
            Vector3Int newRiverStart = riverStarts.Dequeue();
            //Debug.Log("River start is " + newRiverStart);
            newRiver.Enqueue(newRiverStart);
            List<Vector3Int> newRiverTiles = new();
            int[] alternateDirections = DirectionSetUp(random.Next(0, 4));

            List<int> directionList = new();
            List<int> directionOne = new () { alternateDirections[0], alternateDirections[1] };
            List<int> directionTwo = new () { alternateDirections[0], alternateDirections[2] };
            int main = alternateDirections[0];
            int left = alternateDirections[1];
            int right = alternateDirections[2];
            int newDirection = main;
            int firstPrevDirection = main;
            int secondPrevDirection = main;


            while (newRiver.Count > 0)
            {
                Vector3Int position = newRiver.Dequeue();
                //Debug.Log("river goes " + position);
                //if (position == new Vector3Int(34, 3, 21))
                //    Debug.Log("found");

                newRiverTiles.Add(position);
                potentialStartTiles.Remove(position);
                int attempt = 0;
                int attemptMax = 1;
                bool success = false;

                foreach (Vector3Int tile in neighborsFourDirections) //if next to water, end river
                {
                    if (!newRiverTiles.Contains(position + tile) && 
                        (!mapDict.ContainsKey(position + tile) || mapDict[position + tile] == sea || mapDict[position + tile] == river))
                    {
                        success = true;
                    }
                }

                while (!success)
                {
                    attempt++;

                    if (attempt == 1)
                    {
                        if (newDirection == main) //setting up range of directions to randomly choose from 
                        {
                            if (newDirection != secondPrevDirection)
                            {
                                directionList = new();
                                directionList.AddRange(alternateDirections);
                                directionList.Remove(secondPrevDirection == left ? right : left);
                            }
                            else
                            {
                                directionList = new();
                                directionList.AddRange(alternateDirections);
                            }
                        }
                        else if (newDirection == 1)
                            directionList = new(directionOne); //can't go backwards
                        else
                            directionList = new(directionTwo); //can't go backwards

                        attemptMax = directionList.Count;
                    }

                    newDirection = directionList[random.Next(0, directionList.Count)];//directionList[random.Next(0, directionList.Count)];

                    Vector3Int neighborPos = position + neighborsFourDirections[newDirection];
                    directionList.Remove(newDirection); //try all directions before failing

                    if (!mapDict.ContainsKey(neighborPos))
                    {
                        success = true;
                    }
                    else if (mapDict[neighborPos] != mountain && mapDict[neighborPos] != hill && !newRiverTiles.Contains(neighborPos))
                    {
                        newRiver.Enqueue(neighborPos);
                        success = true;
                    }
                    else if (mapDict[neighborPos] == river || mapDict[neighborPos] == sea) //just as a precaution
                    {
                        success = true;
                    }
                    else //if river can't find ocean then don't count as river, start over
                    {
                        if (attempt >= attemptMax)
                        {
                            if (potentialStartTiles.Count > 0)
                            {
                                Vector3Int newStart = potentialStartTiles[random.Next(0, potentialStartTiles.Count)];
                                riverStarts.Enqueue(newStart);

                                foreach (Vector3Int neighbor in neighborsEightDirections) //don't want rivers starting up right next to other, 2 deep
                                {
                                    for (int j = 0; j < 2; j++)
                                    {
                                        potentialStartTiles.Remove(newStart + neighbor * (j + 1));
                                    }
                                }

                                newRiverTiles.Clear();
                            }
                            success = true;
                        }
                    }
                }

                secondPrevDirection = firstPrevDirection;
                firstPrevDirection = newDirection;
            }

            foreach (Vector3Int tile in newRiverTiles)
            {
                mapDict[tile] = river;
                riverTileCount++;
            }

            riverCount++;
            if (riverCount < riverCountMin && potentialStartTiles.Count > 0) //(riverTileCount < riverTileTotal)
            {
                Vector3Int newStart = potentialStartTiles[random.Next(0, potentialStartTiles.Count)];
                riverStarts.Enqueue(newStart);

                foreach (Vector3Int neighbor in neighborsEightDirections) //don't want rivers starting up right next to other, 2 deep
                {
                    for (int j = 0; j < 2; j++)
                    {
                        potentialStartTiles.Remove(newStart + neighbor * (j + 1));
                    }
                }
            }
        }

        return mapDict;
    }
}
