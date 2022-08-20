using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MovementSystem : MonoBehaviour
{
    private List<Vector3Int> currentPath = new();
    public int CurrentPathLength { get { return currentPath.Count; } }
    private List<bool> newTurnList = new();
    private int totalMovementCost;
    private int totalTurns;

    //for order queueing
    private List<Vector3Int> priorPath = new(); //used for order queueing
    private List<bool> priorTurnList = new();
    private int priorMovementCost;
    private bool orderQueueing;
    private int newCurrentMovePoints;

    //[SerializeField]
    //private GameObject turnCountPrefab;
    private Queue<GameObject> turnCountQueue = new(); //the pool in object pooling
    private List<GameObject> turnCounterList = new(); //gathering the turn counters to repool

    //[SerializeField]
    //private GameObject shoePrintPrefab;
    private Queue<GameObject> shoePrintQueue = new(); //the pool in object pooling
    private List<GameObject> shoePrintList = new(); //gathering the shoe prints to repool
    private Vector3 currentLoc;


    private void Awake()
    {
        GrowTurnCountPool(); //Grow pool of turn count icons
        GrowShoePrintPool();
    }

    public void ShowPathToMove(Unit selectedUnit) //for showing their continued orders when selected
    {
        totalTurns = 2;
        (currentPath, newTurnList, totalMovementCost) = selectedUnit.GetContinuedMovementPath();
        currentLoc = selectedUnit.transform.position;
        ShowPath();
    }

    public void GetPathToMove(MapWorld world, Unit selectedUnit, Vector3Int endPosition, bool isTrader) //Using AStar movement
    {
        totalMovementCost = 0; //reset each time
        totalTurns = 1; //number shown on each tile, reset each time
        int currentMovePoints = selectedUnit.CurrentMovementPoints;
        int regMovePoints = selectedUnit.GetUnitData().movementPoints;
        currentLoc = selectedUnit.transform.position;

        if (currentMovePoints <= 0) //added for making movement orders when movement points are gone
        {
            totalTurns++;
            currentMovePoints = 0;
        }

        if (orderQueueing) //adding lists to each other for order queueing, turn counter starts at 1 each time
        {
            newCurrentMovePoints = regMovePoints - ((priorMovementCost + (regMovePoints - currentMovePoints)) % regMovePoints); //adjusting if currentMovePoints is less than reg

            if (newCurrentMovePoints % regMovePoints > 0)
                priorTurnList[priorTurnList.Count - 1] = false; //have to set this to false now since the turn isn't over
            //Debug.Log("prior MC is " + priorMovementCost);
            //Debug.Log("current MP is " + newCurrentMovePoints);
            (currentPath, newTurnList, totalMovementCost) = GridSearch.AStarSearch(world, priorPath[priorPath.Count - 1], 
                endPosition, newCurrentMovePoints, regMovePoints, isTrader);
            priorPath.AddRange(currentPath);
            currentPath = new(priorPath);
            priorTurnList.AddRange(newTurnList);
            newTurnList = new(priorTurnList);
            totalMovementCost += priorMovementCost;
            orderQueueing = false;
        }
        else
            (currentPath, newTurnList, totalMovementCost) = GridSearch.AStarSearch(world, world.GetClosestTile(selectedUnit.transform.position), 
                endPosition, currentMovePoints, regMovePoints, isTrader);

        ShowPath();
    }

    private void ShowPath()
    {
        bool nextTurnNew = false; //to identify stopping points on path

        for (int i = 0; i < currentPath.Count; i++)
        {
            //position to place shoePrint
            Vector3 turnCountPosition = currentPath[i];
            turnCountPosition.y += 0.51f; //added .01 to make it not blend with terrain

            Vector3 prevPosition;

            if (i == 0)
            {
                prevPosition = currentLoc; 
            }
            else
            {
                prevPosition = currentPath[i - 1];
            }

            prevPosition.y = 0.51f;
            GameObject shoePrintPath = GetFromShoePrintPool();
            shoePrintPath.transform.position = (turnCountPosition + prevPosition) / 2;
            float xDiff = turnCountPosition.x - prevPosition.x;
            float zDiff = turnCountPosition.z - prevPosition.z;
            
            int z = 0;

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
            shoePrintList.Add(shoePrintPath);


            
            //Object pooling set up
            GameObject tempObject = GetFromTurnCountPool();
            tempObject.transform.position = turnCountPosition; //placing on center of tile
            tempObject.transform.rotation = Quaternion.Euler(90, 0, 0); //rotating to lie flat on tile
            MovementTurnCounter turnCounter = tempObject.GetComponent<MovementTurnCounter>(); //getting this class to add turn count
            turnCounterList.Add(turnCounter.gameObject);

            //setting turn count
            if (nextTurnNew) //this is first tile moved to on new turn
            {
                totalTurns++; //increment totalTurns once all move points have been used
                nextTurnNew = false;
            }

            turnCounter.SetTurnCount(totalTurns);

            //setting stopping points
            if (newTurnList[i]) //this is stopping tile for each turn
            {
                nextTurnNew = true; //next tile indicates change for the turn count
            }
        }
    }

    public void AppendNewPath()
    {
        if (currentPath.Count == 0)
            return;
        priorPath = new(currentPath);
        priorTurnList = new(newTurnList);
        priorMovementCost = totalMovementCost;
        orderQueueing = true;
    }

    public void HidePath(MapWorld world)
    {
        if (currentPath != null)
        {
            for (int i = 0; i < turnCounterList.Count; i++)
            {
                AddToTurnCountPool(turnCounterList[i]);
                AddToShoePrintPool(shoePrintList[i]);
            }
            
            turnCounterList.Clear();
            shoePrintList.Clear();
        }
    }

    public void ClearPaths()
    {
        //need to reset these two so they don't pass along through queued orders
        currentPath.Clear();
        newTurnList.Clear();
    }

    public void MoveUnit(Unit selectedUnit, MapWorld world)
    {        
        Debug.Log("Moving Unit " + selectedUnit.name);

        List<TerrainData> totalMovementList = new List<TerrainData>(); //the entire path

        foreach (Vector3Int pos in currentPath)
        {
            TerrainData td = world.GetTerrainDataAt(pos);
            totalMovementList.Add(td);
        }

        selectedUnit.MoveThroughPath(totalMovementList);
    }

    public float GetTotalMovementCost()
    {
        return totalMovementCost;
    }

    public void ResetTotalMovementCost() //Set this for memory saving purposes, but does it make a difference?
    {
        totalMovementCost = 0;
    }



    //Object pooling methods
    //pooling for turn count
    private void GrowTurnCountPool()
    {
        for (int i = 0; i < 10; i++) //grow pool 10 at a time
        {
            AddToTurnCountPool(Instantiate(GameAssets.Instance.turnCountPrefab));
        }
    }

    private void AddToTurnCountPool(GameObject gameObject)
    {
        gameObject.SetActive(false); //inactivate it when adding to pool
        turnCountQueue.Enqueue(gameObject);
    }

    private GameObject GetFromTurnCountPool()
    {
        if (turnCountQueue.Count == 0)
            GrowTurnCountPool();

        var turnCount = turnCountQueue.Dequeue();
        turnCount.SetActive(true);
        return turnCount;
    }



    //pooling for shoe prints
    private void GrowShoePrintPool()
    {
        for (int i = 0; i < 10; i++) //grow pool 10 at a time
        {
            AddToShoePrintPool(Instantiate(GameAssets.Instance.shoePrintPrefab));
        }
    }

    private void AddToShoePrintPool(GameObject gameObject)
    {
        gameObject.SetActive(false); //inactivate it when adding to pool
        shoePrintQueue.Enqueue(gameObject);
    }

    private GameObject GetFromShoePrintPool()
    {
        if (shoePrintQueue.Count == 0)
            GrowShoePrintPool();

        var shoePrint = shoePrintQueue.Dequeue();
        shoePrint.SetActive(true);
        return shoePrint;
    }
}
