using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TurnBasedManager : MonoBehaviour
{
    [SerializeField]
    private UnitTurnListHandler turnListHandler;

    public UnityEvent BlockPlayerInput, UnblockPlayerInput;

    public void NextTurn()
    {
        Debug.Log("Waiting...");
        BlockPlayerInput?.Invoke();
        SystemsTurn();
        PlayerTurn();
        PlayerSystemsTurn();
    }


    private void SystemsTurn()
    {
        foreach (SystemTurnTaker turnTaker in FindObjectsOfType<SystemTurnTaker>()) //FindObjectsOfType is slow, fine for turn based though
        {
            turnTaker.WaitTurn();
        }
    }

    private void PlayerTurn()
    {
        //foreach (TerrainData td in FindObjectsOfType<TerrainData>())
        //{
        //    td.DisableHighlight();
        //}

        foreach (UnitTurnTaker turnTaker in FindObjectsOfType<UnitTurnTaker>())
        {
            turnTaker.WaitTurn();
            //Debug.Log($"Object {turnTaker.name} is waiting");
        }
    }

    private void PlayerSystemsTurn()
    {
        foreach (PlayerSystemTurnTaker turnTaker in FindObjectsOfType<PlayerSystemTurnTaker>())
        {
            turnTaker.WaitTurn();
            Debug.Log($"Object {turnTaker.name} is waiting");
        }

        //turnListHandler.StartTurn(); //to start turn on unit, need to make sure continued movement orders are finished first though
        Debug.Log("New turn ready!");
        UnblockPlayerInput?.Invoke();
    }
}



public interface ITurnDependent
{
    void WaitTurn();
}
