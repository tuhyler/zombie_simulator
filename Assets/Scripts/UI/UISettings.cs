using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class UISettings : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;

    [SerializeField]
    private TitleScreen titleScreen;

    [SerializeField]
    private Slider masterVolume, musicVolume, soundVolume, ambienceVolume;

    [SerializeField]
    private TMP_Dropdown graphicsDropdown, resolutionDropdown;

    [SerializeField]
    private Toggle tutorialToggle;
    public bool tutorial = true;

    [HideInInspector]
    public bool activeStatus;

    [SerializeField]
    private AudioMixer audioMixer;

    Resolution[] resolutions;

	private void Awake()
	{
        resolutions = Screen.resolutions;

        List<string> options = new();
        int currentResolution = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + "x" + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
                currentResolution = i;
        }

		resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolution;
        resolutionDropdown.RefreshShownValue();
 	}

	private void Start()
	{
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.gamePersist.SettingsFileCheck())
            {
                SettingsData data = GameManager.Instance.gamePersist.LoadSettings();
                LoadSettings(data);
            }
        }

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
            SaveSettings();
            gameObject.SetActive(false);
            activeStatus = false;
        }
    }

    public void CloseWindowButton()
    {
        if (world != null)
            world.cityBuilderManager.PlayCloseAudio();
        else
            titleScreen.PlayCloseAudio();
        ToggleVisibility(false);
    }

    public void ChangeMain(float value)
    {
        audioMixer.SetFloat("MainVolume", value);
    }

    public void ChangeMusic(float value)
    {
        audioMixer.SetFloat("MusicVolume", value);
	}

    public void ChangeAmbience(float value)
    {
		audioMixer.SetFloat("AmbienceVolume", value);
    }

	public void ChangeSound(float value)
	{
		audioMixer.SetFloat("SoundEffectVolume", value);
	}

    public void SetGraphics(int index)
    {
        if (world != null)
            world.cityBuilderManager.PlaySelectAudio();
        else
            titleScreen.PlaySelectAudio();
		index = Mathf.Abs(index - 2);
        QualitySettings.SetQualityLevel(index);
    }

    public void SetResolution(int index)
    {
		if (world != null)
        {
            if (world.cityBuilderManager.audioSource != null)
                world.cityBuilderManager.PlaySelectAudio();
        }
        else
        {
			titleScreen.PlaySelectAudio();
        }

		Resolution resolution = resolutions[index];
        Screen.SetResolution(resolution.width, resolution.height, true);
    }

    public void SetTutorialToggle(bool v)
    {
        tutorialToggle.isOn = v;
        if (world != null)
            world.tutorial = v;

        tutorial = v;
    }

    public void SaveSettings()
    {
        SettingsData data = new();

        data.mainVolume = masterVolume.value;
		data.musicVolume = musicVolume.value;
		data.soundVolume = soundVolume.value;
        data.ambienceVolume = ambienceVolume.value;
        data.graphics = graphicsDropdown.value;
        data.resolution = resolutionDropdown.value;
        data.tutorial = tutorialToggle.isOn;

		GameManager.Instance.gamePersist.SaveSettings(data);
    }

    public void LoadSettings(SettingsData data)
    {
        masterVolume.value = data.mainVolume;
		audioMixer.SetFloat("MainVolume", data.mainVolume);

		musicVolume.value = data.musicVolume;
		audioMixer.SetFloat("MusicVolume", data.musicVolume);

		soundVolume.value = data.soundVolume;
		audioMixer.SetFloat("SoundEffectVolume", data.soundVolume);

		ambienceVolume.value = data.ambienceVolume;
		audioMixer.SetFloat("AmbienceVolume", data.ambienceVolume);

		graphicsDropdown.value = data.graphics;
        graphicsDropdown.RefreshShownValue();
		QualitySettings.SetQualityLevel(Mathf.Abs(data.graphics - 2));

		resolutionDropdown.value = data.resolution;
		resolutionDropdown.RefreshShownValue();
		Resolution resolution = resolutions[data.resolution];
		Screen.SetResolution(resolution.width, resolution.height, true);

        SetTutorialToggle(data.tutorial);
	}
}
