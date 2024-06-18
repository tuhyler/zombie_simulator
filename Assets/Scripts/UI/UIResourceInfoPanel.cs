using System;
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
        resourceAmountText.outlineColor = Color.black;
        resourceAmountText.outlineWidth = .2f;
    }

    public void SetResourceAmount(int amount)
    {
        if (amount < 10000)
        {
            resourceAmountText.text = $"{amount:n0}";
        }
        else if (amount < 1000000)
        {
            resourceAmountText.text = Math.Round(amount * 0.001f, 1) + "k";
        }
        else if (amount < 1000000000)
        {
            resourceAmountText.text = Math.Round(amount * 0.000001f, 1) + "M";
        }
    }

    public void SetNegativeAmount(float amount)
    {
		if (amount < 10000)
		{
            double newAmount = Math.Round(amount, 0) * -1;
			resourceAmountText.text = $"{newAmount:n0}";
		}
		else if (amount < 1000000)
		{
			resourceAmountText.text = "-" + Math.Round(amount * 0.001f, 1) + "k";
		}
		else if (amount < 1000000000)
		{
			resourceAmountText.text = "-" + Math.Round(amount * 0.000001f, 1) + "M";
		}
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
