using System.Collections;
using System.Collections.Generic;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class UIResearchTreePanel : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;

    [SerializeField]
    private Transform uiElementsParent;

    //[SerializeField]
    //private RectTransform globalVolume;

    [SerializeField]
    private Image queueButton; 

    [SerializeField]
    private UnitMovement unitMovement;

    [SerializeField]
    private CityBuilderManager cityBuilderManager;

    [SerializeField]
    private Volume globalVolume;
    private DepthOfField dof;

    private UIResearchItem chosenResearchItem;
    private List<UIResearchItem> researchItemList = new();
    private Queue<UIResearchItem> researchItemQueue = new();
    private int extraResearch;
    [HideInInspector]
    public bool isQueueing;
    private Color originalColor;

    [SerializeField] //for tweening
    private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus;
    private Vector3 originalLoc;

    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;
        originalColor = queueButton.color;
        gameObject.SetActive(false);

        if (globalVolume.profile.TryGet<DepthOfField>(out DepthOfField tmpDof))
        {
            dof = tmpDof;
        }

        dof.focalLength.value = 15;

        foreach (Transform transform in uiElementsParent)
        {
            if (transform.TryGetComponent(out UIResearchItem researchItem))
            {
                researchItem.SetResearchTree(this);
                researchItemList.Add(researchItem);
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
            cityBuilderManager.ResetCityUI();
            unitMovement.ClearSelection();
            
            gameObject.SetActive(v);

            foreach (UIResearchItem researchItem in researchItemList)
            {
                if (researchItem.ResearchReceived > 0 || researchItem == chosenResearchItem)
                    researchItem.UpdateProgressBar();
                if (!researchItem.locked && researchItem != chosenResearchItem)
                    researchItem.ResetAlpha();
            }

            activeStatus = true;

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
                world.SetResearchName("No Current Research");
                world.SetWorldResearchUI(0, 1);
            }

            isQueueing = false;
            queueButton.color = originalColor;
            activeStatus = false;

            LeanTween.value(globalVolume.gameObject, dof.focalLength.value, 15, 0.3f)
            .setEase(LeanTweenType.easeOutSine)
            .setOnUpdate((value) =>
            {
                dof.focalLength.value = value;
            });
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 1200f, 0.3f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void CloseResearchTree()
    {
        ToggleVisibility(false);
    }

    public void StartQueue()
    {
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

    public void SetResearchItem(UIResearchItem researchItem)
    {
        //undoing from previously selected research item
        if (chosenResearchItem != null && !chosenResearchItem.completed)
        {
            chosenResearchItem.EndQueue();
            chosenResearchItem.ChangeColor();
            if (chosenResearchItem.ResearchReceived == 0)
                chosenResearchItem.HideProgressBar();

            if (chosenResearchItem == researchItem)
            {
                world.researching = false;
                world.SetResearchName("No Current Research");
                world.SetWorldResearchUI(0, 1);
                chosenResearchItem = null;
                return;
            }
        }

        researchItem.ChangeColor();
        world.SetResearchName(researchItem.ResearchName);
        chosenResearchItem = researchItem;
        chosenResearchItem.UpdateProgressBar();
        if (extraResearch > 0)
            AddResearch(extraResearch);

        world.researching = true;
        if (world.CitiesResearchWaitingCheck())
            world.RestartResearch();

        world.SetWorldResearchUI(chosenResearchItem.ResearchReceived, chosenResearchItem.totalResearchNeeded);
    }

    public int AddResearch(int amount)
    {
        int diff = chosenResearchItem.totalResearchNeeded - chosenResearchItem.ResearchReceived;
        extraResearch = 0;

        if (amount > diff)
        {
            extraResearch = amount - diff;
            amount = diff;
        }
            
        chosenResearchItem.ResearchReceived += amount;

        if (activeStatus)
            chosenResearchItem.UpdateProgressBar();

        return amount;
    }

    public void CompletedResearchCheck()
    {
        if (chosenResearchItem.ResearchReceived == chosenResearchItem.totalResearchNeeded)
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
        return chosenResearchItem.ResearchName;
    }
}
