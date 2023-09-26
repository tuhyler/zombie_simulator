using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICityUpgradePanel : MonoBehaviour
{
    public MapWorld world;
    public CityBuilderManager cityBuilderManager;
    public TMP_Text title, level, producesTitle, health, speed, strength;
    private TMP_Text producesText;
    public Image mainImage, strengthImage;
    public Sprite inventorySprite, strengthSprite;
    public RectTransform allContents, resourceProduceAllHolder, imageLine, resourceProducedHolder, resourceCostHolder, unitInfo;
    public VerticalLayoutGroup resourceProduceLayout;
    private List<Transform> produceConsumesHolders = new();
    private List<UIResourceInfoPanel> costsInfo = new(), producesInfo = new();
    private List<List<UIResourceInfoPanel>> consumesInfo = new();

    private Vector3Int improvementLoc;
    private CityImprovement improvement;
    private Unit unit;
    private bool cannotAfford;
    private List<ResourceValue> upgradeCost = new();
    private List<ResourceValue> produces = new();
    List<List<ResourceValue>> consumes = new();
    List<int> produceTime = new();

    //for tweening
    [HideInInspector]
    public bool activeStatus;

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

                j++;
            }

            consumesInfo.Add(consumesPanels);
        }
        foreach (Transform selection in resourceCostHolder)
        {
            costsInfo.Add(selection.GetComponent<UIResourceInfoPanel>());
        }
    }

    public void ToggleVisibility(bool v, ResourceManager resourceManager = null, CityImprovement improvement = null, Unit unit = null)
    {
        if (!activeStatus && !v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            world.infoPopUpCanvas.gameObject.SetActive(true);
            activeStatus = true;
            
            this.improvement = improvement;
            if (improvement != null)
                improvementLoc = improvement.loc;
            this.unit = unit;

            string name = improvement.GetImprovementData.improvementName + '-' + improvement.GetImprovementData.improvementLevel;
            upgradeCost = new(world.GetUpgradeCost(name));

            if (unit != null)
            {
                UnitBuildDataSO unitData = world.GetUnitUpgradeData(name);
                List<int> produceTime = new();
                SetInfo(unitData.image, unitData.unitType.ToString(), unitData.unitName, unitData.unitLevel, 0, unitData.unitDescription,
                produces, consumes, produceTime, true, unitData.health, unitData.movementSpeed, unitData.baseAttackStrength, unitData.cargoCapacity, resourceManager);
            }
            else
            {
                ImprovementDataSO improvementData = world.GetUpgradeData(name);

                consumes.Add(new(improvementData.consumedResources));
                if (improvementData.consumedResources1.Count > 0)
                    consumes.Add(new(improvementData.consumedResources1));
                if (improvementData.consumedResources2.Count > 0)
                    consumes.Add(new(improvementData.consumedResources2));
                if (improvementData.consumedResources3.Count > 0)
                    consumes.Add(new(improvementData.consumedResources3));
                if (improvementData.consumedResources4.Count > 0)
                    consumes.Add(new(improvementData.consumedResources4));

                SetInfo(improvementData.image, improvementData.improvementName, improvementData.improvementDisplayName, improvementData.improvementLevel, improvementData.workEthicChange, 
                    improvementData.improvementDescription, improvementData.producedResources, consumes, improvementData.producedResourceTime, false, 0, 0, 0, 0, resourceManager);
            }

            gameObject.SetActive(true);

            LeanTween.scale(allContents, Vector3.one, 0.25f).setEaseLinear();
        }
        else
        {
            ResetData();
            LeanTween.scale(allContents, Vector3.zero, 0.25f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
        world.infoPopUpCanvas.gameObject.SetActive(false);
    }

    private void ResetData()
    {
        activeStatus = false;
        this.improvement = null;
        this.unit = null;
        produces.Clear();
        consumes.Clear();
        produceTime.Clear();
    }

    public void SetInfo(Sprite mainSprite, string title, string displayTitle, int level, float workEthic, string description, List<ResourceValue> produces,
        List<List<ResourceValue>> consumes, List<int> produceTimeList, bool unit, int health, float speed, int strength, int cargoCapacity, ResourceManager resourceManager)
    {
        mainImage.sprite = mainSprite;
        this.title.text = displayTitle;
        if (title == displayTitle)
            this.level.text = "Level " + level.ToString();
        else
            this.level.text = "Level " + level.ToString() + " " + title;

        int producesCount = produces.Count;
        int maxCount = upgradeCost.Count - 2;
        int resourcePanelSize = 90;
        resourcePanelSize += 0; //for gap
        int produceHolderWidth = 220;
        int produceHolderHeight = 110;
        int produceContentsWidth = 340;
        int produceContentsHeight = 120;

        //reseting produce section layout
        resourceProduceLayout.padding.top = 0;
        resourceProduceLayout.spacing = 0;

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

        GenerateResourceInfo(upgradeCost, costsInfo, 0, true, resourceManager); //0 means don't show produce time

        //adjusting height of panel
        if (producesCount > 1)
        {
            int shift = resourcePanelSize * (producesCount - 1);
            produceContentsHeight += shift;
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
        allContents.sizeDelta = new Vector2(produceContentsWidth, 390 + produceContentsHeight);
        imageLine.sizeDelta = new Vector2(produceContentsWidth - 20, 4);

        PositionCheck();
    }

    private void GenerateResourceInfo(List<ResourceValue> resourcesInfo, List<UIResourceInfoPanel> resourcesToShow, int produceTime, bool cost, ResourceManager resourceManager = null)
    {
        int resourcesCount = resourcesInfo.Count;
        cannotAfford = false;

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
                    resourcesToShow[i].resourceAmountText.text = produceTime.ToString();
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

                resourcesToShow[i].resourceAmountText.text = resourcesInfo[i].resourceAmount.ToString();
                resourcesToShow[i].resourceImage.sprite = ResourceHolder.Instance.GetIcon(resourcesInfo[i].resourceType);
                resourcesToShow[i].resourceType = resourcesInfo[i].resourceType;

                if (cost)
                {
                    if (resourceManager.CheckResourceAvailability(resourcesInfo[i]))
                    {
                        resourcesToShow[i].resourceAmountText.color = Color.white;
                    }
                    else
                    {
                        cannotAfford = true;
                        resourcesToShow[i].resourceAmountText.color = Color.red;
                    }
                }
            }
        }
    }

    private void GenerateProduceInfo(ResourceValue producedResource, List<ResourceValue> consumedResources, int produceIndex, int produceTime)
    {
        producesInfo[produceIndex].resourceAmountText.text = producedResource.resourceAmount.ToString();
        producesInfo[produceIndex].resourceImage.sprite = ResourceHolder.Instance.GetIcon(producedResource.resourceType);
        producesInfo[produceIndex].resourceType = producedResource.resourceType;

        GenerateResourceInfo(consumedResources, consumesInfo[produceIndex], produceTime, false);
    }

    private void PositionCheck()
    {
        Vector3 p = Input.mousePosition;
        p.z = 1;
        allContents.pivot = new Vector2(p.x / Screen.width, p.y / Screen.height);

        Vector3 pos = Camera.main.ScreenToWorldPoint(p);
        allContents.transform.position = pos;
    }


    public void OnPointerClick()
    {
        if (cannotAfford && !cityBuilderManager.isQueueing)
        {
            StartCoroutine(Shake());
            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't afford");
            return;
        }

        if (improvement != null)
            cityBuilderManager.UpgradeSelectedImprovementQueueCheck(improvementLoc, improvement);
        else if (unit != null)
            cityBuilderManager.UpgradeUnit();

        ResetData();
        gameObject.SetActive(false);
        world.infoPopUpCanvas.gameObject.SetActive(false);
    }

    private IEnumerator Shake()
    {
        Vector3 initialPos = transform.localPosition;
        float elapsedTime = 0f;
        float duration = 0.2f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            transform.localPosition = initialPos + (Random.insideUnitSphere * 10f);
            yield return null;
        }

        transform.localPosition = initialPos;
    }

    public void CheckCosts(ResourceManager resourceManager)
    {
        bool cannotAffordTemp = false;
        
        int i = 0;
        foreach (ResourceValue value in upgradeCost)
        {
            if (resourceManager.CheckResourceAvailability(value))
            {
                costsInfo[i].resourceAmountText.color = Color.white;
            }
            else
            {
                cannotAffordTemp = true;
                costsInfo[i].resourceAmountText.color = Color.red;
            }

            i++;

        }

        cannotAfford = cannotAffordTemp;
    }

    public void CloseWindow()
    {
        ToggleVisibility(false);
    }
}
