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

    private MeshFilter[] meshFilter;
    public MeshFilter[] MeshFilter { get { return meshFilter; } }
    private SkinnedMeshRenderer skinnedMesh;
    public SkinnedMeshRenderer SkinnedMesh { get { return skinnedMesh; } }

    private SelectionHighlight highlight;
    private ImprovementDataSO improvementData;
    public ImprovementDataSO GetImprovementData { get { return improvementData; } }
    private City city; //for buildings, click on them to select city
    private City queueCity; //for improvements, when queued for upgrades
    [HideInInspector]
    public City meshCity; //for improvements, when mesh combining
    [HideInInspector]
    public bool queued, building, isConstruction, isConstructionPrefab, isUpgrading, canBeUpgraded, isTraining, wonderHarbor, firstStart;
    private List<ResourceValue> upgradeCost = new();
    public List<ResourceValue> UpgradeCost { get { return upgradeCost; } set { upgradeCost = value; } }
    [HideInInspector]
    public int housingIndex, laborCost; //for city centeer housing only, and for canceling training in barracks

    [HideInInspector]
    public Vector3Int loc;
    [HideInInspector]
    public ResourceType producedResource;
    [HideInInspector]
    public int producedResourceIndex;
    public List<List<ResourceValue>> allConsumedResources = new();

    [SerializeField]
    private ParticleSystem smokeEmitter, smokeSplash, removeSplash;
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
    private int workCycles, workCycleLimit;
    Coroutine co;
    private GameObject animMesh; //for making inactive when not working
    private WaitForSeconds oneSecondWait = new WaitForSeconds(1);
    private WaitForEndOfFrame startWorkWait = new();

    [SerializeField]
    private SpriteRenderer mapIcon;

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

    private void Start()
    {
        Vector3 loc = transform.position;

        if (!isConstructionPrefab)
        {
            removeSplash = Instantiate(removeSplash, loc, Quaternion.Euler(-90, 0, 0));
            removeSplash.Stop();

			if (improvementData != null && improvementData.hideIdleMesh && workCycles == 0)
				animMesh.SetActive(false);

            if (improvementData.producedResourceTime.Count > 0)
    			CalculateWorkCycleLimit();
		}
	}

    public void CalculateWorkCycleLimit()
    {
        workCycleLimit = 30 / improvementData.producedResourceTime[producedResourceIndex];
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

            workCycles++;
		}
        else
        {
            if (improvementAnimator != null)
            {
                if (workCycles >= workCycleLimit)
                {
                    workCycles = 0;
                    co = StartCoroutine(StartWorkAnimation(offset));
                }
                else
                {
                    workCycles++;
                }
            }
        }
    }

    //ridiculous workaround since you can't stop and then start an animation at the same time.
    private IEnumerator StartWorkAnimation(float offset)
    {
        improvementAnimator.SetBool(isWorkingHash, false); //stop animation first
        yield return startWorkWait;
        improvementAnimator.SetBool(isWorkingHash, true);
		improvementAnimator.SetFloat("offset", offset);
		improvementAnimator.SetFloat("speed", 1f / improvementData.producedResourceTime[producedResourceIndex]);
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
            if (improvementData.hideIdleMesh)
                animMesh.SetActive(false);
        }

        foreach (ParticleSystem ps in workPS)
        {
            if (ps.isPlaying)
                ps.Stop();
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

        int count = meshFilter.Length;
        for (int i = 0; i < count; i++)
        {
            meshFilter[i].transform.localScale = newScale;
            Vector3 pos = meshFilter[i].transform.position;
            pos.y += 0.01f;
            meshFilter[i].transform.position = pos;
        }
    }

    public void SetInactive()
    {
        int count = meshFilter.Length;
        for (int i = 0; i < count; i++)
            meshFilter[i].gameObject.SetActive(false);
    }

    public void SetNewMaterial(Material mat)
    {
        highlight.SetNewMaterial(mat, skinnedMesh);
    }

    public void EnableHighlight(Color highlightColor)
    {
        if (highlight.isGlowing)
            return;
        
        int count = meshFilter.Length;
        for (int i = 0; i < count; i++)
        {
            meshFilter[i].gameObject.SetActive(true);
        }
        
        highlight.EnableHighlight(highlightColor);
    }

    public void DisableHighlight()
    {
        if (!highlight.isGlowing)
            return;
        
        int count = meshFilter.Length;
        for (int i = 0; i < count; i++)
            meshFilter[i].gameObject.SetActive(false);

        highlight.DisableHighlight();
    }

    public void SetCity(City city)
    {
        this.city = city;
    }

    public City GetCity()
    {
        return city;
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
        if (isHill)
            loc.y += .8f;
        else
            loc.y += .1f;

        removeSplash.transform.position = loc;
        removeSplash.Play();
    }

    private void PlaySmokeEmitter(Vector3 loc, bool load)
    {
        int time = improvementData.buildTime;
        var emission = smokeEmitter.emission;
        emission.rateOverTime = 10f / time;

        smokeEmitter.transform.position = loc;
        smokeEmitter.gameObject.SetActive(true);
        if (load)
            smokeEmitter.time = time - timePassed;

        smokeEmitter.Play();
    }

    public void PlaySmokeSplash(bool isHill)
    {
        Vector3 loc = transform.position;
        if (isHill)
            loc.y += .6f;
        else
            loc.y += .1f;
        smokeSplash.transform.position = loc;
        smokeSplash.Play();
    }

    public void PlaySmokeSplashBuilding()
    {
        Vector3 loc = transform.position;
        loc.y += .1f;
        smokeSplash = Instantiate(smokeSplash, loc, Quaternion.Euler(-90, 0, 0));
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
        smokeEmitter.gameObject.SetActive(false);
    }

    public void BeginImprovementConstructionProcess(City city, ResourceProducer producer, Vector3Int tempBuildLocation, CityBuilderManager cityBuilderManager, bool isHill, bool load)
    {
        Vector3 loc = transform.position;

        if (isHill)
            loc.y += 0.6f;
        else
            loc.y += .1f;
        PlaySmokeEmitter(loc, load); 

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
        PlaySmokeSplash(isHill);
        producer.HideConstructionProgressTimeBar();
        cityBuilderManager.RemoveConstruction(tempBuildLocation);
        cityBuilderManager.FinishImprovement(city, improvementData, tempBuildLocation);
        cityBuilderManager.AddToConstructionTilePool(this);
    }

    public void BeginImprovementUpgradeProcess(City city, ResourceProducer producer, Vector3Int tempBuildLocation, ImprovementDataSO data, bool load)
    {
		if (!load)
            timePassed = data.buildTime;
		constructionCo = StartCoroutine(UpgradeImprovementCoroutine(city, producer, tempBuildLocation, data, load));
    }

    private IEnumerator UpgradeImprovementCoroutine(City city, ResourceProducer producer, Vector3Int tempBuildLocation, ImprovementDataSO data, bool load)
    {
        PlaySmokeEmitter(tempBuildLocation, load);
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

        StopUpgradeProcess(producer);
        city.world.cityBuilderManager.UpgradeSelectedImprovement(tempBuildLocation, this, city, data);
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
		producer.HideConstructionProgressTimeBar();
	}

	public void BeginTraining(City city, ResourceProducer producer, Vector3Int tempBuildLocation, UnitBuildDataSO data, bool upgrading, Unit upgradedUnit, bool load)
    {
        if (!upgrading)
			upgradeCost = new List<ResourceValue>(data.unitCost);
		
        isUpgrading = upgrading;
        laborCost = data.laborCost;
        trainingUnitName = data.unitNameAndLevel;

        if (!load)
    		timePassed = data.trainTime;
        constructionCo = StartCoroutine(TrainUnitCoroutine(city, producer, tempBuildLocation, data, upgrading, upgradedUnit, load));
    }

    private IEnumerator TrainUnitCoroutine(City city, ResourceProducer producer, Vector3Int tempBuildLocation, UnitBuildDataSO data, bool upgrading, Unit upgradedUnit, bool load)
    {
		PlaySmokeEmitter(tempBuildLocation, load);
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

        StopTraining(producer);
        city.world.cityBuilderManager.BuildUnit(city, data, upgrading, upgradedUnit);
	}

    private void StopTraining(ResourceProducer producer)
    {
		StopSmokeEmitter();
		isTraining = false;
		producer.isUpgrading = false;
		upgradeCost.Clear();
		producer.HideConstructionProgressTimeBar();
	}

    public void CancelTraining(ResourceProducer producer)
    {
		if (isUpgrading)
		{
			foreach (Unit unit in city.army.UnitsInArmy)
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

	public void RemoveConstruction(CityBuilderManager cityBuilderManager, Vector3Int tempBuildLocation)
    {
        StopCoroutine(constructionCo);
        StopSmokeEmitter();
        cityBuilderManager.RemoveConstruction(tempBuildLocation);
        cityBuilderManager.AddToConstructionTilePool(this);
    }

    public void DestroyImprovement()
    {
        TerrainData td = city.world.GetTerrainDataAt(loc);
        
        if (city.activeCity)
        {
            city.world.cityBuilderManager.tilesToChange.Remove(loc);
            td.DisableHighlight();

            city.world.cityBuilderManager.uiCityUpgradePanel.CurrentImprovementCheck(this);
        }

        td.RemoveMinimapResource(city.world.mapHandler);
        city.world.RemoveResourceIcon(loc);
        //city.SetNewTerrainData(loc);
		city.UpdateCityBools(producedResource, ResourceHolder.Instance.GetRawResourceType(producedResource), td.terrainData.type);
        city.world.uiCityImprovementTip.CloseCheck(this);
        city.world.cityBuilderManager.RemoveImprovement(loc, this, city, false);
    }

    public CityImprovementData SaveData()
    {
        CityImprovementData data = new();

        data.name = improvementData.improvementNameAndLevel;
        data.location = loc;
        data.cityLoc = city.cityLoc;
        data.queued = queued;
        data.isConstruction = isConstruction;
        data.isUpgrading = isUpgrading;
        data.isTraining = isTraining;
        if (isTraining)
            data.trainingUnitName = trainingUnitName;
        data.housingIndex = housingIndex;
        data.laborCost = laborCost;
        
        if (isConstruction)
            data.timePassed = city.world.GetCityDevelopmentConstruction(loc).timePassed;
        else
            data.timePassed = timePassed;

        data.producedResourceIndex = producedResourceIndex;

        if (!building || improvementData.isBuildingImprovement)
        {
            //Resource Producer
            ResourceProducer producer = city.world.GetResourceProducer(loc);
		    data.currentLabor = producer.currentLabor;
            data.tempLabor = producer.tempLabor;
            data.unloadLabor = producer.unloadLabor;
            data.isWaitingForStorageRoom = producer.isWaitingForStorageRoom;
            data.isWaitingforResources = producer.isWaitingforResources;
            data.isWaitingToUnload = producer.isWaitingToUnload;
            data.isWaitingForResearch = producer.isWaitingForResearch;
            data.isProducing = producer.isProducing;
		    data.productionTimer = producer.productionTimer;
		    data.producedResource = producedResource;
		    data.tempLaborPercsList = producer.tempLaborPercsList;
        }

		//updating terrain resource amounts
		GameLoader.Instance.gameData.allTerrain[loc].resourceAmount = city.world.GetTerrainDataAt(loc).resourceAmount;

        return data;
    }

    public void LoadData(CityImprovementData data, City city)
    {
        loc = data.location;
        queued = data.queued;
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
		    ResourceProducer producer = city.world.GetResourceProducer(loc);
		    producer.currentLabor = data.currentLabor;
		    producer.tempLabor = data.tempLabor;
		    producer.unloadLabor = data.unloadLabor;
		    producer.isWaitingForStorageRoom = data.isWaitingForStorageRoom;
		    producer.isWaitingforResources = data.isWaitingforResources;
		    producer.isWaitingToUnload = data.isWaitingToUnload;
		    producer.isWaitingForResearch = data.isWaitingForResearch;
		    producer.isProducing = data.isProducing;
            producer.productionTimer = data.productionTimer;
		    producedResource = data.producedResource;
            producer.tempLaborPercsList = data.tempLaborPercsList;

            if (producer.isProducing)
            {
			    if (!producer.isWaitingforResources && !producer.isWaitingForStorageRoom && !producer.isWaitingToUnload && !producer.isWaitingForResearch)
                {
                    producer.SetResourceManager(city.ResourceManager);
                    producer.LoadProducingCoroutine();

                    //work around to get lights showing on all polys, something to do with combinemeshes
                    if (workLights.Count > 0)
                    {
					    for (int i = 0; i < meshFilter.Length; i++)
						    meshFilter[i].gameObject.SetActive(true);
					    for (int i = 0; i < meshFilter.Length; i++)
						    meshFilter[i].gameObject.SetActive(false);
				    }
			    }
            }
        }
        
        if (isTraining)
        {
            if (isUpgrading)
            {
                GameLoader.Instance.improvementUnitUpgradeDict[this] = data.trainingUnitName;
            }
            else
            {
                BeginTraining(city, city.world.GetResourceProducer(loc), loc, UpgradeableObjectHolder.Instance.unitDict[data.trainingUnitName], data.isUpgrading, null, true);
            }
		}
        else if (isUpgrading)
        {
            city.world.CreateUpgradedImprovement(loc, this, city);
        }
    }

    //need upgraded unit, this is run after all units have been made
    public void ResumeTraining(string trainingUnitName)
    {
        Unit unit = null;

        if (isUpgrading)
        {
            if (improvementData.improvementName == "Barracks")
                unit = city.FindUpgradingLandUnit();
            else if (improvementData.improvementName == "Harbor")
                unit = city.FindUpgradingSeaTraderUnit();
        }

		BeginTraining(city, city.world.GetResourceProducer(loc), loc, UpgradeableObjectHolder.Instance.unitDict[trainingUnitName], true, unit, true);
	}
}
