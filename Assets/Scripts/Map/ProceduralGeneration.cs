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

    //public static HashSet<Vector3Int> SimpleRandomWalk(Vector3Int startPosition, int walkLength)
    //{
    //    HashSet<Vector3Int> path = new HashSet<Vector3Int>();

    //    path.Add(startPosition);
    //    var previousPosition = startPosition;

    //    for (int i = 0; i < walkLength; i++)
    //    {
    //        while (path.Count == i+1)
    //        {
    //            var newPosition = previousPosition + neighborsFourDirections[Random.Range(0, 4)]; //random direction

    //            path.Add(newPosition);
    //            previousPosition = newPosition;
    //        }
    //    }

    //    return path;
    //}

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

    public static List<float> PerlinNoiseGenerator(List<Vector3Int> positions, float scale, int octaves, float persistance,
        float lacunarity, int seed, float offset)
    {
        List<float> noiseList = new();

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
                float yCoord = (float)pos.z / (scale * frequency) + octaveOffsets[k].y;

                float simplexValue = Unity.Mathematics.noise.snoise(new Unity.Mathematics.float4(xCoord, 3.0f, yCoord, 1.0f));
                //noiseHeight += simplexValue * amplitude;

                float simplex2Value = Unity.Mathematics.noise.psrnoise(new Unity.Mathematics.float2(xCoord,yCoord), new Unity.Mathematics.float2(xCoord,yCoord));
                //noiseHeight += simplex2Value * amplitude;

                float perlinValue = Mathf.PerlinNoise(xCoord, yCoord) * 2 - 1;
                //noiseHeight += perlinValue * amplitude;

                float avgNoise = (simplexValue + simplexValue + perlinValue) / 3;
                noiseHeight += avgNoise * amplitude;

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

        return noiseList;
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

    public static Dictionary<Vector3Int, int> GenerateMountainRanges(Dictionary<Vector3Int, int> mainTiles, 
        int width, int height, int mountainousPerc, int mountainRangeLength, int seed) 
    {
        System.Random random = new System.Random(seed.GetHashCode());

        Dictionary<int, List<Vector3Int>> landMassDict = GetLandMasses(mainTiles);
        int mapSize = width * height;
        List<Vector3Int> mountainStarts = new();
        
        foreach(int key in landMassDict.Keys)
        {
            if (landMassDict[key].Count < mapSize / 10)
            {
                mountainStarts.Add(landMassDict[key][Random.Range(0, landMassDict[key].Count)]);
                continue;
            }

            int mountainRangeCount = landMassDict[key].Count / 250;

            for (int i = 0; i < mountainRangeCount; i++)
            {
                mountainStarts.Add(landMassDict[key][Random.Range(0, landMassDict[key].Count)]);
            }
        }

        foreach (Vector3Int pos in mountainStarts)
        {
            Queue<Vector3Int> queue = new();
            queue.Enqueue(pos);
            
            mainTiles[pos] = 2;
            int step = 0;

            int mainDirection = random.Next(0, 8);
            int[] diagonals = new int[3];
            int[] perpendiculars = new int[2];

            diagonals[0] = mainDirection;
            if (mainDirection == 0)
                perpendiculars[0] = 6;
            else if (mainDirection == 1)
                perpendiculars[0] = 7;
            else
            perpendiculars[1] = mainDirection - 2;

            if (mainDirection == 6)
                perpendiculars[0] = 0;
            else if (mainDirection == 7)
                perpendiculars[0] = 1;
            else
                perpendiculars[0] = mainDirection + 2;

            if (mainDirection == 0)
                diagonals[1] = 7;
            else
                diagonals[1] = mainDirection - 1;

            if (mainDirection == 7)
                diagonals[2] = 0;
            else
                diagonals[2] = mainDirection + 1;


            while (queue.Count > 0)
            {
                Vector3Int nextDirection = queue.Dequeue() + neighborsEightDirections[diagonals[random.Next(0,3)]];

                if (mainTiles[nextDirection] == 0)
                {
                    int newTile = random.Next(0, 100) < mountainousPerc ? 2 : 3; //2 is hill, 3 is mountain
                    mainTiles[nextDirection] = newTile;

                    //foreach (int side in perpendiculars)
                    //{

                    //}

                    if (random.Next(0, mountainRangeLength) > 0)
                        queue.Enqueue(nextDirection);

                    step++;
                }

                
            }


        }

        return mainTiles;
    }

    //private int[] DirectionRoll()
    //{
    //    return int[]
    //}
}
