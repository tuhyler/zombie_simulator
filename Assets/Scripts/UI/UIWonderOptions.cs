using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIWonderOptions : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private WonderDataSO buildData;
    public WonderDataSO BuildData { get { return buildData; } set { buildData = value; } }

    private UIWonderHandler buttonHandler;

    [SerializeField]
    private TMP_Text objectName, objectDesc;

    [SerializeField]
    private Image objectImage;

    [SerializeField]
    private GameObject resourceInfoPanel, newIcon;

    [SerializeField]
    private RectTransform resourceCostAllHolder, resourceCostHolder, percentCostHolder, imageLine;

    [SerializeField]
    private GridLayoutGroup resourceCostGrid;

    [SerializeField]
    private TMP_Text description;

    private bool isShowing;
    [HideInInspector]
    public bool somethingNew, locked;

    private void Awake()
    {
        //buttonHandler = GetComponentInParent<UIWonderHandler>();
        //PopulateSelectionPanel();
    }

    public void ToggleVisibility(bool v)
    {
        if (isShowing == v)
            return;

        isShowing = v;
        gameObject.SetActive(v);
    }

    //creating the menu card for each buildable object, showing name, function, cost, etc. 
    public void SetBuildOptionData(UIWonderHandler wonderHandler)
    {
        buttonHandler = wonderHandler;
        locked = true;
        PopulateSelectionPanel();
    }

    private void PopulateSelectionPanel()
    {
        string objectDescription = "";
        List<ResourceValue> objectCost;
        List<ResourceValue> percentCost = new();

        objectName.text = buildData.wonderDisplayName;
        objectDesc.text = Regex.Replace(buildData.wonderEra.ToString(), "((?<=[a-z])[A-Z]|[A-Z](?=[a-z]))", " $1") + " Wonder";
        objectImage.sprite = buildData.image;
        objectCost = new(buildData.wonderCost);
        objectDescription = buildData.wonderDescription;

        percentCost.Add(MakeResourceValue(ResourceType.Gold, buildData.workersNeeded * buildData.workerCost));
        percentCost.Add(MakeResourceValue(ResourceType.Labor, buildData.workersNeeded));
        percentCost.Add(MakeResourceValue(ResourceType.Time, buildData.buildTimePerPercent));

        //cost info
        GenerateResourceInfo(resourceCostHolder, objectCost);
        //cost per percent info
        GenerateResourceInfo(percentCostHolder, percentCost);

        int maxCount = Mathf.Min(objectCost.Count, 5);

        int resourcePanelSize = 90;
        int costHolderWidth = 300;
        int costHolderHeight = 110;
        int imageLineWidth = 300;

        description.gameObject.SetActive(true);
        description.text = objectDescription;

        resourceCostGrid.constraintCount = maxCount; 

        //adjusting width of panel
        if (maxCount > 3)
        {
            int shift = resourcePanelSize * (maxCount - 3);
            costHolderWidth += shift;
            imageLineWidth += shift - 40;
            costHolderHeight += Mathf.FloorToInt((objectCost.Count - 1) / 5) * resourcePanelSize;
        }

        resourceCostAllHolder.sizeDelta = new Vector2(costHolderWidth, costHolderHeight);
        imageLine.sizeDelta = new Vector2(imageLineWidth, 4);
    }

    private void GenerateResourceInfo(Transform transform, List<ResourceValue> resources)
    {        
        foreach (ResourceValue value in resources)
        {
            GameObject panel = Instantiate(resourceInfoPanel);
            panel.transform.SetParent(transform, false);
            UIResourceInfoPanel uiResourceCostPanel = panel.GetComponent<UIResourceInfoPanel>();
            uiResourceCostPanel.transform.SetParent(transform, false);

            uiResourceCostPanel.SetResourceAmount(value.resourceAmount);
            uiResourceCostPanel.resourceImage.sprite = ResourceHolder.Instance.GetIcon(value.resourceType);
            uiResourceCostPanel.SetResourceType(value.resourceType);
        }
    }

    private ResourceValue MakeResourceValue(ResourceType type, int amount)
    {
        ResourceValue value;
        value.resourceType = type;
        value.resourceAmount = amount;

        return value;
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
			UITooltipSystem.Hide();
			buttonHandler.world.cityBuilderManager.PlaySelectAudio();
            buttonHandler.PrepareBuild(buildData);
            buttonHandler.HandleButtonClick();
        }
    }
}
