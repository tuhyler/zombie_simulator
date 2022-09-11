using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public static HashSet<float> PerlinNoiseGenerator(int width, int height, float scale, int octaves, float persistance, 
        float lacunarity)
    {
        HashSet<float> noiseList = new();
        
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
        float lacunarity)
    {
        List<float> noiseList = new();

        if (scale <= 0)
            scale = 0.0001f;

        foreach (Vector3Int pos in positions)
        {
            float amplitude = 1;
            float frequency = 1;
            float noiseHeight = 0;

            for (int k = 0; k < octaves; k++)
            {
                float xCoord = (float)pos.x /*/ width**/ / (scale * frequency);
                float yCoord = (float)pos.z /*/ height **/ / (scale * frequency);

                float perlinValue = Mathf.PerlinNoise(xCoord, yCoord) * 2 - 1;

                noiseHeight += perlinValue * amplitude;

                amplitude *= persistance;
                frequency *= lacunarity;
            }

            noiseList.Add(noiseHeight);
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

    public static int GetRegions(Dictionary<Vector3Int, int> mapDict)
    {
        
        
        return 0;
    }
}
