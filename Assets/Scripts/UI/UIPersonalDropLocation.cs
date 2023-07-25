using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIPersonalDropLocation : MonoBehaviour, /*IDropHandler, */IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector]
    public int gridLocation;
    public UIPersonalResources resource;
    private bool main;

    private UIPersonalResourceInfoPanel resourceManager;


    public void OnPointerEnter(PointerEventData eventData)
    {
        if (resourceManager.dragging)
        {
            main = true;
            GameObject dropped = eventData.pointerDrag;
            UIPersonalResources resource = dropped.GetComponent<UIPersonalResources>();
            if (resource == null || resource.clickable)
                return;
            resource.originalParent = transform;
            resourceManager.MoveResources(resource.loc, gridLocation, resource.resourceType);
            this.resource = resource;
            resource.loc = gridLocation;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        main = false;
    }

    //public void OnDrop(PointerEventData eventData)
    //{
    //    GameObject dropped = eventData.pointerDrag;
    //    UIPersonalResources resource = dropped.GetComponent<UIPersonalResources>();
    //    if (resource == null || resource.clickable)
    //        return;
    //    resource.originalParent = transform;
    //    resourceManager.MoveResources(resource.loc, gridLocation, resource.resourceType);
    //    this.resource = resource;
    //    resource.loc = gridLocation;
    //}

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
        int remainder = left ? 0 : resourceManager.gridWidth - 1;

        if (newDrop.gridLocation != 0 && newDrop.gridLocation % resourceManager.gridWidth == remainder)
            resource.transform.localPosition = Vector3.zero;
        else
            LeanTween.move(resource.gameObject, newLoc, 0.2f).setEaseOutSine().setOnComplete(SetNewParent);
    }

    public void SetNewParent()
    {
        if (!main)
            resource.transform.localPosition = Vector3.zero;
    }
}
