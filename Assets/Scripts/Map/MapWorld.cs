using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using TMPro;
using Unity.Burst.Intrinsics;
using Unity.Mathematics;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.UI.CanvasScaler;
//using static UnityEngine.RuleTile.TilingRuleOutput;

public class MapWorld : MonoBehaviour
{
    private string version = "0.1";
    private DateTime currentTime;
    [HideInInspector]
    public Era currentEra = Era.AncientEra;
    [HideInInspector]
    public Region startingRegion = Region.South;
    [SerializeField]
    public HandlePlayerInput playerInput;
    [SerializeField]
    public Worker mainPlayer, scott, azai;
    //[SerializeField]
    //public Unit /*scott, */azai;
    [SerializeField]
    public Light startingSpotlight;
    [SerializeField]
    public Water water;
    [SerializeField]
    public GameObject battleCamera, resourceIcon, treasureChest, campfire, spotlight, dizzyMarker, speechBubble, unexploredTile, uiHelperWindow;
    [SerializeField]
    public CameraController cameraController;
    [SerializeField]
    public Canvas immoveableCanvas, cityCanvas, workerCanvas, traderCanvas, tradeRouteManagerCanvas, infoPopUpCanvas, overflowGridCanvas, personalResourceCanvas, tcCanvas;
    [HideInInspector]
    public bool tutorial, hideUI, tutorialGoing, scottFollow, azaiFollow, bridgeResearched, waterResearched, powerResearched, waterTransport, airTransport;
    [SerializeField]
    public DayNightCycle dayNightCycle;
    [SerializeField]
    public MeshFilter borderOne, borderTwoCorner, borderTwoCross, borderThree, borderFour;
    [SerializeField]
    public UtilityCostDisplay utilityCostDisplay;
    [SerializeField]
    public UIAttackWarning uiAttackWarning;
    [SerializeField]
    public UIWorldResources uiWorldResources;
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
    public Material transparentMat, atlasMain, atlasSemiClear;
    [SerializeField]
    private GameObject selectionIcon, enemyCampIcon, buildPanel, wonderBuildPanel, canvasHolder, enemyBorder;
    [SerializeField]
    public UnitBuildDataSO laborerData;

    [SerializeField]
    private ParticleSystem lightBeam, godRays, removeEruption, removeSplash, deathSplash, resourceSplash;

    [SerializeField]
    public Transform terrainHolder, cityHolder, wonderHolder, tradeCenterHolder, psHolder, enemyCityHolder, unitHolder, enemyUnitHolder, roadHolder, orphanImprovementHolder, objectPoolItemHolder;

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
    private bool sideways;
    [HideInInspector]
    public List<Wonder> allWonders = new();
    public Dictionary<string, Wonder> wonderConstructionDict = new();
    private Dictionary<Vector3Int, Wonder> wonderStopDict = new();
    private GameObject wonderGhost;

    //trade center info
    [SerializeField]
    private Sprite tradeCenterMapIcon;
    public Dictionary<string, TradeCenter> tradeCenterDict = new();
    private Dictionary<Vector3Int, TradeCenter> tradeCenterStopDict = new();
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
    public List<ResourceProducer> researchWaitList = new();
    [HideInInspector]
    public List<City> goldCityWaitList = new();
    [HideInInspector]
    public List<City> goldCityRouteWaitList = new();
    [HideInInspector]
    public List<Wonder> goldWonderWaitList = new();
    [HideInInspector]
    public List<TradeCenter> goldTradeCenterWaitList = new();
    //resource multiplier
    //public Dictionary<ResourceType, float> resourceStorageMultiplierDict = new();

	private Dictionary<Vector3Int, TerrainData> world = new();
    private Dictionary<Vector3Int, GameObject> buildingPosDict = new(); //to see if cities already exist in current location
    private List<Vector3Int> noWalkList = new(); //tiles where wonders are and units can't walk
    private List<GameObject> cityNamesMaps = new();

    public Dictionary<Vector3Int, City> cityDict = new(); //caching cities for easy reference
    private Dictionary<Vector3Int, string> tradeLocDict = new(); //cities and the respective locations of their harbors
    public Dictionary<Vector3Int, CityImprovement> cityImprovementDict = new(); //all the City development prefabs
    public Dictionary<Vector3Int, CityImprovement> cityImprovementConstructionDict = new();
    private Dictionary<Vector3Int, Dictionary<string, CityImprovement>> cityBuildingDict = new(); //all the buildings for highlighting
    private Dictionary<Vector3Int, Dictionary<string, GameObject>> cityBuildingGODict = new(); //all the buildings and info within a city 
    public Dictionary<Vector3Int, Vector3Int> cityImprovementQueueList = new();
    [HideInInspector]
    public List<Vector3Int> unclaimedSingleBuildList = new ();
	private Dictionary<string, Vector3Int> cityNameDict = new();
    private Dictionary<Vector3Int, string> cityLocDict = new();
    public Dictionary<Vector3Int, Unit> unitPosDict = new(); //to track unitGO locations
    [HideInInspector]
    public List<Laborer> laborerList = new();
    [HideInInspector]
    public List<Trader> traderList = new();
    [HideInInspector]
    public List<Transport> transportList = new();
    private Dictionary<string, ImprovementDataSO> improvementDataDict = new();
    private Dictionary<string, UnitBuildDataSO> unitBuildDataDict = new();
    private Dictionary<string, int> upgradeableObjectMaxLevelDict = new();
    private Dictionary<string, List<ResourceValue>> upgradeableObjectPriceDict = new(); 
    private Dictionary<string, ImprovementDataSO> upgradeableObjectDataDict = new();
    private Dictionary<string, UnitBuildDataSO> upgradeableUnitDataDict = new();
    //private Dictionary<ResourceType, Sprite> resourceSpriteDict = new();
    //private Dictionary<ResourceType, int> defaultResourcePriceDict = new();
    //private Dictionary<ResourceType, int> blankResourceDict = new();
    //private Dictionary<ResourceType, bool> boolResourceDict = new();
    //private Dictionary<Vector3Int, GameObject> queueGhostsDict = new(); //for displaying samples for queued items
    //private Dictionary<Vector3Int, GameObject> traderPosDict = new(); //to track trader locations 
    //private Dictionary<Vector3Int, List<GameObject>> multiUnitPosDict = new(); //to handle multiple units in one spot

    //for assigning labor in cities
    public Dictionary<Vector3Int, int> currentWorkedTileDict = new(); //to see how much labor is assigned to tile
    private Dictionary<Vector3Int, int> maxWorkedTileDict = new(); //the max amount of labor that can be assigned to tile
    public Dictionary<Vector3Int, Vector3Int?> cityWorkedTileDict = new(); //the city worked tiles belong to
    private Dictionary<Vector3Int, ResourceProducer> cityImprovementProducerDict = new(); //all the improvements that have resource producers (for speed)
    //private Dictionary<Vector3Int, Dictionary<string, int>> cityBuildingCurrentWorkedDict = new(); //current worked for buildings in city

    //private Dictionary<Vector3Int, Dictionary<string, int>> cityBuildingMaxWorkedDict = new(); //max labor of buildings within city
    private Dictionary<Vector3Int, List<string>> cityBuildingList = new(); //list of buildings on city tiles (here instead of City because buildings can be without a city)
    //private Dictionary<Vector3Int, Dictionary<string, ResourceProducer>> cityBuildingIsProducer = new(); //all the buildings that are resource producers (for speed)

    //for workers
    private List<Vector3Int> workerBusyLocations = new();

    //for PathFinding
    private PathNode[,] grid;

    //for roads
    public Dictionary<Vector3Int, List<Road>> roadTileDict = new(); //stores road GOs, only on terrain locations
    private List<Vector3Int> soloRoadLocsList = new(); //indicates which tiles have solo roads on them
    private List<Vector3Int> roadLocsList = new(); //indicates which tiles have roads on them
    private int roadCost; //set in road manager

    //for terrain speeds
    public TerrainDataSO flatland, forest, hill, forestHill;
    //for boats to avoid traveling the coast
    private List<Vector3Int> coastCoastList = new();

    //for enemy
    private Dictionary<Vector3Int, EnemyCamp> enemyCampDict = new();
    private Dictionary<Vector3Int, List<GameObject>> enemyBordersDict = new();
    public Dictionary<Vector3Int, EnemyAmbush> enemyAmbushDict = new();
    public Dictionary<Vector3Int, City> enemyCityDict = new();
    public int enemyUnitGrowthTime = 20;
    [HideInInspector]
    public List<Vector3Int> militaryStationLocs = new();
    public Dictionary<Vector3Int, TreasureChest> treasureLocs = new();

    //for resource icons on minimap (so they're rotated correctly)
    private Dictionary<Vector3Int, ResourceMinimapIcon> resourceIconDict = new();

    //for expanding gameobject size
    private static int increment = 3;
    public int Increment { get { return increment; } }

    [HideInInspector]
    public bool unitOrders, buildingWonder, tooltip, somethingSelected, showingMap, citySelected, cityUnitSelected;
    //private bool showObstacle, showDifficult, showGround, showSea;

    //for tracking stats
    [HideInInspector]
    public int ambushes, cityCount, infantryCount, rangedCount, cavalryCount, traderCount, boatTraderCount, laborerCount, food, lumber, popGrowth, popLost;
    [HideInInspector]
    public string tutorialStep, gameStep;
    private bool flashingButton;
    private List<string> cityNamePool;

    //for when terrain runs out of resources
    [SerializeField]
    public TerrainDataSO grasslandTerrain, grasslandHillTerrain, desertTerrain, desertHillTerrain;

    public bool showGizmo, hideTerrain = true;

    [SerializeField]
    public AudioManager ambienceAudio, musicAudio;
    [SerializeField]
    public AudioClip newWorldSong, badGuySong, congratsSong;
    private AudioSource audioSource;

    //handling discovering resources
    List<UIResourceSelectionGrid> resourceSelectionGridList = new();
    [HideInInspector]
    public List<ResourceType> resourceDiscoveredList = new();

    [HideInInspector]
    public List<string> newUnitsAndImprovements = new();

    [HideInInspector]
    public GamePersist gamePersist = new();

    //ambush info
    public Dictionary<Era, string> ambushUnitDict = new();
    //battle camera stuff
    List<Vector3Int> battleLocs = new();

    //character units
    [HideInInspector]
    public List<Unit> characterUnits;

    //permanent changes to cities, units, and city improvments
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

		//foreach (ResourceIndividualSO resourceData in ResourceHolder.Instance.allStorableResources) //Enum.GetValues(typeof(ResourceType)) 
		//{
		//	if (resourceData.resourceType == ResourceType.None)
		//		continue;
		//	resourceStorageMultiplierDict[resourceData.resourceType] = resourceData.resourceStorageMultiplier;
		//}

		//if (gameData.allTerrain.Count == 0)
		//    loadNewGame = true;

		if (!hideTerrain)
            cameraController.SetDefaultLimits();
        
        worldResourceManager = GetComponent<WorldResourceManager>();
        worldResourceManager.SetUI(uiWorldResources);
        if (researching)
            SetResearchName(researchTree.GetChosenResearchName());
        else
            SetResearchName("No Research");
  //      GameObject speechBubbleGO = Instantiate(GameAssets.Instance.speechBubble);
		//speechBubbleGO.transform.SetParent(gameObject.transform, false);
		//speechBubble = speechBubbleGO.GetComponent<SpeechBubbleHandler>();
  //      speechBubble.gameObject.SetActive(false);

        uiInfoPopUpHandler.SetWarningMessage(uiInfoPopUpHandler);
        uiInfoPopUpHandler.gameObject.SetActive(false);

        selectionIcon = Instantiate(selectionIcon);
        selectionIcon.transform.SetParent(gameObject.transform, false);
        selectionIcon.SetActive(false);
    }

    private void Start()
    {
        NewGamePrep(false);
		AddToDiscoverList(ResourceType.Gold);
        AddToDiscoverList(ResourceType.Research);

		string upgradeableObjectName = "";
        List<ResourceValue> upgradeableObjectTotalCost = new();
        int upgradeableObjectLevel = 9999;

        foreach (ImprovementDataSO data in UpgradeableObjectHolder.Instance.allBuildingsAndImprovements)
        {
            if (!data.isSecondary)
            {
                GameObject buildPanelGO = Instantiate(buildPanel);
                UIBuilderHandler builderHandler;

                if (data.rawMaterials)
                    builderHandler = cityBuilderManager.uiRawGoodsBuilder;
                else if (data.isBuilding)
				    builderHandler = cityBuilderManager.uiBuildingBuilder;
                else
				    builderHandler = cityBuilderManager.uiProducerBuilder;
            
                UIBuildOptions buildOption = buildPanelGO.GetComponent<UIBuildOptions>();
                buildOption.BuildData = data;
			    buildPanelGO.transform.SetParent(builderHandler.objectHolder, false);
			    buildOption.SetBuildOptionData(builderHandler);

                if (data.availableInitially)
                    buildOption.locked = false;
                else
                    buildOption.locked = true;
            }
            
            improvementDataDict[data.improvementNameAndLevel] = data;
            
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

                upgradeableObjectMaxLevelDict[data.improvementName] = data.improvementLevel;
                upgradeableObjectPriceDict[upgradeableObjectName] = upgradeableObjectCost;
            }

            upgradeableObjectName = data.improvementNameAndLevel; //needs to be last to compare to following data
            upgradeableObjectTotalCost = new(data.improvementCost);
            upgradeableObjectLevel = data.improvementLevel;
        }

        cityBuilderManager.uiRawGoodsBuilder.FinishMenuSetup();
		cityBuilderManager.uiBuildingBuilder.FinishMenuSetup();
		cityBuilderManager.uiProducerBuilder.FinishMenuSetup();

		upgradeableObjectName = "";
		upgradeableObjectTotalCost.Clear();
		upgradeableObjectLevel = 9999;

		//populating the upgradeableobjectdict, every one starts at level 1. 
		foreach (UnitBuildDataSO data in UpgradeableObjectHolder.Instance.allUnits)
        {
            GameObject buildPanelGO = Instantiate(buildPanel);
			
			UIBuildOptions buildOption = buildPanelGO.GetComponent<UIBuildOptions>();
			buildOption.UnitBuildData = data;
			buildPanelGO.transform.SetParent(cityBuilderManager.uiUnitBuilder.objectHolder, false);
			buildOption.SetBuildOptionData(cityBuilderManager.uiUnitBuilder);

			unitBuildDataDict[data.unitNameAndLevel] = data;
            
            if (data.availableInitially)
                buildOption.locked = false;
            else
                buildOption.locked = true;
            upgradeableObjectMaxLevelDict[data.unitName] = 1;

			if (upgradeableObjectLevel < data.unitLevel) //skip if reached max level
			{
				upgradeableUnitDataDict[upgradeableObjectName] = data; //adding the data necessary to upgrade the object to

				//calculating costs to improve
				Dictionary<ResourceType, int> prevResourceCosts = new(); //making dict to more easily find the data
				List<ResourceValue> upgradeableObjectCost = new();

				foreach (ResourceValue prevResourceValue in upgradeableObjectTotalCost)
				{
					prevResourceCosts[prevResourceValue.resourceType] = prevResourceValue.resourceAmount;
				}

				foreach (ResourceValue resourceValue in data.unitCost)
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

			upgradeableObjectName = data.unitNameAndLevel; //needs to be last to compare to following data
			upgradeableObjectTotalCost = new(data.unitCost);
			upgradeableObjectLevel = data.unitLevel;
		}

		cityBuilderManager.uiUnitBuilder.FinishMenuSetup();

		//foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
  //      {
  //          //resourceSpriteDict[resource.resourceType] = resource.resourceIcon;
  //          defaultResourcePriceDict[resource.resourceType] = resource.resourcePrice;
  //          blankResourceDict[resource.resourceType] = 0;
  //          boolResourceDict[resource.resourceType] = false;
  //      }

        //foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allWorldResources)
        //{
        //    resourceSpriteDict[resource.resourceType] = resource.resourceIcon;
        //}

        foreach (WonderDataSO wonder in UpgradeableObjectHolder.Instance.allWonders)
        {
			GameObject wonderPanelGO = Instantiate(wonderBuildPanel);

			UIWonderOptions wonderOption = wonderPanelGO.GetComponent<UIWonderOptions>();
			wonderOption.BuildData = wonder;
			wonderPanelGO.transform.SetParent(wonderHandler.objectHolder, false);
			wonderOption.SetBuildOptionData(wonderHandler);
		}

        wonderHandler.FinishMenuSetup();

        CreateParticleSystems();
        uiMainMenu.uiSaveGame.PopulateSaveItems();
        DeactivateCanvases();
        //CreateGrid(); //for alternative grid search method
        //if (gamePersist.loadNewGame)
        //{
        //    Debug.Log("loading new game");
        //    LoadData();
        //}

        //populating ambush dict
        foreach (UnitBuildDataSO unit in UpgradeableObjectHolder.Instance.enemyUnitDict.Values)
        {
            if (unit.unitType == UnitType.Infantry)
                ambushUnitDict[unit.unitEra] = unit.unitNameAndLevel;
        }
    }

    public void NewGamePrep(bool newGame, Dictionary<Vector3Int, TerrainData> terrainDict = null, List<Vector3Int> enemyCityLocs = null, List<Vector3Int> enemyRoadLocs = null, bool tutorial = false)
    {
		wonderButton.gameObject.SetActive(true);
		uiMainMenuButton.gameObject.SetActive(true);
        conversationListButton.gameObject.SetActive(true);
		uiWorldResources.SetActiveStatus(true);
        this.tutorial = tutorial;
        GameLoader.Instance.gameData.tutorial = tutorial;
		if (tutorial)
		{
			GameObject helperWindow = Instantiate(uiHelperWindow);
			helperWindow.transform.SetParent(cityCanvas.transform, false);
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
				    //StaticBatchingUtility.Combine(terrainHolder.gameObject);
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

				    //if (!hideTerrain && td.hasResourceMap)
				    //	td.resourceIcon.SetActive(true);
				    if (td.rawResourceType == RawResourceType.Rocks)
					    td.PrepParticleSystem();

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
		    //foreach (TerrainData td in terrainToCheck)
		    //{
		    //	//ConfigureUVs(td);
		    //	if (hideTerrain)
		    //		td.Hide();
		    //	else
		    //		td.Discover();
		    //}
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
			unitEnemy.enemyAI.CampLoc = unitTerrainLoc;
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
                GameObject fire = Instantiate(campfire);
                fire.transform.SetParent(terrainHolder, false);
				enemyCampDict[loc].SetCampfire(fire, world[loc].isHill, !hideTerrain);
            }

			GameLoader.Instance.gameData.enemyCampLocs[loc] = enemyCampDict[loc].SendCampData();
			Vector3 position = Vector3.zero;
			position.y += 1;
			GameObject icon = Instantiate(enemyCampIcon);
			icon.transform.position = position;
			icon.transform.SetParent(GetTerrainDataAt(loc).transform, false);
			enemyCampDict[loc].minimapIcon = icon;
			LoadEnemyBorders(loc);
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
			tradeCenterDict[center.tradeCenterName] = center;
			tradeCenterStopDict[center.mainLoc] = center;
			//tradeCenterStopDict[center.harborLoc] = center;
			AddTradeLoc(center.mainLoc, center.tradeCenterName);
			AddTradeLoc(center.harborLoc, center.tradeCenterName);
			if (hideTerrain)
				center.Hide();
			else
				center.isDiscovered = true;

            center.SetTradeCenterRep();
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
            scottFollow = false;
            azaiFollow = false;
			scott.gameObject.tag = "Character";
			scott.marker.gameObject.tag = "Character";
			azai.gameObject.tag = "Character";
			azai.marker.gameObject.tag = "Character";
			unitMovement.uiWorkerTask.DeactivateButtons();
        }
        else
        {
            characterUnits.Add(scott);
            characterUnits.Add(azai);
            scottFollow = true;
            azaiFollow = true;
			scott.gameObject.tag = "Player";
            scott.marker.gameObject.tag = "Player";
            azai.gameObject.tag = "Player";
            azai.marker.gameObject.tag = "Player";
            unit.currentLocation = RoundToInt(unit.transform.position);
            AddUnitPosition(unit.transform.position, unit);

		    scott.currentLocation = RoundToInt(scott.transform.position);
		    AddUnitPosition(scott.transform.position, scott);
            azai.currentLocation = RoundToInt(azai.transform.position);
            AddUnitPosition(azai.transform.position, azai);
        }

        uiSpeechWindow.AddToSpeakingDict("Scott", scott);
        uiSpeechWindow.AddToSpeakingDict("Azai", azai);
        scott.SetReferences(this);
        azai.SetReferences(this);

		unit.Reveal();
		Vector3Int unitPos = RoundToInt(unit.transform.position);
        //if (!unitPosDict.ContainsKey(RoundToInt(unitPos))) //just in case dictionary was missing any
        //	unit.CurrentLocation = AddUnitPosition(unitPos, unit);

        unit.currentLocation = unitPos;
		unit.SetMinimapIcon(cityBuilderManager.friendlyUnitHolder);

		if (newGame)
        {
            ToggleMainUI(false);
            playerInput.paused = true;
            Physics.gravity = new Vector3(0, -5, 0);
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

        if (enemyCityLocs == null)
            enemyCityLocs = new();
        //enemyCityLocs.Add(new Vector3Int(12, 0, 12));

        if (enemyRoadLocs == null)
            enemyRoadLocs = new();
		//enemyRoadLocs.Add(new Vector3Int(9, 0, 12));
        //enemyRoadLocs.Add(new Vector3Int(12, 0, 12); //make sure enemy road tiles can't be connected to with another road

        //adding city locs to road locs to build roads there (building roads first to build cities on top
        enemyRoadLocs.AddRange(enemyCityLocs);
        BuildEnemyRoads(enemyRoadLocs, 1);
		
        for (int i = 0; i < enemyCityLocs.Count; i++)
        {
            System.Random random = new();
            BuildEnemyCity(enemyCityLocs[i], GetTerrainDataAt(enemyCityLocs[i]), UpgradeableObjectHolder.Instance.improvementDict["City-0"].prefab, enemyRoadLocs, true, currentEra, false, random);
        }

        //temporary for enemy cities
        //above is temporary for enemy cities
    }

    private IEnumerator StartingAmbience()
    {
        yield return new WaitForSeconds(0.5f);

		ambienceAudio.AmbienceCheck();
	}

    private IEnumerator StartingMusic(bool newGame)
    {
		yield return new WaitForSeconds(5f);

        if (newGame)
            musicAudio.PlaySpecificSong(newWorldSong);
        else
            musicAudio.StartMusic();
	}

	private IEnumerator StartingSpotlight()
    {
        mainPlayer.unitRigidbody.useGravity = false;
        yield return new WaitForSeconds(2);

        //cityBuilderManager.PlayRingAudio();
		mainPlayer.unitRigidbody.useGravity = true;
		spotlight.SetActive(true);
        Vector3 scale = spotlight.transform.localScale;
        startingSpotlight.gameObject.SetActive(true);
        cityBuilderManager.PlayFieryOpen();

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

		//cityBuilderManager.PlayBoomAudio();
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

			if (terrainData.decors[td.decorIndex] != null)
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

				if (td.rawResourceType == RawResourceType.Rocks)
					td.PrepParticleSystem();
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
			GameObject helperWindow = Instantiate(uiHelperWindow);
			helperWindow.transform.SetParent(cityCanvas.transform, false);
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

            GameObject go = Instantiate(terrainData.prefabs[data.variant], data.tileCoordinates, data.rotation);
            go.transform.SetParent(terrainHolder, false);
            TerrainData td = go.GetComponent<TerrainData>();
            td.LoadData(data);
            td.SetData(terrainData);

            if (terrainData.decors[data.decor] != null)
            {
				if ((terrainData.type == TerrainType.Forest || terrainData.type == TerrainType.ForestHill) && terrainData.terrainDesc != TerrainDesc.Swamp)
                {
					GameObject prop = Instantiate(terrainData.decors[data.decor], Vector3Int.zero, Quaternion.identity);
					prop.transform.SetParent(td.prop, false);
					prop.GetComponent<TreeHandler>().propMesh.rotation = data.propRotation;
					td.SetProp();
					terrainPropsToModify.Add(td);

					GameObject nonStaticProp = Instantiate(terrainData.decors[data.decor], Vector3.zero, Quaternion.identity);
                    nonStaticProp.transform.SetParent(td.nonstatic, false);
                    td.nonstatic.rotation = data.propRotation;
                    td.SetNonStatic();
					td.SkinnedMeshCheck();
				}
				else
                {
				    GameObject prop = Instantiate(terrainData.decors[data.decor], Vector3.zero, data.propRotation);
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

                if (td.hasResourceMap)
					ToggleResourceIcon(td.TileCoordinates, true);
			}
			else
            {
                td.Hide();

                if (td.rawResourceType == RawResourceType.Rocks)
                    td.PrepParticleSystem();
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
			GameObject unexploredGO = Instantiate(unexploredTile, loc, Quaternion.identity);
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
            GameObject go = Instantiate(UpgradeableObjectHolder.Instance.tradeCenterDict[centerData.name], centerData.mainLoc, Quaternion.identity);
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
			
			tradeCenterDict[center.tradeCenterName] = center;
			tradeCenterStopDict[center.mainLoc] = center;
			//tradeCenterStopDict[center.harborLoc] = center;
			AddTradeLoc(center.mainLoc, center.tradeCenterName);
			AddTradeLoc(center.harborLoc, center.tradeCenterName);
			if (!GetTerrainDataAt(centerData.mainLoc).isDiscovered && hideTerrain)
				center.Hide();
			else
				center.isDiscovered = true;

            center.SetTradeCenterRep();
            LoadTradeCenterBorders(centerData.mainLoc);
			GameLoader.Instance.centerWaitingDict[center] = (centerData.waitList, centerData.seaWaitList);
		}
	}

    public void GenerateEnemyCities(Dictionary<Vector3Int, EnemyCityData> data, List<Vector3Int> enemyRoadLocs)
    {
        foreach (Vector3Int tile in data.Keys)
        {
			BuildEnemyCity(tile, GetTerrainDataAt(tile), UpgradeableObjectHolder.Instance.improvementDict["City-0"].prefab, enemyRoadLocs, true, data[tile].era, true);
		}

		//BuildEnemyRoads(enemyRoadLocs);
	}

	public void BuildEnemyCity(Vector3Int cityTile, TerrainData td, GameObject prefab, List<Vector3Int> roadTiles, bool hasWater, Era era, bool load, System.Random random = null)
	{
        Dictionary<string, string> buildingEraDict = GetBuildingEraDict(era);
        EnemyCityData data;
		
		if (load)
        {
            data = GameLoader.Instance.gameData.enemyCities[cityTile];
        }
        else
        {
            data = new();
            data.era = era;
            data.loc = cityTile;
            data.hasWater = hasWater;
            data.cityName = cityNamePool[random.Next(0, cityNamePool.Count)];
		    if (hasWater)
			    data.popSize = random.Next(5, 13);
		    else
			    data.popSize = 4;
        }

		List<Vector3Int> foodLocs = new();
        List<Vector3Int> fishLocs = new();
        List<Vector3Int> flatlandLocs = new();
        List<Vector3Int> coastLocs = new();
        List<Vector3Int> mineLocs = new();

        td.enemyCamp = true;
        td.enemyZone = true;
		AddToCityLabor(cityTile, null);
		foreach (Vector3Int tile in GetNeighborsFor(cityTile, State.EIGHTWAYINCREMENT))
        {
            TerrainData tdNeighbor = GetTerrainDataAt(tile);
            tdNeighbor.enemyZone = true;
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
            
            if (tdNeighbor.terrainData.type == TerrainType.Flatland)
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
		city.SetWorld(this);
        city.ExtinguishFire();
        city.cityLoc = cityTile;
        city.cityName = data.cityName;
        city.currentPop = data.popSize;
        city.minimapIcon.sprite = city.enemyCityIcon;

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
            buildingArray = new string[2] { buildingEraDict["Monument"], buildingEraDict["Market"] };
        else
            buildingArray = new string[3] { buildingEraDict["Monument"], buildingEraDict["Well"], buildingEraDict["Market"] };

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

        //checking to load fighting of moving enemy units
        //bool attacked = false;
        bool movingOut = false;
        Dictionary<Vector3Int, UnitData> fightingEnemies = new();

        if (load)
        {
		    /*if (GameLoader.Instance.gameData.attackedEnemyBases.ContainsKey(cityTile))
		    {
			    attacked = true;
			    EnemyCampData enemyData = GameLoader.Instance.gameData.attackedEnemyBases[cityTile];

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
			    city.enemyCamp.returning = enemyData.returning;
			    city.enemyCamp.campCount = enemyData.campCount;
			    city.enemyCamp.infantryCount = enemyData.infantryCount;
			    city.enemyCamp.rangedCount = enemyData.rangedCount;
			    city.enemyCamp.cavalryCount = enemyData.cavalryCount;
			    city.enemyCamp.seigeCount = enemyData.seigeCount;
			    city.enemyCamp.health = enemyData.health;
			    city.enemyCamp.strength = enemyData.strength;
		    }
            else */if (GameLoader.Instance.gameData.movingEnemyBases.ContainsKey(cityTile))
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
			    city.enemyCamp.moveToLoc = enemyData.chaseLoc;
			    city.enemyCamp.pathToTarget = enemyData.pathToTarget;
			    city.enemyCamp.movingOut = enemyData.movingOut;
			    city.enemyCamp.returning = enemyData.returning;
			    city.enemyCamp.chasing = enemyData.chasing;
                city.enemyCamp.pillage = enemyData.pillage;
                city.enemyCamp.pillageTime = enemyData.pillageTime;
                city.enemyCamp.growing = enemyData.growing;
                city.enemyCamp.fieldBattleLoc = enemyData.fieldBattleLoc;
                city.enemyCamp.lastSpot = enemyData.lastSpot;
                city.enemyCamp.removingOut = enemyData.removingOut;
                city.enemyCamp.seaTravel = enemyData.seaTravel;
                city.enemyCamp.actualAttackLoc = enemyData.actualAttackLoc;
                city.countDownTimer = enemyData.countDownTimer;

                if ((city.enemyCamp.inBattle || city.enemyCamp.movingOut) && !city.enemyCamp.returning && city.enemyCamp.campCount != 0)
                    GameLoader.Instance.attackingEnemyCitiesList.Add(city);
			
                if (city.enemyCamp.campCount != 0)
                    movingOut = true;

                if (GetTerrainDataAt(cityTile).isDiscovered && !city.enemyCamp.growing && !city.enemyCamp.movingOut && !city.enemyCamp.inBattle && !city.enemyCamp.prepping && 
                    !city.enemyCamp.attackReady && !city.enemyCamp.returning)
                    city.LoadSendAttackWait();
		    }
        }
        else
        {
			city.enemyCamp.fieldBattleLoc = cityTile;
            city.enemyCamp.moveToLoc = city.barracksLocation;
			city.enemyCamp.SetCityEnemyCamp();
			AddAllEnemyUnits(city.enemyCamp, random, data, load);
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

			    GameObject enemyGO = Instantiate(enemyData.prefab, unitSpawn, rotation);
			    enemyGO.name = enemyData.unitDisplayName;
			    enemyGO.transform.SetParent(enemyUnitHolder, false);

			    Unit unit = enemyGO.GetComponent<Unit>();
			    unit.SetReferences(this);
			    unit.SetMinimapIcon(enemyUnitHolder);
			    if (!city.enemyCamp.movingOut)
                {
				    unit.minimapIcon.gameObject.SetActive(false);
                }

			    //just in case dictionary was missing any
				unit.currentLocation = AddUnitPosition(unitLoc, unit);

			    unit.currentLocation = unitLoc;
			    city.enemyCamp.UnitsInCamp.Add(unit.military);
			    unit.enemyAI.CampLoc = city.enemyCamp.loc;
			    unit.enemyAI.CampSpot = unitLoc;
                unit.military.enemyCamp = city.enemyCamp;

				//RemoveUnitPosition(unitLoc);
			    unit.military.LoadUnitData(fightingEnemies[unitLoc]);
			    AddUnitPosition(unit.currentLocation, unit);

                TerrainData tdUnit = GetTerrainDataAt(unit.currentLocation);
                if (city.enemyCamp.movingOut)
                {
                    if (!tdUnit.isDiscovered)
                        unit.HideUnit(false);
				    else if (tdUnit.CompareTag("Forest") || tdUnit.CompareTag("Forest Hill"))
					    unit.marker.ToggleVisibility(true);
			    }
                else
                {
                    if (!tdUnit.isDiscovered)
                        unit.gameObject.SetActive(false);
				}
		    }
        }

		//city.enemyCamp.SetCityEnemyCamp();

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

        LoadEnemyBorders(cityTile);

        if (load)
        {
            if (!td.isDiscovered)
                city.subTransform.gameObject.SetActive(false); //necessary if one of improvements is showing but not city
        
   //         if (city.enemyCamp.inBattle && !city.enemyCamp.returning)
   //         {
   //             if (movingOut)
   //                 ToggleCityMaterialClear(cityTile, city.enemyCamp.attackingArmy.city.cityLoc, city.enemyCamp.moveToLoc, city.enemyCamp.threatLoc, true);
   //             else
			//		ToggleCityMaterialClear(cityTile, city.enemyCamp.attackingArmy.city.cityLoc, cityTile, city.enemyCamp.threatLoc, true);
			//}

			if (city.enemyCamp.growing && !city.enemyCamp.prepping && !city.enemyCamp.attackReady && !city.enemyCamp.inBattle)
            {
                city.enemyCamp.LoadCityEnemyCamp();
				city.StartSpawnCycle(false);
            }
		}
        else
        {
			GameLoader.Instance.gameData.movingEnemyBases[cityTile] = new();
			GameLoader.Instance.gameData.enemyCities[cityTile] = data;
            GameLoader.Instance.gameData.enemyRoads = new(roadTiles);
        }
	}

    private void AddAllEnemyUnits(EnemyCamp camp, System.Random random, EnemyCityData data, bool load)
    {
		List<Vector3Int> campLocs = GetNeighborsFor(camp.loc, State.EIGHTWAYARMY);

		//0 is for infantry, 1 ranged, 2 cavalry
		List<int> unitList = new() { 0, 1, 2 };
        List<Vector3Int> barracksLocs = new();
        
        if (load)
            barracksLocs = data.enemyUnitData.Keys.ToList();

		for (int i = 0; i < 9; i++)
		{
            Vector3Int spawnLoc;
            int chosenUnitType = 0;

			if (load)
            {
                spawnLoc = barracksLocs[i];
            }
            else
            {
                if (i < 3)
                {
                    spawnLoc = camp.GetAvailablePosition(UnitType.Infantry);
                    chosenUnitType = 0;
                }
                else if (i < 6)
                {
					spawnLoc = camp.GetAvailablePosition(UnitType.Ranged); 
                    chosenUnitType = 1;
				}
                else
                {
					spawnLoc = camp.GetAvailablePosition(UnitType.Cavalry);
					chosenUnitType = 2;
                }
			}

			campLocs.Remove(spawnLoc);

			GameObject enemy;

			if (load)
            {
                 enemy = UpgradeableObjectHolder.Instance.enemyUnitDict[data.enemyUnitData[spawnLoc]].prefab;
            }
            else
            {
			    enemy = GameLoader.Instance.terrainGenerator.enemyUnits[chosenUnitType];
            }

			UnitType type = enemy.GetComponent<Unit>().buildDataSO.unitType;

			if (type == UnitType.Infantry)
			{
				infantryCount++;
				if (infantryCount >= 3)
					unitList.Remove(0);
			}
			else if (type == UnitType.Ranged)
			{
				rangedCount++;
				if (rangedCount >= 3)
					unitList.Remove(1);
			}
			else if (type == UnitType.Cavalry)
			{
				cavalryCount++;
				if (cavalryCount >= 3)
					unitList.Remove(2);
			}

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
			if (!unitPosDict.ContainsKey(RoundToInt(unitLoc))) //just in case dictionary was missing any
				unitEnemy.currentLocation = AddUnitPosition(unitLoc, unitEnemy);
			unitEnemy.currentLocation = unitLoc;
			unitEnemy.gameObject.name = unitEnemy.buildDataSO.unitDisplayName;
			unitEnemy.military.barracksBunk = spawnLoc;

			camp.UnitsInCamp.Add(unitEnemy.military);
			unitEnemy.enemyAI.CampLoc = camp.loc;
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
            roadManager.BuildRoadAtPosition(roadList[i], level);
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
		AddCity(cityTile, city);
		//city.SetCityBuilderManager(cityBuilderManager);
		city.CheckForAvailableSingleBuilds();

		city.LightFire(td.isHill);

		AddCityBuildingDict(cityTile);

        city.LoadCityData(data);

        //building buildings
        foreach (CityImprovementData improvementData in data.cityBuildings)
            CreateBuilding(UpgradeableObjectHolder.Instance.improvementDict[improvementData.name], city, improvementData);

        GameLoader.Instance.cityWaitingDict[city] = (data.waitingforResourceProducerList, data.waitingForProducerStorageList, data.waitingToUnloadProducerList,
            data.waitList, data.seaWaitList, data.waitingForTraderList, data.tradersHere);
	}

    private void CreateBuilding(ImprovementDataSO buildingData, City city, CityImprovementData data)
    {
		if (buildingData.improvementName == city.housingData.improvementName)
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
			building = Instantiate(buildingData.prefab, cityPos, Quaternion.identity);
		}
		else
		{
			building = Instantiate(buildingData.prefab, cityPos, Quaternion.identity);
		}

		//setting world data
		CityImprovement improvement = building.GetComponent<CityImprovement>();
        improvement.SetWorld(this);
		improvement.loc = city.cityLoc;
		building.transform.parent = city.subTransform;

		string buildingName = buildingData.improvementName;
		SetCityBuilding(improvement, buildingData, city.cityLoc, building, city, buildingName);
		
        city.HousingCount += buildingData.housingIncrease;

		cityBuilderManager.CombineMeshes(city, city.subTransform, false);
		improvement.SetInactive();

		if (buildingData.singleBuild)
			city.singleBuildImprovementsBuildingsDict[buildingData.improvementName] = city.cityLoc;

		if (buildingData.improvementName == "Market")
			city.hasMarket = true;
	}

	public void CreateImprovement(City city, CityImprovementData data, bool enemy = false)
	{
        //spending resources to build
        ImprovementDataSO improvementData = UpgradeableObjectHolder.Instance.improvementDict[data.name];
        Vector3Int tempBuildLocation = data.location;
		Vector3Int buildLocation = tempBuildLocation;
		buildLocation.y = 0;

		//rotating harbor so it's closest to city
		int rotation = 0;
		if (improvementData.improvementName == "Harbor")
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
			improvement = Instantiate(improvementData.prefab, buildLocationHill, Quaternion.Euler(0, rotation, 0));
		}
		else
		{
			improvement = Instantiate(improvementData.prefab, buildLocation, Quaternion.Euler(0, rotation, 0));
		}

		if (enemy)
			improvement.tag = "Enemy";
		AddStructure(buildLocation, improvement);
		CityImprovement cityImprovement = improvement.GetComponent<CityImprovement>();
        cityImprovement.SetWorld(this);
        cityImprovement.loc = buildLocation;
		cityImprovement.InitializeImprovementData(improvementData);
		//cityImprovement.SetPSLocs();
		cityImprovement.SetQueueCity(null);
		cityImprovement.building = improvementData.isBuilding;
        if (city != null)
    		cityImprovement.city = city;

		SetCityDevelopment(tempBuildLocation, cityImprovement);

        if (data.isConstruction)
    		improvement.SetActive(false);

		//setting single build rules
		if (improvementData.singleBuild && city != null)
		{
			city.singleBuildImprovementsBuildingsDict[improvementData.improvementName] = tempBuildLocation;
			AddToCityLabor(tempBuildLocation, city.cityLoc);
		}

		//resource production
		ResourceProducer resourceProducer = improvement.GetComponent<ResourceProducer>();
		buildLocation.y = 0;
		AddResourceProducer(buildLocation, resourceProducer);
        if (city != null)
    		resourceProducer.SetResourceManager(city.ResourceManager);
		resourceProducer.InitializeImprovementData(improvementData, td.resourceType); //allows the new structure to also start generating resources
		resourceProducer.SetCityImprovement(cityImprovement);
		resourceProducer.SetLocation(tempBuildLocation);
        cityImprovement.CheckPermanentChanges();

        if (data.isConstruction)
        {
            cityImprovement.LoadData(data, city, this);
            CityImprovement constructionTile = cityBuilderManager.GetFromConstructionTilePool();
            constructionTile.timePassed = data.timePassed; 
            constructionTile.InitializeImprovementData(improvementData);
		    SetCityImprovementConstruction(tempBuildLocation, constructionTile);
		    constructionTile.transform.position = tempBuildLocation;
		    constructionTile.BeginImprovementConstructionProcess(city, resourceProducer, tempBuildLocation, cityBuilderManager, td.isHill, true);		
        }
        else
        {
			if (!improvementData.isBuildingImprovement)
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

			tempObject.transform.localScale = improvement.transform.localScale;
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

				foreach (MeshFilter mesh in cityImprovement.MeshFilter)
				{
					if (mesh.name == "Ground")
					{
						Vector2[] terrainUVs = td.UVs;
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
			if (improvementData.replaceRocks)
			{
				foreach (MeshFilter mesh in cityImprovement.MeshFilter)
				{
					if (mesh.name == "Rocks")
					{
						Vector2 rockUVs = td.RockUVs;
						Vector2[] newUVs = mesh.mesh.uv;
						int i = 0;

						while (i < newUVs.Length)
						{
							newUVs[i] = rockUVs;
							i++;
						}
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
							int j = 0;
							Vector2[] skinnedUVs = cityImprovement.SkinnedMesh.sharedMesh.uv;

							while (j < skinnedUVs.Length)
							{
								skinnedUVs[j] = rockUVs;
								j++;
							}

							cityImprovement.SkinnedMesh.sharedMesh.uv = skinnedUVs;

							//if (cityImprovement.SkinnedMesh.name == "RocksAnim")
							//{
							Material mat = td.prop.GetComponentInChildren<MeshRenderer>().sharedMaterial;
							cityImprovement.SkinnedMesh.material = mat;
							cityImprovement.SetNewMaterial(mat);
							//}
						}

						break;
					}
				}

			}

			if (td.prop != null && improvementData.hideProp)
                td.ShowProp(false);

			//setting harbor info
			if (improvementData.improvementName == "Harbor")
			{
                cityImprovement.mapIconHolder.localRotation = Quaternion.Inverse(improvement.transform.rotation);
				if (city != null)
                {
                    city.hasHarbor = true;
				    city.harborLocation = tempBuildLocation;
				    AddTradeLoc(tempBuildLocation, city.cityName);
                }
			}
			else if (improvementData.improvementName == "Barracks" && city != null)
			{
				militaryStationLocs.Add(tempBuildLocation);
				city.hasBarracks = true;
				city.barracksLocation = tempBuildLocation;

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

			//setting labor info (harbors have no labor)
			AddToMaxLaborDict(tempBuildLocation, improvementData.maxLabor);
			//if (city.AutoAssignLabor && city.cityPop.UnusedLabor > 0 && improvementData.maxLabor > 0)
			//	city.AutoAssignmentsForLabor();

			//removing areas to work
			foreach (Vector3Int loc in improvementData.noWalkAreas)
				AddToNoWalkList(loc + tempBuildLocation);

			cityImprovement.SetInactive();

            if (enemy)
            {
                if (td.isDiscovered)
                {
                    if (GetTerrainDataAt(city.cityLoc).isDiscovered)
                        cityImprovement.StartJustWorkAnimation();
                    else
                        cityImprovement.RevealImprovement();
                }
            }
            else
            {
                cityImprovement.LoadData(data, city, this);
            }
		}
	}

	public void CreateUpgradedImprovement(Vector3Int upgradeLoc, CityImprovement selectedImprovement, City city)
	{
		string nameAndLevel = selectedImprovement.GetImprovementData.improvementNameAndLevel;
		List<ResourceValue> upgradeCost = new(GetUpgradeCost(nameAndLevel));
		selectedImprovement.UpgradeCost = upgradeCost;
		ImprovementDataSO data = GetUpgradeData(nameAndLevel);

		ResourceProducer resourceProducer = GetResourceProducer(upgradeLoc);
		selectedImprovement.BeginImprovementUpgradeProcess(city, resourceProducer, upgradeLoc, data, true);
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

				camp.moveToLoc = enemyData.chaseLoc;
				camp.pathToTarget = enemyData.pathToTarget;
				camp.movingOut = enemyData.movingOut;
				camp.returning = enemyData.returning;
				camp.chasing = enemyData.chasing;
                camp.atSea = enemyData.atSea;
			}

			bool reveal = false;
            if (discovered.Contains(loc))
                reveal = true;

			bool fullCamp = enemyCampDict[loc].campCount == 9;
			if (!fullCamp)
            {
                GameObject fire = Instantiate(campfire);
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
				GameObject enemyGO = Instantiate(enemyData.prefab, unitSpawn, rotation);
                enemyGO.name = enemyData.unitDisplayName;
                enemyGO.transform.SetParent(enemyUnitHolder, false);
                if (!reveal)
                    enemyGO.SetActive(false);

                Unit unit = enemyGO.GetComponent<Unit>();
                if (tdCamp.CompareTag("Forest") || tdCamp.CompareTag("Forest Hill"))
                    unit.marker.ToggleVisibility(true);
		        unit.SetReferences(this);
                unit.SetMinimapIcon(enemyUnitHolder);
                if (!movingOut)
                    unit.minimapIcon.gameObject.SetActive(false);
		        if (!attacked) //just in case dictionary was missing any
			        unit.currentLocation = AddUnitPosition(unitLoc, unit);
		        unit.currentLocation = unitLoc;
                enemyCampDict[loc].UnitsInCamp.Add(unit.military);
		        unit.enemyAI.CampLoc = loc;
		        unit.enemyAI.CampSpot = unitLoc;
        		unit.military.enemyCamp = enemyCampDict[loc];

				if (attacked || movingOut)
                {
                    //RemoveUnitPosition(unitLoc);
                    unit.military.LoadUnitData(fightingEnemies[unitLoc]);
                    AddUnitPosition(unit.currentLocation, unit);
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
			GameObject icon = Instantiate(enemyCampIcon);
	        icon.transform.position = position;
			icon.transform.SetParent(GetTerrainDataAt(loc).transform, false);
			enemyCampDict[loc].minimapIcon = icon;
        }

        for (int i = 0; i < enemyCampPos.Count; i++)
        {
            LoadEnemyBorders(enemyCampPos[i]);
        }
	}

    public void MakeEnemyAmbushes(Dictionary<Vector3Int, EnemyAmbushData> ambushLocs, Dictionary<string, Trader> ambushedTraders)
    {
		foreach (Vector3Int tile in ambushLocs.Keys)
		{
			EnemyAmbush ambush = new();
			ambush.loc = tile;
			ambush.attackedTrader = ambushLocs[tile].attackedTrader;
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
                
				GameObject enemyGO = Instantiate(enemyData.prefab, data.position, data.rotation);
				enemyGO.name = enemyData.unitDisplayName;
				enemyGO.transform.SetParent(enemyUnitHolder, false);

				Unit unit = enemyGO.GetComponent<Unit>();
				unit.SetMinimapIcon(enemyUnitHolder);
				if (td.CompareTag("Forest") || td.CompareTag("Forest Hill"))
					unit.marker.ToggleVisibility(true);
				unit.SetReferences(this);
				unit.currentLocation = data.currentLocation;
                ambush.attackingUnits.Add(unit.military);
                unit.military.enemyAmbush = ambush;

				unit.military.LoadUnitData(data);
				AddUnitPosition(unit.currentLocation, unit);
			}

            enemyAmbushDict[tile] = ambush;
		}
	}

    public void CreateUnit(IUnitData data, City city = null)
    {
        UnitBuildDataSO unitData = UpgradeableObjectHolder.Instance.unitDict[data.unitNameAndLevel];
		GameObject unitGO = unitData.prefab;

        if (data.secondaryPrefab)
            unitGO = unitData.secondaryPrefab;

		GameObject unit = Instantiate(unitGO, data.position, data.rotation); //produce unit at specified position
		unit.transform.SetParent(unitHolder, false);
		Unit newUnit = unit.GetComponent<Unit>();
		newUnit.SetReferences(this);
		newUnit.SetMinimapIcon(unitHolder);

		//assigning army details and rotation
		if (newUnit.inArmy)
        {
            newUnit.military.homeBase = city;
			city.army.AddToArmy(newUnit.military);
            if (city.currentPop == 0 && city.army.armyCount == 1)
                city.StartGrowthCycle(true);
            city.army.AddToOpenSpots(data.barracksBunk);
			newUnit.name = unitData.unitDisplayName;
		}

        if (newUnit.trader)
        {
            newUnit.trader.SetRouteManagers(unitMovement.uiTradeRouteManager, unitMovement.uiPersonalResourceInfoPanel); //start method is too slow and awake is too fast
            traderList.Add(newUnit.trader);
			newUnit.trader.LoadTraderData(data.GetTraderData());

            if (newUnit.trader.ambush)
                GameLoader.Instance.ambushedTraders[newUnit.trader.name] = newUnit.trader;
        }
        else if (newUnit.isLaborer)
        {
            Laborer laborer = newUnit.GetComponent<Laborer>();
            laborerList.Add(laborer);
            laborer.LoadLaborerData(data.GetLaborerData());
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
		GameObject unitGO = unitData.prefab;

		if (data.secondaryPrefab)
			unitGO = unitData.secondaryPrefab;

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
        StartCoroutine(TakeScreenshot(saveName));
    }

    private IEnumerator TakeScreenshot(string saveName)
    {
        yield return new WaitForEndOfFrame();

        int height = Mathf.RoundToInt(Screen.width * 0.625f);
        int width = height / 4 * 3;
        Texture2D texture = new Texture2D(height, width, TextureFormat.ARGB32, false);
        texture.ReadPixels(new Rect((Screen.width - height), (Screen.height - width), texture.width, texture.height), 0, 0);
        texture.Apply();
        byte[] bytes = texture.EncodeToPNG();
        string bytesString = Convert.ToBase64String(bytes);
        //File.WriteAllBytes(Application.persistentDataPath + "/Screenshot.png", bytes);
        Texture2D newTexture = texture;

        canvasHolder.SetActive(true);
        float playTime = (DateTime.Now - currentTime).Seconds;
		uiMainMenu.uiSaveGame.UpdateSaveItems(saveName, playTime, version, newTexture);
        //Destroy(texture);
        GameLoader.Instance.SaveGame(saveName, playTime, version, bytesString);
    }

    //it's actually "F12"
    public void HandleCtrlT()
    {
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

	public void ClearMap()
    {
		foreach (Transform go in terrainHolder)
			Destroy(go.gameObject);

        world.Clear();

		foreach (Transform go in tradeCenterHolder)
			Destroy(go.gameObject);

        foreach (Transform go in enemyUnitHolder)
            Destroy(go.gameObject);

        foreach (Vector3Int loc in enemyCampDict.Keys)
            Destroy(enemyCampDict[loc].minimapIcon);

        foreach (Vector3Int loc in resourceIconDict.Keys)
            Destroy(resourceIconDict[loc].gameObject);

        tradeCenterDict.Clear();
        tradeCenterStopDict.Clear(); 
        cityWorkedTileDict.Clear();
        buildingPosDict.Clear();
        cityNamesMaps.Clear();
        tradeLocDict.Clear();
        enemyCampDict.Clear();
        unitPosDict.Clear();
        resourceIconDict.Clear();
        mapHandler.ResetResourceLocDict();
	}

	private void LoadEnemyBorders(Vector3Int enemyLoc)
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
					//borderPosition.y = -0.1f;
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

					GameObject border = Instantiate(enemyBorder, borderPosition, rotation);
					border.transform.SetParent(terrainHolder, false);

                    if (!enemyBordersDict.ContainsKey(tile))
                        enemyBordersDict[tile] = new();

                    enemyBordersDict[tile].Add(border);

                    if (!world[tile].isDiscovered)
    					border.SetActive(false);
					//world[tile].enemyBorders.Add(border);
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

				    GameObject border = Instantiate(enemyBorder, borderPosition, rotation);
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

	public void AaddGold() //for testing, on a button
    {
        UpdateWorldResources(ResourceType.Gold, 100);

        resourceYieldChangeDict[ResourceType.Food] = .5f;
        bridgeResearched = true;

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
   //     if (!openingImmoveable)
        immoveableCanvas.gameObject.SetActive(false);
   //     else
			//openingImmoveable = false;
        //if (!openingCity)
        cityCanvas.gameObject.SetActive(false);
        //else
        //    openingCity = false;
        personalResourceCanvas.gameObject.SetActive(false);
        tcCanvas.gameObject.SetActive(false);
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

        /*if (desc == TerrainDesc.Grassland || desc == TerrainDesc.GrasslandFloodPlain || desc == TerrainDesc.Forest || desc == TerrainDesc.Jungle || desc == TerrainDesc.GrasslandHill || desc == TerrainDesc.Swamp)
        {
            foreach (Vector3Int neighbor in GetNeighborsFor(td.TileCoordinates, State.FOURWAYINCREMENT))
            {
                if (GetTerrainDataAt(neighbor).terrainData.grassland || GetTerrainDataAt(neighbor).terrainData.desert) //grassland only fades edges for coast tiles
                    grasslandCount[i] = 0;
                else
                    grasslandCount[i] = 1;

                i++;
            }
        }
        else */if (td.terrainData.type == TerrainType.Coast || desc == TerrainDesc.Desert || desc == TerrainDesc.DesertFloodPlain || desc == TerrainDesc.DesertHill || desc == TerrainDesc.River)
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

        int eulerAngle = Mathf.RoundToInt(td.main.eulerAngles.y);

        Vector2[] uvs = SetUVMap(grasslandCount, SetUVShift(desc), eulerAngle);
        if (td.UVs.Length > 4)
            uvs = NormalizeUVs(uvs, td.UVs);
        td.SetUVs(uvs);
	}

    public float SetUVShift(TerrainDesc desc)
    {
        float interval = .0625f; 
        float shift = 0;
        
        switch (desc)
        {
            case TerrainDesc.Desert:
                shift = interval;
                break;
            //case TerrainDesc.GrasslandFloodPlain:
            //    shift = interval * 2;
            //    break;
            case TerrainDesc.DesertFloodPlain:
                shift = interval;/* * 3;*/
                break;
            case TerrainDesc.River:
                shift = interval * 10 /** 3*/;
                break;
            case TerrainDesc.Sea:
                shift = interval * 10;
                break;
            //case TerrainDesc.GrasslandHill:
            //    shift = interval * 7;
            //    break;
            case TerrainDesc.DesertHill:
                shift = interval * 8;
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

    public void OpenMainMenu()
    {
        if (uiMainMenu.activeStatus)
        {
            cityBuilderManager.PlaySelectAudio();
            uiMainMenu.ToggleVisibility(false);
		}
        else
        {
            //unitMovement.ClearSelection();
            //cityBuilderManager.ResetCityUI();

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

    private void CreateParticleSystems()
    {
        lightBeam = Instantiate(lightBeam, new Vector3(0, 0, 0), Quaternion.Euler(-90, 0, 0));
        lightBeam.transform.SetParent(transform, false);
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
        CloseCampTooltipButton();
        CloseTradeRouteBeginTooltipButton();
        cityBuilderManager.ResetCityUI();
        unitMovement.ClearSelection();
        cityBuilderManager.UnselectWonder();
        cityBuilderManager.UnselectTradeCenter();
        //unitMovement.LoadUnloadFinish(false);
        researchTree.ToggleVisibility(false);
        wonderHandler.ToggleVisibility(false);
        //mapPanel.ToggleVisibility(false);
        wonderButton.ToggleButtonColor(false);
        conversationListButton.ToggleButtonColor(false);
        uiConversationTaskManager.ToggleVisibility(false);
        CloseMap();
        CloseTerrainTooltipButton();
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

    public void HandleEsc()
    {
        if (buildingWonder)
        {
            CloseBuildingSomethingPanel();
		}
        else if (uiCityPopIncreasePanel.activeStatus)
        {
            cityBuilderManager.CloseAddPopWindow();
        }
        else if (uiTradeRouteBeginTooltip.activeStatus)
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
			if (unitMovement.selectedUnit.isBusy)
			{
				unitMovement.workerTaskManager.CancelTask();
			}
            else if (unitMovement.selectedUnit.military && unitMovement.selectedUnit.military.guard)
            {
				unitMovement.CancelOrders();
			}
            else if (unitMovement.selectedUnit.inArmy && (unitMovement.selectedUnit.military.isMarching || unitMovement.selectedUnit.military.homeBase.army.inBattle 
                || unitMovement.selectedUnit.military.homeBase.army.traveling || unitMovement.selectedUnit.military.homeBase.army.atHome))
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
		if (!cityBuilderManager.uiCityNamer.activeStatus)
			OpenConversationList();
    }

    public void HandleK()
    {
		if (!cityBuilderManager.uiCityNamer.activeStatus)
			uiTomFinder.FindTom();
    }

    public void HandleM()
    {
		if (!cityBuilderManager.uiCityNamer.activeStatus)
			mapHandler.ToggleMap();
    }

    public void HandleN()
    {
        if (!cityBuilderManager.uiCityNamer.activeStatus)
            OpenWonders();
    }

    public void HandleR()
    {
        if (buildingWonder)
            RotateWonderPlacement();
    }

    public void HandleI()
    {
		if (!cityBuilderManager.uiCityNamer.activeStatus)
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

            uiRotateWonder.ToggleVisibility(true);

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

            uiConfirmWonderBuild.ToggleVisibility(true);
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
        uiConfirmWonderBuild.ToggleVisibility(false);
        buildingWonder = false;
        uiBuildingSomething.ToggleVisibility(false);
        uiRotateWonder.ToggleVisibility(false);

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

        //prep terrain
        foreach(Vector3Int tile in wonderPlacementLoc)
        {
            CheckTileForTreasure(tile);
            TerrainData td = GetTerrainDataAt(tile);

            if (wonderData.isSea)
            {
                td.sailable = false;
                td.walkable = true;
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
        GameObject wonderGO = Instantiate(wonderData.wonderPrefab, centerPos, rotation);
        wonderGO.gameObject.transform.SetParent(wonderHolder, false);
        Wonder wonder = wonderGO.GetComponent<Wonder>();
        wonder.wonderName = wonderData.wonderName;
        allWonders.Add(wonder);
        wonder.SetReferences(this, cityBuilderManager.focusCam);
        wonder.WonderData = wonderData;
        wonder.WonderLocs = new(wonderPlacementLoc);
        wonder.SetPrefabs(false);
        //wonder.wonderName = "Wonder - " + wonderData.wonderName;
        wonder.SetResourceDict(wonderData.wonderCost, false);
        wonder.unloadLoc = finalUnloadLoc;
        AddTradeLoc(finalUnloadLoc, wonder.wonderName);
        wonderNoWalkLoc.Remove(finalUnloadLoc);
        wonder.roadPreExisted = IsRoadOnTerrain(finalUnloadLoc);
        //wonder.Rotation = rotation;
        wonder.SetCenterPos(centerPos);
        wonderConstructionDict[wonder.wonderName] = wonder;
        foreach (Vector3Int tile in wonderPlacementLoc)
            wonderStopDict[tile] = wonder;

        //building road in unload area
        if (!wonder.roadPreExisted)
        {
            int level = 1;

            for (int i = 0; i < neighborsEightDirectionsIncrement.Count; i++)
            {
                if (IsRoadOnTerrain(neighborsEightDirectionsIncrement[i] + finalUnloadLoc))
                    level = Math.Max(GetRoadLevel(neighborsEightDirectionsIncrement[i] + finalUnloadLoc), level);            
            }

            roadManager.BuildRoadAtPosition(finalUnloadLoc, level);
        }

        //claiming the area for the wonder
        List<Vector3Int> harborTiles = new();
        foreach (Vector3Int tile in wonderPlacementLoc)
        {
            AddToCityLabor(tile, null); //so cities can't take the spot
            AddStructure(tile, wonderGO); //so nothing else can be built there
            //AddStructureMap(tile, wonderMapIcon);
            //mapPanel.SetTileSprite(tile, TerrainDesc.Wonder);

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
                        wonder.CoastTiles.Add(neighbor + newCoastFinder);
                        if (newCoastFinder.x != 0)
                        {
                            coastCoastList.Add(neighbor + newCoastFinder + new Vector3Int(0, 0, 1));
                            wonder.CoastTiles.Add(neighbor + newCoastFinder + new Vector3Int(0, 0, 1));
                            coastCoastList.Add(neighbor + newCoastFinder + new Vector3Int(0, 0, -1));
                            wonder.CoastTiles.Add(neighbor + newCoastFinder + new Vector3Int(0, 0, -1));
                        }
                        else if (newCoastFinder.z != 0)
                        {
						    coastCoastList.Add(neighbor + newCoastFinder + new Vector3Int(1, 0, 0));
                            wonder.CoastTiles.Add(neighbor + newCoastFinder + new Vector3Int(1, 0, 0));
                            coastCoastList.Add(neighbor + newCoastFinder + new Vector3Int(-1, 0, 0));
                            wonder.CoastTiles.Add(neighbor + newCoastFinder + new Vector3Int(-1, 0, 0));
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

        wonder.PossibleHarborLocs = harborTiles;

        noWalkList.AddRange(wonderNoWalkLoc);
        wonderPlacementLoc.Clear();
        wonderNoWalkLoc.Clear();
        cityBuilderManager.PlayBoomAudio();
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
    		GameObject wonderGO = Instantiate(wonderData.wonderPrefab, data.centerPos, data.rotation);
    		wonderGO.gameObject.transform.SetParent(wonderHolder, false);
		    Wonder wonder = wonderGO.GetComponent<Wonder>();
            wonder.LoadData(data);
            allWonders.Add(wonder);
		    wonder.SetReferences(this, cityBuilderManager.focusCam);
		    wonder.WonderData = wonderData;
		    wonder.SetPrefabs(true);
    		wonder.SetResourceDict(wonderData.wonderCost, true);
		    AddTradeLoc(data.unloadLoc, wonder.wonderName);
		    wonder.SetCenterPos(data.centerPos);
    		wonderConstructionDict[wonder.wonderName] = wonder;
	
            foreach (Vector3Int tile in data.wonderLocs)
			    wonderStopDict[tile] = wonder;

		    //building road in unload area
            if (data.isConstructing)
            {
    			roadManager.BuildRoadAtPosition(data.unloadLoc, 1);

                if (data.hasHarbor)
                    cityBuilderManager.LoadWonderHarbor(data.harborLoc, wonder);
            }
            else
            {
                wonder.MeshCheck();
                wonder.DestroyParticleSystems();
                wonder.ApplyWonderCompletionReward();
            }

			//claiming the area for the wonder
			foreach (Vector3Int tile in wonderPlacementLoc)
			{
				AddToCityLabor(tile, null); //so cities can't take the spot
				AddStructure(tile, wonderGO); //so nothing else can be built there
			}
    
            noWalkList.AddRange(wonderNoWalkLoc);

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

        if (!newUnitsAndImprovements.Contains(wonderName))
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

    public bool GetWondersConstruction(string name)
    {
        return wonderConstructionDict.Keys.ToList().Contains(name);
    }

 //   public void AddToWondersDict(Vector3Int harborLoc, Wonder wonder)
 //   {
	//	wonderStopDict[harborLoc] = wonder;
	//}

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

    //Research info
    public void OpenResearchTree()
    {
		//if (workerOrders)
		//    return;
		cityBuilderManager.PlaySelectAudio();

		if (unitOrders || buildingWonder)
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

    //city improvement tooltip
    public void OpenImprovementTooltip(CityImprovement improvement)
    {
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

	public void CloseTradeRouteBeginTooltip()
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

    public void SetResearchName(string name)
    {
        uiWorldResources.SetResearchName(name);
    }

    public void AddToResearchWaitList(ResourceProducer producer)
    {
        if (!researchWaitList.Contains(producer))
            researchWaitList.Add(producer);
    }

    public void RemoveFromResearchWaitList(ResourceProducer producer)
    {
        if (researchWaitList.Contains(producer))
            researchWaitList.Remove(producer);
    }

    public void RestartResearch()
    {
        List<ResourceProducer> producerResearchWaitList = new(researchWaitList);

        for (int i = 0; i < producerResearchWaitList.Count; i++)
        {
			if (researching)
			{
                researchWaitList.Remove(producerResearchWaitList[i]);
				producerResearchWaitList[i].CheckProducerResearchWaitList();
			}
		}
    }

    public bool CitiesResearchWaitingCheck()
    {
        return researchWaitList.Count > 0;
    }

    public void AddToGoldCityWaitList(City city, bool trader)
    {
        if (trader)
        {
			if (!goldCityRouteWaitList.Contains(city))
				goldCityRouteWaitList.Add(city);
		}
        else
        {
            if (!goldCityWaitList.Contains(city))
                goldCityWaitList.Add(city);
        }
        
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

    private bool WondersWaitingCheck()
    {
        return goldWonderWaitList.Count > 0;
    }

    public void AddToGoldTradeCenterWaitList(TradeCenter tradeCenter)
    {
        if (!goldTradeCenterWaitList.Contains(tradeCenter))
            goldTradeCenterWaitList.Add(tradeCenter);
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

    public void RestartCityRoutes()
    {
        List<City> traderWaitList = new(goldCityRouteWaitList);

        for (int i = 0; i < traderWaitList.Count; i++)
        {
			goldCityRouteWaitList.Remove(traderWaitList[i]);
            traderWaitList[i].ResourceManager.CheckTraderWaitList(ResourceType.Gold);
		}
    }

    private bool TraderWaitingCheck()
    {
        return goldCityRouteWaitList.Count > 0;
    }

    public void RemoveTraderFromWaitList(City city)
    {
        goldCityRouteWaitList.Remove(city);
    }

    //lights in the world
    public void ToggleWorldLights(bool v)
    {
        foreach (TradeCenter center in tradeCenterDict.Values)
        {
            center.ToggleLights(v);
        }

        foreach (Wonder wonder in wonderConstructionDict.Values)
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
    public void UpdateWorldResources(ResourceType resourceType, int amount)
    {
        if (amount == 0)
            return;
        
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
                if (TraderWaitingCheck())
                    RestartCityRoutes();

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
            else if (uiCampTooltip.EnemyScreenActive())
                uiCampTooltip.UpdateBattleCostCheck(currentAmount, ResourceType.Gold);
            else if (uiTradeRouteBeginTooltip.activeStatus)
                uiTradeRouteBeginTooltip.UpdateRouteCost(currentAmount, ResourceType.Gold);
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

    public int GetWorldGoldLevel()
    {
        return worldResourceManager.GetWorldGoldLevel();
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

    public void SetResearchBackground(bool complete)
    {
        uiWorldResources.SetResearchBackground(complete);
    }

    public bool ResourceCheck(ResourceType type)
    {
        return resourceDiscoveredList.Contains(type);
    }

    public void LoadDiscoveredResources()
    {
        for (int i = 0; i < resourceDiscoveredList.Count; i++)
        {
            UpdateResourceSelectionGrids(resourceDiscoveredList[i]);
            cityBuilderManager.uiMarketPlaceManager.UpdateMarketPlaceManager(resourceDiscoveredList[i]);
		}
    }

    public void DiscoverResource(ResourceType type)
    {
        AddToDiscoverList(type);
        UpdateResourceSelectionGrids(type);
		cityBuilderManager.uiMarketPlaceManager.UpdateMarketPlaceManager(type);

		foreach (City city in cityDict.Values)
			city.ResourceManager.UpdateDicts(type);
	}

    //ambush logic
    public void SetUpAmbush(Vector3Int loc, Unit unitTrader)
    {
        //only one ambush per tile at one time
        if (enemyAmbushDict.ContainsKey(loc))
            return;
        
        ambushes++;
        List<Vector3Int> randomLocs = new();

        if (unitTrader.trader.guarded)
        {
			Vector3Int guardLoc = RoundToInt(unitTrader.trader.guardUnit.transform.position);
			foreach (Vector3Int tile in GetNeighborsFor(loc, State.EIGHTWAY))
			{
                if (tile == guardLoc)
                    continue;

                randomLocs.Add(tile);
			}
		}
        else
        {
            randomLocs.Add(GetNeighborsFor(loc, State.EIGHTWAY)[UnityEngine.Random.Range(0,8)]);
        }

		Vector3Int ambushLoc = randomLocs[UnityEngine.Random.Range(0,randomLocs.Count)];
        //ambushLoc = new Vector3Int(9, 0, 26);
        UnitBuildDataSO ambushingUnit = UpgradeableObjectHolder.Instance.enemyUnitDict[ambushUnitDict[currentEra]];
        TerrainData td = GetTerrainDataAt(loc);
        if (td.treeHandler != null)
            td.ToggleTransparentForest(true);

		EnemyAmbush ambush = new();
		ambush.loc = loc;
        ambush.attackedTrader = unitTrader.name;

        //check for main player
        CheckMainPlayerLoc(loc);

		GameObject enemyGO = Instantiate(ambushingUnit.prefab, ambushLoc, rotation);
        enemyGO.name = ambushingUnit.unitDisplayName;
		enemyGO.transform.SetParent(enemyUnitHolder, false);

		Unit unit = enemyGO.GetComponent<Unit>();
        unit.SetMinimapIcon(enemyUnitHolder);
		if (td.CompareTag("Forest") || td.CompareTag("Forest Hill"))
			unit.marker.ToggleVisibility(true);

		Vector3 unitScale = unit.transform.localScale;
		AddUnitPosition(ambushLoc, unit);
		float scaleX = unitScale.x;
		float scaleZ = unitScale.z;
		unit.transform.localScale = new Vector3(scaleX, 0.1f, scaleZ);

        Vector3 lightBeamLoc = ambushLoc;
        lightBeamLoc.y += .01f;
        if (IsRoadOnTileLocation(ambushLoc))
            lightBeamLoc.y += .1f;

        unit.lightBeam.transform.position = lightBeamLoc;
        unit.lightBeam.Play();
		LeanTween.scale(enemyGO, unitScale, 0.5f).setEase(LeanTweenType.easeOutBack);

		unit.ambush = true;
		unit.SetReferences(this);
		unit.currentLocation = ambushLoc;
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
			unitTrader.trader.guardUnit.StopMovement();
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

    public void CheckMainPlayerLoc(Vector3Int loc, List<Vector3Int> route = null)
    {
		Vector3Int playerLoc = GetClosestTerrainLoc(mainPlayer.transform.position);
        int xDiff = Mathf.Abs(playerLoc.x - loc.x);
        int zDiff = Mathf.Abs(playerLoc.z - loc.z);

		if (xDiff < 4 && zDiff < 4)
		{
            if (mainPlayer.isBusy)
				unitMovement.workerTaskManager.ForceCancelWorkerTask();

			mainPlayer.StopPlayer();
			mainPlayer.exclamationPoint.SetActive(true);
			mainPlayer.runningAway = true;
			mainPlayer.isBusy = true;
			mainPlayer.stepAside = true;

			if (playerLoc - loc == Vector3Int.zero || (route != null && route.Contains(playerLoc)))
			{
				mainPlayer.StepAside(playerLoc, route);
			}
			else //look to watch
			{
				mainPlayer.Rotate(loc);
				scott.Rotate(loc);
				azai.Rotate(loc);
			}
		}
        else if (mainPlayer.runningAway && (xDiff > 12 || zDiff > 12))
        {
			mainPlayer.StopRunningAway();
			mainPlayer.stepAside = false;
		}
	}

    public void ClearAmbush(Vector3Int loc)
    {
        enemyAmbushDict.Remove(loc);
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

    public City GetEnemyCity(Vector3Int tile)
    {
        return enemyCityDict[tile];
    }

    public void RemoveWonder(Vector3Int tile)
    {
        wonderStopDict.Remove(tile);
    }

    public List<string> GetConnectedCityNames(Vector3Int unitLoc, bool bySea, bool isTrader)
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

            if (!isTrader && destination == unitLoc)
                continue;
            //check if trader can reach all destinations
            if (GridSearch.TraderMovementCheck(this, unitLoc, destination, bySea))
            {
                names.Add(name);
            } 
        }

        //trade center names third
        if (isTrader)
        {
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
        }

        return names;
    }

    public Wonder GetWonderByName(string wonder)
    {
        return wonderConstructionDict[wonder];
    }

    public TradeCenter GetTradeCenterByName(string center)
    {
        return tradeCenterDict[center];
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
        return cityImprovementDict[harborLocation].city;
    }

    public bool IsCityHarborOnTile(Vector3Int loc)
    {
        if (cityImprovementDict.ContainsKey(loc))
        {
            if (cityImprovementDict[loc].GetImprovementData.improvementName == "Harbor")
                return true;
            else
                return false;
        }
        else
        {
            return false;
        }
    }

	public bool IsCityAirportOnTile(Vector3Int loc)
	{
		if (cityImprovementDict.ContainsKey(loc))
		{
			if (cityImprovementDict[loc].GetImprovementData.improvementName == "Airport")
				return true;
			else
				return false;
		}
		else
		{
			return false;
		}
	}

	//public bool IsWonderHarborOnTile(Vector3Int loc)
	//{
	//    return wonderStopDict.ContainsKey(loc);
	//}

	public bool IsTradeCenterHarborOnTile(Vector3Int loc)
    {
        return tradeCenterStopDict.ContainsKey(loc);
    }

    public bool CheckIfStopStillExists(Vector3Int location)
    {
        Vector3Int loc;

        if (tradeLocDict.ContainsKey(location))
            loc = GetStopLocation(GetTradeLoc(location));
        else
            return false;

        if (cityDict.ContainsKey(loc))
        {
            return true;
        }
        else if (wonderStopDict.ContainsKey(loc))
        {
            return true;
        }
        else if (tradeCenterStopDict.ContainsKey(loc))
        {
            return true;
        }
        else if (cityImprovementDict.ContainsKey(loc) && cityImprovementDict[loc].GetImprovementData.improvementName == "Harbor" && cityImprovementDict[loc].city != null)
        {
            return true;
        }

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
        else if (cityImprovementDict.ContainsKey(loc))
        {
            return cityImprovementDict[loc].city.cityName;
        }
        else
        {
            return "";
        }
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
        if (!workerBusyLocations.Contains(loc))
            workerBusyLocations.Add(loc);
    }

    public void RemoveWorkerWorkLocation(Vector3Int loc)
    {
        if (workerBusyLocations.Contains(loc))
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

    public void SetSelectionCircleLocation(Vector3Int loc)
    {
        Vector3 pos = GetClosestTerrainLoc(loc);
        pos.y += 0.07f;
        selectionIcon.transform.position = pos;
        selectionIcon.SetActive(true);
    }

    public void HideSelectionCircles()
    {
        selectionIcon.SetActive(false);
    }

    public void AddToResourceSelectionGridList(UIResourceSelectionGrid selectionGrid)
    {
        resourceSelectionGridList.Add(selectionGrid);
    }

    public void AddToDiscoverList(ResourceType type)
    {
        if (resourceDiscoveredList.Contains(type))
            return;

        resourceDiscoveredList.Add(type);
        GameLoader.Instance.gameData.resourceDiscoveredList.Add(type);
    }

    public void UpdateResourceSelectionGrids(ResourceType type)
    {
        if (type == ResourceType.Gold || type == ResourceType.Research)
            return;
        
        for (int i = 0; i < resourceSelectionGridList.Count; i++)
        {
            resourceSelectionGridList[i].DiscoverResource(type);
        }
    }

    public void ToggleBarracksExclamation(bool v)
    {
        foreach (City city in cityDict.Values)
        {
            if (city.hasBarracks)
                GetCityDevelopment(city.barracksLocation).exclamationPoint.SetActive(v);
        }
    }

    public void HighlightCitiesWithBarracks(City homeCity)
    {
        foreach (City city in cityDict.Values)
        {
            if (city == homeCity)
                continue;

            if (city.hasBarracks && !city.army.isFull)
            {

                city.Select(Color.green);
            }
        }
    }

    public void UnhighlightCitiesWithBarracks()
    {
        foreach (City city in cityDict.Values)
        {
            if (city.hasBarracks)
                city.Deselect();
        }
    }

    public void HighlightCitiesAndWonders()
    {
        foreach (City city in cityDict.Values)
        {
            city.Select(Color.green);
        }

       foreach (Wonder wonder in wonderConstructionDict.Values)
        {
            wonder.EnableHighlight(Color.green, true);
        }
    }

    public void UnhighlightCitiesAndWonders()
    {
        foreach (City city in cityDict.Values)
        {
            city.Deselect();
        }

        foreach (Wonder wonder in wonderConstructionDict.Values)
        {
            wonder.DisableHighlight();
        }
    }

    public void HighlightCitiesAndWondersAndTradeCenters(bool bySea)
    {
		if (bySea)
        {
            foreach(City city in cityDict.Values)
            {
                if (city.hasHarbor)
                    GetCityDevelopment(city.harborLocation).EnableHighlight(Color.green);
            }

            foreach(Wonder wonder in wonderConstructionDict.Values)
            {
                if (wonder.hasHarbor)
                    wonder.harborImprovement.EnableHighlight(Color.green);
            }
        }
        else
        {
            foreach (City city in cityDict.Values)
			    city.Select(Color.green);

		    foreach (Wonder wonder in wonderConstructionDict.Values)
			    wonder.EnableHighlight(Color.green, true);

        }

        foreach (TradeCenter center in tradeCenterStopDict.Values)
        {
            if (center.isDiscovered)
                center.EnableHighlight(Color.green, true);
        }
	}

    public void UnhighlightCitiesAndWondersAndTradeCenters(bool bySea)
    {
		if (bySea)
        {
			foreach (City city in cityDict.Values)
			{
				if (city.hasHarbor)
					GetCityDevelopment(city.harborLocation).DisableHighlight();
			}

			foreach (Wonder wonder in wonderConstructionDict.Values)
			{
				if (wonder.hasHarbor)
					wonder.harborImprovement.DisableHighlight();
			}
		}
        else
        {
            foreach (City city in cityDict.Values)
			    city.Deselect();

		    foreach (Wonder wonder in wonderConstructionDict.Values)
			    wonder.DisableHighlight();
        }
        
		foreach (TradeCenter center in tradeCenterStopDict.Values)
        {
            if (center.isDiscovered)
    			center.DisableHighlight();
        }
	}

    public void HighlightTraders(bool bySea)
    {
        for (int i = 0; i < traderList.Count; i++)
        {
            if (traderList[i].followingRoute || traderList[i].bySea != bySea || traderList[i].guarded || traderList[i].isMoving)
                continue;

            traderList[i].Highlight(Color.white);
        }
    }

    public void UnhighlightTraders()
    {
        for (int i = 0; i < traderList.Count; i++)
            traderList[i].Unhighlight();
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

    //public void WakeUpCamp(Vector3Int campLoc, Unit target)
    //{
    //    enemyCampDict[campLoc].threatQueue.Enqueue(GetClosestTerrainLoc(target.transform.position));

    //    //determining which one is closest first        
    //    Unit closest = null;
    //    for (int i = 0; i < enemyCampDict[campLoc].UnitsInCamp.Count; i++)
    //    {
    //        Unit enemy = enemyCampDict[campLoc].UnitsInCamp[i];
    //        if (enemy.buildDataSO.unitType == UnitType.Ranged)
    //        {
    //            enemy.enemyAI.Attack();
    //            continue;
    //        }

    //        if (closest == null)
    //        {
    //            closest = enemy;
    //            continue;
    //        }

    //        if (Vector3.SqrMagnitude(target.transform.position - enemy.transform.position) < Vector3.SqrMagnitude(target.transform.position - closest.transform.position))
    //            closest = enemy;
    //    }

    //    if (closest != null)
    //        closest.enemyAI.WakeUp(target);
    //}

    public void CityBattleStations(Vector3Int cityLoc, Vector3Int attackLoc, Vector3Int targetZone, EnemyCamp camp)
    {
        if (!cityDict[cityLoc].hasBarracks)
            return;
        
        if (GetCityDevelopment(cityDict[cityLoc].barracksLocation).isTraining)
            cityBuilderManager.RemoveImprovement(cityDict[cityLoc].barracksLocation, GetCityDevelopment(cityDict[cityLoc].barracksLocation), cityDict[cityLoc], true, false);

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

        if (uiCampTooltip.activeStatus && uiCampTooltip.army == GetCity(cityLoc).army)
            unitMovement.CancelArmyDeployment();
        //ToggleCityMaterialClear(cityLoc, targetZone, true, false);
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

        if (unitMovement.deployingArmy)
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
            
            enemyCityDict[cityLoc].enemyCamp.threatLoc = armyLoc;
			enemyCityDict[cityLoc].enemyCamp.forward = (armyLoc - campLoc) / 3;
			enemyCityDict[cityLoc].enemyCamp.BattleStations(campLoc, enemyCityDict[cityLoc].enemyCamp.forward);
			enemyCityDict[cityLoc].StopSpawnCycle(true);
		}
        else
        {
			enemyCampDict[campLoc].threatLoc = armyLoc;
			enemyCampDict[campLoc].forward = (armyLoc - campLoc) / 3;
			enemyCampDict[campLoc].BattleStations(campLoc, enemyCampDict[campLoc].forward);
		}

  //      if (enemyCampDict.ContainsKey(campLoc) && enemyCampDict[campLoc].attackingArmy != null) //only get ready if army is intending to go attack
  //      {
  //          enemyCampDict[campLoc].threatLoc = armyLoc;
		//	enemyCampDict[campLoc].forward = (armyLoc - campLoc) / 3;
		//	enemyCampDict[campLoc].BattleStations(campLoc, enemyCampDict[campLoc].forward);
  //      }
  //      else if (enemyCityDict[campLoc].enemyCamp.attackingArmy != null)
  //      {
  //          enemyCityDict[campLoc].enemyCamp.threatLoc = armyLoc;
		//	enemyCityDict[campLoc].enemyCamp.forward = (armyLoc - campLoc) / 3;
		//	enemyCityDict[campLoc].enemyCamp.BattleStations(campLoc, enemyCityDict[campLoc].enemyCamp.forward);
  //          enemyCityDict[campLoc].StopSpawnCycle(true);
		//	//ToggleCityMaterialClear(campLoc, armyLoc, true, true);
		//}
    }

    public void EnemyCampReturn(Vector3Int loc)
    {
        if (enemyCampDict.ContainsKey(loc))
            enemyCampDict[loc].ReturnToCamp();
        else
            enemyCityDict[loc].enemyCamp.ReturnToCamp();
    }

	public void ToggleCityMaterialClear(Vector3Int enemyLoc, Vector3Int armyLoc, Vector3Int loc, Vector3Int targetLoc, bool v)
    {
        if (GetTerrainDataAt(loc).treeHandler != null)
			GetTerrainDataAt(loc).ToggleTransparentForest(v);
        
        if (GetTerrainDataAt(targetLoc).treeHandler != null)
            GetTerrainDataAt(targetLoc).ToggleTransparentForest(v);

        if (v)
        {
            if (battleLocs.Count == 0)
				battleCamera.SetActive(true);

			if (!battleLocs.Contains(armyLoc))
                battleLocs.Add(armyLoc);

			if (IsEnemyCityOnTile(enemyLoc))
			{
				foreach (Military unit in enemyCityDict[enemyLoc].enemyCamp.UnitsInCamp)
					unit.unitMesh.layer = LayerMask.NameToLayer("BattleLayer");
			}
			else
			{
				foreach (Military unit in enemyCampDict[enemyLoc].UnitsInCamp)
					unit.unitMesh.layer = LayerMask.NameToLayer("BattleLayer");
			}

			foreach (Military unit in cityDict[armyLoc].army.UnitsInArmy)
				unit.unitMesh.layer = LayerMask.NameToLayer("BattleLayer");
		}
		else
        {
			battleLocs.Remove(armyLoc);

			if (battleLocs.Count == 0)
				battleCamera.SetActive(false);

			if (IsEnemyCityOnTile(enemyLoc))
            {
                foreach (Military unit in enemyCityDict[enemyLoc].enemyCamp.UnitsInCamp)
                    unit.unitMesh.layer = LayerMask.NameToLayer("Enemy");
            }
            else
            {
			    foreach (Military unit in enemyCampDict[enemyLoc].UnitsInCamp)
				    unit.unitMesh.layer = LayerMask.NameToLayer("Enemy");
		    }

            foreach (Military unit in cityDict[armyLoc].army.UnitsInArmy)
			    unit.unitMesh.layer = LayerMask.NameToLayer("Agent");
        }




  //      List<Vector3Int> cityImprovementLocs;
  //      //doing all this so as to maintain combined meshes but still show buildings when attacking
  //      if (v)
  //      {
  //          if (enemy)
  //          {
  //              enemyCityDict[loc].subTransform.GetComponent<MeshRenderer>().sharedMaterial = atlasClear;
  //              cityImprovementLocs = enemyCityDict[loc].improvementMeshes.Keys.ToList();
  //              if (enemyCityDict[loc].activeCity)
  //                  cityBuilderManager.ResetCityUI();
  //          }
  //          else
  //          {
  //              cityDict[loc].subTransform.GetComponent<MeshRenderer>().sharedMaterial = atlasClear;
  //              cityImprovementLocs = cityDict[loc].improvementMeshes.Keys.ToList();
  //              if (cityDict[loc].activeCity)
  //                  cityBuilderManager.ResetCityUI();
  //          }

  //          cityBuilderManager.ToggleBuildingMaterial(loc, atlasSemiClear, false);

  //          foreach (Vector3Int tile in cityImprovementLocs)
  //          {
  //              if (tile == targetLoc)
  //              {
  //                  if (cityImprovementDict[tile].GetImprovementData.replaceTerrain)
  //                      GetTerrainDataAt(tile).ToggleTerrainMesh(true);

  //                  cityImprovementDict[tile].EnableMaterial(atlasSemiClear);
  //              }
  //              else
  //              {
  //                  cityImprovementDict[tile].ShowEmbiggenedMesh();
  //                  cityImprovementDict[tile].showing = true;
  //              }
  //          }
		//}
  //      else
  //      {
		//	if (enemy)
		//	{
		//		enemyCityDict[loc].subTransform.GetComponent<MeshRenderer>().sharedMaterial = atlasMain;
		//		cityImprovementLocs = enemyCityDict[loc].improvementMeshes.Keys.ToList();
		//	}
		//	else
		//	{
		//		cityDict[loc].subTransform.GetComponent<MeshRenderer>().sharedMaterial = atlasMain;
		//		cityImprovementLocs = cityDict[loc].improvementMeshes.Keys.ToList();
		//	}

		//	cityBuilderManager.ToggleBuildingMaterial(loc, atlasMain, true);

		//	foreach (Vector3Int tile in cityImprovementLocs)
		//	{
		//		if (tile == targetLoc)
		//		{
		//			if (cityImprovementDict[tile].GetImprovementData.replaceTerrain)
		//				GetTerrainDataAt(tile).ToggleTerrainMesh(false);

		//			cityImprovementDict[tile].EnableMaterial(atlasMain);
		//		}
		//		else
		//		{
		//			cityImprovementDict[tile].HideImprovement();
		//			cityImprovementDict[tile].showing = false;
		//		}
		//	}
		//}
    }

    public void BattleCamCheck(bool v)
    {
        if (battleLocs.Count > 0)
            battleCamera.SetActive(!v);
    }

    public void HighlightAllEnemyCamps()
    {
        foreach (Vector3Int tile in enemyCampDict.Keys)
        {
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
		//GameLoader.Instance.gameData.attackedEnemyBases[loc] = new();
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
		ParticleSystem deathSplash = Instantiate(this.deathSplash, loc, Quaternion.identity);
		//deathSplash.transform.position = transform.position;
        deathSplash.transform.rotation = Quaternion.Euler(rotation);
        deathSplash.Play();
    }

    public void PlayRemoveSplash(Vector3 loc)
    {
		ParticleSystem removeSplash = Instantiate(this.removeSplash, loc, Quaternion.identity);
		Vector3 rotation = new Vector3(-90, 0, 0);
		removeSplash.transform.rotation = Quaternion.Euler(rotation);
		removeSplash.Play();
	}

	public void PlayRemoveEruption(Vector3 loc)
    {
        ParticleSystem removeEruption = Instantiate(this.removeEruption, loc, Quaternion.identity);
        Vector3 rotation = new Vector3(-90, 0, 0);
        removeEruption.transform.rotation = Quaternion.Euler(rotation);
        removeEruption.Play();
    }

    public void RemoveEnemyCamp(Vector3Int loc, bool isCity)
    {
        TerrainData camp = GetTerrainDataAt(loc);
		camp.enemyCamp = false;
        camp.enemyZone = false;
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
                    Vector3Int borderLocation = enemyZones[i] - neighborTiles[j];
                    Vector3 borderPosition = neighborTiles[j];
					//borderPosition.y = -0.1f;
					Quaternion rotation = Quaternion.identity;

					if (borderPosition.x != 0)
					{
                        borderPosition.x += 0.5f * borderLocation.x;// (borderPosition.x / 3 * 0.99f);
						rotation = Quaternion.Euler(0, 90, 0); //only need to rotate on this one
						borderPosition.x -= borderLocation.x > 0 ? -.01f : .01f;
					}
					else if (borderPosition.z != 0)
					{
                        borderPosition.z += 0.5f * borderLocation.z;// (borderPosition.z / 3 * 0.99f);
						borderPosition.z -= borderLocation.z > 0 ? -.01f : .01f;
					}

					GameObject border = Instantiate(enemyBorder, borderPosition, rotation);
					border.transform.SetParent(terrainHolder, false);

                    if (!enemyBordersDict.ContainsKey(neighborTiles[j]))
                        enemyBordersDict[neighborTiles[j]] = new();

                    enemyBordersDict[neighborTiles[j]].Add(border);

					//world[neighborTiles[j]].enemyBorders.Add(border);
				}
			}

			TerrainData td = GetTerrainDataAt(enemyZones[i]);
            DestroyBorders(enemyZones[i]);
            //td.DestroyBorders();
            td.enemyZone = false;

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
                    world[tile].Reveal();
            }
            
            //playing god rays to show triumph
			ParticleSystem godRays = Instantiate(this.godRays);
			godRays.transform.position = loc + new Vector3(1, 3, 0);
			godRays.Play();

			City city = enemyCityDict[loc];
            city.PlaySelectAudio(cityBuilderManager.sunBeam);
			StartCoroutine(EnemyCityCampDestroyWait(loc));
		    
            if (city.activeCity)
			    cityBuilderManager.ResetCityUI();

            city.SetWorld(this); //reset dicts and add wonder benefits
            cityDict[loc] = city;
            city.gameObject.tag = "Player";
            city.cityNameField.SetOriginalBackground();
            city.StopSpawnCycle(false);
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
            else
            {
                city.AddCityNameToWorld();
            }

            unitMovement.workerTaskManager.SetCityBools(city, city.cityLoc);
			city.waterCount = city.hasFreshWater ? 9999 : 0;

			//give some food so pop don't start starving immediately
			city.ResourceManager.AddResource(ResourceType.Food, city.currentPop * 3);

            List<ResourceType> resourcesToAdd = new() { ResourceType.Lumber, ResourceType.Stone };
            for (int i = 0; i < resourcesToAdd.Count; i++)
                city.ResourceManager.AddResource(resourcesToAdd[i],UnityEngine.Random.Range(city.currentPop,city.currentPop * 4));

            GameLoader.Instance.RemoveEnemyCity(loc);
		}
        else
        {
            PlaceTreasureChest(loc, enemyCampDict[loc].forward);
            StartCoroutine(EnemyCampDestroyWait(loc));
            if (enemyCampDict[loc].campfire != null)
                Destroy(enemyCampDict[loc].campfire);
            Destroy(enemyCampDict[loc].minimapIcon);
            GameLoader.Instance.RemoveEnemyCamp(loc);
        }
	}

    private void PlaceTreasureChest(Vector3Int loc, Vector3Int forward)
    {
        Vector3Int rotationLoc = loc + forward;

		Vector3 direction = rotationLoc - loc;
		Quaternion rotation;
		if (direction == Vector3.zero)
			rotation = Quaternion.identity;
		else
			rotation = Quaternion.LookRotation(direction, Vector3.up);

        int amount = 0;
        for (int i = 0; i < enemyCampDict[loc].UnitsInCamp.Count; i++)
        {
            Vector2Int goldRange = enemyCampDict[loc].UnitsInCamp[i].buildDataSO.goldDropRange;
            amount += UnityEngine.Random.Range(goldRange.x, goldRange.y);
        }

        if (amount > 0)
        {
            GameObject chestGO = Instantiate(treasureChest, loc, rotation);
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

    public void LoadTreasureChest(Vector3Int placementLoc, int amount, Vector3 direction)
    {
		Quaternion rotation;
		if (direction == Vector3.zero)
			rotation = Quaternion.identity;
		else
			rotation = Quaternion.LookRotation(direction, Vector3.up);

		GameObject chestGO = Instantiate(treasureChest, placementLoc, rotation);
		TreasureChest chest = chestGO.GetComponent<TreasureChest>();
		chest.amount = amount;

		treasureLocs[placementLoc] = chest;
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
			UpdateWorldResources(ResourceType.Gold, treasureLocs[tile].amount);
			InfoResourcePopUpHandler.CreateResourceStat(tile, treasureLocs[tile].amount, ResourceHolder.Instance.GetIcon(ResourceType.Gold));
            Destroy(treasureLocs[tile].gameObject);
			treasureLocs.Remove(tile);
			GameLoader.Instance.gameData.treasureLocs.Remove(tile);

            if (player)
                mainPlayer.PlayRingAudio();
            else
                cityBuilderManager.PlayRingAudio();

            PlayResourceSplash(tile);

			return true;
        }
        else
        {
            return false;
        }
    }

    public void PlayResourceSplash(Vector3Int loc)
    {
        Instantiate(resourceSplash, loc, Quaternion.Euler(-90, UnityEngine.Random.Range(0,360), 0));
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

    public UnitBuildDataSO GetUnitUpgradeData(string nameAndLevel)
    {
        return upgradeableUnitDataDict[nameAndLevel];
    }

    public ImprovementDataSO GetImprovementData(string nameAndLevel)
    {
        return improvementDataDict[nameAndLevel];
    }

    public UnitBuildDataSO GetUnitBuildData(string nameAndLevel)
    {
        return unitBuildDataDict[nameAndLevel];
    }

    //public Sprite GetResourceIcon(ResourceType resourceType)
    //{
    //    return resourceSpriteDict[resourceType];
    //}

    //public Dictionary<ResourceType, int> GetDefaultResourcePrices()
    //{
    //    return defaultResourcePriceDict;
    //}

    //public Dictionary<ResourceType, int> GetBlankResourceDict()
    //{
    //    return blankResourceDict;
    //}

    //public Dictionary<ResourceType, bool> GetBoolResourceDict()
    //{
    //    return boolResourceDict; 
    //}

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

    public void SetCityBuilding(CityImprovement improvement, ImprovementDataSO improvementData, Vector3Int cityTile, GameObject building, City city, string buildingName)
    {
        //CityImprovement improvement = building.GetComponent<CityImprovement>();
        improvement.building = improvementData.isBuilding;
        improvement.InitializeImprovementData(improvementData);
        //string buildingName = improvementData.improvementName;
        improvement.city = city;
        improvement.transform.parent = city.transform;
        city.workEthic += improvementData.workEthicChange;
        city.improvementWorkEthic += improvementData.workEthicChange;
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

    //public void SetCityHarbor(City city, Vector3Int harborLoc)
    //{
    //    cityDict[city.cityLoc].harborLocation = harborLoc;
    //}

    public void AddLocationToQueueList(Vector3Int location, Vector3Int cityLoc)
    {
        cityImprovementQueueList[location] = cityLoc;
    }

    public bool CheckQueueLocation(Vector3Int location)
    {
        return cityImprovementQueueList.ContainsKey(location);
    }

    //public City GetQueuedImprovementCity(Vector3Int loc)
    //{
    //    return GetCity(cityImprovementQueueList[loc]);
    //}

    public void RemoveLocationFromQueueList(Vector3Int location)
    {
        cityImprovementQueueList.Remove(location);  
    }

    //public void RemoveQueueGhostImprovement(Vector3Int location, City city)
    //{
    //    cityBuilderManager.RemoveQueueGhostImprovement(location, city);
    //}

    public void RemoveQueueItemCheck(Vector3Int location)
    {
		if (cityImprovementQueueList.ContainsKey(location))
        {
            City city = GetCity(cityImprovementQueueList[location]);
            Vector3Int localLocation = location - city.cityLoc;

            if (city.activeCity && cityBuilderManager.uiQueueManager.activeStatus)
            {
                QueueItem item = city.improvementQueueDict[localLocation];
                cityBuilderManager.RemoveQueueGhostImprovement(item);

				List<UIQueueItem> tempQueueItemList = new(cityBuilderManager.uiQueueManager.uiQueueItemList);
				for (int i = 0; i < tempQueueItemList.Count; i++)
				{
					if (tempQueueItemList[i].item.queueLoc == item.queueLoc)
					{
                        cityBuilderManager.uiQueueManager.RemoveFromQueue(tempQueueItemList[i], city.cityLoc);
						break;
					}
				}
            }
            else
            {
    			city.RemoveFromQueue(localLocation);
            }
        }
    }

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
        return cityDict.ContainsKey(tile);
    }

    public bool IsWonderOnTile(Vector3Int tile)
    {
        return wonderStopDict.ContainsKey(tile);
    }

    public bool IsTradeCenterOnTile(Vector3Int tile)
    {
        return tradeCenterStopDict.ContainsKey(tile);
    }

    public bool IsEnemyCityOnTile(Vector3Int tile)
    {
        return enemyCityDict.ContainsKey(tile);
    }

    public bool IsTradeLocOnTile(Vector3Int tile)
    {
        return tradeLocDict.ContainsKey(tile);
    }

    public bool IsBuildLocationTaken(Vector3Int buildLoc)
    {
        return buildingPosDict.ContainsKey(buildLoc);
    }

    public bool IsUnitLocationTaken(Vector3Int unitPosition)
    {
        return unitPosDict.ContainsKey(unitPosition);
    }

    //public bool IsBuildingInCity(Vector3Int cityTile, string buildingName)
    //{
    //    return cityBuildingGODict[cityTile].ContainsKey(buildingName);
    //}

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
		}

        roadManager.CombineMeshes();
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
            if (CheckIfSeaPositionIsValid(newTile))
                return newTile;
		}
        else
        {
            if (CheckIfPositionIsValid(newTile))
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
                    if (CheckIfSeaPositionIsValid(trySpot))
                        break;
                }
                else
                {
					if (CheckIfPositionIsValid(trySpot))
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
    public bool CheckIfPositionIsValid(Vector3Int tile)
    {
        return world.ContainsKey(tile) && world[tile].isDiscovered && world[tile].walkable && !noWalkList.Contains(tile);
    }

	public bool CheckIfAmphibuousPositionIsValid(Vector3Int tile)
    {
		return world.ContainsKey(tile) && (world[tile].walkable || !world[tile].isLand);
	}

	public bool CheckIfPositionIsMarchable(Vector3Int tile)
    {
		return world.ContainsKey(tile) && world[tile].isDiscovered && world[tile].walkable;
	}

    public bool CheckForFinalMarch(Vector3Int tile)
    {
        return !world[tile].isDiscovered || world[tile].terrainData.terrainDesc == TerrainDesc.Mountain;
    }

    public bool CheckIfPositionIsValidForEnemy(Vector3Int tile)
    {
		return world.ContainsKey(tile) && world[tile].walkable && !noWalkList.Contains(tile);
	}

    public bool CheckIfPositionIsMarchableForEnemy(Vector3Int tile)
    {
		return world.ContainsKey(tile) && world[tile].walkable && !enemyCampDict.ContainsKey(tile);
	}

	public bool CheckIfPositionIsSailableForEnemy(Vector3Int tile)
	{
		return world.ContainsKey(tile) && world[tile].sailable && !enemyCampDict.ContainsKey(tile);
	}

	public bool CheckIfPositionIsArmyValid(Vector3Int tile) //preventing going diagonally
    {
		return world.ContainsKey(tile) && world[tile].walkable && !noWalkList.Contains(tile) && !world[tile].sailable && !world[tile].enemyZone;
	}

    public bool CheckIfPositionIsEnemyArmyValid(Vector3Int tile) //preventing going diagonally
	{ 
		return world.ContainsKey(tile) && world[tile].walkable && !world[tile].sailable;
	}

    public bool CheckIfSeaPositionIsValid(Vector3Int tile)
    {
        return world.ContainsKey(tile) && world[tile].isDiscovered && world[tile].sailable && !noWalkList.Contains(tile) && !world[tile].border;
    }

	public bool CheckIfSeaPositionIsValidForEnemy(Vector3Int tile)
	{
		return world.ContainsKey(tile) && world[tile].sailable && !noWalkList.Contains(tile);
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
        world.TryGetValue(tileWorldPosition, out TerrainData td);
        return td;
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

 //   private readonly static List<Vector3Int> neighborsUp = new()
 //   {
 //       new Vector3Int(-1,0,0), //left
 //       new Vector3Int(-1,0,1), //upper left
	//	new Vector3Int(0,0,1), //up
 //       new Vector3Int(1,0,1), //upper right
 //       new Vector3Int(1,0,0), //right
 //   };

	//private readonly static List<Vector3Int> neighborsDown = new()
	//{
 //       new Vector3Int(1,0,0), //right
 //       new Vector3Int(1,0,-1), //lower right
	//	new Vector3Int(0,0,-1), //down
 //       new Vector3Int(-1,0,-1), //lower left
 //       new Vector3Int(-1,0,0), //left
 //   };

	//private readonly static List<Vector3Int> neighborsRight = new()
	//{
	//	new Vector3Int(0,0,-1), //down
 //       new Vector3Int(1,0,-1), //lower right
 //       new Vector3Int(1,0,0), //right
 //       new Vector3Int(1,0,1), //upper right
	//	new Vector3Int(0,0,1), //up
 //   };

	//private readonly static List<Vector3Int> neighborsLeft = new()
	//{
	//	new Vector3Int(0,0,-1), //down
 //       new Vector3Int(-1,0,-1), //lower left
 //       new Vector3Int(-1,0,0), //left
 //       new Vector3Int(-1,0,1), //upper left
	//	new Vector3Int(0,0,1), //up
 //   };

	public enum State { FOURWAY, FOURWAYINCREMENT, EIGHTWAY, EIGHTWAYARMY, EIGHTWAYTWODEEP, EIGHTWAYINCREMENT, CITYRADIUS };

 //   public List<Vector3Int> GenerateForwardsTiles(Vector3Int forward)
 //   {
 //       List<Vector3Int> listToUse = new();

 //       if (forward.z == 1)
 //           listToUse = new(neighborsUp);
 //       else if (forward.z == -1)
 //           listToUse = new(neighborsDown);
 //       else if (forward.x == 1)
 //           listToUse = new(neighborsRight);
 //       else if (forward.x == -1)
 //           listToUse = new(neighborsLeft);

	//	return listToUse;
	//}

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

    public (List<Vector3Int>, List<Vector3Int>, List<Vector3Int>) GetCityRadiusFor(Vector3Int worldTilePosition) //two ring layer around specific city
    {
        List<Vector3Int> neighbors = new();
        List<Vector3Int> developed = new();
        List<Vector3Int> constructing = new();
        foreach (Vector3Int direction in cityRadius)
        {
            Vector3Int checkPosition = worldTilePosition + direction;
            if (world.ContainsKey(checkPosition)) //checking if it exists in world
            {
                if (cityWorkedTileDict.ContainsKey(checkPosition) && GetCityLaborForTile(checkPosition) != worldTilePosition)
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

            if (world.ContainsKey(neighbor) && CheckIfTileIsImproved(neighbor))
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
                
                if (roadTileDict.ContainsKey(neighbor) && GetTerrainDataAt(neighbor).isLand)
                {
					int[] neighborRoads = { 0, 0, 0, 0 };

					for (int j = 0; j < neighborsFourDirectionsIncrement.Count; j++)
					{
						if (roadTileDict.ContainsKey(neighbor + neighborsFourDirectionsIncrement[j]))
							neighborRoads[j] = 1;
					}

					neighbors.Add((neighbor, true, neighborRoads));
                    straightRoads[i / 2] = 1;
                }
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

    public void AddTradeCenterName(GameObject nameMap)
    {
        cityNamesMaps.Add(nameMap);
    }

    public void AddCity(Vector3 buildPosition, City city)
    {
        Vector3Int position = Vector3Int.RoundToInt(buildPosition);
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
        //cityBuildingCurrentWorkedDict[cityTile] = new Dictionary<string, int>();
        //cityBuildingMaxWorkedDict[cityTile] = new Dictionary<string, int>();
        cityBuildingList[cityTile] = new List<string>();
        //cityBuildingIsProducer[cityTile] = new Dictionary<string, ResourceProducer>();
    }

    //public int CityCount()
    //{
    //    return cityNameDict.Count;
    //}

    public void RemoveCityBuilding(Vector3Int cityTile, string buildingName) 
    {
        cityBuildingGODict[cityTile].Remove(buildingName);
        cityBuildingDict[cityTile].Remove(buildingName);
        //cityBuildingCurrentWorkedDict[cityTile].Remove(buildingName);
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
            bool isHill = GetTerrainDataAt(buildPosition).isHill;
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

            cityDict.Remove(buildPosition);
        }
    }

    public void RemoveConstruction(Vector3Int tile)
    {
        cityImprovementConstructionDict.Remove(tile);   
    }

    //public void RemoveHarbor(Vector3Int harborLoc)
    //{
    //    cityDict.Remove(harborLoc);
    //}

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

    //public void AddToCurrentBuildingLabor(Vector3Int cityTile, string buildingName, int current)
    //{
    //    cityBuildingCurrentWorkedDict[cityTile][buildingName] = current;
    //}

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

    public void AddToCityLabor(Vector3Int pos, Vector3Int? cityLoc)
    {
        cityWorkedTileDict[pos] = cityLoc;
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

    //public int GetCurrentLaborForBuilding(Vector3Int cityTile, string buildingName)
    //{
    //    if (cityBuildingCurrentWorkedDict[cityTile].ContainsKey(buildingName))
    //        return cityBuildingCurrentWorkedDict[cityTile][buildingName];
    //    return 0;
    //}

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
        if (cityName == null)
            return false;
        
        return cityNameDict.ContainsKey(cityName);
    }

    public bool CheckWonderName(string wonderName)
    {
        if (wonderName == null)
            return false;

        return wonderConstructionDict.ContainsKey(wonderName);
    }

    public bool CheckTradeCenterName(string centerName)
    {
        if (centerName == null)
            return false;

        return tradeCenterDict.ContainsKey(centerName);
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

    //public void RemoveFromBuildingCurrentWorked(Vector3Int cityTile, string buildingName)
    //{
    //    if (cityBuildingCurrentWorkedDict[cityTile].ContainsKey(buildingName))
    //    {
    //        cityBuildingCurrentWorkedDict[cityTile].Remove(buildingName);
    //    }
    //}

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

    public void CityCanvasCheck()
    {
        bool turnOff = true;

        if (cityBuilderManager.uiWonderSelection.activeStatus)
            turnOff = false;
        if (cityBuilderManager.uiCityTabs.activeStatus)
            turnOff = false;

		if (turnOff)
			cityCanvas.gameObject.SetActive(false);
	}

    public void ImmoveableCheck()
    {
        bool turnOff = true;

        if (cityBuilderManager.activeBuilderHandler != null)
            turnOff = false;
		if (uiMainMenu.activeStatus)
			turnOff = false;
		if (researchTree.activeStatus)
			turnOff = false;
		if (wonderHandler.activeStatus)
			turnOff = false;
        if (uiConversationTaskManager.activeStatus)
			turnOff = false;

		if (turnOff)
            immoveableCanvas.gameObject.SetActive(false);
    }

    public void GoToNext()
    {
        if (unitMovement.selectedTrader != null)
        {
            int indexOf = traderList.IndexOf(unitMovement.selectedTrader);
            unitMovement.ClearSelection();
            int nextIndex = indexOf + 1;

            if (nextIndex == traderList.Count)
                nextIndex = 0;
            
            unitMovement.PrepareMovement(traderList[nextIndex], true);
        }
        else
        {
			int indexOf = laborerList.IndexOf(unitMovement.selectedUnit.GetComponent<Laborer>());
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
                    cityBuilderManager.uiCityTabs.builderUI.UpdateProducedNumbers(cityBuilderManager.SelectedCity.ResourceManager);
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
					cityBuilderManager.uiCityTabs.builderUI.UpdateProducedNumbers(cityBuilderManager.SelectedCity.ResourceManager);
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
					cityBuilderManager.uiCityTabs.builderUI.UpdateProducedNumbers(cityBuilderManager.SelectedCity.ResourceManager);
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
					cityBuilderManager.uiCityTabs.builderUI.UpdateProducedNumbers(cityBuilderManager.SelectedCity.ResourceManager);
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
        if (unitsSpeedChangeDict.ContainsKey(unit.buildDataSO.unitName))
            unit.originalMoveSpeed *= 1f + unitsSpeedChangeDict[unit.buildDataSO.unitName];
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
		GameObject resourceIconGO = Instantiate(this.resourceIcon, new Vector3(0, 1.5f, -0.75f) + td.TileCoordinates, Quaternion.Euler(90, 0, 0));
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

        while (searching)
        {
            cityName = cityNamePool[UnityEngine.Random.Range(0, cityNamePool.Count)];
            
            if (!IsCityNameTaken(cityName))
                searching = false;
        }

        return cityName;
    }

    public void ToggleGiftGiving(NPC npc)
    {
        uiResourceGivingPanel.ToggleVisibility(true, false, false, npc);
		unitMovement.uiPersonalResourceInfoPanel.SetPosition(false, null);
	}

    public void StatsCheck(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.Food:
                food += amount;
                break;

            case ResourceType.Fish:
                food += amount;
                break;

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
   //         uiButtonHighlight.Stop();
   //         uiCircleHighlight.Stop();
   //         uiButtonHighlight.gameObject.SetActive(false);
			//uiCircleHighlight.gameObject.SetActive(false);
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
							if (CheckIfPositionIsValid(loc))
							{
								scottLoc = loc;
								break;
							}
						}
					}
					scott.transform.position = scottLoc;

					Vector3 goScale = scott.transform.localScale;
					AddUnitPosition(scottLoc, scott);
                    scott.currentLocation = scottLoc;
					scott.gameObject.SetActive(true);
					float scaleX = goScale.x;
					float scaleZ = goScale.z;
					scott.transform.localScale = new Vector3(scaleX, 0.1f, scaleZ);
					scott.lightBeam.Play();
					LeanTween.scale(scott.gameObject, goScale, 0.5f).setEase(LeanTweenType.easeOutBack);
					cityBuilderManager.PlayBoomAudio();
					scott.SetSomethingToSay("first_labor");
                    gameStep = "first_infantry";

                    if (tutorial)
                    {
                        ButtonFlashCheck();
    					StartCoroutine(WaitASecToSpeak(mainPlayer, 2, "tutorial3a"));
                    }
				}
				break;
            case "first_infantry":
                if (source != "Hunting Research Complete")
                    return;

				City azaiCity = null;
				foreach (Vector3Int tile in GetNeighborsFor(GetClosestTerrainLoc(mainPlayer.currentLocation), MapWorld.State.CITYRADIUS))
				{
					if (IsCityOnTile(tile))
					{
						azaiCity = GetCity(tile);
						break;
					}
				}

				Vector3Int azaiLoc = RoundToInt(mainPlayer.transform.position);
				if (azaiCity != null)
				{
					azaiLoc = azaiCity.cityLoc;
				}
				else
				{
					foreach (Vector3Int loc in GetNeighborsFor(RoundToInt(mainPlayer.transform.position), State.EIGHTWAY))
					{
						if (CheckIfPositionIsValid(loc))
						{
							azaiLoc = loc;
							break;
						}
					}
				}
				azai.transform.position = azaiLoc;

				Vector3 azaiScale = azai.transform.localScale;
                azai.currentLocation = azaiLoc;
				AddUnitPosition(azaiLoc, azai);
				azai.gameObject.SetActive(true);
				float azaiScaleX = azaiScale.x;
				float azaiScaleZ = azaiScale.z;
				azai.transform.localScale = new Vector3(azaiScaleX, 0.1f, azaiScaleZ);
				azai.lightBeam.Play();
				LeanTween.scale(azai.gameObject, azaiScale, 0.5f).setEase(LeanTweenType.easeOutBack);
				cityBuilderManager.PlayBoomAudio();
				azai.SetSomethingToSay("first_infantry");
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
            if (tutorialGoing)
            {
                switch (tutorialStep)
                {
                    case "just_landed":
                        if (source != "Build City")
                            return;

					    mainPlayer.SetSomethingToSay("tutorial1");
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
                        mainPlayer.SetSomethingToSay("tutorial2");
                        break;
                    case "tutorial2":
                        if (source != "Resource")
                            return;

                        if (food == 1)
                        {
                            ButtonFlashCheck();
                            mainPlayer.SetSomethingToSay("tutorial3");
                        }
                        else if (food > 1 && food < 5)
                        {
                            StartCoroutine(EnableButtonHighlight(unitMovement.uiWorkerTask.GetButton("Gather").transform, true));
                            unitMovement.uiWorkerTask.GetButton("Gather").isFlashing = true;
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
                        mainPlayer.SetSomethingToSay("tutorial4");
                        break;
                    case "tutorial4":
                        if (source != "Resource")
                            return;

						bool enoughLumber = false;
						foreach (City city in cityDict.Values)
						{
							if (city.ResourceManager.ResourceDict[ResourceType.Lumber] >= 10)
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
							StartCoroutine(EnableButtonHighlight(unitMovement.uiWorkerTask.GetButton("Gather").transform, true));
							unitMovement.uiWorkerTask.GetButton("Gather").isFlashing = true;
							return;
						}

                        ButtonFlashCheck();
                        mainPlayer.SetSomethingToSay("tutorial5");
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
                            if (cityBuilderManager.uiBuildingBuilder.buildOptions[i].BuildData.improvementName == "Housing")
                            {
    						    StartCoroutine(EnableButtonHighlight(cityBuilderManager.uiBuildingBuilder.buildOptions[i].transform, true, true));
                                cityBuilderManager.uiBuildingBuilder.buildOptions[i].isFlashing = true;
                                break;
                            }
                        }

						tutorialStep = "tutorial5b";
						break;
                    case "tutorial5b":
                        if (source != "Building Building")
                            return;

                        tutorialStep = "tutorial6";
                        mainPlayer.SetSomethingToSay("tutorial6");
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
                        mainPlayer.SetSomethingToSay("tutorial7");

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
							if (city.ResourceManager.ResourceDict[ResourceType.Stone] >= 10)
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
						mainPlayer.SetSomethingToSay("tutorial8", scott);
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
							if (cityBuilderManager.uiProducerBuilder.buildOptions[i].BuildData.improvementName == "Research")
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
                        if (source != "Finished Building Something")
                            return;

                        mainPlayer.SetSomethingToSay("tutorial9", scott);
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
                        mainPlayer.SetSomethingToSay("tutorial10", scott);
                        break;
                    case "tutorial10":
                        if (source != "Research")
                            return;

						tutorialStep = "tutorial11";
						mainPlayer.SetSomethingToSay("tutorial11", scott);
						break;
                    case "tutorial11":
                        if (source != "Resource")
                            return;

                        mainPlayer.SetSomethingToSay("tutorial12");
                        tutorialStep = "tutorial12";

                        break;
                    case "tutorial12":
                        if (source != "Agriculture Research Complete")
                            return;

                        mainPlayer.SetSomethingToSay("tutorial13", scott);
                        tutorialGoing = false;
                        tutorialStep = "";

                        break;
				}
            }
        }
    }

    private IEnumerator WaitASecToSpeak(Worker unit, int timeToWait, string conversationTopic)
    {
        playerInput.paused = true;
        yield return new WaitForSeconds(timeToWait);

        unit.SetSomethingToSay(conversationTopic);
    }

    public IEnumerator EnableButtonHighlight(Transform selection, bool button, bool big = false)
    {
		ButtonFlashCheck();

        //wait til end of frame to make sure everything is active
		yield return new WaitForEndOfFrame();

        buttonHighlight.transform.SetParent(selection, false);
        buttonHighlight.PlayFlash(button, big);
		flashingButton = true;
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
                        uiConversationTaskManager.CreateConversationTask("Tutorial");
                        tutorialGoing = true;
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
				if (number == 16)
				{
					scottFollow = true;
					scott.gameObject.tag = "Player";
                    scott.marker.gameObject.tag = "Player";
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
                if (number == 12)
                {
					azaiFollow = true;
                    azai.gameObject.tag = "Player";
                    azai.marker.gameObject.tag = "Player";
					characterUnits.Add(azai);

                    if (mainPlayer.isSelected)
                        azai.Highlight(Color.white);
				}
				break;
		}
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
    West
}