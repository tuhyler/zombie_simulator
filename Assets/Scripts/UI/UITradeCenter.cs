using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITradeCenter : MonoBehaviour
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

    [SerializeField]
    private Transform buyGrid, sellGrid;

    private Dictionary<ResourceType, UITradeResource> buyDict = new();
    private Dictionary<ResourceType, UITradeResource> sellDict = new();

    private List<UITradeResource> activeBuyResources = new();
    private List<UITradeResource> activeSellResources = new();
    [HideInInspector]
    public TradeCenter center;

    private float buyMultiple;

    [SerializeField] //for tweening
    private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus;
    private Vector3 originalLoc;

    //for updating colors
    private int maxBuyCost;

    private void Awake()
    {
        gameObject.SetActive(false);
        originalLoc = allContents.anchoredPosition3D;

        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
        {
            GameObject newResource = Instantiate(resourcePanel);
            newResource.transform.SetParent(buyGrid, false);

            UITradeResource newTradeResource = newResource.GetComponent<UITradeResource>();
            newTradeResource.resourceType = resource.resourceType;
            newTradeResource.resourceImage.sprite = resource.resourceIcon;
            buyDict[resource.resourceType] = newTradeResource;
            newResource.SetActive(false);

            GameObject newResource2 = Instantiate(resourcePanel);
            newResource2.transform.SetParent(sellGrid, false);

            UITradeResource newTradeResource2 = newResource2.GetComponent<UITradeResource>();
            newTradeResource2.resourceType = resource.resourceType;
            newTradeResource2.resourceImage.sprite = resource.resourceIcon;
            sellDict[resource.resourceType] = newTradeResource2;
            newResource2.SetActive(false);
        }
    }

    public void ToggleVisibility(bool v, TradeCenter center = null)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            this.center = center;
            activeStatus = true;
            world.tcCanvas.gameObject.SetActive(true);
            gameObject.SetActive(v);
            ownerName.text = "Rapport with " + center.tcRep.npcName;
            ownerImage.sprite = center.tcRep.npcImage;
            increaseText.text = "+" + center.tcRep.angryIncrease.ToString() + "%";
            decreaseText.text = "-" + center.tcRep.happyDiscount.ToString() + "%,";
            ecstaticText.text = "-" + center.tcRep.ecstaticDiscount.ToString() + "%,";
            SetHappinessMeter(center.tcRep);

            allContents.anchoredPosition3D = originalLoc + new Vector3(-500f, 0, 0);
            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + 500f, 0.4f).setEaseOutBack();
        }
        else
        {
            activeStatus = false;
            this.center = null;
            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + -1000f, 0.2f).setOnComplete(SetActiveStatusFalse);
        }
    }

    public void UpdateColors(int prevGold, int currentGold, bool pos)
    {
        if (pos)
        {
            if (prevGold > maxBuyCost)
                return;
        }
        else
        {
            if (currentGold > maxBuyCost)
                return;
        }
       
        foreach (UITradeResource resource in activeBuyResources)
            resource.SetColor(world.CheckWorldGold(resource.resourceAmount) ? Color.white : Color.red);
    }

    private void SetActiveStatusFalse()
    {
		foreach (UITradeResource resource in activeBuyResources)
			resource.gameObject.SetActive(false);

		foreach (UITradeResource resource in activeSellResources)
			resource.gameObject.SetActive(false);

		activeBuyResources.Clear();
		activeSellResources.Clear();
		maxBuyCost = 0;

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

    public void SetHappinessMeter(NPC rep)
    {
		int meterShift = rep.rapportScore * 32;
		Vector2 meterLoc = happinessMeter.transform.localPosition;
		meterLoc.x = meterShift;
		happinessMeter.transform.localPosition = meterLoc;

        if (meterShift == 160)
        {
            happinessMeter.sprite = ecstatic;
            buyMultiple = 1 - rep.ecstaticDiscount * 0.01f;
        }
		else if (meterShift >= 65)
		{
			happinessMeter.sprite = happy;
			buyMultiple = 1 - rep.happyDiscount * 0.01f;
		}
		else if (meterShift <= -60)
		{
			happinessMeter.sprite = mad;
			buyMultiple = 1 + rep.angryIncrease * 0.01f;
		}
		else
		{
			happinessMeter.sprite = neutral;
			buyMultiple = 1;
		}

		foreach (ResourceType type in center.ResourceBuyDict.Keys)
		{
			buyDict[type].gameObject.SetActive(true);
			int price = Mathf.CeilToInt(center.ResourceBuyDict[type] * buyMultiple);
			buyDict[type].SetValue(price);
			if (price > maxBuyCost)
				maxBuyCost = price;
			buyDict[type].SetColor(world.CheckWorldGold(price) ? Color.white : Color.red);
			activeBuyResources.Add(buyDict[type]);
		}

		foreach (ResourceType type in center.ResourceSellDict.Keys)
		{
			sellDict[type].gameObject.SetActive(true);
			sellDict[type].SetValue(center.ResourceSellDict[type]);
			sellDict[type].SetColor(Color.green);
			activeSellResources.Add(sellDict[type]);
		}
	}
}
