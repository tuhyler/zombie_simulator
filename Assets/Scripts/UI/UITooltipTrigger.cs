using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class UITooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    //private static LTDescr delay;
    public string message;
    public float secondDelay = 1f;
    public bool workEthic;
    private Coroutine co, co2;
    private WaitForSeconds wait = new(1);
    private bool showMessage;
    private City city;

    public void OnPointerEnter(PointerEventData eventData)
    {
        co = StartCoroutine(ShowMessage());
        //delay = LeanTween.delayedCall(secondDelay, () => { UITooltipSystem.Show(message); });
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //LeanTween.cancel(delay.uniqueId);
        showMessage = false;
        if (co != null)
        {
            StopCoroutine(co);
            co = null;
        }
        //else if (co2 != null)
        //{
        //    StopCoroutine(co2);
        //    co2 = null;
        //}

        if (workEthic)
            UITooltipSystem.HideWorkEthic();
        else
            UITooltipSystem.Hide();
    }

    private IEnumerator ShowMessage()
    {
        showMessage = true;
        yield return wait;

        co = null;

        if (showMessage)
        {
            if (workEthic)
                UITooltipSystem.ShowWorkEthic(city);
            else
                UITooltipSystem.Show(message);
        }

        //co2 = StartCoroutine(DisplayMessage());
    }

    //private IEnumerator DisplayMessage()
    //{
    //    int waitTime = 0;
    //    while (waitTime < 2)
    //    {
    //        yield return wait;
    //        waitTime++;
    //    }

    //    UITooltipSystem.Hide();
    //}

    public void SetMessage(string message)
    {
        this.message = message;
    }

    public void SetCity(City city)
    {
        this.city = city;
    }

    public void CancelCall()
    {
        //LeanTween.cancel(delay.uniqueId);
        if (co != null)
            StopCoroutine(co);
        UITooltipSystem.Hide();
    }
}
