using Mono.Cecil;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Resources;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;
using static UnityEditor.FilePathAttribute;
using static UnityEditor.PlayerSettings;

public class UnitMovement : MonoBehaviour
{
    [SerializeField]
    public MapWorld world;
    [HideInInspector]
    public InfoManager infoManager;
    [SerializeField]
    public WorkerTaskManager workerTaskManager;
    [SerializeField]
    public UISingleConditionalButtonHandler uiJoinCity, uiMoveUnit, uiCancelTask, uiConfirmOrders, uiDeployArmy, uiSwapPosition, uiChangeCity, uiAssignGuard; 
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
    public Trader selectedTrader;
    //private TerrainData selectedTile;
    //private InfoProvider selectedUnitInfoProvider;
    private bool loadScreenSet; //flag if load/unload ui is showing
    [HideInInspector]
    public bool moveUnit;

    //for transferring cargo to/from trader
    private ResourceManager cityResourceManager;
    private Wonder wonder;
    private TradeCenter tradeCenter;
    public int cityTraderIncrement = 1;

    //for deploying army
    private Vector3Int potentialAttackLoc;

    //for worker orders
    [HideInInspector]
    public bool buildingRoad, removingAll, removingRoad, removingLiquid, removingPower, unitSelected, swappingArmy, deployingArmy, changingCity, assigningGuard, attackMovingTarget;

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
        infoManager = GetComponent<InfoManager>();
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
            if (selectedUnit.trader && selectedTrader.followingRoute)
                CancelTradeRoute();
        }
    }

    public void HandleG()
    {
        if (selectedUnit != null)
        {
            if (selectedUnit.isBusy)
                return;

            if (world.cityBuilderManager.uiCityNamer.activeStatus)
                return;

			if (selectedUnit.inArmy)
            {
                if (uiDeployArmy.activeStatus)
                    DeployArmyLocation();
            }
            else
            {
                if (uiMoveUnit.activeStatus)
                    MoveUnitToggle();
            }
        }
    }

    public void HandleX()
    {
        if (selectedUnit != null && selectedUnit.inArmy && uiChangeCity.activeStatus && !world.cityBuilderManager.uiCityNamer.activeStatus)
            ChangeHomeBase();
    }

    public void HandleSpace()
    {
        if ((world.mainPlayer.isSelected && world.unitOrders) || world.buildingWonder && !world.cityBuilderManager.uiCityNamer.activeStatus)
            ConfirmWorkerOrders();
    }

    public void HandleB()
    {
        if (uiJoinCity.activeStatus && !world.cityBuilderManager.uiCityNamer.activeStatus)
            JoinCity();
    }

    public void HandleR()
    {
        if (uiSwapPosition.activeStatus && !world.cityBuilderManager.uiCityNamer.activeStatus)
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
                    if (unit.inArmy && !unit.military.homeBase.army.selected)
                        unit.Unhighlight();
                    else if (unit.trader)
                        unit.Unhighlight();
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
                else if (td.terrainData.type == TerrainType.River && !world.bridgeResearched)
                {
                    if (td.straightRiver)
                        UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Research bridge building first");
                    else
						UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't build road on this river section");
				}
                else if (td.terrainData.type == TerrainType.River && !td.straightRiver)
                {
					UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't build road on this river section");
				}
                else if (world.CheckIfEnemyTerritory(pos))
                {
                    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Not in enemy territory");
                }
                else
                {
                    if (world.scott.AddToOrderQueue(pos))
                    {
                        if (world.scott.IsOrderListMoreThanZero())
                            uiConfirmOrders.ToggleVisibility(true);

                        td.EnableHighlight(Color.white);

                        world.utilityCostDisplay.ShowUtilityCostDisplay(pos);
                        if (td.straightRiver)
                            world.utilityCostDisplay.AddCost(world.scott.currentUtilityCost.bridgeCost, world.mainPlayer.personalResourceManager.ResourceDict, true);
                        else
                            world.utilityCostDisplay.AddCost(world.scott.currentUtilityCost.utilityCost, world.mainPlayer.personalResourceManager.ResourceDict, true);

                        world.cityBuilderManager.PlaySelectAudio();
                        //highlightedTiles.Add(td);
                    }
                    else
                    {
                        if (!world.scott.IsOrderListMoreThanZero())
                            uiConfirmOrders.ToggleVisibility(false);

                        if (td.straightRiver)
							world.utilityCostDisplay.SubtractCost(world.scott.currentUtilityCost.bridgeCost, world.mainPlayer.personalResourceManager.ResourceDict, true);
                        else
							world.utilityCostDisplay.SubtractCost(world.scott.currentUtilityCost.utilityCost, world.mainPlayer.personalResourceManager.ResourceDict, true);

                        if (world.utilityCostDisplay.currentLoc == pos)
                            world.utilityCostDisplay.ShowUtilityCostDisplay(world.scott.OrderList[world.scott.OrderList.Count - 1]);
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
                    if (world.scott.AddToOrderQueue(pos))
                    {
                        if (world.scott.IsOrderListMoreThanZero())
                            uiConfirmOrders.ToggleVisibility(true);

                        td.EnableHighlight(Color.red);

						world.utilityCostDisplay.ShowUtilityCostDisplay(pos);
						if (td.straightRiver)
							world.utilityCostDisplay.AddCost(world.scott.currentUtilityCost.bridgeCost, world.mainPlayer.personalResourceManager.ResourceDict, false);
						else
							world.utilityCostDisplay.AddCost(world.scott.currentUtilityCost.utilityCost, world.mainPlayer.personalResourceManager.ResourceDict, false);

                        if (world.utilityCostDisplay.inventoryCount > world.mainPlayer.personalResourceManager.resourceStorageLimit - world.mainPlayer.personalResourceManager.ResourceStorageLevel)
							UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Not enough storage space");

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
                        if (!world.scott.IsOrderListMoreThanZero())
                            uiConfirmOrders.ToggleVisibility(false);

						if (td.straightRiver)
							world.utilityCostDisplay.SubtractCost(world.scott.currentUtilityCost.bridgeCost, world.mainPlayer.personalResourceManager.ResourceDict, false);
						else
							world.utilityCostDisplay.SubtractCost(world.scott.currentUtilityCost.utilityCost, world.mainPlayer.personalResourceManager.ResourceDict, false);

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
                City homeBase = selectedUnit.military.homeBase;

				if (selectedUnit.isMoving)
                {
                    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Not ready yet");
                }
                else if (homeBase.army.CheckIfInBase(loc))
                {
                    if (loc == selectedUnit.currentLocation)
                        return;

                    homeBase.army.isRepositioning = true;
                    bool swapping = false;
                    selectedUnit.military.repositioning = true;
                    selectedUnit.military.atHome = false;

                    if (world.IsUnitLocationTaken(loc))
                    {
                        Unit unit = world.GetUnit(loc);
                        swapping = true;
                        unit.military.barracksBunk = selectedUnit.currentLocation;

                        if (unit.military.atHome)
                        {
                            unit.finalDestinationLoc = selectedUnit.currentLocation;
                            unit.MoveThroughPath(GridSearch.AStarSearch(world, loc, selectedUnit.currentLocation, false, unit.bySea));
                        }
                    }
                    else
                    {
                        Vector3 starLoc = loc;
                        starLoc.y += .1f;
                        starshine.transform.position = starLoc;
                        starshine.Play();

                        foreach (Unit unit in homeBase.army.UnitsInArmy)
                        {
                            if (unit == selectedUnit)
                                continue;

                            if (unit.military.barracksBunk == loc)
                            {
                                swapping = true;
                                unit.military.barracksBunk = selectedUnit.currentLocation;

                                if (unit.isMoving)
                                {
                                    unit.StopAnimation();
                                    unit.ShiftMovement();
                                }

                                unit.finalDestinationLoc = selectedUnit.currentLocation;
                                unit.MoveThroughPath(GridSearch.AStarSearch(world, loc, selectedUnit.currentLocation, false, unit.bySea));

                                break;
                            }
                        }
                    }

                    if (!swapping)
                        homeBase.army.UpdateLocation(selectedUnit.currentLocation, loc);
                    selectedUnit.military.barracksBunk = loc;
                    selectedUnit.finalDestinationLoc = loc;
                    selectedUnit.MoveThroughPath(GridSearch.AStarSearch(world, selectedUnit.currentLocation, loc, false, selectedUnit.bySea));

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
                City homeBase = selectedUnit.military.homeBase;

				if (detectedObject.TryGetComponent(out Military unit) && unit.enemyAI && unit.enemyCamp.movingOut)
                {
                    if (unit.preparingToMoveOut)
                        return;

                    if (unit.enemyCamp.attacked || unit.enemyCamp.inBattle || unit.enemyCamp.attackReady)
                    {
						UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Already attacked");
						return;
                    }

                    if (world.IsCityOnTile(unit.enemyCamp.moveToLoc))
					{
                        if (homeBase.attacked && unit.enemyCamp.moveToLoc != homeBase.cityLoc)
                        {
						    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Select attacking enemy only");
                        }
                        else if (world.GetCity(unit.enemyCamp.moveToLoc).army.defending)
                        {
							UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Enemy too close to target to get to in time");
						}
                        else 
                        {
							if (homeBase.army.DeployArmyMovingTargetCheck(world.GetClosestTerrainLoc(selectedUnit.currentLocation), unit.enemyCamp.cityLoc, unit.enemyCamp.pathToTarget, unit.enemyCamp.lastSpot))
                            {
							    for (int i = 0; i < unit.enemyCamp.UnitsInCamp.Count; i++)
                                    unit.enemyCamp.UnitsInCamp[i].SoftSelect(Color.white);

                                attackMovingTarget = true;
								homeBase.army.ShowBattlePath();
								potentialAttackLoc = unit.enemyCamp.cityLoc;
								world.infoPopUpCanvas.gameObject.SetActive(true);
								world.uiCampTooltip.ToggleVisibility(true, null, unit.enemyCamp, homeBase.army, true, true);
                            }
                            else
                            {
								UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Too close to target city");
							}
                        }

                        return;
					}
                }
                else if (world.CheckIfEnemyCamp(pos) || world.IsEnemyCityOnTile(pos))
                {
                    if (homeBase.attacked)
                        return;
                    
                    if (world.CheckIfEnemyAlreadyAttacked(pos))
                    {
						UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Already sending troops");
						return;
                    }

					attackMovingTarget = false;

					if (homeBase.army.DeployArmyCheck(world.GetClosestTerrainLoc(selectedUnit.currentLocation), pos))
                    {
                        homeBase.army.ShowBattlePath();

                        //rehighlight in case selecting a different one
                        if (world.IsEnemyCityOnTile(potentialAttackLoc))
                            world.HighlightEnemyCity(potentialAttackLoc, Color.red);
                        else
                            world.HighlightEnemyCamp(potentialAttackLoc, Color.red);

                        potentialAttackLoc = pos;
						world.infoPopUpCanvas.gameObject.SetActive(true);
                        if (world.IsEnemyCityOnTile(pos))
                        {
                            world.HighlightEnemyCity(pos, Color.white);
							world.uiCampTooltip.ToggleVisibility(true, null, world.GetEnemyCity(pos).enemyCamp, homeBase.army);
						}
                        else
                        {
                            world.HighlightEnemyCamp(pos, Color.white);
						    world.uiCampTooltip.ToggleVisibility(true, null, world.GetEnemyCamp(pos), homeBase.army);
                        }
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
                City homeBase = selectedUnit.military.homeBase;

				if (world.IsCityOnTile(pos) && world.GetCity(pos).highlighted)
                {                    
                    City newCity = world.GetCity(pos);
                    
                    if (newCity.attacked)
                    {
						UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't transfer now, enemy approaching");
						return;
                    }
                    
                    Vector3Int newLoc = newCity.army.GetAvailablePosition(selectedUnit.buildDataSO.unitType);
                    List<Vector3Int> path = GridSearch.AStarSearch(world, selectedUnit.transform.position, newLoc, false, selectedUnit.bySea);
					if (homeBase != null)
                        homeBase.army.UnselectArmy(selectedUnit);

                    if (path.Count > 0)
                    {
                        TransferMilitaryUnit(selectedUnit, newCity, newLoc, path);
						world.citySelected = true;
						world.UnhighlightCitiesWithBarracks();
						world.unitOrders = false;
						changingCity = false;
						uiBuildingSomething.ToggleVisibility(false);
					}

					location.y += .1f;
					starshine.transform.position = location;
					starshine.Play();
					world.cityBuilderManager.MoveUnitAudio();
                }
                else
                {
					UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Select new city with room in barracks");
				}
            }
            else if (assigningGuard)
            {
                if (detectedObject.TryGetComponent(out Trader trader))
                {
                    if (trader.isSelected)
                    {
                        Vector3Int currentLoc = world.RoundToInt(selectedUnit.transform.position);
                        Vector3Int traderLoc = world.RoundToInt(trader.transform.position);

						//going to assigned trader to guard
						List<Vector3Int> path = GridSearch.AStarSearch(world, currentLoc, traderLoc, false, selectedUnit.bySea);

                        if (path.Count == 0)
                        {
							if (Mathf.Abs(traderLoc.x - currentLoc.x) + Mathf.Abs(traderLoc.z - currentLoc.z) > 2)
                            {
							    //no need for message
                                return;
                            }
                            else
                            {
                                foreach (Vector3Int tile in world.GetNeighborsFor(traderLoc, MapWorld.State.EIGHTWAY))
                                {
                                    if (world.CheckIfPositionIsValid(tile))
                                    {
                                        path.Add(tile);
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
							foreach (Vector3Int tile in world.GetNeighborsFor(traderLoc, MapWorld.State.EIGHTWAY))
							{
								if (world.CheckIfPositionIsValid(tile))
								{
                                    path.RemoveAt(path.Count - 1);
                                    path.Add(tile);
									break;
								}
							}
						}


						if (selectedUnit.isMoving)
						{
							selectedUnit.StopAnimation();
							selectedUnit.ShiftMovement();
							selectedUnit.FinishedMoving.RemoveAllListeners();
						}

						world.UnhighlightTraders();
						uiBuildingSomething.ToggleVisibility(false);
						uiChangeCity.ToggleVisibility(true);
						uiAssignGuard.ToggleVisibility(true);
						uiCancelTask.ToggleVisibility(false);
						world.unitOrders = false;
						assigningGuard = false;

						if (selectedUnit.military)
                        {
						    selectedUnit.military.homeBase.army.UnselectArmy(selectedUnit);
                            selectedUnit.military.homeBase.army.RemoveFromArmy(selectedUnit, selectedUnit.military.barracksBunk);
							selectedUnit.military.homeBase = null;
                            selectedUnit.military.atHome = false;
                        }
                        else if (selectedUnit.military.guardedTrader != null)
                        {
                            selectedUnit.military.guardedTrader.guarded = false;
                            selectedUnit.military.guardedTrader.guardUnit = null;
							selectedUnit.military.guardedTrader = null;
                            selectedUnit.originalMoveSpeed = selectedUnit.buildDataSO.movementSpeed;
                            selectedUnit.military.isGuarding = false;
                        }

						trader.guarded = true;
						trader.guardUnit = selectedUnit;
						trader.waitingOnGuard = true; //use this to wait for guard to show up
                        trader.SoftSelect(Color.white);
						selectedUnit.military.guardedTrader = trader;
                        selectedUnit.military.guard = true;
                        selectedUnit.military.transferring = true;
                        selectedUnit.finalDestinationLoc = path[path.Count - 1] /*+ new Vector3(0.5f,0,0)*/; //moving to the side a smidge
                        selectedUnit.MoveThroughPath(path);

						location.y += .1f;
						starshine.transform.position = location;
						starshine.Play();
						world.cityBuilderManager.MoveUnitAudio();
                        selectedUnit.military.ToggleIdleTimer(false);
					}
                    else
                    {
                        if (trader.guarded)
							UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Trader already has guard");
                        else if (trader.followingRoute)
							UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Trader currently following route");
						else if (trader.isMoving)
							UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Not at city");
						else if (selectedUnit.bySea)
                            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Select sea trader only");
                        else
							UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Select land trader only");

						return;
					}
                }
                else
                {
					UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Select trader, silly");
					return;
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
            {
                SelectEnemy(unitReference);
            }
            else if (unitReference.CompareTag("Character"))
            {
                SelectCharacter(unitReference);
            }
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
            else if (unitMarker.CompareTag("Character"))
            {
                SelectCharacter(unitReference);
            }
        }
        else if (detectedObject.TryGetComponent(out Resource resource))
        {
            Worker tempWorker = resource.GetHarvestingWorker();

            if (tempWorker == null)
            {
                ClearSelection();
            }
            else if (tempWorker == world.mainPlayer)
            {
                world.mainPlayer.SendResourceToCity();
            }
            else if (tempWorker == world.scott)
            {
                world.scott.SendResourceToCity();
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

    public void TransferMilitaryUnit(Unit unit, City newCity, Vector3Int newLoc, List<Vector3Int> path)
    {
		if (unit.isMoving)
		{
			unit.StopAnimation();
			unit.ShiftMovement();
			unit.FinishedMoving.RemoveAllListeners();
		}

		if (unit.military.guardedTrader != null)
		{
			unit.military.guardedTrader.Unhighlight();
			unit.military.guardedTrader.guarded = false;
			unit.military.guardedTrader.guardUnit = null;
			unit.military.guardedTrader = null;
			unit.military.guard = false;
			unit.originalMoveSpeed = unit.buildDataSO.movementSpeed;
			unit.military.isGuarding = false;
		}
		else
		{
			unit.military.homeBase.army.RemoveFromArmy(unit, unit.military.barracksBunk);
		}

		unit.military.homeBase = newCity;
		unit.military.atHome = false;
		newCity.army.AddToArmy(unit);

		if (newCity.currentPop == 0 && newCity.army.armyCount == 1)
			newCity.StartGrowthCycle(false);

		unit.military.barracksBunk = newLoc;
		unit.military.transferring = true;
		unit.military.homeBase.army.isTransferring = true;

		unit.finalDestinationLoc = newLoc;
		unit.MoveThroughPath(path);
		
        if (unit.isSelected)
        {
		    uiChangeCity.ToggleVisibility(true);
		    uiAssignGuard.ToggleVisibility(true);
		    uiCancelTask.ToggleVisibility(false);
        }

        unit.military.ToggleIdleTimer(false);
	}

    //private void SelectLaborer()
    //{
    //    if (selectedUnit.isLaborer)
    //    {
    //        Laborer laborer = selectedUnit.GetComponent<Laborer>();
    //        //if (laborer.co != null)
    //        //{
    //        //    StopCoroutine(laborer.Celebrate());
    //        //    laborer.StopLaborAnimations();
    //        //}
    //    }
    //}

    private void SelectUnitPrep(Unit unitReference, Vector3 location)
    {
        if (upgradingUnit && highlightedUnitList.Contains(unitReference))
        {
            world.cityBuilderManager.UpgradeUnitWindow(unitReference);
            return;
        }
        
        if (selectedUnit == unitReference) //Unselect when clicking same unit
        {
            if (selectedUnit != null && selectedUnit.harvested)
                selectedUnit.SendResourceToCity();
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

        if (world.characterUnits.Contains(selectedUnit) && world.mainPlayer.somethingToSay)
        {
            selectedUnit = world.mainPlayer;
            
            if (world.mainPlayer.harvested) //if unit just finished harvesting something, send to closest city
				world.mainPlayer.SendResourceToCity();
            
            if (world.scott.harvested)
				world.scott.SendResourceToCity();

            QuickSelect(selectedUnit);
			selectedUnit.GetComponent<Worker>().SpeakingCheck();
        }
        else
        {
            //SelectLaborer();
            SelectWorker();
            SelectTrader();

            if (!selectedUnit.isLaborer && !selectedUnit.trader && !selectedUnit.inArmy && !selectedUnit.isUpgrading && !selectedUnit.runningAway)
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
		selectedUnit.Highlight(Color.red);
		infoManager.ShowInfoPanel(selectedUnit.name, selectedUnit.buildDataSO, selectedUnit.currentHealth, selectedUnit.isTrader/*, selectedUnit.isLaborer*/);
	}

    private void SelectCharacter(Unit unitReference)
    {
        if (selectedUnit == unitReference)
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

		selectedUnit.SayHello();
		world.somethingSelected = true;
		selectedUnit.Highlight(new Color(.5f, 0, 1));
		infoManager.ShowInfoPanel(selectedUnit.name, selectedUnit.buildDataSO, selectedUnit.currentHealth, selectedUnit.isTrader/*, selectedUnit.isLaborer*/);
	}

	public void SelectWorker()
    {        
		if (world.characterUnits.Contains(selectedUnit))
        {
            selectedUnit = world.mainPlayer;

			uiPersonalResourceInfoPanel.SetTitleInfo(world.mainPlayer.name, world.mainPlayer.personalResourceManager.ResourceStorageLevel, world.mainPlayer.personalResourceManager.resourceStorageLimit);
			uiPersonalResourceInfoPanel.ToggleVisibility(true, world.mainPlayer);

            workerTaskManager.SetWorkerUnit();
            if (!selectedUnit.sayingSomething)
                uiWorkerTask.ToggleVisibility(true, world);
            if (world.scott.IsOrderListMoreThanZero())
                ToggleOrderHighlights(true);

            if (selectedUnit.harvested) //if unit just finished harvesting something, send to closest city
                selectedUnit.SendResourceToCity();

            Vector3Int terrainLoc = world.GetClosestTerrainLoc(selectedUnit.transform.position);
            if (world.IsTradeLocOnTile(terrainLoc) && !world.IsWonderOnTile(terrainLoc))
                uiWorkerTask.uiLoadUnload.ToggleInteractable(true);
            else
                uiWorkerTask.uiLoadUnload.ToggleInteractable(false);

            for (int i = 0; i < world.characterUnits.Count; i++)
            {
				if (world.characterUnits[i] == selectedUnit)
					world.characterUnits[i].Highlight(Color.green);
				else
					world.characterUnits[i].SoftSelect(Color.white);
			}
        }
    }

    private void SelectTrader()
    {
        if (selectedUnit.trader)
        {
            selectedTrader = selectedUnit.trader;
            if (selectedTrader.guardLeft)
                selectedTrader.ShowGuardLeftMessage();
            if (selectedTrader.interruptedRoute)
                selectedTrader.InterruptedRouteMessage();
            uiPersonalResourceInfoPanel.SetTitleInfo(selectedTrader.name, selectedTrader.personalResourceManager.ResourceStorageLevel, selectedTrader.personalResourceManager.resourceStorageLimit);
            uiPersonalResourceInfoPanel.ToggleVisibility(true, selectedTrader);
            world.traderCanvas.gameObject.SetActive(true);
            world.personalResourceCanvas.gameObject.SetActive(true);
            uiTraderPanel.ToggleVisibility(true);

            //if (world.IsTradeLocOnTile(world.RoundToInt(selectedTrader.transform.position)) && !selectedTrader.followingRoute)
            //{
            //    uiTraderPanel.uiLoadUnload.ToggleInteractable(true);
            //}
            //else
            //{
            //    uiTraderPanel.uiLoadUnload.ToggleInteractable(false);
            //}

            if (selectedTrader.hasRoute/* && !selectedTrader.followingRoute && !selectedTrader.interruptedRoute*/)
            {
                uiTraderPanel.uiBeginTradeRoute.ToggleInteractable(true);

                if (selectedTrader.followingRoute)
                {
                    uiTraderPanel.SwitchRouteIcons(true);
                }
            }
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

		//SelectLaborer();
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
        {
            if (selectedUnit.military.guard)
            {
                selectedUnit.military.guardedTrader.SoftSelect(Color.white);
                selectedUnit.Highlight(Color.green);
            }
            else
            {
    			selectedUnit.military.homeBase.army.SelectArmy(selectedUnit);
            }
        }
        else if (selectedUnit.trader && selectedTrader.guarded)
        {
			selectedUnit.Highlight(Color.green);
			selectedTrader.guardUnit.SoftSelect(Color.white);
		}
		else if (!world.characterUnits.Contains(selectedUnit)) //selection handled elsewhere
        {
			selectedUnit.Highlight(Color.green);
        }

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
        infoManager.ShowInfoPanel(selectedUnit.name, selectedUnit.buildDataSO, selectedUnit.currentHealth, selectedUnit.isTrader/*, selectedUnit.isLaborer*/);
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

        Vector3Int startPosition = world.RoundToInt(unit.transform.position);
        movementSystem.GetPathToMove(world, unit, startPosition, terrainPos, unit.isTrader); //Call AStar movement

        unit.finalDestinationLoc = location;

        if (!queueMovementOrders)
        {
            moveUnit = false;
            uiMoveUnit.ToggleButtonColor(false);
			if (unit.trader)
				world.UnhighlightCitiesAndWondersAndTradeCenters(unit.bySea);
			else if (unit.isLaborer)
				world.UnhighlightCitiesAndWonders();

            if (movementSystem.MoveUnit(unit))
            {
                if (unit.isPlayer && world.scottFollow)
                {
                    unit.firstStep = true;
                }
                else if (unit.trader && selectedTrader.guarded)
                {
                    List<Vector3Int> guardPath = movementSystem.GetGuardPath(startPosition);
                    selectedTrader.guardUnit.finalDestinationLoc = guardPath[guardPath.Count - 1];
                    selectedTrader.guardUnit.MoveThroughPath(guardPath);
                }
            }
            else
            {
                return;
            }
        }

        //if (!unit.isTrader)
        //    uiCancelMove.ToggleVisibility(!unit.isBusy);
        
        movementSystem.ClearPaths();
        uiJoinCity.ToggleVisibility(false);
        uiWorkerTask.uiLoadUnload.ToggleInteractable(false);
    }

	public void GoStraightToSelectedLocation(Vector3 location, Vector3Int terrainPos, Unit unit)
	{
		if (unit.isMoving)
		{
			unit.StopAnimation();
			unit.ShiftMovement();
			unit.ResetMovementOrders();
		}

        if (unit.worker.building)
        {
		    if (!world.IsTileOpenCheck(terrainPos) || !world.CheckIfPositionIsValid(terrainPos))
		    {
			    unit.worker.SkipRoadBuild();
                if (world.mainPlayer.isSelected)
                    world.GetTerrainDataAt(terrainPos).DisableHighlight();
			    return;
		    }
        }

		//Vector3Int originalLoc = world.RoundToInt(unit.transform.position);
		List<Vector3Int> path = GridSearch.AStarSearch(world, unit.transform.position, terrainPos, unit.isTrader, unit.bySea);
        //movementSystem.GetPathToMove(world, unit, originalLoc, terrainPos, unit.isTrader); //Call AStar movement

		moveUnit = false;
		uiMoveUnit.ToggleButtonColor(false);

        if (path.Count > 0)
        {
    		unit.finalDestinationLoc = location;
            unit.MoveThroughPath(path);
        }
        else
        {
            return;
        }
		//if (!movementSystem.MoveUnit(unit))
		//	return;

        if (unit.isPlayer)
        {
            if (world.scottFollow && !world.mainPlayer.isBusy)
            {
                world.scott.GoToPosition(terrainPos, true);
            }
            
            if (world.azaiFollow)
            {
                world.azai.GoToPosition(terrainPos, false);
                //world.azai.ShiftMovement();
                //List<Vector3Int> azaiPath = new(path);
                //azaiPath.Insert(0, originalLoc);
                //azaiPath.RemoveAt(azaiPath.Count - 1);
                ////don't move if already there
                //if (world.RoundToInt(world.azai.transform.position) != azaiPath[azaiPath.Count - 1])
                //{
                //    world.azai.finalDestinationLoc = azaiPath[azaiPath.Count - 1];
                //    world.azai.MoveThroughPath(azaiPath);
                //}
            }
		}
		//uiCancelMove.ToggleVisibility(!unit.isBusy);

		//movementSystem.ClearPaths();
		uiJoinCity.ToggleVisibility(false);
		uiWorkerTask.uiLoadUnload.ToggleInteractable(false);
	}

	public void HandleSelectedFollowerLoc(Queue<Vector3Int> path, Vector3Int priorSpot, Vector3Int currentSpot, Vector3Int finalSpot)
    {
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

        List<Vector3Int> scottPath;
        List<Vector3Int> azaiPath;
        Vector3Int scottSpot = world.RoundToInt(world.scott.transform.position);
        if (path.Count > 0)
        {
            List<Vector3Int> tempPath = path.ToList();
            tempPath.Remove(finalSpot); //remove last loc
            tempPath.Insert(0, currentSpot);

            //Vector3Int newLastStep = tempPath[tempPath.Count - 1];
 
            scottPath = new(tempPath);
            azaiPath = new(tempPath);
        }
        else
        {
            scottPath = new();
            azaiPath = new();
        }

        //in case player moves only one tile
        scottPath.Insert(0, priorSpot);
        azaiPath.Insert(0, priorSpot);
        Vector3Int finalScottSpot = scottPath[scottPath.Count - 1];
		world.scott.finalDestinationLoc = finalScottSpot;

    	azaiPath.Insert(0, scottSpot);

        moveUnit = false;
        world.scott.MoveThroughPath(scottPath);

        if (world.azaiFollow)
        {
            world.azai.finalDestinationLoc = azaiPath[azaiPath.Count - 1]; //last spot for now, will change once arriving at final loc; 
            world.azai.MoveThroughPath(azaiPath);
        }
        //unit.followerPath.Clear();
	}

    public void MoveUnitRightClick(Vector3 location, GameObject detectedObject)
    {
		if (selectedUnit == null || !selectedUnit.CompareTag("Player") || selectedUnit.inArmy || selectedUnit.isLaborer)
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
        if (selectedUnit.ambush)
        {
			UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Currently being ambushed...", false);
			return; //can't manually move when ambushed
        }

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

		//if (selectedUnit.isLaborer)
		//{
  //          Vector3Int terrainLoc = world.GetClosestTerrainLoc(locationInt);

		//	if (detectedObject.TryGetComponent(out City city) && detectedObject.CompareTag("Player"))
		//	{
  //              //locationInt = city.cityLoc;
  //              world.citySelected = leftClick;
		//	}
  //          else if (world.IsCityOnTile(terrainLoc))
  //          {
  //              //locationInt = terrainLoc;
		//		world.citySelected = leftClick;
		//	}
		//	else if (detectedObject.TryGetComponent(out Wonder wonder))
		//	{
  //              if (wonder.isConstructing)
  //              {
		//			UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Move to city or constructing wonder");
		//			return;
  //              }

  //              locationInt = wonder.unloadLoc;
  //              world.citySelected = leftClick;
		//	}
  //          else if (world.IsWonderOnTile(terrainLoc))
  //          {
  //              Wonder wonderLoc = world.GetWonder(world.GetClosestTerrainLoc(locationInt));
  //              locationInt = wonderLoc.unloadLoc;
		//		world.citySelected = leftClick;
		//	}
		//	else
		//	{
		//		UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Move to city or constructing wonder");
		//		return;
		//	}

		//	//locationFlat = locationInt;
		//}

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

            selectedUnit.TurnOnRipples();
        }
        else if (world.CheckIfEnemyTerritory(locationInt))
        {
			UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Enemy territory");
			return;
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

        if (selectedUnit.trader)
        {
            if (selectedTrader.waitingOnGuard)
            {
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Waiting for guard to arrive");
				return;
            }
            
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

            if (selectedTrader.guarded)
            {
				if (selectedTrader.guardUnit.isMoving && !queueMovementOrders)
                {
                    selectedTrader.guardUnit.StopAnimation();
				    selectedTrader.guardUnit.ShiftMovement();
				    selectedTrader.guardUnit.FinishedMoving.RemoveAllListeners();
                }

                selectedTrader.guardUnit.military.ToggleIdleTimer(false);
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
            world.citySelected = true;

            if (selectedUnit.trader)
                world.HighlightCitiesAndWondersAndTradeCenters(selectedUnit.bySea);
            else if (selectedUnit.isLaborer)
                world.HighlightCitiesAndWonders();
        }
        else
        {
            moveUnit = false;
			world.citySelected = false;

			if (selectedUnit.trader)
				world.UnhighlightCitiesAndWondersAndTradeCenters(selectedUnit.bySea);
			else if (selectedUnit.isLaborer)
				world.UnhighlightCitiesAndWonders();
		}

		uiMoveUnit.ToggleButtonColor(moveUnit);
    }

    public void CancelMove()
    {
        if (selectedUnit.runningAway)
            return;
        
        moveUnit = false;
		world.citySelected = false;

		if (selectedUnit.trader)
			world.UnhighlightCitiesAndWondersAndTradeCenters(selectedUnit.bySea);
		else if (selectedUnit.isLaborer)
			world.UnhighlightCitiesAndWonders();

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
        uiCancelTask.ToggleVisibility(v);
    }

    //public void CancelContinuedMovementOrdersButton()
    //{
    //    world.cityBuilderManager.PlaySelectAudio();
    //    CancelContinuedMovementOrders();
    //}

    //public void CancelContinuedMovementOrders()
    //{
    //    if (selectedUnit != null && !selectedUnit.runningAway)
    //    {
    //        if (selectedUnit.isTrader && selectedUnit.followingRoute)
    //        {
    //            CancelTradeRoute();
    //            return;
    //        }

    //        selectedUnit.ResetMovementOrders();
    //        uiCancelMove.ToggleVisibility(false);
    //        selectedUnit.HidePath();

    //        if (selectedUnit.isPlayer)
    //        {
    //            //world.scott.ResetMovementOrders();
    //            world.scott.CatchUp(world.RoundToInt(selectedUnit.transform.position));
    //            world.azai.CatchUp(world.RoundToInt(selectedUnit.transform.position));
    //        }
    //    }
    //    else if (world.buildingWonder)
    //    {
    //        world.CloseBuildingSomethingPanel();
    //    }
    //}

    public void JoinCity() //for Join City button
    {
        world.cityBuilderManager.PlaySelectAudio();

        if (selectedUnit.isUpgrading)
        {
			InfoPopUpHandler.WarningMessage().Create(selectedUnit.transform.position, "Currently upgrading");
			return;
        }
        else if (selectedUnit.trader && selectedTrader.guarded)
        {
			InfoPopUpHandler.WarningMessage().Create(selectedUnit.transform.position, "Currently has guard");
			return;
		}

		if (moveUnit)
        {
            moveUnit = false;
            uiMoveUnit.ToggleButtonColor(false);
            if (selectedUnit.trader)
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

            city = selectedUnit.military.homeBase;
			//AddToCity(selectedUnit.homeBase, selectedUnit);
            //selectedUnit.homeBase.army.RemoveFromArmy(selectedUnit, selectedUnit.barracksBunk);
        }

        if (city != null)
            world.uiCityPopIncreasePanel.ToggleVisibility(true, selectedUnit.buildDataSO.laborCost, city, true, selectedUnit.isTrader);
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
			selectedUnit.military.homeBase.army.RemoveFromArmy(selectedUnit, selectedUnit.military.barracksBunk);

		AddToCity(city, selectedUnit);
		selectedUnit.DestroyUnit();
		ClearSelection();
	}

    public void LaborerJoin(Unit unit)
    {
		Vector3Int unitLoc = world.GetClosestTerrainLoc(unit.transform.position);
        bool kill = false;

		if (world.IsCityOnTile(unitLoc))
		{
            City city = world.GetCity(unitLoc);

			//if (city.reachedWaterLimit) //force join
   //             return;
			
            AddToCity(city, unit);
		}
        else if (world.IsWonderOnTile(unitLoc))
        {
            Wonder wonder = world.GetWonder(unitLoc);
            if (wonder.StillNeedsWorkers())
            {
                wonder.AddWorker(unit);
            }
            else
            {
                Laborer laborer = unit.GetComponent<Laborer>();
				laborer.GoToDestination(laborer.homeCityLoc);
                return;
            }
        }
        else
        {
			Laborer laborer = unit.GetComponent<Laborer>();
			
            if (laborer.homeCityLoc != unitLoc)
            {
                laborer.GoToDestination(laborer.homeCityLoc);
                return;
            }

            kill = true;
		}

        if (unit.isSelected)
        {
            world.somethingSelected = false;
            ClearSelection();
        }
		
        if (kill)
        {
            Vector3 rotation = unit.transform.position - unit.prevTile;
            rotation.x = -90;
			world.PlayDeathSplash(unit.transform.position, rotation);
		}

		unit.DestroyUnit();
	}

    public void RepositionArmy()
    {
        world.cityBuilderManager.PlaySelectAudio();
        world.cameraController.CenterCameraNoFollow(world.GetClosestTerrainLoc(selectedUnit.currentLocation));
        uiSwapPosition.ToggleVisibility(false);
        uiJoinCity.ToggleVisibility(false);
        uiDeployArmy.ToggleVisibility(false);
		uiChangeCity.ToggleVisibility(false);
        uiAssignGuard.ToggleVisibility(false);
		uiConfirmOrders.ToggleVisibility(true);
		uiBuildingSomething.ToggleVisibility(true);
        uiBuildingSomething.SetText("Repositioning Unit");
		world.unitOrders = true;
        swappingArmy = true;
        world.SetSelectionCircleLocation(selectedUnit.currentLocation);
    }

    public void CancelReposition()
    {
		if (selectedUnit.military.atHome)
        {
            uiSwapPosition.ToggleVisibility(true);
		    uiJoinCity.ToggleVisibility(true);
		    uiDeployArmy.ToggleVisibility(true);
            uiChangeCity.ToggleVisibility(true);
		    uiAssignGuard.ToggleVisibility(true);
        }
		else if (!selectedUnit.military.homeBase.army.defending && !selectedUnit.military.repositioning)
		{
			uiCancelTask.ToggleVisibility(true);
		}

		uiConfirmOrders.ToggleVisibility(false);
		uiBuildingSomething.ToggleVisibility(false);
		world.unitOrders = false;
        swappingArmy = false;
        world.HideSelectionCircles();
    }

    public void AddToCity(City joinedCity, Unit unit)
    {
        bool joinCity = true;
        
        if (unit.trader)
        {
            world.traderList.Remove(unit.trader);
            joinedCity.tradersHere.Remove(unit);
            joinCity = false;
        }
        else if (unit.isLaborer)
        {
            world.laborerList.Remove(unit.GetComponent<Laborer>());
        }
        
        joinedCity.PopulationGrowthCheck(joinCity, unit.buildDataSO.laborCost);

		int i = 0;
		foreach (ResourceValue resourceValue in unit.buildDataSO.unitCost) //adding back 100% of cost (if there's room)
		{
			int resourcesGiven = joinedCity.ResourceManager.AddResource(resourceValue.resourceType, resourceValue.resourceAmount);
			Vector3 cityLoc = joinedCity.cityLoc;
			cityLoc.y += unit.buildDataSO.unitCost.Count * 0.4f;
			cityLoc.y += -0.4f * i;
			InfoResourcePopUpHandler.CreateResourceStat(cityLoc, resourcesGiven, ResourceHolder.Instance.GetIcon(resourceValue.resourceType));
			i++;
		}
	}

    public void LoadUnloadPrep() //for loadunload button for Koa
    {
		world.cityBuilderManager.PlaySelectAudio();

		if (!loadScreenSet)
        {
            uiWorkerTask.uiLoadUnload.ToggleColor(true);
            selectedUnit.HidePath();
            movementSystem.ClearPaths();
            //uiTradeRouteManager.ToggleVisibility(false);
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
			if (!world.utilityCostDisplay.hasEnough)
            {
                UIInfoPopUpHandler.WarningMessage().Create(world.scott.OrderList[world.scott.OrderList.Count - 1], "Need more supplies", false);
				return;
            }
            
            uiBuildingSomething.ToggleVisibility(false);

			if (swappingArmy)
            {
                CancelReposition();
                return;
            }
            
            ClearBuildRoad();
            if (buildingRoad)
            {
                world.scott.building = true;
                world.scott.SetRoadQueue();
            }
            else if (removingRoad)
            {
                world.scott.removing = true;
                world.scott.SetRoadRemovalQueue();
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
        world.utilityCostDisplay.HideUtilityCostDisplay();
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
            if (deployingArmy || changingCity || assigningGuard)
            {
                CancelArmyDeployment();
                return;
            }
            
            ToggleOrderHighlights(false);
            ClearBuildRoad();
            ResetOrderFlags();

            world.scott.ResetOrderQueue();
            world.mainPlayer.isBusy = false;
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

            if (world.scott.removing)
                highlightColor = Color.red;
            else
                highlightColor = Color.white;

            foreach (Vector3Int tile in world.scott.OrderList)
            {
                world.GetTerrainDataAt(tile).EnableHighlight(highlightColor);

                if (world.scott.removing && world.IsRoadOnTerrain(tile))
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
            foreach (Vector3Int tile in world.scott.OrderList)
            {
                world.GetTerrainDataAt(tile).DisableHighlight();

                if ((world.scott.removing || world.unitOrders) && world.IsRoadOnTerrain(tile))
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

    public void LoadUnloadFinish(bool keepSelection) //putting the screens back after finishing loading cargo
    {
        if (loadScreenSet)
        {
            uiWorkerTask.uiLoadUnload.ToggleColor(false);
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
        world.mainPlayer.personalResourceManager.DictCheckSolo(resourceType);

        if (tradeCenter)
        {
            if (resourceAmount > 0) //buying 
            {
                if (!world.CheckWorldGold(tradeCenter.ResourceBuyDict[resourceType]))
                {
                    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't afford");
                    return;
                }
                
                int resourceAmountAdjusted = world.mainPlayer.personalResourceManager.ManuallyAddResource(resourceType, resourceAmount);

                if (resourceAmountAdjusted == 0)
                {
                    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Full inventory");
                    return;
                }

				//world.cityBuilderManager.PlayRingAudio();
				int buyAmount = -resourceAmountAdjusted * tradeCenter.ResourceBuyDict[resourceType];
                world.UpdateWorldResources(ResourceType.Gold, buyAmount);
                InfoResourcePopUpHandler.CreateResourceStat(world.mainPlayer.transform.position, buyAmount, ResourceHolder.Instance.GetIcon(ResourceType.Gold));

                uiPersonalResourceInfoPanel.UpdateResourceInteractable(resourceType, world.mainPlayer.personalResourceManager.GetResourceDictValue(resourceType), true);
                uiPersonalResourceInfoPanel.UpdateStorageLevel(world.mainPlayer.personalResourceManager.ResourceStorageLevel);
            }
            else if (resourceAmount <= 0) //selling
            {
                if (tradeCenter.ResourceSellDict.ContainsKey(resourceType))
                {
                    int remainingWithTrader = world.mainPlayer.personalResourceManager.GetResourceDictValue(resourceType);

                    if (remainingWithTrader < Mathf.Abs(resourceAmount))
                        resourceAmount = -remainingWithTrader;

                    if (resourceAmount == 0)
                        return;

					//world.cityBuilderManager.PlayRingAudio();
					world.mainPlayer.personalResourceManager.ManuallySubtractResource(resourceType, -resourceAmount);

                    int sellAmount = -resourceAmount * tradeCenter.ResourceSellDict[resourceType];
                    world.UpdateWorldResources(ResourceType.Gold, sellAmount);
                    InfoResourcePopUpHandler.CreateResourceStat(world.mainPlayer.transform.position, sellAmount, ResourceHolder.Instance.GetIcon(ResourceType.Gold));

                    uiPersonalResourceInfoPanel.UpdateResourceInteractable(resourceType, world.mainPlayer.personalResourceManager.GetResourceDictValue(resourceType), false);
                    uiCityResourceInfoPanel.FlashResource(resourceType);
                    uiPersonalResourceInfoPanel.UpdateStorageLevel(world.mainPlayer.personalResourceManager.ResourceStorageLevel);
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

            int resourceAmountAdjusted = world.mainPlayer.personalResourceManager.ManuallyAddResource(resourceType, resourceAmount);
            personalFull = resourceAmountAdjusted == 0;

            if (cityResourceManager != null)
                cityResourceManager.SubtractResource(resourceType, resourceAmountAdjusted);
            //else
            //    wonder.AddResource(resourceType, -resourceAmountAdjusted);
        }

        bool cityFull = false;

        if (resourceAmount <= 0) //moving from trader to city
        {
			//world.cityBuilderManager.PlayRingAudio();
			int remainingWithTrader = world.mainPlayer.personalResourceManager.GetResourceDictValue(resourceType);

            if (remainingWithTrader < Mathf.Abs(resourceAmount))
                resourceAmount = -remainingWithTrader;

            int resourceAmountAdjusted;
            if (cityResourceManager != null)
                resourceAmountAdjusted = cityResourceManager.AddResource(resourceType, -resourceAmount);
            else
                resourceAmountAdjusted = wonder.AddResource(resourceType, -resourceAmount);

            cityFull = resourceAmountAdjusted == 0;
			world.mainPlayer.personalResourceManager.ManuallySubtractResource(resourceType, resourceAmountAdjusted);
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
        else
        {
    		InfoPopUpHandler.WarningMessage().Create(selectedUnit.transform.position, "No storage space");
        }

        if (!personalFull)
        {
            uiPersonalResourceInfoPanel.UpdateResourceInteractable(resourceType, world.mainPlayer.personalResourceManager.GetResourceDictValue(resourceType), toTrader);
            uiPersonalResourceInfoPanel.UpdateStorageLevel(world.mainPlayer.personalResourceManager.ResourceStorageLevel);
        }
        else
        {
			InfoPopUpHandler.WarningMessage().Create(selectedUnit.transform.position, "No storage space");
		}

		world.mainPlayer.personalResourceManager.ResetDictSolo(resourceType);
    }

    public void SetUpTradeRoute()
    {
        if (selectedTrader == null)
            return;

        world.cityBuilderManager.PlaySelectAudio();
        if (!uiTradeRouteManager.activeStatus)
        {
            //LoadUnloadFinish(true);
            infoManager.HideInfoPanel();
            uiTradeRouteManager.ToggleButtonColor(true);

            Vector3Int traderLoc = Vector3Int.RoundToInt(selectedTrader.transform.position);

            List<string> cityNames = world.GetConnectedCityNames(traderLoc, selectedTrader.bySea, true); //only showing city names accessible by unit
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
        if (!selectedTrader.hasRoute)
            return;
        
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
            if (selectedTrader.waitingOnGuard)
            {
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Waiting for guard to arrive");
				return;
            }
            
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

            if (selectedTrader.guarded)
                selectedTrader.guardUnit.military.ToggleIdleTimer(false);
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
                if (world.IsCityHarborOnTile(world.GetClosestTerrainLoc(selectedUnit.currentLocation)))
                    world.GetHarborCity(world.GetClosestTerrainLoc(selectedUnit.currentLocation)).tradersHere.Remove(selectedUnit);
            }
            else
            {
                if (world.IsCityOnTile(selectedUnit.currentLocation))
                    world.GetCity(world.GetClosestTerrainLoc(selectedUnit.currentLocation)).tradersHere.Remove(selectedUnit);
            }

			selectedUnit.StopMovement();

            if (selectedTrader.guarded)
            {
                selectedTrader.guardUnit.StopMovement();
            }

            selectedTrader.BeginNextStepInRoute();
            uiTraderPanel.SwitchRouteIcons(true);
            //LoadUnloadFinish(true);
            //uiTraderPanel.uiLoadUnload.ToggleInteractable(false);
            uiTradeRouteManager.ToggleVisibility(false);
        }
    }

    public void CancelTradeRoute() //stop following route but still keep route description
    {
        if (selectedTrader.ambush)
        {
			UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Too late to cancel...", false);
			return;
        }
        
        selectedTrader.RefundRouteCosts();
        selectedTrader.followingRoute = false; //done earlier as it's in stopmovement
        selectedTrader.waitingOnRouteCosts = false;
        selectedUnit.StopMovement();
        selectedTrader.CancelRoute();
        ShowIndividualCityButtonsUI();
		//CancelContinuedMovementOrders();
		selectedUnit.ResetMovementOrders();
		selectedUnit.HidePath();

		if (selectedTrader.guarded)
        {
            selectedTrader.guardUnit.StopMovement();
            selectedTrader.guardUnit.military.ToggleIdleTimer(true);
        }

        if (!selectedTrader.followingRoute/*.interruptedRoute*/)
            uiTraderPanel.SwitchRouteIcons(false);
        
        if (uiTradeRouteManager.activeStatus)
        {
            uiTradeRouteManager.ResetTradeRouteInfo(selectedTrader.tradeRouteManager);
            uiTradeRouteManager.ResetButtons();
        }
    }

    public void UninterruptedRoute()
    {
        //selectedTrader.interruptedRoute = false;
    }

    public void ShowIndividualCityButtonsUI()
    {
        if (selectedUnit == null)
            return;

		Vector3Int currentLoc = world.GetClosestTerrainLoc(selectedUnit.transform.position);

		if (selectedUnit.isPlayer)
        {
			if (selectedUnit.moreToMove)
            {
				if (!selectedUnit.runningAway)
                {
                    //uiCancelMove.ToggleVisibility(true);
				    selectedUnit.ShowContinuedPath();
                }
			}
   //         else
   //         {
			//	uiCancelMove.ToggleVisibility(false);
			//}
		}
        else if (selectedUnit.trader)
        {
			if (selectedUnit.moreToMove)
			{
				//uiCancelMove.ToggleVisibility(true);
				selectedUnit.ShowContinuedPath();
			}
            else
            {
				//uiCancelMove.ToggleVisibility(false);

                if (!selectedUnit.trader.followingRoute)
                {
					if (selectedUnit.bySea)
                    {
                        if (world.IsCityHarborOnTile(currentLoc))
							uiJoinCity.ToggleVisibility(true);
						else
							uiJoinCity.ToggleVisibility(false);
					}
                    else
                    {
                        if (world.IsCityOnTile(currentLoc))
    					    uiJoinCity.ToggleVisibility(true);
                        else
						    uiJoinCity.ToggleVisibility(false);
                    }
				}
			}
		}
        else if (selectedUnit.inArmy)
        {
			if (selectedUnit.military.atHome)
			{
				if (!selectedUnit.military.homeBase.army.defending && !selectedUnit.military.homeBase.army.returning)
				{
					uiJoinCity.ToggleVisibility(true);
					uiSwapPosition.ToggleVisibility(true);
					uiDeployArmy.ToggleVisibility(true);
					uiChangeCity.ToggleVisibility(true);
					uiAssignGuard.ToggleVisibility(true);
				}
			}
			else if (selectedUnit.military.guard)
			{
				if (!selectedUnit.military.guardedTrader.followingRoute)
				{
					uiChangeCity.ToggleVisibility(true);
					uiAssignGuard.ToggleVisibility(true);
				}
			}
            else if (selectedUnit.military.homeBase.army.traveling)
            {
                if (selectedUnit.military.homeBase.army.targetCamp.fieldBattleLoc == selectedUnit.military.homeBase.army.targetCamp.cityLoc)
					uiCancelTask.ToggleVisibility(true);
            }
            else if (selectedUnit.military.inBattle)
            {
				if (!selectedUnit.military.homeBase.army.defending && selectedUnit.military.homeBase.army.targetCamp.fieldBattleLoc == selectedUnit.military.homeBase.army.targetCamp.cityLoc)
					uiCancelTask.ToggleVisibility(true);
			}
			else if (selectedUnit.military.transferring)
			{
				uiChangeCity.ToggleVisibility(true);
				uiAssignGuard.ToggleVisibility(true);
			}
		}
        else if (selectedUnit.isLaborer)
        {
			selectedUnit.ShowContinuedPath();
   //         if (selectedUnit.moreToMove)
   //         {
			//	uiCancelMove.ToggleVisibility(true);
			//}
   //         else
   //         {
			//	uiCancelMove.ToggleVisibility(false);

			//	if (world.IsCityOnTile(currentLoc))
			//		uiJoinCity.ToggleVisibility(true);

			//	if (world.IsWonderOnTile(currentLoc))
			//	{
			//		if (world.GetWonder(currentLoc).StillNeedsWorkers())
			//			uiJoinCity.ToggleVisibility(true);
			//	}
			//}
        }

		//if (selectedUnit.moreToMove && !selectedUnit.inArmy && !selectedUnit.runningAway)
		//{
		//	uiCancelMove.ToggleVisibility(true);
		//	selectedUnit.ShowContinuedPath();
		//}
		//else if (selectedUnit.inArmy)
		//{
		//	if (selectedUnit.transferring)
		//	{
		//		uiChangeCity.ToggleVisibility(true);
		//		uiAssignGuard.ToggleVisibility(true);
		//	}
		//	else if (selectedUnit.guard)
		//	{
		//		if (!selectedUnit.guardedTrader.followingRoute)
		//		{
		//			uiChangeCity.ToggleVisibility(true);
		//			uiAssignGuard.ToggleVisibility(true);
		//		}
		//	}
		//	else if (selectedUnit.homeBase.army.traveling)
		//	{
		//		uiCancelTask.ToggleVisibility(true);
		//	}
		//	else if (selectedUnit.inBattle)
		//	{
		//		uiCancelTask.ToggleVisibility(true);
		//	}
		//}

		//if (!selectedUnit.moreToMove)
  //      {
  //          uiCancelMove.ToggleVisibility(false);
  //      }

        
   //     if (!selectedUnit.followingRoute && !selectedUnit.isMoving)
   //     {
   ////         if (world.IsCityOnTile(currentLoc) && !selectedUnit.isPlayer && !selectedUnit.inArmy)
   ////         {
   ////             uiJoinCity.ToggleVisibility(true);
   ////         }

   ////         if (selectedUnit.bySea && world.IsCityHarborOnTile(currentLoc))
   ////         {
			////	uiJoinCity.ToggleVisibility(true);
			////}
            
   ////         if (selectedTrader != null && world.IsTradeLocOnTile(currentLoc))
   ////         {
   ////             uiTraderPanel.uiLoadUnload.ToggleInteractable(true);
   ////         }
   ////         else if (selectedUnit.isLaborer && world.IsWonderOnTile(currentLoc))
   ////         {
   ////             if (world.GetWonder(currentLoc).StillNeedsWorkers())
   ////                 uiJoinCity.ToggleVisibility(true);
   ////         }
   //         if (selectedUnit.inArmy)
   //         {
   //             if (selectedUnit.atHome)
   //             {
			//		if (!selectedUnit.homeBase.army.defending && !selectedUnit.homeBase.army.returning)
   //                 {
			//			uiJoinCity.ToggleVisibility(true);
   //                     uiSwapPosition.ToggleVisibility(true);
   //                     uiDeployArmy.ToggleVisibility(true);
   //                     uiChangeCity.ToggleVisibility(true);
			//		    uiAssignGuard.ToggleVisibility(true);
   //                 }
			//	}
   //             else if (selectedUnit.transferring)
   //             {
   //                 uiChangeCity.ToggleVisibility(true);
			//		uiAssignGuard.ToggleVisibility(true);
			//	}
   //             else if (selectedUnit.guard)
   //             {
   //                 if (!selectedUnit.guardedTrader.followingRoute)
   //                 {
			//			uiChangeCity.ToggleVisibility(true);
			//			uiAssignGuard.ToggleVisibility(true);
			//		}
			//	}
   //             else if (!selectedUnit.homeBase.army.defending && !selectedUnit.repositioning)
   //             {
   //                 uiCancelTask.ToggleVisibility(true);
   //             }
			//}
   //     }
   //     else
   //     {
   //         //uiJoinCity.ToggleVisibility(false);
   //         //uiTraderPanel.uiLoadUnload.ToggleInteractable(false);
   //     }
    }


    public void TurnOnInfoScreen()
    {
        if (selectedTrader != null)
        {
            infoManager.ShowInfoPanel(selectedUnit.name, selectedUnit.buildDataSO, selectedUnit.currentHealth, selectedUnit.isTrader/*, selectedUnit.isLaborer*/);
        }
    }

    public void ResetArmyHomeButtons()
    {
		uiJoinCity.ToggleVisibility(false);
		uiSwapPosition.ToggleVisibility(false);
		uiDeployArmy.ToggleVisibility(false);
		uiChangeCity.ToggleVisibility(false);
		uiAssignGuard.ToggleVisibility(false);
	}

    public void ChangeHomeBase()
    {
        world.cityBuilderManager.PlaySelectAudio();
        uiJoinCity.ToggleVisibility(false);
		uiSwapPosition.ToggleVisibility(false);
		uiDeployArmy.ToggleVisibility(false);
		uiChangeCity.ToggleVisibility(false);
        uiAssignGuard.ToggleVisibility(false);
		world.HighlightCitiesWithBarracks(selectedUnit.military.homeBase);
        uiBuildingSomething.ToggleVisibility(true);
        uiBuildingSomething.SetText("Changing Home Base");
		uiCancelTask.ToggleVisibility(true);
		world.unitOrders = true;
        changingCity = true;
    }

    public void AssignGuard()
    {
		world.cityBuilderManager.PlaySelectAudio();
		uiJoinCity.ToggleVisibility(false);
		uiSwapPosition.ToggleVisibility(false);
		uiDeployArmy.ToggleVisibility(false);
		uiChangeCity.ToggleVisibility(false);
        uiAssignGuard.ToggleVisibility(false);
		world.HighlightTraders(selectedUnit.bySea);
		uiBuildingSomething.ToggleVisibility(true);
		uiBuildingSomething.SetText("Assigning Guard");
		uiCancelTask.ToggleVisibility(true);
		world.unitOrders = true;
		assigningGuard = true;
	}

	public void DeployArmyLocation()
    {
        world.cityBuilderManager.PlaySelectAudio();
        City homeBase = selectedUnit.military.homeBase;
        
        if (homeBase.army.isTraining)
        {
            Vector3 mousePosition = uiDeployArmy.transform.position;
            mousePosition.x -= 120;
            UIInfoPopUpHandler.WarningMessage().Create(mousePosition, "Still training", false);
			return;
        }
        else if (homeBase.army.isTransferring)
        {
			Vector3 mousePosition = uiDeployArmy.transform.position;
			mousePosition.x -= 150;
			UIInfoPopUpHandler.WarningMessage().Create(mousePosition, "Still transferring", false);
            return;
		}
        else if (homeBase.army.isRepositioning)
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
		uiAssignGuard.ToggleVisibility(false);
		uiBuildingSomething.ToggleVisibility(true);
		uiBuildingSomething.SetText("Deploying Army");
        uiCancelTask.ToggleVisibility(true);
		world.unitOrders = true;
        attackMovingTarget = false;
        deployingArmy = true;

        if (homeBase.attacked)
            world.HighlightAttackingCity(homeBase.cityLoc);
        else
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
        City homeBase = selectedUnit.military.homeBase;

        if (selectedUnit == null)
            return;

        if (selectedUnit.military.guard)
        {
            world.unitOrders = false;
            changingCity = false;
            assigningGuard = false;
			uiBuildingSomething.ToggleVisibility(false);
			world.UnhighlightTraders();
            ShowIndividualCityButtonsUI();

            if (selectedUnit.isMoving)
            {
				uiChangeCity.ToggleVisibility(true);
				uiAssignGuard.ToggleVisibility(true);
			}

			return;
        }

        if (homeBase.army.traveling)
        {
            GameLoader.Instance.gameData.attackedEnemyBases.Remove(homeBase.army.enemyTarget);
            homeBase.army.MoveArmyHome(homeBase.barracksLocation);
            world.EnemyCampReturn(homeBase.army.enemyTarget);
            HideBattlePath();
        }
        else if (homeBase.army.inBattle)
        {
            homeBase.army.Retreat();
		}
        else if (homeBase.army.atHome)
        {
            if (changingCity)
                world.UnhighlightCitiesWithBarracks();
            else if (deployingArmy)
                world.UnhighlightAllEnemyCamps();
            else if (assigningGuard)
                world.UnhighlightTraders();

            if (selectedUnit.military.atHome)
            {
                uiJoinCity.ToggleVisibility(true);
		        uiSwapPosition.ToggleVisibility(true);
                uiDeployArmy.ToggleVisibility(true);
			    uiChangeCity.ToggleVisibility(true);
			    uiAssignGuard.ToggleVisibility(true);
            }
            else if (!homeBase.army.defending && !selectedUnit.military.repositioning)
            {
                uiCancelTask.ToggleVisibility(true);
            }

			uiBuildingSomething.ToggleVisibility(false);
            world.unitOrders = false;
            deployingArmy = false;
            changingCity = false;
            assigningGuard = false;
        }
    }

    public void DeployArmy()
    {
        if (selectedUnit == null)
            return;

		if (world.uiCampTooltip.cantAfford)
        {
			StartCoroutine(world.uiCampTooltip.Shake());
			UIInfoPopUpHandler.WarningMessage().Create(world.uiCampTooltip.attackButton.transform.position, "Can't afford", false);
			return;
        }

        if (!attackMovingTarget && world.IsEnemyCityOnTile(potentialAttackLoc) && world.GetEnemyCity(potentialAttackLoc).enemyCamp.movingOut)
        {
			StartCoroutine(world.uiCampTooltip.Shake());
			UIInfoPopUpHandler.WarningMessage().Create(world.uiCampTooltip.attackButton.transform.position, "Can't attack now, enemy deployed", false);
			return;
        }

        world.uiCampTooltip.ToggleVisibility(false, null, null, null, false);
        uiBuildingSomething.ToggleVisibility(false);
		world.UnhighlightAllEnemyCamps();
		world.unitOrders = false;
		deployingArmy = false;
        world.cityBuilderManager.PlayBoomAudio();
        City homeBase = selectedUnit.military.homeBase;

        if (world.IsEnemyCityOnTile(potentialAttackLoc))
        {
            City city = world.GetEnemyCity(potentialAttackLoc);
			if (city.enemyCamp.movingOut)
            {
				uiCancelTask.ToggleVisibility(false);
				homeBase.army.enemyCityLoc = city.cityLoc;
				city.enemyCamp.attacked = true;
                city.enemyCamp.attackingArmy = homeBase.army;
                city.enemyCamp.fieldBattleLoc = homeBase.army.enemyTarget;
            }
            else
            {
                world.SetEnemyCityAsAttacked(potentialAttackLoc, homeBase.army);
            }
            
            selectedUnit.military.homeBase.army.targetCamp = city.enemyCamp;
        }
        else
        {
		    world.SetEnemyCampAsAttacked(potentialAttackLoc, homeBase.army);
		    homeBase.army.targetCamp = world.GetEnemyCamp(potentialAttackLoc);
        }
        
        homeBase.army.DeployArmy();
	}

    public void HideBattlePath()
    {
        if (selectedUnit != null)
        {
            selectedUnit.military.homeBase.army.HidePath();
		    world.HighlightEnemyCamp(potentialAttackLoc, Color.red);
        }
	}

    public void CancelOrders()
    {
		if (world.buildingWonder)
			world.CloseBuildingSomethingPanel();
		else if (selectedUnit.inArmy)
            CancelArmyDeployment();
        else if (selectedUnit.trader)
            CancelTradeRoute();
    }

    public void ClearSelection()
    {
        //selectedTile = null;
        if (selectedUnit != null)
        {
            //world.somethingSelected = false;
            if (selectedUnit.isBusy && world.scott.IsOrderListMoreThanZero())
                ToggleOrderHighlights(false);

            if (moveUnit)
            {
                if (selectedUnit.isLaborer)
                    world.UnhighlightCitiesAndWonders();
                else if (selectedUnit.trader)
                    world.UnhighlightCitiesAndWondersAndTradeCenters(selectedUnit.bySea);
            }

			//SpeakingCheck();
			world.cityBuilderManager.uiTraderNamer.ToggleVisibility(false);
			moveUnit = false;
            uiMoveUnit.ToggleVisibility(false);
            //uiCancelMove.ToggleVisibility(false);
            uiJoinCity.ToggleVisibility(false);
            uiSwapPosition.ToggleVisibility(false);
            uiDeployArmy.ToggleVisibility(false);
            uiChangeCity.ToggleVisibility(false);
			uiAssignGuard.ToggleVisibility(false);

			if (selectedUnit.isPlayer)
            {
                world.scott.Unhighlight();
                world.azai.Unhighlight();
                uiCancelTask.ToggleVisibility(false);
                uiWorkerTask.ToggleVisibility(false, world);
                LoadUnloadFinish(false); //clear load cargo screen
				uiPersonalResourceInfoPanel.ToggleVisibility(false);
				//workerTaskManager.NullWorkerUnit();
			}
            else if (selectedTrader != null)
            {
                uiTraderPanel.uiBeginTradeRoute.ToggleInteractable(false);
                uiTraderPanel.SwitchRouteIcons(false);
                uiTraderPanel.ToggleVisibility(false, world);
                //uiCancelTradeRoute.ToggleTweenVisibility(false);
                uiTradeRouteManager.ToggleVisibility(false);
                uiPersonalResourceInfoPanel.ToggleVisibility(false);
                if (selectedTrader.guarded)
                    selectedTrader.guardUnit.Unhighlight();
                selectedTrader = null;
            }
            else if (selectedUnit.inArmy)
            {
				uiCancelTask.ToggleVisibility(false);

                if (selectedUnit.military.guard)
                    selectedUnit.military.guardedTrader.Unhighlight();
                else
                    selectedUnit.military.homeBase.army.UnselectArmy(selectedUnit);

                if (selectedUnit.military.homeBase != null && selectedUnit.military.homeBase.army.traveling)
                    selectedUnit.military.homeBase.army.HidePath();
                //CancelArmyDeployment();
            }
            //if (selectedUnit != null)
            //{
            selectedUnit.Unhighlight();
            selectedUnit.HidePath();
            //}
            infoManager.HideInfoPanel();
            //movementSystem.ClearPaths(); //necessary to queue movement orders
            //selectedUnitInfoProvider = null;
            selectedUnit = null;
            unitSelected = false;
        }
    }

    //private bool CheckIfTheSameUnitSelected(Unit unitReference)
    //{
    //    return selectedUnit == unitReference;
    //}
}
