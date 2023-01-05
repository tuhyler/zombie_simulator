using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIWonderOptions : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private WonderDataSO buildData;
    public WonderDataSO BuildData { get { return buildData; } }

    private UIWonderHandler buttonHandler;

    [SerializeField]
    private CanvasGroup canvasGroup; //handles all aspects of a group of UI elements together, instead of individually in Unity

    [SerializeField]
    private TMP_Text objectName;

    [SerializeField]
    private Image objectImage;

    [SerializeField]
    private GameObject resourceInfoPanel;

    [SerializeField]
    private Transform resourceCostHolder, resourceCostContents;

    private bool cannotAfford, isShowing;//, produced = true, consumed = true;

    //for checking if city can afford resource
    private List<UIResourceInfoPanel> costResourcePanels = new();
    //private bool ;

    private void Awake()
    {
        buttonHandler = GetComponentInParent<UIWonderHandler>();
        //if (!buttonHandler.showResourceProduced)
        //{
        //    resourceProducedContents.gameObject.SetActive(false);
        //    //produced = false;
        //}
        //if (!buttonHandler.showResourceConsumed)
        //{
        //    resourceConsumedContents.gameObject.SetActive(false);
        //    //consumed = false;
        //}
        canvasGroup = GetComponent<CanvasGroup>();
        PopulateSelectionPanel();

        //buildData.prefabRenderers = buildData.wonderPrefab.GetComponentsInChildren<MeshRenderer>();
    }

    public void ToggleInteractable(bool v)
    {
        canvasGroup.interactable = v;

        foreach (UIResourceInfoPanel resourceInfoPanel in costResourcePanels)
        {
            resourceInfoPanel.backgroundCanvas.interactable = v;
        }
    }

    public void ToggleVisibility(bool v)
    {
        if (isShowing == v)
            return;

        isShowing = v;
        gameObject.SetActive(v);
    }

    //public void OnPointerClick()
    //{
    //    buttonHandler.PrepareBuild(buildData);
    //}

    //creating the menu card for each buildable object, showing name, function, cost, etc. 
    private void PopulateSelectionPanel()
    {
        float workEthicChange = 0;
        string objectDescription = buildData.wonderDecription;
        List<ResourceValue> objectCost;
        List<ResourceIndividualSO> resourceInfo = ResourceHolder.Instance.allStorableResources.Concat(ResourceHolder.Instance.allWorldResources).ToList();

        objectName.text = buildData.wonderName;
        objectImage.sprite = buildData.image;
        objectCost = buildData.wonderCost;
        workEthicChange = buildData.workEthicChange;

        //cost info
        GenerateResourceInfoPanels(resourceCostHolder, objectDescription, objectCost, resourceInfo, workEthicChange, true);
    }

    private void GenerateResourceInfoPanels(Transform transform, string description, List<ResourceValue> resources,
        List<ResourceIndividualSO> resourceInfo, float workEthicChange = 0, bool isCost = false)
    {
        if (workEthicChange != 0 || description.Length > 0)
        {
            GameObject panel = Instantiate(resourceInfoPanel);
            panel.transform.SetParent(transform, false);
            UIResourceInfoPanel uiResourceInfoPanel = panel.GetComponent<UIResourceInfoPanel>();
            //UIResourceInfoPanel uiResourceInfoPanel = buttonHandler.GetFromResourceInfoPanelPool();
            uiResourceInfoPanel.transform.SetParent(transform, false);

            //uiResourceInfoPanel.resourceAmount.color = Color.black;//new Color32(28, 72, 140, 255); //color32 uses bytes
            uiResourceInfoPanel.resourceAmount.alignment = TextAlignmentOptions.Midline;
            uiResourceInfoPanel.resourceTransform.anchoredPosition3D += new Vector3(0, 20f, 0);
            uiResourceInfoPanel.resourceTransform.sizeDelta = new Vector2(230, 0);
            uiResourceInfoPanel.resourceTransform.SetParent(uiResourceInfoPanel.allContents, false);
            uiResourceInfoPanel.backgroundCanvas.gameObject.SetActive(false);

            if (workEthicChange != 0)
                description = "Work Ethic +" + Mathf.RoundToInt(workEthicChange * 100) + "%";

            uiResourceInfoPanel.resourceAmount.text = description;
        }

        foreach (ResourceValue value in resources)
        {
            GameObject panel = Instantiate(resourceInfoPanel);
            panel.transform.SetParent(transform, false);
            UIResourceInfoPanel uiResourceCostPanel = panel.GetComponent<UIResourceInfoPanel>();
            //UIResourceInfoPanel uiResourceCostPanel = buttonHandler.GetFromResourceInfoPanelPool();
            uiResourceCostPanel.transform.SetParent(transform, false);

            int index = resourceInfo.FindIndex(a => a.resourceType == value.resourceType);

            uiResourceCostPanel.resourceAmount.text = value.resourceAmount.ToString();
            uiResourceCostPanel.resourceImage.sprite = resourceInfo[index].resourceIcon;
            uiResourceCostPanel.resourceType = value.resourceType;
            if (isCost)
            {
                costResourcePanels.Add(uiResourceCostPanel);
            }
        }

        //if (resources.Count == 0 && workEthicChange == 0 && description.Length == 0)
        //{
        //    //Code is repeated here because it won't work in separate method for some reason
        //    GameObject panel = Instantiate(resourceInfoPanel);
        //    panel.transform.SetParent(transform, false);
        //    UIResourceInfoPanel uiResourceInfoPanel = panel.GetComponent<UIResourceInfoPanel>();
        //    //UIResourceInfoPanel uiResourceInfoPanel = buttonHandler.GetFromResourceInfoPanelPool();
        //    uiResourceInfoPanel.transform.SetParent(transform, false);

        //    uiResourceInfoPanel.resourceAmount.color = Color.black;// new Color32(28, 72, 140, 255); //color32 uses bytes
        //    uiResourceInfoPanel.resourceTransform.sizeDelta = new Vector2(70, 20);
        //    uiResourceInfoPanel.resourceTransform.SetParent(uiResourceInfoPanel.allContents, false);
        //    uiResourceInfoPanel.backgroundCanvas.gameObject.SetActive(false);
        //    uiResourceInfoPanel.resourceAmount.text = "None";
        //}
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!canvasGroup.interactable)
            return;

        buttonHandler.PrepareBuild(buildData);
        buttonHandler.HandleButtonClick();
    }
}
