using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIResourceSelectionGrid : MonoBehaviour
{
    [SerializeField]
    private GameObject resourceSquare;

    [SerializeField]
    private RectTransform allContents, closeButton, rawHolder, rockHolder, buildingHolder, soldHolder, luxuryHolder;

    [HideInInspector]
    public bool activeStatus;
    private IResourceGridUser resourceGridUser;
    //private UITradeResourceTask resourceTask;
    //private UILaborResourcePriority laborResourcePriority;

    private void Awake()
    {
        float maxX = 290;
        float maxY = 70;
        int width = 50;

        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
        {
            if (resource.forVisual)
                continue;

            GameObject resourceGO = Instantiate(resourceSquare);
            AssignParent(resource.resourceCategory, resourceGO);
            UIResourceSquare uiResourceSquare = resourceGO.GetComponent<UIResourceSquare>();
            uiResourceSquare.SetInfo(resource.resourceType, resource.resourceName);
            uiResourceSquare.SetGrid(this);
            uiResourceSquare.resourceIcon.sprite = resource.resourceIcon;
        }

        int childMax = 0;

        if (rawHolder.childCount > 0)
        {
            maxY += width;

            if (rawHolder.childCount > childMax)
            {
                maxX = rawHolder.childCount * width;
                childMax = rawHolder.childCount;
            }
        }
        if (rockHolder.childCount > 0)
        {
            maxY += width;
            
            if (rockHolder.childCount > childMax)
            {
                maxX = rockHolder.childCount * width;
                childMax = rockHolder.childCount;
            }
        }
        if (buildingHolder.childCount > 0)
        {
            maxY += width;

            if (buildingHolder.childCount > childMax)
            {
                maxX = buildingHolder.childCount * width;
                childMax = buildingHolder.childCount;
            }
        }
        if (soldHolder.childCount > 0)
        {
            maxY += width;
            
            if (soldHolder.childCount > childMax)
            {
                maxX = soldHolder.childCount * width;
                childMax = soldHolder.childCount;
            }
        }
        if (luxuryHolder.childCount > 0)
        {
            maxY += width;
            
            if (luxuryHolder.childCount > childMax)
                maxX = luxuryHolder.childCount * width;
        }

        allContents.sizeDelta = new Vector2(maxX - 80 ,maxY + 40);
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (activeStatus && Input.mouseScrollDelta.y != 0)
            CloseGrid();
    }

    private void AssignParent(ResourceCategory cat, GameObject go)
    {

        switch (cat)
        {
            case ResourceCategory.Raw:
                go.transform.SetParent(rawHolder, false);
                break;
            case ResourceCategory.Rock:
                go.transform.SetParent(rockHolder, false);
                break;
            case ResourceCategory.BuildingBlock:
                go.transform.SetParent(buildingHolder, false);
                break;
            case ResourceCategory.SoldGood:
                go.transform.SetParent(soldHolder, false);
                break;
            case ResourceCategory.LuxuryGood:
                go.transform.SetParent(luxuryHolder, false);
                break;
            case ResourceCategory.None:
                go.transform.SetParent(rawHolder, false);
                break;
        }
    }

    public void CloseGrid()
    {
        ToggleVisibility(false);
        UITooltipSystem.Hide();
    }

    public void ToggleVisibility(bool v, IResourceGridUser resourceGridUser = null /*UITradeResourceTask resourceTask = null, *//*UILaborResourcePriority laborResourcePriority = null*/)
    {
        if (activeStatus == v)
            return;

        activeStatus = v;
        
        if (v)
        {
            if (resourceGridUser != null)
            {
                this.resourceGridUser = resourceGridUser;
                transform.position = resourceGridUser.GridPosition;
            }
            //else if (resourceTask != null)
            //{
            //    this.resourceTask = resourceTask;
            //    transform.position = resourceTask.resourceDropdown.position;
            //}
            //else if (laborResourcePriority != null)
            //{
            //    this.laborResourcePriority = laborResourcePriority;
            //    transform.position = laborResourcePriority.resourceDropdown.position;
            //}

            closeButton.pivot = new Vector2((allContents.localPosition.x + allContents.sizeDelta.x * 0.5f) / closeButton.sizeDelta.x, (allContents.localPosition.y + allContents.sizeDelta.y * 0.5f) / closeButton.sizeDelta.y);
        }
        else
        {
            this.resourceGridUser = null;
            //this.resourceTask = null;
            //this.laborResourcePriority = null;
        }

        gameObject.SetActive(v);
    }

    public void ChooseResourceType(ResourceType resourceType)
    {
        Sprite icon = ResourceHolder.Instance.GetIcon(resourceType);

        if (resourceGridUser != null)
        {
            resourceGridUser.SetData(icon, resourceType);
        }
        //else if (resourceTask != null)
        //{
        //    resourceTask.chosenResourceSprite.sprite = icon;
        //    resourceTask.chosenResource = resourceType;
        //}
        //else if (laborResourcePriority != null)
        //{
        //    laborResourcePriority.chosenResourceSprite.sprite = icon;
        //    laborResourcePriority.chosenResource = resourceType;
        //}

        CloseGrid();
    }

    private void PositionCheck()
    {
        if (transform.localPosition.y + allContents.rect.height > Screen.height)
        {
            allContents.pivot = new Vector2(0, 1);
        }
        else
        {
            allContents.pivot = new Vector2(0, 0);
        }
    }    
}

public interface IResourceGridUser
{
    Vector3 GridPosition { get; }
    void SetData(Sprite icon, ResourceType resourceType);
}
