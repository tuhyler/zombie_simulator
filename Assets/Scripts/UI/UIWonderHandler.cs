using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIWonderHandler : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;
    
    private WonderDataSO buildData;
    
    [SerializeField]
    private UnityEvent<WonderDataSO> OnIconButtonClick;
   
    [SerializeField]
    private Transform uiElementsParent;
    private List<UIWonderOptions> buildOptions;

    [SerializeField]
    public ScrollRect optionsScroller;

    [SerializeField]
    private UIScrollButton scrollLeft, scrollRight;

    //for tweening
    [SerializeField]
    private RectTransform allContents;
    private Vector3 originalLoc;
    [HideInInspector]
    public bool activeStatus; //set this up so we don't have to wait for tween to set inactive


    private void Awake()
    {
        gameObject.SetActive(false);

        buildOptions = new List<UIWonderOptions>();

        foreach (Transform selection in uiElementsParent)
        {
            buildOptions.Add(selection.GetComponent<UIWonderOptions>());
        }

        originalLoc = allContents.anchoredPosition3D;
    }

    private void Update()
    {
        if (scrollLeft != null)
        {
            if (scrollLeft.isDown)
            {
                ScrollLeft();
            }
        }

        if (scrollRight != null)
        {
            if (scrollRight.isDown)
            {
                ScrollRight();
            }
        }
    }

    private void ScrollLeft()
    {
        if (optionsScroller.horizontalNormalizedPosition >= 0f)
        {
            optionsScroller.horizontalNormalizedPosition -= 0.05f;
        }
    }

    private void ScrollRight()
    {
        if (optionsScroller.horizontalNormalizedPosition <= 1f)
        {
            optionsScroller.horizontalNormalizedPosition += 0.05f;
        }
    }

    public void HandleButtonClick()
    {
        OnIconButtonClick?.Invoke(buildData);
    }

    public void ToggleVisibility(bool v, ResourceManager resourceManager = null) //pass resources to know if affordable in the UI (optional)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            world.UnselectAll();
            gameObject.SetActive(v);
            activeStatus = true;
            allContents.anchoredPosition3D = originalLoc + new Vector3(0, -200f, 0);

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 200f, 0.4f).setEaseOutBack();
            LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();

            if (resourceManager != null)
            {
                PrepareBuildOptions(resourceManager);
            }
        }
        else
        {
            activeStatus = false;
            LeanTween.alpha(allContents, 0f, 0.2f).setEaseLinear();
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - 300f, 0.2f).setOnComplete(SetActiveStatusFalse);
        }
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void PrepareBuild(WonderDataSO buildData)
    {
        this.buildData = buildData;
    }

    private void PrepareBuildOptions(ResourceManager resourceManager)
    {
        List<string> improvementSingleBuildList = resourceManager.city.singleBuildImprovementsBuildingsDict.Keys.ToList();

        foreach (UIWonderOptions buildItem in buildOptions)
        {
            if (buildItem == null)
                continue;

            string itemName = buildItem.BuildData.wonderName;
            List<ResourceValue> resourceCosts = new(buildItem.BuildData.wonderCost);
            bool locked = buildItem.BuildData.locked;

            buildItem.ToggleVisibility(true); //turn them all on initially, so as to not turn them on when things change

            if (locked || improvementSingleBuildList.Contains(itemName))
            {
                buildItem.ToggleVisibility(false);
                continue;
            }
        }
    }
}
