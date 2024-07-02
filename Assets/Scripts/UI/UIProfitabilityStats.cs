using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.UI;
using System.Linq;

public class UIProfitabilityStats : MonoBehaviour, IImmoveable
{
	[SerializeField]
	private MapWorld world;

	[SerializeField]
	private CameraController cameraController;

	[SerializeField]
	private Transform cityStatsHolder;

	private Dictionary<City, UIProfitabilityCityStats> cityStatDict = new();

	//for sorting
	[SerializeField]
	private Image nameSort, popSort, laborSort, avgSort, costSort, profitSort; 
	private Color originalColor;
	bool nameSortUp, popSortUp, laborSortUp, avgSortUp, costSortUp, profitSortUp, nameSorted, popSorted, laborSorted, avgSorted, costSorted, profitSorted;
	[SerializeField]
	private Sprite buttonUp;
	private Sprite buttonDown;

	//for blurring background
	[SerializeField]
	private Volume globalVolume;
	private DepthOfField dof;

	[SerializeField] //for tweening
	private RectTransform allContents;
	[HideInInspector]
	public bool activeStatus;
	private Vector3 originalLoc;

	private void Awake()
	{
		originalLoc = allContents.anchoredPosition3D;
		originalColor = nameSort.color;
		buttonDown = nameSort.sprite;
		gameObject.SetActive(false);
		
		if (globalVolume.profile.TryGet<DepthOfField>(out DepthOfField tmpDof))
			dof = tmpDof;

		dof.focalLength.value = 15;
	}

	public void ToggleVisibility(bool v)
	{
		if (activeStatus == v)
			return;

		LeanTween.cancel(gameObject);

		if (v)
		{
			world.UnselectAll();
			world.immoveableCanvas.gameObject.SetActive(true);
			world.iImmoveable = this;
			gameObject.SetActive(true);
			world.tooltip = false;
			world.somethingSelected = true;
			activeStatus = true;

			PopulateCityProfitabilityStats();

			allContents.anchoredPosition3D = originalLoc + new Vector3(0, 1200f, 0);

			LeanTween.value(globalVolume.gameObject, dof.focalLength.value, 45, 0.5f)
			.setEase(LeanTweenType.easeOutSine)
			.setOnUpdate((value) =>
			{
				dof.focalLength.value = value;
			});
			LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -1200f, 0.5f).setEaseOutSine();

			if (nameSorted)
				SortName(nameSortUp);
			else if (popSorted)
				SortPop(popSortUp);
			else if (laborSorted)
				SortLabor(laborSortUp);
			else if (avgSorted)
				SortAvg(avgSortUp);
			else if (costSorted)
				SortCost(costSortUp);
			else if (profitSorted)
				SortProfit(profitSortUp);
		}
		else
		{
			activeStatus = false;
			world.iImmoveable = null;

			LeanTween.value(globalVolume.gameObject, dof.focalLength.value, 15, 0.3f)
			.setEase(LeanTweenType.easeOutSine)
			.setOnUpdate((value) =>
			{
				dof.focalLength.value = value;
			});
			LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 1200f, 0.3f).setOnComplete(SetActiveStatusFalse);
		}

		cameraController.enabled = !v;
	}

	private void SetActiveStatusFalse()
	{
		gameObject.SetActive(false);
		if (world.iImmoveable == null)
			world.immoveableCanvas.gameObject.SetActive(false);
	}

	public void CloseProfitabilityStats()
	{
		world.cityBuilderManager.PlayCloseAudio();
		ToggleVisibility(false);
		world.somethingSelected = false;
	}

	private void PopulateCityProfitabilityStats()
	{
		foreach (UIProfitabilityCityStats cityStats in cityStatDict.Values)
			cityStats.UpdateStats();
	}

	public void CreateNewProfitabilityCityStats(City city, bool load)
	{
		UIProfitabilityCityStats cityStats = Instantiate(Resources.Load<UIProfitabilityCityStats>("Prefabs/UIPrefabs/ProfitabilityCityStats"));
		cityStats.transform.SetParent(cityStatsHolder, false);
		cityStats.city = city;
		cityStats.cityNameText.text = city.cityName;
		cityStats.cityName = city.cityName;
		if (load)
			cityStats.UpdateStats();
		cityStatDict[city] = cityStats;


		if (activeStatus)
			cityStats.UpdateStats();
	}

	public void RemoveCityStats(City city)
	{
		UIProfitabilityCityStats removedStats = cityStatDict[city];
		cityStatDict.Remove(city);
		Destroy(removedStats.gameObject);
	}

	public void UpdateCityName(City city)
	{
		if (cityStatDict.ContainsKey(city))
		{
			cityStatDict[city].cityName = city.cityName;
			cityStatDict[city].cityNameText.text = city.cityName;
		}
	}

	public void UpdateCityPop(City city)
	{
		cityStatDict[city].UpdatePopStats();
	}

	public void UpdateCityProfitability(City city)
	{
		cityStatDict[city].UpdateProfitability();
	}




	//sorting
	private void ChangeSortButtonsColors(string column)
	{
		switch (column)
		{
			case "name":
				nameSort.color = Color.green;
				popSort.color = originalColor;
				laborSort.color = originalColor;
				avgSort.color = originalColor;
				costSort.color = originalColor;
				profitSort.color = originalColor;
				nameSorted = true;
				popSorted = false;
				laborSorted = false;
				avgSorted = false;
				costSorted = false;
				profitSorted = false;
				break;
			case "pop":
				nameSort.color = originalColor;
				popSort.color = Color.green;
				laborSort.color = originalColor;
				avgSort.color = originalColor;
				costSort.color = originalColor;
				profitSort.color = originalColor;
				nameSorted = false;
				popSorted = true;
				laborSorted = false;
				avgSorted = false;
				costSorted = false;
				profitSorted = false;
				break;
			case "labor":
				nameSort.color = originalColor;
				popSort.color = originalColor;
				laborSort.color = Color.green;
				avgSort.color = originalColor;
				costSort.color = originalColor;
				profitSort.color = originalColor;
				nameSorted = false;
				popSorted = false;
				laborSorted = true;
				avgSorted = false;
				costSorted = false;
				profitSorted = false;
				break;
			case "avg":
				nameSort.color = originalColor;
				popSort.color = originalColor;
				laborSort.color = originalColor;
				avgSort.color = Color.green;
				costSort.color = originalColor;
				profitSort.color = originalColor;
				nameSorted = false;
				popSorted = false;
				laborSorted = false;
				avgSorted = true;
				costSorted = false;
				profitSorted = false;
				break;
			case "cost":
				nameSort.color = originalColor;
				popSort.color = originalColor;
				laborSort.color = originalColor;
				avgSort.color = originalColor;
				costSort.color = Color.green;
				profitSort.color = originalColor;
				nameSorted = false;
				popSorted = false;
				laborSorted = false;
				avgSorted = false;
				costSorted = true;
				profitSorted = false;
				break;
			case "profit":
				nameSort.color = originalColor;
				popSort.color = originalColor;
				laborSort.color = originalColor;
				avgSort.color = originalColor;
				costSort.color = originalColor;
				profitSort.color = Color.green;
				nameSorted = false;
				popSorted = false;
				laborSorted = false;
				avgSorted = false;
				costSorted = false;
				profitSorted = true;
				break;
		}
	}

	public void SortName()
	{
		world.cityBuilderManager.PlaySelectAudio();
		ChangeSortButtonsColors("name");

		nameSort.sprite = nameSortUp ? buttonUp : buttonDown;

		SortName(nameSortUp);
		nameSortUp = !nameSortUp;
	}

	private void SortName(bool up)
	{
		List<UIProfitabilityCityStats> cityStatsList = cityStatDict.Values.ToList();
		cityStatsList = up ? cityStatsList.OrderBy(m => m.cityName).ToList() : cityStatsList.OrderByDescending(m => m.cityName).ToList();

		//reorder on UI
		for (int i = 0; i < cityStatsList.Count; i++)
			cityStatsList[i].transform.SetSiblingIndex(i);
	}

	public void SortPop()
	{
		world.cityBuilderManager.PlaySelectAudio();
		ChangeSortButtonsColors("pop");

		popSort.sprite = popSortUp ? buttonUp : buttonDown;

		SortPop(popSortUp);
		popSortUp = !popSortUp;
	}

	private void SortPop(bool up)
	{
		List<UIProfitabilityCityStats> cityStatsList = cityStatDict.Values.ToList();
		cityStatsList = up ? cityStatsList.OrderBy(m => m.cityPop).ToList() : cityStatsList.OrderByDescending(m => m.cityPop).ToList();

		//reorder on UI
		for (int i = 0; i < cityStatsList.Count; i++)
			cityStatsList[i].transform.SetSiblingIndex(i);
	}

	public void SortLabor()
	{
		world.cityBuilderManager.PlaySelectAudio();
		ChangeSortButtonsColors("labor");

		laborSort.sprite = laborSortUp ? buttonUp : buttonDown;

		SortLabor(laborSortUp);
		laborSortUp = !laborSortUp;
	}

	private void SortLabor(bool up)
	{
		List<UIProfitabilityCityStats> cityStatsList = cityStatDict.Values.ToList();
		cityStatsList = up ? cityStatsList.OrderBy(m => m.cityUnusedLabor).ToList() : cityStatsList.OrderByDescending(m => m.cityUnusedLabor).ToList();

		//reorder on UI
		for (int i = 0; i < cityStatsList.Count; i++)
			cityStatsList[i].transform.SetSiblingIndex(i);
	}

	public void SortAvg()
	{
		world.cityBuilderManager.PlaySelectAudio();
		ChangeSortButtonsColors("avg");

		avgSort.sprite = avgSortUp ? buttonUp : buttonDown;

		SortAvg(avgSortUp);
		avgSortUp = !avgSortUp;
	}

	private void SortAvg(bool up)
	{
		List<UIProfitabilityCityStats> cityStatsList = cityStatDict.Values.ToList();
		cityStatsList = up ? cityStatsList.OrderBy(m => m.cityAvg).ToList() : cityStatsList.OrderByDescending(m => m.cityAvg).ToList();

		//reorder on UI
		for (int i = 0; i < cityStatsList.Count; i++)
			cityStatsList[i].transform.SetSiblingIndex(i);
	}

	public void SortCost()
	{
		world.cityBuilderManager.PlaySelectAudio();
		ChangeSortButtonsColors("cost");

		costSort.sprite = costSortUp ? buttonUp : buttonDown;

		SortCost(costSortUp);
		costSortUp = !costSortUp;
	}

	private void SortCost(bool up)
	{
		List<UIProfitabilityCityStats> cityStatsList = cityStatDict.Values.ToList();
		cityStatsList = up ? cityStatsList.OrderBy(m => m.cityCost).ToList() : cityStatsList.OrderByDescending(m => m.cityCost).ToList();

		//reorder on UI
		for (int i = 0; i < cityStatsList.Count; i++)
			cityStatsList[i].transform.SetSiblingIndex(i);
	}

	public void SortProfit()
	{
		world.cityBuilderManager.PlaySelectAudio();
		ChangeSortButtonsColors("profit");

		profitSort.sprite = profitSortUp ? buttonUp : buttonDown;

		SortProfit(profitSortUp);
		profitSortUp = !profitSortUp;
	}

	private void SortProfit(bool up)
	{
		List<UIProfitabilityCityStats> cityStatsList = cityStatDict.Values.ToList();
		cityStatsList = up ? cityStatsList.OrderBy(m => m.cityProfit).ToList() : cityStatsList.OrderByDescending(m => m.cityProfit).ToList();

		//reorder on UI
		for (int i = 0; i < cityStatsList.Count; i++)
			cityStatsList[i].transform.SetSiblingIndex(i);
	}
}
