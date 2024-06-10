using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class HandlePlayerInput : MonoBehaviour
{
    //public MapWorld world;
    public Camera mainCamera;
    public LayerMask layerMask;
    [HideInInspector]
    public bool paused;

    private Vector3 mousePositionClick;

    //all input is handled on this class through unity events, just more organized here. (here and camera controller)
    //public UnityEvent<GameObject> HandleOtherSelection;
    //public UnityEvent<GameObject> HandleCityTileSelection;
    public UnityEvent<Vector3, GameObject> HandleLocationSelection;
    public UnityEvent<Vector3, GameObject> HandleLocationMovementSelection;
    public UnityEvent HandleShiftDown, HandleShiftUp, HandleEsc, HandleR, HandleEnter, HandleC, HandleB, HandleF, HandleG, HandleX, HandleSpace, HandleJ, HandleK, HandleM, HandleN, HandleT, HandleI, HandleZ,
        HandleCtrlT, Handle1, Handle2, Handle3, HandleTilde;
    //public UnityEvent HandleShiftUp;
    //public UnityEvent HandleR;


    private void Update()
    {
        if (paused)
            return;
        
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) //second check is so you don't click behind the UI
            HandleLeftMouseClick();

        if (Input.GetMouseButtonDown(1) && !EventSystem.current.IsPointerOverGameObject())  //second check is so you don't click behind the UI
            HandleRightMouseClick();

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) //for order queueing
        {
            HandleShiftDown?.Invoke();
        }

        if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift))
        {
            HandleShiftUp?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            HandleB?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            HandleC?.Invoke();
        }

		if (Input.GetKeyDown(KeyCode.F))
		{
			HandleF?.Invoke();
		}
		
        if (Input.GetKeyDown(KeyCode.G))
        {
            HandleG?.Invoke();
        }

		if (Input.GetKeyDown(KeyCode.I))
		{
			HandleI?.Invoke();
		}

		if (Input.GetKeyDown(KeyCode.J))
        {
            HandleJ?.Invoke();
        }

		if (Input.GetKeyDown(KeyCode.K))
		{
			HandleK?.Invoke();
		}

		if (Input.GetKeyDown(KeyCode.M))
		{
			HandleM?.Invoke();
		}

		if (Input.GetKeyDown(KeyCode.N))
		{
			HandleN?.Invoke();
		}

		if (Input.GetKeyDown(KeyCode.T))
		{
			HandleT?.Invoke();
		}

		if (Input.GetKeyDown(KeyCode.R))
        {
            HandleR?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            HandleX?.Invoke();
        }

		if (Input.GetKeyDown(KeyCode.Z))
		{
			HandleZ?.Invoke();
		}

		if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            HandleEnter?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            HandleSpace?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEsc?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.F12))
        {
            HandleCtrlT?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Handle1?.Invoke();
        }

		if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			Handle2?.Invoke();
		}

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Handle3?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            HandleTilde.Invoke();
        }
	}

    private void HandleLeftMouseClick()
    {
        GameObject selectedGameObject;

        mousePositionClick = Input.mousePosition;
        Ray ray = mainCamera.ScreenPointToRay(mousePositionClick);

		//RaycastHit[] hits = Physics.RaycastAll(ray, 100, layerMask);

		//if (hits.Length > 0)
		//{
		//	bool hitUnit = false;
		//	int loc = 0;
		//	for (int i = 0; i < hits.Length; i++)
		//	{
		//		if (hits[i].collider.gameObject.GetComponent<Unit>())
		//		{
		//			hitUnit = true;
		//			loc = i;
		//			break;
		//		}
		//	}

		//	if (hitUnit)
		//	{
		//		world.unitMovement.HandleUnitSelectionAndMovement(hits[loc].point, hits[loc].collider.gameObject); //"Resource" is in here
		//	}
		//	else
		//	{
  //              world.cityBuilderManager.HandleCitySelection(hits[0].point, hits[0].collider.gameObject); //need to account for wonder placement
		//	}
		//}

		if (Physics.Raycast(ray, out RaycastHit hit, 100, layerMask))
            selectedGameObject = hit.collider.gameObject;
        else
            selectedGameObject = null;

        if (selectedGameObject == null)
            return;

        HandleLocationSelection?.Invoke(hit.point, selectedGameObject);
    }

    private void HandleRightMouseClick()
    {
        GameObject selectedGameObject;

        mousePositionClick = Input.mousePosition;
        Ray ray = mainCamera.ScreenPointToRay(mousePositionClick);
        if (Physics.Raycast(ray, out RaycastHit hit, 100, layerMask))
            selectedGameObject = hit.collider.gameObject;
        else
            selectedGameObject = null;

        if (selectedGameObject == null)
            return;

        HandleLocationMovementSelection?.Invoke(hit.point, selectedGameObject);
    }
}
