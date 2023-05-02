using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UILaborResourcePriorityHolder : MonoBehaviour
{
    [HideInInspector]
    public int loc;
    [HideInInspector]
    public UILaborResourcePriority resource;

    public void MoveStop(UILaborResourcePriorityHolder newDrop)
    {
        //stopHandler.loc = newDrop.loc;
        newDrop.resource = resource;

        Vector3 newLoc = newDrop.transform.position;
        //int test = newDrop.stopHandler.resourceCount;
        //newLoc.y += 70 * test;
        resource.transform.SetParent(newDrop.transform);

        LeanTween.move(resource.gameObject, newLoc, 0.2f).setEaseOutSine().setOnComplete(SetNewParent);
    }

    public void SetNewParent()
    {
        resource.transform.localPosition = Vector3.zero;
    }
}
