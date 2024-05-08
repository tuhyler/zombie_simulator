using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class UIWonderHandler : MonoBehaviour, IImmoveable
{
    [SerializeField]
    public MapWorld world;

    [SerializeField]
    private CameraController cameraController;

    private WonderDataSO buildData;
    
    [SerializeField]
    private UnityEvent<WonderDataSO> OnIconButtonClick;
   
    [SerializeField]
    public Transform objectHolder, finalSpaceHolder;
    [HideInInspector]
    public List<UIWonderOptions> buildOptions = new();

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
    public bool activeStatus, somethingNew; //set this up so we don't have to wait for tween to set inactive


    private void Awake()
    {
        gameObject.SetActive(false);

        if (globalVolume.profile.TryGet<DepthOfField>(out DepthOfField tmpDof))
        {
            dof = tmpDof;
        }

        dof.focalLength.value = 15;

        //buildOptions = new List<UIWonderOptions>();

        //foreach (Transform selection in uiElementsParent)
        //{
        //    buildOptions.Add(selection.GetComponent<UIWonderOptions>());
        //}

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
            //world.openingImmoveable = true;
            world.immoveableCanvas.gameObject.SetActive(true);
            world.iImmoveable = this;
			world.BattleCamCheck(true);
			activeStatus = true;
            allContents.anchoredPosition3D = originalLoc + new Vector3(0, 1200f, 0);
            world.tooltip = false;
            world.somethingSelected = true;

            LeanTween.value(globalVolume.gameObject, dof.focalLength.value, 45, 0.4f)
            .setEase(LeanTweenType.easeOutSine)
            .setOnUpdate((value) =>
            {
                dof.focalLength.value = value;
            });
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -1200f, 0.4f).setEaseOutSine();
            //LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();

            PrepareBuildOptions();
        }
        else
        {
			world.BattleCamCheck(false);
			activeStatus = false;
            world.iImmoveable = null;

            if (somethingNew)
            {
                somethingNew = false;
                world.wonderButton.newIcon.SetActive(false);

                for (int i = 0; i < buildOptions.Count; i++)
                {
                    if (buildOptions[i].somethingNew)
                    {
                        buildOptions[i].ToggleSomethingNew(false);
                        world.newUnitsAndImprovements.Remove(buildOptions[i].BuildData.wonderName);
                    }
                }
            }

            LeanTween.value(globalVolume.gameObject, dof.focalLength.value, 15, 0.35f)
            .setEase(LeanTweenType.easeOutSine)
            .setOnUpdate((value) =>
            {
                dof.focalLength.value = value;
            });
            //LeanTween.alpha(allContents, 0f, 0.2f).setEaseLinear();
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 1200f, 0.35f).setOnComplete(SetActiveStatusFalse);
        }

        cameraController.enabled = !v;
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
		if (world.iImmoveable == null)
			world.immoveableCanvas.gameObject.SetActive(false);
		//world.ImmoveableCheck();
		//     if (!world.openingImmoveable)
		//         world.immoveableCanvas.gameObject.SetActive(false);
		//     else
		//world.openingImmoveable = false;
	}

	//private void OpeningComplete()
	//{
	//    world.openingImmoveable = false;
	//}

	public void FinishMenuSetup()
	{
		finalSpaceHolder.SetAsLastSibling();

		foreach (Transform selection in objectHolder) //populate list
		{
			if (selection.TryGetComponent(out UIWonderOptions option))
				buildOptions.Add(option);
		}
	}

	public void PrepareBuild(WonderDataSO buildData)
    {
        this.buildData = buildData;
    }

    private void PrepareBuildOptions()
    {
        for (int i = 0; i < buildOptions.Count; i++)
        {
			if (buildOptions[i] == null)
				continue;

			//string itemName = buildItem.BuildData.wonderDisplayName;
			//List<ResourceValue> resourceCosts = new(buildOptions[i].BuildData.wonderCost);
			bool locked = buildOptions[i].locked;

			buildOptions[i].ToggleVisibility(true); //turn them all on initially, so as to not turn them on when things change

			if (locked || world.TradeStopNameExists(buildOptions[i].BuildData.wonderName))
			{
				buildOptions[i].ToggleVisibility(false);
				continue;
			}
		}
    }
}
