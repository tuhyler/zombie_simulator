using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UILaborAssignmentOptions : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    private int laborChange;
    public int LaborChange { get { return laborChange; } }

    private UILaborAssignment buttonHandler;
    private Button button;

    private CityBuilderManager cityBuilderManager;

    //[SerializeField]
    //private CanvasGroup canvasGroup; //handles all aspects of a group of UI elements together, instead of individually in Unity

    [SerializeField] //changing color of button when selected
    private Image buttonImage;
    private Color originalButtonColor;

    private bool isSelected, buttonIsWorking = true;
    [HideInInspector]
    public bool isFlashing;

    private void Awake()
    {
        buttonHandler = GetComponentInParent<UILaborAssignment>();
        //canvasGroup = GetComponent<CanvasGroup>();
        originalButtonColor = buttonImage.color;
        button = GetComponent<Button>();
    }

    public void SetCityBuilderManager(CityBuilderManager cityBuilderManager)
    {
        this.cityBuilderManager = cityBuilderManager;
    }

    public void ToggleInteractable(bool v)
    {
        button.interactable = v;
        if (!v)
            ToggleButtonSelection(v);
    }

    public void ToggleEnable(bool v)
    {
        buttonIsWorking = v;
    }

    //public void OnButtonClick()
    //{
    //    if (!isSelected)
    //    {
    //        ToggleButtonSelection(true);
    //        buttonHandler.PrepareLaborChange(laborChange);
    //    }
    //    else
    //    {
    //        ToggleButtonSelection(false);
    //    }
    //}

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.interactable || !buttonIsWorking)
            return;

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
        //buttonHandler.PrepareLaborChange(laborChange);
        //buttonHandler.HandleButtonClick();
    }

    public void ToggleButtonSelection(bool v)
    {
        //if (!button.interactable && !isSelected)
        //    return;
        
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
