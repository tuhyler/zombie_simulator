using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;

public class ResourceManager : MonoBehaviour
{
    public Dictionary<ResourceType, int> resourceDict = new(), resourcePriceDict = new(), resourceMinHoldDict = new(), resourceMaxHoldDict = new(), resourceSellHistoryDict = new();
    private Dictionary<ResourceType, float> resourceGenerationPerMinuteDict = new(); //for resource generation stats
    public Dictionary<ResourceType, float> resourceConsumedPerMinuteDict = new(); //for resource consumption stats
    [HideInInspector]
    public List<ResourceType> resourceSellList = new();

    [HideInInspector]
    public int resourceStorageLevel;

    //waiting for gold, resources, or storage lists
    [HideInInspector]
    public List<ICityGoldWait> cityGoldWaitList = new();
    [HideInInspector]
    public Dictionary<ResourceType, List<ICityResourceWait>> cityResourceWaitDict = new();
    [HideInInspector]
    public Dictionary<ResourceType, List<ResourceProducer>> cityResourceMaxWaitDict = new();
	[HideInInspector]
    public List<ICityResourceWait> unloadWaitList = new();

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
    private int cycleCount;
    public int CycleCount { get { return cycleCount; } set { cycleCount = value; } }

    //for queued build orders
    public Dictionary<ResourceType, int> queuedResourcesToCheck = new();
    [HideInInspector]
    public int resourceCount; //for counting wasted resources


    public void PrepareResourceDictionary()
    {
        resourceSellList.Clear();
        
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
                resourceMaxHoldDict[type] = -1;
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
        resourceMaxHoldDict[type] = -1;
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

    public void ModifyResourceGenerationPerMinute(ResourceType resourceType, float generationDiff)
    {
        resourceGenerationPerMinuteDict[resourceType] += generationDiff;
    }

    public void ModifyResourceConsumptionPerMinute(ResourceType resourceType, float change)
    {
        resourceConsumedPerMinuteDict[resourceType] += change;
    }

    public float GetResourceGenerationValues(ResourceType resourceType)
    {
        return resourceGenerationPerMinuteDict[resourceType];
    }

    public bool CheckResourceAvailability(ResourceType type, int amount)
    {
        return resourceDict[type] >= amount;
    }

    public bool CheckResourceAvailability(ResourceValue resourceRequired) //this will be used by building system to see if we have enough resources
    {
        return resourceDict[resourceRequired.resourceType] >= resourceRequired.resourceAmount;
    }

    public bool CheckResourceTypeAvailability(ResourceType type, int amount)
    {
		return resourceDict[type] >= amount;
	} 

    //for city pop, traders, and army
    public void ConsumeMaintenanceResources(List<ResourceValue> consumedResource, Vector3 location, bool military = false, bool showPopUp = false)
    {
		int i = 0;
		location.y += consumedResource.Count * 0.4f;

		foreach (ResourceValue value in consumedResource)
		{
			int consumedAmount = value.resourceAmount;
			if (consumedAmount == 0)
				continue;

            if (military && value.resourceType == ResourceType.Gold)
            {
                int goldAmount = city.world.GetWorldGoldLevel();
                if (goldAmount < value.resourceAmount)
                {
                    consumedAmount = goldAmount;
					city.army.AWOLCheck();
                }
                else
                {
					city.army.AWOLClear();
                }
			}

			consumedAmount = SubtractResource(value.resourceType, consumedAmount);

            if (consumedAmount != 0 && (showPopUp || city.activeCity))
            {
				Vector3 loc = location;
				loc.y -= 0.4f * i;
				InfoResourcePopUpHandler.CreateResourceStat(loc, -consumedAmount, ResourceHolder.Instance.GetIcon(value.resourceType), city.world);
				i++;
			}
		}
	}

    //for resource producers
	public void ConsumeResources(List<ResourceValue> consumedResource, float currentLabor, Vector3 location)
    {
        int i = 0;
        location.y += consumedResource.Count * 0.4f;

        foreach (ResourceValue value in consumedResource)
        {
            int consumedAmount = Mathf.RoundToInt(value.resourceAmount * currentLabor);
            if (consumedAmount == 0)
                continue;

            consumedAmount = SubtractResource(value.resourceType, consumedAmount);

            if (city.activeCity && consumedAmount != 0)
            {
                Vector3 loc = location;
                loc.y -= 0.4f * i;
                InfoResourcePopUpHandler.CreateResourceStat(loc, -consumedAmount, ResourceHolder.Instance.GetIcon(value.resourceType), city.world);
                i++;
            }
		}
	}

    public bool ResourceWaitCheck(List<ResourceValue> consumeResources, int labor, ResourceType type)
    {
        for (int i = 0; i < consumeResources.Count; i++)
        {
            if (consumeResources[i].resourceType == type)
                return resourceDict[consumeResources[i].resourceType] >= consumeResources[i].resourceAmount * labor;
		}

        return true;
    }

    public bool ConsumeResourcesCheck(List<ResourceValue> consumeResources, int labor, ResourceProducer producer)
    {
        foreach (ResourceValue value in consumeResources)
        {
            if (value.resourceType == ResourceType.Gold)
            {
                if (!city.CheckWorldGold(value.resourceAmount * labor))
                {
                    producer.goldNeeded = value.resourceAmount * labor;
                    cityGoldWaitList.Add(producer);
                    city.world.AddToGoldWaitList(city);
                    return false;
                }
            }
            else if (resourceDict[value.resourceType] < value.resourceAmount * labor)
            {
                AddToResourceWaitList(value.resourceType, producer);
                return false;
            }
        }

        return true;
    }

    public void AddToUnloadWaitList(ICityResourceWait unloadWaiter)
    {
        if (!unloadWaitList.Contains(unloadWaiter))
            unloadWaitList.Add(unloadWaiter);
    }

    public void AddToResourceWaitList(ResourceType type, ICityResourceWait resourceWaiter)
    {
        if (!cityResourceWaitDict.ContainsKey(type))
            cityResourceWaitDict[type] = new();

        if (!cityResourceWaitDict[type].Contains(resourceWaiter))
            cityResourceWaitDict[type].Add(resourceWaiter);
    }

    public void RemoveFromResourceWaitList(ResourceType type)
    {
        cityResourceWaitDict[type].RemoveAt(0);

        if (cityResourceWaitDict[type].Count == 0)
            cityResourceWaitDict.Remove(type);
    }

    public void AddToResourceMaxWaitList(ResourceType type, ResourceProducer producer)
    {
		if (!cityResourceMaxWaitDict.ContainsKey(type))
			cityResourceMaxWaitDict[type] = new();

		if (!cityResourceMaxWaitDict[type].Contains(producer))
			cityResourceMaxWaitDict[type].Add(producer);
	}

	public void RemoveFromResourceMaxWaitList(ResourceType type)
	{
		cityResourceMaxWaitDict[type].RemoveAt(0);

		if (cityResourceMaxWaitDict[type].Count == 0)
			cityResourceMaxWaitDict.Remove(type);
	}

	public bool RouteCostCheck(List<ResourceValue> costs, ResourceType type)
    {
        for (int i = 0; i < costs.Count; i++)
        {
            if (costs[i].resourceType == type)
                return costs[i].resourceType == type && resourceDict[costs[i].resourceType] >= costs[i].resourceAmount;
        }

        return true;
    }

    public bool ConsumeResourcesForRouteCheck(List<ResourceValue> consumeResources, Trader trader)
    {
        for (int i = 0; i < consumeResources.Count; i++)
        {
            if (consumeResources[i].resourceType == ResourceType.Gold)
            {
                if (!city.CheckWorldGold(consumeResources[i].resourceAmount))
                {
                    trader.goldNeeded = consumeResources[i].resourceAmount;
                    if (!cityGoldWaitList.Contains(trader))
                    {
                        cityGoldWaitList.Add(trader);
                        city.world.AddToGoldWaitList(city);
                    }
                    return false;
                }
            }
            else if (resourceDict[consumeResources[i].resourceType] < consumeResources[i].resourceAmount)
            {
                AddToResourceWaitList(consumeResources[i].resourceType, trader);
                return false;
            }
        }

        return true;
    }

    public bool PrepareConsumedResource(List<ResourceValue> producedResource, float currentLabor, Vector3 producerLoc)
    {
        bool destroy = false;
        
        int i = 0;
        resourceCount = 0;
        
        foreach (ResourceValue resourceVal in producedResource)
        {
            int newResourceAmount;

            newResourceAmount = Mathf.RoundToInt(resourceVal.resourceAmount * currentLabor);
            int resourceAmount = AddResource(resourceVal.resourceType, newResourceAmount);

            Vector3 loc = producerLoc;
            loc.y += 0.4f * i;

            if (resourceAmount != 0)
                InfoResourcePopUpHandler.CreateResourceStat(loc, resourceAmount, ResourceHolder.Instance.GetIcon(resourceVal.resourceType), city.world);
            i++;
        }

        return destroy;
    }

	public bool PrepareResource(int newResourceAmount, ResourceValue producedResource, Vector3 producerLoc, CityImprovement improvement)
	{
		bool destroy = false;

		resourceCount = 0;

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
			InfoResourcePopUpHandler.CreateResourceStat(loc, resourceAmount, ResourceHolder.Instance.GetIcon(producedResource.resourceType), city.world);

		return destroy;
	}

    public int CalculateResourceProductionAmount(ResourceValue producedResource, float currentLabor, CityImprovement improvement)
    {
		int newResourceAmount = CalculateResourceGeneration(producedResource.resourceAmount, currentLabor, producedResource.resourceType);

		if (improvement.GetImprovementData.rawMaterials && improvement.td.resourceAmount > 0 && improvement.td.resourceAmount < newResourceAmount)
			newResourceAmount = improvement.td.resourceAmount;

        return newResourceAmount;
	}

    public int SubtractResource(ResourceType type, int amount)
    {
		if (type == ResourceType.Gold)
		{
			city.world.UpdateWorldGold(-amount);
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

		if (type == ResourceType.Research)
		{
            city.world.UpdateWorldResearch(amount);
            return amount;
		}
        else if (type == ResourceType.Gold)
        {
			city.world.UpdateWorldGold(amount);
			return amount;
        }

		amount = AddResourceCheck(type, amount);
		if (amount > 0)
        {
            if (!city.resourceGridDict.ContainsKey(type))
				city.AddToGrid(type);

			AddResourceToStorage(type, amount);
        }

        return amount;
	}

    //slightly faster
    public int AddTraderResource(ResourceType type, int amount)
    {
        amount = AddResourceTraderCheck(type, amount);
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
			InfoResourcePopUpHandler.CreateResourceStat(loc, amount - diff, ResourceHolder.Instance.GetIcon(type), city.world, true);
			resourceCount++;

            amount = diff;
		}

		return amount;
	}

	private int AddResourceTraderCheck(ResourceType type, int amount)
	{
		int diff = city.warehouseStorageLimit - resourceStorageLevel;

		if (diff < amount)
			amount = diff;

		return amount;
	}

	private void AddResourceToStorage(ResourceType type, int amount)
    {
		int prevAmount = resourceDict[type];
        resourceDict[type] += amount;
		resourceStorageLevel += amount;

        ResourceWaitListCheck(type);
		UICheck(type, amount, prevAmount);
	}

    public void ResourceWaitListCheck(ResourceType type)
    {
		if (cityResourceWaitDict.ContainsKey(type))
		{
            bool success = true;
            while (success)
            {
                if (cityResourceWaitDict.ContainsKey(type) && cityResourceWaitDict[type][0].RestartCheck(type))
                {
                    ICityResourceWait waiter = cityResourceWaitDict[type][0];
                    RemoveFromResourceWaitList(type);
                    waiter.Restart(type);
                }
                else
                {
                    success = false;
                }
            }
		}
	}

    public void ResourceMaxWaitListCheck(ResourceType type)
    {
        if (cityResourceMaxWaitDict.ContainsKey(type))
        {
            bool success = true;
			while (success)
			{
				if (cityResourceMaxWaitDict.ContainsKey(type) && cityResourceMaxWaitDict[type][0].RestartCheck(type))
                {
					ResourceProducer waiter = cityResourceMaxWaitDict[type][0];
					RemoveFromResourceMaxWaitList(type);
                    waiter.Restart(ResourceType.None); //None since all it's doing is unloading finished product
                }
                else
                {
					success = false;
                }
			}
		}
    }

    public void StorageSpaceCheck()
    {
        if (unloadWaitList.Count > 0)
        {
            bool success = true;
            while (success)
            {
                if (unloadWaitList.Count > 0 && unloadWaitList[0].RestartCheck(ResourceType.None))
                {
					ICityResourceWait waiter = unloadWaitList[0];
					unloadWaitList.RemoveAt(0);
                    waiter.Restart(ResourceType.None);
                }
                else
                {
                    success = false;
                }
            }
        }
    }

    private void SubtractResourceFromStorage(ResourceType type, int amount)
    {
		int prevAmount = resourceDict[type];
        resourceDict[type] -= amount;
		resourceStorageLevel -= amount;

        //if (fullInventory)
        //    fullInventory = false;

        //check lists needing inventory room, in this order
        StorageSpaceCheck();
        ResourceMaxWaitListCheck(type);
		//city.CheckLimitWaiter();
        //CheckProducerUnloadWaitList();

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
                city.world.cityBuilderManager.uiCityUpgradePanel.ResourceCheck(resourceDict[type], type);

            if (city.world.cityBuilderManager.buildOptionsActive)
    			city.CheckBuildOptionsResource(type, prevAmount, resourceDict[type], amount > 0);

            if (city.uiCityResourceInfoPanel)
            {
				city.uiCityResourceInfoPanel.UpdateResourceInteractable(type, resourceDict[type], false); //false so it doesn't play ps
				city.uiCityResourceInfoPanel.UpdateStorageLevel(resourceStorageLevel);
			}

            if (city.world.uiCityPopIncreasePanel.CheckCity(city) && type == ResourceType.Food)
				city.world.uiCityPopIncreasePanel.UpdateFoodCosts(city);
		}
        
        if (city.world.iTooltip != null)
            city.world.iTooltip.CheckResource(city, resourceDict[type], type);        
	}

	public int CalculateResourceGeneration(int resourceAmount, float labor, ResourceType type)
    {
        return Mathf.RoundToInt((city.workEthic + city.world.GetResourceTypeBonus(type)) * (resourceAmount * labor) * (1 + .1f * (labor - 1)));
    }

    public void SpendResource(List<ResourceValue> buildCost, Vector3 loc, bool refund = false, List<ResourceValue> refundCost = null)
    {
        loc.y += buildCost.Count * 0.4f;

        for (int i = 0; i < buildCost.Count; i++)
        {
            Vector3 newLoc = loc;
            
            if (buildCost[i].resourceType == ResourceType.Gold)
            {
                city.world.UpdateWorldGold(-buildCost[i].resourceAmount);
            }
            else
            {
                int prevAmount = resourceDict[buildCost[i].resourceType];
				resourceDict[buildCost[i].resourceType] -= buildCost[i].resourceAmount;
                resourceStorageLevel -= buildCost[i].resourceAmount;
                UICheck(buildCost[i].resourceType, buildCost[i].resourceAmount, prevAmount);
            }
    
            newLoc.y += -.4f * i;
            InfoResourcePopUpHandler.CreateResourceStat(newLoc, -buildCost[i].resourceAmount, ResourceHolder.Instance.GetIcon(buildCost[i].resourceType), city.world);
        }

        if (refund)
            IssueRefund(refundCost, loc, buildCost.Count);

        StorageSpaceCheck();

        //those in line for storage space get precedence
        for (int i = 0; i < buildCost.Count; i++)
        {
            if (resourceMaxHoldDict.ContainsKey(buildCost[i].resourceType))
                ResourceMaxWaitListCheck(buildCost[i].resourceType);
        }
    }

    public void IssueRefund(List<ResourceValue> refundCost, Vector3 loc, int i = 0)
    {
		foreach (ResourceValue value in refundCost)
		{
			Vector3 newLoc = loc;

			if (value.resourceAmount > 0)
			{
				AddResource(value.resourceType, value.resourceAmount);
				newLoc.y += -.4f * i;
				InfoResourcePopUpHandler.CreateResourceStat(newLoc, value.resourceAmount, ResourceHolder.Instance.GetIcon(value.resourceType), city.world);
				i++;
			}
		}
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
				    city.uiCityResourceInfoPanel.UpdateStorageLevel(resourceStorageLevel);
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
                int totalDemand = Mathf.RoundToInt(data.resourceQuantityPerPop * city.currentPop * city.purchaseAmountMultiple);
                int demandDiff = resourceDict[data.resourceType] - totalDemand;
                int currentPrice = resourcePriceDict[data.resourceType];
				int sellAmount;

                if (demandDiff < 0)
                    sellAmount = resourceDict[data.resourceType];
                else
                    sellAmount = totalDemand;

				goldAdded += currentPrice * sellAmount;
				resourceSellHistoryDict[data.resourceType] += sellAmount;
				SubtractResource(data.resourceType, sellAmount);

                SetNewPrice(data.resourceType, demandDiff, totalDemand, data.resourcePrice, currentPrice);

				Vector3 cityLoc = city.cityLoc;
				cityLoc.y += length * 0.4f;
				cityLoc.y += -0.4f * i;
				if (city.activeCity && sellAmount != 0)
					InfoResourcePopUpHandler.CreateResourceStat(cityLoc, -sellAmount, ResourceHolder.Instance.GetIcon(data.resourceType), city.world);
			}
		}

        if (goldAdded > 0)
        {
            Vector3 cityLoc = city.cityLoc;
            cityLoc.y += length * 0.4f + 0.4f;
            InfoResourcePopUpHandler.CreateResourceStat(cityLoc, goldAdded, ResourceHolder.Instance.GetIcon(ResourceType.Gold), city.world);
        }

        return goldAdded;
    }

    private void SetNewPrice(ResourceType type, int demandDiff, int originalDemand, int originalPrice, int currentPrice)
    {
        float demandRatio = (float)demandDiff / originalDemand;
        
        if (demandDiff < 0)
        {
            if (demandRatio <= -0.5f)
            {
                resourcePriceDict[type] = currentPrice;
            }
            else
            {
                resourcePriceDict[type] = Mathf.Min(currentPrice++, originalPrice * 3);
            }
            
   //         if (demandRatio <= -0.5f)
   //         {
   //             resourcePriceDict[type] = originalPrice;
   //         }
   //         else if (demandRatio > -0.1f)
   //         {
			//	resourcePriceDict[type] *= Mathf.CeilToInt(resourcePriceDict[type] * 1.2f);
			//}
   //         else if (demandRatio > -0.5f)
   //         {
   //             resourcePriceDict[type] *= Mathf.CeilToInt(resourcePriceDict[type] * 1.1f); 
   //         }
        }
        else
        {
            if (demandRatio < 2f)
            {
                resourcePriceDict[type] = currentPrice;
            }
            else
            {
                resourcePriceDict[type] = Mathf.Max(1, currentPrice--);
            }
            
   //         if (demandRatio < 3f)
   //         {
   //             resourcePriceDict[type] = originalPrice;
   //         }
   //         else if (demandRatio > 10)
   //         {
   //             int newPrice = Mathf.FloorToInt(resourcePriceDict[type] * .8f);
			//	resourcePriceDict[type] = Mathf.Clamp(resourcePriceDict[type], 1, newPrice);
			//}
   //         else if (demandRatio > 5)
   //         {
			//	int newPrice = Mathf.FloorToInt(resourcePriceDict[type] * .9f);
			//	resourcePriceDict[type] = Mathf.Clamp(resourcePriceDict[type], 1, newPrice);
			//}
        }
    }

    //checking if enough food to not starve
    public void CheckForPopGrowth()
    {
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

            //if (city.autoGrow && resourceDict[ResourceType.Food] >= city.growthFood + resourceConsumedPerMinuteDict[ResourceType.Food] && city.HousingCount > 0 && !pauseGrowth && !city.reachedWaterLimit) //if enough food left over to grow
            //    city.PopulationGrowthCheck(false, 1);
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
                //remove priority is as follows: TradeDepot, Airport, Harbor, Barracks, Airbase, Shipyard, regular pop.
                if (city.singleBuildDict.ContainsKey(SingleBuildType.TradeDepot) && city.world.GetCityDevelopment(city.singleBuildDict[SingleBuildType.TradeDepot]).unitsWithinCount > 0)
                {
                    city.PlayHellHighlight(city.world.GetCityDevelopment(city.singleBuildDict[SingleBuildType.TradeDepot]).RemoveRandomTrader());
                }
				else if (city.singleBuildDict.ContainsKey(SingleBuildType.Airport) && city.world.GetCityDevelopment(city.singleBuildDict[SingleBuildType.Airport]).unitsWithinCount > 0)
                {
					city.PlayHellHighlight(city.world.GetCityDevelopment(city.singleBuildDict[SingleBuildType.Airport]).RemoveRandomTrader());
                }
				else if (city.singleBuildDict.ContainsKey(SingleBuildType.Harbor) && city.world.GetCityDevelopment(city.singleBuildDict[SingleBuildType.Harbor]).unitsWithinCount > 0)
                {
					city.PlayHellHighlight(city.world.GetCityDevelopment(city.singleBuildDict[SingleBuildType.Harbor]).RemoveRandomTrader());
                }
				else if (city.singleBuildDict.ContainsKey(SingleBuildType.Barracks) && city.army.UnitsInArmy.Count > 0 && !city.army.defending && city.army.atHome)
                {
                    city.PlayHellHighlight(city.army.RemoveRandomArmyUnit());
                    city.army.AWOLClear();
                }
                else
                {
                    city.PopulationDeclineCheck(true, false);
                }
				
				city.world.popLost++;

				if (!city.activeCity)
					city.lostPop++;

				if (city.world.tutorial && city.world.popLost == 1)
					city.world.mainPlayer.conversationHaver.SetSomethingToSay("first_pop_loss", city.world.scott);

				starvationCount = 0;
                noHousingCount = 0;
                noWaterCount = 0;
                growthDeclineDanger = false;
                city.exclamationPoint.SetActive(false);

                if (city.activeCity)
                    city.world.cityBuilderManager.uiInfoPanelCity.TogglewWarning(false);
            }
        }

        if (city.HousingCount < 0)
        {
            noHousingCount++;

			if (noHousingCount >= cyclesToWait)
            {
                city.PopulationDeclineCheck(false, false);
				city.world.popLost++;

                if (!city.activeCity)
                    city.lostPop++;

				if (city.world.tutorial && city.world.popLost == 1)
					city.world.mainPlayer.conversationHaver.SetSomethingToSay("first_pop_loss", city.world.scott);

				starvationCount = 0;
                noHousingCount = 0;
                noWaterCount = 0;
                growthDeclineDanger = false;
                city.exclamationPoint.SetActive(false);

                if (city.activeCity)
                    city.world.cityBuilderManager.uiInfoPanelCity.TogglewWarning(false);
			}
            else
            {
                growthDeclineDanger = true;
                city.exclamationPoint.SetActive(true);

			    if (city.activeCity)
				    city.world.cityBuilderManager.uiInfoPanelCity.TogglewWarning(true);
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

			if (noWaterCount >= cyclesToWait)
            {
                city.PopulationDeclineCheck(false, false);
				city.world.popLost++;

				if (!city.activeCity)
					city.lostPop++;

				if (city.world.tutorial && city.world.popLost == 1)
					city.world.mainPlayer.conversationHaver.SetSomethingToSay("first_pop_loss", city.world.scott);

				starvationCount = 0;
                noHousingCount = 0;
                noWaterCount = 0;
                growthDeclineDanger = false;
                city.exclamationPoint.SetActive(false);

				if (city.activeCity)
					city.world.cityBuilderManager.uiInfoPanelCity.TogglewWarning(false);
            }
            else
            {
                growthDeclineDanger = true;
                city.exclamationPoint.SetActive(true);

			    if (city.activeCity)
				    city.world.cityBuilderManager.uiInfoPanelCity.TogglewWarning(true);
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
    }

    public void RemoveWarning()
    {
        if (growthDeclineDanger)
        {
            growthDeclineDanger = false;
            city.exclamationPoint.SetActive(false);
        }
    }

    public int RemoveFromGoldWaitList(ICityGoldWait cityGoldWait)
    {
        int num = cityGoldWaitList.IndexOf(cityGoldWait);
        cityGoldWaitList.RemoveAt(num);

        return num;
    }

    public void RemoveFromCityResourceWaitList(ICityResourceWait resourceWait, List<ResourceValue> resources)
    {
        for (int i = 0; i < resources.Count; i++)
        {
            if (cityResourceWaitDict.ContainsKey(resources[i].resourceType))
            {
                int index = cityResourceWaitDict[resources[i].resourceType].IndexOf(resourceWait);

                if (index >= 0)
                {
    				cityResourceWaitDict[resources[i].resourceType].RemoveAt(index);

                    if (cityResourceWaitDict[resources[i].resourceType].Count == 0)
                        cityResourceWaitDict.Remove(resources[i].resourceType);
                    else if (index == 0) //if first in line, checking to see if next one up can go
						ResourceWaitListCheck(resources[i].resourceType);
                }
            }
        }
    }

    public void RemoveFromCityResourceMaxWaitList(ResourceProducer producer, ResourceType type)
    {
		if (cityResourceMaxWaitDict.ContainsKey(type))
		{
			int index = cityResourceMaxWaitDict[type].IndexOf(producer);

			if (index >= 0)
			{
				cityResourceMaxWaitDict[type].RemoveAt(index);

				if (cityResourceMaxWaitDict[type].Count == 0)
					cityResourceMaxWaitDict.Remove(type);
				else if (index == 0) //if first in line, checking to see if next one up can go
					ResourceMaxWaitListCheck(type);
			}
		}
	}

    public void RemoveFromUnloadWaitList(ICityResourceWait unloadWaiter)
    {
        int index = unloadWaitList.IndexOf(unloadWaiter);

        if (index >= 0)
        {
            unloadWaitList.RemoveAt(index);

            if (index == 0)
                StorageSpaceCheck();
        }
    }

    public void RemoveFromResearchWaitlist(ResourceProducer resourceProducer)
    {
        city.world.RemoveFromResearchWaitList(resourceProducer);
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
    Coal,
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
    Time,
    Bricks,
    Incense,
    Spices
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

public interface ICityGoldWait
{
    Vector3Int WaitLoc { get; }
    int waitId { get; }
    bool RestartGold(int gold);
}

public interface ICityResourceWait
{
    Vector3Int WaitLoc { get; }
    int waitId { get; }
    void Restart(ResourceType type);
    bool RestartCheck(ResourceType type);
}