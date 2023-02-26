using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIQueueButton : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    private UIQueueManager uiQueueManager;

    private UICityBuildTabHandler uiBuildTabHandler;

    [SerializeField] //changing color of button when selected
    private Image buttonImage;
    private Color originalButtonColor;

    private bool isSelected;

    private void Awake()
    {
        uiBuildTabHandler = GetComponentInParent<UICityBuildTabHandler>();
        originalButtonColor = buttonImage.color;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!uiBuildTabHandler.buttonsAreWorking)
            return;

        ToggleButtonSelection(!isSelected);
    }

    public void ToggleButtonSelection(bool v)
    {
        if (isSelected == v)
            return;
        
        if (v)
        {
            isSelected = true;
            buttonImage.color = Color.green;
            uiBuildTabHandler.CloseRemovalWindow();
            uiQueueManager.ToggleVisibility(true);
        }
        else
        {
            isSelected = false;
            buttonImage.color = originalButtonColor;
            uiBuildTabHandler.cityBuilderManager.CloseQueueUI();
            //uiQueueManager.ToggleVisibility(false);
        }
    }
}
