using System.Collections;
using TMPro;
using UnityEngine;

public class TitleScreen : MonoBehaviour
{
    public UISaveGame uiLoadGame;
    public UISettings uiSettings;
    public UINewGameMenu uiNewGame;
    public TMP_Text startContinueText;
    public AudioSource musicSource;
    private AudioSource audioSource;

    public AudioClip closeClip, selectClip, checkClip;
    //public Texture2D cursorArrow, cursorArrowMed, cursorArrowBig;

	private void Awake()
	{
        audioSource = GetComponent<AudioSource>();
        uiLoadGame.PopulateSaveItems();
        CursorCheck();

        startContinueText.text = uiLoadGame.currentSaves.Count > 0 ? "Continue" : "Start Game";
	}

    public void PlayMusic()
    {
        StartCoroutine(WaitASec());
    }

    private IEnumerator WaitASec()
    {
        yield return new WaitForSeconds(1);

        if (!uiSettings.muted)
            musicSource.Play();
    }

	public void CursorCheck()
	{
		if (Screen.width > 3000)
			Cursor.SetCursor(Resources.Load<Texture2D>("Prefabs/MiscPrefabs/cursor_gold_big"), Vector2.zero, CursorMode.ForceSoftware);
		else if (Screen.width > 1920)
			Cursor.SetCursor(Resources.Load<Texture2D>("Prefabs/MiscPrefabs/cursor_gold_med"), Vector2.zero, CursorMode.ForceSoftware);
		else
			Cursor.SetCursor(Resources.Load<Texture2D>("Prefabs/MiscPrefabs/cursor_gold"), Vector2.zero, CursorMode.ForceSoftware);
	}

	public void StartContinueGame()
    {
        if (uiLoadGame.saveHolder.childCount > 0)
        {
            Cursor.visible = false;
            GameManager.Instance.LoadGame(uiLoadGame.GetNameOfLatestSave());
        }
        else
        {
            InstaStartNewGame();
        }
    }

	public void NewGame()
    {
        if (uiLoadGame.activeStatus)
            uiLoadGame.ToggleVisibility(false);

        if (uiSettings.activeStatus)
            uiSettings.ToggleVisibility(false);

        if (uiNewGame.activeStatus)
            return;

		PlaySelectAudio();
		
        if (uiLoadGame.currentSaves.Count > 0)
        {
            uiNewGame.ToggleVisibility(true);
        }
        else
        {
            InstaStartNewGame();
        }
	}

    private void InstaStartNewGame()
    {
		Cursor.visible = false;
		GameManager.Instance.NewGame("SouthToggle", "ContinentsToggle", "NormalToggle", "ModerateToggle", "MediumToggle", true, Random.Range(0, 9999999));
	}

    public void LoadGame()
    {
        if (uiLoadGame.activeStatus)
            return;

        if (uiSettings.activeStatus)
            uiSettings.ToggleVisibility(false);

        if (uiNewGame.activeStatus)
            uiNewGame.ToggleVisibility(false);

		PlaySelectAudio();
		uiLoadGame.ToggleVisibility(true, true);
    }

    public void SettingsMenu()
    {
        if (uiLoadGame.activeStatus)
            uiLoadGame.ToggleVisibility(false);

        if (uiSettings.activeStatus)
            return;

        if (uiNewGame.activeStatus)
            uiNewGame.ToggleVisibility(false);

        PlaySelectAudio();
        uiSettings.ToggleVisibility(true);
	}

    public void ExitGame()
    {
		if (uiSettings.activeStatus)
			uiSettings.ToggleVisibility(false);

		if (uiNewGame.activeStatus)
			uiNewGame.ToggleVisibility(false);

		if (uiLoadGame.activeStatus)
			uiLoadGame.ToggleVisibility(false);

		Application.Quit();
    }

	public void PlayCloseAudio()
	{
		audioSource.clip = closeClip;
		audioSource.Play();
	}

	public void PlaySelectAudio()
	{
		audioSource.clip = selectClip;
		audioSource.Play();
	}

	public void PlayCheckAudio()
	{
		audioSource.clip = checkClip;
		audioSource.Play();
	}
}
