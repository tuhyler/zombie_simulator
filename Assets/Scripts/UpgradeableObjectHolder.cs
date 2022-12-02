using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeableObjectHolder : MonoBehaviour
{
    public static UpgradeableObjectHolder Instance { get; private set; }

    public List<ImprovementDataSO> allBuildingsAndImprovements = new(); //not static so as to populate lists in inspector
    public List<UnitBuildDataSO> allUnits = new(); //not static so as to populate lists in inspector

    private void Awake()
    {
        Instance = this;
    }
}
