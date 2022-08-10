using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitTurnTaker : MonoBehaviour
{
    public void WaitTurn() //for player units
    {
        foreach (ITurnDependent item in GetComponents<ITurnDependent>())
        {
            item.WaitTurn();
        }
    }
}
