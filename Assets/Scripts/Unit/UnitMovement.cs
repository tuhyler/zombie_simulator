using System.Collections.Generic;
using System.Resources;
using UnityEngine;

public class UnitMovement : MonoBehaviour
{
    [SerializeField]
    public MapWorld world;
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
    public UIPersonalResourceInfoPanel uiPersonalResourceInfoPanel;
    [SerializeField]
    public UIPersonalResourceInfoPanel uiCityResourceInfoPanel;
    [SerializeField]
    private UITradeRouteManager uiTradeRouteManager;
    [SerializeField]
    private UISingleConditionalButtonHandler uiCancelTradeRoute;

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
    public bool buildingRoad, removingAll, removingRoad, removingLiquid, removingPower; 
    //private List<TerrainData> highlightedTiles = new();

    private void Awake()
    {
        movementSystem = GetComponent<MovementSystem>();
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
        if (world.workerOrders)
        {
            TerrainData td = world.GetTerrainDataAt(pos);
            
            if (buildingRoad)
            {
                if (world.IsRoadOnTerrain(pos) || world.IsBuildLocationTaken(pos))
                {
                    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Already something here");
                }
                else if (!td.terrainData.walkable)
                {
                    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't build here");
                }
                else
                {
                    if (selectedWorker.AddToOrderQueue(pos))
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
                            uiConfirmWorkerOrders.ToggleTweenVisibility(true);

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
                            uiConfirmWorkerOrders.ToggleTweenVisibility(false);

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

            return;            
        }
        
        //moving unit upon selection
        if (moveUnit && selectedUnit != null) //detectedObject.TryGetComponent(out TerrainData terrainSelected) && selectedUnit != null)
        {
            if (selectedUnit.isBusy)
                return;

            //location.y = 0;
            //TerrainData terrainSelected = world.GetTerrainDataAt(Vector3Int.RoundToInt(location));
            MoveUnit(location);
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

            if (unitReference.somethingToSay)
            {
                unitReference.somethingToSay = false;
                unitReference.sayingSomething = true;
                world.PlayMessage(location);
                CenterCamOnUnit();
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
            if (selectedUnit.interruptedRoute)
                selectedUnit.InterruptedRouteMessage();
            //uiPersonalResourceInfoPanel.PrepareResourceUI(selectedTrader.resourceGridDict);
            uiPersonalResourceInfoPanel.SetTitleInfo(selectedTrader.name,
                selectedTrader.personalResourceManager.GetResourceStorageLevel, selectedTrader.cargoStorageLimit);
            uiPersonalResourceInfoPanel.ToggleVisibility(true, selectedTrader);
            uiTraderPanel.ToggleVisibility(true);
            if (world.IsTradeLocOnTile(world.RoundToInt(selectedTrader.transform.position)) && !selectedTrader.followingRoute)
            {
                uiTraderPanel.uiLoadUnload.ToggleInteractable(true);
            }
            else
            {
                uiTraderPanel.uiLoadUnload.ToggleInteractable(false);
            }

            if (selectedTrader.hasRoute && !selectedTrader.followingRoute/* && !selectedTrader.interruptedRoute*/)
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
        world.somethingSelected = true;
        selectedUnit.Select();
        turnHandler.SetIndex(selectedUnit);
        //selectedUnitInfoProvider = selectedUnit.GetComponent<InfoProvider>(); //getting the information to show in info panel
        infoManager.ShowInfoPanel(selectedUnit.buildDataSO, selectedUnit.currentHealth);
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
        if (queueMovementOrders /*&& unit.FinalDestinationLoc != location*/ && unit.isMoving)
        {
            if (unit.FinalDestinationLoc == location)
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

        if (selectedUnit != null && selectedUnit.sayingSomething)
            SpeakingCheck();

        MoveUnit(location);
    }

    private void MoveUnit(Vector3 location)
    {
        Vector3 locationInt = location;
        locationInt.y = 0f;
        TerrainData terrainSelected = world.GetTerrainDataAt(world.RoundToInt(locationInt));
        Vector3Int terrainPos = world.RoundToInt(locationInt);
        //if (world.RoundToInt(selectedUnit.transform.position) == world.GetClosestTerrainLoc(locationInt)) //won't move within same tile
        //    return;

        if (selectedUnit.harvested) //if unit just finished harvesting something, send to closest city
            selectedUnit.SendResourceToCity();
        else if (loadScreenSet)
            LoadUnloadFinish(true);

        if (selectedUnit.bySea)
        {
            if (!terrainSelected.GetTerrainData().sailable)
            {
                UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't move there");
                return;
            }

            selectedTrader.TurnOnRipples();
        }
        else if (!world.CheckIfPositionIsValid(terrainPos)) //cancel movement if terrain isn't walkable
        {
            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't move there");
            return;
        }

        if (selectedTrader != null && selectedTrader.followingRoute) //can't change orders if following route
        {
            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Currently following route");
            return;
        }

        if (selectedTrader != null && !selectedTrader.bySea && !world.IsRoadOnTileLocation(terrainPos))
        {
            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Must travel on road");
            return;
        }

        if (selectedUnit.isMoving && !queueMovementOrders)
            selectedUnit.StopAnimation();

        location.y += .1f;
        starshine.transform.position = location;
        starshine.Play();

        if (selectedUnit.isMoving && !queueMovementOrders) //interrupt orders if new ones
        {
            selectedUnit.ShiftMovement();
            selectedUnit.FinishedMoving.RemoveAllListeners();
        }

        HandleSelectedLocation(locationInt, terrainPos, selectedUnit);
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
        else
        {
            world.GetWonder(unitLoc).AddWorker(selectedUnit.transform.position);
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
            else
            {
                tradeCenter = world.GetTradeCenter(tradeLoc);
                uiCityResourceInfoPanel.SetTitleInfo(tradeCenter.tradeCenterName, 10000, 10000); //not showing inventory levels
                uiCityResourceInfoPanel.HideInventoryLevel();
                uiCityResourceInfoPanel.ToggleVisibility(true, null, null, null, tradeCenter);
                uiCityResourceInfoPanel.SetPosition();
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
            ToggleOrderHighlights(false);
            ClearBuildRoad();
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

                if ((selectedWorker.removing || world.workerOrders) && world.IsRoadOnTerrain(tile))
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

                uiPersonalResourceInfoPanel.UpdateResourceInteractable(resourceType, selectedTrader.personalResourceManager.GetResourceDictValue(resourceType), false);
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
                cityResourceManager.CheckResource(resourceType, -resourceAmountAdjusted);
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
                resourceAmountAdjusted = cityResourceManager.CheckResource(resourceType, -resourceAmount);
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
            if (!selectedTrader.tradeRouteManager.TradeRouteCheck())
                return;
            selectedUnit.StopMovement();
            selectedTrader.BeginNextStepInRoute();
            uiTraderPanel.uiBeginTradeRoute.ToggleInteractable(false);
            uiTraderPanel.uiLoadUnload.ToggleInteractable(false);
            uiCancelTradeRoute.ToggleTweenVisibility(true);
            uiTradeRouteManager.ToggleVisibility(false);
            //if (uiTradeRouteManager.activeStatus)
            //{
            //    uiTradeRouteManager.PrepTradeRoute();
            //}
        }
    }

    public void CancelTradeRoute() //stop following route but still keep route description
    {
        selectedTrader.CancelRoute();
        selectedUnit.StopMovement();
        ShowIndividualCityButtonsUI();
        CancelContinuedMovementOrders();
        uiCancelTradeRoute.ToggleTweenVisibility(false);
        if (!selectedTrader.followingRoute/*.interruptedRoute*/)
            uiTraderPanel.uiBeginTradeRoute.ToggleInteractable(true);
        if (uiTradeRouteManager.activeStatus)
            uiTradeRouteManager.ToggleVisibility(false);
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

    public void ClearSelection()
    {
        //selectedTile = null;
        if (selectedUnit != null)
        {
            if (selectedUnit.isBusy && selectedWorker.IsOrderListMoreThanZero())
                ToggleOrderHighlights(false);

            SpeakingCheck();
            moveUnit = false;
            uiMoveUnit.ToggleTweenVisibility(false);
            uiCancelMove.ToggleTweenVisibility(false);
            uiJoinCity.ToggleTweenVisibility(false);

            if (selectedWorker != null)
                uiCancelTask.ToggleTweenVisibility(false);
            uiTraderPanel.uiBeginTradeRoute.ToggleInteractable(false);
            uiTraderPanel.ToggleVisibility(false);
            uiCancelTradeRoute.ToggleTweenVisibility(false);
            uiTradeRouteManager.ToggleVisibility(false);
            uiWorkerTask.ToggleVisibility(false);
            //if (selectedUnit != null)
            //{
            selectedUnit.Deselect();
            selectedUnit.HidePath();
            //}
            uiPersonalResourceInfoPanel.ToggleVisibility(false, selectedTrader);
            LoadUnloadFinish(false); //clear load cargo screen
            infoManager.HideInfoPanel();
            //movementSystem.ClearPaths(); //necessary to queue movement orders
            //selectedUnitInfoProvider = null;
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
