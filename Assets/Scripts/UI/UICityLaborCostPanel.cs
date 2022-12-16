using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using UnityEngine;
using UnityEngine.UI;

public class UICityLaborCostPanel : MonoBehaviour
{
    [SerializeField]
    private Transform laborCostScrollRect;

    [SerializeField]
    private Image openCostsImage;
    [SerializeField]
    private Sprite buttonRight;
    private Sprite buttonLeft;
    private Color originalColor;

    [SerializeField]
    private GameObject uiResourceInfoPanel;
    private List<UIResourceInfoPanel> resourceOptions = new();

    [SerializeField] //for tweening
    private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus;
    private Vector3 originalLoc;

    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;
        gameObject.SetActive(false);
        buttonLeft = openCostsImage.sprite;
        originalColor = openCostsImage.color;

        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources.Concat(ResourceHolder.Instance.allWorldResources))
        {
            if (resource.resourceType == ResourceType.Research || resource.resourceType == ResourceType.Food)
                continue;

            GameObject resourceOptionGO = Instantiate(uiResourceInfoPanel, laborCostScrollRect);

            if (resource.resourceType == ResourceType.Gold)
                resourceOptionGO.transform.SetSiblingIndex(0);

            UIResourceInfoPanel resourceOption = resourceOptionGO.GetComponent<UIResourceInfoPanel>();
            RectTransform goSizing = resourceOption.GetComponent<RectTransform>();
            goSizing.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 80);
            goSizing.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 80);
            RectTransform imageSizing = resourceOption.image.GetComponent<RectTransform>();
            imageSizing.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 70);
            imageSizing.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 70);
            RectTransform resourceImageSizing = resourceOption.resourceImage.GetComponent<RectTransform>();
            resourceImageSizing.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 55);
            resourceImageSizing.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 55);

            resourceOption.resourceImage.sprite = resource.resourceIcon;
            resourceOption.resourceType = resource.resourceType;
            resourceOption.resourceAmount.color = Color.red;
            resourceOption.gameObject.SetActive(false);

            resourceOptions.Add(resourceOption);
        }
    }

    public void ToggleVisibility(bool v, bool suddenly)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(true);
            openCostsImage.sprite = buttonRight;
            openCostsImage.color = Color.green;

            activeStatus = true;

            allContents.anchoredPosition3D = originalLoc + new Vector3(150f, 0, 0);

            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x - 150f, 0.3f).setEaseOutSine();
            LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear();
        }
        else
        {
            openCostsImage.sprite = buttonLeft;
            openCostsImage.color = originalColor;

            activeStatus = false;
            if (suddenly) //when closing the entire labor menu
                LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + 600f, 0.2f).setOnComplete(SetActiveStatusFalse);
            else
                LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + 150f, 0.2f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void ResetUI()
    {
        if (activeStatus)
        {
            foreach (UIResourceInfoPanel resource in resourceOptions)
            {
                resource.gameObject.SetActive(false);
            }
        }
    }

    public void SetConsumedResourcesInfo(Dictionary<ResourceType, float> consumedResourcesDict)
    {
        foreach (UIResourceInfoPanel resourceOption in resourceOptions)
        {
            if (consumedResourcesDict.ContainsKey(resourceOption.resourceType) && consumedResourcesDict[resourceOption.resourceType] > 0)
            {
                resourceOption.gameObject.SetActive(true);
                resourceOption.resourceAmount.text = $"-{consumedResourcesDict[resourceOption.resourceType]}";
            }
        }
    }

    public void UpdateConsumedResources(List<ResourceType> consumedResourceTypes, Dictionary<ResourceType, float> consumedResourcesDict)
    {
        foreach (UIResourceInfoPanel resourceOption in resourceOptions)
        {
            if (consumedResourceTypes.Contains(resourceOption.resourceType) && consumedResourcesDict[resourceOption.resourceType] > 0)
            {
                resourceOption.gameObject.SetActive(true);
                resourceOption.resourceAmount.text = $"-{consumedResourcesDict[resourceOption.resourceType]}";
            }
        }
    }
}
