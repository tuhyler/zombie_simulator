using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameObject loadingScreen;
	public bool isLoading;
	private List<AsyncOperation> scenesLoading = new();

	private void Awake()
	{
		Instance = this;

		SceneManager.LoadSceneAsync((int)SceneIndexes.TITLE_SCREEN, LoadSceneMode.Additive);
		SceneManager.UnloadSceneAsync((int)SceneIndexes.MANAGER);
	}

	public void NewGame()
	{
		loadingScreen.SetActive(true);

		scenesLoading.Add(SceneManager.UnloadSceneAsync((int)SceneIndexes.TITLE_SCREEN));
		scenesLoading.Add(SceneManager.LoadSceneAsync((int)SceneIndexes.MAIN, LoadSceneMode.Additive));
		StartCoroutine(GetSceneLoadProgress(true));
	}

	public void LoadGame()
	{
		isLoading = true;
		loadingScreen.SetActive(true);

		scenesLoading.Add(SceneManager.UnloadSceneAsync((int)SceneIndexes.TITLE_SCREEN));
		scenesLoading.Add(SceneManager.LoadSceneAsync((int)SceneIndexes.MAIN, LoadSceneMode.Additive));

		StartCoroutine(GetSceneLoadProgress(false));
	}

	public IEnumerator GetSceneLoadProgress(bool newGame)
	{
		for (int i = 0; i < scenesLoading.Count; i++)
		{
			while (!scenesLoading[i].isDone)
			{
				yield return null;
			}
		}

		//SceneManager.SetActiveScene(SceneManager.GetSceneByName("ScarcityMainMap"));
		//SceneManager.UnloadSceneAsync("TitleScreen");
		if (newGame)
			loadingScreen.SetActive(false);
		else
		{
			GameLoader.Instance.LoadData();
			StartCoroutine(GetDataLoadProgress());
		}
	}

	public IEnumerator GetDataLoadProgress()
	{
		while (GameLoader.Instance == null || !GameLoader.Instance.isDone)
		{
			yield return null;
		}

		loadingScreen.SetActive(false);
		isLoading = false;
	}
}

public enum SceneIndexes
{
	MANAGER = 0,
	TITLE_SCREEN = 1,
	MAIN = 2
}
