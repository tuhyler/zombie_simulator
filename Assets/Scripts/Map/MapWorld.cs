using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class MapWorld : MonoBehaviour
{
    [SerializeField]
    private UIWorldResources uiWorldResources;
    [SerializeField]
    private UIResearchTreePanel researchTree;
    [SerializeField]
    private UISingleConditionalButtonHandler wonderButton, uiConfirmWonderBuild, uiRotateWonder;
    [SerializeField]
    private UIWonderHandler wonderHandler;
    [SerializeField]
    private UIBuildingSomething uiBuildingSomething;
    [SerializeField]
    private UnitMovement unitMovement;
    [SerializeField]
    private CityBuilderManager cityBuilderManager;
    [SerializeField]
    private RoadManager roadManager;

    private WorldResourceManager worldResourceManager;
    private WonderDataSO wonderData;
    [SerializeField]
    private UnityEvent<WonderDataSO> OnIconButtonClick;
    private List<Vector3Int> wonderPlacementLoc = new();
    private int rotationCount;
    private Vector3Int unloadLoc;
    private Dictionary<string, Wonder> wonderConstructionDict = new();
    private Dictionary<Vector3Int, Wonder> wonderStopDict = new();

    [HideInInspector]
    public bool researching;

    private List<City> researchWaitList = new();

    private Dictionary<Vector3Int, TerrainData> world = new();
    private Dictionary<Vector3Int, GameObject> buildingPosDict = new(); //to see if cities already exist in current location
    private List<Vector3Int> cityLocations = new();

    private Dictionary<Vector3Int, City> cityDict = new(); //caching cities for easy reference
    //private Dictionary<Vector3Int, City> cityHarborDict = new(); //cities and the respective locations of their harbors
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
    //private Dictionary<Vector3Int, GameObject> traderPosDict = new(); //to track trader locations 
    //private Dictionary<Vector3Int, List<GameObject>> multiUnitPosDict = new(); //to handle multiple units in one spot

    //for assigning labor in cities
    private Dictionary<Vector3Int, int> currentWorkedTileDict = new(); //to see how much labor is assigned to tile
    private Dictionary<Vector3Int, int> maxWorkedTileDict = new(); //the max amount of labor that can be assigned to tile
    private Dictionary<Vector3Int, GameObject> cityWorkedTileDict = new(); //the city worked tiles belong to
    private Dictionary<Vector3Int, ResourceProducer> cityImprovementProducerDict = new(); //all the improvements that have resource producers (for speed)
    private Dictionary<Vector3Int, Dictionary<string, int>> cityBuildingCurrentWorkedDict = new(); //current worked for buildings in city

    private Dictionary<Vector3Int, Dictionary<string, int>> cityBuildingMaxWorkedDict = new(); //max labor of buildings within city
    private Dictionary<Vector3Int, List<string>> cityBuildingList = new(); //list of buildings on city tiles (here instead of City because buildings can be without a city)
    //private Dictionary<Vector3Int, Dictionary<string, ResourceProducer>> cityBuildingIsProducer = new(); //all the buildings that are resource producers (for speed)

    //for workers
    private List<Vector3Int> workerBusyLocations = new();

    //for roads
    private Dictionary<Vector3Int, List<GameObject>> roadTileDict = new(); //stores road GOs, only on terrain locations
    private List<Vector3Int> soloRoadLocsList = new(); //indicates which tiles have solo roads on them
    private List<Vector3Int> roadLocsList = new(); //indicates which tiles have roads on them
    private int roadCost; //set in road manager

    //for terrain speeds
    public TerrainDataSO flatland, forest, hill, forestHill;
    
    //for expanding gameobject size
    private static int increment = 3;

    [SerializeField] //for gizmos
    private bool showGizmo;

    [HideInInspector]
    public bool buildingRoad, buildingWonder;
    //private bool showObstacle, showDifficult, showGround, showSea;

    //for naming of units
    [HideInInspector]
    public int workerCount, infantryCount;

    private void Awake()
    {
        worldResourceManager = GetComponent<WorldResourceManager>();
        worldResourceManager.SetUI(uiWorldResources);
        if (researching)
            SetResearchName(researchTree.GetChosenResearchName());
        else
            SetResearchName("No Current Research");
    }

    private void Start()
    {
        wonderButton.ToggleTweenVisibility(true);
        uiWorldResources.SetActiveStatus(true);

        foreach (TerrainData td in FindObjectsOfType<TerrainData>())
        {
            Vector3Int tileCoordinate = td.GetTileCoordinates();
            world[tileCoordinate] = td; 

            foreach (Vector3Int tile in neighborsEightDirections)
            {
                world[tileCoordinate + tile] = td;
            }

        }

        foreach (Unit unit in FindObjectsOfType<Unit>()) //adds all units and their locations to start game.
        {
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
                data.locked = false;
            else
                data.locked = true;
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
    }

    public void UnselectAll()
    {
        cityBuilderManager.ResetCityUI();
        unitMovement.ClearSelection();
        cityBuilderManager.UnselectWonder();
    }

    public void GiveWarningMessage(string message)
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; //z must be more than 0, else just gives camera position
        Vector3 mouseLoc = Camera.main.ScreenToWorldPoint(mousePos);

        InfoPopUpHandler.Create(mouseLoc, message);
    }

    //wonder info
    public void HandleWonderPlacement(Vector3 location, GameObject detectedObject)
    {
        if (buildingWonder) //only works is placing wonder
        {
            Vector3Int locationPos = GetClosestTerrainLoc(location);

            if (wonderPlacementLoc.Count > 0)
            {
                foreach (Vector3Int tile in wonderPlacementLoc)
                {
                    GetTerrainDataAt(tile).DisableHighlight();
                }

                if (locationPos == wonderPlacementLoc[0])
                {
                    wonderPlacementLoc.Clear();
                    uiConfirmWonderBuild.ToggleTweenVisibility(false);
                    uiRotateWonder.ToggleTweenVisibility(false);
                    return;
                }
            }

            wonderPlacementLoc.Clear();
            rotationCount = 0;
            List<Vector3Int> wonderLocList = new();

            for (int i = 0; i < wonderData.sizeWidth; i++)
            {
                for (int j = 0; j < wonderData.sizeHeight; j++)
                {
                    Vector3Int newPos = locationPos;
                    newPos.z += i*increment;
                    newPos.x += j*increment;
                    TerrainData td = GetTerrainDataAt(newPos);

                    if (td.terrainData.type != wonderData.terrainType)
                    {
                        GiveWarningMessage("Must build on " + wonderData.terrainType);
                        return;
                    }
                    else if (!IsTileOpenCheck(newPos))
                    {
                        GiveWarningMessage("Something in the way");
                        return;
                    }

                    wonderLocList.Add(newPos);
                }
            }

            foreach (Vector3Int tile in wonderLocList)
            {
                if (tile-locationPos == wonderData.unloadLoc)
                {
                    GetTerrainDataAt(tile).EnableHighlight(new Color(0, 1, 0, 0.2f));
                    unloadLoc = tile;
                }
                else
                    GetTerrainDataAt(tile).EnableHighlight(new Color(1, 1, 1, 0.2f));
            }

            //GameObject wonderGhostGO = Instantiate(wonderData.prefabComplete);
            //foreach ()

            uiConfirmWonderBuild.ToggleTweenVisibility(true);
            uiRotateWonder.ToggleTweenVisibility(true);
            wonderPlacementLoc = wonderLocList;
        }
    }

    public void SetWonderConstruction()
    {
        //resetting ui
        if (wonderPlacementLoc.Count > 0)
        {
            foreach (Vector3Int tile in wonderPlacementLoc)
            {
                GetTerrainDataAt(tile).DisableHighlight();
            }
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
        foreach (Vector3Int tile in wonderPlacementLoc)
        {
            avgLoc += tile;

            if (!IsTileOpenCheck(tile))
            {
                GiveWarningMessage("Something in the way");
                wonderPlacementLoc.Clear();
                return;
            }
        }

        //setting up wonder info
        Vector3 centerPos = avgLoc / wonderPlacementLoc.Count;
        GameObject wonderGO = Instantiate(wonderData.prefab0Percent, centerPos, Quaternion.identity);
        Wonder wonder = wonderGO.GetComponent<Wonder>();
        wonder.WonderData = wonderData;
        wonder.wonderName = "Wonder - " + wonderData.wonderName;
        wonder.SetResourceDict(wonderData.wonderCost);
        wonder.unloadLoc = unloadLoc;
        wonder.centerPos = centerPos;
        wonderConstructionDict[wonder.wonderName] = wonder;
        wonderStopDict[unloadLoc] = wonder;
        //building road in unload area
        roadManager.BuildRoadAtPosition(unloadLoc);

        //claiming the area for the wonder
        List<Vector3Int> harborTiles = new();
        foreach (Vector3Int tile in wonderPlacementLoc)
        {
            AddToCityLabor(tile, wonderGO); //so cities can't take the spot
            AddStructure(tile, wonderGO); //so nothing else can be built there

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
        wonderPlacementLoc.Clear();
    }

    public void PlaceWonder(WonderDataSO wonderData)
    {
        buildingWonder = true;
        this.wonderData = wonderData;
        CloseWonders();

        uiBuildingSomething.SetText("Building " + wonderData.wonderName);
        uiBuildingSomething.ToggleVisibility(true);
        unitMovement.ToggleCancelButton(true);
    }

    public void RotateWonderPlacement()
    {
        rotationCount++;
        Vector3Int placementLoc = wonderPlacementLoc[0];

        if (rotationCount % 4 == 1)
            placementLoc.z += (wonderData.sizeHeight - 1) * increment;
        if (rotationCount % 4 == 2)
        {
            placementLoc.z += (wonderData.sizeHeight - 1) * increment;
            placementLoc.x += (wonderData.sizeWidth - 1) * increment;
        }
        if (rotationCount % 4 == 3)
            placementLoc.x += (wonderData.sizeWidth - 1) * increment;

        foreach (Vector3Int tile in wonderPlacementLoc)
        {
            GetTerrainDataAt(tile).DisableHighlight();

            if (tile-placementLoc == wonderData.unloadLoc)
                GetTerrainDataAt(tile).EnableHighlight(new Color(0, 1, 0, 0.2f));
            else
                GetTerrainDataAt(tile).EnableHighlight(new Color(1, 1, 1, 0.2f));
        }

        unloadLoc = placementLoc;
    }

    public void CloseBuildingSomethingPanel()
    {
        if (wonderPlacementLoc.Count > 0)
        {
            foreach (Vector3Int tile in wonderPlacementLoc)
            {
                GetTerrainDataAt(tile).DisableHighlight();
            }

            wonderPlacementLoc.Clear();
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

    //Research info
    public void OpenResearchTree()
    {
        if (researchTree.activeStatus)
            researchTree.ToggleVisibility(false);
        else
            researchTree.ToggleVisibility(true);
    }

    public void CloseResearchTree()
    {
        researchTree.ToggleVisibility(false);
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
            worldResourceManager.SetResource(resourceType, amount);
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


    public Unit GetUnit(Vector3Int tile)
    {
        return unitPosDict[tile].GetComponent<Unit>();
    }

    public bool IsUnitWaitingForSameStop(Vector3Int tile, Vector3 finalDestination)
    {
        if (!unitPosDict.ContainsKey(tile))
            return false;

        Unit tempUnit = unitPosDict[tile];

        if (tempUnit.isWaiting && tempUnit.FinalDestinationLoc == finalDestination)
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

        return names;
    }

    public Vector3Int GetStopLocation(string name)
    {
        if (cityNameDict.ContainsKey(name))
            return cityNameDict[name];
        else
            return wonderConstructionDict[name].unloadLoc;
    }

    public City GetHarborCity(Vector3Int harborLocation)
    {
        return cityDict[harborLocation];
    }

    public bool CheckIfStopStillExists(Vector3Int location)
    {
        if (cityDict.ContainsKey(location))
            return true;
        else if (wonderStopDict.ContainsKey(location))
            return true;

        return false;
    }

    public Vector3Int GetHarborStopLocation(string name)
    {
        if (cityNameDict.ContainsKey(name))
            return cityDict[cityNameDict[name]].harborLocation;
        else
            return wonderConstructionDict[name].harborLoc;
    }

    public string GetStopName(Vector3Int cityLoc)
    {
        if (cityDict.ContainsKey(cityLoc))
        {
            return cityDict[cityLoc].CityName;
        }
        else
        {
            return wonderStopDict[cityLoc].wonderName;
        }
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

    public GameObject GetRoads(Vector3Int tile, bool straight)
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

    public List<GameObject> GetAllRoadsOnTile(Vector3Int tile)
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

    public void SetCityBuilding(ImprovementDataSO improvementData, Vector3Int cityTile, GameObject building, City city, bool isInitialCityHouse)
    {
        CityImprovement improvement = building.GetComponent<CityImprovement>();
        improvement.InitializeImprovementData(improvementData);
        string buildingName = improvementData.improvementName;
        improvement.SetCity(city);
        improvement.initialCityHouse = isInitialCityHouse;
        cityBuildingGODict[cityTile][buildingName] = building;
        cityBuildingDict[cityTile][buildingName] = improvement;
        cityBuildingList[cityTile].Add(buildingName);
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

    public void RemoveLocationFromQueueList(Vector3Int location)
    {
        cityImprovementQueueList.Remove(location);  
    }

    public void SetRoads(Vector3Int tile, GameObject road, bool straight)
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

    public bool IsTradeLocOnTile(Vector3Int tile)
    {
        if (cityDict.ContainsKey(tile))
            return true;
        else if (wonderStopDict.ContainsKey(tile))
            return true;

        return false;
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
    public bool CheckIfPositionIsValid(Vector3Int tileWorldPosition)
    {
        return world.ContainsKey(tileWorldPosition) && world[tileWorldPosition].GetTerrainData().walkable;
    }

    public bool CheckIfSeaPositionIsValid(Vector3Int tileWorldPosition)
    {
        return world.ContainsKey(tileWorldPosition) && world[tileWorldPosition].GetTerrainData().sailable;
    }

    public bool CheckIfSeaPositionIsRiverOrCoast(Vector3Int tileWorldPosition)
    {
        return world[tileWorldPosition].GetTerrainData().type == TerrainType.River || world[tileWorldPosition].GetTerrainData().type == TerrainType.Coast;
    }

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

    public enum State { FOURWAY, FOURWAYINCREMENT, EIGHTWAY, EIGHTWAYTWODEEP, CITYRADIUS };

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

    public (List<(Vector3Int, bool, int[])>, int[], int[]) GetRoadNeighborsFor(Vector3Int position)
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
                int neighborCount = 0;

                List<Vector3Int> neighborDirectionList = straightFlag ? neighborsFourDirectionsIncrement : neighborsDiagFourDirectionsIncrement;
                foreach (Vector3Int neighborDirection in neighborDirectionList)
                {
                    if (roadTileDict.ContainsKey(neighbor + neighborDirection))
                    {
                        neighborRoads[j] = 1;
                        neighborCount++;
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

    public Vector3Int GetClosestTerrainLoc(Vector3 v)
    {
        //c sharp rounds to the closest even number at the midpoint. 
        v.y = 0f;
        v.x = (float)Math.Round(v.x, MidpointRounding.AwayFromZero);
        v.z = (float)Math.Round(v.z, MidpointRounding.AwayFromZero);

        return world[Vector3Int.RoundToInt(v)].GetTileCoordinates();
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

    public void AddStructure(Vector3 buildPosition, GameObject structure) //method to add building to dict
    {
        Vector3Int position = Vector3Int.RoundToInt(buildPosition);
        if (buildingPosDict.ContainsKey(position))
        {
            Debug.LogError($"There is a structure already at this position {buildPosition}");
            return;
        }

        buildingPosDict[position] = structure;
    }

    public void AddCity(Vector3 buildPosition, City city)
    {
        Vector3Int position = Vector3Int.RoundToInt(buildPosition);
        cityLocations.Add(position);
        cityDict[position] = city;

        foreach (Vector3Int tile in neighborsFourDirections)
        {
            cityLocations.Add(tile + position);
        }
    }

    public void AddResourceProducer(Vector3 buildPosition, ResourceProducer resourceProducer)
    {
        Vector3Int position = Vector3Int.RoundToInt(buildPosition);
        cityImprovementProducerDict[position] = resourceProducer;
    }

    public void AddCityBuildingDict(Vector3 cityPos)
    {
        Vector3Int cityTile = Vector3Int.RoundToInt(cityPos);
        cityBuildingGODict[cityTile] = new Dictionary<string, GameObject>();
        cityBuildingDict[cityTile] = new Dictionary<string, CityImprovement>();
        cityBuildingCurrentWorkedDict[cityTile] = new Dictionary<string, int>();
        cityBuildingMaxWorkedDict[cityTile] = new Dictionary<string, int>();
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
        cityBuildingMaxWorkedDict[cityTile].Remove(buildingName);
        cityBuildingList[cityTile].Remove(buildingName);
        //cityBuildingIsProducer[cityTile].Remove(buildingName);
    }

    public void RemoveCityName(Vector3Int cityLoc)
    {
        string cityName = cityLocDict[cityLoc];
        cityNameDict.Remove(cityName);
        cityLocDict.Remove(cityLoc);
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
            foreach (string building in cityBuildingGODict[buildPosition].Keys)
            {
                Destroy(cityBuildingGODict[buildPosition][building]);
            }
            
            cityBuildingGODict.Remove(buildPosition);
            cityBuildingDict.Remove(buildPosition);
            cityBuildingMaxWorkedDict.Remove(buildPosition);
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
        Vector3Int position = Vector3Int.RoundToInt(unitPosition);

        unitPosDict[position] = unit;

        return position;
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

    public void AddToCityMaxLaborDict(Vector3Int cityTile, string buildingName, int max)
    {
        cityBuildingMaxWorkedDict[cityTile][buildingName] = max;
    }

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

