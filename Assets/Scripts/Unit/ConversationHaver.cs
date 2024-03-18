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

	public void SetSomethingToSay(string conversationTopic, Worker alternateSpeaker = null)
	{
		if (!conversationTopics.Contains(conversationTopic))
			conversationTopics.Add(conversationTopic);

		if (!unit.somethingToSay && !unit.sayingSomething)
			StartCoroutine(SetSomethingToSayCoroutine(alternateSpeaker));
	}

	//wait till everything's done before setting up something to say
	private IEnumerator SetSomethingToSayCoroutine(Worker alternateSpeaker)
	{
		yield return new WaitForEndOfFrame();

		unit.somethingToSay = true;

		if (alternateSpeaker != null)
		{
			alternateSpeaker.marker.gameObject.SetActive(false);
			alternateSpeaker.questionMark.SetActive(true);
		}
		else
		{
			unit.marker.gameObject.SetActive(false);
			unit.questionMark.SetActive(true);
		}

		if (unit.isSelected)
		{
			unit.world.unitMovement.QuickSelect(unit);
			SpeakingCheck();
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
		world.cameraController.followTransform = transform;
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
		unit.marker.gameObject.SetActive(false);
		unit.world.speechBubble.SetActive(true);
		unit.world.speechBubble.transform.SetParent(transform, false);
	}

	public void SaidSomething()
	{
		unit.sayingSomething = false;
		if (conversationTopics.Count > 0)
		{
			if (unit.isPlayer && unit.isSelected)
				StartCoroutine(WaitASecToSpeakAgain());
			else
				StartCoroutine(SetSomethingToSayCoroutine(null));
		}
	}

	private IEnumerator WaitASecToSpeakAgain()
	{
		unit.world.playerInput.paused = true;
		yield return new WaitForSeconds(1);

		SpeakingCheck();
	}

	public void RemoveConversationTopic(string conversationTopic)
	{
		if (conversationTopics.Contains(conversationTopic))
		{
			conversationTopics.Remove(conversationTopic);

			if (conversationTopics.Count == 0)
			{
				unit.somethingToSay = false;
				unit.questionMark.SetActive(false);
			}
		}
	}
}
