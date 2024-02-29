using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class PersonalResourceManager : MonoBehaviour
{
    private Dictionary<ResourceType, int> resourceDict = new();

    public Dictionary<ResourceType, int> ResourceDict { get { return resourceDict; } set { resourceDict = value; } }

    [HideInInspector]
    public int resourceStorageLimit;
    private int resourceStorageLevel;
    public int ResourceStorageLevel { get { return resourceStorageLevel; } set { resourceStorageLevel = value; } }

    private MapWorld world;
    private Unit unit;

    public void SetUnit(Unit unit)
    {
        world = unit.world;
        this.unit = unit;
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

    public void SubtractResource(ResourceType type, int amount)
    {
		AddRemoveResource(type, -amount);
	}

    public int ManuallySubtractResource(ResourceType type, int amount)
    {
		amount = SubtractResourceCheck(type, amount);
		if (amount > 0)
			AddRemoveResource(type, -amount);

		return amount;
	}

    private int SubtractResourceCheck(ResourceType type, int amount)
    {
		int prevAmount = resourceDict[type];

		if (prevAmount < amount)
			amount = prevAmount;

		return amount;
	}

    public void AddResource(ResourceType type, int amount)
    {
		AddRemoveResource(type, amount);
	}

	public int ManuallyAddResource(ResourceType type, int amount)
	{
		amount = AddResourceCheck(amount);
		if (amount > 0)
        {
			if (!world.mainPlayer.resourceGridDict.ContainsKey(type))
				world.mainPlayer.AddToGrid(type);

			AddRemoveResource(type, amount);
        }

		return amount;
	}

	private int AddResourceCheck(int amount)
    {
		int diff = resourceStorageLimit - resourceStorageLevel;

		if (diff < amount)
			amount = diff;

		return amount;
	}

	private void UICheck(ResourceType type)
    {
        if (world.unitMovement.uiPersonalResourceInfoPanel.activeStatus && world.unitMovement.uiPersonalResourceInfoPanel.unit == unit)
        {
            world.unitMovement.uiPersonalResourceInfoPanel.UpdateResource(type, GetResourceDictValue(type));
		    world.unitMovement.uiPersonalResourceInfoPanel.UpdateStorageLevel(resourceStorageLevel);
        }
	}

    private int AddRemoveResource(ResourceType type, int amount)
    {
		resourceDict[type] += amount;
		resourceStorageLevel += amount;

        UICheck(type);

        return amount;
    }

    public int GetResourceDictValue(ResourceType resourceType)
    {
        return resourceDict[resourceType];
    }
}
