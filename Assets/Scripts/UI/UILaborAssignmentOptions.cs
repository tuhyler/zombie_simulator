using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UILaborAssignmentOptions : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    private int laborChange;
    public int LaborChange { get { return laborChange; } }

    private UILaborAssignment buttonHandler;

    [SerializeField]
    private CityBuilderManager cityBuilderManager;

    [SerializeField]
    private CanvasGroup canvasGroup; //handles all aspects of a group of UI elements together, instead of individually in Unity

    private void Awake()
    {
        buttonHandler = GetComponentInParent<UILaborAssignment>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void ToggleInteractable(bool v)
    {
        canvasGroup.interactable = v;
    }

    public void OnButtonClick()
    {
        buttonHandler.PrepareLaborChange(laborChange);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        buttonHandler.PrepareLaborChange(laborChange);
        buttonHandler.HandleButtonClick();
    }
}
