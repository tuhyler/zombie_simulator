using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIInfoPanelCity : MonoBehaviour
{
    [SerializeField]
    private TMP_Text nameText, cityPop, availableHousing, unusedLabor, workEthic, waterLevel/*, foodLevelText, foodGrowthText*//*, foodPerMinute*/;

    //[SerializeField]
    //private Toggle pauseGrowthToggle;

    //private int foodPerUnit = 1;

    [SerializeField] //for tweening
    private RectTransform allContents;
    private bool activeStatus;
    private Vector3 originalLoc;

    //int foodLimit;

    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;

        gameObject.SetActive(false);
    }

    //setting food goal to grow
    //public void SetGrowthNumber(int growth)
    //{
    //    foodPerUnit = growth;
    //}

    public void SetAllData(City city)
    {
        nameText.text = city.cityName;

		cityPop.text = city.cityPop.CurrentPop.ToString();

        UpdateHousing(city.HousingCount);
        UpdateWater(city.waterCount);

        unusedLabor.text = city.cityPop.UnusedLabor.ToString();
        UpdateWorkEthic(city.workEthic);
    }

    public void SetGrowthData(City city)
    {
		cityPop.text = city.cityPop.CurrentPop.ToString();
		UpdateHousing(city.HousingCount);
        UpdateWater(city.waterCount);
    }
    //public void SetTimer(int time)
    //{
    //    growthTimer.text = string.Format("Food Consumption Time: {0:00}:{1:00}", time / 60, time % 60);
    //}

    public void UpdateFoodStats(int pop/*, int foodLevel*//*, float food*/)
    {
		cityPop.text = pop.ToString();
	}

    //public void SetGrowthPauseToggle(bool v)
    //{
    //    pauseGrowthToggle.isOn = v;
    //}

    public void UpdateCityName(string name)
    {
        nameText.text = name;
    }

    //public void UpdateFoodGrowth(int foodLevel)
    //{
    //    foodLevelAndLimit.text = $"Food Level: {foodLevel}/{foodLimit}";
    //}

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
            //LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();
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
}
