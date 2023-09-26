using System.Collections;
using UnityEngine;

public class ResourceIndividualHandler : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;
    [SerializeField]
    private WorkerTaskManager workerTaskManager;

    private WaitForSeconds gatherResourceWait = new WaitForSeconds(1);

    public ResourceIndividualSO GetResourcePrefab(Vector3Int workerPos)
    {
        TerrainData td = world.GetTerrainDataAt(workerPos);
        ResourceType rt = td.terrainData.resourceType;

        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
        {
            if (rt == resource.resourceType)
                return resource;
        }

        return null; //returns nothing in case nothing is found
    }

    public IEnumerator GenerateHarvestedResource(Vector3 unitPos, Worker worker, City city, ResourceIndividualSO resourceIndividual, bool clearForest)
    {
        int timePassed;

        if (clearForest)
            timePassed = 1;
        else
            timePassed = resourceIndividual.ResourceGatheringTime;

        worker.ShowProgressTimeBar(timePassed);
        worker.SetWorkAnimation(true);
        worker.SetTime(timePassed);

        while (timePassed > 0)
        {
            yield return gatherResourceWait;
            timePassed--;
            worker.SetTime(timePassed);
        }

        workerTaskManager.taskCoroutine = null;
        worker.HideProgressTimeBar();
        worker.SetWorkAnimation(false);
        if (worker.isSelected)
            workerTaskManager.TurnOffCancelTask();

        if (clearForest)
        {
            TerrainData td = world.GetTerrainDataAt(world.RoundToInt(unitPos));
			td.prop.gameObject.SetActive(false);

            if (td.isHill)
            {
                td.terrainData = td.terrainData.grassland ? world.grasslandHillTerrain : world.desertHillTerrain;
                td.gameObject.tag = "Hill";
            }
            else
            {
                td.terrainData = td.terrainData.grassland ? world.grasslandTerrain : world.desertTerrain;
				td.gameObject.tag = "Flatland";
            }
		}

        //showing harvested resource
        worker.harvested = true;
        unitPos.y += 1.5f;
        GameObject resourceGO = Instantiate(GameAssets.Instance.resourceBubble, unitPos, Quaternion.Euler(90, 0, 0));
        Resource resource = resourceGO.GetComponent<Resource>();
        resource.SetSprites(resourceIndividual.resourceIcon);
        resource.SetInfo(worker, city, resourceIndividual, clearForest);
        Vector3 localScale = resourceGO.transform.localScale;
        resourceGO.transform.localScale = Vector3.zero;
        LeanTween.scale(resourceGO, localScale, 0.25f).setEase(LeanTweenType.easeOutBack);
    }
}
