using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class UIQueueManager : MonoBehaviour
{
    [SerializeField]
    private GameObject uiQueueItemPrefab;
    //private List<UIQueueItem> queueItems = new();
    private UIQueueItem selectedQueueItem;
    private UIQueueItem firstQueueItem;
    //private List<string> queueItemNames = new();

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

    [HideInInspector]
    public List<ResourceValue> upgradeCosts;

	//for object pooling of UIQueueItems
	private Queue<UIQueueItem> uiQueueItemQueue = new();
    [HideInInspector]
    public List<UIQueueItem> uiQueueItemList = new();

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
			//cityBuilderManager.ShowQueuedGhost();
			uiQueueButton.ToggleButtonSelection(true);
            cityBuilderManager.CloseLaborMenus();
            List<QueueItem> tempQueueItems = cityBuilderManager.GetQueueItems();
            for (int i = 0; i < tempQueueItems.Count; i++)
            {
                PopulateImprovementQueueList(tempQueueItems[i], cityBuilderManager.SelectedCityLoc);
			}
            
            //foreach(UIQueueItem item in tempQueueItems)
            //{
            //    item.gameObject.SetActive(true);
            //    queueItemNames.Add(item.itemName);
            //    PlaceQueueItem(item);
            //}

            activeStatus = true;

            allContents.anchoredPosition3D = originalLoc + new Vector3(300f, 0, 0);

            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + -300, 0.4f).setEase(LeanTweenType.easeOutSine);
            //LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear();
        }
        else
        {
            HideUIQueueItem();
            activeStatus = false;
            cityBuilderManager.DestroyQueuedGhost();
            uiQueueButton.ToggleButtonSelection(false);
            ToggleButtonSelection(false);
            //HideQueueItems();
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
        firstQueueItem = uiQueueItemList[0];
        //cityBuilderManager.SetCityQueueItems();
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

    //public bool AddToQueue(Vector3Int worldLoc, Vector3Int loc, Vector3Int cityLoc, ImprovementDataSO improvementData = null, UnitBuildDataSO unitBuildData = null, List<ResourceValue> upgradeCosts = null)
    //{
    //    bool building = loc == new Vector3Int(0, 0, 0);
    //    string buildingName = "";

    //    if (improvementData != null && building)
    //        buildingName = improvementData.improvementName;

    //    bool upgrading = upgradeCosts != null;
    //    string buildName = CreateItemName(loc, upgrading, improvementData, unitBuildData);

    //    if (unitBuildData == null && queueItemNames.Contains(buildName))
    //    {
    //        UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Item already in queue");
    //        return false;
    //    }
    //    else if (loc != new Vector3Int(0, 0, 0) && world.CheckQueueLocation(worldLoc))
    //    {
    //        UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Location already queued");
    //        return false;
    //    }

    //    GameObject newQueueItem = Instantiate(uiQueueItem);
    //    world.AddLocationToQueueList(worldLoc, cityLoc);
    //    newQueueItem.SetActive(true);
    //    UIQueueItem queueItemHandler = newQueueItem.GetComponent<UIQueueItem>();
    //    if (building)
    //        queueItemHandler.buildingName = buildingName;
    //    queueItemHandler.SetQueueManager(this);
    //    //queueItemHandler.CreateQueueItem(buildName, loc, improvementData, upgradeCosts, unitBuildData);
    //    //queueItemHandler.upgrading = upgrading;
    //    queueItemNames.Add(buildName);
    //    if (improvementData != null)
    //    {
    //        cityBuilderManager.PlayQueueAudio();
            
    //        if (upgrading)
    //            cityBuilderManager.CreateQueuedArrow(improvementData, worldLoc, building);
    //        else
    //            cityBuilderManager.CreateQueuedGhost(improvementData, worldLoc, building);
    //    }
    //    PlaceQueueItem(queueItemHandler);
    //    return true;
    //}

    public void AddToQueueList(QueueItem item, Vector3Int cityLoc)
    {
		ImprovementDataSO improvementData = UpgradeableObjectHolder.Instance.improvementDict[item.queueName];
		string buildName = CreateItemName(item.queueLoc, item.upgrade, improvementData);

		UIQueueItem queueItemHandler = GetFromUIQueueItemPool();
		queueItemHandler.CreateQueueItem(item.queueName, buildName, item.queueLoc, item.upgrade);

		PlaceQueueItem(queueItemHandler);
	}

    public void PopulateImprovementQueueList(QueueItem item, Vector3Int cityLoc)
    {
        ImprovementDataSO improvementData = UpgradeableObjectHolder.Instance.improvementDict[item.queueName];
        bool building = item.queueLoc == Vector3Int.zero;
        Vector3Int worldLoc = cityLoc + item.queueLoc;

		string buildName = CreateItemName(item.queueLoc, item.upgrade, improvementData);

		//GameObject newQueueItem = Instantiate(uiQueueItem);
		//newQueueItem.SetActive(true);
        UIQueueItem queueItemHandler = GetFromUIQueueItemPool();
        queueItemHandler.transform.SetAsLastSibling(); //set all as last so that first is on top
		queueItemHandler.CreateQueueItem(item.queueName, buildName, item.queueLoc, item.upgrade);

		if (improvementData != null)
		{
			cityBuilderManager.PlayQueueAudio();

			if (item.upgrade)
				cityBuilderManager.CreateQueuedArrow(item, improvementData, worldLoc, building);
			else
				cityBuilderManager.CreateQueuedGhost(item, improvementData, worldLoc, building);
		}

		PlaceQueueItem(queueItemHandler);
	}

    private void PlaceQueueItem(UIQueueItem queueItemHandler)
    {
        queueItemHandler.transform.SetParent(queueItemHolder, false);
        //queueItems.Add(queueItemHandler);
        if (uiQueueItemList.Count == 1) //if first to list, make top of list
            SetFirstQueueItem();
    }

    public void RemoveFromQueue()
    {
        if (selectedQueueItem != null)
        {
            cityBuilderManager.PlaySelectAudio();
    
            cityBuilderManager.RemoveQueueGhostImprovement(selectedQueueItem.item);
            //if (selectedQueueItem.buildLoc.x == 0 && selectedQueueItem.buildLoc.z == 0)
            //    cityBuilderManager.RemoveQueueGhostBuilding(selectedQueueItem.buildingName, cityBuilderManager.SelectedCity);
            //else
            //{
            //    cityBuilderManager.SelectedCity.improvementQueueLocs.Remove(selectedQueueItem.buildLoc + cityBuilderManager.SelectedCityLoc);
            //}

            RemoveFromQueue(selectedQueueItem, cityBuilderManager.SelectedCityLoc);
        }
    }

    public void RemoveFromQueue(UIQueueItem queueItem, Vector3Int cityLoc)
    {
        uiQueueItemList.Remove(queueItem);
        Vector3Int worldLoc = queueItem.item.queueLoc + cityLoc;
        //queueItemNames.Remove(queueItem.itemName);
        //city.savedQueueItems.Remove(queueItem);
        //city.savedQueueItemsNames.Remove(queueItem.itemName);
        if (queueItem.item.queueLoc != Vector3Int.zero)
        {
            City city = world.GetCity(cityLoc);
            city.queueItemList.Remove(queueItem.item);
            world.RemoveLocationFromQueueList(worldLoc);
            if (world.TileHasCityImprovement(worldLoc))
                world.GetCityDevelopment(worldLoc).queued = false;
        }

        if (queueItem == firstQueueItem)
        {
            if (uiQueueItemList.Count > 0)
                SetFirstQueueItem();
            else
                cityBuilderManager.resourceManager.ClearQueueResources();
        }

		//select the next item when this one is removed
        if (queueItem == selectedQueueItem && uiQueueItemList.Count > 1)
        {
			queueItem.ToggleItemSelection(false);
            //int nextItemIndex = -1;
            int nextItemIndex = queueItem.GetNextItemIndex();
            UIQueueItem nextItem = queueItemHolder.GetChild(nextItemIndex).GetComponent<UIQueueItem>();
		    AddToUIQueueItemPool(queueItem);
            //uiQueueItemList.Remove(queueItem);
            nextItem.ToggleItemSelection(true); //must be last
            return;
		    //Destroy(selectedQueueItem.gameObject);
		    //selectedQueueItem = null;
		    //if (nextItemIndex >= 0)
            //return;
        }

		AddToUIQueueItemPool(queueItem);
		//uiQueueItemList.Remove(queueItem);
        //uiQueueItemList.Remove(selectedQueueItem);
        //AddToUIQueueItemPool(selectedQueueItem);
        //Destroy(queueItem.gameObject);
    }

    public void MoveItemUp()
    {
        cityBuilderManager.PlayMoveAudio();
        
        if (selectedQueueItem != null)
        {
            int index = selectedQueueItem.MoveItemUp();
            if (index == -1)
                return;
            cityBuilderManager.SelectedCity.queueItemList.Remove(selectedQueueItem.item);
            cityBuilderManager.SelectedCity.queueItemList.Insert(index, selectedQueueItem.item);
			uiQueueItemList.Remove(selectedQueueItem);
			uiQueueItemList.Insert(index, selectedQueueItem);
            if (index == 0)
                SetFirstQueueItem();
        }
    }

    public void MoveItemDown()
    {
		cityBuilderManager.PlayMoveAudio();

		if (selectedQueueItem != null)
        {
            int index = selectedQueueItem.MoveItemDown();
            if (index == -1)
                return;
            cityBuilderManager.SelectedCity.queueItemList.Remove(selectedQueueItem.item);
			uiQueueItemList.Remove(selectedQueueItem);
            if (index >= cityBuilderManager.SelectedCity.queueItemList.Count)
            {
                cityBuilderManager.SelectedCity.queueItemList.Add(selectedQueueItem.item);
    			uiQueueItemList.Add(selectedQueueItem);
            }
            else
            {
                cityBuilderManager.SelectedCity.queueItemList.Insert(index, selectedQueueItem.item);
				uiQueueItemList.Insert(index, selectedQueueItem);
			}
			if (index == 1)
                SetFirstQueueItem();
        }
    }

    private string CreateItemName(Vector3Int loc, bool upgrading, ImprovementDataSO improvementData = null, UnitBuildDataSO unitBuildData = null)
    {
        string buildName = "";
        if (upgrading)
            buildName = "Upgrade";
        else if (improvementData != null)
            buildName = improvementData.improvementName;
        //else if (unitBuildData != null)
        //    buildName = unitBuildData.unitName;

        if (!(loc.x == 0 && loc.z == 0))
        {
            buildName = buildName + " (" + loc.x/3 + "," + loc.z/3 + ")";
        }
        //else
        //{
        //    buildName = buildName + " " + improvementData.improvementName;
        //}

        return buildName;
    }

    //public void CheckIfBuiltUnitIsQueued(UnitBuildDataSO unitData, Vector3Int cityLoc)
    //{
    //    string builtName = CreateItemName(new Vector3Int(0, 0, 0), false, null, unitData);

    //    if (queueItemNames.Contains(builtName))
    //    {
    //        foreach (UIQueueItem item in queueItems)
    //        {
    //            if (item.itemName == builtName)
    //            {
    //                RemoveFromQueue(item, cityLoc);
    //                return;
    //            }
    //        }
    //    }
    //}

    public void CheckIfBuiltItemIsQueued(Vector3Int worldLoc, Vector3Int loc, bool upgrading, ImprovementDataSO improvementData, City city)
    {
        QueueItem item;
        item.queueName = improvementData.improvementNameAndLevel;
        item.queueLoc = loc;
        item.upgrade = upgrading;

        //string builtName = CreateItemName(loc, upgrading, improvementData);

        if (city.queueItemList.Contains(item))
        {
            int index = city.queueItemList.IndexOf(item);
            city.queueItemList.Remove(item);
            //city.savedQueueItemsNames.Remove(builtName);
            //city.improvementQueueLocs.Remove(worldLoc);
            world.RemoveLocationFromQueueList(worldLoc);
            //UIQueueItem queueItem = city.savedQueueItems[index];
            //city.savedQueueItems.Remove(queueItem);
            //Destroy(queueItem);

            if (index == 0)
                city.ResourceManager.ClearQueueResources();

            if (city.activeCity && activeStatus)
            {
                Destroy(cityBuilderManager.queueGhostDict[item]);
                
                List<UIQueueItem> tempQueueItems = new(uiQueueItemList);
                for (int i = 0; i < tempQueueItems.Count; i++)
                {
                    if (item.queueLoc == tempQueueItems[i].item.queueLoc && item.queueName == tempQueueItems[i].item.queueName)
                    {
                        RemoveFromQueue(tempQueueItems[i], cityBuilderManager.SelectedCityLoc);
                        //AddToUIQueueItemPool(tempQueueItems[i]);
                        //uiQueueItemList.Remove(tempQueueItems[i]);
                        break;
                    }
                }

                //foreach (UIQueueItem item in tempQueueItems)
                //{
                //    if (item.itemName == builtName)
                //    {
                //        RemoveFromQueue(item, city.cityLoc);
                //    }
                //}
            }

            //return true;
        }
        //else if (!(loc.x == 0 && loc.z == 0) && world.CheckQueueLocation(worldLoc))
        //{
        //    world.RemoveQueueItemCheck(worldLoc);
        //    //return false;
        //}

        //return false;
    }

    private void SetResourcesToCheck()
    {
        //QueueItem item = firstQueueItem.item;

        List<ResourceValue> resourceCosts;

        if (firstQueueItem.item.upgrade)
            resourceCosts = new(upgradeCosts);
        else
            resourceCosts = new(UpgradeableObjectHolder.Instance.improvementDict[firstQueueItem.item.queueName].improvementCost);
        //if (unitBuildData != null)
        //    resourceCosts = new(unitBuildData.unitCost);
        //else if (upgradeCosts != null)
        //    resourceCosts = upgradeCosts;
        //else if (improvementData != null)
        //    resourceCosts = new(improvementData.improvementCost);

        cityBuilderManager.resourceManager.SetQueueResources(resourceCosts);
    }

    //public (List<UIQueueItem>, List<string>) SetQueueItems()
    //{
    //    List<UIQueueItem> queueItems = new(this.queueItems);
    //    List<string> queueItemNames = new(this.queueItemNames);
        
    //    return (queueItems, queueItemNames);
    //}

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
    
    //public void HideQueueItems()
    //{
    //    foreach (UIQueueItem queueItem in uiQueueItemList)
    //    {
    //        queueItem.gameObject.SetActive(false);
    //        queueItem.transform.SetParent(null, false); //false is necessary for higher resolutions
    //    }

    //    uiQueueItemList.Clear();
    //    queueItemNames.Clear();
    //    //queueItemHolder.DetachChildren(); //this doubles the scale of children's rect transform in 4k. 
    //}



    //UIQueueItem pool
	private void GrowUIQueueItemPool()
	{
		for (int i = 0; i < 5; i++) //grow pool 5 at a time
		{
			GameObject uiQueueItem = Instantiate(uiQueueItemPrefab);
			//uiQueueItem.gameObject.transform.SetParent(queueItemHolder, false);
			UIQueueItem uiItem = uiQueueItem.GetComponent<UIQueueItem>();
			uiItem.SetQueueManager(this);
			AddToUIQueueItemPool(uiItem);
		}
	}

	private void AddToUIQueueItemPool(UIQueueItem uiItem)
	{
        uiItem.transform.SetParent(transform, false);
        uiItem.gameObject.SetActive(false); //inactivate it when adding to pool
		uiQueueItemQueue.Enqueue(uiItem);
	}

	private UIQueueItem GetFromUIQueueItemPool()
	{
		if (uiQueueItemQueue.Count == 0)
			GrowUIQueueItemPool();

		UIQueueItem uiItem = uiQueueItemQueue.Dequeue();
		uiItem.gameObject.SetActive(true);
        uiItem.transform.SetParent(queueItemHolder, false);
        uiQueueItemList.Add(uiItem);
		return uiItem;
	}

	private void HideUIQueueItem()
	{
        for (int i = 0; i < uiQueueItemList.Count; i++)
        {
            AddToUIQueueItemPool(uiQueueItemList[i]);
        }

		uiQueueItemList.Clear();
	}
}

public struct QueueItem
{
    public string queueName;
    public Vector3Int queueLoc;
    public bool upgrade;
}
