using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UITooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector]
    public string title, displayTitle;

    [HideInInspector]
    public int level, health, strength, cargo;

    [HideInInspector]
    public float workEthic, speed;

    [HideInInspector]
    public string description;

    [HideInInspector]
    public List<ResourceValue> produces, costs;

    [HideInInspector]
    public List<List<ResourceValue>> consumes = new();

    [HideInInspector]
    public List<int> produceTime;

    [HideInInspector]
    public bool unit;
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        UITooltipSystem.Show(transform.position, title, displayTitle, level, workEthic, description, costs, produces, consumes, produceTime, unit, health, speed, strength, cargo);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UITooltipSystem.Hide();
    }
}
