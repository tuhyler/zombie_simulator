using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class UIWonderHandler : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;

    [SerializeField]
    private CameraController cameraController;

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

    //for blurring background
    [SerializeField]
    private Volume globalVolume;
    private DepthOfField dof;

    //for tweening
    [SerializeField]
    private RectTransform allContents;
    private Vector3 originalLoc;
    [HideInInspector]
    public bool activeStatus; //set this up so we don't have to wait for tween to set inactive


    private void Awake()
    {
        gameObject.SetActive(false);

        if (globalVolume.profile.TryGet<DepthOfField>(out DepthOfField tmpDof))
        {
            dof = tmpDof;
        }

        dof.focalLength.value = 15;

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

    public void ToggleVisibility(bool v) //pass resources to know if affordable in the UI (optional)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            world.UnselectAll();
            gameObject.SetActive(v);
            activeStatus = true;
            allContents.anchoredPosition3D = originalLoc + new Vector3(0, 1200f, 0);

            LeanTween.value(globalVolume.gameObject, dof.focalLength.value, 45, 0.5f)
            .setEase(LeanTweenType.easeOutSine)
            .setOnUpdate((value) =>
            {
                dof.focalLength.value = value;
            });
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -1200f, 0.5f).setEaseOutSine();
            //LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();

            PrepareBuildOptions();
        }
        else
        {
            activeStatus = false;

            LeanTween.value(globalVolume.gameObject, dof.focalLength.value, 15, 0.3f)
            .setEase(LeanTweenType.easeOutSine)
            .setOnUpdate((value) =>
            {
                dof.focalLength.value = value;
            });
            //LeanTween.alpha(allContents, 0f, 0.2f).setEaseLinear();
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 1200f, 0.3f).setOnComplete(SetActiveStatusFalse);
        }

        cameraController.enabled = !v;
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void PrepareBuild(WonderDataSO buildData)
    {
        this.buildData = buildData;
    }

    private void PrepareBuildOptions()
    {
        foreach (UIWonderOptions buildItem in buildOptions)
        {
            if (buildItem == null)
                continue;

            string itemName = buildItem.BuildData.wonderName;
            List<ResourceValue> resourceCosts = new(buildItem.BuildData.wonderCost);
            bool locked = buildItem.BuildData.locked;

            buildItem.ToggleVisibility(true); //turn them all on initially, so as to not turn them on when things change

            if (locked || world.GetWondersConstruction("Wonder - " + itemName))
            {
                buildItem.ToggleVisibility(false);
                continue;
            }
        }
    }
}
