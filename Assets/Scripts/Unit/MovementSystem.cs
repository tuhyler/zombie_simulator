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
    //private List<GameObject> shoePrintList = new(); //gathering the shoe prints to repool
    private Vector3 currentLoc;


    private void Awake()
    {
        GrowShoePrintPool();
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

        //ShowPath();
    }

    //public void ShowPathToMove(Unit selectedUnit) //for showing their continued orders when selected
    //{
    //    //currentPath = selectedUnit.GetContinuedMovementPath();
    //    //currentLoc = selectedUnit.transform.position;
    //    //ShowPath();
    //    selectedUnit.ShowPath();
    //}

    //private void ShowPath()
    //{
    //    for (int i = 0; i < currentPath.Count; i++)
    //    {
    //        //position to place shoePrint
    //        Vector3 turnCountPosition = currentPath[i];
    //        turnCountPosition.y += 0.01f; //added .01 to make it not blend with terrain

    //        Vector3 prevPosition;

    //        if (i == 0)
    //        {
    //            prevPosition = currentLoc; 
    //        }
    //        else
    //        {
    //            prevPosition = currentPath[i - 1];
    //        }

    //        prevPosition.y = 0.01f;
    //        GameObject shoePrintPath = GetFromShoePrintPool();
    //        shoePrintPath.transform.position = (turnCountPosition + prevPosition) / 2;
    //        float xDiff = turnCountPosition.x - prevPosition.x;
    //        float zDiff = turnCountPosition.z - prevPosition.z;
            
    //        int z = 0;

    //        //checking tile placements to see how to rotate shoe prints
    //        if (xDiff < 0)
    //        {
    //            if (zDiff > 0)
    //                z = 135;
    //            else if (zDiff == 0)
    //                z = 180;
    //            else if (zDiff < 0)
    //                z = 225;
    //        }
    //        else if (xDiff == 0)
    //        {
    //            if (zDiff > 0)
    //                z = 90;
    //            else if (zDiff < 0)
    //                z = 270;
    //        }
    //        else //xDiff > 0
    //        {
    //            if (zDiff < 0)
    //                z = 315;
    //            else if (zDiff == 0)
    //                z = 0;
    //            else
    //                z = 45;
    //        }

    //        shoePrintPath.transform.rotation = Quaternion.Euler(90, 0, z); //x is rotating to lie flat on tile
    //        shoePrintQueue.Add(shoePrintPath);
    //    }
    //}

    public void AppendNewPath(Unit selectedUnit)
    {
        priorPath = selectedUnit.FinalDestinationLoc;
        orderQueueing = true;
    }

    //public void HidePath()
    //{
    //    if (currentPath != null)
    //    {
    //        for (int i = 0; i < shoePrintQueue.Count; i++)
    //        {
    //            AddToShoePrintPool(shoePrintQueue[i]);
    //        }
            
    //        shoePrintQueue.Clear();
    //    }
    //}

    public void ClearPaths()
    {
        currentPath.Clear();
    }

    public void MoveUnit(Unit selectedUnit)
    {        
        //Debug.Log("Moving Unit " + selectedUnit.name);

        //List<TerrainData> totalMovementList = new(); //the entire path

        //foreach (Vector3Int pos in currentPath)
        //{
        //    TerrainData td = world.GetTerrainDataAt(pos);
        //    totalMovementList.Add(td);
        //}

        if (currentPath.Count > 0)
            selectedUnit.MoveThroughPath(currentPath);
    }


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
}
