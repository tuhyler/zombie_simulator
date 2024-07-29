using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainData : MonoBehaviour
{
    private MapWorld world;
    
    [SerializeField]
    public TerrainDataSO terrainData;

    [SerializeField]
    public int prefabIndex = 0, decorIndex = 0;

    [HideInInspector]
    public List<int> uvMapIndex = new();

    [SerializeField]
    public Transform main, prop, nonstatic/*, minimapIcon*/;

    [SerializeField]
    public MeshFilter mainMesh;

    [SerializeField]
    public GameObject fog, highlightPlane, highlightPlaneIcon, flatlandFP;

    [SerializeField]
    private Material white;
    private List<MeshRenderer> whiteMesh = new();
    [HideInInspector]
    public List<Material> materials = new();
    [HideInInspector]
    public Region region;
    private GameObject animMesh; //hide when not discovered

    private SelectionHighlight highlight;

    private Vector3Int tileCoordinates;
    public Vector3Int TileCoordinates { get { return tileCoordinates; } set { tileCoordinates = value; } }

    private int movementCost; 
    public int MovementCost { get { return movementCost; } set { movementCost = value; } }

    [HideInInspector]
    public bool changeLeafColor;
    [HideInInspector] //walkable is when it's not discovered, canWalk is when it is, canPlayerWalk includes battlezones
	public bool isHill, hasRoad, hasResourceMap, walkable, sailable, enemyCamp, enemyZone, isSeaCorner, isLand = true, isGlowing = false, isDiscovered = true, beingCleared, 
        showProp = true, hasNonstatic, straightRiver, border, hasBattle, inBattle, canWalk, canPlayerWalk, canSail, canPlayerSail, canFly, canPlayerFly, spring;

    [HideInInspector]
    public string nonStaticName;
    [HideInInspector]
    public ResourceGraphicHandler resourceGraphic;
    [HideInInspector]
    public TreeHandler treeHandler;

    [SerializeField]
    private MeshFilter terrainMesh, minimapIconMesh;

    public RawResourceType rawResourceType;
    public ResourceType resourceType;
    public int resourceAmount;
    private int[] rotations = new int[4] { 0, 90, 180, 270 };

    private void Awake()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isLoading)
            TerrainDataPrep();
        terrainData.MovementCostCheck();
        ResetMovementCost();
        highlight = GetComponentInChildren<SelectionHighlight>();
        if (flatlandFP != null)
            flatlandFP.SetActive(false); //hide after adding to selectionhighlight
        if (highlightPlane != null)
        {
            highlightPlane.SetActive(false);
			highlightPlaneIcon.SetActive(false);
		}
    }

    private void Start()
    {
        if (flatlandFP != null)
            highlight.ManuallyAddRenderer(flatlandFP.GetComponent<MeshRenderer>());
    }

    public void SkinnedMeshCheck()
    {
		SkinnedMeshRenderer skinnedMesh = prop.GetComponentInChildren<SkinnedMeshRenderer>();
		if (skinnedMesh != null)
			animMesh = skinnedMesh.gameObject;
	}

	public void SetWorld(MapWorld world)
    {
        this.world = world;
    }

    public void SetData(TerrainDataSO data)
    {
        terrainData = data;
        TerrainDataPrep();

        if (terrainData.terrainDesc != TerrainDesc.Swamp && (CompareTag("Forest") || CompareTag("Forest Hill") || CompareTag("Hill") || CompareTag("Mountain")))
    		hasNonstatic = true;
	}

	public void TerrainDataPrep()
    {
        gameObject.tag = terrainData.tag;

        if (terrainData.type == TerrainType.Hill || terrainData.type == TerrainType.ForestHill)
			isHill = true;

        if (terrainData.sailable && terrainData.walkable && (prefabIndex == 1 || prefabIndex == 3 || prefabIndex == 4 || prefabIndex == 5))
            straightRiver = true;

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

		//if (terrainData.type == TerrainType.Coast || terrainData.type == TerrainType.Flatland || terrainData.type == TerrainType.Hill || terrainData.type == TerrainType.ForestHill ||
  //          terrainData.type == TerrainType.Forest || terrainData.type == TerrainType.River)
  //          uvs = mainMesh.sharedMesh.uv;

   //     if (rawResourceType == RawResourceType.Rocks)
   //     {
			//rockUVs = ResourceHolder.Instance.GetUVs(resourceType);
   //     }

		if (resourceType == ResourceType.Food && terrainData.terrainDesc == TerrainDesc.Grassland)
        {
            if (uvMapIndex.Count == 2)
                SetFlowers(uvMapIndex[0], uvMapIndex[1]);
        }

		isLand = terrainData.isLand;
		isSeaCorner = terrainData.isSeaCorner;
	}

    public void AddMountainMiddleToWhiteMesh(MeshRenderer renderer)
    {
		whiteMesh.Add(renderer);
		materials.Add(renderer.sharedMaterial);
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
			resourceGraphic = prop.GetComponentInChildren<ResourceGraphicHandler>();

		    foreach (MeshFilter mesh in resourceGraphic.GetComponentsInChildren<MeshFilter>())
		    {
                Vector2[] newUVs = mesh.mesh.uv;
			    int i = 0;
			    while (i < newUVs.Length)
			    {
				    newUVs[i] = ResourceHolder.Instance.GetUVs(resourceType);
				    i++;
			    }
			    mesh.mesh.uv = newUVs;
		    }

			//RocksCheck();
        }

		if (terrainData.type == TerrainType.Forest || terrainData.type == TerrainType.ForestHill)
		{
			treeHandler = prop.GetComponentInChildren<TreeHandler>();
            //treeHandler.TurnOffGraphics(false);
            //treeHandler.SwitchFromRoad(isHill);
            //treeHandler.SetMapIcon(isHill);

            if (changeLeafColor)
                ChangeLeafColors(treeHandler, spring);
		}
	}

    //used for non-static mesh, ie should not be included in staticbatchingutility
    public void SetNonStatic()
    {
		if (terrainData.type == TerrainType.Forest || terrainData.type == TerrainType.ForestHill)
		{
			TreeHandler treeHandler = nonstatic.GetComponentInChildren<TreeHandler>(); //must be its own treehandler

            if (changeLeafColor)
                ChangeLeafColors(treeHandler, spring);
		}
        else if (isHill && rawResourceType == RawResourceType.Rocks)
        {
            ResourceGraphicHandler resourceGraphic = nonstatic.GetComponentInChildren<ResourceGraphicHandler>();
            resourceGraphic.SetRocksAmount(resourceAmount);

			foreach (MeshFilter mesh in resourceGraphic.GetComponentsInChildren<MeshFilter>())
			{
				Vector2[] newUVs = mesh.mesh.uv;
				int i = 0;
				while (i < newUVs.Length)
				{
					newUVs[i] = ResourceHolder.Instance.GetUVs(resourceType);
					i++;
				}
				mesh.mesh.uv = newUVs;
			}
		}
	}

    public void SetVisibleProp()
    {
		if (rawResourceType == RawResourceType.Rocks)
        {
			RocksCheck();
		}
	}

    public void ClearRocks()
    {
        resourceGraphic.resourceAll.SetActive(false);
        //Destroy(resourceGraphic.gameObject);
    }

	public void ClearForest()
	{
        treeHandler.gameObject.SetActive(false);
        //Destroy(treeHandler.gameObject);
	}

	public void ShowProp(bool v)
    {
        prop.gameObject.SetActive(v);
        showProp = v;
        GameLoader.Instance.gameData.allTerrain[tileCoordinates].showProp = showProp;
    }

    public void FloodPlainCheck(bool v)
    {
        if (terrainData.specificTerrain == SpecificTerrain.FloodPlain)
        {
			ToggleTerrainMesh(!v);
            flatlandFP.SetActive(v);
		}
    }

    public void SetMinimapIcon()
    {
        if (minimapIconMesh)
            minimapIconMesh.mesh.uv = terrainMesh.sharedMesh.uv;   
    }

    public void CheckMinimapResource(UIMapHandler uiMapHandler)
    {
        if (resourceType != ResourceType.None /*&& resourceType != ResourceType.Food*/ && resourceType != ResourceType.Lumber /*&& resourceType != ResourceType.Fish*/)
        {
            hasResourceMap = true;
            //resourceIconSprite.sprite = ResourceHolder.Instance.GetIcon(resourceType);
            //resourceIcon.SetActive(true);
            ResourceType type = resourceType;
            //if (resourceType == ResourceType.Fish)
            //    type = ResourceType.Food;
            uiMapHandler.AddResourceToMap(tileCoordinates, type);
        }
    }

    public void RemoveMinimapResource(UIMapHandler uiMapHandler)
    {
        uiMapHandler.RemoveResourceFromMap(tileCoordinates, resourceType);
        hasResourceMap = false;
        world.ToggleResourceIcon(tileCoordinates, false);
    }

    public void SetUVs(Vector2[] uvs)
    {
        //this.uvs = uvs;
        terrainMesh.mesh.uv = uvs;
        if (minimapIconMesh)
            minimapIconMesh.mesh.uv = uvs;
    }

    //coordinates range from 0 to 2 for each
    private void SetFlowers(int flowerCoordX, int flowerCoordY)
    {
		foreach (MeshFilter mesh in prop.GetComponentsInChildren<MeshFilter>())
		{
            if (mesh.name == "Flowers")
            {
                float shiftConstant = 0.0208333f;
                float shiftX = 0;
                float shiftY = 0;

                switch (flowerCoordX)
                {
                    case 1:
                        shiftX = shiftConstant;
                        break;
                    case 2:
                        shiftX = shiftConstant * 2;
                        break;
				}

                switch (flowerCoordY)
                {
                    case 1:
                        shiftY = shiftConstant;
                        break;
                    case 2:
                        shiftY = shiftConstant * 2;
                        break;
                }
                
                Vector2[] oldUVs = mesh.mesh.uv;

                for (int i = 0; i < oldUVs.Length; i++)
                {
                    oldUVs[i].x = oldUVs[i].x + shiftX;
                    oldUVs[i].y = oldUVs[i].y - shiftY;
                }

                mesh.mesh.uv = oldUVs;
            }

        }
	}

	private void ChangeLeafColors(TreeHandler treeHandler, bool spring)
    {
		for (int i = 0; i < treeHandler.leafMeshList.Count; i++)
		{
			float xChange = 0;
			float yChange = 0;

			int newColor = uvMapIndex[i];

            if (spring)
            {
                xChange += 0.125f;
            }
            else
            {
                if (!spring)
                    newColor = i % 3 + 1;
            }
			
            if (newColor == 1)
			{
				xChange += 0.031153f;
			}
			else if (newColor == 2)
			{
				yChange += -0.031351f;
			}
			else
			{
				xChange += 0.031153f;
				yChange += -0.031351f;
			}

			Vector2[] leafUVs = treeHandler.leafMeshList[i].mesh.uv;
            Vector2[] changedUVs = ChangeLeafMeshUVs(leafUVs, xChange, yChange);
            treeHandler.leafMeshList[i].mesh.uv = changedUVs;
		}
    }

    private Vector2[] ChangeLeafMeshUVs(Vector2[] leafUVs, float xChange, float yChange)
    {
		int i = 0;

		while (i < leafUVs.Length)
		{
			leafUVs[i].x += xChange;
			leafUVs[i].y += yChange;
			i++;
		}

		return leafUVs;
    }

    public void ChangeMountainUVs(bool desert)
    {
        Vector2[] mountainUVs = mainMesh.sharedMesh.uv;

		int i = 0;
        float shift = desert ? 0.0625f : -0.0625f;

        while (i < mountainUVs.Length)
        {
            mountainUVs[i].x += shift;
			i++;
        }

        mainMesh.mesh.uv = mountainUVs;
        nonstatic.GetComponentInChildren<MeshFilter>().mesh.uv = mountainUVs;
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
        fog.SetActive(true);
        Vector3 newRot = new Vector3(0, rotations[Random.Range(0, 4)], 0);
        fog.transform.localEulerAngles = newRot;

		foreach (MeshRenderer renderer in whiteMesh)
        {
            renderer.material = white;
        }

		if (animMesh != null)
			animMesh.SetActive(false);
	}

    //no nonstatic movement
  //  public void HardReveal()
  //  {
  //      isDiscovered = true;
  //      SetMovement();
		//GameLoader.Instance.gameData.allTerrain[tileCoordinates].isDiscovered = true;

  //      //foreach (Vector3Int neighbor in world.GetNeighborsFor(tileCoordinates, MapWorld.State.EIGHTWAYINCREMENT))
  //      //{
  //      //    TerrainData td = world.GetTerrainDataAt(neighbor);
  //      //    if (td.isDiscovered)
  //      //        continue;

  //      //    td.HardSemiReveal();
  //      //}

		////if (rawResourceType == RawResourceType.Rocks)
  ////      {
  ////          godRays.transform.position = tileCoordinates + new Vector3(1, 3, 0);
  ////          godRays.Play();
		////}

  //      if (hasResourceMap)
		//	world.ToggleResourceIcon(tileCoordinates, true);

  //      if (resourceType != ResourceType.None && !world.resourceDiscoveredList.Contains(resourceType))
  //          world.DiscoverResource(resourceType);

		//for (int i = 0; i < whiteMesh.Count; i++)
  //          whiteMesh[i].material = materials[i];

  //      world.TurnOnEnemyBorders(tileCoordinates);
  //      world.TurnOnCenterBorders(tileCoordinates);

  //      //fog.SetActive(false);
  //      Destroy(fog);

  //      GameObject tempFog = Instantiate(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/UnexploredFade"));
  //      tempFog.transform.SetParent(transform, false);
  //      StartCoroutine(tempFog.GetComponent<UnexploredTerrain>().FadeFog());

		////fogNonStatic.gameObject.SetActive(true);
  ////      StartCoroutine(fogNonStatic.FadeFog(tileCoordinates == new Vector3Int(9, 0, 9)));

  //      whiteMesh.Clear();
  //      materials.Clear();

  //      if (animMesh != null)
  //          animMesh.SetActive(true);

  //      main.gameObject.SetActive(true);
  //      ShowProp(true);
  //  }

    public void LimitPlayerMovement()
    {
        canPlayerWalk = false;
        canPlayerSail = false;
        canPlayerFly = false;
    }

    public void SetMovement()
    {
		if (walkable)
		{
			canWalk = true;
			canPlayerWalk = true;
		}
		if (!border)
		{
			if (sailable)
			{
				canSail = true;
				canPlayerSail = true;
			}

			canFly = true;
			canPlayerFly = true;
		}
	}

    public void CanMoveCheck()
    {
        if (canWalk)
            canPlayerWalk = true;
        if (canSail)
            canPlayerSail = true;
        if (canFly)
            canPlayerFly = true;
    }

    public void Reveal(bool normReveal)
    {
		isDiscovered = true;
        SetMovement();
		GameLoader.Instance.gameData.allTerrain[tileCoordinates].isDiscovered = true;

        if (normReveal)
        {
		    if (rawResourceType == RawResourceType.Rocks)
            {
                Vector3 loc = tileCoordinates + new Vector3(1, 3, 0);
                world.CreateGodRay(loc);
			    //godRays.Play();

                if (!world.CompletedImprovementCheck(tileCoordinates))
                {
                    if (isHill)
                        resourceGraphic.PlaySoundHill();
                    else
                        resourceGraphic.PlaySound();
                }
		    }

            GameObject tempFog = Instantiate(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/UnexploredFade"));
            tempFog.transform.SetParent(transform, false);
            StartCoroutine(tempFog.GetComponent<UnexploredTerrain>().FadeFog());

            if (hasNonstatic)
            {
                main.gameObject.SetActive(false);
                if (rawResourceType != RawResourceType.Rocks) //so that sound will play
                    prop.gameObject.SetActive(false);
			    GameLoader.Instance.gameData.allTerrain[tileCoordinates].showProp = true;
                StartCoroutine(PopUp());
            }
        }

        if (hasResourceMap)
			world.ToggleResourceIcon(tileCoordinates, true);

        if (resourceType != ResourceType.None && !world.ResourceCheck(resourceType))
            world.DiscoverResource(resourceType);

        Destroy(fog);

        for (int i = 0; i < whiteMesh.Count; i++)
            whiteMesh[i].material = materials[i];

        world.TurnOnEnemyBorders(tileCoordinates);
        world.TurnOnCenterBorders(tileCoordinates);

		if (animMesh != null)
			animMesh.SetActive(true);

		whiteMesh.Clear();
        materials.Clear();
    }

    public void Discover()
    {
        //fog.SetActive(false);
        Destroy(fog);
        //Destroy(fogNonStatic.gameObject);
        Destroy(nonstatic.gameObject);
        whiteMesh.Clear();
        materials.Clear();
	}

    public void SetNewData(TerrainDataSO data)
    {
        terrainData = data;
        rawResourceType = data.rawResourceType;
        resourceType = data.resourceType;
        gameObject.tag = data.tag;
        decorIndex = 0;
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
        if (isHill && rawResourceType == RawResourceType.Rocks)
            RocksCheck();

        Destroy(nonstatic.gameObject);
        //nonstatic.gameObject.SetActive(false);
    }

    public void ToggleTerrainMesh(bool v)
    {
        if (terrainMesh != null)
            terrainMesh.gameObject.SetActive(v);
	}

    //public void HighlightResource(Sprite sprite)
    //{
    //    resourceBackgroundSprite.sprite = sprite;
    //}

    //public void RestoreResourceIcon(Sprite sprite)
    //{
    //    resourceBackgroundSprite.sprite = sprite;
    //}

    public void HideResourceMap()
    {
		world.ToggleResourceIcon(tileCoordinates, false);
	}

    public void RestoreResourceMap()
    {
		world.ToggleResourceIcon(tileCoordinates, true);
	}

    public void SetHighlightMesh()
    {
        highlight.PrepareMaterialDictionaries();
    }

    public void EnableHighlight(Color highlightColor)
    {
        if (!isDiscovered)
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

        if (hasBattle && !inBattle)
            highlight.EnableHighlight(Color.red);
    }

    public void BattleHighlight()
    {
        if (isGlowing)
            return;

		if (!isLand)
			ToggleHighlightPlane(true);

		highlight.EnableHighlight(Color.red);
	}

    public void DisableBattleHighlight()
    {
		if (isGlowing)
			return;

		if (!isLand)
			ToggleHighlightPlane(false);

		if (highlight != null)
			highlight.DisableHighlight();
	}

    //public void ToggleTransparentForest(bool v)
    //{
    //    if (treeHandler != null)
    //    {
    //        treeHandler.ToggleForestClear(v, isHill, world.atlasSemiClear);
    //    } 
    //    else if (world.IsTradeCenterOnTile(tileCoordinates))
    //    {
    //        world.GetTradeCenter(tileCoordinates).ToggleClear(v);
    //    }
    //}

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
        {
            //prop.gameObject.SetActive(v);
            highlightPlane.SetActive(v);
            highlightPlaneIcon.SetActive(v);
        }
    }

    public void RocksCheck()
    {
        resourceGraphic.SetRocksAmount(resourceAmount);
    }

    public void SwitchToRoad()
    {
		treeHandler.RoadSwitch(false);
    }

    public void SwitchFromRoad()
    {
        treeHandler.RoadSwitch(true);
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
            ClearRocks();
			//ShowProp(false);
			GameLoader.Instance.gameData.allTerrain[tileCoordinates] = SaveData();
		}
		else //only updating resource amount
		{
			GameLoader.Instance.gameData.allTerrain[tileCoordinates].resourceAmount = resourceAmount;
		}


		return amount;
    }

    //public void DestroyBorders()
    //{
    //    for (int i = 0; i < enemyBorders.Count; i++)
    //    {
    //        Destroy(enemyBorders[i]);
    //    }

    //    enemyBorders.Clear();
    //}

    public TerrainSaveData SaveData()
    {
        TerrainSaveData data = new();

        data.name = terrainData.terrainName;
        data.tileCoordinates = tileCoordinates;
        data.rotation = transform.rotation;
        data.mainRotation = main.rotation;
        if (treeHandler != null && treeHandler.propMesh != null)
            data.propRotation = treeHandler.propMesh.rotation;
        else
            data.propRotation = prop.rotation;
        data.rawResourceType = rawResourceType;
        data.resourceType = resourceType;
        data.changeLeafColor = changeLeafColor;
        data.spring = spring;
        data.showProp = showProp;
        data.variant = prefabIndex;
        data.decor = decorIndex;
        data.uvMapIndex = uvMapIndex;
        data.border = border;
        data.region = region;

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
        changeLeafColor = data.changeLeafColor;
        spring = data.spring;
        showProp = data.showProp;
        prefabIndex = data.variant;
        decorIndex = data.decor;
        main.rotation = data.mainRotation;
        nonstatic.rotation = data.mainRotation;
        uvMapIndex = data.uvMapIndex;
        border = data.border;
        region = data.region;
	}
}
