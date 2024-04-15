using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UITradeResourceNum : MonoBehaviour
{
	[SerializeField]
	public TMP_InputField resourceNumField;

	[SerializeField]
	public RectTransform allContents, closeButton;

	private UITradeResourceTask resourceTask;

	[HideInInspector]
	public bool activeStatus;

	private void Awake()
	{
		gameObject.SetActive(false);
	}

	private void Start()
	{
		//for checking if number is positive and integer
		resourceNumField.onValidateInput += delegate (string input, int charIndex, char addedChar) { return PositiveIntCheck(addedChar); };
	}

	private void Update()
	{
		if (activeStatus)
		{
			if (Input.mouseScrollDelta.y != 0 || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
				ToggleVisibility(false);
		} 
	}

	public void ToggleVisibility(bool v, UITradeResourceTask resourceTask = null)
	{
		if (activeStatus == v)
			return;

		activeStatus = v;
		gameObject.SetActive(v);

		if (v)
		{
			transform.position = resourceTask.resourceCount.transform.position;
			this.resourceTask = resourceTask;
			resourceNumField.text = resourceTask.chosenResourceAmount.ToString();
			closeButton.pivot = new Vector2((allContents.localPosition.x /*+ allContents.sizeDelta.x * 0f*/) / closeButton.sizeDelta.x, 
				(allContents.localPosition.y + allContents.sizeDelta.y * 0.4f) / closeButton.sizeDelta.y);
			resourceNumField.Select();
		}
		else
		{
			this.resourceTask.SetNewResourceCount(int.Parse(resourceNumField.text));	
			this.resourceTask = null;
		}
	}

	private char PositiveIntCheck(char charToValidate) //ensuring numbers are positive
	{
		if (charToValidate != '1'
			&& charToValidate != '2'
			&& charToValidate != '3'
			&& charToValidate != '4'
			&& charToValidate != '5'
			&& charToValidate != '6'
			&& charToValidate != '7'
			&& charToValidate != '8'
			&& charToValidate != '9'
			&& charToValidate != '0')
		{
			charToValidate = '\0';
		}

		return charToValidate;
	}

	public void CloseResourceNum()
	{
		ToggleVisibility(false);
	}
}
