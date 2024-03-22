using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIResourceGivingSubPanel : MonoBehaviour
{
    [SerializeField]
    private TMP_Text title, textBody;

	[SerializeField] //for tweening
	private RectTransform allContents;
	private bool activeStatus;
	private Vector3 originalLoc;

	private void Awake()
	{
		gameObject.SetActive(false);
		originalLoc = allContents.anchoredPosition3D;
	}

	public void ToggleVisibility(bool v, string npcName = "", string hintText = "")
	{
		if (activeStatus == v)
			return;

		LeanTween.cancel(gameObject);

		if (v)
		{
			gameObject.SetActive(v);
			activeStatus = true;
			title.text = "Request Details for " + npcName;
			textBody.text = hintText;

			allContents.anchoredPosition3D = originalLoc;
			LeanTween.moveX(allContents, allContents.anchoredPosition3D.x + 500, 0.4f).setEase(LeanTweenType.easeOutSine);
		}
		else
		{
			activeStatus = false;
			LeanTween.moveX(allContents, allContents.anchoredPosition3D.x - 500f, 0.4f)
				.setEase(LeanTweenType.easeOutSine)
				.setOnComplete(SetVisibilityFalse);
		}
	}

	private void SetVisibilityFalse()
	{
		gameObject.SetActive(false);
	}
}
