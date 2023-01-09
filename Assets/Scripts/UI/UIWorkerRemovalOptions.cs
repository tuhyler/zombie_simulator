using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIWorkerRemovalOptions : MonoBehaviour
{
    [SerializeField]
    private GameObject allButton, roadButton, liquidButton, powerButton;

    //for tweening
    [SerializeField]
    private RectTransform allContents;
    private Vector3 originalLoc;
    private bool activeStatus; //set this up so we don't have to wait for tween to set inactive

    private void Awake()
    {
        allButton.SetActive(false);
        liquidButton.SetActive(false);
        powerButton.SetActive(false);

        gameObject.SetActive(false);
        originalLoc = allContents.anchoredPosition3D;
    }

    public void ToggleVisibility(bool val, bool suddenly) //pass resources to know if affordable in the UI (optional)
    {
        if (activeStatus == val)
            return;

        LeanTween.cancel(gameObject);

        if (val)
        {
            gameObject.SetActive(val);
            activeStatus = true;
            allContents.anchoredPosition3D = originalLoc + new Vector3(0, -100f, 0);

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 100f, 0.4f).setEaseOutBack();
            //LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();
        }
        else
        {
            activeStatus = false;
            if (suddenly)
                LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - 300f, 0.2f).setOnComplete(SetActiveStatusFalse);
            else
                LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - 100f, 0.2f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void RemoveAll()
    {

    }

    public void RemoveRoad()
    {

    }

    public void RemoveLiquid()
    {

    } 

    public void RemovePower()
    {

    }
}
