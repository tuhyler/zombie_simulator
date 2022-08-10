using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TerrainData : MonoBehaviour
{
    [SerializeField]
    private TerrainDataSO terrainData;

    private SelectionHighlight highlight;

    private Vector3Int tileCoordinates;

    private int originalMovementCost;
    public int OriginalMovementCost { get { return originalMovementCost; } }

    private int movementCost; 
    public int MovementCost { get { return movementCost; } set { movementCost = value; } }

    [HideInInspector]
    public bool hasRoad;

    private void Awake()
    {
        terrainData.MovementCostCheck();
        ResetMovementCost();
        //turnNumberHolder.GetComponent<MeshRenderer>().enabled = false;
        highlight = GetComponent<SelectionHighlight>(); //Instantiated here, rather than making an empty in Unity and dragging it over.
    }

    public Vector3Int GetTileCoordinates()
    {
        tileCoordinates = Vector3Int.FloorToInt(transform.position);
        return tileCoordinates;
    }

    public TerrainDataSO GetTerrainData() => terrainData;

    public void EnableHighlight(Color highlightColor)
    {
        //highlight.ToggleGlow(true, highlightColor);
        highlight.EnableHighlight(highlightColor);
    }

    public void DisableHighlight()
    {
        //highlight.ToggleGlow(false, Color.white);
        highlight.DisableHighlight();
    }

    public void ResetMovementCost()
    {
        movementCost = GetTerrainData().movementCost;
        originalMovementCost = movementCost;
    }

    public void AddTerrainToWorld(MapWorld world)
    {
        world.SetTerrainData(GetTileCoordinates(), this);
    }

    public void DestroyTile(MapWorld world)
    {
        world.RemoveTerrain(GetTileCoordinates());
        Destroy(gameObject);
    }
    
}
