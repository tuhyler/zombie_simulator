using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ResourceProducer : MonoBehaviour
{
    private ResourceManager resourceManager;
    //private TaskDataSO taskData;
    private ImprovementDataSO improvementData;
    private int currentLabor;
    private float tempLabor; //if adding labor during production process
    Queue<float> tempLaborPercsQueue = new();
    private Vector3 producerLoc;

    //for production info
    private Coroutine producingCo;
    private int productionTimer;
    private TimeProgressBar timeProgressBar;
    [HideInInspector]
    public bool isWaitingToStart, isWaitingToUnload;
    private float unloadLabor;
    private bool isProducing;
    [HideInInspector]
    public List<ResourceType> producedResources; //too see what this producer is making
    private Dictionary<ResourceType, float> generatedPerMinute = new();
    private Dictionary<ResourceType, float> consumedPerMinute = new();

    public void InitializeImprovementData(ImprovementDataSO data)
    {
        improvementData = data;
        
        foreach(ResourceValue resourceValue in data.producedResources)
        {
            producedResources.Add(resourceValue.resourceType);
        }
    }

    internal void SetResourceManager(ResourceManager resourceManager)
    {
        this.resourceManager = resourceManager;
    }

    public void SetLocation(Vector3Int loc)
    {
        producerLoc = loc;
        //if (loc != resourceManager.city.cityLoc)
        //    isImprovement = true;

        //if (isImprovement)
        SetProgressTimeBar();
    }

    private void SetProgressTimeBar()
    {
        producerLoc.z -= 1.5f; //bottom center of tile
        GameObject gameObject = Instantiate(GameAssets.Instance.timeProgressPrefab, producerLoc, Quaternion.Euler(90, 0, 0));
        timeProgressBar = gameObject.GetComponent<TimeProgressBar>();
        //timeProgressBar.SetTimeProgressBarValue(improvementData.producedResourceTime);
    }

    public void UpdateCurrentLaborData(int currentLabor)
    {
        this.currentLabor = currentLabor;
        CalculateResourceGenerationPerMinute();
        CalculateResourceConsumedPerMinute();
    }

    //public bool CheckResourceManager(ResourceManager resourceManager)
    //{
    //    return this.resourceManager = resourceManager;
    //}

    public void ShowConstructionProgressTimeBar(int time, bool active)
    {
        //Vector3 pos = transform.position;
        //pos.z += -1f;
        //timeProgressBar.gameObject.transform.position = pos;
        //timeProgressBar.SetConstructionTime(time);
        timeProgressBar.SetTimeProgressBarValue(time);
        if (active)
            timeProgressBar.SetActive(true);
    }

    public void HideConstructionProgressTimeBar()
    {
        timeProgressBar.SetTimeProgressBarValue(improvementData.producedResourceTime);
        timeProgressBar.SetActive(false);
    }

    public void SetConstructionTime(int time)
    {
        timeProgressBar.SetTime(time);
    }


    //for producing resources
    public void StartProducing()
    {
        if (resourceManager.fullInventory)
        {
            AddToProduceStartWaitList();
            return;
        }
        
        CalculateResourceGenerationPerMinute();
        CalculateResourceConsumedPerMinute();

        if (resourceManager.city.activeCity)
            timeProgressBar.SetActive(true);
        producingCo = StartCoroutine(ProducingCoroutine());
        isProducing = true;
    }

    public void AddLaborMidProduction()
    {
        CalculateResourceGenerationPerMinute();
        CalculateResourceConsumedPerMinute();

        float percWorked = (float)productionTimer / improvementData.producedResourceTime;
        tempLabor += percWorked;
        tempLaborPercsQueue.Enqueue(tempLabor);
        resourceManager.ConsumeResources(improvementData.consumedResources, tempLabor);
    }

    public void RemoveLaborMidProduction()
    {
        CalculateResourceGenerationPerMinute();
        CalculateResourceConsumedPerMinute();

        if (tempLaborPercsQueue.Count > 0)
            resourceManager.PrepareResource(improvementData.consumedResources, tempLaborPercsQueue.Dequeue(), producerLoc, true);
        else
            resourceManager.PrepareResource(improvementData.consumedResources, 1, producerLoc, true);
    }

    //timer for producing resources 
    private IEnumerator ProducingCoroutine()
    {
        productionTimer = improvementData.producedResourceTime;
        if (resourceManager.city.activeCity)
        {
            timeProgressBar.SetProgressBarBeginningPosition();
            timeProgressBar.SetTime(productionTimer);
        }

        tempLabor = currentLabor;
        resourceManager.ConsumeResources(improvementData.consumedResources, currentLabor);
        
        while (productionTimer > 0)
        {
            yield return new WaitForSeconds(1);
            productionTimer--;
            if (resourceManager.city.activeCity)
                timeProgressBar.SetTime(productionTimer);
        }

        //checking of storage is free to unload
        if (resourceManager.fullInventory)
        {
            isWaitingToUnload = true;
            unloadLabor = tempLabor;
            resourceManager.waitingToUnloadProducers.Enqueue(this);
            timeProgressBar.SetToZero();
        }
        else
        {
            RestartProductionCheck(tempLabor);
        }
    }

    public void UnloadAndRestart()
    {
        if (isWaitingToUnload)
        {
            isWaitingToUnload = false;
            timeProgressBar.ResetProgressBar();
            RestartProductionCheck(unloadLabor);
        }
    }

    public void RestartProductionCheck(float labor)
    {
        resourceManager.PrepareResource(improvementData.producedResources, labor, producerLoc);
        Debug.Log("Resources for " + improvementData.prefab.name);

        //checking storage again after loading
        if (resourceManager.fullInventory)
        {
            AddToProduceStartWaitList();
            timeProgressBar.SetActive(false);
        }
        else
        {
            tempLaborPercsQueue.Clear();
            producingCo = StartCoroutine(ProducingCoroutine());
        }
    }

    public void StopProducing()
    {
        CalculateResourceGenerationPerMinute();
        CalculateResourceConsumedPerMinute();

        if (producingCo != null)
        {
            StopCoroutine(producingCo);
            resourceManager.PrepareResource(improvementData.consumedResources, 1, producerLoc, true);
        }

        if (isWaitingToStart)
        {
            resourceManager.city.RemoveFromWaitToStartList(this);
            isWaitingToStart = false;
        }
        if (isWaitingToUnload)
        {
            resourceManager.RemoveFromWaitUnloadQueue(this);
            isWaitingToUnload = false;
        }
        timeProgressBar.SetActive(false);
        isProducing = false;
    }

    private void AddToProduceStartWaitList()
    {
        isWaitingToStart = true;
        resourceManager.city.AddToWaitToStartList(this);

    }

    public void SetTimeProgressBarToZero()
    {
        timeProgressBar.SetToZero();
    }

    public void TimeProgressBarSetActive(bool v)
    {
        if (isProducing)
        {
            timeProgressBar.SetActive(v);
            if (v)
            {
                timeProgressBar.SetProgressBarMask(productionTimer);
                timeProgressBar.SetTime(productionTimer);
                //timeProgressBar.SetProgressBarMask();
            }
        }
    }

    public void TimeConstructionProgressBarSetActive(bool v, int time)
    {
        timeProgressBar.SetActive(v);
        if (v)
        {
            timeProgressBar.SetProgressBarMask(time);
            timeProgressBar.SetTime(time);
        }
    }

    //recalculating generation per resource every time labor/work ethic changes
    public void CalculateResourceGenerationPerMinute()
    {
        foreach (ResourceValue resourceValue in improvementData.producedResources)
        {
            if (!generatedPerMinute.ContainsKey(resourceValue.resourceType))
                generatedPerMinute[resourceValue.resourceType] = 0;
            else
                resourceManager.ModifyResourceGenerationPerMinute(resourceValue.resourceType, generatedPerMinute[resourceValue.resourceType], false);

            int amount = Mathf.RoundToInt(resourceManager.CalculateResourceGeneration(resourceValue.resourceAmount, currentLabor) * (60f / improvementData.producedResourceTime));
            generatedPerMinute[resourceValue.resourceType] = amount;
            resourceManager.ModifyResourceGenerationPerMinute(resourceValue.resourceType, amount, true);

            if (amount == 0)
                generatedPerMinute.Remove(resourceValue.resourceType);
        }
    }

    //only changes when labor changes, not work ethic
    private void CalculateResourceConsumedPerMinute()
    {
        foreach (ResourceValue resourceValue in improvementData.consumedResources)
        {
            if (!consumedPerMinute.ContainsKey(resourceValue.resourceType))
                consumedPerMinute[resourceValue.resourceType] = 0;
            else
                resourceManager.ModifyResourceConsumptionPerMinute(resourceValue.resourceType, consumedPerMinute[resourceValue.resourceType], false);

            int amount = Mathf.RoundToInt((resourceValue.resourceAmount * currentLabor) * (60f / improvementData.producedResourceTime));
            consumedPerMinute[resourceValue.resourceType] = amount;
            resourceManager.ModifyResourceConsumptionPerMinute(resourceValue.resourceType, amount, true);

            if (amount == 0)
                generatedPerMinute.Remove(resourceValue.resourceType);
        }
    }
}
