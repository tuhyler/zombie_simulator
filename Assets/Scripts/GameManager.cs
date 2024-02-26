using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
//using UnityEngine.UIElements;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameObject loadingScreen;
	public Image loadingBackground, loadingProgress;
	public CanvasGroup alphaCanvas;
	public TMP_Text tipsText;
	[HideInInspector]
	public float currentProgress;
	private int totalToLoad = 100;
	[HideInInspector]
	public bool isLoading;
	private List<AsyncOperation> scenesLoading = new();
	public GamePersist gamePersist = new();
	[HideInInspector]
	public SettingsData settingsData = new();
	public List<Sprite> loadingScreenImages = new();
	public List<string> loadingTips = new();

	private void Awake()
	{
		Instance = this;

		SceneManager.LoadSceneAsync((int)SceneIndexes.TITLE_SCREEN, LoadSceneMode.Additive);
		SceneManager.UnloadSceneAsync((int)SceneIndexes.MANAGER);
	}

	public void NewGame(string starting, string landType, string resource, /*string mountains,*/string enemy, string mapSize, bool tutorial)
	{
		loadingScreen.SetActive(true);
		loadingBackground.sprite = loadingScreenImages[Random.Range(0, loadingScreenImages.Count)];
		StartCoroutine(GenerateTip());
		tipsText.outlineColor = Color.black;
		tipsText.outlineWidth = 0.3f;

		scenesLoading.Clear();
		scenesLoading.Add(SceneManager.UnloadSceneAsync((int)SceneIndexes.TITLE_SCREEN));
		scenesLoading.Add(SceneManager.LoadSceneAsync((int)SceneIndexes.MAIN, LoadSceneMode.Additive));
		StartCoroutine(GetSceneLoadProgress(true, "", starting, landType, resource, /*mountains,*/enemy, mapSize, tutorial));
	}

	public void LoadGame(string loadName)
	{
		isLoading = true;
		loadingScreen.SetActive(true);
		loadingBackground.sprite = loadingScreenImages[Random.Range(0, loadingScreenImages.Count)];
		StartCoroutine(GenerateTip());
		tipsText.outlineColor = Color.black;
		tipsText.outlineWidth = 0.3f;

		scenesLoading.Clear();
		scenesLoading.Add(SceneManager.UnloadSceneAsync((int)SceneIndexes.TITLE_SCREEN));
		scenesLoading.Add(SceneManager.LoadSceneAsync((int)SceneIndexes.MAIN, LoadSceneMode.Additive));

		StartCoroutine(GetSceneLoadProgress(false, loadName));
	}

	public GameData GetLoadInfo(string loadName)
	{
		return gamePersist.LoadData(loadName, false);
	}

	public void BackToMainMenu(bool load, string loadName = "")
	{
		//loadingScreen.SetActive(true);
		scenesLoading.Clear();
		scenesLoading.Add(SceneManager.UnloadSceneAsync((int)SceneIndexes.MAIN));
		scenesLoading.Add(SceneManager.LoadSceneAsync((int)SceneIndexes.TITLE_SCREEN, LoadSceneMode.Additive));

		StartCoroutine(GetMainMenuReturnProgress(load, loadName));
	}

	public IEnumerator GetMainMenuReturnProgress(bool load, string loadName = "")
	{
		for (int i = 0; i < scenesLoading.Count; i++)
		{
			while (!scenesLoading[i].isDone)
			{
				yield return null;
			}
		}

		if (load)
		{
			LoadGame(loadName);
		}
		else
		{
			//loadingScreen.SetActive(false);
		}
	}

	public IEnumerator GetSceneLoadProgress(bool newGame, string loadName = "", string starting = "", string landType = "", string resource = "", /*string mountains = "",*/string enemy = "", string mapSize = "", bool tutorial = false)
	{
		for (int i = 0; i < scenesLoading.Count; i++)
		{
			while (!scenesLoading[i].isDone)
			{
				yield return null;
			}
		}

		if (newGame)
		{
			GameLoader.Instance.NewGame(starting, landType, resource, enemy, mapSize, tutorial);
			StartCoroutine(GetNewGameProgress());
			//loadingScreen.SetActive(false);
			//scenesLoading.Clear();
		}
		else
		{
			string totalLoadName = "/" + loadName + ".save";
			GameLoader.Instance.LoadData(totalLoadName);
			StartCoroutine(GetDataLoadProgress());
		}
	}

	public IEnumerator GetNewGameProgress()
	{
		while (GameLoader.Instance == null || !GameLoader.Instance.isDone)
		{
			yield return null;
		}

		loadingScreen.SetActive(false);
		scenesLoading.Clear();
	}

	public IEnumerator GetDataLoadProgress()
	{
		while (GameLoader.Instance == null || !GameLoader.Instance.isDone)
		{
			yield return null;
		}

		loadingScreen.SetActive(false);
		isLoading = false;
		scenesLoading.Clear();
	}

	public void ResetProgress()
	{
		currentProgress = 0;
		loadingProgress.fillAmount = 0;
	}

	public void UpdateProgress(int v)
	{
		currentProgress += v;
		loadingProgress.fillAmount = currentProgress / totalToLoad;

		//can't really do this, don't know how fast it will load
		//LeanTween.value(loadingProgress.gameObject, loadingProgress.fillAmount, currentProgress / totalToLoad, 1f)
		//	.setEase(LeanTweenType.linear)
		//	.setOnUpdate((value) =>
		//	{
		//		loadingProgress.fillAmount = value;
		//	});
	}

	private IEnumerator GenerateTip()
	{
		List<string> tips = new(loadingTips);
		alphaCanvas.alpha = 0;

		while (isLoading && tips.Count > 0)
		{	
			string shownTip = tips[Random.Range(0, tips.Count)];
			tipsText.text = shownTip;
			tips.Remove(shownTip);

			LeanTween.alphaCanvas(alphaCanvas, 1, 0.5f);

			yield return new WaitForSeconds(5f);

			LeanTween.alphaCanvas(alphaCanvas, 0, 0.5f);

			yield return new WaitForSeconds(0.5f);
		}
	}
}

public enum SceneIndexes
{
	MANAGER = 0,
	TITLE_SCREEN = 1,
	MAIN = 2
}
