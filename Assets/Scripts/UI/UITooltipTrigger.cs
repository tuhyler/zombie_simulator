using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UITooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector]
    public string title;

    [HideInInspector]
    public int level;

    [HideInInspector]
    public float workEthic;

    [HideInInspector]
    public string description;

    [HideInInspector]
    public List<ResourceValue> produces, costs;

    [HideInInspector]
    public List<List<ResourceValue>> consumes = new();

    [HideInInspector]
    public bool unit;
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        UITooltipSystem.Show(transform.position, title, level, workEthic, description, costs, produces, consumes, unit);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UITooltipSystem.Hide();
    }
}
