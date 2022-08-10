using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIPersonalResources : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    private TMP_Text resourceAmountText;
    private int resourceAmount;

    [SerializeField] //for tweening
    private RectTransform allContents;
    private bool activeStatus;

    [SerializeField]
    private ResourceType resourceType;
    public ResourceType ResourceType { get => resourceType; }

    [SerializeField]
    private CanvasGroup canvasGroup;

    private UIPersonalResourceInfoPanel buttonHandler;

    private void Awake()
    {
        SetButtonInteractable(false);
        buttonHandler = GetComponentInParent<UIPersonalResourceInfoPanel>();
    }

    public void CheckVisibility()
    {
        //buttonHandler.showingCount = 0;

        if (resourceAmount > 0) //convert string to integer
        {
            activeStatus = true;
            gameObject.SetActive(true);
        }
        else
        {
            activeStatus = false;
            gameObject.SetActive(false);
        }
    }

    public void SetButtonInteractable(bool v)
    {
        canvasGroup.interactable = v;
    }

    public void SetValue(int val)
    {
        resourceAmountText.text = val.ToString();
        resourceAmount = val;
    }

    public void UpdateValue(int val)
    {
        //int currentAmount = int.Parse(resourceAmountText.text);
        //currentAmount += val;
        if (val <= 0)
        {
            SetButtonInteractable(false);
            activeStatus = false;
            LeanTween.scale(gameObject, Vector3.zero, 0.2f).setEase(LeanTweenType.easeOutSine).setOnComplete(SetActiveStatusFalse);
            //gameObject.SetActive(false);
        }
        else if (!activeStatus && val > 0)
        {
            SetButtonInteractable(true);
            activeStatus = true;
            gameObject.SetActive(true);
            allContents.localScale = Vector3.zero;
            LeanTween.scale(allContents, Vector3.one, 0.2f).setEase(LeanTweenType.easeOutBack);
        }
        resourceAmountText.text = val.ToString();
        resourceAmount = val;
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void OnPointerClick()
    {
        buttonHandler.PrepareResource(resourceType);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        buttonHandler.PrepareResource(resourceType);
        buttonHandler.HandleButtonClick();
    }
}
