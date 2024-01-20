using System.Collections;
using System.Collections.Generic;
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
	public List<Unit> attackingUnitList = new();
	public Dictionary<string, Trader> ambushedTraders = new();
	public Dictionary<TradeCenter, (List<int>, List<int>)> centerWaitingDict = new();
	public Dictionary<Wonder, (List<int>, List<int>)> wonderWaitingDict = new();
	public Dictionary<City, (List<Vector3Int>, List<Vector3Int>, List<Vector3Int>, List<int>, List<int>, List<int>, List<int>)> cityWaitingDict = new();
	public Dictionary<CityImprovement, string> improvementUnitUpgradeDict = new();
	[HideInInspector]
	public List<GameObject> textList = new();

	private void Awake()
	{
		Instance = this;
	}

	public void NewGame(bool tutorial)
	{
		GameManager.Instance.ResetProgress();
		world.ClearMap();

		terrainGenerator.SetYCoord(0);
		Dictionary<Vector3Int, TerrainData> terrainDict = terrainGenerator.RunProceduralGeneration(true);
		terrainGenerator.SetMainPlayerLoc();
		world.NewGamePrep(true, terrainDict, tutorial);
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
		gameData.azai = world.azai.SaveWorkerData();

		//traders
		gameData.allTraders.Clear();
		for (int i = 0; i < world.traderList.Count; i++)
			gameData.allTraders.Add(world.traderList[i].SaveTraderData());

		//laborers
		gameData.allLaborers.Clear();
		for (int i = 0; i < world.laborerList.Count; i++)
			gameData.allLaborers.Add(world.laborerList[i].SaveLaborerData());

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

		world.GenerateTradeCenters(gameData.allTradeCenters);
		world.MakeEnemyCamps(gameData.enemyCampLocs, gameData.discoveredEnemyCampLocs);
		
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

		//updating progress
		GameManager.Instance.UpdateProgress(5);

		for (int i = 0; i < gameData.allCities.Count; i++)
		{
			world.BuildCity(gameData.allCities[i].location, world.GetTerrainDataAt(gameData.allCities[i].location), UpgradeableObjectHolder.Instance.improvementDict["City-0"].prefab, gameData.allCities[i]);
		}
		gameData.allCities.Clear();
		gameData.allArmies.Clear();

		//updating progress
		GameManager.Instance.UpdateProgress(10);

		for (int i = 0; i < gameData.allCityImprovements.Count; i++)
		{
			world.CreateImprovement(world.GetCity(gameData.allCityImprovements[i].cityLoc), gameData.allCityImprovements[i]);
		}
		gameData.allCityImprovements.Clear();
		gameData.militaryUnits.Clear();

		//updating progress
		GameManager.Instance.UpdateProgress(20);

		for (int i = 0; i < gameData.allRoads.Count; i++)
		{
			if (!world.roadTileDict.ContainsKey(gameData.allRoads[i].position))
				world.roadManager.BuildRoadAtPosition(gameData.allRoads[i].position);
		}
		gameData.allRoads.Clear();

		//updating progress
		GameManager.Instance.UpdateProgress(10);

		if (gameData.scott.somethingToSay) world.scott.gameObject.SetActive(true);
		world.scott.LoadWorkerData(gameData.scott);

		if (gameData.azai.somethingToSay) world.azai.gameObject.SetActive(true);
		world.azai.LoadWorkerData(gameData.azai);
		world.mainPlayer.LoadWorkerData(gameData.playerUnit);

		//traders
		for (int i = 0; i < gameData.allTraders.Count; i++)
		{
			world.CreateUnit(gameData.allTraders[i]);
		}
		gameData.allTraders.Clear();

		//updating progress
		GameManager.Instance.UpdateProgress(5);

		//laborers
		for (int i = 0; i < gameData.allLaborers.Count; i++)
		{
			world.CreateUnit(gameData.allLaborers[i]);
		}
		gameData.allLaborers.Clear();

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

		//attack info
		for (int i = 0; i < attackingUnitList.Count; i++)
		{
			attackingUnitList[i].LoadAttack();
		}
		attackingUnitList.Clear();

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

		//loading conversation task list
		foreach (string task in gameData.conversationTaskDict.Keys)
		{
			world.uiConversationTaskManager.LoadConversationTask(task, gameData.conversationTaskDict[task].Item1, gameData.conversationTaskDict[task].Item2);
		}
		//updating progress
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
}
