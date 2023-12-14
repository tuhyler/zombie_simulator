using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
//using static UnityEngine.RuleTile.TilingRuleOutput;

public class MapWorld : MonoBehaviour
{
    private string version = "0.1";
    private DateTime currentTime;
    [SerializeField]
    public HandlePlayerInput playerInput;
    [SerializeField]
    public Worker mainPlayer;
    [SerializeField]
    public Light startingSpotlight;
    [SerializeField]
    public Water water;
    [SerializeField]
    public GameObject resourceIcon, campfire, spotlight, dizzyMarker, speechBubble, unexploredTile;
    [SerializeField]
    public CameraController cameraController;
    [SerializeField]
    public Canvas immoveableCanvas, cityCanvas, workerCanvas, traderCanvas, tradeRouteManagerCanvas, infoPopUpCanvas, overflowGridCanvas;
    [HideInInspector]
    public bool tutorial, hideUI, tutorialGoing;
    [SerializeField]
    public DayNightCycle dayNightCycle;
    [SerializeField]
    public MeshFilter borderOne, borderTwoCorner, borderTwoCross, borderThree, borderFour;
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
    private RectTransform mapPanelButton, mainMenuButton;
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
    public UnitMovement unitMovement;
    [SerializeField]
    public CityBuilderManager cityBuilderManager;
    [SerializeField]
    public RoadManager roadManager;
    [SerializeField]
    private Material transparentMat;
    [SerializeField]
    private GameObject selectionIcon, enemyCampIcon, buildPanel, canvasHolder, enemyBorder;

    [SerializeField]
    private ParticleSystem lightBeam;

    [SerializeField]
    public Transform terrainHolder, cityHolder, wonderHolder, tradeCenterHolder, psHolder, enemyCityHolder, unitHolder, enemyUnitHolder, roadHolder, orphanImprovementHolder, objectPoolItemHolder;

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
    [HideInInspector]
    public Dictionary<string, TradeCenter> tradeCenterDict = new();
    private Dictionary<Vector3Int, TradeCenter> tradeCenterStopDict = new();

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
    [HideInInspector]
    public Dictionary<ResourceType, float> resourceStorageMultiplierDict = new();

	private Dictionary<Vector3Int, TerrainData> world = new();
    private Dictionary<Vector3Int, GameObject> buildingPosDict = new(); //to see if cities already exist in current location
    private List<Vector3Int> noWalkList = new(); //tiles where wonders are and units can't walk
    private List<Vector3Int> cityLocations = new();
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
    private Dictionary<Vector3Int, Unit> unitPosDict = new(); //to track unitGO locations
    [HideInInspector]
    public List<Laborer> laborerList = new();
    [HideInInspector]
    public List<Trader> traderList = new();
    private Dictionary<string, ImprovementDataSO> improvementDataDict = new();
    private Dictionary<string, UnitBuildDataSO> unitBuildDataDict = new();
    private Dictionary<string, int> upgradeableObjectMaxLevelDict = new();
    private Dictionary<string, List<ResourceValue>> upgradeableObjectPriceDict = new(); 
    private Dictionary<string, ImprovementDataSO> upgradeableObjectDataDict = new();
    private Dictionary<string, UnitBuildDataSO> upgradeableUnitDataDict = new();
    private Dictionary<ResourceType, Sprite> resourceSpriteDict = new();
    private Dictionary<ResourceType, int> defaultResourcePriceDict = new();
    private Dictionary<ResourceType, int> blankResourceDict = new();
    private Dictionary<ResourceType, bool> boolResourceDict = new();
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
    public int cityCount, infantryCount, rangedCount, cavalryCount, traderCount, boatTraderCount, food, lumber;
    [HideInInspector]
    public string tutorialStep;
    private bool flashingButton;

    //for when terrain runs out of resources
    [SerializeField]
    public TerrainDataSO grasslandTerrain, grasslandHillTerrain, desertTerrain, desertHillTerrain;

    public bool showGizmo, hideTerrain = true;

    [SerializeField]
    public AudioManager ambienceAudio;
    private AudioSource audioSource;

    [HideInInspector]
    public GamePersist gamePersist = new();


    private void Awake()
    {
        currentTime = DateTime.Now;
        tutorial = uiMainMenu.uiSettings.tutorial;
        uiSpeechWindow.AddToSpeakingDict("Camera", null);
        speechBubble.SetActive(false);
        audioSource = GetComponent<AudioSource>();

		foreach (ResourceIndividualSO resourceData in ResourceHolder.Instance.allStorableResources) //Enum.GetValues(typeof(ResourceType)) 
		{
			if (resourceData.resourceType == ResourceType.None)
				continue;
			resourceStorageMultiplierDict[resourceData.resourceType] = resourceData.resourceStorageMultiplier;
		}

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
            }
            
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

                upgradeableObjectMaxLevelDict[data.improvementName] = data.improvementLevel;
                upgradeableObjectPriceDict[upgradeableObjectName] = upgradeableObjectCost;
            }

            upgradeableObjectName = data.improvementNameAndLevel; //needs to be last to compare to following data
            upgradeableObjectTotalCost = data.improvementCost;
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
			if (!data.isSecondary)
            {
                GameObject buildPanelGO = Instantiate(buildPanel);
			
			    UIBuildOptions buildOption = buildPanelGO.GetComponent<UIBuildOptions>();
			    buildOption.UnitBuildData = data;
			    buildPanelGO.transform.SetParent(cityBuilderManager.uiUnitBuilder.objectHolder, false);
			    buildOption.SetBuildOptionData(cityBuilderManager.uiUnitBuilder);
            }

			unitBuildDataDict[data.unitNameAndLevel] = data;
            
            if (data.availableInitially)
                data.locked = false;
            else
                data.locked = true;
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
			upgradeableObjectTotalCost = data.unitCost;
			upgradeableObjectLevel = data.unitLevel;
		}

		cityBuilderManager.uiUnitBuilder.FinishMenuSetup();

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

        CreateParticleSystems();
        uiMainMenu.uiSaveGame.PopulateSaveItems();
        DeactivateCanvases();
        //CreateGrid(); //for alternative grid search method
        //if (gamePersist.loadNewGame)
        //{
        //    Debug.Log("loading new game");
        //    LoadData();
        //}
    }

    public void NewGamePrep(bool newGame, Dictionary<Vector3Int, TerrainData> terrainDict = null)
    {
		wonderButton.gameObject.SetActive(true);
		uiMainMenuButton.gameObject.SetActive(true);
        conversationListButton.gameObject.SetActive(true);
		uiWorldResources.SetActiveStatus(true);
		List<TerrainData> coastalTerrain = new();
		List<TerrainData> terrainToCheck = new();

        if (newGame)
        {
            NewMap(terrainDict);
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
			unitEnemy.SetReferences(this, cityBuilderManager.focusCam, cityBuilderManager.uiUnitTurn, cityBuilderManager.movementSystem);

			Vector3Int unitLoc = RoundToInt(unitEnemy.transform.position);
			if (!unitPosDict.ContainsKey(RoundToInt(unitLoc))) //just in case dictionary was missing any
				unitEnemy.CurrentLocation = AddUnitPosition(unitLoc, unitEnemy);

			unitEnemy.CurrentLocation = unitLoc;

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

			enemyCampDict[unitTerrainLoc].UnitsInCamp.Add(unitEnemy);
			unitEnemy.enemyAI.CampLoc = unitTerrainLoc;
			unitEnemy.enemyAI.CampSpot = unitLoc;
			unitEnemy.enemyCamp = enemyCampDict[unitTerrainLoc];


			if (hideTerrain)
            {
                unitEnemy.gameObject.SetActive(false);
            }
            else
            {
			    if (unitEnemy.buildDataSO.unitType != UnitType.Cavalry)
				    unitEnemy.ToggleSitting(true);
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
		}

		Unit unit = mainPlayer.GetComponent<Unit>();
        uiSpeechWindow.AddToSpeakingDict("Koa", mainPlayer);
		unit.SetReferences(this, cameraController, cityBuilderManager.uiUnitTurn, cityBuilderManager.movementSystem);

		unit.Reveal();
		Vector3Int unitPos = RoundToInt(unit.transform.position);
        //if (!unitPosDict.ContainsKey(RoundToInt(unitPos))) //just in case dictionary was missing any
        //	unit.CurrentLocation = AddUnitPosition(unitPos, unit);

        unit.CurrentLocation = unitPos;
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
	}

    private IEnumerator StartingAmbience()
    {
        yield return new WaitForSeconds(0.5f);

		ambienceAudio.AmbienceCheck();
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
		cityBuilderManager.uiUnitTurn.gameObject.SetActive(v);
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
			GameLoader.Instance.gameData.allTerrain[td.TileCoordinates] = td.SaveData();
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
	}

	public void GenerateMap(Dictionary<Vector3Int, TerrainSaveData> mainMap)
    {
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
                GameObject prop = Instantiate(terrainData.decors[data.decor], Vector3.zero, data.propRotation);
                prop.transform.SetParent(td.prop, false);
                td.SetProp();
                terrainPropsToModify.Add(td);

				if (terrainData.type == TerrainType.Forest || terrainData.type == TerrainType.ForestHill)
                {
					GameObject nonStaticProp = Instantiate(terrainData.decors[data.decor], Vector3.zero, Quaternion.identity);
                    nonStaticProp.transform.SetParent(td.nonstatic, false);
                    td.nonstatic.rotation = data.propRotation;
                    td.SetNonStatic();
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
        StartCoroutine(StartingAmbience());
	}

	public void SetWorldBoundaries(int maxX, int maxZ)
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

	public void GenerateTradeCenters(Dictionary<Vector3Int, TradeCenterData> centers)
    {
        foreach (TradeCenterData centerData in centers.Values)
        {
            GameObject go = Instantiate(UpgradeableObjectHolder.Instance.tradeCenterDict[centerData.name], centerData.mainLoc, centerData.rotation);
            go.transform.SetParent(tradeCenterHolder, false);
            TradeCenter center = go.GetComponent<TradeCenter>();
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
			if (hideTerrain)
				center.Hide();
			else
				center.isDiscovered = true;

			GameLoader.Instance.centerWaitingDict[center] = (centerData.waitList, centerData.seaWaitList);
		}
	}

	public void BuildCity(Vector3Int cityTile, TerrainData td, GameObject prefab, CityData data)
	{
        td.ShowProp(false);

		GameObject newCity = Instantiate(prefab, cityTile, Quaternion.identity);
		newCity.gameObject.transform.SetParent(cityHolder, false);
		AddStructure(cityTile, newCity); //adds building location to buildingDict
		City city = newCity.GetComponent<City>();
		city.SetWorld(this);
		city.SetNewCityName();
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
		improvement.loc = city.cityLoc;
		building.transform.parent = city.subTransform;

		string buildingName = buildingData.improvementName;
		SetCityBuilding(improvement, buildingData, city.cityLoc, building, city, buildingName);
		
        city.HousingCount += buildingData.housingIncrease;

		cityBuilderManager.CombineMeshes(city, city.subTransform, false);
		improvement.SetInactive();

		if (buildingData.singleBuild)
			city.singleBuildImprovementsBuildingsDict[buildingData.improvementName] = city.cityLoc;
	}

	public void CreateImprovement(City city, CityImprovementData data)
	{
        //spending resources to build
        ImprovementDataSO improvementData = UpgradeableObjectHolder.Instance.improvementDict[data.name];
        Vector3Int tempBuildLocation = data.location;
		Vector3Int buildLocation = tempBuildLocation;
		buildLocation.y = 0;

		//rotating harbor so it's closest to city
		int rotation = 0;
		if (improvementData.terrainType == TerrainType.Coast || improvementData.terrainType == TerrainType.River)
		{
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

		AddStructure(buildLocation, improvement);
		CityImprovement cityImprovement = improvement.GetComponent<CityImprovement>();
		cityImprovement.loc = buildLocation;
		cityImprovement.InitializeImprovementData(improvementData);
		//cityImprovement.SetPSLocs();
		cityImprovement.SetQueueCity(null);
		cityImprovement.building = improvementData.isBuilding;
		cityImprovement.SetCity(city);

		SetCityDevelopment(tempBuildLocation, cityImprovement);

        if (data.isConstruction)
    		improvement.SetActive(false);

		//setting single build rules
		if (improvementData.singleBuild)
		{
			city.singleBuildImprovementsBuildingsDict[improvementData.improvementName] = tempBuildLocation;
			AddToCityLabor(tempBuildLocation, city.cityLoc);
		}

		//resource production
		ResourceProducer resourceProducer = improvement.GetComponent<ResourceProducer>();
		buildLocation.y = 0;
		AddResourceProducer(buildLocation, resourceProducer);
		resourceProducer.SetResourceManager(city.ResourceManager);
		resourceProducer.InitializeImprovementData(improvementData, td.resourceType); //allows the new structure to also start generating resources
		resourceProducer.SetCityImprovement(cityImprovement);
		resourceProducer.SetLocation(tempBuildLocation);

        if (data.isConstruction)
        {
            cityImprovement.LoadData(data, city);
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
			cityImprovement.meshCity = city;
			cityImprovement.transform.parent = city.transform;
			city.AddToImprovementList(cityImprovement);

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

			city.AddToMeshFilterList(tempObject, meshes, false, tempBuildLocation);
			tempObject.transform.parent = city.transform;
			tempObject.SetActive(false);

			//resetting ground UVs is necessary
			if (improvementData.replaceTerrain)
			{
				td.HideTerrainMesh();

				foreach (MeshFilter mesh in cityImprovement.MeshFilter)
				{
					if (mesh.name == "Ground")
					{
						Vector2[] terrainUVs = td.UVs;
						Vector2[] newUVs = mesh.mesh.uv;
						Vector2[] finalUVs = NormalizeUVs(terrainUVs, newUVs);
						//mesh.mesh.uv = finalUVs;

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

			if (td.terrainData.type == TerrainType.Forest || td.terrainData.type == TerrainType.ForestHill)
				td.SwitchToRoad();
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
				city.hasHarbor = true;
				city.harborLocation = tempBuildLocation;
				//SetCityHarbor(city, tempBuildLocation);
                AddTradeLoc(tempBuildLocation, city.cityName);
			}
			else if (improvementData.improvementName == "Barracks")
			{
				city.hasBarracks = true;
				city.barracksLocation = tempBuildLocation;

				foreach (Vector3Int tile in GetNeighborsFor(tempBuildLocation, MapWorld.State.EIGHTWAYARMY))
					city.army.SetArmySpots(tile);

				city.army.SetLoc(tempBuildLocation, city);

                List<UnitData> militaryUnits = GameLoader.Instance.gameData.militaryUnits[tempBuildLocation];

				for (int i = 0; i < militaryUnits.Count; i++)
                {
                    CreateUnit(militaryUnits[i], city);
                }
			}

			//setting labor info (harbors have no labor)
			AddToMaxLaborDict(tempBuildLocation, improvementData.maxLabor);
			//if (city.AutoAssignLabor && city.cityPop.UnusedLabor > 0 && improvementData.maxLabor > 0)
			//	city.AutoAssignmentsForLabor();

			//removing areas to work
			foreach (Vector3Int loc in improvementData.noWalkAreas)
				AddToNoWalkList(loc + tempBuildLocation);

            cityImprovement.LoadData(data, city);
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
                enemyGO.transform.SetParent(enemyUnitHolder, false);
                if (!reveal)
                    enemyGO.SetActive(false);

                Unit unit = enemyGO.GetComponent<Unit>();
                if (tdCamp.CompareTag("Forest") || tdCamp.CompareTag("Forest Hill"))
                    unit.marker.gameObject.SetActive(true);
		        unit.SetReferences(this, cityBuilderManager.focusCam, cityBuilderManager.uiUnitTurn, cityBuilderManager.movementSystem);
		        if (!attacked) //just in case dictionary was missing any
			        unit.CurrentLocation = AddUnitPosition(unitLoc, unit);
		        unit.CurrentLocation = unitLoc;
                enemyCampDict[loc].UnitsInCamp.Add(unit);
		        unit.enemyAI.CampLoc = loc;
		        unit.enemyAI.CampSpot = unitLoc;
        		unit.enemyCamp = enemyCampDict[loc];

				if (attacked)
                {
                    //RemoveUnitPosition(unitLoc);
                    unit.LoadUnitData(fightingEnemies[unitLoc]);
                    AddUnitPosition(unit.CurrentLocation, unit);
                    if (camp.campfire != null)
                        camp.campfire.SetActive(false);
                }
                else if (reveal)
                {
                    if (unit.buildDataSO.unitType != UnitType.Cavalry)
                        unit.ToggleSitting(true);
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

    public void CreateUnit(IUnitData data, City city = null)
    {
        UnitBuildDataSO unitData = UpgradeableObjectHolder.Instance.unitDict[data.unitNameAndLevel];
		GameObject unitGO = unitData.prefab;

        if (data.secondaryPrefab)
            unitGO = unitData.secondaryPrefab;

		GameObject unit = Instantiate(unitGO, data.currentLocation, data.rotation); //produce unit at specified position
		unit.gameObject.transform.SetParent(unitHolder, false);
		Unit newUnit = unit.GetComponent<Unit>();
		newUnit.SetReferences(this, cameraController, cityBuilderManager.uiUnitTurn, cityBuilderManager.movementSystem);
		AddUnitPosition(data.currentLocation, newUnit);
		newUnit.SetMinimapIcon(unitHolder);

		//assigning army details and rotation
		if (newUnit.inArmy)
        {
            newUnit.homeBase = city;
			city.army.AddToArmy(newUnit);
            if (city.cityPop.CurrentPop == 0)
                city.StartGrowthCycle(true);
            city.army.AddToOpenSpots(data.barracksBunk);
        }

        if (newUnit.isWorker)
        {
            newUnit.GetComponent<Worker>().LoadWorkerData(data.GetWorkerData());
        }
        else if (newUnit.isTrader)
        {
            Trader trader = newUnit.GetComponent<Trader>();
            trader.SetRouteManagers(unitMovement.uiTradeRouteManager, unitMovement.uiPersonalResourceInfoPanel); //start method is too slow and awake is too fast
            traderList.Add(trader);
            trader.LoadTraderData(data.GetTraderData());
        }
        else if (newUnit.isLaborer)
        {
            Laborer laborer = newUnit.GetComponent<Laborer>();
            laborerList.Add(laborer);
            laborer.LoadLaborerData(data.GetLaborerData());
        }
        else
        {
            newUnit.LoadUnitData(data.GetUnitData());
        }
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
        texture.ReadPixels(new Rect((Screen.width - height) * 0.5f, (Screen.height - width) * 0.5f, texture.width, texture.height), 0, 0);
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

	public void AaddGold() //for testing, on a button
    {
        UpdateWorldResources(ResourceType.Gold, 100);
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
                if (GetTerrainDataAt(neighbor).terrainData.grassland || GetTerrainDataAt(neighbor).terrainData.desert) //grassland only fades edges for coast tiles
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

        int eulerAngle = Mathf.RoundToInt(td.main.eulerAngles.y);

        Vector2[] uvs = SetUVMap(grasslandCount, SetUVShift(desc), eulerAngle);
        if (td.UVs.Length > 4)
            uvs = NormalizeUVs(uvs, td.UVs);
        td.SetUVs(uvs);
	}

    private float SetUVShift(TerrainDesc desc)
    {
        float interval = .0625f; //256/4096
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
                shift = interval /** 3*/;
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
        CloseCampTooltipButton();
        CloseTradeRouteBeginTooltipButton();
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
            LeanTween.moveX(mainMenuButton, mainMenuButton.anchoredPosition3D.x + -400f, 0.5f).setEaseOutSine();
            LeanTween.moveX(uiTomFinder.allContents, uiTomFinder.allContents.anchoredPosition3D.x + -400f, 0.5f).setEaseOutSine();
		}
        else
        {
            LeanTween.moveX(mapHandler.minimapHolder, mapHandler.minimapHolder.anchoredPosition3D.x + 400f, 0.3f);
            LeanTween.moveX(mapHandler.minimapRing, mapHandler.minimapRing.anchoredPosition3D.x + 400f, 0.3f);
            LeanTween.moveX(wonderButton.allContents, wonderButton.allContents.anchoredPosition3D.x + 400f, 0.3f);
			LeanTween.moveX(conversationListButton.allContents, conversationListButton.allContents.anchoredPosition3D.x + 400f, 0.3f);
            LeanTween.moveX(mapPanelButton, mapPanelButton.anchoredPosition3D.x + 400f, 0.3f);
            LeanTween.moveX(mainMenuButton, mainMenuButton.anchoredPosition3D.x + 400f, 0.3f);
			LeanTween.moveX(uiTomFinder.allContents, uiTomFinder.allContents.anchoredPosition3D.x + 400f, 0.3f);
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

                td.HideTerrainMesh();
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
            roadManager.BuildRoadAtPosition(finalUnloadLoc);

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

					td.HideTerrainMesh();
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
    			roadManager.BuildRoadAtPosition(data.unloadLoc);

                if (data.hasHarbor)
                    cityBuilderManager.LoadWonderHarbor(data.harborLoc, wonder);
            }
            else
            {
                wonder.MeshCheck();
                wonder.DestroyParticleSystems();
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

        //trade center names third
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
        return cityImprovementDict[harborLocation].GetCity();
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
        else if (cityImprovementDict.ContainsKey(loc) && cityImprovementDict[loc].GetImprovementData.improvementName == "Harbor" && cityImprovementDict[loc].GetCity() != null)
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
            return cityImprovementDict[loc].GetCity().cityName;
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

	public EnemyCamp GetEnemyCamp(Vector3Int loc)
    {
        return enemyCampDict[loc];
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

    public void RevealEnemyCamp(Vector3Int loc)
    {
        if (enemyCampDict[loc].revealed)
            return;
        else
            enemyCampDict[loc].revealed = true;

        enemyCampDict[loc].campfire.SetActive(true);
        GameLoader.Instance.gameData.discoveredEnemyCampLocs.Add(loc);

		for (int i = 0; i < enemyCampDict[loc].UnitsInCamp.Count; i++)
		{
			Unit unit = enemyCampDict[loc].UnitsInCamp[i];
            unit.gameObject.SetActive(true);

			if (unit.buildDataSO.unitType != UnitType.Cavalry)
                unit.DiscoverSitting();
		}
    }

    public void BattleStations(Vector3Int campLoc, Vector3Int armyLoc)
    {
        if (enemyCampDict[campLoc].attackingArmy != null) //only get ready if army is intending to go attack
        {
            enemyCampDict[campLoc].threatLoc = armyLoc;
            enemyCampDict[campLoc].BattleStations();
        }
    }

    public void EnemyCampReturn(Vector3Int loc)
    {
    	enemyCampDict[loc].ReturnToCamp();
    }

    //public void MoveCamp(List<Vector3Int> leaderPath, Vector3Int campLoc, Unit leader, Unit target)
    //{
    //    foreach (Unit unit in enemyCampDict[campLoc].UnitsInCamp)
    //    {
    //        if (unit == leader)
    //            continue;

    //        Vector3Int leaderDiff = RoundToInt(unit.transform.position - leader.transform.position);
    //        unit.enemyAI.FollowLeader(leaderPath, leaderDiff, target);
    //    }
    //}

    //public void ConvergeCamp(Unit leader, Vector3Int campLoc)
    //{
    //    foreach (Unit unit in enemyCampDict[campLoc].UnitsInCamp)
    //    {
    //        if (unit == leader)
    //            continue;

    //        unit.enemyAI.Converge();
    //    }
    //}

    public void HighlightAllEnemyCamps()
    {
        foreach (Vector3Int tile in enemyCampDict.Keys)
        {
            TerrainData td = GetTerrainDataAt(tile);
            if (!td.isDiscovered)
                continue;

            td.EnableHighlight(Color.red);
            foreach (Unit unit in enemyCampDict[tile].UnitsInCamp)
                unit.SoftSelect(Color.red);
        }
    }

    public void HighlightEnemyCamp(Vector3Int loc, Color color)
    {
        if (!enemyCampDict.ContainsKey(loc))
            return;

        TerrainData td = GetTerrainDataAt(loc);
		td.DisableHighlight();
		td.EnableHighlight(color);
		foreach (Unit unit in enemyCampDict[loc].UnitsInCamp)
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
			foreach (Unit unit in enemyCampDict[tile].UnitsInCamp)
				unit.Deselect();
		}
	}

    public bool CheckIfEnemyAlreadyAttacked(Vector3Int loc)
    {
        return enemyCampDict[loc].attacked;
    }

    public void SetEnemyCampAsAttacked(Vector3Int loc, Army army)
    {
        enemyCampDict[loc].attacked = true;
        enemyCampDict[loc].attackingArmy = army;
        enemyCampDict[loc].forward = army.forward * -1;
        GameLoader.Instance.gameData.attackedEnemyBases[loc] = new();
    }

    public void RemoveEnemyCamp(Vector3Int loc)
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
		}

        StartCoroutine(EnemyCampDestroyWait(loc));
        if (enemyCampDict[loc].campfire != null)
            Destroy(enemyCampDict[loc].campfire);
        Destroy(enemyCampDict[loc].minimapIcon);
        GameLoader.Instance.RemoveEnemyCamp(loc);
	}

    private IEnumerator EnemyCampDestroyWait(Vector3Int loc)
    {
        yield return new WaitForSeconds(5);
        
        foreach (Unit unit in enemyCampDict[loc].DeadList)
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

    public void SetCityBuilding(CityImprovement improvement, ImprovementDataSO improvementData, Vector3Int cityTile, GameObject building, City city, string buildingName)
    {
        //CityImprovement improvement = building.GetComponent<CityImprovement>();
        improvement.building = improvementData.isBuilding;
        improvement.InitializeImprovementData(improvementData);
        //string buildingName = improvementData.improvementName;
        improvement.SetCity(city);
        improvement.transform.parent = city.transform;
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
        return world.ContainsKey(tile) && world[tile].isDiscovered && world[tile].walkable && !noWalkList.Contains(tile);
    }

    public bool CheckIfPositionIsArmyValid(Vector3Int tile)
    {
		return world.ContainsKey(tile) && world[tile].walkable && !noWalkList.Contains(tile) && !world[tile].sailable && !world[tile].enemyZone;
	}

    public bool CheckIfSeaPositionIsValid(Vector3Int tile)
    {
        return world.ContainsKey(tile) && world[tile].isDiscovered && world[tile].sailable && !noWalkList.Contains(tile);
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
        //cityBuildingCurrentWorkedDict[cityTile] = new Dictionary<string, int>();
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
		GameObject resourceIconGO = Instantiate(this.resourceIcon, new Vector3(0, 1.5f, -0.5f) + td.TileCoordinates, Quaternion.Euler(90, 0, 0));
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
    
    //if going through tutorial, goes through the steps of it 
    public void TutorialCheck(string source)
    {
        //only check if tutorial is currently on going
        if (tutorialGoing) 
        {
            if (tutorial)
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

                        Vector3Int playerLoc = mainPlayer.CurrentLocation;
						if (GetTerrainDataAt(playerLoc).resourceType != ResourceType.Lumber)
                            return;

                        foreach (Vector3Int loc in cityDict.Keys)
                        {
                            if (Mathf.Abs(playerLoc.x - loc.x) / 3 > 2 || Mathf.Abs(playerLoc.z - loc.z) / 3 > 2)
                                return;
                        }
                        
                        foreach (Vector3Int loc in cityDict.Keys)
                        {
                            foreach (Vector3Int tile in GetNeighborsFor(loc, State.CITYRADIUS))
                            {
                                GetTerrainDataAt(tile).DisableHighlight();
                            }
                        }

						ButtonFlashCheck();
						tutorialStep = "tutorial2";
                        mainPlayer.SetSomethingToSay("tutorial2");
                        break;
                    case "tutorial2":
                        if (source != "Resource")
                            return;

						if (lumber == 1)
                        {
							ButtonFlashCheck();
							mainPlayer.SetSomethingToSay("tutorial3");
						}
                        else if (lumber > 1 && lumber < 5)
                        {
							StartCoroutine(EnableButtonHighlight(unitMovement.uiWorkerTask.GetButton("Gather").transform, true));
                            unitMovement.uiWorkerTask.GetButton("Gather").isFlashing = true;
						}
                        else if (lumber == 5)
                        {
                            ButtonFlashCheck();
                            tutorialStep = "tutorial4";
							mainPlayer.SetSomethingToSay("tutorial4");
						}

						break;
                    case "tutorial4":
                        if (source != "Open City")
                            return;

                        StartCoroutine(EnableButtonHighlight(cityBuilderManager.uiCityTabs.GetTab("Buildings").transform, true));
                        cityBuilderManager.uiCityTabs.GetTab("Buildings").isFlashing = true;
						tutorialStep = "tutorial4a";

						foreach (Vector3Int tile in cityDict.Keys)
						{
							if (!cityDict[tile].activeCity)
                                cityDict[tile].Deselect();
						}

						break;
                    case "tutorial4a":
                        if (source != "Open Build Tab")
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

						tutorialStep = "tutorial4b";
						break;
                    case "tutorial4b":
                        if (source != "Building Something")
                            return;

                        tutorialStep = "tutorial5";
                        mainPlayer.SetSomethingToSay("tutorial5");
						break;
                    case "tutorial5":
                        if (source != "Finished Movement")
                            return;

						Vector3Int playerLoc2 = mainPlayer.CurrentLocation;
						if (GetTerrainDataAt(playerLoc2).resourceType != ResourceType.Food)
							return;

						foreach (Vector3Int loc in cityDict.Keys)
						{
							if (Mathf.Abs(playerLoc2.x - loc.x) / 3 > 2 || Mathf.Abs(playerLoc2.z - loc.z) / 3 > 2)
								return;
						}

						foreach (Vector3Int loc in cityDict.Keys)
						{
							foreach (Vector3Int tile in GetNeighborsFor(loc, State.CITYRADIUS))
							{
								GetTerrainDataAt(tile).DisableHighlight();
							}
						}

						tutorialStep = "tutorial6";
						mainPlayer.SetSomethingToSay("tutorial6");
						break;
                    case "tutorial6":
                        if (source != "Resource")
                            return;

                        City city = null;
						foreach (Vector3Int tile in GetNeighborsFor(GetClosestTerrainLoc(mainPlayer.CurrentLocation), MapWorld.State.CITYRADIUS))
						{
							if (IsCityOnTile(tile))
							{
                                city = GetCity(tile);
                                break;
							}
						}

                        if (city != null)
    						cityBuilderManager.CreateUnit(GetUnitBuildData("Laborer-1"), city, false);

                        StartCoroutine(WaitASecToSpeak(mainPlayer, 2, "tutorial7"));
						break;
                }
            }
        }
    }

    private IEnumerator WaitASecToSpeak(Unit unit, int timeToWait, string conversationTopic)
    {
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

					//finding closest lumber
					foreach (Vector3Int tile in GetNeighborsFor(RoundToInt(mainPlayer.transform.position), State.CITYRADIUS))
					{
						TerrainData td = GetTerrainDataAt(tile);
						if (td.resourceType == ResourceType.Lumber)
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
            case "tutorial4":
                if (number == 1)
                {
                    foreach (Vector3Int tile in cityDict.Keys)
                    {
                        cityDict[tile].Select(Color.green);
                    }
                }
                break;
            case "tutorial5":
                if (number == 1)
                {
                    //get closest city
                    City city = null;

                    // just get first one
                    foreach (City cities in cityDict.Values)
                    {
                        city = cities;
                        break;
                    }

                    if (city == null)
                    {
                        uiConversationTaskManager.CompleteTask("Tutorial", true);
                        return;
                    }
                    
                    foreach (Vector3Int tile in GetNeighborsFor(city.cityLoc, State.CITYRADIUS))
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
            case "tutorial6":
                if (number == 1)
                {
					StartCoroutine(EnableButtonHighlight(unitMovement.uiWorkerTask.GetButton("Gather").transform, true));
					unitMovement.uiWorkerTask.GetButton("Gather").isFlashing = true;
				}
                break;
        }
    }

    //public City FindVisibleCity()
    //{
    //    foreach (City city in cityDict.Values)
    //    {
    //        if (city.cityRenderer.isVisible && city.ImprovementList.Count > 0)
    //            return city;
    //    }

    //    return null;
    //}


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
