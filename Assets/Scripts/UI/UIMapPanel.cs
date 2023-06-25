using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Linq;

public class UIMapPanel : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    private MapWorld world;

    [SerializeField]
    private UIUnitTurnHandler uiUnitTurn;

    [SerializeField]
    private CameraController cameraController;

    [SerializeField]
    private RectTransform mainRect, gridRect;

    [SerializeField]
    private Transform unitHolder, cityNameHolder, roadHolder, improvementHolder, resourceHolder;

    [SerializeField]
    private Image panel;

    [SerializeField]
    private GameObject mapPanelTile, mapUnitIcon, mapCityText, mapPanelImage, mapPanelResource;

    [SerializeField]
    private GridLayoutGroup grid;

    [SerializeField]
    private UIMapResourceSearch resourceSearch;

    private Dictionary<Vector3Int, UIMapPanelTile> mapTileDict = new();
    private Dictionary<Vector3Int, Vector3> mapLocDict = new();
    private Dictionary<ResourceType, List<Vector3Int>> resourceLocDict = new();
    private ResourceType highlightType = ResourceType.None;
    private Dictionary<Vector3Int, GameObject> resourceImageDict = new();
    private Dictionary<Vector3Int, GameObject> improvementImageDict = new();
    private Dictionary<Vector3Int, GameObject[]> roadImageDict = new();

    [SerializeField]
    private Sprite grassland, desert, grasslandHill, desertHill, grasslandFloodPlain, desertFloodPlain, forest, forestHill, jungle, jungleHill, mountain, swamp, sea, undiscovered, highlight, city, wonder, tradeCenter;

    [SerializeField]
    public Sprite roadSolo, roadStraight, roadDiagonal;// roadUp, roadUpRight, roadRight, roadDownRight, roadDown, roadDownLeft, roadLeft, roadUpLeft;

    private Vector3 newPosition;
    public float movementSpeed = 1, movementTime, zoomTime;

    public int mapWidth = 50, mapHeight = 50, offset = 25; //offset if tile values are less than 0 on map

    //[SerializeField] //for tweening
    //private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus;
    private Vector3 originalLoc;

    private void Awake()
    {
        CreateDictionary();
        resourceSearch.mapPanel = this;

        //if (Screen.height > 1080)
        //    workerIcon.transform.localScale = new Vector3(50f, 50f, 50f);
        //else if (Screen.height < 1080)
        //    workerIcon.transform.localScale = new Vector3(70f, 70f, 70f);

        newPosition = transform.localPosition; //set static position that doesn't default to 0

        //originalLoc = mainRect.anchoredPosition3D;
        //gameObject.SetActive(false);
        //mainRect.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        //Vector3 minimapLoc = new Vector3(-35, -95, 0);
        //Vector3 ugh = Camera.main.WorldToScreenPoint(minimapLoc);
        //ugh.z = 1000;
        //Vector3 ugh2 = Camera.main.ScreenToWorldPoint(ugh);
        //transform.position = ugh;
        gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        HandleKeyboardInput();
        Zoom();
    }

    public void CreateDictionary()
    {
        for (int z = mapHeight; z >= 0; z--)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                Vector3Int tileCoordinates = new Vector3Int(-offset * world.Increment + x * world.Increment, 0, -offset * world.Increment + z * world.Increment);
                GameObject tile = Instantiate(mapPanelTile);
                UIMapPanelTile panelTile = tile.GetComponent<UIMapPanelTile>();
                panelTile.SetTile(undiscovered);
                panelTile.gameObject.transform.SetParent(grid.transform, false);
                mapTileDict[tileCoordinates] = panelTile;
                mapLocDict[tileCoordinates] = SetDictLocValue(tileCoordinates);
                //SetDictLocValue(tileCoordinates);

                foreach (Vector3Int neighbor in world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAY))
                {
                    Vector3Int newTile = neighbor + tileCoordinates;
                    mapLocDict[newTile] = SetDictLocValue(newTile);
                }
            }
        }

        mainRect.sizeDelta = new Vector2(mapWidth * grid.cellSize.x, mapHeight * grid.cellSize.y);
        gridRect.sizeDelta = new Vector2(mapWidth * grid.cellSize.x, mapHeight * grid.cellSize.y);
        grid.constraintCount = mapWidth;
    }

    private Vector3 SetDictLocValue(Vector3Int pos)
    {
        Vector3 loc = pos;
        loc *= grid.cellSize.x / 3;
        loc.x += grid.cellSize.x * 0.5f;
        loc.z += -grid.cellSize.x * 0.5f;
        loc.y = loc.z;
        loc.z = 0;

        return loc;
        //mapLocDict[pos] = loc;
    }

    public void AddTileToMap(Vector3Int loc)
    {
        TerrainData td = world.GetTerrainDataAt(loc);
        mapTileDict[loc].SetTile(GetSprite(td.terrainData.terrainDesc));
        mapTileDict[loc].TileDesc = td.terrainData.terrainDesc;

        if (td.terrainData.resourceType != ResourceType.Food && td.terrainData.resourceType != ResourceType.None && td.terrainData.resourceType != ResourceType.Lumber && td.terrainData.resourceType != ResourceType.Fish)
        {
            GameObject resourceGO = Instantiate(mapPanelResource);
            resourceGO.gameObject.transform.SetParent(resourceHolder, false);
            Vector3 pos = mapLocDict[loc];// SetDictLocValue(loc);
            pos.y -= grid.cellSize.x * 0.5f;
            resourceGO.transform.localPosition = pos;
            resourceGO.GetComponent<UIMapResourceImage>().resourceImage.sprite = ResourceHolder.Instance.GetIcon(td.terrainData.resourceType);
            resourceImageDict[loc] = resourceGO;
            //mapTileDict[loc].SetResource(ResourceHolder.Instance.GetIcon(td.terrainData.resourceType));
            mapTileDict[loc].isDiscovered = true; //currently everything is discovered

            if (!resourceLocDict.ContainsKey(td.terrainData.resourceType))
                resourceLocDict[td.terrainData.resourceType] = new List<Vector3Int>();

            resourceLocDict[td.terrainData.resourceType].Add(loc);
        }
    }

    public void ToggleVisibility(bool v)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            world.UnselectAll();
            uiUnitTurn.gameObject.SetActive(false);
            gameObject.SetActive(v);
            resourceSearch.gameObject.SetActive(true);
            world.showingMap = true;

            activeStatus = true;

            Vector3 loc = cameraController.transform.position;
            loc /= world.Increment;
            loc.x *= -75f;
            loc.z *= -100f;
            loc.x = Mathf.Clamp(loc.x, -280f, 280);
            loc.z = Mathf.Clamp(loc.z, -240f, 240f);

            newPosition = new Vector3(loc.x, loc.z, -700f);
            mainRect.localPosition = new Vector3(loc.x, loc.z, -700f);
            //mainRect.localScale = Vector3.one;

            //allContents.anchoredPosition3D = originalLoc + new Vector3(0, 1200f, 0);

            //LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -1200f, 0.5f).setEaseOutSine();
            //LeanTween.alpha(allContents, 1f, 0.5f).setFrom(0f).setEaseLinear();
        }
        else
        {
            RemoveTileHighlights();
            highlightType = ResourceType.None;
            resourceSearch.ResetDropdown();
            resourceSearch.gameObject.SetActive(false);
            activeStatus = false;
            world.showingMap = false;
            uiUnitTurn.gameObject.SetActive(true);
            gameObject.SetActive(v);
            //transform.position = originalLoc;

            //LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 1200f, 0.3f).setOnComplete(SetActiveStatusFalse);
        }

        cameraController.enabled = !v;
    }

    public void SetImprovement(Vector3Int loc, Sprite sprite)
    {
        GameObject improvementGO = Instantiate(mapPanelImage);
        improvementGO.gameObject.transform.SetParent(improvementHolder, false);
        Vector3 pos = mapLocDict[loc];
        improvementGO.transform.localPosition = pos;
        //improvementGO.transform.position = SetDictLocValue(loc);
        improvementGO.GetComponent<Image>().sprite = sprite;
        improvementImageDict[loc] = improvementGO;

        //mapTileDict[loc].SetImprovement(sprite);
    }

    public void RemoveImprovement(Vector3Int loc)
    {
        //mapTileDict[loc].RemoveImprovement();
        Destroy(improvementImageDict[loc]);
    }

    public void RemoveResource(Vector3Int loc)
    {
        Destroy(resourceImageDict[loc]);
    }

    public void SetTileSprite(Vector3Int loc, TerrainDesc terrainDesc)
    {
        mapTileDict[loc].SetTile(GetSprite(terrainDesc));
    }

    public void HighlightTile(ResourceType type)
    {
        RemoveTileHighlights();

        if (!resourceLocDict.ContainsKey(type))
            return;

        highlightType = type;

        if (highlightType == ResourceType.None)
            return;

        foreach (Vector3Int tile in resourceLocDict[highlightType])
        {
            if (mapTileDict[tile].isDiscovered)
                mapTileDict[tile].SetTile(highlight);
        }
    }

    public void RemoveTileHighlights()
    {
        if (highlightType == ResourceType.None)
            return;

        foreach (Vector3Int tile in resourceLocDict[highlightType])
        {
            if (mapTileDict[tile].isDiscovered)
                mapTileDict[tile].SetTile(GetSprite(mapTileDict[tile].TileDesc));
        }
    }

    public Sprite GetSprite(TerrainDesc desc)
    {

        Sprite sprite = null;

        switch (desc)
        {
            case TerrainDesc.Grassland:
                sprite = grassland;
                break;
            case TerrainDesc.Desert:
                sprite = desert;
                break;
            case TerrainDesc.GrasslandHill:
                sprite = grasslandHill;
                break;
            case TerrainDesc.DesertHill:
                sprite = desertHill;
                break;
            case TerrainDesc.GrasslandFloodPlain:
                sprite = grasslandFloodPlain;
                break;
            case TerrainDesc.DesertFloodPlain:
                sprite = desertFloodPlain;
                break;
            case TerrainDesc.Forest:
                sprite = forest;
                break;
            case TerrainDesc.ForestHill:
                sprite = forestHill;
                break;
            case TerrainDesc.Jungle:
                sprite = jungle;
                break;
            case TerrainDesc.JungleHill:
                sprite = jungleHill;
                break;
            case TerrainDesc.Swamp:
                sprite = swamp;
                break;
            case TerrainDesc.Mountain:
                sprite = mountain;
                break;
            case TerrainDesc.Sea:
                sprite = sea;
                break;
            case TerrainDesc.River:
                sprite = sea;
                break;
            case TerrainDesc.City:
                sprite = city;
                break;
            case TerrainDesc.Wonder:
                sprite = wonder;
                break;
            case TerrainDesc.TradeCenter:
                sprite = tradeCenter;
                break;
        }

        return sprite;
    }

    private (Sprite, Vector3, Vector2, int) GetRoad(int i)
    {
        Sprite road = roadSolo;
        Vector3 shift = Vector3.zero;
        Vector2 size = Vector2.zero;
        int rotation = 0;

        switch (i)
        {
            case 0:
                road = roadSolo;
                size = new Vector2(20, 20);
                break;
            case 1:
                road = roadStraight;
                shift = new Vector3(0, 13.5f, 0);
                size = new Vector2(48, 21);
                rotation = 270;
                break;
            case 2:
                road = roadDiagonal;
                shift = new Vector3(23f, 13.5f, 0);
                size = new Vector2(48, 48);
                rotation = 180;
                break;
            case 3:
                road = roadStraight;
                shift = new Vector3(13.5f, 0, 0);
                size = new Vector2(48, 21);
                rotation = 180;
                break;
            case 4:
                road = roadDiagonal;
                shift = new Vector3(23f, -13.5f, 0);
                size = new Vector2(48, 48);
                rotation = 270;
                break;
            case 5:
                road = roadStraight;
                shift = new Vector3(0, -13.5f, 0);
                size = new Vector2(48, 21);
                rotation = 90;
                break;
            case 6:
                road = roadDiagonal;
                shift = new Vector3(-23f, -13.5f, 0);
                size = new Vector2(48, 48);
                break;
            case 7:
                road = roadStraight;
                shift = new Vector3(-13.5f, 0, 0);
                size = new Vector2(48, 21);
                break;
            case 8:
                road = roadDiagonal;
                shift = new Vector3(-23f, 13.5f, 0);
                size = new Vector2(48, 48);
                rotation = 90;
                break;
        }

        return (road, shift, size, rotation);
    }

    public TMP_Text CreateCityText(Vector3Int loc, string text)
    {
        GameObject cityTextGO = Instantiate(mapCityText);
        cityTextGO.gameObject.transform.SetParent(cityNameHolder, false);
        TMP_Text cityText = cityTextGO.GetComponent<TMP_Text>();
        cityText.text = text;
        cityText.outlineWidth = 0.35f;
        cityText.outlineColor = Color.black;
        Vector3 textLoc = mapLocDict[loc];
        textLoc.y -= grid.cellSize.x * 0.5f;
        cityText.transform.localPosition = textLoc;

        return cityText;
    }

    public void SetIconTile(Vector3Int loc, GameObject icon)
    {
        icon.transform.localPosition = mapLocDict[loc];
    }

    public void SetRoad(Vector3Int loc, int num)
    {
        GameObject roadGO = Instantiate(mapPanelImage);
        roadGO.gameObject.transform.SetParent(roadHolder, false);
        (Sprite sprite, Vector3 shift, Vector2 size, int rotation) = GetRoad(num);
        roadGO.GetComponent<RectTransform>().sizeDelta = size;
        Vector3 pos = mapLocDict[loc];
        roadGO.transform.localEulerAngles = new Vector3Int(0, 0, rotation);
        roadGO.transform.localPosition = pos + shift;
        roadGO.GetComponent<Image>().sprite = sprite;

        if (!roadImageDict.ContainsKey(loc))
            roadImageDict[loc] = new GameObject[9];

        roadImageDict[loc][num] = roadGO;

        if (num > 0)
            ClearSoloRoad(loc);
        //mapTileDict[loc].TurnOnRoad(num);
    }

    public void RemoveRoad(Vector3Int loc, int num)
    {
        GameObject road = roadImageDict[loc][num];
        Destroy(road);
        roadImageDict[loc][num] = null;

        if (roadImageDict[loc].Count(x => x != null) == 0)
            SetRoad(loc, 0); //adding solo road if there's none left
        //mapTileDict[loc].TurnOffRoad(num);
    }

    public void RemoveAllRoads(Vector3Int loc)
    {
        for (int i = 0; i < 9; i++)
        {
            if (roadImageDict[loc][i] != null)
            {
                Destroy(roadImageDict[loc][i]);
                roadImageDict[loc][i] = null;
            }
        }

        //foreach (GameObject road in roadImageDict[loc])
        //{
        //    if (road != null)
        //        Destroy(road);
        //}

        roadImageDict.Remove(loc);
        //mapTileDict[loc].TurnOffAllRoads();
    }

    private void ClearSoloRoad(Vector3Int loc)
    {
        if (roadImageDict[loc][0] != null)
        {
            Destroy(roadImageDict[loc][0]);
            roadImageDict[loc][0] = null;
        }
    }

    public GameObject CreateUnitIcon(Sprite sprite)
    {
        GameObject unitIconGO = Instantiate(mapUnitIcon);
        unitIconGO.gameObject.transform.SetParent(unitHolder, false);
        //unitIconGO.transform.SetAsLastSibling();
        Image unitIcon = unitIconGO.GetComponent<Image>();
        unitIcon.sprite = sprite;
        return unitIconGO;
    }

    public void CenterCamera(Vector3 location)
    {
        cameraController.CenterCameraInstantly(location);
        ToggleVisibility(false);
    }

    public void CloseMapPanel()
    {
        ToggleVisibility(false);
    }

    private void HandleKeyboardInput()
    {
        if (!activeStatus)
            return;

        //assigning keys
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            newPosition -= Vector3.up * movementSpeed; //up
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            newPosition += Vector3.right * movementSpeed; //left
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            newPosition += Vector3.up * movementSpeed; //down
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            newPosition -= Vector3.right * movementSpeed; //right
        }

        MoveMap();
    }

    private void Zoom()
    {
        if (!activeStatus)
            return;
        
        if (Input.mouseScrollDelta.y != 0)
        {
            newPosition += Input.mouseScrollDelta.y * new Vector3(0,0,-30);
        }

        newPosition.z = Mathf.Clamp(newPosition.z, -700, 80);

        mainRect.localPosition = Vector3.Lerp(mainRect.localPosition, newPosition, Time.deltaTime * movementTime);
        //transform.localPosition = Vector3.Lerp(transform.localPosition, newPosition, Time.deltaTime * movementTime);
    }

    private void MoveMap()
    {
        //Clamps in ToggleVisibility method as well
        newPosition.x = Mathf.Clamp(newPosition.x, -280f, 280);
        newPosition.y = Mathf.Clamp(newPosition.y, -240f, 240f);

        mainRect.localPosition = Vector3.Lerp(mainRect.localPosition, newPosition, Time.deltaTime * movementTime);
        //transform.localPosition = Vector3.Lerp(transform.localPosition, newPosition, Time.deltaTime * movementTime);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(mainRect, Input.mousePosition, Camera.main, out Vector2 localPoint))
        {
            Vector3 centerPoint = localPoint;
            centerPoint.x += -grid.cellSize.x * 0.5f;
            centerPoint.y += grid.cellSize.x * 0.5f;
            centerPoint.z = centerPoint.y;
            centerPoint.y = 0;
            centerPoint /= grid.cellSize.x;
            centerPoint *= world.Increment;

            CenterCamera(centerPoint);
        }
    }
}

