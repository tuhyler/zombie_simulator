using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UITooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    //private static LTDescr delay;
    public string message;
    public float secondDelay = 1f;
    private Coroutine co, co2;
    private WaitForSeconds wait = new(1);

    public void OnPointerEnter(PointerEventData eventData)
    {
        co = StartCoroutine(ShowMessage());
        //delay = LeanTween.delayedCall(secondDelay, () => { UITooltipSystem.Show(message); });
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //LeanTween.cancel(delay.uniqueId);
        if (co != null)
        {
            StopCoroutine(co);
            co = null;
        }
        else if (co2 != null)
        {
            StopCoroutine(co2);
            co2 = null;
        }
        UITooltipSystem.Hide();
    }

    private IEnumerator ShowMessage()
    {
        yield return wait;
        co = null;

        UITooltipSystem.Show(message);
        co2 = StartCoroutine(DisplayMessage());
    }

    private IEnumerator DisplayMessage()
    {
        int waitTime = 0;
        while (waitTime < 2)
        {
            yield return wait;
            waitTime++;
        }

        UITooltipSystem.Hide();
    }

    public void SetMessage(string message)
    {
        this.message = message;
    }

    public void CancelCall()
    {
        //LeanTween.cancel(delay.uniqueId);
        if (co != null)
            StopCoroutine(co);
        UITooltipSystem.Hide();
    }
}
