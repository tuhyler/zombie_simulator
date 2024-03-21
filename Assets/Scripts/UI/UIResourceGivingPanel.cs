using Mono.Cecil;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIResourceGivingPanel : MonoBehaviour
{
	[SerializeField]
	public MapWorld world;

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
	public NPC npc;

	private void Awake()
	{
		gameObject.SetActive(false);
		originalLoc = allContents.anchoredPosition3D;
		giftedResource.givingPanel = this;
	}

	public void ToggleVisibility(bool v, bool keepSelection, bool confirmed, NPC npc = null)
	{
		if (activeStatus == v)
			return;

		LeanTween.cancel(gameObject);

		if (v)
		{
			this.npc = npc;
			gameObject.SetActive(v);
			activeStatus = true;
			unitNameTitle.text = "Gift to " + npc.npcName;
			nameHolder.sizeDelta = new Vector3(170 + 16*npc.npcName.Length, 50);
			tradeRepImage.sprite = npc.npcImage;
			giftedResource.resourceType = ResourceType.None;
			giftedResource.resourceAmount = 0;
			giftedResource.gameObject.SetActive(false);
			confirmButton.SetActive(false);

			allContents.anchoredPosition3D = originalLoc;
			LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 800f, 0.4f).setEase(LeanTweenType.easeOutSine);
		}
		else
		{
			if (!confirmed && showingResource)
				ReturnResources();
			world.unitMovement.uiPersonalResourceInfoPanel.RestorePosition(keepSelection);
			activeStatus = false;
			showingResource = false;
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

		ToggleVisibility(false, true, true);
		bool doneGood = npc.GiftCheck(gift);
		world.PlayGiftResponse(npc.transform.position, doneGood);
	}
}
