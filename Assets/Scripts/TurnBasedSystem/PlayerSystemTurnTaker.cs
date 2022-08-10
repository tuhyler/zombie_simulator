using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSystemTurnTaker : MonoBehaviour
{
    public void WaitTurn() //for player system, after units have done their waitturn
    {
        foreach (ITurnDependent item in GetComponents<ITurnDependent>())
        {
            item.WaitTurn();
        }
    }
}
