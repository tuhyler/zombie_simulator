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

	private void Awake()
	{
		Instance = this;

		//LoadData();
	}

	public void SaveGame()
	{
		//GamePersist persist = new GamePersist();
		gameData.camPosition = world.cameraController.transform.position;
		gameData.camRotation = world.cameraController.transform.rotation;
		gameData.timeODay = world.dayNightCycle.timeODay;
		gameData.camLimits.Clear();
		gameData.camLimits.Add(world.cameraController.xMin);
		gameData.camLimits.Add(world.cameraController.xMax);
		gameData.camLimits.Add(world.cameraController.zMin);
		gameData.camLimits.Add(world.cameraController.zMax);

		//GameData gameData = new();

		////Terrain
		//foreach (TerrainData td in world.finiteResourceList)
		//	gameData.allTerrain[td.TileCoordinates].resourceAmount = td.resourceAmount;

		gameData.allWonders.Clear();
		foreach (Wonder wonder in world.allWonders)
			gameData.allWonders.Add(wonder.SaveData());
		//Wonders
		gameData.allCities.Clear();
		foreach (City city in world.cityDict.Values)
		{
			gameData.allCities.Add(city.SaveCityData());
		}

		gameData.allRoads.Clear();
		foreach (Vector3Int loc in world.roadTileDict.Keys)
		{
			Road road = world.roadTileDict[loc][0];

			if (road == null)
				road = world.roadTileDict[loc][1];

			gameData.allRoads.Add(road.SaveData(loc));
		}
		
		//CityImprovements


		gameData.playerUnit = world.mainPlayer.SaveUnitData();

		//Units, enemy and player

		gamePersist.SaveData("/game_data.save", gameData, false);
		world.uiMainMenu.ToggleVisibility(false);
	}

	public void LoadDataGame()
	{
		world.uiMainMenu.ToggleVisibility(false);
		GameManager.Instance.BackToMainMenu(true);
	}


	public void LoadData(string name)
	{
		isLoading = true;
		world.ClearMap();
		
		gameData = gamePersist.LoadData("/game_data.save", false);

		world.GenerateMap(gameData.allTerrain);
		world.GenerateTradeCenters(gameData.allTradeCenters);
		world.LoadWonder(gameData.allWonders);
		gameData.allWonders.Clear();
		//      //create trade centers

		//      WorldStartOrders();

		//      //create wonders

		foreach (CityData cityData in gameData.allCities)
		{
			world.BuildCity(cityData.location, world.GetTerrainDataAt(cityData.location), UpgradeableObjectHolder.Instance.improvementDict["City-0"].prefab, cityData);
		}
		gameData.allCities.Clear();

		foreach (RoadData roadData in gameData.allRoads)
		{
			if (!world.roadTileDict.ContainsKey(roadData.position))
				world.roadManager.BuildRoadAtPosition(roadData.position);
		}
		gameData.allRoads.Clear();

		//      //create cities
		//      //create city improvements
		//      //assign labor
		//      //create roads

		world.mainPlayer.LoadUnitData(gameData.playerUnit);

		//world.cameraController.transform.position = gameData.camPosition;
		world.cameraController.newPosition = gameData.camPosition;
		world.cameraController.newRotation = gameData.camRotation;
		world.dayNightCycle.timeODay = gameData.timeODay;
		world.cameraController.LoadCameraLimits(gameData.camLimits[0], gameData.camLimits[1], gameData.camLimits[2], gameData.camLimits[3]);
		gameData.camLimits.Clear();

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
}
