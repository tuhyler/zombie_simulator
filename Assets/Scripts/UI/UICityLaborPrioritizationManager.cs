using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.UI;

public class UICityLaborPrioritizationManager : MonoBehaviour
{
    [SerializeField]
    private CityBuilderManager cityBuilderManager;

    [SerializeField]
    private GameObject uiLaborResourcePriority;

    [SerializeField]
    private Transform resourceHolder;

    //for generating resource lists
    private List<TMP_Dropdown.OptionData> resources = new();

    [SerializeField] //for tweening
    private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus;
    private Vector3 originalLoc;

    //List of all displayed resource priorities
    private List<UILaborResourcePriority> resourcePriorityList = new();

    private City city;

    [SerializeField]
    private Image openPrioritizationImage;
    [SerializeField]
    private Sprite buttonLeft;
    private Sprite buttonRight;

    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;
        AddResources();
        buttonRight = openPrioritizationImage.sprite;
        gameObject.SetActive(false);
    }

    public void LoadLaborPrioritizationInfo()
    {
        List<ResourceType> resourcePriorities = city.ResourcePriorities;

        foreach (ResourceType resourceType in resourcePriorities)
        {
            UILaborResourcePriority uiLaborResource = AddResourcePriority();
            uiLaborResource.SetCaptionResourceInfo(resourceType);
        }
    }

    private void AddResources()
    {
        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources.Concat(ResourceHolder.Instance.allWorldResources).ToList())
        {
            resources.Add(new TMP_Dropdown.OptionData(resource.resourceName, resource.resourceIcon));
        }
    }

    public void ToggleVisibility(bool v, bool exitCity = false)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(v);
            openPrioritizationImage.sprite = buttonLeft;
            activeStatus = true;

            allContents.anchoredPosition3D = originalLoc + new Vector3(-300f, 0, 0);

            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + 300f, 0.3f).setEaseOutSine();
            //LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear(); //don't like alpha fading here
        }
        else
        {
            activeStatus = false;
            city = null;
            openPrioritizationImage.sprite = buttonRight;
            foreach (UILaborResourcePriority uiLaborResourcePriority in resourcePriorityList)
            {
                uiLaborResourcePriority.RemoveWindow();
            }

            resourcePriorityList.Clear();
            if (!exitCity)
                LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + -300f, 0.3f).setOnComplete(SetActiveStatusFalse);
            else
                LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -850f, 0.3f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void PrepareLaborPrioritizationMenu(City city)
    {
        this.city = city;
    }

    public void AddResourcePriorityButton() //added this as a method attached to button as it can't return anything
    {
        if (resourcePriorityList.Count >= resources.Count) //limit
        {
            InfoPopUpHandler.Create(city.cityLoc, "Nope, that's too many");
            return;
        }

        AddResourcePriority();
    }

    private UILaborResourcePriority AddResourcePriority() //showing a new resource priority panel
    {
        GameObject newPriority = Instantiate(uiLaborResourcePriority);
        newPriority.SetActive(true);
        newPriority.transform.SetParent(resourceHolder, false);

        UILaborResourcePriority newResourcePriority = newPriority.GetComponent<UILaborResourcePriority>();
        newResourcePriority.AddResources(resources);
        newResourcePriority.SetPriority(); //set priority before attaching to this
        newResourcePriority.SetCityLaborPrioritizationManager(this);

        return newResourcePriority;
    }

    public void AddToResourcePriorityList(int priority, UILaborResourcePriority uiLaborResourcePriority)
    {
        resourcePriorityList.Insert(priority-1, uiLaborResourcePriority);
    }

    public void MovePriorityUp(UILaborResourcePriority uiLaborResourcePriority, int priority)
    {
        UILaborResourcePriority priorityAbove = resourcePriorityList[priority - 1];
        priorityAbove.SetPriority(priorityAbove.currentPriorityNumber + 1);
        resourcePriorityList.Remove(uiLaborResourcePriority);
        resourcePriorityList.Insert(priority-1, uiLaborResourcePriority);
    }

    public void MovePriorityDown(UILaborResourcePriority uiLaborResourcePriority, int priority)
    {
        UILaborResourcePriority priorityBelow = resourcePriorityList[priority - 1];
        priorityBelow.SetPriority(priorityBelow.currentPriorityNumber - 1);
        resourcePriorityList.Remove(uiLaborResourcePriority);
        resourcePriorityList.Insert(priority-1, uiLaborResourcePriority);
    }

    public void RemoveFromResourcePriorityList(UILaborResourcePriority uiLaborResourcePriority)
    {
        int priority = resourcePriorityList.IndexOf(uiLaborResourcePriority);
        resourcePriorityList.Remove(uiLaborResourcePriority);

        //moving one down for the priority numbers for all resources below the one removed
        for (int i = priority; i < resourcePriorityList.Count; i++)
        {
            UILaborResourcePriority priorityBelow = resourcePriorityList[i];
            priorityBelow.SetPriority(priorityBelow.currentPriorityNumber - 1);
        }
    }

    public void SetLaborPriorities()
    {
        List<ResourceType> resourcePriorities = new();

        foreach (UILaborResourcePriority uiLaborResource in resourcePriorityList)
        {
            ResourceType resourceType = uiLaborResource.GetChosenResource();
            uiLaborResource.RemoveWindow();
            if (resourceType == ResourceType.None || resourcePriorities.Contains(resourceType))
                continue;
            resourcePriorities.Add(resourceType);
        }

        resourcePriorityList.Clear();
        city.ResourcePriorities = resourcePriorities;

        //if (resourcePriorities.Count > 0)
        //{
        //    city.AutoAssignmentsForLabor();
        //    cityBuilderManager.UpdateCityLaborUIs();
        //}

        ToggleVisibility(false);
    }
}
