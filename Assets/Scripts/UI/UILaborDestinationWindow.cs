using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UILaborDestinationWindow : MonoBehaviour
{
	[SerializeField]
	private MapWorld world;
	
	[SerializeField]
	public TMP_Dropdown destinationDropdown;

	[SerializeField] //for tweening
	private RectTransform allContents;
	[HideInInspector]
	public bool activeStatus;
	List<string> destinationList = new();
	List<string> initialOption = new() { "Select..." };

	private void Awake()
	{
		gameObject.SetActive(false);
	}

	public void ToggleVisibility(bool v, Vector3Int cityLoc)
	{
		if (activeStatus == v)
			return;

		LeanTween.cancel(gameObject);

		if (v)
		{
			destinationList = world.GetConnectedCityNames(cityLoc, false, false);
			destinationDropdown.AddOptions(destinationList);
			gameObject.SetActive(true);
			activeStatus = true;
			allContents.localScale = Vector3.zero;
			LeanTween.scale(allContents, Vector3.one, 0.25f).setEase(LeanTweenType.easeOutSine);
		}
		else
		{
			activeStatus = false;
			destinationList.Clear();
			destinationDropdown.ClearOptions();
			destinationDropdown.AddOptions(initialOption);
			LeanTween.scale(allContents, Vector3.zero, 0.25f).setOnComplete(SetActiveStatusFalse);
		}
	}

	public void SetActiveStatusFalse()
	{
		gameObject.SetActive(false);
	}

	public void ConfirmDestination()
	{
		if (destinationDropdown.value == 0)
		{
			ToggleVisibility(false, Vector3Int.zero);
		}
		else
		{
			string chosenDestination = destinationList[destinationDropdown.value - 1];
			world.cityBuilderManager.TransferLaborPrep(chosenDestination);
			ToggleVisibility(false, Vector3Int.zero);
		}
	}

	public void CloseWindowButton()
	{
		ToggleVisibility(false, Vector3Int.zero);
	}
}
