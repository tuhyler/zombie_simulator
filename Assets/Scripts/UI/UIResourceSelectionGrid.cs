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
    private RectTransform allContents, closeButton, resourceHolder, rawHolder, rockHolder, buildingHolder, soldHolder, luxuryHolder;

    [HideInInspector]
    public bool activeStatus;
    private UITradeResourceTask resourceTask;

    private void Awake()
    {
        float maxX = 290;
        float maxY = 70;
        int width = 50;

        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
        {
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

    public void ToggleVisibility(bool v, UITradeResourceTask resourceTask = null)
    {
        if (activeStatus == v)
            return;

        activeStatus = v;
        
        if (v)
        {
            this.resourceTask = resourceTask;
            transform.position = resourceTask.resourceDropdown.position;
            //PositionCheck();
        }
        else
        {
            this.resourceTask = null;
        }

        gameObject.SetActive(v);
    }

    public void ChooseResourceType(ResourceType resourceType)
    {
        resourceTask.chosenResourceSprite.sprite = ResourceHolder.Instance.GetIcon(resourceType);
        resourceTask.chosenResource = resourceType;
        CloseGrid();
    }

    private void PositionCheck()
    {
        if (transform.localPosition.y - allContents.rect.height < Screen.height * 0.5)
        {
            allContents.pivot = new Vector2(0, 0);
        }
        else
        {
            allContents.pivot = new Vector2(0, 1);
        }
    }
}
