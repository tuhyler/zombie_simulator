using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TerrainGeneratorAbstract : MonoBehaviour
{
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

    protected abstract void RunProceduralGeneration();

    protected abstract void RemoveAllTiles();
}
