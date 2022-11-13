using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TerrainData : MonoBehaviour
{
    [SerializeField]
    public TerrainDataSO terrainData;

    [SerializeField]
    public Transform prop;

    [SerializeField]
    private GameObject highlightPlane;

    private SelectionHighlight highlight;

    private Vector3Int tileCoordinates;

    //private int originalMovementCost;
    //public int OriginalMovementCost { get { return originalMovementCost; } }

    private int movementCost; 
    public int MovementCost { get { return movementCost; } set { movementCost = value; } }

    [HideInInspector]
    public bool hasRoad;
    
    private bool isCoast = false;

    private void Awake()
    {
        terrainData.MovementCostCheck();
        ResetMovementCost();
        highlight = GetComponent<SelectionHighlight>();
        if (highlightPlane != null)
        {
            highlightPlane.SetActive(false);
            isCoast = true;
        }
    }

    public Vector3Int GetTileCoordinates()
    {
        tileCoordinates = Vector3Int.RoundToInt(transform.position);
        return tileCoordinates;
    }

    public TerrainDataSO GetTerrainData() => terrainData;

    public void EnableHighlight(Color highlightColor)
    {
        //highlight.ToggleGlow(true, highlightColor);
        if (isCoast)
            ToggleHighlightPlane(true);

        highlight.EnableHighlight(highlightColor);
    }

    public void DisableHighlight()
    {
        //highlight.ToggleGlow(false, Color.white);
        if (isCoast)
            ToggleHighlightPlane(false);

        highlight.DisableHighlight();
    }

    public void ResetMovementCost()
    {
        movementCost = GetTerrainData().movementCost;
        //originalMovementCost = movementCost;
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
    
    private void ToggleHighlightPlane(bool v)
    {
        if (highlightPlane != null)
            highlightPlane.SetActive(v);
    }

}
