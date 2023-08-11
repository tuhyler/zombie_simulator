using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIInfoPanelCity : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI nameText, cityPop, availableHousing, unusedLabor, workEthic, foodLevelText, foodGrowthText, foodPerMinute;

    [SerializeField]
    private Toggle pauseGrowthToggle;

    private int foodPerUnit = 1;

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

    public void SetGrowthNumber(int growth)
    {
        foodPerUnit = growth;
    }

    public void SetData(string name, int pop, int housing, int labor, float ethic, int foodLevel, float food)
    {
        nameText.text = name;

        SetCityAndFoodStats(pop, foodLevel, food);

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

    public void UpdateFoodStats(int pop, int foodLevel, float food)
    {
        SetCityAndFoodStats(pop, foodLevel, food);
    }

    private void SetCityAndFoodStats(int pop, int foodLevel, float food)
    {
        //this.foodLimit = foodLimit;
        //int foodLevelAdj = Mathf.Max(0, foodLevel);

        cityPop.text = $"Size: {pop}";
        foodLevelText.text = foodLevel.ToString();
        foodPerMinute.text = food.ToString();
        foodGrowthText.text = (foodLevel + foodPerUnit).ToString();

        if (food - foodLevel > 0)
            foodPerMinute.color = Color.green;
        else if (food - foodLevel < 0)
            foodPerMinute.color = Color.red;
        else
            foodPerMinute.color = Color.white;
    }

    public void SetGrowthPauseToggle(bool v)
    {
        pauseGrowthToggle.isOn = v;
    }

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
        if (ethic < .5)
            workEthic.color = Color.red;
        else if (ethic < .8)
            workEthic.color = Color.yellow;
        else
            workEthic.color = Color.green;
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
