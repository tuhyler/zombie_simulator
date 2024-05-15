using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIConversationTask : MonoBehaviour, IPointerDownHandler
{
	[SerializeField]
	private TMP_Text titleText, subtext;
	
	[HideInInspector]
    public string title/*, taskText*/;

	[HideInInspector]
	public UIConversationTaskManager manager;

	[HideInInspector]
	public bool completed, failed;

	[SerializeField]
	private Image background;
	private Color originalColor;

	private void Awake()
	{
		originalColor = background.color;
	}

	public void RevertBackground()
	{
		background.color = originalColor;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left)
		{
			background.color = new Color(.8f, .8f, .8f);
			manager.SelectTask(this);
		}
	}

	public void SetTitle(string title)
	{
		this.title = title;
		titleText.text = title;
	}

	public void CompleteTask(bool failed)
	{
		subtext.gameObject.SetActive(true);
		completed = true;

		if (failed)
		{
			titleText.color = Color.red;
			subtext.text = "Failed";
			subtext.color = Color.red;
			this.failed = true;
		}
		else
		{
			titleText.color = Color.green;
			subtext.text = "Completed";
			subtext.color = Color.green;
		}
	}
}
