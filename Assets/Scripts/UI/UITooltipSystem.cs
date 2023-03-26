using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITooltipSystem : MonoBehaviour
{
    private static UITooltipSystem current;
    
    public UITooltip tooltip;

    private void Awake()
    {
        current = this;
    }

    public static void Show(Vector3 position, string title, List<ResourceValue> produces, List<ResourceValue> consumes, List<ResourceValue> costs)
    {
        current.tooltip.SetInfo(position, title, produces, consumes, costs);
        current.tooltip.gameObject.SetActive(true);
    }

    public static void Hide()
    {
        current.tooltip.gameObject.SetActive(false);
    }
}
