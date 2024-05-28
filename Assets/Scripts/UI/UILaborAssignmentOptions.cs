using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UILaborAssignmentOptions : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    private int laborChange;
    public int LaborChange { get { return laborChange; } }

    [SerializeField]
    private UILaborAssignment buttonHandler;
    [SerializeField]
    private Button button;

    private CityBuilderManager cityBuilderManager;

    [SerializeField] //changing color of button when selected
    private Image buttonImage;
    private Color originalButtonColor;

    private bool isSelected/*, buttonIsWorking = true*/;
    [HideInInspector]
    public bool isFlashing;

    private void Awake()
    {
        //buttonHandler = GetComponentInParent<UILaborAssignment>();
        originalButtonColor = buttonImage.color;
    }

    public void SetCityBuilderManager(CityBuilderManager cityBuilderManager)
    {
        this.cityBuilderManager = cityBuilderManager;
    }

    public void Handle2()
    {
		if (cityBuilderManager.CityTypingCheck() && buttonHandler.activeStatus)
		{
			laborChange = 1;
            SelectButton();
		}
	}

    public void Handle3()
    {
        if (cityBuilderManager.CityTypingCheck() && buttonHandler.activeStatus)
        {
            laborChange = -1;
            SelectButton();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isSelected)
        {
            ToggleButtonSelection(true);
            buttonHandler.PrepareLaborChange(laborChange);
            buttonHandler.HandleButtonClick();
        }
        else
        {
            ToggleButtonSelection(false);
            cityBuilderManager.CloseLaborMenus();
        }

        cityBuilderManager.PlaySelectAudio();
    }

    public void SelectButton()
    {
		if (!isSelected)
		{
			ToggleButtonSelection(true);
			buttonHandler.PrepareLaborChange(laborChange);
			buttonHandler.HandleButtonClick();
		}
		else
		{
			ToggleButtonSelection(false);
			cityBuilderManager.CloseLaborMenus();
		}

		cityBuilderManager.PlaySelectAudio();
	}

	public void ToggleButtonSelection(bool v)
    {
        if (v)
        {
            if (!isSelected)
            {
                isSelected = true;

                Color colorToChange;

                if (laborChange >= 0)
                {
                    colorToChange = Color.green;
                }
                else
                {
                    colorToChange = new Color(1, .56f, .56f);//Color.red is too red;
                }

                buttonImage.color = colorToChange;

                if (isFlashing)
                    cityBuilderManager.world.ButtonFlashCheck();
            }
        }
        else
        {
            if (isSelected)
            {
                isSelected = false;
                buttonImage.color = originalButtonColor;
            }
        }
    }
}
