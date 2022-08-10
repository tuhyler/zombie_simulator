using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SystemTurnTaker : MonoBehaviour
{
    public void WaitTurn() //for the enemies
    {
        foreach (ITurnDependent item in GetComponents<ITurnDependent>())
        {
            item.WaitTurn();
        }
    }
}
