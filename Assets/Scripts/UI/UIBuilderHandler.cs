using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class UIBuilderHandler : MonoBehaviour
{
    private ImprovementDataSO buildData;
    private UnitBuildDataSO unitBuildData;
    private CityBuilderManager cityBuilderManager;

    [SerializeField]
    private CameraController cameraController;

    [SerializeField]
    private UnityEvent<ImprovementDataSO> OnIconButtonClick;
    [SerializeField]
    private UnityEvent<UnitBuildDataSO> OnUnitIconButtonClick;

    [SerializeField]
    private Transform uiElementsParent;
    private List<UIBuildOptions> buildOptions;

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

    //public bool showResourceProduced, showResourceConsumed;

    [SerializeField]
    public ScrollRect optionsScroller;

    [SerializeField]
    private UIScrollButton scrollLeft, scrollRight;

    [HideInInspector]
    public bool isQueueing;

    //for updating resources
    private int maxResource;
    private int maxLabor;
    private int maxGold;

    //for object pooling
    //private Queue<UIResourceInfoPanel> resourceInfoPanelQueue = new();
    //[SerializeField]
    //private GameObject resourceInfoPanel;

    //public bool isUnit; //flag indicating if units will be built using this UI

    //[SerializeField]
    //public CanvasGroup uiBuildGroup;

    private void Awake()
    {
        gameObject.SetActive(false); //Hide to start

        if (globalVolume.profile.TryGet<DepthOfField>(out DepthOfField tmpDof))
        {
            dof = tmpDof;
        }
        dof.focalLength.value = 15;

        buildOptions = new List<UIBuildOptions>(); //instantiate

        foreach (Transform selection in uiElementsParent) //populate list
        {
            buildOptions.Add(selection.GetComponent<UIBuildOptions>());
        }

        originalLoc = allContents.anchoredPosition3D;
        cityBuilderManager = FindObjectOfType<CityBuilderManager>();

        //GrowResourceInfoPanelPool();
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

    public void HandleUnitButtonClick()
    {
        OnUnitIconButtonClick?.Invoke(unitBuildData);
    }

    public void ToggleVisibility(bool v, ResourceManager resourceManager = null) //pass resources to know if affordable in the UI (optional)
    {
        if (activeStatus == v)
            return;

        LeanTween.cancel(gameObject);

        if (v)
        {
            cityBuilderManager.world.immoveableCanvas.gameObject.SetActive(true);
            gameObject.SetActive(v);
            activeStatus = true;
            cityBuilderManager.buildOptionsActive = true;
            cityBuilderManager.activeBuilderHandler = this;

            //LeanTween.value(globalVolume.gameObject, dof.focalLength.value, 45, 0.4f)
            //.setEase(LeanTweenType.easeOutSine)
            //.setOnUpdate((value) =>
            //{
            //    dof.focalLength.value = value;
            //});

            dof.focalLength.value = 45;

            allContents.anchoredPosition3D = originalLoc + new Vector3(0, -200f, 0);

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 200f, 0.4f).setEaseOutBack();
            //LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();

            if (resourceManager != null)
            {
                PrepareBuildOptions(resourceManager);
            }

            //if (buildOptions.Count <= 5) //hiding the scroll buttons if not enough options to scroll
            //{
            //    scrollLeft.gameObject.SetActive(false);
            //    scrollRight.gameObject.SetActive(false);
            //}
            //else
            //{
            //    scrollLeft.gameObject.SetActive(true);
            //    scrollRight.gameObject.SetActive(true);
            //}
        }
        else
        {
            cityBuilderManager.world.immoveableCanvas.gameObject.SetActive(false);
            activeStatus = false;
            maxResource = 0;
            maxLabor = 0;
            maxGold = 0;
            cityBuilderManager.buildOptionsActive = false;
            cityBuilderManager.activeBuilderHandler = null;

            //dof.focalLength.value = 15;
            //LeanTween.value(globalVolume.gameObject, dof.focalLength.value, 15, 0.35f)
            //.setEase(LeanTweenType.easeOutSine)
            //.setOnUpdate((value) =>
            //{
            //    dof.focalLength.value = value;
            //});

            dof.focalLength.value = 15;
            //LeanTween.alpha(allContents, 0f, 0.2f).setEaseLinear();
            //LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - 600f, 0.35f).setOnComplete(SetActiveStatusFalse);
            gameObject.SetActive(false);
        }

        cameraController.enabled = !v;
    }

    private void SetActiveStatusFalse()
    {
        gameObject.SetActive(false);
    }

    public void PrepareBuild(ImprovementDataSO buildData)
    {
        this.buildData = buildData;
    }

    public void PrepareUnitBuild(UnitBuildDataSO unitBuildData)
    {
        this.unitBuildData = unitBuildData;
    }

    public void PrepareBuildOptions(ResourceManager resourceManager)
    {
        List<string> improvementSingleBuildList = resourceManager.city.singleBuildImprovementsBuildingsDict.Keys.ToList();

        foreach (UIBuildOptions buildItem in buildOptions)
        {
            if (buildItem == null)
                continue;

            string itemName = "";
            List<ResourceValue> resourceCosts = new();
            bool locked = false;

            if (buildItem.UnitBuildData != null)
            {
                itemName = buildItem.UnitBuildData.unitName;
                resourceCosts = new(buildItem.UnitBuildData.unitCost);
                locked = buildItem.UnitBuildData.locked;

                ResourceValue laborCost;
                laborCost.resourceType = ResourceType.Labor;
                laborCost.resourceAmount = buildItem.UnitBuildData.laborCost;
                resourceCosts.Add(laborCost);
            }
            else if (buildItem.BuildData != null)
            {
                itemName = buildItem.BuildData.improvementName;
                resourceCosts = new(buildItem.BuildData.improvementCost);
                locked = buildItem.BuildData.Locked;
            }

            buildItem.ToggleVisibility(true); //turn them all on initially, so as to not turn them on when things change

            if (locked || improvementSingleBuildList.Contains(itemName))
            {
                buildItem.ToggleVisibility(false);
                continue;
            }

            //buildItem.ToggleInteractable(true);
            buildItem.SetResourceTextToDefault();

            foreach (ResourceValue item in resourceCosts)
            {
                if (item.resourceType == ResourceType.Gold)
                {
                    if (item.resourceAmount > maxGold)
                        maxGold = item.resourceAmount;
                    
                    if (!resourceManager.city.CheckWorldGold(item.resourceAmount))
                        buildItem.SetResourceTextToRed(item);
                }
                else if (item.resourceType == ResourceType.Labor)
                {
                    if (item.resourceAmount > maxLabor)
                        maxLabor = item.resourceAmount;

                    int pop = resourceManager.city.cityPop.CurrentPop;
                    if (pop < item.resourceAmount)
                        buildItem.SetResourceTextToRed(item);
                }
                else if (!resourceManager.CheckResourceAvailability(item))
                {
                    if (item.resourceAmount > maxResource)
                        maxResource = item.resourceAmount;

                    buildItem.SetResourceTextToRed(item);
                }
            }
        }
    }

    public void UpdateBuildOptions(ResourceType type, int prevAmount, int currentAmount, bool pos, ResourceManager resourceManager)
    {
        //checking if updating is necessary
        if (type == ResourceType.Gold)
        {
            if (pos)
            {
                if (prevAmount > maxGold)
                    return;
            }
            else
            {
                if (currentAmount > maxGold)
                    return;
            }
        }
        else if (type == ResourceType.Labor)
        {
            if (pos)
            {
                if (prevAmount > maxLabor)
                    return;
            }
            else
            {
                if (currentAmount > maxLabor)
                    return;
            }
        }
        else
        {
            if (pos)
            {
                if (prevAmount > maxResource)
                    return;
            }
            else
            {
                if (currentAmount > maxResource)
                    return;
            }
        }

        PrepareBuildOptions(resourceManager);
    }
}
