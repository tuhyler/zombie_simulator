using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class UIMapResourceSearch : MonoBehaviour
{
    [SerializeField]
    private UIMapHandler mapHandler;
    //[HideInInspector]
    //public UIMapPanel mapPanel;
    
    [SerializeField]
    private TMP_Dropdown resourceList;
    private List<string> resources = new();

    [SerializeField]
    private TMP_Dropdown.OptionData defaultFirstChoice;

    private void Awake()
    {
        GetResources();
        gameObject.SetActive(false);
    }

    private void GetResources()
    {
        List<TMP_Dropdown.OptionData> allResources = new();

        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources.Concat(ResourceHolder.Instance.allWorldResources).ToList())
        {
            if (resource.resourceType == ResourceType.Food || resource.resourceType == ResourceType.None || resource.resourceType == ResourceType.Lumber || resource.resourceType == ResourceType.Fish)
                continue;

            if (resource.rawResource)
                allResources.Add(new TMP_Dropdown.OptionData(resource.resourceName, resource.resourceIcon));
        }

        AddResources(allResources);
    }

    public void SetChosenResource(int value)
    {
        string chosenResource;

        if (value == 0)
            chosenResource = "None";
        else
            chosenResource = resources[value - 1];

        resourceList.value = value;
        resourceList.RefreshShownValue();
        
        //mapPanel.HighlightTile(GetChosenResource(chosenResource));
    }

    public void ResetDropdown()
    {
        resourceList.value = 0;
        resourceList.RefreshShownValue();
    }

    private ResourceType GetChosenResource(string chosenResource)
    {
        ResourceType chosenResourceType = ResourceType.None;

        if (chosenResource == "None")
            return chosenResourceType;

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
}
