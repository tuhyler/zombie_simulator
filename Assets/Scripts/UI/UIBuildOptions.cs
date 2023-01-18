using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.VersionControl;
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

    private bool isUnitPanel, cannotAfford, isShowing;//, produced = true, consumed = true;

    private Vector3 initialPos; //for shaking 

    //for checking if city can afford resource
    private List<UIResourceInfoPanel> costResourcePanels = new();
    //private bool ;

    private void Awake()
    {
        initialPos = transform.position;

        buttonHandler = GetComponentInParent<UIBuilderHandler>();
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
        if (unitBuildData != null)
            isUnitPanel = true;
        PopulateSelectionPanel();

        //if (buildData != null)
        //    buildData.prefabRenderers = buildData.prefab.GetComponentsInChildren<MeshRenderer>();
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
        List<ResourceValue> objectConsumed;
        List<ResourceValue> objectCost;
        List<ResourceIndividualSO> resourceInfo = ResourceHolder.Instance.allStorableResources.Concat(ResourceHolder.Instance.allWorldResources).ToList();

        if (isUnitPanel)
        {
            objectName.text = unitBuildData.unitName;
            objectImage.sprite = unitBuildData.image;
            objectCost = unitBuildData.unitCost;
            objectProduced = new();
            objectConsumed = unitBuildData.consumedResources;
            objectDescription = unitBuildData.unitDescription;
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

        //cost info
        GenerateResourceInfoPanels(resourceCostHolder, "", objectCost, resourceInfo, 0, true);
        //producer info
        GenerateResourceInfoPanels(resourceProducedHolder, objectDescription, objectProduced, resourceInfo, workEthicChange);
        //consumed info
        GenerateResourceInfoPanels(resourceConsumedHolder, "", objectConsumed, resourceInfo);
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

            uiResourceInfoPanel.resourceAmount.color = Color.black;//new Color32(28, 72, 140, 255); //color32 uses bytes
            uiResourceInfoPanel.resourceAmount.alignment = TextAlignmentOptions.Midline;
            uiResourceInfoPanel.resourceTransform.anchoredPosition3D += new Vector3(0,20f,0);
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

        if (resources.Count == 0 && workEthicChange == 0 && description.Length == 0)
        {
            //Code is repeated here because it won't work in separate method for some reason
            GameObject panel = Instantiate(resourceInfoPanel);
            panel.transform.SetParent(transform, false);
            UIResourceInfoPanel uiResourceInfoPanel = panel.GetComponent<UIResourceInfoPanel>();
            //UIResourceInfoPanel uiResourceInfoPanel = buttonHandler.GetFromResourceInfoPanelPool();
            uiResourceInfoPanel.transform.SetParent(transform, false);

            uiResourceInfoPanel.resourceAmount.color = Color.black;// new Color32(28, 72, 140, 255); //color32 uses bytes
            uiResourceInfoPanel.resourceTransform.sizeDelta = new Vector2(70, 20);
            uiResourceInfoPanel.resourceTransform.SetParent(uiResourceInfoPanel.allContents, false);
            uiResourceInfoPanel.backgroundCanvas.gameObject.SetActive(false);
            uiResourceInfoPanel.resourceAmount.text = "None";
        }
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
        if (!canvasGroup.interactable)
            return;

        if (cannotAfford && !buttonHandler.isQueueing)
        {
            StartCoroutine(Shake());
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 10f; //z must be more than 0, else just gives camera position
            Vector3 mouseLoc = Camera.main.ScreenToWorldPoint(mousePos);

            InfoPopUpHandler.Create(mouseLoc, "Can't afford");
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
        Vector3 initialPos = transform.position;
        float elapsedTime = 0f;
        float duration = 0.2f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            transform.position = initialPos + (Random.insideUnitSphere * .01f);
            yield return null;
        }

        transform.position = initialPos;
    }
}
