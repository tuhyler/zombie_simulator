using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Unit Build Data", menuName = "EconomyData/UnitBuildData")]
public class UnitBuildDataSO : ScriptableObject
{
    public GameObject prefab;
    public GameObject secondaryPrefab;
    public string unitName;
    public string unitDisplayName;
    public int unitLevel;
    public string unitNameAndLevel;
    public Sprite image;
    public Sprite mapIcon;
    public List<ResourceValue> unitCost; //cost to build
    public List<ResourceValue> cycleCost; //cost per city cycle
    public List<ResourceValue> battleCost; //cost per battle
    public int laborCost;
    public UnitType unitType;
    public string unitDescription;
    public bool availableInitially = false;
    [HideInInspector]
    public bool locked = true;
    public int health = 10;
    public int baseAttackStrength = 10;
    public float baseAttackSpeed = 0.5f;
    public float movementSpeed = 1f;
    public int trainTime = 0;
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
