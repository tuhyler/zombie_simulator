using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIPersonalDropLocation : MonoBehaviour, IDropHandler
{
    [HideInInspector]
    public int gridLocation;
    public UIPersonalResources resource;

    private UIPersonalResourceInfoPanel resourceManager;

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        UIPersonalResources resource = dropped.GetComponent<UIPersonalResources>();
        if (resource == null || resource.clickable)
            return;
        resource.originalParent = transform;
        resourceManager.MoveResources(resource.loc, gridLocation, resource.resourceType);
        this.resource = resource;
        resource.loc = gridLocation;
    }

    public void SetUIResourceManager(UIPersonalResourceInfoPanel resourceManager)
    {
        this.resourceManager = resourceManager;
    }

    public void MoveResource(UIPersonalDropLocation newDrop, bool left)
    {
        resource.loc = newDrop.gridLocation;
        newDrop.resource = resource;

        Vector3 newLoc = newDrop.transform.position;
        resource.transform.SetParent(newDrop.transform);
        int factor = left ? resourceManager.gridWidth : resourceManager.gridWidth - 1;

        if (newDrop.gridLocation != 0 && newDrop.gridLocation % factor == 0)
            resource.transform.localPosition = Vector3.zero;
        else
            LeanTween.moveX(resource.gameObject, newLoc.x, 0.2f).setEaseOutSine().setOnComplete(SetNewParent);
    }

    public void SetNewParent()
    {
        resource.transform.localPosition = Vector3.zero;
    }
}
