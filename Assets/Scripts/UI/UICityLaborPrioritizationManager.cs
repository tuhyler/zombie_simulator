using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UICityLaborPrioritizationManager : MonoBehaviour
{
    [SerializeField]
    private GameObject uiLaborResourcePriority;

    [SerializeField]
    private Transform resourceHolder;

    //for generating resource lists
    private List<TMP_Dropdown.OptionData> resources = new();

    [SerializeField] //for tweening
    private RectTransform allContents;
    [HideInInspector]
    public bool activeStatus;
    private Vector3 originalLoc;


    private void Awake()
    {
        originalLoc = allContents.anchoredPosition3D;
        AddResources();
        gameObject.SetActive(false);
    }

    public void AddResources()
    {
        foreach (ResourceIndividualSO resource in ResourceHolder.Instance.allStorableResources)
        {
            resources.Add(new TMP_Dropdown.OptionData(resource.resourceName, resource.resourceIcon));
        }
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

            allContents.anchoredPosition3D = originalLoc + new Vector3(-600f, 0, 0);

            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + 600f, 0.3f).setEaseOutSine();
            LeanTween.alpha(allContents, 1f, 0.3f).setFrom(0f).setEaseLinear();
        }
        else
        {
            activeStatus = false;
            LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + -600f, 0.3f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }


    public void SetLaborPriorities()
    {

    }
}
