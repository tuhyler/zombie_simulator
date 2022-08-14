using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersonalResourceManager : MonoBehaviour
{
    private Dictionary<ResourceType, int> resourceDict = new();
    private Dictionary<ResourceType, float> resourceStorageMultiplierDict = new();

    public Dictionary<ResourceType, int> ResourceDict { get { return resourceDict; } }

    private int resourceStorageLimit;
    public int ResourceStorageLimit { get { return resourceStorageLimit; } set { resourceStorageLimit = value; } }
    private float resourceStorageLevel;
    public float GetResourceStorageLevel { get { return resourceStorageLevel; } }
    //public List<ResourceIndividualSO> initialResourceData = new(); //list the resource data of resources to store


    private void Awake()
    {
        PrepareResourceDictionary();
    }

    private void PrepareResourceDictionary()
    {
        foreach (ResourceIndividualSO resourceData in ResourceHolder.Instance.allStorableResources) //Enum.GetValues(typeof(ResourceType)) 
        {
            if (resourceData.resourceType == ResourceType.None)
                continue;
            resourceDict[resourceData.resourceType] = 0;
            resourceStorageMultiplierDict[resourceData.resourceType] = resourceData.resourceStorageMultiplier;
        }
    }

    public int CheckResource(ResourceType resourceType, int resourceAmount)
    {
        if (CheckStorageSpaceForResource(resourceType, resourceAmount) && resourceDict.ContainsKey(resourceType))
        {
            return AddResourceToStorage(resourceType, resourceAmount);
        }
        else
        {
            Debug.Log($"Error moving {resourceType}!");
            return 0;
        }
    }

    private int AddResourceToStorage(ResourceType resourceType, int resourceAmount)
    {
        if (resourceAmount < 0 && -resourceAmount > resourceDict[resourceType])
        {
            resourceAmount = -resourceDict[resourceType];
        }

        if (resourceStorageMultiplierDict.ContainsKey(resourceType))
            resourceAmount = Mathf.CeilToInt(resourceAmount * resourceStorageMultiplierDict[resourceType]);

        int newResourceBalance = (Mathf.CeilToInt(resourceStorageLevel) + resourceAmount) - resourceStorageLimit;

        if (newResourceBalance >= 0 && resourceStorageLimit >= 0)//limit of 0 or less means infinite storage
        {
            resourceAmount -= newResourceBalance;
        }

        int resourceAmountAdjusted = Mathf.RoundToInt(resourceAmount / resourceStorageMultiplierDict[resourceType]);
        resourceDict[resourceType] += resourceAmountAdjusted;
        VerifyResourceAmount(resourceType); //check to see if resource is less than 0 (just in case)

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
        return Mathf.CeilToInt(resourceStorageLevel + resourceStorageMultiplierDict[resourceType]) <= resourceStorageLimit;
    }

    private void VerifyResourceAmount(ResourceType resourceType)
    {
        if (resourceDict[resourceType] < 0 && resourceType != ResourceType.Food)
            throw new InvalidOperationException("Can't have resources less than 0 " + resourceType);
    }

    public int GetResourceDictValue(ResourceType resourceType)
    {
        return resourceDict[resourceType];
    }
}
