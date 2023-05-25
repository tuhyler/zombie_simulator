using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Unit : MonoBehaviour
{
    //[SerializeField]
    //private UnitDataSO unitDataSO;
    //public UnitDataSO GetUnitData() => unitDataSO;

    [SerializeField]
    public UnitBuildDataSO buildDataSO;
    
    [SerializeField]
    private ParticleSystem lightBeam;

    [SerializeField]
    private GameObject selectionCircle;

    [HideInInspector]
    public MapWorld world;
    [HideInInspector]
    public UnityEvent FinishedMoving; //listeners are worker tasks and show individualcity buttons

    //movement details
    //private Rigidbody unitRigidbody;
    private float rotationDuration = 0.2f, moveSpeed = 0.5f, originalMoveSpeed = 0.5f, threshold = 0.01f;
    private Queue<Vector3Int> pathPositions = new();
    [HideInInspector]
    public bool moreToMove, isBusy, isMoving, isBeached, interruptedRoute; //if there's more move orders, if they're doing something, if they're moving, if they're a boat on land, if route was interrupted unexpectedly
    private Vector3 destinationLoc;
    private Vector3 finalDestinationLoc;
    public Vector3 FinalDestinationLoc { get { return finalDestinationLoc; } set { finalDestinationLoc = value; } }
    private Vector3Int currentLocation;
    public Vector3Int CurrentLocation { get { return CurrentLocation; } set { currentLocation = value; } }
    private int flatlandSpeed, forestSpeed, hillSpeed, forestHillSpeed, roadSpeed;
    private Coroutine movingCo;
    private MovementSystem movementSystem;
    private Queue<GameObject> pathQueue = new();
    private int queueCount = 0;
    public int QueueCount { set { queueCount = value; } }
    private Vector3 shoePrintScale;

    //combat info
    [HideInInspector]
    public int currentHealth;

    //selection indicators
    private SelectionHighlight highlight;
    private CameraController focusCam;
    [HideInInspector]
    public UIUnitTurnHandler turnHandler;

    [HideInInspector]
    public bool bySea, isTrader, atStop, followingRoute, isWorker, isSelected, isWaiting, harvested, somethingToSay, sayingSomething;

    //animation
    [HideInInspector]
    public Animator unitAnimator;
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
        originalMoveSpeed = buildDataSO.movementSpeed;
        bySea = buildDataSO.transportationType == TransportationType.Sea;
        shoePrintScale = GameAssets.Instance.shoePrintPrefab.transform.localScale;
        if (bySea)
            selectionCircle.SetActive(false);

        currentHealth = buildDataSO.health;
    }

    private void Start()
    {
        turnHandler.turnHandler.AddToTurnList(this);
        roadSpeed = world.GetRoadCost();

        Vector3 loc = transform.position;
        loc.y += 0.1f;
        lightBeam = Instantiate(lightBeam, loc, Quaternion.Euler(0,0,0));
        lightBeam.transform.parent = transform;
        lightBeam.Play();
        //Physics.IgnoreLayerCollision(8, 10);
    }

    public void CenterCamera()
    {
        focusCam.followTransform = transform;
    }

    public void StopAnimation()
    {
        unitAnimator.SetBool(isMovingHash, false);
    }

    public void InterruptedRouteMessage()
    {
        interruptedRoute = false;
        InfoPopUpHandler.WarningMessage().Create(transform.position, "Route not possible to complete");
    }

    //Methods for moving unit
    //Gets the path positions and starts the coroutines
    public void MoveThroughPath(List<Vector3Int> currentPath) 
    {
        //CenterCamera(); //focus to start moving

        world.RemoveUnitPosition(currentLocation);//removing previous location

        //finalDestinationLoc = currentPath[currentPath.Count - 1].transform.position; //currentPath is list instead of queue for this line
        pathPositions = new Queue<Vector3Int>(currentPath);

        //ShowPath(currentPath);
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
        Vector3Int endPositionInt = world.RoundToInt(endPosition);
        TerrainData td = world.GetTerrainDataAt(endPositionInt);
        float y = 0f;

        if (bySea)
        {
            y = -.45f;

            if (isBeached)
            {
                isBeached = false;
            }
            else if (world.CheckIfCoastCoast(endPositionInt))
            {
                y = -.3f;
                TurnOffRipples();
                isBeached = true;
            }
        }
        else if (td.terrainData.isHill)
        {
            if (endPositionInt.x % 3 == 0)
                y = .4f;
            else
                y = .2f;
        }
        else if (td.IsSeaCorner) //walking the beach in rivers
        {
            if (world.CheckIfCoastCoast(endPositionInt))
                y = -.10f;
            else
                y = transform.position.y;
        }

        if (followingRoute && world.IsUnitWaitingForSameStop(endPositionInt, finalDestinationLoc))
        {
            GetInLine(endPosition);
        }
        else if (world.IsUnitLocationTaken(endPositionInt)) //don't occupy sqaure if another unit is there
        {
            Unit unitInTheWay = world.GetUnit(endPositionInt);

            if (unitInTheWay.isBusy)
            {
                if (isBusy)
                {
                    SkipRoadBuild();
                    if (isSelected)
                        td.DisableHighlight();

                    yield break;
                }
            }
            else
            {
                Vector3Int next;
                if (pathPositions.Count > 0)
                    next = pathPositions.Peek();
                else
                    next = new Vector3Int(0, -10, 0);
                unitInTheWay.FindNewSpot(endPositionInt, next);
            }
        }

        destinationLoc = endPosition;
        
        //checks if tile can still be moved to before moving there
        if (isTrader && !bySea && !world.IsRoadOnTileLocation(world.RoundToInt(endPosition)))
        {
            if (followingRoute)
            {
                CancelRoute();
                interruptedRoute = true;
                if (isSelected)
                    InterruptedRouteMessage();
            }
                

            FinishMoving(endPosition);
            yield break;
        }

        if (pathPositions.Count == 0)
            endPosition = finalDestinationLoc;

        Quaternion startRotation = transform.rotation;
        endPosition.y = y;
        Vector3 direction = endPosition - transform.position;
        Quaternion endRotation = Quaternion.LookRotation(direction, Vector3.up);
        float distance = 1f;
        float timeElapsed = 0;
        while (distance > threshold)
        //while (Math.Abs(transform.localPosition.x - endPosition.x) + Math.Abs(transform.localPosition.z - endPosition.z) > threshold)
        //while (Mathf.Pow(transform.localPosition.x - endPosition.x, 2) + Mathf.Pow(transform.localPosition.z - endPosition.z, 2) > threshold)
        {
            distance = Math.Abs(transform.localPosition.x - endPosition.x) + Math.Abs(transform.localPosition.z - endPosition.z);
            timeElapsed += Time.deltaTime;
            float movementThisFrame = Time.deltaTime * moveSpeed;
            float lerpStep = timeElapsed / rotationDuration; //Value between 0 and 1
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, endPosition, movementThisFrame);
            //transform.localPosition = newPosition;
            transform.rotation = Quaternion.Lerp(startRotation, endRotation, lerpStep);
            //Debug.Log("distance: " + distance);
            //Debug.Log("current: " + transform.localPosition);

            if (distance <= threshold)
                break;
            //if (Mathf.Pow(transform.localPosition.x - endPosition.x, 2) + Mathf.Pow(transform.localPosition.z - endPosition.z, 2) <= threshold)
            //    break;
            //if (Math.Abs(transform.localPosition.x - endPosition.x) + Math.Abs(transform.localPosition.z - endPosition.z) <= threshold)
            //    break;

            yield return null;
        } 

        if (pathPositions.Count > 0)
        {
            movingCo = StartCoroutine(MovementCoroutine(pathPositions.Dequeue()));
            if (pathQueue.Count > 0)
            {
                DequeuePath();
            }
        }
        else
        {
            FinishMoving(endPosition);
        }
    }

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
        queueCount++; 
        queuedOrders.ForEach(pos => pathPositions.Enqueue(pos));

        if (queueCount == 1)
        {
            List<Vector3Int> entirePath = new(pathPositions.ToList());
            ShowPath(entirePath, true);
        }
        else
        {
            ShowPath(queuedOrders, true);
        }
    }

    private void GetInLine(Vector3 endPosition)
    {
        movingCo = null;
        //Vector3Int endPos = world.RoundToInt(endPosition);
        Vector3Int tradePos = world.GetStopLocation(world.GetTradeLoc(world.RoundToInt(endPosition)));

        if (world.IsCityOnTile(tradePos))
            world.GetCity(tradePos).AddToWaitList(this);
        else if (world.IsWonderOnTile(tradePos))
            world.GetWonder(tradePos).AddToWaitList(this);
        else
            world.GetTradeCenter(tradePos).AddToWaitList(this);

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
        if (bySea && !isBeached)
            TurnOffRipples();
        queueCount = 0;
        moreToMove = false;
        isMoving = false;
        currentLocation = Vector3Int.RoundToInt(transform.position);
        HidePath();
        pathPositions.Clear();
        unitAnimator.SetBool(isMovingHash, false);
        FinishedMoving?.Invoke();
        if (world.IsUnitLocationTaken(currentLocation) && !followingRoute)
        {
            FindNewSpot(currentLocation, new Vector3Int(0, -10, 0));
            return;
        }
        world.AddUnitPosition(currentLocation, this);
        TradeRouteCheck(endPosition);
    }

    private void FindNewSpot(Vector3Int current, Vector3Int next)
    {
        //Vector3Int lastTile = current;        
        
        foreach (Vector3Int tile in world.GetNeighborsFor(current, MapWorld.State.EIGHTWAY))
        {
            if (isTrader && !world.IsRoadOnTileLocation(tile))
                continue;

            if (!world.CheckIfPositionIsValid(tile) || world.IsUnitLocationTaken(tile))
                continue;

            if (tile == next && tile != new Vector3Int(0, -10, 0)) //this just means null
                continue;

            //if (world.IsUnitLocationTaken(tile))
            //{
            //    //lastTile = tile;
            //    continue;
            //}

            finalDestinationLoc = tile;
            MoveThroughPath(new List<Vector3Int> { tile });
            return;
        }

        //if (newSpotTry < 2)
        //{
        //    newSpotTry++;
        //    FindNewSpot(lastTile); //keep going until finding new spot
        //}
    }

    //sees if trader is at trade route stop and has finished trade orders
    protected virtual void TradeRouteCheck(Vector3 endPosition)
    {
        
    }

    //sends trader to next stop
    public virtual void BeginNextStepInRoute()
    {

    }

    public virtual void CancelRoute()
    {

    }

    //for harvesting resource
    public virtual void SendResourceToCity()
    {

    }

    //if space is occupied with something
    public virtual void SkipRoadBuild()
    {

    }

    public virtual void TurnOffRipples()
    {

    }

    private void OnCollisionStay(Collision collision)
    {
        //Debug.Log("colliding with " + collision.gameObject.tag);
        
        //threshold = 0.001f;

        if (collision.gameObject.CompareTag("Road"))
        {
            moveSpeed = roadSpeed * .1f * originalMoveSpeed * 2.5f;
            unitAnimator.SetFloat("speed", originalMoveSpeed * 18f);
            threshold = 0.1f;
            //unitRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
        }
        else if (collision.gameObject.CompareTag("Flatland"))
        {
            moveSpeed = flatlandSpeed * .1f * originalMoveSpeed;
            unitAnimator.SetFloat("speed", originalMoveSpeed * 18f);
        }
        else if (collision.gameObject.CompareTag("Forest"))
        {
            moveSpeed = forestSpeed * .1f * originalMoveSpeed * 0.25f;
            unitAnimator.SetFloat("speed", originalMoveSpeed * 9f);
        }
        else if (collision.gameObject.CompareTag("Hill"))
        {
            moveSpeed = hillSpeed * .1f * originalMoveSpeed * 0.25f;
            unitAnimator.SetFloat("speed", originalMoveSpeed * 9f);
            threshold = 0.1f;
        }
        else if (collision.gameObject.CompareTag("Forest Hill"))
        {
            moveSpeed = forestHillSpeed * .1f * originalMoveSpeed * 0.125f;
            unitAnimator.SetFloat("speed", originalMoveSpeed * 5f);
        }
        else if (collision.gameObject.CompareTag("Water"))
        {
            if (bySea)
            {
                moveSpeed = flatlandSpeed * .1f * originalMoveSpeed;
                unitAnimator.SetFloat("speed", originalMoveSpeed * 12f);
            }
            else
            {
                moveSpeed = flatlandSpeed * .1f * originalMoveSpeed * 0.25f;
                unitAnimator.SetFloat("speed", originalMoveSpeed * 3f);
            }
        }
        else if (collision.gameObject.CompareTag("Swamp"))
        {
            moveSpeed = forestSpeed * .1f * originalMoveSpeed * .125f;
            unitAnimator.SetFloat("speed", originalMoveSpeed * 5f);
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
        if (bySea)
            selectionCircle.SetActive(true);
        highlight.EnableHighlight(Color.white);
        //CenterCamera();
    }

    public void Deselect()
    {
        //selectionCircle.enabled = false;
        //highlight.ToggleGlow(false, Color.white);
        isSelected = false;
        if (bySea)
            selectionCircle.SetActive(false); 
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
        queueCount = 1;
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
            turnCountPosition.y += 0.01f; 

            Vector3 prevPosition;

            if (i == 0)
            {
                if (queued && queueCount > 1)
                    prevPosition = world.RoundToInt(finalDestinationLoc);
                else
                    prevPosition = transform.position;
            }
            else
            {
                prevPosition = currentPath[i - 1];
            }

            prevPosition.y = 0.01f;
            GameObject path;

            if (bySea)
                path = movementSystem.GetFromChevronPool();
            else
                path = movementSystem.GetFromShoePrintPool();

            path.transform.position = (turnCountPosition + prevPosition) / 2;
            float xDiff = turnCountPosition.x - prevPosition.x;
            float zDiff = turnCountPosition.z - prevPosition.z;

            float x = 0;
            int z = 0;

            //if (Mathf.Abs(xDiff) + Mathf.Abs(zDiff) == 1)
            //{
            //    x = .2f;
            //}

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

            path.transform.rotation = Quaternion.Euler(90, 0, z); //x is rotating to lie flat on tile

            //squishing the sprite a little for straights
            Vector3 scale = path.transform.localScale; 
            scale.x += x;
            path.transform.localScale = scale;

            pathQueue.Enqueue(path);
        }
    }

    public void HidePath()
    {
        if (pathQueue.Count > 0)
        {
            int count = pathQueue.Count; //can't decrease count while using it
            
            for (int i = 0; i < count; i++)
            {
                DequeuePath();
            }

            pathQueue.Clear();
        }
    }

    private void DequeuePath()
    {
        GameObject path = pathQueue.Dequeue();
        path.transform.localScale = shoePrintScale;
        if (bySea)
            movementSystem.AddToChevronPool(path);
        else
            movementSystem.AddToShoePrintPool(path);
    }
}
