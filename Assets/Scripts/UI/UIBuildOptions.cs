using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIBuildOptions : MonoBehaviour, IPointerClickHandler //use this to pass buildData to the button handler, (or just turn into a button?)
{
    [SerializeField]
    private ImprovementDataSO buildData;
    public ImprovementDataSO BuildData { get { return buildData; } }

    [SerializeField]
    private UnitBuildDataSO unitBuildData;
    public UnitBuildDataSO UnitBuildData { get { return unitBuildData; } }

    private UIBuilderHandler buttonHandler;

    [SerializeField]
    private CanvasGroup canvasGroup; //handles all aspects of a group of UI elements together, instead of individually in Unity

    [SerializeField]
    private TMP_Text objectName;

    [SerializeField]
    private Image objectImage;

    [SerializeField]
    private GameObject resourceInfoPanel;

    [SerializeField]
    private Transform resourceProducedHolder, resourceConsumedHolder, resourceCostHolder, resourceProducedContents, resourceConsumedContents, resourceCostContents;

    private bool isUnitPanel, produced = true, consumed = true;

    private void Awake()
    {
        buttonHandler = GetComponentInParent<UIBuilderHandler>();
        if (!buttonHandler.showResourceProduced)
        {
            resourceProducedContents.gameObject.SetActive(false);
            produced = false;
        }
        if (!buttonHandler.showResourceConsumed)
        {
            resourceConsumedContents.gameObject.SetActive(false);
            consumed = false;
        }
        canvasGroup = GetComponent<CanvasGroup>();
        if (unitBuildData != null)
            isUnitPanel = true;
        PopulateSelectionPanel();
    }

    public void ToggleInteractable(bool v)
    {
        canvasGroup.interactable = v;
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
        List<ResourceValue> objectProduced;
        List<ResourceValue> objectConsumed;
        List<ResourceValue> objectCost;
        List<ResourceIndividualSO> resourceInfo = buttonHandler.resources.allStorableResources.Concat(buttonHandler.resources.allWorldResources).ToList();

        if (isUnitPanel)
        {
            objectName.text = unitBuildData.unitName;
            objectImage.sprite = unitBuildData.image;
            objectCost = unitBuildData.unitCost;
            objectProduced = new();
            objectConsumed = new();
        }
        else
        {
            objectName.text = buildData.improvementName;
            objectImage.sprite = buildData.image;
            objectCost = buildData.improvementCost;
            objectProduced = buildData.producedResources;
            objectConsumed = buildData.consumedResources;
            workEthicChange = buildData.workEthicChange;
        }

        GenerateResourceInfoPanels(resourceCostHolder, objectCost, resourceInfo);
        if (produced)
            GenerateResourceInfoPanels(resourceProducedHolder, objectProduced, resourceInfo, workEthicChange);
        if (consumed)
            GenerateResourceInfoPanels(resourceConsumedHolder, objectConsumed, resourceInfo);
    }

    private void GenerateResourceInfoPanels(Transform transform, List<ResourceValue> resources, List<ResourceIndividualSO> resourceInfo, float workEthicChange = 0)
    {
        if (workEthicChange != 0)
        {
            GameObject panel = Instantiate(resourceInfoPanel);
            panel.transform.SetParent(transform, false);
            UIResourceInfoPanel uiResourceInfoPanel = panel.GetComponent<UIResourceInfoPanel>();

            uiResourceInfoPanel.resourceAmount.color = Color.black;//new Color32(28, 72, 140, 255); //color32 uses bytes
            uiResourceInfoPanel.resourceAmount.alignment = TextAlignmentOptions.Midline;
            uiResourceInfoPanel.resourceTransform.anchoredPosition3D += new Vector3(0,20f,0);
            uiResourceInfoPanel.resourceTransform.sizeDelta = new Vector2(140, 0);
            uiResourceInfoPanel.resourceTransform.SetParent(uiResourceInfoPanel.allContents, false);
            uiResourceInfoPanel.backgroundCanvas.gameObject.SetActive(false);
            uiResourceInfoPanel.resourceAmount.text = "Work Ethic: +" + Mathf.RoundToInt(workEthicChange * 100) + "%";
        }
        
        foreach (ResourceValue value in resources)
        {
            GameObject panel = Instantiate(resourceInfoPanel); 
            panel.transform.SetParent(transform, false);
            UIResourceInfoPanel uiResourceCostPanel = panel.GetComponent<UIResourceInfoPanel>();

            int index = resourceInfo.FindIndex(a => a.resourceType == value.resourceType);

            uiResourceCostPanel.resourceAmount.text = value.resourceAmount.ToString();
            uiResourceCostPanel.resourceImage.sprite = resourceInfo[index].resourceIcon;
        }

        if (resources.Count == 0 && workEthicChange == 0)
        {
            //Code is repeated here because it won't work in separate method for some reason
            GameObject panel = Instantiate(resourceInfoPanel);
            panel.transform.SetParent(transform, false);
            UIResourceInfoPanel uiResourceInfoPanel = panel.GetComponent<UIResourceInfoPanel>();

            uiResourceInfoPanel.resourceAmount.color = Color.black;// new Color32(28, 72, 140, 255); //color32 uses bytes
            uiResourceInfoPanel.resourceTransform.sizeDelta = new Vector2(70, 20);
            uiResourceInfoPanel.resourceTransform.SetParent(uiResourceInfoPanel.allContents, false);
            uiResourceInfoPanel.backgroundCanvas.gameObject.SetActive(false);
            uiResourceInfoPanel.resourceAmount.text = "None";
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
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
}
