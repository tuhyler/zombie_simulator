using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceIndividualHandler : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;

    [SerializeField]
    private List<ResourceIndividualSO> resource;

    private City tempCity;
    private Worker workerUnit;
    private ResourceIndividualSO resourceIndividual;
    private GameObject resourceGO;


    public void HandleResourceBubbleSelection(Vector3 location, GameObject selectedObject)
    {
        if (selectedObject == null)
            return;

        if (selectedObject.TryGetComponent(out Resource resource))
        {
            if (resource != null)
            {
                SendResourceToCity();
            }
        }
    }

    public (City city, ResourceIndividualSO resourceIndividual) GetResourceGatheringDetails()
    {
        return (tempCity, resourceIndividual);
    }

    private void SendResourceToCity()
    {
        //Unit harvestingUnit = world.GetUnit(world.GetClosestTile(resource.transform.position));
        workerUnit.SendResourceToCity();
    }

    public bool CheckForCity(Vector3Int workerPos) //finds if city is nearby, returns it (interface? in WorkerTaskManager)
    {
        foreach (Vector3Int tile in world.GetNeighborsFor(workerPos, MapWorld.State.EIGHTWAYTWODEEP))
        {
            if (!world.IsCityOnTile(tile))
            {
                continue;
            }
            else
            {
                tempCity = world.GetCity(tile);
                return true;
            }
        }

        Debug.Log("No nearby city in which to store resource");
        return false;
    }

    internal void SetResourceActive()
    {
        resourceGO.SetActive(true);
    }

    public ResourceIndividualSO GetResourcePrefab(Vector3Int workerPos)
    {
        TerrainData td = world.GetTerrainDataAt(workerPos);
        ResourceType rt = td.GetTerrainData().resourceType;

        foreach (ResourceIndividualSO resource in resource)
        {
            if (rt == resource.resourceType)
            {
                resourceIndividual = resource;
                return resource;
            }
        }

        return null; //returns nothing in case nothing is found
    }


    public void GenerateHarvestedResource(ResourceIndividualSO resourceIndividual, Vector3 unitPos, Worker workerUnit)
    {
        this.workerUnit = workerUnit;
        workerUnit.PrepResourceGathering(this);
        unitPos.y += 1.0f; //setting it up to float above worker's head
        resourceGO = Instantiate(resourceIndividual.prefab, unitPos, Quaternion.identity);
        workerUnit.resourceIsNotNull = true;
        resourceGO.SetActive(false);
        this.workerUnit.harvesting = true;
    }

    public void NullHarvestValues() //nulling out all the values used to harvest resources
    {
        Destroy(resourceGO);
        tempCity = null;
        resourceIndividual = null;
    }
}
