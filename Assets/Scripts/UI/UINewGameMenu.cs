using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UINewGameMenu : MonoBehaviour
{
	[SerializeField]
	private TitleScreen titleScreen;

	[SerializeField]
	private ToggleGroup startingGroup, landTypeGroup, /*mountainGroup,*/enemyGroup, resourceGroup, mapSizeGroup;

	[SerializeField]
	private Toggle tutorialToggle;
	private bool tutorial = false;

	[HideInInspector]
	public bool activeStatus;

	private void Start()
	{
		gameObject.SetActive(false);
	}

	public void ToggleVisibility(bool v)
	{
		if (activeStatus == v)
			return;

		if (v)
		{
			gameObject.SetActive(true);
			activeStatus = true;
		}
		else
		{
			gameObject.SetActive(false);
			activeStatus = false;
		}
	}

	public void CloseWindowButton()
	{
		titleScreen.PlayCloseAudio();
		ToggleVisibility(false);
	}

	public void SetTutorialToggle(bool v)
	{
		tutorialToggle.isOn = v;
		tutorial = v;
	}

	public void StartNewGame()
	{
		string starting = startingGroup.ActiveToggles().FirstOrDefault().name;
		string mapSize = mapSizeGroup.ActiveToggles().FirstOrDefault().name;
		string landType = landTypeGroup.ActiveToggles().FirstOrDefault().name;
		string resource = resourceGroup.ActiveToggles().FirstOrDefault().name;
		//string mountains = mountainGroup.ActiveToggles().FirstOrDefault().name;
		string enemy = enemyGroup.ActiveToggles().FirstOrDefault().name;
		GameManager.Instance.NewGame(starting, landType, resource, /*mountains,*/enemy, mapSize, tutorial);
	}
}
