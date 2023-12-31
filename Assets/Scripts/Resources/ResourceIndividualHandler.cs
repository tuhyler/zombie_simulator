using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class ResourceIndividualHandler : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;
    [SerializeField]
    private WorkerTaskManager workerTaskManager;

    [SerializeField]
    public int timePassed;
    private WaitForSeconds oneSecondWait = new WaitForSeconds(1);

    //public ResourceIndividualSO GetResourcePrefab(Vector3Int workerPos)
    //{
    //    TerrainData td = world.GetTerrainDataAt(workerPos);
    //    ResourceType rt = td.resourceType;

    //    foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
    //    {
    //        if (rt == resource.resourceType)
    //            return resource;
    //    }

    //    return null; //returns nothing in case nothing is found
    //}

    public IEnumerator GenerateHarvestedResource(Vector3 unitPos, Worker worker, City city, ResourceIndividualSO resourceIndividual, bool clearForest)
    {
        //int timePassed;
        Vector3Int pos = world.GetClosestTerrainLoc(unitPos);

		if (clearForest)
        {
			//timePassed = worker.clearingForestTime;
			worker.ShowProgressTimeBar(worker.clearingForestTime);
		}
        else
        {
            //timePassed = resourceIndividual.ResourceGatheringTime;
            worker.ShowProgressTimeBar(resourceIndividual.ResourceGatheringTime);
        }

        if (clearForest)
            worker.SetWorkAnimation(true);
        else
			worker.SetGatherAnimation(true);

		worker.SetTime(timePassed);

        while (timePassed > 0)
        {
            yield return oneSecondWait;
            timePassed--;
            worker.SetTime(timePassed);
        }

        workerTaskManager.taskCoroutine = null;
        worker.HideProgressTimeBar();
        if (clearForest)
            worker.SetWorkAnimation(false);
        else
			worker.SetGatherAnimation(false);

		if (world.mainPlayer.isSelected)
            workerTaskManager.TurnOffCancelTask();

        if (clearForest)
        {
            worker.clearingForest = false;
            //otherWorker.clearingForest = false;
            TerrainData td = world.GetTerrainDataAt(pos);
            td.beingCleared = false;
            td.ShowProp(false);
            //Destroy(td.treeHandler.gameObject);
			worker.marker.ToggleVisibility(false);
            //otherWorker.marker.ToggleVisibility(false);
            TerrainDataSO tempData;

			if (td.isHill)
            {
                tempData = td.terrainData.grassland ? world.grasslandHillTerrain : world.desertHillTerrain;
            }
            else
            {
                tempData = td.terrainData.grassland ? world.grasslandTerrain : world.desertTerrain;
            }

            td.SetNewData(tempData);
			GameLoader.Instance.gameData.allTerrain[pos].beingCleared = false;
            GameLoader.Instance.gameData.allTerrain[pos] = td.SaveData();

            if (!world.mainPlayer.buildingCity)
    			city.UpdateCityBools(ResourceType.Lumber);
		}
        //else
        //{
        //    if (otherWorker.isMoving)
        //    {
        //        otherWorker.WorkerOrdersPreparations();
        //    }
        //    else
        //    {
        //        otherWorker.SetGatherAnimation(false);
        //    }
        //}

        //showing harvested resource
        worker.gathering = false;
        //otherWorker.gathering = false;

        if (world.mainPlayer.buildingCity)
        {
            worker.clearedForest = true; 
            world.RemoveWorkerWorkLocation(pos);
            workerTaskManager.BuildCityPrep();
        }
        else
        {
            worker.harvested = true;
            worker.harvestedForest = clearForest;
            unitPos.y += 1.5f;
            GameObject resourceGO = Instantiate(GameAssets.Instance.resourceBubble, unitPos, Quaternion.Euler(90, 0, 0));
            GameLoader.Instance.textList.Add(resourceGO);
            Resource resource = resourceGO.GetComponent<Resource>();
            resource.SetSprites(resourceIndividual.resourceIcon);
            resource.SetInfo(worker, city, resourceIndividual, clearForest);
            Vector3 localScale = resourceGO.transform.localScale;
            resourceGO.transform.localScale = Vector3.zero;
            LeanTween.scale(resourceGO, localScale, 0.25f).setEase(LeanTweenType.easeOutBack);
        }
    }

    public void LoadHarvestedResource(Vector3 unitPos, ResourceIndividualSO resourceIndividual, City city, Worker worker, bool clearForest)
    {
		unitPos.y += 1.5f;
		GameObject resourceGO = Instantiate(GameAssets.Instance.resourceBubble, unitPos, Quaternion.Euler(90, 0, 0));
		GameLoader.Instance.textList.Add(resourceGO);
		Resource resource = resourceGO.GetComponent<Resource>();
		resource.SetSprites(resourceIndividual.resourceIcon);
		resource.SetInfo(worker, city, resourceIndividual, clearForest);
		Vector3 localScale = resourceGO.transform.localScale;
		resourceGO.transform.localScale = Vector3.zero;
		LeanTween.scale(resourceGO, localScale, 0.25f).setEase(LeanTweenType.easeOutBack);
	}
}
