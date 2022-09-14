using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;

public class ProceduralGeneration
{
    public readonly static List<Vector3Int> neighborsFourDirections = new()
    {
        new Vector3Int(0,0,1), //up
        new Vector3Int(1,0,0), //right
        new Vector3Int(0,0,-1), //down
        new Vector3Int(-1,0,0), //left
    };

    public readonly static List<Vector3Int> neighborsEightDirections = new()
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

/*    Terrain Key:
    0 = placeholder for land
    1 = sea
    2 = hill
    3 = mountain
    4 = grassland
    5 = desert
    6 = forest
    7 = jungle
    8 = swamp
*/

    public static List<float> PerlinNoiseGenerator(int width, int height, float scale, int octaves, float persistance, 
        float lacunarity)
    {
        List<float> noiseList = new();
        
        if (scale <= 0)
            scale = 0.0001f;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                //int x = i;// - width * 1 / 2;
                //int z = j;// - height * 1 / 2;
                //Vector3Int position = new Vector3Int(x, yCoordinate, z);

                for (int k = 0; k < octaves; k++)
                {
                    float xCoord = i /*/ width**/ / (scale * frequency);
                    float yCoord = j /*/ height **/ / (scale * frequency);

                    float perlinValue = Mathf.PerlinNoise(xCoord, yCoord) * 2 - 1;

                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                noiseList.Add(noiseHeight);
            }
        }

        return noiseList;
    }

    public static Dictionary<Vector3Int, float> PerlinNoiseGenerator(List<Vector3Int> positions, 
        float scale, int octaves, float persistance, float lacunarity, int seed, float offset)
    {
        List<float> noiseList = new();
        Dictionary<Vector3Int, float> noiseMap = new();

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

        foreach (Vector3Int pos in positions)
        {
            float amplitude = 1;
            float frequency = 1;
            float noiseHeight = 0;

            for (int k = 0; k < octaves; k++)
            {
                float xCoord = (float)pos.x / (scale * frequency) + octaveOffsets[k].x;
                float zCoord = (float)pos.z / (scale * frequency) + octaveOffsets[k].y;

                float simplexValue = Unity.Mathematics.noise.snoise(new Unity.Mathematics.float4(xCoord, 3.0f, zCoord, 1.0f));
                //noiseHeight += simplexValue * amplitude;

                float simplex2Value = Unity.Mathematics.noise.psrnoise(new Unity.Mathematics.float2(xCoord, zCoord), new Unity.Mathematics.float2(xCoord, zCoord));
                //noiseHeight += simplex2Value * amplitude;

                Unity.Mathematics.float2 cellularValue = Unity.Mathematics.noise.cellular2x2(new Unity.Mathematics.float2(xCoord, zCoord));
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

        for (int i = 0; i < positions.Count; i++)
        {
            noiseMap[positions[i]] = noiseList[i];
        }

        return noiseMap;
    }

    public static Dictionary<Vector3Int, int> GenerateTerrain(Dictionary<Vector3Int, int> mainTiles, Dictionary<Vector3Int, float> mainNoise,
        float lowerThreshold, float upperThreshold, int width, int height, int yCoord, int equatorDist, int seed)
    {
        System.Random random = new System.Random(seed);

        if (lowerThreshold > upperThreshold)
            lowerThreshold = upperThreshold;
        
        //cleaning up diagonals first
        mainTiles = DiagonalCheck(mainTiles, width, height, yCoord);
        //getting sections of maps to assign terrain to
        Dictionary<int, List<Vector3Int>> terrainRegions = GetTerrainRegions(mainNoise, lowerThreshold, upperThreshold);

        int equatorPos = height / 2;

        foreach (int region in terrainRegions.Keys)
        {
            int[] terrainOptions = new int[] { 4, 5, 6 };
            int chosenTerrain = terrainOptions[random.Next(0, terrainOptions.Length)];
            if (region == 0)
                chosenTerrain = 4; //first region (borders of noise regions) is grassland
            
            foreach (Vector3Int tile in terrainRegions[region])
            {
                if (chosenTerrain == 6 && tile.z < equatorPos + equatorDist && tile.z > equatorPos - equatorDist)
                    chosenTerrain = 7;
                mainTiles[tile] = chosenTerrain;
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
                        if (tileHolder[pos + tile] == 1)
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
                    newTiles[tile] = 1;
                }
                else
                {
                    newTiles[tile] = 0;
                }
            }
        }

        newTiles = RemoveSingles(newTiles, width, height, yCoord);
        return newTiles;
    }

    private static Dictionary<Vector3Int, int> RandomFillMap(int width, int height, int randomFillPercent, int seed, int yCoord)
    {
        Dictionary<Vector3Int, int> randomTiles = new();

        System.Random random = new System.Random(seed.GetHashCode());

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                //positions.Add(new Vector3Int(i, 3, j));
                randomTiles[new Vector3Int(i, yCoord, j)] = random.Next(0, 100) < randomFillPercent ? 1 : 0;
                //randomTiles[new Vector3Int(i, 3, j)] = Random.Range(0, 2);

                //randList.Add(Random.Range(0, count));
            }
        }

        return randomTiles;
    }

    private static Dictionary<Vector3Int, int> RemoveSingles(Dictionary<Vector3Int, int> mapDict, int width, int height, int yCoord)
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

            Vector3Int currentTile = new Vector3Int(0, 0, 0);
            int firstPrevType = 0;
            int secondPrevType = 0;

            for (int i = 0; i < boundary1; i++)
            {
                for (int j = 0; j < boundary2; j++)
                {
                    if (k == 0)
                        currentTile = new Vector3Int(i, yCoord, j);
                    else
                        currentTile = new Vector3Int(j, yCoord, i);

                    int currentType = mapDict[currentTile];

                    if (currentType == 0 && firstPrevType == 1 && secondPrevType == 0)
                    {
                        mapDict[currentTile] = 1;
                    }

                    secondPrevType = firstPrevType;
                    firstPrevType = currentType;
                }
            }
        }

        return mapDict;
    }

    private static Dictionary<int, List<Vector3Int>> GetLandMasses(Dictionary<Vector3Int, int> mapDict)
    {
        Dictionary<int, List<Vector3Int>> landMassDict = new();
        List<Vector3Int> checkedPositions = new();
        Queue<Vector3Int> queue = new();
        int landMassCount = 0;

        foreach (Vector3Int pos in mapDict.Keys)
        {
            if (checkedPositions.Contains(pos) || mapDict[pos] == 1)
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

                        if (mapDict[neighborCheck] == 0)
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
            bool upper = mapDict[pos] >= upperThreshold;
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

                        if (upper)
                        {
                            if (mapDict[neighborCheck] >= upperThreshold)
                            {
                                landMassDict[landMassCount].Add(neighborCheck);
                                queue.Enqueue(neighborCheck);
                            }
                        }
                        else
                        {
                            if (mapDict[neighborCheck] <= lowerThreshold)
                            {
                                landMassDict[landMassCount].Add(neighborCheck);
                                queue.Enqueue(neighborCheck);
                            }
                        }
                    }
                }
            }

            landMassCount++;
        }

        return landMassDict;
    }

    public static Dictionary<Vector3Int, int> GenerateMountainRanges(Dictionary<Vector3Int, int> mainTiles, 
        int mountainPerc, int mountainousPerc, int mountainRangeLength, int seed) 
    {
        System.Random random = new System.Random(seed);

        Dictionary<int, List<Vector3Int>> landMassDict = GetLandMasses(mainTiles);
        Queue<Vector3Int> mountainStarts = new();
        List<Vector3Int> allTiles = new();
        bool justHills = false;

        foreach (int key in landMassDict.Keys)
        {
            mountainStarts.Enqueue(landMassDict[key][Random.Range(0, landMassDict[key].Count)]);
            allTiles.AddRange(landMassDict[key]);
        }

        int mountainCount = 0;
        int mountainTotal = Mathf.RoundToInt((mountainPerc / 100f) * allTiles.Count);

        while (mountainStarts.Count > 0)
        {
            Vector3Int startPosition = mountainStarts.Dequeue();
            List<Vector3Int> sideTileList = new();
            
            mainTiles[startPosition] = 2; //first position is always hill
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

                if (mainTiles[nextTile] == 0)
                {
                    if (!justHills)
                        mainTiles[nextTile] = random.Next(0, 100) > mountainousPerc ? 2 : 3; //2 is hill, 3 is mountain
                    else
                        mainTiles[nextTile] = 2;

                    mountainCount++;
                    allTiles.Remove(nextTile);
                    sideTileList.Remove(nextTile);

                    //adding tiles to the side tile list
                    if (tileShift.x == 0)
                    {
                        sideTileList.Add(nextTile + new Vector3Int(-1, 0, 0));
                        sideTileList.Add(nextTile + new Vector3Int(1, 0, 0));
                    }
                    else
                    {
                        sideTileList.Add(nextTile + new Vector3Int(0, 0, -1));
                        sideTileList.Add(nextTile + new Vector3Int(0, 0, 1));
                    }

                    if (random.Next(0, mountainRangeLength) > 0)
                        queue.Enqueue(nextTile);
                }
                else if (mainTiles[nextTile] == 3)
                {
                    mainTiles[prevTile] = 2; //turn back into hill if encountered mountain
                }
            }

            //adding hills to the side
            foreach (Vector3Int tile in sideTileList)
            {
                if (mainTiles.ContainsKey(tile) && mainTiles[tile] == 0)
                {
                    mainTiles[tile] = 2;
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
                    justHills = random.Next(0, 10) > 7;
                }
            }
        }

        return mainTiles;
    }

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
                    Vector3Int tileA = new Vector3Int(i, yCoord, j);
                    Vector3Int tileB = new Vector3Int(i, yCoord, j + 1);
                    Vector3Int tileC = new Vector3Int(i + 1, yCoord, j + 1);
                    Vector3Int tileD = new Vector3Int(i + 1, yCoord, j);

                    if (mapDict[tileA] == 1 && mapDict[tileC] == 1 && mapDict[tileB] != 1 && mapDict[tileD] != 1)
                        mapDict[tileB] = 1;

                    if (mapDict[tileA] != 1 && mapDict[tileC] != 1 && mapDict[tileB] == 1 && mapDict[tileD] == 1)
                        mapDict[tileA] = 1;

                    if (mapDict[tileA] == 3 && mapDict[tileC] == 3 && mapDict[tileB] != 3 && mapDict[tileD] != 3)
                        mapDict[tileA] = 2;

                    if (mapDict[tileA] != 3 && mapDict[tileC] != 3 && mapDict[tileB] == 3 && mapDict[tileD] == 3)
                        mapDict[tileB] = 2;
                }
            }
        }

        return mapDict;
    }
}
