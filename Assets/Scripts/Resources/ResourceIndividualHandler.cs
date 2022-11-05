using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceIndividualHandler : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;
    [SerializeField]
    private WorkerTaskManager workerTaskManager;

    //private City tempCity;
    //private Worker workerUnit;
    //private ResourceIndividualSO resourceIndividual;
    //private GameObject resourceGO;


    public void HandleResourceBubbleSelection(Vector3 location, GameObject selectedObject)
    {
        if (selectedObject == null)
            return;

        if (selectedObject.TryGetComponent(out Resource resource))
        {
            if (resource != null)
            {
                //resourceGO = resource.gameObject;
                resource.SendResourceToCity();
            }
        }
    }

    //public (City city, ResourceIndividualSO resourceIndividual) GetResourceGatheringDetails()
    //{
    //    return (tempCity, resourceIndividual);
    //}

    //private void SendResourceToCity()
    //{
    //    //Unit harvestingUnit = world.GetUnit(world.GetClosestTile(resource.transform.position));
    //    workerUnit.SendResourceToCity();
    //}

    //public bool CheckForCity(Vector3Int workerPos) //finds if city is nearby, returns it (interface? in WorkerTaskManager)
    //{
    //    foreach (Vector3Int tile in world.GetNeighborsFor(workerPos, MapWorld.State.EIGHTWAYTWODEEP))
    //    {
    //        if (!world.IsCityOnTile(tile))
    //        {
    //            continue;
    //        }
    //        else
    //        {
    //            tempCity = world.GetCity(tile);
    //            return true;
    //        }
    //    }

    //    return false;
    //}

    //internal void SetResourceActive()
    //{
    //    resourceGO.SetActive(true);
    //}

    public ResourceIndividualSO GetResourcePrefab(Vector3Int workerPos)
    {
        TerrainData td = world.GetTerrainDataAt(workerPos);
        ResourceType rt = td.GetTerrainData().resourceType;

        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
        {
            if (rt == resource.resourceType)
            {
                //resourceIndividual = resource;
                return resource;
            }
        }

        return null; //returns nothing in case nothing is found
    }


    public IEnumerator GenerateHarvestedResource(Vector3 unitPos, Worker worker, City city, ResourceIndividualSO resourceIndividual)
    {
        int timePassed = resourceIndividual.ResourceGatheringTime;
        worker.ShowProgressTimeBar(timePassed);
        worker.SetTime(timePassed);

        while (timePassed > 0)
        {
            yield return new WaitForSeconds(1);
            timePassed--;
            worker.SetTime(timePassed);
        }

        worker.HideProgressTimeBar();
        workerTaskManager.TurnOffCancelTask();

        //showing harvested resource
        //worker.PrepResourceGathering(this);
        worker.harvested = true;
        unitPos.x += 1f;
        unitPos.z += 1f;
        //unitPos += Vector3.one; //setting it up to float above worker's head
        GameObject resourceGO = Instantiate(GameAssets.Instance.resourceBubble, unitPos, Quaternion.Euler(90, 0, 0));
        Resource resource = resourceGO.GetComponent<Resource>();
        resource.SetSprites(resourceIndividual.resourceIcon);
        resource.SetInfo(worker, city, resourceIndividual);
        resourceGO.transform.localScale = Vector3.zero;
        LeanTween.scale(resourceGO, Vector3.one, 0.25f).setEase(LeanTweenType.easeOutBack);
        //ShowHarvestedResource(unitPos, worker);
    }


    //public void GenerateHarvestedResource(Vector3 unitPos, Worker workerUnit)
    //{
    //    this.workerUnit = workerUnit;
    //    workerUnit.PrepResourceGathering(this);
    //    unitPos.y += 1.0f; //setting it up to float above worker's head
    //    resourceGO = Instantiate(GameAssets.Instance.resourceBubble, unitPos, Quaternion.Euler(90, 0, 0));
    //    //resourceGO = Instantiate(resourceIndividual.prefab, unitPos, Quaternion.Euler(90,0,0));
    //    //workerUnit.resourceIsNotNull = true;
    //    resourceGO.SetActive(false);
    //    this.workerUnit.harvesting = true;
    //}

    //public void SetWorker(Worker workerUnit)
    //{
    //    this.workerUnit = workerUnit;
    //}

    //private void ShowHarvestedResource(Vector3 unitPos)
    //{
    //    workerUnit.PrepResourceGathering(this);
    //    workerUnit.harvested = true;
    //    unitPos.x += 1f;
    //    unitPos.z += 1f;
    //    //unitPos += Vector3.one; //setting it up to float above worker's head
    //    resourceGO = Instantiate(GameAssets.Instance.resourceBubble, unitPos, Quaternion.Euler(90, 0, 0));
    //    Resource resource = resourceGO.GetComponent<Resource>();
    //    resource.SetSprites(resourceIndividual.resourceIcon);
    //    resourceGO.transform.localScale = Vector3.zero;
    //    LeanTween.scale(resourceGO, Vector3.one, 0.25f).setEase(LeanTweenType.easeOutBack);
    //}

    //public void ResetHarvestValues() //nulling out all the values used to harvest resources
    //{
    //    //LeanTween.scale(resourceGO, Vector3.zero, 0.1f).setOnComplete(DestroyResourceIcon);
    //    //tempCity = null;
    //    //resourceIndividual = null;
    //    //workerUnit = null;
    //}

    //public void NullHarvestValues()
    //{
    //    //tempCity = null;
    //    //resourceIndividual = null;
    //    //workerUnit = null;
    //}

    //private void DestroyResourceIcon()
    //{
    //    Destroy(resourceGO);
    //}
}
