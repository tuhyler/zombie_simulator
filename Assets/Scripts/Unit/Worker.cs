using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Worker : Unit
{
    //[HideInInspector]
    //public bool harvested;// harvesting, resourceIsNotNull;
    private ResourceIndividualHandler resourceIndividualHandler;


    private void Awake()
    {
        AwakeMethods();
        isWorker = true;
    }

    protected override void AwakeMethods()
    {
        base.AwakeMethods();
    }

    public override void SendResourceToCity()
    {
        isBusy = false;
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

    //private void HarvestResource()
    //{
    //    if (harvesting)
    //    {
    //        harvesting = false;
    //        harvested = true;
    //        resourceIsNotNull = false;
    //        resourceIndividualHandler.SetResourceActive();
    //    }
    //}

    //protected override void WaitTurnMethods()
    //{
    //    base.WaitTurnMethods();
    //    if (harvesting && resourceIsNotNull)
    //        HarvestResource();
    //}

    //public new void WaitTurn()
    //{
    //    WaitTurnMethods();
    //}
}
