using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceProducer : MonoBehaviour
{
    private ResourceManager resourceManager;
    //private TaskDataSO taskData;
    private ImprovementDataSO myImprovementData;
    public ImprovementDataSO GetImprovementData { get { return myImprovementData; } }
    private int currentLabor;
    
    //for production info
    private Coroutine producingCo;
    bool suddenStop;



    public void InitializeImprovementData(ImprovementDataSO data)
    {
        myImprovementData = data;
    }

    public void BeginResourceGeneration()
    {
        currentLabor = 1;
        resourceManager.BeginResourceGeneration(myImprovementData.producedResources);
    }


    public void UpdateCurrentLaborData(int currentLabor, Vector3Int laborLocation)
    {
        this.currentLabor = currentLabor;
        resourceManager.UpdateResourceGeneration(myImprovementData.producedResources, laborLocation, this.currentLabor);
    }

    public void UpdateBuildingCurrentLaborData(int currentLabor, string buildingName)
    {
        this.currentLabor = currentLabor;
        resourceManager.UpdateBuildingResourceGeneration(myImprovementData.producedResources, buildingName, this.currentLabor);
    }

    public void UpdateResourceGenerationData(Vector3Int workedLocation)
    {
        resourceManager.UpdateResourceGeneration(myImprovementData.producedResources, workedLocation, currentLabor);
    }

    public void UpdateBuildingResourceGenerationData(string buildingName)
    {
        resourceManager.UpdateBuildingResourceGeneration(myImprovementData.producedResources, buildingName, currentLabor);
    }

    //public void WaitTurn()
    //{
    //    resourceManager.PrepareResource(myImprovementData.producedResources, currentLabor);
    //    //Debug.Log("Resources for " + myImprovementData.prefab.name);
    //}

    internal void SetResourceManager(ResourceManager resourceManager)
    {
        this.resourceManager = resourceManager;
    }

    public bool CheckResourceManager(ResourceManager resourceManager)
    {
        return this.resourceManager = resourceManager;
    }

    public void StartProducing()
    {
        suddenStop = false;
        StartCoroutine(ProducingCoroutine());
    }

    //timer for producing resources 
    private IEnumerator ProducingCoroutine()
    {
        bool timer = true;
        resourceManager.ConsumeResources(myImprovementData.consumedResources, currentLabor);
        
        while (timer)
        {
            yield return new WaitForSeconds(myImprovementData.producedResourceTime);
            timer = false;
        }

        if (!suddenStop)
        {
            resourceManager.PrepareResource(myImprovementData.producedResources, currentLabor);
            Debug.Log("Resources for " + myImprovementData.prefab.name);
        }

        if (currentLabor > 0)
            StartCoroutine(ProducingCoroutine());
    }

    public void StopProducing()
    {
        if (producingCo != null)
        {
            StopCoroutine(producingCo);
            suddenStop = true;
        }
    }
}
