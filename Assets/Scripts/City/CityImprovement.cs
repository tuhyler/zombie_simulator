using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityImprovement : MonoBehaviour
{
    //[SerializeField]
    //private ImprovementDataSO improvementDataSO;
    //public ImprovementDataSO GetImprovementDataSO { get { return improvementDataSO; } }
    
    private SelectionHighlight[] highlight;
    private string improvementName;
    public string ImprovementName { get { return improvementName; } set { improvementName = value; } }
    private City city;
    [HideInInspector]
    public bool initialCityHouse, isConstruction, singleBuild;
    private int constructionTime;
    public int ConstructionTime { get { return constructionTime; } set { constructionTime = value; } }
    private int buildingLevel = 99;
    public int SetBuildingLevel { set { buildingLevel = value; } }

    private Coroutine constructionCo;
    private int timePassed;
    public int GetTimePassed { get { return timePassed; } }

    private void Awake()
    {
        highlight = GetComponents<SelectionHighlight>();
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

    public void BeginImprovementConstructionProcess(City city, ResourceProducer producer, ImprovementDataSO improvementData, Vector3Int tempBuildLocation, CityBuilderManager cityBuilderManager)
    {
        constructionCo = StartCoroutine(BuildImprovementCoroutine(city, producer, improvementData, tempBuildLocation, cityBuilderManager));
    }

    private IEnumerator BuildImprovementCoroutine(City city, ResourceProducer producer, ImprovementDataSO improvementData, Vector3Int tempBuildLocation, CityBuilderManager cityBuilderManager)
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
