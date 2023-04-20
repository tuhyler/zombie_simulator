using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Unit Build Data", menuName = "EconomyData/UnitBuildData")]
public class UnitBuildDataSO : ScriptableObject
{
    public GameObject prefab;
    public string unitName;
    public int unitLevel;
    public Sprite image;
    public List<ResourceValue> unitCost;
    public List<ResourceValue> consumedResources;
    public UnitType unitType;
    public string unitDescription;
    public bool availableInitially = false;
    [HideInInspector]
    public bool locked = true;
    public int movementPoints = 10; //remove
    public int health = 10;
    public int attackStrength = 10;
    public float movementSpeed = 1f;
    public int cargoCapacity;
    public TransportationType transportationType = TransportationType.Land;
}

public enum UnitType
{
    Worker,
    Infantry,
    Ranged,
    Cavalry,
    Seige,
    Trader,
    BoatTrader
}

public enum TransportationType
{
    Land,
    Sea,
}
