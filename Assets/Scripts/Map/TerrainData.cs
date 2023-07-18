using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainData : MonoBehaviour
{
    [SerializeField]
    public TerrainDataSO terrainData;

    [SerializeField]
    public Transform main, prop, nonstatic;

    [SerializeField]
    public GameObject fog, highlightPlane, resourceIcon;

    [SerializeField]
    private UnexploredTerrain fogNonStatic;

    [SerializeField]
    private SpriteRenderer resourceBackgroundSprite, resourceIconSprite;

    private SelectionHighlight highlight;

    private Vector3Int tileCoordinates;
    public Vector3Int TileCoordinates { get { return tileCoordinates; } }

    //private int originalMovementCost;
    //public int OriginalMovementCost { get { return originalMovementCost; } }

    private int movementCost; 
    public int MovementCost { get { return movementCost; } set { movementCost = value; } }

    [HideInInspector]
    public bool isHill, hasRoad, hasResourceMap;

    private bool isLand = true;
    private bool isCoast = false;
    public bool IsCoast { get { return isCoast; } }
    private bool isSeaCorner = false;
    public bool IsSeaCorner { get { return isSeaCorner; } }
    [HideInInspector]
    public bool isGlowing = false, isDiscovered = true;

    private ResourceGraphicHandler resourceGraphic;
    private TreeHandler treeHandler;
    [SerializeField]
    private ParticleSystem godRays;

    //private List<MeshRenderer> renderers = new();

    [SerializeField]
    private MeshFilter terrainMesh, minimapIconMesh;
    private Vector2[] uvs;
    public Vector2[] UVs { get { return uvs; } }
    private Vector2 rockUVs;
    public Vector2 RockUVs { get { return rockUVs; } }

    public int resourceAmount;

    private void Awake()
    {
        if (terrainData.type == TerrainType.Hill || terrainData.type == TerrainType.ForestHill)
            isHill = true;
        
        if (terrainData.type == TerrainType.Flatland || terrainData.type == TerrainType.Hill || terrainData.type == TerrainType.ForestHill || terrainData.type == TerrainType.Forest || terrainData.type == TerrainType.River)
            uvs = main.GetComponentInChildren<MeshFilter>().mesh.uv;
        if (terrainData.hasRocks)
        {
            if (terrainData.rawResourceType == RawResourceType.Rocks)
                rockUVs = ResourceHolder.Instance.GetUVs(terrainData.resourceType);
            //rockUVs = prop.GetComponentInChildren<MeshFilter>().mesh.uv[0];

            foreach (MeshFilter mesh in prop.GetComponentsInChildren<MeshFilter>())
            {
                Vector2[] newUVs = mesh.mesh.uv;
                int i = 0;
                while (i < newUVs.Length)
                {
                    newUVs[i] = rockUVs;
                    i++;
                }
                mesh.mesh.uv = newUVs;
            }
        }

        //PrepareRenderers();
        
        isLand = terrainData.isLand;
        isSeaCorner = terrainData.isSeaCorner;
        terrainData.MovementCostCheck();
        ResetMovementCost();
        highlight = GetComponentInChildren<SelectionHighlight>();
        if (highlightPlane != null)
        {
            highlightPlane.SetActive(false);
            isCoast = true;
        }

        if (isLand && terrainMesh == null)
            Debug.Log(transform.position);
    }

    private void Start()
    {
        if (terrainData.hasRocks)
        {
            resourceGraphic = prop.GetComponentInChildren<ResourceGraphicHandler>();
            resourceGraphic.isHill = isHill;
            RocksCheck();
        }
        
        if (terrainData.type == TerrainType.Forest || terrainData.type == TerrainType.ForestHill)
        {
            treeHandler = prop.GetComponentInChildren<TreeHandler>();
            treeHandler.TurnOffGraphics(false);
            treeHandler.SwitchFromRoad(isHill);
            treeHandler.SetMapIcon(isHill);
        }
    }

    //private void PrepareRenderers()
    //{
    //    foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>())
    //    {
    //        renderers.Add(renderer);
    //    }
    //}

    public void PrepParticleSystem()
    {
        godRays = Instantiate(godRays);
        //godRays.transform.parent = transform;
        //godRays.transform.position = transform.position;
        //godRays.transform.SetParent(transform, false);
        godRays.Pause();
    }

    public void SetMinimapIcon()
    {
        if (minimapIconMesh)
            minimapIconMesh.mesh.uv = terrainMesh.mesh.uv;   
    }

    public void CheckMinimapResource(UIMapHandler uiMapHandler)
    {
        if (terrainData.resourceType != ResourceType.None && terrainData.resourceType != ResourceType.Food && terrainData.resourceType != ResourceType.Lumber && terrainData.resourceType != ResourceType.Fish)
        {
            hasResourceMap = true;
            resourceIconSprite.sprite = ResourceHolder.Instance.GetIcon(terrainData.resourceType);
            //resourceIcon.SetActive(true);
            uiMapHandler.AddResourceToMap(tileCoordinates, terrainData.resourceType);
        }
    }

    public void RemoveMinimapResource(UIMapHandler uiMapHandler)
    {
        uiMapHandler.RemoveResourceFromMap(tileCoordinates, terrainData.resourceType);
        hasResourceMap = false;
        resourceIcon.SetActive(false);
    }

    public void SetUVs(Vector2[] uvs)
    {
        this.uvs = uvs;
        terrainMesh.mesh.uv = uvs;
        if (minimapIconMesh)
            minimapIconMesh.mesh.uv = uvs;
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

    public void Hide()
    {
        isDiscovered = false;
        main.gameObject.SetActive(false);
        prop.gameObject.SetActive(false);
        //Vector3 offsetY = new Vector3(0, -.01f, 0);

        //if ((tileCoordinates.x % 2 == 0 && tileCoordinates.z % 2 == 0) || (tileCoordinates.x % 2 == 1 && tileCoordinates.z % 2 == 1))
        //    fog.transform.localPosition += offsetY;
    }

    public void Reveal()
    {
        isDiscovered = true;
        if (hasResourceMap)
        {
            resourceIcon.SetActive(true);
            godRays.transform.position = tileCoordinates + new Vector3(1, 2, 0);
            godRays.Play();
        }

        fog.SetActive(false);
        fogNonStatic.gameObject.SetActive(true);
        StartCoroutine(fogNonStatic.FadeFog());
        if (nonstatic.childCount > 0)
          StartCoroutine(PopUp());
        else
        {
            main.gameObject.SetActive(true);
            prop.gameObject.SetActive(true);
        }
    }

    public void Discover()
    {
        fog.gameObject.SetActive(false);
    }

    private IEnumerator PopUp()
    {
        Vector3 scale = nonstatic.localScale;
        float growSpeed = 2f;
        nonstatic.gameObject.SetActive(true);

        while (nonstatic.localScale.y < 1.2f)
        {
            scale.y += 4 * Time.deltaTime;
            nonstatic.localScale = scale; 
            
            yield return null;
        }

        while (nonstatic.localScale.y > 1f)
        {
            scale.y -= growSpeed * Time.deltaTime;
            nonstatic.localScale = scale;

            yield return null;
        }

        main.gameObject.SetActive(true);
        prop.gameObject.SetActive(true);
        nonstatic.gameObject.SetActive(false);
    }

    public void HideTerrainMesh()
    {
        terrainMesh.gameObject.SetActive(false);
    }

    public void RestoreTerrainMesh()
    {
        terrainMesh.gameObject.SetActive(true);
    }

    public void HighlightResource(Sprite sprite)
    {
        resourceBackgroundSprite.sprite = sprite;
    }

    public void RestoreResource(Sprite sprite)
    {
        resourceBackgroundSprite.sprite = sprite;
    }

    public void HideResourceMap()
    {
        resourceIcon.SetActive(false);
    }

    public void RestoreResourceMap()
    {
        resourceIcon.SetActive(true);
    }

    public void EnableHighlight(Color highlightColor)
    {
        if (isGlowing || !isDiscovered)
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

    public void SwitchToRoad()
    {
        treeHandler.SwitchToRoad(isHill);
    }

    public void SwitchFromRoad()
    {
        treeHandler.SwitchFromRoad(isHill);
    }
}
