using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Worker : Unit, ITurnDependent
{
    [HideInInspector]
    public bool harvesting, harvested, resourceIsNotNull;
    private ResourceIndividualHandler resourceIndividualHandler;
 
    public void SendResourceToCity()
    {
        (City city, ResourceIndividualSO resourceIndividual) = resourceIndividualHandler.GetResourceGatheringDetails();
        city.ResourceManager.CheckResource(resourceIndividual.resourceType, 1); //only add one of respective resource
        harvested = false;

        resourceIndividualHandler.NullHarvestValues();
        resourceIndividualHandler = null;
    }

    internal void PrepResourceGathering(ResourceIndividualHandler resourceIndividualHandler)
    {
        this.resourceIndividualHandler = resourceIndividualHandler;
    }

    private void HarvestResource()
    {
        if (harvesting)
        {
            harvesting = false;
            harvested = true;
            resourceIsNotNull = false;
            resourceIndividualHandler.SetResourceActive();
        }
    }

    public override void WaitTurnMethods()
    {
        base.WaitTurnMethods();
        if (harvesting && resourceIsNotNull)
            HarvestResource();
    }

    public new void WaitTurn()
    {
        WaitTurnMethods();
    }
}