using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UtilityCostDisplay : MonoBehaviour
{
	[SerializeField]
	private Transform utilityCostDisplay;
	private Dictionary<ResourceType, ResourceInfoPanel> resourcesShownDict = new();
	private List<ResourceInfoPanel> resourceList = new();
	private List<int> usedResources = new();
	[HideInInspector]
	public bool hasEnough;
	[HideInInspector]
	public Vector3Int currentLoc;
	[HideInInspector]
	public int inventoryCount;
	
	private void Awake()
	{
		foreach (Transform resource in utilityCostDisplay)
		{
			resourceList.Add(resource.GetComponent<ResourceInfoPanel>());
			resource.gameObject.SetActive(false);
		}

		gameObject.SetActive(false);
	}

	void LateUpdate()
	{
		transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
	}

	public void ShowUtilityCostDisplay(Vector3Int loc)
	{
		currentLoc = loc;
		transform.position = loc;
		gameObject.SetActive(true);
	}

	public void AddCost(List<ResourceValue> costs, Dictionary<ResourceType, int> resourceDict, bool building)
    {
		for (int i = 0; i < costs.Count; i++)
		{
			if (resourcesShownDict.ContainsKey(costs[i].resourceType))
			{
				int storageAmount = 0;

				if (resourceDict.ContainsKey(costs[i].resourceType))
					storageAmount = resourceDict[costs[i].resourceType];

				resourcesShownDict[costs[i].resourceType].amount += costs[i].resourceAmount;
				bool hasEnough = false;
				if (building)
				{
					if (storageAmount >= resourcesShownDict[costs[i].resourceType].amount)
						hasEnough = true;
				}
				else
				{
					inventoryCount += costs[i].resourceAmount;
				}

				resourcesShownDict[costs[i].resourceType].SetResourcePanelAmount(hasEnough, building);
			}
			else
			{
				for (int j = 0; j < resourceList.Count; j++)
				{
					if (!usedResources.Contains(j))
					{
						resourceList[j].gameObject.SetActive(true);
						usedResources.Add(j);
						resourcesShownDict[costs[i].resourceType] = resourceList[j];

						bool hasEnough = false;
						if (building)
						{
							if (resourceDict.ContainsKey(costs[i].resourceType) && resourceDict[costs[i].resourceType] >= costs[i].resourceAmount)
								hasEnough = true;
						}
						else
						{
							inventoryCount += costs[i].resourceAmount;
						}
						resourceList[j].SetResourcePanel(ResourceHolder.Instance.GetIcon(costs[i].resourceType), costs[i].resourceAmount, hasEnough, building);
						break;
					}
				}
			}
		}

		if (building)
			CheckIfCanAfford(resourceDict);
    }

	public void SubtractCost(List<ResourceValue> costs, Dictionary<ResourceType, int> resourceDict, bool building)
	{
		for (int i = 0; i < costs.Count; i++)
		{
			resourcesShownDict[costs[i].resourceType].amount -= costs[i].resourceAmount;
			bool hasEnough = false;

			if (building)
			{
				if (resourceDict.ContainsKey(costs[i].resourceType) && resourceDict[costs[i].resourceType] >= resourcesShownDict[costs[i].resourceType].amount)
					hasEnough = true;
			}
			else
			{
				inventoryCount -= costs[i].resourceAmount;
			}

			resourcesShownDict[costs[i].resourceType].SetResourcePanelAmount(hasEnough, building);

			if (resourcesShownDict[costs[i].resourceType].amount <= 0)
			{
				usedResources.Remove(resourceList.IndexOf(resourcesShownDict[costs[i].resourceType]));
				resourcesShownDict[costs[i].resourceType].gameObject.SetActive(false);
				resourcesShownDict.Remove(costs[i].resourceType);
			}
		}

		if (building)
			CheckIfCanAfford(resourceDict);
	}

	//have to do separately
	public void CheckIfCanAfford(Dictionary<ResourceType, int> resourceDict)
	{
		foreach (ResourceType type in resourcesShownDict.Keys)
		{
			if (!resourceDict.ContainsKey(type) || resourcesShownDict[type].amount > resourceDict[type])
			{
				hasEnough = false;
				return;
			}
		}

		hasEnough = true;
	}

	public void HideUtilityCostDisplay()
	{
		resourcesShownDict.Clear();
		usedResources.Clear();
		hasEnough = true;
		inventoryCount = 0;

		for (int i = 0; i < resourceList.Count; i++)
			resourceList[i].gameObject.SetActive(false);

		gameObject.SetActive(false);
	}
}
