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

    [SerializeField]
    private Image background;

    private UIQueueManager uiQueueManager;

    private UnitBuildDataSO unitBuildData;
    private ImprovementDataSO improvementData;
    private Vector3Int buildLoc;

    //for unselecting
    private Color originalTextColor;
    private Color originalBackgroundColor;
    private bool isSelected;

    private void Awake()
    {
        originalTextColor = itemText.color;
        originalBackgroundColor = background.color;
        uiQueueManager = GetComponentInParent<UIQueueManager>();
    }

    public void CreateQueueItem(string text, Vector3Int loc, UnitBuildDataSO unitBuildData = null, ImprovementDataSO improvementData = null)
    {
        if (loc.x == 0 && loc.z == 0)
        {
            itemText.text = text;
        }
        else
        {
            itemText.text = (text + " (" + loc.x + "," + loc.z + ")");
        }

        buildLoc = loc;
        this.unitBuildData = unitBuildData;
        this.improvementData = improvementData;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ToggleItemSelection(!isSelected);
    }

    private void ToggleItemSelection(bool v)
    {
        if (v)
        {
            isSelected = true;
            itemText.color = Color.white;
            background.color = Color.gray;
        }
        else
        {
            isSelected = false;
            itemText.color = originalTextColor;
            background.color = originalBackgroundColor;
        }
    }
}
