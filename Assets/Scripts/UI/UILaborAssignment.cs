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
    private List<UILaborAssignmentOptions> laborOptions = new();

    [SerializeField]
    public GameObject showPrioritiesButton;

    [SerializeField] //for tweening
    public RectTransform allContents;
    [HideInInspector]
    public bool activeStatus;
    [HideInInspector]
    public Vector3 originalLoc;


    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;
        gameObject.SetActive(false); //Hide to start

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

    public void ShowUI() 
    {
        if (activeStatus)
            return;

        LeanTween.cancel(gameObject);

        gameObject.SetActive(true);
        activeStatus = true;
        allContents.anchoredPosition3D = originalLoc + new Vector3(0, -200f, 0);

        LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 200f, 0.4f).setEaseOutBack();
    }

    public UILaborAssignmentOptions GetLaborButton(int change)
    {
		foreach (UILaborAssignmentOptions option in laborOptions)
		{
            if (option.LaborChange == change)
                return option;
		}

        return laborOptions[0];
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
}
