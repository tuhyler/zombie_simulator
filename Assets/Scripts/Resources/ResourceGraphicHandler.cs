using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceGraphicHandler : MonoBehaviour
{
    public GameObject resourceLargeFlat, resourceMediumFlat, resourceSmallFlat, resourceLargeHill, resourceMediumHill, resourceSmallHill;
    public bool isHill;
    public int largeThreshold, mediumThreshold, smallThreshold;

    private AudioSource audioSource;
	private void Awake()
	{
        audioSource = GetComponent<AudioSource>();
	}

    public void PlaySound()
    {
        audioSource.Play();
    }

    public void PlaySoundHill()
    {
        TurnOffGraphics();
        audioSource.Play();
    }

	public void TurnOffGraphics()
    {
        resourceLargeFlat.SetActive(false);
        resourceMediumFlat.SetActive(false);
        resourceSmallFlat.SetActive(false);
        resourceLargeHill.SetActive(false);
        resourceMediumHill.SetActive(false);
        resourceSmallHill.SetActive(false);
    }
}
