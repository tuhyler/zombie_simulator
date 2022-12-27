using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIBuildingSomething : MonoBehaviour
{
    [SerializeField]
    private TMP_Text text;

    //for tweening
    [SerializeField]
    private RectTransform allContents;
    private Vector3 originalLoc;
    private bool activeStatus;

    private void Awake()
    {
        gameObject.SetActive(false);
        originalLoc = allContents.anchoredPosition3D;
    }

    public void ToggleVisibility(bool val) //pass resources to know if affordable in the UI (optional)
    {
        if (activeStatus == val)
            return;

        LeanTween.cancel(gameObject);

        if (val)
        {
            gameObject.SetActive(val);
            activeStatus = true;
            allContents.anchoredPosition3D = originalLoc + new Vector3(0, -200f, 0);

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 200f, 0.4f).setEaseOutBack();
            LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();
        }
        else
        {
            activeStatus = false;
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - 200f, 0.2f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void SetText(string text)
    {
        this.text.text = text;
    }
}
