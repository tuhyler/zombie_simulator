using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class UIWonderOptions : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private WonderDataSO buildData;
    public WonderDataSO BuildData { get { return buildData; } }

    private UIWonderHandler buttonHandler;

    [SerializeField]
    private TMP_Text objectName, objectDesc;

    [SerializeField]
    private Image objectImage;

    [SerializeField]
    private GameObject resourceInfoPanel;

    [SerializeField]
    private RectTransform resourceCostAllHolder, resourceCostHolder, imageLine;

    [SerializeField]
    private GridLayoutGroup resourceCostGrid;

    [SerializeField]
    private TMP_Text description;

    private bool isShowing;

    private void Awake()
    {
        buttonHandler = GetComponentInParent<UIWonderHandler>();
        PopulateSelectionPanel();
    }

    public void ToggleVisibility(bool v)
    {
        if (isShowing == v)
            return;

        isShowing = v;
        gameObject.SetActive(v);
    }

    //creating the menu card for each buildable object, showing name, function, cost, etc. 
    private void PopulateSelectionPanel()
    {
        float workEthicChange = 0;
        string objectDescription = "";
        List<ResourceValue> objectCost;

        objectName.text = buildData.wonderName;
        objectDesc.text = Regex.Replace(buildData.wonderEra.ToString(), "((?<=[a-z])[A-Z]|[A-Z](?=[a-z]))", " $1") + " Wonder";
        objectImage.sprite = buildData.image;
        objectCost = new(buildData.wonderCost);
        workEthicChange = buildData.workEthicChange;
        objectDescription = buildData.wonderDecription;

        objectCost.Add(MakeResourceValue(ResourceType.Gold, buildData.workersNeeded * buildData.workerCost * 100));
        objectCost.Add(MakeResourceValue(ResourceType.Labor, buildData.workersNeeded));

        //cost info
        GenerateResourceInfo(resourceCostHolder, objectCost);

        int maxCount = Mathf.Min(objectCost.Count, 5);

        int resourcePanelSize = 90;
        int costHolderWidth = 300;
        int costHolderHeight = 110;
        int imageLineWidth = 300;

        description.gameObject.SetActive(true);
        if (workEthicChange > 0)
            description.text = "Work Ethic +" + Mathf.RoundToInt(workEthicChange * 100) + "%";
        else
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

            uiResourceCostPanel.resourceAmount.text = value.resourceAmount.ToString();
            uiResourceCostPanel.resourceImage.sprite = ResourceHolder.Instance.GetIcon(value.resourceType);
            uiResourceCostPanel.resourceType = value.resourceType;
        }
    }

    private ResourceValue MakeResourceValue(ResourceType type, int amount)
    {
        ResourceValue value;
        value.resourceType = type;
        value.resourceAmount = amount;

        return value;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        buttonHandler.PrepareBuild(buildData);
        buttonHandler.HandleButtonClick();
    }
}
