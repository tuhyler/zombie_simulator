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
    private int yCoord = 3, desertPerc = 20, forestPerc = 20, junglePerc = 20, mountainPerc = 10, mountainousPerc = 80, 
        mountainRangeLength = 20, equatorDist = 10;

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
    private GameObject desert, forest, jungle, swamp, grasslandHill, desertHill, grasslandMountain, desertMountain, forestHill, jungleHill,
        riverGrasslandEnd, riverGrasslandStraight, riverGrasslandCurve, riverDesertEnd, riverDesertStraight, riverDesertCurve,
        oceanGrasslandCoast, oceanGrasslandCorner, oceanGrasslandRiver, oceanDesertCoast, oceanDesertCorner, oceanDesertRiver,
        ocean;

    [Header("Parents for Tiles")]
    [SerializeField]
    private Transform groundTiles;

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

        //GameObject tile = null;

        //HashSet<float> noise = ProceduralGeneration.PerlinNoiseGenerator(width, height, scale, octaves, persistance, lacunarity);
        Dictionary<Vector3Int, int> randNumbers = 
            ProceduralGeneration.GenerateCellularAutomata(threshold, width, height, iterations, randomFillPercent, seed, yCoord);

        Dictionary<Vector3Int, float> noise = ProceduralGeneration.PerlinNoiseGenerator(randNumbers,
            scale, octaves, persistance, lacunarity, seed, offset);

        randNumbers = ProceduralGeneration.GenerateTerrain(randNumbers, noise, 0.45f, 0.65f, desertPerc, forestPerc,
            width, height, yCoord, equatorDist, seed);
        randNumbers = ProceduralGeneration.GenerateMountainRanges(randNumbers, mountainPerc, mountainousPerc, mountainRangeLength, seed);

        //List<Vector3Int> landPositions = new();

        foreach (Vector3Int position in randNumbers.Keys)
        {            
            if (randNumbers[position] == ProceduralGeneration.sea)
            {
                GenerateTile(ocean, position);
            }
            else if (randNumbers[position] == ProceduralGeneration.grasslandHill)
            {
                GenerateTile(grasslandHill, position);
            }
            else if (randNumbers[position] == ProceduralGeneration.desertHill)
            {
                GenerateTile(desertHill, position);
            }
            else if (randNumbers[position] == ProceduralGeneration.forestHill)
            {
                GenerateTile(forestHill, position);
            }
            else if (randNumbers[position] == ProceduralGeneration.jungleHill)
            {
                GenerateTile(jungleHill, position);
            }
            else if (randNumbers[position] == ProceduralGeneration.grasslandMountain)
            {
                GenerateTile(grasslandMountain, position);
            }
            else if (randNumbers[position] == ProceduralGeneration.desertMountain)
            {
                GenerateTile(desertMountain, position);
            }
            else if (randNumbers[position] == ProceduralGeneration.grassland)
            {
                GenerateTile(grassland, position);
            }
            else if (randNumbers[position] == ProceduralGeneration.desert)
            {
                GenerateTile(desert, position);
            }
            else if (randNumbers[position] == ProceduralGeneration.forest)
            {
                GenerateTile(forest, position);
            }
            else if (randNumbers[position] == ProceduralGeneration.jungle)
            {
                GenerateTile(jungle, position);
            }
            else if (randNumbers[position] == ProceduralGeneration.swamp)
            {
                GenerateTile(swamp, position);
            }
            else
            {
                GenerateTile(grassland, position);
            }
        }
    }



    private void GenerateTile(/*float perlinCoord, */GameObject tile, Vector3Int position)
    {
        GameObject newTile = Instantiate(tile, position, Quaternion.identity);
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
//    public int desertHill;
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
