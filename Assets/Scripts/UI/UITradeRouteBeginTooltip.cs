using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Burst.Intrinsics;
using UnityEngine;

public class UITradeRouteBeginTooltip : MonoBehaviour
{
	[SerializeField]
	private MapWorld world;

	[SerializeField]
	private TMP_Text none;

	[SerializeField]
	private Transform costsRect;

	private List<UIResourceInfoPanel> costsInfo = new();
	private List<ResourceType> cantAffordList = new();

	[HideInInspector]
	public bool cantAfford;
	private Trader trader;
	private City city;

	//for tweening
	[SerializeField]
	private RectTransform allContents;
	[HideInInspector]
	public bool activeStatus;

	private void Awake()
	{
		transform.localScale = Vector3.zero;
		gameObject.SetActive(false);

		foreach (Transform selection in costsRect)
		{
			if (selection.TryGetComponent(out UIResourceInfoPanel panel))
			{
				costsInfo.Add(panel);
			}
		}
	}

	public void ToggleVisibility(bool val, Trader trader = null)
	{
		if (activeStatus == val)
			return;

		LeanTween.cancel(gameObject);

		if (val)
		{
			this.trader = trader;
			city = trader.GetStartingCity();
			SetResourcePanelInfo(costsInfo, trader.ShowRouteCost(), city.ResourceManager);

			gameObject.SetActive(val);
			activeStatus = true;
			LeanTween.scale(allContents, Vector3.one, 0.25f).setEaseLinear();
		}
		else
		{
			cantAffordList.Clear();
			this.trader = null;
			city = null;

			activeStatus = false;
			LeanTween.scale(allContents, Vector3.zero, 0.25f).setOnComplete(SetActiveStatusFalse);
		}
	}

	private void SetActiveStatusFalse()
	{
		gameObject.SetActive(false);
		world.infoPopUpCanvas.gameObject.SetActive(false);
	}

	private void SetResourcePanelInfo(List<UIResourceInfoPanel> panelList, List<ResourceValue> resourceList, ResourceManager manager)
	{
		int resourcesCount = resourceList.Count;

		//show text if blank
		if (resourcesCount == 0)
			none.gameObject.SetActive(true);
		else
			none.gameObject.SetActive(false);

		cantAfford = false;
		for (int i = 0; i < panelList.Count; i++)
		{
			if (i >= resourcesCount)
			{
				panelList[i].gameObject.SetActive(false);
			}
			else
			{
				panelList[i].gameObject.SetActive(true);
				panelList[i].resourceAmountText.text = resourceList[i].resourceAmount.ToString();
				panelList[i].resourceType = resourceList[i].resourceType;
				panelList[i].resourceImage.sprite = ResourceHolder.Instance.GetIcon(resourceList[i].resourceType);

				if (resourceList[i].resourceType == ResourceType.Gold)
				{
					if (world.CheckWorldGold(resourceList[i].resourceAmount))
					{
						panelList[i].resourceAmountText.color = Color.white;
					}
					else
					{
						panelList[i].resourceAmountText.color = Color.red;
						cantAfford = true;
						cantAffordList.Add(resourceList[i].resourceType);
					}

					continue;
				}

				if (!manager.CheckResourceAvailability(resourceList[i]))
				{
					panelList[i].resourceAmountText.color = Color.red;
					cantAfford = true;
					cantAffordList.Add(resourceList[i].resourceType);
				}
				else
					panelList[i].resourceAmountText.color = Color.white;
			}
		}
	}

	public bool CityCheck(City city)
	{
		return activeStatus && this.city == city;
	}

	public void UpdateRouteCost(int amount, ResourceType type)
	{
		List<ResourceValue> resourceList = trader.totalRouteCosts;
		bool tempCantAfford = false;

		for (int i = 0; i < costsInfo.Count; i++)
		{
			if (i >= resourceList.Count || type != costsInfo[i].resourceType)
				continue;

			if (amount >= resourceList[i].resourceAmount)
			{
				costsInfo[i].resourceAmountText.color = Color.white;
			}
			else
			{
				costsInfo[i].resourceAmountText.color = Color.red;
				tempCantAfford = true;
			}
		}

		if (cantAffordList.Contains(type))
		{
			if (!tempCantAfford)
				cantAffordList.Remove(type);
		}
		else
		{
			if (tempCantAfford)
				cantAffordList.Add(type);
		}

		if (cantAffordList.Count == 0)
			cantAfford = false;
		else
			cantAfford = true;
	}

	public bool AffordCheck()
	{
		if (cantAfford)
		{
			StartCoroutine(Shake());
			UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't afford", true, false);
			return false;
		}

		return true;
	}

	public IEnumerator Shake()
	{
		Vector3 initialPos = transform.localPosition;
		float elapsedTime = 0f;
		float duration = 0.2f;

		while (elapsedTime < duration)
		{
			elapsedTime += Time.deltaTime;
			transform.localPosition = initialPos + (Random.insideUnitSphere * 10f);
			yield return null;
		}

		transform.localPosition = initialPos;
	}
}
