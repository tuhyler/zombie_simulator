using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ResourceProducer : MonoBehaviour, ICityGoldWait, ICityResourceWait
{
    private ResourceManager resourceManager;
    [HideInInspector]
    public CityImprovement cityImprovement;
    [HideInInspector]
    public int producedResourceIndex;
    //private TaskDataSO taskData;
    [HideInInspector]
    public ImprovementDataSO improvementData;
    [HideInInspector]
    public int currentLabor;
    //[HideInInspector]
    //public float tempLabor; //if adding labor during production process
    //[HideInInspector]
    //public List<float> tempLaborPercsList = new();
    [HideInInspector]
    public Vector3Int producerLoc;

    //for production info
    private Coroutine producingCo;
    [HideInInspector]
    public int productionTimer;
    //private TimeProgressBar timeProgressBar;
    [HideInInspector]
    public CityImprovementStats cityImprovementStats;
    [HideInInspector]
    public UITimeProgressBar uiTimeProgressBar;
    [HideInInspector]
    public bool isWaitingForStorageRoom, isWaitingforResources, hitResourceMax, isWaitingForResearch, isUpgrading;
    //[HideInInspector]
    //public float unloadLabor;
    [HideInInspector]
    public bool isProducing;
    [HideInInspector]
    public ResourceValue producedResource; //too see what this producer is making
    [HideInInspector]
    public List<ResourceValue> producedResources; 
    private float currentGPM; //generated per minute
    private Dictionary<ResourceType, float> consumedPerMinute = new();
    [HideInInspector]
    public List<ResourceValue> consumedResources = new();
    [HideInInspector]
    public List<ResourceType> consumedResourceTypes = new();
    private WaitForSeconds workTimeWait = new WaitForSeconds(1);
    [HideInInspector]
    public int goldNeeded;
    Vector3Int ICityGoldWait.WaitLoc => producerLoc;
	Vector3Int ICityResourceWait.WaitLoc => producerLoc;
	int ICityGoldWait.waitId => -1;
    int ICityResourceWait.waitId => -1;

	public void InitializeImprovementData(ImprovementDataSO data, ResourceType type, int index)
    {
        improvementData = data;
        //ResourceValue laborCost;
        //laborCost.resourceType = ResourceType.Gold;
        //laborCost.resourceAmount = data.laborCost;
        //consumedResources.Insert(0, laborCost);


        if (data.getTerrainResource)
        {
            if (data.rawResourceType == RawResourceType.Rocks)
            {
                int i = 0;
                foreach (ResourceValue value in data.producedResources)
                {
                    if (ResourceHolder.Instance.GetRocksType(value.resourceType) == ResourceHolder.Instance.GetRocksType(type))
                    {
                        ResourceValue newValue;
                        newValue.resourceType = type;
                        newValue.resourceAmount = value.resourceAmount;

                        producedResource = newValue;
                        producedResources.Add(newValue);
                        //producedResourceIndex = i;
                    }

                    i++;
                }
            }
            else
            {
                for (int i = 0; i < data.producedResources.Count; i++)
                {
                    if (data.producedResources[i].resourceType == type)
                    {
                        producedResource = data.producedResources[i];
                        producedResources.Add(data.producedResources[i]);
                        //producedResourceIndex = i;
                        break;
                    }
                }
            }
            
            return;
        }

		producedResourceIndex = index;
        if (data.producedResources.Count > 0)
        {
			if (type == ResourceType.Fish)
			{
				ResourceValue newValue;
				newValue.resourceType = ResourceType.Food;
				newValue.resourceAmount = data.producedResources[index].resourceAmount;
				producedResource = newValue;
			}
            else
            {
			    producedResource = data.producedResources[producedResourceIndex];
            }
        }

        foreach(ResourceValue value in data.producedResources)
        {
            producedResources.Add(value);
        }
    }

    public void SetConsumedResourceTypes()
    {
        foreach (ResourceValue value in consumedResources)
        {
            consumedResourceTypes.Add(value.resourceType);
        }
    }

    public void SetResourceManager(ResourceManager resourceManager)
    {
        this.resourceManager = resourceManager;
    }

    public void SetCityImprovement(CityImprovement cityImprovement, int index)
    {
        this.cityImprovement = cityImprovement;
        consumedResources = new(cityImprovement.allConsumedResources[index]);
        SetConsumedResourceTypes();
        //cityImprovement.producedResource = producedResource.resourceType;
        //cityImprovement.producedResourceIndex = producedResourceIndex;
    }

    public void SetLocation(Vector3Int loc)
    {
        producerLoc = loc;
        //if (loc != resourceManager.city.cityLoc)
        //    isImprovement = true;

        //if (isImprovement)
        SetCityImprovementStats();
        SetProgressTimeBar();
    }

    public void UpdateCityImprovementStats()
    {
        if (improvementData.maxLabor > 0)
        {
            cityImprovementStats.SetActive(true);
            cityImprovementStats.SetLaborNumber(currentLabor + "/" + improvementData.maxLabor);

            if (producedResource.resourceType != ResourceType.None)
            {
                ResourceType type = cityImprovement.world.GetTerrainDataAt(producerLoc).resourceType == ResourceType.Fish ? ResourceType.Fish : producedResource.resourceType; 
                cityImprovementStats.SetImage(ResourceHolder.Instance.GetIcon(type));
            }
        }
    }

    private void SetCityImprovementStats()
    {
        if (improvementData.maxLabor > 0)
        {
			GameObject statsGO = Instantiate(Resources.Load<GameObject>("Prefabs/InGameSpritePrefabs/CityImprovementStatPanel"), Vector3.zero, Quaternion.Euler(90, 0, 0));
			statsGO.gameObject.transform.SetParent(cityImprovement.transform, false);
            cityImprovementStats = statsGO.GetComponent<CityImprovementStats>();

            if (producedResource.resourceType == ResourceType.None)
                cityImprovementStats.HideImage();

            cityImprovementStats.SetActive(false);
		}
    }

    private void SetProgressTimeBar()
    {
        GameObject progressBar = Instantiate(Resources.Load<GameObject>("Prefabs/InGameSpritePrefabs/TimeProgressBar2"), Vector3.zero, Quaternion.Euler(90, 0, 0));
        progressBar.transform.SetParent(cityImprovement.transform, false);
        uiTimeProgressBar = progressBar.GetComponent<UITimeProgressBar>();
    }

    public void SetProgressTimeBarSize()
    {
        uiTimeProgressBar.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
    }

    public void UpdateCurrentLaborData(int currentLabor)
    {
        this.currentLabor = currentLabor;
		UpdateResourceGenerationData();
	}

    public void UpdateResourceGenerationData()
    {
        if (!improvementData.cityBonus)
            CalculateResourceGenerationPerMinute();
        CalculateResourceConsumedPerMinute();
    }

    //public bool CheckResourceManager(ResourceManager resourceManager)
    //{
    //    return this.resourceManager = resourceManager;
    //}

    public void ShowConstructionProgressTimeBar(int time, bool active)
    {
        uiTimeProgressBar.SetTimeProgressBarValue(time);
        uiTimeProgressBar.SetToZero();
        if (active)
            uiTimeProgressBar.gameObject.SetActive(true);
    }

    public void SetUpgradeProgressTimeBar(int time)
    {
        uiTimeProgressBar.SetTimeProgressBarValue(time);
        uiTimeProgressBar.SetToZero();
        uiTimeProgressBar.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
    }

    public void HideConstructionProgressTimeBar()
    {
        if (improvementData.producedResourceTime.Count > 0)
            uiTimeProgressBar.SetTimeProgressBarValue(improvementData.producedResourceTime[producedResourceIndex]);
		uiTimeProgressBar.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
		uiTimeProgressBar.gameObject.SetActive(false);
    }

    public void SetNewProgressTime()
    {
        uiTimeProgressBar.SetTimeProgressBarValue(improvementData.producedResourceTime[producedResourceIndex]);
    }

    public void SetConstructionTime(int time)
    {
        uiTimeProgressBar.SetTime(time);
    }

	public bool RestartGold(int gold)
	{
		if (goldNeeded <= gold)
		{
            RestartResourceWaitProduction();
			return true;
		}
		else
		{
			return false;
		}
	}

    public bool RestartResourceMax(ResourceType type)
    {
        return resourceManager.resourceMaxHoldDict[type] - resourceManager.resourceDict[type] >= producedResource.resourceAmount * currentLabor;
	}

    public bool RestartCheck(ResourceType type)
    {
        if (type == ResourceType.None)
            return resourceManager.city.warehouseStorageLimit - resourceManager.resourceStorageLevel >= resourceManager.CalculateResourceProductionAmount(producedResource, currentLabor, cityImprovement);
        else
            return resourceManager.ResourceWaitCheck(consumedResources, currentLabor, type);
    }

    public void Restart(ResourceType type)
    {
		if (type == ResourceType.None)
        {
            isWaitingForStorageRoom = false;
            hitResourceMax = false;
		    cityImprovement.exclamationPoint.SetActive(false);

			int unloadAmount = resourceManager.CalculateResourceProductionAmount(producedResource, currentLabor, cityImprovement);
			int maxLimit = resourceManager.resourceMaxHoldDict[producedResource.resourceType];
            ResourceType producedType = producedResource.resourceType;// == ResourceType.Fish ? ResourceType.Food : producedResource.resourceType;
            if (resourceManager.city.warehouseStorageLimit - resourceManager.resourceStorageLevel < unloadAmount)
            {
                PrepForStorageRoomWaitList();
            }
            else if (maxLimit >= 0 && maxLimit - resourceManager.resourceDict[producedType] < unloadAmount)
            {
                PrepForResourceMaxWaitList(producedType);
            }
            else
            {
                if (resourceManager.city.activeCity)
                {
                    uiTimeProgressBar.gameObject.SetActive(true);
                    uiTimeProgressBar.SetToZero();
                }

                RestartProductionCheck(unloadAmount);
            }
        }
        else
        {
			RestartResourceWaitProduction();
		}
	}

	//checking if production can continue
	public void RestartResourceWaitProduction()
    {
        if (resourceManager.ConsumeResourcesCheck(consumedResources, currentLabor, this))
        {
            isWaitingforResources = false;
			cityImprovement.world.uiCityImprovementTip.ToggleWaiting(false, cityImprovement);
			//resourceManager.RemoveFromResourceWaitList(this);
            //resourceManager.RemoveFromResourcesNeededForProduction(consumedResourceTypes);

			cityImprovement.exclamationPoint.SetActive(false);
			StartProducing(false);
        }
    }

    //for producing resources
    public void StartProducing(bool checkResources)
    {
        //CalculateResourceGenerationPerMinute(); //calculate before checks to show stats
        //CalculateResourceConsumedPerMinute();
        cityImprovement.firstStart = true;
        isProducing = true;

        if (improvementData.isResearch)
        {
            if (!cityImprovement.world.researching)
            {
                AddToResearchWaitList();
                return;
            }
            else if (!resourceManager.ConsumeResourcesCheck(consumedResources, currentLabor, this))
            {
				AddToResourceWaitList();
                return;
			}
        }
        else if (checkResources && !resourceManager.ConsumeResourcesCheck(consumedResources, currentLabor, this))
        {
            AddToResourceWaitList();
            return;
        }

        if (improvementData.cityBonus)
        {
			if (resourceManager.city.activeCity)
            {
				uiTimeProgressBar.gameObject.SetActive(true);
				uiTimeProgressBar.SetProgressBarMask(resourceManager.city.countDownTimer);
                uiTimeProgressBar.SetTime(resourceManager.city.countDownTimer);
            }

            resourceManager.city.AddCityBonus(cityImprovement, true, 1);
			cityImprovement.StartWork(0, false);
			resourceManager.ConsumeResources(consumedResources, currentLabor, producerLoc);
		}
        else
        {
            if (resourceManager.city.activeCity)
                uiTimeProgressBar.gameObject.SetActive(true);

		    productionTimer = improvementData.producedResourceTime[producedResourceIndex];
		    producingCo = StartCoroutine(ProducingCoroutine());
        }
    }

    public void LoadProducingCoroutine()
    {
        if (improvementData.cityBonus)
        {
            resourceManager.city.AddCityBonus(cityImprovement, true, currentLabor);
			cityImprovement.firstStart = true;
			cityImprovement.StartWork(0, true);
		}
        else
        {
		    cityImprovement.firstStart = true;
            float percLeft = 1 - ((float)productionTimer / improvementData.producedResourceTime[producedResourceIndex]);
            producingCo = StartCoroutine(ProducingCoroutine(percLeft, true));
        }

        UpdateResourceGenerationData();
	}

    public void AddLaborMidProduction()
    {
		//UpdateResourceGenerationData();

		//float percWorked;
        /*if (isWaitingforResources || isWaitingForStorageRoom || hitResourceMax)
        {
            percWorked = 0;
        }
        else if (!resourceManager.ConsumeResourcesCheckMidProd(consumedResources, 1)*//*!ConsumeResourcesCheck()*//*)
        {
            percWorked = 0;
        }
        else*/ if (improvementData.cityBonus)
        {
            //percWorked = 1;
            if (improvementData.cityBonus)
                resourceManager.city.AddCityBonus(cityImprovement, false, 1);
        }
        //else
        //{
        //    percWorked = 1/*(float)productionTimer / improvementData.producedResourceTime[producedResourceIndex]*/; //no more partial productions
        //}

        //tempLabor += percWorked;
        //tempLaborPercsList.Add(percWorked);
        resourceManager.ConsumeResources(consumedResources, 1, producerLoc);
    }

    public void RemoveLaborCheck(City city)
    {
		RemoveLaborMidProduction();
		if (isWaitingforResources)
		{
			for (int i = 0; i < consumedResources.Count; i++)
			{
				if (consumedResources[i].resourceType == ResourceType.Gold)
				{
					goldNeeded = consumedResources[i].resourceAmount * currentLabor;
					city.world.GoldWaitListCheck();
				}
				else
				{
					city.resourceManager.ResourceWaitListCheck(consumedResources[i].resourceType);
				}
			}
		}
		else if (isWaitingForStorageRoom)
		{
			city.resourceManager.StorageSpaceCheck();
		}
		else if (hitResourceMax)
		{
			city.resourceManager.ResourceMaxWaitListCheck(producedResource.resourceType);
		}
	}

    private void RemoveLaborMidProduction()
    {
		//UpdateResourceGenerationData();

		if (!isWaitingForResearch && !isWaitingforResources /*&& !isWaitingForStorageRoom && !hitResourceMax*/)
        {
   //         int tempLaborPercCount = tempLaborPercsList.Count;

   //         if (tempLaborPercCount > 0)
   //         {
   //             int index = tempLaborPercCount - 1;
   //             float tempLaborRemoved = tempLaborPercsList[index]; //LIFO
   //             tempLaborPercsList.RemoveAt(index);
   //             tempLabor -= tempLaborRemoved;
   //             resourceManager.PrepareConsumedResource(consumedResources, tempLaborRemoved, producerLoc);
                
   //             if (improvementData.cityBonus && tempLaborRemoved > 0)
			//	    resourceManager.city.RemoveCityBonus(cityImprovement, false, 1);
   //         }
   //         else
   //         {
			//}
            resourceManager.PrepareConsumedResource(consumedResources, 1, producerLoc);

			if (improvementData.cityBonus)
				resourceManager.city.RemoveCityBonus(cityImprovement, false, 1);
		}
    }

    //public void RestartMidProduction()
    //{
    //    StopProducing(true);
    //    StartProducing();
    //}

    //checking if is currently paused
    public bool ProductionPausedCheck()
    {
        return isWaitingforResources || isWaitingForStorageRoom || hitResourceMax;
    }

    //checking if one more labor can be added
    public bool ConsumeResourcesCheck()
    {
        return resourceManager.ConsumeResourcesCheckMidProd(consumedResources, 1);
    }

    //timer for producing resources 
    private IEnumerator ProducingCoroutine(float offset = 0, bool load = false)
    {
        //productionTimer = improvementData.producedResourceTime[producedResourceIndex];
        cityImprovement.StartWork(offset, load);
        if (resourceManager.city.activeCity)
        {
            //timeProgressBar.SetProgressBarBeginningPosition();
            //timeProgressBar.SetTime(productionTimer);
            uiTimeProgressBar.SetToZero();
            uiTimeProgressBar.SetTime(productionTimer);
        }

        if (!load)
        {
            //tempLabor = currentLabor;
            resourceManager.ConsumeResources(consumedResources, currentLabor, producerLoc);
        }

        while (productionTimer > 0)
        {
            yield return workTimeWait;
            productionTimer--;
			if (resourceManager.city.activeCity)
                uiTimeProgressBar.SetTime(productionTimer);
        }

        int unloadAmount = resourceManager.CalculateResourceProductionAmount(producedResource, currentLabor, cityImprovement);
        //checking if still researching
        if (improvementData.isResearch)
        {
            if (!cityImprovement.world.researching)
            {
                isWaitingForStorageRoom = true;
                cityImprovement.exclamationPoint.SetActive(true);
                cityImprovement.world.uiCityImprovementTip.ToggleWaiting(true, cityImprovement, false, false, true);
                //unloadLabor = tempLabor;
                //resourceManager.waitingToUnloadResearch.Add(this);
                //timeProgressBar.SetToZero();
                uiTimeProgressBar.SetToFull();
                cityImprovement.StopWork();
                resourceManager.city.AddToWorldResearchWaitList(this);
            }
            else
            {
                RestartProductionCheck(unloadAmount);
            }

            yield break;
        }

        int maxLimit = resourceManager.resourceMaxHoldDict[producedResource.resourceType];
        ResourceType type = producedResource.resourceType;// == ResourceType.Fish ? ResourceType.Food : producedResource.resourceType;
        //checking if storage is free to unload
        if (resourceManager.city.warehouseStorageLimit - resourceManager.resourceStorageLevel < unloadAmount)
            PrepForStorageRoomWaitList();
        else if (maxLimit >= 0 && maxLimit - resourceManager.resourceDict[type] < unloadAmount)
            PrepForResourceMaxWaitList(type);
        else
            RestartProductionCheck(unloadAmount);
    }

    private void PrepForStorageRoomWaitList()
    {
		AddToStorageRoomWaitList();
		//unloadLabor = tempLabor;
		uiTimeProgressBar.SetToFull();
		cityImprovement.StopWork();
	}

    private void PrepForResourceMaxWaitList(ResourceType type)
    {
		AddToResourceMaxWaitList(type);
        //uiTimeProgressBar.SetToFull();
        uiTimeProgressBar.gameObject.SetActive(false);
        cityImprovement.StopWork();
	}

    private void RestartProductionCheck(int unloadAmount)
    {
        if (resourceManager.PrepareResource(unloadAmount, producedResource, producerLoc, cityImprovement))
        {
            cityImprovement.DestroyImprovement();
            return;
        }

        //checking storage again after loading
        if (improvementData.isResearch)
        {
            if (!cityImprovement.world.researching)
            {
                AddToResearchWaitList();
			    cityImprovement.exclamationPoint.SetActive(true);
			    cityImprovement.world.uiCityImprovementTip.ToggleWaiting(true, cityImprovement, false, false, true);
			    uiTimeProgressBar.gameObject.SetActive(false);
                cityImprovement.StopWork();
            }
            else if (!resourceManager.ConsumeResourcesCheck(consumedResources, currentLabor, this))
            {
                SetUpResourceWaiting();
			}
            else
            {
                ReadyToProduce();
			}
        }
        else if (!resourceManager.ConsumeResourcesCheck(consumedResources, currentLabor, this))
        {
            SetUpResourceWaiting();
		}
        else
        {
			ReadyToProduce();
		}
    }

    public void RestartCityBonusCheck(bool active)
    {
        if (active)
        {
			uiTimeProgressBar.SetProgressBarMask(resourceManager.city.secondsTillGrowthCheck);
			uiTimeProgressBar.SetTime(resourceManager.city.secondsTillGrowthCheck);
		}
        
        if (!resourceManager.ConsumeResourcesCheck(consumedResources, currentLabor, this))
        {
            resourceManager.city.RemoveCityBonus(cityImprovement, true, currentLabor);
            SetUpResourceWaiting();
        }
        else
        {
			resourceManager.ConsumeResources(consumedResources, currentLabor, producerLoc);
		}
	}

    private void SetUpResourceWaiting()
    {
		AddToResourceWaitList();
		//cityImprovement.exclamationPoint.SetActive(true);
		//resourceManager.city.world.uiCityImprovementTip.ToggleWaiting(true, cityImprovement, true);
		uiTimeProgressBar.gameObject.SetActive(false);
		cityImprovement.StopWork();
	}

    private void ReadyToProduce()
    {
		//tempLaborPercsList.Clear();
		productionTimer = improvementData.producedResourceTime[producedResourceIndex];
		producingCo = StartCoroutine(ProducingCoroutine());
	}

    public void StopProducing(bool allLabor = false)
    {
		cityImprovement.exclamationPoint.SetActive(false);

		if (isWaitingForResearch)
        {
            resourceManager.RemoveFromResearchWaitlist(this);
            isWaitingForResearch = false;
        }
        else if (isWaitingForStorageRoom)
        {
            isWaitingForStorageRoom = false;
            resourceManager.RemoveFromUnloadWaitList(this);
            //resourceManager.RemoveFromStorageRoomWaitList(this);
			float laborCount = allLabor ? currentLabor : 1;
            resourceManager.PrepareConsumedResource(consumedResources, laborCount, producerLoc);
        }
        else if (isWaitingforResources)
        {
            //resourceManager.RemoveFromResourceWaitList(this);
            int num = resourceManager.RemoveFromGoldWaitList(this);
            if (num >= 0)
                cityImprovement.world.RemoveCityFromGoldWaitList(resourceManager.city, num);
            resourceManager.RemoveFromCityResourceWaitList(this, consumedResources);
            //resourceManager.RemoveFromResourcesNeededForProduction(consumedResourceTypes);
            isWaitingforResources = false;
			cityImprovement.world.uiCityImprovementTip.ToggleWaiting(false, cityImprovement);

            if (improvementData.cityBonus)
				resourceManager.city.RemoveCityBonus(cityImprovement, true, allLabor ? currentLabor : 1);
		}
        else if (hitResourceMax)
        {
            hitResourceMax = false;
            resourceManager.RemoveFromCityResourceMaxWaitList(this, producedResource.resourceType);
			float laborCount = allLabor ? currentLabor : 1;
			resourceManager.PrepareConsumedResource(consumedResources, laborCount, producerLoc);
		}
        else if (improvementData.cityBonus)
        {
            resourceManager.city.RemoveCityBonus(cityImprovement, true, allLabor ? currentLabor : 1);
			resourceManager.PrepareConsumedResource(consumedResources, allLabor ? currentLabor : 1, producerLoc);
		}
        else if (producingCo != null)
        {
            StopCoroutine(producingCo);
            producingCo = null;
            float laborCount = allLabor ? currentLabor : 1;
            resourceManager.PrepareConsumedResource(consumedResources, laborCount, producerLoc);
        }

        cityImprovement.StopWork();
        //timeProgressBar.SetActive(false);
        uiTimeProgressBar.gameObject.SetActive(false);
        isProducing = false;
    }

	public void CheckProducerResearchWaitList()
	{
		if (cityImprovement.world.researching)
		{
			isWaitingForResearch = false;

            if (isWaitingForStorageRoom)
            {
                isWaitingForStorageRoom = false;
                int unloadAmount = resourceManager.CalculateResourceProductionAmount(producedResource, currentLabor, cityImprovement);
				resourceManager.PrepareResource(unloadAmount, producedResource, producerLoc, cityImprovement);
            }

			cityImprovement.exclamationPoint.SetActive(false);
			StartProducing(true);
		}
	}

	private void AddToResearchWaitList()
    {
        isWaitingForResearch = true;
		cityImprovement.exclamationPoint.SetActive(true);
		cityImprovement.world.uiCityImprovementTip.ToggleWaiting(true, cityImprovement, false, false, true);
		//resourceManager.AddToResearchWaitList(this);
		resourceManager.city.AddToWorldResearchWaitList(this);
    }

    private void AddToStorageRoomWaitList()
    {
        isWaitingForStorageRoom = true;
		cityImprovement.exclamationPoint.SetActive(true);
		cityImprovement.world.uiCityImprovementTip.ToggleWaiting(true, cityImprovement, false, true);
		SetTimeProgressBarToFull();
		//uiTimeProgressBar.gameObject.SetActive(false);
		resourceManager.AddToUnloadWaitList(this);
		//resourceManager.AddToStorageRoomWaitList(this);
    }

    private void AddToResourceMaxWaitList(ResourceType type)
    {
        hitResourceMax = true;
		cityImprovement.world.uiCityImprovementTip.ToggleWaiting(true, cityImprovement, false, false, true);
		SetTimeProgressBarToFull();
		//uiTimeProgressBar.gameObject.SetActive(false);
		resourceManager.AddToResourceMaxWaitList(type, this);
	}

    private void AddToResourceWaitList()
    {
        isWaitingforResources = true;
		cityImprovement.exclamationPoint.SetActive(true);
		cityImprovement.world.uiCityImprovementTip.ToggleWaiting(true, cityImprovement, true);
        //resourceManager.AddToResourceWaitList(this);
        //resourceManager.AddToResourcesNeededForProduction(consumedResourceTypes);
    }

    public void SetTimeProgressBarToFull()
    {
        uiTimeProgressBar.SetToFull();
    }

    public void TimeProgressBarSetActive(bool v)
    {
        if (isProducing)
        {
            //timeProgressBar.SetActive(v);
            uiTimeProgressBar.gameObject.SetActive(v);
            if (v)
            {
                int time;
                if (improvementData.cityBonus)
                    time = resourceManager.city.countDownTimer;
                else
                    time = productionTimer;

				uiTimeProgressBar.SetProgressBarMask(time);
                uiTimeProgressBar.SetTime(time);
            }
        }
    }

    public void TimeConstructionProgressBarSetActive(bool v)
    {
        //timeProgressBar.SetActive(v);
        uiTimeProgressBar.gameObject.SetActive(v);
        if (v)
        {
            uiTimeProgressBar.SetProgressBarMask(cityImprovement.timePassed);
            uiTimeProgressBar.SetTime(cityImprovement.timePassed);
        }
    }

    //recalculating generation per resource every time labor/work ethic changes
    public void CalculateResourceGenerationPerMinute()
    {
        if (improvementData.producedResourceTime.Count == 0)
            return;

        //subtracting prev amount first, then adding updated one
		resourceManager.ModifyResourceGenerationPerMinute(producedResource.resourceType, -currentGPM);
		currentGPM = Mathf.RoundToInt(resourceManager.CalculateResourceGeneration(producedResource.resourceAmount, currentLabor, producedResource.resourceType) * 
            (60f / improvementData.producedResourceTime[producedResourceIndex]));
        resourceManager.ModifyResourceGenerationPerMinute(producedResource.resourceType, currentGPM);
    }

    //only changes when labor changes, not work ethic
    private void CalculateResourceConsumedPerMinute()
    {
        foreach (ResourceValue resourceValue in consumedResources)
        {
            //subtracting prev amount first, then adding updated one
            if (!consumedPerMinute.ContainsKey(resourceValue.resourceType))
                consumedPerMinute[resourceValue.resourceType] = 0;
            else
                resourceManager.ModifyResourceConsumptionPerMinute(resourceValue.resourceType, -consumedPerMinute[resourceValue.resourceType]);

            int amount = Mathf.RoundToInt((resourceValue.resourceAmount * currentLabor) * (60f / improvementData.producedResourceTime[producedResourceIndex]));
            consumedPerMinute[resourceValue.resourceType] = amount;
            resourceManager.ModifyResourceConsumptionPerMinute(resourceValue.resourceType, amount);

            if (amount == 0)
                consumedPerMinute.Remove(resourceValue.resourceType);
        }
    }

    //public void DestroyProgressBar()
    //{
    //    Destroy(uiTimeProgressBar.gameObject);

    //    if (cityImprovementStats != null)
    //        Destroy(cityImprovementStats.gameObject);
    //}
}
