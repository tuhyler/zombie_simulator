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

    //[SerializeField]
    //private GameObject minimapIcon;

    [SerializeField]
    public SkinnedMeshRenderer unitMesh;

    [SerializeField]
    public SpriteRenderer minimapIcon;

    [SerializeField]
    public ParticleSystem lightBeam, deathSplash;

    [SerializeField]
    private GameObject selectionCircle;

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

    private InfoManager infoManager;

    //movement details
    [HideInInspector]
    public Rigidbody unitRigidbody;
    private float rotationDuration = 0.2f, moveSpeed = 0.5f, originalMoveSpeed = 0.5f, threshold = 0.01f;
    [HideInInspector]
    public Queue<Vector3Int> pathPositions = new();
    [HideInInspector]
    public bool moreToMove, isBusy, isMoving, isBeached, interruptedRoute, secondaryPrefab; //if there's more move orders, if they're doing something, if they're moving, if they're a boat on land, if route was interrupted unexpectedly
    [HideInInspector]
    public Vector3 destinationLoc;
    [HideInInspector]
    public Vector3 finalDestinationLoc;
    private Vector3Int currentLocation;
    public Vector3Int CurrentLocation { get { return currentLocation; } set { currentLocation = value; } }
    [HideInInspector]
    public Vector3Int prevRoadTile, prevTerrainTile; //first one is for traders, in case road they're on is removed
    private int flatlandSpeed, forestSpeed, hillSpeed, forestHillSpeed, roadSpeed;
    [HideInInspector]
    public Coroutine movingCo, waitingCo, attackCo;
    private MovementSystem movementSystem;

    //showing footprints
    private Queue<GameObject> pathQueue = new();
    private int queueCount = 0;
    public int QueueCount { set { queueCount = value; } }
    //private Vector3 shoePrintScale;
    //private GameObject mapIcon;
    private WaitForSeconds moveInLinePause = new WaitForSeconds(0.5f);
    //private bool onTop; //if on city tile and is on top

    //combat info
    [HideInInspector]
    public City homeBase;
    [HideInInspector]
    public Vector3Int barracksBunk, marchPosition; //setting their position in the army based on bunk and barracks loc
    [HideInInspector]
    public int currentHealth;
    [HideInInspector]
    public BasicEnemyAI enemyAI;
    [HideInInspector]
    public EnemyCamp enemyCamp;
    private int healthMax;
    [HideInInspector]
    public float baseSpeed, attackSpeed;
    [HideInInspector]
    public int attackStrength;
    [HideInInspector]
    public float[] attackPause = new[] { 0.8f, 1f, 1.2f };
    public WaitForSeconds[] attackPauses = new WaitForSeconds[4];
    [HideInInspector]
    public Projectile projectile;

    //selection indicators
    private SelectionHighlight highlight;
    private CameraController focusCam;
    [HideInInspector]
    public UIUnitTurnHandler turnHandler;

    [HideInInspector]
    public bool bySea, isTrader, atStop, followingRoute, isWorker, isLaborer, isSelected, isWaiting, harvested, harvestedForest, somethingToSay, sayingSomething;

    //military booleans
    [HideInInspector]
    public bool isLeader, readyToMarch = true, inArmy, atHome, preparingToMoveOut, isMarching, transferring, repositioning, inBattle, attacking, targetSearching, flanking, flankedOnce, cavalryLine, isDead, isUpgrading;
    [HideInInspector]
    public Vector3Int targetLocation, targetBunk; //targetLocation is in case units overlap on same tile

    //animation
    [HideInInspector]
    public Animator unitAnimator;
    private int isMovingHash;
    private int isMarchingHash;
    private int isAttackingHash;

    private void Awake()
    {
        AwakeMethods();
    }

    protected virtual void AwakeMethods()
    {
        //turnHandler = FindObjectOfType<UIUnitTurnHandler>();
        //focusCam = FindObjectOfType<CameraController>();
        //world = FindObjectOfType<MapWorld>();
        //movementSystem = FindObjectOfType<MovementSystem>();
        audioSource = GetComponent<AudioSource>();
        highlight = GetComponent<SelectionHighlight>();
        if (highlight == null)
            highlight = GetComponentInChildren<SelectionHighlight>();
        unitAnimator = GetComponent<Animator>();
        isMovingHash = Animator.StringToHash("isMoving");
        isMarchingHash = Animator.StringToHash("isMarching");
        isAttackingHash = Animator.StringToHash("isAttacking");
        attackPauses[0] = new WaitForSeconds(.9f);
		attackPauses[1] = new WaitForSeconds(1f);
		attackPauses[2] = new WaitForSeconds(1.1f);
        attackPauses[3] = new WaitForSeconds(.25f);
		unitRigidbody = GetComponent<Rigidbody>();
        baseSpeed = 1;

        originalMoveSpeed = buildDataSO.movementSpeed;
        //mapIcon = world.CreateMapIcon(buildDataSO.mapIcon);
        bySea = buildDataSO.transportationType == TransportationType.Sea;
		if (!bySea)
			marker.Unit = this;

		healthMax = buildDataSO.health;
        currentHealth = healthMax;
        attackSpeed = buildDataSO.baseAttackSpeed;
        //attackPause = new(attackSpeed);
        attackStrength = buildDataSO.baseAttackStrength;
        inArmy = attackStrength > 0 && CompareTag("Player");

        if (buildDataSO.unitType == UnitType.Ranged)
        {
            projectile = GetComponentInChildren<Projectile>();
            projectile.SetProjectilePos();
            projectile.gameObject.SetActive(false);
        } 

        enemyAI = GetComponent<BasicEnemyAI>();
        readyToMarch = true;
        healthbar.SetUnit(this);
        //if (enemyAI != null)
        //    enemyAI.SetAttackSpeed(attackSpeed);
        //shoePrintScale = GameAssets.Instance.shoePrintPrefab.transform.localScale;
        if (bySea)
            selectionCircle.SetActive(false);
        if (isTrader)
            prevRoadTile = Vector3Int.RoundToInt(transform.position); //world hasn't been initialized yet

        if (currentHealth == healthMax)
            healthbar.gameObject.SetActive(false);
    }

  //  private void Start()
  //  {
		////turnHandler.turnHandler.AddToTurnList(this);
		////roadSpeed = world.GetRoadCost();
		////Vector3 loc = transform.position;
		////loc.y += 0.04f;
		////lightBeam = Instantiate(lightBeam, loc, Quaternion.Euler(0, 0, 0));
		////lightBeam.transform.parent = world.psHolder;
		
		////if (CompareTag("Player"))
  ////      {
  ////          lightBeam.Play();
  ////      }

  //      //world.SetMapIconLoc(world.RoundToInt(transform.position), mapIcon);
  //      //SetMinimapIcon();
  //      //Physics.IgnoreLayerCollision(8, 10);
  //  }

	private void SetParticleSystems()
    {
		Vector3 loc = transform.position;
		loc.y += 0.07f;
		lightBeam = Instantiate(lightBeam, loc, Quaternion.Euler(0, 0, 0));
		lightBeam.transform.parent = world.psHolder;

		if (CompareTag("Player"))
		{
			lightBeam.Play();
		}

		if (inArmy || enemyAI)
		{
			deathSplash = Instantiate(deathSplash);
			deathSplash.transform.parent = world.psHolder;
			deathSplash.Stop();
		}
	}

	public void SetReferences(MapWorld world, CameraController focusCam, UIUnitTurnHandler turnHandler, MovementSystem movementSystem)
    {
        this.world = world;
        this.focusCam = focusCam;

        if (!enemyAI)
            this.turnHandler = turnHandler;

        this.movementSystem = movementSystem;
        infoManager = movementSystem.unitMovement.infoManager;

        if (CompareTag("Player"))
            turnHandler.turnHandler.AddToTurnList(this);

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
        focusCam.followTransform = transform;
    }

    public void SayHello()
    {
        audioSource.clip = greetings[0];// [UnityEngine.Random.Range(0, greetings.Length)];
        audioSource.Play();
    }

    //public void SayOK()
    //{
    //    audioSource.clip = acknowledges[0];// [UnityEngine.Random.Range(0, acknowledges.Length)];
    //    audioSource.Play();
    //}

    public void StartAttackingAnimation()
    {
		unitAnimator.SetBool(isAttackingHash, true);
        unitAnimator.SetFloat("attackSpeed", 1);
    }

 //   private void StopAttackingAnimation()
 //   {
	//	unitAnimator.SetBool(isAttackingHash, false);
	//}

	public void StopAnimation()
    {
        if (isMarching)
            unitAnimator.SetBool(isMarchingHash, false);
        else
            unitAnimator.SetBool(isMovingHash, false);
    
        if (attackStrength > 0)
            unitAnimator.SetBool(isAttackingHash, false);
    }

    public void StopAttackAnimation()
    {
		unitAnimator.SetBool(isAttackingHash, false);
	}

    public void InterruptedRouteMessage()
    {
        interruptedRoute = false;
        SetInterruptedAnimation(false);
        InfoPopUpHandler.WarningMessage().Create(transform.position, "Route not possible to complete");
    }

    public void SetMinimapIcon(Transform parent)
    {
        minimapIcon.sprite = buildDataSO.mapIcon;
        ConstraintSource constraintSource = new();
        constraintSource.sourceTransform = parent;
        constraintSource.weight = 1;
        RotationConstraint rotation = minimapIcon.GetComponent<RotationConstraint>();
		rotation.rotationAtRest = new Vector3(90, 0, 0);
		rotation.rotationOffset = new Vector3(90, 0, 0);
		rotation.AddSource(constraintSource);
        minimapIcon.gameObject.SetActive(true);
        
        //GameObject icon = Instantiate(minimapIcon);
        //minimapIcon.GetComponent<FollowNoRotate>().objectToFollow = transform;
        //world.AddToMinimap(minimapIcon.gameObject);
    }

    //taking damage
    public void ReduceHealth(int damage, Vector3 rotation, AudioClip audio)
    {
        audioSource.clip = audio;
        audioSource.Play();
        
        if (isDead)
            return;

		baseSpeed = .8f;

		healthbar.gameObject.SetActive(true);

        currentHealth -= (damage - 1 + UnityEngine.Random.Range(0, 3));

        if (currentHealth <= 0)
        {
			StopAttacking();
            KillUnit(rotation);
            return;
        }
            //DestroyUnit();

        if (isSelected)
            infoManager.SetHealth(currentHealth, healthMax);

        healthbar.SetHealthLevel(currentHealth);
    }

    public void UpdateHealth(float healthLevel)
    {
        currentHealth = Mathf.FloorToInt(healthLevel * healthMax);
        //this.currentHealth = currentHealth;

        if (isSelected)
			infoManager.SetHealth(currentHealth, healthMax);
	}

    //private IEnumerator SlowMovement()
    //{

    //}


    private void StartAnimation()
    {
        if (isMarching)
            unitAnimator.SetBool(isMarchingHash, true);
        else
            unitAnimator.SetBool(isMovingHash, true);
    }

    //Methods for moving unit
    //Gets the path positions and starts the coroutines
    public void MoveThroughPath(List<Vector3Int> currentPath) 
    {
        if (isDead)
            return;
        //CenterCamera(); //focus to start moving

        world.RemoveUnitPosition(currentLocation);//removing previous location

        //finalDestinationLoc = currentPath[currentPath.Count - 1].transform.position; //currentPath is list instead of queue for this line
        pathPositions = new Queue<Vector3Int>(currentPath);

        //ShowPath(currentPath);

        if (isTrader)
        {
		    if (followingRoute)
            {                
                if (world.IsUnitWaitingForSameStop(pathPositions.Peek(), finalDestinationLoc))
			    {
                    moreToMove = true;
                    isMoving = true;
                    GetInLine();
				    return;
			    }
            }
            else
            {
                if (bySea)
                {
                    if (world.IsCityHarborOnTile(currentLocation))
                        world.GetHarborCity(world.GetClosestTerrainLoc(currentLocation)).tradersHere.Remove(this);
                }
                else
                {
                    if (world.IsCityOnTile(currentLocation))
                        world.GetCity(world.GetClosestTerrainLoc(currentLocation)).tradersHere.Remove(this);
                }
            }
        }  

		Vector3 firstTarget = pathPositions.Dequeue();

        moreToMove = true;
        isMoving = true;
        //unitRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
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
        //else if (!td.isLand)
        //{
        //    y = -.45f;
        //}

        //if (world.IsRoadOnTileLocation(endPositionInt))
        //{
        //    marker.ToggleVisibility(false);
        //}
        //else if (td.IsSeaCorner) //walking the beach in rivers
        //{
        //    if (world.CheckIfCoastCoast(endPositionInt))
        //        y = -.10f;
        //    else
        //        y = transform.position.y;
        //}

        //if (followingRoute && world.IsUnitWaitingForSameStop(endPositionInt, finalDestinationLoc))
        //{
        //    GetInLine(endPosition);
        //    yield break;
        //}
        if (pathPositions.Count == 0 && !enemyAI && !inArmy && world.IsUnitLocationTaken(endPositionInt)) //don't occupy sqaure if another unit is there
        {
            Unit unitInTheWay = world.GetUnit(endPositionInt);

            if (unitInTheWay.isBusy || unitInTheWay.followingRoute || unitInTheWay.inArmy)
            {
                if (isBusy)
                {
                    SkipRoadBuild();
                    if (isSelected)
                        td.DisableHighlight();

                    yield break;
                }
            }
            else
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
        if (isTrader && !bySea && !world.IsRoadOnTileLocation(world.RoundToInt(endPosition)))
        {
            if (followingRoute)
            {
                InterruptRoute();
            }
                
            FinishMoving(endPosition);
            yield break;
        }

        if (pathPositions.Count == 0)
            endPosition = finalDestinationLoc;

        Quaternion startRotation = transform.rotation;
        endPosition.y = y;
        Vector3 direction = endPosition - transform.position;
        direction.y = 0;
        if (world.RoundToInt(direction) == world.RoundToInt(transform.position))
            direction += new Vector3(0, 0.05f, 0);

        Quaternion endRotation = Quaternion.LookRotation(direction, Vector3.up);
        float distance = 1f;
        float timeElapsed = 0;

        while (distance > threshold)
        //while (Math.Abs(transform.localPosition.x - endPosition.x) + Math.Abs(transform.localPosition.z - endPosition.z) > threshold)
        //while (Mathf.Pow(transform.localPosition.x - endPosition.x, 2) + Mathf.Pow(transform.localPosition.z - endPosition.z, 2) > threshold)
        {
            distance = Math.Abs(transform.localPosition.x - endPosition.x) + Math.Abs(transform.localPosition.z - endPosition.z);
            timeElapsed += Time.deltaTime;
            float movementThisFrame = Time.deltaTime * moveSpeed;
            float lerpStep = timeElapsed / rotationDuration; //Value between 0 and 1
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, endPosition, movementThisFrame);
            //unitRigidbody.MovePosition(Vector3.MoveTowards(transform.localPosition, endPosition, movementThisFrame));
            //transform.localPosition = newPosition;
            transform.rotation = Quaternion.Lerp(startRotation, endRotation, lerpStep);
            //Debug.Log("distance: " + distance);
            //Debug.Log("current: " + transform.localPosition);

            if (distance <= threshold)
                break;
            //if (Mathf.Pow(transform.localPosition.x - endPosition.x, 2) + Mathf.Pow(transform.localPosition.z - endPosition.z, 2) <= threshold)
            //    break;
            //if (Math.Abs(transform.localPosition.x - endPosition.x) + Math.Abs(transform.localPosition.z - endPosition.z) <= threshold)
            //    break;

            yield return null;
        }

        if (isWorker || isLeader || bySea)
        {
			Vector3Int pos = world.GetClosestTerrainLoc(transform.position);
			if (pos != prevTerrainTile)
				RevealCheck(pos);
		}

		//exploring
		//if (isTrader && !bySea && !world.hideTerrain) {}
  //      else if (!enemyAI)
  //      {
  //          Vector3Int pos = world.GetClosestTerrainLoc(transform.position);
  //          if (pos != prevTerrainTile)
  //              RevealCheck(pos);
  //      }

        //if (enemyAI)
        //{
        //    if (enemyAI.ResetTarget())
        //        yield break;
        //}

        //making sure army is all in line
        if (isMarching)
        {
            readyToMarch = false;
            homeBase.army.UnitNextStep();

			unitAnimator.SetBool(isMarchingHash, false);
			while (!readyToMarch)
            {
                yield return null;
            }
			unitAnimator.SetBool(isMarchingHash, true);
		}

		//if (onTop && world.GetTerrainDataAt(endPositionInt).gameObject.tag != "City")
		//{
		//    Debug.Log("success!");
		//    onTop = false;
		//    mesh.layer = LayerMask.NameToLayer("Default");
		//}
		//if (world.showingMap)
		//world.SetMapIconLoc(endPositionInt, mapIcon);

		if (pathPositions.Count > 0)
        {
            if (isTrader)
                prevRoadTile = world.RoundToInt(endPosition);

            if (followingRoute && world.IsUnitWaitingForSameStop(pathPositions.Peek(), finalDestinationLoc))
            {
                GetInLine();
                yield break;
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
                StopCoroutine(movingCo);

            if (isMoving) //check here twice in case still moving after stopping coroutine
                FinishMoving(destinationLoc);
        }
        else 
        {
            if (bySea)
            {
                if (world.IsCityHarborOnTile(currentLocation))
					world.GetHarborCity(world.GetClosestTerrainLoc(currentLocation)).tradersHere.Remove(this);
            }
            else
            {
				if (world.IsCityOnTile(currentLocation))
					world.GetCity(world.GetClosestTerrainLoc(currentLocation)).tradersHere.Remove(this);
			}
        }

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

    //private IEnumerator MarchingInPlace()
    //{
    //    while (!readyToMarch)
    //    {
    //        yield return null;
    //    }
    //}

    public void Rotate(Vector3 lookAtTarget)
    {
        StartCoroutine(RotateTowardsPosition(lookAtTarget));
    }

	public IEnumerator RotateTowardsPosition(Vector3 lookAtTarget)
	{
        if (lookAtTarget == CurrentLocation)
            lookAtTarget += new Vector3(0, 0.05f, 0);
        
        Vector3 direction = lookAtTarget - transform.position;
		//direction.y = 0;

		float totalTime = 0;
		while (totalTime < 0.35f)
		{
			float timePassed = Time.deltaTime;
			transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(direction, Vector3.up), timePassed * 12);
			totalTime += timePassed;
			yield return null;
		}
	}

	private void GetInLine()
    {
        Vector3Int currentLoc = world.RoundToInt(transform.position);

        if (world.UnitAlreadyThere(this, currentLoc))
            currentLocation = currentLoc;
        else
        {
            if (world.IsUnitWaitingForSameStop(currentLoc, finalDestinationLoc))
            {
                GoToBackOfLine(world.RoundToInt(finalDestinationLoc), currentLoc);
                GetInLine();
                return;
            }

            currentLocation = world.AddUnitPosition(currentLoc, this);
        }
        isWaiting = true;
        movingCo = null;
        unitAnimator.SetBool(isMovingHash, false);

        Vector3Int tradePos = world.GetStopLocation(world.GetTradeLoc(world.RoundToInt(finalDestinationLoc)));

		if (world.IsCityOnTile(tradePos))
			world.GetCity(tradePos).AddToWaitList(this);
		else if (world.IsWonderOnTile(tradePos))
			world.GetWonder(tradePos).AddToWaitList(this);
		else if (world.IsTradeCenterOnTile(tradePos))
			world.GetTradeCenter(tradePos).AddToWaitList(this);
    }

    private void GoToBackOfLine(Vector3Int finalLoc, Vector3Int currentLoc)
    {
        List<Vector3Int> positionsToCheck = new(){ currentLoc };
        bool success = false;
        bool prevPath = true;
        List<Vector3Int> newPath = new();

        while (positionsToCheck.Count > 0)
        {
            Vector3Int current = positionsToCheck[0];
            positionsToCheck.Remove(current);
            newPath.Add(current);
            
            if (prevPath) //first check the tile the trader came from
            {
                prevPath = false;

                if (!world.IsUnitWaitingForSameStop(prevRoadTile, finalLoc))
                {
                    Teleport(prevRoadTile);
                    positionsToCheck.Clear();
                    success = true;
                }
                else
                {
                    positionsToCheck.Add(prevRoadTile);
                }
            }
            else
            {
                foreach (Vector3Int neighbor in world.GetNeighborsFor(current, MapWorld.State.EIGHTWAY))
                {
                    if (!bySea)
                    {
                        if (!world.IsRoadOnTileLocation(neighbor))
                            continue;
                    }
                    else
                    {
                        if (!world.CheckIfSeaPositionIsValid(neighbor))
                            continue;
                    }

                    if (world.IsUnitWaitingForSameStop(neighbor, finalLoc)) //going away from final loc
                    {
                        if (ManhattanDistance(finalLoc, current) < ManhattanDistance(finalLoc, neighbor))
                        {
                            positionsToCheck.Add(neighbor);
                            break;
                        }
                    }
                    else
                    {
                        //teleport to back of line
                        Teleport(neighbor);
                        positionsToCheck.Clear();
                        success = true;
                        break;
                    }
                }
            }
        }

        if (!success)
            InterruptRoute();
        else
        {
            List<Vector3Int> oldPath = new(pathPositions);
            newPath.Reverse();
            newPath.AddRange(oldPath);
            pathPositions = new Queue<Vector3Int>(newPath);
        }
    }

    public void InterruptRoute()
    {
        CancelRoute();
        interruptedRoute = true;
        if (isSelected)
            InterruptedRouteMessage();
        else
            SetInterruptedAnimation(true);
    }

    private int ManhattanDistance(Vector3Int endPos, Vector3Int point) 
    {
        return Math.Abs(endPos.x - point.x) + Math.Abs(endPos.z - point.z);
    }

    public void ExitLine()
    {
        world.RemoveUnitPosition(currentLocation);
        unitAnimator.SetBool(isMovingHash, true);
        movingCo = StartCoroutine(MovementCoroutine(pathPositions.Dequeue()));
    }

    public IEnumerator MoveUpInLine(int placeInLine)
    {
        if (pathPositions.Count == 1)
            yield break;

        float pause = 0.5f * placeInLine;

        while (pause > 0)
        {
            yield return moveInLinePause;// new WaitForSeconds(0.5f * placeInLine);
            pause -= 0.5f;
        }

        if (world.IsUnitWaitingForSameStop(pathPositions.Peek(), finalDestinationLoc))
            yield break;

        Vector3Int nextSpot = pathPositions.Dequeue();
        world.RemoveUnitPosition(currentLocation);
        if (world.IsUnitLocationTaken(nextSpot))
        {
            Unit unitInTheWay = world.GetUnit(nextSpot);
            unitInTheWay.FindNewSpot(nextSpot, pathPositions.Peek());
        }
        world.AddUnitPosition(nextSpot, this);
        unitAnimator.SetBool(isMovingHash, true);
        movingCo = StartCoroutine(MovementCoroutine(nextSpot));
    }

    //public bool LineCutterCheck()
    //{
    //    if (world.IsUnitWaitingForSameStop(world.RoundToInt(transform.position), finalDestinationLoc))
    //    {
    //        CancelRoute();
    //        InfoPopUpHandler.WarningMessage().Create(transform.position, "No cutting in line");
    //        return true;
    //    }

    //    return false;
    //}

    private void FinishMoving(Vector3 endPosition)
    {
        //unitRigidbody.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation; 
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
        if (isTrader)
        {
            if (bySea)
            {
				if (!followingRoute && world.IsCityHarborOnTile(currentLocation))
                {
                    City city = world.GetHarborCity(world.GetClosestTerrainLoc(currentLocation));
					city.tradersHere.Add(this);
                    if (world.unitMovement.upgradingUnit)
						world.unitMovement.ToggleUnitHighlights(true, city);
                }
			}
			else
            {
                if (!followingRoute && world.IsCityOnTile(currentLocation))
                {
                    City city = world.GetCity(world.GetClosestTerrainLoc(currentLocation));
					city.tradersHere.Add(this);
					if (world.unitMovement.upgradingUnit)
						world.unitMovement.ToggleUnitHighlights(true, city);
				}
                //if location has been taken away (such as when wonder finishes)
                if (!world.CheckIfPositionIsValid(world.GetClosestTerrainLoc(endPosition)))
                {
                    if (followingRoute)
                        InterruptRoute();
                    TeleportToNearestRoad(world.RoundToInt(currentLocation));
                    prevRoadTile = currentLocation;
                    return;
                }
            }

            if (followingRoute)
            {
                if (world.IsUnitWaitingForSameStop(currentLocation, finalDestinationLoc))
                {
                    GoToBackOfLine(world.RoundToInt(finalDestinationLoc), currentLocation);
                    GetInLine();
                    prevRoadTile = currentLocation;
                    return;
                }
            }

            prevRoadTile = currentLocation;            
        }

        TradeRouteCheck(endPosition);

        if (isDead)
            return;
        else if (inArmy)
        {
            //specifically for military units
            if (inBattle)
            {
                if (world.IsUnitLocationTaken(currentLocation))
                    FindNewBattleSpot(currentLocation, targetLocation);
                else
					world.AddUnitPosition(currentLocation, this);

				AggroCheck();
            }
            else if (preparingToMoveOut)
            {
				world.AddUnitPosition(currentLocation, this);
				preparingToMoveOut = false;
                homeBase.army.UnitReady();
            }
            else if (isMarching)
            {
				world.AddUnitPosition(currentLocation, this);
				isMarching = false;

				Vector3Int endTerrain = world.GetClosestTerrainLoc(endPosition);
				homeBase.army.UnitArrived(endTerrain);

				if (currentLocation == barracksBunk)
                {
					if (currentHealth < buildDataSO.health)
                        healthbar.RegenerateHealth();

					atHome = true;
                    marker.ToggleVisibility(false);
                    if (isSelected && !world.unitOrders)
                        world.unitMovement.ShowIndividualCityButtonsUI();

					StartCoroutine(RotateTowardsPosition(homeBase.army.GetRandomSpot(barracksBunk)));
                    return;
				}

                //turning to face enemy
                Vector3Int diff = endTerrain - homeBase.army.EnemyTarget;
                if (Math.Abs(diff.x) == 3)
                    diff.z = 0;
                else if (Math.Abs(diff.z) == 3)
                    diff.x = 0;

                StartCoroutine(RotateTowardsPosition(endPosition - diff));
            }
            else if (transferring)
            {
				world.AddUnitPosition(currentLocation, this);
				if (endPosition != barracksBunk)
                    GoToBunk();
                else
                {
                    transferring = false;
                    atHome = true;

					StartCoroutine(RotateTowardsPosition(homeBase.army.GetRandomSpot(barracksBunk)));
					if (isSelected && !world.unitOrders)
						world.unitMovement.ShowIndividualCityButtonsUI();

                    if (homeBase.army.AllAreHomeCheck())
                        homeBase.army.isTransferring = false;
				}
            }
            else if (repositioning)
            {
                world.AddUnitPosition(currentLocation, this);
                atHome = true;
				repositioning = false;
                StartCoroutine(RotateTowardsPosition(homeBase.army.GetRandomSpot(barracksBunk)));
				if (homeBase.army.AllAreHomeCheck())
					homeBase.army.isRepositioning = false;
            }
        }
        //enemy combat orders
        else if (enemyAI)
        {
            if (inBattle)
            {
				if (world.IsUnitLocationTaken(currentLocation))
					FindNewBattleSpot(currentLocation, targetLocation);
                else
					world.AddUnitPosition(currentLocation, this);

				enemyAI.AggroCheck();
            }
            else if (preparingToMoveOut)
            {
                world.AddUnitPosition(currentLocation, this);
                preparingToMoveOut = false;
                enemyCamp.EnemyReady(this);
            }
            else if (repositioning)
            {
				world.AddUnitPosition(currentLocation, this);
				repositioning = false;
                enemyCamp.EnemyReturn(this);
			}
            //else
            //{
            //    enemyAI.StateCheck(world);
            //}

            //enemyAI.needsDestination = true;
        }
        else if (world.IsUnitLocationTaken(currentLocation) && !followingRoute)
		{
			Unit unitInTheWay = world.GetUnit(currentLocation);

			if (unitInTheWay == this)
			{
				world.AddUnitPosition(currentLocation, this);
				TradeRouteCheck(endPosition);
				return;
			}

			Vector3Int loc;
			if (unitInTheWay.pathPositions.Count > 0)
				loc = unitInTheWay.pathPositions.Peek();
			else
				loc = new Vector3Int(0, -10, 0);

			FindNewSpot(currentLocation, loc);
			return;
		}
        else
        {
			world.AddUnitPosition(currentLocation, this);
            
            if (isSelected)
                world.unitMovement.ShowIndividualCityButtonsUI();

            if (isLaborer)
				if (world.RoundToInt(finalDestinationLoc) == endPosition)
					world.unitMovement.JoinCity(this);
        }
    }

    private void GoToBunk()
    {
        finalDestinationLoc = barracksBunk;
        MoveThroughPath(GridSearch.AStarSearch(world, CurrentLocation, barracksBunk, isTrader, bySea));
    }

 //   public void EnemyAttackSetup()
 //   {
 //       isMoving = false;
	//	currentLocation = world.RoundToInt(transform.position);

 //       if (world.IsUnitLocationTaken(currentLocation))
 //       {
 //           enemyAI.ReadjustAttack();
 //       }

	//	world.AddUnitPosition(currentLocation, this);
	//}

    public void FindNewSpot(Vector3Int current, Vector3Int? next)
    {
        //Vector3Int lastTile = current;        

        int i = 0;
        bool outerRing = false;
        foreach (Vector3Int tile in world.GetNeighborsFor(current, MapWorld.State.EIGHTWAYTWODEEP))
        {
            i++;
            if (i > 8)
                outerRing = true;

            if (isTrader && !world.IsRoadOnTileLocation(tile))
                continue;

            if (!world.CheckIfPositionIsValid(tile) || world.IsUnitLocationTaken(tile))
                continue;

            if (tile == next && tile != null) 
                continue;

            //if (world.IsUnitLocationTaken(tile))
            //{
            //    //lastTile = tile;
            //    continue;
            //}

            finalDestinationLoc = tile;

            if (outerRing)
                MoveThroughPath(new List<Vector3Int> { (current + tile) / 2, tile });
            else
                MoveThroughPath(new List<Vector3Int> { tile });
            return;
        }

        //if (newSpotTry < 2)
        //{
        //    newSpotTry++;
        //    FindNewSpot(lastTile); //keep going until finding new spot
        //}
    }

    public void FindNewBattleSpot(Vector3Int current, Vector3Int target)
    {
        Army army;

        if (inArmy)
            army = homeBase.army;
        else
            army = enemyCamp.attackingArmy;

        Vector3Int closestTile = current;
        float dist = 0;
        int i = 0;
        UnitType type = buildDataSO.unitType;

        foreach (Vector3Int tile in world.GetNeighborsFor(target, MapWorld.State.EIGHTWAY))
        {
            if (type == UnitType.Cavalry)
            {
                if (!army.cavalryRange.Contains(tile))
                    continue;
            }
            else
            {
                if (!army.movementRange.Contains(tile))
                    continue;
            }
            
            if (!world.CheckIfPositionIsValid(tile) || world.IsUnitLocationTaken(tile) || army.attackingSpots.Contains(tile))
                continue;

            if (i == 0)
            {
                i++;
                closestTile = tile;
                dist = Math.Abs(current.x - tile.x) + Math.Abs(current.z - tile.z);

                if (dist == 1)
                    break;
                continue;
            }

            float newDist = Math.Abs(current.x - tile.x) + Math.Abs(current.z - tile.z);
            if (newDist < dist)
            {
                closestTile = tile;
                dist = newDist;
                if (dist == 1)
                    break;
            }
        }

        if (dist <= 3f)
        {
            finalDestinationLoc = closestTile;
		    MoveThroughPath(new List<Vector3Int> { closestTile });
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

    private void Teleport(Vector3Int loc)
    {
        currentLocation = loc;
        transform.position = loc;
        world.AddUnitPosition(currentLocation, this);
        transform.rotation = Quaternion.LookRotation(finalDestinationLoc - transform.position, Vector3.up);
    }

    public void Reveal()
    {
        if (!CompareTag("Player"))
            return;
        
        Vector3Int pos = world.GetClosestTerrainLoc(transform.position);
        //prevTerrainTile = pos;
        TerrainData td = world.GetTerrainDataAt(pos);
        td.HardReveal();

        prevTerrainTile = pos;
        foreach (Vector3Int loc in world.GetNeighborsFor(pos, MapWorld.State.CITYRADIUS))
        {
            TerrainData td2 = world.GetTerrainDataAt(loc);
            if (td2.isDiscovered)
                continue;

            td2.HardReveal();
            focusCam.CheckLoc(loc);
            if (world.IsTradeCenterOnTile(loc))
                world.GetTradeCenter(loc).Reveal();
        }

        //RevealCheck(pos);
    }

    private void RevealCheck(Vector3Int pos)
    {
        prevTerrainTile = pos;

        //int i = 0;
        foreach (Vector3Int loc in world.GetNeighborsFor(pos, MapWorld.State.CITYRADIUS))
        {
            //i++;
            
            TerrainData td = world.GetTerrainDataAt(loc);
            if (!bySea)
            {
                if (td.enemyCamp)
                {
                    world.RevealEnemyCamp(loc);
                    
                    if (inArmy && homeBase.army.traveling)
                        world.BattleStations(loc, homeBase.army.attackZone);
                    //else
                    //    world.WakeUpCamp(loc, this);
                }
            }
            
            if (td.isDiscovered)
                continue;

            td.Reveal();
            focusCam.CheckLoc(loc);
            if (world.IsTradeCenterOnTile(loc))
                world.GetTradeCenter(loc).Reveal();
        }
    }

    public void StartAttack(Unit target)
    {
        targetSearching = false;
        Rotate(target.transform.position);
        attackCo = StartCoroutine(Attack(target));
	}

	public IEnumerator Attack(Unit target)
	{
        targetBunk = target.barracksBunk;
        attacking = true;

        if (target.targetSearching)
            target.enemyAI.StartAttack(this);

		while (target.currentHealth > 0)
		{
			transform.rotation = Quaternion.LookRotation(target.transform.position - transform.position);
			StartAttackingAnimation();
			yield return attackPauses[3];
			target.ReduceHealth(attackStrength, transform.eulerAngles, attacks[UnityEngine.Random.Range(0, attacks.Length)]);
			yield return attackPauses[UnityEngine.Random.Range(0,3)];
		}

        attacking = false;
        attackCo = null;
		StopMovement();
		StopAnimation();
        AggroCheck();
	}

    private IEnumerator RangedAttack(Unit target)
    {
        targetBunk = target.barracksBunk;
        attacking = true;
        Rotate(target.transform.position);

		while (target.currentHealth > 0)
        {
			transform.rotation = Quaternion.LookRotation(target.transform.position - transform.position);
			StartAttackingAnimation();
			yield return attackPauses[3];
			projectile.SetPoints(transform.position, target.transform.position);
            StartCoroutine(projectile.Shoot(this, target));
			yield return attackPauses[UnityEngine.Random.Range(0, 3)];
		}

        attackCo = null;
        attacking = false;
        StopAnimation();
        AggroCheck();
    }

    public void AggroCheck()
    {
		UnitType type = buildDataSO.unitType;
        if (attacking)
            return;

		if (!homeBase.army.FinishAttack())
		{
			if (type == UnitType.Infantry)
				InfantryAggroCheck();
			else if (type == UnitType.Ranged)
				RangedAggroCheck();
			else if (type == UnitType.Cavalry)
				CavalryAggroCheck();
		}
	}

    public void InfantryAggroCheck()
    {
		targetSearching = false;

		List<Vector3Int> attackingZones = new();

		Vector3Int forward = homeBase.army.forward;
		Vector3Int forwardTile = forward + currentLocation;

		attackingZones.Add(forwardTile);
		if (forward.z != 0)
		{
			attackingZones.Add(new Vector3Int(1, 0, forward.z) + currentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, forward.z) + currentLocation);
			attackingZones.Add(new Vector3Int(1, 0, 0) + currentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, 0) + currentLocation);
		}
		else
		{
			attackingZones.Add(new Vector3Int(forward.x, 0, 1) + currentLocation);
			attackingZones.Add(new Vector3Int(forward.x, 0, -1) + currentLocation);
			attackingZones.Add(new Vector3Int(0, 0, 1) + currentLocation);
			attackingZones.Add(new Vector3Int(0, 0, -1) + currentLocation);
		}

		foreach (Vector3Int zone in attackingZones)
		{
			if (!homeBase.army.movementRange.Contains(zone))
				continue;

			if (world.IsUnitLocationTaken(zone))
			{
				Unit enemy = world.GetUnit(zone);
				if (enemy.enemyAI)
				{
					//attacking = true;
					if (!homeBase.army.attackingSpots.Contains(currentLocation))
						homeBase.army.attackingSpots.Add(currentLocation);

					if (!attacking)
                    {
						StartAttack(enemy);
                    }
                    else
                    {
						if (enemy.targetSearching)
						    enemy.enemyAI.StartAttack(this);
                    }
				}
			}
		}

		if (attacking)
            return;

		Unit newEnemy = homeBase.army.FindClosestTarget(this);
		targetLocation = newEnemy.CurrentLocation;
		List<Vector3Int> path = homeBase.army.PathToEnemy(currentLocation, world.RoundToInt(newEnemy.transform.position));

		if (path.Count > 0)
		{
			homeBase.army.attackingSpots.Remove(currentLocation);

			//moving unit behind if stuck
			Vector3Int positionBehind = homeBase.army.forward * -1 + currentLocation;

			if (world.IsUnitLocationTaken(positionBehind))
			{
				Unit unitBehind = world.GetUnit(positionBehind);
				if (unitBehind.inArmy && unitBehind.targetSearching)
					unitBehind.AggroCheck();
			}

			if (path.Count >= 2)
			{
				List<Vector3Int> shortPath = new() { path[0] };
				finalDestinationLoc = shortPath[0];
				MoveThroughPath(shortPath);
				homeBase.army.attackingSpots.Add(path[0]);
			}
			else if (path.Count == 1)
				StartAttack(newEnemy);
		}
		else
		{
            Vector3Int scoochCloser = new Vector3Int(Math.Sign(targetLocation.x - currentLocation.x), 0, Math.Sign(targetLocation.z - currentLocation.z)) + currentLocation;
            
            if (homeBase.army.movementRange.Contains(scoochCloser) && !world.IsUnitLocationTaken(scoochCloser) && !homeBase.army.attackingSpots.Contains(scoochCloser))
			{
				finalDestinationLoc = scoochCloser;
				//List<Vector3Int> newPath = new() { scoochCloser };
				MoveThroughPath(new List<Vector3Int> { scoochCloser });
			}

			targetSearching = true;
		}
	}

    public void RangedAggroCheck()
    {
        Unit enemy = homeBase.army.FindClosestTarget(this);

        if (enemy != null)
    		attackCo = StartCoroutine(RangedAttack(enemy));
	}

	public void CavalryAggroCheck()
	{
		targetSearching = false;

		List<Vector3Int> attackingZones = new();

		Vector3Int forward = homeBase.army.forward;
		Vector3Int forwardTile = forward + currentLocation;

		attackingZones.Add(forwardTile);
		if (forward.z != 0)
		{
			attackingZones.Add(new Vector3Int(1, 0, forward.z) + currentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, forward.z) + currentLocation);
			attackingZones.Add(new Vector3Int(1, 0, 0) + currentLocation);
			attackingZones.Add(new Vector3Int(-1, 0, 0) + currentLocation);
		}
		else
		{
			attackingZones.Add(new Vector3Int(forward.x, 0, 1) + currentLocation);
			attackingZones.Add(new Vector3Int(forward.x, 0, -1) + currentLocation);
			attackingZones.Add(new Vector3Int(0, 0, 1) + currentLocation);
			attackingZones.Add(new Vector3Int(0, 0, -1) + currentLocation);
		}

		foreach (Vector3Int zone in attackingZones)
		{
			if (!homeBase.army.cavalryRange.Contains(zone))
				continue;

			if (world.IsUnitLocationTaken(zone))
			{
				Unit enemy = world.GetUnit(zone);
				if (enemy.enemyAI)
				{
					//attacking = true;
					if (!homeBase.army.attackingSpots.Contains(currentLocation))
						homeBase.army.attackingSpots.Add(currentLocation);

					if (!attacking)
                    {
                        if (flanking) //check for those sleeping if they can attack
                        {
							foreach (Unit unit in homeBase.army.targetCamp.UnitsInCamp)
							{
								if (unit.targetSearching)
									unit.enemyAI.AggroCheck();
							}
						}

                        flankedOnce = true; //can't flank if attacking front lines
                        flanking = false;
						StartAttack(enemy);
                    }
                    else
                    {
						if (enemy.targetSearching)
						    enemy.enemyAI.StartAttack(this);
                    }
				}
			}
		}

		if (attacking)
			return;

        Unit newEnemy = null;

        if (!flankedOnce) //only one flank per battle
        {
            flankedOnce = true;
            
            if ((world.IsUnitLocationTaken(forwardTile) && world.GetUnit(forwardTile).inArmy) || cavalryLine)
            {
                cavalryLine = false; //for subsequent battles
                newEnemy = homeBase.army.FindEdgeRanged(currentLocation);
            }
        }
        else
        {
            flanking = false;
        }

        if (newEnemy == null)
            newEnemy = homeBase.army.FindClosestTarget(this);
        else
            flanking = true;

        targetLocation = newEnemy.CurrentLocation;
		List<Vector3Int> path = homeBase.army.CavalryPathToEnemy(currentLocation, world.RoundToInt(newEnemy.transform.position));

		if (path.Count > 0)
		{
			homeBase.army.attackingSpots.Remove(currentLocation);

			//moving unit behind if stuck
			Vector3Int positionBehind = homeBase.army.forward * -1 + currentLocation;

			if (world.IsUnitLocationTaken(positionBehind))
			{
				Unit unitBehind = world.GetUnit(positionBehind);
                if (unitBehind.inArmy && unitBehind.targetSearching)
                    unitBehind.AggroCheck();
                else if (unitBehind.inArmy && unitBehind.buildDataSO.unitType == UnitType.Cavalry && !unitBehind.flankedOnce)
                    unitBehind.cavalryLine = true;
			}

            if (flanking)
            {
                if (path.Count > 1)
                    path.RemoveAt(path.Count - 1); //remove last one
                finalDestinationLoc = path[path.Count - 1];
                MoveThroughPath(path);
            }
			else if (path.Count >= 2)
			{
				List<Vector3Int> shortPath = new() { path[0] };
				finalDestinationLoc = shortPath[0];
				MoveThroughPath(shortPath);
				homeBase.army.attackingSpots.Add(path[0]);
			}
			else if (path.Count == 1)
				StartAttack(newEnemy);
		}
		else
		{
			Vector3Int scoochCloser = new Vector3Int(Math.Sign(targetLocation.x - currentLocation.x), 0, Math.Sign(targetLocation.z - currentLocation.z)) + currentLocation;

			if (homeBase.army.movementRange.Contains(scoochCloser) && !world.IsUnitLocationTaken(scoochCloser) && !homeBase.army.attackingSpots.Contains(scoochCloser))
			{
				finalDestinationLoc = scoochCloser;
				//List<Vector3Int> newPath = new() { scoochCloser };
				MoveThroughPath(new List<Vector3Int> { scoochCloser });
			}

			targetSearching = true;
		}
	}

	public void StopAttacking()
    {
        if (inArmy && attackCo != null)
            StopCoroutine(attackCo);
        attackCo = null;
		attacking = false;
		inBattle = false;
        flankedOnce = false;
        flanking = false;
        targetSearching = false;
        isMarching = false;
        StopMovement();
		StopAnimation();
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

    //if space is occupied with something
    public virtual void SkipRoadBuild()
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

        if (!bySea)
        {
            if (collision.gameObject.CompareTag("Forest") || collision.gameObject.CompareTag("Forest Hill") || collision.gameObject.CompareTag("City"))
            {
                marker.ToggleVisibility(true);
            }
            else
            {
                marker.ToggleVisibility(false);
            }
        }
        
        //if (collision.gameObject.CompareTag("City"))
        //{
        //    onTop = true;
        //    Debug.Log("worked");
        //    mesh.layer = LayerMask.NameToLayer("Agent");
        //}

        //if (isTrader && !bySea && !world.hideTerrain)
        //    return;

        //if (collision.transform.position != Vector3Int.zero)
        //    RevealCheck(world.GetTerrainDataAt(world.RoundToInt(collision.transform.position)).TileCoordinates);
    }

    private void OnCollisionStay(Collision collision)
    {
        //Debug.Log("colliding with " + collision.gameObject.tag);

        //threshold = 0.001f;

        if (isMarching)
        {
            moveSpeed = baseSpeed * flatlandSpeed * .05f;
            unitAnimator.SetFloat("speed", baseSpeed * 18f);
            return;
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
        //else if (collision.gameObject.CompareTag("Player"))
        //{
        //    unitRigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
        //}
    }

    //private void OnCollisionExit(Collision collision)
    //{
    //    if (collision.gameObject.CompareTag("Player"))
    //    {
    //        unitRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
    //    }
    //}

    //public void CheckForSpotAvailability()
    //{

    //}

    public void SoftSelect(Color color)
    {
        highlight.EnableHighlight(color);
    }

    //Methods for selecting and unselecting unit
    public void Select(Color color)
    {
        //selectionCircle.enabled = true;
        //highlight.ToggleGlow(true, Color.white);
        isSelected = true;
        if (bySea)
            selectionCircle.SetActive(true);
        highlight.EnableHighlight(color);
        //CenterCamera();
    }

    public void Deselect()
    {
        //selectionCircle.enabled = false;
        //highlight.ToggleGlow(false, Color.white);
        isSelected = false;
        if (bySea)
            selectionCircle.SetActive(false); 
        highlight.DisableHighlight();
    }

    //public void FinishMovement() //Stops all movement, run when performing certain actions
    //{
    //    Deselect();
    //    turnHandler.GoToNextUnit();
    //}

    public void PlayAudioClip(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.Play();
    }

    public void KillUnit(Vector3 rotation)
    {
        isDead = true;
        attacking = false;
        targetSearching = false;
        isMarching = false;
        inBattle = false;

        deathSplash.transform.position = transform.position;
        rotation.x = -90;
        deathSplash.transform.eulerAngles = rotation;
        deathSplash.Play();
        audioSource.clip = kills[UnityEngine.Random.Range(0, kills.Length)];
        audioSource.Play();

        //gameObject.SetActive(false);
        unitMesh.gameObject.SetActive(false);
        healthbar.gameObject.SetActive(false);
        Vector3 sixFeetUnder = transform.position;
        sixFeetUnder.y -= 6f;
        transform.position = sixFeetUnder;
        unitRigidbody.useGravity = false;

		if (enemyAI)
        {
            //enemyCamp.UnitsInCamp.Remove(this);
            enemyCamp.deathCount++;
            enemyCamp.attackingArmy.attackingSpots.Remove(currentLocation);
            enemyCamp.ClearCampCheck();
			world.RemoveUnitPosition(currentLocation);

            foreach (Unit unit in enemyCamp.UnitsInCamp)
            {
                if (unit.targetSearching)
                    unit.enemyAI.AggroCheck();
            }
			//Vector3Int tileBehind = currentLocation + enemyCamp.forward * -1;
			//if (world.IsUnitLocationTaken(tileBehind))
			//{
			//	Unit unitBehind = world.GetUnit(tileBehind);
			//	if (unitBehind.enemyAI)
			//		unitBehind.enemyAI.AggroCheck();
			//}

            if (isSelected)
				world.unitMovement.ClearSelection();

			enemyCamp.DeadList.Add(this);
		}
        else
        {
            minimapIcon.gameObject.SetActive(false);
            //homeBase.army.deathCount++;
            homeBase.army.UnitsInArmy.Remove(this);
			homeBase.army.attackingSpots.Remove(currentLocation);
			RemoveUnitFromData();
            //homeBase.army.TargetCheck();

            foreach (Unit unit in homeBase.army.UnitsInArmy)
            {
                if (unit.targetSearching)
                    unit.AggroCheck();
            }
            //Vector3Int tileBehind = currentLocation + homeBase.army.forward * -1;
            //if (world.IsUnitLocationTaken(tileBehind))
            //{
            //    Unit unitBehind = world.GetUnit(tileBehind);
            //    if (unitBehind.inArmy)
            //        unitBehind.AggroCheck();
            //}

		    if (isSelected)
		    {
			    if (homeBase.army.UnitsInArmy.Count > 0)//armyCount isn't changed until after battle
				{
                    Unit nextUnitUp = homeBase.army.GetNextLivingUnit();
                    if (nextUnitUp != null)
    				    world.unitMovement.PrepareMovement(nextUnitUp);
                    else
						world.unitMovement.ClearSelection();
				}
                else
                {
                    world.somethingSelected = false;
				    world.unitMovement.ClearSelection();
                }
		    }

			homeBase.army.RemoveFromArmy(this, barracksBunk);
			homeBase.army.DeadList.Add(this);
		}

		Deselect();

		//if (enemyAI)
  //          enemyCamp.DeadList.Add(this);
  //      else
  //      {
  //          homeBase.army.RemoveFromArmy(this, barracksBunk);
  //          homeBase.army.DeadList.Add(this);
  //          if (world.uiCampTooltip.activeStatus)
  //              world.uiCampTooltip.RefreshData();
  //      }
    }

    public void DestroyUnit()
    {
		RemoveUnitFromData();
		Deselect();
		Destroy(gameObject);
	}


    //Methods for movement order information 
    //displays movement orders when selected
    public List<Vector3Int> GetContinuedMovementPath() 
    {
        List<Vector3Int> continuedOrdersPositions = new(pathPositions);

        //foreach (TerrainData td in pathPositions)
        //{
        //    continuedOrdersPositions.Add(td.GetTileCoordinates());
        //}

        return continuedOrdersPositions;
    }

    public void ResetMovementOrders()
    {
        pathPositions.Clear();
        moreToMove = false;
    }

    public void RemoveUnitFromData()
    {
        ResetMovementOrders();
        turnHandler.turnHandler.RemoveUnitFromTurnList(this);
        world.RemoveUnitPosition(currentLocation);
    }

    public void ShowContinuedPath()
    {
        queueCount = 1;
        ShowPath(new List<Vector3Int>(pathPositions));
    }

    private void ShowPath(List<Vector3Int> currentPath, bool queued = false)
    {
        //List<Vector3Int> currentPath = new(pathPositions);
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

            if (isWorker)
                path = movementSystem.GetFromShoePrintPool();
            else
                path = movementSystem.GetFromChevronPool();

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

            //squishing the sprite a little for straights
            //Vector3 scale = path.transform.localScale; 
            //scale.x += x;
            //path.transform.localScale = scale;

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

    private void DequeuePath()
    {
        GameObject path = pathQueue.Dequeue();
        if (isWorker)
        {
            movementSystem.AddToShoePrintPool(path);
        }
        else
        {
            movementSystem.AddToChevronPool(path);
        }
    }

    public UnitData SaveMilitaryUnitData()
    {
        UnitData data = new();

        data.unitNameAndLevel = buildDataSO.unitNameAndLevel;
        data.position = transform.position;
        data.rotation = transform.rotation;
        data.destinationLoc = destinationLoc;
        data.finalDestinationLoc = finalDestinationLoc;
        data.currentLocation = currentLocation;
        data.prevTerrainTile = prevTerrainTile;
        data.moveOrders = pathPositions.ToList();
        data.isMoving = isMoving;

        if (isMoving && readyToMarch)
            data.moveOrders.Insert(0, world.RoundToInt(destinationLoc));

        data.moreToMove = moreToMove;
        data.somethingToSay = somethingToSay;
        data.isUpgrading = isUpgrading;

        //combat
        if (inArmy || enemyAI)
        {
            if (inArmy)
            {
                data.cityHomeBase = homeBase.cityLoc;
                data.transferring = transferring;
                data.repositioning = repositioning;
            }
            else
            {
                data.campSpot = enemyAI.CampSpot;
            }

            data.barracksBunk = barracksBunk;
            data.marchPosition = marchPosition;
            data.targetBunk = targetBunk;
            data.currentHealth = currentHealth;
            data.baseSpeed = baseSpeed;
            data.isLeader = isLeader;
            data.readyToMarch = readyToMarch;
            data.atHome = atHome;
            data.preparingToMoveOut = preparingToMoveOut;
            data.isMarching = isMarching;
            data.inBattle = inBattle;
            data.attacking = attacking;
            data.targetSearching = targetSearching;
            data.flanking = flanking;
            data.flankedOnce = flankedOnce;
            data.cavalryLine = cavalryLine;
            data.isDead = isDead;
        }

        return data;
    }

    public void LoadUnitData(UnitData data)
    {
        transform.position = data.position;
        transform.rotation = data.rotation;
        destinationLoc = data.destinationLoc;
        finalDestinationLoc = data.finalDestinationLoc;
        currentLocation = data.currentLocation;
        prevTerrainTile = data.prevTerrainTile;
        isMoving = data.isMoving;
        moreToMove = data.moreToMove;

        somethingToSay = data.somethingToSay;
        isUpgrading = data.isUpgrading;

        //if (somethingToSay)
        if (inArmy || enemyAI)
        {
            if (inArmy)
            {
                transferring = data.transferring;
                repositioning = data.repositioning;
            }
            else
            {
                enemyAI.CampSpot = data.campSpot;
            }
            
            barracksBunk = data.barracksBunk;
            marchPosition = data.marchPosition;
            targetBunk = data.targetBunk;
            currentHealth = data.currentHealth;

            if (currentHealth < healthMax)
            {
                healthbar.LoadHealthLevel(currentHealth);
                healthbar.gameObject.SetActive(true);
            }

            baseSpeed = data.baseSpeed; //coroutine
            isLeader = data.isLeader;
            readyToMarch = data.readyToMarch;
            atHome = data.atHome;
            preparingToMoveOut = data.preparingToMoveOut;
            isMarching = data.isMarching;
            inBattle = data.inBattle;
            attacking = data.attacking;

            targetSearching = data.targetSearching;
            flanking = data.flanking;
            flankedOnce = data.flankedOnce;
            cavalryLine = data.cavalryLine;
            isDead = data.isDead;

			GameLoader.Instance.attackingUnitList.Add(this);
		}

		if (isMoving)
		{
            Vector3Int endPosition = world.RoundToInt(finalDestinationLoc);
			
            if (!readyToMarch)
            {
				if (data.moveOrders.Count == 0)
					data.moveOrders.Add(endPosition);

				pathPositions = new Queue<Vector3Int>(data.moveOrders);

				StartCoroutine(WaitForOthers(endPosition));
                return;
            }
            
            if (data.moveOrders.Count == 0)
				data.moveOrders.Add(endPosition);

			MoveThroughPath(data.moveOrders);
		}
	}

    public void LoadAttack()
    {
		if (attacking)
        {
            Unit target = null;
		    if (enemyAI)
		    {
			    List<Unit> units = enemyCamp.attackingArmy.UnitsInArmy;

			    for (int i = 0; i < units.Count; i++)
			    {
				    if (units[i].barracksBunk == targetBunk)
				    {
					    target = units[i];
					    break;
				    }
			    }
		    }
		    else
		    {
			    List<Unit> units = homeBase.army.targetCamp.UnitsInCamp;

			    for (int i = 0; i < units.Count; i++)
			    {
				    if (units[i].barracksBunk == targetBunk)
				    {
					    target = units[i];
					    break;
				    }
			    }
		    }

		    if (target == null)
		    {
			    attacking = false;
                if (inArmy)
                    AggroCheck();
                else
                    enemyAI.AggroCheck();
            
                return;
		    }

		    if (inArmy)
            {
                if (buildDataSO.unitType == UnitType.Ranged)
			        attackCo = StartCoroutine(RangedAttack(target));
		        else
			        attackCo = StartCoroutine(Attack(target));
            }
            else
            {
			    if (buildDataSO.unitType == UnitType.Ranged)
				    attackCo = StartCoroutine(enemyAI.RangedAttack(target));
			    else
				    attackCo = StartCoroutine(enemyAI.Attack(target));
		    }
        }
        else if (isDead)
        {
			unitMesh.gameObject.SetActive(false);
			healthbar.gameObject.SetActive(false);
            Vector3 sixFeetUnder = transform.position;
            sixFeetUnder.y -= 6;
            transform.position = sixFeetUnder;
            unitRigidbody.useGravity = false;

			if (enemyAI)
			{
				enemyCamp.deathCount++;
				enemyCamp.attackingArmy.attackingSpots.Remove(currentLocation);
				enemyCamp.ClearCampCheck();
				world.RemoveUnitPosition(currentLocation);
				enemyCamp.DeadList.Add(this);
			}
			else
			{
				minimapIcon.gameObject.SetActive(false);
				homeBase.army.UnitsInArmy.Remove(this);
				homeBase.army.attackingSpots.Remove(currentLocation);
				RemoveUnitFromData();
				homeBase.army.RemoveFromArmy(this, barracksBunk);
				homeBase.army.DeadList.Add(this);
				world.RemoveUnitPosition(currentLocation);
			}
		}
	}

    private IEnumerator WaitForOthers(Vector3Int endPosition)
    {
		while (!readyToMarch)
		{
			yield return null;
		}
		unitAnimator.SetBool(isMarchingHash, true);

		if (pathPositions.Count > 0)
		{
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
}
