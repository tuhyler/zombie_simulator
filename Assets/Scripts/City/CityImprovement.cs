using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityImprovement : MonoBehaviour
{
    //[SerializeField]
    //private ImprovementDataSO improvementDataSO;
    //public ImprovementDataSO GetImprovementDataSO { get { return improvementDataSO; } }
    
    private SelectionHighlight highlight;

    private void Awake()
    {
        highlight = GetComponent<SelectionHighlight>();
    }

    public void EnableHighlight(Color highlightColor)
    {
        highlight.EnableHighlight(highlightColor);
    }

    public void DisableHighlight()
    {
        highlight.DisableHighlight();
    }
}
