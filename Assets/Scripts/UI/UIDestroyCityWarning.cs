using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIDestroyCityWarning : MonoBehaviour
{
    [SerializeField]
    private HandlePlayerInput playerInput;

    [SerializeField]
    private UIUnitTurnHandler turnHandler;

    [SerializeField]
    private UICityBuildTabHandler uiCityBuildTabHandler;

    [SerializeField]
    private CameraController cameraController;

    [SerializeField]
    private UILaborAssignment uiLaborAssignment;

    [SerializeField]
    private UIInfoPanelCity uiInfoPanelCity;

    [SerializeField]
    private UIWonderSelection uiWonderSelection;

    [SerializeField] //for tweening
    private RectTransform allContents;
    private bool activeStatus;


    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void ToggleVisibility(bool v)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(v);
            activeStatus = true;

            allContents.localScale = Vector3.zero;
            LeanTween.scale(allContents, Vector3.one, 0.25f).setEase(LeanTweenType.easeOutSine);
        }
        else
        {
            activeStatus = false;
            LeanTween.scale(allContents, Vector3.zero, 0.25f).setOnComplete(SetActiveStatusFalse);
        }

        playerInput.enabled = !v;
        cameraController.enabled = !v;
        uiCityBuildTabHandler.ToggleEnable(!v);
        uiLaborAssignment.ToggleEnable(!v);
        uiInfoPanelCity.enabled = !v;
        turnHandler.ToggleEnable(!v);
        uiWonderSelection.ToggleEnable(!v);
    }

    public void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }
}
