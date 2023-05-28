using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
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
    private TMP_Text objectName;

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
    [SerializeField]
    private TMP_Text description;

    private bool cannotAfford, isShowing;//, produced = true, consumed = true;

    //for checking if city can afford resource
    private List<UIResourceInfoPanel> costResourcePanels = new();
    //private bool ;

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
        objectImage.sprite = buildData.image;
        objectCost = buildData.wonderCost;
        workEthicChange = buildData.workEthicChange;
        objectDescription = buildData.wonderDecription;

        //cost info
        GenerateResourceInfo(resourceCostHolder, objectCost, true, 60);

        //producer and consumed info
        for (int i = 0; i < produceConsumesHolders.Count; i++) //turning them all off initially
            produceConsumesHolders[i].gameObject.SetActive(false);

        int maxCount = objectCost.Count - 1;

        int resourcePanelSize = 90;
        int produceHolderWidth = 330;
        int produceHolderHeight = 80;
        int produceContentsWidth = 350;
        int produceContentsHeight = 110;
        int produceLayoutPadding = -10;
        int imageLineWidth = 300;

        description.gameObject.SetActive(true);
        if (workEthicChange > 0)
            description.text = "Work Ethic +" + Mathf.RoundToInt(workEthicChange * 100) + "%";
        else
            description.text = objectDescription;

        //adjusting width of panel
        if (maxCount > 2)
        {
            int shift = resourcePanelSize * (maxCount - 2);
            produceContentsWidth += shift;
            imageLineWidth += shift;
        }

        resourceProducedHolder.GetComponent<RectTransform>().sizeDelta = new Vector2(produceHolderWidth, produceHolderHeight);
        resourceProduceAllHolder.sizeDelta = new Vector2(produceContentsWidth, produceContentsHeight);
        resourceProduceLayout.padding.bottom = produceLayoutPadding;
        imageLine.sizeDelta = new Vector2(imageLineWidth, 4);
    }

    private void GenerateResourceInfo(Transform transform, List<ResourceValue> resources, bool cost, int producedResourceTime)
    {
        foreach (ResourceValue value in resources)
        {
            GameObject panel = Instantiate(resourceInfoPanel);
            panel.transform.SetParent(transform, false);
            UIResourceInfoPanel uiResourceCostPanel = panel.GetComponent<UIResourceInfoPanel>();
            uiResourceCostPanel.transform.SetParent(transform, false);

            uiResourceCostPanel.resourceAmount.text = Mathf.RoundToInt(value.resourceAmount * (60f / producedResourceTime)).ToString();
            uiResourceCostPanel.resourceImage.sprite = ResourceHolder.Instance.GetIcon(value.resourceType);
            uiResourceCostPanel.resourceType = value.resourceType;

            if (cost)
                costResourcePanels.Add(uiResourceCostPanel);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        buttonHandler.PrepareBuild(buildData);
        buttonHandler.HandleButtonClick();
    }
}
