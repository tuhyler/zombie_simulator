using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Units/AllUnitData")]
public class AllUnitDataSO : ScriptableObject //have this class for saving it
{
    private Dictionary<Unit, List<TerrainData>> previousMovementOrdersDict = new();

    public void SetMovementOrders(Unit unit, List<TerrainData> previousMovementOrders)
    {
        previousMovementOrdersDict[unit] = previousMovementOrders;
    }

    public List<TerrainData> GetMovementOrders(Unit unit)
    {
        return previousMovementOrdersDict[unit];
    }

    public void RemoveUnitFromDicts(Unit unit)
    {
        previousMovementOrdersDict.Remove(unit);
    }

    public int MovementOrdersLengthRemaining(Unit unit)
    {
        return previousMovementOrdersDict[unit].Count;
    }

    //public int CountOfDict() //for checking how many in the dict
    //{
    //    foreach (var thing in previousMovementOrdersDict.Keys)
    //        Debug.Log("In dict" + thing);
    //    return previousMovementOrdersDict.Count;
    //}
}
