using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;

public class UIMarketResourcePanel : MonoBehaviour
{
    //for sorting
    [HideInInspector]
    public string resourceName;
    [HideInInspector]
    public int price, amount;
    [HideInInspector]
    public bool sell;
    [HideInInspector]
    public ResourceType resourceType;

    //UI elements
    [SerializeField]
    public Image resourceImage;

    [SerializeField]
    public TMP_Text cityPrice, cityAmount;

    [SerializeField]
    public Toggle sellToggle;

    [SerializeField]
    public TMP_InputField minimumAmount;
    [SerializeField]
    private TMP_Text minimumAmountText;

    private UIMarketPlaceManager uiMarketPlaceManager;

    private void Start()
    {
        minimumAmount.onValidateInput += delegate (string input, int charIndex, char addedChar) { return PositiveIntCheck(addedChar); };
    }

    public void SetMarketPlaceManager(UIMarketPlaceManager uiMarketPlaceManager)
    {
        this.uiMarketPlaceManager = uiMarketPlaceManager;
    }

    public void UpdateSellBool()
    {
        if (uiMarketPlaceManager != null)
        {
            bool isOn = sellToggle.isOn;
            minimumAmount.interactable = isOn;
            minimumAmountText.color = isOn ? Color.black : Color.gray;
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
