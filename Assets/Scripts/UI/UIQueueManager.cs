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

    [SerializeField]
    private MapWorld world;


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
        cityBuilderManager.SetCityQueueItems();
        //firstQueueItem = item;
        SetResourcesToCheck();
    }

    public void ClearQueueItemSelect()
    {
        if (selectedQueueItem != null)
        {
            selectedQueueItem.ToggleItemSelection(false);
            selectedQueueItem = null;
        }
    }

    public void AddToQueue(Vector3Int worldLoc, Vector3Int loc, ImprovementDataSO improvementData = null, UnitBuildDataSO unitBuildData = null, List<ResourceValue> upgradeCosts = null)
    {
        bool upgrading = upgradeCosts != null;
        string buildName = CreateItemName(loc, upgrading, improvementData, unitBuildData);

        if (unitBuildData == null && queueItemNames.Contains(buildName))
        {
            GiveWarningMessage("Item already in queue");
            return;
        }
        else if (loc != new Vector3Int(0, 0, 0) && world.CheckQueueLocation(worldLoc))
        {
            GiveWarningMessage("Location already queued");
            return;
        }

        GameObject newQueueItem = Instantiate(uiQueueItem);
        world.AddLocationToQueueList(worldLoc);
        newQueueItem.SetActive(true);
        UIQueueItem queueItemHandler = newQueueItem.GetComponent<UIQueueItem>();
        queueItemHandler.SetQueueManager(this);
        queueItemHandler.CreateQueueItem(buildName, loc, unitBuildData, improvementData, upgradeCosts);
        queueItemHandler.upgrading = upgrading;
        queueItemNames.Add(buildName);
        PlaceQueueItem(queueItemHandler);
    }

    private void PlaceQueueItem(UIQueueItem queueItemHandler)
    {
        queueItemHandler.transform.SetParent(queueItemHolder, false);
        queueItems.Add(queueItemHandler);
        if (queueItems.Count == 1) //if first to list, make top of list
            SetFirstQueueItem();
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
        world.RemoveLocationFromQueueList(queueItem.buildLoc);

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

    private string CreateItemName(Vector3Int loc, bool upgrading, ImprovementDataSO improvementData = null, UnitBuildDataSO unitBuildData = null)
    {
        string buildName = "";
        if (improvementData != null)
            buildName = improvementData.improvementName;
        if (unitBuildData != null)
            buildName = unitBuildData.unitName;

        if (!(loc.x == 0 && loc.z == 0))
        {
            buildName = buildName + " (" + loc.x/3 + "," + loc.z/3 + ")";
        }

        if (upgrading)
            buildName = "Upgrade " + buildName;

        return buildName;
    }

    public void CheckIfBuiltUnitIsQueued(UnitBuildDataSO unitData)
    {
        string builtName = CreateItemName(new Vector3Int(0, 0, 0), false, null, unitData);

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

    public void CheckIfBuiltItemIsQueued(Vector3Int loc, bool upgrading, ImprovementDataSO improvementData, City city)
    {
        string builtName = CreateItemName(loc, upgrading, improvementData);

        if (city.savedQueueItemsNames.Contains(builtName))
        {
            int index = city.savedQueueItemsNames.IndexOf(builtName);
            city.savedQueueItemsNames.Remove(builtName);
            world.RemoveLocationFromQueueList(loc);
            UIQueueItem queueItem = city.savedQueueItems[index];
            city.savedQueueItems.Remove(queueItem);
            Destroy(queueItem);

            if (city.activeCity)
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
    }

    private void SetResourcesToCheck()
    {
        (ImprovementDataSO improvementData, UnitBuildDataSO unitBuildData, List<ResourceValue> upgradeCosts) = firstQueueItem.GetQueueItemData();

        List<ResourceValue> resourceCosts = new();

        if (unitBuildData != null)
            resourceCosts = new(unitBuildData.unitCost);
        else if (upgradeCosts != null)
            resourceCosts = upgradeCosts;
        else if (improvementData != null)
            resourceCosts = new(improvementData.improvementCost);

        cityBuilderManager.resourceManager.SetQueueResources(resourceCosts, cityBuilderManager);
    }

    public (List<UIQueueItem>, List<string>) SetQueueItems()
    {
        List<UIQueueItem> queueItems = new(this.queueItems);
        List<string> queueItemNames = new(this.queueItemNames);
        
        return (queueItems, queueItemNames);
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
    
    private void GiveWarningMessage(string message)
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; //z must be more than 0, else just gives camera position
        Vector3 mouseLoc = Camera.main.ScreenToWorldPoint(mousePos);

        InfoPopUpHandler.Create(mouseLoc, message);
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
            queueItem.transform.SetParent(null, false); //false is necessary for higher resolutions
        }

        queueItems.Clear();
        queueItemNames.Clear();
        //queueItemHolder.DetachChildren(); //this doubles the scale of children's rect transform in 4k. 
    }
}
