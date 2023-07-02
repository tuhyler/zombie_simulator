using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMapHandler : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;

    [SerializeField]
    private UIUnitTurnHandler uiUnitTurn;

    [SerializeField]
    private CameraController cameraController;

    [SerializeField]
    private GameObject minimapRing, camDirection;

    [SerializeField]
    private RectTransform minimapHolder, minimapMask, minimapImage;

    [SerializeField]
    private Image minimapMaskImage;

    [SerializeField]
    private RawImage minimapRawImage;

    [SerializeField]
    private Sprite squareMask, roundMask;

    [SerializeField]
    private RenderTexture minimapScreen, mapScreen;

    [SerializeField]
    private Camera minimapCamera;

    private Vector3 originalMaskPos, originalMaskSize, newPosition;
    private float orthoSize, movementLimit = 40f;
    public float movementSpeed = 1, movementTime, zoomTime;

    [HideInInspector]
    public bool activeStatus;

    private void Awake()
    {
        originalMaskPos = minimapHolder.transform.localPosition;
        originalMaskSize = minimapMask.sizeDelta;
        newPosition = transform.localPosition;
        orthoSize = minimapCamera.orthographicSize;
    }

    private void LateUpdate()
    {
        if (activeStatus)
        {
            HandleKeyboardInput();
            Zoom();
        }
    }

    public void ToggleMap()
    {
        if (activeStatus)
            ToggleVisibility(false);
        else
            ToggleVisibility(true);
    }

    private void ToggleVisibility(bool v)
    {
        if (activeStatus == v)
            return;

        Vector2 anchorChange = v ? new Vector2(0.5f, 0.5f) : new Vector2(1f, 1f);
        cameraController.enabled = !v;

        if (v)
        {
            activeStatus = true;
            Vector3 enlargedSize = new Vector3(Screen.width * 1.25f, Screen.height * 1.25f, 0);
            world.UnselectAll();
            uiUnitTurn.gameObject.SetActive(false);

            minimapRing.SetActive(false);
            camDirection.SetActive(false);

            minimapMaskImage.sprite = squareMask;
            minimapRawImage.texture = mapScreen;

            //setting up camera
            minimapCamera.orthographicSize = orthoSize;
            minimapCamera.targetTexture = mapScreen;
            minimapCamera.GetComponent<FollowNoRotate>().active = false;

            //setting map location
            minimapHolder.anchorMin = anchorChange;
            minimapHolder.anchorMax = anchorChange;
            minimapHolder.pivot = anchorChange;
            minimapHolder.transform.localPosition = new Vector3(0, 0, 0);
            minimapHolder.sizeDelta = enlargedSize;
            minimapMask.sizeDelta = enlargedSize;
            minimapImage.sizeDelta = enlargedSize;

            minimapCamera.transform.position = newPosition;
        }
        else
        {
            activeStatus = false;
            uiUnitTurn.gameObject.SetActive(true);

            minimapRing.SetActive(true);
            camDirection.SetActive(true);

            minimapMaskImage.sprite = roundMask;
            minimapRawImage.texture = minimapScreen;

            minimapCamera.orthographicSize = 20;
            minimapCamera.targetTexture = minimapScreen;
            minimapCamera.GetComponent<FollowNoRotate>().active = true;

            minimapHolder.anchorMin = anchorChange;
            minimapHolder.anchorMax = anchorChange;
            minimapHolder.pivot = anchorChange;
            minimapHolder.transform.localPosition = originalMaskPos;
            minimapHolder.sizeDelta = originalMaskSize;
            minimapMask.sizeDelta = originalMaskSize;
            minimapImage.sizeDelta = originalMaskSize;

        }
    }

    private void HandleKeyboardInput()
    {
        //assigning keys
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            newPosition += transform.forward * movementSpeed; //up
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            newPosition -= transform.right * movementSpeed; //left
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            newPosition -= transform.forward * movementSpeed; //down
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            newPosition += transform.right * movementSpeed; //right
        }

        MoveMap();
    }

    private void Zoom()
    {
        if (Input.mouseScrollDelta.y != 0)
        {
            orthoSize += Input.mouseScrollDelta.y * -1;
        }

        orthoSize = Mathf.Clamp(orthoSize, 10, 30);

        minimapCamera.orthographicSize = Mathf.Lerp(minimapCamera.orthographicSize, orthoSize, Time.deltaTime * movementTime);
    }

    private void MoveMap()
    {
        //Clamps in ToggleVisibility method as well
        newPosition.x = Mathf.Clamp(newPosition.x, -movementLimit, movementLimit);
        newPosition.y = 10;
        newPosition.z = Mathf.Clamp(newPosition.z, -movementLimit, movementLimit);

        minimapCamera.transform.position = Vector3.Lerp(minimapCamera.transform.position, newPosition, Time.deltaTime * movementTime);
    }
}
