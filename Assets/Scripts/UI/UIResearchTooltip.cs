using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIResearchTooltip : MonoBehaviour
{
    public MapWorld world;
    public TMP_Text title, level, producesTitle, health, speed, strength;
    private TMP_Text producesText;
    public Image mainImage, strengthImage;
    public Sprite inventorySprite, strengthSprite;
    public RectTransform allContents, resourceProduceAllHolder, imageLine, resourceProducedHolder, resourceCostHolder, unitInfo;
    public VerticalLayoutGroup resourceProduceLayout;
    private List<Transform> produceConsumesHolders = new();
    private List<UIResourceInfoPanel> costsInfo = new(), producesInfo = new();
    private List<List<UIResourceInfoPanel>> consumesInfo = new();

    //for tweening
    [HideInInspector]
    public bool activeStatus;
    //private List<TMP_Text> noneTextList = new();

    private void Awake()
    {
        transform.localScale = Vector3.zero;
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
                else if (j >= 2 /*&& j <= 7*/)
                    consumesPanels.Add(selection.GetComponent<UIResourceInfoPanel>());
                //else if (j == 8)
                //    noneTextList.Add(selection.GetComponent<TMP_Text>());

                j++;
            }

            consumesInfo.Add(consumesPanels);
        }
        foreach (Transform selection in resourceCostHolder)
        {
            costsInfo.Add(selection.GetComponent<UIResourceInfoPanel>());
        }
    }

    public void ToggleVisibility(bool v)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            world.infoPopUpCanvas.gameObject.SetActive(true);
            activeStatus = true;
            gameObject.SetActive(v);

            LeanTween.scale(allContents, Vector3.one, 0.25f).setEaseLinear();
        }
        else
        {
            activeStatus = false;
            LeanTween.scale(allContents, Vector3.zero, 0.25f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
        world.infoPopUpCanvas.gameObject.SetActive(false);
    }

    public void SetInfo(Vector3 position, Sprite mainSprite, string title, string displayTitle, int level, float workEthic, string description, List<ResourceValue> costs, List<ResourceValue> produces,
        List<List<ResourceValue>> consumes, List<int> produceTimeList, bool unit, int health, float speed, int strength, int cargoCapacity)
    {
        mainImage.sprite = mainSprite;
        this.title.text = displayTitle;
        if (title == displayTitle)
            this.level.text = "Level " + level.ToString();
        else
            this.level.text = "Level " + level.ToString() + " " + title;

        int producesCount = produces.Count;
        int maxCount = costs.Count - 2;
        int resourcePanelSize = 90;
        resourcePanelSize += 0; //for gap
        int produceHolderWidth = 220;
        int produceHolderHeight = 110;
        int produceContentsWidth = 340;
        int produceContentsHeight = 120;

        //reseting produce section layout
        resourceProduceLayout.padding.top = 0;
        resourceProduceLayout.spacing = 0;
        //int produceLayoutPadding = 10;

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

                resourceProduceLayout.padding.top = -5;
                resourceProduceLayout.spacing = 15;
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

            produceContentsWidth = 370;
            produceHolderWidth = 340;
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

                if (maxCount < consumes[i].Count + 1)
                    maxCount = consumes[i].Count + 1;
            }
        }

        //must be below produce consumes holder activations
        if (unit)
            unitInfo.gameObject.SetActive(true);

        GenerateResourceInfo(costs, costsInfo, 0); //0 means don't show produce time

        //adjusting height of panel
        if (producesCount > 1)
        {
            int shift = resourcePanelSize * (producesCount - 1);
            produceContentsHeight += shift;
            //produceLayoutPadding += shift;
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
        }

        resourceProducedHolder.sizeDelta = new Vector2(produceHolderWidth, produceHolderHeight);
        allContents.sizeDelta = new Vector2(produceContentsWidth, 340 + produceContentsHeight);
        //resourceProduceLayout.padding.bottom = produceLayoutPadding;
        imageLine.sizeDelta = new Vector2(produceContentsWidth - 20, 4);
        //allContents.sizeDelta = new Vector2(300, 314 + produceContentsHeight); //height without produce contents window plus 70

        PositionCheck();
    }

    private void GenerateResourceInfo(List<ResourceValue> resourcesInfo, List<UIResourceInfoPanel> resourcesToShow, int produceTime)
    {
        int resourcesCount = resourcesInfo.Count;

        for (int i = 0; i < resourcesToShow.Count; i++)
        {
            if (i > resourcesCount)
            {
                resourcesToShow[i].gameObject.SetActive(false);
            }
            else if (i == resourcesCount) //for adding production time
            {
                if (produceTime > 0)
                {
                    resourcesToShow[i].gameObject.SetActive(true);
                    resourcesToShow[i].resourceAmount.text = produceTime.ToString();
                    resourcesToShow[i].resourceType = ResourceType.Time;
                    resourcesToShow[i].resourceImage.sprite = ResourceHolder.Instance.GetIcon(ResourceType.Time);
                }
                else
                {
                    resourcesToShow[i].gameObject.SetActive(false);
                }
            }
            else
            {
                resourcesToShow[i].gameObject.SetActive(true);

                resourcesToShow[i].resourceAmount.text = resourcesInfo[i].resourceAmount.ToString();
                //resourcesToShow[i].resourceAmount.text = Mathf.RoundToInt(resourcesInfo[i].resourceAmount * (60f / produceTime)).ToString();
                resourcesToShow[i].resourceImage.sprite = ResourceHolder.Instance.GetIcon(resourcesInfo[i].resourceType);
                resourcesToShow[i].resourceType = resourcesInfo[i].resourceType;
            }
        }
    }

    private void GenerateProduceInfo(ResourceValue producedResource, List<ResourceValue> consumedResources, int produceIndex, int produceTime)
    {
        producesInfo[produceIndex].resourceAmount.text = producedResource.resourceAmount.ToString();
        //producesInfo[produceIndex].resourceAmount.text = Mathf.RoundToInt(producedResource.resourceAmount * (60f / produceTime)).ToString();
        producesInfo[produceIndex].resourceImage.sprite = ResourceHolder.Instance.GetIcon(producedResource.resourceType);
        producesInfo[produceIndex].resourceType = producedResource.resourceType;

        //noneTextList[produceIndex].gameObject.SetActive(false);
        //if (consumedResources.Count > 0)
        //{
        //    noneTextList[produceIndex].gameObject.SetActive(false);
        //}
        //else
        //{
        //    noneTextList[produceIndex].gameObject.SetActive(true);
        //}

        GenerateResourceInfo(consumedResources, consumesInfo[produceIndex], produceTime);
    }

    private void PositionCheck()
    {
        Vector3 p = Input.mousePosition;
        //float x = 0.5f;
        //float y = 0.5f;
        //float xChange = 0;
        //float yChange = 0;

        p.z = 1;
        //p.z = 1;
        //if (p.y + allContents.rect.height * 0.4f > Screen.height - 100)
        //    y = 1f;
        //else if (p.y - allContents.rect.height * 0.4f < 0)
        //    y = 0f;

        //if (p.x + allContents.rect.width * 0.5f > Screen.width)
        //    x = 1f;
        //else if (p.x - allContents.rect.width * 0.5f < 0)
        //    x = 0f;

        allContents.pivot = new Vector2(p.x / Screen.width, p.y / Screen.height);

        Vector3 pos = Camera.main.ScreenToWorldPoint(p);
        allContents.transform.position = pos;
        //if (transform.localPosition.y - rectTransform.rect.height < screenHeightNegHalf)
        //{
        //    allContents.pivot = new Vector2(0.5f, 0);
        //}
        //else
        //{
        //    allContents.pivot = new Vector2(0.5f, 1);
        //}
    }

    public void CloseWindow()
    {
        ToggleVisibility(false);
    }
}
