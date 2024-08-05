using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class UITradeRouteBeginTooltip : MonoBehaviour, IGoldUpdateCheck, ITooltip
{
	[SerializeField]
	private MapWorld world;

	[SerializeField]
	private TMP_Text none, titleNote;

	[SerializeField]
	private Transform costsRect;

	[SerializeField]
	private GameObject beginButton, addGuardButton;

	private List<UIResourceInfoPanel> costsInfo = new();
	private HashSet<ResourceType> /*cantAffordList = new(), */resourceTypeList = new();

	[HideInInspector]
	public bool /*cantAfford, */shaking;
	[HideInInspector]
	public Trader trader;
	[HideInInspector]
	public City startingCity, homeCity;
	[HideInInspector]
	public SingleBuildType typeNeeded;

	//for tweening
	[SerializeField]
	private RectTransform allContents;
	[HideInInspector]
	public bool activeStatus;

	private void Awake()
	{
		transform.localScale = Vector3.zero;
		gameObject.SetActive(false);

		foreach (Transform selection in costsRect)
		{
			if (selection.TryGetComponent(out UIResourceInfoPanel panel))
			{
				costsInfo.Add(panel);
			}
		}
	}

	//public void HandleEsc()
	//{
	//	if (activeStatus)
	//		world.CloseTradeRouteBeginTooltipCloseButton();
	//}

	public void HandleSpace()
	{
		if (activeStatus)
			world.unitMovement.BeginTradeRoute();
	}

	public void ToggleVisibility(bool val, bool confirm = false, Trader trader = null)
	{
		if (activeStatus == val)
			return;

		LeanTween.cancel(gameObject);

		if (val)
		{
			this.trader = trader;
			startingCity = trader.GetStartingCity();
			titleNote.text = "(Taken from " + startingCity.cityName + ")";
			homeCity = world.GetCity(trader.homeCity);

			ToggleAddGuard(!trader.guarded);

			if (trader.buildDataSO.singleBuildType == SingleBuildType.TradeDepot)
				typeNeeded = SingleBuildType.Barracks;
			else if (trader.buildDataSO.singleBuildType == SingleBuildType.Harbor)
				typeNeeded = SingleBuildType.Shipyard;
			else
				typeNeeded = SingleBuildType.AirBase;
			
			SetResourcePanelInfo(costsInfo, trader.ShowRouteCost(), startingCity.resourceManager);

			shaking = false;
			gameObject.SetActive(val);
			activeStatus = true;
			world.iTooltip = this;
			world.goldUpdateCheck = this;
			LeanTween.scale(allContents, Vector3.one, 0.25f).setEaseLinear();
		}
		else
		{
			//cantAffordList.Clear();
			resourceTypeList.Clear();
			if (!confirm)
				ResetTrader();
			this.trader = null;
			startingCity = null;
			homeCity = null;

			activeStatus = false;
			world.iTooltip = null;
			world.goldUpdateCheck = null;
			LeanTween.scale(allContents, Vector3.zero, 0.25f).setOnComplete(SetActiveStatusFalse);
		}
	}

	private void SetActiveStatusFalse()
	{
		gameObject.SetActive(false);
		if (world.iTooltip == null)
			world.infoPopUpCanvas.gameObject.SetActive(false);
	}

	private void SetResourcePanelInfo(List<UIResourceInfoPanel> panelList, List<ResourceValue> resourceList, ResourceManager manager)
	{
		int resourcesCount = resourceList.Count;

		//show text if blank
		if (resourcesCount == 0)
			none.gameObject.SetActive(true);
		else
			none.gameObject.SetActive(false);

		//cantAfford = false;
		for (int i = 0; i < panelList.Count; i++)
		{
			if (i >= resourcesCount)
			{
				panelList[i].gameObject.SetActive(false);
			}
			else
			{
				resourceTypeList.Add(resourceList[i].resourceType);
				panelList[i].gameObject.SetActive(true);
				panelList[i].SetResourceAmount(resourceList[i].resourceAmount);
				panelList[i].SetResourceType(resourceList[i].resourceType);
				panelList[i].resourceImage.sprite = ResourceHolder.Instance.GetIcon(resourceList[i].resourceType);

				if (resourceList[i].resourceType == ResourceType.Gold)
				{
					if (world.CheckWorldGold(resourceList[i].resourceAmount))
					{
						panelList[i].resourceAmountText.color = Color.white;
						panelList[i].red = false;
					}
					else
					{
						panelList[i].resourceAmountText.color = Color.red;
						panelList[i].red = true;
						//cantAfford = true;
						//cantAffordList.Add(resourceList[i].resourceType);
					}

					continue;
				}

				if (!manager.CheckResourceAvailability(resourceList[i]))
				{
					panelList[i].resourceAmountText.color = Color.red;
					panelList[i].red = true;
					//cantAfford = true;
					//cantAffordList.Add(resourceList[i].resourceType);
				}
				else
				{
					panelList[i].resourceAmountText.color = Color.white;
					panelList[i].red = false;
				}
			}
		}
	}

	//public bool CityCheck(City city)
	//{
	//	return activeStatus && this.startingCity == city;
	//}

	public void ToggleAddGuard(bool v)
	{
		addGuardButton.SetActive(v);
		int height = v ? 370 : 300;
		allContents.sizeDelta = new Vector2(490, height);
	}

	//for interface
	public void UpdateGold(int prevAmount, int amount, bool pos)
	{
		UpdateRouteCost(amount, ResourceType.Gold);
	}

	public void UpdateRouteCost(int amount, ResourceType type)
	{
		List<ResourceValue> resourceList = trader.totalRouteCosts;
		//bool tempCantAfford = false;

		for (int i = 0; i < costsInfo.Count; i++)
		{
			if (i >= resourceList.Count || type != costsInfo[i].resourceType)
				continue;

			if (costsInfo[i].red)
			{
				if (amount >= resourceList[i].resourceAmount)
				{
					costsInfo[i].resourceAmountText.color = Color.white;
					costsInfo[i].red = false;
				}
			}
			else
			{
				if (amount < resourceList[i].resourceAmount)
				{
					costsInfo[i].resourceAmountText.color = Color.red;
					costsInfo[i].red = true;
				}
				//tempCantAfford = true;
			}

			break;
		}

		//if (cantAffordList.Contains(type))
		//{
		//	if (!tempCantAfford)
		//		cantAffordList.Remove(type);
		//}
		//else
		//{
		//	if (tempCantAfford)
		//		cantAffordList.Add(type);
		//}

		//if (cantAffordList.Count == 0)
		//	cantAfford = false;
		//else
		//	cantAfford = true;
	}

	public void AssignGuard()
	{
		addGuardButton.GetComponent<UITooltipTrigger>().CancelCall();
		UITooltipSystem.Hide();
		if (!activeStatus)
			return;

		if (trader.isMoving)
		{
			UIInfoPopUpHandler.WarningMessage().Create(addGuardButton.transform.position, "Must be at home to assign guard", false);
			return;
		}
		else if (!homeCity.singleBuildDict.ContainsKey(typeNeeded))
		{
			string singleBuildType = Regex.Replace(typeNeeded.ToString(), "((?<=[a-z])[A-Z]|[A-Z](?=[a-z]))", " $1");
			UIInfoPopUpHandler.WarningMessage().Create(addGuardButton.transform.position, "Need " + singleBuildType + " in home city", false);
			return;
		}
		
		CityImprovement improvement = world.GetCityDevelopment(homeCity.singleBuildDict[typeNeeded]);
		
		if (!improvement.army.atHome)
		{
			UIInfoPopUpHandler.WarningMessage().Create(addGuardButton.transform.position, "Military not at home", false);
			return;
		}
		else if (improvement.army.defending)
		{
			UIInfoPopUpHandler.WarningMessage().Create(addGuardButton.transform.position, "Military defending", false);
			return;
		}
		else if (improvement.army.armyCount == 0)
		{
			UIInfoPopUpHandler.WarningMessage().Create(addGuardButton.transform.position, "No units stationed here", false);
			return;
		}

		world.cityBuilderManager.PlaySelectAudio();
		world.unitMovement.AssignGuard(improvement.army);
	}

	public bool MilitaryLocCheck(Vector3Int loc)
	{
		return homeCity.singleBuildDict[typeNeeded] == loc;
	}

	public void UnselectArmy()
	{
		world.GetCityDevelopment(homeCity.singleBuildDict[typeNeeded]).army.UnSoftSelectArmy();
	}

	public void ResetTrader()
	{
		trader.guarded = false;
		trader.guardUnit = null;
	}

	public void UpdateGuardCosts()
	{
		//cantAffordList.Clear();
		SetResourcePanelInfo(costsInfo, trader.ShowRouteCost(), homeCity.resourceManager);
	}

	public bool AffordCheck()
	{
		for (int i = 0; i < costsInfo.Count; i++)
		{
			if (!costsInfo[i].gameObject.activeSelf)
				continue;

			if (costsInfo[i].resourceType == ResourceType.Gold)
			{
				if (!world.CheckWorldGold(costsInfo[i].amount))
				{
					StartShaking();
					UIInfoPopUpHandler.WarningMessage().Create(beginButton.transform.position, "Can't afford", false);
					return false;
				}
			}
			else if (startingCity.resourceManager.resourceDict[costsInfo[i].resourceType] < costsInfo[i].amount)
			{
				StartShaking();
				UIInfoPopUpHandler.WarningMessage().Create(beginButton.transform.position, "Can't afford", false);
				return false;
			}
		}

		//if (cantAfford)
		//{
		//	StartShaking();
		//	UIInfoPopUpHandler.WarningMessage().Create(beginButton.transform.position, "Can't afford", false);
		//	return false;
		//}

		return true;
	}

	private void StartShaking()
	{
		if (!shaking)
			StartCoroutine(Shake());
	}

	private IEnumerator Shake()
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
		if (startingCity == city && resourceTypeList.Contains(type))
			UpdateRouteCost(amount, type);
	}
}
