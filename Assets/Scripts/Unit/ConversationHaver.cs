using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConversationHaver : MonoBehaviour
{
    [HideInInspector]
    Unit unit;
	[HideInInspector]
	public List<string> conversationTopics = new();

	private void Awake()
	{
		unit = GetComponent<Unit>();
	}

	public void SetSomethingToSay(string conversationTopic, Unit alternateSpeaker = null)
	{
		if (!conversationTopics.Contains(conversationTopic))
			conversationTopics.Add(conversationTopic);

		if (!unit.somethingToSay && !unit.sayingSomething)
			StartCoroutine(SetSomethingToSayCoroutine(alternateSpeaker));
	}

	//wait till everything's done before setting up something to say
	private IEnumerator SetSomethingToSayCoroutine(Unit alternateSpeaker)
	{
		yield return new WaitForEndOfFrame();

		unit.somethingToSay = true;

		if (alternateSpeaker != null)
		{
			if (!alternateSpeaker.sayingSomething)
				alternateSpeaker.questionMark.SetActive(true);
		}
		else
		{
			if (!unit.sayingSomething)
				unit.questionMark.SetActive(true);
		}

		if (unit.isPlayer && unit.isSelected && !unit.worker.isBusy && !unit.worker.inTransport)
		{
			unit.world.unitMovement.QuickSelect(unit);
			SpeakingCheck();
			unit.world.ToggleCharacterConversationCam(true);
		}
	}

	public void SpeakingCheck()
	{
		MapWorld world = unit.world;
		string newConversation = conversationTopics[0];
		conversationTopics.Remove(newConversation);
		if (conversationTopics.Count == 0)
			unit.somethingToSay = false;
		unit.sayingSomething = true;
		world.cameraController.CenterCameraNoFollow(unit.transform.position);
		//world.cameraController.CenterCameraNoFollow(transform.position);
		world.cameraController.someoneSpeaking = true;
		if (unit.isPlayer)
		{
			for (int i = 0; i < world.characterUnits.Count; i++)
				world.characterUnits[i].questionMark.SetActive(false);
		}
		else
		{
			unit.questionMark.SetActive(false);
		}

		world.playerInput.paused = true;
		world.uiSpeechWindow.SetConversation(newConversation);
		world.uiSpeechWindow.ToggleVisibility(true);
	}

	public void SetSpeechBubble()
	{
		//unit.marker.gameObject.SetActive(false);
		unit.world.speechBubble.SetActive(true);
		unit.world.speechBubble.transform.SetParent(transform, false);
	}

	public bool SaidSomething()
	{
		unit.sayingSomething = false;
		if (conversationTopics.Count > 0)
		{
			if (unit.isPlayer && unit.isSelected && !unit.worker.isBusy)
				StartCoroutine(WaitASecToSpeakAgain());
			else
				StartCoroutine(SetSomethingToSayCoroutine(null));

			return true;
		}

		return false;
	}

	private IEnumerator WaitASecToSpeakAgain()
	{
		unit.world.playerInput.paused = true;
		yield return new WaitForEndOfFrame();

		unit.world.unitMovement.QuickSelect(unit);
		SpeakingCheck();
		unit.world.ToggleCharacterConversationCam(true);
	}

	//public void RemoveConversationTopic(string conversationTopic)
	//{
	//	if (conversationTopics.Contains(conversationTopic))
	//	{
	//		conversationTopics.Remove(conversationTopic);

	//		if (conversationTopics.Count == 0)
	//		{
	//			unit.somethingToSay = false;
	//			unit.questionMark.SetActive(false);
	//		}
	//	}
	//}
}