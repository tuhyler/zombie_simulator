using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static UnityEditor.PlayerSettings;
using TMPro;

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
    private Transform unitHolder, cityNameHolder;

    [SerializeField]
    private Image panel;

    [SerializeField]
    private GameObject mapPanelTile, mapUnitIcon, mapCityText;

    [SerializeField]
    private GridLayoutGroup grid;

    //[SerializeField]
    //private SpriteRenderer workerIcon;

    private Dictionary<Vector3Int, UIMapPanelTile> mapTileDict = new();
    private Dictionary<Vector3Int, Vector3> mapLocDict = new();

    [SerializeField]
    private Sprite grassland, desert, grasslandHill, desertHill, grasslandFloodPlain, desertFloodPlain, forest, forestHill, jungle, jungleHill, mountain, swamp, sea, undiscovered, city, wonder, tradeCenter;

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
                SetDictLocValue(tileCoordinates);

                foreach (Vector3Int neighbor in world.GetNeighborsCoordinates(MapWorld.State.EIGHTWAY))
                {
                    Vector3Int newTile = neighbor + tileCoordinates;
                    SetDictLocValue(newTile);
                }
            }
        }

        mainRect.sizeDelta = new Vector2(mapWidth * grid.cellSize.x, mapHeight * grid.cellSize.y);
        gridRect.sizeDelta = new Vector2(mapWidth * grid.cellSize.x, mapHeight * grid.cellSize.y);
        grid.constraintCount = mapWidth;
    }

    private void SetDictLocValue(Vector3Int pos)
    {
        Vector3 loc = pos;
        loc *= grid.cellSize.x / 3;
        loc.x += grid.cellSize.x * 0.5f;
        loc.z += -grid.cellSize.x * 0.5f;
        loc.y = loc.z;
        loc.z = 0;

        mapLocDict[pos] = loc;
    }

    public void AddTileToMap(Vector3Int loc)
    {
        TerrainData td = world.GetTerrainDataAt(loc);
        mapTileDict[loc].SetTile(GetSprite(td.terrainData.terrainDesc));

        if (td.terrainData.resourceType != ResourceType.Food && td.terrainData.resourceType != ResourceType.None && td.terrainData.resourceType != ResourceType.Lumber && td.terrainData.resourceType != ResourceType.Fish)
            mapTileDict[loc].SetResource(ResourceHolder.Instance.GetIcon(td.terrainData.resourceType));
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
            activeStatus = false;
            world.showingMap = false;
            uiUnitTurn.gameObject.SetActive(true);
            gameObject.SetActive(v);
            //transform.position = originalLoc;

            //LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 1200f, 0.3f).setOnComplete(SetActiveStatusFalse);
        }

        cameraController.enabled = !v;
    }

    public void SetImprovement(Vector3Int pos, Sprite sprite)
    {
        mapTileDict[pos].SetImprovement(sprite);
    }

    public void RemoveImprovement(Vector3Int pos)
    {
        mapTileDict[pos].RemoveImprovement();
    }

    public void SetTileSprite(Vector3Int pos, TerrainDesc terrainDesc)
    {
        mapTileDict[pos].SetTile(GetSprite(terrainDesc));
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

    //public void MoveWorker(Vector3Int newLoc, float movement)
    //{
    //    workerIcon.transform.position = Vector3.MoveTowards(workerIcon.transform.position, mapLocDict[newLoc], movement);
    //}

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

