using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UISpeechWindow : MonoBehaviour, IPointerDownHandler
{
	[SerializeField]
	private MapWorld world;
	
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

	[SerializeField] //for tweening
	private RectTransform allContents;
	[HideInInspector]
	private bool activeStatus, showingText, hideUI;
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
			
			gameObject.SetActive(v);

			activeStatus = true;

			allContents.anchoredPosition3D = originalLoc + new Vector3(0, -600f, 0);

			LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 600f, 0.4f).setEaseOutBack().setOnComplete(StartSpeech);
			LeanTween.alpha(allContents, 1f, 0.2f).setFrom(0f).setEaseLinear();
		}
		else
		{
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
		LeanTween.cancelAll();
		
		if (co != null)
		{
			StopCoroutine(co);
			co = null;
		}
		
		for (int i = 0; i < textBlock.Length; i++)
		{
			textBlock[i].anchoredPosition3D = originalLocs[i];
		}
	}

	public void SetConversation(string conversationTopic, bool hideUI = false)
	{
		this.conversationTopic = conversationTopic;
		this.hideUI = hideUI;
		conversationItems = Conversations.Instance.conversationDict[conversationTopic];
	}

	private void StartSpeech()
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

			if (conversationItems[conversationPlace].speakerDirection != "Camera")
				unit.Rotate(speakerDict[conversationItems[conversationPlace].speakerDirection].transform.position);
		}

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
			CancelText();
		}
		else
		{
			StartSpeech();
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

	public void CancelText()
	{
		ToggleVisibility(false);

		if (co != null)
		{
			StopCoroutine(co);
			co = null;
		}
		showingText = false;

		world.unitMovement.uiWorkerTask.ToggleVisibility(true, world);
		world.unitMovement.PrepareMovement();
		world.playerInput.paused = false;
		world.cameraController.someoneSpeaking = false;
		world.speechBubble.SetActive(false);
		if (hideUI)
			world.ResetMainUI();
		conversationItems.Clear();
		conversationPlace = 0;

		for (int i = 0; i < unitsSpeaking.Count; i++)
			unitsSpeaking[i].SaidSomething();

		unitsSpeaking.Clear();

		world.CheckPostConvoStep(conversationTopic);

		conversationTopic = "";
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (showingText)
			SkipToEnd();
		else
			GoToNextText();
	}
}
