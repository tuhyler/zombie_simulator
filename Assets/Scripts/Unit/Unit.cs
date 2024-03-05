using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using static UnityEngine.UI.CanvasScaler;

[System.Serializable]
public class Unit : MonoBehaviour
{
	[HideInInspector]
	public int id;

	[SerializeField]
    public UnitBuildDataSO buildDataSO;

    [SerializeField]
    public SkinnedMeshRenderer unitMesh;

    [SerializeField]
    public SpriteRenderer minimapIcon;

    [SerializeField]
    public ParticleSystem lightBeam;

    [SerializeField]
    public GameObject selectionCircle, questionMark, exclamationPoint, ripples;

    [SerializeField]
    public UnitMarker marker;

    [SerializeField]
    public Healthbar healthbar;

    [SerializeField]
    public AudioClip[] greetings, attacks, kills;

    [HideInInspector]
    public AudioSource audioSource;

    [HideInInspector]
    public MapWorld world;
    [HideInInspector]
    public UnityEvent FinishedMoving; //listeners are worker tasks and show individualcity buttons
    [HideInInspector]
    public Trader trader;
    [HideInInspector]
    public Worker worker;
	[HideInInspector]
	public Military military;

	//movement details
	[HideInInspector]
    public Rigidbody unitRigidbody;
    [HideInInspector]
    public float rotationDuration = 0.2f, moveSpeed = 0.5f, originalMoveSpeed = 0.5f, threshold = 0.01f;
    [HideInInspector]
    public Queue<Vector3Int> pathPositions = new();
    [HideInInspector]
    public bool moreToMove, isBusy, isMoving, isBeached, secondaryPrefab, readyToMarch = true; 
    [HideInInspector]
    public Vector3 destinationLoc;
    [HideInInspector]
    public Vector3 finalDestinationLoc;
    [HideInInspector]
    public Vector3Int currentLocation;
    //public Vector3Int CurrentLocation { get { return currentLocation; } set { currentLocation = value; } }
    [HideInInspector]
    public Vector3Int prevTile, prevTerrainTile, ambushLoc, lastClearTile; //second one for traders is in case road they're on is removed
    private int flatlandSpeed, forestSpeed, hillSpeed, forestHillSpeed, roadSpeed;
    [HideInInspector]
    public Coroutine movingCo;

    //showing footprints
    [HideInInspector]
    public Queue<GameObject> pathQueue = new();
    private int queueCount = 0;
    public int QueueCount { set { queueCount = value; } }

    //combat info
    [HideInInspector]
    public int currentHealth;
    [HideInInspector]
    public BasicEnemyAI enemyAI;
    [HideInInspector]
    public float baseSpeed;
    [HideInInspector]
    public int healthMax;
    [HideInInspector]
    public WaitForSeconds[] attackPauses = new WaitForSeconds[3];

    //selection indicators
    private SelectionHighlight highlight;
    
    [HideInInspector]
    public bool bySea, isTrader, isPlayer, isLaborer, isSelected, isWaiting, harvested, harvestedForest, somethingToSay, sayingSomething, firstStep;

    [HideInInspector]
    public bool inArmy, isDead, runningAway, ambush, hidden, isUpgrading;

    //animation
    [HideInInspector]
    public Animator unitAnimator;
    private int isMovingHash, isMarchingHash, isAttackingHash;

    private void Awake()
    {
        AwakeMethods();
    }

	protected virtual void AwakeMethods()
    {
        audioSource = GetComponent<AudioSource>();
        highlight = GetComponent<SelectionHighlight>();
        if (highlight == null)
            highlight = GetComponentInChildren<SelectionHighlight>();
        unitAnimator = GetComponent<Animator>();
        isMovingHash = Animator.StringToHash("isMoving");
        isMarchingHash = Animator.StringToHash("isMarching");
        isAttackingHash = Animator.StringToHash("isAttacking");
        attackPauses[0] = new WaitForSeconds(1f);
		attackPauses[1] = new WaitForSeconds(.125f);
        //attackPauses[2] = new WaitForSeconds(1.1f);
        attackPauses[2] = new WaitForSeconds(.25f);
		unitRigidbody = GetComponent<Rigidbody>();
        baseSpeed = 1;

        originalMoveSpeed = buildDataSO.movementSpeed;
        bySea = buildDataSO.transportationType == TransportationType.Sea;
		if (!bySea)
			marker.Unit = this;

		healthMax = buildDataSO.health;
        currentHealth = healthMax;
        inArmy = buildDataSO.baseAttackStrength > 0 && CompareTag("Player");

        enemyAI = GetComponent<BasicEnemyAI>();
        healthbar.SetUnit(this);

        if (bySea)
            selectionCircle.SetActive(false);
        
        prevTile = Vector3Int.RoundToInt(transform.position); //world hasn't been initialized yet

        if (currentHealth == healthMax)
            healthbar.gameObject.SetActive(false);
    }

	private void SetParticleSystems()
    {
		Vector3 loc = transform.position;

        if (world.IsRoadOnTileLocation(world.RoundToInt(loc)))
		    loc.y += 0.17f;
		else
			loc.y += 0.07f;

		lightBeam = Instantiate(lightBeam, loc, Quaternion.Euler(0, 0, 0));
		lightBeam.transform.parent = world.psHolder;

		if (CompareTag("Player"))
			lightBeam.Play();
	}

	public void SetReferences(MapWorld world)
    {
        this.world = world;
		world.CheckUnitPermanentChanges(this);

        //caching terrain costs
        roadSpeed = world.GetRoadCost();
        flatlandSpeed = world.flatland.movementCost;
        forestSpeed = world.forest.movementCost;
        hillSpeed = world.hill.movementCost;
        forestHillSpeed = world.forestHill.movementCost;
        SetParticleSystems();
    }

    public void CenterCamera()
    {
        world.cameraController.followTransform = transform;
    }

    public void SayHello()
    {
        audioSource.clip = greetings[0];// [UnityEngine.Random.Range(0, greetings.Length)];
        audioSource.Play();
    }

	public void StartAttackingAnimation()
	{
		unitAnimator.SetBool(isAttackingHash, true);
		unitAnimator.SetFloat("attackSpeed", 1);
	}

	public void StopAnimation()
    {
        if (military && (military.isMarching || military.isGuarding))
            unitAnimator.SetBool(isMarchingHash, false);
        else
            unitAnimator.SetBool(isMovingHash, false);
    
        if (military)
            unitAnimator.SetBool(isAttackingHash, false);
    }

    public void StopAttackAnimation()
    {
		unitAnimator.SetBool(isAttackingHash, false);
	}

    public void SetMinimapIcon(Transform parent)
    {
        minimapIcon.sprite = buildDataSO.mapIcon;
        ConstraintSource constraintSource = new();
        constraintSource.sourceTransform = parent; //so never rotates
        constraintSource.weight = 1;
        RotationConstraint rotation = minimapIcon.GetComponent<RotationConstraint>();
		rotation.rotationAtRest = new Vector3(90, 0, 0);
		rotation.rotationOffset = new Vector3(90, 0, 0);
		rotation.AddSource(constraintSource);
        minimapIcon.gameObject.SetActive(true);
    }

    public void PillageSound()
    {
        audioSource.clip = attacks[UnityEngine.Random.Range(0, attacks.Length)];
        audioSource.Play();
	}

    //taking damage
    public void ReduceHealth(Unit attackingUnit, AudioClip audio)
    {
        audioSource.clip = audio;
        audioSource.Play();
        
        if (isDead)
            return;

		healthbar.gameObject.SetActive(true);

        currentHealth -= (attackingUnit.military.attackStrength - 1 + UnityEngine.Random.Range(0, 3));

        if (currentHealth <= 0)
        {
			if (military)
                military.StopAttacking();
            if (!attackingUnit.military.aoe)
                attackingUnit.military.attacking = false;
            KillUnit(attackingUnit.transform.eulerAngles);
            return;
        }
            //DestroyUnit();

        if (isSelected)
            world.unitMovement.infoManager.SetHealth(currentHealth, healthMax);

        healthbar.SetHealthLevel(currentHealth);
    }

    public void UpdateHealth(float healthLevel)
    {
        currentHealth = Mathf.FloorToInt(healthLevel * healthMax);
        //this.currentHealth = currentHealth;

        if (isSelected)
			world.unitMovement.infoManager.SetHealth(currentHealth, healthMax);
	}

    public void StartAnimation()
    {
        if (military && (military.isMarching || military.isGuarding))
            unitAnimator.SetBool(isMarchingHash, true);
        else
            unitAnimator.SetBool(isMovingHash, true);
    }

	//Methods for moving unit
    public void RestartPath(Vector3Int tile)
    {
        movingCo = StartCoroutine(MovementCoroutine(tile));
    }

	//Gets the path positions and starts the coroutines
	public void MoveThroughPath(List<Vector3Int> currentPath) 
    {
        if (isDead)
            return;

        world.RemoveUnitPosition(currentLocation);//removing previous location

        pathPositions = new Queue<Vector3Int>(currentPath);

        if (trader)
        {
		    if (trader.followingRoute)
            {                
                if (world.IsUnitWaitingForSameStop(pathPositions.Peek(), finalDestinationLoc))
			    {
                    moreToMove = true;
                    isMoving = true;
                    trader.GetInLine();
				    return;
			    }
            }
            else
            {
				Vector3Int terrainLoc = world.GetClosestTerrainLoc(currentLocation);
				if (bySea)
                {
					if (world.IsCityHarborOnTile(terrainLoc))
                        world.GetHarborCity(terrainLoc).tradersHere.Remove(this);
                }
                else
                {
                    if (world.IsCityOnTile(terrainLoc))
                        world.GetCity(terrainLoc).tradersHere.Remove(this);
                }
            }
        }

		Vector3 firstTarget = pathPositions.Dequeue();

        moreToMove = true;
        isMoving = true;
        StartAnimation();
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
        else if (td.isHill)
        {
            if (endPositionInt.x % 3 == 0 && endPositionInt.z % 3 == 0)
                y = 0.4f;//world.test;
            else
                y = 0.2f;
        }

		if (world.IsRoadOnTileLocation(endPositionInt))
		{
			y += .1f;
		}

		if (pathPositions.Count == 0 && !enemyAI && !inArmy && world.IsUnitLocationTaken(endPositionInt)) //don't occupy sqaure if another unit is there
        {
            Unit unitInTheWay = world.GetUnit(endPositionInt);

            if (this == world.mainPlayer && unitInTheWay.somethingToSay)
            {
				world.unitMovement.QuickSelect(this);
				unitInTheWay.worker.SpeakingCheck();
                FinishMoving(transform.position);
				yield break;
            }

            if (!unitInTheWay.isBusy && !(unitInTheWay.trader && unitInTheWay.trader.followingRoute) && !unitInTheWay.military && !(unitInTheWay.worker && unitInTheWay.worker.gathering))
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
        if (trader && !bySea && !world.IsRoadOnTileLocation(world.RoundToInt(endPosition)))
        {
            if (trader.followingRoute)
            {
                trader.InterruptRoute();
            }
                
            FinishMoving(transform.position);
            yield break;
        }

        if (pathPositions.Count == 0)
            endPosition = finalDestinationLoc;

        Quaternion startRotation = transform.rotation;
        endPosition.y = y;
        Vector3 direction = endPosition - transform.position;
        direction.y = 0;

        Quaternion endRotation;
        if (direction == Vector3.zero)
            endRotation = Quaternion.identity;
        else
            endRotation = Quaternion.LookRotation(direction, Vector3.up);


        float distance = 1f;
        float timeElapsed = 0;

        while (distance > threshold)
        {
            distance = Math.Abs(transform.localPosition.x - endPosition.x) + Math.Abs(transform.localPosition.z - endPosition.z);
            timeElapsed += Time.deltaTime;
            float movementThisFrame = Time.deltaTime * moveSpeed;
            float lerpStep = timeElapsed / rotationDuration; //Value between 0 and 1
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, endPosition, movementThisFrame);
            transform.rotation = Quaternion.Lerp(startRotation, endRotation, lerpStep);

            if (distance <= threshold)
                break;

            yield return null;
        }

        if (isPlayer)
        {
			Vector3Int pos = world.GetClosestTerrainLoc(transform.position);
			if (pos != prevTerrainTile)
            {
				RevealCheck(pos, false);

                if (!world.azaiFollow && !runningAway && CampAggroCheck(pos))
                {
                    if (isBusy)
                        world.unitMovement.workerTaskManager.ForceCancelWorkerTask();

                    if (!world.mapHandler.activeStatus && Camera.main.WorldToViewportPoint(transform.position).z >= 0)
                        world.cityBuilderManager.PlayWarningAudio();

					isBusy = true;
                    world.AddUnitPosition(transform.position, this);
					StopPlayer();
					currentLocation = world.RoundToInt(transform.position);

					yield return null;
				}
            }

			if (firstStep && (Mathf.Abs(transform.position.x - world.scott.transform.position.x) > 1.2f || Mathf.Abs(transform.position.z - world.scott.transform.position.z) > 1.2f))
			{
				firstStep = false;
				if (!isBusy)
					world.unitMovement.HandleSelectedFollowerLoc(pathPositions, prevTile, world.RoundToInt(endPosition), world.RoundToInt(finalDestinationLoc));
				else if (runningAway)
					world.unitMovement.HandleSelectedFollowerLoc(pathPositions, prevTile, world.RoundToInt(endPosition), world.RoundToInt(finalDestinationLoc));
			}
		}

		//making sure army is all in line
		if (military && military.isMarching)
        {
            readyToMarch = false;
            bool close = pathPositions.Count == 2;

            if (enemyAI)
				military.enemyCamp.UnitNextStep(close, endPositionInt);
            else
                military.homeBase.army.UnitNextStep(close);

			unitAnimator.SetBool(isMarchingHash, false);
			while (!readyToMarch)
            {
                yield return null; //waiting for others to arrive
            }

			unitAnimator.SetBool(isMarchingHash, true);
		}

		if (pathPositions.Count > 0)
        {
            if (trader)
            {
                if (trader.followingRoute && world.IsUnitWaitingForSameStop(pathPositions.Peek(), finalDestinationLoc))
                {
                    trader.GetInLine();
                    yield break;
                }

				prevTile = world.RoundToInt(endPosition);
				if (prevTile == ambushLoc) //prevTile is a misnomer, asking if current tile is ambushLoc. Can also trigger ambush when walking
                {
                    ambush = true;
                    world.AddUnitPosition(prevTile, this);
                    currentLocation = prevTile;
                    StopAnimation();

                    world.SetUpAmbush(ambushLoc, this);
                    yield break;
                }
            }
            else
            {
                prevTile = world.RoundToInt(endPosition);
            }

            if (pathPositions.Count == 1)
            {
                //for azai's final step
                if (buildDataSO.unitDisplayName == "Azai" && !world.mainPlayer.isBusy)
                {
                    NextToCheck();
                }
                else if (ambush)
                {
                    StopAnimation();
                    isMoving = false;
                    moreToMove = false;
                    finalDestinationLoc = prevTile;
                    currentLocation = prevTile;
                    world.AddUnitPosition(currentLocation, this);

                    if (enemyAI)
                        enemyAI.StartAttack(world.GetUnit(pathPositions.Dequeue()));
					else
						military.StartAttack(world.GetUnit(pathPositions.Dequeue()));

					yield break;
                }
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
            {
                StopCoroutine(movingCo);
            }

            StopAnimation();

            if (isMoving) //check here twice in case still moving after stopping coroutine
                FinishMoving(destinationLoc);
        }
        else if (trader)
        {
			Vector3Int terrainLoc = world.GetClosestTerrainLoc(currentLocation);
			if (bySea)
            {
                if (world.IsCityHarborOnTile(terrainLoc))
					world.GetHarborCity(terrainLoc).tradersHere.Remove(this);
            }
            else
            {
				if (world.IsCityOnTile(terrainLoc))
					world.GetCity(terrainLoc).tradersHere.Remove(this);
			}
        }

    }

    public void NextToCheck()
    {
		Vector3Int targetArea = pathPositions.Dequeue();
		Vector3Int diff = world.RoundToInt(world.mainPlayer.transform.position) - targetArea;
		List<Vector3Int> potentialAreas = new();

		if (diff.x != 0 && diff.z != 0)
		{
			potentialAreas.Add(targetArea + new Vector3Int(diff.x, 0, 0));
			potentialAreas.Add(targetArea + new Vector3Int(0, 0, diff.z));
		}
		else if (diff.x != 0)
		{
			potentialAreas.Add(targetArea + new Vector3Int(0, 0, diff.x));
			potentialAreas.Add(targetArea + new Vector3Int(0, 0, -diff.x));
		}
		else if (diff.z != 0)
		{
			potentialAreas.Add(targetArea + new Vector3Int(diff.z, 0, 0));
			potentialAreas.Add(targetArea + new Vector3Int(-diff.z, 0, 0));
		}

		potentialAreas.Add(prevTile);
		Vector3Int closestLoc = prevTile;

		bool firstOne = true;
		int dist = 0;
		for (int i = 0; i < potentialAreas.Count; i++)
		{
			if (!world.CheckIfPositionIsValid(potentialAreas[i]))
				continue;

			if (firstOne)
			{
				firstOne = false;
				dist = Math.Abs(prevTile.x - potentialAreas[i].x) + Math.Abs(prevTile.z - potentialAreas[i].z);
				closestLoc = potentialAreas[i];
				continue;
			}

			int newDist = Math.Abs(prevTile.x - potentialAreas[i].x) + Math.Abs(prevTile.z - potentialAreas[i].z);
			if (newDist < dist)
				closestLoc = potentialAreas[i];

			break;
		}

        finalDestinationLoc = closestLoc;
		pathPositions.Enqueue(closestLoc);
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

    public void Rotate(Vector3 lookAtTarget)
    {
        StartCoroutine(RotateTowardsPosition(lookAtTarget));
    }

	public IEnumerator RotateTowardsPosition(Vector3 lookAtTarget)
	{
        Vector3 direction = lookAtTarget - transform.position;
        Quaternion endRotation;
		if (direction == Vector3.zero)
			endRotation = Quaternion.identity;
		else
			endRotation = Quaternion.LookRotation(direction, Vector3.up);

		float totalTime = 0;
		while (totalTime < 0.35f)
		{
			float timePassed = Time.deltaTime;
			transform.rotation = Quaternion.Lerp(transform.rotation, endRotation, timePassed * 12);
			totalTime += timePassed;
			yield return null;
		}
	}

    public void FinishMoving(Vector3 endPosition)
    {
        if (bySea && !isBeached)
            TurnOffRipples();
        queueCount = 0;
        moreToMove = false;
        isMoving = false;
        currentLocation = world.RoundToInt(transform.position);
        HidePath();
        pathPositions.Clear();
        StopAnimation();
        FinishedMoving?.Invoke();
        prevTile = currentLocation;

        if (isDead)
        {
            return;
        }
        else if (trader)
        {
            trader.FinishMovementTrader(endPosition);
        }
        else if (inArmy)
        {
            military.FinishMovementMilitary(endPosition);
        }
        else if (enemyAI)
        {
            military.FinishMovementEnemyMilitary();
        }
        else if (world.IsUnitLocationTaken(currentLocation) && !(trader && trader.followingRoute))
		{
			Unit unitInTheWay = world.GetUnit(currentLocation);

			if (unitInTheWay == this)
			{
				world.AddUnitPosition(currentLocation, this);
				TradeRouteCheck(endPosition);
				return;
			}
            else if (unitInTheWay == world.mainPlayer && buildDataSO.characterUnit)
            {
                return;
            }

			Vector3Int loc;
			if (unitInTheWay.pathPositions.Count > 0)
				loc = unitInTheWay.pathPositions.Peek();
			else
				loc = new Vector3Int(0, -10, 0);

			FindNewSpot(currentLocation, loc);
		}
        else
        {
			if (world.tutorialGoing)
            {
                if (isPlayer)
                    world.TutorialCheck("Finished Movement");
            }
			world.AddUnitPosition(currentLocation, this);
            
            if (isSelected)
            {
                world.unitMovement.ShowIndividualCityButtonsUI();

				if (isPlayer)
                {
                    Vector3Int terrainLoc = world.GetClosestTerrainLoc(currentLocation);

					if (world.IsTradeLocOnTile(terrainLoc) && !world.IsWonderOnTile(terrainLoc))
					        world.unitMovement.uiWorkerTask.uiLoadUnload.ToggleInteractable(true);
                }
			}

            if (isLaborer)
                world.unitMovement.LaborerJoin(this);
        }
    }

    public void FindNewSpot(Vector3Int current, Vector3Int? next)
    {
        //Vector3Int lastTile = current;        
        if (isBusy || (trader && trader.followingRoute) || inArmy || enemyAI)
            return;

        int i = 0;
        bool outerRing = false;
        foreach (Vector3Int tile in world.GetNeighborsFor(current, MapWorld.State.EIGHTWAYTWODEEP))
        {
            i++;
            if (i > 8)
                outerRing = true;

            if (trader && !world.IsRoadOnTileLocation(tile))
                continue;

            if (!world.CheckIfPositionIsValid(tile) || world.IsUnitLocationTaken(tile))
                continue;

            if (tile == next && tile != null) 
                continue;

            finalDestinationLoc = tile;

            if (outerRing)
                MoveThroughPath(new List<Vector3Int> { (current + tile) / 2, tile });
            else
                MoveThroughPath(new List<Vector3Int> { tile });
            return;
        }
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

    public void Teleport(Vector3Int loc)
    {
        currentLocation = loc;
        transform.position = loc;
        world.AddUnitPosition(currentLocation, this);
    }

    public void Reveal()
    {
        if (!CompareTag("Player"))
            return;
        
        Vector3Int pos = world.GetClosestTerrainLoc(transform.position);
        TerrainData td = world.GetTerrainDataAt(pos);
        td.HardReveal();

        prevTerrainTile = pos;
        foreach (Vector3Int loc in world.GetNeighborsFor(pos, MapWorld.State.CITYRADIUS))
        {
            TerrainData td2 = world.GetTerrainDataAt(loc);
            if (td2.isDiscovered)
                continue;

            td2.HardReveal();
            world.cameraController.CheckLoc(loc);
            if (world.IsTradeCenterOnTile(loc))
                world.GetTradeCenter(loc).Reveal();
            else if (world.IsEnemyCityOnTile(loc))
                world.enemyCityDict[loc].gameObject.SetActive(true);
        }
    }

    public void RevealCheck(Vector3Int pos, bool military)
    {
        prevTerrainTile = pos;

        List<Vector3Int> tilesToCheck;

		if (military)
            tilesToCheck = world.GetNeighborsFor(pos, MapWorld.State.EIGHTWAYINCREMENT);
        else
			tilesToCheck = world.GetNeighborsFor(pos, MapWorld.State.CITYRADIUS);

        for (int i = 0; i < tilesToCheck.Count; i++)
        {
			TerrainData td = world.GetTerrainDataAt(tilesToCheck[i]);
			if (!bySea)
			{
				if (td.enemyCamp)
				{
					if (world.enemyCityDict.ContainsKey(tilesToCheck[i]))
					{
						if (!td.isDiscovered)
						{
							world.enemyCityDict[tilesToCheck[i]].RevealEnemyCity();

							foreach (Vector3Int tile in world.GetNeighborsFor(tilesToCheck[i], MapWorld.State.EIGHTWAYINCREMENT))
							{
								TerrainData td2 = world.GetTerrainDataAt(tile);

								if (world.IsRoadOnTerrain(tile))
									world.SetRoadActive(tile);

								if (world.cityImprovementDict.ContainsKey(tile))
								{
									CityImprovement improvement = world.cityImprovementDict[tile];
									improvement.HideImprovement();

									if (!td2.isDiscovered)
										improvement.StartJustWorkAnimation();
								}

								if (!td2.isDiscovered)
									td2.Reveal();
							}
						}
					}
					else
					{
						if (!td.isDiscovered)
							world.RevealEnemyCamp(tilesToCheck[i]);
					}
				}
			}

			if (td.isDiscovered)
				continue;

			if (world.IsRoadOnTerrain(tilesToCheck[i]))
				world.SetRoadActive(tilesToCheck[i]);

			if (world.cityImprovementDict.ContainsKey(tilesToCheck[i]))
				world.cityImprovementDict[tilesToCheck[i]].RevealImprovement();

			td.Reveal();
			world.cameraController.CheckLoc(tilesToCheck[i]);
			if (world.IsTradeCenterOnTile(tilesToCheck[i]))
				world.GetTradeCenter(tilesToCheck[i]).Reveal();

		}
    }

    private void CheckPrevTile()
    {
        TerrainData td = world.GetTerrainDataAt(lastClearTile);
        td.ToggleTransparentForest(false);
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

    public virtual void TurnOffRipples()
    {

    }

    //for animations
    public virtual void SetInterruptedAnimation(bool v)
    {

    }

    private void OnCollisionEnter(Collision collision)
    {
        //Debug.Log(collision.gameObject.tag);

        if (enemyAI)
        {
            TerrainData td = collision.gameObject.GetComponent<TerrainData>();
            if (td == null)
            {
				marker.ToggleVisibility(false);
				UnhideUnit();
			}
			else if (!td.isDiscovered)
            {
                HideUnit(false);
            }
            else if (collision.gameObject.CompareTag("Forest") || collision.gameObject.CompareTag("Forest Hill") || collision.gameObject.CompareTag("City"))
			{
				marker.ToggleVisibility(true);
				UnhideUnit();
			}
			else
			{
				marker.ToggleVisibility(false);
				UnhideUnit();
			}
		}
        else if (!bySea)
        {
            Vector3Int loc = world.RoundToInt(collision.gameObject.transform.position);
            TerrainData td = world.GetTerrainDataAt(loc);

            if (td.treeHandler != null || world.IsCityOnTile(loc))
            //if (collision.gameObject.CompareTag("Forest") || collision.gameObject.CompareTag("Forest Hill") || world.IsCityOnTile(loc))
            {
                marker.ToggleVisibility(true);

				if (isPlayer)
				{
					//TerrainData td = collision.gameObject.GetComponent<TerrainData>();

                    CheckPrevTile();
					td.ToggleTransparentForest(true);
                    lastClearTile = td.TileCoordinates;
				}
			}
			else
            {
                marker.ToggleVisibility(false);

                if (isPlayer)
                {
                    CheckPrevTile();
                }
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        //Debug.Log("colliding with " + collision.gameObject.tag);

        //threshold = 0.001f;

        if (military)
        {
            if (military.isMarching)
            {
                moveSpeed = baseSpeed * flatlandSpeed * .05f;
                unitAnimator.SetFloat("speed", baseSpeed * 18f);
                return;
            }
        }

        if (collision.gameObject.CompareTag("Road"))
        {
            moveSpeed = baseSpeed * roadSpeed * originalMoveSpeed * .25f;
            unitAnimator.SetFloat("speed", baseSpeed * originalMoveSpeed * 18f);
            threshold = 0.1f;
            //unitRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
        }
        else if (collision.gameObject.CompareTag("City"))
        {
            moveSpeed = baseSpeed * flatlandSpeed * originalMoveSpeed * .1f;
            unitAnimator.SetFloat("speed", baseSpeed * originalMoveSpeed * 18f);
        }
        else if (collision.gameObject.CompareTag("Flatland"))
        {
            moveSpeed = baseSpeed * flatlandSpeed * originalMoveSpeed * .1f;
            unitAnimator.SetFloat("speed", baseSpeed * originalMoveSpeed * 18f);
        }
        else if (collision.gameObject.CompareTag("Forest"))
        {
            moveSpeed = baseSpeed * forestSpeed * originalMoveSpeed * 0.025f;
            unitAnimator.SetFloat("speed", baseSpeed * originalMoveSpeed * 9f);
        }
        else if (collision.gameObject.CompareTag("Hill"))
        {
            moveSpeed = baseSpeed * hillSpeed * originalMoveSpeed * 0.025f;
            unitAnimator.SetFloat("speed", baseSpeed * originalMoveSpeed * 9f);
            threshold = 0.1f;
        }
        else if (collision.gameObject.CompareTag("Forest Hill"))
        {
            moveSpeed = baseSpeed * forestHillSpeed * originalMoveSpeed * 0.02f;
            unitAnimator.SetFloat("speed", baseSpeed * originalMoveSpeed * 5f);
		}
		else if (collision.gameObject.CompareTag("Water"))
        {
            if (bySea)
            {
                moveSpeed = baseSpeed * flatlandSpeed * originalMoveSpeed * .1f;
                unitAnimator.SetFloat("speed", baseSpeed * originalMoveSpeed * 12f);
            }
            else
            {
                moveSpeed = baseSpeed * flatlandSpeed * originalMoveSpeed * 0.025f;
                unitAnimator.SetFloat("speed", baseSpeed * originalMoveSpeed * 3f);
            }
        }
        else if (collision.gameObject.CompareTag("Swamp"))
        {
            moveSpeed = baseSpeed * forestSpeed * originalMoveSpeed * .0125f;
            unitAnimator.SetFloat("speed", baseSpeed * originalMoveSpeed * 5f);
        }
    }

    private bool CampAggroCheck(Vector3Int loc)
    {
        foreach (Vector3Int tile in world.GetNeighborsFor(loc, MapWorld.State.CITYRADIUS))
        {
            if (world.CheckIfEnemyCamp(tile))
            {
                world.GetEnemyCamp(tile).StartChase(loc);
                return true;
            }
        }

        return false;
    }

    public void StopPlayer()
    {
		if (isMoving)
		{
			StopAnimation();
			ShiftMovement();
			ResetMovementOrders();
		}

		if (world.scott.isMoving)
		{
			world.scott.StopAnimation();
			world.scott.ShiftMovement();
			world.scott.ResetMovementOrders();
		}

		if (world.azai.isMoving)
		{
			world.azai.StopAnimation();
			world.azai.ShiftMovement();
			world.azai.ResetMovementOrders();
		}
	}

    public void StepAside(Vector3Int playerLoc, List<Vector3Int> route)
    {
        Vector3Int safeTarget = playerLoc;

        foreach (Vector3Int tile in world.GetNeighborsFor(playerLoc, MapWorld.State.EIGHTWAYINCREMENT))
        {
            if (route != null && route.Contains(tile))
                continue;
            
            if (world.CheckIfPositionIsValid(tile))
            {
                safeTarget = tile;
                break;
            }
        }
        
        finalDestinationLoc = safeTarget;
		firstStep = true;
		List<Vector3Int> runAwayPath = GridSearch.AStarSearch(world, currentLocation, safeTarget, isTrader, bySea);

		//in case already there
		if (runAwayPath.Count > 0)
			MoveThroughPath(runAwayPath);
	}

	public void StartRunningAway()
    {
        if (!runningAway)
        {
            exclamationPoint.SetActive(true);
            runningAway = true;
            StartCoroutine(RunAway());
        }    
    }

	private IEnumerator RunAway()
    {
        //have to do the two following just in case
        pathPositions.Clear();
        isMoving = false;
        yield return new WaitForSeconds(1);

        //finding closest city
        Vector3Int safeTarget = world.startingLoc;

        bool firstOne = true;
        int dist = 0;
        foreach (City city in world.cityDict.Values)
        {
            if (firstOne)
            {
                firstOne = false;
                dist = Mathf.Abs(city.cityLoc.x - currentLocation.x) + Mathf.Abs(city.cityLoc.z - currentLocation.z);
                safeTarget = city.cityLoc;
                continue;
            }

            int newDist = Mathf.Abs(city.cityLoc.x - currentLocation.x) + Mathf.Abs(city.cityLoc.z - currentLocation.z);
            if (newDist < dist)
            {
                safeTarget = city.cityLoc;
                dist = newDist;
            }
		}

		finalDestinationLoc = safeTarget;
        if (world.scottFollow)
            firstStep = true;
        List<Vector3Int> runAwayPath = GridSearch.AStarSearch(world, currentLocation, safeTarget, isTrader, bySea);

        //in case already home
        if (runAwayPath.Count > 0)
		    MoveThroughPath(runAwayPath);
	}

    public void StopRunningAway()
    {
	    isBusy = false;
		runningAway = false;
        exclamationPoint.SetActive(false);
	}

	public void TurnOnRipples()
	{
		if (!isMoving)
		{
			ripples.SetActive(true);
			//for tweening
			LeanTween.alpha(ripples, 1f, 0.2f).setFrom(0f).setEase(LeanTweenType.linear);
		}
	}

	public void SoftSelect(Color color)
    {
        highlight.EnableHighlight(color);
    }

    //Methods for selecting and unselecting unit
    public void Highlight(Color color)
    {
        isSelected = true;
        if (bySea)
            selectionCircle.SetActive(true);
        highlight.EnableHighlight(color);
    }

    public void Unhighlight()
    {
        isSelected = false;
        if (bySea)
            selectionCircle.SetActive(false); 
        highlight.DisableHighlight();
    }

    public void PlayAudioClip(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.Play();
    }

    public void KillUnit(Vector3 rotation)
    {
        isDead = true;
        if (military)
        {
            military.attacking = false;
            military.targetSearching = false;
            military.isMarching = false;
            military.inBattle = false;
        }

        rotation.x = -90;
        world.PlayDeathSplash(transform.position, rotation);
        //deathSplash.transform.position = transform.position;
        //deathSplash.transform.eulerAngles = rotation;
        //deathSplash.Play();
        audioSource.clip = kills[UnityEngine.Random.Range(0, kills.Length)];
        audioSource.Play();

        //gameObject.SetActive(false);
        HideUnit(true);
        Vector3 sixFeetUnder = transform.position;
        sixFeetUnder.y -= 6f;
        transform.position = sixFeetUnder;
        unitRigidbody.useGravity = false;

		if (trader)
        {
			if (isSelected)
				world.unitMovement.ClearSelection();

			world.traderList.Remove(trader);
			StartCoroutine(WaitKillUnit());

            if (ambush)
            {
                //assuming trader is last to be killed in ambush
                Vector3Int ambushLoc = world.GetClosestTerrainLoc(transform.position);
                if (world.GetTerrainDataAt(ambushLoc).treeHandler != null)
                    world.GetTerrainDataAt(ambushLoc).ToggleTransparentForest(false);

			    world.ClearAmbush(ambushLoc);
			    world.uiAttackWarning.AttackWarningCheck(ambushLoc);

                if (world.tutorial && world.ambushes == 1)
                {
                    world.mainPlayer.SetSomethingToSay("first_ambush", world.azai);
                }
            
                if (world.mainPlayer.runningAway)
                {
                    world.mainPlayer.StopRunningAway();
                    world.mainPlayer.stepAside = false;
                }
            }
            else
            {
                if (isMoving)
                    StopMovement();
                
                if (trader.followingRoute)
                    trader.CancelRoute();
            }
		}
        else if (isLaborer)
        {
			if (isSelected)
				world.unitMovement.ClearSelection();

            if (isMoving)
                StopMovement();

			world.laborerList.Remove(GetComponent<Laborer>());
			StartCoroutine(WaitKillUnit());
		}
		else if (enemyAI)
        {
            military.StopAttack();
            world.RemoveUnitPosition(currentLocation);
            if (isSelected)
				world.unitMovement.ClearSelection();
            
            if (ambush)
            {
                Vector3Int ambushLoc = world.GetClosestTerrainLoc(transform.position);
				if (world.GetTerrainDataAt(ambushLoc).treeHandler != null)
					world.GetTerrainDataAt(ambushLoc).ToggleTransparentForest(false);

				minimapIcon.gameObject.SetActive(false);
                military.enemyAmbush.ContinueTradeRoute();
                world.ClearAmbush(military.enemyAmbush.loc);
				world.uiAttackWarning.AttackWarningCheck(military.enemyAmbush.loc);
				StartCoroutine(WaitKillUnit());

				if (world.mainPlayer.runningAway)
				{
					world.mainPlayer.StopRunningAway();
					world.mainPlayer.stepAside = false;
				}
			}
            else
            {
				military.enemyCamp.deathCount++;
				military.enemyCamp.attackingArmy.attackingSpots.Remove(currentLocation);
				military.enemyCamp.ClearCampCheck();
                //if (enemyCamp.isCity && enemyCamp.growing)
                //    enemyCamp.RemoveFromCamp(this);
                
                foreach (Military unit in military.enemyCamp.UnitsInCamp)
                {
                    if (unit.targetSearching)
                        unit.enemyAI.AggroCheck();
                }

				military.enemyCamp.DeadList.Add(military);
            }
		}
        else if (military)
        {
            military.StopAttack();
            minimapIcon.gameObject.SetActive(false);
            if (military.guard)
            {
				if (isSelected)
					world.unitMovement.ClearSelection();

				military.guardedTrader.guarded = false;
				military.guardedTrader.guardUnit = null;
				military.guardedTrader = null;
				StartCoroutine(WaitKillUnit());
			}
			else
            {
				military.homeBase.army.UnitsInArmy.Remove(this);
				military.homeBase.army.attackingSpots.Remove(currentLocation);
			    RemoveUnitFromData();
                
                foreach (Unit unit in military.homeBase.army.UnitsInArmy)
                {
                    if (unit.military.targetSearching)
                        unit.military.AggroCheck();
                }
	
                if (isSelected)
		        {
			        if (military.homeBase.army.UnitsInArmy.Count > 0)//armyCount isn't changed until after battle
				    {
                        Unit nextUnitUp = military.homeBase.army.GetNextLivingUnit();
                        if (nextUnitUp != null)
                        {
    				        world.unitMovement.PrepareMovement(nextUnitUp);
                        }
                        else
                        {
                            world.somethingSelected = false;
						    world.unitMovement.ClearSelection();
                        }
				    }
                    else
                    {
                        world.somethingSelected = false;
				        world.unitMovement.ClearSelection();
                    }
		        }

				military.homeBase.army.RemoveFromArmy(this, military.barracksBunk);
				military.homeBase.army.DeadList.Add(this);
            }
		}

		Unhighlight();
    }

    private IEnumerator WaitKillUnit()
    {
        yield return new WaitForSeconds(4);

        DestroyUnit();
    }

    public void DestroyUnit()
    {
		RemoveUnitFromData();
		if (isSelected)
        {
            world.unitMovement.ClearSelection();
            world.somethingSelected = false;
        }
		Destroy(gameObject);
	}

    public void HideUnit(bool hideMarker)
    {
		if (!hidden)
        {
            marker.gameObject.SetActive(!hideMarker);
            unitMesh.gameObject.SetActive(false);
		    healthbar.gameObject.SetActive(false);
            hidden = true;
        }
	}

    public void UnhideUnit()
    {
	    if (hidden)
        {
            unitMesh.gameObject.SetActive(true);
            if (currentHealth < healthMax)
    		    healthbar.gameObject.SetActive(true);
            hidden = false;
        }
	}

    //Methods for movement order information 
    //displays movement orders when selected
    public List<Vector3Int> GetContinuedMovementPath() 
    {
        List<Vector3Int> continuedOrdersPositions = new(pathPositions);

        return continuedOrdersPositions;
    }

    public void ResetMovementOrders()
    {
        pathPositions.Clear();
        moreToMove = false;
    }

    public void CatchUp(Vector3Int lastSpot)
    {
        Queue<Vector3Int> newQueue = new Queue<Vector3Int>(pathPositions.Reverse());

        if (newQueue.Count > 0)
        {
            Vector3Int testLoc = newQueue.Dequeue();
            while (testLoc != lastSpot)
            {
                if (newQueue.Count > 0)
                    testLoc = newQueue.Dequeue();
            }
        }

        if (newQueue.Count > 0)
            finalDestinationLoc = newQueue.Peek();
        else
            finalDestinationLoc = transform.position;

        pathPositions = new Queue<Vector3Int>(newQueue.Reverse());
    }

    public void RemoveUnitFromData()
    {
        ResetMovementOrders();
        world.RemoveUnitPosition(currentLocation);
    }

    public void ShowContinuedPath()
    {
        queueCount = 1;
        ShowPath(new List<Vector3Int>(pathPositions));
    }

    private void ShowPath(List<Vector3Int> currentPath, bool queued = false)
    {
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

            if (isPlayer)
                path = world.unitMovement.movementSystem.GetFromShoePrintPool();
            else
                path = world.unitMovement.movementSystem.GetFromChevronPool();

            path.transform.position = (turnCountPosition + prevPosition) * 0.5f;
            float xDiff = turnCountPosition.x - Mathf.Round(prevPosition.x);
            float zDiff = turnCountPosition.z - Mathf.Round(prevPosition.z);

            //float x = 0;
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

            path.transform.rotation = Quaternion.Euler(90, 0, z); //x is rotating to lie flat on tile

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

    public void DequeuePath()
    {
        GameObject path = pathQueue.Dequeue();
        if (isPlayer)
        {
            world.unitMovement.movementSystem.AddToShoePrintPool(path);
        }
        else
        {
            world.unitMovement.movementSystem.AddToChevronPool(path);
        }
    }
}
