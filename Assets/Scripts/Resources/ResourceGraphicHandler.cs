using UnityEngine;

public class ResourceGraphicHandler : MonoBehaviour
{
    public GameObject resourceAll, resourceMedium, resourceMany;
    public int mediumThreshold, smallThreshold;
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
		resourceAll.SetActive(false);
		audioSource.Play();
    }

    public void SetRocksAmount(int amount)
    {
        resourceAll.SetActive(true);

        if (amount < smallThreshold)
        {
            resourceMedium.SetActive(false);
			resourceMany.SetActive(false);
		}
        else if (amount < mediumThreshold)
        {
			resourceMany.SetActive(false);
		}
        else if (amount <= 0)
        {
            resourceAll.SetActive(false);
        }
	}
}
