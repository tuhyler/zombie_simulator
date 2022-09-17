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
    private int yCoord = 3, desertPerc = 30, forestAndJunglePerc = 70, mountainPerc = 10, mountainousPerc = 80, 
        mountainRangeLength = 20, equatorDist = 10, riverPerc = 5, riverCountMin = 10;

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

        //List<Vector3Int> landPositions = new();

        foreach (Vector3Int position in mainMap.Keys)
        {            
            if (mainMap[position] == ProceduralGeneration.sea)
            {
                GenerateTile(ocean, position);
            }
            else if (mainMap[position] == ProceduralGeneration.grasslandHill)
            {
                GenerateTile(grasslandHill, position);
            }
            else if (mainMap[position] == ProceduralGeneration.desertHill)
            {
                GenerateTile(desertHill, position);
            }
            else if (mainMap[position] == ProceduralGeneration.forestHill)
            {
                GenerateTile(forestHill, position);
            }
            else if (mainMap[position] == ProceduralGeneration.jungleHill)
            {
                GenerateTile(jungleHill, position);
            }
            else if (mainMap[position] == ProceduralGeneration.grasslandMountain)
            {
                GenerateTile(grasslandMountain, position);
            }
            else if (mainMap[position] == ProceduralGeneration.desertMountain)
            {
                GenerateTile(desertMountain, position);
            }
            else if (mainMap[position] == ProceduralGeneration.grassland)
            {
                GenerateTile(grassland, position);
            }
            else if (mainMap[position] == ProceduralGeneration.desert)
            {
                GenerateTile(desert, position);
            }
            else if (mainMap[position] == ProceduralGeneration.forest)
            {
                GenerateTile(forest, position);
            }
            else if (mainMap[position] == ProceduralGeneration.jungle)
            {
                GenerateTile(jungle, position);
            }
            else if (mainMap[position] == ProceduralGeneration.swamp)
            {
                GenerateTile(swamp, position);
            }
            else if (mainMap[position] == ProceduralGeneration.grasslandRiver)
            {
                GenerateTile(riverGrasslandStraight, position);
            }
            else if (mainMap[position] == ProceduralGeneration.desertRiver)
            {
                GenerateTile(riverDesertStraight, position);
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
