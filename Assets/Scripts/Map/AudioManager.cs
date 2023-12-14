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

	private int musicPause = 10; //pause between songs
	private int checkWait = 5;
	private WaitForSeconds ambienceCheck;

	private float checkMultiple;
	private int resetCount;

	private Coroutine musicCo, ambienceCo;

	private bool onLandDay, onLandNight, onWater;

	[SerializeField]
	private bool isMusic, isAmbience, mute;

	private void Awake()
	{
		ambienceCheck = new(checkWait);
		checkMultiple = 1 / (float)checkWait;
		audioSource = GetComponent<AudioSource>();
	}

	private void Start()
	{
		if (isMusic)
		{
			//randomly sorting music
			List<AudioClip> musicToAdd = new(audioClips);

			//if (true) //for havign a specific song play first
			//{
			//	AudioClip specificSong = musicToAdd[0];
			//	musicList.Enqueue(specificSong);
			//	musicToAdd.Remove(specificSong);
			//}

			int length = musicToAdd.Count;
		
			for (int i = 0; i < length; i++)
			{
				AudioClip song = musicToAdd[Random.Range(0, musicToAdd.Count)];
				musicList.Enqueue(song);
				musicToAdd.Remove(song);
			}
	
			if (!mute)
				musicCo = StartCoroutine(MusicPlay());
		}
		else if (isAmbience)
		{
			if (!mute)
				AmbienceCheck();
		}
		else
		{
			//if (!mute)
			//{
			//	world.FindVisibleCity();
			//}
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

	private IEnumerator MusicPlay()
	{
		AudioClip song = musicList.Dequeue();
		musicList.Enqueue(song);

		audioSource.clip = song;
		audioSource.Play();
		yield return new WaitForSeconds(song.length + musicPause);

		musicCo = StartCoroutine(MusicPlay());
	}

	public void StartMusic()
	{
		audioSource.Play();
	}

	public void StopMusic()
	{
		audioSource.Pause();

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
