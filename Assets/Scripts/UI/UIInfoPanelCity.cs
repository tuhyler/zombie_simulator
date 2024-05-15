using System;
using TMPro;
using UnityEngine;

public class UIInfoPanelCity : MonoBehaviour
{
    [SerializeField]
    private TMP_Text nameText, cityPop, availableHousing, unusedLabor, workEthic, waterLevel, powerLevel;

    [SerializeField]
    private GameObject cityWarning, renameCityButton, destroyCityButton;
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

		cityPop.text = SetStringValue(city.currentPop);

        UpdateHousing(city.HousingCount);
        UpdateWater(city.waterCount);
        UpdatePower(city.powerCount);

        unusedLabor.text = SetStringValue(city.unusedLabor);
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
		cityPop.text = SetStringValue(city.currentPop);
        unusedLabor.text = SetStringValue(city.unusedLabor);
		UpdateHousing(city.HousingCount);
        UpdateWater(city.waterCount);
		UpdatePower(city.powerCount);
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

    public void UpdatePower(int power)
    {
		if (power > 0)
		{
			powerLevel.color = Color.green;
			powerLevel.text = "+" + power.ToString();
		}
		else if (power == 0)
		{
			powerLevel.color = Color.white;
			powerLevel.text = power.ToString();
		}
		else
		{
			powerLevel.color = Color.red;
			powerLevel.text = power.ToString();
		}
	}

    public void ToggleVisibility(bool v, bool enemy = false)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(v);

            if (enemy)
            {
                renameCityButton.SetActive(false);
                destroyCityButton.SetActive(false);
            }
            else
            {
				renameCityButton.SetActive(true);
				destroyCityButton.SetActive(true);
			}

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
