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
        List<ResourceIndividualSO> resourceInfo = ResourceHolder.Instance.allStorableResources.Concat(ResourceHolder.Instance.allWorldResources).ToList();

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
            workEthicChange = buildData.workEthicChange;
        }

        //cost info
        //GenerateResourceInfoPanels(resourceCostHolder, "", objectCost, resourceInfo, 0, true);
        GenerateResourceInfo(resourceCostHolder, objectCost, resourceInfo, true);
        //producer info
        //GenerateResourceInfoPanels(resourceProducedHolder, objectDescription, objectProduced, resourceInfo, workEthicChange);
        for (int i = 0; i < produceConsumesHolders.Count; i++) //turning them all off initially
            produceConsumesHolders[i].gameObject.SetActive(false);

        int producedCount = objectProduced.Count;
        int maxCount = 0;

        for (int i = 0; i < objectProduced.Count; i++)
        {
            produceConsumesHolders[i].gameObject.SetActive(true);
            GenerateProduceInfo(produceConsumesHolders[i], objectProduced[i], objectConsumed[i], resourceInfo);

            if (maxCount < objectConsumed[i].Count)
                maxCount = objectConsumed[i].Count;
        }

        if (producedCount == 0)
        {
            if (isUnitPanel)
            {
                description.text = objectDescription;
                producesTitle.text = "Unit Info";
            }
            else
            {
                description.text = "Work Ethic +" + Mathf.RoundToInt(workEthicChange * 100) + "%";
                producesTitle.text = "Produces";
            }
        }

        if (producedCount <= 1)
        {
            SetBaseImageHeight();
        }

        if (maxCount <= 2)
        {
            producesTitle.text = "   Makes       Requires";
            SetBaseImageWidth();
        }

        //consumed info
        //GenerateResourceInfoPanels(resourceConsumedHolder, "", objectConsumed, resourceInfo);
    }

    private void GenerateResourceInfo(Transform transform, List<ResourceValue> resources, List<ResourceIndividualSO> resourceInfo, bool cost)
    {
        foreach (ResourceValue value in resources)
        {
            GameObject panel = Instantiate(resourceInfoPanel);
            panel.transform.SetParent(transform, false);
            UIResourceInfoPanel uiResourceCostPanel = panel.GetComponent<UIResourceInfoPanel>();
            uiResourceCostPanel.transform.SetParent(transform, false);

            int index = resourceInfo.FindIndex(a => a.resourceType == value.resourceType);

            uiResourceCostPanel.resourceAmount.text = value.resourceAmount.ToString();
            uiResourceCostPanel.resourceImage.sprite = resourceInfo[index].resourceIcon;
            uiResourceCostPanel.resourceType = value.resourceType;

            if (cost)
                costResourcePanels.Add(uiResourceCostPanel);
        }
    }

    private void GenerateProduceInfo(Transform transform, ResourceValue producedResource, List<ResourceValue> consumedResources, List<ResourceIndividualSO> resourceInfo)
    {
        foreach (Transform selection in transform)
        {
            if (selection.TryGetComponent(out UIResourceInfoPanel uiResourceInfoPanel))
            {
                int index = resourceInfo.FindIndex(a => a.resourceType == producedResource.resourceType);

                uiResourceInfoPanel.resourceAmount.text = producedResource.resourceAmount.ToString();
                uiResourceInfoPanel.resourceImage.sprite = resourceInfo[index].resourceIcon;
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

        GenerateResourceInfo(transform, consumedResources, resourceInfo, false);
    }

    private void SetBaseImageHeight()
    {

    }

    private void SetBaseImageWidth()
    {

    }

    //private void GenerateResourceInfoPanels(Transform transform, string description, List<ResourceValue> resources, 
    //    List<ResourceIndividualSO> resourceInfo, float workEthicChange = 0, bool isCost = false)
    //{
    //    if (workEthicChange != 0 || description.Length > 0)
    //    {
    //        GameObject panel = Instantiate(resourceInfoPanel);
    //        panel.transform.SetParent(transform, false);
    //        UIResourceInfoPanel uiResourceInfoPanel = panel.GetComponent<UIResourceInfoPanel>();
    //        //UIResourceInfoPanel uiResourceInfoPanel = buttonHandler.GetFromResourceInfoPanelPool();
    //        uiResourceInfoPanel.transform.SetParent(transform, false);

    //        uiResourceInfoPanel.resourceAmount.color = Color.black;//new Color32(28, 72, 140, 255); //color32 uses bytes
    //        uiResourceInfoPanel.resourceAmount.alignment = TextAlignmentOptions.Midline;
    //        uiResourceInfoPanel.resourceTransform.anchoredPosition3D += new Vector3(0,20f,0);
    //        uiResourceInfoPanel.resourceTransform.sizeDelta = new Vector2(230, 0);
    //        uiResourceInfoPanel.resourceTransform.SetParent(uiResourceInfoPanel.allContents, false);
    //        uiResourceInfoPanel.backgroundCanvas.gameObject.SetActive(false);

    //        if (workEthicChange != 0)
    //            description = "Work Ethic +" + Mathf.RoundToInt(workEthicChange * 100) + "%";

    //        uiResourceInfoPanel.resourceAmount.text = description;
    //    }

    //    foreach (ResourceValue value in resources)
    //    {
    //        GameObject panel = Instantiate(resourceInfoPanel);
    //        panel.transform.SetParent(transform, false);
    //        UIResourceInfoPanel uiResourceCostPanel = panel.GetComponent<UIResourceInfoPanel>();
    //        //UIResourceInfoPanel uiResourceCostPanel = buttonHandler.GetFromResourceInfoPanelPool();
    //        uiResourceCostPanel.transform.SetParent(transform, false);

    //        int index = resourceInfo.FindIndex(a => a.resourceType == value.resourceType);

    //        uiResourceCostPanel.resourceAmount.text = value.resourceAmount.ToString();
    //        uiResourceCostPanel.resourceImage.sprite = resourceInfo[index].resourceIcon;
    //        uiResourceCostPanel.resourceType = value.resourceType;
    //        if (isCost)
    //        {
    //            costResourcePanels.Add(uiResourceCostPanel);
    //        }
    //    }

    //    if (resources.Count == 0 && workEthicChange == 0 && description.Length == 0)
    //    {
    //        //Code is repeated here because it won't work in separate method for some reason
    //        GameObject panel = Instantiate(resourceInfoPanel);
    //        panel.transform.SetParent(transform, false);
    //        UIResourceInfoPanel uiResourceInfoPanel = panel.GetComponent<UIResourceInfoPanel>();
    //        //UIResourceInfoPanel uiResourceInfoPanel = buttonHandler.GetFromResourceInfoPanelPool();
    //        uiResourceInfoPanel.transform.SetParent(transform, false);

    //        uiResourceInfoPanel.resourceAmount.color = Color.black;// new Color32(28, 72, 140, 255); //color32 uses bytes
    //        uiResourceInfoPanel.resourceTransform.sizeDelta = new Vector2(70, 20);
    //        uiResourceInfoPanel.resourceTransform.SetParent(uiResourceInfoPanel.allContents, false);
    //        uiResourceInfoPanel.backgroundCanvas.gameObject.SetActive(false);
    //        uiResourceInfoPanel.resourceAmount.text = "None";
    //    }
    //}

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
