using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CityBuilderManager : MonoBehaviour
{
    [SerializeField]
    public UIWonderSelection uiWonderSelection;
    [SerializeField]
    public UICityBuildTabHandler uiCityTabs;
    [SerializeField]
    public UIMarketPlaceManager uiMarketPlaceManager;
    [SerializeField]
    public UIBuilderHandler uiUnitBuilder;
    [SerializeField]
    public UIResourceManager uiResourceManager;
    [SerializeField]
    public UIBuilderHandler uiRawGoodsBuilder;
    [SerializeField]
    public UIBuilderHandler uiProducerBuilder;
    [SerializeField]
    public UIBuilderHandler uiBuildingBuilder;
    [SerializeField]
    public UIQueueManager uiQueueManager;
    [SerializeField]
    public UIInfoPanelCity uiInfoPanelCity;
    [SerializeField]
    public UILaborAssignment uiLaborAssignment;
    [SerializeField]
    public UILaborHandler uiLaborHandler;
    [SerializeField]
    private UIImprovementBuildPanel uiImprovementBuildInfoPanel;
    [SerializeField]
    public UICityUpgradePanel uiCityUpgradePanel;
    [SerializeField]
    public UICityNamer uiCityNamer, uiTraderNamer;
    [SerializeField]
    public UITradeCenter uiTradeCenter;
    [SerializeField]
    public UILaborDestinationWindow uiLaborDestinationWindow;
    [HideInInspector]
    public UIHelperWindow uiHelperWindow;
    //[SerializeField]
    //private UICityResourceGrid uiCityResourceGrid;
    [SerializeField]
    public Button abandonCityButton;
    [SerializeField]
    private UIDestroyCityWarning uiDestroyCityWarning;
    [SerializeField]
    private Sprite upgradeButton, removeButton;

    //for labor prioritization auto-assignment menu
    [SerializeField]
    private Toggle autoAssign;
    [SerializeField]
    private Button openAssignmentPriorityMenu;
    [SerializeField]
    public UICityLaborPrioritizationManager uiLaborPrioritizationManager;

    [SerializeField]
    public MapWorld world;
    [SerializeField]
    public MovementSystem movementSystem;
    [SerializeField]
    public Transform objectPoolHolder, friendlyUnitHolder, enemyUnitHolder;

    [SerializeField]
    public CameraController focusCam;

    private City selectedCity;
    public City SelectedCity { get { return selectedCity; } }

    [HideInInspector]
    public List<Vector3Int> tilesToChange = new();
    [HideInInspector]
    public List<Vector3Int> cityTiles = new();
    private List<Vector3Int> developedTiles = new();
    [HideInInspector]
    public List<Vector3Int> constructingTiles = new();

    [HideInInspector]
    public ImprovementDataSO improvementData;

    [HideInInspector]
    public ResourceManager resourceManager;

    [HideInInspector]
    public int laborChange, placesToWork;

    //wonder management
    private Wonder selectedWonder;
    [SerializeField]
    private GameObject wonderHarbor;

    //trade center management
    private TradeCenter selectedTradeCenter;

    [SerializeField]
    private GameObject upgradeQueueGhost;
    //private List<GameObject> queuedGhost = new();
    //private Dictionary<string, GameObject> buildingQueueGhostDict = new();

    //for object pooling of labor numbers
    private Queue<CityLaborTileNumber> laborNumberQueue = new(); //the pool in object pooling
    private List<CityLaborTileNumber> laborNumberList = new(); //to add to in order to add back into pool

    //for object pooling of city borders
    private Queue<GameObject> borderQueue = new();
    private List<GameObject> borderList = new();

    //for object pooling of construction graphics
    //[SerializeField]
    //private GameObject constructionTilePrefab;
    //private Queue<CityImprovement> constructionTileQueue = new();

    //for object pooling of improvement resources
    private Queue<ImprovementResource> improvementResourceQueue = new();
    private List<ImprovementResource> improvementResourceList = new();

	//for making objects transparent
	[SerializeField]
    public Material transparentMat;

    //queue ghost tracker
    [HideInInspector]
    public Dictionary<QueueItem, GameObject> queueGhostDict = new();

    public bool removingImprovement, upgradingImprovement, placingWonderHarbor; //flags thrown when doing specific tasks
    [HideInInspector]
    public bool buildOptionsActive, isQueueing;
    [HideInInspector]
    public UIBuilderHandler activeBuilderHandler;

    [SerializeField]
    public Transform improvementHolder;
    private Dictionary<Vector3Int, (MeshFilter[], GameObject)> improvementMeshDict = new();
    private List<MeshFilter> improvementMeshList = new();

    [HideInInspector]
    public GameObject emptyGO;

    [SerializeField]
    public AudioClip buildClip, closeClip, selectClip, removeClip, queueClip, checkClip, moveClip, pickUpClip, putDownClip, marchClip, coinsClip, ringClip, chimeClip, fireClip, smallTownClip, 
        largeTownClip, laborInClip, laborOutClip, constructionClip, trainingClip, thudClip, fieryOpen, popGainClip, popLoseClip, alertClip, warningClip, sunBeam, receiveGift, denyGift;
    [SerializeField]
    private AudioClip[] acknowledgements;
    [HideInInspector]
    public AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.ignoreListenerPause = true;
        emptyGO = new GameObject("NewImprovement");
        emptyGO.SetActive(false);
        GrowLaborNumbersPool();
        GrowBordersPool();
        //GrowConstructionTilePool(); 
        //GrowResourceInfoHolderPool();
        //GrowResourceInfoPanelPool();
        GrowImprovementResourcePool();
        PopulateUpgradeDictForTesting();
        //openAssignmentPriorityMenu.interactable = false;
    }

    private void PopulateUpgradeDictForTesting()
    {
        //here just for testing
        //world.SetUpgradeableObjectMaxLevel("Research", 3);
        //world.SetUpgradeableObjectMaxLevel("Monument", 2);
        //world.SetUpgradeableObjectMaxLevel("Housing", 3);
        //world.SetUpgradeableObjectMaxLevel("Infantry", 2);
        //world.SetUpgradeableObjectMaxLevel("Trader", 2);
        //world.SetUpgradeableObjectMaxLevel("Boat Trader", 2);
    }

    public void HandleB()
    {
        if (!uiCityNamer.activeStatus && selectedCity != null)
            OpenAddPopWindow();
    }

    public void CenterCamOnCity()
    {
        if (selectedCity != null)
            focusCam.CenterCameraNoFollow(selectedCity.transform.position);
        else if (selectedWonder != null)
            focusCam.CenterCameraNoFollow(selectedWonder.centerPos);
    }

    //private void CameraBirdsEyeRotation()
    //{
    //    //focusCam.DisableMouse = true;
    //    //originalRotation = focusCam.transform.rotation;
    //    //originalZoom = focusCam.GetZoom();
    //    //focusCam.centerTransform = selectedCity.transform;
    //}

    //private void CameraBirdsEyeRotationWonder()
    //{
    //    //originalRotation = focusCam.transform.rotation;
    //    //originalZoom = focusCam.GetZoom();
    //    //focusCam.centerTransform = selectedWonder.transform;
    //}

    //private void CameraDefaultRotation()
    //{
    //    //focusCam.DisableMouse = false;
    //    //focusCam.centerTransform = null;
    //    //focusCam.transform.rotation = Quaternion.Lerp(focusCam.transform.rotation, originalRotation, Time.deltaTime * 5);
    //    //focusCam.SetZoom(originalZoom);
    //    //focusCam.cameraTransform.localPosition += new Vector3(0, -1f, 1f);
    //}


    public void HandleCitySelection(Vector3 location, GameObject selectedObject)
    {
        if (world.moveUnit || world.unitOrders || world.buildingWonder)
            return;

        if (world.citySelected) //just so the city isn't selected when being chosen to move units (also works with wonders)
        {
            world.citySelected = false;
            return;
        }

        if (selectedObject == null)
            return;

		Vector3Int terrainLocation = world.GetClosestTerrainLoc(location);
        TerrainData tdTry = selectedObject.GetComponent<TerrainData>();

        //selecting terrain of city
        if (world.IsCityOnTile(terrainLocation) && tdTry != null)
        {
            SelectCity(location, world.GetCity(terrainLocation));
        }
        else if (world.IsWonderOnTile(terrainLocation) && tdTry != null)
        {
			SelectWonder(world.GetWonder(terrainLocation));
		}
        else if (world.IsTradeCenterOnTile(terrainLocation) && tdTry != null)
        {
            SelectTradeCenter(world.GetTradeCenter(terrainLocation));
        }
        else if (world.IsEnemyCityOnTile(terrainLocation) && tdTry != null)
        {
			if (tdTry.isDiscovered)
                SelectCity(location, world.GetEnemyCity(terrainLocation));
		}
        //selecting city
		else if (selectedObject.TryGetComponent(out City cityReference))
        {
            SelectCity(location, cityReference);
        }
        //selecting wonder
        else if (selectedObject.TryGetComponent(out Wonder wonder))
        {
            SelectWonder(wonder);
        }
        //selecting trade center
        else if (selectedObject.TryGetComponent(out TradeCenter center))
        {
            SelectTradeCenter(center);
        }
        //selecting improvements to remove or add/remove labor
        else if (selectedObject.TryGetComponent(out CityImprovement improvementSelected))
        {
            if (selectedObject.tag == "Enemy")
            {
				if (world.somethingSelected)
				{
					world.somethingSelected = false;
				}
                else
                {
				    TerrainData td = world.GetTerrainDataAt(terrainLocation);
                
                    if (td.isDiscovered)
                        world.OpenTerrainTooltip(td);
                }
	
                return;
            }
            
            bool isBarracks = false;
            if (improvementSelected.building && !removingImprovement && !upgradingImprovement)
            {
                City city = improvementSelected.city;
                if (city == null) //for orphan barracks or harbors
                    return;

                SingleBuildType type = improvementSelected.GetImprovementData.singleBuildType;
				if (type == SingleBuildType.Barracks || type == SingleBuildType.Shipyard || type == SingleBuildType.AirBase)
                {
                    isBarracks = true;
                }
                else if (type == SingleBuildType.TradeDepot || type == SingleBuildType.Harbor || type == SingleBuildType.Airport)
                {
					if (selectedCity != null)
                    {
						if (selectedCity != improvementSelected.city)
                        {
                            ResetCityUI();
                        }
                        else if (uiCityTabs.openTab)
                        {
                            uiCityTabs.HideSelectedTab(false);
                            return;
                        }
                    }

					world.OpenImprovementTooltip(improvementSelected);
                    return;
				}
                else if (city != null)
                {
                    SelectCity(city.cityLoc, city);
                    return;
                }
            }
            
            if (selectedCity != null)
            {
                //Vector3Int terrainLocation = terrainSelected.GetTileCoordinates();
                //Vector3Int terrainLocation = world.GetClosestTerrainLoc(location);
                //TerrainData terrainSelected = world.GetTerrainDataAt(terrainLocation);

                //deselecting if choosing improvement outside of city
                if (!cityTiles.Contains(terrainLocation) && terrainLocation != selectedCity.cityLoc)
                {
                    ResetCityUI();
                    return;
                }

                //if not manipulating buildings, exit out
                if (improvementData == null && laborChange == 0 && !removingImprovement && !upgradingImprovement && !uiCityTabs.openTab)
                {
                    if (terrainLocation == selectedCity.cityLoc)
                        ResetCityUI();
                    ResetCityUIToBase();
                    if (isBarracks)
                        world.OpenCampTooltip(improvementSelected);
                    else
                        world.OpenImprovementTooltip(improvementSelected);
                    return;
                }

                if (upgradingImprovement)
                {
                    if (tilesToChange.Contains(terrainLocation) || (terrainLocation == selectedCity.cityLoc && improvementSelected.canBeUpgraded))
                    {
                        //PlayConstructionAudio();
						uiCityUpgradePanel.ToggleVisibility(true, selectedCity.ResourceManager, improvementSelected);
                    }
                    //UpgradeSelectedImprovementQueueCheck(terrainLocation, improvementSelected);
                    else
                        UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Not here");
                }
                else if (removingImprovement)
                {
                    //if (improvementSelected.initialCityHouse)
                    //{
                    //    ResetCityUI();
                    //    return;
                    //}
                    if (improvementSelected.isConstruction)
                    {
                        improvementSelected.RemoveConstruction();
                        constructingTiles.Remove(terrainLocation);
                    }

					//if (!improvementSelected.isConstruction && !improvementSelected.isUpgrading && !improvementSelected.isTraining)
					//    improvementSelected.PlayRemoveEffect(world.GetTerrainDataAt(terrainLocation).isHill);
					if (RemoveImprovement(terrainLocation, improvementSelected, false))
					    PlayAudioClip(removeClip);
                }
                else if (laborChange != 0) //for changing labor counts in tile
                {
                    if (improvementSelected.isConstruction)
                    {
                        UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Still building...");
                        return;
                    }
                    else if (!tilesToChange.Contains(terrainLocation))
                    {
                        UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Not here");
                    }
                    else
                    {
                        ChangeLaborCount(terrainLocation);
                    }
                }
                else if (uiCityTabs.openTab)
                {
                    uiCityTabs.HideSelectedTab(false);
                }
                else
                {
                    if (isBarracks)
                        world.OpenCampTooltip(improvementSelected);
                    else
                        world.OpenImprovementTooltip(improvementSelected);
                }
            }
            else
            {
                if (world.somethingSelected)
                {
                    world.somethingSelected = false;
                }
                else
                {
                    if (improvementSelected.wonderHarbor)
                    {
                        if (selectedWonder != null)
							UnselectWonder();

						return;
                    }
                    
                    if (isBarracks)
                        world.OpenCampTooltip(improvementSelected);
                    else
                        world.OpenImprovementTooltip(improvementSelected);
                }
            }
        }
        //placing wonder harbor
        else if (selectedWonder != null && placingWonderHarbor && selectedObject.TryGetComponent(out TerrainData terrainForHarbor)) //for placing harbor
        {
            Vector3Int terrainLoc = terrainForHarbor.TileCoordinates;

            if (!terrainForHarbor.isDiscovered)
            {
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Explore here first");
			}
            else if (!tilesToChange.Contains(terrainLoc))
            {
                UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't build here");
            }
            else
            {
                BuildWonderHarbor(terrainLoc);
            }
        }
        //selecting terrain to show info
        else if (!world.unitOrders && selectedCity == null && selectedWonder == null && selectedTradeCenter == null && selectedObject.TryGetComponent(out TerrainData td))
        {
            if (world.somethingSelected)
            {
                world.somethingSelected = false;
            }
            else
            {
                if (td.isDiscovered)
                    world.OpenTerrainTooltip(td);
            }
        }
        //selecting tiles to place improvements
        else if (selectedCity != null && selectedObject.TryGetComponent(out TerrainData terrainSelected))
        {
            Vector3Int terrainLoc = terrainSelected.TileCoordinates;

            //deselecting if choosing tile outside of city
            //if (!cityTiles.Contains(terrainLoc))
            //{
            //    ResetCityUI();
            //    //world.OpenTerrainTooltip(terrainSelected);
            //    return;
            //}

            if (!tilesToChange.Contains(terrainLoc))
            {
				if (uiCityTabs.openTab)
					uiCityTabs.HideSelectedTab(false);
				else if (upgradingImprovement || removingImprovement || laborChange != 0 || improvementData != null)
                    ResetCityUIToBase();
                else
                    ResetCityUI();
            }
            else 
            {
                if (improvementData != null)
                {
                    if (terrainSelected.beingCleared)
                    {
						UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Forest being cleared");
						return;
                    }

                    if (world.IsInBattleArea(terrainLoc))
                    {
						UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Battle fought on tile");
						return;
					}

                    BuildImprovementQueueCheck(improvementData, terrainLoc); //passing the data here as method requires it

                    world.TutorialCheck("Build Something");
                }
                else if (upgradingImprovement)
                {
                    CityImprovement improvement = world.GetCityDevelopment(terrainLoc);
                    uiCityUpgradePanel.ToggleVisibility(true, selectedCity.ResourceManager, improvement);
					PlayConstructionAudio();
					//UpgradeSelectedImprovementQueueCheck(terrainLocation, improvement);
				}
				else if (removingImprovement)
                {
                    if (world.TileHasCityImprovement(terrainLoc))
                    {
                        CityImprovement improvement = world.GetCityDevelopment(terrainLoc);

                        if (improvement.isConstruction)
                        {
                            //improvement = world.GetCityDevelopmentConstruction(terrainLoc);
                            improvement.RemoveConstruction();
							constructingTiles.Remove(terrainLoc);
						}

					    if (RemoveImprovement(terrainLoc, improvement, false))
                            PlayAudioClip(removeClip);
                    } 
                        
                    
                    //if (world.CheckIfTileIsUnderConstruction(terrainLoc))
                    //{
                    //}

                    //if (!improvement.isConstruction && !improvement.isUpgrading && !improvement.isTraining)
                    //    improvement.PlayRemoveEffect(terrainSelected.isHill);
                }
                else if (laborChange != 0)
                {
					if (world.TileHasCityImprovement(terrainLoc))
                    {
						if (world.GetCityDevelopment(terrainLoc).isConstruction)
                        {
                            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Still building...");
                            return;
                        }
    
                        ChangeLaborCount(terrainLoc);
                    }
                }
            }
        }
        else if (world.unitMovement.upgradingUnit && selectedObject.TryGetComponent(out Unit unit) && world.unitMovement.highlightedUnitList.Contains(unit)) //here to prevent city window from closing when upgrading units
        {

		}
        else
        {
            ResetCityUI();
            UnselectWonder();
            UnselectTradeCenter();
        }
    }

    private void SelectWonder(Wonder wonderReference)
    {
        if (selectedWonder != null)
        {
            if (selectedWonder != wonderReference)
            {
                UnselectWonder();
            }
            else
            {
                UnselectWonder();
                return; //deselect if same wonder selected
            }
        }

        ResetCityUI();
        UnselectTradeCenter();

        uiWonderSelection.ToggleVisibility(true, wonderReference);
        selectedWonder = wonderReference;
        selectedWonder.isActive = true;
        selectedWonder.TimeProgressBarSetActive(true);
        selectedWonder.EnableHighlight(Color.white, false);
        CenterCamOnCity();
    }

    private void SelectTradeCenter(TradeCenter center)
    {
        if (selectedTradeCenter != null)
        {
            if (selectedTradeCenter != center)
            {
                UnselectTradeCenter();
            }
            else
            {
                UnselectTradeCenter();
                return;
            }
        }

        ResetCityUI();
        UnselectWonder();

        uiTradeCenter.ToggleVisibility(true, center);
        selectedTradeCenter = center;
        selectedTradeCenter.EnableHighlight(new Color(0.5f, 0, 1), false);
        uiTradeCenter.SetName(selectedTradeCenter.tradeCenterDisplayName);
        CenterCamOnCity();
    }

    public void BuildHarbor()
    {
        if (selectedWonder.hasHarbor)
        {
            PlayAudioClip(removeClip);
            selectedWonder.DestroyHarbor();
            uiWonderSelection.UpdateHarborButton(false);
            return;
        }
        
        if (!uiWonderSelection.buttonsAreWorking)
            return;

        PlaySelectAudio();
        tilesToChange.Clear();

        foreach (Vector3Int loc in selectedWonder.PossibleHarborLocs)
        {
            if (!world.IsTileOpenCheck(loc))
                continue;
            
            TerrainData td = world.GetTerrainDataAt(loc);

            td.EnableHighlight(Color.white);
            tilesToChange.Add(loc);
        }

        if (tilesToChange.Count == 0)
        {
            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "No available spots");
        }
        else
        {
            placingWonderHarbor = true;
            //CameraBirdsEyeRotationWonder();
            uiImprovementBuildInfoPanel.SetText("Building Harbor");
            uiImprovementBuildInfoPanel.ToggleVisibility(true);
        }
    }

    public void CreateWorkerButton()
    {
        if (!uiWonderSelection.buttonsAreWorking)
            return;

        if (selectedWonder.WorkersReceived == 0)
        {
            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "No workers to unload");
            return;
        }

        PlaySelectAudio();

        selectedWonder.StopConstructing();
        selectedWonder.WorkersReceived--; //decrease worker count 
        bool secondary = selectedWonder.workerSexAndHome[0].Item1;
        Vector3Int homeCityLoc = selectedWonder.workerSexAndHome[0].Item2;
        selectedWonder.workerSexAndHome.RemoveAt(0);

        if (uiWonderSelection.activeStatus)
            uiWonderSelection.UpdateUIWorkers(selectedWonder.WorkersReceived, selectedWonder);

        Vector3Int buildPosition = selectedWonder.unloadLoc;
        world.RemoveFromGoldWaitList(selectedWonder);
        //if (world.IsUnitLocationTaken(buildPosition) || !world.CheckIfPositionIsValid(buildPosition)) //placing unit in world after building in city
        //{
        //    //List<Vector3Int> newPositions = world.GetNeighborsFor(Vector3Int.FloorToInt(buildPosition));
        //    foreach (Vector3Int pos in world.GetNeighborsFor(buildPosition, MapWorld.State.EIGHTWAYTWODEEP))
        //    {
        //        if (!world.IsUnitLocationTaken(pos) && world.CheckIfPositionIsValid(pos))
        //        {
        //            buildPosition = pos;
        //            break;
        //        }
        //    }
        //}

        TransferWorker(secondary, homeCityLoc, buildPosition, true);
    }

    public void TransferWorker(bool secondary, Vector3Int destination, Vector3Int buildPosition, bool wonder)
    {
		GameObject workerGO;

		if (secondary)
			workerGO = world.laborerData.secondaryPrefab;
		else
			workerGO = world.laborerData.prefab;

		GameObject unit = Instantiate(workerGO, buildPosition, Quaternion.identity); //produce unit at specified position
		unit.transform.SetParent(friendlyUnitHolder, false);
		//for tweening
		Vector3 goScale = unit.transform.localScale;
        float scaleX = goScale.x;
        float scaleZ = goScale.z;
        unit.transform.localScale = new Vector3(scaleX, 0.1f, scaleZ);
        LeanTween.scale(unit, goScale, 0.5f).setEase(LeanTweenType.easeOutBack);

		Unit newUnit = unit.GetComponent<Unit>();
		newUnit.SetReferences(world);
		newUnit.currentLocation = world.AddUnitPosition(buildPosition, newUnit);
		world.laborerCount++;
		unit.name = "Laborer " + world.laborerCount;
		newUnit.PlayAudioClip(buildClip);

		Laborer laborer = newUnit.GetComponent<Laborer>();
		world.laborerList.Add(laborer);
        if (wonder)
            laborer.homeCityLoc = destination;
        else
			laborer.homeCityLoc = selectedCity.cityLoc;

		laborer.GoToDestination(destination);
	}

    public void CreateAllWorkers(Wonder wonder)
    {
        wonder.StopConstructing();
        wonder.WorkersReceived = 0; //decrease worker count 

        if (uiWonderSelection.activeStatus && uiWonderSelection.wonder == wonder)
            uiWonderSelection.UpdateUIWorkers(0, wonder);

        int lostWorkersCount = 0;
        List<Vector3Int> locs = wonder.OuterRim();

        for (int i = 0; i < wonder.workerSexAndHome.Count; i++)
        {
            GameObject workerGO;
            
            if (wonder.workerSexAndHome[i].Item1)
                workerGO = world.laborerData.secondaryPrefab;
            else
				workerGO = world.laborerData.prefab;

			if (locs.Count == 0)
                lostWorkersCount++;
            else
            {
                List<Vector3Int> tempLocs = new(locs);

                foreach (Vector3Int loc in tempLocs)
                {
                    locs.Remove(loc);

                    if (world.IsUnitLocationTaken(loc) || !world.CheckIfPositionIsValid(loc))
                        continue;

                    GameObject unit = Instantiate(workerGO, loc, Quaternion.identity); //produce unit at specified position
                    unit.transform.SetParent(friendlyUnitHolder, false);
                    unit.transform.rotation = Quaternion.LookRotation(wonder.centerPos - unit.transform.position);
                    Laborer laborer = unit.GetComponent<Laborer>();
                    laborer.marker.ToggleVisibility(true);
                    world.laborerList.Add(laborer);
                    laborer.StartLaborAnimations(false, wonder.workerSexAndHome[i].Item2);
                    
                    Unit newUnit = unit.GetComponent<Unit>();
                    newUnit.SetReferences(world);
                    newUnit.currentLocation = world.AddUnitPosition(loc, newUnit);
					world.laborerCount++;
					unit.name = "Laborer " + world.laborerCount;
					world.laborerList.Add(newUnit.GetComponent<Laborer>());

					break;
                }
            }
        }

        wonder.workerSexAndHome.Clear();

        if (lostWorkersCount > 0)
            InfoPopUpHandler.WarningMessage().Create(wonder.unloadLoc, "Lost " + lostWorkersCount.ToString() + " worker(s) due to no available space");
    }

    public void BuildWonderHarbor(Vector3Int loc)
    {
        PlayBoomAudio();
        GameObject harborGO = Instantiate(wonderHarbor, loc, Quaternion.Euler(0, HarborRotation(loc, selectedWonder.unloadLoc), 0));
        //for tweening
        Vector3 goScale = harborGO.transform.localScale;
        CityImprovement harbor = harborGO.GetComponent<CityImprovement>();
        harbor.SetWorld(world);
        harbor.wonderHarbor = true;
        harbor.PlaySmokeSplash(false);
        selectedWonder.harborImprovement = harbor;
        harborGO.transform.localScale = Vector3.zero;
        LeanTween.scale(harborGO, goScale, 0.25f).setEase(LeanTweenType.easeOutBack);
        selectedWonder.hasHarbor = true;
        selectedWonder.harborLoc = loc;

        world.AddToCityLabor(loc, null);
        world.AddStructure(loc, harborGO);
        world.AddTradeLoc(loc, selectedWonder.wonderName);
        uiWonderSelection.UpdateHarborButton(true);

        CloseImprovementBuildPanel();
    }

    public void LoadWonderHarbor(Vector3Int loc, Wonder wonder)
    {
		GameObject harborGO = Instantiate(wonderHarbor, loc, Quaternion.Euler(0, HarborRotation(loc, wonder.unloadLoc), 0));
		CityImprovement harbor = harborGO.GetComponent<CityImprovement>();
        harbor.SetWorld(world);
		wonder.harborImprovement = harbor;
		
		world.AddToCityLabor(loc, null);
		world.AddStructure(loc, harborGO);
		world.AddTradeLoc(loc, wonder.wonderName);
	}

    public void OpenCancelWonderConstructionWarning()
    {
        if (uiWonderSelection.buttonsAreWorking)
        {
            PlaySelectAudio();
            uiDestroyCityWarning.ToggleVisibility(true);
        }
    }

    private void CancelWonderConstruction()
    {
        selectedWonder.PlayRemoveEffect();
		PlayAudioClip(removeClip);
		uiDestroyCityWarning.ToggleVisibility(false);
        uiWonderSelection.ToggleVisibility(false, selectedWonder);
        selectedWonder.StopConstructing();

        CreateAllWorkers(selectedWonder);
        
        if (!selectedWonder.roadPreExisted)
        {
            RoadManager roadManager = GetComponent<RoadManager>();
            roadManager.RemoveRoadAtPosition(selectedWonder.unloadLoc);
        }

        if (selectedWonder.hasHarbor)
            selectedWonder.DestroyHarbor();
     
        world.RemoveWonderName(selectedWonder.wonderName);
        world.RemoveTradeLoc(selectedWonder.unloadLoc);


        GameObject priorGO = world.GetStructure(selectedWonder.unloadLoc);
        Destroy(priorGO);

        //for no walk zone
        int k = 0;
        int[] xArray = new int[selectedWonder.WonderLocs.Count];
        int[] zArray = new int[selectedWonder.WonderLocs.Count];

        foreach (Vector3Int tile in selectedWonder.WonderLocs)
        {
            world.RemoveStructure(tile);
            world.RemoveSingleBuildFromCityLabor(tile);
            world.RemoveWonder(tile);

            TerrainData td = world.GetTerrainDataAt(tile);

            if (selectedWonder.WonderData.isSea)
            {
                td.sailable = true;
                td.walkable = false;
            }
			
            if (td.prop != null && td.resourceAmount > 0)
                td.ShowProp(true);

            td.ToggleTerrainMesh(true);
            if (td.hasResourceMap)
                td.RestoreResourceMap();

            xArray[k] = tile.x;
            zArray[k] = tile.z;
            k++;
        }

        int xMin = Mathf.Min(xArray) - 1;
        int xMax = Mathf.Max(xArray) + 1;
        int zMin = Mathf.Min(zArray) - 1;
        int zMax = Mathf.Max(zArray) + 1;

        foreach (Vector3Int tile in selectedWonder.WonderLocs)
        {
            world.RemoveFromNoWalkList(tile);
            
            foreach (Vector3Int neighbor in world.GetNeighborsFor(tile, MapWorld.State.EIGHTWAY))
            {
                if (neighbor.x == xMin || neighbor.x == xMax || neighbor.z == zMin || neighbor.z == zMax)
                    continue;

                world.RemoveFromNoWalkList(neighbor);
            }
        }

        foreach (Vector3Int tile in selectedWonder.CoastTiles)
            world.RemoveFromCoastList(tile);

        world.allWonders.Remove(selectedWonder);
        selectedWonder = null;
    }

    public void PlaySelectAudio()
    {
        //audioSource.clip = selectClip;
        audioSource.PlayOneShot(selectClip);
    }

    public void PlayBoomAudio()
    {
        //audioSource.clip = buildClip;
        audioSource.PlayOneShot(buildClip);
    }

    public void PlayCloseAudio()
    {
        //audioSource.clip = closeClip;
        audioSource.PlayOneShot(closeClip);
    }

    public void PlayCheckAudio()
    {
        audioSource.clip = checkClip;
        audioSource.Play();
    }

    public void PlayMoveAudio()
    {
        audioSource.clip = moveClip;
        audioSource.Play();
    }

    public void PlayPickUpAudio()
    {
		audioSource.clip = pickUpClip;
		audioSource.Play();
	}

    public void PlayPutDownAudio()
    {
		audioSource.clip = putDownClip;
		audioSource.Play();
	}

    public void PlayQueueAudio()
    {
		audioSource.clip = queueClip;
		audioSource.Play();
	}

	public void PlayMarchAudio()
    {
        audioSource.clip = marchClip;
        audioSource.Play();
    }

    public void PlayCoinsAudio()
    {
        audioSource.clip = coinsClip;
        audioSource.Play();
    }

    public void PlayRingAudio()
    {
        audioSource.clip = ringClip;
        audioSource.Play();
    }

    public void PlayChimeAudio()
    {
        audioSource.clip = chimeClip;
        audioSource.Play();
    }

    public void PlayAlertAudio()
    {
		audioSource.clip = alertClip;
		audioSource.Play();
	}

 //   public void PlayWarningAudio()
 //   {
	//	audioSource.clip = warningClip;
	//	audioSource.Play();
	//}

    public void PlayOpenCityAudio()
    {
        if (selectedCity.currentPop < 5)
            world.PlayCityAudio(fireClip);
        else if (selectedCity.currentPop < 12)
			world.PlayCityAudio(smallTownClip);
        else
			world.PlayCityAudio(largeTownClip);
    }

    public void PlayAudioClip(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.Play();
    }

    public void PlayLaborAudio(bool add)
    {
        audioSource.clip = add ? laborInClip : laborOutClip;
        audioSource.Play();
    }

	public void MoveUnitAudio()
	{
		audioSource.clip = acknowledgements[UnityEngine.Random.Range(0, acknowledgements.Length)];
		audioSource.Play();
	}

    public void PlayTrainingAudio()
    {
        audioSource.clip = trainingClip;
        audioSource.Play();
    }

    public void PlayConstructionAudio()
    {
        audioSource.clip = constructionClip;
        audioSource.Play();
    }

    public void PlayThudAudio()
    {
        audioSource.clip = thudClip;
        audioSource.Play();
    }

    public void PlayFieryOpen()
    {
        audioSource.clip = fieryOpen;
        audioSource.Play();
    }

    public void StopAudio()
    {
        audioSource.Stop();
    }


    public void OpenAddPopWindow()
    {
        if (world.uiCityPopIncreasePanel.activeStatus)
        {
    		world.uiCityPopIncreasePanel.ToggleVisibility(false);
        }
        else
        {
		    CloseLaborMenus();
		    CloseImprovementTooltipButton();
		    CloseImprovementBuildPanel();
		    CloseSingleWindows();
		    uiResourceManager.ToggleOverflowVisibility(false);
		    world.uiCityPopIncreasePanel.ToggleVisibility(false);
		    uiCityTabs.HideSelectedTab(false);
			
            world.uiCityPopIncreasePanel.ToggleVisibility(true, 1, selectedCity);
        }
	}

    public void CloseAddPopWindow()
    {
        PlayCloseAudio();
        world.uiCityPopIncreasePanel.ToggleVisibility(false);
    }

	private void SelectCity(Vector3 location, City cityReference)
    {
        if (selectedCity != null)
        {
            if (selectedCity != cityReference)
            {
                ResetCityUI();
            }
            else
            {
                ResetCityUI();
                return; //deselect if same city selected
            }
        }

        UnselectWonder();
        UnselectTradeCenter();

        selectedCity = cityReference;

		if (cityReference.gameObject.tag == "Enemy")
        {
			if (!world.GetTerrainDataAt(world.RoundToInt(location)).isDiscovered)
            {
                world.somethingSelected = false;
                selectedCity = null;
                return;
            }

            selectedCity.activeCity = true;
            ToggleEnemyBuildingHighlight(selectedCity.cityLoc, Color.red);
            world.somethingSelected = false;
            world.GetTerrainDataAt(selectedCity.cityLoc).EnableHighlight(Color.red);

			world.cityCanvas.gameObject.SetActive(true);
			uiInfoPanelCity.ToggleVisibility(true, true);
			uiInfoPanelCity.SetAllData(selectedCity);
			uiInfoPanelCity.SetWorkEthicPopUpCity(selectedCity);
			uiInfoPanelCity.UpdateWater(selectedCity.waterCount);
            uiInfoPanelCity.UpdatePower(selectedCity.powerCount);
			return;
        }

        world.TutorialCheck("Open City");
        if (selectedCity.lostPop > 0)
        {
			Vector3 middle = new Vector3(Screen.width / 2, Screen.height / 2, 0);
			UIInfoPopUpHandler.WarningMessage().Create(middle, "Lost " + selectedCity.lostPop + " pop since last visit");
            selectedCity.lostPop = 0;
		}

        //selectedCity.exclamationPoint.SetActive(false);
        if (selectedCity.ResourceManager.growthDeclineDanger)
            uiInfoPanelCity.TogglewWarning(true);
        else
            uiInfoPanelCity.TogglewWarning(false);
        PlayOpenCityAudio();

        //world.openingCity = true;
        world.cityCanvas.gameObject.SetActive(true);
        world.somethingSelected = false; //cities aren't considered "selected" due to intricate selection code
        selectedCity.activeCity = true;
        (cityTiles, developedTiles, constructingTiles) = world.GetCityRadiusFor(selectedCity.cityLoc);
        focusCam.SetCityLimit(cityTiles, selectedCity.cityLoc);
        ResourceProducerTimeProgressBarsSetActive(true);
        ToggleBuildingHighlight(true, selectedCity.cityLoc);
        world.GetTerrainDataAt(selectedCity.cityLoc).EnableHighlight(Color.green);
        DrawBorders();
        CheckForWork();
        autoAssign.isOn = selectedCity.AutoAssignLabor;
        //if (selectedCity.autoGrow)
        //      {
        //	uiLaborPrioritizationManager.ToggleVisibility(true, true);
        //    uiLaborPrioritizationManager.PrepareLaborPrioritizationMenu(selectedCity);
        //    uiLaborPrioritizationManager.LoadLaborPrioritizationInfo();
        //      }
		resourceManager = selectedCity.ResourceManager;
        //uiResourceManager.SetCityInfo(selectedCity.cityName, selectedCity.warehouseStorageLimit, selectedCity.ResourceManager.GetResourceStorageLevel);
        resourceManager.UpdateUI(selectedCity.GetResourceValues());
        uiCityTabs.ToggleVisibility(true, selectedCity.singleBuildList.Contains(SingleBuildType.Market), resourceManager);
        uiResourceManager.ToggleVisibility(true, selectedCity);
        CenterCamOnCity();
        //uiInfoPanelCity.SetGrowthNumber(selectedCity.GetGrowthNumber());
        uiInfoPanelCity.SetAllData(selectedCity);
        uiInfoPanelCity.SetWorkEthicPopUpCity(selectedCity);
        //uiInfoPanelCity.SetGrowthPauseToggle(selectedCity.ResourceManager.pauseGrowth);
        uiInfoPanelCity.UpdateWater(selectedCity.waterCount);
		uiInfoPanelCity.UpdatePower(selectedCity.powerCount);

		if (selectedCity.growing)
        {
            uiLaborAssignment.showPrioritiesButton.SetActive(selectedCity.AutoAssignLabor);
            uiLaborAssignment.ShowUI(selectedCity, placesToWork);
        }
        else
        {
            uiLaborAssignment.UpdateUI(selectedCity, placesToWork);
        }

        uiInfoPanelCity.ToggleVisibility(true);

        uiLaborHandler.SetCity(selectedCity);
        //uiUnitTurn.buttonClicked.AddListener(ResetCityUI);
        if (selectedCity.growing)
        {
            selectedCity.CityGrowthProgressBarSetActive(true);
            abandonCityButton.interactable = false;
        }
        else
        {
			selectedCity.CityGrowthProgressBarSetActive(false);
			abandonCityButton.interactable = true;
        }

        UpdateLaborNumbers(selectedCity);

        for (int i = 0; i < selectedCity.singleBuildList.Count; i++)
            Debug.Log(selectedCity.singleBuildList[i]);
        //selectedCity.Select();
    }

    public void ToggleCityGrowthPause(bool v)
    {
        PlayCheckAudio();
        selectedCity.ResourceManager.pauseGrowth = v;
    }

    //public void SetGrowthNumber(int num)
    //{
    //    uiInfoPanelCity.SetGrowthNumber(num);
    //}

    //public void ShowQueuedGhost()
    //{
    //    foreach (Vector3Int tile in selectedCity.improvementQueueLocs) //improvements
    //    {
    //        if (world.ShowQueueGhost(tile))
    //            queuedGhost.Add(world.GetQueueGhost(tile));
    //    }

    //    foreach (string building in selectedCity.buildingQueueGhostDict.Keys) //buildings
    //    {
    //        GameObject ghost = selectedCity.buildingQueueGhostDict[building];
    //        ghost.SetActive(true);
    //        //for tweening
    //        Vector3 goScale = ghost.transform.localScale;
    //        ghost.transform.localScale = Vector3.zero;
    //        LeanTween.scale(ghost, goScale, 0.25f).setEase(LeanTweenType.easeOutBack);
    //        queuedGhost.Add(selectedCity.buildingQueueGhostDict[building]);
    //    }
    //}

    public void DestroyQueuedGhost()
    {
        foreach (QueueItem item in queueGhostDict.Keys)
        {
            Destroy(queueGhostDict[item]);
        }

        queueGhostDict.Clear();
        //foreach (GameObject go in queuedGhost)
        //{
        //    go.SetActive(false);
        //}

        //queuedGhost.Clear();
    }

    public void CreateQueuedGhost(QueueItem item, ImprovementDataSO improvementData, Vector3Int loc, bool isBuilding)
    {
        Color newColor = new(0, 1f, 0, 0.8f);
        Vector3 newLoc = loc;

        if (isBuilding)
            newLoc += improvementData.buildingLocation;
        else if (improvementData.replaceTerrain)
            newLoc.y += .1f;

        GameObject improvementGhost = Instantiate(improvementData.prefab, newLoc, Quaternion.identity);
		improvementGhost.layer = LayerMask.NameToLayer("Text");
		//improvementGhost.transform.SetParent(selectedCity.transform, false);
		//for tweening
		Vector3 goScale = improvementGhost.transform.localScale;
        improvementGhost.transform.localScale = Vector3.zero;
        LeanTween.scale(improvementGhost, goScale, 0.25f).setEase(LeanTweenType.easeOutBack);
        MeshRenderer[] renderers = improvementGhost.GetComponentsInChildren<MeshRenderer>();
        SkinnedMeshRenderer[] skinnedRenderers = improvementGhost.GetComponentsInChildren<SkinnedMeshRenderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
			Material[] newMats = renderers[i].materials;

			for (int j = 0; j < newMats.Length; j++)
			{
				Material newMat = new(transparentMat);
				newMat.color = newColor;
				newMat.SetTexture("_BaseMap", newMats[j].mainTexture);
				newMats[j] = newMat;
			}

			renderers[i].materials = newMats;
		}

		for (int i = 0; i < skinnedRenderers.Length; i++)
		{
            skinnedRenderers[i].gameObject.SetActive(false);
		}


		//if (isBuilding)
		//    selectedCity.buildingQueueGhostDict[improvementData.improvementName] = improvementGhost;
		//else
		//{
		//CityImprovement improvement = improvementGhost.GetComponent<CityImprovement>();
  //      improvement.SetCity(selectedCity);
            //world.SetQueueGhost(loc, improvementGhost);
            //world.SetQueueImprovement()
        //}

        //queuedGhost.Add(improvementGhost);
        queueGhostDict[item] = improvementGhost;
    }

    public void CreateQueuedArrow(QueueItem item, ImprovementDataSO improvementData, Vector3Int tempBuildLocation, bool isBuilding)
    {
        //setting up ghost
        GameObject arrowGhost = Instantiate(upgradeQueueGhost, tempBuildLocation, Quaternion.Euler(0, 90f, 0));
        arrowGhost.layer = LayerMask.NameToLayer("Text");
        //arrowGhost.transform.SetParent(selectedCity.transform, false);
        //for tweening
        Vector3 goScale = arrowGhost.transform.localScale;
        arrowGhost.transform.localScale = Vector3.zero;
        LeanTween.scale(arrowGhost, goScale, 0.25f).setEase(LeanTweenType.easeOutBack);
        if (isBuilding)
        {
            arrowGhost.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            Vector3 newLoc = tempBuildLocation + improvementData.buildingLocation;
            newLoc.y += .5f;
            arrowGhost.transform.position = newLoc;
            //selectedCity.buildingQueueGhostDict[improvementData.improvementName] = arrowGhost;
        }
        //else
        //{
        //    world.SetQueueGhost(tempBuildLocation, arrowGhost);
        //}

        //queuedGhost.Add(arrowGhost);
        queueGhostDict[item] = arrowGhost;
    }

    public void RemoveQueueGhostImprovement(QueueItem item)
    {
        GameObject go = queueGhostDict[item];
        //bool upgrading = false;
        //if (go.CompareTag("Upgrade"))
        //    upgrading = true;
        //if (city.activeCity)
        //    queuedGhost.Remove(go);
        Destroy(go);
        //world.RemoveQueueGhost(loc);

        //if (city.activeCity && cityTiles.Contains(loc))
        //{
        //    if (upgrading && upgradingImprovement)
        //    {
        //        tilesToChange.Add(loc);
        //        world.GetTerrainDataAt(loc).EnableHighlight(Color.green);
        //    }
        //}
    }

    //public void RemoveQueueGhostBuilding(string building, City city)
    //{
    //    QueueItem item = city.buildingQueueGhostDict[building];
    //    if (city.activeCity)
    //        Destroy(queueGhostDict[item]);
    //    //Destroy(go);
    //    //city.buildingQueueGhostDict.Remove(building);
    //}

    public void SellResources()
    {
        uiMarketPlaceManager.ToggleVisibility(true, selectedCity);
    }

    public void CloseSellResources()
    {
        PlayCloseAudio();
        uiMarketPlaceManager.ToggleVisibility(false);
        uiCityTabs.CloseSelectedTab();
    }

    public void RemoveImprovements()
    {
        laborChange = 0;
        //uiCityTabs.HideSelectedTab();
        //CloseLaborMenus();
        
        removingImprovement = true;
        world.uiCityPopIncreasePanel.ToggleVisibility(false);
        CloseQueueUI();
        ToggleBuildingHighlight(true, selectedCity.cityLoc);
        ImprovementTileHighlight(true);
    }

    //public void RemoveConstruction(Vector3Int tempBuildLocation)
    //{
    //    //constructingTiles.Remove(tempBuildLocation);
    //    //world.RemoveConstruction(tempBuildLocation);
    //}

    public void UpgradeImprovements()
    {
        upgradingImprovement = true;
        world.unitMovement.ToggleUnitHighlights(true, selectedCity);
        ToggleBuildingHighlight(true, selectedCity.cityLoc);
        UpgradeTileHighlight();
    }

    private void UpgradeTileHighlight()
    {
        tilesToChange.Clear();

        uiImprovementBuildInfoPanel.SetText("Upgrading Items");
        uiImprovementBuildInfoPanel.SetImage(upgradeButton, false);
        uiImprovementBuildInfoPanel.ToggleVisibility(true);

        foreach (Vector3Int tile in developedTiles)
        {
            if (isQueueing && world.CheckQueueLocation(tile))
                continue;
            
            TerrainData td = world.GetTerrainDataAt(tile);
            td.DisableHighlight(); //turning off all highlights first
            //PoolResourceInfoPanel(tile);
            //resourceInfoHolderDict.Remove(tile);
            //resourceInfoPanelDict.Remove(tile);

            CityImprovement improvement = world.GetCityDevelopment(tile); 
            improvement.DisableHighlight();

            if (improvement.GetImprovementData.improvementLevel < world.GetUpgradeableObjectMaxLevel(improvement.GetImprovementData.improvementName) && !improvement.isUpgrading)
            {
                td.EnableHighlight(Color.green);
                improvement.EnableHighlight(Color.green);
                tilesToChange.Add(tile);

                //SetUpResourceInfoPanel(improvement, tile);
            }
        }
    }

    //for updating resource info while choosing upgrade options
    //public void UpdateResourceInfo()
    //{
    //    if (uiCityUpgradePanel.activeStatus)
    //    {
    //        uiCityUpgradePanel.CheckCosts(selectedCity.ResourceManager);
            
    //        //foreach (Vector3 location in resourceInfoHolderDict.Keys)
    //        //    PoolResourceInfoPanel(location);    
    //        //resourceInfoHolderDict.Clear();
    //        //resourceInfoPanelDict.Clear();

    //        //foreach (Vector3Int tile in tilesToChange)
    //        //{
    //        //    CityImprovement improvement = world.GetCityDevelopment(tile);
    //        //    //SetUpResourceInfoPanel(improvement, tile);
    //        //}

    //        //ToggleBuildingResourceInfo();
    //    }
    //}

    public void UpgradeUnit(Unit unit, List<ResourceValue> upgradeCost, List<ResourceValue> refundCost) //can't queue, don't need to pass in city
    {
        //PlayBoomAudio();
        unit.isUpgrading = true;
        string name = unit.buildDataSO.unitType.ToString();
        if (unit == world.azai)
            name = "Azai";
        unit.upgradeLevel = world.GetUpgradeableObjectMaxLevel(name);
		string upgradeNameAndLevel = name + "-" + unit.upgradeLevel;
        //List<ResourceValue> upgradeCost = new(world.GetUpgradeCost(nameAndLevel));
        bool refund = true;

        if (unit.buildDataSO.singleBuildType != SingleBuildType.None)
        {
            refund = false;
            CityImprovement improvement = world.GetCityDevelopment(selectedCity.singleBuildDict[unit.buildDataSO.singleBuildType]);
			improvement.upgradeCost = upgradeCost;
			improvement.refundCost = refundCost;
		}
  //      if (unit.inArmy)
  //      {
  //          refund = false;
  //          CityImprovement improvement = world.GetCityDevelopment(selectedCity.barracksLocation);
  //          improvement.upgradeCost = upgradeCost;
  //          improvement.refundCost = refundCost;
  //      }
  //      else if (unit.bySea)
  //      {
  //          refund = false;
  //          CityImprovement improvement = world.GetCityDevelopment(selectedCity.harborLocation);
		//	improvement.upgradeCost = upgradeCost;
		//	improvement.refundCost = refundCost;
		//}
  //      else if (unit.byAir)
  //      {
  //          refund = false;
  //          CityImprovement improvement = world.GetCityDevelopment(selectedCity.airportLocation);
		//	improvement.upgradeCost = upgradeCost;
		//	improvement.refundCost = refundCost;
		//}
	
		selectedCity.ResourceManager.SpendResource(upgradeCost, unit.transform.position, refund, refundCost);
        UnitBuildDataSO data = UpgradeableObjectHolder.Instance.unitDict[upgradeNameAndLevel];

        world.unitMovement.ToggleUnitHighlights(false);
		world.unitMovement.ToggleUnitHighlights(true, selectedCity);
		//world.unitMovement.highlightedUnitList.Remove(unit);
		//      unit.Unhighlight();
		//world.unitMovement.ClearSelection();
		CreateUnit(data, selectedCity, true, unit);
	}

    public void UpgradeSelectedImprovementQueueCheck(Vector3Int tempBuildLocation, CityImprovement selectedImprovement, List<ResourceValue> upgradeCost, List<ResourceValue> refundCost)
    {
        string nameAndLevel = selectedImprovement.GetImprovementData.improvementNameAndLevel;

        //queue information
        if (isQueueing)
        {
            tilesToChange.Remove(tempBuildLocation);
            world.GetCityDevelopment(tempBuildLocation).DisableHighlight();
            world.GetTerrainDataAt(tempBuildLocation).DisableHighlight();
            selectedImprovement.queued = true;
            selectedImprovement.SetQueueCity(selectedCity);
            //if (!uiQueueManager.AddToQueue(tempBuildLocation, tempBuildLocation - selectedCityLoc, selectedCityLoc, world.GetUpgradeData(nameAndLevel), null, new(world.GetUpgradeCost(nameAndLevel))))
            uiQueueManager.upgradeCosts = upgradeCost;
            if (!selectedCity.AddToQueue(UpgradeableObjectHolder.Instance.improvementDict[nameAndLevel], tempBuildLocation, tempBuildLocation - selectedCity.cityLoc, true))  
                return; //checks if queue item already exists
            //else
            //    selectedCity.improvementQueueLocs.Add(tempBuildLocation);
            return;
        }

        //foreach (ResourceValue value in upgradeCost)
        //{
        //    if (!selectedCity.ResourceManager.CheckResourceAvailability(value))
        //    {
        //        //UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't afford");
        //        return;
        //    }
        //}

        if (!selectedImprovement.building)
		    PlayConstructionAudio();
		tilesToChange.Remove(tempBuildLocation);
        world.GetTerrainDataAt(tempBuildLocation).DisableHighlight();
        UpgradeSelectedImprovementPrep(tempBuildLocation, selectedImprovement, selectedCity, upgradeCost, refundCost);
    }

    private void UpgradeSelectedImprovementPrep(Vector3Int upgradeLoc, CityImprovement selectedImprovement, City city, List<ResourceValue> upgradeCost, List<ResourceValue> refundCost)
    {
        if (city.activeCity)
            selectedImprovement.DisableHighlight();

        bool cityCenterBuilding = upgradeLoc == city.cityLoc;
        string name = selectedImprovement.GetImprovementData.improvementName;
        selectedImprovement.upgradeLevel = world.GetUpgradeableObjectMaxLevel(name);
        string upgradeNameAndLevel = name + "-" + selectedImprovement.upgradeLevel;
		//string nameAndLevel = selectedImprovement.GetImprovementData.improvementNameAndLevel;
        //string upgradeNameAndLevel = selectedImprovement.GetImprovementData.improvementName + "-" + world.GetUpgradeableObjectMaxLevel(selectedImprovement.GetImprovementData.improvementName);
        //List<ResourceValue> upgradeCost = new(world.CalculateUpgradeCost(nameAndLevel, upgradeNameAndLevel, false));
        city.ResourceManager.SpendResource(upgradeCost, upgradeLoc, cityCenterBuilding, refundCost);
        ImprovementDataSO data = UpgradeableObjectHolder.Instance.improvementDict[upgradeNameAndLevel];

        if (cityCenterBuilding)
        {
            //selectedImprovement.PlayUpgradeSplash();
            RemoveImprovement(upgradeLoc, selectedImprovement, true);
            CreateBuilding(data, city, true);
        }
        else
        {
            selectedImprovement.upgradeCost = upgradeCost;
            selectedImprovement.refundCost = refundCost;
            //putting the labor back
            ResourceProducer resourceProducer = selectedImprovement.resourceProducer;

            //foreach (ResourceType resourceType in resourceProducer.producedResources)
            //{
            ResourceType resourceType = resourceProducer.producedResource.resourceType;

            for (int i = 0; i < world.GetCurrentLaborForTile(upgradeLoc); i++)
            {
                city.ChangeResourcesWorked(resourceType, -1);

                int totalResourceLabor = city.GetResourcesWorkedResourceCount(resourceType);

                if (city.activeCity)
                    uiLaborHandler.PlusMinusOneLabor(resourceType, totalResourceLabor, -1, city.ResourceManager.GetResourceGenerationValues(resourceType));
                if (totalResourceLabor == 0)
                    city.RemoveFromResourcesWorked(resourceType);
            }
            //}

            resourceProducer.UpdateCurrentLaborData(0);
            //resourceProducer.UpdateResourceGenerationData();
            resourceProducer.StopProducing();

            if (city.activeCity)
            {
                if (!world.CheckIfTileIsMaxxed(upgradeLoc))
                    placesToWork--;
                int currentLabor = world.GetCurrentLaborForTile(upgradeLoc);
                city.unusedLabor += currentLabor;
                city.usedLabor -= currentLabor;

                RemoveLaborFromDicts(upgradeLoc);
                UpdateCityLaborUIs();
                uiInfoPanelCity.SetAllData(selectedCity);
                //UpdateLaborNumbers(city);
                uiLaborAssignment.UpdateUI(selectedCity, placesToWork);

                //resourceManager.UpdateUI(city.GetResourceValues());
                uiResourceManager.SetCityCurrentStorage(city.ResourceManager.ResourceStorageLevel);
            }
            else
            {
                RemoveLaborFromDicts(upgradeLoc);
            }
            
            if (city.AutoAssignLabor && city.unusedLabor > 0)
                city.AutoAssignmentsForLabor();

            selectedImprovement.BeginImprovementUpgradeProcess(city, resourceProducer, data, false);
        }
    }

    public void UpgradeSelectedImprovement(Vector3Int upgradeLoc, CityImprovement selectedImprovement, City city, ImprovementDataSO data)
    {
        RemoveImprovement(upgradeLoc, selectedImprovement, true);
        BuildImprovement(data, upgradeLoc, city, true);
    }

    public void ToggleEnemyBuildingHighlight(Vector3Int cityLoc, Color color)
    {
		foreach (CityImprovement building in world.cityBuildingDict[cityLoc].Values)
        {
            //CityImprovement building = world.GetBuildingData(cityLoc, name);
            building.DisableHighlight();
			building.EnableHighlight(color, true);
		}
	}

  //  public void ToggleBuildingMaterial(Vector3Int cityLoc, Material mat, bool lightsOn)
  //  {
  //      foreach (string name in world.GetBuildingListForCity(cityLoc))
  //      {
  //          CityImprovement building = world.GetBuildingData(cityLoc, name);
  //          building.DisableHighlight();
		//	building.EnableMaterial(mat);
		//}

  //      if (lightsOn)
  //      {
		//	if (world.enemyCityDict.ContainsKey(cityLoc))
		//		world.enemyCityDict[cityLoc].HouseLightCheck();
		//	else
		//		world.cityDict[cityLoc].HouseLightCheck();
		//}
  //      else
  //      {
  //          if (world.enemyCityDict.ContainsKey(cityLoc))
  //              world.enemyCityDict[cityLoc].TurnOffLights();
  //          else
  //              world.cityDict[cityLoc].TurnOffLights();
  //      }
  //  }

    public void ToggleBuildingHighlight(bool v, Vector3Int cityLoc)
    {        
        if (v)
        {
            foreach (CityImprovement building in world.cityBuildingDict[cityLoc].Values)
            {
                //CityImprovement building = world.GetBuildingData(cityLoc, name);
                building.DisableHighlight();

                if (removingImprovement)
                {
                    building.EnableHighlight(Color.red, true);
                }
                else if (upgradingImprovement)
                {
                    if (building.GetImprovementData.improvementLevel < world.GetUpgradeableObjectMaxLevel(building.GetImprovementData.improvementName))
                    {
                        building.canBeUpgraded = true;
                        building.EnableHighlight(Color.green, true);
                    }
                    else
                    {
                        building.canBeUpgraded = false;
                        building.EnableHighlight(Color.white);
                    }
                }
                else
                {
                    building.EnableHighlight(Color.white);
                }
            }
        }
        else
        {
			foreach (CityImprovement building in world.cityBuildingDict[cityLoc].Values)
            {
                //CityImprovement building = world.GetBuildingData(cityLoc, name);
                building.DisableHighlight();
            }
        }
    }


    //private (List<Vector3Int>, List<Vector3Int>) GetThisCityRadius() //radius just for selected city
    //{
    //    return world.GetCityRadiusFor(selectedCity.cityLoc);
    //}

    private void ResourceProducerTimeProgressBarsSetActive(bool v)
    {
        foreach (Vector3Int tile in developedTiles)
        {
            ResourceProducer producer = world.GetResourceProducer(tile);
            
            if (producer.isWaitingToUnload)
            {
                producer.TimeProgressBarSetActive(v);
                
                if (v)
                    producer.SetTimeProgressBarToFull();
            }
            else if (!producer.isWaitingForResearch && !producer.isWaitingForStorageRoom && !producer.isWaitingforResources)
            {
                if (producer.isUpgrading)
                {
                    //int time = 0;
                    //if (v)
                    //    time = world.GetCityDevelopment(tile).GetTimePassed;
                    world.GetResourceProducer(tile).TimeConstructionProgressBarSetActive(v);
                    continue;
                }
                producer.TimeProgressBarSetActive(v);
            }
        }

        foreach (Vector3Int tile in constructingTiles)
        {
            world.GetResourceProducer(tile).TimeConstructionProgressBarSetActive(v);
        }
    }

    private void DrawBorders()
    {
        List<Vector3Int> tempCityTiles = new(cityTiles) { selectedCity.cityLoc };

        for (int i = 0; i < tempCityTiles.Count; i++)
        {
			//finding border neighbors for tile
			List<Vector3Int> neighborList = world.GetNeighborsFor(tempCityTiles[i], MapWorld.State.FOURWAYINCREMENT);
            for (int j = 0; j < neighborList.Count; j++)
			{
				if (!tempCityTiles.Contains(neighborList[j])) //only draw borders on areas that aren't city tiles
				{
					//Object pooling set up
					GameObject tempObject = GetFromBorderPool();
					borderList.Add(tempObject);
					Vector3Int borderLocation = neighborList[j] - tempCityTiles[i]; //used to determine where on tile border should be
					Vector3 borderPosition = tempCityTiles[i];
					borderPosition.y = 0f;

					if (borderLocation.x != 0)
					{
						borderPosition.x += (0.5f * borderLocation.x);
						tempObject.transform.rotation = Quaternion.Euler(0, 90, 0); //rotating to make it look like city wall
					}
					else if (borderLocation.z != 0)
					{
						borderPosition.z += (0.5f * borderLocation.z);
					}

					tempObject.transform.position = borderPosition;
				}
			}
		}
    }

    public void CheckPopForUnit(UnitBuildDataSO unitData)
    {
        if (unitData.unitType == UnitType.Worker) 
        {
            uiLaborDestinationWindow.ToggleVisibility(true, selectedCity.cityLoc);
			uiCityTabs.HideSelectedTab(false);
			return;
        }
        
        CreateUnit(unitData, selectedCity, false);
    }

    public void CreateUnit(UnitBuildDataSO unitData, City city, bool upgrading, Unit upgradedUnit = null)
    {
        if (!upgrading)
        {
            city.ResourceManager.SpendResource(unitData.unitCost, city.cityLoc);

            for (int i = 0; i < unitData.laborCost; i++)
                city.PopulationDeclineCheck(true, true); //decrease population before creating unit so we can see where labor will be lost

            //updating uis after losing pop
            if (city.activeCity)
            {
                uiLaborAssignment.UpdateUI(city, placesToWork);
                uiInfoPanelCity.SetAllData(selectedCity);
                uiResourceManager.SetCityCurrentStorage(city.ResourceManager.ResourceStorageLevel);
                uiCityTabs.HideSelectedTab(false);
            }
        }

		Vector3Int buildPosition = city.cityLoc;

        if (upgradedUnit == world.azai)
        {
			buildPosition = upgradedUnit.currentLocation;
		}
        else if (unitData.unitType != UnitType.Laborer)
        {
            if (unitData.unitType == UnitType.Transport)
            {
                if (unitData.transportationType == TransportationType.Sea)
                    world.waterTransport = true;
                else if (unitData.transportationType == TransportationType.Air)
                    world.airTransport = true;
            }

			CityImprovement improvement = world.GetCityDevelopment(city.singleBuildDict[unitData.singleBuildType]);
			improvement.BeginTraining(city, improvement.resourceProducer, unitData, upgrading, upgradedUnit, false);
            return;
        }
  //      else if (unitData.baseAttackStrength > 0)
  //      {
  //          if (unitData.transportationType == TransportationType.Land)
  //          {
  //              CityImprovement improvement = world.GetCityDevelopment(city.singleBuildDict[SingleBuildType.Barracks]);
  //              improvement.BeginTraining(city, improvement.resourceProducer, unitData, upgrading, upgradedUnit, false);
  //              //selectedCity.army.isTraining = true;
  //              return;
  //          }
		//	else if (unitData.transportationType == TransportationType.Sea)
		//	{
		//		return;
		//	}
		//	else if (unitData.transportationType == TransportationType.Air)
		//	{
		//		CityImprovement improvement = world.GetCityDevelopment(city.singleBuildDict[SingleBuildType.AirBase]);
		//		improvement.BeginTraining(city, improvement.resourceProducer, unitData, upgrading, upgradedUnit, false);
		//		return;
		//	}
		//}
  //      else
  //      {
  //          if (unitData.transportationType == TransportationType.Land)
  //          {
  //              if (unitData.unitType == UnitType.Laborer)
  //              {

  //              }
  //          }
  //          else if (unitData.transportationType == TransportationType.Sea)
  //          {
  //              if (unitData.unitType == UnitType.Transport)
		//		    world.waterTransport = true;

  //          }
  //          else if (unitData.transportationType == TransportationType.Air)
  //          {
		//		if (unitData.unitType == UnitType.Transport)
  //              {
		//			world.airTransport = true;
  //                  CityImprovement improvement = world.GetCityDevelopment(city.singleBuildDict[SingleBuildType.Airport]);
		//		    improvement.BeginTraining(city, improvement.resourceProducer, unitData, upgrading, upgradedUnit, false);
  //                  return;
  //              }
  //          }

		//	CityImprovement improvement = world.GetCityDevelopment(city.singleBuildDict[unitData.singleBuildType]);
  //          improvement.BeginTraining(city, improvement.resourceProducer, unitData, upgrading, upgradedUnit, false);
  //          return;
  //      }
        //else if (upgrading)
        //{
        //    buildPosition = upgradedUnit.currentLocation;
        //}
        //else if (world.IsUnitLocationTaken(buildPosition)) //placing unit in world after building in city
        //{
        //    //List<Vector3Int> newPositions = world.GetNeighborsFor(Vector3Int.FloorToInt(buildPosition));
        //    foreach (Vector3Int pos in world.GetNeighborsFor(buildPosition, MapWorld.State.EIGHTWAY))
        //    {
        //        if (!world.IsUnitLocationTaken(pos) && world.GetTerrainDataAt(pos).walkable)
        //        {
        //            buildPosition = pos;
        //            break;
        //        }
        //    }

        //    if (buildPosition == world.RoundToInt(transform.position))
        //    {
        //        Debug.Log("No suitable locations to build unit");
        //        return;
        //    }
        //}

		GameObject unitGO = unitData.prefab;
        bool secondaryPrefab = false;

		if (unitData.secondaryPrefab != null && !world.tutorialGoing)
		{
			if (UnityEngine.Random.Range(0, 2) == 1)
            {
                secondaryPrefab = true;
				unitGO = unitData.secondaryPrefab;
            }
		}

        Vector3 buildLoc = buildPosition;
        if (world.GetTerrainDataAt(buildPosition).isHill)
        {
            if (buildPosition.z % 3 == 0 && buildPosition.x % 3 == 0)
                buildLoc.y += .6f;
            else
                buildLoc.y += .3f;
        }

		GameObject unit = Instantiate(unitGO, buildLoc, Quaternion.identity); //produce unit at specified position
        unit.transform.SetParent(friendlyUnitHolder, false);
        //for tweening
        Vector3 goScale = unitGO.transform.localScale;
        float scaleX = goScale.x;
        float scaleZ = goScale.z;
        unit.transform.localScale = new Vector3(scaleX, 0.1f, scaleZ);
        LeanTween.scale(unit, goScale, 0.5f).setEase(LeanTweenType.easeOutBack);
        Unit newUnit = unit.GetComponent<Unit>();
        newUnit.secondaryPrefab = secondaryPrefab;
		newUnit.SetReferences(world);
        newUnit.PlayAudioClip(buildClip);

        if (upgradedUnit == world.azai)
        {
            newUnit.transform.rotation = upgradedUnit.transform.rotation;
            newUnit.name = "Azai";
            world.azai = newUnit.GetComponent<BodyGuard>();
            world.characterUnits.Remove(upgradedUnit);
            upgradedUnit.DestroyUnit();
            world.characterUnits.Add(newUnit);
            world.uiSpeechWindow.AddToSpeakingDict("Azai", newUnit);
            world.azai.SetArmy();
			newUnit.currentLocation = world.AddUnitPosition(buildPosition, newUnit);
        }
        else //for laborers
        {
            world.laborerCount++;
            unit.name = "Laborer " + world.laborerCount;
            world.laborerList.Add(newUnit.GetComponent<Laborer>());
		    //newUnit.SetReferences(world);
            newUnit.SetMinimapIcon(friendlyUnitHolder);
            Vector3 mainCamLoc = Camera.main.transform.position;
            mainCamLoc.y = 0;
            unit.transform.rotation = Quaternion.LookRotation(mainCamLoc - unit.transform.position);
        }
        //transferring all previous trader info to new one
  //      if (upgrading && upgradedUnit.isTrader)
		//{
  //          world.traderList.Remove(upgradedUnit.trader);
  //          newUnit.trader.name = upgradedUnit.name;
		//	//world.traderList.Add(newTrader);
		//	newUnit.trader.hasRoute = upgradedUnit.trader.hasRoute;
		//	newUnit.trader.tradeRouteManager = upgradedUnit.trader.tradeRouteManager;
		//	newUnit.trader.tradeRouteManager.SetTrader(newUnit.trader);
		//	newUnit.trader.personalResourceManager = upgradedUnit.trader.personalResourceManager;
		//	newUnit.trader.resourceGridDict = upgradedUnit.trader.resourceGridDict;
  //          selectedCity.tradersHere.Remove(upgradedUnit);
  //          upgradedUnit.DestroyUnit();
  //          //upgradedUnit.gameObject.SetActive(false);
  //      }
        //else
        //{
        //}
 
   //     if (newUnit.trader)
   //     {
   //         world.traderCount++;
   //         if (!upgrading)
   //             unit.name = "Trader " + world.traderCount;
    
			//newUnit.trader.id = world.traderCount;
   //         world.traderList.Add(newUnit.trader);
   //         selectedCity.tradersHere.Add(newUnit);
   //     }

        //if (newUnit.isLaborer)
        //{
        //}

        //if (unitData.baseAttackStrength > 0)
        //{
        //    city.army.AddToArmy(newUnit);
        //    newUnit.homeBase = city;
        //}

        //newUnit.currentLocation = world.AddUnitPosition(buildPosition, newUnit);
	}

    public void BuildUnit(City city, UnitBuildDataSO unitData, bool upgrading, Unit upgradedUnit)
    {
        Vector3Int buildPosition = city.cityLoc;
        bool reselectAfterUpgrade = false;

        if (unitData.baseAttackStrength > 0)
        {
		    if (unitData.transportationType == TransportationType.Sea)
            {

			}
            else if (unitData.transportationType == TransportationType.Air)
            {

            }
			else
            {
		        if (upgrading)
                {
				    buildPosition = upgradedUnit.currentLocation;
				    reselectAfterUpgrade = upgradedUnit.isSelected;
				    upgradedUnit.RemoveUnitFromData();
					upgradedUnit.military.army.RemoveFromArmy(upgradedUnit.military, upgradedUnit.military.barracksBunk);
				    city.army.AddToOpenSpots(buildPosition);
				    upgradedUnit.DestroyUnit();
		        }
                else
                {
		            buildPosition = city.army.GetAvailablePosition(unitData.unitType);
                }
            }
        }
        else 
        {
            if (upgrading)
            {
                buildPosition = upgradedUnit.currentLocation;
            }
            else
            {
                buildPosition = world.GetTraderBuildLoc(city.singleBuildDict[unitData.singleBuildType]);
            }

		    if (city.activeCity && uiUnitBuilder.activeStatus)
			    uiUnitBuilder.UpdateTrainingStatus(unitData.singleBuildType);
		}

            //if (unitData.transportationType == TransportationType.Sea)
            //{
				//if (uiUnitBuilder.activeStatus)

    //            if (unitData.unitType == UnitType.Transport) //placing transport ship next to land
				//{                    
    //                Vector3Int closestLand = city.singleBuildDict[SingleBuildType.Harbor];
    //                int dist = 0;
    //                bool firstOne = true;
    //                foreach (Vector3Int pos in world.GetNeighborsFor(city.singleBuildDict[SingleBuildType.Harbor], MapWorld.State.FOURWAYINCREMENT))
    //                {
    //                    TerrainData td = world.GetTerrainDataAt(pos);
    //                    if (td.isLand && td.walkable && !td.enemyZone)
    //                    {
    //                        if (firstOne)
    //                        {
    //                            firstOne = false;
    //                            dist = Math.Abs(pos.x - city.cityLoc.x) + Math.Abs(pos.z - city.cityLoc.z);
    //                            closestLand = pos;
    //                            continue;
    //                        }
                            
    //                        int newDist = Math.Abs(pos.x - city.cityLoc.x) + Math.Abs(pos.z - city.cityLoc.z);
    //                        if (newDist < dist)
    //                        {
    //                            dist = newDist;
    //							closestLand = pos;
    //                        }
    //                    }
    //                }

    //                Vector3Int diff = (closestLand - city.singleBuildDict[SingleBuildType.Harbor]) / 3;
    //                Vector3Int newLoc = city.singleBuildDict[SingleBuildType.Harbor] + diff;

    //                if (diff.z == 0)
    //                    newLoc.z += 1;
    //                else
    //                    newLoc.x += 1;

    //                buildPosition = newLoc;
    //            }
    //            else
    //            {
    //                buildPosition = city.singleBuildDict[SingleBuildType.Harbor];
                    
    //                if (world.IsUnitLocationTaken(buildPosition))
    //                {
			 //           foreach (Vector3Int pos in world.GetNeighborsFor(buildPosition, MapWorld.State.EIGHTWAY))
			 //           {
				//            if (!world.IsUnitLocationTaken(pos) && world.GetTerrainDataAt(pos).sailable)
				//            {
				//	            buildPosition = pos;
				//	            break;
				//            }
			 //           }
    //                }
    //            }
            //}
			//else if (unitData.transportationType == TransportationType.Air)
   //         {

   //         }
   //         else
   //         {

   //         }


		GameObject unitGO = unitData.prefab;
		GameObject unit = Instantiate(unitGO, buildPosition, Quaternion.identity); //produce unit at specified position
		unit.gameObject.transform.SetParent(friendlyUnitHolder, false);

		//for tweening
		Vector3 goScale = unitGO.transform.localScale;
        float scaleX = goScale.x;
        float scaleZ = goScale.z;
        unit.transform.localScale = new Vector3(scaleX, 0.1f, scaleZ);
        LeanTween.scale(unit, goScale, 0.5f).setEase(LeanTweenType.easeOutBack);

		//unit.name = unit.name.Replace("(Clone)", ""); //getting rid of the clone part in name 
		Unit newUnit = unit.GetComponent<Unit>();
		newUnit.SetReferences(world);
		newUnit.SetMinimapIcon(friendlyUnitHolder);
        newUnit.PlayAudioClip(buildClip);

		Vector3 mainCamLoc = Camera.main.transform.position;
		mainCamLoc.y = 0;
		Vector3 rot = mainCamLoc - unit.transform.position;

		//transferring all previous trader info to new one
		if (newUnit.trader)
		{
			if (upgrading)
            {
                world.traderList.Remove(upgradedUnit.trader);
				newUnit.trader.name = upgradedUnit.name;
				newUnit.trader.id = upgradedUnit.trader.id;
				newUnit.trader.hasRoute = upgradedUnit.trader.hasRoute;
				newUnit.trader.tradeRouteManager = upgradedUnit.trader.tradeRouteManager;
				newUnit.trader.tradeRouteManager.SetTrader(newUnit.trader);
				newUnit.trader.personalResourceManager = upgradedUnit.trader.personalResourceManager;
				newUnit.trader.resourceGridDict = upgradedUnit.trader.resourceGridDict;
				world.GetCityDevelopment(city.singleBuildDict[upgradedUnit.buildDataSO.singleBuildType]).RemoveTraderFromImprovement(upgradedUnit);
				//city.tradersHere.Remove(upgradedUnit);
			    upgradedUnit.RemoveUnitFromData();
			    upgradedUnit.DestroyUnit();
            }

            world.GetCityDevelopment(city.singleBuildDict[unitData.singleBuildType]).AddTraderToImprovement(newUnit);
            world.traderCount++;
			if (!upgrading)
				unit.name = "Trader " + world.traderCount;

			newUnit.trader.id = world.traderCount;
            world.traderList.Add(newUnit.trader);
            //city.tradersHere.Add(newUnit);
            newUnit.trader.atHome = true;
            newUnit.trader.homeCity = city.cityLoc;
		}

		//assigning army details and rotation
		if (newUnit.inArmy)
        {
		    newUnit.military.atHome = true;
		    city.army.AddToArmy(newUnit.military);
		    newUnit.military.army = city.army;
            newUnit.military.barracksBunk = buildPosition;

            if (newUnit.military.army.selected)
                newUnit.SoftSelect(Color.white);
            else if (world.assigningGuard && world.uiTradeRouteBeginTooltip.MilitaryLocCheck(world.GetClosestTerrainLoc(buildPosition)))
                newUnit.SoftSelect(Color.green);
        
            rot = city.army.GetRandomSpot(newUnit.military.barracksBunk) - newUnit.transform.position;
            //rot += new Vector3(0, 0.05f, 0); //to avoid the warning message

			if (city.activeCity && uiUnitBuilder.activeStatus)
				uiUnitBuilder.UpdateBarracksStatus(city.army.isFull);
			else if (world.uiCampTooltip.activeStatus && world.uiCampTooltip.improvement == world.GetCityDevelopment(city.singleBuildDict[SingleBuildType.Barracks]))
				world.uiCampTooltip.RefreshData();
    //        else if (city.army.selected)
				//newUnit.SoftSelect(Color.white);

            newUnit.name = unitData.unitDisplayName;
            if (!upgrading)
            {
                switch (unitData.unitType)
			    {
				    case UnitType.Infantry:
					    world.infantryCount++;
					    break;
					case UnitType.Ranged:
						world.rangedCount++;
						break;
					case UnitType.Cavalry:
						world.cavalryCount++;
						break;
				}
			}
		}
        else if (newUnit.transport)
        {
            newUnit.name = unitData.unitDisplayName;
            world.transportList.Add(newUnit.transport);
        }

        Quaternion rotation;
        if (rot == Vector3.zero)
            rotation = Quaternion.identity;
        else
            rotation = Quaternion.LookRotation(rot);
        newUnit.transform.rotation = rotation;
		newUnit.currentLocation = world.AddUnitPosition(buildPosition, newUnit);

        if (world.unitMovement.upgradingUnit)
            world.unitMovement.ToggleUnitHighlights(true, city);

        if (reselectAfterUpgrade)
            world.unitMovement.PrepareMovement(newUnit);
	}

	public void TransferLaborPrep(string destination)
	{
        if (!world.CheckCityName(destination) && !world.CheckWonderName(destination))
        {
			InfoPopUpHandler.WarningMessage().Create(selectedCity.cityLoc, "Destination no londer available");
			return;
        }

        if (selectedCity.currentPop == 0)
        {
			InfoPopUpHandler.WarningMessage().Create(selectedCity.cityLoc, "No pop available");
			return;
        }

        selectedCity.PopulationDeclineCheck(true, true);
		//updating uis after losing pop
		//UpdateLaborNumbers(selectedCity);
		uiLaborAssignment.UpdateUI(selectedCity, placesToWork);
		uiInfoPanelCity.SetAllData(selectedCity);
		resourceManager.UpdateUI(world.laborerData.unitCost);
		uiResourceManager.SetCityCurrentStorage(selectedCity.ResourceManager.ResourceStorageLevel);
		uiCityTabs.HideSelectedTab(false);

		Vector3Int buildPosition = selectedCity.cityLoc;

		if (world.IsUnitLocationTaken(buildPosition)) //placing unit in world after building in city
		{
			foreach (Vector3Int pos in world.GetNeighborsFor(buildPosition, MapWorld.State.EIGHTWAY))
			{
				if (!world.IsUnitLocationTaken(pos) && world.GetTerrainDataAt(pos).walkable)
				{
					buildPosition = pos;
					break;
				}
			}
		}

		bool secondaryPrefab;

        if (selectedCity.currentPop % 2 == 0)
            secondaryPrefab = false;
        else
            secondaryPrefab = true;

        Vector3Int loc = world.GetStopLocation(destination);
        TransferWorker(secondaryPrefab, loc, buildPosition, false);
	}


	public void UpgradeUnitWindow(Unit unit)
    {
        uiCityUpgradePanel.ToggleVisibility(true, selectedCity.ResourceManager, null, unit);
    }

	public void CreateBuildingQueueCheck(ImprovementDataSO buildingData)
    {
        //Queue info
        if (isQueueing)
        {
            //uiQueueManager.AddToQueue(selectedCityLoc, new Vector3Int(0, 0, 0), selectedCityLoc, buildingData);
            selectedCity.AddToQueue(buildingData, selectedCity.cityLoc, Vector3Int.zero, false);
            return;
        }

        CreateBuilding(buildingData, selectedCity, false);
    }

    private void CreateBuilding(ImprovementDataSO buildingData, City city, bool upgradingImprovement)
    {
        uiQueueManager.CheckIfBuiltItemIsQueued(city.cityLoc, new Vector3Int(0, 0, 0), upgradingImprovement, buildingData, city);

        if (buildingData.improvementName == city.housingData.improvementName)
        {
            city.SetHouse(buildingData, city.cityLoc, world.GetTerrainDataAt(city.cityLoc).isHill, upgradingImprovement);

            if (city.activeCity)
            {
                uiInfoPanelCity.UpdateHousing(city.HousingCount);
                //resourceManager.UpdateUI(buildingData.improvementCost);
                uiResourceManager.SetCityCurrentStorage(city.ResourceManager.ResourceStorageLevel);

                if (!upgradingImprovement)
                    uiCityTabs.HideSelectedTab(false);
            }

            return;
        }
        //for some non buildings in the building selection list (eg harbor)
        if (buildingData.isBuildingImprovement)
        {
            CreateImprovement(buildingData);
            return;
        }

        //setting building locations
        Vector3 cityPos = city.cityLoc;
        Vector3 buildingLocalPos = buildingData.buildingLocation; //putting the building in it's position in the city square
        cityPos += buildingLocalPos;

        if (!upgradingImprovement)
        {
            city.ResourceManager.SpendResource(buildingData.improvementCost, cityPos);
		}
		PlayBoomAudio();

        GameObject building;
        if (world.GetTerrainDataAt(city.cityLoc).isHill)
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
        improvement.SetWorld(world);
        improvement.loc = city.cityLoc;
        building.transform.parent = city.subTransform;

        string buildingName = buildingData.improvementName;
		improvement.PlaySmokeSplashBuilding();
		world.SetCityBuilding(improvement, buildingData, city.cityLoc, building, city, buildingName);

        if (buildingData.workEthicChange != 0)
        {
            if (city.activeCity && uiCityTabs.builderUI != null)
                uiCityTabs.builderUI.UpdateProducedNumbers(city.ResourceManager);
		    else if (world.uiCityImprovementTip.activeStatus)
			    world.uiCityImprovementTip.UpdateProduceNumbers();
        }
		//world.AddToCityMaxLaborDict(city.cityLoc, buildingName, buildingData.maxLabor);
		city.HousingCount += buildingData.housingIncrease;
        if (buildingData.waterIncrease > 0)
        {
            city.waterCount += buildingData.waterIncrease;
            if (city.activeCity)
                uiInfoPanelCity.UpdateWater(city.waterCount);

			if (city.waterCount <= 0)
				city.reachedWaterLimit = true;
			else
				city.reachedWaterLimit = false;
		}

        //for tweening
        Vector3 goScale = building.transform.localScale;
        building.transform.localScale = Vector3.zero;
        LeanTween.scale(building, goScale, 0.25f).setEase(LeanTweenType.easeOutBack).setOnComplete( ()=> { CombineMeshes(city, city.subTransform, this.upgradingImprovement); 
            improvement.SetInactive(); ToggleBuildingHighlight(true, city.cityLoc); });

        if (buildingData.singleBuildType != SingleBuildType.None)
        {
            city.singleBuildList.Add(buildingData.singleBuildType);
        }

        if (buildingData.singleBuildType == SingleBuildType.Market)
        {
            if (city.activeCity)
                uiCityTabs.marketButton.SetActive(true);
        }

        //updating uis
        if (city.activeCity)
        {
            if (buildingData.housingIncrease > 0)  
                uiInfoPanelCity.UpdateHousing(city.HousingCount);
            if (buildingData.workEthicChange != 0)
            {
                uiInfoPanelCity.UpdateWorkEthic(city.workEthic);
                UpdateCityWorkEthic();
            }
     
            //resourceManager.UpdateUI(buildingData.improvementCost);
            uiResourceManager.SetCityCurrentStorage(city.ResourceManager.ResourceStorageLevel);

            if (!upgradingImprovement)
                uiCityTabs.HideSelectedTab(false);
        }
    }

    private void RemoveBuilding(CityImprovement improvement, ImprovementDataSO data, City city, bool upgradingImprovement)
    {
		if (!upgradingImprovement)
            improvement.PlayRemoveEffect(world.GetTerrainDataAt(city.cityLoc).isHill);

		//putting the resources and labor back
		string selectedBuilding = data.improvementName;
        if (selectedBuilding == city.housingData.improvementName)
            selectedBuilding = city.DecreaseHousingCount(improvement.housingIndex);

        CityImprovement building = world.GetBuildingData(city.cityLoc, selectedBuilding);

        if (!upgradingImprovement)
        {
            int i = 0;
            Vector3 cityLoc = city.cityLoc;
            cityLoc.y += data.improvementCost.Count * 0.4f;

            resourceManager.resourceCount = 0;
            foreach (ResourceValue resourceValue in data.improvementCost)
            {
                int resourcesReturned = resourceManager.AddResource(resourceValue.resourceType, resourceValue.resourceAmount);
                Vector3 cityLoc2 = cityLoc;
                cityLoc2.y += -.4f * i;
                InfoResourcePopUpHandler.CreateResourceStat(cityLoc2, resourcesReturned, ResourceHolder.Instance.GetIcon(resourceValue.resourceType));
                i++;
            }
        }

        //changing city stats
        city.HousingCount -= data.housingIncrease;
        city.workEthic -= data.workEthicChange;
        city.improvementWorkEthic -= data.workEthicChange;

		if (data.waterIncrease > 0)
        {
            city.waterCount -= data.waterIncrease;
			uiInfoPanelCity.UpdateWater(city.waterCount);
            
            if (city.waterCount <= 0)
                city.reachedWaterLimit = true;
            else
				city.reachedWaterLimit = false;
        }

        if (data.singleBuildType != SingleBuildType.None)
            city.singleBuildList.Remove(data.singleBuildType);

		if (data.singleBuildType == SingleBuildType.Market)
        {
            //city.hasMarket = false;
            uiCityTabs.marketButton.SetActive(false);
        }


        //if (city.singleBuildImprovementsBuildingsDict.ContainsKey(selectedBuilding))
        //    city.singleBuildImprovementsBuildingsDict.Remove(selectedBuilding);

        //updating ui
        if (city.activeCity)
        {
            if (data.workEthicChange != 0)
                UpdateCityWorkEthic();
            //resourceManager.UpdateUI(data.improvementCost);
            uiResourceManager.SetCityCurrentStorage(resourceManager.ResourceStorageLevel);
            uiInfoPanelCity.SetAllData(selectedCity);
        }

        city.RemoveFromMeshFilterList(true, Vector3Int.zero, selectedBuilding);
        Destroy(building.gameObject);

        //updating world dicts
        world.cityBuildingDict[city.cityLoc].Remove(selectedBuilding);
        
        //updating city graphic
        CombineMeshes(city, city.subTransform, this.upgradingImprovement);
    }

    public void CreateImprovement(ImprovementDataSO improvementData)
    {
        laborChange = 0;

        this.improvementData = improvementData;

        if (!upgradingImprovement)
            uiCityTabs.HideSelectedTab(false);
        ImprovementTileHighlight(false);
    }

    private void ImprovementTileHighlight(bool removingImprovement)
    {
        tilesToChange.Clear();

        if (removingImprovement)
        {
            if (!upgradingImprovement) //only show when not upgrading
            {
                uiImprovementBuildInfoPanel.SetText("Removing Items");
                uiImprovementBuildInfoPanel.SetImage(removeButton, false);
                uiImprovementBuildInfoPanel.ToggleVisibility(true);
            }
        }
        else
        {
            uiImprovementBuildInfoPanel.SetText("Building " + improvementData.improvementName);
            uiImprovementBuildInfoPanel.SetImage(improvementData.image, true);
            uiImprovementBuildInfoPanel.ToggleVisibility(true);
        }

        foreach (Vector3Int tile in cityTiles)
        {
            TerrainData td = world.GetTerrainDataAt(tile);
            td.DisableHighlight(); //turning off all highlights first

            if (removingImprovement) //If removing improvement
            {                                
                if (world.TileHasCityImprovement(tile))
                {
                    CityImprovement improvement = world.GetCityDevelopment(tile); //cached for speed
                    improvement.DisableHighlight();

                    if (!improvement.isConstruction)
                        improvement.EnableHighlight(Color.red);
    
                    td.EnableHighlight(Color.red);
                    tilesToChange.Add(tile);
                }                
                //else if (world.CheckIfTileIsUnderConstruction(tile))
                //{
                //    //CityImprovement improvement = world.GetCityDevelopmentConstruction(tile);
                //    //improvement.DisableHighlight();

                //    //improvement.EnableHighlight(Color.red);
                //    td.EnableHighlight(Color.red);
                //    tilesToChange.Add(tile);
                //}
            }
            else //if placing improvement
            {
                if (isQueueing && world.CheckQueueLocation(tile))
                    continue;
                
                if (world.IsTileOpenCheck(tile) && !world.IsInBattleArea(tile))
                {
                    TerrainType type = td.terrainData.type;

                    //for building sea improvements on rivers
                    if (type == TerrainType.River)
                        type = TerrainType.Coast;

                    if (improvementData.rawMaterials)
                    {
                        if (td.rawResourceType == improvementData.rawResourceType)
                        {
                            if (improvementData.oneTerrain && type!= improvementData.terrainType)
                                continue;
                        
                            td.EnableHighlight(Color.white);
                            tilesToChange.Add(tile);
                        }
                    }
                    else
                    {
                        if (improvementData.oneTerrain && type != improvementData.terrainType)
                            continue;

                        td.EnableHighlight(Color.white);
                        tilesToChange.Add(tile);
                    }
                }
            }
        }
    }

    public void BuildImprovementQueueCheck(ImprovementDataSO improvementData, Vector3Int tempBuildLocation)
    {
        //queue information
        if (isQueueing)
        {
            //if (!uiQueueManager.AddToQueue(tempBuildLocation, tempBuildLocation - selectedCityLoc, selectedCityLoc, improvementData))
            if (!selectedCity.AddToQueue(improvementData, tempBuildLocation, tempBuildLocation - selectedCity.cityLoc, false))
                return;
            //else
            //    SelectedCity.improvementQueueLocs.Add(tempBuildLocation);
            tilesToChange.Remove(tempBuildLocation);
            world.GetTerrainDataAt(tempBuildLocation).DisableHighlight();
            return;
        }

        BuildImprovement(improvementData, tempBuildLocation, selectedCity, false);
    }

    private void BuildImprovement(ImprovementDataSO improvementData, Vector3Int tempBuildLocation, City city, bool upgradingImprovement)
    {
        uiQueueManager.CheckIfBuiltItemIsQueued(tempBuildLocation, tempBuildLocation - city.cityLoc, upgradingImprovement, improvementData, city);
        //RemoveQueueGhostImprovement(tempBuildLocation, city);
        world.CheckTileForTreasure(tempBuildLocation);

        if (!upgradingImprovement && !world.IsTileOpenCheck(tempBuildLocation))
        {
            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Something already here");
            return;
        }

        if (!upgradingImprovement)
    		PlayConstructionAudio();

		//spending resources to build
		Vector3Int buildLocation = tempBuildLocation;
        buildLocation.y = 0;

        if (!upgradingImprovement)
        {
            city.ResourceManager.SpendResource(improvementData.improvementCost, buildLocation);
        }

        if (city.activeCity)
        {
            //resourceManager.UpdateUI(improvementData.improvementCost);
            uiResourceManager.SetCityCurrentStorage(city.ResourceManager.ResourceStorageLevel);
        }

        //rotating harbor so it's closest to city
        int rotation = 0;
		if (improvementData.singleBuildType == SingleBuildType.Harbor)
		{
            rotation = HarborRotation(tempBuildLocation, city.cityLoc);
        }

        //adding improvement to world dictionaries
        TerrainData td = world.GetTerrainDataAt(tempBuildLocation);

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

        world.AddStructure(buildLocation, improvement);
        CityImprovement cityImprovement = improvement.GetComponent<CityImprovement>();
        cityImprovement.SetWorld(world);
        cityImprovement.loc = buildLocation;
        cityImprovement.InitializeImprovementData(improvementData);
        //cityImprovement.SetPSLocs();
        cityImprovement.SetQueueCity(null);
        cityImprovement.building = improvementData.isBuilding;
        cityImprovement.city = city;

        world.SetCityDevelopment(tempBuildLocation, cityImprovement);
        //improvement.SetActive(false);
        cityImprovement.improvementMesh.SetActive(false);

        //setting single build rules
        if (improvementData.singleBuildType != SingleBuildType.None)
        {
            city.singleBuildList.Add(improvementData.singleBuildType);
            world.AddToCityLabor(tempBuildLocation, city.cityLoc);
            //city.singleBuildDict[improvementData.singleBuildType] = tempBuildLocation;
        }
        //if (improvementData.singleBuild)
        //{
        //}

        //resource production
        ResourceProducer resourceProducer = improvement.GetComponent<ResourceProducer>();
        buildLocation.y = 0;
        world.AddResourceProducer(tempBuildLocation, resourceProducer);
        resourceProducer.SetResourceManager(city.ResourceManager);
        resourceProducer.InitializeImprovementData(improvementData, td.resourceType); //allows the new structure to also start generating resources
        resourceProducer.SetCityImprovement(cityImprovement);
        resourceProducer.SetLocation(tempBuildLocation);
        cityImprovement.CheckPermanentChanges();

        if (upgradingImprovement)
        {
            resourceProducer.SetUpgradeProgressTimeBar(improvementData.buildTime);
            FinishImprovement(city, improvementData, tempBuildLocation);
        }
        else
        {
            cityImprovement.isConstruction = true;
            //CityImprovement constructionTile = GetFromConstructionTilePool();
            //constructionTile.InitializeImprovementData(improvementData);
            //world.SetCityImprovementConstruction(tempBuildLocation, constructionTile);
            //constructionTile.transform.position = tempBuildLocation;
            //TerrainData td = world.GetTerrainDataAt(tempBuildLocation);
            cityImprovement.BeginImprovementConstructionProcess(city, resourceProducer, tempBuildLocation, this, td.isHill, false);

            if (city.activeCity)
            {
                constructingTiles.Add(tempBuildLocation);
                //ResetTileLists();
                CloseImprovementBuildPanel();
            }
        }
    }

    public void FinishImprovement(City city, ImprovementDataSO improvementData, Vector3Int tempBuildLocation)
    {
		world.TutorialCheck("Finished Building Something");
		//activating structure
		GameObject improvement = world.GetStructure(tempBuildLocation);
        //world.AddStructureMap(tempBuildLocation, improvementData.mapIcon);
        //improvement.SetActive(true);
        TerrainData td = world.GetTerrainDataAt(tempBuildLocation);
        CityImprovement cityImprovement = world.GetCityDevelopment(tempBuildLocation);
        cityImprovement.improvementMesh.SetActive(true);
        cityImprovement.HideIdleMesh();
        cityImprovement.isConstruction = false;
        cityImprovement.SetMinimapIcon(td);
        cityImprovement.meshCity = city;
        cityImprovement.transform.parent = city.transform;
        city.AddToImprovementList(cityImprovement);
        cityImprovement.PlaySmokeSplash(td.isHill);
        cityImprovement.PlayPlacementAudio(buildClip);

        if (improvementData.housingIncrease > 0)
        {
			city.HousingCount += improvementData.housingIncrease;

            if (city.activeCity)
    			uiInfoPanelCity.UpdateHousing(city.HousingCount);

			//must go last
			if (world.uiCityPopIncreasePanel.CheckCity(city))
                world.uiCityPopIncreasePanel.UpdateHousingCosts(city);
        }

        if (improvementData.waterIncrease > 0)
        {
			city.waterCount += improvementData.waterIncrease;
			if (city.activeCity)
				uiInfoPanelCity.UpdateWater(city.waterCount);

			if (city.waterCount <= 0)
				city.reachedWaterLimit = true;
			else
				city.reachedWaterLimit = false;

			//must go last
			if (world.uiCityPopIncreasePanel.CheckCity(city))
				world.uiCityPopIncreasePanel.UpdateWaterCosts(city);
		}

        if (improvementData.workEthicChange != 0)
        {
            city.workEthic += improvementData.workEthicChange;
            city.improvementWorkEthic += improvementData.workEthicChange;
            
            if (city.activeCity && uiCityTabs.builderUI != null)
				uiCityTabs.builderUI.UpdateProducedNumbers(city.ResourceManager);
			else if (world.uiCityImprovementTip.activeStatus)
				world.uiCityImprovementTip.UpdateProduceNumbers();
		}

		//making two objects, this one for the parent mesh
		GameObject tempObject = Instantiate(emptyGO, cityImprovement.transform.position, cityImprovement.transform.rotation);
        tempObject.name = improvement.name;
        MeshFilter[] improvementMeshes = cityImprovement.MeshFilter;

        MeshFilter[] meshes = new MeshFilter[improvementMeshes.Length];
        int k = 0;

        foreach (MeshFilter mesh in improvementMeshes)
        {
            Quaternion rotation = mesh.transform.rotation;
            meshes[k] = Instantiate(mesh, mesh.transform.position, rotation);
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
            td.ToggleTerrainMesh(false);

            foreach (MeshFilter mesh in cityImprovement.MeshFilter)
            {
                if (mesh.name == "Ground")
                {
                    Vector2[] terrainUVs = td.UVs;
                    Vector2[] newUVs = mesh.mesh.uv;
					Vector2[] finalUVs = world.NormalizeUVs(terrainUVs, newUVs, Mathf.RoundToInt(td.main.localEulerAngles.y / 90));
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
            
            //for tweening
            Vector3 goScale = improvement.transform.localScale;
            improvement.transform.localScale = Vector3.zero;
            LeanTween.scale(improvement, goScale, 0.4f).setEase(LeanTweenType.easeOutBack).setOnComplete( () => { CombineMeshes(city, city.subTransform, this.upgradingImprovement); cityImprovement.SetInactive(); TileCheck(tempBuildLocation, city, improvementData.maxLabor); });
        }

		if (td.terrainData.type == TerrainType.Forest || td.terrainData.type == TerrainType.ForestHill)
			td.SwitchToRoad();
		//LeanTween.moveLocalY(td.gameObject, -0.5f, 0.4f).setEase(LeanTweenType.linear);

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

        //if (improvementData.replaceTerrain)
        //{
        //    world.GetTerrainDataAt(tempBuildLocation).gameObject.SetActive(false);
        //}

        if (improvementData.singleBuildType != SingleBuildType.None)
        {
            city.singleBuildDict[improvementData.singleBuildType] = tempBuildLocation;
            if (city.activeCity && uiUnitBuilder.activeStatus)
                uiUnitBuilder.UpdateTrainingStatus(improvementData.singleBuildType);
        }

        //setting harbor info
        if (improvementData.singleBuildType == SingleBuildType.Harbor)
        {
            //city.hasHarbor = true;
            //city.harborLocation = tempBuildLocation;
			cityImprovement.mapIconHolder.localRotation = Quaternion.Inverse(improvement.transform.rotation);
			world.AddTradeLoc(tempBuildLocation, city.cityName);
        }
        else if (improvementData.singleBuildType == SingleBuildType.Barracks)
        {
            world.militaryStationLocs.Add(tempBuildLocation);
            //city.hasBarracks = true;
            //city.barracksLocation = tempBuildLocation;
            cityImprovement.army = city.army;

			foreach (Vector3Int tile in world.GetNeighborsFor(tempBuildLocation, MapWorld.State.EIGHTWAYARMY))
                city.army.SetArmySpots(tile);

            city.army.SetLoc(tempBuildLocation, city);

            if (city.activeCity && uiUnitBuilder.activeStatus)
                uiUnitBuilder.UpdateBarracksStatus(city.army.isFull);
        }

        //setting labor info (harbors have no labor)
        world.AddToMaxLaborDict(tempBuildLocation, improvementData.maxLabor);
        if (city.AutoAssignLabor && city.unusedLabor > 0 && improvementData.maxLabor > 0)
            city.AutoAssignmentsForLabor();

        //removing areas to work
        foreach (Vector3Int loc in improvementData.noWalkAreas)
            world.AddToNoWalkList(loc + tempBuildLocation);

        //no tweening, so must be done here
        if (improvementData.replaceTerrain)
        {
            CombineMeshes(city, city.subTransform, this.upgradingImprovement); 
            cityImprovement.SetInactive();
            TileCheck(tempBuildLocation, city, improvementData.maxLabor);
        }

        constructingTiles.Remove(tempBuildLocation); //don't need to check for active city
    }

    private void TileCheck(Vector3Int tempBuildLocation, City city, int maxLabor)
    {
        if (selectedCity != null && cityTiles.Contains(tempBuildLocation))
        {
            developedTiles.Add(tempBuildLocation);

            //in case improvement is made while another city is selected and the city that made it has auto assign on
            if (city.activeCity)
            {
                if (maxLabor > 0)
                {
                    placesToWork++;
                    UpdateCityLaborUIs();
                }

                if (laborChange != 0)
                {
                    uiLaborAssignment.ToggleInteractable(laborChange);
                    LaborTileHighlight();
                }
                else if (removingImprovement && uiImprovementBuildInfoPanel.activeStatus)
                {
                    ImprovementTileHighlight(true);
                }
                else if (upgradingImprovement && uiImprovementBuildInfoPanel.activeStatus)
                {
                    UpgradeTileHighlight();
                }
            }
            else
            {
                (cityTiles, developedTiles, constructingTiles) = world.GetCityRadiusFor(selectedCity.cityLoc);
                HideBorders();
                DrawBorders();
            }
        }
    }

    public int HarborRotation(Vector3Int tempBuildLocation, Vector3Int originationLocation)
    {
        int rotation = 0;
        int minimum = 99999;
        int rotationIndex = 0;

        foreach (Vector3Int neighbor in world.GetNeighborsFor(tempBuildLocation, MapWorld.State.FOURWAYINCREMENT))
        {
            if (!world.GetTerrainDataAt(neighbor).sailable) //don't place harbor on neighboring water tiles
            {
                //int distanceFromCity = neighbor.sqrMagnitude - originationLocation.sqrMagnitude;
                int distanceFromCity = Math.Abs(neighbor.x - originationLocation.x) + Math.Abs(neighbor.z - originationLocation.z);
                if (distanceFromCity < minimum)
                {
                    minimum = distanceFromCity;
                    rotation = rotationIndex * 90;
                }
            }

            rotationIndex++;
        }
        
        return rotation;
    }

    //boolean is to see to play sound or not
    public bool RemoveImprovement(Vector3Int improvementLoc, CityImprovement selectedImprovement, bool upgradingImprovement, bool destroyingCity = false, bool enemy = false)
    {
        ImprovementDataSO improvementData = selectedImprovement.GetImprovementData;
        //remove building
        if (selectedImprovement.GetImprovementData.isBuilding && !selectedImprovement.GetImprovementData.isBuildingImprovement)
        {
            RemoveBuilding(selectedImprovement, improvementData, selectedImprovement.city, upgradingImprovement);
            return true;
        }

        bool updateCity = false;
        if (selectedCity != null && cityTiles.Contains(improvementLoc))
            updateCity = true;

		if (selectedImprovement.queued && updateCity)
            uiQueueManager.CheckIfBuiltItemIsQueued(improvementLoc, improvementLoc - selectedImprovement.city.cityLoc, true, improvementData, selectedImprovement.GetQueueCity());

        ResourceProducer resourceProducer = selectedImprovement.resourceProducer;
		TerrainData td = world.GetTerrainDataAt(improvementLoc);

		//if cancelling training in a barracks or harbor, stop here
		if (selectedImprovement.isTraining)
		{
			if (!destroyingCity && !enemy)
                ReplaceImprovementCost(selectedImprovement.upgradeCost, improvementLoc);

			if (!selectedImprovement.isUpgrading)
			{
				selectedImprovement.city.PopulationGrowthCheck(true, selectedImprovement.laborCost);
				if (updateCity)
                    UpdateCityLaborUIs();
			}

			selectedImprovement.CancelTraining(resourceProducer);
			selectedImprovement.StopUpgrade();
            if (updateCity)
            {
    			uiResourceManager.SetCityCurrentStorage(selectedImprovement.city.ResourceManager.ResourceStorageLevel);
			    ImprovementTileHighlight(true);
            }

            if (!enemy || selectedImprovement.unitsWithinCount > 0)
    			return true;
		}
		//if removing/canceling upgrade process, stop here
		else if (selectedImprovement.isUpgrading)
        {
            if (!destroyingCity && !enemy)
                ReplaceImprovementCost(selectedImprovement.upgradeCost, improvementLoc);
            selectedImprovement.StopUpgradeProcess(resourceProducer);
            selectedImprovement.StopUpgrade();

            if (updateCity)
            {
                placesToWork++;
                UpdateCityLaborUIs();
                //resourceManager.UpdateUI(selectedImprovement.upgradeCost);
                uiResourceManager.SetCityCurrentStorage(selectedCity.ResourceManager.ResourceStorageLevel);
                ImprovementTileHighlight(true);
            }

            if (!enemy)
                return true;
        }
		//can't remove barracks if army holding army
		else if (!upgradingImprovement && improvementData.singleBuildType != SingleBuildType.None && selectedImprovement.unitsWithinCount > 0)
        {
			if (!enemy)
                InfoPopUpHandler.WarningMessage().Create(improvementLoc, "Currently stationing units");

			return false;
		}
		
        if (!upgradingImprovement) //not redundant, for when actually removing
        {
            //putting the labor back
            //foreach (ResourceType resourceType in resourceProducer.producedResources)
            //{
            ResourceType resourceType = resourceProducer.producedResource.resourceType;

            for (int i = 0; i < world.GetCurrentLaborForTile(improvementLoc); i++)
            {
				selectedImprovement.city.ChangeResourcesWorked(resourceType, -1);

                int totalResourceLabor = selectedImprovement.city.GetResourcesWorkedResourceCount(resourceType);
                if (updateCity)
                    uiLaborHandler.PlusMinusOneLabor(resourceType, totalResourceLabor, -1, selectedImprovement.city.ResourceManager.GetResourceGenerationValues(resourceType));
                if (totalResourceLabor == 0)
					selectedImprovement.city.RemoveFromResourcesWorked(resourceType);
            }
            //}

            resourceProducer.UpdateCurrentLaborData(0);
            //resourceProducer.UpdateResourceGenerationData();
            resourceProducer.StopProducing();

            //replacing the cost
            if (!destroyingCity && !enemy)
                ReplaceImprovementCost(improvementData.improvementCost, improvementLoc);

            if (updateCity)
            {
                //resourceManager.UpdateUI(improvementData.improvementCost);
                uiResourceManager.SetCityCurrentStorage(selectedCity.ResourceManager.ResourceStorageLevel);
            }

            if (improvementData.replaceTerrain)
            {
                td.ToggleTerrainMesh(true);
            }
            else if (td.terrainData.specificTerrain == SpecificTerrain.FloodPlain)
            {
                td.FloodPlainCheck(false);
			}

			if (td.rawResourceType == RawResourceType.Rocks)
            {
                if (td.resourceAmount > 0)
                {
                    td.prop.gameObject.SetActive(true);
                    td.ShowProp(true);
					td.RocksCheck();
				}
                else
                {
                    //TerrainDataSO tempData;
                
                    //if (td.terrainData.grassland)
                    //    tempData = td.isHill ? world.grasslandHillTerrain : world.grasslandTerrain;
                    //else
                    //    tempData = td.isHill ? world.desertHillTerrain : world.desertTerrain;

                    td.ShowProp(false);
                    //Destroy(td.resourceGraphic.gameObject);
                    world.SetNewTerrainData(td);
                    //td.SetNewData(tempData);
                    //GameLoader.Instance.gameData.allTerrain[improvementLoc] = td.SaveData();
			    }
            }
            else
            {
				td.ShowProp(true);
				if (td.terrainData.type == TerrainType.Forest || td.terrainData.type == TerrainType.ForestHill)
					td.SwitchFromRoad();
			}
            
            //if (improvementData.singleBuildType != SingleBuildType.None)
            //{
            //    city.singleBuildDict.Remove(improvementData.singleBuildType);
            //}
            //if (improvementData.singleBuild)
            //{
            //}

            if (!selectedImprovement.isConstruction)
            {
                if (updateCity)
                {
                    if (!world.CheckIfTileIsMaxxed(improvementLoc))
                        placesToWork--;
                }

                if (selectedImprovement.city != null)
                {
                    int currentLabor = world.GetCurrentLaborForTile(improvementLoc);
                    selectedImprovement.city.unusedLabor += currentLabor;
                    selectedImprovement.city.usedLabor -= currentLabor;
                }
            }
        }

        if (!upgradingImprovement && !selectedImprovement.isConstruction)
		    selectedImprovement.PlayRemoveEffect(td.isHill);

		foreach (Vector3Int loc in improvementData.noWalkAreas)
            world.RemoveFromNoWalkList(loc + improvementLoc);

        //updating city graphic
        if (!selectedImprovement.isConstruction)
        {
            if (selectedImprovement.meshCity != null)
            {
                selectedImprovement.meshCity.RemoveFromImprovementList(selectedImprovement);
                selectedImprovement.meshCity.RemoveFromMeshFilterList(false, improvementLoc);
                CombineMeshes(selectedImprovement.meshCity, selectedImprovement.meshCity.subTransform, this.upgradingImprovement);
            }
            else
            {
                (MeshFilter[] meshes, GameObject go) = improvementMeshDict[improvementLoc];
                improvementMeshDict.Remove(improvementLoc);

                int count = meshes.Length;
                for (int i = 0; i < count; i++)
                    improvementMeshList.Remove(meshes[i]);

                Destroy(go);
                CombineMeshes();
            }
        }

		//changing city stats
        if (selectedImprovement.city != null)
        {
			selectedImprovement.city.HousingCount -= improvementData.housingIncrease;

            if (improvementData.workEthicChange != 0)
            {
			    selectedImprovement.city.workEthic -= improvementData.workEthicChange;
			    selectedImprovement.city.improvementWorkEthic -= improvementData.workEthicChange;
				UpdateCityWorkEthic();
			}
		    
            if (improvementData.waterIncrease > 0)
		    {
			    selectedImprovement.city.waterCount -= improvementData.waterIncrease;
                if (updateCity)
    			    uiInfoPanelCity.UpdateWater(selectedCity.waterCount);

			    if (selectedImprovement.city.waterCount <= 0)
					selectedImprovement.city.reachedWaterLimit = true;
			    else
					selectedImprovement.city.reachedWaterLimit = false;
		    }
        }


		GameObject improvement = world.GetStructure(improvementLoc);
        Destroy(improvement);

        //updating world dicts
        if (!upgradingImprovement)
        {
            RemoveLaborFromDicts(improvementLoc);
            world.RemoveFromMaxWorked(improvementLoc);
        }
        world.RemoveStructure(improvementLoc);
        //world.RemoveStructureMap(improvementLoc);
        developedTiles.Remove(improvementLoc);

        if (upgradingImprovement) //stop here if upgrading
            return true; 

        if (updateCity && removingImprovement && uiImprovementBuildInfoPanel.activeStatus)
            ImprovementTileHighlight(true);

        //stop here if it in construction process
        if (selectedImprovement.isConstruction)
        {
            constructingTiles.Remove(improvementLoc);
            return true;
        }

        if (selectedImprovement.city != null)
        {
            if (selectedImprovement.city.singleBuildDict.ContainsKey(SingleBuildType.Harbor) && improvementLoc == selectedImprovement.city.singleBuildDict[SingleBuildType.Harbor])
            {
                //city.hasHarbor = false;
                //world.RemoveHarbor(improvementLoc);
                world.RemoveTradeLoc(improvementLoc);
            }
            else if (selectedImprovement.city.singleBuildDict.ContainsKey(SingleBuildType.Barracks) && improvementLoc == selectedImprovement.city.singleBuildDict[SingleBuildType.Barracks])
            {
			    world.militaryStationLocs.Remove(improvementLoc);
				//city.hasBarracks = false;
				selectedImprovement.city.army.ClearArmySpots();
            }

            if (selectedImprovement.city.AutoAssignLabor && selectedImprovement.city.unusedLabor > 0)
				selectedImprovement.city.AutoAssignmentsForLabor();

            if (improvementData.singleBuildType != SingleBuildType.None)
            {
				selectedImprovement.city.singleBuildList.Remove(improvementData.singleBuildType);
				selectedImprovement.city.singleBuildDict.Remove(improvementData.singleBuildType);
                world.RemoveSingleBuildFromCityLabor(improvementLoc);
            }
        }

        //updating ui
        if (updateCity)
        {
            //if (improvementData.workEthicChange != 0)
            //    UpdateCityWorkEthic();

            if (!upgradingImprovement)
            {
                uiInfoPanelCity.SetAllData(selectedCity);
                UpdateLaborNumbers(selectedCity);
                uiLaborAssignment.UpdateUI(selectedCity, placesToWork);
            }
        }

        return true;
    }

	private void ReplaceImprovementCost(List<ResourceValue> replaceCost, Vector3 improvementLoc)
    {
        int i = 0;
        improvementLoc.y += replaceCost.Count * 0.4f;

        resourceManager.resourceCount = 0;
        foreach (ResourceValue resourceValue in replaceCost) //adding back 100% of cost (if there's room)
        {
            int resourcesReturned = resourceManager.AddResource(resourceValue.resourceType, resourceValue.resourceAmount);
            Vector3 loc = improvementLoc;
            loc.y += -.4f * i;
            i++;
            if (resourcesReturned == 0)
            {
                InfoResourcePopUpHandler.CreateResourceStat(loc, resourceValue.resourceAmount, ResourceHolder.Instance.GetIcon(resourceValue.resourceType), true);
                continue;
            }
            InfoResourcePopUpHandler.CreateResourceStat(loc, resourcesReturned, ResourceHolder.Instance.GetIcon(resourceValue.resourceType));
        }
    }

    public void CloseImprovementTooltipButton()
    {
        world.CloseImprovementTooltipButton();
        world.CloseCampTooltipButton();
    }

    public void CloseImprovementBuildPanelButton()
    {
        PlayCloseAudio();
        CloseImprovementBuildPanel();
    }

    public void CloseImprovementBuildPanel()
    {
        if (uiImprovementBuildInfoPanel.activeStatus)
        {
            if (selectedWonder != null)
            {
                //CameraDefaultRotation();
                foreach (Vector3Int tile in tilesToChange)
                {
                    world.GetTerrainDataAt(tile).DisableHighlight();
                }
                tilesToChange.Clear();
                uiImprovementBuildInfoPanel.ToggleVisibility(false);
                placingWonderHarbor = false;
                return;
            }
            
            //if (uiImprovementBuildInfoPanel.activeStatus && !removingImprovement && !upgradingImprovement)
            //    CameraDefaultRotation();
            if (removingImprovement || upgradingImprovement)
                uiCityTabs.CloseSelectedTab();
            //removingImprovement = false;
            //upgradingImprovement = false;
            ResetTileLists();
            world.unitMovement.ToggleUnitHighlights(false);
            ToggleBuildingHighlight(true, selectedCity.cityLoc);
            uiImprovementBuildInfoPanel.ToggleVisibility(false);
            uiCityUpgradePanel.ToggleVisibility(false);
        }
    }

    public void CancelUpgrade()
    {
        upgradingImprovement = false;
    }




    public void LaborManager(int laborChange) //for assigning labor
    {
        if (laborChange == 0)
        {
            ResetTileLists();
            uiLaborAssignment.HideUI();
            return;
        }

        //uiQueueManager.ToggleVisibility(false);
        CloseQueueUI();
		world.uiCityPopIncreasePanel.ToggleVisibility(false);
		uiCityTabs.HideSelectedTab(false);
        CloseSingleWindows();
        //uiLaborHandler.HideUI();
        //uiLaborHandler.ShowUI(selectedCity);
        //ResetTileLists();
        world.CloseImprovementTooltipButton();
        world.CloseCampTooltipButton();
        CloseImprovementBuildPanel();

        //removingBuilding = false;
        improvementData = null;
        
        if (laborChange != uiLaborAssignment.laborChangeFlag) //turn on menu only when selecting different button
        {
            uiLaborAssignment.ResetLaborAssignment(-laborChange);
            uiLaborAssignment.laborChangeFlag = laborChange;
            this.laborChange = laborChange;

            //if (world.GetBuildingListForCity(selectedCityLoc).Count > 0) //only shows if city has buildings
            uiLaborHandler.ToggleVisibility(true);

            //BuildingButtonHighlight();
            LaborTileHighlight();
            //UpdateLaborNumbers(selectedCity);
        }
        else
        {
            uiLaborHandler.HideUI();
            uiLaborAssignment.ResetLaborAssignment(laborChange);
        }
    }

    //for labor handler in buildings (currently disabled)
    //public void PassLaborChange(string buildingName) //for changing labor counts in city labor
    //{
    //    //if (removingBuilding)
    //    //{
    //    //    RemoveBuilding(buildingName);
    //    //    return;
    //    //}

    //    int labor = uiLaborHandler.GetCurrentLabor;
    //    int maxLabor = uiLaborHandler.GetMaxLabor;

    //    if (laborChange > 0 && labor == maxLabor) //checks in case button interactables don't work
    //        return;
    //    if (laborChange < 0 && labor == 0)
    //        return;

    //    labor = ChangePlacesToWorkCount(labor, maxLabor);
    //    selectedCity.cityPop.GetSetCityLaborers += laborChange;
    //    ChangeCityLaborInfo();

    //    //if (world.CheckBuildingIsProducer(selectedCityLoc, buildingName))
    //    //{
    //    //    world.GetBuildingProducer(selectedCityLoc, buildingName).UpdateCurrentLaborData(labor);
    //    //}

    //    WorkEthicHandler workEthicHandler = world.GetBuilding(selectedCityLoc, buildingName).GetComponent<WorkEthicHandler>();

    //    float workEthicChange = 0f;
    //    if (workEthicHandler != null)
    //        workEthicChange = workEthicHandler.GetWorkEthicChange(labor);
        
    //    UpdateCityWorkEthic(workEthicChange);

    //    if (labor == 0) //removing from world dicts when zeroed out
    //    {
    //        RemoveLaborFromBuildingDicts(buildingName);
    //    }

    //    if (labor != 0)
    //    {
    //        world.AddToCurrentBuildingLabor(selectedCityLoc, buildingName, labor);
    //    }

    //    selectedCity.UpdateCityPopInfo();

    //    uiInfoPanelCity.SetData(selectedCity.CityName, selectedCity.cityPop.CurrentPop, selectedCity.HousingCount, selectedCity.cityPop.UnusedLabor, selectedCity.GetSetWorkEthic, 
    //        resourceManager.FoodGrowthLevel, resourceManager.FoodGrowthLimit, resourceManager.FoodPerMinute);

    //    //resourceManager.UpdateUIGenerationAll();
    //    BuildingButtonHighlight();
    //    LaborTileHighlight();
    //    uiLaborAssignment.UpdateUI(selectedCity.cityPop, selectedCity.PlacesToWork);
    //    uiLaborHandler.ShowUI(laborChange, selectedCity, world, selectedCity.PlacesToWork);
    //}

    private void UpdateCityWorkEthic()
    {
        foreach (Vector3Int tile in developedTiles)
        {
            if (world.CheckImprovementIsProducer(tile))
                world.GetResourceProducer(tile).CalculateResourceGenerationPerMinute();
        }
    }

    private void CheckForWork() //only used for enabling the "add labor" button
    {
        placesToWork = 0;

        foreach (Vector3Int tile in developedTiles)
        {
            if (/*world.CheckIfTileIsImproved(tile) && */!world.CheckIfTileIsMaxxed(tile) /*&& world.CheckIfCityOwnsTile(tile, selectedCity.gameObject)*/)
                placesToWork++;
        }
    }

    //private int ChangePlacesToWorkCount(int labor, int maxLabor)
    //{
    //    if (labor == maxLabor) //if decreasing from max amount
    //        selectedCity.PlacesToWork++;
    //    labor += laborChange;
    //    if (labor == maxLabor) //if increasing to max amount
    //        selectedCity.PlacesToWork--;
    //    return labor;
    //}

    //private void ChangeCityLaborInfo()
    //{
    //    selectedCity.cityPop.UnusedLabor -= laborChange;
    //    selectedCity.cityPop.UsedLabor += laborChange;
    //}

    //private void BuildingButtonHighlight()
    //{
    //    foreach (UILaborHandlerOptions option in uiLaborHandler.GetLaborOptions)
    //    {
    //        if (world.GetBuildingListForCity(selectedCityLoc).Contains(option.GetBuildingName))
    //        {
    //            option.DisableHighlight();
    //            if (laborChange > 0 && !option.CheckLaborIsMaxxed() && selectedCity.cityPop.UnusedLabor > 0)
    //            {
    //                option.EnableHighlight(Color.green); //neon green
    //            }

    //            if (laborChange < 0 && option.GetCurrentLabor > 0)
    //            {
    //                option.EnableHighlight(Color.red); //neon red
    //            }
    //        }
    //    }
    //}

    public void UpdateLaborNumbers(City city)
    {
        if (city.activeCity)
        {
            HideLaborNumbers();
            HideImprovementResources();
            foreach (Vector3Int tile in developedTiles)
            {
                PrepareLaborNumber(tile);
                PrepareImprovementResource(tile);
            }
        }
    }

    private void LaborTileHighlight()
    {
        tilesToChange.Clear();

        foreach (Vector3Int tile in developedTiles)
        {
            TerrainData td = world.GetTerrainDataAt(tile);
            td.DisableHighlight(); //turning off all highlights first

            CityImprovement improvement = world.GetCityDevelopment(tile);
            improvement.DisableHighlight();
            if (improvement.isUpgrading || improvement.GetImprovementData.singleBuildType != SingleBuildType.None)
                continue;

            if (laborChange > 0 && !world.CheckIfTileIsMaxxed(tile) && selectedCity.unusedLabor > 0) //for increasing labor, can't be maxxed out
            {
                td.EnableHighlight(Color.green);
                improvement.EnableHighlight(Color.green);
                tilesToChange.Add(tile);   
            }
            else if (laborChange < 0 && world.CheckIfTileIsWorked(tile)) //for decreasing labor, can't be at 0. 
            {
                td.EnableHighlight(Color.red);
                improvement.EnableHighlight(Color.red);
                tilesToChange.Add(tile);
            }
        }
    }

    private void PrepareLaborNumber(Vector3Int tile) //grabbing labor numbers and prefab from pool
    {
        if (world.GetMaxLaborForTile(tile) == 0) //don't show numbers for those that don't take labor
            return;

        //specifying location on tile
        Vector3 numberPosition = tile;
        //numberPosition.y += .01f;
        numberPosition.z += 1f; //upper center of tile

        //Object pooling set up
        CityLaborTileNumber tempObject = GetFromLaborNumbersPool();
        tempObject.transform.position = numberPosition;
        laborNumberList.Add(tempObject);

        tempObject.SetLaborNumber(world.PrepareLaborNumbers(tile));
    }

    private void PrepareImprovementResource(Vector3Int tile)
    {
        if (world.GetMaxLaborForTile(tile) == 0) //don't show numbers for those that don't take labor
            return;

        //specifying location on tile
        Vector3 position = tile;
        //position.z += .3f;

        ImprovementResource resource = GetFromImprovementResourcePool();
        resource.transform.position = position;
        improvementResourceList.Add(resource);

        resource.SetImage(ResourceHolder.Instance.GetIcon(world.GetCityDevelopment(tile).producedResource));
    }

    private void ChangeLaborCount(Vector3Int terrainLocation)
    {
        ResourceProducer resourceProducer = world.GetResourceProducer(terrainLocation); //cached all resource producers in dict
        
        int labor = world.GetCurrentLaborForTile(terrainLocation);
        int maxLabor = world.GetMaxLaborForTile(terrainLocation);

        if (laborChange > 0 && labor == maxLabor) //checks in case button interactables don't work
            return;
        if (laborChange < 0 && labor == 0)
            return;

        if (labor == maxLabor) //if decreasing from max amount
            placesToWork++;
        labor += laborChange;
        if (labor == maxLabor) //if increasing to max amount
            placesToWork--;

        //selectedCity.cityPop.GetSetFieldLaborers += laborChange;
        selectedCity.unusedLabor -= laborChange;
        selectedCity.usedLabor += laborChange;

        resourceProducer.UpdateCurrentLaborData(labor);
        //if (!resourceProducer.CheckResourceManager(resourceManager))
        //    resourceProducer.SetResourceManager(resourceManager);
        PlayLaborAudio(laborChange > 0);

        if (labor == 0) //removing from world dicts when zeroed out
        {
            RemoveLaborFromDicts(terrainLocation);
            resourceProducer.StopProducing();
        }
        else if (laborChange > 0)
        {
            if (labor == 1) //assigning city to location if working for first time
			{
				CityImprovement selectedImprovement = world.GetCityDevelopment(terrainLocation);
				//if mesh isn't owned by anyone, add to this city's
				if (selectedImprovement.meshCity == null)
				{
					selectedImprovement.meshCity = selectedCity;
					selectedImprovement.transform.parent = selectedCity.transform;
					selectedCity.SetNewMeshCity(terrainLocation, improvementMeshDict, improvementMeshList);
					CombineMeshes(selectedCity, selectedCity.subTransform, this.upgradingImprovement);
					selectedCity.AddToImprovementList(selectedImprovement);
				}

				if (world.GetCityDevelopment(terrainLocation).queued)
				{
					//CityImprovement selectedImprovement = world.GetCityDevelopment(terrainLocation);
					City tempCity = selectedImprovement.GetQueueCity();

					if (tempCity != selectedCity)
					{
						tempCity.RemoveFromQueue(terrainLocation - tempCity.cityLoc);
						selectedImprovement.SetQueueCity(null);
					}
				}

                selectedImprovement.city = selectedCity;
				world.AddToCityLabor(terrainLocation, selectedCity.cityLoc);
				resourceProducer.SetResourceManager(selectedCity.ResourceManager);
                resourceProducer.UpdateResourceGenerationData();
				resourceProducer.StartProducing();
			}
            else
            {
				resourceProducer.AddLaborMidProduction();
			}
        }
        else if (labor > 0 && laborChange < 0)
        {
            resourceProducer.RemoveLaborMidProduction();
            if (resourceProducer.isWaitingforResources)
                selectedCity.ResourceManager.CheckProducerResourceWaitList();
        }

        if (labor != 0)
        {
            world.AddToCurrentFieldLabor(terrainLocation, labor);
        }

        //updating resource generation uis
        //resourceProducer.UpdateResourceGenerationData();
        //foreach (ResourceType resourceType in resourceProducer.producedResources)
        //{
        ResourceType resourceType = resourceProducer.producedResource.resourceType;

        selectedCity.ChangeResourcesWorked(resourceType, laborChange);

        int totalResourceLabor = selectedCity.GetResourcesWorkedResourceCount(resourceType);

        //updating all the labor info
        UpdateLaborNumbers(selectedCity);
        uiLaborHandler.PlusMinusOneLabor(resourceType, totalResourceLabor, laborChange, selectedCity.ResourceManager.GetResourceGenerationValues(resourceType));
        uiLaborHandler.UpdateResourcesConsumed(resourceProducer.consumedResourceTypes, selectedCity.ResourceManager.resourceConsumedPerMinuteDict);

        if (totalResourceLabor == 0)
            selectedCity.RemoveFromResourcesWorked(resourceType);
        //}


        uiInfoPanelCity.SetAllData(selectedCity);

        //resourceManager.UpdateUIGeneration(terrainSelected.GetTerrainData().resourceType);
        //BuildingButtonHighlight();
        LaborTileHighlight();
        uiLaborAssignment.UpdateUI(selectedCity, placesToWork);

        world.TutorialCheck("Change Labor");
        //if (world.GetBuildingListForCity(selectedCityLoc).Count > 0)
        //uiLaborHandler.ShowUI(laborChange, selectedCity, world, placesToWork);
    }

    private void RemoveLaborFromDicts(Vector3Int terrainLocation)
    {
        world.RemoveFromCurrentWorked(terrainLocation);
        world.RemoveFromCityLabor(terrainLocation);
        //resourceManager.RemoveKeyFromGenerationDict(terrainLocation);
    }

    //private void RemoveLaborFromBuildingDicts(string buildingName)
    //{
    //    world.RemoveFromBuildingCurrentWorked(selectedCityLoc, buildingName);
    //    //resourceManager.RemoveKeyFromBuildingGenerationDict(buildingName);
    //}

    public void UpdateCityLaborUIs()
    {
        UpdateLaborNumbers(selectedCity);
        uiInfoPanelCity.SetAllData(selectedCity);
        uiLaborAssignment.UpdateUI(selectedCity, placesToWork);
    }

    public void CloseCityTab()
    {
        uiCityTabs.HideSelectedTab(false);
        //PlayCloseAudio();
    }

    //public void OpenResourceGrid()
    //{
    //    if (uiCityResourceGrid.activeStatus)
    //        uiCityResourceGrid.ToggleVisibility(false);
    //    else
    //        uiCityResourceGrid.ToggleVisibility(true, selectedCity);
    //}

    //public void CloseResourceGrid()
    //{
    //    uiCityResourceGrid.ToggleVisibility(false);
    //}

    public void ResetCityUIToBase()
    {
        world.CloseImprovementTooltipButton();
        world.CloseCampTooltipButton();
        CloseImprovementBuildPanel();
        uiLaborAssignment.ResetLaborAssignment();
		world.uiCityPopIncreasePanel.ToggleVisibility(false);
		CloseQueueUI();
        uiCityTabs.HideSelectedTab(false);
        CloseLaborMenus();
        CloseSingleWindows();

		if (world.tutorialGoing)
		{
			if (uiHelperWindow != null && uiHelperWindow.activeStatus)
				uiHelperWindow.ToggleVisibility(false);
		}
		//uiLaborHandler.HideUI();
		//ResetTileLists(); //already in improvement build panel
	}

    public void ToggleLaborHandlerMenu()
    {
        PlaySelectAudio();
        
        if (uiLaborHandler.activeStatus)
        {
            CloseLaborMenus();
        }
        else
        {
            CloseCityTab();
            world.CloseImprovementTooltipButton();
            world.CloseCampTooltipButton();
            CloseImprovementBuildPanel();
			world.uiCityPopIncreasePanel.ToggleVisibility(false);
			CloseQueueUI();
            ResetTileLists();
            CloseSingleWindows();
            uiLaborHandler.ToggleVisibility(true);
        }
    }

    public void CloseLaborMenusButton()
    {
        PlayCloseAudio();
        CloseLaborMenus();
    }

    public void CloseLaborMenus()
    {
        uiLaborHandler.HideUI();

        if (laborChange != 0)
        {
            uiLaborAssignment.ResetLaborAssignment();

            if (!uiImprovementBuildInfoPanel.activeStatus)
            {
                ResetTileLists();
                ToggleBuildingHighlight(true, selectedCity.cityLoc);
            }
        }

        uiLaborPrioritizationManager.ToggleVisibility(false);
    }

    public void DestroyCityWarning()
    {
        if (selectedCity != null)
        {
			if (selectedCity.attacked)
            {
                UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Enemy approaching", true);
			    return;
            }
            else if (!selectedCity.army.atHome)
            {
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Army is deployed", true);
				return;
			}
        }
        
        PlaySelectAudio();
        
        if (uiDestroyCityWarning.activeStatus)
        {
            uiDestroyCityWarning.ToggleVisibility(false);
        }
        else
        {
            ResetCityUIToBase();
            uiDestroyCityWarning.ToggleVisibility(true);
        }
    }






    public void ToggleAutoAssign()
    {
        PlayCheckAudio();
        
        if (autoAssign.isOn)
        {
            //CloseLaborMenus();
            //openAssignmentPriorityMenu.interactable = true;
            //selectedCity.autoGrow = true;
            selectedCity.AutoAssignLabor = true;

            if (selectedCity.unusedLabor > 0)
            {
                selectedCity.AutoAssignmentsForLabor();
                UpdateCityLaborUIs();
            }

            uiLaborAssignment.SetAssignmentOptionsInteractableOff();
            uiLaborAssignment.showPrioritiesButton.SetActive(true);
        }
        else
        {
            //selectedCity.autoGrow = false;
            selectedCity.AutoAssignLabor = false;
            uiLaborAssignment.UpdateUI(selectedCity, placesToWork);
            uiLaborPrioritizationManager.ToggleVisibility(false);
			uiLaborAssignment.showPrioritiesButton.SetActive(false);
		}
    }

    public void ClosePrioritizationManager()
    {
        //openAssignmentPriorityMenu.interactable = false;
        //selectedCity.AutoAssignLabor = false;
        PlayCloseAudio();
        uiLaborPrioritizationManager.ToggleVisibility(false);
    }

    public void TogglePrioritizationMenu()
    {
        PlaySelectAudio();
        
        if (!uiLaborPrioritizationManager.activeStatus)
        {
            CloseLaborMenus();
            world.CloseImprovementTooltipButton();
            world.CloseCampTooltipButton();
            CloseImprovementBuildPanel();
            uiCityTabs.HideSelectedTab(false);
			world.uiCityPopIncreasePanel.ToggleVisibility(false);
			CloseQueueUI();
            CloseSingleWindows();
            uiLaborPrioritizationManager.ToggleVisibility(true);
            uiLaborPrioritizationManager.PrepareLaborPrioritizationMenu(selectedCity);
            uiLaborPrioritizationManager.LoadLaborPrioritizationInfo();
            //prioritizationMenuActive = true;
        }
        else
        {
            uiLaborPrioritizationManager.ToggleVisibility(false);
        }
    }





    public void ToggleQueue()
    {
        PlaySelectAudio();
        
        if (!isQueueing)
            BeginBuildQueue();
        else
            EndBuildQueue();
    }

    private void BeginBuildQueue()
    {
        SetQueueStatus(true);
        uiQueueManager.ToggleButtonSelection(true);
    }

    private void EndBuildQueue()
    {
        SetQueueStatus(false);
        uiQueueManager.ToggleButtonSelection(false);
    }

    private void SetQueueStatus(bool v)
    {
        if (isQueueing && !v)
            CloseImprovementBuildPanel();

        isQueueing = v;
        //uiUnitBuilder.isQueueing = v;
        uiRawGoodsBuilder.isQueueing = v;
        uiProducerBuilder.isQueueing = v;
        uiBuildingBuilder.isQueueing = v;
    }

    public void BuildQueuedBuilding(City city, ResourceManager resourceManager)
    {
        QueueItem item = city.GetBuildInfo();
        this.resourceManager = resourceManager;
        bool building = item.queueLoc == Vector3Int.zero;
        //selectedCity = city;
        //selectedCityLoc = city.cityLoc;
        
        if (item.upgrade)
        {
            Vector3Int tile = item.queueLoc + city.cityLoc;
            if (building)
                UpgradeSelectedImprovementPrep(city.cityLoc, world.GetBuildingData(tile, item.queueName), city, uiQueueManager.upgradeCosts, uiQueueManager.refundCosts);
            else
                UpgradeSelectedImprovementPrep(tile, world.GetCityDevelopment(tile), city, uiQueueManager.upgradeCosts, uiQueueManager.refundCosts);
        }
		else if (building) //build building
        {
            CreateBuilding(UpgradeableObjectHolder.Instance.improvementDict[item.queueName], city, false);
        }
        else //build improvement
        {
			ImprovementDataSO improvementData = UpgradeableObjectHolder.Instance.improvementDict[item.queueName];
			Vector3Int tile = item.queueLoc + city.cityLoc;

            if (world.IsBuildLocationTaken(tile) || world.IsRoadOnTerrain(tile) || improvementData.rawResourceType != world.GetTerrainDataAt(tile).rawResourceType)
            {
                city.GoToNextItemInQueue();
                uiQueueManager.CheckIfBuiltItemIsQueued(tile, item.queueLoc, false, improvementData, city);
                    //RemoveQueueGhostImprovement(tile, city);
                return;
            }
            BuildImprovement(improvementData, tile, city, false);
        }

        city.GoToNextItemInQueue();
    }

    public List<QueueItem> GetQueueItems()
    {
        return selectedCity.queueItemList;
    }

    public void CloseQueueUIButton()
    {
        PlayCloseAudio();
        CloseQueueUI();
    }

    public void CloseQueueUI()
    {
        if (uiQueueManager.activeStatus)
        {
            SetQueueStatus(false);
            uiQueueManager.UnselectQueueItem();
            //SetCityQueueItems();
            uiQueueManager.ToggleVisibility(false);
        }
    }

    //public void SetCityQueueItems()
    //{
    //    if (uiQueueManager.activeStatus)
    //    {
    //        (selectedCity.savedQueueItems, selectedCity.savedQueueItemsNames) = uiQueueManager.SetQueueItems();
    //    }
    //}






    public void RunCityNamerUI()
    {
        if (selectedCity != null && selectedCity.attacked)
        {
			UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Enemy approaching", true);
            return;
		}
        
        PlaySelectAudio();
        
        if (uiCityNamer.activeStatus)
        {
            uiCityNamer.ToggleVisibility(false);
            uiTraderNamer.ToggleVisibility(false);
        }
        else
        {
            if (selectedCity != null)
            {
                ResetCityUIToBase();
                ResetTileLists();
                uiCityNamer.ToggleVisibility(true, selectedCity);
            }
            else if (world.unitMovement.selectedUnit && world.unitMovement.selectedUnit.isTrader)
            {
				uiTraderNamer.ToggleVisibility(true, null, world.unitMovement.selectedUnit.trader);
            }
        }
    }

    public void CloseSingleWindows()
    {
        uiDestroyCityWarning.ToggleVisibility(false);
        uiCityNamer.ToggleVisibility(false);
        uiLaborDestinationWindow.CloseWindowButton();
        focusCam.paused = false;
    }

    public void DestroyCityConfirm()
    {
        PlaySelectAudio();
        
        if (selectedWonder != null)
        {
			CancelWonderConstruction();
			return;
		}
        else
        {
            DestroyCity(selectedCity);
        }
    }

    public void DestroyCity(City city) //set on destroy city warning message
    {
        ResetCityUI();

        //stop upgrading and training improvements
        foreach (Vector3Int tile in world.GetNeighborsFor(city.cityLoc, MapWorld.State.CITYRADIUS))
        {
            if (world.TileHasCityImprovement(tile))
            {
                CityImprovement improvement = world.GetCityDevelopment(tile);

                if (improvement.city == city)
                {
                    improvement.city = null;

                    if (improvement.isUpgrading || improvement.isTraining || improvement.isConstruction)
                        RemoveImprovement(tile, improvement, true, true);
				}
			}
        }

        //disassociating improvements from city mesh
        foreach (CityImprovement improvement in city.ImprovementList)
        {
            if (improvement.city == city)
            improvement.meshCity = null;
            improvement.transform.parent = improvementHolder.transform;
        }

		city.DestroyFire();
		city.ReassignMeshes(improvementHolder, improvementMeshDict, improvementMeshList);
        CombineMeshes();

        TerrainData td = world.GetTerrainDataAt(city.cityLoc);
        if (td.rawResourceType == RawResourceType.Rocks && td.resourceAmount == 0)
            td.ShowProp(false);
        else
			td.ShowProp(true);

		td.FloodPlainCheck(false);

        //destroying queued objects
        //foreach (UIQueueItem queueItem in selectedCity.savedQueueItems)
        //{
        //    if (queueItem.buildLoc.x == 0 && queueItem.buildLoc.z == 0)
        //        RemoveQueueGhostBuilding(queueItem.buildingName, selectedCity);
        //    else
        //        RemoveQueueGhostImprovement(queueItem.buildLoc + selectedCityLoc, selectedCity);

        //    world.RemoveLocationFromQueueList(queueItem.buildLoc + selectedCityLoc);
        //}

        GameObject destroyedCity = world.GetStructure(city.cityLoc);

        //destroy all construction projects upon destroying city
        //List<Vector3Int> constructionToStop = new(constructingTiles);

        //foreach (Vector3Int constructionTile in constructionToStop)
        //{
        //    CityImprovement construction = world.GetCityDevelopmentConstruction(constructionTile);
        //    construction.RemoveConstruction(this, constructionTile);
        //    RemoveImprovement(constructionTile, construction, city, false, true);
        //}

        world.RemoveCityNameMap(city.cityLoc);
        world.RemoveStructure(city.cityLoc);
        //world.RemoveStructureMap(selectedCityLoc);
        //world.ResetTileMap(selectedCityLoc);
        world.RemoveCityName(city.cityLoc);
        world.RemoveTradeLoc(city.cityLoc);

		city.DestroyThisCity();

        //for all single build improvements, finding a nearby city to join that doesn't have one. If not one available, then is unowned. 
        SetSingleBuildsAvailable(city);
        Destroy(destroyedCity);

        uiDestroyCityWarning.ToggleVisibility(false);
    }

    public void SetSingleBuildsAvailable(City city)
    {
		foreach (SingleBuildType singleImprovement in city.singleBuildDict.Keys)
		{
            if (singleImprovement == SingleBuildType.None)
                continue;

            Vector3Int improvementLoc = city.singleBuildDict[singleImprovement];
            CityImprovement improvement = world.GetCityDevelopment(improvementLoc);
			if (improvementLoc == city.cityLoc)
				continue;

			world.RemoveSingleBuildFromCityLabor(improvementLoc);
			bool unclaimed = true;

			foreach (Vector3Int tile in world.GetNeighborsFor(improvementLoc, MapWorld.State.CITYRADIUS))
			{
				if (world.IsCityOnTile(tile))
				{
					City tempCity = world.GetCity(tile);
					if (!tempCity.singleBuildList.Contains(singleImprovement))
					{
                        tempCity.singleBuildList.Add(singleImprovement);
                        tempCity.singleBuildDict[singleImprovement] = improvementLoc;
						world.AddToCityLabor(improvementLoc, tempCity.cityLoc);

						/*if (singleImprovement == "Harbor") //is also done in City object
						{
                            //tempCity.singleBuildDict[SingleBuildType.Harbor] = improvementLoc;
                            //world.RemoveHarbor(improvementLoc);
							//tempCity.hasHarbor = true;
							//tempCity.harborLocation = improvementLoc;
							//world.SetCityHarbor(tempCity, improvementLoc);
						}
						else */if (singleImprovement == SingleBuildType.Barracks)
						{
							//tempCity.hasBarracks = true;
							//tempCity.barracksLocation = improvementLoc;
                            tempCity.army.city = tempCity;
                            improvement.army = tempCity.army;

							foreach (Vector3Int armySpot in world.GetNeighborsFor(improvementLoc, MapWorld.State.EIGHTWAYARMY))
								tempCity.army.SetArmySpots(armySpot);
						}

						unclaimed = false;
						break;
					}
				}
			}

			if (unclaimed)
			{
				if (singleImprovement == SingleBuildType.Harbor)
					world.RemoveTradeLoc(improvementLoc);

                if (singleImprovement == SingleBuildType.Barracks)
                    improvement.army = null;
				//else if (singleImprovement == "Barracks")
				//	city.army.city = null;

				world.AddToUnclaimedSingleBuild(improvementLoc);
			}
		}
	}

    public void AddToOrphanMeshFilterList(GameObject go, MeshFilter[] meshFilter, Vector3Int loc)
    {
		int count = meshFilter.Length;
		for (int i = 0; i < count; i++)
		{
			improvementMeshList.Add(meshFilter[i]);
		}

		improvementMeshDict[loc] = (meshFilter, go);
	}

    public void NoDestroyCity() //in case user chickens out
    {
        PlayCloseAudio();
        
        uiDestroyCityWarning.ToggleVisibility(false);
        if (selectedCity != null)
            uiCityTabs.HideSelectedTab(false);
    }

    public void ResetTileLists()
    {
        //foreach (Vector3 location in resourceInfoHolderDict.Keys)
        //    PoolResourceInfoPanel(location);
        //resourceInfoHolderDict.Clear();
        //resourceInfoPanelDict.Clear();

        foreach (Vector3Int tile in tilesToChange)
        {
            world.GetTerrainDataAt(tile).DisableHighlight();
            if (world.TileHasCityImprovement(tile))
                world.GetCityDevelopment(tile).DisableHighlight();

            //if (world.CheckIfTileIsUnderConstruction(tile))
            //    world.GetCityDevelopmentConstruction(tile).DisableHighlight();
        }
        tilesToChange.Clear();
        improvementData = null;
        laborChange = 0;
        //removingBuilding = false;
        removingImprovement = false;
        upgradingImprovement = false;
    }

    public void ResetCityUI()
    {
        if (selectedCity != null)
        {
            //if (!movementSystem.unitMovement.unitSelected)
            //    world.somethingSelected = false;
            ResourceProducerTimeProgressBarsSetActive(false);
            cityTiles.Clear();
            developedTiles.Clear();
            constructingTiles.Clear();
            //ResetTileLists();
            removingImprovement = false;
            //removingBuilding = false;
            uiLaborPrioritizationManager.ToggleVisibility(false, true);
            ResetCityUIToBase();
            ResetTileLists();
            uiCityTabs.ToggleVisibility(false);
            uiResourceManager.ToggleVisibility(false);
            uiInfoPanelCity.ToggleVisibility(false);
            uiLaborAssignment.HideUI();
            //uiUnitTurn.buttonClicked.RemoveListener(ResetCityUI);
            HideLaborNumbers();
            HideImprovementResources();
            uiLaborHandler.ResetUI();
            HideBorders();
			//CloseResourceGrid();
            //if (!world.cityUnitSelected)
			world.GetTerrainDataAt(selectedCity.cityLoc).DisableHighlight();
            world.unitMovement.ToggleUnitHighlights(false);
            ToggleBuildingHighlight(false, selectedCity.cityLoc);
            //ToggleCityHighlight(false);
            //selectedCity.Deselect();
            selectedCity.HideCityGrowthProgressTimeBar();
            selectedCity.activeCity = false;

            if (selectedCity.gameObject.tag == "Player")
                focusCam.RestoreWorldLimit();
            selectedCity = null;
			world.StopAudio();

            if (world.tutorialGoing)
            {
                if (uiHelperWindow.activeStatus)
                    uiHelperWindow.ToggleVisibility(false);
            }

        }
    }

    public void UnselectWonderButton()
    {
        PlayCloseAudio();
        UnselectWonder();
    }

    public void UnselectWonder()
    {
        if (selectedWonder != null)
        {
            world.somethingSelected = false;
            uiDestroyCityWarning.ToggleVisibility(false);
            selectedWonder.isActive = false;
            selectedWonder.TimeProgressBarSetActive(false);
            uiWonderSelection.ToggleVisibility(false, selectedWonder);
            if (placingWonderHarbor)
                CloseImprovementBuildPanel();
            selectedWonder.DisableHighlight();
            selectedWonder = null;
        }
    }

    public void UnselectTradeCenter()
    {
        if (selectedTradeCenter != null)
        {
            world.somethingSelected = false;
            uiTradeCenter.ToggleVisibility(false);
            selectedTradeCenter.DisableHighlight();
            if (world.GetClosestTerrainLoc(world.mainPlayer.transform.position) == selectedTradeCenter.mainLoc)
                selectedTradeCenter.ToggleClear(true);

            selectedTradeCenter = null;
        }
    }

    public void CombineMeshes(City city, Transform cityTransform, bool upgrade)
    {
        //MeshFilter[] meshFilters = improvementHolder.GetComponentsInChildren<MeshFilter>();
        MeshFilter[] meshFilters = city.CityMeshFilters.ToArray();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        int i = 0;

        while (i < meshFilters.Length)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);

            i++;
        }

        MeshFilter meshFilter = cityTransform.GetComponent<MeshFilter>();
        meshFilter.mesh = new Mesh();
        meshFilter.mesh.CombineMeshes(combine);
        //cityTransform.GetComponent<MeshCollider>().sharedMesh = meshFilter.sharedMesh;

        cityTransform.transform.gameObject.SetActive(true);

        //meshes inexplicably move without this when making the parent a non-pre-existing item in the heirarchy
        cityTransform.localScale = new Vector3(1, 1, 1);
        cityTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        if (selectedCity == city)
        {
            //bool inexplicably changes value when switching to this method, so resetting it here
            //upgradingImprovement = upgrade;
            //ToggleBuildingHighlight(true);
            if (removingImprovement)
                ImprovementTileHighlight(true);
        }
    }

    //for the improvement holder
    public void CombineMeshes()
    {
        MeshFilter[] meshFilters = improvementMeshList.ToArray();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        int i = 0;

        while (i < meshFilters.Length)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);

            i++;
        }

        MeshFilter meshFilter = improvementHolder.GetComponent<MeshFilter>();
        meshFilter.mesh = new Mesh();
        meshFilter.mesh.CombineMeshes(combine);

        improvementHolder.transform.gameObject.SetActive(true);
    }

    #region object pooling
    //Object pooling methods
    private void GrowLaborNumbersPool()
    {
        for (int i = 0; i < 12; i++) //grow pool 12 at a time
        {
            GameObject laborNumber = Instantiate(GameAssets.Instance.laborNumberPrefab);
            laborNumber.gameObject.transform.SetParent(objectPoolHolder, false);
            CityLaborTileNumber cityLaborNumber = laborNumber.GetComponent<CityLaborTileNumber>();
            cityLaborNumber.transform.rotation = Quaternion.Euler(90, 0, 0); //rotating to lie flat on tile
            AddToLaborNumbersPool(cityLaborNumber);
        }
    }

    private void AddToLaborNumbersPool(CityLaborTileNumber laborNumber)
    {
        laborNumber.SetActive(false); //inactivate it when adding to pool
        laborNumberQueue.Enqueue(laborNumber);
    }

    private CityLaborTileNumber GetFromLaborNumbersPool()
    {
        if (laborNumberQueue.Count == 0)
            GrowLaborNumbersPool();

        CityLaborTileNumber laborNumber = laborNumberQueue.Dequeue();
        laborNumber.SetActive(true);
        return laborNumber;
    }

    private void HideLaborNumbers()
    {
        foreach (CityLaborTileNumber number in laborNumberList)
        {
            AddToLaborNumbersPool(number);
        }

        laborNumberList.Clear();
    }



    private void GrowBordersPool()
    {
        for (int i = 0; i < 20; i++) //grow pool 20 at a time
        {
            GameObject border = Instantiate(GameAssets.Instance.cityBorderPrefab);
            border.gameObject.transform.SetParent(objectPoolHolder, false);
            AddToBorderPool(border);
        }
    }

    private void AddToBorderPool(GameObject gameObject)
    {
        gameObject.SetActive(false); //inactivate it when adding to pool
        borderQueue.Enqueue(gameObject);
    }

    private GameObject GetFromBorderPool()
    {
        if (borderQueue.Count == 0)
            GrowBordersPool();

        var border = borderQueue.Dequeue();
        border.SetActive(true);
        return border;
    }

    private void HideBorders()
    {
        Quaternion origRotation = GameAssets.Instance.cityBorderPrefab.transform.rotation;

        foreach (GameObject border in borderList)
        {
            border.transform.rotation = origRotation;
            AddToBorderPool(border);
        }

        borderList.Clear();
    }

    //object pooling the construction graphics
    //private void GrowConstructionTilePool()
    //{
    //    for (int i = 0; i < 2; i++) //grow pool 2 at a time
    //    {
    //        GameObject constructionTileGO = Instantiate(constructionTilePrefab);
    //        constructionTileGO.gameObject.transform.SetParent(objectPoolHolder, false);
    //        CityImprovement constructionImprovement = constructionTileGO.GetComponent<CityImprovement>();
    //        constructionImprovement.SetWorld(world);
    //        constructionImprovement.isConstructionPrefab = true;
    //        AddToConstructionTilePool(constructionImprovement);
    //    }
    //}

    //public void AddToConstructionTilePool(CityImprovement constructionTile)
    //{
    //    constructionTile.gameObject.SetActive(false);
    //    constructionTileQueue.Enqueue(constructionTile);
    //}

    //public CityImprovement GetFromConstructionTilePool()
    //{
    //    if (constructionTileQueue.Count == 0)
    //        GrowConstructionTilePool();

    //    var constructionTile = constructionTileQueue.Dequeue();
    //    constructionTile.gameObject.SetActive(true);
    //    return constructionTile;
    //}

    //object pooling the resource info holders
    //private void GrowResourceInfoHolderPool()
    //{
    //    for (int i = 0; i < 10; i++) //grow pool 20 at a time
    //    {
    //        GameObject resourceInfoHolderGO = Instantiate(GameAssets.Instance.resourceInfoHolder);
    //        resourceInfoHolderGO.gameObject.transform.SetParent(objectPoolHolder, false);
    //        ResourceInfoHolder resourceInfoHolder = resourceInfoHolderGO.GetComponent<ResourceInfoHolder>();
    //        //resourceInfoPanel.transform.rotation = Quaternion.Euler(90, 0, 0); //rotating to lie flat on tile
    //        AddToResourceInfoHolderPool(resourceInfoHolder);
    //    }
    //}

    //private void AddToResourceInfoHolderPool(ResourceInfoHolder resourceInfoHolder)
    //{
    //    resourceInfoHolder.gameObject.SetActive(false);
    //    resourceInfoHolderQueue.Enqueue(resourceInfoHolder);
    //}

    //private ResourceInfoHolder GetFromResourceInfoHolderPool()
    //{
    //    if (resourceInfoHolderQueue.Count == 0)
    //        GrowResourceInfoHolderPool();

    //    var resourceInfoHolder = resourceInfoHolderQueue.Dequeue();
    //    resourceInfoHolder.gameObject.SetActive(true);
    //    return resourceInfoHolder;
    //} 

    //object pooling the resource info panels
    //private void GrowResourceInfoPanelPool()
    //{
    //    for (int i = 0; i < 20; i++) //grow pool 20 at a time
    //    {
    //        GameObject resourceInfoPanelGO = Instantiate(GameAssets.Instance.resourceInfoPanel);
    //        resourceInfoPanelGO.gameObject.transform.SetParent(objectPoolHolder, false);
    //        ResourceInfoPanel resourceInfoPanel = resourceInfoPanelGO.GetComponent<ResourceInfoPanel>();
    //        //resourceInfoPanel.transform.rotation = Quaternion.Euler(90, 0, 0); //rotating to lie flat on tile
    //        AddToResourceInfoPanelPool(resourceInfoPanel);
    //    }
    //}

    //private void AddToResourceInfoPanelPool(ResourceInfoPanel resourceInfoPanel)
    //{
    //    resourceInfoPanel.gameObject.SetActive(false);
    //    resourceInfoPanelQueue.Enqueue(resourceInfoPanel);
    //}

    //private ResourceInfoPanel GetFromResourceInfoPanelPool()
    //{
    //    if (resourceInfoPanelQueue.Count == 0)
    //        GrowResourceInfoPanelPool();

    //    var resourceInfoPanel = resourceInfoPanelQueue.Dequeue();
    //    resourceInfoPanel.gameObject.SetActive(true);
    //    return resourceInfoPanel;
    //}


    private void GrowImprovementResourcePool()
    {
        for (int i = 0; i < 6; i++) //grow pool 6 at a time
        {
            GameObject improvementResource = Instantiate(GameAssets.Instance.improvementResource);
            improvementResource.gameObject.transform.SetParent(objectPoolHolder, false);
            ImprovementResource resource = improvementResource.GetComponent<ImprovementResource>();
            resource.transform.rotation = Quaternion.Euler(90, 0, 0); //rotating to lie flat on tile
            AddToImprovementResourcePool(resource);
        }
    }

    private void AddToImprovementResourcePool(ImprovementResource resource)
    {
        resource.gameObject.SetActive(false); //inactivate it when adding to pool
        improvementResourceQueue.Enqueue(resource);
    }

    private ImprovementResource GetFromImprovementResourcePool()
    {
        if (improvementResourceQueue.Count == 0)
            GrowImprovementResourcePool();

        ImprovementResource resource= improvementResourceQueue.Dequeue();
        resource.gameObject.SetActive(true);
        return resource;
    }

    private void HideImprovementResources()
    {
        foreach (ImprovementResource resource in improvementResourceList)
        {
            AddToImprovementResourcePool(resource);
        }

        improvementResourceList.Clear();
    }
    #endregion
}
