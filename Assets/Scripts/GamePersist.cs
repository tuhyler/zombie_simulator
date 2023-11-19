using System.IO;
using System;
using Newtonsoft.Json;
using UnityEngine;
using System.Security.Cryptography;
using System.Text;

public class GamePersist
{
	public bool loadNewGame;
	private const string KEY = "DXDnfYABkQ6w96nqLIjAZM0JGMl5vNx/iJH86d0vN2M=";
	private const string IV = "mBWbRm5jUWvdaAh8UzS2NQ==";

	public bool SaveData(string relativePath, GameData data, bool encrypted)
	{
		string path = Application.persistentDataPath + relativePath;

		try
		{
			if (File.Exists(path))
				File.Delete(path);

			using FileStream stream = File.Create(path);
			if (encrypted)
			{
				WriteEncryptedData(data, stream);
			}
			else
			{
				stream.Close();
				File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.None));
			}

			return true;
		}
		catch (Exception e)
		{
			Debug.LogError($"Unable to save data due to {e.Message} {e.StackTrace}");

			return false;
		}
	}

	private void WriteEncryptedData(GameData data, FileStream stream)
	{
		using Aes aesProvider = Aes.Create();
		//Debug.Log($"Initialization Vector: {Convert.ToBase64String(aesProvider.IV)}");
		//Debug.Log($"Key: {Convert.ToBase64String(aesProvider.Key)}");
		aesProvider.Key = Convert.FromBase64String(KEY);
		aesProvider.IV = Convert.FromBase64String(IV);
		using ICryptoTransform cryptoTransform = aesProvider.CreateEncryptor();
		using CryptoStream cryptoStream = new CryptoStream(stream, cryptoTransform, CryptoStreamMode.Write);

		cryptoStream.Write(Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(data)));
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
			GameData data;

			if (encrypted)
			{
				data = ReadEncryptedData(path);
			}
			else
			{
				data = JsonConvert.DeserializeObject<GameData>(File.ReadAllText(path));
			}

			return data;
		}
		catch (Exception e)
		{
			Debug.LogError($"Failed to load file due to {e.Message} {e.StackTrace}");
			throw e;
		}
	}

	private GameData ReadEncryptedData(string path)
	{
		byte[] fileBytes = File.ReadAllBytes(path);
		using Aes aesProvider = Aes.Create();

		aesProvider.Key = Convert.FromBase64String(KEY);
		aesProvider.IV = Convert.FromBase64String(IV);

		using ICryptoTransform cryptoTransform = aesProvider.CreateDecryptor(aesProvider.Key, aesProvider.IV);
		using MemoryStream decryptionStream = new(fileBytes);
		using CryptoStream cryptoStream = new CryptoStream(decryptionStream, cryptoTransform, CryptoStreamMode.Read);
		using StreamReader reader = new StreamReader(cryptoStream);
		string result = reader.ReadToEnd();

		return JsonConvert.DeserializeObject<GameData>(result); ;
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
