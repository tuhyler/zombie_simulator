using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIInfoPanelCity : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI nameText, cityPop, unusedLabor, workEthic, foodLevelAndLimit, growthTimer, foodPerMinute, foodConsumed, 
        minutesTillGrowth, goldPerMinute, researchPerMinute;

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

    public void SetData(string name, int pop, int labor, float ethic, int foodLevel, int foodLimit, int time, float food,
        int foodEaten, string growthTime, int gold, int research)
    {
        this.foodLimit = foodLimit;
        nameText.text = name;
        cityPop.text = $"City Size: {pop}";
        unusedLabor.text = $"Unused Labor: {labor}";
        workEthic.text = $"Work Ethic: {ethic * 100}%";
        foodLevelAndLimit.text = $"Food Level: {foodLevel}/{foodLimit}";
        growthTimer.text = string.Format("Time till growth: {0:00}:{1:00}", time / 60, time % 60);
        if (food > 0)
            foodPerMinute.text = $"Food/Minute: +{food}";
        else
            foodPerMinute.text = $"Food/Minute: {food}";
        foodConsumed.text = $"Food Consumed/Minute: {foodEaten}";
        minutesTillGrowth.text = $"Time Till Growth: {growthTime}";
        goldPerMinute.text = $"Gold/Minute: {gold}";
        researchPerMinute.text = $"Research/Minute: {research}";
    }

    public void SetTimer(int time)
    {
        growthTimer.text = string.Format("Time till growth: {0:00}:{1:00}", time / 60, time % 60);
    }

    public void UpdateFoodStats(int pop, int foodLevel, int foodLimit, float food, int foodEaten, string growthTime)
    {
        cityPop.text = $"City Size: {pop}";
        foodLevelAndLimit.text = $"Food Level: {foodLevel}/{foodLimit}";
        if (food > 0)
            foodPerMinute.text = $"Food/Minute: +{food}";
        else
            foodPerMinute.text = $"Food/Minute: {food}";
        foodConsumed.text = $"Food Consumed/Minute: {foodEaten}";
        minutesTillGrowth.text = $"Time Till Growth: {growthTime}";
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
