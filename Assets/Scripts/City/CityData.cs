using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CityData 
{
    public string name;
	public Vector3Int location;
	public bool reachedWaterLimit, autoAssignLabor, hasWater, hasFreshWater, hasRocksFlat, hasRocksHill, hasTrees, hasFood, hasWool, hasSilk, hasClay,
		/*fullInventory, */isNamed, attacked, growing;
	public int waterMaxPop, currentPop, unusedLabor, usedLabor, countDownTimer, warehouseStorageLevel, lostPop;
	public List<ResourceType> resourcePriorities;
	public Dictionary<ResourceType, int> resourceGridDict;
	public List<SingleBuildType> singleBuildList;
	public int[] lastFiveCoin = new int[5];
	//public List<int> tradersHere = new();

	//resource manager data
	//public Dictionary<ResourceType, int> resourceDict;
	//public Dictionary<ResourceType, int> resourcePriceDict;
	//public List<ResourceType> resourceSellList;
	//public Dictionary<ResourceType, int> resourceMinHoldDict;
	//public Dictionary<ResourceType, int> resourceSellHistoryDict;
	public Dictionary<ResourceType, (int, int, int, int, int, bool)> allResourceInfoDict = new(); //putting it all into one to reduce storage size
	public bool pauseGrowth, growthDeclineDanger;
	public int starvationCount, noHousingCount, noWaterCount, cycleCount;

	//queueing
	public List<QueueItem> queueItemList;
	public Dictionary<ResourceType, int> queuedResourcesToCheck;

	//city building data
	public List<CityImprovementData> cityBuildings = new();

	//army data
	public Vector3Int armyForward, armyAttackZone, enemyTarget;
	public int cyclesGone;
	public List<Vector3Int> armyPathToTarget, armyPathTraveled, armyAttackingSpots, armyMovementRange, armyCavalryRange;
	public bool isEmpty = true, isFull, isTraining, isTransferring, isRepositioning, traveling, inBattle, returning, atHome, enemyReady, issueRefund = true;

	//waiting lists
	public List<Vector3Int> /*waitingforResourceProducerList = new(), *//*waitingForProducerStorageList = new(), waitingToUnloadProducerList = new(),*/ waitingToUnloadResearchList = new();
	public List<(Vector3Int, int)> goldWaitList = new(), unloadWaitList = new();
	public Dictionary<ResourceType, List<(Vector3Int, int)>> resourceWaitDict = new();
	public Dictionary<ResourceType, List<Vector3Int>> resourceMaxWaitDict = new();
	//public List<ResourceType> resourcesNeededForProduction;
	public List<int> /*waitingForTraderList = new(), */waitList = new(), seaWaitList = new(), airWaitList = new();
	//public List<ResourceType> resourcesNeededForRoute;
}
