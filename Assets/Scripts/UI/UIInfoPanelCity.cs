using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIInfoPanelCity : MonoBehaviour
{
    [SerializeField]
    private TMP_Text nameText, cityPop, availableHousing, unusedLabor, workEthic, waterLevel;

    [SerializeField]
    private GameObject cityWarning;
	private UITooltipTrigger tooltipTrigger;

	[SerializeField] //for tweening
    private RectTransform allContents;
    [SerializeField]
    public bool activeStatus;
    private Vector3 originalLoc;

    //int foodLimit;

    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;

        gameObject.SetActive(false);

        foreach (UITooltipTrigger tooltips in GetComponentsInChildren<UITooltipTrigger>())
        {
            if (tooltips.workEthic)
                tooltipTrigger = tooltips;
        }
    }

    public void SetAllData(City city)
    {
        nameText.text = city.cityName;

		cityPop.text = SetStringValue(city.cityPop.CurrentPop);

        UpdateHousing(city.HousingCount);
        UpdateWater(city.waterCount);

        unusedLabor.text = SetStringValue(city.cityPop.UnusedLabor);
        UpdateWorkEthic(city.workEthic);
	}

	private string SetStringValue(int amount)
	{
		string amountStr = "-";

		if (amount < 1000)
		{
			amountStr = amount.ToString();
		}
		else if (amount < 1000000)
		{
			amountStr = Math.Round(amount * 0.001f, 1) + " k";
		}
		else if (amount < 1000000000)
		{
			amountStr = Math.Round(amount * 0.000001f, 1) + " M";
		}

		return amountStr;
	}

	public void SetWorkEthicPopUpCity(City city)
    {
		tooltipTrigger.SetCity(city);
	}

	public void SetGrowthData(City city)
    {
		cityPop.text = SetStringValue(city.cityPop.CurrentPop);
		UpdateHousing(city.HousingCount);
        UpdateWater(city.waterCount);
    }

    public void UpdateFoodStats(int pop/*, int foodLevel*//*, float food*/)
    {
		cityPop.text = SetStringValue(pop);
	}

    public void UpdateCityName(string name)
    {
        nameText.text = name;
    }

    public void UpdateHousing(int housing)
    {
        if (housing < 0)
        {
            availableHousing.text = $"-{housing}";
            availableHousing.color = Color.red;
        }
        else if (housing == 0)
        {
            availableHousing.text = $"{housing}";
            availableHousing.color = Color.white;

        }
        else
        {
            availableHousing.text = $"+{housing}";
            availableHousing.color = Color.green;
        }
    }

    public void UpdateWorkEthic(float ethic)
    {
        workEthic.text = $"{ethic * 100}%";
        if (ethic < 1)
            workEthic.color = Color.red;
        else if (ethic == 1)
            workEthic.color = Color.white;
        else
            workEthic.color = Color.green;
    }

    public void UpdateWater(int waterPop)
    {
        if (waterPop >= 9000)
        {
            waterLevel.color = Color.green;
			waterLevel.text = "\u221E"; //infinity symbol
			waterLevel.fontSize = 44;
		}
        else if (waterPop > 0)
        {
			waterLevel.color = Color.green;
			waterLevel.text = "+" + waterPop.ToString();
			waterLevel.fontSize = 26;
		}
        else if (waterPop == 0)
        {
            waterLevel.color = Color.white;
            waterLevel.text = waterPop.ToString();
			waterLevel.fontSize = 26;
		}
        else
        {
			waterLevel.color = Color.red;
			waterLevel.text = waterPop.ToString();
			waterLevel.fontSize = 26;
		}
	}

    public void ToggleVisibility(bool v)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(v);

            activeStatus = true;

            allContents.anchoredPosition3D = originalLoc + new Vector3(0, -200f, 0);

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 200f, 0.4f).setEaseOutBack();
        }
        else
        {
            activeStatus = false;
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -600f, 0.2f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void TogglewWarning(bool v)
    {
        cityWarning.SetActive(v);
    }
}
