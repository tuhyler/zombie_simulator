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
    public Vector3Int TileCoordinates { get { return tileCoordinates; } }

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
    [HideInInspector]
    public bool isGlowing = false;

    private ResourceGraphicHandler resourceGraphic;

    //private List<MeshRenderer> renderers = new();

    [SerializeField]
    private MeshFilter terrainMesh;
    private Vector2[] uvs;
    public Vector2[] UVs { get { return uvs; } }
    private Vector2 rockUVs;
    public Vector2 RockUVs { get { return rockUVs; } }

    public int resourceAmount;

    private void Awake()
    {
        if (terrainData.type == TerrainType.Flatland || terrainData.type == TerrainType.Hill || terrainData.type == TerrainType.ForestHill || terrainData.type == TerrainType.Forest || terrainData.type == TerrainType.River)
            uvs = main.GetComponentInChildren<MeshFilter>().mesh.uv;
        if (terrainData.rawResourceType == RawResourceType.Stone)
        {
            rockUVs = prop.GetComponentInChildren<MeshFilter>().mesh.uv[0];
            resourceGraphic = prop.GetComponentInChildren<ResourceGraphicHandler>();
            resourceGraphic.isHill = terrainData.type == TerrainType.Hill;

            RocksCheck();
        }

        //PrepareRenderers();
        
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

    //private void PrepareRenderers()
    //{
    //    foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>())
    //    {
    //        renderers.Add(renderer);
    //    }
    //}

    public void SetUVs(Vector2[] uvs, int rotation)
    {
        this.uvs = uvs;
        terrainMesh.mesh.uv = uvs;
        main.localEulerAngles += new Vector3(0, rotation * 90, 0);
    }

    public void SetRockUVs(Vector2 uv)
    {
        MeshFilter mesh = prop.GetComponentInChildren<MeshFilter>();
        Vector2[] uvs = mesh.mesh.uv;
        int i = 0;
        
        while (i < uvs.Length)
        {
            uvs[i] = uv;
            i++;
        }

        mesh.mesh.uv = uvs;
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

    public void SetNewRenderer(MeshRenderer[] oldRenderer, MeshRenderer[] newRenderer)
    {
        highlight.SetNewRenderer(oldRenderer, newRenderer);
    }

    public void EnableHighlight(Color highlightColor)
    {
        if (isGlowing)
            return;

        isGlowing = true;

        if (!isLand)
            ToggleHighlightPlane(true);

        if (highlight != null)
            highlight.EnableHighlight(highlightColor);
    }

    public void DisableHighlight()
    {
        if (!isGlowing) 
            return;
        
        isGlowing = false;
        
        if (!isLand)
            ToggleHighlightPlane(false);

        if (highlight != null)
            highlight.DisableHighlight();
    }

    public void ResetMovementCost()
    {
        movementCost = terrainData.movementCost;
        //originalMovementCost = movementCost;
    }

    public void AddTerrainToWorld(MapWorld world)
    {
        world.SetTerrainData(tileCoordinates, this);
    }

    public void DestroyTile(MapWorld world)
    {
        world.RemoveTerrain(tileCoordinates);
        Destroy(gameObject);
    }
    
    private void ToggleHighlightPlane(bool v)
    {
        if (highlightPlane != null)
            highlightPlane.SetActive(v);
    }

    public void RocksCheck()
    {
        resourceGraphic.TurnOffGraphics();

        if (resourceGraphic.isHill)
        {
            if (resourceAmount > 100)
                resourceGraphic.resourceLargeHill.SetActive(true);
            else if (resourceAmount > 50)
                resourceGraphic.resourceMediumHill.SetActive(true);
            else if (resourceAmount > 0)
                resourceGraphic.resourceSmallHill.SetActive(true);
        }
        else
        {
            if (resourceAmount > 100)
                resourceGraphic.resourceLargeFlat.SetActive(true);
            else if (resourceAmount > 50)
                resourceGraphic.resourceMediumFlat.SetActive(true);
            else if (resourceAmount > 0)
                resourceGraphic.resourceSmallFlat.SetActive(true);
        }
    }

}
