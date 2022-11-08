using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class UILaborHandler : MonoBehaviour
{
    [HideInInspector]
    public int laborChange;
    private int currentLabor;
    public int GetCurrentLabor { get { return currentLabor; } }
    private int maxLabor;
    public int GetMaxLabor { get { return maxLabor; } }

    public string buildingName;

    [SerializeField]
    private UnityEvent<string> OnIconButtonClick;

    [SerializeField]
    private Transform uiElementsParent;
    private List<UILaborHandlerOptions> laborOptions;
    public List<UILaborHandlerOptions> GetLaborOptions { get { return laborOptions; } }

    //[SerializeField]
    //private TMP_Text noneText;

    [SerializeField] //for tweening
    private RectTransform allContents;
    private bool activeStatus;
    private Vector3 originalLoc;



    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;
        gameObject.SetActive(false);

        laborOptions = new List<UILaborHandlerOptions>();

        foreach (Transform selection in uiElementsParent) //populates list with just one card - "None"
        {
            laborOptions.Add(selection.GetComponent<UILaborHandlerOptions>());
        }

    }

    public void HandleButtonClick()
    {
        OnIconButtonClick?.Invoke(buildingName);
    }

    public void ShowUIRemoveBuildings(Vector3Int cityTile, MapWorld world)
    {
        ToggleVisibility(true);
        int showCount = 0;

        foreach (UILaborHandlerOptions option in laborOptions)
        {
            if (option.noneText)
            {
                if (showCount == 0)
                    option.ToggleVisibility(true);
                else
                    option.ToggleVisibility(false);

                continue;
            }

            showCount += option.CheckVisibility(cityTile, world);

            option.EnableHighlight(Color.red);
            option.ToggleInteractable(true); //toggle all interactable
            option.CheckVisibility(cityTile, world);
        }
    }

    //pass data to know if can show in the UI
    public void ShowUI(int laborChange, City city, MapWorld world, int placesToWork) 
    {
        ToggleVisibility(true);
        
        this.laborChange = laborChange;
        int showCount = 0; //to see if None text should be shown

        foreach (UILaborHandlerOptions option in laborOptions)
        {
            if (option.noneText)
            {
                if (showCount == 0)
                    option.ToggleVisibility(true);
                else
                    option.ToggleVisibility(false);

                continue;
            }

            showCount += option.CheckVisibility(city.cityLoc, world);
            //showCount += result;
        }

        PrepareLaborChangeOptions(city.cityPop, placesToWork);
    }

    private void ToggleVisibility(bool v)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(true);

            activeStatus = true;

            allContents.anchoredPosition3D = originalLoc + new Vector3(500f, 0, 0);

            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x - 500f, 0.3f).setEaseOutSine();
            LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear();
        }
        else
        {
            activeStatus = false;
            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + 600f, 0.2f).setOnComplete(SetActiveStatusFalse);
        }
    }

    public void HideUI()
    {
        ToggleVisibility(false);        
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public int GetLaborChange()
    {
        return laborChange;
    }

    public void PrepareLaborChange(int currentLabor, int maxLabor, string buildingName)
    {
        this.currentLabor = currentLabor;
        this.maxLabor = maxLabor;
        this.buildingName = buildingName;
    }

    private void PrepareLaborChangeOptions(CityPopulation cityPop, int placesToWork)
    {
        //uiLaborHandler.ToggleTweenVisibility(true);

        foreach (UILaborHandlerOptions laborItem in laborOptions)
        {
            laborItem.ToggleInteractable(true);

            if (laborChange > 0 && (cityPop.GetSetUnusedLabor == 0 || placesToWork == 0 || laborItem.CheckLaborIsMaxxed()))
                laborItem.ToggleInteractable(false); //deactivate if not enough unused labor

            if (laborChange < 0 && (cityPop.GetSetUsedLabor == 0 || laborItem.GetCurrentLabor == 0))
                laborItem.ToggleInteractable(false); //deactivate if not enough used labor
        }
    }
}
