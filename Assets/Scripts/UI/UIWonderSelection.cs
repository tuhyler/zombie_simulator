using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIWonderSelection : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;

    [SerializeField]
    private TMP_Text wonderTitle, wonderDescription, workerText, workerCount, workerTotal, percentDone, harborText, workerCostText;

    [SerializeField]
    private Image progressBarMask;

    [SerializeField]
    private Transform wonderCostHolder;

    [SerializeField]
    private GameObject uiWonderResourcePanel, removeWorkerButton, goldImage;
    private Dictionary<ResourceType, UIWonderResource> resourceOptions = new();

    [SerializeField]
    private GameObject addHarborButton, cancelConstructionButton;

    [HideInInspector]
    public bool buttonsAreWorking;



    [SerializeField] //for tweening
    private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus;
    private Vector3 originalLoc;

    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;
        percentDone.outlineWidth = 0.5f;
        percentDone.outlineColor = new Color(0, 0, 0, 255);
        buttonsAreWorking = true;

        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
        {
            GameObject resourceOptionGO = Instantiate(uiWonderResourcePanel, wonderCostHolder);
            UIWonderResource resourceOption = resourceOptionGO.GetComponent<UIWonderResource>();
            resourceOption.resourceImage.sprite = resource.resourceIcon;
            resourceOption.resourceType = resource.resourceType;
            resourceOption.gameObject.SetActive(false);

            resourceOptions[resource.resourceType] = resourceOption;
        }

        gameObject.SetActive(false);
        addHarborButton.SetActive(false);
    }

    public void ToggleVisibility(bool v, Wonder wonder)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            world.UnselectAll();
            world.somethingSelected = true;

            SetWonderInfo(wonder);
            SetResources(wonder);
            if (!wonder.isConstructing)
            {
                cancelConstructionButton.SetActive(false);
                HideWorkerCounts();
            }
            else
            {
                cancelConstructionButton.SetActive(true);
                ShowWorkerCounts();
            }

            if (wonder.canBuildHarbor && wonder.isConstructing)
            {
                addHarborButton.SetActive(true);
                if (wonder.hasHarbor)
                    harborText.text = "Remove Harbor";
                else
                    harborText.text = "Add Harbor";
            }

            gameObject.SetActive(v);

            activeStatus = true;

            allContents.anchoredPosition3D = originalLoc + new Vector3(-600f, 0, 0);

            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + 600f, 0.5f).setEaseOutSine();
            //LeanTween.alpha(allContents, 1f, 0.5f).setFrom(0f).setEaseLinear();
        }
        else
        {
            activeStatus = false;
            SetResourcesInactive(wonder);

            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + -600f, 0.3f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        addHarborButton.SetActive(false);
        gameObject.SetActive(false);
    }

    private void SetWonderInfo(Wonder wonder)
    {
        wonderTitle.text = wonder.WonderData.wonderName;
        wonderDescription.text = wonder.WonderData.wonderDecription;
        workerCount.text = $"{wonder.WorkersReceived}";
        workerTotal.text = $"/{wonder.WonderData.workersNeeded}";
        percentDone.text = $"{wonder.PercentDone}%";
        workerCostText.text = $"Cost per Worker: {wonder.WonderData.workerCost}";
        int shift = (workerCostText.text.Length - 18) * 8;
        Vector3 pos = goldImage.transform.localPosition;
        pos.x = 410 + shift;
        goldImage.transform.localPosition = pos;

        progressBarMask.fillAmount = wonder.PercentDone / 100f;

        if (wonder.WorkersReceived < wonder.WonderData.workersNeeded)
            workerCount.color = Color.red;
        else
            workerCount.color = Color.white;
    }

    private void SetResources(Wonder wonder)
    {
        foreach (ResourceValue value in wonder.WonderData.wonderCost)
        {
            int totalAmount = wonder.ResourceCostDict[value.resourceType];
            int amount = wonder.ResourceDict[value.resourceType];

            resourceOptions[value.resourceType].ToggleActive(true);
            resourceOptions[value.resourceType].SetResourceAmount(amount, totalAmount);
        }
    }

    private void SetResourcesInactive(Wonder wonder)
    {
        foreach (ResourceValue value in wonder.WonderData.wonderCost)
        {
            resourceOptions[value.resourceType].ToggleActive(false);
        }
    }

    public void UpdateUI(ResourceType type, int amount, int totalAmount)
    {
        resourceOptions[type].SetResourceAmount(amount, totalAmount);
    }

    public void UpdateUIPercent(int newPercentDone)
    {
        percentDone.text = $"{newPercentDone}%";
        progressBarMask.fillAmount = newPercentDone / 100f;
    }

    public void HideHarborButton()
    {
        addHarborButton.SetActive(false);
    }

    public void UpdateHarborButton(bool v)
    {
        if (v)
            harborText.text = "Remove Harbor";
        else
            harborText.text = "Add Harbor";
    }

    public void HideCancelConstructionButton()
    {
        cancelConstructionButton.SetActive(false);
    }

    public void HideWorkerCounts()
    {
        workerText.gameObject.SetActive(false);
        workerCount.gameObject.SetActive(false);
        workerTotal.gameObject.SetActive(false);
        workerCostText.gameObject.SetActive(false);
        goldImage.SetActive(false);
        removeWorkerButton.SetActive(false);
    }

    public void ShowWorkerCounts()
    {
        workerText.gameObject.SetActive(true);
        workerCount.gameObject.SetActive(true);
        workerTotal.gameObject.SetActive(true);
        workerCostText.gameObject.SetActive(true);
        goldImage.SetActive(true);
        removeWorkerButton.SetActive(true);
    }

    internal void UpdateUIWorkers(int workersReceived)
    {
        workerCount.text = $"{workersReceived}";
        workerCount.color = Color.red;
    }

    internal void ToggleEnable(bool v)
    {
        buttonsAreWorking = v;
    }
}
