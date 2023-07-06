using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mono.Cecil;

public class ResourceManager : MonoBehaviour
{
    private Dictionary<ResourceType, int> resourceDict = new(); //need this later for save system
    private Dictionary<ResourceType, float> resourceStorageMultiplierDict = new();
    private Dictionary<ResourceType, float> resourceGenerationPerMinuteDict = new(); //for resource generation stats
    private Dictionary<ResourceType, float> resourceConsumedPerMinuteDict = new(); //for resource consumption stats
    private Dictionary<ResourceType, int> resourcePriceDict = new();
    private Dictionary<ResourceType, bool> resourceSellDict = new();
    private Dictionary<ResourceType, int> resourceMinHoldDict = new();
    private Dictionary<ResourceType, int> resourceSellHistoryDict = new();

    public Dictionary<ResourceType, int> ResourceDict { get { return resourceDict; } set { resourceDict = value; } }
    public Dictionary<ResourceType, float> ResourceConsumedPerMinuteDict { get { return resourceConsumedPerMinuteDict; } }
    public Dictionary<ResourceType, int> ResourcePriceDict { get { return resourcePriceDict; } set { resourcePriceDict = value; } }
    public Dictionary<ResourceType, bool> ResourceSellDict { get { return resourceSellDict; } set { resourceSellDict = value; } }
    public Dictionary<ResourceType, int> ResourceMinHoldDict { get { return resourceMinHoldDict; } set { resourceMinHoldDict = value; } }
    public Dictionary<ResourceType, int> ResourceSellHistoryDict { get { return resourceSellHistoryDict; } set { resourceSellHistoryDict = value; } }

    private int resourceStorageLimit; 
    public int ResourceStorageLimit { get { return resourceStorageLimit; } set { resourceStorageLimit = value; } }
    private float resourceStorageLevel;
    public float GetResourceStorageLevel { get { return resourceStorageLevel; } }
    [HideInInspector]
    public Queue<ResourceProducer> waitingToUnloadProducers = new();
    [HideInInspector]
    public Queue<ResourceProducer> waitingToUnloadResearch = new();
    [HideInInspector]
    public bool fullInventory;

    //wait list for research
    private List<ResourceProducer> waitingForResearchProducerList = new();

    //wait list for inventory space
    private List<ResourceProducer> waitingForStorageRoomProducerList = new();

    //consuming resources
    private List<ResourceProducer> waitingforResourceProducerList = new();
    private List<ResourceType> resourcesNeededForProduction = new();

    //UIs to update
    private UIResourceManager uiResourceManager;
    private UIMarketPlaceManager uiMarketPlaceManager;
    [HideInInspector]
    public UIInfoPanelCity uiInfoPanelCity;

    //initial resources
    public List<ResourceValue> initialResources = new(); //resources you start a city with
    [HideInInspector]
    public City city;

    //for managing food consumption
    private bool growth;
    private int foodGrowthLevel;
    public int FoodGrowthLevel { get { return foodGrowthLevel; } }
    private int foodGrowthLimit;
    public int FoodGrowthLimit { get { return foodGrowthLimit; } }
    public float FoodPerMinute { get { return resourceGenerationPerMinuteDict[ResourceType.Food]; } }
    private int cycleCount;
    public int CycleCount { get { return cycleCount; } set { cycleCount = value; } }

    //for queued build orders
    private List<ResourceValue> queuedResourcesToCheck = new();
    private List<ResourceType> queuedResourceTypesToCheck = new(); //have this to check if the queue type has recently been added (can't check values easily)   
    private CityBuilderManager cityBuilderManager; //only instantiated through queue build

    private int resourceCount; //for counting wasted resources

    private void Awake()
    {
        CalculateAndChangeFoodLimit();
        PrepareResourceDictionary();
        SetInitialResourceValues();
        SetPrices();

        //only relevant during editing
        if (resourceStorageLevel >= resourceStorageLimit)
            fullInventory = true;
    }

    private void PrepareResourceDictionary()
    {
        foreach (ResourceIndividualSO resourceData in ResourceHolder.Instance.allStorableResources.Concat(ResourceHolder.Instance.allWorldResources).ToList())
        {
            if (resourceData.resourceType == ResourceType.None)
                continue;
            resourceDict[resourceData.resourceType] = 0;
            resourceGenerationPerMinuteDict[resourceData.resourceType] = 0;

            if (resourceData.resourceType != ResourceType.Research)
            {
                if (resourceData.resourceStorageMultiplier <= 0) //absolutely cannot be zero or less
                    resourceStorageMultiplierDict[resourceData.resourceType] = 1;
                resourceStorageMultiplierDict[resourceData.resourceType] = resourceData.resourceStorageMultiplier;
                resourceConsumedPerMinuteDict[resourceData.resourceType] = 0;
            }
        }
    }

    public void SetCity(City city)
    {
        this.city = city;
    }

    private void SetInitialResourceValues()
    {
        foreach (ResourceValue resourceData in initialResources)
        {
            ResourceType resourceType = resourceData.resourceType;
            resourceDict[resourceType] = resourceData.resourceAmount; //assigns the initial values for each resource
            if (resourceStorageMultiplierDict.ContainsKey(resourceType))
            {
                resourceStorageLevel += resourceData.resourceAmount * resourceStorageMultiplierDict[resourceType];
            }
        }
    }

    //public void BeginResourceGeneration(List<ResourceValue> producedResources) //for city tiles
    //{
    //    UpdateResourceGeneration(producedResources);

    //    IncreaseFoodConsumptionPerTurn(true);        
    //}

    public void ModifyResourceGenerationPerMinute(ResourceType resourceType, float generationDiff, bool add)
    {
        if (resourceType == ResourceType.None)
            return;
            
        if (add)
            resourceGenerationPerMinuteDict[resourceType] += generationDiff;
        else
            resourceGenerationPerMinuteDict[resourceType] -= generationDiff;

        if (city.CheckIfWorldResource(resourceType))
        {
            city.UpdateWorldResourceGeneration(resourceType, generationDiff, add);
        }
    }

    public void ModifyResourceConsumptionPerMinute(ResourceType resourceType, float change, bool add)
    {
        if (resourceType == ResourceType.Food)
        {
            resourceConsumedPerMinuteDict[resourceType] = change; //change completely for food
            return;
        }

        if (add)
            resourceConsumedPerMinuteDict[resourceType] += change;
        else
            resourceConsumedPerMinuteDict[resourceType] -= change;
    }

    //public void RemoveKeyFromGenerationDict(Vector3Int pos)
    //{
    //    currentWorkedResourceGenerationDict.Remove(pos);
    //}

    //public void RemoveKeyFromBuildingGenerationDict(string buildingName)
    //{
    //    buildingResourceGenerationDict.Remove(buildingName);
    //}

    public int GetResourceValues(ResourceType resourceType)
    {
        return resourceDict[resourceType];
    }

    public float GetResourceGenerationValues(ResourceType resourceType)
    {
        return resourceGenerationPerMinuteDict[resourceType];
    }

    public bool CheckResourceAvailability(ResourceValue resourceRequired) //this will be used by building system to see if we have enough resources
    {
        return resourceDict[resourceRequired.resourceType] >= resourceRequired.resourceAmount;
    }

    public bool CheckStorageSpaceForResource(ResourceType resourceType, int resourceAdded)
    {
        if (resourceAdded < 0 && resourceDict[resourceType] > 0)
            return true;
        else if (resourceAdded < 0 && resourceDict[resourceType] == 0)
            return false;
        else if (resourceStorageMultiplierDict[resourceType] < 1)
            return Mathf.CeilToInt(resourceStorageLevel + resourceStorageMultiplierDict[resourceType]) <= resourceStorageLimit;
        else if (resourceAdded > 0 && resourceStorageLimit <= 0) //for infinite storage
            return true;

        return resourceStorageLevel < resourceStorageLimit;
    }

    private void VerifyResourceAmount(ResourceType resourceType)
    {
        if (resourceDict[resourceType] < 0)
            throw new InvalidOperationException("Can't have resources less than 0 " + resourceType);
    }

    public void ConsumeResources(List<ResourceValue> consumedResource, float currentLabor, Vector3 location)
    {
        int i = 0;
        location.y += consumedResource.Count * 0.4f;

        foreach (ResourceValue value in consumedResource)
        {
            int consumedAmount = Mathf.RoundToInt(value.resourceAmount * currentLabor);
            ResourceType resourceType = value.resourceType;
            if (consumedAmount == 0)
                continue;

            if (resourceType == ResourceType.Gold)
            {
                city.UpdateWorldResources(resourceType, -consumedAmount);
            }
            else
            {
                int storageAmount = resourceDict[resourceType];

                //resourceConsumedPerMinuteDict[resourceType] = consumedAmount;

                if (value.resourceType == ResourceType.Food && consumedAmount > storageAmount)
                {
                    foodGrowthLevel -= consumedAmount - storageAmount;
                    consumedAmount = storageAmount;
                }

                resourceDict[resourceType] -= consumedAmount;
                resourceStorageLevel -= consumedAmount;
                CheckProducerUnloadWaitList();
                city.CheckLimitWaiter();
                UpdateUI(resourceType);
            }

            if (city.activeCity && consumedAmount > 0 && value.resourceType != ResourceType.Food)
            {
                Vector3 loc = location;
                loc.y -= 0.4f * i;
                InfoResourcePopUpHandler.CreateResourceStat(loc, -consumedAmount, ResourceHolder.Instance.GetIcon(resourceType));
                i++;
            }
        }

        if (city.activeCity)
            city.UpdateResourceInfo();
    }

    public bool ConsumeResourcesCheck(List<ResourceValue> consumeResources, int labor)
    {
        foreach (ResourceValue value in consumeResources)
        {
            if (value.resourceType == ResourceType.Gold)
            {
                if (!city.CheckWorldGold(value.resourceAmount * labor))
                {
                    city.AddToWorldGoldWaitList();
                    return false;
                }
            }
            else if (resourceDict[value.resourceType] < value.resourceAmount * labor)
            {
                return false;
            }
        }

        return true;
    }

    public void PrepareResource(List<ResourceValue> producedResource, float currentLabor, Vector3 producerLoc, bool returnResource = false)
    {
        int i = 0;
        producerLoc.y += producedResource.Count * 0.4f;
        resourceCount = 0;
        foreach (ResourceValue resourceVal in producedResource)
        {
            int newResourceAmount;

            if (returnResource)
            {
                newResourceAmount = Mathf.RoundToInt(resourceVal.resourceAmount * currentLabor);
            }
            else
            {
                newResourceAmount = CalculateResourceGeneration(resourceVal.resourceAmount, currentLabor);
            }

            int resourceAmount = CheckResource(resourceVal.resourceType, newResourceAmount);
            Vector3 loc = producerLoc;
            loc.y += 0.4f * i;

            if (resourceAmount != 0)
                InfoResourcePopUpHandler.CreateResourceStat(loc, resourceAmount, ResourceHolder.Instance.GetIcon(resourceVal.resourceType));
            i++;
        }
    }

    public int CheckResource(ResourceType type, int amount)
    {
        if (type == ResourceType.Fish)
            type = ResourceType.Food;
        
        if (type == ResourceType.Food && amount > 0)
        {
            return AddFood(amount);
        }
        else if (type == ResourceType.Gold || type == ResourceType.Research)
        {
            city.UpdateWorldResources(type, amount);
            return amount;
        }
        else if (CheckStorageSpaceForResource(type, amount) && resourceDict.ContainsKey(type))
        {
            if (!city.resourceGridDict.ContainsKey(type))
                city.AddToGrid(type);

            return AddResourceToStorage(type, amount);
        }
        else
        {
            return 0;
        }
    }

    private int AddFood(int resourceAmount)
    {
        int newResourceBalance = (foodGrowthLevel + resourceAmount) - foodGrowthLimit;

        if (newResourceBalance >= 0 && city.HousingCount > 0)
        {
            if (!city.resourceGridDict.ContainsKey(ResourceType.Food))
                city.AddToGrid(ResourceType.Food);

            growth = true;
            resourceAmount -= newResourceBalance;
            foodGrowthLevel += resourceAmount;
            if (city.activeCity)
                uiInfoPanelCity.UpdateFoodGrowth(foodGrowthLevel);
            int addedResource = AddResourceToStorage(ResourceType.Food, newResourceBalance);
            if (city.cityPop.CurrentPop == 0)
                CheckForPopGrowth();
            return resourceAmount + addedResource;
        }

        foodGrowthLevel += resourceAmount;

        if (city.activeCity)
            uiInfoPanelCity.UpdateFoodGrowth(foodGrowthLevel);

        return resourceAmount;
    }

    //returns how much is actually moved
    private int AddResourceToStorage(ResourceType type, int resourceAmount)
    {
        int prevAmount = resourceDict[type];
        
        //check to ensure you don't take out more resources than are available in dictionary
        if (resourceAmount < 0 && -resourceAmount > prevAmount)
        {
            resourceAmount = -prevAmount;
        }
        
        if (resourceStorageMultiplierDict.ContainsKey(type))
            resourceAmount = Mathf.CeilToInt(resourceAmount * resourceStorageMultiplierDict[type]);

        //adjusting resource amount to move based on how much space is available
        int newResourceAmount = resourceAmount;
        int newResourceBalance = (Mathf.CeilToInt(resourceStorageLevel) + newResourceAmount) - resourceStorageLimit;
        if (newResourceBalance >= 0 && resourceStorageLimit > 0) //limit of 0 or less means infinite storage
        {
            newResourceAmount -= newResourceBalance;
        }

        int resourceAmountAdjusted = Mathf.RoundToInt(newResourceAmount / resourceStorageMultiplierDict[type]);

        resourceDict[type] += resourceAmountAdjusted; //updating the dictionary

        resourceStorageLevel += newResourceAmount;
        if (resourceStorageLevel >= resourceStorageLimit)
            fullInventory = true;
        if (newResourceAmount < 0)
            CheckProducerUnloadWaitList();
        else if (resourcesNeededForProduction.Contains(type))
            CheckProducerResourceWaitList(type);

        int wasteCheck = 0;
        if (resourceStorageMultiplierDict.ContainsKey(type) && resourceStorageMultiplierDict[type] > 0)
            wasteCheck = Mathf.RoundToInt((resourceAmount - newResourceAmount) / resourceStorageMultiplierDict[type]);

        if (wasteCheck > 0)
        {
            Vector3 loc = city.cityLoc;
            loc.y += 2f; //limit of 5 resources at once wasted
            loc.y += -.4f * resourceCount;
            InfoResourcePopUpHandler.CreateResourceStat(loc, wasteCheck, ResourceHolder.Instance.GetIcon(type), true);
            Debug.Log($"Wasted {wasteCheck} of {type}");
            resourceCount++;
        }

        if (queuedResourceTypesToCheck.Contains(type))
            CheckResourcesForQueue();
        UpdateUI(type);
        if (city.activeCity)
        {
            city.UpdateResourceInfo();
            city.CheckBuildOptionsResource(type, prevAmount, resourceDict[type], resourceAmount > 0);
        }
        if (newResourceAmount > 0)
            city.CheckResourceWaiter(type);
        else if (newResourceAmount < 0)
            city.CheckLimitWaiter();
        
        return resourceAmountAdjusted;
    }

    public int CalculateResourceGeneration(int resourceAmount, float labor)
    {
        return Mathf.FloorToInt(city.workEthic * (resourceAmount * labor * (1 + .1f * (labor - 1))));
    }

    private void CalculateAndChangeFoodLimit()
    {
        int cityPop = city.cityPop.CurrentPop;
        int newLimit = 9 + cityPop;

        foodGrowthLimit = newLimit;
        //SetResourceLimitValues(ResourceType.Food, foodGrowthLimit);
    }


    public void SpendResource(List<ResourceValue> buildCost, Vector3 loc)
    {
        int i = 0;
        loc.y += buildCost.Count * 0.4f;

        foreach (ResourceValue resourceValue in buildCost)
        {
            Vector3 newLoc = loc;
            
            if (resourceValue.resourceType == ResourceType.Gold)
                city.UpdateWorldResources(resourceValue.resourceType, -resourceValue.resourceAmount);
            else
                SpendResource(resourceValue.resourceType, resourceValue.resourceAmount);
    
            newLoc.y += -.4f * i;
            InfoResourcePopUpHandler.CreateResourceStat(newLoc, -resourceValue.resourceAmount, ResourceHolder.Instance.GetIcon(resourceValue.resourceType));
            i++;
        }
    }

    private void SpendResource(ResourceType resourceType, int resourceAmount) //changing the resource counter upon using resources
    {
        //ResourceType resourceValue = resourceValue.resourceValue; 
        resourceDict[resourceType] -= resourceAmount; //subtract cost
        if (resourceStorageMultiplierDict.ContainsKey(resourceType))
        {
            resourceStorageLevel -= resourceAmount * resourceStorageMultiplierDict[resourceType];
            CheckProducerUnloadWaitList();
        }
        city.CheckLimitWaiter();
        VerifyResourceAmount(resourceType);
        //UpdateUI(resourceValue);
    }

    public void SetUI(UIResourceManager uiResourceManager, UIMarketPlaceManager uiMarketPlaceManager, UIInfoPanelCity uiInfoPanelCity)
    {
        this.uiResourceManager = uiResourceManager;
        this.uiMarketPlaceManager = uiMarketPlaceManager;
        this.uiInfoPanelCity = uiInfoPanelCity;
    }

    public void UpdateUI(List<ResourceValue> values) //updating the UI with the resource information in the dictionary
    {
        foreach (ResourceValue value in values)
        {
            if (value.resourceType == ResourceType.Gold)
                continue;
            
            UpdateUI(value.resourceType);
        }
    }

    private void UpdateUI(ResourceType resourceType)
    {
        if (city.activeCity) //only update UI for currently selected city
        {
            uiResourceManager.SetResource(resourceType, resourceDict[resourceType]);
            uiResourceManager.SetCityCurrentStorage(resourceStorageLevel);

            if (uiMarketPlaceManager.activeStatus)
                uiMarketPlaceManager.UpdateMarketResourceNumbers(resourceType, resourcePriceDict[resourceType], resourceDict[resourceType], resourceSellHistoryDict[resourceType]);
        }
        else if (city.uiCityResourceInfoPanel)
        {
            city.uiCityResourceInfoPanel.UpdateResourceInteractable(resourceType, resourceDict[resourceType], false);
            city.uiCityResourceInfoPanel.UpdateStorageLevel(GetResourceStorageLevel);
        }
    }

    public void IncreaseFoodConsumptionPerTurn(bool v) //only used when increasing pop when joining city, growth, or building city
    {
        if (v)
            resourceGenerationPerMinuteDict[ResourceType.Food] -= city.unitFoodConsumptionPerMinute;
        else
            resourceGenerationPerMinuteDict[ResourceType.Food] += city.unitFoodConsumptionPerMinute;
    }

    //public int GetResourceLimit(ResourceType resourceType)
    //{
    //    return resourceStorageLimit;
    //}

    public int GetResourceDictValue(ResourceType resourceType)
    {
        return resourceDict[resourceType];
    }

    public int SellResources()
    {
        int goldAdded = 0;
        int i = 0;
        int length = Mathf.Max(resourceSellDict.Count,2);

        foreach (ResourceType resourceType in resourceSellDict.Keys)
        {
            if (resourceSellDict[resourceType])
            {
                int sellAmount = resourceDict[resourceType] - resourceMinHoldDict[resourceType];
                if (sellAmount > 0)
                {
                    int goldGained = resourcePriceDict[resourceType] * sellAmount;
                    goldAdded += goldGained;
                    resourceSellHistoryDict[resourceType] += sellAmount;
                    CheckResource(resourceType, -sellAmount);

                    Vector3 cityLoc = city.cityLoc;
                    cityLoc.y += length * 0.4f;
                    cityLoc.y += -0.4f * i;
                    if (city.activeCity)
                        InfoResourcePopUpHandler.CreateResourceStat(cityLoc, -sellAmount, ResourceHolder.Instance.GetIcon(resourceType));
                    i++;
                }
            }
        }

        if (goldAdded > 0)
        {
            Vector3 cityLoc = city.cityLoc;
            cityLoc.y += length * 0.4f + 0.4f;
            InfoResourcePopUpHandler.CreateResourceStat(cityLoc, goldAdded, ResourceHolder.Instance.GetIcon(ResourceType.Gold));
        }

        SetPrices();

        return goldAdded;
    }

    private void SetPrices()
    {
        int currentPop = city.cityPop.CurrentPop;
        float populationFactor = 0.2f; //ratio of how much a new pop increases prices
        float cycleAttrition = 0.02f; //ratio of how many cycles to burn through resourceQuantityePerPop
        float abundanceRatio = 0.5f; //how many purchases of resources by current pop to reduce the price in half. 

        foreach (ResourceIndividualSO resourceData in ResourceHolder.Instance.allStorableResources)
        {
            ResourceType resourceType = resourceData.resourceType;
            int resourceQuantityPerPop = resourceData.resourceQuantityPerPop;

            if (currentPop > 0)
            {
                float priceByPop = (1 + (currentPop-1) * populationFactor);
                float abundanceWithAttrition = resourceQuantityPerPop * cycleAttrition * cycleCount * currentPop;
                float abundanceFactor = 1 - ((resourceSellHistoryDict[resourceType] - abundanceWithAttrition) / currentPop) * 1 / resourceQuantityPerPop * abundanceRatio;
                resourcePriceDict[resourceType] = Mathf.Max((int)Math.Round(resourceData.resourcePrice * priceByPop * abundanceFactor, MidpointRounding.AwayFromZero),1);
                //resourcePriceDict[resourceType] = Mathf.Max(Mathf.FloorToInt(resourceData.resourcePrice * priceByPop * abundanceFactor), 1);
            }
            else //no price if no one there to purchase
            {
                resourcePriceDict[resourceType] = 0;
            }
        }
    }

    //checking if enough food to grow
    public void CheckForPopGrowth()
    {
        if (growth) //increasing pop if food over or equal to max
        {
            growth = false;
            int excessFood = 0;

            city.PopulationGrowthCheck(false);
            CalculateAndChangeFoodLimit();

            if (resourceDict[ResourceType.Food] > 0) //can only carry over one limit of food, rest of food goes to storage
            {
                if (resourceDict[ResourceType.Food] > foodGrowthLimit)
                {
                    excessFood = foodGrowthLimit;
                    resourceDict[ResourceType.Food] -= foodGrowthLimit;
                    resourceStorageLevel -= foodGrowthLimit;
                    CheckProducerUnloadWaitList();
                }
                else
                {
                    excessFood = resourceDict[ResourceType.Food];
                    resourceDict[ResourceType.Food] -= excessFood;
                    resourceStorageLevel -= excessFood;
                    CheckProducerUnloadWaitList();
                }

                city.CheckLimitWaiter();
            }

            foodGrowthLevel = excessFood;
        }
        else
        {
            if (foodGrowthLevel < foodGrowthLimit && resourceDict[ResourceType.Food] > 0) //fill up food coffers if below food limit (but no growth). 
            {
                int diff = foodGrowthLimit - foodGrowthLevel;
                if (resourceDict[ResourceType.Food] < diff)
                    diff = resourceDict[ResourceType.Food];
                foodGrowthLevel += diff;
                resourceDict[ResourceType.Food] -= diff;
                resourceStorageLevel -= diff;
                CheckProducerUnloadWaitList();
                city.CheckLimitWaiter();
            }
        }

        if (foodGrowthLevel < 0) //decreasing pop if food under 0 for 2 straight turns
        {
            city.PopulationDeclineCheck();
            foodGrowthLevel = 0;
            CalculateAndChangeFoodLimit();
        }

        city.UpdateCityPopInfo(); //update city info after correcting food info
        UpdateUI(ResourceType.Food);

        if (city.activeCity)
            uiInfoPanelCity.UpdateFoodStats(city.cityPop.CurrentPop, foodGrowthLevel, foodGrowthLimit, FoodPerMinute);
    }

    private void CheckProducerUnloadWaitList()
    {
        if (fullInventory)
        {
            fullInventory = false;
            int queueCount = waitingToUnloadProducers.Count;

            for (int i = 0; i < queueCount; i++)
            {
                if (!fullInventory)
                    waitingToUnloadProducers.Dequeue().UnloadAndRestart();
                else
                    break;
            }

            //check again to start the others
            if (!fullInventory)
                RestartStorageRoomWaitProduction();
        }
    }

    public void CheckProducerUnloadResearchWaitList()
    {
        if (city.WorldResearchingCheck())
        {
            int queueCount = waitingToUnloadResearch.Count;

            for (int i = 0; i < queueCount; i++)
            {
                if (city.WorldResearchingCheck())
                    waitingToUnloadResearch.Dequeue().UnloadAndRestart();
                else
                    break;
            }

            if (city.WorldResearchingCheck())
                RestartResearchWaitProduction();
        }
    }

    public void RestartResearchWaitProduction()
    {
        List<ResourceProducer> tempProducers = new(waitingForResearchProducerList);

        foreach (ResourceProducer producer in tempProducers)
        {
            producer.isWaitingForResearch = false;
            producer.StartProducing();
            waitingForResearchProducerList.Remove(producer);
        }
    }

    public void RestartStorageRoomWaitProduction()
    {
        List<ResourceProducer> tempProducers = new(waitingForStorageRoomProducerList);

        foreach (ResourceProducer producer in tempProducers)
        {
            producer.isWaitingForStorageRoom = false;
            producer.StartProducing();
            waitingForStorageRoomProducerList.Remove(producer);
        }
    }

    public void RemoveFromWaitUnloadResearchQueue(ResourceProducer resourceProducer)
    {
        if (waitingToUnloadResearch.Contains(resourceProducer))
            waitingToUnloadResearch = new Queue<ResourceProducer>(waitingToUnloadResearch.Where(x => x != resourceProducer));
    }

    public void RemoveFromWaitUnloadQueue(ResourceProducer resourceProducer)
    {
        if (waitingToUnloadProducers.Contains(resourceProducer))
            waitingToUnloadProducers = new Queue<ResourceProducer>(waitingToUnloadProducers.Where(x => x != resourceProducer));
    }

    public void CheckProducerResourceWaitList()
    {
        List<ResourceProducer> tempWaitingForResource = new(waitingforResourceProducerList);

        foreach (ResourceProducer producer in tempWaitingForResource)
        {
            producer.RestartResourceWaitProduction();
        }
    }

    public void CheckProducerResourceWaitList(ResourceType resourceType)
    {
        List<ResourceProducer> tempWaitingForResource = new(waitingforResourceProducerList);
        
        foreach (ResourceProducer producer in tempWaitingForResource)
        {
            if (producer.consumedResourceTypes.Contains(resourceType))
                producer.RestartResourceWaitProduction();
        }
    }

    public void AddToResearchWaitList(ResourceProducer resourceProducer)
    {
        waitingForResearchProducerList.Add(resourceProducer);
    }

    public void RemoveFromResearchWaitlist(ResourceProducer resourceProducer)
    {
        waitingForResearchProducerList.Remove(resourceProducer);
    }

    public void AddToStorageRoomWaitList(ResourceProducer resourceProducer)
    {
        waitingForStorageRoomProducerList.Add(resourceProducer);
    }

    public void RemoveFromStorageRoomWaitList(ResourceProducer resourceProducer)
    {
        waitingForStorageRoomProducerList.Remove(resourceProducer);
    }

    public void AddToResourceWaitList(ResourceProducer resourceProducer)
    {
        waitingforResourceProducerList.Add(resourceProducer);
    }

    public void RemoveFromResourceWaitList(ResourceProducer resourceProducer)
    {
        waitingforResourceProducerList.Remove(resourceProducer);
    }

    public void AddToResourcesNeededForProduction(List<ResourceType> consumedResources)
    {
        foreach (ResourceType type in consumedResources)
            resourcesNeededForProduction.Add(type);
    }

    public void RemoveFromResourcesNeededForProduction(List<ResourceType> consumedResources)
    {
        foreach (ResourceType type in consumedResources)
            resourcesNeededForProduction.Remove(type);
    }

    //public void SetCityBuilderManager(CityBuilderManager cityBuilderManager)
    //{
    //    this.cityBuilderManager = cityBuilderManager;
    //}

    //for queued build orders in cities
    public void SetQueueResources(List<ResourceValue> resourceList, CityBuilderManager cityBuilderManager)
    {
        queuedResourcesToCheck = resourceList;
        this.cityBuilderManager = cityBuilderManager;

        foreach (ResourceValue resource in resourceList)
        {
            queuedResourceTypesToCheck.Add(resource.resourceType);
        }

        CheckResourcesForQueue();
    }

    public void ClearQueueResources()
    {
        queuedResourcesToCheck.Clear();
        queuedResourceTypesToCheck.Clear();
    }

    private void CheckResourcesForQueue()
    {
        if (queuedResourcesToCheck.Count > 0)
        {
            foreach (ResourceValue resource in queuedResourcesToCheck)
            {
                if (!CheckResourceAvailability(resource))
                {
                    return;
                }
            }

            ClearQueueResources();
            cityBuilderManager.BuildQueuedBuilding(city, this);
        }
    }

    //public void WaitTurn()
    //{
    //    CheckForPopGrowth();
    //    CheckResourcesForQueue();
    //}
}

[Serializable] //needs to be serializable in order to be seen in unity
public struct ResourceValue
{
    public ResourceType resourceType;
    [Min(0)]
    public int resourceAmount;
}

public enum ResourceType    
{
    None,
    Labor,
    Food,
    Fish,
    Gold,
    Research,
    Lumber,
    Stone,
    CopperOre,
    Clay,
    CoilPottery,
    Pottery,
    Sculpture,
    Copper,
    GoldIngot,
    BronzeIngot,
    Bronzeware,
    Wool,
    Silk,
    Cloth,
    SilkCloth,
    StoneSlab,
    Ruby,
    Cotton,
    IronOre,
    Iron,
    GoldOre,
    Emerald,
    Diamond
}

public enum RawResourceType
{
    None,
    FoodLand,
    FoodSea,
    Rocks,
    Lumber,
    ThreadForCloth,
    Clay,
}