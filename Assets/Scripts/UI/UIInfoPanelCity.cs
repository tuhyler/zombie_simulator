using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIInfoPanelCity : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI nameText, cityPop, availableHousing, unusedLabor, workEthic, foodLevelAndLimit, foodPerMinute;

    [SerializeField] //for tweening
    private RectTransform allContents;
    private bool activeStatus;
    private Vector3 originalLoc;

    int foodLimit;

    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;

        gameObject.SetActive(false);
    }

    public void SetData(string name, int pop, int housing, int labor, float ethic, int foodLevel, int foodLimit, float food)
    {
        nameText.text = name;

        SetCityAndFoodStats(pop, foodLevel, foodLimit, food);

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

        unusedLabor.text = $"Unemployed: {labor}";
        workEthic.text = $"{ethic * 100}%";
        if (ethic < .5)
            workEthic.color = Color.red;
        else if (ethic < .8)
            workEthic.color = Color.yellow;
        else
            workEthic.color = Color.green;
    }

    //public void SetTimer(int time)
    //{
    //    growthTimer.text = string.Format("Food Consumption Time: {0:00}:{1:00}", time / 60, time % 60);
    //}

    public void UpdateFoodStats(int pop, int foodLevel, int foodLimit, float food)
    {
        SetCityAndFoodStats(pop, foodLevel, foodLimit, food);
    }

    private void SetCityAndFoodStats(int pop, int foodLevel, int foodLimit, float food)
    {
        this.foodLimit = foodLimit;

        cityPop.text = $"Size: {pop}";
        foodLevelAndLimit.text = $"Food Level: {foodLevel}/{foodLimit}";
        SetSurplusFoodText(food);
    }

    private void SetSurplusFoodText(float food)
    {
        if (food > 0)
        {
            foodPerMinute.text = $"+{food}";
            foodPerMinute.color = Color.green;
        }
        else if (food < 0)
        {
            foodPerMinute.text = $"-{food}";
            foodPerMinute.color = Color.red;
        }
        else
        {
            foodPerMinute.text = $"{food}";
            foodPerMinute.color = Color.white;
        }

    }

    public void UpdateCityName(string name)
    {
        nameText.text = name;
    }

    public void UpdateFoodGrowth(int foodLevel)
    {
        foodLevelAndLimit.text = $"Food Level: {foodLevel}/{foodLimit}";
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

            allContents.anchoredPosition3D = originalLoc + new Vector3(0, -600f, 0);

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 600f, 0.3f).setEaseOutSine();
            LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();
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
