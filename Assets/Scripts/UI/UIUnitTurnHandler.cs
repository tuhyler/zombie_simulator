using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIUnitTurnHandler : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;

    [HideInInspector]
    public UnitTurnListHandler turnHandler;
    [SerializeField]
    public List<Button> buttons;

    [HideInInspector]
    public int currentListIndex;

    private void Awake()
    {
        turnHandler = GetComponent<UnitTurnListHandler>();
    }

    private void SelectUnit(Unit unit) => turnHandler.SelectUnit(unit);

    public void NextUnitToMove() //used on right button
    {
        if (world.unitOrders)
            return;

		world.cityBuilderManager.PlaySelectAudio();
		IncreaseIndex();
        SelectUnit(turnHandler.GetFromTurnList(currentListIndex));
        world.cityBuilderManager.ResetCityUI();
    }

    public void PrevUnitToMove() //used on left button
    {
        if (world.unitOrders)
            return;

        world.cityBuilderManager.PlaySelectAudio();
        DecreaseIndex();
        SelectUnit(turnHandler.GetFromTurnList(currentListIndex));
        world.cityBuilderManager.ResetCityUI();
    }

    private void IncreaseIndex()
    {
        int unitListLength = turnHandler.CountOfList();
        if (currentListIndex >= unitListLength - 1)
            currentListIndex = 0;
        else
            currentListIndex++;
    }

    private void DecreaseIndex()
    {
        int unitListLength = turnHandler.CountOfList();
        if (currentListIndex <= 0)
            currentListIndex = unitListLength - 1;
        else
            currentListIndex--;
    }

    public void SetIndex(Unit unit)
    {
        int tempIndex = turnHandler.GetIndexOf(unit);
        if (tempIndex < 0) //in case a unit with no movement points is selected and isn't on list
            return;
        currentListIndex = tempIndex;
    }

    public void ToggleEnable(bool v)
    {
        foreach (Button button in buttons)
        {
            button.enabled = v;
        }
    }
}
