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

    private UILaborHandler buttonHandler;
    private ButtonHighlight highlight;

    [SerializeField]
    private CanvasGroup canvasGroup; //handles all aspects of a group of UI elements together, instead of individually in Unity

    private void Awake()
    {
        buildingName = buildingTMP.text;
        buttonHandler = GetComponentInParent<UILaborHandler>();
        canvasGroup = GetComponent<CanvasGroup>();
        highlight = GetComponent<ButtonHighlight>();
        SetUICount();
    }

    public void CheckVisibility(Vector3Int cityTile, MapWorld world)
    {
        if (world.IsBuildingInCity(cityTile, buildingName))
        {
            currentLabor = world.GetCurrentLaborForBuilding(cityTile, buildingName);
            maxLabor = world.GetMaxLaborForBuilding(cityTile, buildingName);
            gameObject.SetActive(true);
            SetUICount();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

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
