using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIQueueManager : MonoBehaviour
{
    [SerializeField]
    private GameObject uiQueueItem;

    [SerializeField]
    private Transform queueItemHolder;

    [SerializeField]
    private UIQueueButton uiQueueButton;

    [SerializeField]
    private CityBuilderManager cityBuilderManager;

    [SerializeField] //for tweening
    private RectTransform allContents;
    private bool activeStatus;
    private Vector3 originalLoc;

    //[HideInInspector]
    //public bool isQueueing;

    [SerializeField] //changing color of add queue button when selected
    private Image addQueueImage;
    private Color originalButtonColor;


    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;
        gameObject.SetActive(false);
        originalButtonColor = addQueueImage.color;
    }

    public void ToggleVisibility(bool v)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(v);
            uiQueueButton.ToggleButtonSelection(true);
            cityBuilderManager.CloseLaborMenus();

            activeStatus = true;

            allContents.anchoredPosition3D = originalLoc + new Vector3(300f, 0, 0);

            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + -300, 0.4f).setEase(LeanTweenType.easeOutSine);
            LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear();
        }
        else
        {
            activeStatus = false;
            uiQueueButton.ToggleButtonSelection(false);
            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + 300f, 0.2f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void AddToQueue(Vector3Int loc, ImprovementDataSO improvementData = null, UnitBuildDataSO unitBuildData = null)
    {
        GameObject newQueueItem = Instantiate(uiQueueItem);
        newQueueItem.SetActive(true);
        newQueueItem.transform.SetParent(queueItemHolder, false);

        UIQueueItem queueItemHandler = newQueueItem.GetComponent<UIQueueItem>();
        string buildName = "";
        if (improvementData != null)
            buildName = improvementData.improvementName;
        if (unitBuildData != null)
            buildName = unitBuildData.unitName;
        queueItemHandler.CreateQueueItem(buildName, loc, unitBuildData, improvementData);
    }

    public void ToggleButtonSelection(bool v)
    {
        if (v)
        {
            addQueueImage.color = Color.green;
        }
        else
        {
            addQueueImage.color = originalButtonColor;
        }
    }
}
