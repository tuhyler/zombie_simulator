using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITradeResourceTask : MonoBehaviour
{
    [SerializeField]
    private TMP_Dropdown resourceList;

    [SerializeField]
    private TMP_Dropdown actionList;

    [SerializeField]
    private TMP_InputField inputStorageAmount;

    //[SerializeField]
    //private Image resourceIcon;

    private List<string> resources = new();

    private string chosenResource;
    private int chosenMultiple = 1;
    //private string chosenAmount;
    private int traderCargoStorageLimit;

    //private ResourceHolder resourceHolder;

    [SerializeField]
    private TMP_Dropdown.OptionData defaultFirstChoice;

    private bool getAll;


    private void Start()
    {
        //for checking if number is positive and integer
        inputStorageAmount.onValidateInput += delegate (string input, int charIndex, char addedChar) { return PositiveIntCheck(addedChar); };
        inputStorageAmount.interactable = true;
    }

    public void SetChosenResource(int value)
    {
        bool newValue = false;

        if (resourceList.options.Contains(defaultFirstChoice))
        {
            newValue = true;
            if (value == 0) return;
            chosenResource = resources[value - 1];
        }
        else
        {
            chosenResource = resources[value];
        }

        resourceList.options.Remove(defaultFirstChoice); //removing first choice command from list
        if (newValue)
        {
            resourceList.value = value - 1;
            resourceList.RefreshShownValue();
        }
    }

    public void SetChosenResourceMultiple(int value)
    {
        if (value == 1)
            chosenMultiple = -1;
    }

    //public void SetChosenResourceAmount(string value)
    //{
    //    chosenAmount = value;
    //}

    public void SetCargoStorageLimit(int cargoLimit)
    {
        traderCargoStorageLimit = cargoLimit;
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

    public void SetCaptionResourceInfo(ResourceValue resourceValue)
    {
        resourceList.options.Remove(defaultFirstChoice); //removing top choice

        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
        {
            if (resourceValue.resourceType == resource.resourceType)
            {
                chosenResource = resource.resourceName;
                resourceList.value = resources.IndexOf(chosenResource);
                if (resourceValue.resourceAmount < 0)
                    actionList.value = 1;
                resourceList.RefreshShownValue();

                inputStorageAmount.text = Mathf.Abs(resourceValue.resourceAmount).ToString();
            }
        }
    }

    private void PrepareResourceList()
    {
        resourceList.ClearOptions();
        resourceList.options.Add(defaultFirstChoice);
    }

    public void AddResources(List<TMP_Dropdown.OptionData> resources)
    {
        PrepareResourceList();

        foreach (var resource in resources)
        {
            this.resources.Add(resource.text);
        }

        resourceList.AddOptions(resources);
    }

    public void GetAllOfResource(bool v)
    {
        inputStorageAmount.interactable = !v;
        inputStorageAmount.text = "";
        getAll = v;
    }

    public ResourceValue GetResourceTasks()
    {
        ResourceValue resourceValue;

        resourceValue.resourceType = ResourceType.None;
        resourceValue.resourceAmount = 0;

        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
        {
            if (chosenResource == resource.resourceName)
                resourceValue.resourceType = resource.resourceType;
        }

        string chosenAmount = inputStorageAmount.text;

        if (getAll) //get all of the resource
        {
            resourceValue.resourceAmount = traderCargoStorageLimit;
        }
        else
        {
            if (int.TryParse(chosenAmount, out int result))
            {
                //if (inputAmount == "")
                //    result = 0;
                resourceValue.resourceAmount = result;
            }
            else //silly workaround to remove trailing invisible char ("Trim" doesn't work)
            {
                string stringAmount = "0";
                int i = 0;

                foreach (char c in chosenAmount)
                {
                    if (i == chosenAmount.Length - 1)
                        continue;

                    stringAmount += c;
                    Debug.Log("letter " + c);
                    i++;
                }

                resourceValue.resourceAmount = int.Parse(stringAmount);
            }
        }

        resourceValue.resourceAmount *= chosenMultiple;
        return resourceValue;
    }

    public void CloseWindow()
    {
        //resourceHolder = null;
        resources.Clear();
        Destroy(gameObject);
    }

}
