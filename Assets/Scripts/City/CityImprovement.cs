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
    public SkinnedMeshRenderer skinnedMesh;

    private SelectionHighlight highlight;
    private ImprovementDataSO improvementData;
    public ImprovementDataSO GetImprovementData { get { return improvementData; } }
    private City city; //for buildings, click on them to select city
    private City queueCity; //for improvements, when queued for upgrades
    [HideInInspector]
    public City meshCity; //for improvements, when mesh combining
    [HideInInspector]
    public bool initialCityHouse, queued, building, isConstruction, isUpgrading;
    private List<ResourceValue> upgradeCost = new();
    public List<ResourceValue> UpgradeCost { get { return upgradeCost; } set { upgradeCost = value; } }

    [SerializeField]
    private ParticleSystem upgradeSwirl, upgradeSwirlDown, upgradeFlash, upgradeSplash, smokeSlow, smokeEmitter, smokeSplash, removeEruption, removeSplash;
    [SerializeField]
    private List<ParticleSystem> workPS = new();

    [SerializeField]
    private List<Light> workLights = new();

    private Coroutine constructionCo;
    private int timePassed;
    public int GetTimePassed { get { return timePassed; } }

    //animation
    private Animator improvementAnimator;
    private int isWorkingHash;
    //private int isWaitingHash;
    Coroutine co;
    [SerializeField]
    private GameObject animMesh;

    private void Awake()
    {
        foreach (Light light in workLights)
            light.gameObject.SetActive(false);

        highlight = GetComponent<SelectionHighlight>();
        meshFilter = GetComponentsInChildren<MeshFilter>();
        skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();
        improvementAnimator = GetComponent<Animator>();
        isWorkingHash = Animator.StringToHash("isWorking");
        //isWaitingHash = Animator.StringToHash("isWaiting");
    }

    private void Start()
    {
        Vector3 loc = transform.position;

        if (!building && !isConstruction)
        {
            //upgradeSwirl = Instantiate(upgradeSwirl, loc, Quaternion.Euler(-90, 0, 0));
            //particleSystems.Add(upgradeSwirl);
            //upgradeSwirl.Stop();

            removeEruption = Instantiate(removeEruption, loc, Quaternion.Euler(-90, 0, 0));
            removeEruption.Stop();

            //loc.y += 0.1f;
            //upgradeFlash.transform.position = loc;
            //upgradeFlash = Instantiate(upgradeFlash, loc, Quaternion.Euler(0, 0, 0));
            //particleSystems.Add(upgradeFlash);
            //upgradeFlash.Pause();


            //loc.y += 1.5f;
            //upgradeSwirlDown.transform.position = loc;
            //upgradeSwirlDown.transform.rotation = Quaternion.Euler(-270, 0, 0);
            //upgradeSwirlDown = Instantiate(upgradeSwirlDown, loc, Quaternion.Euler(-270, 0, 0));
            //particleSystems.Add(upgradeSwirlDown);
            //upgradeSwirlDown.Pause();

            if (improvementData.hideAnimMesh)
                animMesh.SetActive(false);
        }
        else if (building)
        {
            //upgradeSplash = Instantiate(upgradeSplash, loc, Quaternion.Euler(-90, 0, 0));
            //particleSystems.Add(upgradeSplash);
            //upgradeSplash.Stop();
            //upgradeSplash.gameObject.SetActive(false);

            loc.y += .1f; 
            removeSplash = Instantiate(removeSplash, loc, Quaternion.Euler(-90, 0, 0));
            removeSplash.Stop();
        }
    }

    public void InitializeImprovementData(ImprovementDataSO data)
    {
        improvementData = data;
    }

    //public void SetPSLocs()
    //{
    //    workFireLoc = improvementData.workFireLoc;
    //    workSmokeLoc = improvementData.workSmokeLoc;
    //}

    public void SetSmokeEmitters()
    {
        smokeEmitter = Instantiate(smokeEmitter, new Vector3(0, 0, 0), Quaternion.Euler(-90, 0, 0));
        smokeEmitter.Stop();

        smokeSplash = Instantiate(smokeSplash, new Vector3(0, 0, 0), Quaternion.Euler(-90, 0, 0));
        smokeSplash.Stop();

        smokeSlow = Instantiate(smokeSlow, new Vector3(0, 0, 0), Quaternion.Euler(-90, 0, 0));
        smokeSlow.Stop();
    }

    public void StartWork(int seconds)
    {
        foreach (Light light in workLights)
        {
            if (!light.isActiveAndEnabled)
                light.gameObject.SetActive(true);
        }
        
        if (improvementAnimator != null)
        {
            if (improvementData.workAnimLoop)
            {
                improvementAnimator.SetBool(isWorkingHash, true);
                if (improvementData.hideAnimMesh)
                    animMesh.SetActive(true);
            }
            else
            {
                if (improvementData.hideAnimMesh)
                    animMesh.SetActive(true);
                co = StartCoroutine(StartWorkAnimation(seconds));
            }
        }

        foreach (ParticleSystem ps in workPS)
        {
            if (!ps.isPlaying)
                ps.Play();
        }
    }

    //ridiculous workaround since you can't stop and then start an animation at the same time.
    private IEnumerator StartWorkAnimation(int seconds)
    {
        improvementAnimator.SetBool(isWorkingHash, false); //stop animation first
        yield return new WaitForSeconds(0.001f);
        improvementAnimator.SetBool(isWorkingHash, true);
        improvementAnimator.SetFloat("speed", 1f / seconds);
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
            if (improvementData.workAnimLoop)
            {
                improvementAnimator.SetBool(isWorkingHash, false);
                if (improvementData.hideAnimMesh)
                    animMesh.SetActive(false);
            }
            else
            {
                if (co != null)
                    StopCoroutine(co);
                improvementAnimator.SetBool(isWorkingHash, false);
                if (improvementData.hideAnimMesh)
                    animMesh.SetActive(false);
            }
        }

        foreach (ParticleSystem ps in workPS)
        {
            if (ps.isPlaying)
                ps.Stop();
        }
    }

    //public void StartWorkAnimation(int seconds = 1)
    //{
    //    if (improvementAnimator != null)
    //    {
    //        improvementAnimator.SetBool(isWorkingHash, true);
    //        improvementAnimator.SetFloat("speed", 1f / (seconds-1));
    //    }

    //    //if (animators.Count > 0)
    //    //{
    //    //    foreach (ImprovementAnimators animator in animators)
    //    //        animator.StopAnimation(false);
    //    //}
    //}

    //public void StopWorkAnimation()
    //{
    //    if (improvementAnimator != null)
    //        improvementAnimator.SetBool(isWorkingHash, false);
    //}

    //public void StopWaiting()
    //{
    //    improvementAnimator.SetBool(isWaitingHash, false);

    //    //if (animators.Count > 0)
    //    //{
    //    //    foreach (ImprovementAnimators animator in animators)
    //    //        animator.StopAnimation(true);
    //    //}
    //}

    //doing this so that the highlight doesn't mix with the combinedmesh.
    public void Embiggen()
    {
        Vector3 newScale = new Vector3(1.01f, 1.01f, 1.01f);

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

    //public void DestroyPS()
    //{
    //    //foreach (ParticleSystem ps in particleSystems)
    //    //    Destroy(ps.gameObject);
    //}

    public void EnableHighlight(Color highlightColor, bool secondary = false)
    {
        if (highlight.isGlowing)
            return;
        
        int count = meshFilter.Length;
        for (int i = 0; i < count; i++)
        {
            meshFilter[i].gameObject.SetActive(true);
        }
        
        highlight.EnableHighlight(highlightColor, secondary);
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

    //in case object has two of this script (such as in buildings)
    //public void EnableHighlight2(Color highlightColor)
    //{
    //    highlight[1].EnableHighlight(highlightColor);
    //}

    //public void DisableHighlight2()
    //{
    //    highlight[1].DisableHighlight();
    //}

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

    public void PlayUpgradeSwirl(int time)
    {
        upgradeSwirl.gameObject.SetActive(true);
        var main = upgradeSwirl.main;
        //start delay is a function of whatever the simulation speed is
        main.startDelay = time * 0.5f * 0.2f;
        upgradeSwirl.Play();

        upgradeSwirlDown.gameObject.SetActive(true);
        var mainDown = upgradeSwirlDown.main;
        mainDown.startDelay = time * 0.5f * 0.4f + 1.5f;
        upgradeSwirlDown.Play();
    }

    //public void PlayUpgradeSplash()
    //{
    //    upgradeSplash.Play();
    //}

    public void DestroyUpgradeSplash()
    {
        Destroy(upgradeSplash);
    }

    public void PlayRemoveEffect(bool isHill)
    {
        Vector3 loc = transform.position;
        if (isHill)
            loc.y += .8f;
        else
            loc.y += .1f;

        if (building)
        {
            removeSplash.transform.position = loc;
            removeSplash.Play();
        }
        else
        {
            removeEruption.transform.position = loc;
            removeEruption.Play();
        }
    }

    private void PlaySmokeEmitter(Vector3 loc)
    {
        int time = improvementData.buildTime;
        var emission = smokeEmitter.emission;
        emission.rateOverTime = 10f / time;
        smokeEmitter.transform.position = loc;
        smokeEmitter.gameObject.SetActive(true);
        smokeEmitter.Play();

        smokeSlow.transform.position = loc;
        smokeSlow.gameObject.SetActive(true);
        smokeSlow.Play();
    }

    private void PlaySmokeSplash(bool isHill)
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

    public void StopUpgradeSwirls()
    {
        if (upgradeSwirl.isPlaying)
        {
            upgradeSwirl.Stop();
            //upgradeSwirl.gameObject.SetActive(false);

            upgradeSwirlDown.Stop();
            //upgradeSwirlDown.gameObject.SetActive(false);
        }
    }

    public void StopSmokeEmitter()
    {
        smokeEmitter.Stop();
        smokeSlow.Stop();
        smokeEmitter.gameObject.SetActive(false);
        smokeSlow.gameObject.SetActive(false);
    }

    public void BeginImprovementConstructionProcess(City city, ResourceProducer producer, Vector3Int tempBuildLocation, CityBuilderManager cityBuilderManager, bool isHill)
    {
        Vector3 loc = transform.position;

        if (isHill)
            loc.y += 0.6f;
        else
            loc.y += .1f;
        PlaySmokeEmitter(loc); 
        constructionCo = StartCoroutine(BuildImprovementCoroutine(city, producer, tempBuildLocation, cityBuilderManager, isHill));
    }

    private IEnumerator BuildImprovementCoroutine(City city, ResourceProducer producer, Vector3Int tempBuildLocation, CityBuilderManager cityBuilderManager, bool isHill)
    {
        timePassed = improvementData.buildTime;

        producer.ShowConstructionProgressTimeBar(timePassed, city.activeCity);
        //if (!city.activeCity)
        //    producer.HideConstructionProgressTimeBar();
        producer.SetConstructionTime(timePassed);

        while (timePassed > 0)
        {
            yield return new WaitForSeconds(1);
            timePassed--;
            producer.SetConstructionTime(timePassed);
        }

        //if (isConstruction)
        StopSmokeEmitter();
        PlaySmokeSplash(isHill);
        producer.HideConstructionProgressTimeBar();
        cityBuilderManager.RemoveConstruction(tempBuildLocation);
        cityBuilderManager.FinishImprovement(city, improvementData, tempBuildLocation);
        cityBuilderManager.AddToConstructionTilePool(this);
    }

    public void BeginImprovementUpgradeProcess(City city, ResourceProducer producer, Vector3Int tempBuildLocation, CityBuilderManager cityBuilderManager, ImprovementDataSO data)
    {
        constructionCo = StartCoroutine(UpgradeImprovementCoroutine(city, producer, tempBuildLocation, data, cityBuilderManager));
    }

    private IEnumerator UpgradeImprovementCoroutine(City city, ResourceProducer producer, Vector3Int tempBuildLocation, ImprovementDataSO data, CityBuilderManager cityBuilderManager)
    {
        timePassed = data.buildTime;
        PlayUpgradeSwirl(timePassed);
        isUpgrading = true;
        producer.isUpgrading = true;
        producer.ShowConstructionProgressTimeBar(timePassed, city.activeCity);
        producer.SetConstructionTime(timePassed);

        while (timePassed > 1)
        {
            yield return new WaitForSeconds(1);
            timePassed--;
            producer.SetConstructionTime(timePassed);
        }
        upgradeFlash.Play();
        while (timePassed > 0)
        {
            yield return new WaitForSeconds(1);
            timePassed--;
            producer.SetConstructionTime(timePassed);
        }

        StopUpgradeProcess(producer);
        cityBuilderManager.UpgradeSelectedImprovement(tempBuildLocation, this, city, data);
    }

    public void StopUpgrade()
    {
        StopCoroutine(constructionCo);
    }

    public void StopUpgradeProcess(ResourceProducer producer)
    {
        StopUpgradeSwirls();
        isUpgrading = false;
        producer.isUpgrading = false;
        upgradeCost.Clear();
        producer.HideConstructionProgressTimeBar();
    }

    public void RemoveConstruction(CityBuilderManager cityBuilderManager, Vector3Int tempBuildLocation)
    {
        StopCoroutine(constructionCo);
        StopSmokeEmitter();
        cityBuilderManager.RemoveConstruction(tempBuildLocation);
        cityBuilderManager.AddToConstructionTilePool(this);
    }
}
