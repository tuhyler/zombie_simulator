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
    public bool initialCityHouse, isConstruction, queued, building;

    [SerializeField]
    private ParticleSystem upgradeSwirl, upgradeSwirlDown, upgradeFlash;

    private Coroutine constructionCo;
    private int timePassed;
    public int GetTimePassed { get { return timePassed; } }

    private void Awake()
    {
        highlight = GetComponents<SelectionHighlight>();
    }

    private void Start()
    {
        if (!building)
        {
            Vector3 loc = transform.position;
            upgradeSwirl = Instantiate(upgradeSwirl, loc, Quaternion.Euler(-90, 0, 0));
            upgradeSwirl.Pause();
            upgradeSwirl.gameObject.SetActive(false);

            loc.y += 0.1f;
            upgradeFlash = Instantiate(upgradeFlash, loc, Quaternion.Euler(0, 0, 0));
            upgradeFlash.Pause();
            //upgradeFlash.gameObject.SetActive(false);

            loc.y += 1.9f;
            upgradeSwirlDown = Instantiate(upgradeSwirlDown, loc, Quaternion.Euler(-270, 0, 0));
            upgradeSwirlDown.Pause();
            upgradeSwirlDown.gameObject.SetActive(false);

        }
    }

    public void InitializeImprovementData(ImprovementDataSO data)
    {
        improvementData = data;
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
        main.startDelay = time * 0.75f * 0.4f;
        upgradeSwirl.Play();

        upgradeSwirlDown.gameObject.SetActive(true);
        var mainDown = upgradeSwirlDown.main;
        mainDown.startDelay = time * 0.75f * 0.4f + 1.5f;
        upgradeSwirlDown.Play();
    }

    public void StopParticleSystems()
    {
        if (upgradeSwirl.isPlaying)
        {
            upgradeSwirl.Stop();
            upgradeSwirl.gameObject.SetActive(false);

            upgradeSwirlDown.Stop();
            upgradeSwirlDown.gameObject.SetActive(false);
            //upgradeFlash.Stop();
            //upgradeFlash.gameObject.SetActive(false);
        }
    }

    public void BeginImprovementConstructionProcess(City city, ResourceProducer producer, Vector3Int tempBuildLocation, CityBuilderManager cityBuilderManager)
    {
        constructionCo = StartCoroutine(BuildImprovementCoroutine(city, producer, tempBuildLocation, cityBuilderManager));
    }

    private IEnumerator BuildImprovementCoroutine(City city, ResourceProducer producer, Vector3Int tempBuildLocation, CityBuilderManager cityBuilderManager)
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

        StopParticleSystems();
        producer.isUpgrading = false;
        producer.HideConstructionProgressTimeBar();
        cityBuilderManager.UpgradeSelectedImprovement(tempBuildLocation, this, city, data);
    }

    public void RemoveConstruction(CityBuilderManager cityBuilderManager, Vector3Int tempBuildLocation)
    {
        StopCoroutine(constructionCo);
        cityBuilderManager.RemoveConstruction(tempBuildLocation);
        cityBuilderManager.AddToConstructionTilePool(this);
    }
}
