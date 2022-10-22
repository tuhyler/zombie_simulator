using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;

public class UITradeStopHandler : MonoBehaviour
{
    [SerializeField]
    private TMP_Dropdown cityNameList;

    [SerializeField]
    private GameObject tradeResourceTaskTemplate;

    [SerializeField]
    private TMP_InputField inputWaitTime;

    [SerializeField]
    private Toggle waitForeverToggle;

    //[SerializeField]
    //private Transform resourceDetailHolder;

    private List<string> cityNames;

    private int traderCargoStorageLimit;

    private string chosenCity;
    //private string chosenWaitTime;

    //handling all the resource info for this stop
    private List<UITradeResourceTask> uiResourceTasks = new();

    private List<TMP_Dropdown.OptionData> resources;

    private int waitTime;
    private bool waitForever = true;

    [SerializeField]
    private TMP_Dropdown.OptionData defaultFirstChoice;


    private void Start() 
    {
        //for checking if number is positive and integer
        inputWaitTime.onValidateInput += delegate (string input, int charIndex, char addedChar) { return PositiveIntCheck(addedChar); };
        //inputWaitTime.interactable = false;
    }

    public void MoveStopUp()
    {
        int placement = transform.GetSiblingIndex();
        if (placement == 0)
            return;

        transform.SetSiblingIndex(placement-1);
    }

    public void MoveStopDown()
    {
        int placement = transform.GetSiblingIndex();
        if (placement == transform.parent.childCount - 1)
            return;

        transform.SetSiblingIndex(placement + 1);
    }

    public void AddResources(List<TMP_Dropdown.OptionData> resources)
    {
        this.resources = new(resources);
    }

    public void SetChosenCity(int value)
    {
        bool newValue = false;

        if (cityNameList.options.Contains(defaultFirstChoice))
        {
            newValue = true;
            if (value == 0) return;
            chosenCity = cityNames[value - 1];
        }
        else
        {
            chosenCity = cityNames[value];
        }

        cityNameList.options.Remove(defaultFirstChoice);
        if (newValue)
        {
            cityNameList.value = value - 1;
            cityNameList.RefreshShownValue();
        }
    }

    public void SetCaptionCity(string cityName)
    {
        cityNameList.options.Remove(defaultFirstChoice);

        chosenCity = cityName;
        cityNameList.value = cityNames.IndexOf(chosenCity);
        cityNameList.RefreshShownValue();
    }

    public void SetResourceAssignments(List<ResourceValue> resourceValues)
    {
        foreach (ResourceValue resourceValue in resourceValues)
        {
            AddResourceTaskPanel().SetCaptionResourceInfo(resourceValue);
        }
    }

    //public void SetInputWaitTime(string value)
    //{
    //    chosenWaitTime = value.Trim();
    //}

    public void SetCargoStorageLimit(int cargoLimit)
    {
        traderCargoStorageLimit = cargoLimit;
    }

    public void WaitForever(bool v)
    {
        inputWaitTime.interactable = !v;
        inputWaitTime.text = "";
        waitForever = v;
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

    public void SetWaitTimes(int waitTime)
    {
        this.waitTime = waitTime;

        if (waitTime < 0)
            waitForeverToggle.isOn = true;
        else
        {
            inputWaitTime.interactable = true;
            waitForeverToggle.isOn = false;
            inputWaitTime.text = waitTime.ToString();
        }
    }

    private void PrepareNameList()
    {
        cityNameList.ClearOptions();
        cityNameList.options.Add(defaultFirstChoice);
    }

    public void AddCityNames(List<string> cityNames)
    {
        PrepareNameList();
        this.cityNames = new(cityNames);
        cityNameList.AddOptions(cityNames);
    }


    public void AddResourceTaskPanelButton() //added this as a method attached to button can't return anything
    {
        AddResourceTaskPanel();
    }

    private UITradeResourceTask AddResourceTaskPanel() //showing a new resource task panel
    {
        GameObject newTask = Instantiate(tradeResourceTaskTemplate);
        newTask.SetActive(true);
        newTask.transform.SetParent(transform, false);

        UITradeResourceTask newResourceTask = newTask.GetComponent<UITradeResourceTask>();
        newResourceTask.AddResources(resources);
        newResourceTask.SetCargoStorageLimit(traderCargoStorageLimit);
        uiResourceTasks.Add(newResourceTask);

        return newResourceTask;
    }

    //sending all the resource information for each stop
    public (string, List<ResourceValue>, int) GetStopInfo()
    {
        List<ResourceValue> chosenResourceValues = new();

        foreach (UITradeResourceTask resourceTask in uiResourceTasks)
        {
            ResourceValue resourceValue = resourceTask.GetResourceTasks();
            if (resourceValue.resourceType == ResourceType.None)
                continue;

            chosenResourceValues.Add(resourceValue);
        }

        string chosenWaitTime = inputWaitTime.text;

        if (waitForever)
        {
            waitTime = -1;
        }
        else
        {
            if (int.TryParse(chosenWaitTime, out int result))
            {
                waitTime = result;
            }
            else //silly workaround to remove trailing invisible char ("Trim" doesn't work)
            {
                string stringAmount = "0";
                int i = 0;

                foreach (char c in chosenWaitTime)
                {
                    if (i == chosenWaitTime.Length - 1)
                        continue;

                    stringAmount += c;
                    Debug.Log("letter " + c);
                    i++;
                }

                waitTime = int.Parse(stringAmount);
            }
        }

        return (chosenCity, chosenResourceValues, waitTime);
    }

    public void CloseWindow()
    {
        uiResourceTasks.Clear();
        cityNames.Clear();
        resources.Clear();
        //gameObject.SetActive(false);
        Destroy(gameObject);
    }

}
