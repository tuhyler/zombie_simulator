using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.UI;

public class UICityUpgradePanel : MonoBehaviour
{
    public MapWorld world;
    public CityBuilderManager cityBuilderManager;
    public TMP_Text title, level, producesTitle, health, speed, strength, descriptionTitle, descriptionText, workEthicText, housingText, waterText;
    //private TMP_Text producesText;
    public Image mainImage, strengthImage, waterImage;
    public Sprite inventorySprite, strengthSprite, waterSprite, powerSprite, purchaseAmountSprite;
    public GameObject descriptionHolder, produceHolder, confirmButton, workEthicImage, housingImage;
    public RectTransform allContents, resourceProduceAllHolder, imageLine, resourceProducedHolder, resourceCostHolder, unitInfo, spaceHolder, cityStatsDescription;
    public VerticalLayoutGroup resourceProduceLayout;
	public HorizontalLayoutGroup firstResourceProduceLayout;
	private List<Transform> produceConsumesHolders = new();
    private List<UIResourceInfoPanel> costsInfo = new(), producesInfo = new();
    private List<List<UIResourceInfoPanel>> consumesInfo = new();
    private GameObject firstArrow;

    private Vector3Int improvementLoc;
    private ResourceManager resourceManager;
    private CityImprovement improvement;
    [HideInInspector]
    public Unit unit;
    private bool /*cantAfford, */shaking;
    private List<ResourceValue> upgradeCost = new(), refundCost = new();
    private HashSet<ResourceType> /*cantAffordList = new(), */resourceTypeList = new();
    List<List<ResourceValue>> consumes = new();
    List<int> produceTime = new();

    //for tweening
    [HideInInspector]
    public bool activeStatus;

	private void Awake()
    {
        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);

        foreach (Transform selection in resourceProducedHolder)
            produceConsumesHolders.Add(selection);

        for (int i = 0; i < produceConsumesHolders.Count; i++)
        {
            int j = 0;
            List<UIResourceInfoPanel> consumesPanels = new();

            foreach (Transform selection in produceConsumesHolders[i])
            {
				if (i == 0 && j == 1)
					firstArrow = selection.gameObject; //getting first arrow to hide for units

				//first one is for showing produces
				if (j == 0)
                    producesInfo.Add(selection.GetComponent<UIResourceInfoPanel>());
                //after first two is for showing consumes
                else if (j >= 2 /*&& j <= 7*/)
                    consumesPanels.Add(selection.GetComponent<UIResourceInfoPanel>());

                j++;
            }

            consumesInfo.Add(consumesPanels);
        }
        foreach (Transform selection in resourceCostHolder)
        {
            costsInfo.Add(selection.GetComponent<UIResourceInfoPanel>());
        }
    }

    //public void HandleEsc()
    //{
    //    if (activeStatus)
    //        CloseWindow();
    //}

    public void HandleSpace()
    {
        if (activeStatus)
            OnPointerClick();
    }

    public void ToggleVisibility(bool v, ResourceManager resourceManager = null, CityImprovement improvement = null, Unit unit = null)
    {
        if (!activeStatus && !v) //this is different so as to easily switch between different objects
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            string name;
            string nameAndLevel;
            world.infoPopUpCanvas.gameObject.SetActive(true);
            activeStatus = true;
            
            this.improvement = improvement;
            this.resourceManager = resourceManager;
            if (improvement != null)
            {
                improvementLoc = improvement.loc;
                name = improvement.GetImprovementData.improvementName;
                nameAndLevel = improvement.GetImprovementData.improvementNameAndLevel;
            }
            else
            {
                name = unit.buildDataSO.unitType.ToString();
                nameAndLevel = unit.buildDataSO.unitNameAndLevel;
                if (unit == world.azai)
                {
                    name = "Azai";
                    nameAndLevel = "Azai-" + unit.buildDataSO.unitLevel;
                }
			}

            this.unit = unit;

            string upgradeNameAndLevel = name + "-" + world.GetUpgradeableObjectMaxLevel(name);
            (upgradeCost, refundCost) = world.CalculateUpgradeCost(nameAndLevel, upgradeNameAndLevel, improvement == null);

            if (unit != null)
            {
                List<ResourceValue> produces = new();
                
                if (unit.buildDataSO.cycleCost.Count > 0)
                {
                    ResourceValue value;
                    value.resourceType = ResourceType.None;
                    value.resourceAmount = 0;
                    produces.Add(value);
                    consumes.Add(new(unit.buildDataSO.cycleCost));
                }

                UnitBuildDataSO unitData = UpgradeableObjectHolder.Instance.unitDict[upgradeNameAndLevel];
                List<int> produceTime = new() { 0 };
                SetInfo(unitData.image, unitData.unitType.ToString(), unitData.unitDisplayName, unitData.unitLevel, 0, 0, 0, 0, 0, unitData.unitDescription,
                produces, consumes, produceTime, true, unitData.health, unitData.movementSpeed, unitData.baseAttackStrength, unitData.cargoCapacity);
            }
            else
            {
                ImprovementDataSO improvementData = UpgradeableObjectHolder.Instance.improvementDict[upgradeNameAndLevel];

                consumes.Add(new(improvementData.consumedResources));
                if (improvementData.consumedResources1.Count > 0)
                    consumes.Add(new(improvementData.consumedResources1));
                if (improvementData.consumedResources2.Count > 0)
                    consumes.Add(new(improvementData.consumedResources2));
                if (improvementData.consumedResources3.Count > 0)
                    consumes.Add(new(improvementData.consumedResources3));
                if (improvementData.consumedResources4.Count > 0)
                    consumes.Add(new(improvementData.consumedResources4));

                float workEthic = improvementData.workEthicChange - improvement.GetImprovementData.workEthicChange;
                int housing = improvementData.housingIncrease - improvement.GetImprovementData.housingIncrease;
                int water = improvementData.waterIncrease - improvement.GetImprovementData.waterIncrease;
                int power = improvementData.powerIncrease - improvement.GetImprovementData.powerIncrease;
                float purchaseAmount = improvementData.purchaseAmountChange - improvement.GetImprovementData.purchaseAmountChange;

                SetInfo(improvementData.image, improvementData.improvementName, improvementData.improvementDisplayName, improvementData.improvementLevel, workEthic, purchaseAmount, housing, 
                    water, power, improvementData.improvementDescription, improvementData.producedResources, consumes, improvementData.producedResourceTime, false, 0, 0, 0, 0, 
                    improvementData.rawResourceType == RawResourceType.Rocks, improvementData.cityBonus);
            }

            shaking = false;
            gameObject.SetActive(true);

            LeanTween.scale(allContents, Vector3.one, 0.25f).setEaseLinear();
        }
        else
        {
            ResetData();
            LeanTween.scale(allContents, Vector3.zero, 0.25f).setOnComplete(SetActiveStatusFalse);
        }
    }

    public void CurrentImprovementCheck(CityImprovement improvement)
    {
        if (activeStatus)
        {
            if (improvement == this.improvement)
                ToggleVisibility(false);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    private void ResetData()
    {
        activeStatus = false;
        improvement = null;
        unit = null;
        resourceManager = null;
        upgradeCost.Clear();
        refundCost.Clear();
        consumes.Clear();
        produceTime.Clear();
        //cantAffordList.Clear();
        resourceTypeList.Clear();
    }

    public void SetInfo(Sprite mainSprite, string title, string displayTitle, int level, float workEthic, float purchaseAmount, int housing, int water, int power, string description, 
        List<ResourceValue> produces, List<List<ResourceValue>> consumes, List<int> produceTimeList, bool unit, int health, float speed, int strength, int cargoCapacity, 
        bool rocks = false, bool cityBonus = false)
    {
        mainImage.sprite = mainSprite;
        this.title.text = displayTitle;
        if (title == displayTitle)
            this.level.text = "Level " + level.ToString();
        else
            this.level.text = "Level " + level.ToString() + " " + title;

        int producesCount = produces.Count;
        int maxCount = upgradeCost.Count;
		int maxConsumed = 0;
		int resourcePanelSize = 90;
        resourcePanelSize += 0; //for gap
        int produceHolderWidth = 220;
        int produceHolderHeight = 160;
        int produceContentsWidth = 370;
        int produceContentsHeight = 520;
		bool arrowBuffer = false;
        bool showCityStatsDesc = false;

        producesTitle.text = "Produces / Requires";

        if (workEthic != 0)
        {
			workEthicImage.SetActive(true);
			workEthicText.gameObject.SetActive(true);
			string prefix = "+";
			workEthicText.color = Color.black;
			if (workEthic < 0)
			{
				prefix = "";
				workEthicText.color = Color.red;
			}
			workEthicText.text = prefix + Mathf.RoundToInt(workEthic * 100).ToString() + "%";
			showCityStatsDesc = true;
		}
        else
        {
			workEthicImage.SetActive(false);
			workEthicText.gameObject.SetActive(false);
		}

        if (housing > 0)
        {
			housingImage.SetActive(true);
			housingText.gameObject.SetActive(true);
			housingText.text = "+" + housing.ToString();
			showCityStatsDesc = true;
		}
        else
        {
			housingImage.SetActive(false);
			housingText.gameObject.SetActive(false);
		}


		if (water > 0 || power > 0 || purchaseAmount > 0)
        {
			bool isWater = water > 0;
			bool isPurchaseAmount = purchaseAmount > 0;
			if (isPurchaseAmount)
				waterImage.sprite = purchaseAmountSprite;
			else
				waterImage.sprite = isWater ? waterSprite : powerSprite;
			waterImage.gameObject.SetActive(true);
			waterText.gameObject.SetActive(true);
			if (isPurchaseAmount)
			{
				waterText.text = "+" + purchaseAmount;
				waterText.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 50);
			}
			else
			{
				waterText.text = isWater ? "+" + water : "+" + power;
				waterText.GetComponent<RectTransform>().sizeDelta = new Vector2(55, 50);
			}
			showCityStatsDesc = true;
		}
        else
        {
			waterImage.gameObject.SetActive(false);
			waterText.gameObject.SetActive(false);
		}

		cityStatsDescription.gameObject.SetActive(showCityStatsDesc);

        if (description.Length > 0 || unit || showCityStatsDesc)
        {
			descriptionHolder.SetActive(true);
			descriptionText.gameObject.SetActive(true);

			if (!unit && description.Length < 23)
				produceContentsHeight += 85;
			else if (description.Length > 0)
				produceContentsHeight += 120;

			if (unit)
            {
				descriptionText.text = description;
				producesTitle.text = "Cost per Travel Cycle";
				descriptionTitle.text = "Unit Info";
				this.health.text = health.ToString();
                this.speed.text = Mathf.RoundToInt(speed * 2).ToString();
                if (cargoCapacity > 0)
                {
                    strengthImage.sprite = inventorySprite;
                    this.strength.text = cargoCapacity.ToString();
                }
                else
                {
                    this.strength.text = strength.ToString();
                    strengthImage.sprite = strengthSprite;
                }

				Vector3 localPosition = unitInfo.localPosition;
				if (description.Length == 0)
				{
					produceContentsHeight += 40;
					localPosition.y = 12.5f;
					unitInfo.localPosition = localPosition;
				}
				else
				{
					localPosition.y = -65;
					unitInfo.localPosition = localPosition;
				}

				unitInfo.gameObject.SetActive(true);
				produceContentsHeight += 50;
				resourceProduceLayout.childAlignment = TextAnchor.UpperCenter;
			}
            else
            {
				unitInfo.gameObject.SetActive(false);

				if (showCityStatsDesc)
                {
					descriptionTitle.text = "Benefits";
					producesTitle.text = "Cost per Cycle";
					produceContentsHeight += 40;
					descriptionText.gameObject.SetActive(false);
				}
                else
                {
				    descriptionTitle.text = "Additional Info";
				    descriptionText.text = description;
				    producesTitle.text = "Produces";
                }
			}
        }
        else
        {
			descriptionHolder.SetActive(false);
			unitInfo.gameObject.SetActive(false);
		}

		if (unit)
		{
			unitInfo.gameObject.SetActive(true);
			produceContentsHeight += 10;
            resourceProduceLayout.childAlignment = TextAnchor.UpperCenter;

			if (consumes.Count > 0)
                firstResourceProduceLayout.padding.left = -(consumes[0].Count - 1) * (resourcePanelSize / 2);
		}
        else if (cityBonus)
        {
            resourceProduceLayout.childAlignment = TextAnchor.UpperCenter;
			
            if (consumes.Count > 0)
				firstResourceProduceLayout.padding.left = -(consumes[0].Count - 1) * (resourcePanelSize / 2);
		}
		else
		{
			unitInfo.gameObject.SetActive(false);
			resourceProduceLayout.childAlignment = TextAnchor.UpperLeft;
			firstResourceProduceLayout.padding.left = 0;
        }

		if (producesCount > 0)
		{
			produceHolder.SetActive(true);
		}
		else
		{
			produceHolder.SetActive(false);
			produceContentsHeight -= 140;
		}

		for (int i = 0; i < produceConsumesHolders.Count; i++)
        {
            if (i >= producesCount)
            {
                produceConsumesHolders[i].gameObject.SetActive(false);
            }
            else
            {
                produceConsumesHolders[i].gameObject.SetActive(true);
                GenerateProduceInfo(produces[i], consumes[i], i, produceTimeList[i], rocks, cityBonus);

				int one = produceTimeList[i] > 0 ? 1 : 0;

				if (maxCount < consumes[i].Count + one + 1)
				{
					arrowBuffer = true;
					maxCount = consumes[i].Count + one + 1;
				}

				if (maxConsumed < consumes[i].Count + one)
					maxConsumed = consumes[i].Count + one;
			}
        }

        GenerateResourceInfo(upgradeCost, costsInfo, 0, true, cityBonus, resourceManager); //0 means don't show produce time

		//adjusting height of panel
		if (producesCount > 1)
			produceContentsHeight += resourcePanelSize * (producesCount - 1);

		//adjusting width of panel
		if (maxCount > 4)
			produceContentsWidth += resourcePanelSize * (maxCount - 4);

		if (maxConsumed > 1)
			produceHolderWidth += resourcePanelSize * (maxConsumed - 1);

		if (arrowBuffer)
			produceContentsWidth += 40;

		resourceProducedHolder.sizeDelta = new Vector2(produceHolderWidth, produceHolderHeight);
		allContents.sizeDelta = new Vector2(produceContentsWidth, produceContentsHeight);
		spaceHolder.sizeDelta = new Vector2(100, Mathf.Max(resourcePanelSize * (producesCount - 1), 0));
        imageLine.sizeDelta = new Vector2(produceContentsWidth - 20, 4);

		PositionCheck();
    }

    private void GenerateResourceInfo(List<ResourceValue> resourcesInfo, List<UIResourceInfoPanel> resourcesToShow, int prodTime, bool cost, bool cityBonus, ResourceManager resourceManager = null)
    {
        int resourcesCount = resourcesInfo.Count;
        //cantAfford = false;

        for (int i = 0; i < resourcesToShow.Count; i++)
        {
            if (i > resourcesCount)
            {
                resourcesToShow[i].gameObject.SetActive(false);
            }
            else if (i == resourcesCount) //for adding production time
            {
                if (prodTime > 0 && !cityBonus)
                {
                    resourcesToShow[i].gameObject.SetActive(true);
                    resourcesToShow[i].SetResourceAmount(prodTime);
                    resourcesToShow[i].SetResourceType(ResourceType.Time);
                    resourcesToShow[i].resourceImage.sprite = ResourceHolder.Instance.GetIcon(ResourceType.Time);
					resourcesToShow[i].resourceAmountText.color = Color.white;
                    resourcesToShow[i].red = false;
				}
                else
                {
                    resourcesToShow[i].gameObject.SetActive(false);
					resourcesToShow[i].resourceAmountText.color = Color.white;
                    resourcesToShow[i].red = false;
				}
            }
            else
            {
                resourcesToShow[i].gameObject.SetActive(true);
                resourcesToShow[i].SetResourceAmount(resourcesInfo[i].resourceAmount);
                resourcesToShow[i].resourceImage.sprite = ResourceHolder.Instance.GetIcon(resourcesInfo[i].resourceType);
                resourcesToShow[i].SetResourceType(resourcesInfo[i].resourceType);

                if (cost)
                {
                    resourceTypeList.Add(resourcesInfo[i].resourceType);
                    if (resourceManager.CheckResourceAvailability(resourcesInfo[i]))
                    {
                        resourcesToShow[i].resourceAmountText.color = Color.white;
                        resourcesToShow[i].red = false;
                    }
                    else
                    {
                        resourcesToShow[i].resourceAmountText.color = Color.red;
                        resourcesToShow[i].red = true;
                        //cantAfford = true;
                        //cantAffordList.Add(resourcesInfo[i].resourceType);
                    }
                }
                else
                {
					resourcesToShow[i].resourceAmountText.color = Color.white;
                    resourcesToShow[i].red = false;
				}
            }
        }
    }

    private void GenerateProduceInfo(ResourceValue producedResource, List<ResourceValue> consumedResources, int produceIndex, int produceTime, bool rocks, bool cityBonus)
    {
        if (producedResource.resourceType != ResourceType.None)
        {
            if (produceIndex == 0)
                producesInfo[0].gameObject.SetActive(true);

            producesInfo[produceIndex].SetResourceAmount(producedResource.resourceAmount);
            producesInfo[produceIndex].SetResourceType(producedResource.resourceType);
			if (rocks)
			{
				RocksType rocksType = ResourceHolder.Instance.GetRocksType(producedResource.resourceType);
				Sprite tempImage;

				if (rocksType == RocksType.Normal)
					tempImage = world.rocksNormal;
				else if (rocksType == RocksType.Luxury)
					tempImage = world.rocksLuxury;
				else
					tempImage = world.rocksChemical;

				producesInfo[produceIndex].resourceImage.sprite = tempImage;
                producesInfo[produceIndex].SetMessage(rocksType);
			}
            else
            {
    			producesInfo[produceIndex].resourceImage.sprite = ResourceHolder.Instance.GetIcon(producedResource.resourceType);
            }

			firstArrow.SetActive(true);
			GenerateResourceInfo(consumedResources, consumesInfo[produceIndex], produceTime, false, cityBonus);
        }
        else
        {
            producesInfo[produceIndex].gameObject.SetActive(false);
            firstArrow.SetActive(false);
            GenerateResourceInfo(consumedResources, consumesInfo[produceIndex], produceTime, false, cityBonus);
		}
    }

    private void PositionCheck()
    {
        Vector3 p = Input.mousePosition;
        p.z = 935;
        //p.z = 1;
        allContents.pivot = new Vector2(p.x / Screen.width, p.y / Screen.height);

        Vector3 pos = Camera.main.ScreenToWorldPoint(p);
        allContents.transform.position = pos;
    }


    public void OnPointerClick()
    {
        if (cityBuilderManager.SelectedCity.attacked)
        {
			ShakeCheck();
			UIInfoPopUpHandler.WarningMessage().Create(confirmButton.transform.position, "Can't upgrade now, enemy approaching", false);
			return;
		}
        
        if (!AffordCheck()/*&& !cityBuilderManager.isQueueing*/)
        {
			ShakeCheck();
			UIInfoPopUpHandler.WarningMessage().Create(confirmButton.transform.position, "Can't afford", false);
            return;
        }
            
        if (improvement != null)
        {
            cityBuilderManager.UpgradeSelectedImprovementQueueCheck(improvementLoc, improvement, new(upgradeCost), new(refundCost));
        }
        else if (unit != null)
        {
            cityBuilderManager.UpgradeUnit(unit, new(upgradeCost), new(refundCost));
            cityBuilderManager.PlaySelectAudio(cityBuilderManager.buildClip);
        }

        ResetData();
        gameObject.SetActive(false);
        world.infoPopUpCanvas.gameObject.SetActive(false);
    }

    private bool AffordCheck()
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
			else if (resourceManager.resourceDict[costsInfo[i].resourceType] < costsInfo[i].amount)
			{
				return false;
			}
		}

		return true;
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

    public void ResourceCheck(int amount, ResourceType type)
    {
        if (resourceTypeList.Contains(type))
            UpdateResource(amount, type);
    }

	private void UpdateResource(int amount, ResourceType type)
	{
		
        //bool tempCantAfford = false;

		for (int i = 0; i < costsInfo.Count; i++)
		{
			if (i >= upgradeCost.Count || type != costsInfo[i].resourceType)
				continue;

			if (costsInfo[i].red)
			{
				if (amount >= upgradeCost[i].resourceAmount)
				{
					costsInfo[i].resourceAmountText.color = Color.white;
					costsInfo[i].red = false;
				}
			}
			else
			{
				if (amount < upgradeCost[i].resourceAmount)
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

	//public void CheckCosts(ResourceManager resourceManager)
 //   {
 //       bool cannotAffordTemp = false;
        
 //       int i = 0;
 //       foreach (ResourceValue value in upgradeCost)
 //       {
 //           if (resourceManager.CheckResourceAvailability(value))
 //           {
 //               costsInfo[i].resourceAmountText.color = Color.white;
 //           }
 //           else
 //           {
 //               cannotAffordTemp = true;
 //               costsInfo[i].resourceAmountText.color = Color.red;
 //           }

 //           i++;

 //       }

 //       cannotAfford = cannotAffordTemp;
 //   }

    public void CloseWindow()
    {
        cityBuilderManager.PlayCloseAudio();
        ToggleVisibility(false);
    }
}
