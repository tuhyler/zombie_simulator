using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MapWorld : MonoBehaviour
{
    [SerializeField]
    private UIWorldResources uiWorldResources;

    private WorldResourceManager worldResourceManager;
    
    private Dictionary<Vector3Int, TerrainData> world = new();
    private Dictionary<Vector3Int, GameObject> buildingPosDict = new(); //to see if cities already exist in current location
    private List<Vector3Int> cityLocations = new();

    private Dictionary<Vector3Int, City> cityDict = new(); //caching cities for easy reference
    private Dictionary<Vector3Int, City> cityHarborDict = new(); //cities and the respective locations of their harbors
    private Dictionary<Vector3Int, CityImprovement> cityImprovementDict = new(); //all the City development prefabs
    private Dictionary<Vector3Int, Dictionary<string, CityImprovement>> cityBuildingDict = new(); //all the buildings for highlighting
    private Dictionary<Vector3Int, Dictionary<string, GameObject>> cityBuildingGODict = new(); //all the buildings and info within a city 
    private Dictionary<string, Vector3Int> cityNameDict = new();
    private Dictionary<Vector3Int, string> cityLocDict = new();
    private Dictionary<Vector3Int, Unit> unitPosDict = new(); //to track unitGO locations
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
    private Dictionary<Vector3Int, Dictionary<string, ResourceProducer>> cityBuildingIsProducer = new(); //all the buildings that are resource producers (for speed)

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
    //private bool showObstacle, showDifficult, showGround, showSea;

    private void Awake()
    {
        worldResourceManager = GetComponent<WorldResourceManager>();
        worldResourceManager.SetUI(uiWorldResources);
    }

    private void Start()
    {
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
    }

    //world resources management
    public void UpdateWorldResources(ResourceType resourceType, int amount)
    {
        worldResourceManager.SetResource(resourceType, amount);
    }

    public void UpdateWorldResourceGeneration(ResourceType resourceType, float amount, bool add)
    {
        worldResourceManager.ModifyResourceGenerationPerMinute(resourceType, amount, add);
    }

    public List<ResourceType> WorldResourcePrep()
    {
        return worldResourceManager.PassWorldResources();
    }

    public void UpdateWorldResourceUI(ResourceType resourceType, int diffAmount)
    {
        worldResourceManager.UpdateUIGeneration(resourceType, diffAmount);
    }


    public Unit GetUnit(Vector3Int tile)
    {
        return unitPosDict[tile].GetComponent<Unit>();
    }

    public bool IsUnitWaitingForSameCity(Vector3Int tile, Vector3 finalDestination)
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

    public List<string> GetConnectedCityNames(Vector3Int unitLoc, bool bySea)
    {
        List<string> names = new();

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

    public Vector3Int GetCityLocation(string cityName)
    {
        return cityNameDict[cityName];
    }

    public City GetHarborCityLocation(Vector3Int harborLocation)
    {
        return cityHarborDict[harborLocation];
    }

    public Vector3Int GetCityHarborLocation(string cityName)
    {
        return cityDict[cityNameDict[cityName]].harborLocation;
    }

    public string GetCityName(Vector3Int cityLoc, bool bySea = false)
    {
        if (bySea)
            return cityHarborDict[cityLoc].CityName;
        else
            return cityLocDict[cityLoc];
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

    public ResourceProducer GetBuildingProducer(Vector3Int cityTile, string buildingName)
    {
        return cityBuildingIsProducer[cityTile][buildingName];
    }

    public CityImprovement GetCityDevelopment(Vector3Int tile)
    {
        return cityImprovementDict[tile];
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

    public void SetTerrainData(Vector3Int tile, TerrainData td)
    {
        world[tile] = td;
    }

    public void SetCityDevelopment(Vector3 tile, CityImprovement cityDevelopment)
    {
        Vector3Int position = Vector3Int.RoundToInt(tile);
        cityImprovementDict[position] = cityDevelopment;
    }

    public void SetCityBuilding(Vector3Int cityTile, string buildingName, GameObject building)
    {
        cityBuildingGODict[cityTile][buildingName] = building;
        cityBuildingDict[cityTile][buildingName] = building.GetComponent<CityImprovement>();
        cityBuildingList[cityTile].Add(buildingName);
    }

    public void SetCityHarbor(City city, Vector3Int harborLoc)
    {
        cityHarborDict[harborLoc] = city;
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

    public bool TileHasBuildings(Vector3Int cityTile)
    {
        if (!cityBuildingGODict.ContainsKey(cityTile))
        {
            return false;
        }

        if (cityBuildingGODict[cityTile].Count > 0)
            return true;
        else
            return false;
    }



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

    public Vector3Int GetClosestTile(Vector3 worldPosition)
    {
        worldPosition.y = 0;
        return Vector3Int.RoundToInt(worldPosition);
    }

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

    public enum State { FOURWAY, FOURWAYINCREMENT, EIGHTWAY, EIGHTWAYTWODEEP };

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
                return new(cityRadius);
        }

        return neighbors;
    }

    public (List<Vector3Int>, List<Vector3Int>) GetCityRadiusFor(Vector3Int worldTilePosition, GameObject city) //two ring layer around specific city
    {
        List<Vector3Int> neighbors = new();
        List<Vector3Int> developed = new();
        foreach (Vector3Int direction in cityRadius)
        {
            Vector3Int checkPosition = worldTilePosition + direction;
            if (world.ContainsKey(checkPosition)) //checking if it exists in world
            {
                if (cityWorkedTileDict.ContainsKey(checkPosition) && GetCityLaborForTile(checkPosition) != city)
                    continue;

                neighbors.Add(checkPosition);
                if (CheckIfTileIsImproved(checkPosition))
                    developed.Add(checkPosition);
            }

        }
        return (neighbors, developed);
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

    //public TerrainData GetTerrainData(Vector3Int tileLoc)
    //{
    //    return world[tileLoc];
    //}

    public Vector3Int GetClosestTerrainLoc(Vector3 worldPosition)
    {
        return world[GetClosestTile(worldPosition)].GetTileCoordinates();
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
        cityBuildingIsProducer[cityTile] = new Dictionary<string, ResourceProducer>();
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
        cityBuildingIsProducer[cityTile].Remove(buildingName);
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
        if (cityBuildingGODict.ContainsKey(buildPosition) && cityBuildingGODict[buildPosition].Count == 0) //buildings will stay if city abandoned
        {
            cityBuildingGODict.Remove(buildPosition);
            cityBuildingDict.Remove(buildPosition);
            cityBuildingMaxWorkedDict.Remove(buildPosition);
            cityBuildingList.Remove(buildPosition);
            cityBuildingIsProducer.Remove(buildPosition);

            cityLocations.Remove(buildPosition);
            cityDict.Remove(buildPosition);
            foreach (Vector3Int tile in neighborsEightDirections)
            {
                cityLocations.Remove(buildPosition + tile);
            }
        }
    }

    public void RemoveHarbor(Vector3Int harborLoc)
    {
        cityHarborDict.Remove(harborLoc);
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

    public void AddToCityBuildingIsProducerDict(Vector3Int cityTile, string buildingName, ResourceProducer resourceProducer)
    {
        cityBuildingIsProducer[cityTile][buildingName] = resourceProducer;
    }

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

    public int GetMaxLaborForBuilding(Vector3Int cityTile, string buildingName)
    {
        return cityBuildingMaxWorkedDict[cityTile][buildingName];
    }

    public List<string> GetBuildingListForCity(Vector3Int cityTile)
    {
        return cityBuildingList[cityTile];
    }

    public bool CheckImprovementIsProducer(Vector3Int pos)
    {
        return cityImprovementProducerDict.ContainsKey(pos);
    }

    public bool CheckBuildingIsProducer(Vector3Int cityTile, string buildingName)
    {
        return cityBuildingIsProducer[cityTile].ContainsKey(buildingName);
    }

    private GameObject GetCityLaborForTile(Vector3Int pos)
    {
        return cityWorkedTileDict[pos];
    }

    public bool CheckIfTileIsWorked(Vector3Int pos)
    {
        return currentWorkedTileDict.ContainsKey(pos);
    }

    public bool CheckIfTileIsImproved(Vector3Int pos)
    {
        return maxWorkedTileDict.ContainsKey(pos);
    }

    public bool CheckIfTileIsMaxxed(Vector3Int pos)
    {
        if (currentWorkedTileDict.ContainsKey(pos))
            return maxWorkedTileDict[pos] == currentWorkedTileDict[pos];
        return false;
    }

    public bool CheckIfBuildingIsMaxxed(Vector3Int cityTile, string buildingName)
    {
        if (cityBuildingCurrentWorkedDict[cityTile].ContainsKey(buildingName))
            return cityBuildingMaxWorkedDict[cityTile][buildingName] == cityBuildingCurrentWorkedDict[cityTile][buildingName];
        return false;
    }

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
        if (maxWorkedTileDict.ContainsKey(pos))
            maxWorkedTileDict.Remove(pos);
    }

    public void RemoveFromCityLabor(Vector3Int pos)
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

