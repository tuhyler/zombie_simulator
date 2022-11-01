using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
    //public static CameraController instance; //for only one time use in another class

    [SerializeField]
    public Transform cameraTransform;
    //CameraController.instance.followTransform = transform; //use this in other scripts to center and follow; 

    [HideInInspector]
    public Transform followTransform;
    [HideInInspector]
    public Transform centerTransform;
    [HideInInspector]
    public Quaternion followRotation;

    private bool scrolling;
    public bool Scrolling { set { scrolling = value; } }

    private float movementLimit = 40f;
    private float edgeSize = 40f; //pixel buffer size

    public float movementSpeed, movementTime, rotationAmount;
    public Vector3 zoomAmount;

    public Vector3 newPosition, newZoom;
    public Quaternion newRotation;

    void Start()
    {
        //instance = this;

        newPosition = transform.position; //set static position that doesn't default to 0
        newZoom = cameraTransform.localPosition; //local position so that the text layer stays in rightful place
        newRotation = transform.rotation;
        scrolling = true;
    }

    void LateUpdate() //Lateupdate to reduce jittering on camera 
    {
        if (followTransform != null) //to center on something
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * 5);
            transform.position = Vector3.Lerp(transform.position, followTransform.position, Time.deltaTime * movementTime);
            newPosition = followTransform.position; //so you don't return to previous spot when breaking focus
        }
        else if (centerTransform != null)
        {
            transform.position = Vector3.Lerp(transform.position, centerTransform.position + new Vector3(0, -.4f, 0.7f), Time.deltaTime * movementTime);
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, new Vector3(0, 17.5f, -11.0f), Time.deltaTime * movementTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(20, 0, 0), Time.deltaTime * movementTime);
            newPosition = centerTransform.position; //so you don't return to previous spot when breaking focus
            newZoom = cameraTransform.localPosition;
        }
        else
        {
            HandleKeyboardMovementInput();
            //HandleMouseMovementInput(); //turned off temporarily
        }
        //if (!EventSystem.current.IsPointerOverGameObject()) //so you can still zoom in but still be focused
        if (scrolling)
            HandleMouseZoomInput(); 


        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) ||
            Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.RightArrow) ||
            Input.mousePosition.y > Screen.height - edgeSize || Input.mousePosition.x < edgeSize || Input.mousePosition.y < edgeSize ||
            Input.mousePosition.x > Screen.width - edgeSize) //break focus
        //if (Input.anyKey) //break focus
        {
            followTransform = null;
        }

        //if (Input.mousePosition.y > Screen.height - edgeSize || Input.mousePosition.x < edgeSize || 
        //    Input.mousePosition.y < edgeSize || Input.mousePosition.x > Screen.width - edgeSize) 
        //{
        //    followTransform = null;
        //}
        //if (Input.GetKey(KeyCode.Q))
        //{
        //    newRotation *= Quaternion.Euler(Vector3.left * rotationAmount);
        //}
        //if (Input.GetKey(KeyCode.E))
        //{
        //    newRotation *= Quaternion.Euler(Vector3.left * -rotationAmount);
        //}
    }

    public void CenterCameraNoFollow(Vector3 pos)
    {
        followTransform = null;
        newPosition = pos;
    }

    public void SetZoom(Vector3 zoom)
    {
        newZoom = zoom;
    }

    private void HandleKeyboardMovementInput()
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

        //for rotation
        //if (Input.GetKey(KeyCode.Q))
        //{
        //    newRotation *= Quaternion.Euler(Vector3.up * rotationAmount);
        //}
        //if (Input.GetKey(KeyCode.E))
        //{
        //    newRotation *= Quaternion.Euler(Vector3.up * -rotationAmount);
        //}

        MoveCamera();
    }

    private void HandleMouseMovementInput()
    {

        if (Input.mousePosition.y > Screen.height - edgeSize)
        {
            newPosition += transform.forward * movementSpeed; //up
        }
        if (Input.mousePosition.x < edgeSize)
        {
            newPosition -= transform.right * movementSpeed; //left
        }
        if (Input.mousePosition.y < edgeSize)
        {
            newPosition -= transform.forward * movementSpeed; //down
        }
        if (Input.mousePosition.x > Screen.width - edgeSize)
        {
            newPosition += transform.right * movementSpeed; //right
        }

        MoveCamera();
    }

    private void MoveCamera()
    {
        //movement limits
        newPosition.x = Mathf.Clamp(newPosition.x, -movementLimit, movementLimit);
        newPosition.z = Mathf.Clamp(newPosition.z, -movementLimit, movementLimit); //change to edge

        //smoothing camera movement
        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * movementTime);
    }

    public void HandleMouseZoomInput()
    {
        //assigning mousewheel
        if (Input.mouseScrollDelta.y != 0)
        {
            newZoom += Input.mouseScrollDelta.y * zoomAmount;
        }

        ZoomCamera();
    }

    private void ZoomCamera()
    {
        //zooming limits (set manually)
        newZoom.y = Mathf.Clamp(newZoom.y, 5f, 17); //difference between the two need to be the same. 
        newZoom.z = Mathf.Clamp(newZoom.z, -13f, -1); //last has to be -1ish

        //smoothing zoom
        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, newZoom, Time.deltaTime * movementTime);
    }

    private void RotateCamera()
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * movementTime);
    }
}
