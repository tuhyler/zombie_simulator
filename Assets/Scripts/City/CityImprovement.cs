using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public bool initialCityHouse, isConstruction, queued;

    private Coroutine constructionCo;
    private int timePassed;
    public int GetTimePassed { get { return timePassed; } }

    private void Awake()
    {
        highlight = GetComponents<SelectionHighlight>();
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

    public void RemoveConstruction(CityBuilderManager cityBuilderManager, Vector3Int tempBuildLocation)
    {
        StopCoroutine(constructionCo);
        cityBuilderManager.RemoveConstruction(tempBuildLocation);
        cityBuilderManager.AddToConstructionTilePool(this);
    }
}
