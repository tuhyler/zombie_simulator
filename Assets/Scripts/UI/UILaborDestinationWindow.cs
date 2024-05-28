using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UILaborDestinationWindow : MonoBehaviour, IGoldUpdateCheck, ITooltip
{
	[SerializeField]
	private MapWorld world;

	[SerializeField]
	private GameObject transferAmountHolder, confirmButton, costsHolder;

	[SerializeField]
	private TMP_Text title, transferAmountText;

	[SerializeField]
	private Button increaseAmount, decreaseAmount;

	[SerializeField]
	public TMP_Dropdown destinationDropdown;

	[SerializeField]
	private Transform costsRect;

	[HideInInspector]
	public int transferAmount;

	[HideInInspector]
	public City city;
	private SingleBuildType buildType = SingleBuildType.None;

	private List<UIResourceInfoPanel> costsInfo = new();
	private HashSet<ResourceType> cantAffordList = new(), resourceTypeList = new();
	private List<ResourceValue> costResourceList = new();
	private Dictionary<string, int> transferCostDict = new();
	private Dictionary<string, bool> transferSeaDict = new();

	[SerializeField] //for tweening
	private RectTransform allContents;
	[HideInInspector]
	public bool activeStatus, cantAfford, isLabor, shaking;
	List<string> destinationList = new();
	List<string> initialOption = new() { "Select..." };

	private void Awake()
	{
		gameObject.SetActive(false);

		foreach (Transform selection in costsRect)
		{
			if (selection.TryGetComponent(out UIResourceInfoPanel panel))
				costsInfo.Add(panel);
		}
	}

	public void HandleSpace()
	{
		if (activeStatus && destinationDropdown.value != 0)
			ConfirmDestination();
	}

	public void ToggleVisibility(bool v, bool isLabor = false, City city = null, Unit unit = null)
	{
		if (activeStatus == v)
			return;

		LeanTween.cancel(gameObject);

		if (v)
		{
			transferAmount = 1;
			this.city = city;
			this.isLabor = isLabor;
			world.infoPopUpCanvas.gameObject.SetActive(true);
			world.iTooltip = this;
			world.goldUpdateCheck = this;
			float moveSpeed = 0.5f;
			costsHolder.SetActive(false);
			bool bySea = false;
			bool byAir = false;

			if (this.isLabor)
			{
				transferAmountHolder.SetActive(true);
				decreaseAmount.interactable = false;
				increaseAmount.interactable = true;
				allContents.sizeDelta = new Vector2(370, 290);
				transferAmountText.text = transferAmount.ToString();
				costResourceList = new(world.laborTransferCost);
				title.text = "Transfer Labor";
			}
			else
			{
				title.text = "Transfer Unit";
				transferAmountHolder.SetActive(false);
				allContents.sizeDelta = new Vector2(370, 185);
				costResourceList = new(unit.buildDataSO.cycleCost);
				buildType = unit.buildDataSO.singleBuildType;
				moveSpeed = unit.buildDataSO.movementSpeed;
				bySea = unit.bySea;
				byAir = unit.byAir;
			}
			
			(List<string> tempDestinationList, List<int> distances, List<bool> atSea) = world.GetConnectedCityNamesAndDistances(city, !isLabor, bySea, byAir, buildType);
			destinationList = tempDestinationList;

			for (int i = 0; i < tempDestinationList.Count; i++)
			{
				transferCostDict[tempDestinationList[i]] = Mathf.CeilToInt((distances[i] * 2) / (moveSpeed * 8)); //8 instead of 24 since only counting entire tiles
				transferSeaDict[tempDestinationList[i]] = atSea[i];
			}
			for (int i = 0; i < costsInfo.Count; i++)
				costsInfo[i].gameObject.SetActive(false);
			destinationDropdown.AddOptions(destinationList);
			gameObject.SetActive(true);
			activeStatus = true;
			allContents.localScale = Vector3.zero;
			LeanTween.scale(allContents, Vector3.one, 0.25f).setEase(LeanTweenType.easeOutSine);
		}
		else
		{
			this.city = null;
			world.iTooltip = null;
			world.goldUpdateCheck = null;
			world.changingCity = false;
			costResourceList.Clear();
			destinationList.Clear();
			transferCostDict.Clear();
			transferSeaDict.Clear();
			resourceTypeList.Clear();
			cantAffordList.Clear();
			activeStatus = false;
			destinationDropdown.ClearOptions();
			destinationDropdown.AddOptions(initialOption);
			LeanTween.scale(allContents, Vector3.zero, 0.25f).setOnComplete(SetActiveStatusFalse);
		}
	}

	public void SetActiveStatusFalse()
	{
		gameObject.SetActive(false);
		if (world.iTooltip == null)
			world.infoPopUpCanvas.gameObject.SetActive(false);
	}

	public void DecreasePopCount()
	{
		if (transferAmount > 0)
		{
			if (transferAmount == 9)
				increaseAmount.interactable = true;
			transferAmount--;

			if (transferAmount == 1)
				decreaseAmount.interactable = false;

			transferAmountText.text = transferAmount.ToString();

			if (destinationDropdown.value != 0)
				ShowAllCostData();
		}
	}

	public void IncreasePopCount()
	{
		if (transferAmount < 10)
		{
			if (transferAmount == 1)
				decreaseAmount.interactable = true;
			transferAmount++;

			if (transferAmount == 9)
				increaseAmount.interactable = false;

			transferAmountText.text = transferAmount.ToString();

			if (destinationDropdown.value != 0)
				ShowAllCostData();
		}
	}

	private void SetResourcePanelInfo(ResourceManager manager, int distance)
	{
		int resourcesCount = costResourceList.Count;

		cantAfford = false;
		for (int i = 0; i < costsInfo.Count; i++)
		{
			if (i >= resourcesCount)
			{
				costsInfo[i].gameObject.SetActive(false);
			}
			else
			{
				ResourceType type = costResourceList[i].resourceType;
				resourceTypeList.Add(type);
				
				costsInfo[i].gameObject.SetActive(true);
				costsInfo[i].SetResourceType(type);
				costsInfo[i].resourceImage.sprite = ResourceHolder.Instance.GetIcon(type);
				int amount = costResourceList[i].resourceAmount * transferAmount;

				if (type == ResourceType.Labor)
				{
					costsInfo[i].SetResourceAmount(amount);
					if (manager.city.currentPop < amount)
					{
						costsInfo[i].resourceAmountText.color = Color.red;
						cantAfford = true;
						cantAffordList.Add(costResourceList[i].resourceType);
					}
					else
					{
						costsInfo[i].resourceAmountText.color = Color.white;
					}
					
					continue;
				}

				amount *= distance;
				costsInfo[i].SetResourceAmount(amount);
				if (type == ResourceType.Gold)
				{
					if (world.CheckWorldGold(amount))
					{
						costsInfo[i].resourceAmountText.color = Color.white;
					}
					else
					{
						costsInfo[i].resourceAmountText.color = Color.red;
						cantAfford = true;
						cantAffordList.Add(type);
					}

					continue;
				}


				if (!manager.CheckResourceAvailability(costResourceList[i].resourceType, amount))
				{
					costsInfo[i].resourceAmountText.color = Color.red;
					cantAfford = true;
					cantAffordList.Add(costResourceList[i].resourceType);
				}
				else
				{
					costsInfo[i].resourceAmountText.color = Color.white;
				}
			}
		}
	}

	public void SetDestination()
	{
		costsHolder.SetActive(true);
		int height = isLabor ? 500 : 395;
		allContents.sizeDelta = new Vector2(370, height);
		SetResourcePanelInfo(city.resourceManager, transferCostDict[destinationList[destinationDropdown.value - 1]]);
	}

	public void ShowAllCostData()
	{
		cantAffordList.Clear();
		SetResourcePanelInfo(city.resourceManager, transferCostDict[destinationList[destinationDropdown.value - 1]]);
	}

	public void ConfirmDestination()
	{
		if (destinationDropdown.value == 0)
		{
			ToggleVisibility(false);
		}
		else
		{
			if (AffordCheck())
			{
				string chosenDestination = destinationList[destinationDropdown.value - 1];

				if (!world.TradeStopNameExists(chosenDestination))
				{
					UIInfoPopUpHandler.WarningMessage().Create(confirmButton.transform.position, "Selected location is gone", false);
					return; 
				}
				
				if (isLabor)
				{
					world.cityBuilderManager.TransferLaborPrep(chosenDestination, transferAmount, transferSeaDict[chosenDestination]);
				}
				else
				{	
					City newCity = world.GetCity(world.GetStopMainLocation(chosenDestination));
					CityImprovement dest = world.GetCityDevelopment(newCity.singleBuildDict[buildType]);

					if (!dest.army.atHome || dest.army.defending || dest.army.isFull)
					{
						UIInfoPopUpHandler.WarningMessage().Create(confirmButton.transform.position, "Selected location can't take transfers now", false);
						return;
					}
					else if (newCity.attacked)
					{
						UIInfoPopUpHandler.WarningMessage().Create(confirmButton.transform.position, "Selected location is currently attacked", false);
						return;
					}

					world.unitMovement.TransferMilitaryUnit(newCity, transferSeaDict[chosenDestination]);
				}

				List<ResourceValue> costs = new();
				for (int i = 0; i < costResourceList.Count; i++)
				{
					if (costResourceList[i].resourceType == ResourceType.Labor)
						continue;
					
					ResourceValue newResource = costResourceList[i];
					newResource.resourceAmount = costResourceList[i].resourceAmount * transferCostDict[chosenDestination] * transferAmount;
					costs.Add(newResource);
				}

				city.resourceManager.ConsumeMaintenanceResources(costs, city.cityLoc, !isLabor, !isLabor);
				world.cityBuilderManager.PlaySelectAudio();
				ToggleVisibility(false);
			}
		}
	}

	public void CloseWindowButton()
	{
		if (!isLabor)
		{
			world.unitMovement.uiJoinCity.ToggleVisibility(true);
			//world.unitMovement.uiSwapPosition.ToggleVisibility(true);
			world.unitMovement.uiDeployArmy.ToggleVisibility(true);
			world.unitMovement.uiChangeCity.ToggleVisibility(true);
		}

		ToggleVisibility(false);
	}

	public void UpdateGold(int prevAmount, int currentAmount, bool pos)
	{
		if (destinationDropdown.value != 0 && resourceTypeList.Contains(ResourceType.Gold))
			UpdateResource(currentAmount, ResourceType.Gold, transferCostDict[destinationList[destinationDropdown.value - 1]]);
	}

	public void UpdateResource(int amount, ResourceType type, int distance)
	{
		bool tempCantAfford = false;

		for (int i = 0; i < costsInfo.Count; i++)
		{
			if (i >= costResourceList.Count || type != costsInfo[i].resourceType)
				continue;

			if (amount >= costResourceList[i].resourceAmount * distance * transferAmount)
			{
				costsInfo[i].resourceAmountText.color = Color.white;
			}
			else
			{
				costsInfo[i].resourceAmountText.color = Color.red;
				tempCantAfford = true;
			}

			break;
		}

		if (cantAffordList.Contains(type))
		{
			if (!tempCantAfford)
				cantAffordList.Remove(type);
		}
		else
		{
			if (tempCantAfford)
				cantAffordList.Add(type);
		}

		if (cantAffordList.Count == 0)
			cantAfford = false;
		else
			cantAfford = true;
	}

	public bool AffordCheck()
	{
		if (cantAfford)
		{
			if (!shaking)
				StartCoroutine(Shake());
			UIInfoPopUpHandler.WarningMessage().Create(confirmButton.transform.position, "Can't afford", false);
			return false;
		}

		return true;
	}

	public IEnumerator Shake()
	{
		Vector3 initialPos = transform.localPosition;
		float elapsedTime = 0f;
		float duration = 0.2f;
		shaking = true;

		while (elapsedTime < duration)
		{
			elapsedTime += Time.deltaTime;
			transform.localPosition = initialPos + (Random.insideUnitSphere * 10f);
			yield return null;
		}

		shaking = false;
		transform.localPosition = initialPos;
	}

	public void CheckResource(City city, int amount, ResourceType type)
	{
		if (this.city == city && destinationDropdown.value != 0 && resourceTypeList.Contains(type))
			UpdateResource(amount, type, transferCostDict[destinationList[destinationDropdown.value - 1]]);
	}
}
