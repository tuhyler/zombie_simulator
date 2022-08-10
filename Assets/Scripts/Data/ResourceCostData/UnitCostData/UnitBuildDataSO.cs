using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Unit Build Data", menuName = "EconomyData/UnitBuildData")]
public class UnitBuildDataSO : ScriptableObject
{
    public GameObject prefab;
    public string unitName;
    public Sprite image;
    public List<ResourceValue> unitCost;
    public UnitType unitType;
}

public enum UnitType
{
    Worker,
    Infantry,
    Ranged,
    Cavalry,
    Seige,
    Trader
}
