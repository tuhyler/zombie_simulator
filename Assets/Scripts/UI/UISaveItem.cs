using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UISaveItem : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    public TMP_Text saveItemText;

	[HideInInspector]
	public DateTime dateTime;

    [SerializeField]
    private Image background;

	public UISaveGame uiSaveGame;

    public string saveName, version, fileName;
    public float playTime;
    public Sprite screenshot;

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
	}

	public void UnselectItem()
	{
		saveItemText.color = originalTextColor;
		background.color = originalBackgroundColor;
	}
}
