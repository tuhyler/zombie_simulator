using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static UnityEditor.Progress;

public class City : MonoBehaviour
{
    //city graphics
    [SerializeField]
    public GameObject cityNameMap, exclamationPoint, cityBase, battleIcon;
    [SerializeField]
    public ImprovementDataSO housingData;
    //private int housingCenterCount;
    //private CityImprovement initialHouse;
    //private GameObject currentHouse;
    //private CityBuilderManager cityBuilderManager;
    private List<MeshFilter> cityMeshFilters = new();
    public List<MeshFilter> CityMeshFilters { get { return cityMeshFilters; } }
    private Dictionary<string, (MeshFilter[], GameObject)> buildingMeshes = new(); //for removing meshes
    private Dictionary<Vector3Int, (MeshFilter[], GameObject)> improvementMeshes = new();
    private List<CityImprovement> improvementList = new();
    public List<CityImprovement> ImprovementList { get { return improvementList; } }

    //private SelectionHighlight selectionHighlight;
    //public SelectionHighlight SelectionHighlight { get { return selectionHighlight; } }
    [SerializeField]
    public Renderer cityRenderer;
    
    [SerializeField]
    public Transform subTransform;

    [SerializeField]
    private CityNameField cityNameField;

    //particle systems
    [SerializeField]
    private ParticleSystem heavenHighlight, hellHighlight, resourceSplash, lightBullet, fire;

    [SerializeField]
    private SpriteRenderer minimapIcon;

    [SerializeField]
    public Sprite campIcon, cityIcon;

    //city info
    [HideInInspector]
    public string cityName;
    
    [HideInInspector]
    public Vector3Int cityLoc;
    [HideInInspector]
    public bool hasWater, hasFreshWater, reachedWaterLimit, hasRocksFlat, hasRocksHill, hasTrees, hasFood, hasWool, hasSilk, hasClay, activeCity, hasHarbor, hasBarracks, highlighted, harborTraining,
        hasMarket, isNamed, stopCycle;

    [HideInInspector]
    public UIPersonalResourceInfoPanel uiCityResourceInfoPanel;

    [HideInInspector]
    public Vector3Int harborLocation, barracksLocation;

    [HideInInspector]
    public Army army;

    [HideInInspector]
    public MapWorld world;

    private ResourceManager resourceManager;
    public ResourceManager ResourceManager { get { return resourceManager; } }

    [HideInInspector]
    public Dictionary<string, Vector3Int> singleBuildImprovementsBuildingsDict = new();

    [HideInInspector]
    public CityPopulation cityPop;

    //foodConsumed info
    public int unitFoodConsumptionPerMinute = 1, secondsTillGrowthCheck = 60, growthFood = 3; //how much foodConsumed one unit eats per turn
    private UITimeProgressBar uiTimeProgressBar;
    private int countDownTimer;
    private Coroutine co;
    private WaitForSeconds foodConsumptionWait = new(1);
    ResourceValue foodValue;

	//housingInfo
	[HideInInspector]
	public bool housingLocsAtMax;
	//private int[] housingIndex = new[] { 0, 0, 0, 0 };
	private List<Vector3> housingLocs = new() { new Vector3(0.7f, 0, 1.2f), new Vector3(-1.2f, 0, 0.7f), new Vector3(-1.2f, 0, -0.7f), new Vector3(0.7f, 0, -1.2f) };
	private int housingCount = 0, houseCount, upgradeIndex;
    public int HousingCount { get { return housingCount; } set { housingCount = value; } }
    private CityImprovement[] housingArray = new CityImprovement[4];
    [HideInInspector]
    public int waterCount;

    //resource info
    public float workEthic = 1f;
    public float improvementWorkEthic;
    public float wonderWorkEthic;
    public int warehouseStorageLimit = 200;
    private TradeRouteManager tradeRouteWaiter;
    private ResourceType resourceWaiter = ResourceType.None;
    private Queue<Unit> waitList = new(), seaWaitList = new();
    private Dictionary<ResourceType, int> resourcesWorkedDict = new();
    [HideInInspector]
    public Dictionary<ResourceType, int> resourceGridDict = new(); //order of resources shown

    //world resource info
    //private int goldPerMinute;
    //public int GetGoldPerMinute { get { return goldPerMinute; } }
    private int researchPerMinute;
    public int GetResearchPerMinute { get { return researchPerMinute; } }

    //resource priorities
    [HideInInspector]
    public bool autoGrow;
    private bool autoAssignLabor;
    public bool AutoAssignLabor { get { return autoAssignLabor; } set { autoAssignLabor = value; } }
    private List<ResourceType> resourcePriorities = new();
    public List<ResourceType> ResourcePriorities { get { return resourcePriorities; } set { resourcePriorities = value; } }

    //for upgrading traders
    [HideInInspector]
    public List<Unit> tradersHere = new();

    //audio
    //[SerializeField]
    //private AudioClip popGainClip, popLoseClip;
    private AudioSource audioSource;

	//stored queue items
	[HideInInspector]
    public Dictionary<Vector3Int, QueueItem> improvementQueueDict = new();
    [HideInInspector]
    public List<QueueItem> queueItemList = new();
	//[HideInInspector]
	//public Dictionary<string, bool> buildingQueueDict = new();
	//[HideInInspector]
 //   public List<UIQueueItem> savedQueueItems = new();
 //   [HideInInspector]
 //   public List<string> savedQueueItemsNames = new();
 //   [HideInInspector]
 //   public Dictionary<string, GameObject> buildingQueueGhostDict = new();
 //   [HideInInspector]
 //   public List<Vector3Int> improvementQueueLocs = new();

    //private SelectionHighlight highlight; //Highlight doesn't work on city name text

    private void Awake()
    {
        //selectionCircle.GetComponent<MeshRenderer>().enabled = false;
        //world = FindObjectOfType<MapWorld>();
        cityPop = GetComponent<CityPopulation>();
        audioSource = GetComponent<AudioSource>();
        army = GetComponent<Army>();
        //selectionHighlight = GetComponentInChildren<SelectionHighlight>();
        resourceManager = GetComponent<ResourceManager>();
        //resourceManager.ResourceStorageLimit = warehouseStorageLimit;
        //resourceProducer = GetComponent<ResourceProducer>();
        resourceManager.SetCity(this);
        //resourceProducer.SetResourceManager(resourceManager);

        cityLoc = Vector3Int.RoundToInt(transform.position);

        SetProgressTimeBar();
        //highlight = GetComponent<SelectionHighlight>();

        //resourceManager.ResourceDict = new(world.GetBlankResourceDict());
        //resourceManager.ResourcePriceDict = new(world.GetDefaultResourcePrices());
        //resourceManager.ResourceSellDict = new(world.GetBoolResourceDict());
        //resourceManager.ResourceMinHoldDict = new(world.GetBlankResourceDict());
        //resourceManager.ResourceSellHistoryDict = new(world.GetBlankResourceDict());
        cityNameMap.GetComponentInChildren<TMP_Text>().outlineWidth = 0.35f;
        cityNameMap.GetComponentInChildren<TMP_Text>().outlineColor = Color.black;

		foodValue.resourceType = ResourceType.Food;
		foodValue.resourceAmount = growthFood;
	}

    private void Start()
    {
        //if (cityPop.CurrentPop >= 1 || army.UnitsInArmy.Count > 0)
        //{
        //    StartGrowthCycle(false);
        //}

        resourceManager.ModifyResourceConsumptionPerMinute(ResourceType.Food, cityPop.CurrentPop * unitFoodConsumptionPerMinute);

        cityNameField.ToggleVisibility(false);
        InstantiateParticleSystems();
        //Physics.IgnoreLayerCollision(6,7);

        //int i = 0;
        //foreach (ResourceType type in resourceManager.ResourceDict.Keys)
        //{
        //    int amount = resourceManager.ResourceDict[type];
        //    if (amount > 0)
        //    {
        //        resourceGridDict[type] = i;
        //        i++;
        //    }

        //}
    }

	public void PlayPopGainAudio()
	{
		audioSource.clip = world.cityBuilderManager.popGainClip;
		audioSource.Play();
	}

	public void PlayPopLossAudio()
	{
		audioSource.clip = world.cityBuilderManager.popLoseClip;
		audioSource.Play();
	}

	public void SetWorld(MapWorld world)
    {
        this.world = world;
		world.CheckCityPermanentChanges(this);
		army.SetWorld(world);
		resourceManager.ResourceDict = new(world.GetBlankResourceDict());
        resourceManager.ResourcePriceDict = new(world.GetDefaultResourcePrices());
        resourceManager.ResourceSellDict = new(world.GetBoolResourceDict());
        resourceManager.ResourceMinHoldDict = new(world.GetBlankResourceDict());
        resourceManager.ResourceSellHistoryDict = new(world.GetBlankResourceDict());
        resourceManager.PrepareResourceDictionary();
        resourceManager.SetInitialResourceValues();
        resourceManager.SetPrices();

        int i = 0;
        foreach (ResourceType type in resourceManager.ResourceDict.Keys)
        {
            int amount = resourceManager.ResourceDict[type];
            if (amount > 0)
            {
                resourceGridDict[type] = i;
                i++;
            }

        }
    }

    public void InstantiateParticleSystems()
    {
        bool isHill = world.GetTerrainDataAt(cityLoc).isHill;
        
        Vector3 pos = transform.position;
        pos.y = isHill ? .8f : .2f;
        resourceSplash = Instantiate(resourceSplash, pos, Quaternion.Euler(-90, 0, 0));
        resourceSplash.transform.parent = transform;
        resourceSplash.Pause();
        pos.y = 8f;
        lightBullet = Instantiate(lightBullet, pos, Quaternion.Euler(90, 0, 0));
        lightBullet.transform.parent = transform;
        lightBullet.Pause();
        pos.y = isHill ? 3.6f : 3f;
        heavenHighlight = Instantiate(heavenHighlight, pos, Quaternion.identity);
        heavenHighlight.transform.parent = transform;
        heavenHighlight.Pause();
        hellHighlight = Instantiate(hellHighlight, pos, Quaternion.identity);
        hellHighlight.transform.parent = transform;
        hellHighlight.Pause();

    }

    public void LightFire(bool isHill)
    {
        Vector3 loc = cityLoc;
        loc.y += 0.1f;
        loc.z += -0.6f;
        Vector3 cityBaseLoc = cityBase.transform.localPosition;

        if (world.IsRoadOnTerrain(cityLoc + new Vector3Int(0, 0, -3)))
        {
            loc.y += 0.1f;
            cityBaseLoc.y += 0.1f;
            cityBase.transform.localPosition = cityBaseLoc;
        }

        if (isHill)
        {
            loc.y += .5f;
            cityBaseLoc.y += .5f;
            cityBase.transform.localPosition = cityBaseLoc;
        }

        fire = Instantiate(fire, loc, Quaternion.Euler(-90, 0, 0));
        fire.transform.SetParent(world.psHolder, false);
        fire.Play();
    }

    private void ReigniteFire()
    {
        bool alreadyRoad = fire.transform.position.y == 0.2f || fire.transform.position.y == 0.8f; 
        fire.Play();
        cityBase.gameObject.SetActive(true);

        if (!alreadyRoad && world.IsRoadOnTerrain(cityLoc + new Vector3Int(0, 0, -3))) //in case road was added since growth
        {
            Vector3 fireLoc = fire.transform.position;
            fireLoc.y += 0.1f;
            fire.transform.position = fireLoc;
            Vector3 cityBaseLoc = cityBase.transform.localPosition;
            cityBaseLoc.y += 0.1f;
            cityBase.transform.localPosition = cityBaseLoc;
        }
        else if (alreadyRoad && !world.IsRoadOnTerrain(cityLoc + new Vector3Int(0, 0, -3))) //in case road was deleted since growth
        {
            Vector3 fireLoc = fire.transform.position;
            fireLoc.y -= 0.1f;
            fire.transform.position = fireLoc;
            Vector3 cityBaseLoc = cityBase.transform.localPosition;
            cityBaseLoc.y -= 0.1f;
            cityBase.transform.localPosition = cityBaseLoc;
        }
    }
    
    public void ExtinguishFire()
    {
        fire.Stop();
        cityBase.gameObject.SetActive(false);    
    }

    public void DestroyFire()
    {
        Destroy(fire.gameObject);
    }

    public void RepositionFire()
    {
        Vector3 cityBaseLoc = cityBase.transform.localPosition;
        Vector3 fireLoc = fire.transform.position;
        cityBaseLoc.y += 0.1f;
        if (world.GetTerrainDataAt(cityLoc).isHill)
        {
            cityBaseLoc.y += 0.1f;
            fireLoc.y += 0.1f;
        }

        fireLoc.y += 0.1f;
        cityBase.transform.localPosition = cityBaseLoc;
        fire.transform.position = fireLoc;
        //CombineFire();
    }

    //public void SetCityBuilderManager(CityBuilderManager cityBuilderManager)
    //{
    //    this.cityBuilderManager = cityBuilderManager;
    //}

    public void UpdateResourceInfo()
    {
        world.cityBuilderManager.UpdateResourceInfo();
    }

    public void AddToMeshFilterList(GameObject go, MeshFilter[] meshFilter, bool building, Vector3Int loc, string name = "")
    {
        int count = meshFilter.Length;
        //List<MeshFilter> meshFilterList = new();
        
        for (int i = 0; i < count; i++)
        {
            //if (meshFilter[i].name == "Animation")
            //    continue;

            //meshFilterList.Add(meshFilter[i]);
            cityMeshFilters.Add(meshFilter[i]);
        }

        if (building)
            buildingMeshes[name] = (meshFilter, go);
        else
            improvementMeshes[loc] = (meshFilter, go);
    }

    public void RemoveFromMeshFilterList(bool building, Vector3Int loc, string name = "")
    {
        GameObject go;
        MeshFilter[] meshFilter;

        if (building)
        {
            (meshFilter, go) = buildingMeshes[name];
            buildingMeshes.Remove(name);
        }
        else
        {
            (meshFilter, go) = improvementMeshes[loc];
            improvementMeshes.Remove(loc);
        }
        
        int count = meshFilter.Length;
        for (int i = 0; i < count; i++)
            cityMeshFilters.Remove(meshFilter[i]);

        Destroy(go);
    }

    //Reassigning improvement meshes to the orphan transform in the heirarchy
    public void ReassignMeshes(Transform orphan, Dictionary<Vector3Int, (MeshFilter[], GameObject)> meshDict, List<MeshFilter> meshList)
    {
        foreach (Vector3Int loc in improvementMeshes.Keys)
        {
            (MeshFilter[] meshes, GameObject go)= improvementMeshes[loc];

            int count = meshes.Length;
            for (int i = 0;i < count; i++)
                meshList.Add(meshes[i]);

            meshDict[loc] = (meshes, go);
            go.transform.parent = orphan;
        }
    }

    public void SetNewMeshCity(Vector3Int loc, Dictionary<Vector3Int, (MeshFilter[], GameObject)> meshDict, List<MeshFilter> meshList)
    {
        (MeshFilter[] meshes, GameObject go) = meshDict[loc];
        meshDict.Remove(loc);

        int count = meshes.Length;
        for (int i = 0; i < count; i++)
        {
            meshList.Remove(meshes[i]);
            cityMeshFilters.Add(meshes[i]);
            meshes[i].gameObject.SetActive(false);
        }

        improvementMeshes[loc] = (meshes, go);
        go.transform.parent = transform;
    }

    public void AddToImprovementList(CityImprovement improvement)
    {
        improvementList.Add(improvement);
    }

    public void RemoveFromImprovementList(CityImprovement improvement)
    {
        improvementList.Remove(improvement);
    }

    public bool WorldResearchingCheck()
    {
        return world.researching;
    }

    //public void RestartResearch()
    //{
    //    resourceManager.CheckProducerUnloadResearchWaitList();
    //}

    public void RestartProduction()
    {
        resourceManager.CheckProducerResourceWaitList(ResourceType.Gold);
    }

    public void AddToWorldResearchWaitList(ResourceProducer producer)
    {
        world.AddToResearchWaitList(producer);
    }

    public void AddToWorldGoldWaitList(bool trader = false)
    {
        world.AddToGoldCityWaitList(this, trader);
    }

    //private void EnableHighlight()
    //{
    //    cityNameField.EnableHighlight();
    //}

    //private void DisableHighlight()
    //{
    //    cityNameField.DisableHighlight();
    //}

    public bool CheckCityName(string cityName)
    {
        return world.IsCityNameTaken(cityName);
    }

    public void SetNewCityName()
    {
        bool approvedName = false;
        int cityCount = world.cityCount;
        string cityName = "";

        while (!approvedName)
        {
            cityName = "Camp " + cityCount.ToString();

            if (!world.CheckCityName(cityName))
            {
                approvedName = true;
            }

            cityCount++;
        }

        cityNameMap.GetComponentInChildren<TMP_Text>().text = cityName;
        SetCityNameFieldSize(cityName);
        SetCityName(cityName);
        SetCityPop();
        AddCityNameToWorld();
    }

    private void SetCityNameFieldSize(string cityName)
    {
        cityNameField.SetCityNameFieldSize(cityName);
    }

    public void SetCityName(string cityName)
    {
        this.cityName = cityName;
        cityNameField.SetCityName(cityName);
    }

    public void AddCityNameToWorld()
    {
        world.AddCityName(cityName, cityLoc);
        world.AddTradeLoc(cityLoc, cityName);
    }

    private void SetCityPop()
    {
        cityNameField.SetCityPop(cityPop.CurrentPop);
    }

    public void RemoveCityName()
    {
        world.RemoveCityName(cityLoc);
    }

    public void UpdateCityName(string newCityName)
    {
        cityNameMap.GetComponentInChildren<TMP_Text>().text = newCityName;
        if (!world.showingMap)
            cityNameMap.SetActive(false);
        SetCityNameFieldSize(newCityName);
        SetCityName(newCityName);
        AddCityNameToWorld();
    }

    ////update city info after changing foodConsumed info
    //public void UpdateCityPopInfo(int foodConsumption)
    //{
    //    //cityPopText.text = cityPop.CurrentPop.ToString();
    //    //cityLaborText.text = cityPop.UnusedLabor.ToString();
    //    resourceManager.ModifyResourceConsumptionPerMinute(ResourceType.Food, foodConsumption);
    //    //float foodPerMinute = resourceManager.GetResourceGenerationValues(ResourceType.Food);
    //    //int foodStorage = foodConsumptionPerMinute;

    //    //if (foodPerMinute > 0)
    //    //{
    //    //    minutesTillGrowth = Mathf.CeilToInt(((float)resourceManager.FoodGrowthLimit - foodStorage) / foodPerMinute).ToString();
    //    //}
    //    //else if (foodPerMinute < 0) 
    //    //{
    //    //    minutesTillGrowth = Mathf.FloorToInt(foodStorage / foodPerMinute).ToString(); //maybe take absolute value, change color to red?
    //    //}
    //    //else if (foodPerMinute == 0)
    //    //{
    //    //    minutesTillGrowth = "-";
    //    //}
    //}

    public void SetHouse(ImprovementDataSO housingData, Vector3Int cityLoc, bool isHill, bool upgrade)
    {
        houseCount++;
        if (autoGrow && cityPop.CurrentPop == 0 && resourceManager.ResourceDict[ResourceType.Food] >= growthFood) //growing if waiting on hosue to grow
            PopulationGrowthCheck(false , 1);

        //seeing which house will be build first
        Vector3 houseLoc = cityLoc;
        int index;
        if (upgrade)
            index = upgradeIndex;
        else
            index = Array.FindIndex(housingArray, x => x == null);
        houseLoc += housingLocs[index];
        //housingCenterCount++;
        if (houseCount == housingLocs.Count)
            housingLocsAtMax = true;

        if (isHill)
            houseLoc.y += housingData.hillAdjustment;

        Vector3 direction = houseLoc - cityLoc;
        direction.y = 0;
        Quaternion endRotation = Quaternion.LookRotation(direction, Vector3.up);

        GameObject housing = Instantiate(housingData.prefab, houseLoc, endRotation); //underground temporarily
        //housing.transform.position = houseLoc;
        CityImprovement improvement = housing.GetComponent<CityImprovement>();
        housingArray[index] = improvement;
        HouseLightCheck();
        //improvement.DestroyUpgradeSplash();
        improvement.loc = cityLoc;
        improvement.housingIndex = index;
        //if (index == 0)
        //{
        //    initialHouse = improvement;
        //    improvement.initialCityHouse = true;
        //}
        //else
        resourceManager.SpendResource(housingData.improvementCost, cityLoc);
        housingCount += housingData.housingIncrease;
        string buildingName = housingData.improvementName + index.ToString();
		improvement.PlaySmokeSplashBuilding();
		world.SetCityBuilding(improvement, housingData, cityLoc, housing, this, buildingName);
        //for tweening
        housing.transform.localScale = Vector3.zero;
        LeanTween.scale(housing, new Vector3(1.5f, 1.5f, 1.5f), 0.25f).setEase(LeanTweenType.easeOutBack).setOnComplete(()=> { 
            world.cityBuilderManager.CombineMeshes(this, subTransform, world.cityBuilderManager.upgradingImprovement); improvement.SetInactive(); world.cityBuilderManager.ToggleBuildingHighlight(true);
        });
    }

    //for loading up hosuing
	public void LoadHouse(ImprovementDataSO housingData, Vector3Int cityLoc, bool isHill, int houseIndex)
	{
		houseCount++;

		//seeing which house will be build first
		Vector3 houseLoc = cityLoc;
		int index = houseIndex;

		houseLoc += housingLocs[index];
		
		if (houseCount == housingLocs.Count)
			housingLocsAtMax = true;

		if (isHill)
			houseLoc.y += housingData.hillAdjustment;

		Vector3 direction = houseLoc - cityLoc;
		direction.y = 0;
		Quaternion endRotation = Quaternion.LookRotation(direction, Vector3.up);

		GameObject housing = Instantiate(housingData.prefab, houseLoc, endRotation); //underground temporarily
																					 //housing.transform.position = houseLoc;
		CityImprovement improvement = housing.GetComponent<CityImprovement>();
		housingArray[index] = improvement;
		HouseLightCheck();
		//improvement.DestroyUpgradeSplash();
		improvement.loc = cityLoc;
		improvement.housingIndex = index;

		housingCount += housingData.housingIncrease;
		string buildingName = housingData.improvementName + index.ToString();
		world.SetCityBuilding(improvement, housingData, cityLoc, housing, this, buildingName);
		
		world.cityBuilderManager.CombineMeshes(this, subTransform, false);
		improvement.SetInactive();
	}


	public void CombineFire()
    {
		world.cityBuilderManager.CombineMeshes(this, subTransform, world.cityBuilderManager.upgradingImprovement);
	}

    public string DecreaseHousingCount(int index)
    {
        houseCount--;
        upgradeIndex = index;
        housingArray[index] = null;
        housingLocsAtMax = false;
        HouseLightCheck();

        return housingData.improvementName + index.ToString();
    }

    //public int GetGrowthNumber()
    //{
    //    if (cityPop.CurrentPop == 0)
    //        return growthFood;
    //    else
    //        return unitFoodConsumptionPerMinute;
    //}

    public void PopulationGrowthCheck(bool joinCity, int amount)
    {        
        int prevPop = cityPop.CurrentPop;
        PlayPopGainAudio();

        cityPop.IncreasePopulationAndLabor(amount);
        housingCount -= amount;
        waterCount -= amount;
        heavenHighlight.Play();
        SetCityPop();

        if (waterCount <= 0)
            reachedWaterLimit = true;

        if (activeCity && world.cityBuilderManager.uiUnitBuilder.activeStatus)
            world.cityBuilderManager.uiUnitBuilder.UpdateBuildOptions(ResourceType.Labor, prevPop, cityPop.CurrentPop, true, resourceManager);

		//if (cityPop.CurrentPop > 1)
		//    resourceManager.IncreaseFoodConsumptionPerTurn(true);

		if (activeCity)
		{
            world.cityBuilderManager.uiInfoPanelCity.SetGrowthData(this);
		}

        resourceManager.ModifyResourceConsumptionPerMinute(ResourceType.Food, unitFoodConsumptionPerMinute);
        if (!joinCity) //spend food to grow
        {
			List<ResourceValue> valueList = new() { foodValue };
            resourceManager.ConsumeResources(valueList, 1, cityLoc, false, true);
        }

        for (int i = 0; i < amount; i++)
        {
            if (autoAssignLabor)
            {
                AutoAssignmentsForLabor();
                if (activeCity)
                {
                    world.cityBuilderManager.UpdateCityLaborUIs();
                }
            }

            if (cityPop.CurrentPop <= 4)
            {
                HouseLightCheck();

                if (cityPop.CurrentPop == 1)
                {
                    if (activeCity)
                    {
                        //world.cityBuilderManager.uiInfoPanelCity.ToggleVisibility(true);
                        world.cityBuilderManager.uiLaborAssignment.ShowUI(this, 0, false);
                        CityGrowthProgressBarSetActive(true);
                        world.cityBuilderManager.abandonCityButton.interactable = false;
                        //world.cityBuilderManager.SetGrowthNumber(unitFoodConsumptionPerMinute);
                    }
                    resourceManager.SellResources();
                    StartGrowthCycle(false);
                }
                else if (cityPop.CurrentPop == 4)
                {
                    minimapIcon.sprite = cityIcon;
                    ExtinguishFire();
                }
            }
            else
            {
                if (!isNamed)
                {
                    isNamed = true;
                    string newName = world.GetNextCityName();

					RemoveCityName();
					UpdateCityName(newName);

                    if (activeCity)
    					world.cityBuilderManager.uiInfoPanelCity.UpdateCityName(newName);
				}

                cityNameField.ToggleVisibility(true);
            }
        }
    }

    public void PopulationDeclineCheck(bool any, bool building)
    {
        int prevPop = cityPop.CurrentPop;

        cityPop.CurrentPop--;
        housingCount++;
        waterCount++;
		
        if (!building)
        {
            PlayPopLossAudio();

            if (world.GetTerrainDataAt(cityLoc).isHill)
            {
    		    Vector3 loc = cityLoc;
                loc.y += .6f;
                PlayHellHighlight(loc);
            }
            else
            {
                PlayHellHighlight(cityLoc);
            }
        }

        SetCityPop();

		if (waterCount > 0)
			reachedWaterLimit = false;

		if (activeCity && world.cityBuilderManager.uiUnitBuilder.activeStatus)
            world.cityBuilderManager.uiUnitBuilder.UpdateBuildOptions(ResourceType.Labor, prevPop, cityPop.CurrentPop, false, resourceManager);

		if (activeCity)
		{
            world.cityBuilderManager.uiInfoPanelCity.SetGrowthData(this);
		}

		if (cityPop.CurrentPop <= 3)
        {
            HouseLightCheck();
			cityNameField.ToggleVisibility(false);

			if (cityPop.CurrentPop == 0 && army.UnitsInArmy.Count == 0)
            {
                if (co != null)
                {
                    StopCoroutine(co);
                    stopCycle = true;
                    //countDownTimer = secondsTillGrowthCheck;
                    co = null;
                }

                if (activeCity)
                {
                    CityGrowthProgressBarSetActive(false);
                    world.cityBuilderManager.abandonCityButton.interactable = true;
                }
            }
            else if (cityPop.CurrentPop == 3)
            {
				minimapIcon.sprite = campIcon;
				ReigniteFire();
            }
        }

        if (cityPop.UnusedLabor > 0) //if unused labor, get rid of first
            cityPop.UnusedLabor--;
        else
            RemoveRandomFieldLaborer(any);

        resourceManager.ModifyResourceConsumptionPerMinute(ResourceType.Food, -unitFoodConsumptionPerMinute);
    }

    public void PlayHellHighlight(Vector3 loc)
    {
        loc.y += 3f;
        hellHighlight.transform.position = loc;
        hellHighlight.Play();
    }

    private void HouseLightCheck()
    {
		int lightingCount = 0;
        bool lightsOn = true;

		for (int j = 0; j < housingArray.Length; j++)
		{
            if (lightingCount >= cityPop.CurrentPop)
                lightsOn = false;

			if (housingArray[j] != null)
				housingArray[j].ToggleLights(lightsOn);
			else
				continue;

			lightingCount++;
		}
	}

    private void RemoveRandomFieldLaborer(bool any)
    {
        System.Random random = new();
        List<Vector3Int> workedTiles = world.GetWorkedCityRadiusFor(cityLoc);

        //below is giving every labor in any tile equal chance of being chosen
        int currentLabor = 0;
        Dictionary<int, Vector3Int> laborByTile = new();
        foreach (Vector3Int tile in workedTiles)
        {
            if (!any && world.GetCityDevelopment(tile).GetImprovementData.housingIncrease > 0)
                continue;
            
            int prevLabor = currentLabor;
            currentLabor += world.GetCurrentLaborForTile(tile);
            for (int i = prevLabor; i < currentLabor; i++)
            {
                laborByTile[currentLabor] = tile;
            }
        }

        Vector3Int chosenTile = laborByTile[random.Next(currentLabor)];
        //above is giving labor in any tile equal chance of being chosen
        //Vector3Int chosenTile = workedTiles[random.Next(workedTiles.Count)]; //equal chance of being chosen, regardless of labor size

        int labor = world.GetCurrentLaborForTile(chosenTile);
        labor--;

        if (labor == 0) //removing from world dicts when zeroed out
        {
            world.RemoveFromCurrentWorked(chosenTile);
            world.RemoveFromCityLabor(chosenTile);
            //resourceManager.RemoveKeyFromGenerationDict(chosenTile);
        }
        else
        {
            world.AddToCurrentFieldLabor(chosenTile, labor);
        }
    }

    //private void RemoveRandomCityLaborer(System.Random random)
    //{
    //    List<string> buildingNames = world.GetBuildingListForCity(cityLoc);

    //    //below is giving every labor in any building equal chance of being chosen
    //    int currentLabor = 0;
    //    Dictionary<int, string> laborByBuilding = new();
    //    foreach (string buildingName in buildingNames)
    //    {
    //        int prevLabor = currentLabor;
    //        currentLabor += world.GetCurrentLaborForBuilding(cityLoc, buildingName);
    //        for (int i = prevLabor; i < currentLabor; i++)
    //        {
    //            laborByBuilding[currentLabor] = buildingName;
    //        }
    //    }

    //    string chosenBuildingName = laborByBuilding[random.Next(currentLabor)];
    //    //above is giving labor in any building equal chance of being chosen

    //    //string chosenBuildingName = buildingNames[random.Next(buildingNames.Count)]; //equal chance of being chosen, regardless of labor size
        
    //    int labor = world.GetCurrentLaborForBuilding(cityLoc, chosenBuildingName);
    //    labor--;

    //    if (labor == 0) //removing from world dicts when zeroed out
    //    {
    //        world.RemoveFromBuildingCurrentWorked(cityLoc, chosenBuildingName);
    //        //resourceManager.RemoveKeyFromBuildingGenerationDict(chosenBuildingName);
    //    }
    //    else
    //    {
    //        world.AddToCurrentBuildingLabor(cityLoc, chosenBuildingName, labor);
    //    }
    //}

    public void SetWaiter(TradeRouteManager tradeRouteManager, ResourceType resourceType = ResourceType.None)
    {
        tradeRouteWaiter = tradeRouteManager;
        resourceWaiter = resourceType;
    }

    public void CheckResourceWaiter(ResourceType resourceType)
    {
        if (tradeRouteWaiter != null && resourceWaiter == resourceType)
        {
            tradeRouteWaiter.resourceCheck = false;
            tradeRouteWaiter = null;
            resourceWaiter = ResourceType.None;
        }
    }

    public void CheckLimitWaiter()
    {
        if (tradeRouteWaiter != null && resourceWaiter == ResourceType.None)
        {
            tradeRouteWaiter.resourceCheck = false;
            tradeRouteWaiter = null;
        }
    }

    public void AddToWaitList(Unit unit)
    {
        if (unit.bySea)
        {
            if (!seaWaitList.Contains(unit))
                seaWaitList.Enqueue(unit);
        }
        else
        {
            if (!waitList.Contains(unit))
                waitList.Enqueue(unit);
        }
    }

    public void RemoveFromWaitList(Unit unit)
    {
        List<Unit> waitListList = unit.bySea ? seaWaitList.ToList() : waitList.ToList();
        
        if (!waitListList.Contains(unit))
        {
            if (unit.bySea)
                CheckSeaQueue();
            else
                CheckQueue();

            return;
        }

        int index = waitListList.IndexOf(unit);
        waitListList.Remove(unit);

        int j = 0;
        for (int i = index; i < waitListList.Count; i++)
        {
            j++;
            waitListList[i].waitingCo = StartCoroutine(waitListList[i].MoveUpInLine(j));
        }

        if (unit.bySea)
            seaWaitList = new Queue<Unit>(waitListList);
        else
			waitList = new Queue<Unit>(waitListList);
	}

    public void CheckQueue()
    {
        if (waitList.Count > 0)
        {
            waitList.Dequeue().ExitLine();
        }

        if (waitList.Count > 0)
        {
            int i = 0;
            foreach(Unit unit in waitList)
            {
                i++;
                unit.waitingCo = StartCoroutine(unit.MoveUpInLine(i));
            }
        }
    }

    public void CheckSeaQueue()
    {
		if (seaWaitList.Count > 0)
		{
			seaWaitList.Dequeue().ExitLine();
		}

		if (seaWaitList.Count > 0)
		{
			int i = 0;
			foreach (Unit unit in seaWaitList)
			{
				i++;
				unit.waitingCo = StartCoroutine(unit.MoveUpInLine(i));
			}
		}
	}

    public void PlayResourceSplash()
    {
        world.cityBuilderManager.PlayRingAudio();
        resourceSplash.Play();
    }

    public void PlayLightBullet()
    {
        lightBullet.Play();
    }

    public void AddToGrid(ResourceType type)
    {
        resourceGridDict[type] = resourceGridDict.Count;
    }

    public void ReshuffleGrid()
    {
        int i = 0;

        //re-sorting
        Dictionary<ResourceType, int> myDict = resourceGridDict.OrderBy(d => d.Value).ToDictionary(x => x.Key, x => x.Value );

        List<ResourceType> types = new List<ResourceType>(myDict.Keys);

        foreach (ResourceType type in types)
        {
            int amount = resourceManager.ResourceDict[type];
            if (amount > 0  || type == ResourceType.Food)
            {
                resourceGridDict[type] = i;
                i++;
            }
            else
            {
                resourceGridDict.Remove(type);
            }
        }
    }

    public List<ResourceValue> GetResourceValues()
    {
        List<ResourceValue> values = new();

        foreach (ResourceType type in resourceGridDict.Keys)
        {
            ResourceValue value;
            value.resourceType = type;
        }

        return values;
    }

    //world resource manager
    public bool CheckIfWorldResource(ResourceType resourceType) //seeing if its world resource
    {
        return world.WorldResourcePrep().Contains(resourceType);
    }

    public void UpdateWorldResourceGeneration(ResourceType resourceType, float diffAmount, bool add)
    {
        world.UpdateWorldResourceGeneration(resourceType, diffAmount, add);

        if (resourceType == ResourceType.Research)
        {
            if (add)
                researchPerMinute += Mathf.RoundToInt(diffAmount);
            else
                researchPerMinute += Mathf.RoundToInt(diffAmount);
        }
    }

    public void CheckBuildOptionsResource(ResourceType type, int prevAmount, int currentAmount, bool pos)
    {
        if (world.cityBuilderManager.buildOptionsActive)
            world.cityBuilderManager.activeBuilderHandler.UpdateBuildOptions(type, prevAmount, currentAmount, pos, resourceManager);
    }

    public void ChangeResourcesWorked(ResourceType resourceType, int laborChange)
    {
        if (resourcesWorkedDict.ContainsKey(resourceType))
        {
            resourcesWorkedDict[resourceType] += laborChange;
        }
        else
        {
            resourcesWorkedDict[resourceType] = laborChange;
        }
    }

    public void RemoveFromResourcesWorked(ResourceType resourceType)
    {
        resourcesWorkedDict.Remove(resourceType);
    }

    public bool CheckResourcesWorkedExists(ResourceType resourceType)
    {
        return resourcesWorkedDict.ContainsKey(resourceType);
    }

    public List<ResourceType> GetResourcesWorked()
    {
        List<ResourceType> resourceKeys = resourcesWorkedDict.Keys.ToList();
        return resourceKeys;
    }

    public int GetResourcesWorkedResourceCount(ResourceType resourceType)
    {
        return resourcesWorkedDict[resourceType];
    }

    public void UpdateWorldResources(ResourceType resourceType, int amount)
    {
        world.UpdateWorldResources(resourceType, amount);
    }

    public bool CheckWorldGold(int amount)
    {
        return world.CheckWorldGold(amount);
    }

    public int GetWorldGoldLevel()
    {
        return world.GetWorldGoldLevel();
    }

 //   public void StartFoodCycle()
 //   {
	//	if (activeCity)
 //       {
	//		CityGrowthProgressBarSetActive(true);
	//		world.cityBuilderManager.abandonCityButton.interactable = false;
	//		world.cityBuilderManager.SetGrowthNumber(unitFoodConsumptionPerMinute);
	//	}

	//	co = StartCoroutine(FoodConsumptionCoroutine());
	//}

    public void StopGrowthCycle()
    {
        if (co != null)
        {
            StopCoroutine(co);
            co = null;
		}

        stopCycle = true;

        if (activeCity)
            CityGrowthProgressBarSetActive(false);
    }

    public void StartGrowthCycle(bool load)
    {
		if (activeCity)
		{
			CityGrowthProgressBarSetActive(true);
			world.cityBuilderManager.abandonCityButton.interactable = false;
			//world.cityBuilderManager.SetGrowthNumber(unitFoodConsumptionPerMinute);
		}

		if (!load)
            countDownTimer = secondsTillGrowthCheck;

		stopCycle = false;
		co = StartCoroutine(GrowthCycleCoroutine());
    }

    private void ContinueGrowthCycle()
    {
		countDownTimer = secondsTillGrowthCheck;
		co = StartCoroutine(GrowthCycleCoroutine());
	}

    //Time generator to consume food
    private IEnumerator GrowthCycleCoroutine()
    {
        if (activeCity)
        {
            uiTimeProgressBar.SetToZero();
            uiTimeProgressBar.SetTime(countDownTimer);
        }

        while (countDownTimer > 0)
        {
            yield return foodConsumptionWait;
            countDownTimer--;
            if (activeCity)
                uiTimeProgressBar.SetTime(countDownTimer);
        }

        //sell everything first
        world.UpdateWorldResources(ResourceType.Gold, resourceManager.SellResources());

        //for army maintenance
        if (army.atHome)
			resourceManager.ConsumeResources(army.GetArmyCycleCost(),1,barracksLocation, true);
        else
            army.cyclesGone++;
        //army.AddStagingCostToCycle();

        //food consumption
        resourceManager.CheckForPopGrowth();
        resourceManager.CycleCount++;

        Debug.Log(cityName + " is checking for growth");
        //countDownTimer = secondsTillGrowthCheck;

        if (stopCycle)
            stopCycle = false;
        else
            ContinueGrowthCycle();         
    }

    private void SetProgressTimeBar()
    {
        Vector3 cityPos = cityLoc;
        //cityPos.z -= 1.5f; //bottom center of tile
        cityPos.y += 1.5f; //above tile
        GameObject gameObject = Instantiate(GameAssets.Instance.cityGrowthProgressPrefab2, cityPos, Quaternion.Euler(90, 0, 0));
        uiTimeProgressBar = gameObject.GetComponent<UITimeProgressBar>();
        uiTimeProgressBar.SetAdditionalText = "Growth: ";
        uiTimeProgressBar.SetTimeProgressBarValue(secondsTillGrowthCheck);
    }

    public void HideCityGrowthProgressTimeBar()
    {
        uiTimeProgressBar.gameObject.SetActive(false);
    }

    public void CityGrowthProgressBarSetActive(bool v)
    {
        if (v && cityPop.CurrentPop == 0 && army.UnitsInArmy.Count == 0)
            return;

        uiTimeProgressBar.gameObject.SetActive(v);
        if (v)
        {
            uiTimeProgressBar.SetProgressBarMask(countDownTimer);
            uiTimeProgressBar.SetTime(countDownTimer);
        }
    }

    //for automatically assigning labor
    public void AutoAssignmentsForLabor()
    {
        //if (reassignAll)
        //{
        //    List<Vector3Int> workedTiles = world.GetWorkedCityRadiusFor(cityLoc, gameObject);

        //    foreach (Vector3Int tile in workedTiles)
        //    {
        //        ResourceProducer resourceProducer = world.GetResourceProducer(tile);
                
        //        int currentLabor = world.GetCurrentLaborForTile(tile);
        //        int maxLabor = world.GetMaxLaborForTile(tile);
        //        cityPop.UnusedLabor += currentLabor;
        //        cityPop.UsedLabor -= currentLabor;
        //        world.RemoveFromCurrentWorked(tile);

        //        if (currentLabor == maxLabor)  
        //            PlacesToWork++;
        //    }
        //}
        
        int unusedLabor = cityPop.UnusedLabor;
        bool maxxed;

        List<Vector3Int> laborLocs = world.GetPotentialLaborLocationsForCity(cityLoc);

        if (laborLocs.Count == 0)
            return;

        //Going through resource priorities first
        foreach (ResourceType resourceType in ResourcePriorities)
        {
            List<Vector3Int> laborTiles = new(laborLocs);
            
            foreach (Vector3Int laborTile in laborTiles)
            {
                if (unusedLabor == 0)
                    break;
                
                if (world.GetResourceProducer(laborTile).producedResource.resourceType == resourceType)
                {
                    (unusedLabor, maxxed) = IncreaseLaborCount(unusedLabor, laborTile);
                    if (maxxed)
                        laborLocs.Remove(laborTile);
                }
            }
        }

        //randomly assigning the rest
        if (laborLocs.Count > 0 && unusedLabor > 0)
        {
            RandomLaborAssignment(unusedLabor, laborLocs);
        }
    }

    private void RandomLaborAssignment(int labor, List<Vector3Int> locations)
    {
        System.Random random = new System.Random();
        
        for (int i = 0; i < labor; i++)
        {
            if (locations.Count == 0) 
                break;
            
            int tileIndex = random.Next(0, locations.Count);
            (int remainingLabor, bool maxxed) = IncreaseLaborCount(1, locations[tileIndex]);
            if (maxxed)
                locations.Remove(locations[tileIndex]);
        }
    }

    private (int, bool) IncreaseLaborCount(int laborChange, Vector3Int terrainLocation)
    {
        int labor = world.GetCurrentLaborForTile(terrainLocation);
        int maxLabor = world.GetMaxLaborForTile(terrainLocation);
        bool maxxed = false;
        int remainingLabor = 0;

        ResourceProducer resourceProducer = world.GetResourceProducer(terrainLocation); //cached all resource producers in dict

        int laborDiff = maxLabor - labor;

        if (laborDiff < laborChange)
        {
            remainingLabor = laborChange - laborDiff;
            laborChange = laborDiff;
        }

        //selectedCity.cityPop.GetSetFieldLaborers += laborChange;
        cityPop.UnusedLabor -= laborChange;
        cityPop.UsedLabor += laborChange;

        if (labor == 0) //assigning city to location if working for first time
        {
            world.GetCityDevelopment(terrainLocation).SetCity(this);
            world.AddToCityLabor(terrainLocation, cityLoc);
            resourceProducer.SetResourceManager(resourceManager);
            resourceProducer.UpdateResourceGenerationData();
			resourceProducer.StartProducing();
        }
        else
        {
            resourceProducer.AddLaborMidProduction();
        }

        labor += laborChange;
        if (labor == maxLabor)
        {
            //PlacesToWork--;
            maxxed = true;
        }
        resourceProducer.UpdateCurrentLaborData(labor);
        //resourceProducer.UpdateResourceGenerationData();

        //foreach (ResourceType resourceType in resourceProducer.producedResources)
        //{
        ResourceType resourceType = resourceProducer.producedResource.resourceType;

        for (int i = 0; i < laborChange; i++)
        {
            ChangeResourcesWorked(resourceType, 1);
            int totalResourceLabor = GetResourcesWorkedResourceCount(resourceType);
            if (activeCity && world.cityBuilderManager.uiLaborHandler.activeStatus)
                world.cityBuilderManager.uiLaborHandler.PlusMinusOneLabor(resourceType, totalResourceLabor, 1, ResourceManager.GetResourceGenerationValues(resourceType));
        }
        //}

        world.AddToCurrentFieldLabor(terrainLocation, labor);

        return (remainingLabor, maxxed);
    }

    public void UpdateCityBools(ResourceType type, RawResourceType rawResourceType = RawResourceType.None, TerrainType terrainType = TerrainType.Flatland)
    {
        if (rawResourceType == RawResourceType.Rocks)
        {
			if (terrainType == TerrainType.Flatland)
            {
                hasRocksFlat = false;
                
                foreach (Vector3Int tile in world.GetNeighborsFor(cityLoc, MapWorld.State.CITYRADIUS))
                {
				    TerrainData td = world.GetTerrainDataAt(tile);

                    if (td.rawResourceType == rawResourceType)
                    {
                        hasRocksFlat = true;
                        break;
                    }
			    }
            }
            else
            {
                hasRocksHill = false;
                
                foreach (Vector3Int tile in world.GetNeighborsFor(cityLoc, MapWorld.State.CITYRADIUS))
				{
					TerrainData td = world.GetTerrainDataAt(tile);

					if (td.rawResourceType == rawResourceType)
                    {
						hasRocksHill = true;
                        break;
                    }
				}
			}
		}
        else
        {
            CheckResourceType(type, false);
            
            foreach (Vector3Int tile in world.GetNeighborsFor(cityLoc, MapWorld.State.CITYRADIUS))
            {
                TerrainData td = world.GetTerrainDataAt(tile);

                if (td.resourceType == type)
                {
                    CheckResourceType(type, true);
                    break;
                }
            }
        }
    }

    private void CheckResourceType(ResourceType type, bool v)
    {
        switch (type)
        {
            case ResourceType.Lumber:
                hasTrees = v;
                break;
            case ResourceType.Wool:
                hasWool = v;
                break;
            case ResourceType.Clay:
                hasClay = v;
                break;
            case ResourceType.Silk:
                hasSilk = v;
                break;
        }
    }

    public void SetNewTerrainData(TerrainData td)
    {
        TerrainDataSO tempData;

		if (td.isHill)
			tempData = td.terrainData.grassland ? world.grasslandHillTerrain : world.desertHillTerrain;
		else
			tempData = td.terrainData.grassland ? world.grasslandTerrain : world.desertTerrain;

        td.SetNewData(tempData);
        GameLoader.Instance.gameData.allTerrain[td.TileCoordinates] = td.SaveData();
	}

	//for queued build items
	public void GoToNextItemInQueue()
    {
        //UIQueueItem item = savedQueueItems[0];
        //savedQueueItems.Remove(item);
        //savedQueueItemsNames.RemoveAt(0);
        //Destroy(item);

        if (queueItemList.Count > 0)
            GoToNextItemInBuildQueue();
    }

    //public void RemoveFromQueue(Vector3Int loc)
    //{
    //    string name = "Upgrade" + " (" + loc.x / 3 + "," + loc.z / 3 + ")";

    //    int index = 0;
    //    foreach (UIQueueItem item in savedQueueItems)
    //    {
    //        if (item.upgrading && item.itemName == name)
    //        {
    //            savedQueueItems.Remove(item);
    //            savedQueueItemsNames.RemoveAt(index);
    //            resourceManager.ClearQueueResources();
    //            world.RemoveLocationFromQueueList(loc);
    //            Destroy(item);
    //            if (index == 0 && savedQueueItems.Count > 0)
    //                GoToNextItemInBuildQueue();
    //            break;
    //        }

    //        index++;
    //    }
    //}

    public void RemoveFromQueue(Vector3Int loc)
    {
        QueueItem item = improvementQueueDict[loc];
		world.RemoveLocationFromQueueList(loc);
        int index = queueItemList.IndexOf(item);
        queueItemList.Remove(item);

        if (index == 0)
        {
			resourceManager.ClearQueueResources();

			if (queueItemList.Count > 0)
				GoToNextItemInBuildQueue();
		}
    }

    private void GoToNextItemInBuildQueue()
    {
        //UIQueueItem nextItem = savedQueueItems[0];
        QueueItem item = queueItemList[0];

        List<ResourceValue> resourceCosts;

        if (item.upgrade)
            resourceCosts = new(world.GetUpgradeCost(item.queueName));
        else
            resourceCosts = new(UpgradeableObjectHolder.Instance.improvementDict[item.queueName].improvementCost);
        //if (nextItem.unitBuildData != null)
        //    resourceCosts = new(nextItem.unitBuildData.unitCost);
        //if (nextItem.upgradeCosts != null)
        //    resourceCosts = new(nextItem.upgradeCosts);
        //else if (nextItem.improvementData != null)
        //    resourceCosts = new(nextItem.improvementData.improvementCost);

        resourceManager.SetQueueResources(resourceCosts);
    }

    public QueueItem GetBuildInfo()
    {
        return queueItemList[0];
    }

    //looking to see if there are any unclaimed single builds in the area to lay claim to
    public void CheckForAvailableSingleBuilds()
    {
        foreach (Vector3Int tile in world.GetNeighborsFor(cityLoc, MapWorld.State.CITYRADIUS))
        {
            if (world.CheckIfUnclaimedSingleBuild(tile))
            {
                CityImprovement cityImprovement = world.GetCityDevelopment(tile);

                string name = cityImprovement.GetImprovementData.improvementName;
                singleBuildImprovementsBuildingsDict[name] = tile;
                world.AddToCityLabor(tile, cityLoc);

                if (name == "Harbor")
                {
                    hasHarbor = true;
                    harborLocation = tile;
                    //world.SetCityHarbor(this, tile);
                    world.AddTradeLoc(tile, name);
                }
                else if (name == "Barracks")
                {
                    hasBarracks = true;
                    barracksLocation = tile;

					foreach (Vector3Int pos in world.GetNeighborsFor(tile, MapWorld.State.EIGHTWAYARMY))
						army.SetArmySpots(pos);

					army.SetLoc(tile, this);
				}

                world.RemoveFromUnclaimedSingleBuild(tile);
            }
        }
    }

    public bool AddToQueue(ImprovementDataSO improvementData, Vector3Int worldLoc, Vector3Int loc, bool upgrade)
    {
        QueueItem item;
        item.queueName = improvementData.improvementNameAndLevel;
        item.queueLoc = loc;
        item.upgrade = upgrade;
        
        bool building = false;

		if (loc == Vector3Int.zero)
        {
            if (queueItemList.Contains(item))
            {
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Item already in queue");
				return false;
			}

            building = true;
        }
        else
        {
            if (improvementQueueDict.ContainsKey(loc) || world.CheckQueueLocation(worldLoc))
            {
				UIInfoPopUpHandler.WarningMessage().Create(Input.mousePosition, "Location already queued");
				return false;
			}

            improvementQueueDict[loc] = item;
        }

        queueItemList.Add(item);
		world.cityBuilderManager.PlayQueueAudio();
		if (!building)
            world.AddLocationToQueueList(worldLoc, cityLoc);

		if (upgrade)
			world.cityBuilderManager.CreateQueuedArrow(item, improvementData, worldLoc, building);
		else
			world.cityBuilderManager.CreateQueuedGhost(item, improvementData, worldLoc, building);

        world.cityBuilderManager.uiQueueManager.AddToQueueList(item, cityLoc);
		return true;
    }

    public void Select(Color color)
    {
		//EnableHighlight();
		//foreach (string name in world.GetBuildingListForCity(cityLoc))
  //      {
		//	CityImprovement building = world.GetBuildingData(cityLoc, name);
		//	building.DisableHighlight();
		//	building.EnableHighlight(color);
		//}

        highlighted = true;
        world.GetTerrainDataAt(cityLoc).EnableHighlight(color);
	}

    public void Deselect()
    {
		//DisableHighlight();
		//foreach (string name in world.GetBuildingListForCity(cityLoc))
		//{
		//	CityImprovement building = world.GetBuildingData(cityLoc, name);
		//	building.DisableHighlight();
		//}

        highlighted = false;
        world.GetTerrainDataAt(cityLoc).DisableHighlight();
	}

    public void DestroyThisCity()
    {
        //initialHouse.DestroyPS();
        //initialHouse.PlayRemoveEffect(world.GetTerrainDataAt(cityLoc).isHill);
        if (co != null)
        {
            StopCoroutine(co);
			//countDownTimer = secondsTillGrowthCheck;
			co = null;
        }
        stopCycle = true;

        Destroy(uiTimeProgressBar.gameObject);
    }

    public CityData SaveCityData()
    {
        CityData data = new();

        data.name = cityName;
        data.isNamed = isNamed;
        data.location = cityLoc;
        data.reachedWaterLimit = reachedWaterLimit;
        data.harborTraining = harborTraining;
        data.autoGrow = autoGrow;
        data.autoAssignLabor = autoAssignLabor;
        data.hasWater = hasWater;
        data.hasFreshWater = hasFreshWater;
        data.hasRocksFlat = hasRocksFlat;
        data.hasRocksHill = hasRocksHill;
        data.hasTrees = hasTrees;
        data.hasFood = hasFood;
        data.hasWool = hasWool;
        data.hasSilk = hasSilk;
        data.hasClay = hasClay;
        data.hasBarracks = hasBarracks;
        data.hasHarbor = hasHarbor;
		data.waterMaxPop = waterCount;
        data.currentPop = cityPop.CurrentPop;
        data.unusedLabor = cityPop.UnusedLabor;
        data.usedLabor = cityPop.UsedLabor;
        data.resourcePriorities = resourcePriorities;
        data.countDownTimer = countDownTimer;

        for (int i = 0; i < tradersHere.Count; i++)
        {
            data.tradersHere.Add(tradersHere[i].id);
        }

        //resource manager
        data.warehouseStorageLevel = resourceManager.ResourceStorageLevel;
		//data.warehouseStorageLimit = resourceManager.ResourceStorageLimit;
        data.fullInventory = resourceManager.fullInventory;
		data.resourceDict = resourceManager.ResourceDict;
        data.resourcePriceDict = resourceManager.ResourcePriceDict;
        data.resourceSellDict = resourceManager.ResourceSellDict;
        data.resourceMinHoldDict = resourceManager.ResourceMinHoldDict;
        data.resourceSellHistoryDict = resourceManager.ResourceSellHistoryDict;
        data.pauseGrowth = resourceManager.pauseGrowth;
        data.growthDeclineDanger = resourceManager.growthDeclineDanger;
        data.starvationCount = resourceManager.starvationCount;
        data.noHousingCount = resourceManager.noHousingCount;
        data.noWaterCount = resourceManager.noWaterCount;
        data.cycleCount = resourceManager.CycleCount;
        data.resourceGridDict = resourceGridDict;

        //queue lists
        data.queueItemList = queueItemList;
		data.queuedResourcesToCheck = resourceManager.queuedResourcesToCheck;

		List<string> buildingList = world.GetBuildingListForCity(cityLoc);
        //for buildings
        for (int i = 0; i < buildingList.Count; i++)
            data.cityBuildings.Add(world.GetBuildingData(cityLoc, buildingList[i]).SaveData());

		//army data
		if (hasBarracks)
        {
            ArmyData armyData = new();
            
            GameLoader.Instance.gameData.militaryUnits[barracksLocation] = army.SendData();
			armyData.forward = army.forward;
			armyData.attackZone = army.attackZone;
			armyData.enemyTarget = army.EnemyTarget;
			armyData.cyclesGone = army.cyclesGone;
            armyData.unitsReady = army.unitsReady;
            armyData.stepCount = army.stepCount;
            armyData.noMoneyCycles = army.noMoneyCycles;
			armyData.pathToTarget = army.pathToTarget;
			armyData.pathTraveled = army.pathTraveled;
			armyData.attackingSpots = army.attackingSpots;
			armyData.movementRange = army.movementRange;
			armyData.cavalryRange = army.cavalryRange;
			armyData.isTransferring = army.isTransferring;
			armyData.isRepositioning = army.isRepositioning;
			armyData.traveling = army.traveling;
			armyData.inBattle = army.inBattle;
			armyData.returning = army.returning;
			armyData.atHome = army.atHome;
			armyData.enemyReady = army.enemyReady;
			armyData.issueRefund = army.issueRefund;

            GameLoader.Instance.gameData.allArmies[cityLoc] = armyData;
        }

        List<Unit> tempWaitList = waitList.ToList();

		for (int i = 0; i < tempWaitList.Count; i++)
			data.waitList.Add(tempWaitList[i].id);

		List<Unit> tempSeaWaitList = seaWaitList.ToList();

		for (int i = 0; i < tempSeaWaitList.Count; i++)
			data.seaWaitList.Add(tempSeaWaitList[i].id);

		for (int i = 0; i < resourceManager.waitingForTraderList.Count; i++)
			data.waitingForTraderList.Add(resourceManager.waitingForTraderList[i].id);

		for (int i = 0; i < resourceManager.waitingforResourceProducerList.Count; i++)
            data.waitingforResourceProducerList.Add(resourceManager.waitingforResourceProducerList[i].producerLoc);

		for (int i = 0; i < resourceManager.waitingForStorageRoomProducerList.Count; i++)
			data.waitingForProducerStorageList.Add(resourceManager.waitingForStorageRoomProducerList[i].producerLoc);

        List<ResourceProducer> waitingToUnload = new(resourceManager.waitingToUnloadProducers.ToList());
		for (int i = 0; i < waitingToUnload.Count; i++)
			data.waitingToUnloadProducerList.Add(waitingToUnload[i].producerLoc);

  //      List<ResourceProducer> waitingToUnloadResearch = new(resourceManager.waitingToUnloadResearch.ToList());
		//for (int i = 0; i < waitingToUnloadResearch.Count; i++)
		//	data.waitingToUnloadResearchList.Add(waitingToUnloadResearch[i].producerLoc);

		data.resourcesNeededForProduction = resourceManager.resourcesNeededForProduction;

		for (int i = 0; i < resourceManager.waitingForTraderList.Count; i++)
            data.waitingForTraderList.Add(resourceManager.waitingForTraderList[i].id);

        data.resourcesNeededForRoute = resourceManager.resourcesNeededForRoute;

		return data;
    }

    public void LoadCityData(CityData data)
    {
        cityName = data.name;
        isNamed = data.isNamed;
        cityLoc = data.location;
        reachedWaterLimit = data.reachedWaterLimit;
        harborTraining = data.harborTraining;
        autoGrow = data.autoGrow;
        autoAssignLabor = data.autoAssignLabor;
		hasWater = data.hasWater;
		hasFreshWater = data.hasFreshWater;
		hasRocksFlat = data.hasRocksFlat;
		hasRocksHill = data.hasRocksHill;
		hasTrees = data.hasTrees;
		hasFood = data.hasFood;
		hasWool = data.hasWool;
		hasSilk = data.hasSilk;
		hasClay = data.hasClay;
        hasBarracks = data.hasBarracks;
        hasHarbor = data.hasHarbor;
        waterCount = data.waterMaxPop;
		cityPop.CurrentPop = data.currentPop;
        cityPop.UnusedLabor = data.unusedLabor;
        cityPop.UsedLabor = data.usedLabor;
        resourcePriorities = data.resourcePriorities;
        countDownTimer = data.countDownTimer;

        if (cityPop.CurrentPop > 0)
            StartGrowthCycle(true);
        else if (cityPop.CurrentPop > 4)
			cityNameField.ToggleVisibility(true);

		//resource manager
		//warehouseStorageLimit = data.warehouseStorageLimit;
        //resourceManager.ResourceStorageLimit = data.warehouseStorageLimit;
        resourceManager.ResourceStorageLevel = data.warehouseStorageLevel;
        resourceManager.fullInventory = data.fullInventory;
        resourceManager.ResourceDict = data.resourceDict;
        resourceManager.ResourcePriceDict = data.resourcePriceDict;
        resourceManager.ResourceSellDict = data.resourceSellDict;
        resourceManager.ResourceMinHoldDict = data.resourceMinHoldDict;
        resourceManager.ResourceSellHistoryDict = data.resourceSellHistoryDict;
        resourceManager.pauseGrowth = data.pauseGrowth;
        resourceManager.growthDeclineDanger = data.growthDeclineDanger;
		resourceManager.starvationCount = data.starvationCount;
		resourceManager.noHousingCount = data.noHousingCount;
		resourceManager.noWaterCount = data.noWaterCount;
		resourceManager.CycleCount = data.cycleCount;
		resourceGridDict = data.resourceGridDict;

        //queue lists
        queueItemList = data.queueItemList;

        for (int i = 0; i < queueItemList.Count; i++)
            improvementQueueDict[queueItemList[i].queueLoc] = queueItemList[i];

        resourceManager.queuedResourcesToCheck = data.queuedResourcesToCheck;

		//waiting lists
		resourceManager.resourcesNeededForProduction = data.resourcesNeededForProduction;
		resourceManager.resourcesNeededForRoute = data.resourcesNeededForRoute;

		//army data
		if (hasBarracks)
        {
            ArmyData armyData = GameLoader.Instance.gameData.allArmies[cityLoc];
            army.forward = armyData.forward;
		    army.attackZone = armyData.attackZone;
		    army.EnemyTarget = armyData.enemyTarget;
			army.cyclesGone = armyData.cyclesGone;
            army.unitsReady = armyData.unitsReady;
            army.stepCount = armyData.stepCount;
            army.noMoneyCycles = armyData.noMoneyCycles;
			army.pathToTarget = armyData.pathToTarget;
			army.pathTraveled = armyData.pathTraveled;
			army.attackingSpots = armyData.attackingSpots;
			army.movementRange = armyData.movementRange;
			army.cavalryRange = armyData.cavalryRange;
			army.isTransferring = armyData.isTransferring;
			army.isRepositioning = armyData.isRepositioning;
			army.traveling = armyData.traveling;
			army.inBattle = armyData.inBattle;
			army.returning = armyData.returning;
			army.atHome = armyData.atHome;
			army.enemyReady = armyData.enemyReady;
			army.issueRefund = armyData.issueRefund;

            if (army.traveling || army.inBattle || army.enemyReady)
            {
                army.targetCamp = world.GetEnemyCamp(army.EnemyTarget);
				world.GetEnemyCamp(army.EnemyTarget).attackingArmy = army;
			}
		}
	}

    public void SetProducerWaitingList(List<Vector3Int> producerWaiting)
    {
        for (int i = 0; i < producerWaiting.Count; i++)
        {
            resourceManager.waitingforResourceProducerList.Add(world.GetResourceProducer(producerWaiting[i]));
        }
    }

    public void SetProducerStorageRoomWaitingList(List<Vector3Int> producerWaiting)
    {
		for (int i = 0; i < producerWaiting.Count; i++)
		{
			resourceManager.waitingForStorageRoomProducerList.Add(world.GetResourceProducer(producerWaiting[i]));
		}
	}

    public void SetWaitingToUnloadProducerList(List<Vector3Int> producerWaiting)
    {
		for (int i = 0; i < producerWaiting.Count; i++)
		{
			resourceManager.waitingToUnloadProducers.Enqueue(world.GetResourceProducer(producerWaiting[i]));
		}
	}

 //   public void SetWaitingToUnloadResearchList(List<Vector3Int> producerWaiting)
 //   {
	//	for (int i = 0; i < producerWaiting.Count; i++)
	//	{
	//		resourceManager.waitingToUnloadResearch.Add(world.GetResourceProducer(producerWaiting[i]));
	//	}
	//}

	public void SetWaitList(List<int> waitList)
    {
        for (int i = 0; i < waitList.Count; i++)
        {
			for (int j = 0; j < world.traderList.Count; j++)
			{
				if (world.traderList[j].id == waitList[i])
				{
					this.waitList.Enqueue(world.traderList[j]);
					break;
				}
			}
		}
    }

	public void SetSeaWaitList(List<int> seaWaitList)
	{
		for (int i = 0; i < seaWaitList.Count; i++)
		{
			for (int j = 0; j < world.traderList.Count; j++)
			{
				if (world.traderList[j].id == seaWaitList[i])
				{
					this.seaWaitList.Enqueue(world.traderList[j]);
					break;
				}
			}
		}
	}

	public void SetTraderRouteWaitingList(List<int> tradersWaiting)
    {
        for (int i = 0; i < tradersWaiting.Count; i++)
        {
            for (int j = 0; j < world.traderList.Count; j++)
            {
                if (world.traderList[j].id == tradersWaiting[i])
                {
                    resourceManager.waitingForTraderList.Add(world.traderList[j]);
                    break;
                }
            }
        }
    }

    public void SetTradersHereList(List<int> tradersHere)
    {
        for (int i = 0; i < tradersHere.Count; i++)
        {
			for (int j = 0; j < world.traderList.Count; j++)
			{
				if (world.traderList[j].id == tradersHere[i])
				{
					this.tradersHere.Add(world.traderList[j]);
					break;
				}
			}
		}
    }

    public Unit FindUpgradingLandUnit()
    {
        Unit upgradedUnit = null;
        
        for (int i = 0; i < army.UnitsInArmy.Count; i++)
        {
            if (army.UnitsInArmy[i].isUpgrading)
            {
                upgradedUnit = army.UnitsInArmy[i];
                break;
            }
        }

        return upgradedUnit;
    }

    public Unit FindUpgradingSeaTraderUnit()
    {
        Unit upgradedUnit = null;

		for (int i = 0; i < tradersHere.Count; i++)
		{
			if (tradersHere[i].isUpgrading)
			{
				upgradedUnit = tradersHere[i];
				break;
			}
		}

		return upgradedUnit;
    }
}
