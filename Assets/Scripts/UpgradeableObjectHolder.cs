using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UpgradeableObjectHolder : MonoBehaviour
{
    public static UpgradeableObjectHolder Instance { get; private set; }

	//not static so as to populate lists in inspector
	public List<ImprovementDataSO> allBuildingsAndImprovements = new(); 
    public List<UnitBuildDataSO> allUnits = new(); 
    public List<UnitBuildDataSO> allEnemyLeaders = new();
    public List<UnitBuildDataSO> allEnemyUnits = new();
    public List<TerrainDataSO> allTerrain = new();
    public List<GameObject> allTradeCenters = new();
    public List<WonderDataSO> allWonders = new();
    public List<UtilityCostSO> allUtilities = new();

    public Dictionary<string, ImprovementDataSO> improvementDict = new();
    public Dictionary<string, UnitBuildDataSO> unitDict = new();
    public Dictionary<string, UnitBuildDataSO> enemyLeaderDict = new();
    public Dictionary<string, UnitBuildDataSO> enemyUnitDict = new();
    public Dictionary<string, TerrainDataSO> terrainDict = new();
    public Dictionary<string, GameObject> tradeCenterDict = new();
    public Dictionary<string, WonderDataSO> wonderDict = new();
    public Dictionary<UtilityType, Dictionary<int, UtilityCostSO>> utilityDict = new();
    public Dictionary<string, string> conversationTaskDict = new();

    private void Awake()
    {
        Instance = this;

        //putting order to set up upgrading costs in MapWorld
        allBuildingsAndImprovements = allBuildingsAndImprovements.OrderBy(x => x.improvementLevel).ToList();
        allBuildingsAndImprovements = allBuildingsAndImprovements.OrderBy(x => x.improvementName).ToList();

        foreach (ImprovementDataSO improvement in allBuildingsAndImprovements)
            improvementDict[improvement.improvementNameAndLevel] = improvement;

        foreach (UnitBuildDataSO unit in allUnits)
        {
            string nameAndLevel = unit.unitNameAndLevel;
            if (unit.unitDisplayName == "Azai")
                nameAndLevel = "Azai-" + unit.unitLevel;
            unitDict[nameAndLevel] = unit;
        }

        foreach (UnitBuildDataSO unit in allEnemyLeaders)
            enemyLeaderDict[unit.unitNameAndLevel] = unit;

        foreach (UnitBuildDataSO unit in allEnemyUnits)
            enemyUnitDict[unit.unitNameAndLevel] = unit;

        foreach (TerrainDataSO terrain in allTerrain)
            terrainDict[terrain.name] = terrain;

   //     foreach (GameObject center in allTradeCenters)
   //     {
   //         string name = center.GetComponent<TradeCenter>().tradeCenterName;
   //         if (name == "")
   //             Debug.LogError("Trade Center must be given name");

			//tradeCenterDict[name] = center;
   //     }

        foreach (WonderDataSO wonder in allWonders)
            wonderDict[wonder.wonderName] = wonder;

        foreach (UtilityCostSO utility in allUtilities)
        {
            if (!utilityDict.ContainsKey(utility.utilityType))
                utilityDict[utility.utilityType] = new(); 

            utilityDict[utility.utilityType][utility.utilityLevel] = utility;
        }

        //converation task creation
        conversationTaskDict["Tutorial"] = "Finish tutorial by completing the following: " +
							"\n\n - Make camp \n\n - Build a hut \n\n - Build research \n\n - Build a farm";

    }
}
