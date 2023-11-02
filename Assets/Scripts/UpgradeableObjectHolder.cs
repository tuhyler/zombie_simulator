using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UpgradeableObjectHolder : MonoBehaviour
{
    public static UpgradeableObjectHolder Instance { get; private set; }

    public List<ImprovementDataSO> allBuildingsAndImprovements = new(); //not static so as to populate lists in inspector
    public List<UnitBuildDataSO> allUnits = new(); //not static so as to populate lists in inspector
    public List<TerrainDataSO> allTerrain = new();

    public Dictionary<string, ImprovementDataSO> improvementDict = new();
    public Dictionary<string, UnitBuildDataSO> unitDict = new();
    public Dictionary<string, TerrainDataSO> terrainDict = new();

    private void Awake()
    {
        Instance = this;

        //putting order to set up upgrading costs in MapWorld
        allBuildingsAndImprovements = allBuildingsAndImprovements.OrderBy(x => x.improvementLevel).ToList();
        allBuildingsAndImprovements = allBuildingsAndImprovements.OrderBy(x => x.improvementName).ToList();

        foreach (ImprovementDataSO improvement in allBuildingsAndImprovements)
            improvementDict[improvement.improvementNameAndLevel] = improvement;

        foreach (UnitBuildDataSO unit in allUnits)
            unitDict[unit.unitNameAndLevel] = unit;

        foreach (TerrainDataSO terrain in allTerrain)
            terrainDict[terrain.name] = terrain;
    }
}
