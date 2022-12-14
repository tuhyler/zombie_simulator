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
    private Image progressBarMask, researchItemPanel, queueNumberHolderImage, queueNumberCheck;

    [SerializeField]
    private Transform uiElementsParent, progressBarHolder, queueNumberHolder;

    [SerializeField]
    private Sprite selectedResearchSprite, selectedQueueSprite;
    private Sprite originalResearchSprite, originalQueueSprite;

    //[SerializeField]
    //private CanvasGroup canvasGroup;

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
    private Color selectedColor = new Color(1f, .8f, .65f);
    private Color lockedColor = new Color(.8f, .8f, .8f);
    private Color completedColor = new Color(0f, 1f, 0f);


    private Color arrowOriginalColor;
    private bool isSelected;

    [HideInInspector]
    public bool tempUnlocked;

    private void Awake()
    {
        progressBarMask.fillAmount = 0;
        progressBarHolder.gameObject.SetActive(false);
        queueNumberHolder.gameObject.SetActive(false);
        queueNumberCheck.gameObject.SetActive(false);

        researchPercentDone.outlineWidth = 0.35f;
        researchPercentDone.outlineColor = new Color(0, 0, 0, 255);

        originalColor = researchItemPanel.color;
        originalResearchSprite = researchItemPanel.sprite;
        originalQueueSprite = queueNumberHolderImage.sprite;
        researchItemPanel.color = lockedColor;

        //if (completed)
        //    canvasGroup.interactable = false;

        arrowOriginalColor = arrows[0].color;
        foreach (Image arrow in arrows)
            arrow.color = selectedColor;

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
            //researchItemPanel.color = originalColor;
            researchItemPanel.sprite = originalResearchSprite;
            queueNumberHolderImage.sprite = originalQueueSprite;
            //queueNumberHolderImage.color = originalColor;
            queueNumberHolder.gameObject.SetActive(false);

            foreach (Image arrow in arrows)
                arrow.color = selectedColor;
        }
        else
        {
            isSelected = true;
            //researchItemPanel.color = selectedColor;
            researchItemPanel.sprite = selectedResearchSprite;
            queueNumberHolderImage.sprite = selectedQueueSprite;
            //queueNumberHolderImage.color = selectedColor;
            queueNumberHolder.gameObject.SetActive(true);
            queueNumber.text = 1.ToString();

            foreach (Image arrow in arrows)
                arrow.color = originalColor;
        }
    }

    public void ResetAlpha()
    {
        //canvasGroup.alpha = 1f;
        researchItemPanel.color = originalColor;
    }

    public void ResearchComplete(MapWorld world)
    {
        ChangeColor();
        HideProgressBar();
        completed = true;
        locked = true;
        tempUnlocked = true;
        researchItemPanel.color = completedColor;
        queueNumber.gameObject.SetActive(false);
        queueNumberHolder.gameObject.SetActive(true);
        queueNumberCheck.gameObject.SetActive(true);
        queueNumberHolderImage.color = completedColor;

        foreach (Image arrow in arrows)
        {
            arrow.color = completedColor;
        }
        //canvasGroup.alpha = 0.5f;

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

        ResetAlpha();
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
        foreach (UIResearchItem researchItem in researchUnlocked)
            researchItem.tempUnlocked = false;

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
