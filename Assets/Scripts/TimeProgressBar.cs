using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TimeProgressBar : MonoBehaviour
{
    [SerializeField]
    private TMP_Text timeText;

    [SerializeField]
    private Transform timeProgressBarMask;

    public float fullProgressBarAmount = 0.77f;
    private float positionCorrection = -.71f; //progress bar moves slightly to the right as it fills up, so this corrects for that.
    private int totalTime;
    private float increment;
    private float positionIncrement;
    private Vector3 newScale;
    private Vector3 newPosition;
    private string additionalText = "";
    public string SetAdditionalText { set { additionalText= value; } }

    private void Awake()
    {
        //laborNumberHolder.GetComponent<SpriteRenderer>().enabled = false;
        timeText.outlineWidth = 0.5f;
        timeText.outlineColor = new Color(0, 0, 0, 255);

        newScale = timeProgressBarMask.localScale; //change progress bar through scale
        newPosition = timeProgressBarMask.localPosition;
        newScale.x = 0;
        newPosition.x = positionCorrection;
        timeProgressBarMask.localScale = newScale;
        SetActive(false);
    }

    public void SetTime(int time)
    {
        timeText.text = additionalText + string.Format("{0:00}:{1:00}", time / 60, time % 60);
        newScale.x = (totalTime - time) * increment + increment;
        newPosition.x = positionCorrection + ((totalTime - time) * positionIncrement + positionIncrement);

        if (timeProgressBarMask.localScale.x > totalTime * increment - increment)
        {
            Vector3 tempScale = timeProgressBarMask.localScale;
            Vector3 tempPosition = timeProgressBarMask.localPosition;
            tempScale.x = 0;
            timeProgressBarMask.localScale = tempScale;
            tempPosition.x = positionCorrection;
            timeProgressBarMask.localPosition = tempPosition;
            //timeProgressBarMask.localScale = newScale;
        }

        LeanTween.value(timeProgressBarMask.gameObject, timeProgressBarMask.localScale.x, newScale.x, 1.09f)
        .setEase(LeanTweenType.linear)
        .setOnUpdate((value) =>
        {
            newScale.x = value;
            timeProgressBarMask.localScale = newScale;
        });

        LeanTween.value(timeProgressBarMask.gameObject, timeProgressBarMask.localPosition.x, newPosition.x, 1.09f)
        .setEase(LeanTweenType.linear)
        .setOnUpdate((value) =>
        {
            newPosition.x = value;
            timeProgressBarMask.localPosition = newPosition;
        });

    }

    public void SetTimeProgressBarValue(int fillAmount)
    {
        totalTime = fillAmount;
        
        if (fillAmount <= 0)
            fillAmount = 1;

        increment = fullProgressBarAmount / fillAmount;
        positionIncrement = (-0.745f - positionCorrection) / fillAmount;
    }

    public void SetActive(bool v)
    {
        gameObject.SetActive(v);
        if (v)
        {
            newScale.x = 0;
            timeProgressBarMask.localScale = newScale;
        }
    }
}
