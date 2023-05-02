using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITradeRouteStopHolder : MonoBehaviour
{
    [SerializeField]
    private RectTransform allContents;
    
    [HideInInspector]
    public int loc;
    [HideInInspector]
    public UITradeStopHandler stopHandler;

    public void MoveStop(UITradeRouteStopHolder newDrop)
    {
        //stopHandler.loc = newDrop.loc;
        newDrop.stopHandler = stopHandler;

        Vector3 newLoc = newDrop.transform.position;
        //int test = newDrop.stopHandler.resourceCount;
        //newLoc.y += 70 * test;
        stopHandler.transform.SetParent(newDrop.transform);

        LeanTween.move(stopHandler.gameObject, newLoc, 0.2f).setEaseOutSine().setOnComplete(SetNewParent);
    }

    public void SetNewParent()
    {
        stopHandler.transform.localPosition = Vector3.zero;
    }
}
