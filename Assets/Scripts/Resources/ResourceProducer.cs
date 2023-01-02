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
    List<float> tempLaborPercsList = new();
    private Vector3 producerLoc;

    //for production info
    private Coroutine producingCo;
    private int productionTimer;
    //private TimeProgressBar timeProgressBar;
    private UITimeProgressBar uiTimeProgressBar;
    [HideInInspector]
    public bool isWaitingForStorageRoom, isWaitingforResources, isWaitingToUnload, isWaitingForResearch;
    private float unloadLabor;
    private bool isProducing;
    [HideInInspector]
    public List<ResourceType> producedResources; //too see what this producer is making
    private Dictionary<ResourceType, float> generatedPerMinute = new();
    private Dictionary<ResourceType, float> consumedPerMinute = new();
    [HideInInspector]
    public List<ResourceValue> consumedResources = new();
    [HideInInspector]
    public List<ResourceType> consumedResourceTypes = new();

    public void InitializeImprovementData(ImprovementDataSO data)
    {
        improvementData = data;
        consumedResources = new(data.consumedResources);
        ResourceValue laborCost;
        laborCost.resourceType = ResourceType.Gold;
        laborCost.resourceAmount = data.laborCost;
        consumedResources.Insert(0, laborCost);

        foreach (ResourceValue value in consumedResources)
        {
            consumedResourceTypes.Add(value.resourceType);
        }
        
        foreach(ResourceValue resourceValue in data.producedResources)
        {
            producedResources.Add(resourceValue.resourceType);
        }
    }

    public void SetResourceManager(ResourceManager resourceManager)
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
        Vector3 progressBarLoc = producerLoc; //bottom center of tile
        progressBarLoc.z -= 1.5f;
        //GameObject gameObject = Instantiate(GameAssets.Instance.timeProgressPrefab, progressBarLoc, Quaternion.Euler(90, 0, 0));
        //timeProgressBar = gameObject.GetComponent<TimeProgressBar>();

        GameObject progressBar = Instantiate(GameAssets.Instance.uiTimeProgressPrefab, progressBarLoc, Quaternion.Euler(90, 0, 0));
        uiTimeProgressBar = progressBar.GetComponent<UITimeProgressBar>();
        //timeProgressBar.SetTimeProgressBarValue(improvementData.producedResourceTime);
    }

    public void UpdateCurrentLaborData(int currentLabor)
    {
        this.currentLabor = currentLabor;
    }

    public void UpdateResourceGenerationData()
    {
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
        //timeProgressBar.SetTimeProgressBarValue(time);
        uiTimeProgressBar.SetTimeProgressBarValue(time);
        uiTimeProgressBar.SetToZero();
        if (active)
            uiTimeProgressBar.gameObject.SetActive(true);
        //timeProgressBar.SetActive(true);
    }

    public void HideConstructionProgressTimeBar()
    {
        uiTimeProgressBar.SetTimeProgressBarValue(improvementData.producedResourceTime);
        //timeProgressBar.SetActive(false);
        uiTimeProgressBar.gameObject.SetActive(false);
    }

    public void SetConstructionTime(int time)
    {
        //timeProgressBar.SetTime(time);
        uiTimeProgressBar.SetTime(time);
    }

    //checking if production can continue
    public void RestartResourceWaitProduction()
    {
        if (resourceManager.ConsumeResourcesCheck(consumedResources, currentLabor))
        {
            isWaitingforResources = false;
            resourceManager.RemoveFromResourceWaitList(this);
            resourceManager.RemoveFromResourcesNeededForProduction(consumedResourceTypes);

            StartProducing();
        }
    }

    //for producing resources
    public void StartProducing()
    {
        if (improvementData.resourceType == ResourceType.Research && !resourceManager.city.WorldResearchingCheck())
        {
            AddToResearchWaitList();
            return;
        }
        else if (resourceManager.fullInventory)
        {
            AddToStorageRoomWaitList();
            return;
        }
        else if (!resourceManager.ConsumeResourcesCheck(consumedResources, currentLabor))
        {
            AddToResourceWaitList();
            return;
        }

        CalculateResourceGenerationPerMinute();
        CalculateResourceConsumedPerMinute();

        if (resourceManager.city.activeCity)
            uiTimeProgressBar.gameObject.SetActive(true);
            //timeProgressBar.SetActive(true);
        producingCo = StartCoroutine(ProducingCoroutine());
        isProducing = true;
    }

    public void AddLaborMidProduction()
    {
        CalculateResourceGenerationPerMinute();
        CalculateResourceConsumedPerMinute();

        float percWorked;
        if (isWaitingforResources || resourceManager.fullInventory)
            percWorked = 0;
        else if (!ConsumeResourcesCheck())
            percWorked = 0;
        else
            percWorked = (float)productionTimer / improvementData.producedResourceTime;
        tempLabor += percWorked;
        tempLaborPercsList.Add(percWorked);
        resourceManager.ConsumeResources(consumedResources, percWorked, producerLoc);
    }

    public void RemoveLaborMidProduction()
    {
        CalculateResourceGenerationPerMinute();
        CalculateResourceConsumedPerMinute();

        if (!isWaitingForResearch && !isWaitingforResources && !isWaitingForStorageRoom)
        {
            int tempLaborPercCount = tempLaborPercsList.Count;

            if (tempLaborPercCount > 0)
            {
                float tempLaborRemoved = tempLaborPercsList[tempLaborPercCount-1]; //LIFO
                tempLaborPercsList.RemoveAt(tempLaborPercCount-1);
                tempLabor -= tempLaborRemoved;
                resourceManager.PrepareResource(consumedResources, tempLaborRemoved, producerLoc, true);
            }
            else
            {
                resourceManager.PrepareResource(consumedResources, 1, producerLoc, true);
            }
        }
    }

    //checking if one more labor can be added
    public bool ConsumeResourcesCheck()
    {
        return resourceManager.ConsumeResourcesCheck(consumedResources, 1);
    }

    //timer for producing resources 
    private IEnumerator ProducingCoroutine()
    {
        productionTimer = improvementData.producedResourceTime;
        if (resourceManager.city.activeCity)
        {
            //timeProgressBar.SetProgressBarBeginningPosition();
            //timeProgressBar.SetTime(productionTimer);
            uiTimeProgressBar.SetToZero();
            uiTimeProgressBar.SetTime(productionTimer);
        }

        tempLabor = currentLabor;
        resourceManager.ConsumeResources(consumedResources, currentLabor, producerLoc);
        
        while (productionTimer > 0)
        {
            yield return new WaitForSeconds(1);
            productionTimer--;
            if (resourceManager.city.activeCity)
                uiTimeProgressBar.SetTime(productionTimer);
            //timeProgressBar.SetTime(productionTimer);
        }

        //checking if still researching
        if (improvementData.resourceType == ResourceType.Research && !resourceManager.city.WorldResearchingCheck())
        {
            isWaitingToUnload = true;
            unloadLabor = tempLabor;
            resourceManager.waitingToUnloadResearch.Enqueue(this);
            //timeProgressBar.SetToZero();
            uiTimeProgressBar.SetToFull();
            resourceManager.city.AddToWorldResearchWaitList();
        }
        //checking of storage is free to unload
        else if (resourceManager.fullInventory)
        {
            isWaitingToUnload = true;
            unloadLabor = tempLabor;
            resourceManager.waitingToUnloadProducers.Enqueue(this);
            //timeProgressBar.SetToZero();
            uiTimeProgressBar.SetToFull();
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
            //timeProgressBar.ResetProgressBar();
            uiTimeProgressBar.SetToZero();
            RestartProductionCheck(unloadLabor);
        }
    }

    private void RestartProductionCheck(float labor)
    {
        resourceManager.PrepareResource(improvementData.producedResources, labor, producerLoc);
        Debug.Log("Resources for " + improvementData.prefab.name);

        //checking storage again after loading
        if (improvementData.resourceType == ResourceType.Research && !resourceManager.city.WorldResearchingCheck())
        {
            AddToResearchWaitList();
            //timeProgressBar.SetActive(false);
            uiTimeProgressBar.gameObject.SetActive(false);
        }
        else if (resourceManager.fullInventory)
        {
            AddToStorageRoomWaitList();
            uiTimeProgressBar.gameObject.SetActive(false);
            //timeProgressBar.SetActive(false);
        }
        else if (!resourceManager.ConsumeResourcesCheck(consumedResources, currentLabor))
        {
            AddToResourceWaitList();
            uiTimeProgressBar.gameObject.SetActive(false);
            //timeProgressBar.SetActive(false);
        }
        else
        {
            tempLaborPercsList.Clear();
            producingCo = StartCoroutine(ProducingCoroutine());
        }
    }

    public void StopProducing()
    {
        CalculateResourceGenerationPerMinute();
        CalculateResourceConsumedPerMinute();

        if (isWaitingForResearch)
        {
            resourceManager.RemoveFromResearchWaitlist(this);
            isWaitingForResearch = false;
        }
        else if (isWaitingForStorageRoom)
        {
            resourceManager.RemoveFromStorageRoomWaitList(this);
            isWaitingForStorageRoom = false;
        }
        else if (isWaitingforResources)
        {
            resourceManager.RemoveFromResourceWaitList(this);
            resourceManager.RemoveFromResourcesNeededForProduction(consumedResourceTypes);
            isWaitingforResources = false;
        }
        else if (isWaitingToUnload)
        {
            resourceManager.RemoveFromWaitUnloadQueue(this);
            resourceManager.RemoveFromWaitUnloadResearchQueue(this);
            isWaitingToUnload = false;
            resourceManager.PrepareResource(consumedResources, 1, producerLoc, true);
        }
        else if (producingCo != null)
        {
            StopCoroutine(producingCo);
            resourceManager.PrepareResource(consumedResources, 1, producerLoc, true);
        }

        //timeProgressBar.SetActive(false);
        uiTimeProgressBar.gameObject.SetActive(false);
        isProducing = false;
    }

    private void AddToResearchWaitList()
    {
        isWaitingForResearch = true;
        resourceManager.AddToResearchWaitList(this);
        resourceManager.city.AddToWorldResearchWaitList();
    }

    private void AddToStorageRoomWaitList()
    {
        isWaitingForStorageRoom = true;
        resourceManager.AddToStorageRoomWaitList(this);
    }

    private void AddToResourceWaitList()
    {
        isWaitingforResources = true;
        resourceManager.AddToResourceWaitList(this);
        resourceManager.AddToResourcesNeededForProduction(consumedResourceTypes);
    }

    public void SetTimeProgressBarToFull()
    {
        uiTimeProgressBar.SetToFull();
    }

    public void TimeProgressBarSetActive(bool v)
    {
        if (isProducing)
        {
            //timeProgressBar.SetActive(v);
            uiTimeProgressBar.gameObject.SetActive(v);
            if (v)
            {
                uiTimeProgressBar.SetProgressBarMask(productionTimer);
                uiTimeProgressBar.SetTime(productionTimer);
            }
        }
    }

    public void TimeConstructionProgressBarSetActive(bool v, int time)
    {
        //timeProgressBar.SetActive(v);
        uiTimeProgressBar.gameObject.SetActive(true);
        if (v)
        {
            uiTimeProgressBar.SetProgressBarMask(time);
            uiTimeProgressBar.SetTime(time);
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
        foreach (ResourceValue resourceValue in consumedResources)
        {
            if (!consumedPerMinute.ContainsKey(resourceValue.resourceType))
                consumedPerMinute[resourceValue.resourceType] = 0;
            else
                resourceManager.ModifyResourceConsumptionPerMinute(resourceValue.resourceType, consumedPerMinute[resourceValue.resourceType], false);

            int amount = Mathf.RoundToInt((resourceValue.resourceAmount * currentLabor) * (60f / improvementData.producedResourceTime));
            consumedPerMinute[resourceValue.resourceType] = amount;
            resourceManager.ModifyResourceConsumptionPerMinute(resourceValue.resourceType, amount, true);

            if (amount == 0)
                consumedPerMinute.Remove(resourceValue.resourceType);
        }
    }
}
