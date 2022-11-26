using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class UILaborResourcePriority : MonoBehaviour
{
    [SerializeField]
    private TMP_Dropdown resourceList;

    [SerializeField]
    private TMP_Text priorityNumber;
    [HideInInspector]
    public int currentPriorityNumber;

    private UICityLaborPrioritizationManager uiLaborPrioritizationManager;

    private List<string> resources = new();

    private string chosenResource;

    [SerializeField]
    private TMP_Dropdown.OptionData defaultFirstChoice;


    public void SetPriority()
    {
        int priorityNumberInt = (transform.GetSiblingIndex() + 1);
        priorityNumber.text = priorityNumberInt.ToString(); //adding one for more intuitive numbers
        currentPriorityNumber = priorityNumberInt;
    }

    public void SetCityLaborPrioritizationManager(UICityLaborPrioritizationManager uiLaborPrioritizationManager)
    {
        this.uiLaborPrioritizationManager = uiLaborPrioritizationManager;
        uiLaborPrioritizationManager.AddToResourcePriorityList(currentPriorityNumber, this);
    }

    public void MovePriorityUp()
    {
        //RepositionPanel(true);
        
        int placement = transform.GetSiblingIndex();
        if (placement == 0)
            return;

        transform.SetSiblingIndex(placement - 1);
        int priorityNumberInt = placement;
        priorityNumber.text = priorityNumberInt.ToString(); //adding one for more intuitive numbers
        currentPriorityNumber= priorityNumberInt;

        uiLaborPrioritizationManager.MovePriorityUp(this, priorityNumberInt);
    }

    public void MovePriorityDown()
    {
        //RepositionPanel(false);

        int placement = transform.GetSiblingIndex();
        if (placement == transform.parent.childCount - 1)
            return;

        transform.SetSiblingIndex(placement + 1);
        int priorityNumberInt = placement + 2;
        priorityNumber.text = priorityNumberInt.ToString(); //adding one for more intuitive numbers
        currentPriorityNumber = priorityNumberInt;

        uiLaborPrioritizationManager.MovePriorityDown(this, priorityNumberInt);
    }

    public void SetPriority(int priority)
    {
        currentPriorityNumber = priority;
        priorityNumber.text = priority.ToString();
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

    public void SetCaptionResourceInfo(ResourceType resourceType)
    {
        resourceList.options.Remove(defaultFirstChoice); //removing top choice

        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources.Concat(ResourceHolder.Instance.allWorldResources).ToList())
        {
            if (resourceType == resource.resourceType)
            {
                chosenResource = resource.resourceName;
                resourceList.value = resources.IndexOf(chosenResource);
                resourceList.RefreshShownValue();
            }
        }
    }

    public ResourceType GetChosenResource()
    {
        ResourceType chosenResourceType = ResourceType.None;
        
        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources.Concat(ResourceHolder.Instance.allWorldResources).ToList())
        {
            if (chosenResource == resource.resourceName)
                chosenResourceType = resource.resourceType;
        }

        return chosenResourceType;
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

    //used for closing priortizations individually
    public void CloseWindow()
    {
        uiLaborPrioritizationManager.RemoveFromResourcePriorityList(this);
        //resources.Clear();
        Destroy(gameObject);
    }

    //used for closing prioritization window
    public void RemoveWindow()
    {
        Destroy(gameObject);
    }
}
