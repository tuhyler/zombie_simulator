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
    public UnityEvent FinishedMoving; //listeners are in BuildingManager (1) and UnitMovement (2)(3 total)

    //movement details
    private Rigidbody unitRigidbody;
    private float rotationDuration = 0.2f, moveSpeed = 0.5f, originalMoveSpeed = 0.5f, threshold = 0.005f;
    private Queue<Vector3Int> pathPositions = new();
    [HideInInspector]
    public bool moreToMove, isBusy, isMoving; //check if they're doing something
    private Vector3 destinationLoc;
    private Vector3 finalDestinationLoc;
    public Vector3 FinalDestinationLoc { get { return finalDestinationLoc; } set { finalDestinationLoc = value; } }
    private int flatlandSpeed, forestSpeed, hillSpeed, forestHillSpeed, roadSpeed;
    private Coroutine movingCo;
    private MovementSystem movementSystem;
    private Queue<GameObject> shoePrintQueue = new();

    //selection indicators
    private SelectionHighlight highlight;
    private CameraController focusCam;
    [HideInInspector]
    public UIUnitTurnHandler turnHandler;

    [HideInInspector]
    public bool isTrader, atStop, followingRoute, isWorker;

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
        movementSystem = FindObjectOfType<MovementSystem>();
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

    private void Start()
    {
        turnHandler.turnHandler.AddToTurnList(this);
        roadSpeed = world.GetRoadCost();
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

        //finalDestinationLoc = currentPath[currentPath.Count - 1].transform.position; //currentPath is list instead of queue for this line
        pathPositions = new Queue<Vector3Int>(currentPath);
        ShowPath(currentPath);
        Vector3 firstTarget = pathPositions.Dequeue();

        moreToMove = true;
        isMoving = true;
        unitRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePosition;
        unitAnimator.SetBool(isMovingHash, true);
        movingCo = StartCoroutine(MovementCoroutine(firstTarget));
    }

    //rotate unit before moving
    private IEnumerator MovementCoroutine(Vector3 endPosition)
    {
        //Vector3 endPosition = endPositionTile.transform.position;
        //Debug.Log("next stop is " + endPosition);

        destinationLoc = endPosition;

        //checks if tile can still be moved to before moving there
        if (/*(pathPositions.Count == 0 && world.IsUnitLocationTaken(endLoc)) || */(isTrader && !world.IsRoadOnTileLocation(Vector3Int.FloorToInt(endPosition))))
        {
            FinishMoving(endPosition);
            yield break;
        }

        if (pathPositions.Count == 0)
            endPosition = finalDestinationLoc;

        endPosition.y = 0f; //fixed y position

        Quaternion startRotation = transform.rotation;
        endPosition.y = transform.position.y;
        Vector3 direction = endPosition - transform.position;
        Quaternion endRotation = Quaternion.LookRotation(direction, Vector3.up);

        //if (Mathf.Approximately(Mathf.Abs(Quaternion.Dot(startRotation, endRotation)), 1.0f) == false)
        //{
        float timeElapsed = 0;
        while (Mathf.Pow(transform.localPosition.x - endPosition.x, 2) + Mathf.Pow(transform.localPosition.z - endPosition.z, 2) > threshold)
        {
            timeElapsed += Time.deltaTime;
            float movementThisFrame = Time.deltaTime * moveSpeed;
            float lerpStep = timeElapsed / rotationDuration; //Value between 0 and 1
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, endPosition, movementThisFrame);
            //transform.localPosition = newPosition;
            transform.rotation = Quaternion.Lerp(startRotation, endRotation, lerpStep);

            if (Mathf.Pow(transform.localPosition.x - endPosition.x, 2) + Mathf.Pow(transform.localPosition.z - endPosition.z, 2) <= threshold)
                break;

            yield return null;
        }
        //transform.rotation = endRotation;
        //}

        //Vector3 startPosition = transform.position;
        //int movementCost = endPositionTile.MovementCost;
        //if (!world.GetTerrainDataAt(Vector3Int.FloorToInt(startPosition)).hasRoad) //for moving onto road from non-road
        //    movementCost = endPositionTile.OriginalMovementCost; 

        if (pathPositions.Count > 0)
        {
            movingCo = StartCoroutine(MovementCoroutine(pathPositions.Dequeue()));
            if (shoePrintQueue.Count > 0)
                movementSystem.AddToShoePrintPool(shoePrintQueue.Dequeue());
        }
        else
        {
            FinishMoving(endPosition);
        }
    }



    //moves unit
    //private void MovementCoroutine(Vector3 endPosition)
    //{
        //Vector3 startPosition = transform.position;
        //Vector3Int endLoc = Vector3Int.FloorToInt(endPosition);
        //destinationLoc = endPosition;

        ////checks if tile can still be moved to before moving there
        //if (/*(pathPositions.Count == 0 && world.IsUnitLocationTaken(endLoc)) || */(isTrader && !world.GetTerrainDataAt(endLoc).hasRoad))
        //{
        //    FinishMoving(endPosition);
        //    yield break;
        //}

        //float diff = Mathf.Sqrt(Mathf.Pow(startPosition.x - endPosition.x, 2) + Mathf.Pow(startPosition.z - endPosition.z, 2));
        //movementDuration = diff * movementDurationOneTerrain;

        //    world.AddUnitPosition(finalDestinationLoc, gameObject); //add to world dict just before moving to tile

        //endPosition.y = 0f; //fixed y position
        //Debug.Log("end position is " + endPosition);
        //Debug.Log("transform position is " + transform.localPosition);
        //float timeElapsed = 0;
        //float moveSpeed = .5f;

        //Vector3 test = Vector3.one;

        //while (Mathf.Pow(transform.localPosition.x - endPosition.x,2) + Mathf.Pow(transform.localPosition.z - endPosition.z,2) > 0.005f)
        //{
        //    float movementThisFrame = Time.deltaTime * moveSpeed;
        //    Vector3 newPosition = Vector3.MoveTowards(transform.localPosition, endPosition, movementThisFrame);
        //    transform.localPosition = newPosition;

        //    //Debug.Log("current location is " + transform.localPosition);
        //    Debug.Log("local position is " + Mathf.Pow(transform.localPosition.x - endPosition.x,2) + Mathf.Pow(transform.localPosition.z - endPosition.z,2));

        //    if (Mathf.Pow(transform.localPosition.x - endPosition.x,2) + Mathf.Pow(transform.localPosition.z - endPosition.z,2) <= 0.005f)
        //        break;

        //    if (!isMoving)
        //        yield break;

        //    yield return null;
        //}

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
    //    if (pathPositions.Count > 0)
    //    {
    //        StartCoroutine(MovementCoroutine(pathPositions.Dequeue()));
    //    }
    //    else
    //    {   
    //        FinishMoving(endPosition);
    //    }
    //}

    public void StopMovement()
    {
        if (movingCo != null)
            StopCoroutine(movingCo);
        FinishMoving(destinationLoc);
    }

    public void AddToMovementQueue(List<Vector3Int> queuedOrders)
    {
        queuedOrders.ForEach(pos => pathPositions.Enqueue(pos));
        ShowPath(queuedOrders, true);
    }

    private void FinishMoving(Vector3 endPosition)
    {
        //unitRigidbody.constraints = RigidbodyConstraints.FreezePositionX;
        unitRigidbody.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation; 
        //unitRigidbody.constraints = RigidbodyConstraints.FreezeRotation;

        TradeRouteCheck(endPosition);
        moreToMove = false;
        isMoving = false;
        HidePath();
        pathPositions.Clear();
        unitAnimator.SetBool(isMovingHash, false);
        FinishedMoving?.Invoke();
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
        Debug.Log("colliding with " + collision.gameObject.tag);
        
        threshold = 0.001f;
        
        if (collision.gameObject.CompareTag("Flatland"))
        {
            moveSpeed = (flatlandSpeed / 10f) * originalMoveSpeed;
        }
        else if (collision.gameObject.CompareTag("Forest"))
        {
            moveSpeed = (forestSpeed / 10f) * originalMoveSpeed * 0.25f;
        }
        else if (collision.gameObject.CompareTag("Hill"))
        {
            moveSpeed = (hillSpeed / 10f) * originalMoveSpeed * 0.25f;
            threshold = 0.01f;
        }
        else if (collision.gameObject.CompareTag("Forest Hill"))
        {
            moveSpeed = (forestHillSpeed / 10f) * originalMoveSpeed * 0.125f;
        }
        else if (collision.gameObject.CompareTag("Road"))
        {
            moveSpeed = (roadSpeed / 10f) * originalMoveSpeed;
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

    //public void FinishMovement() //Stops all movement, run when performing certain actions
    //{
    //    Deselect();
    //    turnHandler.GoToNextUnit();
    //}

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

    public void ShowContinuedPath()
    {
        ShowPath(new List<Vector3Int>(pathPositions));
    }

    private void ShowPath(List<Vector3Int> currentPath, bool queued = false)
    {
        //List<Vector3Int> currentPath = new(pathPositions);

        for (int i = 0; i < currentPath.Count; i++)
        {
            //position to place shoePrint
            Vector3 turnCountPosition = currentPath[i];
            turnCountPosition.y += 0.01f; //added .01 to make it not blend with terrain

            Vector3 prevPosition;

            if (i == 0)
            {
                if (queued)
                    prevPosition = Vector3Int.RoundToInt(finalDestinationLoc);
                else
                    prevPosition = transform.position;
            }
            else
            {
                prevPosition = currentPath[i - 1];
            }

            prevPosition.y = 0.01f;
            GameObject shoePrintPath = movementSystem.GetFromShoePrintPool();
            shoePrintPath.transform.position = (turnCountPosition + prevPosition) / 2;
            float xDiff = turnCountPosition.x - prevPosition.x;
            float zDiff = turnCountPosition.z - prevPosition.z;

            float x = 0;
            int z = 0;

            if (Mathf.Abs(xDiff) + Mathf.Abs(zDiff) == 1)
            {
                x = .07f;
            }

            //checking tile placements to see how to rotate shoe prints
            if (xDiff < 0)
            {
                if (zDiff > 0)
                    z = 135;
                else if (zDiff == 0)
                    z = 180;
                else if (zDiff < 0)
                    z = 225;
            }
            else if (xDiff == 0)
            {
                if (zDiff > 0)
                    z = 90;
                else if (zDiff < 0)
                    z = 270;
            }
            else //xDiff > 0
            {
                if (zDiff < 0)
                    z = 315;
                else if (zDiff == 0)
                    z = 0;
                else
                    z = 45;
            }

            shoePrintPath.transform.rotation = Quaternion.Euler(90, 0, z); //x is rotating to lie flat on tile

            //stretching the sprite a little for diagonal 
            Vector3 scale = shoePrintPath.transform.localScale; 
            scale.x -= x;
            shoePrintPath.transform.localScale = scale;

            shoePrintQueue.Enqueue(shoePrintPath);
        }
    }

    public void HidePath()
    {
        if (shoePrintQueue.Count > 0)
        {
            int count = shoePrintQueue.Count; //can't decrease count while using it
            
            for (int i = 0; i < count; i++)
            {
                movementSystem.AddToShoePrintPool(shoePrintQueue.Dequeue());
            }

            shoePrintQueue.Clear();
        }
    }
}
