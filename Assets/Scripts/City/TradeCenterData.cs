using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TradeCenterData
{
    public Vector3Int mainLoc, harborLoc;
    public Quaternion rotation;
    public GameObject prefab;
    public string name;
    public int cityPop;
    public bool isDiscovered;
}
