using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIResearchItem : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    private TMP_Text researchName;
    
    [SerializeField]
    private Image researchItemPanel;
    
    [SerializeField]
    private Transform uiElementsParent;

    [SerializeField]
    private CanvasGroup canvasGroup;

    private UIResearchTreePanel researchTree;

    //private string researchName;
    public string ResearchName { get { return researchName.text; } }

    private int researchReceived;
    public int ResearchReceived { get { return researchReceived; } set { researchReceived = value; } }

    //ResearchItem preferences
    public int totalResearchNeeded = 100;
    public bool locked = true;
    public List<UIResearchItem> researchUnlocked = new();
    public List<UIResearchItem> researchDependent = new();
    public List<Image> arrows = new();

    private bool completed;

    private List<UIResearchReward> researchRewardList = new();
    private Color originalColor;
    //private Color arrowOriginalColor;
    private bool isSelected;

    private void Awake()
    {
        if (completed)
            canvasGroup.interactable = false;

        originalColor = researchItemPanel.color;
        //arrowOriginalColor = arrows[0].color;

        foreach (Transform transform in uiElementsParent)
            researchRewardList.Add(transform.GetComponent<UIResearchReward>());
    }

    public void SetResearchTree(UIResearchTreePanel researchTree)
    {
        this.researchTree = researchTree;
    }

    public void SelectResearchItem()
    {
        researchTree.SetResearchItem(this);
    }

    public void ChangeColor()
    {
        if (isSelected)
        {
            isSelected = false;
            researchItemPanel.color = originalColor;

            //foreach (Image arrow in arrows)
            //    arrow.color = arrowOriginalColor;
        }
        else
        {
            isSelected = true;
            researchItemPanel.color = Color.green;

            //foreach (Image arrow in arrows)
            //    arrow.color = Color.green;
        }
    }

    public void ResearchComplete(MapWorld world)
    {
        completed = true;
        canvasGroup.interactable = false;
        
        foreach (UIResearchReward researchReward in researchRewardList)
        {
            if (researchReward.improvementData != null)
            {
                ImprovementDataSO data = researchReward.improvementData;
                data.locked = false;
                world.SetUpgradeableObjectMaxLevel(data.improvementName, data.improvementLevel);
                if (data.improvementLevel > 1)
                {
                    string nameAndLevel = data.improvementName + "-" + (data.improvementLevel - 1);
                    world.GetUpgradeData(nameAndLevel).locked = true;
                }
            }
            else if (researchReward.unitData != null)
            {
                UnitBuildDataSO data = researchReward.unitData;
                data.locked = false;
                world.SetUpgradeableObjectMaxLevel(data.unitName, data.unitLevel);
                if (data.unitLevel > 1)
                {
                    string nameAndLevel = data.unitName + "-" + (data.unitLevel - 1);
                    world.GetUpgradeData(nameAndLevel).locked = true;
                }
            }
        }

        foreach (UIResearchItem researchItem in researchUnlocked)
            researchItem.UnlockCheck();
    }

    private void UnlockCheck()
    {
        foreach (UIResearchItem researchItem in researchDependent)
        {
            if (researchItem.locked)
                return;
        }

        locked = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!locked)
        {
            SelectResearchItem();
        }
    }
}
