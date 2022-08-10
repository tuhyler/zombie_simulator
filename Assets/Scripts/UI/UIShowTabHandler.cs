using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIShowTabHandler : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    private UIBuilderHandler uiBuilder;
    public UIBuilderHandler UIBuilder { get { return uiBuilder; } }

    private UICityBuildTabHandler uiBuildTabHandler;

    [SerializeField]
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        uiBuildTabHandler = GetComponentInParent<UICityBuildTabHandler>();
    }

    public void ToggleInteractable(bool v)
    {
        canvasGroup.interactable = v;
    }

    public void OnButtonClick()
    {
        uiBuildTabHandler.PassUI(uiBuilder);
        uiBuildTabHandler.ShowUI();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        uiBuildTabHandler.PassUI(uiBuilder);
        uiBuildTabHandler.ShowUI();
    }
}
