using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIWarning : MonoBehaviour
{
	[SerializeField]
	private TMP_Text warningText, affirmationText, negateText;
	
	private void Awake()
	{
		gameObject.SetActive(false);
	}

	public void ToggleVisibilty(bool v)
	{
		gameObject.SetActive(v);
	}

	public void SetWarningMessages(string warning, string affirm, string negate)
	{
		warningText.text = warning;
		affirmationText.text = affirm;
		negateText.text = negate;
	}

	public void CloseWarning()
	{
		ToggleVisibilty(false);
	}
}
