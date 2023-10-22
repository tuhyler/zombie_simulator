using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UITradeResourceTask : MonoBehaviour, IResourceGridUser, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    //[SerializeField]
    //public TMP_Dropdown resourceList;

    [SerializeField]
    public TMP_Dropdown actionList;

    //[SerializeField]
    //public TMP_InputField inputStorageAmount;

    //[SerializeField]
    //public Toggle allToggle;

    [SerializeField]
    public TMP_Text counter, resourceCount;

    [SerializeField]
    public Slider resourceCountSlider;

    [SerializeField]
    public Image background, completeImage, check, chosenResourceSprite, grips;

    [SerializeField]
    private Sprite redX, blueCheck;

    [SerializeField]
    public GameObject dragGrips, closeButton;

    [SerializeField]
    public Transform resourceDropdown;

    //[SerializeField]
    //private Image resourceIcon;

    //for moving resource task
    [HideInInspector]
    public int loc;//, cargoLimit;
    [HideInInspector]
    public Transform originalParent, tempParent;
    [HideInInspector]
    public UITradeRouteResourceHolder resourceHolder;

    [HideInInspector]
    public List<string> resources = new();

    //private string chosenResource;
    [HideInInspector]
    public ResourceType chosenResource;
    private int chosenMultiple = 1;
    private int chosenResourceAmount;
    //private string chosenAmount;
    //private int traderCargoStorageLimit;

    //private ResourceHolder resourceHolder;

    //[SerializeField]
    //private TMP_Dropdown.OptionData defaultFirstChoice;

    //private bool getAll = true;
    [HideInInspector]
    public bool draggable = true;
    Vector3 diff;
    public Vector3 GridPosition { get { return resourceDropdown.position; } }

	private void Awake()
    {
        counter.outlineColor = Color.black;//new Color(0.2f, 0.2f, 0.2f);
        counter.outlineWidth = 0.3f;
        check.gameObject.SetActive(false);
        completeImage.gameObject.SetActive(false);
        //allToggle.gameObject.SetActive(false);
    }

    //private void Start()
    //{
    //    //for checking if number is positive and integer
    //    //inputStorageAmount.onValidateInput += delegate (string input, int charIndex, char addedChar) { return PositiveIntCheck(addedChar); };
    //}

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (draggable)
        {
            resourceHolder.tradeStopHandler.dragging = true;
            originalParent = transform.parent;
            transform.SetParent(tempParent);
            transform.SetAsLastSibling();
            //background.raycastTarget = false;
            grips.raycastTarget = false;

            //for dragging based on where it was clicked
            //Vector3 p = Input.mousePosition;
            //p.z = 935;
            //Vector3 pos = Camera.main.ScreenToWorldPoint(p);

            //diff = transform.position - pos;
            diff = transform.position - dragGrips.transform.position;
            resourceHolder.tradeStopHandler.tradeRouteManager.world.cityBuilderManager.PlayPickUpAudio();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggable)
        {
            Vector3 p = Input.mousePosition;
            p.z = 935;
            Vector3 pos = Camera.main.ScreenToWorldPoint(p);
            pos += diff;
            transform.position = pos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggable)
        {
            resourceHolder.tradeStopHandler.dragging = false;
            transform.SetParent(originalParent);
            transform.localPosition = Vector3.zero;
            //background.raycastTarget = true;
            grips.raycastTarget = true;
			resourceHolder.tradeStopHandler.tradeRouteManager.world.cityBuilderManager.PlayPutDownAudio();
		}
    }

    //public void SetChosenResource(int value)
    //{
    //    //bool newValue = false;

    //    //if (resourceList.options.Contains(defaultFirstChoice))
    //    //{
    //    //    newValue = true;
    //    //    if (value == 0) return;
    //    //    chosenResource = resources[value - 1];
    //    //}
    //    //else
    //    //{
    //    //    chosenResource = resources[value];
    //    //}

    //    //resourceList.options.Remove(defaultFirstChoice); //removing first choice command from list
    //    //if (newValue)
    //    //{
    //    //    resourceList.value = value - 1;
    //    //    resourceList.RefreshShownValue();
    //    //}
    //}

    public void SetChosenResourceMultiple(int value)
    {
        if (value == 1)
        {
            chosenMultiple = -1;
            resourceCountSlider.value = resourceCountSlider.maxValue;
            chosenResourceAmount = (int)resourceCountSlider.maxValue;
            //allToggle.gameObject.SetActive(true);
            //if (getAll)
            //{
            //    inputStorageAmount.interactable = false;
            //    inputStorageAmount.text = "";
            //}
        }
        else
        {
            chosenMultiple = 1;
            //allToggle.gameObject.SetActive(false);
        }
    }

    public void SetAmount(float perc)
    {
        //float perc = (float)(amount) / totalAmount;
        
        if (perc == 1)
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
        completeImage.gameObject.SetActive(true);
        check.gameObject.SetActive(false);
        completeImage.sprite = blueCheck;

        if (totalAmount == 0)
            totalAmount = 1;
        completeImage.fillAmount = (float)amount / totalAmount;
    }

    public void SetCompleteFull(bool failed, bool activate)
    {
        if (activate)
            completeImage.gameObject.SetActive(true);
        
        completeImage.fillAmount = 1;

        if (failed)
        {
            completeImage.sprite = redX;
            completeImage.transform.localScale = Vector3.zero;
            LeanTween.scale(completeImage.gameObject, Vector3.one, 0.25f).setEase(LeanTweenType.easeOutBack);
        }
        else
            SetCheck();
    }

    public void SetComplete(float compPerc)
    {
        completeImage.fillAmount = 1;

        if (compPerc == 0)
            completeImage.sprite = redX;
        else if (compPerc == 100)
            check.gameObject.SetActive(true);
        else
            completeImage.fillAmount = compPerc * .01f;
    }

    public void SetCheck()
    {
        check.gameObject.SetActive(true);
        check.transform.localScale = Vector3.zero;
        LeanTween.scale(check.gameObject, Vector3.one, 0.25f).setEase(LeanTweenType.easeOutBack);
    }

    //public void CheckMaxValue()
    //{
    //    string amount = inputStorageAmount.text;
    //    if (int.TryParse(amount, out int result))
    //        if (result > cargoLimit)
    //            inputStorageAmount.text = cargoLimit.ToString();
    //}

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

    public void SetCaptionResourceInfo(ResourceValue resourceValue)
    {
        if (resourceValue.resourceAmount < 0)
        {
            actionList.value = 1;
            //allToggle.gameObject.SetActive(true);
            chosenMultiple = -1;
        }
        
        //resourceList.options.Remove(defaultFirstChoice); //removing top choice
        chosenResource = resourceValue.resourceType;
        chosenResourceSprite.sprite = ResourceHolder.Instance.GetIcon(chosenResource);
        chosenResourceAmount = Mathf.Abs(resourceValue.resourceAmount);
        //resourceCount.text = chosenResourceAmount.ToString();
        resourceCountSlider.value = chosenResourceAmount;


        //if (Mathf.Abs(resourceValue.resourceAmount) >= 9999)
        //{
        //    allToggle.isOn = true;
        //}
        //foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
        //{
        //    if (resourceValue.resourceType == resource.resourceType)
        //    {
        //        chosenResource = resource.resourceName;
        //        resourceList.value = resources.IndexOf(chosenResource);
        //        if (resourceValue.resourceAmount < 0)
        //        {
        //            actionList.value = 1;
        //            allToggle.gameObject.SetActive(true);
        //        }
        //        resourceList.RefreshShownValue();

            //        if (Mathf.Abs(resourceValue.resourceAmount) > 1000)
            //        {
            //            inputStorageAmount.interactable = false;
            //            allToggle.isOn = true;
            //        }
            //        else
            //        {
            //            inputStorageAmount.text = Mathf.Abs(resourceValue.resourceAmount).ToString();
            //            inputStorageAmount.interactable = true;
            //            allToggle.isOn = false;
            //        }

            //        break;
            //    }
            //}
    }

    //private void PrepareResourceList()
    //{
    //    //resourceList.ClearOptions();
    //    //resourceList.options.Add(defaultFirstChoice);
    //}

    public void OpenResourceGrid()
    {
        resourceHolder.tradeStopHandler.tradeRouteManager.resourceSelectionGrid.ToggleVisibility(true, this);
    }

    //public void AddResources(List<TMP_Dropdown.OptionData> resources)
    //{
    //    //PrepareResourceList();

    //    //foreach (var resource in resources)
    //    //{
    //    //    this.resources.Add(resource.text);
    //    //}

    //    //resourceList.AddOptions(resources);
    //}

    public void ChangeSlider(float value)
    {
        chosenResourceAmount = Mathf.RoundToInt(value);
        resourceCount.text = chosenResourceAmount.ToString();
    }

    //public void GetAllOfResource(bool v)
    //{
    //    //inputStorageAmount.interactable = !v;
    //    //inputStorageAmount.text = "";
    //    getAll = v;
    //}

    public ResourceValue GetResourceTasks()
    {
        ResourceValue resourceValue;

        resourceValue.resourceType = chosenResource;
        resourceValue.resourceAmount = chosenResourceAmount;
        //resourceValue.resourceType = ResourceType.None;

        //foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
        //{
        //    if (chosenResource == resource.resourceName)
        //        resourceValue.resourceType = resource.resourceType;
        //}

        //string chosenAmount = 0.ToString();// inputStorageAmount.text;

        //if (chosenMultiple < 0 && getAll) //get all of the resource
        //{
        //    resourceValue.resourceAmount = 99999;
        //}
        //else
        //{
        //    if (int.TryParse(chosenAmount, out int result))
        //    {
        //        //if (inputAmount == "")
        //        //    result = 0;
        //        resourceValue.resourceAmount = result;
        //    }
        //    else //silly workaround to remove trailing invisible char ("Trim" doesn't work)
        //    {
        //        string stringAmount = "0";
        //        int i = 0;

        //        foreach (char c in chosenAmount)
        //        {
        //            if (i == chosenAmount.Length - 1)
        //                continue;

        //            stringAmount += c;
        //            Debug.Log("letter " + c);
        //            i++;
        //        }

        //        resourceValue.resourceAmount = int.Parse(stringAmount);
        //    }
        //}

        resourceValue.resourceAmount *= chosenMultiple;
        return resourceValue;
    }

    //internal void SetStop(UITradeStopHandler uiTradeStopHandler)
    //{
    //    this.uiTradeStopHandler = uiTradeStopHandler;
    //}

    public void CloseWindow()
    {
        resourceHolder.CloseWindow(true);
		resourceHolder.tradeStopHandler.tradeRouteManager.world.cityBuilderManager.PlayCloseAudio();
	}

	public void SetData(Sprite icon, ResourceType resourceType)
	{
        chosenResourceSprite.sprite = icon;
        chosenResource = resourceType;
	}
}
