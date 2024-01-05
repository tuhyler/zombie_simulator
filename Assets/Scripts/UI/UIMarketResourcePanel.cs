using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMarketResourcePanel : MonoBehaviour
{
    //for sorting
    [HideInInspector]
    public string resourceName;
    [HideInInspector]
    public int price, amount/*, total*/;
    [HideInInspector]
    public bool sell;
    [HideInInspector]
    public ResourceType resourceType;

    //UI elements
    [SerializeField]
    public Image resourceImage;

    [SerializeField]
    public TMP_Text cityPrice, cityAmount/*, cityTotals*/;

    [SerializeField]
    public Toggle sellToggle;

    [SerializeField]
    public TMP_InputField minimumAmount;
    //[SerializeField]
    //private TMP_Text minimumAmountText;

    private UITooltipTrigger tooltipTrigger;
    private UIMarketPlaceManager uiMarketPlaceManager;

	private void Awake()
	{
        tooltipTrigger = GetComponentInChildren<UITooltipTrigger>();
	}

	private void Start()
    {
        minimumAmount.onValidateInput += delegate (string input, int charIndex, char addedChar) { return PositiveIntCheck(addedChar); };
    }

    public void SetMarketPlaceManager(UIMarketPlaceManager uiMarketPlaceManager)
    {
        this.uiMarketPlaceManager = uiMarketPlaceManager;
    }

    public void SetResourceType(ResourceType type, string resourceName)
    {
        resourceType = type;
        this.resourceName = resourceName;

        if (tooltipTrigger == null)
            tooltipTrigger = GetComponentInChildren<UITooltipTrigger>();

        tooltipTrigger.SetMessage(resourceName);
    }

    public void SetPrice(int amount)
    {
        price = amount;
        
        if (amount < 1000)
		{
			cityPrice.text = amount.ToString();
		}
		else if (amount < 1000000)
		{
			cityPrice.text = Math.Round(amount * 0.001f, 1) + " k";
		}
		else if (amount < 1000000000)
		{
			cityPrice.text = Math.Round(amount * 0.000001f, 1) + " M";
		}
	}

	public void SetAmount(int amount)
	{
		this.amount = amount;

		if (amount < 1000)
		{
			cityAmount.text = amount.ToString();
		}
		else if (amount < 1000000)
		{
			cityAmount.text = Math.Round(amount * 0.001f, 1) + " k";
		}
		else if (amount < 1000000000)
		{
			cityAmount.text = Math.Round(amount * 0.000001f, 1) + " M";
		}
	}

	public void UpdateSellBool()
    {
        if (uiMarketPlaceManager != null)
        {
            uiMarketPlaceManager.city.world.cityBuilderManager.PlayCheckAudio();
            bool isOn = sellToggle.isOn;
            minimumAmount.gameObject.SetActive(isOn);
            //minimumAmount.interactable = isOn;
            //minimumAmountText.color = isOn ? Color.black : Color.gray;
            uiMarketPlaceManager.SetResourceSell(resourceType, isOn);
        }
    }

    public void UpdateMinHold()
    {
        string chosenMinimumAmount = minimumAmount.text;
        int finalMinimumAmount;

        if (int.TryParse(chosenMinimumAmount, out int result))
        {
            finalMinimumAmount = result;
        }
        else //silly workaround to remove trailing invisible char ("Trim" doesn't work)
        {
            string stringAmount = "0";
            int i = 0;

            foreach (char c in chosenMinimumAmount)
            {
                if (i == chosenMinimumAmount.Length - 1)
                    continue;

                stringAmount += c;
                Debug.Log("letter " + c);
                i++;
            }

            finalMinimumAmount = int.Parse(stringAmount);
        }

        uiMarketPlaceManager.SetResourceMinHold(resourceType, finalMinimumAmount);
    }

    private char PositiveIntCheck(char charToValidate) //ensuring numbers are positive
    {
        if (charToValidate != '1'
            && charToValidate != '2'
            && charToValidate != '3'
            && charToValidate != '4'
            && charToValidate != '5'
            && charToValidate != '6'
            && charToValidate != '7'
            && charToValidate != '8'
            && charToValidate != '9'
            && charToValidate != '0')
        {
            charToValidate = '\0';
        }

        return charToValidate;
    }
}
