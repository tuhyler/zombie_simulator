using System.Collections.Generic;
using UnityEngine;

public class TerrainData : MonoBehaviour
{
    [SerializeField]
    public TerrainDataSO terrainData;

    [SerializeField]
    public Transform main, prop;

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

    private bool isLand = true;
    private bool isCoast = false;
    public bool IsCoast { get { return isCoast; } }
    private bool isSeaCorner = false;
    public bool IsSeaCorner { get { return isSeaCorner; } }
    private bool isGlowing = false;

    private List<MeshRenderer> renderers = new();
    private Vector2[] uvs;
    public Vector2[] UVs { get { return uvs; } }

    private void Awake()
    {
        if (terrainData.type == TerrainType.Flatland)
            uvs = main.GetComponentInChildren<MeshFilter>().mesh.uv;

        PrepareRenderers();
        
        isLand = terrainData.isLand;
        isSeaCorner = terrainData.isSeaCorner;
        terrainData.MovementCostCheck();
        ResetMovementCost();
        highlight = GetComponent<SelectionHighlight>();
        if (highlightPlane != null)
        {
            highlightPlane.SetActive(false);
            isCoast = true;
        }
    }

    private void PrepareRenderers()
    {
        foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>())
        {
            renderers.Add(renderer);
        }
    }


    public void SetCoastCoordinates(MapWorld world)
    {
        List<Vector3Int> allTileLocs = world.GetNeighborsFor(tileCoordinates, MapWorld.State.EIGHTWAY);

        foreach (Vector3Int tile in allTileLocs)
        {
            if (tile == tileCoordinates)
                continue;

            foreach (Vector3Int neighbor in world.GetNeighborsFor(tile, MapWorld.State.EIGHTWAY))
            {
                if (allTileLocs.Contains(neighbor) || tile == tileCoordinates)
                    continue;

                TerrainData tileCheck = world.GetTerrainDataAt(world.GetClosestTerrainLoc(neighbor));

                if (tileCheck.isLand)
                {
                    world.AddToCoastList(tile);
                    break;
                }
            }
        }
    }

    public void SetTileCoordinates(MapWorld world)
    {
        tileCoordinates = world.RoundToInt(transform.position);
    }

    public Vector3Int GetTileCoordinates()
    {
        //tileCoordinates = Vector3Int.RoundToInt(transform.position);
        return tileCoordinates;
    }

    public TerrainDataSO GetTerrainData() => terrainData;

    public void SetNewRenderer(MeshRenderer[] oldRenderer, MeshRenderer[] newRenderer)
    {
        highlight.SetNewRenderer(oldRenderer, newRenderer);
    }

    public void EnableHighlight(Color highlightColor)
    {
        if (isGlowing)
            return;

        isGlowing = true;

        if (isCoast)
            ToggleHighlightPlane(true);

        highlight.EnableHighlight(highlightColor);
    }

    public void DisableHighlight()
    {
        if (!isGlowing) 
            return;
        
        isGlowing = false;
        
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
