using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class UITooltip : MonoBehaviour
{
    public TMP_Text title, level, producesTitle, health, speed, strength;
    private TMP_Text producesText;
    public Image strengthImage;
    public Sprite inventorySprite, strengthSprite;
    private int screenHeightNegHalf = -750;
    public RectTransform rectTransform, resourceProduceAllHolder, imageLine, resourceProducedHolder, resourceCostHolder, unitInfo;
    public VerticalLayoutGroup resourceProduceLayout;
    private List<Transform> produceConsumesHolders = new();
    private List<UIResourceInfoPanel> costsInfo = new(), producesInfo = new();
    private List<List<UIResourceInfoPanel>> consumesInfo = new();
    private List<TMP_Text> noneTextList = new();

    private void Awake()
    {
        gameObject.SetActive(false);

        foreach (Transform selection in resourceProducedHolder)
        {
            if (selection.TryGetComponent(out TMP_Text text))
            {
                producesText = text;
            }
            else
            {
                produceConsumesHolders.Add(selection);
            }
        }

        for (int i = 0; i < produceConsumesHolders.Count; i++)
        {
            int j = 0;
            List<UIResourceInfoPanel> consumesPanels = new();
            
            foreach (Transform selection in produceConsumesHolders[i])
            {
                
                //first one is for showing produces
                if (j == 0)
                    producesInfo.Add(selection.GetComponent<UIResourceInfoPanel>());
                //after first two is for showing consumes
                else if (j >= 2 && j <= 6)
                    consumesPanels.Add(selection.GetComponent<UIResourceInfoPanel>());
                else if (j == 7)
                    noneTextList.Add(selection.GetComponent<TMP_Text>());

                j++;
            }

            consumesInfo.Add(consumesPanels);
        }
        foreach (Transform selection in resourceCostHolder)
        {
            costsInfo.Add(selection.GetComponent<UIResourceInfoPanel>());
        }
    }

    public void SetInfo(Vector3 position, string title, string displayTitle, int level, float workEthic, string description, List<ResourceValue> costs, List<ResourceValue> produces, 
        List<List<ResourceValue>> consumes, List<int> produceTimeList, bool unit, int health, float speed, int strength, int cargoCapacity)
    {
        transform.position = position;
        this.title.text = displayTitle;
        if (title == displayTitle)
            this.level.text = "Level " + level.ToString();
        else
            this.level.text = "Level " + level.ToString() + " " + title;

        int producesCount = produces.Count;
        int maxCount = costs.Count-1;
        int resourcePanelSize = 90;
        int produceHolderWidth = 220;
        int produceHolderHeight = 90;
        int produceContentsWidth = 320;
        int produceContentsHeight = 120;
        int produceLayoutPadding = 10;
        int imageLineWidth = 300;

        producesTitle.text = "Produces / Requires";

        if (producesCount == 0)
        {
            producesText.gameObject.SetActive(true);

            if (unit)
            {
                producesText.text = description;
                producesTitle.text = "Unit Info";
                this.health.text = health.ToString();
                this.speed.text = Mathf.RoundToInt(speed * 2).ToString();
                if (cargoCapacity > 0)
                {
                    strengthImage.sprite = inventorySprite;
                    this.strength.text = cargoCapacity.ToString();
                }
                else
                {
                    this.strength.text = strength.ToString();
                    strengthImage.sprite = strengthSprite;
                }

                produceContentsHeight = 160;
                produceHolderHeight = 110;
            }
            else
            {
                if (workEthic > 0)
                    producesText.text = "Work Ethic +" + Mathf.RoundToInt(workEthic * 100) + "%";
                else
                    producesText.text = description;
                producesTitle.text = "Produces";
            }

            produceHolderWidth = 330;
            produceContentsWidth = 350;
        }
        else
        {
            producesText.gameObject.SetActive(false);
            unitInfo.gameObject.SetActive(false);
        }

        for (int i = 0; i < produceConsumesHolders.Count; i++)
        {
            if (i >= producesCount)
            {
                produceConsumesHolders[i].gameObject.SetActive(false);
            }
            else
            {
                produceConsumesHolders[i].gameObject.SetActive(true);
                GenerateProduceInfo(produces[i], consumes[i], i, produceTimeList[i]);

                if (maxCount < consumes[i].Count)
                    maxCount = consumes[i].Count;
            }
        }

        //must be below produce consumes holder activations
        if (unit)
            unitInfo.gameObject.SetActive(true);

        GenerateResourceInfo(costs, costsInfo, 60);

        //adjusting height of panel
        if (producesCount > 1)
        {
            int shift = resourcePanelSize * (producesCount - 1);
            produceContentsHeight += shift;
            produceLayoutPadding += shift;
        }

        //adjusting width of panel
        if (maxCount > 1)
        {
            int shift = resourcePanelSize * (maxCount - 1);
            if (producesCount > 0)
                produceHolderWidth += shift;
        }
        if (maxCount > 2)
        {
            int shift = resourcePanelSize * (maxCount - 2);
            produceContentsWidth += shift;
            imageLineWidth += shift;
        }

        resourceProducedHolder.sizeDelta = new Vector2(produceHolderWidth, produceHolderHeight);
        resourceProduceAllHolder.sizeDelta = new Vector2(produceContentsWidth, produceContentsHeight);
        resourceProduceLayout.padding.bottom = produceLayoutPadding;
        imageLine.sizeDelta = new Vector2(imageLineWidth, 4);
        rectTransform.sizeDelta = new Vector2(300, 314 + produceContentsHeight); //height without produce contents window plus 70



        PositionCheck();
    }

    private void GenerateResourceInfo(List<ResourceValue> resourcesInfo, List<UIResourceInfoPanel> resourcesToShow, int produceTime)
    {
        int resourcesCount = resourcesInfo.Count;
        
        for (int i = 0; i < resourcesToShow.Count; i++)
        {
            if (i >= resourcesCount)
            {
                resourcesToShow[i].gameObject.SetActive(false);
            }
            else
            {
                resourcesToShow[i].gameObject.SetActive(true);

                resourcesToShow[i].resourceAmount.text = Mathf.RoundToInt(resourcesInfo[i].resourceAmount * (60f / produceTime)).ToString();
                resourcesToShow[i].resourceImage.sprite = ResourceHolder.Instance.GetIcon(resourcesInfo[i].resourceType);
                resourcesToShow[i].resourceType = resourcesInfo[i].resourceType;
            }
        }
    }

    private void GenerateProduceInfo(ResourceValue producedResource, List<ResourceValue> consumedResources, int produceIndex, int produceTime)
    {
        producesInfo[produceIndex].resourceAmount.text = Mathf.RoundToInt(producedResource.resourceAmount * (60f / produceTime)).ToString();
        producesInfo[produceIndex].resourceImage.sprite = ResourceHolder.Instance.GetIcon(producedResource.resourceType);
        producesInfo[produceIndex].resourceType = producedResource.resourceType;

        if (consumedResources.Count > 0)
        {
            noneTextList[produceIndex].gameObject.SetActive(false);
        }
        else
        {
            noneTextList[produceIndex].gameObject.SetActive(true);
        }

        GenerateResourceInfo(consumedResources, consumesInfo[produceIndex], produceTime);
    }

    private void PositionCheck()
    {
        if (transform.localPosition.y - rectTransform.rect.height < screenHeightNegHalf)
        {
            rectTransform.pivot = new Vector2(0.5f,0);
        }
        else
        {
            rectTransform.pivot = new Vector2(0.5f, 1);
        }
    }

}
