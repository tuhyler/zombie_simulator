using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISpeechWindow : MonoBehaviour
{
    [SerializeField]
    private TMP_Text speakerName, speechText;

    [SerializeField]
    private Image speakerImage;

	public float wordPause = 0.1f;
	public float sentencePause = 0.4f;
	private WaitForSeconds wordWait, sentenceWait;

	[SerializeField] //for tweening
	private RectTransform allContents;
	private bool activeStatus;
	private Vector3 originalLoc;

	private Coroutine co;

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
			gameObject.SetActive(v);

			activeStatus = true;

			allContents.anchoredPosition3D = originalLoc + new Vector3(0, -600f, 0);

			LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 600f, 0.4f).setEaseOutBack();
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

	public void SetText(string text)
	{
		string[] textArray = text.Split(' ');
		co = StartCoroutine(ShowSpeech(textArray));
	}

	private IEnumerator ShowSpeech(string[] textArray)
	{
		speechText.text = textArray[0];
		for (int i = 1; i < textArray.Length; i++)
		{
			if (speechText.text.EndsWith('.'))
				yield return sentenceWait;
			else
				yield return wordWait;

			speechText.text += " " + textArray[i];
		}
	}

	public void CancelText()
	{
		gameObject.SetActive(false);

		if (co != null)
			StopCoroutine(co);
	}
}
