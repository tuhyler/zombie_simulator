using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIScrollingHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    private CameraController cameraController;

    public void OnPointerEnter(PointerEventData eventData) //for scrolling the menu, don't zoom in the world
    {
        cameraController.Scrolling = true;
        Debug.Log("mouse is over the menu");
    }

    public void OnPointerExit(PointerEventData eventData) //for scrolling the menu
    {
        cameraController.Scrolling = false;
        Debug.Log("mouse is no longer over the menu");
    }
}
