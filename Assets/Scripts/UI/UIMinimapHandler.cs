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

    [SerializeField]
    private UIMapHandler mapHandler;
    
    private Image image;
    
    private RectTransform mainRect;

    float minimapCamSize;
    Vector2 imageSize;

    private void Awake()
    {
        mainRect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        image.alphaHitTestMinimumThreshold = 0.8f;
    }

    private void CenterCamera(Vector3 location)
    {
        cameraController.CenterCameraInstantly(location);

        if (mapHandler.activeStatus)
            mapHandler.ToggleMap();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(mainRect, Input.mousePosition, Camera.main, out Vector2 localPoint))
        {
            //Debug.Log(localPoint);
            imageSize = mainRect.sizeDelta;
            minimapCamSize = minimapCam.orthographicSize;

            Vector3 camLoc = cameraController.transform.localPosition;
            Vector3 minimapCamLoc = minimapCam.transform.localPosition;
            float percX = localPoint.x / (imageSize.x * 0.5f);
            float percY = localPoint.y / (imageSize.y * 0.5f);

            Vector3 centerPoint;

            if (mapHandler.activeStatus)
                centerPoint = new Vector3(minimapCamLoc.x + minimapCamSize * 1.75f * percX, 0, minimapCamLoc.z + minimapCamSize * percY);
            else
                centerPoint = new Vector3(camLoc.x + minimapCamSize * percX, 0, camLoc.z + minimapCamSize * percY);
            //Vector3 centerPoint = new Vector3(camLoc.x + minimapCamSize - percX * minimapCamSize * 2, 0, camLoc.z + minimapCamSize - percY * minimapCamSize * 2);

            if (!cameraController.BoundaryCheck(centerPoint))
                return;

            CenterCamera(centerPoint);
        }
    }
}
