using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITomFinder : MonoBehaviour
{
    [SerializeField]
    private UnitMovement unitMovement;
    [SerializeField]
    private CityBuilderManager cityBuilderManager;
    [SerializeField]
    private MapWorld world;
    [SerializeField]
    public RectTransform allContents;
    [SerializeField]
    private Button button;

    public void FindTom()
    {
        if (world.unitOrders || world.buildingWonder)
            return;

        world.cityBuilderManager.PlaySelectAudio();
        world.CloseResearchTree();
        world.CloseConversationList();
        world.CloseWonders();
        world.CloseMap();
        world.CloseTerrainTooltipButton();
        world.CloseImprovementTooltipButton();
        world.CloseCampTooltipButton();
        world.CloseTradeRouteBeginTooltipButton();
		cityBuilderManager.ResetCityUI();
        if (world.mainPlayer.isSelected)
            unitMovement.ClearSelection();
        unitMovement.HandleUnitSelectionAndMovement(world.mainPlayer.transform.position, world.mainPlayer.gameObject);
        world.mainPlayer.CenterCamera();
    }

    public void ToggleButtonOn(bool v)
    {
        button.enabled = v;
    }
}
