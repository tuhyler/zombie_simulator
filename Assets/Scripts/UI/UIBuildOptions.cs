using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

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
    private TMP_Text objectName, objectLevel, producesTitle, health, speed, strength, description, descriptionTitle;

    [SerializeField]
    private Image objectImage, strengthImage;

    [SerializeField]
    private Sprite inventorySprite, rocksNormal, rocksLuxury, rocksChemical;

    [SerializeField]
    private GameObject resourceInfoPanel, productionPanel, descriptionPanel;

    [SerializeField]
    private Transform resourceProducedHolder, resourceCostHolder, unitDescription;

    [SerializeField]
    private RectTransform /*resourceProduceAllHolder, */allContents, imageLine;

    //[SerializeField]
    //private VerticalLayoutGroup resourceProduceLayout;

    private List<Transform> produceConsumesHolders = new();

    private bool isUnitPanel, cannotAfford, isShowing;

    [HideInInspector]
    public bool needsBarracks, fullBarracks, travelingBarracks, trainingBarracks, waterMax, isFlashing;
    //for checking if city can afford resource
    private List<UIResourceInfoPanel> costResourcePanels = new();
    //private bool ;

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
        float workEthicChange = 0;
        string objectDescription = "";
        List<ResourceValue> objectProduced;
        List<int> producedResourceTime;
        List<List<ResourceValue>> objectConsumed = new();
        List<ResourceValue> objectCost;
        bool arrowBuffer = false;

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
            objectLevel.text = "Level " + unitBuildData.unitLevel + " " + unitBuildData.unitType.ToString();
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
            if (buildData.improvementDisplayName == buildData.improvementName)
                objectLevel.text = "Level " + buildData.improvementLevel;
            else
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
            workEthicChange = buildData.workEthicChange;
            objectDescription = buildData.improvementDescription;

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

        if (objectDescription.Length > 0)
        {
            descriptionPanel.SetActive(true);

            if (!isUnitPanel && objectDescription.Length < 23)
                allContentsHeight += 85;
            else
				allContentsHeight += 120;

			if (isUnitPanel)
            {
                unitDescription.gameObject.SetActive(true);
                description.text = objectDescription;
                producesTitle.text = "Cost per Growth Cycle";
                descriptionTitle.text = "Unit Info";
            }
            else
            {
                if (workEthicChange > 0)
                    description.text = "Work Ethic +" + Mathf.RoundToInt(workEthicChange * 100) + "%";
                else
                    description.text = objectDescription;
                producesTitle.text = "Produces";
                descriptionTitle.text = "Additional Info";
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
            uiResourceCostPanel.resourceAmountText.text = value.resourceAmount.ToString();
            uiResourceCostPanel.resourceImage.sprite = ResourceHolder.Instance.GetIcon(value.resourceType);
            uiResourceCostPanel.resourceType = value.resourceType;

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
                    uiResourceInfoPanel.resourceAmountText.text = producedResource.resourceAmount.ToString();
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
                    }
                    else
					    uiResourceInfoPanel.resourceImage.sprite = ResourceHolder.Instance.GetIcon(producedResource.resourceType);
                    uiResourceInfoPanel.resourceType = producedResource.resourceType;
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
        cannotAfford = false;
        foreach (UIResourceInfoPanel resourcePanel in costResourcePanels)
        {
            resourcePanel.resourceAmountText.color = Color.white;
        }
    }

    public void SetResourceTextToRed(ResourceValue resourceValue)
    {
        cannotAfford = true;
        foreach (UIResourceInfoPanel resourcePanel in costResourcePanels)
        {
            if (resourcePanel.resourceType == resourceValue.resourceType)
            {
                resourcePanel.resourceAmountText.color = Color.red;
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (unitBuildData != null)
        {
            if (unitBuildData.transportationType == TransportationType.Sea)
            {
                if (trainingBarracks)
                {
					StartCoroutine(Shake());
					UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Currently training");
					return;
				}
            }
            else
            {
                if (unitBuildData.baseAttackStrength > 0)
                {
                    if (needsBarracks)
                    {
			            StartCoroutine(Shake());
			            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Barracks required");
			            return;
		            }
                    else if (travelingBarracks)
                    {
			            StartCoroutine(Shake());
			            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Barracks currently deployed");
			            return;
		            }
                    else if (fullBarracks)
                    {
			            StartCoroutine(Shake());
			            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Barracks full");
			            return;
		            }
                    else if (trainingBarracks)
                    {
			            StartCoroutine(Shake());
			            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Currently training");
			            return;
		            }
                }
            }
        }
        
        if (cannotAfford && !buttonHandler.isQueueing)
        {
            StartCoroutine(Shake());
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
            if (buildData.housingIncrease > 0 && waterMax)
            {
				StartCoroutine(Shake());
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Reached water limit. Build well or have river in boundaries");
				return;
			}

            buttonHandler.cityBuilderManager.world.TutorialCheck("Building Something");
            buttonHandler.PrepareBuild(buildData);
            buttonHandler.HandleButtonClick();
        }
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
}
