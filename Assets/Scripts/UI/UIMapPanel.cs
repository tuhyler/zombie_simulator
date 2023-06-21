using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIMapPanel : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    private MapWorld world;

    [SerializeField]
    private UIUnitTurnHandler uiUnitTurn;

    [SerializeField]
    private CameraController cameraController;

    //[SerializeField]
    //private Tilemap tilemap;

    //private Grid grid;

    [SerializeField]
    private RectTransform mainRect;

    [SerializeField]
    private Image panel;

    [SerializeField]
    private GameObject mapPanelTile;

    [SerializeField]
    private GridLayoutGroup grid;

    [SerializeField]
    private GridLayout gridLayout;

    [SerializeField]
    private ParticleSystem workerIcon;

    private Dictionary<Vector3Int, UIMapPanelTile> mapDict = new();

    [SerializeField]
    private Sprite grassland, desert, grasslandHill, desertHill, grasslandFloodPlain, desertFloodPlain, forest, forestHill, jungle, jungleHill, mountain, swamp, sea, undiscovered;

    //[SerializeField]
    //private Tile grassland, desert, grasslandHill, desertHill, grasslandFloodPlain, desertFloodPlain, forest, forestHill, jungle, jungleHill, swamp, mountain, sea, river, undiscovered;

    private Vector3 newPosition;
    //private Vector3 zoomAmount = new Vector3(0, 0, -0.3f);
    public float movementSpeed = 1, movementTime, zoomTime;

    public int mapWidth = 50, mapHeight = 50, offset = 25; //offset if tile values are less than 0 on map

    //[SerializeField] //for tweening
    //private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus;
    //private Vector3 originalLoc;

    private void Awake()
    {
        CreateDictionary();

        if (Screen.height > 1080)
            workerIcon.transform.localScale = new Vector3(50f, 50f, 50f);
        else if (Screen.height < 1080)
            workerIcon.transform.localScale = new Vector3(70f, 70f, 70f);
        //grid = GetComponent<Grid>();

        newPosition = transform.localPosition; //set static position that doesn't default to 0

        //originalLoc = allContents.anchoredPosition3D;
        gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        HandleKeyboardInput();
        Zoom();
    }

    public void CreateDictionary()
    {
        int increment = world.Increment;

        for (int z = mapHeight; z >= 0; z--)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                Vector3Int tileCoordinates = new Vector3Int(-offset * increment + x * increment, 0, -offset * increment + z * increment);
                GameObject tile = Instantiate(mapPanelTile);
                UIMapPanelTile panelTile = tile.GetComponent<UIMapPanelTile>();
                panelTile.SetMapPanel(this);
                panelTile.SetTile(tileCoordinates, undiscovered, world.Increment);
                panelTile.gameObject.transform.SetParent(grid.transform, false);
                mapDict[tileCoordinates] = panelTile;
            }
        }

        mainRect.sizeDelta = new Vector2(mapWidth * 75, mapHeight * 75);
        grid.constraintCount = mapWidth;
    }

    //private void CreateMap()
    //{
    //    for (int x = 0; x < mapWidth; x++)
    //    {
    //        for (int y = 0; y < mapHeight; y++)
    //        {
    //            Vector3Int loc = new Vector3Int(x-25, y-25, 0);
    //            tilemap.SetTile(loc, undiscovered);
    //        }
    //    }
    //}

    //public void CreateMap()
    //{


    //    for (int z = mapHeight - 1; z >= 0; z--)
    //    {
    //        for (int x = 0; x < mapWidth; x++)
    //        {
    //            TerrainData td = world.GetTerrainDataAt(tileCoordinates);
    //            GameObject tile = Instantiate(mapPanelTile);

    //            UIMapPanelTile panelTile = tile.GetComponent<UIMapPanelTile>();
    //            panelTile.SetMapPanel(this);
    //            panelTile.SetTile(td.GetTileCoordinates(), td.terrainData.terrainDesc);
    //            panelTile.gameObject.transform.SetParent(grid.transform, false);
                
    //            if (td.terrainData.resourceType != ResourceType.Food && td.terrainData.resourceType != ResourceType.None && td.terrainData.resourceType != ResourceType.Lumber && td.terrainData.resourceType != ResourceType.Fish)
    //                panelTile.SetResource(ResourceHolder.Instance.GetIcon(td.terrainData.resourceType));
    //        }
    //    }

    //    mainRect.sizeDelta = new Vector2(mapWidth * 50, mapHeight * 50);
    //    grid.constraintCount = mapWidth;
    //}

    public void AddTileToMap(Vector3Int loc)
    {
        TerrainData td = world.GetTerrainDataAt(loc);
        mapDict[loc].SetTile(td.GetTileCoordinates(), GetSprite(td.terrainData.terrainDesc), world.Increment);
        //mapDict[loc].TurnOnRayCast();

        if (td.terrainData.resourceType != ResourceType.Food && td.terrainData.resourceType != ResourceType.None && td.terrainData.resourceType != ResourceType.Lumber && td.terrainData.resourceType != ResourceType.Fish)
            mapDict[loc].SetResource(ResourceHolder.Instance.GetIcon(td.terrainData.resourceType));
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
        }

        return sprite;
    }

    //public void SetTile(Vector3Int loc, TerrainDesc desc)
    //{
    //    Tile tile = null;

    //    switch (desc)
    //    {
    //        case TerrainDesc.Grassland:
    //            tile = grassland;
    //            break;
    //        case TerrainDesc.Desert:
    //            tile = desert;
    //            break;
    //        case TerrainDesc.GrasslandHill:
    //            tile = grasslandHill;
    //            break;
    //        case TerrainDesc.DesertHill:
    //            tile = desertHill;
    //            break;
    //        case TerrainDesc.GrasslandFloodPlain:
    //            tile = grasslandFloodPlain;
    //            break;
    //        case TerrainDesc.DesertFloodPlain:
    //            tile = desertFloodPlain;
    //            break;
    //        case TerrainDesc.Forest:
    //            tile = forest;
    //            break;
    //        case TerrainDesc.ForestHill:
    //            tile = forestHill;
    //            break;
    //        case TerrainDesc.Jungle:
    //            tile = jungle;
    //            break;
    //        case TerrainDesc.JungleHill:
    //            tile = jungleHill;
    //            break;
    //        case TerrainDesc.Swamp:
    //            tile = swamp;
    //            break;
    //        case TerrainDesc.Mountain:
    //            tile = mountain;
    //            break;
    //        case TerrainDesc.Sea:
    //            tile = sea;
    //            break;
    //        case TerrainDesc.River:
    //            tile = river;
    //            break;
    //    }

    //    tilemap.SetTile(loc, tile);
    //}

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
            world.somethingSelected = true;

            activeStatus = true;

            Vector3 loc = cameraController.transform.position;
            loc /= world.Increment;
            loc.x *= -75f;
            loc.z *= -100f;
            loc.x = Mathf.Clamp(loc.x, -280f, 280);
            loc.z = Mathf.Clamp(loc.z, -240f, 240f);

            newPosition = new Vector3(loc.x, loc.z, -700f);
            mainRect.localPosition = new Vector3(loc.x, loc.z, -700f);

            FindWorker();
            workerIcon.Play();
            //allContents.anchoredPosition3D = originalLoc + new Vector3(0, 1200f, 0);

            //LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -1200f, 0.5f).setEaseOutSine();
            //LeanTween.alpha(allContents, 1f, 0.5f).setFrom(0f).setEaseLinear();
        }
        else
        {
            workerIcon.Pause();
            activeStatus = false;
            uiUnitTurn.gameObject.SetActive(true);
            gameObject.SetActive(v);

            //LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 1200f, 0.3f).setOnComplete(SetActiveStatusFalse);
        }

        cameraController.enabled = !v;
    }

    public void FindWorker()
    {
        Worker worker = FindObjectOfType<Worker>();
        //Vector3 loc = world.GetClosestTerrainLoc(worker.transform.position) / world.Increment;
        //loc *= 75;
        //loc.x += 37.5f;
        //loc.z += -37.5f;
        //loc.y = loc.z;
        //loc.z = 0;

        workerIcon.transform.localPosition = mapDict[world.GetClosestTerrainLoc(worker.transform.position)].localCoordinates;
        //workerIcon.Play();
    }

    //private void SetActiveStatusFalse()
    //{
    //    gameObject.SetActive(false);
    //}


    //private void Ugh()
    //{
    //    // save the camera as public field if you using not the main camera

    //    //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //    Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //    //worldPoint.z = 935;
    //    Vector3 position = tilemap.WorldToCell(worldPoint);
    //    Debug.Log(position * world.Increment);
    //    // get the collision point of the ray with the z = 0 plane
    //    //Vector3 worldPoint = ray.GetPoint(-ray.origin.z / ray.direction.z);
    //    //Vector3Int position = grid.WorldToCell(worldPoint);
    //}

    //public void HandleMapClick(Vector3 location, GameObject selectedObject)
    //{
    //    if (!activeStatus)
    //        return;

    //    Vector3Int position = grid.WorldToCell(location);
    //    position.z = position.y;
    //    position.y = 0;
    //    Debug.Log(position * world.Increment);
    //    cameraController.CenterCameraInstantly(position * world.Increment);
    //    ToggleVisibility(false);
    //}

    public void CenterCamera(Vector3 location)
    {
        cameraController.CenterCameraInstantly(location);
        ToggleVisibility(false);
    }

    //public void CloseMapPanel()
    //{
    //    ToggleVisibility(false);
    //}

    private void HandleKeyboardInput()
    {
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
            centerPoint.x += -37.5f;
            centerPoint.y += 37.5f;
            centerPoint.z = centerPoint.y;
            centerPoint.y = 0;
            centerPoint /= 75f;
            centerPoint *= world.Increment;

            CenterCamera(centerPoint);
        }
    }
}

