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
    private float tempLabor; //if adding labor during production process
    Queue<float> tempLaborPercsQueue = new();
    private Vector3 producerLoc;

    //for production info
    private Coroutine producingCo;
    private int productionTimer;
    private TimeProgressBar timeProgressBar;
    private bool isProducing;
    private Dictionary<ResourceType, float> generatedPerMinute = new();
    private Dictionary<ResourceType, float> consumedPerMinute = new();

    public void InitializeImprovementData(ImprovementDataSO data)
    {
        myImprovementData = data;
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
        //timeProgressBar.SetTimeProgressBarValue(myImprovementData.producedResourceTime);
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
        timeProgressBar.SetTimeProgressBarValue(myImprovementData.producedResourceTime);
        timeProgressBar.SetActive(false);
    }

    public void SetConstructionTime(int time)
    {
        timeProgressBar.SetTime(time);
    }


    //for producing resources
    public void StartProducing()
    {
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

        float percWorked = (float)productionTimer / myImprovementData.producedResourceTime;
        tempLabor += percWorked;
        tempLaborPercsQueue.Enqueue(tempLabor);
        resourceManager.ConsumeResources(myImprovementData.consumedResources, tempLabor);
    }

    public void RemoveLaborMidProduction()
    {
        CalculateResourceGenerationPerMinute();
        CalculateResourceConsumedPerMinute();

        if (tempLaborPercsQueue.Count > 0)
            resourceManager.PrepareResource(myImprovementData.consumedResources, tempLaborPercsQueue.Dequeue(), true);
        else
            resourceManager.PrepareResource(myImprovementData.consumedResources, 1, true);
    }

    //timer for producing resources 
    private IEnumerator ProducingCoroutine()
    {
        productionTimer = myImprovementData.producedResourceTime;
        if (resourceManager.city.activeCity)
        {
            timeProgressBar.SetTime(productionTimer);
        }

        tempLabor = currentLabor;
        resourceManager.ConsumeResources(myImprovementData.consumedResources, currentLabor);
        
        while (productionTimer > 0)
        {
            yield return new WaitForSeconds(1);
            productionTimer--;
            if (resourceManager.city.activeCity)
                timeProgressBar.SetTime(productionTimer);
        }

        resourceManager.PrepareResource(myImprovementData.producedResources, tempLabor);
        Debug.Log("Resources for " + myImprovementData.prefab.name);

        if (currentLabor > 0)
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
            resourceManager.PrepareResource(myImprovementData.consumedResources, 1, true);
        }

        timeProgressBar.SetActive(false);
        isProducing = false;
    }

    public void TimeProgressBarSetActive(bool v)
    {
        if (isProducing)
        {
            timeProgressBar.SetTime(productionTimer);
            timeProgressBar.SetActive(v);
        }
    }

    public void TimeConstructionProgressBarSetActive(bool v, int time)
    {
        if (v)
        {
            timeProgressBar.SetTime(time);
        }

        timeProgressBar.SetActive(v);
    }

    //recalculating generation per resource every time labor/work ethic changes
    public void CalculateResourceGenerationPerMinute()
    {
        foreach (ResourceValue resourceValue in myImprovementData.producedResources)
        {
            if (!generatedPerMinute.ContainsKey(resourceValue.resourceType))
                generatedPerMinute[resourceValue.resourceType] = 0;
            else
                resourceManager.ModifyResourceGenerationPerMinute(resourceValue.resourceType, generatedPerMinute[resourceValue.resourceType], false);

            int amount = Mathf.RoundToInt(resourceManager.CalculateResourceGeneration(resourceValue.resourceAmount, currentLabor) * (60f / myImprovementData.producedResourceTime));
            generatedPerMinute[resourceValue.resourceType] = amount;
            resourceManager.ModifyResourceGenerationPerMinute(resourceValue.resourceType, amount, true);

            if (amount == 0)
                generatedPerMinute.Remove(resourceValue.resourceType);
        }
    }

    //only changes when labor changes, not work ethic
    private void CalculateResourceConsumedPerMinute()
    {
        foreach (ResourceValue resourceValue in myImprovementData.consumedResources)
        {
            if (!consumedPerMinute.ContainsKey(resourceValue.resourceType))
                consumedPerMinute[resourceValue.resourceType] = 0;
            else
                resourceManager.ModifyResourceConsumptionPerMinute(resourceValue.resourceType, consumedPerMinute[resourceValue.resourceType], false);

            int amount = Mathf.RoundToInt((resourceValue.resourceAmount * currentLabor) * (60f / myImprovementData.producedResourceTime));
            consumedPerMinute[resourceValue.resourceType] = amount;
            resourceManager.ModifyResourceConsumptionPerMinute(resourceValue.resourceType, amount, true);

            if (amount == 0)
                generatedPerMinute.Remove(resourceValue.resourceType);
        }
    }
}
