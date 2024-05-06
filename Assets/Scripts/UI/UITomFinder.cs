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
        world.CloseTransferTooltip();
        world.CloseImprovementTooltipButton();
        world.CloseCampTooltipButton();
        world.CloseTradeRouteBeginTooltipButton();
		cityBuilderManager.ResetCityUI();
        cityBuilderManager.UnselectWonder();
        cityBuilderManager.UnselectTradeCenter();

        if (world.mainPlayer.isSelected || (world.mainPlayer.inTransport && world.GetKoasTransport().isSelected))
            unitMovement.ClearSelection();

        if (world.mainPlayer.inTransport)
        {
            Transport transport = world.GetKoasTransport();
            transport.CenterCamera();
            unitMovement.HandleUnitSelectionAndMovement(transport.transform.position, transport.gameObject);
        }
        else
        {
            world.mainPlayer.CenterCamera();
			unitMovement.HandleUnitSelectionAndMovement(world.mainPlayer.transform.position, world.mainPlayer.gameObject);
		}
    }

    public void ToggleButtonOn(bool v)
    {
        button.enabled = v;
    }
}
