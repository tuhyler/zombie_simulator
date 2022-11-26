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
    public UnityEvent FinishedMoving; //listeners are worker tasks and show individualcity buttons

    //movement details
    //private Rigidbody unitRigidbody;
    private float rotationDuration = 0.2f, moveSpeed = 0.5f, originalMoveSpeed = 0.5f, threshold = 0.005f;
    private Queue<Vector3Int> pathPositions = new();
    [HideInInspector]
    public bool moreToMove, isBusy, isMoving; //check if they're doing something
    private Vector3 destinationLoc;
    private Vector3 finalDestinationLoc;
    public Vector3 FinalDestinationLoc { get { return finalDestinationLoc; } set { finalDestinationLoc = value; } }
    private Vector3Int currentLocation;
    public Vector3Int CurrentLocation { get { return CurrentLocation; } set { currentLocation = value; } }
    private int flatlandSpeed, forestSpeed, hillSpeed, forestHillSpeed, roadSpeed, newSpotTry;
    private Coroutine movingCo;
    private MovementSystem movementSystem;
    private Queue<GameObject> shoePrintQueue = new();
    private Vector3 shoePrintScale;

    //selection indicators
    private SelectionHighlight highlight;
    private CameraController focusCam;
    [HideInInspector]
    public UIUnitTurnHandler turnHandler;

    [HideInInspector]
    public bool bySea, isTrader, atStop, followingRoute, isWorker, isSelected, isWaiting, harvested;

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
        //unitRigidbody = GetComponent<Rigidbody>();

        //caching terrain costs
        flatlandSpeed = world.flatland.movementCost;
        forestSpeed = world.forest.movementCost;
        hillSpeed = world.hill.movementCost;
        forestHillSpeed = world.forestHill.movementCost;
        originalMoveSpeed = unitDataSO.movementSpeed;
        bySea = unitDataSO.transportationType == TransportationType.Sea;
        shoePrintScale = GameAssets.Instance.shoePrintPrefab.transform.localScale;
    }

    private void Start()
    {
        turnHandler.turnHandler.AddToTurnList(this);
        roadSpeed = world.GetRoadCost();

        //Physics.IgnoreLayerCollision(8, 10);
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

        world.RemoveUnitPosition(currentLocation);//removing previous location

        //finalDestinationLoc = currentPath[currentPath.Count - 1].transform.position; //currentPath is list instead of queue for this line
        pathPositions = new Queue<Vector3Int>(currentPath);

        ShowPath(currentPath);
        Vector3 firstTarget = pathPositions.Dequeue();

        moreToMove = true;
        isMoving = true;
        //unitRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
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
        if (/*(pathPositions.Count == 0 && world.IsUnitLocationTaken(endLoc)) || */(isTrader && !bySea && !world.IsRoadOnTileLocation(Vector3Int.RoundToInt(endPosition))))
        {
            FinishMoving(endPosition);
            yield break;
        }

        if (pathPositions.Count == 0)
            endPosition = finalDestinationLoc;

        //endPosition.y = 0f; //fixed y position

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
            //Debug.Log("current location: " + Mathf.Pow(transform.localPosition.x - endPosition.x, 2) + Mathf.Pow(transform.localPosition.z - endPosition.z, 2));

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
            Vector3Int nextStep = pathPositions.Peek();
            
            if (followingRoute && world.IsUnitWaitingForSameCity(nextStep, finalDestinationLoc))
            {
                GetInLine(endPosition);
            }
            else if (pathPositions.Count == 1 && !followingRoute && world.IsUnitLocationTaken(nextStep)) //don't occupy sqaure if another unit is there
            {
                FinishMoving(endPosition);
            }
            else
            {
                movingCo = StartCoroutine(MovementCoroutine(pathPositions.Dequeue()));
                if (shoePrintQueue.Count > 0)
                {
                    DequeueShoePrint();
                }
            }
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
    //private IEnumerator SlideUnit(Vector3 endPosition)
    //{
    //    newSpotTry = 0;
    //    Quaternion startRotation = transform.rotation;
    //    endPosition.y = transform.position.y;
    //    Vector3 direction = endPosition - transform.position;
    //    Quaternion endRotation = Quaternion.LookRotation(direction, Vector3.up);

    //    float timeElapsed = 0;
    //    while (Mathf.Pow(transform.localPosition.x - endPosition.x, 2) + Mathf.Pow(transform.localPosition.z - endPosition.z, 2) > threshold)
    //    {
    //        timeElapsed += Time.deltaTime;
    //        float movementThisFrame = Time.deltaTime * moveSpeed;
    //        float lerpStep = timeElapsed / rotationDuration; //Value between 0 and 1
    //        transform.localPosition = Vector3.MoveTowards(transform.localPosition, endPosition, movementThisFrame);
    //        //transform.localPosition = newPosition;
    //        transform.rotation = Quaternion.Lerp(startRotation, endRotation, lerpStep);

    //        if (Mathf.Pow(transform.localPosition.x - endPosition.x, 2) + Mathf.Pow(transform.localPosition.z - endPosition.z, 2) <= threshold)
    //            break;

    //        yield return null;
    //    }

    //    FinishMoving(endPosition);
    //}


    public void StopMovement()
    {
        if (isMoving)
        {
            if (movingCo != null)
                StopCoroutine(movingCo);

            if (isMoving) //check here twice in case still moving after stopping coroutine
                FinishMoving(destinationLoc);
        }
    }

    public void ShiftMovement()
    {
        StopAllCoroutines();
        HidePath();
        pathPositions.Clear();
    }

    public void AddToMovementQueue(List<Vector3Int> queuedOrders)
    {
        queuedOrders.ForEach(pos => pathPositions.Enqueue(pos));
        ShowPath(queuedOrders, true);
    }

    private void GetInLine(Vector3 endPosition)
    {
        movingCo = null;
        world.GetCity(Vector3Int.RoundToInt(finalDestinationLoc)).AddToWaitList(this);
        currentLocation = world.AddUnitPosition(endPosition, this);
        isWaiting = true;
        unitAnimator.SetBool(isMovingHash, false);
    }

    public void MoveUpInLine()
    {
        world.RemoveUnitPosition(currentLocation);//removing previous location
        //unitRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
        unitAnimator.SetBool(isMovingHash, true);
        movingCo = StartCoroutine(MovementCoroutine(pathPositions.Dequeue()));
    }

    private void FinishMoving(Vector3 endPosition)
    {
        //unitRigidbody.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation; 

        moreToMove = false;
        isMoving = false;
        currentLocation = Vector3Int.RoundToInt(transform.position);
        HidePath();
        pathPositions.Clear();
        unitAnimator.SetBool(isMovingHash, false);
        FinishedMoving?.Invoke();
        TradeRouteCheck(endPosition);
        if (world.IsUnitLocationTaken(currentLocation) && !followingRoute)
        {
            FindNewSpot(currentLocation);
            return;
        }
        world.AddUnitPosition(currentLocation, this);
    }

    private void FindNewSpot(Vector3Int current)
    {
        Vector3Int lastTile = current;        
        
        foreach (Vector3Int tile in world.GetNeighborsFor(current, MapWorld.State.EIGHTWAY))
        {
            if (isTrader && !world.IsRoadOnTileLocation(tile))
                continue;

            if (world.IsUnitLocationTaken(tile))
            {
                lastTile = tile;
                continue;
            }

            MoveThroughPath(new List<Vector3Int> { tile });
            return;
        }

        if (newSpotTry < 2)
        {
            newSpotTry++;
            FindNewSpot(lastTile); //keep going until finding new spot
        }
    }

    //sees if trader is at trade route stop and has finished trade orders
    protected virtual void TradeRouteCheck(Vector3 endPosition)
    {
        
    }

    //sends trader to next stop
    public virtual void BeginNextStepInRoute()
    {

    }

    //for harvesting resource
    public virtual void SendResourceToCity()
    {

    }

    private void OnCollisionStay(Collision collision)
    {
        //Debug.Log("colliding with " + collision.gameObject.tag);
        
        threshold = 0.001f;


        if (collision.gameObject.CompareTag("Road"))
        {
            moveSpeed = (roadSpeed / 10f) * originalMoveSpeed * 3f;
            unitAnimator.SetFloat("speed", originalMoveSpeed * 16f);
            //unitRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
        }
        else if (collision.gameObject.CompareTag("Flatland"))
        {
            moveSpeed = (flatlandSpeed / 10f) * originalMoveSpeed;
            unitAnimator.SetFloat("speed", originalMoveSpeed * 8f);
        }
        else if (collision.gameObject.CompareTag("Forest"))
        {
            moveSpeed = (forestSpeed / 10f) * originalMoveSpeed * 0.25f;
            unitAnimator.SetFloat("speed", originalMoveSpeed * 4f);
        }
        else if (collision.gameObject.CompareTag("Hill"))
        {
            moveSpeed = (hillSpeed / 10f) * originalMoveSpeed * 0.25f;
            unitAnimator.SetFloat("speed", originalMoveSpeed * 4f);
            threshold = 0.05f;
        }
        else if (collision.gameObject.CompareTag("Forest Hill"))
        {
            moveSpeed = (forestHillSpeed / 10f) * originalMoveSpeed * 0.125f;
            unitAnimator.SetFloat("speed", originalMoveSpeed * 2f);
        }
        else if (collision.gameObject.CompareTag("Water"))
        {
            moveSpeed = (flatlandSpeed / 10f) * originalMoveSpeed;
            unitAnimator.SetFloat("speed", originalMoveSpeed * 8f);
        }
        //else if (collision.gameObject.CompareTag("Player"))
        //{
        //    unitRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
        //}
    }

    //private void OnCollisionExit(Collision collision)
    //{
    //    if (collision.gameObject.CompareTag("Player"))
    //    {
    //        unitRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
    //    }
    //}

    //public void CheckForSpotAvailability()
    //{

    //}



    //Methods for selecting and unselecting unit
    public void Select()
    {
        //selectionCircle.enabled = true;
        //highlight.ToggleGlow(true, Color.white);
        isSelected = true;
        highlight.EnableHighlight(Color.white);
        //CenterCamera();
    }

    public void Deselect()
    {
        //selectionCircle.enabled = false;
        //highlight.ToggleGlow(false, Color.white);
        isSelected = false;
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
        world.RemoveUnitPosition(currentLocation);
    }

    public void ShowContinuedPath()
    {
        ShowPath(new List<Vector3Int>(pathPositions));
    }

    private void ShowPath(List<Vector3Int> currentPath, bool queued = false)
    {
        //List<Vector3Int> currentPath = new(pathPositions);
        if (!isSelected)
            return;

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

            //squishing the sprite a little for straights
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
                DequeueShoePrint();
            }

            shoePrintQueue.Clear();
        }
    }

    private void DequeueShoePrint()
    {
        GameObject shoePrint = shoePrintQueue.Dequeue();
        shoePrint.transform.localScale = shoePrintScale;
        movementSystem.AddToShoePrintPool(shoePrint);
    }
}

public enum TransportationType
{
    Land,
    Sea,
}
