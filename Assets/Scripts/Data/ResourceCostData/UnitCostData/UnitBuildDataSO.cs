using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Unit Build Data", menuName = "EconomyData/UnitBuildData")]
public class UnitBuildDataSO : ScriptableObject
{
    //public GameObject prefab;
    //public GameObject secondaryPrefab;
    public int showOrder;
    public string prefabLoc;
    public string secondaryPrefabLoc;
    public string nationalityAdjective;
    public string unitDisplayName;
    public int unitLevel;
    public string unitNameAndLevel;
    public bool empireUnit;
    public bool inMilitary;
    public Sprite image;
    public string imageName;
    public Sprite mapIcon;
    public List<ResourceValue> unitCost; //cost to build
    public List<ResourceValue> cycleCost; //cost per city cycle
    public List<ResourceValue> battleCost; //cost per battle
    public int laborCost;
    public UnitType unitType;
    public string unitDescription;
    public bool availableInitially = false;
    public int health = 10;
    public float regenerationRate = 2;
    public int baseAttackStrength = 10;
    public float baseAttackSpeed = 0.5f;
    public float movementSpeed = 1f;
    public int trainTime = 0;
    public int cargoCapacity;
    public Vector2Int goldDropRange = Vector2Int.zero;
    public TransportationType transportationType = TransportationType.Land;
    public bool characterUnit;
    public bool npc;
    public SingleBuildType singleBuildType;
    public Era unitEra;
    public Region unitRegion = Region.None;
}

public enum UnitType
{
    Worker,
    Infantry,
    Ranged,
    Cavalry,
    Seige,
    Trader,
    BoatTrader,
    Transport,
    TradeRepresentative,
    Laborer
}

public enum TransportationType
{
    Land,
    Sea,
    Air
}
