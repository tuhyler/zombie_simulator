using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class ResourceManager : MonoBehaviour
{
    private Dictionary<ResourceType, int> resourceDict = new(); //need this later for save system
    private Dictionary<ResourceType, float> resourceStorageMultiplierDict = new();
    private Dictionary<ResourceType, int> resourceGenerationPerTurnDict = new();
    private Dictionary<ResourceType, int> resourceConsumedDict = new();

    public Dictionary<ResourceType, int> ResourceDict { get { return resourceDict; } }

    private int resourceStorageLimit; 
    public int ResourceStorageLimit { get { return resourceStorageLimit; } set { resourceStorageLimit = value; } }
    private float resourceStorageLevel;
    public float GetResourceStorageLevel { get { return resourceStorageLevel; } }

    private Dictionary<Vector3Int, Dictionary<ResourceType, int>> currentWorkedResourceGenerationDict = new(); //to see how many resources generated per turn in tile.
    private Dictionary<string , int> buildingResourceGenerationDict = new(); //to see how many building resources generated per turn in city.

    //initial resources
    public List<ResourceValue> initialResources = new(); //resources you start a city with
    private City city;

    //for managing food consumption
    private bool consumeResources; //allows resources to be consumed before adding resources to the coffers on turn end
    private bool growth;
    private int foodGrowthLevel;
    public int FoodGrowthLevel { get { return foodGrowthLevel; } }
    private int foodGrowthLimit;
    public int FoodGrowthLimit { get { return foodGrowthLimit; } }
    public int FoodPerTurn { get { return resourceGenerationPerTurnDict[ResourceType.Food]; } }

    //for queued build orders
    private List<ResourceValue> queuedResourcesToCheck = new();
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
            resourceGenerationPerTurnDict[resourceData.resourceType] = 0;
            resourceConsumedDict[resourceData.resourceType] = 0;
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

    public void BeginResourceGeneration(List<ResourceValue> producedResources) //for city tiles
    {
        UpdateResourceGeneration(producedResources, Vector3Int.FloorToInt(city.transform.position));

        IncreaseFoodConsumptionPerTurn(true);        
    }

    private void ModifyResourceGenerationPerTurn(ResourceType resourceType, int generationDiff)
    {
        resourceGenerationPerTurnDict[resourceType] += generationDiff;

        if (city.CheckIfWorldResource(resourceType))
        {
            //city.AddToChangedResourcesList(resourceType);
            city.UpdateWorldResourceGeneration(resourceType, generationDiff);
        }
    }

    public void ModifyResourceConsumptionPerTurn(ResourceType resourceType, int change)
    {
        resourceConsumedDict[resourceType] = change; //change completely for food
    }

    public void RemoveKeyFromGenerationDict(Vector3Int pos)
    {
        currentWorkedResourceGenerationDict.Remove(pos);
    }

    public void RemoveKeyFromBuildingGenerationDict(string buildingName)
    {
        buildingResourceGenerationDict.Remove(buildingName);
    }

    public int GetResourceValues(ResourceType resourceType)
    {
        return resourceDict[resourceType];
    }

    public int GetResourceGenerationValues(ResourceType resourceType)
    {
        return resourceGenerationPerTurnDict[resourceType];
    }

    public bool CheckResourceAvailability(ResourceValue resourceRequired) //this will be used by building system to see if we have enough resources
    {
        return resourceDict[resourceRequired.resourceType] >= resourceRequired.resourceAmount;
    }

    public bool CheckStorageSpaceForResource(ResourceType resourceType, int resourceAdded)
    {
        if (resourceAdded > 0 && resourceStorageLimit <= 0)
            return true;
        if (resourceAdded < 0 && resourceDict[resourceType] == 0)
            return false;
        return Mathf.CeilToInt(resourceStorageLevel + resourceStorageMultiplierDict[resourceType]) <= resourceStorageLimit;
    }

    private void VerifyResourceAmount(ResourceType resourceType)
    {
        if (resourceDict[resourceType] < 0)
            throw new InvalidOperationException("Can't have resources less than 0 " + resourceType);
    }

    public void UpdateResourceGeneration(List<ResourceValue> producedResource, Vector3Int laborLocation, int currentLabor = 1)
    {
        foreach (ResourceValue resourceVal in producedResource)
        {
            int prevResourceAmount = 0;
            if (currentWorkedResourceGenerationDict.ContainsKey(laborLocation))
                prevResourceAmount = currentWorkedResourceGenerationDict[laborLocation][resourceVal.resourceType];
            else
                currentWorkedResourceGenerationDict[laborLocation] = new Dictionary<ResourceType, int>();
            int newResourceAmount = CalculateResourceGeneration(resourceVal.resourceAmount, currentLabor);
            currentWorkedResourceGenerationDict[laborLocation][resourceVal.resourceType] = newResourceAmount;
            ModifyResourceGenerationPerTurn(resourceVal.resourceType, newResourceAmount - prevResourceAmount); //subtract to pass difference
        }
    }

    public void UpdateBuildingResourceGeneration(List<ResourceValue> producedResource, string buildingName, int currentLabor)
    {
        foreach (ResourceValue resourceVal in producedResource)
        {
            int prevResourceAmount = 0;
            if (buildingResourceGenerationDict.ContainsKey(buildingName))
                prevResourceAmount = buildingResourceGenerationDict[buildingName];
            int newResourceAmount = CalculateResourceGeneration(resourceVal.resourceAmount, currentLabor);
            buildingResourceGenerationDict[buildingName] = newResourceAmount;
            ModifyResourceGenerationPerTurn(resourceVal.resourceType, newResourceAmount - prevResourceAmount); //subtract to pass difference
        }
    }

    private void ConsumeResources()
    {
        foreach (ResourceType resourceType in resourceConsumedDict.Keys)
        {
            int consumedAmount = resourceConsumedDict[resourceType];

            if (resourceType == ResourceType.Food && consumedAmount > resourceDict[resourceType])
            {
                foodGrowthLevel -= consumedAmount - resourceDict[resourceType];
                consumedAmount = resourceDict[resourceType];
            }
            resourceDict[resourceType] -= consumedAmount;
            resourceStorageLevel -= consumedAmount;
        }
    }

    public void PrepareResource(List<ResourceValue> producedResource, int currentLabor)
    {
        ConsumeResourcesCheck(); //this is here because prepare resources runs before resourcemanager on turn end

        foreach (ResourceValue resourceVal in producedResource)
        {
            int newResourceAmount = CalculateResourceGeneration(resourceVal.resourceAmount, currentLabor);

            CheckResource(resourceVal.resourceType, newResourceAmount);
        }
    }

    private void ConsumeResourcesCheck()
    {
        if (!consumeResources) //because of this, PrepareResource can only be used in WaitTurn method
        {
            consumeResources = true;
            ConsumeResources();
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
            return AddResourceToStorage(ResourceType.Food, newResourceBalance);
        }

        foodGrowthLevel += resourceAmount;
        return resourceAmount;
    }

    private int AddResourceToStorage(ResourceType resourceType, int resourceAmount)
    {
        if (resourceAmount < 0 && -resourceAmount > resourceDict[resourceType])
        {
            resourceAmount = -resourceDict[resourceType];
        }
        
        if (resourceStorageMultiplierDict.ContainsKey(resourceType))
            resourceAmount = Mathf.CeilToInt(resourceAmount * resourceStorageMultiplierDict[resourceType]);

        int newResourceAmount = resourceAmount;
        int newResourceBalance = (Mathf.CeilToInt(resourceStorageLevel) + newResourceAmount) - resourceStorageLimit;

        if (newResourceBalance >= 0 && resourceStorageLimit > 0) //limit of 0 or less means infinite storage
        {
            newResourceAmount -= newResourceBalance;
        }

        int resourceAmountAdjusted = Mathf.RoundToInt(newResourceAmount / resourceStorageMultiplierDict[resourceType]);

        resourceDict[resourceType] += resourceAmountAdjusted; //updating the dictionary
        VerifyResourceAmount(resourceType); //check to see if resource is less than 0 (just in case)

        resourceStorageLevel += newResourceAmount;

        int wasteCheck = 0;
        if (resourceStorageMultiplierDict.ContainsKey(resourceType) && resourceStorageMultiplierDict[resourceType] > 0)
            wasteCheck = Mathf.RoundToInt((resourceAmount - newResourceAmount) / resourceStorageMultiplierDict[resourceType]);

        if (wasteCheck > 0)
            Debug.Log($"Wasted {wasteCheck} of {resourceType}");

        return resourceAmountAdjusted;
    }

    private int CalculateResourceGeneration(int resourceAmount, int labor)
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
        //ResourceType resourceType = resourceValue.resourceType; 
        resourceDict[resourceType] -= resourceAmount; //subtract cost
        if (resourceStorageMultiplierDict.ContainsKey(resourceType))
        {
            resourceStorageLevel -= resourceAmount * resourceStorageMultiplierDict[resourceType];
        }
        VerifyResourceAmount(resourceType);
        //UpdateUI(resourceType);
    }

    public void SetUI(UIResourceManager uiResourceManager, string cityName, int cityStorageLimit, float cityStorageLevel)
    {
        uiResourceManager.SetCityInfo(cityName, cityStorageLimit, cityStorageLevel);
    }

    public void UpdateUI(UIResourceManager uiResourceManager) //updating the UI with the resource information in the dictionary
    {
        foreach (ResourceType resourceType in resourceDict.Keys)
        {
            UpdateUI(resourceType, uiResourceManager);
        }
    }

    private void UpdateUI(ResourceType resourceType, UIResourceManager uiResourceManager)
    {
        uiResourceManager.SetResource(resourceType, resourceDict[resourceType]);
        uiResourceManager.SetResourceGenerationAmount(resourceType, resourceGenerationPerTurnDict[resourceType]);
    }

    public void UpdateUIGeneration(ResourceType resourceType, UIResourceManager uiResourceManager)
    {
        uiResourceManager.SetResourceGenerationAmount(resourceType, resourceGenerationPerTurnDict[resourceType]);
    }

    public void UpdateUIGenerationAll(UIResourceManager uiResourceManager)
    {
        foreach (ResourceType resourceType in resourceDict.Keys)
        {
            uiResourceManager.SetResourceGenerationAmount(resourceType, resourceGenerationPerTurnDict[resourceType]);
        }
    }

    public void IncreaseFoodConsumptionPerTurn(bool v) //only used when increasing pop when joining city, growth, or building city
    {
        if (v)
            resourceGenerationPerTurnDict[ResourceType.Food] -= city.unitFoodConsumptionPerTurn;
        else
            resourceGenerationPerTurnDict[ResourceType.Food] += city.unitFoodConsumptionPerTurn;
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
    private void CheckForPopGrowth()
    {
        consumeResources = false;

        if (growth) //increasing pop if food over or equal to max
        {
            growth = false;
            int excessFood = 0;

            city.PopulationGrowthCheck();
            CalculateAndChangeFoodLimit();

            if (resourceDict[ResourceType.Food] > 0) //can only carry over one turn limit of food, rest of food goes to storage
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
            }
        }

        if (foodGrowthLevel < 0) //decreasing pop if food under 0 for 2 straight turns
        {
            city.PopulationDeclineTurnCount++; //changing counter of starving labor
            Debug.Log($"{city.CityName} is losing food for {city.PopulationDeclineTurnCount} turns");
            int turnWaitTillDecline = 2; //cannot be zero!
            if (city.PopulationDeclineTurnCount >= turnWaitTillDecline) //if in the negative for 2 straight turns, remove pop based on food lost. 
            {
                for (int i = 0; i < Mathf.Max(Mathf.Abs(foodGrowthLevel) / turnWaitTillDecline, 1); i++) //Lose a minimum of one
                {
                    city.PopulationDeclineCheck();
                }

                city.PopulationDeclineTurnCount = 0;
                foodGrowthLevel = 0;
                CalculateAndChangeFoodLimit();
            }
        }
        else
        {
            city.PopulationDeclineTurnCount = 0;
        }

        city.UpdateCityPopInfo(); //update city info after correcting food info
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