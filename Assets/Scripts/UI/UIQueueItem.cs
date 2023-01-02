using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIQueueItem : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    public TMP_Text itemText;
    [HideInInspector]
    public string itemName; //string name only used to check if already on queue list

    [SerializeField]
    private Image background;

    private UIQueueManager uiQueueManager;

    [HideInInspector] 
    public UnitBuildDataSO unitBuildData;
    [HideInInspector] 
    public ImprovementDataSO improvementData;
    [HideInInspector] 
    public List<ResourceValue> upgradeCosts;
    [HideInInspector] 
    public Vector3Int buildLoc;

    //for unselecting
    private Color originalTextColor;
    private Color originalBackgroundColor;

    [HideInInspector]
    public bool isSelected, upgrading;

    private void Awake()
    {
        originalTextColor = itemText.color;
        originalBackgroundColor = background.color;
        //if (transform.parent.childCount == 0)
        //    uiQueueManager.SetFirstQueueItem(this);
    }

    public void CreateQueueItem(string text, Vector3Int loc, UnitBuildDataSO unitBuildData, ImprovementDataSO improvementData, List<ResourceValue> upgradeCosts)
    {
        itemText.text = text;
        itemName = text;
        buildLoc = loc;
        this.unitBuildData = unitBuildData;

        if (upgradeCosts == null)
            this.improvementData = improvementData;
        this.upgradeCosts = upgradeCosts;
    }

    public (ImprovementDataSO, UnitBuildDataSO, List<ResourceValue>) GetQueueItemData()
    {
        return (improvementData, unitBuildData, upgradeCosts);
    }

    public void SetQueueManager(UIQueueManager uiQueueManager)
    {
        this.uiQueueManager = uiQueueManager;
    }

    public int MoveItemUp()
    {
        int placement = transform.GetSiblingIndex();
        if (placement == 0)
        {
            return -1;
        }

        transform.SetSiblingIndex(placement - 1);
        //if (placement - 1 == 0)
        //    uiQueueManager.SetFirstQueueItem();

        return placement - 1;
    }

    public int MoveItemDown()
    {
        int placement = transform.GetSiblingIndex();
        if (placement == transform.parent.childCount - 1)
            return -1;

        transform.SetSiblingIndex(placement + 1);
        //if (placement == 0)
        //    uiQueueManager.SetFirstQueueItem();

        return placement + 1;
    }

    public int GetNextItemIndex()
    {
        int placement = transform.GetSiblingIndex();
        if (placement == transform.parent.childCount - 1)
            placement--;
        else
            placement++;

        return placement;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ToggleItemSelection(!isSelected);
    }

    public void ToggleItemSelection(bool v)
    {
        if (v)
        {
            uiQueueManager.ClearQueueItemSelect();
            uiQueueManager.SetSelectedQueueItem(this);
            isSelected = true;
            itemText.color = Color.white;
            background.color = Color.gray;
        }
        else
        {
            isSelected = false;
            //uiQueueManager.ResetSelectedQueueItem();
            itemText.color = originalTextColor;
            background.color = originalBackgroundColor;
        }
    }
}
