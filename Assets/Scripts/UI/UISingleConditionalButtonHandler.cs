using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISingleConditionalButtonHandler : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup button;

    [SerializeField] //for tweening
    private RectTransform allContents;
    private bool activeStatus;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void ToggleTweenVisibility(bool v)
    {
        if (activeStatus == v)
            return;
        
        LeanTween.cancel(gameObject);
        
        if (v)
        {
            SetActiveStatusTrue();
            activeStatus = true;
            allContents.localScale = Vector3.zero;
            LeanTween.scale(allContents, Vector3.one, 0.25f).setDelay(0.125f).setEase(LeanTweenType.easeOutBack);
        }
        else
        {
            activeStatus = false;
            LeanTween.scale(allContents, Vector3.zero, 0.25f).setDelay(0.125f).setOnComplete(SetActiveStatusFalse);
        }
    }

    public void SetActiveStatusTrue()
    {
        gameObject.SetActive(true);
    }

    public void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void ToggleInteractable(bool v)
    {
        button.interactable = v;
    }
}
