using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIResourceGivingPanel : MonoBehaviour
{
	[SerializeField]
	public MapWorld world;

	[SerializeField]
	UIResourceGivingSubPanel uiResourceSubPanel;

	[SerializeField]
	private Image tradeRepImage;

	[SerializeField]
	private TMP_Text unitNameTitle;

	[SerializeField]
	public UIPersonalResources giftedResource;

	[SerializeField]
	private GameObject confirmButton;

	[SerializeField]
	private UnityEvent<ResourceType> OnIconButtonClick;

	[SerializeField] //for tweening
	private RectTransform allContents, nameHolder;
	[HideInInspector]
	public bool activeStatus, showingResource;
	private Vector3 originalLoc;

	[HideInInspector]
	public TradeRep tradeRep;

	private void Awake()
	{
		gameObject.SetActive(false);
		originalLoc = allContents.anchoredPosition3D;
		giftedResource.givingPanel = this;
	}

	public void ToggleVisibility(bool v, bool keepSelection, bool confirmed, TradeRep tradeRep = null)
	{
		if (activeStatus == v)
			return;

		LeanTween.cancel(gameObject);

		if (v)
		{
			this.tradeRep = tradeRep;
			gameObject.SetActive(v);
			activeStatus = true;
			unitNameTitle.text = "Gift to " + tradeRep.tradeRepName;
			nameHolder.sizeDelta = new Vector3(170 + 16*tradeRep.tradeRepName.Length, 50);
			tradeRepImage.sprite = Resources.Load<Sprite>("MyConvoFaces/" + tradeRep.buildDataSO.imageName);
			giftedResource.resourceType = ResourceType.None;
			giftedResource.resourceAmount = 0;
			giftedResource.gameObject.SetActive(false);
			confirmButton.SetActive(false);
			uiResourceSubPanel.ToggleVisibility(true, tradeRep.tradeRepName, tradeRep.questHints[tradeRep.currentQuest]);

			world.unitMovement.loadScreenSet = true;
			world.unitMovement.GivenAmount = 0;
			allContents.anchoredPosition3D = originalLoc;
			LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 800f, 0.4f).setEase(LeanTweenType.easeOutSine);
		}
		else
		{
			if (!confirmed && showingResource)
				ReturnResources();
			world.unitMovement.loadScreenSet = false;
			uiResourceSubPanel.ToggleVisibility(false);
			world.unitMovement.uiPersonalResourceInfoPanel.RestorePosition(keepSelection);
			activeStatus = false;
			showingResource = false;
			this.tradeRep = null;
			LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - 800f, 0.4f)
				.setEase(LeanTweenType.easeOutSine)
				.setOnComplete(SetVisibilityFalse);
		}
	}

	private void SetVisibilityFalse()
	{
		gameObject.SetActive(false);
	}

	public void ReturnResources()
	{
		world.unitMovement.uiPersonalResourceInfoPanel.ReturnResource(giftedResource.resourceType, giftedResource.resourceAmount);
	}

	public void HandleButtonClick()
	{
		OnIconButtonClick?.Invoke(giftedResource.resourceType);
	}

	public void ChangeGiftAmount(ResourceType type, int amount)
	{
		int newAmount = giftedResource.resourceAmount + amount;
		
		if (amount > 0)
		{
			if (showingResource)
			{
				giftedResource.UpdateValue(newAmount, true);
			}
			else
			{
				showingResource = true;
				giftedResource.resourceType = type;
				giftedResource.resourceAmount = newAmount;
				giftedResource.gameObject.SetActive(true);
				giftedResource.resourceImage.sprite = ResourceHolder.Instance.GetIcon(type);
				giftedResource.SetValue(newAmount);
				giftedResource.resourceType = type;
				giftedResource.Activate(false);
				confirmButton.SetActive(true);
			}
		}
		else
		{
			giftedResource.UpdateValue(newAmount, false);

			if (giftedResource.resourceAmount == 0)
			{
				showingResource = false;
				confirmButton.SetActive(false);
				giftedResource.gameObject.SetActive(false);
			}
		}
	}

	public void ConfirmGiftButton()
	{
		ResourceValue gift;
		gift.resourceType = giftedResource.resourceType;
		gift.resourceAmount = giftedResource.resourceAmount;

		bool doneGood = tradeRep.GiftCheck(gift);
		world.PlayGiftResponse(tradeRep.transform.position, doneGood);
		ToggleVisibility(false, true, true);
	}
}
