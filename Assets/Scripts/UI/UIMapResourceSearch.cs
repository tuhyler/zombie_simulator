using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class UIMapResourceSearch : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;
    
    [SerializeField]
    private UIMapHandler mapHandler;

    [SerializeField]
    private Sprite originalResourceBackground, highlightResourceBackground;
    //[HideInInspector]
    //public UIMapPanel mapPanel;
    
    [SerializeField]
    private TMP_Dropdown resourceList;
    private List<string> resources = new();

    [SerializeField]
    private TMP_Dropdown.OptionData defaultFirstChoice;

    private Dictionary<ResourceType, List<Vector3Int>> resourceLocDict = new();
    private ResourceType selectedResource;

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
            if (resource.rawResource != RawResourceType.None)
            {
                if (/*resource.resourceType == ResourceType.Food || */resource.resourceType == ResourceType.None || resource.resourceType == ResourceType.Lumber /*|| resource.resourceType == ResourceType.Fish*/)
                    continue;

                allResources.Add(new TMP_Dropdown.OptionData(resource.resourceName, resource.resourceIcon));
            }
        }

        AddResources(allResources);
    }

    public void AddResourceToDict(Vector3Int loc, ResourceType type)
    {
        if (!resourceLocDict.ContainsKey(type))
            resourceLocDict[type] = new List<Vector3Int>();

        resourceLocDict[type].Add(loc);
    }

    public void RemoveResourceFromDict(Vector3Int loc, ResourceType type)
    {
        resourceLocDict[type].Remove(loc);

        if (resourceLocDict[type].Count == 0)
            resourceLocDict.Remove(type);
    }

    public void ResetResourceLocDict()
    {
        resourceLocDict.Clear();
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
        
        DisableHighlights();
        
        //highlighting tiles
        if (chosenResource != "None")
        {

            ResourceType type = GetChosenResource(chosenResource);

            if (!resourceLocDict.ContainsKey(type))
                return;

            selectedResource = type;

            foreach (Vector3Int tile in resourceLocDict[type])
            {
                TerrainData td = world.GetTerrainDataAt(tile);
                td.EnableHighlight(Color.white);
                world.HighlightResourceIcon(td.TileCoordinates, highlightResourceBackground);
            }
        }
        else
        {
            selectedResource = ResourceType.None;
        }
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

    private void AddResources(List<TMP_Dropdown.OptionData> resources)
    {
        PrepareResourceList();

        foreach (var resource in resources)
        {
            this.resources.Add(resource.text);
        }

        resourceList.AddOptions(resources);
    }

    public void DisableHighlights()
    {
        if (selectedResource == ResourceType.None)
            return;
        
        foreach (Vector3Int tile in resourceLocDict[selectedResource])
        {
            TerrainData td = world.GetTerrainDataAt(tile);
            td.DisableHighlight();
            world.RestoreResourceIcon(td.TileCoordinates, originalResourceBackground);
        }

        selectedResource = ResourceType.None;
    }
}
