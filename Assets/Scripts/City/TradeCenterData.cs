using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TradeCenterData
{
    public string name;
    public Vector3Int mainLoc;
    public Dictionary<SingleBuildType, Vector3Int> singleBuildDict = new();
    public Quaternion rotation;
    public int cityPop;
    public bool isDiscovered;
    public List<int> waitList = new(), seaWaitList = new(), airWaitList = new();
}
