using System.Collections.Generic;
using UnityEngine;

public class WorldResourceManager : MonoBehaviour
{
    private UIWorldResources uiWorldResources;

    public Dictionary<ResourceType, int> resourceDict = new(); 

    //initial resources
    public List<ResourceValue> worldResources = new(); //define world resources here

    private void Awake()
    {
        PrepareResourceDictionary();
        //SetInitialResourceValues();
    }

    private void PrepareResourceDictionary()
    {
        foreach (ResourceValue resourceValue in worldResources) 
        {
            ResourceType resourceType = resourceValue.resourceType;
            if (resourceType == ResourceType.None)
                continue;
            resourceDict[resourceType] = resourceValue.resourceAmount;
        }

        UpdateUI();
    }

    //private void SetInitialResourceValues()
    //{
    //    foreach (ResourceValue initialResourceValue in worldResources)
    //    {
    //        if (initialResourceValue.resourceType == ResourceType.None)
    //            throw new ArgumentException("Resource can't be none!");
    //        resourceDict[initialResourceValue.resourceType] = initialResourceValue.resourceAmount; //assigns the initial values for each resource
    //    }

    //    UpdateUI();
    //}

    //public void SetResearch(int amount)
    //{
    //    resourceDict[ResourceType.Research] = amount;
    //}

    public void SetResource(ResourceType resourceType, int resourceAmount)
    {
        resourceDict[resourceType] += resourceAmount; //updating the dictionary
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

    public void SetWorldGoldLevel(int gold)
    {
        resourceDict[ResourceType.Gold] = gold;
        UpdateUI();
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
    }
}
