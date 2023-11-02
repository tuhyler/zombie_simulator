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
	public GameData gameData;
	[HideInInspector]
	public bool isDone;

	private void Awake()
	{
		Instance = this;
	}

	public void SaveGame()
	{
		//GamePersist persist = new GamePersist();
		gameData.camPosition = world.cameraController.transform.position;
		gameData.camRotation = world.cameraController.transform.rotation;
		gameData.timeODay = world.dayNightCycle.timeODay;
		//GameData gameData = new();

		//Terrain
		//foreach (TerrainData td in finiteResourceList)
		//	gameData.allTerrain[td.TileCoordinates].resourceAmount = td.resourceAmount;

		gameData.allCities.Clear();
		//foreach (City city in cityDict.Values)
		//{
		//	gameData.allCities.Add(city.SaveCityData());
		//}

		//CityImprovements
		//Roads
		//Wonders


		gameData.playerUnit = world.mainPlayer.SaveUnitData();

		//Units, enemy and player

		gamePersist.SaveData("/game_data.save", gameData, false);
		world.uiMainMenu.ToggleVisibility(false);
	}

	public void LoadData()
	{
		foreach (Transform go in world.terrainHolder)
			Destroy(go.gameObject);

		foreach (Transform go in world.tradeCenterHolder)
			Destroy(go.gameObject);

		gameData = gamePersist.LoadData("/game_data.save", false);

		world.cameraController.transform.position = gameData.camPosition;
		world.cameraController.transform.rotation = gameData.camRotation;
		world.dayNightCycle.timeODay = gameData.timeODay;

		world.GenerateMap(gameData.allTerrain);
		//      //create trade centers

		//      WorldStartOrders();

		//      //create wonders

		//foreach (CityData cityData in gameData.allCities)
		//{
		//	world.BuildCity(cityData.location, world.GetTerrainDataAt(cityData.location), UpgradeableObjectHolder.Instance.improvementDict["City-0"].prefab, cityData);
		//}
		gameData.allCities.Clear();

		//      //create cities
		//      //create city improvements
		//      //assign labor
		//      //create roads

		world.mainPlayer.LoadUnitData(gameData.playerUnit);

		StartCoroutine(WaitASec());

		////create units
		////populate dictionaries

		//uiMainMenu.ToggleVisibility(false);
	}

	private IEnumerator WaitASec()
	{
		yield return new WaitForSeconds(0.5f);

		isDone = true;
	}


}
