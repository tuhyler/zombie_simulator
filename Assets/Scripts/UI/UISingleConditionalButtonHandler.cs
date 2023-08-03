using UnityEngine;
using UnityEngine.UI;

public class UISingleConditionalButtonHandler : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup button;

    [SerializeField] //for tweening
    public RectTransform allContents;
    [HideInInspector]
    public bool activeStatus;

    [SerializeField] //changing color of button when selected
    public Image buttonImage;
    private Color originalButtonColor;


    private void Awake()
    {
        gameObject.SetActive(false);
        originalButtonColor = buttonImage.color;
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
            buttonImage.color = originalButtonColor;
            allContents.localScale = Vector3.zero;
            LeanTween.scale(allContents, Vector3.one, 0.25f).setDelay(0.125f).setEase(LeanTweenType.easeOutBack);
        }
        else
        {
            activeStatus = false;
            LeanTween.scale(allContents, Vector3.zero, 0.25f).setDelay(0.125f).setOnComplete(SetActiveStatusFalse);
        }
    }

    public void ToggleButtonColor(bool v)
    {
        if (v)
            buttonImage.color = Color.green;
        else
            buttonImage.color = originalButtonColor;
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
