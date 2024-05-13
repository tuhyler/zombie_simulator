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
	public List<Vector3> attackLocs = new();
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
			gameObject.SetActive(true);
			co = StartCoroutine(NotificationWaiting());
			activeStatus = true;
			allContents.localScale = Vector3.zero;
			LeanTween.scale(allContents, Vector3.one, 0.25f).setDelay(0.125f).setEase(LeanTweenType.easeOutSine).setOnComplete(StartFlash);
		}
		else
		{
			attackLocs.Clear();
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

	public void AttackNotification(Vector3 loc)
	{
		if (!world.mapHandler.activeStatus && Camera.main.WorldToViewportPoint(loc).z >= 0)
			return;

		attackLocs.Add(loc);
		world.cityBuilderManager.PlaySelectAudio(world.cityBuilderManager.alertClip);
		
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
		if (attackLocs.Count == 0)
			return;
		
		Vector3 attackLoc = attackLocs[0];
		attackLocs.Remove(attackLoc);

		world.cameraController.CenterCameraInstantly(attackLoc);

		CloseWarningCheck();
	}

	public void AttackWarningCheck(Vector3 loc)
	{
		if (attackLocs.Contains(loc))
		{
			attackLocs.Remove(loc);
			CloseWarningCheck();
		}
	}

	private void CloseWarningCheck()
	{
		if (attackLocs.Count == 0)
		{
			ToggleVisibility(false);

			if (co != null)
				StopCoroutine(co);

			co = null;
		}
	}

	public void LoadAttackLocs(List<Vector3> attackLocs)
	{
		if (attackLocs.Count > 0)
		{
			this.attackLocs = new(attackLocs);
			ToggleVisibility(true);
		}
	}
}
