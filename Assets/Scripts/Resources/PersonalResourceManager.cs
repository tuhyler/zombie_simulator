using System;
using System.Collections.Generic;
using UnityEngine;

public class PersonalResourceManager : MonoBehaviour
{
    private Dictionary<ResourceType, int> resourceDict = new();
    //private Dictionary<ResourceType, float> resourceStorageMultiplierDict = new();

    public Dictionary<ResourceType, int> ResourceDict { get { return resourceDict; } set { resourceDict = value; } }

    private int resourceStorageLimit;
    public int ResourceStorageLimit { get { return resourceStorageLimit; } set { resourceStorageLimit = value; } }
    private float resourceStorageLevel;
    public float ResourceStorageLevel { get { return resourceStorageLevel; } set { resourceStorageLevel = value; } }
    //public List<ResourceIndividualSO> initialResourceData = new(); //list the resource data of resources to store

    private Trader trader;


    public void SetTrader(Trader trader)
    {
        this.trader = trader;
    }

    public void ResetDict(List<ResourceValue> resources)
    {
		for (int i = 0; i < resources.Count; i++)
		{
			if (resourceDict[resources[i].resourceType] == 0)
				resourceDict.Remove(resources[i].resourceType);
		}
	}

    public void ResetDictSolo(ResourceType type)
    {
		if (resourceDict[type] == 0)
			resourceDict.Remove(type);
	}

    public void DictCheck(List<ResourceValue> resources)
    {
        for (int i = 0; i < resources.Count; i++)
        {
            if (!resourceDict.ContainsKey(resources[i].resourceType))
                resourceDict[resources[i].resourceType] = 0;
        }
    }

    public void DictCheckSolo(ResourceType type)
    {
        if (!resourceDict.ContainsKey(type))
            resourceDict[type] = 0;   
    }

    public int CheckResource(ResourceType type, int resourceAmount)
    {
        if (CheckStorageSpaceForResource(type, resourceAmount))
        {
            if (!trader.resourceGridDict.ContainsKey(type))
                trader.AddToGrid(type);

            return AddResourceToStorage(type, resourceAmount);
        }
        else
        {
            if (!trader.followingRoute)
                UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Full inventory");
            return 0;
        }
    }

    private int AddResourceToStorage(ResourceType resourceType, int resourceAmount)
    {
        if (resourceAmount < 0 && -resourceAmount > resourceDict[resourceType])
        {
            resourceAmount = -resourceDict[resourceType];
        }

        resourceAmount = Mathf.CeilToInt(resourceAmount * trader.world.resourceStorageMultiplierDict[resourceType]);

        int newResourceBalance = (Mathf.CeilToInt(resourceStorageLevel) + resourceAmount) - resourceStorageLimit;

        if (newResourceBalance >= 0 && resourceStorageLimit >= 0)//limit of 0 or less means infinite storage
        {
            resourceAmount -= newResourceBalance;
        }

        int resourceAmountAdjusted = Mathf.RoundToInt(resourceAmount / trader.world.resourceStorageMultiplierDict[resourceType]);
        resourceDict[resourceType] += resourceAmountAdjusted;
        //VerifyResourceAmount(resourceType); //check to see if resource is less than 0 (just in case)

        resourceStorageLevel += resourceAmount;

        return resourceAmountAdjusted;
    }

    private bool CheckStorageSpaceForResource(ResourceType resourceType, int resourceAdded)
    {
        if (resourceAdded > 0 && resourceStorageLimit <= 0) //unlimited space if 0  
            return true;
        if (resourceAdded < 0)
        {
            if (resourceDict[resourceType] == 0)
                return false;
            return true;
        }
        return Mathf.CeilToInt(resourceStorageLevel + trader.world.resourceStorageMultiplierDict[resourceType]) <= resourceStorageLimit;
    }

    private void VerifyResourceAmount(ResourceType resourceType)
    {
        if (resourceDict[resourceType] <= 0)
            resourceDict.Remove(resourceType);
    }

    public int GetResourceDictValue(ResourceType resourceType)
    {
        return resourceDict[resourceType];
    }
}
