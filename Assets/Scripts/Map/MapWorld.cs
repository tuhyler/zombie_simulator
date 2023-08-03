using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.Rendering.DebugUI;

public class MapWorld : MonoBehaviour
{
    //public float test = 0.4f;
    [SerializeField]
    public Canvas immoveableCanvas, cityCanvas, workerCanvas, traderCanvas, tradeRouteManagerCanvas, infoPopUpCanvas, overflowGridCanvas;
    [SerializeField]
    public MeshFilter borderOne, borderTwoCorner, borderTwoCross, borderThree, borderFour;
    [SerializeField]
    private UIWorldResources uiWorldResources;
    [SerializeField]
    private UIResearchTreePanel researchTree;
    [SerializeField]
    private UIMapHandler mapHandler;
    //[SerializeField]
    //private UIMapPanel mapPanel;
    [SerializeField]
    private UISingleConditionalButtonHandler wonderButton, uiConfirmWonderBuild, uiRotateWonder;
    [SerializeField]
    private RectTransform mapPanelButton, mainMenuButton;
    [SerializeField]
    private UIWonderHandler wonderHandler;
    [SerializeField]
    private UIBuildingSomething uiBuildingSomething;
    [SerializeField]
    private UITerrainTooltip uiTerrainTooltip;
    [SerializeField]
    private UICityImprovementTip uiCityImprovementTip;
    [SerializeField]
    public UITomFinder uiTomFinder;
    [SerializeField]
    public UIInfoPopUpHandler uiInfoPopUpHandler;
    [SerializeField]
    private UnitMovement unitMovement;
    [SerializeField]
    public CityBuilderManager cityBuilderManager;
    [SerializeField]
    public RoadManager roadManager;
    [SerializeField]
    private Material transparentMat;

    [SerializeField]
    private ParticleSystem lightBeam;

    [SerializeField]
    public Transform cityHolder, wonderHolder, tradeCenterHolder;

    //wonder info
    private WonderDataSO wonderData;
    [SerializeField]
    private UnityEvent<WonderDataSO> OnIconButtonClick;
    [SerializeField]
    private Sprite wonderMapIcon;
    private List<Vector3Int> wonderPlacementLoc = new();
    private List<Vector3Int> wonderNoWalkLoc = new();
    private int rotationCount;
    private Vector3Int unloadLoc;
    private Vector3Int finalUnloadLoc;
    private Quaternion rotation;
    private bool sideways;
    private Dictionary<string, Wonder> wonderConstructionDict = new();
    private Dictionary<Vector3Int, Wonder> wonderStopDict = new();
    private GameObject wonderGhost;

    //trade center info
    [SerializeField]
    private Sprite tradeCenterMapIcon;
    private Dictionary<string, TradeCenter> tradeCenterDict = new();
    private Dictionary<Vector3Int, TradeCenter> tradeCenterStopDict = new();
    private List<string> tradeCenterNamePool = new();
    private List<int> tradeCenterPopPool = new();

    //world resource info
    private WorldResourceManager worldResourceManager;
    [HideInInspector]
    public bool researching;
    private List<City> researchWaitList = new();
    private List<City> goldCityWaitList = new();
    private List<Wonder> goldWonderWaitList = new();
    private List<TradeCenter> goldTradeCenterWaitList = new();

    private Dictionary<Vector3Int, TerrainData> world = new();
    private Dictionary<Vector3Int, GameObject> buildingPosDict = new(); //to see if cities already exist in current location
    private List<Vector3Int> noWalkList = new(); //tiles where wonders are and units can't walk
    private List<Vector3Int> cityLocations = new();
    private List<GameObject> cityNamesMaps = new();

    private Dictionary<Vector3Int, City> cityDict = new(); //caching cities for easy reference
    private Dictionary<Vector3Int, string> tradeLocDict = new(); //cities and the respective locations of their harbors
    private Dictionary<Vector3Int, CityImprovement> cityImprovementDict = new(); //all the City development prefabs
    private Dictionary<Vector3Int, CityImprovement> cityImprovementConstructionDict = new();
    private Dictionary<Vector3Int, Dictionary<string, CityImprovement>> cityBuildingDict = new(); //all the buildings for highlighting
    private Dictionary<Vector3Int, Dictionary<string, GameObject>> cityBuildingGODict = new(); //all the buildings and info within a city 
    private List<Vector3Int> cityImprovementQueueList = new();
    private List<Vector3Int> unclaimedSingleBuildList = new();
    private Dictionary<string, Vector3Int> cityNameDict = new();
    private Dictionary<Vector3Int, string> cityLocDict = new();
    private Dictionary<Vector3Int, Unit> unitPosDict = new(); //to track unitGO locations
    private Dictionary<string, ImprovementDataSO> improvementDataDict = new();
    private Dictionary<string, UnitBuildDataSO> unitBuildDataDict = new();
    private Dictionary<string, int> upgradeableObjectMaxLevelDict = new();
    private Dictionary<string, List<ResourceValue>> upgradeableObjectPriceDict = new(); 
    private Dictionary<string, ImprovementDataSO> upgradeableObjectDataDict = new();
    private Dictionary<ResourceType, Sprite> resourceSpriteDict = new();
    private Dictionary<ResourceType, int> defaultResourcePriceDict = new();
    private Dictionary<ResourceType, int> blankResourceDict = new();
    private Dictionary<ResourceType, bool> boolResourceDict = new();
    private Dictionary<Vector3Int, GameObject> queueGhostsDict = new(); //for displaying samples for queued items
    //private Dictionary<Vector3Int, GameObject> traderPosDict = new(); //to track trader locations 
    //private Dictionary<Vector3Int, List<GameObject>> multiUnitPosDict = new(); //to handle multiple units in one spot

    //for assigning labor in cities
    private Dictionary<Vector3Int, int> currentWorkedTileDict = new(); //to see how much labor is assigned to tile
    private Dictionary<Vector3Int, int> maxWorkedTileDict = new(); //the max amount of labor that can be assigned to tile
    private Dictionary<Vector3Int, GameObject> cityWorkedTileDict = new(); //the city worked tiles belong to
    private Dictionary<Vector3Int, ResourceProducer> cityImprovementProducerDict = new(); //all the improvements that have resource producers (for speed)
    private Dictionary<Vector3Int, Dictionary<string, int>> cityBuildingCurrentWorkedDict = new(); //current worked for buildings in city

    //private Dictionary<Vector3Int, Dictionary<string, int>> cityBuildingMaxWorkedDict = new(); //max labor of buildings within city
    private Dictionary<Vector3Int, List<string>> cityBuildingList = new(); //list of buildings on city tiles (here instead of City because buildings can be without a city)
    //private Dictionary<Vector3Int, Dictionary<string, ResourceProducer>> cityBuildingIsProducer = new(); //all the buildings that are resource producers (for speed)

    //for workers
    private List<Vector3Int> workerBusyLocations = new();

    //for roads
    private Dictionary<Vector3Int, List<Road>> roadTileDict = new(); //stores road GOs, only on terrain locations
    private List<Vector3Int> soloRoadLocsList = new(); //indicates which tiles have solo roads on them
    private List<Vector3Int> roadLocsList = new(); //indicates which tiles have roads on them
    private int roadCost; //set in road manager

    //for terrain speeds
    public TerrainDataSO flatland, forest, hill, forestHill;
    //for boats to avoid traveling the coast
    private List<Vector3Int> coastCoastList = new();

    //for expanding gameobject size
    private static int increment = 3;
    public int Increment { get { return increment; } }

    private SpeechBubbleHandler speechBubble;

    public bool showGizmo, hideTerrain = true;

    [HideInInspector]
    public bool workerOrders, buildingWonder, tooltip, somethingSelected, showingMap;
    //private bool showObstacle, showDifficult, showGround, showSea;

    //for naming of units
    [HideInInspector]
    public int workerCount, infantryCount;

    private void Awake()
    {
        if (!hideTerrain)
            cityBuilderManager.focusCam.SetDefaultLimits();
        
        worldResourceManager = GetComponent<WorldResourceManager>();
        worldResourceManager.SetUI(uiWorldResources);
        if (researching)
            SetResearchName(researchTree.GetChosenResearchName());
        else
            SetResearchName("No Research");
        GameObject speechBubbleGO = Instantiate(GameAssets.Instance.speechBubble);
        speechBubble = speechBubbleGO.GetComponent<SpeechBubbleHandler>();
        speechBubble.gameObject.SetActive(false);

        uiInfoPopUpHandler.SetWarningMessage(uiInfoPopUpHandler);
        uiInfoPopUpHandler.gameObject.SetActive(false);

        tradeCenterNamePool.Add("Indus Valley");
        tradeCenterNamePool.Add("Trade_Center_2");
        tradeCenterNamePool.Add("Trade_Center_3");
        tradeCenterNamePool.Add("Trade_Center_4");
        tradeCenterNamePool.Add("Trade_Center_5");

        tradeCenterPopPool.Add(5);
        tradeCenterPopPool.Add(4);
        tradeCenterPopPool.Add(7);
        tradeCenterPopPool.Add(8);
        tradeCenterPopPool.Add(6);
    }

    private void Start()
    {
        wonderButton.gameObject.SetActive(true);
        uiWorldResources.SetActiveStatus(true);
        List<TerrainData> coastalTerrain = new();
        List<TerrainData> terrainToCheck = new();

        foreach (TerrainData td in FindObjectsOfType<TerrainData>())
        {
            if (td.IsSeaCorner && !coastalTerrain.Contains(td))
                coastalTerrain.Add(td);
            td.SetTileCoordinates(this);
            Vector3Int tileCoordinate = td.TileCoordinates;
            world[tileCoordinate] = td;
            terrainToCheck.Add(td);
            td.CheckMinimapResource(mapHandler);
            if (!hideTerrain && td.hasResourceMap)
                td.resourceIcon.SetActive(true);
            if (td.hasResourceMap)
                td.PrepParticleSystem();
            //mapPanel.AddTileToMap(tileCoordinate);

            //Vector3Int mod = tileCoordinate / increment;
            //mod.y = mod.z;
            //mod.z = 0;
            //mapPanel.SetTile(mod, td.GetTerrainData().terrainDesc);

            foreach (Vector3Int tile in neighborsEightDirections)
            {
                world[tileCoordinate + tile] = td;
            }
        }

        foreach (TerrainData td in coastalTerrain)
            td.SetCoastCoordinates(this);

        foreach (TerrainData td in terrainToCheck)
        {
            //ConfigureUVs(td);
            if (hideTerrain)
                td.Hide();
            else
                td.Discover();
        }

        foreach (Unit unit in FindObjectsOfType<Unit>()) //adds all units and their locations to start game.
        {
            unit.SetReferences(this, cityBuilderManager.focusCam, cityBuilderManager.uiUnitTurn, cityBuilderManager.movementSystem);
            unit.SetMinimapIcon(cityBuilderManager.friendlyUnitHolder);
            unit.Reveal();
            Vector3 unitPos = unit.transform.position;
            if (!unitPosDict.ContainsKey(Vector3Int.RoundToInt(unitPos))) //just in case dictionary was missing any
                unit.CurrentLocation = AddUnitPosition(unitPos, unit);
        }

        string upgradeableObjectName = "";
        List<ResourceValue> upgradeableObjectTotalCost = new();
        int upgradeableObjectLevel = 9999;

        foreach (ImprovementDataSO data in UpgradeableObjectHolder.Instance.allBuildingsAndImprovements)
        {
            improvementDataDict[data.improvementName + "-" + data.improvementLevel] = data;
            
            if (data.availableInitially)
                data.Locked = false;
            else
                data.Locked = true;
            upgradeableObjectMaxLevelDict[data.improvementName] = 1;

            if (upgradeableObjectLevel < data.improvementLevel) //skip if reached max level
            {
                upgradeableObjectDataDict[upgradeableObjectName] = data; //adding the data necessary to upgrade the object to
                
                //calculating costs to improve
                Dictionary<ResourceType, int> prevResourceCosts = new(); //making dict to more easily find the data
                List<ResourceValue> upgradeableObjectCost = new();

                foreach (ResourceValue prevResourceValue in upgradeableObjectTotalCost)
                {
                    prevResourceCosts[prevResourceValue.resourceType] = prevResourceValue.resourceAmount;
                }

                foreach (ResourceValue resourceValue in data.improvementCost)
                {
                    if (prevResourceCosts.ContainsKey(resourceValue.resourceType))
                    {
                        ResourceValue newResourceValue;
                        newResourceValue.resourceType = resourceValue.resourceType;
                        newResourceValue.resourceAmount = resourceValue.resourceAmount - prevResourceCosts[resourceValue.resourceType];
                        if (newResourceValue.resourceAmount > 0)
                            upgradeableObjectCost.Add(newResourceValue);
                    }
                    else //if it doesn't have the resourceType, then add the whole thing
                    {
                        upgradeableObjectCost.Add(resourceValue);
                    }
                }

                upgradeableObjectPriceDict[upgradeableObjectName] = upgradeableObjectCost;
            }

            upgradeableObjectName = data.improvementName + "-" + data.improvementLevel; //needs to be last to compare to following data
            upgradeableObjectTotalCost = data.improvementCost;
            upgradeableObjectLevel = data.improvementLevel;
        }

        //populating the upgradeableobjectdict, every one starts at level 1. 
        foreach (UnitBuildDataSO data in UpgradeableObjectHolder.Instance.allUnits)
        {
            unitBuildDataDict[data.unitName + "-" + data.unitLevel] = data;
            
            if (data.availableInitially)
                data.locked = false;
            else
                data.locked = true;
            upgradeableObjectMaxLevelDict[data.unitName] = 1;
        }

        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
        {
            resourceSpriteDict[resource.resourceType] = resource.resourceIcon;
            defaultResourcePriceDict[resource.resourceType] = resource.resourcePrice;
            blankResourceDict[resource.resourceType] = 0;
            boolResourceDict[resource.resourceType] = false;
        }

        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allWorldResources)
        {
            resourceSpriteDict[resource.resourceType] = resource.resourceIcon;
        }

        int i = 0;
        foreach(TradeCenter center in FindObjectsOfType<TradeCenter>())
        {
            center.SetWorld(this);
            center.SetName(tradeCenterNamePool[i]);
            center.SetPop(tradeCenterPopPool[i]);
            center.ClaimSpotInWorld(increment);
            //mapPanel.SetImprovement(center.mainLoc, tradeCenterMapIcon);
            //mapPanel.SetTileSprite(center.mainLoc, TerrainDesc.TradeCenter);
            //roadManager.BuildRoadAtPosition(center.mainLoc);
            tradeCenterDict[center.tradeCenterName] = center;
            tradeCenterStopDict[center.mainLoc] = center;
            AddTradeLoc(center.mainLoc, center.tradeCenterName);
            AddTradeLoc(center.harborLoc, center.tradeCenterName);
            center.Hide();
            i++;
        }

        CreateParticleSystems();
        DeactivateCanvases();
    }

    private void DeactivateCanvases()
    {
        immoveableCanvas.gameObject.SetActive(false);
        cityCanvas.gameObject.SetActive(false);
        traderCanvas.gameObject.SetActive(false);
        workerCanvas.gameObject.SetActive(false);
        tradeRouteManagerCanvas.gameObject.SetActive(false);
        infoPopUpCanvas.gameObject.SetActive(false);
        overflowGridCanvas.gameObject.SetActive(false);
    }

    public void ConfigureUVs(TerrainData td)
    {
        TerrainDesc desc = td.terrainData.terrainDesc;
        int[] grasslandCount = new int[4];
        int i = 0;

        if (desc == TerrainDesc.Grassland || desc == TerrainDesc.GrasslandFloodPlain || desc == TerrainDesc.Forest || desc == TerrainDesc.Jungle || desc == TerrainDesc.GrasslandHill || desc == TerrainDesc.Swamp)
        {
            foreach (Vector3Int neighbor in GetNeighborsFor(td.TileCoordinates, State.FOURWAYINCREMENT))
            {
                if (GetTerrainDataAt(neighbor).terrainData.grassland || GetTerrainDataAt(neighbor).terrainData.desert)
                    grasslandCount[i] = 0;
                else
                    grasslandCount[i] = 1;

                i++;
            }
        }
        else if (desc == TerrainDesc.Desert || desc == TerrainDesc.DesertFloodPlain || desc == TerrainDesc.DesertHill || desc == TerrainDesc.River)
        {
            foreach (Vector3Int neighbor in GetNeighborsFor(td.TileCoordinates, State.FOURWAYINCREMENT))
            {
                if (GetTerrainDataAt(neighbor).terrainData.grassland)
                    grasslandCount[i] = 1;
                else
                    grasslandCount[i] = 0;

                i++;
            }
        }
        else
            return;
        
        if (grasslandCount.Sum() == 0)
        {
            td.SetMinimapIcon();
            return;
        }

        int eulerAngle = Mathf.RoundToInt(td.main.transform.eulerAngles.y);

        Vector2[] uvs = SetUVMap(grasslandCount, SetUVShift(desc), eulerAngle);
        if (td.UVs.Length > 4)
            uvs = NormalizeUVs(uvs, td.UVs);
        td.SetUVs(uvs);
    }

    private float SetUVShift(TerrainDesc desc)
    {
        float interval = 256f / 4096;
        float shift = 0;
        
        switch (desc)
        {
            case TerrainDesc.Desert:
                shift = interval;
                break;
            case TerrainDesc.GrasslandFloodPlain:
                shift = interval * 2;
                break;
            case TerrainDesc.DesertFloodPlain:
                shift = interval * 3;
                break;
            case TerrainDesc.River:
                shift = interval * 3;
                break;
            case TerrainDesc.GrasslandHill:
                shift = interval * 7;
                break;
            case TerrainDesc.DesertHill:
                shift = interval * 8;
                break;
        }

        return shift;
    }

    //for setting the uv map to transition between terrains, also includes uv coordinates rotation
    private Vector2[] SetUVMap(int[] count, float shift, int eulerAngle)
    {
        Vector2[] uvMap = new Vector2[4];
        int rotation = 0;
        float change = 0.0025f;
        //if (river) 
        int currentRot = eulerAngle / 90; //to offset tile rotation

        switch (count.Sum())
        {
            case 1:
                rotation = Array.FindIndex(count, x => x == 1) - currentRot;
                if (rotation < 0)
                    rotation += 4;
                uvMap = borderOne.sharedMesh.uv;

                if (rotation > 0)
                {
                    uvMap[0].y -= change;
                    uvMap[3].y -= change;

                    switch (rotation)
                    {
                        case 1:
                            uvMap[0].x += change;
                            uvMap[1].x += change;
                            break;
                        case 2:
                            uvMap[1].y -= change;
                            uvMap[2].y -= change;
                            break;
                        case 3:
                            uvMap[2].x -= change;
                            uvMap[3].x -= change;
                            break;
                    }
                }
                break;
            case 2:
                int sum = count[1] + count[3];
                bool cross = false;

                if (count[0] == 0)
                {
                    if (sum == 2)
                    {
                        rotation = 1 - currentRot;
                        uvMap = borderTwoCross.sharedMesh.uv;
                        cross = true;
                    }
                    else
                    {
                        rotation = (count[1] == 1 ? 3 : 0) - currentRot;
                        uvMap = borderTwoCorner.sharedMesh.uv;
                    }
                }
                else
                {
                    if (sum == 0)
                    {
                        rotation = 0 - currentRot;
                        uvMap = borderTwoCross.sharedMesh.uv;
                        cross = true;
                    }
                    else
                    {
                        rotation = (count[1] == 1 ? 2 : 1) - currentRot;
                        uvMap = borderTwoCorner.sharedMesh.uv;
                    }
                }

                if (rotation < 0)
                    rotation += 4;

                if (rotation > 0)
                {
                    if (cross)
                    {
                        uvMap[0] += new Vector2(change, -change);
                        uvMap[1] += new Vector2(change, change);
                        uvMap[2] += new Vector2(-change, change);
                        uvMap[3] += new Vector2(-change, -change);
                    }
                    else
                    {
                        switch (rotation)
                        {
                            case 1:
                                uvMap[0].y += change;
                                uvMap[1].y += change;
                                uvMap[2].y += change;
                                uvMap[3].y += change;
                                break;
                            case 2:
                                Vector2 change2 = new Vector2(change, change);
                                uvMap[0] += change2;
                                uvMap[1] += change2;
                                uvMap[2] += change2;
                                uvMap[3] += change2;
                                break;
                            case 3:
                                uvMap[0].x += change;
                                uvMap[1].x += change;
                                uvMap[2].x += change;
                                uvMap[3].x += change;
                                break;
                        }
                    }
                }
                break;
            case 3:
                rotation = Array.FindIndex(count, x => x == 0) - currentRot;
                if (rotation < 0)
                    rotation += 4;
                uvMap = borderThree.sharedMesh.uv;

                if (rotation > 0)
                {
                    uvMap[0].y += change;
                    uvMap[3].y += change;

                    switch (rotation)
                    {
                        case 1:
                            uvMap[0].x -= change;
                            uvMap[1].x -= change;
                            break;
                        case 2:
                            uvMap[1].y += change;
                            uvMap[2].y += change;
                            break;
                        case 3:
                            uvMap[2].x += change;
                            uvMap[3].x += change;
                            break;
                    }
                }
                break;
            case 4:
                uvMap = borderFour.sharedMesh.uv;
                break;
        }
        
        for (int i = 0; i < uvMap.Length; i++)
            uvMap[i].x += shift;

        return uvMap;
    }

    //for reassigning UVs when the Vector2 counts don't match
    public Vector2[] NormalizeUVs(Vector2[] terrainUVs, Vector2[] newUVs)
    {
        int i = 0;
        float maxX = 0;
        float minX = 1;
        float maxY = 0;
        float minY = 1;
        float newMaxX = 0;
        float newMinX = 1;
        float newMaxY = 0;
        float newMinY = 1;
        while (i < terrainUVs.Length)
        {
            Vector2 vector = terrainUVs[i];
            if (maxX < vector.x)
                maxX = vector.x;
            if (maxY < vector.y)
                maxY = vector.y;
            if (minX > vector.x)
                minX = vector.x;
            if (minY > vector.y)
                minY = vector.y;
            i++;
        }

        i = 0;
        while (i < newUVs.Length)
        {
            Vector2 vector = newUVs[i];
            if (newMaxX < vector.x)
                newMaxX = vector.x;
            if (newMaxY < vector.y)
                newMaxY = vector.y;
            if (newMinX > vector.x)
                newMinX = vector.x;
            if (newMinY > vector.y)
                newMinY = vector.y;
            i++;
        }

        i = 0;
        float rangeX = maxX - minX;
        float rangeY = maxY - minY;
        float newRangeX = newMaxX - newMinX;
        float newRangeY = newMaxY - newMinY;
        while (i < newUVs.Length)
        {
            Vector2 uv = newUVs[i];
            uv.x = minX + rangeX * ((uv.x - newMinX) / newRangeX);
            uv.y = minY + rangeY * ((uv.y - newMinY) / newRangeY);
            newUVs[i] = uv;
            i++;
        }

        return newUVs;
    }

    private void CreateParticleSystems()
    {
        lightBeam = Instantiate(lightBeam, new Vector3(0, 0, 0), Quaternion.Euler(-90, 0, 0));
        lightBeam.transform.parent = transform;
        lightBeam.Pause();
    }

    public void CreateLightBeam(Vector3 loc)
    {
        loc.y += 2f;
        lightBeam.transform.position = loc;
        lightBeam.Play();
    }

    public void UnselectAll()
    {
        cityBuilderManager.ResetCityUI();
        unitMovement.ClearSelection();
        cityBuilderManager.UnselectWonder();
        cityBuilderManager.UnselectTradeCenter();
        //unitMovement.LoadUnloadFinish(false);
        researchTree.ToggleVisibility(false);
        wonderHandler.ToggleVisibility(false);
        //mapPanel.ToggleVisibility(false);
        wonderButton.ToggleButtonColor(false);
        CloseMap();
        CloseTerrainTooltip();
        CloseImprovementTooltip();
    }

    public void ToggleMinimap(bool v)
    {
        if (v)
        {
            LeanTween.moveX(mapHandler.minimapHolder, mapHandler.minimapHolder.anchoredPosition3D.x + -400f, 0.5f).setEaseOutSine();
            LeanTween.moveX(mapHandler.minimapRing, mapHandler.minimapRing.anchoredPosition3D.x + -400f, 0.5f).setEaseOutSine();
            LeanTween.moveX(wonderButton.allContents, wonderButton.allContents.anchoredPosition3D.x + -400f, 0.5f).setEaseOutSine();
            LeanTween.moveX(mapPanelButton, mapPanelButton.anchoredPosition3D.x + -400f, 0.5f).setEaseOutSine();
            LeanTween.moveX(mainMenuButton, mainMenuButton.anchoredPosition3D.x + -400f, 0.5f).setEaseOutSine();
            LeanTween.moveX(uiTomFinder.allContents, uiTomFinder.allContents.anchoredPosition3D.x + -400f, 0.5f).setEaseOutSine();
        }
        else
        {
            LeanTween.moveX(mapHandler.minimapHolder, mapHandler.minimapHolder.anchoredPosition3D.x + 400f, 0.3f);
            LeanTween.moveX(mapHandler.minimapRing, mapHandler.minimapRing.anchoredPosition3D.x + 400f, 0.3f);
            LeanTween.moveX(wonderButton.allContents, wonderButton.allContents.anchoredPosition3D.x + 400f, 0.3f);
            LeanTween.moveX(mapPanelButton, mapPanelButton.anchoredPosition3D.x + 400f, 0.3f);
            LeanTween.moveX(mainMenuButton, mainMenuButton.anchoredPosition3D.x + 400f, 0.3f);
            LeanTween.moveX(uiTomFinder.allContents, uiTomFinder.allContents.anchoredPosition3D.x + 400f, 0.3f);
        }
    }

    //wonder info
    public void HandleWonderPlacement(Vector3 location, GameObject detectedObject)
    {
        uiRotateWonder.ToggleTweenVisibility(false);
        wonderNoWalkLoc.Clear();

        if (buildingWonder) //only thing that works is placing wonder
        {
            if (wonderPlacementLoc != null)
                Destroy(wonderGhost);

            Vector3Int locationPos = GetClosestTerrainLoc(location);
            uiConfirmWonderBuild.ToggleTweenVisibility(false);

            if (wonderPlacementLoc.Count > 0)
            {
                foreach (Vector3Int tile in wonderPlacementLoc)
                {
                    GetTerrainDataAt(tile).DisableHighlight();
                }

                if (locationPos == wonderPlacementLoc[0]) //reset if select same square twice
                {
                    wonderPlacementLoc.Clear();
                    return;
                }
            }

            List<Vector3Int> wonderLocList = new();

            int width = sideways ? wonderData.sizeHeight : wonderData.sizeWidth;
            int height = sideways ? wonderData.sizeWidth : wonderData.sizeHeight;
            Vector3 avgLoc = new Vector3(0, -0.01f, 0);

            //for checking for units
            int k = 0;
            int[] xArray = new int[width * height];
            int[] zArray = new int[width * height];

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Vector3Int newPos = locationPos;
                    newPos.z += i * increment;
                    newPos.x += j * increment;
                    TerrainData td = GetTerrainDataAt(newPos);

                    if (!td.isDiscovered)
                    {
                        UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Must explore here");
                        wonderPlacementLoc.Clear();
                        return;
                    }
                    else if (td.terrainData.type != wonderData.terrainType)
                    {
                        UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Must build on " + wonderData.terrainType);
                        wonderPlacementLoc.Clear();
                        return;
                    }
                    //else if (td.terrainData.hasRocks)
                    //{
                    //    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Resources in the way");
                    //    wonderPlacementLoc.Clear();
                    //    wonderNoWalkLoc.Clear();
                    //    return;
                    //}
                    else if (newPos - locationPos != unloadLoc && !IsTileOpenCheck(newPos))
                    {
                        UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Something in the way");
                        wonderPlacementLoc.Clear();
                        return;
                    }
                    else if (newPos - locationPos == unloadLoc && !IsTileOpenButRoadCheck(newPos))
                    {
                        UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Something in the way");
                        wonderPlacementLoc.Clear();
                        return;
                    }
 
                    xArray[k] = newPos.x;
                    zArray[k] = newPos.z;
                    k++;
                    avgLoc += newPos;
                    wonderLocList.Add(newPos);
                }
            }

            int xMin = Mathf.Min(xArray) - 1;
            int xMax = Mathf.Max(xArray) + 1;
            int zMin = Mathf.Min(zArray) - 1;
            int zMax = Mathf.Max(zArray) + 1;

            foreach (Vector3Int tile in wonderLocList)
            {
                foreach (Vector3Int neighbor in GetNeighborsFor(tile, State.EIGHTWAY))
                {
                    if (neighbor.x == xMin || neighbor.x == xMax || neighbor.z == zMin || neighbor.z == zMax)
                        continue;

                    if (IsUnitLocationTaken(neighbor))
                    {
                        UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Unit in the way");
                        wonderPlacementLoc.Clear();
                        wonderNoWalkLoc.Clear();
                        return;
                    }

                    wonderNoWalkLoc.Add(neighbor);
                }

                wonderNoWalkLoc.Add(tile);
            }

            uiRotateWonder.ToggleTweenVisibility(true);

            wonderGhost = Instantiate(wonderData.wonderPrefab, avgLoc / wonderLocList.Count, rotation);
            wonderGhost.GetComponent<Wonder>().SetLastPrefab(); //only showing 100 Perc prefab
            Color newColor = new(1, 1, 1, .75f);
            MeshRenderer[] renderers = wonderGhost.GetComponentsInChildren<MeshRenderer>();

            //assigning transparent material to all meshrenderers
            foreach (MeshRenderer render in renderers)
            {
                Material[] newMats = render.materials;

                for (int i = 0; i < newMats.Length; i++)
                {
                    Material newMat = new(transparentMat);
                    newMat.color = newColor;
                    newMat.SetTexture("_BaseMap", newMats[i].mainTexture);
                    newMats[i] = newMat;
                }

                render.materials = newMats;
            }

            foreach (Vector3Int tile in wonderLocList)
            {
                if (tile-locationPos == unloadLoc)
                {
                    GetTerrainDataAt(tile).EnableHighlight(Color.blue);
                    finalUnloadLoc = tile;
                }
                else
                    GetTerrainDataAt(tile).EnableHighlight(Color.white);
            }

            //GameObject wonderGhostGO = Instantiate(wonderData.prefabComplete);
            //foreach ()

            uiConfirmWonderBuild.ToggleTweenVisibility(true);
            //uiRotateWonder.ToggleTweenVisibility(true);
            wonderPlacementLoc = wonderLocList;
        }
    }

    public void SetWonderConstruction()
    {
        Destroy(wonderGhost);

        //resetting ui
        if (wonderPlacementLoc.Count > 0)
        {
            foreach (Vector3Int tile in wonderPlacementLoc)
                GetTerrainDataAt(tile).DisableHighlight();
        }

        unitMovement.ToggleCancelButton(false);
        uiConfirmWonderBuild.ToggleTweenVisibility(false);
        buildingWonder = false;
        uiBuildingSomething.ToggleVisibility(false);
        uiRotateWonder.ToggleTweenVisibility(false);

        if (wonderPlacementLoc.Count == 0)
            return;

        Vector3 avgLoc = new Vector3(0, 0, 0);

        //double checking if it's blocked
        foreach (Vector3Int tile in wonderNoWalkLoc)
        {
            if (IsUnitLocationTaken(tile))
            {
                InfoPopUpHandler.WarningMessage().Create(tile, "Unit in the way");
                wonderPlacementLoc.Clear();
                return;
            }

            if (wonderPlacementLoc.Contains(tile))
            {
                avgLoc += tile;

                if ((tile != finalUnloadLoc && !IsTileOpenCheck(tile)) || (tile == finalUnloadLoc && !IsTileOpenButRoadCheck(tile)))
                {
                    InfoPopUpHandler.WarningMessage().Create(tile, "Something in the way");
                    wonderPlacementLoc.Clear();
                    return;
                }
            }
        }

        //hide ground
        foreach(Vector3Int tile in wonderPlacementLoc)
        {
            TerrainData td = GetTerrainDataAt(tile);

            if (td.prop != null)
                td.prop.gameObject.SetActive(false);
            td.HideTerrainMesh();
            if (td.hasResourceMap)
                td.HideResourceMap();
        }
        //setting up wonder info
        Vector3 centerPos = avgLoc / wonderPlacementLoc.Count;
        GameObject wonderGO = Instantiate(wonderData.wonderPrefab, centerPos, rotation);
        wonderGO.gameObject.transform.SetParent(wonderHolder, false);
        Wonder wonder = wonderGO.GetComponent<Wonder>();
        wonder.SetReferences(this, cityBuilderManager.focusCam);
        wonder.WonderData = wonderData;
        wonder.SetPrefabs();
        wonder.wonderName = "Wonder - " + wonderData.wonderName;
        wonder.SetResourceDict(wonderData.wonderCost);
        wonder.unloadLoc = finalUnloadLoc;
        AddTradeLoc(finalUnloadLoc, wonder.wonderName);
        wonderNoWalkLoc.Remove(finalUnloadLoc);
        wonder.roadPreExisted = IsRoadOnTerrain(finalUnloadLoc);
        //wonder.Rotation = rotation;
        wonder.SetCenterPos(centerPos);
        wonder.WonderLocs = new(wonderPlacementLoc);
        wonderConstructionDict[wonder.wonderName] = wonder;
        foreach (Vector3Int tile in wonderPlacementLoc)
            wonderStopDict[tile] = wonder;

        //building road in unload area
        if (!wonder.roadPreExisted)
            roadManager.BuildRoadAtPosition(finalUnloadLoc);

        //claiming the area for the wonder
        List<Vector3Int> harborTiles = new();
        foreach (Vector3Int tile in wonderPlacementLoc)
        {
            AddToCityLabor(tile, wonderGO); //so cities can't take the spot
            AddStructure(tile, wonderGO); //so nothing else can be built there
            //AddStructureMap(tile, wonderMapIcon);
            //mapPanel.SetTileSprite(tile, TerrainDesc.Wonder);

            //checking if there's a spot to build harbor
            foreach (Vector3Int neighbor in GetNeighborsFor(tile, State.FOURWAYINCREMENT))
            {
                if (wonderPlacementLoc.Contains(neighbor))
                    continue;

                TerrainData td = GetTerrainDataAt(neighbor);

                if (td.terrainData.type == TerrainType.Coast || td.terrainData.type == TerrainType.River)
                {
                    wonder.canBuildHarbor = true;
                    harborTiles.Add(neighbor);
                }
            }
        }

        wonder.PossibleHarborLocs = harborTiles;

        noWalkList.AddRange(wonderNoWalkLoc);
        wonderPlacementLoc.Clear();
        wonderNoWalkLoc.Clear();
    }

    public void PlaceWonder(WonderDataSO wonderData)
    {
        buildingWonder = true;
        this.wonderData = wonderData;
        CloseWonders();

        rotationCount = 0;
        unloadLoc = wonderData.unloadLoc;

        uiBuildingSomething.SetText("Building " + wonderData.wonderName);
        uiBuildingSomething.ToggleVisibility(true);
        unitMovement.ToggleCancelButton(true);
    }

    public void RotateWonderPlacement()
    {
        rotationCount++;

        Vector3Int tempPlacementLoc;
        if (wonderPlacementLoc.Count > 0)
            tempPlacementLoc = wonderPlacementLoc[0];
        else
            tempPlacementLoc = new Vector3Int(0, 0, 0);

        sideways = false;

        Vector3Int placementLoc = tempPlacementLoc;
        if (rotationCount % 4 == 1)
        {
            placementLoc.z += (wonderData.sizeHeight - 1) * increment;
            sideways = true;
        }
        if (rotationCount % 4 == 2)
        {
            placementLoc.z += (wonderData.sizeHeight - 1) * increment;
            placementLoc.x += (wonderData.sizeWidth - 1) * increment;
            sideways = false;
        }
        if (rotationCount % 4 == 3)
        {
            placementLoc.x += (wonderData.sizeWidth - 1) * increment;
            sideways = true;
        }

        foreach (Vector3Int tile in wonderPlacementLoc)
        {
            GetTerrainDataAt(tile).DisableHighlight();

            if (tile-placementLoc == wonderData.unloadLoc)
            {
                GetTerrainDataAt(tile).EnableHighlight(Color.blue);
                finalUnloadLoc = tile;
            }
            else
            {
                GetTerrainDataAt(tile).EnableHighlight(Color.white);
            }
        }

        rotation = Quaternion.Euler(0, rotationCount * 90, 0);
        wonderGhost.transform.rotation = rotation;
        unloadLoc = placementLoc - tempPlacementLoc;
    }

    public void CloseBuildingSomethingPanel()
    {
        if (workerOrders)
        {
            unitMovement.CloseBuildingSomethingPanel();
            return;
        }
        
        if (wonderGhost != null)
            Destroy(wonderGhost);
        
        if (wonderPlacementLoc.Count > 0)
        {
            foreach (Vector3Int tile in wonderPlacementLoc)
            {
                GetTerrainDataAt(tile).DisableHighlight();
            }

            wonderPlacementLoc.Clear();
            wonderNoWalkLoc.Clear();
        }
        
        unitMovement.ToggleCancelButton(false);
        uiConfirmWonderBuild.ToggleTweenVisibility(false);
        buildingWonder = false;
        uiBuildingSomething.ToggleVisibility(false);
        uiRotateWonder.ToggleTweenVisibility(false);
    }

    public bool GetWondersConstruction(string name)
    {
        return wonderConstructionDict.Keys.ToList().Contains(name);
    }

    public void OpenWonders()
    {
        //if (workerOrders)
        //    return;

        if (buildingWonder || workerOrders)
            CloseBuildingSomethingPanel();

        if (wonderHandler.activeStatus)
        {
            wonderHandler.ToggleVisibility(false);
            wonderButton.ToggleButtonColor(false);
        }
        else
        {
            wonderHandler.ToggleVisibility(true);
            wonderButton.ToggleButtonColor(true);
        }
    }

    public void CloseWonders()
    {
        if (wonderHandler.activeStatus)
        {
            wonderHandler.ToggleVisibility(false);
            wonderButton.ToggleButtonColor(false);
        }
    }

    //add unload zone when finishing wonder, also for improvements
    public void AddToNoWalkList(Vector3Int loc)
    {
        noWalkList.Add(loc);
    }

    //when cancelling a wonder or removing improvement
    public void RemoveFromNoWalkList(Vector3Int loc)
    {
        noWalkList.Remove(loc);
    }

    //Research info
    public void OpenResearchTree()
    {
        //if (workerOrders)
        //    return;

        if (workerOrders || buildingWonder)
            CloseBuildingSomethingPanel();
        
        if (researchTree.activeStatus)
            researchTree.ToggleVisibility(false);
        else
            researchTree.ToggleVisibility(true);
    }

    public void CloseResearchTree()
    {
        researchTree.ToggleVisibility(false);
    }

    //public void OpenMap()
    //{
    //    if (workerOrders)
    //        return;

    //    if (mapPanel.activeStatus)
    //        mapPanel.ToggleVisibility(false);
    //    else
    //        mapPanel.ToggleVisibility(true);
    //}

    public void CloseMap()
    {
        mapHandler.ToggleVisibility(false);
        //mapPanel.ToggleVisibility(false);
    }
    
    //public void AddToMinimap(GameObject icon)
    //{
    //    icon.gameObject.transform.SetParent(mapIconHolder, false);
    //}

    //terrain tooltip
    public void OpenTerrainTooltip(TerrainData td)
    {
        if (tooltip)
        {
            CloseTerrainTooltip();
            CloseImprovementTooltip();
            tooltip = false;
            return;
        }
        
        tooltip = true;
        infoPopUpCanvas.gameObject.SetActive(true);
        uiTerrainTooltip.ToggleVisibility(true, td);
        //uiTerrainTooltip.SetData(td);
    }

    public void CloseTerrainTooltipButton()
    {
        tooltip = false;
        //infoPopUpCanvas.gameObject.SetActive(false);
        uiTerrainTooltip.ToggleVisibility(false);
    }

    public void CloseTerrainTooltip()
    {
        //infoPopUpCanvas.gameObject.SetActive(false);
        uiTerrainTooltip.ToggleVisibility(false);
    }

    //city improvement tooltip
    public void OpenImprovementTooltip(CityImprovement improvement)
    {
        if (tooltip)
        {
            CloseTerrainTooltip();
            CloseImprovementTooltip();
            tooltip = false;
            return;
        }

        tooltip = true;
        infoPopUpCanvas.gameObject.SetActive(true);
        uiCityImprovementTip.ToggleVisibility(true, improvement);
        //uiTerrainTooltip.SetData(td);
    }

    public void CloseImprovementTooltipButton()
    {
        tooltip = false;
        //infoPopUpCanvas.gameObject.SetActive(false);
        uiCityImprovementTip.ToggleVisibility(false);
    }

    public void CloseImprovementTooltip()
    {
        //infoPopUpCanvas.gameObject.SetActive(false);
        uiCityImprovementTip.ToggleVisibility(false);
    }

    public void SetResearchName(string name)
    {
        uiWorldResources.SetResearchName(name);
    }

    public void AddToResearchWaitList(City city)
    {
        if (!researchWaitList.Contains(city))
            researchWaitList.Add(city);
    }

    public void RestartResearch()
    {
        List<City> cityResearchWaitList = new(researchWaitList);
        
        foreach (City city in cityResearchWaitList)
        {
            if (researching)
            {
                researchWaitList.Remove(city);
                city.RestartResearch();
            }
        }
    }

    public bool CitiesResearchWaitingCheck()
    {
        return researchWaitList.Count > 0;
    }

    public void AddToGoldCityWaitList(City city)
    {
        if (!goldCityWaitList.Contains(city))
            goldCityWaitList.Add(city);
    }

    public void RestartCityProduction()
    {
        List<City> cityWaitList = new(goldCityWaitList);

        foreach (City city in cityWaitList)
        {
            goldCityWaitList.Remove(city);
            city.RestartProduction();
        }
    }

    public bool CitiesGoldWaitingCheck()
    {
        return goldCityWaitList.Count > 0;
    }

    public void AddToGoldWonderWaitList(Wonder wonder)
    {
        if (!goldWonderWaitList.Contains(wonder))
            goldWonderWaitList.Add(wonder);
    }

    private void RestartWonderConstruction()
    {
        List<Wonder> wonderWaitList = new(goldWonderWaitList);

        foreach (Wonder wonder in wonderWaitList)
        {
            goldWonderWaitList.Remove(wonder);
            wonder.ThresholdCheck();
        }
    }

    public void AddToGoldTradeCenterWaitList(TradeCenter tradeCenter)
    {
        if (!goldTradeCenterWaitList.Contains(tradeCenter))
            goldTradeCenterWaitList.Add(tradeCenter);
    }

    private bool WondersWaitingCheck()
    {
        return goldWonderWaitList.Count > 0;
    }

    private void RestartTradeCenterRoutes()
    {
        List<TradeCenter> tradeCenterWaitList = new(goldTradeCenterWaitList);

        foreach (TradeCenter tradeCenter in tradeCenterWaitList)
        {
            goldTradeCenterWaitList.Remove(tradeCenter);
            tradeCenter.GoldCheck();
        }
    }

    private bool TradeCentersWaitingCheck()
    {
        return goldTradeCenterWaitList.Count > 0;
    }

    public void RemoveTradeCenterFromWaitList(TradeCenter tradeCenter)
    {
        goldTradeCenterWaitList.Remove(tradeCenter);
    }

    //world resources management
    public void UpdateWorldResources(ResourceType resourceType, int amount)
    {
        if (resourceType == ResourceType.Research)
        {
            amount = researchTree.AddResearch(amount);
            researchTree.CompletedResearchCheck();
            worldResourceManager.SetResource(resourceType, amount);
            researchTree.CompletionNextStep();
        }
        else
        {
            int prevAmount = worldResourceManager.GetWorldGoldLevel();

            worldResourceManager.SetResource(resourceType, amount);
            bool pos = amount > 0;

            if (pos)
            {
                if (CitiesGoldWaitingCheck())
                    RestartCityProduction();
                
                if (WondersWaitingCheck())
                    RestartWonderConstruction();

                if (TradeCentersWaitingCheck())
                    RestartTradeCenterRoutes();
            }

            int currentAmount = worldResourceManager.GetWorldGoldLevel();
            if (cityBuilderManager.uiTradeCenter.activeStatus)
                cityBuilderManager.uiTradeCenter.UpdateColors(prevAmount, currentAmount, pos);
            else if (unitMovement.uiCityResourceInfoPanel.activeStatus)
                unitMovement.uiCityResourceInfoPanel.UpdatePriceColors(prevAmount, currentAmount, pos);
            else if (cityBuilderManager.uiUnitBuilder.activeStatus)
                cityBuilderManager.uiUnitBuilder.UpdateBuildOptions(ResourceType.Gold, prevAmount, currentAmount, pos, cityBuilderManager.SelectedCity.ResourceManager);
        }
    }

    public void UpdateWorldResourceGeneration(ResourceType resourceType, float amount, bool add)
    {
        worldResourceManager.ModifyResourceGenerationPerMinute(resourceType, amount, add);
    }

    public bool CheckWorldGold(int amount)
    {
        return worldResourceManager.GetWorldGoldLevel() >= amount;
    }

    public List<ResourceType> WorldResourcePrep()
    {
        return worldResourceManager.PassWorldResources();
    }

    public void UpdateWorldResourceUI(ResourceType resourceType, int diffAmount)
    {
        worldResourceManager.UpdateUIGeneration(resourceType, diffAmount);
    }

    public void SetWorldResearchUI(int researchReceived, int totalResearch)
    {
        worldResourceManager.SetResearch(researchReceived);
        uiWorldResources.ResearchLimit = totalResearch;
        uiWorldResources.SetResearchValue(researchReceived);
    }

    //updating builder handlers if one is selected
    public void BuilderHandlerCheck()
    {
        if (cityBuilderManager.activeBuilderHandler)
            cityBuilderManager.activeBuilderHandler.PrepareBuildOptions(cityBuilderManager.SelectedCity.ResourceManager);
    }

    public Unit GetUnit(Vector3Int tile)
    {
        return unitPosDict[tile];
    }

    public bool IsUnitWaitingForSameStop(Vector3Int tile, Vector3 finalDestination)
    {
        if (!unitPosDict.ContainsKey(tile))
            return false;

        Unit tempUnit = unitPosDict[tile];

        if (tempUnit.isWaiting && tempUnit.finalDestinationLoc == finalDestination)
            return true;
        else
            return false;
    }

    public City GetCity(Vector3Int tile)
    {
        return cityDict[tile];
    }

    public Wonder GetWonder(Vector3Int tile)
    {
        return wonderStopDict[tile];
    }

    public TradeCenter GetTradeCenter(Vector3Int tile)
    {
        return tradeCenterStopDict[tile];
    }

    public void RemoveWonder(Vector3Int tile)
    {
        wonderStopDict.Remove(tile);
    }

    public List<string> GetConnectedCityNames(Vector3Int unitLoc, bool bySea)
    {
        List<string> names = new();

        //getting wonder names first
        foreach (string name in wonderConstructionDict.Keys)
        {
            Vector3Int destination;

            if (bySea)
            {
                if (wonderConstructionDict[name].hasHarbor)
                    destination = wonderConstructionDict[name].harborLoc;
                else
                    continue;
            }
            else
            {
                destination = wonderConstructionDict[name].unloadLoc;
            }

            if (GridSearch.TraderMovementCheck(this, unitLoc, destination, bySea))
            {
                names.Add(name);
            }
        }
        
        //getting city names second
        foreach (string name in cityNameDict.Keys)
        {
            Vector3Int destination;
            
            if (bySea)
            {
                City city = cityDict[cityNameDict[name]];
                if (!city.hasHarbor)
                    continue;
                else
                    destination = city.harborLocation;
            }
            else
            {
                destination = cityNameDict[name];
            }

            //check if trader can reach all destinations
            if (GridSearch.TraderMovementCheck(this, unitLoc, destination, bySea))
            {
                names.Add(name);
            } 
        }

        foreach (string name in tradeCenterDict.Keys)
        {
            Vector3Int destination;

            if (bySea)
                destination = tradeCenterDict[name].harborLoc;
            else
                destination = tradeCenterDict[name].mainLoc;

            if (GridSearch.TraderMovementCheck(this, unitLoc, destination, bySea))
                names.Add(name);
        }

        return names;
    }

    public Vector3Int GetStopLocation(string name)
    {
        if (cityNameDict.ContainsKey(name))
            return cityNameDict[name];
        else if (wonderConstructionDict.ContainsKey(name))
            return wonderConstructionDict[name].unloadLoc;
        else
            return tradeCenterDict[name].mainLoc;
    }

    public City GetHarborCity(Vector3Int harborLocation)
    {
        return cityDict[harborLocation];
    }

    public bool CheckIfStopStillExists(Vector3Int location)
    {
        Vector3Int loc;

        if (tradeLocDict.ContainsKey(location))
            loc = GetStopLocation(GetTradeLoc(location));
        else
            return false;

        if (cityDict.ContainsKey(loc))
            return true;
        else if (wonderStopDict.ContainsKey(loc))
            return true;
        else if (tradeCenterStopDict.ContainsKey(loc))
            return true;

        return false;
    }

    public Vector3Int GetHarborStopLocation(string name)
    {
        if (cityNameDict.ContainsKey(name))
            return cityDict[cityNameDict[name]].harborLocation;
        else if (wonderConstructionDict.ContainsKey(name))
            return wonderConstructionDict[name].harborLoc;
        else
            return tradeCenterDict[name].harborLoc;
    }

    public void AddTradeLoc(Vector3Int loc, string name)
    {
        tradeLocDict[loc] = name;
    }

    public void RemoveTradeLoc(Vector3Int loc)
    {
        tradeLocDict.Remove(loc);
    }

    public string GetTradeLoc(Vector3Int loc)
    {
        return tradeLocDict[loc];
    }

    public string GetStopName(Vector3Int location)
    {
        Vector3Int loc;

        if (tradeLocDict.ContainsKey(location))
            loc = GetStopLocation(GetTradeLoc(location));
        else
            return "";

        if (cityDict.ContainsKey(loc))
        {
            return cityDict[loc].cityName;
        }
        else if (wonderStopDict.ContainsKey(loc))
        {
            return wonderStopDict[loc].wonderName;
        }
        else if (tradeCenterStopDict.ContainsKey(loc))
        {
            return tradeCenterStopDict[loc].tradeCenterName;
        }
        else
        {
            return "";
        }
    }

    public void SetQueueGhost(Vector3Int loc, GameObject gameObject)
    {
        queueGhostsDict[loc] = gameObject;
    }

    public bool ShowQueueGhost(Vector3Int loc)
    {
        if (queueGhostsDict.ContainsKey(loc))
        {
            GameObject ghost = queueGhostsDict[loc];
            ghost.SetActive(true);
            //for tweening
            ghost.transform.localScale = Vector3.zero;
            LeanTween.scale(ghost, new Vector3(1.5f, 1.5f, 1.5f), 0.25f).setEase(LeanTweenType.easeOutBack);
            return true;
        }

        return false;
    }

    public bool IsLocationQueued(Vector3Int loc)
    {
        return queueGhostsDict.ContainsKey(loc);
    }

    public GameObject GetQueueGhost(Vector3Int loc)
    {
        return queueGhostsDict[loc];
    }

    public void RemoveQueueGhost(Vector3Int loc)
    {
        queueGhostsDict.Remove(loc);
    }

    public GameObject GetStructure(Vector3Int tile)
    {
        return buildingPosDict[tile];
    }

    public GameObject GetBuilding(Vector3Int cityTile, string buildingName)
    {
        return cityBuildingGODict[cityTile][buildingName];
    }

    public CityImprovement GetBuildingData(Vector3Int cityTile, string buildingName)
    {
        return cityBuildingDict[cityTile][buildingName];
    }

    public ResourceProducer GetResourceProducer(Vector3Int pos)
    {
        return cityImprovementProducerDict[pos];
    }

    //public ResourceProducer GetBuildingProducer(Vector3Int cityTile, string buildingName)
    //{
    //    return cityBuildingIsProducer[cityTile][buildingName];
    //}

    public bool TileHasCityImprovement(Vector3Int tile)
    {
        return cityImprovementDict.ContainsKey(tile);
    }

    public CityImprovement GetCityDevelopment(Vector3Int tile)
    {
        return cityImprovementDict[tile];
    }

    public CityImprovement GetCityDevelopmentConstruction(Vector3Int tile)
    {
        return cityImprovementConstructionDict[tile];
    }

    public void SetWorkerWorkLocation(Vector3Int loc)
    {
        workerBusyLocations.Add(loc);
    }

    public void RemoveWorkerWorkLocation(Vector3Int loc)
    {
        workerBusyLocations.Remove(loc);
    }

    public bool IsWorkerWorkingAtTile(Vector3Int loc)
    {
        return workerBusyLocations.Contains(loc);
    }

    public Road GetRoads(Vector3Int tile, bool straight)
    {
        int index = straight ? 0 : 1;
        return roadTileDict[tile][index];
    }

    public void SetRoadCost(int cost)
    {
        roadCost = cost;
    }

    public int GetRoadCost()
    {
        return roadCost;
    }

    public List<Road> GetAllRoadsOnTile(Vector3Int tile)
    {
        return roadTileDict[tile];
    }

    public int GetUpgradeableObjectMaxLevel(string name)
    {
        return upgradeableObjectMaxLevelDict[name];
    }

    public void SetUpgradeableObjectMaxLevel(string name, int level)
    {
        if (upgradeableObjectMaxLevelDict[name] >= level)
            return;

        upgradeableObjectMaxLevelDict[name] = level;
    }

    public List<ResourceValue> GetUpgradeCost(string nameAndLevel)
    {
        return upgradeableObjectPriceDict[nameAndLevel]; 
    }

    public ImprovementDataSO GetUpgradeData(string nameAndLevel)
    {
        return upgradeableObjectDataDict[nameAndLevel];
    }

    public ImprovementDataSO GetImprovementData(string nameAndLevel)
    {
        return improvementDataDict[nameAndLevel];
    }

    public UnitBuildDataSO GetUnitBuildData(string nameAndLevel)
    {
        return unitBuildDataDict[nameAndLevel];
    }

    public Sprite GetResourceIcon(ResourceType resourceType)
    {
        return resourceSpriteDict[resourceType];
    }

    public Dictionary<ResourceType, int> GetDefaultResourcePrices()
    {
        return defaultResourcePriceDict;
    }

    public Dictionary<ResourceType, int> GetBlankResourceDict()
    {
        return blankResourceDict;
    }

    public Dictionary<ResourceType, bool> GetBoolResourceDict()
    {
        return boolResourceDict; 
    }

    public void SetTerrainData(Vector3Int tile, TerrainData td)
    {
        world[tile] = td;
    }

    public void SetCityDevelopment(Vector3Int tile, CityImprovement cityDevelopment)
    {
        //Vector3Int position = Vector3Int.RoundToInt(tile);
        cityImprovementDict[tile] = cityDevelopment;
    }

    public void SetCityImprovementConstruction(Vector3Int tile, CityImprovement cityDevelopment)
    {
        cityImprovementConstructionDict[tile] = cityDevelopment;
    }

    public void SetCityBuilding(CityImprovement improvement, ImprovementDataSO improvementData, Vector3Int cityTile, GameObject building, City city)
    {
        //CityImprovement improvement = building.GetComponent<CityImprovement>();
        improvement.building = improvementData.isBuilding;
        improvement.PlaySmokeSplashBuilding();
        improvement.InitializeImprovementData(improvementData);
        string buildingName = improvementData.improvementName;
        improvement.SetCity(city);
        improvement.transform.parent = city.transform;
        improvement.initialCityHouse = improvementData.cityHousing;
        city.workEthic += improvementData.workEthicChange;
        cityBuildingGODict[cityTile][buildingName] = building;
        cityBuildingDict[cityTile][buildingName] = improvement;
        cityBuildingList[cityTile].Add(buildingName);

        //making two objects, this one for the parent mesh
        GameObject tempObject = Instantiate(cityBuilderManager.emptyGO, improvement.transform.position, improvement.transform.rotation);
        tempObject.name = improvement.name;
        MeshFilter[] improvementMeshes = improvement.MeshFilter;

        MeshFilter[] meshes = new MeshFilter[improvementMeshes.Length];
        int k = 0;

        foreach (MeshFilter mesh in improvementMeshes)
        {
            Quaternion rotation = mesh.transform.rotation;
            meshes[k] = Instantiate(mesh, improvement.transform.position, rotation);
            meshes[k].name = mesh.name;
            meshes[k].transform.parent = tempObject.transform;
            k++;
        }

        tempObject.transform.localScale = improvement.transform.localScale;
        improvement.Embiggen();

        //GameObject tempObject = Instantiate(improvementData.prefab, (Vector3)cityTile + improvementData.buildingLocation, Quaternion.identity);
        //CityImprovement tempImprovement = tempObject.GetComponent<CityImprovement>();
        city.AddToMeshFilterList(tempObject, meshes, true, Vector3Int.zero, buildingName);
        tempObject.transform.parent = city.transform;
        tempObject.SetActive(false);
    }

    public void SetCityHarbor(City city, Vector3Int harborLoc)
    {
        cityDict[harborLoc] = city;
    }

    public void AddLocationToQueueList(Vector3Int location)
    {
        cityImprovementQueueList.Add(location);
    }

    public bool CheckQueueLocation(Vector3Int location)
    {
        return cityImprovementQueueList.Contains(location);
    }

    public City GetQueuedImprovementCity(Vector3Int loc)
    {
        return queueGhostsDict[loc].GetComponent<CityImprovement>().GetCity();
    }

    public void RemoveLocationFromQueueList(Vector3Int location)
    {
        cityImprovementQueueList.Remove(location);  
    }

    public void RemoveQueueGhostImprovement(Vector3Int location, City city)
    {
        cityBuilderManager.RemoveQueueGhostImprovement(location, city);
    }

    public void RemoveQueueItemCheck(Vector3Int location)
    {
        if (CheckQueueLocation(location))
        {
            RemoveLocationFromQueueList(location);
            City city = GetQueuedImprovementCity(location);
            RemoveQueueGhostImprovement(location, city);
            city.RemoveFromQueue(location - city.cityLoc);
        }
    }

    public void SetRoads(Vector3Int tile, Road road, bool straight)
    {
        int index = straight ? 0 : 1;
        roadTileDict[tile][index] = road;
    }

    public void SetRoadLocations(Vector3Int tile)
    {
        if (!roadLocsList.Contains(tile))
            roadLocsList.Add(tile);
    }

    public void SetSoloRoadLocations(Vector3Int tile)
    {
        if (!soloRoadLocsList.Contains(tile))
            soloRoadLocsList.Add(tile);
    }

    public bool IsRoadOnTileLocation(Vector3Int tile)
    {
        return roadLocsList.Contains(tile);
    }

    public bool IsSoloRoadOnTileLocation(Vector3Int tile)
    {
        return soloRoadLocsList.Contains(tile);
    }

    public void RemoveRoadLocation(Vector3Int tile)
    {
        roadLocsList.Remove(tile);
    }

    public void RemoveSoloRoadLocation(Vector3Int tile)
    {
        soloRoadLocsList.Remove(tile);
    }

    public void InitializeRoads(Vector3Int tile)
    {
        roadTileDict[tile] = new() { null, null }; //two place holders for road, first for straight, second for diagonal
    }

    public bool IsCityOnTile(Vector3Int tile) //checking if city is on tile
    {
        //return buildingPosDict.ContainsKey(tile) && buildingPosDict[tile].GetComponent<City>();
        return cityLocations.Contains(tile);
    }

    public bool IsWonderOnTile(Vector3Int tile)
    {
        return wonderStopDict.ContainsKey(tile);
    }

    public bool IsTradeCenterOnTile(Vector3Int tile)
    {
        return tradeCenterStopDict.ContainsKey(tile);
    }

    public bool IsTradeLocOnTile(Vector3Int tile)
    {
        return tradeLocDict.ContainsKey(tile);
        
        //if (cityDict.ContainsKey(tile))
        //    return true;
        //else if (wonderStopDict.ContainsKey(tile) && wonderStopDict[tile].unloadLoc == tile)
        //    return true;
        //else if (tradeCenterStopDict.ContainsKey(tile))
        //    return true;

        //return false;
    }

    public bool IsBuildLocationTaken(Vector3Int buildLoc)
    {
        return buildingPosDict.ContainsKey(buildLoc);
    }

    public bool IsUnitLocationTaken(Vector3Int unitPosition)
    {
        return unitPosDict.ContainsKey(unitPosition);
    }

    public bool IsBuildingInCity(Vector3Int cityTile, string buildingName)
    {
        return cityBuildingGODict[cityTile].ContainsKey(buildingName);
    }

    public bool IsRoadOnTerrain(Vector3Int position)
    {
        return roadTileDict.ContainsKey(position);
    }

    public bool IsCityNameTaken(string cityName)
    {
        //List<string> test = new(cityNameDict.Keys);

        foreach (string name in cityNameDict.Keys)
        {
            if (cityName.ToLower() == name.ToLower())
            {
                return true;
            }
        }

        return false;
        //return cityNameDict.ContainsKey(cityName);
    }

    //public bool TileHasBuildings(Vector3Int cityTile)
    //{
    //    if (!cityBuildingGODict.ContainsKey(cityTile))
    //    {
    //        return false;
    //    }

    //    if (cityBuildingGODict[cityTile].Count > 0)
    //        return true;
    //    else
    //        return false;
    //}



    //for movement
    public bool CheckIfPositionIsValid(Vector3Int tile)
    {
        return world.ContainsKey(tile) && world[tile].terrainData.walkable && !noWalkList.Contains(tile);
    }

    public bool CheckIfSeaPositionIsValid(Vector3Int tile)
    {
        return world.ContainsKey(tile) && world[tile].terrainData.sailable && !noWalkList.Contains(tile);
    }

    public bool CheckIfCoastCoast(Vector3Int tile)
    {
        return coastCoastList.Contains(tile);
    }

    public void AddToCoastList(Vector3Int tile)
    {
        coastCoastList.Add(tile);
    }
    //public bool CheckIfIsCoast(Vector3Int tileWorldPosition)
    //{
    //    return GetTerrainDataAt(tileWorldPosition).IsCoast;
    //    //return world[tileWorldPosition].GetTerrainData().type == TerrainType.River || world[tileWorldPosition].GetTerrainData().type == TerrainType.Coast;
    //}

    //public Vector3Int GetClosestTile(Vector3 worldPosition)
    //{
    //    worldPosition.y = 0;
    //    return Vector3Int.RoundToInt(worldPosition);
    //}

    public int GetMovementCost(Vector3Int tileWorldPosition)
    {
        //if (v)
        //    return world[tileWorldPosition].MovementCost;
        //else
        return world[tileWorldPosition].MovementCost;
        //return world[tileWorldPosition].MovementCost; //for counting road movement cost from non-road terrain
    }

    public TerrainData GetTerrainDataAt(Vector3Int tileWorldPosition)
    {
        world.TryGetValue(tileWorldPosition, out TerrainData td);
        return td;
    }

    private readonly static List<Vector3Int> neighborsFourDirections = new()
    {
        new Vector3Int(0,0,1), //up
        new Vector3Int(1,0,0), //right
        new Vector3Int(0,0,-1), //down
        new Vector3Int(-1,0,0), //left
    };

    private readonly static List<Vector3Int> neighborsFourDirectionsIncrement = new()
    {
        new Vector3Int(0,0,increment), //up
        new Vector3Int(increment,0,0), //right
        new Vector3Int(0,0,-increment), //down
        new Vector3Int(-increment,0,0), //left
    };

    private readonly static List<Vector3Int> neighborsDiagFourDirections = new()
    {
        new Vector3Int(1, 0, 1), //upper right
        new Vector3Int(1, 0, -1), //lower right
        new Vector3Int(-1, 0, -1), //lower left
        new Vector3Int(-1, 0, 1), //upper left
    };

    private readonly static List<Vector3Int> neighborsDiagFourDirectionsIncrement = new()
    {
        new Vector3Int(increment, 0, increment), //upper right
        new Vector3Int(increment, 0, -increment), //lower right
        new Vector3Int(-increment, 0, -increment), //lower left
        new Vector3Int(-increment, 0, increment), //upper left
    };

    private readonly static List<Vector3Int> neighborsEightDirections = new()
    {
        new Vector3Int(0,0,1), //up
        new Vector3Int(1,0,1), //upper right
        new Vector3Int(1,0,0), //right
        new Vector3Int(1,0,-1), //lower right
        new Vector3Int(0,0,-1), //down
        new Vector3Int(-1,0,-1), //lower left
        new Vector3Int(-1,0,0), //left
        new Vector3Int(-1,0,1), //upper left
    };

    private readonly static List<Vector3Int> neighborsEightDirectionsIncrement = new()
    {
        new Vector3Int(0,0,increment), //up
        new Vector3Int(increment,0,increment), //upper right
        new Vector3Int(increment,0,0), //right
        new Vector3Int(increment,0,-increment), //lower right
        new Vector3Int(0,0,-increment), //down
        new Vector3Int(-increment,0,-increment), //lower left
        new Vector3Int(-increment,0,0), //left
        new Vector3Int(-increment,0,increment), //upper left
    };

    private readonly static List<Vector3Int> neighborsEightDirectionsTwoDeep = new()
    {
        new Vector3Int(0,0,1), //up
        new Vector3Int(1,0,1), //upper right
        new Vector3Int(1,0,0), //right
        new Vector3Int(1,0,-1), //lower right
        new Vector3Int(0,0,-1), //down
        new Vector3Int(-1,0,-1), //lower left
        new Vector3Int(-1,0,0), //left
        new Vector3Int(-1,0,1), //upper left
        new Vector3Int(0,0,2), //up up
        new Vector3Int(1,0,2), //up up right
        new Vector3Int(2,0,2), //upper right corner
        new Vector3Int(2,0,1), //up right right
        new Vector3Int(2,0,0), //right right
        new Vector3Int(2,0,-1), //right right down
        new Vector3Int(2,0,-2), //lower right corner
        new Vector3Int(1,0,-2), //down down right
        new Vector3Int(0,0,-2), //down down
        new Vector3Int(-1,0,-2), //down down left
        new Vector3Int(-2,0,-2), //lower left corner
        new Vector3Int(-2,0,-1), //left left down
        new Vector3Int(-2,0,0), //left left
        new Vector3Int(-2,0,1), //left left up
        new Vector3Int(-2,0,2), //upper left corner
        new Vector3Int(-1,0,2), //up up left
    };

    private readonly static List<Vector3Int> cityRadius = new()
    {
        new Vector3Int(0,0,increment), //up
        new Vector3Int(increment,0,increment), //upper right
        new Vector3Int(increment,0,0), //right
        new Vector3Int(increment,0,-increment), //lower right
        new Vector3Int(0,0,-increment), //down
        new Vector3Int(-increment,0,-increment), //lower left
        new Vector3Int(-increment,0,0), //left
        new Vector3Int(-increment,0,increment), //upper left
        new Vector3Int(0,0,2*increment), //up up
        new Vector3Int(increment,0,2*increment), //up up right
        new Vector3Int(2*increment,0,2*increment), //upper right corner
        new Vector3Int(2*increment,0,increment), //up right right
        new Vector3Int(2*increment,0,0), //right right
        new Vector3Int(2*increment,0,-increment), //right right down
        new Vector3Int(2*increment,0,-2*increment), //lower right corner
        new Vector3Int(increment,0,-2*increment), //down down right
        new Vector3Int(0,0,-2*increment), //down down
        new Vector3Int(-increment,0,-2*increment), //down down left
        new Vector3Int(-2*increment,0,-2*increment), //lower left corner
        new Vector3Int(-2*increment,0,-increment), //left left down
        new Vector3Int(-2*increment,0,0), //left left
        new Vector3Int(-2*increment,0,increment), //left left up
        new Vector3Int(-2*increment,0,2*increment), //upper left corner
        new Vector3Int(-increment,0,2*increment), //up up left
    };

    public enum State { FOURWAY, FOURWAYINCREMENT, EIGHTWAY, EIGHTWAYTWODEEP, EIGHTWAYINCREMENT, CITYRADIUS };

    public List<Vector3Int> GetNeighborsFor(Vector3Int worldTilePosition, State criteria)
    {
        List<Vector3Int> neighbors = new();
        List<Vector3Int> listToUse = new();
        switch (criteria)
        {
            case State.FOURWAY:
                listToUse = new(neighborsFourDirections);
                break;
            case State.FOURWAYINCREMENT:
                listToUse = new(neighborsFourDirectionsIncrement);
                break;
            case State.EIGHTWAY:
                listToUse = new(neighborsEightDirections);
                break;
            case State.EIGHTWAYTWODEEP:
                listToUse = new(neighborsEightDirectionsTwoDeep);
                break;
            case State.EIGHTWAYINCREMENT:
                listToUse = new(neighborsEightDirectionsIncrement);
                break;
            case State.CITYRADIUS:
                listToUse = new(cityRadius);
                break;
        }

        foreach (Vector3Int direction in listToUse)
        {
            Vector3Int checkPosition = worldTilePosition + direction;
            if (world.ContainsKey(checkPosition)) //checking if it exists in world
                neighbors.Add(checkPosition);
        }
        return neighbors;
    }

    public List<Vector3Int> GetNeighborsCoordinates(State criteria)
    {
        List<Vector3Int> neighbors = new();
        switch (criteria)
        {
            case State.FOURWAY:
                return new(neighborsFourDirections);
            case State.FOURWAYINCREMENT:
                return new(neighborsFourDirectionsIncrement);
            case State.EIGHTWAY:
                return new(neighborsEightDirections);
            case State.EIGHTWAYTWODEEP:
                return new(neighborsEightDirectionsTwoDeep);
            case State.CITYRADIUS:
                return new(cityRadius);
        }

        return neighbors;
    }

    public (List<Vector3Int>, List<Vector3Int>, List<Vector3Int>) GetCityRadiusFor(Vector3Int worldTilePosition, GameObject city) //two ring layer around specific city
    {
        List<Vector3Int> neighbors = new();
        List<Vector3Int> developed = new();
        List<Vector3Int> constructing = new();
        foreach (Vector3Int direction in cityRadius)
        {
            Vector3Int checkPosition = worldTilePosition + direction;
            if (world.ContainsKey(checkPosition)) //checking if it exists in world
            {
                if (cityWorkedTileDict.ContainsKey(checkPosition) && GetCityLaborForTile(checkPosition) != city)
                    continue;

                if (unclaimedSingleBuildList.Contains(checkPosition))
                    continue;

                neighbors.Add(checkPosition);
                if (CheckIfTileIsImproved(checkPosition))
                    developed.Add(checkPosition);
                else if (CheckIfTileIsUnderConstruction(checkPosition))
                    constructing.Add(checkPosition);
            }

        }
        return (neighbors, developed, constructing);
    }

    public List<Vector3Int> GetWorkedCityRadiusFor(Vector3Int worldTilePosition, GameObject city) //two ring layer around specific city
    {
        List<Vector3Int> neighbors = new();
        foreach (Vector3Int direction in cityRadius)
        {
            Vector3Int checkPosition = worldTilePosition + direction;
            if (world.ContainsKey(checkPosition)) //checking if it exists in world
            {
                if (cityWorkedTileDict.ContainsKey(checkPosition) && GetCityLaborForTile(checkPosition) == city)//if city has worked tiles, add to list
                    neighbors.Add(checkPosition);
            }

        }
        return neighbors;
    }

    //to see what is developed for a city and what's worked for the city specifically
    public List<Vector3Int> GetPotentialLaborLocationsForCity(Vector3Int cityTile, GameObject city)
    {
        List<Vector3Int> neighbors = new();

        foreach (Vector3Int direction in cityRadius)
        {
            Vector3Int neighbor = cityTile + direction;

            if (world.ContainsKey(neighbor) && CheckIfTileIsImproved(neighbor))
            {
                if ((cityWorkedTileDict.ContainsKey(neighbor) && GetCityLaborForTile(neighbor) != city) || CheckIfTileIsMaxxed(neighbor))
                    continue;

                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    public (List<(Vector3Int, bool, int[])>, int[], int[]) GetRoadNeighborsFor(Vector3Int position, bool removing)
    {
        List<(Vector3Int, bool, int[])> neighbors = new();
        int[] straightRoads = { 0, 0, 0, 0 };
        int[] diagRoads = { 0, 0, 0, 0 }; 
        int i = 0;
        foreach (Vector3Int direction in neighborsEightDirectionsIncrement)
        {
            Vector3Int neighbor = direction + position;
            bool straightFlag = i % 2 == 0;

            if (roadTileDict.ContainsKey(neighbor))
            {
                int j = 0;
                int[] neighborRoads = { 0, 0, 0, 0 };
                //int neighborCount = 0;
                //if (removing)
                //{
                //    if (i > 3)
                //        RemoveRoadMapIcon(neighbor, i - 3);
                //    else
                //        RemoveRoadMapIcon(neighbor, i + 5);
                //}
                //else
                //{
                //    SetRoadMapIcon(position, i + 1);
                //    if (i > 3)
                //        SetRoadMapIcon(neighbor, i - 3);
                //    else
                //        SetRoadMapIcon(neighbor, i + 5);
                //}

                List<Vector3Int> neighborDirectionList = straightFlag ? neighborsFourDirectionsIncrement : neighborsDiagFourDirectionsIncrement;
                foreach (Vector3Int neighborDirection in neighborDirectionList)
                {
                    if (roadTileDict.ContainsKey(neighbor + neighborDirection))
                    {
                        neighborRoads[j] = 1;
                        //neighborCount++;
                    }
                    j++;
                }

                neighbors.Add((neighbor,straightFlag,neighborRoads)); 
                if (straightFlag)
                    straightRoads[i/2] = 1;
                else 
                    diagRoads[i/2] = 1;
            }
            i++;
        }

        return (neighbors, straightRoads, diagRoads);
    }

    public bool SoloRoadCheck(Vector3Int neighbor, bool straightFlag)
    {
        List<Vector3Int> neighborDirectionList = !straightFlag ? neighborsFourDirectionsIncrement : neighborsDiagFourDirectionsIncrement;
        bool soloRoad = true;

        foreach (Vector3Int neighborDirection in neighborDirectionList)
        {
            if (roadTileDict.ContainsKey(neighbor + neighborDirection))
                soloRoad = false;
        }

        return soloRoad;
    }

    public bool IsTileOpenCheck(Vector3Int tile)
    {
        if (IsBuildLocationTaken(tile) || IsRoadOnTerrain(tile) || CheckIfTileIsUnderConstruction(tile) || IsWorkerWorkingAtTile(tile))
            return false;
        else
            return true;
    }

    public bool IsTileOpenButRoadCheck(Vector3Int tile)
    {
        if (IsBuildLocationTaken(tile) || CheckIfTileIsUnderConstruction(tile) || IsWorkerWorkingAtTile(tile))
            return false;
        else
            return true;
    }

    public Vector3Int GetClosestTerrainLoc(Vector3 v)
    {
        //c# by default rounds to the closest even number at the midpoint. 
        Vector3Int vInt = new Vector3Int(0, 0, 0);

        vInt.y = 0;
        vInt.x = (int)Math.Round(v.x, MidpointRounding.AwayFromZero);
        vInt.z = (int)Math.Round(v.z, MidpointRounding.AwayFromZero);

        return world[vInt].TileCoordinates;
    }

    public Vector3Int RoundToInt(Vector3 v)
    {
        Vector3Int vInt = new Vector3Int(0,0,0);
        
        vInt.y = 0;
        vInt.x = (int)Math.Round(v.x, MidpointRounding.AwayFromZero);
        vInt.z = (int)Math.Round(v.z, MidpointRounding.AwayFromZero);

        return vInt;
    }

    public void AddCityName(string cityName, Vector3Int cityLoc)
    {
        cityNameDict[cityName] = cityLoc;
        cityLocDict[cityLoc] = cityName;
    }

    public void AddStructure(Vector3Int position, GameObject structure) //method to add building to dict
    {
        if (buildingPosDict.ContainsKey(position))
        {
            Debug.LogError($"There is a structure already at this position {position}");
            return;
        }

        buildingPosDict[position] = structure;
    }

    //public void AddStructureMap(Vector3Int pos, Sprite sprite)
    //{
    //    mapPanel.SetImprovement(pos, sprite);
    //}

    //public void RemoveStructureMap(Vector3Int pos)
    //{
    //    mapPanel.RemoveImprovement(pos);
    //}

    //public TMP_Text SetCityTileMap(Vector3Int pos, string name)
    //{
    //    //mapPanel.SetTileSprite(pos, TerrainDesc.City);
    //    //return mapPanel.CreateCityText(pos, name);
    //}

    //public void ResetTileMap(Vector3Int pos)
    //{
    //    mapPanel.SetTileSprite(pos, GetTerrainDataAt(pos).terrainData.terrainDesc);
    //}

    //public GameObject CreateMapIcon(Sprite sprite)
    //{
    //    return mapPanel.CreateUnitIcon(sprite);
    //}

    //public void SetMapIconLoc(Vector3Int loc, GameObject icon)
    //{
    //    mapPanel.SetIconTile(loc, icon);
    //}

    //public void SetRoadMapIcon(Vector3Int loc, int num)
    //{
    //    mapPanel.SetRoad(loc, num);
    //}

    //public void RemoveRoadMapIcon(Vector3Int loc, int num)
    //{
    //    mapPanel.RemoveRoad(loc, num);
    //}

    //public void RemoveAllRoadIcons(Vector3Int loc)
    //{
    //    mapPanel.RemoveAllRoads(loc);
    //}

    //public void MoveWorkerIcon(Vector3Int pos, float movement)
    //{
    //    mapPanel.MoveWorker(pos, movement);
    //}
    public void AddTradeCenterName(GameObject nameMap)
    {
        cityNamesMaps.Add(nameMap);
    }

    public void AddCity(Vector3 buildPosition, City city)
    {
        Vector3Int position = Vector3Int.RoundToInt(buildPosition);
        cityLocations.Add(position);
        cityDict[position] = city;
        cityNamesMaps.Add(city.cityNameMap);

        foreach (Vector3Int tile in neighborsFourDirections)
        {
            cityLocations.Add(tile + position);
        }
    }

    public void ShowCityNamesMap()
    {
        foreach (GameObject go in cityNamesMaps)
        {
            go.SetActive(true);
        }
    }

    public void HideCityNamesMap()
    {
        foreach (GameObject go in cityNamesMaps)
        {
            go.SetActive(false);
        }
    }

    public void AddResourceProducer(Vector3 buildPosition, ResourceProducer resourceProducer)
    {
        Vector3Int position = RoundToInt(buildPosition);
        cityImprovementProducerDict[position] = resourceProducer;
    }

    public void AddCityBuildingDict(Vector3 cityPos)
    {
        Vector3Int cityTile = Vector3Int.RoundToInt(cityPos);
        cityBuildingGODict[cityTile] = new Dictionary<string, GameObject>();
        cityBuildingDict[cityTile] = new Dictionary<string, CityImprovement>();
        cityBuildingCurrentWorkedDict[cityTile] = new Dictionary<string, int>();
        //cityBuildingMaxWorkedDict[cityTile] = new Dictionary<string, int>();
        cityBuildingList[cityTile] = new List<string>();
        //cityBuildingIsProducer[cityTile] = new Dictionary<string, ResourceProducer>();
    }

    public int CityCount()
    {
        return cityNameDict.Count;
    }

    public void RemoveCityBuilding(Vector3Int cityTile, string buildingName) 
    {
        cityBuildingGODict[cityTile].Remove(buildingName);
        cityBuildingDict[cityTile].Remove(buildingName);
        cityBuildingCurrentWorkedDict[cityTile].Remove(buildingName);
        //cityBuildingMaxWorkedDict[cityTile].Remove(buildingName);
        cityBuildingList[cityTile].Remove(buildingName);
        //cityBuildingIsProducer[cityTile].Remove(buildingName);
    }

    public void RemoveCityName(Vector3Int cityLoc)
    {
        string cityName = cityLocDict[cityLoc];
        cityNameDict.Remove(cityName);
        cityLocDict.Remove(cityLoc);
    }

    public void RemoveCityNameMap(Vector3Int cityLoc)
    {
        cityNamesMaps.Remove(GetCity(cityLoc).cityNameMap);
    }

    public void RemoveWonderName(string name)
    {
        wonderConstructionDict.Remove(name);
    }

    public void RemoveStructure(Vector3Int buildPosition)
    {
        buildingPosDict.Remove(buildPosition);
        if (cityImprovementDict.ContainsKey(buildPosition))
        {
            cityImprovementDict.Remove(buildPosition);
            cityImprovementProducerDict.Remove(buildPosition);
        }
        if (cityBuildingGODict.ContainsKey(buildPosition)) //if destroying city, destroy all buildings within
        {
            bool isHill = GetTerrainDataAt(buildPosition).terrainData.type == TerrainType.Hill;
            foreach (string building in cityBuildingGODict[buildPosition].Keys)
            {
                CityImprovement improvement = cityBuildingDict[buildPosition][building];
                //improvement.DestroyPS();
                improvement.PlayRemoveEffect(isHill);
                Destroy(cityBuildingGODict[buildPosition][building]);
            }
            
            cityBuildingGODict.Remove(buildPosition);
            cityBuildingDict.Remove(buildPosition);
            //cityBuildingMaxWorkedDict.Remove(buildPosition);
            cityBuildingList.Remove(buildPosition);
            //cityBuildingIsProducer.Remove(buildPosition);

            cityLocations.Remove(buildPosition);
            cityDict.Remove(buildPosition);
            foreach (Vector3Int tile in neighborsEightDirections)
            {
                cityLocations.Remove(buildPosition + tile);
            }
        }
    }

    public void RemoveConstruction(Vector3Int tile)
    {
        cityImprovementConstructionDict.Remove(tile);   
    }

    public void RemoveHarbor(Vector3Int harborLoc)
    {
        cityDict.Remove(harborLoc);
    }

    public void RemoveRoad(Vector3Int buildPosition)
    {
        roadTileDict.Remove(buildPosition);
    }

    public Vector3Int AddUnitPosition(Vector3 unitPosition, Unit unit)
    {
        Vector3Int position = RoundToInt(unitPosition);

        //checking if same unit doesn't already have a tile occupied (just in case)
        if (unitPosDict.ContainsValue(unit))
        {
            var item = unitPosDict.First(key => key.Value == unit);
            unitPosDict.Remove(item.Key);
        }

        unitPosDict[position] = unit;

        return position;
    }

    public bool UnitAlreadyThere(Unit unit, Vector3Int position)
    {
        if (unitPosDict.ContainsKey(position))
            return unitPosDict[position] == unit;

        return false;
    }

    public void RemoveUnitPosition(Vector3Int position/*, GameObject unitGO*/)
    {
        //Vector3Int position = Vector3Int.RoundToInt(unitPosition);

        unitPosDict.Remove(position);
    }

    public void AddToUnclaimedSingleBuild(Vector3Int location)
    {
        unclaimedSingleBuildList.Add(location);
    }

    public bool CheckIfUnclaimedSingleBuild(Vector3Int location)
    {
        return unclaimedSingleBuildList.Contains(location);
    }

    public void RemoveFromUnclaimedSingleBuild(Vector3Int location)
    {
        unclaimedSingleBuildList.Remove(location);
    }




    //for assigning labor
    public void AddToCurrentFieldLabor(Vector3Int pos, int current)
    {
        currentWorkedTileDict[pos] = current;
    }

    public void AddToCurrentBuildingLabor(Vector3Int cityTile, string buildingName, int current)
    {
        cityBuildingCurrentWorkedDict[cityTile][buildingName] = current;
    }

    public void AddToMaxLaborDict(Vector3 pos, int max) //only adding to max labor when improvements are built, hence Vector3
    {
        Vector3Int posInt = Vector3Int.RoundToInt(pos);
        maxWorkedTileDict[posInt] = max;
    }

    //public void AddToCityMaxLaborDict(Vector3Int cityTile, string buildingName, int max)
    //{
    //    cityBuildingMaxWorkedDict[cityTile][buildingName] = max;
    //}

    //public void AddToCityBuildingList(Vector3Int cityTile, string buildingName)
    //{
    //    cityBuildingList[cityTile].Add(buildingName);
    //}

    //public void AddToCityBuildingIsProducerDict(Vector3Int cityTile, string buildingName, ResourceProducer resourceProducer)
    //{
    //    cityBuildingIsProducer[cityTile][buildingName] = resourceProducer;
    //}

    public void AddToCityLabor(Vector3Int pos, GameObject city)
    {
        cityWorkedTileDict[pos] = city;
    }

    //public bool CheckIfCityOwnsTile(Vector3Int pos, GameObject city)
    //{
    //    if (cityWorkedTileDict.ContainsKey(pos))
    //    {
    //        return (cityWorkedTileDict[pos] == city);
    //    }

    //    return true; //if no one owns it, then city owns it
    //}


    public int GetCurrentLaborForTile(Vector3Int pos)
    {
        if (currentWorkedTileDict.ContainsKey(pos))
            return currentWorkedTileDict[pos];
        return 0;
    }

    public int GetCurrentLaborForBuilding(Vector3Int cityTile, string buildingName)
    {
        if (cityBuildingCurrentWorkedDict[cityTile].ContainsKey(buildingName))
            return cityBuildingCurrentWorkedDict[cityTile][buildingName];
        return 0;
    }

    public int GetMaxLaborForTile(Vector3Int pos)
    {
        return maxWorkedTileDict[pos];
    }

    //public int GetMaxLaborForBuilding(Vector3Int cityTile, string buildingName)
    //{
    //    return cityBuildingMaxWorkedDict[cityTile][buildingName];
    //}

    public List<string> GetBuildingListForCity(Vector3Int cityTile)
    {
        if (cityBuildingList.ContainsKey(cityTile))
            return cityBuildingList[cityTile];
        else
        {
            List<string> noList = new();
            return noList;
        }
    }

    public bool CheckImprovementIsProducer(Vector3Int pos)
    {
        return cityImprovementProducerDict.ContainsKey(pos);
    }

    //public bool CheckBuildingIsProducer(Vector3Int cityTile, string buildingName)
    //{
    //    return cityBuildingIsProducer[cityTile].ContainsKey(buildingName);
    //}

    private GameObject GetCityLaborForTile(Vector3Int pos)
    {
        return cityWorkedTileDict[pos];
    }

    public bool CheckIfCityOwnsTile(Vector3Int pos)
    {
        return cityWorkedTileDict.ContainsKey(pos);
    }

    public bool CheckIfTileIsWorked(Vector3Int pos)
    {
        return currentWorkedTileDict.ContainsKey(pos);
    }

    public bool CheckIfTileIsImproved(Vector3Int pos)
    {
        return maxWorkedTileDict.ContainsKey(pos);
    }

    public bool CheckIfTileIsUnderConstruction(Vector3Int pos)
    {
        return cityImprovementConstructionDict.ContainsKey(pos);
    }

    public bool CheckIfTileIsMaxxed(Vector3Int pos)
    {
        if (currentWorkedTileDict.ContainsKey(pos))
            return maxWorkedTileDict[pos] == currentWorkedTileDict[pos];
        return false;
    }

    //public bool CheckIfBuildingIsMaxxed(Vector3Int cityTile, string buildingName)
    //{
    //    if (cityBuildingCurrentWorkedDict[cityTile].ContainsKey(buildingName))
    //        return cityBuildingMaxWorkedDict[cityTile][buildingName] == cityBuildingCurrentWorkedDict[cityTile][buildingName];
    //    return false;
    //}

    //public bool CheckIfTileHasBuildings(Vector3Int cityTile)
    //{
    //    return cityBuildingGODict.ContainsKey(cityTile);
    //}

    public bool CheckCityName(string cityName)
    {
        return cityNameDict.ContainsKey(cityName);
    }

    public string PrepareLaborNumbers(Vector3Int pos)
    {
        return GetCurrentLaborForTile(pos) + "/" + GetMaxLaborForTile(pos);
    }

    public void RemoveTerrain(Vector3Int tile)
    {
        world.Remove(tile);
    }

    public void RemoveFromCurrentWorked(Vector3Int pos)
    {
        if (currentWorkedTileDict.ContainsKey(pos))
        {
            currentWorkedTileDict.Remove(pos);
        }
    }

    public void RemoveFromBuildingCurrentWorked(Vector3Int cityTile, string buildingName)
    {
        if (cityBuildingCurrentWorkedDict[cityTile].ContainsKey(buildingName))
        {
            cityBuildingCurrentWorkedDict[cityTile].Remove(buildingName);
        }
    }

    public void RemoveFromMaxWorked(Vector3Int pos) //only removing when improvements are destroyed
    {
        maxWorkedTileDict.Remove(pos);
    }

    public void RemoveFromCityLabor(Vector3Int pos)
    {
        if (cityImprovementDict[pos].GetImprovementData.singleBuild)
            return;
        
        if (cityWorkedTileDict.ContainsKey(pos))
            cityWorkedTileDict.Remove(pos);
    }

    public void RemoveSingleBuildFromCityLabor(Vector3Int pos)
    {
        if (cityWorkedTileDict.ContainsKey(pos))
            cityWorkedTileDict.Remove(pos);
    }

    public void PlayMessage(Vector3 loc)
    {
        speechBubble.SetText(loc, "This is a test. This is only a test.");
    }

    public void StopMessage()
    {
        speechBubble.CancelText();
    }


    //debug gizmos
    private void OnDrawGizmos() //for highlighting difficulty of terrain
    {
        if (!Application.isPlaying)
            return;
        DrawMovementCostGizmoOf(Color.green, showGizmo);
        //DrawGizmoOf(TerrainType.Difficult, Color.yellow, showDifficult);
        //DrawGizmoOf(TerrainType.Obstacle, Color.red, showObstacle);
        //DrawGizmoOf(TerrainType.Moveable, Color.green, showGround);
        //DrawGizmoOf(TerrainType.Sea, Color.blue, showSea);
    }

    //private void DrawGizmoOf(TerrainType type, Color color, bool isShowing) //for highlighting difficulty of terrain
    //{
    //    if (isShowing)
    //    {
    //        Gizmos.color = color;
    //        foreach (Vector3Int td in world.Keys)
    //        {
    //            if (world[td].GetTerrainData().type == type)
    //            {
    //                Vector3Int pos = td;
    //                if (type == TerrainType.Obstacle)
    //                {
    //                    Gizmos.DrawSphere(new Vector3(pos.x, pos.y + 1.5f, pos.z), 0.3f); //draws spheres of 0.3 size on each tile
    //                }
    //                else
    //                {
    //                    Gizmos.DrawSphere(new Vector3(pos.x, pos.y + 0.5f, pos.z), 0.3f); //draws spheres of 0.3 size on each tile
    //                }

    //            }
    //        }
    //    }
    //}


    private void DrawMovementCostGizmoOf(Color color, bool isShowing) //for highlighting difficulty of terrain
    {
        if (isShowing)
        {
            Gizmos.color = color;
            //foreach (Vector3Int td in world.Keys)
            //{
            //    Vector3Int pos = td;

            //    //for movement cost
            //    //int movementCost = GetTerrainDataAt(pos).MovementCost;
            //    //float movementCostFloat = (float)movementCost;
            //    //Gizmos.DrawSphere(new Vector3(pos.x, pos.y + 1.5f, pos.z), movementCostFloat / 30); //draws spheres of 0.3 size on each tile

            //    //for hasRoad flag
            //    if (!GetTerrainDataAt(pos).hasRoad)
            //    {
            //        Gizmos.DrawSphere(new Vector3(pos.x, pos.y + 1.5f, pos.z), .5f);
            //    }
            //}

            foreach (Vector3Int pos in unitPosDict.Keys)
            {
                //for isTrader flag
                if (unitPosDict[pos].GetComponent<Unit>().isTrader)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(new Vector3(pos.x, pos.y + 1.5f, pos.z), .2f);
                }
                else
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(new Vector3(pos.x, pos.y + 1.5f, pos.z), .2f);
                }
            }
        }
    }
}

public enum Era
{
    BronzeAge,
    IronAge,
    ClassicAge,
    MedievalAge,
    Renaissance,
    IndustrialEra,
    ModernEra
}
