using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MovementSystem : MonoBehaviour
{
    [HideInInspector]
    public UnitMovement unitMovement;
    [HideInInspector]
    public List<Vector3Int> currentPath = new();
    
    //for order queueing
    private Vector3 priorPath; //used for order queueing
    private bool orderQueueing;

    //for shoeprint path
    private Queue<GameObject> shoePrintQueue = new(); //the pool in object pooling
    private Queue<GameObject> chevronQueue = new();


    public void GetPathToMove(MapWorld world, Unit selectedUnit, Vector3Int startPosition, Vector3Int endPosition, bool isTrader) //Using AStar movement
    {
        Vector3 currentLoc = selectedUnit.transform.position;

        if (orderQueueing) //adding lists to each other for order queueing, turn counter starts at 1 each time
        {
            currentPath = GridSearch.AStarSearch(world, priorPath, endPosition, isTrader, selectedUnit.bySea);
            Vector3Int prevFinalSpot = world.RoundToInt(selectedUnit.finalDestinationLoc);
            selectedUnit.AddToMovementQueue(currentPath);

            if (currentPath.Count > 0)
            {
                if (selectedUnit.isPlayer)
                {
                    if (world.scottFollow && world.scott.isMoving)
                    {
                        Vector3Int scottPrevSpot = world.RoundToInt(world.scott.finalDestinationLoc);
                        List<Vector3Int> nextPath = SetFollowerPaths(world, currentPath, prevFinalSpot, world.scott);

                        if (world.azaiFollow)
                            SetFollowerPaths(world, nextPath, scottPrevSpot, world.azai);
                    }
                }
                else if (selectedUnit.trader)
                {
                    if (selectedUnit.trader.guarded)
                    {
                        List<Vector3Int> queuedGuardPath = GetGuardPath(world.RoundToInt(selectedUnit.finalDestinationLoc));
						selectedUnit.trader.guardUnit.finalDestinationLoc = queuedGuardPath[queuedGuardPath.Count - 1];
						selectedUnit.trader.guardUnit.AddToMovementQueue(queuedGuardPath);
                    }
                }
            }

            orderQueueing = false;
        }
        else
        {
            selectedUnit.QueueCount = 0;
            currentPath = GridSearch.AStarSearch(world, currentLoc, endPosition, isTrader, selectedUnit.bySea);

			if (startPosition == endPosition) //if moving within current square
                currentPath.Add(endPosition);
        }
    }

    private List<Vector3Int> SetFollowerPaths(MapWorld world, List<Vector3Int> path, Vector3Int prevSpot, Worker follower)
    {
		List<Vector3Int> followerPath = new(path);
		followerPath.RemoveAt(followerPath.Count - 1);
		Vector3Int newEnd;

		if (followerPath.Count > 0)
			newEnd = followerPath[followerPath.Count - 1];
		else
			newEnd = prevSpot;

		followerPath.Insert(0, prevSpot);
		follower.finalDestinationLoc = newEnd;
		follower.AddToMovementQueue(followerPath);

        return followerPath;
	}

    public void ResetFollowerPaths(MapWorld world)
    {
        if (currentPath.Count == 0)
            return;
        
        if (world.scottFollow)
        {
            List<Vector3Int> scottPath = new(currentPath);
            scottPath.RemoveAt(scottPath.Count - 1);
            scottPath.Insert(0, world.RoundToInt(world.mainPlayer.transform.position));
            world.scott.AddToMovementQueue(scottPath);

            if (world.azaiFollow)
            {
                scottPath.RemoveAt(scottPath.Count - 1);
                scottPath.Insert(0, world.RoundToInt(world.scott.transform.position));
                world.azai.AddToMovementQueue(scottPath);
            }
        }
    }

    public void AppendNewPath(Unit selectedUnit)
    {
        priorPath = selectedUnit.finalDestinationLoc;
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

    public List<Vector3Int> GetGuardPath(Vector3Int currentLoc)
    {
        List<Vector3Int> guardPath = new(currentPath);
        guardPath.Insert(0, currentLoc);
        guardPath.RemoveAt(guardPath.Count - 1);

        return guardPath;
	}

    public List<Vector3Int> GetFollowPath(Vector3Int currentLoc, Vector3Int currentLeaderLoc)
    {
        List<Vector3Int> sidePath = new();

        for (int i = 0; i < currentPath.Count; i++)
        {
            if (Mathf.Abs(currentPath[i].x - currentLoc.x) > 1 || Mathf.Abs(currentPath[i].z - currentLoc.z) > 1)
            {
                if (i == 0)
                    sidePath.Add(currentLeaderLoc);
                else
                    sidePath.Add(currentPath[i-1]);
            }
        }

        return sidePath;
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
