using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceHolder : MonoBehaviour
{
    public static ResourceHolder Instance { get; private set; }

    public List<ResourceIndividualSO> allStorableResources = new(); //not static so as to populate lists in inspector
    public List<ResourceIndividualSO> allWorldResources = new();

    private void Awake()
    {
        Instance = this;
    }
}
