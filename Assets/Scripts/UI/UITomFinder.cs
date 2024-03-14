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

        if (world.mainPlayer.inTransport)
        {
            for (int i = 0; i < world.transportList.Count; i++)
            {
                if (world.transportList[i].hasKoa)
                {
                    world.transportList[i].CenterCamera();
                    unitMovement.HandleUnitSelectionAndMovement(world.transportList[i].transform.position, world.transportList[i].gameObject);
					break;
				}
            }
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
