using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIResourceInfoPanel : MonoBehaviour
{
    [SerializeField]
    public TMP_Text resourceAmountText;

    [SerializeField]
    public Image image;

    [SerializeField]
    public Image resourceImage;

    [SerializeField]
    public CanvasGroup backgroundCanvas;

    [SerializeField]
    public Image backgroundImage;

    [SerializeField]
    public RectTransform resourceTransform, allContents;

    public ResourceType resourceType;

    private UITooltipTrigger tooltipTrigger;

    private void Awake()
    {
        resourceAmountText.outlineColor = new Color(0f, 0f, 0f);
        resourceAmountText.outlineWidth = .1f;

    }

    public void SetResourceType(ResourceType type)
    {
        //won't work in awake for some reason
        if (!tooltipTrigger)
            tooltipTrigger = GetComponentInChildren<UITooltipTrigger>();
    
        resourceType = type;
        tooltipTrigger.SetMessage(ResourceHolder.Instance.GetName(type));
    }

    public void SetMessage(RocksType type)
    {
        switch (type)
        {
            case RocksType.Normal:
                tooltipTrigger.SetMessage("Common Minerals");
                break;
			case RocksType.Luxury:
				tooltipTrigger.SetMessage("Luxury Minerals");
				break;
			case RocksType.Chemical:
				tooltipTrigger.SetMessage("Radioactive Minerals");
				break;
		}
    }
}
