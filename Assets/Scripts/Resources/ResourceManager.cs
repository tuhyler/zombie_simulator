using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Mono.Cecil;
using System.Resources;
using UnityEditor.iOS;
using static UnityEngine.Rendering.DebugUI;

public class ResourceManager : MonoBehaviour
{
    private Dictionary<ResourceType, int> resourceDict = new(); //need this later for save system
    private Dictionary<ResourceType, float> resourceGenerationPerMinuteDict = new(); //for resource generation stats
    public Dictionary<ResourceType, float> resourceConsumedPerMinuteDict = new(); //for resource consumption stats
    public Dictionary<ResourceType, int> resourcePriceDict = new();
    public List<ResourceType> resourceSellList = new();
    public Dictionary<ResourceType, int> resourceMinHoldDict = new();
    public Dictionary<ResourceType, int> resourceSellHistoryDict = new();

    public Dictionary<ResourceType, int> ResourceDict { get { return resourceDict; } set { resourceDict = value; } }

    //private int resourceStorageLimit; 
    //public int ResourceStorageLimit { get { return resourceStorageLimit; } set { resourceStorageLimit = value; } }
    private int resourceStorageLevel;
    public int ResourceStorageLevel { get { return resourceStorageLevel; } set { resourceStorageLevel = value; } }
    [HideInInspector]
    public Queue<ResourceProducer> waitingToUnloadProducers = new();
    //[HideInInspector]
    //public List<ResourceProducer> waitingToUnloadResearch = new();
    [HideInInspector]
    public bool fullInventory;

    //wait list for research
    //private List<ResourceProducer> waitingForResearchProducerList = new();

    //wait list for inventory space
    [HideInInspector]
    public List<ResourceProducer> waitingForStorageRoomProducerList = new();

    //consuming resources
    [HideInInspector]
    public List<ResourceProducer> waitingforResourceProducerList = new();
    [HideInInspector]
    public List<ResourceType> resourcesNeededForProduction = new();
    [HideInInspector]
    public List<Trader> waitingForTraderList = new();
    [HideInInspector]
    public List<ResourceType> resourcesNeededForRoute = new();

    //initial resources
    public List<ResourceValue> initialResources = new(); //resources you start a city with
    [HideInInspector]
    public City city;

    //for managing food consumption
    //private bool growth;
    [HideInInspector]
    public bool pauseGrowth, growthDeclineDanger;
    public int cyclesToWait = 2;
    [HideInInspector]
    public int starvationCount, noHousingCount, noWaterCount;
    //private int foodGrowthLevel;
    //public int FoodGrowthLevel { get { return foodGrowthLevel; } }
    //private int foodGrowthLimit;
    //public int FoodGrowthLimit { get { return foodGrowthLimit; } }
    //public float FoodPerMinute { get { return resourceGenerationPerMinuteDict[ResourceType.Food]; } }
    private int cycleCount;
    public int CycleCount { get { return cycleCount; } set { cycleCount = value; } }

    //for queued build orders
    public Dictionary<ResourceType, int> queuedResourcesToCheck = new();
    //private List<ResourceType> queuedResourceTypesToCheck = new(); //have this to check if the queue type has recently been added (can't check values easily)   
    //private CityBuilderManager cityBuilderManager; //only instantiated through queue build

    private int resourceCount; //for counting wasted resources

    private void Awake()
    {
        //CalculateAndChangeFoodLimit();
        //PrepareResourceDictionary();
        //SetInitialResourceValues();
        //SetPrices();

        //only relevant during editing
        //if (resourceStorageLevel >= city.warehouseStorageLimit)
        //    fullInventory = true;
    }

    public void PrepareResourceDictionary()
    {
        foreach (ResourceIndividualSO resourceData in ResourceHolder.Instance.allStorableResources.Concat(ResourceHolder.Instance.allWorldResources).ToList())
        {
            ResourceType type = resourceData.resourceType;
            
            if (!city.world.ResourceCheck(type))
                continue;
            resourceGenerationPerMinuteDict[type] = 0;

            if (type != ResourceType.Research)
            {
                resourceDict[type] = 0;
                resourceConsumedPerMinuteDict[type] = 0;
                if (ResourceHolder.Instance.GetSell(type))
                    resourceSellList.Add(type);
                resourcePriceDict[type] = ResourceHolder.Instance.GetPrice(type);
                resourceMinHoldDict[type] = 0;
                resourceSellHistoryDict[type] = 0;
            }
        }
    }

    public void UpdateDicts(ResourceType type)
    {
		resourceDict[type] = 0;
		resourceGenerationPerMinuteDict[type] = 0;
		resourceConsumedPerMinuteDict[type] = 0;
        if (ResourceHolder.Instance.GetSell(type))
            resourceSellList.Add(type);
        resourcePriceDict[type] = ResourceHolder.Instance.GetPrice(type);
        resourceMinHoldDict[type] = 0;
        resourceSellHistoryDict[type] = 0;
	}

    public void SetCity(City city)
    {
        this.city = city;
    }

    public void SetInitialResourceValues()
    {
        foreach (ResourceValue resourceData in initialResources)
        {
            ResourceType resourceType = resourceData.resourceType;
            resourceDict[resourceType] = resourceData.resourceAmount; //assigns the initial values for each resource
            resourceStorageLevel += resourceData.resourceAmount /** city.world.resourceStorageMultiplierDict[resourceType]*/;
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

    public void ModifyResourceConsumptionPerMinute(ResourceType resourceType, float change)
    {
        resourceConsumedPerMinuteDict[resourceType] += change;
    }

    public float GetResourceGenerationValues(ResourceType resourceType)
    {
        return resourceGenerationPerMinuteDict[resourceType];
    }

    public bool CheckResourceAvailability(ResourceValue resourceRequired) //this will be used by building system to see if we have enough resources
    {
        return resourceDict[resourceRequired.resourceType] >= resourceRequired.resourceAmount;
    }

    public bool CheckResourceTypeAvailability(ResourceType type, int amount)
    {
		return resourceDict[type] >= amount;
	} 

    //public bool CheckStorageSpaceForResource(ResourceType resourceType, int resourceAdded)
    //{
    //    if (resourceAdded < 0 && resourceDict[resourceType] > 0)
    //        return true;
    //    else if (resourceAdded < 0 && resourceDict[resourceType] == 0)
    //        return false;
    //    //else if (city.world.resourceStorageMultiplierDict[resourceType] < 1)
    //    //    return Mathf.CeilToInt(resourceStorageLevel + city.world.resourceStorageMultiplierDict[resourceType]) <= city.warehouseStorageLimit;
    //    else if (resourceAdded > 0 && city.warehouseStorageLimit <= 0) //for infinite storage
    //        return true;

    //    return resourceStorageLevel < city.warehouseStorageLimit;
    //}

    private void VerifyResourceAmount(ResourceType resourceType)
    {
        if (resourceDict[resourceType] < 0)
            throw new InvalidOperationException("Can't have resources less than 0 " + resourceType);
    }

    public void ConsumeResources(List<ResourceValue> consumedResource, float currentLabor, Vector3 location, bool military = false, bool showSpend = false)
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
                if (military && !city.CheckWorldGold(value.resourceAmount))
                {
                    consumedAmount = city.GetWorldGoldLevel();
                    city.army.AWOLCheck();
                }

                city.UpdateWorldResources(resourceType, -consumedAmount);
            }
            else
            {
                int storageAmount = resourceDict[resourceType];

                resourceDict[resourceType] -= consumedAmount;
                resourceStorageLevel -= consumedAmount;
                CheckProducerUnloadWaitList();
                city.CheckLimitWaiter();
                UICheck(resourceType, consumedAmount, storageAmount);
            }

            if (showSpend)
            {
				Vector3 loc = location;
				loc.y -= 0.4f * i;
				InfoResourcePopUpHandler.CreateResourceStat(loc, -consumedAmount, ResourceHolder.Instance.GetIcon(resourceType));
				i++;
			}
			else if (city.activeCity && consumedAmount > 0)
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

    public bool ConsumeResourcesForRouteCheck(List<ResourceValue> consumeResources)
    {
        for (int i = 0; i < consumeResources.Count; i++)
        {
            if (consumeResources[i].resourceType == ResourceType.Gold)
            {
                if (!city.CheckWorldGold(consumeResources[i].resourceAmount))
                {
                    city.AddToWorldGoldWaitList(true);
                    return false;
                }
            }
            else if (resourceDict[consumeResources[i].resourceType] < consumeResources[i].resourceAmount)
            {
                return false;
            }
        }

        return true;
    }

    public bool PrepareConsumedResource(List<ResourceValue> producedResource, float currentLabor, Vector3 producerLoc, bool returnResource = false)
    {
        bool destroy = false;
        
        int i = 0;
        //producerLoc.y += producedResource.Count * 0.4f;
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
                newResourceAmount = CalculateResourceGeneration(resourceVal.resourceAmount, currentLabor, resourceVal.resourceType);
            }

            int resourceAmount = SubtractResource(resourceVal.resourceType, newResourceAmount);

            Vector3 loc = producerLoc;
            loc.y += 0.4f * i;

            if (resourceAmount != 0)
                InfoResourcePopUpHandler.CreateResourceStat(loc, resourceAmount, ResourceHolder.Instance.GetIcon(resourceVal.resourceType));
            i++;
        }

        return destroy;
    }

	public bool PrepareResource(ResourceValue producedResource, float currentLabor, Vector3 producerLoc, CityImprovement improvement = null)
	{
		bool destroy = false;

		resourceCount = 0;
		int newResourceAmount = CalculateResourceGeneration(producedResource.resourceAmount, currentLabor, producedResource.resourceType);	

		if (improvement.GetImprovementData.rawMaterials && improvement.td.resourceAmount > 0 && improvement.td.resourceAmount < newResourceAmount)
			newResourceAmount = improvement.td.resourceAmount;

		int resourceAmount = AddResource(producedResource.resourceType, newResourceAmount);

		if (improvement.GetImprovementData.rawMaterials && improvement.td.resourceAmount > 0)
		{
			improvement.td.resourceAmount -= resourceAmount;
			city.world.uiCityImprovementTip.UpdateResourceAmount(improvement);

			if (improvement.td.resourceAmount <= 0)
			{
				improvement.td.resourceAmount = -1;
				destroy = true;
			}
		}

		Vector3 loc = producerLoc;

		if (resourceAmount != 0)
			InfoResourcePopUpHandler.CreateResourceStat(loc, resourceAmount, ResourceHolder.Instance.GetIcon(producedResource.resourceType));

		return destroy;
	}

	//public int CheckResource(ResourceType type, int amount)
 //   {
	//	if (type == ResourceType.Fish)
	//		type = ResourceType.Food;

	//	if (type == ResourceType.Gold || type == ResourceType.Research)
 //       {
 //           city.UpdateWorldResources(type, amount);
 //           return amount;
 //       }
 //       else if (CheckStorageSpaceForResource(type, amount) && resourceDict.ContainsKey(type))
 //       {
 //           if (!city.resourceGridDict.ContainsKey(type))
 //               city.AddToGrid(type);

 //           return AddResourceToStorage(type, amount);
 //       }
 //       else
 //       {
 //           return 0;
 //       }
 //   }

    public int SubtractResource(ResourceType type, int amount)
    {
		if (type == ResourceType.Gold)
		{
			city.UpdateWorldResources(type, amount);
			return amount;
		}

		amount = SubtractResourceCheck(type, amount);
        if (amount > 0)
            SubtractResourceFromStorage(type, amount);

        return amount;
    }

    //slightly faster
    public int SubtractTraderResource(ResourceType type, int amount)
    {
		amount = SubtractResourceCheck(type, amount);
		if (amount > 0)
			SubtractResourceFromStorage(type, amount);

		return amount;
	}

    private int SubtractResourceCheck(ResourceType type, int amount)
    {
		int prevAmount = resourceDict[type];

		if (prevAmount < amount)
			amount = prevAmount;

		return amount;
	}

    public int AddResource(ResourceType type, int amount)
    {
		if (type == ResourceType.Fish)
			type = ResourceType.Food;

		if (type == ResourceType.Gold || type == ResourceType.Research)
		{
			city.UpdateWorldResources(type, amount);
			return amount;
		}

		amount = AddResourceCheck(type, amount);
		if (amount > 0)
			AddResourceToStorage(type, amount);

        return amount;
	}

    //slightly faster
    public int AddTraderResource(ResourceType type, int amount)
    {
        amount = AddResourceCheck(type, amount);
        if (amount > 0)
            AddResourceToStorage(type, amount);

        return amount;
    }

	private int AddResourceCheck(ResourceType type, int amount)
    {
        int diff = city.warehouseStorageLimit - resourceStorageLevel;

        if (diff < amount)
        {
			Vector3 loc = city.cityLoc;
			loc.y += 2f; //limit of 5 different resource types at once wasted
			loc.y += -.4f * resourceCount;
			InfoResourcePopUpHandler.CreateResourceStat(loc, amount - diff, ResourceHolder.Instance.GetIcon(type), true);
			resourceCount++;

            amount = diff;
		}

		if (amount > 0 && !city.resourceGridDict.ContainsKey(type))
			city.AddToGrid(type);

		return amount;
	}

    private void AddResourceToStorage(ResourceType type, int amount)
    {
		int prevAmount = resourceDict[type];
        resourceDict[type] += amount;
		resourceStorageLevel += amount;

		if (resourceStorageLevel >= city.warehouseStorageLimit)
			fullInventory = true;

        //check lists needing resources, in this order
		if (resourcesNeededForRoute.Contains(type))
			CheckTraderWaitList(type);
		city.CheckResourceWaiter(type);
		if (resourcesNeededForProduction.Contains(type))
			CheckProducerResourceWaitList(type);
		if (queuedResourcesToCheck.ContainsKey(type))
            CheckResourcesForQueue();

        CityAutoGrowCheck(type);
		UICheck(type, amount, prevAmount);
	}

    private void SubtractResourceFromStorage(ResourceType type, int amount)
    {
		int prevAmount = resourceDict[type];
        resourceDict[type] -= amount;
		resourceStorageLevel -= amount;

        if (fullInventory)
            fullInventory = false;

        //check lists needing inventory room, in this order
		city.CheckLimitWaiter();
        CheckProducerUnloadWaitList();

		UICheck(type, amount, prevAmount);
	}

    private void UICheck(ResourceType type, int amount, int prevAmount)
    {
		if (city.activeCity) //only update UI for currently selected city
		{
			city.world.cityBuilderManager.uiResourceManager.SetResource(type, resourceDict[type]);
			city.world.cityBuilderManager.uiResourceManager.SetCityCurrentStorage(resourceStorageLevel);

			if (city.world.cityBuilderManager.uiMarketPlaceManager.activeStatus)
				city.world.cityBuilderManager.uiMarketPlaceManager.UpdateMarketResourceNumbers(type, resourceDict[type]/*, resourceSellHistoryDict[resourceType]*/);

            if (city.world.cityBuilderManager.uiCityUpgradePanel.activeStatus)
				city.world.cityBuilderManager.uiCityUpgradePanel.CheckCosts(this);

            if (city.world.cityBuilderManager.buildOptionsActive)
    			city.CheckBuildOptionsResource(type, prevAmount, resourceDict[type], amount > 0);

            if (city.uiCityResourceInfoPanel)
            {
				city.uiCityResourceInfoPanel.UpdateResourceInteractable(type, resourceDict[type], false); //false so it doesn't play ps
				city.uiCityResourceInfoPanel.UpdateStorageLevel(ResourceStorageLevel);
			}

            if (type == ResourceType.Food && city.world.uiCityPopIncreasePanel.CheckCity(city))
				city.world.uiCityPopIncreasePanel.UpdateFoodCosts(city);
		}
		else if (city.army.DeployBattleScreenCheck())
        {
			city.world.uiCampTooltip.UpdateBattleCostCheck(resourceDict[type], type);
        }
		else if (city.world.uiTradeRouteBeginTooltip.CityCheck(city))
        {
			city.world.uiTradeRouteBeginTooltip.UpdateRouteCost(resourceDict[type], type);
        }
	}

    private void CityAutoGrowCheck(ResourceType type)
    {
		if (city.autoGrow && city.cityPop.CurrentPop == 0 && city.HousingCount > 0 && !city.reachedWaterLimit && type == ResourceType.Food && resourceDict[type] >= city.growthFood && !pauseGrowth)
		{
			resourceDict[type] -= city.growthFood;
			city.PopulationGrowthCheck(false, 1);
		}
	}

    //returns how much is actually moved
  //  private int AddResourceToStorage(ResourceType type, int resourceAmount)
  //  {
  //      int prevAmount = resourceDict[type];
        
  //      //check to ensure you don't take out more resources than are available in dictionary
  //      if (resourceAmount < 0 && -resourceAmount > prevAmount)
  //      {
  //          resourceAmount = -prevAmount;
  //      }
        
  //      //resourceAmount = Mathf.CeilToInt(resourceAmount * city.world.resourceStorageMultiplierDict[type]);

  //      //adjusting resource amount to move based on how much space is available
  //      int newResourceAmount = resourceAmount;
  //      int newResourceBalance = resourceStorageLevel + newResourceAmount - city.warehouseStorageLimit;
  //      if (newResourceBalance >= 0 && city.warehouseStorageLimit > 0) //limit of 0 or less means infinite storage
  //      {
  //          newResourceAmount -= newResourceBalance;
  //      }

  //      //int resourceAmountAdjusted = newResourceAmount; /* Mathf.RoundToInt(newResourceAmount / city.world.resourceStorageMultiplierDict[type]);*/

  //      resourceDict[type] += newResourceAmount /*resourceAmountAdjusted*/; //updating the dictionary

  //      resourceStorageLevel += newResourceAmount;
  //      if (resourceStorageLevel >= city.warehouseStorageLimit)
  //          fullInventory = true;
  //      if (newResourceAmount < 0)
  //          CheckProducerUnloadWaitList();
        
  //      if (resourcesNeededForRoute.Contains(type))
  //          CheckTraderWaitList(type);
        
  //      if (resourcesNeededForProduction.Contains(type))
  //          CheckProducerResourceWaitList(type);

  //      int wasteCheck = resourceAmount - newResourceAmount; //int wasteCheck = Mathf.RoundToInt((resourceAmount - newResourceAmount) / city.world.resourceStorageMultiplierDict[type]);

  //      if (wasteCheck > 0)
  //      {
  //          Vector3 loc = city.cityLoc;
  //          loc.y += 2f; //limit of 5 resources at once wasted
  //          loc.y += -.4f * resourceCount;
  //          InfoResourcePopUpHandler.CreateResourceStat(loc, wasteCheck, ResourceHolder.Instance.GetIcon(type), true);
  //          Debug.Log($"Wasted {wasteCheck} of {type}");
  //          resourceCount++;
  //      }

  //      if (queuedResourcesToCheck.ContainsKey(type))
  //          CheckResourcesForQueue();
  //      UICheck(type, resourceAmount, prevAmount);

		//if (newResourceAmount > 0)
  //          city.CheckResourceWaiter(type);
  //      else if (newResourceAmount < 0)
  //          city.CheckLimitWaiter();

  //      if (city.autoGrow && city.cityPop.CurrentPop == 0 && city.HousingCount > 0 && !city.reachedWaterLimit && type == ResourceType.Food && resourceDict[type] >= city.growthFood && !pauseGrowth)
  //      {
  //          resourceDict[type] -= city.growthFood;
  //          city.PopulationGrowthCheck(false, 1);
  //      }

  //      return newResourceAmount /*resourceAmountAdjusted*/;
  //  }

	public int CalculateResourceGeneration(int resourceAmount, float labor, ResourceType type)
    {
        return Mathf.RoundToInt((city.workEthic + city.world.GetResourceTypeBonus(type)) * (resourceAmount * labor) * (1 + .1f * (labor - 1)));
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
        resourceStorageLevel -= resourceAmount /** city.world.resourceStorageMultiplierDict[resourceType]*/;
        CheckProducerUnloadWaitList();
        city.CheckLimitWaiter();
        VerifyResourceAmount(resourceType);
        //UpdateUI(resourceValue);
    }

    public void UpdateUI(List<ResourceValue> values) //updating the UI with the resource information in the dictionary
    {
        foreach (ResourceValue value in values)
        {
            if (value.resourceType == ResourceType.Gold)
                continue;

            //updating the resource manager ui only
			if (city.activeCity) //only update UI for currently selected city
			{
				city.world.cityBuilderManager.uiResourceManager.SetResource(value.resourceType, resourceDict[value.resourceType]);
				city.world.cityBuilderManager.uiResourceManager.SetCityCurrentStorage(resourceStorageLevel);

                if (city.uiCityResourceInfoPanel)
                {
				    city.uiCityResourceInfoPanel.UpdateResourceInteractable(value.resourceType, resourceDict[value.resourceType], false); //false so it doesn't play ps
				    city.uiCityResourceInfoPanel.UpdateStorageLevel(ResourceStorageLevel);
                }
			}
		}
    }

    public int GetResourceDictValue(ResourceType resourceType)
    {
        return resourceDict[resourceType];
    }

    public int SellResources()
    {
        int goldAdded = 0;
        int length = Mathf.Max(resourceSellList.Count,2);

        for (int i = 0; i < resourceSellList.Count; i++)
        {
            ResourceIndividualSO data = ResourceHolder.Instance.GetData(resourceSellList[i]);

			if (resourceDict[data.resourceType] - resourceMinHoldDict[data.resourceType] > 0)
			{
                int totalDemand = data.resourceQuantityPerPop * city.cityPop.CurrentPop;
                int demandDiff = resourceDict[data.resourceType] - totalDemand;
                int sellAmount;

                if (demandDiff < 0)
                    sellAmount = resourceDict[data.resourceType];
                else
                    sellAmount = totalDemand;

				goldAdded += resourcePriceDict[data.resourceType] * sellAmount;
				resourceSellHistoryDict[data.resourceType] += sellAmount;
				SubtractResource(data.resourceType, sellAmount);

                SetNewPrice(data.resourceType, demandDiff, totalDemand, data.resourcePrice);

				Vector3 cityLoc = city.cityLoc;
				cityLoc.y += length * 0.4f;
				cityLoc.y += -0.4f * i;
				if (city.activeCity)
					InfoResourcePopUpHandler.CreateResourceStat(cityLoc, -sellAmount, ResourceHolder.Instance.GetIcon(data.resourceType));
			}
		}

        if (goldAdded > 0)
        {
            Vector3 cityLoc = city.cityLoc;
            cityLoc.y += length * 0.4f + 0.4f;
            InfoResourcePopUpHandler.CreateResourceStat(cityLoc, goldAdded, ResourceHolder.Instance.GetIcon(ResourceType.Gold));
        }

        //SetPrices();

        return goldAdded;
    }

    //private void SetPrices()
    //{
    //    int currentPop = city.cityPop.CurrentPop;
    //    float populationFactor = 0.2f; //ratio of how much a new pop increases prices
    //    float cycleAttrition = 0.02f; //ratio of how many cycles to burn through resourceQuantityePerPop
    //    float abundanceRatio = 0.5f; //how many purchases of resources by current pop to reduce the price in half. 

    //    foreach (ResourceIndividualSO resourceData in ResourceHolder.Instance.allStorableResources)
    //    {
    //        ResourceType resourceType = resourceData.resourceType;
    //        int resourceQuantityPerPop = resourceData.resourceQuantityPerPop;

    //        if (currentPop > 0)
    //        {
    //            float priceByPop = (1 + (currentPop-1) * populationFactor);
    //            float abundanceWithAttrition = resourceQuantityPerPop * cycleAttrition * cycleCount * currentPop;
    //            float abundanceFactor = 1 - ((resourceSellHistoryDict[resourceType] - abundanceWithAttrition) / currentPop) * 1 / resourceQuantityPerPop * abundanceRatio;
    //            resourcePriceDict[resourceType] = Mathf.Max((int)Math.Round(resourceData.resourcePrice * priceByPop * abundanceFactor, MidpointRounding.AwayFromZero),1);
    //            //resourcePriceDict[resourceType] = Mathf.Max(Mathf.FloorToInt(resourceData.resourcePrice * priceByPop * abundanceFactor), 1);
    //        }
    //        else //no price if no one there to purchase
    //        {
    //            resourcePriceDict[resourceType] = 0;
    //        }
    //    }
    //}

    private void SetNewPrice(ResourceType type, int demandDiff, int originalDemand, int originalPrice)
    {
        float demandRatio = (float)demandDiff / originalDemand;
        
        if (demandDiff < 0)
        {
            if (demandRatio <= -0.5f)
            {
                resourcePriceDict[type] = originalPrice;
            }
            else if (demandRatio > -0.1f)
            {
				resourcePriceDict[type] *= Mathf.CeilToInt(resourcePriceDict[type] * 1.2f);
			}
            else if (demandRatio > -0.5f)
            {
                resourcePriceDict[type] *= Mathf.CeilToInt(resourcePriceDict[type] * 1.1f); 
            }
        }
        else
        {
            if (demandRatio < 3f)
            {
                resourcePriceDict[type] = originalPrice;
            }
            else if (demandRatio > 10)
            {
                int newPrice = Mathf.FloorToInt(resourcePriceDict[type] * .8f);
				resourcePriceDict[type] = Mathf.Clamp(resourcePriceDict[type], 1, newPrice);
			}
            else if (demandRatio > 5)
            {
				int newPrice = Mathf.FloorToInt(resourcePriceDict[type] * .9f);
				resourcePriceDict[type] = Mathf.Clamp(resourcePriceDict[type], 1, newPrice);
			}
        }
    }

    //checking if enough food to not starve
    public void CheckForPopGrowth()
    {
        //ResourceValue foodConsumed;
        //foodConsumed.resourceType = ResourceType.Food;
        //foodConsumed.resourceAmount = city.cityPop.CurrentPop * city.unitFoodConsumptionPerMinute;

        if (resourceDict[ResourceType.Food] >= resourceConsumedPerMinuteDict[ResourceType.Food])
        {
            if (starvationCount > 0)
            {
                starvationCount = 0;
                growthDeclineDanger = false;
                city.exclamationPoint.SetActive(false);

                if (city.activeCity)
                    city.world.cityBuilderManager.uiInfoPanelCity.TogglewWarning(false);
			}

            if (city.autoGrow && resourceDict[ResourceType.Food] >= city.growthFood + resourceConsumedPerMinuteDict[ResourceType.Food] && city.HousingCount > 0 && !pauseGrowth && !city.reachedWaterLimit) //if enough food left over to grow
                city.PopulationGrowthCheck(false, 1);
        }
        else
        {
            //foodConsumed.resourceAmount = resourceDict[ResourceType.Food];
            starvationCount++;

            growthDeclineDanger = true;
            city.exclamationPoint.SetActive(true);

            if (city.activeCity)
                city.world.cityBuilderManager.uiInfoPanelCity.TogglewWarning(true);

            if (starvationCount >= cyclesToWait) //decreasing if starving for 2 cycles
            {
                if (city.army.UnitsInArmy.Count > 0)
                    city.PlayHellHighlight(city.army.RemoveRandomArmyUnit());
                else
                    city.PopulationDeclineCheck(true, false);
    
                starvationCount = 0;
                noHousingCount = 0;
                noWaterCount = 0;
                growthDeclineDanger = false;
                city.exclamationPoint.SetActive(false);

                if (city.activeCity)
                    city.world.cityBuilderManager.uiInfoPanelCity.TogglewWarning(false);
            }
        }

        //ConsumeResources(new List<ResourceValue> { foodConsumed }, 1, city.cityLoc);


        if (city.HousingCount < 0)
        {
            noHousingCount++;

            growthDeclineDanger = true;
            city.exclamationPoint.SetActive(true);

			if (city.activeCity)
				city.world.cityBuilderManager.uiInfoPanelCity.TogglewWarning(true);

			if (noHousingCount >= cyclesToWait)
            {
                city.PopulationDeclineCheck(false, false);
                starvationCount = 0;
                noHousingCount = 0;
                noWaterCount = 0;
                growthDeclineDanger = false;
                city.exclamationPoint.SetActive(false);

                if (city.activeCity)
                    city.world.cityBuilderManager.uiInfoPanelCity.TogglewWarning(false);
			}

        }
        else
        {
            if (noHousingCount > 0)
            {
                noHousingCount = 0;
                growthDeclineDanger = false;
                city.exclamationPoint.SetActive(false);

                if (city.activeCity)
                    city.world.cityBuilderManager.uiInfoPanelCity.TogglewWarning(false);
			}
        }

        if (city.waterCount < 0)
        {
            noWaterCount++;

            growthDeclineDanger = true;
            city.exclamationPoint.SetActive(true);

			if (city.activeCity)
				city.world.cityBuilderManager.uiInfoPanelCity.TogglewWarning(true);

			if (noWaterCount >= cyclesToWait)
            {
                city.PopulationDeclineCheck(false, false);
                starvationCount = 0;
                noHousingCount = 0;
                noWaterCount = 0;
                growthDeclineDanger = false;
                city.exclamationPoint.SetActive(false);

				if (city.activeCity)
					city.world.cityBuilderManager.uiInfoPanelCity.TogglewWarning(false);
			}
        }
        else
        {
            if (noWaterCount > 0)
            {
                noHousingCount = 0;
                growthDeclineDanger = false;
                city.exclamationPoint.SetActive(false);

                if (city.activeCity)
                    city.world.cityBuilderManager.uiInfoPanelCity.TogglewWarning(false);
			}
        }

        //CheckProducerUnloadWaitList();
        //city.CheckLimitWaiter();
        //UpdateUI(ResourceType.Food);
    }

    public void CheckProducerUnloadWaitList()
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

    public void RestartStorageRoomWaitProduction()
    {
        List<ResourceProducer> tempProducers = new(waitingForStorageRoomProducerList);

        foreach (ResourceProducer producer in tempProducers)
        {
            producer.isWaitingForStorageRoom = false;
            producer.cityImprovement.exclamationPoint.SetActive(false);
			producer.StartProducing();
            waitingForStorageRoomProducerList.Remove(producer);
        }
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

    public void CheckTraderWaitList(ResourceType resourceType)
    {
        List<Trader> tempWaitingForTrader = new(waitingForTraderList);

        for (int i = 0; i < tempWaitingForTrader.Count; i++)
        {
            if (tempWaitingForTrader[i].routeCostTypes.Contains(resourceType))
                tempWaitingForTrader[i].RestartRoute(city.cityLoc);
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

    public void RemoveFromResearchWaitlist(ResourceProducer resourceProducer)
    {
        city.world.RemoveFromResearchWaitList(resourceProducer);
    }

    public void AddToStorageRoomWaitList(ResourceProducer resourceProducer)
    {
        if (!waitingForStorageRoomProducerList.Contains(resourceProducer))
            waitingForStorageRoomProducerList.Add(resourceProducer);
    }

    public void RemoveFromStorageRoomWaitList(ResourceProducer resourceProducer)
    {
        if (waitingForStorageRoomProducerList.Contains(resourceProducer))
            waitingForStorageRoomProducerList.Remove(resourceProducer);
    }

    public void AddToResourceWaitList(ResourceProducer resourceProducer)
    {
        if (!waitingforResourceProducerList.Contains(resourceProducer))
            waitingforResourceProducerList.Add(resourceProducer);
    }

    public void RemoveFromResourceWaitList(ResourceProducer resourceProducer)
    {
        if (waitingforResourceProducerList.Contains(resourceProducer))
            waitingforResourceProducerList.Remove(resourceProducer);
    }

    public void AddToResourcesNeededForProduction(List<ResourceType> consumedResources)
    {
        for (int i = 0; i < consumedResources.Count; i++)
        {
            if (!resourcesNeededForProduction.Contains(consumedResources[i]))
                resourcesNeededForProduction.Add(consumedResources[i]);
		}
    }

    public void RemoveFromResourcesNeededForProduction(List<ResourceType> consumedResources)
    {
        for (int i = 0; i < consumedResources.Count; i++)
        {
            if (resourcesNeededForProduction.Contains(consumedResources[i]))
                resourcesNeededForProduction.Remove(consumedResources[i]);
		}
    }

    public void AddToTraderWaitList(Trader trader)
    {
        if (!waitingForTraderList.Contains(trader))
            waitingForTraderList.Add(trader);
    }

    public void RemoveFromTraderWaitList(Trader trader)
    {
        if (waitingForTraderList.Contains(trader))
            waitingForTraderList.Remove(trader);
    }

	public void AddToResourcesNeededForTrader(List<ResourceType> consumedResources)
	{
        for (int i = 0; i < consumedResources.Count; i++)
        {
			if (!resourcesNeededForRoute.Contains(consumedResources[i]))
				resourcesNeededForRoute.Add(consumedResources[i]);
		}
	}

	public void RemoveFromResourcesNeededForTrader(List<ResourceType> consumedResources)
	{
        for (int i = 0; i < consumedResources.Count; i++)
        {
			if (resourcesNeededForRoute.Contains(consumedResources[i]))
                resourcesNeededForRoute.Remove(consumedResources[i]);
		}
	}

	//for queued build orders in cities
	public void SetQueueResources(List<ResourceValue> resourceList)
    {
        //queuedResourcesToCheck = resourceList;
        //this.cityBuilderManager = cityBuilderManager;
        queuedResourcesToCheck.Clear();
        for (int i = 0; i < resourceList.Count; i++)
            queuedResourcesToCheck[resourceList[i].resourceType] = resourceList[i].resourceAmount;

        CheckResourcesForQueue();
    }

    public void ClearQueueResources()
    {
        queuedResourcesToCheck.Clear();
    }

    private void CheckResourcesForQueue()
    {
        if (queuedResourcesToCheck.Keys.Count > 0)
        {
            foreach (ResourceType type in queuedResourcesToCheck.Keys)
            {
                if (!CheckResourceTypeAvailability(type, queuedResourcesToCheck[type]))
                {
                    return;
                }
            }

            ClearQueueResources();
            city.world.cityBuilderManager.BuildQueuedBuilding(city, this);
        }
    }
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
    Diamond,
    Jewelry,
    Time
}

public enum RawResourceType
{
    None,
    FoodLand,
    FoodSea,
    Rocks,
    Lumber,
    Clay,
    Silk,
    Wool,
    Cotton
}

public enum ResourceCategory
{
    Raw,
    Rock,
    BuildingBlock,
    SoldGood,
    LuxuryGood,
    None
}

public enum RocksType
{
    None,
    Normal,
    Luxury,
    Chemical
}