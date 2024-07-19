using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITradeCenter : MonoBehaviour/*, IGoldUpdateCheck*/
{
    [SerializeField]
    private MapWorld world;
    
    [SerializeField]
    private TMP_Text title, ownerName, increaseText, decreaseText, ecstaticText;

    [SerializeField]
    private Image happinessMeter, ownerImage;

    [SerializeField]
    private Sprite mad, neutral, happy, ecstatic;

    [SerializeField]
    private GameObject resourcePanel;
    private Queue<UITradeResource> buyPanelQueue = new();
	private Queue<UITradeResource> sellPanelQueue = new();
	private List<UITradeResource> buyPanelList = new();
	private List<UITradeResource> sellPanelList = new();

	[SerializeField]
    private Transform buyGrid, sellGrid;

    //private Dictionary<ResourceType, UITradeResource> buyDict = new();
    //private Dictionary<ResourceType, UITradeResource> sellDict = new();

    //private List<UITradeResource> activeBuyResources = new();
    //private List<UITradeResource> activeSellResources = new();
    [HideInInspector]
    public TradeCenter center;

    private float buyMultiple;

    [SerializeField] //for tweening
    private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus;
    private Vector3 originalLoc;

    //for updating colors
    //private int maxBuyCost;

    private void Awake()
    {
        gameObject.SetActive(false);
        originalLoc = allContents.anchoredPosition3D;

        GrowPanelsPool(15, true);
        GrowPanelsPool(15, false);

        //foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
        //{
        //    GameObject newResource = Instantiate(resourcePanel);
        //    newResource.transform.SetParent(buyGrid, false);

        //    UITradeResource newTradeResource = newResource.GetComponent<UITradeResource>();
        //    newTradeResource.resourceType = resource.resourceType;
        //    newTradeResource.resourceImage.sprite = resource.resourceIcon;
        //    buyDict[resource.resourceType] = newTradeResource;
        //    newResource.SetActive(false);

        //    GameObject newResource2 = Instantiate(resourcePanel);
        //    newResource2.transform.SetParent(sellGrid, false);

        //    UITradeResource newTradeResource2 = newResource2.GetComponent<UITradeResource>();
        //    newTradeResource2.resourceType = resource.resourceType;
        //    newTradeResource2.resourceImage.sprite = resource.resourceIcon;
        //    sellDict[resource.resourceType] = newTradeResource2;
        //    newResource2.SetActive(false);
        //}

		increaseText.outlineColor = new Color(0, .5f, 0);
		increaseText.outlineWidth = .1f;
		decreaseText.outlineColor = new Color(.3f, 0, 0);
		decreaseText.outlineWidth = .1f;
		ecstaticText.outlineColor = new Color(0, .4f, 0);
		ecstaticText.outlineWidth = .1f;
	}

    public void ToggleVisibility(bool v, TradeCenter center = null)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            this.center = center;
            //world.goldUpdateCheck = this;
            activeStatus = true;
            world.tcCanvas.gameObject.SetActive(true);
            gameObject.SetActive(v);
            ownerName.text = "Rapport with " + center.tcRep.tradeRepName;
            ownerImage.sprite = Resources.Load<Sprite>("MyConvoFaces/" + center.tcRep.buildDataSO.imageName);
            increaseText.text = "+" + center.tcRep.angryIncrease.ToString() + "%";
            decreaseText.text = "-" + center.tcRep.happyDiscount.ToString() + "%,";
            ecstaticText.text = "-" + center.tcRep.ecstaticDiscount.ToString() + "%";
            SetHappinessMeter(center.tcRep);

            allContents.anchoredPosition3D = originalLoc + new Vector3(-500f, 0, 0);
            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + 500f, 0.4f).setEaseOutBack();
        }
        else
        {
			UITooltipSystem.Hide();
			activeStatus = false;
            this.center = null;
            //world.goldUpdateCheck = null;
            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + -1000f, 0.2f).setOnComplete(SetActiveStatusFalse);
        }
    }

    //public void UpdateGold(int prevGold, int currentGold, bool pos)
    //{
    //    if (pos)
    //    {
    //        if (prevGold > maxBuyCost)
    //            return;
    //    }
    //    else
    //    {
    //        if (currentGold > maxBuyCost)
    //            return;
    //    }
       
    //    foreach (UITradeResource resource in activeBuyResources)
    //        resource.SetColor(world.CheckWorldGold(resource.resourceAmount) ? Color.white : Color.red);
    //}

    private void SetActiveStatusFalse()
    {
        //foreach (UITradeResource resource in activeBuyResources)
        //	resource.gameObject.SetActive(false);

        //foreach (UITradeResource resource in activeSellResources)
        //	resource.gameObject.SetActive(false);

        //activeBuyResources.Clear();
        //activeSellResources.Clear();
        //maxBuyCost = 0;

        HideAllPanels();
		gameObject.SetActive(false);
        world.tcCanvas.gameObject.SetActive(false);
    }

    public void CloseUITradeCenter()
    {
        world.cityBuilderManager.PlayCloseAudio();
		world.cityBuilderManager.UnselectTradeCenter();
    }

    public void SetName(string name)
    {
        title.text = name;
    }

    public void SetHappinessMeter(TradeRep rep)
    {
		int meterShift = rep.tradeRep.rapportScore * 32;
		Vector2 meterLoc = happinessMeter.transform.localPosition;
		meterLoc.x = meterShift;
		happinessMeter.transform.localPosition = meterLoc;

        if (rep.tradeRep.rapportScore == 5)
        {
            happinessMeter.sprite = ecstatic;
            buyMultiple = 1 - rep.tradeRep.ecstaticDiscount * 0.01f;
        }
		else if (rep.tradeRep.rapportScore > 2)
		{
			happinessMeter.sprite = happy;
			buyMultiple = 1 - rep.tradeRep.happyDiscount * 0.01f;
		}
		else if (rep.tradeRep.rapportScore < -2)
		{
			happinessMeter.sprite = mad;
			buyMultiple = 1 + rep.tradeRep.angryIncrease * 0.01f;
		}
		else
		{
			happinessMeter.sprite = neutral;
			buyMultiple = 1;
		}

		foreach (ResourceType type in center.resourceBuyDict.Keys)
		{
			int price = Mathf.CeilToInt(center.resourceBuyDict[type] * buyMultiple);
			//if (price > maxBuyCost)
   //             maxBuyCost = price;
            UITradeResource panel = GetFromPanelPool(true);
            panel.resourceType = type;
            panel.resourceImage.sprite = ResourceHolder.Instance.GetIcon(type);
			panel.SetValue(price);
            panel.transform.SetAsLastSibling();
            panel.GetComponent<UITooltipTrigger>().SetMessage(ResourceHolder.Instance.GetName(type));
            //panel.SetColor(world.CheckWorldGold(price) ? Color.white : Color.red);
            //activeBuyResources.Add(panel);
            //buyDict[type].gameObject.SetActive(true);
            //int price = Mathf.CeilToInt(center.resourceBuyDict[type] * buyMultiple);
            //buyDict[type].SetValue(price);
            //if (price > maxBuyCost)
            //	maxBuyCost = price;
            //buyDict[type].SetColor(world.CheckWorldGold(price) ? Color.white : Color.red);
            //activeBuyResources.Add(buyDict[type]);
        }

		foreach (ResourceType type in center.resourceSellDict.Keys)
		{
			UITradeResource panel = GetFromPanelPool(false);
			panel.resourceType = type;
			panel.resourceImage.sprite = ResourceHolder.Instance.GetIcon(type);
			//sellDict[type].gameObject.SetActive(true);
            panel.SetValue(center.resourceSellDict[type]);
			panel.transform.SetAsLastSibling();
			panel.GetComponent<UITooltipTrigger>().SetMessage(ResourceHolder.Instance.GetName(type));
			//sellDict[type].SetValue(center.resourceSellDict[type]);
			//sellDict[type].SetColor(Color.green);
			//activeSellResources.Add(sellDict[type]);
		}
	}

    public void UpdateResourcePrice(ResourceType type)
    {
        foreach (UITradeResource panel in sellPanelList)
        {
            if (panel.resourceType == type)
            {
                panel.SetValue(Mathf.Max(1, panel.resourceAmount - world.maxPriceDiff));
                break;
            }
        }
    }

	#region object pooling
	private void GrowPanelsPool(int num, bool buy)
	{
		if (buy)
        {
            for (int i = 0; i < num; i++)
		    {
			    GameObject panel = Instantiate(resourcePanel);
			    panel.gameObject.transform.SetParent(buyGrid, false);
                UITradeResource uiPanel = panel.GetComponent<UITradeResource>();
			    AddToPanelPool(uiPanel, true);
		    }
        }
        else
        {
			for (int i = 0; i < num; i++)
			{
				GameObject panel = Instantiate(resourcePanel);
				panel.gameObject.transform.SetParent(sellGrid, false);
				UITradeResource uiPanel = panel.GetComponent<UITradeResource>();
				AddToPanelPool(uiPanel, false);
			}
		}
	}

	private void AddToPanelPool(UITradeResource panel, bool buy)
	{
		panel.gameObject.SetActive(false); //inactivate it when adding to pool

        if (buy)
            buyPanelQueue.Enqueue(panel);
        else
            sellPanelQueue.Enqueue(panel);
	}

	private UITradeResource GetFromPanelPool(bool buy)
	{
		if (buy)
        {
            if (buyPanelQueue.Count == 0)
			    GrowPanelsPool(5, true);

		    var panel = buyPanelQueue.Dequeue();
		    panel.gameObject.SetActive(true);
            buyPanelList.Add(panel);
		    return panel;
        }
        else
        {
			if (sellPanelQueue.Count == 0)
				GrowPanelsPool(5, false);

			var panel = sellPanelQueue.Dequeue();
			panel.gameObject.SetActive(true);
            sellPanelList.Add(panel);
			return panel;
		}
	}

	private void HideAllPanels()
	{
		foreach (UITradeResource panel in buyPanelList)
			AddToPanelPool(panel, true);

		foreach (UITradeResource panel in sellPanelList)
			AddToPanelPool(panel, false);

		buyPanelList.Clear();
        sellPanelList.Clear();
	}
	#endregion
}
