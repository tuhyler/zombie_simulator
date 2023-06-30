using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIMinimapHandler : MonoBehaviour, IPointerDownHandler
{
    [SerializeField]
    private CameraController cameraController;

    [SerializeField]
    private Camera minimapCam;
    
    private Image image;
    
    private RectTransform mainRect;

    float minimapCamSize;
    Vector2 imageSize;

    private void Awake()
    {
        mainRect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        image.alphaHitTestMinimumThreshold = 0.8f;
        minimapCamSize = minimapCam.orthographicSize;
        imageSize = mainRect.sizeDelta;
    }

    private void CenterCamera(Vector3 location)
    {
        cameraController.CenterCameraInstantly(location);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(mainRect, Input.mousePosition, Camera.main, out Vector2 localPoint))
        {
            Vector3 camLoc = cameraController.transform.localPosition;
            float percX = -localPoint.x / imageSize.x;
            float percY = -localPoint.y / imageSize.y;
            Vector3 centerPoint = new Vector3(camLoc.x + minimapCamSize - percX * minimapCamSize * 2, 0, camLoc.z + minimapCamSize - percY * minimapCamSize * 2);

            CenterCamera(centerPoint);
        }
    }
}
