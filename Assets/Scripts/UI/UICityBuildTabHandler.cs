using System.Collections.Generic;
using UnityEngine;

public class UICityBuildTabHandler : MonoBehaviour
{
    [SerializeField]
    public CityBuilderManager cityBuilderManager;

    [SerializeField]
    public GameObject marketButton, queueButton;

    //[SerializeField]

    //[SerializeField]
    //public CanvasGroup canvasGroup;

    //[SerializeField]
    //private UILaborHandler uiLaborHandler;

    [HideInInspector]
    public UIBuilderHandler builderUI;
    [HideInInspector]
    public UIShowTabHandler currentTabSelected;
    [HideInInspector]
    public bool sameUI, openTab, somethingNew;
    private ResourceManager resourceManager;
    private List<UIShowTabHandler> tabList = new();

    [HideInInspector]
    public bool buttonsAreWorking;

    [SerializeField] // for tweening
    private RectTransform allContents;
    private Vector3 originalLoc;
    [HideInInspector]
    public bool activeStatus;

    
    //for side buttons
    private bool isRemoving, isUpgrading, isSelling;

    private void Awake()
    {
        gameObject.SetActive(false);
        originalLoc = allContents.anchoredPosition3D;
        buttonsAreWorking = true;

		foreach (Transform selection in allContents)
		{
			foreach (Transform nextSelection in selection)
            {
                if (nextSelection.TryGetComponent(out UIShowTabHandler tabHandler))
                    tabList.Add(tabHandler);
            }
		}
	}

    public void ToggleButtonNew(string tabName, string buildOptionName, bool unit, bool v)
    {
        if (v)
            cityBuilderManager.world.newUnitsAndImprovements.Add(buildOptionName);
        else
            cityBuilderManager.world.newUnitsAndImprovements.Remove(buildOptionName);

        somethingNew = v;

        for (int i = 0; i < tabList.Count; i++)
        {
            if (tabList[i].tabName == tabName)
            {
                tabList[i].ToggleSomethingNew(v);

                if (unit)
                {
					for (int j = 0; j < tabList[i].UIBuilder.buildOptions.Count; j++)
                    {
						if (tabList[i].UIBuilder.buildOptions[j].UnitBuildData.unitNameAndLevel == buildOptionName)
						{
							tabList[i].UIBuilder.buildOptions[j].ToggleSomethingNew(v);
							break;
						}
					}
				}
                else
                {
					for (int j = 0; j < tabList[i].UIBuilder.buildOptions.Count; j++)
                    {
						if (tabList[i].UIBuilder.buildOptions[j].BuildData.improvementNameAndLevel == buildOptionName)
						{
							tabList[i].UIBuilder.buildOptions[j].ToggleSomethingNew(v);
							break;
						}
					}
				}
            }
            else
            {
                if (tabList[i].somethingNew)
                    somethingNew = true;
            }
        }
    }

    //public void ToggleLockButton(string tabName, string improvementNameAndLevel, bool v)
    //{
    //    for (int i = 0; i < tabList.Count; i++)
    //    {
    //        if (tabList[i].tabName == tabName)
    //        {
    //            for (int j = 0; j < tabList[i].UIBuilder.buildOptions.Count; j++)
    //            {
    //                if (tabList[i].UIBuilder.buildOptions[j].BuildData.improvementNameAndLevel == improvementNameAndLevel)
    //                {
    //                    tabList[i].UIBuilder.buildOptions[j].locked = v;
    //                    break;
    //                }
    //            }
    //            break;
    //        }
    //    }
    //}

 //   public void ToggleUnitLockButton(string tabName, string unitNameAndLevel, bool v)
	//{
	//	for (int i = 0; i < tabList.Count; i++)
	//	{
	//		if (tabList[i].tabName == tabName)
	//		{
	//			for (int j = 0; j < tabList[i].UIBuilder.buildOptions.Count; j++)
	//			{
	//				if (tabList[i].UIBuilder.buildOptions[j].UnitBuildData.unitNameAndLevel == unitNameAndLevel)
	//				{
	//					tabList[i].UIBuilder.buildOptions[j].locked = v;
	//					break;
	//				}
	//			}
	//			break;
	//		}
	//	}
	//}

	public void PassUI(UIBuilderHandler uiBuilder)
    {
        CloseOtherWindows();
		bool openTab = true;

        //bool currentlyActive = uiBuilder.activeStatus;
        if (builderUI == uiBuilder) //turn off if same tab is clicked
        {
            sameUI = true; //so it doesn't reopen selected tab
            openTab = false;
        }
        
        if (builderUI != null) //checking if new tab is clicked 
        {
            HideSelectedTab(openTab);
        }
        else if (isRemoving || isUpgrading || isSelling) //turning off side buttons
        {
            HideSelectedTab(openTab);
        }

        builderUI = uiBuilder;
    }

    public void StartLeftSideButton()
    {
        sameUI = false;

        if (isSelling)
            sameUI = true;

        isSelling = true;
        CloseOtherWindows();
		HideSelectedTab(false);
    }

    public void StartRightSideButton(bool option)
    {
        sameUI = false;

        if (option)
        {
            if (isRemoving)
            {
                sameUI = true;
            }
            isRemoving = true;
        }
        else
        {
            if (isUpgrading)
            {
                sameUI = true;
            }
            isUpgrading = true;
        }

        CloseOtherWindows();
        HideSelectedTab(false);
    }

    private void CloseOtherWindows()
    {
		cityBuilderManager.CloseLaborMenus();
		cityBuilderManager.CloseImprovementTooltipButton();
		cityBuilderManager.CloseImprovementBuildPanel();
		cityBuilderManager.CloseSingleWindows();
		cityBuilderManager.uiResourceManager.ToggleOverflowVisibility(false);
		cityBuilderManager.world.uiCityPopIncreasePanel.ToggleVisibility(false);
	}

    public void SetSelectedTab(UIShowTabHandler selectedTab)
    {
        if (!sameUI)
            currentTabSelected = selectedTab;
        else
            sameUI = false;
    }

    public void HideUnitsTab()
    {
        if (currentTabSelected != null && currentTabSelected.isUnits)
            HideSelectedTab(false);
    }

    public void HideSelectedTab(bool newTab)
    {
        if (builderUI != null)
        {
            openTab = newTab;
            builderUI.ToggleVisibility(false, openTab);
        }
        if (currentTabSelected != null)
        {
            if (currentTabSelected.isRemoving)
            {
                cityBuilderManager.CloseImprovementTooltipButton();
                cityBuilderManager.CloseImprovementBuildPanel();
                isRemoving = false;
            }
            else if (currentTabSelected.isUpgrading)
            {
                cityBuilderManager.CancelUpgrade();
                isUpgrading = false;
            }
            else if (currentTabSelected.isSelling)
            {
                cityBuilderManager.uiMarketPlaceManager.ToggleVisibility(false);
                isSelling = false;
            }
            CloseSelectedTab();
            builderUI = null;
        }
    }

    public void ToggleVisibility(bool v, /*bool market = false, */ResourceManager resourceManager = null)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(v);
            marketButton.SetActive(true);

            activeStatus = true;
            allContents.anchoredPosition3D = originalLoc + new Vector3(0, -200f, 0);

			if (cityBuilderManager.world.tutorialGoing)
			{
				for (int i = 0; i < tabList.Count; i++)
				{
					if (tabList[i].isFlashing)
						StartCoroutine(cityBuilderManager.world.EnableButtonHighlight(tabList[i].transform, true, false));
				}
			}

			LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 200f, 0.4f).setEaseOutBack();
            //LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();
        }
        else
        {
            activeStatus = false;
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - 200f, 0.2f).setOnComplete(SetActiveStatusFalse);
            builderUI = null;
        }

        this.resourceManager = resourceManager;
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
        cityBuilderManager.world.CityCanvasCheck();
        //if (!cityBuilderManager.world.openingCity)
        //    cityBuilderManager.world.cityCanvas.gameObject.SetActive(false);
        //else
        //    cityBuilderManager.world.openingCity = true;
    }

    //public void ToggleInteractable(bool v)
    //{
    //    canvasGroup.interactable = v;
    //}

    public void ToggleEnable(bool v)
    {
        buttonsAreWorking = v;
    }

    public void ShowUI()
    {
        if (sameUI)
        {
            //sameUI = false;
            builderUI = null;
            return;
        }
        builderUI.ToggleVisibility(true, openTab, somethingNew, resourceManager);
        openTab = true;
        //tabUI.ToggleInteractable(false);
    }

    public void ShowUILeftSideButton()
    {
        if (sameUI)
        {
            //sameUI = false;
            return;
        }

        cityBuilderManager.SellResources();
    }

    public void ShowUIRightSideButton(bool isRemoving)
    {
        if (sameUI)
        {
            //sameUI = false;
            return;
        }

        if (isRemoving)
            cityBuilderManager.RemoveImprovements();
        else
            cityBuilderManager.UpgradeImprovements();
    }

    public UIShowTabHandler GetTab(string tabName)
    {
        for (int i = 0; i < tabList.Count; i++)
        {
            if (tabList[i].tabName == tabName)
            {
                return tabList[i];
            }
        }

        return null;
    }

    public void CloseSelectedTab()
    {
        if (currentTabSelected != null)
        {
            currentTabSelected.ToggleButtonSelection(false);

            if (currentTabSelected.isRemoving)
                isRemoving = false;
            else if (currentTabSelected.isUpgrading)
                isUpgrading = false;
            else if (currentTabSelected.isSelling)
                isSelling = false;

            currentTabSelected = null;
        }
    }

    public void CloseRemovalWindow()
    {
        if (isRemoving)
        {
            cityBuilderManager.CloseImprovementTooltipButton();
            cityBuilderManager.CloseImprovementBuildPanel();
        }
    }

    public void CloseImprovementTooltipButton()
    {
        cityBuilderManager.CloseImprovementTooltipButton();
    }

    public void HandleC()
	{
		if (activeStatus && !cityBuilderManager.uiCityNamer.activeStatus)
			GetTab("Upgrade").SelectTabKeyboardShortcut();
	}

    public void HandleG()
    {
        if (activeStatus && !cityBuilderManager.uiCityNamer.activeStatus)
            GetTab("Buildings").SelectTabKeyboardShortcut();
    }

	public void HandleT()
	{
		if (activeStatus && !cityBuilderManager.uiCityNamer.activeStatus)
			GetTab("Units").SelectTabKeyboardShortcut();
	}

	public void HandleR()
	{
		if (activeStatus && !cityBuilderManager.uiCityNamer.activeStatus)
			GetTab("Raw Goods").SelectTabKeyboardShortcut();
	}

	public void HandleF()
	{
		if (activeStatus && !cityBuilderManager.uiCityNamer.activeStatus)
			GetTab("Producers").SelectTabKeyboardShortcut();
	}

	public void HandleX()
	{
		if (activeStatus && !cityBuilderManager.uiCityNamer.activeStatus)
			GetTab("Remove").SelectTabKeyboardShortcut();
	}

	public void HandleZ()
    {
		if (activeStatus && !cityBuilderManager.uiCityNamer.activeStatus)
			GetTab("Sell").SelectTabKeyboardShortcut();
	}
}
