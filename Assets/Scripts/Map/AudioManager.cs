using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
	[SerializeField]
	private MapWorld world;
	
	[SerializeField]
	private List<AudioClip> music;
	private Queue<AudioClip> musicList = new();

	private AudioSource audioSource;

	[SerializeField]
	private AudioSource[] ambience;

	private int musicPause = 10; //pause between songs
	private WaitForSeconds ambiencePause = new(3);
	private WaitForSeconds ambienceCheck = new(3);

	private Coroutine musicCo, ambienceCo;

	bool onLandDay, onLandNight, onWater;

	private void Awake()
	{
		audioSource = GetComponent<AudioSource>();
	}

	private void Start()
	{
		//randomly sorting music
		List<AudioClip> musicToAdd = new(music);

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

		//StartCoroutine(AmbiencePlay());
		//musicCo = StartCoroutine(MusicPlay());
	}

	private IEnumerator AmbiencePlay()
	{
		yield return ambiencePause;

		if (world.CameraLocCheck())
		{
			if (world.DayTimeCheck())
			{
				if (onLandDay)
					yield break;

				onLandDay = true;
				onLandNight = false;
				onWater = false;
			}
			else
			{
				if (onLandNight)
					yield break;

				onLandNight = true;
				onLandDay = false;
				onWater = false;
			}
		}
		else
		{
			if (onWater)
				yield break;

			onWater = true;
			onLandDay = false;
			onLandNight = false;
		}

		ambience[Random.Range(0, ambience.Length)].Play();
	}

	private IEnumerator AmbienceCheck()
	{
		yield return ambienceCheck;
	}
	
	private IEnumerator MusicPlay()
	{
		AudioClip song = musicList.Dequeue();
		float length = song.length + musicPause;
		musicList.Enqueue(song);

		audioSource.clip = song;
		audioSource.Play();
		yield return new WaitForSeconds(length);

		musicCo = StartCoroutine(MusicPlay());
	}

	public void StopMusic()
	{
		audioSource.Stop();

		if (musicCo != null)
			StopCoroutine(musicCo);

		musicCo = null;
	}
}
