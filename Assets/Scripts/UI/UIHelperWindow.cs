using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIHelperWindow : MonoBehaviour
{
    [SerializeField]
    private TMP_Text messageText;

    [SerializeField]
    private GameObject arrowUp, arrowRight, arrowDown, arrowLeft; 

    [SerializeField]
    private RectTransform allContents;

    [HideInInspector]
    public bool activeStatus;

	private void Awake()
	{
        gameObject.SetActive(false);
	}

	public void SetMessage(string text)
    {
        messageText.text = text;
    }

    public void ToggleVisibility(bool v, int arrowDirection = 0)
    {
        if (activeStatus == v)
            return;

        if (v)
        {
            activeStatus = true;
            gameObject.SetActive(true);
            SetArrow(arrowDirection);

			allContents.localScale = Vector3.zero;
			LeanTween.scale(allContents, Vector3.one, 0.25f).setEase(LeanTweenType.linear);
		}
        else
        {
            activeStatus = false;
            gameObject.SetActive(false);
			//LeanTween.scale(allContents, Vector3.zero, 0.25f).setDelay(0.125f).setOnComplete(SetActiveStatusFalse);
		}
    }

	//public void SetActiveStatusFalse()
	//{
	//	gameObject.SetActive(false);
	//}

	public void SetPlacement(Vector3 placement, Vector2 pivot)
    {
        allContents.pivot = pivot;
        allContents.anchorMin = pivot;
        allContents.anchorMax = pivot;
        allContents.anchoredPosition3D = placement;
    }

    public void CloseWindowButton()
    {
        ToggleVisibility(false);
    }

    private void SetArrow(int direction)
    {
        if (direction == 0)
        {
            arrowUp.SetActive(true);
            arrowRight.SetActive(false);
            arrowDown.SetActive(false);
            arrowLeft.SetActive(false);
        }
        else if (direction == 1)
        {
            arrowUp.SetActive(false);
            arrowRight.SetActive(true);
			arrowDown.SetActive(false);
			arrowLeft.SetActive(false);
		}
        else if (direction == 2)
        {
			arrowUp.SetActive(false);
			arrowRight.SetActive(false);
			arrowDown.SetActive(true);
			arrowLeft.SetActive(false);
		}
        else
        {
			arrowUp.SetActive(false);
			arrowRight.SetActive(false);
			arrowDown.SetActive(false);
            arrowLeft.SetActive(true);
        }
    }
}
