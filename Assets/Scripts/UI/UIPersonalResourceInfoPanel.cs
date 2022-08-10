using System;
using System.Collections;
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
    private Image progressBarMask;

    [SerializeField] //for tweening
    private RectTransform allContents;
    private Vector3 originalLoc;
    private Vector3 loadUnloadPosition;
    private bool activeStatus;

    public bool city; //indicate which window is for the city or trader

    private float unitStorageLevel;
    private int unitStorageLimit;

    private Dictionary<ResourceType, UIPersonalResources> personalResourceUIDictionary = new();

    [SerializeField]
    private Transform uiElementsParent;

    private List<UIPersonalResources> personalResources = new();

    //for clicking resource buttons
    [SerializeField]
    private UnityEvent<ResourceType> OnIconButtonClick;
    private ResourceType resourceType;
    
    [HideInInspector]
    public bool inUse; //for clicking off screen to exit trade screen

    //for managing resource managers
    //private ResourceManager resourceManager;
    //private PersonalResourceManager personalResourceManager;

    private void Awake()
    {
        if (!city)
        { //whole sequence is to determine where trader resource window goes during unload/load (changes based on resolution)
            originalLoc = allContents.anchoredPosition3D;
            allContents.anchorMin = new Vector2(0.5f, 0.5f);
            allContents.anchorMax = new Vector2(0.5f, 0.5f);
            allContents.anchoredPosition3D = new Vector3(0f, 175f, 0f);
            loadUnloadPosition = allContents.transform.localPosition;
            allContents.anchorMin = new Vector2(0.5f, 1.0f);
            allContents.anchorMax = new Vector2(0.5f, 1.0f);
            allContents.anchoredPosition3D = new Vector3(0f, 0f, 0f);
        }

        gameObject.SetActive(false);
        PrepareResourceDictionary();

        foreach (Transform selection in uiElementsParent) //populate list
        {
            personalResources.Add(selection.GetComponent<UIPersonalResources>());
        }
    }

    public void ToggleVisibility(bool v)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(v);
            activeStatus = true;

            if (!city)
            {
                allContents.anchoredPosition3D = originalLoc + new Vector3(0, 200f, 0);

                LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -200f, 0.3f).setEaseOutSine();
                LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();
            }

            foreach (UIPersonalResources resources in personalResources) //ShowUI resources with more than 1
            {
                resources.CheckVisibility();
            }

            progressBarMask.fillAmount = unitStorageLevel / unitStorageLimit;
            //    ResourceHolderCheck();
        }
        else
        {
            activeStatus = false;

            if (!city)
            {
                LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 200f, 0.2f).setOnComplete(SetActiveStatusFalse);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void ResourceHolderCheck() //show resources if carrying something (not necessary apparently?)
    {
        if (unitStorageLevel > 0)
            uiElementsParent.gameObject.SetActive(true);
        else
            uiElementsParent.gameObject.SetActive(false);
    }

    public void HandleButtonClick()
    {
        OnIconButtonClick?.Invoke(resourceType);
    }

    public void SetTitleInfo(string name, float level, int limit)
    {
        unitStorageLevel = level;
        unitStorageLimit = limit;
        
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
        if (unitStorageLevel == 0)
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

        LeanTween.value(progressBarMask.gameObject, progressBarMask.fillAmount, unitStorageLevel / unitStorageLimit, 0.3f)
            .setEase(LeanTweenType.easeOutSine)
            .setOnUpdate((value) =>
            {
                progressBarMask.fillAmount = value;
            });

        //ResourceHolderCheck();
    }

    private void PrepareResourceDictionary() //put all resources in dictionary
    {
        foreach (UIPersonalResources uiPersonalResources in GetComponentsInChildren<UIPersonalResources>())
        {
            if (personalResourceUIDictionary.ContainsKey(uiPersonalResources.ResourceType))
                Debug.Log("Dictionary already contains a " + uiPersonalResources.ResourceType);
            personalResourceUIDictionary[uiPersonalResources.ResourceType] = uiPersonalResources;
            SetResource(uiPersonalResources.ResourceType, 0);
        }
    }

    public void PrepareResource(ResourceType resourceType)
    {
        this.resourceType = resourceType;
    }

    public void PrepareResourceUI(Dictionary<ResourceType, int> resourceDict)
    {
        foreach (UIPersonalResources selection in personalResources)
        { 
            SetResource(selection.ResourceType, resourceDict[selection.ResourceType]);
            if (resourceDict[selection.ResourceType] > 0)
                inUse = true;
        }
    }

    public void ToggleButtonInteractable(bool v)
    {
        foreach (UIPersonalResources selection in personalResources)
        {
            selection.SetButtonInteractable(v);
        }
    }

    public void EmptyResourceUI()
    {
        foreach (UIPersonalResources uiPersonalResources in GetComponentsInChildren<UIPersonalResources>())
        {
            uiPersonalResources.SetButtonInteractable(false);
            SetResource(uiPersonalResources.ResourceType, 0);
        }

        inUse = false;
    }

    public void SetResource(ResourceType resourceType, int val) //Set the resources to a value
    {
        if (personalResourceUIDictionary.ContainsKey(resourceType))//checking if resource is in dictionary
        {
            personalResourceUIDictionary[resourceType].SetValue(val);
        }
    }

    public void SetPosition()
    {

        if (city)
        {
            ToggleVisibility(true);
            allContents.anchoredPosition3D = originalLoc + new Vector3(0, -200f, 0);

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 200f, 0.4f).setEase(LeanTweenType.easeOutSine);
            LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();
        }
        else
        {
            allContents.anchoredPosition3D = originalLoc;
            float unloadLoadShift = allContents.transform.localPosition.y - loadUnloadPosition.y; //how much to decrease 

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - unloadLoadShift, 0.4f).setEase(LeanTweenType.easeOutSine);
        }

        //allContents.anchoredPosition3D += new Vector3(0, -200f, 0);
        //LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + shiftAmount, 0.4f).setEase(LeanTweenType.easeOutSine);
        ToggleButtonInteractable(true);
    }

    public void RestorePosition()
    {
        ToggleButtonInteractable(false);

        if (city)
        {
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - 300f, 0.4f)
                .setEase(LeanTweenType.easeOutSine)
                .setOnComplete(SetVisibilityFalse);
            LeanTween.alpha(allContents, 0f, 0.4f).setEaseLinear();
        }
        else
        {
            LeanTween.moveY(allContents, originalLoc.y, 0.4f).setEase(LeanTweenType.easeOutSine);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    private void SetVisibilityFalse()
    {
        ToggleVisibility(false);
    }

    public void UpdateResource(ResourceType resourceType, int val) //Set the resources to a value
    {
        if (personalResourceUIDictionary.ContainsKey(resourceType))//checking if resource is in dictionary
        {
            personalResourceUIDictionary[resourceType].UpdateValue(val);
        }
    }

}
