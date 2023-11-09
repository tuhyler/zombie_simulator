using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UINewGameMenu : MonoBehaviour
{
	[HideInInspector]
	public bool activeStatus;

	private void Awake()
	{
		gameObject.SetActive(false);
	}

	public void ToggleVisibility(bool v)
	{
		if (activeStatus == v)
			return;

		if (v)
		{
			gameObject.SetActive(true);
			activeStatus = true;
		}
		else
		{
			gameObject.SetActive(false);
			activeStatus = false;
		}
	}
}
