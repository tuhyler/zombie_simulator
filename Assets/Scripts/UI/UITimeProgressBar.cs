using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITimeProgressBar : MonoBehaviour
{
    [SerializeField]
    private TMP_Text timeText;

    [SerializeField]
    private Image progressBarMask;

    private int totalTime;
    private float totalTimeFactor;
    private string additionalText = "";
    public string SetAdditionalText { set { additionalText = value; } }

    private void Awake()
    {
        timeText.outlineWidth = 0.5f;
        timeText.outlineColor = new Color(0, 0, 0, 255);

        gameObject.SetActive(false);
    }

    public void SetTime(int time)
    {
        timeText.text = additionalText + string.Format("{0:00}:{1:00}", time / 60, time % 60);
        int nextTime = (totalTime - time) + 1;

        LeanTween.value(progressBarMask.gameObject, progressBarMask.fillAmount, nextTime * totalTimeFactor, 1f)
            .setEase(LeanTweenType.linear)
            .setOnUpdate((value) =>
            {
                progressBarMask.fillAmount = value;
            });
    }

    public void SetToZero()
    {
        progressBarMask.fillAmount = 0;
    }

    public void SetToFull()
    {
        progressBarMask.fillAmount = 1;
        timeText.text = additionalText + string.Format("{0:00}:{1:00}", 0, 0);
    }

    public void SetTimeProgressBarValue(int totalTime)
    {
        this.totalTime = totalTime;
        totalTimeFactor = 1f / totalTime;

        if (this.totalTime <= 0)
            this.totalTime = 1;
    }

    public void SetProgressBarMask(int time)
    {
        progressBarMask.fillAmount = (totalTime - time) * totalTimeFactor;

    }
}
