using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class City : MonoBehaviour
{
    [SerializeField]
    private GameObject housingPrefab;
    [SerializeField]
    private ImprovementDataSO housingData;
    private GameObject currentHouse;
    private CityBuilderManager cityBuilderManager;

    [SerializeField]
    private CityNameField cityNameField;

    //particle systems
    [SerializeField]
    private ParticleSystem heavenHighlight, resourceSplash, lightBullet;

    //[SerializeField]
    //private Material cityNameMaterial;

    //private Material originalCityNameMaterial;//, originalCityStatMaterial;

    //[SerializeField]
    //private SpriteRenderer cityNameField, citySizeField;//, unusedLaborField;

    //[SerializeField]
    //private TMP_Text cityName, cityPopText;
    private string cityName;
    public string CityName { get { return cityName; } }

    [HideInInspector]
    public Vector3Int cityLoc;
    [HideInInspector]
    public bool activeCity, hasHarbor;

    [HideInInspector]
    public Vector3Int harborLocation;
    
    private MapWorld world;

    private ResourceManager resourceManager;
    public ResourceManager ResourceManager { get { return resourceManager; } }

    [HideInInspector]
    public Dictionary<string, Vector3Int> singleBuildImprovementsBuildingsDict = new();

    //private ResourceProducer resourceProducer;

    [HideInInspector]
    public CityPopulation cityPop;

    //foodConsumed info
    public int unitFoodConsumptionPerMinute = 1, secondsTillGrowthCheck = 60; //how much foodConsumed one unit eats per turn
    private int foodConsumptionPerMinute; //total 
    public int FoodConsumptionPerMinute { get { return foodConsumptionPerMinute; } set { foodConsumptionPerMinute = value; } }
    private string minutesTillGrowth; //string in case there's no growth
    public string GetMinutesTillGrowth { get { return minutesTillGrowth; } }
    //private TimeProgressBar timeProgressBar;
    private UITimeProgressBar uiTimeProgressBar;
    private int countDownTimer;

    //housingInfo
    private int housingCount = 2;
    public int HousingCount { get { return housingCount; } set { housingCount = value; } }

    //resource info
    private float workEthic = 1.0f;
    public float GetSetWorkEthic { get { return workEthic; } set { workEthic = value; } }
    public int warehouseStorageLimit = 200;
    private TradeRouteManager tradeRouteWaiter;
    private ResourceType resourceWaiter = ResourceType.None;
    private Queue<Unit> waitList = new();
    private Dictionary<ResourceType, int> resourcesWorkedDict = new();
    
    //world resource info
    //private int goldPerMinute;
    //public int GetGoldPerMinute { get { return goldPerMinute; } }
    private int researchPerMinute;
    public int GetResearchPerMinute { get { return researchPerMinute; } }

    //resource priorities
    private bool autoAssignLabor;
    public bool AutoAssignLabor { get { return autoAssignLabor; } set { autoAssignLabor = value; } }
    private List<ResourceType> resourcePriorities = new();
    public List<ResourceType> ResourcePriorities { get { return resourcePriorities; } set { resourcePriorities = value; } }

    //stored queue items
    [HideInInspector]
    public List<UIQueueItem> savedQueueItems = new();
    [HideInInspector]
    public List<string> savedQueueItemsNames = new();
    [HideInInspector]
    public Dictionary<string, GameObject> buildingQueueGhostDict = new();

    //private SelectionHighlight highlight; //Highlight doesn't work on city name text

    private void Awake()
    {
        //selectionCircle.GetComponent<MeshRenderer>().enabled = false;
        world = FindObjectOfType<MapWorld>();
        cityPop = GetComponent<CityPopulation>();
        resourceManager = GetComponent<ResourceManager>();
        resourceManager.ResourceStorageLimit = warehouseStorageLimit;
        //resourceProducer = GetComponent<ResourceProducer>();
        resourceManager.SetCity(this);
        //resourceProducer.SetResourceManager(resourceManager);

        cityLoc = Vector3Int.RoundToInt(transform.position);

        SetProgressTimeBar();
        //highlight = GetComponent<SelectionHighlight>();

        resourceManager.ResourceDict = new(world.GetBlankResourceDict());
        resourceManager.ResourcePriceDict = new(world.GetDefaultResourcePrices());
        resourceManager.ResourceSellDict = new(world.GetBoolResourceDict());
        resourceManager.ResourceMinHoldDict = new(world.GetBlankResourceDict());
        resourceManager.ResourceSellHistoryDict = new(world.GetBlankResourceDict());
    }

    private void Start()
    {
        UpdateCityPopInfo();
        if (cityPop.CurrentPop >= 1)
            StartCoroutine(FoodConsumptionCoroutine());

        foodConsumptionPerMinute = cityPop.CurrentPop * unitFoodConsumptionPerMinute - 1; //first pop is free
        countDownTimer = secondsTillGrowthCheck;

        cityNameField.ToggleVisibility(false);
        InstantiateParticleSystems();
        //Physics.IgnoreLayerCollision(6,7);
    }

    public void InstantiateParticleSystems()
    {
        Vector3 pos = transform.position;
        resourceSplash = Instantiate(resourceSplash, pos, Quaternion.Euler(-90, 0, 0));
        resourceSplash.Pause();
        pos.y = 8f;
        lightBullet = Instantiate(lightBullet, pos, Quaternion.Euler(90, 0, 0));
        lightBullet.Pause();
        pos.y = 3f;
        heavenHighlight = Instantiate(heavenHighlight, pos, Quaternion.identity);
        heavenHighlight.Pause();
    }

    public void SetCityBuilderManager(CityBuilderManager cityBuilderManager)
    {
        this.cityBuilderManager = cityBuilderManager;
    }

    public void UpdateResourceInfo()
    {
        
        cityBuilderManager.UpdateResourceInfo();
    }

    public bool WorldResearchingCheck()
    {
        return world.researching;
    }

    public void RestartResearch()
    {
        resourceManager.CheckProducerUnloadResearchWaitList();
    }

    public void RestartProduction()
    {
        resourceManager.CheckProducerResourceWaitList(ResourceType.Gold);
    }

    public void AddToWorldResearchWaitList()
    {
        world.AddToResearchWaitList(this);
    }

    public void AddToWorldGoldWaitList()
    {
        world.AddToGoldCityWaitList(this);
    }

    private void EnableHighlight()
    {
        cityNameField.EnableHighlight();
    }

    private void DisableHighlight()
    {
        cityNameField.DisableHighlight();
    }

    public bool CheckCityName(string cityName)
    {
        return world.IsCityNameTaken(cityName);
    }

    public void SetNewCityName()
    {
        bool approvedName = false;
        int cityCount = world.CityCount();
        string cityName = "";

        while (!approvedName)
        {
            cityCount++;
            cityName = "City_" + cityCount.ToString();

            if (!world.CheckCityName(cityName))
            {
                approvedName = true;
            }
        }

        SetCityNameFieldSize(cityName);
        SetCityName(cityName);
        SetCityPop();
        AddCityNameToWorld();
    }

    private void SetCityNameFieldSize(string cityName)
    {
        cityNameField.SetCityNameFieldSize(cityName);
    }

    public void SetCityName(string cityName)
    {
        this.cityName = cityName;
        cityNameField.SetCityName(cityName);
    }

    public void AddCityNameToWorld()
    {
        world.AddCityName(cityName, cityLoc);
    }

    private void SetCityPop()
    {
        cityNameField.SetCityPop(cityPop.CurrentPop);
    }

    public void RemoveCityName()
    {
        world.RemoveCityName(cityLoc);
    }

    public void UpdateCityName(string newCityName)
    {
        SetCityNameFieldSize(newCityName);
        SetCityName(newCityName);
        AddCityNameToWorld();
    }

    //update city info after changing foodConsumed info
    public void UpdateCityPopInfo()
    {
        //cityPopText.text = cityPop.CurrentPop.ToString();
        //cityLaborText.text = cityPop.UnusedLabor.ToString();
        resourceManager.ModifyResourceConsumptionPerMinute(ResourceType.Food, foodConsumptionPerMinute, true);
        float foodPerMinute = resourceManager.GetResourceGenerationValues(ResourceType.Food);
        int foodStorage = resourceManager.FoodGrowthLevel;

        if (foodPerMinute > 0)
        {
            minutesTillGrowth = Mathf.CeilToInt(((float)resourceManager.FoodGrowthLimit - foodStorage) / foodPerMinute).ToString();
        }
        else if (foodPerMinute < 0) 
        {
            minutesTillGrowth = Mathf.FloorToInt(foodStorage / foodPerMinute).ToString(); //maybe take absolute value, change color to red?
        }
        else if (foodPerMinute == 0)
        {
            minutesTillGrowth = "-";
        }
    }

    public void SetHouse(Vector3Int cityLoc)
    {
        if (currentHouse != null)
            Destroy(currentHouse);

        Vector3 houseLoc = cityLoc;
        houseLoc.z -= 1f;
        GameObject housing = Instantiate(housingPrefab, houseLoc, Quaternion.identity);
        world.SetCityBuilding(housingData, cityLoc, housing, this, true);
    }

    public void PopulationGrowthCheck(bool joinCity)
    {
        cityPop.IncreasePopulationAndLabor();
        heavenHighlight.Play();
        SetCityPop();
        foodConsumptionPerMinute = cityPop.CurrentPop * unitFoodConsumptionPerMinute - 1;
        if (cityPop.CurrentPop > 1)
            resourceManager.IncreaseFoodConsumptionPerTurn(true);
        
        if (joinCity)
            UpdateCityPopInfo();

        if (autoAssignLabor)
        {
            AutoAssignmentsForLabor();
            if (activeCity)
            {
                cityBuilderManager.UpdateCityLaborUIs();
            }
        }

        if (cityPop.CurrentPop == 1)
        {
            if (activeCity)
            {
                CityGrowthProgressBarSetActive(true);
                cityBuilderManager.abandonCityButton.interactable = false;
            }
            cityNameField.ToggleVisibility(true);
            resourceManager.SellResources();
            StartCoroutine(FoodConsumptionCoroutine());
        }
    }

    public void PopulationDeclineCheck()
    {
        cityPop.CurrentPop--;
        SetCityPop();
        foodConsumptionPerMinute = cityPop.CurrentPop * unitFoodConsumptionPerMinute - 1;
        if (cityPop.CurrentPop == 0)
        {
            StopAllCoroutines();
            if (activeCity)
            {
                CityGrowthProgressBarSetActive(false);
                cityBuilderManager.abandonCityButton.interactable = true;
            }
        }

        if (cityPop.UnusedLabor > 0) //if unused labor, get rid of first
            cityPop.UnusedLabor--;
        else
        {
            //StopFoodConsumptionCoroutine();
            System.Random random = new();
            //int randomLabor = random.Next(cityPop.UsedLabor); //randomly choosing by weight between field and city labor

            //if (randomLabor < cityPop.GetSetFieldLaborers)
            RemoveRandomFieldLaborer(random);
            //else
            //    RemoveRandomCityLaborer(random);
        }

        if (cityPop.CurrentPop > 0)
            resourceManager.IncreaseFoodConsumptionPerTurn(false);
        UpdateCityPopInfo();
    }

    private void RemoveRandomFieldLaborer(System.Random random)
    {
        List<Vector3Int> workedTiles = world.GetWorkedCityRadiusFor(cityLoc, gameObject);

        //below is giving every labor in any tile equal chance of being chosen
        int currentLabor = 0;
        Dictionary<int, Vector3Int> laborByTile = new();
        foreach (Vector3Int tile in workedTiles)
        {
            int prevLabor = currentLabor;
            currentLabor += world.GetCurrentLaborForTile(tile);
            for (int i = prevLabor; i < currentLabor; i++)
            {
                laborByTile[currentLabor] = tile;
            }
        }

        Vector3Int chosenTile = laborByTile[random.Next(currentLabor)];
        //above is giving labor in any tile equal chance of being chosen
        //Vector3Int chosenTile = workedTiles[random.Next(workedTiles.Count)]; //equal chance of being chosen, regardless of labor size

        int labor = world.GetCurrentLaborForTile(chosenTile);
        labor--;

        if (labor == 0) //removing from world dicts when zeroed out
        {
            world.RemoveFromCurrentWorked(chosenTile);
            world.RemoveFromCityLabor(chosenTile);
            //resourceManager.RemoveKeyFromGenerationDict(chosenTile);
        }
        else
        {
            world.AddToCurrentFieldLabor(chosenTile, labor);
        }
    }

    //private void RemoveRandomCityLaborer(System.Random random)
    //{
    //    List<string> buildingNames = world.GetBuildingListForCity(cityLoc);

    //    //below is giving every labor in any building equal chance of being chosen
    //    int currentLabor = 0;
    //    Dictionary<int, string> laborByBuilding = new();
    //    foreach (string buildingName in buildingNames)
    //    {
    //        int prevLabor = currentLabor;
    //        currentLabor += world.GetCurrentLaborForBuilding(cityLoc, buildingName);
    //        for (int i = prevLabor; i < currentLabor; i++)
    //        {
    //            laborByBuilding[currentLabor] = buildingName;
    //        }
    //    }

    //    string chosenBuildingName = laborByBuilding[random.Next(currentLabor)];
    //    //above is giving labor in any building equal chance of being chosen

    //    //string chosenBuildingName = buildingNames[random.Next(buildingNames.Count)]; //equal chance of being chosen, regardless of labor size
        
    //    int labor = world.GetCurrentLaborForBuilding(cityLoc, chosenBuildingName);
    //    labor--;

    //    if (labor == 0) //removing from world dicts when zeroed out
    //    {
    //        world.RemoveFromBuildingCurrentWorked(cityLoc, chosenBuildingName);
    //        //resourceManager.RemoveKeyFromBuildingGenerationDict(chosenBuildingName);
    //    }
    //    else
    //    {
    //        world.AddToCurrentBuildingLabor(cityLoc, chosenBuildingName, labor);
    //    }
    //}

    public void SetWaiter(TradeRouteManager tradeRouteManager, ResourceType resourceType = ResourceType.None)
    {
        tradeRouteWaiter = tradeRouteManager;
        resourceWaiter = resourceType;
    }

    public void CheckResourceWaiter(ResourceType resourceType)
    {
        if (tradeRouteWaiter != null && resourceWaiter == resourceType)
        {
            tradeRouteWaiter.resourceCheck = false;
            tradeRouteWaiter = null;
            resourceWaiter = ResourceType.None;
        }
    }

    public void CheckLimitWaiter()
    {
        if (tradeRouteWaiter != null && resourceWaiter == ResourceType.None)
        {
            tradeRouteWaiter.resourceCheck = false;
            tradeRouteWaiter = null;
        }
    }

    public void AddToWaitList(Unit unit)
    {
        if (!waitList.Contains(unit))
            waitList.Enqueue(unit);
    }

    public void CheckQueue()
    {
        if (waitList.Count > 0)
        {
            waitList.Dequeue().MoveUpInLine();
        }

        if (waitList.Count > 0)
        {
            foreach(Unit unit in waitList)
            {
                unit.MoveUpInLine();
            }
        }
    }

    public void ChangeWorkEthic(float change)
    {
        workEthic += change;
    }

    public void PlayResourceSplash()
    {
        resourceSplash.Play();
    }

    public void PlayLightBullet()
    {
        lightBullet.Play();
    }


    //world resource manager
    public bool CheckIfWorldResource(ResourceType resourceType) //seeing if its world resource
    {
        return world.WorldResourcePrep().Contains(resourceType);
    }

    public void UpdateWorldResourceGeneration(ResourceType resourceType, float diffAmount, bool add)
    {
        world.UpdateWorldResourceGeneration(resourceType, diffAmount, add);

        if (resourceType == ResourceType.Research)
        {
            if (add)
                researchPerMinute += Mathf.RoundToInt(diffAmount);
            else
                researchPerMinute += Mathf.RoundToInt(diffAmount);
        }
    }

    public void ChangeResourcesWorked(ResourceType resourceType, int laborChange)
    {
        if (resourcesWorkedDict.ContainsKey(resourceType))
        {
            resourcesWorkedDict[resourceType] += laborChange;
        }
        else
        {
            resourcesWorkedDict[resourceType] = laborChange;
        }
    }

    public void RemoveFromResourcesWorked(ResourceType resourceType)
    {
        resourcesWorkedDict.Remove(resourceType);
    }

    public bool CheckResourcesWorkedExists(ResourceType resourceType)
    {
        return resourcesWorkedDict.ContainsKey(resourceType);
    }

    public List<ResourceType> GetResourcesWorked()
    {
        List<ResourceType> resourceKeys = resourcesWorkedDict.Keys.ToList();
        return resourceKeys;
    }

    public int GetResourcesWorkedResourceCount(ResourceType resourceType)
    {
        return resourcesWorkedDict[resourceType];
    }

    public void UpdateWorldResources(ResourceType resourceType, int amount)
    {
        world.UpdateWorldResources(resourceType, amount);
    }

    public bool CheckWorldGold(int amount)
    {
        return world.CheckWorldGold(amount);
    }


    //Time generator to consume food
    private IEnumerator FoodConsumptionCoroutine()
    {
        if (activeCity)
        {
            uiTimeProgressBar.SetToZero();
            uiTimeProgressBar.SetTime(countDownTimer);
        }

        while (countDownTimer > 0)
        {
            yield return new WaitForSeconds(1);
            countDownTimer--;
            if (activeCity)
                uiTimeProgressBar.SetTime(countDownTimer);
        }

        //sell before growing
        world.UpdateWorldResources(ResourceType.Gold, resourceManager.SellResources());

        ResourceValue foodConsumed;
        foodConsumed.resourceType = ResourceType.Food;
        foodConsumed.resourceAmount = foodConsumptionPerMinute;

        //consume before checking for growth
        resourceManager.ConsumeResources(new List<ResourceValue> { foodConsumed }, 1, cityLoc);
        resourceManager.CheckForPopGrowth();
        resourceManager.CycleCount++;

        Debug.Log(cityName + " is checking for growth");
        countDownTimer = secondsTillGrowthCheck;
        StartCoroutine(FoodConsumptionCoroutine());
    }

    private void SetProgressTimeBar()
    {
        Vector3 cityPos = cityLoc;
        cityPos.z -= 1.5f; //bottom center of tile
        GameObject gameObject = Instantiate(GameAssets.Instance.cityGrowthProgressPrefab2, cityPos, Quaternion.Euler(90, 0, 0));
        uiTimeProgressBar = gameObject.GetComponent<UITimeProgressBar>();
        uiTimeProgressBar.SetAdditionalText = "Growth: ";
        uiTimeProgressBar.SetTimeProgressBarValue(secondsTillGrowthCheck);
    }

    public void HideCityGrowthProgressTimeBar()
    {
        uiTimeProgressBar.gameObject.SetActive(false);
    }

    public void CityGrowthProgressBarSetActive(bool v)
    {
        if (v && cityPop.CurrentPop == 0)
            return;

        uiTimeProgressBar.gameObject.SetActive(v);
        if (v)
        {
            uiTimeProgressBar.SetProgressBarMask(countDownTimer);
            uiTimeProgressBar.SetTime(countDownTimer);
        }
    }

    //for automatically assigning labor
    public void AutoAssignmentsForLabor()
    {
        //if (reassignAll)
        //{
        //    List<Vector3Int> workedTiles = world.GetWorkedCityRadiusFor(cityLoc, gameObject);

        //    foreach (Vector3Int tile in workedTiles)
        //    {
        //        ResourceProducer resourceProducer = world.GetResourceProducer(tile);
                
        //        int currentLabor = world.GetCurrentLaborForTile(tile);
        //        int maxLabor = world.GetMaxLaborForTile(tile);
        //        cityPop.UnusedLabor += currentLabor;
        //        cityPop.UsedLabor -= currentLabor;
        //        world.RemoveFromCurrentWorked(tile);

        //        if (currentLabor == maxLabor)  
        //            PlacesToWork++;
        //    }
        //}
        
        int unusedLabor = cityPop.UnusedLabor;
        bool maxxed;

        List<Vector3Int> laborLocs = world.GetPotentialLaborLocationsForCity(cityLoc, gameObject);

        if (laborLocs.Count == 0)
            return;

        //Going through resource priorities first
        foreach (ResourceType resourceType in ResourcePriorities)
        {
            List<Vector3Int> laborTiles = new(laborLocs);
            
            foreach (Vector3Int laborTile in laborTiles)
            {
                if (unusedLabor == 0)
                    break;
                
                if (world.GetResourceProducer(laborTile).producedResources.Contains(resourceType))
                {
                    (unusedLabor, maxxed) = IncreaseLaborCount(unusedLabor, laborTile);
                    if (maxxed)
                        laborLocs.Remove(laborTile);
                }
            }
        }

        //randomly assigning the rest
        if (laborLocs.Count > 0 && unusedLabor > 0)
        {
            RandomLaborAssignment(unusedLabor, laborLocs);
        }
    }

    private void RandomLaborAssignment(int labor, List<Vector3Int> locations)
    {
        System.Random random = new System.Random();
        
        for (int i = 0; i < labor; i++)
        {
            if (locations.Count == 0) 
                break;
            
            int tileIndex = random.Next(0, locations.Count);
            (int remainingLabor, bool maxxed) = IncreaseLaborCount(1, locations[tileIndex]);
            if (maxxed)
                locations.Remove(locations[tileIndex]);
        }
    }

    private (int, bool) IncreaseLaborCount(int laborChange, Vector3Int terrainLocation)
    {
        int labor = world.GetCurrentLaborForTile(terrainLocation);
        int maxLabor = world.GetMaxLaborForTile(terrainLocation);
        bool maxxed = false;
        int remainingLabor = 0;

        ResourceProducer resourceProducer = world.GetResourceProducer(terrainLocation); //cached all resource producers in dict

        int laborDiff = maxLabor - labor;

        if (laborDiff < laborChange)
        {
            remainingLabor = laborChange - laborDiff;
            laborChange = laborDiff;
        }

        //selectedCity.cityPop.GetSetFieldLaborers += laborChange;
        cityPop.UnusedLabor -= laborChange;
        cityPop.UsedLabor += laborChange;

        if (labor == 0) //assigning city to location if working for first time
        {
            world.AddToCityLabor(terrainLocation, gameObject);
            resourceProducer.SetResourceManager(resourceManager);
            resourceProducer.StartProducing();
        }
        else
        {
            resourceProducer.AddLaborMidProduction();
        }

        labor += laborChange;
        if (labor == maxLabor)
        {
            //PlacesToWork--;
            maxxed = true;
        }
        resourceProducer.UpdateCurrentLaborData(labor);
        //resourceProducer.UpdateResourceGenerationData();

        foreach (ResourceType resourceType in resourceProducer.producedResources)
        {
            for (int i = 0; i < laborChange; i++)
            {
                ChangeResourcesWorked(resourceType, 1);
                int totalResourceLabor = GetResourcesWorkedResourceCount(resourceType);
                cityBuilderManager.uiLaborHandler.PlusMinusOneLabor(resourceType, totalResourceLabor, 1, ResourceManager.GetResourceGenerationValues(resourceType));
            }
        }

        world.AddToCurrentFieldLabor(terrainLocation, labor);

        //updating all the city labor info
        UpdateCityPopInfo();

        return (remainingLabor, maxxed);
    }

    //for queued build items
    public void GoToNextItemInQueue()
    {
        //UIQueueItem item = savedQueueItems[0];
        //savedQueueItems.Remove(item);
        //savedQueueItemsNames.RemoveAt(0);
        //Destroy(item);

        if (savedQueueItems.Count > 0)
            GoToNextItemInBuildQueue();
    }

    //public void RemoveFromQueue(Vector3Int loc)
    //{
    //    string name = "Upgrade" + " (" + loc.x / 3 + "," + loc.z / 3 + ")";

    //    int index = 0;
    //    foreach (UIQueueItem item in savedQueueItems)
    //    {
    //        if (item.upgrading && item.itemName == name)
    //        {
    //            savedQueueItems.Remove(item);
    //            savedQueueItemsNames.RemoveAt(index);
    //            resourceManager.ClearQueueResources();
    //            world.RemoveLocationFromQueueList(loc);
    //            Destroy(item);
    //            if (index == 0 && savedQueueItems.Count > 0)
    //                GoToNextItemInBuildQueue();
    //            break;
    //        }

    //        index++;
    //    }
    //}

    public void RemoveFromQueue(Vector3Int loc)
    {
        int index = 0;
        foreach (UIQueueItem item in savedQueueItems)
        {
            if (item.buildLoc == loc)
            {
                savedQueueItems.Remove(item);
                savedQueueItemsNames.RemoveAt(index);
                resourceManager.ClearQueueResources();
                world.RemoveLocationFromQueueList(loc);
                Destroy(item);
                if (index == 0 && savedQueueItems.Count > 0)
                    GoToNextItemInBuildQueue();
                break;
            }

            index++;
        }
    }

    private void GoToNextItemInBuildQueue()
    {
        UIQueueItem nextItem = savedQueueItems[0];

        List<ResourceValue> resourceCosts = new();

        if (nextItem.unitBuildData != null)
            resourceCosts = new(nextItem.unitBuildData.unitCost);
        else if (nextItem.upgradeCosts != null)
            resourceCosts = new(nextItem.upgradeCosts);
        else if (nextItem.improvementData != null)
            resourceCosts = new(nextItem.improvementData.improvementCost);

        resourceManager.SetQueueResources(resourceCosts, cityBuilderManager);
    }

    public UIQueueItem GetBuildInfo()
    {
        return savedQueueItems[0];
    }

    //looking to see if there are any unclaimed single builds in the area to lay claim to
    public void CheckForAvailableSingleBuilds()
    {
        foreach (Vector3Int tile in world.GetNeighborsFor(cityLoc, MapWorld.State.CITYRADIUS))
        {
            if (world.CheckIfUnclaimedSingleBuild(tile))
            {
                CityImprovement cityImprovement = world.GetCityDevelopment(tile);

                string name = cityImprovement.GetImprovementData.improvementName;
                singleBuildImprovementsBuildingsDict[name] = tile;
                world.AddToCityLabor(tile, gameObject);

                if (name == "Harbor")
                {
                    hasHarbor = true;
                    harborLocation = tile;
                    world.SetCityHarbor(this, tile);
                }

                world.RemoveFromUnclaimedSingleBuild(tile);
            }
        }
    }


    internal void Select()
    {
        EnableHighlight();
        //selectionCircle.enabled = true;
        //highlight.ToggleGlow(true);
    }

    public void Deselect()
    {
        DisableHighlight();
        //selectionCircle.enabled = false;
        //highlight.ToggleGlow(false);
    }

    public void DestroyThisCity()
    {
        StopAllCoroutines();
        Destroy(uiTimeProgressBar.gameObject);
    }
}
