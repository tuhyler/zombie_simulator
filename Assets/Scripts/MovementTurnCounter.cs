using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MovementTurnCounter : MonoBehaviour
{
    //[SerializeField]
    //private SpriteRenderer turnCountHolder;

    [SerializeField]
    private TMP_Text turnCountText;

    private void Awake()
    {
        //turnCountHolder.GetComponent<SpriteRenderer>().enabled = false;
        //turnCountText.GetComponent<TMP_Text>().enabled = false;
    }

    public void SetTurnCount(int turnCount)
    {
        //turnCountHolder.enabled = true;
        //turnCountText.enabled = true;
        turnCountText.text = turnCount.ToString();
    }

    public void HideTurnCount()
    {
        //turnCountHolder.enabled = false;
        turnCountText.enabled = false;
    }

    public void DestroyTurnCount()
    {
        Destroy(gameObject);
    }
}
