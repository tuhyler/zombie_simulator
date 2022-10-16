using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldResourceManager : MonoBehaviour
{
    private UIWorldResources uiWorldResources;

    private Dictionary<ResourceType, int> resourceDict = new(); 
    private Dictionary<ResourceType, float> resourceGenerationPerTurnDict = new();

    //initial resources
    public List<ResourceValue> worldResources = new(); //define world resources here
    //public List<ResourceGenerationValue> initialWorldResourcesGeneration = new(); 

    private void Awake()
    {
        PrepareResourceDictionary();
        SetInitialResourceValues();
        //SetInitialResourceGenerationValues();
    }

    private void PrepareResourceDictionary()
    {
        foreach (ResourceValue resourceValue in worldResources) 
        {
            ResourceType resourceType = resourceValue.resourceType;
            if (resourceType == ResourceType.None)
                continue;
            resourceDict[resourceType] = 0;
            resourceGenerationPerTurnDict[resourceType] = 0;
        }
    }

    private void SetInitialResourceValues()
    {
        foreach (ResourceValue initialResourceValue in worldResources)
        {
            if (initialResourceValue.resourceType == ResourceType.None)
                throw new ArgumentException("Resource can't be none!");
            resourceDict[initialResourceValue.resourceType] = initialResourceValue.resourceAmount; //assigns the initial values for each resource
        }
    }

    //private void SetInitialResourceGenerationValues()
    //{
    //    foreach (ResourceGenerationValue initialResourceGeneration in initialWorldResourcesGeneration)
    //    {
    //        if (initialResourceGeneration.resourceType == ResourceType.None)
    //            throw new ArgumentException("Resource can't be none!");
    //        resourceGenerationPerMinuteDict[initialResourceGeneration.resourceType] = initialResourceGeneration.resourceGenerationAmount; //assign initial generation
    //    }
    //}

    public void SetResource(ResourceType resourceType, int resourceAmount)
    {
        resourceDict[resourceType] += resourceAmount; //updating the dictionary
        UpdateUI(resourceType);
    }

    public void ModifyResourceGenerationPerMinute(ResourceType resourceType, float generationDiff)
    {
        resourceGenerationPerTurnDict[resourceType] += generationDiff;
        UpdateUI(resourceType);
    }

    public List<ResourceType> PassWorldResources()
    {
        List<ResourceType> resourceList = new();

        foreach (ResourceValue resourceValue in worldResources)
        {
            resourceList.Add(resourceValue.resourceType);
        }

        return resourceList;
    }

    //methods for managing world resource UI
    public void SetUI(UIWorldResources uiWorldResources)
    {
        this.uiWorldResources = uiWorldResources;
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
        uiWorldResources.SetResource(resourceType, resourceDict[resourceType]);
        //uiWorldResources.SetResourceGenerationAmount(resourceType, resourceGenerationPerMinuteDict[resourceType]);
    }

    public void UpdateUIGeneration(ResourceType resourceType, float diffAmount)
    {
        uiWorldResources.SetResourceGenerationAmount(resourceType, diffAmount);
    }
}
