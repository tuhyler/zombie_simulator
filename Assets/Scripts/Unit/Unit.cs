using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;

public class Unit : MonoBehaviour
{
    //[SerializeField]
    //private UnitDataSO unitDataSO;
    //public UnitDataSO GetUnitData() => unitDataSO;

    [SerializeField]
    public UnitBuildDataSO buildDataSO;

    //[SerializeField]
    //private GameObject minimapIcon;

    [SerializeField]
    private SpriteRenderer minimapIcon;
    
    [SerializeField]
    private ParticleSystem lightBeam;

    [SerializeField]
    private GameObject selectionCircle, mesh;

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
    [HideInInspector]
    public Vector3 finalDestinationLoc;
    public Vector3 FinalDestinationLoc { get { return finalDestinationLoc; } set { finalDestinationLoc = value; } }
    private Vector3Int currentLocation;
    public Vector3Int CurrentLocation { get { return CurrentLocation; } set { currentLocation = value; } }
    private Vector3Int prevRoadTile, prevTerrainTile; //first one is for traders, in case road they're on is removed
    private int flatlandSpeed, forestSpeed, hillSpeed, forestHillSpeed, roadSpeed;
    [HideInInspector]
    public Coroutine movingCo, waitingCo;
    private MovementSystem movementSystem;
    private Queue<GameObject> pathQueue = new();
    private int queueCount = 0;
    public int QueueCount { set { queueCount = value; } }
    private Vector3 shoePrintScale;
    private GameObject mapIcon;
    private WaitForSeconds moveInLinePause = new WaitForSeconds(0.5f);
    private bool onTop; //if on city tile and is on top

    //combat info
    [HideInInspector]
    public int currentHealth;

    //selection indicators
    private SelectionHighlight highlight;
    private CameraController focusCam;
    [HideInInspector]
    public UIUnitTurnHandler turnHandler;

    [HideInInspector]
    public bool bySea, isTrader, atStop, followingRoute, isWorker, isLaborer, isSelected, isWaiting, harvested, somethingToSay, sayingSomething;

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
        //turnHandler = FindObjectOfType<UIUnitTurnHandler>();
        //focusCam = FindObjectOfType<CameraController>();
        //world = FindObjectOfType<MapWorld>();
        //movementSystem = FindObjectOfType<MovementSystem>();
        highlight = GetComponent<SelectionHighlight>();
        unitAnimator = GetComponent<Animator>();
        isMovingHash = Animator.StringToHash("isMoving");
        //unitRigidbody = GetComponent<Rigidbody>();

        originalMoveSpeed = buildDataSO.movementSpeed;
        //mapIcon = world.CreateMapIcon(buildDataSO.mapIcon);
        bySea = buildDataSO.transportationType == TransportationType.Sea;
        shoePrintScale = GameAssets.Instance.shoePrintPrefab.transform.localScale;
        if (bySea)
            selectionCircle.SetActive(false);
        if (isTrader)
            prevRoadTile = Vector3Int.RoundToInt(transform.position); //world hasn't been initialized yet

        currentHealth = buildDataSO.health;
    }

    private void Start()
    {
        //turnHandler.turnHandler.AddToTurnList(this);
        //roadSpeed = world.GetRoadCost();

        Vector3 loc = transform.position;
        loc.y += 0.1f;
        lightBeam = Instantiate(lightBeam, loc, Quaternion.Euler(0,0,0));
        lightBeam.transform.parent = transform;
        lightBeam.Play();

        //world.SetMapIconLoc(world.RoundToInt(transform.position), mapIcon);
        //SetMinimapIcon();
        //Physics.IgnoreLayerCollision(8, 10);
    }

    public void SetReferences(MapWorld world, CameraController focusCam, UIUnitTurnHandler turnHandler, MovementSystem movementSystem)
    {
        this.world = world;
        this.focusCam = focusCam;
        this.turnHandler = turnHandler;
        this.movementSystem = movementSystem;

        turnHandler.turnHandler.AddToTurnList(this);
        //caching terrain costs
        roadSpeed = world.GetRoadCost();
        flatlandSpeed = world.flatland.movementCost;
        forestSpeed = world.forest.movementCost;
        hillSpeed = world.hill.movementCost;
        forestHillSpeed = world.forestHill.movementCost;
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
        SetInterruptedAnimation(false);
        InfoPopUpHandler.WarningMessage().Create(transform.position, "Route not possible to complete");
    }

    public void SetMinimapIcon(Transform parent)
    {
        minimapIcon.sprite = buildDataSO.mapIcon;
        ConstraintSource constraintSource = new();
        constraintSource.sourceTransform = parent;
        constraintSource.weight = 1;
        RotationConstraint ugh = minimapIcon.GetComponent<RotationConstraint>();
        ugh.rotationAtRest = new Vector3(90, 0, 0);
        ugh.rotationOffset = new Vector3(90, 0, 0);
        ugh.AddSource(constraintSource);
        
        //GameObject icon = Instantiate(minimapIcon);
        //minimapIcon.GetComponent<FollowNoRotate>().objectToFollow = transform;
        //world.AddToMinimap(minimapIcon.gameObject);
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
        if (followingRoute && world.IsUnitWaitingForSameStop(pathPositions.Peek(), finalDestinationLoc))
        {
            GetInLine();
            return;
        }

        Vector3 firstTarget = pathPositions.Dequeue();

        moreToMove = true;
        isMoving = true;
        //unitRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
        unitAnimator.SetBool(isMovingHash, true);
        movingCo = StartCoroutine(MovementCoroutine(firstTarget));
    }

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
        else if (td.terrainData.type == TerrainType.Hill)
        {
            if (endPositionInt.x % 3 == 0 && endPositionInt.z % 3 == 0)
                y = .5f;//world.test;
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

        //if (followingRoute && world.IsUnitWaitingForSameStop(endPositionInt, finalDestinationLoc))
        //{
        //    GetInLine(endPosition);
        //    yield break;
        //}
        if (world.IsUnitLocationTaken(endPositionInt)) //don't occupy sqaure if another unit is there
        {
            Unit unitInTheWay = world.GetUnit(endPositionInt);

            if (unitInTheWay.isBusy || unitInTheWay.followingRoute)
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
                InterruptRoute();
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

        //exploring
        if (isTrader && !bySea && !world.hideTerrain) {}
        else
        {
            Vector3Int pos = world.GetClosestTerrainLoc(transform.position);
            if (pos != prevTerrainTile)
                RevealCheck(pos);
        }

        if (onTop && world.GetTerrainDataAt(endPositionInt).gameObject.tag != "City")
        {
            Debug.Log("success!");
            onTop = false;
            mesh.layer = LayerMask.NameToLayer("Default");
        }
        //if (world.showingMap)
        //world.SetMapIconLoc(endPositionInt, mapIcon);

        if (pathPositions.Count > 0)
        {
            if (isTrader)
                prevRoadTile = world.RoundToInt(endPosition);

            if (followingRoute && world.IsUnitWaitingForSameStop(pathPositions.Peek(), finalDestinationLoc))
            {
                GetInLine();
                yield break;
            }

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

    private void GetInLine()
    {
        Vector3Int currentLoc = world.RoundToInt(transform.position);

        if (world.UnitAlreadyThere(this, currentLoc))
            currentLocation = currentLoc;
        else
        {
            if (world.IsUnitWaitingForSameStop(currentLoc, finalDestinationLoc))
            {
                GoToBackOfLine(world.RoundToInt(finalDestinationLoc), currentLoc);
                GetInLine();
                return;
            }

            currentLocation = world.AddUnitPosition(currentLoc, this);
        }
        isWaiting = true;
        movingCo = null;
        unitAnimator.SetBool(isMovingHash, false);

        Vector3Int tradePos = world.GetStopLocation(world.GetTradeLoc(world.RoundToInt(finalDestinationLoc)));

        if (world.IsCityOnTile(tradePos))
            world.GetCity(tradePos).AddToWaitList(this);
        else if (world.IsWonderOnTile(tradePos))
            world.GetWonder(tradePos).AddToWaitList(this);
        else if (world.IsTradeCenterOnTile(tradePos))
            world.GetTradeCenter(tradePos).AddToWaitList(this);
    }

    private void GoToBackOfLine(Vector3Int finalLoc, Vector3Int currentLoc)
    {
        List<Vector3Int> positionsToCheck = new(){ currentLoc };
        bool success = false;
        bool prevPath = true;
        List<Vector3Int> newPath = new();

        while (positionsToCheck.Count > 0)
        {
            Vector3Int current = positionsToCheck[0];
            positionsToCheck.Remove(current);
            newPath.Add(current);
            
            if (prevPath) //first check the tile the trader came from
            {
                prevPath = false;

                if (!world.IsUnitWaitingForSameStop(prevRoadTile, finalLoc))
                {
                    Teleport(prevRoadTile);
                    positionsToCheck.Clear();
                    success = true;
                }
                else
                {
                    positionsToCheck.Add(prevRoadTile);
                }
            }
            else
            {
                foreach (Vector3Int neighbor in world.GetNeighborsFor(current, MapWorld.State.EIGHTWAY))
                {
                    if (!bySea)
                    {
                        if (!world.IsRoadOnTileLocation(neighbor))
                            continue;
                    }
                    else
                    {
                        if (!world.CheckIfSeaPositionIsValid(neighbor))
                            continue;
                    }

                    if (world.IsUnitWaitingForSameStop(neighbor, finalLoc)) //going away from final loc
                    {
                        if (ManhattanDistance(finalLoc, current) < ManhattanDistance(finalLoc, neighbor))
                        {
                            positionsToCheck.Add(neighbor);
                            break;
                        }
                    }
                    else
                    {
                        //teleport to back of line
                        Teleport(neighbor);
                        positionsToCheck.Clear();
                        success = true;
                        break;
                    }
                }
            }
        }

        if (!success)
            InterruptRoute();
        else
        {
            List<Vector3Int> oldPath = new(pathPositions);
            newPath.Reverse();
            newPath.AddRange(oldPath);
            pathPositions = new Queue<Vector3Int>(newPath);
        }
    }

    public void InterruptRoute()
    {
        CancelRoute();
        interruptedRoute = true;
        if (isSelected)
            InterruptedRouteMessage();
        else
            SetInterruptedAnimation(true);
    }

    private int ManhattanDistance(Vector3Int endPos, Vector3Int point) //assigns priority for each tile, lower is higher priority
    {
        return Math.Abs(endPos.x - point.x) + Math.Abs(endPos.z - point.z);
    }

    public void ExitLine()
    {
        world.RemoveUnitPosition(currentLocation);
        unitAnimator.SetBool(isMovingHash, true);
        movingCo = StartCoroutine(MovementCoroutine(pathPositions.Dequeue()));
    }

    public IEnumerator MoveUpInLine(int placeInLine)
    {
        if (pathPositions.Count == 1)
            yield break;

        float pause = 0.5f * placeInLine;

        while (pause > 0)
        {
            yield return moveInLinePause;// new WaitForSeconds(0.5f * placeInLine);
            pause -= 0.5f;
        }

        if (world.IsUnitWaitingForSameStop(pathPositions.Peek(), finalDestinationLoc))
            yield break;

        Vector3Int nextSpot = pathPositions.Dequeue();
        world.RemoveUnitPosition(currentLocation);
        if (world.IsUnitLocationTaken(nextSpot))
        {
            Unit unitInTheWay = world.GetUnit(nextSpot);
            unitInTheWay.FindNewSpot(nextSpot, pathPositions.Peek());
        }
        world.AddUnitPosition(nextSpot, this);
        unitAnimator.SetBool(isMovingHash, true);
        movingCo = StartCoroutine(MovementCoroutine(nextSpot));
    }

    //public bool LineCutterCheck()
    //{
    //    if (world.IsUnitWaitingForSameStop(world.RoundToInt(transform.position), finalDestinationLoc))
    //    {
    //        CancelRoute();
    //        InfoPopUpHandler.WarningMessage().Create(transform.position, "No cutting in line");
    //        return true;
    //    }

    //    return false;
    //}

    private void FinishMoving(Vector3 endPosition)
    {
        //unitRigidbody.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation; 
        if (bySea && !isBeached)
            TurnOffRipples();
        queueCount = 0;
        moreToMove = false;
        isMoving = false;
        currentLocation = world.RoundToInt(transform.position);
        HidePath();
        pathPositions.Clear();
        unitAnimator.SetBool(isMovingHash, false);
        FinishedMoving?.Invoke();
        if (isTrader)
        {
            if (!bySea)
            {
                //if location has been taken away (such as when wonder finishes)
                if (!world.CheckIfPositionIsValid(world.GetClosestTerrainLoc(endPosition)))
                {
                    if (followingRoute)
                        InterruptRoute();
                    TeleportToNearestRoad(world.RoundToInt(currentLocation));
                    prevRoadTile = currentLocation;
                    return;
                }
            }

            if (followingRoute)
            {
                if (world.IsUnitWaitingForSameStop(currentLocation, finalDestinationLoc))
                {
                    GoToBackOfLine(world.RoundToInt(finalDestinationLoc), currentLocation);
                    GetInLine();
                    prevRoadTile = currentLocation;
                    return;
                }
            }

            prevRoadTile = currentLocation;            
        }
        if (world.IsUnitLocationTaken(currentLocation) && !followingRoute)
        {
            Unit unitInTheWay = world.GetUnit(currentLocation);
            if (unitInTheWay == this)
            {
                world.AddUnitPosition(currentLocation, this);
                TradeRouteCheck(endPosition);
                return; 
            }

            Vector3Int loc;
            if (unitInTheWay.pathPositions.Count > 0)
                loc = unitInTheWay.pathPositions.Peek();
            else
                loc = new Vector3Int(0, -10, 0);

            FindNewSpot(currentLocation, loc);
            return;
        }
        world.AddUnitPosition(currentLocation, this);
        TradeRouteCheck(endPosition);
    }

    public void FindNewSpot(Vector3Int current, Vector3Int next)
    {
        //Vector3Int lastTile = current;        

        int i = 0;
        bool outerRing = false;
        foreach (Vector3Int tile in world.GetNeighborsFor(current, MapWorld.State.EIGHTWAYTWODEEP))
        {
            i++;
            if (i > 8)
                outerRing = true;

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

            if (outerRing)
                MoveThroughPath(new List<Vector3Int> { (current + tile) / 2, tile });
            else
                MoveThroughPath(new List<Vector3Int> { tile });
            return;
        }

        //if (newSpotTry < 2)
        //{
        //    newSpotTry++;
        //    FindNewSpot(lastTile); //keep going until finding new spot
        //}
    }

    public void TeleportToNearestRoad(Vector3Int loc)
    {
        foreach (Vector3Int neighbor in world.GetNeighborsFor(loc, MapWorld.State.EIGHTWAY))
        {
            if (world.IsRoadOnTileLocation(neighbor))
                return;
        }

        Vector3Int newSpot = loc;
        Vector3Int terrainLoc = world.GetClosestTerrainLoc(loc);    

        foreach (Vector3Int neighbor in world.GetNeighborsFor(terrainLoc, MapWorld.State.EIGHTWAYINCREMENT))
        {
            if (world.CheckIfPositionIsValid(neighbor))
            {
                newSpot = neighbor;
                if (world.IsRoadOnTerrain(neighbor))
                {
                    Teleport(neighbor);
                    return;
                }
            }
        }

        Teleport(newSpot);
    }

    private void Teleport(Vector3Int loc)
    {
        currentLocation = loc;
        transform.position = loc;
        world.AddUnitPosition(currentLocation, this);
        transform.rotation = Quaternion.LookRotation(finalDestinationLoc - transform.position, Vector3.up);
    }

    public void Reveal()
    {
        Vector3Int pos = world.GetClosestTerrainLoc(transform.position);
        //prevTerrainTile = pos;
        TerrainData td = world.GetTerrainDataAt(pos);
        td.HardReveal();

        prevTerrainTile = pos;
        foreach (Vector3Int loc in world.GetNeighborsFor(pos, MapWorld.State.CITYRADIUS))
        {
            TerrainData td2 = world.GetTerrainDataAt(loc);
            if (td2.isDiscovered)
                continue;

            td2.HardReveal();
            focusCam.CheckLoc(loc);
            if (world.IsTradeCenterOnTile(loc))
                world.GetTradeCenter(loc).Reveal();
        }

        //RevealCheck(pos);
    }

    public void RevealCheck(Vector3Int pos)
    {
        prevTerrainTile = pos;
        
        foreach (Vector3Int loc in world.GetNeighborsFor(pos, MapWorld.State.CITYRADIUS))
        {
            TerrainData td = world.GetTerrainDataAt(loc);
            if (td.isDiscovered)
                continue;

            td.Reveal();
            focusCam.CheckLoc(loc);
            if (world.IsTradeCenterOnTile(loc))
                world.GetTradeCenter(loc).Reveal();
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

    //for animations
    public virtual void SetInterruptedAnimation(bool v)
    {

    }

    private void OnCollisionEnter(Collision collision)
    {    
        //if (collision.gameObject.CompareTag("City"))
        //{
        //    onTop = true;
        //    Debug.Log("worked");
        //    mesh.layer = LayerMask.NameToLayer("Agent");
        //}
        
        //if (isTrader && !bySea && !world.hideTerrain)
        //    return;

        //if (collision.transform.position != Vector3Int.zero)
        //    RevealCheck(world.GetTerrainDataAt(world.RoundToInt(collision.transform.position)).TileCoordinates);
    }

    //private void OnCollisionExit(Collision collision)
    //{
    //    if (onTop && !collision.gameObject.CompareTag("City"))
    //    {
    //        onTop = false;
    //        mesh.layer = LayerMask.NameToLayer("Default");
    //    }
    //}

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
        else if (collision.gameObject.CompareTag("City"))
        {
            moveSpeed = flatlandSpeed * .1f * originalMoveSpeed;
            unitAnimator.SetFloat("speed", originalMoveSpeed * 18f);
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
