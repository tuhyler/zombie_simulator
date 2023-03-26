using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UITooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector]
    public string title;

    [HideInInspector]
    public List<ResourceValue> produces, consumes, costs;
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        UITooltipSystem.Show(transform.position, title, produces, consumes, costs);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UITooltipSystem.Hide();
    }
}
