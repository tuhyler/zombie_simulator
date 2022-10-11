using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class UnitMovement : MonoBehaviour
{
    [SerializeField]
    private MapWorld world;

    [SerializeField]
    private MovementSystem movementSystem;

    [SerializeField]
    private UIUnitTurnHandler turnHandler;

    [SerializeField]
    private InfoManager infoManager;

    [SerializeField]
    private UISingleConditionalButtonHandler uiCancelMove; //button to cancel movement orders

    [SerializeField]
    private UISingleConditionalButtonHandler uiJoinCity; //button to join city

    [SerializeField]
    public UITraderOrderHandler uiTraderPanel;

    [SerializeField]
    private UIPersonalResourceInfoPanel uiPersonalResourceInfoPanel;

    [SerializeField]
    private UIPersonalResourceInfoPanel uiCityResourceInfoPanel;

    [SerializeField]
    private UITradeRouteManager uiTradeRouteManager;

    [SerializeField]
    private UISingleConditionalButtonHandler uiCancelTradeRoute;

    private bool queueMovementOrders;

    private Unit selectedUnit;
    private Trader selectedTrader;
    private TerrainData selectedTile;
    private InfoProvider selectedUnitInfoProvider;
    private bool loadScreenSet; //flag if load/unload ui is showing

    //for transferring cargo to/from trader
    private ResourceManager cityResourceManager; 
    public int cityTraderIncrement = 1;

    public void CenterCamOnUnit()
    {
        if (selectedUnit != null)
            selectedUnit.CenterCamera();
    }

    public void HandleUnitSelection(GameObject selectedObject)
    {
        //if nothing detected, nothing selected
        if (selectedObject == null)
        {
            selectedUnit = null;
            selectedTrader = null;
            return;
        }

        if (selectedObject.CompareTag("Player")) //to control for enemy units
        {
            Worker worker = selectedObject.GetComponent<Worker>();

            if (worker != null && worker.harvesting) //can't select unit if just harvested
                return;

            Unit unitReference = selectedObject.GetComponent<Unit>();
            if (CheckIfTheSameUnitSelected(unitReference) && unitReference != null)
            {
                ClearSelection();
                return;
            }

            else if (selectedUnit != null) //changing to a different unit
            {
                ClearSelection();
                selectedUnit = unitReference;
            }

            else //clicking a unit for the first time
            {
                selectedUnit = unitReference;
            }

        }
        else
        {
            selectedUnit = null;
            selectedTrader = null;
        }

        if (selectedUnit == null)
            return;

        SelectTrader();

        PrepareMovement();
    }

    private void SelectTrader()
    {
        if (selectedUnit.TryGetComponent(out selectedTrader))
        {
            uiPersonalResourceInfoPanel.PrepareResourceUI(selectedTrader.PersonalResourceManager.ResourceDict);
            uiPersonalResourceInfoPanel.ToggleVisibility(true);
            uiTraderPanel.ToggleVisibility(true);
            uiPersonalResourceInfoPanel.SetTitleInfo(selectedTrader.name,
                selectedTrader.PersonalResourceManager.GetResourceStorageLevel, selectedTrader.CargoStorageLimit);
            if (world.IsCityOnTile(Vector3Int.FloorToInt(selectedTrader.transform.position)))
            {
                uiTraderPanel.uiLoadUnload.ToggleInteractable(true);
            }
            else
            {
                uiTraderPanel.uiLoadUnload.ToggleInteractable(false);
            }

            if (selectedTrader.hasRoute && !selectedTrader.followingRoute)
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
        SelectTrader();
        PrepareMovement();
    }

    private void PrepareMovement()
    {
        Debug.Log("Sel. unit is " + selectedUnit.name);
        turnHandler.SetIndex(selectedUnit);
        selectedUnitInfoProvider = selectedUnit.GetComponent<InfoProvider>(); //getting the information to show in info panel
        infoManager.ShowInfoPanel(selectedUnitInfoProvider);
        if (selectedUnit.moreToMove)
        {
            uiCancelMove.ToggleTweenVisibility(true);
            movementSystem.ShowPathToMove(selectedUnit);
        }

        ShowIndividualCityButtonsUI();
        selectedUnit.Select();
    }

    public void HandleLocationSelection(Vector3 location, GameObject detectedObject)
    {
        if (uiCityResourceInfoPanel.inUse)
        {
            LoadUnloadFinish();
            return;
        }

        if (selectedUnit == null) //only select tile when unit is selected
            return;

        if (UnitSelected(detectedObject)) //don't run if you're selecting a unit 
            return;

        selectedUnit.DestinationLoc = location;

        Vector3Int terrainPos = world.GetClosestTile(location);
        TerrainData terrainSelected = world.GetTerrainDataAt(terrainPos);

        if (!terrainSelected.GetTerrainData().walkable) //cancel movement if terrain isn't walkable
        {
            Debug.Log("Not suitable location");
            return;
        }

        if (selectedTrader != null && selectedTrader.followingRoute)
        {
            Debug.Log("Currently following route");
            return;
        }

        if (selectedTrader != null && !terrainSelected.hasRoad)
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 10f; //z must be more than 0, else just gives camera position
            Vector3 mouseLoc = Camera.main.ScreenToWorldPoint(mousePos);

            InfoPopUpHandler.Create(mouseLoc, "Must travel on road.");

            Debug.Log("Trader must travel on road.");
            movementSystem.HidePath();
            movementSystem.ClearPaths();
            selectedTile = null;
            return;
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

        HandleSelectedTile(terrainSelected, terrainPos);
    }

    private bool UnitSelected(GameObject detectedObject)
    {
        return detectedObject.GetComponent<Unit>() != null;
    }

    private void HandleSelectedTile(TerrainData terrainSelected, Vector3Int terrainPos)
    {
        if (selectedTile == null || selectedTile != terrainSelected) //selecting a new or different tile
        {
            if (selectedTile != terrainSelected)
            {
                movementSystem.HidePath(); //Hides previous path if new tile is selected
                if (queueMovementOrders)
                {
                    movementSystem.AppendNewPath();
                }
            }
            selectedTile = terrainSelected; //sets selectedTile value
            bool isTrader = selectedTrader != null;
            movementSystem.GetPathToMove(world, selectedUnit, terrainPos, isTrader); //Call AStar movement
        }
        else //moving unit
        {
            if (movementSystem.CurrentPathLength == 0)
            {
                Debug.Log("No defined path.");
                return;
            }

            MovementPreparations();

            movementSystem.MoveUnit(selectedUnit, world);
            movementSystem.HidePath();
            movementSystem.ClearPaths();
            selectedTile = null;
        }
    }

    private void MovementPreparations()
    {
        turnHandler.ToggleInteractable(false);
        uiJoinCity.ToggleTweenVisibility(false);
    }

    public void HandleShiftDown()
    {
        queueMovementOrders = true;
    }

    public void HandleShiftUp()
    {
        queueMovementOrders = false;
    }

    public void CancelContinuedMovementOrders()
    {
        selectedUnit.ResetMovementOrders(); //deleting continued movement orders
    }

    public void JoinCity() //for Join City button
    {
        Vector3Int unitLoc = Vector3Int.FloorToInt(selectedUnit.transform.position);
        City joinedCity = world.GetCity(unitLoc);
        joinedCity.PopulationGrowthCheck();

        foreach (ResourceValue resourceValue in selectedUnit.GetBuildDataSO().unitCost) //adding back 100% of cost (if there's room)
        {
            joinedCity.ResourceManager.CheckResource(resourceValue.resourceType, resourceValue.resourceAmount);
        }

        selectedUnit.DestroyUnit();
        ClearSelection();
    }

    public void LoadUnloadPrep() //for loadunload button for traders
    {
        if (!loadScreenSet)
        {
            movementSystem.HidePath();
            movementSystem.ClearPaths();
            selectedTile = null;

            Vector3Int unitLoc = Vector3Int.FloorToInt(selectedUnit.transform.position);
            City selectedCity = world.GetCity(unitLoc);
            cityResourceManager = selectedCity.ResourceManager;

            uiPersonalResourceInfoPanel.SetPosition();

            uiCityResourceInfoPanel.SetTitleInfo(selectedCity.CityName,
                cityResourceManager.GetResourceStorageLevel, selectedCity.warehouseStorageLimit);
            uiCityResourceInfoPanel.PrepareResourceUI(cityResourceManager.ResourceDict);
            uiCityResourceInfoPanel.ToggleVisibility(true);
            uiCityResourceInfoPanel.SetPosition();
            loadScreenSet = true;
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

    public void LoadUnloadFinish() //putting the screens back after finishing loading cargo
    {
        if (loadScreenSet)
        {
            uiPersonalResourceInfoPanel.RestorePosition();
            uiCityResourceInfoPanel.RestorePosition();
            if (uiCityResourceInfoPanel.inUse)
                uiCityResourceInfoPanel.EmptyResourceUI();
            cityResourceManager = null;
            loadScreenSet = false;
        }
    }

    private void ChangeResourceManagersAndUIs(ResourceType resourceType, int resourceAmount)
    {
        if (resourceAmount > 0) //moving from city to trader
        {
            int remainingInCity = cityResourceManager.GetResourceDictValue(resourceType);

            if (remainingInCity < resourceAmount)
                resourceAmount = remainingInCity;

            int resourceAmountAdjusted = selectedTrader.PersonalResourceManager.CheckResource(resourceType, resourceAmount);
            cityResourceManager.CheckResource(resourceType, -resourceAmountAdjusted);
        }

        if (resourceAmount <= 0) //moving from trader to city
        {
            int remainingWithTrader = selectedTrader.PersonalResourceManager.GetResourceDictValue(resourceType);

            if (remainingWithTrader < Mathf.Abs(resourceAmount))
                resourceAmount = -remainingWithTrader;

            int resourceAmountAdjusted = cityResourceManager.CheckResource(resourceType, -resourceAmount);
            selectedTrader.PersonalResourceManager.CheckResource(resourceType, -resourceAmountAdjusted);
        }

        uiCityResourceInfoPanel.UpdateResource(resourceType, cityResourceManager.GetResourceDictValue(resourceType));
        uiCityResourceInfoPanel.UpdateStorageLevel(cityResourceManager.GetResourceStorageLevel);
        uiPersonalResourceInfoPanel.UpdateResource(resourceType, selectedTrader.PersonalResourceManager.GetResourceDictValue(resourceType));
        uiPersonalResourceInfoPanel.UpdateStorageLevel(selectedTrader.PersonalResourceManager.GetResourceStorageLevel);
    }

    public void SetUpTradeRoute()
    {
        if (selectedTrader == null)
            return;

        LoadUnloadFinish();
        infoManager.HideInfoPanel();
        
        Vector3Int traderLoc = Vector3Int.FloorToInt(selectedTrader.transform.position);

        List<string> cityNames = world.GetConnectedCityNames(traderLoc); //only showing city names accessible by unit
        uiTradeRouteManager.PrepareTradeRouteMenu(cityNames, selectedTrader);
        uiTradeRouteManager.ToggleVisibility(true);
        uiTradeRouteManager.LoadTraderRouteInfo(selectedTrader, world);
    }

    public void BeginTradeRoute() //start going trade route
    {
        if (selectedTrader != null)
        {
            selectedTrader.BeginNextStepInRoute();
        }

        ClearSelection();
        turnHandler.GoToNextUnit();
    }

    public void CancelTradeRoute() //stop following route but still keep route description
    {
        selectedTrader.CancelRoute();
        CancelContinuedMovementOrders();
    }

    private void ShowIndividualCityButtonsUI()
    {
        if (selectedUnit == null)
            return;

        if (world.IsCityOnTile(Vector3Int.FloorToInt(selectedUnit.transform.position)))
        {
            uiJoinCity.ToggleTweenVisibility(true);
            if (selectedTrader != null)
            {
                //uiLoadUnload.ToggleTweenVisibility(true);
                uiTraderPanel.uiLoadUnload.ToggleInteractable(true);
            }
        }
        else
        {
            uiJoinCity.ToggleTweenVisibility(false);
            //uiLoadUnload.ToggleTweenVisibility(false);
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

    private void ClearSelection()
    {
        selectedTile = null;
        uiCancelMove.ToggleTweenVisibility(false);
        uiJoinCity.ToggleTweenVisibility(false);
        uiTraderPanel.uiBeginTradeRoute.ToggleInteractable(false);
        uiTraderPanel.ToggleVisibility(false);
        uiCancelTradeRoute.ToggleTweenVisibility(false);
        uiTradeRouteManager.CloseWindow();
        if (selectedUnit != null)
            selectedUnit.Deselect();
        uiPersonalResourceInfoPanel.ToggleVisibility(false);
        LoadUnloadFinish(); //clear load cargo screen
        infoManager.HideInfoPanel();
        movementSystem.HidePath();
        movementSystem.ClearPaths(); //necessary to queue movement orders
        selectedUnitInfoProvider = null;
        selectedTrader = null;
        selectedUnit = null;
    }

    private bool CheckIfTheSameUnitSelected(Unit unitReference)
    {
        return selectedUnit == unitReference;
    }
}
