using System.Collections.Generic;
using UnityEngine;

public class UIWorkerHandler : MonoBehaviour
{
    //private ImprovementDataSO buildData;

    //[SerializeField]
    //private UnityEvent<ImprovementDataSO> OnIconButtonClick;
    [SerializeField]
    public MapWorld world;

    [SerializeField]
    private UIWorkerRemovalOptions uiWorkerBuildOptions, uiWorkerRemovalOptions;

    [SerializeField]
    private Transform uiElementsParent;
    [HideInInspector]
    public List<UIWorkerOptions> buildOptions;
    [SerializeField]
    private List<UIWorkerOptions> removalOptions;
    private List<string> buttonsToExclude = new() { "Road", "Remove", "Clear", "LoadUnload"};
    [HideInInspector]
    public UIWorkerOptions uiLoadUnload;

    //for tweening
    [SerializeField]
    private RectTransform allContents;
    private Vector3 originalLoc;
    private bool activeStatus; //set this up so we don't have to wait for tween to set inactive

    private void Awake()
    {
        gameObject.SetActive(false); 

        buildOptions = new List<UIWorkerOptions>(); 

        foreach (Transform selection in uiElementsParent)
        {
            UIWorkerOptions button = selection.GetComponent<UIWorkerOptions>();
			buildOptions.Add(button);
            if (button.buttonName == "LoadUnload")
                uiLoadUnload = button;
        }

        originalLoc = allContents.anchoredPosition3D;
        uiWorkerRemovalOptions.world = world;
    }

    public void ToggleRemovalOptions(bool v, bool remove)
    {
        world.cityBuilderManager.PlaySelectAudio();

        if (remove)
            uiWorkerRemovalOptions.ToggleVisibility(v, false);
        else
			uiWorkerBuildOptions.ToggleVisibility(v, false);
	}

    public void ToggleVisibility(bool val, MapWorld world, bool temporary = false) //pass resources to know if affordable in the UI (optional), pass world for canvas
    {
        if (activeStatus == val)
            return;

        LeanTween.cancel(gameObject);

        if (val)
        {
            world.workerCanvas.gameObject.SetActive(true);
            world.personalResourceCanvas.gameObject.SetActive(true);
            foreach (UIWorkerOptions option in removalOptions)
                option.ToggleColor(false);
            gameObject.SetActive(val);
            activeStatus = true;
            allContents.anchoredPosition3D = originalLoc + new Vector3(0, -600f, 0);
            
            if (world.tutorialGoing)
            {
                for (int i = 0; i < buildOptions.Count; i++)
                {
                    if (buildOptions[i].isFlashing)
                        StartCoroutine(world.EnableButtonHighlight(buildOptions[i].transform, true, false));
                }
            }

            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 600f, 0.4f).setEaseOutBack();
        }
        else
        {
            activeStatus = false;
            uiWorkerBuildOptions.ToggleVisibility(false, true);
            uiWorkerRemovalOptions.ToggleVisibility(false, true);
            LeanTween.moveY(allContents, allContents.anchoredPosition3D.y - 200f, 0.2f).setOnComplete(() => SetActiveStatusFalse(world, temporary));
        }
    }

    public void DeactivateButtons()
    {
		for (int i = 0; i < buildOptions.Count; i++)
		{
			if (buttonsToExclude.Contains(buildOptions[i].buttonName))
				buildOptions[i].gameObject.SetActive(false);
		}
	}

	public void ReactivateButtons()
    {
		for (int i = 0; i < buildOptions.Count; i++)
		{
			if (buttonsToExclude.Contains(buildOptions[i].buttonName))
				buildOptions[i].gameObject.SetActive(true);
		}
	}

    private void SetActiveStatusFalse(MapWorld world, bool temporary)
    {
        gameObject.SetActive(false);
        world.workerCanvas.gameObject.SetActive(false);

        if (!temporary && (!world.unitMovement.selectedUnit || !world.unitMovement.selectedUnit.trader))
            world.personalResourceCanvas.gameObject.SetActive(false);
    }

    public UIWorkerOptions GetButton(string buttonName)
    {
        for (int i = 0; i < buildOptions.Count; i++)
        {
            if (buildOptions[i].buttonName == buttonName)
                return buildOptions[i];
        }

        return null;
    }
}
