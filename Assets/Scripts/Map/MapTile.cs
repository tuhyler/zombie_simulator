using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapTile : MonoBehaviour
{

    private Vector3Int tileCoordinates;

    private TerrainData terrainData;

    public Vector3Int GetTileCoordinates => Vector3Int.FloorToInt(transform.position);

    private void Awake()
    {
        terrainData = GetComponent<TerrainData>();
    }



}
