using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class HandlePlayerInput : MonoBehaviour
{
    public Camera mainCamera;
    public LayerMask layerMask;

    private Vector3 mousePositionClick;

    //all input is handled on this class through unity events, just more organized here. (here and camera controller)
    public UnityEvent<GameObject> HandleOtherSelection;
    //public UnityEvent<GameObject> HandleTileSelection;
    public UnityEvent<Vector3, GameObject> HandleLocationSelection;
    public UnityEvent HandleShiftDown, HandleShiftUp, HandleR, HandleEnter, HandleC, HandleB, HandleG, HandleX, HandleSpace;
    //public UnityEvent HandleShiftUp;
    //public UnityEvent HandleR;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())  //second check is so you don't click behind the UI
            HandleMouseClick();

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) //for order queueing
        {
            HandleShiftDown?.Invoke();
        }

        if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift))
        {
            HandleShiftUp?.Invoke();
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            HandleR?.Invoke();
        }

        if (Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.KeypadEnter))
        {
            HandleEnter?.Invoke();
        }

        if (Input.GetKeyUp(KeyCode.C))
        {
            HandleC?.Invoke();
        }

        if (Input.GetKeyUp(KeyCode.B))
        {
            HandleB?.Invoke();
        }

        if (Input.GetKeyUp(KeyCode.G))
        {
            HandleG?.Invoke();
        }

        if (Input.GetKeyUp(KeyCode.X))
        {
            HandleX?.Invoke();
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            HandleSpace?.Invoke();
        }
    }

    private void HandleMouseClick()
    {
        GameObject selectedGameObject;

        mousePositionClick = Input.mousePosition;
        Ray ray = mainCamera.ScreenPointToRay(mousePositionClick);
        if (Physics.Raycast(ray, out RaycastHit hit, 100, layerMask))
        {
            selectedGameObject = hit.collider.gameObject;
        }
        else
            selectedGameObject = null;

        if (selectedGameObject == null)
            return;

        HandleLocationSelection?.Invoke(hit.point, selectedGameObject);
        HandleOtherSelection?.Invoke(selectedGameObject);

        //if (selectedGameObject.GetComponent<TerrainData>() == null)
        //    HandleOtherSelection?.Invoke(selectedGameObject);
        //else
        //    HandleTileSelection?.Invoke(selectedGameObject);
    }
}
