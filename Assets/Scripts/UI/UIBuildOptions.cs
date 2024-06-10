using System.Collections;
using System.Collections.Generic;
using System.Resources;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIBuildOptions : MonoBehaviour, IPointerClickHandler 
{
    [SerializeField]
    private ImprovementDataSO buildData;
    public ImprovementDataSO BuildData { get { return buildData; } set { buildData = value; } }

    [SerializeField]
    private UnitBuildDataSO unitBuildData;
    public UnitBuildDataSO UnitBuildData { get { return unitBuildData; } set { unitBuildData = value; } }

    private UIBuilderHandler buttonHandler;

    [SerializeField]
    private TMP_Text objectName, objectLevel, producesTitle, health, speed, strength, description, descriptionTitle, workEthicText, housingText, waterText;

    [SerializeField]
    private Image objectImage, strengthImage, waterImage;

    [SerializeField]
    private Sprite inventorySprite, rocksNormal, rocksLuxury, rocksChemical, waterIcon, powerIcon;

    [SerializeField]
    private GameObject resourceInfoPanel, productionPanel, descriptionPanel, workEthicImage, housingImage, newIcon;

    [SerializeField]
    private Transform resourceProducedHolder, resourceCostHolder, unitDescription, cityStatsDecription;

    [SerializeField]
    private RectTransform /*resourceProduceAllHolder, */allContents, imageLine;

    //[SerializeField]
    //private VerticalLayoutGroup resourceProduceLayout;

    private List<Transform> produceConsumesHolders = new();

    private bool isUnitPanel, cantAfford, isShowing, shaking;

    [HideInInspector]
    public bool /*needsBarracks, fullBarracks, travelingBarracks, trainingBarracks, waterMax, */isFlashing, somethingNew/*, locked*/;
    //for checking if city can afford resource and if work ethic changes values
    [HideInInspector]
    public List<UIResourceInfoPanel> costResourcePanels = new(), producedResourcePanels = new();

    private void Awake()
    {
        //buttonHandler = GetComponentInParent<UIBuilderHandler>();

        foreach (Transform selection in resourceProducedHolder)
            produceConsumesHolders.Add(selection);

        //if (unitBuildData != null)
        //    isUnitPanel = true;
        //PopulateSelectionPanel();
    }

    public void SetBuildOptionData(UIBuilderHandler builderHandler)
    {
		this.buttonHandler = builderHandler;

		if (unitBuildData != null)
			isUnitPanel = true;
		PopulateSelectionPanel();
	}

    public void ToggleVisibility(bool v)
    {
        if (isShowing == v)
            return;
        
        isShowing = v;
        gameObject.SetActive(v);
    }

    public void Hide()
    {
		isShowing = false;
		gameObject.SetActive(false);
	}

    public void OnUnitPointerClick()
    {
        buttonHandler.PrepareUnitBuild(unitBuildData);
    }

    public void OnPointerClick()
    {
        buttonHandler.PrepareBuild(buildData);
    }

    //creating the menu card for each buildable object, showing name, function, cost, etc. 
    private void PopulateSelectionPanel()
    {
        shaking = false;
        string objectDescription = "";
        List<ResourceValue> objectProduced;
        List<int> producedResourceTime;
        List<List<ResourceValue>> objectConsumed = new();
        List<ResourceValue> objectCost;
        bool arrowBuffer = false;
		bool showCityStatsDesc = false;

		if (isUnitPanel)
        {
            health.text = unitBuildData.health.ToString();
            speed.text = Mathf.RoundToInt(unitBuildData.movementSpeed * 2).ToString();
            strength.text = unitBuildData.baseAttackStrength.ToString();
            if (unitBuildData.cargoCapacity > 0)
            {
                strengthImage.sprite = inventorySprite;
                strength.text = unitBuildData.cargoCapacity.ToString();
            }

            objectName.text = unitBuildData.unitDisplayName;
            objectLevel.text = "Level " + unitBuildData.unitLevel + " " + Regex.Replace(unitBuildData.unitType.ToString(), "((?<=[a-z])[A-Z]|[A-Z](?=[a-z]))", " $1");
            objectImage.sprite = unitBuildData.image;
            objectCost = new(unitBuildData.unitCost);

            if (unitBuildData.cycleCost.Count > 0)
            {
                ResourceValue value;
                value.resourceType = ResourceType.None;
                value.resourceAmount = 0;
                objectProduced = new() { value };
                objectConsumed.Add(new(unitBuildData.cycleCost));
			}
            else
            {
                objectProduced = new();
            }
            producedResourceTime = new();
            objectDescription = unitBuildData.unitDescription;

            ResourceValue laborCost;
            laborCost.resourceType = ResourceType.Labor;
            laborCost.resourceAmount = unitBuildData.laborCost;
            objectCost.Add(laborCost);
        }
        else
        {
            unitDescription.gameObject.SetActive(false);
            objectName.text = buildData.improvementDisplayName;
            objectLevel.text = "Level " + buildData.improvementLevel + " " + buildData.improvementName;
            objectImage.sprite = buildData.image;
            objectCost = buildData.improvementCost;
            objectProduced = new(buildData.producedResources);
            producedResourceTime = new(buildData.producedResourceTime);
            objectConsumed.Add(new(buildData.consumedResources));
            if (buildData.consumedResources1.Count > 0)
                objectConsumed.Add(new(buildData.consumedResources1));
            if (buildData.consumedResources2.Count > 0)
                objectConsumed.Add(new(buildData.consumedResources2));
            if (buildData.consumedResources3.Count > 0)
                objectConsumed.Add(new(buildData.consumedResources3));
            if (buildData.consumedResources4.Count > 0)
                objectConsumed.Add(new(buildData.consumedResources4));
            objectDescription = buildData.improvementDescription;

            if (buildData.workEthicChange != 0)
            {
                workEthicImage.SetActive(true);
                workEthicText.gameObject.SetActive(true);
                string prefix;
                if (buildData.workEthicChange < 0)
                {
                    prefix = "";
					workEthicText.color = Color.red;
				}
                else
                {
                    prefix = "+";
                    workEthicText.color = Color.black;
                }

                workEthicText.text = prefix + Mathf.RoundToInt(buildData.workEthicChange * 100).ToString() + "%";
                showCityStatsDesc = true;
            }

            if (buildData.housingIncrease > 0)
            {
                housingImage.SetActive(true);
                housingText.gameObject.SetActive(true);
                housingText.text = "+" + buildData.housingIncrease.ToString();
				showCityStatsDesc = true;
			}

            if (buildData.waterIncrease > 0 || buildData.powerIncrease > 0)
            {
                bool isWater = buildData.waterIncrease > 0;
                waterImage.gameObject.SetActive(true);
                waterImage.sprite = isWater ? waterIcon : powerIcon;
                waterText.gameObject.SetActive(true);
                int num = isWater ? buildData.waterIncrease : buildData.powerIncrease;
                waterText.text = "+" + num.ToString();
				showCityStatsDesc = true;
			}

            if (showCityStatsDesc)
                cityStatsDecription.gameObject.SetActive(true);

            if (buildData.secondaryData.Count > 0)
            {
                foreach (ImprovementDataSO tempData in buildData.secondaryData)
                {
                    if (tempData.producedResources[0].resourceType == buildData.producedResources[0].resourceType)
                        continue;
                    
                    objectProduced.AddRange(tempData.producedResources);
                    producedResourceTime.AddRange(tempData.producedResourceTime);
                    objectConsumed.Add(tempData.consumedResources);
                    if (tempData.consumedResources1.Count > 1)
                        objectConsumed.Add(tempData.consumedResources1);
                    if (tempData.consumedResources2.Count > 0)
                        objectConsumed.Add(tempData.consumedResources2);
                    if (tempData.consumedResources3.Count > 0) 
                        objectConsumed.Add(tempData.consumedResources3);
                    if (tempData.consumedResources4.Count > 0) 
                        objectConsumed.Add(tempData.consumedResources4);
                }
            }
        }

        //cost info
        GenerateResourceInfo(resourceCostHolder, objectCost, true/*, 60*/);

        //producer and consumed info
        for (int i = 0; i < produceConsumesHolders.Count; i++) //turning them all off initially
            produceConsumesHolders[i].gameObject.SetActive(false);

        int producedCount = objectProduced.Count;

        int maxCount = objectCost.Count;
        int maxConsumed = 0;

        for (int i = 0; i < producedCount; i++)
        {
            if (!isUnitPanel)
            {
                ResourceValue productionTime;
                productionTime.resourceType = ResourceType.Time;
                productionTime.resourceAmount = producedResourceTime[i];
                objectConsumed[i].Add(productionTime);
            }

            produceConsumesHolders[i].gameObject.SetActive(true);
            
            bool rocks = false;
            if (buildData != null && buildData.rawResourceType == RawResourceType.Rocks)
                rocks = true;

            GenerateProduceInfo(produceConsumesHolders[i], objectProduced[i], objectConsumed[i], rocks);

            if (maxCount < objectConsumed[i].Count + 1)
            {
                arrowBuffer = true;
                maxCount = objectConsumed[i].Count + 1;
            }

            if (maxConsumed < objectConsumed[i].Count)
                maxConsumed = objectConsumed[i].Count;
        }

        int resourcePanelSize = 90;
        int allContentsWidth = 370;
        int allContentsHeight = 520;
        int produceHolderWidth = 220;
        int imageLineWidth = 300;

        if (objectDescription.Length > 0 || showCityStatsDesc)
        {
            descriptionPanel.SetActive(true);

            if (objectDescription.Length > 0)
            {
                bool bigText;
                
                if (!isUnitPanel && objectDescription.Length < 23)
                {
                    allContentsHeight += 85;
                    bigText = false;
                }
                else
                {
				    allContentsHeight += 120;
                    bigText = true;
                }

                description.text = objectDescription;
				descriptionTitle.text = "Additional Info";

                if (showCityStatsDesc)
                {
                    Vector3 localPosition = cityStatsDecription.localPosition;
                    localPosition.y += bigText ? -75 : -45;
                    cityStatsDecription.localPosition = localPosition;
                }
            }
            else
            {
				description.gameObject.SetActive(false);
            }
            
            if (showCityStatsDesc)
            {
				allContentsHeight += objectDescription.Length > 0 ? 60 : 100;
                descriptionTitle.text = "Benefits";
			}

			if (isUnitPanel)
            {
                unitDescription.gameObject.SetActive(true);
                producesTitle.text = "Cost per Growth Cycle";
                descriptionTitle.text = "Unit Info";
            }
            else
            {
                producesTitle.text = "Produces";
            }

            if (producedCount == 0)
            {
                productionPanel.SetActive(false);
                allContentsHeight -= 140;
            }
        }
        else
        {
            descriptionPanel.SetActive(false);
        }

        //adjusting height of panel
        if (producedCount > 1)
            allContentsHeight += resourcePanelSize * (producedCount - 1);
       
        //adjusting width of panel
        if (maxCount > 4)
            allContentsWidth += resourcePanelSize * (maxCount - 4); 

        if (maxConsumed > 1)
            produceHolderWidth += resourcePanelSize * (maxConsumed - 1);

		if (isUnitPanel)
            allContentsHeight += 60;

        if (arrowBuffer)
        {
            allContentsWidth += 40;
            imageLineWidth += 40;
        }

        allContents.sizeDelta = new Vector2(allContentsWidth, allContentsHeight);
        resourceProducedHolder.GetComponent<RectTransform>().sizeDelta = new Vector2(produceHolderWidth, 160);
        Vector3 descriptionShift = descriptionPanel.transform.localPosition;
        if (producedCount == 0)
            descriptionShift.y -= resourcePanelSize * -1.5f;
        else
			descriptionShift.y -= resourcePanelSize * (producedCount - 1);
		descriptionPanel.transform.localPosition = descriptionShift;
        imageLine.sizeDelta = new Vector2(imageLineWidth, 4);
        if (isUnitPanel)
            resourceProducedHolder.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
    }

    private void GenerateResourceInfo(Transform transform, List<ResourceValue> resources, bool cost/*, int producedResourceTime*/)
    {
        foreach (ResourceValue value in resources)
        {
            GameObject panel = Instantiate(resourceInfoPanel);
            panel.transform.SetParent(transform, false);
            UIResourceInfoPanel uiResourceCostPanel = panel.GetComponent<UIResourceInfoPanel>();
            uiResourceCostPanel.transform.SetParent(transform, false);

            //uiResourceCostPanel.resourceAmount.text = Mathf.RoundToInt(value.resourceAmount * (60f / producedResourceTime)).ToString();
            uiResourceCostPanel.SetResourceAmount(value.resourceAmount);
            uiResourceCostPanel.resourceImage.sprite = ResourceHolder.Instance.GetIcon(value.resourceType);
            uiResourceCostPanel.SetResourceType(value.resourceType);

            if (cost)
                costResourcePanels.Add(uiResourceCostPanel);
            else if (isUnitPanel)
            {
                transform.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
                transform.GetComponent<RectTransform>().sizeDelta = new Vector2(resources.Count * 90, 90);
            }
        }
    }

    private void GenerateProduceInfo(Transform transform, ResourceValue producedResource, /*int producedResourceTime, */List<ResourceValue> consumedResources, bool rocks)
    {
        int i = 0;
        foreach (Transform selection in transform)
        {
            if (selection.TryGetComponent(out UIResourceInfoPanel uiResourceInfoPanel))
            {
                //uiResourceInfoPanel.resourceAmount.text = Mathf.RoundToInt(producedResource.resourceAmount * (60f / producedResourceTime)).ToString();
                if (isUnitPanel)
                {
                    selection.gameObject.SetActive(false);
                }
                else
                {
                    uiResourceInfoPanel.SetResourceAmount(producedResource.resourceAmount);
                    uiResourceInfoPanel.SetResourceType(producedResource.resourceType);
                    if (rocks)
                    {
                        RocksType rocksType = ResourceHolder.Instance.GetRocksType(producedResource.resourceType);
                        Sprite tempImage;

                        if (rocksType == RocksType.Normal)
                            tempImage = rocksNormal;
                        else if (rocksType == RocksType.Luxury)
                            tempImage = rocksLuxury;
                        else
                            tempImage = rocksChemical;

						uiResourceInfoPanel.resourceImage.sprite = tempImage;
                        uiResourceInfoPanel.SetMessage(rocksType);
                    }
                    else
                    {
					    uiResourceInfoPanel.resourceImage.sprite = ResourceHolder.Instance.GetIcon(producedResource.resourceType);
                    }

                    if (i == 0)
                        producedResourcePanels.Add(uiResourceInfoPanel);
                }
            }
            else if (i == 1 && isUnitPanel)
            {
                selection.gameObject.SetActive(false);
            }
            else if (selection.TryGetComponent(out TMP_Text text))
            {
                if (consumedResources.Count > 0)
                {
                    text.gameObject.SetActive(false);
                }
                else
                {
                    text.gameObject.SetActive(true);
                    return;
                }
            }

            i++;
        }

        GenerateResourceInfo(transform, consumedResources, false);
    }

    public void SetResourceTextToDefault()
    {
        cantAfford = false;
        foreach (UIResourceInfoPanel resourcePanel in costResourcePanels)
        {
            resourcePanel.resourceAmountText.color = Color.white;
        }
    }

    public void SetResourceTextToRed(ResourceValue resourceValue)
    {
        cantAfford = true;
        foreach (UIResourceInfoPanel resourcePanel in costResourcePanels)
        {
            if (resourcePanel.resourceType == resourceValue.resourceType)
            {
                resourcePanel.resourceAmountText.color = Color.red;
            }
        }
    }

    public void ToggleSomethingNew(bool v)
    {
        somethingNew = v;
        newIcon.SetActive(v);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
		if (eventData.button == PointerEventData.InputButton.Left)
        {
		    if (buttonHandler.cityBuilderManager.SelectedCity.army != null && buttonHandler.cityBuilderManager.SelectedCity.army.defending)
            {
				ShakeCheck();
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't build now, enemy approaching");
                return;
            }

            if (unitBuildData != null)
			{
                if (unitBuildData.singleBuildType != SingleBuildType.None && !buttonHandler.cityBuilderManager.SelectedCity.singleBuildDict.ContainsKey(unitBuildData.singleBuildType))
                {
                    string building = Regex.Replace(unitBuildData.singleBuildType.ToString(), "((?<=[a-z])[A-Z]|[A-Z](?=[a-z]))", " $1");
					ShakeCheck();
					UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, building + " required");
			        return;
		        }
                else if (unitBuildData.singleBuildType != SingleBuildType.None && buttonHandler.cityBuilderManager.world.GetCityDevelopment(buttonHandler.cityBuilderManager.SelectedCity.singleBuildDict[unitBuildData.singleBuildType]).isTraining)
                {
					string building = Regex.Replace(unitBuildData.singleBuildType.ToString(), "((?<=[a-z])[A-Z]|[A-Z](?=[a-z]))", " $1");
					ShakeCheck();
					UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, building + " currently training");
			        return;
		        }
                else if (unitBuildData.inMilitary)
                {
                    Army army;

                    if (unitBuildData.transportationType == TransportationType.Sea)
                        army = buttonHandler.cityBuilderManager.SelectedCity.navy;
                    else if (unitBuildData.transportationType == TransportationType.Air)
                        army = buttonHandler.cityBuilderManager.SelectedCity.airForce;
                    else
                        army = buttonHandler.cityBuilderManager.SelectedCity.army;

					if (army.IsGone())
                    {
					    string building = Regex.Replace(unitBuildData.singleBuildType.ToString(), "((?<=[a-z])[A-Z]|[A-Z](?=[a-z]))", " $1");
					    ShakeCheck();
					    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, building + " currently deployed");
			            return;
		            }
                    else if (army.isFull)
                    {
					    string building = Regex.Replace(unitBuildData.singleBuildType.ToString(), "((?<=[a-z])[A-Z]|[A-Z](?=[a-z]))", " $1");
					    ShakeCheck();
					    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, building + " full");
			            return;
		            }
                }
                
            }
        
            if (cantAfford && !buttonHandler.isQueueing)
            {
                ShakeCheck();
                UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't afford");
                return;
            }

            isFlashing = false;

            if (isUnitPanel)
            {
                buttonHandler.PrepareUnitBuild(unitBuildData);
                buttonHandler.HandleUnitButtonClick();
            }
            else
            {
                buttonHandler.cityBuilderManager.world.TutorialCheck("Building " + buildData.improvementName);
                buttonHandler.PrepareBuild(buildData);
                buttonHandler.HandleButtonClick();
            }
        }
    }

    private void ShakeCheck()
    {
        if (!shaking)
            StartCoroutine(Shake());
    }

    private IEnumerator Shake()
    {
        Vector3 initialPos = transform.localPosition;
        float elapsedTime = 0f;
        float duration = 0.2f;
        shaking = true;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            transform.localPosition = initialPos + (Random.insideUnitSphere * 10f);
            yield return null;
        }

        shaking = false;
        transform.localPosition = initialPos;
    }
}
