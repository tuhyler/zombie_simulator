using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;

[System.Serializable]
public class Unit : MonoBehaviour
{
	[HideInInspector]
	public int id;

	[SerializeField]
    public UnitBuildDataSO buildDataSO;

    [SerializeField]
    public GameObject unitMesh;

    [SerializeField]
    public SpriteRenderer minimapIcon;

    //[SerializeField]
    //public ParticleSystem lightBeam;

    [SerializeField]
    public GameObject selectionCircle, questionMark, exclamationPoint, ripples;

    //[SerializeField]
    //public UnitMarker marker;

    [SerializeField]
    public Healthbar healthbar;

    [SerializeField]
    public AudioClip[] greetings, attacks, kills;

    [HideInInspector]
    public Outline outline;

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
    [HideInInspector]
    public Transport transport;
    [HideInInspector]
    public TradeRep tradeRep;
    [HideInInspector]
    public Laborer laborer;
    [HideInInspector]
    public ConversationHaver conversationHaver;

	//movement details
	[HideInInspector]
    public Rigidbody unitRigidbody;
    [HideInInspector]
    public float rotationDuration = 0.3f, moveSpeed = 0.5f, originalMoveSpeed = 0.5f, threshold = 0.1f;
    [HideInInspector]
    public Queue<Vector3Int> pathPositions = new();
    [HideInInspector]
    public Vector3 destinationLoc;
    [HideInInspector]
    public Vector3 finalDestinationLoc;
    [HideInInspector]
    public Vector3Int currentLocation;
    //public Vector3Int CurrentLocation { get { return currentLocation; } set { currentLocation = value; } }
    [HideInInspector]
    public Vector3Int prevTile, prevTerrainTile, ambushLoc/*, lastClearTile*/;
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
    public int currentHealth, healthMax, upgradeLevel;
    [HideInInspector]
    public BasicEnemyAI enemyAI;
    [HideInInspector]
    public float baseSpeed;
    [HideInInspector]
    public WaitForSeconds[] attackPauses = new WaitForSeconds[3];

    //selection indicators
    [HideInInspector]
    public SelectionHighlight highlight;
    
    [HideInInspector]
    public bool isMoving, readyToMarch = true, isPlayer, isSelected, somethingToSay, sayingSomething, bySea, byAir, posSet, isDead, ambush, hidden, isUpgrading;

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
        outline = GetComponentInChildren<Outline>();
        outline.PrepOutline();

        audioSource = GetComponent<AudioSource>();
        //highlight = GetComponent<SelectionHighlight>();
        //if (highlight == null)
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
        conversationHaver = GetComponent<ConversationHaver>();
        baseSpeed = 1;

        originalMoveSpeed = buildDataSO.movementSpeed;
        bySea = buildDataSO.transportationType == TransportationType.Sea;
		//if (!bySea)
		//	marker.unit = this;

		healthMax = buildDataSO.health;
        currentHealth = healthMax;

  //      if (buildDataSO.unitDisplayName != "Azai" && buildDataSO.inMilitary && CompareTag("Player"))
  //      {
  //          inArmy = buildDataSO.transportationType == TransportationType.Land;
  //          inNavy = buildDataSO.transportationType == TransportationType.Sea;
  //          inAirForce = buildDataSO.transportationType == TransportationType.Air;
		//}

        enemyAI = GetComponent<BasicEnemyAI>();
        healthbar.SetUnit(this);

        if (bySea)
            selectionCircle.SetActive(false);

        Vector3 pos = transform.position;
        pos.y = pos.y < 3 ? 0 : 3;
        prevTile = Vector3Int.RoundToInt(pos); //world hasn't been initialized yet

        if (currentHealth == healthMax)
            healthbar.gameObject.SetActive(false);
    }

    public void PlayLightBeam()
    {
		Vector3 loc = transform.position;
        loc.y += world.IsRoadOnTileLocation(world.RoundToInt(loc)) ? 0.17f : 0.07f;

		ParticleSystem lightBeam = Instantiate(Resources.Load<ParticleSystem>("Prefabs/ParticlePrefabs/LightBeam"), loc, Quaternion.Euler(0, 0, 0));
		lightBeam.transform.SetParent(world.psHolder, false);
        lightBeam.Play();
	}

	//private void SetParticleSystems()
 //   {
	//	Vector3 loc = transform.position;

 //       if (world.IsRoadOnTileLocation(world.RoundToInt(loc)))
	//	    loc.y += 0.17f;
	//	else
	//		loc.y += 0.07f;

	//	lightBeam = Instantiate(lightBeam, loc, Quaternion.Euler(0, 0, 0));
	//	lightBeam.transform.SetParent(world.psHolder, false);

	//	if (CompareTag("Player"))
	//		lightBeam.Play();
	//}

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

		if (CompareTag("Player"))
			PlayLightBeam();
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
        if (military)
        {
            if (military.isMarching || military.isGuarding)
				unitAnimator.SetBool(isMarchingHash, false);
            else
				unitAnimator.SetBool(isMovingHash, false);

			unitAnimator.SetBool(isAttackingHash, false);
		}
        else
        {
            unitAnimator.SetBool(isMovingHash, false);
        }
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

    //taking damage
    public void ReduceHealth(Unit attackingUnit, AudioClip audio)
    {
        audioSource.clip = audio;
        audioSource.Play();
        
        if (isDead)
            return;

		healthbar.gameObject.SetActive(true);

        currentHealth -= attackingUnit.military.attackStrength + attackingUnit.military.strengthBonus/* - 1 + UnityEngine.Random.Range(0, 3)*/;

        if (currentHealth <= 0)
        {
			if (military)
            {
                if (enemyAI)
                    enemyAI.AttackCheck();
                else
                    military.AttackCheck();

                military.StopAttacking(false);
            }

            if (!attackingUnit.military.aoe)
                attackingUnit.military.attacking = false;

			if (military && military.bodyGuard)
			{
				currentHealth = 1;
				healthbar.SetHealthLevel(currentHealth);
				attackingUnit.enemyAI.AttackCheck();
				attackingUnit.military.StopAttacking(true);
                attackingUnit.military.leader.StartGloating();

                if (!attackingUnit.isDead)
    				military.bodyGuard.GoDizzy();
			}
            else
            {
			    KillUnit(attackingUnit.transform.eulerAngles);
                return;
            }
        }

        if (isSelected)
            world.unitMovement.infoManager.SetHealth(currentHealth, healthMax);

        healthbar.SetHealthLevel(currentHealth);
    }

    public void UpdateHealth(float healthLevel)
    {
        int tempHealth = Mathf.FloorToInt(healthLevel * healthMax);
        if (currentHealth != tempHealth)
        {
    		currentHealth = tempHealth;

            if (military && military.bodyGuard && world.mainPlayer.isSelected)
			    world.unitMovement.infoManager.SetHealth(currentHealth, healthMax);
            else if (isSelected)
			    world.unitMovement.infoManager.SetHealth(currentHealth, healthMax);
        }
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
        if (isDead || !gameObject.activeSelf)
            return;

        if (bySea)
            TurnOnRipples();

        pathPositions = new Queue<Vector3Int>(currentPath);

        if (trader && !trader.atStall)
        {
            world.RemoveTraderPosition(currentLocation, trader);
            
            if (trader.followingRoute && world.IsTraderWaitingForSameStop(pathPositions.Peek(), trader.tradeRouteManager.currentDestination, trader))
            {
                isMoving = true;
                trader.GetInLine();
			    return;
            }
        }
        else if (military && posSet && !military.bodyGuard)
		{
            world.RemoveUnitPosition(currentLocation);
		}
		else if (world.characterUnits.Contains(this) || transport)
        {
            world.RemovePlayerPosition(currentLocation);
        }

		Vector3 firstTarget = pathPositions.Dequeue();

        isMoving = true;
        StartAnimation();
        movingCo = StartCoroutine(MovementCoroutine(firstTarget));
    }

    private IEnumerator MovementCoroutine(Vector3 endPosition)
    {
        Vector3Int endPositionInt = world.RoundToInt(endPosition);
        destinationLoc = endPosition;
        
        //checks if tile can still be moved to before moving there
        if (trader)
        {
            if (bySea)
            {
                if (world.IsRoadOnTileLocation(endPositionInt))
                    trader.BendOverBackwards();

            }
            else
            {
                if (!trader.atStall && !world.IsRoadOnTileLocation(world.RoundToInt(endPosition)) && !trader.returning)
                {
                    if (trader.followingRoute)
                        trader.InterruptRoute(true);
            
                    yield break;
                }
            }
        }
        else if (isPlayer)
        {
            if (worker.NPCCheck(endPositionInt))
                yield break;
        }
        else if (worker)
        {
            if (worker.TransportCheck(endPosition, endPositionInt))
                yield break;
        }

        if (pathPositions.Count == 0)
            endPosition = finalDestinationLoc;

        Quaternion startRotation = transform.rotation;
        Vector3 direction = endPosition - transform.position;
        direction.y = 0;

        Quaternion endRotation;
        if (direction == Vector3.zero)
            endRotation = Quaternion.identity;
        else
            endRotation = Quaternion.LookRotation(direction, Vector3.up);

        float distance = 1f;
        float timeElapsed = 0;
        Vector3 actualEnd = endPosition;

        while (distance > threshold)
        {
            distance = Math.Abs(transform.position.x - endPosition.x) + Math.Abs(transform.position.z - endPosition.z);
            actualEnd.y = transform.position.y;
            timeElapsed += Time.deltaTime;
            float movementThisFrame = Time.deltaTime * moveSpeed;
            float lerpStep = timeElapsed / rotationDuration; //Value between 0 and 1
            transform.position = Vector3.MoveTowards(transform.position, actualEnd, movementThisFrame);
            transform.rotation = Quaternion.Lerp(startRotation, endRotation, lerpStep);

            yield return null;
        }

        if (trader)
        {
            trader.NextStepCheck(endPosition, endPositionInt);
		}
        else if (isPlayer)
        {
            worker.PlayerNextStepCheck(endPosition, endPositionInt);
		}
        else if (transport)
        {
            transport.NextStepCheck(endPosition, endPositionInt);
		}
        else if (worker)
        {
            worker.WorkerNextStepCheck(endPosition, endPositionInt);    
		}
		else if (military) 
		{
			if (military.isMarching) //making sure army is all in line
			{
                readyToMarch = false;
                bool close = pathPositions.Count == 2;

                if (enemyAI)
				    military.enemyCamp.UnitNextStep(close, endPositionInt);
                else
                    military.army.UnitNextStep(close);

			    unitAnimator.SetBool(isMarchingHash, false);
			    while (!readyToMarch)
                {
                    yield return null; //waiting for others to arrive
                }

			    unitAnimator.SetBool(isMarchingHash, true);
            }
            else if (enemyAI && military.enemyCamp.returning && military.enemyCamp.seaTravel)
            {
                if (military.atSea)
                {
					Vector3Int nextSpot;
					if (pathPositions.Count == 0)
						nextSpot = world.RoundToInt(finalDestinationLoc);
					else
						nextSpot = pathPositions.Peek();

					if (world.GetTerrainDataAt(nextSpot).isLand)
						military.ToggleBoat(false);
				}
				else
                {
					if (pathPositions.Count > 0)
					{
						if (!world.GetTerrainDataAt(endPositionInt).isLand && !world.GetTerrainDataAt(pathPositions.Peek()).isLand)
							military.ToggleBoat(true);
					}
                }
            }
            else if (military.transferring)
            {
                if (military.switchLocs.Contains(endPositionInt))
                {
                    //if (world.GetCityDevelopment(endPositionInt).GetImprovementData.singleBuildType == SingleBuildType.Harbor)
                    military.ToggleBoat(endPositionInt == military.switchLocs[0]);
                }
            }

            if (pathPositions.Count > 0)
            {
                if (ambush && pathPositions.Count == 1)
                {
				    StopAnimation();
				    isMoving = false;
				    finalDestinationLoc = prevTile;
				    currentLocation = world.AddUnitPosition(prevTile, this);

				    if (enemyAI)
					    enemyAI.StartAttack(world.GetUnit(pathPositions.Dequeue()));
				    else
					    military.StartAttack(world.GetUnit(pathPositions.Dequeue()));

				    yield break;
			    }
			    else
			    {
				    prevTile = endPositionInt;
			    }

				GoToNextStepInPath();
			}
            else
            {
				FinishMoving(endPosition);
			}
		}
		else if (laborer)
		{
            laborer.NextStepCheck(endPosition, endPositionInt);
		}
    }

    public void GoToNextStepInPath()
    {
		movingCo = StartCoroutine(MovementCoroutine(pathPositions.Dequeue()));
		if (pathQueue.Count > 0)
			DequeuePath();
	}

	public void StopMovementCheck(bool finish)
    {
        if (isMoving)
        {
            if (movingCo != null)
            {
                StopCoroutine(movingCo);
                movingCo = null;
            }

            StopAnimation();

            if (finish)
            {
                FinishMoving(destinationLoc);
            }
            else
            {
				HidePath();
				pathPositions.Clear();
			}
        }
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
        if (bySea)
            TurnOffRipples();
        queueCount = 0;
        isMoving = false;
        currentLocation = world.RoundToInt(transform.position);
        HidePath();
        pathPositions.Clear();
        StopAnimation();
        FinishedMoving?.Invoke();
        
        if (trader)
        {
            trader.FinishMovementTrader(endPosition);
            return;
        }
		else if (isDead)
		{
			return;
		}

		prevTile = currentLocation;
        
        if (buildDataSO.inMilitary)
        {
            military.FinishMovementMilitary(endPosition);
        }
        else if (enemyAI)
        {
            military.FinishMovementEnemyMilitary();
        }
        else if (isPlayer)
        {
            worker.FinishMovementPlayer(endPosition);
		}
        else if (worker)
        {
            worker.FinishMovementWorker(endPosition);
		}
        else if (transport)
        {
            transport.FinishMovementTransport(endPosition);
        }
        else if (military && military.bodyGuard)
        {
            if (military.bodyGuard.dueling)
				military.FinishMovementMilitary(endPosition);
            else
			    military.bodyGuard.FinishMovementBodyGuard(endPosition);
        }
        else if (laborer)
        {
            laborer.FinishMovementLaborer();
		}
    }

    //below two methods only used for player, scott, and azai (currently disabled as it just messes everything up)
 //   public void UnitInWayCheck() //two references in worker, one in bodyguard
 //   {
	//	//Unit unitInTheWay = world.GetPlayer(currentLocation);

	//	//if (unitInTheWay == this)
	//	//{
	//	//	world.AddPlayerPosition(currentLocation, this);
	//	//	return;
	//	//}
	//	//else if (unitInTheWay.buildDataSO.characterUnit)
	//	//{
 // //          if (worker && unitInTheWay.worker)
 // //          {
 // //              FindNewSpot(currentLocation, null);
 // //              return;
 // //          }
	//	//}

	//	//Vector3Int loc;
	//	//if (unitInTheWay.pathPositions.Count > 0)
	//	//	loc = unitInTheWay.pathPositions.Peek();
	//	//else
	//	//	loc = new Vector3Int(0, -10, 0);

	//	//FindNewSpot(currentLocation, loc);
	//}

    //public void FindNewSpot(Vector3Int current, Vector3Int? next) //one reference in worker
    //{
    //    //if (isBusy || (trader && trader.followingRoute) || inArmy || enemyAI)
    //    //    return;

    //    //int i = 0;
    //    //bool outerRing = false;
    //    //foreach (Vector3Int tile in world.GetNeighborsFor(current, MapWorld.State.EIGHTWAYTWODEEP))
    //    //{
    //    //    i++;
    //    //    if (i > 8)
    //    //        outerRing = true;

    //    //    if (trader && !world.IsRoadOnTileLocation(tile))
    //    //        continue;

    //    //    if (!world.PlayerCheckIfPositionIsValid(tile) || world.IsPlayerLocationTaken(tile))
    //    //        continue;

    //    //    if (tile == next && tile != null) 
    //    //        continue;

    //    //    finalDestinationLoc = tile;

    //    //    if (outerRing)
    //    //        MoveThroughPath(new List<Vector3Int> { (current + tile) / 2, tile });
    //    //    else
    //    //        MoveThroughPath(new List<Vector3Int> { tile });
    //    //    return;
    //    //}
    //}

    public void Teleport(Vector3 loc)
    {
        if (military && !military.guard)
        {
            if (military.atSea)
                military.ToggleBoat(false);
            transform.position = loc;
            destinationLoc = loc;
            StopMovementCheck(true);
        }
        else if (trader)
        {
            currentLocation = world.RoundToInt(loc);
			transform.position = loc;
			world.AddTraderPosition(currentLocation, trader);
        }
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
                                    improvement.HardReveal();
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

			if (td.isDiscovered)
				continue;

			if (world.IsRoadOnTerrain(tilesToCheck[i]))
				world.SetRoadActive(tilesToCheck[i]);

			if (world.cityImprovementDict.ContainsKey(tilesToCheck[i]))
				world.cityImprovementDict[tilesToCheck[i]].RevealImprovement(false);

			td.Reveal();
			world.cameraController.CheckLoc(tilesToCheck[i]);
			if (world.IsTradeCenterOnTile(tilesToCheck[i]))
				world.GetTradeCenter(tilesToCheck[i]).Reveal();

		}
    }

    public void SpeakingCheck()
    {
        conversationHaver.SpeakingCheck();
    }

	public void SetSpeechBubble()
	{
		conversationHaver.SetSpeechBubble();
	}

	public bool SaidSomething()
	{
		return conversationHaver.SaidSomething();
	}

	//private void CheckPrevTile()
 //   {
 //       TerrainData td = world.GetTerrainDataAt(lastClearTile);
 //       td.ToggleTransparentForest(false);
 //   }

    public void TurnOffRipples()
    {
		LeanTween.cancel(ripples.gameObject);
		LeanTween.alpha(ripples, 0f, 0.5f).setFrom(1f).setEase(LeanTweenType.linear).setOnComplete(SetActiveStatusFalse);
	}

	private void SetActiveStatusFalse()
	{
		if (!isMoving)
            ripples.SetActive(false);
	}

 //   public void MarkerCheck()
 //   {
 //       if (isDead)
 //           return;
        
 //       Vector3Int loc = world.GetClosestTerrainLoc(transform.position);
 //       TerrainData td = world.GetTerrainDataAt(loc);

 //       if (world.IsBuildLocationTaken(loc) || td.treeHandler)
 //           outline.ToggleOutline(true);
 //           //marker.ToggleVisibility(true);
	//}

  //  private void OnCollisionEnter(Collision collision)
  //  {
  //      if (military && !military.bodyGuard)
  //      {
  //          if (!military.battleCam && (military.isMarching || military.returning || military.transferring))
  //          {
		//		Vector3Int loc = world.GetClosestTerrainLoc(transform.position);
  //              if (collision.gameObject.CompareTag("Forest") || collision.gameObject.CompareTag("Forest Hill") || world.IsBuildLocationTaken(loc))
  //                  outline.ToggleOutline(true);
  //              else
  //                  outline.ToggleOutline(false);
  //          }
		//}
  //      else if (!bySea && !byAir)
  //      {
  // //         Vector3Int loc = world.GetClosestTerrainLoc(transform.position);

  // //         if (collision.gameObject.CompareTag("Forest") || collision.gameObject.CompareTag("Forest Hill") || world.IsBuildLocationTaken(loc))
  // //             outline.ToggleOutline(true);
		//	//else
  // //             outline.ToggleOutline(false);
  //      }
  //  }

    private void OnCollisionStay(Collision collision)
    {
        //Debug.Log("colliding with " + collision.gameObject.tag);

        //threshold = 0.001f;

        if (military)
        {
			if (enemyAI)
			{
				TerrainData td = collision.gameObject.GetComponent<TerrainData>();
				if (td)
				{
					if (td.isDiscovered)
						UnhideUnit();
                    else
						HideUnit();
				}
			}

			if (military.isMarching)
            {
                moveSpeed = baseSpeed * flatlandSpeed * .05f;
                unitAnimator.SetFloat("speed", baseSpeed * 18f);
                return;
            }            
        }

        string tag = collision.gameObject.tag;

        switch (tag)
        {
            case "Road":
				moveSpeed = baseSpeed * roadSpeed * originalMoveSpeed * .25f;
				unitAnimator.SetFloat("speed", baseSpeed * originalMoveSpeed * 18f);
				//threshold = 0.1f;
				break;
            case "City":
				moveSpeed = baseSpeed * flatlandSpeed * originalMoveSpeed * .1f;
				unitAnimator.SetFloat("speed", baseSpeed * originalMoveSpeed * 18f);
				break;
            case "Flatland":
				moveSpeed = baseSpeed * flatlandSpeed * originalMoveSpeed * .1f;
				unitAnimator.SetFloat("speed", baseSpeed * originalMoveSpeed * 18f);
				break;
            case "Forest":
				moveSpeed = baseSpeed * forestSpeed * originalMoveSpeed * 0.025f;
				unitAnimator.SetFloat("speed", baseSpeed * originalMoveSpeed * 9f);
				break;
            case "Hill":
				moveSpeed = baseSpeed * hillSpeed * originalMoveSpeed * 0.025f;
				unitAnimator.SetFloat("speed", baseSpeed * originalMoveSpeed * 9f);
				break;
            case "Forest Hill":
				moveSpeed = baseSpeed * forestHillSpeed * originalMoveSpeed * 0.02f;
				unitAnimator.SetFloat("speed", baseSpeed * originalMoveSpeed * 5f);
				break;
            case "Water":
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
				break;
            case "Swamp":
				moveSpeed = baseSpeed * forestSpeed * originalMoveSpeed * .0125f;
				unitAnimator.SetFloat("speed", baseSpeed * originalMoveSpeed * 5f);
				break;
        }
    }

	public void TurnOnRipples()
	{
		if (!isMoving)
		{
            LeanTween.cancel(ripples.gameObject);
            ripples.SetActive(true);
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
        if (isDead)
            return;
        
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
        audioSource.clip = kills[UnityEngine.Random.Range(0, kills.Length)];
        audioSource.Play();

        HideUnit();
        Vector3 sixFeetUnder = transform.position;
        sixFeetUnder.y -= 6f;
        transform.position = sixFeetUnder;
        unitRigidbody.useGravity = false;

		if (enemyAI)
            military.KillMilitaryEnemyUnit();
        else if (military)
            military.KillMilitaryUnit(rotation);
		else if (trader)
            trader.KillTrader();
        else if (laborer)
            laborer.KillLaborer();

		Unhighlight();
    }

    public IEnumerator WaitKillUnit()
    {
        yield return new WaitForSeconds(4);

        DestroyUnit();
    }

    public void DestroyUnit()
    {
		RemoveUnitFromData();
		if (isSelected)
        {
            world.somethingSelected = false;
            world.unitMovement.ClearSelection();
        }
		Destroy(gameObject);
	}

    public void HideUnit()
    {
		if (!hidden)
        {
            //if (marker != null)
            //    marker.gameObject.SetActive(!hideMarker);
            if (military && military.atSea)
            {
                military.boatMesh.SetActive(false);
                ripples.SetActive(false);
            }
            unitMesh.SetActive(false);
		    healthbar.gameObject.SetActive(false);
            hidden = true;
        }
	}

    public void UnhideUnit()
    {
	    if (hidden)
        {
            if (military && military.atSea)
            {
                military.boatMesh.SetActive(true);
                ripples.SetActive(true);
                //marker.gameObject.SetActive(false);
            }
            else
            {
                unitMesh.SetActive(true);

                //TerrainData td = world.GetTerrainDataAt(world.RoundToInt(transform.position));
				//if (!td.CompareTag("Forest") && !td.CompareTag("Forest Hill"))
				//	marker.gameObject.SetActive(false);
			}
            if (currentHealth < healthMax)
    		    healthbar.gameObject.SetActive(true);
            hidden = false;
        }
	}

    //Methods for movement order information 
    //displays movement orders when selected
    //public List<Vector3Int> GetContinuedMovementPath() 
    //{
    //    List<Vector3Int> continuedOrdersPositions = new(pathPositions);

    //    return continuedOrdersPositions;
    //}

    //public void CatchUp(Vector3Int lastSpot)
    //{
    //    Queue<Vector3Int> newQueue = new Queue<Vector3Int>(pathPositions.Reverse());

    //    if (newQueue.Count > 0)
    //    {
    //        Vector3Int testLoc = newQueue.Dequeue();
    //        while (testLoc != lastSpot)
    //        {
    //            if (newQueue.Count > 0)
    //                testLoc = newQueue.Dequeue();
    //        }
    //    }

    //    if (newQueue.Count > 0)
    //        finalDestinationLoc = newQueue.Peek();
    //    else
    //        finalDestinationLoc = transform.position;

    //    pathPositions = new Queue<Vector3Int>(newQueue.Reverse());
    //}

    public void RemoveUnitFromData()
    {
		pathPositions.Clear();

        if (military && posSet)
            world.RemoveUnitPosition(currentLocation);
        else if (trader)
            world.RemoveTraderPosition(currentLocation, trader);
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
