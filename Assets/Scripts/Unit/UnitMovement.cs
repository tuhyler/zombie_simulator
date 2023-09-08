using System.Collections.Generic;
using System.Resources;
using UnityEngine;
using static UnityEditor.FilePathAttribute;

public class UnitMovement : MonoBehaviour
{
    [SerializeField]
    public MapWorld world;
    [SerializeField]
    private CameraController focusCam;
    [SerializeField]
    private UIUnitTurnHandler turnHandler;
    [SerializeField]
    public InfoManager infoManager;
    [SerializeField]
    private WorkerTaskManager workerTaskManager;
    [SerializeField]
    public UISingleConditionalButtonHandler uiCancelMove, uiJoinCity, uiMoveUnit, uiCancelTask, uiConfirmOrders, uiDeployArmy, uiSwapPosition, uiChangeCity; 
    [SerializeField]
    public UITraderOrderHandler uiTraderPanel;
    [SerializeField]
    public UIWorkerHandler uiWorkerTask;
    [SerializeField]
    public UIPersonalResourceInfoPanel uiPersonalResourceInfoPanel;
    [SerializeField]
    public UIPersonalResourceInfoPanel uiCityResourceInfoPanel;
    [SerializeField]
    private UITradeRouteManager uiTradeRouteManager;
    [SerializeField]
    private UIBuildingSomething uiBuildingSomething;
    //[SerializeField]
    //private UISingleConditionalButtonHandler uiCancelTradeRoute;

    [SerializeField]
    private ParticleSystem starshine;

    private bool queueMovementOrders;
    private MovementSystem movementSystem;

    private Unit selectedUnit;
    private Worker selectedWorker;
    public Worker SelectedWorker { get { return selectedWorker; } set { selectedWorker = value; } }
    private Trader selectedTrader;
    //private TerrainData selectedTile;
    //private InfoProvider selectedUnitInfoProvider;
    private bool loadScreenSet; //flag if load/unload ui is showing
    private bool moveUnit;

    //for transferring cargo to/from trader
    private ResourceManager cityResourceManager;
    private Wonder wonder;
    private TradeCenter tradeCenter;
    public int cityTraderIncrement = 1;

    //for worker orders
    [HideInInspector]
    public bool buildingRoad, removingAll, removingRoad, removingLiquid, removingPower, unitSelected, swappingArmy, deployingArmy, changingCity; 
    //private List<TerrainData> highlightedTiles = new();

    private void Awake()
    {
        movementSystem = GetComponent<MovementSystem>();
        movementSystem.GrowObjectPools(this);
    }

    private void Start()
    {
        starshine = Instantiate(starshine, new Vector3(0, 0, 0), Quaternion.identity);
        starshine.Pause();
    }

    public void HandleEsc()
    {
        if (world.buildingWonder)
            world.CloseBuildingSomethingPanel();
        
        if (selectedUnit != null && !selectedUnit.isBusy)
        {
            if (selectedUnit.isMoving)
            {
                CancelContinuedMovementOrders();
            }
            else if (selectedUnit.followingRoute)
            {
                CancelTradeRoute();
            }
        }
    }

    public void HandleEnter()
    {
        if (selectedWorker != null && world.unitOrders)
            ConfirmWorkerOrders();
    }

    public void HandleB()
    {
        if (uiJoinCity.activeStatus)
            JoinCity();
    }

    public void CenterCamOnUnit()
    {
        if (selectedUnit != null)
            selectedUnit.CenterCamera();
    }

    public void HandleUnitSelectionAndMovement(Vector3 location, GameObject detectedObject)
    {
        if (world.buildingWonder)
            return;

        if (selectedUnit != null && selectedUnit.sayingSomething)
            SpeakingCheck();
        //else if (loadScreenSet)
        //    LoadUnloadFinish(false);

        //if nothing detected, nothing selected
        if (detectedObject == null)
        {
            selectedUnit = null;
            selectedTrader = null;
            return;
        }

        world.CloseResearchTree();
        world.CloseWonders();
        world.CloseTerrainTooltip();
        world.CloseImprovementTooltip();
        location.y = 0;

        Vector3Int pos = world.GetClosestTerrainLoc(location);

        //if building road, can't select anything else
        if (world.unitOrders)
        {
            TerrainData td = world.GetTerrainDataAt(pos);
            if (!td.isDiscovered)
                return;
            
            if (buildingRoad)
            {
                if (world.IsRoadOnTerrain(pos) || world.IsBuildLocationTaken(pos))
                {
                    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Already something here");
                }
                else if (!td.walkable)
                {
                    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't build here");
                }
                else if (td.terrainData.type == TerrainType.River)
                {
                    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Research bridge building first");
                }
                else if (world.CheckIfEnemyTerritory(pos))
                {
					UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Not in enemy territory");
				}
                else
                {
                    if (selectedWorker.AddToOrderQueue(pos))
                    {
                        if (selectedWorker.IsOrderListMoreThanZero())
                            uiConfirmOrders.ToggleTweenVisibility(true);

                        td.EnableHighlight(Color.white);
                        //highlightedTiles.Add(td);
                    }
                    else
                    {
                        if (!selectedWorker.IsOrderListMoreThanZero())
                            uiConfirmOrders.ToggleTweenVisibility(false);

                        td.DisableHighlight();
                        //highlightedTiles.Remove(td);
                    }
                }
            }
            else if (removingRoad)
            {
                if (!world.IsRoadOnTerrain(pos))
                {
                    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "No road here");
                }
                else if (world.IsCityOnTile(pos) || world.IsWonderOnTile(pos) || world.IsTradeCenterOnTile(pos))
                {
                    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't remove this");
                }
                else
                {
                    if (selectedWorker.AddToOrderQueue(pos))
                    {
                        if (selectedWorker.IsOrderListMoreThanZero())
                            uiConfirmOrders.ToggleTweenVisibility(true);

                        td.EnableHighlight(Color.red);
                        foreach (Road road in world.GetAllRoadsOnTile(pos))
                        {
                            if (road == null)
                                continue;
                            road.MeshFilter.gameObject.SetActive(true);
                            road.Embiggen();
                            road.SelectionHighlight.EnableHighlight(Color.white);
                        }
                        //highlightedTiles.Add(td);
                    }
                    else
                    {
                        if (!selectedWorker.IsOrderListMoreThanZero())
                            uiConfirmOrders.ToggleTweenVisibility(false);

                        td.DisableHighlight();
                        foreach (Road road in world.GetAllRoadsOnTile(pos))
                        {
                            if (road == null)
                                continue;
                            road.MeshFilter.gameObject.SetActive(false);
                            road.SelectionHighlight.DisableHighlight();
                        }
                        //highlightedTiles.Remove(td);
                    }
                }
            }
            //moving positions within barracks
            else if (swappingArmy)
            {
				Vector3Int loc = world.RoundToInt(location);

				if (selectedUnit.isMoving)
                {
					UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Not ready yet");
				}
                else if (selectedUnit.homeBase.army.CheckIfInBase(loc))
                {
                    if (loc == selectedUnit.CurrentLocation)
                        return;

                    bool swapping = false;
                    selectedUnit.repositioning = true;

                    if (world.IsUnitLocationTaken(loc))
                    {
                        Unit unit = world.GetUnit(loc);
                        swapping = true;
                        unit.barracksBunk = selectedUnit.CurrentLocation;

                        if (unit.atHome)
                        {
                            unit.finalDestinationLoc = selectedUnit.CurrentLocation;
						    unit.MoveThroughPath(GridSearch.AStarSearch(world, loc, selectedUnit.CurrentLocation, false, unit.bySea));
                        }
					}
                    else
                    {
                        foreach (Unit unit in selectedUnit.homeBase.army.UnitsInArmy)
                        {
                            if (unit == selectedUnit)
                                continue;

                            if (unit.barracksBunk == loc)
                            {
                                swapping = true;
                                unit.barracksBunk = selectedUnit.CurrentLocation;

                                if (unit.isMoving)
                                {
									unit.StopAnimation();
									unit.ShiftMovement();
								}

                                unit.finalDestinationLoc = selectedUnit.CurrentLocation;
                                unit.MoveThroughPath(GridSearch.AStarSearch(world, loc, selectedUnit.CurrentLocation, false, unit.bySea));

								break;
                            }
                        }
                    }

                    if (!swapping)
					    selectedUnit.homeBase.army.UpdateLocation(selectedUnit.CurrentLocation, loc);
                    selectedUnit.barracksBunk = loc;
                    selectedUnit.finalDestinationLoc = loc;
					selectedUnit.MoveThroughPath(GridSearch.AStarSearch(world, selectedUnit.CurrentLocation, loc, false, selectedUnit.bySea));
                }
                else
                {
					UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Outside of barracks");
				}
            }
            //going to attack
            else if (deployingArmy)
            {
                if (world.CheckIfEnemyCamp(pos))
                {
                    if (world.CheckIfEnemyAlreadyAttacked(pos))
                    {
						UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Already sending troops");
						return;
                    }
                    
                    if (selectedUnit.homeBase.army.MoveArmy(world, world.GetClosestTerrainLoc(selectedUnit.CurrentLocation), pos, true))
                    {
    					uiBuildingSomething.ToggleVisibility(false);
						world.UnhighlightAllEnemyCamps();
                        world.citySelected = true;
						world.unitOrders = false;
						deployingArmy = false;
                        world.SetEnemyCampAsAttacked(pos, selectedUnit.homeBase.army);
                        selectedUnit.homeBase.army.targetCamp = world.GetEnemyCamp(pos);
					}
				}
                else
                {
					UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Selected enemy camp");
				}
            }
            //changing barracks
            else if (changingCity)
            {
                if (world.IsCityOnTile(pos) && world.GetCity(pos).highlighted)
                {
                    City newCity = world.GetCity(pos);
                    Vector3Int newLoc = newCity.army.GetAvailablePosition();
                    List<Vector3Int> path = GridSearch.AStarSearch(world, selectedUnit.CurrentLocation, newLoc, false, selectedUnit.bySea);

                    if (path.Count > 0)
                    {
                        selectedUnit.homeBase = newCity; 
                        selectedUnit.atHome = false;
                        newCity.army.AddToArmy(selectedUnit);
                        selectedUnit.barracksBunk = newLoc;
                        selectedUnit.transferring = true;
                        selectedUnit.homeBase.army.isTransferring = true;

						selectedUnit.finalDestinationLoc = newLoc;
                        selectedUnit.MoveThroughPath(path);
                        world.citySelected = true;

						world.UnhighlightCitiesWithBarracks();
                        uiChangeCity.ToggleTweenVisibility(true);
						uiBuildingSomething.ToggleVisibility(false);
                        uiCancelTask.ToggleTweenVisibility(false);
                        world.unitOrders = false;
						changingCity = false;
                    }
                }
                else
                {
					UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Select new city with room in barracks");
				}
            }

            return;            
        }
        
        //moving unit upon selection
        if (moveUnit && selectedUnit != null) //detectedObject.TryGetComponent(out TerrainData terrainSelected) && selectedUnit != null)
        {
            if (selectedUnit.isBusy)
                return;

            //location.y = 0;
            //TerrainData terrainSelected = world.GetTerrainDataAt(Vector3Int.RoundToInt(location));
            MoveUnit(location, detectedObject);
        }
        else if (detectedObject.TryGetComponent(out Unit unitReference))
        {
            if (unitReference.CompareTag("Player"))
                SelectUnitPrep(unitReference, location);
            else if (unitReference.CompareTag("Enemy"))
                SelectEnemy(unitReference);
        }
        else if (detectedObject.TryGetComponent(out UnitMarker unitMarker))
        {
            Unit unit = unitMarker.Unit;

            if (unitMarker.CompareTag("Player"))
                SelectUnitPrep(unit, location);
            else if (unitMarker.CompareTag("Enemy"))
                SelectEnemy(unit);
        }
        else if (detectedObject.TryGetComponent(out Resource resource))
        {
            Worker tempWorker = resource.GetHarvestingWorker();

            if (tempWorker == null)
            {
                ClearSelection();
            }
            else if (selectedWorker != null && selectedWorker == tempWorker)
            {
                selectedWorker.SendResourceToCity();
            }
            else
            {
                ClearSelection();
                tempWorker.SendResourceToCity();
                selectedUnit = tempWorker;
                uiMoveUnit.ToggleTweenVisibility(true);
                SelectWorker();
                PrepareMovement();
            }
        }
        else
        {
            ClearSelection();
        }
    }

    private void SelectLaborer()
    {
        if (selectedUnit.isLaborer)
        {
            Laborer laborer = selectedUnit.GetComponent<Laborer>();
            if (laborer.co != null)
            {
                StopCoroutine(laborer.Celebrate());
                laborer.StopLaborAnimations();
            }
        }
    }

    private void SelectUnitPrep(Unit unitReference, Vector3 location)
    {
        if (selectedUnit == unitReference) //Unselect when clicking same unit
        {
            if (selectedWorker != null && selectedWorker.harvested)
                selectedWorker.SendResourceToCity();
            else if (selectedUnit.inArmy)
                selectedUnit.homeBase.army.UnselectArmy(selectedUnit);
            else
                ClearSelection();

            return;
        }
        else if (selectedUnit != null) //Change to a different unit
        {
            ClearSelection();
            selectedUnit = unitReference;
        }
        else //Select unit for the first time
        {
            selectedUnit = unitReference;
        }

        if (unitReference.somethingToSay)
        {
            unitReference.somethingToSay = false;
            unitReference.sayingSomething = true;
            world.PlayMessage(location);
            CenterCamOnUnit();
        }

        if (!selectedUnit.inArmy)
            uiMoveUnit.ToggleTweenVisibility(true);
        
        SelectLaborer();
        SelectWorker();
        SelectTrader();
        PrepareMovement();
    }

    private void SelectEnemy(Unit unitReference)
    {
		if (selectedUnit == unitReference) //Unselect when clicking same unit
		{
			ClearSelection();
			return;
		}
		else if (selectedUnit != null) //Change to a different unit
		{
			ClearSelection();
			selectedUnit = unitReference;
		}
		else //Select unit for the first time
		{
			selectedUnit = unitReference;
		}

		//if (unitReference.somethingToSay)
		//{
		//	unitReference.somethingToSay = false;
		//	unitReference.sayingSomething = true;
		//	world.PlayMessage(location);
		//	CenterCamOnUnit();
		//}
		world.somethingSelected = true;
		selectedUnit.Select(Color.red);
		infoManager.ShowInfoPanel(selectedUnit.buildDataSO, selectedUnit.currentHealth);
	}

    private void SelectWorker()
    {
        if (selectedUnit.isWorker)
        {
            selectedWorker = selectedUnit.GetComponent<Worker>();
            workerTaskManager.SetWorkerUnit(selectedWorker);
            uiWorkerTask.ToggleVisibility(true, null, world);
            if (selectedWorker.IsOrderListMoreThanZero())
                ToggleOrderHighlights(true);

            if (selectedWorker.harvested) //if unit just finished harvesting something, send to closest city
                selectedWorker.SendResourceToCity();
        }
    }

    private void SelectTrader()
    {
        if (selectedUnit.isTrader)
        {
            selectedTrader = selectedUnit.GetComponent<Trader>();
            if (selectedUnit.interruptedRoute)
                selectedUnit.InterruptedRouteMessage();
            //uiPersonalResourceInfoPanel.PrepareResourceUI(selectedTrader.resourceGridDict);
            uiPersonalResourceInfoPanel.SetTitleInfo(selectedTrader.name,
                selectedTrader.personalResourceManager.GetResourceStorageLevel, selectedTrader.cargoStorageLimit);
            uiPersonalResourceInfoPanel.ToggleVisibility(true, selectedTrader);
            world.traderCanvas.gameObject.SetActive(true);
            uiTraderPanel.ToggleVisibility(true);
            if (world.IsTradeLocOnTile(world.RoundToInt(selectedTrader.transform.position)) && !selectedTrader.followingRoute)
            {
                uiTraderPanel.uiLoadUnload.ToggleInteractable(true);
            }
            else
            {
                uiTraderPanel.uiLoadUnload.ToggleInteractable(false);
            }

            if (selectedTrader.hasRoute/* && !selectedTrader.followingRoute && !selectedTrader.interruptedRoute*/)
            {
                uiTraderPanel.uiBeginTradeRoute.ToggleInteractable(true);

                if (selectedTrader.followingRoute)
                    uiTraderPanel.SwitchRouteIcons(true);
            }

            //if (selectedTrader.followingRoute)
            //{
            //    uiCancelTradeRoute.ToggleTweenVisibility(true);
            //}
        }
    }

    public void PrepareMovement(Unit unit) //handling unit selection through the unit turn buttons
    {
        if (selectedUnit != null) //clearing selection if a new unit is clicked
        {
            ClearSelection();
        }

        selectedUnit = unit;

        SelectLaborer();
        SelectWorker();
        SelectTrader();
        PrepareMovement();
    }

    private void PrepareMovement()
    {
        //Debug.Log("Sel. unit is " + selectedUnit.name);
        world.somethingSelected = true;
        unitSelected = true;
        if (selectedUnit.inArmy)
			selectedUnit.homeBase.army.SelectArmy(selectedUnit);
		else
			selectedUnit.Select(Color.white);

        if (selectedUnit.isLaborer)
        {
            if (world.cityBuilderManager.SelectedCity != null)
                world.laborerSelected = true;
            
            world.HighlightCitiesAndWonders();
        }

        turnHandler.SetIndex(selectedUnit);
        //selectedUnitInfoProvider = selectedUnit.GetComponent<InfoProvider>(); //getting the information to show in info panel
        infoManager.ShowInfoPanel(selectedUnit.buildDataSO, selectedUnit.currentHealth);
        if (selectedUnit.moreToMove && !selectedUnit.inArmy)
        {
            uiCancelMove.ToggleTweenVisibility(true);
            //movementSystem.ShowPathToMove(selectedUnit);
            selectedUnit.ShowContinuedPath();
        }
        else if (selectedUnit.inArmy)
        {
            if (selectedUnit.homeBase.army.traveling)
                uiCancelTask.ToggleTweenVisibility(true);
            //else if (selectedUnit.homeBase.army.returning)
            //    uiDeployArmy.ToggleTweenVisibility(true);
            else if (selectedUnit.transferring)
                uiChangeCity.ToggleTweenVisibility(true);
        }

        ShowIndividualCityButtonsUI();
    }

    public void HandleSelectedLocation(Vector3 location, Vector3Int terrainPos, Unit unit)
    {
        if (!selectedUnit.CompareTag("Player"))
            return;

        if (queueMovementOrders /*&& unit.FinalDestinationLoc != location*/ && unit.isMoving)
        {
            if (unit.finalDestinationLoc == location)
                return;
            
            movementSystem.AppendNewPath(selectedUnit);
            //movementSystem.GetPathToMove(world, selectedUnit, terrainPos, isTrader); //Call AStar movement
            //movementSystem.ClearPaths();
            //return;
        }
        else if (unit.isMoving)
        {
            selectedUnit.ResetMovementOrders();
        }


        //selectedTile = terrainSelected; //sets selectedTile value
        //bool isTrader = selectedTrader != null;
        //movementSystem.GetPathToMove(world, selectedUnit, terrainPos, isTrader); //Call AStar movement
        //else //moving unit
        //{
        //    //if (movementSystem.CurrentPathLength == 0)
        //    //{
        //    //    Debug.Log("No defined path.");
        //    //    return;
        //    //}

        //    uiJoinCity.ToggleTweenVisibility(false);

        //    movementSystem.MoveUnitToggle(selectedUnit, world);
        //    movementSystem.HidePath();
        //    movementSystem.ClearPaths();
        //    //selectedTile = null;
        //}

        //bool isTrader = selectedTrader != null;

        //unit.FinishedMoving.AddListener(ShowIndividualCityButtonsUI);
        
        movementSystem.GetPathToMove(world, unit, terrainPos, unit.isTrader); //Call AStar movement

        unit.finalDestinationLoc = location;
        //uiJoinCity.ToggleTweenVisibility(false);
        if (unit.isBusy)
            uiCancelMove.ToggleTweenVisibility(false);
        else
            uiCancelMove.ToggleTweenVisibility(true);

        if (!queueMovementOrders)
        {
            moveUnit = false;
            uiMoveUnit.ToggleButtonColor(false);
            if (!movementSystem.MoveUnit(unit))
                return;
        }

        movementSystem.ClearPaths();
        uiJoinCity.ToggleTweenVisibility(false);
        uiTraderPanel.uiLoadUnload.ToggleInteractable(false);
    }

    public void MoveUnitRightClick(Vector3 location, GameObject detectedObject)
    {
		if (selectedUnit == null || !selectedUnit.CompareTag("Player") || selectedUnit.inArmy)
			return;

		//if nothing detected, nothing selected
		if (detectedObject == null)
        {
            selectedUnit = null;
            selectedTrader = null;
            return;
        }

        if (selectedUnit == null)
            return;

        if (selectedUnit.isBusy)
            return;

        if (selectedUnit != null && selectedUnit.sayingSomething)
            SpeakingCheck();

        selectedUnit.projectile.SetPoints(selectedUnit.transform.position, location);
        StartCoroutine(selectedUnit.projectile.ShootTest());
        //MoveUnit(location, detectedObject);
    }

    private void MoveUnit(Vector3 location, GameObject detectedObject)
    {
        Vector3 locationFlat = location;
        locationFlat.y = 0f;
        Vector3Int locationInt = world.RoundToInt(locationFlat);
        //TerrainData terrainSelected = world.GetTerrainDataAt(world.RoundToInt(locationFlat));
        if (!world.GetTerrainDataAt(locationInt).isDiscovered)
            return;
        //if (world.RoundToInt(selectedUnit.transform.position) == world.GetClosestTerrainLoc(locationInt)) //won't move within same tile
        //    return;

        if (selectedUnit.harvested) //if unit just finished harvesting something, send to closest city
            selectedUnit.SendResourceToCity();
        else if (loadScreenSet)
            LoadUnloadFinish(true);

		if (selectedUnit.isLaborer)
		{
			if (detectedObject.TryGetComponent(out City city) && detectedObject.CompareTag("Player"))
			{
                locationInt = city.cityLoc;
                world.citySelected = true;
			}
			else if (detectedObject.TryGetComponent(out Wonder wonder))
			{
                locationInt = wonder.unloadLoc;
                world.citySelected = true;
			}
            else if (world.IsWonderOnTile(world.GetClosestTerrainLoc(locationInt)))
            {
                Wonder wonderLoc = world.GetWonder(world.GetClosestTerrainLoc(locationInt));
                locationInt = wonderLoc.unloadLoc;
				world.citySelected = true;
			}
			else
			{
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Select only a city or a wonder");
				return;
			}

			locationFlat = locationInt;
		}

		if (selectedUnit.bySea)
        {
            if (!world.CheckIfSeaPositionIsValid(locationInt))
            {
                UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't move there");
                return;
            }

            selectedTrader.TurnOnRipples();
        }
        else if (!world.CheckIfPositionIsValid(locationInt) && !selectedUnit.isLaborer) //cancel movement if terrain isn't walkable
        {
            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't move there");
            return;
        }
        else if (world.CheckIfEnemyTerritory(locationInt))
        {
			UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Enemy territory");
			return;
        }

        if (selectedTrader != null && selectedTrader.followingRoute) //can't change orders if following route
        {
            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Currently following route");
            return;
        }

        if (selectedTrader != null && !selectedTrader.bySea && !world.IsRoadOnTileLocation(locationInt))
        {
            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Must travel on road");
            return;
        }

        location.y += .1f;
        starshine.transform.position = location;
        starshine.Play();

        if (selectedUnit.isMoving && !queueMovementOrders) //interrupt orders if new ones
        {
            selectedUnit.StopAnimation();
            selectedUnit.ShiftMovement();
            selectedUnit.FinishedMoving.RemoveAllListeners();
        }

        HandleSelectedLocation(locationFlat, locationInt, selectedUnit);
    }

    public void MoveUnitToggle()
    {
        if (!moveUnit)
            moveUnit = true;
        else
            moveUnit = false;

        uiMoveUnit.ToggleButtonColor(moveUnit);
    }

    public void HandleShiftDown()
    {
        queueMovementOrders = true;
    }

    public void HandleShiftUp()
    {
        queueMovementOrders = false;
    }

    public void ToggleCancelButton(bool v)
    {
        uiCancelMove.ToggleTweenVisibility(v);
    }

    public void CancelContinuedMovementOrders()
    {
        if (selectedUnit != null)
        {
            selectedUnit.ResetMovementOrders();
            uiCancelMove.ToggleTweenVisibility(false);
            selectedUnit.HidePath();
        }
        else if (world.buildingWonder)
        {
            world.CloseBuildingSomethingPanel();
        }
    }

    public void JoinCity() //for Join City button
    {
        Vector3Int unitLoc = world.GetClosestTerrainLoc(selectedUnit.transform.position);

        if (world.IsCityOnTile(unitLoc))
        {
            AddToCity(world.GetCity(unitLoc));
        }
        else if (selectedUnit.homeBase)
        {
            AddToCity(selectedUnit.homeBase);
            selectedUnit.homeBase.army.RemoveFromArmy(selectedUnit, selectedUnit.CurrentLocation);
        }
        else
        {
            world.GetWonder(unitLoc).AddWorker(selectedUnit.transform.position);
        }

        selectedUnit.DestroyUnit();
        ClearSelection();
    }

    public void JoinCity(Unit unit)
    {
		Vector3Int unitLoc = world.GetClosestTerrainLoc(unit.transform.position);

		if (world.IsCityOnTile(unitLoc))
		{
			AddToCity(world.GetCity(unitLoc));
		}
		else
		{
			world.GetWonder(unitLoc).AddWorker(unit.transform.position);
		}

        if (unit.isSelected)
        {
            world.somethingSelected = false;
            ClearSelection();
        }
		
		unit.DestroyUnit();
	}

    public void RepositionArmy()
    {
        focusCam.CenterCameraNoFollow(world.GetClosestTerrainLoc(selectedUnit.CurrentLocation));
        uiSwapPosition.ToggleTweenVisibility(false);
        uiJoinCity.ToggleTweenVisibility(false);
        uiDeployArmy.ToggleTweenVisibility(false);
		uiChangeCity.ToggleTweenVisibility(false);
		uiConfirmOrders.ToggleTweenVisibility(true);
		uiBuildingSomething.ToggleVisibility(true);
        uiBuildingSomething.SetText("Repositioning Unit");
		world.unitOrders = true;
        swappingArmy = true;
        world.SetSelectionCircleLocation(selectedUnit.CurrentLocation);
    }

    public void CancelReposition()
    {
		uiSwapPosition.ToggleTweenVisibility(true);
		uiJoinCity.ToggleTweenVisibility(true);
		uiDeployArmy.ToggleTweenVisibility(true);
        uiChangeCity.ToggleTweenVisibility(true);
		uiConfirmOrders.ToggleTweenVisibility(false);
		uiBuildingSomething.ToggleVisibility(false);
		world.unitOrders = false;
        swappingArmy = false;
        world.HideSelectionCircles();
    }

    private void AddToCity(City joinedCity)
    {
		joinedCity.PopulationGrowthCheck(true, selectedUnit.buildDataSO.laborCost);

		int i = 0;
		foreach (ResourceValue resourceValue in selectedUnit.buildDataSO.unitCost) //adding back 100% of cost (if there's room)
		{
			int resourcesGiven = joinedCity.ResourceManager.CheckResource(resourceValue.resourceType, resourceValue.resourceAmount);
			Vector3 cityLoc = joinedCity.cityLoc;
			cityLoc.y += selectedUnit.buildDataSO.unitCost.Count * 0.4f;
			cityLoc.y += -0.4f * i;
			InfoResourcePopUpHandler.CreateResourceStat(cityLoc, resourcesGiven, ResourceHolder.Instance.GetIcon(resourceValue.resourceType));
			i++;
		}
	}

    public void LoadUnloadPrep() //for loadunload button for traders
    {
        if (!loadScreenSet)
        {
            uiTraderPanel.uiLoadUnload.ToggleButtonColor(true);
            selectedUnit.HidePath();
            movementSystem.ClearPaths();
            uiTradeRouteManager.ToggleVisibility(false);
            //selectedTile = null;

            //Vector3Int unitLoc = Vector3Int.RoundToInt(selectedUnit.transform.position);
            //Vector3Int unitLoc = world.GetClosestTerrainLoc(selectedUnit.transform.position);
            Vector3Int tradeLoc = world.GetStopLocation(world.GetTradeLoc(world.GetClosestTerrainLoc(selectedUnit.transform.position)));
            bool atTradeCenter = false;

            if (world.IsCityOnTile(tradeLoc))
            {
                City selectedCity = world.GetCity(tradeLoc);

                cityResourceManager = selectedCity.ResourceManager;
                uiCityResourceInfoPanel.SetTitleInfo(selectedCity.cityName,
                    cityResourceManager.GetResourceStorageLevel, selectedCity.warehouseStorageLimit);
                //uiCityResourceInfoPanel.PrepareResourceUI(selectedCity.resourceGridDict);
                uiCityResourceInfoPanel.ToggleVisibility(true, null, selectedCity);
                uiCityResourceInfoPanel.SetPosition();
            }
            else if (world.IsWonderOnTile(tradeLoc))
            {
                wonder = world.GetWonder(tradeLoc);
                uiCityResourceInfoPanel.SetTitleInfo(wonder.WonderData.wonderName, 10000, 10000); //not showing inventory levels
                //uiCityResourceInfoPanel.PrepareResourceUI(wonder.ResourceDict);
                uiCityResourceInfoPanel.HideInventoryLevel();
                uiCityResourceInfoPanel.ToggleVisibility(true, null, null, wonder);
                uiCityResourceInfoPanel.SetPosition();
            }
            else if (world.IsTradeCenterOnTile(tradeLoc))
            {
                tradeCenter = world.GetTradeCenter(tradeLoc);
                uiCityResourceInfoPanel.SetTitleInfo(tradeCenter.tradeCenterName, 10000, 10000); //not showing inventory levels
                uiCityResourceInfoPanel.HideInventoryLevel();
                uiCityResourceInfoPanel.ToggleVisibility(true, null, null, null, tradeCenter);
                uiCityResourceInfoPanel.SetPosition(true);
                atTradeCenter = true;
                //world.cityBuilderManager.uiTradeCenter.ToggleVisibility(true, tradeCenter);
            }

            uiPersonalResourceInfoPanel.SetPosition(atTradeCenter, tradeCenter);
            
            loadScreenSet = true;
        }
        else
        {
            LoadUnloadFinish(true);
        }
    }

    public void ConfirmWorkerOrders()
    {
        queueMovementOrders = false;
        
        if (world.unitOrders)
        {
			uiBuildingSomething.ToggleVisibility(false);

			if (swappingArmy)
            {
                CancelReposition();
                return;
            }
            
            ClearBuildRoad();
            if (buildingRoad)
            {
                selectedWorker.SetRoadQueue();
            }
            else if (removingRoad)
            {
                selectedWorker.SetRoadRemovalQueue();
                selectedWorker.removing = true;
            }
            ResetOrderFlags();
        }
        else if (world.buildingWonder)
        {
            world.SetWonderConstruction();
        }
    }

    public void ResetOrderFlags()
    {
        buildingRoad = false;
        removingAll = false;
        removingRoad = false;
        removingLiquid = false;
        removingPower = false;
    }

    public void CloseBuildingSomethingPanel()
    {
        if (world.unitOrders)
        {
            workerTaskManager.TurnOffCancelTask();
			uiBuildingSomething.ToggleVisibility(false);

			if (swappingArmy)
            {
                CancelReposition();
                return;
            }
            if (deployingArmy || changingCity)
            {
                CancelArmyDeployment();
                return;
            }
            
            ToggleOrderHighlights(false);
            ClearBuildRoad();
            ResetOrderFlags();

            selectedWorker.ResetOrderQueue();
            selectedWorker.isBusy = false;
            uiWorkerTask.ToggleVisibility(true, null, world);
        }
        else if (world.buildingWonder)
        {
            world.CloseBuildingSomethingPanel();
        }
    }

    public void ClearBuildRoad()
    {
        world.unitOrders = false;
        uiConfirmOrders.ToggleTweenVisibility(false);
        uiMoveUnit.ToggleTweenVisibility(true);
        uiWorkerTask.ToggleVisibility(true, null, world);
        //workerTaskManager.ToggleRoadBuild(false);
        //foreach (TerrainData td in highlightedTiles)
        //{
        //    td.DisableHighlight();

        //    if (removingRoad)
        //    {
        //        foreach (GameObject go in world.GetAllRoadsOnTile(td.GetTileCoordinates()))
        //        {
        //            if (go == null)
        //                continue;
        //            go.GetComponent<SelectionHighlight>().DisableHighlight();
        //        }
        //    }
        //}
    }

    public void ToggleOrderHighlights(bool v)
    {
        if (v)
        {
            Color highlightColor;

            if (selectedWorker.removing)
                highlightColor = Color.red;
            else
                highlightColor = Color.white;

            foreach (Vector3Int tile in selectedWorker.OrderList)
            {
                world.GetTerrainDataAt(tile).EnableHighlight(highlightColor);

                if (selectedWorker.removing && world.IsRoadOnTerrain(tile))
                {
                    foreach (Road road in world.GetAllRoadsOnTile(tile))
                    {
                        if (road == null)
                            continue;
                        road.MeshFilter.gameObject.SetActive(true);
                        road.Embiggen();
                        road.SelectionHighlight.EnableHighlight(Color.white);
                    }
                }
            }
        }
        else
        {
            foreach (Vector3Int tile in selectedWorker.OrderList)
            {
                world.GetTerrainDataAt(tile).DisableHighlight();

                if ((selectedWorker.removing || world.unitOrders) && world.IsRoadOnTerrain(tile))
                {
                    foreach (Road road in world.GetAllRoadsOnTile(tile))
                    {
                        if (road == null)
                            continue;
                        road.MeshFilter.gameObject.SetActive(false);
                        road.SelectionHighlight.DisableHighlight();
                    }
                }
            }
        }
    }

    public void Load(ResourceType resourceType)
    {
        ChangeResourceManagersAndUIs(resourceType, cityTraderIncrement);
    }

    public void Unload(ResourceType resourceType)
    {
        ChangeResourceManagersAndUIs(resourceType, -cityTraderIncrement);
    }

    private void LoadUnloadFinish(bool keepSelection) //putting the screens back after finishing loading cargo
    {
        if (loadScreenSet)
        {
            uiTraderPanel.uiLoadUnload.ToggleButtonColor(false);
            //if (uiCityResourceInfoPanel.inUse)
            //    uiCityResourceInfoPanel.EmptyResourceUI();
            uiPersonalResourceInfoPanel.RestorePosition(keepSelection);
            uiCityResourceInfoPanel.RestorePosition(keepSelection);
            cityResourceManager = null;
            wonder = null;
            if (tradeCenter)
            {
                //world.cityBuilderManager.uiTradeCenter.ToggleVisibility(false);
                tradeCenter = null;
            }
            loadScreenSet = false;
        }
    }

    private void ChangeResourceManagersAndUIs(ResourceType resourceType, int resourceAmount)
    {
        //for buying and selling resources in trade center (stand alone)
        if (tradeCenter)
        {
            if (resourceAmount > 0) //buying 
            {
                if (!world.CheckWorldGold(tradeCenter.ResourceBuyDict[resourceType]))
                {
                    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't afford");
                    return;
                }
                
                int resourceAmountAdjusted = selectedTrader.personalResourceManager.CheckResource(resourceType, resourceAmount);

                if (resourceAmountAdjusted == 0)
                {
                    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Full inventory");
                    return;
                }

                int buyAmount = -resourceAmountAdjusted * tradeCenter.ResourceBuyDict[resourceType];
                world.UpdateWorldResources(ResourceType.Gold, buyAmount);
                InfoResourcePopUpHandler.CreateResourceStat(selectedTrader.transform.position, buyAmount, ResourceHolder.Instance.GetIcon(ResourceType.Gold));

                uiPersonalResourceInfoPanel.UpdateResourceInteractable(resourceType, selectedTrader.personalResourceManager.GetResourceDictValue(resourceType), true);
                uiPersonalResourceInfoPanel.UpdateStorageLevel(selectedTrader.personalResourceManager.GetResourceStorageLevel);
            }
            else if (resourceAmount <= 0) //selling
            {
                if (tradeCenter.ResourceSellDict.ContainsKey(resourceType))
                {
                    int remainingWithTrader = selectedTrader.personalResourceManager.GetResourceDictValue(resourceType);

                    if (remainingWithTrader < Mathf.Abs(resourceAmount))
                        resourceAmount = -remainingWithTrader;

                    if (resourceAmount == 0)
                        return;

                    selectedTrader.personalResourceManager.CheckResource(resourceType, resourceAmount);

                    int sellAmount = -resourceAmount * tradeCenter.ResourceSellDict[resourceType];
                    world.UpdateWorldResources(ResourceType.Gold, sellAmount);
                    InfoResourcePopUpHandler.CreateResourceStat(selectedTrader.transform.position, sellAmount, ResourceHolder.Instance.GetIcon(ResourceType.Gold));

                    uiPersonalResourceInfoPanel.UpdateResourceInteractable(resourceType, selectedTrader.personalResourceManager.GetResourceDictValue(resourceType), false);
                    uiCityResourceInfoPanel.FlashResource(resourceType);
                    uiPersonalResourceInfoPanel.UpdateStorageLevel(selectedTrader.personalResourceManager.GetResourceStorageLevel);
                }
                else
                {
                    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't sell " + resourceType);
                }
            }
            
            return;
        }



        if (wonder != null)
        {
            if (!wonder.CheckResourceType(resourceType))
            {
                InfoPopUpHandler.WarningMessage().Create(selectedUnit.transform.position, "Can't move resource " + resourceType);
                return;
            }
            else if (resourceAmount > 0)
            {
                InfoPopUpHandler.WarningMessage().Create(selectedUnit.transform.position, "Can't move from wonder");
                return;
            }
        }

        bool personalFull = false;

        if (resourceAmount > 0) //moving from city to trader
        {
            int remainingInCity;
            
            if (cityResourceManager != null)
                remainingInCity = cityResourceManager.GetResourceDictValue(resourceType);
            else
                remainingInCity = wonder.ResourceDict[resourceType];

            if (remainingInCity < resourceAmount)
                resourceAmount = remainingInCity;

            int resourceAmountAdjusted = selectedTrader.personalResourceManager.CheckResource(resourceType, resourceAmount);
            personalFull = resourceAmountAdjusted == 0;

            if (cityResourceManager != null)
                cityResourceManager.CheckResource(resourceType, -resourceAmountAdjusted, false);
            else
                wonder.CheckResource(resourceType, -resourceAmountAdjusted);
        }

        bool cityFull = false;

        if (resourceAmount <= 0) //moving from trader to city
        {
            int remainingWithTrader = selectedTrader.personalResourceManager.GetResourceDictValue(resourceType);

            if (remainingWithTrader < Mathf.Abs(resourceAmount))
                resourceAmount = -remainingWithTrader;

            int resourceAmountAdjusted;
            if (cityResourceManager != null)
                resourceAmountAdjusted = cityResourceManager.CheckResource(resourceType, -resourceAmount, false);
            else
                resourceAmountAdjusted = wonder.CheckResource(resourceType, -resourceAmount);

            cityFull = resourceAmountAdjusted == 0;
            selectedTrader.personalResourceManager.CheckResource(resourceType, -resourceAmountAdjusted);
        }

        bool toTrader = resourceAmount > 0;

        if (!cityFull)
        {
            if (cityResourceManager != null)
            {
                uiCityResourceInfoPanel.UpdateResourceInteractable(resourceType, cityResourceManager.GetResourceDictValue(resourceType), !toTrader);
                uiCityResourceInfoPanel.UpdateStorageLevel(cityResourceManager.GetResourceStorageLevel);
            }
            else
            {
                uiCityResourceInfoPanel.UpdateResourceInteractable(resourceType, wonder.ResourceDict[resourceType], !toTrader);
            }
        }

        if (!personalFull)
        {
            uiPersonalResourceInfoPanel.UpdateResourceInteractable(resourceType, selectedTrader.personalResourceManager.GetResourceDictValue(resourceType), toTrader);
            uiPersonalResourceInfoPanel.UpdateStorageLevel(selectedTrader.personalResourceManager.GetResourceStorageLevel);
        }
    }

    public void SetUpTradeRoute()
    {
        if (selectedTrader == null)
            return;

        if (!uiTradeRouteManager.activeStatus)
        {
            LoadUnloadFinish(true);
            infoManager.HideInfoPanel();
            uiTradeRouteManager.ToggleButtonColor(true);

            Vector3Int traderLoc = Vector3Int.RoundToInt(selectedTrader.transform.position);

            List<string> cityNames = world.GetConnectedCityNames(traderLoc, selectedTrader.bySea); //only showing city names accessible by unit
            uiTradeRouteManager.PrepareTradeRouteMenu(cityNames, selectedTrader);
            uiTradeRouteManager.ToggleVisibility(true);
            uiTradeRouteManager.LoadTraderRouteInfo(selectedTrader, selectedTrader.tradeRouteManager, world);
        }
        else
        {
            uiTradeRouteManager.ToggleVisibility(false);
        }
    }

    public void BeginTradeRoute() //start going trade route
    {
        if (selectedTrader != null)
        {
            if (selectedTrader.followingRoute)
            {
                CancelTradeRoute();
                return;
            }

            if (!selectedTrader.tradeRouteManager.TradeRouteCheck())
                return;
            if (selectedTrader.LineCutterCheck())
                return;
            selectedUnit.StopMovement();
            selectedTrader.BeginNextStepInRoute();
            //uiTraderPanel.uiBeginTradeRoute.ToggleInteractable(false);
            uiTraderPanel.SwitchRouteIcons(true);
            LoadUnloadFinish(true);
            uiTraderPanel.uiLoadUnload.ToggleInteractable(false);
            //uiCancelTradeRoute.ToggleTweenVisibility(true);
            uiTradeRouteManager.ToggleVisibility(false);
            //if (uiTradeRouteManager.activeStatus)
            //{
            //    uiTradeRouteManager.PrepTradeRoute();
            //}
        }
    }

    public void CancelTradeRoute() //stop following route but still keep route description
    {
        selectedTrader.followingRoute = false; //done earlier as it's in stopmovement
        selectedUnit.StopMovement();
        selectedTrader.CancelRoute();
        ShowIndividualCityButtonsUI();
        CancelContinuedMovementOrders();
        //uiCancelTradeRoute.ToggleTweenVisibility(false);
        if (!selectedTrader.followingRoute/*.interruptedRoute*/)
            uiTraderPanel.SwitchRouteIcons(false);
            //uiTraderPanel.uiBeginTradeRoute.ToggleInteractable(true);
        if (uiTradeRouteManager.activeStatus)
        {
            uiTradeRouteManager.ResetTradeRouteInfo(selectedTrader.tradeRouteManager);
            uiTradeRouteManager.ResetButtons();
        }
        //    uiTradeRouteManager.ToggleVisibility(false);
    }

    public void UninterruptedRoute()
    {
        //selectedTrader.interruptedRoute = false;
    }

    public void ShowIndividualCityButtonsUI()
    {
        if (selectedUnit == null)
            return;

        if (!selectedUnit.moreToMove)
        {
            //selectedUnit.FinishedMoving.RemoveListener(ShowIndividualCityButtonsUI);
            uiCancelMove.ToggleTweenVisibility(false);
        }

        Vector3Int currentLoc = world.GetClosestTerrainLoc(selectedUnit.transform.position);

        if (!selectedUnit.followingRoute && !selectedUnit.isMoving)
        {
            if (world.IsCityOnTile(currentLoc) && !selectedUnit.isWorker)
            {
                uiJoinCity.ToggleTweenVisibility(true);
            }
            
            if (selectedTrader != null && world.IsTradeLocOnTile(currentLoc))
            {
                uiTraderPanel.uiLoadUnload.ToggleInteractable(true);
            }
            else if (selectedUnit.isLaborer && world.IsWonderOnTile(currentLoc))
            {
                if (world.GetWonder(currentLoc).StillNeedsWorkers())
                    uiJoinCity.ToggleTweenVisibility(true);
            }
            else if (selectedUnit.inArmy)
            {
				if (selectedUnit.atHome)
                {
                    uiJoinCity.ToggleTweenVisibility(true);
                    uiSwapPosition.ToggleTweenVisibility(true);
                    uiDeployArmy.ToggleTweenVisibility(true);
                    uiChangeCity.ToggleTweenVisibility(true);
                }
                else if (selectedUnit.transferring)
                {
                    uiChangeCity.ToggleTweenVisibility(true);
                }
                else
                {
                    uiCancelTask.ToggleTweenVisibility(true);
                }
			}
        }
        else
        {
            uiJoinCity.ToggleTweenVisibility(false);
            uiTraderPanel.uiLoadUnload.ToggleInteractable(false);
        }
    }

    public void TurnOnInfoScreen()
    {
        if (selectedTrader != null)
        {
            infoManager.ShowInfoPanel(selectedUnit.buildDataSO, selectedUnit.currentHealth);
        }
    }

    private void SpeakingCheck()
    {
        if (selectedUnit.sayingSomething)
        {
            selectedUnit.sayingSomething = false;
            world.StopMessage();
        }
    }

    public void ChangeHomeBase()
    {
		uiJoinCity.ToggleTweenVisibility(false);
		uiSwapPosition.ToggleTweenVisibility(false);
		uiDeployArmy.ToggleTweenVisibility(false);
		uiChangeCity.ToggleTweenVisibility(false);
		world.HighlightCitiesWithBarracks(selectedUnit.homeBase);
        uiBuildingSomething.ToggleVisibility(true);
        uiBuildingSomething.SetText("Changing Home Base");
		uiCancelTask.ToggleTweenVisibility(true);
		world.unitOrders = true;
        changingCity = true;
    }

	public void DeployArmyLocation()
    {
        if (selectedUnit.homeBase.army.isTraining)
        {
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.x -= 120;
            UIInfoPopUpHandler.WarningMessage().Create(mousePosition, "Still training");
			return;
        }
        else if (selectedUnit.homeBase.army.isTransferring)
        {
			Vector3 mousePosition = Input.mousePosition;
			mousePosition.x -= 150;
			UIInfoPopUpHandler.WarningMessage().Create(mousePosition, "Still transferring");
            return;
		}
        
        uiJoinCity.ToggleTweenVisibility(false);
        uiSwapPosition.ToggleTweenVisibility(false);
        uiDeployArmy.ToggleTweenVisibility(false);
        uiChangeCity.ToggleTweenVisibility(false);
        uiBuildingSomething.ToggleVisibility(true);
		uiBuildingSomething.SetText("Deploying Army");
        uiCancelTask.ToggleTweenVisibility(true);
		world.unitOrders = true;
        deployingArmy = true;
        world.HighlightAllEnemyCamps();
    }

    public void CancelArmyDeployment()
    {
		uiCancelTask.ToggleTweenVisibility(false);
		
        if (selectedUnit.homeBase.army.traveling)
        {
            selectedUnit.homeBase.army.MoveArmy(world, world.GetClosestTerrainLoc(selectedUnit.transform.position), selectedUnit.homeBase.barracksLocation, false);
            world.EnemyCampReturn(selectedUnit.homeBase.army.EnemyTarget);
            uiDeployArmy.ToggleTweenVisibility(true);
        }
        else if (selectedUnit.homeBase.army.inBattle)
        {
            selectedUnit.homeBase.army.Retreat();
		}
        else if (selectedUnit.homeBase.army.atHome)
        {
            if (changingCity)
                world.UnhighlightCitiesWithBarracks();
            else if (deployingArmy)
                world.UnhighlightAllEnemyCamps();
                
            uiJoinCity.ToggleTweenVisibility(true);
		    uiSwapPosition.ToggleTweenVisibility(true);
            uiDeployArmy.ToggleTweenVisibility(true);
			uiChangeCity.ToggleTweenVisibility(true);
			uiBuildingSomething.ToggleVisibility(false);
            world.unitOrders = false;
            deployingArmy = false;
            changingCity = false;
        }
    }

    public void CancelOrders()
    {
        if (selectedUnit.inArmy)
            CancelArmyDeployment();
    }

    public void ClearSelection()
    {
        //selectedTile = null;
        if (selectedUnit != null)
        {
            if (selectedUnit.isBusy && selectedWorker.IsOrderListMoreThanZero())
                ToggleOrderHighlights(false);

            if (selectedUnit.isLaborer)
                world.UnhighlightCitiesAndWonders();

            SpeakingCheck();
            moveUnit = false;
            uiMoveUnit.ToggleTweenVisibility(false);
            uiCancelMove.ToggleTweenVisibility(false);
            uiJoinCity.ToggleTweenVisibility(false);
            uiSwapPosition.ToggleTweenVisibility(false);
            uiDeployArmy.ToggleTweenVisibility(false);
            uiChangeCity.ToggleTweenVisibility(false);

            if (selectedWorker != null)
            {
                uiCancelTask.ToggleTweenVisibility(false);
                uiWorkerTask.ToggleVisibility(false, null, world);
                workerTaskManager.NullWorkerUnit();
            }
            if (selectedTrader != null)
            {
                uiTraderPanel.uiBeginTradeRoute.ToggleInteractable(false);
                uiTraderPanel.SwitchRouteIcons(false);
                uiTraderPanel.ToggleVisibility(false, world);
                //uiCancelTradeRoute.ToggleTweenVisibility(false);
                uiTradeRouteManager.ToggleVisibility(false);
                uiPersonalResourceInfoPanel.ToggleVisibility(false, selectedTrader);
                LoadUnloadFinish(false); //clear load cargo screen
            }
            if (selectedUnit.inArmy)
            {
                uiCancelTask.ToggleTweenVisibility(false);
                selectedUnit.homeBase.army.UnselectArmy(selectedUnit);
            }
            //if (selectedUnit != null)
            //{
            selectedUnit.Deselect();
            selectedUnit.HidePath();
            //}
            infoManager.HideInfoPanel();
            //movementSystem.ClearPaths(); //necessary to queue movement orders
            //selectedUnitInfoProvider = null;
            selectedTrader = null;
            selectedWorker = null;
            selectedUnit = null;
            unitSelected = false;
        }
    }

    //private bool CheckIfTheSameUnitSelected(Unit unitReference)
    //{
    //    return selectedUnit == unitReference;
    //}
}
