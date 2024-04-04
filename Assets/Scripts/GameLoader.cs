using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.Playables;

public class GameLoader : MonoBehaviour
{
    public static GameLoader Instance;

	public MapWorld world;

	public TerrainGenerator terrainGenerator;

	[HideInInspector]
	public GamePersist gamePersist = new();
	[HideInInspector]
	public GameData gameData = new();
	[HideInInspector]
	public bool isLoading, isDone;
	[HideInInspector]
	public List<City> attackingEnemyCitiesList = new();
	[HideInInspector]
	public List<Military> attackingUnitList = new();
	public Dictionary<string, List<Vector3Int>> enemyLeaderDict = new();
	public Dictionary<string, Trader> ambushedTraders = new();
	public Dictionary<TradeCenter, (List<int>, List<int>)> centerWaitingDict = new();
	public Dictionary<Wonder, (List<int>, List<int>)> wonderWaitingDict = new();
	public Dictionary<Unit, List<Vector3Int>> unitMoveOrders = new();
	public Dictionary<City, (List<Vector3Int>, List<Vector3Int>, List<Vector3Int>, List<int>, List<int>, List<int>, List<int>)> cityWaitingDict = new();
	public Dictionary<CityImprovement, string> improvementUnitUpgradeDict = new();
	[HideInInspector]
	public List<GameObject> textList = new();
	[HideInInspector]
	public MilitaryLeader duelingLeader;

	private void Awake()
	{
		Instance = this;
	}

	public void NewGame(string starting, string landType, string resource, /*string mountains,*/string enemy, string mapSize, bool tutorial)
	{
		GameManager.Instance.ResetProgress();
		world.ClearMap();

		//if (starting == "NorthToggle")
		//{
		//	terrainGenerator.startingRegion = Region.North;
		//	terrainGenerator.desertPerc = 5;
		//	terrainGenerator.forestAndJunglePerc = 95;

		//	if (mapSize == "SmallToggle")
		//	{
		//		terrainGenerator.width = 20;
		//		terrainGenerator.height = 20;
		//	}
		//	else if (mapSize == "MediumToggle")
		//	{
		//		terrainGenerator.width = 25;
		//		terrainGenerator.height = 25;
		//	}
		//	else if (mapSize == "LargeToggle")
		//	{
		//		terrainGenerator.width = 30;
		//		terrainGenerator.height = 30;
		//	}

		//	terrainGenerator.equatorDist = 2;
		//	terrainGenerator.equatorPos = 6; //bottom of map
		//}
		//else if (starting == "SouthToggle")
		//{
		//	terrainGenerator.startingRegion = Region.South;
		//	terrainGenerator.desertPerc = 90;
		//	terrainGenerator.forestAndJunglePerc = 10;

		//	if (mapSize == "SmallToggle")
		//	{
		//		terrainGenerator.width = 20;
		//		terrainGenerator.height = 20;
		//	}
		//	else if (mapSize == "MediumToggle")
		//	{
		//		terrainGenerator.width = 25;
		//		terrainGenerator.height = 25;
		//	}
		//	else if (mapSize == "LargeToggle")
		//	{
		//		terrainGenerator.width = 30;
		//		terrainGenerator.height = 30;
		//	}

		//	terrainGenerator.equatorDist = 5;
		//	terrainGenerator.equatorPos = terrainGenerator.height * 3 * 3 / 4;
		//}
		//else if (starting == "EastToggle")
		//{
		//	terrainGenerator.startingRegion = Region.East;
		//	terrainGenerator.desertPerc = 20;
		//	terrainGenerator.forestAndJunglePerc = 80;

		//	if (mapSize == "SmallToggle")
		//	{
		//		terrainGenerator.width = 20;
		//		terrainGenerator.height = 20;
		//	}
		//	else if (mapSize == "MediumToggle")
		//	{
		//		terrainGenerator.width = 25;
		//		terrainGenerator.height = 25;
		//	}
		//	else if (mapSize == "LargeToggle")
		//	{
		//		terrainGenerator.width = 30;
		//		terrainGenerator.height = 30;
		//	}

		//	terrainGenerator.equatorDist = 4;
		//	terrainGenerator.equatorPos = terrainGenerator.height * 3 * 2 / 5;
		//}
		//else if (starting == "WestToggle")
		//{
		//	terrainGenerator.startingRegion = Region.West;
		//	terrainGenerator.desertPerc = 40;
		//	terrainGenerator.forestAndJunglePerc = 60;

		//	if (mapSize == "SmallToggle")
		//	{
		//		terrainGenerator.width = 20;
		//		terrainGenerator.height = 20;
		//	}
		//	else if (mapSize == "MediumToggle")
		//	{
		//		terrainGenerator.width = 25;
		//		terrainGenerator.height = 25;
		//	}
		//	else if (mapSize == "LargeToggle")
		//	{
		//		terrainGenerator.width = 30;
		//		terrainGenerator.height = 30;
		//	}

		//	terrainGenerator.equatorDist = 7;
		//	terrainGenerator.equatorPos = terrainGenerator.height * 3 * 2 / 5;
		//}

		//if (landType == "IslandsToggle")
		//{
		//	terrainGenerator.iterations = 2;
		//	terrainGenerator.landMassLimit = 20;
		//	terrainGenerator.totalLandLimit = terrainGenerator.width * terrainGenerator.height / 4 + (terrainGenerator.width * terrainGenerator.height / 8);
		//	terrainGenerator.totalLandLimit = 0;
		//	terrainGenerator.continentsFlag = 0;
		//}
		//else if (landType == "ContinentsToggle")
		//{
		//	terrainGenerator.iterations = 15;
		//	terrainGenerator.landMassLimit = 2;
		//	terrainGenerator.totalLandLimit = terrainGenerator.width * terrainGenerator.height / 2;
		//	terrainGenerator.continentsFlag = 1;
		//}

		//if (resource == "ScarceToggle")
		//{
		//	terrainGenerator.resourceFrequency = 5;
		//	terrainGenerator.resourceFlag = 0;
		//}
		//else if (resource == "PlentyToggle")
		//{
		//	terrainGenerator.resourceFrequency = 4;
		//	terrainGenerator.resourceFlag = 1;
		//}
		//else if (resource == "AbundantToggle")
		//{
		//	terrainGenerator.resourceFrequency = 3;
		//	terrainGenerator.resourceFlag = 2;
		//}

		//if (mountains == "FlatToggle")
		//{
		//	terrainGenerator.mountainPerc = 20;
		//	terrainGenerator.mountainousPerc = 40;
		//}
		//else if (mountains == "RegularToggle")
		//{
		//	terrainGenerator.mountainPerc = 33;
		//	terrainGenerator.mountainousPerc = 70;
		//}
		//else if (mountains == "MountainousToggle")
		//{
		//	terrainGenerator.mountainPerc = 50;
		//	terrainGenerator.mountainousPerc = 70;
		//}

		//if (enemy == "WeakToggle")
		//	terrainGenerator.enemyCountDifficulty = 1;
		//else if (enemy == "ModerateToggle")
		//	terrainGenerator.enemyCountDifficulty = 2;
		//else if (enemy == "StrongToggle")
		//	terrainGenerator.enemyCountDifficulty = 3;

		terrainGenerator.SetYCoord(0);
		terrainGenerator.RunProceduralGeneration(true);
		terrainGenerator.SetMainPlayerLoc();
		world.NewGamePrep(true, terrainGenerator.terrainDict, terrainGenerator.enemyEmpires, terrainGenerator.enemyRoadLocs, tutorial);
		terrainGenerator.Clear();
		StartCoroutine(WaitASec());
	}

	public void SaveGame(string saveNameRaw, float playTime, string version, string screenshot)
	{
		//string saveName = "game_data";
		gameData.saveDate = System.DateTime.Now.ToString();
		string saveName = "/" + saveNameRaw + ".save";
		gameData.saveName = saveName;
		gameData.savePlayTime += playTime;
		gameData.saveVersion = version;
		gameData.saveScreenshot = screenshot;
		gameData.currentEra = world.currentEra;
		gameData.startingRegion = world.startingRegion;
		gameData.currentWorkedTileDict = new(world.currentWorkedTileDict);
		gameData.cityWorkedTileDict = new(world.cityWorkedTileDict);
		gameData.cityImprovementQueueList = new(world.cityImprovementQueueList);
		gameData.unclaimedSingleBuildList = new(world.unclaimedSingleBuildList);
		gameData.camPosition = world.cameraController.transform.position;
		gameData.camRotation = world.cameraController.transform.rotation;
		gameData.timeODay = world.dayNightCycle.timeODay;
		gameData.camLimits.Clear();
		gameData.camLimits.Add(world.cameraController.xMin);
		gameData.camLimits.Add(world.cameraController.xMax);
		gameData.camLimits.Add(world.cameraController.zMin);
		gameData.camLimits.Add(world.cameraController.zMax);
		gameData.tutorialStep = world.tutorialStep;
		gameData.gameStep = world.gameStep;
		gameData.goldAmount = world.worldResourceManager.GetWorldGoldLevel();
		gameData.scottFollow = world.scottFollow;
		gameData.azaiFollow = world.azaiFollow;
		gameData.startingLoc = world.startingLoc;
		gameData.tutorialGoing = world.tutorialGoing;

		gameData.attackLocs.Clear();
		gameData.attackLocs = new(world.uiAttackWarning.attackLocs);

		gameData.ambushes = world.ambushes;
		gameData.cityCount = world.cityCount;
		gameData.infantryCount = world.infantryCount;
		gameData.rangedCount = world.rangedCount;
		gameData.cavalryCount = world.cavalryCount;
		gameData.traderCount = world.traderCount;
		gameData.boatTraderCount = world.boatTraderCount;
		gameData.laborerCount = world.laborerCount;
		gameData.food = world.food;
		gameData.lumber = world.lumber;
		gameData.popGrowth = world.popGrowth;
		gameData.popLost = world.popLost;
		gameData.newUnitsAndImprovements = new(world.newUnitsAndImprovements);
		gameData.currentResearch = world.researchTree.SaveResearch();

		gameData.researchWaitList.Clear();
		for (int i = 0; i < world.researchWaitList.Count; i++)
			gameData.researchWaitList.Add(world.researchWaitList[i].producerLoc);

		gameData.goldCityWaitList.Clear();
		for (int i = 0; i < world.goldCityWaitList.Count; i++)
			gameData.goldCityWaitList.Add(world.goldCityWaitList[i].cityLoc);

		gameData.goldCityRouteWaitList.Clear();
		for (int i = 0; i < world.goldCityRouteWaitList.Count; i++)
			gameData.goldCityRouteWaitList.Add(world.goldCityRouteWaitList[i].cityLoc);

		gameData.goldWonderWaitList.Clear();
		for (int i = 0; i < world.goldWonderWaitList.Count; i++)
			gameData.goldWonderWaitList.Add(world.goldWonderWaitList[i].unloadLoc);

		gameData.goldTradeCenterWaitList.Clear();
		for (int i = 0; i < world.goldTradeCenterWaitList.Count; i++)
			gameData.goldTradeCenterWaitList.Add(world.goldTradeCenterWaitList[i].mainLoc);

		gameData.allTCRepData.Clear();
		foreach (string name in world.allTCReps.Keys)
			gameData.allTCRepData[name] = world.allTCReps[name].SaveTradeRepData();

		gameData.allEnemyLeaderData.Clear();
		for (int i = 0; i < world.allEnemyLeaders.Count; i++)
			gameData.allEnemyLeaderData.Add(world.allEnemyLeaders[i].SaveMilitaryUnitData());

		List<Vector3Int> enemyCampLocs = new List<Vector3Int>(gameData.attackedEnemyBases.Keys);
		for (int i = 0; i < enemyCampLocs.Count; i++)
		{
			gameData.attackedEnemyBases[enemyCampLocs[i]] = world.GetEnemyCamp(enemyCampLocs[i]).SendCampUnitData();
		}

		List<Vector3Int> movingEnemyCampLocs = new List<Vector3Int>(gameData.movingEnemyBases.Keys);
		for (int i = 0; i < movingEnemyCampLocs.Count; i++)
		{
			gameData.movingEnemyBases[movingEnemyCampLocs[i]] = world.GetEnemyCamp(movingEnemyCampLocs[i]).SendMovingCampUnitData();
		}

		gameData.ambushLocs.Clear();
		List<Vector3Int> ambushLocs = new List<Vector3Int>(world.enemyAmbushDict.Keys);
		for (int i = 0; i < ambushLocs.Count; i++)
		{
			gameData.ambushLocs[ambushLocs[i]] = world.enemyAmbushDict[ambushLocs[i]].GetAmbushData();
		}

		//trade centers (waiting lists)
		foreach (TradeCenter center in world.tradeCenterDict.Values)
		{
			gameData.allTradeCenters[center.mainLoc].waitList = center.SaveWaitListData(false);
			gameData.allTradeCenters[center.mainLoc].seaWaitList = center.SaveWaitListData(true);
		}

		//wonders
		gameData.allWonders.Clear();
		for (int i = 0; i < world.allWonders.Count; i++)
		{
			gameData.allWonders.Add(world.allWonders[i].SaveData());
		}

		//cities
		gameData.allCities.Clear();
		gameData.militaryUnits.Clear();
		gameData.allArmies.Clear();
		foreach (City city in world.cityDict.Values)
		{
			gameData.allCities.Add(city.SaveCityData());
		}

		//city improvements
		gameData.allCityImprovements.Clear();
		foreach (Vector3Int tile in world.cityImprovementDict.Keys)
		{
			if (world.GetTerrainDataAt(tile).enemyZone)
				continue;
			
			gameData.allCityImprovements.Add(world.GetCityDevelopment(tile).SaveData());
		}

		//roads
		gameData.allRoads.Clear();
		foreach (Vector3Int loc in world.roadTileDict.Keys)
		{
			Road road = world.roadTileDict[loc][0];

			if (road == null)
				road = world.roadTileDict[loc][1];

			gameData.allRoads.Add(road.SaveData(loc));
		}
		
		//main characters
		gameData.playerUnit = world.mainPlayer.SaveWorkerData();
		gameData.scott = world.scott.SaveWorkerData();
		gameData.azai = world.azai.SaveMilitaryUnitData();

		//traders
		gameData.allTraders.Clear();
		for (int i = 0; i < world.traderList.Count; i++)
			gameData.allTraders.Add(world.traderList[i].SaveTraderData());

		//laborers
		gameData.allLaborers.Clear();
		for (int i = 0; i < world.laborerList.Count; i++)
			gameData.allLaborers.Add(world.laborerList[i].SaveLaborerData());

		//transports
		gameData.allTransports.Clear();
		for (int i = 0; i < world.transportList.Count; i++)
			gameData.allTransports.Add(world.transportList[i].SaveTransportData());

		Vector3 middle = new Vector3(Screen.width / 2, Screen.height / 2, 0);
		if (gamePersist.SaveData(saveName, gameData, false))
		{
			UIInfoPopUpHandler.WarningMessage().Create(middle, "Game Saved!");
		}
		else
		{
			UIInfoPopUpHandler.WarningMessage().Create(middle, "Failed to save...");
		}

		Resources.UnloadUnusedAssets();
		world.uiMainMenu.uiSaveGame.ToggleVisibility(false);
		world.uiMainMenu.uiSaveGame.currentSaves.Add(saveNameRaw);
		world.uiMainMenu.ToggleVisibility(false);
	}

	public void LoadDataGame(string loadName)
	{
		world.uiMainMenu.ToggleVisibility(false);
		GameManager.Instance.BackToMainMenu(true, loadName);
	}


	public void LoadData(string saveName)
	{
		GameManager.Instance.ResetProgress();
		isLoading = true;
		duelingLeader = null;
		//Time.timeScale = 0f;
		//AudioListener.pause = true;

		world.ClearMap();
		
		gameData = gamePersist.LoadData(saveName, false);

		world.currentEra = gameData.currentEra;
		world.startingRegion = gameData.startingRegion;
		world.tutorial = gameData.tutorial; 
		world.tutorialGoing = gameData.tutorialGoing;
		world.GenerateMap(gameData.allTerrain);
		world.resourceDiscoveredList = new(gameData.resourceDiscoveredList);
		world.LoadDiscoveredResources();

		//updating progress
		GameManager.Instance.UpdateProgress(20);
		List<Vector3Int> roadList = new();
		for (int i = 0; i < gameData.allRoads.Count; i++)
			roadList.Add(gameData.allRoads[i].position);

		world.GenerateTradeCenters(gameData.allTradeCenters);
		gameData.allTCRepData.Clear();
		for (int i = 0; i < gameData.allEnemyLeaderData.Count; i++)
		{
			EnemyEmpire empire = world.GenerateEnemyLeaders(gameData.allEnemyLeaderData[i]);
			
			for (int j = 0; j < empire.empireCities.Count; j++)
				world.GenerateEnemyCities(gameData.enemyCities[empire.empireCities[j]], empire, roadList);
		}
		gameData.allEnemyLeaderData.Clear();

		world.MakeEnemyCamps(gameData.enemyCampLocs, gameData.discoveredEnemyCampLocs);
		foreach (Vector3Int tile in gameData.enemyCities.Keys)
			world.LoadEnemyBorders(tile, world.GetEnemyCity(tile).empire.enemyLeader.buildDataSO.borderColor);
		foreach (Vector3Int tile in gameData.enemyCampLocs.Keys)
			world.LoadEnemyBorders(tile, new Color(1, 0, 0, 0.68f));

		//updating progress
		GameManager.Instance.UpdateProgress(15);

		world.newUnitsAndImprovements = new(gameData.newUnitsAndImprovements);
		gameData.newUnitsAndImprovements.Clear();
		world.researchTree.LoadCompletedResearch(gameData.completedResearch);
		world.tutorialStep = gameData.tutorialStep;
		world.gameStep = gameData.gameStep;
		world.worldResourceManager.SetWorldGoldLevel(gameData.goldAmount);
		world.ambushes = gameData.ambushes;
		world.cityCount = gameData.cityCount;
		world.infantryCount = gameData.infantryCount;
		world.rangedCount = gameData.rangedCount;
		world.cavalryCount = gameData.cavalryCount;
		world.traderCount = gameData.traderCount;
		world.boatTraderCount = gameData.boatTraderCount;
		world.laborerCount = gameData.laborerCount;
		world.food = gameData.food;
		world.lumber = gameData.lumber;
		world.popGrowth = gameData.popGrowth;
		world.popLost = gameData.popLost;
		world.researchTree.LoadCurrentResearch(gameData.currentResearch, gameData.researchAmount);
		world.currentWorkedTileDict = new(gameData.currentWorkedTileDict);
		gameData.currentWorkedTileDict.Clear();
		world.cityWorkedTileDict = new(gameData.cityWorkedTileDict);
		gameData.cityWorkedTileDict.Clear();
		world.cityImprovementQueueList = new(gameData.cityImprovementQueueList);
		gameData.cityImprovementQueueList.Clear();
		world.unclaimedSingleBuildList = new(gameData.unclaimedSingleBuildList);
		gameData.unclaimedSingleBuildList.Clear();
		world.LoadWonder(gameData.allWonders);
		world.scottFollow = gameData.scottFollow;
		world.azaiFollow = gameData.azaiFollow;
		world.startingLoc = gameData.startingLoc;
		gameData.allWonders.Clear();

		if (!world.scottFollow)
		{
			world.scott.gameObject.tag = "Character";
			world.scott.marker.gameObject.tag = "Character";
			world.scott.gameObject.SetActive(false);
			world.characterUnits.Remove(world.scott);
			world.RemoveUnitPosition(world.RoundToInt(world.scott.transform.position));
			world.unitMovement.uiWorkerTask.DeactivateButtons();
		}

		if (!world.azaiFollow)
		{
			world.azai.gameObject.tag = "Character";
			world.azai.marker.gameObject.tag = "Character";
			world.azai.gameObject.SetActive(false);
			world.characterUnits.Remove(world.azai);
			world.RemoveUnitPosition(world.RoundToInt(world.azai.transform.position));
		}

		GameManager.Instance.UpdateProgress(5);

		//cities
		for (int i = 0; i < gameData.allCities.Count; i++)
		{
			world.BuildCity(gameData.allCities[i].location, world.GetTerrainDataAt(gameData.allCities[i].location), UpgradeableObjectHolder.Instance.improvementDict["City-0"].prefab, gameData.allCities[i]);
		}
		gameData.allCities.Clear();
		gameData.allArmies.Clear();

		//updating attacked cities
		foreach (City city in attackingEnemyCitiesList)
		{
			if (city.enemyCamp.moveToLoc != city.enemyCamp.loc)
			{
				city.enemyCamp.attackingArmy = world.cityDict[city.enemyCamp.moveToLoc].army;
				world.cityDict[city.enemyCamp.moveToLoc].army.targetCamp = city.enemyCamp;
			}

			if (city.enemyCamp.pillage && city.enemyCamp.pillageTime > 0)
				StartCoroutine(city.enemyCamp.Pillage());
		}

		GameManager.Instance.UpdateProgress(10);

		//improvements
		for (int i = 0; i < gameData.allCityImprovements.Count; i++)
		{
			City city;
			if (gameData.allCityImprovements[i].cityLoc == new Vector3Int(0, -10, 0))
				city = null;
			else
				city = world.GetCity(gameData.allCityImprovements[i].cityLoc);

			world.CreateImprovement(city, gameData.allCityImprovements[i]);
		}
		gameData.allCityImprovements.Clear();
		gameData.militaryUnits.Clear();

		GameManager.Instance.UpdateProgress(20);
		for (int i = 0; i < gameData.allRoads.Count; i++)
		{
			if (!world.roadTileDict.ContainsKey(gameData.allRoads[i].position))
				world.roadManager.BuildRoadAtPosition(gameData.allRoads[i].position, gameData.allRoads[i].utilityLevel);
		}
		gameData.allRoads.Clear();

		//transports
		for (int i = 0; i < gameData.allTransports.Count; i++)
			world.CreateUnit(gameData.allTransports[i]);
		gameData.allTransports.Clear();
		
		if (gameData.scott.somethingToSay) world.scott.gameObject.SetActive(true);
		world.scott.LoadWorkerData(gameData.scott);

		if (gameData.azai.bodyGuardData.somethingToSay) world.azai.gameObject.SetActive(true);
		world.azai.LoadBodyGuardData(gameData.azai);
		world.mainPlayer.LoadWorkerData(gameData.playerUnit);
		world.mainPlayer.lastClearTile = world.RoundToInt(world.mainPlayer.transform.position);

		//traders
		for (int i = 0; i < gameData.allTraders.Count; i++)
			world.CreateUnit(gameData.allTraders[i]);
		gameData.allTraders.Clear();

		//laborers
		for (int i = 0; i < gameData.allLaborers.Count; i++)
			world.CreateUnit(gameData.allLaborers[i]);
		gameData.allLaborers.Clear();

		//updating progress
		GameManager.Instance.UpdateProgress(5);
		
		//duelling orders
		if (duelingLeader)
		{
			List<Vector3Int> battleZone = new() { duelingLeader.enemyCamp.loc };
			foreach (Vector3Int tile in world.GetNeighborsFor(duelingLeader.enemyCamp.loc, MapWorld.State.EIGHTWAY))
				battleZone.Add(tile);

			duelingLeader.enemyCamp.attackingArmy.movementRange = battleZone;
		}
		duelingLeader = null;

		//ambushes
		world.MakeEnemyAmbushes(gameData.ambushLocs, ambushedTraders);
		gameData.ambushLocs.Clear();
		ambushedTraders.Clear();

		//updating progress
		GameManager.Instance.UpdateProgress(5);

		//world.cameraController.transform.position = gameData.camPosition;
		world.cameraController.newPosition = gameData.camPosition;
		world.cameraController.newRotation = gameData.camRotation;
		world.dayNightCycle.timeODay = gameData.timeODay;
		if (gameData.timeODay > 18 || gameData.timeODay < 6)
		{
			world.dayNightCycle.day = false;
			world.ToggleWorldLights(true);
		}
		world.cameraController.LoadCameraLimits(gameData.camLimits[0], gameData.camLimits[1], gameData.camLimits[2], gameData.camLimits[3]);
		gameData.camLimits.Clear();

		//move orders
		foreach (Unit unit in unitMoveOrders.Keys)
		{
			if (unit.gameObject.activeSelf)
				unit.MoveThroughPath(unitMoveOrders[unit]);
		}
		unitMoveOrders.Clear();

		//attack info
		for (int i = 0; i < attackingUnitList.Count; i++)
		{
			attackingUnitList[i].LoadAttack();
		}
		attackingUnitList.Clear();

		foreach (City city in attackingEnemyCitiesList)
		{
			if (city.enemyCamp.inBattle)
				world.ToggleCityMaterialClear(city.cityLoc, city.enemyCamp.attackingArmy.city.cityLoc, city.enemyCamp.attackingArmy.enemyTarget, city.enemyCamp.attackingArmy.attackZone, true);
		}
		attackingEnemyCitiesList.Clear();

		foreach (Vector3Int loc in gameData.attackedEnemyBases.Keys)
		{
			if (gameData.attackedEnemyBases[loc].inBattle)
				world.ToggleCityMaterialClear(world.GetEnemyCamp(loc).loc, world.GetEnemyCamp(loc).attackingArmy.city.cityLoc, world.GetEnemyCamp(loc).attackingArmy.enemyTarget, world.GetEnemyCamp(loc).attackingArmy.attackZone, true);
		}

		world.uiAttackWarning.LoadAttackLocs(gameData.attackLocs);
		gameData.attackLocs.Clear();

		//research wait list
		for (int i = 0; i < gameData.researchWaitList.Count; i++)
			world.researchWaitList.Add(world.GetResourceProducer(gameData.researchWaitList[i]));
		gameData.researchWaitList.Clear();

		//gold city wait list
		for (int i = 0; i < gameData.goldCityWaitList.Count; i++)
			world.goldCityWaitList.Add(world.GetCity(gameData.goldCityWaitList[i]));
		gameData.researchWaitList.Clear();

		//gold city route wait list
		for (int i = 0; i < gameData.goldCityRouteWaitList.Count; i++)
			world.goldCityRouteWaitList.Add(world.GetCity(gameData.goldCityRouteWaitList[i]));
		gameData.researchWaitList.Clear();

		//gold wonder wait list
		for (int i = 0; i < gameData.goldWonderWaitList.Count; i++)
			world.goldWonderWaitList.Add(world.GetWonder(gameData.goldWonderWaitList[i]));
		gameData.researchWaitList.Clear();

		//gold trade center wait list
		for (int i = 0; i < gameData.goldTradeCenterWaitList.Count; i++)
			world.goldTradeCenterWaitList.Add(world.GetTradeCenter(gameData.goldTradeCenterWaitList[i]));
		gameData.researchWaitList.Clear();

		//trade center waiting lists
		foreach (TradeCenter center in centerWaitingDict.Keys)
		{
			(List<int> waitList, List<int> seaWaitList) = centerWaitingDict[center];
			center.SetWaitList(waitList);
			center.SetSeaWaitList(seaWaitList);
		}
		centerWaitingDict.Clear();

		//wonder waiting lists
		foreach (Wonder wonder in wonderWaitingDict.Keys)
		{
			(List<int> waitList, List<int> seaWaitList) = wonderWaitingDict[wonder];
			wonder.SetWaitList(waitList);
			wonder.SetSeaWaitList(seaWaitList);
		}
		wonderWaitingDict.Clear();

		//city waiting lists
		foreach (City city in cityWaitingDict.Keys)
		{
			(List<Vector3Int> producersWaiting, List<Vector3Int> producersStorageWaiting, List<Vector3Int> producersUnloadWaiting,
				List<int> waitList, List<int> seaWaitList, List<int> tradersWaiting, List<int> tradersHere) = cityWaitingDict[city];
			city.SetProducerWaitingList(producersWaiting);
			city.SetProducerStorageRoomWaitingList(producersStorageWaiting);
			city.SetWaitingToUnloadProducerList(producersUnloadWaiting);
			//city.SetWaitingToUnloadResearchList(researchUnloadWaiting);
			city.SetWaitList(waitList);
			city.SetSeaWaitList(seaWaitList);
			city.SetTraderRouteWaitingList(tradersWaiting);
			city.SetTradersHereList(tradersHere);
		}
		cityWaitingDict.Clear();
			
		foreach (CityImprovement improvement in improvementUnitUpgradeDict.Keys)
		{
			improvement.ResumeTraining(improvementUnitUpgradeDict[improvement]);
		}
		improvementUnitUpgradeDict.Clear();

		foreach (Vector3Int tile in gameData.treasureLocs.Keys)
		{
			world.LoadTreasureChest(tile, gameData.treasureLocs[tile].Item1, gameData.treasureLocs[tile].Item2);
		}

		//loading conversation task list
		foreach (string task in gameData.conversationTasks)
		{
			world.uiConversationTaskManager.CreateConversationTask(task, true);
		}

		//combining meshes for orphans
		world.cityBuilderManager.CombineMeshes();

		world.researchTree.SetCurrentEra(world.currentEra);

		GameManager.Instance.UpdateProgress(10);

		//Time.timeScale = 1f;
		//AudioListener.pause = false;
		StartCoroutine(WaitASec());
	}

	private IEnumerator WaitASec()
	{
		yield return new WaitForSeconds(1f);

		isDone = true;
		isLoading = false;
		GameManager.Instance.isLoading = false;
	}

	public void QuitToMenu()
	{
		world.uiMainMenu.ToggleVisibility(false);
		GameManager.Instance.BackToMainMenu(false);
	}

	public void RemoveEnemyCamp(Vector3Int loc)
	{
		gameData.enemyCampLocs.Remove(loc);
		gameData.attackedEnemyBases.Remove(loc);
	}

	public void RemoveEnemyCity(Vector3Int loc)
	{
		//gameData.attackedEnemyBases.Remove(loc);
		gameData.movingEnemyBases.Remove(loc);
		gameData.enemyCities.Remove(loc);
	}
}
