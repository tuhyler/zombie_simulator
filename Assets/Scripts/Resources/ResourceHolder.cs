using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ResourceHolder : MonoBehaviour
{
    public static ResourceHolder Instance { get; private set; }

    public List<ResourceIndividualSO> allStorableResources = new(); //not static so as to populate lists in inspector
    public List<ResourceIndividualSO> allWorldResources = new();
    private Dictionary<ResourceType, Sprite> resourceIconDict = new();

    private void Awake()
    {
        Instance = this;
        PopulateDict();
    }

    private void PopulateDict()
    {
        foreach (var resource in allStorableResources.Concat(allWorldResources))
        {
            resourceIconDict[resource.resourceType] = resource.resourceIcon;
        }
    }

    public Sprite GetIcon(ResourceType resourceType)
    {
        return resourceIconDict[resourceType];
    }
}
