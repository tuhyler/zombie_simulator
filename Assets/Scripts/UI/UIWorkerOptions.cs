using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIWorkerOptions : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private ImprovementDataSO buildData;
    public ImprovementDataSO BuildData { get { return buildData; } }

    private UIWorkerHandler buttonHandler;

    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField] //changing color of button when selected
    private Image buttonImage;
    private Color originalButtonColor;
    private bool isSelected;

    private void Awake()
    {
        buttonHandler = GetComponentInParent<UIWorkerHandler>();
        canvasGroup = GetComponent<CanvasGroup>();
        originalButtonColor = buttonImage.color;

        if (buildData == null)
            buttonHandler.SetRoadBuildOption(this);
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

        if (buildData == null)
        {
            if (isSelected)
                ToggleColor(false);
            else
                ToggleColor(true);
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
            buttonHandler.ToggleRemovalOptions(false);
        }
        else
        {
            buttonImage.color = Color.green;
            isSelected = true;
            buttonHandler.ToggleRemovalOptions(true);
        }
    }
}
