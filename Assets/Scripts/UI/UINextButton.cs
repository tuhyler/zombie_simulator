using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UINextButton : MonoBehaviour
{
    public CanvasGroup nextTurnButton;

    public void ToggleInteractable(bool v)
    {
        nextTurnButton.interactable = v;
    }
}
