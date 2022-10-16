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

    //for production info
    private Coroutine producingCo;
    private int productionTimer;
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

    public void UpdateCurrentLaborData(int currentLabor)
    {
        this.currentLabor = currentLabor;
        CalculateResourceGenerationPerMinute();
        CalculateResourceConsumedPerMinute();
    }

    public bool CheckResourceManager(ResourceManager resourceManager)
    {
        return this.resourceManager = resourceManager;
    }



    //for producing resources
    public void StartProducing()
    {
        CalculateResourceGenerationPerMinute();
        CalculateResourceConsumedPerMinute();
        StartCoroutine(ProducingCoroutine());
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
        tempLabor = currentLabor;
        resourceManager.ConsumeResources(myImprovementData.consumedResources, currentLabor);
        
        while (productionTimer > 0)
        {
            yield return new WaitForSeconds(1);
            productionTimer--;
        }

        resourceManager.PrepareResource(myImprovementData.producedResources, tempLabor);
        Debug.Log("Resources for " + myImprovementData.prefab.name);

        if (currentLabor > 0)
        {
            tempLaborPercsQueue.Clear();
            StartCoroutine(ProducingCoroutine());
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
