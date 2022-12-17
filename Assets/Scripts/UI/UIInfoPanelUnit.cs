using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIInfoPanelUnit : MonoBehaviour //This script is for populating the panel and switching it off/on
{
    [SerializeField]
    private TextMeshProUGUI nameText;
    //[SerializeField]
    //private Image infoImage;
    [SerializeField]
    private TMP_Text currentMovePoints, regMovePoints;

    [SerializeField] //for tweening
    private RectTransform allContents;
    private bool activeStatus;
    private Vector3 originalLoc;

    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;

        gameObject.SetActive(false);
    }

    public void SetData(/*Sprite sprite, */string text)
    {
        nameText.text = text;
        ////infoImage.sprite = sprite;
        //currentMovePoints.text = Mathf.Max(currentMP,0).ToString(); //can't have less than 0
        //regMovePoints.text = regMP.ToString();
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
