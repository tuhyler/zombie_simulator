using UnityEngine;
using UnityEngine.EventSystems;

public class UIScrollingHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    //[SerializeField]
    private CameraController cameraController;
    private UIMapHandler mapHandler;

    private void Awake()
    {
        cameraController = FindObjectOfType<CameraController>();
        mapHandler = FindObjectOfType<UIMapHandler>();
    }

    public void OnPointerEnter(PointerEventData eventData) //for scrolling the menu
    {
        cameraController.scrolling = false;
    }

    public void OnPointerExit(PointerEventData eventData) //for scrolling the world
    {
        cameraController.scrolling = true;
    }
}
