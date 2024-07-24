using Mono.Cecil;
using System;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;

public class CityBuilderManager : MonoBehaviour
{
    [SerializeField]
    public UIWonderSelection uiWonderSelection;
    [SerializeField]
    public UICityBuildTabHandler uiCityTabs;
    [SerializeField]
    public UIMarketPlaceManager uiMarketPlaceManager;
    [SerializeField]
    public UIBuilderHandler uiUnitBuilder;
    [SerializeField]
    public UIResourceManager uiResourceManager;
    [SerializeField]
    public UIBuilderHandler uiRawGoodsBuilder;
    [SerializeField]
    public UIBuilderHandler uiProducerBuilder;
    [SerializeField]
    public UIBuilderHandler uiBuildingBuilder;
    //[SerializeField]
    //public UIQueueManager uiQueueManager; //one in city, three in mapworld
    [SerializeField]
    public UIInfoPanelCity uiInfoPanelCity;
    [SerializeField]
    public UILaborAssignment uiLaborAssignment;
    [SerializeField]
    public UILaborHandler uiLaborHandler;
    [SerializeField]
    private UIImprovementBuildPanel uiImprovementBuildInfoPanel;
    [SerializeField]
    public UICityUpgradePanel uiCityUpgradePanel;
    [SerializeField]
    public UICityNamer uiCityNamer, uiTraderNamer;
    [SerializeField]
    public UITradeCenter uiTradeCenter;
    [HideInInspector]
    public UIHelperWindow uiHelperWindow;
    [SerializeField]
    public Button abandonCityButton;
    [SerializeField]
    public UIDestroyCityWarning uiDestroyCityWarning;
    [SerializeField]
    private Sprite upgradeButton, removeButton;

    //for labor prioritization auto-assignment menu
    [SerializeField]
    private Toggle autoAssign;
    [SerializeField]
    private Button openAssignmentPriorityMenu;
    //[SerializeField]
    //public UICityLaborPrioritizationManager uiLaborPrioritizationManager;

    [SerializeField]
    public MapWorld world;
    [SerializeField]
    public MovementSystem movementSystem;
    [SerializeField]
    public Transform objectPoolHolder, friendlyUnitHolder, enemyUnitHolder;

    [SerializeField]
    public CameraController focusCam;

    private City selectedCity;
    public City SelectedCity { get { return selectedCity; } }

    [HideInInspector]
    public HashSet<Vector3Int> tilesToChange = new();
    [HideInInspector]
    public HashSet<Vector3Int> cityTiles = new();
    private List<Vector3Int> developedTiles = new();
    [HideInInspector]
    public List<Vector3Int> constructingTiles = new();

    [HideInInspector]
    public ImprovementDataSO improvementData;

    //[HideInInspector]
    //public ResourceManager resourceManager;

    [HideInInspector]
    public int laborChange/*, placesToWork*/;

    //wonder management
    private Wonder selectedWonder;
    //[SerializeField]
    //private GameObject wonderHarbor;

    //trade center management
    private TradeCenter selectedTradeCenter;

	//[SerializeField]
	//private GameObject upgradeQueueGhost;

    //for object pooling of city borders
    private Queue<GameObject> borderQueue = new();
    private List<GameObject> borderList = new();

	//for making objects transparent
	[SerializeField]
    public Material transparentMat;

    //queue ghost tracker
    [HideInInspector]
    public Dictionary<QueueItem, GameObject> queueGhostDict = new();

    public bool removingImprovement, upgradingImprovement, placingWonderHarbor; //flags thrown when doing specific tasks
    [HideInInspector]
    public bool buildOptionsActive, isQueueing;
    [HideInInspector]
    public UIBuilderHandler activeBuilderHandler;

    [SerializeField]
    public Transform improvementHolder;
    private Dictionary<Vector3Int, (MeshFilter[], GameObject)> improvementMeshDict = new();
    private List<MeshFilter> improvementMeshList = new();

    [HideInInspector]
    public GameObject emptyGO; //used for combining meshes, not sure if is necessary

    [SerializeField]
    public AudioClip buildClip, closeClip, selectClip, removeClip, queueClip, checkClip, moveClip, pickUpClip, putDownClip, marchClip, coinsClip, ringClip, chimeClip, fireClip, smallTownClip, 
        largeTownClip, laborInClip, laborOutClip, constructionClip, trainingClip, thudClip, fieryOpen, popGainClip, popLoseClip, alertClip, warningClip, sunBeam, receiveGift, denyGift, fireworks;
    [SerializeField]
    private AudioClip[] acknowledgements;
    [HideInInspector]
    public AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.ignoreListenerPause = true;
        emptyGO = new GameObject("NewImprovement");
        emptyGO.SetActive(false);
        //GrowCityImprovementStatsPool();
        //GrowLaborNumbersPool();
        GrowBordersPool();
        //GrowImprovementResourcePool();
    }

    public void HandleB()
    {
        if (!uiCityNamer.activeStatus && selectedCity != null)
            OpenAddPopWindow();
    }

    public void CenterCamOnLoc(Vector3Int loc)
    {
		focusCam.CenterCameraNoFollow(loc);
	}

    public void CenterCamOnCity()
    {
        if (selectedCity != null)
            focusCam.CenterCameraNoFollow(selectedCity.transform.position);
        else if (selectedWonder != null)
            focusCam.CenterCameraNoFollow(selectedWonder.centerPos);
    }

    public void HandleCitySelection(Vector3 location, GameObject selectedObject)
    {
        if (world.moveUnit || world.unitOrders || world.buildingWonder)
            return;

        if (world.upgradingUnit)
        {
            world.upgradingUnit = false;
            return;
        }

        if (world.selectingUnit)
        {
            if (selectedCity || selectedWonder || selectedTradeCenter)
            {
				ResetCityUI();
				UnselectWonder();
				UnselectTradeCenter();
			}

            world.selectingUnit = false;
            return;
        }

        if (world.citySelected) //just so the city isn't selected when being chosen to move units (also works with wonders)
        {
            world.citySelected = false;
            return;
        }

        if (selectedObject == null)
            return;

		Vector3Int terrainLocation = world.GetClosestTerrainLoc(location);
        TerrainData td = selectedObject.GetComponent<TerrainData>();
        if (selectedObject.name == "RoadHolder")
            td = world.GetTerrainDataAt(terrainLocation);

        //selecting terrain of city
        if (world.IsCityOnTile(terrainLocation) && td != null)
        {
            SelectCity(location, world.GetCity(terrainLocation));
        }
		//else if (selectedObject.TryGetComponent(out City cityReference))
  //      {
  //          SelectCity(location, cityReference);
  //      }
        else if (world.WonderTileCheck(terrainLocation) && td != null)
        {
			SelectWonder(world.GetWonder(terrainLocation));
		}
        else if (selectedObject.TryGetComponent(out Wonder wonder))
        {
            SelectWonder(wonder);
        }
        else if (world.IsTradeCenterOnTile(terrainLocation) && td != null)
        {
            SelectTradeCenter(world.GetTradeCenter(terrainLocation));
        }
        else if (selectedObject.TryGetComponent(out TradeCenter center))
        {
            SelectTradeCenter(center);
        }
        else if (world.IsEnemyCityOnTile(terrainLocation) && td != null)
        {
			if (td.isDiscovered)
                SelectCity(location, world.GetEnemyCity(terrainLocation));
		}
        //selecting improvements to remove or add/remove labor
        else if (selectedObject.TryGetComponent(out CityImprovement improvementSelected) || world.TileHasCityImprovement(terrainLocation))
        {
            if (improvementData != null)
            {
				ResetCityUIToBase();
				return;
            }
            
            if (improvementSelected == null)
            {
                improvementSelected = world.GetCityDevelopment(terrainLocation);

                if (improvementSelected.isConstruction)
                {
					if (removingImprovement)
					{
						improvementSelected.RemoveConstruction();
						constructingTiles.Remove(terrainLocation);

						if (RemoveImprovement(terrainLocation, improvementSelected, false, selectedCity))
							PlaySelectAudio(removeClip);
					}

					return;
                }
            }
            
            if (improvementSelected.tag == "Enemy")
            {
				if (world.somethingSelected)
				{
					world.somethingSelected = false;
				}
                else if (selectedCity != null)
                {
					ResetCityUI();
				}
                else
                {
                    TerrainData tdHere = world.GetTerrainDataAt(terrainLocation);
                
                    if (tdHere.isDiscovered)
                        world.OpenTerrainTooltip(tdHere);
                }
	
                return;
            }
            
            bool isBarracks = false;
            if (improvementSelected.building && !removingImprovement && !upgradingImprovement)
            {
                City city = improvementSelected.city;
                if (city == null && laborChange == 0) //for orphan barracks or harbors
				{
                    world.CloseTooltip();
                    return;
                }

                SingleBuildType type = improvementSelected.GetImprovementData.singleBuildType;
				if (type == SingleBuildType.Barracks || type == SingleBuildType.Shipyard || type == SingleBuildType.AirBase)
                {
                    isBarracks = true;
                }
                else if (type == SingleBuildType.TradeDepot || type == SingleBuildType.Harbor || type == SingleBuildType.Airport)
                {
					if (improvementSelected.isUpgrading)
						return;

					if (selectedCity != null)
                    {
						if (selectedCity != improvementSelected.city)
                        {
                            ResetCityUI();
                        }
                        else if (uiCityTabs.openTab)
                        {
                            uiCityTabs.HideSelectedTab(false);
                            return;
                        }
                    }

					world.OpenImprovementTooltip(improvementSelected);
                    return;
				}
                else if (type == SingleBuildType.FinanceCenter || type == SingleBuildType.EntertainmentCenter)
                {
					if (improvementSelected.isUpgrading)
						return;

					if (laborChange != 0)
                    {
                        ChangeLaborCount(terrainLocation);
                        return;
                    }
                    else
                    {
                        world.OpenImprovementTooltip(improvementSelected);
                        return;
                    }
				}
                else if (city != null)
                {
                    SelectCity(city.cityLoc, city);
                    return;
                }
            }

            if (selectedCity != null)
            {
                //deselecting if choosing improvement outside of city
                if (!cityTiles.Contains(terrainLocation) && terrainLocation != selectedCity.cityLoc)
                {
                    ResetCityUI();
                    return;
                }

                //if not manipulating buildings, exit out
                if (improvementData == null && laborChange == 0 && !removingImprovement && !upgradingImprovement && !uiCityTabs.openTab)
                {
                    if (improvementSelected.isUpgrading)
                        return;

                    if (terrainLocation == selectedCity.cityLoc)
                        ResetCityUI();
                    ResetCityUIToBase();
                    if (isBarracks)
                        world.OpenCampTooltip(improvementSelected);
                    else
                        world.OpenImprovementTooltip(improvementSelected);
                    return;
                }

                if (upgradingImprovement)
                {
                    if (tilesToChange.Contains(terrainLocation) || (terrainLocation == selectedCity.cityLoc && improvementSelected.canBeUpgraded))
                        uiCityUpgradePanel.ToggleVisibility(true, selectedCity.resourceManager, improvementSelected);
                    else
                        UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Not here");
                }
                else if (removingImprovement)
                {
                    if (RemoveImprovement(terrainLocation, improvementSelected, false, selectedCity))
                        PlaySelectAudio(removeClip);
                }
                else if (laborChange != 0) //for changing labor counts in tile
                {
                    //if (improvementSelected.isConstruction || !tilesToChange.Contains(terrainLocation))
                    //{
                    //	ResetCityUIToBase();
                    //}
                    if (improvementSelected.isConstruction || improvementSelected.isUpgrading)
                    {
                        UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Still building...");
                        return;
                    }
                    else if (!tilesToChange.Contains(terrainLocation))
                    {
                        if (laborChange > 0 && selectedCity.unusedLabor == 0)
                            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "No more available labor");
                        else if (laborChange < 0 && !world.CheckIfTileIsWorked(terrainLocation))
                            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "No more labor to remove");
                        else
                            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Not here");
                    }
                    else
                    {
                        ChangeLaborCount(terrainLocation);
                    }
                }
                else if (uiCityTabs.openTab)
                {
                    uiCityTabs.HideSelectedTab(false);
                }
                else
                {
					if (improvementSelected.isUpgrading)
						return;

					if (isBarracks)
                        world.OpenCampTooltip(improvementSelected);
                    else
                        world.OpenImprovementTooltip(improvementSelected);
                }
            }
            else
            {
                if (world.somethingSelected)
                {
                    world.somethingSelected = false;
                }
                else
                {
                    if (improvementSelected.wonderHarbor)
                    {
                        if (selectedWonder != null)
							UnselectWonder();

						return;
                    }

					if (improvementSelected.isUpgrading)
						return;

					if (isBarracks)
                        world.OpenCampTooltip(improvementSelected);
                    else
                        world.OpenImprovementTooltip(improvementSelected);
                }
            }
        }
        //placing wonder harbor
        else if (selectedWonder != null && placingWonderHarbor && td != null) //for placing harbor
        {
            Vector3Int terrainLoc = td.TileCoordinates;

            if (!td.isDiscovered)
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Explore here first");
            else if (!tilesToChange.Contains(terrainLoc))
                UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't build here");
            else
                BuildWonderHarbor(terrainLoc);
        }
        //selecting terrain to show info
        else if (!world.unitOrders && selectedCity == null && selectedWonder == null && selectedTradeCenter == null && td != null)
        {
            if (world.somethingSelected)
            {
                world.somethingSelected = false;
            }
            else
            {
                if (td.isDiscovered)
                    world.OpenTerrainTooltip(td);
            }
        }
        //selecting tiles to place improvements
        else if (selectedCity != null && td != null)
        {
            Vector3Int terrainLoc = td.TileCoordinates;

            if (!tilesToChange.Contains(terrainLoc))
            {
				if (uiCityTabs.openTab)
					uiCityTabs.HideSelectedTab(false);
				else if (upgradingImprovement || removingImprovement || laborChange != 0 || improvementData != null)
                    ResetCityUIToBase();
                else
                    ResetCityUI();
            }
            else 
            {
                if (improvementData != null)
                {
                    if (td.beingCleared)
                    {
						UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Forest being cleared");
						return;
                    }

                    if (td.hasBattle)
                    {
						UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Battle is here");
						return;
					}

                    if (improvementData.singleBuildType == SingleBuildType.Barracks || improvementData.singleBuildType == SingleBuildType.Shipyard || 
                        improvementData.singleBuildType == SingleBuildType.AirBase)
                    {
                        foreach (Vector3Int tile in world.GetNeighborsFor(terrainLoc, MapWorld.State.EIGHTWAYINCREMENT))
                        {
                            if (world.GetTerrainDataAt(tile).enemyZone && world.CompletedImprovementCheck(tile))
                            {
                                SingleBuildType improvementType = world.GetCityDevelopment(tile).GetImprovementData.singleBuildType;
                                if (improvementType == SingleBuildType.Barracks || improvementType == SingleBuildType.Shipyard || improvementType == SingleBuildType.AirBase)
                                {
                                    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Too close to enemy military base");
								    return;
                                }
                            }
                        }
                    }

                    //check for affordability
                    for (int i = 0; i < improvementData.improvementCost.Count; i++)
                    {
                        if (improvementData.improvementCost[i].resourceType == ResourceType.Gold)
                        {
                            if (!world.CheckWorldGold(improvementData.improvementCost[i].resourceAmount))
                            {
								UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't afford");
								ResetCityUIToBase();
								return;
							}
                        }
                        else
                        {
                            if (selectedCity.resourceManager.resourceDict[improvementData.improvementCost[i].resourceType] < improvementData.improvementCost[i].resourceAmount)
                            {
							    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't afford");
                                ResetCityUIToBase();
							    return;
						    }
                        }
                    }

                    BuildImprovementQueueCheck(improvementData, terrainLoc); //passing the data here as method requires it

                    world.TutorialCheck("Build Something");
                }
                else if (upgradingImprovement)
                {
					if (td.hasBattle)
					{
						UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Battle is here");
						return;
					}

					CityImprovement improvement = world.GetCityDevelopment(terrainLoc);
                    uiCityUpgradePanel.ToggleVisibility(true, selectedCity.resourceManager, improvement);
                    PlaySelectAudio(constructionClip);
				}
				else if (removingImprovement)
                {
					if (td.inBattle)
					{
						UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Battle is here");
						return;
					}

					if (world.TileHasCityImprovement(terrainLoc))
                    {
                        CityImprovement improvement = world.GetCityDevelopment(terrainLoc);

                        if (improvement.isConstruction)
                        {
                            improvement.RemoveConstruction();
							constructingTiles.Remove(terrainLoc);
						}

					    if (RemoveImprovement(terrainLoc, improvement, false, selectedCity))
                            PlaySelectAudio(removeClip);
                    } 
                }
                else if (laborChange != 0)
                {
					if (td.inBattle)
					{
						UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Battle is here");
						return;
					}

					if (world.TileHasCityImprovement(terrainLoc))
                    {
						if (world.GetCityDevelopment(terrainLoc).isConstruction)
                        {
                            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Still building...");
                            return;
                        }
    
                        ChangeLaborCount(terrainLoc);
                    }
                }
            }
        }
        //here to prevent city window from closing when upgrading units
  //      else if (world.upgradingUnit && selectedObject.TryGetComponent(out Unit unit) && world.unitMovement.highlightedUnitList.Contains(unit)) 
  //      {

		//}
        else
        {
            ResetCityUI();
            UnselectWonder();
            UnselectTradeCenter();
        }
    }

    private void SelectWonder(Wonder wonderReference)
    {
        if (selectedWonder != null)
        {
            if (selectedWonder != wonderReference)
            {
                UnselectWonder();
            }
            else
            {
                UnselectWonder();
                return; //deselect if same wonder selected
            }
        }

        ResetCityUI();
        UnselectTradeCenter();

        uiWonderSelection.ToggleVisibility(true, wonderReference);
        selectedWonder = wonderReference;
        selectedWonder.isActive = true;
        selectedWonder.TimeProgressBarSetActive(true);
        selectedWonder.EnableHighlight(Color.white, false);
        CenterCamOnCity();
    }

    private void SelectTradeCenter(TradeCenter center)
    {
        if (selectedTradeCenter != null)
        {
            if (selectedTradeCenter != center)
            {
                UnselectTradeCenter();
            }
            else
            {
                UnselectTradeCenter();
                return;
            }
        }

        ResetCityUI();
        UnselectWonder();

        uiTradeCenter.ToggleVisibility(true, center);
        selectedTradeCenter = center;
        selectedTradeCenter.EnableHighlight(new Color(0.5f, 0, 1), false);
        uiTradeCenter.SetName(selectedTradeCenter.tradeCenterDisplayName);
        CenterCamOnCity();
    }

    public void BuildHarbor()
    {
        if (selectedWonder.singleBuildDict.ContainsKey(SingleBuildType.Harbor))
        {
            PlaySelectAudio(removeClip);
            selectedWonder.DestroyHarbor();
            uiWonderSelection.UpdateHarborButton(false);
            return;
        }
        
        if (!uiWonderSelection.buttonsAreWorking)
            return;

        PlaySelectAudio();
        tilesToChange.Clear();

        foreach (Vector3Int loc in selectedWonder.possibleHarborLocs)
        {
            if (!world.IsTileOpenCheck(loc))
                continue;

            if (world.CheckIfEnemyTerritory(loc))
                continue;

            TerrainData td = world.GetTerrainDataAt(loc);

            td.EnableHighlight(Color.white);
            tilesToChange.Add(loc);
        }

        if (tilesToChange.Count == 0)
        {
            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "No available spots");
        }
        else
        {
            placingWonderHarbor = true;
            uiImprovementBuildInfoPanel.SetText("Building Harbor");
            uiImprovementBuildInfoPanel.ToggleVisibility(true);
        }
    }

    public void CreateWorkerButton()
    {
        if (!uiWonderSelection.buttonsAreWorking)
            return;

        if (selectedWonder.workersReceived == 0)
        {
            UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "No workers to unload");
            return;
        }

        PlaySelectAudio();

        selectedWonder.StopConstructing();
        selectedWonder.workersReceived--; //decrease worker count 
        bool secondary = selectedWonder.workerSexAndHome[0].Item1;
        Vector3Int homeCityLoc = selectedWonder.workerSexAndHome[0].Item2;
        selectedWonder.workerSexAndHome.RemoveAt(0);

        if (uiWonderSelection.activeStatus)
            uiWonderSelection.UpdateUIWorkers(selectedWonder.workersReceived, selectedWonder);

        Vector3Int buildPosition = selectedWonder.unloadLoc;
        selectedWonder.GoldWaitCheck();

		List<Vector3Int> pathHome = GridSearch.TraderMove(world, transform.position, homeCityLoc, false);
        if (pathHome.Count > 0)
            pathHome.RemoveAt(0);

		TransferWorker(secondary, homeCityLoc, buildPosition, true, pathHome, false);
    }

    public void TransferWorker(bool secondary, Vector3Int destination, Vector3Int buildPosition, bool wonder, List<Vector3Int> transferPath, bool atSea)
    {
		GameObject workerGO;

		if (secondary)
			workerGO = Resources.Load<GameObject>("Prefabs/" + world.laborerData.secondaryPrefabLoc);
		else
			workerGO = Resources.Load<GameObject>("Prefabs/" + world.laborerData.prefabLoc);

		GameObject unit = Instantiate(workerGO, buildPosition, Quaternion.identity); //produce unit at specified position
		unit.transform.SetParent(friendlyUnitHolder, false);
		//for tweening
		Vector3 goScale = unit.transform.localScale;
        float scaleX = goScale.x;
        float scaleZ = goScale.z;
        unit.transform.localScale = new Vector3(scaleX, 0.1f, scaleZ);
        LeanTween.scale(unit, goScale, 0.5f).setEase(LeanTweenType.easeOutBack);

		Unit newUnit = unit.GetComponent<Unit>();
		newUnit.SetReferences(world);
        newUnit.laborer.secondary = secondary;
		world.laborerCount++;
		unit.name = "Laborer " + world.laborerCount;
		newUnit.SetMinimapIcon(friendlyUnitHolder);
		newUnit.PlayAudioClip(buildClip);

        newUnit.laborer.atSea = atSea;
		world.laborerList.Add(newUnit.laborer);
        if (wonder)
			newUnit.laborer.homeCityLoc = destination;
        else
			newUnit.laborer.homeCityLoc = selectedCity.cityLoc;

        if (transferPath.Count == 0)
            transferPath.Add(destination);
		newUnit.laborer.Transfer(transferPath, atSea);
	}

    public void CreateAllWorkers(Wonder wonder)
    {
        wonder.StopConstructing();
        wonder.workersReceived = 0; //decrease worker count 

        if (uiWonderSelection.activeStatus && uiWonderSelection.wonder == wonder)
            uiWonderSelection.UpdateUIWorkers(0, wonder);

        List<Vector3> locs = wonder.OuterRim();

        List<Vector3> tempLocs = new(locs);
        for (int i = 0; i < tempLocs.Count; i++)
        {
            if (!world.PlayerCheckIfPositionIsValid(world.RoundToInt(tempLocs[i])))
                locs.Remove(tempLocs[i]);
		}

        if (locs.Count == 0) //theoretically not possible
        {
			InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(wonder.unloadLoc, "Lost all workers due to no available space");
            return;
		}

        for (int i = 0; i < wonder.workerSexAndHome.Count; i++)
        {
            GameObject workerGO;
            
            if (wonder.workerSexAndHome[i].Item1)
                workerGO = Resources.Load<GameObject>("Prefabs/" + world.laborerData.secondaryPrefabLoc);
            else
				workerGO = Resources.Load<GameObject>("Prefabs/" + world.laborerData.prefabLoc);

            int place = i % locs.Count;

            GameObject unit = Instantiate(workerGO, locs[place], Quaternion.identity); //produce unit at specified position
            unit.transform.SetParent(friendlyUnitHolder, false);
            unit.transform.rotation = Quaternion.LookRotation(wonder.centerPos - unit.transform.position);
            Unit newUnit = unit.GetComponent<Unit>();
            //newUnit.marker.ToggleVisibility(true);
            world.laborerList.Add(newUnit.laborer);
			newUnit.laborer.StartLaborAnimations(false, wonder.workerSexAndHome[i].Item2);
                    
            newUnit.SetReferences(world);
			world.laborerCount++;
			unit.name = "Laborer " + world.laborerCount;
			world.laborerList.Add(newUnit.laborer);
			newUnit.SetMinimapIcon(friendlyUnitHolder);
		}

        wonder.workerSexAndHome.Clear();
    }

    public void BuildWonderHarbor(Vector3Int loc)
    {
        PlaySelectAudio(buildClip);
        GameObject harborGO = Instantiate(Resources.Load<GameObject>("Prefabs/ImprovementPrefabs/Harbor01South"), loc, Quaternion.Euler(0, HarborRotation(loc, selectedWonder.unloadLoc), 0));
        //for tweening
        Vector3 goScale = harborGO.transform.localScale;
        harborGO.transform.SetParent(world.wonderHolder, false);
        CityImprovement harbor = harborGO.GetComponent<CityImprovement>();
        harbor.SetWorld(world);
        harbor.wonderHarbor = true;
        harbor.PlaySmokeSplash(false);
        selectedWonder.harborImprovement = harbor;
        harborGO.transform.localScale = Vector3.zero;
        LeanTween.scale(harborGO, goScale, 0.25f).setEase(LeanTweenType.easeOutBack);
        selectedWonder.singleBuildDict[SingleBuildType.Harbor] = loc;

        world.AddToCityLabor(loc, null);
        world.AddStructure(loc, harborGO);
        world.AddStop(loc, selectedWonder);
        uiWonderSelection.UpdateHarborButton(true);

        CloseImprovementBuildPanel();
    }

    public void LoadWonderHarbor(Vector3Int loc, Wonder wonder)
    {
		GameObject harborGO = Instantiate(Resources.Load<GameObject>("Prefabs/ImprovementPrefabs/Harbor01South"), loc, Quaternion.Euler(0, HarborRotation(loc, wonder.unloadLoc), 0));
        harborGO.transform.SetParent(world.wonderHolder, false);
		CityImprovement harbor = harborGO.GetComponent<CityImprovement>();
        harbor.SetWorld(world);
		harbor.wonderHarbor = true;
		wonder.harborImprovement = harbor;

		world.AddToCityLabor(loc, null);
        world.AddStop(loc, wonder);
		world.AddStructure(loc, harborGO);
	}

    public void OpenCancelWonderConstructionWarning()
    {
        if (uiWonderSelection.buttonsAreWorking)
        {
            PlaySelectAudio();
            uiDestroyCityWarning.ToggleVisibility(true);
        }
    }

    private void CancelWonderConstruction()
    {
        selectedWonder.PlayRemoveEffect();
		PlaySelectAudio(removeClip);
		uiDestroyCityWarning.ToggleVisibility(false);
        uiWonderSelection.ToggleVisibility(false, selectedWonder);
        selectedWonder.StopConstructing();

        CreateAllWorkers(selectedWonder);
		selectedWonder.GoldWaitCheck();

		if (!selectedWonder.hadRoad)
            world.roadManager.RemoveRoadAtPosition(selectedWonder.unloadLoc);

        selectedWonder.RemoveQueuedTraders();

        if (selectedWonder.singleBuildDict.ContainsKey(SingleBuildType.Harbor))
            selectedWonder.DestroyHarbor();

        world.RemoveStop(selectedWonder.unloadLoc);
        world.RemoveStopName(selectedWonder.wonderName);

        GameObject priorGO = world.GetStructure(selectedWonder.unloadLoc);
        Destroy(priorGO);

        //for no walk zone
        int k = 0;
        int[] xArray = new int[selectedWonder.wonderLocs.Count];
        int[] zArray = new int[selectedWonder.wonderLocs.Count];

        foreach (Vector3Int tile in selectedWonder.wonderLocs)
        {
            world.RemoveStructure(tile);
            world.RemoveSingleBuildFromCityLabor(tile);
            world.RemoveWonderTiles(tile);

            TerrainData td = world.GetTerrainDataAt(tile);

            if (selectedWonder.wonderData.isSea)
            {
                td.sailable = true;
                td.canWalk = false;
                td.canPlayerWalk = false;
                td.walkable = false;
            }
			
            if (td.prop != null && td.resourceAmount > 0)
                td.ShowProp(true);

            td.ToggleTerrainMesh(true);
            if (td.hasResourceMap)
                td.RestoreResourceMap();

            xArray[k] = tile.x;
            zArray[k] = tile.z;
            k++;
        }

        int xMin = Mathf.Min(xArray) - 1;
        int xMax = Mathf.Max(xArray) + 1;
        int zMin = Mathf.Min(zArray) - 1;
        int zMax = Mathf.Max(zArray) + 1;

        foreach (Vector3Int tile in selectedWonder.wonderLocs)
        {
            world.RemoveFromNoWalkList(tile);
            
            foreach (Vector3Int neighbor in world.GetNeighborsFor(tile, MapWorld.State.EIGHTWAY))
            {
                if (neighbor.x == xMin || neighbor.x == xMax || neighbor.z == zMin || neighbor.z == zMax)
                    continue;

                world.RemoveFromNoWalkList(neighbor);
            }
        }

        foreach (Vector3Int tile in selectedWonder.coastTiles)
            world.RemoveFromCoastList(tile);

        world.allWonders.Remove(selectedWonder);
        selectedWonder = null;
    }

    public void PlaySelectAudio()
    {
        audioSource.PlayOneShot(selectClip);
    }

    public void PlayCloseAudio()
    {
        audioSource.PlayOneShot(closeClip);
    }

    public void PlaySelectAudio(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
    }

    public void PlayOpenCityAudio()
    {
        if (selectedCity.currentPop < 5)
            world.PlayCityAudio(fireClip);
        else if (selectedCity.currentPop < 12)
			world.PlayCityAudio(smallTownClip);
        else
			world.PlayCityAudio(largeTownClip);
    }

	public void MoveUnitAudio()
	{
		//audioSource.clip = acknowledgements[UnityEngine.Random.Range(0, acknowledgements.Length)];
		audioSource.PlayOneShot(acknowledgements[UnityEngine.Random.Range(0, acknowledgements.Length)]);
	}

    public void StopAudio()
    {
        audioSource.Stop();
    }

    public void OpenAddPopWindow()
    {
        if (world.uiCityPopIncreasePanel.activeStatus)
        {
    		world.uiCityPopIncreasePanel.ToggleVisibility(false);
        }
        else
        {
		    CloseLaborMenus();
		    CloseImprovementTooltipButton();
		    CloseImprovementBuildPanel();
		    CloseSingleWindows();
		    uiResourceManager.ToggleOverflowVisibility(false);
		    world.uiCityPopIncreasePanel.ToggleVisibility(false);
		    uiCityTabs.HideSelectedTab(false);
			
            world.uiCityPopIncreasePanel.ToggleVisibility(true, 1, selectedCity);
        }
	}

    public void CloseAddPopWindow()
    {
        PlayCloseAudio();
        world.uiCityPopIncreasePanel.ToggleVisibility(false);
    }

    public bool CityTypingCheck()
    {
        return selectedCity != null && !uiMarketPlaceManager.isTyping && !uiCityNamer.activeStatus;
    }

	private void SelectCity(Vector3 location, City cityReference)
    {
        if (selectedCity != null)
        {
            if (selectedCity != cityReference)
            {
                ResetCityUI();
            }
            else
            {
                ResetCityUI();
                return; //deselect if same city selected
            }
        }

        UnselectWonder();
        UnselectTradeCenter();

        selectedCity = cityReference;

		if (cityReference.gameObject.tag == "Enemy")
        {
			if (!world.GetTerrainDataAt(world.RoundToInt(location)).isDiscovered)
            {
                world.somethingSelected = false;
                selectedCity = null;
                return;
            }

            selectedCity.activeCity = true;
            ToggleEnemyBuildingHighlight(selectedCity.cityLoc, Color.red);
            world.somethingSelected = false;
            world.GetTerrainDataAt(selectedCity.cityLoc).EnableHighlight(Color.red);

			world.cityCanvas.gameObject.SetActive(true);
			uiInfoPanelCity.ToggleVisibility(true, true);
			uiInfoPanelCity.SetAllData(selectedCity);
			uiInfoPanelCity.SetWorkEthicPopUpCity(selectedCity);
			uiInfoPanelCity.UpdateWater(selectedCity.waterCount);
            uiInfoPanelCity.UpdatePower(selectedCity.powerCount);
			return;
        }

        world.TutorialCheck("Open City");
        if (selectedCity.lostPop > 0)
        {
			Vector3 middle = new Vector3(Screen.width / 2, Screen.height / 2, 0);
			UIInfoPopUpHandler.WarningMessage().Create(middle, "Lost " + selectedCity.lostPop + " pop since last visit");
            selectedCity.lostPop = 0;
		}

        if (selectedCity.resourceManager.growthDeclineDanger)
            uiInfoPanelCity.TogglewWarning(true);
        else
            uiInfoPanelCity.TogglewWarning(false);
        PlayOpenCityAudio();

        world.cityCanvas.gameObject.SetActive(true);
        world.somethingSelected = false;
        selectedCity.activeCity = true;
        (cityTiles, developedTiles, constructingTiles) = world.GetCityRadiusFor(selectedCity.cityLoc);
        focusCam.SetCityLimit(cityTiles, selectedCity.cityLoc);
        ResourceProducerTimeProgressBarsSetActive(true);
        ToggleBuildingHighlight(true, selectedCity.cityLoc);
        world.GetTerrainDataAt(selectedCity.cityLoc).EnableHighlight(Color.green);
        DrawBorders();
        autoAssign.isOn = selectedCity.autoAssignLabor;
		//resourceManager = selectedCity.resourceManager;
        selectedCity.resourceManager.UpdateUI(selectedCity.GetResourceValues());
        uiCityTabs.ToggleVisibility(true, selectedCity.resourceManager);
        uiResourceManager.ToggleVisibility(true, selectedCity);
        CenterCamOnCity();
        uiInfoPanelCity.SetAllData(selectedCity);
        uiInfoPanelCity.SetWorkEthicPopUpCity(selectedCity);
        uiInfoPanelCity.UpdateWater(selectedCity.waterCount);
		uiInfoPanelCity.UpdatePower(selectedCity.powerCount);

		if (world.scottFollow && selectedCity.growing)
        {
            uiLaborAssignment.showPrioritiesButton.SetActive(selectedCity.autoAssignLabor);
            uiLaborAssignment.ShowUI();
        }

        uiInfoPanelCity.ToggleVisibility(true);

        //uiLaborHandler.SetCity(selectedCity);
        if (selectedCity.growing)
        {
            selectedCity.CityGrowthProgressBarSetActive(true);
            abandonCityButton.interactable = false;
        }
        else
        {
			selectedCity.CityGrowthProgressBarSetActive(false);
			abandonCityButton.interactable = true;
        }

        UpdateLaborNumbers(true);
    }

    public void ToggleCityGrowthPause(bool v)
    {
        PlaySelectAudio(checkClip);
        selectedCity.resourceManager.pauseGrowth = v;
    }

    public void DestroyQueuedGhost()
    {
        foreach (QueueItem item in queueGhostDict.Keys)
        {
            Destroy(queueGhostDict[item]);
        }

        queueGhostDict.Clear();
    }

    public void CreateQueuedGhost(QueueItem item, ImprovementDataSO improvementData, Vector3Int loc, bool isBuilding)
    {
        Color newColor = new(0, 1f, 0, 0.8f);
        Vector3 newLoc = loc;

        if (isBuilding)
            newLoc += improvementData.buildingLocation;
        else if (improvementData.replaceTerrain)
            newLoc.y += .1f;

        GameObject improvementGhost = Instantiate(Resources.Load<GameObject>("Prefabs/" + improvementData.prefabLoc), newLoc, Quaternion.identity);
		improvementGhost.layer = LayerMask.NameToLayer("Text");
		//for tweening
		Vector3 goScale = improvementGhost.transform.localScale;
        improvementGhost.transform.localScale = Vector3.zero;
        LeanTween.scale(improvementGhost, goScale, 0.25f).setEase(LeanTweenType.easeOutBack);
        MeshRenderer[] renderers = improvementGhost.GetComponentsInChildren<MeshRenderer>();
        SkinnedMeshRenderer[] skinnedRenderers = improvementGhost.GetComponentsInChildren<SkinnedMeshRenderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
			Material[] newMats = renderers[i].materials;

			for (int j = 0; j < newMats.Length; j++)
			{
				Material newMat = new(transparentMat);
				newMat.color = newColor;
				newMat.SetTexture("_BaseMap", newMats[j].mainTexture);
				newMats[j] = newMat;
			}

			renderers[i].materials = newMats;
		}

		for (int i = 0; i < skinnedRenderers.Length; i++)
		{
            skinnedRenderers[i].gameObject.SetActive(false);
		}

        queueGhostDict[item] = improvementGhost;
    }

    public void CreateQueuedArrow(QueueItem item, ImprovementDataSO improvementData, Vector3Int tempBuildLocation, bool isBuilding)
    {
        //setting up ghost
        GameObject arrowGhost = Instantiate(Resources.Load<GameObject>("Prefabs/ImprovementPrefabs/ArrowUp"), tempBuildLocation, Quaternion.Euler(0, 90f, 0));
        arrowGhost.layer = LayerMask.NameToLayer("Text");
        //for tweening
        Vector3 goScale = arrowGhost.transform.localScale;
        arrowGhost.transform.localScale = Vector3.zero;
        LeanTween.scale(arrowGhost, goScale, 0.25f).setEase(LeanTweenType.easeOutBack);
        if (isBuilding)
        {
            arrowGhost.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            Vector3 newLoc = tempBuildLocation + improvementData.buildingLocation;
            newLoc.y += .5f;
            arrowGhost.transform.position = newLoc;
        }

        queueGhostDict[item] = arrowGhost;
    }

    public void RemoveQueueGhostImprovement(QueueItem item)
    {
        GameObject go = queueGhostDict[item];
        Destroy(go);
    }

    //public void RemoveQueueGhostBuilding(string building, City city)
    //{
    //    QueueItem item = city.buildingQueueGhostDict[building];
    //    if (city.activeCity)
    //        Destroy(queueGhostDict[item]);
    //    //Destroy(go);
    //    //city.buildingQueueGhostDict.Remove(building);
    //}

    public void SellResources()
    {
        uiMarketPlaceManager.ToggleVisibility(true, selectedCity);
    }

    public void CloseSellResources()
    {
        PlayCloseAudio();
        uiMarketPlaceManager.ToggleVisibility(false);
        uiCityTabs.CloseSelectedTab();
    }

    public void RemoveImprovements()
    {
        laborChange = 0;
        
        removingImprovement = true;
        world.uiCityPopIncreasePanel.ToggleVisibility(false);
        CloseQueueUI();
        ToggleBuildingHighlight(true, selectedCity.cityLoc);
        ImprovementTileHighlight(true);
    }

    public void UpgradeImprovements()
    {
        upgradingImprovement = true;
        world.unitMovement.ToggleUnitHighlights(true, selectedCity);
        ToggleBuildingHighlight(true, selectedCity.cityLoc);
        UpgradeTileHighlight();
    }

    private void UpgradeTileHighlight()
    {
        tilesToChange.Clear();

        uiImprovementBuildInfoPanel.SetText("Upgrading Items");
        uiImprovementBuildInfoPanel.SetImage(upgradeButton, false);
        uiImprovementBuildInfoPanel.ToggleVisibility(true);

        foreach (Vector3Int tile in developedTiles)
        {
            if (isQueueing && world.CheckQueueLocation(tile))
                continue;
            
            TerrainData td = world.GetTerrainDataAt(tile);
            td.DisableHighlight(); //turning off all highlights first

            CityImprovement improvement = world.GetCityDevelopment(tile); 
            improvement.DisableHighlight();

            if (improvement.GetImprovementData.improvementLevel < world.GetUpgradeableObjectMaxLevel(improvement.GetImprovementData.improvementName) && !improvement.isUpgrading && !td.hasBattle)
            {
                td.EnableHighlight(Color.green);
                improvement.EnableHighlight(Color.green);
                tilesToChange.Add(tile);
            }
        }
    }

    public void UpgradeUnit(Unit unit, List<ResourceValue> upgradeCost, List<ResourceValue> refundCost) //can't queue, don't need to pass in city
    {
        //PlayBoomAudio();
        unit.isUpgrading = true;
        string name = unit.buildDataSO.unitType.ToString();
        if (unit == world.azai)
            name = "Azai";
        unit.upgradeLevel = world.GetUpgradeableObjectMaxLevel(name);
		string upgradeNameAndLevel = name + "-" + unit.upgradeLevel;
        bool refund = true;

        if (unit.buildDataSO.singleBuildType != SingleBuildType.None)
        {
            refund = false;
            CityImprovement improvement = world.GetCityDevelopment(selectedCity.singleBuildDict[unit.buildDataSO.singleBuildType]);
			improvement.upgradeCost = upgradeCost;
			improvement.refundCost = refundCost;
		}
	
		selectedCity.resourceManager.SpendResource(upgradeCost, unit.transform.position, refund, refundCost);
        UnitBuildDataSO data = UpgradeableObjectHolder.Instance.unitDict[upgradeNameAndLevel];

        world.unitMovement.ToggleUnitHighlights(false);
		world.unitMovement.ToggleUnitHighlights(true, selectedCity);
		CreateUnit(data, selectedCity, true, unit);
	}

    public void UpgradeSelectedImprovementQueueCheck(Vector3Int tempBuildLocation, CityImprovement selectedImprovement, List<ResourceValue> upgradeCost, List<ResourceValue> refundCost)
    {
        //string nameAndLevel = selectedImprovement.GetImprovementData.improvementNameAndLevel;

        //queue information
        //if (isQueueing)
        //{
        //    tilesToChange.Remove(tempBuildLocation);
        //    world.GetCityDevelopment(tempBuildLocation).DisableHighlight();
        //    world.GetTerrainDataAt(tempBuildLocation).DisableHighlight();
        //    selectedImprovement.queued = true;
        //    selectedImprovement.SetQueueCity(selectedCity);
        //    uiQueueManager.upgradeCosts = upgradeCost;
        //    if (!selectedCity.AddToQueue(UpgradeableObjectHolder.Instance.improvementDict[nameAndLevel], tempBuildLocation, tempBuildLocation - selectedCity.cityLoc, true))  
        //        return; //checks if queue item already exists
        //    return;
        //}

        if (selectedImprovement.building)
        {
			PlaySelectAudio(buildClip);
		}
        else
        {
            PlaySelectAudio(constructionClip);
		    tilesToChange.Remove(tempBuildLocation);
            world.GetTerrainDataAt(tempBuildLocation).DisableHighlight();
        }
		    //PlayConstructionAudio();
        UpgradeSelectedImprovementPrep(tempBuildLocation, selectedImprovement, selectedCity, upgradeCost, refundCost);
    }

    private void UpgradeSelectedImprovementPrep(Vector3Int upgradeLoc, CityImprovement selectedImprovement, City city, List<ResourceValue> upgradeCost, List<ResourceValue> refundCost)
    {
        if (city.activeCity)
            selectedImprovement.DisableHighlight();

        bool cityCenterBuilding = upgradeLoc == city.cityLoc;
        string name = selectedImprovement.GetImprovementData.improvementName;
        selectedImprovement.upgradeLevel = world.GetUpgradeableObjectMaxLevel(name);
        string upgradeNameAndLevel = name + "-" + selectedImprovement.upgradeLevel;
        city.resourceManager.SpendResource(upgradeCost, upgradeLoc, cityCenterBuilding, refundCost);
        ImprovementDataSO data = UpgradeableObjectHolder.Instance.improvementDict[upgradeNameAndLevel];

        if (cityCenterBuilding)
        {
            RemoveImprovement(upgradeLoc, selectedImprovement, true, city);
            CreateBuilding(data, city, true);
        }
        else
        {
            selectedImprovement.upgradeCost = upgradeCost;
            selectedImprovement.refundCost = refundCost;

            //putting the labor back
            ResourceProducer resourceProducer = selectedImprovement.resourceProducer;
            int currentLabor = resourceProducer.currentLabor;
            resourceProducer.StopProducing(true);
            ResetProducerLabor(city, upgradeLoc, resourceProducer);
            resourceProducer.UpdateCurrentLaborData(0);

            ResourceType resourceType = resourceProducer.producedResource.resourceType;

			if (resourceType != ResourceType.None)
            {
				int totalResourceLabor = city.ChangeResourcesWorked(resourceType, -currentLabor);
                if (city.activeCity && uiLaborHandler.activeStatus)
                {
                    for (int i = 0; i < currentLabor; i++)
                        uiLaborHandler.PlusMinusOneLabor(resourceType, totalResourceLabor, -1, city.resourceManager.GetResourceGenerationValues(resourceType));
                }
                if (totalResourceLabor == 0)
                    city.RemoveFromResourcesWorked(resourceType);
            }
            
            if (city.autoAssignLabor && city.unusedLabor > 0)
                city.AutoAssignmentsForLabor();

            selectedImprovement.BeginImprovementUpgradeProcess(city, resourceProducer, data, false);
        }
    }

    public void UpgradeSelectedImprovement(Vector3Int upgradeLoc, CityImprovement selectedImprovement, City city, ImprovementDataSO data)
    {
        RemoveImprovement(upgradeLoc, selectedImprovement, true, city);
        BuildImprovement(data, upgradeLoc, city, true);
    }

    public void ToggleEnemyBuildingHighlight(Vector3Int cityLoc, Color color)
    {
		foreach (CityImprovement building in world.cityBuildingDict[cityLoc].Values)
        {
            building.DisableHighlight();
			building.EnableHighlight(color, true);
		}
	}

    public void ToggleBuildingHighlight(bool v, Vector3Int cityLoc)
    {        
        if (v)
        {
            foreach (CityImprovement building in world.cityBuildingDict[cityLoc].Values)
            {
                building.DisableHighlight();

                if (removingImprovement)
                {
                    building.EnableHighlight(Color.red, true);
                }
                else if (upgradingImprovement)
                {
                    if (building.GetImprovementData.improvementLevel < world.GetUpgradeableObjectMaxLevel(building.GetImprovementData.improvementName))
                    {
                        building.canBeUpgraded = true;
                        building.EnableHighlight(Color.green, true);
                    }
                    else
                    {
                        building.canBeUpgraded = false;
                        building.EnableHighlight(Color.white);
                    }
                }
                else
                {
                    building.EnableHighlight(Color.white);
                }
            }
        }
        else
        {
			foreach (CityImprovement building in world.cityBuildingDict[cityLoc].Values)
                building.DisableHighlight();
        }
    }

    private void ResourceProducerTimeProgressBarsSetActive(bool v)
    {
        foreach (Vector3Int tile in developedTiles)
        {
            ResourceProducer producer = world.GetResourceProducer(tile);
            
            if (producer.isWaitingForStorageRoom || producer.hitResourceMax)
            {
                if (!v)
                    producer.TimeProgressBarSetActive(v);
                else
                    producer.SetTimeProgressBarToFull();
            }
            else if (!producer.isWaitingForResearch && !producer.isWaitingforResources)
            {
                if (producer.isUpgrading)
                {
                    world.GetResourceProducer(tile).TimeConstructionProgressBarSetActive(v);
                    continue;
                }
                producer.TimeProgressBarSetActive(v);
            }
        }

        foreach (Vector3Int tile in constructingTiles)
        {
            world.GetResourceProducer(tile).TimeConstructionProgressBarSetActive(v);
        }
    }

    private void DrawBorders()
    {
        List<Vector3Int> tempCityTiles = new(cityTiles) { selectedCity.cityLoc };

        for (int i = 0; i < tempCityTiles.Count; i++)
        {
			//finding border neighbors for tile
			List<Vector3Int> neighborList = world.GetNeighborsFor(tempCityTiles[i], MapWorld.State.FOURWAYINCREMENT);
            for (int j = 0; j < neighborList.Count; j++)
			{
				if (!tempCityTiles.Contains(neighborList[j])) //only draw borders on areas that aren't city tiles
				{
					//Object pooling set up
					GameObject tempObject = GetFromBorderPool();
					borderList.Add(tempObject);
					Vector3Int borderLocation = neighborList[j] - tempCityTiles[i]; //used to determine where on tile border should be
					Vector3 borderPosition = tempCityTiles[i];
					borderPosition.y = 0f;

					if (borderLocation.x != 0)
					{
						borderPosition.x += (0.5f * borderLocation.x);
						tempObject.transform.rotation = Quaternion.Euler(0, 90, 0); //rotating to make it look like city wall
					}
					else if (borderLocation.z != 0)
					{
						borderPosition.z += (0.5f * borderLocation.z);
					}

					tempObject.transform.position = borderPosition;
				}
			}
		}
    }

    public void CheckPopForUnit(UnitBuildDataSO unitData)
    {
        if (unitData.unitType == UnitType.Laborer) 
        {
            world.uiLaborDestinationWindow.ToggleVisibility(true, true, selectedCity);
			uiCityTabs.HideSelectedTab(false);
			return;
        }
        
        CreateUnit(unitData, selectedCity, false);
    }

    public void CreateUnit(UnitBuildDataSO unitData, City city, bool upgrading, Unit upgradedUnit = null)
    {
        if (!upgrading)
        {
            city.resourceManager.SpendResource(unitData.unitCost, city.cityLoc);

            for (int i = 0; i < unitData.laborCost; i++)
                city.PopulationDeclineCheck(true, true); //decrease population before creating unit so we can see where labor will be lost

            //updating uis after losing pop
            if (city.activeCity)
            {
                uiInfoPanelCity.SetAllData(selectedCity);
                uiResourceManager.SetCityCurrentStorage(city.resourceManager.resourceStorageLevel);
                uiCityTabs.HideSelectedTab(false);
            }
        }

		Vector3Int buildPosition = city.cityLoc;

        if (upgradedUnit == world.azai)
        {
			buildPosition = upgradedUnit.currentLocation;
		}
        else if (unitData.unitType != UnitType.Laborer)
        {
            if (unitData.unitType == UnitType.Transport)
            {
                if (unitData.transportationType == TransportationType.Sea)
                    world.waterTransport = true;
                else if (unitData.transportationType == TransportationType.Air)
                    world.airTransport = true;
            }

			CityImprovement improvement = world.GetCityDevelopment(city.singleBuildDict[unitData.singleBuildType]);
			improvement.BeginTraining(city, improvement.resourceProducer, unitData, upgrading, upgradedUnit, false);
            return;
        }

        Vector3 buildLoc = buildPosition;
        if (world.GetTerrainDataAt(buildPosition).isHill)
        {
            if (buildPosition.z % 3 == 0 && buildPosition.x % 3 == 0)
                buildLoc.y += .6f;
            else
                buildLoc.y += .3f;
        }

        if (world.IsRoadOnTileLocation(buildPosition))
            buildLoc.y += .11f;
		GameObject unit = Instantiate(Resources.Load<GameObject>("Prefabs/" + unitData.prefabLoc), buildLoc, Quaternion.identity); //produce unit at specified position
        unit.transform.SetParent(friendlyUnitHolder, false);
        //for tweening
        Vector3 goScale = unit.transform.localScale;
        float scaleX = goScale.x;
        float scaleZ = goScale.z;
        unit.transform.localScale = new Vector3(scaleX, 0.1f, scaleZ);
        LeanTween.scale(unit, goScale, 0.5f).setEase(LeanTweenType.easeOutBack);
        Unit newUnit = unit.GetComponent<Unit>();
		newUnit.SetReferences(world);
        newUnit.PlayAudioClip(buildClip);

        if (upgradedUnit == world.azai)
        {
            newUnit.transform.rotation = upgradedUnit.transform.rotation;
            newUnit.name = "Azai";
            world.azai = newUnit.GetComponent<BodyGuard>();
            world.characterUnits.Remove(upgradedUnit);
            upgradedUnit.DestroyUnit();
            world.characterUnits.Add(newUnit);
            world.uiSpeechWindow.AddToSpeakingDict("Azai", newUnit);
            world.azai.SetArmy();
			newUnit.currentLocation = world.AddPlayerPosition(buildPosition, newUnit);
        }
        else //for laborers
        {
            world.laborerCount++;
            unit.name = "Laborer " + world.laborerCount;
            world.laborerList.Add(newUnit.laborer);
            newUnit.SetMinimapIcon(friendlyUnitHolder);
            Vector3 mainCamLoc = Camera.main.transform.position;
            mainCamLoc.y = 0;
            unit.transform.rotation = Quaternion.LookRotation(mainCamLoc - unit.transform.position);
        }
	}

    public void BuildUnit(City city, UnitBuildDataSO unitData, bool upgrading, Unit upgradedUnit)
    {
        Vector3Int buildPosition = city.cityLoc;
        bool reselectAfterUpgrade = false;

        if (unitData.baseAttackStrength > 0)
        {
		    if (unitData.transportationType == TransportationType.Sea)
            {

			}
            else if (unitData.transportationType == TransportationType.Air)
            {

            }
			else
            {
		        if (upgrading)
                {
				    buildPosition = upgradedUnit.currentLocation;
				    reselectAfterUpgrade = upgradedUnit.isSelected;
				    upgradedUnit.RemoveUnitFromData();
					upgradedUnit.military.army.RemoveFromArmy(upgradedUnit.military, upgradedUnit.military.barracksBunk, true);
				    city.army.AddToOpenSpots(buildPosition);
				    //upgradedUnit.DestroyUnit();
		        }
                else
                {
		            buildPosition = city.army.GetAvailablePosition(unitData.unitType);
                }
            }
        }
        else 
        {
            if (upgrading)
                buildPosition = upgradedUnit.currentLocation;
            else
                buildPosition = world.GetTraderBuildLoc(city.singleBuildDict[unitData.singleBuildType]);

		    //if (city.activeCity && uiUnitBuilder.activeStatus)
			   // uiUnitBuilder.UpdateTrainingStatus(unitData.singleBuildType);
		}

        Quaternion rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
        Vector3 buildLoc = buildPosition;
        if (world.IsRoadOnTileLocation(buildPosition))
            buildLoc.y += .11f;
		GameObject unit = Instantiate(Resources.Load<GameObject>("Prefabs/" + unitData.prefabLoc), buildLoc, rotation); //produce unit at specified position
		unit.gameObject.transform.SetParent(friendlyUnitHolder, false);
		Unit newUnit = unit.GetComponent<Unit>();

		//for tweening
		Vector3 goScale = unit.transform.localScale;
        float scaleX = goScale.x;
        float scaleZ = goScale.z;
        unit.transform.localScale = new Vector3(scaleX, 0.1f, scaleZ);
        LeanTween.scale(unit, goScale, 0.5f).setEase(LeanTweenType.easeOutBack)/*.setOnComplete(() => newUnit.outline.ToggleOutline(false))*/;

		newUnit.SetReferences(world);
		newUnit.SetMinimapIcon(friendlyUnitHolder);
        newUnit.PlayAudioClip(buildClip);

		//transferring all previous trader info to new one
		if (newUnit.trader)
		{
			if (upgrading)
            {
                world.traderList.Remove(upgradedUnit.trader);
				newUnit.trader.name = upgradedUnit.name;
				newUnit.id = upgradedUnit.id;
				newUnit.trader.hasRoute = upgradedUnit.trader.hasRoute;
				newUnit.trader.tradeRouteManager = upgradedUnit.trader.tradeRouteManager;
				newUnit.trader.tradeRouteManager.SetTrader(newUnit.trader);
				newUnit.trader.personalResourceManager = upgradedUnit.trader.personalResourceManager;
				newUnit.trader.resourceGridDict = upgradedUnit.trader.resourceGridDict;
				world.GetCityDevelopment(city.singleBuildDict[upgradedUnit.buildDataSO.singleBuildType]).RemoveTraderFromImprovement(upgradedUnit.trader);
			    upgradedUnit.DestroyUnit();
            }

            world.GetCityDevelopment(city.singleBuildDict[unitData.singleBuildType]).AddTraderToImprovement(newUnit.trader);
            world.traderCount++;
			if (!upgrading)
				unit.name = "Trader " + world.traderCount;

			newUnit.id = world.traderCount;
            world.traderList.Add(newUnit.trader);
            newUnit.trader.atHome = true;
            newUnit.trader.homeCity = city.cityLoc;

            newUnit.currentLocation = world.AddTraderPosition(buildPosition, newUnit.trader);
			world.TutorialCheck("Finished Building Trader");
		}

		//assigning army details and rotation
		if (newUnit.buildDataSO.inMilitary)
        {
            world.militaryCount++;
            newUnit.military.atHome = true;
		    city.army.AddToArmy(newUnit.military);
		    newUnit.military.army = city.army;
            newUnit.military.barracksBunk = buildPosition;

            if (newUnit.military.army.selected)
                newUnit.SoftSelect(Color.white);
            else if (world.assigningGuard && world.uiTradeRouteBeginTooltip.MilitaryLocCheck(world.GetClosestTerrainLoc(buildPosition)))
                newUnit.SoftSelect(Color.green);

			//if (city.activeCity && uiUnitBuilder.activeStatus)
			//	uiUnitBuilder.UpdateBarracksStatus(city.army.isFull);
			else if (world.uiCampTooltip.activeStatus && world.uiCampTooltip.improvement == world.GetCityDevelopment(city.singleBuildDict[SingleBuildType.Barracks]))
				world.uiCampTooltip.RefreshData();

            newUnit.name = unitData.unitDisplayName;
            if (!upgrading)
            {
                switch (unitData.unitType)
			    {
				    case UnitType.Infantry:
					    world.infantryCount++;
					    break;
					case UnitType.Ranged:
						world.rangedCount++;
						break;
					case UnitType.Cavalry:
						world.cavalryCount++;
						break;
				}

                newUnit.id = world.militaryCount;
			}
            else
            {
                newUnit.id = upgradedUnit.id;
                upgradedUnit.DestroyUnit();
			}
		    
            newUnit.currentLocation = world.AddUnitPosition(buildPosition, newUnit);
			if (world.tutorial && newUnit.buildDataSO.unitType == UnitType.Infantry)
                world.TutorialCheck("Finished Building Infantry");
		}
        else if (newUnit.transport)
        {
            newUnit.name = unitData.unitDisplayName;
            world.transportList.Add(newUnit.transport);
            newUnit.prevTerrainTile = world.GetClosestTerrainLoc(buildPosition);
        }

        if (world.upgrading)
            world.unitMovement.ToggleUnitHighlights(true, city);

        if (reselectAfterUpgrade)
            world.unitMovement.PrepareMovement(newUnit);
	}

	public void TransferLaborPrep(string destination, int amount, bool atSea)
	{
        Vector3Int loc = world.GetStopMainLocation(destination);

        List<Vector3Int> transferPath;
        List<Vector3Int> spawnLocs;

        if (atSea)
        {
            Vector3Int harborLoc = selectedCity.singleBuildDict[SingleBuildType.Harbor];
			transferPath = GridSearch.TraderMove(world, harborLoc, world.GetCity(loc).singleBuildDict[SingleBuildType.Harbor], true);
            spawnLocs = world.GetNeighborsFor(harborLoc, MapWorld.State.EIGHTWAY);
			spawnLocs.Insert(0, harborLoc);
        }
        else
        {
		    transferPath = GridSearch.TraderMove(world, selectedCity.cityLoc, loc, false);
		    spawnLocs = world.GetNeighborsFor(selectedCity.cityLoc, MapWorld.State.EIGHTWAY);
            spawnLocs.Insert(0, selectedCity.cityLoc);
        }

		if (transferPath.Count > 0)
            transferPath.RemoveAt(0);

        for (int i = 0; i < amount; i++)
        {
            selectedCity.PopulationDeclineCheck(true, true);
		    bool secondaryPrefab;

            if (selectedCity.currentPop % 2 == 0)
                secondaryPrefab = false;
            else
                secondaryPrefab = true;

            TransferWorker(secondaryPrefab, loc, spawnLocs[i], false, transferPath, atSea);
        }
		
        uiInfoPanelCity.SetAllData(selectedCity);
	}

	public void UpgradeUnitWindow(Unit unit)
    {
        uiCityUpgradePanel.ToggleVisibility(true, selectedCity.resourceManager, null, unit);
    }

	public void CreateBuildingQueueCheck(ImprovementDataSO buildingData)
    {
        //Queue info
        //if (isQueueing)
        //{
        //    selectedCity.AddToQueue(buildingData, selectedCity.cityLoc, Vector3Int.zero, false);
        //    return;
        //}

        CreateBuilding(buildingData, selectedCity, false);
    }

    private void CreateBuilding(ImprovementDataSO buildingData, City city, bool upgradingImprovement)
    {
        //uiQueueManager.CheckIfBuiltItemIsQueued(city.cityLoc, new Vector3Int(0, 0, 0), upgradingImprovement, buildingData, city);

        if (buildingData.improvementName == "Housing"/*city.housingData.improvementName*/)
        {
            city.SetHouse(buildingData, city.cityLoc, world.GetTerrainDataAt(city.cityLoc).isHill, upgradingImprovement);

            if (city.activeCity)
            {
                uiInfoPanelCity.UpdateHousing(city.HousingCount);
                uiResourceManager.SetCityCurrentStorage(city.resourceManager.resourceStorageLevel);

                if (!upgradingImprovement)
                    uiCityTabs.HideSelectedTab(false);
            }

            return;
        }
        //for some non buildings in the building selection list (eg harbor)
        if (buildingData.isBuildingImprovement)
        {
            CreateImprovement(buildingData);
            return;
        }

        //setting building locations
        Vector3 cityPos = city.cityLoc;
        Vector3 buildingLocalPos = buildingData.buildingLocation; //putting the building in it's position in the city square
        cityPos += buildingLocalPos;

        if (!upgradingImprovement)
        {
            city.resourceManager.SpendResource(buildingData.improvementCost, cityPos);
		}
        PlaySelectAudio(buildClip);

        GameObject building;
        if (world.GetTerrainDataAt(city.cityLoc).isHill)
        {
            cityPos.y += buildingData.hillAdjustment;
            building = Instantiate(Resources.Load<GameObject>("Prefabs/" + buildingData.prefabLoc), cityPos, Quaternion.identity);
        }
        else
        {
            building = Instantiate(Resources.Load<GameObject>("Prefabs/" + buildingData.prefabLoc), cityPos, Quaternion.identity);
        }

        //setting world data
        CityImprovement improvement = building.GetComponent<CityImprovement>();
        improvement.SetWorld(world);
        improvement.loc = city.cityLoc;
        building.transform.parent = city.subTransform;

        string buildingName = buildingData.improvementName;
		improvement.PlaySmokeSplashBuilding();
		world.SetCityBuilding(improvement, buildingData, city.cityLoc, city, buildingName);

        if (buildingData.workEthicChange != 0)
        {
            if (city.activeCity && uiCityTabs.builderUI != null)
                uiCityTabs.builderUI.UpdateProducedNumbers(city.resourceManager);
		    else if (world.uiCityImprovementTip.activeStatus)
			    world.uiCityImprovementTip.UpdateProduceNumbers();
        }

        city.purchaseAmountMultiple += buildingData.purchaseAmountChange;
		city.HousingCount += buildingData.housingIncrease;
        if (buildingData.waterIncrease > 0)
        {
            city.waterCount += buildingData.waterIncrease;
            if (city.activeCity)
                uiInfoPanelCity.UpdateWater(city.waterCount);

			if (city.waterCount <= 0)
				city.reachedWaterLimit = true;
			else
				city.reachedWaterLimit = false;
		}

        city.attackBonus += buildingData.attackBonus;

        //for tweening
        Vector3 goScale = new Vector3(1.5f, 1.5f, 1.5f);
        building.transform.localScale = Vector3.zero;
        LeanTween.scale(building, goScale, 0.25f).setEase(LeanTweenType.easeOutBack).setOnComplete( ()=> { CombineMeshes(city, city.subTransform, this.upgradingImprovement); 
            improvement.SetInactive(); ToggleBuildingHighlight(true, city.cityLoc); });

        if (buildingData.singleBuildType != SingleBuildType.None)
            city.singleBuildList.Add(buildingData.singleBuildType);

        //if (buildingData.singleBuildType == SingleBuildType.Market)
        //    if (city.activeCity) uiCityTabs.marketButton.SetActive(true);

        if (buildingData.singleBuildType == SingleBuildType.Well)
            city.ExtinguishFire();

        //updating uis
        if (city.activeCity)
        {
            if (buildingData.housingIncrease > 0)  
                uiInfoPanelCity.UpdateHousing(city.HousingCount);
            if (buildingData.workEthicChange != 0)
            {
                uiInfoPanelCity.UpdateWorkEthic(city.workEthic);
                UpdateCityWorkEthic();
            }
     
            uiResourceManager.SetCityCurrentStorage(city.resourceManager.resourceStorageLevel);

            if (!upgradingImprovement)
                uiCityTabs.HideSelectedTab(false);
        }
    }

    private void RemoveBuilding(CityImprovement improvement, ImprovementDataSO data, City city, bool upgradingImprovement)
    {
		if (!upgradingImprovement)
            improvement.PlayRemoveEffect(world.GetTerrainDataAt(city.cityLoc).isHill);

		//putting the resources and labor back
		string selectedBuilding = data.improvementName;
        if (selectedBuilding == "Housing"/*city.housingData.improvementName*/)
            selectedBuilding = city.DecreaseHousingCount(improvement.housingIndex);

        CityImprovement building = world.GetBuildingData(city.cityLoc, selectedBuilding);

        if (!upgradingImprovement)
        {
            int i = 0;
            Vector3 cityLoc = city.cityLoc;
            cityLoc.y += data.improvementCost.Count * 0.4f;

            city.resourceManager.resourceCount = 0;
            foreach (ResourceValue resourceValue in data.improvementCost)
            {
                int resourcesReturned = city.resourceManager.AddResource(resourceValue.resourceType, resourceValue.resourceAmount);
                Vector3 cityLoc2 = cityLoc;
                cityLoc2.y += -.4f * i;
                InfoResourcePopUpHandler.CreateResourceStat(cityLoc2, resourcesReturned, ResourceHolder.Instance.GetIcon(resourceValue.resourceType), world);
                i++;
            }
        }

        //changing city stats
        city.HousingCount -= data.housingIncrease;
        city.workEthic -= data.workEthicChange;
		city.purchaseAmountMultiple -= data.purchaseAmountChange;
		city.improvementWorkEthic -= data.workEthicChange;
        city.attackBonus -= data.attackBonus;

		if (data.waterIncrease > 0)
        {
            city.waterCount -= data.waterIncrease;
			uiInfoPanelCity.UpdateWater(city.waterCount);
            
            if (city.waterCount <= 0)
                city.reachedWaterLimit = true;
            else
				city.reachedWaterLimit = false;
        }

        if (data.singleBuildType != SingleBuildType.None)
            city.singleBuildList.Remove(data.singleBuildType);

		//if (data.singleBuildType == SingleBuildType.Market)
  //      {
  //          //city.hasMarket = false;
  //          uiCityTabs.marketButton.SetActive(false);
  //      }

        //updating ui
        if (city.activeCity)
        {
            if (data.workEthicChange != 0)
                UpdateCityWorkEthic();
            uiResourceManager.SetCityCurrentStorage(city.resourceManager.resourceStorageLevel);
            uiInfoPanelCity.SetAllData(selectedCity);
        }

        city.RemoveFromMeshFilterList(true, Vector3Int.zero, selectedBuilding);
        Destroy(building.gameObject);

        //updating world dicts
        world.cityBuildingDict[city.cityLoc].Remove(selectedBuilding);
        
        //updating city graphic
        CombineMeshes(city, city.subTransform, this.upgradingImprovement);
    }

    public void CreateImprovement(ImprovementDataSO improvementData)
    {
        laborChange = 0;

        this.improvementData = improvementData;

        if (!upgradingImprovement)
            uiCityTabs.HideSelectedTab(false);
        ImprovementTileHighlight(false);
    }

    private void ImprovementTileHighlight(bool removingImprovement)
    {
        tilesToChange.Clear();

        if (removingImprovement)
        {
            if (!upgradingImprovement) //only show when not upgrading
            {
                uiImprovementBuildInfoPanel.SetText("Removing Items");
                uiImprovementBuildInfoPanel.SetImage(removeButton, false);
                uiImprovementBuildInfoPanel.ToggleVisibility(true);
            }
        }
        else
        {
            uiImprovementBuildInfoPanel.SetText("Building " + improvementData.improvementName);
            uiImprovementBuildInfoPanel.SetImage(improvementData.image, true);
            uiImprovementBuildInfoPanel.ToggleVisibility(true);
        }

        foreach (Vector3Int tile in cityTiles)
        {
            TerrainData td = world.GetTerrainDataAt(tile);
            td.DisableHighlight(); //turning off all highlights first

            if (removingImprovement) //If removing improvement
            {                                
                if (world.TileHasCityImprovement(tile) && !td.inBattle)
                {
                    CityImprovement improvement = world.GetCityDevelopment(tile); //cached for speed
                    improvement.DisableHighlight();

                    if (!improvement.isConstruction)
                        improvement.EnableHighlight(Color.red);
    
                    td.EnableHighlight(Color.red);
                    tilesToChange.Add(tile);
                }                
            }
            else //if placing improvement
            {
                if (isQueueing && world.CheckQueueLocation(tile))
                    continue;

                bool tileCheck;

                if (improvementData.buildOnRoad)
                    tileCheck = world.IsTileOpenButRoadCheck(tile);
                else
                    tileCheck = world.IsTileOpenCheck(tile);

                if (tileCheck && !td.hasBattle)
                {
                    TerrainType type = td.terrainData.type;

                    //for building sea improvements on rivers
                    if (type == TerrainType.River)
                        type = TerrainType.Coast;

                    if (improvementData.rawMaterials)
                    {
                        if (td.rawResourceType == improvementData.rawResourceType)
                        {
                            if (improvementData.oneTerrain && type!= improvementData.terrainType)
                                continue;
                        
                            td.EnableHighlight(Color.white);
                            tilesToChange.Add(tile);
                        }
                    }
                    else
                    {
                        if (improvementData.oneTerrain && type != improvementData.terrainType)
                            continue;

                        td.EnableHighlight(Color.white);
                        tilesToChange.Add(tile);
                    }
                }
            }
        }
    }

    public void BuildImprovementQueueCheck(ImprovementDataSO improvementData, Vector3Int tempBuildLocation)
    {
        //queue information
        //if (isQueueing)
        //{
        //    if (!selectedCity.AddToQueue(improvementData, tempBuildLocation, tempBuildLocation - selectedCity.cityLoc, false))
        //        return;

        //    tilesToChange.Remove(tempBuildLocation);
        //    world.GetTerrainDataAt(tempBuildLocation).DisableHighlight();
        //    return;
        //}

        BuildImprovement(improvementData, tempBuildLocation, selectedCity, false);
    }

    private void BuildImprovement(ImprovementDataSO improvementData, Vector3Int tempBuildLocation, City city, bool upgradingImprovement)
    {
        //uiQueueManager.CheckIfBuiltItemIsQueued(tempBuildLocation, tempBuildLocation - city.cityLoc, upgradingImprovement, improvementData, city);
        world.CheckTileForTreasure(tempBuildLocation);

        if (!upgradingImprovement)
        {
            bool somethingInWay = false;
            if (improvementData.buildOnRoad)
            {
                if (!world.IsTileOpenButRoadCheck(tempBuildLocation))
                    somethingInWay = true;
            }
            else
            {
                if (!world.IsTileOpenCheck(tempBuildLocation))
                    somethingInWay = true;
            } 

            if (somethingInWay)
            {
			    UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Something already here");
                return;
            }
        }

        if (!upgradingImprovement)
            PlaySelectAudio(constructionClip);

		//spending resources to build
		Vector3Int buildLocation = tempBuildLocation;
        buildLocation.y = 0;

        if (!upgradingImprovement)
            city.resourceManager.SpendResource(improvementData.improvementCost, buildLocation);

        if (city.activeCity)
            uiResourceManager.SetCityCurrentStorage(city.resourceManager.resourceStorageLevel);

        //rotating harbor so it's closest to city
        int rotation = 0;
		if (improvementData.singleBuildType == SingleBuildType.Harbor)
            rotation = HarborRotation(tempBuildLocation, city.cityLoc);

        //adding improvement to world dictionaries
        TerrainData td = world.GetTerrainDataAt(tempBuildLocation);

        if (improvementData.secondaryData.Count > 0)
        {
            foreach (ImprovementDataSO tempData in improvementData.secondaryData)
            {
                if (improvementData.producedResources[0].resourceType == tempData.producedResources[0].resourceType)
                {
                    if (td.terrainData.specificTerrain == tempData.specificTerrain)
                    {
                        improvementData = tempData;
                        break;
                    }
                }
                else if (td.resourceType == tempData.producedResources[0].resourceType)
                {   
                    improvementData = tempData;
                    break;
                }
            }
            
            rotation = (int)td.transform.eulerAngles.y;
        }

        GameObject improvement;
        if (td.isHill && improvementData.adjustForHill)
        {
            Vector3 buildLocationHill = buildLocation;
            buildLocationHill.y += improvementData.hillAdjustment;
            improvement = Instantiate(Resources.Load<GameObject>("Prefabs/" + improvementData.prefabLoc), buildLocationHill, Quaternion.Euler(0, rotation, 0));
        }
        else
        {
            improvement = Instantiate(Resources.Load<GameObject>("Prefabs/" + improvementData.prefabLoc), buildLocation, Quaternion.Euler(0, rotation, 0));
        }

        improvement.transform.SetParent(world.objectPoolItemHolder, false);
        world.AddStructure(buildLocation, improvement);
        CityImprovement cityImprovement = improvement.GetComponent<CityImprovement>();
        cityImprovement.SetWorld(world);
        cityImprovement.loc = buildLocation;
        cityImprovement.InitializeImprovementData(improvementData);
        cityImprovement.SetQueueCity(null);
        cityImprovement.building = improvementData.isBuilding;
        cityImprovement.city = city;

        world.SetCityDevelopment(tempBuildLocation, cityImprovement);
        cityImprovement.improvementMesh.SetActive(false);

        //setting single build rules
        if (improvementData.singleBuildType != SingleBuildType.None)
        {
            city.singleBuildList.Add(improvementData.singleBuildType);
            world.AddToCityLabor(tempBuildLocation, city.cityLoc);
        }

        //resource production
        ResourceProducer resourceProducer = improvement.GetComponent<ResourceProducer>();
        buildLocation.y = 0;
        world.AddResourceProducer(tempBuildLocation, resourceProducer);
        resourceProducer.SetResourceManager(city.resourceManager);
        resourceProducer.InitializeImprovementData(improvementData, td.resourceType, 0); //allows the new structure to also start generating resources
        resourceProducer.SetCityImprovement(cityImprovement, 0);
        resourceProducer.SetLocation(tempBuildLocation);
        cityImprovement.CheckPermanentChanges();

        if (upgradingImprovement)
        {
            if (improvementData.producedResourceTime.Count > 0)
                resourceProducer.SetUpgradeProgressTimeBar(improvementData.producedResourceTime[0]);
            FinishImprovement(city, improvementData, tempBuildLocation);
        }
        else
        {
            cityImprovement.isConstruction = true;
            cityImprovement.BeginImprovementConstructionProcess(city, resourceProducer, tempBuildLocation, this, td.isHill, false);

            if (city.activeCity)
            {
                constructingTiles.Add(tempBuildLocation);
                CloseImprovementBuildPanel();
            }
        }
    }

    public void FinishImprovement(City city, ImprovementDataSO improvementData, Vector3Int tempBuildLocation)
    {
		world.TutorialCheck("Finished Building " + improvementData.improvementName);
		//activating structure
		GameObject improvement = world.GetStructure(tempBuildLocation);
        TerrainData td = world.GetTerrainDataAt(tempBuildLocation);
        CityImprovement cityImprovement = world.GetCityDevelopment(tempBuildLocation);
        cityImprovement.improvementMesh.SetActive(true);
        cityImprovement.HideIdleMesh();
        cityImprovement.isConstruction = false;
        cityImprovement.SetMinimapIcon(td);
        cityImprovement.meshCity = city;
        cityImprovement.transform.parent = city.transform;
        city.AddToImprovementList(cityImprovement);
        cityImprovement.PlaySmokeSplash(td.isHill);
        city.PlaySelectAudio(buildClip);
        //cityImprovement.PlayPlacementAudio(buildClip);

        if (improvementData.housingIncrease > 0)
        {
			city.HousingCount += improvementData.housingIncrease;

            if (city.activeCity)
    			uiInfoPanelCity.UpdateHousing(city.HousingCount);

			//must go last
			if (world.uiCityPopIncreasePanel.CheckCity(city))
                world.uiCityPopIncreasePanel.UpdateHousingCosts(city);
        }

        if (improvementData.waterIncrease > 0)
        {
			city.waterCount += improvementData.waterIncrease;
			if (city.activeCity)
				uiInfoPanelCity.UpdateWater(city.waterCount);

			if (city.waterCount <= 0)
				city.reachedWaterLimit = true;
			else
				city.reachedWaterLimit = false;

			//must go last
			if (world.uiCityPopIncreasePanel.CheckCity(city))
				world.uiCityPopIncreasePanel.UpdateWaterCosts(city);
		}

        if (improvementData.workEthicChange != 0)
        {
            city.workEthic += improvementData.workEthicChange;
            city.improvementWorkEthic += improvementData.workEthicChange;
            
            if (city.activeCity && uiCityTabs.builderUI != null)
				uiCityTabs.builderUI.UpdateProducedNumbers(city.resourceManager);
			else if (world.uiCityImprovementTip.activeStatus)
				world.uiCityImprovementTip.UpdateProduceNumbers();
		}

        if (improvementData.purchaseAmountChange != 0 && improvementData.maxLabor == 0)
        {
		    city.purchaseAmountMultiple += improvementData.purchaseAmountChange;
            if (uiMarketPlaceManager.activeStatus && uiMarketPlaceManager.city == city)
                uiMarketPlaceManager.UpdatePurchaseAmounts();
        }
		//making two objects, this one for the parent mesh
		GameObject tempObject = Instantiate(emptyGO, cityImprovement.transform.position, cityImprovement.transform.rotation);
        tempObject.name = improvement.name;
        MeshFilter[] improvementMeshes = cityImprovement.MeshFilter;

        MeshFilter[] meshes = new MeshFilter[improvementMeshes.Length];
        int k = 0;

        foreach (MeshFilter mesh in improvementMeshes)
        {
            Quaternion rotation = mesh.transform.rotation;
            meshes[k] = Instantiate(mesh, mesh.transform.position, rotation);
            meshes[k].name = mesh.name;
            meshes[k].transform.parent = tempObject.transform;
            k++;
        }

        //tempObject.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f); 
        cityImprovement.Embiggen();

        city.AddToMeshFilterList(tempObject, meshes, false, tempBuildLocation);
        tempObject.transform.parent = city.transform;
        tempObject.SetActive(false);

        //resetting ground UVs is necessary
        if (improvementData.replaceTerrain)
        {
            td.ToggleTerrainMesh(false);

            if (!td.terrainData.grassland)
            {
                foreach (MeshFilter mesh in cityImprovement.MeshFilter)
                {
                    if (mesh.name == "Ground")
                    {
					    Vector2[] terrainUVs = world.SetUVMap(world.GetGrasslandCount(td), world.SetUVShift(td.terrainData.terrainDesc), Mathf.RoundToInt(td.main.eulerAngles.y));
                        Vector2[] newUVs = mesh.mesh.uv;
					    Vector2[] finalUVs = world.NormalizeUVs(terrainUVs, newUVs, Mathf.RoundToInt(td.main.localEulerAngles.y / 90));
                        mesh.mesh.uv = finalUVs;

                        foreach (MeshFilter mesh2 in meshes)
                        {
                            if (mesh2.name == "Ground")
                            {
                                mesh2.mesh.uv = finalUVs;
                                break;
                            }
                        }

                        break;
                    }
                }
            }
        }
        else
        {
            if (td.terrainData.specificTerrain == SpecificTerrain.FloodPlain)
                td.FloodPlainCheck(true);

            //for tweening
            Vector3 goScale = new Vector3(1.5f, 1.5f, 1.5f);// improvement.transform.localScale;
            improvement.transform.localScale = Vector3.zero;
            LeanTween.scale(improvement, goScale, 0.4f).setEase(LeanTweenType.easeOutBack).setOnComplete( () => { CombineMeshes(city, city.subTransform, upgradingImprovement); 
                cityImprovement.SetInactive(); TileCheck(tempBuildLocation, city, cityImprovement.resourceProducer); });
        }

		if (td.terrainData.type == TerrainType.Forest || td.terrainData.type == TerrainType.ForestHill)
			td.SwitchToRoad();

        //reseting rock UVs 
        world.ColorCityImprovementRocks(improvementData, cityImprovement, td, meshes);
		//if (improvementData.replaceRocks)
  //      {
  //          foreach (MeshFilter mesh in cityImprovement.MeshFilter)
  //          {
  //              if (mesh.name == "Rocks")
  //              {
  //                  Vector2 rockUVs = ResourceHolder.Instance.GetUVs(td.resourceType);
  //                  Vector2[] newUVs = mesh.mesh.uv;
  //                  int i = 0;

  //                  while (i < newUVs.Length)
  //                  {
  //                      newUVs[i] = rockUVs;
  //                      i++;
  //                  }
  //                  mesh.mesh.uv = newUVs;

  //                  foreach (MeshFilter mesh2 in meshes)
  //                  {
  //                      if (mesh2.name == "Rocks")
  //                      {
  //                          mesh2.mesh.uv = newUVs;
  //                          break;
  //                      }
  //                  }

  //                  if (cityImprovement.SkinnedMesh != null && cityImprovement.SkinnedMesh.name == "RocksAnim")
  //                  {
  //                      int j = 0;
  //                      Vector2[] skinnedUVs = cityImprovement.SkinnedMesh.sharedMesh.uv;

  //                      while (j < skinnedUVs.Length)
  //                      {
  //                          skinnedUVs[j] = rockUVs;
  //                          j++;
  //                      }

  //                      cityImprovement.SkinnedMesh.sharedMesh.uv = skinnedUVs;

  //                      //if (cityImprovement.SkinnedMesh.name == "RocksAnim")
  //                      //{
  //                      Material mat = td.prop.GetComponentInChildren<MeshRenderer>().sharedMaterial;
  //                      cityImprovement.SkinnedMesh.material = mat;
  //                      cityImprovement.SetNewMaterial(mat);
  //                      //}
  //                  }

  //                  break;
  //              }
  //          }

  //      }

        if (td.prop != null && improvementData.hideProp)
            td.ShowProp(false);

        if (improvementData.singleBuildType != SingleBuildType.None)
        {
            city.singleBuildDict[improvementData.singleBuildType] = tempBuildLocation;
            //if (city.activeCity && uiUnitBuilder.activeStatus)
            //    uiUnitBuilder.UpdateTrainingStatus(improvementData.singleBuildType);
        }

        //setting harbor info
        if (improvementData.singleBuildType == SingleBuildType.TradeDepot)
        {
			world.AddStop(tempBuildLocation, city);
            world.BuildRoadWithObject(tempBuildLocation, true);
		}
        else if (improvementData.singleBuildType == SingleBuildType.Harbor)
        {
			cityImprovement.mapIconHolder.localRotation = Quaternion.Inverse(improvement.transform.rotation);
            world.AddStop(tempBuildLocation, city);
        }
        else if (improvementData.singleBuildType == SingleBuildType.Airport)
        {
			world.AddStop(tempBuildLocation, city);
		}
        else if (improvementData.singleBuildType == SingleBuildType.Barracks)
        {
            world.militaryStationLocs.Add(tempBuildLocation);
            cityImprovement.army = city.army;

			foreach (Vector3Int tile in world.GetNeighborsFor(tempBuildLocation, MapWorld.State.EIGHTWAYARMY))
                city.army.SetArmySpots(tile);

            city.army.SetLoc(tempBuildLocation, city);

            //if (city.activeCity && uiUnitBuilder.activeStatus)
            //    uiUnitBuilder.UpdateBarracksStatus(city.army.isFull);
        }

        //setting labor info (harbors have no labor)
        //world.AddToMaxLaborDict(tempBuildLocation, improvementData.maxLabor);
        if (city.autoAssignLabor && city.unusedLabor > 0 && improvementData.maxLabor > 0)
            city.AutoAssignmentsForLabor();

        //no tweening, so must be done here
        if (improvementData.replaceTerrain)
        {
			improvement.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
			CombineMeshes(city, city.subTransform, upgradingImprovement); 
            cityImprovement.SetInactive();
            TileCheck(tempBuildLocation, city, cityImprovement.resourceProducer);
        }

        if (selectedCity != null)
            constructingTiles.Remove(tempBuildLocation); //don't need to check for active city
    }

    private void TileCheck(Vector3Int tempBuildLocation, City city, ResourceProducer producer)
    {
        if (selectedCity != null && cityTiles.Contains(tempBuildLocation))
        {
            developedTiles.Add(tempBuildLocation);

            //in case improvement is made while another city is selected and the city that made it has auto assign on
            if (city.activeCity)
            {
                producer.UpdateCityImprovementStats();
                //if (maxLabor > 0)
                //    UpdateCityLaborUIs();

                if (laborChange != 0)
                    LaborTileHighlight();
                else if (removingImprovement && uiImprovementBuildInfoPanel.activeStatus)
                    ImprovementTileHighlight(true);
                else if (upgradingImprovement && uiImprovementBuildInfoPanel.activeStatus)
                    UpgradeTileHighlight();
            }
            else
            {
                (cityTiles, developedTiles, constructingTiles) = world.GetCityRadiusFor(selectedCity.cityLoc);
                HideBorders();
                DrawBorders();
            }
        }
    }

    public int HarborRotation(Vector3Int tempBuildLocation, Vector3Int originationLocation)
    {
        int rotation = 0;
        int minimum = 99999;
        int rotationIndex = 0;

        foreach (Vector3Int neighbor in world.GetNeighborsFor(tempBuildLocation, MapWorld.State.FOURWAYINCREMENT))
        {
            if (!world.GetTerrainDataAt(neighbor).sailable) //don't place harbor on neighboring water tiles
            {
                int distanceFromCity = Math.Abs(neighbor.x - originationLocation.x) + Math.Abs(neighbor.z - originationLocation.z);
                if (distanceFromCity < minimum)
                {
                    minimum = distanceFromCity;
                    rotation = rotationIndex * 90;
                }
            }

            rotationIndex++;
        }
        
        return rotation;
    }

    //boolean is to see to play sound or not
    public bool RemoveImprovement(Vector3Int improvementLoc, CityImprovement selectedImprovement, bool upgradingImprovement, City city, bool destroyingCity = false/*, bool enemy = false*/)
    {
        ImprovementDataSO improvementData = selectedImprovement.GetImprovementData;
        //remove building
        if (selectedImprovement.GetImprovementData.isBuilding && !selectedImprovement.GetImprovementData.isBuildingImprovement)
        {
            RemoveBuilding(selectedImprovement, improvementData, selectedImprovement.city, upgradingImprovement);
            return true;
        }

        bool updateCity = false;
        if (selectedCity != null && cityTiles.Contains(improvementLoc))
            updateCity = true;

		//if (selectedImprovement.queued && updateCity)
  //          uiQueueManager.CheckIfBuiltItemIsQueued(improvementLoc, improvementLoc - selectedImprovement.city.cityLoc, true, improvementData, selectedImprovement.GetQueueCity());

        ResourceProducer resourceProducer = selectedImprovement.resourceProducer;
		TerrainData td = world.GetTerrainDataAt(improvementLoc);

		//if cancelling training in a barracks or harbor, stop here
		if (selectedImprovement.isTraining)
		{
			if (!destroyingCity /*&& !enemy*/)
                ReplaceImprovementCost(city, selectedImprovement.upgradeCost, improvementLoc);

			if (!selectedImprovement.isUpgrading)
			{
				selectedImprovement.city.PopulationGrowthCheck(true, selectedImprovement.laborCost);
				if (updateCity)
                    UpdateCityLaborUIs();
			}

			selectedImprovement.CancelTraining(resourceProducer);
			selectedImprovement.StopUpgrade();
            if (updateCity)
            {
    			uiResourceManager.SetCityCurrentStorage(selectedImprovement.city.resourceManager.resourceStorageLevel);
			    
                if (removingImprovement)
                    ImprovementTileHighlight(true);
            }

            return removingImprovement;
		}
		//if removing/canceling upgrade process, stop here
		else if (selectedImprovement.isUpgrading)
        {
            if (!destroyingCity /*&& !enemy*/)
                ReplaceImprovementCost(city, selectedImprovement.upgradeCost, improvementLoc);
            selectedImprovement.StopUpgradeProcess(resourceProducer);
            selectedImprovement.StopUpgrade();

            if (updateCity)
            {
                selectedImprovement.resourceProducer.UpdateCityImprovementStats();
                UpdateCityLaborUIs();
                uiResourceManager.SetCityCurrentStorage(selectedCity.resourceManager.resourceStorageLevel);

                if (removingImprovement)
                    ImprovementTileHighlight(true);
            }

            return removingImprovement;
        }
		//can't remove barracks if army holding army
		else if (!upgradingImprovement && improvementData.singleBuildType != SingleBuildType.None && selectedImprovement.unitsWithinCount > 0)
        {
			//if (!enemy)
            InfoPopUpHandler.WarningMessage(world.objectPoolItemHolder).Create(improvementLoc, "Currently stationing units");

			return false;
		}
		
        if (!upgradingImprovement) 
        {
            //putting the labor back
            int currentLabor = resourceProducer.currentLabor;
            resourceProducer.StopProducing(true);
            if (!selectedImprovement.isConstruction)
                ResetProducerLabor(city, improvementLoc, resourceProducer);
            resourceProducer.UpdateCurrentLaborData(0);

			ResourceType resourceType = resourceProducer.producedResource.resourceType;

            if (resourceType != ResourceType.None)
            {
				int totalResourceLabor = selectedImprovement.city.ChangeResourcesWorked(resourceType, -currentLabor);
                if (updateCity && uiLaborHandler.activeStatus)
                {
                    for (int i = 0; i < currentLabor; i++)
                        uiLaborHandler.PlusMinusOneLabor(resourceType, totalResourceLabor, -1, selectedImprovement.city.resourceManager.GetResourceGenerationValues(resourceType));
                }
                if (totalResourceLabor == 0)
					selectedImprovement.city.RemoveFromResourcesWorked(resourceType);
            }

            //replacing the cost
            if (!destroyingCity /*&& !enemy*/)
                ReplaceImprovementCost(city, improvementData.improvementCost, improvementLoc);

            if (updateCity)
                uiResourceManager.SetCityCurrentStorage(selectedCity.resourceManager.resourceStorageLevel);

            if (improvementData.replaceTerrain)
                td.ToggleTerrainMesh(true);
            else if (td.terrainData.specificTerrain == SpecificTerrain.FloodPlain)
                td.FloodPlainCheck(false);

			if (td.rawResourceType == RawResourceType.Rocks)
            {
                if (td.resourceAmount > 0)
                {
                    td.prop.gameObject.SetActive(true);
                    td.ShowProp(true);
					td.RocksCheck();
				}
                else
                {
                    td.ClearRocks();
                    //td.ShowProp(false);
                    world.SetNewTerrainData(td);
			    }
            }
            else
            {
				td.ShowProp(true);
				if (td.terrainData.type == TerrainType.Forest || td.terrainData.type == TerrainType.ForestHill)
					td.SwitchFromRoad();
			}
            
            //if (!selectedImprovement.isConstruction && selectedImprovement.city != null)
            //{
            //    int currentLabor = world.GetCurrentLaborForTile(improvementLoc);
            //    selectedImprovement.city.unusedLabor += currentLabor;
            //    selectedImprovement.city.usedLabor -= currentLabor;
            //}
        }

        if (!upgradingImprovement && !selectedImprovement.isConstruction)
		    selectedImprovement.PlayRemoveEffect(td.isHill);

        //updating city graphic
        if (!selectedImprovement.isConstruction)
        {
            //combined meshes steps
            if (selectedImprovement.meshCity != null)
            {
                selectedImprovement.meshCity.RemoveFromImprovementList(selectedImprovement);
                selectedImprovement.meshCity.RemoveFromMeshFilterList(false, improvementLoc);
                CombineMeshes(selectedImprovement.meshCity, selectedImprovement.meshCity.subTransform, this.upgradingImprovement);
            }
            else
            {
                (MeshFilter[] meshes, GameObject go) = improvementMeshDict[improvementLoc];
                improvementMeshDict.Remove(improvementLoc);

                int count = meshes.Length;
                for (int i = 0; i < count; i++)
                    improvementMeshList.Remove(meshes[i]);

                Destroy(go);
                CombineMeshes();
            }

		    //changing city stats
            if (selectedImprovement.city != null)
            {
			    selectedImprovement.city.HousingCount -= improvementData.housingIncrease;

                if (improvementData.purchaseAmountChange != 0 && improvementData.maxLabor == 0) 
    			    selectedImprovement.city.purchaseAmountMultiple -= improvementData.purchaseAmountChange;

			    if (improvementData.workEthicChange != 0)
                {
			        selectedImprovement.city.workEthic -= improvementData.workEthicChange;
			        selectedImprovement.city.improvementWorkEthic -= improvementData.workEthicChange;
				    UpdateCityWorkEthic();
			    }
		    
                if (improvementData.waterIncrease > 0)
		        {
			        selectedImprovement.city.waterCount -= improvementData.waterIncrease;
                    if (updateCity)
    			        uiInfoPanelCity.UpdateWater(selectedCity.waterCount);

			        if (selectedImprovement.city.waterCount <= 0)
					    selectedImprovement.city.reachedWaterLimit = true;
			        else
					    selectedImprovement.city.reachedWaterLimit = false;
		        }
            }
        }

        //selectedImprovement.resourceProducer.DestroyProgressBar();
		GameObject improvement = world.GetStructure(improvementLoc);
        Destroy(improvement);

        //updating world dicts
        //if (!upgradingImprovement)
        //{
        //    RemoveLaborFromDicts(improvementLoc);
        //    //world.RemoveFromMaxWorked(improvementLoc);
        //}
        world.RemoveStructure(improvementLoc);
        developedTiles.Remove(improvementLoc);

        //stop here if it in construction process
        if (selectedImprovement.isConstruction)
        {
            constructingTiles.Remove(improvementLoc);
            selectedImprovement.RemoveConstruction();

			if (updateCity && removingImprovement)
				ImprovementTileHighlight(true);

			RemoveSingleBuildSteps(selectedImprovement, improvementLoc, improvementData);
			return true;
        }

        if (upgradingImprovement) //stop here if upgrading
            return true; 

        if (updateCity && removingImprovement)
            ImprovementTileHighlight(true);

		if (!upgradingImprovement && world.IsRoadOnTerrain(improvementLoc) && !selectedImprovement.hadRoad)
			world.roadManager.RemoveRoadAtPosition(improvementLoc);

        RemoveSingleBuildSteps(selectedImprovement, improvementLoc, improvementData);

        //updating ui
        if (updateCity)
        {
            if (!upgradingImprovement)
            {
                uiInfoPanelCity.SetAllData(selectedCity);
                //UpdateLaborNumbers(selectedCity);
            }
            //else
            //{
            //    selectedImprovement.resourceProducer.UpdateCityImprovementStats();
            //}
        }

        return true;
    }

    private void RemoveSingleBuildSteps(CityImprovement selectedImprovement, Vector3Int improvementLoc, ImprovementDataSO improvementData)
    {
		if (selectedImprovement.city != null)
		{
			SingleBuildType type = improvementData.singleBuildType;
			if (type == SingleBuildType.TradeDepot || type == SingleBuildType.Harbor || type == SingleBuildType.Airport)
			{
				selectedImprovement.city.singleBuildDict.Remove(type); //must be here
				
                if (!selectedImprovement.isConstruction)
                {
                    selectedImprovement.city.RemoveStopCheck(improvementLoc, type);
				    world.RemoveStop(improvementLoc);
                }
			}
			else if (type == SingleBuildType.Barracks)
			{
				world.militaryStationLocs.Remove(improvementLoc);
				selectedImprovement.city.army.ClearArmySpots();
			}

			if (selectedImprovement.city.autoAssignLabor && selectedImprovement.city.unusedLabor > 0)
				selectedImprovement.city.AutoAssignmentsForLabor();

			if (type != SingleBuildType.None)
			{
				selectedImprovement.city.singleBuildDict.Remove(type);
				selectedImprovement.city.singleBuildList.Remove(type);
				world.RemoveSingleBuildFromCityLabor(improvementLoc);
			}
		}
	}


	private void ReplaceImprovementCost(City city, List<ResourceValue> replaceCost, Vector3 improvementLoc)
    {
        int i = 0;
        improvementLoc.y += replaceCost.Count * 0.4f;

        city.resourceManager.resourceCount = 0;
        foreach (ResourceValue resourceValue in replaceCost) //adding back 100% of cost (if there's room)
        {
            int resourcesReturned = city.resourceManager.AddResource(resourceValue.resourceType, resourceValue.resourceAmount);
            Vector3 loc = improvementLoc;
            loc.y += -.4f * i;
            i++;
            if (resourcesReturned == 0)
            {
                InfoResourcePopUpHandler.CreateResourceStat(loc, resourceValue.resourceAmount, ResourceHolder.Instance.GetIcon(resourceValue.resourceType), world, true);
                continue;
            }
            InfoResourcePopUpHandler.CreateResourceStat(loc, resourcesReturned, ResourceHolder.Instance.GetIcon(resourceValue.resourceType), world);
        }
    }

    public void CloseImprovementTooltipButton()
    {
        world.CloseImprovementTooltipButton();
        world.CloseCampTooltipButton();
    }

    public void CloseImprovementBuildPanelButton()
    {
        PlayCloseAudio();
        CloseImprovementBuildPanel();
    }

    public void CloseImprovementBuildPanel()
    {
        if (uiImprovementBuildInfoPanel.activeStatus)
        {
            if (selectedWonder != null)
            {
                //CameraDefaultRotation();
                foreach (Vector3Int tile in tilesToChange)
                {
                    world.GetTerrainDataAt(tile).DisableHighlight();
                }
                tilesToChange.Clear();
                uiImprovementBuildInfoPanel.ToggleVisibility(false);
                placingWonderHarbor = false;
                return;
            }
            
            if (removingImprovement || upgradingImprovement)
                uiCityTabs.CloseSelectedTab();
            ResetTileLists();
            world.unitMovement.ToggleUnitHighlights(false);
            ToggleBuildingHighlight(true, selectedCity.cityLoc);
            uiImprovementBuildInfoPanel.ToggleVisibility(false);
            uiCityUpgradePanel.ToggleVisibility(false);
        }
    }

    public void CancelUpgrade()
    {
        upgradingImprovement = false;
    }

    public void LaborManager(int laborChange) //for assigning labor
    {
        if (laborChange == 0)
        {
            ResetTileLists();
            uiLaborAssignment.HideUI();
            return;
        }

        CloseQueueUI();
		world.uiCityPopIncreasePanel.ToggleVisibility(false);
		uiCityTabs.HideSelectedTab(false);
        CloseSingleWindows();
        world.CloseImprovementTooltipButton();
        world.CloseCampTooltipButton();
        CloseImprovementBuildPanel();
        improvementData = null;
        
        if (laborChange != uiLaborAssignment.laborChangeFlag) //turn on menu only when selecting different button
        {
            uiLaborAssignment.ResetLaborAssignment(-laborChange);
            uiLaborAssignment.laborChangeFlag = laborChange;
            this.laborChange = laborChange;
            uiLaborHandler.ToggleVisibility(true, selectedCity);
            LaborTileHighlight();
        }
        else
        {
            uiLaborHandler.HideUI();
            uiLaborAssignment.ResetLaborAssignment(laborChange);
        }
    }

    public void UpdateCityWorkEthic()
    {
        foreach (Vector3Int tile in developedTiles)
        {
            ResourceProducer producer = world.GetResourceProducer(tile);
            if (!producer.improvementData.cityBonus)
                producer.CalculateResourceGenerationPerMinute();

            if (uiLaborHandler.activeStatus && producer.producedResource.resourceType != ResourceType.None)
                uiLaborHandler.UpdateUICount(producer.producedResource.resourceType, selectedCity.resourceManager.GetResourceGenerationValues(producer.producedResource.resourceType));
        }
    }

    public void UpdateLaborNumbers(bool show)
    {
        if (show)
        {
            foreach (Vector3Int tile in developedTiles)
				world.GetResourceProducer(tile).UpdateCityImprovementStats();
        }
        else
        {
			foreach (Vector3Int tile in developedTiles)
			{
				ResourceProducer producer = world.GetResourceProducer(tile);

				if (producer.improvementData.maxLabor > 0)
					producer.cityImprovementStats.SetActive(false);
			}
		}
    }

    private void LaborTileHighlight()
    {
        tilesToChange.Clear();

        foreach (Vector3Int tile in developedTiles)
        {
            TerrainData td = world.GetTerrainDataAt(tile);
            td.DisableHighlight(); //turning off all highlights first

            CityImprovement improvement = world.GetCityDevelopment(tile);
            improvement.DisableHighlight();
            if (improvement.isUpgrading || improvement.GetImprovementData.maxLabor == 0 || td.inBattle)
                continue;

            if (laborChange > 0 && !world.CheckIfTileIsMaxxed(tile) && selectedCity.unusedLabor > 0) //for increasing labor, can't be maxxed out
            {
                td.EnableHighlight(Color.green);
                improvement.EnableHighlight(Color.green);
                tilesToChange.Add(tile);   
            }
            else if (laborChange < 0 && world.CheckIfTileIsWorked(tile)) //for decreasing labor, can't be at 0. 
            {
                td.EnableHighlight(Color.red);
                improvement.EnableHighlight(Color.red);
                tilesToChange.Add(tile);
            }
        }
    }

    private void ChangeLaborCount(Vector3Int terrainLocation)
    {
        ResourceProducer resourceProducer = world.GetResourceProducer(terrainLocation); //cached all resource producers in dict
        
        int labor = world.GetCurrentLaborForTile(terrainLocation);
        int maxLabor = world.GetMaxLaborForTile(terrainLocation);

		//checks in case
		if (laborChange > 0)
        {
            if (labor == maxLabor)
            {
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Reached max labor", true);
				return;
			}
            else if (selectedCity.unusedLabor == 0)
            {
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "No more available labor", true);
				return;
            }
            else if (resourceProducer.ProductionPausedCheck())
            {
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Can't, production is paused", true);
				return;
            }
            else if (!resourceProducer.ConsumeResourcesCheck())
            {
                UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Production costs too high for new labor", true);
                return;
            }
        }
        if (laborChange < 0 && labor == 0)
        {
			UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "No labor to remove", true);
			return;
        }

        labor += laborChange;
        selectedCity.unusedLabor -= laborChange;
        selectedCity.usedLabor += laborChange;

        resourceProducer.UpdateCurrentLaborData(labor);
		PlaySelectAudio(laborChange > 0 ? laborInClip : laborOutClip);

        if (labor == 0) //removing from world dicts when zeroed out
        {
            RemoveLaborFromDicts(terrainLocation);
            resourceProducer.StopProducing();
        }
        else if (laborChange > 0)
        {
            if (labor == 1) //assigning city to location if working for first time
			{
				CityImprovement selectedImprovement = world.GetCityDevelopment(terrainLocation);
				//if mesh isn't owned by anyone, add to this city's
				if (selectedImprovement.meshCity == null)
				{
					selectedImprovement.meshCity = selectedCity;
					selectedImprovement.transform.parent = selectedCity.transform;
					selectedCity.SetNewMeshCity(terrainLocation, improvementMeshDict, improvementMeshList);
					CombineMeshes(selectedCity, selectedCity.subTransform, upgradingImprovement);
					selectedCity.AddToImprovementList(selectedImprovement);
				}

				if (world.GetCityDevelopment(terrainLocation).queued)
				{
					City tempCity = selectedImprovement.GetQueueCity();

					if (tempCity != selectedCity)
					{
						tempCity.RemoveFromQueue(terrainLocation - tempCity.cityLoc);
						selectedImprovement.SetQueueCity(null);
					}
				}

                selectedImprovement.city = selectedCity;
				world.AddToCityLabor(terrainLocation, selectedCity.cityLoc);
				resourceProducer.SetResourceManager(selectedCity.resourceManager);
				resourceProducer.StartProducing(false);
			}
            else
            {
				resourceProducer.AddLaborMidProduction();
			}
        }
        else if (labor > 0 && laborChange < 0)
        {
            resourceProducer.RemoveLaborCheck(selectedCity);
        }

        //if (labor != 0)
        //{
        //    world.AddToCurrentFieldLabor(terrainLocation, labor);
        //}

        resourceProducer.UpdateCityImprovementStats();
        //UpdateLaborNumbers(selectedCity);

        //updating all the labor info
        ResourceType resourceType = resourceProducer.producedResource.resourceType;
        if (resourceType != ResourceType.None)
        {
			int totalResourceLabor = selectedCity.ChangeResourcesWorked(resourceType, laborChange);
            uiLaborHandler.PlusMinusOneLabor(resourceType, totalResourceLabor, laborChange, selectedCity.resourceManager.GetResourceGenerationValues(resourceType));
            
            if (totalResourceLabor == 0)
                selectedCity.RemoveFromResourcesWorked(resourceType);
        }

        uiLaborHandler.UpdateResourcesConsumed(resourceProducer.consumedResourceTypes, selectedCity.resourceManager.resourceConsumedPerMinuteDict);

        uiInfoPanelCity.SetAllData(selectedCity);
        LaborTileHighlight();
        world.TutorialCheck("Change Labor");
    }

    private void ResetProducerLabor(City city, Vector3Int loc, ResourceProducer producer)
    {
		int currentLabor = world.GetCurrentLaborForTile(loc);
		city.unusedLabor += currentLabor;
		city.usedLabor -= currentLabor;
    	RemoveLaborFromDicts(loc);

		if (city.activeCity)
		{
			if (producer.improvementData.maxLabor > 0)
				producer.cityImprovementStats.SetActive(false);
			UpdateCityLaborUIs();
			uiInfoPanelCity.SetAllData(selectedCity);
			uiResourceManager.SetCityCurrentStorage(city.resourceManager.resourceStorageLevel);
		}
	}

    private void RemoveLaborFromDicts(Vector3Int terrainLocation)
    {
        world.RemoveFromCityLabor(terrainLocation);
    }

    public void UpdateCityLaborUIs()
    {
        //UpdateLaborNumbers();
        uiInfoPanelCity.SetAllData(selectedCity);
    }

    public void CloseCityTab()
    {
        uiCityTabs.HideSelectedTab(false);
    }

    public void ResetCityUIToBase()
    {
        world.CloseImprovementTooltipButton();
        world.CloseCampTooltipButton();
        CloseImprovementBuildPanel();
        uiLaborAssignment.ResetLaborAssignment();
		world.uiCityPopIncreasePanel.ToggleVisibility(false);
		CloseQueueUI();
        uiCityTabs.HideSelectedTab(false);
        CloseLaborMenus();
        CloseSingleWindows();

		if (world.tutorial)
		{
			if (uiHelperWindow != null && uiHelperWindow.activeStatus)
				uiHelperWindow.ToggleVisibility(false);
		}
	}

    public void ToggleLaborHandlerMenu()
    {
        PlaySelectAudio();
        
        if (uiLaborHandler.activeStatus)
        {
            CloseLaborMenus();
        }
        else
        {
            CloseCityTab();
            world.CloseImprovementTooltipButton();
            world.CloseCampTooltipButton();
            CloseImprovementBuildPanel();
			world.uiCityPopIncreasePanel.ToggleVisibility(false);
			CloseQueueUI();
            ResetTileLists();
            CloseSingleWindows();
            uiLaborHandler.ToggleVisibility(true, selectedCity);
        }
    }

    public void CloseLaborMenusButton()
    {
        PlayCloseAudio();
        CloseLaborMenus();
    }

    public void CloseLaborMenus()
    {
        uiLaborHandler.HideUI();

        if (laborChange != 0)
        {
            uiLaborAssignment.ResetLaborAssignment();

            if (!uiImprovementBuildInfoPanel.activeStatus)
            {
                ResetTileLists();
                ToggleBuildingHighlight(true, selectedCity.cityLoc);
            }
        }

        //uiLaborPrioritizationManager.ToggleVisibility(false);
    }

    public void DestroyCityWarning()
    {
        if (selectedCity != null)
        {
			if (selectedCity.attacked)
            {
                UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Enemy approaching", true);
			    return;
            }
            else if (!selectedCity.army.atHome)
            {
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Army is deployed", true);
				return;
			}
        }
        
        PlaySelectAudio();
        
        if (uiDestroyCityWarning.activeStatus)
        {
            uiDestroyCityWarning.ToggleVisibility(false);
        }
        else
        {
            ResetCityUIToBase();
            uiDestroyCityWarning.ToggleVisibility(true);
        }
    }

    public void ToggleAutoAssign()
    {
        PlaySelectAudio(checkClip);
        
        if (autoAssign.isOn)
        {
            selectedCity.autoAssignLabor = true;

            if (selectedCity.unusedLabor > 0)
            {
                selectedCity.AutoAssignmentsForLabor();
                UpdateCityLaborUIs();
            }

            uiLaborAssignment.showPrioritiesButton.SetActive(true);
        }
        else
        {
            selectedCity.autoAssignLabor = false;
            //uiLaborPrioritizationManager.ToggleVisibility(false);
			uiLaborAssignment.showPrioritiesButton.SetActive(false);
		}
    }

   // public void ClosePrioritizationManager()
   // {
   //     PlayCloseAudio();
   //     uiLaborPrioritizationManager.ToggleVisibility(false);
   // }

   // public void TogglePrioritizationMenu()
   // {
   //     PlaySelectAudio();
        
   //     if (!uiLaborPrioritizationManager.activeStatus)
   //     {
   //         CloseLaborMenus();
   //         world.CloseImprovementTooltipButton();
   //         world.CloseCampTooltipButton();
   //         CloseImprovementBuildPanel();
   //         uiCityTabs.HideSelectedTab(false);
			//world.uiCityPopIncreasePanel.ToggleVisibility(false);
			//CloseQueueUI();
   //         CloseSingleWindows();
   //         uiLaborPrioritizationManager.ToggleVisibility(true);
   //         uiLaborPrioritizationManager.PrepareLaborPrioritizationMenu(selectedCity);
   //         uiLaborPrioritizationManager.LoadLaborPrioritizationInfo();
   //         //prioritizationMenuActive = true;
   //     }
   //     else
   //     {
   //         uiLaborPrioritizationManager.ToggleVisibility(false);
   //     }
   // }

    //public void ToggleQueue()
    //{
    //    PlaySelectAudio();
        
    //    if (!isQueueing)
    //        BeginBuildQueue();
    //    else
    //        EndBuildQueue();
    //}

    //private void BeginBuildQueue()
    //{
    //    SetQueueStatus(true);
    //    uiQueueManager.ToggleButtonSelection(true);
    //}

    //private void EndBuildQueue()
    //{
    //    SetQueueStatus(false);
    //    uiQueueManager.ToggleButtonSelection(false);
    //}

    private void SetQueueStatus(bool v)
    {
        if (isQueueing && !v)
            CloseImprovementBuildPanel();

        isQueueing = v;
        uiRawGoodsBuilder.isQueueing = v;
        uiProducerBuilder.isQueueing = v;
        uiBuildingBuilder.isQueueing = v;
    }

  //  public void BuildQueuedBuilding(City city, ResourceManager resourceManager)
  //  {
  //      QueueItem item = city.GetBuildInfo();
  //      this.resourceManager = resourceManager;
  //      bool building = item.queueLoc == Vector3Int.zero;
        
  //      if (item.upgrade)
  //      {
  //          Vector3Int tile = item.queueLoc + city.cityLoc;
  //          if (building)
  //              UpgradeSelectedImprovementPrep(city.cityLoc, world.GetBuildingData(tile, item.queueName), city, uiQueueManager.upgradeCosts, uiQueueManager.refundCosts);
  //          else
  //              UpgradeSelectedImprovementPrep(tile, world.GetCityDevelopment(tile), city, uiQueueManager.upgradeCosts, uiQueueManager.refundCosts);
  //      }
		//else if (building) //build building
  //      {
  //          CreateBuilding(UpgradeableObjectHolder.Instance.improvementDict[item.queueName], city, false);
  //      }
  //      else //build improvement
  //      {
		//	ImprovementDataSO improvementData = UpgradeableObjectHolder.Instance.improvementDict[item.queueName];
		//	Vector3Int tile = item.queueLoc + city.cityLoc;

  //          if (world.IsBuildLocationTaken(tile) || world.IsRoadOnTerrain(tile) || improvementData.rawResourceType != world.GetTerrainDataAt(tile).rawResourceType)
  //          {
  //              city.GoToNextItemInQueue();
  //              uiQueueManager.CheckIfBuiltItemIsQueued(tile, item.queueLoc, false, improvementData, city);
  //              return;
  //          }
  //          BuildImprovement(improvementData, tile, city, false);
  //      }

  //      city.GoToNextItemInQueue();
  //  }

    public List<QueueItem> GetQueueItems()
    {
        return selectedCity.queueItemList;
    }

    public void CloseQueueUIButton()
    {
        PlayCloseAudio();
        CloseQueueUI();
    }

    public void CloseQueueUI()
    {
        //if (uiQueueManager.activeStatus)
        //{
        //    SetQueueStatus(false);
        //    uiQueueManager.UnselectQueueItem();
        //    uiQueueManager.ToggleVisibility(false);
        //}
    }

    public void RunCityNamerUI()
    {
        if (selectedCity != null && selectedCity.attacked)
        {
			UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Enemy approaching", true);
            return;
		}
        
        PlaySelectAudio();
        
        if (uiCityNamer.activeStatus)
        {
            uiCityNamer.ToggleVisibility(false);
            uiTraderNamer.ToggleVisibility(false);
        }
        else
        {
            if (selectedCity != null)
            {
                ResetCityUIToBase();
                ResetTileLists();
                uiCityNamer.ToggleVisibility(true, selectedCity);
            }
            else if (world.unitMovement.selectedUnit && world.unitMovement.selectedUnit.trader)
            {
				uiTraderNamer.ToggleVisibility(true, null, world.unitMovement.selectedUnit.trader);
            }
        }
    }

    public void CloseSingleWindows()
    {
        uiDestroyCityWarning.ToggleVisibility(false);
        uiCityNamer.ToggleVisibility(false);
        world.uiLaborDestinationWindow.ToggleVisibility(false);
        uiCityUpgradePanel.ToggleVisibility(false);
        focusCam.paused = false;
    }

    public void DestroyCityConfirm()
    {
        PlaySelectAudio();
        
        if (selectedWonder != null)
        {
			CancelWonderConstruction();
			return;
		}
        else
        {
            DestroyCity(selectedCity);
        }
    }

    public void DestroyCity(City city) //set on destroy city warning message
    {
        ResetCityUI();

        //stop upgrading and training improvements
        foreach (Vector3Int tile in world.GetNeighborsFor(city.cityLoc, MapWorld.State.CITYRADIUS))
        {
            if (world.TileHasCityImprovement(tile))
            {
                CityImprovement improvement = world.GetCityDevelopment(tile);

                if (improvement.city == city)
                {
                    if (improvement.isUpgrading || improvement.isTraining || improvement.isConstruction)
                        RemoveImprovement(tile, improvement, true, city, true);

                    improvement.city = null;
				}
			}
        }

        //disassociating improvements from city mesh
        foreach (CityImprovement improvement in city.ImprovementList)
        {
            if (improvement.city == city)
            improvement.meshCity = null;
            improvement.transform.parent = improvementHolder.transform;
        }

		city.ExtinguishFire();
		city.ReassignMeshes(improvementHolder, improvementMeshDict, improvementMeshList);
        CombineMeshes();

        TerrainData td = world.GetTerrainDataAt(city.cityLoc);
        if (td.rawResourceType == RawResourceType.Rocks && td.resourceAmount == 0)
            td.ShowProp(false);
        else
			td.ShowProp(true);

		td.FloodPlainCheck(false);

        GameObject destroyedCity = world.GetStructure(city.cityLoc);
        world.RemoveCityNameMap(city.cityLoc);
        world.RemoveStructure(city.cityLoc);
        world.RemoveStopName(city.cityName);
		city.DestroyThisCity();

        //for all single build improvements, finding a nearby city to join that doesn't have one. If not one available, then is unowned. 
        SetSingleBuildsAvailable(city);
        world.uiProfitabilityStats.RemoveCityStats(city);
        Destroy(destroyedCity);

        uiDestroyCityWarning.ToggleVisibility(false);
    }

    public void SetSingleBuildsAvailable(City city)
    {
		foreach (SingleBuildType singleImprovement in city.singleBuildDict.Keys)
		{
            if (singleImprovement == SingleBuildType.None)
                continue;

            Vector3Int improvementLoc = city.singleBuildDict[singleImprovement];
			if (improvementLoc == city.cityLoc)
				continue;
            CityImprovement improvement = world.GetCityDevelopment(improvementLoc);

			world.RemoveSingleBuildFromCityLabor(improvementLoc);
			bool unclaimed = true;

			foreach (Vector3Int tile in world.GetNeighborsFor(improvementLoc, MapWorld.State.CITYRADIUS))
			{
				if (world.IsCityOnTile(tile))
				{
					City tempCity = world.GetCity(tile);
					if (!tempCity.singleBuildList.Contains(singleImprovement))
					{
                        tempCity.singleBuildList.Add(singleImprovement);
                        tempCity.singleBuildDict[singleImprovement] = improvementLoc;
						world.AddToCityLabor(improvementLoc, tempCity.cityLoc);

						if (singleImprovement == SingleBuildType.TradeDepot || singleImprovement == SingleBuildType.Harbor || singleImprovement == SingleBuildType.Airport)
                            world.AddStop(tile, tempCity);

						if (singleImprovement == SingleBuildType.Barracks)
						{
                            tempCity.army.city = tempCity;
                            improvement.army = tempCity.army;

							foreach (Vector3Int armySpot in world.GetNeighborsFor(improvementLoc, MapWorld.State.EIGHTWAYARMY))
								tempCity.army.SetArmySpots(armySpot);
						}

						unclaimed = false;
						break;
					}
				}
			}

			if (unclaimed)
			{
                improvement.city = null;
                
                if (singleImprovement == SingleBuildType.TradeDepot || singleImprovement == SingleBuildType.Harbor || singleImprovement == SingleBuildType.Airport)
					world.RemoveStop(improvementLoc);

                if (singleImprovement == SingleBuildType.Barracks)
                    improvement.army = null;

				world.AddToUnclaimedSingleBuild(improvementLoc);
			}
		}
	}

    public void AddToOrphanMeshFilterList(GameObject go, MeshFilter[] meshFilter, Vector3Int loc)
    {
		int count = meshFilter.Length;
		for (int i = 0; i < count; i++)
		{
			improvementMeshList.Add(meshFilter[i]);
		}

		improvementMeshDict[loc] = (meshFilter, go);
	}

    public void NoDestroyCity() //in case user chickens out
    {
        PlayCloseAudio();
        
        uiDestroyCityWarning.ToggleVisibility(false);
        if (selectedCity != null)
            uiCityTabs.HideSelectedTab(false);
    }

    public void ResetTileLists()
    {
        foreach (Vector3Int tile in tilesToChange)
        {
            world.GetTerrainDataAt(tile).DisableHighlight();
            if (world.TileHasCityImprovement(tile))
                world.GetCityDevelopment(tile).DisableHighlight();
        }

        tilesToChange.Clear();
        improvementData = null;
        laborChange = 0;
        removingImprovement = false;
        upgradingImprovement = false;
    }

    public void ResetCityUI()
    {
        if (selectedCity != null)
        {
            ResourceProducerTimeProgressBarsSetActive(false);
            UpdateLaborNumbers(false);
            cityTiles.Clear();
            developedTiles.Clear();
            constructingTiles.Clear();
            removingImprovement = false;
            //uiLaborPrioritizationManager.ToggleVisibility(false, true);
            ResetCityUIToBase();
            ResetTileLists();
            uiCityTabs.ToggleVisibility(false);
            uiResourceManager.ToggleVisibility(false);
            uiInfoPanelCity.ToggleVisibility(false);
            uiLaborAssignment.HideUI();
            //HideCityImprovementStats();
            //HideLaborNumbers();
            //HideImprovementResources();
            //uiLaborHandler.ResetUI();
            HideBorders();
			world.GetTerrainDataAt(selectedCity.cityLoc).DisableHighlight();
            world.unitMovement.ToggleUnitHighlights(false);
            ToggleBuildingHighlight(false, selectedCity.cityLoc);
            selectedCity.HideCityGrowthProgressTimeBar();
            selectedCity.activeCity = false;

            if (selectedCity.gameObject.tag == "Player")
                focusCam.RestoreWorldLimit();
            selectedCity = null;
			world.StopAudio();

            if (world.tutorial)
            {
                if (uiHelperWindow.activeStatus)
                    uiHelperWindow.ToggleVisibility(false);
            }

			Resources.UnloadUnusedAssets();
		}
    }

    public void UnselectWonderButton()
    {
        PlayCloseAudio();
        UnselectWonder();
    }

    public void UnselectWonder()
    {
        if (selectedWonder != null)
        {
            world.somethingSelected = false;
            uiDestroyCityWarning.ToggleVisibility(false);
            selectedWonder.isActive = false;
            selectedWonder.TimeProgressBarSetActive(false);
            uiWonderSelection.ToggleVisibility(false, selectedWonder);
            if (placingWonderHarbor)
                CloseImprovementBuildPanel();
            selectedWonder.DisableHighlight();
            selectedWonder = null;
        }
    }

    public void UnselectTradeCenter()
    {
        if (selectedTradeCenter != null)
        {
            world.somethingSelected = false;
            uiTradeCenter.ToggleVisibility(false);
            selectedTradeCenter.DisableHighlight();
            //if (world.GetClosestTerrainLoc(world.mainPlayer.transform.position) == selectedTradeCenter.mainLoc)
            //    selectedTradeCenter.ToggleClear(true);

            selectedTradeCenter = null;
        }
    }

    public void CombineMeshes(City city, Transform cityTransform, bool upgrade)
    {
        MeshFilter[] meshFilters = city.CityMeshFilters.ToArray();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        int i = 0;

        while (i < meshFilters.Length)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);

            i++;
        }

        MeshFilter meshFilter = cityTransform.GetComponent<MeshFilter>();
        meshFilter.mesh = new Mesh();
        meshFilter.mesh.CombineMeshes(combine);

        cityTransform.transform.gameObject.SetActive(true);

        //meshes inexplicably move without this when making the parent a non-pre-existing item in the heirarchy
        cityTransform.localScale = Vector3.one;
        cityTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        if (selectedCity == city)
        {
            if (removingImprovement)
                ImprovementTileHighlight(true);
        }
    }

    //for the improvement holder
    public void CombineMeshes()
    {
        MeshFilter[] meshFilters = improvementMeshList.ToArray();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        int i = 0;

        while (i < meshFilters.Length)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);

            i++;
        }

        MeshFilter meshFilter = improvementHolder.GetComponent<MeshFilter>();
        meshFilter.mesh = new Mesh();
        meshFilter.mesh.CombineMeshes(combine);

        improvementHolder.transform.gameObject.SetActive(true);
    }

	#region object pooling
    private void GrowBordersPool()
    {
        for (int i = 0; i < 20; i++) //grow pool 20 at a time
        {
            GameObject border = Instantiate(Resources.Load<GameObject>("Prefabs/InGameSpritePrefabs/CityBorder"));
            border.gameObject.transform.SetParent(objectPoolHolder, false);
            AddToBorderPool(border);
        }
    }

    private void AddToBorderPool(GameObject gameObject)
    {
        gameObject.SetActive(false); //inactivate it when adding to pool
        borderQueue.Enqueue(gameObject);
    }

    private GameObject GetFromBorderPool()
    {
        if (borderQueue.Count == 0)
            GrowBordersPool();

        var border = borderQueue.Dequeue();
        border.SetActive(true);
        return border;
    }

    private void HideBorders()
    {
        Quaternion origRotation = Quaternion.identity;

        foreach (GameObject border in borderList)
        {
            border.transform.rotation = origRotation;
            AddToBorderPool(border);
        }

        borderList.Clear();
    }
    #endregion
}
