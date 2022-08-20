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
    [HideInInspector]
    public bool activeStatus;
    private Vector3 originalLoc;

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
            List<UIQueueItem> tempQueueItems = cityBuilderManager.GetQueueItems();
            foreach(UIQueueItem item in tempQueueItems)
            {
                item.gameObject.SetActive(true);
                queueItemNames.Add(item.itemName);
                PlaceQueueItem(item);
            }

            activeStatus = true;

            allContents.anchoredPosition3D = originalLoc + new Vector3(300f, 0, 0);

            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + -300, 0.4f).setEase(LeanTweenType.easeOutSine);
            LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear();
        }
        else
        {
            activeStatus = false;
            uiQueueButton.ToggleButtonSelection(false);
            ToggleButtonSelection(false);
            HideQueueItems();
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

    public void SetFirstQueueItem()
    {
        firstQueueItem = queueItems[0];
        //firstQueueItem = item;
        SetResourcesToCheck();
    }

    //public void GetQueueItem(int index)
    //{
    //    SetFirstQueueItem(queueItemHolder.GetChild(index).GetComponent<UIQueueItem>());
    //}

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
        UIQueueItem queueItemHandler = newQueueItem.GetComponent<UIQueueItem>();
        queueItemHandler.SetQueueManager(this);
        queueItemHandler.CreateQueueItem(buildName, loc, unitBuildData, improvementData);
        queueItemNames.Add(buildName);
        PlaceQueueItem(queueItemHandler);

        //newQueueItem.transform.SetParent(queueItemHolder, false);
        //UIQueueItem queueItemHandler = GetFromQueueItemPool();asd;

        //queueItemHandler.transform.SetParent(queueItemHolder, false);
        //queueItems.Add(queueItemHandler);
        //queueItemNames.Add(buildName);
        //if (queueItems.Count == 1) //if first to list, make top of list
        //    SetFirstQueueItem(queueItemHandler);
    }

    private void PlaceQueueItem(UIQueueItem queueItemHandler)
    {
        queueItemHandler.transform.SetParent(queueItemHolder, false);
        queueItems.Add(queueItemHandler);
        if (queueItems.Count == 1) //if first to list, make top of list
            SetFirstQueueItem();
    }

    //public void RemoveFirstFromQueue()
    //{
    //    RemoveFromQueue(firstQueueItem);
    //}

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

        if (queueItem == firstQueueItem && queueItems.Count > 0)
        {
            SetFirstQueueItem();
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
        {
            int index = selectedQueueItem.MoveItemUp();
            if (index == -1)
                return;
            queueItems.Remove(selectedQueueItem);
            queueItems.Insert(index, selectedQueueItem);
            if (index == 0)
                SetFirstQueueItem();
        }
    }

    public void MoveItemDown()
    {
        if (selectedQueueItem != null)
        {
            int index = selectedQueueItem.MoveItemDown();
            if (index == -1)
                return;
            queueItems.Remove(selectedQueueItem);
            queueItems.Insert(index, selectedQueueItem);
            if (index == 1)
                SetFirstQueueItem();
        }
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
        (Vector3Int loc, ImprovementDataSO improvementData, UnitBuildDataSO unitBuildData) = firstQueueItem.GetQueueItemData();

        List<ResourceValue> resourceCosts = new();

        if (unitBuildData != null)
            resourceCosts = new(unitBuildData.unitCost);
        if (improvementData != null)
            resourceCosts = new(improvementData.improvementCost);

        cityBuilderManager.resourceManager.SetQueueResources(resourceCosts, cityBuilderManager);
    }

    //public (Vector3Int, ImprovementDataSO, UnitBuildDataSO) SetBuildInfo()
    //{
    //    return firstQueueItem.GetQueueItemData();
    //}

    public List<UIQueueItem> SetQueueItems()
    {
        List<UIQueueItem> queueItems = new(this.queueItems);
        
        return queueItems;
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

    public void UnselectQueueItem()
    {
        if (selectedQueueItem != null)
        {
            selectedQueueItem.ToggleItemSelection(false);
            selectedQueueItem = null;
        }   
    }





    //private void GrowQueueItemPool()
    //{
    //    for (int i = 0; i < 5; i++) //grow pool 5 at a time
    //    {
    //        GameObject gameObject = Instantiate(uiQueueItem);
    //        UIQueueItem queueItem = gameObject.GetComponent<UIQueueItem>();
    //        AddToQueueItemPool(queueItem);
    //    }
    //}

    //private void AddToQueueItemPool(UIQueueItem queueItem)
    //{
    //    queueItem.gameObject.SetActive(false); //inactivate it when adding to pool
    //    queueItemQueue.Enqueue(queueItem);
    //}

    //private UIQueueItem GetFromQueueItemPool()
    //{
    //    if (queueItemQueue.Count == 0)
    //        GrowQueueItemPool();

    //    UIQueueItem queueItem = queueItemQueue.Dequeue();
    //    queueItem.gameObject.SetActive(true);
    //    return queueItem;
    //}

    public void HideQueueItems()
    {
        foreach (UIQueueItem queueItem in queueItems)
        {
            queueItem.gameObject.SetActive(false);
        }

        queueItems.Clear();
        queueItemNames.Clear();
        queueItemHolder.DetachChildren();
    }
}
