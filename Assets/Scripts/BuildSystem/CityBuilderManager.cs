using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

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
    private UILaborHandler uiLaborHandler;
    [SerializeField]
    private UIImprovementBuildPanel uiImprovementBuildInfoPanel;
    [SerializeField]
    private UICityNamer uiCityNamer;
    [SerializeField]
    private UIDestroyCityWarning uiDestroyCityWarning;

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

    private ImprovementDataSO improvementData;

    [HideInInspector]
    public ResourceManager resourceManager;

    private int laborChange;

    private int placesToWork; //to see how many tiles a city has in its radius to send labor to. 

    //for object pooling of labor numbers
    private Queue<CityLaborTileNumber> laborNumberQueue = new(); //the pool in object pooling
    private List<CityLaborTileNumber> laborNumberList = new(); //to add to in order to add back into pool

    //for object pooling of city borders
    private Queue<GameObject> borderQueue = new();
    private List<GameObject> borderList = new();

    ////for object pooling of city borders
    //private Queue<TimeProgressBar> borderQueue = new();
    //private List<TimeProgressBar> borderList = new();

    //for naming of units (need to store these to save)
    private int workerCount;
    private int infantryCount;

    private bool removingImprovement, removingBuilding, isQueueing; //flags thrown when doing specific tasks
    private bool isActive; //when looking at a city

    private UnitBuildDataSO lastUnitData;

    private void Awake()
    {
        GrowLaborNumbersPool();
        GrowBordersPool();
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
           
            selectedCity = cityReference;

            isActive = true;
            selectedCity.activeCity = true;
            selectedCityLoc = world.GetClosestTerrainLoc(location);
            (cityTiles, developedTiles) = GetThisCityRadius();
            ResourceProducerTimeProgressBarsSetActive(true);
            ToggleBuildingHighlight(true);
            DrawBorders();
            CheckForWork();
            resourceManager = selectedCity.ResourceManager;
            resourceManager.SetUI(uiResourceManager, uiInfoPanelCity);
            uiResourceManager.SetCityInfo(selectedCity.CityName, selectedCity.warehouseStorageLimit, selectedCity.ResourceManager.GetResourceStorageLevel);
            resourceManager.UpdateUI();
            uiCityTabs.ToggleVisibility(true, resourceManager);
            uiResourceManager.ToggleVisibility(true);
            CenterCamOnCity();
            uiInfoPanelCity.SetData(selectedCity.CityName, selectedCity.cityPop.GetPop, selectedCity.cityPop.GetSetUnusedLabor, selectedCity.GetSetWorkEthic, 
                resourceManager.FoodGrowthLevel, resourceManager.FoodGrowthLimit, selectedCity.CountDownTimer, resourceManager.FoodPerMinute,
                selectedCity.FoodConsumptionPerMinute, selectedCity.GetMinutesTillGrowth, selectedCity.GetResearchPerMinute);
            //uiInfoPanelCityWarehouse.SetAllWarehouseData(selectedCity.ResourceManager.ResourceStorageLimit,
            //    selectedCity.ResourceManager.GetResourceStorageLevel);
            uiInfoPanelCity.ToggleVisibility(true);
            //uiInfoPanelCityWarehouse.ToggleTweenVisibility(true);
            uiLaborAssignment.ShowUI(selectedCity.cityPop, placesToWork);
            uiUnitTurn.buttonClicked.AddListener(ResetCityUI);
            UpdateLaborNumbers();
            selectedCity.Select();
        }
        //selecting tiles to place improvements
        else if (selectedCity != null && selectedObject.TryGetComponent(out TerrainData terrainSelected))
        {
            Vector3Int terrainLocation = terrainSelected.GetTileCoordinates();

            //deselecting if choosing tile outside of city
            if (!cityTiles.Contains(terrainLocation))
                ResetCityUI();

            if (improvementData == null && laborChange == 0 && !removingImprovement)
                return;

            //TerrainData terrainSelected = detectedObject.GetComponent<TerrainData>();

            //TerrainData terrainSelected = world.GetTerrainData(terrainLocation);
            //Vector3Int terrainLocation = terrainSelected.GetTileCoordinates();

            if (!tilesToChange.Contains(terrainLocation))
            {
                Debug.Log("Not suitable location");
                return;
            }

            if (removingImprovement)
            {
                RemoveImprovement(terrainSelected);
                return;
            }

            if (world.CheckIfTileIsImproved(terrainLocation)) //for changing labor counts in tile
            {
                if (improvementData != null)
                {
                    Debug.Log("Already developed");
                    return;
                }

                ChangeLaborCount(terrainSelected, terrainLocation);
            }
            else
            {
                if (laborChange != 0)
                {
                    Debug.Log("Needs to be developed to work");
                    return;
                }

                BuildImprovement(improvementData, terrainLocation); //for building improvement
            }
        }
        else
        {
            //selectedCity = null;
            ResetCityUI();
        }
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

    //        BuildImprovement(improvementData, terrainLocation); //for building improvement
    //    }
    //}

    private void ToggleBuildingHighlight(bool v)
    {
        if (v)
        {
            foreach (string name in world.GetBuildingListForCity(selectedCityLoc))
            {
                world.GetBuildingData(selectedCityLoc, name).EnableHighlight(Color.white);
            }
        }
        else
        {
            foreach (string name in world.GetBuildingListForCity(selectedCityLoc))
            {
                world.GetBuildingData(selectedCityLoc, name).DisableHighlight();
            }
        }
    }

    private (List<Vector3Int>, List<Vector3Int>) GetThisCityRadius() //radius just for selected city
    {
        return world.GetCityRadiusFor(selectedCityLoc, selectedCity.gameObject);
    }

    private void ResourceProducerTimeProgressBarsSetActive(bool v)
    {
        foreach (Vector3Int tile in developedTiles)
        {
            world.GetResourceProducer(tile).TimeProgressBarSetActive(v);
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
                        //tempObject.transform.rotation = Quaternion.Euler(0, 0, 0);
                    }

                    tempObject.transform.position = borderPosition;
                    //tempObject.GetComponent<SpriteRenderer>(); //not sure why this was here
                }
            }
        }
    }

    public void CheckPopForUnit(UnitBuildDataSO unitData)
    {
        if (selectedCity.cityPop.GetPop == 1 && !isQueueing)
        {
            uiDestroyCityWarning.ToggleVisibility(true);
            //uiUnitBuilder.ToggleVisibility(false);
            uiCityTabs.HideSelectedTab();
            lastUnitData = unitData;
            return;
        }

        CreateUnit(unitData);
    }

    private void CreateUnit(UnitBuildDataSO unitData) //action for the button to run
    {
        if (isQueueing)
        {
            uiQueueManager.AddToQueue(new Vector3Int(0,0,0), null, unitData);
            return;
        }

        selectedCity.PopulationDeclineCheck(); //decrease population before creating unit so we can see where labor will be lost
        //CheckForWork(); //not necessary

        //updating uis after losing pop
        UpdateLaborNumbers();
        uiLaborAssignment.UpdateUI(selectedCity.cityPop, placesToWork);

        uiInfoPanelCity.SetData(selectedCity.CityName, selectedCity.cityPop.GetPop, selectedCity.cityPop.GetSetUnusedLabor, selectedCity.GetSetWorkEthic, 
            resourceManager.FoodGrowthLevel, resourceManager.FoodGrowthLimit, selectedCity.CountDownTimer, resourceManager.FoodPerMinute,
            selectedCity.FoodConsumptionPerMinute, selectedCity.GetMinutesTillGrowth, selectedCity.GetResearchPerMinute);

        resourceManager.SpendResource(unitData.unitCost);
        resourceManager.UpdateUI();
        uiResourceManager.SetCityCurrentStorage(selectedCity.ResourceManager.GetResourceStorageLevel);
        //uiInfoPanelCityWarehouse.SetWarehouseStorageLevel(selectedCity.ResourceManager.GetResourceStorageLevel);
        Debug.Log("building " + unitData.name + " at " + selectedCityLoc);

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

        selectedCity.SelectUnitToProduce(unitGO);

        //uiUnitBuilder.ToggleVisibility(false);
        uiCityTabs.HideSelectedTab();
    }

    public void CreateBuilding(ImprovementDataSO buildingData)
    {
        //Queue info
        if (isQueueing)
        {
            uiQueueManager.AddToQueue(new Vector3Int(0,0,0), buildingData);
            return;
        }
        uiQueueManager.CheckIfBuiltItemIsQueued(new Vector3Int(0, 0, 0), buildingData);
        
        laborChange = 0;

        //building locations
        Vector3 cityPos = selectedCity.transform.position;
        Vector3 buildingLocalPos = buildingData.buildingLocation; //putting the building in it's position in the city square
        cityPos += buildingLocalPos;
        
        //resource info
        resourceManager.SpendResource(buildingData.improvementCost); //this spends the resources needed to build
        resourceManager.UpdateUI();
        uiResourceManager.SetCityCurrentStorage(selectedCity.ResourceManager.GetResourceStorageLevel);
        //uiInfoPanelCityWarehouse.SetWarehouseStorageLevel(selectedCity.ResourceManager.GetResourceStorageLevel);
        //Debug.Log("Placing structure in " + selectedCity.CityName);

        //setting world data
        GameObject building = Instantiate(buildingData.prefab, cityPos, Quaternion.identity);
        string buildingName = buildingData.prefab.name;
        world.SetCityBuilding(selectedCityLoc, buildingName, building);
        world.AddToCityMaxLaborDict(selectedCityLoc, buildingName, buildingData.maxLabor);

        ResourceProducer resourceProducer = building.GetComponent<ResourceProducer>();
        if (resourceProducer != null) //not all buildings will generate resources 
        {
            resourceProducer.SetResourceManager(resourceManager); //need to set resourceManager for each new resource producer. 
            resourceProducer.InitializeImprovementData(buildingData); //allows the new structure to also start generating resources
            resourceProducer.SetLocation(selectedCityLoc);
            world.AddToCityBuildingIsProducerDict(selectedCityLoc, buildingName, resourceProducer);
        }

        WorkEthicHandler workEthicHandler = building.GetComponent<WorkEthicHandler>();
        if (workEthicHandler != null) //not all buildings will have work ethic changes
        {
            workEthicHandler.InitializeImprovementData(buildingData);
        }

        //setting labor data
        if (buildingData.maxLabor > 0)
        {
            placesToWork++;
            uiLaborAssignment.UpdateUI(selectedCity.cityPop, placesToWork);
        }

        //ui updating
        uiCityTabs.HideSelectedTab();
        UpdateLaborNumbers();
    }

    public void RemoveBuildingButton()
    {
        //uiBuildingBuilder.ToggleVisibility(false);
        uiCityTabs.HideSelectedTab();

        //if (world.GetBuildingListForCity(selectedCityLoc).Count == 0)
        //    return;

        uiLaborHandler.ShowUIRemoveBuildings(selectedCityLoc, world);
        removingBuilding = true;
    }

    private void RemoveBuilding(string selectedBuilding)
    {
        //putting the resources and labor back
        GameObject building = world.GetBuilding(selectedCityLoc, selectedBuilding);
        WorkEthicHandler workEthicHandler = building.GetComponent<WorkEthicHandler>();

        if (world.CheckBuildingIsProducer(selectedCityLoc, selectedBuilding))
        {
            ResourceProducer resourceProducer = world.GetBuildingProducer(selectedCityLoc, selectedBuilding);
            resourceProducer.UpdateCurrentLaborData(0);

            foreach (ResourceValue resourceValue in resourceProducer.GetImprovementData.improvementCost) //adding back 100% of cost (if there's room)
            {
                resourceManager.CheckResource(resourceValue.resourceType, resourceValue.resourceAmount);
            }
        }
        else //if building has both, still only do one (should have at least one)
        {
            foreach (ResourceValue resourceValue in workEthicHandler.GetImprovementData.improvementCost)
            {
                resourceManager.CheckResource(resourceValue.resourceType, resourceValue.resourceAmount);
            }
        }

        if (workEthicHandler != null)
        {
            UpdateCityWorkEthic(workEthicHandler.GetWorkEthicChange(0)); //zero out labor
        }


        resourceManager.UpdateUI();
        uiResourceManager.SetCityCurrentStorage(selectedCity.ResourceManager.GetResourceStorageLevel);
        //uiInfoPanelCityWarehouse.SetWarehouseStorageLevel(selectedCity.ResourceManager.GetResourceStorageLevel);

        int currentLabor = world.GetCurrentLaborForBuilding(selectedCityLoc, selectedBuilding);
        int maxLabor = world.GetMaxLaborForBuilding(selectedCityLoc, selectedBuilding);
        selectedCity.cityPop.GetSetUnusedLabor += currentLabor;
        selectedCity.cityPop.GetSetUsedLabor -= currentLabor;

        Destroy(building);

        //updating world dicts
        world.RemoveCityBuilding(selectedCityLoc, selectedBuilding);

        //updating all the labor info
        selectedCity.UpdateCityPopInfo();

        uiInfoPanelCity.SetData(selectedCity.CityName, selectedCity.cityPop.GetPop, selectedCity.cityPop.GetSetUnusedLabor, selectedCity.GetSetWorkEthic, 
            resourceManager.FoodGrowthLevel, resourceManager.FoodGrowthLimit, selectedCity.CountDownTimer, resourceManager.FoodPerMinute,
            selectedCity.FoodConsumptionPerMinute, selectedCity.GetMinutesTillGrowth, selectedCity.GetResearchPerMinute);

        RemoveLaborFromBuildingDicts(selectedBuilding);
        //resourceManager.UpdateUIGenerationAll();
        UpdateLaborNumbers();

        //this object maintenance
        if (currentLabor < maxLabor)
            placesToWork--;
        //uiImprovementBuilder.ToggleVisibility(false);
        uiCityTabs.HideSelectedTab();
        uiLaborAssignment.UpdateUI(selectedCity.cityPop, placesToWork);
        removingBuilding = false;
        uiLaborHandler.ShowUIRemoveBuildings(selectedCityLoc, world);
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
        CameraBirdsEyeRotation();

        tilesToChange.Clear();

        if (removingImprovement)
        {
            uiImprovementBuildInfoPanel.SetText("Removing Improvement");
            uiImprovementBuildInfoPanel.ToggleVisibility(true);
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
                    //removingImprovement = true;
                }
            }
            else //if placing improvement
            {
                TerrainType tt = td.GetTerrainData().type;

                if (!world.IsBuildLocationTaken(tile) && !world.TileHasBuildings(tile) && !world.IsRoadOnTerrain(tile) && 
                    td.GetTerrainData().resourceType == improvementData.resourceType && tt == improvementData.terrainType)
                {
                    td.EnableHighlight(new Color(1, 1, 1, 0.2f));
                    tilesToChange.Add(tile);
                }
            }
        }
    }

    private void BuildImprovement(ImprovementDataSO improvementData, Vector3Int tempBuildLocation)
    {
        if (isQueueing)
        {
            uiQueueManager.AddToQueue(tempBuildLocation - selectedCityLoc, improvementData);
            return;
        }

        uiQueueManager.CheckIfBuiltItemIsQueued(tempBuildLocation - selectedCityLoc, improvementData);
        
        Vector3 buildLocation = tempBuildLocation;

        buildLocation.y = 0f;
        resourceManager.SpendResource(improvementData.improvementCost); //this spends the resources needed to build
        resourceManager.UpdateUI();
        uiResourceManager.SetCityCurrentStorage(selectedCity.ResourceManager.GetResourceStorageLevel);
        //uiInfoPanelCityWarehouse.SetWarehouseStorageLevel(selectedCity.ResourceManager.GetResourceStorageLevel);
        Debug.Log("Placing structure at " + buildLocation);

        int rotation = 0;

        if (improvementData.terrainType == TerrainType.Coast || improvementData.terrainType == TerrainType.River)
        {
            int minimum = 99999;
            int rotationIndex = 0;
            
            foreach(Vector3Int neighbor in world.GetNeighborsFor(tempBuildLocation, MapWorld.State.FOURWAYINCREMENT))
            {
                if (world.GetTerrainDataAt(neighbor).terrainData.sailable) //don't place harbor on neighboring water tiles
                    continue;
                
                int distanceFromCity = neighbor.sqrMagnitude - selectedCityLoc.sqrMagnitude;
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
        world.SetCityDevelopment(buildLocation, improvement.GetComponent<CityImprovement>());
        world.AddToMaxLaborDict(buildLocation, improvementData.maxLabor);
        if (improvementData.improvementName == "Harbor")
        {
            selectedCity.hasHarbor = true;
            selectedCity.harborLocation = tempBuildLocation;
            world.SetCityHarbor(selectedCity, tempBuildLocation);
            world.AddToCityLabor(tempBuildLocation, selectedCity.gameObject);
        }
        developedTiles.Add(tempBuildLocation);


        //resource production
        ResourceProducer resourceProducer = improvement.GetComponent<ResourceProducer>();
        world.AddResourceProducer(buildLocation, resourceProducer);
        resourceProducer.SetResourceManager(resourceManager); //need to set resourceManager for each new resource producer. 
        resourceProducer.InitializeImprovementData(improvementData); //allows the new structure to also start generating resources
        resourceProducer.SetLocation(tempBuildLocation);

        if (improvementData.replaceTerrain)
        {
            world.GetTerrainDataAt(tempBuildLocation).gameObject.SetActive(false);
        }
        
        placesToWork++;
        uiLaborAssignment.UpdateUI(selectedCity.cityPop, placesToWork);
        ResetTileLists();
        UpdateLaborNumbers();

        CloseImprovementBuildPanel();
    }

    public void RemoveImprovementButton()
    {
        laborChange = 0;
        //uiImprovementBuilder.ToggleVisibility(false);
        uiCityTabs.HideSelectedTab();

        removingImprovement = true;
        ImprovementTileHighlight();
    }

    private void RemoveImprovement(TerrainData selectedImprovement)
    {
        //putting the resources and labor back
        Vector3Int improvementLoc = selectedImprovement.GetTileCoordinates();
        GameObject improvement = world.GetStructure(improvementLoc);
        ResourceProducer resourceProducer = world.GetResourceProducer(improvementLoc);
        resourceProducer.UpdateCurrentLaborData(0);
        resourceProducer.StopProducing();

        foreach (ResourceValue resourceValue in resourceProducer.GetImprovementData.improvementCost) //adding back 100% of cost (if there's room)
        {
            resourceManager.CheckResource(resourceValue.resourceType, resourceValue.resourceAmount);
        }
        resourceManager.UpdateUI();
        uiResourceManager.SetCityCurrentStorage(selectedCity.ResourceManager.GetResourceStorageLevel);

        if (resourceProducer.GetImprovementData.replaceTerrain)
        {
            world.GetTerrainDataAt(improvementLoc).gameObject.SetActive(true);
        }

        //uiInfoPanelCityWarehouse.SetWarehouseStorageLevel(selectedCity.ResourceManager.GetResourceStorageLevel);

        int currentLabor = world.GetCurrentLaborForTile(improvementLoc);
        int maxLabor = world.GetMaxLaborForTile(improvementLoc);
        selectedCity.cityPop.GetSetUnusedLabor += currentLabor;
        selectedCity.cityPop.GetSetUsedLabor -= currentLabor;

        Destroy(improvement);

        //updating world dicts
        world.RemoveFromMaxWorked(improvementLoc);
        world.RemoveStructure(improvementLoc);
        developedTiles.Remove(improvementLoc);
        if (improvementLoc == selectedCity.harborLocation)
        {
            selectedCity.hasHarbor = false;
            world.RemoveHarbor(improvementLoc);
            world.RemoveFromCityLabor(improvementLoc);
        }

        //updating all the labor info
        selectedCity.UpdateCityPopInfo();

        uiInfoPanelCity.SetData(selectedCity.CityName, selectedCity.cityPop.GetPop, selectedCity.cityPop.GetSetUnusedLabor, selectedCity.GetSetWorkEthic, 
            resourceManager.FoodGrowthLevel, resourceManager.FoodGrowthLimit, selectedCity.CountDownTimer, resourceManager.FoodPerMinute,
            selectedCity.FoodConsumptionPerMinute, selectedCity.GetMinutesTillGrowth, selectedCity.GetResearchPerMinute);

        RemoveLaborFromDicts(improvementLoc);
        //resourceManager.UpdateUIGeneration(selectedImprovement.GetTerrainData().resourceType);
        UpdateLaborNumbers();

        //this object maintenance
        if (currentLabor < maxLabor)
            placesToWork--;
        //uiImprovementBuilder.ToggleVisibility(false);
        ResetTileLists();
        removingImprovement = false;
        uiLaborAssignment.UpdateUI(selectedCity.cityPop, placesToWork);

        CloseImprovementBuildPanel();
    }

    public void CloseImprovementBuildPanel()
    {
        if (uiImprovementBuildInfoPanel.activeStatus)
            CameraDefaultRotation();
        ResetTileLists();
        uiImprovementBuildInfoPanel.ToggleVisibility(false);
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
        //ResetTileLists();
        CloseImprovementBuildPanel();

        removingBuilding = false;
        improvementData = null;
        
        if (laborChange != uiLaborAssignment.laborChangeFlag) //turn on menu only when selecting different button
        {
            uiLaborAssignment.ResetLaborAssignment(-laborChange);
            uiLaborAssignment.laborChangeFlag = laborChange;
            this.laborChange = laborChange;

            //if (world.GetBuildingListForCity(selectedCityLoc).Count > 0) //only shows if city has buildings
            uiLaborHandler.ShowUI(laborChange, selectedCity, world, placesToWork);

            BuildingButtonHighlight();
            LaborTileHighlight();
            UpdateLaborNumbers();
        }
        else
        {
            uiLaborHandler.HideUI();
            uiLaborAssignment.ResetLaborAssignment(laborChange);
        }
    }

    public void PassLaborChange(string buildingName) //for changing labor counts in city labor
    {
        if (removingBuilding)
        {
            RemoveBuilding(buildingName);
            return;
        }

        int labor = uiLaborHandler.GetCurrentLabor;
        int maxLabor = uiLaborHandler.GetMaxLabor;

        if (laborChange > 0 && labor == maxLabor) //checks in case button interactables don't work
            return;
        if (laborChange < 0 && labor == 0)
            return;

        labor = ChangePlacesToWorkCount(labor, maxLabor);
        selectedCity.cityPop.GetSetCityLaborers += laborChange;
        ChangeCityLaborInfo();

        if (world.CheckBuildingIsProducer(selectedCityLoc, buildingName))
        {
            world.GetBuildingProducer(selectedCityLoc, buildingName).UpdateCurrentLaborData(labor);
        }

        WorkEthicHandler workEthicHandler = world.GetBuilding(selectedCityLoc, buildingName).GetComponent<WorkEthicHandler>();

        float workEthicChange = 0f;
        if (workEthicHandler != null)
            workEthicChange = workEthicHandler.GetWorkEthicChange(labor);
        
        UpdateCityWorkEthic(workEthicChange);

        if (labor == 0) //removing from world dicts when zeroed out
        {
            RemoveLaborFromBuildingDicts(buildingName);
        }

        if (labor != 0)
        {
            world.AddToCurrentBuildingLabor(selectedCityLoc, buildingName, labor);
        }

        selectedCity.UpdateCityPopInfo();

        uiInfoPanelCity.SetData(selectedCity.CityName, selectedCity.cityPop.GetPop, selectedCity.cityPop.GetSetUnusedLabor, selectedCity.GetSetWorkEthic, 
            resourceManager.FoodGrowthLevel, resourceManager.FoodGrowthLimit, selectedCity.CountDownTimer, resourceManager.FoodPerMinute,
            selectedCity.FoodConsumptionPerMinute, selectedCity.GetMinutesTillGrowth, selectedCity.GetResearchPerMinute);

        //resourceManager.UpdateUIGenerationAll();
        BuildingButtonHighlight();
        LaborTileHighlight();
        uiLaborAssignment.UpdateUI(selectedCity.cityPop, placesToWork);
        uiLaborHandler.ShowUI(laborChange, selectedCity, world, placesToWork);
    }

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

            foreach (string cityBuildingName in world.GetBuildingListForCity(selectedCityLoc))
            {
                if (world.CheckBuildingIsProducer(selectedCityLoc, cityBuildingName))
                    world.GetBuildingProducer(selectedCityLoc, cityBuildingName).CalculateResourceGenerationPerMinute();
            }
        }
    }

    private int ChangePlacesToWorkCount(int labor, int maxLabor)
    {
        if (labor == maxLabor) //if decreasing from max amount
            placesToWork++;
        labor += laborChange;
        if (labor == maxLabor) //if increasing to max amount
            placesToWork--;
        return labor;
    }

    private void ChangeCityLaborInfo()
    {
        selectedCity.cityPop.GetSetUnusedLabor -= laborChange;
        selectedCity.cityPop.GetSetUsedLabor += laborChange;
    }

    private void BuildingButtonHighlight()
    {
        foreach (UILaborHandlerOptions option in uiLaborHandler.GetLaborOptions)
        {
            if (world.GetBuildingListForCity(selectedCityLoc).Contains(option.GetBuildingName))
            {
                option.DisableHighlight();
                if (laborChange > 0 && !option.CheckLaborIsMaxxed() && selectedCity.cityPop.GetSetUnusedLabor > 0)
                {
                    option.EnableHighlight(Color.green); //neon green
                }

                if (laborChange < 0 && option.GetCurrentLabor > 0)
                {
                    option.EnableHighlight(Color.red); //neon red
                }
            }
        }
    }

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
                if (laborChange > 0 && !world.CheckIfTileIsMaxxed(tile) && selectedCity.cityPop.GetSetUnusedLabor > 0) //for increasing labor, can't be maxxed out
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
        numberPosition.z += 1.3f; //top center of tile

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

        labor = ChangePlacesToWorkCount(labor, maxLabor);
        selectedCity.cityPop.GetSetFieldLaborers += laborChange;
        ChangeCityLaborInfo();

        ResourceProducer resourceProducer = world.GetResourceProducer(terrainLocation); //cached all resource producers in dict
        //if (!resourceProducer.CheckResourceManager(resourceManager))
        //    resourceProducer.SetResourceManager(resourceManager);

        resourceProducer.UpdateCurrentLaborData(labor);


        if (labor == 0) //removing from world dicts when zeroed out
        {
            RemoveLaborFromDicts(terrainLocation);
            resourceProducer.StopProducing();
        }
        else if (labor == 1 && laborChange > 0) //assigning city to location if working for first time
        {
            world.AddToCityLabor(terrainLocation, selectedCity.gameObject);
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

        //updating all the labor info
        selectedCity.UpdateCityPopInfo();

        uiInfoPanelCity.SetData(selectedCity.CityName, selectedCity.cityPop.GetPop, selectedCity.cityPop.GetSetUnusedLabor, selectedCity.GetSetWorkEthic, 
            resourceManager.FoodGrowthLevel, resourceManager.FoodGrowthLimit, selectedCity.CountDownTimer, resourceManager.FoodPerMinute, 
            selectedCity.FoodConsumptionPerMinute, selectedCity.GetMinutesTillGrowth, selectedCity.GetResearchPerMinute);

        UpdateLaborNumbers();
        //resourceManager.UpdateUIGeneration(terrainSelected.GetTerrainData().resourceType);
        BuildingButtonHighlight();
        LaborTileHighlight();
        uiLaborAssignment.UpdateUI(selectedCity.cityPop, placesToWork);
        //if (world.GetBuildingListForCity(selectedCityLoc).Count > 0)
        uiLaborHandler.ShowUI(laborChange, selectedCity, world, placesToWork);
    }

    private void RemoveLaborFromDicts(Vector3Int terrainLocation)
    {
        world.RemoveFromCurrentWorked(terrainLocation);
        world.RemoveFromCityLabor(terrainLocation);
        //resourceManager.RemoveKeyFromGenerationDict(terrainLocation);
    }

    private void RemoveLaborFromBuildingDicts(string buildingName)
    {
        world.RemoveFromBuildingCurrentWorked(selectedCityLoc, buildingName);
        //resourceManager.RemoveKeyFromBuildingGenerationDict(buildingName);
    }

    private void CheckForWork() //only used for enabling the "add labor" button
    {
        placesToWork = 0;

        foreach (Vector3Int tile in developedTiles)
        {
            if (/*world.CheckIfTileIsImproved(tile) && */!world.CheckIfTileIsMaxxed(tile) /*&& world.CheckIfCityOwnsTile(tile, selectedCity.gameObject)*/)
                placesToWork++;
        }


        foreach (string buildingName in world.GetBuildingListForCity(selectedCityLoc))
        {
            if (!world.CheckIfBuildingIsMaxxed(selectedCityLoc, buildingName))
                placesToWork++;
        }
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
        uiLaborHandler.HideUI();
        //ResetTileLists(); //already in improvement build panel
    }

    public void CloseLaborMenus()
    {
        uiLaborAssignment.ResetLaborAssignment();
        uiLaborHandler.HideUI();
        
        if (!uiImprovementBuildInfoPanel.activeStatus)
            ResetTileLists();
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
        selectedCity = city;
        selectedCityLoc = city.cityLoc;

        if (queuedItem.unitBuildData != null) //build unit
        {
            if (selectedCity.cityPop.GetPop == 1)
            {
                Debug.Log("not enough pop to make unit");
                return;
            }
            CreateUnit(queuedItem.unitBuildData);
        }
        else if (queuedItem.buildLoc.x == 0 && queuedItem.buildLoc.z == 0) //build building
        {
            CreateBuilding(queuedItem.improvementData);
        }
        else //build improvement
        {
            Vector3Int tile = queuedItem.buildLoc + selectedCityLoc;

            if (world.IsBuildLocationTaken(tile) || world.TileHasBuildings(tile) || world.IsRoadOnTerrain(tile))
            {
                Debug.Log("Tile already taken");
                city.RemoveFirstFromQueue(this);
                return;
            }
            BuildImprovement(queuedItem.improvementData, tile);
        }

        city.RemoveFirstFromQueue(this);
    }

    public List<UIQueueItem> GetQueueItems()
    {
        return selectedCity.savedQueueItems;
    }

    public void CloseQueueUI()
    {
        SetQueueStatus(false);
        uiQueueManager.UnselectQueueItem();
        if (uiQueueManager.activeStatus)
            selectedCity.savedQueueItems = uiQueueManager.SetQueueItems();
        uiQueueManager.ToggleVisibility(false);
    }






    public void RunCityNamerUI()
    {
        ResetCityUIToBase();

        uiCityNamer.ToggleVisibility(true, selectedCity);
    }

    public void DestroyCity() //set on destroy city warning message
    {
        CreateUnit(lastUnitData);
        lastUnitData = null;

        GameObject destroyedCity = world.GetStructure(selectedCityLoc);

        world.RemoveStructure(selectedCityLoc);
        world.RemoveCityName(selectedCityLoc);

        Destroy(destroyedCity);

        uiDestroyCityWarning.ToggleVisibility(false);

        ResetCityUI();
    }

    public void NoDestroyCity() //in case user chickens out
    {
        uiDestroyCityWarning.ToggleVisibility(false);
        //uiUnitBuilder.ToggleVisibility(true);
        uiCityTabs.HideSelectedTab();
        lastUnitData = null;
    }

    public void ResetTileLists()
    {
        foreach (Vector3Int tile in tilesToChange)
        {
            world.GetTerrainDataAt(tile).DisableHighlight();
            if (world.CheckIfTileIsImproved(tile))
                world.GetCityDevelopment(tile).DisableHighlight(); //cached for speed 
        }
        tilesToChange = new List<Vector3Int>();
        improvementData = null;
        laborChange = 0;
        removingBuilding = false;
        removingImprovement = false;
    }

    private void ResetCityUI()
    {
        if (selectedCity != null)
        {
            ResourceProducerTimeProgressBarsSetActive(false);
            isActive = false;
            cityTiles.Clear();
            developedTiles.Clear();
            //ResetTileLists();
            removingImprovement = false;
            removingBuilding = false;
            ResetCityUIToBase();
            uiCityTabs.ToggleVisibility(false);
            uiResourceManager.ToggleVisibility(false);
            uiInfoPanelCity.ToggleVisibility(false);
            uiLaborAssignment.HideUI();
            uiUnitTurn.buttonClicked.RemoveListener(ResetCityUI);
            HideLaborNumbers();
            HideBorders();
            ToggleBuildingHighlight(false);
            if (selectedCity != null)
                selectedCity.Deselect();
            placesToWork = 0;
            selectedCityLoc = new();
            selectedCity.activeCity = false;
            selectedCity = null;
        }
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

    //private void GrowTimeProgressBarsPool()
    //{
    //    for (int i = 0; i < 5; i++) //grow pool 5 at a time
    //    {
    //        GameObject gameObject = Instantiate(GameAssets.Instance.timeProgressPrefab);
    //        TimeProgressBar timeProgressBar = gameObject.GetComponent<TimeProgressBar>();
    //        timeProgressBar.transform.rotation = Quaternion.Euler(90, 0, 0); //rotating to lie flat on tile
    //        AddToTimeProgressBarsPool(timeProgressBar);
    //    }
    //}

    //private void AddToTimeProgressBarsPool(TimeProgressBar timeProgressBar)
    //{
    //    timeProgressBar.SetActive(false); //inactivate it when adding to pool
    //    timeProgressQueue.Enqueue(timeProgressBar);
    //}

    //private TimeProgressBar GetFromTimeProgressBarsPool()
    //{
    //    if (timeProgressQueue.Count == 0)
    //        GrowTimeProgressBarsPool();

    //    var timeProgressBar = timeProgressQueue.Dequeue();
    //    timeProgressBar.SetActive(true);
    //    return timeProgressBar;
    //}

    //private void HideTimeProgressBars()
    //{
    //    foreach (TimeProgressBar timeProgressBar in timeProgressList)
    //    {
    //        AddToTimeProgressBarsPool(timeProgressBar);
    //    }

    //    timeProgressList.Clear();
    //}
}
