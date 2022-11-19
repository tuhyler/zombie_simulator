using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UILaborResourcePriority : MonoBehaviour
{
    [SerializeField]
    private TMP_Dropdown resourceList;

    [SerializeField]
    private TMP_Text priorityNumber;

    private UICityLaborPrioritizationManager uiLaborPrioritizationManager;

    private List<string> resources = new();

    private string chosenResource;

    [SerializeField]
    private TMP_Dropdown.OptionData defaultFirstChoice;


    public void MovePriorityUp()
    {
        int placement = transform.GetSiblingIndex();
        if (placement == 0)
            return;

        transform.SetSiblingIndex(placement - 1);
    }

    public void MovePriorityDown()
    {
        int placement = transform.GetSiblingIndex();
        if (placement == transform.parent.childCount - 1)
            return;

        transform.SetSiblingIndex(placement + 1);
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
}
