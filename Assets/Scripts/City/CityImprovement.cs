using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityImprovement : MonoBehaviour
{
    //[SerializeField]
    //private ImprovementDataSO improvementDataSO;
    //public ImprovementDataSO GetImprovementDataSO { get { return improvementDataSO; } }

    //[SerializeField]
    //private List<ImprovementAnimators> animators = new();
    [HideInInspector]
    public MapWorld world;

    private MeshFilter[] meshFilter;
    public MeshFilter[] MeshFilter { get { return meshFilter; } }
    private SkinnedMeshRenderer skinnedMesh;
    public SkinnedMeshRenderer SkinnedMesh { get { return skinnedMesh; } }

    private SelectionHighlight highlight;
    private ImprovementDataSO improvementData;
    public ImprovementDataSO GetImprovementData { get { return improvementData; } }
    [HideInInspector]
    public City city;
    [HideInInspector]
    public Army army;
    private City queueCity; //for improvements, when queued for upgrades
    [HideInInspector]
    public City meshCity; //for improvements, when mesh combining
    [HideInInspector]
    public bool queued, building, isConstruction, /*isConstructionPrefab, */isUpgrading, canBeUpgraded, isTraining, wonderHarbor, firstStart, /*showing, */hadRoad;
    [HideInInspector]
    public List<ResourceValue> upgradeCost = new(), refundCost = new();
    [HideInInspector]
    public int housingIndex, laborCost, upgradeLevel, unitsWithinCount; //for city centeer housing only, and for canceling training in barracks
    [HideInInspector]
    public ResourceProducer resourceProducer;
	public Dictionary<ResourceType, int> cycleCostDict = new();

    [HideInInspector]
    public Vector3Int loc;
    [HideInInspector]
    public ResourceType producedResource;
    [HideInInspector]
    public int producedResourceIndex;
    public List<List<ResourceValue>> allConsumedResources = new();

	[SerializeField]
    public GameObject improvementMesh, exclamationPoint;
    private ParticleSystem smokeEmitter;
    [SerializeField]
    private List<ParticleSystem> workPS = new();

    [SerializeField]
    private List<Light> workLights = new();

    [HideInInspector]
    public TerrainData td;

    private Coroutine constructionCo;
    [HideInInspector]
    public int timePassed;
    public int GetTimePassed { get { return timePassed; } }
    private string trainingUnitName;

    //animation
    private Animator improvementAnimator;
    private int isWorkingHash;
    //private int workCycles, workCycleLimit;
    Coroutine co;
    private GameObject animMesh; //for making inactive when not working
    private WaitForSeconds oneSecondWait = new WaitForSeconds(1);
    private WaitForEndOfFrame startWorkWait = new();

    [SerializeField]
    private SpriteRenderer mapIcon;

    [SerializeField]
    public Transform mapIconHolder; //for rotating map icon

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        foreach (Light light in workLights)
            light.gameObject.SetActive(false);

        highlight = GetComponent<SelectionHighlight>();
        meshFilter = GetComponentsInChildren<MeshFilter>();
        skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();
        if (skinnedMesh != null)
            animMesh = skinnedMesh.gameObject;

		improvementAnimator = GetComponent<Animator>();
        isWorkingHash = Animator.StringToHash("isWorking");
        //isWaitingHash = Animator.StringToHash("isWaiting");
    }

 //   private void Start()
 //   {
 //       Vector3 loc = transform.position;

 // //      if (!isConstructionPrefab)
 // //      {
 // //          //removeSplash = Instantiate(removeSplash, loc, Quaternion.Euler(-90, 0, 0));
 // //          //removeSplash.Stop();

	//	//	if (improvementData != null && improvementData.hideIdleMesh && co == null)
	//	//		animMesh.SetActive(false);

 // //     //     if (improvementData.producedResourceTime.Count > 0)
 // //  			//CalculateWorkCycleLimit();
	//	//}
	//}

    //public void CalculateWorkCycleLimit()
    //{
    //    workCycleLimit = 30 / improvementData.producedResourceTime[producedResourceIndex];
    //}

    public void SetWorld(MapWorld world)
    {
        this.world = world;
    }

	public void InitializeImprovementData(ImprovementDataSO data)
    {
        improvementData = data;
		allConsumedResources.Add(data.consumedResources);
        allConsumedResources.Add(data.consumedResources1);
        allConsumedResources.Add(data.consumedResources2);
        allConsumedResources.Add(data.consumedResources3);
        allConsumedResources.Add(data.consumedResources4);
    }

    public void PlayPlacementAudio(AudioClip clip)
    {
        audioSource.volume = 1;
        audioSource.clip = clip;
        audioSource.Play();
    }

    public void SetMinimapIcon(TerrainData td)
    {
        this.td = td;
        mapIcon.sprite = improvementData.mapIcon;
        if (td.resourceType != ResourceType.Food && td.resourceType != ResourceType.None && td.resourceType != ResourceType.Lumber && td.resourceType != ResourceType.Fish)
            mapIcon.transform.position += new Vector3(0, 0, 0.5f);
    }

    public void StartWork(float offset, bool load)
    {
        if (firstStart)
        {
            firstStart = false;
            foreach (Light light in workLights)
            {
                if (!light.isActiveAndEnabled)
                    light.gameObject.SetActive(true);
            }

            if (improvementAnimator != null)
            {
				if (improvementData.hideIdleMesh)
					animMesh.SetActive(true);
				co = StartCoroutine(StartWorkAnimation(offset));
			}

			foreach (ParticleSystem ps in workPS)
			{
                if (load)
                {
                    var main = ps.main;
                    main.prewarm = true;
					ps.Play();
                    main.prewarm = false;
				}
                else
                {
					ps.Play();
                }
			}

            //workCycles++;
		}
        else
        {
            PlayImprovementAudio();
            if (improvementAnimator != null)
            {
				co = StartCoroutine(StartWorkAnimation(offset));
				//if (workCycles >= workCycleLimit)
    //            {
    //                workCycles = 0;
    //                co = StartCoroutine(StartWorkAnimation(offset));
    //            }
    //            else
    //            {
    //                workCycles++;
    //            }
            }
        }
    }

    //apparently need to wait till end of frame to start an animation after stopping it.
    private IEnumerator StartWorkAnimation(float offset)
    {
        improvementAnimator.SetBool(isWorkingHash, false); //stop animation first
        yield return startWorkWait;
        improvementAnimator.SetBool(isWorkingHash, true);
		improvementAnimator.SetFloat("offset", offset);
		improvementAnimator.SetFloat("speed", 1f / improvementData.producedResourceTime[producedResourceIndex]);
	}

    public void StartJustWorkAnimation()
    {
		if (improvementAnimator != null)
		{
            float offset = Random.Range(0, 100) * .01f;
			if (improvementData.hideIdleMesh)
				animMesh.SetActive(true);
			co = StartCoroutine(StartWorkAnimation(offset));
		}
	}

    public void HideIdleMesh()
    {
		if (improvementData.hideIdleMesh)
			animMesh.SetActive(false);
	}

    public void StopWork()
    {
        foreach (Light light in workLights)
        {
            if (light.isActiveAndEnabled)
                light.gameObject.SetActive(false);
        }

        if (improvementAnimator != null)
        {
            if (co != null)
                StopCoroutine(co);
            improvementAnimator.SetBool(isWorkingHash, false);
            HideIdleMesh();
        }

        foreach (ParticleSystem ps in workPS)
        {
            if (ps.isPlaying)
                ps.Stop();
        }

		firstStart = true;
	}

    public void StopJustWorkAnimation()
    {
		if (improvementAnimator != null)
		{
			if (co != null)
				StopCoroutine(co);
			improvementAnimator.SetBool(isWorkingHash, false);
            HideIdleMesh();
		}
	}

    public void ToggleLights(bool v)
    {
        foreach (Light light in workLights)
            light.gameObject.SetActive(v);
    }

    //doing this so that the highlight doesn't mix with the combinedmesh.
    public void Embiggen()
    {
        Vector3 newScale = new Vector3(1.02f, 1.02f, 1.02f);
        Vector3 groundScale = Vector3.one;

        for (int i = 0; i < meshFilter.Length; i++)
        {
			if (meshFilter[i].name != "Ground")
            {
				meshFilter[i].transform.localScale = newScale;
                Vector3 pos = meshFilter[i].transform.position;
                pos.y += 0.01f;
                meshFilter[i].transform.position = pos;
            }
            else
            {
				meshFilter[i].transform.localScale = groundScale;
			}
        }
    }

    public void SetInactive()
    {
        for (int i = 0; i < meshFilter.Length; i++)
            meshFilter[i].gameObject.SetActive(false);
    }

    public void SetNewMaterial(Material mat)
    {
        highlight.SetNewMaterial(mat, skinnedMesh);
    }

    public void EnableHighlight(Color highlightColor, bool newGlow = false)
    {
        if (highlight.isGlowing)
            return;
        
        for (int i = 0; i < meshFilter.Length; i++)
        {
            meshFilter[i].gameObject.SetActive(true);
        }
        
        highlight.EnableHighlight(highlightColor, newGlow);
    }

    public void DisableHighlight()
    {
        if (!highlight.isGlowing)
            return;

        //if (!showing)
        HideImprovement();

        highlight.DisableHighlight();
    }

  //  public void EnableMaterial(Material mat)
  //  {
  //      if (showing)
  //          return;

		//for (int i = 0; i < meshFilter.Length; i++)
		//	meshFilter[i].gameObject.SetActive(true);

		//highlight.EnableTransparent(mat);
  //  }

    public void DisableTransparent()
    {
        highlight.DisableHighlight();
    }

    public void RevealImprovement(bool load)
    {
        city.gameObject.SetActive(true);
        city.subTransform.gameObject.SetActive(false);
        city.cityNameField.ToggleVisibility(false);
        city.cityNameMap.gameObject.SetActive(false);
        
        if (improvementData.replaceRocks)
        {
            Material mat;
            if (load)
            {
				mat = td.prop.GetComponentInChildren<MeshRenderer>().sharedMaterial;
			}
            else
            {
                if (td.materials.Count > 1)
                    mat = td.materials[1];
                else
                    mat = world.atlasMain;
            }
            
            skinnedMesh.material = mat;
			SetNewMaterial(mat);
		}

        if (improvementData.improvementName == "Barracks")
            city.RevealUnitsInCamp();

		StartJustWorkAnimation();

        ShowEmbiggenedMesh();
	}

    public void HardReveal()
    {
		if (improvementData.replaceRocks)
		{
			Material mat;
			if (td.materials.Count > 1)
				mat = td.materials[1];
			else
				mat = world.atlasMain;

			skinnedMesh.material = mat;
			SetNewMaterial(mat);
		}

		StartJustWorkAnimation();
	}

    public void ShowEmbiggenedMesh()
    {
		for (int i = 0; i < meshFilter.Length; i++)
			meshFilter[i].gameObject.SetActive(true);
	}

    public void HideImprovement() //hides the embiggened improvement shown in selection
    {
		for (int i = 0; i < meshFilter.Length; i++)
			meshFilter[i].gameObject.SetActive(false);
	}

    public void CheckPermanentChanges()
    {
		world.CheckCityImprovementPermanentChanges(this);
	}

    public void SetQueueCity(City city)
    {
        queueCity = city;
    }

    public City GetQueueCity()
    {
        return queueCity;
    }

    public void PlayRemoveEffect(bool isHill)
    {
        Vector3 loc = transform.position;
        loc.y += isHill ? 0.8f : 0.1f;

        if (building)
			world.PlayRemoveSplash(loc);
        else
    		world.PlayRemoveEruption(loc);
        //removeSplash.transform.position = loc;
        //removeSplash.Play();
    }

    public void PlaySmokeEmitter(Vector3 loc, int time, bool load)
    {
        //int time = improvementData.buildTime;
        ParticleSystem smokeEmitter = Instantiate(Resources.Load<ParticleSystem>("Prefabs/ParticlePrefabs/SmokeEmitter"), loc, Quaternion.Euler(-90, 0, 0));
        smokeEmitter.transform.SetParent(world.psHolder, false);
        this.smokeEmitter = smokeEmitter;

        var emission = smokeEmitter.emission;
        emission.rateOverTime = 10f / time;

        //smokeEmitter.transform.position = loc;
        //smokeEmitter.gameObject.SetActive(true);
        if (load)
            smokeEmitter.time = time - timePassed;

        smokeEmitter.Play();
    }

    public void PlaySmokeSplash(bool isHill)
    {
        Vector3 loc = transform.position;
        loc.y += isHill ? 0.6f : 0.1f;

        ParticleSystem smokeSplash = Instantiate(Resources.Load<ParticleSystem>("Prefabs/ParticlePrefabs/SmokeSplash"), loc, Quaternion.Euler(-90, 0, 0));
        smokeSplash.transform.SetParent(world.psHolder, false);
        smokeSplash.Play();
    }

    public void PlaySmokeSplashBuilding()
    {
        Vector3 loc = transform.position;
        loc.y += .1f;
        ParticleSystem smokeSplash = Instantiate(Resources.Load<ParticleSystem>("Prefabs/ParticlePrefabs/SmokeSplashBuilding"), loc, Quaternion.Euler(-90, 0, 0));
        smokeSplash.transform.SetParent(world.psHolder, false);
        //particleSystems.Add(smokeSplash);
        smokeSplash.Play();
    }

    //public void StopUpgradeSwirls()
    //{
    //    //if (upgradeSwirl.isPlaying)
    //    //{
    //    //    upgradeSwirl.Stop();         
    //    //    upgradeSwirlDown.Stop();
    //    //}
    //}

    public void StopSmokeEmitter()
    {
        smokeEmitter.Stop();
        Destroy(smokeEmitter.gameObject);
        //smokeEmitter.gameObject.SetActive(false);
    }

    public void BeginImprovementConstructionProcess(City city, ResourceProducer producer, Vector3Int tempBuildLocation, CityBuilderManager cityBuilderManager, bool isHill, bool load)
    {
        Vector3 loc = transform.position;

        if (isHill)
            loc.y += 0.6f;
        else
            loc.y += .1f;
        PlaySmokeEmitter(loc, improvementData.buildTime, load); 

        if (!load)
            timePassed = improvementData.buildTime;
        constructionCo = StartCoroutine(BuildImprovementCoroutine(city, producer, tempBuildLocation, cityBuilderManager, isHill));
    }

    private IEnumerator BuildImprovementCoroutine(City city, ResourceProducer producer, Vector3Int tempBuildLocation, CityBuilderManager cityBuilderManager, bool isHill)
    {
        producer.ShowConstructionProgressTimeBar(improvementData.buildTime, city.activeCity);
        producer.SetConstructionTime(timePassed);

        while (timePassed > 0)
        {
            yield return oneSecondWait;
            timePassed--;
            producer.SetConstructionTime(timePassed);
        }

        StopSmokeEmitter();
        //PlaySmokeSplash(isHill);
        producer.HideConstructionProgressTimeBar();
        //cityBuilderManager.RemoveConstruction(tempBuildLocation);
        cityBuilderManager.FinishImprovement(city, improvementData, tempBuildLocation);
        //cityBuilderManager.AddToConstructionTilePool(this);
    }

    public void BeginImprovementUpgradeProcess(City city, ResourceProducer producer, ImprovementDataSO data, bool load)
    {
		if (!load)
            timePassed = data.buildTime;
		constructionCo = StartCoroutine(UpgradeImprovementCoroutine(city, producer, data, load));
    }

    private IEnumerator UpgradeImprovementCoroutine(City city, ResourceProducer producer, ImprovementDataSO data, bool load)
    {
        PlaySmokeEmitter(loc, data.buildTime, load);
        //PlayUpgradeSwirl(timePassed);
        isUpgrading = true;
        producer.isUpgrading = true;
        producer.ShowConstructionProgressTimeBar(data.buildTime, city.activeCity);
        producer.SetConstructionTime(timePassed);

        //while (timePassed > 1)
        //{
        //    yield return new WaitForSeconds(1);
        //    timePassed--;
        //    producer.SetConstructionTime(timePassed);
        //}
        //upgradeFlash.Play();
        while (timePassed > 0)
        {
            yield return oneSecondWait;
            timePassed--;
            producer.SetConstructionTime(timePassed);
        }

		city.resourceManager.IssueRefund(refundCost, loc);
		StopUpgradeProcess(producer);
        world.cityBuilderManager.UpgradeSelectedImprovement(loc, this, city, data);
    }

    public void StopUpgrade()
    {
        StopCoroutine(constructionCo);
    }

	public void StopUpgradeProcess(ResourceProducer producer)
	{
		//StopUpgradeSwirls();
		StopSmokeEmitter();
		isUpgrading = false;
		producer.isUpgrading = false;
		upgradeCost.Clear();
        refundCost.Clear();
		producer.HideConstructionProgressTimeBar();
	}

	public void BeginTraining(City city, ResourceProducer producer, UnitBuildDataSO data, bool upgrading, Unit upgradedUnit, bool load)
    {
        if (!upgrading)
			upgradeCost = new List<ResourceValue>(data.unitCost);
		
        isUpgrading = upgrading;
        laborCost = data.laborCost;
        trainingUnitName = data.unitNameAndLevel;

        if (!load)
    		timePassed = data.trainTime;
        constructionCo = StartCoroutine(TrainUnitCoroutine(city, producer, data, upgrading, upgradedUnit, load));
    }

    private IEnumerator TrainUnitCoroutine(City city, ResourceProducer producer, UnitBuildDataSO data, bool upgrading, Unit upgradedUnit, bool load)
    {
		PlaySmokeEmitter(loc, data.trainTime, load);
		producer.ShowConstructionProgressTimeBar(data.trainTime, city.activeCity);
		producer.SetConstructionTime(timePassed);
        isTraining = true;
		producer.isUpgrading = true;

		while (timePassed > 0)
		{
			yield return oneSecondWait;
			timePassed--;
			producer.SetConstructionTime(timePassed);
		}

        city.resourceManager.IssueRefund(refundCost, loc);
        StopTraining(producer);
        world.cityBuilderManager.BuildUnit(city, data, upgrading, upgradedUnit);
	}

    private void StopTraining(ResourceProducer producer)
    {
		StopSmokeEmitter();
		isTraining = false;
		producer.isUpgrading = false;
		upgradeCost.Clear();
        refundCost.Clear();
		producer.HideConstructionProgressTimeBar();
	}

    public void CancelTraining(ResourceProducer producer)
    {
		if (isUpgrading)
		{
			foreach (Military unit in city.army.UnitsInArmy)
			{
				if (unit.isUpgrading)
				{
					unit.isUpgrading = false;
					break;
				}
			}

			isUpgrading = false;
		}

        StopTraining(producer);
	}

    public void AddTraderToImprovement(Trader unit)
    {
        unitsWithinCount++;
        city.tradersHere.Add(unit);
		List<ResourceType> resourceTypes = new();

		foreach (ResourceValue value in unit.buildDataSO.cycleCost)
        {
            if (value.resourceType == ResourceType.Gold)
                continue;
            
            if (!cycleCostDict.ContainsKey(value.resourceType))
                cycleCostDict[value.resourceType] = value.resourceAmount;
            else
                cycleCostDict[value.resourceType] += value.resourceAmount;

            resourceTypes.Add(value.resourceType);
			city.resourceManager.ModifyResourceConsumptionPerMinute(value.resourceType, value.resourceAmount);
		}

		if (city.activeCity && city.world.cityBuilderManager.uiLaborHandler.activeStatus)
			city.world.cityBuilderManager.uiLaborHandler.UpdateResourcesConsumed(resourceTypes, city.resourceManager.resourceConsumedPerMinuteDict);

		if (world.uiCityImprovementTip.activeStatus && world.uiCityImprovementTip.improvement.loc == loc)
			world.uiCityImprovementTip.UpdateConsumeNumbers();

		if (!city.growing)
			city.StartGrowthCycle(false);
	}

	public void RemoveTraderFromImprovement(Trader unit)
    {
        unitsWithinCount--;
        world.RemoveTraderPosition(unit.currentLocation, unit.trader);
        city.tradersHere.Remove(unit);
		List<ResourceType> resourceTypes = new();

		foreach (ResourceValue value in unit.buildDataSO.cycleCost)
        {
			if (value.resourceType == ResourceType.Gold)
				continue;

			cycleCostDict[value.resourceType] -= value.resourceAmount;

            if (cycleCostDict[value.resourceType] == 0)
                cycleCostDict.Remove(value.resourceType);

            resourceTypes.Add(value.resourceType);
			city.resourceManager.ModifyResourceConsumptionPerMinute(value.resourceType, -value.resourceAmount);
		}

		if (city.activeCity && city.world.cityBuilderManager.uiLaborHandler.activeStatus)
			city.world.cityBuilderManager.uiLaborHandler.UpdateResourcesConsumed(resourceTypes, city.resourceManager.resourceConsumedPerMinuteDict);

		if (world.uiCityImprovementTip.activeStatus && world.uiCityImprovementTip.improvement.loc == loc)
			world.uiCityImprovementTip.UpdateConsumeNumbers();

		city.StopGrowthCycleCheck();
	}

    public List<ResourceValue> GetCycleCost()
    {
		List<ResourceValue> costs = new();

		foreach (ResourceType type in cycleCostDict.Keys)
		{
            if (type == ResourceType.Gold)
                continue;

            ResourceValue value;
			value.resourceType = type;
			value.resourceAmount = cycleCostDict[type];
			costs.Add(value);
		}

		return costs;
	}

    //currently not random, just whoever's first on list
    public Vector3Int RemoveRandomTrader()
    {
        SingleBuildType type = GetImprovementData.singleBuildType;

        foreach(Trader unit in city.tradersHere)
        {
            if (unit.buildDataSO.singleBuildType == type)
            {
				if (unit.isSelected)
                {
                    if (world.uiTradeRouteBeginTooltip.activeStatus && world.uiTradeRouteBeginTooltip.trader == unit)
                    {
						if (!world.uiTradeRouteBeginTooltip.gameObject.activeSelf)
							world.unitMovement.CloseBuildingSomethingPanelButton();

                        world.uiTradeRouteBeginTooltip.ToggleVisibility(false);
					}

                    world.somethingSelected = false;
                    world.unitMovement.ClearSelection();
                }
                
                world.traderList.Remove(unit.trader);
				RemoveTraderFromImprovement(unit);
				unit.DestroyUnit();
				break;
            }
        }

        city.PlaySelectAudio(world.cityBuilderManager.popLoseClip);
        return loc;
    }

    public void PlayImprovementAudio()
    {
        audioSource.clip = GetImprovementData.audio;
        audioSource.Play();
    }

  //  public void PlayPopLossAudio()
  //  {
		//audioSource.clip = world.cityBuilderManager.popLoseClip;
		//audioSource.Play();
  //  }

	public void RemoveConstruction()
    {
        StopCoroutine(constructionCo);
        StopSmokeEmitter();
        //cityBuilderManager.RemoveConstruction(tempBuildLocation);
        //cityBuilderManager.AddToConstructionTilePool(this);
    }

    public void DestroyImprovement()
    {
        TerrainData td = world.GetTerrainDataAt(loc);
        
        if (city.activeCity)
        {
            world.cityBuilderManager.tilesToChange.Remove(loc);
            td.DisableHighlight();

            world.cityBuilderManager.uiCityUpgradePanel.CurrentImprovementCheck(this);
        }

        td.RemoveMinimapResource(world.mapHandler);
        world.RemoveResourceIcon(loc);
        //city.SetNewTerrainData(loc);
		city.UpdateCityBools(producedResource, ResourceHolder.Instance.GetRawResourceType(producedResource), td.terrainData.type);
        world.uiCityImprovementTip.CloseCheck(this);
        world.cityBuilderManager.RemoveImprovement(loc, this, false);
    }

    public CityImprovementData SaveData()
    {
        CityImprovementData data = new();

        data.name = improvementData.improvementNameAndLevel;
        data.rotation = (int)gameObject.transform.localEulerAngles.y;
		data.location = loc;
        data.hadRoad = hadRoad;

        if (city == null)
            data.cityLoc = new Vector3Int(0, -10, 0);
        else
            data.cityLoc = city.cityLoc;
        data.queued = queued;
        data.isConstruction = isConstruction;
        data.isUpgrading = isUpgrading;
        data.upgradeLevel = upgradeLevel;
        data.isTraining = isTraining;
        if (isTraining)
            data.trainingUnitName = trainingUnitName;
        data.housingIndex = housingIndex;
        data.laborCost = laborCost;

        //if (isConstruction)
        //    data.timePassed = world.GetCityDevelopmentConstruction(loc).timePassed;
        //else
        data.timePassed = timePassed;

        data.producedResourceIndex = producedResourceIndex;

        if (!building || improvementData.isBuildingImprovement)
        {
            //Resource Producer
		    data.currentLabor = resourceProducer.currentLabor;
            data.tempLabor = resourceProducer.tempLabor;
            data.unloadLabor = resourceProducer.unloadLabor;
            data.isWaitingForStorageRoom = resourceProducer.isWaitingForStorageRoom;
            data.hitResourceMax = resourceProducer.hitResourceMax;
            data.isWaitingforResources = resourceProducer.isWaitingforResources;
            //data.isWaitingToUnload = resourceProducer.isWaitingToUnload;
            data.isWaitingForResearch = resourceProducer.isWaitingForResearch;
            data.isProducing = resourceProducer.isProducing;
		    data.productionTimer = resourceProducer.productionTimer;
		    data.producedResource = producedResource;
		    data.tempLaborPercsList = resourceProducer.tempLaborPercsList;
            data.goldNeeded = resourceProducer.goldNeeded;  
        }

		//updating terrain resource amounts
		GameLoader.Instance.gameData.allTerrain[loc].resourceAmount = world.GetTerrainDataAt(loc).resourceAmount;

        return data;
    }

    public void LoadData(CityImprovementData data, City city, MapWorld world)
    {
        loc = data.location;
        queued = data.queued;
        hadRoad = data.hadRoad;
        isConstruction = data.isConstruction;
        isUpgrading = data.isUpgrading;
        isTraining = data.isTraining;
        housingIndex = data.housingIndex;
        laborCost = data.laborCost;
		timePassed = data.timePassed;
        producedResourceIndex = data.producedResourceIndex;

        if (!building || improvementData.isBuildingImprovement)
        {
		    //Resource Producer
			resourceProducer.currentLabor = data.currentLabor;
			resourceProducer.tempLabor = data.tempLabor;
            resourceProducer.hitResourceMax = data.hitResourceMax;
			resourceProducer.unloadLabor = data.unloadLabor;
			resourceProducer.isWaitingForStorageRoom = data.isWaitingForStorageRoom;
			resourceProducer.isWaitingforResources = data.isWaitingforResources;
			//resourceProducer.isWaitingToUnload = data.isWaitingToUnload;
			resourceProducer.isWaitingForResearch = data.isWaitingForResearch;
			resourceProducer.isProducing = data.isProducing;
			resourceProducer.productionTimer = data.productionTimer;
		    producedResource = data.producedResource;
			resourceProducer.tempLaborPercsList = data.tempLaborPercsList;
            resourceProducer.goldNeeded = data.goldNeeded;

            if (resourceProducer.isProducing)
            {
			    if (!resourceProducer.isWaitingforResources && !resourceProducer.isWaitingForStorageRoom && !resourceProducer.hitResourceMax && !resourceProducer.isWaitingForResearch)
                {
					resourceProducer.SetResourceManager(city.resourceManager);
					resourceProducer.LoadProducingCoroutine();

                    //work around to get lights showing on all polys, something to do with combinemeshes
                    if (workLights.Count > 0)
                    {
					    for (int i = 0; i < meshFilter.Length; i++)
						    meshFilter[i].gameObject.SetActive(true);
					    for (int i = 0; i < meshFilter.Length; i++)
						    meshFilter[i].gameObject.SetActive(false);
				    }
			    }
                else
                {
                    exclamationPoint.SetActive(true);
                    firstStart = true;
                }
            }
            else
            {
				HideIdleMesh();
			}
        }
        
        if (isTraining)
        {
            if (!isUpgrading)
            //{
            //    GameLoader.Instance.improvementUnitUpgradeDict[this] = data.trainingUnitName;
            //}
            //else
            {
                BeginTraining(city, resourceProducer, UpgradeableObjectHolder.Instance.unitDict[data.trainingUnitName], data.isUpgrading, null, true);
            }
		}
        else if (isUpgrading)
        {
            upgradeLevel = data.upgradeLevel;
            world.CreateUpgradedImprovement(this, city, upgradeLevel);
        }
    }

    //need upgraded unit, this is run after all units have been made
    public void ResumeTraining(Unit unit)
    {
        //Unit unit = null;

        //if (isUpgrading)
        //{
        //    if (improvementData.improvementName == "Barracks")
        //        unit = city.FindUpgradingLandUnit();
        //    else if (improvementData.improvementName == "Harbor")
        //        unit = city.FindUpgradingSeaTraderUnit();
        //}

        world.CreateUpgradedUnit(unit, this, city, unit.upgradeLevel);
		//BeginTraining(city, resourceProducer, UpgradeableObjectHolder.Instance.unitDict[trainingUnitName], true, unit, true);
	}
}

public enum SingleBuildType
{
    None,
    Barracks,
    TradeDepot,
    Harbor,
    Shipyard,
    Airport,
    AirBase,
    Market,
    Monument,
    Well,
    Walls
}
