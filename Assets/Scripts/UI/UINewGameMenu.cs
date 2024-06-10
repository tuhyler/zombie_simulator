using System.Linq;
using TMPro;
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

	[SerializeField]
	private TMP_InputField seedInput;
	
	private bool tutorial = false;
	
	[HideInInspector]
	public bool activeStatus;

	private void Start()
	{
		gameObject.SetActive(false);
		seedInput.onValidateInput += delegate (string input, int charIndex, char addedChar) { return PositiveIntCheck(addedChar); };
	}

	public void ToggleVisibility(bool v)
	{
		if (activeStatus == v)
			return;

		if (v)
		{
			gameObject.SetActive(true);
			activeStatus = true;
			seedInput.text = Random.Range(0, 9999999).ToString();
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
		titleScreen.PlayCheckAudio();
		tutorialToggle.isOn = v;
		tutorial = v;
	}

	public void StartNewGame()
	{
		titleScreen.PlaySelectAudio();
		int seed;

		if (int.TryParse(seedInput.text, out int result))
			seed = result;
		else
			seed = Random.Range(0, 9999999);

		//titleScreen.uiLoadGame.ClearSaveItems();
		string starting = startingGroup.ActiveToggles().FirstOrDefault().name;
		string mapSize = mapSizeGroup.ActiveToggles().FirstOrDefault().name;
		string landType = landTypeGroup.ActiveToggles().FirstOrDefault().name;
		string resource = resourceGroup.ActiveToggles().FirstOrDefault().name;
		//string mountains = mountainGroup.ActiveToggles().FirstOrDefault().name;
		string enemy = enemyGroup.ActiveToggles().FirstOrDefault().name;
		Cursor.visible = false;
		GameManager.Instance.NewGame(starting, landType, resource, /*mountains,*/enemy, mapSize, tutorial, seed);
	}

	private char PositiveIntCheck(char charToValidate) //ensuring numbers are positive
	{
		if (charToValidate != '1'
			&& charToValidate != '2'
			&& charToValidate != '3'
			&& charToValidate != '4'
			&& charToValidate != '5'
			&& charToValidate != '6'
			&& charToValidate != '7'
			&& charToValidate != '8'
			&& charToValidate != '9'
			&& charToValidate != '0')
		{
			charToValidate = '\0';
		}

		return charToValidate;
	}
}
