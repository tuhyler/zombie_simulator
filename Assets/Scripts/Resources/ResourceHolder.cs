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

    public ResourceIndividualSO GetData(ResourceType resourceType)
    {
        return resourceDict[resourceType];
    }

    public Sprite GetIcon(ResourceType resourceType)
    {
        return resourceDict[resourceType].resourceIcon;
    }

    public Vector2 GetUVs(ResourceType resourceType)
    {
        return resourceDict[resourceType].uvCoordinatesForRocks;
    }

    public string GetRequirement(ResourceType resourceType)
    {
        return resourceDict[resourceType].requirement;
    }

    public string GetName(ResourceType resourceType)
    {
        return resourceDict[resourceType].resourceName;
    }

    public RawResourceType GetRawResourceType(ResourceType resourceType)
    {
        return resourceDict[resourceType].rawResource;
    }

    public RocksType GetRocksType(ResourceType resourceType)
    {
        return resourceDict[resourceType].rocksType;
    }
}
