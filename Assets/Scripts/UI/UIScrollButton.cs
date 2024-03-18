using UnityEngine;
using UnityEngine.EventSystems;

public class UIScrollButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [HideInInspector]
    public bool isDown;
    
    public void OnPointerDown(PointerEventData eventData)
    {
		if (eventData.button == PointerEventData.InputButton.Left)
			isDown = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
		if (eventData.button == PointerEventData.InputButton.Left)
			isDown = false;
    }
}
