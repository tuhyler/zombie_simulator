using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UICityBuildTabHandler : MonoBehaviour
{
    [SerializeField]
    private CityBuilderManager cityBuilderManager;

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

    private void Awake()
    {
        gameObject.SetActive(false);
        originalLoc = allContents.anchoredPosition3D;
        buttonsAreWorking = true; //setting default value doesn't work
    }

    public void PassUI(UIBuilderHandler uiBuilder)
    {
        cityBuilderManager.CloseLaborMenus();


        //bool currentlyActive = uiBuilder.activeStatus;
        if (builderUI == uiBuilder) //turn off if same tab is clicked
        {
            sameUI = true; //so it doesn't reopen selected tab
        }
        
        if (builderUI != null) //checking if new tab is clicked 
        {
            HideSelectedTab();
        }

        builderUI = uiBuilder;
    }

    public void SetSelectedTab(UIShowTabHandler selectedTab)
    {
        currentTabSelected = selectedTab;
    }

    public void HideSelectedTab()
    {
        if (builderUI != null)
            builderUI.ToggleVisibility(false);
        if (currentTabSelected != null)
        {
            currentTabSelected.ToggleButtonSelection(false);
            ResetUI();
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
            ResetUI();
        }

        this.resourceManager = resourceManager;
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
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
            sameUI = false;
            builderUI = null;
            return;
        }
        builderUI.ToggleVisibility(true, resourceManager);
        //tabUI.ToggleInteractable(false);
    }

    private void ResetUI()
    {
        builderUI = null;
    }
}
