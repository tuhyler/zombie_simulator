using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UILaborAssignment : MonoBehaviour
{
    private int laborChange;

    [HideInInspector]
    public int laborChangeFlag;

    [SerializeField]
    private UnityEvent<int> OnIconButtonClick;

    [SerializeField]
    private Transform uiElementsParent;
    private List<UILaborAssignmentOptions> laborOptions;

    [SerializeField]
    private Toggle autoAssign;

    [SerializeField]
    private Button openAssignmentPriorityMenu;

    [SerializeField]
    private UICityLaborPrioritizationManager uiLaborPrioritizationManager;

    [SerializeField] //for tweening
    private RectTransform allContents;
    private bool activeStatus;
    private Vector3 originalLoc;


    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;
        gameObject.SetActive(false); //Hide to start

        laborOptions = new List<UILaborAssignmentOptions>(); //instantiate

        foreach (Transform selection in uiElementsParent) //populate list
        {
            laborOptions.Add(selection.GetComponent<UILaborAssignmentOptions>());
            //Debug.Log("print " + selection.name);
        }

        openAssignmentPriorityMenu.interactable = false;
    }

    public void HandleButtonClick()
    {
        OnIconButtonClick?.Invoke(laborChange);
    }

    public void ToggleAutoAssign()
    {
        if (autoAssign.isOn)
        {
            openAssignmentPriorityMenu.interactable = true;
        }
        else
        {
            openAssignmentPriorityMenu.interactable = false;
        }
    }

    public void ShowUI(CityPopulation cityPop, int placesToWork) //pass data to know if can show in the UI
    {
        if (activeStatus)
            return;

        LeanTween.cancel(gameObject);

        gameObject.SetActive(true);
        activeStatus = true;
        allContents.anchoredPosition3D = originalLoc + new Vector3(0, -600f, 0);

        LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 600f, 0.3f).setEaseOutSine();
        LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();

        PrepareLaborChangeOptions(cityPop, placesToWork);
    }

    public void UpdateUI(CityPopulation cityPop, int placesToWork)
    {
        PrepareLaborChangeOptions(cityPop, placesToWork);
    }

    public void ToggleInteractable(bool v)
    {
        foreach (UILaborAssignmentOptions options in laborOptions)
        {
            options.ToggleInteractable(v);
        }
    }

    public void ToggleEnable(bool v)
    {
        foreach (UILaborAssignmentOptions options in laborOptions)
        {
            options.ToggleEnable(v);
        }
    }

    public void HideUI()
    {
        if (!activeStatus)
            return;

        LeanTween.cancel(gameObject);

        activeStatus = false;
        LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -600f, 0.2f).setOnComplete(SetActiveStatusFalse);

        laborChangeFlag = 0;
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void ResetLaborAssignment(int laborChange = 0)
    {
        this.laborChange = 0;
        laborChangeFlag = 0;

        foreach (UILaborAssignmentOptions laborOption in laborOptions)
        {
            if (laborOption.LaborChange == laborChange || laborChange == 0)
                laborOption.ToggleButtonSelection(false);
        }
    }

    public void PrepareLaborChange(int laborChange)
    {
        this.laborChange = laborChange;
    }

    private void PrepareLaborChangeOptions(CityPopulation cityPop, int placesToWork)
    {
        //uiLaborHandler.ToggleTweenVisibility(true);
        
        foreach (UILaborAssignmentOptions laborItem in laborOptions)
        {
            laborItem.ToggleInteractable(true);

            if (laborItem.LaborChange > 0 && (cityPop.GetSetUnusedLabor == 0 || placesToWork == 0))
                laborItem.ToggleInteractable(false); //deactivate if not enough unused labor

            if (laborItem.LaborChange < 0 && cityPop.GetSetUsedLabor == 0)
                laborItem.ToggleInteractable(false); //deactivate if not enough used labor
        }
    }
}
