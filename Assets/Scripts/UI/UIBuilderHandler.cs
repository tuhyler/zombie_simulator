using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIBuilderHandler : MonoBehaviour
{
    private ImprovementDataSO buildData;
    private UnitBuildDataSO unitBuildData;

    [SerializeField]
    private UnityEvent<ImprovementDataSO> OnIconButtonClick;
    [SerializeField]
    private UnityEvent<UnitBuildDataSO> OnUnitIconButtonClick;

    [SerializeField]
    private Transform uiElementsParent;
    private List<UIBuildOptions> buildOptions;

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
    public bool isQueueing;

    //for object pooling
    private Queue<UIResourceInfoPanel> resourceInfoPanelQueue = new();
    [SerializeField]
    private GameObject resourceInfoPanel;

    //public bool isUnit; //flag indicating if units will be built using this UI

    //[SerializeField]
    //public CanvasGroup uiBuildGroup;

    private void Awake()
    {
        gameObject.SetActive(false); //Hide to start

        buildOptions = new List<UIBuildOptions>(); //instantiate

        foreach (Transform selection in uiElementsParent) //populate list
        {
            buildOptions.Add(selection.GetComponent<UIBuildOptions>());
        }

        originalLoc = allContents.anchoredPosition3D;

        GrowResourceInfoPanelPool();
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
            optionsScroller.horizontalNormalizedPosition -= 0.05f;
        }
    }

    private void ScrollRight()
    {
        if (optionsScroller.horizontalNormalizedPosition <= 1f)
        {
            optionsScroller.horizontalNormalizedPosition += 0.05f;
        }
    }

    public void HandleButtonClick()
    {
        OnIconButtonClick?.Invoke(buildData);
    }

    public void HandleUnitButtonClick()
    {
        OnUnitIconButtonClick?.Invoke(unitBuildData);
    }

    public void ToggleVisibility(bool v, ResourceManager resourceManager = null) //pass resources to know if affordable in the UI (optional)
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
            LeanTween.alpha(allContents, 0f, 0.2f).setEaseLinear();
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - 300f, 0.2f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void PrepareBuild(ImprovementDataSO buildData)
    {
        this.buildData = buildData;
    }

    public void PrepareUnitBuild(UnitBuildDataSO unitBuildData)
    {
        this.unitBuildData = unitBuildData;
    }

    private void PrepareBuildOptions(ResourceManager resourceManager)
    {
        List<string> improvementSingleBuildList = resourceManager.city.singleBuildImprovementsAndBuildings;

        foreach (UIBuildOptions buildItem in buildOptions)
        {
            if (buildItem == null)
                continue;

            string itemName;
            if (buildItem.UnitBuildData != null)
                itemName = buildItem.UnitBuildData.unitName;
            else
                itemName = buildItem.BuildData.improvementName;

            buildItem.ToggleVisibility(true); //turn them all on initially, so as to not turn them on when things change

            if (improvementSingleBuildList.Contains(itemName))
            {
                buildItem.ToggleVisibility(false);
                continue;
            }

            //buildItem.ToggleInteractable(true);
            buildItem.SetResourceTextToDefault();

            List<ResourceValue> resourceCosts = new();

            if (buildItem.UnitBuildData != null)
                resourceCosts = new(buildItem.UnitBuildData.unitCost);
            if (buildItem.BuildData != null)
                resourceCosts = new(buildItem.BuildData.improvementCost);

            foreach (ResourceValue item in resourceCosts)
            {
                if (!resourceManager.CheckResourceAvailability(item))
                {
                    //buildItem.ToggleInteractable(false); //deactivate if not enough resources
                    buildItem.SetResourceTextToRed(item);
                    break;
                }
            }
        }
    }

    private void GrowResourceInfoPanelPool()
    {
        for (int i = 0; i < 20; i++) //grow pool 20 at a time
        {
            GameObject panel = Instantiate(resourceInfoPanel);
            UIResourceInfoPanel uiResourceInfoPanel = panel.GetComponent<UIResourceInfoPanel>();
            AddToResourceInfoPanelPool(uiResourceInfoPanel);
        }
    }

    private void AddToResourceInfoPanelPool(UIResourceInfoPanel resourceInfoPanel)
    {
        resourceInfoPanel.gameObject.SetActive(false); //inactivate it when adding to pool
        resourceInfoPanelQueue.Enqueue(resourceInfoPanel);
    }

    public UIResourceInfoPanel GetFromResourceInfoPanelPool()
    {
        if (resourceInfoPanelQueue.Count == 0)
            GrowResourceInfoPanelPool();

        var resourceInfoPanel = resourceInfoPanelQueue.Dequeue();
        resourceInfoPanel.gameObject.SetActive(true);
        return resourceInfoPanel;
    }
}
