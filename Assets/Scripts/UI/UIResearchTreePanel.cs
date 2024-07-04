using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using UnityEngine.EventSystems;
using System.Linq;

public class UIResearchTreePanel : MonoBehaviour, IPointerDownHandler, IImmoveable
{
    [SerializeField]
    public MapWorld world;

    [SerializeField]
    public UIResearchTooltip researchTooltip;

    [SerializeField]
    private CameraController cameraController;

    [SerializeField]
    private Transform uiElementsParent;

    [SerializeField]
    public Scrollbar horizontalScroll;

    //[SerializeField]
    //private RectTransform globalVolume;

    [SerializeField]
    private Image queueButton;

    //for switching between tabs
    [SerializeField]
    public TMP_Text titleText; 
    [SerializeField]
    private Transform researchTreeContents;
    private List<GameObject> researchTreeList = new();
    [SerializeField]
    private Transform tabContents;
    [HideInInspector]
    public List<UIResearchTab> tabList = new();
    [HideInInspector]
    public int selectedTab;
    //[SerializeField]
    //private UnitMovement unitMovement;

    //[SerializeField]
    //private CityBuilderManager cityBuilderManager;

    //for blurring background
    [SerializeField]
    private Volume globalVolume;
    private DepthOfField dof;

    private UIResearchItem chosenResearchItem;
    private List<UIResearchItem> researchItemList = new();
    private Queue<UIResearchItem> researchItemQueue = new();
    [HideInInspector]
    public HashSet<UIResearchItem> partialResearchItemList = new();
    private int extraResearch;
    [HideInInspector]
    public bool isQueueing;
    private Color originalColor;

    [SerializeField] //for tweening
    private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus, isFlashing;
    private Vector3 originalLoc;
    public float[] tabThresholds;

    private void Awake()
    {
        //titleText.outlineColor = new Color(0.25f, 0.18f, 0f);
        //titleText.outlineWidth = 0.3f;

        originalLoc = allContents.anchoredPosition3D;
        originalColor = queueButton.color;
        gameObject.SetActive(false);

        if (globalVolume.profile.TryGet<DepthOfField>(out DepthOfField tmpDof))
        {
            dof = tmpDof;
        }

        dof.focalLength.value = 15;

        PrepResearchItemList();

        foreach (Transform tree in researchTreeContents)
        {
            researchTreeList.Add(tree.gameObject);
        }

        int i = 0;
        foreach (Transform tab in tabContents)
        {
            UIResearchTab researchTab = tab.GetComponent<UIResearchTab>();
            researchTab.tabLoc = i;
            researchTab.SetResearchTree(this);
            tabList.Add(researchTab);
            i++;
        }
    }

    public void PrepResearchItemList()
    {
        if (researchItemList.Count == 0)
        {
            foreach (Transform transform in uiElementsParent)
            {
                if (transform.TryGetComponent(out UIResearchItem researchItem))
                {
                    researchItem.SetResearchRewardInfo();
                    researchItem.SetResearchTree(this);
                    researchItemList.Add(researchItem);
                }
            }
        }
    }

	public void HandleShiftDown()
    {
        if (activeStatus)
            isQueueing = true;
    }

    public void HandleShiftUp()
    {
        if (activeStatus)
            isQueueing = false;
    }

    public void ToggleVisibility(bool v)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            if (isFlashing)
                world.ButtonFlashCheck();
            world.UnselectAll();
            world.ToggleMinimap(false);
			//world.openingImmoveable = true;
			world.immoveableCanvas.gameObject.SetActive(true);
            world.iImmoveable = this;
			world.BattleCamCheck(true);
			gameObject.SetActive(v);
            world.tooltip = false;
            world.somethingSelected = true;

            foreach (UIResearchItem researchItem in researchItemList)
            {
                if (researchItem.researchReceived > 0 || researchItem == chosenResearchItem)
                    researchItem.UpdateProgressBar();
                if (!researchItem.locked && researchItem != chosenResearchItem)
                    researchItem.ResetAlpha();
            }

            activeStatus = true;
            //SetTab();

            allContents.anchoredPosition3D = originalLoc + new Vector3(0, 1200f, 0);

            LeanTween.value(globalVolume.gameObject, dof.focalLength.value, 45, 0.5f)
            .setEase(LeanTweenType.easeOutSine)
            .setOnUpdate((value) =>
            {
                dof.focalLength.value = value;
            });
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -1200f, 0.5f).setEaseOutSine();
            //LeanTween.alpha(allContents, 1f, 0.5f).setFrom(0f).setEaseLinear();
        }
        else
        {
            if (chosenResearchItem == null)
            {
                world.SetResearchName("No Research");
                world.SetWorldResearchUI(0, 1);
                world.SetResearchBackground(false);
            }

            if (researchTooltip.activeStatus)
                researchTooltip.ToggleVisibility(false);

			world.ToggleMinimap(true);
            isQueueing = false;
            queueButton.color = originalColor;
            activeStatus = false;
			world.BattleCamCheck(false);
            world.iImmoveable = null;

			LeanTween.value(globalVolume.gameObject, dof.focalLength.value, 15, 0.3f)
            .setEase(LeanTweenType.easeOutSine)
            .setOnUpdate((value) =>
            {
                dof.focalLength.value = value;
            });
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 1200f, 0.3f).setOnComplete(SetActiveStatusFalse);
        }

        cameraController.enabled = !v;
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
		if (world.iImmoveable == null)
			world.immoveableCanvas.gameObject.SetActive(false);
		//world.ImmoveableCheck();
		//     if (!world.openingImmoveable)
		//         world.immoveableCanvas.gameObject.SetActive(false);
		//     else
		//world.openingImmoveable = false;
	}

 //   private void OpeningComplete()
 //   {
	//	world.openingImmoveable = false;
	//}

    public void CloseResearchTree()
    {
        world.cityBuilderManager.PlayCloseAudio();
        ToggleVisibility(false);
		world.somethingSelected = false;
	}

	public void StartQueue()
    {
        world.cityBuilderManager.PlaySelectAudio();
        
        if (isQueueing)
        {
            isQueueing = false;
            queueButton.color = originalColor;
        }
        else
        {
            isQueueing = true;
            queueButton.color = Color.green;
        }
    }

    public void AddToQueue(UIResearchItem researchItem)
    {
        researchItemQueue.Enqueue(researchItem);
    }

    public int QueueCount()
    {
        return researchItemQueue.Count;
    }

    public bool QueueContainsCheck(UIResearchItem researchItem)
    {
        return researchItemQueue.Contains(researchItem);
    }

    private void MoveDownInQueue()
    {
        for (int i = 0; i < researchItemQueue.Count; i++)
        {
            UIResearchItem researchItem = researchItemQueue.Dequeue();
            researchItem.SetQueueNumber(i + 2);
            researchItemQueue.Enqueue(researchItem);
        }
    }

    public void EndQueue()
    {
        isQueueing = false;
        queueButton.color = originalColor;

        int count = researchItemQueue.Count;
        for (int i = 0; i < count; i++)
            researchItemQueue.Dequeue().EndQueue();
    }

    public void SetCurrentEra(Era currentEra)
    {
        for (int i = 0; i < tabList.Count; i++)
        {
            if (tabList[i].era == currentEra)
            {
                tabList[i].SelectTab();
                break;
            }
        }
    }

    public void SetTab()
    {
        for (int i = 0; i < tabList.Count; i++)
        {
            if (i != selectedTab)
                tabList[i].Unselect();
        }
    }

    public void SetScrollInfo(float value)
    {
        if (tabThresholds.Contains(value))
            return;

        for (int i = tabList.Count - 1; i >= 0; i--)
        {
			if (value >= tabThresholds[i] - 0.01f) //necessary to subtract a small amount as it doesn't always exactly equal
            {
				tabList[i].SelectTab();
                break;
            }
		}
	}

    public void SetResearchItem(UIResearchItem researchItem)
    {
        //undoing from previously selected research item
        if (chosenResearchItem != null && !chosenResearchItem.completed)
        {
            chosenResearchItem.EndQueue();
            chosenResearchItem.ChangeColor();
            if (chosenResearchItem.researchReceived == 0)
                chosenResearchItem.HideProgressBar();

            if (chosenResearchItem == researchItem)
            {
                world.researching = false;
                world.SetResearchName("No Research");
                world.SetWorldResearchUI(0, 1);
                world.SetResearchBackground(false);
				chosenResearchItem = null;
                return;
            }
        }

        researchItem.ChangeColor();
        world.SetResearchName(researchItem.researchName);
		world.SetResearchBackground(false);
		chosenResearchItem = researchItem;
        chosenResearchItem.UpdateProgressBar();
        if (extraResearch > 0)
            AddResearch(extraResearch);

        world.researching = true;
        if (world.CitiesResearchWaitingCheck())
            world.RestartResearch();

        partialResearchItemList.Add(researchItem);
        world.SetWorldResearchUI(chosenResearchItem.researchReceived, chosenResearchItem.totalResearchNeeded);
    }

    public void UnselectResearchItem()
    {
		if (chosenResearchItem != null)
        {
            chosenResearchItem.EndQueue();
		    chosenResearchItem.ChangeColor();
		    if (chosenResearchItem.researchReceived == 0)
			    chosenResearchItem.HideProgressBar();

            chosenResearchItem = null;

		}
	}

    public int AddResearch(int amount)
    {
        int diff = chosenResearchItem.totalResearchNeeded - chosenResearchItem.researchReceived;
        extraResearch = 0;

        if (amount > diff)
        {
            extraResearch = amount - diff;
            amount = diff;
        }
            
        chosenResearchItem.researchReceived += amount;

        if (activeStatus)
            chosenResearchItem.UpdateProgressBar();

        return amount;
    }

    public void CompletedResearchCheck()
    {
        if (chosenResearchItem.researchReceived == chosenResearchItem.totalResearchNeeded)
        {
            chosenResearchItem.ResearchComplete(world);
            researchItemList.Remove(chosenResearchItem);
        }
    }

    public void CompletionNextStep()
    {
        if (chosenResearchItem.completed)
        {
            if (researchItemQueue.Count == 0)
            {
                world.researching = false;
                //world.SetResearchName("No Current Research");
                chosenResearchItem = null;
            }
            else
            {
                SetResearchItem(researchItemQueue.Dequeue());
                MoveDownInQueue();
            }
        }
    }

    public bool IsResearching()
    {
        return chosenResearchItem != null;
    }

    public string GetChosenResearchName()
    {
        return chosenResearchItem.researchName;
    }

    public void CloseTip()
    {
        if (researchTooltip.activeStatus)
            researchTooltip.ToggleVisibility(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (researchTooltip.activeStatus)
            researchTooltip.ToggleVisibility(false);
    }

	public List<string> SaveResearch()
	{
		List<string> currentResearch = new();
    
		if (chosenResearchItem != null)
		{
            //GameLoader.Instance.gameData.researchAmount = chosenResearchItem.researchReceived;
			currentResearch.Add(chosenResearchItem.researchName);

			List<UIResearchItem> queuedResearch = new(researchItemQueue.ToList());
			for (int i = 0; i < queuedResearch.Count; i++)
			{
				currentResearch.Add(queuedResearch[i].researchName);
			}
		}

		return currentResearch;
	}

    public Dictionary<string, int> SavePartialResearch()
    {
        Dictionary<string, int> partialResearchDict = new();

        foreach (UIResearchItem item in partialResearchItemList)
            partialResearchDict[item.researchName] = item.researchReceived;

        return partialResearchDict;
    }

	public void LoadCompletedResearch(List<string> completedResearch)
	{
        List<UIResearchItem> tempResearchItemList = new(researchItemList);
		for (int i = 0; i < tempResearchItemList.Count; i++)
		{
			if (completedResearch.Contains(tempResearchItemList[i].researchName))
				tempResearchItemList[i].LoadResearchComplete(world);
		}
	}

	public void LoadCurrentResearch(List<string> currentResearch)
	{
		for (int i = 0; i < currentResearch.Count; i++)
		{
			for (int j = 0; j < researchItemList.Count; j++)
			{
				if (researchItemList[j].researchName == currentResearch[i])
				{
					if (i == 0)
					{
                        SetResearchItem(researchItemList[j]);
						researchItemList[j].ResetAlpha();
						chosenResearchItem = researchItemList[j];
						world.SetResearchName(chosenResearchItem.researchName);
						//chosenResearchItem.researchReceived = researchAmount;
						world.SetWorldResearchUI(chosenResearchItem.researchReceived, chosenResearchItem.totalResearchNeeded);
                        world.researching = true;
                        break;
					}
                    else
                    {
                        researchItemList[j].LoadQueue();
                        break;
                    }
				}
			}
		}
	}

    public void LoadResearchLevels(Dictionary<string, int> partialResearchDict)
    {
        for (int i = 0; i < researchItemList.Count; i++)
        {
            if (partialResearchDict.ContainsKey(researchItemList[i].researchName))
            {
                researchItemList[i].researchReceived = partialResearchDict[researchItemList[i].researchName];
                partialResearchItemList.Add(researchItemList[i]);
            }
        }
    }
}
