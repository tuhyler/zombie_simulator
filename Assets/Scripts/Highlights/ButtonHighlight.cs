using UnityEngine;
using UnityEngine.UI;

public class ButtonHighlight : MonoBehaviour
{
    [SerializeField]
    private Material buttonHighlight, bigButtonHighlight, circleButtonHighlight;

    [SerializeField]
    private ParticleSystem buttonFlash;

    [SerializeField]
    private ParticleSystemRenderer buttonFlashRenderer;

    public void SetMaterial(bool button, bool big)
    {
        if (button)
        {
            if (big)
            {
				buttonFlashRenderer.material = bigButtonHighlight;
				buttonFlash.transform.localScale = new Vector3(400, 400, 400);
            }
            else
            {
                buttonFlashRenderer.material = buttonHighlight;
                buttonFlash.transform.localScale = new Vector3(100, 100, 100);
			}
		}
        else
        {
			buttonFlashRenderer.material = circleButtonHighlight;
			buttonFlash.transform.localScale = new Vector3(130, 130, 130);
		}
	}

    public void PlayFlash(bool button, bool big)
    {
        gameObject.SetActive(true);
        SetMaterial(button, big);
        buttonFlash.Play();
    }

    public void StopFlash()
    {
        buttonFlash.Stop();
        gameObject.SetActive(false);
    }

}
