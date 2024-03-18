using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIShowTabHandler : MonoBehaviour, IPointerDownHandler
{
    public string tabName;
    
    [SerializeField]
    private UIBuilderHandler uiBuilder;
    public UIBuilderHandler UIBuilder { get { return uiBuilder; } }

    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private GameObject newIcon;
    [HideInInspector]
    public bool somethingNew, isFlashing;

    private UICityBuildTabHandler uiBuildTabHandler;

    //[SerializeField]
    //private CanvasGroup canvasGroup;

    [SerializeField] //changing color of button when selected
    private Image buttonImage;
    private Color originalButtonColor;
    private bool leftSideButton;
    private bool rightSideButton;
    public bool isSelling, isRemoving, isUpgrading, isUnits;

    private void Awake()
    {
        uiBuildTabHandler = GetComponentInParent<UICityBuildTabHandler>();
        originalButtonColor = buttonImage.color;
        if (isRemoving || isUpgrading)
            rightSideButton = true;
        else if (isSelling)
            leftSideButton = true;

        if (uiBuilder != null)
            uiBuilder.tabName = tabName;
    }

    public void ToggleInteractable(bool v)
    {
        canvasGroup.interactable = v;
    }

    public void ToggleSomethingNew(bool v)
    {
        somethingNew = v;
        newIcon.SetActive(v);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
		if (eventData.button == PointerEventData.InputButton.Left)
        {
		    if (!uiBuildTabHandler.buttonsAreWorking)
                return;

		    if (uiBuildTabHandler.cityBuilderManager.world.tutorialGoing)
            {
                if (isFlashing)
                {
                    isFlashing = false;
                    uiBuildTabHandler.cityBuilderManager.world.ButtonFlashCheck();
                }
                uiBuildTabHandler.cityBuilderManager.world.TutorialCheck("Open " + tabName + " Tab");
            }

		    ToggleButtonSelection(true);

            if (leftSideButton)
            {
                uiBuildTabHandler.StartLeftSideButton();
                uiBuildTabHandler.ShowUILeftSideButton();
            }
            else if (rightSideButton)
            {
                uiBuildTabHandler.StartRightSideButton(isRemoving);
                uiBuildTabHandler.ShowUIRightSideButton(isRemoving);
            }
            else
            {
                uiBuildTabHandler.PassUI(uiBuilder);

                if (isUnits)
                    uiBuildTabHandler.cityBuilderManager.CloseQueueUI();

                uiBuildTabHandler.ShowUI();
            }

            uiBuildTabHandler.SetSelectedTab(this);
            uiBuildTabHandler.cityBuilderManager.PlaySelectAudio();
        }
    }

    public void SelectTabKeyboardShortcut()
    {
		if (!uiBuildTabHandler.buttonsAreWorking)
			return;

		if (uiBuildTabHandler.cityBuilderManager.world.tutorialGoing)
		{
			if (isFlashing)
			{
				isFlashing = false;
				uiBuildTabHandler.cityBuilderManager.world.ButtonFlashCheck();
			}
			uiBuildTabHandler.cityBuilderManager.world.TutorialCheck("Open " + tabName + " Tab");
		}

		ToggleButtonSelection(true);

		if (leftSideButton)
		{
			uiBuildTabHandler.StartLeftSideButton();
			uiBuildTabHandler.ShowUILeftSideButton();
		}
		else if (rightSideButton)
		{
			uiBuildTabHandler.StartRightSideButton(isRemoving);
			uiBuildTabHandler.ShowUIRightSideButton(isRemoving);
		}
		else
		{
			uiBuildTabHandler.PassUI(uiBuilder);

			if (isUnits)
				uiBuildTabHandler.cityBuilderManager.CloseQueueUI();

			uiBuildTabHandler.ShowUI();
		}

		uiBuildTabHandler.SetSelectedTab(this);
		uiBuildTabHandler.cityBuilderManager.PlaySelectAudio();
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
