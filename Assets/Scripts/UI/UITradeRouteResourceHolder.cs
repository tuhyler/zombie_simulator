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

    private UITradeStopHandler tradeStopHandler;

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
        resourceTask.loc = loc;
        resourceTask.counter.text = (loc + 1).ToString() + '.';
    }

    public void MoveResourceTask(UITradeRouteResourceHolder newDrop)
    {
        resourceTask.loc = newDrop.loc;
        resourceTask.counter.text = (resourceTask.loc + 1).ToString() + '.';
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

    public void CloseWindow()
    {
        //resourceHolder = null;
        tradeStopHandler.resourceCount--;
        tradeStopHandler.RemoveResource(resourceTask);
        tradeStopHandler.AdjustResources(loc);
        //resourceTask.resources.Clear();
        Destroy(gameObject);
    }
}
