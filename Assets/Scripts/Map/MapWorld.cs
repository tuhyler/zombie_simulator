using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

public class MapWorld : MonoBehaviour
{
    private string version = "0.1";
    private string consoleInput;
    [HideInInspector]
    public int seed;
    private DateTime currentTime;
    [HideInInspector]
    public Era currentEra = Era.AncientEra;
    [HideInInspector]
    public Region startingRegion = Region.South;
    [SerializeField]
    public HandlePlayerInput playerInput;
    [SerializeField]
    public Worker mainPlayer, scott;
    [SerializeField]
    public BodyGuard azai;
    //[SerializeField]
    //public Unit /*scott, */azai;
    [SerializeField]
    public Water water;
    [SerializeField]
    public GameObject mainCam, battleCamera, /*resourceIcon, */speechBubble/*, uiHelperWindow*/;
    [SerializeField]
    public CameraController cameraController;
    [SerializeField]
    public Canvas immoveableCanvas, cityCanvas, workerCanvas, traderCanvas, tradeRouteManagerCanvas, infoPopUpCanvas, overflowGridCanvas, personalResourceCanvas, tcCanvas, wonderCanvas, mainCanvas;
    [HideInInspector]
    public bool tutorial, hideUI, scottFollow, azaiFollow, bridgeResearched, waterTransport, airTransport;
    [SerializeField]
    public DayNightCycle dayNightCycle;
    [SerializeField]
    public MeshFilter borderZero, borderOne, borderTwoCorner, borderTwoCross, borderThree, borderFour;
    [SerializeField]
    public UtilityCostDisplay utilityCostDisplay;
    [SerializeField]
    public UIAttackWarning uiAttackWarning;
    [SerializeField]
    public UIWorldResources uiWorldResources;
    [SerializeField]
    public UIProfitabilityStats uiProfitabilityStats;
    [SerializeField]
    public UIResearchTreePanel researchTree;
    [SerializeField]
    public ButtonHighlight buttonHighlight;
    [SerializeField]
    public UIMapHandler mapHandler;
    //[SerializeField]
    //private UIMapPanel mapPanel;
    [SerializeField]
    public UISingleConditionalButtonHandler wonderButton, uiConfirmWonderBuild, uiRotateWonder, uiMainMenuButton, conversationListButton;
    [SerializeField]
    public RectTransform mapPanelButton;
    [SerializeField]
    private UIWonderHandler wonderHandler;
    [SerializeField]
    public UIMainMenu uiMainMenu;
	[SerializeField]
    private UIBuildingSomething uiBuildingSomething;
    [SerializeField]
    public UITerrainTooltip uiTerrainTooltip;
    [SerializeField]
    public UICityImprovementTip uiCityImprovementTip;
    [SerializeField]
    public UICampTip uiCampTooltip;
    [SerializeField]
    public UITradeRouteBeginTooltip uiTradeRouteBeginTooltip;
    [SerializeField]
    public UICityPopIncreasePanel uiCityPopIncreasePanel;
	[SerializeField]
	public UILaborDestinationWindow uiLaborDestinationWindow;
	[SerializeField]
    public UITomFinder uiTomFinder;
    [SerializeField]
    public UIInfoPopUpHandler uiInfoPopUpHandler;
    [SerializeField]
    public UISpeechWindow uiSpeechWindow;
    [SerializeField]
    public UIConversationTaskManager uiConversationTaskManager;
    [SerializeField]
    public UIResourceGivingPanel uiResourceGivingPanel;
    [SerializeField]
    public UnitMovement unitMovement;
    [SerializeField]
    public CityBuilderManager cityBuilderManager;
    [SerializeField]
    public RoadManager roadManager;
    [SerializeField]
    public Material transparentMat, atlasMain/*, atlasSemiClear*/;
    [SerializeField]
    private GameObject canvasHolder;
    private GameObject selectionIcon;
    [SerializeField]
    public UnitBuildDataSO laborerData;

    [SerializeField]
    public Transform terrainHolder, picGOHolder, cityHolder, wonderHolder, tradeCenterHolder, psHolder, enemyCityHolder, unitHolder, enemyUnitHolder, roadHolder, orphanImprovementHolder, objectPoolItemHolder;
    [SerializeField]
    public LayerMask enemyKillLayerMask, unitMask;
    [SerializeField]
    public List<ResourceValue> laborTransferCost;

	//for worker and army orders
	[HideInInspector]
	public bool buildingRoad, buildingLiquid, buildingPower, removing, removingAll, removingRoad, removingLiquid, removingPower, swappingArmy, deployingArmy, changingCity, assigningGuard, attackMovingTarget;

    [HideInInspector]
    public IGoldUpdateCheck goldUpdateCheck;
    [HideInInspector]
    public ITooltip iTooltip;
    [HideInInspector]
    public IImmoveable iImmoveable;
	[HideInInspector]
    public Vector3Int startingLoc;
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
    private bool sideways, showConsole;
    [HideInInspector]
    public List<Wonder> allWonders = new();
    private HashSet<Vector3Int> wonderTiles = new();
    private GameObject wonderGhost;

    //trade center info
    [SerializeField]
    private Sprite tradeCenterMapIcon;
    [HideInInspector]
    public List<TradeCenter> allTradeCenters = new();
	private Dictionary<Vector3Int, List<GameObject>> centerBordersDict = new();

	//miscellaneous sprites
	[SerializeField]
    public Sprite rocksNormal, rocksLuxury, rocksChemical;

    //world resource info
    [HideInInspector]
    public WorldResourceManager worldResourceManager;
    [HideInInspector]
    public bool researching;
    [HideInInspector]
    public HashSet<ResourceProducer> researchWaitList = new();
    [HideInInspector]
    public List<IGoldWaiter> goldWaitList = new();
    //resource multiplier
    //public Dictionary<ResourceType, float> resourceStorageMultiplierDict = new();

	private Dictionary<Vector3Int, TerrainData> world = new();
    private Dictionary<Vector3Int, GameObject> buildingPosDict = new(); //to see if cities already exist in current location
    [HideInInspector] 
    public HashSet<Vector3Int> tempNoWalkList = new(), tempBattleZone = new(); //in case player is moving when added to no walk list, where battle positions will be
    private HashSet<Vector3Int> noWalkList = new(); //tiles where wonders are and units can't walk
    private List<GameObject> cityNamesMaps = new();

    public Dictionary<Vector3Int, City> cityDict = new(); 
    public Dictionary<Vector3Int, CityImprovement> cityImprovementDict = new(); //all the City development locs
    public Dictionary<Vector3Int, Dictionary<string, CityImprovement>> cityBuildingDict = new(); //all the buildings for highlighting
    public Dictionary<Vector3Int, Vector3Int> cityImprovementQueueList = new();
    [HideInInspector]
    public HashSet<Vector3Int> unclaimedSingleBuildList = new ();
    private Dictionary<Vector3Int, ITradeStop> tradeStopDict = new();
    private Dictionary<string, ITradeStop> tradeStopNameDict = new();
    public Dictionary<Vector3Int, List<Trader>> traderPosDict = new();
    public Dictionary<Vector3Int, TraderStallManager> traderStallDict = new();
    //unit positions
    public Dictionary<Vector3Int, Unit> playerPosDict = new();
    public Dictionary<Vector3Int, Unit> unitPosDict = new(); //to track unitGO locations
    public Dictionary<Vector3Int, Unit> npcPosDict = new();
    [HideInInspector]
    public List<Laborer> laborerList = new();
    [HideInInspector]
    public List<Trader> traderList = new();
    [HideInInspector]
    public List<Transport> transportList = new();
    public Dictionary<string, int> upgradeableObjectMaxLevelDict = new();

    //for assigning labor in cities
    //public Dictionary<Vector3Int, int> currentWorkedTileDict = new(); //to see how much labor is assigned to tile
    //private Dictionary<Vector3Int, int> maxWorkedTileDict = new(); //the max amount of labor that can be assigned to tile
    public Dictionary<Vector3Int, Vector3Int?> cityWorkedTileDict = new(); //the city worked tiles belong to

    //for workers
    private Vector3Int workerBusyLocation = new Vector3Int(0, -10, 0);

    //for PathFinding
    private PathNode[,] grid;

    //for roads
    public Dictionary<Vector3Int, List<Road>> roadTileDict = new(); //stores road GOs, only on terrain locations
    private HashSet<Vector3Int> soloRoadLocsList = new(); //indicates which tiles have solo roads on them
    private HashSet<Vector3Int> roadLocsList = new(); //indicates which tiles have roads on them
    private int roadCost; //set in road manager

    //for terrain speeds
    public TerrainDataSO flatland, forest, hill, forestHill;
    //for boats to avoid traveling the coast
    private HashSet<Vector3Int> coastCoastList = new();

    //for npcs
    public Dictionary<string, TradeRep> allTCReps = new();
    [HideInInspector]
    public List<MilitaryLeader> allEnemyLeaders = new();

    //for enemy
    private Dictionary<Vector3Int, EnemyCamp> enemyCampDict = new();
    private Dictionary<Vector3Int, List<GameObject>> enemyBordersDict = new();
    public Dictionary<Vector3Int, EnemyAmbush> enemyAmbushDict = new();
    public Dictionary<Vector3Int, City> enemyCityDict = new();
    public int waitTillAttackTime = 600, maxDistance, minDistance, enemyUnitGrowthTime = 20, enemyStartAttackLevel = 3, ambushProb, maxPriceDiff = 5;
    public float obsoleteResourceReduction = 0.75f;
    [HideInInspector]
    public HashSet<Vector3Int> militaryStationLocs = new(); //for traveling around barracks
    public Dictionary<Vector3Int, TreasureChest> treasureLocs = new();
    private HashSet<Vector3Int> neutralZones = new(); //areas around cities that can be traveled into to talk

    //for resource icons on minimap (so they're rotated correctly)
    private Dictionary<Vector3Int, ResourceMinimapIcon> resourceIconDict = new();

    //for expanding gameobject size
    private static int increment = 3;

    [HideInInspector]
    public bool moveUnit, unitOrders, buildingWonder, tooltip, somethingSelected, selectingUnit, citySelected, cityUnitSelected, enemyAttackBegin, upgrading, upgradingUnit;
    //private bool showObstacle, showDifficult, showGround, showSea;

    //for resources
    public Dictionary<ResourceType, float> resourcePurchaseAmountDict = new();

    //for tracking stats
    [HideInInspector]
    public int ambushes, cityCount, infantryCount, rangedCount, cavalryCount, traderCount, boatTraderCount, laborerCount, food, lumber, popGrowth, popLost, enemyCount, militaryCount, maxResearchLevel;
    [HideInInspector]
    public string tutorialStep, gameStep;
    [HideInInspector]
    public bool flashingButton;
    private List<string> cityNamePool;

    //for when terrain runs out of resources
    [SerializeField]
    public TerrainDataSO grasslandTerrain, grasslandHillTerrain, desertTerrain, desertHillTerrain;

    public bool hideTerrain = true, showAllBuildOptions = true;

    [SerializeField]
    public AudioManager ambienceAudio, musicAudio;
    [SerializeField]
    public AudioClip newWorldSong, badGuySong, congratsSong;
    private AudioSource audioSource;

    //handling discovering resources
    List<UIResourceSelectionGrid> resourceSelectionGridList = new();
    [HideInInspector]
    public HashSet<ResourceType> resourceDiscoveredList = new(), sellableResourceList = new();

    [HideInInspector]
    public HashSet<string> newUnitsAndImprovements = new();

    [HideInInspector]
    public GamePersist gamePersist = new();

    //ambush info
    public Dictionary<Era, string> ambushUnitDict = new();
    //battle camera stuff
    private HashSet<Vector3Int> battleLocs = new();

    //character units
    [HideInInspector]
    public List<Unit> characterUnits;

    //permanent changes to cities, units, and city improvments (from completing wonders)
    private float cityWorkEthicChange;
    private int cityWarehouseStorageChange;    
    private Dictionary<string, float> unitsSpeedChangeDict = new();
    private Dictionary<ResourceType, float> resourceYieldChangeDict = new();


    private void Awake()
    {
		currentTime = DateTime.Now;
        uiSpeechWindow.AddToSpeakingDict("Camera", null);
        speechBubble.SetActive(false);
        audioSource = GetComponent<AudioSource>();
        cityNamePool = new() { "Haran", "Rhagae", "Ecbatana", "Teredon", "Susa", "Tarsus","Sardis","Tiphsah","Carmana","Bactria","Merv","Oxus","Dyrta","Nicaea","Arbela","Sardis","Zagros","Lydia"};

		if (!hideTerrain)
            cameraController.SetDefaultLimits();
        
        worldResourceManager = GetComponent<WorldResourceManager>();
        worldResourceManager.SetUI(uiWorldResources);
        if (researching)
            SetResearchName(researchTree.GetChosenResearchName());
        else
            SetResearchName("No Research");

        uiInfoPopUpHandler.SetWarningMessage(uiInfoPopUpHandler);
        uiInfoPopUpHandler.gameObject.SetActive(false);
    }

    private void Start()
    {
        CursorCheck();

		NewGamePrep(false);
		AddToDiscoverList(ResourceType.Gold);
        AddToDiscoverList(ResourceType.Research);

        //Debug.Log("Height is " + Screen.height + " and width is " + Screen.width);

        foreach (ImprovementDataSO data in UpgradeableObjectHolder.Instance.allBuildingsAndImprovements)
        {
            if (!data.isSecondary)
            {
                if (data.improvementNameAndLevel == "City-0")
                    continue;

                UIBuilderHandler builderHandler;

                if (data.rawMaterials)
                    builderHandler = cityBuilderManager.uiRawGoodsBuilder;
                else if (data.isBuilding)
				    builderHandler = cityBuilderManager.uiBuildingBuilder;
                else
				    builderHandler = cityBuilderManager.uiProducerBuilder;

                GameObject buildPanelGO = Instantiate(Resources.Load<GameObject>("Prefabs/UIPrefabs/BuildObjectPanel"));
                UIBuildOptions buildOption = buildPanelGO.GetComponent<UIBuildOptions>();
                buildOption.BuildData = data;
			    buildPanelGO.transform.SetParent(builderHandler.objectHolder, false);
			    buildOption.SetBuildOptionData(builderHandler);

                if (data.availableInitially || (showAllBuildOptions /*&& data.improvementLevel == 1*/))
                    SetUpgradeableObjectMaxLevel(data.improvementName, data.improvementLevel);
            }
        }

        if (showAllBuildOptions)
        {
			foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
			{
				if (!ResourceCheck(resource.resourceType))
					DiscoverResource(resource.resourceType);
			}
		}

        cityBuilderManager.uiRawGoodsBuilder.FinishMenuSetup();
		cityBuilderManager.uiBuildingBuilder.FinishMenuSetup();
		cityBuilderManager.uiProducerBuilder.FinishMenuSetup();

		//populating the upgradeableobjectdict, every one starts at level 1. 
		foreach (UnitBuildDataSO data in UpgradeableObjectHolder.Instance.allUnits)
        {
            if (data.unitDisplayName == "Azai")
            {
                if (data.availableInitially || showAllBuildOptions)
                    SetUpgradeableObjectMaxLevel("Azai", data.unitLevel);

				continue;
			}

            GameObject buildPanelGO = Instantiate(Resources.Load<GameObject>("Prefabs/UIPrefabs/BuildObjectPanel"));
			UIBuildOptions buildOption = buildPanelGO.GetComponent<UIBuildOptions>();
			buildOption.UnitBuildData = data;
			buildPanelGO.transform.SetParent(cityBuilderManager.uiUnitBuilder.objectHolder, false);
			buildOption.SetBuildOptionData(cityBuilderManager.uiUnitBuilder);

            if (data.availableInitially || showAllBuildOptions)
                SetUpgradeableObjectMaxLevel(data.unitType.ToString(), data.unitLevel);
		}

        cityBuilderManager.uiUnitBuilder.FinishMenuSetup();

        SetUpgradeableObjectMaxLevel(UtilityType.Road.ToString(), 1);
		SetUpgradeableObjectMaxLevel(UtilityType.Power.ToString(), 0);
		SetUpgradeableObjectMaxLevel(UtilityType.Water.ToString(), 0);

        foreach (WonderDataSO wonder in UpgradeableObjectHolder.Instance.allWonders)
        {
			GameObject wonderPanelGO = Instantiate(Resources.Load<GameObject>("Prefabs/UIPrefabs/BuildWonderPanel"));

			UIWonderOptions wonderOption = wonderPanelGO.GetComponent<UIWonderOptions>();
			wonderOption.BuildData = wonder;
			wonderPanelGO.transform.SetParent(wonderHandler.objectHolder, false);
			wonderOption.SetBuildOptionData(wonderHandler);
		}

        wonderHandler.FinishMenuSetup();

        //CreateParticleSystems();
        uiMainMenu.uiSaveGame.PopulateSaveItems();
        DeactivateCanvases();

        //populating ambush dict
        foreach (UnitBuildDataSO unit in UpgradeableObjectHolder.Instance.enemyUnitDict.Values)
        {
            if (unit.unitType == UnitType.Infantry && !unit.empireUnit)
                ambushUnitDict[unit.unitEra] = unit.unitNameAndLevel;
        }

        if (showAllBuildOptions)
            AaddGold(1000);
    }

    public void CursorCheck()
    {
		if (Screen.width > 3000)
			Cursor.SetCursor(Resources.Load<Texture2D>("Prefabs/MiscPrefabs/cursor_gold_big"), Vector2.zero, CursorMode.ForceSoftware);
		else if (Screen.width > 1920)
			Cursor.SetCursor(Resources.Load<Texture2D>("Prefabs/MiscPrefabs/cursor_gold_med"), Vector2.zero, CursorMode.ForceSoftware);
		else
			Cursor.SetCursor(Resources.Load<Texture2D>("Prefabs/MiscPrefabs/cursor_gold"), Vector2.zero, CursorMode.ForceSoftware);
	}

	public void NewGamePrep(bool newGame, Dictionary<Vector3Int, TerrainData> terrainDict = null, List<EnemyEmpire> enemyEmpires = null, List<Vector3Int> enemyRoadLocs = null, int width = 25, 
        int height= 25, int resources = 4, int enemies = 2, int islands = 15, bool tutorial = false)
    {
		wonderButton.gameObject.SetActive(true);
		uiMainMenuButton.gameObject.SetActive(true);
        conversationListButton.gameObject.SetActive(true);
		uiWorldResources.SetActiveStatus(true);
        this.tutorial = tutorial;
        GameLoader.Instance.gameData.tutorialData = new();
        GameLoader.Instance.gameData.width = width;
        GameLoader.Instance.gameData.height = height;
        GameLoader.Instance.gameData.resources = resources;
        GameLoader.Instance.gameData.enemies = enemies;
        GameLoader.Instance.gameData.islands = islands;
        GameLoader.Instance.gameData.tutorial = tutorial;
		if (tutorial)
		{
			GameObject helperWindow = Instantiate(Resources.Load<GameObject>("Prefabs/UIPrefabs/HelperWindow"));
			helperWindow.transform.SetParent(mainCanvas.transform, false);
			cityBuilderManager.uiHelperWindow = helperWindow.GetComponent<UIHelperWindow>();
		}
		List<TerrainData> coastalTerrain = new();
		List<TerrainData> terrainToCheck = new();

        if (newGame)
        {
            NewMap(terrainDict);
            characterUnits.Clear();
        }
        else
        {
            foreach (Transform go in terrainHolder)
		    {
			    if (go.TryGetComponent(out TerrainData td))
			    {
				    td.SetWorld(this);
				    td.SetProp();

                    td.SetVisibleProp();

				    if (td.isSeaCorner && !coastalTerrain.Contains(td))
					    coastalTerrain.Add(td);
				    td.SetTileCoordinates();
					td.SetData(td.terrainData);
				    Vector3Int tileCoordinate = td.TileCoordinates;
				    GameLoader.Instance.gameData.allTerrain[tileCoordinate] = td.SaveData();

				    world[tileCoordinate] = td;
				    terrainToCheck.Add(td);
				    td.CheckMinimapResource(mapHandler);

                    if (td.hasResourceMap)
						SetResourceMinimapIcon(td);

					if (hideTerrain)
                    {
					    td.Hide();
				    }
                    else
                    {
                        td.Discover();
                    
                        if (td.hasResourceMap)
							ToggleResourceIcon(td.TileCoordinates, true);
					}

				    //if (td.rawResourceType == RawResourceType.Rocks)
					   // td.PrepParticleSystem();

				    foreach (Vector3Int tile in neighborsEightDirections)
				    {
					    world[tileCoordinate + tile] = td;
				    }
			    }
		    }

            for (int i = 0; i < coastalTerrain.Count; i++)
                coastalTerrain[i].SetCoastCoordinates();

		    for (int i = 0; i < terrainToCheck.Count; i++)
                terrainToCheck[i].SetVisibleProp();
        }

		List<Vector3Int> enemyLocs = new();
		foreach (Transform go in enemyUnitHolder) //adds all enemy units to start game
		{
			Unit unitEnemy = go.GetComponent<Unit>();
            unitEnemy.SetMinimapIcon(enemyUnitHolder);
            unitEnemy.minimapIcon.gameObject.SetActive(false);
			unitEnemy.gameObject.name = unitEnemy.buildDataSO.unitDisplayName;
			unitEnemy.SetReferences(this);

			Vector3Int unitLoc = RoundToInt(unitEnemy.transform.position);
			if (!unitPosDict.ContainsKey(unitLoc)) //just in case dictionary was missing any
				unitEnemy.currentLocation = AddUnitPosition(unitLoc, unitEnemy);

			unitEnemy.currentLocation = unitLoc;

			Vector3Int unitTerrainLoc = GetClosestTerrainLoc(unitLoc);

			if (!enemyCampDict.ContainsKey(unitTerrainLoc))
			{
				EnemyCamp camp = new();
				camp.world = this;
				camp.loc = unitTerrainLoc;

				TerrainData tdCamp = GetTerrainDataAt(unitTerrainLoc);
				tdCamp.enemyCamp = true;
				tdCamp.enemyZone = true;
				AddToCityLabor(unitTerrainLoc, null);

				foreach (Vector3Int tile in GetNeighborsFor(unitTerrainLoc, State.EIGHTWAYINCREMENT))
				{
					AddToCityLabor(tile, null);
					TerrainData td = GetTerrainDataAt(tile);
					td.enemyZone = true;
				}

				enemyCampDict[unitTerrainLoc] = camp;
				enemyLocs.Add(unitTerrainLoc);
			}

			enemyCampDict[unitTerrainLoc].UnitsInCamp.Add(unitEnemy.military);
			unitEnemy.enemyAI.CampSpot = unitLoc;
			unitEnemy.military.enemyCamp = enemyCampDict[unitTerrainLoc];

			if (hideTerrain)
            {
                unitEnemy.gameObject.SetActive(false);
            }
            else
            {
			    if (unitEnemy.buildDataSO.unitType != UnitType.Cavalry)
				    unitEnemy.military.ToggleSitting(true);
            }
		}

		foreach (Vector3Int loc in enemyCampDict.Keys)
		{
            enemyCampDict[loc].FormBattlePositions();
            
            if (enemyCampDict[loc].campCount < 9)
            {
                GameObject fire = Instantiate(Resources.Load<GameObject>("Prefabs/MiscPrefabs/Campfire"));
                fire.transform.SetParent(terrainHolder, false);
				enemyCampDict[loc].SetCampfire(fire, world[loc].isHill, !hideTerrain);
            }

			GameLoader.Instance.gameData.enemyCampLocs[loc] = enemyCampDict[loc].SendCampData();
			Vector3 position = Vector3.zero;
			position.y += 1;
			GameObject icon = Instantiate(Resources.Load<GameObject>("Prefabs/InGameSpritePrefabs/MinimapEnemyCamp"));
			icon.transform.position = position;
			icon.transform.SetParent(GetTerrainDataAt(loc).transform, false);
			enemyCampDict[loc].minimapIcon = icon;
		}

		GameLoader.Instance.gameData.allTradeCenters.Clear();
		foreach (Transform go in tradeCenterHolder)
		{
			TradeCenter center = go.GetComponent<TradeCenter>();

			center.SetWorld(this);
			center.ToggleLights(false);
			center.SetName();
			center.SetPop(UnityEngine.Random.Range(4, 9));
			center.ClaimSpotInWorld(increment, false);
			GameLoader.Instance.gameData.allTradeCenters[center.mainLoc] = center.SaveData();
            allTradeCenters.Add(center);
            tradeStopNameDict[center.tradeCenterName] = center;
            foreach (SingleBuildType type in center.singleBuildDict.Keys)
                AddStop(center.singleBuildDict[type], center);
            
			if (hideTerrain)
				center.Hide();
			else
				center.isDiscovered = true;

            center.SetTradeCenterRep(false);
			LoadTradeCenterBorders(center.mainLoc);
		}

		Unit unit = mainPlayer.GetComponent<Unit>();
        unit.isPlayer = true;
        characterUnits.Add(mainPlayer);
        uiSpeechWindow.AddToSpeakingDict("Koa", mainPlayer);
		unit.SetReferences(this);
        startingLoc = RoundToInt(mainPlayer.transform.position);

        if (newGame)
        {
            scott.gameObject.SetActive(false);
            azai.gameObject.SetActive(false);
            azai.id = 0;
            scottFollow = false;
            azaiFollow = false;
			scott.gameObject.tag = "Character";
			//scott.marker.gameObject.tag = "Character";
			azai.gameObject.tag = "Character";
			//azai.marker.gameObject.tag = "Character";
			unitMovement.uiWorkerTask.DeactivateButtons();
        }
        else
        {
            characterUnits.Add(scott);
            characterUnits.Add(azai);
            mainPlayer.name = "Koa & Co.";
            scottFollow = true;
            azaiFollow = true;

			scott.gameObject.tag = "Player";
            //scott.marker.gameObject.tag = "Player";
            azai.gameObject.tag = "Player";
            //azai.marker.gameObject.tag = "Player";
            unit.currentLocation = RoundToInt(unit.transform.position);
            //AddUnitPosition(unit.transform.position, unit);

		    scott.currentLocation = RoundToInt(scott.transform.position);
		    //AddUnitPosition(scott.transform.position, scott);
            azai.currentLocation = RoundToInt(azai.transform.position);
            //AddUnitPosition(azai.transform.position, azai);
        }

        uiSpeechWindow.AddToSpeakingDict("Scott", scott);
        uiSpeechWindow.AddToSpeakingDict("Azai", azai);
        scott.SetReferences(this);
        azai.SetReferences(this);
        azai.SetArmy();

		unit.Reveal();
		Vector3Int unitPos = RoundToInt(unit.transform.position);

        unit.currentLocation = unitPos;
		unit.SetMinimapIcon(cityBuilderManager.friendlyUnitHolder);

		if (newGame)
        {
            ToggleMainUI(false);
            playerInput.paused = true;
            Physics.gravity = new Vector3(0, -3, 0);
            Vector3 skyRotation = unit.transform.localEulerAngles;
            skyRotation.y += 180;
            unit.transform.rotation = Quaternion.Euler(skyRotation);
            Vector3 skyLoc = unitPos;
            skyLoc.y += 100f;
            unit.transform.position = skyLoc;
            Worker worker = unit.GetComponent<Worker>();
		    worker.ToggleFalling(true);
            StartCoroutine(StartingSpotlight());
            StartCoroutine(worker.FallingCoroutine(unitPos));
            StartCoroutine(StartingAmbience());
        }
        
        StartCoroutine(StartingMusic(newGame));

        if (enemyEmpires == null)
            enemyEmpires = new();

        if (enemyRoadLocs == null)
            enemyRoadLocs = new();

        foreach (EnemyEmpire empire in enemyEmpires)
        {
            empire.enemyLeader.SetUpNPC(this);
            empire.enemyLeader.SetEmpire(empire);
            allEnemyLeaders.Add(empire.enemyLeader);

            int i = 0;
			System.Random random = new();
			foreach (Vector3Int tile in empire.empireCities)
            {
                EnemyCityData data = new();
				data.era = currentEra;
				data.loc = tile;
				data.hasWater = true;
                data.cityName = empire.enemyLeader.cityNameList[i];
                i++;
				if (data.hasWater)
					data.popSize = random.Next(5, 13);
				else
					data.popSize = 4;
				BuildEnemyCity(data, tile, GetTerrainDataAt(tile), Resources.Load<GameObject>("Prefabs/" + UpgradeableObjectHolder.Instance.improvementDict["City-0"].prefabLoc), 
                    enemyRoadLocs, true, currentEra, false, empire, random);
            }
    
            //adding city locs to road locs to build roads there (building roads first to build cities on top
            enemyRoadLocs.AddRange(empire.empireCities);
            BuildEnemyRoads(enemyRoadLocs, 1);

            foreach (Vector3Int tile in empire.empireCities)
                LoadEnemyBorders(tile, GetEnemyCity(tile).empire.enemyLeader.borderColor);
		}

        foreach (Vector3Int tile in enemyCampDict.Keys)
            LoadEnemyBorders(tile, new Color(1, 0, 0, 0.68f));
	}

	private IEnumerator StartingAmbience()
    {
        yield return new WaitForSeconds(0.5f);

		ambienceAudio.AmbienceCheck();
	}

    private IEnumerator StartingMusic(bool newGame)
    {
		yield return new WaitForSeconds(10);

        if (newGame)
            musicAudio.PlaySpecificSong(newWorldSong);
        else
            musicAudio.StartMusic();
	}

	private IEnumerator StartingSpotlight()
    {
        mainPlayer.unitRigidbody.useGravity = false;
        yield return new WaitForSeconds(2);

		mainPlayer.unitRigidbody.useGravity = true;
        Vector3 startingLoc = mainPlayer.transform.position;
        startingLoc.y = 0;
        GameObject spotlight = Instantiate(Resources.Load<GameObject>("Prefabs/MiscPrefabs/Spotlight"));
        //spotlight.SetActive(true);
        spotlight.transform.position = startingLoc;
        spotlight.transform.SetParent(transform, false);
        Vector3 scale = spotlight.transform.localScale;
        Light startingSpotlight = Instantiate(Resources.Load<Light>("Prefabs/MiscPrefabs/StartingSpotlight"));
        startingSpotlight.transform.SetParent(transform, false);
        startingLoc.y += 5;
        startingSpotlight.transform.position = startingLoc;
		//startingSpotlight.gameObject.SetActive(true);
        cityBuilderManager.PlaySelectAudio(cityBuilderManager.fieryOpen);

        while (startingSpotlight.spotAngle < 20)
        {
            float lightScale = Time.deltaTime * 1.5f;
            float increase = lightScale * 2;
            startingSpotlight.innerSpotAngle += increase;
            startingSpotlight.spotAngle += increase;
            scale.x += lightScale;
            scale.z += lightScale;
            spotlight.transform.localScale = scale;

            yield return null;
        }

        cityBuilderManager.StopAudio();

        while (mainPlayer.transform.position.y > 0)
        {
            yield return null;
        }

		Destroy(startingSpotlight.gameObject);
        Destroy(spotlight);
	}

    public void ToggleMainUI(bool v)
    {
        if (hideUI == !v)
            return;
        
        hideUI = !v;
        ToggleMinimap(v);
		ToggleWorldResourceUI(v);
	}

	public void NewMap(Dictionary<Vector3Int, TerrainData> terrainDict)
    {
		List<TerrainData> coastalTerrain = new();
		List<TerrainData> terrainToCheck = new();
		List<TerrainData> terrainPropsToModify = new();
        int maxX = 0;
        int maxZ = 0;

		foreach (Vector3Int position in terrainDict.Keys)
		{
			if (position.x > maxX)
				maxX = position.x;
			if (position.z > maxZ)
				maxZ = position.z;

			TerrainData td = terrainDict[position];
			TerrainDataSO terrainData = td.terrainData;

			td.SetData(terrainData);
			
			if (terrainData.decorLocs[td.decorIndex] != "")
			{
				td.SetProp();
				terrainPropsToModify.Add(td);
			}

			td.SetWorld(this);

			if (td.isSeaCorner && !coastalTerrain.Contains(td))
				coastalTerrain.Add(td);

			world[td.TileCoordinates] = td;
			terrainToCheck.Add(td);
			td.CheckMinimapResource(mapHandler);

			td.SetHighlightMesh();

			if (td.hasResourceMap)
				SetResourceMinimapIcon(td);

			if (hideTerrain)
			{
				td.Hide();

				//if (td.rawResourceType == RawResourceType.Rocks)
				//	td.PrepParticleSystem();
			}
			else
			{
				td.Discover();

				if (td.hasResourceMap)
					ToggleResourceIcon(td.TileCoordinates, true);
			}

			if (!hideTerrain && td.hasResourceMap)
				ToggleResourceIcon(td.TileCoordinates, true);

			foreach (Vector3Int tile in neighborsEightDirections)
			{
				world[td.TileCoordinates + tile] = td;
			}

			GameLoader.Instance.gameData.allTerrain[td.TileCoordinates] = td.SaveData();
		}

		for (int i = 0; i < coastalTerrain.Count; i++)
			coastalTerrain[i].SetCoastCoordinates();

		for (int i = 0; i < terrainToCheck.Count; i++)
            ConfigureUVs(terrainToCheck[i]);

        StaticBatchingUtility.Combine(terrainHolder.gameObject);

		//after combine, then hide mesh
		for (int i = 0; i < terrainPropsToModify.Count; i++)
			terrainPropsToModify[i].SetVisibleProp();

		SetWorldBoundaries(maxX, maxZ);
        SetWaterSize(maxX, maxZ);
	}

	public void GenerateMap(Dictionary<Vector3Int, TerrainSaveData> mainMap)
    {
		if (tutorial)
		{
			GameObject helperWindow = Instantiate(Resources.Load<GameObject>("Prefabs/UIPrefabs/HelperWindow"));
			helperWindow.transform.SetParent(mainCanvas.transform, false);
			cityBuilderManager.uiHelperWindow = helperWindow.GetComponent<UIHelperWindow>();
		}

		List<TerrainData> coastalTerrain = new();
		List<TerrainData> terrainToCheck = new();
        List<TerrainData> terrainPropsToModify = new();
        int maxX = 0;
        int maxZ = 0;

		foreach (Vector3Int position in mainMap.Keys)
		{
            if (position.x > maxX)
                maxX = position.x;
            if (position.z > maxZ)
                maxZ = position.z;
            
            TerrainSaveData data = mainMap[position];
            TerrainDataSO terrainData = UpgradeableObjectHolder.Instance.terrainDict[data.name];
            
			GameObject go = Instantiate(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/" + terrainData.prefabLocs[data.variant]), data.tileCoordinates, data.rotation);
            go.name = terrainData.prefabLocs[data.variant];
            go.transform.SetParent(terrainHolder, false);
            TerrainData td = go.GetComponent<TerrainData>();
            td.LoadData(data);
            
            //checking to place mountain middles and to change uvs
            if (td.terrainData.terrainDesc == TerrainDesc.Mountain)
            {
                if (td.terrainData.grassland)
                {
                    for (int i = 0; i < neighborsFourDirectionsIncrement.Count; i++)
				    {
                        Vector3Int neighbor = td.TileCoordinates + neighborsFourDirectionsIncrement[i];
					    if (mainMap[neighbor].name == "GrasslandMountainTerrain")
						    SetMountainMiddle(td, i, true);
				    }

                    //Set region based on location relative to starting region? or just add east boolean
                    if (startingRegion == Region.East)
						td.ChangeMountainUVs(false);
				}
                else
                {
					for (int i = 0; i < neighborsFourDirectionsIncrement.Count; i++)
				    {
						Vector3Int neighbor = td.TileCoordinates + neighborsFourDirectionsIncrement[i];
						if (mainMap[neighbor].name == "DesertMountainTerrain")
							SetMountainMiddle(td, i, false);
					}

                    td.ChangeMountainUVs(true);
				}
			}
            
            td.SetData(terrainData);

            if (terrainData.decorLocs[data.decor] != "")
            {
				if ((terrainData.type == TerrainType.Forest || terrainData.type == TerrainType.ForestHill) && terrainData.terrainDesc != TerrainDesc.Swamp)
                {
					GameObject prop = Instantiate(Resources.Load<GameObject>("Prefabs/TerrainPropPrefabs/" + terrainData.decorLocs[data.decor]), Vector3Int.zero, Quaternion.identity);
                    prop.name = terrainData.decorLocs[data.decor];
					prop.transform.SetParent(td.prop, false);
					prop.GetComponent<TreeHandler>().propMesh.rotation = data.propRotation;
					td.SetProp();
					terrainPropsToModify.Add(td);

					GameObject nonStaticProp = Instantiate(Resources.Load<GameObject>("Prefabs/TerrainPropPrefabs/" + terrainData.decorLocs[data.decor]), Vector3.zero, Quaternion.identity);
                    nonStaticProp.transform.SetParent(td.nonstatic, false);
                    td.nonstatic.rotation = data.propRotation;
                    td.SetNonStatic();
					td.SkinnedMeshCheck();
				}
				else
                {
				    GameObject prop = Instantiate(Resources.Load<GameObject>("Prefabs/TerrainPropPrefabs/" + terrainData.decorLocs[data.decor]), Vector3.zero, data.propRotation);
                    prop.name = terrainData.decorLocs[data.decor];
					prop.transform.SetParent(td.prop, false);
                    td.SetProp();
                    terrainPropsToModify.Add(td);
					td.SkinnedMeshCheck();
				}
			}

			td.SetWorld(this);

			if (td.isSeaCorner && !coastalTerrain.Contains(td))
				coastalTerrain.Add(td);

			world[data.tileCoordinates] = td;
			terrainToCheck.Add(td);
			td.CheckMinimapResource(mapHandler);

            td.SetHighlightMesh();

			if (td.hasResourceMap)
                SetResourceMinimapIcon(td);

			if (td.isDiscovered)
            {
                td.Discover();
                td.SetMovement();

                if (td.hasResourceMap)
					ToggleResourceIcon(td.TileCoordinates, true);
			}
			else
            {
                td.Hide();

                if (td.CompareTag("Hill") || td.CompareTag("Mountain"))
                {
                    td.nonStaticName = "TerrainPrefabs / " + terrainData.prefabLocs[data.variant];
                    td.hasNonstatic = true;

                    //if has rocks
                }
                else if (terrainData.terrainDesc != TerrainDesc.Swamp && (td.CompareTag("Forest") || td.CompareTag("Forest Hill")))
                {
					td.nonStaticName = "TerrainPropPrefabs/" + terrainData.decorLocs[data.decor];
                    td.hasNonstatic = true;
                }
			}

            if (!hideTerrain && td.hasResourceMap)
                ToggleResourceIcon(td.TileCoordinates, true);

			foreach (Vector3Int tile in neighborsEightDirections)
			{
				world[data.tileCoordinates + tile] = td;
			}
		}

        for (int i = 0; i < coastalTerrain.Count; i++)
            coastalTerrain[i].SetCoastCoordinates();

        for (int i = 0; i < terrainToCheck.Count; i++)
            ConfigureUVs(terrainToCheck[i]);

        StaticBatchingUtility.Combine(terrainHolder.gameObject);

        //after combine, then hide mesh
        for (int i = 0; i < terrainPropsToModify.Count; i++)
            terrainPropsToModify[i].SetVisibleProp();

        SetWorldBoundaries(maxX, maxZ);
        SetWaterSize(maxX, maxZ);
        StartCoroutine(StartingAmbience());
	}

	private void SetWorldBoundaries(int maxX, int maxZ)
	{
        int width = maxX + 3;
        int height = maxZ + 3;
        
        Vector3Int[] locs = new Vector3Int[8] { new(width / 2, 0, height * 2 - 2), new(width * 2, 0, height * 2 - 2),
												new(width * 2 - 2, 0, height / 2), new(width * 2 - 2, 0, -height - 9),
												new(width / 2, 0, -height - 9), new(-width - 9, 0, -height - 9),
												new(-width - 9, 0, height / 2), new(-width - 9, 0, height * 2)};

		for (int i = 0; i < 8; i++)
		{
			Vector3Int loc = locs[i];
			GameObject unexploredGO = Instantiate(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/UnexploredBoundary"), loc, Quaternion.identity);
			unexploredGO.transform.SetParent(terrainHolder, false);
			unexploredGO.transform.localScale = new Vector3(width, 1, height);
		}
	}

    private void SetWaterSize(int maxX, int maxZ)
    {
		int width = maxX + 3;
		int height = maxZ + 3;

		water.transform.position = new Vector3(width / 2f, -0.06f, height / 2f);
		water.minimapIcon.localScale = new Vector3(0.14f * (width / 3), 1.8f, 0.14f * (height / 3));
	}

	public void GenerateTradeCenters(Dictionary<Vector3Int, TradeCenterData> centers)
    {
        foreach (TradeCenterData centerData in centers.Values)
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("Prefabs/TradeCenterPrefabs/" + centerData.name), centerData.mainLoc, Quaternion.identity);
            go.name = centerData.name;
            go.transform.SetParent(tradeCenterHolder, false);
            TradeCenter center = go.GetComponent<TradeCenter>();
            center.main.rotation = centerData.rotation;
            center.lightHolder.rotation = centerData.rotation;
            center.LoadData(centerData);
			center.SetWorld(this);
			center.ToggleLights(false);
			center.SetName();
            center.SetPop(centerData.cityPop);
			center.ClaimSpotInWorld(increment, true);
            allTradeCenters.Add(center);
            tradeStopNameDict[center.tradeCenterName] = center;

            foreach (SingleBuildType type in center.singleBuildDict.Keys)
                AddStop(center.singleBuildDict[type], center);
			if (!GetTerrainDataAt(centerData.mainLoc).isDiscovered && hideTerrain)
				center.Hide();
			else
				center.isDiscovered = true;

            center.SetTradeCenterRep(true);
            LoadTradeCenterBorders(centerData.mainLoc);
			GameLoader.Instance.centerWaitingDict[center] = (centerData.goldWaitList, centerData.waitList, centerData.seaWaitList, centerData.airWaitList);
		}
	}

    public EnemyEmpire GenerateEnemyLeaders(UnitData data)
	{
        GameObject go = Resources.Load<GameObject>("Prefabs/" + UpgradeableObjectHolder.Instance.enemyLeaderDict[data.unitNameAndLevel].prefabLoc);
		GameObject leaderGO = Instantiate(go, data.position, data.rotation);
        leaderGO.name = go.name;
        leaderGO.transform.SetParent(enemyCityHolder, false);
        MilitaryLeader leader = leaderGO.GetComponent<MilitaryLeader>();
		leader.SetUpNPC(this, data);
        leader.LoadMilitaryLeaderData(data);
        EnemyEmpire empire = new();
		empire.LoadData(data.leaderData);
		empire.enemyLeader = leader;
		leader.SetEmpire(empire);
		allEnemyLeaders.Add(leader);

        if (!GetTerrainDataAt(leader.currentLocation).isDiscovered)
            leader.gameObject.SetActive(false);

        return empire;
	}

	public void GenerateEnemyCities(EnemyCityData data, EnemyEmpire empire, List<Vector3Int> roadLocs)
    {
		BuildEnemyCity(data, data.loc, GetTerrainDataAt(data.loc), Resources.Load<GameObject>("Prefabs/" + UpgradeableObjectHolder.Instance.improvementDict["City-0"].prefabLoc), roadLocs, true, data.era, true, empire);
	}

	public void BuildEnemyCity(EnemyCityData data, Vector3Int cityTile, TerrainData td, GameObject prefab, List<Vector3Int> roadTiles, bool hasWater, Era era, bool load, EnemyEmpire empire, System.Random random = null)
	{
        Dictionary<string, string> buildingEraDict = GetBuildingEraDict(era);
  
		List<Vector3Int> foodLocs = new();
        List<Vector3Int> fishLocs = new();
        List<Vector3Int> flatlandLocs = new();
        List<Vector3Int> coastLocs = new();
        List<Vector3Int> mineLocs = new();

        td.enemyCamp = true;
        td.enemyZone = true;
        neutralZones.Add(cityTile);
		AddToCityLabor(cityTile, null);
		foreach (Vector3Int tile in GetNeighborsFor(cityTile, State.EIGHTWAYINCREMENT))
        {
            TerrainData tdNeighbor = GetTerrainDataAt(tile);
            tdNeighbor.enemyZone = true;
            neutralZones.Add(tile);
			AddToCityLabor(tile, null);

            if (roadTiles.Contains(tile))
                continue;

            if (tdNeighbor.rawResourceType == RawResourceType.Rocks)
            {
                mineLocs.Add(tile);
                continue;
            }

			if (tdNeighbor.resourceType == ResourceType.Food)
                foodLocs.Add(tile);
            else if (tdNeighbor.resourceType == ResourceType.Fish)
                fishLocs.Add(tile);
            
            if (tdNeighbor.terrainData.type == TerrainType.Flatland && tdNeighbor.resourceType == ResourceType.None)
                flatlandLocs.Add(tile);
            else if (tdNeighbor.terrainData.type == TerrainType.Coast)
                coastLocs.Add(tile);
        }

        td.ShowProp(false);
        td.FloodPlainCheck(true);

		AddCityBuildingDict(cityTile);
		GameObject newCity = Instantiate(prefab, cityTile, Quaternion.identity);
        newCity.tag = "Enemy";
        newCity.gameObject.transform.SetParent(enemyCityHolder, false);
		AddStructure(cityTile, newCity); //adds building location to buildingDict
		City city = newCity.GetComponent<City>();
		city.SetWorld(this, true);
        city.ExtinguishFire();
        city.cityLoc = cityTile;
        city.cityName = data.cityName;
        newCity.name = city.cityName + " (Enemy)";
        city.currentPop = data.popSize;
        city.unusedLabor = data.popSize;
        city.minimapIcon.sprite = city.enemyCityIcon;
        city.empire = empire;

        enemyCityDict[cityTile] = city;

        //setting up external labels
        city.cityNameField.SetEnemyBackGround();
        city.cityNameField.SetCityPop(data.popSize);
		city.cityNameField.cityName.text = data.cityName;
		city.cityNameField.SetCityNameFieldSize(data.cityName);
        city.cityNameField.ToggleVisibility(true);
        city.cityNameMap.GetComponentInChildren<TMP_Text>().text = data.cityName;

		//building Housing
		string[] housingArray;

		if (data.popSize < 8)
        {
			housingArray = new string[4] { buildingEraDict["House Small"], buildingEraDict["House Small"], buildingEraDict["House Small"], buildingEraDict["House Small"] };

            for (int i = 0; i < data.popSize - 4; i++)
                housingArray[i] = buildingEraDict["House Medium"];
        }
        else if (data.popSize < 12)
        {
			housingArray = new string[4] { buildingEraDict["House Medium"], buildingEraDict["House Medium"], buildingEraDict["House Medium"], buildingEraDict["House Medium"] };

			for (int i = 0; i < data.popSize - 8; i++)
				housingArray[i] = buildingEraDict["House Large"];
		}
		else
        {
			housingArray = new string[4] { buildingEraDict["House Large"], buildingEraDict["House Large"], buildingEraDict["House Large"], buildingEraDict["House Large"] };
		}

        for (int i = 0; i < housingArray.Length; i++)
        {
            ImprovementDataSO buildingData = UpgradeableObjectHolder.Instance.improvementDict[housingArray[i]];
            city.LoadHouse(buildingData, city.cityLoc, GetTerrainDataAt(city.cityLoc).isHill, i);
		}

        //buildings to make
        string[] buildingArray;

        if (hasWater)
            buildingArray = new string[2] { buildingEraDict["Monument"], buildingEraDict["Walls"] };
        else
            buildingArray = new string[3] { buildingEraDict["Monument"], buildingEraDict["Well"], buildingEraDict["Walls"] };

        for (int i = 0; i < buildingArray.Length; i++)
        {
            ImprovementDataSO buildingData = UpgradeableObjectHolder.Instance.improvementDict[buildingArray[i]];
            CreateBuilding(buildingData, city, null); //can be null as no data is technically loaded
        }

        city.HousingCount = 0;
        city.waterCount = 0;

        if (!td.isDiscovered)
            newCity.SetActive(false);

        //building improvements
        List<CityImprovementData> improvementList = new();
        
        //farms
        for (int i = 0; i < foodLocs.Count; i++)
        {
            if (i == 2)
                break;

            if (flatlandLocs.Count <= 1)
            {
                if (flatlandLocs.Count == 0)    
                    flatlandLocs.Add(foodLocs[i]);
                break;
            }

            CityImprovementData improvementData = new();
            improvementData.location = foodLocs[i];
            improvementData.name = buildingEraDict["Farm"];
            flatlandLocs.Remove(foodLocs[i]);
            improvementList.Add(improvementData);
        }

        //fishing
        for (int i = 0; i < fishLocs.Count; i++)
        {
            if (i == 2)
                break;

            if (coastLocs.Count <= 1)
            {
                if (coastLocs.Count == 0)
                    coastLocs.Add(fishLocs[i]);
                break;
            }

			CityImprovementData improvementData = new();
			improvementData.location = fishLocs[i];
			improvementData.name = buildingEraDict["Fishing"];
			coastLocs.Remove(fishLocs[i]);
			improvementList.Add(improvementData);
		}

        //mines & querries
        for (int i = 0; i < mineLocs.Count; i++)
        {
            CityImprovementData improvementData = new();
            improvementData.location = mineLocs[i];
            improvementData.name = world[mineLocs[i]].isHill ? buildingEraDict["Mine"] : buildingEraDict["Quarry"];
            improvementList.Add(improvementData);
        }

        //barracks
        CityImprovementData barracksData = new();
        if (load)
        {
            barracksData.location = data.barracksLoc;
        }
        else
        {
            barracksData.location = flatlandLocs[random.Next(0, flatlandLocs.Count)];
            data.barracksLoc = barracksData.location;
        }
		barracksData.name = buildingEraDict["Barracks"];
		improvementList.Add(barracksData);

        //set up enemy camp after barracks build
		city.enemyCamp = new();
		city.enemyCamp.loc = barracksData.location;
		city.enemyCamp.world = this;
        city.enemyCamp.isCity = true;
        city.enemyCamp.cityLoc = cityTile;
        if (cityTile == empire.capitalCity)
        {
            if (!empire.enemyLeader.dueling)
			    empire.enemyLeader.enemyCamp = city.enemyCamp;
            empire.enemyLeader.enemyAI.CampSpot = city.cityLoc;
            empire.enemyLeader.SetBarracksBunk(city.enemyCamp.loc);
        }

        //checking to load fighting of moving enemy units
        //bool attacked = false;
        bool movingOut = false;
        Dictionary<Vector3Int, UnitData> fightingEnemies = new();

        if (load)
        {
            if (GameLoader.Instance.gameData.movingEnemyBases.ContainsKey(cityTile))
            {
			    EnemyCampData enemyData = GameLoader.Instance.gameData.movingEnemyBases[cityTile];

			    for (int i = 0; i < enemyData.allUnits.Count; i++)
			    {
				    fightingEnemies[enemyData.allUnits[i].campSpot] = enemyData.allUnits[i];
			    }

			    city.enemyCamp.enemyReady = enemyData.enemyReady;
                city.enemyCamp.threatLoc = enemyData.threatLoc;
                city.enemyCamp.forward = enemyData.forward;
			    city.enemyCamp.revealed = enemyData.revealed;
			    city.enemyCamp.prepping = enemyData.prepping;
			    city.enemyCamp.attacked = enemyData.attacked;
			    city.enemyCamp.attackReady = enemyData.attackReady;
			    city.enemyCamp.armyReady = enemyData.armyReady;
			    city.enemyCamp.inBattle = enemyData.inBattle;
			    city.enemyCamp.campCount = enemyData.campCount;
			    city.enemyCamp.infantryCount = enemyData.infantryCount;
			    city.enemyCamp.rangedCount = enemyData.rangedCount;
			    city.enemyCamp.cavalryCount = enemyData.cavalryCount;
			    city.enemyCamp.seigeCount = enemyData.seigeCount;
			    city.enemyCamp.health = enemyData.health;
			    city.enemyCamp.strength = enemyData.strength;
			    city.enemyCamp.moveToLoc = enemyData.moveToLoc;
			    city.enemyCamp.pathToTarget = enemyData.pathToTarget;
			    city.enemyCamp.movingOut = enemyData.movingOut;
			    city.enemyCamp.returning = enemyData.returning;
                city.enemyCamp.pillage = enemyData.pillage;
                city.enemyCamp.pillageTime = enemyData.pillageTime;
                city.enemyCamp.growing = enemyData.growing;
                city.enemyCamp.fieldBattleLoc = enemyData.fieldBattleLoc;
                city.enemyCamp.lastSpot = enemyData.lastSpot;
                city.enemyCamp.removingOut = enemyData.removingOut;
                city.enemyCamp.seaTravel = enemyData.seaTravel;
                city.enemyCamp.actualAttackLoc = enemyData.actualAttackLoc;
                city.countDownTimer = enemyData.countDownTimer;
                city.enemyCamp.timeTilReturn = enemyData.timeTilReturn;
                city.enemyCamp.retreat = enemyData.retreat;

                if (city.enemyCamp.timeTilReturn > 0)
                    StartCoroutine(city.enemyCamp.RetreatTimer());

                if (city.enemyCamp.movingOut && !city.enemyCamp.returning && !city.enemyCamp.pillage)
                    AddBattleZones(city.enemyCamp.actualAttackLoc, city.enemyCamp.threatLoc, city.enemyCamp.inBattle);

                if ((city.enemyCamp.inBattle || city.enemyCamp.movingOut) && !city.enemyCamp.returning && city.enemyCamp.campCount != 0)
                    GameLoader.Instance.attackingEnemyCitiesList.Add(city);
			
                if (city.enemyCamp.campCount != 0)
                    movingOut = true;

                if (city.empire.capitalCity != cityTile || !city.empire.enemyLeader.dueling)
                {
					if (city.empire.attackingCity == cityTile && !GetTerrainDataAt(cityTile).isDiscovered)
						city.ActivateButHideCity();
					city.LoadSendAttackWait(false);
                }
		    }
        }
        else
        {
			city.enemyCamp.fieldBattleLoc = cityTile;
            city.enemyCamp.moveToLoc = data.barracksLoc;
			city.enemyCamp.SetCityEnemyCamp();
			AddAllEnemyUnits(city.enemyCamp, random, data, load, empire.enemyLeader, empire.empireUnitCount);
        }

        if (movingOut)
        {
		    foreach (Vector3Int unitLoc in data.enemyUnitData.Keys)
		    {
                if (!fightingEnemies.ContainsKey(unitLoc)) //in case it's growing
                    continue;
                
                Vector3 unitSpawn = unitLoc;

			    UnitBuildDataSO enemyData = UpgradeableObjectHolder.Instance.enemyUnitDict[data.enemyUnitData[unitLoc]];
			    Quaternion rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
                GameObject enemy;

                if (enemyData.unitType == UnitType.Infantry)
                    enemy = Resources.Load<GameObject>("Prefabs/" + empire.enemyLeader.leaderUnitDict[UnitType.Infantry].prefabLoc);
                else if (enemyData.unitType == UnitType.Ranged)
					enemy = Resources.Load<GameObject>("Prefabs/" + empire.enemyLeader.leaderUnitDict[UnitType.Ranged].prefabLoc);
                else if (enemyData.unitType == UnitType.Cavalry)
					enemy = Resources.Load<GameObject>("Prefabs/" + empire.enemyLeader.leaderUnitDict[UnitType.Cavalry].prefabLoc);
                else
					enemy = Resources.Load<GameObject>("Prefabs/" + empire.enemyLeader.leaderUnitDict[UnitType.Infantry].prefabLoc);

				GameObject enemyGO = Instantiate(enemy, unitSpawn, rotation);
			    enemyGO.name = enemyData.nationalityAdjective + " " + enemyData.unitDisplayName;
			    enemyGO.transform.SetParent(enemyUnitHolder, false);

			    Unit unit = enemyGO.GetComponent<Unit>();
			    unit.SetReferences(this);
			    unit.SetMinimapIcon(enemyUnitHolder);
                unit.military.SetSailColor(empire.enemyLeader.colorOne);

				if (!city.enemyCamp.movingOut)
                {
				    unit.minimapIcon.gameObject.SetActive(false);
                }

			    unit.currentLocation = unitLoc;
                if (fightingEnemies[unitLoc].benched)
                {
                    city.enemyCamp.benchedUnit = unit.military;
                    city.enemyCamp.UnitsInCamp.Add(empire.enemyLeader);
                }
                else
                {
			        city.enemyCamp.UnitsInCamp.Add(unit.military);
                }
			    unit.enemyAI.CampSpot = unitLoc;
                unit.military.enemyCamp = city.enemyCamp;
			    unit.military.LoadUnitData(fightingEnemies[unitLoc]);

                TerrainData tdUnit = GetTerrainDataAt(unit.currentLocation);
                if (city.enemyCamp.movingOut)
                {
                    if (!tdUnit.isDiscovered)
                        unit.HideUnit();
                    //else if (tdUnit.CompareTag("Forest") || tdUnit.CompareTag("Forest Hill") || IsBuildLocationTaken(tdUnit.TileCoordinates))
                    if (!unit.bySea && !unit.byAir)
                        unit.outline.ToggleOutline(true);
					    //unit.marker.ToggleVisibility(true);
			    }
                else
                {
                    if (!tdUnit.isDiscovered)
                        unit.gameObject.SetActive(false);
				}
		    }
        }

		//harbor
		CityImprovementData harborData = new();
        if (load)
        {
            harborData.location = data.harborLoc;
        }
        else
        {
		    harborData.location = coastLocs[random.Next(0, coastLocs.Count)];
            data.harborLoc = harborData.location;
        }
		harborData.name = buildingEraDict["Harbor"];
		improvementList.Add(harborData);

		for (int i = 0; i < improvementList.Count; i++)
            CreateImprovement(city, improvementList[i], true);

        if (load)
        {
			if (empire.enemyLeader.dueling)
                ToggleDuelBattleCam(true, city.singleBuildDict[SingleBuildType.Barracks], azai, empire.enemyLeader);

			if (!td.isDiscovered)
                city.subTransform.gameObject.SetActive(false); //necessary if one of improvements is showing but not city

            city.enemyCamp.LoadCityEnemyCamp();

			if (city.enemyCamp.growing && !city.enemyCamp.prepping && !city.enemyCamp.attackReady && !city.enemyCamp.inBattle)
            {
                if (city.empire.capitalCity != city.cityLoc || !city.empire.enemyLeader.dueling)
                {
					if (!GetTerrainDataAt(cityTile).isDiscovered)
						city.ActivateButHideCity();
					city.StartSpawnCycle(false);
                }
            }
		}
        else
        {
			GameLoader.Instance.gameData.movingEnemyBases[cityTile] = new();
			GameLoader.Instance.gameData.enemyCities[cityTile] = data;
            GameLoader.Instance.gameData.enemyRoads = new(roadTiles);
        }
	}

    private void AddAllEnemyUnits(EnemyCamp camp, System.Random random, EnemyCityData data, bool load, MilitaryLeader leader, int unitCount)
    {
		List<Vector3Int> campLocs = GetNeighborsFor(camp.loc, State.EIGHTWAYARMY);
        List<UnitType> types = new() { UnitType.Infantry, UnitType.Ranged, UnitType.Cavalry };

		for (int i = 0; i < unitCount; i++)
		{
            Vector3Int spawnLoc;
            GameObject enemy;

            if (i < 3)
            {
                enemy = Resources.Load<GameObject>("Prefabs/" + leader.leaderUnitDict[UnitType.Infantry].prefabLoc);
                spawnLoc = camp.GetAvailablePosition(UnitType.Infantry);
			}
            else if (i < 6)
            {
                enemy = Resources.Load<GameObject>("Prefabs/" + leader.leaderUnitDict[UnitType.Ranged].prefabLoc);
				spawnLoc = camp.GetAvailablePosition(UnitType.Ranged);
			}
            else if (i < 8)
            {
                enemy = Resources.Load<GameObject>("Prefabs/" + leader.leaderUnitDict[UnitType.Cavalry].prefabLoc);
				spawnLoc = camp.GetAvailablePosition(UnitType.Cavalry);
			}
            else
            {
                UnitType type = types[UnityEngine.Random.Range(0, types.Count)];
                enemy = Resources.Load<GameObject>("Prefabs/" + leader.leaderUnitDict[type].prefabLoc);
				spawnLoc = camp.GetAvailablePosition(type);
			}


			campLocs.Remove(spawnLoc);

			Quaternion rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);

			GameObject enemyGO = Instantiate(enemy, spawnLoc, rotation);
			enemyGO.transform.SetParent(enemyUnitHolder, false);
		
            Unit unitEnemy = enemyGO.GetComponent<Unit>();
		    unitEnemy.SetReferences(this);
		    unitEnemy.SetMinimapIcon(enemyUnitHolder);

            if (!GetTerrainDataAt(camp.loc).isDiscovered)
            {
			    unitEnemy.minimapIcon.gameObject.SetActive(false);
			    enemyGO.SetActive(false);
            }

			Vector3Int unitLoc = RoundToInt(unitEnemy.transform.position);
			unitEnemy.currentLocation = unitLoc;
			enemyGO.name = unitEnemy.buildDataSO.nationalityAdjective + " " + unitEnemy.buildDataSO.unitDisplayName;
			unitEnemy.military.barracksBunk = spawnLoc;

			camp.UnitsInCamp.Add(unitEnemy.military);
			unitEnemy.enemyAI.CampSpot = unitLoc;
			unitEnemy.military.enemyCamp = camp;
		}

		camp.FormBattlePositions();

        if (!load)
		    data.enemyUnitData = camp.SendCampData();
	}

    public void BuildEnemyRoads(List<Vector3Int> roadList, int level)
    {
        for (int i = 0; i < roadList.Count; i++)
        {
            roadManager.BuildRoadAtPosition(roadList[i], UtilityType.Road, level);
        }
    }

    private Dictionary<string, string> GetBuildingEraDict(Era era)
    {
        Dictionary<string, string> newDict = new();

        switch (era)
        {
            case Era.AncientEra:
                newDict["House Small"] = "Housing-1";
				newDict["House Medium"] = "Housing-2";
				newDict["House Large"] = "Housing-3";
				newDict["Monument"] = "Monument-1";
				newDict["Market"] = "Market-1";
				newDict["Well"] = "Well-1";
				newDict["Barracks"] = "Barracks-1";
                newDict["Harbor"] = "Harbor-1";
                newDict["Farm"] = "Farm-2";
                newDict["Fishing"] = "FishingBoats-1";
                newDict["Mine"] = "Mine-1";
                newDict["Quarry"] = "Quarry-1";
                newDict["Walls"] = "City Walls-1";
				break;
        }

        return newDict;
    }

	public void BuildCity(Vector3Int cityTile, TerrainData td, GameObject prefab, CityData data)
	{
        td.ShowProp(false);
        td.FloodPlainCheck(true);

		GameObject newCity = Instantiate(prefab, cityTile, Quaternion.identity);
		newCity.gameObject.transform.SetParent(cityHolder, false);
		AddStructure(cityTile, newCity); //adds building location to buildingDict
		City city = newCity.GetComponent<City>();
		city.SetWorld(this);
		city.UpdateCityName(data.name);
		AddStopName(city.cityName, city);
		AddCity(cityTile, city);

		city.LightFire(td.isHill);

		uiProfitabilityStats.CreateNewProfitabilityCityStats(city, true);
		AddCityBuildingDict(cityTile);

        city.LoadCityData(data);
        unitMovement.workerTaskManager.SetCityBools(city, cityTile);

        //building buildings
        foreach (CityImprovementData improvementData in data.cityBuildings)
            CreateBuilding(UpgradeableObjectHolder.Instance.improvementDict[improvementData.name], city, improvementData);

        GameLoader.Instance.cityWaitingDict[city] = (data.goldWaitList, data.resourceWaitDict, data.resourceMaxWaitDict, data.unloadWaitList,
            data.waitList, data.seaWaitList, data.airWaitList);

        GameLoader.Instance.cityBonusDict[city] = data.cityBonusList;
	}

    private void CreateBuilding(ImprovementDataSO buildingData, City city, CityImprovementData data)
    {
		if (buildingData.improvementName == "Housing"/*city.housingData.improvementName*/)
		{
			city.LoadHouse(buildingData, city.cityLoc, GetTerrainDataAt(city.cityLoc).isHill, data.housingIndex);
			return;
		}

		//for some non buildings in the building selection list (eg harbor)
		if (buildingData.isBuildingImprovement)
		{
			CreateImprovement(city, data);
			return;
		}

		//setting building locations
		Vector3 cityPos = city.cityLoc;
		Vector3 buildingLocalPos = buildingData.buildingLocation; //putting the building in it's position in the city square
		cityPos += buildingLocalPos;

		GameObject building;
		if (GetTerrainDataAt(city.cityLoc).isHill)
		{
			cityPos.y += buildingData.hillAdjustment;
			building = Instantiate(Resources.Load<GameObject>("Prefabs/" + buildingData.prefabLoc), cityPos, Quaternion.identity);
		}
		else
		{
			building = Instantiate(Resources.Load<GameObject>("Prefabs/" + buildingData.prefabLoc), cityPos, Quaternion.identity);
		}

		//setting world data
		CityImprovement improvement = building.GetComponent<CityImprovement>();
        improvement.SetWorld(this);
		improvement.loc = city.cityLoc;
		building.transform.parent = city.subTransform;

		string buildingName = buildingData.improvementName;
		SetCityBuilding(improvement, buildingData, city.cityLoc, city, buildingName);
        building.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
		
        city.HousingCount += buildingData.housingIncrease;

        city.attackBonus += buildingData.attackBonus;

		cityBuilderManager.CombineMeshes(city, city.subTransform, false);
		improvement.SetInactive();
	}

	public void CreateImprovement(City city, CityImprovementData data, bool enemy = false)
	{
        ImprovementDataSO improvementData = UpgradeableObjectHolder.Instance.improvementDict[data.name];
        Vector3Int tempBuildLocation = data.location;
		Vector3Int buildLocation = tempBuildLocation;
		buildLocation.y = 0;

		//rotating harbor so it's closest to city
		int rotation = 0;
		if (improvementData.singleBuildType == SingleBuildType.Harbor)
		{
            if (city == null) //for orphans
                rotation = data.rotation;
            else
                rotation = cityBuilderManager.HarborRotation(tempBuildLocation, city.cityLoc);
		}

		//adding improvement to world dictionaries
		TerrainData td = GetTerrainDataAt(tempBuildLocation);

		if (improvementData.secondaryData.Count > 0)
		{
			foreach (ImprovementDataSO tempData in improvementData.secondaryData)
			{
				if (improvementData.producedResources[0].resourceType == tempData.producedResources[0].resourceType)
				{
					if (td.terrainData.specificTerrain == tempData.specificTerrain)
					{
						improvementData = tempData;
						break;
					}
				}
				else if (td.resourceType == tempData.producedResources[0].resourceType)
				{
					improvementData = tempData;
					break;
				}
			}

			rotation = (int)td.transform.eulerAngles.y;
		}

		GameObject improvement;
		if (td.isHill && improvementData.adjustForHill)
		{
			Vector3 buildLocationHill = buildLocation;
			buildLocationHill.y += improvementData.hillAdjustment;
			improvement = Instantiate(Resources.Load<GameObject>("Prefabs/" + improvementData.prefabLoc), buildLocationHill, Quaternion.Euler(0, rotation, 0));
		}
		else
		{
			improvement = Instantiate(Resources.Load<GameObject>("Prefabs/" + improvementData.prefabLoc), buildLocation, Quaternion.Euler(0, rotation, 0));
		}

		if (enemy)
			improvement.tag = "Enemy";
		AddStructure(buildLocation, improvement);
		CityImprovement cityImprovement = improvement.GetComponent<CityImprovement>();
        cityImprovement.SetWorld(this);
        cityImprovement.loc = buildLocation;
		cityImprovement.InitializeImprovementData(improvementData);
		cityImprovement.SetQueueCity(null);
		cityImprovement.building = improvementData.isBuilding;
        if (city != null)
    		cityImprovement.city = city;

		SetCityDevelopment(tempBuildLocation, cityImprovement);

        if (data.isConstruction)
    		cityImprovement.improvementMesh.SetActive(false);

		//setting single build rules
		if (improvementData.singleBuildType != SingleBuildType.None && city != null)
		{
            AddToCityLabor(tempBuildLocation, city.cityLoc);
			
            if (!data.isConstruction)
                city.singleBuildDict[improvementData.singleBuildType] = tempBuildLocation;
		}

		//resource production
		ResourceProducer resourceProducer = improvement.GetComponent<ResourceProducer>();
		buildLocation.y = 0;
		AddResourceProducer(tempBuildLocation, resourceProducer);
        if (city != null)
    		resourceProducer.SetResourceManager(city.resourceManager);
		resourceProducer.InitializeImprovementData(improvementData, td.resourceType, data.producedResourceIndex); //allows the new structure to also start generating resources
		resourceProducer.SetCityImprovement(cityImprovement, data.producedResourceIndex);
		resourceProducer.SetLocation(tempBuildLocation);
        resourceProducer.SetProgressTimeBarSize();
        cityImprovement.CheckPermanentChanges();

        if (data.isConstruction)
        {
            cityImprovement.LoadData(data, city, this);
            cityImprovement.timePassed = data.timePassed; 
		    cityImprovement.BeginImprovementConstructionProcess(city, resourceProducer, tempBuildLocation, cityBuilderManager, td.isHill, true);		
        }
        else
        {
			//if (!improvementData.isBuildingImprovement)
            resourceProducer.SetNewProgressTime();
			cityImprovement.SetMinimapIcon(td);
            if (city != null)
            {
			    cityImprovement.meshCity = city;
			    cityImprovement.transform.parent = city.transform;
                city.AddToImprovementList(cityImprovement);
            }

			//making two objects, this one for the parent mesh
			GameObject tempObject = Instantiate(cityBuilderManager.emptyGO, cityImprovement.transform.position, cityImprovement.transform.rotation);
			tempObject.name = improvement.name;
            MeshFilter[] improvementMeshes = cityImprovement.MeshFilter;

			MeshFilter[] meshes = new MeshFilter[improvementMeshes.Length];
			int k = 0;

			foreach (MeshFilter mesh in improvementMeshes)
			{
				Quaternion rotation2 = mesh.transform.rotation;
				meshes[k] = Instantiate(mesh, mesh.transform.position, rotation2);
				meshes[k].name = mesh.name;
				meshes[k].transform.parent = tempObject.transform;
				k++;
			}

            //tempObject.transform.localScale = improvement.transform.localScale;
            improvement.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            cityImprovement.Embiggen();

            if (city != null)
            {
			    city.AddToMeshFilterList(tempObject, meshes, false, tempBuildLocation);
			    tempObject.transform.parent = city.transform;
            }
            else
            {
                cityBuilderManager.AddToOrphanMeshFilterList(tempObject, meshes, tempBuildLocation);
				improvement.transform.parent = cityBuilderManager.improvementHolder.transform;
			}

			tempObject.SetActive(false);

			//resetting ground UVs is necessary
			if (improvementData.replaceTerrain)
			{
				td.ToggleTerrainMesh(false);

                if (!td.terrainData.grassland)
                {
				    foreach (MeshFilter mesh in cityImprovement.MeshFilter)
				    {
					    if (mesh.name == "Ground")
					    {						
                            Vector2[] terrainUVs = SetUVMap(GetGrasslandCount(td), SetUVShift(td.terrainData.terrainDesc), Mathf.RoundToInt(td.main.eulerAngles.y));
						    Vector2[] newUVs = mesh.mesh.uv;
						    Vector2[] finalUVs = NormalizeUVs(terrainUVs, newUVs, Mathf.RoundToInt(td.main.eulerAngles.y / 90));
						    mesh.mesh.uv = finalUVs;

                            foreach (MeshFilter mesh2 in meshes)
						    {
                                if (mesh2.name == "Ground")
							    {
								    mesh2.mesh.uv = finalUVs;
								    break;
							    }
						    }

						    break;
					    }
				    }
                }
			}
            else
            {
				if (td.terrainData.specificTerrain == SpecificTerrain.FloodPlain)
                {
                    td.FloodPlainCheck(true);
                }
			}

			if (td.terrainData.type == TerrainType.Forest || td.terrainData.type == TerrainType.ForestHill)
				td.SwitchToRoad();
            if (city != null)
                cityBuilderManager.CombineMeshes(city, city.subTransform, false);

			//reseting rock UVs 
			ColorCityImprovementRocks(improvementData, cityImprovement, td, meshes);

			if (td.prop != null && improvementData.hideProp)
                td.ShowProp(false);

			//setting trade stop info
            if (improvementData.singleBuildType == SingleBuildType.TradeDepot)
            {
				if (city != null)
					AddStop(tempBuildLocation, city);
			}
			else if (improvementData.singleBuildType == SingleBuildType.Harbor)
			{
                cityImprovement.mapIconHolder.localRotation = Quaternion.Inverse(improvement.transform.rotation);
				if (city != null)
                    AddStop(tempBuildLocation, city);
			}
            else if (improvementData.singleBuildType == SingleBuildType.Airport)
            {
				if (city != null)
					AddStop(tempBuildLocation, city);
			}
			else if (improvementData.singleBuildType == SingleBuildType.Barracks)
			{
				militaryStationLocs.Add(tempBuildLocation);
				
                if (city != null)
                {
					cityImprovement.army = city.army;
					foreach (Vector3Int tile in GetNeighborsFor(tempBuildLocation, MapWorld.State.EIGHTWAYARMY))
					    city.army.SetArmySpots(tile);

				    city.army.SetLoc(tempBuildLocation, city);

                    if (city.army.noMoneyCycles > 0)
					    cityImprovement.exclamationPoint.SetActive(true);

				    if (td.isDiscovered && !enemy)
                    {
                        List<UnitData> militaryUnits = GameLoader.Instance.gameData.militaryUnits[tempBuildLocation];

				        for (int i = 0; i < militaryUnits.Count; i++)
                        {
                            CreateUnit(militaryUnits[i], city);
                        }
                    }
                }
			}

			//setting labor info (harbors have no labor)
			//AddToMaxLaborDict(tempBuildLocation, improvementData.maxLabor);
			cityImprovement.SetInactive();

            if (enemy)
            {
                if (td.isDiscovered)
                {
                    if (GetTerrainDataAt(city.cityLoc).isDiscovered)
                        cityImprovement.StartJustWorkAnimation();
                    else
                        cityImprovement.RevealImprovement(true);
                }
            }
            else
            {
                //if (data.producedResource != ResourceType.None && data.currentLabor > 0)
                if (resourceProducer.producedResources.Count > 0 && resourceProducer.producedResources[data.producedResourceIndex].resourceType != ResourceType.None && data.currentLabor > 0)
                {
                    //ResourceType type = data.producedResource == ResourceType.Fish ? ResourceType.Food : data.producedResource;
					ResourceType type = resourceProducer.producedResources[data.producedResourceIndex].resourceType == ResourceType.Fish ? ResourceType.Food : resourceProducer.producedResources[data.producedResourceIndex].resourceType;
					city.ChangeResourcesWorked(type, data.currentLabor);
                }
                
                cityImprovement.LoadData(data, city, this);
            }
		}
	}

    public void ColorCityImprovementRocks(ImprovementDataSO improvementData, CityImprovement cityImprovement, TerrainData td, MeshFilter[] meshes)
    {
		if (improvementData.replaceRocks)
		{
			foreach (MeshFilter mesh in cityImprovement.MeshFilter)
			{
				if (mesh.name == "Rocks")
				{
					Vector2 rockUVs = ResourceHolder.Instance.GetUVs(td.resourceType);
					Vector2[] newUVs = mesh.mesh.uv;

                    for (int i = 0; i < newUVs.Length; i++)
						newUVs[i] = rockUVs;

					mesh.mesh.uv = newUVs;

					foreach (MeshFilter mesh2 in meshes)
					{
						if (mesh2.name == "Rocks")
						{
							mesh2.mesh.uv = newUVs;
							break;
						}
					}

					if (cityImprovement.SkinnedMesh != null && cityImprovement.SkinnedMesh.name == "RocksAnim")
					{
                        Mesh newSkinnedMesh = Instantiate(cityImprovement.SkinnedMesh.sharedMesh);
                        Vector2[] skinnedUVs = cityImprovement.SkinnedMesh.sharedMesh.uv;
                        
                        for (int j = 0; j < skinnedUVs.Length; j++)
							skinnedUVs[j] = rockUVs;

                        cityImprovement.SkinnedMesh.sharedMesh = newSkinnedMesh;
						//cityImprovement.SkinnedMesh.sharedMesh.uv = skinnedUVs;
                        cityImprovement.SkinnedMesh.sharedMesh.SetUVs(0, skinnedUVs);
						Material mat = td.prop.GetComponentInChildren<MeshRenderer>().sharedMaterial;
						cityImprovement.SkinnedMesh.material = mat;
						cityImprovement.SetNewMaterial(mat);
					}

					break;
				}
			}

		}
	}

	public void CreateUpgradedImprovement(CityImprovement selectedImprovement, City city, int upgradeLevel)
	{
        string name = selectedImprovement.GetImprovementData.improvementName;
        string nameAndLevel = selectedImprovement.GetImprovementData.improvementNameAndLevel;
        string upgradeNameAndLevel = name + "-" + upgradeLevel;
        (List<ResourceValue> upgradeCost, List<ResourceValue> refundCost) = CalculateUpgradeCost(nameAndLevel, upgradeNameAndLevel, false);
		selectedImprovement.upgradeCost = upgradeCost;
        selectedImprovement.refundCost = refundCost;
		ImprovementDataSO data = UpgradeableObjectHolder.Instance.improvementDict[upgradeNameAndLevel];

		ResourceProducer resourceProducer = selectedImprovement.resourceProducer;
		selectedImprovement.BeginImprovementUpgradeProcess(city, resourceProducer, data, true);
	}

    public void CreateUpgradedUnit(Unit unit, CityImprovement improvement, City city, int upgradeLevel)
    {
        string name = unit.buildDataSO.unitType.ToString();
        string nameAndLevel = unit.buildDataSO.unitNameAndLevel;
		string upgradeNameAndLevel = name + "-" + upgradeLevel;
		(List<ResourceValue> upgradeCost, List<ResourceValue> refundCost) = CalculateUpgradeCost(nameAndLevel, upgradeNameAndLevel, true);
        improvement.upgradeCost = upgradeCost;
        improvement.refundCost = refundCost;

		ResourceProducer resourceProducer = improvement.resourceProducer;
		improvement.BeginTraining(city, resourceProducer, UpgradeableObjectHolder.Instance.unitDict[upgradeNameAndLevel], true, unit, true);
	}

	internal void MakeEnemyCamps(Dictionary<Vector3Int, Dictionary<Vector3Int, string>> enemyCampLocs, List<Vector3Int> discovered)
	{
        List<Vector3Int> enemyCampPos = new();
        
        foreach (Vector3Int loc in enemyCampLocs.Keys)
        {
            enemyCampPos.Add(loc);
            bool attacked = false;
            bool movingOut = false;
            Dictionary<Vector3Int, UnitData> fightingEnemies = new();

            EnemyCamp camp = new();
			camp.world = this;
			camp.loc = loc;
			TerrainData tdCamp = GetTerrainDataAt(loc);
			tdCamp.enemyCamp = true;
			tdCamp.enemyZone = true;
			AddToCityLabor(loc, null);

			foreach (Vector3Int tile in GetNeighborsFor(loc, State.EIGHTWAYINCREMENT))
			{
				AddToCityLabor(tile, null);
				TerrainData td = GetTerrainDataAt(tile);
				td.enemyZone = true;
			}

			enemyCampDict[loc] = camp;

			if (GameLoader.Instance.gameData.attackedEnemyBases.ContainsKey(loc))
			{
				attacked = true;
				EnemyCampData enemyData = GameLoader.Instance.gameData.attackedEnemyBases[loc];

				for (int i = 0; i < enemyData.allUnits.Count; i++)
				{
					fightingEnemies[enemyData.allUnits[i].campSpot] = enemyData.allUnits[i];
				}

				camp.enemyReady = enemyData.enemyReady;
				camp.threatLoc = enemyData.threatLoc;
				camp.forward = enemyData.forward;
				camp.revealed = enemyData.revealed;
				camp.prepping = enemyData.prepping;
				camp.attacked = enemyData.attacked;
				camp.attackReady = enemyData.attackReady;
				camp.armyReady = enemyData.armyReady;
				camp.inBattle = enemyData.inBattle;
				camp.returning = enemyData.returning;
                camp.campCount = enemyData.campCount;
				camp.infantryCount = enemyData.infantryCount;
				camp.rangedCount = enemyData.rangedCount;
				camp.cavalryCount = enemyData.cavalryCount;
				camp.seigeCount = enemyData.seigeCount;
				camp.health = enemyData.health;
				camp.strength = enemyData.strength;
                camp.timeTilReturn = enemyData.timeTilReturn;
                camp.retreat = enemyData.retreat;

                if (camp.timeTilReturn > 0)
                    StartCoroutine(camp.RetreatTimer());

                if (!world[camp.threatLoc].isLand)
                    camp.battleAtSea = true;
			}
            else if (GameLoader.Instance.gameData.movingEnemyBases.ContainsKey(loc))
            {
				EnemyCampData enemyData = GameLoader.Instance.gameData.movingEnemyBases[loc];
                movingOut = true;

				for (int i = 0; i < enemyData.allUnits.Count; i++)
				{
					fightingEnemies[enemyData.allUnits[i].campSpot] = enemyData.allUnits[i];
				}

				camp.moveToLoc = enemyData.moveToLoc;
				camp.pathToTarget = enemyData.pathToTarget;
				camp.movingOut = enemyData.movingOut;
				camp.returning = enemyData.returning;
				//camp.chasing = enemyData.chasing;
                camp.atSea = enemyData.atSea;
				camp.timeTilReturn = enemyData.timeTilReturn;

				if (camp.timeTilReturn > 0)
					StartCoroutine(camp.RetreatTimer());
			}

			bool reveal = false;
            if (discovered.Contains(loc))
                reveal = true;

			bool fullCamp = enemyCampDict[loc].campCount == 9;
			if (!fullCamp)
            {
                GameObject fire = Instantiate(Resources.Load<GameObject>("Prefabs/MiscPrefabs/Campfire"));
                fire.transform.SetParent(terrainHolder, false);
				enemyCampDict[loc].SetCampfire(fire, world[loc].isHill, reveal);
            }
			foreach (Vector3Int unitLoc in enemyCampLocs[loc].Keys)
            {
                Vector3 unitSpawn = unitLoc;
                if (tdCamp.isHill)
                {
                    unitSpawn.y += 0.15f;

                    if (unitLoc == loc)
                        unitSpawn.y += 0.5f;
                }

                UnitBuildDataSO enemyData = UpgradeableObjectHolder.Instance.enemyUnitDict[enemyCampLocs[loc][unitLoc]];
                
                Quaternion rotation;
                if (fullCamp)
                {
                    rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
                }
                else
                {
					Vector3 direction = loc - unitLoc;

					if (direction == Vector3.zero)
						rotation = Quaternion.identity;
					else
						rotation = Quaternion.LookRotation(direction, Vector3.up);
				}
				GameObject enemyGO = Instantiate(Resources.Load<GameObject>("Prefabs/" + enemyData.prefabLoc), unitSpawn, rotation);
                enemyGO.name = enemyData.unitDisplayName;
                enemyGO.transform.SetParent(enemyUnitHolder, false);
                if (!reveal)
                    enemyGO.SetActive(false);

                Unit unit = enemyGO.GetComponent<Unit>();
                if (tdCamp.CompareTag("Forest") || tdCamp.CompareTag("Forest Hill"))
                    unit.outline.ToggleOutline(true);
                    //unit.marker.ToggleVisibility(true);
		        unit.SetReferences(this);
                unit.SetMinimapIcon(enemyUnitHolder);
                if (!movingOut)
                    unit.minimapIcon.gameObject.SetActive(false);
		        unit.currentLocation = unitLoc;
                enemyCampDict[loc].UnitsInCamp.Add(unit.military);
		        unit.enemyAI.CampSpot = unitLoc;
        		unit.military.enemyCamp = enemyCampDict[loc];

				if (attacked || movingOut)
                {
                    unit.military.LoadUnitData(fightingEnemies[unitLoc]);
                    if (camp.campfire != null)
                        camp.campfire.SetActive(false);
                }
                else if (reveal)
                {
                    if (unit.buildDataSO.unitType != UnitType.Cavalry)
                        unit.military.ToggleSitting(true);
                }
            }

            if (!attacked)
    			enemyCampDict[loc].FormBattlePositions();

			Vector3 position = Vector3.zero;
	        position.y += 1;
			GameObject icon = Instantiate(Resources.Load<GameObject>("Prefabs/InGameSpritePrefabs/MinimapEnemyCamp"));
	        icon.transform.position = position;
			icon.transform.SetParent(GetTerrainDataAt(loc).transform, false);
			enemyCampDict[loc].minimapIcon = icon;
        }
	}

    public void MakeEnemyAmbushes(Dictionary<Vector3Int, EnemyAmbushData> ambushLocs, Dictionary<string, Trader> ambushedTraders)
    {
		foreach (Vector3Int tile in ambushLocs.Keys)
		{
			EnemyAmbush ambush = new();
			ambush.loc = tile;
			ambush.attackedTrader = ambushLocs[tile].attackedTrader;
            ambush.targetTrader = ambushLocs[tile].targetTrader;
            Trader unitTrader = ambushedTraders[ambush.attackedTrader];
            ambush.attackedUnits.Add(unitTrader);

            TerrainData td = GetTerrainDataAt(tile);

            if (unitTrader.guarded)
            {
                ambush.attackedUnits.Add(unitTrader.guardUnit);
            }

            for (int i = 0; i < ambushLocs[tile].attackingUnits.Count; i++)
            {
                UnitData data = ambushLocs[tile].attackingUnits[i];
				UnitBuildDataSO enemyData = UpgradeableObjectHolder.Instance.enemyUnitDict[data.unitNameAndLevel];
                
				GameObject enemyGO = Instantiate(Resources.Load<GameObject>("Prefabs/" + enemyData.prefabLoc), data.position, data.rotation);
				enemyGO.name = enemyData.unitDisplayName;
				enemyGO.transform.SetParent(enemyUnitHolder, false);

				Unit unit = enemyGO.GetComponent<Unit>();
				unit.SetMinimapIcon(enemyUnitHolder);
                //if (td.CompareTag("Forest") || td.CompareTag("Forest Hill"))
                if (!unit.bySea && !unit.byAir)
                    unit.outline.ToggleOutline(true);
					//unit.marker.ToggleVisibility(true);
				unit.SetReferences(this);
				unit.currentLocation = data.currentLocation;
                ambush.attackingUnits.Add(unit.military);
                unit.military.enemyAmbush = ambush;

				unit.military.LoadUnitData(data);
			}

            enemyAmbushDict[tile] = ambush;
		}
	}

    public void CreateUnit(IUnitData data, City city = null)
    {
        UnitBuildDataSO unitData = UpgradeableObjectHolder.Instance.unitDict[data.unitNameAndLevel];
		GameObject unitGO = Resources.Load<GameObject>("Prefabs/" + unitData.prefabLoc);

        if (data.secondaryPrefab)
            unitGO = Resources.Load<GameObject>("Prefabs/" + unitData.secondaryPrefabLoc);

		GameObject unit = Instantiate(unitGO, data.position, data.rotation); //produce unit at specified position
		unit.transform.SetParent(unitHolder, false);
		Unit newUnit = unit.GetComponent<Unit>();
		newUnit.SetReferences(this);
		newUnit.SetMinimapIcon(unitHolder);

		//assigning army details and rotation
		if (newUnit.buildDataSO.inMilitary)
        {
            if (newUnit.buildDataSO.transportationType == TransportationType.Land)
                newUnit.military.army = city.army;
			else if (newUnit.buildDataSO.transportationType == TransportationType.Sea)
				newUnit.military.army = city.navy;
			else if (newUnit.buildDataSO.transportationType == TransportationType.Air)
				newUnit.military.army = city.airForce;

			city.army.AddToArmy(newUnit.military);
            city.army.AddToOpenSpots(data.barracksBunk);
			newUnit.name = unitData.unitDisplayName;
		}

        if (newUnit.trader)
        {
            newUnit.trader.SetRouteManagers(unitMovement.uiTradeRouteManager/*, unitMovement.uiPersonalResourceInfoPanel*/); //start method is too slow and awake is too fast
            traderList.Add(newUnit.trader);
            newUnit.trader.personalResourceManager.SetUnit(newUnit);
			newUnit.trader.LoadTraderData(data.GetTraderData());

            if (newUnit.trader.ambush)
                GameLoader.Instance.ambushedTraders[newUnit.trader.name] = newUnit.trader;
        }
        else if (newUnit.laborer)
        {
            laborerList.Add(newUnit.laborer);
            newUnit.laborer.LoadLaborerData(data.GetLaborerData());
        }
        else if (newUnit.transport)
        {
            transportList.Add(newUnit.transport);
            newUnit.transport.LoadTransportData(data.GetTransportData());

            if (newUnit.buildDataSO.transportationType == TransportationType.Sea)
                waterTransport = true;
			if (newUnit.buildDataSO.transportationType == TransportationType.Air)
				airTransport = true;
        }
        else
        {
            newUnit.military.LoadUnitData(data.GetUnitData());
        }
	}

    public Transport LoadTransport(string transportName)
    {
        for (int i = 0; i < transportList.Count; i++)
        {
            if (transportList[i].name == transportName)
                return transportList[i];
        }

        return null;
    }

    public List<Vector3Int> GetSeaLandRoute(List<Vector3Int> chosenTiles, Vector3Int harborLocation, Vector3Int target, bool enemy = false)
    {
		int dist = 0;
		List<Vector3Int> chosenPath = new();
		bool firstOne = true;
		for (int i = 0; i < chosenTiles.Count; i++)
		{
            List<Vector3Int> chosenSeaPath;
			
            if (enemy)
                chosenSeaPath = GridSearch.TerrainSearchSeaEnemy(this, harborLocation, chosenTiles[i]);
            else
				chosenSeaPath = GridSearch.TerrainSearchSea(this, harborLocation, chosenTiles[i]);

			if (chosenSeaPath.Count > 0)
			{
                List<Vector3Int> chosenLandPath;
				if (enemy)
                    chosenLandPath = GridSearch.TerrainSearchEnemy(this, chosenTiles[i], target);
                else
					chosenLandPath = GridSearch.TerrainSearch(this, chosenTiles[i], target);

				if (chosenLandPath.Count > 0)
				{
					chosenSeaPath.AddRange(chosenLandPath);

					if (firstOne)
					{
						firstOne = false;
						dist = chosenSeaPath.Count;
						chosenPath = new(chosenSeaPath);
						continue;
					}

					int newDist = chosenSeaPath.Count;
					if (newDist < dist)
					{
						dist = newDist;
						chosenPath = new(chosenSeaPath);
					}
				}
			}
		}

        return chosenPath;
	}

	public void CreateGuard(UnitData data, Trader trader)
    {
		UnitBuildDataSO unitData = UpgradeableObjectHolder.Instance.unitDict[data.unitNameAndLevel];
		GameObject unitGO = Resources.Load<GameObject>("Prefabs/" + unitData.prefabLoc);
		GameObject unit = Instantiate(unitGO, data.position, data.rotation); //produce unit at specified position
		unit.transform.SetParent(unitHolder, false);
		Unit newUnit = unit.GetComponent<Unit>();
		newUnit.SetReferences(this);
		newUnit.SetMinimapIcon(unitHolder);
        newUnit.military.guardedTrader = trader;
        trader.guardUnit = newUnit;
        newUnit.originalMoveSpeed = trader.originalMoveSpeed;
		newUnit.name = unitData.unitDisplayName;

		newUnit.military.LoadUnitData(data);
	}

	public void StartSaveProcess(string saveName)
    {
        canvasHolder.SetActive(false);
        Cursor.visible = false;
        StartCoroutine(TakeScreenshot(saveName));
    }

    private IEnumerator TakeScreenshot(string saveName)
    {
        yield return new WaitForEndOfFrame();

        //ScreenCapture.CaptureScreenshot("Assets/Resources/SaveScreens/test.png");
        int width = Mathf.RoundToInt(Screen.width * 0.75f);
        int height = width / 4 * 3;
        Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        texture.ReadPixels(new Rect((Screen.width - width), (Screen.height - height), texture.width, texture.height), 0, 0);
        texture.Apply();
        byte[] bytes = texture.EncodeToPNG();
        //string bytesString = Convert.ToBase64String(bytes);
        //File.WriteAllBytes(Application.persistentDataPath + "/Screenshot.png", bytes);
		File.WriteAllBytes(Application.persistentDataPath + "/" + saveName + "Screen.png", bytes);
		//Texture2D newTexture = texture;

        Cursor.visible = true;
        canvasHolder.SetActive(true);
        int playTime = Convert.ToInt32((DateTime.Now - currentTime).TotalSeconds);
        uiMainMenu.uiSaveGame.UpdateSaveItems(saveName, playTime, version, seed/*, newTexture*/);
        //Destroy(texture);
        GameLoader.Instance.SaveGame(saveName, playTime, version/*, bytesString*/);
    }

    //it's actually "F12"
    public void HandleCtrlT()
    {
        if (!cityBuilderManager.uiCityNamer.activeStatus && !cityBuilderManager.uiTraderNamer.activeStatus)
            StartCoroutine(TakeScreenshot());
    }

    private IEnumerator TakeScreenshot()
    {
        canvasHolder.SetActive(false);
        DateTime now = DateTime.Now;
        ScreenCapture.CaptureScreenshot("screenshot" + now.Month + "-" + now.Day + "-" + now.Year + "-" + now.Hour+ "-" + now.Minute + ".png", 1);
        Debug.Log("Took a screenshot");

        yield return new WaitForEndOfFrame();

		//canvasHolder.SetActive(true);
	}

    //probably isn't necessary
	public void ClearMap()
    {
        foreach (Transform go in terrainHolder)
            Destroy(go.gameObject);

        foreach (Transform go in picGOHolder)
            Destroy(go.gameObject);

        world.Clear();
        mapHandler.ClearResourceDict();

		//foreach (Transform go in tradeCenterHolder)
		//	Destroy(go.gameObject);

		//      foreach (Transform go in enemyUnitHolder)
		//          Destroy(go.gameObject);

		//      foreach (Vector3Int loc in enemyCampDict.Keys)
		//          Destroy(enemyCampDict[loc].minimapIcon);

		//      foreach (Vector3Int loc in resourceIconDict.Keys)
		//          Destroy(resourceIconDict[loc].gameObject);

		//      tradeStopDict.Clear();
		//      tradeStopNameDict.Clear();
		//      allWonders.Clear();
		//      wonderTiles.Clear();
		//      allTradeCenters.Clear();
		//      centerBordersDict.Clear();
		//      researchWaitList.Clear();
		//      goldWaitList.Clear();
		//      cityWorkedTileDict.Clear();
		//      buildingPosDict.Clear();
		//      noWalkList.Clear();
		//      cityNamesMaps.Clear();
		//      cityDict.Clear();
		//      cityImprovementDict.Clear();
		//      unclaimedSingleBuildList.Clear();
		//      enemyCampDict.Clear();
		//      playerPosDict.Clear();
		//      traderPosDict.Clear();
		//      traderStallDict.Clear();
		//      unitPosDict.Clear();
		//      npcPosDict.Clear();
		//      laborerList.Clear();
		//      resourceIconDict.Clear();
		//      mapHandler.ResetResourceLocDict();
		//      traderList.Clear();
		//      transportList.Clear();
		//      //upgradeableObjectMaxLevelDict.Clear();
		//      currentWorkedTileDict.Clear();
		//      maxWorkedTileDict.Clear();
		//      roadTileDict.Clear();
		//      soloRoadLocsList.Clear();
		//      roadLocsList.Clear();
		//      coastCoastList.Clear();
		//      allTCReps.Clear();
		//      allEnemyLeaders.Clear();
		//      enemyBordersDict.Clear();
		//      enemyAmbushDict.Clear();
		//      enemyCityDict.Clear();
		//      militaryStationLocs.Clear();
		//      treasureLocs.Clear();
		//      neutralZones.Clear();
		//      resourceSelectionGridList.Clear();
		//      //resourceDiscoveredList.Clear();
		//      newUnitsAndImprovements.Clear();
		//      ambushUnitDict.Clear();
		//      battleLocs.Clear();
		//      unitsSpeedChangeDict.Clear();
		//      resourceYieldChangeDict.Clear();
	}

	public void LoadEnemyBorders(Vector3Int enemyLoc, Color color)
	{
		for (int j = 0; j < ProceduralGeneration.neighborsEightDirections.Count; j++)
		{
			Vector3Int tile = enemyLoc + ProceduralGeneration.neighborsEightDirections[j];

			for (int k = 0; k < ProceduralGeneration.neighborsFourDirections.Count; k++)
			{
				Vector3Int newTile = tile + ProceduralGeneration.neighborsFourDirections[k];

				if (!world[newTile].enemyZone)
				{
                    Vector3Int borderLocation = newTile - tile;
                    Vector3 borderPosition = tile;
					Quaternion rotation = Quaternion.identity;

					if (borderLocation.x != 0)
					{
                        borderPosition.x += 0.5f * borderLocation.x;//(borderPosition.x / 3 * 0.99f);
						rotation = Quaternion.Euler(0, 90, 0); //only need to rotate on this one
                        borderPosition.x += borderLocation.x > 0 ? -.01f : .01f;
					}
					else if (borderLocation.z != 0)
					{
                        borderPosition.z += 0.5f * borderLocation.z;//(borderPosition.z / 3 * 0.99f);
                        borderPosition.z += borderLocation.z > 0 ? -.01f : .01f;
					}

					GameObject border = Instantiate(Resources.Load<GameObject>("Prefabs/InGameSpritePrefabs/EnemyBorder"), borderPosition, rotation);
					border.GetComponent<SpriteRenderer>().color = color;
					border.transform.SetParent(terrainHolder, false);

                    if (!enemyBordersDict.ContainsKey(tile))
                        enemyBordersDict[tile] = new();

                    enemyBordersDict[tile].Add(border);

                    if (!world[tile].isDiscovered)
    					border.SetActive(false);
				}
			}
		}
	}

    private void LoadTradeCenterBorders(Vector3Int centerLoc)
    {
        List<Vector3Int> borderTiles = GetNeighborsFor(centerLoc, State.EIGHTWAYINCREMENT);
        for (int j = 0; j < borderTiles.Count; j++)
		{
            List<Vector3Int> borderBorderTiles = GetNeighborsFor(borderTiles[j], State.FOURWAYINCREMENT);
            for (int k = 0; k < borderBorderTiles.Count; k++)
			{
				if (!borderTiles.Contains(borderBorderTiles[k]) && borderBorderTiles[k] != centerLoc)
                {
                    Vector3Int borderLocation = borderBorderTiles[k] - borderTiles[j];
				    Vector3 borderPosition = borderTiles[j];
				    Quaternion rotation = Quaternion.identity;

				    if (borderLocation.x != 0)
				    {
					    borderPosition.x += 0.5f * borderLocation.x;//(borderPosition.x / 3 * 0.99f);
					    rotation = Quaternion.Euler(0, 90, 0); //only need to rotate on this one
					    borderPosition.x += borderLocation.x > 0 ? -.01f : .01f;
				    }
				    else if (borderLocation.z != 0)
				    {
					    borderPosition.z += 0.5f * borderLocation.z;//(borderPosition.z / 3 * 0.99f);
					    borderPosition.z += borderLocation.z > 0 ? -.01f : .01f;
				    }

				    GameObject border = Instantiate(Resources.Load<GameObject>("Prefabs/InGameSpritePrefabs/EnemyBorder"), borderPosition, rotation);
                    border.GetComponent<SpriteRenderer>().color = new Color(0.43f, 0, 1, 0.68f); 
                    border.transform.SetParent(terrainHolder, false);

				    if (!centerBordersDict.ContainsKey(borderTiles[j]))
					    centerBordersDict[borderTiles[j]] = new();

				    centerBordersDict[borderTiles[j]].Add(border);

				    if (!world[borderTiles[j]].isDiscovered)
					    border.SetActive(false);
                }
			}
		}
	}

	public void AaddGold(int amount) //for testing, on a button
    {
		UpdateWorldGold(amount);

        //resourceYieldChangeDict[ResourceType.Food] = .5f;
        bridgeResearched = true;

        //for (int i = 0; i < roadLocsList.Count; i++)
        //    Debug.Log(roadLocsList[i]);
        //      List<int> num = new() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        //      List<int> newNum = new();

        //      for (int i = 0; i < 10; i++)
        //      {
        //          int rand = num[UnityEngine.Random.Range(0, num.Count)];
        //          num.Remove(rand);
        //          newNum.Add(rand);
        //      }

        //      for (int i = 0; i < newNum.Count; i++)
        //      {
        //          Debug.Log(newNum[i]);
        //      }

        //      for (int i = 0; i < 10; i++)
        //      {
        //          for (int j = i + 1; j < 10; j++)
        //          {
        //              if (newNum[i] < newNum[j])
        //              {
        //                  int oldNum = newNum[j];
        //                  newNum.Remove(oldNum);
        //                  newNum.Insert(i, oldNum);
        //              }
        //          }
        //      }

        //for (int i = 0; i < newNum.Count; i++)
        //{
        //	Debug.Log(newNum[i]);
        //}
    }

	public void SetMountainMiddle(TerrainData td, int i, bool grassland)
    {
		int diff = Mathf.RoundToInt(td.main.rotation.eulerAngles.y / 90);

		i -= diff;
		if (i < 0)
			i += 4;

		Vector3 pos;
		if (i == 0)
			pos = new Vector3(0, 0, 1);
		else if (i == 1)
			pos = new Vector3(1, 0, 0);
		else if (i == 2)
			pos = new Vector3(0, 0, -1);
		else
			pos = new Vector3(-1, 0, 0);
		Quaternion rotation = Quaternion.Euler(0, i * 90, 0);
		GameObject mmGO = Instantiate(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/MountainMiddle"), pos, rotation);
        mmGO.name = "MountainMiddle";
		GameObject mmNonStatic = Instantiate(Resources.Load<GameObject>("Prefabs/TerrainPrefabs/MountainMiddle"), pos, rotation);
		mmGO.transform.SetParent(td.main, false);
		mmNonStatic.transform.SetParent(td.nonstatic, false);

		if (grassland)
		{
			MeshFilter mesh = mmGO.GetComponentInChildren<MeshFilter>();
			Vector2[] allUVs = mesh.mesh.uv;

			for (int j = 0; j < allUVs.Length; j++)
				allUVs[j].x += -0.062f;

			mesh.mesh.uv = allUVs;
			mmNonStatic.GetComponentInChildren<MeshFilter>().mesh.uv = allUVs;
		}
	}

    public void PlayCityAudio(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.Play();
        ambienceAudio.PauseAmbience();
    }

    public void StopAudio()
    {
        audioSource.Stop();
        ambienceAudio.RestartAmbience();
        ambienceAudio.AmbienceCheck();
    }

    private void DeactivateCanvases()
    {
        immoveableCanvas.gameObject.SetActive(false);
        cityCanvas.gameObject.SetActive(false);
        personalResourceCanvas.gameObject.SetActive(false);
        tcCanvas.gameObject.SetActive(false);
        wonderCanvas.gameObject.SetActive(false);
        traderCanvas.gameObject.SetActive(false);
        workerCanvas.gameObject.SetActive(false);
        tradeRouteManagerCanvas.gameObject.SetActive(false);
        infoPopUpCanvas.gameObject.SetActive(false);
        overflowGridCanvas.gameObject.SetActive(false);
    }

    public int[] GetGrasslandCount(TerrainData td)
    {
		int[] grasslandCount = new int[4];
        int i = 0;

		foreach (Vector3Int neighbor in GetNeighborsFor(td.TileCoordinates, State.FOURWAYINCREMENT))
		{
			if (GetTerrainDataAt(neighbor).terrainData.grassland)
				grasslandCount[i] = 1;
			else
				grasslandCount[i] = 0;

			i++;
		}

		return grasslandCount;
	}

    public void ConfigureUVs(TerrainData td)
    {
        TerrainDesc desc = td.terrainData.terrainDesc;
        if (td.terrainData.type != TerrainType.Coast && desc != TerrainDesc.Desert && desc != TerrainDesc.DesertFloodPlain && desc != TerrainDesc.DesertHill && desc != TerrainDesc.River)
            return;

        int[] grasslandCount = GetGrasslandCount(td);
        
        if (grasslandCount.Sum() == 0)
        {
            td.SetMinimapIcon();
            return;
        }

        Vector2[] uvs = SetUVMap(grasslandCount, SetUVShift(desc), Mathf.RoundToInt(td.main.eulerAngles.y));
        Vector2[] newUVs = td.mainMesh.sharedMesh.uv;
		if (newUVs.Length > 4)
            uvs = NormalizeUVs(uvs, newUVs);
        td.SetUVs(uvs);
	}

    public float SetUVShift(TerrainDesc desc)
    {
        float interval = .0625f; 
        float shift = 0;
        
        switch (desc)
        {
            case TerrainDesc.River:
                shift = interval * 9;
                break;
            case TerrainDesc.Sea:
                shift = interval * 9;
                break;
            case TerrainDesc.DesertHill:
                shift = interval * 7;
                break;
        }

        return shift;
    }

    //for setting the uv map to transition between terrains, also includes uv coordinates rotation
    public Vector2[] SetUVMap(int[] count, float shift, int eulerAngle)
    {
        Vector2[] uvMap = new Vector2[4];
        int rotation = 0;
        float change = 0.0025f;
        //if (river) 
        int currentRot = eulerAngle / 90; //to offset tile rotation

        switch (count.Sum())
        {
            case 0:
                uvMap = borderZero.sharedMesh.uv;
                break;
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
                        if (rotation != 2)
                        {
                            uvMap[0] += new Vector2(change, -change);
                            uvMap[1] += new Vector2(change, change);
                            uvMap[2] += new Vector2(-change, change);
                            uvMap[3] += new Vector2(-change, -change);
                        }
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
    public Vector2[] NormalizeUVs(Vector2[] terrainUVs, Vector2[] newUVs, int rot = 0)
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
			//rotating uvs
			if (rot > 0)
			{                
                float xMaxDiff = newMaxX - uv.x;
				float yMaxDiff = newMaxY - uv.y;

				switch (rot)
				{
					case 1:
						uv.x = newMinX + yMaxDiff;
						uv.y = newMaxY - xMaxDiff;
						break;
					case 2:
						uv.x = newMinX + xMaxDiff;
						uv.y = newMinY + yMaxDiff;
						break;
					case 3:
						uv.x = newMaxX - yMaxDiff;
						uv.y = newMinY + xMaxDiff;
						break;
				}
			}
            
            uv.x = minX + rangeX * ((uv.x - newMinX) / newRangeX);
            uv.y = minY + rangeY * ((uv.y - newMinY) / newRangeY);

			newUVs[i] = uv;
            i++;
        }

        return newUVs;
    }

    public void UpgradeAzai(UnitBuildDataSO data)
    {
		GameObject unit = Instantiate(Resources.Load<GameObject>("Prefabs/" + data.prefabLoc), Vector3.zero, Quaternion.identity); //produce unit at specified position
		unit.transform.SetParent(cityBuilderManager.friendlyUnitHolder, false);
		//for tweening
		Vector3 goScale = unit.transform.localScale;
		float scaleX = goScale.x;
		float scaleZ = goScale.z;
		unit.transform.localScale = new Vector3(scaleX, 0.1f, scaleZ);
		LeanTween.scale(unit, goScale, 0.5f).setEase(LeanTweenType.easeOutBack);
		Unit newAzai = unit.GetComponent<Unit>();

		characterUnits.Remove(azai);
		azai.DestroyUnit();
		newAzai.name = "Azai";
		azai = newAzai.GetComponent<BodyGuard>();
		characterUnits.Add(newAzai);
		uiSpeechWindow.AddToSpeakingDict("Azai", newAzai);
		newAzai.SetReferences(this);
		azai.SetArmy();
	}

    public void OpenMainMenu()
    {
        if (uiMainMenu.activeStatus)
        {
            cityBuilderManager.PlaySelectAudio();
            uiMainMenu.ToggleVisibility(false);
		}
        else
        {
			if (hideUI)
				return;

			if (unitOrders || buildingWonder)
				CloseBuildingSomethingPanel();

			uiMainMenu.ToggleVisibility(true);
        }
    }

    private void CreateGrid()
    {
        int gridSizeX = 54, gridSizeY = 39; //temporary size for testing
        int offsetX = 22, offsetY = 16;  
        
        grid = new PathNode[gridSizeX, gridSizeY];

        for (int i = 0; i < gridSizeX; i++)
        {
            for (int j = 0; j < gridSizeY; j++)
            {
                int x = i - offsetX;
                int y = j - offsetY;
                
                Vector3Int pos = new Vector3Int(x, 0, y);
                Vector3Int terrainPos = GetClosestTerrainLoc(pos);
                bool isObstacle = GetTerrainDataAt(terrainPos).terrainData.type == TerrainType.Obstacle;
                bool isSea = !GetTerrainDataAt(terrainPos).terrainData.isLand;

                grid[i, j] = new PathNode(isObstacle, isSea, pos, x, y, world[pos].MovementCost);
            }
        }
    }

    public PathNode GetPathNode(Vector3Int loc)
    {
        return grid[loc.x + 22, loc.z + 16];
    }

    //private void CreateParticleSystems()
    //{
    //    lightBeam = Instantiate(lightBeam, new Vector3(0, 0, 0), Quaternion.Euler(-90, 0, 0));
    //    lightBeam.transform.SetParent(transform, false);
    //    lightBeam.Pause();
    //}

    public void CreateLightBeam(Vector3 loc)
    {
        loc.y += 2f;
		ParticleSystem lightBeam = Instantiate(Resources.Load<ParticleSystem>("Prefabs/ParticlePrefabs/LightBullet"), loc, Quaternion.Euler(-90, 0, 0));
        lightBeam.transform.SetParent(psHolder, false);
        lightBeam.Play();
    }

    public void CreateGodRay(Vector3 loc)
    {
		ParticleSystem godRay = Instantiate(Resources.Load<ParticleSystem>("Prefabs/ParticlePrefabs/Godrays"));
        godRay.transform.position = loc;
        godRay.transform.SetParent(psHolder, false);
        godRay.Play();
	}

    public void UnselectAll()
    {
        CloseCampTooltipButton();
        CloseTradeRouteBeginTooltipButton();
        cityBuilderManager.ResetCityUI();
        unitMovement.ClearSelection();
        cityBuilderManager.UnselectWonder();
        cityBuilderManager.UnselectTradeCenter();
        //unitMovement.LoadUnloadFinish(false);
        researchTree.ToggleVisibility(false);
        uiProfitabilityStats.ToggleVisibility(false);
        wonderHandler.ToggleVisibility(false);
        //mapPanel.ToggleVisibility(false);
        wonderButton.ToggleButtonColor(false);
        conversationListButton.ToggleButtonColor(false);
        uiConversationTaskManager.ToggleVisibility(false);
        CloseMap();
        CloseTerrainTooltipButton();
        CloseTransferTooltip();
        CloseImprovementTooltipButton();
	}

    public void ToggleMinimap(bool v)
    {
		if (v)
        {
            LeanTween.moveX(mapHandler.minimapHolder, mapHandler.minimapHolder.anchoredPosition3D.x + -400f, 0.5f).setEaseOutSine();
            LeanTween.moveX(mapHandler.minimapRing, mapHandler.minimapRing.anchoredPosition3D.x + -400f, 0.5f).setEaseOutSine();
            LeanTween.moveX(wonderButton.allContents, wonderButton.allContents.anchoredPosition3D.x + -400f, 0.5f).setEaseOutSine();
			LeanTween.moveX(conversationListButton.allContents, conversationListButton.allContents.anchoredPosition3D.x + -400f, 0.5f).setEaseOutSine();
            LeanTween.moveX(mapPanelButton, mapPanelButton.anchoredPosition3D.x + -400f, 0.5f).setEaseOutSine();
            LeanTween.moveX(uiMainMenuButton.allContents, uiMainMenuButton.allContents.anchoredPosition3D.x + -400f, 0.5f).setEaseOutSine();
            LeanTween.moveX(uiTomFinder.allContents, uiTomFinder.allContents.anchoredPosition3D.x + -400f, 0.5f).setEaseOutSine();
            LeanTween.moveX(uiAttackWarning.allContents, uiAttackWarning.allContents.anchoredPosition3D.x + -400f, 0.5f).setEaseOutSine();
		}
        else
        {
            LeanTween.moveX(mapHandler.minimapHolder, mapHandler.minimapHolder.anchoredPosition3D.x + 400f, 0.3f);
            LeanTween.moveX(mapHandler.minimapRing, mapHandler.minimapRing.anchoredPosition3D.x + 400f, 0.3f);
            LeanTween.moveX(wonderButton.allContents, wonderButton.allContents.anchoredPosition3D.x + 400f, 0.3f);
			LeanTween.moveX(conversationListButton.allContents, conversationListButton.allContents.anchoredPosition3D.x + 400f, 0.3f);
            LeanTween.moveX(mapPanelButton, mapPanelButton.anchoredPosition3D.x + 400f, 0.3f);
            LeanTween.moveX(uiMainMenuButton.allContents, uiMainMenuButton.allContents.anchoredPosition3D.x + 400f, 0.3f);
			LeanTween.moveX(uiTomFinder.allContents, uiTomFinder.allContents.anchoredPosition3D.x + 400f, 0.3f);
			LeanTween.moveX(uiAttackWarning.allContents, uiAttackWarning.allContents.anchoredPosition3D.x + 400f, 0.3f);
		}
    }

    public void ToggleWorldResourceUI(bool v)
    {
        if (v)
        {
            LeanTween.moveY(uiWorldResources.allContents, uiWorldResources.allContents.anchoredPosition3D.y + -400f, 0.3f);
        }
        else
        {
            LeanTween.moveY(uiWorldResources.allContents, uiWorldResources.allContents.anchoredPosition3D.y + 400f, 0.5f);
        }
    }

    public void Handle1()
    {
        if (unitOrders)
            return;
        
        if (cityBuilderManager.uiCityNamer.activeStatus || cityBuilderManager.uiTraderNamer.activeStatus || cityBuilderManager.uiMarketPlaceManager.isTyping)
            return;
        
        if (uiMainMenu.activeStatus || mapHandler.activeStatus)
            return;

        if (unitMovement.uiTradeRouteManager.activeStatus && unitMovement.uiTradeRouteManager.uiTradeResourceNum.activeStatus)
            return;
        
        if (mainPlayer.isSelected)
        {
            unitMovement.CenterCamOnUnit();
        }
        else
        {
            UnselectAll();
            unitMovement.SelectUnitPrep(mainPlayer);
        }
    }

    public void HandleTilde()
    {
        showConsole = !showConsole;
        consoleInput = "";
    }

	private void OnGUI()
	{
		if (!showConsole) { return; }

        float y = 0f;

        //GUI.SetNextControlName("test");
        GUI.Box(new Rect(0, y, Screen.width, 30), "");
        GUI.backgroundColor = new Color(0, 0, 0, 0);
        consoleInput = GUI.TextField(new Rect(10f, y + 5f, Screen.width - 20f, 20f), consoleInput);

        //if (Event.current.isKey && Event.current.Equals(Event.KeyboardEvent("w")))// && GUI.GetNameOfFocusedControl() == "test")
        //    Debug.Log("Pass");
	}

    public void GiveToCity(City city, ResourceType type, int amount)
    {
        if (!ResourceCheck(type))
            DiscoverResource(type);
        
        city.resourceManager.AddResource(type, amount);
    }

    public void HandleReturn()
    {
        if (showConsole)
        {
            string[] words = consoleInput.Split(" ");
            
            if (words.Length > 0)
            {
                //"dammi "int"" gives gold
                if (words[0] == "dammi")
                {
                    if (words.Length > 1)
                    {
                        if (int.TryParse(words[1], out int amount))
                            AaddGold(amount);
                    }
                }
                //"tido "ResourceType" "int" "city_name"" gives eresource to city
                else if (words[0] == "tido")
                {
                    if (words.Length > 3)
                    {
                        string cityName = "";

                        for (int i = 3; i < words.Length; i++)
                        {
                            cityName += words[i];

                            if (i != words.Length - 1)
                                cityName += " ";
                        }
                        
                        if (tradeStopNameDict.ContainsKey(cityName))
                        {
                            City city = tradeStopNameDict[cityName].city;
                            
                            if (city != null)
                            {
                                if (Enum.TryParse(words[1], out ResourceType type))
                                {
                                    if (int.TryParse(words[2], out int amount))
                                        GiveToCity(city, type, amount);
                                }
                            }
                        }
                    }
                }
                //"mostra tutto" shows all building options
                else if ( words.Length > 1 && words[0] == "mostra" && words[1] == "tutto")
                {
                    showAllBuildOptions = true;

                    foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
                    {
                        if (!ResourceCheck(resource.resourceType))
                            DiscoverResource(resource.resourceType);
                    }
                }
            }

            consoleInput = "";
        }
    }

	public void HandleEsc()
    {
        if (showConsole)
        {
            HandleTilde();
        }
        else if (buildingWonder)
        {
            CloseBuildingSomethingPanel();
		}
        else if (uiCityPopIncreasePanel.activeStatus)
        {
            cityBuilderManager.CloseAddPopWindow();
        }
        else if (uiTradeRouteBeginTooltip.gameObject.activeSelf)
        {
            CloseTradeRouteBeginTooltipCloseButton();
        }
        else if (uiCampTooltip.activeStatus)
        {
            unitMovement.CancelArmyDeploymentButton();
        }
        else if (cityBuilderManager.uiCityUpgradePanel.activeStatus)
        {
            cityBuilderManager.uiCityUpgradePanel.CloseWindow();
        }
        else if (researchTree.researchTooltip.activeStatus)
        {
            researchTree.researchTooltip.CloseWindow();
        }
        else if (uiCityImprovementTip.activeStatus)
        {
            CloseImprovementTooltipCloseButton();
        }
        else if (uiTerrainTooltip.activeStatus)
        {
            CloseTerrainTooltipCloseButton();
        }
        else if (unitMovement.uiTradeRouteManager.activeStatus)
        {
            unitMovement.uiTradeRouteManager.CloseMenu();
        }
        else if (uiMainMenu.activeStatus)
        {
            if (uiMainMenu.uiSettings.activeStatus)
            {
                uiMainMenu.uiSettings.CloseWindowButton();
            }
            else if (uiMainMenu.uiSaveGame.activeStatus)
            {
                uiMainMenu.uiSaveGame.CloseSaveGameButton();
            }
            else
            {
                uiMainMenu.CloseMenuButton();
            }
        }
        else if (unitMovement.uiBuildingSomething.activeStatus)
        {
            unitMovement.CloseBuildingSomethingPanelButton();
        }
        else if (unitMovement.selectedUnit != null && unitMovement.uiCancelTask.activeStatus)
        {
			if (unitMovement.selectedUnit.worker && unitMovement.selectedUnit.worker.isBusy)
			{
				unitMovement.workerTaskManager.CancelTask();
			}
   //         else if (unitMovement.selectedUnit.military && unitMovement.selectedUnit.military.guard)
   //         {
			//	unitMovement.CancelOrders();
			//}
            else if (unitMovement.selectedUnit.buildDataSO.inMilitary && (/*unitMovement.selectedUnit.military.isMarching *//*|| unitMovement.selectedUnit.military.army.inBattle */
                unitMovement.selectedUnit.military.army.traveling || unitMovement.selectedUnit.military.army.atHome))
            {
                unitMovement.CancelOrders();
            }
			//else if (unitMovement.selectedUnit.isMoving)
			//{
			//	unitMovement.CancelContinuedMovementOrders();
			//}
			else if (unitMovement.selectedUnit.trader.followingRoute)
			{
				unitMovement.CancelTradeRoute();
			}
            else
            {
			    unitMovement.ClearSelection();
			    somethingSelected = false;
            }
		}
        else if (somethingSelected)
        {
            UnselectAll();
			somethingSelected = false;
		}
        else if (cityBuilderManager.SelectedCity != null)
        {
			if (cityBuilderManager.uiCityTabs.openTab)
            {
				cityBuilderManager.uiCityTabs.HideSelectedTab(false);
            }
			else if (cityBuilderManager.upgradingImprovement || cityBuilderManager.removingImprovement || cityBuilderManager.laborChange != 0 || cityBuilderManager.improvementData != null)
            {
				cityBuilderManager.ResetCityUIToBase();
            }
            else if (cityBuilderManager.uiMarketPlaceManager.activeStatus)
            {
                cityBuilderManager.CloseSellResources();
			}
            else if (cityBuilderManager.uiCityNamer.activeStatus)
            {
                cityBuilderManager.uiCityNamer.ToggleVisibility(false);
            }
			else
            {
                cityBuilderManager.ResetCityUI();
                somethingSelected = false;
            }
        }
        else
        {
            uiMainMenu.ToggleVisibility(true);
        }
    }

    public void HandleJ()
    {
		if (!cityBuilderManager.uiCityNamer.activeStatus && !cityBuilderManager.uiTraderNamer.activeStatus)
			OpenConversationList();
    }

    public void HandleK()
    {
		//if (!cityBuilderManager.uiCityNamer.activeStatus && !cityBuilderManager.uiTraderNamer.activeStatus)
		//	uiTomFinder.FindTom();
    }

    public void HandleM()
    {
		if (!cityBuilderManager.uiCityNamer.activeStatus && !cityBuilderManager.uiTraderNamer.activeStatus)
			mapHandler.ToggleMap();
    }

    public void HandleN()
    {
        if (!cityBuilderManager.uiCityNamer.activeStatus && !cityBuilderManager.uiTraderNamer.activeStatus)
            OpenWonders();
    }

    public void HandleR()
    {
        if (buildingWonder)
            RotateWonderPlacement();
    }

    public void HandleI()
    {
		if (!cityBuilderManager.uiCityNamer.activeStatus && !cityBuilderManager.uiTraderNamer.activeStatus)
			OpenResearchTree();
    }

    //wonder info
    public void HandleWonderPlacement(Vector3 location, GameObject detectedObject)
    {
        if (!buildingWonder)
            return;

        somethingSelected = false;
        uiRotateWonder.ToggleVisibility(false);
        wonderNoWalkLoc.Clear();
		rotation = Quaternion.Euler(0, 0, 0);
        unloadLoc = Vector3Int.zero;

		if (buildingWonder) //only thing that works is placing wonder
        {
            if (wonderPlacementLoc != null)
                Destroy(wonderGhost);

            Vector3Int locationPos = GetClosestTerrainLoc(location);
            uiConfirmWonderBuild.ToggleVisibility(false);

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
                if (CheckForUnits(tile))
                {
                    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Unit in the way");
                    wonderPlacementLoc.Clear();
                    wonderNoWalkLoc.Clear();
                    return;
                }
               
                if (GetTerrainDataAt(tile).hasBattle)
                {
					UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Battle is here");
					wonderPlacementLoc.Clear();
					wonderNoWalkLoc.Clear();
					return;
				}

                foreach (Vector3Int neighbor in GetNeighborsFor(tile, State.EIGHTWAY))
                {
                    if (neighbor.x == xMin || neighbor.x == xMax || neighbor.z == zMin || neighbor.z == zMax)
                        continue;

                    wonderNoWalkLoc.Add(neighbor);
                }

                wonderNoWalkLoc.Add(tile);
            }

            uiRotateWonder.ToggleVisibility(true);

            wonderGhost = Instantiate(Resources.Load<GameObject>("Prefabs/WonderPrefabs/" + wonderData.wonderPrefabName), avgLoc / wonderLocList.Count, rotation);
            wonderGhost.transform.SetParent(wonderHolder, false);
            Wonder wonderInfo = wonderGhost.GetComponent<Wonder>();
            wonderInfo.SetLastPrefab(); //only showing 100 Perc prefab
            if (wonderInfo.wonderCollider != null)
                wonderInfo.wonderCollider.SetActive(false);
            //Color newColor = new(1, 1, 1, .75f);
            MeshRenderer[] renderers = wonderGhost.GetComponentsInChildren<MeshRenderer>();

            //assigning transparent material to all meshrenderers
            foreach (MeshRenderer render in renderers)
            {
                render.material = transparentMat;
                //Material[] newMats = render.materials;

                //for (int i = 0; i < newMats.Length; i++)
                //{
                //    Material newMat = new(transparentMat);
                //    newMat.color = newColor;
                //    newMat.SetTexture("_BaseMap", newMats[i].mainTexture);
                //    newMats[i] = newMat;
                //}

                //render.materials = newMats;
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

            uiConfirmWonderBuild.ToggleVisibility(true);
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
        uiConfirmWonderBuild.ToggleVisibility(false);
        buildingWonder = false;
        uiBuildingSomething.ToggleVisibility(false);
        uiRotateWonder.ToggleVisibility(false);

        if (wonderPlacementLoc.Count == 0)
            return;

        //final check if clear
        foreach (Vector3Int tile in wonderPlacementLoc)
        {
			TerrainData td = GetTerrainDataAt(tile);

			if ((tile != finalUnloadLoc && !IsTileOpenCheck(tile)) || (tile == finalUnloadLoc && !IsTileOpenButRoadCheck(tile)) || CheckForUnits(tile))
			{
				InfoPopUpHandler.WarningMessage(objectPoolItemHolder).Create(tile, "Something in the way");
				wonderPlacementLoc.Clear();
				return;
			}

			if (td.hasBattle)
			{
				InfoPopUpHandler.WarningMessage(objectPoolItemHolder).Create(tile, "Battle is here");
				wonderPlacementLoc.Clear();
				return;
			}
        }

        Vector3 avgLoc = Vector3.zero;

        //prep terrain
        foreach(Vector3Int tile in wonderPlacementLoc)
        {
            CheckTileForTreasure(tile);
			TerrainData td = GetTerrainDataAt(tile);
			avgLoc += tile;

			if (wonderData.isSea)
            {
                td.sailable = false;
                td.walkable = true;
                td.canWalk = true;
                td.canPlayerWalk = true;
            }
            else
            {
                if (td.prop != null)
                    td.ShowProp(false);

                td.ToggleTerrainMesh(false);
                if (td.hasResourceMap)
                    td.HideResourceMap();
            }
        }
        //setting up wonder info
        Vector3 centerPos = avgLoc / wonderPlacementLoc.Count;
        GameObject wonderGO = Instantiate(Resources.Load<GameObject>("Prefabs/WonderPrefabs/" + wonderData.wonderPrefabName), centerPos, rotation);
        wonderGO.gameObject.transform.SetParent(wonderHolder, false);
        Wonder wonder = wonderGO.GetComponent<Wonder>();
		wonder.mapIcon.transform.localRotation = Quaternion.Inverse(rotation);
		Vector3 angles = wonder.mapIcon.transform.localEulerAngles;
		angles.x = 90;
		wonder.mapIcon.transform.localEulerAngles = angles;
		wonder.wonderName = wonderData.wonderName;
        wonderGO.name = wonder.wonderName;
        allWonders.Add(wonder);
        wonder.SetReferences(this, cityBuilderManager.focusCam);
        wonder.wonderData = wonderData;
        wonder.wonderLocs = new(wonderPlacementLoc);
        wonder.SetPrefabs(false);
        wonder.SetResourceDict(wonderData.wonderCost, false);
        wonder.unloadLoc = finalUnloadLoc;
        wonder.singleBuildDict[SingleBuildType.TradeDepot] = wonder.unloadLoc;
        wonder.SetExclamationPoint();
        AddStop(finalUnloadLoc, wonder);
        wonderNoWalkLoc.Remove(finalUnloadLoc);
        wonder.hadRoad = IsRoadOnTerrain(finalUnloadLoc);
        wonder.SetCenterPos(centerPos);
        tradeStopNameDict[wonder.wonderName] = wonder;
        foreach (Vector3Int tile in wonderPlacementLoc)
            wonderTiles.Add(tile);

        //building road in unload area
        BuildRoadWithObject(finalUnloadLoc, false);

        //claiming the area for the wonder
        List<Vector3Int> harborTiles = new();
        foreach (Vector3Int tile in wonderPlacementLoc)
        {
            AddToCityLabor(tile, null); //so cities can't take the spot
            AddStructure(tile, wonderGO); //so nothing else can be built there

            //checking if there's a spot to build harbor
            foreach (Vector3Int neighbor in GetNeighborsFor(tile, State.FOURWAYINCREMENT))
            {
                if (wonderPlacementLoc.Contains(neighbor))
                    continue;

                TerrainData td = GetTerrainDataAt(neighbor);

                if (wonderData.isSea)
                {
                    if (td.terrainData.type == TerrainType.Coast || td.terrainData.type == TerrainType.Sea)
                    {
                        harborTiles.Add(neighbor);

                        //adding new coast tiles for boat travel
                        Vector3Int newCoastFinder = (tile - neighbor) / 3;
                        coastCoastList.Add(neighbor + newCoastFinder);
                        wonder.coastTiles.Add(neighbor + newCoastFinder);
                        if (newCoastFinder.x != 0)
                        {
                            coastCoastList.Add(neighbor + newCoastFinder + new Vector3Int(0, 0, 1));
                            wonder.coastTiles.Add(neighbor + newCoastFinder + new Vector3Int(0, 0, 1));
                            coastCoastList.Add(neighbor + newCoastFinder + new Vector3Int(0, 0, -1));
                            wonder.coastTiles.Add(neighbor + newCoastFinder + new Vector3Int(0, 0, -1));
                        }
                        else if (newCoastFinder.z != 0)
                        {
						    coastCoastList.Add(neighbor + newCoastFinder + new Vector3Int(1, 0, 0));
                            wonder.coastTiles.Add(neighbor + newCoastFinder + new Vector3Int(1, 0, 0));
                            coastCoastList.Add(neighbor + newCoastFinder + new Vector3Int(-1, 0, 0));
                            wonder.coastTiles.Add(neighbor + newCoastFinder + new Vector3Int(-1, 0, 0));
					    }
                    }

                }
                else
                {
                    if (td.terrainData.type == TerrainType.Coast || td.terrainData.type == TerrainType.River)
                        harborTiles.Add(neighbor);
                }
            }

            if (harborTiles.Count > 0)
                wonder.canBuildHarbor = true;
        }

        wonder.possibleHarborLocs = harborTiles;

        noWalkList.AddRange(wonderNoWalkLoc);

        if (mainPlayer.isMoving)
            tempNoWalkList.AddRange(wonderNoWalkLoc);

        if (wonderData.terrainType == TerrainType.Coast)
            AdjustSeaTraderPaths(wonderPlacementLoc);

        wonderPlacementLoc.Clear();
        wonderNoWalkLoc.Clear();
        cityBuilderManager.PlaySelectAudio(cityBuilderManager.buildClip);
    }

	public bool CheckForUnits(Vector3Int centerLoc)
	{
		float increase = 0.1f;

		Vector3 rayCastLoc = centerLoc;
		rayCastLoc.y += increase;
		RaycastHit hit;

		List<Vector3Int> directions = GetNeighborsCoordinates(State.EIGHTWAY);
		directions.Add(Vector3Int.down);
        for (int i = 0; i < directions.Count; i++)
        {
			Vector3 pos = directions[i];
		    float distance = i % 2 == 0 ? 1.5f : 2.1f;

            if (i == 8)
                rayCastLoc.y += 1.3f;

            //Debug.DrawRay(rayCastLoc, pos * distance, Color.yellow, 10);
			if (Physics.Raycast(rayCastLoc, pos, out hit, distance, unitMask))
			{
				GameObject hitGO = hit.collider.gameObject;
				if (hitGO && hitGO.TryGetComponent(out Unit unit))
                    return true;
			}
        }

        return false;
	}

	//for when building wonder in ocean and on trader's paths
	private void AdjustSeaTraderPaths(List<Vector3Int> wonderLoc)
    {
        List<Trader> tempTraderList = new(traderList);
        for (int i = 0; i < tempTraderList.Count; i++)
        {
            if (tempTraderList[i].bySea)
            {
                if (tempTraderList[i].followingRoute)
                {
                    Dictionary<int, List<Vector3Int>> routeDict = new(tempTraderList[i].tradeRouteManager.routePathsDict);
                    foreach (int stop in routeDict.Keys)
                    {
                        bool adjust = false;
                        for (int j = 0; j < wonderLoc.Count; j++)
                        {
                            if (routeDict[stop].Contains(wonderLoc[j]))
                            {
                                adjust = true;
                                break;
                            }
                        }

                        if (adjust)
                        {
                            Vector3Int start = tempTraderList[i].tradeRouteManager.cityStops[stop];
                            int nextStop = stop + 1;

                            if (tempTraderList[i].tradeRouteManager.cityStops.Count == nextStop)
								nextStop = 0;

							Vector3Int dest = tempTraderList[i].tradeRouteManager.cityStops[nextStop];

                            List<Vector3Int> path = GridSearch.TraderMove(this, start, dest, true);

                            if (path.Count > 0)
                            {
								tempTraderList[i].tradeRouteManager.routePathsDict[stop] = path;

                                if (tempTraderList[i].tradeRouteManager.currentStop == stop)
									CurrentMovementAdjust(i, wonderLoc);
							}
                            else
                            {
								//in case it checks more than once
								if (tempTraderList[i].followingRoute)
									tempTraderList[i].CancelRoute();
                            }
                        }
                    }
                }
                else if (tempTraderList[i].isMoving)
                {
                    CurrentMovementAdjust(i, wonderLoc);
                }
            }
        }
    }

    //if trader is currently on route to go through recently placed wonder
    private void CurrentMovementAdjust(int i, List<Vector3Int> wonderLoc)
    {
		bool adjust = false;

		for (int j = 0; j < wonderLoc.Count; j++)
		{
			if (traderList[i].pathPositions.Contains(wonderLoc[j]))
			{
				adjust = true;
				break;
			}
		}

		if (adjust)
		{
			Vector3Int dest = RoundToInt(traderList[i].finalDestinationLoc);
			List<Vector3Int> path = GridSearch.TraderMove(this, traderList[i].transform.position, dest, true);

			if (path.Count > 0)
			{
				traderList[i].StopMovementCheck(false);
				traderList[i].MoveThroughPath(path);
			}
			else
			{
                if (traderList[i].followingRoute)
                    traderList[i].CancelRoute();
                else
                    traderList[i].ReturnHome(0);
			}
		}
	}

	public void LoadWonder(List<WonderData> allWonderData)
	{
		foreach (WonderData data in allWonderData)
        {
            WonderDataSO wonderData = UpgradeableObjectHolder.Instance.wonderDict[data.name];

			//prep terrain
			foreach (Vector3Int tile in data.wonderLocs)
			{
				TerrainData td = GetTerrainDataAt(tile);

				if (wonderData.isSea)
				{
					td.sailable = false;
					td.walkable = true;
                    td.canWalk = true;
                    td.canPlayerWalk = true;
				}
				else
				{
					if (td.prop != null)
						td.ShowProp(false);

					td.ToggleTerrainMesh(false);
					if (td.hasResourceMap)
						td.HideResourceMap();
				}
			}

    		//setting up wonder info
    		GameObject wonderGO = Instantiate(Resources.Load<GameObject>("Prefabs/WonderPrefabs/" + wonderData.wonderPrefabName), data.centerPos, data.rotation);
    		wonderGO.gameObject.transform.SetParent(wonderHolder, false);
		    Wonder wonder = wonderGO.GetComponent<Wonder>();
            wonder.mapIcon.transform.localRotation = Quaternion.Inverse(data.rotation);
            Vector3 angles = wonder.mapIcon.transform.localEulerAngles;
            angles.x = 90;
            wonder.mapIcon.transform.localEulerAngles = angles;
            wonder.LoadData(data);
            allWonders.Add(wonder);
		    wonder.SetReferences(this, cityBuilderManager.focusCam);
		    wonder.wonderData = wonderData;
		    wonder.SetPrefabs(true);
    		wonder.SetResourceDict(wonderData.wonderCost, true);
            AddStop(data.unloadLoc, wonder);
		    wonder.SetCenterPos(data.centerPos);
			tradeStopNameDict[wonder.wonderName] = wonder;

			foreach (Vector3Int tile in data.wonderLocs)
                wonderTiles.Add(tile);

            if (data.isConstructing)
            {
    			//roadManager.BuildRoadAtPosition(data.unloadLoc, UtilityType.Road, 1);

                if (wonder.singleBuildDict.ContainsKey(SingleBuildType.Harbor))
                    cityBuilderManager.LoadWonderHarbor(wonder.singleBuildDict[SingleBuildType.Harbor], wonder);
            }
            else
            {
                wonder.MeshCheck();
                wonder.DestroyParticleSystems();
                wonder.ApplyWonderCompletionReward();
            }

			int xMin = wonder.wonderLocs[0].x - 1;
			int xMax = wonder.wonderLocs[0].x + 1;
			int zMin = wonder.wonderLocs[0].z - 1;
			int zMax = wonder.wonderLocs[0].z + 1;
			
            //claiming the area for the wonder
			foreach (Vector3Int tile in wonder.wonderLocs)
			{
				AddToCityLabor(tile, null); //so cities can't take the spot
				AddStructure(tile, wonderGO); //so nothing else can be built there

                if (tile.x - 1 < xMin)
                    xMin = tile.x - 1;
                if (tile.x + 1 > xMax)
                    xMax = tile.x + 1;
                if (tile.z - 1 < zMin)
                    zMin = tile.z - 1;
                if (tile.z + 1 > zMax)
                    zMax = tile.z + 1;
			}

            //setting no walk zone
            List<Vector3Int> tempNoWalkList = new();

            //int rotation = Mathf.RoundToInt(wonderGO.transform.eulerAngles.y);
            //bool sideways = rotation / 90 == 1 || rotation / 90 == 3;

			foreach (Vector3Int tile in wonder.wonderLocs)
			{
				foreach (Vector3Int neighbor in GetNeighborsFor(tile, State.EIGHTWAY))
				{
					if (neighbor.x == xMin || neighbor.x == xMax || neighbor.z == zMin || neighbor.z == zMax)
						continue;

					tempNoWalkList.Add(neighbor);
				}

				tempNoWalkList.Add(tile);
			}

            if (data.isConstructing)
                tempNoWalkList.Remove(data.unloadLoc);

			noWalkList.AddRange(tempNoWalkList);

            if (data.isBuilding)
                wonder.LoadWonderBuild();

			GameLoader.Instance.wonderWaitingDict[wonder] = (data.waitList, data.seaWaitList);
		}
	}

	public void PlaceWonder(WonderDataSO wonderData)
    {
        cityBuilderManager.PlayCloseAudio();
        buildingWonder = true;
        this.wonderData = wonderData;
        CloseWonders();

        rotationCount = 0;
        unloadLoc = wonderData.unloadLoc;

        uiBuildingSomething.SetText("Building " + wonderData.wonderDisplayName);
        uiBuildingSomething.ToggleVisibility(true);
        unitMovement.ToggleCancelButton(true);
    }

    public void RotateWonderPlacement()
    {
        cityBuilderManager.PlaySelectAudio();
        
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

    public void UnlockWonder(string wonderName)
    {
        for (int i = 0; i < wonderHandler.buildOptions.Count; i++)
        {
            if (wonderHandler.buildOptions[i].BuildData.wonderName == wonderName)
            {
                wonderHandler.buildOptions[i].locked = false;
                break;
            }
        }
    }

    public void SetNewWonder(string wonderName)
    {
        wonderButton.newIcon.SetActive(true);
        wonderHandler.somethingNew = true;

        newUnitsAndImprovements.Add(wonderName);

        for (int i = 0; i < wonderHandler.buildOptions.Count; i++)
        {
            if (wonderHandler.buildOptions[i].BuildData.wonderName == wonderName)
            {
                wonderHandler.buildOptions[i].ToggleSomethingNew(true);
                break;
            }
        }
    }

    public void CloseBuildingSomethingPanel()
    {
        if (unitOrders)
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
        uiConfirmWonderBuild.ToggleVisibility(false);
        buildingWonder = false;
        uiBuildingSomething.ToggleVisibility(false);
        uiRotateWonder.ToggleVisibility(false);
        somethingSelected = false;
    }

    public bool TradeStopNameExists(string stopName)
    {
        return tradeStopNameDict.ContainsKey(stopName);
    }

    public void OpenConversationList()
    {
		cityBuilderManager.PlaySelectAudio();

		if (buildingWonder || unitOrders)
			CloseBuildingSomethingPanel();

		if (uiConversationTaskManager.activeStatus)
		{
			uiConversationTaskManager.ToggleVisibility(false);
			conversationListButton.ToggleButtonColor(false);
            somethingSelected = false;
		}
		else
		{
			if (hideUI)
				return;

			uiConversationTaskManager.ToggleVisibility(true);
			conversationListButton.ToggleButtonColor(true);
		}
	}

    public void CloseConversationList()
    {
		if (uiConversationTaskManager.activeStatus)
		{
			uiConversationTaskManager.ToggleVisibility(false);
			conversationListButton.ToggleButtonColor(false);
		}
	}

    public void OpenWonders()
    {
        cityBuilderManager.PlaySelectAudio();

        if (buildingWonder || unitOrders)
            CloseBuildingSomethingPanel();

        if (wonderHandler.activeStatus)
        {
            wonderHandler.ToggleVisibility(false);
            wonderButton.ToggleButtonColor(false);
            somethingSelected = false;
        }
        else
        {
			if (hideUI)
				return;

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

    public void CloseWondersButton()
    {
		wonderHandler.ToggleVisibility(false);
		wonderButton.ToggleButtonColor(false);
		somethingSelected = false;
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

    public bool IsInNoWalkZone(Vector3Int loc)
    {
        return tempNoWalkList.Contains(loc);
    }

    //Profitability info
    public void OpenProfitabilityStats()
    {
		cityBuilderManager.PlaySelectAudio();

		if (unitOrders || buildingWonder)
			CloseBuildingSomethingPanel();

        if (uiProfitabilityStats.activeStatus)
        {
            uiProfitabilityStats.ToggleVisibility(false);
        }
        else
        {
			if (hideUI)
				return;

			uiProfitabilityStats.ToggleVisibility(true);
        }
	}

    //Research info
    public void OpenResearchTree()
    {
		cityBuilderManager.PlaySelectAudio();

		if (unitOrders || buildingWonder)
            CloseBuildingSomethingPanel();
        
        if (researchTree.activeStatus)
        {
            researchTree.ToggleVisibility(false);
        }
        else
        {
			if (hideUI)
				return;

			researchTree.ToggleVisibility(true);
        }
    }

    public void CloseResearchTree()
    {
        researchTree.ToggleVisibility(false);
        uiProfitabilityStats.ToggleVisibility(false);
    }

    public void CloseMap()
    {
        mapHandler.ToggleVisibility(false);
    }

    //terrain tooltip
    public void OpenTerrainTooltip(TerrainData td)
    {
		if (TooltipCheck())
			return;

        uiTerrainTooltip.ToggleVisibility(true, td);
    }

    public void CloseTerrainTooltipCloseButton()
    {
		cityBuilderManager.PlayCloseAudio();
		tooltip = false;
		uiTerrainTooltip.ToggleVisibility(false);
	}

	public void CloseTerrainTooltipButton()
    {
		tooltip = false;
        uiTerrainTooltip.ToggleVisibility(false);
    }

    public void CloseTerrainTooltip()
    {
        uiTerrainTooltip.ToggleVisibility(false);
    }

	public void CloseTransferTooltip()
    {
        uiLaborDestinationWindow.ToggleVisibility(false);
    }

    //city improvement tooltip
    public void OpenImprovementTooltip(CityImprovement improvement)
    {
        if (improvement.isConstruction)
        {
            CloseTooltip();
            return;
        }
        
        if (TooltipCheck())
            return;

        uiCityImprovementTip.ToggleVisibility(true, improvement);
    }

    public void CloseImprovementTooltipCloseButton()
    {
		cityBuilderManager.PlayCloseAudio();
		tooltip = false;
		uiCityImprovementTip.ToggleVisibility(false);
	}
    
    public void CloseImprovementTooltipButton()
    {
        tooltip = false;
        uiCityImprovementTip.ToggleVisibility(false);
    }

    public void CloseImprovementTooltip()
    {
        uiCityImprovementTip.ToggleVisibility(false);
    }

    public void OpenCampTooltip(CityImprovement improvement)
    {
        if (improvement.isConstruction)
        {
            CloseTooltip();
            return;
        }
        
        if (TooltipCheck())
			return;

		uiCampTooltip.ToggleVisibility(true, improvement);
    }

    public void CloseCampTooltipButton()
    {
		tooltip = false;
		uiCampTooltip.ToggleVisibility(false);
	}

    public void CloseCampTooltip()
    {
        uiCampTooltip.ToggleVisibility(false);
    }

    public void CloseTradeRouteBeginTooltipCloseButton()
    {
        cityBuilderManager.PlayCloseAudio();
        uiTradeRouteBeginTooltip.ToggleVisibility(false);
	}

    public void CloseTradeRouteBeginTooltipButton()
    {
        uiTradeRouteBeginTooltip.ToggleVisibility(false);
    }

	private bool TooltipCheck()
    {
		if (tooltip)
		{
			CloseTerrainTooltipButton();
			CloseImprovementTooltipButton();
			CloseCampTooltipButton();
			return true;
		}

        tooltip = true;
        infoPopUpCanvas.gameObject.SetActive(true);
        return false;
	}

    public void CloseTooltip()
    {
		somethingSelected = false;

		if (tooltip)
		{
			CloseTerrainTooltipButton();
			CloseImprovementTooltipButton();
			CloseCampTooltipButton();
		}
	}

    public void SetResearchName(string name)
    {
        uiWorldResources.SetResearchName(name);
    }

    public void AddToResearchWaitList(ResourceProducer producer)
    {
        researchWaitList.Add(producer);
    }

    public void RemoveFromResearchWaitList(ResourceProducer producer)
    {
        researchWaitList.Remove(producer);
    }

    public void RestartResearch()
    {
        HashSet<ResourceProducer> producerResearchWaitList = new(researchWaitList);

        foreach (ResourceProducer producer in producerResearchWaitList)
        {
			if (researching)
			{
                researchWaitList.Remove(producer);
				producer.CheckProducerResearchWaitList();
			}
		}
    }

    public bool CitiesResearchWaitingCheck()
    {
        return researchWaitList.Count > 0;
    }

    public void AddToGoldWaitList(IGoldWaiter goldWaiter)
    {
        goldWaitList.Add(goldWaiter);
    }

    public void RemoveFromGoldWaitList(IGoldWaiter goldWaiter)
    {
        int index = goldWaitList.IndexOf(goldWaiter);

        if (index >= 0)
        {
            goldWaitList.RemoveAt(index);

			//if first in line, checking to see if next one up can go
			if (index == 0)
				GoldWaitListCheck();
		}
    }

    public void RemoveCityFromGoldWaitList(IGoldWaiter goldWaiter, int place)
    {
        int j = 0;
        for (int i = 0; i < goldWaitList.Count; i++)
        {
            if (goldWaitList[i] == goldWaiter)
            {
                if (j == place)
                {
                    goldWaitList.RemoveAt(i);

					//if first in line, checking to see if next one up can go
					if (i == 0)
                        GoldWaitListCheck();

                    break;
                }
                else
                {
                    j++;
                }
            }
        }
    }

    //lights in the world
    public void ToggleWorldLights(bool v)
    {
        foreach (TradeCenter center in allTradeCenters)
        {
            center.ToggleLights(v);
        }

        foreach (Wonder wonder in allWonders)
        {
            if (wonder.PercentDone != 100)
                continue;

            foreach (Light light in wonder.wonderLights)
            {
                light.gameObject.SetActive(v);
            }    
        }
    }

    //world resources management
    public void UpdateWorldGold(int amount)
    {
        if (amount == 0)
            return;

		int prevAmount = worldResourceManager.resourceDict[ResourceType.Gold];
		worldResourceManager.SetResource(ResourceType.Gold, amount);
		bool pos = amount > 0;

		if (pos && goldWaitList.Count > 0)
            GoldWaitListCheck();

        int currentAmount = worldResourceManager.resourceDict[ResourceType.Gold];
        if (goldUpdateCheck != null && prevAmount != currentAmount)
            goldUpdateCheck.UpdateGold(prevAmount, currentAmount, pos);
	}

    public void GoldWaitListCheck()
    {
        bool success = true;
        while (success)
        {
            if (goldWaitList.Count > 0 && goldWaitList[0].RestartGold(worldResourceManager.resourceDict[ResourceType.Gold]))
                goldWaitList.RemoveAt(0);
            else
                success = false;
        }
	}

    public void UpdateWorldResearch(int amount)
    {
        amount = researchTree.AddResearch(amount);
        researchTree.CompletedResearchCheck();
        worldResourceManager.SetResource(ResourceType.Research, amount);
        researchTree.CompletionNextStep();
    }

    public bool CheckWorldGold(int amount)
    {
        return worldResourceManager.resourceDict[ResourceType.Gold] >= amount;
    }

    public int GetWorldGoldLevel()
    {
        return worldResourceManager.resourceDict[ResourceType.Gold];
	}

    public List<ResourceType> WorldResourcePrep()
    {
        return worldResourceManager.PassWorldResources();
    }

    public void SetWorldResearchUI(int researchReceived, int totalResearch)
    {
        worldResourceManager.resourceDict[ResourceType.Research] = researchReceived;
        uiWorldResources.ResearchLimit = totalResearch;
        uiWorldResources.SetResearchValue(researchReceived);
    }

    public void SetResearchBackground(bool complete)
    {
        uiWorldResources.SetResearchBackground(complete);
    }

    public bool ResourceCheck(ResourceType type)
    {
        return resourceDiscoveredList.Contains(type);
    }

    public void SetSellableResourceList()
    {
        foreach (ResourceType type in resourceDiscoveredList)
        {
            if (ResourceHolder.Instance.GetSell(type))
                sellableResourceList.Add(type);
        }
    }

  //  public void LoadDiscoveredResources()
  //  {
  //      foreach (ResourceType type in resourceDiscoveredList)
  //      {
		//	if (type == ResourceType.Gold || type == ResourceType.Research || resourceDiscoveredList.Contains(type))
		//		return;

		//	UpdateResourceSelectionGrids(type);
		//	cityBuilderManager.uiMarketPlaceManager.UpdateMarketPlaceManager(type);
		//}
  //  }

    public void DiscoverResource(ResourceType type)
    {
        AddToDiscoverList(type);
        UpdateResourceSelectionGrids(type);
        
        if (ResourceHolder.Instance.GetSell(type))
            resourcePurchaseAmountDict[type] = ResourceHolder.Instance.GetPurchaseAmount(type);

        ResourceType obsolete = ResourceHolder.Instance.GetObsoleteResource(type);
        if (resourcePurchaseAmountDict.ContainsKey(obsolete))
        {
            resourcePurchaseAmountDict[obsolete] *= 1-obsoleteResourceReduction;

            for (int i = 0; i < allTradeCenters.Count; i++)
            {
                if (allTradeCenters[i].resourceSellDict.ContainsKey(obsolete))
                {
                    int price = allTradeCenters[i].resourceSellDict[obsolete];
					allTradeCenters[i].resourceSellDict[obsolete] = Mathf.Max(1, price - maxPriceDiff);
                }
            }

            if (cityBuilderManager.uiTradeCenter.activeStatus)
                cityBuilderManager.uiTradeCenter.UpdateResourcePrice(obsolete);
        }

		cityBuilderManager.uiMarketPlaceManager.UpdateMarketPlaceManager(type);
        cityBuilderManager.uiLaborHandler.CreateLaborHandlerOption(type);
        cityBuilderManager.uiLaborHandler.CreateLaborCostResource(type);

		foreach (City city in cityDict.Values)
			city.resourceManager.UpdateDicts(type);
	}

    //when running out of rocks
	public void SetNewTerrainData(TerrainData td)
	{
		TerrainDataSO tempData;

		if (td.isHill)
			tempData = td.terrainData.grassland ? grasslandHillTerrain : desertHillTerrain;
		else
			tempData = td.terrainData.grassland ? grasslandTerrain : desertTerrain;

		td.SetNewData(tempData);
		GameLoader.Instance.gameData.allTerrain[td.TileCoordinates] = td.SaveData();
	}

	public Transport GetKoasTransport()
    {
        for (int i = 0; i < transportList.Count; i++)
		{
            if (transportList[i].hasKoa)
                return transportList[i];
		}

        return null;
	}

    //if battle is next to ambush, cancel ambush
    public bool BattleNearbyCheck(Vector3Int loc)
    {
		List<Vector3Int> eightWay = GetNeighborsCoordinates(State.EIGHTWAYINCREMENT);
		eightWay.Insert(0, loc);

		for (int i = 0; i < eightWay.Count; i++)
		{
			if (tempBattleZone.Contains(eightWay[i] + loc))
				return true;
		}

        return false;
	}

    public void IncreaseEnemyUnitCount()
    {
        for (int i = 0; i < allEnemyLeaders.Count; i++)
        {
            if (allEnemyLeaders[i].researchLevelIncreaseCount > maxResearchLevel)
                continue;
            
            if (allEnemyLeaders[i].IncreaseUnitCount())
            {
                for (int j = 0; j < allEnemyLeaders[i].empire.empireCities.Count; j++)
                {
                    City enemyCity = enemyCityDict[allEnemyLeaders[i].empire.empireCities[j]];

                    if (enemyCity.enemyCamp.growing)
                        continue;

                    if (enemyCity.cityLoc == enemyCity.empire.attackingCity)
                    {
					    if (enemyCity.PauseForGrowthCheck())
                        {
                            enemyCity.enemyCamp.growing = true;
                        }
                        else
                        {
                            enemyCity.empire.paused = true;
                            enemyCity.empire.pauseTimer = enemyCity.countDownTimer;
                            enemyCity.CancelSendAttackWait();

							if (!GetTerrainDataAt(enemyCity.cityLoc).isDiscovered)
								enemyCity.ActivateButHideCity();
							enemyCity.StartSpawnCycle(true);
                        }
                    }
                    else
                    {
						if (!GetTerrainDataAt(enemyCity.cityLoc).isDiscovered)
							enemyCity.ActivateButHideCity();
						enemyCity.StartSpawnCycle(true);
                    }
				}
            }
        }
    }

    public void EnemyAttackCheck()
    {
        if (enemyAttackBegin)
        {
            foreach (City city in enemyCityDict.Values)
            {
                if (city.empire.attackingCity == new Vector3Int(0, -10, 0))
                {
                    StartAttacks();
                    break;
                }
                    
                if (city.empire.attackingCity == city.cityLoc)
                {
                    if (!GetTerrainDataAt(city.cityLoc).isDiscovered)
                        city.ActivateButHideCity();
                    city.StartSendAttackWait();
                }
            }
        }
    }

    //begin attacks
    public void StartAttacks()
    {
        enemyAttackBegin = true;
        if (enemyCityDict.Count > 0 && cityDict.Count > 0)
        {
            List<City> enemyCityList = enemyCityDict.Values.ToList();
            List<City> cityList = cityDict.Values.ToList();
            int dist = 0;
            City enemyCity = enemyCityList[0];

            for (int i = 0; i < enemyCityList.Count; i++)
            {
                for (int j = 0; j < cityList.Count; j++)
                {
                    if (j == 0 && i == 0)
                    {
                        enemyCity = enemyCityList[i];
                        dist = Math.Abs(cityList[j].cityLoc.x - enemyCityList[i].cityLoc.x) + Math.Abs(cityList[j].cityLoc.z - enemyCityList[i].cityLoc.z);
                        continue;
                    }

                    int newDist = Math.Abs(cityList[j].cityLoc.x - enemyCityList[i].cityLoc.x) + Math.Abs(cityList[j].cityLoc.z - enemyCityList[i].cityLoc.z);
                    if (newDist < dist)
                    {
                        enemyCity = enemyCityList[i];
                        dist = newDist;
                    }
                }
            }

            if (dist < maxDistance)
            {
                enemyCity.empire.attackingCity = enemyCity.cityLoc;
				if (!GetTerrainDataAt(enemyCity.cityLoc).isDiscovered)
					enemyCity.ActivateButHideCity();
				enemyCity.StartSendAttackWait();
            }
        }
    }

	//ambush logic
	public void SetUpAmbush(Vector3Int loc, Unit unitTrader)
    {
        ambushes++;
        List<Vector3Int> randomLocs = GetNeighborsFor(loc, State.EIGHTWAY);
        TerrainData td = GetTerrainDataAt(loc);

        if (unitTrader.trader.guarded)
        {
            if (!unitTrader.bySea && !unitTrader.byAir)
            {
                unitTrader.trader.guardUnit.transform.position = unitTrader.transform.position + Vector3.left;
                unitTrader.trader.guardUnit.gameObject.SetActive(true);
                unitTrader.trader.guardMeshList[unitTrader.trader.guardUnit.buildDataSO.unitLevel - 1].SetActive(false);

                //if (td.CompareTag("Forest") || td.CompareTag("Forest Hill"))
                //unitTrader.trader.guardUnit.outline.ToggleOutline(true);
					//unitTrader.trader.guardUnit.marker.ToggleVisibility(true);
			}

            randomLocs.Remove(RoundToInt(unitTrader.trader.guardUnit.transform.position));
		}

		Vector3Int ambushLoc = randomLocs[UnityEngine.Random.Range(0,randomLocs.Count)];
        UnitBuildDataSO ambushingUnit = UpgradeableObjectHolder.Instance.enemyUnitDict[ambushUnitDict[currentEra]];
        td.LimitPlayerMovement();
        //if (td.treeHandler != null)
        //    td.ToggleTransparentForest(true);

		EnemyAmbush ambush = new();
		ambush.loc = loc;
        ambush.attackedTrader = unitTrader.name;

        //check for main player
        CheckMainPlayerLoc(loc, loc);

		Vector3 direction = loc - ambushLoc;
		Quaternion endRotation;
		if (direction == Vector3.zero)
			endRotation = Quaternion.identity;
		else
			endRotation = Quaternion.LookRotation(direction, Vector3.up);
        //ambushLoc = new Vector3Int(35, 0, 38);
		GameObject enemyGO = Instantiate(Resources.Load<GameObject>("Prefabs/" + ambushingUnit.prefabLoc), ambushLoc, endRotation);
        enemyGO.name = ambushingUnit.unitDisplayName;
		enemyGO.transform.SetParent(enemyUnitHolder, false);

		Unit unit = enemyGO.GetComponent<Unit>();
        unit.SetMinimapIcon(enemyUnitHolder);
        //if (td.CompareTag("Forest") || td.CompareTag("Forest Hill"))
        if (!unit.bySea && !unit.byAir)
            unit.outline.ToggleOutline(true);
			//unit.marker.ToggleVisibility(true);

		Vector3 unitScale = unit.transform.localScale;
		unit.currentLocation = AddUnitPosition(ambushLoc, unit);
		float scaleX = unitScale.x;
		float scaleZ = unitScale.z;
		unit.transform.localScale = new Vector3(scaleX, 0.1f, scaleZ);

        unit.PlayLightBeam();
        //Vector3 lightBeamLoc = ambushLoc;
        //lightBeamLoc.y += .01f;
        //if (IsRoadOnTileLocation(ambushLoc))
        //    lightBeamLoc.y += .1f;

        //unit.lightBeam.transform.position = lightBeamLoc;
        //unit.lightBeam.Play();
		LeanTween.scale(enemyGO, unitScale, 0.5f).setEase(LeanTweenType.easeOutBack);

		unit.ambush = true;
		unit.SetReferences(this);
        unit.military.enemyAmbush = ambush;
		ambush.attackingUnits.Add(unit.military);
        enemyAmbushDict[loc] = ambush;
		uiAttackWarning.AttackNotification(ambush.loc);
        
        ambush.attackedUnits.Add(unitTrader);

        if (unitTrader.trader.guarded)
        {
			unitTrader.PlayAudioClip(cityBuilderManager.warningClip); //not attacked so can play sound
			unitTrader.trader.guardUnit.originalMoveSpeed = unitTrader.trader.guardUnit.buildDataSO.movementSpeed;
            unitTrader.trader.guardUnit.ambush = true;
			unitTrader.trader.guardUnit.StopMovementCheck(true);
            unitTrader.trader.guardUnit.military.isGuarding = false;
            ambush.attackedUnits.Add(unitTrader.trader.guardUnit);
			Vector3 pos = unitTrader.trader.guardUnit.transform.position;

            if (Mathf.Abs(pos.x - ambushLoc.x) < 1.3f && Mathf.Abs(pos.z - ambushLoc.z) < 1.3f)
            {
                unit.enemyAI.StartAttack(unitTrader.trader.guardUnit);

				if (unitTrader.trader.guardUnit.buildDataSO.unitType == UnitType.Ranged)
					unitTrader.trader.guardUnit.military.RangedAmbushCheck(unit);
				else
					unitTrader.trader.guardUnit.military.StartAttack(unit);
            }
            else
            {
                if (unitTrader.trader.guardUnit.buildDataSO.unitType == UnitType.Ranged)
                {
                    Vector3Int endPosition = RoundToInt(unitTrader.trader.guardUnit.transform.position);
                    unitTrader.trader.guardUnit.military.RangedAmbushCheck(unit);
                    unit.enemyAI.AmbushAggro(endPosition, loc);
				}
                else
                {
                    unit.military.targetSearching = true;
                    Vector3Int endPosition = RoundToInt(unit.transform.position);
                    unitTrader.trader.guardUnit.military.AmbushAggro(endPosition, loc);
                }
            }
        }
        else
        {
			unit.PlayAudioClip(cityBuilderManager.warningClip); //not attacked so can play sound
			unit.enemyAI.StartAttack(unitTrader);
        }
	}

    public void CheckMainPlayerLoc(Vector3Int loc, Vector3Int targetLoc, List<Vector3Int> route = null)
    {
        if (mainPlayer.inEnemyLines || uiSpeechWindow.activeStatus)
            return;

        Vector3Int playerLoc;
        Transport transport = null;
		if (mainPlayer.inTransport)
        {
            transport = GetKoasTransport();
            playerLoc = GetClosestTerrainLoc(transport.transform.position);
        }
        else
        {
			playerLoc = GetClosestTerrainLoc(mainPlayer.transform.position);
        }
		
        int xDiff = Mathf.Abs(playerLoc.x - loc.x);
        int zDiff = Mathf.Abs(playerLoc.z - loc.z);

		if (xDiff < 4 && zDiff < 4)
		{
            if (mainPlayer.isBusy)
				unitMovement.workerTaskManager.ForceCancelWorkerTask();
			
            mainPlayer.runningAway = true;

            HashSet<Vector3Int> tempRoute;

            if (route != null)
                tempRoute = new(route) { loc, targetLoc };
            else
                tempRoute = new() { loc, targetLoc };

			if (mainPlayer.inTransport)
            {
                transport.StopMovementCheck(true);
                transport.exclamationPoint.SetActive(true);

				if (tempRoute.Contains(playerLoc))
				{
					transport.StepAside(playerLoc, tempRoute);
				}
				else //look to watch
				{
					transport.Rotate(loc);
				}
			}
            else
            {
			    mainPlayer.StopPlayer();
			    mainPlayer.exclamationPoint.SetActive(true);

			    if (tempRoute.Contains(playerLoc))
			    {
				    mainPlayer.StepAside(playerLoc, tempRoute);
			    }
			    else
			    {
				    mainPlayer.Rotate(loc);
                    Vector3Int scottLoc = GetClosestTerrainLoc(scott.transform.position);
					Vector3Int azaiLoc = GetClosestTerrainLoc(azai.transform.position);

                    if (tempRoute.Contains(scottLoc) || tempRoute.Contains(azaiLoc))
                    {
                        if (RoundToInt(mainPlayer.transform.position) != playerLoc)
                        {
                            List<Vector3Int> path = new() { playerLoc };
                            mainPlayer.finalDestinationLoc = playerLoc;
                            mainPlayer.MoveThroughPath(path);
                        }
                        
                        mainPlayer.RealignFollowers(playerLoc, mainPlayer.prevTile, false);
                    }
                    else
                    {
                        foreach (Unit unit in characterUnits)
                            unit.FinishMoving(unit.transform.position);
                        
                        if (scottFollow)
                            scott.Rotate(loc);
				        if (azaiFollow)
                            azai.Rotate(loc);
                    }
			    }
            }
		}
        else if (mainPlayer.runningAway && (xDiff > 8 || zDiff > 8))
        {
            mainPlayer.StopRunningAway();
		}
	}

    public void ClearAmbush(Vector3Int loc)
    {
        world[loc].CanMoveCheck();
        enemyAmbushDict.Remove(loc);
	}

    //updating builder handlers if one is selected
    public void BuilderHandlerCheck()
    {
        if (cityBuilderManager.activeBuilderHandler)
            cityBuilderManager.activeBuilderHandler.PrepareBuildOptions(cityBuilderManager.SelectedCity.resourceManager);
    }

    public Unit GetPlayer(Vector3Int tile)
    {
        return playerPosDict[tile];
    }

    public List<Trader> GetTrader(Vector3Int tile)
    {
        return traderPosDict[tile];
    }

    public Unit GetUnit(Vector3Int tile)
    {
        return unitPosDict[tile];
    }
    
    public Trader CycleThroughTraderCheck(Vector3Int tile, Trader trader)
    {
        if (traderPosDict.ContainsKey(tile))
        {
            int index = traderPosDict[tile].IndexOf(trader);

            if (index >= 0)
            {
                int count = traderPosDict[tile].Count();

                if (count > 1)
                {
                    index++;
                    if (index >= count)
                        index = 0;

                    return traderPosDict[tile][index]; 
                }
            }
        }

        return null;
    }

    public bool IsTraderWaitingForSameStop(Vector3Int tile, Vector3Int finalDest, Trader trader)
    {
        if (traderPosDict.ContainsKey(tile) && tradeStopDict.ContainsKey(finalDest))
        {
		    return tradeStopDict[finalDest].IsTraderWaitingAtSameStop(tile, finalDest, this, trader);
        }

        return false;
    }

    public City GetCity(Vector3Int tile)
    {
        return cityDict[tile];
    }

    public Wonder GetWonder(Vector3Int tile)
    {
        return tradeStopDict[tile].wonder;
    }

    public TradeCenter GetTradeCenter(Vector3Int tile)
    {
        return tradeStopDict[tile].center;
    }

    public City GetEnemyCity(Vector3Int tile)
    {
        return enemyCityDict[tile];
    }

    public void RemoveWonderTiles(Vector3Int tile)
    {
        wonderTiles.Remove(tile);
    }

    public List<string> GetConnectedCityNames(Vector3Int unitLoc, bool bySea, bool citiesOnly, SingleBuildType type)
    {
        List<string> names = new();
        List<ITradeStop> wonderStops = new();
        List<ITradeStop> centerStops = new();

        //getting city names first
        foreach (string name in tradeStopNameDict.Keys)
        {
            ITradeStop stop = tradeStopNameDict[name];

            if (!stop.singleBuildLocDict.ContainsKey(type))
                continue;

            if (stop.wonder)
                wonderStops.Add(stop);
            else if (stop.center)
                centerStops.Add(stop);
            else if (GridSearch.TraderMovementCheck(this, unitLoc, stop.singleBuildLocDict[type], bySea))
                names.Add(name);
        }

        if (!citiesOnly)
        {
            for (int i = 0; i < wonderStops.Count; i++)
            {
			    if (GridSearch.TraderMovementCheck(this, unitLoc, wonderStops[i].singleBuildLocDict[type], bySea))
                    names.Add(wonderStops[i].stopName);
		    }

		    for (int i = 0; i < centerStops.Count; i++)
		    {
			    if (GridSearch.TraderMovementCheck(this, unitLoc, centerStops[i].singleBuildLocDict[type], bySea))
				    names.Add(centerStops[i].stopName);
		    }
        }

        return names;
    }

    public (List<string>, List<int>, List<bool>) GetConnectedCityNamesAndDistances(City city, bool citiesOnly, bool bySea, bool byAir, SingleBuildType buildType = SingleBuildType.None)
    {
        List<string> names = new();
        List<int> lengths = new();
        List<bool> atSeas = new();
        List<ITradeStop> wonderStops = new();

		//getting city names first
		foreach (string name in tradeStopNameDict.Keys)
		{
            if (name == city.cityName)
                continue;

            ITradeStop stop = tradeStopNameDict[name];

            if (stop.center)
                continue;

            if (citiesOnly)
            {
                if (!stop.singleBuildLocDict.ContainsKey(buildType))
                    continue;
            }
            else if (stop.wonder)
            {
                wonderStops.Add(stop);
                continue;
            }

            int length = 0;
            bool atSea = false;

            //getting land distance first
            if (!bySea && !byAir)
                length = GridSearch.TraderMovementCheckLength(this, city.cityLoc, stop.mainLoc, false);

			if (city.singleBuildDict.ContainsKey(SingleBuildType.Harbor) && stop.singleBuildLocDict.ContainsKey(SingleBuildType.Harbor))
			{
                int harborLength = GridSearch.TraderMovementCheckLength(this, city.singleBuildDict[SingleBuildType.Harbor], stop.singleBuildLocDict[SingleBuildType.Harbor], true);
                if (length == 0 || (harborLength != 0 && harborLength < length))
                {
                    if ((!bySea && !byAir) || bySea)
                    {
                        length = harborLength;
                        atSea = true;
                    }
                }
			}

			if (city.singleBuildDict.ContainsKey(SingleBuildType.Airport) && stop.singleBuildLocDict.ContainsKey(SingleBuildType.Airport))
			{
				int airportLength = GridSearch.TraderMovementCheckLength(this, city.singleBuildDict[SingleBuildType.Harbor], stop.singleBuildLocDict[SingleBuildType.Harbor], true);
				if (length == 0 || (airportLength != 0 && airportLength < length))
				{
					if ((!bySea && !byAir) || byAir)
					{
						length = airportLength;
						atSea = true;
					}
				}
			}

			if (length > 0)
            {
                names.Add(name);
                lengths.Add(length);
                atSeas.Add(atSea);
            }
		}

        //getting wonder names second (can only walk to wonders)
        for (int i = 0; i < wonderStops.Count; i++)
        {
			int length = GridSearch.TraderMovementCheckLength(this, city.cityLoc, wonderStops[i].mainLoc, false);

			if (length > 0)
			{
                names.Add(wonderStops[i].stopName);
				lengths.Add(length);
				atSeas.Add(false);
			}
		}

        return (names, lengths, atSeas);
	}

    public ITradeStop GetTradeStopByName(string stopName)
    {
        return tradeStopNameDict[stopName];
    }

    public ITradeStop GetStop(Vector3Int loc)
    {
        return tradeStopDict[loc];
    }

    public bool TraderStallCheck(Vector3Int loc)
    {
        return traderStallDict[loc].isFull;
    }

    public Vector3Int GetTraderStallLoc(Vector3Int loc, Vector3Int currentLoc)
    {
        return traderStallDict[loc].GetAvailableStall(currentLoc);
    }

    public void SetTraderStallLoc(Vector3Int loc, Vector3Int stallLoc)
    {
        traderStallDict[loc].TakeStall(stallLoc);
    }

    public List<Vector3Int> GetAllUsedStallLocs(Vector3Int loc)
    {
        return traderStallDict[loc].GetUsedStalls().ToList();
    }

    public void RemoveTraderFromStall(Vector3Int loc, Vector3Int currentLoc)
    {
        traderStallDict[loc].OpenStall(currentLoc);
    }

    public Vector3Int GetStopMainLocation(string name)
    {
        return tradeStopNameDict[name].mainLoc;
    }

    public Vector3Int GetStopLocation(string name, SingleBuildType type)
    {
        return tradeStopNameDict[name].singleBuildLocDict[type];
    }

    public City GetSingleBuildStopCity(Vector3Int loc)
    {
        return cityImprovementDict[loc].city;
    }

	public bool IsSingleBuildStopOnTile(Vector3Int loc, SingleBuildType type)
	{
		return cityImprovementDict.ContainsKey(loc) && cityImprovementDict[loc].GetImprovementData.singleBuildType == type && cityImprovementDict[loc].city != null;
	}

    public bool StopExistsCheck(Vector3Int loc)
    {
        return tradeStopDict.ContainsKey(loc);
    }

    public string GetStopName(Vector3Int location)
    {
        if (tradeStopDict.ContainsKey(location))
            return tradeStopDict[location].stopName;
        else
            return "";
    }

    //public void SetQueueGhost(Vector3Int loc, GameObject gameObject)
    //{
    //    queueGhostsDict[loc] = gameObject;
    //}

    //public bool ShowQueueGhost(Vector3Int loc)
    //{
    //    if (queueGhostsDict.ContainsKey(loc))
    //    {
    //        GameObject ghost = queueGhostsDict[loc];
    //        ghost.SetActive(true);
    //        //for tweening
    //        ghost.transform.localScale = Vector3.zero;
    //        LeanTween.scale(ghost, new Vector3(1.5f, 1.5f, 1.5f), 0.25f).setEase(LeanTweenType.easeOutBack);
    //        return true;
    //    }

    //    return false;
    //}

    //public bool IsLocationQueued(Vector3Int loc)
    //{
    //    return queueGhostsDict.ContainsKey(loc);
    //}

    //public GameObject GetQueueGhost(Vector3Int loc)
    //{
    //    return queueGhostsDict[loc];
    //}

    //public void RemoveQueueGhost(Vector3Int loc)
    //{
    //    queueGhostsDict.Remove(loc);
    //}

    public GameObject GetStructure(Vector3Int tile)
    {
        return buildingPosDict[tile];
    }

    public CityImprovement GetBuildingData(Vector3Int cityTile, string buildingName)
    {
        return cityBuildingDict[cityTile][buildingName];
    }

    public ResourceProducer GetResourceProducer(Vector3Int pos)
    {
        return cityImprovementDict[pos].resourceProducer;
    }

    public bool TileHasCityImprovement(Vector3Int tile)
    {
        return cityImprovementDict.ContainsKey(tile);
    }

    public CityImprovement GetCityDevelopment(Vector3Int tile)
    {
        return cityImprovementDict[tile];
    }

    public Vector3Int GetAdjacentMoveToTile(Vector3Int currentLoc, Vector3Int locationInt, bool enemy)
    {
		Vector3Int trySpot = locationInt;
		Vector3Int playerTile = currentLoc;
		bool firstOne = true;
		int dist = 0;
		foreach (Vector3Int tile in GetNeighborsFor(locationInt, MapWorld.State.FOURWAY))
		{
			TerrainData td = GetTerrainDataAt(tile);
			if (td.isLand && td.canWalk)
			{
                if (!enemy && td.enemyZone)
                    continue;
                
                if (firstOne)
				{
					firstOne = false;
					dist = Mathf.Abs(tile.x - playerTile.x) + Mathf.Abs(tile.z - playerTile.z);
					trySpot = tile;
					continue;
				}

				int newDist = Mathf.Abs(tile.x - playerTile.x) + Mathf.Abs(tile.z - playerTile.z);
				if (newDist < dist)
				{
					dist = newDist;
					trySpot = tile;
				}
			}
		}

        return trySpot;
	}

    public void SetWorkerWorkLocation(Vector3Int loc)
    {
        workerBusyLocation = loc;
    }

    public void RemoveWorkerWorkLocation()
    {
        workerBusyLocation = new Vector3Int(0, -10, 0);
    }

    public bool IsWorkerWorkingAtTile(Vector3Int loc)
    {
        return workerBusyLocation == loc;
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

    public void SetSelectionCircleLocation(Vector3Int loc)
    {
        Vector3 pos = GetClosestTerrainLoc(loc);
        pos.y += 0.07f;

		selectionIcon = Instantiate(Resources.Load<GameObject>("Prefabs/InGameSpritePrefabs/MilitaryGrid"));
		selectionIcon.transform.SetParent(transform, false);
		//selectionIcon.SetActive(false);

		selectionIcon.transform.position = pos;
        //selectionIcon.SetActive(true);
    }

    public void HideSelectionCircles()
    {
        Destroy(selectionIcon);
    }

    public void AddToResourceSelectionGridList(UIResourceSelectionGrid selectionGrid)
    {
        resourceSelectionGridList.Add(selectionGrid);

        foreach (ResourceType type in resourceDiscoveredList)
            selectionGrid.DiscoverResource(type);
    }

    public void AddToDiscoverList(ResourceType type)
    {
        if (resourceDiscoveredList.Contains(type))
            return;

        resourceDiscoveredList.Add(type);

        if (ResourceHolder.Instance.GetSell(type))
            sellableResourceList.Add(type);

        GameLoader.Instance.gameData.resourceDiscoveredList.Add(type);
    }

    public void UpdateResourceSelectionGrids(ResourceType type)
    {
        if (type == ResourceType.Gold || type == ResourceType.Research || type == ResourceType.Fish)
            return;

        for (int i = 0; i < resourceSelectionGridList.Count; i++)
            resourceSelectionGridList[i].DiscoverResource(type);
    }

	public EnemyCamp GetEnemyCamp(Vector3Int loc)
    {
        if (enemyCampDict.ContainsKey(loc))
            return enemyCampDict[loc];
        else
            return enemyCityDict[loc].enemyCamp;
    }

    public bool IsEnemyCampHere(Vector3Int loc)
    {
        return enemyCampDict.ContainsKey(loc);
    }

    public EnemyAmbush GetEnemyAmbush(Vector3Int loc)
    {
        return enemyAmbushDict[loc];
    }

    public bool IsEnemyAmbushHere(Vector3Int loc)
    {
        return enemyAmbushDict.ContainsKey(loc);
    }

    public void AddBattleZones(Vector3Int loc1, Vector3Int loc2, bool inBattle)
    {
        Vector3Int[] zones = new Vector3Int[] { loc1, loc2 };

        for (int i = 0; i < zones.Length; i++)
        {
            TerrainData td = GetTerrainDataAt(zones[i]);

            td.hasBattle = true;

            if (!IsCityOnTile(zones[i])) //can still walk on city tiles
                td.LimitPlayerMovement();

            if (inBattle)
            {
                td.inBattle = true;
            }
            else
            {
                if (!td.isGlowing)
                    td.BattleHighlight();
            }

            tempBattleZone.Add(zones[i]);

            if (!uiSpeechWindow.activeStatus)
            {
                foreach (Vector3Int tile in GetNeighborsFor(zones[i], State.EIGHTWAY))
                {
                    if (IsPlayerLocationTaken(tile))
                    {
		                if (mainPlayer.isBusy)
			                unitMovement.workerTaskManager.ForceCancelWorkerTask();

					    mainPlayer.StepAside(mainPlayer.currentLocation, null);
				    }
                }

                uiTerrainTooltip.UpdateText(td);
            }
        }
	}

	public void RemoveBattleZones(Vector3Int loc1, Vector3Int loc2)
	{
        Vector3Int[] zones = new Vector3Int[] { loc1, loc2 };

        for (int i = 0; i < zones.Length; i++)
        {
            TerrainData td = GetTerrainDataAt(zones[i]);
			td.hasBattle = false;
            td.inBattle = false;
            td.CanMoveCheck();
            tempBattleZone.Remove(zones[i]);
            GetTerrainDataAt(zones[i]).DisableBattleHighlight();
			uiTerrainTooltip.UpdateText(td);
		}
	}

    public void DisableBattleHighlight(Vector3Int loc1, Vector3Int loc2)
    {
		Vector3Int[] zones = new Vector3Int[] { loc1, loc2 };

		for (int i = 0; i < zones.Length; i++)
        {
            TerrainData td = GetTerrainDataAt(zones[i]);
            td.inBattle = true;
			td.DisableBattleHighlight();
			uiTerrainTooltip.UpdateText(td);
		}
	}

	public void CityBattleStations(Vector3Int cityLoc, Vector3Int attackLoc, Vector3Int targetZone, EnemyCamp camp)
    {
        if (!cityDict[cityLoc].singleBuildDict.ContainsKey(SingleBuildType.Barracks))
            return;
        
        if (GetCityDevelopment(cityDict[cityLoc].singleBuildDict[SingleBuildType.Barracks]).isTraining)
            cityBuilderManager.RemoveImprovement(cityDict[cityLoc].singleBuildDict[SingleBuildType.Barracks], 
                GetCityDevelopment(cityDict[cityLoc].singleBuildDict[SingleBuildType.Barracks]), true, cityDict[cityLoc]);

		cityDict[cityLoc].army.targetCamp = camp;
        cityDict[cityLoc].army.defending = true;
		cityDict[cityLoc].army.forward = (targetZone - attackLoc) / 3;
		cityDict[cityLoc].army.unitsReady = 0;
        cityDict[cityLoc].army.attackZone = attackLoc;
        cityDict[cityLoc].army.enemyTarget = targetZone;
        cityDict[cityLoc].army.enemyCityLoc = camp.cityLoc;
        cityDict[cityLoc].army.EveryoneHomeCheck();
		cityDict[cityLoc].army.RealignUnits(this, targetZone, attackLoc, attackLoc);
        if (cityDict[cityLoc].army.selected)
            unitMovement.ResetArmyHomeButtons();
        //ToggleBattleCam(camp.cityLoc, cityLoc, true);

		if (deployingArmy)
            unitMovement.CancelArmyDeployment();

        if (uiTradeRouteBeginTooltip.activeStatus && uiTradeRouteBeginTooltip.trader.homeCity == cityLoc)
        {
            if (!uiTradeRouteBeginTooltip.gameObject.activeSelf)
				unitMovement.CloseBuildingSomethingPanelButton();

            uiTradeRouteBeginTooltip.ResetTrader();
            uiTradeRouteBeginTooltip.UpdateGuardCosts();
		}
    }

    public void RevealEnemyCamp(Vector3Int loc)
    {
        if (enemyCampDict[loc].revealed)
            return;
        else
            enemyCampDict[loc].revealed = true;

        if (enemyCampDict[loc].campfire != null)
            enemyCampDict[loc].campfire.SetActive(true);
        GameLoader.Instance.gameData.discoveredEnemyCampLocs.Add(loc);

		for (int i = 0; i < enemyCampDict[loc].UnitsInCamp.Count; i++)
		{
			Military unit = enemyCampDict[loc].UnitsInCamp[i];
            unit.gameObject.SetActive(true);

			if (unit.buildDataSO.unitType != UnitType.Cavalry)
                unit.DiscoverSitting();
		}

        if (deployingArmy)
        {
			GetTerrainDataAt(loc).EnableHighlight(Color.red);
			foreach (Military unit in enemyCampDict[loc].UnitsInCamp)
				unit.SoftSelect(Color.red);
		}
    }

    public void EnemyBattleStations(Vector3Int campLoc, Vector3Int armyLoc, bool isCity)
    {
        if (isCity)
        {
            Vector3Int actualAttackLoc = enemyCityDict[campLoc].enemyCamp.attackingArmy.enemyTarget;
            Vector3Int cityLoc = campLoc;

            if (actualAttackLoc != campLoc)
                campLoc = actualAttackLoc;

            MilitaryLeader leader = null;
            if (enemyCityDict[cityLoc].empire.capitalCity == cityLoc && !enemyCityDict[cityLoc].empire.enemyLeader.isDead)
                leader = enemyCityDict[cityLoc].empire.enemyLeader;
            enemyCityDict[cityLoc].enemyCamp.threatLoc = armyLoc;
			enemyCityDict[cityLoc].enemyCamp.forward = (armyLoc - campLoc) / 3;
			enemyCityDict[cityLoc].enemyCamp.BattleStations(campLoc, enemyCityDict[cityLoc].enemyCamp.forward, leader);
			enemyCityDict[cityLoc].StopSpawnAndSendAttackCycle(true);
            //ToggleBattleCam(cityLoc, enemyCityDict[cityLoc].enemyCamp.attackingArmy.city.cityLoc, true);
        }
        else
        {
			enemyCampDict[campLoc].threatLoc = armyLoc;
			enemyCampDict[campLoc].forward = (armyLoc - campLoc) / 3;
			enemyCampDict[campLoc].BattleStations(campLoc, enemyCampDict[campLoc].forward);
		}
    }

    public void EnemyCampReturn(Vector3Int loc)
    {
        if (enemyCampDict.ContainsKey(loc))
            enemyCampDict[loc].ReturnToCamp();
    }

    public void EnemyCityReturn(Vector3Int loc)
    {
        if (enemyCityDict.ContainsKey(loc))
        {
			enemyCityDict[loc].enemyCamp.ReturnToCamp();
		}
    }

    public void ToggleCharacterConversationCam(bool v)
    {
        if (v)
        {
			if (battleLocs.Count == 0)
				battleCamera.SetActive(true);

			battleLocs.Add(RoundToInt(mainPlayer.transform.position));

			foreach (Unit unit in characterUnits)
			{
				unit.unitMesh.layer = LayerMask.NameToLayer("BattleLayer");
				unit.outline.ToggleOutline(false);
			}

            foreach (Vector3Int loc in npcPosDict.Keys)
            {
                if (npcPosDict[loc].buildDataSO.unitDisplayName == "Scott" || npcPosDict[loc].buildDataSO.unitDisplayName == "Azai")
                {
                    npcPosDict[loc].unitMesh.layer = LayerMask.NameToLayer("BattleLayer");
                    break;
                }
            }
		}
        else
        {
			battleLocs.Remove(RoundToInt(mainPlayer.transform.position));

			if (battleLocs.Count == 0)
				battleCamera.SetActive(false);

			foreach (Unit unit in characterUnits)
			{
				unit.unitMesh.layer = LayerMask.NameToLayer("Agent");
				unit.outline.ToggleOutline(true);
			}
		}
    }

    public void ToggleConversationCam(bool v, Vector3Int enemyLoc, bool enemy = false)
    {
		if (v)
        {
            if (battleLocs.Count == 0)
			    battleCamera.SetActive(true);

			battleLocs.Add(enemyLoc);

            Unit npc = GetNPC(enemyLoc);
            npc.unitMesh.layer = LayerMask.NameToLayer("BattleLayer");
            npc.outline.ToggleOutline(false);
            //npc.marker.ToggleVisibility(false);

            foreach (Unit unit in characterUnits)
            {
                unit.unitMesh.layer = LayerMask.NameToLayer("BattleLayer");
                unit.outline.ToggleOutline(false);
                //unit.marker.ToggleVisibility(false);
            }
        }
        else
        {
            string layer = enemy ? "Enemy" : "Agent";
            
            battleLocs.Remove(enemyLoc);

			if (battleLocs.Count == 0)
				battleCamera.SetActive(false);

            Unit npc = GetNPC(enemyLoc);
			npc.unitMesh.layer = LayerMask.NameToLayer(layer);
			npc.outline.ToggleOutline(true);
			//npc.MarkerCheck();

			foreach (Unit unit in characterUnits)
            {
				unit.unitMesh.layer = LayerMask.NameToLayer("Agent");
			    unit.outline.ToggleOutline(true);
                //unit.MarkerCheck();
            }
		}
	}

    public void ToggleDuelBattleCam(bool v, Vector3Int battleLoc, Unit bodyGuard, Unit leader)
    {
        if (v)
        {
		    if (battleLocs.Count == 0)
			    battleCamera.SetActive(true);

			battleLocs.Add(battleLoc);

			foreach (Unit unit in characterUnits)
            {
				unit.unitMesh.layer = LayerMask.NameToLayer("BattleLayer");
                unit.outline.ToggleOutline(false);
                //unit.marker.ToggleVisibility(false);
            }

			leader.unitMesh.layer = LayerMask.NameToLayer("BattleLayer");
            leader.outline.ToggleOutline(false);
            //leader.marker.ToggleVisibility(false);
        }
        else
        {
			battleLocs.Remove(battleLoc);

			if (battleLocs.Count == 0)
				battleCamera.SetActive(false);

			foreach (Unit unit in characterUnits)
            {
				unit.unitMesh.layer = LayerMask.NameToLayer("Agent");
				unit.outline.ToggleOutline(true);
				//unit.MarkerCheck();
			}

			leader.unitMesh.layer = LayerMask.NameToLayer("Enemy");
			leader.outline.ToggleOutline(true);
			//leader.MarkerCheck();
		}
	}

    public void ToggleBattleCam(Vector3Int enemyLoc, Vector3Int armyLoc, bool v, bool outlineOn = true)
    {
        if (v)
        {
			if (battleLocs.Count == 0)
				battleCamera.SetActive(true);

			if (!battleLocs.Contains(armyLoc))
				battleLocs.Add(armyLoc);

			if (IsEnemyCityOnTile(enemyLoc))
			{
				foreach (Military unit in enemyCityDict[enemyLoc].enemyCamp.UnitsInCamp)
                {
					unit.unitMesh.layer = LayerMask.NameToLayer("BattleLayer");
                    unit.battleCam = true;
                    unit.outline.ToggleOutline(false);
                    //unit.marker.ToggleVisibility(false);
                }
			}
			else
			{
				foreach (Military unit in enemyCampDict[enemyLoc].UnitsInCamp)
                {
					unit.unitMesh.layer = LayerMask.NameToLayer("BattleLayer");
                    unit.battleCam = true;
                    unit.outline.ToggleOutline(false);
					//unit.marker.ToggleVisibility(false);
				}
			}

			foreach (Military unit in cityDict[armyLoc].army.UnitsInArmy)
            {
				unit.unitMesh.layer = LayerMask.NameToLayer("BattleLayer");
                unit.battleCam = true;
                unit.outline.ToggleOutline(false);
                //unit.marker.ToggleVisibility(false);
            }
		}
        else
        {
			battleLocs.Remove(armyLoc);

			if (battleLocs.Count == 0)
				battleCamera.SetActive(false);

			if (IsEnemyCityOnTile(enemyLoc))
			{
				foreach (Military unit in enemyCityDict[enemyLoc].enemyCamp.UnitsInCamp)
                {
					unit.unitMesh.layer = LayerMask.NameToLayer("Enemy");
                    unit.battleCam = false;
                    if (!unit.bySea && !unit.byAir)
    					unit.outline.ToggleOutline(outlineOn);
					//unit.MarkerCheck();
				}
			}
			else
			{
				foreach (Military unit in enemyCampDict[enemyLoc].UnitsInCamp)
                {
					unit.unitMesh.layer = LayerMask.NameToLayer("Enemy");
                    unit.battleCam = false;

                    if (world[enemyLoc].treeHandler != null)
    					unit.outline.ToggleOutline(outlineOn);
					//unit.MarkerCheck();
				}
			}

			foreach (Military unit in cityDict[armyLoc].army.UnitsInArmy)
            {
				unit.unitMesh.layer = LayerMask.NameToLayer("Agent");
                unit.battleCam = false;
                if (!unit.bySea && !unit.byAir)
    				unit.outline.ToggleOutline(true);
				//unit.MarkerCheck();
			}
		}
	}

	//public void ToggleForestsInBattleClear(Vector3Int loc, Vector3Int targetLoc, bool v)
 //   {
 //       if (GetTerrainDataAt(loc).treeHandler != null)
	//		GetTerrainDataAt(loc).ToggleTransparentForest(v);
        
 //       if (GetTerrainDataAt(targetLoc).treeHandler != null)
 //           GetTerrainDataAt(targetLoc).ToggleTransparentForest(v);

 //       if (!v)
 //           RemoveBattleZones(loc, targetLoc);
 //   }

    public void BattleCamCheck(bool v)
    {
        if (battleLocs.Count > 0)
            battleCamera.SetActive(!v);
    }

	public void HighlightAllEnemyCamps()
    {
        foreach (Vector3Int tile in enemyCampDict.Keys)
        {
            if (enemyCampDict[tile].attacked || enemyCampDict[tile].attackReady || enemyCampDict[tile].inBattle)
                continue;
            
            TerrainData td = GetTerrainDataAt(tile);
            if (!td.isDiscovered)
                continue;

            td.EnableHighlight(Color.red);
            foreach (Military unit in enemyCampDict[tile].UnitsInCamp)
                unit.SoftSelect(Color.red);
        }

        foreach (Vector3Int tile in enemyCityDict.Keys)
        {
			TerrainData td = GetTerrainDataAt(tile);
			if (!td.isDiscovered)
				continue;

            if (enemyCityDict[tile].enemyCamp.movingOut)
            {
                if (enemyCityDict[tile].enemyCamp.inBattle || enemyCityDict[tile].enemyCamp.pillage || enemyCityDict[tile].enemyCamp.returning)
                    continue;

                for (int i = 0; i < enemyCityDict[tile].enemyCamp.UnitsInCamp.Count; i++)
                    enemyCityDict[tile].enemyCamp.UnitsInCamp[i].SoftSelect(Color.red);
            }
            else
            {
			    td.EnableHighlight(Color.red);
                cityBuilderManager.ToggleEnemyBuildingHighlight(tile, Color.red);
            }
		}
    }

    public void HighlightEnemyCity(Vector3Int loc, Color color)
    {
		TerrainData td = GetTerrainDataAt(loc);
		td.DisableHighlight();
		td.EnableHighlight(color);
		cityBuilderManager.ToggleEnemyBuildingHighlight(loc, color);
	}

    public void HighlightEnemyCamp(Vector3Int loc, Color color)
    {
        if (!enemyCampDict.ContainsKey(loc))
            return;

        TerrainData td = GetTerrainDataAt(loc);
		td.DisableHighlight();
		td.EnableHighlight(color);
		foreach (Military unit in enemyCampDict[loc].UnitsInCamp)
			unit.SoftSelect(color);
	}

    public bool CheckIfEnemyCamp(Vector3Int loc)
    {
        return enemyCampDict.ContainsKey(loc) && GetTerrainDataAt(loc).isDiscovered;
    }
    
    public bool CheckIfEnemyTerritory(Vector3Int loc)
    {
        TerrainData td = GetTerrainDataAt(loc);
		return td.enemyZone;
    }

    public bool CheckIfEnemyNotNeutral(Vector3Int loc)
    {
        if (GetTerrainDataAt(loc).enemyZone)
            return !neutralZones.Contains(GetClosestTerrainLoc(loc));
        else
            return false;
    }

    public bool CheckIfNeutral(Vector3Int loc)
    {
        return neutralZones.Contains(GetClosestTerrainLoc(loc));
    }

	public HashSet<Vector3Int> GetExemptList(Vector3 endLoc)
	{
        Vector3Int newEndLoc = GetClosestTerrainLoc(endLoc);
        HashSet<Vector3Int> exemptList = new() { newEndLoc };

		foreach (Vector3Int tile in GetNeighborsFor(newEndLoc, MapWorld.State.EIGHTWAYINCREMENT))
			exemptList.Add(tile);

		return exemptList;
	}

	public void UnhighlightAllEnemyCamps()
    {
		foreach (Vector3Int tile in enemyCampDict.Keys)
		{
			TerrainData td = GetTerrainDataAt(tile);
			if (!td.isDiscovered)
				continue;

			td.DisableHighlight();
			foreach (Military unit in enemyCampDict[tile].UnitsInCamp)
				unit.Unhighlight();
		}

        foreach (Vector3Int tile in enemyCityDict.Keys)
        {
			TerrainData td = GetTerrainDataAt(tile);
			if (!td.isDiscovered)
				continue;

            if (enemyCityDict[tile].enemyCamp.movingOut)
            {
				for (int i = 0; i < enemyCityDict[tile].enemyCamp.UnitsInCamp.Count; i++)
					enemyCityDict[tile].enemyCamp.UnitsInCamp[i].Unhighlight();
			}

			td.DisableHighlight();
            cityBuilderManager.ToggleBuildingHighlight(false, tile);    
		}
	}

    public void HighlightAttackingCity(Vector3Int cityLoc)
    {
        foreach (Vector3Int tile in enemyCityDict.Keys)
        {
            if (enemyCityDict[tile].enemyCamp.movingOut && enemyCityDict[tile].enemyCamp.moveToLoc == cityLoc)
            {
				for (int i = 0; i < enemyCityDict[tile].enemyCamp.UnitsInCamp.Count; i++)
					enemyCityDict[tile].enemyCamp.UnitsInCamp[i].SoftSelect(Color.red);
			}
        }
    }

    public bool CheckIfEnemyAlreadyAttacked(Vector3Int loc)
    {
        if (enemyCampDict.ContainsKey(loc))
            return enemyCampDict[loc].attacked || enemyCampDict[loc].inBattle || enemyCampDict[loc].attackReady;
        else
            return enemyCityDict[loc].enemyCamp.attacked || enemyCityDict[loc].enemyCamp.inBattle || enemyCityDict[loc].enemyCamp.attackReady;
    }

    public void SetEnemyCityAsAttacked(Vector3Int loc, Army army)
    {
        enemyCityDict[loc].enemyCamp.attacked = true;
		enemyCityDict[loc].enemyCamp.attackingArmy = army;
        enemyCityDict[loc].enemyCamp.forward = army.forward * -1;
        enemyCityDict[loc].CancelSendAttackWait();
	}

    public void SetEnemyCampAsAttacked(Vector3Int loc, Army army)
    {
        enemyCampDict[loc].attacked = true;
        enemyCampDict[loc].attackingArmy = army;
        enemyCampDict[loc].forward = army.forward * -1;
        GameLoader.Instance.gameData.attackedEnemyBases[loc] = new();
    }

    public void PlayDeathSplash(Vector3 loc, Vector3 rotation)
    {
		ParticleSystem deathSplash = Instantiate(Resources.Load<ParticleSystem>("Prefabs/ParticlePrefabs/DeathSplash"), loc, Quaternion.identity);
        deathSplash.transform.SetParent(psHolder, false);
        deathSplash.transform.rotation = Quaternion.Euler(rotation);
        deathSplash.Play();
    }

    public void PlayRemoveSplash(Vector3 loc)
    {
		ParticleSystem removeSplash = Instantiate(Resources.Load<ParticleSystem>("Prefabs/ParticlePrefabs/RemoveSplash"), loc, Quaternion.identity);
        removeSplash.transform.SetParent(psHolder, false);
		Vector3 rotation = new Vector3(-90, 0, 0);
		removeSplash.transform.rotation = Quaternion.Euler(rotation);
		removeSplash.Play();
	}

	public void PlayRemoveEruption(Vector3 loc, bool big = false)
    {
        ParticleSystem removeEruption = Instantiate(Resources.Load<ParticleSystem>("Prefabs/ParticlePrefabs/RemoveEruption"), loc, Quaternion.Euler(-90, 0, 0));
        removeEruption.transform.SetParent(objectPoolItemHolder, false);
        if (big)
			removeEruption.transform.localScale = new Vector3(2, 2, 2);
		removeEruption.Play();
    }

    public void RemoveEnemyCamp(Vector3Int loc, bool isCity)
    {
        TerrainData camp = GetTerrainDataAt(loc);
		camp.enemyCamp = false;
        camp.enemyZone = false;
        neutralZones.Remove(loc);
		RemoveSingleBuildFromCityLabor(loc);

        List<Vector3Int> enemyZones = GetNeighborsFor(loc, State.EIGHTWAYINCREMENT);
        for (int i = 0; i < enemyZones.Count; i++)
        {
            List<Vector3Int> neighborTiles = GetNeighborsFor(enemyZones[i], State.FOURWAYINCREMENT);
            for (int j = 0; j < neighborTiles.Count; j++)
            {
				if (enemyZones.Contains(neighborTiles[j]))
                    continue;

                TerrainData nextTD = GetTerrainDataAt(neighborTiles[j]);
				if (nextTD.enemyZone)
				{
                    Color borderColor = new Color(1, 0, 0, 0.68f);
                    //Checking if enemy zone is enemy empire
                    List<Vector3Int> potentialEnemyCityTiles = GetNeighborsFor(neighborTiles[j], State.EIGHTWAYINCREMENT);
                    for (int k = 0; k < potentialEnemyCityTiles.Count; k++)
                    {
                        if (enemyCityDict.ContainsKey(potentialEnemyCityTiles[k]))
                        {
                            borderColor = enemyCityDict[potentialEnemyCityTiles[k]].empire.enemyLeader.borderColor;
                            break;
                        }
                    }
                    
                    Vector3Int borderLocation = enemyZones[i] - neighborTiles[j];
                    Vector3 borderPosition = neighborTiles[j];
					//borderPosition.y = -0.1f;
					Quaternion rotation = Quaternion.identity;

					if (borderLocation.x != 0)
					{
                        borderPosition.x += 0.5f * borderLocation.x;// (borderPosition.x / 3 * 0.99f);
						rotation = Quaternion.Euler(0, 90, 0); //only need to rotate on this one
						borderPosition.x -= borderLocation.x > 0 ? -.01f : .01f;
					}
					else if (borderLocation.z != 0)
					{
                        borderPosition.z += 0.5f * borderLocation.z;// (borderPosition.z / 3 * 0.99f);
						borderPosition.z -= borderLocation.z > 0 ? -.01f : .01f;
					}

					GameObject border = Instantiate(Resources.Load<GameObject>("Prefabs/InGameSpritePrefabs/EnemyBorder"), borderPosition, rotation);
					border.GetComponent<SpriteRenderer>().color = borderColor;
					border.transform.SetParent(terrainHolder, false);

                    if (!enemyBordersDict.ContainsKey(neighborTiles[j]))
                        enemyBordersDict[neighborTiles[j]] = new();

                    enemyBordersDict[neighborTiles[j]].Add(border);
				}
			}

			TerrainData td = GetTerrainDataAt(enemyZones[i]);
            DestroyBorders(enemyZones[i]);
            td.enemyZone = false;
            neutralZones.Remove(enemyZones[i]);

			RemoveSingleBuildFromCityLabor(enemyZones[i]);

            if (isCity && cityImprovementDict.ContainsKey(enemyZones[i]))
            {
				CityImprovement improvement = cityImprovementDict[enemyZones[i]];
                improvement.gameObject.tag = "Player";
                improvement.StopJustWorkAnimation();
			}
		}

        if (isCity)
        {
            //revealing everything within the city borders
            foreach (Vector3Int tile in GetNeighborsFor(loc, State.CITYRADIUS))
            {
                if (!world[tile].isDiscovered)
                {
                    world[tile].Reveal(true);

					if (IsRoadOnTerrain(tile))
						SetRoadActive(tile);

					if (cityImprovementDict.ContainsKey(tile))
						cityImprovementDict[tile].RevealImprovement(false);

					if (IsTradeCenterMainOnTile(tile))
						GetTradeCenter(tile).Reveal();
				}
            }
            
            //playing god rays to show triumph
			ParticleSystem godRays = Instantiate(Resources.Load<ParticleSystem>("Prefabs/ParticlePrefabs/Godrays"));
            godRays.transform.SetParent(objectPoolItemHolder, false);
			godRays.transform.position = loc + new Vector3(1, 3, 0);
			godRays.Play();

			City city = enemyCityDict[loc];
            city.PlaySelectAudio(cityBuilderManager.sunBeam);
			StartCoroutine(EnemyCityCampDestroyWait(loc));

			//placing treasure
			PlaceTreasureChest(loc, enemyCityDict[loc].enemyCamp.forward, true);

			if (city.activeCity)
			    cityBuilderManager.ResetCityUI();

            city.SetWorld(this, true); //reset dicts and add wonder benefits
            cityDict[loc] = city;
            city.gameObject.tag = "Player";
            city.cityNameField.SetOriginalBackground();
            city.StopSpawnAndSendAttackCycle(false);
            city.StartGrowthCycle(false);
			city.minimapIcon.sprite = city.cityIcon;
            city.gameObject.transform.SetParent(cityHolder, false);
			cityCount++;
			city.CheckForAvailableSingleBuilds();

			if (IsCityNameTaken(city.cityName))
            {
                bool found = false;
                int i = 2;
                string newName = city.cityName;
                while (!found)
                {
                    i++;
                    newName = newName + " " + i;

                    if (!IsCityNameTaken(newName))
                        found = true;
                }
                
                city.UpdateCityName(newName);
            }

            city.isNamed = true;
            tradeStopNameDict[city.cityName] = city;

            foreach (SingleBuildType type in city.singleBuildDict.Keys)
                tradeStopDict[city.singleBuildDict[type]] = city;

            unitMovement.workerTaskManager.SetCityBools(city, city.cityLoc);
			city.waterCount = city.hasFreshWater ? 9999 : 0;

			//give some food so pop don't start starving immediately
			city.resourceManager.AddResource(ResourceType.Food, city.currentPop * 3);

            List<ResourceType> resourcesToAdd = new() { ResourceType.Lumber, ResourceType.Stone };
            for (int i = 0; i < resourcesToAdd.Count; i++)
                city.resourceManager.AddResource(resourcesToAdd[i],UnityEngine.Random.Range(city.currentPop,city.currentPop * 4));

            city.empire.empireCities.Remove(city.cityLoc);
            if (city.empire.empireCities.Count == 0)
            {
				allEnemyLeaders.Remove(city.empire.enemyLeader);
				Destroy(city.empire.enemyLeader.gameObject);
			}

            if (city.empire.attackingCity == city.cityLoc)
                city.empire.SetNextAttackingCity(this, city.cityLoc);

            GameLoader.Instance.RemoveEnemyCity(loc);
		}
        else
        {
            PlaceTreasureChest(loc, enemyCampDict[loc].forward, false);
            StartCoroutine(EnemyCampDestroyWait(loc));
            if (enemyCampDict[loc].campfire != null)
                Destroy(enemyCampDict[loc].campfire);
            Destroy(enemyCampDict[loc].minimapIcon);
            GameLoader.Instance.RemoveEnemyCamp(loc);
        }
	}

    public Vector3Int GetCloserTile(Vector3Int testTile, Vector3Int tile1, Vector3Int tile2)
    {
		//tie goes to tile1
		int tile1Dist = Mathf.Abs(tile1.x - testTile.x) + Mathf.Abs(tile1.z - testTile.z);
        int tile2Dist = Mathf.Abs(tile2.x - testTile.x) + Mathf.Abs(tile2.z - testTile.z);

        if (tile2Dist < tile1Dist)
            return tile2;
        else
            return tile1;
	}

    private void PlaceTreasureChest(Vector3Int loc, Vector3Int forward, bool isCity)
    {
        Vector3 placementLoc = loc;
        if (GetTerrainDataAt(loc).isHill)
            placementLoc.y += 0.65f;
        Vector3Int rotationLoc = loc + forward;

		Vector3 direction = rotationLoc - loc;
		Quaternion rotation;
		if (direction == Vector3.zero)
			rotation = Quaternion.identity;
		else
			rotation = Quaternion.LookRotation(direction, Vector3.up);

        int amount = 0;
        if (isCity)
        {
			Vector2Int goldRange = enemyCityDict[loc].empire.enemyLeader.buildDataSO.goldDropRange;
			amount += UnityEngine.Random.Range(goldRange.x, goldRange.y);

            if (enemyCityDict[loc].empire.capitalCity == loc)
                amount += goldRange.x / 4;
		}
        else
        {
            for (int i = 0; i < enemyCampDict[loc].UnitsInCamp.Count; i++)
            {
                Vector2Int goldRange = enemyCampDict[loc].UnitsInCamp[i].buildDataSO.goldDropRange;
                amount += UnityEngine.Random.Range(goldRange.x, goldRange.y);
            }
        }

        if (amount > 0)
        {
            GameObject chestGO = Instantiate(Resources.Load<GameObject>("Prefabs/MiscPrefabs/TreasureChest"), placementLoc, rotation);
			chestGO.transform.SetParent(unitHolder, false);
			TreasureChest chest = chestGO.GetComponent<TreasureChest>();
            chest.amount = amount;

		    treasureLocs[loc] = chest;
            GameLoader.Instance.gameData.treasureLocs[loc] = (amount, direction);

			//for tweening
			Vector3 goScale = chestGO.transform.localScale;
			chestGO.transform.localScale = Vector3.zero;
			LeanTween.scale(chestGO, goScale, 0.5f).setEase(LeanTweenType.easeOutBack);
		}
    }

    public void LoadTreasureChest(Vector3Int loc, int amount, Vector3 direction)
    {
        Vector3 placementLoc = loc;
        if (GetTerrainDataAt(loc).isHill)
            placementLoc.y += .65f;
        Quaternion rotation;
		if (direction == Vector3.zero)
			rotation = Quaternion.identity;
		else
			rotation = Quaternion.LookRotation(direction, Vector3.up);

		GameObject chestGO = Instantiate(Resources.Load<GameObject>("Prefabs/MiscPrefabs/TreasureChest"), placementLoc, rotation);
        chestGO.transform.SetParent(unitHolder, false);
        TreasureChest chest = chestGO.GetComponent<TreasureChest>();
		chest.amount = amount;

		treasureLocs[loc] = chest;
	}

    public void CheckTileForTreasure(Vector3Int tile)
    {
        if (treasureLocs.ContainsKey(tile))
            IsTreasureHere(tile, false);
    }

    public bool IsTreasureHere(Vector3Int tile, bool player)
    {
        if (treasureLocs.ContainsKey(tile))
        {
			UpdateWorldGold(treasureLocs[tile].amount);
			InfoResourcePopUpHandler.CreateResourceStat(tile, treasureLocs[tile].amount, ResourceHolder.Instance.GetIcon(ResourceType.Gold), this);
            Destroy(treasureLocs[tile].gameObject);
			treasureLocs.Remove(tile);
			GameLoader.Instance.gameData.treasureLocs.Remove(tile);

            if (player)
                mainPlayer.PlayRingAudio();
            else
                cityBuilderManager.PlaySelectAudio(cityBuilderManager.ringClip);

            PlayResourceSplash(tile);

			return true;
        }
        else
        {
            return false;
        }
    }

    public void ReceiveQuestReward(Vector3 loc, int amount)
    {
		UpdateWorldGold(amount);
		InfoResourcePopUpHandler.CreateResourceStat(loc, amount, ResourceHolder.Instance.GetIcon(ResourceType.Gold), this);

		cityBuilderManager.PlaySelectAudio(cityBuilderManager.ringClip);
		
		PlayResourceSplash(loc);
	}

    public void PlayResourceSplash(Vector3 loc)
    {
        ParticleSystem splash = Instantiate(Resources.Load<ParticleSystem>("Prefabs/ParticlePrefabs/ResourceSplash"), loc, Quaternion.Euler(-90, UnityEngine.Random.Range(0,360), 0));
        splash.transform.SetParent(psHolder, false);
    }

    public void PlayGiftResponse(Vector3 pos, bool doneGood)
    {
		Quaternion rotation = Quaternion.Euler(new Vector3(-90, 0, 0));
		pos.y += 0.2f;
        ParticleSystem ps;

		if (doneGood)
			ps = Instantiate(Resources.Load<ParticleSystem>("Prefabs/ParticlePrefabs/GiftResponsePos"), pos, rotation);
		else
			ps = Instantiate(Resources.Load<ParticleSystem>("Prefabs/ParticlePrefabs/GiftResponseNeg"), pos, rotation);

        ps.transform.SetParent(psHolder, false);
	}

	public void ClearCityEnemyDead(Vector3Int loc)
    {
        StartCoroutine(EnemyCityClearDeadWait(loc));
    }

    private IEnumerator EnemyCityClearDeadWait(Vector3Int loc)
    {
		yield return new WaitForSeconds(5);

		foreach (Military unit in enemyCityDict[loc].enemyCamp.DeadList)
			Destroy(unit.gameObject);

		enemyCityDict[loc].enemyCamp.DeadList.Clear();
	}

    private IEnumerator EnemyCityCampDestroyWait(Vector3Int loc)
    {
		yield return new WaitForSeconds(5);

		foreach (Military unit in enemyCityDict[loc].enemyCamp.DeadList)
			Destroy(unit.gameObject);

		enemyCityDict[loc].enemyCamp.DeadList.Clear();
        enemyCityDict[loc].enemyCamp = null;
        enemyCityDict.Remove(loc);
	}

    private IEnumerator EnemyCampDestroyWait(Vector3Int loc)
    {
        yield return new WaitForSeconds(5);
        
        foreach (Military unit in enemyCampDict[loc].DeadList)
			Destroy(unit.gameObject);

		enemyCampDict[loc].DeadList.Clear();
        enemyCampDict.Remove(loc);
	}

	public int GetUpgradeableObjectMaxLevel(string name)
    {
        return upgradeableObjectMaxLevelDict[name];
    }

    public void SetUpgradeableObjectMaxLevel(string name, int level)
    {
        if (!upgradeableObjectMaxLevelDict.ContainsKey(name) || upgradeableObjectMaxLevelDict[name] < level)
            upgradeableObjectMaxLevelDict[name] = level;
    }

    public (List<ResourceValue>, List<ResourceValue>) CalculateUpgradeCost(string nameAndLevel, string upgradeNameAndLevel, bool unit)
    {
        List<ResourceValue> originalCost, upgradeCost;
        
        if (unit)
        {
            originalCost = UpgradeableObjectHolder.Instance.unitDict[nameAndLevel].unitCost;
            upgradeCost = UpgradeableObjectHolder.Instance.unitDict[upgradeNameAndLevel].unitCost;
        }
        else
        {
			originalCost = UpgradeableObjectHolder.Instance.improvementDict[nameAndLevel].improvementCost;
			upgradeCost = UpgradeableObjectHolder.Instance.improvementDict[upgradeNameAndLevel].improvementCost;
		}

        Dictionary<ResourceType, int> origResourceCosts = new(); //making dict to more easily find the data
		List<ResourceType> sharedResourceTypes = new(); //making dict to more easily find the data
		List<ResourceValue> upgradeRefund = new();
		List<ResourceValue> upgradeableObjectCost = new();

		foreach (ResourceValue origResourceValue in originalCost)
			origResourceCosts[origResourceValue.resourceType] = origResourceValue.resourceAmount;

		foreach (ResourceValue resourceValue in upgradeCost)
		{
			if (origResourceCosts.ContainsKey(resourceValue.resourceType))
			{
                sharedResourceTypes.Add(resourceValue.resourceType);
                ResourceValue newResourceValue;
				newResourceValue.resourceType = resourceValue.resourceType;
				newResourceValue.resourceAmount = resourceValue.resourceAmount - origResourceCosts[resourceValue.resourceType];
				if (newResourceValue.resourceAmount > 0)
                {
					upgradeableObjectCost.Add(newResourceValue);
                }
                else if (newResourceValue.resourceAmount < 0)
                {
                    newResourceValue.resourceAmount *= -1;
                    upgradeRefund.Add(newResourceValue);
                }
			}
			else //if it doesn't have the resourceType, then add the whole thing
			{
				upgradeableObjectCost.Add(resourceValue);
			}
		}

        //refunding resources that aren't shared
        foreach (ResourceValue value in originalCost)
        {
            if (!sharedResourceTypes.Contains(value.resourceType))
                upgradeRefund.Add(value);
        }

        return (upgradeableObjectCost, upgradeRefund);
	}

    public void SetTerrainData(Vector3Int tile, TerrainData td)
    {
        world[tile] = td;
    }

    public void SetCityDevelopment(Vector3Int tile, CityImprovement cityDevelopment)
    {
        cityImprovementDict[tile] = cityDevelopment;
    }

    public void SetCityBuilding(CityImprovement improvement, ImprovementDataSO improvementData, Vector3Int cityTile, City city, string buildingName)
    {
        improvement.building = improvementData.isBuilding;
        improvement.InitializeImprovementData(improvementData);
        improvement.city = city;
        improvement.transform.parent = city.transform;
        city.workEthic += improvementData.workEthicChange;
        city.improvementWorkEthic += improvementData.workEthicChange;
		city.purchaseAmountMultiple += improvementData.purchaseAmountChange;
		cityBuildingDict[cityTile][buildingName] = improvement;

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

        //tempObject.transform.localScale = improvement.transform.localScale;
        improvement.Embiggen();
        city.AddToMeshFilterList(tempObject, meshes, true, Vector3Int.zero, buildingName);
        tempObject.transform.parent = city.transform;
        tempObject.SetActive(false);
    }

    public void AddLocationToQueueList(Vector3Int location, Vector3Int cityLoc)
    {
        cityImprovementQueueList[location] = cityLoc;
    }

    public bool CheckQueueLocation(Vector3Int location)
    {
        return cityImprovementQueueList.ContainsKey(location);
    }

    public void RemoveLocationFromQueueList(Vector3Int location)
    {
        cityImprovementQueueList.Remove(location);  
    }

  //  public void RemoveQueueItemCheck(Vector3Int location) //two in worker
  //  {
		//if (cityImprovementQueueList.ContainsKey(location))
  //      {
  //          City city = GetCity(cityImprovementQueueList[location]);
  //          Vector3Int localLocation = location - city.cityLoc;

  //          if (city.activeCity && cityBuilderManager.uiQueueManager.activeStatus)
  //          {
  //              QueueItem item = city.improvementQueueDict[localLocation];
  //              cityBuilderManager.RemoveQueueGhostImprovement(item);

		//		List<UIQueueItem> tempQueueItemList = new(cityBuilderManager.uiQueueManager.uiQueueItemList);
		//		for (int i = 0; i < tempQueueItemList.Count; i++)
		//		{
		//			if (tempQueueItemList[i].item.queueLoc == item.queueLoc)
		//			{
  //                      cityBuilderManager.uiQueueManager.RemoveFromQueue(tempQueueItemList[i], city.cityLoc);
		//				break;
		//			}
		//		}
  //          }
  //          else
  //          {
  //  			city.RemoveFromQueue(localLocation);
  //          }
  //      }
  //  }

    public void SetRoads(Vector3Int tile, Road road, bool straight)
    {
        int index = straight ? 0 : 1;
        roadTileDict[tile][index] = road;
    }

    public int GetRoadLevel(Vector3Int tile)
    {
        int index = 0;

        if (roadTileDict[tile][index] == null)  
            index = 1;

        return roadTileDict[tile][index].roadLevel;
    }

    public void SetRoadLocations(Vector3Int tile)
    {
        roadLocsList.Add(tile);
    }

    public void SetSoloRoadLocations(Vector3Int tile)
    {
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
        return cityDict.ContainsKey(tile);
    }

    public bool WonderTileCheck(Vector3Int tile)
    {
        return wonderTiles.Contains(tile);
    }

    public bool IsWonderOnTile(Vector3Int tile)
    {
        return tradeStopDict.ContainsKey(tile) && tradeStopDict[tile].wonder;
    }

    public bool IsTradeCenterOnTile(Vector3Int tile)
    {
        return tradeStopDict.ContainsKey(tile) && tradeStopDict[tile].center != null;
    }

    public bool IsTradeCenterMainOnTile(Vector3Int tile)
    {
        return tradeStopDict.ContainsKey(tile) && tradeStopDict[tile].center != null && tradeStopDict[tile].mainLoc == tile;
	}

    public bool IsEnemyCityOnTile(Vector3Int tile)
    {
        return enemyCityDict.ContainsKey(tile);
    }

    public bool IsBuildLocationTaken(Vector3Int buildLoc)
    {
        return buildingPosDict.ContainsKey(buildLoc);
    }

    public bool IsPlayerLocationTaken(Vector3Int pos)
    {
        return playerPosDict.ContainsKey(pos);
    }

    public bool IsTraderLocationTaken(Vector3Int pos)
    {
        return traderPosDict.ContainsKey(pos);
    }

    public bool IsUnitLocationTaken(Vector3Int unitPosition)
    {
        return unitPosDict.ContainsKey(unitPosition);
    }

    public bool IsNPCThere(Vector3Int loc)
    {
        return npcPosDict.ContainsKey(loc);
    }

    public void SetNPCLoc(Vector3 pos, Unit npc)
    {
        Vector3Int loc = RoundToInt(pos);
        npcPosDict[loc] = npc;
    }

    public Unit GetNPC(Vector3Int loc)
    {
        return npcPosDict[loc];
    }

    public void RemoveNPCLoc(Vector3Int loc)
    {
        npcPosDict.Remove(loc);
    }

    public bool IsRoadOnTerrain(Vector3Int position)
    {
        return roadTileDict.ContainsKey(position);
    }

    public void SetRoadActive(Vector3Int position)
    {
        for (int i = 0; i < roadTileDict[position].Count; i++)
        {
            Road road = roadTileDict[position][i];
            if (road == null)
                continue;

			road.gameObject.SetActive(true);
			roadManager.roadMeshList.Add(road.MeshFilter);
            roadManager.colliderMeshList.Add(road.colliderMesh);
		}

        roadManager.CombineMeshes();
    }

    public bool IsCityNameTaken(string cityName)
    {
        foreach (string name in tradeStopNameDict.Keys)
        {
            if (cityName.ToLower() == name.ToLower())
            {
                return true;
            }
        }

        return false;
    }

    public Vector3Int GetClosestMoveToSpot(Vector3Int tile, Vector3 position, bool sea)
    {
        //checking closest spot first
        Vector3Int positionInt = RoundToInt(position);
		Vector3Int diff = positionInt - tile;
        
        if (diff.x != 0)
            diff.x = (diff.x > 0) ? 1 : -1;
        if (diff.z != 0)
            diff.z = (diff.z > 0) ? 1 : -1;
        Vector3Int newTile = diff + tile;

        if (sea)
        {
            if (PlayerCheckIfSeaPositionIsValid(newTile))
                return newTile;
		}
        else
        {
            if (PlayerCheckIfPositionIsValid(newTile))
                return newTile;
        }

        //checking which spot has closeset open spot
        Vector3Int[] tilesChecked = new Vector3Int[4];
        int[] triesArray = new int[4];
        List<Vector3Int> neighbors = GetNeighborsCoordinates(State.FOURWAY);

        for (int i = 0; i < neighbors.Count; i++)
        {
            Vector3Int trySpot = tile;
            int tries = 0;
            
            while (tries < 10)
            {
                trySpot += neighbors[i];

                if (sea)
                {
                    if (PlayerCheckIfSeaPositionIsValid(trySpot))
                        break;
                }
                else
                {
					if (PlayerCheckIfPositionIsValid(trySpot))
						break;
				}

				tries++;
            }

            tilesChecked[i] = trySpot;
            triesArray[i] = tries;
        }

        int min = triesArray.Min();
        Vector3Int bestSpot = tile;
        int dist = 0;
        bool firstOne = true;

        for (int i = 0; i < tilesChecked.Length; i++)
        {
			if (triesArray[i] == min)
			{
			    if (firstOne)
                {
                    firstOne = false;
                    dist = Mathf.Abs(positionInt.x - tilesChecked[i].x) + Mathf.Abs(positionInt.z - tilesChecked[i].z);
                    bestSpot = tilesChecked[i];
                }

                int newDist = Mathf.Abs(positionInt.x - tilesChecked[i].x) + Mathf.Abs(positionInt.z - tilesChecked[i].z);

			    if (newDist < dist)
                {
                    dist = newDist;
                    bestSpot = tilesChecked[i];
                }
			}
        }
        
        return bestSpot;
    }

    //for movement
    public bool PlayerCheckIfPositionIsValid(Vector3Int tile)
    {
        return world.ContainsKey(tile) && world[tile].canPlayerWalk && !noWalkList.Contains(tile);
	}
    
    public bool CheckIfPositionIsValid(Vector3Int tile)
    {
        return world.ContainsKey(tile) && world[tile].canWalk;
    }

	public bool CheckIfPositionIsArmyValid(Vector3Int tile) //preventing going diagonally
    {
		return world.ContainsKey(tile) && world[tile].canPlayerWalk && !noWalkList.Contains(tile) && !world[tile].sailable && !world[tile].enemyZone;
	}

	public bool CheckIfAmphibuousPositionIsValid(Vector3Int tile)
    {
		return world.ContainsKey(tile) && (world[tile].canWalk || !world[tile].isLand);
	}

    public bool CheckForFinalMarch(Vector3Int tile)
    {
        return !world[tile].isDiscovered || world[tile].terrainData.terrainDesc == TerrainDesc.Mountain;
    }

    public bool CheckIfPositionIsValidForEnemy(Vector3Int tile)
    {
		return world.ContainsKey(tile) && world[tile].walkable;
	}

    public bool CheckIfPositionIsMarchableForEnemy(Vector3Int tile)
    {
		return world.ContainsKey(tile) && world[tile].walkable && !enemyCampDict.ContainsKey(tile) && !noWalkList.Contains(tile) && !tempBattleZone.Contains(tile) && !enemyAmbushDict.ContainsKey(tile);
	}

    public bool CheckIfPositionIsEnemyArmyValid(Vector3Int tile) //preventing going diagonally
	{ 
		return world.ContainsKey(tile) && world[tile].walkable && !world[tile].sailable && !noWalkList.Contains(tile) && !tempBattleZone.Contains(tile) && !enemyAmbushDict.ContainsKey(tile);
	}

	public bool PlayerCheckIfSeaPositionIsValid(Vector3Int tile)
	{
		return world.ContainsKey(tile) && world[tile].canPlayerSail && !noWalkList.Contains(tile);
	}

	public bool CheckIfSeaPositionIsValid(Vector3Int tile)
    {
        return world.ContainsKey(tile) && world[tile].canSail && !noWalkList.Contains(tile);
    }

	public bool CheckIfPositionIsSailableForEnemy(Vector3Int tile)
	{
		return world.ContainsKey(tile) && world[tile].sailable && !enemyCampDict.ContainsKey(tile) && !noWalkList.Contains(tile) && !tempBattleZone.Contains(tile) && !enemyAmbushDict.ContainsKey(tile);
	}

	public bool CheckIfSeaPositionIsValidForEnemy(Vector3Int tile)
	{
		return world.ContainsKey(tile) && world[tile].sailable && !noWalkList.Contains(tile) && !tempBattleZone.Contains(tile) && !enemyAmbushDict.ContainsKey(tile);
	}

    public bool PlayerCheckIfAirPositionIsValid(Vector3Int tile)
    {
        return world.ContainsKey(tile) && world[tile].canPlayerFly;
    }

    public bool CheckIfAirPositionIsValid(Vector3Int tile)
    {
        return world.ContainsKey(tile) && world[tile].canFly;
    }

    public bool CheckIfPositionIsFlyableForEnemy(Vector3Int tile)
    {
        return world.ContainsKey(tile) && !world[tile].border && !tempBattleZone.Contains(tile) && !enemyAmbushDict.ContainsKey(tile); //currently can't go over land or sea battles
    }

	public bool CheckIfCoastCoast(Vector3Int tile)
    {
        return coastCoastList.Contains(tile);
    }

    public void AddToCoastList(Vector3Int tile)
    {
        coastCoastList.Add(tile);
    }

    public void RemoveFromCoastList(Vector3Int tile)
    {
        coastCoastList.Remove(tile);
    } 

    public int GetMovementCost(Vector3Int tileWorldPosition)
    {
        return world[tileWorldPosition].MovementCost;
    }

    public int GetMovementCostAmphibious(Vector3Int tile)
    {
        if (world[tile].sailable)
            return 9; 
        else
            return world[tile].MovementCost; 
    }

    public TerrainData GetTerrainDataAt(Vector3Int tileWorldPosition)
    {
        return world[tileWorldPosition];
        //world.TryGetValue(tileWorldPosition, out TerrainData td);
        //return td;
    }

    public bool TileExists(Vector3Int tile)
    {
        return world.ContainsKey(tile);
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

    private readonly static List<Vector3Int> centerAndNeighborsEightDirectionsArmy = new()
    {
        new Vector3Int(0,0,-1), //down
        new Vector3Int(-1,0,-1), //lower left
        new Vector3Int(1,0,-1), //lower right
        new Vector3Int(0, 0, 0), //center
        new Vector3Int(-1,0,0), //left
        new Vector3Int(1,0,0), //right
		new Vector3Int(0,0,1), //up
        new Vector3Int(-1,0,1), //upper left
        new Vector3Int(1,0,1), //upper right
    };

	public enum State { FOURWAY, FOURWAYINCREMENT, EIGHTWAY, EIGHTWAYARMY, EIGHTWAYTWODEEP, EIGHTWAYINCREMENT, CITYRADIUS };

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
            case State.EIGHTWAYARMY:
                listToUse = new(centerAndNeighborsEightDirectionsArmy);
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
			case State.EIGHTWAYINCREMENT:
				return new(neighborsEightDirectionsIncrement);
			case State.EIGHTWAYTWODEEP:
                return new(neighborsEightDirectionsTwoDeep);
            case State.CITYRADIUS:
                return new(cityRadius);
        }

        return neighbors;
    }

    public (HashSet<Vector3Int>, List<Vector3Int>, List<Vector3Int>) GetCityRadiusFor(Vector3Int worldTilePosition) //two tile deep layer around specific city
    {
        HashSet<Vector3Int> neighbors = new();
        List<Vector3Int> developed = new();
        List<Vector3Int> constructing = new();
        foreach (Vector3Int direction in cityRadius)
        {
            Vector3Int checkPosition = worldTilePosition + direction;
            if (world.ContainsKey(checkPosition))
            {
                if (cityWorkedTileDict.ContainsKey(checkPosition) && GetCityLaborForTile(checkPosition) != worldTilePosition)
                    continue;

                if (unclaimedSingleBuildList.Contains(checkPosition))
                    continue;

                neighbors.Add(checkPosition);
                if (CompletedImprovementCheck(checkPosition))
                    developed.Add(checkPosition);
                else if (TileHasCityImprovement(checkPosition))
                    constructing.Add(checkPosition);
            }

        }
        return (neighbors, developed, constructing);
    }

    public List<Vector3Int> HarborStallLocs(int eulerAngle)
    {
        List<Vector3Int> stallLocs = new();
        
        if (eulerAngle == 270)
        {
            stallLocs.Add(new Vector3Int(-1, 0, 1));
			stallLocs.Add(new Vector3Int(-1, 0, 0));
			stallLocs.Add(new Vector3Int(-1, 0, -1));
		}
        else if (eulerAngle == 180)
        {
			stallLocs.Add(new Vector3Int(-1, 0, -1));
			stallLocs.Add(new Vector3Int(0, 0, -1));
			stallLocs.Add(new Vector3Int(1, 0, -1));
		}
        else if (eulerAngle == 90)
        {
			stallLocs.Add(new Vector3Int(1, 0, -1));
			stallLocs.Add(new Vector3Int(1, 0, 0));
			stallLocs.Add(new Vector3Int(1, 0, 1));
		}
        else
        {
			stallLocs.Add(new Vector3Int(-1, 0, 1));
			stallLocs.Add(new Vector3Int(0, 0, 1));
			stallLocs.Add(new Vector3Int(1, 0, 1));
		}

        return stallLocs;
    }

    public List<Vector3Int> GetWorkedCityRadiusFor(Vector3Int worldTilePosition) //two ring layer around specific city
    {
        List<Vector3Int> neighbors = new();
        foreach (Vector3Int direction in cityRadius)
        {
            Vector3Int checkPosition = worldTilePosition + direction;
            if (world.ContainsKey(checkPosition)) //checking if it exists in world
            {
                if (cityWorkedTileDict.ContainsKey(checkPosition) && GetCityLaborForTile(checkPosition) == worldTilePosition)//if city has worked tiles, add to list
                    neighbors.Add(checkPosition);
            }

        }
        return neighbors;
    }

    //to see what is developed for a city and what's worked for the city specifically
    public List<Vector3Int> GetPotentialLaborLocationsForCity(Vector3Int cityTile)
    {
        List<Vector3Int> neighbors = new();

        foreach (Vector3Int direction in cityRadius)
        {
            Vector3Int neighbor = cityTile + direction;

            if (CompletedImprovementCheck(neighbor) && GetCityDevelopment(neighbor).GetImprovementData.maxLabor > 0)
            {
                if ((cityWorkedTileDict.ContainsKey(neighbor) && GetCityLaborForTile(neighbor) != cityTile) || CheckIfTileIsMaxxed(neighbor))
                    continue;

                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    public (List<(Vector3Int, bool, int[])>, int[], int[]) GetRoadNeighborsFor(Vector3Int position, bool river)
    {
        List<(Vector3Int, bool, int[])> neighbors = new();
        int[] straightRoads = { 0, 0, 0, 0 };
        int[] diagRoads = { 0, 0, 0, 0 }; 

        if (river)
        {
            for (int i = 0; i < neighborsFourDirectionsIncrement.Count; i++)
            {
				Vector3Int neighbor = neighborsFourDirectionsIncrement[i] + position;
                
                if (GetTerrainDataAt(neighbor).isLand)
                {
                    straightRoads[i] = 1;

                    if (roadTileDict.ContainsKey(neighbor))
                    {
					    int[] neighborRoads = { 0, 0, 0, 0 };

					    for (int j = 0; j < neighborsFourDirectionsIncrement.Count; j++)
					    {
						    if (roadTileDict.ContainsKey(neighbor + neighborsFourDirectionsIncrement[j]))
							    neighborRoads[j] = 1;
					    }

					    neighbors.Add((neighbor, true, neighborRoads));
                    }
                }
			}

            if (straightRoads[0] + straightRoads[2] == 2)
            {
                straightRoads[1] = 0;
                straightRoads[3] = 0;
            }
            else
            {
				straightRoads[0] = 0;
				straightRoads[2] = 0;
			}

            return (neighbors, straightRoads, diagRoads);
        }

        for (int i = 0; i < neighborsEightDirectionsIncrement.Count; i++)
        {
			Vector3Int neighbor = neighborsEightDirectionsIncrement[i] + position;

			if (roadTileDict.ContainsKey(neighbor))
			{
    			bool straightFlag = i % 2 == 0;
                if (!straightFlag && GetTerrainDataAt(neighbor).straightRiver)
                    continue;

				int[] neighborRoads = { 0, 0, 0, 0 };

				List<Vector3Int> neighborDirectionList = straightFlag ? neighborsFourDirectionsIncrement : neighborsDiagFourDirectionsIncrement;
                for (int j = 0; j < neighborDirectionList.Count; j++)
                {
					if (roadTileDict.ContainsKey(neighbor + neighborDirectionList[j]))
						neighborRoads[j] = 1;
				}

				neighbors.Add((neighbor, straightFlag, neighborRoads));
				if (straightFlag)
					straightRoads[i / 2] = 1;
				else
					diagRoads[i / 2] = 1;
			}
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

    public void BuildRoadWithObject(Vector3Int pos, bool improvement)
    {
		if (IsRoadOnTerrain(pos))
        {
            if (improvement)
                cityImprovementDict[pos].hadRoad = true;
        }
        else
		{
			int level = 1;
			foreach (Vector3Int loc in GetNeighborsFor(pos, MapWorld.State.EIGHTWAYINCREMENT))
			{
				if (IsRoadOnTerrain(loc))
					level = Mathf.Max(level, GetRoadLevel(loc));
			}

			roadManager.BuildRoadAtPosition(pos, UtilityType.Road, level);
		}
	}

    public bool IsTileOpenCheck(Vector3Int tile)
    {
        if (IsBuildLocationTaken(tile) || IsRoadOnTerrain(tile) || IsWorkerWorkingAtTile(tile))
            return false;
        else
            return true;
    }

    public bool IsTileOpenButRoadCheck(Vector3Int tile)
    {
        if (IsBuildLocationTaken(tile) || IsWorkerWorkingAtTile(tile))
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
        
        vInt.y = v.y < 3 ? 0 : 3;
        vInt.x = (int)Math.Round(v.x, MidpointRounding.AwayFromZero);
        vInt.z = (int)Math.Round(v.z, MidpointRounding.AwayFromZero);

        return vInt;
    }

    public void AddStop(Vector3Int stopLoc, ITradeStop stop)
    {
        tradeStopDict[stopLoc] = stop;

        TraderStallManager stalls = new();
        stalls.SetUpStallLocs(stopLoc);
        traderStallDict[stopLoc] = stalls;
    }

    public void AddStopName(string stopName, ITradeStop stop)
    {
        tradeStopNameDict[stopName] = stop;
    }

    public void AddStructure(Vector3Int position, GameObject structure) //method to add building to dict
    {
        //if (buildingPosDict.ContainsKey(position))
        //{
        //    Debug.LogError($"There is a structure already at this position {position}");
        //    return;
        //}

        buildingPosDict[position] = structure;
    }

    public void AddTradeCenterName(GameObject nameMap)
    {
        cityNamesMaps.Add(nameMap);
    }

    public void AddCity(Vector3 buildPosition, City city)
    {
        Vector3Int position = RoundToInt(buildPosition);
        cityDict[position] = city;
        cityNamesMaps.Add(city.cityNameMap);
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

    public void AddResourceProducer(Vector3Int buildPosition, ResourceProducer resourceProducer)
    {
        cityImprovementDict[buildPosition].resourceProducer = resourceProducer;
    }

    public void AddCityBuildingDict(Vector3 cityPos)
    {
        Vector3Int cityTile = RoundToInt(cityPos);
        cityBuildingDict[cityTile] = new Dictionary<string, CityImprovement>();
    }

    public void RemoveStop(Vector3Int stopLoc)
    {
        tradeStopDict.Remove(stopLoc);
        traderStallDict.Remove(stopLoc);
    }

    public void RemoveStopName(string stopName)
    {
        tradeStopNameDict.Remove(stopName);
    }

    public void UpdateStopName(string prevName, string newName)
    {
        tradeStopNameDict[newName] = tradeStopNameDict[prevName];
		tradeStopNameDict.Remove(prevName);
    }

    public void RemoveCityNameMap(Vector3Int cityLoc)
    {
        cityNamesMaps.Remove(GetCity(cityLoc).cityNameMap);
    }

    public void RemoveStructure(Vector3Int buildPosition)
    {
        buildingPosDict.Remove(buildPosition);
        if (cityImprovementDict.ContainsKey(buildPosition))
        {
            cityImprovementDict.Remove(buildPosition);
        }
        if (cityBuildingDict.ContainsKey(buildPosition)) //if destroying city, destroy all buildings within
        {
            bool isHill = GetTerrainDataAt(buildPosition).isHill;
            foreach (string building in cityBuildingDict[buildPosition].Keys)
            {
                CityImprovement improvement = cityBuildingDict[buildPosition][building];
                improvement.PlayRemoveEffect(isHill);
                Destroy(cityBuildingDict[buildPosition][building].gameObject);
            }
            
            cityBuildingDict.Remove(buildPosition);
            cityDict.Remove(buildPosition);
        }
    }

    public void RemoveRoad(Vector3Int buildPosition)
    {
        roadTileDict.Remove(buildPosition);
    }

    public Vector3Int AddPlayerPosition(Vector3 unitPosition, Unit unit)
    {
		Vector3Int position = RoundToInt(unitPosition);
		playerPosDict[position] = unit;

		return position;
	}

    public Vector3Int AddTraderPosition(Vector3Int pos, Trader trader)
    {
        if (traderPosDict.ContainsKey(pos))
        {
            if (!traderPosDict[pos].Contains(trader))
                traderPosDict[pos].Add(trader);
        }
        else
        {
            traderPosDict[pos] = new() { trader };
        }

        return pos;
    }

    public Vector3Int AddUnitPosition(Vector3 unitPosition, Unit unit)
    {
        unit.posSet = true;
        Vector3Int position = RoundToInt(unitPosition);
        unitPosDict[position] = unit;

        return position;
    }

    public void RemovePlayerPosition(Vector3Int pos)
    {
        playerPosDict.Remove(pos);
    }

    public void RemoveTraderPosition(Vector3Int pos, Trader trader)
    {
        if (traderPosDict.ContainsKey(pos))
        {
            traderPosDict[pos].Remove(trader);

            if (traderPosDict[pos].Count == 0)
                traderPosDict.Remove(pos);
        }
    }

    public void RemoveUnitPosition(Vector3Int position/*, GameObject unitGO*/)
    {
		if (unitPosDict.ContainsKey(position))
        {
            unitPosDict[position].posSet = false;
		    unitPosDict.Remove(position);
        }
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

    private List<Vector3Int> GetStallLocs(/*Vector3Int pos*/)
    {
		List<Vector3Int> stallLocs = new() { new Vector3Int(1,0,1), new Vector3Int(1,0,-1), new Vector3Int(-1,0,-1), new Vector3Int(-1,0,1) };

        return stallLocs;
	}

    public Vector3Int GetTraderBuildLoc(Vector3Int pos)
    {
        List<Vector3Int> stallLocs = GetStallLocs(/*pos*/);
        int[] stallCount = new int[stallLocs.Count];

        for (int i = 0; i < stallLocs.Count; i++)
        {
            Vector3Int stall = stallLocs[i] + pos;
            if (!traderPosDict.ContainsKey(stall))
                return stall;
            else
                stallCount[i] = traderPosDict[stall].Count;
        }

        //in case none are get the one with the fewest amount
        int min = stallCount.Min();
		int index = Array.FindIndex(stallCount, x => x == min);
        return stallLocs[index] + pos;
    }

    public bool IsSpotAvailable(Vector3Int pos)
    {
		List<Vector3Int> stallLocs = GetStallLocs(/*pos*/);

		for (int i = 0; i < stallLocs.Count; i++)
		{
			Vector3Int stall = stallLocs[i] + pos;
			if (!traderPosDict.ContainsKey(stall))
				return true;
		}

        return false;
	}

    //for assigning labor
    //public void AddToCurrentFieldLabor(Vector3Int pos, int current)
    //{
    //    currentWorkedTileDict[pos] = current;
    //}

    //public void AddToMaxLaborDict(Vector3 pos, int max) //only adding to max labor when improvements are built, hence Vector3
    //{
    //    Vector3Int posInt = RoundToInt(pos);
    //    maxWorkedTileDict[posInt] = max;
    //}

    public void AddToCityLabor(Vector3Int pos, Vector3Int? cityLoc)
    {
        cityWorkedTileDict[pos] = cityLoc;
    }

    public int GetCurrentLaborForTile(Vector3Int pos)
    {
        if (CheckIfTileIsWorked(pos))
            return cityImprovementDict[pos].resourceProducer.currentLabor;
        return 0;
    }

    public int GetMaxLaborForTile(Vector3Int pos)
    {
        return cityImprovementDict[pos].GetImprovementData.maxLabor;
    }

    //public bool CheckImprovementIsProducer(Vector3Int pos)
    //{
    //    return cityImprovementDict[pos].resourceProducer != null;
    //}

    private Vector3Int? GetCityLaborForTile(Vector3Int pos)
    {
        return cityWorkedTileDict[pos];
    }

    public bool CheckIfCityOwnsTile(Vector3Int pos)
    {
        return cityWorkedTileDict.ContainsKey(pos);
    }

    public bool CheckIfTileIsWorked(Vector3Int pos)
    {
        return cityImprovementDict.ContainsKey(pos) && cityImprovementDict[pos].resourceProducer.currentLabor > 0; //every non-city center improvement is a producer
    }

    public bool CompletedImprovementCheck(Vector3Int pos)
    {
        return cityImprovementDict.ContainsKey(pos) && !cityImprovementDict[pos].isConstruction;
    }

    public bool CheckIfTileIsMaxxed(Vector3Int pos)
    {
        if (CheckIfTileIsWorked(pos))
            return cityImprovementDict[pos].GetImprovementData.maxLabor == cityImprovementDict[pos].resourceProducer.currentLabor;
        return false;
    }

    public string PrepareLaborNumbers(Vector3Int pos)
    {
        return GetCurrentLaborForTile(pos) + "/" + GetMaxLaborForTile(pos);
    }

    public void RemoveTerrain(Vector3Int tile)
    {
        world.Remove(tile);
    }

    //public void RemoveFromCurrentWorked(Vector3Int pos)
    //{
    //    currentWorkedTileDict.Remove(pos);
    //}

    //public void RemoveFromMaxWorked(Vector3Int pos) //only removing when improvements are destroyed
    //{
    //    maxWorkedTileDict.Remove(pos);
    //}

    public void RemoveFromCityLabor(Vector3Int pos)
    {
        if (cityImprovementDict[pos].GetImprovementData.singleBuildType != SingleBuildType.None)
            return;
        
        //if (cityWorkedTileDict.ContainsKey(pos))
        cityWorkedTileDict.Remove(pos);
    }

    public void RemoveSingleBuildFromCityLabor(Vector3Int pos)
    {
        //if (cityWorkedTileDict.ContainsKey(pos))
        cityWorkedTileDict.Remove(pos);
    }

    public void CityCanvasCheck()
    {
        bool turnOff = true;

        if (cityBuilderManager.uiCityTabs.activeStatus)
            turnOff = false;

		if (turnOff)
			cityCanvas.gameObject.SetActive(false);
	}

    public void GoToNext()
    {
        if (unitMovement.selectedUnit && unitMovement.selectedUnit.trader)
        {
            int indexOf = traderList.IndexOf(unitMovement.selectedUnit.trader);
            unitMovement.ClearSelection();
            int nextIndex = indexOf + 1;

            if (nextIndex == traderList.Count)
                nextIndex = 0;
            
            unitMovement.PrepareMovement(traderList[nextIndex], true);
        }
        else
        {
			int indexOf = laborerList.IndexOf(unitMovement.selectedUnit.laborer);
			unitMovement.ClearSelection();
            int nextIndex = indexOf + 1;

            if (nextIndex == laborerList.Count)
                nextIndex = 0;	
			
			unitMovement.PrepareMovement(laborerList[nextIndex], true);
		}
    }

    public float GetResourceTypeBonus(ResourceType type)
    {
        if (resourceYieldChangeDict.ContainsKey(type))
            return resourceYieldChangeDict[type];
        else
            return 0;
    }

    // doing this all manually with strings for now
    public void AddToCityPermanentChanges(string propertyName, float changeValue)
    {
        switch (propertyName)
        {
            case "Work Ethic":
                cityWorkEthicChange += changeValue;

                if (cityBuilderManager.uiCityTabs.activeStatus && cityBuilderManager.uiCityTabs.builderUI != null)
                    cityBuilderManager.uiCityTabs.builderUI.UpdateProducedNumbers(cityBuilderManager.SelectedCity.resourceManager);
                else if (uiCityImprovementTip.activeStatus)
                    uiCityImprovementTip.UpdateProduceNumbers();
				break;
            case "Warehouse Storage":
                cityWarehouseStorageChange += (int)changeValue;
                break;
        }
    }

    public void RemoveFromCityPermanentChanges(string propertyName, float changeValue)
    {
		switch (propertyName)
		{
			case "Work Ethic":
				cityWorkEthicChange -= changeValue;

				if (cityBuilderManager.uiCityTabs.activeStatus && cityBuilderManager.uiCityTabs.builderUI != null)
					cityBuilderManager.uiCityTabs.builderUI.UpdateProducedNumbers(cityBuilderManager.SelectedCity.resourceManager);
				else if (uiCityImprovementTip.activeStatus)
					uiCityImprovementTip.UpdateProduceNumbers();
				break;
			case "Warehouse Storage":
				cityWarehouseStorageChange -= (int)changeValue;
				break;
		}
	}

    public void AddToUnitPermanentChanges(string unitName, string propertyName, float changeValue)
    {
        switch (propertyName)
        {
            case "Movement Speed":
                if (!unitsSpeedChangeDict.ContainsKey(unitName))
                    unitsSpeedChangeDict[unitName] = 0;

				unitsSpeedChangeDict[unitName] += changeValue;
                break;
        }
    }

    public void RemoveFromUnitPermanentChanges(string unitName, string propertyName, float changeValue)
    {
		switch (propertyName)
		{
			case "Movement Speed":
				unitsSpeedChangeDict[unitName] -= changeValue;

				if (unitsSpeedChangeDict[unitName] == 0)
					unitsSpeedChangeDict.Remove(unitName);
				break;
		}
	}

    public void AddToCityImprovementChanges(string propertyName, ResourceType type, float changeValue)
    {
        switch (propertyName)
        {
            case "Production Yield":
                if (!resourceYieldChangeDict.ContainsKey(type))
                    resourceYieldChangeDict[type] = 0;

				resourceYieldChangeDict[type] += changeValue;

				if (cityBuilderManager.uiCityTabs.activeStatus && cityBuilderManager.uiCityTabs.builderUI != null)
					cityBuilderManager.uiCityTabs.builderUI.UpdateProducedNumbers(cityBuilderManager.SelectedCity.resourceManager);
				else if (uiCityImprovementTip.activeStatus)
					uiCityImprovementTip.UpdateProduceNumbers();
				break;
        }
    }

    public void RemoveFromCityImprovementChanges(string propertyName, ResourceType type, float changeValue)
    {
		switch (propertyName)
		{
			case "Production Yield":
				resourceYieldChangeDict[type] -= changeValue;

                if (resourceYieldChangeDict[type] == 0)
                    resourceYieldChangeDict.Remove(type);

				if (cityBuilderManager.uiCityTabs.activeStatus && cityBuilderManager.uiCityTabs.builderUI != null)
					cityBuilderManager.uiCityTabs.builderUI.UpdateProducedNumbers(cityBuilderManager.SelectedCity.resourceManager);
				else if (uiCityImprovementTip.activeStatus)
					uiCityImprovementTip.UpdateProduceNumbers();
				break;
		}
	}

    public void CheckCityPermanentChanges(City city)
    {
        city.workEthic += cityWorkEthicChange;
        city.wonderWorkEthic += cityWorkEthicChange;
        city.warehouseStorageLimit += cityWarehouseStorageChange;
    }

    public void CheckUnitPermanentChanges(Unit unit)
    {
        if (unitsSpeedChangeDict.ContainsKey(unit.buildDataSO.unitType.ToString()))
            unit.originalMoveSpeed *= 1f + unitsSpeedChangeDict[unit.buildDataSO.unitType.ToString()];
    }

    public void CheckCityImprovementPermanentChanges(CityImprovement improvement)
    {
		 
    }

    public void TurnOnCenterBorders(Vector3Int loc)
    {
		if (centerBordersDict.ContainsKey(loc))
		{
			foreach (GameObject border in centerBordersDict[loc])
				border.SetActive(true);
		}
	}

    public void TurnOnEnemyBorders(Vector3Int loc)
    {
        if (enemyBordersDict.ContainsKey(loc))
        {
            foreach (GameObject border in enemyBordersDict[loc])
                border.SetActive(true);
        }
    }

    public void DestroyBorders(Vector3Int loc)
    {
        if (enemyBordersDict.ContainsKey(loc))
        {
            foreach (GameObject border in enemyBordersDict[loc])
                Destroy(border);

            enemyBordersDict[loc].Clear();
            enemyBordersDict.Remove(loc);
        }
    }

	private void SetResourceMinimapIcon(TerrainData td)
    {
		GameObject resourceIconGO = Instantiate(Resources.Load<GameObject>("Prefabs/InGameSpritePrefabs/ResourceMinimapIcon"), new Vector3(0, 1.5f, -0.75f) + td.TileCoordinates, Quaternion.Euler(90, 0, 0));
		resourceIconGO.transform.SetParent(terrainHolder, false);
		ResourceMinimapIcon resourceIcon = resourceIconGO.GetComponent<ResourceMinimapIcon>();
		resourceIcon.resourceIconSprite.sprite = ResourceHolder.Instance.GetIcon(td.resourceType);
		resourceIconDict[td.TileCoordinates] = resourceIcon;
		resourceIconGO.SetActive(false);
	}

	public void ToggleResourceIcon(Vector3Int loc, bool v)
    {
        resourceIconDict[loc].gameObject.SetActive(v);
    }

	public void HighlightResourceIcon(Vector3Int loc, Sprite sprite)
	{
        resourceIconDict[loc].resourceBackgroundSprite.sprite = sprite;
	}

	public void RestoreResourceIcon(Vector3Int loc, Sprite sprite)
	{
		resourceIconDict[loc].resourceBackgroundSprite.sprite = sprite;
	}

    public void RemoveResourceIcon(Vector3Int loc)
    {
        Destroy(resourceIconDict[loc].gameObject);
        resourceIconDict.Remove(loc);
    }

	public bool CameraLocCheck()
    {
        TerrainData td = GetTerrainDataAt(RoundToInt(cameraController.transform.position));

        if (td == null)
            return false;

		if (td.terrainData.type == TerrainType.Sea)
            return false;
        else
            return true;
    }

    public bool DayTimeCheck()
    {
        if (dayNightCycle.day)
            return true;
        else
            return false;
    }

    public string GetNextCityName()
    {
        string cityName = "";
        bool searching = true;
        string prefix = "";
        int totalCount = 0;

        while (searching)
        {
            cityName = prefix + cityNamePool[UnityEngine.Random.Range(0, cityNamePool.Count)];
            
            if (!IsCityNameTaken(cityName))
                searching = false;

            totalCount++;

            if (totalCount == cityNamePool.Count)
            {
                totalCount = 0;
                prefix += "New ";
            }
        }

        return cityName;
    }

    public void ToggleGiftGiving(TradeRep tradeRep)
    {
        uiResourceGivingPanel.ToggleVisibility(true, false, false, tradeRep);
		unitMovement.uiPersonalResourceInfoPanel.SetPosition(false, null);
	}

    public void StatsCheck(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.Food:
                food += amount;
                break;

            //case ResourceType.Fish:
            //    food += amount;
            //    break;

            case ResourceType.Lumber:
                lumber += amount;
                break;
        }
    }

    public void ButtonFlashCheck()
    {
        if (flashingButton)
        {
			flashingButton = false;
            buttonHighlight.StopFlash();
		}
    }

    public void GameCheck(string source)
    {
        switch (gameStep)
        {
            case "first_labor":
				if (source != "Resource")
					return;

				if (food == 5)
				{
					City city = null;
					foreach (Vector3Int tile in GetNeighborsFor(GetClosestTerrainLoc(mainPlayer.currentLocation), MapWorld.State.CITYRADIUS))
					{
						if (IsCityOnTile(tile))
						{
							city = GetCity(tile);
							break;
						}
					}

					Vector3Int scottLoc = RoundToInt(mainPlayer.transform.position);
					if (city != null)
					{
						scottLoc = city.cityLoc;
					}
					else
					{
						foreach (Vector3Int loc in GetNeighborsFor(RoundToInt(mainPlayer.transform.position), State.EIGHTWAY))
						{
							if (PlayerCheckIfPositionIsValid(loc))
							{
								scottLoc = loc;
								break;
							}
						}
					}

                    Vector3 scottPos = scottLoc;
                    if (GetTerrainDataAt(RoundToInt(scottPos)).isHill)
                        scottPos.y += 1f;
					scott.transform.position = scottPos;
					scott.transform.rotation = Quaternion.Euler(0, 180, 0);

					Vector3 goScale = scott.transform.localScale;
					//AddUnitPosition(scottLoc, scott);
                    SetNPCLoc(scottLoc, scott);
                    scott.currentLocation = scottLoc;
					scott.gameObject.SetActive(true);
					float scaleX = goScale.x;
					float scaleZ = goScale.z;
					scott.transform.localScale = new Vector3(scaleX, 0.1f, scaleZ);
                    scott.PlayLightBeam();
					//scott.lightBeam.Play();
					LeanTween.scale(scott.gameObject, goScale, 0.5f).setEase(LeanTweenType.easeOutBack);
					cityBuilderManager.PlaySelectAudio(cityBuilderManager.buildClip);
					scott.conversationHaver.SetSomethingToSay("first_labor");
                    gameStep = "first_infantry";

                    if (tutorial)
                    {
                        ButtonFlashCheck();
    					StartCoroutine(WaitASecToSpeak(mainPlayer, 2, "tutorial3a"));
                    }
				}
				break;
            case "first_infantry":
                if (source != "Agriculture Research Complete")
                    return;

				City azaiCity = null;
				
                foreach (City city in cityDict.Values)
                {
                    azaiCity = city;
                    break;
                }

				Vector3Int azaiLoc = RoundToInt(mainPlayer.transform.position);
				if (azaiCity != null)
				{
					azaiLoc = azaiCity.cityLoc;

                    if ((!mainPlayer.isMoving && mainPlayer.currentLocation == azaiLoc) || (!scott.isMoving && scott.currentLocation == azaiLoc))
                    {
                        HashSet<Vector3Int> usedLocs = new();

                        if (!mainPlayer.isMoving)
                            usedLocs.Add(mainPlayer.currentLocation);
                        if (!scott.isMoving)
                            usedLocs.Add(scott.currentLocation);
                        
                        foreach (Vector3Int loc in GetNeighborsFor(azaiLoc, State.EIGHTWAY))
                        {
                            if (!usedLocs.Contains(loc))
                            {
                                azaiLoc = loc;
                                break;
                            }
                        }
                    }
				}
				else
				{
					foreach (Vector3Int loc in GetNeighborsFor(RoundToInt(mainPlayer.transform.position), State.EIGHTWAY))
					{
						if (PlayerCheckIfPositionIsValid(loc))
						{
							azaiLoc = loc;
							break;
						}
					}
				}

				Vector3 azaiPos = azaiLoc;
				if (GetTerrainDataAt(RoundToInt(azaiPos)).isHill)
					azaiPos.y += 1f;
				azai.transform.position = azaiPos;
                azai.transform.rotation = Quaternion.Euler(0, 180, 0);

				Vector3 azaiScale = azai.transform.localScale;
                azai.currentLocation = azaiLoc;
				//AddUnitPosition(azaiLoc, azai);
				SetNPCLoc(azaiLoc, azai);
				azai.gameObject.SetActive(true);
				float azaiScaleX = azaiScale.x;
				float azaiScaleZ = azaiScale.z;
				azai.transform.localScale = new Vector3(azaiScaleX, 0.1f, azaiScaleZ);
                azai.PlayLightBeam();
				//azai.lightBeam.Play();
				LeanTween.scale(azai.gameObject, azaiScale, 0.5f).setEase(LeanTweenType.easeOutBack);
				cityBuilderManager.PlaySelectAudio(cityBuilderManager.buildClip);
				azai.conversationHaver.SetSomethingToSay("first_infantry");
				gameStep = "";

				break;
        }
    }

    //if going through tutorial, goes through the steps of it 
    public void TutorialCheck(string source)
    {
        //only check if tutorial is currently on going
        if (tutorial) 
        {
            switch (tutorialStep)
            {
                case "just_landed":
                    if (source != "Build City")
                        return;

					mainPlayer.conversationHaver.SetSomethingToSay("tutorial1");
					tutorialStep = "tutorial1";
					break;
                case "tutorial1":
                    if (source != "Finished Movement")
                        return;

					Vector3Int playerLoc = GetClosestTerrainLoc(mainPlayer.transform.position);
					if (GetTerrainDataAt(playerLoc).resourceType != ResourceType.Food)
						return;

					bool foundCity = false;
					foreach (Vector3Int loc in GetNeighborsFor(playerLoc, State.CITYRADIUS))
					{
						if (IsCityOnTile(loc))
						{
							GetTerrainDataAt(playerLoc).DisableHighlight();
							foundCity = true;
							break;
						}
					}

					if (!foundCity)
						return;

					ButtonFlashCheck();
					tutorialStep = "tutorial2";
                    mainPlayer.conversationHaver.SetSomethingToSay("tutorial2");
					break;
                case "tutorial2":
                    if (source != "Resource")
                        return;

                    if (food == 1)
                    {
                        ButtonFlashCheck();
                        mainPlayer.conversationHaver.SetSomethingToSay("tutorial3");
                    }
                    else if (food > 1 && food < 5)
                    {
                        if (!mainPlayer.isBusy)
                        {
                            StartCoroutine(EnableButtonHighlight(unitMovement.uiWorkerTask.GetButton("Gather").transform, true));
                            unitMovement.uiWorkerTask.GetButton("Gather").isFlashing = true;
                        }
                    }
                    break;
                case "first_labor":
                    if (source != "Finished Movement")
                        return;

                    Vector3Int playerLoc2 = GetClosestTerrainLoc(mainPlayer.transform.position);
                    if (GetTerrainDataAt(playerLoc2).resourceType != ResourceType.Lumber)
                        return;

                    bool foundCity2 = false;
                    foreach (Vector3Int loc in GetNeighborsFor(playerLoc2, State.CITYRADIUS))
                    {
                        if (IsCityOnTile(loc))
                        {
                            GetTerrainDataAt(playerLoc2).DisableHighlight();
                            foundCity2 = true;
                            break;
                        }
                    }

                    if (!foundCity2)
                        return;

                    tutorialStep = "tutorial4";
                    mainPlayer.conversationHaver.SetSomethingToSay("tutorial4");
                    break;
                case "tutorial4":
                    if (source != "Resource")
                        return;

					bool enoughLumber = false;
					foreach (City city in cityDict.Values)
					{
						if (city.resourceManager.resourceDict[ResourceType.Lumber] >= 5)
						{
							enoughLumber = true;
							break;
						}
					}

					if (enoughLumber)
                    {
						unitMovement.uiWorkerTask.GetButton("Gather").isFlashing = false;
					}
                    else
					{
						if (!mainPlayer.isBusy)
                        {
                            StartCoroutine(EnableButtonHighlight(unitMovement.uiWorkerTask.GetButton("Gather").transform, true));
						    unitMovement.uiWorkerTask.GetButton("Gather").isFlashing = true;
                        }
						return;
					}

                    ButtonFlashCheck();
                    mainPlayer.conversationHaver.SetSomethingToSay("tutorial5");
                    tutorialStep = "tutorial5";

					break;
                case "tutorial5":
					if (source != "Open City")
						return;

					StartCoroutine(EnableButtonHighlight(cityBuilderManager.uiCityTabs.GetTab("Buildings").transform, true));
					cityBuilderManager.uiCityTabs.GetTab("Buildings").isFlashing = true;
					tutorialStep = "tutorial5a";

					foreach (Vector3Int tile in cityDict.Keys)
					{
						if (!cityDict[tile].activeCity)
							cityDict[tile].Deselect();
					}

					break;
                case "tutorial5a":
                    if (source != "Open Buildings Tab")
                        return;

                    for (int i = 0; i < cityBuilderManager.uiBuildingBuilder.buildOptions.Count; i++)
                    {
                        if (cityBuilderManager.uiBuildingBuilder.buildOptions[i].BuildData.improvementNameAndLevel == "Housing-1")
                        {
    						StartCoroutine(EnableButtonHighlight(cityBuilderManager.uiBuildingBuilder.buildOptions[i].transform, true, true));
                            cityBuilderManager.uiBuildingBuilder.buildOptions[i].isFlashing = true;
                            break;
                        }
                    }

					tutorialStep = "tutorial5b";
					break;
                case "tutorial5b":
                    if (source != "Building Housing")
                        return;

                    tutorialStep = "tutorial6";
                    mainPlayer.conversationHaver.SetSomethingToSay("tutorial6");
					break;
                case "tutorial6":
                    if (source != "Open City")
                        return;

					StartCoroutine(EnableButtonHighlight(uiCityPopIncreasePanel.buttonImage.transform, true));
                    uiCityPopIncreasePanel.isFlashing = true;
                    tutorialStep = "tutorial6a";

					break;
                case "tutorial6a":
                    if (source != "Add Pop")
                        return;

                    cityBuilderManager.CenterCamOnCity();
                    cityBuilderManager.uiHelperWindow.SetMessage("This is the cycle countdown for every camp. Each pop consumes 1 food per cycle, but if no food is available, then pop will leave the camp.");
					cityBuilderManager.uiHelperWindow.SetPlacement(cityBuilderManager.uiResourceManager.originalLoc + new Vector3(0, -300, 0), cityBuilderManager.uiResourceManager.allContents.pivot);
                    cityBuilderManager.uiHelperWindow.ToggleVisibility(true, 2);
                    tutorialStep = "tutorial7";
                    mainPlayer.conversationHaver.SetSomethingToSay("tutorial7");
                    GameLoader.Instance.gameData.tutorialData.hadPopAdd = true;
					break;
                case "tutorial7":
                    if (source != "Finished Movement")
                        return;

					Vector3Int playerLoc3 = GetClosestTerrainLoc(mainPlayer.transform.position);
					if (GetTerrainDataAt(playerLoc3).resourceType != ResourceType.Stone)
						return;

					bool foundCity3 = false;
					foreach (Vector3Int loc in GetNeighborsFor(playerLoc3, State.CITYRADIUS))
					{
						if (IsCityOnTile(loc))
						{
							GetTerrainDataAt(playerLoc3).DisableHighlight();
							foundCity3 = true;
							break;
						}
					}

					if (!foundCity3)
						return;

					StartCoroutine(EnableButtonHighlight(unitMovement.uiWorkerTask.GetButton("Gather").transform, true));
					unitMovement.uiWorkerTask.GetButton("Gather").isFlashing = true;
					tutorialStep = "tutorial7a";

					break;
                case "tutorial7a":
                    if (source != "Resource")
                        return;

					bool enoughStone = false;
					foreach (City city in cityDict.Values)
					{
						if (city.resourceManager.resourceDict[ResourceType.Stone] >= 10)
						{
							enoughStone = true;
							break;
						}
					}

					if (enoughStone)
                    {
						unitMovement.uiWorkerTask.GetButton("Gather").isFlashing = false;
					}
                    else
					{
						StartCoroutine(EnableButtonHighlight(unitMovement.uiWorkerTask.GetButton("Gather").transform, true));
						unitMovement.uiWorkerTask.GetButton("Gather").isFlashing = true;
						return;
					}

                    ButtonFlashCheck();
					mainPlayer.conversationHaver.SetSomethingToSay("tutorial8", scott);
					tutorialStep = "tutorial8";

					break;
                case "tutorial8":
                    if (source != "Open City")
                        return;

					StartCoroutine(EnableButtonHighlight(cityBuilderManager.uiCityTabs.GetTab("Producers").transform, true));
					cityBuilderManager.uiCityTabs.GetTab("Producers").isFlashing = true;
					tutorialStep = "tutorial8a";

					break;
				case "tutorial8a":
					if (source != "Open Producers Tab")
						return;

					for (int i = 0; i < cityBuilderManager.uiProducerBuilder.buildOptions.Count; i++)
					{
						if (cityBuilderManager.uiProducerBuilder.buildOptions[i].BuildData.improvementNameAndLevel == "Research-1")
						{
							StartCoroutine(EnableButtonHighlight(cityBuilderManager.uiProducerBuilder.buildOptions[i].transform, true, true));
							cityBuilderManager.uiProducerBuilder.buildOptions[i].isFlashing = true;
							break;
						}
					}

					tutorialStep = "tutorial8b";
					break;
				case "tutorial8b":
					if (source != "Build Something")
						return;

					cityBuilderManager.CenterCamOnCity();
					cityBuilderManager.uiHelperWindow.SetMessage("Resources held in camp will be shown here. The order of resources shown can be rearranged by clicking and dragging the resource.");
					cityBuilderManager.uiHelperWindow.SetPlacement(cityBuilderManager.uiResourceManager.originalLoc + new Vector3(0, -230, 0), cityBuilderManager.uiResourceManager.allContents.pivot);
					cityBuilderManager.uiHelperWindow.ToggleVisibility(true, 0);

                    tutorialStep = "tutorial8c";
					break;
                case "tutorial8c":
                    if (source != "Finished Building Research")
                        return;

                    mainPlayer.conversationHaver.SetSomethingToSay("tutorial9", scott);
                    tutorialStep = "tutorial9";
                    break;
                case "tutorial9":
                    if (source != "Open City")
                        return;

					cityBuilderManager.uiHelperWindow.SetMessage("Whenever pop is added to a camp and there is somewhere for that pop to work, you can assign the labor using these buttons.");
					cityBuilderManager.uiHelperWindow.SetPlacement(cityBuilderManager.uiLaborAssignment.originalLoc + new Vector3(200, 0, 0), cityBuilderManager.uiLaborAssignment.allContents.pivot);
					cityBuilderManager.uiHelperWindow.ToggleVisibility(true, 3);

                    tutorialStep = "tutorial9a";
					break;
                case "tutorial9a":
                    if (source != "Change Labor")
                        return;

                    tutorialStep = "tutorial10";
                    mainPlayer.conversationHaver.SetSomethingToSay("tutorial10", scott);
                    break;
                case "tutorial10":
                    if (source != "Research")
                        return;

					tutorialStep = "tutorialAny";
					mainPlayer.conversationHaver.SetSomethingToSay("tutorial11", scott);
					break;
                case "tutorialAny":
                    switch (source)
                    {
                        case "Resource":
                            if (GameLoader.Instance.gameData.tutorialData.gathered)
                                return;

                            mainPlayer.conversationHaver.SetSomethingToSay("last_resource");
                            GameLoader.Instance.gameData.tutorialData.gathered = true;
							break;
                        case "Agriculture Research Complete":
                            mainPlayer.conversationHaver.SetSomethingToSay("agriculture", scott);

							StartCoroutine(EnableButtonHighlight(cityBuilderManager.uiCityTabs.GetTab("Raw Goods").transform, true));
							cityBuilderManager.uiCityTabs.GetTab("Raw Goods").isFlashing = true;
							break;
                        case "Pottery Research Complete":
							mainPlayer.conversationHaver.SetSomethingToSay("pottery", scott);

							StartCoroutine(EnableButtonHighlight(cityBuilderManager.uiCityTabs.GetTab("Sell").transform, true));
							cityBuilderManager.uiCityTabs.GetTab("Sell").isFlashing = true;
							break;
                        case "Hunting Research Complete":
							mainPlayer.conversationHaver.SetSomethingToSay("hunting", azai);
							break;
                        case "Trade Research Complete":
							mainPlayer.conversationHaver.SetSomethingToSay("trade", scott);
                            break;
                        case "Building Housing":
							if (GameLoader.Instance.gameData.tutorialData.built2ndHouse)
								return;

							StartCoroutine(EnableButtonHighlight(uiCityPopIncreasePanel.buttonImage.transform, true));
							uiCityPopIncreasePanel.isFlashing = true;

                            GameLoader.Instance.gameData.tutorialData.built2ndHouse = true;
							break;
                        case "Finished Building Farm":
                            if (GameLoader.Instance.gameData.tutorialData.builtFarm)
                                return;

							StartCoroutine(EnableButtonHighlight(cityBuilderManager.uiLaborAssignment.GetLaborButton(1).transform, true));
							cityBuilderManager.uiLaborAssignment.GetLaborButton(1).isFlashing = true;
							mainPlayer.conversationHaver.SetSomethingToSay("first_farm", scott);
							GameLoader.Instance.gameData.tutorialData.builtFarm = true;
							break;
                        case "Finished Building Infantry":
                            if (GameLoader.Instance.gameData.tutorialData.builtInfantry)
                                return;

							mainPlayer.conversationHaver.SetSomethingToSay("first_built_infantry", azai);
                            GameLoader.Instance.gameData.tutorialData.builtInfantry = true;
							break;
                        case "Finished Building Trader":
                            if (GameLoader.Instance.gameData.tutorialData.builtTrader)
                                return;

							StartCoroutine(EnableButtonHighlight(unitMovement.uiTraderPanel.GetButtonTransform(), true));
							mainPlayer.conversationHaver.SetSomethingToSay("first_trader", scott);
                            GameLoader.Instance.gameData.tutorialData.builtTrader = true;
							break;
                        case "First Ambush":
							mainPlayer.conversationHaver.SetSomethingToSay("first_ambush", azai);
                            GameLoader.Instance.gameData.tutorialData.hadAmbush = true;
							break;
                        case "First Pop Loss":
							mainPlayer.conversationHaver.SetSomethingToSay("first_pop_loss", scott);
                            GameLoader.Instance.gameData.tutorialData.hadPopLoss = true;
							break;
       //                 case "New City":
							//mainPlayer.conversationHaver.SetSomethingToSay("new_city", scott);
							//GameLoader.Instance.gameData.tutorialData.newCity = true;
       //                     break;
                        case "Finished Building Finance Center":
                            if (GameLoader.Instance.gameData.tutorialData.newFinance)
                                return;

							mainPlayer.conversationHaver.SetSomethingToSay("new_finance_center", scott);
                            GameLoader.Instance.gameData.tutorialData.newFinance = true;
							break;
                        case "Finished Building Entertainment Center":
							if (GameLoader.Instance.gameData.tutorialData.newEntertainment)
								return;

							mainPlayer.conversationHaver.SetSomethingToSay("new_entertainment_center", scott);
							GameLoader.Instance.gameData.tutorialData.newEntertainment = true;
							break;
						case "Finished Building Ceramist":
							if (GameLoader.Instance.gameData.tutorialData.newProducer)
								return;

							mainPlayer.conversationHaver.SetSomethingToSay("new_producer", scott);
							GameLoader.Instance.gameData.tutorialData.newProducer = true;
							break;
						case "Finished Building Weaver":
							if (GameLoader.Instance.gameData.tutorialData.newProducer)
								return;

							mainPlayer.conversationHaver.SetSomethingToSay("new_producer", scott);
							GameLoader.Instance.gameData.tutorialData.newProducer = true;
							break;
                        case "Build City":
                            if (GameLoader.Instance.gameData.tutorialData.nextCity)
                                return;

							mainPlayer.conversationHaver.SetSomethingToSay("next_city", scott);
                            GameLoader.Instance.gameData.tutorialData.nextCity = true;
							break;
					}

                    break;
			}
        }
    }

    private IEnumerator WaitASecToSpeak(Unit unit, int timeToWait, string conversationTopic)
    {
        playerInput.paused = true;
        yield return new WaitForSeconds(timeToWait);

		playerInput.paused = false;
		unit.conversationHaver.SetSomethingToSay(conversationTopic);
    }

    public IEnumerator EnableButtonHighlight(Transform selection, bool button, bool big = false)
    {
		ButtonFlashCheck();
		flashingButton = true;

        //wait til end of frame to make sure everything is active
		yield return new WaitForEndOfFrame();

        buttonHighlight.transform.SetParent(selection, false);
        buttonHighlight.PlayFlash(button, big);
	}

	//in case some actions need to take place during and immediately after conversation (step numbers must be manually entered,
	//immediately after is count of conversation items + 1)
	public void ConversationActionCheck(string topic, int number)
    {
        switch (topic)
        {
			case "just_landed":
                if (number == 3)
                {
				    if (tutorial)
				    {
                        //uiConversationTaskManager.CreateConversationTask("Tutorial");
                        tutorialStep = "just_landed";
						StartCoroutine(EnableButtonHighlight(unitMovement.uiWorkerTask.GetButton("Build").transform, true));
						unitMovement.uiWorkerTask.GetButton("Build").isFlashing = true;
                        playerInput.paused = true;
					}

                    gameStep = "first_labor";
                }
				break;

			case "tutorial1":
                if (number == 1)
                {
                    cameraController.someoneSpeaking = false;
                }
                else if (number == 2)
                {
					unitMovement.uiMoveUnit.ToggleVisibility(true);
					StartCoroutine(EnableButtonHighlight(unitMovement.uiMoveUnit.transform, false));

					//finding closest food
					foreach (Vector3Int tile in GetNeighborsFor(RoundToInt(mainPlayer.transform.position), State.CITYRADIUS))
					{
						TerrainData td = GetTerrainDataAt(tile);
						if (td.resourceType == ResourceType.Food)
						{
							td.EnableHighlight(Color.white);
							break;
						}
					}
				}
                break;
            case "tutorial2":
                if (number == 1)
                {
					StartCoroutine(EnableButtonHighlight(unitMovement.uiWorkerTask.GetButton("Gather").transform, true));
					unitMovement.uiWorkerTask.GetButton("Gather").isFlashing = true;
				}
                break;
            case "tutorial3":
                if (number == 1)
                {
					StartCoroutine(EnableButtonHighlight(unitMovement.uiWorkerTask.GetButton("Gather").transform, true));
					unitMovement.uiWorkerTask.GetButton("Gather").isFlashing = true;
				}
				break;
            case "first_labor":
				if (number == 0)
                {
                    if (tutorial)
                        mainPlayer.conversationHaver.SetSomethingToSay("tutorial3b", scott);
                }
                else if (number == 17)
				{
                    RemoveNPCLoc(scott.currentLocation);
                    mainPlayer.gameObject.name = "Koa & Co.";
                    scottFollow = true;
					scott.gameObject.tag = "Player";
                    //scott.marker.gameObject.tag = "Player";
					unitMovement.uiWorkerTask.ReactivateButtons();
					characterUnits.Add(scott);

                    if (mainPlayer.isSelected)
                        scott.Highlight(Color.white);

                    if (tutorial)
                    {
                        Vector3Int scottLoc = RoundToInt(scott.transform.position); 

                        City city = null;
					    if (IsCityOnTile(scottLoc))
                            city = GetCity(scottLoc);

					    if (city == null)
					    {
						    return;
					    }

					    foreach (Vector3Int tile in GetNeighborsFor(city.cityLoc, State.CITYRADIUS))
					    {
						    TerrainData td = GetTerrainDataAt(tile);
						    if (td.resourceType == ResourceType.Lumber)
						    {
							    td.EnableHighlight(Color.white);
							    break;
						    }
					    }

                        tutorialStep = "first_labor";
                    }
				}
				break;
            case "tutorial4":
                if (number == 3)
                {
					StartCoroutine(EnableButtonHighlight(unitMovement.uiWorkerTask.GetButton("Gather").transform, true));
					unitMovement.uiWorkerTask.GetButton("Gather").isFlashing = true;
				}
                break;
			case "tutorial5":
                if (number == 0)
                {
                    foreach (Vector3Int tile in cityDict.Keys)
                    {
                        cityDict[tile].Select(Color.green);
                    }
                }
                break;
            case "tutorial6":
                if (number == 3)
                {
					foreach (Vector3Int tile in cityDict.Keys)
					{
						cityDict[tile].Select(Color.green);
					}
				}
                break;
            case "tutorial7":
                if (number == 2)
                {
					Vector3Int scottLoc = RoundToInt(scott.transform.position);

					City city = null;
                    int dist = 0;

                    bool firstTime = true;
                    foreach (City city2 in cityDict.Values)
                    {
                        if (firstTime)
                        {
                            city = city2;
                            firstTime = false;
                            dist = Mathf.Abs(city2.cityLoc.x - scottLoc.x) + Mathf.Abs(city2.cityLoc.z - scottLoc.z);
                            continue;
                        }

                        int nextDist = Mathf.Abs(city2.cityLoc.x - scottLoc.x) + Mathf.Abs(city2.cityLoc.z - scottLoc.z);
						if (nextDist < dist)
                        {
                            dist = nextDist;
                            city = city2;
                        }
                    }

					if (city == null)
						return;

					foreach (Vector3Int tile in GetNeighborsFor(city.cityLoc, State.CITYRADIUS))
					{
						TerrainData td = GetTerrainDataAt(tile);
						if (td.resourceType == ResourceType.Stone)
						{
							td.EnableHighlight(Color.white);
							break;
						}
					}
				}
                break;
			case "tutorial8":
				if (number == 1)
				{
					foreach (Vector3Int tile in cityDict.Keys)
					{
						cityDict[tile].Select(Color.green);
					}
				}
				break;
            case "tutorial9":
                if (number == 1)
                {
					foreach (Vector3Int tile in cityDict.Keys)
					{
                        if (cityDict[tile].currentPop > 0)
                            cityDict[tile].Select(Color.green);
					}

					StartCoroutine(EnableButtonHighlight(cityBuilderManager.uiLaborAssignment.GetLaborButton(1).transform, true));
					cityBuilderManager.uiLaborAssignment.GetLaborButton(1).isFlashing = true;
				}
                break;
            case "tutorial10":
                if (number == 1)
                {
                    if (researching)
                    {
                        tutorialStep = "tutorial10";
                        TutorialCheck("Research");
                    }
                    else
                    {
                        StartCoroutine(EnableButtonHighlight(uiWorldResources.buttons[1].transform, false));
					    researchTree.isFlashing = true;
                    }
				}
                break;
            case "first_infantry":
                if (number == 11)
                {
                    RemoveNPCLoc(azai.currentLocation);
                    azaiFollow = true;
					azai.gameObject.tag = "Player";

					for (int i = 0; i < allTradeCenters.Count; i++)
					{
						if (allTradeCenters[i].isDiscovered)
							allTradeCenters[i].tcRep.SetSomethingToSay(allTradeCenters[i].tcRep.tradeRepName + "_intro");
					}

					foreach (Vector3Int loc in enemyCityDict.Keys)
					{
						if (enemyCityDict[loc].enemyCamp.revealed)
							enemyCityDict[loc].empire.enemyLeader.SetSomethingToSay(enemyCityDict[loc].empire.enemyLeader.leaderName + "_intro");
					}

                    //azai.marker.gameObject.tag = "Player";
					characterUnits.Add(azai);

                    if (mainPlayer.isSelected)
                        azai.Highlight(Color.white);
				}
				break;
            case "next_city":
                if (number == 2 && tutorial)
                {
					StartCoroutine(EnableButtonHighlight(unitMovement.uiWorkerTask.GetButton("LoadUnload").transform, true));
					unitMovement.uiWorkerTask.GetButton("LoadUnload").isFlashing = true;
				}
                break;
            case "Natakamani_quest0":
                if (number == 14 && tutorial)
                {
				    mainPlayer.conversationHaver.SetSomethingToSay("first_task");
					StartCoroutine(EnableButtonHighlight(unitMovement.uiWorkerTask.GetButton("LoadUnload").transform, true));
					unitMovement.uiWorkerTask.GetButton("LoadUnload").isFlashing = true;
				}
				break;
			case "Natakamani_quest1":
				if (number == 11 && tutorial)
				{
					mainPlayer.conversationHaver.SetSomethingToSay("toggle_sell");
					StartCoroutine(EnableButtonHighlight(cityBuilderManager.uiCityTabs.GetTab("Sell").transform, true));
					cityBuilderManager.uiCityTabs.GetTab("Sell").isFlashing = true;
				}
				break;
			case "Haniya_intro":
                mainPlayer.conversationHaver.SetSomethingToSay("Haniya_intro_coda");
                break;
			case "Haniya_quest0_complete":
				mainPlayer.conversationHaver.SetSomethingToSay("Haniya_quest0_complete_coda");
				break;
			case "Haniya_quest1_complete":
				mainPlayer.conversationHaver.SetSomethingToSay("Haniya_quest1_complete_coda");
				break;
			case "Haniya_quest2_complete":
				mainPlayer.conversationHaver.SetSomethingToSay("Haniya_quest2_complete_coda");
				break;
			case "Haniya_quest3_complete":
				mainPlayer.conversationHaver.SetSomethingToSay("Haniya_quest3_complete_coda");
				break;
		}
    }

    public void StartFlashingButton(Transform transform, bool button, bool big = false)
    {
		StartCoroutine(EnableButtonHighlight(transform, button, big));
	}
}


public enum Era
{
    AncientEra,
    ClassicEra,
    MedievalEra,
    RenaissanceEra,
    ModernEra,
    None
}

public enum Region
{
    North,
    South,
    East,
    West,
    None
}

public interface IGoldUpdateCheck
{
    void UpdateGold(int prevAmount, int currentAmount, bool pos);
}

public interface IGoldWaiter
{
    int goldNeeded { get; }
    Vector3Int waiterLoc { get; }

    bool RestartGold(int gold);
}

public interface ITradeStop
{
    string stopName { get; }
    Vector3Int mainLoc { get; }
    Dictionary<SingleBuildType, Vector3Int> singleBuildLocDict { get; }
    public List<Trader> waitList { get; }
    public List<Trader> seaWaitList { get; }
    public List<Trader> airWaitList { get; }
    City city { get; }
    Wonder wonder { get; }
    TradeCenter center { get; }

    public bool TraderAlreadyThere(Vector3Int loc, Trader trader, MapWorld world)
    {
        return world.traderPosDict.ContainsKey(loc) && world.traderPosDict[loc].Contains(trader);
    }

	public void InLineCheck(Trader trader, ITradeStop stop)
	{
		if (trader.bySea)
            stop.seaWaitList.Remove(trader);
		else if (trader.byAir)
            stop.airWaitList.Remove(trader);
        else
            stop.waitList.Remove(trader);
	}

    public bool IsTraderWaitingAtHomeCity(Vector3Int pos, Vector3Int dest, MapWorld world, Trader trader)
    {
		for (int i = 0; i < world.traderPosDict[pos].Count; i++)
		{
			if (world.traderPosDict[pos][i].followingRoute && world.traderPosDict[pos][i] != trader && world.traderPosDict[pos][i].tradeRouteManager.currentDestination == dest)
				return true;
		}

		return false;
	}

	public bool IsTraderWaitingAtSameStop(Vector3Int pos, Vector3Int dest, MapWorld world, Trader trader)
	{
        for (int i = 0; i < world.traderPosDict[pos].Count; i++)
        {
            if (world.traderPosDict[pos][i] != trader && world.traderPosDict[pos][i].followingRoute && world.traderPosDict[pos][i].tradeRouteManager.currentDestination == dest)
    			return true;
        }

		return false;
	}

	public void AddToWaitList(Trader trader, ITradeStop stop)
	{
		if (trader.bySea)
			AddCheck(stop.seaWaitList, trader);
		else if (trader.byAir)
			AddCheck(stop.airWaitList, trader);
		else
            AddCheck(stop.waitList, trader);
	}

    public void AddCheck(List<Trader> waitList, Trader trader)
    {
		if (!waitList.Contains(trader))
			waitList.Add(trader);
	}

	public void RemoveFromWaitList(Trader trader, ITradeStop stop)
	{
		if (trader.bySea)
			RemoveCheck(stop.seaWaitList, trader);
		else if (trader.byAir)
			RemoveCheck(stop.airWaitList, trader);
		else
            RemoveCheck(stop.waitList, trader);
	}

    void RemoveCheck(List<Trader> waitList, Trader trader)
    {
		int index = waitList.IndexOf(trader);
		    
        if (index >= 0) //if is in waitlist
        {
			waitList.RemoveAt(index);

			for (int i = index; i < waitList.Count; i++)
			{
                if (!waitList[i].movingUpInLine)
				    waitList[i].StartMoveUpInLine(i + 1);
			}
		}
        else
        {
			if (waitList.Count > 0)
			{
				Trader nextTrader = waitList[0];
				waitList.RemoveAt(0);
				nextTrader.ExitLine();
			}

            for (int i = 0; i < waitList.Count; i++)
            {
				if (!waitList[i].movingUpInLine)
                    waitList[i].StartMoveUpInLine(i + 1);

			}
		}
	}

    public List<Trader> GetWaitList(SingleBuildType type)
    {
        if (type == SingleBuildType.TradeDepot)
            return waitList;
        if (type == SingleBuildType.Harbor)
            return seaWaitList;
        if (type == SingleBuildType.Airport)
            return airWaitList;

        List<Trader> tempList = new();
        return tempList;
    }

	public void ClearStopCheck(List<Trader> waitList, Vector3Int stopLoc, MapWorld world)
	{
        List<Trader> tempWaitList = new(waitList);
        
        for (int i = 0; i < tempWaitList.Count; i++)
        {
            tempWaitList[i].isWaiting = false;
            waitList.Remove(tempWaitList[i]);
            tempWaitList[i].InterruptRoute(true);
        }

        waitList.Clear();

        List<Vector3Int> stallLocs = new(world.GetAllUsedStallLocs(stopLoc));
        for (int i = 0; i < stallLocs.Count; i++)
        {
            if (world.traderPosDict.ContainsKey(stallLocs[i]))
            {
                List<Trader> tempList = new(world.traderPosDict[stallLocs[i]]);
            
                for (int j = 0; j < tempList.Count; j++)
                    tempList[j].InterruptRoute(true);
            }
        }
	}

    public void ClearRestInLine(Trader trader, ITradeStop stop)
    {
        List<Trader> waitList;
        List<Trader> targetList;

        if (trader.bySea)
        {
            waitList = new(stop.seaWaitList);
            targetList = stop.seaWaitList;
        }
        else if (trader.byAir)
        {
            waitList = new(stop.airWaitList);
            targetList = stop.airWaitList;
        }
        else
        {
            waitList = new(stop.waitList);
            targetList = stop.waitList;
        }

        if (waitList.Contains(trader))
		{
			int index = waitList.IndexOf(trader);
            trader.isWaiting = false;
            targetList.Remove(trader);
			waitList.RemoveAt(index);

			for (int i = index; i < waitList.Count; i++)
			{
				if (!waitList[i].isDead && waitList[i].followingRoute)
				{
					waitList[i].isWaiting = false;
					targetList.Remove(waitList[i]);
					waitList[i].InterruptRoute(true);
				}
			}
		}
		else
		{
			if (waitList.Count > 0)
			{
                for (int i = 0; i < waitList.Count; i++)
                {
                    if (!waitList[i].isDead && waitList[i].followingRoute)
                    {
                        waitList[i].isWaiting = false;
                        targetList.Remove(waitList[i]);
                        waitList[i].InterruptRoute(true);
                    }
				}
			}
		}
	}
}

public interface ITooltip
{
    void CheckResource(City city, int amount, ResourceType type);
}

public interface IImmoveable
{

}