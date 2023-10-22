using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITomFinder : MonoBehaviour
{
    [SerializeField]
    private UnitMovement unitMovement;
    [SerializeField]
    private CityBuilderManager cityBuilderManager;
    [SerializeField]
    private MapWorld world;
    private Worker worker;
    [SerializeField]
    public RectTransform allContents;

    private void Awake()
    {
        worker = FindObjectOfType<Worker>();
    }

    public void FindTom()
    {
        if (world.unitOrders || world.buildingWonder)
            return;

        world.cityBuilderManager.PlaySelectAudio(true);
        world.CloseResearchTree();
        world.CloseWonders();
        world.CloseMap();
        world.CloseTerrainTooltipButton();
        world.CloseImprovementTooltipButton();
        world.CloseCampTooltipButton();
        world.CloseTradeRouteBeginTooltipButton();
		cityBuilderManager.ResetCityUI();
        if (unitMovement.SelectedWorker != null)
            unitMovement.ClearSelection();
        unitMovement.HandleUnitSelectionAndMovement(worker.transform.position, worker.gameObject);
        worker.CenterCamera();
    }
}
