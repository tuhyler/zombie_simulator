using UnityEngine;
using UnityEngine.EventSystems;

public class UIScrollingHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    private CameraController cameraController;

    public void OnPointerEnter(PointerEventData eventData) //for scrolling the menu
    {
        cameraController.Scrolling = false;
    }

    public void OnPointerExit(PointerEventData eventData) //for scrolling the world
    {
        cameraController.Scrolling = true;
    }
}
