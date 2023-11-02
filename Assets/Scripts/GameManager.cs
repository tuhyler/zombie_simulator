using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameObject loadingScreen;
	private List<AsyncOperation> scenesLoading = new();

	private void Awake()
	{
		Instance = this;

		//SceneManager.LoadSceneAsync("LoadingScreen", LoadSceneMode.Additive);
	}

	public void NewGame()
	{
		loadingScreen.SetActive(true);

		scenesLoading.Add(SceneManager.LoadSceneAsync("ScarcityMainMap", LoadSceneMode.Additive));
		StartCoroutine(GetSceneLoadProgress());
	}

	public void LoadGame()
	{
		loadingScreen.SetActive(true);

		GameLoader.Instance.LoadData();
		scenesLoading.Add(SceneManager.UnloadSceneAsync("LoadingScreen"));
		scenesLoading.Add(SceneManager.LoadSceneAsync("ScarcityMainMap", LoadSceneMode.Additive));

		StartCoroutine(GetSceneLoadProgress());
		StartCoroutine(GetDataLoadProgress());
	}

	public IEnumerator GetSceneLoadProgress()
	{
		for (int i = 0; i < scenesLoading.Count; i++)
		{
			while (!scenesLoading[i].isDone)
			{
				yield return null;
			}
		}

		SceneManager.SetActiveScene(SceneManager.GetSceneByName("ScarcityMainMap"));
		SceneManager.UnloadSceneAsync("LoadingScreen");
		loadingScreen.SetActive(false);
	}

	public IEnumerator GetDataLoadProgress()
	{
		while (GameLoader.Instance == null || !GameLoader.Instance.isDone)
		{
			yield return null;
		}

		loadingScreen.SetActive(false);
	}
}
