using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UISpeechWindow : MonoBehaviour, IPointerDownHandler
{
	[SerializeField]
	private MapWorld world;

	[SerializeField]
	private UIDuelWarning uiDuelWarning;
	
	[SerializeField]
    private TMP_Text speakerName, speechText;

    [SerializeField]
    private Image speakerImage;

	[SerializeField]
	private RectTransform[] textBlock;
	private Vector3[] originalLocs;

	public int linePause = 5;
	//public float wordPause = 0.1f;
	//public float sentencePause = 0.4f;
	private WaitForSeconds lineWait;

	private List<ConversationItem> conversationItems;
	private int conversationPlace;
	private Dictionary<string, Unit> speakerDict = new();
	private List<Unit> unitsSpeaking = new();
	private Unit speakingNPC;

	[SerializeField] //for tweening
	private RectTransform allContents;
	[HideInInspector]
	private bool activeStatus, showingText;
	private Vector3 originalLoc;
	private string conversationTopic;

	private Coroutine co;

	private void Awake()
	{
		lineWait = new WaitForSeconds(1);
		originalLoc = allContents.anchoredPosition3D;
		originalLocs = new Vector3[textBlock.Length];

		for (int i = 0; i < textBlock.Length; i++)
		{
			originalLocs[i] = textBlock[i].anchoredPosition3D;
		}

		gameObject.SetActive(false);
	}

	public void AddToSpeakingDict(string name, Unit unit)
	{
		speakerDict[name] = unit;
	}

	public void ToggleVisibility(bool v)
	{
		if (activeStatus == v)
			return;

		LeanTween.cancel(gameObject);

		if (v)
		{
			ResetTextBlocks();
			world.ToggleMainUI(false);

			gameObject.SetActive(v);

			activeStatus = true;

			allContents.anchoredPosition3D = originalLoc + new Vector3(0, -600f, 0);

			PrepNextSpeech();
			LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 600f, 0.4f).setEaseOutBack().setOnComplete(StartNextSpeech);
			//LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();
		}
		else
		{
			world.ToggleMainUI(true);
			activeStatus = false;
			LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -600f, 0.2f).setOnComplete(SetActiveStatusFalse);
		}
	}

	private void SetActiveStatusFalse()
	{
		gameObject.SetActive(false);
	}

	private void ResetTextBlocks()
	{
		//LeanTween.cancelAll();
		
		if (co != null)
		{
			StopCoroutine(co);
			co = null;
		}
		
		for (int i = 0; i < textBlock.Length; i++)
		{
			LeanTween.cancel(textBlock[i]);
			textBlock[i].anchoredPosition3D = originalLocs[i];
		}
	}

	public void SetConversation(string conversationTopic)
	{
		this.conversationTopic = conversationTopic;
		conversationItems = Conversations.Instance.conversationDict[conversationTopic];
	}

	public void SetSpeakingNPC(Unit speakingNPC)
	{
		this.speakingNPC = speakingNPC;

		if (speakingNPC.military && speakingNPC.military.leader)
			world.ToggleBadGuyTalk(true, speakingNPC.currentLocation);
	}

	private void PrepNextSpeech()
	{
		speakerImage.sprite = conversationItems[conversationPlace].speakerImage;
		string name = conversationItems[conversationPlace].speakerName;
		speakerName.text = name;
		speechText.text = conversationItems[conversationPlace].speakerText;
		showingText = true;
		if (speakerDict.ContainsKey(name))
		{
			Unit unit = speakerDict[name];
			unit.SetSpeechBubble();

			if (!unitsSpeaking.Contains(unit))
				unitsSpeaking.Add(unit);

			if (conversationItems[conversationPlace].speakerDirection == "Camera")
			{
				Vector3 loc = Camera.main.transform.position;
				loc.y = 0;
				unit.Rotate(loc);
			}
			else
			{
				unit.Rotate(speakerDict[conversationItems[conversationPlace].speakerDirection].transform.position);
				string listenerName = conversationItems[conversationPlace].speakerDirection;
				Unit listener = speakerDict[listenerName];

				if (!unitsSpeaking.Contains(listener))
					unitsSpeaking.Add(listener);
			}
		}

		if (conversationItems[conversationPlace].action)
			world.ConversationActionCheck(conversationTopic, conversationPlace);
	}

	private void StartNextSpeech()
	{
		co = StartCoroutine(ShowSpeech());
	}

	private IEnumerator ShowSpeech()
	{
		int i = 0;
		int length = speechText.text.Length / 15;

		while (showingText && i < textBlock.Length)
		{
			LeanTween.moveX(textBlock[i], originalLocs[i].x + 1200, linePause);

			int totalWait = linePause * i;
			for (int j = 0; j < linePause; j++)
			{
				yield return lineWait;

				totalWait++;

				if (totalWait > length)
					showingText = false;
			}

			i++;
		}

		showingText = false;
	}

	private void GoToNextText()
	{
		ResetTextBlocks();
		conversationPlace++;
		
		if (conversationPlace >= conversationItems.Count)
		{
			if (!CheckNPC())
				FinishText(false);
		}
		else
		{
			PrepNextSpeech();
			StartNextSpeech();
		}
	}

	private void SkipToEnd()
	{
		if (co != null)
		{
			StopCoroutine(co);
			co = null;
		}
		showingText = false;
		
		for (int i = 0; i < textBlock.Length; i++)
		{
			LeanTween.cancel(textBlock[i]);
			Vector3 finishedLoc = originalLocs[i];
			finishedLoc.x += 1200;
			textBlock[i].anchoredPosition3D = finishedLoc;
		}
	}

	public void FinishText(bool dueling)
	{
		ToggleVisibility(false);

		if (co != null)
		{
			StopCoroutine(co);
			co = null;
		}
		showingText = false;

		if (!dueling)
		{
			if (world.mainPlayer.inTransport)
			{
				world.unitMovement.ClearSelection();
			}
			else
			{
				world.unitMovement.SelectWorker();
				world.unitMovement.uiWorkerTask.ToggleVisibility(true, world);
				world.unitMovement.PrepareMovement();
				//if (!world.mainPlayer.inEnemyLines)
				//	world.unitMovement.uiMoveUnit.ToggleVisibility(true);
			}
		}

		world.playerInput.paused = false;
		world.cameraController.someoneSpeaking = false;
		world.speechBubble.transform.SetParent(transform, false);
		world.speechBubble.SetActive(false);
		conversationItems.Clear();

		world.ConversationActionCheck(conversationTopic, conversationPlace);
		conversationPlace = 0;

		for (int i = 0; i < unitsSpeaking.Count; i++)
			unitsSpeaking[i].SaidSomething();

		unitsSpeaking.Clear();
		speakingNPC = null;

		conversationTopic = "";
	}

	private bool CheckNPC()
	{
		//checking if action immediately after conversation needs to take place
		if (speakingNPC)
		{
			if (speakingNPC.military && speakingNPC.military.leader) //for enemy leaders
			{
				if (conversationTopic.Contains("intro"))
				{
					ReturnMainPlayer();
					speakingNPC.military.leader.BeginChallengeWait();
				}
				else if (conversationTopic.Contains("challenge"))
				{
					uiDuelWarning.ToggleVisibility(true, speakingNPC.military.leader);
					return true;
				}
			}
			else //for trade reps
			{
				if (conversationTopic.Contains("intro"))
				{
					speakingNPC.tradeRep.BeginNextQuestWait(false);
				}
				else if (conversationTopic.Contains("_quest"))
				{
					if (conversationTopic.Contains("_complete"))
					{
						speakingNPC.tradeRep.BeginNextQuestWait(true);
					}
					else
					{
						if (speakingNPC.tradeRep.currentQuest == 0)
							speakingNPC.tradeRep.CreateConversationTaskItem();

						speakingNPC.tradeRep.onQuest = true;
					}
				}

				speakingNPC.Rotate(speakingNPC.tradeRep.center.mainLoc);
			}
		}
			
		return false;
	}

	public void ReturnMainPlayer()
	{
		world.mainPlayer.ReturnToFriendlyTile();
		world.ToggleBadGuyTalk(false, speakingNPC.currentLocation);
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left)
		{
			if (showingText)
				SkipToEnd();
			else
				GoToNextText();
		}
	}
}
