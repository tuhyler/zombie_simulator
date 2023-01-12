using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Events;

public class UnitMovement : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;
    [SerializeField]
    private UIUnitTurnHandler turnHandler;
    [SerializeField]
    private InfoManager infoManager;
    [SerializeField]
    private WorkerTaskManager workerTaskManager;
    [SerializeField]
    private UISingleConditionalButtonHandler uiCancelMove; //cancel movement orders
    [SerializeField]
    public UISingleConditionalButtonHandler uiJoinCity;
    [SerializeField]
    public UISingleConditionalButtonHandler uiMoveUnit;
    [SerializeField]
    public UISingleConditionalButtonHandler uiCancelTask;
    [SerializeField]
    public UISingleConditionalButtonHandler uiConfirmWorkerOrders;
    [SerializeField]
    public UITraderOrderHandler uiTraderPanel;
    [SerializeField]
    public UIWorkerHandler uiWorkerTask;
    [SerializeField]
    private UIPersonalResourceInfoPanel uiPersonalResourceInfoPanel;
    public UIPersonalResourceInfoPanel GetUIPersonalResourceInfoPanel { get { return uiPersonalResourceInfoPanel; } }
    [SerializeField]
    private UIPersonalResourceInfoPanel uiCityResourceInfoPanel;
    [SerializeField]
    private UITradeRouteManager uiTradeRouteManager;
    [SerializeField]
    private UISingleConditionalButtonHandler uiCancelTradeRoute;

    private bool queueMovementOrders;
    private MovementSystem movementSystem;

    private Unit selectedUnit;
    private Worker selectedWorker;
    public Worker SetSelectedWorker { set { selectedWorker = value; } }
    private Trader selectedTrader;
    //private TerrainData selectedTile;
    private InfoProvider selectedUnitInfoProvider;
    private bool loadScreenSet; //flag if load/unload ui is showing
    private bool moveUnit;

    //for transferring cargo to/from trader
    private ResourceManager cityResourceManager;
    private Wonder wonder;
    public int cityTraderIncrement = 1;

    //for worker orders
    [HideInInspector]
    public bool buildingRoad, removingAll, removingRoad, removingLiquid, removingPower; 
    //private List<TerrainData> highlightedTiles = new();

    private void Awake()
    {
        movementSystem = GetComponent<MovementSystem>();
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
        if (selectedWorker != null && world.workerOrders)
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
        location.y = 0;

        Vector3Int locationPos = world.GetClosestTerrainLoc(location);

        //if building road, can't select anything else
        if (world.workerOrders)
        {
            TerrainData td = world.GetTerrainDataAt(locationPos);
            
            if (buildingRoad)
            {
                if (world.IsRoadOnTerrain(locationPos) || world.IsBuildLocationTaken(locationPos))
                {
                    GiveWarningMessage("Already something here");
                }
                else if (!td.terrainData.walkable)
                {
                    GiveWarningMessage("Can't build here");
                }
                else
                {
                    if (selectedWorker.AddToOrderQueue(locationPos))
                    {
                        if (selectedWorker.IsOrderListMoreThanZero())
                            uiConfirmWorkerOrders.ToggleTweenVisibility(true);

                        td.EnableHighlight(Color.white);
                        //highlightedTiles.Add(td);
                    }
                    else
                    {
                        if (!selectedWorker.IsOrderListMoreThanZero())
                            uiConfirmWorkerOrders.ToggleTweenVisibility(false);

                        td.DisableHighlight();
                        //highlightedTiles.Remove(td);
                    }
                }
            }
            else if (removingRoad)
            {
                if (!world.IsRoadOnTerrain(locationPos))
                {
                    GiveWarningMessage("No road here");
                }
                else if (world.IsCityOnTile(locationPos) || world.IsWonderOnTile(locationPos))
                {
                    GiveWarningMessage("Can't remove this");
                }
                else
                {
                    if (selectedWorker.AddToOrderQueue(locationPos))
                    {
                        if (selectedWorker.IsOrderListMoreThanZero())
                            uiConfirmWorkerOrders.ToggleTweenVisibility(true);

                        td.EnableHighlight(Color.red);
                        foreach (GameObject go in world.GetAllRoadsOnTile(locationPos))
                        {
                            if (go == null)
                                continue;
                            go.GetComponent<SelectionHighlight>().EnableHighlight(Color.white);
                        }
                        //highlightedTiles.Add(td);
                    }
                    else
                    {
                        if (!selectedWorker.IsOrderListMoreThanZero())
                            uiConfirmWorkerOrders.ToggleTweenVisibility(false);

                        td.DisableHighlight();
                        foreach (GameObject go in world.GetAllRoadsOnTile(locationPos))
                        {
                            if (go == null)
                                continue;
                            go.GetComponent<SelectionHighlight>().DisableHighlight();
                        }
                        //highlightedTiles.Remove(td);
                    }
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
            TerrainData terrainSelected = world.GetTerrainDataAt(Vector3Int.RoundToInt(location));
            MoveUnit(terrainSelected, location);
        }
        else if (detectedObject.TryGetComponent(out Unit unitReference) && unitReference.CompareTag("Player"))
        {
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

            uiMoveUnit.ToggleTweenVisibility(true);
            SelectWorker();
            SelectTrader();
            PrepareMovement();
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
            //selectedUnit = null;
            //selectedTrader = null;
            ClearSelection();
        }

        //if (selectedUnit == null)
        //    return;
    }

    private void SelectWorker()
    {
        if (selectedUnit.isWorker)
        {
            selectedWorker = selectedUnit.GetComponent<Worker>();
            workerTaskManager.SetWorkerUnit(selectedWorker);
            uiWorkerTask.ToggleVisibility(true);
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
            uiPersonalResourceInfoPanel.PrepareResourceUI(selectedTrader.PersonalResourceManager.ResourceDict);
            uiPersonalResourceInfoPanel.SetTitleInfo(selectedTrader.name,
                selectedTrader.PersonalResourceManager.GetResourceStorageLevel, selectedTrader.CargoStorageLimit);
            uiPersonalResourceInfoPanel.ToggleVisibility(true);
            uiTraderPanel.ToggleVisibility(true);
            if (world.IsTradeLocOnTile(world.RoundToInt(selectedTrader.transform.position)) && !selectedTrader.followingRoute)
            {
                uiTraderPanel.uiLoadUnload.ToggleInteractable(true);
            }
            else
            {
                uiTraderPanel.uiLoadUnload.ToggleInteractable(false);
            }

            if (selectedTrader.hasRoute && !selectedTrader.followingRoute && !selectedTrader.interruptedRoute)
            {
                uiTraderPanel.uiBeginTradeRoute.ToggleInteractable(true);
            }

            if (selectedTrader.followingRoute)
            {
                uiCancelTradeRoute.ToggleTweenVisibility(true);
            }
        }
    }

    public void PrepareMovement(Unit unit) //handling unit selection through the unit turn buttons
    {
        world.CloseResearchTree();
        
        if (selectedUnit != null) //clearing selection if a new unit is clicked
        {
            ClearSelection();
        }

        selectedUnit = unit;

        SelectWorker();
        SelectTrader();
        PrepareMovement();
    }

    private void PrepareMovement()
    {
        //Debug.Log("Sel. unit is " + selectedUnit.name);
        selectedUnit.Select();
        turnHandler.SetIndex(selectedUnit);
        selectedUnitInfoProvider = selectedUnit.GetComponent<InfoProvider>(); //getting the information to show in info panel
        infoManager.ShowInfoPanel(selectedUnitInfoProvider);
        if (selectedUnit.moreToMove)
        {
            uiCancelMove.ToggleTweenVisibility(true);
            //movementSystem.ShowPathToMove(selectedUnit);
            selectedUnit.ShowContinuedPath();
        }
        ShowIndividualCityButtonsUI();
    }

    public void HandleSelectedLocation(Vector3 location, Vector3Int terrainPos, Unit unit)
    {
        if (queueMovementOrders && unit.FinalDestinationLoc != location && unit.isMoving)
        {
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

        unit.FinishedMoving.AddListener(ShowIndividualCityButtonsUI);
        movementSystem.GetPathToMove(world, unit, terrainPos, unit.isTrader); //Call AStar movement

        unit.FinalDestinationLoc = location;
        //uiJoinCity.ToggleTweenVisibility(false);
        if (unit.isBusy)
            uiCancelMove.ToggleTweenVisibility(false);
        else
            uiCancelMove.ToggleTweenVisibility(true);

        if (!queueMovementOrders)
        {
            moveUnit = false;
            uiMoveUnit.ToggleButtonColor(false);
            movementSystem.MoveUnit(unit);
        }
        //movementSystem.HidePath();
        movementSystem.ClearPaths();
        uiJoinCity.ToggleTweenVisibility(false);
        uiTraderPanel.uiLoadUnload.ToggleInteractable(false);
    }

    public void MoveUnitRightClick(Vector3 location, GameObject detectedObject)
    {
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

        location.y = 0;
        Vector3Int locationInt = Vector3Int.RoundToInt(location);
        if (Vector3Int.RoundToInt(selectedUnit.transform.position) == locationInt) //won't move within same tile
            return;

        TerrainData terrain = world.GetTerrainDataAt(locationInt);
        MoveUnit(terrain, location);
    }

    private void MoveUnit(TerrainData terrainSelected, Vector3 location)
    {
        if (selectedUnit.harvested) //if unit just finished harvesting something, send to closest city
            selectedUnit.SendResourceToCity();
        else if (loadScreenSet)
            LoadUnloadFinish(true);

        //if (uiCityResourceInfoPanel.inUse) //close trade panel when clicking to terrain
        //{
        //    LoadUnloadFinish();
        //    return;
        //}

        Vector3Int terrainPos = Vector3Int.RoundToInt(location);


        if (selectedUnit.bySea)
        {
            if (!terrainSelected.GetTerrainData().sailable)
            {
                GiveWarningMessage("Can't move there");
                return;
            }
        }
        else if (!terrainSelected.GetTerrainData().walkable) //cancel movement if terrain isn't walkable
        {
            GiveWarningMessage("Can't move there");
            return;
        }

        if (selectedTrader != null && selectedTrader.followingRoute) //can't change orders if following route
        {
            GiveWarningMessage("Currently following route");
            return;
        }

        if (selectedTrader != null && !selectedTrader.bySea && !world.IsRoadOnTileLocation(Vector3Int.RoundToInt(location)))
        {
            GiveWarningMessage("Must travel on road");

            //Debug.Log("Trader must travel on road.");
            //if (!selectedUnit.isMoving)
            //{
            //    selectedUnit.HidePath();
            //    movementSystem.ClearPaths();
            //}
            //selectedTile = null;
            return;
        }

        if (selectedUnit.isMoving && !queueMovementOrders) //interrupt orders if new ones
        {
            //selectedUnit.isMoving = false;
            //selectedUnit.StopMovement();
            selectedUnit.ShiftMovement();
            selectedUnit.FinishedMoving.RemoveAllListeners();
            //return;
        }

        //    //if (world.IsUnitLocationTaken(terrainPos))
        //    //{ //cancel movement if unit is already there

        //    //    //below is for switching places with neighboring unit
        //    //    Vector3Int currentPos = Vector3Int.FloorToInt(selectedUnit.transform.position);

        //    //    if (Math.Abs(currentPos.x - terrainPos.x) <= 1 && Math.Abs(currentPos.z - terrainPos.z) <= 1) //seeing if next to each other
        //    //    {
        //    //        Unit unitInTheWay = world.GetUnit(terrainPos);
        //    //        if (unitInTheWay.GetComponent<Trader>() != null && !world.GetTerrainDataAt(currentPos).hasRoad)
        //    //        {
        //    //            Debug.Log("Trader must travel on road.");
        //    //            return;
        //    //        }

        //    //        if (!unitInTheWay.isBusy && !selectedUnit.isBusy)
        //    //        {
        //    //            MovementPreparations();
        //    //            world.RemoveUnitPosition(currentPos/*, selectedUnit.gameObject*/); //need to remove both at same time to allow swapping spaces
        //    //            world.RemoveUnitPosition(terrainPos/*, unitInTheWay.gameObject*/);

        //    //            //moving unit in the way
        //    //            TerrainData currentTile = world.GetTerrainDataAt(currentPos);
        //    //            TerrainData terrainTile = world.GetTerrainDataAt(terrainPos);
        //    //            List<TerrainData> unitInTheWayPath = new() { currentTile };
        //    //            unitInTheWay.MoveThroughPath(unitInTheWayPath);

        //    //            //moving selected unit
        //    //            List<TerrainData> selectedPath = new() { terrainTile };
        //    //            selectedUnit.MoveThroughPath(selectedPath);
        //    //            return;
        //    //        }
        //    //    }
        //    //    //above is for switching places with neighboring unit

        //    //    Debug.Log("Unit already at selected tile");
        //    //    return;
        //    //}

        HandleSelectedLocation(location, terrainPos, selectedUnit);
    }

    private void GiveWarningMessage(string message)
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; //z must be more than 0, else just gives camera position
        Vector3 mouseLoc = Camera.main.ScreenToWorldPoint(mousePos);

        InfoPopUpHandler.Create(mouseLoc, message);
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
            City joinedCity = world.GetCity(unitLoc);
            joinedCity.PopulationGrowthCheck(true);

            int i = 0;
            foreach (ResourceValue resourceValue in selectedUnit.GetBuildDataSO().unitCost) //adding back 100% of cost (if there's room)
            {
                int resourcesGiven = joinedCity.ResourceManager.CheckResource(resourceValue.resourceType, resourceValue.resourceAmount);
                Vector3 cityLoc = joinedCity.cityLoc;
                cityLoc.z += -.5f * i;
                InfoResourcePopUpHandler.CreateResourceStat(cityLoc, resourcesGiven, ResourceHolder.Instance.GetIcon(resourceValue.resourceType));
                i++;
            }
        }
        else
        {
            world.GetWonder(unitLoc).AddWorker();
        }

        selectedUnit.DestroyUnit();
        ClearSelection();
    }

    public void LoadUnloadPrep() //for loadunload button for traders
    {
        if (!loadScreenSet)
        {
            uiTraderPanel.uiLoadUnload.ToggleButtonColor(true);
            selectedUnit.HidePath();
            movementSystem.ClearPaths();
            uiTradeRouteManager.CloseWindow();
            //selectedTile = null;

            //Vector3Int unitLoc = Vector3Int.RoundToInt(selectedUnit.transform.position);
            Vector3Int unitLoc = world.GetClosestTerrainLoc(selectedUnit.transform.position);

            if (world.IsCityOnTile(unitLoc))
            {
                City selectedCity = world.GetCity(unitLoc);

                cityResourceManager = selectedCity.ResourceManager;
                uiCityResourceInfoPanel.SetTitleInfo(selectedCity.CityName,
                    cityResourceManager.GetResourceStorageLevel, selectedCity.warehouseStorageLimit);
                uiCityResourceInfoPanel.PrepareResourceUI(cityResourceManager.ResourceDict);
            }
            else
            {
                wonder = world.GetWonder(unitLoc);
                uiCityResourceInfoPanel.SetTitleInfo(wonder.WonderData.wonderName, 10000, 10000);
                uiCityResourceInfoPanel.PrepareResourceUI(wonder.ResourceDict);
                uiCityResourceInfoPanel.HideInventoryLevel();
            }

            uiPersonalResourceInfoPanel.SetPosition();
            uiCityResourceInfoPanel.ToggleVisibility(true);
            uiCityResourceInfoPanel.SetPosition();

            loadScreenSet = true;
        }
        else
        {
            LoadUnloadFinish(true);
        }
    }

    public void ConfirmWorkerOrders()
    {
        if (world.workerOrders)
        {
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
        if (world.workerOrders)
        {
            ClearBuildRoad();
            ToggleOrderHighlights(false);
            ResetOrderFlags();

            selectedWorker.ResetOrderQueue();
            selectedWorker.isBusy = false;
            workerTaskManager.TurnOffCancelTask();
        }
        else if (world.buildingWonder)
        {
            world.CloseBuildingSomethingPanel();
        }
    }

    public void ClearBuildRoad()
    {
        world.workerOrders = false;
        uiConfirmWorkerOrders.ToggleTweenVisibility(false);
        uiMoveUnit.ToggleTweenVisibility(true);
        uiWorkerTask.ToggleVisibility(true);
        workerTaskManager.CloseBuildingRoadPanel();
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
                    foreach (GameObject go in world.GetAllRoadsOnTile(tile))
                    {
                        if (go == null)
                            continue;
                        go.GetComponent<SelectionHighlight>().EnableHighlight(Color.white);
                    }
                }
            }
        }
        else
        {
            foreach (Vector3Int tile in selectedWorker.OrderList)
            {
                world.GetTerrainDataAt(tile).DisableHighlight();

                if (selectedWorker.removing && world.IsRoadOnTerrain(tile))
                {
                    foreach (GameObject go in world.GetAllRoadsOnTile(tile))
                    {
                        if (go == null)
                            continue;
                        go.GetComponent<SelectionHighlight>().DisableHighlight();
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
            if (uiCityResourceInfoPanel.inUse)
                uiCityResourceInfoPanel.EmptyResourceUI();
            uiPersonalResourceInfoPanel.RestorePosition(keepSelection);
            uiCityResourceInfoPanel.RestorePosition(keepSelection);
            cityResourceManager = null;
            wonder = null;
            loadScreenSet = false;
        }
    }

    private void ChangeResourceManagersAndUIs(ResourceType resourceType, int resourceAmount)
    {
        if (wonder != null)
        {
            if (!wonder.CheckResourceType(resourceType))
            {
                InfoPopUpHandler.Create(selectedUnit.transform.position, "Can't move resource " + resourceType);
                return;
            }
            else if (resourceAmount > 0)
            {
                InfoPopUpHandler.Create(selectedUnit.transform.position, "Can't move from wonder");
                return;
            }
        }

        if (resourceAmount > 0) //moving from city to trader
        {
            int remainingInCity;
            
            if (cityResourceManager != null)
                remainingInCity = cityResourceManager.GetResourceDictValue(resourceType);
            else
                remainingInCity = wonder.ResourceDict[resourceType];

            if (remainingInCity < resourceAmount)
                resourceAmount = remainingInCity;

            int resourceAmountAdjusted = selectedTrader.PersonalResourceManager.CheckResource(resourceType, resourceAmount);

            if (cityResourceManager != null)
                cityResourceManager.CheckResource(resourceType, -resourceAmountAdjusted);
            else
                wonder.CheckResource(resourceType, -resourceAmountAdjusted);
        }

        if (resourceAmount <= 0) //moving from trader to city
        {
            int remainingWithTrader = selectedTrader.PersonalResourceManager.GetResourceDictValue(resourceType);

            if (remainingWithTrader < Mathf.Abs(resourceAmount))
                resourceAmount = -remainingWithTrader;

            int resourceAmountAdjusted;
            if (cityResourceManager != null)
                resourceAmountAdjusted = cityResourceManager.CheckResource(resourceType, -resourceAmount);
            else
                resourceAmountAdjusted = wonder.CheckResource(resourceType, -resourceAmount);
            selectedTrader.PersonalResourceManager.CheckResource(resourceType, -resourceAmountAdjusted);
        }

        if (cityResourceManager != null)
        {
            uiCityResourceInfoPanel.UpdateResourceInteractable(resourceType, cityResourceManager.GetResourceDictValue(resourceType));
            uiCityResourceInfoPanel.UpdateStorageLevel(cityResourceManager.GetResourceStorageLevel);
        }
        else
        {
            uiCityResourceInfoPanel.UpdateResourceInteractable(resourceType, wonder.ResourceDict[resourceType]);
        }

        uiPersonalResourceInfoPanel.UpdateResourceInteractable(resourceType, selectedTrader.PersonalResourceManager.GetResourceDictValue(resourceType));
        uiPersonalResourceInfoPanel.UpdateStorageLevel(selectedTrader.PersonalResourceManager.GetResourceStorageLevel);
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
            uiTradeRouteManager.LoadTraderRouteInfo(selectedTrader, world);
        }
        else
        {
            uiTradeRouteManager.CloseWindow();
        }
    }

    public void BeginTradeRoute() //start going trade route
    {
        if (selectedTrader != null)
        {
            selectedTrader.BeginNextStepInRoute();
            uiTraderPanel.uiBeginTradeRoute.ToggleInteractable(false);
            uiCancelTradeRoute.ToggleTweenVisibility(true);
        }

        //ClearSelection();
    }

    public void CancelTradeRoute() //stop following route but still keep route description
    {
        selectedTrader.CancelRoute();
        ShowIndividualCityButtonsUI();
        CancelContinuedMovementOrders();
        uiCancelTradeRoute.ToggleTweenVisibility(false);
        if (!selectedTrader.interruptedRoute)
            uiTraderPanel.uiBeginTradeRoute.ToggleInteractable(true);
    }

    public void UninterruptedRoute()
    {
        selectedTrader.interruptedRoute = false;
    }

    public void ShowIndividualCityButtonsUI()
    {
        if (selectedUnit == null)
            return;

        if (!selectedUnit.moreToMove)
        {
            selectedUnit.FinishedMoving.RemoveListener(ShowIndividualCityButtonsUI);
            uiCancelMove.ToggleTweenVisibility(false);
        }

        Vector3Int currentLoc = world.GetClosestTerrainLoc(selectedUnit.transform.position);

        if (!selectedUnit.followingRoute && !selectedUnit.isMoving)
        {
            if (world.IsCityOnTile(currentLoc))
            {
                uiJoinCity.ToggleTweenVisibility(true);
            }
            
            if (selectedTrader != null && world.IsTradeLocOnTile(currentLoc))
            {
                uiTraderPanel.uiLoadUnload.ToggleInteractable(true);
            }
            else if (selectedUnit.isWorker && world.IsWonderOnTile(currentLoc))
            {
                if (world.GetWonder(currentLoc).StillNeedsWorkers())
                    uiJoinCity.ToggleTweenVisibility(true);
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
            infoManager.ShowInfoPanel(selectedUnitInfoProvider);
        }
    }

    public void ClearSelection()
    {
        //selectedTile = null;
        if (selectedUnit != null)
        {
            if (selectedUnit.isBusy && selectedWorker.IsOrderListMoreThanZero())
                ToggleOrderHighlights(false);

            moveUnit = false;
            uiMoveUnit.ToggleTweenVisibility(false);
            uiCancelMove.ToggleTweenVisibility(false);
            uiJoinCity.ToggleTweenVisibility(false);

            if (selectedWorker != null)
                uiCancelTask.ToggleTweenVisibility(false);
            uiTraderPanel.uiBeginTradeRoute.ToggleInteractable(false);
            uiTraderPanel.ToggleVisibility(false);
            uiCancelTradeRoute.ToggleTweenVisibility(false);
            uiTradeRouteManager.CloseWindow();
            uiWorkerTask.ToggleVisibility(false);
            //if (selectedUnit != null)
            //{
            selectedUnit.Deselect();
            selectedUnit.HidePath();
            //}
            uiPersonalResourceInfoPanel.ToggleVisibility(false);
            LoadUnloadFinish(false); //clear load cargo screen
            infoManager.HideInfoPanel();
            //movementSystem.ClearPaths(); //necessary to queue movement orders
            selectedUnitInfoProvider = null;
            workerTaskManager.NullWorkerUnit();
            selectedTrader = null;
            selectedWorker = null;
            selectedUnit = null;
        }
    }

    //private bool CheckIfTheSameUnitSelected(Unit unitReference)
    //{
    //    return selectedUnit == unitReference;
    //}
}
