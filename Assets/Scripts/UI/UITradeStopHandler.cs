using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEditor.Experimental.GraphView;
using static UnityEngine.Rendering.DebugUI;

public class UITradeStopHandler : MonoBehaviour
{
    [SerializeField]
    public Image background, resourceButton;

    [SerializeField]
    public Sprite nextStopSprite, currentStopSprite;

    [SerializeField]
    public TMP_Dropdown cityNameList;

    [SerializeField]
    private GameObject tradeResourceHolder, tradeResourceTaskTemplate;

    //[SerializeField]
    //public TMP_InputField inputWaitTime;

    [SerializeField]
    public Slider waitSlider;

    [SerializeField]
    public Toggle waitForeverToggle;

    [SerializeField]
    public TMP_Text counter, waitTimeText;
    private int counterInt;

    [SerializeField]
    public GameObject progressBarHolder, addResourceButton, arrowUpButton, arrowDownButton;

    [SerializeField]
    private TMP_Text timeText;

    [SerializeField]
    private Image progressBarMask;

    //[HideInInspector]
    //public int loc;
    [HideInInspector]
    public UITradeRouteManager tradeRouteManager;
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
    public int resourceCount, currentResourceTask;

    //private List<TMP_Dropdown.OptionData> resources;
    private Dictionary<int, UITradeRouteResourceHolder> resourceTaskDict = new();

    private int waitTime;
    [HideInInspector]
    public bool waitForever = true, dragging;

    [SerializeField]
    private TMP_Dropdown.OptionData defaultFirstChoice;

    //for object pooling
    //private Queue<UITradeRouteResourceHolder> uiResourceTaskQueue = new();


    private void Awake()
    {
        timeText.outlineWidth = 0.5f;
        timeText.outlineColor = new Color(0, 0, 0, 255);
        progressBarHolder.SetActive(false);
        //GrowResourceTaskPool();
    }

    private void Start() 
    {
        //for checking if number is positive and integer
        //inputWaitTime.onValidateInput += delegate (string input, int charIndex, char addedChar) { return PositiveIntCheck(addedChar); };
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
        counterInt = num;
    }

    //public void AddResources(List<TMP_Dropdown.OptionData> resources)
    //{
    //    this.resources = new(resources);
    //}

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

    public void SetResourceAssignments(List<ResourceValue> resourceValues, bool onRoute)
    {
        foreach (ResourceValue resourceValue in resourceValues)
        {
            UITradeResourceTask resourceTask = AddResourceTaskPanel(onRoute);
            if (resourceTask != null)
            {
                resourceTask.SetCaptionResourceInfo(resourceValue);
            }
        }
    }

    public void WaitForever(bool v)
    {
        //inputWaitTime.interactable = !v;
        //inputWaitTime.text = "";
        waitSlider.gameObject.SetActive(!v);
        waitTimeText.gameObject.SetActive(!v);
        waitForever = v;
    }

    //private char PositiveIntCheck(char charToValidate) //ensuring numbers are positive
    //{
    //    if (charToValidate != '1'
    //        && charToValidate != '2'
    //        && charToValidate != '3'
    //        && charToValidate != '4'
    //        && charToValidate != '5'
    //        && charToValidate != '6'
    //        && charToValidate != '7'
    //        && charToValidate != '8'
    //        && charToValidate != '9'
    //        && charToValidate != '0')
    //    {
    //        charToValidate = '\0';
    //    }

    //    return charToValidate;
    //}

    public void SetWaitTimes(int waitTime)
    {
        this.waitTime = waitTime;

        if (waitTime < 0)
        {
            waitForeverToggle.isOn = true;
            waitForever = true;
            waitSlider.gameObject.SetActive(false);
            waitTimeText.gameObject.SetActive(false);
        }
        else
        {
            waitForever = false;
            waitSlider.gameObject.SetActive(true);
            waitTimeText.gameObject.SetActive(true);
            waitSlider.value = waitTime;
            waitTimeText.text = waitTime.ToString();
            //inputWaitTime.interactable = true;
            waitForeverToggle.isOn = false;
            //inputWaitTime.text = waitTime.ToString();
        }
    }

    public void SetSlider(float value)
    {
        float value2 = value * 0.01f;
        float b = 1.475561021f;
        float c = 8.821349657f;
        
        waitTime = Mathf.RoundToInt(b * (Mathf.Exp(c * value2) - 1));
        waitTimeText.text = waitTime.ToString();
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
        AddResourceTaskPanel(false);
    }

    private UITradeResourceTask AddResourceTaskPanel(bool onRoute) //showing a new resource task panel
    {
        if (resourceCount >= 10)
        {
            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Hit resource limit");
            return null;
        }

        //ChangeSize(true);

        GameObject newHolder = Instantiate(tradeResourceHolder);
        newHolder.transform.SetParent(transform, false);
        UITradeRouteResourceHolder resourceHolder = newHolder.GetComponent<UITradeRouteResourceHolder>();
        resourceHolder.SetStop(this);

        //UITradeRouteResourceHolder resourceHolder = GetFromResourceTaskPool();
        resourceTaskDict[resourceCount] = resourceHolder;
        resourceHolder.loc = resourceCount;
        
        GameObject newTask = Instantiate(tradeResourceTaskTemplate);
        newTask.transform.SetParent(resourceHolder.transform, false);
        //newTask.SetActive(true);
        //newTask.transform.SetParent(transform, false);


        //UITradeResourceTask newResourceTask = resourceHolder.GetComponentInChildren<UITradeResourceTask>();
        UITradeResourceTask newResourceTask = newTask.GetComponent<UITradeResourceTask>();
        resourceHolder.resourceTask = newResourceTask;
        newResourceTask.resourceHolder = resourceHolder; 
        //newResourceTask.AddResources(resources);
        newResourceTask.tempParent = tradeRouteManager.transform;
        newResourceTask.counter.text = (resourceCount + 1).ToString() + '.';
        newResourceTask.loc = resourceCount;
        //newResourceTask.cargoLimit = tradeRouteManager.traderCargoLimit;
        newResourceTask.resourceCountSlider.maxValue = tradeRouteManager.traderCargoLimit;
        //newResourceTask.SetCargoStorageLimit(traderCargoStorageLimit);
        uiResourceTasks.Add(newResourceTask);
        resourceCount++;

        //newResourceTask.transform.localPosition = Vector3.zero;
        //resourceHolder.transform.localPosition = Vector3.zero;
        //resourceHolder.transform.localScale = Vector3.one;
        //resourceHolder.transform.localEulerAngles = Vector3.zero;
        if (onRoute)
            PrepResource(newResourceTask);

        return newResourceTask;
    }

    public void SetAsNext()
    {
        background.sprite = nextStopSprite;
        background.color = new Color(1, 1, 1, 1);
        resourceButton.color = new Color(1, 1, 1, 1);
        for (int i = 0; i < uiResourceTasks.Count; i++)
        {
            uiResourceTasks[i].background.sprite = nextStopSprite;
            uiResourceTasks[i].background.color = new Color(1, 1, 1, 1);
            uiResourceTasks[i].completeImage.gameObject.SetActive(false);
        }
    }

    public void SetAsComplete()
    {
        background.sprite = nextStopSprite;
        background.color = new Color(1, 1, 1, 0.4f);
        resourceButton.color = new Color(1, 1, 1, 0.4f);
        for (int i = 0; i < uiResourceTasks.Count; i++)
        {
            uiResourceTasks[i].background.sprite = nextStopSprite;
            uiResourceTasks[i].background.color = new Color(1, 1, 1, 0.2f);
            uiResourceTasks[i].completeImage.gameObject.SetActive(false);
        }
    }

    public void SetAsCurrent(int currentResourceTask = 0, int amount = 0, int totalAmount = 0)
    {
        background.sprite = currentStopSprite;
        background.color = new Color(1, 1, 1, 1);
        resourceButton.color = new Color(1, 1, 1, 1);

        for (int i = 0; i < uiResourceTasks.Count; i++)
        {
            uiResourceTasks[i].background.sprite = currentStopSprite;
            uiResourceTasks[i].background.color = new Color(1, 1, 1, 1);
            if (i < currentResourceTask)
            {
                uiResourceTasks[i].completeImage.gameObject.SetActive(true);
                uiResourceTasks[i].SetCompleteFull();
            }
            else if (i == currentResourceTask)
            {
                uiResourceTasks[i].completeImage.gameObject.SetActive(true);
                uiResourceTasks[i].check.gameObject.SetActive(false);
                uiResourceTasks[i].SetCompletePerc(amount, totalAmount);
            }
            else
            {
                uiResourceTasks[i].completeImage.gameObject.SetActive(false);
            }
        }
    }

    public void SetTime(int time, int totalTime, bool forever)
    {
        timeText.text = string.Format("{0:00}:{1:00}", time / 60, time % 60);
        //int nextTime = (totalTime - time) + 1;

        if (!forever)
        {
            LeanTween.value(progressBarMask.gameObject, progressBarMask.fillAmount, (float)time / totalTime, 1f)
                .setEase(LeanTweenType.linear)
                .setOnUpdate((value) =>
                {
                    progressBarMask.fillAmount = value;
                });
        }
    }

    public void SetProgressBarMask(int time, int totalTime)
    {
        progressBarMask.fillAmount = (float)time / totalTime;
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

    //turns off functionality for going on route
    public void PrepResource(UITradeResourceTask task)
    {
        //task.allToggle.interactable = false;
        task.resourceCountSlider.interactable = false;
        //task.inputStorageAmount.enabled = false;
        task.draggable = false;
        task.dragGrips.SetActive(false);
        //task.resourceList.enabled = false;
        task.actionList.enabled = false;
    }

    public void PrepResources()
    {
        foreach (UITradeResourceTask task in uiResourceTasks)
        {
            PrepResource(task);
        }
    }

    //setting everything active again
    public void ResetResources()
    {
        foreach (UITradeResourceTask task in uiResourceTasks) 
        {
            //task.allToggle.interactable = true;
            task.resourceCountSlider.interactable = true;
            //task.inputStorageAmount.enabled = true;
            task.draggable = true;
            task.dragGrips.SetActive(true);
            //task.resourceList.enabled = true;
            task.actionList.enabled = true;
        }
    }

    //for deleting resources
    public void AdjustResources(int oldNum)
    {
        for (int i = oldNum; i < uiResourceTasks.Count; i++)
        {
            uiResourceTasks[i].loc -= 1;
            uiResourceTasks[i].resourceHolder.loc -= 1;
            uiResourceTasks[i].counter.text = (i + 1).ToString() + '.';
            resourceTaskDict[i] = resourceTaskDict[i + 1];
        }

        resourceTaskDict.Remove(uiResourceTasks.Count); //remove last one
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

        //waitTime = int.Parse(waitTimeText.text);

        if (waitForever)
        {
            waitTime = -1;
        }
        //else
        //{
        //    if (int.TryParse(chosenWaitTime, out int result))
        //    {
        //        waitTime = result;
        //    }
        //    else //silly workaround to remove trailing invisible char ("Trim" doesn't work)
        //    {
        //        string stringAmount = "0";
        //        int i = 0;

        //        foreach (char c in chosenWaitTime)
        //        {
        //            if (i == chosenWaitTime.Length - 1)
        //                continue;

        //            stringAmount += c;
        //            Debug.Log("letter " + c);
        //            i++;
        //        }

        //        waitTime = int.Parse(stringAmount);
        //    }
        //}

        return (chosenCity, chosenResourceValues, waitTime);
    }

    public void RemoveResource(UITradeResourceTask uiTradeResourceTask)
    {
        //ChangeSize(false);
        uiResourceTasks.Remove(uiTradeResourceTask);
    }

    private void ResetStop()
    {
        cityNameList.options.Insert(0, defaultFirstChoice);
        cityNameList.value = 0;
        cityNameList.RefreshShownValue();
        waitForeverToggle.isOn = true;
        waitForever = true;
        waitTime = 0;
        waitSlider.gameObject.SetActive(true);
        waitTimeText.gameObject.SetActive(true);
        //inputWaitTime.interactable = false;
        //inputWaitTime.text = "";
    }

    public void CloseWindow()
    {
        CloseWindow(true);
    }

    public void CloseWindow(bool justOne)
    {
        if (justOne)
        {
            TMP_Dropdown.OptionData option = tradeRouteManager.chosenStop.options.Find((x) => x.text == tradeRouteManager.stopCount.ToString());
            tradeRouteManager.chosenStop.options.Remove(option);
            tradeRouteManager.UpdateStopNumbers(counterInt);
        }

        int taskCount = uiResourceTasks.Count;
        for (int i = 0; i < taskCount; i++)
            uiResourceTasks[0].resourceHolder.CloseWindow(false); //do the first one till list is empty

        tradeRouteManager.stopCount--;
        if (tradeRouteManager.stopCount == 0)
            tradeRouteManager.startingStopGO.SetActive(false);
        ResetStop();
        tradeRouteManager.tradeStopHandlerList.Remove(this);
        tradeRouteManager.AddToTradeStopPool(this);
        //Destroy(gameObject);
    }

    //Object pooling resources //no object pooling for resource tasks because otherwise UI moves too slowly (even when inactive)
    //private void GrowResourceTaskPool()
    //{
    //    for (int i = 0; i < 5; i++) //grow pool 5 at a time
    //    {
    //        GameObject newHolder = Instantiate(tradeResourceHolder);
    //        newHolder.transform.SetParent(transform, false);
    //        UITradeRouteResourceHolder resourceHolder = newHolder.GetComponent<UITradeRouteResourceHolder>();
    //        resourceHolder.SetStop(this);
    //        GameObject newTask = Instantiate(tradeResourceTaskTemplate);
    //        newTask.transform.SetParent(resourceHolder.transform, false);
    //        AddToResourceTaskPool(resourceHolder);
    //    }
    //}

    //public void AddToResourceTaskPool(UITradeRouteResourceHolder newResourceTask)
    //{
    //    newResourceTask.gameObject.SetActive(false);
    //    uiResourceTaskQueue.Enqueue(newResourceTask);
    //}

    //private UITradeRouteResourceHolder GetFromResourceTaskPool()
    //{
    //    if (uiResourceTaskQueue.Count == 0)
    //        GrowResourceTaskPool();

    //    UITradeRouteResourceHolder newResourceTask = uiResourceTaskQueue.Dequeue();
    //    newResourceTask.gameObject.SetActive(true);
    //    return newResourceTask;
    //}
}
