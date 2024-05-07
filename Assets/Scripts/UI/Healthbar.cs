using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Healthbar : MonoBehaviour
{
    [SerializeField]
    private Transform bar;
    [SerializeField]
    private SpriteRenderer barImage;

    Unit unit;
    private int currentHealth;
    private int healthMax;
    private float regenerationRate;
    private int outOfCombatWait = 3;
    private WaitForSeconds wait = new WaitForSeconds(1);

    float lerpSpeed;
    
    private Coroutine co;

	private void LateUpdate()
	{
        //transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
        transform.LookAt(transform.position + Camera.main.transform.forward);
	}

    public void SetUnit(Unit unit)
    {
        this.unit = unit;
        healthMax = unit.buildDataSO.health;
		regenerationRate = .01f * healthMax * unit.buildDataSO.regenerationRate;
    }

	public void SetHealthLevel(int health)
    {
        float perc = (float)health / healthMax;
		bar.localScale = new Vector3(perc, 1f);
		barImage.color = Color.Lerp(Color.red, Color.green, perc);
		currentHealth = health;

        StartCoroutine(SpeedUp());
        //if (co != null)
        //    StopCoroutine(co);
        //co = StartCoroutine(Regenerate());
    }

    public void RegenerateHealth()
    {
        gameObject.SetActive(true);
        co = StartCoroutine(Regenerate());
	}

    private IEnumerator SpeedUp()
    {
		int timeWaited = 0;

		while (timeWaited < outOfCombatWait)
		{
			yield return wait;
			timeWaited++;
		}

		unit.baseSpeed = 1;
	}

	private IEnumerator Regenerate()
    {
        //float startingHealth = currentHealth;

        //float currentLerpTime = 0;
        float healthFloat = currentHealth;
        float perc = currentHealth / healthMax; 

        while (perc < 1)
        {
			healthFloat += Time.deltaTime * regenerationRate;
            perc = healthFloat / healthMax;
            lerpSpeed = 3 * Time.deltaTime;

            unit.UpdateHealth(perc);
            bar.localScale = new Vector3(Mathf.Lerp(bar.localScale.x, perc, lerpSpeed),1);
            barImage.color = Color.Lerp(Color.red, Color.green, perc); 
            yield return null;
        }

        co = null;
        gameObject.SetActive(false);
    }

    public void CancelRegeneration()
    {
        if (co != null)
            StopCoroutine(co);

        co = null;
        if (unit.currentHealth == healthMax)
            gameObject.SetActive(false);
    }

    public void LoadHealthLevel(int health)
    {
		float perc = (float)health / healthMax;
		bar.localScale = new Vector3(perc, 1f);
		barImage.color = Color.Lerp(Color.red, Color.green, perc);
		currentHealth = health;
	}
}
