using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIProfitabilityCityStats : MonoBehaviour, IPointerDownHandler
{
	[SerializeField]
    public TMP_Text cityNameText, cityPopText, cityUnemployedText, cityCostText, cityAvgText, cityAvgProfitText;

	[SerializeField]
	private RectTransform[] circles = new RectTransform[5], lines = new RectTransform[4];

	[SerializeField]
	private Image[] circleImages = new Image[5], lineImages = new Image[4];

    [HideInInspector]
    public City city;

	[HideInInspector]
	public string cityName;
	[HideInInspector]
	public int cityPop, cityUnusedLabor, cityAvg;
	[HideInInspector]
	public float cityCost, cityProfit;


	public void UpdateStats()
    {
		UpdatePopStats();
		UpdateProfitability();
	}

	public void UpdatePopStats()
	{
		cityPop = city.currentPop;
		cityPopText.text = city.currentPop.ToString();
		cityUnusedLabor = city.unusedLabor;
		cityUnemployedText.text = city.unusedLabor.ToString();
	}

	public void UpdateProfitability()
	{
		cityCost = city.resourceManager.resourceConsumedPerMinuteDict[ResourceType.Gold];
		cityCostText.text = cityCost.ToString();
        cityAvg = Mathf.RoundToInt(city.lastFiveCoin.Sum() / (float)Mathf.Clamp(city.resourceManager.cycleCount, 1, 5));
        cityAvgText.text = cityAvg.ToString();
        cityProfit = cityAvg - cityCost;
		cityAvgProfitText.text = cityProfit.ToString();

		if (cityProfit < 0)
			cityAvgProfitText.color = new Color(0.6f, 0, 0);
		else if (cityProfit > 0)
			cityAvgProfitText.color = new Color(0, 0.6f, 0);
		else
			cityAvgProfitText.color = cityPopText.color;

		SetGraph(city.lastFiveCoin);
	}

	public void SetGraph(int[] coinArray)
	{
		int min = coinArray.Min();
		int max = coinArray.Max();
		float range = Mathf.Clamp(max - min,1,max);
		Color color = coinArray[4] < coinArray[0] ? new Color(0.6f, 0, 0) : new Color(0, 0.6f, 0);

		for (int i = 0; i < 5; i++)
		{
			Vector3 pos = circles[i].anchoredPosition;
			pos.y = ((coinArray[i] - min) / range * 30) - 15;
			circles[i].anchoredPosition = pos;
			circleImages[i].color = color;
		}

		for (int i = 0; i < 4; i++)
		{
			float pos1 = circles[i].anchoredPosition.y;
			float pos2 = circles[i + 1].anchoredPosition.y;
			Vector3 pos = lines[i].anchoredPosition;
			pos.y = (pos1 + pos2) * 0.5f;
			lines[i].anchoredPosition = pos;

			lines[i].transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan((pos2 - pos1) * 0.025f) * Mathf.Rad2Deg);
			lineImages[i].color = color;
		}
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		city.world.cityBuilderManager.CenterCamOnLoc(city.cityLoc);
		city.world.uiProfitabilityStats.ToggleVisibility(false);
	}
}
