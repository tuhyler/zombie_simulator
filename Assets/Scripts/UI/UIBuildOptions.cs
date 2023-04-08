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
    public ImprovementDataSO BuildData { get { return buildData; } }

    [SerializeField]
    private UnitBuildDataSO unitBuildData;
    public UnitBuildDataSO UnitBuildData { get { return unitBuildData; } }

    private UIBuilderHandler buttonHandler;

    [SerializeField]
    private TMP_Text objectName, objectLevel, producesTitle;

    [SerializeField]
    private Image objectImage;

    [SerializeField]
    private GameObject resourceInfoPanel;

    [SerializeField]
    private Transform resourceProducedHolder, resourceCostHolder;

    [SerializeField]
    private RectTransform resourceProduceAllHolder, imageLine;

    [SerializeField]
    private VerticalLayoutGroup resourceProduceLayout;

    private List<Transform> produceConsumesHolders = new();
    private TMP_Text description;

    private bool isUnitPanel, cannotAfford, isShowing;

    //for checking if city can afford resource
    private List<UIResourceInfoPanel> costResourcePanels = new();
    //private bool ;

    private void Awake()
    {
        buttonHandler = GetComponentInParent<UIBuilderHandler>();

        foreach (Transform selection in resourceProducedHolder)
        {
            if (selection.TryGetComponent(out TMP_Text text))
            {
                description = text;
                description.gameObject.SetActive(false);
            }
            else
            {
                produceConsumesHolders.Add(selection);
            }
        }

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
        List<List<ResourceValue>> objectConsumed = new();
        List<ResourceValue> objectCost;

        if (isUnitPanel)
        {
            objectName.text = unitBuildData.unitName;
            objectLevel.text = "Level " + unitBuildData.unitLevel;
            objectImage.sprite = unitBuildData.image;
            objectCost = unitBuildData.unitCost;
            objectProduced = new();
            objectDescription = unitBuildData.unitDescription;
        }
        else
        {
            objectName.text = buildData.improvementName;
            objectLevel.text = "Level " + buildData.improvementLevel;
            objectImage.sprite = buildData.image;
            objectCost = buildData.improvementCost;
            objectProduced = buildData.producedResources;
            objectConsumed.Add(buildData.consumedResources);
            objectConsumed.Add(buildData.consumedResources1);
            objectConsumed.Add(buildData.consumedResources2);
            objectConsumed.Add(buildData.consumedResources3);
            objectConsumed.Add(buildData.consumedResources4);
            workEthicChange = buildData.workEthicChange;
            objectDescription = buildData.improvementDescription;
        }

        //cost info
        GenerateResourceInfo(resourceCostHolder, objectCost, true);

        //producer and consumed info
        for (int i = 0; i < produceConsumesHolders.Count; i++) //turning them all off initially
            produceConsumesHolders[i].gameObject.SetActive(false);

        int producedCount = objectProduced.Count;
        int maxCount = 0;

        for (int i = 0; i < objectProduced.Count; i++)
        {
            produceConsumesHolders[i].gameObject.SetActive(true);
            GenerateProduceInfo(produceConsumesHolders[i], objectProduced[i], objectConsumed[i]);

            if (maxCount < objectConsumed[i].Count)
                maxCount = objectConsumed[i].Count;
        }

        int resourcePanelSize = 60;
        int produceHolderWidth = 160;
        int produceContentsWidth = 240;
        int produceContentsHeight = 80;
        int produceLayoutPadding = -10;
        int imageLineWidth = 210;

        if (producedCount == 0)
        {
            description.gameObject.SetActive(true);
            
            if (isUnitPanel)
            {
                description.text = objectDescription;
                producesTitle.text = "Unit Info";
            }
            else
            {
                if (workEthicChange > 0)
                    description.text = "Work Ethic +" + Mathf.RoundToInt(workEthicChange * 100) + "%";
                else
                    description.text = objectDescription;
                producesTitle.text = "Produces";
            }

            //producesTitle.horizontalAlignment = HorizontalAlignmentOptions.Center;
            produceHolderWidth = 235;
        }

        //adjusting height of panel
        if (producedCount > 1)
        {
            int shift = resourcePanelSize * (producedCount - 1);
            produceContentsHeight += shift;
            produceLayoutPadding += shift;
        }

        //adjusting width of panel
        if (maxCount > 1)
        {
            int shift = resourcePanelSize * (maxCount - 1);
            produceHolderWidth += shift;
        }
        if (maxCount > 2)
        {
            int shift = resourcePanelSize * (maxCount - 2);
            produceContentsWidth += shift;
            imageLineWidth += shift;
        }

        resourceProducedHolder.GetComponent<RectTransform>().sizeDelta = new Vector2(produceHolderWidth, 60);
        resourceProduceAllHolder.sizeDelta = new Vector2(produceContentsWidth, produceContentsHeight);
        resourceProduceLayout.padding.bottom = produceLayoutPadding;
        imageLine.sizeDelta = new Vector2(imageLineWidth, 4);
    }

    private void GenerateResourceInfo(Transform transform, List<ResourceValue> resources, bool cost)
    {
        foreach (ResourceValue value in resources)
        {
            GameObject panel = Instantiate(resourceInfoPanel);
            panel.transform.SetParent(transform, false);
            UIResourceInfoPanel uiResourceCostPanel = panel.GetComponent<UIResourceInfoPanel>();
            uiResourceCostPanel.transform.SetParent(transform, false);

            uiResourceCostPanel.resourceAmount.text = value.resourceAmount.ToString();
            uiResourceCostPanel.resourceImage.sprite = ResourceHolder.Instance.GetIcon(value.resourceType);
            uiResourceCostPanel.resourceType = value.resourceType;

            if (cost)
                costResourcePanels.Add(uiResourceCostPanel);
        }
    }

    private void GenerateProduceInfo(Transform transform, ResourceValue producedResource, List<ResourceValue> consumedResources)
    {
        foreach (Transform selection in transform)
        {
            if (selection.TryGetComponent(out UIResourceInfoPanel uiResourceInfoPanel))
            {
                uiResourceInfoPanel.resourceAmount.text = producedResource.resourceAmount.ToString();
                uiResourceInfoPanel.resourceImage.sprite = ResourceHolder.Instance.GetIcon(producedResource.resourceType);
                uiResourceInfoPanel.resourceType = producedResource.resourceType;
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
        }

        GenerateResourceInfo(transform, consumedResources, false);
    }

    public void SetResourceTextToDefault()
    {
        cannotAfford = false;
        foreach (UIResourceInfoPanel resourcePanel in costResourcePanels)
        {
            resourcePanel.resourceAmount.color = Color.white;
        }
    }

    public void SetResourceTextToRed(ResourceValue resourceValue)
    {
        cannotAfford = true;
        foreach (UIResourceInfoPanel resourcePanel in costResourcePanels)
        {
            if (resourcePanel.resourceType == resourceValue.resourceType)
            {
                resourcePanel.resourceAmount.color = Color.red;
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (cannotAfford && !buttonHandler.isQueueing)
        {
            StartCoroutine(Shake());
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 10f; //z must be more than 0, else just gives camera position
            Vector3 mouseLoc = Camera.main.ScreenToWorldPoint(mousePos);

            InfoPopUpHandler.WarningMessage().Create(mouseLoc, "Can't afford");
            return;
        }

        if (isUnitPanel)
        {
            buttonHandler.PrepareUnitBuild(unitBuildData);
            buttonHandler.HandleUnitButtonClick();
        }
        else
        {
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
