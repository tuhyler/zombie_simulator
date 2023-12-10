using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UILaborAssignment : MonoBehaviour
{
    private int laborChange;

    [HideInInspector]
    public int laborChangeFlag;

    [SerializeField]
    private UnityEvent<int> OnIconButtonClick;

    [SerializeField]
    private CityBuilderManager cityBuildingManager;

    [SerializeField]
    private Transform uiElementsParent;
    private List<UILaborAssignmentOptions> laborOptions;

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
            UILaborAssignmentOptions assignmentOption = selection.GetComponent<UILaborAssignmentOptions>();
            assignmentOption.SetCityBuilderManager(cityBuildingManager);
            laborOptions.Add(assignmentOption);
            //Debug.Log("print " + selection.name);
        }
    }

    public void HandleButtonClick()
    {
        OnIconButtonClick?.Invoke(laborChange);
    }

    public void ShowUI(City city, int placesToWork, bool update = true) //pass data to know if can show in the UI
    {
        if (activeStatus)
            return;

        LeanTween.cancel(gameObject);

        gameObject.SetActive(true);
        activeStatus = true;
        allContents.anchoredPosition3D = originalLoc + new Vector3(0, -600f, 0);

        LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 600f, 0.3f).setEaseOutSine();
        //LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();

        //if (city.AutoAssignLabor)
        //{
        //    SetAssignmentOptionsInteractableOff();
        //    return;
        //}

        if (update)
            PrepareLaborChangeOptions(city.cityPop, placesToWork, city.AutoAssignLabor);
    }

    public void UpdateUI(City city, int placesToWork)
    {
        PrepareLaborChangeOptions(city.cityPop, placesToWork, city.AutoAssignLabor);
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

    private void PrepareLaborChangeOptions(CityPopulation cityPop, int placesToWork, bool autoAssign)
    {
        if (autoAssign) //can't adjust labor with auto assign on
        {
            SetAssignmentOptionsInteractableOff();
            return;
        }
        
        foreach (UILaborAssignmentOptions laborItem in laborOptions)
        {
            laborItem.ToggleInteractable(true);

            if (laborItem.LaborChange > 0 && (cityPop.UnusedLabor == 0 || placesToWork == 0))
            {
                laborItem.ToggleInteractable(false); //deactivate if not enough unused labor
                //cityBuildingManager.LaborChange = 0;
            }

            if (laborItem.LaborChange < 0 && cityPop.UsedLabor == 0)
            {
                laborItem.ToggleInteractable(false); //deactivate if not enough used labor
                //cityBuildingManager.LaborChange = 0;
            }
        }
    }

    public void ToggleInteractable(int laborChange)
    {
        foreach (UILaborAssignmentOptions laborItem in laborOptions)
        {
            if (laborItem.LaborChange == laborChange)
            {
                laborItem.ToggleButtonSelection(true);
            }
        }
    }

    public void SetAssignmentOptionsInteractableOff()
    {
        foreach (UILaborAssignmentOptions laborItem in laborOptions)
        {
            laborItem.ToggleInteractable(false);
        }

        laborChange = 0;
        laborChangeFlag = 0;
    }
}
