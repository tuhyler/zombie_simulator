using System.Collections.Generic;
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


    public void GetPathToMove(MapWorld world, Unit selectedUnit, Vector3Int startPosition, Vector3Int endPosition, bool moveToSpeak) //Using AStar movement
    {
        if (orderQueueing) //adding lists to each other for order queueing, turn counter starts at 1 each time
        {
            if (moveToSpeak)
                currentPath = GridSearch.PlayerMoveExempt(world, priorPath, endPosition, world.GetExemptList(endPosition), true);
            else
                currentPath = GridSearch.PlayerMove(world, priorPath, endPosition, selectedUnit.bySea);

            Vector3Int prevFinalSpot = world.RoundToInt(selectedUnit.finalDestinationLoc);
            selectedUnit.AddToMovementQueue(currentPath);

            if (currentPath.Count > 0)
            {
                if (selectedUnit.isPlayer)
                {
                    if (world.scottFollow)
                    {
                        if (world.scott.isMoving)
                        {
                            Vector3Int scottPrevSpot = world.RoundToInt(world.scott.finalDestinationLoc);
                            List<Vector3Int> nextPath = SetFollowerPaths(currentPath, prevFinalSpot, world.scott);

                            if (world.azaiFollow)
                                SetFollowerPaths(nextPath, scottPrevSpot, world.azai);
                        }
                        else
                        {
                            selectedUnit.worker.firstStep = true;
                        }
                    }
                }
            }

            orderQueueing = false;
        }
        else
        {
            Vector3 currentLoc = selectedUnit.transform.position;
            selectedUnit.QueueCount = 0;

            if (moveToSpeak)
                currentPath = GridSearch.PlayerMoveExempt(world, currentLoc, endPosition, world.GetExemptList(endPosition), true);
            else
                currentPath = GridSearch.PlayerMove(world, currentLoc, endPosition, selectedUnit.bySea);

			if (startPosition == endPosition) //if moving within current square
                currentPath.Add(endPosition);
        }
    }

    private List<Vector3Int> SetFollowerPaths(List<Vector3Int> path, Vector3Int prevSpot, Unit follower)
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
            GameObject print = Instantiate(Resources.Load<GameObject>("Prefabs/InGameSpritePrefabs/ShoePrintHolder"));
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
            GameObject chevron = Instantiate(Resources.Load<GameObject>("Prefabs/InGameSpritePrefabs/ChevronHolder"));
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
