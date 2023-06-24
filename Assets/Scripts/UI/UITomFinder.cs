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

    private void Awake()
    {
        worker = FindObjectOfType<Worker>();
    }

    public void FindTom()
    {
        world.CloseResearchTree();
        world.CloseWonders();
        world.CloseMap();
        world.CloseTerrainTooltip();
        world.CloseImprovementTooltip();
        cityBuilderManager.ResetCityUI();
        if (unitMovement.SelectedWorker != null)
            unitMovement.ClearSelection();
        unitMovement.HandleUnitSelectionAndMovement(worker.transform.position, worker.gameObject);
        worker.CenterCamera();
    }
}
