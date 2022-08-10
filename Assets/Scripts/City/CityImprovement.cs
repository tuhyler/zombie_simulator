using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CityImprovement : MonoBehaviour
{
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
