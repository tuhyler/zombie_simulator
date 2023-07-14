using System.Collections;
using System.Collections.Generic;
using System.Resources;
using UnityEngine;
using UnityEngine.EventSystems;

public class UITradeRouteResourceHolder : MonoBehaviour, IDropHandler
{
    [HideInInspector]
    public int loc;
    [HideInInspector]
    public UITradeResourceTask resourceTask;
    [HideInInspector]
    public UITradeStopHandler tradeStopHandler;

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        UITradeResourceTask resourceTask = dropped.GetComponent<UITradeResourceTask>();
        if (resourceTask == null || !tradeStopHandler.uiResourceTasks.Contains(resourceTask))
            return;
        resourceTask.originalParent = transform;
        tradeStopHandler.uiResourceTasks.Remove(resourceTask);
        tradeStopHandler.uiResourceTasks.Insert(loc, resourceTask); 
        tradeStopHandler.MoveResourceTask(resourceTask.loc, loc);
        this.resourceTask = resourceTask;
        this.resourceTask.resourceHolder = this;
        resourceTask.loc = loc;
        resourceTask.counter.text = (loc + 1).ToString() + '.';
    }

    public void MoveResourceTask(UITradeRouteResourceHolder newDrop)
    {
        resourceTask.loc = newDrop.loc;
        resourceTask.counter.text = (resourceTask.loc + 1).ToString() + '.';
        //newDrop.resourceTask.resourceHolder = this;
        resourceTask.resourceHolder = newDrop;
        newDrop.resourceTask = resourceTask;

        Vector3 newLoc = newDrop.transform.position;
        resourceTask.transform.SetParent(newDrop.transform);

        LeanTween.move(resourceTask.gameObject, newLoc, 0.2f).setEaseOutSine().setOnComplete(()=> { SetNewParent(newDrop); });
    }

    public void SetNewParent(UITradeRouteResourceHolder newDrop)
    {
        newDrop.resourceTask.transform.localPosition = Vector3.zero;
        resourceTask.transform.localPosition = Vector3.zero;
    }

    public void SetStop(UITradeStopHandler tradeStopHandler)
    {
        this.tradeStopHandler = tradeStopHandler;
    }

    public void CloseWindow(bool justOne)
    {
        tradeStopHandler.resourceCount--;
        tradeStopHandler.RemoveResource(resourceTask);

        if (justOne)
        {
            tradeStopHandler.AdjustResources(loc);
        }

        //tradeStopHandler.AddToResourceTaskPool(this);
        //resourceTask.resources.Clear();
        Destroy(gameObject);
    }
}
