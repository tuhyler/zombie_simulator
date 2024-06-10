using UnityEngine;

public class TitleScreen : MonoBehaviour
{
    public UISaveGame uiLoadGame;
    public UISettings uiSettings;
    public UINewGameMenu uiNewGame;
    private AudioSource audioSource;

    public AudioClip closeClip, selectClip, checkClip;
    public Texture2D cursorArrow;

	private void Awake()
	{
        audioSource = GetComponent<AudioSource>();
        uiLoadGame.PopulateSaveItems();
		Cursor.SetCursor(cursorArrow, Vector2.zero, CursorMode.ForceSoftware);
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
            Cursor.visible = false;
			GameManager.Instance.NewGame("SouthToggle", "ContinentsToggle", "NormalToggle", "ModerateToggle", "MediumToggle", true, Random.Range(0, 9999999));
        }
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
