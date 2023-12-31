using System.Collections.Generic;
using TMPro;
using UnityEditorInternal.Profiling.Memory.Experimental;
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
    private Sprite selectedResearchSprite, selectedTopBar, selectedQueueSprite, completedResearchSprite, completedTopBar, completedCircle, completedCheck, completedRewardBackground;
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
    //private Color completedColor = new Color(0.3431471f, 0.3625781f, 0.764151f);

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
        researchPercentDone.outlineColor = Color.black;
        //researchPercentDone.outlineWidth = .4f;
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
        //tempUnlocked = true;
        //foreach (UIResearchItem researchItem in researchUnlocked)
        //    researchItem.TempUnlockCheck();

        researchTree.world.TutorialCheck("Research");
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
        researchTree.world.cityBuilderManager.PlayChimeAudio();
        ChangeColor();
        HideProgressBar();
        researchIcon.gameObject.SetActive(false);
        researchPercentDone.gameObject.SetActive(false);
        completed = true;
        locked = true;
        researchItemPanel.sprite = completedResearchSprite;
        topBar.sprite = completedTopBar;
        researchNameText.color = Color.white;

        for (int i = 0; i < researchRewardList.Count; i++)
            researchRewardList[i].Complete(completedRewardBackground);

        queueNumber.gameObject.SetActive(false);
        queueNumberHolder.gameObject.SetActive(true);
        queueNumberCheck.gameObject.SetActive(true);
        queueNumberHolderImage.sprite = completedCircle;
		queueNumberCheck.sprite = completedCheck;

		//foreach (Image arrow in arrows)
  //          arrow.color = completedColor;
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

            for (int i = 0; i < researchReward.resourcesUnlocked.Count; i++)
            {
                world.UpdateResourceSelectionGrids(researchReward.resourcesUnlocked[i]);
                world.cityBuilderManager.uiMarketPlaceManager.UpdateMarketPlaceManager(researchReward.resourcesUnlocked[i]);

                foreach (City city in world.cityDict.Values)
                    city.ResourceManager.UpdateDicts(researchReward.resourcesUnlocked[i]);
            }
        }

        //unlocking research items down further in tree
        foreach (UIResearchItem researchItem in researchUnlocked)
            researchItem.UnlockCheck();

        world.BuilderHandlerCheck();
        world.SetResearchBackground(true);
        GameLoader.Instance.gameData.completedResearch.Add(ResearchName);
    }

	public void LoadResearchComplete(MapWorld world)
	{
        ResetAlpha();
        HideProgressBar();
		researchIcon.gameObject.SetActive(false);
		researchPercentDone.gameObject.SetActive(false);
        completed = true;
		locked = true;
		researchItemPanel.sprite = completedResearchSprite;
		topBar.sprite = completedTopBar;
		researchNameText.color = Color.white;

		for (int i = 0; i < researchRewardList.Count; i++)
			researchRewardList[i].Complete(completedRewardBackground);

		queueNumber.gameObject.SetActive(false);
		queueNumberHolder.gameObject.SetActive(true);
		queueNumberCheck.gameObject.SetActive(true);
		queueNumberHolderImage.sprite = completedCircle;
        queueNumberCheck.sprite = completedCheck;

   //     foreach (Image arrow in arrows)
			//arrow.color = completedColor;
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
            if (!isSelected && !researchTree.QueueContainsCheck(this))
            {
				researchTree.world.cityBuilderManager.PlaySelectAudio();

				if (!researchTree.IsResearching() && !locked)
                {
                    SelectResearchItem();
                    return;
                }

                bool canBeQueued = true;

                for (int i = 0; i < researchDependent.Count; i++)
                {
                    if (!researchDependent[i].isSelected && !researchDependent[i].completed &&!researchTree.QueueContainsCheck(researchDependent[i]))
                    {
                        canBeQueued = false;
                        break;
                    }
                }

                if (canBeQueued)
                    AddToQueue();
			}
        }
        else if (locked)
        {
			researchTree.world.cityBuilderManager.PlaySelectAudio();
            QueueToItem();
		}
        else
        {
			researchTree.world.cityBuilderManager.PlaySelectAudio();

			if (researchTree.QueueCount() > 0)
                researchTree.EndQueue();
            SelectResearchItem();
        }
    }

    private void QueueToItem()
    {
        researchTree.UnselectResearchItem();

        if (researchTree.QueueCount() > 0)
            researchTree.EndQueue();

        List<UIResearchItem> itemsCheckList = new() { this };
        List<UIResearchItem> branchList = new();
        List<UIResearchItem> pathList = new();
        bool firstSelection = true;

		while (itemsCheckList.Count > 0)
        {
            UIResearchItem nextItem = itemsCheckList[0];
            itemsCheckList.Remove(nextItem);
            bool firstItem = true;
            if (!pathList.Contains(nextItem))
                pathList.Insert(0, nextItem); //always in front of line

            bool branchUp = false;
            int queuedCount = 0;
            int dependentCount = nextItem.researchDependent.Count;
			for (int i = 0; i < dependentCount; i++)
            {            
                if (nextItem.researchDependent[i].isSelected || researchTree.QueueContainsCheck(nextItem.researchDependent[i]))
                {
                    queuedCount++;
                    continue;
                }
                else if (nextItem.researchDependent[i].locked)
                {
                    if (firstItem)
                    {
                        firstItem = false;
                        itemsCheckList.Add(nextItem.researchDependent[i]);
                    }
                    else
                    {
                        branchList.Add(nextItem);
                    }
                }
                else
                {
					if (!nextItem.researchDependent[i].isSelected)
                    {
					    if (firstSelection)
                        {
                            nextItem.researchDependent[i].SelectResearchItem();
                            firstSelection = false;
                        }
                        else
                        {
                            nextItem.researchDependent[i].AddToQueue();
                        }
    
                    }

                    firstItem = false;
                    branchUp = true;
                }
            }

            //go up path if all dependents are already queued
            if (queuedCount == dependentCount)
                branchUp = true;
            
            if (branchUp)
            {
				List<UIResearchItem> tempList = new(pathList);
				for (int j = 0; j < tempList.Count; j++)
				{
					if (!branchList.Contains(tempList[j]))
					{
						tempList[j].AddToQueue();
						pathList.Remove(tempList[j]);
					}
					else
					{
						break;
					}
				}

				if (branchList.Count > 0)
				{
					UIResearchItem nextBranch = branchList[branchList.Count - 1];
					branchList.Remove(nextBranch);
					itemsCheckList.Add(nextBranch);
				}
			}
        }
    }

	public void AddToQueue()
    {
		researchTree.AddToQueue(this);
		queueNumberHolder.gameObject.SetActive(true);
		queueNumber.text = (researchTree.QueueCount() + 1).ToString();
	}

    public void LoadQueue()
    {
		researchTree.AddToQueue(this);
		queueNumberHolder.gameObject.SetActive(true);
		queueNumber.text = (researchTree.QueueCount() + 1).ToString();
	}

    public void EndQueue()
    {
        queueNumberHolder.gameObject.SetActive(false);
    }

    public void HideProgressBar()
    {
        progressBarHolder.gameObject.SetActive(false);
        //researchPercentDone.outlineWidth = 0f;
        researchPercentDone.color = new Color(0.1098039f, 0.282353f, 0.5490196f);
		researchPercentDone.outlineWidth = 0f;
		researchPercentDone.text = totalResearchNeeded.ToString();
        //researchPercentDone.outlineColor = new Color(0.2f, 0.2f, 0.2f);
        //researchPercentDone.outlineWidth = 0f;
        researchPercentDone.gameObject.SetActive(false);
        researchIcon.gameObject.SetActive(true);
		researchPercentDone.gameObject.SetActive(true); //work around to ensure changed outline width is updated. Outline width is very buggy
	}

    public void UpdateProgressBar()
    {
        progressBarHolder.gameObject.SetActive(true);
        float researchPerc = (float)researchReceived / totalResearchNeeded;
        //researchPercentDone.text = $"{Mathf.Round(100 * researchPerc)}%";
        researchPercentDone.color = Color.white;
		researchPercentDone.outlineWidth = .4f;
        researchPercentDone.text = $"{researchReceived}/{totalResearchNeeded}";
        researchPercentDone.gameObject.SetActive(false);
		researchPercentDone.gameObject.SetActive(true);
		//researchPercentDone.outlineWidth = 0.4f; //this makes the text appear outside of the scroll rect for some reason

		LeanTween.value(progressBarMask.gameObject, progressBarMask.fillAmount, researchPerc, 0.2f)
            .setEase(LeanTweenType.easeOutSine)
            .setOnUpdate((value) =>
            {
                progressBarMask.fillAmount = value;
            });
    }
}
