using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICityPopIncreasePanel : MonoBehaviour
{
	[SerializeField]
	private MapWorld world;

	[SerializeField]
	private GameObject foodCostGO, increaseButton, popIncreaseHolder;

	[SerializeField]
	private TMP_Text foodCostText, foodCycleCostText, housingCostText, waterCostText, popCountText;

	[SerializeField]
	private Button increasePop, decreasePop;

	[SerializeField]
	public Image buttonImage;

	private City city;

	private int foodCost, foodCycleCost, housingCost, waterCost, amount;
	private ResourceValue food;
	private Color originalButtonColor;

	private bool cantAfford, needWater, needHousing;
	
	//for tweening
	[SerializeField]
	private RectTransform allContents;
	[HideInInspector]
	public bool activeStatus, isFlashing;

	private void Awake()
	{
		originalButtonColor = buttonImage.color;
		gameObject.SetActive(false);
	}

	public void HandleSpace()
	{
		if (activeStatus)
			IncreasePop();
	}

	public void ToggleVisibility(bool val, int amount = 0, City city = null, bool joinCity = false, bool isTrader = false) //trader to charge food when joining city
	{
		if (activeStatus == val)
			return;

		LeanTween.cancel(gameObject);

		if (val)
		{
			this.city = city;
			this.amount = amount;
			if (joinCity)
			{
				popIncreaseHolder.SetActive(false);
				allContents.sizeDelta = new Vector2(370, 400);
			}
			else
			{
				popIncreaseHolder.SetActive(true);
				decreasePop.interactable = false;
				increasePop.interactable = true;
				popCountText.text = amount.ToString();
				allContents.sizeDelta = new Vector2(370, 480);
			}

			bool hideFoodCost = joinCity;
			if (isTrader)
				hideFoodCost = false;
			SetCosts(city);
			SetCostPanelInfo(city, hideFoodCost);
			ToggleColor(true);

			world.infoPopUpCanvas.gameObject.SetActive(true);
			gameObject.SetActive(val);
			activeStatus = true;
			LeanTween.scale(allContents, Vector3.one, 0.25f).setEaseLinear();
		}
		else
		{
			this.city = null;
			activeStatus = false;
			ToggleColor(false);
			LeanTween.scale(allContents, Vector3.zero, 0.25f).setOnComplete(SetActiveStatusFalse);
		}
	}

	private void SetActiveStatusFalse()
	{
		gameObject.SetActive(false);
		world.infoPopUpCanvas.gameObject.SetActive(false);
	}

	//setting this in case cities differ on food costs
	private void SetCosts(City city)
	{
		foodCost = city.growthFood * amount;
		foodCycleCost = city.unitFoodConsumptionPerMinute * amount/*+ (int)city.ResourceManager.resourceConsumedPerMinuteDict[ResourceType.Food]*/;
		housingCost = 1 * amount;
		waterCost = 1 * amount;
	}

	private void SetCostPanelInfo(City city, bool hideCost)
	{
		cantAfford = false;

		foodCostText.text = foodCost.ToString();
		foodCycleCostText.text = foodCycleCost.ToString();
		housingCostText.text = housingCost.ToString();
		waterCostText.text = waterCost.ToString();

		if (hideCost)
			foodCostGO.SetActive(false);
		else
			foodCostGO.SetActive(true);

		food.resourceType = ResourceType.Food;
		food.resourceAmount = foodCost;

		if (city.ResourceManager.CheckResourceAvailability(food))
		{
			foodCostText.color = Color.white;
		}
		else
		{
			cantAfford = true;
			foodCostText.color = Color.red;
		}

		if (city.HousingCount < housingCost)
		{
			cantAfford = true;
			needHousing = true;
			housingCostText.color = Color.red;
		}
		else
		{
			needHousing = false;
			housingCostText.color = Color.white;
		}

		if (city.waterCount < waterCost)
		{
			cantAfford = true;
			needWater = true;
			waterCostText.color = Color.red;
		}
		else
		{
			needWater = false;
			waterCostText.color = Color.white;
		}
	}

	public bool CheckCity(City city)
	{
		if (activeStatus && this.city == city)
			return true;

		return false;
	}

	public void UpdateFoodCosts(City city)
	{
		if (city.ResourceManager.CheckResourceAvailability(food))
		{
			foodCostText.color = Color.white;

			if (cantAfford)
			{
				if (city.waterCount >= amount * waterCost && city.HousingCount >= amount * housingCost)
					cantAfford = false;
			}
		}
		else
		{
			cantAfford = true;
			foodCostText.color = Color.red;
		}
	}

	public void UpdateHousingCosts(City city)
	{
		if (city.HousingCount < amount * housingCost)
		{
			cantAfford = true;
			housingCostText.color = Color.red;
		}
		else
		{
			housingCostText.color = Color.white;
		}
	}

	public void UpdateWaterCosts(City city)
	{
		if (city.waterCount < amount * waterCost)
		{
			cantAfford = true;
			waterCostText.color = Color.red;
		}
		else
		{
			waterCostText.color = Color.white;
		}
	}

	public void DecreasePopCount()
	{
		if (amount > 0)
		{
			if (amount == 99)
				increasePop.interactable = true;
			amount--;

			if (amount == 1)
				decreasePop.interactable = false;

			popCountText.text = amount.ToString();
			SetCosts(city);
			SetCostPanelInfo(city, false);
		}
	}

	public void IncreasePopCount()
	{
		if (amount < 100)
		{
			if (amount == 1)
				decreasePop.interactable = true;
			amount++;

			if (amount == 99)
				increasePop.interactable = false;

			popCountText.text = amount.ToString();
			SetCosts(city);
			SetCostPanelInfo(city, false);
		}
	}

	public void IncreasePop()
	{
		if (AffordCheck())
		{
			if (world.tutorialGoing)
				world.TutorialCheck("Add Pop");
			
			if (city.world.unitMovement.selectedUnit != null)
				city.world.unitMovement.JoinCityConfirm(city);
			else
				city.PopulationGrowthCheck(false, amount);

			ToggleVisibility(false);
		}
	}

	public void ToggleColor(bool v)
	{
		if (v)
		{
			if (isFlashing)
			{
				isFlashing = false;
				world.ButtonFlashCheck();
			}

			buttonImage.color = Color.green;
		}
		else
		{
			buttonImage.color = originalButtonColor;
		}
	}

	public bool AffordCheck()
	{
		if (cantAfford)
		{
			StartCoroutine(Shake());
			if (needWater)
				UIInfoPopUpHandler.WarningMessage().Create(increaseButton.transform.position, "Need water. Build camp with river in radius or build a well.", false);
			else if (needHousing)
				UIInfoPopUpHandler.WarningMessage().Create(increaseButton.transform.position, "Need housing. Build more housing.", false);
			else
				UIInfoPopUpHandler.WarningMessage().Create(increaseButton.transform.position, "Need more food", false);

			return false;
		}

		return true;
	}

	public IEnumerator Shake()
	{
		Vector3 initialPos = transform.localPosition;
		float elapsedTime = 0f;
		float duration = 0.2f;

		while (elapsedTime < duration)
		{
			elapsedTime += Time.deltaTime;
			transform.localPosition = initialPos + (Random.insideUnitSphere * 10f);
			yield return null;
		}

		transform.localPosition = initialPos;
	}
}
