using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class UIDropLocation : MonoBehaviour, /*IDropHandler, */IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector]
    public int gridLocation;
    public UIResources resource;

    private bool main;
    private UIResourceManager resourceManager;


    public void OnPointerEnter(PointerEventData eventData)
    {
        if (resourceManager.dragging)
        {
            main = true;
            GameObject dropped = eventData.pointerDrag;
            UIResources resource = dropped.GetComponent<UIResources>();
            if (resource == null)
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
    //    //GameObject dropped = eventData.pointerDrag;
    //    //UIResources resource = dropped.GetComponent<UIResources>();
    //    //if (resource == null)
    //    //    return;
    //    //resource.originalParent = transform;
    //    //resourceManager.MoveResources(resource.loc, gridLocation, resource.resourceType);
    //    //this.resource = resource;
    //    //resource.loc = gridLocation;
    //}

    public void SetUIResourceManager(UIResourceManager resourceManager)
    {
        this.resourceManager = resourceManager;
        GetComponentInChildren<UIResources>().SetResourceManager(resourceManager);
    }

    public void MoveResource(UIDropLocation newDrop, bool left)
    {
        resource.loc = newDrop.gridLocation;
        newDrop.resource = resource;

        Vector3 newLoc = newDrop.transform.position;
        resource.transform.SetParent(newDrop.transform);
        int remainder = left ? 0 : resourceManager.gridWidth - 1;

        if (newDrop.gridLocation != 0 && newDrop.gridLocation % resourceManager.gridWidth == remainder)
            resource.transform.localPosition = Vector3.zero;
        else
            LeanTween.move(resource.gameObject, newLoc, 0.2f).setEaseOutSine().setOnComplete(SetToZero);
    }

    public void SetToZero()
    {
        if (!main)
            resource.transform.localPosition = Vector3.zero;
    }
}
