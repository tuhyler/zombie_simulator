using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class TerrainData : MonoBehaviour
{
    private MapWorld world;
    
    [SerializeField]
    public TerrainDataSO terrainData;

    [SerializeField]
    public int prefabIndex = 0, decorIndex = 0, uvMapIndex;

    [SerializeField]
    public Transform main, prop, nonstatic/*, minimapIcon*/;

    [SerializeField]
    public GameObject fog, highlightPlane, resourceIcon;

    [SerializeField]
    private UnexploredTerrain fogNonStatic;

    [SerializeField]
    private SpriteRenderer resourceBackgroundSprite, resourceIconSprite;

    [SerializeField]
    private Material white;
    private List<MeshRenderer> whiteMesh = new();
    private List<Material> materials = new();

    private SelectionHighlight highlight;

    private Vector3Int tileCoordinates;
    public Vector3Int TileCoordinates { get { return tileCoordinates; } set { tileCoordinates = value; } }

    //private int originalMovementCost;
    //public int OriginalMovementCost { get { return originalMovementCost; } }

    private int movementCost; 
    public int MovementCost { get { return movementCost; } set { movementCost = value; } }

    public bool changeLeafColor;
    [HideInInspector]
    public bool isHill, hasRoad, hasResourceMap, walkable, sailable, enemyCamp, enemyZone, isSeaCorner, isLand = true, isGlowing = false, isDiscovered = true, beingCleared, showProp = true;

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

    public RawResourceType rawResourceType;
    public ResourceType resourceType;
    public int resourceAmount;
    private int[] rotations = new int[4] { 0, 90, 180, 270 };
    //[HideInInspector]
    //public string originalTag;

    private void Awake()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isLoading)
            TerrainDataPrep();
        terrainData.MovementCostCheck();
        ResetMovementCost();
        highlight = GetComponentInChildren<SelectionHighlight>();
        if (highlightPlane != null)
            highlightPlane.SetActive(false);
    }

    //private void Start()
    //{
    //    SetProp();
    //}

    public void SetWorld(MapWorld world)
    {
        this.world = world;
    }

    public void SetData(TerrainDataSO data)
    {
        terrainData = data;
        TerrainDataPrep();
    }

    public void TerrainDataPrep()
    {
        gameObject.tag = terrainData.tag;
        
        if (terrainData.type == TerrainType.Hill || terrainData.type == TerrainType.ForestHill)
			isHill = true;

        if (terrainData.rawResourceType != RawResourceType.None)
            rawResourceType = terrainData.rawResourceType;

		if (terrainData.resourceType != ResourceType.None)
			resourceType = terrainData.resourceType;

		walkable = terrainData.walkable;
		sailable = terrainData.sailable;

		if (terrainData.type == TerrainType.Obstacle || isHill)
		{
			foreach (MeshRenderer renderer in main.GetComponentsInChildren<MeshRenderer>())
			{
				whiteMesh.Add(renderer);
				materials.Add(renderer.sharedMaterial);
			}
		}

		if (terrainData.type == TerrainType.Flatland || terrainData.type == TerrainType.Hill || terrainData.type == TerrainType.ForestHill || terrainData.type == TerrainType.Forest || terrainData.type == TerrainType.River)
			uvs = main.GetComponentInChildren<MeshFilter>().sharedMesh.uv;
		if (rawResourceType == RawResourceType.Rocks)
		{
			if (rawResourceType == RawResourceType.Rocks)
            {
				rockUVs = ResourceHolder.Instance.GetUVs(resourceType);
            }

		}

		isLand = terrainData.isLand;
		isSeaCorner = terrainData.isSeaCorner;
	}

    public void SetProp()
    {
		foreach (MeshRenderer renderer in prop.GetComponentsInChildren<MeshRenderer>())
		{
			whiteMesh.Add(renderer);
			materials.Add(renderer.sharedMaterial);
		}

        if (rawResourceType == RawResourceType.Rocks)
        {
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

			resourceGraphic = prop.GetComponentInChildren<ResourceGraphicHandler>();
			resourceGraphic.isHill = isHill;
			//RocksCheck();
        }

		if (terrainData.type == TerrainType.Forest || terrainData.type == TerrainType.ForestHill)
		{
			treeHandler = prop.GetComponentInChildren<TreeHandler>();
			//treeHandler.TurnOffGraphics(false);
			//treeHandler.SwitchFromRoad(isHill);
			//treeHandler.SetMapIcon(isHill);

            if (changeLeafColor)
                ChangeLeafColors(uvMapIndex);
		}
	}

    public void SetNonStatic()
    {
		if (terrainData.type == TerrainType.Forest || terrainData.type == TerrainType.ForestHill)
		{
			TreeHandler treeHandler = nonstatic.GetComponentInChildren<TreeHandler>();
            treeHandler.TurnOffGraphics(false);
            treeHandler.SwitchFromRoad(isHill);
            //treeHandler.SetMapIcon(isHill);

            if (changeLeafColor)
				ChangeLeafColors(uvMapIndex);
		}
	}

    public void SetVisibleProp()
    {
		if (terrainData.type == TerrainType.Forest || terrainData.type == TerrainType.ForestHill)
        {
			treeHandler.TurnOffGraphics(false);
			treeHandler.SwitchFromRoad(isHill);
			treeHandler.SetMapIcon(isHill/*, transform.rotation*/);
		}
        else if (rawResourceType == RawResourceType.Rocks)
        {
			RocksCheck();
		}
	}

	//private void PrepareRenderers()
	//{
	//    foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>())
	//    {
	//        renderers.Add(renderer);
	//    }
	//}

	public void ShowProp(bool v)
    {
        prop.gameObject.SetActive(v);
        showProp = v;
        GameLoader.Instance.gameData.allTerrain[tileCoordinates].showProp = showProp;
    }

    public void PrepParticleSystem()
    {
        godRays = Instantiate(godRays);
        godRays.transform.SetParent(world.psHolder, false);
        //godRays.transform.parent = transform;
        //godRays.transform.position = transform.position;
        //godRays.transform.SetParent(transform, false);
        godRays.Pause();
    }

    public void SetMinimapIcon()
    {
        if (minimapIconMesh)
            minimapIconMesh.sharedMesh.uv = terrainMesh.sharedMesh.uv;   
    }

    public void CheckMinimapResource(UIMapHandler uiMapHandler)
    {
        if (resourceType != ResourceType.None && resourceType != ResourceType.Food && resourceType != ResourceType.Lumber && resourceType != ResourceType.Fish)
        {
            hasResourceMap = true;
            resourceIconSprite.sprite = ResourceHolder.Instance.GetIcon(resourceType);
            //resourceIcon.SetActive(true);
            uiMapHandler.AddResourceToMap(tileCoordinates, resourceType);
        }
    }

    public void RemoveMinimapResource(UIMapHandler uiMapHandler)
    {
        uiMapHandler.RemoveResourceFromMap(tileCoordinates, resourceType);
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

    public void ChangeLeafColors(int color)
    {
        float xChange = 0;
        float yChange = 0;
        
        if (color == 1)
        {
            xChange = 0.031153f;
        }
        else if (color == 2)
        {
            yChange = -0.031351f;
        }
        else
        {
			xChange = 0.031153f;
			yChange = -0.031351f;
		}

        Vector2[] leafUVs = treeHandler.leafMesh.mesh.uv;
        int i = 0;

        while (i < leafUVs.Length)
        {
            leafUVs[i].x += xChange;
            leafUVs[i].y += yChange;
            i++;
        }

        treeHandler.leafMesh.mesh.uv = leafUVs;
    }

    public void ChangeMountainSnow()
    {
        Vector2[] mountainUVs = uvs;
        int i = 0;

        while (i < mountainUVs.Length)
        {
            mountainUVs[i].x += 0.375031f;
			i++;
        }

        main.GetComponentInChildren<MeshFilter>().mesh.uv = mountainUVs;
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

    public void SetCoastCoordinates()
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

    public void SetTileCoordinates()
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
		GameLoader.Instance.gameData.allTerrain[tileCoordinates].isDiscovered = false;
        fog.SetActive(true);
        Vector3 newRot = new Vector3(0, rotations[Random.Range(0, 4)], 0);
        fog.transform.localEulerAngles = newRot;

		foreach (MeshRenderer renderer in whiteMesh)
        {
            renderer.material = white;
        }


        //main.gameObject.SetActive(false);
        //prop.gameObject.SetActive(false);
        //Vector3 offsetY = new Vector3(0, -.01f, 0);

        //if ((tileCoordinates.x % 2 == 0 && tileCoordinates.z % 2 == 0) || (tileCoordinates.x % 2 == 1 && tileCoordinates.z % 2 == 1))
        //    fog.transform.localPosition += offsetY;
    }

    public void HardReveal()
    {
        isDiscovered = true;
		GameLoader.Instance.gameData.allTerrain[tileCoordinates].isDiscovered = true;

		//foreach (Vector3Int neighbor in world.GetNeighborsFor(tileCoordinates, MapWorld.State.EIGHTWAYINCREMENT))
		//{
		//    TerrainData td = world.GetTerrainDataAt(neighbor);
		//    if (td.isDiscovered)
		//        continue;

		//    td.HardSemiReveal();
		//}

		if (hasResourceMap)
        {
            resourceIcon.SetActive(true);
            godRays.transform.position = tileCoordinates + new Vector3(1, 3, 0);
            godRays.Play();
        }

        for (int i = 0; i < whiteMesh.Count; i++)
            whiteMesh[i].material = materials[i];

        fog.SetActive(false);
        fogNonStatic.gameObject.SetActive(true);
        StartCoroutine(fogNonStatic.FadeFog());

        whiteMesh.Clear();
        materials.Clear();

        main.gameObject.SetActive(true);
        ShowProp(true);
    }

    //public void HardSemiReveal()
    //{
    //    foreach (MeshRenderer renderer in whiteMesh)
    //    {
    //        renderer.material = white;
    //    }

    //    main.gameObject.SetActive(true);
    //    prop.gameObject.SetActive(true);
    //}

    public void Reveal()
    {
        isDiscovered = true;
		GameLoader.Instance.gameData.allTerrain[tileCoordinates].isDiscovered = true;

		//foreach (Vector3Int neighbor in world.GetNeighborsFor(tileCoordinates, MapWorld.State.EIGHTWAYINCREMENT))
		//{
		//    TerrainData td = world.GetTerrainDataAt(neighbor);
		//    if (td.isDiscovered)
		//        continue;

		//    td.SemiReveal();
		//}

		if (hasResourceMap)
        {
            resourceIcon.SetActive(true);
            godRays.transform.position = tileCoordinates + new Vector3(1, 3, 0);
			godRays.Play();
        }

        fog.SetActive(false);
        fogNonStatic.gameObject.SetActive(true);
        StartCoroutine(fogNonStatic.FadeFog());

        int i = 0;
        foreach (MeshRenderer renderer in whiteMesh)
        {
            renderer.material = materials[i];
            i++;
        }

        whiteMesh.Clear();
        materials.Clear();
        if (nonstatic.childCount > 0)
        {
            main.gameObject.SetActive(false);
            //ShowProp(false);
			prop.gameObject.SetActive(false);
			GameLoader.Instance.gameData.allTerrain[tileCoordinates].showProp = true;
            StartCoroutine(PopUp());
        }
    }

    public void Discover()
    {
        fog.SetActive(false);
	}

    public void SetNewData(TerrainDataSO data)
    {
        terrainData = data;
        rawResourceType = data.rawResourceType;
        resourceType = data.resourceType;
        gameObject.tag = data.tag;
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
		ShowProp(true);
        nonstatic.gameObject.SetActive(false);
    }

    public void HideTerrainMesh()
    {
        if (terrainMesh != null)
            terrainMesh.gameObject.SetActive(false);
    }

    public void RestoreTerrainMesh()
    {
        if (terrainMesh != null)
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

    public void SetHighlightMesh()
    {
        highlight.PrepareMaterialDictionaries();
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

    public void AddTerrainToWorld()
    {
        world.SetTerrainData(tileCoordinates, this);
    }

    public void DestroyTile()
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

    public int GatherResourceAmount(int amount)
    {
        if (resourceAmount < 0)
            return amount;
        
        if (amount >= resourceAmount)
        {
            amount = resourceAmount;
            resourceAmount = -1;
        }
        else
        {
            resourceAmount -= amount;
		}

        if (rawResourceType == RawResourceType.Rocks)
            RocksCheck();

        if (resourceAmount < 0)
        {
            TerrainDataSO tempData;
            if (terrainData.grassland)
				tempData = isHill ? world.grasslandHillTerrain : world.grasslandTerrain;
			else
				tempData = isHill ? world.desertHillTerrain : world.desertTerrain;

            SetNewData(tempData);
			GameLoader.Instance.gameData.allTerrain[tileCoordinates] = SaveData();
			ShowProp(false);
		}
		else //only updating resource amount
		{
			GameLoader.Instance.gameData.allTerrain[tileCoordinates].resourceAmount = resourceAmount;
		}


		return amount;
    }

    public TerrainSaveData SaveData()
    {
        TerrainSaveData data = new();

        data.name = terrainData.terrainName;
        data.tileCoordinates = tileCoordinates;
        data.rotation = transform.rotation;
        data.propRotation = prop.rotation;
        data.rawResourceType = rawResourceType;
        data.resourceType = resourceType;
        data.showProp = showProp;
        data.variant = prefabIndex;
        data.decor = decorIndex;

        data.isDiscovered = isDiscovered;
        data.beingCleared = beingCleared;
        data.resourceAmount = resourceAmount;

        return data;
    }

    public void LoadData(TerrainSaveData data)
    {
		tileCoordinates = data.tileCoordinates;
		isDiscovered = data.isDiscovered;
		beingCleared = data.beingCleared;
		resourceAmount = data.resourceAmount;
        rawResourceType = data.rawResourceType;
        resourceType = data.resourceType;
        showProp = data.showProp;
        prefabIndex = data.variant;
        decorIndex = data.decor;
	}
}
