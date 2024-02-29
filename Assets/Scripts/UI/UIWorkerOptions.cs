using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIWorkerOptions : MonoBehaviour, IPointerClickHandler
{
    public string buttonName;
    private UIWorkerHandler buttonHandler;

    [SerializeField] //changing color of button when selected
    private Image buttonImage;
    public bool toggleColor;
    public bool showRemovalOptions;
    private Color originalButtonColor;
    [HideInInspector]
    public bool isSelected, isFlashing;
    private Button button;

    private void Awake()
    {
        buttonHandler = GetComponentInParent<UIWorkerHandler>();
        button = GetComponent<Button>();
        originalButtonColor = buttonImage.color;
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

        FlashCheck();
    }

    public void ToggleInteractable(bool v)
    {
        button.interactable = v;
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

    public void FlashCheck()
    {
		if (isFlashing)
		{
			isFlashing = false;
			buttonHandler.world.ButtonFlashCheck();
		}
	}
}
