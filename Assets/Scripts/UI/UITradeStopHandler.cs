using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEditor.Experimental.GraphView;

public class UITradeStopHandler : MonoBehaviour
{
    [SerializeField]
    public Image background, resourceButton;

    [SerializeField]
    public Sprite nextStopSprite, currentStopSprite, completedStopSprite, nextResource, currentResource, completedResource;

    [SerializeField]
    private TMP_Dropdown cityNameList;

    [SerializeField]
    private GameObject tradeResourceHolder, tradeResourceTaskTemplate;

    [SerializeField]
    private TMP_InputField inputWaitTime;

    [SerializeField]
    private Toggle waitForeverToggle;

    [SerializeField]
    public TMP_Text counter;

    [SerializeField]
    public GameObject progressBarHolder;

    [SerializeField]
    private TMP_Text timeText;

    [SerializeField]
    private Image progressBarMask;

    //[HideInInspector]
    //public int loc;

    private UITradeRouteManager tradeRouteManager;
    //private UITradeRouteStopHolder tradeStopHolder;

    //[SerializeField]
    //private Transform resourceDetailHolder;

    private List<string> cityNames;

    //private int traderCargoStorageLimit;

    private string chosenCity;
    //private string chosenWaitTime;

    //handling all the resource info for this stop
    [HideInInspector]
    public List<UITradeResourceTask> uiResourceTasks = new();
    [HideInInspector]
    public int resourceCount;

    private List<TMP_Dropdown.OptionData> resources;
    private Dictionary<int, UITradeRouteResourceHolder> resourceTaskDict = new();

    private int waitTime;
    private bool waitForever = true;

    [SerializeField]
    private TMP_Dropdown.OptionData defaultFirstChoice;


    private void Awake()
    {
        timeText.outlineWidth = 0.5f;
        timeText.outlineColor = new Color(0, 0, 0, 255);
        progressBarHolder.SetActive(false);
    }

    private void Start() 
    {
        //for checking if number is positive and integer
        inputWaitTime.onValidateInput += delegate (string input, int charIndex, char addedChar) { return PositiveIntCheck(addedChar); };
        //inputWaitTime.interactable = false;
    }

    public void SetTradeRouteManager(UITradeRouteManager tradeRouteManager)
    {
        this.tradeRouteManager = tradeRouteManager;
    }

    //public void SetStopHolder(UITradeRouteStopHolder tradeStopHolder)
    //{
    //    this.tradeStopHolder = tradeStopHolder;
    //}

    public void MoveStopUp()
    {
        int placement = transform.GetSiblingIndex();
        if (placement == 0)
            return;

        ChangeCounter(placement);
        transform.SetSiblingIndex(placement - 1);
        tradeRouteManager.MoveStop(placement, true);
    }

    public void MoveStopDown()
    {
        int placement = transform.GetSiblingIndex();
        if (placement == tradeRouteManager.stopCount - 1)
            return;

        ChangeCounter(placement + 2);
        transform.SetSiblingIndex(placement + 1);
        tradeRouteManager.MoveStop(placement + 2, false);
    }

    public void ChangeCounter(int num)
    {
        counter.text = num.ToString();
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

    public void SetResourceAssignments(List<ResourceValue> resourceValues, bool completed, bool current)
    {
        foreach (ResourceValue resourceValue in resourceValues)
        {
            UITradeResourceTask resourceTask = AddResourceTaskPanel();
            if (resourceTask != null)
            {
                resourceTask.SetCaptionResourceInfo(resourceValue);
                if (completed)
                    resourceTask.background.sprite = completedStopSprite;
                else if (current)
                    resourceTask.background.sprite = currentStopSprite;
                else
                    resourceTask.background.sprite = nextStopSprite;
            }
        }
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
        if (resourceCount >= 20)
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 10f; 
            Vector3 mouseLoc = Camera.main.ScreenToWorldPoint(mousePos);
            InfoPopUpHandler.WarningMessage().Create(mouseLoc, "Hit resource limit");

            return null;
        }

        //ChangeSize(true);

        GameObject newHolder = Instantiate(tradeResourceHolder);
        newHolder.transform.SetParent(transform, false);
        UITradeRouteResourceHolder resourceHolder = newHolder.GetComponent<UITradeRouteResourceHolder>();
        resourceHolder.SetStop(this);
        resourceTaskDict[resourceCount] = resourceHolder;
        resourceHolder.loc = resourceCount;
        
        GameObject newTask = Instantiate(tradeResourceTaskTemplate);
        newTask.transform.SetParent(resourceHolder.transform, false);
        //newTask.SetActive(true);
        //newTask.transform.SetParent(transform, false);


        UITradeResourceTask newResourceTask = newTask.GetComponent<UITradeResourceTask>();
        resourceHolder.resourceTask = newResourceTask;
        newResourceTask.resourceHolder = resourceHolder; 
        newResourceTask.AddResources(resources);
        newResourceTask.tempParent = tradeRouteManager.transform;
        newResourceTask.counter.text = (resourceCount + 1).ToString() + '.';
        newResourceTask.loc = resourceCount;
        //newResourceTask.SetCargoStorageLimit(traderCargoStorageLimit);
        uiResourceTasks.Add(newResourceTask);
        resourceCount++;

        //newResourceTask.transform.localPosition = Vector3.zero;
        //resourceHolder.transform.localPosition = Vector3.zero;
        //resourceHolder.transform.localScale = Vector3.one;
        //resourceHolder.transform.localEulerAngles = Vector3.zero;
        return newResourceTask;
    }

    public void SetAsNext()
    {
        background.sprite = nextStopSprite;
        resourceButton.sprite = nextResource;
    }

    public void SetAsComplete()
    {
        background.sprite = completedStopSprite;
        resourceButton.sprite = completedResource;
    }

    public void SetAsCurrent()
    {
        background.sprite = currentStopSprite;
        resourceButton.sprite = currentResource;
    }

    public void SetTime(int time, int totalTime)
    {
        timeText.text = string.Format("{0:00}:{1:00}", time / 60, time % 60);
        int nextTime = (totalTime - time) + 1;
        float totalTimeFactor = 1f / totalTime;

        LeanTween.value(progressBarMask.gameObject, progressBarMask.fillAmount, nextTime * totalTimeFactor, 1f)
            .setEase(LeanTweenType.linear)
            .setOnUpdate((value) =>
            {
                progressBarMask.fillAmount = value;
            });
    }

    public void SetProgressBarMask(int time, int totalTime)
    {
        progressBarMask.fillAmount = (totalTime - time) * (1f / totalTime);
    }

    public void MoveResourceTask(int oldNum, int newNum)
    {
        //shifting all the other resources
        if (oldNum > newNum)
        {
            for (int i = oldNum; i > newNum; i--)
            {
                int next = i - 1;
                resourceTaskDict[next].MoveResourceTask(resourceTaskDict[i]);
            }
        }
        else
        {
            for (int i = oldNum; i < newNum; i++)
            {
                int next = i + 1;
                resourceTaskDict[next].MoveResourceTask(resourceTaskDict[i]);
            }
        }
    }

    //for deleting resources
    public void AdjustResources(int oldNum)
    {
        for (int i = oldNum; i < uiResourceTasks.Count; i++)
        {
            uiResourceTasks[i].loc -= 1;
            uiResourceTasks[i].counter.text = (i + 1).ToString() + '.';
        }
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

    public void RemoveResource(UITradeResourceTask uiTradeResourceTask)
    {
        //ChangeSize(false);
        uiResourceTasks.Remove(uiTradeResourceTask);
    }

    public void CloseWindow()
    {
        tradeRouteManager.stopCount--;
        tradeRouteManager.tradeStopHandlerList.Remove(this);
        Destroy(gameObject);
    }

    //public void ChangeSize(bool increase)
    //{
    //    int factor = increase ? 1 : -1;

    //    Vector2 currentSize = allContents.rect.size;
    //    currentSize.y += 70 * factor;
    //    allContents.sizeDelta = currentSize;
    //}
}
