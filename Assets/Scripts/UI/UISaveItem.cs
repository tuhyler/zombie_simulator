using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.UI;

public class UISaveItem : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    public TMP_Text saveItemText;

	[HideInInspector]
	public DateTime dateTime;

    [SerializeField]
    private Image background;

	[HideInInspector]
	public UISaveGame uiSaveGame;

	[HideInInspector]
    public string saveName, version, fileName;

	[HideInInspector]
	public float playTime;

	[HideInInspector]
	public Sprite screenshot;

	//[HideInInspector]
	//public bool loaded;

	//for unselecting
	private Color originalTextColor;
	private Color originalBackgroundColor;

	private void Awake()
	{
		originalTextColor = saveItemText.color;
		originalBackgroundColor = background.color;
	}

	public void SetSaveGameMenu(UISaveGame uiSaveGame)
	{
		this.uiSaveGame = uiSaveGame;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		SelectItem();
	}

	public void SelectItem()
	{
		uiSaveGame.SelectItem(this);
		saveItemText.color = Color.white;
		background.color = Color.gray;

		//GameData gameData;
		//if (GameLoader.Instance == null)
		//	gameData = GameManager.Instance.GetLoadInfo(fileName);
		//else
		//	gameData = GameLoader.Instance.gamePersist.LoadData(fileName, false);

		//if (gameData != null)
		//{
		//playTime = gameData.savePlayTime;
		//version = gameData.saveVersion;
		////Texture2D texture = new Texture2D(1200, 900, TextureFormat.ARGB32, false);
		////Rect rect = new Rect(0, 0, texture.width, texture.height);
		////texture.LoadImage(Convert.FromBase64String(gameData.saveScreenshot));
		//screenshot = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));

		Texture2D texture = Resources.Load("SaveScreens/" + saveName) as Texture2D;

		if (texture != null)
		{
			Rect rect = new(0, 0, texture.width, texture.height);
			screenshot = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
			uiSaveGame.screenshot.sprite = screenshot;
		}
				//dateTime = Convert.ToDateTime(gameData.saveDate);
			//}

			//loaded = true;
	}

	public void UnselectItem()
	{
		saveItemText.color = originalTextColor;
		background.color = originalBackgroundColor;
		screenshot = null;
		uiSaveGame.screenshot.sprite = null;
	}
}
