using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIShowTabHandler : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    private UIBuilderHandler uiBuilder;
    public UIBuilderHandler UIBuilder { get { return uiBuilder; } }

    [SerializeField]
    private CanvasGroup canvasGroup;

    private UICityBuildTabHandler uiBuildTabHandler;

    //[SerializeField]
    //private CanvasGroup canvasGroup;

    [SerializeField] //changing color of button when selected
    private Image buttonImage;
    private Color originalButtonColor;
    private bool sideButton;
    public bool isRemoving, isUpgrading;

    private void Awake()
    {
        uiBuildTabHandler = GetComponentInParent<UICityBuildTabHandler>();
        originalButtonColor = buttonImage.color;
        if (isRemoving || isUpgrading)
            sideButton = true;
    }

    public void ToggleInteractable(bool v)
    {
        canvasGroup.interactable = v;
    }

    //public void OnButtonClick()
    //{
    //    uiBuildTabHandler.PassUI(uiBuilder);
    //    uiBuildTabHandler.ShowUI();
    //}

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!uiBuildTabHandler.buttonsAreWorking)
            return;
        
        ToggleButtonSelection(true);

        if (sideButton)
        {
            uiBuildTabHandler.StartSideButton(isRemoving);
            uiBuildTabHandler.ShowUISideButton(isRemoving);
        }
        else
        {
            uiBuildTabHandler.PassUI(uiBuilder);
            uiBuildTabHandler.ShowUI();
        }

        uiBuildTabHandler.SetSelectedTab(this);
    }

    public void ToggleButtonSelection(bool v)
    {
        if (v)
        {
            buttonImage.color = Color.green;
        }
        else
        {
            buttonImage.color = originalButtonColor;
        }
    }
}
