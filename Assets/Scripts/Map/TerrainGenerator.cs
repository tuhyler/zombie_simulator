using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TerrainGenerator : TerrainGeneratorAbstract
{
    [Header("World Generation Info")]
    //[SerializeField]
    //protected Vector3Int startPosition = Vector3Int.zero;
    [SerializeField]
    private int width;
    [SerializeField]
    private int height;
    [SerializeField]
    private float scale = 1;
    [SerializeField]
    private int octaves;
    [Range(0f, 1f)]
    [SerializeField]
    private float persistance;
    [SerializeField]
    private float lacunarity;
    [SerializeField]
    private float grasslandThreshold = 0;

    [Header("Random Walk")]
    [SerializeField]
    private int iterations;
    [SerializeField]
    private int walkLength;
    [SerializeField]
    private bool iterationStartRandom = true;

    [Header("Tiles")]
    [SerializeField]
    private GameObject grassland; //separate here so that header isn't on every line
    [SerializeField]
    private GameObject desert, forest, jungle, swamp, grasslandHill, desertHill, grasslandMountain, desertMountain, 
        riverGrasslandEnd, riverGrasslandStraight, riverGrasslandCurve, riverDesertEnd, riverDesertStraight, riverDesertCurve,
        oceanGrasslandCoast, oceanGrasslandCorner, oceanGrasslandRiver, oceanDesertCoast, oceanDesertCorner, oceanDesertRiver;

    [Header("Parents for Tiles")]
    [SerializeField]
    private Transform groundTiles;

    private List<GameObject> allTiles = new();


    protected override void RunProceduralGeneration()
    {
        RemoveAllTiles();

        if (scale <= 0)
            scale = 0.0001f;

        GameObject tile = null;

        for(int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                int x = i;// - width * 1 / 2;
                int y = j;// - height * 1 / 2;
                Vector3Int position = new Vector3Int(x, 3, y);

                for (int k = 0; k < octaves; k++)
                {                    
                    float xCoord = (float)i /*/ width**/ / (scale * frequency);
                    float yCoord = (float)j /*/ height **/ / (scale * frequency);

                    float perlinValue = Mathf.PerlinNoise(xCoord, yCoord) * 2 - 1;

                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                //grasslandThreshold - ((grasslandThreshold - 1) / 2)
                //grasslandThreshold - ((grasslandThreshold + 1) / 2)

                if (noiseHeight > .75f)
                {
                    tile = grasslandMountain;
                }
                else if (noiseHeight > .5f)
                {
                    tile = grasslandHill;
                }
                else if (noiseHeight > .25f)
                {
                    tile = forest;
                }
                else if (noiseHeight >= -.1)
                {
                    tile = grassland;
                }
                else if (noiseHeight > -.25f)
                {
                    tile = swamp;
                }
                else if (noiseHeight > -.5f)
                {
                    tile = desert;
                }
                else if (noiseHeight > -.75f)
                {
                    tile = desertHill;
                }
                else if (noiseHeight > -1f)
                {
                    tile = desertMountain;
                }

                GenerateTile(noiseHeight, tile, position);
            }
        }

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
         
            }
        }

                //HashSet<Vector3Int> groundPositions = RunRandomWalk();

                //foreach (Vector3Int position in groundPositions)
                //{
                //    GameObject newTile = Instantiate(grassland, position, Quaternion.identity);
                //    newTile.transform.SetParent(groundTiles.transform, false);
                //    allTiles.Add(newTile);
                //}
            }

    private void GenerateTile(float perlinCoord, GameObject tile, Vector3Int position)
    {
        GameObject newTile = Instantiate(tile, position, Quaternion.identity);
        newTile.transform.SetParent(groundTiles.transform, false);
        allTiles.Add(newTile);

        //temporary, just to see the values
        //newTile.GetComponent<TerrainData>().numbers.text = Math.Round(perlinCoord, 3).ToString();
    }

    protected override void RemoveAllTiles()
    {
        foreach (GameObject tile in allTiles)
        {
            DestroyImmediate(tile);
        }
    }

    private HashSet<Vector3Int> RunRandomWalk()
    {
        var currentPosition = startPosition;
        HashSet<Vector3Int> groundPositions = new();
        for (int i = 0; i < iterations; i++)
        {
            var path = ProceduralGeneration.SimpleRandomWalk(currentPosition, walkLength);
            groundPositions.UnionWith(path);
            if (iterationStartRandom)
            {
                currentPosition = groundPositions.ElementAt(UnityEngine.Random.Range(0, groundPositions.Count));
            }
        }

        return groundPositions;
    }
}
