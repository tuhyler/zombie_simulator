using System.Collections.Generic;
using UnityEngine;

public class UnitTurnListHandler : MonoBehaviour
{
    [SerializeField]
    private UnitMovement unitMovement;

    [SerializeField]
    private WorkerTaskManager buildingManager;

    private List<Unit> unitTurnList = new(); 


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
    }

    public Unit GetFromTurnList(int index)
    {
        return unitTurnList[index];
    }

    public void RemoveUnitFromTurnList(Unit unit)
    {
        unitTurnList.Remove(unit);
    }

    public int CountOfList()
    {
        return unitTurnList.Count;
    }
}
