using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIResearchItem : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    private TMP_Text researchName, researchPercentDone, queueNumber;

    [SerializeField]
    private Image progressBarMask, researchItemPanel, queueNumberHolderImage;

    [SerializeField]
    private Transform uiElementsParent, progressBarHolder, queueNumberHolder;

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

    [HideInInspector]
    public bool completed;

    private List<UIResearchReward> researchRewardList = new();
    private Color originalColor;
    private Color selectedColor = new Color(0, 1f, 1f);
    private Color arrowOriginalColor;
    private bool isSelected;

    [HideInInspector]
    public bool tempUnlocked;

    private void Awake()
    {
        progressBarMask.fillAmount = 0;
        progressBarHolder.gameObject.SetActive(false);
        queueNumberHolder.gameObject.SetActive(false);

        researchPercentDone.outlineWidth = 0.35f;
        researchPercentDone.outlineColor = new Color(0, 0, 0, 255);

        if (completed)
            canvasGroup.interactable = false;

        originalColor = researchItemPanel.color;
        arrowOriginalColor = arrows[0].color;

        foreach (Transform transform in uiElementsParent)
            researchRewardList.Add(transform.GetComponent<UIResearchReward>());
    }

    public void SetResearchTree(UIResearchTreePanel researchTree)
    {
        this.researchTree = researchTree;
    }

    public void SetQueueNumber(int number)
    {
        queueNumber.text = number.ToString();
    }

    public void SelectResearchItem()
    {
        tempUnlocked = true;
        foreach (UIResearchItem researchItem in researchUnlocked)
            researchItem.TempUnlockCheck();

        researchTree.SetResearchItem(this);
    }

    public void ChangeColor()
    {
        if (isSelected)
        {
            isSelected = false;
            researchItemPanel.color = originalColor;
            queueNumberHolderImage.color = originalColor;
            queueNumberHolder.gameObject.SetActive(false);

            foreach (Image arrow in arrows)
                arrow.color = arrowOriginalColor;
        }
        else
        {
            isSelected = true;
            researchItemPanel.color = selectedColor;
            queueNumberHolderImage.color = selectedColor;
            queueNumberHolder.gameObject.SetActive(true);
            queueNumber.text = 1.ToString();

            foreach (Image arrow in arrows)
                arrow.color = selectedColor;
        }
    }

    public void ResearchComplete(MapWorld world)
    {
        ChangeColor();
        HideProgressBar();
        completed = true;
        locked = true;
        tempUnlocked = true;
        canvasGroup.alpha = 0.5f;
        
        //unlocking all research rewards
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
                    world.GetImprovementData(nameAndLevel).locked = true;
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
                    world.GetUnitBuildData(nameAndLevel).locked = true;
                }
            }
        }

        //unlocking research items down further in tree
        foreach (UIResearchItem researchItem in researchUnlocked)
            researchItem.UnlockCheck();
    }

    private void UnlockCheck()
    {
        foreach (UIResearchItem researchItem in researchDependent)
        {
            if (!researchItem.completed)
                return;
        }

        locked = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (researchTree.isQueueing)
        {
            if (!researchTree.QueueContainsCheck(this) && (!locked || tempUnlocked))
            {
                if (!researchTree.IsResearching())
                {
                    SelectResearchItem();
                    return;
                }
                
                tempUnlocked = true;
                researchTree.AddToQueue(this);
                queueNumberHolder.gameObject.SetActive(true);
                queueNumber.text = (researchTree.QueueCount() + 1).ToString();

                foreach (UIResearchItem researchItem in researchUnlocked)
                    researchItem.TempUnlockCheck();
            }
        }
        else if (!locked)
        {
            if (!researchTree.isQueueing && researchTree.QueueCount() > 0)
                researchTree.EndQueue();
            SelectResearchItem();
        }
    }

    public void TempUnlockCheck()
    {
        foreach (UIResearchItem researchItem in researchDependent)
        {
            if (!researchItem.tempUnlocked)
                return;
        }

        tempUnlocked = true;
    }

    public void EndQueue()
    {
        tempUnlocked = false;
        queueNumberHolder.gameObject.SetActive(false);
    }

    public void HideProgressBar()
    {
        progressBarHolder.gameObject.SetActive(false);
    }

    public void UpdateProgressBar()
    {
        progressBarHolder.gameObject.SetActive(true);
        float researchPerc = (float)researchReceived / totalResearchNeeded;
        researchPercentDone.text = $"{Mathf.Round(100 * researchPerc)}%";
        
        LeanTween.value(progressBarMask.gameObject, progressBarMask.fillAmount, researchPerc, 0.2f)
            .setEase(LeanTweenType.easeOutSine)
            .setOnUpdate((value) =>
            {
                progressBarMask.fillAmount = value;
            });
    }
}
