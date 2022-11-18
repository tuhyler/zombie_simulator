using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class ResourceManager : MonoBehaviour
{
    private Dictionary<ResourceType, int> resourceDict = new(); //need this later for save system
    private Dictionary<ResourceType, float> resourceStorageMultiplierDict = new();
    private Dictionary<ResourceType, float> resourceGenerationPerMinuteDict = new(); //for resource generation stats
    private Dictionary<ResourceType, float> resourceConsumedPerMinuteDict = new(); //for resource consumption stats

    public Dictionary<ResourceType, int> ResourceDict { get { return resourceDict; } }

    private int resourceStorageLimit; 
    public int ResourceStorageLimit { get { return resourceStorageLimit; } set { resourceStorageLimit = value; } }
    private float resourceStorageLevel;
    public float GetResourceStorageLevel { get { return resourceStorageLevel; } }
    private UIResourceManager uiResourceManager;
    [HideInInspector]
    public UIInfoPanelCity uiInfoPanelCity;

    //private Dictionary<Vector3Int, Dictionary<ResourceType, int>> currentWorkedResourceGenerationDict = new(); //to see how many resources generated per turn in tile.
    //private Dictionary<string , int> buildingResourceGenerationDict = new(); //to see how many building resources generated per turn in city.

    //initial resources
    public List<ResourceValue> initialResources = new(); //resources you start a city with
    public City city;

    //for managing food consumption
    private bool growth;
    private int foodGrowthLevel;
    public int FoodGrowthLevel { get { return foodGrowthLevel; } }
    private int foodGrowthLimit;
    public int FoodGrowthLimit { get { return foodGrowthLimit; } }
    public float FoodPerMinute { get { return resourceGenerationPerMinuteDict[ResourceType.Food]; } }

    //for queued build orders
    private List<ResourceValue> queuedResourcesToCheck = new();
    private List<ResourceType> queuedResourceTypesToCheck = new();
    private CityBuilderManager cityBuilderManager; //only instantiated through queue build

    private void Awake()
    {
        CalculateAndChangeFoodLimit();
        PrepareResourceDictionary();
        SetInitialResourceValues();
    }

    private void PrepareResourceDictionary()
    {
        foreach (ResourceIndividualSO resourceData in ResourceHolder.Instance.allStorableResources.Concat(ResourceHolder.Instance.allWorldResources).ToList())
        {
            if (resourceData.resourceType == ResourceType.None)
                continue;
            resourceDict[resourceData.resourceType] = 0;
            if (resourceData.resourceStorageMultiplier <= 0) //absolutely cannot be zero or less
                resourceStorageMultiplierDict[resourceData.resourceType] = 1;
            resourceStorageMultiplierDict[resourceData.resourceType] = resourceData.resourceStorageMultiplier;
            resourceGenerationPerMinuteDict[resourceData.resourceType] = 0;
            resourceConsumedPerMinuteDict[resourceData.resourceType] = 0;
        }
    }

    internal void SetCity(City city)
    {
        this.city = city;
    }

    private void SetInitialResourceValues()
    {
        foreach (ResourceValue resourceData in initialResources)
        {
            ResourceType resourceType = resourceData.resourceType;
            if (resourceType == ResourceType.None)
                throw new ArgumentException("Resource can't be none!");
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

    public void ConsumeResources(List<ResourceValue> consumedResource, float currentLabor)
    {
        foreach (ResourceValue resourceValue in consumedResource)
        {
            int consumedAmount = Mathf.RoundToInt(resourceValue.resourceAmount * currentLabor);
            ResourceType resourceType = resourceValue.resourceType;

            resourceConsumedPerMinuteDict[resourceType] = consumedAmount;

            if (resourceValue.resourceType == ResourceType.Food && consumedAmount > resourceDict[resourceType])
            {
                foodGrowthLevel -= consumedAmount - resourceDict[resourceType];
                consumedAmount = resourceDict[resourceType];
            }

            resourceDict[resourceType] -= consumedAmount;
            resourceStorageLevel -= consumedAmount;
            city.CheckLimitWaiter();

            UpdateUI(resourceType);
        }
    }

    public void PrepareResource(List<ResourceValue> producedResource, float currentLabor, bool returnResource = false)
    {
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

            CheckResource(resourceVal.resourceType, newResourceAmount);
        }
    }

    public int CheckResource(ResourceType resourceType, int newResourceAmount)
    {
        if (resourceType == ResourceType.Food && newResourceAmount > 0)
        {
            return AddFood(newResourceAmount);
        }
        else if (CheckStorageSpaceForResource(resourceType, newResourceAmount) && resourceDict.ContainsKey(resourceType))
        {
            return AddResourceToStorage(resourceType, newResourceAmount);
        }
        else
        {
            Debug.Log($"Error moving {resourceType}!");
            return 0;
        }
    }

    private int AddFood(int resourceAmount)
    {
        int newResourceBalance = (foodGrowthLevel + resourceAmount) - foodGrowthLimit;

        if (newResourceBalance >= 0)
        {
            growth = true;
            resourceAmount -= newResourceBalance;
            foodGrowthLevel += resourceAmount;
            if (city.activeCity)
                uiInfoPanelCity.UpdateFoodGrowth(foodGrowthLevel);
            return AddResourceToStorage(ResourceType.Food, newResourceBalance);
        }

        foodGrowthLevel += resourceAmount;

        if (city.activeCity)
            uiInfoPanelCity.UpdateFoodGrowth(foodGrowthLevel);

        return resourceAmount;
    }

    //returns how much is actually moved
    private int AddResourceToStorage(ResourceType resourceType, int resourceAmount)
    {
        //check to ensure you don't take out more resources than are available in dictionary
        if (resourceAmount < 0 && -resourceAmount > resourceDict[resourceType])
        {
            resourceAmount = -resourceDict[resourceType];
        }
        
        if (resourceStorageMultiplierDict.ContainsKey(resourceType))
            resourceAmount = Mathf.CeilToInt(resourceAmount * resourceStorageMultiplierDict[resourceType]);

        //adjusting resource amount to move based on how much space is available
        int newResourceAmount = resourceAmount;
        int newResourceBalance = (Mathf.CeilToInt(resourceStorageLevel) + newResourceAmount) - resourceStorageLimit;
        if (newResourceBalance >= 0 && resourceStorageLimit > 0) //limit of 0 or less means infinite storage
        {
            newResourceAmount -= newResourceBalance;
        }

        int resourceAmountAdjusted = Mathf.RoundToInt(newResourceAmount / resourceStorageMultiplierDict[resourceType]);

        resourceDict[resourceType] += resourceAmountAdjusted; //updating the dictionary

        resourceStorageLevel += newResourceAmount;

        int wasteCheck = 0;
        if (resourceStorageMultiplierDict.ContainsKey(resourceType) && resourceStorageMultiplierDict[resourceType] > 0)
            wasteCheck = Mathf.RoundToInt((resourceAmount - newResourceAmount) / resourceStorageMultiplierDict[resourceType]);

        if (wasteCheck > 0)
            Debug.Log($"Wasted {wasteCheck} of {resourceType}");

        if (queuedResourceTypesToCheck.Contains(resourceType))
            CheckResourcesForQueue();
        UpdateUI(resourceType);
        if (newResourceAmount > 0)
            city.CheckResourceWaiter(resourceType);
        else if (newResourceAmount < 0)
            city.CheckLimitWaiter();
        
        return resourceAmountAdjusted;
    }

    public int CalculateResourceGeneration(int resourceAmount, float labor)
    {
        return Mathf.FloorToInt(city.GetSetWorkEthic * (resourceAmount * labor * (1 + .1f * (labor - 1))));
    }

    private void CalculateAndChangeFoodLimit()
    {
        int cityPop = city.cityPop.GetPop;
        int newLimit = 9 + cityPop;

        foodGrowthLimit = newLimit;
        //SetResourceLimitValues(ResourceType.Food, foodGrowthLimit);
    }


    public void SpendResource(List<ResourceValue> buildCost)
    {
        foreach (ResourceValue resourceValue in buildCost)
        {
            SpendResource(resourceValue.resourceType, resourceValue.resourceAmount);
        }
    }

    private void SpendResource(ResourceType resourceType, int resourceAmount) //changing the resource counter upon using resources
    {
        //ResourceType resourceValue = resourceValue.resourceValue; 
        resourceDict[resourceType] -= resourceAmount; //subtract cost
        if (resourceStorageMultiplierDict.ContainsKey(resourceType))
        {
            resourceStorageLevel -= resourceAmount * resourceStorageMultiplierDict[resourceType];
        }
        city.CheckLimitWaiter();
        VerifyResourceAmount(resourceType);
        //UpdateUI(resourceValue);
    }

    public void SetUI(UIResourceManager uiResourceManager, UIInfoPanelCity uiInfoPanelCity)
    {
        this.uiResourceManager = uiResourceManager;
        this.uiInfoPanelCity = uiInfoPanelCity;
    }

    public void UpdateUI() //updating the UI with the resource information in the dictionary
    {
        foreach (ResourceType resourceType in resourceDict.Keys)
        {
            UpdateUI(resourceType);
        }
    }

    private void UpdateUI(ResourceType resourceType)
    {
        if (city.activeCity) //only update UI for currently selected city
        {
            uiResourceManager.SetResource(resourceType, resourceDict[resourceType]);
            //uiResourceManager.SetResourceGenerationAmount(resourceType, resourceGenerationPerMinuteDict[resourceType]);
        }
    }

    public void IncreaseFoodConsumptionPerTurn(bool v) //only used when increasing pop when joining city, growth, or building city
    {
        if (v)
            resourceGenerationPerMinuteDict[ResourceType.Food] -= city.unitFoodConsumptionPerTurn;
        else
            resourceGenerationPerMinuteDict[ResourceType.Food] += city.unitFoodConsumptionPerTurn;
    }

    public int GetResourceLimit(ResourceType resourceType)
    {
        return resourceStorageLimit;
    }

    public int GetResourceDictValue(ResourceType resourceType)
    {
        return resourceDict[resourceType];
    }

    //checking if enough food to grow
    public void CheckForPopGrowth()
    {
        if (growth) //increasing pop if food over or equal to max
        {
            growth = false;
            int excessFood = 0;

            city.PopulationGrowthCheck();
            CalculateAndChangeFoodLimit();

            if (resourceDict[ResourceType.Food] > 0) //can only carry over one limit of food, rest of food goes to storage
            {
                if (resourceDict[ResourceType.Food] > foodGrowthLimit)
                {
                    excessFood = foodGrowthLimit;
                    resourceDict[ResourceType.Food] -= foodGrowthLimit;
                    resourceStorageLevel -= foodGrowthLimit;
                }
                else
                {
                    excessFood = resourceDict[ResourceType.Food];
                    resourceDict[ResourceType.Food] -= excessFood;
                    resourceStorageLevel -= excessFood;
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

        if (city.activeCity)
            uiInfoPanelCity.UpdateFoodStats(city.cityPop.GetPop, foodGrowthLevel, foodGrowthLimit, FoodPerMinute, city.FoodConsumptionPerMinute);
    }



    //for queued build orders in cities
    //public void SetCityBuilderManager(CityBuilderManager cityBuilderManager)
    //{
    //    this.cityBuilderManager = cityBuilderManager;
    //}

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

            cityBuilderManager.BuildQueuedBuilding(city, this);
            queuedResourcesToCheck.Clear();
            queuedResourceTypesToCheck.Clear();
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
    Food,
    Gold,
    Research,
    Lumber,
    Stone,
}