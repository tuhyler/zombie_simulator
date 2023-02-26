using UnityEngine;
using TMPro;

public class TimeProgressBar : MonoBehaviour
{
    [SerializeField]
    private TMP_Text timeText;

    [SerializeField]
    private Transform timeProgressBarMask;

    public float fullProgressBarAmount = .77f;
    private float positionCorrectionAtBeginning = -.71f; //progress bar moves slightly to the right as it fills up, so this corrects for that.
    private float positionCorrectionAtEnd = -.745f;
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
        ResetProgressBar();
        SetActive(false);
    }

    //public void SetActiveTime(int time)
    //{
    //    timeText.text = additionalText + string.Format("{0:00}:{1:00}", time / 60, time % 60);
    //    Vector3 tempScale = timeProgressBarMask.localScale;
    //    tempScale.x = time * increment;
    //    timeProgressBarMask.localScale = tempScale;
    //}

    public void SetTime(int time)
    {
        timeText.text = additionalText + string.Format("{0:00}:{1:00}", time / 60, time % 60);
        newScale.x = (totalTime - time) * increment + increment;
        newPosition.x = positionCorrectionAtBeginning + ((totalTime - time) * positionIncrement + positionIncrement);
        //setting the last increment
        //if (timeProgressBarMask.localScale.x > totalTime * increment - increment)
        if (time == 0)
        {
            Vector3 tempScale = timeProgressBarMask.localScale;
            tempScale.x = 0;
            timeProgressBarMask.localScale = tempScale;

            Vector3 tempPosition = timeProgressBarMask.localPosition;
            tempPosition.x = positionCorrectionAtBeginning;
            timeProgressBarMask.localPosition = tempPosition;
            //timeProgressBarMask.localScale = newScale;
        }

        LeanTween.value(timeProgressBarMask.gameObject, timeProgressBarMask.localScale.x, newScale.x, 1.09f) //just over one second
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

    public void SetProgressBarMask(int time)
    {
        Vector3 tempScale = timeProgressBarMask.localScale;
        tempScale.x = (totalTime - time) * increment;
        timeProgressBarMask.localScale = tempScale;
    }

    public void SetProgressBarBeginningPosition()
    {
        newPosition.x = positionCorrectionAtBeginning;
        timeProgressBarMask.localPosition = newPosition;
    }

    public void SetTimeProgressBarValue(int fillAmount)
    {
        totalTime = fillAmount;
        
        if (fillAmount <= 0)
            fillAmount = 1;

        increment = fullProgressBarAmount / fillAmount;
        positionIncrement = (positionCorrectionAtEnd - positionCorrectionAtBeginning) / fillAmount;
    }

    public void SetActive(bool v)
    {
        if (v)
        {
            newScale.x = 0;
            timeProgressBarMask.localScale = newScale;
        }

        gameObject.SetActive(v);
    }

    public void SetToZero()
    {
        LeanTween.cancel(timeProgressBarMask.gameObject);
        timeText.text = additionalText + string.Format("{0:00}:{1:00}", 0 / 60, 0 % 60);
        newScale.x = fullProgressBarAmount;
        timeProgressBarMask.localScale = newScale;
        newPosition.x = positionCorrectionAtEnd;
        timeProgressBarMask.localPosition = newPosition;
    }

    public void ResetProgressBar()
    {
        newScale.x = 0;
        newPosition.x = positionCorrectionAtBeginning;
        timeProgressBarMask.localPosition = newPosition;
        timeProgressBarMask.localScale = newScale;
    }
}
