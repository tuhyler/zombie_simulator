using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITooltipWorkEthic : MonoBehaviour
{
	public TMP_Text titleText, improvementText, wonderText;
	public RectTransform allContents, line;

	private void Awake()
	{
		gameObject.SetActive(false);
		Color fade = titleText.color;
		fade.a = 0;
		titleText.color = fade;
		improvementText.color = fade;
		wonderText.color = fade;
	}

	public void SetInfo(string improvementMessage, string wonderMessage)
	{
		Vector3 p = Input.mousePosition;
		float x = 0.5f;
		float y = 0f;

		p.z = 935f;
		if (p.y + allContents.rect.height > Screen.height)
			y = 1f;

		if (p.x + allContents.rect.width * 0.5f > Screen.width)
			x = 1f;
		else if (p.x - allContents.rect.width * 0.5 < 0)
			x = 0f;

		allContents.pivot = new Vector2(x, y);

		Vector3 pos = Camera.main.ScreenToWorldPoint(p);
		allContents.transform.position = pos;

		//layoutElement.enabled = (text.Length > characterWrapLimit) ? true : false;
		improvementText.text = improvementMessage;
		wonderText.text = wonderMessage;
	}

	public string PrepareMessage(string beginningText, TMP_Text textToUse, Color fade, float num, bool show)
	{
		string messageToShow;
		
		if (show)
		{
			messageToShow = beginningText + "<color=green>+" + (num * 100).ToString() + "%</color>";
			textToUse.gameObject.SetActive(true);
			LeanTween.value(textToUse.gameObject, fade.a, 1, 0.2f).setOnUpdate((value) => { fade.a = value; textToUse.color = fade; });
		}
		else
		{
			textToUse.gameObject.SetActive(false);
			messageToShow = "";
		}

		return messageToShow;
	}
}
