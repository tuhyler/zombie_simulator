using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class UIConversationTaskManager : MonoBehaviour, IImmoveable
{
	[SerializeField]
	private MapWorld world;

	[SerializeField]
	private UIMinimapHandler minimapHandler;

	[SerializeField]
	private GameObject conversationTask;

	[SerializeField]
	private Transform taskTitleHolder;

	[SerializeField]
	private TMP_Text textField; 

	private Dictionary<string, UIConversationTask> conversationTaskDict = new();
	private UIConversationTask selectedTask; 

	[SerializeField]
	private Volume globalVolume;
	private DepthOfField dof;

	[SerializeField]
	private RectTransform allContents;
	[HideInInspector]
	public bool activeStatus;
	private Vector3 originalLoc;

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

		LeanTween.cancel(gameObject);

		if (v)
		{
			world.UnselectAll();
			world.iImmoveable = this;
			world.cameraController.paused = true;
			minimapHandler.paused = true;
			world.mapHandler.SetInteractable(false);
			world.tooltip = false;
			world.somethingSelected = true;

			activeStatus = true;
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
			LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + -1200f, 0.5f).setEaseOutSine(); 
		}
		else
		{
			if (selectedTask != null)
			{
				selectedTask.RevertBackground();
				selectedTask = null;
			}

			textField.text = "";
			world.cameraController.paused = false;
			minimapHandler.paused = false;
			world.uiTomFinder.ToggleButtonOn(true);
			world.mapHandler.SetInteractable(true);
			world.BattleCamCheck(false);
			world.iImmoveable = null;

			activeStatus = false;

			LeanTween.value(globalVolume.gameObject, dof.focalLength.value, 15, 0.3f)
			.setEase(LeanTweenType.easeOutSine)
			.setOnUpdate((value) =>
			{
				dof.focalLength.value = value;
			});
			LeanTween.moveY(allContents, allContents.anchoredPosition3D.y + 1200f, 0.3f).setOnComplete(SetActiveStatusFalse);
		}
	}

	private void SetActiveStatusFalse()
	{
		gameObject.SetActive(false);
		if (world.iImmoveable == null)
			world.immoveableCanvas.gameObject.SetActive(false);
		//world.ImmoveableCheck();
	}

	public void CloseWindow()
	{
		world.cityBuilderManager.PlayCloseAudio();
		world.CloseConversationList();
		world.somethingSelected = false;
	}

	public void CreateConversationTask(string title, bool loading = false)
	{
		GameObject taskGO = Instantiate(conversationTask);
		taskGO.transform.SetParent(taskTitleHolder, false);
		UIConversationTask task = taskGO.GetComponent<UIConversationTask>();
		task.manager = this;
		task.SetTitle(title);

		if (world.allTCReps.ContainsKey(title))
		{
			if (!conversationTaskDict.ContainsKey(title))
			{
				conversationTaskDict[title] = task;
				if (!loading)
					GameLoader.Instance.gameData.conversationTasks.Add(title);
			}
			//task.taskText = npc.questHints[npc.currentQuest];
		}
		//else
		//{

		//}
			//task.taskText = UpgradeableObjectHolder.Instance.conversationTaskDict[title];
		

		//conversationTasks.Add(task);
	}

	//public void LoadConversationTask(string title)
	//{
	//	CreateConversationTask(title, true);

	//	//if (completed)
	//	//	CompleteTask(title, failed, false);
	//}

	public void SelectTask(UIConversationTask task)
	{
		if (selectedTask != null)
		{
			selectedTask.RevertBackground();
		}
		else if (selectedTask == task)
		{
			task.RevertBackground();
			textField.text = "";
		}

		selectedTask = task;

		if (world.allTCReps.ContainsKey(task.title))
		{
			string textToAdd = "";
			TradeRep tradeRep = world.allTCReps[task.title];
			int currentQuest;
			string status;
			string color;

			if (tradeRep.onQuest)
			{
				currentQuest = tradeRep.currentQuest;
				status = " (Active)";
				color = "black";
			}
			else
			{
				currentQuest = tradeRep.currentQuest - 1;
				status = " (Completed)";
				color = "#007000";
			}

			for (int i = 0; i < tradeRep.questHints.Count; i++)
			{
				if (i > 0)
					textToAdd += "\n\n";

				if (i < currentQuest)
				{
					textToAdd += "<color=#007000>Task " + (i + 1).ToString() + " (Completed)" + "\n" + tradeRep.questHints[i] + "</color>";
				}
				else if (i == currentQuest)
				{
					textToAdd += "<color=" + color + ">Task " + (i + 1).ToString() + status + "\n" + tradeRep.questHints[i] + "</color>";
				}
				else
				{
					break;
				}
			}
		
			textField.text = textToAdd;
		}
	}

	//public void CompleteTask(string title, bool failed, bool loading = false)
	//{
	//	for (int i = 0; i < conversationTasks.Count; i++)
	//	{
	//		if (conversationTasks[i].title == title)
	//		{
	//			if (!conversationTasks[i].completed)
	//			{
	//				if (!loading)
	//					GameLoader.Instance.gameData.conversationTaskDict[title] = (true, failed);
	//				conversationTasks[i].CompleteTask(failed);
	//			}
	
	//			break;
	//		}
	//	}
	//}
}
