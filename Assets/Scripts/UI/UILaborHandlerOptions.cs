using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UILaborHandlerOptions : MonoBehaviour
{
    [SerializeField]
    private TMP_Text laborCountAndMax;

    [SerializeField]
    private TMP_Text buildingTMP;

    private string buildingName;
    public string GetBuildingName { get { return buildingName; } }

    private int currentLabor;
    public int GetCurrentLabor { get { return currentLabor; } }
    private int maxLabor;
    public int SetMaxLabor { set { maxLabor = value; } }

    public bool noneText; //flag if it's none text, shown for no buildings

    [HideInInspector]
    public bool removingOption;

    private UILaborHandler buttonHandler;
    private ButtonHighlight highlight;

    [SerializeField]
    private CanvasGroup canvasGroup; //handles all aspects of a group of UI elements together, instead of individually in Unity

    private void Awake()
    {
        if (!noneText)
        {
            buildingName = buildingTMP.text;
            //buildingTMP.text = buildingName.Remove(buildingName.Length - 2); //just displaying simplified name
            buttonHandler = GetComponentInParent<UILaborHandler>();
            canvasGroup = GetComponent<CanvasGroup>();
            highlight = GetComponent<ButtonHighlight>();
            SetUICount();
        }
    }

    public void ToggleVisibility(bool v)
    {
        gameObject.SetActive(v);
    }

    //public int CheckVisibility(Vector3Int cityTile, MapWorld world)
    //{
    //    if (world.IsBuildingInCity(cityTile, buildingName))
    //    {
    //        currentLabor = world.GetCurrentLaborForBuilding(cityTile, buildingName);
    //        maxLabor = world.GetMaxLaborForBuilding(cityTile, buildingName);

    //        if (!removingOption && maxLabor == 0)
    //        {
    //            ToggleVisibility(false);
    //            return 0;
    //        }

    //        gameObject.SetActive(true);
    //        SetUICount();
    //        return 1;
    //    }
    //    else
    //    {
    //        gameObject.SetActive(false);
    //        return 0;
    //    }
    //}

    public void ToggleInteractable(bool v)
    {
        canvasGroup.interactable = v;
    }

    public void OnButtonClick()
    {
        buttonHandler.PrepareLaborChange(currentLabor, maxLabor, buildingName);
        currentLabor += buttonHandler.GetLaborChange();
        SetUICount();
    }

    public bool CheckLaborIsMaxxed()
    {
        return currentLabor == maxLabor;
    }

    private void SetUICount()
    {
        laborCountAndMax.text = $"{currentLabor}/{maxLabor}";
        if (maxLabor == 0)
            laborCountAndMax.gameObject.SetActive(false);
        else
            laborCountAndMax.gameObject.SetActive(true);
    }

    public void EnableHighlight(Color highlightColor)
    {
        highlight.EnableHighlight(highlightColor);
    }

    public void DisableHighlight()
    {
        highlight.DisableHighlight();
    }
}
