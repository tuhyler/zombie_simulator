using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIAttackWarning : MonoBehaviour
{
	[SerializeField]
	private MapWorld world;
	
	[SerializeField]
	private ParticleSystem flash;
	[HideInInspector]
	public List<Unit> attackUnits = new();
	private int notificationWait = 10;
	private WaitForSeconds wait = new WaitForSeconds(1);
	private Coroutine co;

	[SerializeField] //for tweening
	public RectTransform allContents;
	[HideInInspector]
	public bool activeStatus, flashing;

	private void Awake()
	{
		gameObject.SetActive(false);
	}

	public void ToggleVisibility(bool v)
	{
		if (activeStatus == v)
			return;

		LeanTween.cancel(gameObject);

		if (v)
		{
			co = StartCoroutine(NotificationWaiting());
			gameObject.SetActive(true);
			activeStatus = true;
			allContents.localScale = Vector3.zero;
			LeanTween.scale(allContents, Vector3.one, 0.25f).setDelay(0.125f).setEase(LeanTweenType.easeOutSine).setOnComplete(StartFlash);
		}
		else
		{
			attackUnits.Clear();
			activeStatus = false;
			flash.Stop();
			LeanTween.scale(allContents, Vector3.zero, 0.25f).setDelay(0.125f).setOnComplete(SetActiveStatusFalse);
		}
	}

	public void StartFlash()
	{
		flash.Play();
	}

	public void SetActiveStatusFalse()
	{
		gameObject.SetActive(false);
	}

	public void AttackNotification(Unit attacker)
	{
		if (!world.mapHandler.activeStatus && Camera.main.WorldToViewportPoint(attacker.transform.position).z >= 0)
			return;

		attackUnits.Add(attacker);
		world.cityBuilderManager.PlayAlertAudio();
		
		if (!world.mapHandler.activeStatus)

		if (co != null)
			StopCoroutine(co);

		if (!world.mapHandler.activeStatus)
			ToggleVisibility(true);
	}

	private IEnumerator NotificationWaiting()
	{
		int timeWaited = 0;

		while (timeWaited < notificationWait)
		{
			yield return wait;
			timeWaited++;
		}

		co = null;
		ToggleVisibility(false);
	}

	public void GoToAttack()
	{
		if (attackUnits.Count == 0)
			return;
		
		Unit attacker = attackUnits[0];
		attackUnits.Remove(attacker);

		world.cameraController.CenterCameraInstantly(attacker.transform.position);

		CloseWarningCheck();
	}

	public void AttackWarningCheck(Unit unit)
	{
		if (attackUnits.Contains(unit))
		{
			attackUnits.Remove(unit);
			CloseWarningCheck();
		}
	}

	public void CloseWarningCheck()
	{
		if (attackUnits.Count == 0)
		{
			ToggleVisibility(false);

			if (co != null)
				StopCoroutine(co);

			co = null;
		}
	}

	public void LoadAttackLocs(List<Vector3Int> attackLocs)
	{
		if (attackLocs.Count > 0)
		{
			for (int i = 0; i < attackLocs.Count; i++)
			{
				if (world.IsEnemyCampHere(attackLocs[i]))
				{
					EnemyCamp camp = world.GetEnemyCamp(attackLocs[i]);
					if (camp.UnitsInCamp.Count > 0)
						attackUnits.Add(camp.UnitsInCamp[0]);
				}
				else if (world.IsEnemyAmbushHere(attackLocs[i]))
				{
					EnemyAmbush ambush = world.GetEnemyAmbush(attackLocs[i]);
					if (ambush.attackedUnits.Count > 0)
						attackUnits.Add(ambush.attackedUnits[0]);
				}
			}

			ToggleVisibility(true);
		}
	}
}
