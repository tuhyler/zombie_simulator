using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIImprovementBuildPanel : MonoBehaviour
{
    [SerializeField]
    private TMP_Text text;

    [SerializeField] //for tweening
    private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus;
    private Vector3 originalLoc;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public void SetText(string text)
    {
        this.text.text = text;
    }

    public void ToggleVisibility(bool v)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            gameObject.SetActive(true);

            activeStatus = true;

            allContents.anchoredPosition3D = originalLoc + new Vector3(0, 100f, 0);

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - 100f, 0.3f).setEaseOutSine();
            //LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear();
        }
        else
        {
            activeStatus = false;
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 100f, 0.2f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }
}
