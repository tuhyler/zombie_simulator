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
    private GameObject desert, forest, jungle, swamp, grasslandHill, desertHill, grasslandMountain, desertMountain,
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

        GameObject tile = null;

        //HashSet<float> noise = ProceduralGeneration.PerlinNoiseGenerator(width, height, scale, octaves, persistance, lacunarity);
        Dictionary<Vector3Int, int> randNumbers = 
            ProceduralGeneration.GenerateCellularAutomata(threshold, width, height, iterations, randomFillPercent, seed, yCoord);
        //randNumbers = ProceduralGeneration.GenerateMountainRanges(randNumbers, mountainPerc, mountainousPerc, mountainRangeLength, seed);
        //randNumbers = ProceduralGeneration.GenerateTerrain(randNumbers, width, height, yCoord, equatorDist, seed);
        

        List<Vector3Int> landPositions = new();

        foreach (Vector3Int position in randNumbers.Keys)
        {
            if (randNumbers[position] == 1)
            {
                GenerateTile(ocean, position);
            }
            else if (randNumbers[position] == 2)
            {
                GenerateTile(grasslandHill, position);
            }
            else if (randNumbers[position] == 3)
            {
                GenerateTile(grasslandMountain, position);
            }
            else if (randNumbers[position] == 4)
            {
                GenerateTile(grassland, position);
            }
            else if (randNumbers[position] == 5)
            {
                GenerateTile(desert, position);
            }
            else if (randNumbers[position] == 6)
            {
                GenerateTile(forest, position);
            }
            else if (randNumbers[position] == 7)
            {
                GenerateTile(jungle, position);
            }
            else if (randNumbers[position] == 8)
            {
                GenerateTile(swamp, position);
            }
            else
            {
                //tile = grassland;
                landPositions.Add(position);
            }
        }

        Dictionary<Vector3Int, float> noise = ProceduralGeneration.PerlinNoiseGenerator(landPositions, 
            scale, octaves, persistance, lacunarity, seed, offset);

        randNumbers = ProceduralGeneration.GenerateTerrain(randNumbers, noise, 0.45f, 0.65f, width, height, yCoord, equatorDist, seed);

        for (int i = 0; i < landPositions.Count; i++)
        {

            //if (noise.ElementAt(i) > .875f)
            //{
            //    tile = grasslandMountain;
            //}
            //else if (noise.ElementAt(i) > 75f)
            //{
            //    tile = grasslandHill;
            //}
            //List<float> noiseSorted = new(noise);

            //noiseSorted.Sort();

            //int noiseCount = Mathf.RoundToInt(noiseSorted.Count);

            //float grasslandPerc = 100 - (desertPerc + junglePerc + forestPerc);
            //float groupOneValue = noiseSorted[Mathf.RoundToInt(noiseCount * (desertPerc / 100f))];
            //float groupTwoValue = noiseSorted[Mathf.RoundToInt(noiseCount * ((desertPerc + grasslandPerc) / 100f))];
            //float groupThreeValue = noiseSorted[Mathf.RoundToInt(noiseCount * ((desertPerc + grasslandPerc + junglePerc) / 100))];
            //float groupFourValue = noiseSorted[noiseCount * 4];
            
            //if (noise[i] >.45 && noise[i] < .65)
            //{
            //    tile = desert;
            //}
            //else if (noise[i] >= .65)
            //{
            //    tile = grassland;
            //}
            //else
            //{
            //    tile = forest;
            //}

            //if (noise.ElementAt(i) > groupThreeValue)
            //{
            //    tile = forest;
            //}
            //else if (noise.ElementAt(i) > groupTwoValue)
            //{
            //    tile = jungle;
            //}
            //else if (noise.ElementAt(i) > groupOneValue)
            //{
            //    tile = grassland;
            //}
            ////else if (noise.ElementAt(i) > groupOneValue)
            ////{
            ////    tile = swamp;
            ////}
            //else if (noise.ElementAt(i) > 0f)
            //{
            //    tile = desert;
            //}

            //tile = grassland;
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
