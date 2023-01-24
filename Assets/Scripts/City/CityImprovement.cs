using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Rendering.MaterialUpgrader;

public class CityImprovement : MonoBehaviour
{
    //[SerializeField]
    //private ImprovementDataSO improvementDataSO;
    //public ImprovementDataSO GetImprovementDataSO { get { return improvementDataSO; } }
    
    private SelectionHighlight[] highlight;
    private ImprovementDataSO improvementData;
    public ImprovementDataSO GetImprovementData { get { return improvementData; } }
    private City city; //for buildings, click on them to select city
    private City queueCity; //for improvements, when queued for upgrades
    [HideInInspector]
    public bool initialCityHouse, isConstruction, queued, building, isUpgrading;
    private List<ResourceValue> upgradeCost = new();
    public List<ResourceValue> UpgradeCost { get { return upgradeCost; } set { upgradeCost = value; } }

    [SerializeField]
    private ParticleSystem upgradeSwirl, upgradeSwirlDown, upgradeFlash, upgradeSplash, smokeSlow, smokeEmitter, smokeSplash, removeEruption, removeSplash;

    private Coroutine constructionCo;
    private int timePassed;
    public int GetTimePassed { get { return timePassed; } }

    private void Awake()
    {
        highlight = GetComponents<SelectionHighlight>();
    }

    private void Start()
    {
        Vector3 loc = transform.position;

        if (!building)
        {
            upgradeSwirl = Instantiate(upgradeSwirl, loc, Quaternion.Euler(-90, 0, 0));
            upgradeSwirl.Stop();
            upgradeSwirl.gameObject.SetActive(false);

            removeEruption = Instantiate(removeEruption, loc, Quaternion.Euler(-90, 0, 0));
            removeEruption.Stop();

            loc.y += 0.1f;
            upgradeFlash = Instantiate(upgradeFlash, loc, Quaternion.Euler(0, 0, 0));
            upgradeFlash.Stop();


            loc.y += 1.5f;
            upgradeSwirlDown = Instantiate(upgradeSwirlDown, loc, Quaternion.Euler(-270, 0, 0));
            upgradeSwirlDown.Stop();
            upgradeSwirlDown.gameObject.SetActive(false);

        }
        //else if (isConstruction)
        //{
        //    loc.y += 0.1f;
        //    smokeEmitter = Instantiate(smokeEmitter, loc, Quaternion.Euler(0, 0, 0));
        //    smokeEmitter.Pause();
        //    smokeEmitter.gameObject.SetActive(false);
        //}
        else
        {
            loc.y += .1f; 
            upgradeSplash = Instantiate(upgradeSplash, loc, Quaternion.Euler(-90, 0, 0));
            upgradeSplash.Stop();

            removeSplash = Instantiate(removeSplash, loc, Quaternion.Euler(-90, 0, 0));
            removeSplash.Stop();
        }
    }

    public void InitializeImprovementData(ImprovementDataSO data)
    {
        improvementData = data;
    }

    public void SetSmokeEmitters()
    {
        smokeEmitter = Instantiate(smokeEmitter, new Vector3(0, 0, 0), Quaternion.Euler(-90, 0, 0));
        smokeEmitter.Stop();
        smokeEmitter.gameObject.SetActive(false);

        smokeSplash = Instantiate(smokeSplash, new Vector3(0, 0, 0), Quaternion.Euler(-90, 0, 0));
        smokeSplash.Stop();

        smokeSlow = Instantiate(smokeSlow, new Vector3(0, 0, 0), Quaternion.Euler(-90, 0, 0));
        smokeSlow.Stop();
        smokeSlow.gameObject.SetActive(false);
    }

    public void EnableHighlight(Color highlightColor)
    {
        highlight[0].EnableHighlight(highlightColor);
    }

    public void DisableHighlight()
    {
        highlight[0].DisableHighlight();
    }

    //in case object has two of this script (such as in buildings)
    public void EnableHighlight2(Color highlightColor)
    {
        highlight[1].EnableHighlight(highlightColor);
    }

    public void DisableHighlight2()
    {
        highlight[1].DisableHighlight();
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

    public void PlayUpgradeSwirl(int time)
    {
        upgradeSwirl.gameObject.SetActive(true);
        var main = upgradeSwirl.main;
        //start delay is a function of whatever the simulation speed is
        main.startDelay = time * 0.5f * 0.4f;
        upgradeSwirl.Play();

        upgradeSwirlDown.gameObject.SetActive(true);
        var mainDown = upgradeSwirlDown.main;
        mainDown.startDelay = time * 0.5f * 0.4f + 1.5f;
        upgradeSwirlDown.Play();
    }

    public void PlayUpgradeSplash()
    {
        upgradeSplash.Play();
    }

    public void PlayRemoveEffect()
    {
        if (building)
            removeSplash.Play();
        else
            removeEruption.Play();
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
        smokeSplash.Play();
    }

    public void StopUpgradeSwirls()
    {
        if (upgradeSwirl.isPlaying)
        {
            upgradeSwirl.Stop();
            upgradeSwirl.gameObject.SetActive(false);

            upgradeSwirlDown.Stop();
            upgradeSwirlDown.gameObject.SetActive(false);
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
        cityBuilderManager.AddToConstructionTilePool(this);
        cityBuilderManager.FinishImprovement(city, improvementData, tempBuildLocation);
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

        while (timePassed > 2)
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
