using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Unit : MonoBehaviour
{
    [SerializeField]
    private UnitDataSO unitDataSO;
    public UnitDataSO GetUnitData() => unitDataSO;

    [SerializeField]
    private UnitBuildDataSO buildDataSO;
    public UnitBuildDataSO GetBuildDataSO() => buildDataSO;


    [HideInInspector]
    public MapWorld world;
    [HideInInspector]
    //public UnityEvent FinishedMoving; //listeners are in BuildingManager (1) and UnitMovement (2)(3 total)

    //movement details
    private Rigidbody unitRigidbody;
    private float rotationDuration = 0.001f, moveSpeed = 0.5f, originalMoveSpeed = 0.5f;
    private Queue<Vector3Int> pathPositions = new();
    [HideInInspector]
    public bool moreToMove, isBusy; //check if they're doing something
    private Vector3 destinationLoc;
    public Vector3 DestinationLoc { set { destinationLoc = value; } }
    private int flatlandSpeed, forestSpeed, hillSpeed, forestHillSpeed;

    //selection indicators
    private SelectionHighlight highlight;
    private CameraController focusCam;
    [HideInInspector]
    public UIUnitTurnHandler turnHandler;

    [HideInInspector]
    public bool isTrader, atStop, followingRoute;

    //animation
    private Animator unitAnimator;
    private int isMovingHash;


    private void Awake()
    {
        AwakeMethods();
    }

    protected virtual void AwakeMethods()
    {
        turnHandler = FindObjectOfType<UIUnitTurnHandler>();
        focusCam = FindObjectOfType<CameraController>();
        world = FindObjectOfType<MapWorld>();
        highlight = GetComponent<SelectionHighlight>();
        unitAnimator = GetComponent<Animator>();
        isMovingHash = Animator.StringToHash("isMoving");
        unitRigidbody = GetComponent<Rigidbody>();

        //caching terrain costs
        flatlandSpeed = world.flatland.movementCost;
        forestSpeed = world.forest.movementCost;
        hillSpeed = world.hill.movementCost;
        forestHillSpeed = world.forestHill.movementCost;
        originalMoveSpeed = unitDataSO.movementSpeed;
    }

    public void CenterCamera()
    {
        focusCam.followTransform = transform;
    }


    //Methods for moving unit
    //Gets the path positions and starts the coroutines
    public void MoveThroughPath(List<Vector3Int> currentPath) 
    {
        //CenterCamera(); //focus to start moving

        world.RemoveUnitPosition(transform.position/*, gameObject*/);//removing previous location

        //destinationLoc = currentPath[currentPath.Count - 1].transform.position; //currentPath is list instead of queue for this line
        pathPositions = new Queue<Vector3Int>(currentPath);
        Vector3 firstTarget = pathPositions.Dequeue();

        moreToMove = true;
        unitRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        unitAnimator.SetBool(isMovingHash, true);
        StartCoroutine(RotationCoroutine(firstTarget));
    }

    //rotate unit before moving
    private IEnumerator RotationCoroutine(Vector3 endPosition)
    {
        //Vector3 endPosition = endPositionTile.transform.position;
        //Debug.Log("next stop is " + endPosition);

        if (pathPositions.Count == 0)
            endPosition = destinationLoc;

        Quaternion startRotation = transform.rotation;
        endPosition.y = transform.position.y;
        Vector3 direction = endPosition - transform.position;
        Quaternion endRotation = Quaternion.LookRotation(direction, Vector3.up);

        if (Mathf.Approximately(Mathf.Abs(Quaternion.Dot(startRotation, endRotation)), 1.0f) == false)
        {
            float timeElapsed = 0;
            while (timeElapsed < rotationDuration)
            {
                timeElapsed += Time.deltaTime;
                float lerpStep = timeElapsed / rotationDuration; //Value between 0 and 1
                transform.rotation = Quaternion.Lerp(startRotation, endRotation, lerpStep);
                yield return null;
            }
            transform.rotation = endRotation;
        }

        //Vector3 startPosition = transform.position;
        //int movementCost = endPositionTile.MovementCost;
        //if (!world.GetTerrainDataAt(Vector3Int.FloorToInt(startPosition)).hasRoad) //for moving onto road from non-road
        //    movementCost = endPositionTile.OriginalMovementCost; 

        StartCoroutine(MovementCoroutine(endPosition));

    }



    //moves unit
    private IEnumerator MovementCoroutine(Vector3 endPosition)
    {
        //Vector3 startPosition = transform.position;
        Vector3Int endLoc = Vector3Int.FloorToInt(endPosition);

        //checks if tile can still be moved to before moving there
        if (/*(pathPositions.Count == 0 && world.IsUnitLocationTaken(endLoc)) || */(isTrader && !world.GetTerrainDataAt(endLoc).hasRoad))
        {
            FinishMoving(endPosition);
            yield break;
        }

        //float diff = Mathf.Sqrt(Mathf.Pow(startPosition.x - endPosition.x, 2) + Mathf.Pow(startPosition.z - endPosition.z, 2));
        //movementDuration = diff * movementDurationOneTerrain;

        //    world.AddUnitPosition(destinationLoc, gameObject); //add to world dict just before moving to tile

        endPosition.y = 0f; //fixed y position
        //Debug.Log("end position is " + endPosition);
        //float timeElapsed = 0;
        //float moveSpeed = .5f;

        //Vector3 test = Vector3.one;

        while (Mathf.Pow(transform.localPosition.x - endPosition.x,2) + Mathf.Pow(transform.localPosition.z - endPosition.z,2) > 0.005f)
        {
            float movementThisFrame = Time.deltaTime * moveSpeed;
            Vector3 newPosition = Vector3.MoveTowards(transform.localPosition, endPosition, movementThisFrame);
            transform.localPosition = newPosition;

            //Debug.Log("local position is " + Mathf.Pow(transform.localPosition.x - endPosition.x,2) + Mathf.Pow(transform.localPosition.z - endPosition.z,2));

            if (Mathf.Pow(transform.localPosition.x - endPosition.x,2) + Mathf.Pow(transform.localPosition.z - endPosition.z,2) <= 0.005f)
                break;

            yield return null;
        }

        //while ((transform.localPosition - endPosition).sqrMagnitude > 0)
        //{
        //    float movementThisFrame = Time.deltaTime * moveSpeed;
        //    Vector3 newPosition = Vector3.MoveTowards(transform.localPosition, endPosition, movementThisFrame);
        //    transform.localPosition = newPosition;

        //    Debug.Log("local position is " + transform.localPosition);

        //    if ((transform.localPosition - endPosition).sqrMagnitude == 0)
        //        break;

        //    yield return null;
        //}

        //while (timeElapsed < movementDuration)
        //{
        //    timeElapsed += Time.deltaTime;
        //    float lerpStep = timeElapsed / movementDuration;
        //    //transform.position = Vector3.MoveTowards(transform.position, endPosition, diff / 100);
        //    //transform.position = Vector3.SmoothDamp(transform.position, endPosition, ref test, movementDuration);

        //    //transform.position = Vector3.Lerp(transform.position, endPosition, lerpStep);
        //    yield return null;
        //}

        //Decisions based on how many movement points and path positions are left
        if (pathPositions.Count > 0)
        {
            StartCoroutine(RotationCoroutine(pathPositions.Dequeue()));
        }
        else
        {   
            FinishMoving(endPosition);
        }
    }

    private void FinishMoving(Vector3 endPosition)
    {
        //unitRigidbody.constraints = RigidbodyConstraints.FreezePositionX;
        unitRigidbody.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;
        //unitRigidbody.constraints = RigidbodyConstraints.FreezeRotation;

        TradeRouteCheck(endPosition);
        moreToMove = false;
        pathPositions.Clear();
        unitAnimator.SetBool(isMovingHash, false);
    }

    protected void TradeRouteCheck(Vector3 endPosition)
    {
        if (isTrader && followingRoute && TryGetComponent<TradeRouteManager>(out TradeRouteManager routeManager))
        {
            Vector3Int endLoc = Vector3Int.FloorToInt(endPosition);
            
            if (endLoc == routeManager.CurrentDestination)
            {
                routeManager.SetCity(world.GetCity(endLoc));
                atStop = true;
                //Deselect(); //lots of repetition here. 
                //routeManager.CompleteTradeRouteOrders();
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        //Debug.Log("Entering tile type " + collision.gameObject.tag);
        
        if (collision.gameObject.CompareTag("Flatland"))
        {
            moveSpeed = flatlandSpeed * originalMoveSpeed;
        }
        else if (collision.gameObject.CompareTag("Forest"))
        {
            moveSpeed = forestSpeed * originalMoveSpeed * 0.25f;
        }
        else if (collision.gameObject.CompareTag("Hill"))
        {
            moveSpeed = hillSpeed * originalMoveSpeed * 0.25f;
        }
        else if (collision.gameObject.CompareTag("Forest Hill"))
        {
            moveSpeed = forestHillSpeed * originalMoveSpeed * 0.125f;
        }
    }





    //Methods for selecting and unselecting unit
    public void Select()
    {
        //selectionCircle.enabled = true;
        //highlight.ToggleGlow(true, Color.white);
        highlight.EnableHighlight(Color.white);
        //CenterCamera();
    }

    public void Deselect()
    {
        //selectionCircle.enabled = false;
        //highlight.ToggleGlow(false, Color.white);
        highlight.DisableHighlight();
    }

    public void FinishMovement() //Stops all movement, run when performing certain actions
    {
        Deselect();
        turnHandler.GoToNextUnit();
    }

    public void DestroyUnit()
    {
        RemoveUnitFromData();
        Deselect();
        Destroy(gameObject);
    }



    //Methods for movement order information 
    //displays movement orders when selected
    public List<Vector3Int> GetContinuedMovementPath() 
    {
        List<Vector3Int> continuedOrdersPositions = new(pathPositions);

        //foreach (TerrainData td in pathPositions)
        //{
        //    continuedOrdersPositions.Add(td.GetTileCoordinates());
        //}

        return continuedOrdersPositions;
    }

    public void ResetMovementOrders()
    {
        pathPositions.Clear();
        moreToMove = false;
    }

    private void RemoveUnitFromData()
    {
        ResetMovementOrders();
        turnHandler.turnHandler.RemoveUnitFromTurnList(this);
        world.RemoveUnitPosition(transform.position);
    }
}
