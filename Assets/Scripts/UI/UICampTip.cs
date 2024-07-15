using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICampTip : MonoBehaviour, IGoldUpdateCheck, ITooltip
{
	[SerializeField]
	private MapWorld world;

	[SerializeField]
	private Sprite armyInfantry, armyRanged, armyCavalry, armySeige, enemyInfantry, enemyRanged, enemyCavalry, enemySeige;

	[SerializeField]
	private TMP_Text title, costsText, healthText, strengthText, infantryText, rangedText, cavalryText, seigeText, deployedText;

	[SerializeField]
	private Image infantryImage, rangedImage, cavalryImage, seigeImage;

	[SerializeField]
	private Transform costsRect;

	[SerializeField]
	public GameObject infantryHolder, rangedHolder, cavalryHolder, seigeHolder, attackButton, warningText;

	private List<UIResourceInfoPanel> costsInfo = new();
	private HashSet<ResourceType> /*cantAffordList = new(), */resourceTypeList = new();

	[HideInInspector]
	public CityImprovement improvement;
	[HideInInspector]
	public Army army;
	[HideInInspector]
	public EnemyCamp enemyCamp;

	[HideInInspector]
	public bool /*cantAfford, */shaking;
	private int currentWidth;

	//for tweening
	[SerializeField]
	private RectTransform allContents, lineImage;
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
	//		world.unitMovement.CancelArmyDeploymentButton();
	//}

	public void HandleSpace()
	{
		if (activeStatus && army != null)
			world.unitMovement.DeployArmy();
	}

	public void ToggleVisibility(bool val, CityImprovement improvement = null, EnemyCamp enemyCamp = null, Army army = null, bool clearCosts = true)
	{
		if (activeStatus == val)
			return;

		LeanTween.cancel(gameObject);

		if (val)
		{
			if (improvement != null)
			{
				this.improvement = improvement;
				this.improvement.EnableHighlight(Color.white);
				this.army = improvement.city.army;
				SetData(true, this.army.GetArmyCycleCost(), this.army.infantryCount, this.army.rangedCount, this.army.cavalryCount, this.army.seigeCount, this.army.health, this.army.strength);
			}
			else
			{
				this.army = army;
				this.enemyCamp = enemyCamp;
				SetData(false, army.CalculateBattleCost(enemyCamp.strength), enemyCamp.infantryCount, enemyCamp.rangedCount, enemyCamp.cavalryCount, enemyCamp.seigeCount, enemyCamp.health, enemyCamp.strength, enemyCamp.GetLeader());
			}

			gameObject.SetActive(val);
			world.iTooltip = this;
			activeStatus = true;
			if (EnemyScreenActive())
				world.goldUpdateCheck = this;

			//setting up pop up location
			Vector3 p = Input.mousePosition;
			float x = 0.5f;
			float y = 0.5f;

			p.z = 935;
			//p.z = 1;
			if (p.y + allContents.rect.height * 0.5f > Screen.height)
				y = 1f;
			else if (p.y - allContents.rect.height * 0.5f < 0)
				y = 0f;

			if (p.x + allContents.rect.width * 0.5f > Screen.width)
				x = 1f;
			else if (p.x - allContents.rect.width * 0.5f < 0)
				x = 0f;

			allContents.pivot = new Vector2(x, y);
			Vector3 pos = Camera.main.ScreenToWorldPoint(p);
			allContents.transform.position = pos;

			shaking = false;
			if (enemyCamp == null)
				WarningCheck();
			else
				warningText.SetActive(false);
			LeanTween.scale(allContents, Vector3.one, 0.25f).setEaseLinear();
		}
		else
		{
			world.iTooltip = null;
			if (this.improvement != null)
			{
				this.improvement.DisableHighlight();
				this.improvement = null;
			}
			else
			{
				this.enemyCamp = null;
			}

			//cantAffordList.Clear();
			resourceTypeList.Clear();

			if (clearCosts) //only time when this is relevant is when confirming to deploy army somewhere
			{
				this.army.ClearBattleCosts();
				world.unitMovement.HideBattlePath();
			}

			this.army = null;

			activeStatus = false;
			world.goldUpdateCheck = null;
			LeanTween.scale(allContents, Vector3.zero, 0.25f).setOnComplete(SetActiveStatusFalse);
		}
	}

	public void WarningCheck() //in case not enough money to support army next cycle
	{
		if (activeStatus && improvement != null)
		{
			if (improvement.city.army.noMoneyCycles > 0)
			{
				warningText.SetActive(true);
				allContents.sizeDelta = new Vector2(currentWidth, 460);
			}
			else
			{
				warningText.SetActive(false);
				allContents.sizeDelta = new Vector2(currentWidth, 435);
			}
		}
	}

	private void SetActiveStatusFalse()
	{
		gameObject.SetActive(false);
		if (world.iTooltip == null)
			world.infoPopUpCanvas.gameObject.SetActive(false);
	}

	//public bool ArmyScreenActive(Army army)
	//{
	//	return activeStatus && this.army == army;
	//}

	public bool EnemyScreenActive()
	{
		return activeStatus && improvement == null;
	}

	public bool EnemyScreenActiveForArmy(Army army)
	{
		return activeStatus && this.army == army;
	}

	public void RefreshData()
	{
		if (improvement != null)
			SetData(true, army.GetArmyCycleCost(), army.infantryCount, army.rangedCount, army.cavalryCount, army.seigeCount, army.health, army.strength);
		else
			SetData(false, army.CalculateBattleCost(enemyCamp.strength), enemyCamp.infantryCount, enemyCamp.rangedCount, enemyCamp.cavalryCount, enemyCamp.seigeCount, enemyCamp.health, enemyCamp.strength);
	}

	private void SetData(bool isArmy, List<ResourceValue> costs, int infantry, int ranged, int cavalry, int seige, int health, int strength, MilitaryLeader leader = null)
	{
		int totalArmyComp = 0;
		
		if (leader)
		{
			strength += leader.buildDataSO.baseAttackStrength;
			health += leader.buildDataSO.health;

			bool displaceUnit = false;
			foreach (Military unit in enemyCamp.UnitsInCamp)
			{
				if (unit.barracksBunk == leader.barracksBunk)
				{
					displaceUnit = true;
					strength -= unit.buildDataSO.baseAttackStrength;
					health -= unit.buildDataSO.health;
					break;
				}
			}

			if (!displaceUnit)
			{
				if (leader.buildDataSO.unitType == UnitType.Infantry)
					infantry++;
				else if (leader.buildDataSO.unitType == UnitType.Ranged)
					ranged++;
				else if (leader.buildDataSO.unitType == UnitType.Cavalry)
					cavalry++;
			}
		}

		if (infantry == 0)
		{
			infantryHolder.SetActive(false);
		}
		else
		{
			totalArmyComp++;
			infantryHolder.SetActive(true);
			infantryImage.sprite = isArmy ? armyInfantry : enemyInfantry;
			infantryText.text = infantry.ToString();
		}

		if (ranged == 0)
		{
			rangedHolder.SetActive(false);
		}
		else
		{
			totalArmyComp++;
			rangedHolder.SetActive(true);
			rangedImage.sprite = isArmy ? armyRanged : enemyRanged;
			rangedText.text = ranged.ToString();
		}

		if (cavalry == 0)
		{
			cavalryHolder.SetActive(false);
		}
		else
		{
			totalArmyComp++;
			cavalryHolder.SetActive(true);
			cavalryImage.sprite = isArmy ? armyCavalry : enemyCavalry;
			cavalryText.text = cavalry.ToString();
		}

		if (seige == 0)
		{
			seigeHolder.SetActive(false);
		}
		else
		{
			totalArmyComp++;
			seigeHolder.SetActive(true);
			seigeImage.sprite = isArmy ? armySeige : enemySeige;
			seigeText.text = cavalry.ToString();
		}
		
		strengthText.text = strength.ToString();
		healthText.text = health.ToString();
		
		int maxCount = costs.Count;
		if (totalArmyComp < 3)
			totalArmyComp = 3;

		if (totalArmyComp > maxCount)
			maxCount = totalArmyComp;

		if (isArmy)
		{
			title.text = "Barracks";
			SetResourcePanelInfo(costsInfo, costs, true);
			
			costsText.gameObject.SetActive(true);
			costsText.text = "Costs Per Cycle";
			costsRect.gameObject.SetActive(true);
			attackButton.SetActive(false);
		}
		else
		{
			title.text = "Enemy Camp";
			SetResourcePanelInfo(costsInfo, costs, false, army.city.resourceManager);

			costsText.gameObject.SetActive(true);
			costsText.text = "Cost to Attack";
			costsRect.gameObject.SetActive(true);
			attackButton.SetActive(true);
		}

		int multiple = Mathf.Max(maxCount - 2, 0) * 90; //allowing one extra for production time ResourceValue
		int panelWidth = 310 + multiple;
		int panelHeight = isArmy ? 435 : 500;
		int lineWidth = 280 + multiple;

		currentWidth = panelWidth;
		allContents.sizeDelta = new Vector2(panelWidth, panelHeight);
		lineImage.sizeDelta = new Vector2(lineWidth, 4);
	}

	private void SetResourcePanelInfo(List<UIResourceInfoPanel> panelList, List<ResourceValue> resourceList, bool isArmy, ResourceManager manager = null)
	{
		int resourcesCount = resourceList.Count;
		bool showText = false;

		//show text for army
		if (isArmy)
		{
			if (army.atHome)
			{
				if (resourcesCount == 0)
				{
					deployedText.gameObject.SetActive(true);
					deployedText.text = "None";
				}
				else
				{
					deployedText.gameObject.SetActive(false);
				}
			}
			else
			{
				deployedText.gameObject.SetActive(true);
				deployedText.text = "Deployed";
				showText = true;
			}
		}
		//show text for enemy
		else
		{
			if (resourcesCount == 0)
			{
				deployedText.gameObject.SetActive(true);
				deployedText.text = "None";
			}
			else
			{
				deployedText.gameObject.SetActive(false);
			}
		}

		//if deployed, don't show costs
		if (showText)
			resourcesCount = 0;

		//cantAfford = false;
		for (int i = 0; i < panelList.Count; i++)
		{
			if (i >= resourcesCount)
			{
				panelList[i].gameObject.SetActive(false);
			}
			else
			{
				panelList[i].gameObject.SetActive(true);
				resourceTypeList.Add(resourceList[i].resourceType);
				panelList[i].SetResourceAmount(resourceList[i].resourceAmount);
				panelList[i].SetResourceType(resourceList[i].resourceType);
				panelList[i].resourceImage.sprite = ResourceHolder.Instance.GetIcon(resourceList[i].resourceType);

				if (manager)
				{
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
				else
				{
					panelList[i].resourceAmountText.color = Color.white;
				}
			}
		}
	}

	public void UpdateGold(int prevAmount, int amount, bool pos)
	{
		UpdateBattleCostCheck(amount, ResourceType.Gold);
	}

	//seeing if battle can be afforded or not
	public void UpdateBattleCostCheck(int amount, ResourceType type)
	{
		List<ResourceValue> resourceList = army.GetBattleCost();
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

	public bool AffordCheck()
	{
		for (int i = 0; i < costsInfo.Count; i++)
		{
			if (!costsInfo[i].gameObject.activeSelf)
				continue;

			if (costsInfo[i].resourceType == ResourceType.Gold)
			{
				if (!world.CheckWorldGold(costsInfo[i].amount))
					return false;
			}
			else if (army.city.resourceManager.resourceDict[costsInfo[i].resourceType] < costsInfo[i].amount)
			{
				return false;
			}
		}

		return true;
	}

	public void ShakeCheck()
	{
		if (!shaking)
			StartCoroutine(Shake());
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
		if (city.army == this.army && resourceTypeList.Contains(type))
			UpdateBattleCostCheck(amount, type);
	}
}
