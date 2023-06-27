using UnityEngine;

public class UICityBuildTabHandler : MonoBehaviour
{
    [SerializeField]
    public CityBuilderManager cityBuilderManager;

    //[SerializeField]

    //[SerializeField]
    //public CanvasGroup canvasGroup;

    //[SerializeField]
    //private UILaborHandler uiLaborHandler;

    private UIBuilderHandler builderUI;
    private UIShowTabHandler currentTabSelected;
    private bool sameUI;
    private ResourceManager resourceManager;

    [HideInInspector]
    public bool buttonsAreWorking;

    [SerializeField] // for tweening
    private RectTransform allContents;
    private Vector3 originalLoc;
    private bool activeStatus;

    //for side buttons
    private bool isRemoving, isUpgrading, isSelling;

    private void Awake()
    {
        gameObject.SetActive(false);
        originalLoc = allContents.anchoredPosition3D;
        buttonsAreWorking = true; 
    }

    public void PassUI(UIBuilderHandler uiBuilder)
    {
        cityBuilderManager.CloseLaborMenus();
        cityBuilderManager.CloseImprovementTooltipButton();
        cityBuilderManager.CloseImprovementBuildPanel();

        //bool currentlyActive = uiBuilder.activeStatus;
        if (builderUI == uiBuilder) //turn off if same tab is clicked
        {
            sameUI = true; //so it doesn't reopen selected tab
        }
        
        if (builderUI != null) //checking if new tab is clicked 
        {
            HideSelectedTab();
        }
        else if (isRemoving || isUpgrading || isSelling) //turning off side buttons
        {
            HideSelectedTab();
        }

        builderUI = uiBuilder;
    }

    public void StartLeftSideButton()
    {
        sameUI = false;

        if (isSelling)
            sameUI = true;

        isSelling = true;
        cityBuilderManager.CloseLaborMenus();
        cityBuilderManager.CloseImprovementTooltipButton();
        cityBuilderManager.CloseImprovementBuildPanel();
        HideSelectedTab();
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

        cityBuilderManager.CloseLaborMenus();
        cityBuilderManager.CloseImprovementTooltipButton();
        cityBuilderManager.CloseImprovementBuildPanel();
        HideSelectedTab();
    }

    public void SetSelectedTab(UIShowTabHandler selectedTab)
    {
        if (!sameUI)
            currentTabSelected = selectedTab;
        else
            sameUI = false;
    }

    public void HideSelectedTab()
    {
        if (builderUI != null)
        {
            builderUI.ToggleVisibility(false);
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

    public void ToggleVisibility(bool v, ResourceManager resourceManager = null)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(v);
            activeStatus = true;
            allContents.anchoredPosition3D = originalLoc + new Vector3(0, -200f, 0);

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 200f, 0.4f).setEaseOutBack();
            LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();
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
        cityBuilderManager.world.cityCanvas.gameObject.SetActive(false);
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
        builderUI.ToggleVisibility(true, resourceManager);
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
}
