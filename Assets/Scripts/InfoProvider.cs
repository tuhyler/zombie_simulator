using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoProvider : MonoBehaviour
{
    //[SerializeField]
    //private SpriteRenderer spriteRenderer;
    //public Sprite Image => spriteRenderer.sprite; //lambda "return spriteRenderer.sprite"

    private string objectName;
    public string NameToDisplay => objectName;

    //private int currentMovementPoints;
    //public int CurrentMovementPoints => currentMovementPoints;

    //private int regMovementPoints;
    //public int RegMovementPoints => regMovementPoints;

    private Unit selectedUnit;

    private void Start()
    {
        GetBaseInfo();
    }

    private void GetBaseInfo()
    {
        selectedUnit = gameObject.GetComponent<Unit>();
        objectName = gameObject.name;
        //regMovementPoints = selectedUnit.GetUnitData().movementPoints;
    }
}
