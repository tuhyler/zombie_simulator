using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UITooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private static LTDescr delay;
    public string message;
    public float secondDelay = 1f;
    private Coroutine co;

    public void OnPointerEnter(PointerEventData eventData)
    {
        co = StartCoroutine(ShowMessage());
        //delay = LeanTween.delayedCall(secondDelay, () => { UITooltipSystem.Show(message); });
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //LeanTween.cancel(delay.uniqueId);
        if (co != null)
            StopCoroutine(co);
        UITooltipSystem.Hide();
    }

    private IEnumerator ShowMessage()
    {
        yield return new WaitForSeconds(1);

        UITooltipSystem.Show(message);
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
