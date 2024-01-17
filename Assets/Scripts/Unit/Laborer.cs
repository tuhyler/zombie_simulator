using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Laborer : Unit
{
    //animations
    private int celebrateTime = 15;
    private int isCelebratingHash;
    private int isJumpingHash;
    [HideInInspector]
    public Coroutine co;

    private void Awake()
    {
        AwakeMethods();
        isLaborer = true;
        isCelebratingHash = Animator.StringToHash("isCelebrating");
        isJumpingHash = Animator.StringToHash("isJumping");
    }

    protected override void AwakeMethods()
    {
        base.AwakeMethods();
    }

    public IEnumerator Celebrate()
    {
        unitAnimator.SetBool(isCelebratingHash, true);
        int randomWait = Random.Range(1, 4);
        int currentWait = 0;
        int totalWait = 0;

        while (totalWait < celebrateTime)
        {
            yield return new WaitForSeconds(1);
            totalWait++;
            currentWait++;
            if (currentWait == randomWait)
            {
                unitAnimator.SetBool(isJumpingHash, true);
            }
            else if (currentWait > randomWait)
            {
                unitAnimator.SetBool(isJumpingHash, false);
                currentWait = 0;
                randomWait = Random.Range(0, 5);
            }
        }

        StopLaborAnimations();
    }

    public void StartLaborAnimations()
    {
        co = StartCoroutine(Celebrate());
    }

    public void StopLaborAnimations()
    {
        unitAnimator.SetBool(isCelebratingHash, false);
        unitAnimator.SetBool(isJumpingHash, false);
    }

	public LaborerData SaveLaborerData()
	{
		LaborerData data = new();

        data.unitName = gameObject.name;
		data.unitNameAndLevel = buildDataSO.unitNameAndLevel;
		data.secondaryPrefab = secondaryPrefab;
		data.position = transform.position;
		data.rotation = transform.rotation;
		data.destinationLoc = destinationLoc;
		data.finalDestinationLoc = finalDestinationLoc;
		data.currentLocation = CurrentLocation;
		data.prevTerrainTile = prevTerrainTile;
		data.moveOrders = pathPositions.ToList();
		data.isMoving = isMoving;
		data.moreToMove = moreToMove;
        data.somethingToSay = somethingToSay;
        data.conversationTopic = conversationTopic;

		return data;
	}

    public void LoadLaborerData(LaborerData data)
    {
        gameObject.name = data.unitName;
        secondaryPrefab = data.secondaryPrefab;
		transform.position = data.position;
		transform.rotation = data.rotation;
		destinationLoc = data.destinationLoc;
		finalDestinationLoc = data.finalDestinationLoc;
		CurrentLocation = data.currentLocation;
		prevTerrainTile = data.prevTerrainTile;
		isMoving = data.isMoving;
		moreToMove = data.moreToMove;

		if (!isMoving)
			world.AddUnitPosition(CurrentLocation, this);

		if (data.somethingToSay)
            SetSomethingToSay(data.conversationTopic);

		if (isMoving)
		{
			Vector3Int endPosition = world.RoundToInt(finalDestinationLoc);

			if (data.moveOrders.Count == 0)
				data.moveOrders.Add(endPosition);

			MoveThroughPath(data.moveOrders);
		}
	}
}
