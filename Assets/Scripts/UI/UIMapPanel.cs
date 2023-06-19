using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class UIMapPanel : MonoBehaviour
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
    private GameObject mapPanelTile;

    [SerializeField]
    private GridLayoutGroup grid;

    //[SerializeField]
    //private Tile grassland, desert, grasslandHill, desertHill, grasslandFloodPlain, desertFloodPlain, forest, forestHill, jungle, jungleHill, swamp, mountain, sea, river, undiscovered;

    private Vector3 newPosition;
    //private Vector3 zoomAmount = new Vector3(0, 0, -0.3f);
    public float movementSpeed = 1, movementTime, zoomTime;

    public int mapWidth = 18, mapHeight = 13, widthMin = -21, heightMin = -15;

    //[SerializeField] //for tweening
    //private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus;
    //private Vector3 originalLoc;

    private void Awake()
    {
        //CreateMap();
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

    public void CreateMap()
    {
        int increment = world.Increment;

        for (int z = mapHeight - 1; z >= 0; z--)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                Vector3Int tileCoordinates = new Vector3Int(widthMin + x * increment, 0 , heightMin + z * increment);
                TerrainData td = world.GetTerrainDataAt(tileCoordinates);
                GameObject tile = Instantiate(mapPanelTile);

                UIMapPanelTile panelTile = tile.GetComponent<UIMapPanelTile>();
                panelTile.SetMapPanel(this);
                panelTile.SetTile(td.GetTileCoordinates(), td.terrainData.terrainDesc);
                panelTile.gameObject.transform.SetParent(grid.transform, false);
                
                if (td.terrainData.resourceType != ResourceType.Food && td.terrainData.resourceType != ResourceType.None && td.terrainData.resourceType != ResourceType.Lumber && td.terrainData.resourceType != ResourceType.Fish)
                    panelTile.SetResource(ResourceHolder.Instance.GetIcon(td.terrainData.resourceType));
            }
        }

        mainRect.sizeDelta = new Vector2(mapWidth * 50, mapHeight * 50);
        grid.constraintCount = mapWidth;
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

            newPosition = new Vector3(0, 0, -800f);
            mainRect.localPosition = new Vector3(0, 0, -800f);
            //allContents.anchoredPosition3D = originalLoc + new Vector3(0, 1200f, 0);

            //LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -1200f, 0.5f).setEaseOutSine();
            //LeanTween.alpha(allContents, 1f, 0.5f).setFrom(0f).setEaseLinear();
        }
        else
        {
            activeStatus = false;
            uiUnitTurn.gameObject.SetActive(true);
            gameObject.SetActive(v);

            //LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 1200f, 0.3f).setOnComplete(SetActiveStatusFalse);
        }

        cameraController.enabled = !v;
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

    public void CenterCamera(Vector3Int location)
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

        newPosition.z = Mathf.Clamp(newPosition.z, -1000, 80);

        mainRect.localPosition = Vector3.Lerp(mainRect.localPosition, newPosition, Time.deltaTime * movementTime);
        //transform.localPosition = Vector3.Lerp(transform.localPosition, newPosition, Time.deltaTime * movementTime);
    }

    private void MoveMap()
    {
        newPosition.x = Mathf.Clamp(newPosition.x, -280f, 280);
        newPosition.y = Mathf.Clamp(newPosition.y, -240f, 240f);

        mainRect.localPosition = Vector3.Lerp(mainRect.localPosition, newPosition, Time.deltaTime * movementTime);
        //transform.localPosition = Vector3.Lerp(transform.localPosition, newPosition, Time.deltaTime * movementTime);
    }
}

