using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIQueueItem : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    private TMP_Text itemText;
    [HideInInspector]
    public string itemName; //string name only used to check if already on queue list

    [SerializeField]
    private Image background;

    private UIQueueManager uiQueueManager;

    public UnitBuildDataSO unitBuildData;
    public ImprovementDataSO improvementData;
    public Vector3Int buildLoc;

    //for unselecting
    private Color originalTextColor;
    private Color originalBackgroundColor;

    [HideInInspector]
    public bool isSelected;

    private void Awake()
    {
        originalTextColor = itemText.color;
        originalBackgroundColor = background.color;
        //if (transform.parent.childCount == 0)
        //    uiQueueManager.SetFirstQueueItem(this);
    }

    public void CreateQueueItem(string text, Vector3Int loc, UnitBuildDataSO unitBuildData = null, ImprovementDataSO improvementData = null)
    {
        itemText.text = text;
        itemName = text;
        buildLoc = loc;
        this.unitBuildData = unitBuildData;
        this.improvementData = improvementData;
    }

    public (Vector3Int, ImprovementDataSO, UnitBuildDataSO) GetQueueItemData()
    {
        return (buildLoc, improvementData, unitBuildData);
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
            uiQueueManager.SetFirstQueueItem();
            return placement;
        }

        transform.SetSiblingIndex(placement - 1);

        return placement - 1;
    }

    public int MoveItemDown()
    {
        int placement = transform.GetSiblingIndex();
        if (placement == transform.parent.childCount - 1)
            return placement;

        transform.SetSiblingIndex(placement + 1);
        if (placement == 0)
            uiQueueManager.SetFirstQueueItem();

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
