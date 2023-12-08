using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIUnitTurnHandler : MonoBehaviour
{
    //[SerializeField]
    //private AllUnitDataSO allUnitDataSO;
    [SerializeField]
    private MapWorld world;

    [SerializeField]
    private CanvasGroup canvasGroup;

    [HideInInspector]
    public UnitTurnListHandler turnHandler;
    [SerializeField]
    public List<Button> buttons;

    [HideInInspector]
    public int currentListIndex;
    //[HideInInspector]
    //public UnityEvent buttonClicked; //only listener in CityBuilderManager to ResetUI


    private void Awake()
    {
        turnHandler = GetComponent<UnitTurnListHandler>();
    }

    private void SelectUnit(Unit unit) => turnHandler.SelectUnit(unit);

    //public void GoToNextUnit() //for when a unit runs out of movement points
    //{
    //    int listCount = turnHandler.CountOfList();

    //    if (listCount == 0) //if the last unit to move finishes
    //    {
    //        ToggleInteractable(false);
    //        return;
    //    }

    //    if (currentListIndex >= listCount) //go to first in list if over list count
    //        currentListIndex = 0;

    //    SelectUnit(turnHandler.GetFromTurnList(currentListIndex));
    //}

    public void NextUnitToMove() //used on right button
    {
        if (world.unitOrders)
            return;

		world.cityBuilderManager.PlaySelectAudio();
		IncreaseIndex();
        SelectUnit(turnHandler.GetFromTurnList(currentListIndex));
        world.cityBuilderManager.ResetCityUI();
        //buttonClicked?.Invoke();
    }

    public void PrevUnitToMove() //used on left button
    {
        if (world.unitOrders)
            return;

        world.cityBuilderManager.PlaySelectAudio();
        DecreaseIndex();
        SelectUnit(turnHandler.GetFromTurnList(currentListIndex));
        world.cityBuilderManager.ResetCityUI();
        //buttonClicked?.Invoke();
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

    public void ToggleInteractable(bool v)
    {
        canvasGroup.interactable = v;
    }

    public void ToggleEnable(bool v)
    {
        foreach (Button button in buttons)
        {
            button.enabled = v;
        }
    }
}
