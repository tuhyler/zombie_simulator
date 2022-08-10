using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIWorkerOptions : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private ImprovementDataSO buildData;
    public ImprovementDataSO BuildData { get { return buildData; } }

    private UIWorkerHandler buttonHandler;

    [SerializeField]
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        buttonHandler = GetComponentInParent<UIWorkerHandler>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void ToggleInteractable(bool v)
    {
        canvasGroup.interactable = v;
    }

    public void OnPointerClick()
    {
        buttonHandler.PrepareBuild(buildData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        buttonHandler.PrepareBuild(buildData);
        buttonHandler.HandleButtonClick();
    }
}
