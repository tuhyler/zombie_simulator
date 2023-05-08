using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UITradeResourceTask : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]
    public TMP_Dropdown resourceList;

    [SerializeField]
    public TMP_Dropdown actionList;

    [SerializeField]
    public TMP_InputField inputStorageAmount;

    [SerializeField]
    public Toggle allToggle;

    [SerializeField]
    public TMP_Text counter;

    [SerializeField]
    public Image background, completeImage, check;

    [SerializeField]
    public GameObject dragGrips;

    //[SerializeField]
    //private Image resourceIcon;

    //for moving resource task
    [HideInInspector]
    public int loc;
    [HideInInspector]
    public Transform originalParent, tempParent;
    [HideInInspector]
    public UITradeRouteResourceHolder resourceHolder;

    [HideInInspector]
    public List<string> resources = new();

    private string chosenResource;
    private int chosenMultiple = 1;
    //private string chosenAmount;
    //private int traderCargoStorageLimit;

    //private ResourceHolder resourceHolder;

    [SerializeField]
    private TMP_Dropdown.OptionData defaultFirstChoice;

    private bool getAll;
    [HideInInspector]
    public bool draggable = true;

    private void Awake()
    {
        counter.outlineColor = Color.black;//new Color(0.2f, 0.2f, 0.2f);
        counter.outlineWidth = 0.3f;
        check.gameObject.SetActive(false);
        completeImage.gameObject.SetActive(false);
        allToggle.gameObject.SetActive(false);
    }

    private void Start()
    {
        //for checking if number is positive and integer
        inputStorageAmount.onValidateInput += delegate (string input, int charIndex, char addedChar) { return PositiveIntCheck(addedChar); };
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (draggable)
        {
            originalParent = transform.parent;
            transform.SetParent(tempParent);
            transform.SetAsLastSibling();
            background.raycastTarget = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggable)
        {
            Vector3 p = Input.mousePosition;
            p.z = 935;
            Vector3 pos = Camera.main.ScreenToWorldPoint(p);
            pos.x += 250;
            transform.position = pos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggable)
        {
            transform.SetParent(originalParent);
            transform.localPosition = Vector3.zero;
            background.raycastTarget = true;
        }
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
        {
            chosenMultiple = -1;
            allToggle.gameObject.SetActive(true);
        }
        else
        {
            chosenMultiple = 1;
            allToggle.gameObject.SetActive(false);
        }
    }

    public void SetAmount(int amount, int totalAmount)
    {
        float perc = (float)(amount) / totalAmount;
        
        if (amount == totalAmount)
        {
            LeanTween.value(completeImage.gameObject, completeImage.fillAmount, perc, 1f)
                .setEase(LeanTweenType.linear)
                .setOnUpdate((value) =>
                {
                    completeImage.fillAmount = value;
                }).setOnComplete(SetCheck);
        }
        else
        {
            LeanTween.value(completeImage.gameObject, completeImage.fillAmount, perc, 1f)
                .setEase(LeanTweenType.linear)
                .setOnUpdate((value) =>
                {
                    completeImage.fillAmount = value;
                });
        }
    }

    public void SetCompletePerc(int amount, int totalAmount)
    {
        if (totalAmount == 0)
            totalAmount = 1;
        completeImage.fillAmount = (float)amount / totalAmount;
    }

    public void SetCompleteFull()
    {
        completeImage.fillAmount = 1;
        SetCheck();
    }

    public void SetCheck()
    {
        check.gameObject.SetActive(true);
        check.transform.localScale = Vector3.zero;
        LeanTween.scale(check.gameObject, Vector3.one, 0.25f).setEase(LeanTweenType.easeOutBack);
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
                {
                    actionList.value = 1;
                    allToggle.gameObject.SetActive(true);
                }
                resourceList.RefreshShownValue();

                if (Mathf.Abs(resourceValue.resourceAmount) > 1000)
                {
                    inputStorageAmount.interactable = false;
                    allToggle.isOn = true;
                }
                else
                {
                    inputStorageAmount.text = Mathf.Abs(resourceValue.resourceAmount).ToString();
                    inputStorageAmount.interactable = true;
                    allToggle.isOn = false;
                }
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
            resourceValue.resourceAmount = 99999;
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

    //internal void SetStop(UITradeStopHandler uiTradeStopHandler)
    //{
    //    this.uiTradeStopHandler = uiTradeStopHandler;
    //}

    public void CloseWindow()
    {
        resourceHolder.CloseWindow();
    }
}
