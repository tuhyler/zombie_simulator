using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMapHandler : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;

    [SerializeField]
    private UIMapResourceSearch resourceSearch;

    [SerializeField]
    private CameraController cameraController;

    [SerializeField]
    private Button mapButton;

    [SerializeField]
    private GameObject camDirection;

    [SerializeField]
    public RectTransform minimapHolder, minimapMask, minimapImage, minimapRing;

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

    private Vector3 originalMaskPos, originalMaskSize, newPosition, prevRotation;
    private float orthoSize, xMin, xMax, zMin, zMax, canvasRatio;
    public float movementSpeed = 1, movementTime, zoomTime;

    [HideInInspector]
    public bool activeStatus;

    private void Awake()
    {
        originalMaskPos = minimapHolder.transform.localPosition;
        originalMaskSize = minimapMask.sizeDelta;
        newPosition = transform.localPosition;
        orthoSize = minimapCamera.orthographicSize;

        canvasRatio = GetComponentInParent<CanvasUpdate>().newCanvasWidth / Screen.width;
    }

    private void LateUpdate()
    {
        if (activeStatus)
        {
            HandleKeyboardInput();
            Zoom();
            HandleEsc();
        }
    }

    public void HandleEsc()
    {
        if (Input.GetKey(KeyCode.Escape))
            ToggleMap();
    }

    public void ToggleMap()
    {
        if (world.unitOrders || world.buildingWonder)
            world.CloseBuildingSomethingPanel();
        
        if (activeStatus)
            ToggleVisibility(false);
        else
        {
            world.cityBuilderManager.PlaySelectAudio();
            world.somethingSelected = false;
            ToggleVisibility(true);
        }
    }

    public void ToggleVisibility(bool v)
    {
        if (activeStatus == v)
            return;

        Vector2 anchorChange = v ? new Vector2(0.5f, 0.5f) : new Vector2(1f, 1f);
        xMin = cameraController.xMin;
        xMax = cameraController.xMax;
        zMin = cameraController.zMin;
        zMax = cameraController.zMax;

        cameraController.enabled = !v;
        ToggleButtons();

        if (v)
        {
            world.UnselectAll();
            world.showingMap = true;
            world.ShowCityNamesMap();
            resourceSearch.gameObject.SetActive(true);
            prevRotation = cameraController.transform.localEulerAngles;
            cameraController.transform.localEulerAngles = Vector3.zero;
            activeStatus = true;
            
            Vector3 enlargedSize = new Vector3(Screen.width * canvasRatio, Screen.height * canvasRatio, 0);

            minimapRing.gameObject.SetActive(false);
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

            newPosition = cameraController.transform.position;
            //minimapCamera.transform.position = newPosition;
        }
        else
        {
            world.showingMap = false;
            world.HideCityNamesMap();

            resourceSearch.DisableHighlights();
            resourceSearch.ResetDropdown();
            resourceSearch.gameObject.SetActive(false);

            cameraController.transform.localEulerAngles = prevRotation;
            activeStatus = false;

            minimapRing.gameObject.SetActive(true);
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

    private void ToggleButtons()
    {
        if (activeStatus)
        {
            world.uiTomFinder.gameObject.SetActive(true);
            world.mapPanelButton.gameObject.SetActive(true);
            world.wonderButton.gameObject.SetActive(true);
            world.uiMainMenuButton.gameObject.SetActive(true);
			world.conversationListButton.gameObject.SetActive(true);

            if (world.uiAttackWarning.attackUnits.Count > 0)
                world.uiAttackWarning.ToggleVisibility(true);
		}
        else
        {
            world.uiTomFinder.gameObject.SetActive(false);
            world.mapPanelButton.gameObject.SetActive(false);
            world.wonderButton.gameObject.SetActive(false);
            world.uiMainMenuButton.gameObject.SetActive(false);
            world.conversationListButton.gameObject.SetActive(false);
			world.uiAttackWarning.gameObject.SetActive(false);
		}
    }

    public void AddResourceToMap(Vector3Int loc, ResourceType type)
    {
        resourceSearch.AddResourceToDict(loc, type);
    }

    public void RemoveResourceFromMap(Vector3Int loc, ResourceType type)
    {
        resourceSearch.RemoveResourceFromDict(loc, type);
    }

    public void ResetResourceLocDict()
    {
        resourceSearch.ResetResourceLocDict();
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
        newPosition.x = Mathf.Clamp(newPosition.x, xMin, xMax);
        newPosition.y = 10;
        newPosition.z = Mathf.Clamp(newPosition.z, zMin, zMax);

        minimapCamera.transform.position = Vector3.Lerp(minimapCamera.transform.position, newPosition, Time.deltaTime * movementTime);
    }

    public void SetInteractable(bool v)
    {
        mapButton.enabled = v;
    }
}
