using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIWorkerOptions : MonoBehaviour, IPointerClickHandler
{
    public string buttonName;
    private UIWorkerHandler buttonHandler;

    [SerializeField] //changing color of button when selected
    private Image buttonImage;
    public ParticleSystem buttonHighlight;
    public bool toggleColor;
    public bool showRemovalOptions;
    private Color originalButtonColor;
    private bool isSelected, isFlashing;

    private void Awake()
    {
        if (showRemovalOptions)
            buttonHandler = GetComponentInParent<UIWorkerHandler>();
        originalButtonColor = buttonImage.color;
    }

    public void FlashButton()
    {
        isFlashing = true;
        buttonHighlight.Play();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (toggleColor)
        {
            if (isSelected)
                ToggleColor(false);
            else
                ToggleColor(true);
        }

        if (isFlashing)
        {
            buttonHighlight.Stop();
            isFlashing = false;
        }
    }

    public void ToggleColor(bool v)
    {
        if (isSelected == v)
            return;

        if (isSelected)
        {
            isSelected = false;
            buttonImage.color = originalButtonColor;

            if (showRemovalOptions)
                buttonHandler.ToggleRemovalOptions(false);
        }
        else
        {
            buttonImage.color = Color.green;
            isSelected = true;
            
            if (showRemovalOptions)
                buttonHandler.ToggleRemovalOptions(true);
        }
    }
}
