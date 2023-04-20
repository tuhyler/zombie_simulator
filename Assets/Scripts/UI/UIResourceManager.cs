using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIResourceManager : MonoBehaviour
{
    [SerializeField]
    private TMP_Text cityStorageInfo, cityStoragePercent, cityLevelAndLimit;

    [SerializeField]
    private Image progressBarMask;

    [SerializeField]
    private Transform resourceHolder;

    [SerializeField]
    private GameObject resourcePanel;

    [SerializeField] //for tweening
    private RectTransform allContents;
    private Vector3 originalLoc;
    private bool activeStatus;

    private string cityName;
    private int cityStorageLimit;
    private float cityStorageLevel; 
    
    private Dictionary<ResourceType, UIResources> resourceUIDict = new();

    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;
        gameObject.SetActive(false);

        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
        {
            GameObject panel = Instantiate(resourcePanel);
            panel.transform.SetParent(resourceHolder);
            panel.name = resource.resourceType.ToString();
            UIResources resourceUI = panel.GetComponent<UIResources>();
            resourceUI.resourceImage.sprite = resource.resourceIcon;
            resourceUI.resourceType = resource.resourceType;

            //fixing z location & x rotation
            Vector3 loc = panel.transform.position;
            loc.z = 0;
            panel.transform.localPosition = loc;
            Vector3 rot = panel.transform.eulerAngles;
            rot.x = 0;
            panel.transform.localEulerAngles = rot;
            panel.transform.localScale = Vector3.one;
        }

        PrepareResourceDictionary();
    }

    //public void SetActiveStatus(bool v)
    //{
    //    gameObject.SetActive(v);

    //    foreach (ResourceType resourceType in resourceUIDict.Keys)
    //    {
    //        resourceUIDict[resourceType].SetActiveStatus(v);
    //    }
    //}

    public void ToggleVisibility(bool v)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(v);
            activeStatus = true;

            allContents.anchoredPosition3D = originalLoc + new Vector3(0, 200f, 0);

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -200f, 0.3f).setEaseOutSine();
            LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();

            foreach (ResourceType resourceType in resourceUIDict.Keys)
            {
                resourceUIDict[resourceType].CheckVisibility();
            }
        }
        else
        {
            activeStatus = false;
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 200f, 0.2f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void SetCityInfo(string cityName, int cityStorageLimit, float cityStorageLevel)
    {
        this.cityStorageLevel = cityStorageLevel;
        this.cityName = cityName;
        this.cityStorageLimit = cityStorageLimit;
        progressBarMask.fillAmount = cityStorageLevel / cityStorageLimit;

        UpdateCityInfo();
    }

    private void UpdateCityInfo()
    {
        cityStorageInfo.text = $"{cityName} Storage";
        cityStoragePercent.text = $"{Mathf.RoundToInt(100 * (cityStorageLevel / cityStorageLimit))}%";
        cityLevelAndLimit.text = $"{cityStorageLevel}/{cityStorageLimit}";
    }

    public void SetCityCurrentStorage(float cityStorageLevel)
    {
        this.cityStorageLevel = cityStorageLevel;
        //progressBarMask.fillAmount = cityStorageLevel / cityStorageLimit;
        UpdateStorage(cityStorageLevel);
        UpdateCityInfo();
    }

    private void UpdateStorage(float cityStorageLevel)
    {
        LeanTween.value(progressBarMask.gameObject, progressBarMask.fillAmount, cityStorageLevel / cityStorageLimit, 0.2f)
            .setEase(LeanTweenType.easeOutSine)
            .setOnUpdate((value) =>
            {
                progressBarMask.fillAmount = value;
            });
    }

    private void PrepareResourceDictionary() //put all resources in dictionary
    {
        foreach (UIResources uiResource in GetComponentsInChildren<UIResources>())//"inchildren" is where the resource prefabs are
        {
            if (resourceUIDict.ContainsKey(uiResource.ResourceType))
                throw new ArgumentException("Dictionary already contains a " + uiResource.ResourceType);
            resourceUIDict[uiResource.ResourceType] = uiResource;
            SetResource(uiResource.ResourceType, 0);
            //SetResourceLimit(uiResource.ResourceType, 0);
            //SetResourceGenerationAmount(uiResource.ResourceType, 0);
        }
    }

    public void SetResource(ResourceType resourceType, int val) //Set the resources to a value
    {
        if (resourceUIDict.ContainsKey(resourceType))//checking if resource is in dictionary
        {
            resourceUIDict[resourceType].SetValue(val);
            resourceUIDict[resourceType].CheckVisibility();
        }
    }

    //public void SetResourceGenerationAmount(ResourceType resourceType, int val) //Set the resources to a value
    //{
    //    if (resourceUIDict.ContainsKey(resourceType))//checking if resource is in dictionary
    //    {
    //        //resourceUIDict[resourceType].SetGeneration(val);
    //    }
    //}
}
