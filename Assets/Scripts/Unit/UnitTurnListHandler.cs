using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitTurnListHandler : MonoBehaviour
{

    [SerializeField]
    private UIUnitTurnHandler uiUnitTurnHandler;

    [SerializeField]
    private UnitMovement unitMovement;

    [SerializeField]
    private WorkerTaskManager buildingManager;

    private List<Unit> unitTurnList = new(); 


    //public void ListCountCheck()
    //{
    //    if (CountOfList() > 0)
    //    {
    //        uiUnitTurnHandler.ToggleInteractable(true);
    //    }
    //    else
    //    {
    //        uiUnitTurnHandler.ToggleInteractable(false);
    //    }
    //}

    public int GetIndexOf(Unit unit)
    {
        return unitTurnList.IndexOf(unit);
    }

    public void SelectUnit(Unit unit)
    {
        unitMovement.PrepareMovement(unit);
    }

    public void AddToTurnList(Unit unit)
    {
        if (!unitTurnList.Contains(unit))
            unitTurnList.Add(unit);

        uiUnitTurnHandler.ToggleInteractable(true);
    }

    public Unit GetFromTurnList(int index)
    {
        return unitTurnList[index];
    }

    public void RemoveUnitFromTurnList(Unit unit)
    {
        unitTurnList.Remove(unit);
        if (unitTurnList.Count == 0)
        {
            uiUnitTurnHandler.ToggleInteractable(false);
            //unitTurnList = new();
        }
    }

    public int CountOfList()
    {
        return unitTurnList.Count;
    }

    //public void WaitTurn()
    //{
    //    if (CountOfList() > 0)
    //        uiUnitTurnHandler.ToggleInteractable(true);
    //}
}
