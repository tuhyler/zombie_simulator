using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class GameLoader : MonoBehaviour
{
    public static GameLoader Instance;

	public MapWorld world;

	[HideInInspector]
	public GamePersist gamePersist = new();
	[HideInInspector]
	public GameData gameData = new();
	[HideInInspector]
	public bool isLoading, isDone;
	[HideInInspector]
	public List<Unit> attackingUnitList = new();
	public Dictionary<TradeCenter, (List<int>, List<int>)> centerWaitingDict = new();
	public Dictionary<Wonder, (List<int>, List<int>)> wonderWaitingDict = new();
	public Dictionary<City, (List<Vector3Int>, List<int>, List<int>, List<int>, List<int>)> cityWaitingDict = new();
	public Dictionary<CityImprovement, string> improvementUnitUpgradeDict = new();
	[HideInInspector]
	public List<GameObject> textList = new();

	private void Awake()
	{
		Instance = this;
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
		gameData.currentWorkedTileDict = world.currentWorkedTileDict;
		gameData.cityWorkedTileDict = world.cityWorkedTileDict;
		gameData.cityImprovementQueueList = world.cityImprovementQueueList;
		gameData.unclaimedSingleBuildList = world.unclaimedSingleBuildList;
		gameData.camPosition = world.cameraController.transform.position;
		gameData.camRotation = world.cameraController.transform.rotation;
		gameData.timeODay = world.dayNightCycle.timeODay;
		gameData.camLimits.Clear();
		gameData.camLimits.Add(world.cameraController.xMin);
		gameData.camLimits.Add(world.cameraController.xMax);
		gameData.camLimits.Add(world.cameraController.zMin);
		gameData.camLimits.Add(world.cameraController.zMax);

		List<Vector3Int> enemyCampLocs = new List<Vector3Int>(gameData.attackedEnemyBases.Keys);
		for (int i = 0; i < enemyCampLocs.Count; i++)
		{
			gameData.attackedEnemyBases[enemyCampLocs[i]] = world.GetEnemyCamp(enemyCampLocs[i]).SendCampUnitData();
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
		
		//main player
		gameData.playerUnit = world.mainPlayer.SaveWorkerData();

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
		isLoading = true;
		Time.timeScale = 0f;
		AudioListener.pause = true;

		world.ClearMap();
		
		gameData = gamePersist.LoadData(saveName, false);

		world.GenerateMap(gameData.allTerrain);
		world.GenerateTradeCenters(gameData.allTradeCenters);
		world.MakeEnemyCamps(gameData.enemyCampLocs, gameData.discoveredEnemyCampLocs);
		world.currentWorkedTileDict = gameData.currentWorkedTileDict;
		world.cityWorkedTileDict = gameData.cityWorkedTileDict;
		world.cityImprovementQueueList = gameData.cityImprovementQueueList;
		world.unclaimedSingleBuildList = gameData.unclaimedSingleBuildList;
		world.LoadWonder(gameData.allWonders);
		gameData.allWonders.Clear();

		for (int i = 0; i < gameData.allCities.Count; i++)
		{
			world.BuildCity(gameData.allCities[i].location, world.GetTerrainDataAt(gameData.allCities[i].location), UpgradeableObjectHolder.Instance.improvementDict["City-0"].prefab, gameData.allCities[i]);
		}
		gameData.allCities.Clear();
		gameData.allArmies.Clear();

		for (int i = 0; i < gameData.allCityImprovements.Count; i++)
		{
			world.CreateImprovement(world.GetCity(gameData.allCityImprovements[i].cityLoc), gameData.allCityImprovements[i]);
		}
		gameData.allCityImprovements.Clear();
		gameData.militaryUnits.Clear();

		for (int i = 0; i < gameData.allRoads.Count; i++)
		{
			if (!world.roadTileDict.ContainsKey(gameData.allRoads[i].position))
				world.roadManager.BuildRoadAtPosition(gameData.allRoads[i].position);
		}
		gameData.allRoads.Clear();

		//      //assign labor

		world.mainPlayer.LoadWorkerData(gameData.playerUnit);

		//traders
		for (int i = 0; i < gameData.allTraders.Count; i++)
		{
			world.CreateUnit(gameData.allTraders[i]);
		}

		//laborers
		for (int i = 0; i < gameData.allLaborers.Count; i++)
		{
			world.CreateUnit(gameData.allLaborers[i]);
		}

		//world.cameraController.transform.position = gameData.camPosition;
		world.cameraController.newPosition = gameData.camPosition;
		world.cameraController.newRotation = gameData.camRotation;
		world.dayNightCycle.timeODay = gameData.timeODay;
		world.cameraController.LoadCameraLimits(gameData.camLimits[0], gameData.camLimits[1], gameData.camLimits[2], gameData.camLimits[3]);
		gameData.camLimits.Clear();

		for (int i = 0; i < attackingUnitList.Count; i++)
		{
			attackingUnitList[i].LoadAttack();
		}
		attackingUnitList.Clear();

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
			(List<Vector3Int> producersWaiting, List<int> waitList, List<int> seaWaitList, List<int> tradersWaiting, List<int> tradersHere) = cityWaitingDict[city];
			city.SetProducerWaitingList(producersWaiting);
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

		Time.timeScale = 1f;
		AudioListener.pause = false;
		StartCoroutine(WaitASec());

		////create units
		////populate dictionaries

		//uiMainMenu.ToggleVisibility(false);

	}

	private IEnumerator WaitASec()
	{
		yield return new WaitForSeconds(1f);

		isDone = true;
		isLoading = false;
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
