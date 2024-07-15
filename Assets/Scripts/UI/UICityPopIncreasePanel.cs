using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICityPopIncreasePanel : MonoBehaviour, ITooltip
{
	[SerializeField]
	private MapWorld world;

	[SerializeField]
	private GameObject foodCostGO, increaseButton, popIncreaseHolder;

	[SerializeField]
	private TMP_Text foodCostText, foodCycleCostText, housingCostText, waterCostText, popCountText;

	[SerializeField]
	public Button increasePop, decreasePop;

	[SerializeField]
	public Image buttonImage;

	private City city;

	private int foodCost, foodCycleCost, housingCost, waterCost, amount;
	private ResourceValue food;
	private Color originalButtonColor;

	private bool shaking;
	
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

			shaking = false;
			world.infoPopUpCanvas.gameObject.SetActive(true);
			world.iTooltip = this;
			gameObject.SetActive(val);
			activeStatus = true;
			LeanTween.scale(allContents, Vector3.one, 0.25f).setEaseLinear();
		}
		else
		{
			this.city = null;
			world.iTooltip = null;
			activeStatus = false;
			ToggleColor(false);
			LeanTween.scale(allContents, Vector3.zero, 0.25f).setOnComplete(SetActiveStatusFalse);
		}
	}

	private void SetActiveStatusFalse()
	{
		gameObject.SetActive(false);
		if (world.iTooltip == null)
			world.infoPopUpCanvas.gameObject.SetActive(false);
	}

	//setting this in case cities differ on food costs
	private void SetCosts(City city)
	{
		foodCost = city.growthFood * amount;
		foodCycleCost = city.unitFoodConsumptionPerMinute * amount;
		housingCost = 1 * amount;
		waterCost = 1 * amount;
	}

	private void SetCostPanelInfo(City city, bool hideCost)
	{
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

		foodCostText.color = city.resourceManager.CheckResourceAvailability(food) ? Color.white : Color.red;
		housingCostText.color = city.HousingCount < housingCost ? Color.red : Color.white;
		waterCostText.color = city.waterCount < waterCost ? Color.red : Color.white;
	}

	public bool CheckCity(City city)
	{
		if (activeStatus && this.city == city)
			return true;

		return false;
	}

	public void UpdateFoodCosts(City city)
	{
		foodCostText.color = city.resourceManager.CheckResourceAvailability(food) ? Color.white : Color.red;
	}

	public void UpdateHousingCosts(City city)
	{
		housingCostText.color = city.HousingCount < housingCost ? Color.red : Color.white;
	}

	public void UpdateWaterCosts(City city)
	{
		waterCostText.color = city.waterCount < waterCost ? Color.red : Color.white;
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
			if (world.tutorial && !GameLoader.Instance.gameData.tutorialData.hadPopAdd)
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
		bool fail = false;
		
		if (!city.resourceManager.CheckResourceAvailability(food))
		{
			UIInfoPopUpHandler.WarningMessage().Create(increaseButton.transform.position, "Need more food", false);
			fail = true;
		}
		else if (city.HousingCount < housingCost)
		{
			UIInfoPopUpHandler.WarningMessage().Create(increaseButton.transform.position, "Need housing. Build more housing.", false);
			fail = true;
		}
		else if (city.waterCount < waterCost)
		{
			UIInfoPopUpHandler.WarningMessage().Create(increaseButton.transform.position, "Need water. Make camp with river in radius or build a well.", false);
			fail = true;
		}

		if (fail)
		{
			ShakeCheck();
			return false;
		}
		else
		{
			return true;
		}
	}

	private void ShakeCheck()
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
		if (this.city == city && type == ResourceType.Food)
			UpdateFoodCosts(city);
	}
}
