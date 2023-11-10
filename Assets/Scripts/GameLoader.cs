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
	public Dictionary<City, (List<Vector3Int>, List<int>, List<int>)> cityWaitingDict = new();

	private void Awake()
	{
		Instance = this;
	}

	public void SaveGame(string saveName, float playTime, string version, string screenshot)
	{
		//string saveName = "game_data";
		saveName = "/" + saveName + ".save";
		gameData.saveName = saveName;
		gameData.savePlayTime += playTime;
		gameData.saveVersion = version;
		gameData.saveScreenshot = screenshot;
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
		gameData.playerUnit = world.mainPlayer.SaveMilitaryUnitData();

		//traders
		gameData.allTraders.Clear();
		for (int i = 0; i < world.traderList.Count; i++)
			gameData.allTraders.Add(world.traderList[i].SaveTraderData());

		//laborers
		gameData.allLaborers.Clear();
		for (int i = 0; i < world.laborerList.Count; i++)
			gameData.allLaborers.Add(world.laborerList[i].SaveLaborerData());

		if (gamePersist.SaveData(saveName, gameData, false))
		{
			UIInfoPopUpHandler.WarningMessage().Create(Vector3.zero, "Game Saved!");
		}
		else
		{
			UIInfoPopUpHandler.WarningMessage().Create(Vector3.zero, "Failed to save...");
		}

		Resources.UnloadUnusedAssets();
		world.uiMainMenu.uiSaveGame.ToggleVisibility(false);
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
		world.LoadWonder(gameData.allWonders);

		for (int i = 0; i < gameData.allCities.Count; i++)
		{
			world.BuildCity(gameData.allCities[i].location, world.GetTerrainDataAt(gameData.allCities[i].location), UpgradeableObjectHolder.Instance.improvementDict["City-0"].prefab, gameData.allCities[i]);
		}

		for (int i = 0; i < gameData.allCityImprovements.Count; i++)
		{
			world.CreateImprovement(world.GetCity(gameData.allCityImprovements[i].cityLoc), gameData.allCityImprovements[i]);
		}

		for (int i = 0; i < gameData.allRoads.Count; i++)
		{
			if (!world.roadTileDict.ContainsKey(gameData.allRoads[i].position))
				world.roadManager.BuildRoadAtPosition(gameData.allRoads[i].position);
		}

		//      //assign labor


		world.mainPlayer.LoadUnitData(gameData.playerUnit);

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

		for (int i = 0; i < attackingUnitList.Count; i++)
		{
			attackingUnitList[i].LoadAttack();
		}

		//city waiting lists
		foreach (City city in cityWaitingDict.Keys)
		{
			(List<Vector3Int> producersWaiting, List<int> tradersWaiting, List<int> tradersHere) = cityWaitingDict[city];
			city.SetProducerWaitingList(producersWaiting);
			city.SetTraderRouteWaitingList(tradersWaiting);
			city.SetTradersHereList(tradersHere);
		}
			

		gameData.allWonders.Clear();
		gameData.allCities.Clear();
		gameData.allArmies.Clear();
		gameData.allCityImprovements.Clear();
		gameData.militaryUnits.Clear();
		gameData.allRoads.Clear();
		gameData.camLimits.Clear();
		attackingUnitList.Clear();
		cityWaitingDict.Clear();

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
