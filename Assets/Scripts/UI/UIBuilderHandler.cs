using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System.Resources;
using static UnityEditor.Progress;
using UnityEngine.UIElements;

public class UIBuilderHandler : MonoBehaviour, IGoldUpdateCheck
{
    [HideInInspector]
    public string tabName;
    private ImprovementDataSO buildData;
    private UnitBuildDataSO unitBuildData;
    [HideInInspector]
    public CityBuilderManager cityBuilderManager;

    [SerializeField]
    private CameraController cameraController;

    [SerializeField]
    private UnityEvent<ImprovementDataSO> OnIconButtonClick;
    [SerializeField]
    private UnityEvent<UnitBuildDataSO> OnUnitIconButtonClick;

    //[SerializeField]
    //private Transform uiElementsParent;
    [HideInInspector]
    public List<UIBuildOptions> buildOptions = new();

    //for blurring background
    [SerializeField]
    private Volume globalVolume;
    private DepthOfField dof;

    //for tweening
    [SerializeField]
    private RectTransform allContents;
    private Vector3 originalLoc;
    [HideInInspector]
    public bool activeStatus; //set this up so we don't have to wait for tween to set inactive

    //public bool showResourceProduced, showResourceConsumed;

    [SerializeField]
    public ScrollRect optionsScroller;

    [SerializeField]
    private UIScrollButton scrollLeft, scrollRight;

    [HideInInspector]
    public bool isQueueing, somethingNew;

    [SerializeField]
    public Transform objectHolder, finalSpaceHolder;
    //for updating resources
    private int maxResource;
    private int maxLabor;
    private int maxGold;

    //for object pooling
    //private Queue<UIResourceInfoPanel> resourceInfoPanelQueue = new();
    //[SerializeField]
    //private GameObject resourceInfoPanel;

    //public bool isUnit; //flag indicating if units will be built using this UI

    //[SerializeField]
    //public CanvasGroup uiBuildGroup;

    private void Awake()
    {
        gameObject.SetActive(false); //Hide to start

        if (globalVolume.profile.TryGet<DepthOfField>(out DepthOfField tmpDof))
        {
            dof = tmpDof;
        }
        dof.focalLength.value = 15;

       // foreach (Transform selection in uiElementsParent) //populate list
       // {
       //     UIBuildOptions option = selection.GetComponent<UIBuildOptions>();

       //     if (option)
    			//buildOptions.Add(option);
       // }

        originalLoc = allContents.anchoredPosition3D;
        cityBuilderManager = FindObjectOfType<CityBuilderManager>();

        //GrowResourceInfoPanelPool();
    }

	private void Update()
    {
        if (scrollLeft != null)
        {
            if (scrollLeft.isDown)
            {
                ScrollLeft();
            }
        }

        if (scrollRight != null)
        {
            if (scrollRight.isDown)
            {
                ScrollRight();
            }
        }
    }

    private void ScrollLeft()
    {
        if (optionsScroller.horizontalNormalizedPosition >= 0f)
        {
            optionsScroller.horizontalNormalizedPosition -= 0.007f;
        }
    }

    private void ScrollRight()
    {
        if (optionsScroller.horizontalNormalizedPosition <= 1f)
        {
            optionsScroller.horizontalNormalizedPosition += 0.007f;
        }
    }

    public void HandleButtonClick()
    {
        OnIconButtonClick?.Invoke(buildData);

        if (buildData.isBuilding && !buildData.isBuildingImprovement)
			cityBuilderManager.PlayBoomAudio();
        else
		    cityBuilderManager.PlaySelectAudio();
    }

    public void HandleUnitButtonClick()
    {
        OnUnitIconButtonClick?.Invoke(unitBuildData);

        if (unitBuildData.trainTime > 0)
        {
            if (unitBuildData.transportationType == TransportationType.Sea)
                cityBuilderManager.PlayConstructionAudio();
            else
                cityBuilderManager.PlayTrainingAudio();

        }
    }

    public void FinishMenuSetup()
    {
        finalSpaceHolder.SetAsLastSibling();

		foreach (Transform selection in objectHolder) //populate list
		{
			if (selection.TryGetComponent(out UIBuildOptions option))
				buildOptions.Add(option);
		}
	}

    public void ToggleVisibility(bool v, bool openTab, bool somethingNew = false, ResourceManager resourceManager = null) //pass resources to know if affordable in the UI (optional)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(v);
            activeStatus = true;
            cityBuilderManager.world.goldUpdateCheck = this;
            cityBuilderManager.buildOptionsActive = true;
            cityBuilderManager.activeBuilderHandler = this;
            this.somethingNew = somethingNew;
			cityBuilderManager.world.BattleCamCheck(true);

			if (!openTab)
            {
                //cityBuilderManager.world.openingImmoveable = true;
                cityBuilderManager.world.immoveableCanvas.gameObject.SetActive(true);
                LeanTween.value(globalVolume.gameObject, dof.focalLength.value, 45, 1f)
                .setEase(LeanTweenType.easeOutSine)
                .setOnUpdate((value) =>
                {
                    dof.focalLength.value = value;
                });

				if (cityBuilderManager.world.tutorialGoing)
				{
					for (int i = 0; i < buildOptions.Count; i++)
					{
						if (buildOptions[i].isFlashing)
                        {
							StartCoroutine(cityBuilderManager.world.EnableButtonHighlight(buildOptions[i].transform, true, true));
                            break;
                        }   
					}
				}
			}
			//else
			//{
			//    dof.focalLength.value = 45;
			//}

			//dof.focalLength.value = 45;

			allContents.anchoredPosition3D = originalLoc + new Vector3(0, -1000f, 0);

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 1000f, 0.5f).setEaseOutSine();
            
            //this could break things
            //LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();

            if (resourceManager != null)
            {
                PrepareBuildOptions(resourceManager);
            }

            //if (buildOptions.Count <= 5) //hiding the scroll buttons if not enough options to scroll
            //{
            //    scrollLeft.gameObject.SetActive(false);
            //    scrollRight.gameObject.SetActive(false);
            //}
            //else
            //{
            //    scrollLeft.gameObject.SetActive(true);
            //    scrollRight.gameObject.SetActive(true);
            //}
        }
        else
        {
            activeStatus = false;
            cityBuilderManager.world.goldUpdateCheck = null;
            maxResource = 0;
            maxLabor = 0;
            maxGold = 0;
            cityBuilderManager.buildOptionsActive = false;
            cityBuilderManager.activeBuilderHandler = null;
			cityBuilderManager.world.BattleCamCheck(false);

			if (this.somethingNew)
            {
                this.somethingNew = false;

                if (tabName == "Units")
                {
					for (int i = 0; i < buildOptions.Count; i++)
					{
						if (buildOptions[i].somethingNew)
							cityBuilderManager.uiCityTabs.ToggleButtonNew(tabName, buildOptions[i].UnitBuildData.unitNameAndLevel, true, false);
					}
				}
                else
                {
                    for (int i = 0; i < buildOptions.Count; i++)
                    {
                        if (buildOptions[i].somethingNew)
                            cityBuilderManager.uiCityTabs.ToggleButtonNew(tabName, buildOptions[i].BuildData.improvementNameAndLevel, false, false);
                    }
                }
            }

            //dof.focalLength.value = 15;
            if (!openTab)
            {
                //dof.focalLength.value = 15;
                //gameObject.SetActive(false);
                LeanTween.value(globalVolume.gameObject, dof.focalLength.value, 15, 0.05f)
                            .setEase(LeanTweenType.easeOutSine)
                            .setOnUpdate((value) =>
                            {
                                dof.focalLength.value = value;
                            });

                //this could break things
                //LeanTween.alpha(allContents, 0f, 0.2f).setEaseLinear();
                LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - 600f, 0.05f).setOnComplete(SetActiveStatusFalse);
            }
            else
            {
                gameObject.SetActive(false);
            }

            //dof.focalLength.value = 15;
            //gameObject.SetActive(false);
        }

        cameraController.enabled = !v;
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
        cityBuilderManager.world.ImmoveableCheck();
   //     if (!cityBuilderManager.world.openingImmoveable)
   //         cityBuilderManager.world.immoveableCanvas.gameObject.SetActive(false);
   //     else
			//cityBuilderManager.world.openingImmoveable = false;
	}

 //   private void OpeningComplete()
 //   {
	//	cityBuilderManager.world.openingImmoveable = false;
	//}

    public void PrepareBuild(ImprovementDataSO buildData)
    {
        this.buildData = buildData;
    }

    public void PrepareUnitBuild(UnitBuildDataSO unitBuildData)
    {
        this.unitBuildData = unitBuildData;
    }

    public void PrepareBuildOptions(ResourceManager resourceManager)
    {
        //List<SingleBuildType> improvementSingleBuildList = resourceManager.city.singleBuildDict.Keys.ToList();

        for (int i = 0; i < buildOptions.Count; i++)
        {
			SingleBuildType itemType = SingleBuildType.None;
			List<ResourceValue> resourceCosts = new();
			//bool locked = false;
			bool hide = false;

			if (buildOptions[i].UnitBuildData != null && buildOptions[i].UnitBuildData.unitType != UnitType.Laborer)
			{
                if (!cityBuilderManager.world.showAllBuildOptions && (!cityBuilderManager.world.upgradeableObjectMaxLevelDict.ContainsKey(buildOptions[i].UnitBuildData.unitType.ToString()) ||
					buildOptions[i].UnitBuildData.unitLevel != cityBuilderManager.world.GetUpgradeableObjectMaxLevel(buildOptions[i].UnitBuildData.unitType.ToString()) ||
					!resourceManager.city.singleBuildDict.ContainsKey(buildOptions[i].UnitBuildData.singleBuildType)))
                {
					buildOptions[i].Hide();
					continue;
                }

                if (buildOptions[i].UnitBuildData.unitType == UnitType.Transport)
                {
                    if ((buildOptions[i].UnitBuildData.transportationType == TransportationType.Sea && resourceManager.city.world.waterTransport) ||
                        (buildOptions[i].UnitBuildData.transportationType == TransportationType.Air && resourceManager.city.world.airTransport))
                        hide = true;
				}

				if (buildOptions[i].UnitBuildData.baseAttackStrength > 0)
                {
                    buildOptions[i].needsBarracks = !resourceManager.city.singleBuildDict.ContainsKey(buildOptions[i].UnitBuildData.singleBuildType);

                    if (!buildOptions[i].needsBarracks)
                    {
						buildOptions[i].trainingBarracks = resourceManager.city.world.GetCityDevelopment(resourceManager.city.singleBuildDict[buildOptions[i].UnitBuildData.singleBuildType]).isTraining;
						buildOptions[i].fullBarracks = resourceManager.city.army.isFull;
                        buildOptions[i].travelingBarracks = resourceManager.city.army.IsGone();
					}
                    else
                    {
                        buildOptions[i].trainingBarracks = false;
						buildOptions[i].fullBarracks = false;
                        buildOptions[i].travelingBarracks = false;
					}
				}
                else
                {
					buildOptions[i].needsBarracks = !resourceManager.city.singleBuildDict.ContainsKey(buildOptions[i].UnitBuildData.singleBuildType);

                    if (!buildOptions[i].needsBarracks)
                        buildOptions[i].trainingBarracks = resourceManager.city.world.GetCityDevelopment(resourceManager.city.singleBuildDict[buildOptions[i].UnitBuildData.singleBuildType]).isTraining;
                    else
                        buildOptions[i].trainingBarracks = false;
				}
				
                resourceCosts = new(buildOptions[i].UnitBuildData.unitCost);
				ResourceValue laborCost;
				laborCost.resourceType = ResourceType.Labor;
				laborCost.resourceAmount = buildOptions[i].UnitBuildData.laborCost;
				resourceCosts.Add(laborCost);
				
                //itemType = buildOptions[i].UnitBuildData.singleBuildType;

				//if (buildOptions[i].UnitBuildData.transportationType == TransportationType.Sea)
				//{
    //                if (resourceManager.city.singleBuildDict.ContainsKey(SingleBuildType.Harbor))
    //                    buildOptions[i].trainingBarracks = resourceManager.city.world.GetCityDevelopment(resourceManager.city.singleBuildDict[SingleBuildType.Harbor]).isTraining;
    //                else
    //                    buildOptions[i].trainingBarracks = false;
				//}
				//else
				//{
				//	if (buildOptions[i].UnitBuildData.baseAttackStrength > 0)
				//	{
				//		buildOptions[i].needsBarracks = !resourceManager.city.singleBuildDict.ContainsKey(SingleBuildType.Barracks);
				//		buildOptions[i].fullBarracks = resourceManager.city.army.isFull;
    //                    if (!buildOptions[i].needsBarracks)
    //                        buildOptions[i].trainingBarracks = resourceManager.city.world.GetCityDevelopment(resourceManager.city.singleBuildDict[SingleBuildType.Barracks]).isTraining;
    //                    else
    //                        buildOptions[i].trainingBarracks = false;
    //                    buildOptions[i].travelingBarracks = resourceManager.city.army.IsGone();
				//	}
    //                else
    //                {

    //                }
				//}


                //if (/*buildOptions[i].UnitBuildData.singleBuildType != SingleBuildType.None && */!resourceManager.city.singleBuildDict.ContainsKey(buildOptions[i].UnitBuildData.singleBuildType))
                //    hide = true;

    //            if (buildOptions[i].UnitBuildData.transportationType == TransportationType.Land)
    //            {
    //                if (buildOptions[i].UnitBuildData.baseAttackStrength > 0 && !resourceManager.city.hasBarracks)
    //                    hide = true;
    //            }
    //            else if (buildOptions[i].UnitBuildData.transportationType == TransportationType.Sea)
    //            {
    //                if (!resourceManager.city.hasHarbor)
    //                    hide = true;

    //                if (buildOptions[i].UnitBuildData.unitType == UnitType.Transport && resourceManager.city.world.waterTransport)
    //                    hide = true;
				//}
				//else if (buildOptions[i].UnitBuildData.transportationType == TransportationType.Air)
				//{
				//	if (!resourceManager.city.hasAirport)
				//		hide = true;

				//	if (buildOptions[i].UnitBuildData.unitType == UnitType.Transport && resourceManager.city.world.airTransport)
				//		hide = true;
				//}
			}
			else if (buildOptions[i].BuildData != null)
			{
				if (!cityBuilderManager.world.showAllBuildOptions && (!cityBuilderManager.world.upgradeableObjectMaxLevelDict.ContainsKey(buildOptions[i].BuildData.improvementName) || 
                    buildOptions[i].BuildData.improvementLevel != cityBuilderManager.world.GetUpgradeableObjectMaxLevel(buildOptions[i].BuildData.improvementName)))
				{
					buildOptions[i].Hide();
					continue;
				}

				//locked = buildOptions[i].locked;
				itemType = buildOptions[i].BuildData.singleBuildType;
				resourceCosts = new(buildOptions[i].BuildData.improvementCost);

				buildOptions[i].waterMax = resourceManager.city.reachedWaterLimit;

				if (buildOptions[i].BuildData.rawResourceType == RawResourceType.None)
				{
					if (!resourceManager.city.hasWater && buildOptions[i].BuildData.terrainType == TerrainType.Coast)
						hide = true;
				}
				else
				{
					hide = HideBuildOptionCheck(buildOptions[i].BuildData.rawResourceType, buildOptions[i].BuildData.terrainType, resourceManager.city);
				}

                SetProducedNumbers(resourceManager, buildOptions[i]);
			}

			buildOptions[i].ToggleVisibility(true); //turn them all on initially, so as to not turn them on when things change

            //unlocking if new, even if can't build in location
			if (buildOptions[i].somethingNew)
				hide = false;

			if (/*locked || */hide || resourceManager.city.singleBuildList.Contains(itemType) || (buildOptions[i].BuildData == resourceManager.city.housingData && resourceManager.city.housingLocsAtMax))
			{
				buildOptions[i].Hide();
				continue;
			}

			//buildItem.ToggleInteractable(true);
			buildOptions[i].SetResourceTextToDefault();

            for (int j = 0; j < resourceCosts.Count; j++)
            {
				if (resourceCosts[j].resourceType == ResourceType.Gold)
				{
					if (resourceCosts[j].resourceAmount > maxGold)
						maxGold = resourceCosts[j].resourceAmount;

					if (!resourceManager.city.CheckWorldGold(resourceCosts[j].resourceAmount))
						buildOptions[i].SetResourceTextToRed(resourceCosts[j]);
				}
				else if (resourceCosts[j].resourceType == ResourceType.Labor)
				{
					if (resourceCosts[j].resourceAmount > maxLabor)
						maxLabor = resourceCosts[j].resourceAmount;

					int pop = resourceManager.city.currentPop;
					if (pop < resourceCosts[j].resourceAmount)
						buildOptions[i].SetResourceTextToRed(resourceCosts[j]);
				}
				else if (!resourceManager.CheckResourceAvailability(resourceCosts[j]))
				{
					if (resourceCosts[j].resourceAmount > maxResource)
						maxResource = resourceCosts[j].resourceAmount;

					buildOptions[i].SetResourceTextToRed(resourceCosts[j]);
				}
			}
        }
    }

    public void UpdateProducedNumbers(ResourceManager resourceManager) 
    {
        for (int i = 0; i < buildOptions.Count; i++)
            SetProducedNumbers(resourceManager, buildOptions[i]);
    }

    private void SetProducedNumbers(ResourceManager resourceManager, UIBuildOptions option)
    {
		float workEthic = resourceManager.city.workEthic;

		for (int j = 0; j < option.producedResourcePanels.Count; j++)
		{
            int amount = option.BuildData.producedResources[j].resourceAmount;
            int newAmount = Mathf.RoundToInt(amount * (workEthic + cityBuilderManager.world.GetResourceTypeBonus(option.producedResourcePanels[j].resourceType)));

			option.producedResourcePanels[j].SetResourceAmount(newAmount);
			if (newAmount == amount)
				option.producedResourcePanels[j].resourceAmountText.color = Color.white;
			else if (newAmount > amount)
				option.producedResourcePanels[j].resourceAmountText.color = Color.green;
			else
				option.producedResourcePanels[j].resourceAmountText.color = Color.red;
		}
	}

    private bool HideBuildOptionCheck(RawResourceType rawResourceType, TerrainType terrainType, City city)
    {
        bool hide = false;
        switch (rawResourceType)
        {
            case RawResourceType.Clay:
                if (!city.hasClay)
                    hide = true;
                    break;
            case RawResourceType.Wool:
                if (!city.hasWool)
                    hide = true;
                    break;
            case RawResourceType.Silk:
                if (!city.hasSilk)
                    hide = true;
                    break;
            case RawResourceType.Rocks:
                if ((!city.hasRocksFlat && terrainType == TerrainType.Flatland) || (!city.hasRocksHill && terrainType == TerrainType.Hill))
                    hide = true;
                    break;
            case RawResourceType.FoodLand:
                if (!city.hasFood)
                    hide = true;
                    break;
            case RawResourceType.Lumber:
                if (!city.hasTrees)
                    hide = true;
                    break;
            case RawResourceType.FoodSea:
                if (!city.hasWater)
                    hide = true;
                    break;
        }

        return hide;
    }

    public void UpdateGold(int prevAmount, int currentAmount, bool pos)
    {
		if (pos)
		{
			if (prevAmount > maxGold)
				return;
		}
		else
		{
			if (currentAmount > maxGold)
				return;
		}

        PrepareBuildOptions(cityBuilderManager.SelectedCity.ResourceManager);
	}

    public void UpdateBuildOptions(ResourceType type, int prevAmount, int currentAmount, bool pos, ResourceManager resourceManager)
    {
        //checking if updating is necessary
        if (type == ResourceType.Labor)
        {
            if (pos)
            {
                if (prevAmount > maxLabor)
                    return;
            }
            else
            {
                if (currentAmount > maxLabor)
                    return;
            }
        }
        else
        {
            if (pos)
            {
                if (prevAmount > maxResource)
                    return;
            }
            else
            {
                if (currentAmount > maxResource)
                    return;
            }
        }

        PrepareBuildOptions(resourceManager);
    }

    public void UpdateTrainingStatus(SingleBuildType single)
    {
		foreach (UIBuildOptions buildItem in buildOptions)
		{
            if (buildItem.UnitBuildData.singleBuildType == single)
            {
                buildItem.needsBarracks = false;
                buildItem.trainingBarracks = false;
            }
		}
	}

 //   public void UpdateHarborStatus()
 //   {
	//	foreach (UIBuildOptions buildItem in buildOptions)
	//	{
	//		buildItem.trainingBarracks = false;
	//	}
	//}

    public void UpdateBarracksStatus(bool isFull)
    {
        foreach (UIBuildOptions buildItem in buildOptions)
        {
			buildItem.needsBarracks = false;
            buildItem.travelingBarracks = false;
            buildItem.trainingBarracks = false;
			buildItem.fullBarracks = isFull;
		}
	}


}
