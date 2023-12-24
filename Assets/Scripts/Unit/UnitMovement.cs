using Mono.Cecil;
using System.Collections.Generic;
using System.Diagnostics;
using System.Resources;
using UnityEngine;
using static UnityEditor.FilePathAttribute;
using static UnityEditor.PlayerSettings;

public class UnitMovement : MonoBehaviour
{
    [SerializeField]
    public MapWorld world;
    [SerializeField]
    public InfoManager infoManager;
    [SerializeField]
    public WorkerTaskManager workerTaskManager;
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
    public UITradeRouteManager uiTradeRouteManager;
    [SerializeField]
    public UIBuildingSomething uiBuildingSomething;
    //[SerializeField]
    //private UISingleConditionalButtonHandler uiCancelTradeRoute;

    [SerializeField]
    private ParticleSystem starshine;

    private bool queueMovementOrders;
    [HideInInspector]
    public MovementSystem movementSystem;

    [HideInInspector]
    public Unit selectedUnit;
    [HideInInspector]
    public Worker selectedWorker;
    [HideInInspector]
    public Trader selectedTrader;
    //private TerrainData selectedTile;
    //private InfoProvider selectedUnitInfoProvider;
    private bool loadScreenSet; //flag if load/unload ui is showing
    private bool moveUnit;

    //for transferring cargo to/from trader
    private ResourceManager cityResourceManager;
    private Wonder wonder;
    private TradeCenter tradeCenter;
    public int cityTraderIncrement = 1;

    //for deploying army
    private Vector3Int potentialAttackLoc;

    //for worker orders
    [HideInInspector]
    public bool buildingRoad, removingAll, removingRoad, removingLiquid, removingPower, unitSelected, swappingArmy, deployingArmy, changingCity;

    //for upgrading units
    [HideInInspector]
    public List<Unit> highlightedUnitList = new();
    [HideInInspector]
	public bool upgradingUnit;
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

    public void HandleG()
    {
        if (selectedUnit != null)
        {
            if (selectedUnit.inArmy)
                DeployArmyLocation();
            else
                MoveUnitToggle();
        }
    }

    public void HandleX()
    {
        if (selectedUnit != null && selectedUnit.inArmy)
            ChangeHomeBase();
    }

    public void HandleSpace()
    {
        if ((selectedWorker != null && world.unitOrders) || world.buildingWonder)
            ConfirmWorkerOrders();
    }

    public void HandleB()
    {
        if (uiJoinCity.activeStatus)
            JoinCity();
    }

    public void HandleR()
    {
        if (uiSwapPosition.activeStatus)
            RepositionArmy();
    }

    public void CenterCamOnUnit()
    {
        if (selectedUnit != null)
            selectedUnit.CenterCamera();
    }

	public void ToggleUnitHighlights(bool v, City city = null)
	{
        upgradingUnit = v;

		if (v)
		{
			foreach (Unit unit in city.tradersHere)
            {
				if (unit.buildDataSO.unitLevel < world.GetUpgradeableObjectMaxLevel(unit.buildDataSO.unitName))
                {
					highlightedUnitList.Add(unit);
					unit.SoftSelect(Color.green);
				}
			}
            
            if (!city.army.atHome || city.army.isTraining) //only upgrade when at home or not busy
				return;

			foreach (Unit unit in city.army.UnitsInArmy)
			{
				if (unit.buildDataSO.unitLevel < world.GetUpgradeableObjectMaxLevel(unit.buildDataSO.unitName))
				{
                    highlightedUnitList.Add(unit);
                    unit.SoftSelect(Color.green);
				}
			}
		}
		else
		{
			foreach (Unit unit in highlightedUnitList)
			{
				if (!unit.isSelected)
                {
                    if (unit.inArmy && !unit.homeBase.army.selected)
                        unit.Deselect();
                    else if (unit.isTrader)
                        unit.Deselect();
                }
			}

            highlightedUnitList.Clear();
		}
	}

	public void HandleUnitSelectionAndMovement(Vector3 location, GameObject detectedObject)
    {
        if (world.buildingWonder)
            return;

        //if (selectedUnit != null && selectedUnit.sayingSomething)
            //SpeakingCheck();
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
        world.CloseConversationList();
        world.CloseWonders();
        world.CloseTerrainTooltip();
        world.CloseImprovementTooltip();
        world.CloseCampTooltip();
        world.CloseTradeRouteBeginTooltip();

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
                            uiConfirmOrders.ToggleVisibility(true);

                        td.EnableHighlight(Color.white);
                        world.cityBuilderManager.PlaySelectAudio();
                        //highlightedTiles.Add(td);
                    }
                    else
                    {
                        if (!selectedWorker.IsOrderListMoreThanZero())
                            uiConfirmOrders.ToggleVisibility(false);

                        td.DisableHighlight();
                        world.cityBuilderManager.PlaySelectAudio();
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
                            uiConfirmOrders.ToggleVisibility(true);

                        td.EnableHighlight(Color.red);
                        foreach (Road road in world.GetAllRoadsOnTile(pos))
                        {
                            if (road == null)
                                continue;
                            road.MeshFilter.gameObject.SetActive(true);
                            road.Embiggen();
                            road.SelectionHighlight.EnableHighlight(Color.white);
                        }

						world.cityBuilderManager.PlaySelectAudio();
						//highlightedTiles.Add(td);
					}
                    else
                    {
                        if (!selectedWorker.IsOrderListMoreThanZero())
                            uiConfirmOrders.ToggleVisibility(false);

                        td.DisableHighlight();
                        foreach (Road road in world.GetAllRoadsOnTile(pos))
                        {
                            if (road == null)
                                continue;
                            road.MeshFilter.gameObject.SetActive(false);
                            road.SelectionHighlight.DisableHighlight();
                        }

						world.cityBuilderManager.PlaySelectAudio();
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

					selectedUnit.homeBase.army.isRepositioning = true;
					bool swapping = false;
                    selectedUnit.repositioning = true;
                    selectedUnit.atHome = false;

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
                        Vector3 starLoc = loc;
                        starLoc.y += .1f;
						starshine.transform.position = starLoc;
						starshine.Play();

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

                    world.citySelected = true;
                    ConfirmWorkerOrders();
                    world.cityBuilderManager.MoveUnitAudio();
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
                    
                    if (selectedUnit.homeBase.army.DeployArmyCheck(world.GetClosestTerrainLoc(selectedUnit.CurrentLocation), pos))
                    {
                        selectedUnit.homeBase.army.ShowBattlePath();
                        world.HighlightEnemyCamp(potentialAttackLoc, Color.red);
                        world.HighlightEnemyCamp(pos, Color.white);
						world.infoPopUpCanvas.gameObject.SetActive(true);
						world.uiCampTooltip.ToggleVisibility(true, null, world.GetEnemyCamp(pos), selectedUnit.homeBase.army);
                        potentialAttackLoc = pos;
      //                  uiBuildingSomething.ToggleVisibility(false);
						//world.UnhighlightAllEnemyCamps();
      //                  world.citySelected = true;
						//world.unitOrders = false;
						//deployingArmy = false;
      //                  world.SetEnemyCampAsAttacked(pos, selectedUnit.homeBase.army);
      //                  selectedUnit.homeBase.army.targetCamp = world.GetEnemyCamp(pos);
					}
				}
                else
                {
					UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Select enemy camp");
				}
            }
            //changing barracks
            else if (changingCity)
            {
                if (world.IsCityOnTile(pos) && world.GetCity(pos).highlighted)
                {
                    City newCity = world.GetCity(pos);
                    Vector3Int newLoc = newCity.army.GetAvailablePosition(selectedUnit.buildDataSO.unitType);
                    List<Vector3Int> path = GridSearch.AStarSearch(world, selectedUnit.transform.position, newLoc, false, selectedUnit.bySea);

                    if (path.Count > 0)
                    {
                        if (selectedUnit.isMoving)
                        {
							selectedUnit.StopAnimation();
							selectedUnit.ShiftMovement();
							selectedUnit.FinishedMoving.RemoveAllListeners();
						}

                        selectedUnit.homeBase.army.RemoveFromArmy(selectedUnit, selectedUnit.barracksBunk);
                        selectedUnit.homeBase = newCity; 
                        selectedUnit.atHome = false;
                        newCity.army.AddToArmy(selectedUnit);

                        if (newCity.cityPop.CurrentPop == 0)
                            newCity.StartGrowthCycle(false);

                        selectedUnit.barracksBunk = newLoc;
                        selectedUnit.transferring = true;
                        selectedUnit.homeBase.army.isTransferring = true;

						selectedUnit.finalDestinationLoc = newLoc;
                        selectedUnit.MoveThroughPath(path);
                        world.citySelected = true;

						world.UnhighlightCitiesWithBarracks();
                        uiChangeCity.ToggleVisibility(true);
						uiBuildingSomething.ToggleVisibility(false);
                        uiCancelTask.ToggleVisibility(false);
                        world.unitOrders = false;
						changingCity = false;
                    }

                    world.cityBuilderManager.MoveUnitAudio();
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
            MoveUnit(location, detectedObject, true);
        }
        else if (detectedObject.TryGetComponent(out Unit unitReference))
        {
            if (unitReference.CompareTag("Player"))
            {
				if (unitReference.isUpgrading)
				{
					UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Currently being upgraded");
					return;
				}

				SelectUnitPrep(unitReference, location);
            }
            else if (unitReference.CompareTag("Enemy"))
                SelectEnemy(unitReference);
        }
        else if (detectedObject.TryGetComponent(out UnitMarker unitMarker))
        {
            Unit unit = unitMarker.Unit;

            if (unitMarker.CompareTag("Player"))
            {
				if (unit.isUpgrading)
				{
					UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Currently being upgraded");
					return;
				}

				SelectUnitPrep(unit, location);
            }
            else if (unitMarker.CompareTag("Enemy"))
            {
                SelectEnemy(unit);
            }
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
                SelectWorker();
				if (!selectedUnit.sayingSomething)
                {
                    uiMoveUnit.ToggleVisibility(true);
					PrepareMovement();
                }
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
        if (upgradingUnit && highlightedUnitList.Contains(unitReference))
        {
            world.cityBuilderManager.UpgradeUnitWindow(unitReference);
            return;
        }
        
        if (selectedUnit == unitReference) //Unselect when clicking same unit
        {
            if (selectedWorker != null && selectedWorker.harvested)
                selectedWorker.SendResourceToCity();
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
            unitReference.SpeakingCheck();
        }

		if (!selectedUnit.sayingSomething)
        {
            SelectLaborer();
            SelectWorker();
            SelectTrader();

            if (!selectedUnit.inArmy && !selectedUnit.followingRoute && !selectedUnit.isUpgrading)
                uiMoveUnit.ToggleVisibility(true);
		
            PrepareMovement();
        }
    }

    //for selecting worker for speaking when already selected
    public void QuickSelect(Unit unitReference)
    {
		ClearSelection();
		selectedUnit = unitReference;
		//SelectWorker();
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

        selectedUnit.SayHello();
		world.somethingSelected = true;
		selectedUnit.Select(Color.red);
		infoManager.ShowInfoPanel(selectedUnit.name, selectedUnit.buildDataSO, selectedUnit.currentHealth, selectedUnit.isTrader, selectedUnit.isLaborer);
	}

    public void SelectWorker()
    {
        if (selectedUnit.isWorker)
        {
            selectedWorker = selectedUnit.GetComponent<Worker>();
            workerTaskManager.SetWorkerUnit(selectedWorker);
            if (!selectedUnit.sayingSomething)
                uiWorkerTask.ToggleVisibility(true, world);
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
                selectedTrader.personalResourceManager.ResourceStorageLevel, selectedTrader.cargoStorageLimit);
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

    public void PrepareMovement(Unit unit, bool centerCam = false) //handling unit selection through the unit turn buttons
    {
        if (selectedUnit != null) //clearing selection if a new unit is clicked
        {
            ClearSelection();
        }

        selectedUnit = unit;

        if (centerCam)
			world.cameraController.CenterCameraNoFollow(selectedUnit.transform.position);

		SelectLaborer();
        SelectWorker();
        SelectTrader();
        PrepareMovement();
    }

    public void PrepareMovement()
    {
        selectedUnit.SayHello();
	
        world.somethingSelected = true;
        unitSelected = true;
        if (selectedUnit.inArmy)
			selectedUnit.homeBase.army.SelectArmy(selectedUnit);
		else
			selectedUnit.Select(Color.white);

        //so highlight of city doesn't go away
   //     if (selectedUnit.isLaborer)
   //     {
   //         if (world.cityBuilderManager.SelectedCity != null)
   //             world.cityUnitSelected = true;
            
   //         //world.HighlightCitiesAndWonders();
   //     }
   //     else if (selectedUnit.isTrader)
   //     {
			//if (world.cityBuilderManager.SelectedCity != null && !selectedUnit.bySea)
			//	world.cityUnitSelected = true;

			////if (!selectedUnit.followingRoute)
   ////             world.HighlightCitiesAndWondersAndTradeCenters(selectedUnit.bySea);
   //     }

        //selectedUnitInfoProvider = selectedUnit.GetComponent<InfoProvider>(); //getting the information to show in info panel
        infoManager.ShowInfoPanel(selectedUnit.name, selectedUnit.buildDataSO, selectedUnit.currentHealth, selectedUnit.isTrader, selectedUnit.isLaborer);
        if (selectedUnit.moreToMove && !selectedUnit.inArmy)
        {
            uiCancelMove.ToggleVisibility(true);
            //movementSystem.ShowPathToMove(selectedUnit);
            selectedUnit.ShowContinuedPath();
        }
        else if (selectedUnit.inArmy)
        {
            if (selectedUnit.homeBase.army.traveling)
                uiCancelTask.ToggleVisibility(true);
            //else if (selectedUnit.homeBase.army.returning)
            //    uiDeployArmy.ToggleTweenVisibility(true);
            else if (selectedUnit.transferring)
                uiChangeCity.ToggleVisibility(true);
            else if (selectedUnit.inBattle)
                uiCancelTask.ToggleVisibility(true);
        }

        ShowIndividualCityButtonsUI();
    }

    public void HandleSelectedLocation(Vector3 location, Vector3Int terrainPos, Unit unit)
    {
        if (!unit.CompareTag("Player"))
            return;

        if (queueMovementOrders /*&& unit.FinalDestinationLoc != location*/ && unit.isMoving)
        {
            if (unit.finalDestinationLoc == location)
                return;
            
            movementSystem.AppendNewPath(unit);
        }
        else if (unit.isMoving)
        {
            unit.ResetMovementOrders();
        }
        
        movementSystem.GetPathToMove(world, unit, terrainPos, unit.isTrader); //Call AStar movement

        unit.finalDestinationLoc = location;

        if (!queueMovementOrders)
        {
            moveUnit = false;
            uiMoveUnit.ToggleButtonColor(false);
			if (unit.isTrader)
				world.UnhighlightCitiesAndWondersAndTradeCenters(unit.bySea);
			else if (unit.isLaborer)
				world.UnhighlightCitiesAndWonders();


			if (!movementSystem.MoveUnit(unit))
                return;
        }

        uiCancelMove.ToggleVisibility(!unit.isBusy);
        
        movementSystem.ClearPaths();
        uiJoinCity.ToggleVisibility(false);
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

        //if (selectedUnit != null && selectedUnit.sayingSomething)
        //    SpeakingCheck();

        //selectedUnit.projectile.SetPoints(selectedUnit.transform.position, location); // just for testing projectiles
        //StartCoroutine(selectedUnit.projectile.ShootTest());
        MoveUnit(location, detectedObject, false);
    }

    private void MoveUnit(Vector3 location, GameObject detectedObject, bool leftClick)
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
            Vector3Int terrainLoc = world.GetClosestTerrainLoc(locationInt);

			if (detectedObject.TryGetComponent(out City city) && detectedObject.CompareTag("Player"))
			{
                //locationInt = city.cityLoc;
                world.citySelected = leftClick;
			}
            else if (world.IsCityOnTile(terrainLoc))
            {
                //locationInt = terrainLoc;
				world.citySelected = leftClick;
			}
			else if (detectedObject.TryGetComponent(out Wonder wonder))
			{
                if (wonder.isConstructing)
                {
					UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Move to city or constructing wonder");
					return;
                }

                locationInt = wonder.unloadLoc;
                world.citySelected = leftClick;
			}
            else if (world.IsWonderOnTile(terrainLoc))
            {
                Wonder wonderLoc = world.GetWonder(world.GetClosestTerrainLoc(locationInt));
                locationInt = wonderLoc.unloadLoc;
				world.citySelected = leftClick;
			}
			else
			{
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Move to city or constructing wonder");
				return;
			}

			//locationFlat = locationInt;
		}

		if (selectedUnit.bySea)
        {
            if (!world.CheckIfSeaPositionIsValid(locationInt))
            {
                Vector3Int trySpot = world.GetClosestMoveToSpot(locationInt, selectedUnit.transform.position, true);

                //if (trySpot == locationInt)
                //{
                //    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't move there");
                //    return;
                //}

                locationInt = trySpot;
                locationFlat = trySpot;
            }

            selectedTrader.TurnOnRipples();
        }
        else if (!world.CheckIfPositionIsValid(locationInt) && !selectedUnit.isLaborer) //cancel movement if terrain isn't walkable
        {
			if (!world.CheckIfSeaPositionIsValid(locationInt))
			{
				Vector3Int trySpot = world.GetClosestMoveToSpot(locationInt, selectedUnit.transform.position, false);

				//if (trySpot == locationInt)
				//{
				//	UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't move there");
				//	return;
				//}

				locationInt = trySpot;
                locationFlat = trySpot;
			}
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

		if (selectedUnit.isTrader)
		{
            Vector3Int terrainLoc = world.GetClosestTerrainLoc(locationInt);

			if (!world.IsTradeLocOnTile(terrainLoc))
            {
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Move to city, trade center, or a wonder");
				return;
			}

			Vector3Int terrainInt = world.GetStopLocation(world.GetTradeLoc(terrainLoc));

			if (world.IsCityOnTile(terrainInt))
			{
				//locationInt = selectedUnit.bySea ? world.GetCity(terrainInt).harborLocation : terrainInt;
				world.citySelected = leftClick;
			}
			else if (world.IsWonderOnTile(terrainInt))
            {
                Wonder wonderLoc = world.GetWonder(terrainInt);
                locationInt = selectedUnit.bySea ? wonderLoc.harborLoc : wonderLoc.unloadLoc;
                world.citySelected = leftClick;
            }
            else if (world.IsTradeCenterOnTile(terrainInt))
            {
                locationInt = selectedUnit.bySea ? world.GetTradeCenter(terrainInt).harborLoc : terrainInt;
                world.citySelected = leftClick;
            }
            else
			{
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Move to city, trade center, or a wonder");
				return;
			}
		}

		location.y += .1f;
        starshine.transform.position = location;
        starshine.Play();
		world.cityBuilderManager.MoveUnitAudio();

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
        world.cityBuilderManager.PlaySelectAudio();
        
        if (!moveUnit)
        {
            moveUnit = true;

            if (selectedUnit.isTrader)
                world.HighlightCitiesAndWondersAndTradeCenters(selectedUnit.bySea);
            else if (selectedUnit.isLaborer)
                world.HighlightCitiesAndWonders();
        }
        else
        {
            moveUnit = false;

			if (selectedUnit.isTrader)
				world.UnhighlightCitiesAndWondersAndTradeCenters(selectedUnit.bySea);
			else if (selectedUnit.isLaborer)
				world.UnhighlightCitiesAndWonders();
		}

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
        uiCancelMove.ToggleVisibility(v);
    }

    public void CancelContinuedMovementOrdersButton()
    {
        world.cityBuilderManager.PlaySelectAudio();
        CancelContinuedMovementOrders();
    }

    public void CancelContinuedMovementOrders()
    {
        if (selectedUnit != null)
        {
            if (selectedUnit.isTrader && selectedUnit.followingRoute)
            {
                CancelTradeRoute();
                return;
            }

            selectedUnit.ResetMovementOrders();
            uiCancelMove.ToggleVisibility(false);
            selectedUnit.HidePath();
        }
        else if (world.buildingWonder)
        {
            world.CloseBuildingSomethingPanel();
        }
    }

    public void JoinCity() //for Join City button
    {
        world.cityBuilderManager.PlaySelectAudio();

        if (selectedUnit.isUpgrading)
        {
			InfoPopUpHandler.WarningMessage().Create(selectedUnit.transform.position, "Currently upgrading");
			return;
        }
        
        if (moveUnit)
        {
            moveUnit = false;
            uiMoveUnit.ToggleButtonColor(false);
            if (selectedUnit.isTrader)
                world.UnhighlightCitiesAndWondersAndTradeCenters(selectedUnit.bySea);
            else if (selectedUnit.isLaborer)
                world.UnhighlightCitiesAndWonders();
        }

        Vector3Int unitLoc = world.GetClosestTerrainLoc(selectedUnit.transform.position);

        if (world.IsWonderOnTile(unitLoc))
        {
            Wonder wonder = world.GetWonder(unitLoc);
			wonder.AddWorker(selectedUnit);
            selectedUnit.DestroyUnit();
            ClearSelection();
			return;
		}

        City city = null;

        if (world.IsCityOnTile(unitLoc))
        {
            city = world.GetCity(unitLoc);

   //         if (city.reachedWaterLimit)
   //         {
			//	InfoPopUpHandler.WarningMessage().Create(selectedUnit.transform.position, "Can't join. Not enough water");
			//	return;
			//}

			//AddToCity(city, selectedUnit);
        }
        else if (world.IsCityHarborOnTile(unitLoc))
        {
            city = world.GetHarborCity(unitLoc);

			//if (city.reachedWaterLimit)
			//{
			//	InfoPopUpHandler.WarningMessage().Create(selectedUnit.transform.position, "Can't join. Not enough water");
			//	return;
			//}

			//AddToCity(city, selectedUnit);
        }
        else if (selectedUnit.inArmy)
        {
            //if (selectedUnit.homeBase.reachedWaterLimit)
            //{
            //	InfoPopUpHandler.WarningMessage().Create(selectedUnit.transform.position, "Can't join. Not enough water");
            //	return;
            //}

            city = selectedUnit.homeBase;
			//AddToCity(selectedUnit.homeBase, selectedUnit);
            //selectedUnit.homeBase.army.RemoveFromArmy(selectedUnit, selectedUnit.barracksBunk);
        }

        if (city != null)
            world.uiCityPopIncreasePanel.ToggleVisibility(true, selectedUnit.buildDataSO.laborCost, city, true);
        //else
        //{
        //    world.GetWonder(unitLoc).AddWorker(selectedUnit);
        //}

        //selectedUnit.DestroyUnit();
        //ClearSelection();
    }

    public void JoinCityConfirm(City city)
    {
		if (selectedUnit.inArmy)
			selectedUnit.homeBase.army.RemoveFromArmy(selectedUnit, selectedUnit.barracksBunk);

		AddToCity(city, selectedUnit);
		selectedUnit.DestroyUnit();
		ClearSelection();
	}

    public void JoinCity(Unit unit)
    {
		Vector3Int unitLoc = world.GetClosestTerrainLoc(unit.transform.position);

		if (world.IsCityOnTile(unitLoc))
		{
            City city = world.GetCity(unitLoc);

			if (city.reachedWaterLimit)
                return;
			
            AddToCity(city, unit);
		}
		else
		{
            Wonder wonder = world.GetWonder(unitLoc);
            if (wonder.StillNeedsWorkers())
                wonder.AddWorker(unit);
            else
                return;
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
        world.cityBuilderManager.PlaySelectAudio();
        world.cameraController.CenterCameraNoFollow(world.GetClosestTerrainLoc(selectedUnit.CurrentLocation));
        uiSwapPosition.ToggleVisibility(false);
        uiJoinCity.ToggleVisibility(false);
        uiDeployArmy.ToggleVisibility(false);
		uiChangeCity.ToggleVisibility(false);
		uiConfirmOrders.ToggleVisibility(true);
		uiBuildingSomething.ToggleVisibility(true);
        uiBuildingSomething.SetText("Repositioning Unit");
		world.unitOrders = true;
        swappingArmy = true;
        world.SetSelectionCircleLocation(selectedUnit.CurrentLocation);
    }

    public void CancelReposition()
    {
		uiSwapPosition.ToggleVisibility(true);
		uiJoinCity.ToggleVisibility(true);
		uiDeployArmy.ToggleVisibility(true);
        uiChangeCity.ToggleVisibility(true);
		uiConfirmOrders.ToggleVisibility(false);
		uiBuildingSomething.ToggleVisibility(false);
		world.unitOrders = false;
        swappingArmy = false;
        world.HideSelectionCircles();
    }

    public void AddToCity(City joinedCity, Unit unit)
    {
        if (unit.isTrader)
        {
            world.traderList.Remove(unit.GetComponent<Trader>());
            joinedCity.tradersHere.Remove(unit);
        }
        else if (unit.isLaborer)
            world.laborerList.Remove(unit.GetComponent<Laborer>());
        
        joinedCity.PopulationGrowthCheck(true, unit.buildDataSO.laborCost);

		int i = 0;
		foreach (ResourceValue resourceValue in unit.buildDataSO.unitCost) //adding back 100% of cost (if there's room)
		{
			int resourcesGiven = joinedCity.ResourceManager.CheckResource(resourceValue.resourceType, resourceValue.resourceAmount);
			Vector3 cityLoc = joinedCity.cityLoc;
			cityLoc.y += unit.buildDataSO.unitCost.Count * 0.4f;
			cityLoc.y += -0.4f * i;
			InfoResourcePopUpHandler.CreateResourceStat(cityLoc, resourcesGiven, ResourceHolder.Instance.GetIcon(resourceValue.resourceType));
			i++;
		}
	}

    public void LoadUnloadPrep() //for loadunload button for traders
    {
		world.cityBuilderManager.PlaySelectAudio();

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
                    cityResourceManager.ResourceStorageLevel, selectedCity.warehouseStorageLimit);
                //uiCityResourceInfoPanel.PrepareResourceUI(selectedCity.resourceGridDict);
                uiCityResourceInfoPanel.ToggleVisibility(true, null, selectedCity);
                uiCityResourceInfoPanel.SetPosition();
            }
            else if (world.IsWonderOnTile(tradeLoc))
            {
                wonder = world.GetWonder(tradeLoc);
                uiCityResourceInfoPanel.SetTitleInfo(wonder.WonderData.wonderDisplayName, 10000, 10000); //not showing inventory levels
                //uiCityResourceInfoPanel.PrepareResourceUI(wonder.ResourceDict);
                uiCityResourceInfoPanel.HideInventoryLevel();
                uiCityResourceInfoPanel.ToggleVisibility(true, null, null, wonder);
                uiCityResourceInfoPanel.SetPosition();
            }
            else if (world.IsTradeCenterOnTile(tradeLoc))
            {
                tradeCenter = world.GetTradeCenter(tradeLoc);
                uiCityResourceInfoPanel.SetTitleInfo(tradeCenter.tradeCenterDisplayName, 10000, 10000); //not showing inventory levels
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

    public void ConfirmWorkerOrdersButton()
    {
        //world.cityBuilderManager.PlayBoomAudio(); //seems to double play
        ConfirmWorkerOrders();
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

    public void CloseBuildingSomethingPanelButton()
    {
        world.cityBuilderManager.PlayCloseAudio();
        CloseBuildingSomethingPanel();
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
            uiWorkerTask.ToggleVisibility(true, world);
        }
        else if (world.buildingWonder)
        {
            world.CloseBuildingSomethingPanel();
        }
    }

    public void ClearBuildRoad()
    {
        world.unitOrders = false;
        uiConfirmOrders.ToggleVisibility(false);
        uiMoveUnit.ToggleVisibility(true);
        uiWorkerTask.ToggleVisibility(true, world);
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
        selectedTrader.personalResourceManager.DictCheckSolo(resourceType);

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

				//world.cityBuilderManager.PlayRingAudio();
				int buyAmount = -resourceAmountAdjusted * tradeCenter.ResourceBuyDict[resourceType];
                world.UpdateWorldResources(ResourceType.Gold, buyAmount);
                InfoResourcePopUpHandler.CreateResourceStat(selectedTrader.transform.position, buyAmount, ResourceHolder.Instance.GetIcon(ResourceType.Gold));

                uiPersonalResourceInfoPanel.UpdateResourceInteractable(resourceType, selectedTrader.personalResourceManager.GetResourceDictValue(resourceType), true);
                uiPersonalResourceInfoPanel.UpdateStorageLevel(selectedTrader.personalResourceManager.ResourceStorageLevel);
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

                    //world.cityBuilderManager.PlayRingAudio();
                    selectedTrader.personalResourceManager.CheckResource(resourceType, resourceAmount);

                    int sellAmount = -resourceAmount * tradeCenter.ResourceSellDict[resourceType];
                    world.UpdateWorldResources(ResourceType.Gold, sellAmount);
                    InfoResourcePopUpHandler.CreateResourceStat(selectedTrader.transform.position, sellAmount, ResourceHolder.Instance.GetIcon(ResourceType.Gold));

                    uiPersonalResourceInfoPanel.UpdateResourceInteractable(resourceType, selectedTrader.personalResourceManager.GetResourceDictValue(resourceType), false);
                    uiCityResourceInfoPanel.FlashResource(resourceType);
                    uiPersonalResourceInfoPanel.UpdateStorageLevel(selectedTrader.personalResourceManager.ResourceStorageLevel);
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
			//world.cityBuilderManager.PlayRingAudio();

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
			//world.cityBuilderManager.PlayRingAudio();
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
                uiCityResourceInfoPanel.UpdateStorageLevel(cityResourceManager.ResourceStorageLevel);
            }
            else
            {
                uiCityResourceInfoPanel.UpdateResourceInteractable(resourceType, wonder.ResourceDict[resourceType], !toTrader);
            }
        }

        if (!personalFull)
        {
            uiPersonalResourceInfoPanel.UpdateResourceInteractable(resourceType, selectedTrader.personalResourceManager.GetResourceDictValue(resourceType), toTrader);
            uiPersonalResourceInfoPanel.UpdateStorageLevel(selectedTrader.personalResourceManager.ResourceStorageLevel);
        }

        selectedTrader.personalResourceManager.ResetDictSolo(resourceType);
    }

    public void SetUpTradeRoute()
    {
        if (selectedTrader == null)
            return;

        world.cityBuilderManager.PlaySelectAudio();
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

    public void ShowTradeRouteCost()
    {
		world.cityBuilderManager.PlaySelectAudio();

		if (selectedTrader.followingRoute)
		{
			CancelTradeRoute();
			return;
		}

		if (!selectedTrader.tradeRouteManager.TradeRouteCheck())
			return;

		world.infoPopUpCanvas.gameObject.SetActive(true);
		world.uiTradeRouteBeginTooltip.ToggleVisibility(true, selectedTrader);
	}

	public void BeginTradeRoute() //start going trade route
    {    
        if (selectedTrader != null)
        {
			if (moveUnit)
            {
                world.UnhighlightCitiesAndWondersAndTradeCenters(selectedTrader.bySea);
                moveUnit = false;
				uiMoveUnit.ToggleButtonColor(false);
			}

			uiMoveUnit.ToggleVisibility(false);

			if (selectedTrader.LineCutterCheck())
				return;

            if (!world.uiTradeRouteBeginTooltip.AffordCheck())
                return;

            world.cityBuilderManager.PlayCoinsAudio();
            world.uiTradeRouteBeginTooltip.ToggleVisibility(false);
            selectedTrader.SpendRouteCosts(selectedTrader.tradeRouteManager.startingStop);
			//if (selectedTrader.followingRoute)
			//{
			//    CancelTradeRoute();
			//    return;
			//}

			//if (!selectedTrader.tradeRouteManager.TradeRouteCheck())
			//    return;
			//if (selectedTrader.LineCutterCheck())
			//    return;

            if (selectedUnit.bySea)
            {
                if (world.IsCityHarborOnTile(selectedUnit.CurrentLocation))
                    world.GetHarborCity(world.GetClosestTerrainLoc(selectedUnit.CurrentLocation)).tradersHere.Remove(selectedUnit);
            }
            else
            {
                if (world.IsCityOnTile(selectedUnit.CurrentLocation))
                    world.GetCity(world.GetClosestTerrainLoc(selectedUnit.CurrentLocation)).tradersHere.Remove(selectedUnit);
            }

			selectedUnit.StopMovement();
            selectedTrader.BeginNextStepInRoute();
            uiTraderPanel.SwitchRouteIcons(true);
            LoadUnloadFinish(true);
            uiTraderPanel.uiLoadUnload.ToggleInteractable(false);
            uiTradeRouteManager.ToggleVisibility(false);
        }
    }

    public void CancelTradeRoute() //stop following route but still keep route description
    {
        selectedTrader.RefundRouteCosts();
        selectedTrader.followingRoute = false; //done earlier as it's in stopmovement
        selectedTrader.waitingOnRouteCosts = false;
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
            uiCancelMove.ToggleVisibility(false);
        }

        Vector3Int currentLoc = world.GetClosestTerrainLoc(selectedUnit.transform.position);

        if (!selectedUnit.followingRoute && !selectedUnit.isMoving)
        {
            if (world.IsCityOnTile(currentLoc) && !selectedUnit.isWorker)
            {
                uiJoinCity.ToggleVisibility(true);
            }

            if (selectedUnit.bySea && world.IsCityHarborOnTile(currentLoc))
            {
				uiJoinCity.ToggleVisibility(true);
			}
            
            if (selectedTrader != null && world.IsTradeLocOnTile(currentLoc))
            {
                uiTraderPanel.uiLoadUnload.ToggleInteractable(true);
            }
            else if (selectedUnit.isLaborer && world.IsWonderOnTile(currentLoc))
            {
                if (world.GetWonder(currentLoc).StillNeedsWorkers())
                    uiJoinCity.ToggleVisibility(true);
            }
            else if (selectedUnit.inArmy)
            {
				if (selectedUnit.atHome)
                {
                    uiJoinCity.ToggleVisibility(true);
                    uiSwapPosition.ToggleVisibility(true);
                    uiDeployArmy.ToggleVisibility(true);
                    uiChangeCity.ToggleVisibility(true);
                }
                else if (selectedUnit.transferring)
                {
                    uiChangeCity.ToggleVisibility(true);
                }
                else
                {
                    uiCancelTask.ToggleVisibility(true);
                }
			}
        }
        else
        {
            uiJoinCity.ToggleVisibility(false);
            uiTraderPanel.uiLoadUnload.ToggleInteractable(false);
        }
    }

    public void TurnOnInfoScreen()
    {
        if (selectedTrader != null)
        {
            infoManager.ShowInfoPanel(selectedUnit.name, selectedUnit.buildDataSO, selectedUnit.currentHealth, selectedUnit.isTrader, selectedUnit.isLaborer);
        }
    }

    //private void SpeakingCheck()
    //{
    //    if (selectedUnit.sayingSomething)
    //    {
    //        selectedUnit.sayingSomething = false;
    //        world.StopMessage();
    //    }
    //}

    public void ChangeHomeBase()
    {
        world.cityBuilderManager.PlaySelectAudio();
        uiJoinCity.ToggleVisibility(false);
		uiSwapPosition.ToggleVisibility(false);
		uiDeployArmy.ToggleVisibility(false);
		uiChangeCity.ToggleVisibility(false);
		world.HighlightCitiesWithBarracks(selectedUnit.homeBase);
        uiBuildingSomething.ToggleVisibility(true);
        uiBuildingSomething.SetText("Changing Home Base");
		uiCancelTask.ToggleVisibility(true);
		world.unitOrders = true;
        changingCity = true;
    }

	public void DeployArmyLocation()
    {
        world.cityBuilderManager.PlaySelectAudio();
        
        if (selectedUnit.homeBase.army.isTraining)
        {
            Vector3 mousePosition = uiDeployArmy.transform.position;
            mousePosition.x -= 120;
            UIInfoPopUpHandler.WarningMessage().Create(mousePosition, "Still training", false);
			return;
        }
        else if (selectedUnit.homeBase.army.isTransferring)
        {
			Vector3 mousePosition = uiDeployArmy.transform.position;
			mousePosition.x -= 150;
			UIInfoPopUpHandler.WarningMessage().Create(mousePosition, "Still transferring", false);
            return;
		}
        else if (selectedUnit.homeBase.army.isRepositioning)
        {
			Vector3 mousePosition = uiDeployArmy.transform.position;
			mousePosition.x -= 150;
			UIInfoPopUpHandler.WarningMessage().Create(mousePosition, "Still repositioning", false);
            return;
		}
        
        uiJoinCity.ToggleVisibility(false);
        uiSwapPosition.ToggleVisibility(false);
        uiDeployArmy.ToggleVisibility(false);
        uiChangeCity.ToggleVisibility(false);
        uiBuildingSomething.ToggleVisibility(true);
		uiBuildingSomething.SetText("Deploying Army");
        uiCancelTask.ToggleVisibility(true);
		world.unitOrders = true;
        deployingArmy = true;
        world.HighlightAllEnemyCamps();
    }

    public void CancelArmyDeploymentButton()
    {
        world.cityBuilderManager.PlayCloseAudio();
        CancelArmyDeployment();
    }

    public void CancelArmyDeployment()
    {
		uiCancelTask.ToggleVisibility(false);
        world.uiCampTooltip.ToggleVisibility(false);

        if (selectedUnit == null)
            return;

        if (selectedUnit.homeBase.army.traveling)
        {
            GameLoader.Instance.gameData.attackedEnemyBases.Remove(selectedUnit.homeBase.army.EnemyTarget);
            selectedUnit.homeBase.army.MoveArmyHome(selectedUnit.homeBase.barracksLocation);
            world.EnemyCampReturn(selectedUnit.homeBase.army.EnemyTarget);
            //uiDeployArmy.ToggleTweenVisibility(true);
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
                
            uiJoinCity.ToggleVisibility(true);
		    uiSwapPosition.ToggleVisibility(true);
            uiDeployArmy.ToggleVisibility(true);
			uiChangeCity.ToggleVisibility(true);
			uiBuildingSomething.ToggleVisibility(false);
            world.unitOrders = false;
            deployingArmy = false;
            changingCity = false;
        }
    }

    public void DeployArmy()
    {
        if (world.uiCampTooltip.cantAfford)
        {
			StartCoroutine(world.uiCampTooltip.Shake());
			UIInfoPopUpHandler.WarningMessage().Create(world.uiCampTooltip.attackButton.transform.position, "Can't afford", false);
			return;
        }

        world.cityBuilderManager.PlayBoomAudio();

		HideBattlePath();
        selectedUnit.homeBase.army.DeployArmy();
        world.uiCampTooltip.ToggleVisibility(false, null, null, null, false);
        uiBuildingSomething.ToggleVisibility(false);
		world.UnhighlightAllEnemyCamps();
		//world.citySelected = true;
		world.unitOrders = false;
		deployingArmy = false;
		world.SetEnemyCampAsAttacked(potentialAttackLoc, selectedUnit.homeBase.army);
		selectedUnit.homeBase.army.targetCamp = world.GetEnemyCamp(potentialAttackLoc);
	}

    public void HideBattlePath()
    {
        if (selectedUnit != null)
        {
            selectedUnit.homeBase.army.HidePath();
		    world.HighlightEnemyCamp(potentialAttackLoc, Color.red);
        }
	}

    public void CancelOrders()
    {
        if (selectedUnit.inArmy)
            CancelArmyDeployment();
        else if (selectedUnit.isTrader)
            CancelTradeRoute();
    }

    public void ClearSelection()
    {
        //selectedTile = null;
        if (selectedUnit != null)
        {
            //world.somethingSelected = false;
            if (selectedUnit.isBusy && selectedWorker.IsOrderListMoreThanZero())
                ToggleOrderHighlights(false);

            if (moveUnit)
            {
                if (selectedUnit.isLaborer)
                    world.UnhighlightCitiesAndWonders();
                else if (selectedUnit.isTrader)
                    world.UnhighlightCitiesAndWondersAndTradeCenters(selectedUnit.bySea);
            }

			//SpeakingCheck();
			world.cityBuilderManager.uiTraderNamer.ToggleVisibility(false);
			moveUnit = false;
            uiMoveUnit.ToggleVisibility(false);
            uiCancelMove.ToggleVisibility(false);
            uiJoinCity.ToggleVisibility(false);
            uiSwapPosition.ToggleVisibility(false);
            uiDeployArmy.ToggleVisibility(false);
            uiChangeCity.ToggleVisibility(false);

            if (selectedWorker != null)
            {
                uiCancelTask.ToggleVisibility(false);
                uiWorkerTask.ToggleVisibility(false, world);
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
				uiCancelTask.ToggleVisibility(false);
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
