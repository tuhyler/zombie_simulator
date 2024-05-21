using System.Collections.Generic;
using UnityEngine;

public class UnitMovement : MonoBehaviour
{
    [SerializeField]
    public MapWorld world;
    [HideInInspector]
    public InfoManager infoManager;
    [SerializeField]
    public WorkerTaskManager workerTaskManager;
	[SerializeField]
	private List<AttackBonusHandler> attackBonusText;
	[SerializeField]
    public UISingleConditionalButtonHandler uiJoinCity, uiMoveUnit, uiCancelTask, uiConfirmOrders, uiDeployArmy, /*uiSwapPosition, */uiChangeCity, uiUnload; 
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

    [SerializeField]
    private ParticleSystem starshine;

    private bool queueMovementOrders;
    [HideInInspector]
    public MovementSystem movementSystem;

    [HideInInspector]
    public Unit selectedUnit;

    private ResourceManager cityResourceManager;
    private TradeCenter tradeCenter;
    public int playerLoadIncrement = 1;

    //for deploying army
    private Vector3Int potentialAttackLoc;
    private HashSet<Vector3Int> attackZoneList = new();

    //for upgrading units
    [HideInInspector]
    public HashSet<Unit> highlightedUnitList = new();
    [HideInInspector]
	public bool upgradingUnit, loadScreenSet;

    private void Awake()
    {
        movementSystem = GetComponent<MovementSystem>();
        movementSystem.GrowObjectPools(this);
        infoManager = GetComponent<InfoManager>();
    }

    private void Start()
    {
        starshine = Instantiate(starshine, new Vector3(0, 0, 0), Quaternion.identity);
        starshine.transform.SetParent(world.objectPoolItemHolder, false);
        starshine.Pause();
    }

    public void HandleG()
    {
        if (selectedUnit != null)
        {
            if (selectedUnit.worker && (selectedUnit.worker.isBusy || selectedUnit.worker.runningAway))
                return;

            if (world.cityBuilderManager.uiCityNamer.activeStatus)
                return;

			if (selectedUnit.buildDataSO.inMilitary)
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
        if (selectedUnit != null && selectedUnit.buildDataSO.inMilitary && uiChangeCity.activeStatus && !world.cityBuilderManager.uiCityNamer.activeStatus)
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
        //if (uiSwapPosition.activeStatus && !world.cityBuilderManager.uiCityNamer.activeStatus)
        //    RepositionArmy();
    }

    public void CenterCamOnUnit()
    {
        if (selectedUnit != null)
            selectedUnit.CenterCamera();
    }

    public void CheckIndividualUnitHighlight(Unit unit, City city)
    {
        if (world.azaiFollow && world.azai == unit)
        {
			Vector3Int azaiTerrain = world.GetClosestTerrainLoc(world.azai.transform.position);
			if (city.cityLoc == azaiTerrain || world.cityBuilderManager.cityTiles.Contains(azaiTerrain) && world.azai.buildDataSO.unitLevel < world.GetUpgradeableObjectMaxLevel("Azai"))
			{
				highlightedUnitList.Add(world.azai);
				world.azai.SoftSelect(Color.green);
			}
		}
        else if (unit.trader)
        {
			if (unit.buildDataSO.unitLevel < world.GetUpgradeableObjectMaxLevel(unit.buildDataSO.unitType.ToString()))
            {
				highlightedUnitList.Add(unit);
				unit.SoftSelect(Color.green);
			}
        }
    }

	public void ToggleUnitHighlights(bool v, City city = null)
	{
        upgradingUnit = v;

		if (v)
		{
            if (world.azaiFollow && !world.azai.isMoving && !world.mainPlayer.isMoving)
            {
                Vector3Int azaiTerrain = world.GetClosestTerrainLoc(world.azai.transform.position);
                if ((city.cityLoc == azaiTerrain || city.cityLoc == world.GetClosestTerrainLoc(world.mainPlayer.transform.position)) && world.azai.buildDataSO.unitLevel < world.GetUpgradeableObjectMaxLevel("Azai"))
                {
                    highlightedUnitList.Add(world.azai);
                    world.azai.SoftSelect(Color.green);
                }
            }
            
            foreach (Trader unit in city.tradersHere)
            {
				if (unit.buildDataSO.unitLevel < world.GetUpgradeableObjectMaxLevel(unit.buildDataSO.unitType.ToString()))
                {
					highlightedUnitList.Add(unit);
					unit.SoftSelect(Color.green);
				}
			}

			//only upgrade when at home or not busy
			if (city.army.atHome && city.singleBuildDict.ContainsKey(SingleBuildType.Barracks) && !world.GetCityDevelopment(city.singleBuildDict[SingleBuildType.Barracks]).isTraining)
            {
			    foreach (Military unit in city.army.UnitsInArmy)
			    {
				    if (unit.buildDataSO.unitLevel < world.GetUpgradeableObjectMaxLevel(unit.buildDataSO.unitType.ToString()))
				    {
                        highlightedUnitList.Add(unit);
                        unit.SoftSelect(Color.green);
				    }
			    }
            } 

            foreach (Transport unit in world.transportList)
            {
                if (unit.passengerCount > 0)
                    continue;
                
                if (city.singleBuildDict.ContainsKey(SingleBuildType.Harbor) && unit.bySea && !world.GetCityDevelopment(city.singleBuildDict[SingleBuildType.Harbor]).isTraining)
                {
                    if (world.GetClosestTerrainLoc(unit.transform.position) == city.singleBuildDict[SingleBuildType.Harbor])
                    {
                        highlightedUnitList.Add(unit);
                        unit.SoftSelect(Color.green);
                    }
                }

                if (city.singleBuildDict.ContainsKey(SingleBuildType.Airport) && unit.byAir && !world.GetCityDevelopment(city.singleBuildDict[SingleBuildType.Airport]).isTraining)
                {
                    if (world.GetClosestTerrainLoc(unit.transform.position) == city.singleBuildDict[SingleBuildType.Airport])
                    {
                        highlightedUnitList.Add(unit);
                        unit.SoftSelect(Color.green);
                    }
                }
            }
		}
		else
		{
			foreach (Unit unit in highlightedUnitList)
			{
				if (!unit.isSelected)
                {
                    if (unit == world.azai && !world.mainPlayer.isSelected)
						unit.Unhighlight();
					else if (unit.buildDataSO.inMilitary && !unit.military.army.selected)
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

        //if nothing detected, nothing selected
        if (detectedObject == null)
        {
            selectedUnit = null;
            return;
        }

		location.y = 0;

        Vector3Int pos = world.GetClosestTerrainLoc(location);

        //if building road, can't select anything else
        if (world.unitOrders)
        {
            TerrainData td = world.GetTerrainDataAt(pos);
            if (!td.isDiscovered)
                return;

            if (world.buildingRoad)
            {
                if (world.IsRoadOnTerrain(pos) || world.IsBuildLocationTaken(pos))
                {
                    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Already something here");
                }
                else if (!td.canPlayerWalk)
                {
                    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't build here");
                }
                else if (td.terrainData.type == TerrainType.River && !world.bridgeResearched)
                {
                    if (td.straightRiver)
                        UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Research bridges in masonry first");
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
                            world.utilityCostDisplay.AddCost(world.scott.currentUtilityCost.bridgeCost, world.mainPlayer.personalResourceManager.resourceDict, true);
                        else
                            world.utilityCostDisplay.AddCost(world.scott.currentUtilityCost.utilityCost, world.mainPlayer.personalResourceManager.resourceDict, true);

                        world.cityBuilderManager.PlaySelectAudio();
                    }
                    else
                    {
                        if (!world.scott.IsOrderListMoreThanZero())
                            uiConfirmOrders.ToggleVisibility(false);

                        if (td.straightRiver)
							world.utilityCostDisplay.SubtractCost(world.scott.currentUtilityCost.bridgeCost, world.mainPlayer.personalResourceManager.resourceDict, true);
                        else
							world.utilityCostDisplay.SubtractCost(world.scott.currentUtilityCost.utilityCost, world.mainPlayer.personalResourceManager.resourceDict, true);

                        if (world.scott.OrderList.Count == 0)
                            world.utilityCostDisplay.HideUtilityCostDisplay();
                        else if (world.utilityCostDisplay.currentLoc == pos)
                            world.utilityCostDisplay.ShowUtilityCostDisplay(world.scott.OrderList[world.scott.OrderList.Count - 1]);
						td.DisableHighlight();
                        world.cityBuilderManager.PlaySelectAudio();
                    }
                }
            }
            else if (world.removing)
            {
                if (!world.IsRoadOnTerrain(pos))
                {
                    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "No road here");
                }
                else if (world.IsCityOnTile(pos) || world.IsWonderOnTile(pos) || world.IsTradeCenterOnTile(pos))
                {
                    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't remove this");
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

                        td.EnableHighlight(Color.red);

						world.utilityCostDisplay.ShowUtilityCostDisplay(pos);
						if (td.straightRiver)
							world.utilityCostDisplay.AddCost(world.scott.currentUtilityCost.bridgeCost, world.mainPlayer.personalResourceManager.resourceDict, false);
						else
							world.utilityCostDisplay.AddCost(world.scott.currentUtilityCost.utilityCost, world.mainPlayer.personalResourceManager.resourceDict, false);

                        if (world.utilityCostDisplay.inventoryCount > world.mainPlayer.personalResourceManager.resourceStorageLimit - world.mainPlayer.personalResourceManager.resourceStorageLevel)
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
                    }
                    else
                    {
                        if (!world.scott.IsOrderListMoreThanZero())
                            uiConfirmOrders.ToggleVisibility(false);

						if (td.straightRiver)
							world.utilityCostDisplay.SubtractCost(world.scott.currentUtilityCost.bridgeCost, world.mainPlayer.personalResourceManager.resourceDict, false);
						else
							world.utilityCostDisplay.SubtractCost(world.scott.currentUtilityCost.utilityCost, world.mainPlayer.personalResourceManager.resourceDict, false);

						td.DisableHighlight();
                        foreach (Road road in world.GetAllRoadsOnTile(pos))
                        {
                            if (road == null)
                                continue;
                            road.MeshFilter.gameObject.SetActive(false);
                            road.SelectionHighlight.DisableHighlight();
                        }

                        world.cityBuilderManager.PlaySelectAudio();
                    }
                }
            }
            //moving positions within barracks
            else if (world.swappingArmy)
            {
                Vector3Int loc = world.RoundToInt(location);
                City homeBase = selectedUnit.military.army.city;

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
                        if (unit.military)
                        {
                            swapping = true;
                            unit.military.barracksBunk = selectedUnit.currentLocation;

                            if (unit.military.atHome)
                            {
                                unit.finalDestinationLoc = selectedUnit.currentLocation;
                                unit.MoveThroughPath(GridSearch.MilitaryMove(world, loc, selectedUnit.currentLocation, unit.bySea));
                            }
                        }
                    }
                    else
                    {
                        Vector3 starLoc = loc;
                        starLoc.y += .1f;
                        starshine.transform.position = starLoc;
                        starshine.Play();

                        foreach (Military unit in homeBase.army.UnitsInArmy)
                        {
                            if (unit == selectedUnit)
                                continue;

                            if (unit.barracksBunk == loc)
                            {
                                swapping = true;
                                unit.barracksBunk = selectedUnit.currentLocation;

                                unit.StopMovementCheck(false);

                                unit.finalDestinationLoc = selectedUnit.currentLocation;
                                unit.MoveThroughPath(GridSearch.MilitaryMove(world, loc, selectedUnit.currentLocation, unit.bySea));

                                break;
                            }
                        }
                    }

                    if (!swapping)
                        homeBase.army.UpdateLocation(selectedUnit.currentLocation, loc);
                    selectedUnit.military.barracksBunk = loc;
                    selectedUnit.finalDestinationLoc = loc;
                    selectedUnit.MoveThroughPath(GridSearch.MilitaryMove(world, selectedUnit.currentLocation, loc, selectedUnit.bySea));

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
            else if (world.deployingArmy)
            {
                City homeBase = selectedUnit.military.army.city;

				if (detectedObject.TryGetComponent(out Military unit) && unit.enemyAI && unit.enemyCamp.movingOut)
                {
                    if (unit.leader)
                        return;
                    
                    if (unit.preparingToMoveOut)
                        return;

                    if (unit.enemyCamp.attacked || unit.enemyCamp.inBattle || unit.enemyCamp.attackReady)
                    {
						UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Already attacking");
						return;
                    }

                    if (world.IsCityOnTile(unit.enemyCamp.moveToLoc))
					{
                        if (unit.enemyCamp.atSea)
                        {
							UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Currently at sea");
						}
                        else if (homeBase.attacked && unit.enemyCamp.moveToLoc != homeBase.cityLoc)
                        {
						    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Home currently being attacked by another army");
                        }
                        else if (world.GetCity(unit.enemyCamp.moveToLoc).army.defending)
                        {
							UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Enemy too close to target to get to in time");
						}
                        else 
                        {
							if (homeBase.army.DeployArmyMovingTargetCheck(world.GetClosestTerrainLoc(selectedUnit.currentLocation), unit.enemyCamp.threatLoc, homeBase.cityLoc, unit.enemyCamp.cityLoc, unit.enemyCamp.pathToTarget, unit.enemyCamp.lastSpot))
                            {
							    for (int i = 0; i < unit.enemyCamp.UnitsInCamp.Count; i++)
                                    unit.enemyCamp.UnitsInCamp[i].SoftSelect(Color.white);

                                SetMovingAttackBonusText(homeBase.army.pathToTarget[homeBase.army.pathToTarget.Count - 2],homeBase.army.enemyTarget);

                                world.attackMovingTarget = true;
								homeBase.army.ShowBattlePath();
								potentialAttackLoc = unit.enemyCamp.cityLoc;
								world.infoPopUpCanvas.gameObject.SetActive(true);
								world.uiCampTooltip.ToggleVisibility(true, null, unit.enemyCamp, homeBase.army, true);
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
						UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Already attacking");
						return;
					}

                    bool isCity = false;
                    if (world.IsEnemyCityOnTile(pos))
                        isCity = true;

                    if (isCity)
                    {
                        City enemyCity = world.GetEnemyCity(pos);
                        if (enemyCity.enemyCamp.movingOut)
                        {
                            return;
                        }
                        else if (enemyCity.empire.capitalCity == enemyCity.cityLoc && enemyCity.empire.enemyLeader.dueling)
                        {
							UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Currently in duel");
							return;
                        }
					} 

					//rehighlight in case selecting a different one
					if (world.IsEnemyCityOnTile(potentialAttackLoc))
						world.HighlightEnemyCity(potentialAttackLoc, Color.red);
                    else
						world.HighlightEnemyCamp(potentialAttackLoc, Color.red);

					uiBuildingSomething.SetText("Select Attack Zone");
					ShutDownAttackZones();
                    potentialAttackLoc = pos;
					SetUpAttackZoneInfo(pos, isCity);
				}
                else if (attackZoneList.Contains(pos))
                {
					if (world.IsEnemyCityOnTile(potentialAttackLoc) && pos == world.GetEnemyCity(potentialAttackLoc).singleBuildDict[SingleBuildType.Barracks])
                        return;
                    
                    world.attackMovingTarget = false;

                    foreach (Vector3Int zone in attackZoneList)
					{
                        if (world.IsEnemyCityOnTile(potentialAttackLoc) && zone == world.GetEnemyCity(potentialAttackLoc).singleBuildDict[SingleBuildType.Barracks])
                            continue;
                        
						Color color = pos == zone ? Color.white : Color.green;
						world.GetTerrainDataAt(zone).EnableHighlight(color);
                        if (world.CompletedImprovementCheck(zone))
                        {
                            world.GetCityDevelopment(zone).DisableHighlight();
                            world.GetCityDevelopment(zone).EnableHighlight(color);
                        }
					}

					homeBase.army.HidePath();
					if (homeBase.army.DeployArmyCheck(world.GetClosestTerrainLoc(selectedUnit.currentLocation), pos, potentialAttackLoc))
					{
						homeBase.army.ShowBattlePath();

						//rehighlight in case selecting a different one
						if (world.IsEnemyCityOnTile(potentialAttackLoc))
                        {
							Vector3Int barracksLoc = world.GetEnemyCity(potentialAttackLoc).singleBuildDict[SingleBuildType.Barracks];
							world.HighlightEnemyCity(potentialAttackLoc, Color.red);
							world.GetCityDevelopment(barracksLoc).DisableHighlight();
							world.GetCityDevelopment(barracksLoc).EnableHighlight(Color.red);
						}
                        else
                        {
							world.HighlightEnemyCamp(potentialAttackLoc, Color.red);
                        }

						world.infoPopUpCanvas.gameObject.SetActive(true);
						if (world.IsEnemyCityOnTile(potentialAttackLoc))
						{
                            Vector3Int barracksLoc = world.GetEnemyCity(potentialAttackLoc).singleBuildDict[SingleBuildType.Barracks];
							if (world.GetCloserTile(pos, potentialAttackLoc, barracksLoc) == potentialAttackLoc)
                            {
                                world.HighlightEnemyCity(potentialAttackLoc, Color.white);
                            }
                            else
                            {
								world.GetCityDevelopment(barracksLoc).DisableHighlight();
								world.GetCityDevelopment(barracksLoc).EnableHighlight(Color.white);
                            }
							world.uiCampTooltip.ToggleVisibility(true, null, world.GetEnemyCity(potentialAttackLoc).enemyCamp, homeBase.army);
						}
						else
						{
							world.HighlightEnemyCamp(potentialAttackLoc, Color.white);
							world.uiCampTooltip.ToggleVisibility(true, null, world.GetEnemyCamp(potentialAttackLoc), homeBase.army);
						}
					}
				}
                else
                {
					UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Select enemy camp");
				}
            }
            else if (world.assigningGuard)
            {
                if (detectedObject.TryGetComponent(out Military military) && military.buildDataSO.unitType == UnitType.Infantry)
                {
                    if (military.atHome && world.uiTradeRouteBeginTooltip.MilitaryLocCheck(world.GetClosestTerrainLoc(military.currentLocation)))
                    {
                        selectedUnit.trader.guarded = true;
                        selectedUnit.trader.guardUnit = military;
                        world.uiTradeRouteBeginTooltip.UnselectArmy();
                        world.uiTradeRouteBeginTooltip.ToggleAddGuard(false);
                        world.uiTradeRouteBeginTooltip.UpdateGuardCosts();

                        uiTraderPanel.gameObject.SetActive(true);
                        world.uiTradeRouteBeginTooltip.gameObject.SetActive(true);
						world.unitOrders = false;
						world.assigningGuard = false;
						uiBuildingSomething.ToggleVisibility(false);
						uiCancelTask.ToggleVisibility(false);
					}
                }
                else
                {
					UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Select infantry unit from same city");
					return;
				}
            }

            return;            
        }

        //closing all windows when selecting units
		world.CloseResearchTree();
		world.CloseConversationList();
		world.CloseWonders();
		world.CloseTerrainTooltip();
        world.CloseTransferTooltip();
		world.CloseImprovementTooltip();
		world.CloseCampTooltip();
		world.uiTradeRouteBeginTooltip.ToggleVisibility(false);

		//moving unit upon selection
		if (world.moveUnit && selectedUnit != null && (selectedUnit.isPlayer || selectedUnit.transport))
        {
            MoveUnit(location, detectedObject);
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

				SelectUnitPrep(unitReference);
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
    //    else if (detectedObject.TryGetComponent(out UnitMarker unitMarker))
    //    {
    //        Unit unit = unitMarker.unit;

    //        if (unitMarker.CompareTag("Player"))
    //        {
				//if (unit.isUpgrading)
				//{
				//	UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Currently being upgraded");
				//	return;
				//}

				//SelectUnitPrep(unit);
    //        }
    //        else if (unitMarker.CompareTag("Enemy"))
    //        {
    //            SelectEnemy(unit);
    //        }
    //        else if (unitMarker.CompareTag("Character"))
    //        {
    //            SelectCharacter(unitReference);
    //        }
    //    }
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
					PrepareMovement();
            }
        }
        else
        {
            ClearSelection();
        }
    }

    public void TransferMilitaryUnit(City newCity, bool bySea)
    {
        Military unit = selectedUnit.military;
        Vector3Int newLoc = newCity.singleBuildDict[selectedUnit.buildDataSO.singleBuildType];// army.GetAvailablePosition(selectedUnit.buildDataSO.unitType);
        List<Vector3Int> path;

        if (unit.isMoving)
            unit.StopMovementCheck(false);

        if (bySea)
        {
			Vector3Int terrainLoc = world.GetClosestTerrainLoc(selectedUnit.transform.position);
            Vector3Int beginningHarbor = unit.army.city.singleBuildDict[SingleBuildType.Harbor];
            Vector3Int endHarbor = newCity.singleBuildDict[SingleBuildType.Harbor];

			path = GridSearch.TerrainSearch(world, terrainLoc, beginningHarbor, true);
			if (path.Count == 0)
				path = GridSearch.MoveWherever(world, selectedUnit.transform.position, beginningHarbor);

			List<Vector3Int> seaPath = GridSearch.TraderMove(world, beginningHarbor, endHarbor, true);

			List<Vector3Int> endPath = GridSearch.TerrainSearch(world, endHarbor, world.GetClosestTerrainLoc(newLoc));
			if (endPath.Count == 0)
				endPath = GridSearch.MoveWherever(world, endHarbor, newLoc);
			else
				endPath[endPath.Count - 1] = newLoc;

			path.AddRange(seaPath);
			path.AddRange(endPath);

			unit.switchLocs.Add(beginningHarbor);
            unit.switchLocs.Add(endHarbor);
        }
        else
        {
			path = GridSearch.MilitaryMove(world, unit.transform.position, newLoc, selectedUnit.bySea);
		}

		unit.army.RemoveFromArmy(unit, unit.barracksBunk, true);
		unit.army = null;
		unit.atHome = false;
		//newCity.army.AddToArmy(unit);
		unit.barracksBunk = new Vector3Int(0, 0, 1);
		unit.transferring = true;
		//unit.army.isTransferring = true;

		unit.finalDestinationLoc = newLoc;
		unit.MoveThroughPath(path);
		
        if (unit.isSelected)
        {
		    //uiChangeCity.ToggleVisibility(true);
		    //uiCancelTask.ToggleVisibility(false);
        }
	}

    public void SelectUnitPrep(Unit unitReference)
    {
        if (upgradingUnit && highlightedUnitList.Contains(unitReference))
        {
            world.cityBuilderManager.UpgradeUnitWindow(unitReference);
            return;
        }
        
        if (selectedUnit == unitReference) //Unselect when clicking same unit
        {
            if (selectedUnit != null)
            {
                if (selectedUnit.worker && selectedUnit.worker.harvested)
                {
					selectedUnit.worker.SendResourceToCity();
					return;
				}
                else if (selectedUnit.trader)
                {
                    Trader nextTrader = world.CycleThroughTraderCheck(world.RoundToInt(selectedUnit.transform.position), selectedUnit.trader);
                    ClearSelection();

                    if (nextTrader)
                        selectedUnit = nextTrader;
                    else
                        return;
                }
                else
                {
                    ClearSelection();
                    return;
                }
			}
            else
            {
                selectedUnit = unitReference;
            }
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
			selectedUnit.SpeakingCheck();
        }
        else
        {
            SelectWorker();
            SelectTrader();
            PrepareMovement();
        }
    }

    //for selecting worker for speaking when already selected
    public void QuickSelect(Unit unitReference)
    {
		ClearSelection();
		selectedUnit = unitReference;
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

        selectedUnit.SayHello();
        world.tooltip = false;
		world.somethingSelected = true;
		selectedUnit.Highlight(Color.red);
        int bonus = selectedUnit.military ? selectedUnit.military.strengthBonus : 0;
        bool leader = selectedUnit.military && selectedUnit.military.leader;
		infoManager.ShowInfoPanel(selectedUnit.name, selectedUnit.buildDataSO, selectedUnit.currentHealth, selectedUnit.trader, bonus, leader);
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
        world.tooltip = false;
		world.somethingSelected = true;
		selectedUnit.Highlight(new Color(.5f, 0, 1));
		infoManager.ShowInfoPanel(selectedUnit.name, selectedUnit.buildDataSO, selectedUnit.currentHealth, selectedUnit.trader, 0, false);
	}

	public void SelectWorker()
    {        
		if (world.characterUnits.Contains(selectedUnit))
        {
			if (selectedUnit.military && selectedUnit.military.bodyGuard.dueling)
			{
				SelectBodyGuard();
			}
            else
            {
			    selectedUnit = world.mainPlayer;

			    uiPersonalResourceInfoPanel.SetTitleInfo(world.mainPlayer.name, world.mainPlayer.personalResourceManager.resourceStorageLevel, world.mainPlayer.personalResourceManager.resourceStorageLimit);
			    uiPersonalResourceInfoPanel.ToggleVisibility(true, world.mainPlayer);

			    if (world.mainPlayer.isBusy)
				    uiCancelTask.ToggleVisibility(true);
			    if (!selectedUnit.sayingSomething)
                    uiWorkerTask.ToggleVisibility(true, world);
                if (world.scott.IsOrderListMoreThanZero())
                    ToggleOrderHighlights(true);

                if (selectedUnit.worker.harvested) //if unit just finished harvesting something, send to closest city
                    selectedUnit.worker.SendResourceToCity();

                Vector3Int terrainLoc = world.GetClosestTerrainLoc(selectedUnit.transform.position);
                if (world.IsCityOnTile(terrainLoc) || world.IsTradeCenterOnTile(terrainLoc))
                    uiWorkerTask.uiLoadUnload.ToggleInteractable(true);
                else
                    uiWorkerTask.uiLoadUnload.ToggleInteractable(false);

                for (int i = 0; i < world.characterUnits.Count; i++)
                {
				    if (world.characterUnits[i] == selectedUnit)
					    world.characterUnits[i].Highlight(Color.green);
				    else
					    world.characterUnits[i].SoftSelect(Color.green);
			    }

                if (selectedUnit.worker.inEnemyLines && !selectedUnit.isMoving)
                {
				    foreach (Vector3Int tile in world.GetNeighborsFor(selectedUnit.currentLocation, MapWorld.State.EIGHTWAY))
                    {
                        if (world.IsNPCThere(tile) && world.GetNPC(tile).somethingToSay)
                        {                        
                            world.unitMovement.QuickSelect(selectedUnit);
				            world.GetNPC(tile).SpeakingCheck();
    				        world.uiSpeechWindow.SetSpeakingNPC(world.GetNPC(tile));
                            break;
                        }
                    }
			    }
            }
        }
    }

    private void SelectTrader()
    {
        if (selectedUnit.trader)
        {
            if (selectedUnit.trader.interruptedRoute)
                selectedUnit.trader.InterruptedRouteMessage();
            if (selectedUnit.trader.tradeRouteManager.resourceCheck || selectedUnit.trader.waitingOnRouteCosts)
                selectedUnit.trader.CheckWarning();
            uiPersonalResourceInfoPanel.SetTitleInfo(selectedUnit.trader.name, selectedUnit.trader.personalResourceManager.resourceStorageLevel, selectedUnit.trader.personalResourceManager.resourceStorageLimit);
            uiPersonalResourceInfoPanel.ToggleVisibility(true, selectedUnit.trader);
            world.traderCanvas.gameObject.SetActive(true);
            world.personalResourceCanvas.gameObject.SetActive(true);
            uiTraderPanel.ToggleVisibility(true);

            if (selectedUnit.trader.hasRoute)
            {
                uiTraderPanel.uiBeginTradeRoute.ToggleInteractable(true);

                if (selectedUnit.trader.followingRoute)
                    uiTraderPanel.SwitchRouteIcons(true);
            }
        }
    }

    private void SelectBodyGuard()
    {
		selectedUnit.SayHello();
        world.tooltip = false;
		world.somethingSelected = true;
		selectedUnit.Highlight(Color.green);
		infoManager.ShowInfoPanel(selectedUnit.name, selectedUnit.buildDataSO, selectedUnit.currentHealth, false, selectedUnit.military.strengthBonus, false);
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

        SelectWorker();
        SelectTrader();
        PrepareMovement();
    }

    public void PrepareMovement()
    {
        selectedUnit.SayHello();

        world.tooltip = false;
        world.somethingSelected = true;
        if (selectedUnit.buildDataSO.inMilitary)
        {
            if (selectedUnit.military.guard)
            {
                selectedUnit.military.guardedTrader.SoftSelect(Color.white);
                selectedUnit.Highlight(Color.green);
            }
            else if (selectedUnit.military.army != null)
            {
    			selectedUnit.military.army.SelectArmy(selectedUnit.military);
            }
            else
            {
                selectedUnit.Highlight(Color.green);
            }
        }
        else if (selectedUnit.trader && selectedUnit.trader.guarded && (selectedUnit.bySea || selectedUnit.byAir))
        {
			selectedUnit.Highlight(Color.green);
			selectedUnit.trader.guardUnit.SoftSelect(Color.white);
		}
		else if (!world.characterUnits.Contains(selectedUnit)) //selection handled elsewhere
        {
			selectedUnit.Highlight(Color.green);
        }

        if (selectedUnit.isPlayer)
        {
			if (world.azaiFollow)
				infoManager.ShowInfoPanel(selectedUnit.name, world.azai.buildDataSO, world.azai.currentHealth, false, world.azai.military.strengthBonus, false);
            else
                infoManager.ShowInfoPanel(selectedUnit.name, selectedUnit.buildDataSO, selectedUnit.currentHealth, false, 0, false);
		}
        else
        {
    		int bonus = selectedUnit.military ? selectedUnit.military.strengthBonus : 0;
    		infoManager.ShowInfoPanel(selectedUnit.name, selectedUnit.buildDataSO, selectedUnit.currentHealth, selectedUnit.trader, bonus, false);
        }

        ShowIndividualCityButtonsUI();
    }

    public void HandleSelectedLocation(Vector3 location, Vector3Int terrainPos, Unit unit, bool moveToSpeak)
    {
        if (queueMovementOrders && unit.isMoving)
        {
            if (unit.finalDestinationLoc == location)
                return;
            
            movementSystem.AppendNewPath(unit);
        }
        else if (unit.isMoving)
        {
            unit.pathPositions.Clear();
        }

        Vector3Int startPosition = world.RoundToInt(unit.transform.position);
        movementSystem.GetPathToMove(world, unit, startPosition, terrainPos, moveToSpeak); //Call AStar movement

        unit.finalDestinationLoc = location;

        if (!queueMovementOrders)
        {
            world.moveUnit = false;
            uiMoveUnit.ToggleButtonColor(false);

            if (movementSystem.currentPath.Count > 0)
            {
                bool updateFollowers = false;
                if (unit.isPlayer)
                {
                    if (unit.isMoving && !unit.worker.firstStep)
                        updateFollowers = true;
                    else
                        unit.worker.firstStep = true;
				}

				unit.MoveThroughPath(movementSystem.currentPath);

                if (updateFollowers)
                    unit.worker.CheckToFollow(startPosition);
			}
			else
			{
				return;
			}
        }

        movementSystem.ClearPaths();

        if (unit.transport)
        {
            uiJoinCity.ToggleVisibility(false);
            uiUnload.ToggleVisibility(false);
        }
        else
        {
            uiWorkerTask.uiLoadUnload.ToggleInteractable(false);
        }
    }

	public void GoStraightToSelectedLocation(Vector3Int location, Vector3Int terrainPos, Unit unit)
	{
		unit.StopMovementCheck(false);

        if (unit.worker.building)
        {
		    if (!world.IsTileOpenCheck(terrainPos) || !world.PlayerCheckIfPositionIsValid(terrainPos))
		    {
			    unit.worker.SkipRoadBuild();
                if (world.mainPlayer.isSelected)
                    world.GetTerrainDataAt(terrainPos).DisableHighlight();
			    return;
		    }
        }

		List<Vector3Int> path = GridSearch.MilitaryMove(world, unit.transform.position, location, false);

        if (path.Count > 0)
        {
    		unit.finalDestinationLoc = location;
            unit.MoveThroughPath(path);
        }
        else
        {
            unit.FinishMoving(unit.transform.position);
            return;
        }

        if (unit.isPlayer)
        {
            if (world.scottFollow && !world.mainPlayer.isBusy)
            {
                world.scott.GoToPosition(terrainPos, true);
            }
            
            if (unit.isSelected)
            {
		        uiJoinCity.ToggleVisibility(false);
		        uiWorkerTask.uiLoadUnload.ToggleInteractable(false);
            }
		}
        else if (unit == world.scott)
        {
            if (world.azaiFollow)
            {
                world.azai.FollowScott(path, unit.transform.position);
            }
        }
	}

    public void MoveUnitRightClick(Vector3 location, GameObject detectedObject)
    {
        if (selectedUnit != null && (selectedUnit.isPlayer || selectedUnit.transport))
            MoveUnit(location, detectedObject);
    }

    private void MoveUnit(Vector3 location, GameObject detectedObject)
    {
		if (detectedObject == null)
            return;

		if (selectedUnit.isPlayer && (world.mainPlayer.runningAway || world.mainPlayer.isBusy))
		{
			UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't move right now");
			return;
		}

        Vector3 originalLoc = location;
        Vector3Int locationInt = world.RoundToInt(location);

        if (!world.GetTerrainDataAt(locationInt).isDiscovered)
            return;

        TerrainData td = world.GetTerrainDataAt(locationInt);

        bool moveToSpeak = false;
        bool moveToTransport = false;
        if (selectedUnit.isPlayer)
        {
			if (td.hasBattle)
            {
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Battle is here");
				return;
            }
            
            if (selectedUnit.worker.inTransport)
                return;

            if (selectedUnit.worker.inEnemyLines)
                return;
            
            if (selectedUnit.worker.harvested) //if unit just finished harvesting something, send to closest city
                selectedUnit.worker.SendResourceToCity();
       
            LoadUnloadFinish(true);
            GivingFinish(true);

            if (world.CheckIfEnemyTerritory(locationInt))
            {
			    if (world.IsNPCThere(locationInt) && world.GetNPC(locationInt).somethingToSay && !world.GetEnemyCity(locationInt).enemyCamp.attacked)
			    {
				    moveToSpeak = true;
			    }
			    else
			    {
				    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Enemy territory");
				    return;
			    }
            }

			if (detectedObject.TryGetComponent(out Transport transport))
            {
                selectedUnit.worker.toTransport = true;
                selectedUnit.worker.transportTarget = transport;

                if (transport.bySea && !world.GetTerrainDataAt(locationInt).canWalk)
                {
                    locationInt = world.GetAdjacentMoveToTile(world.RoundToInt(selectedUnit.transform.position), locationInt, false);
				    //locationInt = trySpot;
                    location = locationInt;
                    moveToTransport = true;
                }
			}
            else if (selectedUnit.worker.transportTarget != null)
            {
                selectedUnit.worker.toTransport = false;
                selectedUnit.worker.transportTarget = null;
            }
            
            if (detectedObject.TryGetComponent(out Unit npc) && npc.buildDataSO.npc)
            {
                locationInt = npc.currentLocation;
                location = npc.currentLocation;
            }
        }
        else if (selectedUnit.transport)
        {
            if (world.mainPlayer.runningAway)
            {
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't move right now");
				return;
            }

            if (!selectedUnit.transport.canMove)
            {
                UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Need all passengers to move");
			    return;
            }
            else if (world.GetTerrainDataAt(locationInt).border)
            {
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Not there, nothing out there");
				return;
			}

            if (world.CheckIfEnemyTerritory(locationInt) && !world.CheckIfNeutral(locationInt))
            {
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Enemy territory");
				return;
			}
        }

        if ((selectedUnit.bySea && !td.sailable) || (!moveToTransport && !selectedUnit.bySea && !td.canPlayerWalk))
        {
			UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Cannot travel to selected area");
			return;
		}

		if (selectedUnit.bySea)
        {
            if (!world.PlayerCheckIfSeaPositionIsValid(locationInt))
            {
                locationInt = world.GetClosestMoveToSpot(locationInt, selectedUnit.transform.position, true);
                //locationInt = trySpot;
                location = locationInt;
            }
        }
        else if (selectedUnit.byAir)
        {

        }
        else if (!world.PlayerCheckIfPositionIsValid(locationInt))
        {
			if (!world.PlayerCheckIfSeaPositionIsValid(locationInt)) //if unit is on land and is not trying to go out to sea
			{
				locationInt = world.GetClosestMoveToSpot(locationInt, selectedUnit.transform.position, false);
				//locationInt = trySpot;
                location = locationInt;
			}
		}

        world.tempNoWalkList.Clear();
        originalLoc.y += .1f;
        starshine.transform.position = originalLoc;
        starshine.Play();
		world.cityBuilderManager.MoveUnitAudio();

		if (selectedUnit.isMoving && !queueMovementOrders) //interrupt orders if new ones
        {
            selectedUnit.StopMovementCheck(false);
            selectedUnit.FinishedMoving.RemoveAllListeners();
        }

        HandleSelectedLocation(location, locationInt, selectedUnit, moveToSpeak);
    }

    public void MoveUnitToggle()
    {
        world.cityBuilderManager.PlaySelectAudio();
        
        if (!world.moveUnit)
        {
            world.moveUnit = true;
            world.citySelected = true;
        }
        else
        {
            world.moveUnit = false;
			world.citySelected = false;
		}

		uiMoveUnit.ToggleButtonColor(world.moveUnit);
    }

    public void CancelMove()
    {
        if (world.moveUnit)
        {
            world.moveUnit = false;
		    world.citySelected = false;

		    uiMoveUnit.ToggleButtonColor(world.moveUnit);
        }
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

    public void JoinCity() //for Join City button
    {
        world.cityBuilderManager.PlaySelectAudio();

        if (selectedUnit.isUpgrading)
        {
			InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(selectedUnit.transform.position, "Currently upgrading");
			return;
        }

        City city = null;

        if (selectedUnit.trader)
            city = world.GetCity(selectedUnit.trader.homeCity);
        else if (selectedUnit.buildDataSO.inMilitary)
            city = selectedUnit.military.army.city;

        if (city != null)
            world.uiCityPopIncreasePanel.ToggleVisibility(true, selectedUnit.buildDataSO.laborCost, city, true, selectedUnit.trader);
    }

    public void JoinCityConfirm(City city)
    {
        if (selectedUnit.buildDataSO.inMilitary)
            selectedUnit.military.army.RemoveFromArmy(selectedUnit.military, selectedUnit.military.barracksBunk, true);
        else if (selectedUnit.trader)
            world.GetCityDevelopment(city.singleBuildDict[selectedUnit.buildDataSO.singleBuildType]).RemoveTraderFromImprovement(selectedUnit.trader);

		AddToCity(city, selectedUnit);
	}

    public void LaborerJoin(Laborer unit)
    {
		Vector3Int unitLoc = world.GetClosestTerrainLoc(unit.transform.position);

        if (world.IsCityOnTile(unitLoc))
        {
        	world.laborerList.Remove(unit);
			AddToCity(world.GetCity(unitLoc), unit);
        }
        else if (world.StopExistsCheck(unitLoc))
        {
			ITradeStop stop = world.GetStop(unitLoc);

            if (stop.wonder)
            {
			    if (world.GetWonder(stop.mainLoc).StillNeedsWorkers())
			    {
				    stop.wonder.AddWorker(unit);
			    }
			    else
			    {
				    unit.GoToDestination(unit.homeCityLoc, true);
				    return;
			    }
            }
            else if (stop.city)
            {
				world.laborerList.Remove(unit);
				AddToCity(world.GetCity(unitLoc), unit);
			}
		}
        else
        {            
            if (unit.homeCityLoc != unitLoc && world.IsCityOnTile(unit.homeCityLoc))
            {
                if (!unit.atSea)
                {
    				unit.GoToDestination(unit.homeCityLoc, true);
                    return;
                }
                else if (world.GetCity(unit.homeCityLoc).singleBuildDict.ContainsKey(SingleBuildType.Harbor))
                {
                    unit.GoToDestination(world.GetCity(unit.homeCityLoc).singleBuildDict[SingleBuildType.Harbor], true);
                    return;
                }
            }

            unit.KillUnit(unit.transform.position - unit.prevTile);
            return;
		}

        if (unit.isSelected)
        {
            world.somethingSelected = false;
            ClearSelection();
        }
	}

    public void RepositionArmy()
    {
        world.cityBuilderManager.PlaySelectAudio();
        world.cameraController.CenterCameraNoFollow(world.GetClosestTerrainLoc(selectedUnit.currentLocation));
        //uiSwapPosition.ToggleVisibility(false);
        uiJoinCity.ToggleVisibility(false);
        uiDeployArmy.ToggleVisibility(false);
		uiChangeCity.ToggleVisibility(false);
		uiConfirmOrders.ToggleVisibility(true);
		uiBuildingSomething.ToggleVisibility(true);
        uiBuildingSomething.SetText("Repositioning Unit");
		world.unitOrders = true;
        world.swappingArmy = true;
        world.SetSelectionCircleLocation(selectedUnit.currentLocation);
    }

    public void CancelReposition()
    {
		if (selectedUnit.military.atHome)
        {
            //uiSwapPosition.ToggleVisibility(true);
		    uiJoinCity.ToggleVisibility(true);
		    uiDeployArmy.ToggleVisibility(true);
            uiChangeCity.ToggleVisibility(true);
        }
		else if (!selectedUnit.military.army.defending && !selectedUnit.military.repositioning)
		{
			uiCancelTask.ToggleVisibility(true);
		}

		uiConfirmOrders.ToggleVisibility(false);
		uiBuildingSomething.ToggleVisibility(false);
		world.unitOrders = false;
        world.swappingArmy = false;
        world.HideSelectionCircles();
    }

    public void AddToCity(City joinedCity, Unit unit)
    {
        bool joinCity = true;
        
        if (unit.trader)
        {
            unit.trader.UnloadAll(joinedCity);
            world.traderList.Remove(unit.trader);
            joinCity = false;
        }
        else if (unit.laborer)
        {
            world.laborerList.Remove(unit.laborer);
        }
        else if (unit.transport)
        {
            if (unit.bySea)
                world.waterTransport = false;
            else if (unit.byAir)
                world.airTransport = false;

            world.transportList.Remove(unit.transport);
            world.RemovePlayerPosition(unit.transport.currentLocation);
        }
        else if (unit.military)
        {
            world.RemoveUnitPosition(unit.currentLocation);
        }

        joinedCity.PopulationGrowthCheck(joinCity, unit.buildDataSO.laborCost);

		int i = 0;
        joinedCity.ResourceManager.resourceCount = 0;
		foreach (ResourceValue resourceValue in unit.buildDataSO.unitCost) //adding back 100% of cost (if there's room)
		{
			int resourcesGiven = joinedCity.ResourceManager.AddResource(resourceValue.resourceType, resourceValue.resourceAmount);
			Vector3 cityLoc = joinedCity.cityLoc;
			cityLoc.y += unit.buildDataSO.unitCost.Count * 0.4f;
			cityLoc.y += -0.4f * i;
			InfoResourcePopUpHandler.CreateResourceStat(cityLoc, resourcesGiven, ResourceHolder.Instance.GetIcon(resourceValue.resourceType), world);
			i++;
		}

        unit.DestroyUnit();
	}

    public void Unload()
    {
        if (selectedUnit.transport)
            selectedUnit.transport.Unload();
        else if (selectedUnit.trader)
            selectedUnit.trader.UnloadAll(world.GetCity(selectedUnit.trader.homeCity));

        uiUnload.ToggleVisibility(false);
    }

    public void LoadUnloadPrep() //for loadunload button for Koa
    {
		world.cityBuilderManager.PlaySelectAudio();

		if (!loadScreenSet)
        {
            uiWorkerTask.uiLoadUnload.ToggleColor(true);
            selectedUnit.HidePath();
            movementSystem.ClearPaths();
            Vector3Int playerLoc = world.GetClosestTerrainLoc(selectedUnit.transform.position);
            
            if (world.IsCityOnTile(playerLoc))
            {
                City city = world.GetCity(playerLoc);
                cityResourceManager = city.ResourceManager;
                uiCityResourceInfoPanel.SetTitleInfo(city.cityName,
                    cityResourceManager.resourceStorageLevel, city.warehouseStorageLimit);
				uiCityResourceInfoPanel.ToggleInventoryLevel(true);
				uiCityResourceInfoPanel.ToggleVisibility(true, null, city);
                uiCityResourceInfoPanel.SetPosition();
				uiPersonalResourceInfoPanel.SetPosition(false, null);
			}
            else
            {
                ITradeStop stop = world.GetStop(playerLoc);
                tradeCenter = stop.center;
                uiCityResourceInfoPanel.SetTitleInfo(stop.center.tradeCenterDisplayName, 10000, 10000); //not showing inventory levels
                uiCityResourceInfoPanel.ToggleInventoryLevel(false);
                uiCityResourceInfoPanel.ToggleVisibility(true, null, null, null, stop.center);
                uiCityResourceInfoPanel.SetPosition(true);
                uiPersonalResourceInfoPanel.SetPosition(true, stop.center);
            }
            
            loadScreenSet = true;
        }
        else
        {
            LoadUnloadFinish(true);
        }
    }

    public void ConfirmWorkerOrdersButton()
    {
        ConfirmWorkerOrders();
    }

    public void ConfirmWorkerOrders()
    {
        queueMovementOrders = false;
        
        if (world.unitOrders)
        {
			if (selectedUnit.worker && !world.utilityCostDisplay.hasEnough)
            {
                UIInfoPopUpHandler.WarningMessage().Create(world.scott.OrderList[world.scott.OrderList.Count - 1], "Need more supplies", false);
				return;
            }
            
            uiBuildingSomething.ToggleVisibility(false);

			if (world.swappingArmy)
            {
                CancelReposition();
                return;
            }
            
            ClearBuildRoad();
            if (world.buildingRoad)
            {
                world.scott.building = true;
                world.scott.SetRoadQueue();
            }
            else if (world.removing)
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
        world.buildingRoad = false;
        world.removing = false;
        world.removingAll = false;
        world.removingRoad = false;
        world.removingLiquid = false;
        world.removingPower = false;
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
            uiCancelTask.ToggleVisibility(false);
			uiBuildingSomething.ToggleVisibility(false);

			if (world.swappingArmy)
            {
                CancelReposition();
                return;
            }
            if (world.deployingArmy || world.assigningGuard)
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
        if (selectedUnit.worker && !selectedUnit.worker.isBusy && !selectedUnit.worker.runningAway)
            uiMoveUnit.ToggleVisibility(true);
        uiWorkerTask.ToggleVisibility(true, world);
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
        ChangeResourceManagersAndUIs(resourceType, playerLoadIncrement);
    }

    public void Unload(ResourceType resourceType)
    {
        if (world.uiResourceGivingPanel.activeStatus)
            Give(resourceType);
        else
            ChangeResourceManagersAndUIs(resourceType, -playerLoadIncrement);
    }

    public void Give(ResourceType resourceType)
    {
        if (world.uiResourceGivingPanel.showingResource && resourceType != world.uiResourceGivingPanel.giftedResource.resourceType)
        {
			UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can only give one resource at a time", true);
			return;
        }

		SetChangesFromGift(resourceType, playerLoadIncrement);
    }

    public void GiveBack(ResourceType resourceType)
    {
		SetChangesFromGift(resourceType, -playerLoadIncrement);
	}

    public void LoadUnloadFinish(bool keepSelection) //putting the screens back after finishing loading cargo
    {
        if (loadScreenSet)
        {
            uiWorkerTask.uiLoadUnload.ToggleColor(false);
            uiPersonalResourceInfoPanel.RestorePosition(keepSelection);
            uiCityResourceInfoPanel.RestorePosition(keepSelection);
            cityResourceManager = null;
            tradeCenter = null;
            loadScreenSet = false;
        }
    }

    public void GivingFinish(bool keepSelection)
    {
		if (world.uiResourceGivingPanel.activeStatus)
			world.uiResourceGivingPanel.ToggleVisibility(false, keepSelection, false);
	}

	private void ChangeResourceManagersAndUIs(ResourceType resourceType, int resourceAmount)
    {
        //for buying and selling resources in trade center (stand alone)
        world.mainPlayer.personalResourceManager.DictCheckSolo(resourceType);

        if (tradeCenter)
        {
            if (resourceAmount > 0) //buying 
            {
                int cost = Mathf.CeilToInt(tradeCenter.resourceBuyDict[resourceType] * tradeCenter.multiple);
				if (!world.CheckWorldGold(cost))
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

				int buyAmount = -resourceAmountAdjusted * cost;
                world.UpdateWorldGold(buyAmount);
                InfoResourcePopUpHandler.CreateResourceStat(world.mainPlayer.transform.position, buyAmount, ResourceHolder.Instance.GetIcon(ResourceType.Gold), world);

                uiPersonalResourceInfoPanel.UpdateResourceInteractable(resourceType, world.mainPlayer.personalResourceManager.GetResourceDictValue(resourceType), true);
                uiPersonalResourceInfoPanel.UpdateStorageLevel(world.mainPlayer.personalResourceManager.resourceStorageLevel);
            }
            else if (resourceAmount <= 0) //selling
            {
                if (tradeCenter.resourceSellDict.ContainsKey(resourceType))
                {
                    int remainingWithTrader = world.mainPlayer.personalResourceManager.GetResourceDictValue(resourceType);

                    if (remainingWithTrader < Mathf.Abs(resourceAmount))
                        resourceAmount = -remainingWithTrader;

                    if (resourceAmount == 0)
                        return;

					world.mainPlayer.personalResourceManager.ManuallySubtractResource(resourceType, -resourceAmount);

                    int sellAmount = -resourceAmount * tradeCenter.resourceSellDict[resourceType];
                    world.UpdateWorldGold(sellAmount);
                    InfoResourcePopUpHandler.CreateResourceStat(world.mainPlayer.transform.position, sellAmount, ResourceHolder.Instance.GetIcon(ResourceType.Gold), world);

                    uiPersonalResourceInfoPanel.UpdateResourceInteractable(resourceType, world.mainPlayer.personalResourceManager.GetResourceDictValue(resourceType), false);
                    uiCityResourceInfoPanel.FlashResource(resourceType);
                    uiPersonalResourceInfoPanel.UpdateStorageLevel(world.mainPlayer.personalResourceManager.resourceStorageLevel);
                }
                else
                {
                    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't sell " + resourceType);
                }
            }
            
            return;
        }

        bool personalFull = false;
        bool noneLeft = false;

        if (resourceAmount > 0) //moving from city to trader
        {
            int remainingInCity;
            remainingInCity = cityResourceManager.GetResourceDictValue(resourceType);

            if (remainingInCity < resourceAmount)
                resourceAmount = remainingInCity;

			noneLeft = resourceAmount == 0;
			int resourceAmountAdjusted = world.mainPlayer.personalResourceManager.ManuallyAddResource(resourceType, resourceAmount);
            personalFull = resourceAmountAdjusted == 0;

            if (cityResourceManager != null)
                cityResourceManager.SubtractResource(resourceType, resourceAmountAdjusted);
        }

        bool cityFull = false;

        if (resourceAmount <= 0) //moving from trader to city
        {
			int remainingWithTrader = world.mainPlayer.personalResourceManager.GetResourceDictValue(resourceType);

            if (remainingWithTrader < Mathf.Abs(resourceAmount))
                resourceAmount = -remainingWithTrader;

            noneLeft = resourceAmount == 0;
            int resourceAmountAdjusted;
            cityResourceManager.resourceCount = 0;
            resourceAmountAdjusted = cityResourceManager.AddResource(resourceType, -resourceAmount);

            cityFull = resourceAmountAdjusted == 0;
			world.mainPlayer.personalResourceManager.ManuallySubtractResource(resourceType, resourceAmountAdjusted);
		}

        bool toTrader = resourceAmount > 0;

        if (!cityFull)
        {
            if (cityResourceManager != null)
            {
                uiCityResourceInfoPanel.UpdateResourceInteractable(resourceType, cityResourceManager.GetResourceDictValue(resourceType), !toTrader);
                uiCityResourceInfoPanel.UpdateStorageLevel(cityResourceManager.resourceStorageLevel);
            }
        }
        else
        {
    		if (noneLeft)
                InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(selectedUnit.transform.position, "None left");
            else
				InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(selectedUnit.transform.position, "No storage space");
		}

        if (!personalFull)
        {
            uiPersonalResourceInfoPanel.UpdateResourceInteractable(resourceType, world.mainPlayer.personalResourceManager.GetResourceDictValue(resourceType), toTrader);
            uiPersonalResourceInfoPanel.UpdateStorageLevel(world.mainPlayer.personalResourceManager.resourceStorageLevel);
        }
        else
        {
			if (noneLeft)
				InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(selectedUnit.transform.position, "None left");
			else
				InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(selectedUnit.transform.position, "No storage space");
		}

		world.mainPlayer.personalResourceManager.ResetDictSolo(resourceType);
    }

	private void SetChangesFromGift(ResourceType type, int amount)
    {
		if (amount > 0)
        {
            int remainingWithTrader = world.mainPlayer.personalResourceManager.GetResourceDictValue(type);

		    if (remainingWithTrader < Mathf.Abs(amount))
			    amount = -remainingWithTrader;

		    world.mainPlayer.personalResourceManager.ManuallySubtractResource(type, amount);
        }
        else
        {
            uiPersonalResourceInfoPanel.UpdateResourceInteractable(type, world.mainPlayer.personalResourceManager.GetResourceDictValue(type), true);
			world.mainPlayer.personalResourceManager.ManuallyAddResource(type, -amount, true);
		}

        world.uiResourceGivingPanel.ChangeGiftAmount(type, amount);
	}

	public void SetUpTradeRoute()
    {
		if (!selectedUnit.bySea && !selectedUnit.trader.followingRoute && !world.IsRoadOnTileLocation(world.GetClosestTerrainLoc(selectedUnit.trader.transform.position)) && !selectedUnit.trader.atHome)
		{
			UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't edit route off road");
			return;
		}

		world.cityBuilderManager.PlaySelectAudio();
        if (!uiTradeRouteManager.activeStatus)
        {
            infoManager.HideInfoPanel();
            uiTradeRouteManager.ToggleButtonColor(true);

            Vector3Int traderLoc;
            traderLoc = selectedUnit.trader.atHome ? world.GetCity(selectedUnit.trader.homeCity).singleBuildDict[selectedUnit.buildDataSO.singleBuildType] : 
                world.GetClosestTerrainLoc(selectedUnit.transform.position);

			//only showing city names accessible by unit
			List<string> cityNames = world.GetConnectedCityNames(traderLoc, selectedUnit.bySea, false, selectedUnit.buildDataSO.singleBuildType); 
            uiTradeRouteManager.PrepareTradeRouteMenu(cityNames, selectedUnit.trader);
            world.uiTradeRouteBeginTooltip.ToggleVisibility(false);
            uiTradeRouteManager.ToggleVisibility(true);
            uiTradeRouteManager.LoadTraderRouteInfo(selectedUnit.trader, selectedUnit.trader.tradeRouteManager, world);
        }
        else
        {
            uiTradeRouteManager.ToggleVisibility(false);
        }
    }

    public void ShowTradeRouteCost()
    {
        if (!selectedUnit.trader.hasRoute)
            return;
        
        world.cityBuilderManager.PlaySelectAudio();

		if (selectedUnit.trader.followingRoute)
		{
			CancelTradeRoute();
			return;
		}

		if (!selectedUnit.trader.tradeRouteManager.TradeRouteCheck())
			return;

		if (!selectedUnit.trader.StartingCityCheck())
		{
			UIInfoPopUpHandler.WarningMessage().Create(uiTraderPanel.uiBeginTradeRoute.transform.position, "Starting city removed");
			return;
		}

		uiTradeRouteManager.ToggleVisibility(false);
		world.infoPopUpCanvas.gameObject.SetActive(true);
		world.uiTradeRouteBeginTooltip.ToggleVisibility(true, false, selectedUnit.trader);
	}

	public void BeginTradeRoute() //start going trade route
    {    
        if (selectedUnit && selectedUnit.trader)
        {
            if (!selectedUnit.bySea && !selectedUnit.byAir && !world.IsRoadOnTileLocation(world.GetClosestTerrainLoc(selectedUnit.trader.transform.position)) && !selectedUnit.trader.atHome)
            {
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't start route off road");
				return;
			}

			if (!world.uiTradeRouteBeginTooltip.AffordCheck())
                return;

			selectedUnit.trader.tradeRouteManager.currentDestination = selectedUnit.trader.tradeRouteManager.cityStops[selectedUnit.trader.tradeRouteManager.currentStop];
            if (!world.StopExistsCheck(selectedUnit.trader.tradeRouteManager.currentDestination))
            {
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Next stop missing");
				return;
            }

            if (!selectedUnit.trader.StartingCityCheck())
            {
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Starting city missing");
				return;
            }

			if (selectedUnit.trader.LineCutterCheck())
				return;

			if (selectedUnit.trader.guarded)
			{
    			selectedUnit.trader.guardUnit.StopMovementCheck(false);
    
                if (selectedUnit.trader.atHome)
                {
                    //first getting a spot for the guard, if there is one
                    Vector3Int middleTile = world.GetClosestTerrainLoc(selectedUnit.transform.position);
                    Vector3 chosenSpot;
                    if (selectedUnit.bySea)
                    {
                        chosenSpot = (selectedUnit.transform.position + middleTile) / 2; //go to the average position between trader and middle
                    }
                    else if (selectedUnit.byAir)
                    {
                        Vector3 tempSpot = selectedUnit.transform.position;
                        tempSpot.y += 1;
						chosenSpot = tempSpot;
                    }
                    else
                    {
						chosenSpot = selectedUnit.transform.position;
                    }

					Military guardUnit = selectedUnit.trader.guardUnit.military;
                
                    if (guardUnit.army != null)
                    {
    				    guardUnit.army.RemoveFromArmy(selectedUnit.trader.guardUnit.military, selectedUnit.trader.guardUnit.military.barracksBunk, true);
				        guardUnit.army = null;
                    }

				    guardUnit.atHome = false;
				    guardUnit.guardedTrader = selectedUnit.trader;
				    guardUnit.guard = true;

                    selectedUnit.trader.waitingOnGuard = true;
                    selectedUnit.trader.CheckWarning();

                    guardUnit.SoloMove(chosenSpot);
				}
			}

			uiUnload.ToggleVisibility(false);
            uiJoinCity.ToggleVisibility(false);

            world.cityBuilderManager.PlaySelectAudio(world.cityBuilderManager.coinsClip);
            world.uiTradeRouteBeginTooltip.ToggleVisibility(false, true);
			selectedUnit.trader.SpendRouteCosts(selectedUnit.trader.tradeRouteManager.startingStop, selectedUnit.trader.GetStartingCity());

			selectedUnit.StopMovementCheck(false);
            selectedUnit.trader.TradersHereCheck();

            if (!selectedUnit.trader.waitingOnGuard)
			    selectedUnit.trader.BeginNextStepInRoute();

            selectedUnit.trader.returning = false;
            uiTraderPanel.SwitchRouteIcons(true);
            uiTradeRouteManager.ToggleVisibility(false);
        }
    }

    public void CancelTradeRoute() //stop following route but still keep route description
    {
        if (selectedUnit.trader.ambush)
        {
			UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't, being ambushed", false);
			return;
        }

		selectedUnit.trader.RefundRouteCosts();
        selectedUnit.trader.RemoveWarning();
		selectedUnit.trader.CancelRoute();
        
        if (uiTradeRouteManager.activeStatus)
        {
            uiTradeRouteManager.ResetTradeRouteInfo(selectedUnit.trader.tradeRouteManager);
            uiTradeRouteManager.ResetButtons();
        }
    }

    public void ShowIndividualCityButtonsUI()
    {
        if (selectedUnit == null)
            return;

		Vector3Int currentLoc = world.GetClosestTerrainLoc(selectedUnit.transform.position);

		if (selectedUnit.isPlayer)
        {
            if (!selectedUnit.worker.runningAway && !selectedUnit.worker.isBusy && !selectedUnit.worker.inEnemyLines)
                uiMoveUnit.ToggleVisibility(true);
            
            if (selectedUnit.isMoving)
            {
				if (!selectedUnit.worker.runningAway)
				    selectedUnit.ShowContinuedPath();
			}
		}
        else if (selectedUnit.trader)
        {
            if (selectedUnit.isMoving)
			{
				selectedUnit.ShowContinuedPath();
			}
            else
            {
                if (selectedUnit.trader.atHome)
                {
					uiJoinCity.ToggleVisibility(true);

                    if (selectedUnit.trader.personalResourceManager.resourceStorageLevel > 0)
                        uiUnload.ToggleVisibility(true);
				}
			}
		}
        else if (selectedUnit.buildDataSO.inMilitary)
        {
			if (selectedUnit.military.atHome)
			{
				if (!selectedUnit.military.army.defending && !selectedUnit.military.army.returning)
				{
					uiJoinCity.ToggleVisibility(true);
					//uiSwapPosition.ToggleVisibility(true);
					uiDeployArmy.ToggleVisibility(true);
					uiChangeCity.ToggleVisibility(true);
				}
			}
            else if (selectedUnit.military.guard || selectedUnit.military.transferring) //here as place holder
            {
            }
            else if (selectedUnit.military.army.traveling)
            {
                if (selectedUnit.military.army.targetCamp.fieldBattleLoc == selectedUnit.military.army.targetCamp.cityLoc)
					uiCancelTask.ToggleVisibility(true);
            }
            else if (selectedUnit.military.inBattle) //can't retreat currently
            {
				//if (!selectedUnit.military.army.defending && selectedUnit.military.army.targetCamp.fieldBattleLoc == selectedUnit.military.army.targetCamp.cityLoc)
				//	uiCancelTask.ToggleVisibility(true);
			}
		}
        else if (selectedUnit.laborer)
        {
			selectedUnit.ShowContinuedPath();
        }
        else if (selectedUnit.transport)
        {
            if (selectedUnit.transport.canMove)
            {
                if (!selectedUnit.isMoving) //checking if next to land to unload
                    selectedUnit.transport.FinishMovementTransport(selectedUnit.transform.position);
                uiMoveUnit.ToggleVisibility(true);
            }
            else if (selectedUnit.transport.passengerCount == 0)
            {
                if (world.IsSingleBuildStopOnTile(currentLoc, selectedUnit.buildDataSO.singleBuildType))
					uiJoinCity.ToggleVisibility(true);
            }
        }
    }


    public void TurnOnInfoScreen()
    {
        if (selectedUnit && selectedUnit.trader)
        {
			infoManager.ShowInfoPanel(selectedUnit.name, selectedUnit.buildDataSO, selectedUnit.currentHealth, selectedUnit.trader, 0, false);
        }
    }

    public void ResetArmyHomeButtons()
    {
		uiJoinCity.ToggleVisibility(false);
		//uiSwapPosition.ToggleVisibility(false);
		uiDeployArmy.ToggleVisibility(false);
		uiChangeCity.ToggleVisibility(false);
	}

    public void ChangeHomeBase()
    {
        world.cityBuilderManager.PlaySelectAudio();
        uiJoinCity.ToggleVisibility(false);
		//uiSwapPosition.ToggleVisibility(false);
		uiDeployArmy.ToggleVisibility(false);
		uiChangeCity.ToggleVisibility(false);
        world.uiLaborDestinationWindow.ToggleVisibility(true, false, selectedUnit.military.army.city, selectedUnit);
        world.changingCity = true;
    }

  //  public void StillOnMilitaryUnitCheck()
  //  {
  //      if (selectedUnit != null)
  //      {
		//	uiJoinCity.ToggleVisibility(true);
		//	uiSwapPosition.ToggleVisibility(true);
		//	uiDeployArmy.ToggleVisibility(true);
		//	uiChangeCity.ToggleVisibility(true);
		//}
  //  }

    public void AssignGuard(Army army)
    {
        world.cityBuilderManager.PlaySelectAudio();
		uiJoinCity.ToggleVisibility(false);
        uiUnload.ToggleVisibility(false);
		uiBuildingSomething.ToggleVisibility(true);
		uiBuildingSomething.SetText("Assigning Guard");
		uiCancelTask.ToggleVisibility(true);
		world.unitOrders = true;
		world.assigningGuard = true;
        army.SoftSelectInfantry(Color.green);
        uiTraderPanel.gameObject.SetActive(false);
		world.uiTradeRouteBeginTooltip.gameObject.SetActive(false);
	}

    public void SetUpAttackZoneInfo(Vector3Int loc, bool isCity, int numUsed = 0)
    {
        Vector3Int barracksLoc = loc;
        if (isCity)
            barracksLoc = world.GetEnemyCity(loc).singleBuildDict[SingleBuildType.Barracks];

		int i = numUsed;
        foreach (Vector3Int tile in world.GetNeighborsFor(loc, MapWorld.State.FOURWAYINCREMENT))
        {
            TerrainData td = world.GetTerrainDataAt(tile);

            if (!td.isDiscovered || td.terrainData.terrainDesc == TerrainDesc.Mountain || tile == barracksLoc || world.IsEnemyCityOnTile(tile) || attackZoneList.Contains(tile))
                continue;

            attackZoneList.Add(tile);
            td.EnableHighlight(Color.green);
            int attackZoneBonus = td.terrainData.terrainAttackBonus;
            if (world.CompletedImprovementCheck(tile))
            {
                attackZoneBonus += world.GetCityDevelopment(tile).GetImprovementData.attackBonus;
                world.GetCityDevelopment(tile).EnableHighlight(Color.green);
            }
            if (attackZoneBonus != 0)
            {
                attackBonusText[i].gameObject.SetActive(true);
                SetAttackBonusText(attackBonusText[i], tile, attackZoneBonus);
                i++;
            }
        }

        int enemyAttackZoneBonus = world.GetTerrainDataAt(potentialAttackLoc).terrainData.terrainAttackBonus;
        if (isCity && world.CompletedImprovementCheck(potentialAttackLoc))
            enemyAttackZoneBonus += world.GetCityDevelopment(potentialAttackLoc).GetImprovementData.attackBonus;
        else if (world.CompletedImprovementCheck(barracksLoc))
            enemyAttackZoneBonus += world.GetCityDevelopment(barracksLoc).GetImprovementData.attackBonus;
		if (enemyAttackZoneBonus != 0)
        {
			attackBonusText[i].gameObject.SetActive(true);
			SetAttackBonusText(attackBonusText[i], loc, enemyAttackZoneBonus);
		}

        if (isCity)
        {
            world.GetCityDevelopment(barracksLoc).EnableHighlight(Color.red);
            world.GetTerrainDataAt(barracksLoc).EnableHighlight(Color.red);
            attackZoneList.Add(barracksLoc);
            SetUpAttackZoneInfo(barracksLoc, false, i);
        }
    }

    public void SetMovingAttackBonusText(Vector3Int attackZone, Vector3Int enemyTarget)
    {
		for (int i = 0; i < 2; i++)
			attackBonusText[i].gameObject.SetActive(false);

		int attackZoneBonus = world.GetTerrainDataAt(attackZone).terrainData.terrainAttackBonus;
        if (world.CompletedImprovementCheck(attackZone))
            attackZoneBonus += world.GetCityDevelopment(attackZone).GetImprovementData.attackBonus;

		if (attackZoneBonus != 0)
        {
            attackBonusText[0].gameObject.SetActive(true);
		    SetAttackBonusText(attackBonusText[0], attackZone, attackZoneBonus);
        }

		int enemyTargetBonus = world.GetTerrainDataAt(enemyTarget).terrainData.terrainAttackBonus;
		if (world.CompletedImprovementCheck(enemyTarget))
			enemyTargetBonus += world.GetCityDevelopment(enemyTarget).GetImprovementData.attackBonus;
		if (enemyTargetBonus != 0)
		{
			attackBonusText[1].gameObject.SetActive(true);
			SetAttackBonusText(attackBonusText[1], enemyTarget, enemyTargetBonus);
		}
	}

	private void SetAttackBonusText(AttackBonusHandler text, Vector3Int loc, int bonus)
	{
		text.transform.position = loc;

		if (bonus > 0)
		{
			text.text.text = "+" + bonus.ToString() + "%";
			text.text.color = Color.green;
		}
		else
		{
			text.text.text = bonus.ToString() + "%";
			text.text.color = Color.red;
		}
	}

	public void ShutDownAttackZones()
    {
        foreach (Vector3Int zone in attackZoneList)
        {
			if (world.CompletedImprovementCheck(zone))
                world.GetCityDevelopment(zone).DisableHighlight();
			world.GetTerrainDataAt(zone).DisableHighlight();
        }

        attackZoneList.Clear();

		for (int i = 0; i < attackBonusText.Count; i++)
			attackBonusText[i].gameObject.SetActive(false);
	}

	public void DeployArmyLocation()
    {
        world.cityBuilderManager.PlaySelectAudio();
        City homeBase = selectedUnit.military.army.city;
        
        if (world.GetCityDevelopment(homeBase.singleBuildDict[SingleBuildType.Barracks]).isTraining)
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
        //uiSwapPosition.ToggleVisibility(false);
        uiDeployArmy.ToggleVisibility(false);
        uiChangeCity.ToggleVisibility(false);
		uiBuildingSomething.ToggleVisibility(true);
		uiBuildingSomething.SetText("Deploying Army");
        uiCancelTask.ToggleVisibility(true);
		world.unitOrders = true;

        world.attackMovingTarget = false;
        world.deployingArmy = true;

		if (homeBase.attacked)
            world.HighlightAttackingCity(homeBase.cityLoc);
        else
            world.HighlightAllEnemyCamps();
    }

    public void CancelArmyDeploymentButton()
    {
        world.cityBuilderManager.PlayCloseAudio();
        world.tooltip = false;
        CancelArmyDeployment();
    }

    public void CancelArmyDeployment()
    {
		uiCancelTask.ToggleVisibility(false);
        world.uiCampTooltip.ToggleVisibility(false);
        
        if (selectedUnit == null)
            return;
        
        if (selectedUnit.trader)
        {
            world.unitOrders = false;
            world.assigningGuard = false;
            uiTraderPanel.gameObject.SetActive(true);
			world.uiTradeRouteBeginTooltip.UnselectArmy();
            world.uiTradeRouteBeginTooltip.gameObject.SetActive(true);
			uiBuildingSomething.ToggleVisibility(false);			
            ShowIndividualCityButtonsUI();
			return;
        }
        
        City homeBase = selectedUnit.military.army.city;

        if (homeBase.army.traveling)
        {
            GameLoader.Instance.gameData.attackedEnemyBases.Remove(homeBase.army.enemyTarget);
            homeBase.army.MoveArmyHome(homeBase.singleBuildDict[SingleBuildType.Barracks]);
            world.EnemyCampReturn(homeBase.army.enemyTarget);
            world.EnemyCityReturn(homeBase.army.enemyCityLoc);
			homeBase.army.HidePath();
            world.RemoveBattleZones(homeBase.army.attackZone, homeBase.army.enemyTarget);
		}
        else if (homeBase.army.inBattle)
        {
            homeBase.army.Retreat();
		}
        else if (homeBase.army.atHome)
        {
            if (world.changingCity)
            {
                world.uiLaborDestinationWindow.ToggleVisibility(false);
            }
            else if (world.deployingArmy)
            {
                world.UnhighlightAllEnemyCamps();
                ShutDownAttackZones();
                world.attackMovingTarget = false;
            }

            if (selectedUnit.military.atHome)
            {
                uiJoinCity.ToggleVisibility(true);
		        //uiSwapPosition.ToggleVisibility(true);
                uiDeployArmy.ToggleVisibility(true);
			    uiChangeCity.ToggleVisibility(true);
            }
            else if (!homeBase.army.defending && !selectedUnit.military.repositioning)
            {
                uiCancelTask.ToggleVisibility(true);
            }

			uiBuildingSomething.ToggleVisibility(false);
            world.unitOrders = false;
			world.deployingArmy = false;
			ShutDownAttackZones();
        }
    }

    public void DeployArmy()
    {
        if (selectedUnit == null)
            return;

		if (world.uiCampTooltip.cantAfford)
        {
			world.uiCampTooltip.ShakeCheck();
			UIInfoPopUpHandler.WarningMessage().Create(world.uiCampTooltip.attackButton.transform.position, "Can't afford", false);
			return;
        }

        if (!world.attackMovingTarget && world.IsEnemyCityOnTile(potentialAttackLoc) && world.GetEnemyCity(potentialAttackLoc).enemyCamp.movingOut)
        {
			world.uiCampTooltip.ShakeCheck();
			UIInfoPopUpHandler.WarningMessage().Create(world.uiCampTooltip.attackButton.transform.position, "Can't attack now, enemy deployed", false);
			return;
        }

        City homeBase = selectedUnit.military.army.city;
        world.uiCampTooltip.ToggleVisibility(false, null, null, null, false);
        uiBuildingSomething.ToggleVisibility(false);
		world.UnhighlightAllEnemyCamps();
		world.unitOrders = false;
		world.deployingArmy = false;
		ShutDownAttackZones();

		//unlikely but just in case
		if (potentialAttackLoc == homeBase.army.loc)
        {
            homeBase.army.HidePath();
            return;
        }

        if (selectedUnit.buildDataSO.transportationType == TransportationType.Land)
            world.cityBuilderManager.PlaySelectAudio(world.cityBuilderManager.trainingClip);

		if (world.IsEnemyCityOnTile(potentialAttackLoc))
        {
            City city = world.GetEnemyCity(potentialAttackLoc);

            if (city.empire.capitalCity == city.cityLoc)
                city.empire.enemyLeader.CancelApproachingConversation();

			homeBase.army.enemyCityLoc = city.cityLoc;
			if (city.enemyCamp.movingOut)
            {
				uiCancelTask.ToggleVisibility(false);
				city.enemyCamp.attacked = true;
                world.RemoveBattleZones(city.enemyCamp.actualAttackLoc, city.enemyCamp.threatLoc); //removing original battle locs for enemy that's moving out
                city.enemyCamp.attackingArmy = homeBase.army;
                city.enemyCamp.fieldBattleLoc = homeBase.army.enemyTarget;
            }
            else
            {
                world.SetEnemyCityAsAttacked(potentialAttackLoc, homeBase.army);
            }
            
            selectedUnit.military.army.targetCamp = city.enemyCamp;
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
            selectedUnit.military.army.HidePath();
		    world.HighlightEnemyCamp(potentialAttackLoc, Color.red);
        }
	}

    public void CancelOrders()
    {
        if (world.buildingWonder)
            world.CloseBuildingSomethingPanel();
        else if (selectedUnit.worker)
            workerTaskManager.CancelTask();
        else if (selectedUnit.buildDataSO.inMilitary || selectedUnit.trader)
            CancelArmyDeployment();
    }

    public void ClearSelection()
    {
        if (selectedUnit != null)
        {
            if (selectedUnit.worker && selectedUnit.worker.isBusy && world.scott.IsOrderListMoreThanZero())
                ToggleOrderHighlights(false);

			world.cityBuilderManager.uiTraderNamer.ToggleVisibility(false);
            uiMoveUnit.ToggleVisibility(false);
            uiJoinCity.ToggleVisibility(false);

			if (selectedUnit.isPlayer)
            {
                CancelMove();
                world.scott.Unhighlight();
                world.azai.Unhighlight();
                uiCancelTask.ToggleVisibility(false);
                uiWorkerTask.ToggleVisibility(false, world);
                LoadUnloadFinish(false); //clear load cargo screen
                GivingFinish(false);
                uiPersonalResourceInfoPanel.ToggleVisibility(false);
			}
            else if (selectedUnit.trader)
            {
                uiTraderPanel.uiBeginTradeRoute.ToggleInteractable(false);
                uiTraderPanel.SwitchRouteIcons(false);
                uiTraderPanel.ToggleVisibility(false, world);
                uiUnload.ToggleVisibility(false);
                uiTradeRouteManager.ToggleVisibility(false);
                uiPersonalResourceInfoPanel.ToggleVisibility(false);
                if (selectedUnit.trader.guarded && (selectedUnit.bySea || selectedUnit.byAir))
					selectedUnit.trader.guardUnit.Unhighlight();
				infoManager.infoPanel.HideWarning();
			}
            else if (selectedUnit.buildDataSO.inMilitary)
            {
                //uiSwapPosition.ToggleVisibility(false);
                uiDeployArmy.ToggleVisibility(false);
                uiChangeCity.ToggleVisibility(false);
                world.uiLaborDestinationWindow.ToggleVisibility(false);
				uiCancelTask.ToggleVisibility(false);

                if (selectedUnit.military.guard)
                {
                    selectedUnit.military.guardedTrader.Unhighlight();
                }
                else if (selectedUnit.military.army != null)
                {
                    selectedUnit.military.army.UnselectArmy(selectedUnit.military);

                    if (selectedUnit.military.army.traveling)
                        selectedUnit.military.army.HidePath();
                }
			}
            else if (selectedUnit.transport)
            {
                uiUnload.ToggleVisibility(false); 
            }

            selectedUnit.Unhighlight();
            selectedUnit.HidePath();
            infoManager.HideInfoPanel();
            selectedUnit = null;
        }
    }
}
