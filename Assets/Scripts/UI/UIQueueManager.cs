using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIQueueManager : MonoBehaviour
{
    [SerializeField]
    private GameObject uiQueueItem;
    private List<UIQueueItem> queueItems = new();
    private UIQueueItem selectedQueueItem;
    private UIQueueItem firstQueueItem;
    private List<string> queueItemNames = new();

    [SerializeField]
    private Transform queueItemHolder;

    [SerializeField]
    private UIQueueButton uiQueueButton;

    [SerializeField]
    private CityBuilderManager cityBuilderManager;

    [SerializeField] //for tweening
    private RectTransform allContents;
    private bool activeStatus;
    private Vector3 originalLoc;

    //[HideInInspector]
    //public bool isQueueing;

    [SerializeField] //changing color of add queue button when selected
    private Image addQueueImage;
    private Color originalButtonColor;


    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;
        gameObject.SetActive(false);
        originalButtonColor = addQueueImage.color;
    }

    public void ToggleVisibility(bool v)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(v);
            uiQueueButton.ToggleButtonSelection(true);
            cityBuilderManager.CloseLaborMenus();

            activeStatus = true;

            allContents.anchoredPosition3D = originalLoc + new Vector3(300f, 0, 0);

            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + -300, 0.4f).setEase(LeanTweenType.easeOutSine);
            LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear();
        }
        else
        {
            activeStatus = false;
            uiQueueButton.ToggleButtonSelection(false);
            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + 300f, 0.2f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void SetSelectedQueueItem(UIQueueItem item)
    {
        selectedQueueItem = item;
    }

    public void SetFirstQueueItem(UIQueueItem item)
    {
        firstQueueItem = item;
        SetResourcesToCheck();
    }

    public void GetQueueItem(int index)
    {
        SetFirstQueueItem(queueItemHolder.GetChild(index).GetComponent<UIQueueItem>());
    }

    public void ClearQueueItemSelect()
    {
        if (selectedQueueItem != null)
        {
            selectedQueueItem.ToggleItemSelection(false);
            selectedQueueItem = null;
        }
    }

    public void AddToQueue(Vector3Int loc, ImprovementDataSO improvementData = null, UnitBuildDataSO unitBuildData = null)
    {
        string buildName = CreateItemName(loc, improvementData, unitBuildData);

        if (unitBuildData == null && queueItemNames.Contains(buildName))
        {
            Debug.Log("Item already in queue");
            return;
        }

        GameObject newQueueItem = Instantiate(uiQueueItem);
        newQueueItem.SetActive(true);
        newQueueItem.transform.SetParent(queueItemHolder, false);

        UIQueueItem queueItemHandler = newQueueItem.GetComponent<UIQueueItem>();

        queueItemHandler.CreateQueueItem(buildName, loc, unitBuildData, improvementData);
        queueItemHandler.SetQueueManager(this);
        queueItems.Add(queueItemHandler);
        queueItemNames.Add(buildName);
        if (queueItems.Count == 1) //if first to list, make top of list
            SetFirstQueueItem(queueItemHandler);
    }

    public void RemoveFirstFromQueue()
    {
        RemoveFromQueue(firstQueueItem);
    }

    public void RemoveFromQueue()
    {
        if (selectedQueueItem != null)
        {
            RemoveFromQueue(selectedQueueItem);
        }
    }

    private void RemoveFromQueue(UIQueueItem queueItem)
    {
        queueItems.Remove(queueItem);
        queueItemNames.Remove(queueItem.itemName);

        if (queueItem == firstQueueItem)
        {
            GetQueueItem(1);
        }

        if (queueItem == selectedQueueItem)
        {
            selectedQueueItem.ToggleItemSelection(false);
            int nextItemIndex = -1;
            if (queueItems.Count > 0) //select the next item when this one is removed
                nextItemIndex = selectedQueueItem.GetNextItemIndex();
            Destroy(selectedQueueItem.gameObject);
            selectedQueueItem = null;
            if (nextItemIndex >= 0)
                queueItemHolder.GetChild(nextItemIndex).GetComponent<UIQueueItem>().ToggleItemSelection(true);
            return;
        }

        Destroy(queueItem.gameObject);
    }

    public void MoveItemUp()
    {
        if (selectedQueueItem != null)
            selectedQueueItem.MoveItemUp();
    }

    public void MoveItemDown()
    {
        if (selectedQueueItem != null)
            selectedQueueItem.MoveItemDown();
    }

    private string CreateItemName(Vector3Int loc, ImprovementDataSO improvementData = null, UnitBuildDataSO unitBuildData = null)
    {
        string buildName = "";
        if (improvementData != null)
            buildName = improvementData.improvementName;
        if (unitBuildData != null)
            buildName = unitBuildData.unitName;

        if (!(loc.x == 0 && loc.z == 0))
        {
            buildName = (buildName + " (" + loc.x + "," + loc.z + ")");
        }

        return buildName;
    }

    public void CheckIfBuiltItemIsQueued(Vector3Int loc, ImprovementDataSO improvementData)
    {
        string builtName = CreateItemName(loc, improvementData);

        if (queueItemNames.Contains(builtName))
        {
            foreach (UIQueueItem item in queueItems)
            {
                if (item.itemName == builtName)
                {
                    RemoveFromQueue(item);
                    return;
                }
            }
        }
    }

    private void SetResourcesToCheck()
    {
        Vector3Int loc = new Vector3Int(0, 0, 0);
        ImprovementDataSO improvementData = null;
        UnitBuildDataSO unitBuildData = null;

        (loc, improvementData, unitBuildData) = firstQueueItem.GetQueueItemData();

        List<ResourceValue> resourceCosts = new();

        if (unitBuildData != null)
            resourceCosts = new(unitBuildData.unitCost);
        if (improvementData != null)
            resourceCosts = new(improvementData.improvementCost);

        cityBuilderManager.SetQueueResources(resourceCosts);
    }

    public (Vector3Int, ImprovementDataSO, UnitBuildDataSO) SetBuildInfo()
    {
        return firstQueueItem.GetQueueItemData();
    }

    public void ToggleButtonSelection(bool v)
    {
        if (v)
        {
            addQueueImage.color = Color.green;
        }
        else
        {
            addQueueImage.color = originalButtonColor;
        }
    }
}
