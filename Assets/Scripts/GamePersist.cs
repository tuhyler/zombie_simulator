using System.IO;
using System;
using Newtonsoft.Json;
using UnityEngine;

public class GamePersist
{
	public bool loadNewGame;

	public bool SaveData(string relativePath, GameData data, bool encrypted)
	{
		string path = Application.persistentDataPath + relativePath;

		try
		{
			if (File.Exists(path))
				File.Delete(path);

			using FileStream stream = File.Create(path);
			stream.Close();
			File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.None));

			return true;
		}
		catch (Exception e)
		{
			Debug.LogError($"Unable to save data due to {e.Message} {e.StackTrace}");

			return false;
		}
	}

	public GameData LoadData(string relativePath, bool encrypted)
	{
		string path = Application.persistentDataPath + relativePath;

		if (!File.Exists(path))
		{
			Debug.Log($"Cannot find file at {path}");
			throw new FileNotFoundException($"{path} does not exists");
		}

		try
		{
			GameData data = JsonConvert.DeserializeObject<GameData>(File.ReadAllText(path));
			return data;
		}
		catch (Exception e)
		{
			Debug.LogError($"Failed to load file due to {e.Message} {e.StackTrace}");
			throw e;
		}
	}

	public void SaveSettings(SettingsData data)
	{
		string path = Application.persistentDataPath + "/settings.game";

		try
		{
			if (File.Exists(path))
				File.Delete(path);

			using FileStream stream = File.Create(path);
			stream.Close();
			File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.None));
		}
		catch (Exception e)
		{
			Debug.LogError($"Unable to save data due to {e.Message} {e.StackTrace}");
		}
	}

	public bool SettingsFileCheck()
	{
		string path = Application.persistentDataPath + "/settings.game";

		return File.Exists(path);
	}

	public SettingsData LoadSettings()
	{
		string path = Application.persistentDataPath + "/settings.game";

		if (!File.Exists(path))
		{
			Debug.Log($"Cannot find file at {path}");
			throw new FileNotFoundException($"{path} does not exists");
		}

		try
		{
			SettingsData data = JsonConvert.DeserializeObject<SettingsData>(File.ReadAllText(path));
			return data;
		}
		catch (Exception e)
		{
			Debug.LogError($"Failed to load file due to {e.Message} {e.StackTrace}");
			throw e;
		}
	}
}

//public interface ISaveSystem
//{
//	void SaveData(GamePersist savedData);

//	void LoadData(GamePersist savedData);

//    //bool SaveData<T>(string relativePath, T data, bool encrypted);

//    //T LoadData<T>(string relativePath, bool encrypted);
//}
