using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.FilePathAttribute;

public class CityBuilderManager : MonoBehaviour
{
    [SerializeField]
    private UICityBuildTabHandler uiCityTabs;
    [SerializeField]
    private UIBuilderHandler uiUnitBuilder;
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
    //[SerializeField]
    //private UIInfoPanelCityWarehouse uiInfoPanelCityWarehouse;
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
    public Button abandonCityButton;
    [SerializeField]
    private UIDestroyCityWarning uiDestroyCityWarning;

    //for labor prioritization auto-assignment menu
    [SerializeField]
    private Toggle autoAssign;
    [SerializeField]
    private Button openAssignmentPriorityMenu;
    [SerializeField]
    private UICityLaborPrioritizationManager uiLaborPrioritizationManager;

    [SerializeField]
    private MapWorld world;

    [SerializeField]
    private CameraController focusCam;
    private Quaternion originalRotation;

    private City selectedCity;
    private Vector3Int selectedCityLoc;

    private List<Vector3Int> tilesToChange = new();
    private List<Vector3Int> cityTiles = new();
    private List<Vector3Int> developedTiles = new();
    [HideInInspector]
    public List<Vector3Int> constructingTiles = new();

    private ImprovementDataSO improvementData;
    private List<ResourceValue> upgradeCost = new();

    [HideInInspector]
    public ResourceManager resourceManager;

    private int laborChange;
    //public int LaborChange { set { laborChange = value; } }
    private int placesToWork;

    //for object pooling of labor numbers
    private Queue<CityLaborTileNumber> laborNumberQueue = new(); //the pool in object pooling
    private List<CityLaborTileNumber> laborNumberList = new(); //to add to in order to add back into pool

    //for object pooling of city borders
    private Queue<GameObject> borderQueue = new();
    private List<GameObject> borderList = new();

    //for object pooling of construction graphics
    [SerializeField]
    private GameObject constructionTile;
    private Queue<CityImprovement> constructionTileQueue = new();

    //for object pooling of resource info panels
    private Queue<ResourceInfoPanel> resourceInfoPanelQueue = new();
    private Dictionary<Vector3, List<ResourceInfoPanel>> resourceInfoPanelDict = new();

    //for naming of units (need to store these to save)
    private int workerCount;
    private int infantryCount;

    private bool removingImprovement, upgradingImprovement, isQueueing; //flags thrown when doing specific tasks
    private bool isActive; //when looking at a city

    private void Awake()
    {
        GrowLaborNumbersPool();
        GrowBordersPool();
        GrowConstructionTilePool(); 
        GrowResourceInfoPanelPool();
        //openAssignmentPriorityMenu.interactable = false;
    }

    private void PopulateUpgradeDictForTesting()
    {
        //here just for testing
        world.SetUpgradeableObjectMaxLevel("Research", 2);
        world.SetUpgradeableObjectMaxLevel("Mine", 2);
        world.SetUpgradeableObjectMaxLevel("Monument", 2);
    }

    private void CenterCamOnCity()
    {
        if (selectedCity != null)
            focusCam.CenterCameraNoFollow(selectedCity.transform.position);
    }

    private void CameraBirdsEyeRotation()
    {
        originalRotation = focusCam.transform.rotation;
        focusCam.centerTransform = selectedCity.transform;
    }

    private void CameraDefaultRotation()
    {
        focusCam.centerTransform = null;
        focusCam.transform.rotation = Quaternion.Lerp(focusCam.transform.rotation, originalRotation, Time.deltaTime * 5);
        focusCam.SetZoom(new Vector3(0, 7.5f, -3.5f));
        //focusCam.cameraTransform.localPosition += new Vector3(0, -1f, 1f);
    }


    public void HandleCitySelection(Vector3 location, GameObject selectedObject)
    {
        //if (selectedCity != null)
            //ResetCityUI();

        if (selectedObject == null)
            return;

        if (selectedObject.CompareTag("Player") && selectedObject.TryGetComponent(out City cityReference))
        {
            SelectCity(location, cityReference);
        }
        //selecting improvements to remove or add/remove labor
        else if (selectedObject.TryGetComponent(out CityImprovement improvementSelected))
        {
            City city = improvementSelected.GetCity();
            if (!removingImprovement && !upgradingImprovement && city != null)
            {
                SelectCity(location, city);
                return;
            }
            
            if (selectedCity != null)
            {
                //Vector3Int terrainLocation = terrainSelected.GetTileCoordinates();
                Vector3Int terrainLocation = world.GetClosestTerrainLoc(location);
                TerrainData terrainSelected = world.GetTerrainDataAt(terrainLocation);

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
                    return;
                }

                if (upgradingImprovement)
                {
                    if (tilesToChange.Contains(terrainLocation) || terrainLocation == selectedCityLoc)
                        UpgradeSelectedImprovementQueueCheck(terrainLocation, improvementSelected);
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

                    RemoveImprovement(terrainLocation, improvementSelected, selectedCity, false);
                }
                else if (laborChange != 0) //for changing labor counts in tile
                {
                    if (constructingTiles.Contains(terrainLocation))
                    {
                        GiveWarningMessage("Still building...");
                        return;
                    }
                    
                    ChangeLaborCount(terrainSelected, terrainLocation);
                }
            }
        }
        //selecting tiles to place improvements
        else if (selectedCity != null && selectedObject.TryGetComponent(out TerrainData terrainSelected))
        {
            Vector3Int terrainLocation = terrainSelected.GetTileCoordinates();

            //deselecting if choosing tile outside of city
            if (!cityTiles.Contains(terrainLocation))
            {
                ResetCityUI();
                return;
            }

            if (!tilesToChange.Contains(terrainLocation))
            {
                if (laborChange != 0)
                {
                    GiveWarningMessage("Nothing here");
                }
                else if (removingImprovement)
                {
                    GiveWarningMessage("Um, there's nothing here...");
                }
                else if (upgradingImprovement)
                {
                    GiveWarningMessage("Nothing here to upgrade");
                }
                else if (improvementData != null)
                {
                    if (world.CheckIfTileIsImproved(terrainLocation))
                        GiveWarningMessage("Already something here");
                    else
                        GiveWarningMessage("Not a good spot");
                }
            }
            else 
            {
                if (improvementData != null)
                {
                    BuildImprovementQueueCheck(improvementData, terrainLocation); //passing the data here as method requires it
                }
                else if (upgradingImprovement)
                {
                    UpgradeSelectedImprovementQueueCheck(terrainLocation, improvementSelected);
                }
                else if (removingImprovement)
                {
                    if (world.CheckIfTileIsUnderConstruction(terrainLocation))
                    {
                        world.GetCityDevelopmentConstruction(terrainLocation).RemoveConstruction(this, terrainLocation);
                    }

                    RemoveImprovement(terrainLocation, improvementSelected, selectedCity, false);
                }
                else if (laborChange != 0)
                {
                    if (constructingTiles.Contains(terrainLocation))
                    {
                        GiveWarningMessage("Still building...");
                        return;
                    }

                    ChangeLaborCount(terrainSelected, terrainLocation);
                }
            }
        }
        else
        {
            ResetCityUI();
        }
    }

    private void SelectCity(Vector3 location, City cityReference)
    {
        if (selectedCity != null)
        {
            if (selectedCity != cityReference)
            {
                ResetCityUI();
            }
            else //deselect if same city selected
            {
                ResetCityUI();
                return;
            }
        }

        PopulateUpgradeDictForTesting();

        selectedCity = cityReference;

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
        resourceManager.SetUI(uiResourceManager, uiInfoPanelCity);
        uiResourceManager.SetCityInfo(selectedCity.CityName, selectedCity.warehouseStorageLimit, selectedCity.ResourceManager.GetResourceStorageLevel);
        resourceManager.UpdateUI();
        uiCityTabs.ToggleVisibility(true, resourceManager);
        uiResourceManager.ToggleVisibility(true);
        CenterCamOnCity();
        uiInfoPanelCity.SetData(selectedCity.CityName, selectedCity.cityPop.CurrentPop, selectedCity.HousingCount, selectedCity.cityPop.UnusedLabor, selectedCity.GetSetWorkEthic,
            resourceManager.FoodGrowthLevel, resourceManager.FoodGrowthLimit, resourceManager.FoodPerMinute);
        uiInfoPanelCity.ToggleVisibility(true);
        uiLaborAssignment.ShowUI(selectedCity, placesToWork);
        uiLaborHandler.PrepUI(selectedCity);
        uiUnitTurn.buttonClicked.AddListener(ResetCityUI);
        if (selectedCity.cityPop.CurrentPop > 0)
            abandonCityButton.interactable = false;
        else
            abandonCityButton.interactable = true;
        UpdateLaborNumbers();
        selectedCity.CityGrowthProgressBarSetActive(true);
        selectedCity.Select();
    }

    //for deselecting city after clicking off of it
    //public void HandleCityDeselect(Vector3 location, GameObject detectedObject)
    //{
    //    if (selectedCity == null)
    //        return;
        
    //    location.y = 0f;
        
    //    if (cityTiles.Contains(world.GetClosestTerrainLoc(location))) //to not deselect city when working within city
    //        return;

    //    ResetCityUI();
    //}

    //public void HandleCityTileSelection(Vector3 location, GameObject detectedObject)
    //{
    //    if (improvementData == null && laborChange == 0 && !removingImprovement)
    //        return;

    //    //TerrainData terrainSelected = detectedObject.GetComponent<TerrainData>();

    //    Vector3Int terrainLocation = world.GetClosestTerrainLoc(location);
    //    TerrainData terrainSelected = world.GetTerrainData(terrainLocation);
    //    //Vector3Int terrainLocation = terrainSelected.GetTileCoordinates();

    //    if (!tilesToChange.Contains(terrainLocation))
    //    {
    //        Debug.Log("Not suitable location");
    //        return;
    //    }

    //    if (removingImprovement)
    //    {
    //        RemoveImprovement(terrainSelected);
    //        return;
    //    }

    //    if (world.CheckIfTileIsImproved(terrainLocation)) //for changing labor counts in tile
    //    {
    //        if (improvementData != null)
    //        {
    //            Debug.Log("Already developed");
    //            return;
    //        }

    //        ChangeLaborCount(terrainSelected, terrainLocation);
    //    }
    //    else
    //    {

    //        if (laborChange != 0)
    //        {
    //            Debug.Log("Needs to be developed to work");
    //            return;
    //        }

    //        BuildImprovementQueueCheck(improvementData, terrainLocation); //for building improvement
    //    }
    //}

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
        uiImprovementBuildInfoPanel.ToggleVisibility(true);

        foreach (Vector3Int tile in developedTiles)
        {
            TerrainData td = world.GetTerrainDataAt(tile);
            td.DisableHighlight(); //turning off all highlights first
            PoolResourceInfoPanel(tile);
            resourceInfoPanelDict.Remove(tile);

            CityImprovement improvement = world.GetCityDevelopment(tile); //cached for speed
            improvement.DisableHighlight();

            if (improvement.GetImprovementData.improvementLevel < world.GetUpgradeableObjectMaxLevel(improvement.GetImprovementData.improvementName))
            {
                td.EnableHighlight(Color.green);
                improvement.EnableHighlight(Color.green);
                tilesToChange.Add(tile);

                SetUpResourceInfoPanel(improvement, tile);
            }
        }
    }

    public void UpgradeSelectedImprovementQueueCheck(Vector3Int tempBuildLocation, CityImprovement selectedImprovement)
    {
        string nameAndLevel = selectedImprovement.GetImprovementData.improvementName + '-' + selectedImprovement.GetImprovementData.improvementLevel;

        //queue information
        if (isQueueing)
        {
            selectedImprovement.queued = true;
            selectedImprovement.SetQueueCity(selectedCity);
            uiQueueManager.AddToQueue(tempBuildLocation - selectedCityLoc, world.GetUpgradeData(nameAndLevel), null, world.GetUpgradeCost(nameAndLevel));
            return;
        }

        foreach (ResourceValue value in world.GetUpgradeCost(nameAndLevel))
        {
            if (!selectedCity.ResourceManager.CheckResourceAvailability(value))
            {
                InfoPopUpHandler.Create(tempBuildLocation, "Can't Afford");
                return;
            }
        }

        UpgradeSelectedImprovement(tempBuildLocation, selectedImprovement, selectedCity);
    }

    public void UpgradeSelectedImprovement(Vector3Int upgradeLoc, CityImprovement selectedImprovement, City city)
    {
        //if (selectedImprovement.queued)
        //{
        //    if (selectedImprovement.GetQueueCity() != city)
        //        selectedImprovement.GetQueueCity().RemoveFromQueue(upgradeLoc, selectedImprovement.GetImprovementData);

        //    if (selectedImprovement.GetQueueCity() == selectedCity)
        //        uiQueueManager.CheckIfBuiltItemIsQueued(upgradeLoc - city.cityLoc, true, selectedImprovement.GetImprovementData, city);
        //}

        if (city == selectedCity)
        {
            removingImprovement = true;
            RemoveImprovement(upgradeLoc, selectedImprovement, city, true);
            removingImprovement = false;
        }
        else
        {
            RemoveImprovement(upgradeLoc, selectedImprovement, city, true);
        }

        string nameAndLevel = selectedImprovement.GetImprovementData.improvementName + '-' + selectedImprovement.GetImprovementData.improvementLevel;
        upgradeCost = new(world.GetUpgradeCost(nameAndLevel));
        BuildImprovement(world.GetUpgradeData(nameAndLevel), upgradeLoc, city, true);

        if (upgradeLoc == selectedCityLoc)
        {
            PoolResourceInfoPanel(selectedImprovement.GetImprovementData.buildingLocation);
            resourceInfoPanelDict.Remove(selectedImprovement.GetImprovementData.buildingLocation); //can't be in previous method
        }
        else
        {
            PoolResourceInfoPanel(upgradeLoc);
            resourceInfoPanelDict.Remove(upgradeLoc);
        }
    }

    private void SetUpResourceInfoPanel(CityImprovement improvement, Vector3 tile, bool building = false)
    {
        //setting up the resourceInfoPanels to appear below improvement
        List<ResourceInfoPanel> resourceInfoPanelList = new();
        List<ResourceValue> upgradeCost = world.GetUpgradeCost(improvement.GetImprovementData.improvementName + '-' + improvement.GetImprovementData.improvementLevel);
        int resourceCount = upgradeCost.Count;
        int i = 0;
        foreach (ResourceValue value in upgradeCost)
        {
            Vector3 panelPos = tile;
            panelPos.z -= 1.5f;
            if (building)
                panelPos.z += 1;
            panelPos.x -= .32f * (resourceCount - 1);
            panelPos.x += .64f * i;

            ResourceInfoPanel resourceInfoPanel = GetFromResourceInfoPanelPool();
            bool haveEnough = selectedCity.ResourceManager.CheckResourceAvailability(value);
            resourceInfoPanel.SetResourcePanel(world.GetResourceIcon(value.resourceType), value.resourceAmount, haveEnough);
            resourceInfoPanel.transform.position = panelPos;
            resourceInfoPanelList.Add(resourceInfoPanel);
            i++;
        }

        resourceInfoPanelDict[tile] = resourceInfoPanelList;
    }

    private void PoolResourceInfoPanel(Vector3 location)
    {
        if (!resourceInfoPanelDict.ContainsKey(location))
            return;
        
        foreach (ResourceInfoPanel resourceInfoPanel in resourceInfoPanelDict[location])
            AddToResourceInfoPanelPool(resourceInfoPanel);
    }

    private void ToggleBuildingHighlight(bool v)
    {
        //foreach (string name in world.GetBuildingListForCity(selectedCityLoc))
        //{
        //    world.GetBuildingData(selectedCityLoc, name).DisableHighlight2();
        //    world.GetBuildingData(selectedCityLoc, name).DisableHighlight();
        //}

        if (v)
        {
            foreach (string name in world.GetBuildingListForCity(selectedCityLoc))
            {
                CityImprovement building = world.GetBuildingData(selectedCityLoc, name);
                building.DisableHighlight();
                building.DisableHighlight2();

                if (removingImprovement)
                {
                    //don't highlight the buildings that can't be removed
                    if (building.initialCityHouse)
                        building.EnableHighlight(Color.white);
                    else
                        building.EnableHighlight2(Color.red);
                }
                else if (upgradingImprovement)
                {
                    if (building.GetImprovementData.improvementLevel < world.GetUpgradeableObjectMaxLevel(building.GetImprovementData.improvementName))
                    {
                        building.EnableHighlight2(Color.green);

                        SetUpResourceInfoPanel(building, building.GetImprovementData.buildingLocation, true);
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
                building.DisableHighlight();
                building.DisableHighlight2();
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
                producer.SetTimeProgressBarToZero();
            }
            else if (!producer.isWaitingToStart)
                producer.TimeProgressBarSetActive(v);
        }

        foreach (Vector3Int tile in constructingTiles)
        {
            //int time = world.GetCityDevelopmentConstruction(tile).ConstructionTime;
            world.GetResourceProducer(tile).TimeConstructionProgressBarSetActive(v, world.GetCityDevelopmentConstruction(tile).GetTimePassed);
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
            uiQueueManager.AddToQueue(new Vector3Int(0,0,0), null, unitData);
            return;
        }

        CreateUnit(unitData, selectedCity);
    }

    private void CreateUnit(UnitBuildDataSO unitData, City city)
    {
        uiQueueManager.CheckIfBuiltUnitIsQueued(unitData);
        city.PopulationDeclineCheck(); //decrease population before creating unit so we can see where labor will be lost
        //CheckForWork(); //not necessary

        resourceManager.SpendResource(unitData.unitCost);

        //updating uis after losing pop
        if (selectedCity != null && selectedCity == city)
        {
            UpdateLaborNumbers();
            uiLaborAssignment.UpdateUI(city, placesToWork);
            uiInfoPanelCity.SetData(selectedCity.CityName, selectedCity.cityPop.CurrentPop, selectedCity.HousingCount, selectedCity.cityPop.UnusedLabor, selectedCity.GetSetWorkEthic,
                resourceManager.FoodGrowthLevel, resourceManager.FoodGrowthLimit, resourceManager.FoodPerMinute);
            resourceManager.UpdateUI();
            uiResourceManager.SetCityCurrentStorage(city.ResourceManager.GetResourceStorageLevel);
            uiCityTabs.HideSelectedTab();
        }

        //uiInfoPanelCityWarehouse.SetWarehouseStorageLevel(selectedCity.ResourceManager.GetResourceStorageLevel);
        //Debug.Log("building " + unitData.name + " at " + selectedCityLoc);

        GameObject unitGO = unitData.prefab;

        if (unitData.unitType == UnitType.Worker)
        {
            workerCount++;
            unitGO.name = unitData.name.Split("_")[0] + "_" + workerCount;
        }
        if (unitData.unitType == UnitType.Infantry)
        {
            infantryCount++;
            unitGO.name = unitData.name.Split("_")[0] + "_" + infantryCount;
        }

        Vector3Int buildPosition = selectedCityLoc;
        if (world.IsUnitLocationTaken(buildPosition)) //placing unit in world after building in city
        {
            //List<Vector3Int> newPositions = world.GetNeighborsFor(Vector3Int.FloorToInt(buildPosition));
            foreach (Vector3Int pos in world.GetNeighborsFor(buildPosition, MapWorld.State.EIGHTWAYTWODEEP))
            {
                if (!world.IsUnitLocationTaken(pos) && world.GetTerrainDataAt(pos).GetTerrainData().walkable)
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
        unit.name = unit.name.Replace("(Clone)", ""); //getting rid of the clone part in name 
        Unit newUnit = unit.GetComponent<Unit>();

        newUnit.CurrentLocation = world.AddUnitPosition(buildPosition, newUnit);
    }

    public void CreateBuildingQueueCheck(ImprovementDataSO buildingData)
    {
        //Queue info
        if (isQueueing)
        {
            uiQueueManager.AddToQueue(new Vector3Int(0,0,0), buildingData);
            return;
        }

        CreateBuilding(buildingData, selectedCity, false);
    }

    private void CreateBuilding(ImprovementDataSO buildingData, City city, bool upgradingImprovement)
    {
        uiQueueManager.CheckIfBuiltItemIsQueued(new Vector3Int(0, 0, 0), false, buildingData, city);

        laborChange = 0;

        //setting building locations
        Vector3 cityPos = city.transform.position;
        Vector3 buildingLocalPos = buildingData.buildingLocation; //putting the building in it's position in the city square
        cityPos += buildingLocalPos;

        //resource info
        if (upgradingImprovement)
        {
            resourceManager.SpendResource(upgradeCost);
            upgradeCost.Clear();
        }
        else
        {
            resourceManager.SpendResource(buildingData.improvementCost); //this spends the resources needed to build
        }
        //uiInfoPanelCityWarehouse.SetWarehouseStorageLevel(selectedCity.ResourceManager.GetResourceStorageLevel);
        //Debug.Log("Placing structure in " + selectedCity.CityName);

        //setting world data
        GameObject building = Instantiate(buildingData.prefab, cityPos, Quaternion.identity);
        string buildingName = buildingData.improvementName;
        world.SetCityBuilding(buildingData, city.cityLoc, building, city, false);
        world.AddToCityMaxLaborDict(city.cityLoc, buildingName, buildingData.maxLabor);

        if (buildingData.singleBuild)
        {
            city.singleBuildImprovementsBuildingsDict[buildingData.improvementName] = selectedCityLoc;
        }

        //ResourceProducer resourceProducer = building.GetComponent<ResourceProducer>();
        //if (resourceProducer != null) //not all buildings will generate resources 
        //{
        //    resourceProducer.SetResourceManager(resourceManager); //need to set resourceManager for each new resource producer. 
        //    resourceProducer.InitializeImprovementData(buildingData); //allows the new structure to also start generating resources
        //    resourceProducer.SetLocation(selectedCityLoc);
        //    world.AddToCityBuildingIsProducerDict(selectedCityLoc, buildingName, resourceProducer);
        //}

        WorkEthicHandler workEthicHandler = building.GetComponent<WorkEthicHandler>();
        if (workEthicHandler != null) //not all buildings will have work ethic changes
        {
            workEthicHandler.InitializeImprovementData(buildingData);
            workEthicHandler.GetWorkEthicChange(1);
        }

        //updating uis
        if (selectedCity != null && city == selectedCity)
        {
            resourceManager.UpdateUI();
            uiResourceManager.SetCityCurrentStorage(city.ResourceManager.GetResourceStorageLevel);

            //setting labor data (no labor for buildings)
            //if (buildingData.maxLabor > 0)
            //{
            //    city.PlacesToWork++;
            //    uiLaborAssignment.UpdateUI(city.cityPop, city.PlacesToWork);
            //}

            uiCityTabs.HideSelectedTab();
        }
    }

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

    private void RemoveBuilding(string selectedBuilding)
    {
        //putting the resources and labor back
        GameObject building = world.GetBuilding(selectedCityLoc, selectedBuilding);
        WorkEthicHandler workEthicHandler = building.GetComponent<WorkEthicHandler>();

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
        int i = 0;
        foreach (ResourceValue resourceValue in workEthicHandler.GetImprovementData.improvementCost)
        {
            int resourcesReturned = resourceManager.CheckResource(resourceValue.resourceType, resourceValue.resourceAmount);
            Vector3 cityLoc = selectedCityLoc;
            cityLoc.z += -.5f * i;
            InfoResourcePopUpHandler.CreateResourceStat(cityLoc, resourcesReturned, ResourceHolder.Instance.GetIcon(resourceValue.resourceType));
            i++;
        }
        //}

        if (workEthicHandler != null)
        {
            UpdateCityWorkEthic(workEthicHandler.GetWorkEthicChange(0)); //zero out labor
        }

        if (selectedCity.singleBuildImprovementsBuildingsDict.ContainsKey(selectedBuilding))
            selectedCity.singleBuildImprovementsBuildingsDict.Remove(selectedBuilding);

        resourceManager.UpdateUI();
        uiResourceManager.SetCityCurrentStorage(selectedCity.ResourceManager.GetResourceStorageLevel);
        //uiInfoPanelCityWarehouse.SetWarehouseStorageLevel(selectedCity.ResourceManager.GetResourceStorageLevel);

        //int currentLabor = world.GetCurrentLaborForBuilding(selectedCityLoc, selectedBuilding);
        //int maxLabor = world.GetMaxLaborForBuilding(selectedCityLoc, selectedBuilding);
        //selectedCity.cityPop.UnusedLabor += currentLabor;
        //selectedCity.cityPop.UsedLabor -= currentLabor;

        Destroy(building);

        //updating world dicts
        world.RemoveCityBuilding(selectedCityLoc, selectedBuilding);

        //updating all the labor info
        //selectedCity.UpdateCityPopInfo();

        uiInfoPanelCity.SetData(selectedCity.CityName, selectedCity.cityPop.CurrentPop, selectedCity.HousingCount, selectedCity.cityPop.UnusedLabor, selectedCity.GetSetWorkEthic, 
            resourceManager.FoodGrowthLevel, resourceManager.FoodGrowthLimit, resourceManager.FoodPerMinute);

        //RemoveLaborFromBuildingDicts(selectedBuilding);
        //resourceManager.UpdateUIGenerationAll();
        //UpdateLaborNumbers();

        //this object maintenance
        //if (currentLabor < maxLabor)
        //    placesToWork--;
        //uiImprovementBuilder.ToggleVisibility(false);
        uiCityTabs.HideSelectedTab();
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
        if (!removingImprovement)
            CameraBirdsEyeRotation();

        tilesToChange.Clear();

        if (removingImprovement)
        {
            if (!upgradingImprovement) //only show when not upgrading
            {
                uiImprovementBuildInfoPanel.SetText("Removing Building");
                uiImprovementBuildInfoPanel.ToggleVisibility(true);
            }
        }
        else
        {
            uiImprovementBuildInfoPanel.SetText("Building " + improvementData.improvementName);
            uiImprovementBuildInfoPanel.SetImage(improvementData.image);
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
                    CityImprovement improvement = world.GetCityDevelopmentConstruction(tile);
                    improvement.DisableHighlight();

                    improvement.EnableHighlight(Color.red);
                    td.EnableHighlight(Color.red);
                    tilesToChange.Add(tile);
                }
            }
            else //if placing improvement
            {
                if (!world.IsBuildLocationTaken(tile) && !world.IsRoadOnTerrain(tile) && td.GetTerrainData().type == improvementData.terrainType)
                {
                    if (improvementData.rawMaterials && td.GetTerrainData().resourceType == improvementData.resourceType)
                    {
                        td.EnableHighlight(new Color(1, 1, 1, 0.2f));
                        tilesToChange.Add(tile);
                    }
                    else if (!improvementData.rawMaterials)
                    {
                        td.EnableHighlight(new Color(1, 1, 1, 0.2f));
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
            uiQueueManager.AddToQueue(tempBuildLocation - selectedCityLoc, improvementData);
            return;
        }

        BuildImprovement(improvementData, tempBuildLocation, selectedCity, false);
    }

    private void BuildImprovement(ImprovementDataSO improvementData, Vector3Int tempBuildLocation, City city, bool upgradingImprovement)
    {
        uiQueueManager.CheckIfBuiltItemIsQueued(tempBuildLocation - city.cityLoc, false,improvementData, city);

        if (tempBuildLocation == selectedCityLoc)
        {
            CreateBuilding(improvementData, city, upgradingImprovement);
            return;
        }

        //spending resources to build
        Vector3 buildLocation = tempBuildLocation;
        buildLocation.y = 0f;

        if (upgradingImprovement)
        {
            resourceManager.SpendResource(upgradeCost);
            upgradeCost.Clear();
        }
        else
        {
            resourceManager.SpendResource(improvementData.improvementCost);
        }

        resourceManager.UpdateUI();
        uiResourceManager.SetCityCurrentStorage(city.ResourceManager.GetResourceStorageLevel);
        //uiInfoPanelCityWarehouse.SetWarehouseStorageLevel(selectedCity.ResourceManager.GetResourceStorageLevel);
        //Debug.Log("Placing structure at " + buildLocation);

        //rotating harbor so it's closest to city
        int rotation = 0;
        if (improvementData.terrainType == TerrainType.Coast || improvementData.terrainType == TerrainType.River)
        {
            int minimum = 99999;
            int rotationIndex = 0;

            foreach (Vector3Int neighbor in world.GetNeighborsFor(tempBuildLocation, MapWorld.State.FOURWAYINCREMENT))
            {
                if (world.GetTerrainDataAt(neighbor).terrainData.sailable) //don't place harbor on neighboring water tiles
                    continue;

                int distanceFromCity = neighbor.sqrMagnitude - city.cityLoc.sqrMagnitude;
                if (distanceFromCity < minimum)
                {
                    minimum = distanceFromCity;
                    rotation = rotationIndex * 90;
                }

                rotationIndex++;
            }
        }

        //adding improvement to world dictionaries
        GameObject improvement = Instantiate(improvementData.prefab, buildLocation, Quaternion.Euler(0, rotation, 0));
        world.AddStructure(buildLocation, improvement);
        CityImprovement cityImprovement = improvement.GetComponent<CityImprovement>();
        cityImprovement.InitializeImprovementData(improvementData);
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
        world.AddResourceProducer(buildLocation, resourceProducer);
        resourceProducer.SetResourceManager(city.ResourceManager);
        resourceProducer.InitializeImprovementData(improvementData); //allows the new structure to also start generating resources
        resourceProducer.SetLocation(tempBuildLocation);

        //reset menu after one build
        if (!upgradingImprovement)
        {
            ResetTileLists();
            CloseImprovementBuildPanel();
        }

        //setting construction time and graphic first
        if (upgradingImprovement)
        {
            FinishImprovement(city, improvementData, tempBuildLocation);
        }
        else
        {
            CityImprovement constructionTile = GetFromConstructionTilePool();
            constructingTiles.Add(tempBuildLocation);
            world.SetCityImprovementConstruction(tempBuildLocation, constructionTile);
            constructionTile.transform.position = tempBuildLocation;
            constructionTile.BeginImprovementConstructionProcess(city, resourceProducer, improvementData, tempBuildLocation, this);
        }

        //if (improvementData.replaceTerrain)
        //{
        //    world.GetTerrainDataAt(tempBuildLocation).gameObject.SetActive(false);
        //}

        ////setting labor info
        //placesToWork++;
        //uiLaborAssignment.UpdateUI(selectedCity.cityPop, placesToWork);
        //ResetTileLists();
        //UpdateLaborNumbers();

        //CloseImprovementBuildPanel();
    }

    public void FinishImprovement(City city, ImprovementDataSO improvementData, Vector3Int tempBuildLocation)
    {
        //activating structure
        world.GetStructure(tempBuildLocation).SetActive(true);

        if (improvementData.replaceProp)
        {
            TerrainData td = world.GetTerrainDataAt(tempBuildLocation);
            if (td.prop != null)
                td.prop.gameObject.SetActive(false);
        }

        if (improvementData.replaceTerrain)
        {
            world.GetTerrainDataAt(tempBuildLocation).gameObject.SetActive(false);
        }

        //setting harbor info
        if (improvementData.improvementName == "Harbor")
        {
            city.hasHarbor = true;
            city.harborLocation = tempBuildLocation;
            world.SetCityHarbor(city, tempBuildLocation);
        }

        //setting labor info
        world.AddToMaxLaborDict(tempBuildLocation, improvementData.maxLabor);
        if (city.AutoAssignLabor && city.cityPop.UnusedLabor > 0)
            city.AutoAssignmentsForLabor();

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
                placesToWork++;
                if (laborChange != 0)
                    LaborTileHighlight();
                else if (removingImprovement && uiImprovementBuildInfoPanel.activeStatus)
                    ImprovementTileHighlight();
                else if (upgradingImprovement && uiImprovementBuildInfoPanel.activeStatus)
                    UpgradeTileHighlight();
                //PrepareLaborNumber(tempBuildLocation);

                UpdateCityLaborUIs();
            }
        }
    }

    //public void RemoveImprovementButton()
    //{
    //    laborChange = 0;
    //    //uiImprovementBuilder.ToggleVisibility(false);
    //    uiCityTabs.HideSelectedTab();

    //    removingImprovement = true;
    //    ImprovementTileHighlight();
    //}

    private void RemoveImprovement(Vector3Int improvementLoc, CityImprovement selectedImprovement, City city, bool upgradingImprovement)
    {
        if (selectedImprovement.queued)
        {
            uiQueueManager.CheckIfBuiltItemIsQueued(improvementLoc, true, selectedImprovement.GetImprovementData, selectedImprovement.GetQueueCity());
        }

        //remove building
        if (improvementLoc == selectedCityLoc)
        {
            RemoveBuilding(selectedImprovement.GetImprovementData.improvementName);
            return;
        }
        else if (selectedImprovement == null) //in case the tile is selected, missing the box collider of development
        {
            if (world.CheckIfTileIsUnderConstruction(improvementLoc))
                selectedImprovement = world.GetCityDevelopmentConstruction(improvementLoc);
            else
                selectedImprovement = world.GetCityDevelopment(improvementLoc);
        }

        //putting the resources and labor back
        GameObject improvement = world.GetStructure(improvementLoc);
        ResourceProducer resourceProducer = world.GetResourceProducer(improvementLoc);

        foreach (ResourceType resourceType in resourceProducer.producedResources)
        {
            for (int i = 0; i < world.GetCurrentLaborForTile(improvementLoc); i++)
            {
                city.ChangeResourcesWorked(resourceType, -1);

                int totalResourceLabor = city.GetResourcesWorkedResourceCount(resourceType);
                uiLaborHandler.PlusMinusOneLabor(resourceType, totalResourceLabor, -1, city.ResourceManager.GetResourceGenerationValues(resourceType));
                if (totalResourceLabor == 0)
                    city.RemoveFromResourcesWorked(resourceType);
            }
        }

        resourceProducer.UpdateCurrentLaborData(0);
        resourceProducer.UpdateResourceGenerationData();
        resourceProducer.StopProducing();
        ImprovementDataSO improvementData = selectedImprovement.GetImprovementData;

        if (!upgradingImprovement)
        {
            int i = 0;
            foreach (ResourceValue resourceValue in improvementData.improvementCost) //adding back 100% of cost (if there's room)
            {
                int resourcesReturned = resourceManager.CheckResource(resourceValue.resourceType, resourceValue.resourceAmount);
                Vector3 loc = improvementLoc;
                loc.z += -.5f * i;
                i++;
                if (resourcesReturned == 0)
                {
                    InfoResourcePopUpHandler.CreateResourceStat(loc, resourceValue.resourceAmount, ResourceHolder.Instance.GetIcon(resourceValue.resourceType), true);
                    continue;
                }
                InfoResourcePopUpHandler.CreateResourceStat(loc, resourcesReturned, ResourceHolder.Instance.GetIcon(resourceValue.resourceType));
            }
        }

        resourceManager.UpdateUI();
        uiResourceManager.SetCityCurrentStorage(city.ResourceManager.GetResourceStorageLevel);

        if (improvementData.replaceTerrain)
        {
            world.GetTerrainDataAt(improvementLoc).gameObject.SetActive(true);
        }

        if (improvementData.singleBuild)
        {
            city.singleBuildImprovementsBuildingsDict.Remove(improvementData.improvementName);
            world.RemoveSingleBuildFromCityLabor(improvementLoc);
        }

        if (!selectedImprovement.isConstruction)
        {
            if (!world.CheckIfTileIsMaxxed(improvementLoc))
                placesToWork--;
            int currentLabor = world.GetCurrentLaborForTile(improvementLoc);
            city.cityPop.UnusedLabor += currentLabor;
            city.cityPop.UsedLabor -= currentLabor;

            Destroy(improvement);
        }

        //updating world dicts
        RemoveLaborFromDicts(improvementLoc);
        world.RemoveFromMaxWorked(improvementLoc);
        world.RemoveStructure(improvementLoc);
        developedTiles.Remove(improvementLoc);

        if (city == selectedCity && removingImprovement && uiImprovementBuildInfoPanel.activeStatus)
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
        }

        //updating all the labor info
        city.UpdateCityPopInfo();

        if (city.activeCity)
        {
            uiInfoPanelCity.SetData(selectedCity.CityName, selectedCity.cityPop.CurrentPop, selectedCity.HousingCount, selectedCity.cityPop.UnusedLabor, selectedCity.GetSetWorkEthic,
                resourceManager.FoodGrowthLevel, resourceManager.FoodGrowthLimit, resourceManager.FoodPerMinute);

        }

        if (city.AutoAssignLabor && city.cityPop.UnusedLabor > 0)
            city.AutoAssignmentsForLabor();

        if (city.activeCity)
        {
            UpdateLaborNumbers();
            uiLaborAssignment.UpdateUI(selectedCity, placesToWork);
        }
    }

    public void CloseImprovementBuildPanel()
    {
        if (uiImprovementBuildInfoPanel.activeStatus)
        {
            if (uiImprovementBuildInfoPanel.activeStatus & !removingImprovement)
                CameraDefaultRotation();
            if (removingImprovement)
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

    private void UpdateCityWorkEthic(float workEthicChange)
    {
        if (workEthicChange != 0)
        {
            selectedCity.ChangeWorkEthic(workEthicChange);

            List<Vector3Int> tempDevelopedTiles = new(developedTiles) {selectedCityLoc};

            foreach (Vector3Int tile in tempDevelopedTiles)
            {
                if (world.CheckImprovementIsProducer(tile))
                    world.GetResourceProducer(tile).CalculateResourceGenerationPerMinute();
            }

            //foreach (string cityBuildingName in world.GetBuildingListForCity(selectedCityLoc))
            //{
            //    if (world.CheckBuildingIsProducer(selectedCityLoc, cityBuildingName))
            //        world.GetBuildingProducer(selectedCityLoc, cityBuildingName).CalculateResourceGenerationPerMinute();
            //}
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

    private void UpdateLaborNumbers()
    {
        if (isActive)
        {
            HideLaborNumbers();
            foreach (Vector3Int tile in developedTiles)
            {
                //if (world.CheckIfTileIsImproved(tile))
                PrepareLaborNumber(tile);
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

            //if (world.CheckIfTileIsImproved(tile))
            //{
                CityImprovement improvement = world.GetCityDevelopment(tile); //cached for speed
                improvement.DisableHighlight();
                if (laborChange > 0 && !world.CheckIfTileIsMaxxed(tile) && selectedCity.cityPop.UnusedLabor > 0) //for increasing labor, can't be maxxed out
                {
                    td.EnableHighlight(Color.green);
                    improvement.EnableHighlight(Color.green);
                    tilesToChange.Add(tile);   
                }

                if (laborChange < 0 && world.CheckIfTileIsWorked(tile)) //for decreasing labor, can't be at 0. 
                {
                    td.EnableHighlight(Color.red);
                    improvement.EnableHighlight(Color.red);
                    tilesToChange.Add(tile);
                }
            //}
        }
    }

    private void PrepareLaborNumber(Vector3Int tile) //grabbing labor numbers and prefab from pool
    {
        //specifying location on tile
        Vector3 numberPosition = tile;
        numberPosition.y += .01f;
        numberPosition.z -= 1f; //bottom center of tile

        //Object pooling set up
        CityLaborTileNumber tempObject = GetFromLaborNumbersPool();
        tempObject.transform.position = numberPosition;
        laborNumberList.Add(tempObject);

        if (world.GetMaxLaborForTile(tile) == 0) //don't show numbers for those that don't take labor
            return;
        tempObject.SetLaborNumber(world.PrepareLaborNumbers(tile));
    }

    private void ChangeLaborCount(TerrainData terrainSelected, Vector3Int terrainLocation)
    {
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

        ResourceProducer resourceProducer = world.GetResourceProducer(terrainLocation); //cached all resource producers in dict
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
            if (world.GetCityDevelopment(terrainLocation).queued)
            {
                CityImprovement selectedImprovement = world.GetCityDevelopment(terrainLocation);
                City tempCity = selectedImprovement.GetQueueCity();

                if (tempCity != selectedCity)
                {
                    tempCity.RemoveFromQueue(terrainLocation, selectedImprovement.GetImprovementData);
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
        }

        if (labor != 0)
        {
            world.AddToCurrentFieldLabor(terrainLocation, labor);
        }

        //updating resource generation uis
        resourceProducer.UpdateResourceGenerationData();
        foreach (ResourceType resourceType in resourceProducer.producedResources)
        {
            selectedCity.ChangeResourcesWorked(resourceType, laborChange);

            int totalResourceLabor = selectedCity.GetResourcesWorkedResourceCount(resourceType);

            uiLaborHandler.PlusMinusOneLabor(resourceType, totalResourceLabor, laborChange, selectedCity.ResourceManager.GetResourceGenerationValues(resourceType));

            if (totalResourceLabor == 0)
                selectedCity.RemoveFromResourcesWorked(resourceType);
        }

        //updating all the labor info
        selectedCity.UpdateCityPopInfo();

        uiInfoPanelCity.SetData(selectedCity.CityName, selectedCity.cityPop.CurrentPop, selectedCity.HousingCount, selectedCity.cityPop.UnusedLabor, selectedCity.GetSetWorkEthic, 
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
        uiInfoPanelCity.SetData(selectedCity.CityName, selectedCity.cityPop.CurrentPop, selectedCity.HousingCount, selectedCity.cityPop.UnusedLabor, selectedCity.GetSetWorkEthic,
            resourceManager.FoodGrowthLevel, resourceManager.FoodGrowthLimit, resourceManager.FoodPerMinute);
        uiLaborAssignment.UpdateUI(selectedCity, placesToWork);
    }

    public void CloseCityTab()
    {
        uiCityTabs.HideSelectedTab();
    }

    public void ResetCityUIToBase()
    {
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

            if (city == selectedCity)
            {
                upgradingImprovement = true;
                UpgradeSelectedImprovement(tile, world.GetCityDevelopment(tile), city);
                upgradingImprovement = false;
            }
            else
            {
                UpgradeSelectedImprovement(tile, world.GetCityDevelopment(tile), city);
            }
        }
        else if (queuedItem.unitBuildData != null) //build unit
        {
            if (city.cityPop.CurrentPop == 0)
            {
                InfoPopUpHandler.Create(city.cityLoc, "Not enough pop to make unit");
                city.GoToNextItemInQueue();
                uiQueueManager.CheckIfBuiltUnitIsQueued(queuedItem.unitBuildData);
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
                uiQueueManager.CheckIfBuiltItemIsQueued(tile, false, queuedItem.improvementData, city);
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
        GameObject destroyedCity = world.GetStructure(selectedCityLoc);

        //destroy all construction projects upon destroying city
        removingImprovement = true;
        List<Vector3Int> constructionToStop = new(constructingTiles);

        foreach (Vector3Int constructionTile in constructionToStop)
        {
            CityImprovement construction = world.GetCityDevelopmentConstruction(constructionTile);
            construction.RemoveConstruction(this, constructionTile);
            RemoveImprovement(constructionTile, construction, selectedCity, false);
        }
        removingImprovement = false;

        world.RemoveStructure(selectedCityLoc);
        world.RemoveCityName(selectedCityLoc);

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
                world.AddToUnclaimedSingleBuild(improvementLoc);
        }
        Destroy(destroyedCity);

        uiDestroyCityWarning.ToggleVisibility(false);

        ResetCityUI();
    }

    public void NoDestroyCity() //in case user chickens out
    {
        uiDestroyCityWarning.ToggleVisibility(false);
        //uiUnitBuilder.ToggleVisibility(true);
        uiCityTabs.HideSelectedTab();
    }

    public void ResetTileLists()
    {
        foreach (Vector3 location in resourceInfoPanelDict.Keys)
            PoolResourceInfoPanel(location);
        resourceInfoPanelDict.Clear();

        foreach (Vector3Int tile in tilesToChange)
        {
            world.GetTerrainDataAt(tile).DisableHighlight();
            if (world.CheckIfTileIsImproved(tile))
                world.GetCityDevelopment(tile).DisableHighlight(); //cached for speed 

            if (world.CheckIfTileIsUnderConstruction(tile))
                world.GetCityDevelopmentConstruction(tile).DisableHighlight();
        }
        tilesToChange = new List<Vector3Int>();
        improvementData = null;
        laborChange = 0;
        //removingBuilding = false;
        removingImprovement = false;
        upgradingImprovement = false;
    }

    private void ResetCityUI()
    {
        if (selectedCity != null)
        {
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
            uiLaborHandler.ResetUI();
            HideBorders();
            ToggleBuildingHighlight(false);
            selectedCity.Deselect();
            selectedCity.HideCityGrowthProgressTimeBar();
            selectedCityLoc = new();
            selectedCity.activeCity = false;
            selectedCity = null;
        }
    }




    private void GiveWarningMessage(string message)
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; //z must be more than 0, else just gives camera position
        Vector3 mouseLoc = Camera.main.ScreenToWorldPoint(mousePos);

        InfoPopUpHandler.Create(mouseLoc, message);
    }


    //Object pooling methods
    private void GrowLaborNumbersPool()
    {
        for (int i = 0; i < 12; i++) //grow pool 12 at a time
        {
            GameObject laborNumber = Instantiate(GameAssets.Instance.laborNumberPrefab);
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
        for (int i = 0; i < 5; i++) //grow pool 5 at a time
        {
            GameObject constructionTileGO = Instantiate(constructionTile);
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

    //object pooling the resource info panels
    private void GrowResourceInfoPanelPool()
    {
        for (int i = 0; i < 20; i++) //grow pool 20 at a time
        {
            GameObject resourceInfoPanelGO = Instantiate(GameAssets.Instance.resourceInfoPanel);
            ResourceInfoPanel resourceInfoPanel = resourceInfoPanelGO.GetComponent<ResourceInfoPanel>();
            //resourceInfoPanel.transform.rotation = Quaternion.Euler(90, 0, 0); //rotating to lie flat on tile
            AddToResourceInfoPanelPool(resourceInfoPanel);
        }
    }

    public void AddToResourceInfoPanelPool(ResourceInfoPanel resourceInfoPanel)
    {
        resourceInfoPanel.gameObject.SetActive(false);
        resourceInfoPanelQueue.Enqueue(resourceInfoPanel);
    }

    public ResourceInfoPanel GetFromResourceInfoPanelPool()
    {
        if (resourceInfoPanelQueue.Count == 0)
            GrowResourceInfoPanelPool();

        var resourceInfoPanel = resourceInfoPanelQueue.Dequeue();
        resourceInfoPanel.gameObject.SetActive(true);
        return resourceInfoPanel;
    }
}
