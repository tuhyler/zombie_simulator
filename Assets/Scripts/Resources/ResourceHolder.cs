using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ResourceHolder : MonoBehaviour
{
    public static ResourceHolder Instance { get; private set; }

    public List<ResourceIndividualSO> allStorableResources = new(); //not static so as to populate lists in inspector
    public List<ResourceIndividualSO> allWorldResources = new();
    private Dictionary<ResourceType, ResourceIndividualSO> resourceDict = new();

    private void Awake()
    {
        Instance = this;
        PopulateDict();
    }

    private void PopulateDict()
    {
        foreach (var resource in allStorableResources.Concat(allWorldResources))
        {
            resourceDict[resource.resourceType] = resource;

        }
    }

    public Sprite GetIcon(ResourceType resourceType)
    {
        return resourceDict[resourceType].resourceIcon;
    }

    public Vector2 GetUVs(ResourceType resourceType)
    {
        return resourceDict[resourceType].uvCoordinatesForRocks;
    }
}
