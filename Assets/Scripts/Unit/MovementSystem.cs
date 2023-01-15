using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MovementSystem : MonoBehaviour
{
    private List<Vector3Int> currentPath = new();
    //public int CurrentPathLength { get { return currentPath.Count; } }

    //for order queueing
    private Vector3 priorPath; //used for order queueing
    private bool orderQueueing;

    //for shoeprint path
    private Queue<GameObject> shoePrintQueue = new(); //the pool in object pooling
    private Queue<GameObject> chevronQueue = new();
    //private List<GameObject> shoePrintList = new(); //gathering the shoe prints to repool
    private Vector3 currentLoc;


    private void Awake()
    {
        GrowShoePrintPool(); GrowChevronPool();
    }

    public void GetPathToMove(MapWorld world, Unit selectedUnit, Vector3Int endPosition, bool isTrader) //Using AStar movement
    {
        currentLoc = selectedUnit.transform.position;
        currentLoc.y = 0;

        if (orderQueueing) //adding lists to each other for order queueing, turn counter starts at 1 each time
        {
            currentPath = GridSearch.AStarSearch(world, priorPath, endPosition, isTrader, selectedUnit.bySea);
            selectedUnit.AddToMovementQueue(currentPath);
            orderQueueing = false;
        }
        else
        {
            currentPath = GridSearch.AStarSearch(world, currentLoc, endPosition, isTrader, selectedUnit.bySea);
        }
    }

    public void AppendNewPath(Unit selectedUnit)
    {
        priorPath = selectedUnit.FinalDestinationLoc;
        orderQueueing = true;
    }

    public void ClearPaths()
    {
        currentPath.Clear();
    }

    public void MoveUnit(Unit selectedUnit)
    {        
        if (currentPath.Count > 0)
            selectedUnit.MoveThroughPath(currentPath);
    }


    #region object pooling
    //pooling for shoe prints
    private void GrowShoePrintPool()
    {
        for (int i = 0; i < 30; i++) //grow pool 30 at a time
        {
            AddToShoePrintPool(Instantiate(GameAssets.Instance.shoePrintPrefab));
        }
    }

    public void AddToShoePrintPool(GameObject gameObject)
    {
        gameObject.SetActive(false); //inactivate it when adding to pool
        shoePrintQueue.Enqueue(gameObject);
    }

    public GameObject GetFromShoePrintPool()
    {
        if (shoePrintQueue.Count == 0)
            GrowShoePrintPool();

        var shoePrint = shoePrintQueue.Dequeue();
        shoePrint.SetActive(true);
        return shoePrint;
    }

    //pooling for chevrons
    private void GrowChevronPool()
    {
        for (int i = 0; i < 30; i++) //grow pool 30 at a time
        {
            AddToChevronPool(Instantiate(GameAssets.Instance.chevronPrefab));
        }
    }

    public void AddToChevronPool(GameObject gameObject)
    {
        gameObject.SetActive(false); //inactivate it when adding to pool
        chevronQueue.Enqueue(gameObject);
    }

    public GameObject GetFromChevronPool()
    {
        if (chevronQueue.Count == 0)
            GrowChevronPool();

        var chevron = chevronQueue.Dequeue();
        chevron.SetActive(true);
        return chevron;
    }

    #endregion 
}
