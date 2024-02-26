using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
	[SerializeField]
	private MapWorld world;
	
	[SerializeField]
	private List<AudioClip> audioClips;
	private Queue<AudioClip> musicList = new();

	private AudioSource audioSource;

	private int musicPause = 30; //pause between songs
	private int checkWait = 5;
	private WaitForSeconds ambienceCheck;

	private Coroutine musicCo, ambienceCo;

	private bool onLandDay, onLandNight, onWater;

	[SerializeField]
	public bool isMusic, isAmbience, mute, playing;

	private void Awake()
	{
		ambienceCheck = new(checkWait);
		audioSource = GetComponent<AudioSource>();
	}

	private void Start()
	{
		/*if (isMusic)
		{
			ShuffleMusic();
		}
		else */if (isAmbience)
		{
			if (!mute)
				AmbienceCheck();
		}
		/*else
		{
			if (!mute)
			{
				world.FindVisibleCity();
			}
		}*/

	}

	private void ShuffleMusic()
	{
		//randomly sorting music
		List<AudioClip> musicToAdd = new(audioClips);

		int length = musicToAdd.Count;

		for (int i = 0; i < length; i++)
		{
			AudioClip song = musicToAdd[Random.Range(0, musicToAdd.Count)];
			musicList.Enqueue(song);
			musicToAdd.Remove(song);
		}
	}

	private IEnumerator AmbiencePlay()
	{
		yield return ambienceCheck;

		AmbienceCheck();
	}

	//checking where camera is to determine which ambience to play
	public void AmbienceCheck()
	{
		if (world.cityBuilderManager.uiCityTabs.activeStatus)
			return;

		if (world.CameraLocCheck())
		{
			if (world.DayTimeCheck())
			{
				if (onLandDay)
				{
					//resetCount--;
					ambienceCo = StartCoroutine(AmbiencePlay());
					return;
				}

				onLandDay = true;
				onLandNight = false;
				onWater = false;
			}
			else
			{
				if (onLandNight)
				{
					//resetCount--;
					ambienceCo = StartCoroutine(AmbiencePlay());
					return;
				}

				onLandNight = true;
				onLandDay = false;
				onWater = false;
			}
		}
		else
		{
			if (onWater)
			{
				//resetCount--;
				ambienceCo = StartCoroutine(AmbiencePlay());
				return;
			}

			onWater = true;
			onLandDay = false;
			onLandNight = false;
		}

		audioSource.Stop();

		if (onLandDay)
			audioSource.clip = audioClips[0];
		else if (onLandNight)
			audioSource.clip = audioClips[1];
		else
			audioSource.clip = audioClips[2];

		//resetCount = Mathf.RoundToInt(audioSource.clip.length * 0.2f);

		audioSource.Play();

		ambienceCo = StartCoroutine(AmbiencePlay());
	}

	public void PauseAmbience()
	{
		audioSource.Pause();
	}

	public void RestartAmbience()
	{
		audioSource.Play();
	}

	private IEnumerator MusicPlay(bool specificSong, AudioClip specifiedSong = null)
	{
		playing = true;
		AudioClip song;

		if (specificSong)
			song = specifiedSong;
		else
			song = musicList.Dequeue();

		musicList.Enqueue(song);

		audioSource.clip = song;
		audioSource.Play();
		yield return new WaitForSeconds(song.length + musicPause);

		if (musicList.Count == 0)
			ShuffleMusic();

		if (!mute)
			musicCo = StartCoroutine(MusicPlay(false));
		else
			playing = false;
	}

	public void PlaySpecificSong(AudioClip song)
	{
		StopMusic();
		
		if (!mute)
		{
			musicCo = StartCoroutine(MusicPlay(true, song));
		}
	}

	public void StartMusic()
	{
		if (playing)
			return;

		StopMusic();
		
		if (!mute)
		{
			if (musicList.Count == 0)
				ShuffleMusic();

			musicCo = StartCoroutine(MusicPlay(false));
		}
	}

	public void StopMusic()
	{
		audioSource.Stop();
		playing = false;

		if (musicCo != null)
			StopCoroutine(musicCo);

		musicCo = null;
	}

	public void StartAmbience()
	{
		audioSource.Play();
	}

	public void StopAmbience()
	{
		audioSource.Pause();

		if (ambienceCo != null)
			StopCoroutine(ambienceCo);

		ambienceCo = null;
	}
}
