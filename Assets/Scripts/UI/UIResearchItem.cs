using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIResearchItem : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    private TMP_Text researchNameText, researchPercentDone, queueNumber;

    [SerializeField]
    private Image progressBarMask, researchItemPanel, topBar, queueNumberHolderImage, queueNumberCheck, researchIcon;

    [SerializeField]
    private Transform uiElementsParent, progressBarHolder, queueNumberHolder;

    [SerializeField]
    private Sprite selectedResearchSprite, selectedTopBar, selectedQueueSprite;
    private Sprite originalResearchSprite, originalTopBar, originalQueueSprite;

    //[SerializeField]
    //private CanvasGroup canvasGroup;

    [HideInInspector]
    public UIResearchTreePanel researchTree;

    //private string researchName;
    private string researchName;
    public string ResearchName { get { return researchName; } }

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
    private Color lockedColor = new Color(.7f, .7f, .7f);
    private Color completedColor = new Color(0f, 1f, 1f);

    private bool isSelected;

    [HideInInspector]
    public bool tempUnlocked;

    private void Awake()
    {
        researchName = researchNameText.text;
        progressBarMask.fillAmount = 0;
        progressBarHolder.gameObject.SetActive(false);
        queueNumberHolder.gameObject.SetActive(false);
        queueNumberCheck.gameObject.SetActive(false);

        researchPercentDone.color = new Color(0.1098039f, 0.282353f, 0.5490196f);
        researchPercentDone.outlineColor = new Color(0f, 0f, 0f);
        //researchPercentDone.color = new Color(0.2509804f, 0.4666667f, 0.7960784f);
        researchPercentDone.text = totalResearchNeeded.ToString();

        originalColor = researchItemPanel.color;
        originalResearchSprite = researchItemPanel.sprite;
        originalTopBar = topBar.sprite;
        originalQueueSprite = queueNumberHolderImage.sprite;
        researchItemPanel.color = lockedColor;
        topBar.color = lockedColor;

        //if (completed)
        //    canvasGroup.interactable = false;

        foreach (Image arrow in arrows)
            arrow.color = selectedColor;

        foreach (Transform transform in uiElementsParent)
        {
            UIResearchReward reward = transform.GetComponent<UIResearchReward>();
            reward.rewardBackground.color = lockedColor;
            researchRewardList.Add(reward);
        }
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
            topBar.sprite = originalTopBar;
            foreach (UIResearchReward reward in researchRewardList)
                reward.Unselect();
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
            topBar.sprite = selectedTopBar;
            foreach (UIResearchReward reward in researchRewardList)
                reward.Select();
            queueNumberHolderImage.sprite = selectedQueueSprite;
            //queueNumberHolderImage.color = selectedColor;
            queueNumberHolder.gameObject.SetActive(true);
            //researchPercentDone.outlineWidth = 0.35f;
            //researchPercentDone.color = new Color(255, 255, 255);
            researchIcon.gameObject.SetActive(false);
            queueNumber.text = 1.ToString();

            foreach (Image arrow in arrows)
                arrow.color = originalColor;
        }
    }

    public void ResetAlpha()
    {
        //canvasGroup.alpha = 1f;
        researchItemPanel.color = originalColor;
        topBar.color = originalColor;

        foreach (UIResearchReward reward in researchRewardList)
            reward.rewardBackground.color = originalColor;
    }

    public void ResearchComplete(MapWorld world)
    {
        ChangeColor();
        HideProgressBar();
        researchIcon.gameObject.SetActive(false);
        researchPercentDone.gameObject.SetActive(false);
        completed = true;
        locked = true;
        tempUnlocked = true;
        researchItemPanel.color = completedColor;
        topBar.color = completedColor;
        foreach (UIResearchReward reward in researchRewardList)
            reward.Complete(completedColor);
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
                data.Locked = false;
                world.SetUpgradeableObjectMaxLevel(data.improvementName, data.improvementLevel);
                if (data.improvementLevel > 1)
                {
                    string nameAndLevel = data.improvementName + "-" + (data.improvementLevel - 1);
                    world.GetImprovementData(nameAndLevel).Locked = true;
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

        world.BuilderHandlerCheck();
        GameLoader.Instance.gameData.completedResearch.Add(ResearchName);
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
        if (researchTree.researchTooltip.activeStatus)
        {
            researchTree.researchTooltip.ToggleVisibility(false);
            return;
        }

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

        researchTree.world.cityBuilderManager.PlaySelectAudio();
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
        //researchPercentDone.outlineWidth = 0f;
        researchPercentDone.color = new Color(0.1098039f, 0.282353f, 0.5490196f);
        researchPercentDone.text = totalResearchNeeded.ToString();
        //researchPercentDone.outlineColor = new Color(0.2f, 0.2f, 0.2f);
        researchPercentDone.outlineWidth = 0f;
        researchIcon.gameObject.SetActive(true);
    }

    public void UpdateProgressBar()
    {
        progressBarHolder.gameObject.SetActive(true);
        float researchPerc = (float)researchReceived / totalResearchNeeded;
        //researchPercentDone.text = $"{Mathf.Round(100 * researchPerc)}%";
        researchPercentDone.color = Color.white; 
        researchPercentDone.text = $"{researchReceived}/{totalResearchNeeded}";
        //researchPercentDone.outlineWidth = 0.4f; //this makes the text appear outside of the scroll rect for some reason

        LeanTween.value(progressBarMask.gameObject, progressBarMask.fillAmount, researchPerc, 0.2f)
            .setEase(LeanTweenType.easeOutSine)
            .setOnUpdate((value) =>
            {
                progressBarMask.fillAmount = value;
            });
    }
}
