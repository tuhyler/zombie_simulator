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
    public bool initialCityHouse, isConstruction;
    private int buildingLevel = 99;
    public int SetBuildingLevel { set { buildingLevel = value; } }

    private Coroutine constructionCo;

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
        int timePassed = improvementData.buildTime;

        producer.ShowConstructionProgressTimeBar(timePassed);
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

    //public void ShowConstructionProgressTimeBar(int time)
    //{
    //    Vector3 pos = transform.position;
    //    pos.z += -1f;
    //    timeProgressBar.gameObject.transform.position = pos;
    //    //timeProgressBar.SetConstructionTime(time);
    //    timeProgressBar.SetTimeProgressBarValue(time);
    //    timeProgressBar.SetActive(true);
    //}

    //public void HideConstructionProgressTimeBar()
    //{
    //    timeProgressBar.SetActive(false);
    //}

    //public void SetConstructionTime(int time)
    //{
    //    timeProgressBar.SetConstructionTime(time);
    //}
}
