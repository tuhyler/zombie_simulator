using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UINewGameMenu : MonoBehaviour
{
	[SerializeField]
	private TitleScreen titleScreen;

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
		GameManager.Instance.NewGame(tutorial);
	}
}
