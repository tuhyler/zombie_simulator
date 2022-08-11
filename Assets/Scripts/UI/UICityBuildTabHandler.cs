using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UICityBuildTabHandler : MonoBehaviour
{
    [SerializeField]
    private CityBuilderManager cityBuilderManager;

    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private UILaborHandler uiLaborHandler;

    private UIBuilderHandler builderUI;
    private bool sameUI;
    private ResourceManager resourceManager;

    [SerializeField]
    private RectTransform allContents;

    private Vector3 originalLoc;
    private bool activeStatus;

    private void Awake()
    {
        gameObject.SetActive(false);
        originalLoc = allContents.anchoredPosition3D;
    }

    public void PassUI(UIBuilderHandler uiBuilder)
    {
        uiLaborHandler.HideUI();

        bool currentlyActive = uiBuilder.activeStatus;

        if (builderUI != null) //checking if new tab is clicked 
        {
            builderUI.ToggleVisibility(false);
        }

        if (builderUI == uiBuilder && currentlyActive) //turn off if same tab is clicked
        {
            sameUI = true;
            builderUI.ToggleVisibility(false);
        }

        builderUI = uiBuilder;
        cityBuilderManager.ResetTileLists(); //to reset highlighted tiles when going to different tab
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

    public void ToggleInteractable(bool v)
    {
        canvasGroup.interactable = v;
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

    public void ResetUI()
    {
        sameUI = false;
        builderUI = null;
    }
}
