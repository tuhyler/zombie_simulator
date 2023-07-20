using System.Collections.Generic;
using UnityEngine;

public class MovementSystem : MonoBehaviour
{
    private UnitMovement unitMovement;
    private List<Vector3Int> currentPath = new();
    
    //for order queueing
    private Vector3 priorPath; //used for order queueing
    private bool orderQueueing;

    //for shoeprint path
    private Queue<GameObject> shoePrintQueue = new(); //the pool in object pooling
    private Queue<GameObject> chevronQueue = new();
    //private List<GameObject> shoePrintList = new(); //gathering the shoe prints to repool
    private Vector3 currentLoc;


    public void GetPathToMove(MapWorld world, Unit selectedUnit, Vector3Int endPosition, bool isTrader) //Using AStar movement
    {
        currentLoc = selectedUnit.transform.position;

        if (orderQueueing) //adding lists to each other for order queueing, turn counter starts at 1 each time
        {
            currentPath = GridSearch.AStarSearch(world, priorPath, endPosition, isTrader, selectedUnit.bySea);
            selectedUnit.AddToMovementQueue(currentPath);
            orderQueueing = false;
        }
        else
        {
            selectedUnit.QueueCount = 0;
            currentPath = GridSearch.AStarSearch(world, currentLoc, endPosition, isTrader, selectedUnit.bySea);

            if (world.RoundToInt(currentLoc) == endPosition) //if moving within current square
                currentPath.Add(endPosition);
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

    public bool MoveUnit(Unit selectedUnit)
    {        
        if (currentPath.Count > 0)
        {
            selectedUnit.MoveThroughPath(currentPath);
            return true;
        }

        return false;
    }


    #region object pooling
    public void GrowObjectPools(UnitMovement unitMovement)
    {
        this.unitMovement = unitMovement;
        GrowShoePrintPool(); 
        GrowChevronPool();
    }

    //pooling for shoe prints
    private void GrowShoePrintPool()
    {
        for (int i = 0; i < 30; i++) //grow pool 30 at a time
        {
            GameObject print = Instantiate(GameAssets.Instance.shoePrintPrefab);
            print.gameObject.transform.SetParent(unitMovement.world.cityBuilderManager.objectPoolHolder, false);
            AddToShoePrintPool(print);
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
            GameObject chevron = Instantiate(GameAssets.Instance.chevronPrefab);
            chevron.gameObject.transform.SetParent(unitMovement.world.cityBuilderManager.objectPoolHolder, false);
            AddToChevronPool(chevron);
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
