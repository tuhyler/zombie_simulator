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

    public List<Unit> unitTurnList = new(); //public just for testing

    private void Awake()
    {
        unitTurnList = new List<Unit>();
    }

    public void StartTurn() //currently not having a unit selected to start turn
    {
        if (CountOfList() > 0)
        {
            SelectUnit(GetFromTurnList(0)); //always goes to the first in the list, should I change this?
            uiUnitTurnHandler.ToggleInteractable(true);
        }
        else
        {
            uiUnitTurnHandler.ToggleInteractable(false);
        }
    }

    public int GetIndexOf(Unit unit)
    {
        return unitTurnList.IndexOf(unit);
    }

    public void SelectUnit(Unit unit)
    {
        unitMovement.PrepareMovement(unit);
        buildingManager.HandleUnitSelection(unit); //If worker, shows UI
    }

    public void AddToTurnList(Unit unit)
    {
        if (!unitTurnList.Contains(unit))
            unitTurnList.Add(unit);
    }

    public Unit GetFromTurnList(int index)
    {
        //Debug.Log("index is " + index);
        return unitTurnList[index];
    }

    public void RemoveUnitFromTurnList(Unit unit)
    {
        unitTurnList.Remove(unit);
        if (unitTurnList.Count == 0)
        {
            uiUnitTurnHandler.ToggleInteractable(false);
            unitTurnList = new();
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
