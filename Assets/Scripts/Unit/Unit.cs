using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Unit : MonoBehaviour, ITurnDependent
{
    private int currentMovementPoints;
    public int CurrentMovementPoints { get { return currentMovementPoints; } set { currentMovementPoints = value; } }

    [SerializeField]
    private UnitDataSO unitDataSO;
    public UnitDataSO GetUnitData() => unitDataSO;

    [SerializeField]
    private UnitBuildDataSO buildDataSO;
    public UnitBuildDataSO GetBuildDataSO() => buildDataSO;

    [SerializeField]
    private AllUnitDataSO allUnitDataSO; //for storing multi-turn movement info and turn order info

    [HideInInspector]
    public MapWorld world;
    [HideInInspector]
    public UnityEvent FinishedMoving; //listeners are in BuildingManager (1) and UnitMovement (2)(3 total)

    //movement details
    [SerializeField]
    private float movementDuration = .5f, rotationDuration = 0.1f;
    private Queue<TerrainData> pathPositions = new();
    [HideInInspector]
    public bool moreToMove; //check if they have continued orders

    //selection indicators
    private SelectionHighlight highlight;
    private CameraController focusCam;
    [HideInInspector]
    public UIUnitTurnHandler turnHandler;

    [HideInInspector]
    public bool zeroMovementPoints, isTrader, atStop, followingRoute;


    private void Awake()
    {
        AwakeMethods();
    }

    private void Start()
    {
        AddUnitToDicts();
        if (!zeroMovementPoints)
            ResetMovementPoints();
    }

    protected virtual void AwakeMethods()
    {
        unitDataSO.MovementPointsCheck();
        turnHandler = FindObjectOfType<UIUnitTurnHandler>();
        focusCam = FindObjectOfType<CameraController>();
        world = FindObjectOfType<MapWorld>();
        highlight = GetComponent<SelectionHighlight>();
    }

    public void CenterCamera()
    {
        focusCam.followTransform = transform;
    }


    //Methods for moving unit
    private void ResetMovementPoints()
    {
        currentMovementPoints = GetUnitData().movementPoints;
        zeroMovementPoints = false;
    }

    public void ContinueMovementOrders()
    {
        List<TerrainData> continuedPath = allUnitDataSO.GetMovementOrders(this);

        if (continuedPath.Count > 0)
            MoveThroughPath(continuedPath);
    }

    public bool CanStillMove()
    {
        if (currentMovementPoints > 0)
            return true;
        return false;
    }

    //Gets the path positions and starts the coroutines
    public void MoveThroughPath(List<TerrainData> currentPath) 
    {
        CenterCamera(); //if you break focus before moving, focus again when moving

        if (currentMovementPoints > 0)
        {
            world.RemoveUnitPosition(transform.position/*, gameObject*/);//removing previous location

            pathPositions = new Queue<TerrainData>(currentPath);
            TerrainData firstTarget = pathPositions.Dequeue();

            StartCoroutine(RotationCoroutine(firstTarget));
        }
        else //if unit has orders but no movement points, turns the unit towards the direction
        {
            SetMovementOrders(this, currentPath);
            StartCoroutine(RotationCoroutine(currentPath[0]));
        }
    }


    //rotate unit before moving
    private IEnumerator RotationCoroutine(TerrainData endPositionTile)
    {
        Vector3 endPosition = endPositionTile.transform.position;

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
        if (currentMovementPoints > 0)
        {
            Vector3 startPosition = transform.position;
            int movementCost = endPositionTile.MovementCost;
            if (!world.GetTerrainDataAt(Vector3Int.FloorToInt(startPosition)).hasRoad) //for moving onto road from non-road
                movementCost = endPositionTile.OriginalMovementCost; 

            StartCoroutine(MovementCoroutine(startPosition, endPosition, movementCost));
        }
        else
        {
            FinishedMoving?.Invoke();
            turnHandler.ToggleInteractable(true);
            turnHandler.GoToNextUnit();
        }
    }



    //moves unit (using Lerp)
    private IEnumerator MovementCoroutine(Vector3 startPosition, Vector3 endPosition, int movementCost)
    {
        //Vector3 startPosition = transform.position;
        Vector3Int endLoc = Vector3Int.FloorToInt(endPosition);

        bool lastMove = false;

        if (pathPositions.Count == 0 || movementCost >= currentMovementPoints)
            lastMove = true;

        //checks if tile can still be moved to before moving there
        if ((lastMove && world.IsUnitLocationTaken(endLoc)) || (isTrader && !world.GetTerrainDataAt(endLoc).hasRoad))
        {
            FinishMoving(endPosition);
            yield break;
        }

        endPosition.y = startPosition.y;
        float timeElapsed = 0;

        while (timeElapsed < movementDuration)
        {
            timeElapsed += Time.deltaTime;
            float lerpStep = timeElapsed / movementDuration;
            transform.position = Vector3.Lerp(startPosition, endPosition, lerpStep);
            yield return null;
        }
        //transform.position = endPosition; //don't think this needs to be here
        currentMovementPoints -= movementCost;

        //Decisions based on how many movement points and path positions are left
        if (currentMovementPoints > 0)
        {
            if (pathPositions.Count > 0)
            {
                StartCoroutine(RotationCoroutine(pathPositions.Dequeue()));
            }
            else
            {   
                FinishMoving(endPosition);
            }
        }
        else
        {
            RemoveFromUnitList();
            FinishMoving(endPosition); //this may need to go back above previous statement for proper ordering
        }
    }

    private void FinishMoving(Vector3 endPosition)
    {
        TradeRouteCheck(endPosition);
        SetMovementOrders(this, pathPositions.ToList()); //sending empty list upon movement finish (need to be lists because Queues dequeue themselves inexplicably)
        moreToMove = pathPositions.Count > 0;
        world.AddUnitPosition(endPosition, gameObject);
        turnHandler.ToggleInteractable(true);
        pathPositions.Clear();
        FinishedMoving?.Invoke(); //turns on UIs again, hides worker one
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
                FinishMovement(); //lots of repetition here. 
                //routeManager.CompleteTradeRouteOrders();
            }
        }
    }






    //Methods for selecting and unselecting unit
    public void Select()
    {
        //selectionCircle.enabled = true;
        //highlight.ToggleGlow(true, Color.white);
        highlight.EnableHighlight(Color.white);
        CenterCamera();
    }

    public void Deselect()
    {
        //selectionCircle.enabled = false;
        //highlight.ToggleGlow(false, Color.white);
        highlight.DisableHighlight();
    }

    protected virtual void WaitTurnMethods() //so inherited classes can set up their own waitturn methods
    {
        ResetMovementPoints();
        if (CompareTag("Player")) //so enemies don't get added to lists
        {
            turnHandler.turnHandler.AddToTurnList(this);
        }
    }

    public void WaitTurn()
    {
        WaitTurnMethods();
    }

    public void FinishMovement() //Stops all movement, run when performing certain actions
    {
        RemoveFromUnitList();
        currentMovementPoints = 0;
        Deselect();
        turnHandler.GoToNextUnit();
        FinishedMoving?.Invoke();
    }

    public void FinishMovementNoNextUnit() //Same as FinishMovement, but doesn't go to next unit
    {
        RemoveFromUnitList();
        currentMovementPoints = 0;
        Deselect();
        FinishedMoving?.Invoke();
    }

    public void DestroyUnit()
    {
        RemoveUnitFromData();
        RemoveFromUnitList();
        currentMovementPoints = 0; //do this to remove workerUI screen

        FinishedMoving?.Invoke(); //Call this so that it doesn't move
        Destroy(gameObject);
    }

    private void RemoveFromUnitList()
    {
        if (CompareTag("Player")) //added this check so enemies don't break it
        {
            turnHandler.turnHandler.RemoveUnitFromTurnList(this); //removes from list
        }
    }



    //Methods for adding information to AllUnitSO
    private void AddUnitToDicts()
    {
        if (CompareTag("Player")) //do this to avoid adding enemy units
        {
            allUnitDataSO.SetMovementOrders(this, new List<TerrainData>());
            if (!zeroMovementPoints) //only add to list if it has movement points
                turnHandler.turnHandler.AddToTurnList(this);
        }
    }

    private void SetMovementOrders(Unit unit, List<TerrainData> movementOrders)
    {
        allUnitDataSO.SetMovementOrders(unit, movementOrders);
    }


    public (List<Vector3Int>, List<bool>, int) GetContinuedMovementPath() //displays movement orders when selected after using all movement points
    {
        List<TerrainData> continuedOrders = allUnitDataSO.GetMovementOrders(this);
        List<Vector3Int> continuedOrdersPositions = new();
        List<bool> newTurnList = new();
        int totalMovementCost = 0;
        int turnMovementCost = 0;
        int regMovementPoints = unitDataSO.movementPoints;

        foreach (TerrainData td in continuedOrders)
        {
            int tileCost = td.MovementCost; //for counting road as regular from non-road
            if (!td.hasRoad)
                tileCost = td.OriginalMovementCost;

            totalMovementCost += tileCost;
            turnMovementCost += tileCost;
            continuedOrdersPositions.Add(td.GetTileCoordinates());

            int diffMovementCost = regMovementPoints - turnMovementCost;

            if (diffMovementCost <= 0) //fixing movement cost if last move on turn puts movement points in negative
            {
                totalMovementCost += diffMovementCost;
                turnMovementCost = 0;
                newTurnList.Add(true);
                continue;
            }

            newTurnList.Add(false);
        }

        return (continuedOrdersPositions, newTurnList, totalMovementCost);
    }

    public void ResetMovementOrders()
    {
        allUnitDataSO.SetMovementOrders(this, new List<TerrainData>());
        moreToMove = false;
    }

    public bool MovementOrdersCheck()
    {
        return allUnitDataSO.MovementOrdersLengthRemaining(this) > 0;
    }

    private void RemoveUnitFromData()
    {
        allUnitDataSO.RemoveUnitFromDicts(this);
        turnHandler.turnHandler.RemoveUnitFromTurnList(this);
        world.RemoveUnitPosition(transform.position/*, gameObject*/);
    }
}
