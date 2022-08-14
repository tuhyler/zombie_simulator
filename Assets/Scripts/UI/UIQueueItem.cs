using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIQueueItem : MonoBehaviour
{
    [SerializeField]
    private TMP_Text itemText;

    [SerializeField]
    private Image background;

    private UnitBuildDataSO unitBuildData;
    private ImprovementDataSO improvementData;

    private void CreateQueueItem(string text, UnitBuildDataSO unitBuildData = null, ImprovementDataSO improvementData = null)
    {
        itemText.text = text;
        this.unitBuildData = unitBuildData;
        this.improvementData = improvementData;
    }

    private void SelectItem()
    {
        background.color = Color.gray;
        itemText.color = Color.white;
    }
}
