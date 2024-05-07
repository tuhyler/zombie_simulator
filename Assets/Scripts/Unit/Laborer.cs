using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VersionControl;
using UnityEngine;

public class Laborer : Unit
{
	[SerializeField]
	public GameObject boatMesh;

	//animations
	private int celebrateTime = 15;
    private int isCelebratingHash;
    private int isJumpingHash;
    [HideInInspector]
    public Coroutine co;
    [HideInInspector]
    public Vector3Int homeCityLoc;
    [HideInInspector]
    public int totalWait;
    [HideInInspector]
    public bool celebrating, atSea;

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

	private void Start()
	{
        if (!atSea)
            boatMesh.SetActive(false);
	}

	private IEnumerator Celebrate(bool load)
    {
        celebrating = true;
        unitAnimator.SetBool(isCelebratingHash, true);
        int randomWait = Random.Range(1, 4);
        int currentWait = 0;
        if (!load)
            totalWait = 0;

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

        celebrating = false;
        StopLaborAnimations();
        CheckDestination(homeCityLoc);
    }

    public void CheckDestination(Vector3Int destination)
    {
        if (world.IsCityOnTile(destination))
            GoToDestination(destination, false);
        else
			KillLaborer();
    }

    public void Transfer(List<Vector3Int> transferPath, bool atSea)
    {
        List<Vector3Int> path;

		if (atSea)
        {
			path = transferPath;
            bySea = true;
            unitMesh.SetActive(false);
            boatMesh.SetActive(true);
        }
        else
        {
            Vector3Int startingLoc = transferPath[0];
            transferPath.RemoveAt(0);

		    path = GridSearch.MilitaryMove(world, transform.position, startingLoc, false);
            path.AddRange(transferPath);
        }

        finalDestinationLoc = path[path.Count - 1];
        MoveThroughPath(path);
    }

    public void GoToDestination(Vector3Int loc, bool returnHome)
    {
        List<Vector3Int> pathHome;
		if (returnHome)
            pathHome = GridSearch.TraderMove(world, transform.position, loc, atSea);
        else
			pathHome = GridSearch.MilitaryMove(world, transform.position, loc, atSea);
		finalDestinationLoc = loc;
		MoveThroughPath(pathHome);
	}

    public void StartLaborAnimations(bool load, Vector3Int homeCityLoc)
    {
        this.homeCityLoc = homeCityLoc;
        co = StartCoroutine(Celebrate(load));
    }

    public void StopLaborAnimations()
    {
        unitAnimator.SetBool(isCelebratingHash, false);
        unitAnimator.SetBool(isJumpingHash, false);
    }

    public void FinishMovementLaborer()
    {
		world.unitMovement.LaborerJoin(this);
	}

    public void KillLaborer()
    {
		if (isSelected)
        {
            world.somethingSelected = false;
			world.unitMovement.ClearSelection();
        }

		StopMovementCheck(true);
		world.laborerList.Remove(this);
		StartCoroutine(WaitKillUnit());
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
		data.currentLocation = currentLocation;
		data.moveOrders = pathPositions.ToList();
		data.isMoving = isMoving;
        data.somethingToSay = somethingToSay;
        data.celebrating = celebrating;
        data.totalWait = totalWait;
        data.homeCityLoc = homeCityLoc;
        data.atSea = atSea;

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
		currentLocation = data.currentLocation;
		isMoving = data.isMoving;
        celebrating = data.celebrating;
        totalWait = data.totalWait;
        homeCityLoc = data.homeCityLoc;
        atSea = data.atSea;

		//if (!isMoving)
		//	world.AddUnitPosition(currentLocation, this);

		if (isMoving)
		{
			Vector3Int endPosition = world.RoundToInt(finalDestinationLoc);

			if (data.moveOrders.Count == 0)
				data.moveOrders.Add(endPosition);

			GameLoader.Instance.unitMoveOrders[this] = data.moveOrders;

            if (atSea)
            {
                bySea = true;
                ripples.SetActive(true);
                boatMesh.SetActive(true);
                unitMesh.SetActive(false);
            }
			//MoveThroughPath(data.moveOrders);
		}

        if (celebrating)
        {
            StartLaborAnimations(true, homeCityLoc);
        }
	}
}
