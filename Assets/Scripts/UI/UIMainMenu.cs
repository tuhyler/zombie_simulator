using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class UIMainMenu : MonoBehaviour, IImmoveable
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
			world.UnselectAll();
			opening = true;
			world.cameraController.paused = true;
			playerInput.paused = true;
			minimapHandler.paused = true;
			world.uiTomFinder.ToggleButtonOn(false);
			world.wonderButton.ToggleEnable(false);
			world.conversationListButton.ToggleEnable(false);
			world.mapHandler.SetInteractable(false);
			world.uiWorldResources.SetInteractable(false);
			world.uiMainMenuButton.ToggleButtonColor(true);
			UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, " ");
			world.iImmoveable = this;

			activeStatus = true;
			//world.openingImmoveable = true;
			world.immoveableCanvas.gameObject.SetActive(true);
			world.BattleCamCheck(true);
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
			Resources.UnloadUnusedAssets();
		}
		else
		{
			world.cameraController.paused = false;
			playerInput.paused = false;
			minimapHandler.paused = false;
			world.uiTomFinder.ToggleButtonOn(true);
			world.wonderButton.ToggleEnable(true);
			world.conversationListButton.ToggleEnable(true);
			world.mapHandler.SetInteractable(true);
			world.uiWorldResources.SetInteractable(true);
			world.uiMainMenuButton.ToggleButtonColor(false);
			world.BattleCamCheck(false);
			world.iImmoveable = null;
			Resources.UnloadUnusedAssets();

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
		if (world.iImmoveable == null)
			world.immoveableCanvas.gameObject.SetActive(false);
		//world.ImmoveableCheck();
		//if (!world.openingImmoveable)
		//	world.immoveableCanvas.gameObject.SetActive(false);
		//else
		//	world.openingImmoveable = false;
	}

	public void SaveGameButton()
	{
		if (!activeStatus)
			return;

		world.cityBuilderManager.PlaySelectAudio();
		uiSaveGame.ToggleVisibility(true);
	}

	public void LoadGameButton()
	{
		if (!activeStatus)
			return;

		world.cityBuilderManager.PlaySelectAudio();
		uiSaveGame.ToggleVisibility(true, true);
	}

	public void OpenSettingsButton()
	{
		if (!activeStatus)
			return;

		world.cityBuilderManager.PlaySelectAudio();
		uiSettings.ToggleVisibility(true);
	}

	public void CloseMenuButton()
	{
		if (!activeStatus)
			return;
		
		world.cityBuilderManager.PlaySelectAudio();
		ToggleVisibility(false);
	}

	public void ExitToDesktopButton()
	{
		if (!activeStatus)
			return;

		Cursor.visible = false;
		Application.Quit();
	}
}
