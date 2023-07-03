using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class CityBuilderManager : MonoBehaviour
{
    [SerializeField]
    public UIWonderSelection uiWonderSelection;
    [SerializeField]
    private UICityBuildTabHandler uiCityTabs;
    [SerializeField]
    public UIMarketPlaceManager uiMarketPlaceManager;
    [SerializeField]
    public UIBuilderHandler uiUnitBuilder;
    [SerializeField]
    private UIResourceManager uiResourceManager;
    [SerializeField]
    private UIBuilderHandler uiImprovementBuilder;
    [SerializeField]
    private UIBuilderHandler uiBuildingBuilder;
    [SerializeField]
    private UIQueueManager uiQueueManager;
    [SerializeField]
    private UIInfoPanelCity uiInfoPanelCity;
    [SerializeField]
    private UIUnitTurnHandler uiUnitTurn;
    [SerializeField]
    private UILaborAssignment uiLaborAssignment;
    [SerializeField]
    public UILaborHandler uiLaborHandler;
    [SerializeField]
    private UIImprovementBuildPanel uiImprovementBuildInfoPanel;
    [SerializeField]
    private UICityNamer uiCityNamer;
    [SerializeField]
    public UITradeCenter uiTradeCenter;
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
    private UICityLaborPrioritizationManager uiLaborPrioritizationManager;

    [SerializeField]
    public MapWorld world;
    [SerializeField]
    public MovementSystem movementSystem;
    [SerializeField]
    public Transform objectPoolHolder, friendlyUnitHolder;

    [SerializeField]
    private CameraController focusCam;
    private Quaternion originalRotation;
    private Vector3 originalZoom;

    private City selectedCity;
    public City SelectedCity { get { return selectedCity; } }
    private Vector3Int selectedCityLoc;
    public Vector3Int SelectedCityLoc { get { return selectedCityLoc; } }

    private List<Vector3Int> tilesToChange = new();
    private List<Vector3Int> cityTiles = new();
    private List<Vector3Int> developedTiles = new();
    [HideInInspector]
    public List<Vector3Int> constructingTiles = new();

    private ImprovementDataSO improvementData;

    [HideInInspector]
    public ResourceManager resourceManager;

    private int laborChange;
    //public int LaborChange { set { laborChange = value; } }
    private int placesToWork;

    //wonder management
    private Wonder selectedWonder;
    [SerializeField]
    private GameObject wonderHarbor;

    //trade center management
    private TradeCenter selectedTradeCenter;

    [SerializeField]
    private GameObject upgradeQueueGhost;
    private List<GameObject> queuedGhost = new();
    //private Dictionary<string, GameObject> buildingQueueGhostDict = new();

    //for object pooling of labor numbers
    private Queue<CityLaborTileNumber> laborNumberQueue = new(); //the pool in object pooling
    private List<CityLaborTileNumber> laborNumberList = new(); //to add to in order to add back into pool

    //for object pooling of city borders
    private Queue<GameObject> borderQueue = new();
    private List<GameObject> borderList = new();

    //for object pooling of construction graphics
    [SerializeField]
    private GameObject constructionTilePrefab;
    private Queue<CityImprovement> constructionTileQueue = new();

    //for object pooling of improvement resources
    private Queue<ImprovementResource> improvementResourceQueue = new();
    private List<ImprovementResource> improvementResourceList = new();

    //for making objects transparent
    [SerializeField]
    public Material transparentMat;

    //for object pooling resource info holders
    private Queue<ResourceInfoHolder> resourceInfoHolderQueue = new();
    private Dictionary<Vector3, ResourceInfoHolder> resourceInfoHolderDict = new();

    //for object pooling of resource info panels
    private Queue<ResourceInfoPanel> resourceInfoPanelQueue = new();
    private Dictionary<Vector3, List<ResourceInfoPanel>> resourceInfoPanelDict = new();

    private bool removingImprovement, upgradingImprovement, isQueueing, placingWonderHarbor; //flags thrown when doing specific tasks
    private bool isActive; //when looking at a city
    [HideInInspector]
    public bool buildOptionsActive;
    [HideInInspector]
    public UIBuilderHandler activeBuilderHandler;

    [SerializeField]
    private Transform improvementHolder;
    private Dictionary<Vector3Int, (MeshFilter[], GameObject)> improvementMeshDict = new();
    private List<MeshFilter> improvementMeshList = new();

    [HideInInspector]
    public GameObject emptyGO;

    private void Awake()
    {
        emptyGO = new GameObject("NewImprovement");
        emptyGO.SetActive(false);
        GrowLaborNumbersPool();
        GrowBordersPool();
        GrowConstructionTilePool(); 
        GrowResourceInfoHolderPool();
        GrowResourceInfoPanelPool();
        GrowImprovementResourcePool();
        //openAssignmentPriorityMenu.interactable = false;
    }

    private void PopulateUpgradeDictForTesting()
    {
        //here just for testing
        world.SetUpgradeableObjectMaxLevel("Research", 2);
        world.SetUpgradeableObjectMaxLevel("Research", 3);
        world.SetUpgradeableObjectMaxLevel("Monument", 2);
        world.SetUpgradeableObjectMaxLevel("Housing", 2);
    }

    private void CenterCamOnCity()
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
        if (world.workerOrders || world.buildingWonder)
            return;

        if (selectedObject == null)
            return;

        if (selectedObject.CompareTag("Player") && selectedObject.TryGetComponent(out City cityReference))
        {
            SelectCity(location, cityReference);
        }
        else if (selectedObject.TryGetComponent(out Wonder wonder))
        {
            SelectWonder(wonder);
        }
        else if (selectedObject.TryGetComponent(out TradeCenter center))
        {
            SelectTradeCenter(center);
        }
        //selecting improvements to remove or add/remove labor
        else if (selectedObject.TryGetComponent(out CityImprovement improvementSelected))
        {
            City city = improvementSelected.GetCity();
            if (improvementSelected.building && !removingImprovement && !upgradingImprovement && city != null)
            {
                SelectCity(location, city);
                return;
            }
            
            if (selectedCity != null)
            {
                //Vector3Int terrainLocation = terrainSelected.GetTileCoordinates();
                Vector3Int terrainLocation = world.GetClosestTerrainLoc(location);
                //TerrainData terrainSelected = world.GetTerrainDataAt(terrainLocation);

                //deselecting if choosing improvement outside of city
                if (!cityTiles.Contains(terrainLocation) && terrainLocation != selectedCityLoc)
                {
                    ResetCityUI();
                    return;
                }

                //if not manipulating buildings, exit out
                if (improvementData == null && laborChange == 0 && !removingImprovement && !upgradingImprovement)
                {
                    if (terrainLocation == selectedCityLoc)
                        ResetCityUI();
                    ResetCityUIToBase();
                    world.OpenImprovementTooltip(improvementSelected);
                    return;
                }

                if (upgradingImprovement)
                {
                    if (tilesToChange.Contains(terrainLocation) || terrainLocation == selectedCityLoc)
                        UpgradeSelectedImprovementQueueCheck(terrainLocation, improvementSelected);
                    else
                        UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Not here");
                }
                else if (removingImprovement)
                {
                    if (improvementSelected.initialCityHouse)
                    {
                        ResetCityUI();
                        return;
                    }
                    else if (improvementSelected.isConstruction)
                    {
                        improvementSelected.RemoveConstruction(this, terrainLocation);
                    }

                    if (!improvementSelected.isConstruction && !improvementSelected.isUpgrading)
                        improvementSelected.PlayRemoveEffect(world.GetTerrainDataAt(terrainLocation).terrainData.type == TerrainType.Hill);
                    RemoveImprovement(terrainLocation, improvementSelected, selectedCity, false);
                }
                else if (laborChange != 0) //for changing labor counts in tile
                {
                    if (constructingTiles.Contains(terrainLocation))
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
                else
                {
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
                    world.OpenImprovementTooltip(improvementSelected);
                }
            }
        }
        else if (selectedWonder != null && placingWonderHarbor && selectedObject.TryGetComponent(out TerrainData terrainForHarbor)) //for placing harbor
        {
            Vector3Int terrainLocation = terrainForHarbor.TileCoordinates;

            if (!tilesToChange.Contains(terrainLocation))
            {
                UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't build here");
            }
            else
            {
                BuildWonderHarbor(terrainLocation);
            }
        }
        //selecting terrain to show info
        else if (selectedCity == null && selectedWonder == null && selectedTradeCenter == null && selectedObject.TryGetComponent(out TerrainData td))
        {
            if (world.somethingSelected)
            {
                world.somethingSelected = false;
            }
            else
            {
                world.OpenTerrainTooltip(td);
            }
        }
        //selecting tiles to place improvements
        else if (selectedCity != null && selectedObject.TryGetComponent(out TerrainData terrainSelected))
        {
            Vector3Int terrainLocation = terrainSelected.TileCoordinates;

            //deselecting if choosing tile outside of city
            if (!cityTiles.Contains(terrainLocation))
            {
                ResetCityUI();
                //world.OpenTerrainTooltip(terrainSelected);
                return;
            }

            if (!tilesToChange.Contains(terrainLocation))
            {
                ResetCityUIToBase();

                //if (laborChange != 0)
                //{
                //    GiveWarningMessage("Nothing here");
                //}
                //else if (removingImprovement)
                //{
                //    GiveWarningMessage("Um, there's nothing here...");
                //}
                //else if (upgradingImprovement)
                //{
                //    GiveWarningMessage("Nothing here to upgrade");
                //}
                //else if (improvementData != null)
                //{
                //    if (world.CheckIfTileIsImproved(terrainLocation))
                //        GiveWarningMessage("Already something here");
                //    else
                //        GiveWarningMessage("Not a good spot");
                //}
            }
            else 
            {
                if (improvementData != null)
                {
                    BuildImprovementQueueCheck(improvementData, terrainLocation); //passing the data here as method requires it
                }
                else if (upgradingImprovement)
                {
                    CityImprovement improvement = world.GetCityDevelopment(terrainLocation);

                    UpgradeSelectedImprovementQueueCheck(terrainLocation, improvement);
                }
                else if (removingImprovement)
                {
                    CityImprovement improvement = world.GetCityDevelopment(terrainLocation);
                    
                    if (world.CheckIfTileIsUnderConstruction(terrainLocation))
                    {
                        improvement = world.GetCityDevelopmentConstruction(terrainLocation);
                        improvement.RemoveConstruction(this, terrainLocation);
                    }

                    if (!improvement.isConstruction && !improvement.isUpgrading)
                        improvement.PlayRemoveEffect(terrainSelected.terrainData.type == TerrainType.Hill);
                    RemoveImprovement(terrainLocation, improvement, selectedCity, false);
                }
                else if (laborChange != 0)
                {
                    if (constructingTiles.Contains(terrainLocation))
                    {
                        UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Still building...");
                        return;
                    }

                    ChangeLaborCount(terrainLocation);
                }
            }
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
        selectedWonder.SetUI(uiWonderSelection);
        selectedWonder.isActive = true;
        selectedWonder.TimeProgressBarSetActive(true);
        selectedWonder.EnableHighlight(Color.white);
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
        selectedTradeCenter.EnableHighlight(Color.white);
        uiTradeCenter.SetName(selectedTradeCenter.tradeCenterName);
        CenterCamOnCity();
    }

    public void BuildHarbor()
    {
        if (selectedWonder.hasHarbor)
        {
            selectedWonder.DestroyHarbor();
            uiWonderSelection.UpdateHarborButton(false);
            return;
        }
        
        if (!uiWonderSelection.buttonsAreWorking)
            return;

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

        selectedWonder.StopConstructing();
        selectedWonder.WorkersReceived--; //decrease worker count 

        if (uiWonderSelection.activeStatus)
            uiWonderSelection.UpdateUIWorkers(selectedWonder.WorkersReceived);

        GameObject workerGO = selectedWonder.WonderData.workerData.prefab;

        world.workerCount++;
        workerGO.name = selectedWonder.WonderData.workerData.name.Split("_")[0] + "_" + world.workerCount;

        Vector3Int buildPosition = selectedWonder.unloadLoc;
        if (world.IsUnitLocationTaken(buildPosition) || !world.CheckIfPositionIsValid(buildPosition)) //placing unit in world after building in city
        {
            //List<Vector3Int> newPositions = world.GetNeighborsFor(Vector3Int.FloorToInt(buildPosition));
            foreach (Vector3Int pos in world.GetNeighborsFor(buildPosition, MapWorld.State.EIGHTWAYTWODEEP))
            {
                if (!world.IsUnitLocationTaken(pos) && world.CheckIfPositionIsValid(pos))
                {
                    buildPosition = pos;
                    break;
                }
            }
        }

        GameObject unit = Instantiate(workerGO, buildPosition, Quaternion.identity); //produce unit at specified position
        //for tweening
        //Vector3 goScale = unit.transform.localScale;
        //float scaleX = goScale.x;
        //float scaleZ = goScale.z;
        //unit.transform.localScale = new Vector3(scaleX, 0, scaleZ);
        //LeanTween.scale(unit, goScale, 0.25f).setEase(LeanTweenType.easeOutBack);

        unit.name = unit.name.Replace("(Clone)", ""); //getting rid of the clone part in name 
        Unit newUnit = unit.GetComponent<Unit>();
        newUnit.SetReferences(world, focusCam, uiUnitTurn, movementSystem);
        newUnit.CurrentLocation = world.AddUnitPosition(buildPosition, newUnit);
    }

    public void CreateAllWorkers(Wonder wonder)
    {
        int workers = wonder.WorkersReceived;
        
        wonder.StopConstructing();
        wonder.WorkersReceived = 0; //decrease worker count 

        if (uiWonderSelection.activeStatus)
            uiWonderSelection.UpdateUIWorkers(0);

        int lostWorkersCount = 0;
        List<Vector3Int> locs = wonder.OuterRim();

        for (int i = 0; i < workers; i++)
        {
            GameObject workerGO = wonder.WonderData.workerData.prefab;
            world.workerCount++;
            workerGO.name = wonder.WonderData.workerData.name.Split("_")[0] + "_" + world.workerCount;

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
                    //for tweening
                    //Vector3 goScale = unit.transform.localScale;
                    //float scaleX = goScale.x;
                    //float scaleZ = goScale.z;
                    //unit.transform.localScale = new Vector3(scaleX, 0, scaleZ);
                    //LeanTween.scale(unit, goScale, 0.25f).setEase(LeanTweenType.easeOutBack);
                    unit.transform.LookAt(wonder.centerPos);
                    unit.GetComponent<Laborer>().StartLaborAnimations();
                    
                    unit.name = unit.name.Replace("(Clone)", ""); //getting rid of the clone part in name 
                    Unit newUnit = unit.GetComponent<Unit>();
                    newUnit.SetReferences(world, focusCam, uiUnitTurn, movementSystem);
                    newUnit.CurrentLocation = world.AddUnitPosition(loc, newUnit);

                    break;
                }
            }
        }

        if (lostWorkersCount > 0)
            InfoPopUpHandler.WarningMessage().Create(wonder.unloadLoc, "Lost " + lostWorkersCount.ToString() + " worker(s) due to no available space");
    }

    public void BuildWonderHarbor(Vector3Int loc)
    {
        GameObject harborGO = Instantiate(wonderHarbor, loc, Quaternion.Euler(0, HarborRotation(loc, selectedWonder.unloadLoc), 0));
        //for tweening
        Vector3 goScale = harborGO.transform.localScale;
        harborGO.GetComponent<CityImprovement>().PlaySmokeSplash(false);
        harborGO.transform.localScale = Vector3.zero;
        LeanTween.scale(harborGO, goScale, 0.25f).setEase(LeanTweenType.easeOutBack);
        selectedWonder.hasHarbor = true;
        selectedWonder.harborLoc = loc;

        world.AddToCityLabor(loc, selectedWonder.gameObject);
        world.AddStructure(loc, harborGO);
        world.AddTradeLoc(loc, selectedWonder.wonderName);
        uiWonderSelection.UpdateHarborButton(true);

        CloseImprovementBuildPanel();
    }

    public void OpenCancelWonderConstructionWarning()
    {
        if (uiWonderSelection.buttonsAreWorking)
            uiDestroyCityWarning.ToggleVisibility(true);
    }

    private void CancelWonderConstruction()
    {
        selectedWonder.PlayRemoveEffect();
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


        GameObject priorGO = world.GetStructure(selectedWonder.WonderLocs[2]);
        Destroy(priorGO);

        //for no walk zone
        int k = 0;
        int[] xArray = new int[selectedWonder.WonderLocs.Count];
        int[] zArray = new int[selectedWonder.WonderLocs.Count];

        foreach (Vector3Int tile in selectedWonder.WonderLocs)
        {
            world.RemoveStructure(tile);
            //world.RemoveStructureMap(tile);
            //world.ResetTileMap(tile);
            world.RemoveSingleBuildFromCityLabor(tile);
            world.RemoveWonder(tile);

            TerrainData td = world.GetTerrainDataAt(tile);
            if (td.prop != null)
                td.prop.gameObject.SetActive(true);
            td.main.gameObject.SetActive(true);

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
            foreach (Vector3Int neighbor in world.GetNeighborsFor(tile, MapWorld.State.EIGHTWAY))
            {
                if (neighbor.x == xMin || neighbor.x == xMax || neighbor.z == zMin || neighbor.z == zMax)
                    continue;

                world.RemoveFromNoWalkList(neighbor);
            }
        }

        selectedWonder = null;
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
        PopulateUpgradeDictForTesting();

        selectedCity = cityReference;

        world.cityCanvas.gameObject.SetActive(true);
        isActive = true;
        selectedCity.activeCity = true;
        selectedCityLoc = world.GetClosestTerrainLoc(location);
        (cityTiles, developedTiles, constructingTiles) = GetThisCityRadius();
        ResourceProducerTimeProgressBarsSetActive(true);
        ToggleBuildingHighlight(true);
        DrawBorders();
        CheckForWork();
        autoAssign.isOn = selectedCity.AutoAssignLabor;
        resourceManager = selectedCity.ResourceManager;
        resourceManager.SetUI(uiResourceManager, uiMarketPlaceManager, uiInfoPanelCity);
        //uiResourceManager.SetCityInfo(selectedCity.cityName, selectedCity.warehouseStorageLimit, selectedCity.ResourceManager.GetResourceStorageLevel);
        resourceManager.UpdateUI(selectedCity.GetResourceValues());
        uiCityTabs.ToggleVisibility(true, resourceManager);
        uiResourceManager.ToggleVisibility(true, selectedCity);
        CenterCamOnCity();
        uiInfoPanelCity.SetData(selectedCity.cityName, selectedCity.cityPop.CurrentPop, selectedCity.HousingCount, selectedCity.cityPop.UnusedLabor, selectedCity.workEthic,
            resourceManager.FoodGrowthLevel, resourceManager.FoodGrowthLimit, resourceManager.FoodPerMinute);
        uiInfoPanelCity.ToggleVisibility(true);
        uiLaborAssignment.ShowUI(selectedCity, placesToWork);
        uiLaborHandler.SetCity(selectedCity);
        uiUnitTurn.buttonClicked.AddListener(ResetCityUI);
        if (selectedCity.cityPop.CurrentPop > 0)
            abandonCityButton.interactable = false;
        else
            abandonCityButton.interactable = true;
        UpdateLaborNumbers();
        selectedCity.CityGrowthProgressBarSetActive(true);
        //selectedCity.Select();
    }

    public void ShowQueuedGhost()
    {
        foreach (Vector3Int tile in cityTiles) //improvements
        {
            if (world.ShowQueueGhost(tile))
                queuedGhost.Add(world.GetQueueGhost(tile));
        }

        foreach (string building in selectedCity.buildingQueueGhostDict.Keys) //buildings
        {
            GameObject ghost = selectedCity.buildingQueueGhostDict[building];
            ghost.SetActive(true);
            //for tweening
            Vector3 goScale = ghost.transform.localScale;
            ghost.transform.localScale = Vector3.zero;
            LeanTween.scale(ghost, goScale, 0.25f).setEase(LeanTweenType.easeOutBack);
            queuedGhost.Add(selectedCity.buildingQueueGhostDict[building]);
        }
    }

    public void HideQueuedGhost()
    {
        foreach (GameObject go in queuedGhost)
        {
            go.SetActive(false);
        }

        queuedGhost.Clear();
    }

    public void CreateQueuedGhost(ImprovementDataSO improvementData, Vector3Int loc, bool isBuilding)
    {
        Color newColor = new(0, 1f, 0, 0.8f);
        Vector3 newLoc = loc;

        if (isBuilding)
            newLoc += improvementData.buildingLocation;
        else if (improvementData.replaceTerrain)
            newLoc.y += .1f;

        GameObject improvementGhost = Instantiate(improvementData.prefab, newLoc, Quaternion.identity);
        //for tweening
        Vector3 goScale = improvementGhost.transform.localScale;
        improvementGhost.transform.localScale = Vector3.zero;
        LeanTween.scale(improvementGhost, goScale, 0.25f).setEase(LeanTweenType.easeOutBack);
        MeshRenderer[] renderers = improvementGhost.GetComponentsInChildren<MeshRenderer>();

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

        if (isBuilding)
            selectedCity.buildingQueueGhostDict[improvementData.improvementName] = improvementGhost;
        else
        {
            CityImprovement improvement = improvementGhost.GetComponent<CityImprovement>();
            improvement.SetCity(selectedCity);
            world.SetQueueGhost(loc, improvementGhost);
            //world.SetQueueImprovement()
        }

        queuedGhost.Add(improvementGhost);
    }

    public void CreateQueuedArrow(ImprovementDataSO improvementData, Vector3Int tempBuildLocation, bool isBuilding)
    {
        //setting up arrow ghost
        GameObject arrowGhost = Instantiate(upgradeQueueGhost, tempBuildLocation, Quaternion.Euler(0, 90f, 0));
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
            selectedCity.buildingQueueGhostDict[improvementData.improvementName] = arrowGhost;
        }
        else
        {
            world.SetQueueGhost(tempBuildLocation, arrowGhost);
        }

        queuedGhost.Add(arrowGhost);
    }

    public void RemoveQueueGhostImprovement(Vector3Int loc, City city)
    {
        GameObject go = world.GetQueueGhost(loc);
        bool upgrading = false;
        if (go.CompareTag("Upgrade"))
            upgrading = true;
        if (city.activeCity)
            queuedGhost.Remove(go);
        Destroy(go);
        world.RemoveQueueGhost(loc);

        if (city.activeCity && cityTiles.Contains(loc))
        {
            if (upgrading && upgradingImprovement)
            {
                tilesToChange.Add(loc);
                world.GetTerrainDataAt(loc).EnableHighlight(Color.green);
            }
        }
    }

    public void RemoveQueueGhostBuilding(string building, City city)
    {
        GameObject go = city.buildingQueueGhostDict[building];
        if (city.activeCity)
            queuedGhost.Remove(go);
        Destroy(go);
        city.buildingQueueGhostDict.Remove(building);
    }

    public void SellResources()
    {
        uiMarketPlaceManager.ToggleVisibility(true, selectedCity);
    }

    public void CloseSellResources()
    {
        uiMarketPlaceManager.ToggleVisibility(false);
        uiCityTabs.CloseSelectedTab();
    }

    public void RemoveImprovements()
    {
        laborChange = 0;
        //uiCityTabs.HideSelectedTab();
        //CloseLaborMenus();
        
        removingImprovement = true;
        CloseQueueUI();
        ToggleBuildingHighlight(true);
        ImprovementTileHighlight();
    }

    public void RemoveConstruction(Vector3Int tempBuildLocation)
    {
        constructingTiles.Remove(tempBuildLocation);
        world.RemoveConstruction(tempBuildLocation);
    }

    public void UpgradeImprovements()
    {
        upgradingImprovement = true;
        ToggleBuildingHighlight(true);
        UpgradeTileHighlight();
    }

    private void UpgradeTileHighlight()
    {
        tilesToChange.Clear();

        uiImprovementBuildInfoPanel.SetText("Upgrading Building");
        uiImprovementBuildInfoPanel.SetImage(upgradeButton, false);
        uiImprovementBuildInfoPanel.ToggleVisibility(true);

        foreach (Vector3Int tile in developedTiles)
        {
            if (isQueueing && world.CheckQueueLocation(tile))
                continue;
            
            TerrainData td = world.GetTerrainDataAt(tile);
            td.DisableHighlight(); //turning off all highlights first
            PoolResourceInfoPanel(tile);
            resourceInfoHolderDict.Remove(tile);
            resourceInfoPanelDict.Remove(tile);

            CityImprovement improvement = world.GetCityDevelopment(tile); 
            improvement.DisableHighlight();

            if (improvement.GetImprovementData.improvementLevel < world.GetUpgradeableObjectMaxLevel(improvement.GetImprovementData.improvementName) && !improvement.isUpgrading)
            {
                td.EnableHighlight(Color.green);
                improvement.EnableHighlight(Color.green);
                tilesToChange.Add(tile);

                SetUpResourceInfoPanel(improvement, tile);
            }
        }
    }

    //for updating resource info while choosing upgrade options
    public void UpdateResourceInfo()
    {
        if (upgradingImprovement)
        {
            foreach (Vector3 location in resourceInfoHolderDict.Keys)
                PoolResourceInfoPanel(location);    
            resourceInfoHolderDict.Clear();
            resourceInfoPanelDict.Clear();

            foreach (Vector3Int tile in tilesToChange)
            {
                CityImprovement improvement = world.GetCityDevelopment(tile);
                SetUpResourceInfoPanel(improvement, tile);
            }

            ToggleBuildingResourceInfo();
        }
    }

    public void UpgradeSelectedImprovementQueueCheck(Vector3Int tempBuildLocation, CityImprovement selectedImprovement)
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
            if (!uiQueueManager.AddToQueue(tempBuildLocation, tempBuildLocation - selectedCityLoc, world.GetUpgradeData(nameAndLevel), null, new(world.GetUpgradeCost(nameAndLevel))))
                return; //checks if queue item already exists
            return;
        }

        foreach (ResourceValue value in world.GetUpgradeCost(nameAndLevel))
        {
            if (!selectedCity.ResourceManager.CheckResourceAvailability(value))
            {
                UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't afford");
                return;
            }
        }

        tilesToChange.Remove(tempBuildLocation);
        world.GetTerrainDataAt(tempBuildLocation).DisableHighlight();
        UpgradeSelectedImprovementPrep(tempBuildLocation, selectedImprovement, selectedCity);
    }

    private void UpgradeSelectedImprovementPrep(Vector3Int upgradeLoc, CityImprovement selectedImprovement, City city)
    {
        if (city.activeCity)
        {
            selectedImprovement.DisableHighlight();
                
            if (upgradeLoc == selectedCityLoc)
            {
                PoolResourceInfoPanel(selectedImprovement.GetImprovementData.buildingLocation + selectedCityLoc);
                resourceInfoHolderDict.Remove(selectedImprovement.GetImprovementData.buildingLocation + selectedCityLoc);
                resourceInfoPanelDict.Remove(selectedImprovement.GetImprovementData.buildingLocation + selectedCityLoc); //can't be in previous method
            }
            else
            {
                PoolResourceInfoPanel(upgradeLoc);
                resourceInfoHolderDict.Remove(upgradeLoc);
                resourceInfoPanelDict.Remove(upgradeLoc);
             
                //placesToWork--;
                //UpdateCityLaborUIs();
            }
        }

        string nameAndLevel = selectedImprovement.GetImprovementData.improvementNameAndLevel;
        List<ResourceValue> upgradeCost = new(world.GetUpgradeCost(nameAndLevel));
        city.ResourceManager.SpendResource(upgradeCost, upgradeLoc);
        selectedImprovement.UpgradeCost = upgradeCost;
        ImprovementDataSO data = world.GetUpgradeData(nameAndLevel);

        if (upgradeLoc == city.cityLoc)
        {
            //selectedImprovement.PlayUpgradeSplash();
            RemoveImprovement(upgradeLoc, selectedImprovement, city, true);
            CreateBuilding(data, city, true);
        }
        else
        {
            //putting the labor back
            ResourceProducer resourceProducer = world.GetResourceProducer(upgradeLoc);

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
            resourceProducer.UpdateResourceGenerationData();
            resourceProducer.StopProducing();

            if (city.activeCity)
            {
                if (!world.CheckIfTileIsMaxxed(upgradeLoc))
                    placesToWork--;
                int currentLabor = world.GetCurrentLaborForTile(upgradeLoc);
                city.cityPop.UnusedLabor += currentLabor;
                city.cityPop.UsedLabor -= currentLabor;

                RemoveLaborFromDicts(upgradeLoc);
                UpdateCityLaborUIs();
                uiInfoPanelCity.SetData(selectedCity.cityName, selectedCity.cityPop.CurrentPop, selectedCity.HousingCount, selectedCity.cityPop.UnusedLabor, selectedCity.workEthic,
                    resourceManager.FoodGrowthLevel, resourceManager.FoodGrowthLimit, resourceManager.FoodPerMinute);
                UpdateLaborNumbers();
                uiLaborAssignment.UpdateUI(selectedCity, placesToWork);

                resourceManager.UpdateUI(city.GetResourceValues());
                uiResourceManager.SetCityCurrentStorage(city.ResourceManager.GetResourceStorageLevel);
            }
            else
            {
                RemoveLaborFromDicts(upgradeLoc);
            }
            
            city.UpdateCityPopInfo();

            if (city.AutoAssignLabor && city.cityPop.UnusedLabor > 0)
                city.AutoAssignmentsForLabor();

            selectedImprovement.BeginImprovementUpgradeProcess(city, resourceProducer, upgradeLoc, this, data, world.GetTerrainDataAt(upgradeLoc).terrainData.type == TerrainType.Hill);
        }
    }

    public void UpgradeSelectedImprovement(Vector3Int upgradeLoc, CityImprovement selectedImprovement, City city, ImprovementDataSO data)
    {
        RemoveImprovement(upgradeLoc, selectedImprovement, city, true);
     
        //string nameAndLevel = selectedImprovement.GetImprovementData.improvementName + '-' + selectedImprovement.GetImprovementData.improvementLevel;
        //List<ResourceValue> upgradeCost = new(world.GetUpgradeCost(nameAndLevel));
        BuildImprovement(data, upgradeLoc, city, true);

        //if (!city.activeCity)
        //    return;

        //if (upgradeLoc == selectedCityLoc)
        //{
        //    PoolResourceInfoPanel(selectedImprovement.GetImprovementData.buildingLocation + selectedCityLoc);
        //    resourceInfoPanelDict.Remove(selectedImprovement.GetImprovementData.buildingLocation); //can't be in previous method
        //}
        //else
        //{
        //    PoolResourceInfoPanel(upgradeLoc);
        //    resourceInfoPanelDict.Remove(upgradeLoc);
        //}
    }

    private void SetUpResourceInfoPanel(CityImprovement improvement, Vector3 tile, bool building = false)
    {
        //setting up the resourceInfoPanels to appear below improvement
        List<ResourceInfoPanel> resourceInfoPanelList = new();
        List<ResourceValue> upgradeCost = new(world.GetUpgradeCost(improvement.GetImprovementData.improvementName + '-' + improvement.GetImprovementData.improvementLevel));
        int resourceCount = upgradeCost.Count;
        Vector3 holderPos = tile;
        //holderPos.y -= 1f;
        holderPos.z -= 1.5f;
        if (building)
            holderPos.z += 1;
        ResourceInfoHolder resourceInfoHolder = GetFromResourceInfoHolderPool();
        resourceInfoHolder.transform.position = holderPos;

        int i = 0;
        foreach (ResourceValue value in upgradeCost)
        {
            Vector3 panelPos = new Vector3(0, 0, 0);
            panelPos.x -= .32f * (resourceCount - 1);
            panelPos.x += .64f * i;

            ResourceInfoPanel resourceInfoPanel = GetFromResourceInfoPanelPool();
            bool haveEnough = selectedCity.ResourceManager.CheckResourceAvailability(value);
            resourceInfoPanel.SetResourcePanel(world.GetResourceIcon(value.resourceType), value.resourceAmount, haveEnough);
            resourceInfoPanel.transform.SetParent(resourceInfoHolder.transform);
            resourceInfoPanel.transform.localPosition = panelPos;
            resourceInfoPanelList.Add(resourceInfoPanel);
            i++;
        }

        resourceInfoHolderDict[tile] = resourceInfoHolder;
        resourceInfoPanelDict[tile] = resourceInfoPanelList;
    }

    private void PoolResourceInfoPanel(Vector3 location)
    {
        if (!resourceInfoHolderDict.ContainsKey(location))
            return;

        AddToResourceInfoHolderPool(resourceInfoHolderDict[location]);

        foreach (ResourceInfoPanel resourceInfoPanel in resourceInfoPanelDict[location])
            AddToResourceInfoPanelPool(resourceInfoPanel);
    }

    private void ToggleBuildingHighlight(bool v)
    {
        if (v)
        {
            foreach (string name in world.GetBuildingListForCity(selectedCityLoc))
            {
                CityImprovement building = world.GetBuildingData(selectedCityLoc, name);
                building.DisableHighlight();
                //building.DisableHighlight2();

                if (removingImprovement)
                {
                    //don't highlight the buildings that can't be removed
                    if (building.initialCityHouse)
                        building.EnableHighlight(Color.white);
                    else
                        building.EnableHighlight(Color.red, true);
                }
                else if (upgradingImprovement)
                {
                    if (building.GetImprovementData.improvementLevel < world.GetUpgradeableObjectMaxLevel(building.GetImprovementData.improvementName))
                    {
                        building.EnableHighlight(Color.green, true);
                        SetUpResourceInfoPanel(building, building.GetImprovementData.buildingLocation+selectedCityLoc, true);
                    }
                    else
                    {
                        building.EnableHighlight(Color.white);
                    }
                }
                else
                    building.EnableHighlight(Color.white);
            }
        }
        else
        {
            foreach (string name in world.GetBuildingListForCity(selectedCityLoc))
            {
                CityImprovement building = world.GetBuildingData(selectedCityLoc, name);
                //int count = building.MeshFilter.Length;
                //for (int i = 0; i < count; i++)
                //{
                //    building.MeshFilter[i].gameObject.SetActive(false);
                //}
                building.DisableHighlight();
                //building.DisableHighlight2();
            }
        }
    }

    //a bit faster than using above method
    private void ToggleBuildingResourceInfo()
    {
        foreach (string name in world.GetBuildingListForCity(selectedCityLoc))
        {
            CityImprovement building = world.GetBuildingData(selectedCityLoc, name);

            if (building.GetImprovementData.improvementLevel < world.GetUpgradeableObjectMaxLevel(building.GetImprovementData.improvementName))
            {
                SetUpResourceInfoPanel(building, building.GetImprovementData.buildingLocation + selectedCityLoc, true);
            }
        }
    }

    private (List<Vector3Int>, List<Vector3Int>, List<Vector3Int>) GetThisCityRadius() //radius just for selected city
    {
        return world.GetCityRadiusFor(selectedCityLoc, selectedCity.gameObject);
    }

    private void ResourceProducerTimeProgressBarsSetActive(bool v)
    {
        foreach (Vector3Int tile in developedTiles)
        {
            ResourceProducer producer = world.GetResourceProducer(tile);
            if (v && producer.isWaitingToUnload)
            {
                producer.TimeProgressBarSetActive(v);
                producer.SetTimeProgressBarToFull();
            }
            else if (!producer.isWaitingForResearch && !producer.isWaitingForStorageRoom && !producer.isWaitingforResources)
            {
                if (producer.isUpgrading)
                {
                    int time = 0;
                    if (v)
                        time = world.GetCityDevelopment(tile).GetTimePassed;
                    world.GetResourceProducer(tile).TimeConstructionProgressBarSetActive(v, time);
                    continue;
                }
                producer.TimeProgressBarSetActive(v);
            }
        }

        foreach (Vector3Int tile in constructingTiles)
        {
            int time = 0; 
            if (v)
                time = world.GetCityDevelopmentConstruction(tile).GetTimePassed;

            world.GetResourceProducer(tile).TimeConstructionProgressBarSetActive(v, time);
        }
    }

    private void DrawBorders()
    {
        List<Vector3Int> tempCityTiles = new(cityTiles) {selectedCityLoc};

        foreach (Vector3Int tile in tempCityTiles)
        {
            //finding border neighbors for tile
            List<Vector3Int> neighborList = world.GetNeighborsFor(tile, MapWorld.State.FOURWAYINCREMENT);
            foreach (Vector3Int neighbor in neighborList)
            {
                if (!tempCityTiles.Contains(neighbor)) //only draw borders on areas that aren't city tiles
                {
                    //Object pooling set up
                    GameObject tempObject = GetFromBorderPool();
                    borderList.Add(tempObject);

                    Vector3Int borderLocation = neighbor - tile; //used to determine where on tile border should be
                    Vector3 borderPosition = tile;
                    borderPosition.y = 0f;

                    if (borderLocation.x != 0)
                    {
                        borderPosition.x += (0.5f * borderLocation.x);
                        tempObject.transform.rotation = Quaternion.Euler(0, 90, 0); //rotating to make it look like city wall
                    }
                    if (borderLocation.z != 0)
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
        CreateUnitQueueCheck(unitData);
    }

    private void CreateUnitQueueCheck(UnitBuildDataSO unitData) //action for the button to run
    {
        if (isQueueing)
        {
            uiQueueManager.AddToQueue(selectedCityLoc, new Vector3Int(0,0,0), null, unitData);
            return;
        }

        CreateUnit(unitData, selectedCity);
    }

    private void CreateUnit(UnitBuildDataSO unitData, City city)
    {
        city.ResourceManager.SpendResource(unitData.unitCost, city.cityLoc);

        for (int i = 0; i < unitData.laborCost; i++)
        {
            city.PopulationDeclineCheck(); //decrease population before creating unit so we can see where labor will be lost
        }

        //updating uis after losing pop
        if (selectedCity != null && selectedCity == city)
        {
            uiQueueManager.CheckIfBuiltUnitIsQueued(unitData, city.cityLoc);
            UpdateLaborNumbers();
            uiLaborAssignment.UpdateUI(city, placesToWork);
            uiInfoPanelCity.SetData(selectedCity.cityName, selectedCity.cityPop.CurrentPop, selectedCity.HousingCount, selectedCity.cityPop.UnusedLabor, selectedCity.workEthic,
                resourceManager.FoodGrowthLevel, resourceManager.FoodGrowthLimit, resourceManager.FoodPerMinute);
            resourceManager.UpdateUI(unitData.unitCost);
            uiResourceManager.SetCityCurrentStorage(city.ResourceManager.GetResourceStorageLevel);
            uiCityTabs.HideSelectedTab();
        }

        GameObject unitGO = unitData.prefab;

        if (unitData.unitType == UnitType.Worker)
        {
            world.workerCount++;
            unitGO.name = unitData.name.Split("_")[0] + "_" + world.workerCount;
        }
        if (unitData.unitType == UnitType.Infantry)
        {
            world.infantryCount++;
            unitGO.name = unitData.name.Split("_")[0] + "_" + world.infantryCount;
        }

        Vector3Int buildPosition = selectedCityLoc;
        if (world.IsUnitLocationTaken(buildPosition)) //placing unit in world after building in city
        {
            //List<Vector3Int> newPositions = world.GetNeighborsFor(Vector3Int.FloorToInt(buildPosition));
            foreach (Vector3Int pos in world.GetNeighborsFor(buildPosition, MapWorld.State.EIGHTWAYTWODEEP))
            {
                if (!world.IsUnitLocationTaken(pos) && world.GetTerrainDataAt(pos).terrainData.walkable)
                {
                    buildPosition = pos;
                    break;
                }
            }

            if (buildPosition == Vector3Int.RoundToInt(transform.position))
            {
                Debug.Log("No suitable locations to build unit");
                return;
            }
        }

        GameObject unit = Instantiate(unitGO, buildPosition, Quaternion.identity); //produce unit at specified position
        unit.gameObject.transform.SetParent(friendlyUnitHolder, false);
        //for tweening
        //Vector3 goScale = unitGO.transform.localScale;
        //float scaleX = goScale.x;
        //float scaleZ = goScale.z;
        //unit.transform.localScale = new Vector3(scaleX, 0, scaleZ);
        //LeanTween.scale(unit, goScale, 0.5f).setEase(LeanTweenType.easeOutBack);
        unit.name = unit.name.Replace("(Clone)", ""); //getting rid of the clone part in name 
        Unit newUnit = unit.GetComponent<Unit>();
        newUnit.SetReferences(world, focusCam, uiUnitTurn, movementSystem);
        newUnit.SetMinimapIcon(friendlyUnitHolder);

        Vector3 mainCamLoc = Camera.main.transform.position;
        mainCamLoc.y = 0;
        unit.transform.LookAt(mainCamLoc);
        newUnit.CurrentLocation = world.AddUnitPosition(buildPosition, newUnit);
    }

    public void CreateBuildingQueueCheck(ImprovementDataSO buildingData)
    {
        //Queue info
        if (isQueueing)
        {
            uiQueueManager.AddToQueue(selectedCityLoc, new Vector3Int(0, 0, 0), buildingData);
            return;
        }

        CreateBuilding(buildingData, selectedCity, false);
    }

    private void CreateBuilding(ImprovementDataSO buildingData, City city, bool upgradingImprovement)
    {
        if(uiQueueManager.CheckIfBuiltItemIsQueued(city.cityLoc, new Vector3Int(0, 0, 0), upgradingImprovement, buildingData, city))
            RemoveQueueGhostBuilding(buildingData.improvementName, city);

        //for some non buildings in the building selection list (eg harbor)
        if (!buildingData.isBuilding)
        {
            CreateImprovement(buildingData);
            return;
        }
        //laborChange = 0;

        //setting building locations
        Vector3 cityPos = city.cityLoc;
        Vector3 buildingLocalPos = buildingData.buildingLocation; //putting the building in it's position in the city square
        cityPos += buildingLocalPos;

        //resource info
        //if (upgradingImprovement)
        //{
        //    city.ResourceManager.SpendResource(upgradeCost);
        //    //upgradeCost.Clear();
        //}
        if (!upgradingImprovement)
        {
            city.ResourceManager.SpendResource(buildingData.improvementCost, cityPos); 
        }
        //uiInfoPanelCityWarehouse.SetWarehouseStorageLevel(selectedCity.ResourceManager.GetResourceStorageLevel);
        //Debug.Log("Placing structure in " + selectedCity.CityName);

        //setting world data
        GameObject building = Instantiate(buildingData.prefab, cityPos, Quaternion.identity);
        CityImprovement improvement = building.GetComponent<CityImprovement>();
        improvement.loc = city.cityLoc;
        //if (!upgradingImprovement)
        //    improvement.DestroyUpgradeSplash();
        building.transform.parent = city.subTransform;

        //if (upgradingImprovement)
        //{
        //    CityImprovement buildingImprovement = building.GetComponent<CityImprovement>();
        //    buildingImprovement.PlayUpgradeSplash();
        //}

        string buildingName = buildingData.improvementName;
        world.SetCityBuilding(improvement, buildingData, city.cityLoc, building, city);
        //world.AddToCityMaxLaborDict(city.cityLoc, buildingName, buildingData.maxLabor);
        city.HousingCount += buildingData.housingIncrease;

        //for tweening
        Vector3 goScale = building.transform.localScale;
        building.transform.localScale = Vector3.zero;
        LeanTween.scale(building, goScale, 0.25f).setEase(LeanTweenType.easeOutBack).setOnComplete( ()=> { CombineMeshes(city, city.subTransform, upgradingImprovement); improvement.SetInactive(); });

        if (buildingData.singleBuild)
        {
            city.singleBuildImprovementsBuildingsDict[buildingData.improvementName] = city.cityLoc;
        }

        //ResourceProducer resourceProducer = building.GetComponent<ResourceProducer>();
        //if (resourceProducer != null) //not all buildings will generate resources 
        //{
        //    resourceProducer.SetResourceManager(resourceManager); //need to set resourceManager for each new resource producer. 
        //    resourceProducer.InitializeImprovementData(buildingData); //allows the new structure to also start generating resources
        //    resourceProducer.SetLocation(selectedCityLoc);
        //    world.AddToCityBuildingIsProducerDict(selectedCityLoc, buildingName, resourceProducer);
        //}

        //WorkEthicHandler workEthicHandler = building.GetComponent<WorkEthicHandler>();
        //if (workEthicHandler != null) //not all buildings will have work ethic changes
        //{
        //    workEthicHandler.InitializeImprovementData(buildingData);
        //    workEthicHandler.GetWorkEthicChange(1);
        //}

        //updating uis
        if (selectedCity != null && city == selectedCity)
        {
            if (buildingData.housingIncrease > 0)  
                uiInfoPanelCity.UpdateHousing(city.HousingCount);
            if (buildingData.workEthicChange > 0)
            {
                uiInfoPanelCity.UpdateWorkEthic(city.workEthic);
                UpdateCityWorkEthic();
            }
     
            resourceManager.UpdateUI(buildingData.improvementCost);
            uiResourceManager.SetCityCurrentStorage(city.ResourceManager.GetResourceStorageLevel);

            //setting labor data (no labor for buildings)
            //if (buildingData.maxLabor > 0)
            //{
            //    city.PlacesToWork++;
            //    uiLaborAssignment.UpdateUI(city.cityPop, city.PlacesToWork);
            //}

            uiCityTabs.HideSelectedTab();
        }

        //StartCoroutine(TestRun(city));
        //CombineMeshes(city.transform, city.CityMeshFilters);
    }

    //private IEnumerator TestRun(City city)
    //{
    //    yield return new WaitForSeconds(0.26f);

    //    CombineMeshes(city.transform, city.CityMeshFilters);
    //}

    //public void RemoveBuildingButton()
    //{
    //    //uiBuildingBuilder.ToggleVisibility(false);
    //    laborChange = 0;
    //    uiCityTabs.HideSelectedTab();

    //    //if (world.GetBuildingListForCity(selectedCityLoc).Count == 0)
    //    //    return;

    //    uiLaborHandler.ShowUIRemoveBuildings(selectedCityLoc, world);
    //    removingBuilding = true;
    //    ToggleBuildingHighlight(true);
    //}

    private void RemoveBuilding(ImprovementDataSO data, City city, bool upgradingImprovement)
    {
        //putting the resources and labor back
        string selectedBuilding = data.improvementName;
        GameObject building = world.GetBuilding(city.cityLoc, selectedBuilding);
        //WorkEthicHandler workEthicHandler = building.GetComponent<WorkEthicHandler>();

        //if (world.CheckBuildingIsProducer(selectedCityLoc, selectedBuilding))
        //{
        //    ResourceProducer resourceProducer = world.GetBuildingProducer(selectedCityLoc, selectedBuilding);
        //    resourceProducer.UpdateCurrentLaborData(0);

        //    foreach (ResourceValue resourceValue in resourceProducer.GetImprovementData.improvementCost) //adding back 100% of cost (if there's room)
        //    {
        //        resourceManager.CheckResource(resourceValue.resourceType, resourceValue.resourceAmount);
        //    }
        //}
        //else //if building has both, still only do one (should have at least one)
        //{
        if (!upgradingImprovement)
        {
            int i = 0;
            Vector3 cityLoc = city.cityLoc;
            cityLoc.y += data.improvementCost.Count * 0.4f;

            foreach (ResourceValue resourceValue in data.improvementCost)
            {
                int resourcesReturned = resourceManager.CheckResource(resourceValue.resourceType, resourceValue.resourceAmount);
                Vector3 cityLoc2 = cityLoc;
                cityLoc2.y += -.4f * i;
                InfoResourcePopUpHandler.CreateResourceStat(cityLoc2, resourcesReturned, ResourceHolder.Instance.GetIcon(resourceValue.resourceType));
                i++;
            }
        }
        //}

        //if (workEthicHandler != null)
        //{
        //    UpdateCityWorkEthic(workEthicHandler.GetWorkEthicChange(0), city); //zero out labor
        //}

        //changing city stats
        city.HousingCount -= data.housingIncrease;
        city.workEthic -= data.workEthicChange;

        if (city.singleBuildImprovementsBuildingsDict.ContainsKey(selectedBuilding))
            city.singleBuildImprovementsBuildingsDict.Remove(selectedBuilding);

        //updating ui
        if (city.activeCity)
        {
            uiInfoPanelCity.UpdateHousing(city.HousingCount);
            uiInfoPanelCity.UpdateWorkEthic(city.workEthic);
            if (data.workEthicChange > 0)
                UpdateCityWorkEthic();
            resourceManager.UpdateUI(data.improvementCost);
            uiResourceManager.SetCityCurrentStorage(resourceManager.GetResourceStorageLevel);
            uiInfoPanelCity.SetData(selectedCity.cityName, selectedCity.cityPop.CurrentPop, selectedCity.HousingCount, selectedCity.cityPop.UnusedLabor, selectedCity.workEthic,
                resourceManager.FoodGrowthLevel, resourceManager.FoodGrowthLimit, resourceManager.FoodPerMinute);
            uiCityTabs.HideSelectedTab();
        }
        //uiInfoPanelCityWarehouse.SetWarehouseStorageLevel(selectedCity.ResourceManager.GetResourceStorageLevel);

        //int currentLabor = world.GetCurrentLaborForBuilding(selectedCityLoc, selectedBuilding);
        //int maxLabor = world.GetMaxLaborForBuilding(selectedCityLoc, selectedBuilding);
        //selectedCity.cityPop.UnusedLabor += currentLabor;
        //selectedCity.cityPop.UsedLabor -= currentLabor;

        city.RemoveFromMeshFilterList(true, Vector3Int.zero, selectedBuilding);
        Destroy(building);

        //updating world dicts
        world.RemoveCityBuilding(city.cityLoc, selectedBuilding);
        
        //updating city graphic
        CombineMeshes(city, city.subTransform, upgradingImprovement);

        //updating all the labor info
        //selectedCity.UpdateCityPopInfo();

        //RemoveLaborFromBuildingDicts(selectedBuilding);
        //resourceManager.UpdateUIGenerationAll();
        //UpdateLaborNumbers();

        //this object maintenance
        //if (currentLabor < maxLabor)
        //    placesToWork--;
        //uiImprovementBuilder.ToggleVisibility(false);
        //uiLaborAssignment.UpdateUI(selectedCity.cityPop, placesToWork);
        //removingBuilding = false;
        //uiLaborHandler.ShowUIRemoveBuildings(selectedCityLoc, world);
        //if (world.GetBuildingListForCity(selectedCityLoc).Count == 0)
        //    uiLaborHandler.HideUI();
    }

    public void CreateImprovement(ImprovementDataSO improvementData)
    {
        laborChange = 0;

        this.improvementData = improvementData;

        //uiImprovementBuilder.ToggleVisibility(false);
        uiCityTabs.HideSelectedTab();
        ImprovementTileHighlight();
    }

    private void ImprovementTileHighlight()
    {
        //CameraBirdsEyeRotationAsync();
        //StartCoroutine(CameraBirdsEyeRotation(5f));
        //if (!removingImprovement)
        //    CameraBirdsEyeRotation();

        tilesToChange.Clear();

        if (removingImprovement)
        {
            if (!upgradingImprovement) //only show when not upgrading
            {
                uiImprovementBuildInfoPanel.SetText("Removing Building");
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
                if (world.CheckIfTileIsImproved(tile))
                {
                    CityImprovement improvement = world.GetCityDevelopment(tile); //cached for speed
                    improvement.DisableHighlight();

                    improvement.EnableHighlight(Color.red);
                    td.EnableHighlight(Color.red);
                    tilesToChange.Add(tile);
                }                
                else if (world.CheckIfTileIsUnderConstruction(tile))
                {
                    //CityImprovement improvement = world.GetCityDevelopmentConstruction(tile);
                    //improvement.DisableHighlight();

                    //improvement.EnableHighlight(Color.red);
                    td.EnableHighlight(Color.red);
                    tilesToChange.Add(tile);
                }
            }
            else //if placing improvement
            {
                if (isQueueing && world.CheckQueueLocation(tile))
                    continue;
                
                if (world.IsTileOpenCheck(tile) && td.terrainData.type == improvementData.terrainType)
                {
                    if (improvementData.rawMaterials && td.terrainData.rawResourceType == improvementData.rawResourceType)
                    {
                        td.EnableHighlight(Color.white);
                        tilesToChange.Add(tile);
                    }
                    else if (!improvementData.rawMaterials)
                    {
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
            if (!uiQueueManager.AddToQueue(tempBuildLocation, tempBuildLocation - selectedCityLoc, improvementData))
                return;
            tilesToChange.Remove(tempBuildLocation);
            world.GetTerrainDataAt(tempBuildLocation).DisableHighlight();
            return;
        }

        BuildImprovement(improvementData, tempBuildLocation, selectedCity, false);
    }

    private void BuildImprovement(ImprovementDataSO improvementData, Vector3Int tempBuildLocation, City city, bool upgradingImprovement)
    {
        if (uiQueueManager.CheckIfBuiltItemIsQueued(tempBuildLocation, tempBuildLocation - city.cityLoc, upgradingImprovement, improvementData, city))
            RemoveQueueGhostImprovement(tempBuildLocation, city);

        //if (tempBuildLocation == city.cityLoc)
        //{
        //    CreateBuilding(improvementData, city, upgradingImprovement, upgradeCost);
        //    return;
        //}

        if (!upgradingImprovement && !world.IsTileOpenCheck(tempBuildLocation))
        {
            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Something already here");
            return;
        }

        //spending resources to build
        Vector3Int buildLocation = tempBuildLocation;
        buildLocation.y = 0;

        //if (upgradingImprovement)
        //{
        //    //city.ResourceManager.SpendResource(upgradeCost);
        //    //upgradeCost.Clear();
        //}
        if (!upgradingImprovement)
        {
            city.ResourceManager.SpendResource(improvementData.improvementCost, buildLocation);
        }

        if (city.activeCity)
        {
            resourceManager.UpdateUI(improvementData.improvementCost);
            uiResourceManager.SetCityCurrentStorage(city.ResourceManager.GetResourceStorageLevel);
        }

        //rotating harbor so it's closest to city
        int rotation = 0;
        if (improvementData.terrainType == TerrainType.Coast || improvementData.terrainType == TerrainType.River)
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
                else if (td.terrainData.resourceType == tempData.producedResources[0].resourceType)
                {   
                    improvementData = tempData;
                    break;
                }
            }
            
            rotation = (int)td.transform.eulerAngles.y;
        }

        //if (improvementData.replaceTerrain)
        //{
        //    rotation = (int)td.transform.eulerAngles.y;
        //    //Vector2[] test = td.main.GetComponentInChildren<Mesh>().uv;
        //    MeshFilter ugh = td.main.GetComponentInChildren<MeshFilter>();
        //    Vector2[] test = ugh.mesh.uv;

        //    //Vector2[] test2 = improvementData.prefab.GetComponentInChildren<MeshFilter>().mesh.uv;

        //    foreach (MeshFilter mesh in improvementData.prefab.GetComponentsInChildren<MeshFilter>())
        //    {

        //    }
        //}
        bool isHill = td.terrainData.type == TerrainType.Hill;
        GameObject improvement;
        if (improvementData.isBuilding && isHill)
        {
            Vector3 buildLocationHill = buildLocation;
            buildLocationHill.y += .6f;
            improvement = Instantiate(improvementData.prefab, buildLocationHill, Quaternion.Euler(0, rotation, 0));
        }
        else
        {
            improvement = Instantiate(improvementData.prefab, buildLocation, Quaternion.Euler(0, rotation, 0));
        }

        world.AddStructure(buildLocation, improvement);
        CityImprovement cityImprovement = improvement.GetComponent<CityImprovement>();
        cityImprovement.loc = buildLocation;
        cityImprovement.InitializeImprovementData(improvementData);
        //cityImprovement.SetPSLocs();
        cityImprovement.SetQueueCity(null);

        world.SetCityDevelopment(tempBuildLocation, cityImprovement);
        improvement.SetActive(false);

        //setting single build rules
        if (improvementData.singleBuild)
        {
            city.singleBuildImprovementsBuildingsDict[improvementData.improvementName] = tempBuildLocation;
            world.AddToCityLabor(tempBuildLocation, city.gameObject);
        }

        //resource production
        ResourceProducer resourceProducer = improvement.GetComponent<ResourceProducer>();
        buildLocation.y = 0;
        world.AddResourceProducer(buildLocation, resourceProducer);
        resourceProducer.SetResourceManager(city.ResourceManager);
        resourceProducer.InitializeImprovementData(improvementData); //allows the new structure to also start generating resources
        resourceProducer.SetCityImprovement(cityImprovement);
        resourceProducer.SetLocation(tempBuildLocation);

        if (upgradingImprovement)
        {
            resourceProducer.SetUpgradeProgressTimeBar(improvementData.buildTime);
            FinishImprovement(city, improvementData, tempBuildLocation);
        }
        else
        {
            CityImprovement constructionTile = GetFromConstructionTilePool();
            constructionTile.InitializeImprovementData(improvementData);
            world.SetCityImprovementConstruction(tempBuildLocation, constructionTile);
            constructionTile.transform.position = tempBuildLocation;
            //TerrainData td = world.GetTerrainDataAt(tempBuildLocation);
            constructionTile.BeginImprovementConstructionProcess(city, resourceProducer, tempBuildLocation, this, isHill);

            if (city.activeCity)
            {
                constructingTiles.Add(tempBuildLocation);
                ResetTileLists();
                CloseImprovementBuildPanel();
            }
        }
    }

    public void FinishImprovement(City city, ImprovementDataSO improvementData, Vector3Int tempBuildLocation)
    {
        //activating structure
        GameObject improvement = world.GetStructure(tempBuildLocation);
        //world.AddStructureMap(tempBuildLocation, improvementData.mapIcon);
        improvement.SetActive(true);
        TerrainData td = world.GetTerrainDataAt(tempBuildLocation);
        CityImprovement cityImprovement = world.GetCityDevelopment(tempBuildLocation);
        cityImprovement.SetMinimapIcon(td);
        cityImprovement.meshCity = city;
        cityImprovement.transform.parent = city.transform;
        city.AddToImprovementList(cityImprovement);
        cityImprovement.PlaySmokeSplash(td.terrainData.type == TerrainType.Hill);

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
            td.main.gameObject.SetActive(false);

            foreach (MeshFilter mesh in cityImprovement.MeshFilter)
            {
                if (mesh.name == "Ground")
                {
                    Vector2[] terrainUVs = td.UVs;
                    Vector2[] newUVs = mesh.mesh.uv;
                    Vector2[] finalUVs = world.NormalizeUVs(terrainUVs, newUVs);
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
        else
        {
            //for tweening
            Vector3 goScale = improvement.transform.localScale;
            improvement.transform.localScale = Vector3.zero;
            LeanTween.scale(improvement, goScale, 0.4f).setEase(LeanTweenType.easeOutBack).setOnComplete( () => { CombineMeshes(city, city.subTransform, upgradingImprovement); cityImprovement.SetInactive(); TileCheck(tempBuildLocation, city, improvementData.maxLabor); });
        }    
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

                    if (cityImprovement.skinnedMesh != null)
                    {
                        int j = 0;
                        Vector2[] skinnedUVs = cityImprovement.skinnedMesh.sharedMesh.uv;

                        while (j < skinnedUVs.Length)
                        {
                            skinnedUVs[j] = rockUVs;
                            j++;
                        }

                        cityImprovement.skinnedMesh.sharedMesh.uv = skinnedUVs;
                    }

                    break;
                }
            }

        }

        if (td.prop != null)
            td.prop.gameObject.SetActive(false);

        //if (improvementData.replaceTerrain)
        //{
        //    world.GetTerrainDataAt(tempBuildLocation).gameObject.SetActive(false);
        //}

        //setting harbor info
        if (improvementData.improvementName == "Harbor")
        {
            city.hasHarbor = true;
            city.harborLocation = tempBuildLocation;
            world.SetCityHarbor(city, tempBuildLocation);
            world.AddTradeLoc(tempBuildLocation, city.cityName);
        }

        //setting labor info (harbors have no labor)
        world.AddToMaxLaborDict(tempBuildLocation, improvementData.maxLabor);
        if (city.AutoAssignLabor && city.cityPop.UnusedLabor > 0 && improvementData.maxLabor > 0)
            city.AutoAssignmentsForLabor();

        //no tweening, so must be done here
        if (improvementData.replaceTerrain)
        {
            CombineMeshes(city, city.subTransform, upgradingImprovement); 
            cityImprovement.SetInactive();
            TileCheck(tempBuildLocation, city, improvementData.maxLabor);
        }
    }

    private void TileCheck(Vector3Int tempBuildLocation, City city, int maxLabor)
    {
        if (selectedCity != null && cityTiles.Contains(tempBuildLocation))
        {
            developedTiles.Add(tempBuildLocation);

            //in case improvement is made while another city is selected and the city that made it has auto assign on
            if (city != selectedCity)
            {
                (cityTiles, developedTiles, constructingTiles) = GetThisCityRadius();
                HideBorders();
                DrawBorders();
            }
            else
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
                    ImprovementTileHighlight();
                }
                else if (upgradingImprovement && uiImprovementBuildInfoPanel.activeStatus)
                {
                    UpgradeTileHighlight();
                }
                //PrepareLaborNumber(tempBuildLocation);

            }
        }
    }

    private int HarborRotation(Vector3Int tempBuildLocation, Vector3Int originationLocation)
    {
        int rotation = 0;
        int minimum = 99999;
        int rotationIndex = 0;

        foreach (Vector3Int neighbor in world.GetNeighborsFor(tempBuildLocation, MapWorld.State.FOURWAYINCREMENT))
        {
            if (!world.GetTerrainDataAt(neighbor).terrainData.sailable) //don't place harbor on neighboring water tiles
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

    private void RemoveImprovement(Vector3Int improvementLoc, CityImprovement selectedImprovement, City city, bool upgradingImprovement)
    {
        ImprovementDataSO improvementData = selectedImprovement.GetImprovementData;
        //selectedImprovement.DestroyPS();

        if (selectedImprovement.queued)
        {
            if(uiQueueManager.CheckIfBuiltItemIsQueued(improvementLoc, improvementLoc-city.cityLoc, true, improvementData, selectedImprovement.GetQueueCity()))
            {
                if (improvementLoc == city.cityLoc)
                    RemoveQueueGhostBuilding(improvementData.improvementName, city);
                else
                    RemoveQueueGhostImprovement(improvementLoc, city);
            }
        }

        //remove building
        if (improvementLoc == city.cityLoc)
        {
            RemoveBuilding(improvementData, city, upgradingImprovement);
            return;
        }
        else if (selectedImprovement == null) //in case the tile is selected, missing the box collider of development
        {
            if (world.CheckIfTileIsUnderConstruction(improvementLoc))
                selectedImprovement = world.GetCityDevelopmentConstruction(improvementLoc);
            else
                selectedImprovement = world.GetCityDevelopment(improvementLoc);
        }
        
        ResourceProducer resourceProducer = world.GetResourceProducer(improvementLoc);

        //if removing/canceling upgrade process, stop here
        if (selectedImprovement.isUpgrading)
        {
            ReplaceImprovementCost(selectedImprovement.UpgradeCost, improvementLoc);
            selectedImprovement.StopUpgradeProcess(resourceProducer);
            selectedImprovement.StopUpgrade();

            if (city.activeCity)
            {
                placesToWork++;
                UpdateCityLaborUIs();
                resourceManager.UpdateUI(selectedImprovement.UpgradeCost);
                uiResourceManager.SetCityCurrentStorage(city.ResourceManager.GetResourceStorageLevel);
                ImprovementTileHighlight();
            }

            return;
        }
        else if (!upgradingImprovement) //not redundant, for when actually removing
        {
            //putting the labor back
            //foreach (ResourceType resourceType in resourceProducer.producedResources)
            //{
            ResourceType resourceType = resourceProducer.producedResource.resourceType;

            for (int i = 0; i < world.GetCurrentLaborForTile(improvementLoc); i++)
            {
                city.ChangeResourcesWorked(resourceType, -1);

                int totalResourceLabor = city.GetResourcesWorkedResourceCount(resourceType);
                uiLaborHandler.PlusMinusOneLabor(resourceType, totalResourceLabor, -1, city.ResourceManager.GetResourceGenerationValues(resourceType));
                if (totalResourceLabor == 0)
                    city.RemoveFromResourcesWorked(resourceType);
            }
            //}

            resourceProducer.UpdateCurrentLaborData(0);
            resourceProducer.UpdateResourceGenerationData();
            resourceProducer.StopProducing();

            //replacing the cost
            ReplaceImprovementCost(improvementData.improvementCost, improvementLoc);

            if (city.activeCity)
            {
                resourceManager.UpdateUI(improvementData.improvementCost);
                uiResourceManager.SetCityCurrentStorage(city.ResourceManager.GetResourceStorageLevel);
            }

            if (improvementData.replaceTerrain)
            {
                world.GetTerrainDataAt(improvementLoc).main.gameObject.SetActive(true);
            }

            if (improvementData.returnProp)
            {
                TerrainData td = world.GetTerrainDataAt(improvementLoc);
                td.prop.gameObject.SetActive(true);
                if (td.terrainData.rawResourceType == RawResourceType.Stone)
                    td.RocksCheck();
            }

            if (improvementData.singleBuild)
            {
                city.singleBuildImprovementsBuildingsDict.Remove(improvementData.improvementName);
                world.RemoveSingleBuildFromCityLabor(improvementLoc);
            }

            if (city.activeCity && !selectedImprovement.isConstruction)
            {
                if (!world.CheckIfTileIsMaxxed(improvementLoc))
                    placesToWork--;
                int currentLabor = world.GetCurrentLaborForTile(improvementLoc);
                city.cityPop.UnusedLabor += currentLabor;
                city.cityPop.UsedLabor -= currentLabor;
            }
        }

        //updating city graphic
        if (!selectedImprovement.isConstruction)
        {
            if (selectedImprovement.meshCity != null)
            {
                selectedImprovement.meshCity.RemoveFromImprovementList(selectedImprovement);
                selectedImprovement.meshCity.RemoveFromMeshFilterList(false, improvementLoc);
                CombineMeshes(selectedImprovement.meshCity, selectedImprovement.meshCity.subTransform, upgradingImprovement);
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
            return; 

        if (city.activeCity && removingImprovement && uiImprovementBuildInfoPanel.activeStatus)
            ImprovementTileHighlight();

        //stop here if it in construction process
        if (selectedImprovement.isConstruction)
        {
            constructingTiles.Remove(improvementLoc);
            return;
        }

        if (improvementLoc == city.harborLocation)
        {
            city.hasHarbor = false;
            world.RemoveHarbor(improvementLoc);
            world.RemoveTradeLoc(improvementLoc);
        }

        //updating all the labor info
        city.UpdateCityPopInfo();

        if (city.AutoAssignLabor && city.cityPop.UnusedLabor > 0)
            city.AutoAssignmentsForLabor();

        if (city.activeCity && !upgradingImprovement)
        {
            uiInfoPanelCity.SetData(selectedCity.cityName, selectedCity.cityPop.CurrentPop, selectedCity.HousingCount, selectedCity.cityPop.UnusedLabor, selectedCity.workEthic,
                resourceManager.FoodGrowthLevel, resourceManager.FoodGrowthLimit, resourceManager.FoodPerMinute);
            UpdateLaborNumbers();
            uiLaborAssignment.UpdateUI(selectedCity, placesToWork);
        }
    }

    private void ReplaceImprovementCost(List<ResourceValue> replaceCost, Vector3 improvementLoc)
    {
        int i = 0;
        improvementLoc.y += replaceCost.Count * 0.4f; 

        foreach (ResourceValue resourceValue in replaceCost) //adding back 100% of cost (if there's room)
        {
            int resourcesReturned = resourceManager.CheckResource(resourceValue.resourceType, resourceValue.resourceAmount);
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
            removingImprovement = false;
            upgradingImprovement = false;
            ResetTileLists();
            ToggleBuildingHighlight(true);
            uiImprovementBuildInfoPanel.ToggleVisibility(false);
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
        uiCityTabs.HideSelectedTab();
        //uiLaborHandler.HideUI();
        //uiLaborHandler.ShowUI(selectedCity);
        //ResetTileLists();
        world.CloseImprovementTooltipButton();
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
            UpdateLaborNumbers();
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

    public void UpdateLaborNumbers()
    {
        if (isActive)
        {
            HideLaborNumbers();
            HideImprovementResources();
            foreach (Vector3Int tile in developedTiles)
            {
                //if (world.CheckIfTileIsImproved(tile))
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
            if (improvement.isUpgrading || improvement.GetImprovementData.improvementName == "Harbor")
                continue;

            if (laborChange > 0 && !world.CheckIfTileIsMaxxed(tile) && selectedCity.cityPop.UnusedLabor > 0) //for increasing labor, can't be maxxed out
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
        selectedCity.cityPop.UnusedLabor -= laborChange;
        selectedCity.cityPop.UsedLabor += laborChange;

        resourceProducer.UpdateCurrentLaborData(labor);
        //if (!resourceProducer.CheckResourceManager(resourceManager))
        //    resourceProducer.SetResourceManager(resourceManager);

        if (labor == 0) //removing from world dicts when zeroed out
        {
            RemoveLaborFromDicts(terrainLocation);
            resourceProducer.StopProducing();
        }
        else if (labor == 1 && laborChange > 0) //assigning city to location if working for first time
        {
            CityImprovement selectedImprovement = world.GetCityDevelopment(terrainLocation);
            //if mesh isn't owned by anyone, add to this city's
            if (selectedImprovement.meshCity == null)
            {
                selectedImprovement.meshCity = selectedCity;
                selectedImprovement.transform.parent = selectedCity.transform;
                selectedCity.SetNewMeshCity(terrainLocation, improvementMeshDict, improvementMeshList);
                CombineMeshes(selectedCity, selectedCity.subTransform, upgradingImprovement);
                selectedCity.AddToImprovementList(selectedImprovement);
            }

            if (world.GetCityDevelopment(terrainLocation).queued)
            {
                //CityImprovement selectedImprovement = world.GetCityDevelopment(terrainLocation);
                City tempCity = selectedImprovement.GetQueueCity();

                if (tempCity != selectedCity)
                {
                    tempCity.RemoveFromQueue(terrainLocation-tempCity.cityLoc);
                    selectedImprovement.SetQueueCity(null);
                }
            }

            world.AddToCityLabor(terrainLocation, selectedCity.gameObject);
            resourceProducer.SetResourceManager(selectedCity.ResourceManager);
            resourceProducer.StartProducing();
        }
        else if (labor > 1 && laborChange > 0)
        {
            resourceProducer.AddLaborMidProduction();
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

        uiLaborHandler.PlusMinusOneLabor(resourceType, totalResourceLabor, laborChange, selectedCity.ResourceManager.GetResourceGenerationValues(resourceType));
        uiLaborHandler.UpdateResourcesConsumed(resourceProducer.consumedResourceTypes, selectedCity.ResourceManager.ResourceConsumedPerMinuteDict);

        if (totalResourceLabor == 0)
            selectedCity.RemoveFromResourcesWorked(resourceType);
        //}

        //updating all the labor info
        selectedCity.UpdateCityPopInfo();

        uiInfoPanelCity.SetData(selectedCity.cityName, selectedCity.cityPop.CurrentPop, selectedCity.HousingCount, selectedCity.cityPop.UnusedLabor, selectedCity.workEthic, 
            resourceManager.FoodGrowthLevel, resourceManager.FoodGrowthLimit, resourceManager.FoodPerMinute);

        UpdateLaborNumbers();
        //resourceManager.UpdateUIGeneration(terrainSelected.GetTerrainData().resourceType);
        //BuildingButtonHighlight();
        LaborTileHighlight();
        uiLaborAssignment.UpdateUI(selectedCity, placesToWork);
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
        UpdateLaborNumbers();
        uiInfoPanelCity.SetData(selectedCity.cityName, selectedCity.cityPop.CurrentPop, selectedCity.HousingCount, selectedCity.cityPop.UnusedLabor, selectedCity.workEthic,
            resourceManager.FoodGrowthLevel, resourceManager.FoodGrowthLimit, resourceManager.FoodPerMinute);
        uiLaborAssignment.UpdateUI(selectedCity, placesToWork);
    }

    public void CloseCityTab()
    {
        uiCityTabs.HideSelectedTab();
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
        CloseImprovementBuildPanel();
        uiLaborAssignment.ResetLaborAssignment();
        CloseQueueUI();
        uiCityTabs.HideSelectedTab();
        CloseLaborMenus();
        //uiLaborHandler.HideUI();
        //ResetTileLists(); //already in improvement build panel
    }

    public void ToggleLaborHandlerMenu()
    {
        if (uiLaborHandler.activeStatus)
        {
            CloseLaborMenus();
        }
        else
        {
            CloseCityTab();
            world.CloseImprovementTooltipButton();
            CloseImprovementBuildPanel();
            CloseQueueUI();
            ResetTileLists();
            uiLaborHandler.ToggleVisibility(true);
        }
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
                ToggleBuildingHighlight(true);
            }
        }

        if (uiLaborPrioritizationManager.activeStatus)
            uiLaborPrioritizationManager.ToggleVisibility(false);
    }

    public void DestroyCityWarning()
    {
        uiDestroyCityWarning.ToggleVisibility(true);

        if (selectedCity != null)
            ResetCityUIToBase();
    }






    public void ToggleAutoAssign()
    {
        if (autoAssign.isOn)
        {
            CloseLaborMenus();
            //openAssignmentPriorityMenu.interactable = true;
            selectedCity.AutoAssignLabor = true;

            if (selectedCity.cityPop.UnusedLabor > 0)
            {
                selectedCity.AutoAssignmentsForLabor();
                UpdateCityLaborUIs();
            }

            uiLaborAssignment.SetAssignmentOptionsInteractableOff();
        }
        else
        {
            selectedCity.AutoAssignLabor = false;
            uiLaborAssignment.UpdateUI(selectedCity, placesToWork);
            uiLaborPrioritizationManager.ToggleVisibility(false);
        }
    }

    public void ClosePrioritizationManager()
    {
        //openAssignmentPriorityMenu.interactable = false;
        //selectedCity.AutoAssignLabor = false;
        uiLaborPrioritizationManager.ToggleVisibility(false);
    }

    public void TogglePrioritizationMenu()
    {
        if (!uiLaborPrioritizationManager.activeStatus)
        {
            CloseLaborMenus();
            world.CloseImprovementTooltipButton();
            CloseImprovementBuildPanel();
            uiCityTabs.HideSelectedTab();
            CloseQueueUI();
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
        uiUnitBuilder.isQueueing = v;
        uiImprovementBuilder.isQueueing = v;
        uiBuildingBuilder.isQueueing = v;
    }

    public void BuildQueuedBuilding(City city, ResourceManager resourceManager)
    {
        UIQueueItem queuedItem = city.GetBuildInfo();
        this.resourceManager = resourceManager;
        //selectedCity = city;
        //selectedCityLoc = city.cityLoc;
        if (queuedItem.upgrading)
        {
            Vector3Int tile = queuedItem.buildLoc + city.cityLoc;
            if (queuedItem.buildLoc.x == 0 && queuedItem.buildLoc.z == 0)
                UpgradeSelectedImprovementPrep(city.cityLoc, world.GetBuildingData(tile, queuedItem.buildingName), city);
            else
                UpgradeSelectedImprovementPrep(tile, world.GetCityDevelopment(tile), city);
        }
        else if (queuedItem.unitBuildData != null) //build unit
        {
            if (city.cityPop.CurrentPop == 0)
            {
                UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Not enough population to make unit");
                city.GoToNextItemInQueue();

                if (selectedCity != null && city == selectedCity)
                    uiQueueManager.CheckIfBuiltUnitIsQueued(queuedItem.unitBuildData, city.cityLoc);
                return;
            }
            CreateUnit(queuedItem.unitBuildData, city);
        }
        else if (queuedItem.buildLoc.x == 0 && queuedItem.buildLoc.z == 0) //build building
        {
            CreateBuilding(queuedItem.improvementData, city, false);
        }
        else //build improvement
        {
            Vector3Int tile = queuedItem.buildLoc + city.cityLoc;

            if (world.IsBuildLocationTaken(tile) || world.IsRoadOnTerrain(tile))
            {
                city.GoToNextItemInQueue();
                if (uiQueueManager.CheckIfBuiltItemIsQueued(tile, queuedItem.buildLoc, false, queuedItem.improvementData, city))
                    RemoveQueueGhostImprovement(tile, city);
                return;
            }
            BuildImprovement(queuedItem.improvementData, tile, city, false);
        }

        city.GoToNextItemInQueue();
    }

    public List<UIQueueItem> GetQueueItems()
    {
        return selectedCity.savedQueueItems;
    }

    public void CloseQueueUI()
    {
        if (uiQueueManager.activeStatus)
        {
            SetQueueStatus(false);
            uiQueueManager.UnselectQueueItem();
            SetCityQueueItems();
            uiQueueManager.ToggleVisibility(false);
        }
    }

    public void SetCityQueueItems()
    {
        if (uiQueueManager.activeStatus)
        {
            (selectedCity.savedQueueItems, selectedCity.savedQueueItemsNames) = uiQueueManager.SetQueueItems();

        }
    }






    public void RunCityNamerUI()
    {
        ResetCityUIToBase();
        ResetTileLists();
        uiCityNamer.ToggleVisibility(true, selectedCity);
    }

    public void DestroyCity() //set on destroy city warning message
    {
        if (selectedWonder != null)
        {
            CancelWonderConstruction();
            return;
        }

        //disassociating improvements from city
        foreach (CityImprovement improvement in selectedCity.ImprovementList)
        {
            improvement.meshCity = null;
            improvement.transform.parent = improvementHolder.transform;
        }

        selectedCity.ReassignMeshes(improvementHolder, improvementMeshDict, improvementMeshList);
        CombineMeshes();

        //destroying queued objects
        foreach(UIQueueItem queueItem in selectedCity.savedQueueItems)
        {
            if (queueItem.buildLoc.x == 0 && queueItem.buildLoc.z == 0)
                RemoveQueueGhostBuilding(queueItem.buildingName, selectedCity);
            else
                RemoveQueueGhostImprovement(queueItem.buildLoc + selectedCityLoc, selectedCity);

            world.RemoveLocationFromQueueList(queueItem.buildLoc + selectedCityLoc);
        }

        GameObject destroyedCity = world.GetStructure(selectedCityLoc);

        //destroy all construction projects upon destroying city
        List<Vector3Int> constructionToStop = new(constructingTiles);

        foreach (Vector3Int constructionTile in constructionToStop)
        {
            CityImprovement construction = world.GetCityDevelopmentConstruction(constructionTile);
            construction.RemoveConstruction(this, constructionTile);
            RemoveImprovement(constructionTile, construction, selectedCity, false);
        }

        world.RemoveStructure(selectedCityLoc);
        //world.RemoveStructureMap(selectedCityLoc);
        //world.ResetTileMap(selectedCityLoc);
        world.RemoveCityName(selectedCityLoc);
        world.RemoveTradeLoc(selectedCityLoc);
        selectedCity.DestroyMapText();

        selectedCity.DestroyThisCity();

        //for all single build improvements, finding a nearby city to join that doesn't have one. If not one available, then is unowned. 
        foreach (string singleImprovement in selectedCity.singleBuildImprovementsBuildingsDict.Keys)
        {
            Vector3Int improvementLoc = selectedCity.singleBuildImprovementsBuildingsDict[singleImprovement];
            if (improvementLoc == selectedCityLoc)
                continue;

            world.RemoveSingleBuildFromCityLabor(improvementLoc);
            bool unclaimed = true;

            foreach (Vector3Int tile in world.GetNeighborsFor(improvementLoc, MapWorld.State.CITYRADIUS))
            {
                if (world.IsCityOnTile(tile))
                {
                    City tempCity = world.GetCity(tile);
                    if (!tempCity.singleBuildImprovementsBuildingsDict.ContainsKey(singleImprovement))
                    {
                        tempCity.singleBuildImprovementsBuildingsDict[singleImprovement] = improvementLoc;
                        world.AddToCityLabor(improvementLoc, tempCity.gameObject);

                        if (singleImprovement == "Harbor") //is also done in City object
                        {
                            world.RemoveHarbor(improvementLoc);
                            tempCity.hasHarbor = true;
                            tempCity.harborLocation = improvementLoc;
                            world.SetCityHarbor(tempCity, improvementLoc);
                        }

                        unclaimed = false;
                        break;
                    }
                }
            }

            if (unclaimed)
            {
                if (singleImprovement == "Harbor")
                    world.RemoveTradeLoc(improvementLoc);

                world.AddToUnclaimedSingleBuild(improvementLoc);
            }
        }
        Destroy(destroyedCity);

        uiDestroyCityWarning.ToggleVisibility(false);

        ResetCityUI();
    }

    public void NoDestroyCity() //in case user chickens out
    {
        uiDestroyCityWarning.ToggleVisibility(false);
        if (selectedCity != null)
            uiCityTabs.HideSelectedTab();
    }

    public void ResetTileLists()
    {
        foreach (Vector3 location in resourceInfoHolderDict.Keys)
            PoolResourceInfoPanel(location);
        resourceInfoHolderDict.Clear();
        resourceInfoPanelDict.Clear();

        foreach (Vector3Int tile in tilesToChange)
        {
            world.GetTerrainDataAt(tile).DisableHighlight();
            if (world.CheckIfTileIsImproved(tile))
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
            world.somethingSelected = false;
            ResourceProducerTimeProgressBarsSetActive(false);
            isActive = false;
            cityTiles.Clear();
            developedTiles.Clear();
            constructingTiles.Clear();
            //ResetTileLists();
            removingImprovement = false;
            //removingBuilding = false;
            ResetCityUIToBase();
            ResetTileLists();
            uiCityTabs.ToggleVisibility(false);
            uiResourceManager.ToggleVisibility(false);
            uiInfoPanelCity.ToggleVisibility(false);
            uiLaborAssignment.HideUI();
            if (uiLaborPrioritizationManager.activeStatus)
                uiLaborPrioritizationManager.ToggleVisibility(false, true);
            uiUnitTurn.buttonClicked.RemoveListener(ResetCityUI);
            HideLaborNumbers();
            HideImprovementResources();
            uiLaborHandler.ResetUI();
            HideBorders();
            //CloseResourceGrid();
            ToggleBuildingHighlight(false);
            //ToggleCityHighlight(false);
            //selectedCity.Deselect();
            selectedCity.HideCityGrowthProgressTimeBar();
            selectedCityLoc = new();
            selectedCity.activeCity = false;
            selectedCity = null;
        }
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
            selectedWonder.SetUI(null);
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
            upgradingImprovement = upgrade;
            //ToggleBuildingHighlight(true);
            if (removingImprovement)
                ImprovementTileHighlight();
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
    private void GrowConstructionTilePool()
    {
        for (int i = 0; i < 2; i++) //grow pool 2 at a time
        {
            GameObject constructionTileGO = Instantiate(constructionTilePrefab);
            constructionTileGO.gameObject.transform.SetParent(objectPoolHolder, false);
            CityImprovement constructionImprovement = constructionTileGO.GetComponent<CityImprovement>();
            constructionImprovement.isConstruction = true;
            AddToConstructionTilePool(constructionImprovement);
        }
    }

    public void AddToConstructionTilePool(CityImprovement constructionTile)
    {
        constructionTile.gameObject.SetActive(false);
        constructionTileQueue.Enqueue(constructionTile);
    }

    public CityImprovement GetFromConstructionTilePool()
    {
        if (constructionTileQueue.Count == 0)
            GrowConstructionTilePool();

        var constructionTile = constructionTileQueue.Dequeue();
        constructionTile.gameObject.SetActive(true);
        return constructionTile;
    }

    //object pooling the resource info holders
    private void GrowResourceInfoHolderPool()
    {
        for (int i = 0; i < 10; i++) //grow pool 20 at a time
        {
            GameObject resourceInfoHolderGO = Instantiate(GameAssets.Instance.resourceInfoHolder);
            resourceInfoHolderGO.gameObject.transform.SetParent(objectPoolHolder, false);
            ResourceInfoHolder resourceInfoHolder = resourceInfoHolderGO.GetComponent<ResourceInfoHolder>();
            //resourceInfoPanel.transform.rotation = Quaternion.Euler(90, 0, 0); //rotating to lie flat on tile
            AddToResourceInfoHolderPool(resourceInfoHolder);
        }
    }

    private void AddToResourceInfoHolderPool(ResourceInfoHolder resourceInfoHolder)
    {
        resourceInfoHolder.gameObject.SetActive(false);
        resourceInfoHolderQueue.Enqueue(resourceInfoHolder);
    }

    private ResourceInfoHolder GetFromResourceInfoHolderPool()
    {
        if (resourceInfoHolderQueue.Count == 0)
            GrowResourceInfoHolderPool();

        var resourceInfoHolder = resourceInfoHolderQueue.Dequeue();
        resourceInfoHolder.gameObject.SetActive(true);
        return resourceInfoHolder;
    } 

    //object pooling the resource info panels
    private void GrowResourceInfoPanelPool()
    {
        for (int i = 0; i < 20; i++) //grow pool 20 at a time
        {
            GameObject resourceInfoPanelGO = Instantiate(GameAssets.Instance.resourceInfoPanel);
            resourceInfoPanelGO.gameObject.transform.SetParent(objectPoolHolder, false);
            ResourceInfoPanel resourceInfoPanel = resourceInfoPanelGO.GetComponent<ResourceInfoPanel>();
            //resourceInfoPanel.transform.rotation = Quaternion.Euler(90, 0, 0); //rotating to lie flat on tile
            AddToResourceInfoPanelPool(resourceInfoPanel);
        }
    }

    private void AddToResourceInfoPanelPool(ResourceInfoPanel resourceInfoPanel)
    {
        resourceInfoPanel.gameObject.SetActive(false);
        resourceInfoPanelQueue.Enqueue(resourceInfoPanel);
    }

    private ResourceInfoPanel GetFromResourceInfoPanelPool()
    {
        if (resourceInfoPanelQueue.Count == 0)
            GrowResourceInfoPanelPool();

        var resourceInfoPanel = resourceInfoPanelQueue.Dequeue();
        resourceInfoPanel.gameObject.SetActive(true);
        return resourceInfoPanel;
    }


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
