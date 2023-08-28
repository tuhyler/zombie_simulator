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
    private float regenerationRate = 2;
    private int outOfCombatWait = 3;
    private WaitForSeconds wait = new WaitForSeconds(1);

    float lerpSpeed;
    private Coroutine co;


	private void Update()
	{
        //lerpSpeed = 3 * Time.deltaTime;
        //bar.localScale = new Vector3(currentHealth, 1f);
	}

	private void LateUpdate()
	{
        //transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
        transform.LookAt(transform.position + Camera.main.transform.forward);
	}

    public void SetUnit(Unit unit)
    {
        this.unit = unit;
    }

	public void SetHealthLevel(int health, int max)
    {
        float perc = (float)health / max;
		bar.localScale = new Vector3(perc, 1f);
		barImage.color = Color.Lerp(Color.red, Color.green, perc);
		currentHealth = health;
        healthMax = max;

        if (co != null)
            StopCoroutine(co);
        co = StartCoroutine(Regenerate());
    }

    private IEnumerator Regenerate()
    {
        float startingHealth = currentHealth;
        int timeWaited = 0;

        while (timeWaited < outOfCombatWait)
        {
            yield return wait;
            timeWaited++;
        }

        unit.baseSpeed = 1;
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

        unit.HideBar();
    }
}
