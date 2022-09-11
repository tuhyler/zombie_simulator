using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    [SerializeField]
    private int seed = 4;
    [SerializeField]
    private int yCoord = 3;

    [Header("Perlin Noise Generation Info")]
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

    [Header("Cellular Automata Generation Info")]
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
    private GameObject desert, forest, jungle, swamp, grasslandHill, desertHill, grasslandMountain, desertMountain,
        riverGrasslandEnd, riverGrasslandStraight, riverGrasslandCurve, riverDesertEnd, riverDesertStraight, riverDesertCurve,
        oceanGrasslandCoast, oceanGrasslandCorner, oceanGrasslandRiver, oceanDesertCoast, oceanDesertCorner, oceanDesertRiver,
        ocean;

    [Header("Parents for Tiles")]
    [SerializeField]
    private Transform groundTiles;

    private List<GameObject> allTiles = new();

    [SerializeField]
    protected Vector3Int startPosition = Vector3Int.zero;

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

        GameObject tile = null;

        //HashSet<float> noise = ProceduralGeneration.PerlinNoiseGenerator(width, height, scale, octaves, persistance, lacunarity);
        Dictionary<Vector3Int, int> randNumbers = 
            ProceduralGeneration.GenerateCellularAutomata(threshold, width, height, iterations, randomFillPercent, seed, yCoord);
        

        List<Vector3Int> landPositions = new();

        foreach (Vector3Int position in randNumbers.Keys)
        {
            if (randNumbers[position] == 1)
            {
                tile = ocean;
                GenerateTile(tile, position);
            }
            else
            {
                //tile = grassland;
                landPositions.Add(position);
            }
        }

        List<float> noise = ProceduralGeneration.PerlinNoiseGenerator(landPositions, scale, octaves, persistance, lacunarity);

        for (int i = 0; i < landPositions.Count; i++)
        {

            if (noise.ElementAt(i) > .75f)
            {
                tile = grasslandMountain;
            }
            else if (noise.ElementAt(i) > .5f)
            {
                tile = grasslandHill;
            }
            else if (noise.ElementAt(i) > .25f)
            {
                tile = forest;
            }
            else if (noise.ElementAt(i) >= -.2)
            {
                tile = grassland;
            }
            else if (noise.ElementAt(i) > -.25f)
            {
                tile = swamp;
            }
            else if (noise.ElementAt(i) > -.5f)
            {
                tile = desert;
            }
            else if (noise.ElementAt(i) > -.75f)
            {
                tile = desertHill;
            }
            else if (noise.ElementAt(i) > -1f)
            {
                tile = desertMountain;
            }

            GenerateTile(tile, landPositions[i]);
        }

        //        for (int i = 0; i < width; i++)
        //        {
        //            for (int j = 0; j < height; j++)
        //            {
        //                int x = i;// - width * 1 / 2;
        //                int y = j;// - height * 1 / 2;
        //                Vector3Int position = new Vector3Int(x, 3, y);

        ///*                if (noise.ElementAt(k) > .75f)
        //                {
        //                    tile = grasslandMountain;
        //                }
        //                else if (noise.ElementAt(k) > .5f)
        //                {
        //                    tile = grasslandHill;
        //                }
        //                else if (noise.ElementAt(k) > .25f)
        //                {
        //                    tile = forest;
        //                }
        //                else if (noise.ElementAt(k) >= -.2)
        //                {
        //                    tile = grassland;
        //                }
        //                else if (noise.ElementAt(k) > -.25f)
        //                {
        //                    tile = swamp;
        //                }
        //                else if (noise.ElementAt(k) > -.5f)
        //                {
        //                    tile = desert;
        //                }
        //                else if (noise.ElementAt(k) > -.75f)
        //                {
        //                    tile = desertHill;
        //                }
        //                else if (noise.ElementAt(k) > -1f)
        //                {
        //                    tile = desertMountain;
        //                }
        //*/
            //                //GenerateTile(tile, position);
            //                //k++;
            //            }
            //        }



            //HashSet<Vector3Int> groundPositions = RunRandomWalk();

            //foreach (Vector3Int position in groundPositions)
            //{
            //    GameObject newTile = Instantiate(grassland, position, Quaternion.identity);
            //    newTile.transform.SetParent(groundTiles.transform, false);
            //    allTiles.Add(newTile);
            //}
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

    //private HashSet<Vector3Int> RunRandomWalk()
    //{
    //    var currentPosition = startPosition;
    //    HashSet<Vector3Int> groundPositions = new();
    //    for (int i = 0; i < iterationsRW; i++)
    //    {
    //        var path = ProceduralGeneration.SimpleRandomWalk(currentPosition, walkLength);
    //        groundPositions.UnionWith(path);
    //        if (iterationStartRandom)
    //        {
    //            currentPosition = groundPositions.ElementAt(UnityEngine.Random.Range(0, groundPositions.Count));
    //        }
    //    }

    //    return groundPositions;
    //}
}
