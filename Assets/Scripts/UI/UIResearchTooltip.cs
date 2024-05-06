using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIResearchTooltip : MonoBehaviour, ITooltip
{
    public MapWorld world;
    public TMP_Text title, level, costTitle, producesTitle, descriptionTitle, descriptionText, health, speed, strength, workEthicText, housingText, waterText;
    public Image mainImage, strengthImage, waterImage;
    public Sprite inventorySprite, strengthSprite, powerSprite, waterSprite;
    public GameObject produceHolder, descriptionHolder, workEthicImage, housingImage;
    public RectTransform allContents, /*resourceProduceAllHolder, */imageLine, resourceProducedHolder, resourceCostHolder, unitInfo, spaceHolder, cityStatsDescription;
    public VerticalLayoutGroup resourceProduceLayout;
    public HorizontalLayoutGroup firstResourceProduceLayout;
    private List<Transform> produceConsumesHolders = new();
    private List<UIResourceInfoPanel> costsInfo = new(), producesInfo = new();
    private GameObject firstArrow;
    private List<List<UIResourceInfoPanel>> consumesInfo = new();

    Vector3 cityStatsOriginalLoc;

	//for tweening
	[HideInInspector]
    public bool activeStatus;
    //private List<TMP_Text> noneTextList = new();

    private void Awake()
    {        
        transform.localScale = Vector3.zero;
        cityStatsOriginalLoc = cityStatsDescription.localPosition;
        gameObject.SetActive(false);

        foreach (Transform selection in resourceProducedHolder)
            produceConsumesHolders.Add(selection);

        for (int i = 0; i < produceConsumesHolders.Count; i++)
        {
            int j = 0;
            List<UIResourceInfoPanel> consumesPanels = new();

            foreach (Transform selection in produceConsumesHolders[i])
            {
                if (i == 0 && j == 1)
                    firstArrow = selection.gameObject; //getting first arrow to hide for units

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

    //public void HandleEsc()
    //{
    //    if (activeStatus)
    //        CloseWindow();
    //}

    public void ToggleVisibility(bool v)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            world.infoPopUpCanvas.gameObject.SetActive(true);
            world.iTooltip = this;
            activeStatus = true;
            gameObject.SetActive(v);

            LeanTween.scale(allContents, Vector3.one, 0.25f).setEaseLinear();
        }
        else
        {
            world.iTooltip = null;
            activeStatus = false;
			LeanTween.scale(allContents, Vector3.zero, 0.25f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        if (world.iTooltip == null)
            world.infoPopUpCanvas.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    public void SetInfo(Sprite mainSprite, string title, string displayTitle, int level, float workEthic, string description, List<ResourceValue> costs, List<ResourceValue> produces,
        List<List<ResourceValue>> consumes, List<int> produceTimeList, bool unit, int health, float speed, int strength, int cargoCapacity, int housing, int water, int power, bool wonder, Era era, bool utility, bool rocks = false)
    {
        mainImage.sprite = mainSprite;
        this.title.text = displayTitle;

        if (wonder)
        {
            this.level.text = Regex.Replace(era.ToString(), "((?<=[a-z])[A-Z]|[A-Z](?=[a-z]))", " $1") + " Wonder";
            costTitle.text = "Total Cost to Build";
        }
        else
        {
            if (title == displayTitle)
                this.level.text = "Level " + level.ToString();
            else
                this.level.text = "Level " + level.ToString() + " " + title;

            costTitle.text = "Cost";
        }

        int producesCount = produces.Count;
        int maxCount = costs.Count;
        int maxConsumed = 0;
        int resourcePanelSize = 90;
        resourcePanelSize += 0; //for gap
        int produceHolderWidth = 220;
        int produceHolderHeight = 160;
        int produceContentsWidth = 370;
        int produceContentsHeight = 460;
        bool arrowBuffer = false;
        bool showCityStatsDesc = false;

		//reseting produce section layout
		//resourceProduceLayout.padding.top = 0;
		//resourceProduceLayout.spacing = 0;
		//int produceLayoutPadding = 10;

		if (workEthic != 0)
		{
			workEthicImage.SetActive(true);
			workEthicText.gameObject.SetActive(true);
            string prefix = "+";
            workEthicText.color = Color.black;
            if (workEthic < 0)
            {
                prefix = "";
                workEthicText.color = Color.red;
            }
			workEthicText.text = prefix + Mathf.RoundToInt(workEthic * 100).ToString() + "%";
			showCityStatsDesc = true;
		}
        else
        {
            workEthicImage.SetActive(false);
            workEthicText.gameObject.SetActive(false);
        }

		if (housing > 0)
		{
			housingImage.SetActive(true);
			housingText.gameObject.SetActive(true);
			housingText.text = "+" + housing.ToString();
			showCityStatsDesc = true;
		}
        else
        {
            housingImage.SetActive(false);
            housingText.gameObject.SetActive(false);
        }

		if (water > 0 || power > 0)
		{
            bool isWater = water > 0;
            waterImage.sprite = isWater ? waterSprite : powerSprite;
            waterImage.gameObject.SetActive(true);
			waterText.gameObject.SetActive(true);
            int num = isWater ? water : power;
			waterText.text = "+" + num.ToString();
			showCityStatsDesc = true;
		}
        else
        {
            waterImage.gameObject.SetActive(false);
            waterText.gameObject.SetActive(false);
        }

		cityStatsDescription.gameObject.SetActive(showCityStatsDesc);

		producesTitle.text = "Produces / Requires";

        if (description.Length > 0 || showCityStatsDesc)
        {
            descriptionHolder.SetActive(true);
			cityStatsDescription.localPosition = cityStatsOriginalLoc;

			if (description.Length > 0)
            {
				descriptionText.gameObject.SetActive(true);
				bool bigText;
                
                if (!unit && description.Length < 23)
                {
				    produceContentsHeight += 85;
                    bigText = false;
                }
                else
                {
				    produceContentsHeight += 120;
                    bigText = true;
                }

                if (showCityStatsDesc)
                {
					Vector3 localPosition = cityStatsOriginalLoc;
					localPosition.y += bigText ? -75 : -45;
					cityStatsDescription.localPosition = localPosition;
				}
            }
            else
            {
                descriptionText.gameObject.SetActive(false);
            }

			if (showCityStatsDesc)
			{
				produceContentsHeight += description.Length > 0 ? 60 : 110;
				descriptionTitle.text = "Benefits";
			}

			if (unit)
            {
                descriptionText.text = description;
                producesTitle.text = "Cost per Growth Cycle";
                descriptionTitle.text = "Unit Info";
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

				unitInfo.gameObject.SetActive(true);
				produceContentsHeight += 50;
				resourceProduceLayout.childAlignment = TextAnchor.UpperCenter;
				//resourceProduceLayout.padding.top = -5;
				//resourceProduceLayout.spacing = 15;
				//produceContentsHeight = 160;
				//produceHolderHeight = 110;
			}
            else if (wonder)
            {
				descriptionTitle.text = "Reward";
                producesTitle.text = "Cost per Percent";
				descriptionText.text = description;
			}
            else if (utility)
            {
				descriptionTitle.text = "Additional Info";
				descriptionText.text = description;
				producesTitle.text = "Bridge Cost";
			}
            else
            {
				descriptionTitle.text = "Additional Info";

				//if (workEthic > 0)
    //                descriptionText.text = "Work Ethic +" + Mathf.RoundToInt(workEthic * 100) + "%";
    //            else
                descriptionText.text = description;
                producesTitle.text = "Produces";
			}

            //produceContentsWidth = 370;
            //produceHolderWidth = 340;
        }
        else
        {
            descriptionHolder.SetActive(false);
            unitInfo.gameObject.SetActive(false);
        }

        if (unit)
        {
			unitInfo.gameObject.SetActive(true);
			produceContentsHeight += 10;
            resourceProduceLayout.childAlignment = TextAnchor.UpperCenter;

            if (consumes.Count > 0)
                firstResourceProduceLayout.padding.left = -(consumes[0].Count - 1) * (resourcePanelSize / 2);
		}
        else if (wonder)
        {
			unitInfo.gameObject.SetActive(false);
			resourceProduceLayout.childAlignment = TextAnchor.UpperCenter;

			if (consumes.Count > 0)
                firstResourceProduceLayout.padding.left = -(consumes[0].Count) * (resourcePanelSize / 2);
		}
        else if (utility)
        {
			unitInfo.gameObject.SetActive(false);
			resourceProduceLayout.childAlignment = TextAnchor.UpperCenter;

			if (consumes.Count > 0)
				firstResourceProduceLayout.padding.left = -(consumes[0].Count - 1) * (resourcePanelSize / 2);
		}
		else
        {
			unitInfo.gameObject.SetActive(false);
			resourceProduceLayout.childAlignment = TextAnchor.UpperLeft;
            firstResourceProduceLayout.padding.left = 0;
		}

        if (producesCount > 0)
        {
            produceHolder.SetActive(true);
        }
		else
		{
			produceHolder.SetActive(false);
			produceContentsHeight -= 140;
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
                GenerateProduceInfo(produces[i], consumes[i], i, produceTimeList[i], rocks);

                //if (maxCount < consumes[i].Count + 1)
                //    maxCount = consumes[i].Count + 1;

                int one = produceTimeList[i] > 0 ? 1 : 0;

				if (maxCount < consumes[i].Count + one + 1)
				{
					arrowBuffer = true;
					maxCount = consumes[i].Count + one + 1;
				}

				if (maxConsumed < consumes[i].Count + one)
					maxConsumed = consumes[i].Count + one;
			}
        }

        //must be below produce consumes holder activations
        //if (unit)
        //{
        //    unitInfo.gameObject.SetActive(true);
        //}
        //else
        //{
        //    unitInfo.gameObject.SetActive(false);
        //}

        GenerateResourceInfo(costs, costsInfo, 0); //0 means don't show produce time

		//adjusting height of panel
		//if (producesCount > 1)
		//{
		//    int shift = resourcePanelSize * (producesCount - 1);
		//    produceContentsHeight += shift;
		//    //produceLayoutPadding += shift;
		//}

		////adjusting width of panel
		//if (maxCount > 1)
		//{
		//    int shift = resourcePanelSize * (maxCount - 1);
		//    if (producesCount > 0)
		//        produceHolderWidth += shift;
		//}
		//if (maxCount > 2)
		//{
		//    int shift = resourcePanelSize * (maxCount - 2);
		//    produceContentsWidth += shift;
		//}

		//adjusting height of panel
		if (producesCount > 1)
			produceContentsHeight += resourcePanelSize * (producesCount - 1);

		//adjusting width of panel
		if (maxCount > 4)
			produceContentsWidth += resourcePanelSize * (maxCount - 4);

		if (maxConsumed > 1)
			produceHolderWidth += resourcePanelSize * (maxConsumed - 1);

		//if (unit)
		//	produceContentsHeight += 50;

		if (arrowBuffer)
			produceContentsWidth += 40;

		resourceProducedHolder.sizeDelta = new Vector2(produceHolderWidth, produceHolderHeight);
        allContents.sizeDelta = new Vector2(produceContentsWidth, produceContentsHeight);
        spaceHolder.sizeDelta = new Vector2(100, Mathf.Max(resourcePanelSize * (producesCount - 1),0));
        imageLine.sizeDelta = new Vector2(produceContentsWidth - 20, 4);

        //descriptionHolder.transform.localPosition = originalDescPos;
        //Vector3 descriptionShift = Vector3.zero;
        //if (producesCount == 0)
        //    descriptionShift -= new Vector3(0, resourcePanelSize * -1.5f, 0);
        //else
        //    descriptionShift -= new Vector3(0, resourcePanelSize * (producesCount - 1), 0);
        //descriptionHolder.transform.localPosition = descriptionShift;
        //resourceProduceLayout.padding.bottom = produceLayoutPadding;
		//allContents.sizeDelta = new Vector2(300, 314 + produceContentsHeight); //height without produce contents window plus 70
		//     if (unit)
		//resourceProduceLayout.childAlignment = TextAnchor.UpperCenter;
		//     else
		//resourceProduceLayout.childAlignment = TextAnchor.UpperLeft;

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
                    resourcesToShow[i].SetResourceAmount(produceTime);
                    resourcesToShow[i].SetResourceType(ResourceType.Time);
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

                resourcesToShow[i].SetResourceAmount(resourcesInfo[i].resourceAmount);
                //resourcesToShow[i].resourceAmount.text = Mathf.RoundToInt(resourcesInfo[i].resourceAmount * (60f / produceTime)).ToString();
                resourcesToShow[i].resourceImage.sprite = ResourceHolder.Instance.GetIcon(resourcesInfo[i].resourceType);
                resourcesToShow[i].SetResourceType(resourcesInfo[i].resourceType);
            }
        }
    }

    private void GenerateProduceInfo(ResourceValue producedResource, List<ResourceValue> consumedResources, int produceIndex, int produceTime, bool rocks)
    {
        if (producedResource.resourceType != ResourceType.None)
        {
            if (produceIndex == 0)
                producesInfo[0].gameObject.SetActive(true);

            producesInfo[produceIndex].SetResourceAmount(producedResource.resourceAmount);
            //producesInfo[produceIndex].resourceAmount.text = Mathf.RoundToInt(producedResource.resourceAmount * (60f / produceTime)).ToString();
            producesInfo[produceIndex].SetResourceType(producedResource.resourceType);
            if (rocks)
            {
                RocksType rocksType = ResourceHolder.Instance.GetRocksType(producedResource.resourceType);
                Sprite tempImage;

                if (rocksType == RocksType.Normal)
                    tempImage = world.rocksNormal;
                else if (rocksType == RocksType.Luxury)
                    tempImage = world.rocksLuxury;
                else
                    tempImage = world.rocksChemical;

                producesInfo[produceIndex].resourceImage.sprite = tempImage;
                producesInfo[produceIndex].SetMessage(rocksType);
			}
            else
            {
                producesInfo[produceIndex].resourceImage.sprite = ResourceHolder.Instance.GetIcon(producedResource.resourceType);
            }

			firstArrow.SetActive(true);
			GenerateResourceInfo(consumedResources, consumesInfo[produceIndex], produceTime);
        }
        else
        {
            producesInfo[produceIndex].gameObject.SetActive(false);
            firstArrow.SetActive(false);
            GenerateResourceInfo(consumedResources, consumesInfo[produceIndex], produceTime);
		}
    }

    private void PositionCheck()
    {
        Vector3 p = Input.mousePosition;
        //float x = 0.5f;
        //float y = 0.5f;
        //float xChange = 0;
        //float yChange = 0;

        p.z = 935;
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
        world.cityBuilderManager.PlayCloseAudio();
        ToggleVisibility(false);
    }

	public void CheckResource(City city, int amount, ResourceType type)
	{
		
	}
}
