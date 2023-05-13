using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class UIPersonalResourceInfoPanel : MonoBehaviour
{
    [SerializeField]
    private TMP_Text unitNameTitle, unitStoragePercent, unitLevelAndLimit;

    [SerializeField]
    private GameObject progressBar;

    [SerializeField]
    private Image progressBarMask, buttonDown;
    [SerializeField]
    private Sprite buttonUp;
    private Sprite originalDown;

    [SerializeField] //for tweening
    private RectTransform allContents;
    private Vector3 originalLoc, overflowOriginalLoc;
    private Vector3 loadUnloadPosition;
    private bool activeStatus, overflowActiveStatus;

    public bool isCity; //indicate which window is for the city or trader
    private Trader trader;
    private City city;
    private Wonder wonder;

    private float unitStorageLevel;
    private int unitStorageLimit;

    //private Dictionary<ResourceType, UIPersonalResources> personalResourceUIDictionary = new();

    //[SerializeField]
    //private Transform uiElementsParent;
    [SerializeField]
    private GameObject resourcePersonalPanel;

    //private List<UIPersonalResources> personalResources = new();

    //for clicking resource buttons
    [SerializeField]
    private UnityEvent<ResourceType> OnIconButtonClick;
    private ResourceType resourceType;
    
    [HideInInspector]
    public bool inUse; //for clicking off screen to exit trade screen

    //resource grid
    public RectTransform gridHolder, overflowGridHolder;
    private Dictionary<int, UIPersonalDropLocation> gridCellDict = new();
    private int activeCells;
    private Dictionary<ResourceType, int> resourceUIDict = new();
    public int gridWidth = 10;

    //for managing resource managers
    //private ResourceManager resourceManager;
    //private PersonalResourceManager personalResourceManager;

    private void Awake()
    {
        if (!isCity)
        { //whole sequence is to determine where trader resource window goes during unload/load (changes based on resolution)
            originalLoc = allContents.anchoredPosition3D;
            overflowOriginalLoc = overflowGridHolder.anchoredPosition3D;
            allContents.anchorMin = new Vector2(0.5f, 0.5f);
            allContents.anchorMax = new Vector2(0.5f, 0.5f);
            allContents.anchoredPosition3D = new Vector3(0f, 235f, 0f);
            loadUnloadPosition = allContents.transform.localPosition;
            allContents.anchorMin = new Vector2(0.5f, 1.0f);
            allContents.anchorMax = new Vector2(0.5f, 1.0f);
            allContents.anchoredPosition3D = new Vector3(0f, 0f, 0f);
        }

        overflowGridHolder.sizeDelta = new Vector2(gridWidth * 90, 90);
        buttonDown.transform.localPosition = new Vector3(gridWidth * 45 + 20 , -80, 0);
        gameObject.SetActive(false);
        overflowGridHolder.gameObject.SetActive(false);
        buttonDown.gameObject.SetActive(false);
        originalDown = buttonDown.sprite;

        //for (int i = 0; i < gridWidth; i++)
        //{

        //}

        int total = 0;
        foreach (Transform selection in gridHolder)
        {
            GridCellPrep(selection, total);
            total++;
        }

        foreach (Transform selection in overflowGridHolder)
        {
            GridCellPrep(selection, total);
            total++;
        }

        //foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
        //{
        //    GameObject panel = Instantiate(resourcePersonalPanel);
        //    panel.transform.SetParent(uiElementsParent);
        //    panel.name = resource.resourceType.ToString();
        //    UIPersonalResources personalResource = panel.GetComponent<UIPersonalResources>();
        //    personalResource.SetButtonHandler(this);
        //    personalResource.resourceImage.sprite = resource.resourceIcon;
        //    personalResource.resourceType = resource.resourceType;


        //    //fixing z location & x rotation
        //    Vector3 loc = panel.transform.position;
        //    loc.z = 0;
        //    panel.transform.localPosition = loc;
        //    Vector3 rot = panel.transform.eulerAngles;
        //    rot.x = 0;
        //    panel.transform.localEulerAngles = rot;

        //    personalResources.Add(personalResource);
        //}

        //PrepareResourceDictionary();
        //foreach (Transform selection in uiElementsParent) //populate list
        //{   
        //    personalResources.Add(selection.GetComponent<UIPersonalResources>());
        //}
    }

    private void GridCellPrep(Transform selection, int total)
    {
        UIPersonalDropLocation loc = selection.GetComponent<UIPersonalDropLocation>();
        loc.gameObject.SetActive(false);
        loc.SetUIResourceManager(this);
        loc.gridLocation = total;
        loc.resource.loc = total;
        gridCellDict.Add(loc.gridLocation, loc);
    }

    public void ToggleVisibility(bool v, Trader trader = null, City city = null, Wonder wonder = null)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            this.trader = trader;
            this.city = city;
            this.wonder = wonder;
            activeCells = 0;
            buttonDown.gameObject.SetActive(false);

            if (isCity)
            {
                if (city)
                    foreach (ResourceType type in city.resourceGridDict.Keys)
                        ActivateCell(type, true);
                else
                    foreach (ResourceType type in wonder.ResourceGridDict.Keys)
                        ActivateCell(type, true);
            }
            else
                foreach (ResourceType type in trader.resourceGridDict.Keys)
                {
                    ActivateCell(type, true);
                    gridCellDict[resourceUIDict[type]].resource.clickable = false;
                }

            if (activeCells > gridWidth)
            {
                gridHolder.sizeDelta = new Vector2(90 * gridWidth, 90);
                if (!isCity)
                    buttonDown.gameObject.SetActive(true);
            }
            else
            {
                gridHolder.sizeDelta = new Vector2(activeCells * 90, 90);
            }

            progressBarMask.fillAmount = 0;
            gameObject.SetActive(v);
            activeStatus = true;

            if (!isCity)
            {
                allContents.anchoredPosition3D = originalLoc + new Vector3(0, 200f, 0);

                LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -200f, 0.3f).setEaseOutSine();
                //LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();
            }

            //foreach (UIPersonalResources resources in personalResources) //ShowUI resources with more than 1
            //{
            //    resources.CheckVisibility();
            //}

            progressBarMask.fillAmount = unitStorageLevel / unitStorageLimit;
            //    ResourceHolderCheck();
        }
        else
        {
            for (int i = 0; i < activeCells; i++)
            {
                gridCellDict[i].gameObject.SetActive(false);
            }

            activeStatus = false;
            ToggleOverflowVisibility(false);
            resourceUIDict.Clear();

            if (!isCity)
            {
                this.trader.ReshuffleGrid(); //gets rid of zeroed out resources
                this.trader = null;
                LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 400f, 0.2f).setOnComplete(SetActiveStatusFalse);
            }
            else if (this.city)
            {
                this.city.ReshuffleGrid();
                this.city = null;
                gameObject.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    public void ToggleOverflow()
    {
        if (overflowActiveStatus)
            ToggleOverflowVisibility(false);
        else
            ToggleOverflowVisibility(true);
    }

    public void ToggleOverflowVisibility(bool v, bool instant = false)
    {
        if (overflowActiveStatus == v)
            return;

        LeanTween.cancel(overflowGridHolder.gameObject);

        if (v)
        {
            buttonDown.sprite = buttonUp;
            overflowGridHolder.gameObject.SetActive(v);
            overflowActiveStatus = true;
            float size = Mathf.CeilToInt((activeCells - gridWidth) / gridWidth) * 90;

            overflowGridHolder.anchoredPosition3D = overflowOriginalLoc + new Vector3(0, size, 0);

            float speed = instant ? 0 : 0.3f;
            LeanTween.moveY(overflowGridHolder, overflowGridHolder.anchoredPosition3D.y + -size, speed).setEaseOutSine();
        }
        else
        {
            buttonDown.sprite = originalDown;
            overflowActiveStatus = false;
            LeanTween.moveY(overflowGridHolder, overflowGridHolder.anchoredPosition3D.y + Mathf.CeilToInt((activeCells - gridWidth) / gridWidth) * 90, 0.2f).setOnComplete(SetOverflowStatusFalse);
        }
    }

    private void SetOverflowStatusFalse()
    {
        overflowGridHolder.gameObject.SetActive(false);
    }

    private void ActivateCell(ResourceType type, bool instant = false)
    {
        int loc;

        if (isCity)
        {
            if (city)
                loc = city.resourceGridDict[type];
            else
                loc = wonder.ResourceGridDict[type];
        }
        else
            loc = trader.resourceGridDict[type];

        gridCellDict[loc].gameObject.SetActive(true);
        activeCells++;

        gridCellDict[loc].resource.resourceType = type;

        if (isCity)
        {
            if (city)
                gridCellDict[loc].resource.SetValue(city.ResourceManager.ResourceDict[type]);
            else
                gridCellDict[loc].resource.SetValue(wonder.ResourceDict[type]);
        }
        else
            gridCellDict[loc].resource.SetValue(trader.personalResourceManager.ResourceDict[type]);

        gridCellDict[loc].resource.resourceImage.sprite = ResourceHolder.Instance.GetIcon(type);
        resourceUIDict[type] = loc;
        gridCellDict[loc].resource.Activate(instant);

        if (inUse && activeCells % gridWidth == 1 && activeCells > 1)
        {
            if (activeCells == gridWidth + 1)
                ToggleOverflowVisibility(true, true);
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 90, 0).setEase(LeanTweenType.easeOutSine);
        }
    }

    //public void ReshuffleGrid()
    //{
    //    if (isCity)
    //        city.ReshuffleGrid();
    //    else
    //        trader.ReshuffleGrid();
    //}

    public void HandleButtonClick()
    {
        if (inUse)
            OnIconButtonClick?.Invoke(resourceType);
    }

    public void SetTitleInfo(string name, float level, int limit)
    {
        unitStorageLevel = level;
        unitStorageLimit = limit;
        progressBarMask.fillAmount = unitStorageLevel / unitStorageLimit;

        unitNameTitle.text = $"{name} Storage";
        if (limit == 0)
        {
            unitLevelAndLimit.text = level.ToString();
            unitStoragePercent.text = "0%";
        }
        else
        {
            unitLevelAndLimit.text = $"{level}/{limit}";
            unitStoragePercent.text = $"{Mathf.RoundToInt((level / limit) * 100)}%";
        }
        //ResourceHolderCheck();
    }

    public void UpdateStorageLevel(float level)
    {
        if (unitStorageLevel == 0) //progress bar gives value of null w/o this
            progressBarMask.fillAmount = 0;

        unitStorageLevel = level;

        if (unitStorageLimit == 0)
        {
            unitLevelAndLimit.text = level.ToString();
        }
        else
        {
            unitLevelAndLimit.text = $"{level}/{unitStorageLimit}";
            unitStoragePercent.text = $"{Mathf.RoundToInt((level / unitStorageLimit) * 100)}%";
        }

        LeanTween.value(progressBarMask.gameObject, progressBarMask.fillAmount, unitStorageLevel / unitStorageLimit, 1)
            .setEase(LeanTweenType.easeOutSine)
            .setOnUpdate((value) =>
            {
                progressBarMask.fillAmount = value;
            });

        //ResourceHolderCheck();
    }

    //private void PrepareResourceDictionary() //put all resources in dictionary
    //{
    //    foreach (UIPersonalResources uiPersonalResources in personalResources)
    //    {
    //        if (personalResourceUIDictionary.ContainsKey(uiPersonalResources.resourceType))
    //            Debug.Log("Dictionary already contains a " + uiPersonalResources.resourceType);
    //        personalResourceUIDictionary[uiPersonalResources.resourceType] = uiPersonalResources;
    //        SetResource(uiPersonalResources.resourceType, 0);
    //    }
    //}

    public void PrepareResource(ResourceType resourceType)
    {
        this.resourceType = resourceType;
    }

    //public void PrepareResourceUI(Dictionary<ResourceType, int> resourceDict)
    //{
    //    foreach (ResourceType type in resourceDict.Keys)
    //    {
    //        //int amount;
    //        //if (resourceDict.ContainsKey(selection.resourceType))
    //        //    amount = resourceDict[selection.resourceType];
    //        //else
    //        //    amount = 0;

    //        //SetResource(selection.resourceType, amount);
    //        SetResource(type, resourceDict[type]);
    //        //if (amount > 0)
    //        //    inUse = true;
    //    }
    //}

    private void ToggleButtonInteractable(bool v)
    {
        foreach (int loc in gridCellDict.Keys)
        {
            gridCellDict[loc].resource.SetButtonInteractable(v);
        }
        
        //foreach (UIPersonalResources selection in personalResources)
        //{
        //    selection.SetButtonInteractable(v);
        //}
    }

    //public void EmptyResourceUI()
    //{
    //    foreach (UIPersonalResources uiPersonalResources in GetComponentsInChildren<UIPersonalResources>())
    //    {
    //        uiPersonalResources.SetButtonInteractable(false);
    //        SetResource(uiPersonalResources.resourceType, 0);
    //    }
    //}

    //private void SetResource(ResourceType type, int val) //Set the resources to a value
    //{
    //    //if (personalResourceUIDictionary.ContainsKey(resourceType))//checking if resource is in dictionary
    //    //{
    //    //    personalResourceUIDictionary[resourceType].SetValue(val);
    //    //}

    //    if (!resourceUIDict.ContainsKey(type))
    //    {
    //        ActivateCell(type);
    //        if (activeCells > gridWidth)
    //        {
    //            if (!inUse)
    //                buttonDown.gameObject.SetActive(true);
    //        }
    //        else
    //            gridHolder.sizeDelta = new Vector2(activeCells * 90, 90);
    //    }

    //    gridCellDict[resourceUIDict[type]].resource.SetValue(val);
    //}

    public void SetPosition()
    {
        if (isCity)
        {
            ToggleVisibility(true);
            allContents.anchoredPosition3D = originalLoc + new Vector3(0, -200f, 0);

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 260f, 0.4f).setEase(LeanTweenType.easeOutSine);
            //LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();
        }
        else
        {
            allContents.anchoredPosition3D = originalLoc;
            float unloadLoadShift = allContents.transform.localPosition.y - loadUnloadPosition.y; //how much to decrease 
            unloadLoadShift -= Mathf.FloorToInt(activeCells / gridWidth) * 90;

            buttonDown.gameObject.SetActive(false);
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - unloadLoadShift, 0.4f).setEase(LeanTweenType.easeOutSine);
            ToggleButtonInteractable(true);
        }

        //allContents.anchoredPosition3D += new Vector3(0, -200f, 0);
        //LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + shiftAmount, 0.4f).setEase(LeanTweenType.easeOutSine);
        if (activeCells > gridWidth)
            ToggleOverflowVisibility(true, true);
        inUse = true;
    }

    public void RestorePosition(bool keepSelection)
    {

        if (isCity)
        {
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - 800f, 0.4f)
                .setEase(LeanTweenType.easeOutSine)
                .setOnComplete(SetVisibilityFalse);
            //LeanTween.alpha(allContents, 0f, 0.4f).setEaseLinear();
        }
        else
        {
            ToggleButtonInteractable(false);
            Vector3 loc;
            if (keepSelection)
                loc = originalLoc;
            else
                loc = originalLoc + new Vector3(0, 200f, 0);
            LeanTween.moveY(allContents, loc.y, 0.4f).setEase(LeanTweenType.easeOutSine).setOnComplete(ShowButtonDown);
        }

        inUse = false;
    }

    private void ShowButtonDown()
    {
        if (activeCells > gridWidth)
            buttonDown.gameObject.SetActive(true);
    }

    private void SetActiveStatusFalse()
    {
        unitLevelAndLimit.gameObject.SetActive(true);
        unitStoragePercent.gameObject.SetActive(true);
        progressBar.SetActive(true);
        gameObject.SetActive(false);
    }

    private void SetVisibilityFalse()
    {
        ToggleVisibility(false);
    }

    public void UpdateResourceInteractable(ResourceType type, int val, bool positive) //Set the resources to a value
    {
        if (resourceUIDict.ContainsKey(type))//checking if resource is in dictionary
        {
            gridCellDict[resourceUIDict[type]].resource.UpdateValue(val,positive);
        }
        else
        {
            ActivateCell(type);
            if (activeCells > gridWidth)
            {
                if (!inUse) 
                    buttonDown.gameObject.SetActive(true);
            }
            else
                gridHolder.sizeDelta = new Vector2(activeCells * 90, 90);
        }
    }

    public void UpdateResource(ResourceType type, int val) //Set the resources to a value
    {
        if (resourceUIDict.ContainsKey(type))//checking if resource is in dictionary
        {
            gridCellDict[resourceUIDict[type]].resource.SetValue(val, true);
        }
        else
        {
            ActivateCell(type);
            if (activeCells > gridWidth)
                buttonDown.gameObject.SetActive(true);
            else
                gridHolder.sizeDelta = new Vector2(activeCells * 90, 90);
        }
    }

    public void HideInventoryLevel()
    {
        unitLevelAndLimit.gameObject.SetActive(false);
        unitStoragePercent.gameObject.SetActive(false);
        progressBar.SetActive(false);
    }

    public void MoveResources(int oldNum, int newNum, ResourceType type)
    {
        //shifting all the other resources
        if (oldNum > newNum)
        {
            for (int i = oldNum; i > newNum; i--)
            {
                int next = i - 1;
                gridCellDict[next].MoveResource(gridCellDict[i], true);
                trader.resourceGridDict[gridCellDict[i].resource.resourceType] = i;
                resourceUIDict[gridCellDict[i].resource.resourceType] = i;
            }
        }
        else
        {
            for (int i = oldNum; i < newNum; i++)
            {
                int next = i + 1;
                gridCellDict[next].MoveResource(gridCellDict[i], false);
                trader.resourceGridDict[gridCellDict[i].resource.resourceType] = i;
                resourceUIDict[gridCellDict[i].resource.resourceType] = i;
            }
        }

        trader.resourceGridDict[type] = newNum;
        resourceUIDict[type] = newNum;
    }
}
