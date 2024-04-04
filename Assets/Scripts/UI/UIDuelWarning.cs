using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIDuelWarning : MonoBehaviour
{
	[SerializeField]
	private UISpeechWindow uiSpeechWindow;
	
	[SerializeField]
	private TMP_Text textBody;
	private MilitaryLeader leader;

	[SerializeField] //for tweening
	private RectTransform allContents;
	[HideInInspector]
	public bool activeStatus;

	private void Awake()
	{
		gameObject.SetActive(false);
	}

	public void ToggleVisibility(bool v, MilitaryLeader leader = null)
	{
		if (activeStatus == v)
			return;

		LeanTween.cancel(gameObject);

		if (v)
		{
			this.leader = leader;
			gameObject.SetActive(v);
			activeStatus = true;

			textBody.text = "Are you ready for Azai to duel " + leader.leaderName + "?";

			allContents.localScale = Vector3.zero;
			LeanTween.scale(allContents, Vector3.one, 0.25f).setEase(LeanTweenType.easeOutSine);
		}
		else
		{
			activeStatus = false;
			LeanTween.scale(allContents, Vector3.zero, 0.25f).setOnComplete(SetActiveStatusFalse);
			this.leader = leader;
		}
	}

	public void SetActiveStatusFalse()
	{
		gameObject.SetActive(false);
	}

	public void ConfirmDuel()
	{
		leader.DuelSetup();
		leader.world.unitMovement.ClearSelection();
		leader.world.unitMovement.SelectUnitPrep(leader.world.azai);
		leader.world.cameraController.someoneSpeaking = false;
		leader.world.azai.CenterCamera();
		ToggleVisibility(false);
		uiSpeechWindow.FinishText(true);
	}

	public void DenyDuel()
	{
		
		uiSpeechWindow.ReturnMainPlayer();
		ToggleVisibility(false);
		uiSpeechWindow.FinishText(false);
	}
}
