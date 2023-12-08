using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class UIMainMenu : MonoBehaviour
{
	[SerializeField]
	private MapWorld world;

	[SerializeField]
	public UISaveGame uiSaveGame;

	[SerializeField]
	public UISettings uiSettings;

	[SerializeField]
	public HandlePlayerInput playerInput;

	[SerializeField]
	private UIMinimapHandler minimapHandler;

	[SerializeField]
	private Volume globalVolume;
	private DepthOfField dof;

	[SerializeField]
	private RectTransform allContents;
	[HideInInspector]
	public bool activeStatus;
	private Vector3 originalLoc;
	private bool opening; //if closing the main menu too fast after opening (time freezes)

	private void Awake()
	{
		originalLoc = allContents.anchoredPosition3D;
		gameObject.SetActive(false);

		if (globalVolume.profile.TryGet<DepthOfField>(out DepthOfField tmpDof))
		{
			dof = tmpDof;
		}

		dof.focalLength.value = 15;
	}

	public void ToggleVisibility(bool v)
    {
		if (activeStatus == v)
			return;

		if (opening)
			return;

		LeanTween.cancel(gameObject);

		if (v)
		{
			opening = true;
			world.cameraController.paused = true;
			playerInput.paused = true;
			minimapHandler.paused = true;
			world.uiTomFinder.ToggleButtonOn(false);
			world.wonderButton.ToggleEnable(false);
			world.mapHandler.SetInteractable(false);
			world.uiWorldResources.SetInteractable(false);
			world.cityBuilderManager.uiUnitTurn.ToggleEnable(false);
			world.uiMainMenuButton.ToggleButtonColor(true);
			UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, " ");

			activeStatus = true;
			world.UnselectAll();
			//world.openingImmoveable = true;
			world.immoveableCanvas.gameObject.SetActive(true);
			gameObject.SetActive(true);

			allContents.anchoredPosition3D = originalLoc + new Vector3(0, 1200f, 0);

			LeanTween.value(globalVolume.gameObject, dof.focalLength.value, 45, 0.5f)
			.setEase(LeanTweenType.easeOutSine)
			.setOnUpdate((value) =>
			{
				dof.focalLength.value = value;
			});
			LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -1200f, 0.5f).setEaseOutSine().setOnComplete(FreezeTime); //so menu doesn't freeze while tweening
			//dof.focalLength.value = 45;
		}
		else
		{
			world.cameraController.paused = false;
			playerInput.paused = false;
			minimapHandler.paused = false;
			world.uiTomFinder.ToggleButtonOn(true);
			world.wonderButton.ToggleEnable(true);
			world.mapHandler.SetInteractable(true);
			world.uiWorldResources.SetInteractable(true);
			world.cityBuilderManager.uiUnitTurn.ToggleEnable(true);
			world.uiMainMenuButton.ToggleButtonColor(false);

			//world.immoveableCanvas.gameObject.SetActive(false);
			Time.timeScale = 1;
			AudioListener.pause = false;
			//gameObject.SetActive(false);
			activeStatus = false;

			LeanTween.value(globalVolume.gameObject, dof.focalLength.value, 15, 0.3f)
			.setEase(LeanTweenType.easeOutSine)
			.setOnUpdate((value) =>
			{
				dof.focalLength.value = value;
			});
			LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 1200f, 0.3f).setOnComplete(SetActiveStatusFalse);
			//dof.focalLength.value = 15;
		}
	}

	private void FreezeTime()
	{
		Time.timeScale = 0f;
		AudioListener.pause = true;
		opening = false;
	}

	private void SetActiveStatusFalse()
	{
		gameObject.SetActive(false);
		world.ImmoveableCheck();
		//if (!world.openingImmoveable)
		//	world.immoveableCanvas.gameObject.SetActive(false);
		//else
		//	world.openingImmoveable = false;
	}

	public void SaveGameButton()
	{
		world.cityBuilderManager.PlaySelectAudio();
		uiSaveGame.ToggleVisibility(true);
	}

	public void LoadGameButton()
	{
		world.cityBuilderManager.PlaySelectAudio();
		uiSaveGame.ToggleVisibility(true, true);
	}

	public void OpenSettingsButton()
	{
		world.cityBuilderManager.PlaySelectAudio();
		uiSettings.ToggleVisibility(true);
	}

	public void CloseMenuButton()
	{
		world.cityBuilderManager.PlaySelectAudio();
		ToggleVisibility(false);
	}
}
