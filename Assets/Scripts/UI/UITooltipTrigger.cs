using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UITooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private static LTDescr delay;
    public string message;
    public float secondDelay = 1f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        delay = LeanTween.delayedCall(secondDelay, () => { UITooltipSystem.Show(message); });
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        LeanTween.cancel(delay.uniqueId);
        UITooltipSystem.Hide();
    }

    public void SetMessage(string message)
    {
        this.message = message;
    }

    public void CancelCall()
    {
        LeanTween.cancel(delay.uniqueId);
    }
}
